using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;

namespace TrafagSalesExporter.Services;

public class TimerBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<TimerBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
                var exportService = scope.ServiceProvider.GetRequiredService<ExportOrchestrationService>();

                await using var db = await dbFactory.CreateDbContextAsync(stoppingToken);
                var settings = await db.ExportSettings.OrderBy(x => x.Id).FirstOrDefaultAsync(stoppingToken);

                if (settings is null || !settings.TimerEnabled)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                    continue;
                }

                var now = DateTime.Now;
                var nextRun = new DateTime(now.Year, now.Month, now.Day, settings.TimerHour, settings.TimerMinute, 0);
                if (nextRun <= now)
                {
                    nextRun = nextRun.AddDays(1);
                }

                var delay = nextRun - now;
                logger.LogInformation("Nächster automatischer Export um {NextRun}", nextRun);
                await Task.Delay(delay, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                await exportService.ExportAllActiveSitesAsync(stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fehler im TimerBackgroundService");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}
