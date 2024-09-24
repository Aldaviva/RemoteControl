using RemoteControl.Applications.Vivaldi;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;

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

    private const int BUFFER_SIZE = 1024;

    private static readonly JsonSerializerOptions JSON_SERIALIZER_OPTIONS = new() {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
        Converters             = { new JsonStringEnumConverter() },
        WriteIndented          = false
    };

    private readonly CancellationTokenSource disposing = new();

    private readonly ConcurrentDictionary<ulong, IOutstandingRequest<BrowserResponse>> outstandingRequests = new();
    private readonly ConcurrentDictionary<WebSocket, SemaphoreSlim>                    sendLocks           = new();

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
                        if (sendLocks.Remove(head, out SemaphoreSlim? sendLock)) {
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
        Task.Run(() => listenForResponsesFromClient(socket, disconnected), disposing.Token);
        connections.Push(socket);
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
                } while (!received.EndOfMessage);

                await pipe.Writer.CompleteAsync();
                await pipe.Reader.CompleteAsync();
                using JsonDocument jsonDocument    = (await deserialized)!;
                BrowserResponse    browserResponse = jsonDocument.Deserialize<BrowserResponse>()!;

                if (outstandingRequests.Remove(browserResponse.requestId, out IOutstandingRequest<BrowserResponse>? request)) {
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

                pipe.Reset();
            }
        } catch (WebSocketException e) when (e.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely) {
            disconnected?.SetResult();
            sendLocks.Remove(socket, out _);
            if (connections.TryPeek(out WebSocket? head) && head == socket && connections.TryPop(out head) && head != socket) {
                connections.Push(head);
            }
        } catch (TaskCanceledException) { }

        pipe.Reset();
    }

    /// <inheritdoc />
    public async Task<RESPONSE> sendCommandToMostRecentActiveConnection<RESPONSE>(BrowserCommand<RESPONSE> command) where RESPONSE: BrowserResponse {
        if (mostRecentActiveConnection is { } webSocket) {
            command.requestId = nextRequestId;
            byte[] serializedCommand = JsonSerializer.SerializeToUtf8Bytes(command, JSON_SERIALIZER_OPTIONS);

            IOutstandingRequest<RESPONSE> outstandingRequest = new OutstandingRequest<RESPONSE>();
            outstandingRequests[command.requestId] = (IOutstandingRequest<BrowserResponse>) outstandingRequest; // this cast will probably crash, unless interfaces magically rescue us

            SemaphoreSlim sendLock = sendLocks.GetOrAdd(webSocket, _ => new SemaphoreSlim(1));
            await sendLock.WaitAsync(disposing.Token);
            try {
                await webSocket.SendAsync(serializedCommand, WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage, disposing.Token);
            } finally {
                sendLock.Release();
            }

            return await outstandingRequest.onComplete.Task.WaitAsync(disposing.Token);
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
        foreach (SemaphoreSlim sendLock in sendLocks.Values) {
            sendLock.Dispose();
        }
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
        foreach (SemaphoreSlim sendLock in sendLocks.Values) {
            sendLock.Dispose();
        }
        disposing.Dispose();
        GC.SuppressFinalize(this);
    }

}

internal interface IOutstandingRequest<RESPONSE> where RESPONSE: BrowserResponse {

    public TaskCompletionSource<RESPONSE> onComplete { get; }
    public Type responseType { get; }

}

internal class OutstandingRequest<RESPONSE>: IOutstandingRequest<RESPONSE> where RESPONSE: BrowserResponse {

    public TaskCompletionSource<RESPONSE> onComplete { get; } = new();

    public Type responseType => typeof(RESPONSE);

}