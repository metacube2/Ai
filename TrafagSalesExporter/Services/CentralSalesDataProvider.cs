using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface ICentralSalesDataProvider
{
    Task<List<SalesRecord>> GetRecordsAsync();
    Task<bool> UsesAuditCsvAsync();
}

public sealed class CentralSalesDataProvider : ICentralSalesDataProvider
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ICentralSalesRecordService _centralSalesRecordService;
    private readonly IExportAuditCsvService _auditCsvService;

    public CentralSalesDataProvider(
        IDbContextFactory<AppDbContext> dbFactory,
        ICentralSalesRecordService centralSalesRecordService,
        IExportAuditCsvService auditCsvService)
    {
        _dbFactory = dbFactory;
        _centralSalesRecordService = centralSalesRecordService;
        _auditCsvService = auditCsvService;
    }

    public async Task<List<SalesRecord>> GetRecordsAsync()
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var settings = await db.ExportSettings.AsNoTracking().FirstOrDefaultAsync() ?? new ExportSettings();
        if (!settings.UseAuditCsvAsCentralSource)
            return await _centralSalesRecordService.GetAllAsync();

        var records = await _auditCsvService.ReadLatestSiteAuditCsvRecordsAsync(settings);
        if (records.Count == 0)
        {
            var directory = _auditCsvService.ResolveAuditCsvDirectory(settings);
            throw new InvalidOperationException(
                $"Audit-CSV ist als zentrale Quelle aktiv, aber im Ordner '{directory}' wurden keine Sales_*.csv-Dateien gefunden.");
        }

        return records
            .OrderBy(r => r.Land)
            .ThenBy(r => r.Tsc)
            .ThenByDescending(r => r.InvoiceDate ?? DateTime.MinValue)
            .ThenBy(r => r.InvoiceNumber)
            .ThenBy(r => r.PositionOnInvoice)
            .ToList();
    }

    public async Task<bool> UsesAuditCsvAsync()
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var settings = await db.ExportSettings.AsNoTracking().FirstOrDefaultAsync() ?? new ExportSettings();
        return settings.UseAuditCsvAsCentralSource;
    }
}
