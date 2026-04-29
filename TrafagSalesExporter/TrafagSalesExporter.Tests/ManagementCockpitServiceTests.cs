using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class ManagementCockpitServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TestDbContextFactory _dbFactory;
    private readonly ManagementCockpitService _service;

    public ManagementCockpitServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using (var db = new AppDbContext(options))
        {
            db.Database.EnsureCreated();
            if (!db.Sites.Any())
            {
                db.Sites.Add(new Site
                {
                    Id = 1,
                    HanaServerId = null,
                    Schema = "test",
                    TSC = "TEST",
                    Land = "Testland",
                    SourceSystem = "SAP",
                    IsActive = true
                });
                db.SaveChanges();
            }
        }

        _dbFactory = new TestDbContextFactory(options);
        _service = new ManagementCockpitService(_dbFactory);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task GetAvailableCentralYearsAsync_Returns_Distinct_Ordered_Years()
    {
        await SeedCentralRowsAsync(
            CreateRow("SAP", "CH", "TRCH", "INV-1", "CHF", 100m, new DateTime(2025, 1, 10)),
            CreateRow("SAP", "CH", "TRCH", "INV-2", "CHF", 200m, new DateTime(2026, 2, 10)),
            CreateRow("SAP", "CH", "TRCH", "INV-3", "CHF", 300m, null, new DateTime(2026, 3, 5)));

        var years = await _service.GetAvailableCentralYearsAsync();

        Assert.Equal([2025, 2026], years);
    }

    [Fact]
    public async Task AnalyzeCentralAsync_Uses_InvoiceDate_Or_ExtractionDate_And_Builds_Monthly_Daily_And_Source_Totals()
    {
        await SeedCentralRowsAsync(
            CreateRow("SAP", "Schweiz", "TRCH", "INV-1", "CHF", 100m, new DateTime(2025, 1, 10)),
            CreateRow("MANUAL_EXCEL", "Deutschland", "TRDE", "INV-2", "EUR", 50m, new DateTime(2025, 1, 11)),
            CreateRow("SAP", "Deutschland", "TRDE", "INV-3", "EUR", 25m, null, new DateTime(2025, 1, 12)),
            CreateRow("SAP", "Schweiz", "TRCH", "INV-4", "CHF", 70m, new DateTime(2026, 2, 5)));

        var result = await _service.AnalyzeCentralAsync(2025, 1);

        Assert.Equal(2025, result.Filter.Year);
        Assert.Equal(1, result.Filter.Month);
        Assert.Equal(3, result.Summary.RowCount);
        Assert.Equal(3, result.Summary.InvoiceCount);
        Assert.Equal(2, result.Summary.SiteCount);
        Assert.Equal(2, result.Summary.CountryCount);
        Assert.Equal(2, result.Summary.CurrencyCount);
        Assert.Equal(new DateTime(2025, 1, 10), result.Summary.PeriodStart);
        Assert.Equal(new DateTime(2025, 1, 12), result.Summary.PeriodEnd);

        var yearly2025Chf = Assert.Single(result.YearlyTotals, x => x.Year == 2025 && x.Currency == "CHF");
        Assert.Equal(100m, yearly2025Chf.SalesValue);

        var yearly2025Eur = Assert.Single(result.YearlyTotals, x => x.Year == 2025 && x.Currency == "EUR");
        Assert.Equal(75m, yearly2025Eur.SalesValue);

        var januaryChf = Assert.Single(result.MonthlyTotals, x => x.Label == "2025-01" && x.Currency == "CHF");
        Assert.Equal(100m, januaryChf.SalesValue);

        var januaryEur = Assert.Single(result.MonthlyTotals, x => x.Label == "2025-01" && x.Currency == "EUR");
        Assert.Equal(75m, januaryEur.SalesValue);

        Assert.Equal(3, result.DailyTotals.Count);
        Assert.Contains(result.DailyTotals, x => x.Label == "2025-01-12" && x.Currency == "EUR" && x.SalesValue == 25m);

        var sapTotal = Assert.Single(result.SourceSystemTotals, x => x.Label == "SAP" && x.Currency == "CHF");
        Assert.Equal(100m, sapTotal.SalesValue);

        var manualTotal = Assert.Single(result.SourceSystemTotals, x => x.Label == "MANUAL_EXCEL" && x.Currency == "EUR");
        Assert.Equal(50m, manualTotal.SalesValue);

        var germanyEur = Assert.Single(result.CountryTotals, x => x.Label == "Deutschland" && x.Currency == "EUR");
        Assert.Equal(75m, germanyEur.SalesValue);
        Assert.Equal(2, germanyEur.InvoiceCount);
    }

    [Fact]
    public async Task AnalyzeCentralAsync_With_Year_Only_Does_Not_Build_DailyTotals()
    {
        await SeedCentralRowsAsync(
            CreateRow("SAP", "Schweiz", "TRCH", "INV-1", "CHF", 100m, new DateTime(2025, 1, 10)),
            CreateRow("SAP", "Schweiz", "TRCH", "INV-2", "CHF", 150m, new DateTime(2025, 2, 10)));

        var result = await _service.AnalyzeCentralAsync(2025, null);

        Assert.Empty(result.DailyTotals);
        Assert.Equal(2, result.MonthlyTotals.Count);
    }

    [Fact]
    public async Task AnalyzeCentralAsync_Can_Convert_Selected_Value_To_Eur()
    {
        await SeedRatesAsync(
            CreateRate("EUR", "CHF", 2m),
            CreateRate("EUR", "USD", 1.25m));
        await SeedCentralRowsAsync(
            CreateRow("SAP", "Schweiz", "TRCH", "INV-1", "CHF", 100m, new DateTime(2025, 1, 10)),
            CreateRow("SAP", "USA", "TRUS", "INV-2", "USD", 100m, new DateTime(2025, 1, 11)),
            CreateRow("SAP", "Deutschland", "TRDE", "INV-3", "EUR", 100m, new DateTime(2025, 1, 12)));

        var result = await _service.AnalyzeCentralAsync(2025, null, new ManagementCockpitAnalysisOptions
        {
            ValueField = ManagementCockpitValueFieldKeys.SalesPriceValue,
            TargetCurrency = ManagementCockpitCurrencyOptions.Eur
        });

        Assert.Equal("EUR", result.Summary.DisplayCurrency);
        Assert.Equal(230m, result.Summary.ValueTotal);
        Assert.Equal(0, result.Summary.MissingExchangeRateCount);

        Assert.All(result.CountryTotals, row => Assert.Equal("EUR", row.Currency));
        Assert.Equal(50m, Assert.Single(result.CountryTotals, x => x.Label == "Schweiz").SalesValue);
        Assert.Equal(80m, Assert.Single(result.CountryTotals, x => x.Label == "USA").SalesValue);
        Assert.Equal(100m, Assert.Single(result.CountryTotals, x => x.Label == "Deutschland").SalesValue);
    }

    [Fact]
    public async Task AnalyzeCentralAsync_Caches_Exchange_Rates_Per_Currency_Target_And_Date()
    {
        var exchangeRates = new CountingCurrencyExchangeRateService();
        var service = new ManagementCockpitService(_dbFactory, exchangeRates);

        await SeedCentralRowsAsync(
            CreateRow("SAP", "USA", "TRUS", "INV-1", "USD", 100m, new DateTime(2025, 1, 10), quantity: 2m, standardCost: 10m),
            CreateRow("SAP", "USA", "TRUS", "INV-2", "USD", 50m, new DateTime(2025, 1, 10), quantity: 3m, standardCost: 20m));

        var result = await service.AnalyzeCentralAsync(2025, 1, new ManagementCockpitAnalysisOptions
        {
            ValueField = ManagementCockpitValueFieldKeys.SalesPriceValue,
            AdditionalValueFields = [ManagementCockpitValueFieldKeys.StandardCostTotal],
            TargetCurrency = ManagementCockpitCurrencyOptions.Eur
        });

        Assert.Equal(300m, result.Summary.ValueTotal);
        Assert.Equal(160m, Assert.Single(result.MonthlyTotals).AdditionalValues[ManagementCockpitValueFieldKeys.StandardCostTotal].Value);
        Assert.Equal(1, exchangeRates.ResolveRateCallCount);
    }

    [Fact]
    public async Task AnalyzeCentralAsync_Can_Sum_Quantity_Without_Currency_Conversion()
    {
        await SeedCentralRowsAsync(
            CreateRow("SAP", "Schweiz", "TRCH", "INV-1", "CHF", 100m, new DateTime(2025, 1, 10), quantity: 2m),
            CreateRow("SAP", "USA", "TRUS", "INV-2", "USD", 100m, new DateTime(2025, 1, 11), quantity: 3m));

        var result = await _service.AnalyzeCentralAsync(2025, null, new ManagementCockpitAnalysisOptions
        {
            ValueField = ManagementCockpitValueFieldKeys.Quantity,
            TargetCurrency = ManagementCockpitCurrencyOptions.Eur
        });

        Assert.Equal(ManagementCockpitValueFieldKeys.Quantity, result.Summary.ValueFieldKey);
        Assert.Equal("-", result.Summary.DisplayCurrency);
        Assert.Equal(5m, result.Summary.ValueTotal);
        Assert.Equal(0, result.Summary.MissingExchangeRateCount);
        Assert.Equal(2m, Assert.Single(result.CountryTotals, x => x.Label == "Schweiz").SalesValue);
        Assert.Equal(3m, Assert.Single(result.CountryTotals, x => x.Label == "USA").SalesValue);
    }

    [Fact]
    public async Task AnalyzeCentralAsync_Adds_Selected_Additional_Value_Fields_To_Time_Rows()
    {
        await SeedCentralRowsAsync(
            CreateRow("SAP", "Deutschland", "TRDE", "INV-1", "EUR", 100m, new DateTime(2025, 1, 10), quantity: 2m, standardCost: 5m),
            CreateRow("SAP", "Deutschland", "TRDE", "INV-2", "EUR", 50m, new DateTime(2025, 2, 10), quantity: 3m, standardCost: 7m));

        var result = await _service.AnalyzeCentralAsync(2025, null, new ManagementCockpitAnalysisOptions
        {
            ValueField = ManagementCockpitValueFieldKeys.SalesPriceValue,
            AdditionalValueFields =
            [
                ManagementCockpitValueFieldKeys.Quantity,
                ManagementCockpitValueFieldKeys.StandardCostTotal
            ],
            TargetCurrency = ManagementCockpitCurrencyOptions.Eur
        });

        Assert.Equal(2, result.AdditionalValueFields.Count);

        var yearly = Assert.Single(result.YearlyTotals);
        Assert.Equal(150m, yearly.SalesValue);
        Assert.Equal(5m, yearly.AdditionalValues[ManagementCockpitValueFieldKeys.Quantity].Value);
        Assert.Equal("-", yearly.AdditionalValues[ManagementCockpitValueFieldKeys.Quantity].Currency);
        Assert.Equal(31m, yearly.AdditionalValues[ManagementCockpitValueFieldKeys.StandardCostTotal].Value);
        Assert.Equal("EUR", yearly.AdditionalValues[ManagementCockpitValueFieldKeys.StandardCostTotal].Currency);

        Assert.Contains(result.MonthlyTotals, row =>
            row.Label == "2025-01" &&
            row.AdditionalValues[ManagementCockpitValueFieldKeys.Quantity].Value == 2m);
        Assert.Contains(result.MonthlyTotals, row =>
            row.Label == "2025-02" &&
            row.AdditionalValues[ManagementCockpitValueFieldKeys.StandardCostTotal].Value == 21m);
    }

    [Fact]
    public async Task AnalyzeCentralAsync_Throws_When_No_Rows_Exist_For_Selected_Period()
    {
        await SeedCentralRowsAsync(
            CreateRow("SAP", "Schweiz", "TRCH", "INV-1", "CHF", 100m, new DateTime(2025, 1, 10)));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AnalyzeCentralAsync(2026, 1));

        Assert.Contains("gewählten Zeitraum", ex.Message);
    }

    private async Task SeedCentralRowsAsync(params CentralSalesRecord[] rows)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.CentralSalesRecords.RemoveRange(db.CentralSalesRecords);
        await db.SaveChangesAsync();
        db.CentralSalesRecords.AddRange(rows);
        await db.SaveChangesAsync();
    }

    private async Task SeedRatesAsync(params CurrencyExchangeRate[] rates)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.CurrencyExchangeRates.RemoveRange(db.CurrencyExchangeRates);
        await db.SaveChangesAsync();
        db.CurrencyExchangeRates.AddRange(rates);
        await db.SaveChangesAsync();
    }

    private static CurrencyExchangeRate CreateRate(string fromCurrency, string toCurrency, decimal rate)
        => new()
        {
            FromCurrency = fromCurrency,
            ToCurrency = toCurrency,
            Rate = rate,
            ValidFrom = new DateTime(2024, 1, 1),
            IsActive = true
        };

    private static CentralSalesRecord CreateRow(
        string sourceSystem,
        string land,
        string tsc,
        string invoiceNumber,
        string currency,
        decimal salesValue,
        DateTime? invoiceDate,
        DateTime? extractionDate = null,
        decimal quantity = 1m,
        decimal standardCost = 1m)
    {
        return new CentralSalesRecord
        {
            SiteId = 1,
            StoredAtUtc = DateTime.UtcNow,
            SourceSystem = sourceSystem,
            ExtractionDate = extractionDate ?? invoiceDate ?? DateTime.UtcNow.Date,
            Tsc = tsc,
            InvoiceNumber = invoiceNumber,
            PositionOnInvoice = 1,
            Material = "MAT",
            Name = "Article",
            ProductGroup = "PG",
            Quantity = quantity,
            SupplierNumber = "SUP",
            SupplierName = "Supplier",
            SupplierCountry = "CH",
            CustomerNumber = "CUS",
            CustomerName = "Customer",
            CustomerCountry = "CH",
            CustomerIndustry = "Industry",
            StandardCost = standardCost,
            StandardCostCurrency = currency,
            PurchaseOrderNumber = "PO",
            SalesPriceValue = salesValue,
            SalesCurrency = currency,
            Incoterms2020 = "DAP",
            SalesResponsibleEmployee = "Alice",
            InvoiceDate = invoiceDate,
            OrderDate = invoiceDate?.AddDays(-2),
            Land = land,
            DocumentType = "Invoice"
        };
    }

    private sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;

        public TestDbContextFactory(DbContextOptions<AppDbContext> options)
        {
            _options = options;
        }

        public AppDbContext CreateDbContext() => new(_options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new AppDbContext(_options));
    }

    private sealed class CountingCurrencyExchangeRateService : ICurrencyExchangeRateService
    {
        public int ResolveRateCallCount { get; private set; }

        public decimal? ResolveRate(string fromCurrency, string toCurrency, DateTime? effectiveDate)
        {
            ResolveRateCallCount++;
            return 2m;
        }

        public string NormalizeCurrencyCode(string? currencyCode)
            => string.IsNullOrWhiteSpace(currencyCode) ? string.Empty : currencyCode.Trim().ToUpperInvariant();
    }
}
