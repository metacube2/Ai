using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace TrafagSalesExporter.Modules.Handbestellwesen;

/// <summary>
/// Plugin-Einstieg: eine Zeile in Program.cs (<c>builder.Services.AddHandbestellwesen(builder.Configuration)</c>), ein Schalter
/// <c>Handbestellwesen:Enabled</c>. Mitgenutzt werden Windows-Anmeldung, Layout und Menue des Dashboards.
/// </summary>
public static class HbRegistration
{
    public static IServiceCollection AddHandbestellwesen(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<HbOptions>(configuration.GetSection(HbOptions.SectionName));
        services.AddDbContextFactory<HbDbContext>((sp, o) =>
            o.UseSqlite(VerbindungFuer(DatenbankPfad(sp.GetRequiredService<IHostEnvironment>(), sp.GetRequiredService<IOptionsMonitor<HbOptions>>().CurrentValue))));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<HbService>();
        services.AddSingleton<HbImportService>();
        services.AddSingleton<IHbAdSuche, HbAdSuche>();
        services.AddHostedService<HbStartUndSicherung>();
        return services;
    }

    public static string DatenbankPfad(IHostEnvironment env, HbOptions o)
    {
        var pfad = string.IsNullOrWhiteSpace(o.DatabasePath) ? Path.Combine("hbdaten", "handbestellwesen.db") : o.DatabasePath;
        return Path.IsPathRooted(pfad) ? pfad : Path.Combine(env.ContentRootPath, pfad);
    }

    /// <summary>Hier legt man den Dump der PHP-Anwendung fuer den Altdaten-Import ab: hbdaten\import\db.sql.</summary>
    public static string ImportPfad(IHostEnvironment env, HbOptions o)
        => Path.Combine(Path.GetDirectoryName(DatenbankPfad(env, o))!, "import", "db.sql");

    public static string VerbindungFuer(string pfad)
        => new SqliteConnectionStringBuilder { DataSource = pfad, DefaultTimeout = 15 }.ToString();
}

/// <summary>Legt die Datenbank beim Start an und sichert sie einmal taeglich (SQLite-Backup-API, auch waehrend des Betriebs konsistent).</summary>
internal sealed class HbStartUndSicherung(
    IDbContextFactory<HbDbContext> dbFactory,
    IOptionsMonitor<HbOptions> options,
    IHostEnvironment env,
    ILogger<HbStartUndSicherung> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.CurrentValue.Enabled)
            return;
        try
        {
            var pfad = HbRegistration.DatenbankPfad(env, options.CurrentValue);
            Directory.CreateDirectory(Path.GetDirectoryName(pfad)!);
            await using (var db = await dbFactory.CreateDbContextAsync(stoppingToken))
            {
                await db.Database.EnsureCreatedAsync(stoppingToken);
                await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", stoppingToken);
                await ErgaenzeSpaltenAsync(db, stoppingToken);
            }
            logger.LogInformation("Handbestellwesen: Datenbank bereit unter {Pfad}", pfad);

            while (!stoppingToken.IsCancellationRequested)
            {
                Sichere(pfad);
                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Handbestellwesen: Start oder Sicherung fehlgeschlagen");
        }
    }

    /// <summary>EnsureCreated legt nur neue Datenbanken an; spaeter hinzugekommene Spalten werden hier nachgezogen.</summary>
    internal static async Task ErgaenzeSpaltenAsync(HbDbContext db, CancellationToken ct)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync(ct);
        var vorhanden = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "PRAGMA table_info(Bestellung);";
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                vorhanden.Add(reader.GetString(1));
        }
        // 2026-10-22: WertText fuer Altdaten mit Text statt Betrag ("KV").
        if (!vorhanden.Contains("WertText"))
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE Bestellung ADD COLUMN WertText TEXT NULL;", ct);
    }

    private void Sichere(string pfad)
    {
        var ordner = Path.Combine(Path.GetDirectoryName(pfad)!, "backup");
        Directory.CreateDirectory(ordner);
        var ziel = Path.Combine(ordner, $"handbestellwesen_{DateTime.Now:yyyyMMdd}.db");
        if (!File.Exists(ziel))
        {
            using var quelle = new SqliteConnection(HbRegistration.VerbindungFuer(pfad));
            using var kopie = new SqliteConnection(HbRegistration.VerbindungFuer(ziel));
            quelle.Open();
            kopie.Open();
            quelle.BackupDatabase(kopie);
            logger.LogInformation("Handbestellwesen: Sicherung {Ziel}", ziel);
        }

        var grenze = DateTime.Now.AddDays(-Math.Max(1, options.CurrentValue.BackupKeepDays));
        foreach (var alt in Directory.GetFiles(ordner, "handbestellwesen_*.db").Where(f => File.GetLastWriteTime(f) < grenze))
            File.Delete(alt);
    }
}
