using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public class AppEventLogService : IAppEventLogService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<AppEventLogService> _logger;

    public AppEventLogService(IDbContextFactory<AppDbContext> dbFactory, ILogger<AppEventLogService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task WriteAsync(string category, string message, string level = "Info", int? siteId = null, string? land = null, string? details = null)
    {
        try
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            db.AppEventLogs.Add(new AppEventLog
            {
                Timestamp = DateTime.Now,
                Level = string.IsNullOrWhiteSpace(level) ? "Info" : level.Trim(),
                Category = category?.Trim() ?? string.Empty,
                SiteId = siteId,
                Land = land?.Trim() ?? string.Empty,
                Message = message?.Trim() ?? string.Empty,
                Details = details?.Trim() ?? string.Empty
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AppEventLog konnte nicht gespeichert werden: {Category} - {Message}", category, message);
        }
    }

    public async Task WriteDebugAsync(string category, string message, int? siteId = null, string? land = null, string? details = null)
    {
        try
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var settings = await db.ExportSettings.FirstOrDefaultAsync();
            if (settings is null || !settings.DebugLoggingEnabled)
                return;

            db.AppEventLogs.Add(new AppEventLog
            {
                Timestamp = DateTime.Now,
                Level = "Debug",
                Category = category?.Trim() ?? string.Empty,
                SiteId = siteId,
                Land = land?.Trim() ?? string.Empty,
                Message = message?.Trim() ?? string.Empty,
                Details = details?.Trim() ?? string.Empty
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Debug-AppEventLog konnte nicht gespeichert werden: {Category} - {Message}", category, message);
        }
    }
}
