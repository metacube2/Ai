using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Cross-Country-Sparten-Fallback fuer die Excel-Exporte: Nur ZSCHWEIZ bekommt die
/// Produktsparten beim Import (SAP-Join auf ProductDivisionRefSet); alle anderen
/// Laender erben sie ueber die normalisierte Materialnummer von der Zeile, die eine
/// Referenz traegt — dieselbe Logik, die die Dashboards live rechnen
/// (ManagementCockpitService) und das Nachweis-Excel in den Sparten-Blaettern nutzt.
///
/// Bewusst als LOOKUP beim Schreiben, nicht als Mutation der Records: dieselbe
/// Record-Liste wird nach dem Excel auch in die zentrale Audit-CSV geschrieben,
/// und die ist ein Revisionsartefakt, das die Rohdaten zeigen muss.
/// </summary>
public static class ProductReferenceEnricher
{
    public static bool HasProductReference(SalesRecord record)
        => !string.IsNullOrWhiteSpace(record.ProductHierarchyCode) ||
           !string.IsNullOrWhiteSpace(record.ProductFamilyCode) ||
           !string.IsNullOrWhiteSpace(record.ProductDivisionCode) ||
           !string.IsNullOrWhiteSpace(record.ProductMappingAssigned);

    public static bool IsAssignedProductReference(SalesRecord record)
        => IsTruthy(record.ProductMappingAssigned) &&
           !string.IsNullOrWhiteSpace(record.ProductDivisionCode) &&
           !record.ProductDivisionCode.Equals("UNASS", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Baut den Referenzpool: je normalisiertem Material die beste Zeile mit
    /// Produktreferenz (zugeordnete Referenzen gewinnen gegen unzugeordnete).
    /// </summary>
    public static Dictionary<string, SalesRecord> BuildReferenceByMaterial(IEnumerable<SalesRecord> records)
        => records
            .Where(record => !string.IsNullOrWhiteSpace(record.Material))
            .Where(HasProductReference)
            .GroupBy(record => MaterialKeyNormalizer.Normalize(record.Material), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(IsAssignedProductReference)
                    .ThenBy(record => record.Tsc, StringComparer.OrdinalIgnoreCase)
                    .First(),
                StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Liefert die Referenzzeile, von der eine Zeile ohne eigene Produktreferenz
    /// die Sparte erbt — oder null, wenn die Zeile selbst eine Referenz traegt
    /// oder kein Material im Pool passt.
    /// </summary>
    public static SalesRecord? ResolveFallback(
        SalesRecord record,
        IReadOnlyDictionary<string, SalesRecord> referenceByMaterial)
    {
        if (HasProductReference(record) || string.IsNullOrWhiteSpace(record.Material))
            return null;

        return referenceByMaterial.TryGetValue(MaterialKeyNormalizer.Normalize(record.Material), out var reference)
            ? reference
            : null;
    }

    private static bool IsTruthy(string value)
    {
        var normalized = (value ?? string.Empty).Trim().ToUpperInvariant();
        return normalized is "X" or "TRUE" or "1" or "Y" or "YES";
    }
}
