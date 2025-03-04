using ManagedWinapi.Windows;
using RemoteControl.Remote;
using SimWinInput;

namespace RemoteControl.Applications.Vivaldi;

public interface Vivaldi: ControllableApplication;

public class VivaldiWebSocketExtensionClient(WebSocketDispatcher webSocketDispatcher, ILogger<VivaldiWebSocketExtensionClient> logger): AbstractControllableApplication, Vivaldi {

    protected override string windowClassName { get; } = "Chrome_WidgetWin_1";
    protected override string? executableFilename { get; } = "vivaldi.exe";
    public override ApplicationPriority priority { get; } = ApplicationPriority.VIVALDI;
    public override string name { get; } = "Vivaldi";

    public override async Task<PlaybackState> fetchPlaybackState() {
        try {
            return (await webSocketDispatcher.sendCommandToMostRecentActiveConnection(new FetchPlaybackState())).playbackState;
        } catch (UnsupportedWebsite e) {
            logger.LogInformation("Browser extension does not support fetching the playback state from {url}", e.url);
        } catch (NoBrowserConnected e) {
            logger.LogWarning(e, "No extension connected while fetching playback state from Vivaldi");
        } catch (BrowserExtensionException e) {
            handleBrowserExtensionException(e);
        }
        return new PlaybackState(false, false);
    }

    public override async Task sendButtonPress(RemoteControlButton button) {
        try {
            ButtonPressed response = await webSocketDispatcher.sendCommandToMostRecentActiveConnection(new PressButton(button));

            switch (button) {
                case RemoteControlButton.PLAY_PAUSE when !isFullscreen:
                    unminimize();
                    break;
                case RemoteControlButton.MEMORY:
                    if (isFocused) {
                        Keys? fullscreenKey = response.website is Website.YOUTUBE or Website.TWITCH or Website.CBC or Website.VIMEO ? Keys.F : null;

                        if (fullscreenKey != null) {
                            // content script has already blurred the page, so this key press shouldn't go into a text box
                            SimKeyboard.Press((byte) fullscreenKey);
                            logger.LogDebug("Sent {key} to foreground window", fullscreenKey);
                        }
                    } else {
                        logger.LogDebug("Vivaldi was not focused, not pressing key to enter fullscreen");
                    }
                    break;
                default:
                    break;
            }
        } catch (UnsupportedWebsite e) {
            logger.LogInformation("Browser extension does not support pressing the {button} button on {url}", button, e.url);
        } catch (NoBrowserConnected e) {
            logger.LogWarning(e, "No extension connected while sending {button} button press to Vivaldi", button);
        } catch (BrowserExtensionException e) {
            handleBrowserExtensionException(e);
        }
    }

    private bool isFullscreen => (appWindow?.ExtendedStyle & WindowExStyleFlags.WINDOWEDGE) == 0;

    private void handleBrowserExtensionException(BrowserExtensionException ex) {
        try {
            throw ex;
        } catch (UnsupportedCommand e) {
            logger.LogError("Browser extension is too old to support the {cmd} command, please update it", e.name);
        } catch (UnmappedBrowserExtensionException e) {
            logger.LogError(e, "Browser extension returned an exception that does not map to any subclasses of {superclass}", nameof(BrowserExtensionException));
        } catch (BrowserExtensionException e) {
            logger.LogError(e, "Browser extension returned a {exName} exception not handled by this method", e.GetType().Name);
        }
    }

}