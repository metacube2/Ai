using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public class ExportOrchestrationService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly HanaQueryService _hanaService;
    private readonly ExcelExportService _excelService;
    private readonly SharePointUploadService _sharePointService;
    private readonly RecordTransformationService _transformationService;
    private readonly ILogger<ExportOrchestrationService> _logger;

    public event Action? OnExportStatusChanged;

    private readonly Dictionary<int, string> _runningExports = new();
    private readonly object _lock = new();

    public ExportOrchestrationService(
        IDbContextFactory<AppDbContext> dbFactory,
        HanaQueryService hanaService,
        ExcelExportService excelService,
        SharePointUploadService sharePointService,
        RecordTransformationService transformationService,
        ILogger<ExportOrchestrationService> logger)
    {
        _dbFactory = dbFactory;
        _hanaService = hanaService;
        _excelService = excelService;
        _sharePointService = sharePointService;
        _transformationService = transformationService;
        _logger = logger;
    }

    public bool IsExporting(int siteId)
    {
        lock (_lock)
        {
            return _runningExports.ContainsKey(siteId);
        }
    }

    public string GetExportStatus(int siteId)
    {
        lock (_lock)
        {
            return _runningExports.TryGetValue(siteId, out var status) ? status : string.Empty;
        }
    }

    public async Task ExportAllAsync()
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var sites = await db.Sites.Include(s => s.HanaServer).Where(s => s.IsActive).ToListAsync();
        var consolidatedRecords = new List<SalesRecord>();

        foreach (var site in sites)
        {
            var result = await ExportSiteAsync(site);
            if (result?.Records is { Count: > 0 })
                consolidatedRecords.AddRange(result.Records);
        }

        if (consolidatedRecords.Count > 0)
        {
            var spConfig = await db.SharePointConfigs.FirstOrDefaultAsync();
            var outputDir = Path.Combine(AppContext.BaseDirectory, "output");
            var consolidatedPath = _excelService.CreateConsolidatedExcelFile(
                outputDir,
                DateTime.UtcNow.Date,
                consolidatedRecords
                    .OrderBy(r => r.Land)
                    .ThenBy(r => r.Tsc)
                    .ThenByDescending(r => r.InvoiceDate ?? DateTime.MinValue)
                    .ThenBy(r => r.InvoiceNumber)
                    .ThenBy(r => r.PositionOnInvoice)
                    .ToList());

            if (spConfig is not null &&
                !string.IsNullOrWhiteSpace(spConfig.TenantId) &&
                !string.IsNullOrWhiteSpace(spConfig.ClientId) &&
                !string.IsNullOrWhiteSpace(spConfig.ClientSecret))
            {
                await _sharePointService.UploadAsync(
                    spConfig.TenantId, spConfig.ClientId, spConfig.ClientSecret,
                    spConfig.SiteUrl, spConfig.ExportFolder, "Alle", consolidatedPath);
            }
        }
    }

    public async Task ExportSiteByIdAsync(int siteId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var site = await db.Sites.Include(s => s.HanaServer).FirstOrDefaultAsync(s => s.Id == siteId);
        if (site is null) return;
        await ExportSiteAsync(site);
    }

    private async Task<SiteExportResult?> ExportSiteAsync(Site site)
    {
        if (site.HanaServer is null) return null;

        lock (_lock)
        {
            if (_runningExports.ContainsKey(site.Id)) return null;
            _runningExports[site.Id] = "HANA Abfrage...";
        }
        NotifyChanged();

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
            using var db = await _dbFactory.CreateDbContextAsync();
            var settings = await db.ExportSettings.FirstOrDefaultAsync() ?? new ExportSettings();
            var spConfig = await db.SharePointConfigs.FirstOrDefaultAsync();

            UpdateStatus(site.Id, "HANA Abfrage...");
            var records = await Task.Run(() => _hanaService.GetSalesRecords(
                site.HanaServer, site.Schema, site.TSC, site.Land, settings.DateFilter));

            UpdateStatus(site.Id, "Transformationen anwenden...");
            var rules = await db.FieldTransformationRules
                .Where(r => r.IsActive && r.SourceSystem == (string.IsNullOrWhiteSpace(site.SourceSystem) ? "SAP" : site.SourceSystem))
                .OrderBy(r => r.SortOrder)
                .ToListAsync();
            _transformationService.Apply(records, rules);

            UpdateStatus(site.Id, "Excel erstellen...");
            var outputDir = Path.Combine(AppContext.BaseDirectory, "output");
            var filePath = _excelService.CreateExcelFile(outputDir, site.TSC, DateTime.UtcNow.Date, records);
            var fileName = Path.GetFileName(filePath);

            if (spConfig is not null &&
                !string.IsNullOrWhiteSpace(spConfig.TenantId) &&
                !string.IsNullOrWhiteSpace(spConfig.ClientId) &&
                !string.IsNullOrWhiteSpace(spConfig.ClientSecret))
            {
                UpdateStatus(site.Id, "SharePoint Upload...");
                await _sharePointService.UploadAsync(
                    spConfig.TenantId, spConfig.ClientId, spConfig.ClientSecret,
                    spConfig.SiteUrl, spConfig.ExportFolder, site.Land, filePath);
            }

            sw.Stop();
            log.Status = "OK";
            log.RowCount = records.Count;
            log.FileName = fileName;
            log.DurationSeconds = sw.Elapsed.TotalSeconds;

            _logger.LogInformation("Export OK: {Land} ({TSC}) - {Rows} Zeilen in {Duration:F1}s",
                site.Land, site.TSC, records.Count, sw.Elapsed.TotalSeconds);

            return new SiteExportResult(records, filePath);
        }
        catch (Exception ex)
        {
            sw.Stop();
            log.Status = "Error";
            log.ErrorMessage = ex.Message;
            log.FileName = string.Empty;
            log.DurationSeconds = sw.Elapsed.TotalSeconds;

            _logger.LogError(ex, "Export Fehler: {Land} ({TSC})", site.Land, site.TSC);
            return null;
        }
        finally
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            db.ExportLogs.Add(log);
            await db.SaveChangesAsync();

            lock (_lock)
            {
                _runningExports.Remove(site.Id);
            }
            NotifyChanged();
        }
    }

    private void UpdateStatus(int siteId, string status)
    {
        lock (_lock)
        {
            _runningExports[siteId] = status;
        }
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        OnExportStatusChanged?.Invoke();
    }

    private sealed record SiteExportResult(List<SalesRecord> Records, string FilePath);
}
