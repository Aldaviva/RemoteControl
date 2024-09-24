using Gma.System.MouseKeyHook;
using RemoteControl.Applications;

namespace RemoteControl.Remote;

public interface InfraredListener: IDisposable {

    void listen();

}

public class VirtualKeyboardInfraredListener(ILogger<VirtualKeyboardInfraredListener> logger, IEnumerable<ControllableApplication> allApplications): InfraredListener {

    private IKeyboardMouseEvents? keyboardShortcuts;

    public void listen() {
        if (keyboardShortcuts == null) {
            keyboardShortcuts = Hook.GlobalEvents();
            keyboardShortcuts.KeyDown += (_, args) => {
                // Ctrl is also part of what Flirc types, but I don't care if Ctrl is pressed here, because as the first keystroke from the Flirc, Ctrl gets eaten by the screensaver if and only if it was running, avoiding the need to press the remote control button twice if the screensaver was running.
                RemoteControlButton? remoteControlButton = args is { Shift: true, Alt: true, KeyCode: var keyCode } ? keyCode switch {
                    Keys.F7  => RemoteControlButton.PREVIOUS_TRACK,
                    Keys.F8  => RemoteControlButton.PLAY_PAUSE,
                    Keys.F9  => RemoteControlButton.NEXT_TRACK,
                    Keys.F10 => RemoteControlButton.BAND,
                    Keys.F11 => RemoteControlButton.STOP,
                    Keys.F12 => RemoteControlButton.MEMORY,
                    _        => null
                } : null;

                args.Handled = remoteControlButton.HasValue;
                if (remoteControlButton.HasValue) {
                    onPressRemoteControlButton(remoteControlButton.Value);
                }
            };
            logger.LogDebug("Listening for infrared remote control button presses");
        }
    }

    private async void onPressRemoteControlButton(RemoteControlButton button) {
        if (await getTargetApplication() is { } targetApplication) {
            await targetApplication.sendButtonPress(button);
            logger.LogDebug("Sent {button} to {appName}", button, targetApplication.name);
        } else {
            logger.LogDebug("No running application to send {button} to, ignoring it", button);
        }
    }

    // ReSharper disable once MergeIntoPattern - variable must be used even if the subsequent check fails, so it can't be in a pattern because the variable scope is too small
    private async Task<ControllableApplication?> getTargetApplication() => allApplications
        .Where(app => app.isRunning).ToList() is var runningApplications && runningApplications.Count <= 1 ? runningApplications.SingleOrDefault()
        : (await Task.WhenAll(runningApplications.Select(async app => (app, playbackState: await app.fetchPlaybackState()))))
        .OrderByDescending(appWithState => appWithState.playbackState.isPlaying)
        .ThenByDescending(appWithState => appWithState.app.isFocused)
        .ThenByDescending(appWithState => appWithState.playbackState.canPlay)
        .ThenBy(appWithState => appWithState.app.priority)
        .First()
        .app;

    /// <inheritdoc />
    public void Dispose() {
        keyboardShortcuts?.Dispose();
        GC.SuppressFinalize(this);
    }

}