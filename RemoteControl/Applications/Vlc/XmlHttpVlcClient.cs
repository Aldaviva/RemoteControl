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
 *
 * Documentation: https://code.videolan.org/videolan/vlc/-/blob/master/share/lua/http/requests/README.txt
 * Source:        https://code.videolan.org/videolan/vlc/-/blob/master/share/lua/http/requests/status.xml
 *                https://code.videolan.org/videolan/vlc/-/blob/master/share/lua/intf/modules/httprequests.lua
 */
public class XmlHttpVlcClient: AbstractControllableApplication, Vlc {

    private readonly HttpClient                           httpClient;
    private readonly IOptions<VlcConfiguration>           config;
    private readonly Uri                                  statusUrl;
    private readonly Uri                                  playlistUrl;
    private readonly SingletonAsyncCache<VlcStatus?>      statusCache;
    private readonly SingletonAsyncCache<XPathNavigator?> playlistCache;

    protected override string windowClassName { get; } = "Qt5QWindowIcon";
    protected override string? processBaseName { get; } = "vlc";
    public override int priority { get; } = 1;
    public override string name { get; } = "VLC";

    public XmlHttpVlcClient(HttpClient httpClient, IOptions<VlcConfiguration> config) {
        this.httpClient = httpClient;
        this.config     = config;

        Uri baseUri = new UriBuilder("http", "localhost", config.Value.port, "/requests/").Uri;
        statusUrl     = new Uri(baseUri, "status.xml");
        playlistUrl   = new Uri(baseUri, "playlist.xml");
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
        VlcStatus? status;
        switch (button) {
            case RemoteControlButton.PLAY_PAUSE:
                await sendCommand("pl_pause");
                break;
            case RemoteControlButton.PREVIOUS_TRACK:
                status = await statusCache.value();
                if (status is { playbackState: VlcPlaybackState.STOPPED or VlcPlaybackState.STOPPING }) {
                    await sendCommand("pl_previous");
                } else {
                    await seek(false);
                }
                break;
            case RemoteControlButton.NEXT_TRACK:
                status = await statusCache.value();
                if (status is { playbackState: VlcPlaybackState.STOPPED or VlcPlaybackState.STOPPING }) {
                    await sendCommand("pl_next");
                } else {
                    await seek(true);
                }
                break;
            case RemoteControlButton.STOP:
                await sendCommand("pl_stop");
                break;
            case RemoteControlButton.MEMORY:
                await sendCommand("fullscreen");
                break;
            default:
                break;
        }
    }

    private async Task seek(bool forwards) {
        await sendCommand("seek", "val", $"{(forwards ? '+' : '-')}{config.Value.jumpDurationSec:D}s");
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

    private Task sendCommand(string command, string parameterName, string parameterValue) {
        return sendCommand(command, [new KeyValuePair<string, string>(parameterName, parameterValue)]);
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