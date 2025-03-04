using ManagedWinapi.Windows;
using RemoteControl.Remote;
using System.Runtime.InteropServices;

namespace RemoteControl.Applications.Winamp;

public interface Winamp: ControllableApplication;

/*
 * Documentation: https://github.com/patrickmmartin/winampremote/blob/master/source/API.txt
 */
public partial class WinampInterProcessMessageClient(ILogger<WinampInterProcessMessageClient> logger): AbstractControllableApplication, Winamp {

    private const uint WM_USER    = 0x400;
    private const uint WM_COMMAND = 0x111;

    protected override string windowClassName { get; } = "Winamp v1.x";
    protected override string executableFilename { get; } = "winamp.exe";
    public override ApplicationPriority priority { get; } = ApplicationPriority.WINAMP;
    public override string name { get; } = "Winamp";

    protected override bool isApplicationWindow(SystemWindow window) => window.ClassName == windowClassName; // class check is unique enough, we don't need to look up the process too

    public override Task<PlaybackState> fetchPlaybackState() => Task.FromResult(new PlaybackState(
        isPlaying: getPlaybackState() == WinampPlaybackState.PLAYING,
        canPlay: appWindow?.HWnd is { } winampWindowHandle && sendUserMessage(winampWindowHandle, UserMessageId.GET_PLAYLIST_TRACK_COUNT, 0) != 0));

    public override Task sendButtonPress(RemoteControlButton button) {
        if (appWindow?.HWnd is { } winampWindowHandle) {
            switch (button) {
                case RemoteControlButton.PLAY_PAUSE:
                    sendCommandMessage(winampWindowHandle, getPlaybackState() == WinampPlaybackState.STOPPED ? CommandMessageId.PLAY : CommandMessageId.PAUSE);
                    break;
                case RemoteControlButton.PREVIOUS_TRACK:
                    sendCommandMessage(winampWindowHandle, CommandMessageId.PREVIOUS_TRACK);
                    break;
                case RemoteControlButton.NEXT_TRACK:
                    sendCommandMessage(winampWindowHandle, CommandMessageId.NEXT_TRACK);
                    break;
                case RemoteControlButton.STOP:
                    sendCommandMessage(winampWindowHandle, CommandMessageId.STOP);
                    break;
                default:
                    break;
            }
        } else {
            logger.LogWarning("No Winamp window to send messages to");
        }
        return Task.CompletedTask;
    }

    private WinampPlaybackState getPlaybackState() => appWindow?.HWnd is { } winampWindowHandle ? (WinampPlaybackState) sendUserMessage(winampWindowHandle, UserMessageId.GET_PLAYBACK_STATE, 0)
        : WinampPlaybackState.STOPPED;

    private int sendUserMessage(IntPtr windowHandle, UserMessageId message, uint data) {
        int response = sendMessage(windowHandle, WM_USER, data, (uint) message);
        logger.LogDebug("Sent {message} user message to Winamp with data {data}, received {response}", message, data, response);
        return response;
    }

    private void sendCommandMessage(IntPtr windowHandle, CommandMessageId message) {
        int response = sendMessage(windowHandle, WM_COMMAND, (uint) message, 0);
        logger.LogDebug("Sent {message} command message to Winamp, received {response}", message, response);
    }

    [LibraryImport("User32.dll", EntryPoint = "SendMessageW", SetLastError = true)]
    private static partial int sendMessage(IntPtr windowHandle, uint message, uint wParameter, uint lParameter);

}