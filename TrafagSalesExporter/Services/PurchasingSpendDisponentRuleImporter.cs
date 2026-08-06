using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Importiert die beiden von der Disposition gelieferten ZDISPO-Listen in eine eigene
/// Zusatz-Tabelle. Bestehende fachliche Zuordnungen werden dabei nicht veraendert.
/// </summary>
internal static class PurchasingSpendDisponentRuleImporter
{
    internal const string GroupFileName = "zdispo_grp.xlsx";
    internal const string TextFileName = "zdispo_spart.xlsx";

    internal static int ImportFromDirectory(AppDbContext db, string directory)
    {
        var groupPath = Path.Combine(directory, GroupFileName);
        var textPath = Path.Combine(directory, TextFileName);
        if (!File.Exists(groupPath) || !File.Exists(textPath))
            return 0;

        IReadOnlyList<PurchasingSpendDisponentRule> rules;
        try
        {
            rules = ReadRules(groupPath, textPath);
        }
        catch
        {
            // Eine defekte Zusatzdatei darf den App-Start und die bisherigen Zuordnungen nicht
            // blockieren. Die zuletzt erfolgreich importierten Regeln bleiben bestehen.
            return 0;
        }

        if (rules.Count == 0)
            return 0;

        using var transaction = db.Database.BeginTransaction();
        db.Database.ExecuteSqlRaw("DELETE FROM PurchasingSpendDisponentRule;");
        var now = DateTime.UtcNow.ToString("O");
        const string source = "zdispo_grp.xlsx + zdispo_spart.xlsx";
        foreach (var rule in rules)
        {
            db.Database.ExecuteSqlInterpolated($@"
INSERT INTO PurchasingSpendDisponentRule
    (DisponentPattern, ProductGroup, ProductGroupText, Source, UpdatedAtUtc)
VALUES
    ({rule.DisponentPattern}, {rule.ProductGroup}, {rule.ProductGroupText}, {source}, {now});");
        }
        transaction.Commit();
        return rules.Count;
    }

    internal static IReadOnlyList<PurchasingSpendDisponentRule> ReadRules(string groupPath, string textPath)
    {
        var descriptions = ReadRows(textPath, "DISPO", "DESCR")
            .GroupBy(row => Normalize(row.Key), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Value.Trim(), StringComparer.OrdinalIgnoreCase);

        // Einzelne Muster kommen in der gelieferten Liste mehrfach vor (aktuell 016/DS1/DS2).
        // Jede Zuordnung bleibt als eigene Regel bestehen. Der Spend-Aufriss behandelt sie wie
        // die bereits bekannten Mehrfachzuordnungen und verteilt den Betrag summenneutral 1/n.
        return ReadRows(groupPath, "DISPO_KZ", "DISPO")
            .Select(row => new
            {
                Pattern = Normalize(row.Key),
                Group = Normalize(row.Value)
            })
            .Where(row => row.Pattern.Length > 0 && row.Group.Length > 0)
            .Distinct()
            .Select(row => new PurchasingSpendDisponentRule(
                row.Pattern,
                row.Group,
                descriptions.TryGetValue(row.Group, out var text) && text.Length > 0 ? text : row.Group))
            .OrderBy(rule => rule.DisponentPattern, StringComparer.OrdinalIgnoreCase)
            .ThenBy(rule => rule.ProductGroup, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<KeyValuePair<string, string>> ReadRows(
        string path,
        string keyHeader,
        string valueHeader)
    {
        using var workbook = new XLWorkbook(path);
        var worksheet = workbook.Worksheets.First();
        var header = worksheet.FirstRowUsed()
            ?? throw new InvalidDataException($"Keine Kopfzeile in {Path.GetFileName(path)}.");
        var keyColumn = FindColumn(header, keyHeader);
        var valueColumn = FindColumn(header, valueHeader);
        if (keyColumn == 0 || valueColumn == 0)
            throw new InvalidDataException($"Spalten {keyHeader}/{valueHeader} fehlen in {Path.GetFileName(path)}.");

        return worksheet.RowsUsed()
            .Skip(1)
            .Select(row => new KeyValuePair<string, string>(
                row.Cell(keyColumn).GetFormattedString().Trim(),
                row.Cell(valueColumn).GetFormattedString().Trim()))
            .Where(row => row.Key.Length > 0 && row.Value.Length > 0)
            .ToList();
    }

    private static int FindColumn(IXLRow header, string name)
        => header.CellsUsed()
            .FirstOrDefault(cell => string.Equals(cell.GetFormattedString().Trim(), name, StringComparison.OrdinalIgnoreCase))
            ?.Address.ColumnNumber ?? 0;

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
}

internal sealed record PurchasingSpendDisponentRule(
    string DisponentPattern,
    string ProductGroup,
    string ProductGroupText);
