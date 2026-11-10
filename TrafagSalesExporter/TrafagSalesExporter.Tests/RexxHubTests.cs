using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Rexx-Hub-Abruf (vorbereitet 2026-11-10): ohne Netzwerk, Rexx wird durch einen HttpMessageHandler ersetzt. Alle Namen,
/// Nummern und Werte sind erfunden.
/// </summary>
public sealed class RexxHubTests : IDisposable
{
    internal const string Token = "tok-ERFUNDEN-7f3a9c";
    internal const string BaseUrl = "https://rexx.example.test/rexx_hub/";
    private readonly string _root;

    public RexxHubTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "trafag-rexxhub-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { }
    }

    // ---- Hilfen ----

    internal sealed class FakeRexx : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, int, HttpResponseMessage> _respond;
        private int _active;
        public List<HttpRequestMessage> Requests { get; } = [];
        public int MaxParallel { get; private set; }
        public TimeSpan Hold { get; init; } = TimeSpan.Zero;

        public FakeRexx(Func<HttpRequestMessage, int, HttpResponseMessage> respond) => _respond = respond;

        public static FakeRexx Json(params string[] bodies)
            => new((_, i) => Ok(bodies[Math.Min(i, bodies.Length - 1)]));

        public static HttpResponseMessage Ok(string body)
            => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

        public static HttpResponseMessage Status(int code, string body = """{"message":"x","_meta":{"request_id":"req-err-1"}}""")
            => new((HttpStatusCode)code) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            int index;
            lock (Requests)
            {
                Requests.Add(request);
                index = Requests.Count - 1;
            }
            var now = Interlocked.Increment(ref _active);
            MaxParallel = Math.Max(MaxParallel, now);
            try
            {
                if (Hold > TimeSpan.Zero)
                    await Task.Delay(Hold, cancellationToken);
                return _respond(request, index);
            }
            finally
            {
                Interlocked.Decrement(ref _active);
            }
        }
    }

    internal sealed class FixedSource : IRexxHubConnectionSource
    {
        public int Calls { get; private set; }
        public Task<RexxHubConnection> ResolveAsync(CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new RexxHubConnection(BaseUrl, Token));
        }
    }

    internal sealed class RecordingLog : IAppEventLogService
    {
        public List<(string Message, string Level, string? Details)> Entries { get; } = [];
        public Task WriteAsync(string category, string message, string level = "Info", int? siteId = null, string? land = null, string? details = null)
        {
            Entries.Add((message, level, details));
            return Task.CompletedTask;
        }
        public Task WriteDebugAsync(string category, string message, int? siteId = null, string? land = null, string? details = null)
            => Task.CompletedTask;
    }

    private sealed class Env(string root) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Production";
        public string ApplicationName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = root;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    internal static RexxHubOptions Options(bool enabled = true) => new() { Enabled = enabled, RetryDelaySeconds = 0 };

    private static (RexxHubClient Client, List<TimeSpan> Waits) Client(FakeRexx rexx)
    {
        var waits = new List<TimeSpan>();
        var client = new RexxHubClient(rexx)
        {
            Delay = (wait, _) => { waits.Add(wait); return Task.CompletedTask; }
        };
        return (client, waits);
    }

    private static readonly RexxHubConnection Connection = new(BaseUrl, Token);

    private static string Envelope(IEnumerable<Dictionary<string, object?>> rows, object? meta = null, object? links = null)
        => JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["data"] = rows,
            ["_links"] = links ?? new Dictionary<string, object?>(),
            ["_meta"] = meta ?? new Dictionary<string, object?> { ["request_id"] = "req-test-1" }
        });

    private static Dictionary<string, object?> Row(params (string Key, object? Value)[] values)
        => values.ToDictionary(x => x.Key, x => x.Value);

    // ---- Schalter und Einstellungen ----

    [Fact]
    public void Einstellungen_Werden_Aus_HrKpi_RexxHub_Gelesen()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HrKpi:RexxHub:Enabled"] = "true",
                ["HrKpi:RexxHub:DailyAt"] = "06:15",
                ["HrKpi:RexxHub:GesamtQueryId"] = "901",
                ["HrKpi:RexxHub:PageParameter"] = "page",
                ["HrKpi:RexxHub:ColumnMap:staff_id"] = "Personalnummer"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddRexxHub(configuration);
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<RexxHubOptions>>().Value;

        Assert.True(options.Enabled);
        Assert.Equal(new TimeSpan(6, 15, 0), options.DailyAt);
        Assert.Equal(901, options.GesamtQueryId);
        Assert.Equal(777, options.CaseQueryId);
        Assert.Equal("page", options.PageParameter);
        Assert.Equal("Personalnummer", options.ColumnMap["staff_id"]);

        // Ohne Eintrag gilt: aus, keine Seiten-Parameter, Pfad laut Produktdokument.
        var defaults = new RexxHubOptions();
        Assert.False(defaults.Enabled);
        Assert.Equal((775, 777, "v1/datawarehouse/{0}", "", "", new TimeSpan(5, 30, 0)),
            (defaults.GesamtQueryId, defaults.CaseQueryId, defaults.PathTemplate, defaults.PageParameter, defaults.PageSizeParameter, defaults.DailyAt));
    }

    [Fact]
    public void Zugang_Aus_Dem_Settings_Eintrag_Wird_Geprueft()
    {
        Assert.Contains("fehlt", Assert.Throws<RexxHubException>(() => RexxHubClient.ToConnection(null)).Message);
        var entry = new SourceSystemDefinition { Code = "REXXHUB", IsActive = false, CentralServiceUrl = BaseUrl, CentralPassword = Token };
        Assert.Contains("nicht aktiv", Assert.Throws<RexxHubException>(() => RexxHubClient.ToConnection(entry)).Message);
        entry.IsActive = true;
        entry.CentralServiceUrl = "";
        Assert.Contains("Basis-URL fehlt", Assert.Throws<RexxHubException>(() => RexxHubClient.ToConnection(entry)).Message);
        entry.CentralServiceUrl = BaseUrl;
        entry.CentralPassword = " ";
        Assert.Contains("Token fehlt", Assert.Throws<RexxHubException>(() => RexxHubClient.ToConnection(entry)).Message);
        entry.CentralPassword = Token;
        Assert.Equal(new RexxHubConnection(BaseUrl, Token), RexxHubClient.ToConnection(entry));
    }

    // ---- Abruf ----

    [Fact]
    public async Task Abruf_Setzt_Bearer_Token_Und_Json_Und_Nimmt_Den_Pfad_Aus_Der_Vorlage()
    {
        var rexx = FakeRexx.Json(Envelope([Row(("Personalnummer", "00001001"))]));
        var (client, _) = Client(rexx);

        var result = await client.FetchQueryAsync(Connection, 775, Options());

        var request = Assert.Single(rexx.Requests);
        Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
        Assert.Equal(Token, request.Headers.Authorization.Parameter);
        Assert.Contains(request.Headers.Accept, x => x.MediaType == "application/json");
        Assert.Equal("https://rexx.example.test/rexx_hub/v1/datawarehouse/775", request.RequestUri!.AbsoluteUri);
        Assert.Single(result.Table.Rows);
        Assert.Equal("req-test-1", result.RequestId);
        Assert.Equal(200, result.HttpStatus);
    }

    [Fact]
    public async Task Abruf_Sendet_Ohne_Einstellung_Keine_Seiten_Parameter()
    {
        var rexx = FakeRexx.Json(Envelope([Row(("Personalnummer", "1"))]));
        var (client, _) = Client(rexx);
        await client.FetchQueryAsync(Connection, 777, Options());
        Assert.Equal(string.Empty, rexx.Requests[0].RequestUri!.Query);

        var withSize = Options();
        withSize.PageSizeParameter = "limit";
        withSize.PageSize = 250;
        withSize.QueryParameters["from"] = "2026-01-01";
        Assert.Equal("?from=2026-01-01&limit=250", RexxHubClient.BuildFirstUri(new Uri(BaseUrl), 777, withSize).Query);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(404)]
    [InlineData(422)]
    [InlineData(429)]
    [InlineData(500)]
    [InlineData(504)]
    public async Task Fehlermeldung_Enthaelt_Nie_Den_Token_Und_Nie_Den_Antwortinhalt(int status)
    {
        // Antwortinhalt mit Token und einem erfundenen Personenwert: beides darf nicht in die Meldung.
        var rexx = new FakeRexx((_, _) => FakeRexx.Status(status, $$$"""{"message":"{{{Token}}} Muster Anna","_meta":{"request_id":"req-9"}}"""));
        var (client, _) = Client(rexx);

        var ex = await Assert.ThrowsAsync<RexxHubException>(() => client.FetchQueryAsync(Connection, 775, Options()));

        Assert.DoesNotContain(Token, ex.Message);
        Assert.DoesNotContain("Muster", ex.Message);
        Assert.Equal(status, ex.StatusCode);
        Assert.Equal("req-9", ex.RequestId);
        Assert.Contains("#775", ex.Message);
    }

    [Fact]
    public async Task Netzwerkfehler_Mit_Token_Im_Text_Wird_Bereinigt()
    {
        var rexx = new FakeRexx((_, _) => throw new HttpRequestException($"Verbindung abgelehnt {Token}"));
        var (client, _) = Client(rexx);

        var ex = await Assert.ThrowsAsync<RexxHubException>(() => client.FetchQueryAsync(Connection, 775, Options()));

        Assert.DoesNotContain(Token, ex.Message);
        Assert.Contains("nicht erreichbar", ex.Message);
        Assert.DoesNotContain(Token, Connection.ToString());
    }

    [Theory]
    [InlineData(401, "nicht freigegeben")]
    [InlineData(403, "Token ungueltig")]
    [InlineData(404, "Abfrage-ID 775 unbekannt oder nicht freigegeben")]
    [InlineData(422, "Zu viele Felder")]
    public async Task Statuscodes_Ergeben_Die_Meldung_Aus_Dem_Produktdokument(int status, string expected)
    {
        var (client, _) = Client(new FakeRexx((_, _) => FakeRexx.Status(status)));

        var ex = await Assert.ThrowsAsync<RexxHubException>(() => client.FetchQueryAsync(Connection, 775, Options()));

        Assert.Contains(expected, ex.Message);
    }

    [Fact]
    public async Task Blaettert_Ueber_Zwei_Seiten_Mit_Links_Next()
    {
        var rexx = FakeRexx.Json(
            Envelope([Row(("Personalnummer", "1")), Row(("Personalnummer", "2"))],
                meta: new { current_page = 1, last_page = 2, total = 3, request_id = "req-a" },
                links: new { next = new { href = BaseUrl + "v1/datawarehouse/775?page=2" } }),
            Envelope([Row(("Personalnummer", "3"), ("Personal Status", "Aktiv"))],
                meta: new { current_page = 2, last_page = 2, total = 3, request_id = "req-b" }));
        var (client, _) = Client(rexx);

        var result = await client.FetchQueryAsync(Connection, 775, Options());

        Assert.Equal(2, rexx.Requests.Count);
        Assert.Equal("?page=2", rexx.Requests[1].RequestUri!.Query);
        Assert.Equal(Token, rexx.Requests[1].Headers.Authorization!.Parameter);
        Assert.Equal(["1", "2", "3"], result.Table.Rows.Select(r => (string)r["Personalnummer"]!));
        // Spalten als Vereinigung ueber alle Seiten: die zweite Seite bringt „Personal Status“ mit.
        Assert.Equal(["Personalnummer", "Personal Status"], result.Table.Columns);
        Assert.Equal(2, result.Pages);
        Assert.Equal("req-b", result.RequestId);
    }

    [Fact]
    public async Task Blaettert_Ueber_Meta_Wenn_Ein_Seitenparameter_Eingestellt_Ist()
    {
        var rexx = FakeRexx.Json(
            Envelope([Row(("Personalnummer", "1"))], meta: new { currentPage = 1, lastPage = 2 }),
            Envelope([Row(("Personalnummer", "2"))], meta: new { currentPage = 2, lastPage = 2 }));
        var (client, _) = Client(rexx);
        var options = Options();
        options.PageParameter = "page";

        var result = await client.FetchQueryAsync(Connection, 775, options);

        Assert.Equal("?page=2", rexx.Requests[1].RequestUri!.Query);
        Assert.Equal(2, result.Table.Rows.Count);
    }

    [Fact]
    public async Task Bricht_Ab_Statt_Halb_Zu_Liefern()
    {
        // Weitere Seiten angekuendigt, aber kein Link und kein Seitenparameter: nichts zurueckgeben.
        var noWay = FakeRexx.Json(Envelope([Row(("Personalnummer", "1"))], meta: new { current_page = 1, last_page = 3 }));
        var ex1 = await Assert.ThrowsAsync<RexxHubException>(() => Client(noWay).Client.FetchQueryAsync(Connection, 775, Options()));
        Assert.Contains("3 Seiten", ex1.Message);

        // Gesamtzahl passt nicht zu den gelieferten Zeilen.
        var short1 = FakeRexx.Json(Envelope([Row(("Personalnummer", "1"))], meta: new { total = 2 }));
        var ex2 = await Assert.ThrowsAsync<RexxHubException>(() => Client(short1).Client.FetchQueryAsync(Connection, 775, Options()));
        Assert.Contains("meldet 2", ex2.Message);

        // Seitenparameter wirkt nicht (Rexx liefert immer Seite 1): Abbruch statt Endlosschleife.
        var stuck = FakeRexx.Json(Envelope([Row(("Personalnummer", "1"))], meta: new { current_page = 1, last_page = 2 }));
        var options = Options();
        options.PageParameter = "seite";
        await Assert.ThrowsAsync<RexxHubException>(() => Client(stuck).Client.FetchQueryAsync(Connection, 775, options));
        options.PageParameter = string.Empty;

        // Link auf einen fremden Server bekommt den Token nicht; die Meldung nennt Schema, Host und Port beider Seiten.
        var foreign = FakeRexx.Json(Envelope([Row(("Personalnummer", "1"))], links: new { next = "https://fremd.example.test/v1/x?page=2" }));
        var ex3 = await Assert.ThrowsAsync<RexxHubException>(() => Client(foreign).Client.FetchQueryAsync(Connection, 775, Options()));
        Assert.Contains("anderen Server (https://fremd.example.test:443, Basis https://rexx.example.test:443)", ex3.Message);
        Assert.Single(foreign.Requests);
    }

    [Fact]
    public async Task Teilabruf_Ohne_Erkannte_Seitenangaben_Bricht_Ab()
    {
        // Beispiel aus der Pruefung: „max_pages“ und „count“, kein next-Link. Frueher galt Seite 1 als vollstaendig.
        var maxPages = FakeRexx.Json("""{"data":[{"Personalnummer":"1"}],"_meta":{"count":812,"items_per_page":500,"page":1,"max_pages":2}}""");
        var ex1 = await Assert.ThrowsAsync<RexxHubException>(() => Client(maxPages).Client.FetchQueryAsync(Connection, 775, Options()));
        Assert.Contains("2 Seiten", ex1.Message);

        // „count“ groesser als das Gelieferte, sonst nichts erkannt.
        var count = FakeRexx.Json("""{"data":[{"Personalnummer":"1"}],"_meta":{"count":393}}""");
        var ex2 = await Assert.ThrowsAsync<RexxHubException>(() => Client(count).Client.FetchQueryAsync(Connection, 775, Options()));
        Assert.Contains("Anzahl von 393", ex2.Message);

        // Unbekannter Name mit „page“ nennt weitere Seiten.
        var unknown = FakeRexx.Json("""{"data":[{"Personalnummer":"1"}],"_meta":{"seite":1,"page_total_x":4}}""");
        var ex3 = await Assert.ThrowsAsync<RexxHubException>(() => Client(unknown).Client.FetchQueryAsync(Connection, 775, Options()));
        Assert.Contains("_meta.page_total_x", ex3.Message);

        // Seite genau voll, keine Seitenzahl, kein Link.
        var full = FakeRexx.Json("""{"data":[{"Personalnummer":"1"},{"Personalnummer":"2"}],"_meta":{"per_page":2}}""");
        var ex4 = await Assert.ThrowsAsync<RexxHubException>(() => Client(full).Client.FetchQueryAsync(Connection, 775, Options()));
        Assert.Contains("genau voll", ex4.Message);

        // Unlesbarer next-Link oder „_links.last“ auf eine andere Seite.
        var badNext = FakeRexx.Json("""{"data":[{"Personalnummer":"1"}],"_links":{"next_url":{"x":"y"}}}""");
        Assert.Contains("_links.next_url", (await Assert.ThrowsAsync<RexxHubException>(() => Client(badNext).Client.FetchQueryAsync(Connection, 775, Options()))).Message);
        var last = FakeRexx.Json("""{"data":[{"Personalnummer":"1"}],"_links":{"self":"v1/datawarehouse/775","last":"v1/datawarehouse/775?page=4"}}""");
        Assert.Contains("_links.last", (await Assert.ThrowsAsync<RexxHubException>(() => Client(last).Client.FetchQueryAsync(Connection, 775, Options()))).Message);

        // Eingestellter Name fuer die letzte Seite: dann blaettert der Abruf ueber den Seitenparameter.
        var custom = FakeRexx.Json(
            """{"data":[{"Personalnummer":"1"}],"_meta":{"seite":1,"seiten":2}}""",
            """{"data":[{"Personalnummer":"2"}],"_meta":{"seite":2,"seiten":2}}""");
        var options = Options();
        options.MetaCurrentPageKeys.Add("seite");
        options.MetaLastPageKeys.Add("seiten");
        options.PageParameter = "page";
        var result = await Client(custom).Client.FetchQueryAsync(Connection, 775, options);
        Assert.Equal(2, result.Table.Rows.Count);
        Assert.Equal(["seite", "seiten"], result.MetaKeys);

        // Kleinere Anzahl (Zeilen dieser Seite) und eine nicht volle letzte Seite sind in Ordnung.
        var fine = FakeRexx.Json("""{"data":[{"Personalnummer":"1"}],"_meta":{"count":1,"per_page":500}}""");
        Assert.Single((await Client(fine).Client.FetchQueryAsync(Connection, 775, Options())).Table.Rows);
    }

    [Fact]
    public async Task Letzte_Seite_Laut_Meta_Beendet_Auch_Mit_Next_Link()
    {
        // Manche Server zeigen auf der letzten Seite mit „next“ auf sich selbst; frueher brach das jeden Tag ab.
        var rexx = FakeRexx.Json(
            """{"data":[{"Personalnummer":"1"}],"_meta":{"current_page":1,"last_page":2},"_links":{"next":"v1/datawarehouse/775?page=2"}}""",
            """{"data":[{"Personalnummer":"2"}],"_meta":{"current_page":2,"last_page":2},"_links":{"next":"v1/datawarehouse/775?page=2"}}""");
        var result = await Client(rexx).Client.FetchQueryAsync(Connection, 775, Options());
        Assert.Equal(2, rexx.Requests.Count);
        Assert.Equal(2, result.Table.Rows.Count);

        // Ohne bekannte letzte Seite laesst sich „next“ auf sich selbst nicht als Ende werten: Abbruch statt Teilabruf.
        var self = FakeRexx.Json("""{"data":[{"Personalnummer":"1"}],"_links":{"next":"775"}}""");
        var ex = await Assert.ThrowsAsync<RexxHubException>(() => Client(self).Client.FetchQueryAsync(Connection, 775, Options()));
        Assert.Contains("schon gelesene Adresse", ex.Message);
    }

    [Fact]
    public async Task Http_Link_Hinter_Tls_Terminator_Wird_Auf_Https_Umgeschrieben()
    {
        var rexx = FakeRexx.Json(
            Envelope([Row(("Personalnummer", "1"))], meta: new { current_page = 1, last_page = 2 },
                links: new { next = "http://rexx.example.test/rexx_hub/v1/datawarehouse/775?page=2" }),
            Envelope([Row(("Personalnummer", "2"))], meta: new { current_page = 2, last_page = 2 }));

        await Client(rexx).Client.FetchQueryAsync(Connection, 775, Options());

        Assert.Equal("https://rexx.example.test/rexx_hub/v1/datawarehouse/775?page=2", rexx.Requests[1].RequestUri!.AbsoluteUri);
        Assert.All(rexx.Requests, r => Assert.Equal("https", r.RequestUri!.Scheme));
    }

    [Fact]
    public async Task Http_Als_Basis_Wird_Ohne_Schalter_Abgelehnt()
    {
        var entry = new SourceSystemDefinition { Code = "REXXHUB", IsActive = true, CentralServiceUrl = "http://rexx.example.test/rexx_hub/", CentralPassword = Token };
        Assert.Contains("nicht https", Assert.Throws<RexxHubException>(() => RexxHubClient.ToConnection(entry)).Message);
        Assert.Equal("http://rexx.example.test/rexx_hub/", RexxHubClient.ToConnection(entry, allowHttp: true).BaseUrl);

        // Auch eine andere Zugangsquelle umgeht die Pruefung nicht: der Abruf selbst schickt den Token nur ueber https.
        var rexx = FakeRexx.Json(Envelope([Row(("Personalnummer", "1"))]));
        var ex = await Assert.ThrowsAsync<RexxHubException>(() =>
            Client(rexx).Client.FetchQueryAsync(new RexxHubConnection("http://rexx.example.test/rexx_hub/", Token), 775, Options()));
        Assert.Contains("nicht https", ex.Message);
        Assert.Empty(rexx.Requests);
    }

    [Fact]
    public void Datumsplatzhalter_In_QueryParameters()
    {
        var options = Options();
        options.QueryParameters["von"] = "{jahresanfang}";
        options.QueryParameters["bis"] = "{heute}";
        var uri = RexxHubClient.BuildFirstUri(new Uri(BaseUrl), 775, options, new DateTime(2026, 11, 10));
        Assert.Equal("?von=2026-01-01&bis=2026-11-10", uri.Query);
    }

    [Fact]
    public async Task Antwort_429_Wird_Hoechstens_Zweimal_Wiederholt()
    {
        var twice = new FakeRexx((_, i) => i < 2 ? FakeRexx.Status(429) : FakeRexx.Ok(Envelope([Row(("Personalnummer", "1"))])));
        var (client, waits) = Client(twice);
        var result = await client.FetchQueryAsync(Connection, 775, Options());
        Assert.Equal(3, twice.Requests.Count);
        Assert.Equal(2, waits.Count);
        Assert.Single(result.Table.Rows);

        var always = new FakeRexx((_, _) => FakeRexx.Status(429));
        var (client2, waits2) = Client(always);
        var ex = await Assert.ThrowsAsync<RexxHubException>(() => client2.FetchQueryAsync(Connection, 775, Options()));
        Assert.Equal(429, ex.StatusCode);
        Assert.Equal(3, always.Requests.Count);
        Assert.Equal(2, waits2.Count);
    }

    [Fact]
    public async Task Nur_Eine_Anfrage_Gleichzeitig_Auch_Ueber_Zwei_Clients()
    {
        var rexx = new FakeRexx((_, _) => FakeRexx.Ok(Envelope([Row(("Personalnummer", "1"))]))) { Hold = TimeSpan.FromMilliseconds(50) };
        var a = new RexxHubClient(rexx);
        var b = new RexxHubClient(rexx);

        await Task.WhenAll(
            a.FetchQueryAsync(Connection, 775, Options()), b.FetchQueryAsync(Connection, 777, Options()),
            a.FetchQueryAsync(Connection, 775, Options()), b.FetchQueryAsync(Connection, 777, Options()));

        Assert.Equal(4, rexx.Requests.Count);
        Assert.Equal(1, rexx.MaxParallel);
    }

    // ---- Antwortformen ----

    [Fact]
    public void Erkennt_Liste_Von_Objekten()
    {
        var page = RexxHubResponseParser.ParsePage("""
            {"data":[{"Personalnummer":"00001001","Urlaub Rest":4.5,"Austrittsdatum":null},{"Personalnummer":"00001002","Stunden Saldo":"-3:15"}],
             "_links":{},"_meta":{"total":2,"requestId":"r-1"}}
            """);

        Assert.Equal(["Personalnummer", "Urlaub Rest", "Austrittsdatum", "Stunden Saldo"], page.Table.Columns);
        Assert.Equal(4.5m, page.Table.Rows[0]["Urlaub Rest"]);
        Assert.Null(page.Table.Rows[0]["Austrittsdatum"]);
        Assert.Equal("-3:15", page.Table.Rows[1]["Stunden Saldo"]);
        Assert.Equal(2, page.Total);
        Assert.Equal("r-1", page.RequestId);
    }

    [Fact]
    public void Erkennt_Objekt_Mit_Spalten_Und_Zeilen()
    {
        var page = RexxHubResponseParser.ParsePage("""
            {"data":{"columns":[{"name":"pnr","label":"Personalnummer"},{"label":"Personal Status"}],
                     "rows":[["00001001","Aktiv"],["00001002","Ausgeschieden"]]},
             "_meta":{"request_id":"r-2"}}
            """);

        Assert.Equal(["Personalnummer", "Personal Status"], page.Table.Columns);
        Assert.Equal("Ausgeschieden", page.Table.Rows[1]["Personal Status"]);
    }

    [Fact]
    public void Erkennt_Liste_Von_Listen_Mit_Separater_Oder_Erster_Kopfzeile()
    {
        var separate = RexxHubResponseParser.ParsePage("""
            {"columns":["Personalnummer","Abwesenheit von"],"data":[["00001001","2026-11-02"],["00001002","2026-11-03"]],"_meta":{}}
            """);
        Assert.Equal(["Personalnummer", "Abwesenheit von"], separate.Table.Columns);
        Assert.Equal(2, separate.Table.Rows.Count);

        var firstRow = RexxHubResponseParser.ParsePage("""
            {"data":[["Personalnummer","Abwesenheit von"],["00001001","2026-11-02"]]}
            """);
        Assert.Equal(["Personalnummer", "Abwesenheit von"], firstRow.Table.Columns);
        Assert.Equal("2026-11-02", Assert.Single(firstRow.Table.Rows)["Abwesenheit von"]);
    }

    [Fact]
    public void Unbekannte_Antwort_Nennt_Die_Schluessel_Aber_Keine_Werte()
    {
        var ex = Assert.Throws<RexxHubFormatException>(() => RexxHubResponseParser.ParsePage("""
            {"data":{"person":"Muster Anna","saldo":"12:30"},"_links":{},"_meta":{"request_id":"r-3"}}
            """));

        Assert.StartsWith("Antwortformat unbekannt", ex.Message);
        Assert.Contains("data, _links, _meta", ex.Message);
        // Seit der Pruefung 2026-11-10: von einem data-Objekt nur Anzahl und Art, nicht die Schluessel (koennten Personalnummern sein).
        Assert.Contains("Objekt mit 2 Schluesseln", ex.Message);
        Assert.DoesNotContain("person", ex.Message);
        Assert.DoesNotContain("Muster", ex.Message);
        Assert.DoesNotContain("12:30", ex.Message);

        var noData = Assert.Throws<RexxHubFormatException>(() => RexxHubResponseParser.ParsePage("""{"items":[1,2]}"""));
        Assert.Contains("items", noData.Message);
    }

    [Fact]
    public void Personalnummern_Als_Schluessel_Erscheinen_Nie_In_Der_Meldung()
    {
        // Ein nach Personalnummer geordnetes Woerterbuch (PHP-Array), einmal unter „data“, einmal ohne Umschlag.
        var inData = Assert.Throws<RexxHubFormatException>(() => RexxHubResponseParser.ParsePage("""
            {"data":{"00004711":{"x":1},"00004712":{"x":2}},"_meta":{"request_id":"r-4"}}
            """));
        Assert.Contains("Objekt mit 2 Schluesseln", inData.Message);
        Assert.DoesNotContain("4711", inData.Message);

        var top = Assert.Throws<RexxHubFormatException>(() => RexxHubResponseParser.ParsePage("""
            {"00004711":{"x":1},"Muster, Anna":{"x":2},"_meta":{}}
            """));
        Assert.DoesNotContain("4711", top.Message);
        Assert.DoesNotContain("Muster", top.Message);
        Assert.Contains("_meta", top.Message);
        Assert.Contains("+2 nicht angezeigt", top.Message);
    }

    [Fact]
    public void Datenzeile_Aus_Lauter_Texten_Wird_Nicht_Zur_Kopfzeile()
    {
        // Ohne Kopfzeile, alle Werte als Text: frueher wurde die erste Person zur Kopfzeile.
        var ex = Assert.Throws<RexxHubFormatException>(() => RexxHubResponseParser.ParsePage("""
            {"data":[["00001001","Muster, Anna","Aktiv","0:00","0:00"],["00001002","Beispiel, Bruno","Aktiv","1:00","0:00"]]}
            """));
        Assert.Contains("ohne erkennbare Kopfzeile", ex.Message);
        Assert.DoesNotContain("Muster", ex.Message);
        Assert.DoesNotContain("1001", ex.Message);

        // Spaltenliste vorhanden, aber nicht lesbar: Formatfehler statt Ausweichen auf die erste Zeile.
        var unreadable = Assert.Throws<RexxHubFormatException>(() => RexxHubResponseParser.ParsePage("""
            {"columns":[{"id":1}],"data":[["Personalnummer"],["00001001"]]}
            """));
        Assert.Contains("nicht lesbar", unreadable.Message);

        // Kopfzeile mit technischen Namen wird erkannt, wenn die ColumnMap sie auf eine Leserspalte abbildet.
        var options = Options();
        options.ColumnMap["staff_id"] = "Personalnummer";
        var mapped = RexxHubResponseParser.ParsePage("""{"data":[["staff_id","other"],["00001001","x"]]}""",
            RexxHubParseSettings.From(options, null));
        Assert.Equal(["staff_id", "other"], mapped.Table.Columns);
        Assert.Single(mapped.Table.Rows);
    }

    [Fact]
    public async Task Kopfzeile_Von_Seite_1_Gilt_Auf_Seite_2()
    {
        // Seite 2 wiederholt die Kopfzeile nicht und liefert alle Werte als Text; Seite 3 wiederholt sie.
        var rexx = FakeRexx.Json(
            """{"data":[["Personalnummer","Personal Status"],["00001001","Aktiv"]],"_meta":{"current_page":1,"last_page":3},"_links":{"next":"v1/datawarehouse/775?page=2"}}""",
            """{"data":[["00001002","Ausgeschieden"]],"_meta":{"current_page":2,"last_page":3},"_links":{"next":"v1/datawarehouse/775?page=3"}}""",
            """{"data":[["Personalnummer","Personal Status"],["00001003","Aktiv"]],"_meta":{"current_page":3,"last_page":3}}""");

        var result = await Client(rexx).Client.FetchQueryAsync(Connection, 775, Options());

        Assert.Equal(["Personalnummer", "Personal Status"], result.Table.Columns);
        Assert.Equal(["00001001", "00001002", "00001003"], result.Table.Rows.Select(r => (string)r["Personalnummer"]!));
        Assert.Equal(3, result.Pages);
    }

    [Fact]
    public async Task ColumnMap_Bildet_Technische_Namen_Auf_Die_Ueberschriften_Ab()
    {
        var rexx = FakeRexx.Json(Envelope([Row(("staff_id", "00001001"), ("status", "Aktiv"), ("weiteres", "x"))]));
        var options = Options();
        options.ColumnMap["STAFF_ID"] = "Personalnummer";
        options.ColumnMap["status"] = "Personal Status";

        var result = await Client(rexx).Client.FetchQueryAsync(Connection, 775, options);

        Assert.Equal(["Personalnummer", "Personal Status", "weiteres"], result.Table.Columns);
        Assert.Equal("00001001", result.Table.Rows[0]["Personalnummer"]);
        var check = RexxHubColumns.Check(RexxHubQueryKind.Gesamt, result.Table);
        Assert.DoesNotContain("Personalnummer", check.FehlendePflichtspalten);
        Assert.Equal(["weiteres"], check.ZusaetzlicheSpalten);
    }

    [Fact]
    public void Spaltenpruefung_Kennt_Die_Schreibweisen_Des_Lesers()
    {
        var table = new RexxHubTable();
        foreach (var name in new[] { "Personalnummer", "Personal Status", "Eintrittsdatum", "Austrittsdatum", "Austrittsart",
                     "O taegliche Sollarbeitszeit (Woche)", "Militaer / Zivildienst angetreten (Stunden)", "Krankheit angetreten (Zeitraum)" })
            table.AddColumn(name);
        table.Rows.Add(new Dictionary<string, object?>
        {
            ["Personalnummer"] = "00001001", ["Eintrittsdatum"] = "2020-01-01", ["Krankheit angetreten (Zeitraum)"] = "2026-11-02 - 2026-11-03"
        });

        var check = RexxHubColumns.Check(RexxHubQueryKind.Gesamt, table);

        // Seit der Pruefung 2026-11-10 sind auch „Stelle“ und der Name Pflicht, und Pflichtspalten muessen Werte haben.
        Assert.Equal(["Nachname, Vorname (Link Personal)", "Stelle"], check.FehlendePflichtspalten);
        Assert.False(check.KannSchreiben);
        Assert.Contains(check.Sperrgruende, x => x.StartsWith("Personal Status: nur 0 von 1 Zeilen gefuellt", StringComparison.Ordinal));
        Assert.DoesNotContain("Ø tägliche Sollarbeitszeit (Woche)", check.FehlendeLeserspalten);
        Assert.DoesNotContain("Militär / Zivildienst angetreten (Stunden)", check.FehlendeLeserspalten);
        Assert.Contains("Urlaub Rest", check.FehlendeLeserspalten);
        // ISO-Daten im Zeitraum-Text liest ParseRexxPeriod nicht: die Formatpruefung meldet es (nur Zaehlwerte).
        var period = Assert.Single(check.Formatpruefung, x => x.Spalte == "Krankheit angetreten (Zeitraum)");
        Assert.Equal((1, 0), (period.Gefuellt, period.Erkannt));
        Assert.Equal((1, 1), check.Formatpruefung.Where(x => x.Spalte == "Eintrittsdatum").Select(x => (x.Gefuellt, x.Erkannt)).Single());
    }

    // ---- Tagesdienst und Schreiben ----

    private RexxHubRefreshService Service(FakeRexx rexx, RecordingLog log, bool enabled = true, IRexxHubConnectionSource? source = null)
        => new(source ?? new FixedSource(), Client(rexx).Client, log, new Env(_root),
            Microsoft.Extensions.Options.Options.Create(Options(enabled)),
            Microsoft.Extensions.Options.Options.Create(new HrKpiDataSourceOptions()));

    [Fact]
    public async Task Fehlt_Eine_Pflichtspalte_Bleibt_Die_Bisherige_Datei_Unveraendert()
    {
        var folder = Path.Combine(_root, "hrdata");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, new HrKpiDataSourceOptions().GesamtFile);
        File.WriteAllBytes(path, [1, 2, 3, 4]);
        var before = File.GetLastWriteTimeUtc(path);
        var rexx = FakeRexx.Json(Envelope([Row(("Personalnummer", "00001001"), ("Eintrittsdatum", "2020-01-01"))]));
        var log = new RecordingLog();

        var written = await Service(rexx, log).RefreshAsync(RexxHubQueryKind.Gesamt, path, CancellationToken.None);

        Assert.False(written);
        Assert.Equal([1, 2, 3, 4], File.ReadAllBytes(path));
        Assert.Equal(before, File.GetLastWriteTimeUtc(path));
        Assert.False(File.Exists(Path.Combine(folder, "Hrcockpitgesamtdashboard.manuell.xlsx")));
        var entry = Assert.Single(log.Entries);
        Assert.Equal("Warning", entry.Level);
        Assert.Contains("Personal Status", entry.Details);
        Assert.Contains("Austrittsart", entry.Details);
        Assert.DoesNotContain("00001001", entry.Details);
    }

    [Fact]
    public async Task Leere_Antwort_Und_Fehler_Schreiben_Nicht()
    {
        var path = Path.Combine(_root, "hrdata", new HrKpiDataSourceOptions().RexxCaseFile);
        var log = new RecordingLog();

        Assert.False(await Service(FakeRexx.Json(Envelope([])), log).RefreshAsync(RexxHubQueryKind.Faelle, path, CancellationToken.None));
        Assert.False(await Service(new FakeRexx((_, _) => FakeRexx.Status(403)), log).RefreshAsync(RexxHubQueryKind.Faelle, path, CancellationToken.None));

        Assert.False(File.Exists(path));
        Assert.Contains(log.Entries, x => x.Details!.Contains("keine Zeile"));
        Assert.Contains(log.Entries, x => x.Message.Contains("fehlgeschlagen") && x.Details!.Contains("Token ungueltig"));
        Assert.All(log.Entries, x => Assert.DoesNotContain(Token, x.Details));
    }

    [Fact]
    public async Task Ausgeschaltet_Wird_Nichts_Abgerufen()
    {
        var rexx = FakeRexx.Json(Envelope([Row(("Personalnummer", "1"))]));
        var source = new FixedSource();
        var log = new RecordingLog();
        var service = Service(rexx, log, enabled: false, source: source);

        await service.RunDueAsync(new DateTime(2026, 11, 10, 9, 0, 0), CancellationToken.None);
        await service.StartAsync(CancellationToken.None);
        await service.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5));
        await service.StopAsync(CancellationToken.None);

        Assert.True(service.ExecuteTask.IsCompletedSuccessfully);
        Assert.Empty(rexx.Requests);
        Assert.Equal(0, source.Calls);
        Assert.Empty(log.Entries);
        Assert.False(Directory.Exists(Path.Combine(_root, "hrdata")));
    }

    [Fact]
    public void Tagesabruf_Ab_DailyAt_Einmal_Je_Tag_Und_Je_Datei()
    {
        var at = new TimeSpan(5, 30, 0);
        var day = new DateTime(2026, 11, 10);
        Assert.False(RexxHubRefreshService.IsDue(day.AddHours(5).AddMinutes(29), at, null, default));
        Assert.True(RexxHubRefreshService.IsDue(day.AddHours(6), at, null, default));
        Assert.True(RexxHubRefreshService.IsDue(day.AddHours(6), at, day.AddDays(-1).AddHours(7), default));
        Assert.False(RexxHubRefreshService.IsDue(day.AddHours(6), at, day.AddHours(5).AddMinutes(45), default));
        Assert.False(RexxHubRefreshService.IsDue(day.AddHours(9), at, null, DateOnly.FromDateTime(day)));
    }

    [Fact]
    public async Task Tagesdienst_Schreibt_Beide_Dateien_Und_Sichert_Den_Handexport_Einmal()
    {
        var folder = Path.Combine(_root, "hrdata");
        Directory.CreateDirectory(folder);
        var gesamtPath = Path.Combine(folder, new HrKpiDataSourceOptions().GesamtFile);
        File.WriteAllBytes(gesamtPath, [9, 9, 9]);
        File.SetLastWriteTime(gesamtPath, new DateTime(2026, 11, 9, 8, 0, 0));
        var gesamt = Envelope([GesamtApiRow(1001, "Aktiv")]);
        var faelle = Envelope([CaseApiRow(1001, new DateTime(2026, 11, 2), "Krankheit")]);
        var rexx = new FakeRexx((r, _) => FakeRexx.Ok(r.RequestUri!.AbsolutePath.EndsWith("/775") ? gesamt : faelle));
        var log = new RecordingLog();

        await Service(rexx, log).RunDueAsync(new DateTime(2026, 11, 10, 6, 0, 0), CancellationToken.None);

        Assert.Equal(2, rexx.Requests.Count);
        Assert.Equal([9, 9, 9], File.ReadAllBytes(Path.Combine(folder, "Hrcockpitgesamtdashboard.manuell.xlsx")));
        Assert.False(File.Exists(Path.Combine(folder, "Hrcockpitgesamtdashboard.tmp.xlsx")));
        using (var workbook = new XLWorkbook(gesamtPath))
            Assert.Equal("Personalnummer", workbook.Worksheets.First().Cell(1, 1).GetString());
        Assert.True(File.Exists(Path.Combine(folder, new HrKpiDataSourceOptions().RexxCaseFile)));
        Assert.Contains(log.Entries, x => x.Message == "Rexx-Hub #775 geschrieben" && x.Details!.Contains("1 Zeilen") && x.Details.Contains("req-test-1"));
        Assert.Contains(log.Entries, x => x.Message == "Rexx-Hub #777 geschrieben");
        Assert.All(log.Entries, x => Assert.DoesNotContain("Muster", x.Details));
    }

    // ---- Pruefung vor dem Schreiben (Nacharbeit aus der Pruefung 2026-11-10) ----

    private static RexxHubTable Table(IEnumerable<Dictionary<string, object?>> rows)
    {
        var table = new RexxHubTable();
        foreach (var row in rows)
        {
            foreach (var key in row.Keys)
                table.AddColumn(key);
            table.Rows.Add(row);
        }
        return table;
    }

    private static Dictionary<string, object?> With(Dictionary<string, object?> row, string key, object? value)
    {
        row[key] = value;
        return row;
    }

    [Fact]
    public void Gesamt_Mit_Allen_Pflichtwerten_Wird_Geschrieben()
    {
        var check = RexxHubColumns.Check(RexxHubQueryKind.Gesamt, Table(People().Select(ApiRow)));
        Assert.True(check.KannSchreiben, check.Ablehnungsgrund());
        Assert.Contains(check.Wertpruefung, x => x.Art.StartsWith("angestellt", StringComparison.Ordinal) && (x.Gefuellt, x.Erkannt) == (4, 3));
        Assert.Contains(check.Wertpruefung, x => x.Art == "Ausgeschieden" && x.Erkannt == 1);
    }

    [Theory]
    [InlineData("Personalnummer", "P-1001", "als Ganzzahl erkannt")]
    [InlineData("Eintrittsdatum", "num", "als Datum erkannt")]
    [InlineData("Personal Status", "1", "kein Wert als angestellt erkannt")]
    [InlineData("Stelle", null, "in keiner Zeile gefuellt")]
    [InlineData("Austrittsdatum", null, "bei keiner der 1 ausgeschiedenen")]
    public void Gesamt_Mit_Unbrauchbaren_Pflichtwerten_Wird_Nicht_Geschrieben(string column, string? value, string expected)
    {
        var rows = People().Select(ApiRow).ToList();
        foreach (var row in rows)
            row[column] = value == "num" ? 20200101m : value;

        var check = RexxHubColumns.Check(RexxHubQueryKind.Gesamt, Table(rows));

        Assert.False(check.KannSchreiben);
        Assert.Contains(expected, check.Ablehnungsgrund());
        Assert.DoesNotContain("Muster", check.Ablehnungsgrund());
        Assert.DoesNotContain("20200101", check.Ablehnungsgrund());
    }

    [Fact]
    public void Kopfzeile_Mit_Namen_Aber_Ohne_Werte_Wird_Nicht_Geschrieben()
    {
        // „columns“ nennt Personalnummer, die Objekte haben aber andere Schluessel: die Spalte ist nur dem Namen nach da.
        var page = RexxHubResponseParser.ParsePage("""
            {"data":{"columns":["Personalnummer","Personal Status"],"rows":[{"staff_id":"00001001","status":"Aktiv"}]}}
            """);
        var check = RexxHubColumns.Check(RexxHubQueryKind.Faelle, page.Table);
        Assert.Contains("Personalnummer: nur 0 von 1 Zeilen gefuellt", check.Ablehnungsgrund());
        Assert.False(check.KannSchreiben);
    }

    [Fact]
    public void Doppelte_Personalnummern_Und_Starker_Rueckgang_Werden_Nicht_Geschrieben()
    {
        var rows = People().Select(ApiRow).ToList();
        var doubled = rows.Concat(People().Select(ApiRow)).ToList();
        var check = RexxHubColumns.Check(RexxHubQueryKind.Gesamt, Table(doubled));
        Assert.Equal(4, check.DoppeltePersonalnummern);
        Assert.Contains("4 Zeilen mit einer Nummer, die schon vorkommt", check.Ablehnungsgrund());
        // Einstellbar (HrKpi:RexxHub:MaxDuplicatePersonalnummern), falls #775 eine Person legitim zweimal fuehrt.
        Assert.True(RexxHubColumns.Check(RexxHubQueryKind.Gesamt, Table(doubled), maxDuplicates: 4).KannSchreiben);

        var drop = RexxHubColumns.Check(RexxHubQueryKind.Gesamt, Table(rows), previousRows: 393);
        Assert.False(drop.KannSchreiben);
        Assert.Contains("nur 4 Zeilen, die bisherige Datei hat 393", drop.Ablehnungsgrund());
        Assert.True(RexxHubColumns.Check(RexxHubQueryKind.Gesamt, Table(rows), previousRows: 5).KannSchreiben);
        Assert.True(RexxHubColumns.Check(RexxHubQueryKind.Gesamt, Table(rows), previousRows: 393, minRowShareOfPrevious: 0).KannSchreiben);
    }

    [Fact]
    public void Faelle_Mit_Codes_Statt_Rexx_Texten_Werden_Nicht_Geschrieben()
    {
        // Auswahlwerte-Mapping (Produktdokument 3.3): Art und Status kaemen als Code.
        var codes = Table([With(With(CaseApiRow(1001, new DateTime(2026, 11, 2), "0260"), "Abwesenheit Status", "2"), "Abwesenheit Tage", 1)]);
        var check = RexxHubColumns.Check(RexxHubQueryKind.Faelle, codes);
        Assert.False(check.KannSchreiben);
        Assert.Contains("kein Fall mit ausgewerteter Art und bestaetigtem Status", check.Ablehnungsgrund());
        Assert.Equal((1, 0), check.Wertpruefung.Where(x => x.Art == "ausgewertete Art").Select(x => (x.Gefuellt, x.Erkannt)).Single());

        var texts = RexxHubColumns.Check(RexxHubQueryKind.Faelle, Table([CaseApiRow(1001, new DateTime(2026, 11, 2), "Krankheit")]));
        Assert.True(texts.KannSchreiben, texts.Ablehnungsgrund());
    }

    [Fact]
    public async Task Unbrauchbare_Werte_In_Nebenspalten_Lassen_Die_Datei_Lesbar()
    {
        // Gemessen 2026-11-10: eine Zahl wie 19900505 in einer Datumsspalte liess die ganze Datei leer lesen
        // („Not a legal OleAut date.“), und „4.5“ als Text las ClosedXML unter de-DE als 45.
        var people = People();
        var rows = people.Select(ApiRow).ToList();
        foreach (var row in rows)
        {
            row["Geburtsdatum"] = 19900505m;
            row["Urlaub Rest"] = "4.5";
            row["Ferien angetreten (Tage)"] = "";
            row["Eintrittsdatum"] = "2020-01-01T00:00:00Z";
        }
        var json = Envelope(rows);
        var caseJson = Envelope([CaseApiRow(1001, PastWorkday(), "Krankheit")]);
        var rexx = new FakeRexx((r, _) => FakeRexx.Ok(r.RequestUri!.AbsolutePath.EndsWith("/775") ? json : caseJson));
        var log = new RecordingLog();
        await Service(rexx, log).RunDueAsync(DateTime.Today.AddHours(23), CancellationToken.None);

        var old = CultureInfo.CurrentCulture;
        HrKpiResult result;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var service = new HrKpiService(Microsoft.Extensions.Options.Options.Create(new HrKpiDataSourceOptions()));
            result = await service.BuildAsync(new HrKpiOptions { DataFolder = Path.Combine(_root, "hrdata") });
        }
        finally
        {
            CultureInfo.CurrentCulture = old;
        }

        var status = result.FileStatuses.Single(x => x.Label == "Rexx #775 Gesamt");
        Assert.Equal((4, "OK"), (status.RowCount, status.Message));
        var first = result.Employees.Single(x => x.Personalnummer == 1001);
        Assert.Null(first.Geburtsdatum);
        Assert.Equal(new DateTime(2020, 1, 1), first.Eintrittsdatum);
        Assert.Equal(4.5m + 2m, first.UrlaubRest); // Rest ohne geplante + ausstehend (2), wie im Leser
        var written = Assert.Single(log.Entries, x => x.Message == "Rexx-Hub #775 geschrieben");
        Assert.Contains("Geburtsdatum (0 von 4 Datum)", written.Details);
        Assert.Contains("4 Datumswerte mit Uhrzeit oder Zeitzone", written.Details);
        Assert.DoesNotContain("19900505", written.Details);
    }

    [Fact]
    public async Task Jeder_Handexport_Wird_Gesichert_Die_Eigene_Datei_Nicht()
    {
        var folder = Path.Combine(_root, "hrdata");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, new HrKpiDataSourceOptions().GesamtFile);
        var json = Envelope(People().Select(ApiRow));
        var service = Service(FakeRexx.Json(json), new RecordingLog());

        // 1. Handexport A, 2. eigener Abruf, 3. Handexport B, 4. Abruf: A und B sind beide gesichert, die eigene Datei nicht.
        WriteHandWorkbook(path, GesamtHeaders, People().Select(HandRow));
        var handA = File.ReadAllBytes(path);
        Assert.True(await service.RefreshAsync(RexxHubQueryKind.Gesamt, path, CancellationToken.None));
        Assert.True(await service.RefreshAsync(RexxHubQueryKind.Gesamt, path, CancellationToken.None));
        WriteHandWorkbook(path, GesamtHeaders, People().Select(HandRow).Take(4));
        var handB = File.ReadAllBytes(path);
        Assert.True(await service.RefreshAsync(RexxHubQueryKind.Gesamt, path, CancellationToken.None));

        var backups = Directory.GetFiles(folder, "Hrcockpitgesamtdashboard.manuell*.xlsx").OrderBy(x => x.Length).ToList();
        Assert.Equal(2, backups.Count);
        Assert.Equal(handA, File.ReadAllBytes(backups[0]));
        Assert.Equal(handB, File.ReadAllBytes(backups[1]));
        Assert.True(RexxHubWorkbook.Inspect(RexxHubQueryKind.Gesamt, path).FromHub);
    }

    [Fact]
    public async Task Voruebergehender_Fehler_Wird_Im_Naechsten_Durchgang_Wiederholt_Andere_Nicht()
    {
        var gesamt = Envelope(People().Select(ApiRow));
        var faelle = Envelope([CaseApiRow(1001, new DateTime(2026, 11, 2), "Krankheit")]);
        var calls775 = 0;
        // #775: zweimal 500, dann Erfolg; #777: immer 403.
        var rexx = new FakeRexx((r, _) => r.RequestUri!.AbsolutePath.EndsWith("/775")
            ? (++calls775 <= 2 ? FakeRexx.Status(500) : FakeRexx.Ok(gesamt))
            : FakeRexx.Status(403));
        var log = new RecordingLog();
        var service = Service(rexx, log);
        var day = new DateTime(2026, 11, 10, 5, 30, 0);

        for (var pass = 0; pass < 4; pass++)
            await service.RunDueAsync(day.AddMinutes(30 * pass), CancellationToken.None);

        Assert.Equal(3, calls775);
        Assert.True(File.Exists(Path.Combine(_root, "hrdata", new HrKpiDataSourceOptions().GesamtFile)));
        Assert.Equal(1, rexx.Requests.Count(r => r.RequestUri!.AbsolutePath.EndsWith("/777")));
        Assert.Contains(log.Entries, x => x.Details!.Contains("Voruebergehend"));

        // Obergrenze je Tag: immer 503 -> hoechstens MaxAttemptsPerDay Versuche.
        var always = new FakeRexx((_, _) => FakeRexx.Status(503));
        var capped = new RexxHubRefreshService(new FixedSource(), Client(always).Client, new RecordingLog(), new Env(Path.Combine(_root, "b")),
            Microsoft.Extensions.Options.Options.Create(new RexxHubOptions { Enabled = true, RetryDelaySeconds = 0, MaxAttemptsPerDay = 3 }),
            Microsoft.Extensions.Options.Options.Create(new HrKpiDataSourceOptions()));
        for (var pass = 0; pass < 6; pass++)
            await capped.RunDueAsync(day.AddMinutes(30 * pass), CancellationToken.None);
        // Je Versuch eine Anfrage je Abfrage (503 wird im Abruf selbst nicht wiederholt): 3 Versuche x 2 Abfragen.
        Assert.Equal(6, always.Requests.Count);
    }

    [Fact]
    public async Task Testabruf_Zeigt_Blaetterschluessel_Und_Prueft_Wie_Der_Tagesdienst()
    {
        var folder = Path.Combine(_root, "hrdata");
        Directory.CreateDirectory(folder);
        // Bisherige Datei mit mehr Zeilen: Testabruf sagt „wuerde nicht geschrieben“, wie der Tagesdienst.
        WriteHandWorkbook(Path.Combine(folder, new HrKpiDataSourceOptions().GesamtFile), GesamtHeaders,
            Enumerable.Range(0, 10).SelectMany(_ => People().Select(HandRow)));
        var gesamt = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["data"] = People().Select(ApiRow),
            ["_links"] = new { self = "x" },
            ["_meta"] = new Dictionary<string, object?> { ["request_id"] = "req-p", ["items_per_page"] = 500, ["00004711"] = 1 }
        });
        // #777 ohne Kopfzeile, alle Werte Text: kein Wert darf im Bericht stehen.
        var faelle = """{"data":[["00001001","Muster, Anna","Krankheit","Genommen"]]}""";
        var rexx = new FakeRexx((r, _) => FakeRexx.Ok(r.RequestUri!.AbsolutePath.EndsWith("/775") ? gesamt : faelle));
        var probe = new RexxHubProbe(new FixedSource(), Client(rexx).Client,
            Microsoft.Extensions.Options.Options.Create(Options(enabled: false)),
            Microsoft.Extensions.Options.Options.Create(new HrKpiDataSourceOptions()), new Env(_root));

        var report = await probe.RunAsync(CancellationToken.None);
        var json = JsonSerializer.Serialize(report);

        var g = report.Abfragen[0];
        Assert.False(g.WuerdeGeschrieben);
        Assert.Equal(40, g.ZeilenBisherigeDatei);
        Assert.Contains("die bisherige Datei hat 40", g.Fehler);
        Assert.Equal(["request_id", "items_per_page", "(+1 nicht angezeigt)"], g.MetaSchluessel);
        Assert.Equal(["self"], g.LinksSchluessel);
        Assert.Equal((4, 500), (g.ZeilenErsteSeite, g.ZeilenJeSeiteLautMeta!.Value));
        Assert.Contains(g.Hinweise, x => x.StartsWith("Zeitraum von #775 nicht bestaetigt", StringComparison.Ordinal));
        var f = report.Abfragen[1];
        Assert.False(f.Ok);
        Assert.Contains("ohne erkennbare Kopfzeile", f.Fehler);
        Assert.DoesNotContain("Muster", json);
        Assert.DoesNotContain("1001", json);
        Assert.DoesNotContain("4711", json);
        Assert.DoesNotContain(Token, json);
    }

    // ---- Rundreise: API-Datei liest das Cockpit wie den Handexport ----

    private static readonly string[] GesamtHeaders =
    [
        "Personalnummer", "Nachname, Vorname (Link Personal)", "Personal Status", "Organisation", "Kostenstelle", "Stelle",
        "Eintrittsdatum", "Austrittsdatum", "Austrittsart", "Geburtsdatum", "Ø tägliche Sollarbeitszeit (Woche)", "Stunden Saldo",
        "Urlaubsanspruch", "Urlaub Rest", "Ferien ausstehend (Tage)", "Ferien angetreten (Tage)", "Krankheit angetreten (Stunden)",
        "Krankheit (über 60 Tage) angetreten (Stunden)", "Unfall angetreten (Stunden)", "Kompensation angetreten (Stunden)",
        "Militär / Zivildienst angetreten (Stunden)", "Krankheit angetreten (Zeitraum)", "Unfall angetreten (Zeitraum)"
    ];

    private static readonly string[] CaseHeaders =
    [
        "Personalnummer", "Abwesenheit von", "Abwesenheit bis", "Abwesenheit Tage", "Abwesenheit Status", "Abwesenheit Buchung",
        "Abwesenheit Tagessoll"
    ];

    /// <summary>Ein Arbeitstag mindestens eine Woche vor heute, damit Faelle im laufenden Jahr und in der Vergangenheit liegen.</summary>
    private static DateTime PastWorkday()
    {
        var day = DateTime.Today.AddDays(-7);
        while (ZurichWorkdayCalendar.CountWorkdays(day, day) == 0)
            day = day.AddDays(-1);
        return day;
    }

    private static string Iso(DateTime? date) => date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;
    private static string De(DateTime date) => date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

    /// <summary>Erfundene Person, einmal als Handexport-Zeile (Excel-Typen), einmal als API-Zeile (JSON-Typen).</summary>
    private sealed record Person(int Pernr, string Name, string Status, string Org, DateTime? Exit, string ExitType,
        decimal Rest, decimal Taken, string Sick, string Accident, DateTime? SickDay);

    private static readonly DateTime Entry = new(2020, 1, 1);
    private static readonly DateTime Birth = new(1990, 5, 5);

    private static Person[] People()
    {
        var day = PastWorkday();
        return
        [
            new(1001, "Muster, Anna", "Aktiv", "Org A, Org Z", null, "", 5, 10, "16:00", "0:00", day),
            new(1002, "Beispiel, Bruno", "Vertrag endend", "Org B", DateTime.Today.AddMonths(2), "", 1.5m, 6, "0:00", "8:00", null),
            new(1003, "Probe, Carla", "Ausgeschieden", "Org A", DateTime.Today.AddMonths(-1), "Kündigung AN", 0, 3, "0:00", "0:00", null),
            new(1004, "Test, Dora", "Aktiv", "Org B", null, "", 12, 0, "4:30", "0:00", null)
        ];
    }

    private static object?[] HandRow(Person p)
        => [p.Pernr, p.Name, p.Status, p.Org, "100 / " + p.Org, "Engineer", Entry, p.Exit, p.ExitType, Birth, 8.0, "-2:15",
            25, p.Rest, 2, p.Taken, p.Sick, "0:00", p.Accident, "4:00", "0:00",
            p.SickDay is { } d ? $"{De(d)} - {De(d)}" : "", ""];

    private static Dictionary<string, object?> ApiRow(Person p)
    {
        var hand = HandRow(p);
        var row = new Dictionary<string, object?>();
        for (var i = 0; i < GesamtHeaders.Length; i++)
        {
            row[GesamtHeaders[i]] = hand[i] switch
            {
                DateTime date => Iso(date),
                null => null,
                var other => other
            };
        }
        row["Personalnummer"] = p.Pernr.ToString("00000000", CultureInfo.InvariantCulture); // Rexx schreibt fuehrende Nullen
        return row;
    }

    private static Dictionary<string, object?> GesamtApiRow(int pernr, string status)
        => ApiRow(new Person(pernr, "Muster, Anna", status, "Org A", null, "", 1, 1, "0:00", "0:00", null));

    private static object?[][] CaseHandRows()
    {
        var day = PastWorkday();
        var holidayStart = day.AddDays(-14);
        return
        [
            [1001, day, day, 1, "Genommen", "Krankheit", "8:00"],
            [1004, holidayStart, holidayStart.AddDays(4), 5, "Genehmigt", "Ferien", "40:00"],
            [1004, day, day, 1, "Storniert", "Ferien", "8:00"],
            [1002, day, day, 1, "Genommen", "Unfall", "8:00"]
        ];
    }

    private static Dictionary<string, object?> CaseApiRow(int pernr, DateTime day, string type)
        => new() { ["Personalnummer"] = pernr.ToString("00000000"), ["Abwesenheit von"] = Iso(day), ["Abwesenheit bis"] = Iso(day),
            ["Abwesenheit Tage"] = 1, ["Abwesenheit Status"] = "Genommen", ["Abwesenheit Buchung"] = type, ["Abwesenheit Tagessoll"] = "8:00" };

    private static void WriteHandWorkbook(string path, string[] headers, IEnumerable<object?[]> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Sheet1");
        for (var column = 0; column < headers.Length; column++)
            sheet.Cell(1, column + 1).Value = headers[column];
        var line = 2;
        foreach (var row in rows)
        {
            for (var column = 0; column < row.Length; column++)
                sheet.Cell(line, column + 1).Value = XLCellValue.FromObject(row[column]);
            line++;
        }
        workbook.SaveAs(path);
    }

    private static string Snapshot(HrKpiResult r)
        => JsonSerializer.Serialize(new
        {
            r.Metrics, r.TurnoverMetrics, r.AbsenceMetrics, r.TimeVacationMetrics, r.PeriodComparisonMetrics, r.TrafficLights,
            r.Employees, r.Absences, r.Leavers, r.AbsenceCases, r.DataQualityIssues, r.Notices,
            Files = r.FileStatuses.Select(x => (x.Label, x.Exists, x.RowCount, x.Message))
        });

    [Fact]
    public async Task Rundreise_Die_API_Datei_Rechnet_Wie_Der_Handexport()
    {
        var handFolder = Path.Combine(_root, "hand");
        Directory.CreateDirectory(handFolder);
        var names = new HrKpiDataSourceOptions();
        WriteHandWorkbook(Path.Combine(handFolder, names.GesamtFile), GesamtHeaders, People().Select(HandRow));
        WriteHandWorkbook(Path.Combine(handFolder, names.RexxCaseFile), CaseHeaders, CaseHandRows());

        var gesamtJson = Envelope(People().Select(ApiRow));
        var caseJson = Envelope(CaseHandRows().Select(row =>
        {
            var api = new Dictionary<string, object?>();
            for (var i = 0; i < CaseHeaders.Length; i++)
                api[CaseHeaders[i]] = row[i] is DateTime d ? Iso(d) : row[i];
            api["Personalnummer"] = ((int)row[0]!).ToString("00000000", CultureInfo.InvariantCulture);
            return api;
        }));
        var rexx = new FakeRexx((r, _) => FakeRexx.Ok(r.RequestUri!.AbsolutePath.EndsWith("/775") ? gesamtJson : caseJson));
        await Service(rexx, new RecordingLog()).RunDueAsync(DateTime.Today.AddHours(23), CancellationToken.None);
        var apiFolder = Path.Combine(_root, "hrdata");
        Assert.True(File.Exists(Path.Combine(apiFolder, names.GesamtFile)));

        var service = new HrKpiService(Microsoft.Extensions.Options.Options.Create(new HrKpiDataSourceOptions()));
        var fromHand = await service.BuildAsync(new HrKpiOptions { DataFolder = handFolder });
        var fromApi = await service.BuildAsync(new HrKpiOptions { DataFolder = apiFolder });

        // Der Test waere wertlos, wenn beide leer rechneten.
        Assert.Equal([1001, 1002, 1004], fromApi.Employees.Select(x => x.Personalnummer!.Value).Order().ToArray());
        Assert.Contains(fromApi.Leavers, x => x.Personalnummer == 1003 && x.Austrittsdatum == DateTime.Today.AddMonths(-1));
        Assert.Contains(fromApi.AbsenceCases, x => x.Personalnummer == "00001004" && x.Abwesenheitsart == "0100");
        Assert.Equal(-2.25m, fromApi.Employees.First().StundenSaldo);
        Assert.Equal(Snapshot(fromHand), Snapshot(fromApi));
    }
}
