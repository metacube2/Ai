using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public class ConsolidatedExportService : IConsolidatedExportService
{
    private const string FinanceImportRootFolder = "/Import/Finance";

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ICentralSalesDataProvider _centralSalesDataProvider;
    private readonly IExcelExportService _excelService;
    private readonly IExportAuditCsvService _auditCsvService;
    private readonly ISharePointUploadService _sharePointService;

    public ConsolidatedExportService(
        IDbContextFactory<AppDbContext> dbFactory,
        ICentralSalesDataProvider centralSalesDataProvider,
        IExcelExportService excelService,
        IExportAuditCsvService auditCsvService,
        ISharePointUploadService sharePointService)
    {
        _dbFactory = dbFactory;
        _centralSalesDataProvider = centralSalesDataProvider;
        _excelService = excelService;
        _auditCsvService = auditCsvService;
        _sharePointService = sharePointService;
    }

    public async Task<string?> ExportAsync()
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

        await SyncLatestProcessedMergeInputFilesAsync(sites, settings, spConfig);

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
        var consolidatedPath = _excelService.CreateConsolidatedExcelFile(
            outputDir,
            fileDate,
            sortedRecords);
        var proofPath = _excelService.CreateDashboardProofExcelFile(
            outputDir,
            fileDate,
            sortedRecords,
            settings.UseAuditCsvAsCentralSource);
        var auditCsvPath = await _auditCsvService.WriteConsolidatedAuditCsvAsync(
            settings,
            fileDate,
            outputDir,
            sortedRecords);

        if (spConfig is not null &&
            !string.IsNullOrWhiteSpace(spConfig.TenantId) &&
            !string.IsNullOrWhiteSpace(spConfig.ClientId) &&
            !string.IsNullOrWhiteSpace(spConfig.ClientSecret))
        {
            var (sharePointFolder, landSubfolder) = ResolveCentralSharePointUploadTarget(spConfig);

            await _sharePointService.UploadAsync(
                spConfig.TenantId, spConfig.ClientId, spConfig.ClientSecret,
                spConfig.SiteUrl, sharePointFolder, landSubfolder, consolidatedPath,
                uploadTimestampedCopyIfLocked: true);
            await _sharePointService.UploadAsync(
                spConfig.TenantId, spConfig.ClientId, spConfig.ClientSecret,
                spConfig.SiteUrl, sharePointFolder, landSubfolder, proofPath,
                uploadTimestampedCopyIfLocked: true);
            if (!string.IsNullOrWhiteSpace(auditCsvPath))
            {
                await _sharePointService.UploadAsync(
                    spConfig.TenantId, spConfig.ClientId, spConfig.ClientSecret,
                    spConfig.SiteUrl, sharePointFolder, landSubfolder, auditCsvPath,
                    uploadTimestampedCopyIfLocked: true);
            }
        }

        return consolidatedPath;
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
                latest.FileReference);

            try
            {
                File.Copy(tempPath, targetPath, overwrite: true);
                File.SetLastWriteTimeUtc(targetPath, remoteLastWriteUtc);
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
                    site.TSC);

                if (candidate?.LastModifiedUtc is null)
                    continue;

                if (latest?.LastModifiedUtc is null || candidate.LastModifiedUtc.Value > latest.LastModifiedUtc.Value)
                    latest = candidate;
            }
            catch
            {
                // A status/sync probe must not block central export when another source is still usable.
            }
        }

        return latest;
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
}
