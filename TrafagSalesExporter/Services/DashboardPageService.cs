using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface IDashboardPageService
{
    Task<DashboardPageState> LoadAsync();
}

public sealed class DashboardPageService : IDashboardPageService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public DashboardPageService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<DashboardPageState> LoadAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var sites = await db.Sites.Include(s => s.HanaServer).Where(s => s.IsActive).ToListAsync();
        var sourceSystems = await db.SourceSystemDefinitions.AsNoTracking().ToListAsync();
        var logs = await db.ExportLogs
            .GroupBy(l => l.SiteId)
            .Select(g => g.OrderByDescending(l => l.Timestamp).First())
            .ToListAsync();
        var appLogs = await db.AppEventLogs
            .Where(l => l.SiteId != null)
            .OrderByDescending(l => l.Timestamp)
            .Take(1000)
            .ToListAsync();
        var latestAppLogsBySite = appLogs
            .GroupBy(l => l.SiteId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Timestamp).First());

        var rows = sites.Select(s =>
        {
            var log = logs.FirstOrDefault(l => l.SiteId == s.Id);
            latestAppLogsBySite.TryGetValue(s.Id, out var appLog);
            var sourceSystem = sourceSystems.FirstOrDefault(x => string.Equals(x.Code, s.SourceSystem, StringComparison.OrdinalIgnoreCase));
            return new DashboardRow
            {
                SiteId = s.Id,
                Land = s.Land,
                TSC = s.TSC,
                Schema = s.Schema,
                ServerName = string.Equals(sourceSystem?.ConnectionKind, SourceSystemConnectionKinds.SapGateway, StringComparison.OrdinalIgnoreCase)
                    ? ResolveDashboardSapServiceUrl(s, sourceSystems)
                    : s.HanaServer?.Name ?? string.Empty,
                LastStatus = log?.Status ?? string.Empty,
                RowCount = log?.RowCount ?? 0,
                LastRun = log?.Timestamp,
                DurationSeconds = log?.DurationSeconds ?? 0,
                ErrorMessage = log?.ErrorMessage ?? string.Empty,
                FilePath = log?.FilePath ?? string.Empty,
                LiveMessage = appLog is null ? string.Empty : $"{appLog.Category}: {appLog.Message}",
                LiveDetails = appLog?.Details ?? string.Empty
            };
        }).ToList();

        return new DashboardPageState
        {
            DashboardRows = rows,
            ConsolidatedRows = BuildConsolidatedRows(await db.ExportSettings.FirstOrDefaultAsync() ?? new())
        };
    }

    private static string ResolveDashboardSapServiceUrl(Site site, List<SourceSystemDefinition> sourceSystems)
    {
        if (!string.IsNullOrWhiteSpace(site.SapServiceUrl))
            return site.SapServiceUrl;

        var sourceSystem = sourceSystems.FirstOrDefault(x => string.Equals(x.Code, site.SourceSystem, StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrWhiteSpace(sourceSystem?.CentralServiceUrl) ? "SAP Gateway" : sourceSystem.CentralServiceUrl;
    }

    private static List<ConsolidatedDashboardRow> BuildConsolidatedRows(ExportSettings settings)
    {
        var outputDirectory = ResolveConsolidatedOutputDirectory(settings);
        if (!Directory.Exists(outputDirectory))
            return [];

        return Directory.GetFiles(outputDirectory, "Sales_All_*.xlsx")
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTime)
            .Take(1)
            .Select(file => new ConsolidatedDashboardRow
            {
                Label = "Konsolidierter Export",
                FilePath = file.FullName,
                DisplayPath = file.FullName,
                LastModified = file.LastWriteTime
            })
            .ToList();
    }

    private static string ResolveConsolidatedOutputDirectory(ExportSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.LocalConsolidatedExportFolder))
            return settings.LocalConsolidatedExportFolder.Trim();

        if (!string.IsNullOrWhiteSpace(settings.LocalSiteExportFolder))
            return settings.LocalSiteExportFolder.Trim();

        return Path.Combine(AppContext.BaseDirectory, "output");
    }
}

public sealed class DashboardPageState
{
    public List<DashboardRow> DashboardRows { get; set; } = [];
    public List<ConsolidatedDashboardRow> ConsolidatedRows { get; set; } = [];
}

public sealed class DashboardRow
{
    public int SiteId { get; set; }
    public string Land { get; set; } = string.Empty;
    public string TSC { get; set; } = string.Empty;
    public string Schema { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public string LastStatus { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public DateTime? LastRun { get; set; }
    public double DurationSeconds { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string LiveMessage { get; set; } = string.Empty;
    public string LiveDetails { get; set; } = string.Empty;
    public bool HasOpenableFile => !string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath);
}

public sealed class ConsolidatedDashboardRow
{
    public string Label { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string DisplayPath { get; set; } = string.Empty;
    public DateTime? LastModified { get; set; }
    public bool HasOpenableFile => !string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath);
}
