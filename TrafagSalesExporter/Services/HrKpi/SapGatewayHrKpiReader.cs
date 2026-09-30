using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

/// <summary>Eine Zeile aus HrKpiSet: eine Person im Stichtagsmonat, ohne Namen und ohne Lohn.</summary>
public sealed record HrKpiSapRow(
    string Personalnummer,
    string Geschaeftsjahr,
    string Monat,
    string Buchungskreis,
    string Personalbereich,
    string Personalteilbereich,
    string Mitarbeitergruppe,
    string Mitarbeiterkreis,
    string Teilzeitkennzeichen,
    decimal Beschaeftigungsgrad,
    string Geschlecht,
    string Planstelle,
    string Stellenschluessel,
    decimal NbuTage,
    decimal BuTage,
    string Abrechnungskreis);

/// <summary>
/// Liest die SAP-HR-Felder des HR-Cockpits ueber OData statt aus der von Hand exportierten
/// <c>HR_KPI_Export.xlsx</c> und schreibt daraus genau diese Datei.
///
/// WARUM EINE DATEI UND KEIN DIREKTER EINBAU IN DEN BUILDER: Das HR-Cockpit liest alle Quellen
/// aus einem Ordner, zeigt je Datei die Frische an und laesst HR eine Datei von Hand ersetzen.
/// Schreibt der Abruf dieselbe Datei, bleibt all das unveraendert, und faellt SAP aus, rechnet
/// das Cockpit mit dem letzten Stand weiter.
///
/// Herkunft: EntitySet <see cref="EntitySet"/> in <c>ZPOWERBI_EINKAUF_SRV</c>, gleiche Rechnung
/// wie der Report <c>Z_HR_KPI_CONS</c>, aber nur die Felder, die
/// <c>HrKpiDashboardBuilder.LoadSapRows</c> liest (Entscheid Ingo 2026-09-30). ABAP-Quelle:
/// <c>docs/abap/ZHR_KPI_*_ADD.abap</c>, Stand und Fallen: <c>docs/HR_KPI.md</c> 8.6.
/// </summary>
public class SapGatewayHrKpiReader
{
    public const string EntitySet = "HrKpiSet";

    /// <summary>
    /// Obergrenze fuer das Blaettern. Trafag CH hat rund 260 Personen; bei 1000 je Seite reicht
    /// eine Seite. Die Grenze verhindert eine Endlosschleife wie am 2026-08-19 bei MARCSet, falls
    /// ein Set Paging still ignoriert.
    /// </summary>
    internal const int PageSize = 1000;
    internal const int MaxPages = 20;

    /// <summary>
    /// Spaltenkoepfe der geschriebenen Datei. Es sind die Namen, die der Report im CSV verwendet
    /// und die <c>LoadSapRows</c> als Alias kennt; die Datei ist damit gegen den Leser getestet.
    /// </summary>
    internal static readonly string[] Headers =
    [
        "Personalnummer", "Geschaeftsjahr", "Buchungsperiode", "Buchungskreis", "Personalbereich",
        "Personalteilbereich", "Mitarbeitergruppe", "Mitarbeiterkreis", "Teilzeitkennzeichen",
        "Beschaeftigungsgrad_Prozent", "Geschlecht", "Planstelle", "Stellenschluessel",
        "NBU_Tage", "BU_Tage", "Abrechnungskreis"
    ];

    public async Task<IReadOnlyList<HrKpiSapRow>> ReadAsync(
        string serviceUrl, string username, string password, int year, int month,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = serviceUrl.TrimEnd('/') + "/";
        using var client = CreateClient(username, password);
        var rows = new List<HrKpiSapRow>();

        for (var page = 0; page < MaxPages; page++)
        {
            var url = BuildPageUrl(baseUrl, year, month, page * PageSize);
            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"SAP OData {EntitySet} fehlgeschlagen ({(int)response.StatusCode} {response.ReasonPhrase}). " +
                    (response.StatusCode == HttpStatusCode.NotFound
                        ? "Das EntitySet ist im Zielsystem vermutlich noch nicht transportiert. "
                        : string.Empty) +
                    $"URL={url} Antwort={(body.Length <= 500 ? body : body[..500])}",
                    null, response.StatusCode);
            }

            var pageRows = ParseRows(await response.Content.ReadAsStringAsync(cancellationToken));
            rows.AddRange(pageRows);
            if (pageRows.Count < PageSize)
                return rows;
        }

        throw new InvalidOperationException(
            $"{EntitySet} lieferte nach {MaxPages} Seiten immer noch volle Seiten. Abbruch, weil das Set " +
            "das Paging vermutlich ignoriert.");
    }

    internal static string BuildPageUrl(string baseUrl, int year, int month, int skip)
    {
        var filter = $"Gjahr eq '{year:0000}' and Monat eq '{month:00}'";
        return $"{baseUrl}{EntitySet}?$format=json&$top={PageSize}&$skip={skip}&$filter={Uri.EscapeDataString(filter)}";
    }

    internal static List<HrKpiSapRow> ParseRows(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("d", out var d) ||
            !d.TryGetProperty("results", out var results) ||
            results.ValueKind != JsonValueKind.Array)
            return [];

        return results.EnumerateArray()
            .Select(item => new HrKpiSapRow(
                Text(item, "Pernr"), Text(item, "Gjahr"), Text(item, "Monat"), Text(item, "Bukrs"),
                Text(item, "Werks"), Text(item, "Btrtl"), Text(item, "Persg"), Text(item, "Persk"),
                Text(item, "Teilk"), Number(item, "Empct"), Text(item, "Gesch"), Text(item, "Plans"),
                Text(item, "Stell"), Number(item, "NbuTage"), Number(item, "BuTage"), Text(item, "Abkrs")))
            .Where(row => !string.IsNullOrWhiteSpace(row.Personalnummer))
            .ToList();
    }

    /// <summary>
    /// Schreibt die Datei zuerst unter einem temporaeren Namen und ersetzt dann die alte. Ein
    /// halb geschriebenes Excel wuerde das Cockpit sonst beim naechsten Oeffnen leer rechnen.
    /// </summary>
    internal static void WriteWorkbook(IReadOnlyList<HrKpiSapRow> rows, string path)
    {
        // ClosedXML verlangt die Endung .xlsx, deshalb nicht einfach ".tmp" anhaengen.
        var tempPath = Path.Combine(Path.GetDirectoryName(path) ?? ".",
            Path.GetFileNameWithoutExtension(path) + ".tmp.xlsx");
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("HR_KPI");
            for (var column = 0; column < Headers.Length; column++)
                sheet.Cell(1, column + 1).Value = Headers[column];

            var line = 2;
            foreach (var row in rows)
            {
                object[] values =
                [
                    row.Personalnummer, row.Geschaeftsjahr, row.Monat, row.Buchungskreis, row.Personalbereich,
                    row.Personalteilbereich, row.Mitarbeitergruppe, row.Mitarbeiterkreis, row.Teilzeitkennzeichen,
                    row.Beschaeftigungsgrad, row.Geschlecht, row.Planstelle, row.Stellenschluessel,
                    row.NbuTage, row.BuTage, row.Abrechnungskreis
                ];
                for (var column = 0; column < values.Length; column++)
                {
                    var cell = sheet.Cell(line, column + 1);
                    if (values[column] is decimal number)
                        cell.Value = number;
                    else
                        cell.Value = (string)values[column];
                }
                line++;
            }

            workbook.SaveAs(tempPath);
        }

        File.Move(tempPath, path, overwrite: true);
    }

    private static string Text(JsonElement item, string name)
        => item.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null
            ? (value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString())?.Trim() ?? string.Empty
            : string.Empty;

    private static decimal Number(JsonElement item, string name)
        => decimal.TryParse(Text(item, name), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0m;

    private static HttpClient CreateClient(string username, string password)
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }
}

/// <summary>
/// Holt die SAP-HR-Felder einmal am Tag ab 05:00 und schreibt <c>hrdata/HR_KPI_Export.xlsx</c>.
/// Nur in Produktion, wie das Vorwaermen des Einkaufs.
///
/// WIRFT NIE: Solange das EntitySet in P76 nicht transportiert ist, antwortet SAP mit 404. Dann
/// bleibt die von Hand exportierte Datei stehen, und es gibt einen Eintrag im Ereignisprotokoll,
/// hoechstens einmal je Tag. Vor dem ersten Ueberschreiben wird die handgemachte Datei einmalig
/// als <c>HR_KPI_Export.manuell.xlsx</c> gesichert.
/// </summary>
public sealed class HrKpiSapRefreshService : BackgroundService
{
    /// <summary>Ab dieser Uhrzeit gilt ein Tag als faellig.</summary>
    internal static readonly TimeSpan DailyStart = TimeSpan.FromHours(5);

    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(30);

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IAppEventLogService _log;
    private readonly IHostEnvironment _environment;
    private readonly HrKpiDataSourceOptions _options;
    private readonly SapGatewayHrKpiReader _reader = new();
    private DateOnly _lastAttempt;

    public HrKpiSapRefreshService(
        IDbContextFactory<AppDbContext> dbFactory,
        IAppEventLogService log,
        IHostEnvironment environment,
        Microsoft.Extensions.Options.IOptions<HrKpiDataSourceOptions> options)
    {
        _dbFactory = dbFactory;
        _log = log;
        _environment = environment;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_environment.IsProduction())
            return;

        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var path = Path.Combine(_environment.ContentRootPath, "hrdata", _options.SapFile);
            if (IsDue(now, File.Exists(path) ? File.GetLastWriteTime(path) : null, _lastAttempt))
            {
                _lastAttempt = DateOnly.FromDateTime(now);
                await RefreshSafeAsync(path, now, stoppingToken);
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    /// <summary>Faellig, wenn es nach 05:00 ist, heute noch nicht versucht wurde und die Datei aelter ist als heute 05:00.</summary>
    internal static bool IsDue(DateTime now, DateTime? fileWrittenAt, DateOnly lastAttempt)
    {
        if (now.TimeOfDay < DailyStart)
            return false;
        if (lastAttempt == DateOnly.FromDateTime(now))
            return false;
        return fileWrittenAt is null || fileWrittenAt.Value < now.Date + DailyStart;
    }

    private async Task RefreshSafeAsync(string path, DateTime now, CancellationToken cancellationToken)
    {
        try
        {
            var (url, user, password) = await ResolveConnectionAsync(cancellationToken);
            var rows = await _reader.ReadAsync(url, user, password, now.Year, now.Month, cancellationToken);
            if (rows.Count == 0)
            {
                await _log.WriteAsync("HR", "SAP-HR-Abruf ohne Zeilen", "Warning",
                    details: $"{SapGatewayHrKpiReader.EntitySet} {now:yyyy-MM} lieferte keine Zeile; Datei bleibt unveraendert.");
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var manualBackup = Path.Combine(Path.GetDirectoryName(path)!, Path.GetFileNameWithoutExtension(path) + ".manuell.xlsx");
            if (File.Exists(path) && !File.Exists(manualBackup))
                File.Copy(path, manualBackup);

            SapGatewayHrKpiReader.WriteWorkbook(rows, path);
            await _log.WriteAsync("HR", "SAP-HR-Datei aus OData geschrieben",
                details: $"{rows.Count} Personen, Stichtag {now:yyyy-MM}, Datei {path}");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            await _log.WriteAsync("HR", "SAP-HR-Abruf fehlgeschlagen", "Warning",
                details: $"Die bisherige Datei bleibt stehen. {ex.Message}");
        }
    }

    /// <summary>Gleiche Verbindung wie der Einkauf, weil das EntitySet im selben Service liegt.</summary>
    private async Task<(string Url, string User, string Password)> ResolveConnectionAsync(CancellationToken cancellationToken)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var sap = await db.SourceSystemDefinitions.AsNoTracking().FirstOrDefaultAsync(x => x.Code == "SAP", cancellationToken)
            ?? throw new InvalidOperationException("SAP Quelle fehlt.");
        var site = await db.Sites.AsNoTracking().FirstOrDefaultAsync(x => x.TSC == PurchasingDataSourcePageService.PurchasingTsc, cancellationToken)
            ?? throw new InvalidOperationException("Einkauf SAP Site fehlt.");
        var url = string.IsNullOrWhiteSpace(site.SapServiceUrl) ? sap.CentralServiceUrl : site.SapServiceUrl;
        var user = string.IsNullOrWhiteSpace(site.UsernameOverride) ? sap.CentralUsername : site.UsernameOverride;
        var password = string.IsNullOrWhiteSpace(site.PasswordOverride) ? sap.CentralPassword : site.PasswordOverride;
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("SAP URL oder Zugangsdaten fehlen.");
        return (url, user, password);
    }
}
