using ManagedWinapi.Windows;
using RemoteControl.Caching;
using RemoteControl.Remote;

namespace RemoteControl.Applications;

public abstract class AbstractControllableApplication: ControllableApplication {

    protected static readonly TimeSpan CACHE_DURATION = TimeSpan.FromSeconds(1);

    private readonly SingletonCache<SystemWindow?> windowCache;
    protected SystemWindow? appWindow => windowCache.value;

    protected AbstractControllableApplication() {
        windowCache = new SingletonCache<SystemWindow?>(() => SystemWindow.FilterToplevelWindows(isApplicationWindow).FirstOrDefault(), CACHE_DURATION);
    }

    protected abstract bool isApplicationWindow(SystemWindow window);

    /// <inheritdoc />
    public abstract int priority { get; }

    /// <inheritdoc />
    public abstract string name { get; }

    /// <inheritdoc />
    public bool isRunning => windowCache.value?.ClassName != null;

    /// <inheritdoc />
    public bool isFocused => windowCache.value?.Enabled == true;

    /// <inheritdoc />
    public abstract Task<bool> isPlaying();

    /// <inheritdoc />
    public abstract Task<bool> isPlayable();

    /// <inheritdoc />
    public abstract Task sendButtonPress(RemoteControlButton button);

}