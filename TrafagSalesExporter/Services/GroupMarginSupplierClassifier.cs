using System.Text.RegularExpressions;
using TrafagSalesExporter.Models;

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

    // Entscheid Ingo, 2026-07-29: CH/AT (Site "ZSCHWEIZ", TSC TRCH/TRAT) verkauft immer als
    // Trafag AG selbst - eine Zeile mit dieser TSC ist damit per Definition intercompany/TR AG,
    // unabhaengig von den Supplier-Feldern. Die SAP-OData-Quelle (FinanzdataSchweizOeSet, siehe
    // docs/FINANCE_VBRP_WAVWR_SPEZ_2026-07-16.md) hat kein Lieferantenfeld - eine Faktura hat
    // einen Kunden, keinen Vorlieferanten - und wird auch nie eines haben, weil VBRP kein
    // Einkaufsbeleg ist. Ohne diese Regel bleiben alle CH/AT-Zeilen trotz vorhandener
    // Kostenbasis (WAVWR/STPRS) auf "Lieferant unklar" stehen (siehe
    // docs/FINANCE_SUPPLIER_LUECKE_ANALYSE_2026-07-28.md Abschnitt 3).
    private static readonly string[] IntercompanySellingTsc = { "TRCH", "TRAT" };

    private static bool IsIntercompanySellingTsc(string? tsc)
        => !string.IsNullOrWhiteSpace(tsc) &&
           IntercompanySellingTsc.Contains(tsc.Trim(), StringComparer.OrdinalIgnoreCase);

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

    public static string Resolve(
        string? supplierNumber,
        string? supplierName,
        string? supplierCountry,
        string? tsc = null,
        string? normalizedMaterialKey = null,
        IReadOnlyDictionary<(string MaterialKey, string ValuationArea), GroupStandardCost>? groupStandardCosts = null)
    {
        if (IsIntercompanySellingTsc(tsc))
            return Internal;

        if (string.IsNullOrWhiteSpace(supplierNumber) &&
            string.IsNullOrWhiteSpace(supplierName) &&
            string.IsNullOrWhiteSpace(supplierCountry))
        {
            return HasGroupCostMatch(normalizedMaterialKey, groupStandardCosts) ? Internal : Unclear;
        }

        var supplierText = string.Join(' ', supplierNumber, supplierName, supplierCountry);
        return InternalMarkerPattern.IsMatch(supplierText) ? Internal : External;
    }

    // Uebergangsregel (Andreas/Ingo, Meeting 2026-07-30): solange die B1-Lieferantenfelder
    // (CardCode) bei den meisten Gesellschaften nicht gepflegt sind, wird ein Material OHNE
    // jegliche Supplier-Angabe als intern gewertet, wenn es in der Konzern-Kostentabelle
    // (GroupStandardCosts, siehe Models/GroupStandardCost.cs) einer bekannten liefernden
    // Gesellschaft vorkommt. AUSDRUECKLICH ALS PROVISORIUM markiert: beide im Meeting einig,
    // dass das "nicht wasserdicht" ist - die Tabelle kann falsch positive Eintraege enthalten
    // (Beispiel aus dem Meeting: ein "Thermostat"-Artikel in der TR-AG/CH-Tabelle, der dort
    // nicht hingehoert), was ein tatsaechlich extern beschafftes Material bei einer anderen
    // Gesellschaft faelschlich als intern klassifizieren wuerde. Nur ein Fallback fuer den
    // Unklar-Fall - ueberschreibt nie ein bereits per Supplier-Text/TSC ermitteltes Extern.
    private static bool HasGroupCostMatch(
        string? normalizedMaterialKey,
        IReadOnlyDictionary<(string MaterialKey, string ValuationArea), GroupStandardCost>? groupStandardCosts)
    {
        if (string.IsNullOrWhiteSpace(normalizedMaterialKey) || groupStandardCosts is null || groupStandardCosts.Count == 0)
            return false;

        foreach (var area in GroupStandardCostAreas.ByEntity.Values)
        {
            if (groupStandardCosts.ContainsKey((normalizedMaterialKey, area)))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Welche liefernde Trafag-Gesellschaft ein interner Lieferant konkret ist (fuer die
    /// Konzern-Kostenbasis, siehe <see cref="GroupStandardCostEntities"/>). Basiert auf
    /// <c>SupplierName</c>-Klartext, NICHT auf <c>SupplierNumber</c>: die Nummer ist je TSC
    /// unterschiedlich verschluesselt (TR AG heisst bei TRIT z. B. "S_CH01_0065180", bei
    /// TRDE "60000", bei TRIN "V0078"), waehrend der Name across TSCs stabil ist
    /// (Stichprobe 2026-07-15 auf Produktivdaten-Snapshot: 8'995 interne Zeilen, 0
    /// Kollisionen). Liefert null, wenn keine der bekannten Gesellschaften erkannt wird
    /// (z. B. GFS/Gesellschaft fuer Sensorik — dafuer ist noch keine eigene Kostenquelle
    /// verifiziert).
    /// </summary>
    public static string? ResolveDeliveringEntity(
        string? supplierName,
        string? tsc = null,
        string? normalizedMaterialKey = null,
        IReadOnlyDictionary<(string MaterialKey, string ValuationArea), GroupStandardCost>? groupStandardCosts = null)
    {
        if (IsIntercompanySellingTsc(tsc))
            return GroupStandardCostEntities.TrAg;

        if (!string.IsNullOrWhiteSpace(supplierName))
        {
            var name = supplierName.Trim();
            if (name.Contains("Trafag AG", StringComparison.OrdinalIgnoreCase))
                return GroupStandardCostEntities.TrAg;
            if (name.Contains("Trafag Italia", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Trafag Italy", StringComparison.OrdinalIgnoreCase))
                return GroupStandardCostEntities.TrIt;
            if (name.Contains("Trafag Controls India", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Trafag India", StringComparison.OrdinalIgnoreCase))
                return GroupStandardCostEntities.TrIn;
            return null;
        }

        // Gleiches Uebergangs-Provisorium wie in Resolve(): ohne jegliche Supplier-Angabe
        // wird die liefernde Gesellschaft ueber die Konzern-Kostentabelle geraten, nicht ueber
        // den (leeren) Supplier-Text. Nur relevant, wenn Resolve() bereits "Intern" geliefert
        // haette (HasGroupCostMatch) - siehe dortigen Kommentar zum Thermostat-Vorbehalt.
        if (string.IsNullOrWhiteSpace(normalizedMaterialKey) || groupStandardCosts is null || groupStandardCosts.Count == 0)
            return null;

        foreach (var (entity, area) in GroupStandardCostAreas.ByEntity)
        {
            if (groupStandardCosts.ContainsKey((normalizedMaterialKey, area)))
                return entity;
        }

        return null;
    }
}

/// <summary>
/// Bekannte liefernde Trafag-Gesellschaften fuer die Konzern-Kostenbasis (Mappe1.xlsx).
/// Nur <see cref="TrAg"/> hat aktuell eine verifiziert befuellte Kostenquelle
/// (MBEW-STPRS, Bewertungskreis 1100 -> <see cref="GroupStandardCost"/>). TR IN/TR IT
/// sind als Konstanten vorhanden, damit der Lieferant korrekt beschriftet wird, liefern
/// aber bewusst (noch) keine Kostenzahl (siehe docs/FINANCE_GRUPPENMARGE_2026-06-16.md
/// Nachtrag 2026-07-15).
/// </summary>
public static class GroupStandardCostEntities
{
    public const string TrAg = "TR_AG";
    public const string TrIt = "TR_IT";
    public const string TrIn = "TR_IN";
}

/// <summary>
/// Liefernde Gesellschaft -> MBEW-Bewertungskreis (nur fuer Gesellschaften mit
/// verifizierter Kostenquelle). TR AG = Bewertungskreis 1100, Hauswaehrung CHF
/// (siehe StandardCostEnricher.ValuationAreaByCountry / docs/FINANCE_STANDARDKOSTEN_2026-07-14.md).
/// </summary>
public static class GroupStandardCostAreas
{
    public static readonly IReadOnlyDictionary<string, string> ByEntity = new Dictionary<string, string>
    {
        [GroupStandardCostEntities.TrAg] = "1100"
    };

    public static readonly IReadOnlyDictionary<string, string> CurrencyByEntity = new Dictionary<string, string>
    {
        [GroupStandardCostEntities.TrAg] = "CHF"
    };
}
