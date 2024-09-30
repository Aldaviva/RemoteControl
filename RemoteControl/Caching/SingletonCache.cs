namespace RemoteControl.Caching;

public class SingletonCache<T>: AbstractSingletonCache<T> {

    private readonly Func<T?> generator;

    private SingletonCache(Func<T?> generator, TimeSpan cacheDurationAfterWrite = default): base(cacheDurationAfterWrite) {
        this.generator = generator;
    }

    public static SingletonCache<T> create(Func<T?> generator, TimeSpan cacheDurationAfterWrite = default) {
        return new SingletonCache<T>(generator, cacheDurationAfterWrite);
    }

    public static SingletonAsyncCache<T> create(Func<ValueTask<T?>> generator, TimeSpan cacheDurationAfterWrite = default) {
        return new SingletonAsyncCache<T>(generator, cacheDurationAfterWrite);
    }

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