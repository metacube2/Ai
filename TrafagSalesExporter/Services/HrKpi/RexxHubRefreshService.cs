using System.Globalization;
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
/// <c>HrKpi:DataFolder</c>), Schreiben ueber <c>.tmp.xlsx</c>, Protokoll ueber das Ereignisprotokoll (Kategorie HR).
/// Eine Datei am Zielpfad, die nicht vom Abruf stammt (Blattname nicht „Rexx775“/„Rexx777“), wird vor dem Ueberschreiben
/// gesichert: beim ersten Mal als <c>*.manuell.xlsx</c>, danach mit Zeitstempel, damit jeder spaetere Handexport erhalten bleibt.
///
/// SCHALTER: Nur mit <c>HrKpi:RexxHub:Enabled = true</c> und nur in Produktion. Aus: der Dienst endet sofort und ruft nichts ab.
/// WIRFT NIE: Bei Fehler, leerer Antwort oder nicht bestandener Pruefung (<see cref="RexxHubColumns.Check"/>, dieselbe wie im
/// Testabruf) bleibt die bisherige Datei stehen. Ins Protokoll kommen Zeilenzahl, Spaltennamen, Zaehlwerte, Dauer und
/// request id, nie Werte (Personendaten) und nie der Token.
/// Jede Datei hat ihren eigenen Faelligkeitsstempel (LEARNINGS 2026-10-01). Voruebergehende Fehler (429, 5xx, Netz) werden im
/// naechsten 30-Minuten-Durchgang wiederholt, hoechstens <c>MaxAttemptsPerDay</c> Mal je Datei und Tag.
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
    private readonly DueState _gesamtState = new();
    private readonly DueState _caseState = new();

    /// <summary>Faelligkeit je Datei: erledigt fuer heute, oder wie oft heute schon versucht.</summary>
    private sealed class DueState
    {
        public DateOnly DoneDay;
        public DateOnly AttemptDay;
        public int Attempts;
    }

    /// <summary>Ausgang eines Abrufs; nur ein voruebergehender Fehler wird heute noch einmal versucht.</summary>
    internal enum RefreshOutcome
    {
        Written,
        Rejected,
        Failed,
        Transient
    }

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

        await RunOneAsync(now, RexxHubQueryKind.Gesamt, Path.Combine(DataFolder, _files.GesamtFile), _gesamtState, cancellationToken);
        await RunOneAsync(now, RexxHubQueryKind.Faelle, Path.Combine(DataFolder, _files.RexxCaseFile), _caseState, cancellationToken);
    }

    private async Task RunOneAsync(DateTime now, RexxHubQueryKind kind, string path, DueState state, CancellationToken cancellationToken)
    {
        if (!IsDue(now, _options.DailyAt, LastWrite(path), state.DoneDay))
            return;
        var today = DateOnly.FromDateTime(now);
        if (state.AttemptDay != today)
        {
            state.AttemptDay = today;
            state.Attempts = 0;
        }
        state.Attempts++;
        var outcome = await RefreshCoreAsync(kind, path, cancellationToken);
        // Erledigt fuer heute, ausser bei einem voruebergehenden Fehler, solange Versuche uebrig sind (jeder Versuch holt alle
        // Seiten neu und zaehlt gegen das Monatskontingent).
        if (outcome != RefreshOutcome.Transient || state.Attempts >= Math.Max(1, _options.MaxAttemptsPerDay))
            state.DoneDay = today;
    }

    private static DateTime? LastWrite(string path) => File.Exists(path) ? File.GetLastWriteTime(path) : null;

    /// <summary>Faellig, wenn es nach <paramref name="dailyAt"/> ist, heute noch nicht erledigt ist und die Datei aelter ist.</summary>
    internal static bool IsDue(DateTime now, TimeSpan dailyAt, DateTime? fileWrittenAt, DateOnly doneDay)
    {
        if (now.TimeOfDay < dailyAt)
            return false;
        if (doneDay == DateOnly.FromDateTime(now))
            return false;
        return fileWrittenAt is null || fileWrittenAt.Value < now.Date + dailyAt;
    }

    /// <summary>Holt eine Abfrage, prueft sie und schreibt die Datei. true, wenn geschrieben wurde.</summary>
    internal async Task<bool> RefreshAsync(RexxHubQueryKind kind, string path, CancellationToken cancellationToken)
        => await RefreshCoreAsync(kind, path, cancellationToken) == RefreshOutcome.Written;

    private async Task<RefreshOutcome> RefreshCoreAsync(RexxHubQueryKind kind, string path, CancellationToken cancellationToken)
    {
        var queryId = kind == RexxHubQueryKind.Gesamt ? _options.GesamtQueryId : _options.CaseQueryId;
        var label = $"#{queryId}";
        string? token = null;
        try
        {
            var connection = await _source.ResolveAsync(cancellationToken);
            token = connection.Token;
            var result = await _client.FetchQueryAsync(connection, queryId, _options, cancellationToken);
            var existing = RexxHubWorkbook.Inspect(kind, path);
            var check = RexxHubColumns.Check(kind, result.Table, existing.Rows, _options.MinShare, _options.MinRowShareOfPrevious, _options.MaxDuplicatePersonalnummern);
            var requestId = string.IsNullOrWhiteSpace(result.RequestId) ? "keine" : result.RequestId;
            if (!check.KannSchreiben)
            {
                await _log.WriteAsync("HR", $"Rexx-Hub {label} nicht geschrieben", "Warning",
                    details: $"{check.Ablehnungsgrund()}. Die bisherige Datei bleibt stehen. Zeilen {check.Zeilen}, request id {requestId}.");
                return RefreshOutcome.Rejected;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var backup = existing.Exists && !existing.FromHub ? BackupPath(path, DateTime.Now) : null;
            if (backup is not null)
                File.Copy(path, backup);

            RexxHubWorkbook.Write(kind, result.Table, path);

            var notes = Notes(kind, check, connection, _options);
            if (backup is not null)
                notes.Add($"Handexport gesichert als {Path.GetFileName(backup)}");
            await _log.WriteAsync("HR", $"Rexx-Hub {label} geschrieben", notes.Any(n => !n.StartsWith("Handexport", StringComparison.Ordinal)) ? "Warning" : "Info",
                details: $"{result.Table.Rows.Count} Zeilen, {result.Table.Columns.Count} Spalten, {result.Pages} Seite(n), " +
                         $"{result.Duration.TotalSeconds:0.0} s, request id {requestId}, Datei {path}" +
                         (check.ZeilenBisher is { } before ? $", bisher {before} Zeilen" : string.Empty) +
                         (notes.Count > 0 ? ". " + string.Join(". ", notes) : string.Empty));
            return RefreshOutcome.Written;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            var transient = ex is RexxHubException { Transient: true } || ex is not (RexxHubException or RexxHubFormatException);
            var message = ex is RexxHubException or RexxHubFormatException ? ex.Message : $"{ex.GetType().Name}: {ex.Message}";
            await _log.WriteAsync("HR", $"Rexx-Hub-Abruf {label} fehlgeschlagen", "Warning",
                details: "Die bisherige Datei bleibt stehen. " + RexxHubClient.Scrub(message, token ?? string.Empty) +
                         (transient ? " Voruebergehend: neuer Versuch im naechsten Durchgang (hoechstens " + Math.Max(1, _options.MaxAttemptsPerDay) + " je Tag)." : string.Empty));
            return transient ? RefreshOutcome.Transient : RefreshOutcome.Failed;
        }
    }

    /// <summary>Hinweise zu einem geschriebenen Abruf, nur Namen und Zaehlwerte; auch fuer den Testabruf.</summary>
    internal static List<string> Notes(RexxHubQueryKind kind, RexxHubColumnCheck check, RexxHubConnection connection, RexxHubOptions options)
    {
        var notes = new List<string>();
        var optionalMissing = check.FehlendeLeserspalten.Except(check.FehlendePflichtspalten).ToList();
        if (optionalMissing.Count > 0)
            notes.Add("Spalten fehlen (keine Pflicht, Kennzahl bleibt leer): " + string.Join("; ", optionalMissing));
        var deviations = check.Formatabweichungen.ToList();
        if (deviations.Count > 0)
            notes.Add("Format nicht erkannt (Datum leer geschrieben, sonst Text): " + string.Join("; ", deviations));
        notes.AddRange(check.Hinweise);
        if (kind == RexxHubQueryKind.Gesamt && !options.GesamtZeitraumGeprueft)
            notes.Add(GesamtPeriodNote);
        if (connection.BaseUrl.TrimStart().StartsWith("http:", StringComparison.OrdinalIgnoreCase))
            notes.Add("Basis-URL ist http (HrKpi:RexxHub:AllowHttp): Token und HR-Daten gehen unverschluesselt");
        return notes;
    }

    /// <summary>Hinweis zum Zeitraumfilter von #775, bis HrKpi:RexxHub:GesamtZeitraumGeprueft gesetzt ist.</summary>
    internal const string GesamtPeriodNote =
        "Zeitraum von #775 nicht bestaetigt: der gespeicherte Filter stand am 04.11. fest auf 01.01. bis 04.11.2026 (docs/HR_KPI.md 8.13). " +
        "Gilt er auch ueber den Hub, frieren die Summen „angetreten“ ein, das Cockpit zeigt aber das Dateidatum als Stand. In Rexx einen " +
        "Zeitraum bis heute einstellen oder ueber QueryParameters ({jahresanfang}, {heute}) uebergeben, dann HrKpi:RexxHub:GesamtZeitraumGeprueft = true";

    /// <summary>Sicherung eines Handexports: zuerst <c>*.manuell.xlsx</c>, danach <c>*.manuell.yyyyMMdd-HHmmss.xlsx</c>.</summary>
    internal static string BackupPath(string path, DateTime now)
    {
        var folder = Path.GetDirectoryName(path)!;
        var name = Path.GetFileNameWithoutExtension(path);
        var first = Path.Combine(folder, name + ".manuell.xlsx");
        if (!File.Exists(first))
            return first;
        var stamped = Path.Combine(folder, $"{name}.manuell.{now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}.xlsx");
        for (var i = 2; File.Exists(stamped); i++)
            stamped = Path.Combine(folder, $"{name}.manuell.{now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}-{i}.xlsx");
        return stamped;
    }
}

/// <summary>Bericht des Testabrufs: nur Status, Zaehlwerte und Spaltennamen, nie Werte und nie der Token.</summary>
public sealed record RexxHubProbeReport(
    bool Eingeschaltet,
    string Zugang,
    string Hinweis,
    List<RexxHubProbeQuery> Abfragen);

/// <summary>Eine Abfrage im Testabruf. „WuerdeGeschrieben“ folgt derselben Pruefung wie der Tagesdienst.</summary>
public sealed class RexxHubProbeQuery
{
    public string Abfrage { get; init; } = string.Empty;
    public int QueryId { get; init; }
    public string Datei { get; init; } = string.Empty;
    public bool Ok { get; init; }
    public int? HttpStatus { get; init; }
    public string? Fehler { get; init; }
    public string? RequestId { get; init; }
    public int Zeilen { get; init; }
    public int Seiten { get; init; }
    /// <summary>Zeilen der ersten Seite und Zeilen je Seite laut „_meta“, fuer „Zeilenanzahl pro Seite“ in Rexx.</summary>
    public int ZeilenErsteSeite { get; init; }
    public int? ZeilenJeSeiteLautMeta { get; init; }
    /// <summary>Schluesselnamen von „_meta“ und „_links“ der ersten Seite (nie Werte), um das Blaettern vor dem Umschalten zu pruefen.</summary>
    public IReadOnlyList<string> MetaSchluessel { get; init; } = [];
    public IReadOnlyList<string> LinksSchluessel { get; init; } = [];
    public double DauerSekunden { get; init; }
    public bool WuerdeGeschrieben { get; init; }
    public int? ZeilenBisherigeDatei { get; init; }
    public List<string> Spalten { get; init; } = [];
    public List<string> FehlendePflichtspalten { get; init; } = [];
    public List<string> FehlendeLeserspalten { get; init; } = [];
    public List<string> ZusaetzlicheSpalten { get; init; } = [];
    public List<string> DoppelteSpalten { get; init; } = [];
    public List<RexxHubFormatCheck> Formatpruefung { get; init; } = [];
    public List<RexxHubFormatCheck> Wertpruefung { get; init; } = [];
    public int DoppeltePersonalnummern { get; init; }
    public int DatumswerteMitZeitanteil { get; init; }
    public List<string> Hinweise { get; init; } = [];
}

/// <summary>
/// Testabruf ohne Schreiben, auch bei ausgeschaltetem Schalter: damit laesst sich vor dem Umschalten pruefen, ob Zugang,
/// Freigabe, Blaettern, Spalten und Werte stimmen. Liest die bisherige Datei nur, um ihre Zeilen zu zaehlen. Kostet je
/// Abfrage mindestens eine Anfrage vom Monatskontingent (50'000).
/// </summary>
public sealed class RexxHubProbe(
    IRexxHubConnectionSource source, RexxHubClient client, IOptions<RexxHubOptions> options, IOptions<HrKpiDataSourceOptions> files,
    IHostEnvironment environment)
{
    public async Task<RexxHubProbeReport> RunAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var fileNames = files.Value.Normalize();
        var folder = Path.Combine(environment.ContentRootPath, "hrdata");
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
                var existing = RexxHubWorkbook.Inspect(kind, Path.Combine(folder, file));
                var check = RexxHubColumns.Check(kind, result.Table, existing.Rows, settings.MinShare, settings.MinRowShareOfPrevious, settings.MaxDuplicatePersonalnummern);
                queries.Add(new RexxHubProbeQuery
                {
                    Abfrage = $"#{queryId}", QueryId = queryId, Datei = file, Ok = true, HttpStatus = result.HttpStatus,
                    Fehler = check.KannSchreiben ? null : check.Ablehnungsgrund(), RequestId = result.RequestId,
                    Zeilen = check.Zeilen, Seiten = result.Pages, ZeilenErsteSeite = result.FirstPageRows, ZeilenJeSeiteLautMeta = result.PageSize,
                    MetaSchluessel = result.MetaKeys, LinksSchluessel = result.LinkKeys,
                    DauerSekunden = Math.Round(result.Duration.TotalSeconds, 1), WuerdeGeschrieben = check.KannSchreiben,
                    ZeilenBisherigeDatei = existing.Rows, Spalten = result.Table.Columns.ToList(),
                    FehlendePflichtspalten = check.FehlendePflichtspalten, FehlendeLeserspalten = check.FehlendeLeserspalten,
                    ZusaetzlicheSpalten = check.ZusaetzlicheSpalten, DoppelteSpalten = check.DoppelteSpalten,
                    Formatpruefung = check.Formatpruefung, Wertpruefung = check.Wertpruefung,
                    DoppeltePersonalnummern = check.DoppeltePersonalnummern, DatumswerteMitZeitanteil = check.DatumswerteMitZeitanteil,
                    Hinweise = RexxHubRefreshService.Notes(kind, check, connection, settings)
                });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (ex is RexxHubException or RexxHubFormatException)
            {
                var hubError = ex as RexxHubException;
                queries.Add(new RexxHubProbeQuery
                {
                    Abfrage = $"#{queryId}", QueryId = queryId, Datei = file, Ok = false, HttpStatus = hubError?.StatusCode,
                    Fehler = RexxHubClient.Scrub(ex.Message, connection.Token), RequestId = hubError?.RequestId
                });
            }
        }

        return new RexxHubProbeReport(
            settings.Enabled,
            $"Basis-URL {connection.BaseUrl}, Token gesetzt (aus Quellsystem {RexxHubOptions.SourceSystemCode}).",
            "Testabruf ohne Schreiben. Werte werden nie angezeigt, nur Spaltennamen und Zaehlwerte. „WuerdeGeschrieben“ folgt derselben " +
            "Pruefung wie der Tagesabruf. Vor dem Umschalten pruefen: Seiten, ZeilenJeSeiteLautMeta, MetaSchluessel/LinksSchluessel " +
            "(Blaettern erkannt?), Wertpruefung (Status und Arten als Rexx-Texte, kein Auswahlwerte-Mapping), Hinweise.",
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
