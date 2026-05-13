using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class FinanceReconciliationServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TestDbContextFactory _dbFactory;

    public FinanceReconciliationServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = new AppDbContext(options);
        db.Database.EnsureCreated();

        _dbFactory = new TestDbContextFactory(options);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task BuildNetSalesReferenceRowsAsync_Uses_PostingDate_For_Year_Filter()
    {
        await using (var db = await _dbFactory.CreateDbContextAsync())
        {
            db.Sites.Add(BuildSite());
            db.FinanceReferences.Add(new FinanceReference { Key = "DE", Label = "Trafag DE", Year = 2025, CheckValue = 100m, IsActive = true });
            db.CentralSalesRecords.AddRange(
                BuildCentralRecord("TRDE", "Deutschland", 1, 1, 100m, new DateTime(2025, 1, 5), new DateTime(2024, 12, 31)),
                BuildCentralRecord("TRDE", "Deutschland", 2, 1, 999m, new DateTime(2024, 12, 31), new DateTime(2025, 1, 5)));
            await db.SaveChangesAsync();
        }

        var service = new FinanceReconciliationService(_dbFactory);

        var rows = await service.BuildNetSalesReferenceRowsAsync(2025);

        var row = Assert.Single(rows);
        Assert.Equal(100m, row.ActualValue);
        Assert.Equal("OK", row.Status);
        Assert.Equal("Nettofakturawert Hauswaehrung pro Position", row.ValueField);
    }

    [Fact]
    public async Task BuildNetSalesReferenceRowsAsync_Does_Not_Multiply_Repeated_Document_Header_Totals()
    {
        await using (var db = await _dbFactory.CreateDbContextAsync())
        {
            db.Sites.Add(BuildSite());
            db.FinanceReferences.Add(new FinanceReference { Key = "IT", Label = "Trafag IT", Year = 2025, CheckValue = 90m, IsActive = true });
            db.CentralSalesRecords.AddRange(
                BuildCentralRecord("TRIT", "Italien", 10, 1, 100m, new DateTime(2025, 2, 1), new DateTime(2025, 2, 1), vatLocal: 10m, salesPriceValue: 40m),
                BuildCentralRecord("TRIT", "Italien", 10, 2, 100m, new DateTime(2025, 2, 1), new DateTime(2025, 2, 1), vatLocal: 10m, salesPriceValue: 50m));
            await db.SaveChangesAsync();
        }

        var service = new FinanceReconciliationService(_dbFactory);

        var rows = await service.BuildNetSalesReferenceRowsAsync(2025);

        var row = Assert.Single(rows);
        Assert.Equal(90m, row.ActualValue);
        Assert.Equal("Positions-Netto (Sales Price/Value)", row.ValueField);
        Assert.Contains(row.Candidates, c => c.Key == "NetDocumentLocalCurrencyPosition" && c.Value == 180m && !c.IsPreferred);
        Assert.Contains(row.Candidates, c => c.Key == "NetDocumentLocalCurrencyDocument" && c.Value == 90m && !c.IsPreferred);
        Assert.Contains(row.Candidates, c => c.Key == "SalesPriceValue" && c.Value == 90m && c.IsPreferred);
    }

    [Fact]
    public async Task BuildNetSalesReferenceRowsAsync_Reports_India_As_Inr_House_Currency()
    {
        await using (var db = await _dbFactory.CreateDbContextAsync())
        {
            db.Sites.Add(BuildSite());
            db.FinanceReferences.Add(new FinanceReference { Key = "IN", Label = "Trafag IN", Year = 2025, CheckValue = 300m, IsActive = true });
            db.CentralSalesRecords.AddRange(
                BuildCentralRecord("TRIN", "Indien", 20, 1, 0m, new DateTime(2025, 3, 1), new DateTime(2025, 3, 1), salesPriceValue: 100m, salesCurrency: "USD"),
                BuildCentralRecord("TRIN", "Indien", 21, 1, 0m, new DateTime(2025, 3, 2), new DateTime(2025, 3, 2), salesPriceValue: 200m, salesCurrency: "EUR"));
            await db.SaveChangesAsync();
        }

        var service = new FinanceReconciliationService(_dbFactory);

        var rows = await service.BuildNetSalesReferenceRowsAsync(2025);

        var row = Assert.Single(rows);
        Assert.Equal(300m, row.ActualValue);
        Assert.Equal("INR", row.ActualCurrency);
        Assert.Equal("INR", row.Currencies);
        Assert.All(row.Candidates, candidate => Assert.NotEqual("EUR, USD", candidate.Currency));
    }

    private static CentralSalesRecord BuildCentralRecord(
        string tsc,
        string land,
        int documentEntry,
        int position,
        decimal documentTotalLocal,
        DateTime postingDate,
        DateTime invoiceDate,
        decimal vatLocal = 0m,
        decimal? salesPriceValue = null,
        string salesCurrency = "EUR")
        => new()
        {
            StoredAtUtc = DateTime.UtcNow,
            SiteId = 1,
            SourceSystem = "TEST",
            ExtractionDate = DateTime.UtcNow,
            Tsc = tsc,
            DocumentEntry = documentEntry,
            InvoiceNumber = documentEntry.ToString(),
            PositionOnInvoice = position,
            SalesPriceValue = salesPriceValue ?? documentTotalLocal - vatLocal,
            SalesCurrency = salesCurrency,
            DocumentCurrency = salesCurrency,
            DocumentTotalLocalCurrency = documentTotalLocal,
            VatSumLocalCurrency = vatLocal,
            CompanyCurrency = "EUR",
            PostingDate = postingDate,
            InvoiceDate = invoiceDate,
            Land = land,
            DocumentType = "INV"
        };

    private static Site BuildSite()
        => new()
        {
            Id = 1,
            Schema = "TEST",
            TSC = "TEST",
            Land = "Test",
            SourceSystem = "TEST",
            IsActive = true
        };

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
