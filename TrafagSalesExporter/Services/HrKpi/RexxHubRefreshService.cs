using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Security;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Holt die Rexx-Abfragen #775 und #777 einmal am Tag ueber den Rexx Hub und schreibt daraus genau die Dateien, die heute
/// von Hand exportiert werden (<see cref="HrKpiDataSourceOptions.GesamtFile"/>, <see cref="HrKpiDataSourceOptions.RexxCaseFile"/>).
/// Gebaut wie <see cref="HrKpiSapRefreshService"/>: Ziel ist <c>ContentRootPath/hrdata</c> (wie der SAP-Abruf, nicht
/// <c>HrKpi:DataFolder</c>), Schreiben ueber <c>.tmp.xlsx</c>, eine von Hand gelegte Datei wird vor dem ersten Ueberschreiben
/// einmalig als <c>*.manuell.xlsx</c> gesichert, Protokoll ueber das Ereignisprotokoll (Kategorie HR).
///
/// SCHALTER: Nur mit <c>HrKpi:RexxHub:Enabled = true</c> und nur in Produktion. Aus: der Dienst endet sofort und ruft nichts ab.
/// WIRFT NIE: Bei Fehler, leerer Antwort oder fehlender Pflichtspalte bleibt die bisherige Datei stehen. Ins Protokoll kommen
/// Zeilenzahl, Spaltennamen, Dauer und request id, nie Werte (Personendaten) und nie der Token.
/// Jede Datei hat ihren eigenen Faelligkeitsstempel (LEARNINGS 2026-10-01).
/// </summary>
public sealed class RexxHubRefreshService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(30);

    private readonly IRexxHubConnectionSource _source;
    private readonly RexxHubClient _client;
    private readonly IAppEventLogService _log;
    private readonly IHostEnvironment _environment;
    private readonly RexxHubOptions _options;
    private readonly HrKpiDataSourceOptions _files;
    private DateOnly _lastGesamtAttempt;
    private DateOnly _lastCaseAttempt;

    public RexxHubRefreshService(
        IRexxHubConnectionSource source,
        RexxHubClient client,
        IAppEventLogService log,
        IHostEnvironment environment,
        IOptions<RexxHubOptions> options,
        IOptions<HrKpiDataSourceOptions> files)
    {
        _source = source;
        _client = client;
        _log = log;
        _environment = environment;
        _options = options.Value;
        _files = files.Value.Normalize();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || !_environment.IsProduction())
            return;

        // Etwas nach dem SAP-Abruf (2 Minuten nach dem Start), damit sich die beiden nicht ueberlappen.
        await Task.Delay(TimeSpan.FromMinutes(3), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunDueAsync(DateTime.Now, stoppingToken);
            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    internal string DataFolder => Path.Combine(_environment.ContentRootPath, "hrdata");

    /// <summary>Ein Durchgang: holt jede faellige Datei. Bei ausgeschaltetem Schalter passiert nichts.</summary>
    internal async Task RunDueAsync(DateTime now, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
            return;

        var gesamtPath = Path.Combine(DataFolder, _files.GesamtFile);
        if (IsDue(now, _options.DailyAt, LastWrite(gesamtPath), _lastGesamtAttempt))
        {
            _lastGesamtAttempt = DateOnly.FromDateTime(now);
            await RefreshAsync(RexxHubQueryKind.Gesamt, gesamtPath, cancellationToken);
        }

        var casePath = Path.Combine(DataFolder, _files.RexxCaseFile);
        if (IsDue(now, _options.DailyAt, LastWrite(casePath), _lastCaseAttempt))
        {
            _lastCaseAttempt = DateOnly.FromDateTime(now);
            await RefreshAsync(RexxHubQueryKind.Faelle, casePath, cancellationToken);
        }
    }

    private static DateTime? LastWrite(string path) => File.Exists(path) ? File.GetLastWriteTime(path) : null;

    /// <summary>Faellig, wenn es nach <paramref name="dailyAt"/> ist, heute noch nicht versucht wurde und die Datei aelter ist.</summary>
    internal static bool IsDue(DateTime now, TimeSpan dailyAt, DateTime? fileWrittenAt, DateOnly lastAttempt)
    {
        if (now.TimeOfDay < dailyAt)
            return false;
        if (lastAttempt == DateOnly.FromDateTime(now))
            return false;
        return fileWrittenAt is null || fileWrittenAt.Value < now.Date + dailyAt;
    }

    /// <summary>Holt eine Abfrage, prueft sie und schreibt die Datei. true, wenn geschrieben wurde.</summary>
    internal async Task<bool> RefreshAsync(RexxHubQueryKind kind, string path, CancellationToken cancellationToken)
    {
        var queryId = kind == RexxHubQueryKind.Gesamt ? _options.GesamtQueryId : _options.CaseQueryId;
        var label = $"#{queryId}";
        string? token = null;
        try
        {
            var connection = await _source.ResolveAsync(cancellationToken);
            token = connection.Token;
            var result = await _client.FetchQueryAsync(connection, queryId, _options, cancellationToken);
            var check = RexxHubColumns.Check(kind, result.Table);
            var requestId = string.IsNullOrWhiteSpace(result.RequestId) ? "keine" : result.RequestId;
            if (!check.KannSchreiben)
            {
                await _log.WriteAsync("HR", $"Rexx-Hub {label} nicht geschrieben", "Warning",
                    details: $"{check.Ablehnungsgrund()}. Die bisherige Datei bleibt stehen. Zeilen {check.Zeilen}, request id {requestId}.");
                return false;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var manualBackup = Path.Combine(Path.GetDirectoryName(path)!, Path.GetFileNameWithoutExtension(path) + ".manuell.xlsx");
            if (File.Exists(path) && !File.Exists(manualBackup))
                File.Copy(path, manualBackup);

            RexxHubWorkbook.Write(kind, result.Table, path);

            var notes = new List<string>();
            var optionalMissing = check.FehlendeLeserspalten.Except(check.FehlendePflichtspalten).ToList();
            if (optionalMissing.Count > 0)
                notes.Add("Spalten fehlen (keine Pflicht, Kennzahl bleibt leer): " + string.Join("; ", optionalMissing));
            var deviations = check.Formatabweichungen.ToList();
            if (deviations.Count > 0)
                notes.Add("Format nicht erkannt: " + string.Join("; ", deviations));
            await _log.WriteAsync("HR", $"Rexx-Hub {label} geschrieben", notes.Count > 0 ? "Warning" : "Info",
                details: $"{result.Table.Rows.Count} Zeilen, {result.Table.Columns.Count} Spalten, {result.Pages} Seite(n), " +
                         $"{result.Duration.TotalSeconds:0.0} s, request id {requestId}, Datei {path}" +
                         (notes.Count > 0 ? ". " + string.Join(". ", notes) : string.Empty));
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            var message = ex is RexxHubException or RexxHubFormatException ? ex.Message : $"{ex.GetType().Name}: {ex.Message}";
            await _log.WriteAsync("HR", $"Rexx-Hub-Abruf {label} fehlgeschlagen", "Warning",
                details: "Die bisherige Datei bleibt stehen. " + RexxHubClient.Scrub(message, token ?? string.Empty));
            return false;
        }
    }
}

/// <summary>Bericht des Testabrufs: nur Status, Zaehlwerte und Spaltennamen, nie Werte und nie der Token.</summary>
public sealed record RexxHubProbeReport(
    bool Eingeschaltet,
    string Zugang,
    string Hinweis,
    List<RexxHubProbeQuery> Abfragen);

public sealed record RexxHubProbeQuery(
    string Abfrage,
    int QueryId,
    string Datei,
    bool Ok,
    int? HttpStatus,
    string? Fehler,
    string? RequestId,
    int Zeilen,
    int Seiten,
    double DauerSekunden,
    bool WuerdeGeschrieben,
    List<string> Spalten,
    List<string> FehlendePflichtspalten,
    List<string> FehlendeLeserspalten,
    List<string> ZusaetzlicheSpalten,
    List<string> DoppelteSpalten,
    List<RexxHubFormatCheck> Formatpruefung);

/// <summary>
/// Testabruf ohne Schreiben, auch bei ausgeschaltetem Schalter: damit laesst sich vor dem Umschalten pruefen, ob Zugang,
/// Freigabe und Spalten stimmen. Kostet je Abfrage mindestens eine Anfrage vom Monatskontingent (50'000).
/// </summary>
public sealed class RexxHubProbe(IRexxHubConnectionSource source, RexxHubClient client, IOptions<RexxHubOptions> options, IOptions<HrKpiDataSourceOptions> files)
{
    public async Task<RexxHubProbeReport> RunAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var fileNames = files.Value.Normalize();
        RexxHubConnection connection;
        try
        {
            connection = await source.ResolveAsync(cancellationToken);
        }
        catch (RexxHubException ex)
        {
            return new RexxHubProbeReport(settings.Enabled, ex.Message, "Kein Abruf, Zugang fehlt.", []);
        }

        var queries = new List<RexxHubProbeQuery>();
        foreach (var (kind, queryId, file) in new[]
                 {
                     (RexxHubQueryKind.Gesamt, settings.GesamtQueryId, fileNames.GesamtFile),
                     (RexxHubQueryKind.Faelle, settings.CaseQueryId, fileNames.RexxCaseFile)
                 })
        {
            try
            {
                var result = await client.FetchQueryAsync(connection, queryId, settings, cancellationToken);
                var check = RexxHubColumns.Check(kind, result.Table);
                queries.Add(new RexxHubProbeQuery(
                    $"#{queryId}", queryId, file, true, result.HttpStatus, check.KannSchreiben ? null : check.Ablehnungsgrund(),
                    result.RequestId, check.Zeilen, result.Pages, Math.Round(result.Duration.TotalSeconds, 1), check.KannSchreiben,
                    result.Table.Columns.ToList(), check.FehlendePflichtspalten, check.FehlendeLeserspalten, check.ZusaetzlicheSpalten,
                    check.DoppelteSpalten, check.Formatpruefung));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (ex is RexxHubException or RexxHubFormatException)
            {
                var hubError = ex as RexxHubException;
                queries.Add(new RexxHubProbeQuery(
                    $"#{queryId}", queryId, file, false, hubError?.StatusCode, RexxHubClient.Scrub(ex.Message, connection.Token),
                    hubError?.RequestId, 0, 0, 0, false, [], [], [], [], [], []));
            }
        }

        return new RexxHubProbeReport(
            settings.Enabled,
            $"Basis-URL {connection.BaseUrl}, Token gesetzt (aus Quellsystem {RexxHubOptions.SourceSystemCode}).",
            "Testabruf ohne Schreiben. Werte werden nie angezeigt, nur Spaltennamen und Zaehlwerte.",
            queries);
    }
}

/// <summary>Anmeldung der Dienste und des Testabrufs; in Program.cs je eine Zeile.</summary>
public static class RexxHubRegistration
{
    /// <summary>Testabruf, unter dem Pfad der App (produktiv also <c>/BiDashboard/admin/rexxhub/test</c>).</summary>
    public const string TestPath = "/admin/rexxhub/test";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static IServiceCollection AddRexxHub(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RexxHubOptions>(configuration.GetSection(RexxHubOptions.SectionName));
        services.AddSingleton<IRexxHubConnectionSource, RexxHubDbConnectionSource>();
        services.AddSingleton<RexxHubClient>();
        services.AddSingleton<RexxHubProbe>();
        services.AddHostedService<RexxHubRefreshService>();
        return services;
    }

    /// <summary>
    /// Zwei Huerden wie im Adminbereich: die Richtlinie <see cref="SecurityPolicies.AdminOnly"/> (wie Settings und Standorte)
    /// UND die Admin-Freischaltung per Cookie (wie Admin &gt; Passwoerter und Sitzungen, <see cref="IAdminAccessService"/>).
    /// Die Freischaltung ist noetig, weil <c>Security:Enabled</c> in appsettings.json auf false steht und die Richtlinie dann
    /// jeden durchlaesst (<c>SecurityPolicyFactory.BuildAdminPolicy</c>); ein Testabruf kostet Anfragen vom Rexx-Kontingent.
    /// </summary>
    public static IEndpointRouteBuilder MapRexxHubTest(this IEndpointRouteBuilder app)
    {
        app.MapGet(TestPath, async Task<IResult> (HttpContext context, RexxHubProbe probe) =>
            {
                context.Response.Headers.CacheControl = "no-store";
                var admin = context.RequestServices.GetRequiredService<IAdminAccessService>();
                if (admin.IsEnabled && !admin.IsUnlocked)
                    return Results.Json(new
                    {
                        Fehler = "Admin-Freischaltung fehlt. Zuerst unter Admin > Passwoerter (admin/passwoerter) mit dem Admin-Login entsperren, dann diese Adresse neu laden."
                    }, JsonOptions, statusCode: StatusCodes.Status403Forbidden);
                var report = await probe.RunAsync(context.RequestAborted);
                return Results.Json(report, JsonOptions);
            })
            .RequireAuthorization(SecurityPolicies.AdminOnly);
        return app;
    }
}
