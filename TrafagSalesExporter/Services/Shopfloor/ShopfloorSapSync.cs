using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TrafagSalesExporter.Data;

namespace TrafagSalesExporter.Services.Shopfloor;

/// <summary>
/// Operations (Shopfloor), 2026-10-07: holt, was das PPA-Shopfloor-Excel bisher von Hand bekam, aus SAP.
/// ZD05 (Fehlteile Einkauf) aus ShopZd05Set je Disponent, Plan- und Fertigungsauftraege fuer den Forecast aus
/// ShopAufSet. Beides geht ueber ShopfloorStore.ApplyPush, also dieselbe Logik wie der Excel-Import
/// (Bemerkung, Liefertermin und Code werden je Material uebernommen). Verbindung wie Logistik live und Einkauf.
/// ABAP: docs/abap/ZSHOP_ADD.abap, Transport T76K912718. Doku: docs/SHOPFLOOR_2026-10-07.md.
/// </summary>
public sealed class ShopfloorSapSync : BackgroundService
{
    public const string Zd05Set = "ShopZd05Set";
    public const string OrderSet = "ShopAufSet";

    private readonly IServiceProvider _services;
    private readonly ShopfloorStoreProvider _stores;
    private readonly IOptionsMonitor<ShopfloorOptions> _options;
    private readonly ILogger<ShopfloorSapSync> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DateTime _lastRunSlot = DateTime.MinValue;

    public ShopfloorSapSync(IServiceProvider services, ShopfloorStoreProvider stores,
        IOptionsMonitor<ShopfloorOptions> options, ILogger<ShopfloorSapSync> logger)
    {
        _services = services;
        _stores = stores;
        _options = options;
        _logger = logger;
    }

    /// <summary>Letztes Ergebnis fuer die Statusabfrage.</summary>
    public JsonObject LastResult { get; private set; } = new() { ["ok"] = false, ["meldung"] = "noch nicht gelaufen" };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var options = _options.CurrentValue;
                var slot = DueSlot(options.SapSyncTimes, DateTime.Now, _lastRunSlot);
                if (options.SapSyncEnabled && slot is { } due)
                {
                    _lastRunSlot = due;
                    await RunAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Shopfloor: SAP-Abgleich fehlgeschlagen");
            }
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    /// <summary>Faelliger Abgleichzeitpunkt an Werktagen, der noch nicht gelaufen ist; sonst null.</summary>
    internal static DateTime? DueSlot(IEnumerable<string> times, DateTime now, DateTime lastRun)
    {
        if (now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return null;
        DateTime? due = null;
        foreach (var t in times)
        {
            if (!TimeOnly.TryParseExact(t.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
                continue;
            var slot = now.Date + time.ToTimeSpan();
            if (slot <= now && slot > lastRun && (due is null || slot > due))
                due = slot;
        }
        return due;
    }

    /// <summary>Ein Abgleich jetzt (auch fuer den Knopf/Endpunkt). Nur einer gleichzeitig.</summary>
    public async Task<JsonObject> RunAsync(CancellationToken ct)
    {
        if (!await _gate.WaitAsync(0, ct))
            return new JsonObject { ["ok"] = false, ["meldung"] = "Abgleich laeuft bereits" };
        try
        {
            var options = _options.CurrentValue;
            var (baseUrl, user, password) = await ResolveConnectionAsync(ct);
            // ZD05 rechnet je Disponent rund 15 bis 20 s (T76, 2026-10-07); deshalb grosszuegiges Zeitlimit.
            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{password}")));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var zd05 = new List<JsonElement>();
            foreach (var dispo in options.Zd05Dispo.Where(d => !string.IsNullOrWhiteSpace(d)))
                zd05.AddRange(await ReadAsync(client, baseUrl, Zd05Set,
                    $"Werks eq '{options.SapPlant}' and Dispo eq '{dispo.Trim()}'", ct));

            var from = DateOnly.FromDateTime(DateTime.Today);
            var orders = await ReadAsync(client, baseUrl, OrderSet,
                $"Werks eq '{options.SapPlant}' and Datum eq '{from:yyyyMMdd}'", ct);
            var until = from.AddDays(Math.Clamp(options.ForecastDays, 1, 60));

            var push = new ShopfloorPush
            {
                Items =
                [
                    new JsonObject { ["module"] = "zd05", ["day"] = from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), ["rows"] = Zd05Rows(zd05) },
                    new JsonObject { ["module"] = "planauftraege", ["rows"] = OrderRows(orders, until) }
                ]
            };
            var results = _stores.Store.ApplyPush(push, "SAP");
            LastResult = new JsonObject
            {
                ["ok"] = true,
                ["zeit"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                ["zd05_positionen"] = zd05.Count,
                ["auftraege"] = orders.Count,
                ["ergebnis"] = results
            };
            _logger.LogInformation("Shopfloor: SAP-Abgleich {Zd05} ZD05-Positionen, {Orders} Auftraege", zd05.Count, orders.Count);
            return LastResult;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            LastResult = new JsonObject
            {
                ["ok"] = false,
                ["zeit"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                ["meldung"] = ex.GetBaseException().Message
            };
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>ShopZd05Set -> Zeilen wie der ZD05-Excel-Import (Feldnamen aus zd05.py AUTO).</summary>
    internal static JsonArray Zd05Rows(IEnumerable<JsonElement> rows)
    {
        var result = new JsonArray();
        foreach (var x in rows)
        {
            var material = Material(Text(x, "Matnr"));
            if (material.Length == 0) continue;
            result.Add(new JsonObject
            {
                ["material"] = material,
                ["text"] = Text(x, "Maktx"),
                ["unterdeck"] = IsoDate(Text(x, "Udek")),
                ["dmk"] = Text(x, "Dismm"),
                ["lzcode"] = Text(x, "Lzcode"),
                ["verbr_wbz"] = Int(x, "VerbrWbz"),
                ["opt_sibe"] = Int(x, "SibeOpt"),
                ["akt_sibe"] = Int(x, "SibeAkt"),
                ["ant_plan"] = Int(x, "AntPlan")
            });
        }
        return result;
    }

    /// <summary>ShopAufSet -> Planauftraege fuer den Forecast (Abteilung ueber den Disponenten, forecast.py).</summary>
    internal static JsonArray OrderRows(IEnumerable<JsonElement> rows, DateOnly until)
    {
        var result = new JsonArray();
        foreach (var x in rows)
        {
            var day = IsoDate(Text(x, "Datum"));
            if (day.Length == 0 || string.CompareOrdinal(day, until.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)) > 0)
                continue;
            result.Add(new JsonObject
            {
                ["day"] = day,
                ["auftrag"] = Text(x, "Aufnr").TrimStart('0'),
                ["material"] = Material(Text(x, "Matnr")),
                ["menge"] = Int(x, "Menge"),
                ["disponent"] = Text(x, "Dispo"),
                ["arbeitsplatz"] = string.Empty
            });
        }
        return result;
    }

    private static string Material(string matnr)
    {
        var m = matnr.Trim();
        return m.All(char.IsDigit) ? m.TrimStart('0') : m;
    }

    internal static string IsoDate(string yyyymmdd)
        => yyyymmdd.Length == 8 && yyyymmdd != "00000000" &&
           DateOnly.TryParseExact(yyyymmdd, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : string.Empty;

    private static string Text(JsonElement x, string name)
        => x.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()?.Trim() ?? string.Empty : string.Empty;

    private static int Int(JsonElement x, string name)
        => x.TryGetProperty(name, out var v)
            ? v.ValueKind switch
            {
                JsonValueKind.Number when v.TryGetInt32(out var i) => i,
                JsonValueKind.String when int.TryParse(v.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var s) => s,
                _ => 0
            }
            : 0;

    private static async Task<List<JsonElement>> ReadAsync(HttpClient client, string baseUrl, string set, string filter, CancellationToken ct)
    {
        const int pageSize = 5000;
        var rows = new List<JsonElement>();
        for (var page = 0; page < 10; page++)
        {
            var url = $"{baseUrl.TrimEnd('/')}/{set}?$format=json&$top={pageSize}&$skip={page * pageSize}&$filter={Uri.EscapeDataString(filter)}";
            using var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"SAP OData {set} fehlgeschlagen ({(int)response.StatusCode} {response.ReasonPhrase})" +
                    (response.StatusCode == System.Net.HttpStatusCode.NotFound ? ", Set vermutlich noch nicht transportiert (T76K912718)." : "."));
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var pageRows = document.RootElement.TryGetProperty("d", out var d) && d.TryGetProperty("results", out var r) && r.ValueKind == JsonValueKind.Array
                ? r.EnumerateArray().Select(e => e.Clone()).ToList()
                : [];
            rows.AddRange(pageRows);
            if (pageRows.Count < pageSize)
                return rows;
        }
        throw new InvalidOperationException($"{set} lieferte zu viele Seiten; Abbruch.");
    }

    /// <summary>Gleiche Verbindung wie Logistik live und Einkauf (Sets im Service ZPOWERBI_EINKAUF_SRV).</summary>
    private async Task<(string Url, string User, string Password)> ResolveConnectionAsync(CancellationToken ct)
    {
        // Erst hier aufgeloest: Hosts ohne Cockpit-Datenbank (Tests) brauchen den Abgleich nicht.
        var factory = _services.GetService<IDbContextFactory<AppDbContext>>()
            ?? throw new InvalidOperationException("Cockpit-Datenbank nicht verfuegbar.");
        await using var db = await factory.CreateDbContextAsync(ct);
        var sap = await db.SourceSystemDefinitions.AsNoTracking().FirstOrDefaultAsync(x => x.Code == "SAP", ct)
            ?? throw new InvalidOperationException("SAP Quelle fehlt.");
        var site = await db.Sites.AsNoTracking().FirstOrDefaultAsync(x => x.TSC == PurchasingDataSourcePageService.PurchasingTsc, ct)
            ?? throw new InvalidOperationException("Einkauf SAP Site fehlt.");
        var url = string.IsNullOrWhiteSpace(site.SapServiceUrl) ? sap.CentralServiceUrl : site.SapServiceUrl;
        var user = string.IsNullOrWhiteSpace(site.UsernameOverride) ? sap.CentralUsername : site.UsernameOverride;
        var password = string.IsNullOrWhiteSpace(site.PasswordOverride) ? sap.CentralPassword : site.PasswordOverride;
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("SAP URL oder Zugangsdaten fehlen.");
        return (url, user, password);
    }
}
