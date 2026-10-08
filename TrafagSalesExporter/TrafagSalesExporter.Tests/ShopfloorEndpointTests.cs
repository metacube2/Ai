using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TrafagSalesExporter.Services.Shopfloor;

namespace TrafagSalesExporter.Tests;

/// <summary>Testanmeldung: Benutzername aus dem Header X-Test-User, ohne Header nicht angemeldet.</summary>
internal sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var u = Request.Headers["X-Test-User"].ToString();
        if (u.Length == 0)
            return Task.FromResult(AuthenticateResult.NoResult());
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, u)], "Test"));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, "Test")));
    }
}

/// <summary>Echter Kestrel-Host auf Port 0: Zugriffssperre, Pflichtheader, Token-Schnittstelle und JSON-Vertraege.</summary>
public sealed class ShopfloorEndpointTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _http = null!;
    private string _dir = null!;

    public async Task InitializeAsync()
    {
        _dir = Path.Combine(Path.GetTempPath(), "sf_ep_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = ShopfloorTestEnv.ProjectRoot });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Shopfloor:DatabasePath"] = Path.Combine(_dir, "shopfloor.db"),
            ["Shopfloor:AllowedUsers:0"] = "tester",
            ["Shopfloor:ApiToken"] = "geheim",
            ["Shopfloor:LoginUsername"] = "operations",
            ["Shopfloor:LoginPasswordHash"] = TrafagSalesExporter.Services.AccessPasswordSettingsWriter.HashPassword("testpasswort")
        });
        // Wie Program.cs: Anmeldung (hier Testschema statt Windows) und FallbackPolicy "angemeldet", sonst wuerde
        // der Test die Ablehnung durch UseAuthorization nie sehen.
        builder.Services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization(o => o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        builder.Services.AddShopfloor(builder.Configuration);
        _app = builder.Build();
        _app.UseShopfloorAccess();       // vor UseStaticFiles, also vor UseAuthentication
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapShopfloor();
        await _app.StartAsync();
        // Ohne automatische Umleitung und ohne Cookie-Speicher, damit die Tests Location und Set-Cookie sehen.
        _http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false }) { BaseAddress = new Uri(_app.Urls.First()) };
    }

    public async Task DisposeAsync()
    {
        _http.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { Directory.Delete(_dir, true); } catch (IOException) { }
    }

    private HttpRequestMessage Req(HttpMethod m, string url, string? user = "TRAFAG\\Tester", string? body = null, bool xUser = true, string? bearer = null)
    {
        var r = new HttpRequestMessage(m, url);
        if (user is not null) r.Headers.Add("X-Test-User", user);
        if (xUser) r.Headers.Add("X-User", "x");
        if (bearer is not null) r.Headers.Add("Authorization", "Bearer " + bearer);
        if (body is not null) r.Content = new StringContent(body, Encoding.UTF8, "application/json");
        return r;
    }

    [Fact]
    public async Task Ohne_Freigabe_Oder_Anmeldung_Ist_Alles_Gesperrt()
    {
        Assert.Equal(HttpStatusCode.Forbidden, (await _http.SendAsync(Req(HttpMethod.Get, "/shopfloor/api/config", user: null))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _http.SendAsync(Req(HttpMethod.Get, "/shopfloor/api/config", user: "TRAFAG\\fremder"))).StatusCode);
        // Seit 2026-10-08 fuehren Seiten ohne Zugriff zur Anmeldung (Login analog Finance/HR).
        var page = await _http.SendAsync(Req(HttpMethod.Get, "/shopfloor/index.html", user: "TRAFAG\\fremder"));
        Assert.Equal(HttpStatusCode.Redirect, page.StatusCode);
        Assert.EndsWith("/shopfloor/login.html", page.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Login_Mit_Benutzer_Und_Passwort_Oeffnet_Den_Zugriff_Per_Cookie()
    {
        var falsch = await _http.SendAsync(Req(HttpMethod.Post, "/shopfloor/api/login", user: "TRAFAG\\fremder",
            body: """{"user":"operations","password":"falsch"}"""));
        Assert.Equal(HttpStatusCode.Unauthorized, falsch.StatusCode);
        Assert.False(falsch.Headers.Contains("Set-Cookie"));

        var ok = await _http.SendAsync(Req(HttpMethod.Post, "/shopfloor/api/login", user: "TRAFAG\\fremder",
            body: """{"user":"Operations","password":"testpasswort"}"""));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var cookie = ok.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
        Assert.StartsWith(ShopfloorLogin.CookieName + "=", cookie);

        var mitCookie = Req(HttpMethod.Get, "/shopfloor/api/config", user: "TRAFAG\\fremder");
        mitCookie.Headers.Add("Cookie", cookie);
        Assert.Equal(HttpStatusCode.OK, (await _http.SendAsync(mitCookie)).StatusCode);

        var gefaelscht = Req(HttpMethod.Get, "/shopfloor/api/config", user: "TRAFAG\\fremder");
        gefaelscht.Headers.Add("Cookie", ShopfloorLogin.CookieName + "=abc");
        Assert.Equal(HttpStatusCode.Forbidden, (await _http.SendAsync(gefaelscht)).StatusCode);
    }

    [Fact]
    public async Task Freigegebener_Benutzer_Sieht_Konfiguration_Und_Schreibt_Mit_Windows_Konto()
    {
        var cfg = await _http.SendAsync(Req(HttpMethod.Get, "/shopfloor/api/config"));
        Assert.Equal(HttpStatusCode.OK, cfg.StatusCode);
        Assert.NotNull(JsonNode.Parse(await cfg.Content.ReadAsStringAsync())!["modules"]);

        var who = JsonNode.Parse(await (await _http.SendAsync(Req(HttpMethod.Get, "/shopfloor/api/whoami"))).Content.ReadAsStringAsync())!;
        Assert.Equal("tester", (string)who["user"]!);

        var post = await _http.SendAsync(Req(HttpMethod.Post, "/shopfloor/api/records", body: """{"module":"pendenzen","changes":{"datum":"2026-10-05","beschreibung":"Ep"}}"""));
        var postText = await post.Content.ReadAsStringAsync();
        Assert.True(post.StatusCode == HttpStatusCode.OK && postText.Length > 0, $"{(int)post.StatusCode} [{postText}] {post.Headers} {post.Content.Headers}");
        var rec = JsonNode.Parse(postText)!;
        Assert.Equal("tester", (string)rec["created_by"]!);       // Windows-Konto, nicht der Header

        var list = JsonNode.Parse(await (await _http.SendAsync(Req(HttpMethod.Get, "/shopfloor/api/records?module=pendenzen"))).Content.ReadAsStringAsync())!.AsArray();
        Assert.Contains(list, x => (string)x!["nr"]! == (string)rec["nr"]!);   // zusaetzlich zum Startstand aus dem Excel

        var bad = await _http.SendAsync(Req(HttpMethod.Get, "/shopfloor/api/records?module=nix"));
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        Assert.Equal("Modul unbekannt", (string)JsonNode.Parse(await bad.Content.ReadAsStringAsync())!["error"]!);

        var del = await _http.SendAsync(Req(HttpMethod.Delete, $"/shopfloor/api/records/{(long)rec["id"]!}"));
        Assert.Equal(HttpStatusCode.OK, del.StatusCode);
        var csv = await _http.SendAsync(Req(HttpMethod.Get, "/shopfloor/api/export.csv?module=pendenzen"));
        Assert.StartsWith("text/csv", csv.Content.Headers.ContentType!.ToString());
    }

    [Fact]
    public async Task Aenderungen_Ohne_Pflichtheader_Werden_Abgelehnt()
    {
        var r = await _http.SendAsync(Req(HttpMethod.Post, "/shopfloor/api/records", body: """{"module":"pendenzen","changes":{}}""", xUser: false));
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task Schnittstelle_Verlangt_Das_Bearer_Token_Und_Braucht_Keinen_Freigegebenen_Benutzer()
    {
        const string body = """{"source":"sap","items":[{"module":"tx","day":"2026-10-05","fields":{"vorrat_total":100}}]}""";

        var none = await _http.SendAsync(Req(HttpMethod.Post, "/shopfloor/api/integration/push", body: body));
        Assert.Equal(HttpStatusCode.Unauthorized, none.StatusCode);

        var wrong = await _http.SendAsync(Req(HttpMethod.Post, "/shopfloor/api/integration/push", user: "TRAFAG\\fremder", body: body, bearer: "falsch"));
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);   // falsches Token zaehlt nie als Freigabe

        // Ohne jeden Windows-Login (SAP/MES) nur mit Token: muss auch die FallbackPolicy passieren (AllowAnonymous).
        var anonym = await _http.SendAsync(Req(HttpMethod.Post, "/shopfloor/api/integration/push", user: null, body: body, xUser: false, bearer: "geheim"));
        Assert.Equal(HttpStatusCode.OK, anonym.StatusCode);

        var ok = await _http.SendAsync(Req(HttpMethod.Post, "/shopfloor/api/integration/push", user: "TRAFAG\\fremder", body: body, xUser: false, bearer: "geheim"));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.True((bool)JsonNode.Parse(await ok.Content.ReadAsStringAsync())!["results"]![0]!["ok"]!);
    }
}
