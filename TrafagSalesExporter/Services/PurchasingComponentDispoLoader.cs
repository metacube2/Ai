using System.Globalization;
using Microsoft.Data.Sqlite;

namespace TrafagSalesExporter.Services;

/// <summary>Eine Verwendung: Einkaufskomponente in einer verkuerzten Nummer (VKNR) mit deren Disponent.</summary>
internal sealed record PurchasingComponentUsage(string Kompnr, string Vknr, string VknrDispo);

/// <summary>Ergebnis eines Laufs: abgefragte Komponenten, davon mit Verwendung, Fehler und noch offene.</summary>
internal sealed record PurchasingComponentDispoRunResult(int Checked, int WithUsage, int Failed, int Remaining, string Warning);

/// <summary>
/// Produktgruppe im Spend-Aufriss ueber die ZLO03-Stuecklistenaufloesung (Gespraech Armin 2026-10-08, Punkt 4):
/// Einkaufsmaterial (EKPO-MATNR) -> verkuerzte Nummer, in der es verbaut ist (ZPOWERBI_VC_TXT) -> Disponent der
/// verkuerzten Nummer (MARC-DISPO Werk 1100) -> Produktgruppe aus ZC23 (ZDISPO_GRP/ZDISPO_SPART).
///
/// WARUM DIESER LADER: Die Produktgruppen-Perspektive las bis hierhin nur <c>MaterialUsageCache</c>. Den fuellt
/// die Seite Stuecklistenanalyse, und zwar nur fuer die dort eingegebenen Nummern (Produktiv-DB 2026-10-08:
/// 86 Zeilen fuer 5 Materialien). Der "Full Load" ohne Nummer liefert wegen des SAP-seitigen Pflichtfilters
/// nur eine Zeile. Damit lag fast der ganze Einkauf unter "ohne Produktgruppe".
///
/// Gelesen wird das bestehende Set <c>ZSTR_LZCODE_USAGESet</c> Bottom-Up, EINE Anfrage je Komponente (eine
/// OR-Gruppe liefert 0 Zeilen, siehe <see cref="MaterialUsageDataRefreshService.BuildMaterialClauses"/>). Eine
/// Anfrage dauerte produktiv rund eine Viertelsekunde (Lauf 45 vom 2026-07-30: 4 Komponenten in 1 s). Keine
/// SAP-Aenderung noetig. Damit P76 geschont wird, gilt je Lauf ein Zeitbudget; eine Komponente wird erst nach
/// <see cref="RecheckAfter"/> erneut gefragt, auch wenn sie keine Verwendung hatte. Der erste Aufbau verteilt
/// sich so auf mehrere Laeufe.
///
/// Die Abfrage ist vom HTTP getrennt (Delegat), damit Auswahl und Schreiben ohne SAP testbar sind.
/// </summary>
internal static class PurchasingComponentDispoLoader
{
    internal const string UsageSet = "ZSTR_LZCODE_USAGESet";
    internal const string UsageSelect = "Richtung,Vknr,Kompnr,VknrDispo";

    /// <summary>Bottom-Up inklusive loeschvorgemerkter Materialien: auch alte Bestellungen sollen ihre Gruppe finden.</summary>
    internal static readonly string Richtung = MaterialUsageDataRefreshService.BuildRichtungValue(topDown: false, includeDeleted: true);

    internal static readonly TimeSpan RecheckAfter = TimeSpan.FromDays(7);

    /// <summary>Materialnummer wie im Dashboard-Join: Grossbuchstaben, ohne fuehrende Nullen.</summary>
    internal static string NormalizeKey(string? value)
    {
        var text = (value ?? string.Empty).Trim().ToUpperInvariant();
        if (text.Length == 0)
            return string.Empty;
        var trimmed = text.TrimStart('0');
        return trimmed.Length == 0 ? "0" : trimmed;
    }

    internal static string BuildFilter(string kompnr)
        => $"Richtung eq '{Richtung}' and Kompnr eq '{MaterialUsageDataRefreshService.NormalizeMaterialToken(kompnr).Replace("'", "''")}'";

    /// <summary>
    /// Komponenten, die gefragt werden muessen: alle bestellten Materialien, zuerst die nie gefragten, dann die am
    /// laengsten nicht mehr gefragten, aber nur solche, deren letzte Abfrage aelter als <see cref="RecheckAfter"/> ist.
    /// Geliefert wird die Nummer in SAP-Schreibweise (aus EKPO), der Schluessel wird beim Schreiben normalisiert.
    /// </summary>
    internal static async Task<List<string>> SelectDueComponentsAsync(SqliteConnection conn, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var dueBefore = (nowUtc - RecheckAfter).ToString("O", CultureInfo.InvariantCulture);
        await using var command = conn.CreateCommand();
        command.CommandText = @"
WITH Purchased AS (
    SELECT CASE WHEN ltrim(upper(trim(Matnr)), '0') = '' THEN '0' ELSE ltrim(upper(trim(Matnr)), '0') END AS MaterialKey,
           MAX(trim(Matnr)) AS SapMatnr
    FROM PurchasingEkpoCache
    WHERE COALESCE(trim(Matnr), '') <> ''
    GROUP BY 1
)
SELECT p.SapMatnr
FROM Purchased p
LEFT JOIN PurchasingComponentDispoState s ON s.Kompnr = p.MaterialKey
WHERE s.Kompnr IS NULL OR s.CheckedAtUtc < $DueBefore
ORDER BY CASE WHEN s.Kompnr IS NULL THEN 0 ELSE 1 END, s.CheckedAtUtc, p.MaterialKey;";
        command.Parameters.AddWithValue("$DueBefore", dueBefore);
        var result = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add(reader.GetString(0));
        return result;
    }

    /// <summary>Wandelt die SAP-Zeilen einer Komponente in Verwendungen; Zeilen ohne VKNR fallen weg, Doppelte zaehlen einmal.</summary>
    internal static List<PurchasingComponentUsage> ParseRows(string kompnr, IEnumerable<Dictionary<string, object?>> rows)
    {
        var key = NormalizeKey(kompnr);
        return rows
            .Select(row => new PurchasingComponentUsage(key, NormalizeKey(Text(row, "Vknr")), Text(row, "VknrDispo").ToUpperInvariant()))
            .Where(usage => usage.Vknr.Length > 0)
            .GroupBy(usage => usage.Vknr, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.FirstOrDefault(usage => usage.VknrDispo.Length > 0) ?? group.First())
            .ToList();
    }

    /// <summary>
    /// Ersetzt die Verwendungen der gefragten Komponenten und vermerkt den Abfragezeitpunkt, auch bei 0 Treffern.
    /// Komponenten mit Lesefehler fehlen in <paramref name="answered"/> und behalten ihren alten Stand.
    /// </summary>
    internal static async Task WriteAsync(
        SqliteConnection conn,
        IReadOnlyDictionary<string, List<PurchasingComponentUsage>> answered,
        string checkedAtUtc,
        CancellationToken cancellationToken)
    {
        if (answered.Count == 0)
            return;

        await using var transaction = (SqliteTransaction)await conn.BeginTransactionAsync(cancellationToken);
        await using var delete = conn.CreateCommand();
        delete.Transaction = transaction;
        delete.CommandText = "DELETE FROM PurchasingComponentDispoCache WHERE Kompnr = $Kompnr;";
        var deleteKey = delete.Parameters.Add("$Kompnr", SqliteType.Text);

        await using var insert = conn.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = "INSERT OR REPLACE INTO PurchasingComponentDispoCache (Kompnr, Vknr, VknrDispo, LastLoadedAtUtc) VALUES ($Kompnr, $Vknr, $VknrDispo, $At);";
        var insertKey = insert.Parameters.Add("$Kompnr", SqliteType.Text);
        var vknr = insert.Parameters.Add("$Vknr", SqliteType.Text);
        var dispo = insert.Parameters.Add("$VknrDispo", SqliteType.Text);
        insert.Parameters.AddWithValue("$At", checkedAtUtc);

        await using var state = conn.CreateCommand();
        state.Transaction = transaction;
        state.CommandText = "INSERT OR REPLACE INTO PurchasingComponentDispoState (Kompnr, CheckedAtUtc, UsageCount) VALUES ($Kompnr, $At, $Count);";
        var stateKey = state.Parameters.Add("$Kompnr", SqliteType.Text);
        var count = state.Parameters.Add("$Count", SqliteType.Integer);
        state.Parameters.AddWithValue("$At", checkedAtUtc);

        foreach (var (kompnr, usages) in answered)
        {
            var key = NormalizeKey(kompnr);
            deleteKey.Value = key;
            await delete.ExecuteNonQueryAsync(cancellationToken);
            foreach (var usage in usages)
            {
                insertKey.Value = key;
                vknr.Value = usage.Vknr;
                dispo.Value = usage.VknrDispo;
                await insert.ExecuteNonQueryAsync(cancellationToken);
            }

            stateKey.Value = key;
            count.Value = usages.Count;
            await state.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    /// <summary>
    /// Ein Lauf: faellige Komponenten mit wenigen parallelen Anfragen abfragen, bis das Zeitbudget aufgebraucht ist,
    /// und in Bloecken schreiben. Ein 404 (Set fehlt) bricht sofort ab, ohne den Cache anzufassen.
    /// </summary>
    internal static async Task<PurchasingComponentDispoRunResult> RunAsync(
        SqliteConnection conn,
        SapEntitySetReader reader,
        TimeSpan budget,
        int parallelism,
        Func<DateTime> utcNow,
        CancellationToken cancellationToken)
    {
        var due = await SelectDueComponentsAsync(conn, utcNow(), cancellationToken);
        var deadline = utcNow() + budget;
        var checkedCount = 0;
        var withUsage = 0;
        var failed = 0;
        var warning = string.Empty;
        var index = 0;

        while (index < due.Count && utcNow() < deadline)
        {
            var block = due.Skip(index).Take(Math.Max(1, parallelism) * 25).ToList();
            index += block.Count;
            var answered = new Dictionary<string, List<PurchasingComponentUsage>>(StringComparer.OrdinalIgnoreCase);
            using var gate = new SemaphoreSlim(Math.Max(1, parallelism));
            var tasks = block.Select(async kompnr =>
            {
                await gate.WaitAsync(cancellationToken);
                try
                {
                    var rows = await reader(UsageSet, UsageSelect, BuildFilter(kompnr), string.Empty, cancellationToken);
                    return (kompnr, usages: ParseRows(kompnr, rows), error: (Exception?)null);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    return (kompnr, usages: new List<PurchasingComponentUsage>(), error: ex);
                }
                finally
                {
                    gate.Release();
                }
            }).ToList();

            foreach (var outcome in await Task.WhenAll(tasks))
            {
                if (outcome.error is null)
                {
                    answered[outcome.kompnr] = outcome.usages;
                    checkedCount++;
                    if (outcome.usages.Count > 0)
                        withUsage++;
                }
                else
                {
                    failed++;
                    if (warning.Length == 0)
                        warning = $"{outcome.error.GetType().Name}: {outcome.error.Message}";
                }
            }

            await WriteAsync(conn, answered, utcNow().ToString("O", CultureInfo.InvariantCulture), cancellationToken);

            // Antwortet das Set fuer einen ganzen Block nur mit Fehlern (z.B. 404, Anmeldung), lohnt kein weiterer Versuch.
            if (answered.Count == 0)
                break;
        }

        var remaining = Math.Max(0, due.Count - checkedCount);
        return new PurchasingComponentDispoRunResult(checkedCount, withUsage, failed, remaining, warning);
    }

    private static string Text(Dictionary<string, object?> row, string key)
        => row.TryGetValue(key, out var value)
            ? (Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty).Trim()
            : string.Empty;
}
