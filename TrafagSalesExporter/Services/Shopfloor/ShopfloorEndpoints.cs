using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace TrafagSalesExporter.Services.Shopfloor;

/// <summary>Liefert den Store beim ersten Zugriff (Datenbank wird erst dann angelegt) und bildet die Pfade auf dem Server.</summary>
public sealed class ShopfloorStoreProvider
{
    private readonly Lazy<ShopfloorStore> _store;
    private readonly IHostEnvironment _env;
    private readonly IOptionsMonitor<ShopfloorOptions> _options;

    public ShopfloorStoreProvider(IHostEnvironment env, IOptionsMonitor<ShopfloorOptions> options)
    {
        _env = env;
        _options = options;
        _store = new Lazy<ShopfloorStore>(Create, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public ShopfloorStore Store => _store.Value;

    public string DatabasePath => Resolve(_options.CurrentValue.DatabasePath, Path.Combine("shopfloor", "shopfloor.db"));

    public string BackupDirectory
        => string.IsNullOrWhiteSpace(_options.CurrentValue.BackupDirectory)
            ? Path.Combine(Path.GetDirectoryName(DatabasePath) ?? _env.ContentRootPath, "backups")
            : Resolve(_options.CurrentValue.BackupDirectory, "backups");

    private string Resolve(string configured, string fallbackRelative)
    {
        var p = string.IsNullOrWhiteSpace(configured) ? fallbackRelative : configured.Trim();
        return Path.IsPathRooted(p) ? p : Path.Combine(_env.ContentRootPath, p);
    }

    private ShopfloorStore Create()
    {
        var root = Path.Combine(_env.ContentRootPath, "Services", "Shopfloor");
        var config = ShopfloorConfig.Load(Path.Combine(root, "shopfloor_config.json"));
        var db = new ShopfloorDb(DatabasePath);
        db.Initialize(Path.Combine(root, "seed", "shopfloor.seed.db"));
        return new ShopfloorStore(db, config, () => _options.CurrentValue.BaseUrl ?? string.Empty);
    }
}

/// <summary>Tagessicherung der Datenbank (stuendliche Pruefung, eine Kopie pro Tag, 60 Tage Aufbewahrung).</summary>
public sealed class ShopfloorBackupService : BackgroundService
{
    private readonly ShopfloorStoreProvider _provider;
    private readonly IOptionsMonitor<ShopfloorOptions> _options;
    private readonly ILogger<ShopfloorBackupService> _logger;

    public ShopfloorBackupService(ShopfloorStoreProvider provider, IOptionsMonitor<ShopfloorOptions> options, ILogger<ShopfloorBackupService> logger)
    {
        _provider = provider;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken).ConfigureAwait(false);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var o = _options.CurrentValue;
                // Solange niemand freigegeben ist, wird weder eine Datenbank angelegt noch gesichert.
                if (o.OpenForAll || o.AllowedUsers.Count > 0)
                    _provider.Store.Db.Backup(_provider.BackupDirectory, o.BackupKeepDays);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Shopfloor-Sicherung fehlgeschlagen.");
            }
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken).ConfigureAwait(false);
        }
    }
}

public static class ShopfloorEndpoints
{
    public const string Root = "/shopfloor";
    public const string ApiRoot = "/shopfloor/api";
    public const string IndexPath = "shopfloor/index.html";
    private const string NoAccessText = "Kein Zugriff. Die Freigabe durch die Leitung Produktion/Operations steht noch aus.";

    public static IServiceCollection AddShopfloor(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ShopfloorOptions>(configuration.GetSection(ShopfloorOptions.SectionName));
        services.AddSingleton<IShopfloorAccess, ShopfloorAccess>();
        services.AddSingleton<ShopfloorStoreProvider>();
        services.AddHostedService<ShopfloorBackupService>();
        services.AddSingleton<ShopfloorSapSync>();
        services.AddHostedService(sp => sp.GetRequiredService<ShopfloorSapSync>());
        return services;
    }

    /// <summary>
    /// Zugriffssperre fuer die statischen Dateien und die API unter /shopfloor. Muss VOR UseStaticFiles stehen.
    /// Authentifiziert selbst, weil UseAuthentication erst danach laeuft. /shopfloor/ wird auf index.html umgeleitet.
    /// </summary>
    public static IApplicationBuilder UseShopfloorAccess(this IApplicationBuilder app)
    {
        return app.Use(async (ctx, next) =>
        {
            if (!ctx.Request.Path.StartsWithSegments(Root, StringComparison.OrdinalIgnoreCase))
            {
                await next().ConfigureAwait(false);
                return;
            }

            // Die Schnittstelle prueft ihr Bearer-Token selbst und braucht keinen freigegebenen Windows-Benutzer.
            var isPush = ctx.Request.Path.Equals(ApiRoot + "/integration/push", StringComparison.OrdinalIgnoreCase)
                         && ctx.Request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);
            if (!isPush)
            {
                var access = ctx.RequestServices.GetRequiredService<IShopfloorAccess>();
                System.Security.Claims.ClaimsPrincipal? user = ctx.User;
                if (user?.Identity?.IsAuthenticated != true)
                {
                    try
                    {
                        user = (await ctx.AuthenticateAsync().ConfigureAwait(false)).Principal ?? user;
                    }
                    catch (InvalidOperationException)
                    {
                        // kein Standard-Schema konfiguriert: gilt als nicht angemeldet
                    }
                }
                if (!access.IsAllowed(user))
                {
                    ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                    ctx.Response.ContentType = "text/plain; charset=utf-8";
                    await ctx.Response.WriteAsync(NoAccessText).ConfigureAwait(false);
                    return;
                }
            }

            if (ctx.Request.Path.Equals(Root, StringComparison.OrdinalIgnoreCase) || ctx.Request.Path.Equals(Root + "/", StringComparison.OrdinalIgnoreCase))
                ctx.Request.Path = Root + "/index.html";
            await next().ConfigureAwait(false);
        });
    }

    // ------------------------------------------------------------ Hilfen
    private static IResult J(HttpContext ctx, JsonNode? node, int status = 200)
    {
        ctx.Response.Headers.CacheControl = "no-store";
        return Results.Text(SfJson.Dump(node), "application/json; charset=utf-8", Encoding.UTF8, status);
    }

    private static async Task<JsonObject> Body(HttpContext ctx)
    {
        using var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8);
        var text = await reader.ReadToEndAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(text))
            return new JsonObject();
        try
        {
            return JsonNode.Parse(text) as JsonObject ?? new JsonObject();
        }
        catch (JsonException)
        {
            throw new ShopfloorException("Ungültiges JSON");
        }
    }

    private static string Editor(HttpContext ctx)
        => ctx.RequestServices.GetRequiredService<IShopfloorAccess>().EditorName(ctx.User);

    private static ShopfloorStore Store(HttpContext ctx) => ctx.RequestServices.GetRequiredService<ShopfloorStoreProvider>().Store;

    private static string Today() => DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static bool TokenOk(HttpContext ctx, string configured)
    {
        if (string.IsNullOrEmpty(configured))
            return false;
        var expected = Encoding.UTF8.GetBytes("Bearer " + configured);
        var given = Encoding.UTF8.GetBytes(ctx.Request.Headers.Authorization.ToString());
        return given.Length == expected.Length && CryptographicOperations.FixedTimeEquals(given, expected);
    }

    private static async ValueTask<object?> Guard(EndpointFilterInvocationContext c, EndpointFilterDelegate next)
    {
        var ctx = c.HttpContext;
        try
        {
            // Aenderungen nur mit dem Header, den die Oberflaeche immer mitschickt. Eine fremde Webseite kann ihn
            // ohne Vorabfrage (CORS) nicht setzen, das schliesst Fremdaufrufe mit dem Windows-Login des Benutzers aus.
            var isPush = ctx.Request.Path.Equals(ApiRoot + "/integration/push", StringComparison.OrdinalIgnoreCase);
            if (!isPush && !HttpMethods.IsGet(ctx.Request.Method) && !ctx.Request.Headers.ContainsKey("X-User"))
                return J(ctx, new JsonObject { ["error"] = "Ungültige Anfrage" }, StatusCodes.Status403Forbidden);

            if (!isPush)
            {
                var access = ctx.RequestServices.GetRequiredService<IShopfloorAccess>();
                if (!access.IsAllowed(ctx.User))
                    return Results.Text(NoAccessText, "text/plain; charset=utf-8", Encoding.UTF8, StatusCodes.Status403Forbidden);
            }
            return await next(c).ConfigureAwait(false);
        }
        catch (ShopfloorException e)
        {
            return J(ctx, new JsonObject { ["error"] = e.Message }, e.StatusCode);
        }
        catch (Exception e)
        {
            ctx.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Shopfloor").LogError(e, "Shopfloor-Anfrage fehlgeschlagen: {Path}", ctx.Request.Path);
            return J(ctx, new JsonObject { ["error"] = e.Message }, StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>Alle Endpunkte von server.py unter /shopfloor/api, mit denselben JSON-Formen.</summary>
    public static WebApplication MapShopfloor(this WebApplication app)
    {
        var g = app.MapGroup(ApiRoot).DisableAntiforgery();
        g.AddEndpointFilter(Guard);

        // --- Lesen
        g.MapGet("/config", (HttpContext c) =>
        {
            c.Response.Headers.CacheControl = "no-store";
            return Results.Text(Store(c).Config.RawJson, "application/json; charset=utf-8", Encoding.UTF8);
        });
        g.MapGet("/whoami", (HttpContext c) => J(c, new JsonObject { ["user"] = Editor(c), ["name"] = c.User.Identity?.Name }));
        // SAP-Abgleich (ShopZd05Set, ShopAufSet), 2026-10-07: Status und Abgleich auf Knopfdruck.
        g.MapGet("/sap/status", (HttpContext c) => J(c, c.RequestServices.GetRequiredService<ShopfloorSapSync>().LastResult.DeepClone()));
        g.MapPost("/sap/sync", async Task<IResult> (HttpContext c) =>
        {
            try
            {
                var result = await c.RequestServices.GetRequiredService<ShopfloorSapSync>().RunAsync(c.RequestAborted);
                return J(c, result.DeepClone());
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return J(c, new JsonObject { ["ok"] = false, ["meldung"] = ex.GetBaseException().Message }, 502);
            }
        });
        g.MapGet("/records", (HttpContext c) => J(c, Store(c).GetRecords(c.Request.Query["module"])));
        g.MapGet("/daily", (HttpContext c) => J(c, Store(c).GetDaily(c.Request.Query["module"], c.Request.Query["from"], c.Request.Query["to"])));
        g.MapGet("/agenda", (HttpContext c) =>
        {
            var day = c.Request.Query["day"].ToString();
            return J(c, Store(c).AgendaStatus(string.IsNullOrEmpty(day) ? Today() : day));
        });
        g.MapGet("/history", (HttpContext c) => J(c, Store(c).GetHistory(c.Request.Query["module"], c.Request.Query["ref"])));
        g.MapGet("/export.csv", (HttpContext c) =>
        {
            var module = c.Request.Query["module"].ToString();
            var bytes = Store(c).ExportCsv(module);
            return Results.File(bytes, "text/csv; charset=utf-8", $"shopfloor_{module}_{Today()}.csv");
        });
        g.MapGet("/zd05", (HttpContext c) => J(c, Store(c).GetZd05(c.Request.Query["day"])));
        g.MapGet("/zd05/history", (HttpContext c) => J(c, Store(c).GetZd05History()));
        g.MapGet("/people", (HttpContext c) => J(c, Store(c).GetPeople()));
        g.MapGet("/mail", (HttpContext c) => J(c, Store(c).GetMail()));
        g.MapGet("/mail/preview", (HttpContext c) =>
            Results.Text(Store(c).MailPreviewHtml(c.Request.Query["id"]) ?? "nicht gefunden", "text/html; charset=utf-8", Encoding.UTF8));
        g.MapGet("/forecast", (HttpContext c) => J(c, Store(c).GetForecast(c.Request.Query["monday"])));
        g.MapGet("/integration/schema", (HttpContext c) => J(c, Store(c).IntegrationSchema()));

        // --- Schreiben
        g.MapPost("/records", async Task<IResult> (HttpContext c) => J(c, Store(c).PostRecord(await Body(c), Editor(c))));
        g.MapPost("/daily", async Task<IResult> (HttpContext c) => J(c, Store(c).PostDaily(await Body(c), Editor(c))));
        g.MapDelete("/records/{id:long}", (HttpContext c, long id) =>
        {
            Store(c).DeleteRecord(id, Editor(c));
            return J(c, new JsonObject { ["ok"] = true });
        });
        g.MapPost("/zd05/{action}", async Task<IResult> (HttpContext c, string action) => J(c, Store(c).PostZd05(action, await Body(c), Editor(c))));
        g.MapPost("/forecast/{action}", async Task<IResult> (HttpContext c, string action) => J(c, Store(c).PostForecast(action, await Body(c), Editor(c))));
        g.MapPost("/people", async Task<IResult> (HttpContext c) => J(c, Store(c).PostMail("people", await Body(c), Editor(c))));
        g.MapPost("/mail/{action}", async Task<IResult> (HttpContext c, string action) => J(c, Store(c).PostMail(action, await Body(c), Editor(c))));

        // --- Schnittstelle fuer SAP / MES / MCP: Bearer-Token statt Benutzerfreigabe
        // AllowAnonymous, weil die FallbackPolicy des Cockpits sonst jeden Aufruf ohne Windows-Login abweisen wuerde,
        // bevor das Token geprueft wird. Die Token-Pruefung im Endpunkt ist die Zugangskontrolle dieses Pfads.
        var push = app.MapGroup(ApiRoot + "/integration").DisableAntiforgery().AllowAnonymous();
        push.AddEndpointFilter(Guard);
        push.MapPost("/push", async Task<IResult> (HttpContext c, IOptionsMonitor<ShopfloorOptions> options) =>
        {
            if (!TokenOk(c, options.CurrentValue.ApiToken))
                return J(c, new JsonObject { ["error"] = "Token fehlt oder ungültig" }, StatusCodes.Status401Unauthorized);
            var b = await Body(c);
            var source = SfJson.Truthy(SfJson.Get(b, "source")) ? SfJson.PyStr(SfJson.Get(b, "source")) : "extern";
            var items = SfJson.Get(b, "items") as JsonArray ?? [];
            var results = Store(c).ApplyPush(new ShopfloorPush { Items = items }, source);
            return J(c, new JsonObject { ["results"] = results });
        });
        return app;
    }
}
