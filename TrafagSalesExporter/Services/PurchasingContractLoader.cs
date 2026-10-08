using System.Globalization;
using Microsoft.Data.Sqlite;

namespace TrafagSalesExporter.Services;

/// <summary>Liest alle Zeilen eines OData-Sets (Paging inklusive). Entspricht <c>ReadAllRowsAsync</c> des Refresh-Dienstes.</summary>
internal delegate Task<List<Dictionary<string, object?>>> SapEntitySetReader(
    string entitySet, string select, string filter, string orderBy, CancellationToken cancellationToken);

/// <param name="Available">true nur, wenn das Set gelesen wurde UND mindestens eine Zeile lieferte.</param>
/// <param name="Warning">Grund, warum nichts uebernommen wird (leer bei Erfolg).</param>
internal sealed record PurchasingSapSetResult(bool Available, IReadOnlyList<Dictionary<string, object?>> Rows, string Warning);

/// <summary>Eine Kontraktposition, bereit fuer den Cache (Zahlen normalisiert, Datum ISO).</summary>
internal sealed record PurchasingContractCacheRow(
    string Ebeln, string Ebelp, string Bukrs, string Bsart, string Lifnr, string Matnr, string Txz01, string Matkl,
    string Waers, string Wkurs, string? Kdatb, string? Kdate, string Loekz, string Meins,
    string Ktmng, string Netpr, string Peinh, string Zwert, string Abmng, string Abwrt);

/// <summary>
/// Laden der Kontrakt- und LZ-Code-Daten (<c>EinkKontraktSet</c>, <c>EinkMatLzSet</c>) in die Caches.
///
/// DESIGNREGEL, die aus dem Vorfall vom 2026-07-02 stammt (ein 404 auf MARA001Set brach den Full
/// Load ab und fror den Datenstand bis zum 2026-07-17 ein): Diese beiden Sets liegen in SAP T76 und
/// sind bis zum Transport nach P76 nicht vorhanden (404). Ein Fehler beim Lesen darf den
/// Einkauf-Lauf NIE abbrechen. <see cref="TryReadAsync"/> faengt deshalb jede Ausnahme (ausser dem
/// Abbruch des Laufs selbst) und meldet sie als Warnung; <see cref="ReplaceContractsAsync"/> und
/// <see cref="ReplaceMaterialLzAsync"/> ersetzen den Cache nur nach erfolgreichem Lesen mit
/// mindestens einer Zeile, sonst bleibt der bisherige Stand stehen.
///
/// Das Parsen und Schreiben ist vom HTTP getrennt (Leser als Delegat), damit es ohne SAP testbar ist.
/// </summary>
internal static class PurchasingContractLoader
{
    internal const string ContractSet = "EinkKontraktSet";
    internal const string LzSet = "EinkMatLzSet";
    internal const string ContractSelect = "Ebeln,Ebelp,Bukrs,Bsart,Lifnr,Matnr,Txz01,Matkl,Waers,Wkurs,Kdatb,Kdate,Loekz,Meins,Ktmng,Netpr,Peinh,Zwert,Abmng,Abwrt";
    internal const string LzSelect = "Matnr,Lzcode,Lzsort";

    internal static async Task<PurchasingSapSetResult> TryReadAsync(
        SapEntitySetReader reader, string entitySet, string select, string orderBy, CancellationToken cancellationToken)
    {
        try
        {
            var rows = await reader(entitySet, select, string.Empty, orderBy, cancellationToken);
            return rows.Count == 0
                ? new PurchasingSapSetResult(false, [], $"{entitySet} lieferte keine Zeilen; bisheriger Cache bleibt unveraendert.")
                : new PurchasingSapSetResult(true, rows, string.Empty);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Abbruch des Laufs selbst ist KEIN Lesefehler und wird nicht verschluckt.
            throw;
        }
        catch (Exception ex)
        {
            return new PurchasingSapSetResult(false, [],
                $"{entitySet} nicht lesbar ({ex.GetType().Name}: {ex.Message}); bisheriger Cache bleibt unveraendert.");
        }
    }

    internal static PurchasingContractCacheRow? ParseContractRow(Dictionary<string, object?> row)
    {
        var ebeln = Text(row, "Ebeln");
        var ebelp = Text(row, "Ebelp");
        if (ebeln.Length == 0 || ebelp.Length == 0)
            return null;

        return new PurchasingContractCacheRow(
            ebeln, ebelp, Text(row, "Bukrs"), Text(row, "Bsart"), Text(row, "Lifnr"),
            Text(row, "Matnr"), Text(row, "Txz01"), Text(row, "Matkl"), Text(row, "Waers"),
            Number(row, "Wkurs"),
            Date(row, "Kdatb"), Date(row, "Kdate"),
            Text(row, "Loekz"), Text(row, "Meins"),
            Number(row, "Ktmng"), Number(row, "Netpr"), Number(row, "Peinh"),
            Number(row, "Zwert"), Number(row, "Abmng"), Number(row, "Abwrt"));
    }

    /// <summary>Normalisierte Materialnummer: Grossbuchstaben, ohne fuehrende Nullen (wie <c>NormalizeMaterialKeySql</c>).</summary>
    internal static string NormalizeMaterialKey(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim().ToUpperInvariant();
        if (trimmed.Length == 0)
            return string.Empty;
        var withoutZeros = trimmed.TrimStart('0');
        return withoutZeros.Length == 0 ? "0" : withoutZeros;
    }

    /// <summary>Wandelt LZ-Zeilen in (Matnr normalisiert, Lzcode, Lzsort); Zeilen ohne Material oder ohne beide Codes entfallen.</summary>
    internal static List<(string Matnr, string Lzcode, string Lzsort)> ParseLzRows(IEnumerable<Dictionary<string, object?>> rows)
    {
        var result = new Dictionary<string, (string Matnr, string Lzcode, string Lzsort)>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            var matnr = NormalizeMaterialKey(Text(row, "Matnr"));
            var lzcode = Text(row, "Lzcode");
            var lzsort = Text(row, "Lzsort");
            if (matnr.Length == 0 || (lzcode.Length == 0 && lzsort.Length == 0))
                continue;
            // Bei doppelter Materialnummer (z.B. mit/ohne fuehrende Nullen) gewinnt die letzte Zeile.
            result[matnr] = (matnr, lzcode, lzsort);
        }

        return result.Values.ToList();
    }

    /// <summary>Ersetzt den Kontrakt-Cache vollstaendig in einer eigenen Transaktion. Liefert die Zahl geschriebener Zeilen.</summary>
    internal static async Task<int> ReplaceContractsAsync(
        SqliteConnection conn,
        IEnumerable<Dictionary<string, object?>> rows,
        Func<string, string> supplierNameResolver,
        string loadedAtUtc,
        CancellationToken cancellationToken)
    {
        var parsed = rows.Select(ParseContractRow).Where(row => row is not null).Select(row => row!).ToList();
        if (parsed.Count == 0)
            return 0;

        await using var transaction = (SqliteTransaction)await conn.BeginTransactionAsync(cancellationToken);
        await using (var delete = conn.CreateCommand())
        {
            delete.Transaction = transaction;
            delete.CommandText = "DELETE FROM PurchasingContractCache;";
            await delete.ExecuteNonQueryAsync(cancellationToken);
        }

        const string sql = @"
INSERT OR REPLACE INTO PurchasingContractCache (Ebeln, Ebelp, Bukrs, Bsart, Lifnr, SupplierName, Matnr, Txz01, Matkl, Waers, Wkurs, Kdatb, Kdate, Loekz, Meins, Ktmng, Netpr, Peinh, Zwert, Abmng, Abwrt, LastLoadedAtUtc)
VALUES ($Ebeln, $Ebelp, $Bukrs, $Bsart, $Lifnr, $SupplierName, $Matnr, $Txz01, $Matkl, $Waers, $Wkurs, $Kdatb, $Kdate, $Loekz, $Meins, $Ktmng, $Netpr, $Peinh, $Zwert, $Abmng, $Abwrt, $LastLoadedAtUtc);";
        foreach (var row in parsed)
        {
            await using var command = conn.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$Ebeln", row.Ebeln);
            command.Parameters.AddWithValue("$Ebelp", row.Ebelp);
            command.Parameters.AddWithValue("$Bukrs", row.Bukrs);
            command.Parameters.AddWithValue("$Bsart", row.Bsart);
            command.Parameters.AddWithValue("$Lifnr", row.Lifnr);
            command.Parameters.AddWithValue("$SupplierName", supplierNameResolver(row.Lifnr) ?? string.Empty);
            command.Parameters.AddWithValue("$Matnr", row.Matnr);
            command.Parameters.AddWithValue("$Txz01", row.Txz01);
            command.Parameters.AddWithValue("$Matkl", row.Matkl);
            command.Parameters.AddWithValue("$Waers", row.Waers);
            command.Parameters.AddWithValue("$Wkurs", row.Wkurs);
            command.Parameters.AddWithValue("$Kdatb", (object?)row.Kdatb ?? DBNull.Value);
            command.Parameters.AddWithValue("$Kdate", (object?)row.Kdate ?? DBNull.Value);
            command.Parameters.AddWithValue("$Loekz", row.Loekz);
            command.Parameters.AddWithValue("$Meins", row.Meins);
            command.Parameters.AddWithValue("$Ktmng", row.Ktmng);
            command.Parameters.AddWithValue("$Netpr", row.Netpr);
            command.Parameters.AddWithValue("$Peinh", row.Peinh);
            command.Parameters.AddWithValue("$Zwert", row.Zwert);
            command.Parameters.AddWithValue("$Abmng", row.Abmng);
            command.Parameters.AddWithValue("$Abwrt", row.Abwrt);
            command.Parameters.AddWithValue("$LastLoadedAtUtc", loadedAtUtc);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return parsed.Count;
    }

    /// <summary>Ersetzt den LZ-Code-Cache vollstaendig in einer eigenen Transaktion. Liefert die Zahl geschriebener Zeilen.</summary>
    internal static async Task<int> ReplaceMaterialLzAsync(
        SqliteConnection conn,
        IEnumerable<Dictionary<string, object?>> rows,
        string loadedAtUtc,
        CancellationToken cancellationToken)
    {
        var parsed = ParseLzRows(rows);
        if (parsed.Count == 0)
            return 0;

        await using var transaction = (SqliteTransaction)await conn.BeginTransactionAsync(cancellationToken);
        await using (var delete = conn.CreateCommand())
        {
            delete.Transaction = transaction;
            delete.CommandText = "DELETE FROM PurchasingMaterialLzCache;";
            await delete.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var command = conn.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "INSERT OR REPLACE INTO PurchasingMaterialLzCache (Matnr, Lzcode, Lzsort, LastLoadedAtUtc) VALUES ($Matnr, $Lzcode, $Lzsort, $LastLoadedAtUtc);";
            var matnr = command.Parameters.Add("$Matnr", SqliteType.Text);
            var lzcode = command.Parameters.Add("$Lzcode", SqliteType.Text);
            var lzsort = command.Parameters.Add("$Lzsort", SqliteType.Text);
            command.Parameters.AddWithValue("$LastLoadedAtUtc", loadedAtUtc);
            foreach (var row in parsed)
            {
                matnr.Value = row.Matnr;
                lzcode.Value = row.Lzcode;
                lzsort.Value = row.Lzsort;
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return parsed.Count;
    }

    private static string Text(Dictionary<string, object?> row, string key)
        => row.TryGetValue(key, out var value)
            ? (Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty).Trim()
            : string.Empty;

    // Zahl als invarianter Text mit fuehrendem Minus (SAP liefert negative Werte teils mit nachgestelltem Minus;
    // SQLite wuerde "1.075-" beim CAST als +1.075 lesen und das Vorzeichen still verlieren).
    private static string Number(Dictionary<string, object?> row, string key)
        => PurchasingContractCalculator.ParseSapDecimal(Text(row, key)).ToString(CultureInfo.InvariantCulture);

    private static string? Date(Dictionary<string, object?> row, string key)
        => PurchasingContractCalculator.ParseSapDate(Text(row, key))?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
