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

    private static CentralSalesRecord CreateRow(string sourceSystem, string land, string tsc, string invoiceNumber, string currency, decimal salesValue, DateTime? invoiceDate, DateTime? extractionDate = null)
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
            Quantity = 1m,
            SupplierNumber = "SUP",
            SupplierName = "Supplier",
            SupplierCountry = "CH",
            CustomerNumber = "CUS",
            CustomerName = "Customer",
            CustomerCountry = "CH",
            CustomerIndustry = "Industry",
            StandardCost = 1m,
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
}
