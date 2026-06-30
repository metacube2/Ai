namespace TrafagSalesExporter.Services;

/// <summary>
/// Classifies a sales-line supplier as internal (intercompany), external (3rd party)
/// or unclear for the group-margin (Gruppenmarge) calculation.
///
/// Finance decision (Andreas, 2026-06-29): a supplier counts as internal/intercompany
/// whenever its name or number contains "TRAFAG" — "because we are Trafag", every Trafag
/// company is an intercompany partner. Known short entity codes (TR-AG, TRCH, TRIT, TRIN)
/// are also treated as internal so code-only supplier references are caught.
///
/// Note: detecting a supplier as internal is separate from the COST BASIS. We only have
/// real group standard costs for the entities that report them (TR AG via MBEW-STPRS,
/// TR IN via SAP B1, TR IT); for internal suppliers without a group cost source the basis
/// falls back like 3rd party. That group-cost sourcing is a separate feature (see Mappe1).
/// </summary>
public static class GroupMarginSupplierClassifier
{
    public const string Internal = "Intern";
    public const string External = "Extern";
    public const string Unclear = "Unklar";

    // "TRAFAG" is the leading marker (covers Trafag AG, Trafag Italy, Trafag India, Trafag
    // GmbH, ...). The short codes catch supplier references that only use the entity code.
    private static readonly string[] InternalMarkers =
    {
        "TRAFAG", "TR-AG", "TRCH", "TRIT", "TRIN"
    };

    public static string Resolve(string? supplierNumber, string? supplierName, string? supplierCountry)
    {
        if (string.IsNullOrWhiteSpace(supplierNumber) &&
            string.IsNullOrWhiteSpace(supplierName) &&
            string.IsNullOrWhiteSpace(supplierCountry))
        {
            return Unclear;
        }

        var supplierText = string.Join(' ', supplierNumber, supplierName, supplierCountry).ToUpperInvariant();
        foreach (var marker in InternalMarkers)
        {
            if (supplierText.Contains(marker, StringComparison.OrdinalIgnoreCase))
                return Internal;
        }

        return External;
    }
}
