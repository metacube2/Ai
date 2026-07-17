namespace TrafagSalesExporter.Services;

/// <summary>
/// Deckungsbeitrag (DB) = Umsatz minus VARIABLE Kostenbasis (Fachinput Andreas 2026-07-17:
/// Fixkosten bleiben bei Artikelwegfall bestehen, variable Kosten entfallen tatsaechlich).
/// Rein additiv zur bestehenden Gruppenmarge: gerechnet wird NUR, wenn die Quelle einen
/// fix/variabel-Split geliefert hat (StandardCostVariable ist nicht null); sonst bleibt der
/// DB offen (null) — fehlende Daten werden nie geschaetzt. Wird von Dashboard
/// (ManagementCockpitService) und Excel (ExcelExportService) gemeinsam genutzt, damit beide
/// Sichten identisch rechnen.
/// </summary>
public static class ContributionMarginCalculator
{
    /// <param name="VariableCostBasis">Variable Kostenbasis in Verkaufswaehrung; null wenn kein Split oder Waehrung maskiert.</param>
    /// <param name="ContributionMargin">Deckungsbeitrag in Verkaufswaehrung; null wenn offen.</param>
    /// <param name="ContributionMarginPercent">DB in Prozent vom Umsatz; null wenn offen oder Umsatz 0.</param>
    public sealed record Result(decimal? VariableCostBasis, decimal? ContributionMargin, decimal? ContributionMarginPercent);

    public static readonly Result Open = new(null, null, null);

    /// <summary>
    /// Variable Kostenbasis mit derselben Vorzeichenregel wie die Margen-Kostenbasis:
    /// bei Gutschriften/Retouren (negativer Umsatz, bei 0 das Mengenvorzeichen) kehrt die
    /// Kostenbasis mit um, sonst wuerden die Kosten doppelt negativ gerechnet.
    /// </summary>
    public static decimal? ResolveVariableCostBasis(decimal quantity, decimal salesValue, decimal? variableUnitCost)
    {
        if (!variableUnitCost.HasValue)
            return null;

        var isReversal = salesValue < 0m || (salesValue == 0m && quantity < 0m);
        var magnitude = quantity != 0m
            ? Math.Abs(quantity) * Math.Abs(variableUnitCost.Value)
            : Math.Abs(variableUnitCost.Value);
        return isReversal ? -magnitude : magnitude;
    }

    /// <summary>
    /// Kompletter DB je Zeile inkl. Waehrungsregel: abweichende Kostenwaehrung folgt exakt dem
    /// Gruppenmarge-Schalter (Mask/Convert) ueber <see cref="GroupMarginCostCurrencyConverter"/>.
    /// </summary>
    public static Result Resolve(
        decimal quantity,
        decimal salesValue,
        decimal? variableUnitCost,
        string? salesCurrency,
        string? costCurrency,
        int year,
        string? costCurrencyMode,
        Func<string, string, DateTime, decimal?>? resolveRate)
    {
        var basis = ResolveVariableCostBasis(quantity, salesValue, variableUnitCost);
        if (!basis.HasValue)
            return Open;

        var conversion = GroupMarginCostCurrencyConverter.Resolve(
            basis.Value, salesCurrency, costCurrency, year, costCurrencyMode, resolveRate);
        if (conversion.IsMasked)
            return Open;

        var contributionMargin = salesValue - conversion.CostBasis;
        decimal? percent = salesValue == 0m ? null : contributionMargin / salesValue * 100m;
        return new Result(conversion.CostBasis, contributionMargin, percent);
    }
}
