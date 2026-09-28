using System.Collections.Concurrent;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Teilt den fertig berechneten Einkauf-Dashboardzustand zwischen Blazor-Circuits.
///
/// Ein Zustand besteht aus rund 55 sequenziellen Aggregationen ueber denselben Einkaufscache.
/// Gemessen am 2026-09-28: lokal rund 12 Sekunden, auf dem Server 98,6 Sekunden - ohne
/// einzelnen Ausreisser, jede Abfrage kostet 0,2 bis 0,8 Sekunden lokal. Er ist nach dem Aufbau
/// read-only: die Razor-Komponente liest Listen und Kennzahlen, veraendert sie aber nicht.
/// Deshalb darf derselbe Snapshot fuer identische Filter geteilt werden.
///
/// SEIT 2026-09-28 LIEFERT EIN ABGELAUFENER EINTRAG SOFORT (stale-while-revalidate): der alte
/// Stand wird zurueckgegeben und im Hintergrund neu berechnet. Vorher wartete der erste
/// Nutzer nach Ablauf die volle Berechnung ab - auf dem Server anderthalb Minuten. Warten muss
/// jetzt nur noch, wer die allererste Berechnung eines Filters erwischt; die Standardansicht
/// waermt <see cref="TimerBackgroundService"/> vor. Nach <see cref="Clear"/> (neue Daten aus
/// einem Einkauf-Lauf) wird bewusst NICHT der alte Stand geliefert, sonst saehe man nach dem
/// Lauf noch die Zahlen von vorher.
/// </summary>
public sealed class PurchasingDashboardSnapshotCache
{
    /// <summary>
    /// Wie lange ein Stand als frisch gilt. 60 statt frueher 15 Minuten: die Daten aendern sich
    /// nur durch einen Einkauf-Lauf, und der leert den Cache ohnehin. Die Lebensdauer deckt nur
    /// noch die tagesabhaengigen Werte (ueberfaellig, faellig in 7 Tagen) ab, und jede
    /// Neuberechnung kostet auf dem Server anderthalb Minuten Rechenzeit.
    /// </summary>
    internal static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(60);
    private const int MaxEntries = 32;

    private readonly ConcurrentDictionary<CacheKey, CacheEntry> _entries = new();
    private readonly ConcurrentDictionary<CacheKey, Lazy<Task<PurchasingDashboardLiveState>>> _loads = new();
    private readonly TimeProvider _time;
    private long _generation;

    public PurchasingDashboardSnapshotCache(TimeProvider? timeProvider = null)
    {
        _time = timeProvider ?? TimeProvider.System;
    }

    public async Task<PurchasingDashboardLiveState> GetOrCreateAsync(
        PurchasingDashboardFilter filter,
        Func<CancellationToken, Task<PurchasingDashboardLiveState>> factory,
        CancellationToken cancellationToken = default)
    {
        var key = CacheKey.From(filter, Volatile.Read(ref _generation));
        if (_entries.TryGetValue(key, out var cached))
        {
            if (cached.ExpiresAtUtc <= UtcNow)
                _ = RefreshInBackgroundAsync(key, factory);
            return cached.State;
        }

        var (lazy, load) = StartOrJoinLoad(key, factory);
        try
        {
            // Ein abgebrochener Browseraufruf beendet nicht die gemeinsame Berechnung fuer
            // andere Benutzer. Nur das Warten dieses Aufrufers wird abgebrochen.
            var state = await load.WaitAsync(cancellationToken);
            Publish(key, state);
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

    private DateTime UtcNow => _time.GetUtcNow().UtcDateTime;

    private (Lazy<Task<PurchasingDashboardLiveState>> Lazy, Task<PurchasingDashboardLiveState> Load) StartOrJoinLoad(
        CacheKey key,
        Func<CancellationToken, Task<PurchasingDashboardLiveState>> factory)
    {
        var lazy = _loads.GetOrAdd(
            key,
            _ => new Lazy<Task<PurchasingDashboardLiveState>>(
                () => factory(CancellationToken.None),
                LazyThreadSafetyMode.ExecutionAndPublication));
        return (lazy, lazy.Value);
    }

    /// <summary>
    /// Berechnet einen abgelaufenen Eintrag neu, ohne dass ein Aufrufer wartet. Laeuft fuer
    /// denselben Schluessel schon eine Berechnung, wird keine zweite gestartet. Ein Fehler
    /// laesst den alten Stand stehen; der naechste Aufruf nach Ablauf versucht es erneut.
    /// </summary>
    private async Task RefreshInBackgroundAsync(
        CacheKey key,
        Func<CancellationToken, Task<PurchasingDashboardLiveState>> factory)
    {
        var (lazy, load) = StartOrJoinLoad(key, factory);
        try
        {
            Publish(key, await load);
        }
        catch
        {
            // Bewusst geschluckt: der alte Stand bleibt sichtbar, statt dass eine Hintergrund-
            // berechnung die Seite leert.
        }
        finally
        {
            _loads.TryRemove(new KeyValuePair<CacheKey, Lazy<Task<PurchasingDashboardLiveState>>>(key, lazy));
        }
    }

    private void Publish(CacheKey key, PurchasingDashboardLiveState state)
    {
        // Ein Ergebnis der alten Generation nach Clear() nicht mehr eintragen: es enthielte
        // die Zahlen von vor dem Einkauf-Lauf.
        if (key.Generation != Volatile.Read(ref _generation))
            return;

        _entries[key] = new CacheEntry(state, UtcNow.Add(Lifetime));
        TrimIfNeeded();
    }

    private void TrimIfNeeded()
    {
        if (_entries.Count <= MaxEntries)
            return;

        var now = UtcNow;
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
