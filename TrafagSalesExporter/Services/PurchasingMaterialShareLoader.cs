using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace TrafagSalesExporter.Services;

/// <summary>Liest ein SAP-Set mit einer einzigen, ungepagten Anfrage (Sets mit SUBMIT im Data Provider rechnen je Seite neu).</summary>
internal delegate Task<List<Dictionary<string, object?>>> SapSingleRequestReader(
    string entitySet, string select, string filter, CancellationToken cancellationToken);

/// <summary>Ergebnis eines Ladelaufs fuer den Materialanteil je Produktgruppe.</summary>
internal sealed record PurchasingMaterialShareLoadResult(int SalesRows, int Vknr, int BomRows, int FailedChunks, bool Written, string Message);

/// <summary>
/// Materialanteil je Produktgruppe nach Materialgruppe (Spend-Aufriss Phase 2, Bauplan
/// saptasks/zc23_materialanteil/BAUPLAN_PHASE2_MATERIALANTEIL_2026-11-02.md, Rechenweg Armin, Entscheide Ingo 2026-11-02).
///
/// Schritt 1: ZC23 (<c>Zc23Set</c>, Report ZM_OFFENE_KDAUF) je Monat ueber „Termin bestaetigt“ (VBEP-EDATU), nur Positionen
/// mit leerem Absagegrund, ohne Materialanteil-Rechnung (Mata leer, schneller). Summiert je verkuerzter Nummer und Monat.
/// Schritt 2: ZLO03 Top-Down (<c>ZloTopDownSet</c>, Z_ZLO03_TURBO2 FIX 25) je Block von verkuerzten Nummern: Menge, Stueckkosten
/// und Wert je 1 VKNR in der Einheit der Stueckliste, nur Komponenten der Einkaufsdisponenten 001 bis 006.
///
/// Geschrieben wird nur, wenn beide Schritte vollstaendig gelesen wurden; sonst bleibt der bisherige Stand. Die Tabellen legt
/// der Lader selbst an. Das HTTP ist vom Lader getrennt (Delegat), damit Auswahl, Summen und Schreiben ohne SAP testbar sind.
/// </summary>
internal static class PurchasingMaterialShareLoader
{
    internal const string SalesSet = "Zc23Set";
    internal const string SalesSelect = "Matnr,Edatu,Bmeng,Netwr1,Dispo,DispoDes,Abgru";
    internal const string BomSet = "ZloTopDownSet";
    internal const string BomSelect = "Vknr,Idnrk,Menge,Meins,Stk,Wert,Dispo,Matkl,Maktx";
    internal const string Plant = "1100";
    internal const int ChunkSize = 100;

    /// <summary>Einkaufsdisponenten wie <see cref="PurchasingDashboardService.PurchasingPlanners"/> (001 bis 006 seit 2026-11-02).</summary>
    internal static IReadOnlyList<string> Planners => PurchasingDashboardService.PurchasingPlanners;

    internal const string SchemaSql = @"
CREATE TABLE IF NOT EXISTS PurchasingMaterialShareSales (
    Vknr TEXT NOT NULL,
    Monat TEXT NOT NULL,
    Produktgruppe TEXT NOT NULL DEFAULT '',
    Dispo TEXT NOT NULL DEFAULT '',
    Bmeng REAL NOT NULL DEFAULT 0,
    Netwr REAL NOT NULL DEFAULT 0,
    LastLoadedAtUtc TEXT NOT NULL,
    PRIMARY KEY (Vknr, Monat, Produktgruppe)
);
CREATE TABLE IF NOT EXISTS PurchasingMaterialShareBom (
    Vknr TEXT NOT NULL,
    Idnrk TEXT NOT NULL,
    Menge REAL NOT NULL DEFAULT 0,
    Meins TEXT NOT NULL DEFAULT '',
    Stk REAL NOT NULL DEFAULT 0,
    Wert REAL NOT NULL DEFAULT 0,
    Dispo TEXT NOT NULL DEFAULT '',
    Matkl TEXT NOT NULL DEFAULT '',
    Maktx TEXT NOT NULL DEFAULT '',
    LastLoadedAtUtc TEXT NOT NULL,
    PRIMARY KEY (Vknr, Idnrk)
);
CREATE TABLE IF NOT EXISTS PurchasingMaterialShareState (
    Id INTEGER NOT NULL PRIMARY KEY CHECK (Id = 1),
    LoadedAtUtc TEXT NOT NULL,
    MonthFrom TEXT NOT NULL,
    MonthTo TEXT NOT NULL,
    Message TEXT NOT NULL DEFAULT ''
);";

    internal static async Task EnsureSchemaAsync(SqliteConnection conn, CancellationToken cancellationToken)
    {
        await using var command = conn.CreateCommand();
        command.CommandText = SchemaSql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>Monatsfenster: Januar des Vorjahres bis Dezember des laufenden Jahres (bestaetigte Termine liegen auch in der Zukunft).</summary>
    internal static List<(DateOnly From, DateOnly To)> MonthWindows(DateOnly today)
    {
        var result = new List<(DateOnly, DateOnly)>();
        for (var month = new DateOnly(today.Year - 1, 1, 1); month.Year <= today.Year; month = month.AddMonths(1))
            result.Add((month, month.AddMonths(1).AddDays(-1)));
        return result;
    }

    internal static string SalesFilter(DateOnly from, DateOnly to)
        => $"Werk eq '{Plant}' and Datvon eq '{from:yyyyMMdd}' and Datbis eq '{to:yyyyMMdd}' and Mata eq ''";

    internal static string BomFilter(IEnumerable<string> vknr)
        => $"Werks eq '{Plant}' and ({string.Join(" or ", vknr.Select(v => $"Vknr eq '{v.Replace("'", "''")}'"))})"
           + $" and ({string.Join(" or ", Planners.Select(p => $"Dispo eq '{p}'"))})";

    internal sealed record SalesKey(string Vknr, string Monat, string Produktgruppe);

    /// <summary>
    /// ZC23-Einteilungen je verkuerzter Nummer, Monat (Termin bestaetigt) und Produktgruppe summieren. Positionen mit
    /// Absagegrund fallen weg (sonst zaehlen abgesagte, nie bestellte Positionen mit, Entscheid Ingo 2026-11-02).
    /// </summary>
    internal static Dictionary<SalesKey, (string Dispo, decimal Bmeng, decimal Netwr)> AggregateSales(IEnumerable<Dictionary<string, object?>> rows)
    {
        var result = new Dictionary<SalesKey, (string, decimal, decimal)>();
        foreach (var row in rows)
        {
            if (Text(row, "Abgru").Length > 0)
                continue;
            var vknr = PurchasingComponentDispoLoader.NormalizeKey(Text(row, "Matnr"));
            var datum = SapDate(Text(row, "Edatu"));
            var bmeng = Number(row, "Bmeng");
            if (vknr.Length == 0 || datum is null || bmeng == 0)
                continue;
            var key = new SalesKey(vknr, datum.Value.ToString("yyyyMM", CultureInfo.InvariantCulture), Text(row, "DispoDes"));
            result.TryGetValue(key, out var sum);
            result[key] = (Text(row, "Dispo"), sum.Item2 + bmeng, sum.Item3 + Number(row, "Netwr1"));
        }
        return result;
    }

    /// <summary>Laedt beide Schritte und ersetzt den Stand nur, wenn alles gelesen wurde.</summary>
    internal static async Task<PurchasingMaterialShareLoadResult> RunAsync(
        SqliteConnection conn, SapSingleRequestReader reader, DateOnly today, Func<DateTime> utcNow, CancellationToken cancellationToken)
    {
        // Probe vor der teuren ZC23-Runde: solange ZloTopDownSet nicht in P76 ist, nichts weiter lesen.
        try
        {
            await reader(BomSet, BomSelect, BomFilter(["0"]), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return new PurchasingMaterialShareLoadResult(0, 0, 0, 1, false,
                $"Materialanteil: {BomSet} antwortet nicht (noch nicht transportiert?), nichts gelesen. {ex.Message}");
        }

        var windows = MonthWindows(today);
        var salesRows = new List<Dictionary<string, object?>>();
        foreach (var (from, to) in windows)
            salesRows.AddRange(await reader(SalesSet, SalesSelect, SalesFilter(from, to), cancellationToken));
        var sales = AggregateSales(salesRows);

        var vknrList = sales.Keys.Select(k => k.Vknr).Distinct(StringComparer.Ordinal).OrderBy(v => v, StringComparer.Ordinal).ToList();
        var bom = new Dictionary<(string Vknr, string Idnrk), Dictionary<string, object?>>();
        var failed = 0;
        var firstError = string.Empty;
        foreach (var chunk in vknrList.Chunk(ChunkSize))
        {
            try
            {
                foreach (var row in await reader(BomSet, BomSelect, BomFilter(chunk), cancellationToken))
                {
                    var vknr = PurchasingComponentDispoLoader.NormalizeKey(Text(row, "Vknr"));
                    var idnrk = PurchasingComponentDispoLoader.NormalizeKey(Text(row, "Idnrk"));
                    if (vknr.Length > 0 && idnrk.Length > 0)
                        bom[(vknr, idnrk)] = row;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                failed++;
                if (firstError.Length == 0)
                    firstError = ex.Message;
            }
        }

        var monthFrom = windows[0].From.ToString("yyyyMM", CultureInfo.InvariantCulture);
        var monthTo = windows[^1].From.ToString("yyyyMM", CultureInfo.InvariantCulture);
        if (failed > 0)
            return new PurchasingMaterialShareLoadResult(sales.Count, vknrList.Count, bom.Count, failed, false,
                $"Materialanteil: {failed} von {vknrList.Chunk(ChunkSize).Count()} Stuecklisten-Bloecken ohne Antwort ({firstError}); bisheriger Stand bleibt.");

        var now = utcNow().ToString("O", CultureInfo.InvariantCulture);
        await EnsureSchemaAsync(conn, cancellationToken);
        await using var transaction = (SqliteTransaction)await conn.BeginTransactionAsync(cancellationToken);
        await ExecuteAsync(conn, transaction, "DELETE FROM PurchasingMaterialShareSales; DELETE FROM PurchasingMaterialShareBom;", [], cancellationToken);
        foreach (var (key, value) in sales)
            await ExecuteAsync(conn, transaction,
                "INSERT INTO PurchasingMaterialShareSales (Vknr, Monat, Produktgruppe, Dispo, Bmeng, Netwr, LastLoadedAtUtc) VALUES ($v, $m, $p, $d, $b, $n, $t);",
                [("$v", key.Vknr), ("$m", key.Monat), ("$p", key.Produktgruppe), ("$d", value.Dispo), ("$b", (double)value.Bmeng), ("$n", (double)value.Netwr), ("$t", now)],
                cancellationToken);
        foreach (var ((vknr, idnrk), row) in bom)
            await ExecuteAsync(conn, transaction,
                "INSERT INTO PurchasingMaterialShareBom (Vknr, Idnrk, Menge, Meins, Stk, Wert, Dispo, Matkl, Maktx, LastLoadedAtUtc) VALUES ($v, $i, $me, $u, $s, $w, $d, $k, $x, $t);",
                [("$v", vknr), ("$i", idnrk), ("$me", (double)Number(row, "Menge")), ("$u", Text(row, "Meins")), ("$s", (double)Number(row, "Stk")),
                 ("$w", (double)Number(row, "Wert")), ("$d", Text(row, "Dispo")), ("$k", Text(row, "Matkl")), ("$x", Text(row, "Maktx")), ("$t", now)],
                cancellationToken);
        var message = $"Materialanteil: {sales.Count:N0} ZC23-Summen ({vknrList.Count:N0} verkuerzte Nummern, Termin bestaetigt {monthFrom} bis {monthTo}), {bom.Count:N0} Stuecklistenzeilen Disponent 001 bis 006.";
        await ExecuteAsync(conn, transaction,
            "INSERT OR REPLACE INTO PurchasingMaterialShareState (Id, LoadedAtUtc, MonthFrom, MonthTo, Message) VALUES (1, $t, $f, $o, $msg);",
            [("$t", now), ("$f", monthFrom), ("$o", monthTo), ("$msg", message)], cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new PurchasingMaterialShareLoadResult(sales.Count, vknrList.Count, bom.Count, 0, true, message);
    }

    private static async Task ExecuteAsync(SqliteConnection conn, SqliteTransaction transaction, string sql,
        IReadOnlyList<(string Name, object Value)> parameters, CancellationToken cancellationToken)
    {
        await using var command = conn.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    internal static string Text(IReadOnlyDictionary<string, object?> row, string name)
        => row.TryGetValue(name, out var value) && value is not null ? Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty : string.Empty;

    /// <summary>Edm.Decimal kommt in OData V2 als Zeichenkette, Zahlen werden aber auch als Zahl angenommen.</summary>
    internal static decimal Number(IReadOnlyDictionary<string, object?> row, string name)
    {
        if (!row.TryGetValue(name, out var value) || value is null)
            return 0m;
        return value switch
        {
            decimal d => d,
            double f => (decimal)f,
            long l => l,
            int i => i,
            JsonElement { ValueKind: JsonValueKind.Number } e => e.GetDecimal(),
            _ => decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0m
        };
    }

    /// <summary>SAP-Datum als JJJJMMTT, JJJJ-MM-TT oder /Date(ms)/.</summary>
    internal static DateOnly? SapDate(string text)
    {
        if (text.StartsWith("/Date(", StringComparison.Ordinal))
        {
            var digits = new string(text.Skip(6).TakeWhile(c => char.IsDigit(c) || c == '-').ToArray());
            return long.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ms)
                ? DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime)
                : null;
        }
        if (text.Length >= 8 && DateOnly.TryParseExact(text[..8], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var compact) && compact.Year > 1900)
            return compact;
        if (text.Length >= 10 && DateOnly.TryParseExact(text[..10], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var iso) && iso.Year > 1900)
            return iso;
        return null;
    }
}
