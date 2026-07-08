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
        var settings = await db.ExportSettings
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync();

        if (settings is null || !settings.TimerEnabled)
        {
            _nextRun = DateTime.MaxValue;
            return;
        }

        _nextRun = TimerSchedule.ComputeNextRun(DateTime.Now, settings.TimerHour, settings.TimerMinute);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Nachhol-Lauf beim Start: War der Prozess zur geplanten Zeit nicht aktiv (z.B. weil
        // IIS den Worker nach einem Deploy erst beim ersten Request startet), fehlt der Tageslauf.
        // Damit das taegliche Tracking lueckenlos bleibt, wird ein verpasster Slot hier einmalig
        // nachgeholt, bevor der regulaere Warteloop beginnt.
        await CatchUpMissedRunAsync();
        await RecalculateNextRunAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            if (DateTime.Now < _nextRun) continue;

            await RunExportAsync("Timer-Export gestartet um {Time}");
            await RecalculateNextRunAsync();
        }
    }

    private async Task CatchUpMissedRunAsync()
    {
        try
        {
            var dbFactory = _serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            using var db = await dbFactory.CreateDbContextAsync();
            var settings = await db.ExportSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
            if (settings is null)
                return;

            var lastRunLocal = settings.LastTimerRunUtc?.ToLocalTime();
            if (!TimerSchedule.IsCatchUpDue(DateTime.Now, settings.TimerHour, settings.TimerMinute, settings.TimerEnabled, lastRunLocal))
                return;

            await RunExportAsync("Timer-Export Nachhol-Lauf (verpasster Slot) um {Time}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler bei der Pruefung auf einen verpassten Timer-Export");
        }
    }

    private async Task RunExportAsync(string logMessage)
    {
        _logger.LogInformation(logMessage, DateTime.Now);

        try
        {
            var orchestrator = _serviceProvider.GetRequiredService<ExportOrchestrationService>();
            await orchestrator.ExportAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Timer-Export");
        }

        // Nach dem Versuch stempeln (auch bei Teilfehler): verhindert, dass ein erneuter
        // Prozessstart am selben Tag den schweren Export wiederholt ausloest. Ein echter
        // Fehlversuch wird regulaer am naechsten geplanten Slot erneut ausgefuehrt.
        await StampLastRunAsync();
    }

    private async Task StampLastRunAsync()
    {
        try
        {
            var dbFactory = _serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            using var db = await dbFactory.CreateDbContextAsync();
            var settings = await db.ExportSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
            if (settings is null)
                return;

            settings.LastTimerRunUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Letzter Timer-Lauf konnte nicht gespeichert werden");
        }
    }
}
