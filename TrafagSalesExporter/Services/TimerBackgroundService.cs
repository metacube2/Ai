using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;

namespace TrafagSalesExporter.Services;

public class TimerBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TimerBackgroundService> _logger;
    private DateTime _nextRun = DateTime.MaxValue;

    public DateTime NextRun => _nextRun;

    public TimerBackgroundService(IServiceProvider serviceProvider, ILogger<TimerBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void Recalculate()
    {
        _ = RecalculateNextRunAsync();
    }

    private async Task RecalculateNextRunAsync()
    {
        var dbFactory = _serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using var db = await dbFactory.CreateDbContextAsync();
        var settings = await db.ExportSettings.FirstOrDefaultAsync();

        if (settings is null || !settings.TimerEnabled)
        {
            _nextRun = DateTime.MaxValue;
            return;
        }

        var now = DateTime.Now;
        var todayRun = new DateTime(now.Year, now.Month, now.Day, settings.TimerHour, settings.TimerMinute, 0);
        _nextRun = todayRun <= now ? todayRun.AddDays(1) : todayRun;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecalculateNextRunAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            if (DateTime.Now < _nextRun) continue;

            _logger.LogInformation("Timer-Export gestartet um {Time}", DateTime.Now);

            try
            {
                var orchestrator = _serviceProvider.GetRequiredService<ExportOrchestrationService>();
                await orchestrator.ExportAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Timer-Export");
            }

            await RecalculateNextRunAsync();
        }
    }
}
