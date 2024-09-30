using ManagedWinapi.Windows;
using RemoteControl.Caching;
using RemoteControl.Remote;
using Unfucked;

namespace RemoteControl.Applications;

public abstract class AbstractControllableApplication: ControllableApplication {

    protected static readonly TimeSpan CACHE_DURATION = TimeSpan.FromSeconds(2);

    private readonly SingletonCache<SystemWindow>  foregroundWindow = SingletonCache<SystemWindow>.create(() => SystemWindow.ForegroundWindow, CACHE_DURATION);
    private readonly SingletonCache<SystemWindow?> windowCache;
    protected SystemWindow? appWindow => windowCache.value;

    protected AbstractControllableApplication() {
        windowCache = SingletonCache<SystemWindow?>.create(() => SystemWindow.FilterToplevelWindows(isApplicationWindow).FirstOrDefault(), CACHE_DURATION);
    }

    protected virtual bool isApplicationWindow(SystemWindow window) =>
        window.ClassName == windowClassName && (processBaseName is not { } exeName || exeName.Equals(window.GetProcessExecutableBasename(), StringComparison.OrdinalIgnoreCase));

    protected abstract string windowClassName { get; }
    protected virtual string? processBaseName { get; } = null;

    /// <inheritdoc />
    public abstract ApplicationPriority priority { get; }

    /// <inheritdoc />
    public abstract string name { get; }

    /// <inheritdoc />
    public bool isRunning => windowCache.value?.ClassName != null;

    /// <inheritdoc />
    public bool isFocused => windowCache.value == foregroundWindow.value;

    /// <inheritdoc />
    public abstract Task<PlaybackState> fetchPlaybackState();

    /// <inheritdoc />
    public abstract Task sendButtonPress(RemoteControlButton button);

    protected virtual void Dispose(bool disposing) {
        if (disposing) {
            windowCache.Dispose();
        }
    }

    /// <inheritdoc />
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

}