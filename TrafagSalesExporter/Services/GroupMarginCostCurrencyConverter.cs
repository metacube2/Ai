using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Zentrale Regel fuer Gruppenmarge bei abweichender Kostenwaehrung (Fachentscheid D, Andreas).
/// Wird von Dashboard (ManagementCockpitService) und Excel-Nachweis (ExcelExportService)
/// gemeinsam genutzt, damit beide Sichten identisch rechnen.
///
/// Verhalten je Modus (ExportSettings.GroupMarginCostCurrencyMode):
/// - Convert (Regel seit dem Beschluss vom 2026-08-27): Kostenbasis wird mit dem
///   <see cref="ResolveRateDate">Tageskurs</see> in die Verkaufswaehrung umgerechnet; ohne
///   verfuegbaren Kurs faellt die Zeile auf Mask zurueck.
/// - Mask (nur noch als bewusst gewaehlte Ausnahme): Kostenbasis gilt als offen, Marge/%
///   werden maskiert (Status <see cref="OpenStatus"/>).
/// Stimmen die Waehrungen ueberein (oder fehlt eine Angabe), bleibt alles unveraendert.
/// </summary>
public static class GroupMarginCostCurrencyConverter
{
    /// <summary>Status fuer Zeilen, deren Marge wegen abweichender Kostenwaehrung offen bleibt.</summary>
    public const string OpenStatus = "Kostenwaehrung abweichend";

    /// <param name="CostBasis">Kostenbasis in Verkaufswaehrung; nur belastbar, wenn nicht maskiert.</param>
    public sealed record Result(decimal CostBasis, decimal? AppliedRate, bool IsMismatch, bool IsMasked);

    /// <summary>
    /// Stichtag der Umrechnung: **der aktuelle Tag**, nicht das Jahresende der Verkaufszeile.
    ///
    /// Beschluss Andreas vom 2026-08-27 (`docs/FINANCE_STANDARDKOSTEN.md` Abschnitt 11, B6):
    /// kein Rueckrechnen auf historische Kurse. Bis dahin galt der Jahreskurs zum 31.12. des
    /// Finance-Jahres. Bewusste Folge: dieselbe Zeile kann an zwei Tagen zwei Margen ergeben,
    /// wenn sich der Kurs dazwischen aendert. Genau das ist gewollt, weil die Gruppenmarge mit
    /// aktuellen Kursen gerechnet und mit anderen Kursen durchsimuliert werden soll.
    ///
    /// Die offizielle Umrechnung des Umsatzes nach CHF (Group-Currency-Ansicht, Pruefbuch)
    /// bleibt davon unberuehrt und rechnet weiter mit dem Jahreskurs; dieser Punkt haengt an
    /// `ISS-008` und ist nicht entschieden.
    /// </summary>
    public static DateTime ResolveRateDate(DateTime? today = null) => (today ?? DateTime.Today).Date;

    public static string NormalizeMode(string? mode)
        => string.Equals(mode?.Trim(), GroupMarginCostCurrencyModes.Mask, StringComparison.OrdinalIgnoreCase)
            ? GroupMarginCostCurrencyModes.Mask
            : GroupMarginCostCurrencyModes.Convert;

    public static bool IsMismatch(string? salesCurrency, string? costCurrency)
        => !string.IsNullOrWhiteSpace(salesCurrency)
           && !string.IsNullOrWhiteSpace(costCurrency)
           && !string.Equals(salesCurrency.Trim(), costCurrency.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <param name="resolveRate">(von, nach, Stichtag) -> Kurs; null = keine Kursquelle verfuegbar.</param>
    /// <param name="today">Nur fuer Tests; sonst gilt der aktuelle Tag.</param>
    public static Result Resolve(
        decimal costBasis,
        string? salesCurrency,
        string? costCurrency,
        string? mode,
        Func<string, string, DateTime, decimal?>? resolveRate,
        DateTime? today = null)
    {
        if (costBasis == 0m || !IsMismatch(salesCurrency, costCurrency))
            return new Result(costBasis, null, false, false);

        if (NormalizeMode(mode) == GroupMarginCostCurrencyModes.Convert && resolveRate is not null)
        {
            var rate = resolveRate(costCurrency!.Trim(), salesCurrency!.Trim(), ResolveRateDate(today));
            if (rate.HasValue)
                return new Result(costBasis * rate.Value, rate, true, false);
        }

        return new Result(costBasis, null, true, true);
    }
}
