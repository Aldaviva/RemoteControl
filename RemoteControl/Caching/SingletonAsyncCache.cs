namespace RemoteControl.Caching;

public class SingletonAsyncCache<T>: AbstractSingletonCache<T> {

    private readonly Func<ValueTask<T?>> generator;

    internal SingletonAsyncCache(Func<ValueTask<T?>> generator, TimeSpan cacheDurationAfterWrite = default): base(cacheDurationAfterWrite) {
        this.generator = generator;
    }

    public async Task<T?> value() {
        await mutex.WaitAsync();
        try {
            if (isStale) {
                setValue(await generator());
            }
            return cached;
        } finally {
            mutex.Release();
        }
    }

}