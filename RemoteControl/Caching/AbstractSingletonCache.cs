using System.Diagnostics;

namespace RemoteControl.Caching;

public abstract class AbstractSingletonCache<T>(TimeSpan cacheDurationAfterWrite = default): IDisposable {

    protected readonly SemaphoreSlim mutex                   = new(1);
    private readonly   Stopwatch     timeSinceLastCacheWrite = new();

    protected T? cached;

    // subclasses call this inside mutex
    protected void setValue(T? newValue) {
        cached = newValue;
        timeSinceLastCacheWrite.Restart();
    }

    protected bool isStale => !timeSinceLastCacheWrite.IsRunning                                                    // was cleared
        || (cacheDurationAfterWrite != TimeSpan.Zero && timeSinceLastCacheWrite.Elapsed > cacheDurationAfterWrite); // is old

    public void clear() {
        timeSinceLastCacheWrite.Stop();
    }

    /// <inheritdoc />
    public void Dispose() {
        mutex.Dispose();
        GC.SuppressFinalize(this);
    }

}