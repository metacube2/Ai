using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public class ExportOrchestrationService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ISiteExportService _siteExportService;
    private readonly IConsolidatedExportService _consolidatedExportService;
    private readonly IExportLogService _exportLogService;

    public event Action? OnExportStatusChanged;

    private readonly Dictionary<int, string> _runningExports = new();
    private readonly object _lock = new();

    public ExportOrchestrationService(
        IDbContextFactory<AppDbContext> dbFactory,
        ISiteExportService siteExportService,
        IConsolidatedExportService consolidatedExportService,
        IExportLogService exportLogService)
    {
        _dbFactory = dbFactory;
        _siteExportService = siteExportService;
        _consolidatedExportService = consolidatedExportService;
        _exportLogService = exportLogService;
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

        await _consolidatedExportService.ExportAsync(consolidatedRecords);
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
        SiteExportResult? result = null;

        lock (_lock)
        {
            if (_runningExports.ContainsKey(site.Id)) return null;
            _runningExports[site.Id] = "HANA Abfrage...";
        }
        NotifyChanged();

        try
        {
            result = await _siteExportService.ExportAsync(site, status => UpdateStatus(site.Id, status));
            return result;
        }
        finally
        {
            if (result is not null)
            {
                await _exportLogService.WriteAsync(result.Log);
            }

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
}
