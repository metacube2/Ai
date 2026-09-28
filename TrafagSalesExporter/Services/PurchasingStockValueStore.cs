using System.Data;
using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Schreibt den zuletzt gelesenen Lagerwert in die Datenbank und liest ihn zurueck.
///
/// WARUM ES DIESEN SPEICHER GIBT (Befund 2026-08-24): Der Lagerwert lag ausschliesslich in
/// einem Feld des Singleton-Readers. Jeder Neustart des IIS-Workers loeschte ihn, und der
/// naechtliche Einkauf-Lauf holt ihn nur im planmaessigen Slot nach, nicht im Nachhol-Lauf.
/// Praktisch stand die KPI-Kachel deshalb jeden Morgen wieder auf "wartet auf Einkauf-Lauf",
/// obwohl der Wert am Vortag gelesen worden war.
/// </summary>
public interface IPurchasingStockValueStore
{
    /// <summary>
    /// Ersetzt den gespeicherten Stand des Bewertungskreises durch den uebergebenen. Ein
    /// Teilstand darf nicht entstehen, deshalb laeuft Loeschen und Schreiben in einer
    /// Transaktion.
    /// </summary>
    Task SaveAsync(StockValueSnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Liest den gespeicherten Stand, oder <c>null</c>, wenn keiner vorliegt. BEWUSST OHNE
    /// HALTBARKEITSPRUEFUNG: das Alter beurteilt der Aufrufer, der es auch anzeigt. Ein
    /// sichtbar alter Wert ist brauchbar, eine leere Kachel ist es nicht.
    /// </summary>
    Task<StockValueSnapshot?> LoadAsync(string valuationArea, CancellationToken cancellationToken = default);

    /// <summary>
    /// Liest den Verlauf, einen Stand je Tag, aufsteigend nach Datum. Leer, wenn noch nichts
    /// gesammelt ist. Die Verdichtung auf Wochen macht <see cref="StockValueHistory.ToWeekly"/>.
    /// </summary>
    Task<IReadOnlyList<StockValueHistoryDay>> LoadHistoryAsync(string valuationArea, CancellationToken cancellationToken = default);
}

/// <summary>Ein gespeicherter Tagesstand des Lagerwerts, je Disponent aufgeschluesselt.</summary>
public sealed record StockValueHistoryDay(
    DateOnly SnapshotDate,
    DateTime ReadAtUtc,
    IReadOnlyList<StockValueByPlannerRow> Rows);

/// <summary>Ein Punkt des Wochenverlaufs, bereits auf die Disponenten-Abgrenzung summiert.</summary>
public sealed record StockValueWeekPoint(
    int IsoYear,
    int IsoWeek,
    DateOnly SnapshotDate,
    DateTime ReadAtUtc,
    decimal Value,
    int MaterialCount);

public static class StockValueHistory
{
    private static readonly TimeZoneInfo SwissTimeZone = ResolveSwissTimeZone();

    /// <summary>
    /// Kalendertag eines Lesezeitpunkts in Schweizer Ortszeit. Ein Lauf kurz nach Mitternacht
    /// Ortszeit gehoert fachlich zum neuen Tag, obwohl er in UTC noch am Vortag liegt.
    /// </summary>
    public static DateOnly ToSnapshotDate(DateTime readAtUtc)
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(readAtUtc, DateTimeKind.Utc), SwissTimeZone));

    /// <summary>
    /// Verdichtet die Tagesstaende auf einen Punkt je ISO-Kalenderwoche: den LETZTEN Tag der
    /// Woche, also den Stand Ende Woche. Wochen ohne Lauf fehlen bewusst und werden nicht
    /// interpoliert — eine Luecke ist ehrlicher als ein erfundener Wert.
    ///
    /// ISO-Jahr und ISO-Woche kommen beide aus <see cref="ISOWeek"/>. Das Kalenderjahr waere
    /// falsch: der 2027-01-01 gehoert zur KW 53 von 2026.
    /// </summary>
    public static IReadOnlyList<StockValueWeekPoint> ToWeekly(
        IEnumerable<StockValueHistoryDay> days, IReadOnlyCollection<string> planners)
    {
        ArgumentNullException.ThrowIfNull(days);
        ArgumentNullException.ThrowIfNull(planners);

        return days
            .GroupBy(day =>
            {
                var date = day.SnapshotDate.ToDateTime(TimeOnly.MinValue);
                return (Year: ISOWeek.GetYear(date), Week: ISOWeek.GetWeekOfYear(date));
            })
            .Select(week =>
            {
                var last = week.OrderBy(day => day.SnapshotDate).Last();
                var inScope = last.Rows
                    .Where(row => planners.Contains(row.Planner, StringComparer.OrdinalIgnoreCase))
                    .ToList();
                return new StockValueWeekPoint(
                    week.Key.Year,
                    week.Key.Week,
                    last.SnapshotDate,
                    last.ReadAtUtc,
                    inScope.Sum(row => row.Value),
                    inScope.Sum(row => row.MaterialCount));
            })
            .OrderBy(point => point.SnapshotDate)
            .ToList();
    }

    private static TimeZoneInfo ResolveSwissTimeZone()
    {
        foreach (var id in new[] { "Europe/Zurich", "W. Europe Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Local;
    }
}

public sealed class PurchasingStockValueStore : IPurchasingStockValueStore
{
    private const string TableName = "PurchasingStockValueCache";
    private const string HistoryTableName = "PurchasingStockValueHistory";

    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public PurchasingStockValueStore(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task SaveAsync(StockValueSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (string.IsNullOrWhiteSpace(snapshot.ValuationArea))
            throw new ArgumentException(
                "Ohne Bewertungskreis waere der gespeicherte Stand nicht zuordenbar.", nameof(snapshot));

        var area = snapshot.ValuationArea.Trim();
        var readAtText = FormatTimestamp(snapshot.ReadAtUtc);

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var conn = (SqliteConnection)db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(cancellationToken);

        await using var transaction = (SqliteTransaction)await conn.BeginTransactionAsync(cancellationToken);

        await using (var delete = conn.CreateCommand())
        {
            delete.Transaction = transaction;
            delete.CommandText = $"DELETE FROM {TableName} WHERE ValuationArea = $area;";
            delete.Parameters.AddWithValue("$area", area);
            await delete.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var row in snapshot.Rows)
        {
            await using var insert = conn.CreateCommand();
            insert.Transaction = transaction;
            // INSERT OR REPLACE statt INSERT: ein doppelter Disponent in der Eingabe darf den
            // Einkauf-Lauf nicht mit einem Schluesselkonflikt abbrechen.
            insert.CommandText = $@"
INSERT OR REPLACE INTO {TableName}
    (ValuationArea, Planner, Value, Quantity, MaterialCount, ReadAtUtc)
VALUES ($area, $planner, $value, $quantity, $count, $readAt);";
            insert.Parameters.AddWithValue("$area", area);
            insert.Parameters.AddWithValue("$planner", row.Planner ?? string.Empty);
            insert.Parameters.AddWithValue("$value", FormatNumber(row.Value));
            insert.Parameters.AddWithValue("$quantity", FormatNumber(row.Quantity));
            insert.Parameters.AddWithValue("$count", row.MaterialCount);
            insert.Parameters.AddWithValue("$readAt", readAtText);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        // Verlauf in derselben Transaktion, damit Kachel und Verlauf nie auseinanderlaufen.
        // Der Tagesstand wird ERSETZT, nicht ergaenzt: ein zweiter Lauf am selben Tag ist der
        // neuere Stand. Aus dem Verlauf wird sonst nie geloescht.
        var snapshotDateText = FormatDate(StockValueHistory.ToSnapshotDate(snapshot.ReadAtUtc));

        await using (var deleteDay = conn.CreateCommand())
        {
            deleteDay.Transaction = transaction;
            deleteDay.CommandText = $"DELETE FROM {HistoryTableName} WHERE ValuationArea = $area AND SnapshotDate = $date;";
            deleteDay.Parameters.AddWithValue("$area", area);
            deleteDay.Parameters.AddWithValue("$date", snapshotDateText);
            await deleteDay.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var row in snapshot.Rows)
        {
            await using var insert = conn.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = $@"
INSERT OR REPLACE INTO {HistoryTableName}
    (ValuationArea, SnapshotDate, Planner, Value, Quantity, MaterialCount, ReadAtUtc)
VALUES ($area, $date, $planner, $value, $quantity, $count, $readAt);";
            insert.Parameters.AddWithValue("$area", area);
            insert.Parameters.AddWithValue("$date", snapshotDateText);
            insert.Parameters.AddWithValue("$planner", row.Planner ?? string.Empty);
            insert.Parameters.AddWithValue("$value", FormatNumber(row.Value));
            insert.Parameters.AddWithValue("$quantity", FormatNumber(row.Quantity));
            insert.Parameters.AddWithValue("$count", row.MaterialCount);
            insert.Parameters.AddWithValue("$readAt", readAtText);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StockValueHistoryDay>> LoadHistoryAsync(
        string valuationArea, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(valuationArea))
            return [];

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var conn = (SqliteConnection)db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(cancellationToken);

        await using var command = conn.CreateCommand();
        command.CommandText = $@"
SELECT SnapshotDate, Planner, Value, Quantity, MaterialCount, ReadAtUtc
FROM {HistoryTableName}
WHERE ValuationArea = $area
ORDER BY SnapshotDate;";
        command.Parameters.AddWithValue("$area", valuationArea.Trim());

        var byDate = new SortedDictionary<DateOnly, (DateTime ReadAt, List<StockValueByPlannerRow> Rows)>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            // Ein unlesbares Datum oder ein Stand ohne Zeitpunkt wird uebersprungen, statt den
            // ganzen Verlauf zu verwerfen oder einen Punkt an eine erfundene Stelle zu setzen.
            if (!DateOnly.TryParseExact(reader.GetString(0), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                continue;
            var stamp = ParseTimestamp(reader.IsDBNull(5) ? string.Empty : reader.GetString(5));
            if (stamp is null)
                continue;

            if (!byDate.TryGetValue(date, out var day))
                day = (stamp.Value, []);

            day.Rows.Add(new StockValueByPlannerRow(
                reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                ParseNumber(reader.IsDBNull(2) ? string.Empty : reader.GetString(2)),
                ParseNumber(reader.IsDBNull(3) ? string.Empty : reader.GetString(3)),
                reader.IsDBNull(4) ? 0 : reader.GetInt32(4)));
            byDate[date] = (stamp.Value > day.ReadAt ? stamp.Value : day.ReadAt, day.Rows);
        }

        return byDate
            .Select(pair => new StockValueHistoryDay(pair.Key, pair.Value.ReadAt, pair.Value.Rows))
            .ToList();
    }

    /// <summary>
    /// Uebernimmt den aktuell gespeicherten Stand als ersten Verlaufspunkt, falls dieser Tag im
    /// Verlauf noch fehlt. Laeuft bei der Schemapflege beim Start; <c>INSERT OR IGNORE</c> macht
    /// es wiederholbar und laesst einen schon vorhandenen Tagesstand unangetastet.
    /// </summary>
    internal static void SeedHistoryFromCache(SqliteConnection conn)
    {
        var rows = new List<(string Area, string Planner, string Value, string Quantity, int Count, string ReadAt)>();
        using (var read = conn.CreateCommand())
        {
            read.CommandText = $"SELECT ValuationArea, Planner, Value, Quantity, MaterialCount, ReadAtUtc FROM {TableName};";
            using var reader = read.ExecuteReader();
            while (reader.Read())
            {
                rows.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2),
                    reader.GetString(3), reader.GetInt32(4), reader.GetString(5)));
            }
        }

        foreach (var row in rows)
        {
            var stamp = ParseTimestamp(row.ReadAt);
            if (stamp is null)
                continue;

            using var insert = conn.CreateCommand();
            insert.CommandText = $@"
INSERT OR IGNORE INTO {HistoryTableName}
    (ValuationArea, SnapshotDate, Planner, Value, Quantity, MaterialCount, ReadAtUtc)
VALUES ($area, $date, $planner, $value, $quantity, $count, $readAt);";
            insert.Parameters.AddWithValue("$area", row.Area);
            insert.Parameters.AddWithValue("$date", FormatDate(StockValueHistory.ToSnapshotDate(stamp.Value)));
            insert.Parameters.AddWithValue("$planner", row.Planner);
            insert.Parameters.AddWithValue("$value", row.Value);
            insert.Parameters.AddWithValue("$quantity", row.Quantity);
            insert.Parameters.AddWithValue("$count", row.Count);
            insert.Parameters.AddWithValue("$readAt", row.ReadAt);
            insert.ExecuteNonQuery();
        }
    }

    private static string FormatDate(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public async Task<StockValueSnapshot?> LoadAsync(string valuationArea, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(valuationArea))
            return null;

        var area = valuationArea.Trim();

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var conn = (SqliteConnection)db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(cancellationToken);

        await using var command = conn.CreateCommand();
        command.CommandText = $@"
SELECT Planner, Value, Quantity, MaterialCount, ReadAtUtc
FROM {TableName}
WHERE ValuationArea = $area;";
        command.Parameters.AddWithValue("$area", area);

        var rows = new List<StockValueByPlannerRow>();
        DateTime? readAtUtc = null;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new StockValueByPlannerRow(
                reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                ParseNumber(reader.IsDBNull(1) ? string.Empty : reader.GetString(1)),
                ParseNumber(reader.IsDBNull(2) ? string.Empty : reader.GetString(2)),
                reader.IsDBNull(3) ? 0 : reader.GetInt32(3)));

            var stamp = ParseTimestamp(reader.IsDBNull(4) ? string.Empty : reader.GetString(4));
            if (stamp.HasValue && (readAtUtc is null || stamp.Value > readAtUtc.Value))
                readAtUtc = stamp;
        }

        if (rows.Count == 0)
            return null;

        // Ohne lesbaren Zeitpunkt wird KEIN Stand geliefert. Ein Wert ohne Datum waere in der
        // Kachel nicht einordenbar, und ein erfundenes "jetzt" waere schlicht falsch.
        if (readAtUtc is null)
            return null;

        return new StockValueSnapshot(
            area,
            rows.OrderByDescending(row => row.Value).ToList(),
            readAtUtc.Value);
    }

    private static string FormatNumber(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    private static decimal ParseNumber(string text)
        => decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0m;

    private static string FormatTimestamp(DateTime value)
        => DateTime.SpecifyKind(value, DateTimeKind.Utc).ToString("O", CultureInfo.InvariantCulture);

    /// <summary>
    /// Liest den Zeitstempel zurueck. <c>RoundtripKind</c> steht bewusst allein: es laesst sich
    /// nicht mit <c>AdjustToUniversal</c> kombinieren (die Kombination wirft eine
    /// <see cref="ArgumentException"/>, hier am 2026-08-24 im Test aufgelaufen). Gespeichert wird
    /// im Format "O" mit Kennzeichnung als UTC, also traegt der geparste Wert bereits
    /// <see cref="DateTimeKind.Utc"/>. Fehlt einem alten Eintrag das Z, ist der Wert trotzdem
    /// UTC, weil nur UTC geschrieben wird — deshalb wird die Kennzeichnung hier gesetzt.
    /// </summary>
    private static DateTime? ParseTimestamp(string text)
        => DateTime.TryParse(
            text,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out var parsed)
            ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
            : null;
}
