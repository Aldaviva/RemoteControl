using ManagedWinapi.Windows;
using Microsoft.Extensions.Options;
using RemoteControl.Caching;
using RemoteControl.Config;
using RemoteControl.Remote;
using System.Xml.XPath;
using Unfucked;

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
 */
public class XmlHttpVlcClient: AbstractControllableApplication, Vlc {

    private readonly HttpClient                           httpClient;
    private readonly IOptions<VlcConfiguration>           config;
    private readonly Uri                                  statusUrl;
    private readonly Uri                                  playlistUrl;
    private readonly SingletonAsyncCache<VlcStatus?>      statusCache;
    private readonly SingletonAsyncCache<XPathNavigator?> playlistCache;

    public XmlHttpVlcClient(HttpClient httpClient, IOptions<VlcConfiguration> config) {
        this.httpClient = httpClient;
        this.config     = config;

        Uri baseUri = new UriBuilder("http", "localhost", config.Value.port, "/requests/").Uri;
        statusUrl   = new Uri(baseUri, "status.xml");
        playlistUrl = new Uri(baseUri, "playlist_jstree.xml");

        statusCache   = new SingletonAsyncCache<VlcStatus?>(async () => await fetchStatus(), CACHE_DURATION);
        playlistCache = new SingletonAsyncCache<XPathNavigator?>(async () => await fetchPlaylist(), CACHE_DURATION);
    }

    protected override bool isApplicationWindow(SystemWindow window) => window.ClassName == "Qt5QWindowIcon" && "vlc".Equals(window.GetProcessExecutableBasename(), StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public override int priority { get; } = 1;

    /// <inheritdoc />
    public override string name { get; } = "VLC";

    /// <inheritdoc />
    public override async Task<bool> isPlaying() => await statusCache.value() is { state: PlaybackState.PLAYING };

    /// <inheritdoc />
    public override async Task<bool> isPlayable() => (await playlistCache.value())?.SelectSingleNode("/root/item/item[@name='Playlist']/item") != null;

    /// <inheritdoc />
    public override async Task sendButtonPress(RemoteControlButton button) {
        VlcStatus? status;
        switch (button) {
            case RemoteControlButton.PLAY_PAUSE:
                status = await statusCache.value();
                await sendCommand(status is { state: PlaybackState.STOPPED } ? "pl_play" : "pl_pause");
                break;
            case RemoteControlButton.PREVIOUS_TRACK:
                status = await statusCache.value();
                if (status is { state: not PlaybackState.STOPPED }) {
                    await seek(false);
                } else {
                    await sendCommand("pl_previous");
                }
                break;
            case RemoteControlButton.NEXT_TRACK:
                status = await statusCache.value();
                if (status is { state: not PlaybackState.STOPPED }) {
                    await seek(true);
                } else {
                    await sendCommand("pl_next");
                }
                break;
            case RemoteControlButton.STOP:
                await sendCommand("pl_stop");
                break;
            case RemoteControlButton.BAND:
                await sendCommand("fullscreen");
                break;
            default:
                break;
        }
    }

    /*
     * Seeking by percentage is better than seeking to a whole number of seconds because, even though the floating-point accuracy may not be perfect,
     * it's isomorphic (seeking forwards then backwards will put you in the original position, not the nearest second boundary)
     * and allows floating-point seeking durations (like 5.5 seconds).
     */
    private async Task seek(bool forwards) {
        if (await statusCache.value() is { } status) {
            int    durationMs  = status.length * 1000;
            double oldPosition = status.position * durationMs;
            double newPosition = Math.Min(1, Math.Max(0, (oldPosition + (forwards ? 1 : -1) * (int) config.Value.jumpDurationMs) / durationMs));
            await sendCommand("seek", new Dictionary<string, string> { { "val", $"{newPosition:F15}%" } });
        }
    }

    private async Task<VlcStatus?> fetchStatus() {
        try {
            using HttpResponseMessage response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, statusUrl), HttpCompletionOption.ResponseHeadersRead);
            return await readStatusResponse(response);
        } catch (TaskCanceledException) { } catch (HttpRequestException) { }
        return null;
    }

    private async Task<XPathNavigator?> fetchPlaylist() {
        try {
            using HttpResponseMessage response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, playlistUrl), HttpCompletionOption.ResponseHeadersRead);
            if (response.IsSuccessStatusCode) {
                return await response.Content.ReadXPathFromXmlAsync();
            }
        } catch (TaskCanceledException) { } catch (HttpRequestException) { }
        return null;
    }

    private async Task sendCommand(string command, IEnumerable<KeyValuePair<string, string>>? parameters = null) {
        try {
            using HttpResponseMessage response = await httpClient.GetAsync(new UriBuilder(statusUrl)
                .WithParameter("command", command)
                .WithParameter(parameters ?? [])
                .Uri); // do I need to call .ToEscapeString() instead?
        } catch (TaskCanceledException) { } catch (HttpRequestException) { }
    }

    private static async Task<VlcStatus?> readStatusResponse(HttpResponseMessage response) {
        return response.IsSuccessStatusCode ? await response.Content.ReadObjectFromXmlAsync<VlcStatus>() : null;
    }

}