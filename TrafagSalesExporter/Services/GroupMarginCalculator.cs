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
        bool hasExchangeRate = true)
    {
        var costs = groupStandardCosts ?? new Dictionary<(string, string), GroupStandardCost>();
        var materialKey = ResolveGroupCostKey(line);

        var supplierType = GroupMarginSupplierClassifier.Resolve(
            line.SupplierNumber, line.SupplierName, line.SupplierCountry, line.Tsc,
            materialKey, costs, line.SalesType);

        var context = new GroupMarginCostContext(
            MaterialKey: materialKey,
            GroupStandardCosts: costs,
            // Gutschriften/Retouren tragen einen negativen Netto-Umsatz. Die Kostenbasis muss
            // mit umkehren, sonst rechnet die Marge die Kosten doppelt negativ (Umsatz -100,
            // Kosten +60 -> -160 statt korrekt -40). Bei Umsatz 0 fuehrt das Mengenvorzeichen.
            IsReversal: line.NetSalesValue < 0m || (line.NetSalesValue == 0m && line.Quantity < 0m));

        var basis = ResolveCostBasis(line, context);

        return new GroupMarginEvaluation(
            SupplierType: supplierType,
            MaterialKey: materialKey,
            CostBasis: basis.CostBasis,
            CostCurrency: basis.CostCurrency,
            IsGroupCost: basis.IsGroupCost,
            IsGroupCostMissing: basis.IsGroupCostMissing,
            CostSource: ResolveCostSource(supplierType, basis),
            Status: ResolveStatus(supplierType, basis, line.NetSalesValue, hasExchangeRate));
    }

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
            return GroupCostSourceLabel;
        if (basis.IsGroupCostMissing)
            return GroupMarginStatuses.GroupCostMissingSource;

        return supplierType switch
        {
            GroupMarginSupplierClassifier.Internal => "Interner Standardpreis",
            GroupMarginSupplierClassifier.External => "Kosten aus Verkaufszeile",
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
    bool IsGroupCostMissing = false);

/// <summary>Was die Regeln ausser der Zeile selbst brauchen.</summary>
public sealed record GroupMarginCostContext(
    string MaterialKey,
    IReadOnlyDictionary<(string MaterialKey, string ValuationArea), GroupStandardCost> GroupStandardCosts,
    bool IsReversal);

/// <summary>Vollstaendiges Ergebnis fuer eine Zeile.</summary>
public sealed record GroupMarginEvaluation(
    string SupplierType,
    string MaterialKey,
    decimal CostBasis,
    string CostCurrency,
    bool IsGroupCost,
    bool IsGroupCostMissing,
    string CostSource,
    string Status);

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
    /// Kostenquelle (aktuell nur TR AG / MBEW-STPRS, siehe <see cref="GroupStandardCostAreas"/>)
    /// UND fuer das Material liegt ein Treffer vor. Das ist der eigentliche Zweck der
    /// Gruppenmarge: der lokal gespeicherte Wert wird durch die Konzernkosten ersetzt.
    /// </summary>
    public static readonly GroupMarginCostRule GroupStandardCost = new(
        nameof(GroupStandardCost),
        "Konzern-Herstellkosten der liefernden Gesellschaft (MBEW-STPRS).",
        (line, context) =>
        {
            var deliveringEntity = GroupMarginSupplierClassifier.ResolveDeliveringEntity(
                line.SupplierName, line.Tsc, context.MaterialKey, context.GroupStandardCosts, line.SalesType);

            if (deliveringEntity is null ||
                !GroupStandardCostAreas.ByEntity.TryGetValue(deliveringEntity, out var area) ||
                !context.GroupStandardCosts.TryGetValue((context.MaterialKey, area), out var groupCost) ||
                groupCost.UnitCost <= 0m)
            {
                return null;
            }

            var magnitude = Magnitude(line.Quantity, groupCost.UnitCost);
            return new GroupMarginCostBasis(
                context.IsReversal ? -magnitude : magnitude, groupCost.Currency, IsGroupCost: true);
        });

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
