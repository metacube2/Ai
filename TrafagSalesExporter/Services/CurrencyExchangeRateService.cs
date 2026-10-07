using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Liefert Wechselkurse aus der Tabelle <c>CurrencyExchangeRates</c>.
///
/// SEIT 2026-09-28 AUS DEM SPEICHER. Vorher oeffnete jeder Aufruf eine neue Datenbankverbindung
/// und stellte zwei bis sechs Abfragen. Das Management-Cockpit ruft den Kurs fuer jede der rund
/// 110'000 Verkaufszeilen mehrfach ab (Audit-Ledger, Pivot, Konzernmarge); das waren mehrere
/// hunderttausend Abfragen je Seitenaufruf und laut Profil 92 % der Cockpit-Auswertung -
/// lokal rund 34 von 37 Sekunden, auf dem Server entsprechend Minuten.
///
/// Jetzt werden die aktiven Kurse einmal geladen und im Speicher gesucht; jedes Ergebnis wird je
/// (von, nach, Tag) gemerkt. Die Suchregeln sind unveraendert: Code ohne Gross/Klein, Gueltigkeit
/// nach Tag, juengstes ValidFrom zuerst, sonst Umkehrkurs, sonst ueber EUR.
///
/// FRISCHE: Der Stand gilt hoechstens <see cref="SnapshotLifetime"/>. Wer Kurse schreibt, ruft
/// zusaetzlich <see cref="NotifyRatesChanged"/>; dann wird beim naechsten Aufruf sofort neu
/// geladen, auch innerhalb der Frist.
/// </summary>
public class CurrencyExchangeRateService : ICurrencyExchangeRateService
{
    private static readonly Dictionary<string, string> BuiltInCurrencyAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["$"] = "USD",
        ["US$"] = "USD",
        ["USD"] = "USD",
        ["€"] = "EUR",
        ["EUR"] = "EUR",
        ["CHF"] = "CHF",
        ["SFR"] = "CHF",
        ["INR"] = "INR",
        ["RS"] = "INR",
        ["GBP"] = "GBP",
        ["CAD"] = "CAD"
    };

    /// <summary>Hoechstes Alter des Speicherstands, falls ein Schreiber die Meldung vergisst.</summary>
    internal static readonly TimeSpan SnapshotLifetime = TimeSpan.FromSeconds(10);

    // Prozessweit, weil die Schreiber eigene DbContexte nutzen und diese Instanz nicht kennen.
    private static long _ratesVersion;

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly object _loadGate = new();
    private RateSnapshot? _snapshot;

    public CurrencyExchangeRateService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <summary>
    /// Nach jedem Schreiben in <c>CurrencyExchangeRates</c> aufrufen. Der naechste
    /// <see cref="ResolveRate"/> laedt dann neu, statt bis zu zehn Sekunden den alten Stand zu nutzen.
    /// </summary>
    public static void NotifyRatesChanged() => Interlocked.Increment(ref _ratesVersion);

    public decimal? ResolveRate(string fromCurrency, string toCurrency, DateTime? effectiveDate)
    {
        var normalizedFrom = NormalizeCurrencyCode(fromCurrency);
        var normalizedTo = NormalizeCurrencyCode(toCurrency);
        if (string.IsNullOrWhiteSpace(normalizedFrom) || string.IsNullOrWhiteSpace(normalizedTo))
            return null;

        if (string.Equals(normalizedFrom, normalizedTo, StringComparison.OrdinalIgnoreCase))
            return 1m;

        var date = (effectiveDate ?? DateTime.UtcNow).Date;
        var snapshot = GetSnapshot();
        return snapshot.Resolved.GetOrAdd(
            (normalizedFrom, normalizedTo, date),
            key => ResolveFromSnapshot(snapshot, key.From, key.To, key.Date));
    }

    /// <summary>
    /// Budgetkurs des Jahres: nur Kurse mit Notiz „Budget &lt;Jahr&gt;", direkt, umgekehrt oder ueber
    /// CHF. Ein juengerer Tageskurs (etwa der EZB-Kurs vom 2026-04-16) ueberlagert ihn nicht; fehlt
    /// das Budget einer Waehrung, bleibt der Wert leer statt still auf einen Tageskurs auszuweichen;
    /// nur ein Jahr ganz ohne Budgetkurse faellt auf den Kurs zum 31.12. zurueck.
    /// </summary>
    public decimal? ResolveBudgetRate(string fromCurrency, string toCurrency, int year)
    {
        var from = NormalizeCurrencyCode(fromCurrency);
        var to = NormalizeCurrencyCode(toCurrency);
        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
            return null;
        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
            return 1m;

        var snapshot = GetSnapshot();
        return snapshot.Budget.GetOrAdd((from, to, year), key =>
        {
            var notes = $"Budget {key.Year}";
            decimal? Direct(string a, string b)
            {
                if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
                    return 1m;
                if (snapshot.Rates.TryGetValue((a, b), out var direct) && direct.FirstOrDefault(r => r.Notes == notes) is { } d)
                    return d.Rate;
                if (snapshot.Rates.TryGetValue((b, a), out var inverse) && inverse.FirstOrDefault(r => r.Notes == notes) is { Rate: not 0m } i)
                    return 1m / i.Rate;
                return null;
            }

            var rate = Direct(key.From, key.To);
            if (rate.HasValue)
                return rate;
            var fromChf = Direct(key.From, "CHF");
            var toChf = Direct(key.To, "CHF");
            if (fromChf.HasValue && toChf is { } t && t != 0m)
                return fromChf.Value / t;
            // Rueckfall nur, wenn es fuer dieses Jahr gar keinen Budgetkurs gibt: Kurs zum 31.12.
            // Gibt es Budgetkurse, fehlt aber diese Waehrung, bleibt der Wert leer (sichtbar).
            var anyBudget = snapshot.Rates.Values.Any(list => list.Any(r => r.Notes == notes));
            return anyBudget ? null : ResolveFromSnapshot(snapshot, key.From, key.To, new DateTime(key.Year, 12, 31));
        });
    }

    public string NormalizeCurrencyCode(string? currencyCode)
    {
        var normalized = currencyCode?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        return BuiltInCurrencyAliases.TryGetValue(normalized, out var mapped)
            ? mapped
            : normalized.ToUpperInvariant();
    }

    private RateSnapshot GetSnapshot()
    {
        var version = Volatile.Read(ref _ratesVersion);
        var current = _snapshot;
        if (IsUsable(current, version))
            return current!;

        lock (_loadGate)
        {
            current = _snapshot;
            if (IsUsable(current, version))
                return current!;

            using var db = _dbFactory.CreateDbContext();
            var rates = db.CurrencyExchangeRates
                .AsNoTracking()
                .Where(x => x.IsActive)
                .ToList()
                .GroupBy(x => (From: x.FromCurrency.ToUpperInvariant(), To: x.ToCurrency.ToUpperInvariant()))
                .ToDictionary(
                    group => group.Key,
                    // Juengstes ValidFrom zuerst, wie vorher OrderByDescending(ValidFrom).First().
                    group => group.OrderByDescending(x => x.ValidFrom).ToArray());

            current = new RateSnapshot(DateTime.UtcNow, version, rates);
            _snapshot = current;
            return current;
        }
    }

    private static bool IsUsable(RateSnapshot? snapshot, long version)
        => snapshot is not null &&
           snapshot.Version == version &&
           DateTime.UtcNow - snapshot.LoadedAtUtc < SnapshotLifetime;

    private static decimal? ResolveFromSnapshot(RateSnapshot snapshot, string from, string to, DateTime date)
    {
        var direct = FindRate(snapshot, from, to, date);
        if (direct is not null)
            return direct.Rate;

        var inverse = FindRate(snapshot, to, from, date);
        if (inverse is not null && inverse.Rate != 0m)
            return 1m / inverse.Rate;

        var fromToEur = ResolveDirectOrInverseRate(snapshot, from, "EUR", date);
        var eurToTarget = ResolveDirectOrInverseRate(snapshot, "EUR", to, date);
        if (fromToEur.HasValue && eurToTarget.HasValue)
            return fromToEur.Value * eurToTarget.Value;

        return null;
    }

    private static decimal? ResolveDirectOrInverseRate(RateSnapshot snapshot, string fromCurrency, string toCurrency, DateTime date)
    {
        if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase))
            return 1m;

        var direct = FindRate(snapshot, fromCurrency, toCurrency, date);
        if (direct is not null)
            return direct.Rate;

        var inverse = FindRate(snapshot, toCurrency, fromCurrency, date);
        if (inverse is not null && inverse.Rate != 0m)
            return 1m / inverse.Rate;

        return null;
    }

    private static CurrencyExchangeRate? FindRate(RateSnapshot snapshot, string from, string to, DateTime date)
    {
        if (!snapshot.Rates.TryGetValue((from, to), out var candidates))
            return null;

        foreach (var rate in candidates)
        {
            if (rate.ValidFrom.Date <= date && (!rate.ValidTo.HasValue || rate.ValidTo.Value.Date >= date))
                return rate;
        }

        return null;
    }

    private sealed class RateSnapshot(
        DateTime loadedAtUtc,
        long version,
        Dictionary<(string From, string To), CurrencyExchangeRate[]> rates)
    {
        public DateTime LoadedAtUtc { get; } = loadedAtUtc;
        public long Version { get; } = version;
        public ConcurrentDictionary<(string From, string To, int Year), decimal?> Budget { get; } = new();
        public Dictionary<(string From, string To), CurrencyExchangeRate[]> Rates { get; } = rates;
        public ConcurrentDictionary<(string From, string To, DateTime Date), decimal?> Resolved { get; } = new();
    }
}
