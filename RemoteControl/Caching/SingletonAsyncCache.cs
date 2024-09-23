namespace RemoteControl.Caching;

public class SingletonAsyncCache<T>(Func<ValueTask<T?>> generator, TimeSpan cacheDurationAfterWrite = default): AbstractSingletonCache<T>(cacheDurationAfterWrite) {

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