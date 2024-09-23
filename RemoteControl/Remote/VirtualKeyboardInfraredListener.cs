using Gma.System.MouseKeyHook;
using RemoteControl.Applications;

namespace RemoteControl.Remote;

public interface InfraredListener: IDisposable {

    void listen();

}

public class VirtualKeyboardInfraredListener(ILogger<VirtualKeyboardInfraredListener> logger, IEnumerable<ControllableApplication> allApplications): InfraredListener {

    private IKeyboardMouseEvents? keyboardShortcuts;

    /// <inheritdoc />
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

    private async Task<ControllableApplication?> getTargetApplication() =>
        (await Task.WhenAll(allApplications.Select(async app => {
            Task<bool> isPlaying  = app.isPlaying();
            Task<bool> isPlayable = app.isPlayable();
            return (app, isPlaying: await isPlaying, isPlayable: await isPlayable, app.isFocused, app.isRunning);
        })))
        .Where(app => app.isRunning)
        .OrderByDescending(app => app.isPlaying)
        .ThenByDescending(app => app.isFocused)
        .ThenByDescending(app => app.isPlayable)
        .ThenBy(app => app.app.priority)
        .Select(app => app.app)
        .FirstOrDefault();

    /// <inheritdoc />
    public void Dispose() {
        keyboardShortcuts?.Dispose();
        GC.SuppressFinalize(this);
    }

}