using System.Text.RegularExpressions;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Classifies a sales-line supplier as internal (intercompany), external (explicit
/// 3rd party), local (verified CH-master non-match) or unclear for the group-margin
/// (Gruppenmarge) calculation.
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
    public const string Local = "Lokal";
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
        "TR AG",
        "TRCH",
        "TRIT",
        "TR IT",
        "TRIN",
        "TR IN",
        "GFS",
        "GESELLSCHAFT FUER SENSORIK",
        "GESELLSCHAFT FUR SENSORIK"
    };

    private static readonly Regex InternalMarkerPattern = new(
        @"\b(" + string.Join('|', InternalMarkers.Select(Regex.Escape)) + @")\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex TrAgEntityPattern = new(
        @"\b(?:TRAFAG\s+AG|TR[\s-]?AG)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex TrItEntityPattern = new(
        @"\b(?:TRAFAG\s+(?:ITALIA|ITALY)|TR[\s-]?IT)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex TrInEntityPattern = new(
        @"\b(?:TRAFAG\s+(?:CONTROLS\s+)?INDIA|TR[\s-]?IN)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string Resolve(
        string? supplierNumber,
        string? supplierName,
        string? supplierCountry,
        string? tsc = null,
        string? normalizedMaterialKey = null,
        IReadOnlyDictionary<(string MaterialKey, string ValuationArea), GroupStandardCost>? groupStandardCosts = null,
        string? salesType = null,
        IReadOnlySet<string>? chPlantMaterialKeys = null,
        string? supplierFallbackMode = null)
    {
        if (IsIntercompanySellingTsc(tsc))
            return Internal;

        // ENTSCHEID INGO, 2026-08-27: Wo die Quelle einen Sales Type fuehrt, entscheidet DIESER -
        // auch dann, wenn die Lieferantenfelder etwas anderes sagen. Damit ist die Frage
        // beantwortet, die hier seit dem 2026-08-05 als offen stand: produktiv widersprechen
        // sich beide Angaben bei 10 TRIN-Artikeln (Sales Type FFM, aber Lieferant gepflegt).
        // Bis heute gewann der Lieferantentext, jetzt gewinnt der Sales Type.
        //
        // Begruendung: Der Sales Type ist die verrechnungspreisliche Rolle aus dem
        // Artikelstamm und damit die fachlich gesetzte Aussage; die Lieferantenfelder sind in
        // Indien nachweislich unvollstaendig und teilweise widerspruechlich gepflegt. Praktisch
        // betrifft die Regel nur TRIN, weil bisher nur Indien das Feld fuehrt - Italien hat es
        // bei allen 19'968 Zeilen leer (gemessen 2026-08-27).
        //
        // Alle drei Werte bezeichnen eine Konzernrolle, keinen Fremdbezug: FFM und CM fertigt
        // der Standort selbst (bei CM im Auftrag von Trafag AG), LRD bezieht von Trafag AG.
        // Siehe docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md Abschnitt 3b.
        var role = ResolveSalesTypeRole(salesType);
        if (role is not null)
            return Internal;

        // Erst ohne gepflegten Sales Type zaehlt der Lieferantentext.
        if (!string.IsNullOrWhiteSpace(supplierNumber) ||
            !string.IsNullOrWhiteSpace(supplierName) ||
            !string.IsNullOrWhiteSpace(supplierCountry))
        {
            var supplierText = string.Join(' ', supplierNumber, supplierName, supplierCountry);
            return InternalMarkerPattern.IsMatch(supplierText) ? Internal : External;
        }

        if (HasMaterialFallbackMatch(
                normalizedMaterialKey, groupStandardCosts, chPlantMaterialKeys, supplierFallbackMode))
            return Internal;

        // Entscheid Andreas/Ingo, Meeting 2026-08-11 (Transkript 06:31-07:16): Ist ein
        // pruefbarer Artikel NICHT im CH-Werkstamm enthalten, werden die Standardkosten der
        // jeweiligen lokalen Gesellschaft verwendet. "Lokal" ist bewusst praeziser als
        // "Extern": Ein Nichttreffer kann lokaler Einkauf oder lokale Fertigung sein.
        // Nur ein geladener MARC-Cache und ein vorhandener Materialschluessel erlauben diese
        // Aussage. Bei fehlender Pruefgrundlage bleibt die Zeile weiterhin ehrlich "Unklar".
        return IsConfirmedLocalMaterial(
            tsc, normalizedMaterialKey, chPlantMaterialKeys, supplierFallbackMode)
            ? Local
            : Unclear;
    }

    /// <summary>
    /// Verrechnungspreisliche Rolle aus dem Rohwert des Artikelstammfeldes „Sales Type"
    /// (Indien: <c>OITM.U_Tasc_ST</c>) auf die liefernde Gesellschaft abgebildet.
    ///
    /// <list type="bullet">
    /// <item><c>FFM</c> - Full Fledged Manufacturing: der Standort fertigt auf eigene Rechnung.</item>
    /// <item><c>CM</c> - Contract Manufacturing: der Standort fertigt im Auftrag von Trafag AG
    /// und fakturiert an sie. Liefernde Gesellschaft ist trotzdem der Standort, denn dort
    /// entstehen die Herstellkosten (belegt 2026-08-05: Kunde beider CM-Artikel ist
    /// ausschliesslich Trafag AG, Aufschlag 31.2 % und 31.7 %, Zeichnungsnummer vorhanden,
    /// kein Vorlieferant).</item>
    /// <item><c>LRD</c> - Limited Risk Distributor: Ware ist in der Schweiz hergestellt und wird
    /// von Trafag AG bezogen. Belegt 2026-08-05: alle 93 LRD-Artikel mit gepflegtem Lieferanten
    /// zeigen auf V0078 = Trafag AG, ohne Ausnahme.</item>
    /// </list>
    ///
    /// Liefert null bei leerem oder unbekanntem Wert - dann wird nicht geraten.
    /// </summary>
    public static string? ResolveSalesTypeRole(string? salesType)
    {
        var value = salesType?.Trim();
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.ToUpperInvariant() switch
        {
            "FFM" => SalesTypeRoles.OwnManufacturing,
            "CM" => SalesTypeRoles.ContractManufacturing,
            "LRD" => SalesTypeRoles.GroupDistribution,
            _ => null
        };
    }

    // Alte, weiterhin umschaltbare Uebergangsregel aus dem Meeting 2026-07-30:
    // Material ohne Supplier-Angabe wird bei einem Treffer in GroupStandardCosts intern.
    // Seit Ingos Entscheid 2026-08-11 ist MARC/Werk 1100 der neue Standard; diese Methode
    // bleibt fuer den expliziten Alt-Modus und als Verfuegbarkeitsfallback bei leerem
    // MARC-Cache erhalten. Ein expliziter Supplier wird nie ueberschrieben.
    private static bool HasTrAgGroupCostMatch(
        string? normalizedMaterialKey,
        IReadOnlyDictionary<(string MaterialKey, string ValuationArea), GroupStandardCost>? groupStandardCosts)
    {
        if (string.IsNullOrWhiteSpace(normalizedMaterialKey) || groupStandardCosts is null || groupStandardCosts.Count == 0)
            return false;

        var trAgArea = GroupStandardCostAreas.ByEntity[GroupStandardCostEntities.TrAg];
        return groupStandardCosts.ContainsKey((normalizedMaterialKey, trAgArea));
    }

    /// <summary>
    /// Umschaltbarer Supplier-Fallback. Der neue Standard prueft MARC/Werk 1100.
    /// Ist dieser Cache noch leer (z. B. direkt nach einer Migration), bleibt die
    /// Auswertung verfuegbar und verwendet voruebergehend den bisherigen MBEW-Fallback.
    /// </summary>
    private static bool HasMaterialFallbackMatch(
        string? normalizedMaterialKey,
        IReadOnlyDictionary<(string MaterialKey, string ValuationArea), GroupStandardCost>? groupStandardCosts,
        IReadOnlySet<string>? chPlantMaterialKeys,
        string? supplierFallbackMode)
    {
        var key = normalizedMaterialKey?.Trim() ?? string.Empty;
        if (key.Length == 0)
            return false;

        var mode = SupplierFallbackModes.Normalize(supplierFallbackMode);
        if (mode == SupplierFallbackModes.ChPlantMaster && chPlantMaterialKeys is { Count: > 0 })
            return chPlantMaterialKeys.Contains(key);

        return HasTrAgGroupCostMatch(key, groupStandardCosts);
    }

    private static bool IsConfirmedLocalMaterial(
        string? tsc,
        string? normalizedMaterialKey,
        IReadOnlySet<string>? chPlantMaterialKeys,
        string? supplierFallbackMode)
    {
        var key = normalizedMaterialKey?.Trim() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(tsc) &&
               key.Length > 0 &&
               SupplierFallbackModes.Normalize(supplierFallbackMode) == SupplierFallbackModes.ChPlantMaster &&
               chPlantMaterialKeys is { Count: > 0 } &&
               !chPlantMaterialKeys.Contains(key);
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
        IReadOnlyDictionary<(string MaterialKey, string ValuationArea), GroupStandardCost>? groupStandardCosts = null,
        string? salesType = null,
        IReadOnlySet<string>? chPlantMaterialKeys = null,
        string? supplierFallbackMode = null,
        string? supplierNumber = null,
        string? supplierCountry = null)
    {
        if (IsIntercompanySellingTsc(tsc))
            return GroupStandardCostEntities.TrAg;

        // Entscheid Ingo, 2026-08-27, identisch zu Resolve(): der gepflegte Sales Type geht vor
        // den Lieferantenfeldern. Beide Methoden MUESSEN dieselbe Reihenfolge haben, sonst
        // laufen Klassifikation und Kostenpfad auseinander - genau der Fehler, wegen dem die
        // Rechnung 2026-08-05 in GroupMarginCalculator zusammengelegt wurde.
        //
        // FFM und CM fertigt der Standort selbst, liefernde Gesellschaft ist damit der Standort.
        // LRD kommt von Trafag AG - und damit greift die Schweizer Kostenquelle (MBEW-STPRS,
        // Bewertungskreis 1100), so wie Ingo es am 2026-08-27 festgelegt hat.
        var role = ResolveSalesTypeRole(salesType);
        if (role is not null)
        {
            return role switch
            {
                SalesTypeRoles.GroupDistribution => GroupStandardCostEntities.TrAg,
                _ => ResolveSiteEntity(tsc)
            };
        }

        if (!string.IsNullOrWhiteSpace(supplierNumber) ||
            !string.IsNullOrWhiteSpace(supplierName) ||
            !string.IsNullOrWhiteSpace(supplierCountry))
        {
            var name = supplierName?.Trim() ?? string.Empty;
            if (TrAgEntityPattern.IsMatch(name))
                return GroupStandardCostEntities.TrAg;
            if (TrItEntityPattern.IsMatch(name))
                return GroupStandardCostEntities.TrIt;
            if (TrInEntityPattern.IsMatch(name))
                return GroupStandardCostEntities.TrIn;
            return null;
        }

        // Gleicher umschaltbarer Material-Fallback wie in Resolve(). Beide Methoden muessen
        // dieselbe liefernde Gesellschaft liefern, damit Klassifikation und Kostenpfad nicht
        // auseinanderlaufen.
        return HasMaterialFallbackMatch(
            normalizedMaterialKey, groupStandardCosts, chPlantMaterialKeys, supplierFallbackMode)
            ? GroupStandardCostEntities.TrAg
            : null;
    }

    /// <summary>
    /// Fertigt der Standort selbst, ist er selbst die liefernde Gesellschaft. Nur Standorte mit
    /// einer bekannten Konstante werden zurueckgegeben - sonst null statt geraten.
    /// </summary>
    private static string? ResolveSiteEntity(string? tsc)
        => tsc?.Trim().ToUpperInvariant() switch
        {
            "TRIN" => GroupStandardCostEntities.TrIn,
            "TRIT" => GroupStandardCostEntities.TrIt,
            _ => null
        };
}

/// <summary>
/// Verrechnungspreisliche Rollen aus dem Artikelstammfeld „Sales Type". Beschreiben, welche
/// Rolle der VERKAUFENDE Standort in diesem Geschaeft einnimmt - nicht, woher die Ware kommt.
/// Siehe docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md Abschnitt 2.
/// </summary>
public static class SalesTypeRoles
{
    /// <summary>Full Fledged Manufacturing: Fertigung und Verkauf auf eigene Rechnung.</summary>
    public const string OwnManufacturing = "FFM";

    /// <summary>Contract Manufacturing: Fertigung im Auftrag von Trafag AG, Fakturierung an sie.</summary>
    public const string ContractManufacturing = "CM";

    /// <summary>Limited Risk Distributor: Bezug von Trafag AG, Weiterverkauf im eigenen Markt.</summary>
    public const string GroupDistribution = "LRD";
}

/// <summary>
/// Bekannte liefernde Trafag-Gesellschaften fuer die Konzern-Kostenbasis (Mappe1.xlsx).
/// TR AG kommt aus MBEW-STPRS; TR IT/TR IN werden aus dem jeweils juengsten positiven
/// B1-Belegkostenwert der eigenen Verkaufszeilen aufgebaut.
/// </summary>
public static class GroupStandardCostEntities
{
    public const string TrAg = "TR_AG";
    public const string TrIt = "TR_IT";
    public const string TrIn = "TR_IN";
}

/// <summary>
/// Liefernde Gesellschaft -> technischer Kostenbereich. Fuer TR AG ist dies der echte
/// MBEW-Bewertungskreis 1100; TR IT/TR IN verwenden stabile B1-Kostenbereichsschluessel.
/// </summary>
public static class GroupStandardCostAreas
{
    public static readonly IReadOnlyDictionary<string, string> ByEntity = new Dictionary<string, string>
    {
        [GroupStandardCostEntities.TrAg] = "1100",
        [GroupStandardCostEntities.TrIt] = "TRIT",
        [GroupStandardCostEntities.TrIn] = "TRIN"
    };

    public static readonly IReadOnlyDictionary<string, string> CurrencyByEntity = new Dictionary<string, string>
    {
        [GroupStandardCostEntities.TrAg] = "CHF",
        [GroupStandardCostEntities.TrIt] = "EUR",
        [GroupStandardCostEntities.TrIn] = "INR"
    };

    public static readonly IReadOnlyDictionary<string, string> SourceLabelByEntity = new Dictionary<string, string>
    {
        [GroupStandardCostEntities.TrAg] = GroupMarginCalculator.GroupCostSourceLabel,
        [GroupStandardCostEntities.TrIt] = "Konzernkosten TR IT (B1 StockPrice)",
        [GroupStandardCostEntities.TrIn] = "Konzernkosten TR IN (B1 StockPrice)"
    };

    public static bool TryResolveB1Source(string? tsc, out string area, out string currency)
    {
        var entity = tsc?.Trim().ToUpperInvariant() switch
        {
            "TRIT" => GroupStandardCostEntities.TrIt,
            "TRIN" => GroupStandardCostEntities.TrIn,
            _ => null
        };

        if (entity is not null &&
            ByEntity.TryGetValue(entity, out area!) &&
            CurrencyByEntity.TryGetValue(entity, out currency!))
        {
            return true;
        }

        area = string.Empty;
        currency = string.Empty;
        return false;
    }
}
