namespace TrafagSalesExporter.Services;

/// <summary>Rüsten einer Lieferung: vom ersten angelegten Transportauftrag bis zur letzten Quittierung.</summary>
public sealed record LivePickingRun(
    string Delivery, string Lgnum, int Items, int ItemsConfirmed, DateTime FirstCreated, DateTime? LastConfirmed)
{
    public bool Done => ItemsConfirmed == Items && LastConfirmed.HasValue;

    /// <summary>Minuten bis fertig gerüstet, oder bei offenen Lieferungen bisher verstrichene Minuten.</summary>
    public double Minutes(DateTime now) => ((Done ? LastConfirmed!.Value : now) - FirstCreated).TotalMinutes;
}

/// <summary>Minuten bis Warenausgang plus Zahl der Lieferungen, die nicht eingehen konnten.</summary>
public sealed record GoodsIssueStats(IReadOnlyList<double> Minutes, int WithoutTime, int Inconsistent);

/// <summary>Leistung je Lagernummer und Stunde (quittierte Positionen), anonym.</summary>
public sealed record LiveHourlyOutput(string Lgnum, int Hour, int Confirmed);

/// <summary>
/// Logistik live, Produktivität (Wunsch Logistik 2026-10-05): Rüsten je Lieferung und Leistung je Stunde, ohne Personen.
/// Rechnet nur aus dem gemeinsamen Abruf (LogTaSet), kein zusätzlicher SAP-Zugriff. Doku docs/LOGISTIK_LIVE_2026-10-01.md.
/// </summary>
public static class LogisticsProductivity
{
    /// <summary>Rüstvorgänge je Lieferung; Positionen ohne Lieferung (Umlagerungen) zählen nicht.</summary>
    public static IReadOnlyList<LivePickingRun> PickingRuns(IEnumerable<LiveTransferItem> transfers)
        => transfers
            .Where(t => t.Delivery.Trim().Length > 0 && t.CreatedAt.HasValue)
            .GroupBy(t => t.Delivery.Trim().TrimStart('0'))
            .Select(g => new LivePickingRun(
                g.Key, g.First().Lgnum, g.Count(), g.Count(t => t.Confirmed),
                g.Min(t => t.CreatedAt!.Value),
                g.Where(t => t.Confirmed && t.ConfirmedAt.HasValue).Select(t => (DateTime?)t.ConfirmedAt!.Value).DefaultIfEmpty(null).Max()))
            .OrderByDescending(r => r.FirstCreated)
            .ToList();

    /// <summary>Quittierte Positionen je Lagernummer und Stunde.</summary>
    public static IReadOnlyList<LiveHourlyOutput> HourlyOutput(IEnumerable<LiveTransferItem> transfers, DateOnly? day = null)
        => transfers
            .Where(t => t.Confirmed && t.ConfirmedAt.HasValue && (day is null || DateOnly.FromDateTime(t.ConfirmedAt.Value) == day))
            .GroupBy(t => (t.Lgnum, t.ConfirmedAt!.Value.Hour))
            .Select(g => new LiveHourlyOutput(g.Key.Lgnum, g.Key.Hour, g.Count()))
            .OrderBy(o => o.Lgnum).ThenBy(o => o.Hour)
            .ToList();

    /// <summary>Minuten vom ersten angelegten TA bis zur Warenausgangsbuchung, je Lieferung mit gebuchtem Warenausgang.</summary>
    public static IReadOnlyList<double> UntilGoodsIssue(IEnumerable<LivePickingRun> runs, IEnumerable<LiveDelivery> deliveries)
        => GoodsIssueDurations(runs, deliveries).Minutes;

    /// <summary>
    /// Wie <see cref="UntilGoodsIssue"/>, nennt aber auch, was nicht in den Minuten steht: Lieferungen mit gebuchtem
    /// Warenausgang ohne Uhrzeit (Datum da, Zeit leer oder 00:00; P76 liefert die Zeit aus VBFA/MKPF erst nach dem
    /// naechsten Transport) und solche mit Warenausgang vor dem ersten TA (widerspruechlich, z. B. TA nach der Buchung).
    /// Beides still zu verwerfen hiess: Kachel dauerhaft "-" ohne Grund.
    /// </summary>
    public static GoodsIssueStats GoodsIssueDurations(IEnumerable<LivePickingRun> runs, IEnumerable<LiveDelivery> deliveries)
    {
        var gi = deliveries.Where(d => d.GoodsIssueAt.HasValue)
            .GroupBy(d => d.Delivery.TrimStart('0'))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(d => d.GoodsIssueTimeKnown).First());
        var minutes = new List<double>();
        int withoutTime = 0, inconsistent = 0;
        foreach (var run in runs)
        {
            if (!gi.TryGetValue(run.Delivery, out var delivery))
                continue;
            if (!delivery.GoodsIssueTimeKnown)
            {
                withoutTime++;
                continue;
            }
            var m = (delivery.GoodsIssueAt!.Value - run.FirstCreated).TotalMinutes;
            if (m < 0)
                inconsistent++;
            else
                minutes.Add(m);
        }
        return new GoodsIssueStats(minutes, withoutTime, inconsistent);
    }

    /// <summary>Wert am Anteil p (0..1) einer Liste, linear zwischen den Nachbarn; null bei leerer Liste.</summary>
    public static double? Quantile(IEnumerable<double> values, double p)
    {
        var v = values.OrderBy(x => x).ToList();
        if (v.Count == 0) return null;
        var pos = (v.Count - 1) * Math.Clamp(p, 0, 1);
        var lo = (int)Math.Floor(pos);
        var hi = (int)Math.Ceiling(pos);
        return v[lo] + (v[hi] - v[lo]) * (pos - lo);
    }
}
