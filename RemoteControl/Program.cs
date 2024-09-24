using Microsoft.Extensions.Options;
using RemoteControl;
using RemoteControl.Applications;
using RemoteControl.Applications.Vivaldi;
using RemoteControl.Applications.Vlc;
using RemoteControl.Applications.Winamp;
using RemoteControl.Config;
using RemoteControl.Remote;
using System.Net;
using System.Net.WebSockets;
using Unfucked;

Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);
Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Configuration.AlsoSearchForJsonFilesInExecutableDirectory();

builder.Logging.AddUnfuckedConsole();

builder.Services
    .Configure<VlcConfiguration>(builder.Configuration.GetSection("vlc"))
    .AddSingleton(context => {
        VlcConfiguration vlcConfig = context.GetRequiredService<IOptions<VlcConfiguration>>().Value;
        return new HttpClient(new SocketsHttpHandler {
                PreAuthenticate = true,
                Credentials = new CredentialCache {
                    { "localhost", vlcConfig.port, "Basic", new NetworkCredential(null, vlcConfig.password) }
                }
            })
            { Timeout = TimeSpan.FromMilliseconds(vlcConfig.timeoutMs) };
    })
    .AddSingleton<WebSocketDispatcher, WebSocketStackDispatcher>()
    .AddSingleton<InfraredListener, VirtualKeyboardInfraredListener>()
    .AddSingleton<ControllableApplication, WinampInterProcessMessageClient>()
    .AddSingleton<ControllableApplication, XmlHttpVlcClient>()
    .AddSingleton<ControllableApplication, WebSocketExtensionVivaldiClient>();

await using WebApplication webapp = builder.Build();
webapp.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(20), AllowedOrigins = { "chrome-extension://cfigdmahkckgdbefgnabphhjifcajjij" } });
webapp.Services.GetRequiredService<InfraredListener>().listen();

CancellationToken applicationStopping = webapp.Lifetime.ApplicationStopping;
webapp.Use(async (req, next) => {
    if (req.Request.Path == "/ws") {
        if (req.WebSockets.IsWebSocketRequest) {
            using WebSocket webSocket    = await req.WebSockets.AcceptWebSocketAsync();
            Task            disconnected = req.RequestServices.GetRequiredService<WebSocketDispatcher>().onConnection(webSocket);

            await Task.WhenAny(disconnected, CancellationTokenSource.CreateLinkedTokenSource(req.RequestAborted, applicationStopping).Token.Wait());
        } else {
            req.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    } else {
        await next(req);
    }
});

Task webappTask = webapp.RunAsync()
    .ContinueWith(_ => Application.Exit());

Application.Run(); // adds a message pump so global keyboard shortcuts can be detected
await webappTask;