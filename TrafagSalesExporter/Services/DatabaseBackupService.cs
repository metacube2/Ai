using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TrafagSalesExporter.Data;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Wochensicherung der Hauptdatenbank in der App (seit 2026-11-24, Auftrag Ingo: „mach alles, dass die Sicherung nicht laeuft,
/// wenn ich am Arbeiten bin“). Bisher sicherte nur das Deploy-Werkzeug vom Laptop aus, seitenweise ueber das Netz (bis 15 Min.,
/// haengte den Deploy). Jetzt kopiert die App die Datenbank nachts selbst mit der SQLite-Sicherung (wie Shopfloor) neben die
/// Datenbank, unter dem Namen, den das Deploy-Werkzeug schon als Sicherung erkennt (<c>trafag_exporter.db.before-appsicherung-*.bak</c>);
/// damit entfaellt dort die Wochensicherung. Die Server selbst sichert die IT mit Veeam.
/// </summary>
public sealed class DatabaseBackupOptions
{
    public const string SectionName = "DatabaseBackup";

    public bool Enabled { get; set; } = true;

    /// <summary>Wochentag der Sicherung; Standard Samstag (Nacht Freitag auf Samstag, nach dem Einkauf-Delta um Mitternacht).</summary>
    public DayOfWeek Day { get; set; } = DayOfWeek.Saturday;

    /// <summary>Nachtfenster, nur darin wird automatisch gesichert (Stunden, Ortszeit, Ende ausschliesslich).</summary>
    public int WindowStartHour { get; set; } = 3;
    public int WindowEndHour { get; set; } = 5;

    /// <summary>So viele eigene Sicherungen bleiben liegen; die des Deploy-Werkzeugs werden nie angefasst.</summary>
    public int Keep { get; set; } = 4;

    /// <summary>Ist die neueste Sicherung aelter, wird in der naechsten Nacht nachgeholt, auch wenn nicht der Wochentag ist.</summary>
    public int MaxAgeDays { get; set; } = 7;
}

public sealed record DatabaseBackupFile(string Name, string FullPath, DateTime WrittenAt, long Bytes, bool FromApp);

public sealed record DatabaseBackupResult(bool Ok, string Message, string? File, long Bytes, TimeSpan Duration, DateTime At);

/// <summary>Schreiben, Aufraeumen und Auflisten; ohne Dienst und DI testbar.</summary>
public static class DatabaseBackupFiles
{
    public const string AppLabel = "appsicherung";

    private static readonly Regex OwnName = new(@"\.before-appsicherung-(\d{8}-\d{6})\.bak$", RegexOptions.CultureInvariant);

    /// <summary>Alle Sicherungen der Datenbank im selben Ordner (eigene und die des Deploy-Werkzeugs), neueste zuerst.</summary>
    public static List<DatabaseBackupFile> List(string databasePath)
    {
        var folder = Path.GetDirectoryName(databasePath)!;
        var name = Path.GetFileName(databasePath);
        if (!Directory.Exists(folder))
            return [];
        return Directory.GetFiles(folder, name + ".before-*.bak")
            .Select(f => new FileInfo(f))
            .Select(f => new DatabaseBackupFile(f.Name, f.FullName, f.LastWriteTime, f.Length, OwnName.IsMatch(f.Name)))
            .OrderByDescending(f => f.WrittenAt)
            .ToList();
    }

    /// <summary>
    /// Faellig: nur im Nachtfenster, und entweder am Sicherungstag ohne eigene Sicherung von heute oder wenn die neueste Sicherung
    /// (gleich welche) aelter als <see cref="DatabaseBackupOptions.MaxAgeDays"/> ist (verpasste Nacht).
    /// </summary>
    public static bool IsDue(DateTime now, DatabaseBackupOptions options, DateTime? newestAny, DateTime? newestOwn)
    {
        if (!options.Enabled)
            return false;
        if (now.Hour < options.WindowStartHour || now.Hour >= options.WindowEndHour)
            return false;
        if (newestOwn is { } own && own.Date == now.Date)
            return false;
        if (now.DayOfWeek == options.Day)
            return true;
        return newestAny is null || (now - newestAny.Value).TotalDays > Math.Max(1, options.MaxAgeDays);
    }

    /// <summary>
    /// Kopiert die Datenbank mit der SQLite-Sicherung (konsistent, im laufenden Betrieb) zuerst in eine .tmp-Datei und benennt sie
    /// erst danach um, damit das Deploy-Werkzeug nie eine halbe Kopie als neueste Sicherung sieht. Danach werden alte eigene
    /// Sicherungen ueber <paramref name="keep"/> hinaus geloescht.
    /// </summary>
    public static DatabaseBackupResult Create(string databasePath, DateTime now, int keep)
    {
        var started = DateTime.Now;
        var target = databasePath + $".before-{AppLabel}-{now:yyyyMMdd-HHmmss}.bak";
        var temp = target + ".tmp";
        try
        {
            if (!File.Exists(databasePath))
                return new DatabaseBackupResult(false, $"Datenbank nicht gefunden: {databasePath}", null, 0, TimeSpan.Zero, now);

            var size = new FileInfo(databasePath).Length;
            var root = Path.GetPathRoot(Path.GetFullPath(databasePath));
            if (!string.IsNullOrEmpty(root))
            {
                var free = new DriveInfo(root).AvailableFreeSpace;
                if (free < 2 * size)
                    return new DatabaseBackupResult(false, $"Zu wenig Platz: frei {free / 1048576:N0} MB, Datenbank {size / 1048576:N0} MB (verlangt das Doppelte).",
                        null, 0, TimeSpan.Zero, now);
            }

            if (File.Exists(temp))
                File.Delete(temp);
            using (var source = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = databasePath, Pooling = false, DefaultTimeout = 60 }.ToString()))
            using (var destination = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = temp, Pooling = false }.ToString()))
            {
                source.Open();
                destination.Open();
                source.BackupDatabase(destination);
            }
            File.Move(temp, target);
            var bytes = new FileInfo(target).Length;
            var removed = Prune(databasePath, keep);
            var duration = DateTime.Now - started;
            return new DatabaseBackupResult(true,
                $"{Path.GetFileName(target)}, {bytes / 1048576.0:N0} MB, {duration.TotalSeconds:N0} s" +
                (removed > 0 ? $", {removed} alte eigene Sicherung(en) geloescht" : string.Empty),
                target, bytes, duration, now);
        }
        catch (Exception ex)
        {
            try
            {
                if (File.Exists(temp))
                    File.Delete(temp);
            }
            catch
            {
                // Die Fehlermeldung unten ist wichtiger als die liegengebliebene .tmp-Datei.
            }
            return new DatabaseBackupResult(false, $"{ex.GetType().Name}: {ex.Message}", null, 0, DateTime.Now - started, now);
        }
    }

    /// <summary>Loescht eigene Sicherungen ueber die neuesten <paramref name="keep"/> hinaus. Sicherungen des Deploy-Werkzeugs bleiben.</summary>
    public static int Prune(string databasePath, int keep)
    {
        var own = List(databasePath)
            .Where(f => f.FromApp)
            .OrderByDescending(f => OwnStamp(f.Name))
            .Skip(Math.Max(1, keep))
            .ToList();
        foreach (var f in own)
            File.Delete(f.FullPath);
        return own.Count;
    }

    private static DateTime OwnStamp(string name)
    {
        var m = OwnName.Match(name);
        return m.Success && DateTime.TryParseExact(m.Groups[1].Value, "yyyyMMdd-HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d
            : DateTime.MinValue;
    }

    /// <summary>Pfad der Datenbank wie SQLite ihn aufloest (relativ zum Arbeitsverzeichnis der App).</summary>
    public static string ResolveDatabasePath(string connectionString)
        => Path.GetFullPath(new SqliteConnectionStringBuilder(connectionString).DataSource);
}

/// <summary>Gemeinsamer Zustand fuer Dienst und Admin-Knopf: hoechstens eine Sicherung zur Zeit.</summary>
public sealed class DatabaseBackupManager
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IAppEventLogService _log;
    private readonly IOptionsMonitor<DatabaseBackupOptions> _options;
    private readonly IPurchasingRefreshRunner? _purchasing;
    private int _running;

    public DatabaseBackupManager(IDbContextFactory<AppDbContext> dbFactory, IAppEventLogService log,
        IOptionsMonitor<DatabaseBackupOptions> options, IPurchasingRefreshRunner? purchasing = null)
    {
        _dbFactory = dbFactory;
        _log = log;
        _options = options;
        _purchasing = purchasing;
    }

    public bool IsRunning => Volatile.Read(ref _running) == 1;
    public DatabaseBackupResult? LastResult { get; private set; }
    public DatabaseBackupOptions Options => _options.CurrentValue;
    public bool PurchasingRunning => _purchasing?.Current.IsRunning == true;

    public string DatabasePath
    {
        get
        {
            using var db = _dbFactory.CreateDbContext();
            return DatabaseBackupFiles.ResolveDatabasePath(db.Database.GetConnectionString()!);
        }
    }

    public List<DatabaseBackupFile> List() => DatabaseBackupFiles.List(DatabasePath);

    /// <summary>Startet eine Sicherung im Hintergrund (nie im Klick-Handler abwarten, Vorfall 2026-08-19). false, wenn schon eine laeuft.</summary>
    public bool TryStart(string trigger)
    {
        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
            return false;
        _ = Task.Run(async () =>
        {
            try
            {
                await RunCoreAsync(trigger);
            }
            finally
            {
                Volatile.Write(ref _running, 0);
            }
        });
        return true;
    }

    /// <summary>Fuer den Dienst: laeuft im eigenen Durchgang und wartet das Ende ab.</summary>
    internal async Task<DatabaseBackupResult?> RunIfFreeAsync(string trigger)
    {
        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
            return null;
        try
        {
            return await RunCoreAsync(trigger);
        }
        finally
        {
            Volatile.Write(ref _running, 0);
        }
    }

    private async Task<DatabaseBackupResult> RunCoreAsync(string trigger)
    {
        var path = DatabasePath;
        var result = await Task.Run(() => DatabaseBackupFiles.Create(path, DateTime.Now, _options.CurrentValue.Keep));
        LastResult = result;
        await _log.WriteAsync("System", result.Ok ? $"Datenbanksicherung geschrieben ({trigger})" : $"Datenbanksicherung fehlgeschlagen ({trigger})",
            result.Ok ? "Info" : "Warning", details: result.Message);
        return result;
    }
}

/// <summary>Prueft alle 10 Minuten, ob die Wochensicherung faellig ist; nie waehrend eines Einkauf-Laufs.</summary>
public sealed class DatabaseBackupService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(10);
    private const int MaxAttemptsPerNight = 2;
    private readonly DatabaseBackupManager _manager;
    private DateTime _attemptDay;
    private int _attempts;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DatabaseBackupService> _logger;

    public DatabaseBackupService(DatabaseBackupManager manager, IHostEnvironment environment, ILogger<DatabaseBackupService> logger)
    {
        _manager = manager;
        _environment = environment;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_environment.IsProduction())
            return;
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_manager.PurchasingRunning)
                {
                    var files = _manager.List();
                    var newestAny = files.FirstOrDefault()?.WrittenAt;
                    var newestOwn = files.FirstOrDefault(f => f.FromApp)?.WrittenAt;
                    var now = DateTime.Now;
                    if (_attemptDay != now.Date)
                    {
                        _attemptDay = now.Date;
                        _attempts = 0;
                    }
                    // Hoechstens zwei Versuche je Nacht, damit ein Dauerfehler (z. B. Platte voll) nicht alle 10 Minuten meldet.
                    if (_attempts < MaxAttemptsPerNight && DatabaseBackupFiles.IsDue(now, _manager.Options, newestAny, newestOwn))
                    {
                        _attempts++;
                        await _manager.RunIfFreeAsync("Wochensicherung");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Datenbanksicherung: Pruefung fehlgeschlagen.");
            }
            await Task.Delay(CheckInterval, stoppingToken);
        }
    }
}
