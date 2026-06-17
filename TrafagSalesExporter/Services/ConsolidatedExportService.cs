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
        var consolidatedRecords = await _centralSalesDataProvider.GetRecordsAsync();
        if (consolidatedRecords.Count == 0)
            return null;

        using var db = await _dbFactory.CreateDbContextAsync();
        var spConfig = await db.SharePointConfigs.FirstOrDefaultAsync();
        var settings = await db.ExportSettings.FirstOrDefaultAsync() ?? new ExportSettings();
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
}
