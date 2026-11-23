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
        // Seit 2026-10-01: direkt nach dem Lauf kommt der alte Stand, gekennzeichnet, und die
        // Neuberechnung laeuft im Hintergrund. Seit 2026-11-23 wirklich im Hintergrund (Task.Run),
        // deshalb hier auf den neuen Stand warten statt ihn synchron zu erwarten.
        var duringRecalculation = await cache.GetOrCreateAsync(Filter(), Factory);
        var refreshed = duringRecalculation;
        for (var i = 0; i < 100 && ReferenceEquals(refreshed, first); i++)
        {
            await Task.Delay(10);
            refreshed = await cache.GetOrCreateAsync(Filter(), Factory);
        }

        Assert.Same(first, cached);
        Assert.Same(first, duringRecalculation);
        Assert.False(cache.IsPrevious(refreshed));
        Assert.NotSame(first, refreshed);
        Assert.Equal(2, calls);
        Assert.Equal(2, refreshed.PurchaseOrderCount);
    }

    [Fact]
    public async Task AfterClear_PreviousSnapshot_IsMarked_Until_The_New_One_Is_Ready()
    {
        var cache = new PurchasingDashboardSnapshotCache();
        var newData = new TaskCompletionSource<PurchasingDashboardLiveState>();
        var old = await cache.GetOrCreateAsync(Filter(), _ => Task.FromResult(new PurchasingDashboardLiveState { PurchaseOrderCount = 1 }));

        cache.Clear();
        var shown = await cache.GetOrCreateAsync(Filter(), _ => newData.Task);

        Assert.Same(old, shown);
        Assert.True(cache.IsPrevious(shown));

        newData.SetResult(new PurchasingDashboardLiveState { PurchaseOrderCount = 2 });
        // Seit 2026-11-23 laeuft die Neuberechnung wirklich im Hintergrund: auf das Eintragen warten.
        var current = shown;
        for (var i = 0; i < 200 && ReferenceEquals(current, old); i++)
        {
            await Task.Delay(10);
            current = await cache.GetOrCreateAsync(Filter(), _ => newData.Task);
        }

        Assert.Equal(2, current.PurchaseOrderCount);
        Assert.False(cache.IsPrevious(current));
        Assert.False(cache.IsPrevious(old));
    }

    [Fact]
    public async Task Filter_Without_Previous_Snapshot_Still_Waits_After_Clear()
    {
        var cache = new PurchasingDashboardSnapshotCache();
        cache.Clear();

        var state = await cache.GetOrCreateAsync(Filter(), _ => Task.FromResult(new PurchasingDashboardLiveState { PurchaseOrderCount = 7 }));

        Assert.Equal(7, state.PurchaseOrderCount);
        Assert.False(cache.IsPrevious(state));
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

        // Zwei getrennte Fabriken statt eines Zaehlers: seit 2026-11-23 starten die Berechnungen ueber
        // Task.Run, die Reihenfolge der Aufrufe ist damit nicht mehr fest (der Zaehler blockierte sonst).
        async Task<PurchasingDashboardLiveState> StaleFactory(CancellationToken _)
        {
            Interlocked.Increment(ref calls);
            await releaseFirst.Task;
            return new PurchasingDashboardLiveState { PurchaseOrderCount = 1 };
        }

        Task<PurchasingDashboardLiveState> FreshFactory(CancellationToken _)
        {
            Interlocked.Increment(ref calls);
            return Task.FromResult(new PurchasingDashboardLiveState { PurchaseOrderCount = 2 });
        }

        var staleLoad = cache.GetOrCreateAsync(Filter(), StaleFactory);
        cache.Clear();
        var fresh = await cache.GetOrCreateAsync(Filter(), FreshFactory);
        releaseFirst.SetResult();
        var stale = await staleLoad;
        var cached = await cache.GetOrCreateAsync(Filter(), FreshFactory);

        Assert.Equal(1, stale.PurchaseOrderCount);
        Assert.Equal(2, fresh.PurchaseOrderCount);
        Assert.Same(fresh, cached);
        Assert.Equal(2, calls);
    }

    // ---- stale-while-revalidate (2026-09-28): ein abgelaufener Stand wird sofort geliefert ----

    private sealed class ManualTime : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 9, 28, 10, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    [Fact]
    public async Task ExpiredSnapshot_IsReturnedImmediately_AndRefreshedInBackground()
    {
        var time = new ManualTime();
        var cache = new PurchasingDashboardSnapshotCache(time);
        var calls = 0;
        var releaseRefresh = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<PurchasingDashboardLiveState> Factory(CancellationToken _)
        {
            var call = Interlocked.Increment(ref calls);
            if (call == 2)
                await releaseRefresh.Task;
            return new PurchasingDashboardLiveState { PurchaseOrderCount = call };
        }

        var first = await cache.GetOrCreateAsync(Filter(), Factory);
        time.Now += PurchasingDashboardSnapshotCache.Lifetime + TimeSpan.FromMinutes(1);

        // Die Neuberechnung haengt noch - trotzdem kommt der alte Stand sofort zurueck.
        var stale = await cache.GetOrCreateAsync(Filter(), Factory).WaitAsync(TimeSpan.FromSeconds(5));
        var staleAgain = await cache.GetOrCreateAsync(Filter(), Factory).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Same(first, stale);
        Assert.Same(first, staleAgain);
        // Die Hintergrundberechnung startet seit 2026-11-23 ueber Task.Run: kurz auf ihren Start warten.
        for (var i = 0; i < 200 && Volatile.Read(ref calls) < 2; i++)
            await Task.Delay(10);
        Assert.Equal(2, calls); // nur EINE Hintergrundberechnung, obwohl zweimal gefragt

        releaseRefresh.SetResult();
        PurchasingDashboardLiveState refreshed = first;
        for (var i = 0; i < 50 && ReferenceEquals(refreshed, first); i++)
        {
            await Task.Delay(20);
            refreshed = await cache.GetOrCreateAsync(Filter(), Factory);
        }

        Assert.Equal(2, refreshed.PurchaseOrderCount);
        Assert.Equal(2, calls);
    }

    /// <summary>
    /// Befund 2026-11-23: Die echte Berechnung laeuft synchron (SQLite-Aufrufe sind auch als *Async synchron
    /// fertig). Startete der Cache die "Hintergrund"-Neuberechnung direkt aus dem Aufruf heraus, wartete der
    /// Aufrufer die ganze Berechnung ab, auf dem Server rund 100 s, jede Stunde nach Ablauf des Stands.
    /// </summary>
    [Fact]
    public async Task ExpiredSnapshot_WithSynchronousFactory_DoesNotBlockTheCaller()
    {
        var time = new ManualTime();
        var cache = new PurchasingDashboardSnapshotCache(time);
        var calls = 0;

        Task<PurchasingDashboardLiveState> SynchronousFactory(CancellationToken _)
        {
            var call = Interlocked.Increment(ref calls);
            if (call == 2)
                Thread.Sleep(TimeSpan.FromSeconds(2));
            return Task.FromResult(new PurchasingDashboardLiveState { PurchaseOrderCount = call });
        }

        var first = await cache.GetOrCreateAsync(Filter(), SynchronousFactory);
        time.Now += PurchasingDashboardSnapshotCache.Lifetime + TimeSpan.FromMinutes(1);

        var watch = System.Diagnostics.Stopwatch.StartNew();
        var stale = await cache.GetOrCreateAsync(Filter(), SynchronousFactory);
        watch.Stop();

        Assert.Same(first, stale);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(1), $"Aufrufer wartete {watch.Elapsed.TotalSeconds:N1} s");
    }

    [Fact]
    public async Task FailedBackgroundRefresh_KeepsTheOldSnapshot()
    {
        var time = new ManualTime();
        var cache = new PurchasingDashboardSnapshotCache(time);
        var calls = 0;

        Task<PurchasingDashboardLiveState> Factory(CancellationToken _)
            => ++calls == 1
                ? Task.FromResult(new PurchasingDashboardLiveState { PurchaseOrderCount = 1 })
                : Task.FromException<PurchasingDashboardLiveState>(new InvalidOperationException("SAP weg"));

        var first = await cache.GetOrCreateAsync(Filter(), Factory);
        time.Now += PurchasingDashboardSnapshotCache.Lifetime + TimeSpan.FromMinutes(1);

        var stale = await cache.GetOrCreateAsync(Filter(), Factory);
        await Task.Delay(50);
        var afterFailure = await cache.GetOrCreateAsync(Filter(), Factory);

        Assert.Same(first, stale);
        Assert.Same(first, afterFailure);
    }

    [Fact]
    public void DefaultFilter_Matches_The_Page_Default()
    {
        var filter = PurchasingDashboardFilter.Default(new DateTime(2026, 9, 28, 15, 30, 0));

        Assert.Equal(new DateTime(2026, 1, 1), filter.FromDate);
        Assert.Equal(new DateTime(2026, 9, 28), filter.ToDate);

        var nextYear = PurchasingDashboardFilter.Default(new DateTime(2027, 2, 3));
        Assert.Equal(new DateTime(2027, 1, 1), nextYear.FromDate);
        Assert.Equal(new DateTime(2027, 2, 3), nextYear.ToDate);
        Assert.True(filter.ExcludeDeletedItems);
        Assert.True(filter.OrdersOnly);
        Assert.True(filter.ExcludeEndDelivered);
    }
}
