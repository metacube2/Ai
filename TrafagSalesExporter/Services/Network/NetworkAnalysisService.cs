using System.Globalization;
using System.Text.RegularExpressions;

namespace TrafagSalesExporter.Services;

public sealed record NetworkTargetStatus(
    NetworkTarget Target, NetworkProbeResult? Last, double? Availability24h, double? AvgLatency24h,
    IReadOnlyList<double?> Latency24h, string Ips, bool Encrypted);

public sealed record NetworkHeatCell(DateTime HourUtc, double? Availability);

public sealed record NetworkDayValue(DateOnly Day, double? AvgLatencyMs, int Problems, int Probes);

public sealed record NetworkExportRun(DateTime Timestamp, string Tsc, string Status, string Error, string Cause, double DurationSeconds);

public sealed record NetworkSiteCriticality(
    string Tsc, string Land, string SourceSystem, string Dependency, decimal RevenueChf2025,
    int Runs, int Failures, int NetworkFailures, double? Availability24h);

public sealed record NetworkSubnetStat(string Subnet, int Circuits, int Drops, int Reconnects)
{
    public double DropsPerCircuit => Circuits == 0 ? Drops : (double)Drops / Circuits;
}

/// <summary>
/// Rechnet alles fuer die fuenf Unterreiter aus den gespeicherten Pruefungen und den vorhandenen
/// Tabellen der App (ExportLogs, AppEventLogs, Sites, CentralSalesRecords). Nur lesend.
/// </summary>
public sealed class NetworkAnalysisService
{
    private static readonly Regex NetworkError = new(
        @"timeout|timed out|zeitueberschreitung|zeitüberschreitung|unreachable|nicht erreichbar|connection|verbindung|socket|host|network|netzwerk|dns|refused|reset|ssl|tls|503|502|504",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly NetworkStore _store;
    private readonly NetworkTargetCatalog _catalog;
    private readonly ICurrencyExchangeRateService _fx;

    public NetworkAnalysisService(NetworkStore store, NetworkTargetCatalog catalog, ICurrencyExchangeRateService fx)
    {
        _store = store;
        _catalog = catalog;
        _fx = fx;
    }

    public Task<IReadOnlyList<NetworkTarget>> TargetsAsync(CancellationToken ct = default) => _catalog.LoadAsync(ct);

    public Task<List<NetworkProbeResult>> ResultsAsync(int days, CancellationToken ct = default)
        => _store.LoadProbeResultsAsync(DateTime.UtcNow.AddDays(-days), ct);

    public static IReadOnlyList<NetworkTargetStatus> Status(IReadOnlyList<NetworkTarget> targets, IReadOnlyList<NetworkProbeResult> results, DateTime nowUtc)
    {
        var byTarget = results.GroupBy(r => r.TargetKey).ToDictionary(g => g.Key, g => g.OrderBy(r => r.TimestampUtc).ToList());
        return targets.Select(t =>
        {
            var list = byTarget.TryGetValue(t.Key, out var l) ? l : [];
            var day = list.Where(r => r.TimestampUtc >= nowUtc.AddHours(-24)).ToList();
            var last = list.LastOrDefault();
            var buckets = Enumerable.Range(0, 24).Select(h =>
            {
                var from = nowUtc.AddHours(-24 + h);
                var inHour = day.Where(r => r.TimestampUtc >= from && r.TimestampUtc < from.AddHours(1) && r.LatencyMs.HasValue).ToList();
                return inHour.Count == 0 ? (double?)null : inHour.Average(r => r.LatencyMs!.Value);
            }).ToList();
            var detail = Parse(last?.Detail);
            return new NetworkTargetStatus(t, last,
                day.Count == 0 ? null : day.Count(r => r.Ok) / (double)day.Count,
                day.Any(r => r.Ok && r.LatencyMs.HasValue) ? day.Where(r => r.Ok && r.LatencyMs.HasValue).Average(r => r.LatencyMs!.Value) : null,
                buckets, detail.GetValueOrDefault("ip", ""),
                t.Kind == NetworkProbeKind.Tls || (t.Url?.StartsWith("https", StringComparison.OrdinalIgnoreCase) ?? false));
        }).ToList();
    }

    /// <summary>Verfuegbarkeit je Stunde fuer die letzten <paramref name="days"/> Tage (null = keine Pruefung).</summary>
    public static IReadOnlyList<NetworkHeatCell> Heatmap(string targetKey, IReadOnlyList<NetworkProbeResult> results, DateTime nowUtc, int days)
    {
        var start = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, nowUtc.Hour, 0, 0, DateTimeKind.Utc).AddHours(-days * 24 + 1);
        var own = results.Where(r => r.TargetKey == targetKey && r.TimestampUtc >= start)
            .GroupBy(r => new DateTime(r.TimestampUtc.Year, r.TimestampUtc.Month, r.TimestampUtc.Day, r.TimestampUtc.Hour, 0, 0, DateTimeKind.Utc))
            .ToDictionary(g => g.Key, g => g.Count(r => r.Ok) / (double)g.Count());
        return Enumerable.Range(0, days * 24).Select(i =>
        {
            var hour = start.AddHours(i);
            return new NetworkHeatCell(hour, own.TryGetValue(hour, out var v) ? v : null);
        }).ToList();
    }

    public static IReadOnlyList<NetworkCertificateInfo> Certificates(IReadOnlyList<NetworkTargetStatus> status, DateTime nowUtc)
        => status.Where(s => s.Last is not null)
            .Select(s => (s.Target.Key, d: Parse(s.Last!.Detail)))
            .Where(x => x.d.ContainsKey("notAfter") && DateTime.TryParse(x.d["notAfter"], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out _))
            .Select(x =>
            {
                var notAfter = DateTime.Parse(x.d["notAfter"], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                return new NetworkCertificateInfo(x.Key, x.d.GetValueOrDefault("subject", ""), x.d.GetValueOrDefault("issuer", ""), notAfter, (int)Math.Floor((notAfter - nowUtc).TotalDays));
            })
            .OrderBy(c => c.DaysLeft)
            .ToList();

    /// <summary>SAP-Fruehwarnung: Antwortzeit des Pings je Tag und Warnungen/Fehler der SAP-Abrufe der App.</summary>
    public async Task<IReadOnlyList<NetworkDayValue>> SapTrendAsync(IReadOnlyList<NetworkProbeResult> results, int days, CancellationToken ct = default)
    {
        var from = DateTime.Today.AddDays(-days + 1);
        var problems = (await _store.QueryAsync(@"
SELECT substr(Timestamp, 1, 10), count(*) FROM AppEventLogs
WHERE Timestamp >= $from AND Level IN ('Error','Warning') AND Category IN ('SAP','Purchasing','HR','Logistik','Journal','MaterialUsage')
GROUP BY substr(Timestamp, 1, 10)", new Dictionary<string, object> { ["$from"] = from.ToString("yyyy-MM-dd") }, ct))
            .ToDictionary(r => Convert.ToString(r[0])!, r => Convert.ToInt32(r[1]));
        var sap = results.Where(r => r.TargetKey.StartsWith("sap:", StringComparison.Ordinal)).ToList();
        return Enumerable.Range(0, days).Select(i =>
        {
            var day = DateOnly.FromDateTime(from.AddDays(i));
            var own = sap.Where(r => DateOnly.FromDateTime(r.TimestampUtc.ToLocalTime()) == day).ToList();
            return new NetworkDayValue(day,
                own.Any(r => r.Ok && r.LatencyMs.HasValue) ? own.Where(r => r.Ok && r.LatencyMs.HasValue).Average(r => r.LatencyMs!.Value) : null,
                problems.TryGetValue(day.ToString("yyyy-MM-dd"), out var p) ? p : 0, own.Count);
        }).ToList();
    }

    /// <summary>Exportlaeufe seit Juni mit Einordnung Netz oder Daten.</summary>
    public async Task<IReadOnlyList<NetworkExportRun>> ExportRunsAsync(IReadOnlyList<NetworkTarget> targets, IReadOnlyList<NetworkProbeResult> results, CancellationToken ct = default)
    {
        var rows = await _store.QueryAsync("SELECT Timestamp, TSC, Status, COALESCE(ErrorMessage,''), COALESCE(DurationSeconds,0) FROM ExportLogs ORDER BY Timestamp", null, ct);
        return rows.Select(r =>
        {
            var ts = DateTime.Parse(Convert.ToString(r[0])!, CultureInfo.InvariantCulture);
            var tsc = Convert.ToString(r[1]) ?? "";
            var status = Convert.ToString(r[2]) ?? "";
            var error = Convert.ToString(r[3]) ?? "";
            return new NetworkExportRun(ts, tsc, status, error, Classify(status, error, tsc, ts, targets, results), Convert.ToDouble(r[4], CultureInfo.InvariantCulture));
        }).ToList();
    }

    /// <summary>
    /// Ok, Netz (Meldung nach Netzwerk oder Ziel zu der Zeit nicht erreichbar) oder Daten (alles andere).
    /// </summary>
    internal static string Classify(string status, string error, string tsc, DateTime timestamp,
        IReadOnlyList<NetworkTarget> targets, IReadOnlyList<NetworkProbeResult> results)
    {
        if (!IsFailure(status, error))
            return "ok";
        var target = targets.FirstOrDefault(t => t.Sites?.Contains(tsc, StringComparer.OrdinalIgnoreCase) == true);
        if (target is not null)
        {
            var utc = timestamp.ToUniversalTime();
            var near = results.Where(r => r.TargetKey == target.Key && Math.Abs((r.TimestampUtc - utc).TotalMinutes) <= 15).ToList();
            if (near.Count > 0 && near.Any(r => !r.Ok))
                return "netz";
        }
        return NetworkError.IsMatch(error) ? "netz" : "daten";
    }

    internal static bool IsFailure(string status, string error)
        => !string.IsNullOrWhiteSpace(error) || status.Contains("Fehler", StringComparison.OrdinalIgnoreCase) ||
           status.Contains("Error", StringComparison.OrdinalIgnoreCase) || status.Contains("Fail", StringComparison.OrdinalIgnoreCase);

    /// <summary>Standorte nach Umsatz 2025 in CHF, mit Abhaengigkeit, Exportfehlern und Verfuegbarkeit.</summary>
    public async Task<IReadOnlyList<NetworkSiteCriticality>> CriticalityAsync(
        IReadOnlyList<NetworkTarget> targets, IReadOnlyList<NetworkTargetStatus> status, IReadOnlyList<NetworkExportRun> runs, CancellationToken ct = default)
    {
        var sites = await _store.QueryAsync("SELECT TSC, Land, SourceSystem FROM Sites WHERE IsActive = 1 AND TSC <> 'PURCHASING_SAP'", null, ct);
        var revenue = await _store.QueryAsync(@"
SELECT Tsc, COALESCE(NULLIF(SalesCurrency,''), CompanyCurrency, ''), SUM(CAST(SalesPriceValue AS REAL))
FROM CentralSalesRecords WHERE InvoiceDate >= '2025' AND InvoiceDate < '2026'
GROUP BY Tsc, COALESCE(NULLIF(SalesCurrency,''), CompanyCurrency, '')", null, ct);
        var chf = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in revenue)
        {
            var tsc = Convert.ToString(r[0]) ?? "";
            var currency = _fx.NormalizeCurrencyCode(Convert.ToString(r[1]));
            var value = Convert.ToDecimal(r[2] ?? 0m, CultureInfo.InvariantCulture);
            var rate = currency is "CHF" or "" ? 1m : _fx.ResolveRate(currency, "CHF", new DateTime(2025, 12, 31));
            if (rate is null)
                continue; // ohne Kurs lieber weglassen als falsch umrechnen
            chf[tsc] = chf.GetValueOrDefault(tsc) + value * rate.Value;
        }

        return sites.Select(r =>
        {
            var tsc = Convert.ToString(r[0]) ?? "";
            var revenueKey = tsc == "ZSCHWEIZ" ? "TRCH" : tsc == "TRSE" ? "TRES" : tsc;
            var dep = targets.FirstOrDefault(t => t.Sites?.Contains(tsc, StringComparer.OrdinalIgnoreCase) == true);
            var depStatus = dep is null ? null : status.FirstOrDefault(s => s.Target.Key == dep.Key);
            var own = runs.Where(x => string.Equals(x.Tsc, tsc, StringComparison.OrdinalIgnoreCase)).ToList();
            var rev = chf.GetValueOrDefault(revenueKey) + (tsc == "ZSCHWEIZ" ? chf.GetValueOrDefault("TRAT") : 0m);
            return new NetworkSiteCriticality(tsc, Convert.ToString(r[1]) ?? "", Convert.ToString(r[2]) ?? "",
                dep?.Label ?? "–", Math.Round(rev, 0), own.Count, own.Count(x => x.Cause != "ok"), own.Count(x => x.Cause == "netz"),
                depStatus?.Availability24h);
        }).OrderByDescending(x => x.RevenueChf2025).ToList();
    }

    /// <summary>Verbindungen und Abbrueche je /24-Netzbereich.</summary>
    public async Task<(IReadOnlyList<NetworkSubnetStat> Subnets, IReadOnlyList<(int Hour, int Drops)> ByHour, DateTime? Since)> ClientStatsAsync(int days, CancellationToken ct = default)
    {
        var events = await _store.LoadClientEventsAsync(DateTime.UtcNow.AddDays(-days), ct);
        var subnets = events.GroupBy(e => e.Subnet)
            .Select(g => new NetworkSubnetStat(g.Key, g.Count(e => e.EventType == "open"), g.Count(e => e.EventType == "down"), g.Count(e => e.EventType == "up")))
            .OrderByDescending(s => s.Drops).ThenByDescending(s => s.Circuits).ToList();
        var byHour = Enumerable.Range(0, 24).Select(h => (h, events.Count(e => e.EventType == "down" && e.TimestampUtc.ToLocalTime().Hour == h))).ToList();
        return (subnets, byHour, events.Count == 0 ? null : events.Min(e => e.TimestampUtc));
    }

    internal static Dictionary<string, string> Parse(string? detail)
        => (detail ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2))
            .Where(p => p.Length == 2)
            .GroupBy(p => p[0].Trim())
            .ToDictionary(g => g.Key, g => g.First()[1].Trim());
}
