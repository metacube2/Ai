using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public class ConsolidatedExportService : IConsolidatedExportService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ICentralSalesRecordService _centralSalesRecordService;
    private readonly IExcelExportService _excelService;
    private readonly ISharePointUploadService _sharePointService;

    public ConsolidatedExportService(
        IDbContextFactory<AppDbContext> dbFactory,
        ICentralSalesRecordService centralSalesRecordService,
        IExcelExportService excelService,
        ISharePointUploadService sharePointService)
    {
        _dbFactory = dbFactory;
        _centralSalesRecordService = centralSalesRecordService;
        _excelService = excelService;
        _sharePointService = sharePointService;
    }

    public async Task<string?> ExportAsync(List<SalesRecord> records)
    {
        var consolidatedRecords = await _centralSalesRecordService.GetAllAsync();
        if (consolidatedRecords.Count == 0)
            return null;

        using var db = await _dbFactory.CreateDbContextAsync();
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

        return consolidatedPath;
    }
}
