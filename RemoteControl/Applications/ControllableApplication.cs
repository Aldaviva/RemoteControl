using RemoteControl.Remote;

namespace RemoteControl.Applications;

public interface ControllableApplication: IDisposable {

    /// <summary>
    /// Lower numbers are higher priority (more important)
    /// </summary>
    ApplicationPriority priority { get; }

    string name { get; }

    bool isRunning { get; }

    bool isFocused { get; }

    Task<PlaybackState> fetchPlaybackState();

    Task sendButtonPress(RemoteControlButton button);

}