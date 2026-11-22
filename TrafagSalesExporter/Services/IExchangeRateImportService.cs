namespace TrafagSalesExporter.Services;

public interface IExchangeRateImportService
{
    Task<ExchangeRateImportResult> RefreshEcbRatesAsync(CancellationToken cancellationToken = default);
}

public sealed class ExchangeRateImportResult
{
    public int ImportedCount { get; init; }
    public DateTime RateDate { get; init; }
    public string SourceName { get; init; } = string.Empty;
}
