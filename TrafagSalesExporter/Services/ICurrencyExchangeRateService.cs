namespace TrafagSalesExporter.Services;

public interface ICurrencyExchangeRateService
{
    decimal? ResolveRate(string fromCurrency, string toCurrency, DateTime? effectiveDate);
    string NormalizeCurrencyCode(string? currencyCode);
}
