using Microsoft.Extensions.Options;
using RemoteControl.Applications;
using RemoteControl.Applications.Vlc;
using RemoteControl.Applications.Winamp;
using RemoteControl.Config;
using RemoteControl.Remote;
using System.Net;
using Unfucked;
using Application = System.Windows.Forms.Application;

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
    .AddSingleton<InfraredListener, VirtualKeyboardInfraredListener>()
    .AddSingleton<ControllableApplication, WinampInterProcessMessageClient>()
    .AddSingleton<ControllableApplication, XmlHttpVlcClient>();

await using WebApplication webapp = builder.Build();
webapp.Services.GetRequiredService<InfraredListener>().listen();

Task webappTask = webapp.RunAsync()
    .ContinueWith(_ => Application.Exit());

Application.Run(); // add a message pump so global keyboard shortcuts can be detected
await webappTask;