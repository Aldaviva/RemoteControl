using Microsoft.Extensions.Options;
using RemoteControl.Caching;
using RemoteControl.Config;
using RemoteControl.Remote;
using SimWinInput;
using System.Net.Sockets;
using System.Xml.XPath;
using ThrottleDebounce;
using Unfucked;
using Unfucked.HTTP;

namespace RemoteControl.Applications.Vlc;

public interface Vlc: ControllableApplication;

/*
 * Sends HTTP requests with Basic authentication to the localhost web server in VLC
 * Prerequisites:
 * 1. Set a password using Tools > Preferences > All > Interface > Main interfaces > Lua > Lua HTTP > Password
 * 2. Enable "Web" under Tools > Preferences > All > Interface > Main interfaces > Extra interface modules
 * 3. Click Save
 * 4. Restart VLC twice, allowing the inbound firewall rules both times (it asks for different NAT traversal rules the second time, causing a second prompt)
 *
 * To change the VLC listening port from 8080, try https://superuser.com/a/1549408/339084
 *
 * Documentation: https://code.videolan.org/videolan/vlc/-/blob/master/share/lua/http/requests/README.txt
 * Source:        https://code.videolan.org/videolan/vlc/-/blob/master/share/lua/http/requests/status.xml
 *                https://code.videolan.org/videolan/vlc/-/blob/master/share/lua/intf/modules/httprequests.lua
 */
public class VlcXmlHttpClient: AbstractControllableApplication, Vlc {

    private const uint MAX_ATTEMPTS = 5;

    private readonly IOptions<VlcConfiguration>           config;
    private readonly ILogger<VlcXmlHttpClient>            logger;
    private readonly SingletonAsyncCache<VlcStatus?>      statusCache;
    private readonly SingletonAsyncCache<XPathNavigator?> playlistCache;

    protected override string windowClassName { get; } = "Qt5QWindowIcon";
    protected override string? executableFilename { get; } = "vlc.exe";
    public override ApplicationPriority priority { get; } = ApplicationPriority.VLC;
    public override string name { get; } = "VLC";

    private readonly IWebTarget vlcApi;

    public VlcXmlHttpClient(HttpClient httpClient, IOptions<VlcConfiguration> config, ILogger<VlcXmlHttpClient> logger) {
        this.config = config;
        this.logger = logger;

        vlcApi        = httpClient.Target(new UrlBuilder("http", "localhost", config.Value.port).Path("requests"));
        statusCache   = new SingletonAsyncCache<VlcStatus?>(async () => await fetchStatus(), CACHE_DURATION);
        playlistCache = new SingletonAsyncCache<XPathNavigator?>(async () => await fetchPlaylist(), CACHE_DURATION);
    }

    public override async Task<PlaybackState> fetchPlaybackState() {
        Task<VlcStatus?>      status   = statusCache.value();
        Task<XPathNavigator?> playlist = playlistCache.value();
        return new PlaybackState(
            isPlaying: await status is { playbackState: VlcPlaybackState.PLAYING or VlcPlaybackState.STARTED },
            canPlay: (await playlist)?.SelectSingleNode("/node/node[@name='Playlist']/leaf") != null);
    }

    public override async Task sendButtonPress(RemoteControlButton button) {
        switch (button) {
            case RemoteControlButton.PLAY_PAUSE:
                await sendCommand("pl_pause");
                unminimize();
                statusCache.clear();
                break;
            case RemoteControlButton.PREVIOUS_TRACK:
                await seekOrChangeTrack(false);
                // status cache is cleared by seekOrChangeTrack
                break;
            case RemoteControlButton.NEXT_TRACK:
                await seekOrChangeTrack(true);
                // status cache is cleared by seekOrChangeTrack
                break;
            case RemoteControlButton.STOP:
                await sendCommand("pl_stop");
                statusCache.clear();
                break;
            case RemoteControlButton.MEMORY:
                await sendCommand("fullscreen");
                statusCache.clear();
                break;
            case RemoteControlButton.BAND:
                SimKeyboard.Press((byte) Keys.T);
                break;
            default:
                break;
        }
    }

    private async Task seekOrChangeTrack(bool forwards) {
        if (await statusCache.value() is { playbackState: VlcPlaybackState.STOPPED or VlcPlaybackState.STOPPING }) {
            await sendCommand(forwards ? "pl_next" : "pl_previous");
        } else {
            // positive sign is required to make it a relative seek instead of absolute
            await sendCommand("seek", "val", $"{(forwards ? '+' : '-')}{config.Value.jumpDurationSec:D}s");
        }
        statusCache.clear();
    }

    private async Task<VlcStatus?> fetchStatus() {
        try {
            return await Retrier.Attempt(async _ => await vlcApi.Path("status.xml").Get<VlcStatus>(), MAX_ATTEMPTS, retryDelay, shouldRetry);
        } catch (TaskCanceledException) {
            logger.LogWarning("Request to fetch VLC status timed out");
        } catch (HttpRequestException e) when (isConnectionReset(e)) {
            logger.LogWarning("Request to fetch VLC status had its connection reset");
        } catch (HttpRequestException e) {
            logger.LogWarning(e, "Request to fetch VLC status failed");
        }
        return null;
    }

    private async Task<XPathNavigator?> fetchPlaylist() {
        try {
            return await Retrier.Attempt(async _ => await vlcApi.Path("playlist.xml").Get<XPathNavigator>(), MAX_ATTEMPTS, retryDelay, shouldRetry);
        } catch (TaskCanceledException) {
            logger.LogWarning("Request to fetch VLC playlist timed out");
        } catch (HttpRequestException e) when (isConnectionReset(e)) {
            logger.LogWarning("Request to fetch VLC playlist had its connection reset");
        } catch (HttpRequestException e) {
            logger.LogWarning(e, "Request to fetch VLC playlist failed");
        }
        return null;
    }

    private Task sendCommand(string command, string parameterName, string parameterValue) => sendCommand(command, [new KeyValuePair<string, string>(parameterName, parameterValue)]);

    private async Task sendCommand(string command, IEnumerable<KeyValuePair<string, string>>? parameters = null) {
        try {
            await Retrier.Attempt(async _ => (await vlcApi.Path("status.xml").QueryParam("command", command).QueryParam(parameters ?? []).Get()).Dispose(), MAX_ATTEMPTS, retryDelay, shouldRetry);
        } catch (TaskCanceledException) {
            logger.LogWarning("Command {command} to VLC timed out", command);
        } catch (HttpRequestException e) when (isConnectionReset(e)) {
            logger.LogWarning("Command {command} to VLC had its connection reset", command);
        } catch (HttpRequestException e) {
            logger.LogWarning(e, "Command {command} to VLC failed", command);
        }
    }

    private static TimeSpan retryDelay(int attempt) => TimeSpan.Zero;

    private static bool shouldRetry(Exception exception) => isConnectionReset(exception);

    private static bool isConnectionReset(Exception exception) => exception is HttpRequestException {
        InnerException: IOException { InnerException: SocketException { SocketErrorCode: SocketError.ConnectionReset } }
    };

    protected override void Dispose(bool disposing) {
        if (disposing) {
            statusCache.Dispose();
            playlistCache.Dispose();
        }
        base.Dispose(disposing);
    }

}