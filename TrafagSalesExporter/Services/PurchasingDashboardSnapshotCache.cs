using System.Collections.Concurrent;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Teilt den fertig berechneten Einkauf-Dashboardzustand zwischen Blazor-Circuits.
///
/// Ein Zustand kostet produktiv rund neun bis elf Sekunden und besteht aus vielen
/// sequenziellen Aggregationen ueber denselben Einkaufscache. Er ist nach dem Aufbau
/// read-only: die Razor-Komponente liest Listen und Kennzahlen, veraendert sie aber nicht.
/// Deshalb darf derselbe Snapshot fuer identische Filter geteilt werden.
/// </summary>
public sealed class PurchasingDashboardSnapshotCache
{
    internal static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);
    private const int MaxEntries = 32;

    private readonly ConcurrentDictionary<CacheKey, CacheEntry> _entries = new();
    private readonly ConcurrentDictionary<CacheKey, Lazy<Task<PurchasingDashboardLiveState>>> _loads = new();
    private long _generation;

    public async Task<PurchasingDashboardLiveState> GetOrCreateAsync(
        PurchasingDashboardFilter filter,
        Func<CancellationToken, Task<PurchasingDashboardLiveState>> factory,
        CancellationToken cancellationToken = default)
    {
        var key = CacheKey.From(filter, Volatile.Read(ref _generation));
        var now = DateTime.UtcNow;
        if (_entries.TryGetValue(key, out var cached) && cached.ExpiresAtUtc > now)
            return cached.State;

        _entries.TryRemove(key, out _);
        var lazy = _loads.GetOrAdd(
            key,
            _ => new Lazy<Task<PurchasingDashboardLiveState>>(
                () => factory(CancellationToken.None),
                LazyThreadSafetyMode.ExecutionAndPublication));
        var load = lazy.Value;

        try
        {
            // Ein abgebrochener Browseraufruf beendet nicht die gemeinsame Berechnung fuer
            // andere Benutzer. Nur das Warten dieses Aufrufers wird abgebrochen.
            var state = await load.WaitAsync(cancellationToken);
            _entries[key] = new CacheEntry(state, DateTime.UtcNow.Add(Lifetime));
            TrimIfNeeded();
            return state;
        }
        catch
        {
            if (load.IsFaulted || load.IsCanceled)
                _entries.TryRemove(key, out _);
            throw;
        }
        finally
        {
            if (load.IsCompleted)
                _loads.TryRemove(new KeyValuePair<CacheKey, Lazy<Task<PurchasingDashboardLiveState>>>(key, lazy));
        }
    }

    /// <summary>Nach einem erfolgreichen Full-/Delta-Lauf sind alle Filter-Snapshots veraltet.</summary>
    public void Clear()
    {
        // Auch bereits laufende Berechnungen gehoeren zur alten Generation. Sie duerfen
        // nach dem Refresh fertig werden, werden von neuen Aufrufen aber nicht mehr benutzt.
        Interlocked.Increment(ref _generation);
        _entries.Clear();
    }

    private void TrimIfNeeded()
    {
        if (_entries.Count <= MaxEntries)
            return;

        var now = DateTime.UtcNow;
        foreach (var pair in _entries.Where(pair => pair.Value.ExpiresAtUtc <= now))
            _entries.TryRemove(pair.Key, out _);

        if (_entries.Count > MaxEntries)
            _entries.Clear();
    }

    private sealed record CacheEntry(PurchasingDashboardLiveState State, DateTime ExpiresAtUtc);

    private readonly record struct CacheKey(
        long Generation,
        DateTime FromDate,
        DateTime ToDate,
        bool ExcludeDeletedItems,
        bool OrdersOnly,
        bool ExcludeEndDelivered)
    {
        public static CacheKey From(PurchasingDashboardFilter filter, long generation) => new(
            generation,
            filter.FromDate.Date,
            filter.ToDate.Date,
            filter.ExcludeDeletedItems,
            filter.OrdersOnly,
            filter.ExcludeEndDelivered);
    }
}
