using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface ILogsPageService
{
    Task<LogsPageState> LoadAsync(string? filterLand, string? filterStatus, DateTime? filterDate);
    Task<int> DeleteOldLogsAsync(int olderThanDays);
}

public sealed class LogsPageService : ILogsPageService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public LogsPageService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<LogsPageState> LoadAsync(string? filterLand, string? filterStatus, DateTime? filterDate)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        IQueryable<ExportLog> query = db.ExportLogs.OrderByDescending(l => l.Timestamp);

        if (!string.IsNullOrEmpty(filterLand))
            query = query.Where(l => l.Land == filterLand);

        if (!string.IsNullOrEmpty(filterStatus))
            query = query.Where(l => l.Status == filterStatus);

        if (filterDate.HasValue)
            query = query.Where(l => l.Timestamp.Date == filterDate.Value.Date);

        IQueryable<AppEventLog> appLogQuery = db.AppEventLogs.OrderByDescending(l => l.Timestamp);

        if (!string.IsNullOrEmpty(filterLand))
            appLogQuery = appLogQuery.Where(l => l.Land == filterLand);

        if (filterDate.HasValue)
            appLogQuery = appLogQuery.Where(l => l.Timestamp.Date == filterDate.Value.Date);

        return new LogsPageState
        {
            AvailableLands = await db.ExportLogs.Select(l => l.Land).Distinct().OrderBy(l => l).ToListAsync(),
            Logs = await query.Take(500).ToListAsync(),
            AppLogs = await appLogQuery.Take(500).ToListAsync()
        };
    }

    public async Task<int> DeleteOldLogsAsync(int olderThanDays)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var cutoff = DateTime.Now.AddDays(-olderThanDays);
        var oldLogs = await db.ExportLogs.Where(l => l.Timestamp < cutoff).ToListAsync();
        db.ExportLogs.RemoveRange(oldLogs);
        await db.SaveChangesAsync();
        return oldLogs.Count;
    }
}

public sealed class LogsPageState
{
    public List<ExportLog> Logs { get; set; } = [];
    public List<AppEventLog> AppLogs { get; set; } = [];
    public List<string> AvailableLands { get; set; } = [];
}
