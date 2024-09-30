using RemoteControl.Caching;

namespace Tests.Caching;

public class SingletonCacheTest {

    private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromMilliseconds(100);

    [Fact]
    public void synchronousInfinite() {
        int                       counter = 0;
        using SingletonCache<int> cache   = SingletonCache<int>.create(() => ++counter);

        cache.value.Should().Be(1);
    }

    [Fact]
    public void synchronousFinite() {
        int                       counter = 0;
        using SingletonCache<int> cache   = SingletonCache<int>.create(() => ++counter, CACHE_DURATION);
        cache.value.Should().Be(1);

        Thread.Sleep(CACHE_DURATION * 2);

        cache.value.Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task asynchronousInfinite() {
        int counter = 0;
        using SingletonAsyncCache<int> cache = SingletonCache<int>.create(async () => {
            await Task.Yield();
            return ++counter;
        });

        (await cache.value()).Should().Be(1);
    }

    [Fact]
    public async Task asynchronousFinite() {
        int counter = 0;
        using SingletonAsyncCache<int> cache = SingletonCache<int>.create(async () => {
            await Task.Yield();
            return ++counter;
        }, CACHE_DURATION);

        (await cache.value()).Should().Be(1);

        await Task.Delay(CACHE_DURATION * 2);

        (await cache.value()).Should().BeGreaterThan(1);
    }

    [Fact]
    public void clearInfinite() {
        int                       counter = 0;
        using SingletonCache<int> cache   = SingletonCache<int>.create(() => ++counter);

        cache.value.Should().Be(1);

        cache.clear();

        cache.value.Should().Be(2);
    }

    [Fact]
    public void clearFinite() {
        int                       counter = 0;
        using SingletonCache<int> cache   = SingletonCache<int>.create(() => ++counter, CACHE_DURATION);

        cache.value.Should().Be(1);

        cache.clear();

        cache.value.Should().Be(2);
    }

}