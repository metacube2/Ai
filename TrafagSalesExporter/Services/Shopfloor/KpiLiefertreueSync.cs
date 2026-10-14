using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace TrafagSalesExporter.Services.Shopfloor;

/// <summary>
/// Operations KPI, Liefertreue (2026-10-14): Speicher (Tabellen kpi_snapshot, kpi_exclusion in shopfloor.db) und Abruf aus SAP.
/// Quellen wie die Referenz von Fabio: C_SLSORDDELIVPERFANLYTSQRY_CDS (Auftraege) und C_DELIVITMPERFANLYTSQRY_CDS (Lieferungen),
/// Verbindung und technischer Benutzer wie Einkauf/Shopfloor. Monatlich ab Kalendertag KpiSyncDay fuer den Vormonat, solange
/// fuer diesen Monat noch kein SAP-Abzug vorliegt. Doku: docs/OPERATIONS_KPI_2026-10-14.md.
/// </summary>
public sealed class KpiLiefertreueSync : BackgroundService
{
    public const string Kind = "liefertreue";
    public const string SeedFile = "shopfloor/kpi/seed_liefertreue_2026-09.json";
    public const string SeedSource = "Referenzabzug Fabio (Fiori), Stand 13.10.2026";
    private const string SoService = "/sap/opu/odata/sap/C_SLSORDDELIVPERFANLYTSQRY_CDS/";
    private const string SoSet = "C_SLSORDDELIVPERFANLYTSQRY(P_ExchangeRateType='M',P_DisplayCurrency='CHF')/Results";
    private const string DlService = "/sap/opu/odata/sap/C_DELIVITMPERFANLYTSQRY_CDS/";
    private const string DlSet = "C_DELIVITMPERFANLYTSQRY(P_ShippedAsPlannedOffsetDays=0)/Results";

    /// <summary>Ausschluesse der Spezifikation (Abschnitt Ausschluesse und Stammdaten), Startstand der Tabelle.</summary>
    public static readonly LtExclusion[] DefaultExclusions =
    [
        new(0, "Kunde", "302906", "Trafag Spol. s.r.o. (CZ)", "konzerninterne Lieferungen"),
        new(0, "Kunde", "301497", "Trafag Spol. s.r.o. (CZ)", "konzerninterne Lieferungen"),
        new(0, "Kunde", "24687", "Trafag Spol. s.r.o. (CZ)", "konzerninterne Lieferungen"),
        new(0, "Kunde", "500731", "Trafag Controls India", "konzerninterne Lieferungen"),
        new(0, "Kunde", "301940", "Trafag Controls India", "konzerninterne Lieferungen"),
        new(0, "Kunde", "302215", "Trafag Controls India", "konzerninterne Lieferungen"),
        new(0, "Kunde", "301873", "Trafag Controls India", "konzerninterne Lieferungen"),
        new(0, "Kunde", "19914", "GFS Germany Gesellschaft für Sensorik mbH", "Vorgabe Supply Chain Operations")
    ];

    private readonly IServiceProvider _services;
    private readonly ShopfloorStoreProvider _stores;
    private readonly IOptionsMonitor<ShopfloorOptions> _options;
    private readonly IHostEnvironment _env;
    private readonly ILogger<KpiLiefertreueSync> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private JsonObject? _last;
    private int _seeded;

    public KpiLiefertreueSync(IServiceProvider services, ShopfloorStoreProvider stores, IOptionsMonitor<ShopfloorOptions> options,
        IHostEnvironment env, ILogger<KpiLiefertreueSync> logger)
    {
        _services = services;
        _stores = stores;
        _options = options;
        _env = env;
        _logger = logger;
    }

    public bool Running { get; private set; }

    /// <summary>
    /// Letzter Abruf: laufend oder aus diesem Prozess, sonst der juengste gespeicherte Versuch (uebersteht einen Neustart).
    /// Ohne Versuch neutral mit ok = null, damit die Seite keinen Fehler vortaeuscht.
    /// </summary>
    public JsonObject LastResult
    {
        get => _last ?? LastAttempt() ?? new JsonObject { ["ok"] = null, ["meldung"] = "Noch kein Abruf gespeichert" };
        private set => _last = value;
    }

    private ShopfloorDb Db => _stores.Store.Db;

    /// <summary>Berichtsmonat (JJJJMM) = Vormonat von <paramref name="today"/>.</summary>
    public static string PreviousMonth(DateOnly today)
        => today.AddMonths(-1).ToString("yyyyMM", CultureInfo.InvariantCulture);

    // ------------------------------------------------------------------ Speicher

    /// <summary>Startstand: Ausschlusstabelle aus der Spezifikation, Referenzabzug Jan-Sep 2026 als erster Stand.</summary>
    public void EnsureSeed()
    {
        if (Interlocked.Exchange(ref _seeded, 1) == 1) return;
        var seedPath = Path.Combine(_env.ContentRootPath, "wwwroot", SeedFile.Replace('/', Path.DirectorySeparatorChar));
        Db.Write(con =>
        {
            if (ShopfloorDb.QueryOne(con, "SELECT COUNT(*) AS n FROM kpi_exclusion")!.Long("n") == 0)
                foreach (var e in DefaultExclusions)
                    ShopfloorDb.Exec(con, "INSERT INTO kpi_exclusion (typ, schluessel, bezeichnung, grund, updated_at, updated_by) VALUES (?,?,?,?,?,?)",
                        e.Typ, e.Schluessel, e.Bezeichnung, e.Grund, ShopfloorDb.Now(), "Spezifikation");
            if (ShopfloorDb.QueryOne(con, "SELECT COUNT(*) AS n FROM kpi_snapshot WHERE kind=?", Kind)!.Long("n") == 0 && File.Exists(seedPath))
            {
                var lt = JsonNode.Parse(File.ReadAllText(seedPath, Encoding.UTF8)) as JsonObject;
                if (lt?["cut"] is not null)
                    ShopfloorDb.Exec(con, "INSERT INTO kpi_snapshot (kind, cut, data, source, created_at, created_by) VALUES (?,?,?,?,?,?)",
                        Kind, lt["cut"]!.GetValue<string>(), lt.ToJsonString(), SeedSource, ShopfloorDb.Now(), "Startstand");
            }
            return 0;
        });
    }

    public List<LtExclusion> Exclusions()
    {
        EnsureSeed();
        using var con = Db.Open();
        return ShopfloorDb.Query(con, "SELECT * FROM kpi_exclusion ORDER BY typ, schluessel")
            .Select(r => new LtExclusion(r.Long("id"), r.Str("typ") ?? "", r.Str("schluessel") ?? "", r.Str("bezeichnung") ?? "", r.Str("grund") ?? ""))
            .ToList();
    }

    public void SaveExclusion(JsonObject body, string user)
    {
        var typ = SfJson.GetString(body, "typ")?.Trim() ?? "";
        var key = SfJson.GetString(body, "schluessel")?.Trim() ?? "";
        if (typ is not ("Kunde" or "Material"))
            throw new ShopfloorException("Typ muss Kunde oder Material sein.");
        if (key.Length == 0)
            throw new ShopfloorException("Kunden- bzw. Materialnummer fehlt.");
        EnsureSeed();
        Db.Write(con => ShopfloorDb.Exec(con,
            "INSERT INTO kpi_exclusion (typ, schluessel, bezeichnung, grund, updated_at, updated_by) VALUES (?,?,?,?,?,?)",
            typ, key, SfJson.GetString(body, "bezeichnung")?.Trim() ?? "", SfJson.GetString(body, "grund")?.Trim() ?? "", ShopfloorDb.Now(), user));
    }

    public void DeleteExclusion(long id) => Db.Write(con => ShopfloorDb.Exec(con, "DELETE FROM kpi_exclusion WHERE id=?", id));

    /// <summary>Juengster Stand (hoechster Berichtsmonat) oder ein bestimmter Monat.</summary>
    public (JsonObject Data, string Source, string CreatedAt)? Snapshot(string? cut = null)
    {
        EnsureSeed();
        using var con = Db.Open();
        var row = string.IsNullOrWhiteSpace(cut)
            ? ShopfloorDb.QueryOne(con, "SELECT * FROM kpi_snapshot WHERE kind=? ORDER BY cut DESC LIMIT 1", Kind)
            : ShopfloorDb.QueryOne(con, "SELECT * FROM kpi_snapshot WHERE kind=? AND cut=?", Kind, cut.Trim());
        if (row is null || JsonNode.Parse(row.Str("data") ?? "{}") is not JsonObject data) return null;
        return (data, row.Str("source") ?? "", row.Str("created_at") ?? "");
    }

    public JsonArray SnapshotList()
    {
        EnsureSeed();
        using var con = Db.Open();
        var arr = new JsonArray();
        foreach (var r in ShopfloorDb.Query(con, "SELECT cut, source, created_at, created_by FROM kpi_snapshot WHERE kind=? ORDER BY cut DESC", Kind))
            arr.Add(new JsonObject { ["cut"] = r.Str("cut"), ["source"] = r.Str("source"), ["created_at"] = r.Str("created_at"), ["created_by"] = r.Str("created_by") });
        return arr;
    }

    private bool HasSapSnapshot(string cut)
    {
        EnsureSeed();
        using var con = Db.Open();
        return ShopfloorDb.QueryOne(con, "SELECT COUNT(*) AS n FROM kpi_snapshot WHERE kind=? AND cut=? AND source LIKE 'SAP%'", Kind, cut)!.Long("n") > 0;
    }

    // ------------------------------------------------------------------ Abrufprotokoll (seit 2026-10-14, Wunsch Fabio)

    /// <summary>Abstand zwischen zwei automatischen Versuchen, solange der Vormonat fehlt.</summary>
    public static readonly TimeSpan RetryInterval = TimeSpan.FromHours(2);

    /// <summary>Juengster gespeicherter Abrufversuch (uebersteht einen Neustart), sonst null.</summary>
    public JsonObject? LastAttempt()
    {
        EnsureSeed();
        using var con = Db.Open();
        var row = ShopfloorDb.QueryOne(con, "SELECT data FROM kpi_run WHERE kind=? ORDER BY id DESC LIMIT 1", Kind);
        return row is null ? null : JsonNode.Parse(row.Str("data") ?? "{}") as JsonObject;
    }

    private DateTime? LastAttemptAt(string cut)
    {
        using var con = Db.Open();
        var row = ShopfloorDb.QueryOne(con, "SELECT started_at FROM kpi_run WHERE kind=? AND cut=? ORDER BY id DESC LIMIT 1", Kind, cut);
        return DateTime.TryParse(row?.Str("started_at"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
    }

    private void SaveAttempt(string cut, DateTime started, bool ok, JsonObject result, string user)
        => Db.Write(con => ShopfloorDb.Exec(con,
            "INSERT INTO kpi_run (kind, cut, started_at, finished_at, ok, data, user) VALUES (?,?,?,?,?,?,?)",
            Kind, cut, started.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), ShopfloorDb.Now(), ok ? 1 : 0, result.ToJsonString(), user));

    /// <summary>
    /// Automatischer Abruf faellig: ab dem Stichtag, solange fuer den Vormonat kein SAP-Stand vorliegt und der letzte
    /// Versuch fuer diesen Monat mindestens <see cref="RetryInterval"/> zurueckliegt.
    /// </summary>
    internal static bool IsDue(DateTime now, int syncDay, bool hasSapSnapshot, DateTime? lastAttempt)
        => now.Day >= Math.Clamp(syncDay, 1, 28) && !hasSapSnapshot && (lastAttempt is null || now - lastAttempt.Value >= RetryInterval);

    /// <summary>Warnhinweis, wenn ab dem Tag nach dem Stichtag fuer den Vormonat noch kein SAP-Stand vorliegt; sonst null.</summary>
    public string? MissingWarning(DateTime now)
    {
        var o = _options.CurrentValue;
        var cut = PreviousMonth(DateOnly.FromDateTime(now));
        if (!o.KpiSyncEnabled || now.Day <= Math.Clamp(o.KpiSyncDay, 1, 28) || HasSapSnapshot(cut)) return null;
        var month = KpiLiefertreue.MonthLong[int.Parse(cut[4..], CultureInfo.InvariantCulture) - 1] + " " + cut[..4];
        var last = LastAttempt();
        var detail = last is null ? "Es wurde noch kein Abruf versucht."
            : $"Letzter Versuch {last["start"]?.GetValue<string>()}: {last["meldung"]?.GetValue<string>()}";
        return $"Für {month} liegt noch kein Stand aus SAP vor. Die Zahlen zeigen den letzten gültigen Monat. {detail}";
    }

    // ------------------------------------------------------------------ Zeitplan

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Nach dem Start etwas warten, damit der Abruf nicht mit dem Hochfahren der App zusammenfaellt.
        try { await Task.Delay(TimeSpan.FromMinutes(3), stoppingToken); } catch (OperationCanceledException) { return; }
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var o = _options.CurrentValue;
                var now = DateTime.Now;
                var cut = PreviousMonth(DateOnly.FromDateTime(now));
                if (o.KpiSyncEnabled && IsDue(now, o.KpiSyncDay, HasSapSnapshot(cut), LastAttemptAt(cut)))
                    await RunAsync(cut, "Monatsabruf", stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Operations KPI: Abruf Liefertreue fehlgeschlagen");
            }
            try { await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken); } catch (OperationCanceledException) { return; }
        }
    }

    /// <summary>Abruf im Hintergrund starten (Knopf). false, wenn schon einer laeuft.</summary>
    public bool Start(string? cut, string user)
    {
        if (Running) return false;
        var c = string.IsNullOrWhiteSpace(cut) ? PreviousMonth(DateOnly.FromDateTime(DateTime.Today)) : cut.Trim();
        if (c.Length != 6 || !c.All(char.IsAsciiDigit))
            throw new ShopfloorException("Berichtsmonat als JJJJMM angeben.");
        _ = Task.Run(async () =>
        {
            try { await RunAsync(c, user, CancellationToken.None); }
            catch (Exception ex) { _logger.LogWarning(ex, "Operations KPI: Abruf Liefertreue fehlgeschlagen"); }
        });
        return true;
    }

    // ------------------------------------------------------------------ Abruf

    public async Task<JsonObject> RunAsync(string cut, string user, CancellationToken ct)
    {
        if (!await _gate.WaitAsync(0, ct))
            return new JsonObject { ["ok"] = false, ["meldung"] = "Abruf laeuft bereits" };
        Running = true;
        var started = DateTime.Now;
        LastResult = new JsonObject { ["ok"] = false, ["laeuft"] = true, ["cut"] = cut, ["start"] = started.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture), ["meldung"] = "Abruf laeuft" };
        try
        {
            var o = _options.CurrentValue;
            var (serviceUrl, sapUser, password) = await ShopfloorSapSync.ResolveConnectionAsync(_services, ct);
            var root = GatewayRoot(o.KpiGatewayRoot, serviceUrl);
            var client0 = string.IsNullOrWhiteSpace(o.KpiSapClient) ? "100" : o.KpiSapClient.Trim();
            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{sapUser}:{password}")));
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var year = cut[..4];
            string So(string path) => $"{root}{SoService}{path}{(path.Contains('?') ? "&" : "?")}sap-client={client0}&$format=json";

            // Zertifikate und Trafag-Gesellschaften ueber die Value-Helps (wie die Referenz)
            var certs = (await ValueHelp(http, So, "Product", "Product_ID", "ProductText", "EN10204", ct))
                .Concat(await ValueHelp(http, So, "Product", "Product_ID", "ProductText", "EN 10204", ct)).Select(x => x.Key).ToList();
            var trafag = (await ValueHelp(http, So, "SoldToParty", "SoldToParty_ID", "SoldToPartyText", "rafag", ct))
                .Concat(await ValueHelp(http, So, "SoldToParty", "SoldToParty_ID", "SoldToPartyText", "RAFAG", ct)).Select(x => x.Key).ToList();

            const string sel = "SalesOrder,SalesOrderItem,SalesOrderDate,CommittedDeliveryDate,CmtdDelivDateYearMonth,ReqdDelivDateYearMonth," +
                               "EarliestReqdDelivDate,DeliveryStatus,SoldToParty,Product,ProductHierarchyNode,NetAmountInDisplayCurrency,DelivToCmtdDelivDelayInDays";
            var q1 = await ReadAll(http, So(SoSet + "?$select=" + sel + "&$filter=" + JsEncode($"SalesOrganization eq '1100' and CmtdDelivDateYear eq '{year}'")), "C_SLSORDDELIVPERFANLYTSQRY", ct);
            q1.AddRange(await ReadAll(http, So(SoSet + "?$select=" + sel + "&$filter=" + JsEncode($"SalesOrganization eq '1100' and ReqdDelivDateYear eq '{year}' and CmtdDelivDateYear ne '{year}'")), "C_SLSORDDELIVPERFANLYTSQRY", ct));
            var dlUrl = $"{root}{DlService}{DlSet}?sap-client={client0}&$format=json&$select=OutboundDelivery,OutboundDeliveryItem,ActualGoodsMovementDate,ReferenceSDDocument,ReferenceSDDocumentItem,NumberOfDeliveryItems&$filter="
                        + JsEncode($"ActualGoodsMovementDate ge '{int.Parse(year, CultureInfo.InvariantCulture) - 1}0901'");
            var q2 = await ReadAll(http, dlUrl, "C_DELIVITMPERFANLYTSQRY", ct);

            var texts = new Dictionary<string, string>(StringComparer.Ordinal);
            try
            {
                foreach (var r in await ReadAll(http, So("ProductHierarchyNode?$top=5000"), "ProductHierarchyNode", ct, single: true))
                    texts[Str(r, "ProductHierarchyNode_ID")] = Str(r, "ProductHierarchyNodeText");
            }
            catch (HttpRequestException) { /* Texte sind nur Anzeige */ }

            var lt = KpiLiefertreue.Compute(q1.Select(ToOrder), q2.Select(ToDelivery), cut, DateOnly.FromDateTime(DateTime.Today),
                Exclusions(), certs, trafag, texts);
            var (cus, prs) = KpiLiefertreue.ListKeys(lt);
            var names = (JsonObject)lt["names"]!;
            names["c"] = await Names(http, So, "SoldToParty", "SoldToParty_ID", "SoldToPartyText", cus, ct);
            names["p"] = await Names(http, So, "Product", "Product_ID", "ProductText", prs, ct);
            lt["log"] = new JsonArray($"certs {certs.Count}, trafag {trafag.Count}", $"so {q1.Count}", $"deliv {q2.Count}");

            var source = $"SAP {DateTime.Now:dd.MM.yyyy HH:mm} ({user})";
            Db.Write(con => ShopfloorDb.Exec(con, "INSERT OR REPLACE INTO kpi_snapshot (kind, cut, data, source, created_at, created_by) VALUES (?,?,?,?,?,?)",
                Kind, cut, lt.ToJsonString(), source, ShopfloorDb.Now(), user));
            var s = KpiLiefertreue.Summarize(lt);
            LastResult = new JsonObject
            {
                ["ok"] = true, ["cut"] = cut, ["start"] = started.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                ["dauer_s"] = (int)(DateTime.Now - started).TotalSeconds, ["auftragspositionen"] = q1.Count, ["lieferpositionen"] = q2.Count,
                ["otd"] = s.OtdTol, ["otif"] = s.OtifTol, ["otp"] = s.OtpTol,
                ["meldung"] = $"Abruf ok: {q1.Count} Auftragspositionen, {q2.Count} Lieferpositionen"
            };
            _logger.LogInformation("Operations KPI: Liefertreue {Cut} aus SAP, {Q1} Auftragspositionen, {Q2} Lieferpositionen, OTD {Otd}", cut, q1.Count, q2.Count, s.OtdTol);
            SaveAttempt(cut, started, true, LastResult, user);
            await EventLogAsync("Info", $"Liefertreue {cut} aus SAP geholt", LastResult["meldung"]!.GetValue<string>());
            return LastResult;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            LastResult = new JsonObject
            {
                ["ok"] = false, ["cut"] = cut, ["start"] = started.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                ["meldung"] = ex.GetBaseException().Message
            };
            // Gespeichert und in den Admin-Logs, damit ein Fehler einen Neustart uebersteht und auffindbar ist (Wunsch Fabio 2026-10-14)
            try
            {
                SaveAttempt(cut, started, false, LastResult, user);
                await EventLogAsync("Warning", $"Liefertreue-Abruf {cut} fehlgeschlagen",
                    $"{ex.GetBaseException().Message} Naechster automatischer Versuch in {RetryInterval.TotalHours:0} Stunden.");
            }
            catch (Exception logEx)
            {
                _logger.LogWarning(logEx, "Operations KPI: Abrufversuch konnte nicht protokolliert werden");
            }
            throw;
        }
        finally
        {
            Running = false;
            _gate.Release();
        }
    }

    private async Task EventLogAsync(string level, string message, string details)
    {
        // Erst hier aufgeloest: Hosts ohne Cockpit-Datenbank (Tests) haben keinen Eventlog-Dienst.
        if (_services.GetService<TrafagSalesExporter.Services.IAppEventLogService>() is { } log)
            await log.WriteAsync("Operations KPI", message, level, details: details);
    }

    /// <summary>Gateway-Wurzel: Einstellung oder alles vor /sap/opu/odata/ der Einkauf-Verbindung.</summary>
    internal static string GatewayRoot(string configured, string serviceUrl)
    {
        if (!string.IsNullOrWhiteSpace(configured)) return configured.Trim().TrimEnd('/');
        var i = serviceUrl.IndexOf("/sap/opu/odata/", StringComparison.OrdinalIgnoreCase);
        if (i > 0) return serviceUrl[..i];
        return new Uri(serviceUrl).GetLeftPart(UriPartial.Authority);
    }

    internal static LtOrderRow ToOrder(JsonElement r) => new(
        Str(r, "SalesOrder"), Str(r, "SalesOrderItem"), Str(r, "SalesOrderDate"), Str(r, "CommittedDeliveryDate"),
        Str(r, "CmtdDelivDateYearMonth"), Str(r, "ReqdDelivDateYearMonth"), Str(r, "EarliestReqdDelivDate"), Str(r, "DeliveryStatus"),
        Str(r, "SoldToParty"), Str(r, "Product"), Str(r, "ProductHierarchyNode"), JsNumber(r, "NetAmountInDisplayCurrency"), JsNumber(r, "DelivToCmtdDelivDelayInDays"));

    internal static LtDeliveryRow ToDelivery(JsonElement r)
        => new(Str(r, "ActualGoodsMovementDate"), Str(r, "ReferenceSDDocument"), Str(r, "ReferenceSDDocumentItem"));

    private static string Str(JsonElement r, string name)
        => r.TryGetProperty(name, out var v)
            ? v.ValueKind switch
            {
                JsonValueKind.String => v.GetString() ?? "",
                JsonValueKind.Number => v.GetRawText(),
                _ => ""
            }
            : "";

    /// <summary>Wie JavaScript "+wert": fehlt = NaN, leer = 0.</summary>
    private static double JsNumber(JsonElement r, string name)
    {
        if (!r.TryGetProperty(name, out var v)) return double.NaN;
        return v.ValueKind switch
        {
            JsonValueKind.Number => v.GetDouble(),
            JsonValueKind.String when string.IsNullOrWhiteSpace(v.GetString()) => 0,
            JsonValueKind.String => double.TryParse(v.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : double.NaN,
            JsonValueKind.Null => 0,
            _ => double.NaN
        };
    }

    /// <summary>Fehlertext aus der OData-Antwort (error.message.value), sonst die ersten Zeichen; fuer die Statusanzeige.</summary>
    internal static string SapMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return string.Empty;
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var e) && e.TryGetProperty("message", out var m))
            {
                var text = m.ValueKind == JsonValueKind.Object && m.TryGetProperty("value", out var v) ? v.GetString() : m.ToString();
                return string.IsNullOrWhiteSpace(text) ? string.Empty : " SAP: " + text.Trim();
            }
        }
        catch (JsonException)
        {
            // XML- oder HTML-Antwort: Tags entfernen
        }
        var plain = System.Text.RegularExpressions.Regex.Replace(body, "<[^>]+>", " ");
        plain = System.Text.RegularExpressions.Regex.Replace(plain, "\\s+", " ").Trim();
        return plain.Length == 0 ? string.Empty : " SAP: " + (plain.Length > 300 ? plain[..300] : plain);
    }

    /// <summary>Wie encodeURIComponent der Referenz: ' ( ) * ! bleiben stehen.</summary>
    internal static string JsEncode(string s)
        => Uri.EscapeDataString(s).Replace("%27", "'").Replace("%28", "(").Replace("%29", ")").Replace("%2A", "*").Replace("%21", "!");

    private static async Task<List<JsonElement>> ReadAll(HttpClient http, string url, string what, CancellationToken ct, bool single = false)
    {
        const int page = 10000;
        var rows = new List<JsonElement>();
        for (var skip = 0; ; skip += page)
        {
            var u = single ? url : $"{url}&$top={page}&$skip={skip}";
            using var response = await http.GetAsync(u, ct);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"SAP {what}: HTTP {(int)response.StatusCode} {response.ReasonPhrase}" + response.StatusCode switch
                {
                    HttpStatusCode.Forbidden => " (dem technischen Benutzer fehlt die Berechtigung fuer diesen Service, S_SERVICE).",
                    HttpStatusCode.NotFound => " (Service im Gateway nicht aktiviert).",
                    HttpStatusCode.Unauthorized => " (Anmeldung abgelehnt).",
                    _ => "."
                } + SapMessage(await response.Content.ReadAsStringAsync(ct)));
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var part = doc.RootElement.TryGetProperty("d", out var d) && d.TryGetProperty("results", out var r) && r.ValueKind == JsonValueKind.Array
                ? r.EnumerateArray().Select(e => e.Clone()).ToList()
                : throw new InvalidOperationException($"SAP {what}: Antwort ohne d.results.");
            rows.AddRange(part);
            if (single || part.Count < page) return rows;
            if (skip > 2_000_000) throw new InvalidOperationException($"SAP {what}: zu viele Seiten, Abbruch.");
        }
    }

    private static async Task<List<KeyValuePair<string, string>>> ValueHelp(HttpClient http, Func<string, string> so, string set, string key, string txt, string term, CancellationToken ct)
    {
        var url = so($"{set}?$top=1000&$select={key},{txt}&$filter=" + JsEncode($"substringof('{term}',{txt})"));
        return (await ReadAll(http, url, set, ct, single: true)).Select(r => new KeyValuePair<string, string>(Str(r, key), Str(r, txt))).ToList();
    }

    private static async Task<JsonObject> Names(HttpClient http, Func<string, string> so, string set, string key, string txt, List<string> keys, CancellationToken ct)
    {
        var result = new JsonObject();
        foreach (var chunk in keys.Where(k => k.Length > 0).Chunk(30))
        {
            try
            {
                var filter = string.Join(" or ", chunk.Select(x => $"{key} eq '{x.Replace("'", "''")}'"));
                foreach (var r in await ReadAll(http, so($"{set}?$select={key},{txt}&$filter=" + JsEncode(filter)), set, ct, single: true))
                    result[Str(r, key)] = Str(r, txt);
            }
            catch (HttpRequestException)
            {
                // Namen sind nur Anzeige; die Referenz uebergeht Fehler hier ebenso.
            }
        }
        return result;
    }
}
