using System.Globalization;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Liest unser Geschaeft fuer die Weltlage (2026-10-05): Verkauf aus <see cref="SalesDataService"/> (Finance-Regeln, CHF),
/// Einkauf aus dem Einkaufscache (Bestellwert 12 Monate je Lieferland, offene Bestellungen, Rohstoffe per Stichwort),
/// Kurse aus der Kurstabelle des Cockpits. Nur lesend, 30 Minuten gemerkt.
/// </summary>
public sealed class WorldImpactService
{
    // Gleiche Bewertung wie PurchasingDashboardService.ChfValueSql (EKKO.Wkurs, CHF unveraendert).
    private const string ChfValue = @"(CASE
        WHEN COALESCE(k.Waers, '') IN ('', 'CHF') THEN CAST(p.Netwr AS REAL)
        WHEN CAST(k.Wkurs AS REAL) > 0 THEN CAST(p.Netwr AS REAL) * CAST(k.Wkurs AS REAL)
        WHEN CAST(k.Wkurs AS REAL) < 0 THEN CAST(p.Netwr AS REAL) / (-CAST(k.Wkurs AS REAL))
        ELSE CAST(p.Netwr AS REAL) END)";

    private readonly WorldDataService _world;
    private readonly SalesDataService _sales;
    private readonly ICurrencyExchangeRateService _fx;
    private readonly ILogger<WorldImpactService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private (DateTime At, WorldExposure Value)? _cache;

    public WorldImpactService(WorldDataService world, SalesDataService sales, ICurrencyExchangeRateService fx, ILogger<WorldImpactService> logger)
    {
        _world = world;
        _sales = sales;
        _fx = fx;
        _logger = logger;
    }

    public static string Iso(string code) => code.Trim().ToUpperInvariant() switch { "UK" => "GB", "EL" => "GR", var c => c };

    public async Task<(WorldSnapshot Snapshot, WorldExposure Exposure, IReadOnlyList<WorldImpact> Impacts)> GetAsync(CancellationToken ct = default)
    {
        var snap = await _world.GetAsync(ct);
        var exposure = WithEcbChanges(await ExposureAsync(ct), snap);
        return (snap, exposure, WorldImpactAnalytics.Compute(snap, exposure, DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    public async Task<WorldExposure> ExposureAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (_cache is { } c && DateTime.UtcNow - c.At < TimeSpan.FromMinutes(30))
                return c.Value;
            var value = await LoadAsync(ct);
            _cache = (DateTime.UtcNow, value);
            return value;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<WorldExposure> LoadAsync(CancellationToken ct)
    {
        var salesByCountry = new Dictionary<string, decimal>();
        var siteByCountry = new Dictionary<string, decimal>();
        var net = new Dictionary<string, decimal>();
        string? salesError = null, purchasingError = null;
        try
        {
            var data = await _sales.GetAsync();
            if (data.Error is not null) salesError = data.Error;
            var from = data.ReferenceEnd.AddMonths(-12);
            foreach (var f in data.Facts.Where(f => f.Date >= from && f.Date < data.ReferenceEnd))
            {
                if (f.CustomerCountry.Length == 2) Add(salesByCountry, Iso(f.CustomerCountry), f.ValueChf);
                if (f.CountryKey.Length == 2) Add(siteByCountry, Iso(f.CountryKey), f.ValueChf);
                if (f.Currency.Length == 3 && f.Currency != "CHF") Add(net, f.Currency, f.ValueChf);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Weltlage: Verkauf nicht lesbar");
            salesError = ex.GetBaseException().Message;
        }

        var purchaseByCountry = new Dictionary<string, decimal>();
        var openByCountry = new Dictionary<string, decimal>();
        var commodity = new Dictionary<string, decimal>();
        try
        {
            var since = DateTime.Today.AddMonths(-12).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var rows = await _world.QueryAsync($@"
SELECT COALESCE(k.SupplierCountry, ''), COALESCE(k.Waers, ''), p.Matkl, p.Txz01, COALESCE(p.Elikz, ''), SUM({ChfValue})
FROM PurchasingEkpoCache p JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
WHERE k.Bedat >= $s AND COALESCE(p.Loekz, '') = ''
GROUP BY 1, 2, 3, 4, 5", ct, ("$s", since));
            foreach (var r in rows)
            {
                var value = Convert.ToDecimal(r[5] ?? 0, CultureInfo.InvariantCulture);
                if (value == 0) continue;
                var country = Iso(Convert.ToString(r[0]) ?? "");
                var currency = (Convert.ToString(r[1]) ?? "").Trim().ToUpperInvariant();
                var matkl = Convert.ToString(r[2]) ?? "";
                var text = PurchasingMaterialGroupTextCatalog.Resolve(matkl) + " " + (Convert.ToString(r[3]) ?? "");
                if (country.Length == 2) Add(purchaseByCountry, country, value);
                if (country.Length == 2 && (Convert.ToString(r[4]) ?? "").Length == 0) Add(openByCountry, country, value);
                if (currency.Length == 3 && currency != "CHF") Add(net, currency, -value);
                if (WorldImpactAnalytics.CommodityOf(text) is { } key) Add(commodity, key, value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Weltlage: Einkauf nicht lesbar");
            purchasingError = ex.GetBaseException().Message;
        }

        var change = new Dictionary<string, double>();
        foreach (var cur in net.Keys)
        {
            var now = _fx.ResolveRate(cur, "CHF", DateTime.Today);
            var before = _fx.ResolveRate(cur, "CHF", DateTime.Today.AddDays(-90));
            if (now is > 0 && before is > 0)
                change[cur] = (double)((now.Value - before.Value) / before.Value * 100m);
        }

        return new WorldExposure
        {
            SalesByCountry = salesByCountry,
            SiteByCountry = siteByCountry,
            PurchaseByCountry = purchaseByCountry,
            OpenOrdersByCountry = openByCountry,
            PurchaseByCommodity = commodity,
            NetByCurrency = net,
            CurrencyChange90 = change,
            SalesError = salesError,
            PurchasingError = purchasingError
        };
    }

    /// <summary>Kursveraenderung aus der EZB-90-Tage-Reihe; die Kurstabelle des Cockpits nur, wenn die Reihe fehlt.</summary>
    internal static WorldExposure WithEcbChanges(WorldExposure e, WorldSnapshot snap)
    {
        if (snap.Currencies.Count == 0) return e;
        var perEur = snap.Currencies.ToDictionary(s => s.Key, s => (IReadOnlyList<(DateOnly, double)>)s.Points.Select(p => (p.Date, p.Value)).ToList(), StringComparer.OrdinalIgnoreCase);
        var change = new Dictionary<string, double>(e.CurrencyChange90);
        foreach (var cur in e.NetByCurrency.Keys)
            if (WorldParsers.ChangeAgainstChf(perEur, cur) is { } c)
                change[cur] = c;
        return new WorldExposure
        {
            SalesByCountry = e.SalesByCountry, SiteByCountry = e.SiteByCountry, PurchaseByCountry = e.PurchaseByCountry,
            OpenOrdersByCountry = e.OpenOrdersByCountry, PurchaseByCommodity = e.PurchaseByCommodity, NetByCurrency = e.NetByCurrency,
            CurrencyChange90 = change, SalesError = e.SalesError, PurchasingError = e.PurchasingError
        };
    }

    private static void Add<TKey>(Dictionary<TKey, decimal> d, TKey key, decimal value) where TKey : notnull
        => d[key] = d.TryGetValue(key, out var v) ? v + value : value;
}
