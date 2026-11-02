using System.Globalization;
using Microsoft.Data.Sqlite;

namespace TrafagSalesExporter.Services;

/// <summary>Eine Zeile der aufklappbaren Tabelle: Produktgruppe, Materialgruppe oder Einkaufsteil.</summary>
public sealed record PurchasingMaterialShareNode(
    string Key,
    string Label,
    decimal Value,
    decimal Revenue,
    decimal Quantity,
    string Unit,
    decimal UnitCost,
    IReadOnlyList<PurchasingMaterialShareNode> Children);

/// <summary>Stand der geladenen Daten (Zeitpunkt, Monatsbereich, Meldung des letzten Laufs).</summary>
public sealed record PurchasingMaterialShareState(string LoadedAtUtc, string MonthFrom, string MonthTo, string Message);

/// <summary>
/// Liest die Tabellen von <see cref="PurchasingMaterialShareLoader"/> und baut die drei Stufen
/// Produktgruppe -> Materialgruppe -> Einkaufsteil (Armins Text, Entscheid Ingo 2026-11-02):
/// Materialanteil = bestaetigte ZC23-Menge der verkuerzten Nummer im Zeitraum x Stuecklistenmenge je 1 VKNR x Stueckkosten,
/// nur Komponenten der Einkaufsdisponenten 001 bis 006 (das filtert schon der Lader). Umsatz bestaetigt = ZC23-Nettowert.
/// </summary>
public static class PurchasingMaterialShareQuery
{
    public const string NoProductGroup = "ohne Produktgruppe";

    public static async Task<PurchasingMaterialShareState?> ReadStateAsync(SqliteConnection conn, CancellationToken cancellationToken)
    {
        await PurchasingMaterialShareLoader.EnsureSchemaAsync(conn, cancellationToken);
        await using var command = conn.CreateCommand();
        command.CommandText = "SELECT LoadedAtUtc, MonthFrom, MonthTo, Message FROM PurchasingMaterialShareState WHERE Id = 1;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new PurchasingMaterialShareState(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3))
            : null;
    }

    /// <summary>Baum fuer die Monate <paramref name="monthFrom"/> bis <paramref name="monthTo"/> (JJJJMM, beide eingeschlossen).</summary>
    public static async Task<IReadOnlyList<PurchasingMaterialShareNode>> LoadAsync(
        SqliteConnection conn, string monthFrom, string monthTo, CancellationToken cancellationToken)
    {
        await PurchasingMaterialShareLoader.EnsureSchemaAsync(conn, cancellationToken);

        var sales = new List<(string Vknr, string Group, decimal Bmeng, decimal Netwr)>();
        await using (var command = conn.CreateCommand())
        {
            command.CommandText = @"SELECT Vknr, Produktgruppe, SUM(Bmeng), SUM(Netwr) FROM PurchasingMaterialShareSales
WHERE Monat >= $f AND Monat <= $t GROUP BY Vknr, Produktgruppe;";
            command.Parameters.AddWithValue("$f", monthFrom);
            command.Parameters.AddWithValue("$t", monthTo);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                sales.Add((reader.GetString(0), reader.GetString(1), (decimal)reader.GetDouble(2), (decimal)reader.GetDouble(3)));
        }

        var bom = new Dictionary<string, List<BomRow>>(StringComparer.Ordinal);
        await using (var command = conn.CreateCommand())
        {
            command.CommandText = "SELECT Vknr, Idnrk, Menge, Meins, Stk, Wert, Matkl, Maktx FROM PurchasingMaterialShareBom;";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new BomRow(reader.GetString(1), (decimal)reader.GetDouble(2), reader.GetString(3),
                    (decimal)reader.GetDouble(4), (decimal)reader.GetDouble(5), reader.GetString(6), reader.GetString(7));
                if (!bom.TryGetValue(reader.GetString(0), out var list))
                    bom[reader.GetString(0)] = list = [];
                list.Add(row);
            }
        }

        return Build(sales, bom);
    }

    public sealed record BomRow(string Idnrk, decimal Menge, string Meins, decimal Stk, decimal Wert, string Matkl, string Maktx);

    /// <summary>Reine Rechnung, ohne Datenbank (Tests).</summary>
    public static IReadOnlyList<PurchasingMaterialShareNode> Build(
        IEnumerable<(string Vknr, string Group, decimal Bmeng, decimal Netwr)> sales,
        IReadOnlyDictionary<string, List<BomRow>> bom)
    {
        var groups = new Dictionary<string, GroupAcc>(StringComparer.Ordinal);
        foreach (var (vknr, groupName, bmeng, netwr) in sales)
        {
            var name = string.IsNullOrWhiteSpace(groupName) ? NoProductGroup : groupName.Trim();
            if (!groups.TryGetValue(name, out var group))
                groups[name] = group = new GroupAcc();
            group.Revenue += netwr;
            if (!bom.TryGetValue(vknr, out var rows))
                continue;
            foreach (var row in rows)
            {
                var matkl = row.Matkl.Trim();
                if (!group.MaterialGroups.TryGetValue(matkl, out var materials))
                    group.MaterialGroups[matkl] = materials = new Dictionary<string, PartAcc>(StringComparer.Ordinal);
                if (!materials.TryGetValue(row.Idnrk, out var part))
                    materials[row.Idnrk] = part = new PartAcc(row.Maktx, row.Meins, row.Stk);
                part.Quantity += bmeng * row.Menge;
                part.Value += bmeng * row.Wert;
            }
        }

        return groups
            .Select(g =>
            {
                var materialGroups = g.Value.MaterialGroups
                    .Select(mg =>
                    {
                        var parts = mg.Value
                            .Select(p => new PurchasingMaterialShareNode($"{g.Key}|{mg.Key}|{p.Key}",
                                p.Value.Text.Length == 0 ? p.Key : $"{p.Key} {p.Value.Text}",
                                Math.Round(p.Value.Value, 2), 0m, Math.Round(p.Value.Quantity, 3), p.Value.Unit, p.Value.UnitCost, []))
                            .OrderByDescending(n => n.Value).ThenBy(n => n.Key, StringComparer.Ordinal)
                            .ToList();
                        var label = mg.Key.Length == 0 ? "ohne Warengruppe" : PurchasingMaterialGroupTextCatalog.Resolve(mg.Key);
                        return new PurchasingMaterialShareNode($"{g.Key}|{mg.Key}", label, parts.Sum(n => n.Value), 0m, 0m, string.Empty, 0m, parts);
                    })
                    .OrderByDescending(n => n.Value).ThenBy(n => n.Key, StringComparer.Ordinal)
                    .ToList();
                return new PurchasingMaterialShareNode(g.Key, g.Key, materialGroups.Sum(n => n.Value), Math.Round(g.Value.Revenue, 2), 0m, string.Empty, 0m, materialGroups);
            })
            .OrderByDescending(n => n.Value).ThenBy(n => n.Key, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Monatsliste JJJJMM von <paramref name="from"/> bis <paramref name="to"/> fuer die Auswahl.</summary>
    public static List<string> Months(string from, string to)
    {
        var result = new List<string>();
        if (!DateOnly.TryParseExact(from + "01", "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var month) ||
            !DateOnly.TryParseExact(to + "01", "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var last))
            return result;
        for (; month <= last; month = month.AddMonths(1))
            result.Add(month.ToString("yyyyMM", CultureInfo.InvariantCulture));
        return result;
    }

    private sealed class GroupAcc
    {
        public decimal Revenue;
        public Dictionary<string, Dictionary<string, PartAcc>> MaterialGroups { get; } = new(StringComparer.Ordinal);
    }

    private sealed class PartAcc(string text, string unit, decimal unitCost)
    {
        public string Text { get; } = text.Trim();
        public string Unit { get; } = unit.Trim();
        public decimal UnitCost { get; } = unitCost;
        public decimal Quantity;
        public decimal Value;
    }
}
