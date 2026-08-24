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
}

public sealed class PurchasingStockValueStore : IPurchasingStockValueStore
{
    private const string TableName = "PurchasingStockValueCache";

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

        await transaction.CommitAsync(cancellationToken);
    }

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
