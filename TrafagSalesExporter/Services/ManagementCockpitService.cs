using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public class ManagementCockpitService : IManagementCockpitService
{
    private readonly CockpitValueAggregator _aggregator;
    private readonly ExcelCockpitAnalyzer _excelAnalyzer;
    private readonly CentralCockpitAnalyzer _centralAnalyzer;
    private readonly FinanceSummaryAnalyzer _financeAnalyzer;

    public ManagementCockpitService(IDbContextFactory<AppDbContext> dbFactory)
        : this(dbFactory, new CurrencyExchangeRateService(dbFactory))
    {
    }

    public ManagementCockpitService(IDbContextFactory<AppDbContext> dbFactory, ICurrencyExchangeRateService exchangeRateService)
    {
        _aggregator = new CockpitValueAggregator(exchangeRateService);
        _excelAnalyzer = new ExcelCockpitAnalyzer(dbFactory, _aggregator);
        _centralAnalyzer = new CentralCockpitAnalyzer(dbFactory, _aggregator);
        _financeAnalyzer = new FinanceSummaryAnalyzer(dbFactory);
    }

    public Task<List<ManagementCockpitFileOption>> GetAvailableFilesAsync()
        => _excelAnalyzer.GetAvailableFilesAsync();

    public IReadOnlyList<ManagementCockpitValueFieldOption> GetValueFieldOptions()
        => _aggregator.GetValueFieldOptions();

    public Task<ManagementCockpitResult> AnalyzeAsync(string filePath)
        => _excelAnalyzer.AnalyzeAsync(filePath, null);

    public Task<ManagementCockpitResult> AnalyzeAsync(string filePath, ManagementCockpitAnalysisOptions? options)
        => _excelAnalyzer.AnalyzeAsync(filePath, options);

    public Task<List<int>> GetAvailableCentralYearsAsync()
        => _centralAnalyzer.GetAvailableCentralYearsAsync();

    public Task<ManagementCockpitCentralResult> AnalyzeCentralAsync(int year, int? month)
        => _centralAnalyzer.AnalyzeCentralAsync(year, month, null);

    public Task<ManagementCockpitCentralResult> AnalyzeCentralAsync(int year, int? month, ManagementCockpitAnalysisOptions? options)
        => _centralAnalyzer.AnalyzeCentralAsync(year, month, options);

    public Task<ManagementFinanceSummaryResult> AnalyzeFinanceSummaryAsync(int year, string? countryKey, string? currency)
        => _financeAnalyzer.AnalyzeFinanceSummaryAsync(year, countryKey, currency);
}
