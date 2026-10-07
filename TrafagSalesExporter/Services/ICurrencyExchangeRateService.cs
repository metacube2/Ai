using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface ICurrencyExchangeRateService
{
    decimal? ResolveRate(string fromCurrency, string toCurrency, DateTime? effectiveDate);
    string NormalizeCurrencyCode(string? currencyCode);

    /// <summary>
    /// Budgetkurs des Jahres (Kurs mit Notiz „Budget &lt;Jahr&gt;"), unabhaengig von juengeren
    /// Tageskursen. Standard-Rueckfall fuer Implementierungen ohne Notizen: Kurs zum 31.12. des Jahres.
    /// </summary>
    decimal? ResolveBudgetRate(string fromCurrency, string toCurrency, int year)
        => ResolveRate(fromCurrency, toCurrency, new DateTime(year, 12, 31));

    /// <summary>
    /// CHF-Finance-Kurs nach Kursprofil. Seit 2026-10-07 ist der Budgetkurs je Finance-Jahr der
    /// Standard (Entscheid Ingo: Trafag arbeitet immer mit Budgetkursen).
    /// </summary>
    decimal? ResolveFinanceRate(string fromCurrency, string toCurrency, int financeYear, string? mode)
        => GroupMarginChfRateModes.Normalize(mode) == GroupMarginChfRateModes.BudgetRate
            ? ResolveBudgetRate(fromCurrency, toCurrency, financeYear)
            : ResolveRate(fromCurrency, toCurrency, GroupMarginChfRateModes.ResolveRateDate(mode, financeYear));
}
