using Microsoft.EntityFrameworkCore;
using System.Data;
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
    private readonly ISharePointUploadService _sharePointService;

    public DashboardPageService(
        IDbContextFactory<AppDbContext> dbFactory,
        ISharePointUploadService sharePointService)
    {
        _dbFactory = dbFactory;
        _sharePointService = sharePointService;
    }

    public async Task<DashboardPageState> LoadAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var sites = await db.Sites.Include(s => s.HanaServer).Where(s => s.IsActive).ToListAsync();
        var sourceSystems = await db.SourceSystemDefinitions.AsNoTracking().ToListAsync();
        var settings = await db.ExportSettings.FirstOrDefaultAsync() ?? new();
        var sharePointConfig = await db.SharePointConfigs.FirstOrDefaultAsync();
        var logs = await db.ExportLogs
            .GroupBy(l => l.SiteId)
            .Select(g => g.OrderByDescending(l => l.Timestamp).First())
            .ToListAsync();
        var centralDataStates = await db.CentralSalesRecords
            .AsNoTracking()
            .GroupBy(r => r.SiteId)
            .Select(g => new CentralDataState
            {
                SiteId = g.Key,
                RowCount = g.Count(),
                LatestStoredAtUtc = g.Max(r => r.StoredAtUtc)
            })
            .ToListAsync();
        var latestAppLogsBySite = await LoadLatestAppLogsBySiteAsync(db);
        var latestCsvBySite = await LoadLatestProcessedMergeInputFilesAsync(sites, settings, sharePointConfig);

        var rows = sites.Select(s =>
        {
            var log = logs.FirstOrDefault(l => l.SiteId == s.Id);
            var centralDataState = centralDataStates.FirstOrDefault(x => x.SiteId == s.Id);
            latestCsvBySite.TryGetValue(s.Id, out var latestCsv);
            var dataFreshness = ResolveDataFreshness(centralDataState, latestCsv, log);
            latestAppLogsBySite.TryGetValue(s.Id, out var appLog);
            var sourceSystem = sourceSystems.FirstOrDefault(x => string.Equals(x.Code, s.SourceSystem, StringComparison.OrdinalIgnoreCase));
            return new DashboardRow
            {
                SiteId = s.Id,
                Land = s.Land,
                DataBasis = ResolveDataBasis(s, sourceSystem),
                TSC = s.TSC,
                Schema = s.Schema,
                ServerName = string.Equals(sourceSystem?.ConnectionKind, SourceSystemConnectionKinds.SapGateway, StringComparison.OrdinalIgnoreCase)
                    ? ResolveDashboardSapServiceUrl(s, sourceSystems)
                    : s.HanaServer?.Name ?? string.Empty,
                LastStatus = log?.Status ?? string.Empty,
                RowCount = centralDataState?.RowCount ?? log?.RowCount ?? 0,
                LastRun = dataFreshness.DisplayAt,
                DataFreshnessSource = dataFreshness.Source,
                DataFreshnessDetails = dataFreshness.Details,
                IsCsvNewerThanDatabase = dataFreshness.IsCsvNewerThanDatabase,
                DurationSeconds = log?.DurationSeconds ?? 0,
                ErrorMessage = log?.ErrorMessage ?? string.Empty,
                FilePath = log?.FilePath ?? string.Empty,
                LiveMessage = appLog is null ? string.Empty : $"{appLog.Category}: {appLog.Message}",
                LiveDetails = appLog?.Details ?? string.Empty
            };
        }).ToList();

        var consolidatedRows = BuildConsolidatedRows(settings, sharePointConfig);
        var latestSuccessfulSiteRun = logs
            .Where(log => log.Status == "OK")
            .Select(log => (DateTime?)log.Timestamp)
            .OrderByDescending(timestamp => timestamp)
            .FirstOrDefault();
        var latestConsolidatedRun = consolidatedRows
            .Select(row => row.LastModified)
            .OrderByDescending(timestamp => timestamp)
            .FirstOrDefault();

        return new DashboardPageState
        {
            DashboardRows = rows,
            ConsolidatedRows = consolidatedRows,
            ReadinessWarnings = BuildReadinessWarnings(sites, sourceSystems),
            IsConsolidatedStale = latestSuccessfulSiteRun.HasValue &&
                (!latestConsolidatedRun.HasValue || latestSuccessfulSiteRun.Value > latestConsolidatedRun.Value),
            LatestSuccessfulSiteRun = latestSuccessfulSiteRun,
            LatestConsolidatedRun = latestConsolidatedRun
        };
    }

    private async Task<Dictionary<int, ProcessedMergeInputState>> LoadLatestProcessedMergeInputFilesAsync(
        IReadOnlyCollection<Site> sites,
        ExportSettings settings,
        SharePointConfig? sharePointConfig)
    {
        var result = new Dictionary<int, ProcessedMergeInputState>();

        foreach (var site in sites)
        {
            var local = ResolveLatestLocalProcessedMergeInputFile(site, settings);
            if (local is not null)
                result[site.Id] = local;
        }

        if (!HasCompleteSharePointConfig(sharePointConfig))
            return result;

        var tasks = sites.Select(site => LoadLatestSharePointProcessedMergeInputFileAsync(site, sharePointConfig!)).ToList();
        var remoteStates = await Task.WhenAll(tasks);
        foreach (var remote in remoteStates.Where(x => x is not null).Select(x => x!))
        {
            if (!result.TryGetValue(remote.SiteId, out var existing) || remote.TimestampUtc > existing.TimestampUtc)
                result[remote.SiteId] = remote;
        }

        return result;
    }

    private async Task<ProcessedMergeInputState?> LoadLatestSharePointProcessedMergeInputFileAsync(
        Site site,
        SharePointConfig sharePointConfig)
    {
        ProcessedMergeInputState? latestState = null;
        foreach (var folder in ResolveSharePointProcessedMergeInputFolders(site, sharePointConfig))
        {
            try
            {
                var file = await _sharePointService.ResolveLatestProcessedMergeInputFileAsync(
                    sharePointConfig.TenantId,
                    sharePointConfig.ClientId,
                    sharePointConfig.ClientSecret,
                    sharePointConfig.SiteUrl,
                    folder,
                    site.TSC);

                if (file?.LastModifiedUtc is null)
                    continue;

                var state = new ProcessedMergeInputState
                {
                    SiteId = site.Id,
                    Source = "SharePoint-CSV",
                    Path = file.FileReference,
                    TimestampUtc = file.LastModifiedUtc.Value.UtcDateTime,
                    DisplayAt = file.LastModifiedUtc.Value.LocalDateTime
                };
                if (latestState is null || state.TimestampUtc > latestState.TimestampUtc)
                    latestState = state;
            }
            catch
            {
                // Dashboard status must not fail only because a SharePoint status probe is unavailable.
            }
        }

        return latestState;
    }

    private static ProcessedMergeInputState? ResolveLatestLocalProcessedMergeInputFile(Site site, ExportSettings settings)
    {
        var candidates = ResolveLocalProcessedMergeInputDirectories(site, settings)
            .Where(Directory.Exists)
            .SelectMany(directory => Directory.EnumerateFiles(directory, "Sales_ProcessedMergeInput_*.csv", SearchOption.TopDirectoryOnly))
            .Where(path => IsProcessedMergeInputForTsc(path, site.TSC))
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .ThenByDescending(file => file.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        return candidates is null
            ? null
            : new ProcessedMergeInputState
            {
                SiteId = site.Id,
                Source = "Lokale CSV",
                Path = candidates.FullName,
                TimestampUtc = candidates.LastWriteTimeUtc,
                DisplayAt = candidates.LastWriteTime
            };
    }

    private static IEnumerable<string> ResolveLocalProcessedMergeInputDirectories(Site site, ExportSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(site.LocalExportFolderOverride))
        {
            yield return site.LocalExportFolderOverride.Trim();
            yield return Path.Combine(site.LocalExportFolderOverride.Trim(), site.Land);
        }

        if (!string.IsNullOrWhiteSpace(settings.LocalSiteExportFolder))
        {
            yield return settings.LocalSiteExportFolder.Trim();
            yield return Path.Combine(settings.LocalSiteExportFolder.Trim(), site.Land);
        }

        var output = Path.Combine(AppContext.BaseDirectory, "output");
        yield return output;
        yield return Path.Combine(output, site.Land);
    }

    private static IEnumerable<string> ResolveSharePointProcessedMergeInputFolders(Site site, SharePointConfig sharePointConfig)
    {
        if (LooksLikeSharePointReference(site.ManualImportFilePath))
            yield return site.ManualImportFilePath.Trim();

        if (!string.IsNullOrWhiteSpace(sharePointConfig.ExportFolder))
            yield return string.Join("/", sharePointConfig.ExportFolder.Trim('/'), site.Land.Trim('/')).Trim('/');
    }

    private static DataFreshnessState ResolveDataFreshness(
        CentralDataState? central,
        ProcessedMergeInputState? csv,
        ExportLog? log)
    {
        var candidates = new List<DataFreshnessCandidate>();
        if (central is not null)
        {
            var utc = EnsureUtc(central.LatestStoredAtUtc);
            candidates.Add(new DataFreshnessCandidate(
                utc,
                utc.ToLocalTime(),
                "DB",
                $"CentralSalesRecords | Zeilen={central.RowCount:N0}",
                false));
        }

        if (csv is not null)
        {
            candidates.Add(new DataFreshnessCandidate(
                EnsureUtc(csv.TimestampUtc),
                csv.DisplayAt,
                csv.Source,
                $"{csv.Source}: {csv.Path}",
                true));
        }

        if (log is not null)
        {
            var utc = AssumeLocal(log.Timestamp).ToUniversalTime();
            candidates.Add(new DataFreshnessCandidate(
                utc,
                log.Timestamp,
                "Export-Log",
                $"ExportLogs | Status={log.Status}",
                false));
        }

        var selected = candidates
            .OrderByDescending(candidate => candidate.TimestampUtc)
            .FirstOrDefault();

        var centralUtc = central is null ? (DateTime?)null : EnsureUtc(central.LatestStoredAtUtc);
        var csvIsNewerThanDatabase = csv is not null &&
            (centralUtc is null || EnsureUtc(csv.TimestampUtc) > centralUtc.Value.AddSeconds(1));

        return selected is null
            ? new DataFreshnessState(null, string.Empty, string.Empty, false)
            : new DataFreshnessState(selected.DisplayAt, selected.Source, selected.Details, csvIsNewerThanDatabase);
    }

    private static bool HasCompleteSharePointConfig(SharePointConfig? config)
        => config is not null &&
           !string.IsNullOrWhiteSpace(config.TenantId) &&
           !string.IsNullOrWhiteSpace(config.ClientId) &&
           !string.IsNullOrWhiteSpace(config.ClientSecret) &&
           !string.IsNullOrWhiteSpace(config.SiteUrl);

    private static bool IsProcessedMergeInputForTsc(string path, string tsc)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        const string prefix = "Sales_ProcessedMergeInput_";
        if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var suffix = name[prefix.Length..];
        var lastUnderscore = suffix.LastIndexOf('_');
        var fileTsc = lastUnderscore <= 0 ? suffix : suffix[..lastUnderscore];
        return string.Equals(fileTsc, tsc, StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeSharePointReference(string path)
        => path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
           path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
           path.Contains("sharepoint.com", StringComparison.OrdinalIgnoreCase);

    private static DateTime EnsureUtc(DateTime value)
        => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    private static DateTime AssumeLocal(DateTime value)
        => value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Local)
            : value.ToLocalTime();

    private static async Task<Dictionary<int, AppEventLog>> LoadLatestAppLogsBySiteAsync(AppDbContext db)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
            await connection.OpenAsync();

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
SELECT Id, Timestamp, Level, Category, SiteId, Land, Message, Details
FROM AppEventLogs
WHERE SiteId IS NOT NULL
ORDER BY Id DESC
LIMIT 1000;
""";

            var logs = new List<AppEventLog>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (!TryReadInt(reader["SiteId"], out var siteId))
                    continue;

                if (!DateTime.TryParse(Convert.ToString(reader["Timestamp"]), out var timestamp))
                    continue;

                logs.Add(new AppEventLog
                {
                    Id = TryReadInt(reader["Id"], out var id) ? id : 0,
                    Timestamp = timestamp,
                    Level = Convert.ToString(reader["Level"]) ?? string.Empty,
                    Category = Convert.ToString(reader["Category"]) ?? string.Empty,
                    SiteId = siteId,
                    Land = Convert.ToString(reader["Land"]) ?? string.Empty,
                    Message = Convert.ToString(reader["Message"]) ?? string.Empty,
                    Details = Convert.ToString(reader["Details"]) ?? string.Empty
                });
            }

            return logs
                .GroupBy(l => l.SiteId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Timestamp).First());
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }

    private static bool TryReadInt(object? value, out int number)
    {
        if (value is int intValue)
        {
            number = intValue;
            return true;
        }

        if (value is long longValue && longValue >= int.MinValue && longValue <= int.MaxValue)
        {
            number = (int)longValue;
            return true;
        }

        return int.TryParse(Convert.ToString(value), out number);
    }

    private static List<string> BuildReadinessWarnings(List<Site> activeSites, List<SourceSystemDefinition> sourceSystems)
    {
        var warnings = new List<string>();
        foreach (var site in activeSites.OrderBy(x => x.Land).ThenBy(x => x.TSC))
        {
            var sourceSystem = sourceSystems.FirstOrDefault(x => string.Equals(x.Code, site.SourceSystem, StringComparison.OrdinalIgnoreCase));
            if (!string.Equals(sourceSystem?.ConnectionKind, SourceSystemConnectionKinds.ManualExcel, StringComparison.OrdinalIgnoreCase))
                continue;

            if (string.IsNullOrWhiteSpace(site.ManualImportFilePath))
                warnings.Add($"{site.Land} / {site.TSC}: manuelle Excel-/CSV-Datei fehlt.");
        }

        return warnings;
    }

    private static string ResolveDashboardSapServiceUrl(Site site, List<SourceSystemDefinition> sourceSystems)
    {
        if (!string.IsNullOrWhiteSpace(site.SapServiceUrl))
            return site.SapServiceUrl;

        var sourceSystem = sourceSystems.FirstOrDefault(x => string.Equals(x.Code, site.SourceSystem, StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrWhiteSpace(sourceSystem?.CentralServiceUrl) ? "SAP Gateway" : sourceSystem.CentralServiceUrl;
    }

    private static string ResolveDataBasis(Site site, SourceSystemDefinition? sourceSystem)
    {
        if (string.Equals(sourceSystem?.ConnectionKind, SourceSystemConnectionKinds.ManualExcel, StringComparison.OrdinalIgnoreCase))
        {
            var path = site.ManualImportFilePath ?? string.Empty;
            var extension = Path.GetExtension(path).TrimStart('.').ToUpperInvariant();

            if (extension is "CSV")
                return "CSV-Datei";
            if (extension is "XLS" or "XLSX" or "XLSM")
                return "Excel-Datei";
            if (!string.IsNullOrWhiteSpace(path))
                return "Excel/CSV-Datei";

            return "Manuelle Datei";
        }

        if (string.Equals(sourceSystem?.ConnectionKind, SourceSystemConnectionKinds.SapGateway, StringComparison.OrdinalIgnoreCase))
            return "SAP Service";

        if (string.Equals(sourceSystem?.ConnectionKind, SourceSystemConnectionKinds.Hana, StringComparison.OrdinalIgnoreCase))
            return "Server";

        return string.IsNullOrWhiteSpace(site.SourceSystem) ? "-" : site.SourceSystem;
    }

    private static List<ConsolidatedDashboardRow> BuildConsolidatedRows(ExportSettings settings, SharePointConfig? sharePointConfig)
    {
        var outputDirectory = ResolveConsolidatedOutputDirectory(settings);
        if (!Directory.Exists(outputDirectory))
            return [];

        var consolidated = Directory.GetFiles(outputDirectory, "Sales_All_*.xlsx")
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTime)
            .FirstOrDefault();
        var proof = Directory.GetFiles(outputDirectory, "Finance_Dashboard_Nachweis_*.xlsx")
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTime)
            .FirstOrDefault();

        return new[]
        {
            BuildConsolidatedRow("Konsolidierter Export", "Consolidated export", consolidated, sharePointConfig),
            BuildConsolidatedRow("Dashboard Nachweis", "Dashboard proof", proof, sharePointConfig)
        }
            .Where(row => row is not null)
            .Select(row => row!)
            .ToList();
    }

    private static ConsolidatedDashboardRow? BuildConsolidatedRow(
        string label,
        string labelEnglish,
        FileInfo? file,
        SharePointConfig? sharePointConfig)
        => file is null
            ? null
            : new ConsolidatedDashboardRow
            {
                Label = label,
                LabelEnglish = labelEnglish,
                FilePath = file.FullName,
                DisplayPath = ResolveConsolidatedDisplayPath(file, sharePointConfig),
                LastModified = file.LastWriteTime
            };

    private static string ResolveConsolidatedDisplayPath(FileInfo file, SharePointConfig? sharePointConfig)
    {
        if (!HasCompleteSharePointConfig(sharePointConfig))
            return file.FullName;

        var folder = ResolveCentralSharePointFolder(sharePointConfig!);
        var relativePath = string.Join("/", folder.Trim('/'), file.Name).Trim('/');
        var siteUrl = sharePointConfig!.SiteUrl.TrimEnd('/');
        return $"{siteUrl}/{relativePath}";
    }

    private static string ResolveConsolidatedOutputDirectory(ExportSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.LocalConsolidatedExportFolder))
            return settings.LocalConsolidatedExportFolder.Trim();

        if (!string.IsNullOrWhiteSpace(settings.LocalSiteExportFolder))
            return settings.LocalSiteExportFolder.Trim();

        return Path.Combine(AppContext.BaseDirectory, "output");
    }

    private static string ResolveCentralSharePointFolder(SharePointConfig config)
    {
        var configuredFolder = !string.IsNullOrWhiteSpace(config.CentralExportFolder)
            ? config.CentralExportFolder
            : IsLegacyExportFolder(config.ExportFolder)
                ? "/Import/Finance"
                : config.ExportFolder;

        var normalizedFolder = configuredFolder.Trim().TrimEnd('/', '\\');
        return normalizedFolder.EndsWith("/Alle", StringComparison.OrdinalIgnoreCase) ||
               normalizedFolder.EndsWith("\\Alle", StringComparison.OrdinalIgnoreCase)
            ? normalizedFolder.Replace('\\', '/')
            : string.Join("/", configuredFolder.Trim('/'), "Alle").Trim('/');
    }

    private static bool IsLegacyExportFolder(string folder)
    {
        var normalized = folder.Trim().TrimEnd('/', '\\');
        return normalized.Equals("/Shared Documents/Exports", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class DashboardPageState
{
    public List<DashboardRow> DashboardRows { get; set; } = [];
    public List<ConsolidatedDashboardRow> ConsolidatedRows { get; set; } = [];
    public List<string> ReadinessWarnings { get; set; } = [];
    public bool IsConsolidatedStale { get; set; }
    public DateTime? LatestSuccessfulSiteRun { get; set; }
    public DateTime? LatestConsolidatedRun { get; set; }
}

public sealed class DashboardRow
{
    public int SiteId { get; set; }
    public string Land { get; set; } = string.Empty;
    public string DataBasis { get; set; } = string.Empty;
    public string TSC { get; set; } = string.Empty;
    public string Schema { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public string LastStatus { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public DateTime? LastRun { get; set; }
    public string DataFreshnessSource { get; set; } = string.Empty;
    public string DataFreshnessDetails { get; set; } = string.Empty;
    public bool IsCsvNewerThanDatabase { get; set; }
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
    public string LabelEnglish { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string DisplayPath { get; set; } = string.Empty;
    public DateTime? LastModified { get; set; }
    public bool HasOpenableFile => !string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath);
}

internal sealed class CentralDataState
{
    public int SiteId { get; set; }
    public int RowCount { get; set; }
    public DateTime LatestStoredAtUtc { get; set; }
}

internal sealed class ProcessedMergeInputState
{
    public int SiteId { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
    public DateTime DisplayAt { get; set; }
}

internal sealed record DataFreshnessState(
    DateTime? DisplayAt,
    string Source,
    string Details,
    bool IsCsvNewerThanDatabase);

internal sealed record DataFreshnessCandidate(
    DateTime TimestampUtc,
    DateTime DisplayAt,
    string Source,
    string Details,
    bool IsCsv);
