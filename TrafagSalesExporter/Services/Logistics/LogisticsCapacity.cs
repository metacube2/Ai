using System.Data;
using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Ein Tag eines Logistikbereichs (Wunsch Neva 2026-10-05, Übergangslösung „tägliche Erfassung von Menge und
/// Personenstunden je Arbeitsplatz“): verfügbare und eingesetzte Personenstunden, Planzeit je Einheit, offene und
/// erledigte Einheiten. Keine Personen, nur Summen je Bereich.
/// </summary>
public sealed record LogisticsCapacityEntry(
    DateOnly Day, string Area, double AvailableHours, double UsedHours, double PlanMinutesPerUnit, double OpenUnits, double DoneUnits)
{
    /// <summary>Erledigte Einheiten ÷ eingesetzte Personenstunden.</summary>
    public double? Productivity => UsedHours > 0 ? DoneUnits / UsedHours : null;

    /// <summary>Eingesetzte Personenminuten ÷ erledigte Einheiten (gemessene Zeit je Einheit).</summary>
    public double? MinutesPerUnit => DoneUnits > 0 && UsedHours > 0 ? UsedHours * 60 / DoneUnits : null;

    /// <summary>Kapazitätsbedarf in Stunden: offene Einheiten × Planzeit.</summary>
    public double RequiredHours => OpenUnits * PlanMinutesPerUnit / 60;

    /// <summary>Kapazitätsbelastung in Prozent: Bedarf ÷ verfügbare Stunden × 100.</summary>
    public double? LoadPercent => AvailableHours > 0 ? RequiredHours / AvailableHours * 100 : null;

    /// <summary>Bedarf da, aber keine verfügbaren Stunden erfasst: die Belastung ist nicht null, sondern unendlich.</summary>
    public bool NoCapacityRecorded => AvailableHours <= 0 && RequiredHours > 0;

    /// <summary>Kapazitätslücke: Bedarf minus verfügbare Stunden (positiv = es fehlt).</summary>
    public double Gap => RequiredHours - AvailableHours;
}

/// <summary>Ein Balken der Vorschau Warenausgang: ein Tag oder der Eimer "überfällig".</summary>
public sealed record LiveOutlookBucket(DateOnly Day, bool Overdue, int Deliveries, int Positions, int WithoutPositions);

/// <summary>Vorschau Warenausgang nach Termin: überfällig, laufender Tag und die nächsten 14 Tage.</summary>
public static class LogisticsOutlook
{
    /// <summary>
    /// Gezählt werden nur Lieferungen mit Kommissionierpositionen (Positions &gt; 0); die ohne stehen getrennt in
    /// <see cref="LiveOutlookBucket.WithoutPositions"/>. Überfällig = Termin vor dem Tag und Warenausgang nicht gebucht;
    /// der Eimer erscheint nur, wenn P76 solche Lieferungen liefert.
    /// </summary>
    public static IReadOnlyList<LiveOutlookBucket> Build(IReadOnlyList<LiveDelivery> deliveries, DateOnly today)
    {
        var result = new List<LiveOutlookBucket>();
        var overdue = deliveries.Where(d => d.PlannedGoodsIssue.HasValue && DateOnly.FromDateTime(d.PlannedGoodsIssue.Value) < today && d.GoodsIssueStatus != "C").ToList();
        if (overdue.Count > 0)
            result.Add(Bucket(today, true, overdue));
        for (var i = 0; i <= 14; i++)
        {
            var day = today.AddDays(i);
            result.Add(Bucket(day, false, deliveries.Where(d => d.PlannedGoodsIssue.HasValue && DateOnly.FromDateTime(d.PlannedGoodsIssue.Value) == day).ToList()));
        }
        return result;
    }

    private static LiveOutlookBucket Bucket(DateOnly day, bool overdue, List<LiveDelivery> list)
    {
        var picking = list.Where(d => d.Positions > 0).ToList();
        return new LiveOutlookBucket(day, overdue, picking.Count, picking.Sum(d => Math.Max(0, d.Positions - d.PositionsPicked)), list.Count - picking.Count);
    }
}

/// <summary>Bereiche nach Nevas Unterlage „Logistikkennzahlen bei Trafag“ mit ihrer Zähleinheit.</summary>
public static class LogisticsCapacityAreas
{
    public const string Picking = "Rüsten Kundenaufträge";

    public static readonly (string Area, string Unit)[] All =
    [
        ("Wareneingang", "Positionen"),
        ("Versorgung Abteilungen", "TA-Positionen"),
        ("Vorverpackung", "Aufträge"),
        ("MLE01", "Gutmenge"),
        ("MLE02", "Gutmenge"),
        ("MLE04", "Gutmenge"),
        (Picking, "TA-Positionen"),
        ("Sammellisten", "Lieferungen")
    ];
}

/// <summary>Speicher der Tageswerte in der Cockpit-Datenbank (eigene Tabelle, angelegt in DatabaseSchemaMaintenanceService).</summary>
public sealed class LogisticsCapacityStore
{
    internal const string TableSql = @"
CREATE TABLE IF NOT EXISTS LogisticsCapacityDay (
    Day TEXT NOT NULL, Area TEXT NOT NULL, AvailableHours REAL NOT NULL DEFAULT 0, UsedHours REAL NOT NULL DEFAULT 0,
    PlanMinutesPerUnit REAL NOT NULL DEFAULT 0, OpenUnits REAL NOT NULL DEFAULT 0, DoneUnits REAL NOT NULL DEFAULT 0,
    UpdatedAtUtc TEXT NOT NULL, PRIMARY KEY (Day, Area));";

    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public LogisticsCapacityStore(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<IReadOnlyList<LogisticsCapacityEntry>> LoadAsync(DateOnly from, DateOnly to)
    {
        var result = new List<LogisticsCapacityEntry>();
        await using var db = await _dbFactory.CreateDbContextAsync();
        var conn = (SqliteConnection)db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Day, Area, AvailableHours, UsedHours, PlanMinutesPerUnit, OpenUnits, DoneUnits FROM LogisticsCapacityDay WHERE Day >= $f AND Day <= $t";
        cmd.Parameters.AddWithValue("$f", from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        cmd.Parameters.AddWithValue("$t", to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            result.Add(new LogisticsCapacityEntry(DateOnly.ParseExact(r.GetString(0), "yyyy-MM-dd", CultureInfo.InvariantCulture), r.GetString(1),
                r.GetDouble(2), r.GetDouble(3), r.GetDouble(4), r.GetDouble(5), r.GetDouble(6)));
        return result;
    }

    public async Task SaveAsync(IEnumerable<LogisticsCapacityEntry> entries)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var conn = (SqliteConnection)db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();
        await using var tx = (SqliteTransaction)await conn.BeginTransactionAsync();
        foreach (var e in entries)
        {
            await using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"INSERT OR REPLACE INTO LogisticsCapacityDay (Day, Area, AvailableHours, UsedHours, PlanMinutesPerUnit, OpenUnits, DoneUnits, UpdatedAtUtc)
VALUES ($d, $a, $av, $u, $p, $o, $dn, $ts)";
            cmd.Parameters.AddWithValue("$d", e.Day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            cmd.Parameters.AddWithValue("$a", e.Area);
            cmd.Parameters.AddWithValue("$av", e.AvailableHours);
            cmd.Parameters.AddWithValue("$u", e.UsedHours);
            cmd.Parameters.AddWithValue("$p", e.PlanMinutesPerUnit);
            cmd.Parameters.AddWithValue("$o", e.OpenUnits);
            cmd.Parameters.AddWithValue("$dn", e.DoneUnits);
            cmd.Parameters.AddWithValue("$ts", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            await cmd.ExecuteNonQueryAsync();
        }
        await tx.CommitAsync();
    }

    /// <summary>Für das Rüsten am laufenden Tag: offene und quittierte TA-Positionen mit Lieferung aus dem Live-Abruf.</summary>
    public static (int Open, int Done) PickingFromLive(IEnumerable<LiveTransferItem> transfers, DateOnly? day = null)
    {
        // Erledigt zaehlt nur, was am Tag selbst quittiert wurde; Offenes von frueheren Tagen bleibt Bedarf.
        var withDelivery = transfers.Where(t => t.Delivery.Trim().Length > 0).ToList();
        return (withDelivery.Count(t => !t.Confirmed),
            withDelivery.Count(t => t.Confirmed && (day is null || t.ConfirmedAt is null || DateOnly.FromDateTime(t.ConfirmedAt.Value) == day)));
    }
}
