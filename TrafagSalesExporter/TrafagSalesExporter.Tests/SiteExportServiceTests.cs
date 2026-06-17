using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;
using TrafagSalesExporter.Services.DataSources;

namespace TrafagSalesExporter.Tests;

public sealed class SiteExportServiceTests : IDisposable
{
    private readonly string _tempDirectory;

    public SiteExportServiceTests()
    {
        _tempDirectory = Path.Combine("C:\\TMP", $"trafag-site-export-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, recursive: true);
    }

    [Fact]
    public async Task ExportAsync_Uploads_AuditCsv_To_Same_SharePoint_Target_As_Excel()
    {
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
                AuditCsvEnabled = true,
                LocalSiteExportFolder = _tempDirectory
            });
            db.SharePointConfigs.Add(new SharePointConfig
            {
                TenantId = "tenant",
                ClientId = "client",
                ClientSecret = "secret",
                SiteUrl = "https://trafagag.sharepoint.com/sites/WorldwideBIPlatform",
                ExportFolder = "Import/Finance"
            });
            db.SourceSystemDefinitions.Add(new SourceSystemDefinition
            {
                Code = "MANUAL_EXCEL",
                DisplayName = "Manual Excel",
                ConnectionKind = SourceSystemConnectionKinds.ManualExcel,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        var sharePoint = new RecordingSharePointUploadService();
        var service = new SiteExportService(
            new TestDbContextFactory(options),
            new FixedDataSourceAdapterResolver(new FixedDataSourceAdapter(new DataSourceFetchResult
            {
                Records =
                [
                    new SalesRecord
                    {
                        SourceSystem = "MANUAL_EXCEL",
                        ExtractionDate = new DateTime(2026, 6, 11, 8, 0, 0, DateTimeKind.Utc),
                        Tsc = "TRSE",
                        Land = "Spanien",
                        InvoiceNumber = "ES-1",
                        SalesPriceValue = 100m,
                        SalesCurrency = "EUR",
                        InvoiceDate = new DateTime(2026, 6, 10),
                        DocumentType = "Invoice"
                    }
                ],
                SharePointUploadFolderOverride = "Import/Finance/Spanien",
                SharePointUploadLandOverride = string.Empty
            })),
            new FileWritingExcelExportService(),
            sharePoint,
            new NoopRecordTransformationService(),
            new NoopCentralSalesRecordService(),
            new ExportAuditCsvService(),
            new NoopAppEventLogService(),
            NullLogger<SiteExportService>.Instance);

        var result = await service.ExportAsync(new Site
        {
            Id = 7,
            TSC = "TRSE",
            Land = "Spanien",
            SourceSystem = "MANUAL_EXCEL",
            IsActive = true
        });

        Assert.NotNull(result.FilePath);
        Assert.True(File.Exists(result.FilePath));
        var auditCsv = Directory.GetFiles(_tempDirectory, "Sales_ProcessedMergeInput_TRSE_*.csv").Single();
        Assert.True(File.Exists(auditCsv));

        Assert.Equal(2, sharePoint.Uploads.Count);
        Assert.EndsWith(".xlsx", sharePoint.Uploads[0].FileName, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(".csv", sharePoint.Uploads[1].FileName, StringComparison.OrdinalIgnoreCase);
        Assert.All(sharePoint.Uploads, upload =>
        {
            Assert.Equal("Import/Finance/Spanien", upload.ExportFolder);
            Assert.Equal(string.Empty, upload.Land);
        });
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

    private sealed class FixedDataSourceAdapterResolver : IDataSourceAdapterResolver
    {
        private readonly IDataSourceAdapter _adapter;

        public FixedDataSourceAdapterResolver(IDataSourceAdapter adapter)
        {
            _adapter = adapter;
        }

        public IDataSourceAdapter Resolve(string connectionKind) => _adapter;
    }

    private sealed class FixedDataSourceAdapter : IDataSourceAdapter
    {
        private readonly DataSourceFetchResult _result;

        public FixedDataSourceAdapter(DataSourceFetchResult result)
        {
            _result = result;
        }

        public string ConnectionKind => SourceSystemConnectionKinds.ManualExcel;

        public Task<DataSourceFetchResult> FetchAsync(DataSourceFetchContext context)
            => Task.FromResult(_result);
    }

    private sealed class FileWritingExcelExportService : IExcelExportService
    {
        public string CreateExcelFile(string outputDirectory, string tsc, DateTime fileDate, List<SalesRecord> records)
        {
            Directory.CreateDirectory(outputDirectory);
            var path = Path.Combine(outputDirectory, $"Sales_{tsc}_{fileDate:yyyy-MM-dd}.xlsx");
            File.WriteAllText(path, "excel");
            return path;
        }

        public string CreateConsolidatedExcelFile(string outputDirectory, DateTime fileDate, List<SalesRecord> records)
            => throw new NotSupportedException();

        public string CreateDashboardProofExcelFile(string outputDirectory, DateTime fileDate, List<SalesRecord> records, bool useAuditCsvAsCentralSource)
            => throw new NotSupportedException();

        public string CreateGenericExcelFile(string outputDirectory, string filePrefix, DateTime fileDate, string worksheetName, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
            => throw new NotSupportedException();
    }

    private sealed class RecordingSharePointUploadService : ISharePointUploadService
    {
        public List<UploadCall> Uploads { get; } = [];

        public Task UploadAsync(string tenantId, string clientId, string clientSecret, string siteUrl, string exportFolder, string land, string localFilePath, bool uploadTimestampedCopyIfLocked = false)
        {
            Uploads.Add(new UploadCall(exportFolder, land, Path.GetFileName(localFilePath)));
            return Task.CompletedTask;
        }

        public Task<string> DownloadToTempFileAsync(string tenantId, string clientId, string clientSecret, string siteUrl, string fileReference)
            => throw new NotSupportedException();

        public Task<SharePointFileReference> ResolveLatestFileInFolderAsync(string tenantId, string clientId, string clientSecret, string siteUrl, string folderReference, string siteTsc, int? preferredYear = null)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<SharePointFileReference>> ResolveManualImportFilesInFolderAsync(string tenantId, string clientId, string clientSecret, string siteUrl, string folderReference, string siteTsc, int? preferredYear = null)
            => throw new NotSupportedException();

        public Task TestConnectionAsync(string tenantId, string clientId, string clientSecret, string siteUrl)
            => Task.CompletedTask;
    }

    private sealed record UploadCall(string ExportFolder, string Land, string FileName);

    private sealed class NoopRecordTransformationService : IRecordTransformationService
    {
        public void Apply(List<SalesRecord> records, IEnumerable<FieldTransformationRule> rules)
        {
        }
    }

    private sealed class NoopCentralSalesRecordService : ICentralSalesRecordService
    {
        public Task ReplaceForSiteAsync(Site site, IEnumerable<SalesRecord> records, Action<string>? updateStatus = null)
            => Task.CompletedTask;

        public Task<List<SalesRecord>> GetAllAsync()
            => Task.FromResult(new List<SalesRecord>());
    }

    private sealed class NoopAppEventLogService : IAppEventLogService
    {
        public Task WriteAsync(string category, string message, string level = "Info", int? siteId = null, string? land = null, string? details = null)
            => Task.CompletedTask;

        public Task WriteDebugAsync(string category, string message, int? siteId = null, string? land = null, string? details = null)
            => Task.CompletedTask;
    }
}
