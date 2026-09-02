using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public sealed class PurchasingDashboardSnapshotCacheTests
{
    private static PurchasingDashboardFilter Filter(int year = 2026) =>
        new(new DateTime(year, 1, 1), new DateTime(year, 12, 31));

    [Fact]
    public async Task IdenticalConcurrentFilters_AreCalculatedOnlyOnce()
    {
        var cache = new PurchasingDashboardSnapshotCache();
        var calls = 0;
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<PurchasingDashboardLiveState> Factory(CancellationToken _)
        {
            Interlocked.Increment(ref calls);
            await release.Task;
            return new PurchasingDashboardLiveState { PurchaseOrderCount = 42 };
        }

        var first = cache.GetOrCreateAsync(Filter(), Factory);
        var second = cache.GetOrCreateAsync(Filter(), Factory);
        release.SetResult();

        var states = await Task.WhenAll(first, second);

        Assert.Equal(1, calls);
        Assert.Same(states[0], states[1]);
        Assert.Equal(42, states[0].PurchaseOrderCount);
    }

    [Fact]
    public async Task CachedFilter_IsReusedUntilCacheIsCleared()
    {
        var cache = new PurchasingDashboardSnapshotCache();
        var calls = 0;
        Task<PurchasingDashboardLiveState> Factory(CancellationToken _)
            => Task.FromResult(new PurchasingDashboardLiveState { PurchaseOrderCount = ++calls });

        var first = await cache.GetOrCreateAsync(Filter(), Factory);
        var cached = await cache.GetOrCreateAsync(Filter(), Factory);
        cache.Clear();
        var refreshed = await cache.GetOrCreateAsync(Filter(), Factory);

        Assert.Same(first, cached);
        Assert.NotSame(first, refreshed);
        Assert.Equal(2, calls);
        Assert.Equal(2, refreshed.PurchaseOrderCount);
    }

    [Fact]
    public async Task DifferentFilters_HaveIndependentSnapshots()
    {
        var cache = new PurchasingDashboardSnapshotCache();
        var calls = 0;
        Task<PurchasingDashboardLiveState> Factory(CancellationToken _)
            => Task.FromResult(new PurchasingDashboardLiveState { PurchaseOrderCount = ++calls });

        var first = await cache.GetOrCreateAsync(Filter(2025), Factory);
        var second = await cache.GetOrCreateAsync(Filter(2026), Factory);

        Assert.Equal(2, calls);
        Assert.NotSame(first, second);
    }

    [Fact]
    public async Task Clear_DuringRunningLoad_DoesNotPublishStaleSnapshotToNewGeneration()
    {
        var cache = new PurchasingDashboardSnapshotCache();
        var calls = 0;
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<PurchasingDashboardLiveState> Factory(CancellationToken _)
        {
            var call = Interlocked.Increment(ref calls);
            if (call == 1)
                await releaseFirst.Task;
            return new PurchasingDashboardLiveState { PurchaseOrderCount = call };
        }

        var staleLoad = cache.GetOrCreateAsync(Filter(), Factory);
        cache.Clear();
        var fresh = await cache.GetOrCreateAsync(Filter(), Factory);
        releaseFirst.SetResult();
        var stale = await staleLoad;
        var cached = await cache.GetOrCreateAsync(Filter(), Factory);

        Assert.Equal(1, stale.PurchaseOrderCount);
        Assert.Equal(2, fresh.PurchaseOrderCount);
        Assert.Same(fresh, cached);
        Assert.Equal(2, calls);
    }
}
