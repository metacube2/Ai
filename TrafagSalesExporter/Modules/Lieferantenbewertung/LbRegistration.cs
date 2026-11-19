using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace TrafagSalesExporter.Modules.Lieferantenbewertung;

public sealed class LbOptions
{
    public const string SectionName = "Lieferantenbewertung";

    /// <summary>Notschalter: aus = Eingabeteil weg, Klasse A/B/C bleibt.</summary>
    public bool Enabled { get; set; } = true;
    public string DatabasePath { get; set; } = "";
    /// <summary>Windows-Kurzzeichen mit Schreibrecht (ohne Domaene). Entsperrte Admins duerfen immer.</summary>
    public List<string> EditUsers { get; set; } = [];
    public int BackupKeepDays { get; set; } = 60;
}

/// <summary>Plugin-Einstieg wie Handbestellwesen: eine Zeile in Program.cs, ein Schalter <c>Lieferantenbewertung:Enabled</c>.</summary>
public static class LbRegistration
{
    public static IServiceCollection AddLieferantenbewertung(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LbOptions>(configuration.GetSection(LbOptions.SectionName));
        services.AddDbContextFactory<LbDbContext>((sp, o) =>
            o.UseSqlite(VerbindungFuer(DatenbankPfad(sp.GetRequiredService<IHostEnvironment>(), sp.GetRequiredService<IOptionsMonitor<LbOptions>>().CurrentValue))));
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<LbService>();
        services.AddHostedService<LbStartUndSicherung>();
        return services;
    }

    public static string DatenbankPfad(IHostEnvironment env, LbOptions o)
    {
        var pfad = string.IsNullOrWhiteSpace(o.DatabasePath) ? Path.Combine("lbdaten", "lieferantenbewertung.db") : o.DatabasePath;
        return Path.IsPathRooted(pfad) ? pfad : Path.Combine(env.ContentRootPath, pfad);
    }

    public static string VerbindungFuer(string pfad)
        => new SqliteConnectionStringBuilder { DataSource = pfad, DefaultTimeout = 15 }.ToString();
}

/// <summary>Legt die Datenbank beim Start an und sichert sie einmal taeglich (SQLite-Backup-API).</summary>
internal sealed class LbStartUndSicherung(
    IDbContextFactory<LbDbContext> dbFactory,
    IOptionsMonitor<LbOptions> options,
    IHostEnvironment env,
    ILogger<LbStartUndSicherung> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.CurrentValue.Enabled)
            return;
        try
        {
            var pfad = LbRegistration.DatenbankPfad(env, options.CurrentValue);
            Directory.CreateDirectory(Path.GetDirectoryName(pfad)!);
            await using (var db = await dbFactory.CreateDbContextAsync(stoppingToken))
            {
                await db.Database.EnsureCreatedAsync(stoppingToken);
                await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", stoppingToken);
            }
            logger.LogInformation("Lieferantenbewertung: Datenbank bereit unter {Pfad}", pfad);

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
            logger.LogError(ex, "Lieferantenbewertung: Start oder Sicherung fehlgeschlagen");
        }
    }

    private void Sichere(string pfad)
    {
        var ordner = Path.Combine(Path.GetDirectoryName(pfad)!, "backup");
        Directory.CreateDirectory(ordner);
        var ziel = Path.Combine(ordner, $"lieferantenbewertung_{DateTime.Now:yyyyMMdd}.db");
        if (!File.Exists(ziel))
        {
            using var quelle = new SqliteConnection(LbRegistration.VerbindungFuer(pfad));
            using var kopie = new SqliteConnection(LbRegistration.VerbindungFuer(ziel));
            quelle.Open();
            kopie.Open();
            quelle.BackupDatabase(kopie);
            logger.LogInformation("Lieferantenbewertung: Sicherung {Ziel}", ziel);
        }

        var grenze = DateTime.Now.AddDays(-Math.Max(1, options.CurrentValue.BackupKeepDays));
        foreach (var alt in Directory.GetFiles(ordner, "lieferantenbewertung_*.db").Where(f => File.GetLastWriteTime(f) < grenze))
            File.Delete(alt);
    }
}
