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
            // Einkauf-Delta laeuft NUR im planmaessigen Slot, nicht im Nachhol-Lauf: ein
            // verpasster Slot (z.B. Deploy nach 03:00) darf nicht bei einem beliebigen
            // Tages-Restart einen SAP-Lauf gegen travp762 ausloesen.
            await RunPurchasingDeltaAsync();
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

    /// <summary>
    /// Naechtliches Einkauf-Delta.
    ///
    /// BEFUND 2026-07-30: Dieses Delta war nie gelaufen. Die Bedingung war vorher
    /// <c>Sites.IsActive</c> fuer <see cref="PurchasingDataSourcePageService.PurchasingTsc"/>, und
    /// dieses Flag steht produktiv auf <c>0</c> - die Methode stieg also jede Nacht STILL aus, ohne
    /// Log-Eintrag und ohne Status in <c>PurchasingSyncState</c> (dort standen ausschliesslich
    /// <c>Full</c>-Eintraege, der letzte vom 2026-07-24). Der Einkaufs-Cache war damit seit dem
    /// letzten manuellen Full Load eingefroren, was zu <c>MAX(EKKO.Bedat) = 2026-07-24</c> passte.
    /// Die manuellen Full Loads aus der UI haben diese Pruefung nicht und liefen weiter - dadurch
    /// blieb der Ausfall unbemerkt.
    ///
    /// <c>Sites.IsActive</c> ist als Bedingung ausserdem falsch belegt: <see
    /// cref="ExportOrchestrationService.ExportAllAsync"/> iteriert ueber ALLE aktiven Sites, und
    /// <c>PURCHASING_SAP</c> ist eine Einkaufs-Datenquelle ohne Sales-Strecke. Das Flag auf 1 zu
    /// setzen wuerde den Sales-Export dazu bringen, sie als Verkaufsstandort zu exportieren.
    /// Deshalb haengt das Delta jetzt nur noch daran, DASS die Einkaufsquelle konfiguriert ist
    /// (Entscheid Ingo, 2026-07-30) - <c>IsActive</c> bleibt unberuehrt und der Sales-Export
    /// unveraendert.
    ///
    /// Wichtig am neuen Verhalten: Ueberspringen wird geloggt. Ein stiller Aussteiger war der
    /// eigentliche Grund, warum der Ausfall sechs Tage unentdeckt blieb. Fehlende Zugangsdaten
    /// meldet <c>RunDeltaAsync</c> selbst als <c>Error</c>-Status samt Meldung, statt hier
    /// vorab geprueft zu werden - dann ist die Ursache im Refresh-Status sichtbar.
    /// </summary>
    private async Task RunPurchasingDeltaAsync()
    {
        try
        {
            var dbFactory = _serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            using var db = await dbFactory.CreateDbContextAsync();
            var purchasingConfigured = await db.Sites
                .AnyAsync(s => s.TSC == PurchasingDataSourcePageService.PurchasingTsc);
            if (!purchasingConfigured)
            {
                _logger.LogWarning(
                    "Einkauf-Delta uebersprungen: Site {Tsc} fehlt in der Konfiguration.",
                    PurchasingDataSourcePageService.PurchasingTsc);
                return;
            }

            _logger.LogInformation("Einkauf-Delta (naechtlich) gestartet um {Time}", DateTime.Now);

            // IPurchasingDataRefreshService ist Scoped -> eigener Scope statt Aufloesung
            // aus dem Singleton-Root (sonst Scope-Validation-Fehler).
            using var scope = _serviceProvider.CreateScope();
            var refresh = scope.ServiceProvider.GetRequiredService<IPurchasingDataRefreshService>();
            await refresh.RunDeltaAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim naechtlichen Einkauf-Delta");
        }
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
