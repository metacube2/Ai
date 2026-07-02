using System.Text.RegularExpressions;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Classifies a sales-line supplier as internal (intercompany), external (3rd party)
/// or unclear for the group-margin (Gruppenmarge) calculation.
///
/// Finance decision (Andreas, 2026-06-29): a supplier counts as internal/intercompany
/// whenever its name or number contains "TRAFAG" - "because we are Trafag", every Trafag
/// company is an intercompany partner. Known short entity codes (TR-AG, TRCH, TRIT, TRIN)
/// are also treated as internal so code-only supplier references are caught.
/// Finance addition (Andreas, 2026-07-01): GFS / Gesellschaft fuer Sensorik is also
/// an internal/intercompany supplier marker.
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
    // GFS catches Gesellschaft fuer Sensorik references that do not include Trafag.
    //
    // These markers are matched on WORD BOUNDARIES, not as raw substrings. Short codes like
    // "TRIT"/"TRIN"/"GFS" would otherwise produce false positives inside unrelated supplier
    // names (e.g. "Triton" -> TRIT, "Trinity" -> TRIN, "AGFS-100" -> GFS) and mark a 3rd
    // party as internal/intercompany, which corrupts the group margin.
    private static readonly string[] InternalMarkers =
    {
        "TRAFAG",
        "TR-AG",
        "TRCH",
        "TRIT",
        "TRIN",
        "GFS",
        "GESELLSCHAFT FUER SENSORIK",
        "GESELLSCHAFT FUR SENSORIK"
    };

    private static readonly Regex InternalMarkerPattern = new(
        @"\b(" + string.Join('|', InternalMarkers.Select(Regex.Escape)) + @")\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string Resolve(string? supplierNumber, string? supplierName, string? supplierCountry)
    {
        if (string.IsNullOrWhiteSpace(supplierNumber) &&
            string.IsNullOrWhiteSpace(supplierName) &&
            string.IsNullOrWhiteSpace(supplierCountry))
        {
            return Unclear;
        }

        var supplierText = string.Join(' ', supplierNumber, supplierName, supplierCountry);
        return InternalMarkerPattern.IsMatch(supplierText) ? Internal : External;
    }
}
