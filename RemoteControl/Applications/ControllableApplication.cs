using RemoteControl.Remote;

namespace RemoteControl.Applications;

public interface ControllableApplication: IDisposable {

    /// <inheritdoc cref="ApplicationPriority" />
    ApplicationPriority priority { get; }

    string name { get; }

    bool isRunning { get; }

    bool isFocused { get; }

    Task<PlaybackState> fetchPlaybackState();

    Task sendButtonPress(RemoteControlButton button);

    bool launch();

}