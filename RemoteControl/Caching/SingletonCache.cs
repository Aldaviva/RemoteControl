namespace RemoteControl.Caching;

public class SingletonCache<T>(Func<T> generator, TimeSpan cacheDurationAfterWrite = default): AbstractSingletonCache<T>(cacheDurationAfterWrite) {

    private readonly Func<T?> generator = generator;

    public T? value {
        get {
            mutex.Wait();
            try {
                if (isStale) {
                    setValue(generator());
                }
                return cached;
            } finally {
                mutex.Release();
            }
        }
    }

}