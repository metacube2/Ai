using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Die vollstaendige Rechnung der Gruppenmarge fuer EINE Verkaufszeile: Lieferantentyp,
/// Kostenbasis, Kostenquelle und Status. Vorher stand dieselbe Logik zweimal da - einmal in
/// <see cref="ExcelExportService"/> auf <c>SalesRecord</c>, einmal in
/// <see cref="ManagementCockpitService"/> auf <c>FinanceAggregationRow</c>. Beide Fassungen
/// mussten „im Gleichschritt" gepflegt werden, und genau das ist am 2026-08-05 misslungen:
/// der neue Status <see cref="GroupMarginStatuses.GroupCostMissing"/> kam im Excel-Nachweis an,
/// im Cockpit nicht - dieselbe Zeile stand dort weiter auf „Standardpreis fehlt".
///
/// Deshalb rechnet jetzt nur noch diese Klasse, und die beiden Dienste bilden ihre Zeile
/// vorher auf <see cref="GroupMarginLine"/> ab.
/// </summary>
public static class GroupMarginCalculator
{
    /// <summary>
    /// Die Regeln zur Kostenbasis in der Reihenfolge, in der sie greifen. DIE REIHENFOLGE IST
    /// DIE FACHREGEL: die erste zutreffende Regel gewinnt, spaetere werden nicht mehr gefragt.
    /// Vorher war diese Reihenfolge nur die zufaellige Abfolge dreier if-Bloecke.
    ///
    /// Eine neue Abweichung (weitere Gesellschaft mit eigener Kostenquelle, weitere
    /// Verrechnungspreisrolle) wird als benannte Regel an der fachlich richtigen Stelle
    /// eingesetzt, statt einen weiteren if-Block in zwei Dateien zu ergaenzen.
    /// Die letzte Regel trifft immer zu.
    /// </summary>
    public static readonly IReadOnlyList<GroupMarginCostRule> CostRules = new[]
    {
        GroupMarginCostRules.GroupStandardCost,
        GroupMarginCostRules.GroupDistributionWithoutGroupCost,
        GroupMarginCostRules.LocalStandardCost
    };

    /// <summary>
    /// Rechnet eine Verkaufszeile durch. <paramref name="hasExchangeRate"/> ist nur fuer das
    /// Audit-Ledger relevant: fehlt dort der CHF-Kurs, ueberlagert das jede andere Aussage zur
    /// Zeile. Die Gruppenmargen-Ansichten rechnen ohne diese Pruefung.
    /// </summary>
    public static GroupMarginEvaluation Evaluate(
        GroupMarginLine line,
        IReadOnlyDictionary<(string MaterialKey, string ValuationArea), GroupStandardCost>? groupStandardCosts = null,
        bool hasExchangeRate = true,
        IReadOnlySet<string>? chPlantMaterialKeys = null,
        string? supplierFallbackMode = null,
        string? internalSupplierCostSourceMode = null,
        IReadOnlySet<string>? foreignProcurementMaterialKeys = null,
        string? marcForeignProcurementMode = null,
        MaterialMappingLookup? materialMappings = null)
    {
        var costs = groupStandardCosts ?? new Dictionary<(string, string), GroupStandardCost>();
        var plantMaterials = chPlantMaterialKeys ?? new HashSet<string>(StringComparer.Ordinal);
        var foreignProcurementMaterials = foreignProcurementMaterialKeys ?? new HashSet<string>(StringComparer.Ordinal);
        // AP4 (Entscheid Ingo 19.11.2026): die Mapping-Tabelle fuellt die Gruppennummer NUR, wo die Quelle sie leer laesst,
        // und nur fuer den eingetragenen TSC. Die Zeile selbst (SalesRecord.GroupMaterialNumber) bleibt unveraendert.
        MaterialMappingHit? mappingHit = null;
        if (string.IsNullOrWhiteSpace(line.GroupMaterialNumber) &&
            materialMappings is not null &&
            materialMappings.TryResolve(line.Tsc, line.Material, out var hit))
        {
            mappingHit = hit;
            line = line with { GroupMaterialNumber = hit.GroupMaterialNumber };
        }

        var materialKey = ResolveGroupCostKey(line);

        var supplierType = GroupMarginSupplierClassifier.Resolve(
            line.SupplierNumber, line.SupplierName, line.SupplierCountry, line.Tsc,
            materialKey, costs, line.SalesType, plantMaterials, supplierFallbackMode,
            foreignProcurementMaterials, marcForeignProcurementMode);

        var context = new GroupMarginCostContext(
            MaterialKey: materialKey,
            GroupStandardCosts: costs,
            ChPlantMaterialKeys: plantMaterials,
            SupplierFallbackMode: SupplierFallbackModes.Normalize(supplierFallbackMode),
            InternalSupplierCostSourceMode: InternalSupplierCostSourceModes.Normalize(internalSupplierCostSourceMode),
            ForeignProcurementMaterialKeys: foreignProcurementMaterials,
            MarcForeignProcurementMode: MarcForeignProcurementModes.Normalize(marcForeignProcurementMode),
            // Gutschriften/Retouren tragen einen negativen Netto-Umsatz. Die Kostenbasis muss
            // mit umkehren, sonst rechnet die Marge die Kosten doppelt negativ (Umsatz -100,
            // Kosten +60 -> -160 statt korrekt -40). Bei Umsatz 0 fuehrt das Mengenvorzeichen.
            IsReversal: line.NetSalesValue < 0m || (line.NetSalesValue == 0m && line.Quantity < 0m));

        var basis = ResolveCostBasis(line, context);

        // Liefernde Konzerngesellschaft, wie sie die Klassifikation erkennt (Mail Andreas 2026-11-13: die Lieferantenfelder
        // sind bei vielen Gesellschaften leer, die Zuordnung der Gruppenmarge war in Sales_All nicht sichtbar). Gleicher
        // Aufruf mit denselben Argumenten wie in GroupMarginCostRules.GroupStandardCost, damit Anzeige und Kostenpfad
        // nicht auseinanderlaufen. Bewusst VOR dem Schalter "Kostenquelle bei internem Lieferanten": der aendert nur die
        // Kostenquelle, nicht die erkannte Gesellschaft.
        var deliveringEntity = GroupMarginSupplierClassifier.ResolveDeliveringEntity(
            line.SupplierName, line.Tsc, context.MaterialKey, context.GroupStandardCosts, line.SalesType,
            context.ChPlantMaterialKeys, context.SupplierFallbackMode,
            line.SupplierNumber, line.SupplierCountry,
            context.ForeignProcurementMaterialKeys, context.MarcForeignProcurementMode);

        // Issue Topic an genau dieser Stelle, mit derselben Gesellschaft und demselben Schluessel (inklusive I-Praefix-Regel)
        // wie der Kostenpfad. Aendert keine Kosten, keinen Status und keine Kostenquelle.
        var lookup = deliveringEntity is null
            ? default
            : GroupMarginCostRules.FindGroupCost(line, context, deliveringEntity);
        var issueTopic = ResolveIssueTopic(line, supplierType, deliveringEntity, basis, context, lookup);

        var mappingLabel = mappingHit is not null
            ? $"{mappingHit.GroupMaterialNumber.Trim()} ({mappingHit.Method})"
            : lookup.UsedItalianIndiaPrefixRule
                ? $"{lookup.EffectiveKey} ({IssueTopics.ItalianIndiaPrefixRuleLabel})"
                : string.Empty;

        return new GroupMarginEvaluation(
            SupplierType: supplierType,
            MaterialKey: materialKey,
            CostBasis: basis.CostBasis,
            CostCurrency: basis.CostCurrency,
            IsGroupCost: basis.IsGroupCost,
            IsGroupCostMissing: basis.IsGroupCostMissing,
            CostSource: ResolveCostSource(supplierType, basis),
            Status: ResolveStatus(supplierType, basis, line.NetSalesValue, hasExchangeRate),
            DeliveringEntity: deliveringEntity ?? string.Empty,
            IssueTopic: issueTopic,
            Mapping: mappingLabel);
    }

    /// <summary>
    /// Issue Topic nach der Matrix von Ingo (19.11.2026, Spalten no Supplier / no Mat Number Match / no Cost), Lesart und
    /// vorlaeufige Zusatzcodes in docs/FINANCE_PLAN_MAPPING_ISSUE_TOPIC_2026-11-18.md Abschnitt 4. Reihenfolge der Pruefung:
    /// 8 (Indien LRD, Kosten bleiben offen), Konzernkosten gefunden (5 bzw. leer), 9 (Lieferant unklar), danach die drei
    /// Merkmale der Matrix woertlich: gefunden ohne Kosten 3/4, nicht gefunden 1/2, nicht gefunden und lokal 0 dann 6/7.
    /// „Kein Lieferant“ heisst: keine CH/AT-Regel, keine Sales-Type-Rolle und alle drei Lieferantenfelder leer.
    /// </summary>
    public static string ResolveIssueTopic(
        GroupMarginLine line,
        string supplierType,
        string? deliveringEntity,
        GroupMarginCostBasis basis,
        GroupMarginCostContext context,
        GroupCostLookup lookup)
    {
        if (basis.IsGroupCostMissing)
            return IssueTopics.IndiaLrdWithoutGroupCost;

        var noSupplier = GroupMarginSupplierClassifier.HasNoSupplierEvidence(
            line.SupplierNumber, line.SupplierName, line.SupplierCountry, line.Tsc, line.SalesType);

        if (basis.IsGroupCost)
            return noSupplier ? IssueTopics.NoSupplierTrAgCost : IssueTopics.None;
        if (supplierType == GroupMarginSupplierClassifier.Unclear)
            return IssueTopics.SupplierUnclear;

        // Lokaler Rueckfall: Kostenbasis 0 heisst, auch die lokalen Kosten fehlen oder sind null.
        var localCostMissing = basis.CostBasis == 0m;

        // Extern: keine Konzernkostensuche, die lokalen Kosten sind die Zielkosten. Nur fehlende lokale Kosten sind ein Issue
        // (vorlaeufig 7, von der Matrix nicht ausdruecklich abgedeckt, offene Frage im Plan Abschnitt 4).
        if (supplierType == GroupMarginSupplierClassifier.External)
            return localCostMissing ? IssueTopics.SupplierUseZero : IssueTopics.None;
        if (supplierType == GroupMarginSupplierClassifier.Local)
            return localCostMissing ? IssueTopics.NoSupplierUseZero : IssueTopics.NoSupplierNotFound;

        // Intern, aber die Konzernkosten wurden nicht verwendet.
        if (deliveringEntity is null)
            return IssueTopics.InternalWithoutCostSource;

        var found = lookup.Found ||
                    (deliveringEntity == GroupStandardCostEntities.TrAg &&
                     context.ChPlantMaterialKeys is not null &&
                     context.ChPlantMaterialKeys.Contains(context.MaterialKey));

        // Korrektur Ingo 19.11.2026: die Matrix gilt woertlich. Ein gefundener Artikel hat „no Mat Number Match“ = 0 und
        // bleibt 3 bzw. 4, auch wenn die lokalen Kosten 0 sind (Kosten dann 0, Margin Status „Standardpreis fehlt“ wie heute).
        // 6 und 7 gibt es nur bei nicht gefundenem Artikel UND fehlenden lokalen Kosten.
        if (found)
            return noSupplier ? IssueTopics.NoSupplierFoundNoCost : IssueTopics.SupplierFoundNoCost;
        if (localCostMissing)
            return noSupplier ? IssueTopics.NoSupplierUseZero : IssueTopics.SupplierUseZero;
        return noSupplier ? IssueTopics.NoSupplierNotFound : IssueTopics.SupplierNotFound;
    }

    /// <summary>Anzeigetext der liefernden Gesellschaft fuer Excel: "TR AG", "TR IT", "TR IN" oder leer.</summary>
    public static string DeliveringEntityLabel(string? deliveringEntity)
        => deliveringEntity switch
        {
            GroupStandardCostEntities.TrAg => "TR AG",
            GroupStandardCostEntities.TrIt => "TR IT",
            GroupStandardCostEntities.TrIn => "TR IN",
            _ => string.Empty
        };

    /// <summary>Erste zutreffende Regel gewinnt. Die letzte Regel der Kette trifft immer zu.</summary>
    private static GroupMarginCostBasis ResolveCostBasis(GroupMarginLine line, GroupMarginCostContext context)
    {
        foreach (var rule in CostRules)
        {
            var basis = rule.TryResolve(line, context);
            if (basis is not null)
                return basis;
        }

        throw new InvalidOperationException(
            $"Keine Kostenbasisregel hat gegriffen. Die letzte Regel ({CostRules[^1].Name}) muss immer zutreffen.");
    }

    /// <summary>
    /// Schluessel fuer die Konzern-Standardkosten. Fuehrt die Quelle neben ihrer eigenen
    /// Artikelnummer die Trafag-Sachnummer (Indien: <c>OITM.U_TASC_OMN</c>), gilt diese - die
    /// lokale Nummer ist dort eine Eigennummerierung und findet die Konzernkosten nicht.
    /// Gemessen 2026-08-05 auf TRIN: ueber die lokale Nummer treffen 34 von 135 Artikeln, ueber
    /// die Trafag-Sachnummer 118 von 123 (alle, die eine gepflegte Nummer haben).
    /// </summary>
    public static string ResolveGroupCostKey(GroupMarginLine line)
        => MaterialKeyNormalizer.Normalize(string.IsNullOrWhiteSpace(line.GroupMaterialNumber)
            ? line.Material
            : line.GroupMaterialNumber);

    /// <summary>Beschriftung der Kostenquelle - woher die Kostenbasis dieser Zeile stammt.</summary>
    public static string ResolveCostSource(string supplierType, GroupMarginCostBasis basis)
    {
        if (basis.IsGroupCost)
            return basis.GroupCostSource ?? GroupCostSourceLabel;
        if (basis.IsGroupCostMissing)
            return GroupMarginStatuses.GroupCostMissingSource;

        return supplierType switch
        {
            GroupMarginSupplierClassifier.Internal => "Interner Standardpreis",
            GroupMarginSupplierClassifier.External => "Kosten aus Verkaufszeile",
            GroupMarginSupplierClassifier.Local => "Standardkosten der lokalen Gesellschaft",
            _ => GroupMarginStatuses.SupplierUnclear
        };
    }

    public const string GroupCostSourceLabel = "Konzernkosten TR AG (MBEW-STPRS)";

    /// <summary>
    /// Status der Zeile. Die Reihenfolge der Pruefungen ist fachlich: ein fehlender Kurs
    /// ueberlagert alles, danach ein unklarer Lieferant, und
    /// <see cref="GroupMarginStatuses.GroupCostMissing"/> steht VOR „Standardpreis fehlt" - ein
    /// Standardpreis IST dort vorhanden, er ist nur der IC-Einkaufspreis und taugt nicht als
    /// Herstellkostenbasis. Ein gemeinsames Label fuer beide Faelle wuerde die Ursache verdecken.
    /// </summary>
    public static string ResolveStatus(
        string supplierType, GroupMarginCostBasis basis, decimal netSalesValue, bool hasExchangeRate = true)
    {
        if (!hasExchangeRate)
            return GroupMarginStatuses.ExchangeRateMissing;
        if (supplierType == GroupMarginSupplierClassifier.Unclear)
            return GroupMarginStatuses.SupplierUnclear;
        if (basis.IsGroupCostMissing)
            return GroupMarginStatuses.GroupCostMissing;
        if (basis.CostBasis == 0m)
            return GroupMarginStatuses.StandardCostMissing;
        if (netSalesValue == 0m)
            return GroupMarginStatuses.SalesMissing;
        return GroupMarginStatuses.Ok;
    }

    /// <summary>
    /// Ergaenzt die Kostenquelle um den Hinweis zur Waehrungsumrechnung. Nur das Cockpit zeigt
    /// diesen Zusatz; der Excel-Nachweis fuehrt die Umrechnung in eigenen Spalten.
    /// </summary>
    public static string DecorateCostSource(
        string label,
        string? costCurrency,
        string? salesCurrency,
        GroupMarginCostCurrencyConverter.Result conversion)
    {
        if (!conversion.IsMismatch)
            return label;
        return conversion.IsMasked
            ? $"{label} ({costCurrency?.Trim()} <> {salesCurrency?.Trim()}, Marge offen)"
            : $"{label} (umgerechnet {costCurrency?.Trim()}->{salesCurrency?.Trim()} @ {conversion.AppliedRate:0.####})";
    }
}

/// <summary>
/// Eine Verkaufszeile, soweit die Gruppenmarge sie braucht - unabhaengig davon, ob sie aus
/// <c>SalesRecord</c> (Excel-Nachweis) oder <c>FinanceAggregationRow</c> (Cockpit) stammt.
///
/// Bewusst mit Objektinitialisierer statt positionsbasiert: mehrere gleichartige
/// <c>string?</c>-Felder liegen nebeneinander (Lieferantenname/-land, Material/Sachnummer).
/// Eine Vertauschung wuerde fehlerfrei compilieren und still eine falsche Marge liefern.
/// </summary>
public readonly record struct GroupMarginLine
{
    public string? SupplierNumber { get; init; }
    public string? SupplierName { get; init; }
    public string? SupplierCountry { get; init; }

    /// <summary>Standortkennung, z. B. <c>TRIN</c>.</summary>
    public string? Tsc { get; init; }

    /// <summary>Artikelnummer der Quelle - bei Standorten mit Eigennummerierung nicht die Trafag-Nummer.</summary>
    public string? Material { get; init; }

    /// <summary>Trafag-Sachnummer aus dem Artikelstamm, wo die Quelle sie fuehrt (Indien: <c>U_TASC_OMN</c>).</summary>
    public string? GroupMaterialNumber { get; init; }

    /// <summary>Verrechnungspreisrolle aus dem Artikelstamm (Indien: <c>U_Tasc_ST</c>): FFM, CM, LRD.</summary>
    public string? SalesType { get; init; }

    public decimal Quantity { get; init; }

    /// <summary>Lokaler Standardpreis je Einheit aus der Verkaufszeile.</summary>
    public decimal StandardCost { get; init; }

    public string StandardCostCurrency { get; init; }

    /// <summary>Netto-Umsatz der Zeile. Negativ bei Gutschriften/Retouren.</summary>
    public decimal NetSalesValue { get; init; }
}

/// <summary>Was eine Kostenbasisregel liefert.</summary>
/// <param name="CostBasis">Kostenbasis der ganzen Zeile (Menge x Preis), vorzeichenrichtig.</param>
/// <param name="IsGroupCost">Echte Konzern-Herstellkosten statt lokaler Standardpreis.</param>
/// <param name="IsGroupCostMissing">Kostenbasis bleibt offen, weil der lokale Wert ein IC-Preis ist.</param>
public sealed record GroupMarginCostBasis(
    decimal CostBasis,
    string CostCurrency,
    bool IsGroupCost = false,
    bool IsGroupCostMissing = false,
    string? GroupCostSource = null);

/// <summary>Was die Regeln ausser der Zeile selbst brauchen.</summary>
public sealed record GroupMarginCostContext(
    string MaterialKey,
    IReadOnlyDictionary<(string MaterialKey, string ValuationArea), GroupStandardCost> GroupStandardCosts,
    bool IsReversal,
    IReadOnlySet<string>? ChPlantMaterialKeys = null,
    string? SupplierFallbackMode = null,
    string? InternalSupplierCostSourceMode = null,
    IReadOnlySet<string>? ForeignProcurementMaterialKeys = null,
    string? MarcForeignProcurementMode = null);

/// <summary>Vollstaendiges Ergebnis fuer eine Zeile.</summary>
public sealed record GroupMarginEvaluation(
    string SupplierType,
    string MaterialKey,
    decimal CostBasis,
    string CostCurrency,
    bool IsGroupCost,
    bool IsGroupCostMissing,
    string CostSource,
    string Status,
    // Erkannte liefernde Konzerngesellschaft (GroupStandardCostEntities) oder leer; additiv seit 2026-11-13.
    string DeliveringEntity = "",
    // Issue Topic (IssueTopics), leer = kein Issue; additiv seit 2026-11-19.
    string IssueTopic = "",
    // Wirksame Zuordnung der Artikelnummer aus Mapping-Tabelle oder Schluesselregel, leer = keine; additiv seit 2026-11-19.
    string Mapping = "");

/// <summary>
/// Ergebnis der Suche nach Konzernkosten bei EINER liefernden Gesellschaft. <see cref="Found"/> heisst: der Schluessel
/// steht in der Kostentabelle dieser Gesellschaft, unabhaengig vom Wert (<see cref="Cost"/> kann 0 sein).
/// </summary>
public readonly record struct GroupCostLookup(
    bool Found,
    GroupStandardCost? Cost,
    string EffectiveKey,
    bool UsedItalianIndiaPrefixRule);

/// <summary>
/// Codes der Spalte „Issue Topic“. 1 bis 7 nach der Matrix von Ingo (19.11.2026); 8 bis 10 sind VORLAEUFIGE Kennzeichen fuer
/// Faelle, die die Matrix nicht abdeckt (offen bei Andreas/Ingo, Plan Abschnitt 4). Die Codes sind Zeichenketten, damit
/// eine spaetere Umbenennung keine Zahlenformate in Excel beruehrt.
/// </summary>
public static class IssueTopics
{
    public const string None = "";
    /// <summary>1 = no Supplier, no Mat Number Match: lokale Standardkosten.</summary>
    public const string NoSupplierNotFound = "1";
    /// <summary>2 = Lieferant vorhanden, Artikel bei der liefernden Gesellschaft nicht gefunden: lokale Standardkosten.</summary>
    public const string SupplierNotFound = "2";
    /// <summary>3 = Lieferant vorhanden, Artikel gefunden, Kosten fehlen dort: lokale Standardkosten.</summary>
    public const string SupplierFoundNoCost = "3";
    /// <summary>4 = kein Lieferant, Artikel bei Trafag AG gefunden, Kosten fehlen dort: lokale Standardkosten.</summary>
    public const string NoSupplierFoundNoCost = "4";
    /// <summary>5 = kein Lieferant, Artikel bei Trafag AG gefunden, Kosten vorhanden: Standardkosten TR AG.</summary>
    public const string NoSupplierTrAgCost = "5";
    /// <summary>6 = kein Lieferant, auch lokale Kosten fehlen oder sind null: use 0.</summary>
    public const string NoSupplierUseZero = "6";
    /// <summary>7 = Lieferant vorhanden, auch lokale Kosten fehlen oder sind null: use 0.</summary>
    public const string SupplierUseZero = "7";
    /// <summary>8 (vorlaeufig) = Indien Sales Type LRD ohne Konzernkosten, Kosten bleiben offen (Entscheid 05.08.).</summary>
    public const string IndiaLrdWithoutGroupCost = "8";
    /// <summary>9 (vorlaeufig) = Lieferant unklar (keine Lieferantenangabe und keine Pruefgrundlage, z. B. ohne Materialnummer).</summary>
    public const string SupplierUnclear = "9";
    /// <summary>10 (vorlaeufig) = interner Lieferant ohne bekannte Kostenquelle (z. B. GFS, Trafag GmbH): lokale Standardkosten.</summary>
    public const string InternalWithoutCostSource = "10";

    public const string ItalianIndiaPrefixRuleLabel = "Regel I-Praefix TRIT";

    /// <summary>Kurztext je Code fuer Feldhilfe und Mapping-Excel (Englisch wie die Matrix).</summary>
    public static readonly IReadOnlyList<(string Code, string NoSupplier, string NoMatch, string NoCost, string Solution)> Matrix =
    [
        ("", "0", "0", "0", "no issue: supplier present, article found, cost present"),
        (NoSupplierNotFound, "1", "1", "0", "local standard cost"),
        (SupplierNotFound, "0", "1", "0", "local standard cost"),
        (SupplierFoundNoCost, "0", "0", "1", "local standard cost"),
        (NoSupplierFoundNoCost, "1", "0", "1", "local standard cost"),
        (NoSupplierTrAgCost, "1", "0", "0", "TR AG standard cost"),
        (NoSupplierUseZero, "1", "1", "1", "use 0 (local cost missing too)"),
        (SupplierUseZero, "0", "1", "1", "use 0 (local cost missing too)"),
        (IndiaLrdWithoutGroupCost, "-", "-", "-", "provisional: India LRD without group cost, cost stays open (decision 05.08.)"),
        (SupplierUnclear, "-", "-", "-", "provisional: supplier unclear, no material number to check"),
        (InternalWithoutCostSource, "-", "-", "-", "provisional: internal supplier without known group cost source, local standard cost")
    ];
}

/// <summary>
/// Eine benannte Regel zur Kostenbasis. Benannt, damit die Kette lesbar bleibt und jede Regel
/// einzeln testbar ist. Liefert <c>null</c>, wenn sie nicht zustaendig ist.
/// </summary>
public sealed class GroupMarginCostRule
{
    private readonly Func<GroupMarginLine, GroupMarginCostContext, GroupMarginCostBasis?> _resolve;

    public GroupMarginCostRule(
        string name,
        string description,
        Func<GroupMarginLine, GroupMarginCostContext, GroupMarginCostBasis?> resolve)
    {
        Name = name;
        Description = description;
        _resolve = resolve;
    }

    public string Name { get; }

    /// <summary>Fachliche Begruendung - erscheint in Tests und Protokollen, nicht nur im Kommentar.</summary>
    public string Description { get; }

    public GroupMarginCostBasis? TryResolve(GroupMarginLine line, GroupMarginCostContext context)
        => _resolve(line, context);
}

/// <summary>Die einzelnen Regeln. Ihre Reihenfolge steht in <see cref="GroupMarginCalculator.CostRules"/>.</summary>
public static class GroupMarginCostRules
{
    /// <summary>
    /// Echte Konzern-Herstellkosten: die liefernde Gesellschaft hat eine verifizierte
    /// Kostenquelle (TR AG aus MBEW-STPRS, TR IT/TR IN aus B1-Belegkosten)
    /// UND fuer das Material liegt ein Treffer vor. Das ist der eigentliche Zweck der
    /// Gruppenmarge: der lokal gespeicherte Wert wird durch die Konzernkosten ersetzt.
    /// </summary>
    public static readonly GroupMarginCostRule GroupStandardCost = new(
        nameof(GroupStandardCost),
        "Konzern-Herstellkosten der liefernden Gesellschaft.",
        (line, context) =>
        {
            var deliveringEntity = GroupMarginSupplierClassifier.ResolveDeliveringEntity(
                line.SupplierName, line.Tsc, context.MaterialKey, context.GroupStandardCosts, line.SalesType,
                context.ChPlantMaterialKeys, context.SupplierFallbackMode,
                line.SupplierNumber, line.SupplierCountry,
                context.ForeignProcurementMaterialKeys, context.MarcForeignProcurementMode);

            // Unabhaengiger Finance-Schalter: Die Lieferantenklassifikation bleibt erhalten,
            // aber bei einem CH-Werkstammtreffer kann Andreas bewusst Schweizer STPRS statt
            // der Kosten der liefernden Konzerngesellschaft vergleichen. Nur ein vorhandener
            // positiver Schweizer Wert aktiviert die Alternative; sonst bleibt die bisherige
            // Kostenquelle sichtbar statt eine Zeile grundlos auf "Kosten fehlen" zu setzen.
            //
            // Die Bedingung "deliveringEntity is not null" fehlte bis zum 2026-09-02 und ist der
            // eigentliche Punkt: der Schalter heisst "Kostenquelle bei INTERNEM Lieferanten" und
            // darf deshalb nur die Kostenquelle einer Zeile aendern, fuer die bereits eine
            // liefernde Konzerngesellschaft erkannt wurde. Ohne die Bedingung setzte er
            // deliveringEntity auch bei Extern, Lokal und Unklar auf TR_AG und erfand damit einen
            // Konzernbezug, den die Klassifikation gerade NICHT gefunden hatte - fuer jedes der
            // rund 66'000 Materialien im CH-Werkstamm. Das widersprach der eigenen Zusage des
            // Schalters, ausschliesslich die Kostenquelle und nie die Klassifikation zu aendern.
            //
            // ABGRENZUNG, falls das jemand erweitern will: Das ist bewusst die enge Lesart. Die
            // Empfehlung in docs/FINANCE_STANDARDKOSTEN.md Abschnitt 10, den Schweizer STPRS
            // unabhaengig von der liefernden Gesellschaft gelten zu lassen, ist eine ANDERE und
            // weitergehende Regel. Andreas hat sie am 2026-09-09 ENTSCHIEDEN, und zwar dagegen:
            // Schnitt nach der ersten internen Lieferstufe, intern sind nur TR AG, TR IN und TR IT.
            // Die enge Lesart hier bleibt damit richtig, und die weitergehende Regel wird nicht
            // gebaut. Wortlaut und akzeptierte Ungenauigkeit in
            // docs/FINANCE_STANDARDKOSTEN_SCHNITT_ANDREAS_2026-09-09.md.
            if (deliveringEntity is not null && ShouldUseSwissStprs(context))
                deliveringEntity = GroupStandardCostEntities.TrAg;

            if (deliveringEntity is null)
                return null;

            var lookup = FindGroupCost(line, context, deliveringEntity);
            if (lookup.Cost is not { } groupCost || groupCost.UnitCost <= 0m)
                return null;

            var magnitude = Magnitude(line.Quantity, groupCost.UnitCost);
            return new GroupMarginCostBasis(
                context.IsReversal ? -magnitude : magnitude,
                groupCost.Currency,
                IsGroupCost: true,
                GroupCostSource: GroupStandardCostAreas.SourceLabelByEntity.GetValueOrDefault(deliveringEntity));
        });

    /// <summary>
    /// Sucht die Konzernkosten der Zeile bei der liefernden Gesellschaft. Zuerst unter dem Kostenschluessel der Zeile.
    /// AP1 (Entscheid Ingo 19.11.2026): verkauft TRIT Ware aus Indien (liefernde Gesellschaft TR IN) und trifft der volle
    /// Schluessel nicht, wird genau EIN vorangestelltes „I“ abgeschnitten und erneut gesucht. Italien fuehrt die Ware unter
    /// „I“ + Trafag-Sachnummer (I53513 = 53513) oder „I“ + indische Nummer (ITS000713 = TS000713); gemessen 18.11.: 401 von
    /// 448 Zeilen (docs/FINANCE_SUPPLIER.md 12a). Der volle Schluessel geht vor, weil echte indische Nummern selbst mit
    /// „I“ beginnen koennen (5 Zeilen IR… trafen schon vorher). Nur TRIT und nur Lieferant Indien.
    /// </summary>
    public static GroupCostLookup FindGroupCost(GroupMarginLine line, GroupMarginCostContext context, string deliveringEntity)
    {
        if (!GroupStandardCostAreas.ByEntity.TryGetValue(deliveringEntity, out var area))
            return new GroupCostLookup(false, null, context.MaterialKey, false);

        if (context.GroupStandardCosts.TryGetValue((context.MaterialKey, area), out var cost))
            return new GroupCostLookup(true, cost, context.MaterialKey, false);

        if (deliveringEntity == GroupStandardCostEntities.TrIn &&
            string.Equals(line.Tsc?.Trim(), "TRIT", StringComparison.OrdinalIgnoreCase) &&
            context.MaterialKey.Length > 1 &&
            (context.MaterialKey[0] == 'I' || context.MaterialKey[0] == 'i'))
        {
            var stripped = MaterialKeyNormalizer.Normalize(context.MaterialKey[1..]);
            if (stripped.Length > 0 && context.GroupStandardCosts.TryGetValue((stripped, area), out var strippedCost))
                return new GroupCostLookup(true, strippedCost, stripped, true);
        }

        return new GroupCostLookup(false, null, context.MaterialKey, false);
    }

    private static bool ShouldUseSwissStprs(GroupMarginCostContext context)
    {
        if (InternalSupplierCostSourceModes.Normalize(context.InternalSupplierCostSourceMode) !=
            InternalSupplierCostSourceModes.SwissStprsForChPlantMaterial)
            return false;
        if (context.ChPlantMaterialKeys is null || !context.ChPlantMaterialKeys.Contains(context.MaterialKey))
            return false;

        var trAgArea = GroupStandardCostAreas.ByEntity[GroupStandardCostEntities.TrAg];
        return context.GroupStandardCosts.TryGetValue((context.MaterialKey, trAgArea), out var swissCost)
               && swissCost.UnitCost > 0m;
    }

    /// <summary>
    /// Konzernvertrieb ohne Kostentreffer: bei Sales Type <c>LRD</c> ist die Ware in der Schweiz
    /// hergestellt und der lokale Standardpreis der IC-Einkaufspreis - also genau der Wert, den
    /// die Gruppenmarge ersetzen soll. Ohne Konzernkostentreffer bleibt die Kostenbasis deshalb
    /// OFFEN. Ein Rueckfall auf den lokalen Wert ergaebe eine Marge auf dem Verrechnungspreis:
    /// plausibel aussehend und falsch, und damit schlechter als eine als offen erkennbare Zeile.
    /// Siehe docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md Abschnitt 6a.
    /// </summary>
    public static readonly GroupMarginCostRule GroupDistributionWithoutGroupCost = new(
        nameof(GroupDistributionWithoutGroupCost),
        "Sales Type LRD ohne Konzernkostentreffer: Kostenbasis bleibt offen statt IC-Preis.",
        (line, _) =>
            GroupMarginSupplierClassifier.ResolveSalesTypeRole(line.SalesType) == SalesTypeRoles.GroupDistribution
                ? new GroupMarginCostBasis(0m, line.StandardCostCurrency, IsGroupCostMissing: true)
                : null);

    /// <summary>
    /// Rueckfall auf den lokalen Standardpreis aus der Verkaufszeile. Trifft immer zu und
    /// schliesst die Kette ab.
    /// </summary>
    public static readonly GroupMarginCostRule LocalStandardCost = new(
        nameof(LocalStandardCost),
        "Lokaler Standardpreis aus der Verkaufszeile.",
        (line, context) =>
        {
            var magnitude = Magnitude(line.Quantity, line.StandardCost);
            return new GroupMarginCostBasis(
                context.IsReversal ? -magnitude : magnitude, line.StandardCostCurrency);
        });

    /// <summary>Menge x Preis; bei Menge 0 der reine Stueckpreis, wie bisher in beiden Diensten.</summary>
    private static decimal Magnitude(decimal quantity, decimal unitCost)
        => quantity != 0m
            ? Math.Abs(quantity) * Math.Abs(unitCost)
            : Math.Abs(unitCost);
}
