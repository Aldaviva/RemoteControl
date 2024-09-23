using ManagedWinapi.Windows;
using RemoteControl.Remote;
using System.Runtime.InteropServices;

namespace RemoteControl.Applications.Winamp;

public interface Winamp: ControllableApplication;

/*
 * Documentation: https://github.com/patrickmmartin/winampremote/blob/master/source/API.txt
 */
public partial class WinampInterProcessMessageClient: AbstractControllableApplication, Winamp {

    private const uint WM_USER    = 0x400;
    private const uint WM_COMMAND = 0x111;

    /// <inheritdoc />
    protected override bool isApplicationWindow(SystemWindow window) => window.ClassName == "Winamp v1.x";

    /// <inheritdoc />
    public override int priority { get; } = 2;

    /// <inheritdoc />
    public override string name { get; } = "Winamp";

    /// <inheritdoc />
    public override Task<bool> isPlaying() => Task.FromResult(getPlaybackState() == PlaybackState.PLAYING);

    /// <inheritdoc />
    public override Task<bool> isPlayable() => Task.FromResult(appWindow?.HWnd is { } winampWindowHandle && sendMessage(winampWindowHandle, WM_USER, 0, UserMessageId.GET_PLAYLIST_TRACK_COUNT) != 0);

    /// <inheritdoc />
    public override Task sendButtonPress(RemoteControlButton button) {
        if (appWindow?.HWnd is { } winampWindowHandle) {
            switch (button) {
                case RemoteControlButton.PLAY_PAUSE:
                    _ = sendMessage(winampWindowHandle, WM_COMMAND, 0, getPlaybackState() == PlaybackState.STOPPED ? CommandMessageId.PLAY : CommandMessageId.PAUSE);
                    break;
                case RemoteControlButton.PREVIOUS_TRACK:
                    _ = sendMessage(winampWindowHandle, WM_COMMAND, 0, CommandMessageId.PREVIOUS_TRACK);
                    break;
                case RemoteControlButton.NEXT_TRACK:
                    _ = sendMessage(winampWindowHandle, WM_COMMAND, 0, CommandMessageId.NEXT_TRACK);
                    break;
                case RemoteControlButton.STOP:
                    _ = sendMessage(winampWindowHandle, WM_COMMAND, 0, CommandMessageId.STOP);
                    break;
                default:
                    break;
            }
        }
        return Task.CompletedTask;
    }

    private PlaybackState getPlaybackState() {
        if (appWindow?.HWnd is { } winampWindowHandle) {
            PlaybackState playbackState = (PlaybackState) sendMessage(winampWindowHandle, WM_USER, 0, UserMessageId.GET_PLAYBACK_STATE);
            return Enum.IsDefined(playbackState) ? playbackState : PlaybackState.STOPPED;
        } else {
            return PlaybackState.STOPPED;
        }
    }

    [LibraryImport("user32.dll", EntryPoint = "SendMessage", SetLastError = true)]
    private static partial int sendMessage(IntPtr windowHandle, uint message, uint wParameter, UserMessageId lParameter);

    [LibraryImport("user32.dll", EntryPoint = "SendMessage", SetLastError = true)]
    private static partial int sendMessage(IntPtr windowHandle, uint message, uint wParameter, CommandMessageId lParameter);

}