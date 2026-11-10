using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TrafagSalesExporter.Security;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>Testanmeldung mit Rolle: Name aus X-Test-User, Rolle aus X-Test-Role; ohne Header nicht angemeldet.</summary>
internal sealed class RexxHubTestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public RexxHubTestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var user = Request.Headers["X-Test-User"].ToString();
        if (user.Length == 0)
            return Task.FromResult(AuthenticateResult.NoResult());
        var claims = new List<Claim> { new(ClaimTypes.Name, user) };
        var role = Request.Headers["X-Test-Role"].ToString();
        if (role.Length > 0)
            claims.Add(new Claim(ClaimTypes.Role, role));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, "Test")));
    }
}

/// <summary>
/// Testabruf <c>/admin/rexxhub/test</c> auf echtem Kestrel-Host: nur fuer Admins, auch bei ausgeschaltetem Schalter,
/// schreibt nichts und gibt keine Werte aus. Registrierung ueber dieselbe <c>AddRexxHub</c> wie Program.cs.
/// </summary>
public sealed class RexxHubEndpointTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _http = null!;
    private RexxHubTests.FakeRexx _rexx = null!;
    private string _dir = null!;

    public async Task InitializeAsync()
    {
        _dir = Path.Combine(Path.GetTempPath(), "rexxhub_ep_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        var gesamt = JsonSerializer.Serialize(new
        {
            data = new[]
            {
                new Dictionary<string, object?> { ["Personalnummer"] = "00004711", ["Personal Status"] = "Aktiv", ["Nachname, Vorname (Link Personal)"] = "Muster, Anna" }
            },
            _meta = new { request_id = "req-ep-1" }
        });
        _rexx = new RexxHubTests.FakeRexx((r, _) => r.RequestUri!.AbsolutePath.EndsWith("/775")
            ? RexxHubTests.FakeRexx.Ok(gesamt)
            : RexxHubTests.FakeRexx.Status(404));

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = _dir, EnvironmentName = "Production" });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        // Der Test setzt den Schalter selbst und erbt nichts aus appsettings.json (LEARNINGS 2026-10-16).
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["HrKpi:RexxHub:Enabled"] = "false" });
        builder.Services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, RexxHubTestAuthHandler>("Test", _ => { });
        var security = new SecurityOptions { Enabled = true, AdminGroups = ["TestAdmins"] };
        builder.Services.AddAuthorization(o =>
        {
            o.FallbackPolicy = SecurityPolicyFactory.BuildAccessPolicy(security, false);
            o.AddPolicy(SecurityPolicies.AdminOnly, SecurityPolicyFactory.BuildAdminPolicy(security, false));
        });
        builder.Services.AddRexxHub(builder.Configuration);
        // Ersetzt Datenbank-Zugang und HTTP-Abruf durch Fakes; die zuletzt registrierte Implementierung gilt.
        builder.Services.AddSingleton<IRexxHubConnectionSource, RexxHubTests.FixedSource>();
        builder.Services.AddSingleton(new RexxHubClient(_rexx));
        builder.Services.AddSingleton<IAppEventLogService, RexxHubTests.RecordingLog>();
        // Admin-Freischaltung wie in Program.cs (AdminAccess, Cookie TrafagAdminUnlocked), mit erfundenem Passwort-Hash.
        builder.Services.AddHttpContextAccessor();
        builder.Services.Configure<AdminAccessOptions>(o =>
        {
            o.Enabled = true;
            o.Username = "admin";
            o.PasswordHash = AdminHash;
        });
        builder.Services.AddScoped<IAdminAccessService, AdminAccessService>();
        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapRexxHubTest();
        // Nur im Test: setzt das Freischalt-Cookie so, wie es /access/admin in Program.cs nach dem Admin-Login setzt.
        _app.MapPost("/test/unlock", (HttpContext c) =>
        {
            AccessUnlockCookie.SetUnlocked(c, AccessUnlockCookie.AdminCookieName, AdminHash);
            return Results.Ok();
        }).AllowAnonymous();
        await _app.StartAsync();
        // Ohne Cookie-Speicher: jeder Aufruf schickt nur das Cookie, das der Test ausdruecklich setzt.
        _http = new HttpClient(new HttpClientHandler { UseCookies = false }) { BaseAddress = new Uri(_app.Urls.First()) };
    }

    public async Task DisposeAsync()
    {
        _http.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
        try { Directory.Delete(_dir, true); } catch (IOException) { }
    }

    private static readonly string AdminHash = AccessPasswordSettingsWriter.HashPassword("erfundenes-testpasswort");

    private HttpRequestMessage Get(string? user, string? role = null, string? cookie = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, RexxHubRegistration.TestPath);
        if (user is not null)
            request.Headers.Add("X-Test-User", user);
        if (role is not null)
            request.Headers.Add("X-Test-Role", role);
        if (cookie is not null)
            request.Headers.Add("Cookie", cookie);
        return request;
    }

    private async Task<string> UnlockCookieAsync()
    {
        var response = await _http.PostAsync("/test/unlock", null);
        return response.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
    }

    [Fact]
    public async Task Ohne_Admin_Kein_Testabruf_Und_Keine_Anfrage_An_Rexx()
    {
        var cookie = await UnlockCookieAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await _http.SendAsync(Get(null, cookie: cookie))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _http.SendAsync(Get("TRAFAG\\Nutzer", cookie: cookie))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _http.SendAsync(Get("TRAFAG\\Nutzer", "AndereGruppe", cookie))).StatusCode);
        // Admin-Gruppe, aber ohne Admin-Freischaltung (oder mit gefaelschtem Cookie): ebenfalls gesperrt.
        var locked = await _http.SendAsync(Get("TRAFAG\\Admin", "TestAdmins"));
        Assert.Equal(HttpStatusCode.Forbidden, locked.StatusCode);
        Assert.Contains("Admin-Freischaltung fehlt", await locked.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Forbidden,
            (await _http.SendAsync(Get("TRAFAG\\Admin", "TestAdmins", AccessUnlockCookie.AdminCookieName + "=abc"))).StatusCode);
        Assert.Empty(_rexx.Requests);
    }

    [Fact]
    public async Task Admin_Sieht_Status_Spalten_Und_Zaehlwerte_Aber_Keine_Werte_Auch_Bei_Ausgeschaltetem_Schalter()
    {
        var response = await _http.SendAsync(Get("TRAFAG\\Admin", "TestAdmins", await UnlockCookieAsync()));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;
        Assert.False(root.GetProperty("Eingeschaltet").GetBoolean());
        var gesamt = root.GetProperty("Abfragen")[0];
        Assert.Equal("#775", gesamt.GetProperty("Abfrage").GetString());
        Assert.Equal(200, gesamt.GetProperty("HttpStatus").GetInt32());
        Assert.Equal(1, gesamt.GetProperty("Zeilen").GetInt32());
        Assert.Equal("req-ep-1", gesamt.GetProperty("RequestId").GetString());
        Assert.Contains("Nachname, Vorname (Link Personal)", gesamt.GetProperty("Spalten").EnumerateArray().Select(x => x.GetString()));
        Assert.Contains("Eintrittsdatum", gesamt.GetProperty("FehlendePflichtspalten").EnumerateArray().Select(x => x.GetString()));
        Assert.False(gesamt.GetProperty("WuerdeGeschrieben").GetBoolean());
        var faelle = root.GetProperty("Abfragen")[1];
        Assert.False(faelle.GetProperty("Ok").GetBoolean());
        Assert.Equal(404, faelle.GetProperty("HttpStatus").GetInt32());
        Assert.Contains("Abfrage-ID 777", faelle.GetProperty("Fehler").GetString());

        // Keine Werte (erfundene Personendaten) und kein Token in der Antwort, keine Datei geschrieben.
        Assert.DoesNotContain("Muster", body);
        Assert.DoesNotContain("4711", body);
        Assert.DoesNotContain(RexxHubTests.Token, body);
        Assert.False(Directory.Exists(Path.Combine(_dir, "hrdata")));
        Assert.Equal(2, _rexx.Requests.Count);
    }
}
