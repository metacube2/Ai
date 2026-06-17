using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public sealed class ExportAuditCsvServiceTests : IDisposable
{
    private readonly string _tempDirectory;

    public ExportAuditCsvServiceTests()
    {
        _tempDirectory = Path.Combine("C:\\TMP", $"trafag-audit-csv-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, recursive: true);
    }

    [Fact]
    public async Task WriteSiteAuditCsvAsync_Roundtrips_Transformed_SalesRecord()
    {
        var service = new ExportAuditCsvService();
        var settings = new ExportSettings
        {
            AuditCsvEnabled = true,
            LocalSiteExportFolder = _tempDirectory,
            LocalAuditCsvFolder = Path.Combine(_tempDirectory, "ignored")
        };
        var site = new Site { TSC = "TRCH", Land = "Schweiz" };
        var record = new SalesRecord
        {
            SourceSystem = "SAP",
            ExtractionDate = new DateTime(2026, 6, 11, 8, 30, 0, DateTimeKind.Utc),
            Tsc = "TRCH",
            SourceLineId = "line-1",
            DocumentEntry = 42,
            InvoiceNumber = "INV-1",
            PositionOnInvoice = 7,
            Material = "MAT;1",
            Name = "Artikel \"Audit\"",
            ProductDivisionCode = "0001",
            ProductDivisionText = "Pressure",
            ProductMappingAssigned = "TRUE",
            Quantity = 2.5m,
            SalesPriceValue = 1234.56m,
            SalesCurrency = "CHF",
            DocumentCurrency = "EUR",
            DocumentTotalForeignCurrency = 1300m,
            DocumentTotalLocalCurrency = 1234.56m,
            VatSumForeignCurrency = 0m,
            VatSumLocalCurrency = 0m,
            DocumentRate = 0.95m,
            CompanyCurrency = "CHF",
            PostingDate = new DateTime(2026, 6, 10),
            InvoiceDate = new DateTime(2026, 6, 11),
            Land = "Schweiz",
            DocumentType = "INV"
        };

        var path = await service.WriteSiteAuditCsvAsync(site, settings, "SAP", _tempDirectory, [record]);

        Assert.True(File.Exists(path));
        Assert.Equal(_tempDirectory, Path.GetDirectoryName(path));
        Assert.StartsWith("Sales_ProcessedMergeInput_TRCH_", Path.GetFileName(path), StringComparison.OrdinalIgnoreCase);
        var records = await service.ReadLatestSiteAuditCsvRecordsAsync(settings);
        var roundtrip = Assert.Single(records);
        Assert.Equal("SAP", roundtrip.SourceSystem);
        Assert.Equal("TRCH", roundtrip.Tsc);
        Assert.Equal("line-1", roundtrip.SourceLineId);
        Assert.Equal("MAT;1", roundtrip.Material);
        Assert.Equal("Artikel \"Audit\"", roundtrip.Name);
        Assert.Equal(1234.56m, roundtrip.SalesPriceValue);
        Assert.Equal("CHF", roundtrip.SalesCurrency);
        Assert.Equal(new DateTime(2026, 6, 10), roundtrip.PostingDate);
        Assert.Equal(new DateTime(2026, 6, 11), roundtrip.InvoiceDate);
    }

    [Fact]
    public async Task ReadLatestSiteAuditCsvRecordsAsync_Reads_New_Name_Before_Legacy_Name()
    {
        var service = new ExportAuditCsvService();
        var settings = new ExportSettings
        {
            AuditCsvEnabled = true,
            LocalSiteExportFolder = _tempDirectory
        };
        var site = new Site { TSC = "TRSE", Land = "Spanien" };

        var legacyPath = await service.WriteSiteAuditCsvAsync(
            site,
            settings,
            "MANUAL_EXCEL",
            _tempDirectory,
            [
                new SalesRecord
                {
                    SourceSystem = "MANUAL_EXCEL",
                    ExtractionDate = new DateTime(2026, 6, 10),
                    Tsc = "TRSE",
                    Land = "Spanien",
                    InvoiceNumber = "NEW",
                    SalesPriceValue = 20m
                }
            ]);
        var oldPath = Path.Combine(_tempDirectory, "Sales_TRSE_2026-06-10.csv");
        File.Move(legacyPath!, oldPath);
        File.SetLastWriteTimeUtc(oldPath, new DateTime(2026, 6, 10, 8, 0, 0, DateTimeKind.Utc));

        var newPath = await service.WriteSiteAuditCsvAsync(
            site,
            settings,
            "MANUAL_EXCEL",
            _tempDirectory,
            [
                new SalesRecord
                {
                    SourceSystem = "MANUAL_EXCEL",
                    ExtractionDate = new DateTime(2026, 6, 11),
                    Tsc = "TRSE",
                    Land = "Spanien",
                    InvoiceNumber = "PROCESSED",
                    SalesPriceValue = 30m
                }
            ]);
        File.SetLastWriteTimeUtc(newPath!, new DateTime(2026, 6, 11, 8, 0, 0, DateTimeKind.Utc));

        var records = await service.ReadLatestSiteAuditCsvRecordsAsync(settings);

        var record = Assert.Single(records);
        Assert.Equal("PROCESSED", record.InvoiceNumber);
        Assert.Equal(30m, record.SalesPriceValue);
    }

    [Fact]
    public async Task WriteConsolidatedAuditCsvAsync_Writes_All_File_Without_Becoming_Central_Input()
    {
        var service = new ExportAuditCsvService();
        var settings = new ExportSettings
        {
            AuditCsvEnabled = true,
            LocalSiteExportFolder = _tempDirectory,
            LocalConsolidatedExportFolder = _tempDirectory
        };

        var sitePath = await service.WriteSiteAuditCsvAsync(
            new Site { TSC = "TRCH", Land = "Schweiz" },
            settings,
            "SAP",
            _tempDirectory,
            [
                new SalesRecord
                {
                    SourceSystem = "SAP",
                    Tsc = "TRCH",
                    Land = "Schweiz",
                    InvoiceNumber = "CH-1",
                    ProductDivisionCode = "0005",
                    ProductDivisionText = "Transmitters"
                }
            ]);
        var allPath = await service.WriteConsolidatedAuditCsvAsync(
            settings,
            new DateTime(2026, 6, 17),
            _tempDirectory,
            [
                new SalesRecord
                {
                    SourceSystem = "SAP",
                    Tsc = "TRCH",
                    Land = "Schweiz",
                    InvoiceNumber = "CH-1",
                    ProductDivisionCode = "0005",
                    ProductDivisionText = "Transmitters"
                },
                new SalesRecord
                {
                    SourceSystem = "SAP",
                    Tsc = "TRUS",
                    Land = "USA",
                    InvoiceNumber = "US-1",
                    ProductDivisionCode = "0008",
                    ProductDivisionText = "Others"
                }
            ]);

        Assert.True(File.Exists(sitePath));
        Assert.True(File.Exists(allPath));
        Assert.Equal("Finance_Dashboard_Audit_All_2026-06-17.csv", Path.GetFileName(allPath));
        var csv = await File.ReadAllTextAsync(allPath!);
        Assert.Contains("ProductDivisionCode", csv);
        Assert.Contains("0008", csv);

        var centralInputRecords = await service.ReadLatestSiteAuditCsvRecordsAsync(settings);

        var record = Assert.Single(centralInputRecords);
        Assert.Equal("CH-1", record.InvoiceNumber);
    }

    [Fact]
    public async Task CentralSalesDataProvider_Uses_AuditCsv_When_Configured()
    {
        var csvService = new ExportAuditCsvService();
        await csvService.WriteSiteAuditCsvAsync(
            new Site { TSC = "TRUK", Land = "England" },
            new ExportSettings
            {
                AuditCsvEnabled = true,
                LocalSiteExportFolder = _tempDirectory,
                LocalAuditCsvFolder = Path.Combine(_tempDirectory, "ignored")
            },
            "MANUAL_EXCEL",
            _tempDirectory,
            [
                new SalesRecord
                {
                    SourceSystem = "MANUAL_EXCEL",
                    ExtractionDate = new DateTime(2026, 6, 11),
                    Tsc = "TRUK",
                    Land = "England",
                    InvoiceNumber = "UK-1",
                    SalesPriceValue = 10m,
                    SalesCurrency = "GBP",
                    InvoiceDate = new DateTime(2026, 1, 1)
                }
            ]);

        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        await using (var db = new AppDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
            db.ExportSettings.Add(new ExportSettings
            {
                UseAuditCsvAsCentralSource = true,
                LocalSiteExportFolder = _tempDirectory,
                LocalAuditCsvFolder = Path.Combine(_tempDirectory, "ignored")
            });
            db.Sites.Add(new Site
            {
                Id = 1,
                Schema = "DB",
                TSC = "TRDB",
                Land = "DB",
                SourceSystem = "DB",
                IsActive = true
            });
            db.CentralSalesRecords.Add(new CentralSalesRecord
            {
                StoredAtUtc = DateTime.UtcNow,
                SiteId = 1,
                SourceSystem = "DB",
                ExtractionDate = new DateTime(2026, 6, 11),
                Tsc = "TRDB",
                InvoiceNumber = "DB-1",
                Land = "DB",
                DocumentType = "INV"
            });
            await db.SaveChangesAsync();
        }

        var dbFactory = new TestDbContextFactory(options);
        var centralService = new CentralSalesRecordService(dbFactory, new NullAppEventLogService());
        var provider = new CentralSalesDataProvider(dbFactory, centralService, csvService);

        var records = await provider.GetRecordsAsync();

        var record = Assert.Single(records);
        Assert.Equal("TRUK", record.Tsc);
        Assert.Equal("UK-1", record.InvoiceNumber);
        Assert.Equal(10m, record.SalesPriceValue);
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
