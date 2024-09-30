using Gma.System.MouseKeyHook;
using RemoteControl.Applications;
using SimWinInput;

namespace RemoteControl.Remote;

public interface InfraredListener: IDisposable {

    void listen();

}

public class VirtualKeyboardInfraredListener(ILogger<VirtualKeyboardInfraredListener> logger, IEnumerable<ControllableApplication> allApplications): InfraredListener {

    private IKeyboardMouseEvents? keyboardShortcuts;

    public void listen() {
        if (keyboardShortcuts == null) {
            keyboardShortcuts         =  Hook.GlobalEvents();
            keyboardShortcuts.KeyDown += onKeyDown;
            logger.LogDebug("Listening for infrared remote control button presses");
        }
    }

    private void onKeyDown(object? _, KeyEventArgs args) {
        // Ctrl is also part of what Flirc types, but I don't care if Ctrl is pressed here, because as the first keystroke from the Flirc, Ctrl gets eaten by the screensaver if and only if it was running, avoiding the need to press the remote control button twice if the screensaver was running.
        RemoteControlButton? remoteControlButton = args is { Shift: true, Alt: true, KeyCode: var keyCode } ? keyCode switch {
            Keys.F5  => RemoteControlButton.CHANNEL_UP,
            Keys.F6  => RemoteControlButton.CHANNEL_DOWN,
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
            // if the application wants to press a key, don't let it conflict with the keys that are already held down that triggered this action in the first place
            SimKeyboard.KeyUp((byte) Keys.ControlKey);
            SimKeyboard.KeyUp((byte) Keys.ShiftKey);
            SimKeyboard.KeyUp((byte) Keys.Menu);

            onPressRemoteControlButton(remoteControlButton.Value);
        }
    }

    private async void onPressRemoteControlButton(RemoteControlButton button) {
        if (await getTargetApplication() is { } targetApplication) {
            try {
                logger.LogDebug("Sending {button} to {appName}", button, targetApplication.name);
                await targetApplication.sendButtonPress(button);
            } catch (Exception e) when (e is not OutOfMemoryException) {
                logger.LogError(e, "Uncaught exception while sending {button} to {app}", button, targetApplication.name);
            }
        } else {
            logger.LogDebug("No running application to send {button} to, ignoring it", button);
        }
    }

    // ReSharper disable once MergeIntoPattern - variable must be used even if the subsequent check fails, so it can't be in a pattern because the variable scope is too small
    /// <summary>
    /// <para>When a remote control button is pressed, decide which application should receive the input based on this priority list.</para>
    /// <para>Running, ties broken by Playing, ties broken by Focused, ties broken by Playable, ties broken by Priority</para>
    /// </summary>
    /// <returns>The application to send the remote control button press to, or <c>null</c> if no suitable application is running</returns>
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
        if (keyboardShortcuts != null) {
            keyboardShortcuts.KeyDown -= onKeyDown;
            keyboardShortcuts.Dispose();
            keyboardShortcuts = null;
        }
        GC.SuppressFinalize(this);
    }

}