using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class CentralSalesRecordServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TestDbContextFactory _dbFactory;

    public CentralSalesRecordServiceTests()
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
    public async Task ReplaceForSiteAsync_Persists_B1_Document_Currency_Fields()
    {
        var site = new Site
        {
            Id = 1,
            Schema = "SBODEMO",
            TSC = "TRCH",
            Land = "Schweiz",
            SourceSystem = "BI1",
            IsActive = true
        };

        await using (var db = await _dbFactory.CreateDbContextAsync())
        {
            db.Sites.Add(site);
            await db.SaveChangesAsync();
        }

        var service = new CentralSalesRecordService(_dbFactory, new NullAppEventLogService());
        await service.ReplaceForSiteAsync(site, [
            new SalesRecord
            {
                ExtractionDate = new DateTime(2026, 4, 29),
                Tsc = "TRCH",
                DocumentEntry = 999,
                InvoiceNumber = "1001",
                PositionOnInvoice = 1,
                Material = "MAT",
                Name = "Article",
                ProductGroup = "PG",
                Quantity = 2m,
                StandardCost = 10m,
                StandardCostCurrency = "CHF",
                SalesPriceValue = 25m,
                SalesCurrency = "EUR",
                DocumentCurrency = "EUR",
                DocumentTotalForeignCurrency = 100m,
                DocumentTotalLocalCurrency = 95m,
                VatSumForeignCurrency = 8m,
                VatSumLocalCurrency = 7.6m,
                DocumentRate = 0.95m,
                CompanyCurrency = "CHF",
                PostingDate = new DateTime(2026, 4, 28),
                InvoiceDate = new DateTime(2026, 4, 29),
                Land = "Schweiz",
                DocumentType = "INV"
            }
        ]);

        var rows = await service.GetAllAsync();

        var row = Assert.Single(rows);
        Assert.Equal(999, row.DocumentEntry);
        Assert.Equal("EUR", row.DocumentCurrency);
        Assert.Equal(100m, row.DocumentTotalForeignCurrency);
        Assert.Equal(95m, row.DocumentTotalLocalCurrency);
        Assert.Equal(8m, row.VatSumForeignCurrency);
        Assert.Equal(7.6m, row.VatSumLocalCurrency);
        Assert.Equal(0.95m, row.DocumentRate);
        Assert.Equal("CHF", row.CompanyCurrency);
        Assert.Equal(new DateTime(2026, 4, 28), row.PostingDate);
        Assert.Equal(new DateTime(2026, 4, 29), row.InvoiceDate);
    }

    private sealed class NullAppEventLogService : IAppEventLogService
    {
        public Task WriteAsync(string category, string message, string level = "Info", int? siteId = null, string? land = null, string? details = null)
            => Task.CompletedTask;

        public Task WriteDebugAsync(string category, string message, int? siteId = null, string? land = null, string? details = null)
            => Task.CompletedTask;
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
