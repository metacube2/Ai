using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public class ConsolidatedExportService : IConsolidatedExportService
{
    private const string FinanceImportRootFolder = "/Import/Finance";
    private const int MaxSingleProofWorkbookRows = 50000;
    private const int MaxPartitionedProofWorkbookRows = 25000;
    private static readonly TimeSpan SharePointProbeTimeout = TimeSpan.FromSeconds(25);
    private static readonly TimeSpan SharePointDownloadTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan SharePointUploadTimeout = TimeSpan.FromMinutes(5);

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ICentralSalesDataProvider _centralSalesDataProvider;
    private readonly IExcelExportService _excelService;
    private readonly IExportAuditCsvService _auditCsvService;
    private readonly ISharePointUploadService _sharePointService;
    private readonly IAppEventLogService _appEventLogService;

    public ConsolidatedExportService(
        IDbContextFactory<AppDbContext> dbFactory,
        ICentralSalesDataProvider centralSalesDataProvider,
        IExcelExportService excelService,
        IExportAuditCsvService auditCsvService,
        ISharePointUploadService sharePointService,
        IAppEventLogService appEventLogService)
    {
        _dbFactory = dbFactory;
        _centralSalesDataProvider = centralSalesDataProvider;
        _excelService = excelService;
        _auditCsvService = auditCsvService;
        _sharePointService = sharePointService;
        _appEventLogService = appEventLogService;
    }

    public async Task<string?> ExportAsync(Action<string>? updateStatus = null)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var spConfig = await db.SharePointConfigs.FirstOrDefaultAsync();
        var settings = await db.ExportSettings.FirstOrDefaultAsync() ?? new ExportSettings();
        var sites = await db.Sites
            .AsNoTracking()
            .Where(site => site.IsActive)
            .OrderBy(site => site.Land)
            .ThenBy(site => site.TSC)
            .ToListAsync();

        updateStatus?.Invoke("Neueste Laenderdateien pruefen...");
        await SyncLatestProcessedMergeInputFilesAsync(sites, settings, spConfig);

        updateStatus?.Invoke("Zentrale Daten aus DB/CSV zusammenstellen...");
        var consolidatedRecords = await _centralSalesDataProvider.GetLatestRecordsBySiteAsync();
        if (consolidatedRecords.Count == 0)
            return null;

        var outputDir = ResolveConsolidatedOutputDirectory(settings);
        var fileDate = DateTime.UtcNow.Date;
        var sortedRecords = consolidatedRecords
            .OrderBy(r => r.Land)
            .ThenBy(r => r.Tsc)
            .ThenByDescending(r => r.InvoiceDate ?? DateTime.MinValue)
            .ThenBy(r => r.InvoiceNumber)
            .ThenBy(r => r.PositionOnInvoice)
            .ToList();

        var hasSharePointUpload = HasCompleteSharePointConfig(spConfig);
        var uploadTarget = hasSharePointUpload
            ? ResolveCentralSharePointUploadTarget(spConfig!)
            : (Folder: string.Empty, LandSubfolder: string.Empty);

        updateStatus?.Invoke("Sales_All Excel erzeugen...");
        var consolidatedPath = _excelService.CreateConsolidatedExcelFile(
            outputDir,
            fileDate,
            sortedRecords);

        if (hasSharePointUpload)
        {
            updateStatus?.Invoke("Sales_All nach SharePoint laden...");
            await UploadCentralFileAsync(spConfig!, uploadTarget.Folder, uploadTarget.LandSubfolder, consolidatedPath);
        }

        updateStatus?.Invoke("Zentrale Audit-CSV erzeugen...");
        var auditCsvPath = await _auditCsvService.WriteConsolidatedAuditCsvAsync(
            settings,
            fileDate,
            outputDir,
            sortedRecords);

        if (hasSharePointUpload && !string.IsNullOrWhiteSpace(auditCsvPath))
        {
            updateStatus?.Invoke("Audit-CSV nach SharePoint laden...");
            await UploadCentralFileAsync(spConfig!, uploadTarget.Folder, uploadTarget.LandSubfolder, auditCsvPath);
        }

        if (sortedRecords.Count > MaxSingleProofWorkbookRows)
            await CreateAndUploadPartitionedProofWorkbooksAsync(
                outputDir,
                fileDate,
                sortedRecords,
                settings.UseAuditCsvAsCentralSource,
                spConfig,
                hasSharePointUpload,
                uploadTarget.Folder,
                uploadTarget.LandSubfolder,
                updateStatus);
        else
            await CreateAndUploadSingleProofWorkbookAsync(
                outputDir,
                fileDate,
                sortedRecords,
                settings.UseAuditCsvAsCentralSource,
                spConfig,
                hasSharePointUpload,
                uploadTarget.Folder,
                uploadTarget.LandSubfolder,
                updateStatus);

        return consolidatedPath;
    }

    private async Task CreateAndUploadSingleProofWorkbookAsync(
        string outputDir,
        DateTime fileDate,
        List<SalesRecord> sortedRecords,
        bool useAuditCsvAsCentralSource,
        SharePointConfig? spConfig,
        bool hasSharePointUpload,
        string sharePointFolder,
        string landSubfolder,
        Action<string>? updateStatus)
    {
        updateStatus?.Invoke("Nachweis-Excel erzeugen...");
        var proofPath = _excelService.CreateDashboardProofExcelFile(
            outputDir,
            fileDate,
            sortedRecords,
            useAuditCsvAsCentralSource);

        if (hasSharePointUpload)
        {
            updateStatus?.Invoke("Nachweis-Excel nach SharePoint laden...");
            await UploadCentralFileAsync(spConfig!, sharePointFolder, landSubfolder, proofPath);
        }
    }

    private async Task CreateAndUploadPartitionedProofWorkbooksAsync(
        string outputDir,
        DateTime fileDate,
        List<SalesRecord> sortedRecords,
        bool useAuditCsvAsCentralSource,
        SharePointConfig? spConfig,
        bool hasSharePointUpload,
        string sharePointFolder,
        string landSubfolder,
        Action<string>? updateStatus)
    {
        var partitions = BuildProofWorkbookPartitions(sortedRecords).ToList();
        await _appEventLogService.WriteAsync(
            "Export",
            "Zentrale Nachweis-Excel werden partitioniert erzeugt",
            details: $"Zeilen={sortedRecords.Count}; Dateien={partitions.Count}; MaxZeilenProDatei={MaxPartitionedProofWorkbookRows}.");

        for (var i = 0; i < partitions.Count; i++)
        {
            var partition = partitions[i];
            updateStatus?.Invoke($"Nachweis-Excel {i + 1}/{partitions.Count} erzeugen: {partition.Scope}...");
            var proofPath = _excelService.CreateDashboardProofExcelFile(
                outputDir,
                fileDate,
                partition.Records,
                useAuditCsvAsCentralSource,
                partition.Scope);

            if (hasSharePointUpload)
            {
                updateStatus?.Invoke($"Nachweis-Excel {i + 1}/{partitions.Count} nach SharePoint laden...");
                await UploadCentralFileAsync(spConfig!, sharePointFolder, landSubfolder, proofPath);
            }
        }
    }

    private async Task SyncLatestProcessedMergeInputFilesAsync(
        IReadOnlyCollection<Site> sites,
        ExportSettings settings,
        SharePointConfig? spConfig)
    {
        if (!HasCompleteSharePointConfig(spConfig))
            return;

        var auditDirectory = _auditCsvService.ResolveAuditCsvDirectory(settings);
        Directory.CreateDirectory(auditDirectory);

        foreach (var site in sites)
        {
            var latest = await ResolveLatestSharePointProcessedMergeInputFileAsync(site, spConfig!);
            if (latest?.LastModifiedUtc is null)
                continue;

            var fileName = ResolveFileName(latest.FileReference);
            if (string.IsNullOrWhiteSpace(fileName))
                continue;

            var targetPath = Path.Combine(auditDirectory, fileName);
            var remoteLastWriteUtc = latest.LastModifiedUtc.Value.UtcDateTime;
            if (File.Exists(targetPath) && File.GetLastWriteTimeUtc(targetPath) >= remoteLastWriteUtc.AddSeconds(-1))
                continue;

            var tempPath = await _sharePointService.DownloadToTempFileAsync(
                spConfig!.TenantId,
                spConfig.ClientId,
                spConfig.ClientSecret,
                spConfig.SiteUrl,
                latest.FileReference).WaitAsync(SharePointDownloadTimeout);

            try
            {
                File.Copy(tempPath, targetPath, overwrite: true);
                File.SetLastWriteTimeUtc(targetPath, remoteLastWriteUtc);
                await _appEventLogService.WriteDebugAsync(
                    "Export",
                    "Neueste SharePoint-Audit-CSV lokal synchronisiert",
                    site.Id,
                    site.Land,
                    $"TSC={site.TSC}; Datei={fileName}; GeaendertUtc={remoteLastWriteUtc:O}");
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }
    }

    private async Task<SharePointFileReference?> ResolveLatestSharePointProcessedMergeInputFileAsync(
        Site site,
        SharePointConfig spConfig)
    {
        SharePointFileReference? latest = null;
        foreach (var folder in ResolveSharePointProcessedMergeInputFolders(site, spConfig))
        {
            try
            {
                var candidate = await _sharePointService.ResolveLatestProcessedMergeInputFileAsync(
                    spConfig.TenantId,
                    spConfig.ClientId,
                    spConfig.ClientSecret,
                    spConfig.SiteUrl,
                    folder,
                    site.TSC).WaitAsync(SharePointProbeTimeout);

                if (candidate?.LastModifiedUtc is null)
                    continue;

                if (latest?.LastModifiedUtc is null || candidate.LastModifiedUtc.Value > latest.LastModifiedUtc.Value)
                    latest = candidate;
            }
            catch (Exception ex)
            {
                // A status/sync probe must not block central export when another source is still usable.
                await _appEventLogService.WriteDebugAsync(
                    "Export",
                    "SharePoint-Audit-CSV Probe uebersprungen",
                    site.Id,
                    site.Land,
                    $"TSC={site.TSC}; Ordner={folder}; Fehler={ex.Message}");
            }
        }

        return latest;
    }

    private async Task UploadCentralFileAsync(
        SharePointConfig spConfig,
        string sharePointFolder,
        string landSubfolder,
        string localFilePath)
    {
        var fileName = Path.GetFileName(localFilePath);
        await _appEventLogService.WriteAsync(
            "Export",
            "Zentrale Datei SharePoint Upload gestartet",
            details: $"Datei={fileName}; Ziel={sharePointFolder}/{landSubfolder}".TrimEnd('/'));

        await _sharePointService.UploadAsync(
            spConfig.TenantId,
            spConfig.ClientId,
            spConfig.ClientSecret,
            spConfig.SiteUrl,
            sharePointFolder,
            landSubfolder,
            localFilePath,
            uploadTimestampedCopyIfLocked: true).WaitAsync(SharePointUploadTimeout);

        await _appEventLogService.WriteAsync(
            "Export",
            "Zentrale Datei SharePoint Upload abgeschlossen",
            details: $"Datei={fileName}; Ziel={sharePointFolder}/{landSubfolder}".TrimEnd('/'));
    }

    private static IEnumerable<ProofWorkbookPartition> BuildProofWorkbookPartitions(List<SalesRecord> sortedRecords)
    {
        foreach (var group in sortedRecords
            .GroupBy(record => new
            {
                Tsc = string.IsNullOrWhiteSpace(record.Tsc) ? "UNKNOWN" : record.Tsc.Trim(),
                Land = string.IsNullOrWhiteSpace(record.Land) ? "Unknown" : record.Land.Trim()
            })
            .OrderBy(group => group.Key.Tsc, StringComparer.OrdinalIgnoreCase)
            .ThenBy(group => group.Key.Land, StringComparer.OrdinalIgnoreCase))
        {
            var records = group.ToList();
            var baseScope = BuildProofWorkbookScope(group.Key.Tsc, group.Key.Land);
            if (records.Count <= MaxPartitionedProofWorkbookRows)
            {
                yield return new ProofWorkbookPartition(baseScope, records);
                continue;
            }

            var partCount = (int)Math.Ceiling(records.Count / (double)MaxPartitionedProofWorkbookRows);
            for (var partIndex = 0; partIndex < partCount; partIndex++)
            {
                var partRecords = records
                    .Skip(partIndex * MaxPartitionedProofWorkbookRows)
                    .Take(MaxPartitionedProofWorkbookRows)
                    .ToList();
                yield return new ProofWorkbookPartition($"{baseScope}_Teil{partIndex + 1:00}", partRecords);
            }
        }
    }

    private static string BuildProofWorkbookScope(string tsc, string land)
    {
        var normalizedTsc = string.IsNullOrWhiteSpace(tsc) ? "UNKNOWN" : tsc.Trim();
        var normalizedLand = string.IsNullOrWhiteSpace(land) ? "Unknown" : land.Trim();
        return string.Equals(normalizedTsc, normalizedLand, StringComparison.OrdinalIgnoreCase)
            ? normalizedTsc
            : $"{normalizedTsc}_{normalizedLand}";
    }

    private static string ResolveConsolidatedOutputDirectory(ExportSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.LocalConsolidatedExportFolder))
            return settings.LocalConsolidatedExportFolder.Trim();

        if (!string.IsNullOrWhiteSpace(settings.LocalSiteExportFolder))
            return settings.LocalSiteExportFolder.Trim();

        return Path.Combine(AppContext.BaseDirectory, "output");
    }

    private static (string Folder, string LandSubfolder) ResolveCentralSharePointUploadTarget(SharePointConfig config)
    {
        var configuredFolder = !string.IsNullOrWhiteSpace(config.CentralExportFolder)
            ? config.CentralExportFolder
            : IsLegacyExportFolder(config.ExportFolder)
                ? FinanceImportRootFolder
                : config.ExportFolder;

        var normalizedFolder = configuredFolder.Trim().TrimEnd('/', '\\');
        return normalizedFolder.EndsWith("/Alle", StringComparison.OrdinalIgnoreCase) ||
               normalizedFolder.EndsWith("\\Alle", StringComparison.OrdinalIgnoreCase)
            ? (normalizedFolder, string.Empty)
            : (configuredFolder, "Alle");
    }

    private static bool IsLegacyExportFolder(string folder)
    {
        var normalized = folder.Trim().TrimEnd('/', '\\');
        return normalized.Equals("/Shared Documents/Exports", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasCompleteSharePointConfig(SharePointConfig? config)
        => config is not null &&
           !string.IsNullOrWhiteSpace(config.TenantId) &&
           !string.IsNullOrWhiteSpace(config.ClientId) &&
           !string.IsNullOrWhiteSpace(config.ClientSecret) &&
           !string.IsNullOrWhiteSpace(config.SiteUrl);

    private static IEnumerable<string> ResolveSharePointProcessedMergeInputFolders(Site site, SharePointConfig sharePointConfig)
    {
        if (LooksLikeSharePointReference(site.ManualImportFilePath))
            yield return site.ManualImportFilePath.Trim();

        if (!string.IsNullOrWhiteSpace(sharePointConfig.ExportFolder))
            yield return string.Join("/", sharePointConfig.ExportFolder.Trim('/'), site.Land.Trim('/')).Trim('/');
    }

    private static bool LooksLikeSharePointReference(string path)
        => path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
           path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
           path.Contains("sharepoint.com", StringComparison.OrdinalIgnoreCase);

    private static string ResolveFileName(string fileReference)
        => fileReference
            .Split('/', '\\')
            .LastOrDefault(segment => !string.IsNullOrWhiteSpace(segment))
            ?.Trim() ?? string.Empty;

    private sealed record ProofWorkbookPartition(string Scope, List<SalesRecord> Records);
}
