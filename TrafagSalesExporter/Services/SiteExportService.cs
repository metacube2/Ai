using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services.DataSources;

namespace TrafagSalesExporter.Services;

public class SiteExportService : ISiteExportService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IDataSourceAdapterResolver _dataSourceResolver;
    private readonly IExcelExportService _excelService;
    private readonly ISharePointUploadService _sharePointService;
    private readonly IRecordTransformationService _transformationService;
    private readonly ICentralSalesRecordService _centralSalesRecordService;
    private readonly IAppEventLogService _appEventLogService;
    private readonly ILogger<SiteExportService> _logger;

    public SiteExportService(
        IDbContextFactory<AppDbContext> dbFactory,
        IDataSourceAdapterResolver dataSourceResolver,
        IExcelExportService excelService,
        ISharePointUploadService sharePointService,
        IRecordTransformationService transformationService,
        ICentralSalesRecordService centralSalesRecordService,
        IAppEventLogService appEventLogService,
        ILogger<SiteExportService> logger)
    {
        _dbFactory = dbFactory;
        _dataSourceResolver = dataSourceResolver;
        _excelService = excelService;
        _sharePointService = sharePointService;
        _transformationService = transformationService;
        _centralSalesRecordService = centralSalesRecordService;
        _appEventLogService = appEventLogService;
        _logger = logger;
    }

    public async Task<SiteExportResult> ExportAsync(Site site, Action<string>? updateStatus = null)
    {
        var sw = Stopwatch.StartNew();
        var log = new ExportLog
        {
            Timestamp = DateTime.Now,
            SiteId = site.Id,
            Land = site.Land,
            TSC = site.TSC
        };

        try
        {
            var sourceSystem = NormalizeSourceSystem(site.SourceSystem);
            await _appEventLogService.WriteAsync("Export", "Export gestartet",
                siteId: site.Id, land: site.Land,
                details: $"Quelle={sourceSystem} | TSC={site.TSC}");

            var (settings, spConfig, sourceDefinition, rules) = await LoadExportConfigAsync(site, sourceSystem);
            var outputDir = ResolveSiteOutputDirectory(settings, site);

            var adapter = _dataSourceResolver.Resolve(sourceDefinition.ConnectionKind);
            var fetchResult = await adapter.FetchAsync(new DataSourceFetchContext
            {
                Site = site,
                SourceDefinition = sourceDefinition,
                Settings = settings,
                SharePointConfig = spConfig,
                UpdateStatus = updateStatus
            });

            var records = fetchResult.Records;

            updateStatus?.Invoke("Transformationen anwenden...");
            await _appEventLogService.WriteAsync("Export", "Transformationen anwenden",
                siteId: site.Id, land: site.Land,
                details: $"Records vor Transformation={records.Count}");
            _transformationService.Apply(records, rules);

            var filePath = fetchResult.ReferenceFilePath;
            if (string.IsNullOrWhiteSpace(filePath))
            {
                updateStatus?.Invoke("Excel erstellen...");
                await _appEventLogService.WriteAsync("Export", "Excel erstellen",
                    siteId: site.Id, land: site.Land,
                    details: $"Records={records.Count}");
                filePath = _excelService.CreateExcelFile(outputDir, site.TSC, DateTime.UtcNow.Date, records);
            }

            log.RowCount = records.Count;

            updateStatus?.Invoke("Zentrale Tabelle aktualisieren...");
            await _appEventLogService.WriteAsync("Export", "Zentrale Tabelle aktualisieren",
                siteId: site.Id, land: site.Land,
                details: $"Records={records.Count}");
            await _centralSalesRecordService.ReplaceForSiteAsync(site, records, updateStatus);

            await UploadToSharePointIfConfiguredAsync(site, spConfig, filePath, updateStatus);

            sw.Stop();
            log.Status = "OK";
            log.FileName = Path.GetFileName(filePath);
            log.FilePath = filePath;
            log.DurationSeconds = sw.Elapsed.TotalSeconds;

            _logger.LogInformation("Export OK: {Land} ({TSC}) - {Rows} Zeilen in {Duration:F1}s",
                site.Land, site.TSC, log.RowCount, sw.Elapsed.TotalSeconds);
            await _appEventLogService.WriteAsync("Export", "Export erfolgreich",
                siteId: site.Id, land: site.Land,
                details: $"Rows={log.RowCount} | Datei={log.FileName} | Pfad={filePath} | Dauer={sw.Elapsed.TotalSeconds:F1}s");

            return new SiteExportResult
            {
                Records = records,
                Log = log,
                FilePath = filePath
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            log.Status = "Error";
            log.ErrorMessage = ex.Message;
            log.FileName = string.Empty;
            log.FilePath = string.Empty;
            log.DurationSeconds = sw.Elapsed.TotalSeconds;

            _logger.LogError(ex, "Export Fehler: {Land} ({TSC})", site.Land, site.TSC);
            await _appEventLogService.WriteAsync("Export", "Export fehlgeschlagen", "Error",
                siteId: site.Id, land: site.Land, details: ex.ToString());

            return new SiteExportResult
            {
                Records = [],
                Log = log,
                FilePath = null
            };
        }
    }

    private async Task<(ExportSettings settings, SharePointConfig? spConfig, SourceSystemDefinition sourceDefinition, List<FieldTransformationRule> rules)>
        LoadExportConfigAsync(Site site, string sourceSystem)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var settings = await db.ExportSettings.FirstOrDefaultAsync() ?? new ExportSettings();
        var spConfig = await db.SharePointConfigs.FirstOrDefaultAsync();
        var sourceDefinition = await db.SourceSystemDefinitions
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(x => x.Code == sourceSystem)
            ?? throw new InvalidOperationException($"Quellsystem '{sourceSystem}' ist nicht konfiguriert.");
        var rules = await db.FieldTransformationRules
            .Where(r => r.IsActive && r.SourceSystem == sourceSystem)
            .OrderBy(r => r.SortOrder)
            .ToListAsync();
        return (settings, spConfig, sourceDefinition, rules);
    }

    private async Task UploadToSharePointIfConfiguredAsync(
        Site site, SharePointConfig? spConfig, string filePath, Action<string>? updateStatus)
    {
        if (spConfig is null ||
            string.IsNullOrWhiteSpace(spConfig.TenantId) ||
            string.IsNullOrWhiteSpace(spConfig.ClientId) ||
            string.IsNullOrWhiteSpace(spConfig.ClientSecret))
            return;

        updateStatus?.Invoke("SharePoint Upload...");
        await _appEventLogService.WriteAsync("Export", "SharePoint Upload gestartet",
            siteId: site.Id, land: site.Land,
            details: $"{spConfig.SiteUrl} | {spConfig.ExportFolder}");
        await _sharePointService.UploadAsync(
            spConfig.TenantId, spConfig.ClientId, spConfig.ClientSecret,
            spConfig.SiteUrl, spConfig.ExportFolder, site.Land, filePath);
    }

    private static string NormalizeSourceSystem(string? sourceSystem)
        => string.IsNullOrWhiteSpace(sourceSystem) ? "SAP" : sourceSystem.Trim().ToUpperInvariant();

    private static string ResolveSiteOutputDirectory(ExportSettings settings, Site site)
    {
        var configured = DataSourceCredentials.FirstNonEmpty(
            site.LocalExportFolderOverride, settings.LocalSiteExportFolder);
        return string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(AppContext.BaseDirectory, "output")
            : configured;
    }
}
