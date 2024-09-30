using Microsoft.AspNetCore.Connections;
using RemoteControl.Applications.Vivaldi;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using Unfucked;

namespace RemoteControl;

public interface WebSocketDispatcher: IDisposable, IAsyncDisposable {

    Task onConnection(WebSocket socket);

    IEnumerable<WebSocket> allConnections { get; }

    WebSocket? mostRecentActiveConnection { get; }

    ulong nextRequestId { get; }

    /// <exception cref="NoBrowserConnected">if the browser isn't running, the extension isn't installed, or its WebSocket connection dropped</exception>
    /// <exception cref="BrowserExtensionException">or one of its subclasses if the browser extension returned an exception response</exception>
    /// <exception cref="UnmappedBrowserExtensionException">if the browser extension returned a response where <c>exception</c> does not have the same name as any subclasses of <see cref="BrowserExtensionException"/> in the same namespace</exception>
    Task<RESPONSE> sendCommandToMostRecentActiveConnection<RESPONSE>(BrowserCommand<RESPONSE> command) where RESPONSE: BrowserResponse;

}

public class WebSocketStackDispatcher(ILogger<WebSocketStackDispatcher> logger): WebSocketDispatcher {

    private const int    BUFFER_SIZE = 1024;
    private const string PING        = "ping";

    private static readonly JsonSerializerOptions JSON_SERIALIZER_OPTIONS = new() {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
        Converters             = { new JsonStringEnumConverter() },
        WriteIndented          = false
    };

    private readonly CancellationTokenSource                         disposing           = new();
    private readonly ConcurrentDictionary<ulong, OutstandingRequest> outstandingRequests = new();
    private readonly ConcurrentDictionary<WebSocket, SemaphoreSlim>  sendLocks           = new();

    private ulong requestId;
    public ulong nextRequestId => Interlocked.Increment(ref requestId); // silently wraps on overflow, which is nice

    private readonly ConcurrentStack<WebSocket> connections = [];
    public IEnumerable<WebSocket> allConnections => connections;

    public WebSocket? mostRecentActiveConnection {
        get {
            while (connections.TryPeek(out WebSocket? head)) {
                if (head.State == WebSocketState.Open) {
                    return head;
                } else if (connections.TryPop(out head)) {
                    if (head.State != WebSocketState.Open) {
                        if (sendLocks.TryRemove(head, out SemaphoreSlim? sendLock)) {
                            sendLock.Dispose();
                        }
                    } else { // accidentally popped an open connection, put it back and try again
                        connections.Push(head);
                        return head;
                    }
                }
            }
            return null; // connections was empty or all were closed
        }
    }

    public Task onConnection(WebSocket socket) {
        TaskCompletionSource disconnected = new();
        Task.Run(async () => await listenForResponsesFromClient(socket, disconnected), disposing.Token);
        connections.Push(socket);
        logger.LogDebug("Client connected over WebSocket. There are currently {total} client(s) connected.", connections.Count);
        return disconnected.Task;
    }

    private async Task listenForResponsesFromClient(WebSocket socket, TaskCompletionSource? disconnected = null) {
        Pipe pipe = new();
        try {
            while (socket.CloseStatus == null && !disposing.IsCancellationRequested) {
                ValueTask<JsonDocument?> deserialized = JsonSerializer.DeserializeAsync<JsonDocument>(pipe.Reader.AsStream(), JSON_SERIALIZER_OPTIONS, disposing.Token);

                ValueWebSocketReceiveResult received;
                do {
                    received = await socket.ReceiveAsync(pipe.Writer.GetMemory(BUFFER_SIZE), disposing.Token);
                    pipe.Writer.Advance(received.Count);
                    logger.LogTrace("Received {count:N0} {type} bytes from a client", received.Count, received.MessageType);
                } while (!received.EndOfMessage);
                await pipe.Writer.CompleteAsync();

                if (received.MessageType != WebSocketMessageType.Close) {
                    using JsonDocument jsonDocument = (await deserialized)!;
                    if (jsonDocument.RootElement.ValueKind != JsonValueKind.String || jsonDocument.RootElement.GetString() != PING) {
                        BaseResponse browserResponse = jsonDocument.Deserialize<BaseResponse>(JSON_SERIALIZER_OPTIONS)!;

                        if (outstandingRequests.TryRemove(browserResponse.requestId, out OutstandingRequest? request)) {
                            if (browserResponse.exception == null) {
                                request.onComplete.SetResult((BrowserResponse) jsonDocument.Deserialize(request.responseType, JSON_SERIALIZER_OPTIONS)!);
                            } else {
                                Type  exceptionSuperclass = typeof(BrowserExtensionException);
                                Type? exceptionClass      = exceptionSuperclass.Assembly.GetType($"{exceptionSuperclass.Namespace}.{browserResponse.exception}");
                                BrowserExtensionException exception = exceptionClass is not null && exceptionClass.IsAssignableTo(exceptionSuperclass)
                                    ? (BrowserExtensionException) jsonDocument.Deserialize(exceptionClass, JSON_SERIALIZER_OPTIONS)!
                                    : new UnmappedBrowserExtensionException(browserResponse.exception!, jsonDocument);
                                request.onComplete.SetException(exception);
                            }
                        } else {
                            logger.LogWarning("No outstanding request found for id {reqId}, ignoring incoming message {msg}", browserResponse.requestId, jsonDocument);
                        }
                    } else {
                        logger.LogTrace("Received ping from client");
                    }
                } else {
                    onDisconnected();
                }

                await pipe.Reader.CompleteAsync();
                pipe.Reset();
            }
        } catch (ConnectionAbortedException e) when (e.Message is "The Socket transport's send loop completed gracefully.") {
            // normal disconnection
        } catch (WebSocketException e) when (e.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely) {
            onDisconnected();
        } catch (TaskCanceledException) { } catch (Exception e) when (e is not OutOfMemoryException) {
            logger.LogError(e, "Uncaught exception while receiving WebSocket response from client");
        }

        pipe.Reset();

        void onDisconnected() {
            disconnected?.SetResult();
            sendLocks.TryRemove(socket, out _);
            if (connections.TryPeek(out WebSocket? head) && head == socket && connections.TryPop(out head) && head != socket) {
                connections.Push(head);
            }
            logger.LogDebug("Client disconnected from WebSocket. There are currently {total} client(s) connected.", connections.Count);
        }
    }

    /// <inheritdoc />
    public async Task<RESPONSE> sendCommandToMostRecentActiveConnection<RESPONSE>(BrowserCommand<RESPONSE> command) where RESPONSE: BrowserResponse {
        if (mostRecentActiveConnection is { } webSocket) {
            command.requestId = nextRequestId;
            // manually specify the type of the object to be serialized to JSON as its concrete type, not its declared type, otherwise its properties will be missing
            byte[] serializedCommand = JsonSerializer.SerializeToUtf8Bytes(command, command.GetType(), JSON_SERIALIZER_OPTIONS);

            OutstandingRequest outstandingRequest = OutstandingRequest.create<RESPONSE>();
            outstandingRequests[command.requestId] = outstandingRequest;

            SemaphoreSlim sendLock = sendLocks.GetOrAddWithDisposal(webSocket, _ => new SemaphoreSlim(1));
            await sendLock.WaitAsync(disposing.Token);
            try {
                await webSocket.SendAsync(serializedCommand, WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage, disposing.Token);
            } finally {
                sendLock.Release();
            }

            BrowserResponse browserResponse = await outstandingRequest.onComplete.Task.WaitAsync(disposing.Token);
            return (RESPONSE) browserResponse;
        } else {
            throw new NoBrowserConnected($"No browser extension was connected to the WebSocket server while trying to send a {command.GetType()} command, ignoring the command");
        }
    }

    /// <inheritdoc />
    public void Dispose() {
        disposing.Cancel();
        foreach (WebSocket webSocket in allConnections) {
            webSocket.Dispose();
        }
        connections.Clear();
        foreach (OutstandingRequest outstandingRequest in outstandingRequests.Values) {
            outstandingRequest.onComplete.TrySetCanceled(disposing.Token);
        }
        outstandingRequests.Clear();
        foreach (SemaphoreSlim sendLock in sendLocks.Values) {
            sendLock.Dispose();
        }
        sendLocks.Clear();
        disposing.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() {
        await disposing.CancelAsync();
        await Task.WhenAll(allConnections.Select(async webSocket => {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "server exiting", CancellationToken.None);
            webSocket.Dispose();
        }));
        connections.Clear();
        foreach (OutstandingRequest outstandingRequest in outstandingRequests.Values) {
            outstandingRequest.onComplete.TrySetCanceled(disposing.Token);
        }
        outstandingRequests.Clear();
        foreach (SemaphoreSlim sendLock in sendLocks.Values) {
            sendLock.Dispose();
        }
        sendLocks.Clear();
        disposing.Dispose();
        GC.SuppressFinalize(this);
    }

}

internal class OutstandingRequest {

    private OutstandingRequest(Type responseType) => this.responseType = responseType;

    public static OutstandingRequest create<T>() => new(typeof(T));

    public Type responseType { get; }
    public TaskCompletionSource<BrowserResponse> onComplete { get; } = new();

}