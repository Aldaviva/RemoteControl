using RemoteControl.Remote;
using SimWinInput;

namespace RemoteControl.Applications.Vivaldi;

public interface Vivaldi: ControllableApplication;

public class WebSocketExtensionVivaldiClient(WebSocketDispatcher webSocketDispatcher, ILogger<WebSocketExtensionVivaldiClient> logger): AbstractControllableApplication, Vivaldi {

    protected override string windowClassName { get; } = "Chrome_WidgetWin_1";
    protected override string? processBaseName { get; } = "vivaldi";
    public override int priority { get; } = 3;
    public override string name { get; } = "Vivaldi";

    public override async Task<PlaybackState> fetchPlaybackState() {
        try {
            return (await webSocketDispatcher.sendCommandToMostRecentActiveConnection(new FetchPlaybackState())).playbackState;
        } catch (NoBrowserConnected e) {
            logger.LogWarning(e, "No extension connected while fetching playback state from Vivaldi");
            return new PlaybackState(false, false);
        }
    }

    public override async Task sendButtonPress(RemoteControlButton button) {
        try {
            Website website = (await webSocketDispatcher.sendCommandToMostRecentActiveConnection(new PressButton(button))).website;

            if (button == RemoteControlButton.BAND && isFocused) {
                Keys? fullscreenKey = website is Website.YOUTUBE or Website.TWITCH or Website.CBC or Website.VIMEO ? Keys.F : null;

                if (fullscreenKey != null) {
                    // content script has already blurred the page, so this key press shouldn't go into a text box
                    SimKeyboard.Press((byte) fullscreenKey);
                }
            }
        } catch (NoBrowserConnected e) {
            logger.LogWarning(e, "No extension connected while sending {button} button press to Vivaldi", button);
        }
    }

}