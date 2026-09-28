using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;

namespace TrafagSalesExporter.Services;

public class TimerBackgroundService : BackgroundService
{
    /// <summary>Wie oft die Nachhol-Regel fuer das Einkauf-Delta geprueft wird.</summary>
    private static readonly TimeSpan PurchasingCatchUpCheckInterval = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Abstand der Selbstaufrufe. Deutlich unter dem IIS-Leerlauf-Timeout (Standard 20 Minuten),
    /// damit auch ein verspaeteter Aufruf den Worker noch haelt.
    /// </summary>
    private static readonly TimeSpan KeepAliveInterval = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Takt des Vorwaermens der Einkaufsansicht. Kurz genug, dass nach einem Einkauf-Lauf
    /// (Cache geleert) hoechstens fuenf Minuten bis zur Vorberechnung vergehen; ist der Stand
    /// frisch, kostet ein Durchgang nichts.
    /// </summary>
    private static readonly TimeSpan PurchasingWarmupInterval = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Ziel des Selbstaufrufs, ueberschreibbar mit <c>KeepAlive:Url</c> (leer = aus). Eine kleine
    /// statische Datei statt einer Seite, damit der Aufruf nichts rendert und nichts rechnet.
    /// </summary>
    private const string DefaultKeepAliveUrl = "https://trch-webapp-bidashboard.trafagch.local/BiDashboard/favicon.svg";

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TimerBackgroundService> _logger;
    private DateTime _nextRun = DateTime.MaxValue;
    private DateTime _nextPurchasingCatchUpCheck = DateTime.MinValue;

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
        _ = KeepAliveLoopAsync(stoppingToken);
        _ = WarmPurchasingDashboardLoopAsync(stoppingToken);

        await CatchUpMissedRunAsync();
        await RecalculateNextRunAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            // Erst in der Schleife, nicht beim Start: der PurchasingRefreshRunner raeumt bei
            // seinem eigenen Start liegengebliebene Running-Eintraege auf und muss vorher
            // gestartet sein. Nach 30 Sekunden ist er das sicher.
            await CheckPurchasingCatchUpAsync();

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

    /// <summary>
    /// Holt das Einkauf-Delta nach, wenn der planmaessige Slot es verpasst hat. Regel und
    /// Befund vom 2026-09-28 in <see cref="TimerSchedule.IsPurchasingCatchUpDue"/>.
    ///
    /// Gestartet wird ueber den <see cref="IPurchasingRefreshRunner"/>, nicht direkt: der Lauf
    /// haengt dann an der Anwendung, und ein gleichzeitiger Lauf (Klick in der Oberflaeche)
    /// wird abgewiesen statt doppelt gegen SAP zu laufen.
    /// </summary>
    private async Task CheckPurchasingCatchUpAsync()
    {
        if (DateTime.Now < _nextPurchasingCatchUpCheck)
            return;

        // Steht der heutige Slot noch aus oder ist er gerade faellig, macht ihn der regulaere
        // Lauf samt Delta. Sonst liefe das Delta um 12:00 doppelt: einmal hier, einmal im Slot.
        if (_nextRun.Date <= DateTime.Now.Date)
            return;
        _nextPurchasingCatchUpCheck = DateTime.Now + PurchasingCatchUpCheckInterval;

        try
        {
            var dbFactory = _serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            using var db = await dbFactory.CreateDbContextAsync();
            var settings = await db.ExportSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
            if (settings is null)
                return;

            if (!await db.Sites.AnyAsync(s => s.TSC == PurchasingDataSourcePageService.PurchasingTsc))
                return;

            var lastSuccessUtc = await ReadLastPurchasingSuccessUtcAsync(db);
            var lastSuccessLocal = lastSuccessUtc?.ToLocalTime();
            if (!TimerSchedule.IsPurchasingCatchUpDue(DateTime.Now, settings.TimerHour, settings.TimerMinute, settings.TimerEnabled, lastSuccessLocal))
                return;

            var runner = _serviceProvider.GetService<IPurchasingRefreshRunner>();
            if (runner is null)
                return;

            var started = runner.TryStart(PurchasingRefreshRunner.ModeDelta);
            _logger.LogInformation(
                "Einkauf-Delta Nachhol-Lauf {Result} um {Time}, letzter Erfolg {LastSuccess}",
                started ? "gestartet" : "nicht gestartet (es laeuft bereits ein Einkauf-Lauf)",
                DateTime.Now,
                lastSuccessLocal?.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture) ?? "nie");

            if (started)
            {
                var eventLog = _serviceProvider.GetService<IAppEventLogService>();
                if (eventLog is not null)
                    await eventLog.WriteAsync("Purchasing", "Einkauf-Delta nachgeholt",
                        details: $"Planmaessiger Slot verpasst. Letzter erfolgreicher Einkauf-Lauf: " +
                                 $"{lastSuccessLocal?.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture) ?? "nie"}.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler bei der Pruefung auf ein verpasstes Einkauf-Delta");
        }
    }

    /// <summary>
    /// Ende des letzten erfolgreichen Einkauf-Laufs (Full oder Delta). Die Zeitstempel liegen als
    /// ISO-Text vor; "Abgebrochen" und "Running" zaehlen bewusst nicht.
    /// </summary>
    private static async Task<DateTime?> ReadLastPurchasingSuccessUtcAsync(AppDbContext db)
    {
        var conn = (SqliteConnection)db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();

        await using var command = conn.CreateCommand();
        command.CommandText = "SELECT MAX(CompletedAtUtc) FROM PurchasingSyncState WHERE Status = 'Success';";
        var value = await command.ExecuteScalarAsync() as string;
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
            ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
            : null;
    }

    /// <summary>
    /// Haelt den IIS-Worker wach, indem die Anwendung sich alle paar Minuten selbst aufruft.
    ///
    /// BEFUND 2026-09-28: Der App-Pool steht nicht auf Dauerbetrieb (<c>AlwaysRunning</c>,
    /// <c>idleTimeout = 0</c>, siehe docs/FINANCE_DASHBOARD_PROZESSABLAUF_2026-06-30.md). IIS
    /// beendet den Worker deshalb nach rund 20 Minuten ohne Anfrage, auch mitten in einem
    /// Hintergrundlauf. Die Protokolle zeigen Neustarts alle ein bis drei Stunden. Dieser Aufruf
    /// ersetzt die Server-Einstellung nicht, er ueberbrueckt sie, bis sie gesetzt ist.
    ///
    /// Nur in Produktion: ein Entwicklungsrechner soll nicht das Produktivsystem wachhalten.
    /// </summary>
    /// <summary>
    /// Haelt die Standardansicht von `/einkauf` fertig berechnet bereit.
    ///
    /// BEFUND 2026-09-28: Die Berechnung dauert auf dem Server 98,6 Sekunden (lokal 12), verteilt
    /// auf rund 55 gleich teure Abfragen - es gibt keinen einzelnen Engpass, den man beheben
    /// koennte. Deshalb soll kein Nutzer sie abwarten: nach dem Start, nach jedem Einkauf-Lauf
    /// (der den Cache leert) und nach Ablauf rechnet hier der Hintergrund. Ist der Stand noch
    /// frisch, kostet der Aufruf nichts; ist er abgelaufen, liefert der Cache den alten Stand
    /// und rechnet selbst im Hintergrund nach.
    ///
    /// Eigene Schleife, nicht im 30-Sekunden-Takt des Timers: der steht waehrend des
    /// Tagesexports rund 45 Minuten still.
    /// </summary>
    private async Task WarmPurchasingDashboardLoopAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Nur in Produktion: ohne Einkaufscache faellt die Seite auf eine SAP-Live-Stichprobe
            // zurueck, und die soll ein Entwicklungsrechner nicht alle fuenf Minuten ausloesen.
            var environment = _serviceProvider.GetService<IHostEnvironment>();
            if (environment is null || !environment.IsProduction())
                return;

            // Erst nach dem Start, damit der Prozess zuerst Anfragen bedient.
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dashboard = scope.ServiceProvider.GetRequiredService<IPurchasingDashboardService>();
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    await dashboard.LoadAsync(PurchasingDashboardFilter.Default(DateTime.Today), stoppingToken);
                    watch.Stop();
                    // Nur melden, wenn wirklich gerechnet wurde; ein Treffer im Cache ist Routine.
                    if (watch.Elapsed > TimeSpan.FromSeconds(5))
                        _logger.LogInformation("Einkauf-Standardansicht vorberechnet in {Seconds:N1} s", watch.Elapsed.TotalSeconds);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogWarning(ex, "Einkauf-Standardansicht konnte nicht vorberechnet werden");
                }

                await Task.Delay(PurchasingWarmupInterval, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task KeepAliveLoopAsync(CancellationToken stoppingToken)
    {
        try
        {
            var environment = _serviceProvider.GetService<IHostEnvironment>();
            if (environment is null || !environment.IsProduction())
                return;

            var configured = _serviceProvider.GetService<IConfiguration>()?["KeepAlive:Url"];
            var url = configured ?? DefaultKeepAliveUrl;
            if (string.IsNullOrWhiteSpace(url))
                return;

            using var handler = new HttpClientHandler { UseDefaultCredentials = true };
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(1) };
            var failureLogged = false;

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(KeepAliveInterval, stoppingToken);
                try
                {
                    using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, stoppingToken);
                    failureLogged = false;
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    // Einmal melden, nicht alle fuenf Minuten: ein dauerhaft falsches Ziel soll
                    // das Protokoll nicht fluten.
                    if (!failureLogged)
                        _logger.LogWarning(ex, "Selbstaufruf zum Wachhalten fehlgeschlagen: {Url}", url);
                    failureLogged = true;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Wachhalten beendet");
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
