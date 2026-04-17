using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public class ExportLogService : IExportLogService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public ExportLogService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task WriteAsync(ExportLog log)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        db.ExportLogs.Add(log);
        await db.SaveChangesAsync();
    }
}
