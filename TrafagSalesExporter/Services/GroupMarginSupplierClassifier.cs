namespace TrafagSalesExporter.Services;

/// <summary>
/// Classifies a sales-line supplier as internal (intercompany), external (3rd party)
/// or unclear for the group-margin (Gruppenmarge) calculation.
///
/// Finance decision (Andreas, Gruppenmarge-Entscheidungsbogen 2026-06): exactly THREE
/// Trafag entities count as internal — Trafag AG (CH), Trafag Italy and Trafag India.
/// Every other supplier — including other Trafag/intercompany entities such as Magnetic
/// Sense — is treated as 3rd party (external) for the cost basis.
///
/// Open follow-up (Gruppenmarge Q1, not yet decided): whether to drive this from SAP
/// master data / a dedicated intercompany table (Entity + Partner number) instead of
/// name/number matching. Until that is decided, this fixed whitelist is the agreed rule.
/// </summary>
public static class GroupMarginSupplierClassifier
{
    public const string Internal = "Intern";
    public const string External = "Extern";
    public const string Unclear = "Unklar";

    // Markers identifying the three internal Trafag entities. Kept specific on purpose:
    // a bare "TRAFAG" match would wrongly classify every Trafag company as internal.
    private static readonly string[] InternalMarkers =
    {
        "TRCH", "TR-AG", "TRAFAG AG",   // Trafag AG (Switzerland)
        "TRIT", "TRAFAG ITAL",          // Trafag Italy (Italy / Italia)
        "TRIN", "TRAFAG INDIA"          // Trafag India
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
