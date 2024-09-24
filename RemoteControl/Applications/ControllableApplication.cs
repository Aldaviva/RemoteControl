using RemoteControl.Remote;

namespace RemoteControl.Applications;

public interface ControllableApplication {

    /// <summary>
    /// Lower numbers are higher priority
    /// </summary>
    int priority { get; }

    string name { get; }

    bool isRunning { get; }

    bool isFocused { get; }

    Task<PlaybackState> fetchPlaybackState();

    Task sendButtonPress(RemoteControlButton button);

}