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

    protected bool isStale => cacheDurationAfterWrite != default && timeSinceLastCacheWrite.Elapsed > cacheDurationAfterWrite;

    /// <inheritdoc />
    public void Dispose() {
        timeSinceLastCacheWrite.Stop();
        GC.SuppressFinalize(this);
    }

}