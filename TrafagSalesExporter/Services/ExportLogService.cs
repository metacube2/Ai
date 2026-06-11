using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public class ExportLogService : IExportLogService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<ExportLogService> _logger;

    public ExportLogService(IDbContextFactory<AppDbContext> dbFactory, ILogger<ExportLogService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task WriteAsync(ExportLog log)
    {
        try
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            db.ExportLogs.Add(log);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExportLog konnte nicht gespeichert werden: {Land} ({TSC})", log.Land, log.TSC);
        }
    }
}
