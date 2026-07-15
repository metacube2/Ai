using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Zentrale Regel fuer Gruppenmarge bei abweichender Kostenwaehrung (Fachentscheid D, Andreas).
/// Wird von Dashboard (ManagementCockpitService) und Excel-Nachweis (ExcelExportService)
/// gemeinsam genutzt, damit beide Sichten identisch rechnen.
///
/// Verhalten je Modus (ExportSettings.GroupMarginCostCurrencyMode):
/// - Mask: Kostenbasis gilt als offen, Marge/% werden maskiert (Status <see cref="OpenStatus"/>).
/// - Convert: Kostenbasis wird mit dem Jahreskurs (31.12. des Finance-Jahres) in die
///   Verkaufswaehrung umgerechnet; ohne verfuegbaren Kurs faellt die Zeile auf Mask zurueck.
/// Stimmen die Waehrungen ueberein (oder fehlt eine Angabe), bleibt alles unveraendert.
/// </summary>
public static class GroupMarginCostCurrencyConverter
{
    /// <summary>Status fuer Zeilen, deren Marge wegen abweichender Kostenwaehrung offen bleibt.</summary>
    public const string OpenStatus = "Kostenwaehrung abweichend";

    /// <param name="CostBasis">Kostenbasis in Verkaufswaehrung; nur belastbar, wenn nicht maskiert.</param>
    public sealed record Result(decimal CostBasis, decimal? AppliedRate, bool IsMismatch, bool IsMasked);

    public static string NormalizeMode(string? mode)
        => string.Equals(mode?.Trim(), GroupMarginCostCurrencyModes.Convert, StringComparison.OrdinalIgnoreCase)
            ? GroupMarginCostCurrencyModes.Convert
            : GroupMarginCostCurrencyModes.Mask;

    public static bool IsMismatch(string? salesCurrency, string? costCurrency)
        => !string.IsNullOrWhiteSpace(salesCurrency)
           && !string.IsNullOrWhiteSpace(costCurrency)
           && !string.Equals(salesCurrency.Trim(), costCurrency.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <param name="resolveRate">(von, nach, Stichtag) -> Kurs; null = keine Kursquelle verfuegbar.</param>
    public static Result Resolve(
        decimal costBasis,
        string? salesCurrency,
        string? costCurrency,
        int year,
        string? mode,
        Func<string, string, DateTime, decimal?>? resolveRate)
    {
        if (costBasis == 0m || !IsMismatch(salesCurrency, costCurrency))
            return new Result(costBasis, null, false, false);

        if (NormalizeMode(mode) == GroupMarginCostCurrencyModes.Convert && resolveRate is not null)
        {
            var rate = resolveRate(costCurrency!.Trim(), salesCurrency!.Trim(), new DateTime(year, 12, 31));
            if (rate.HasValue)
                return new Result(costBasis * rate.Value, rate, true, false);
        }

        return new Result(costBasis, null, true, true);
    }
}
