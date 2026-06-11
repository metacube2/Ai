using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MudBlazor.Services;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Security;
using TrafagSalesExporter.Services;
using TrafagSalesExporter.Services.DataSources;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

var securitySettings = builder.Configuration.GetSection(SecurityOptions.SectionName).Get<SecurityOptions>() ?? new SecurityOptions();
var useDevelopmentAuthentication = builder.Environment.IsDevelopment() && securitySettings.DevelopmentBypass;

if (useDevelopmentAuthentication)
{
    builder.Services
        .AddAuthentication(DevelopmentAuthenticationHandler.SchemeName)
        .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>(
            DevelopmentAuthenticationHandler.SchemeName,
            options => { });
}
else
{
    builder.Services.AddAuthentication(IISDefaults.AuthenticationScheme);
}

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = SecurityPolicyFactory.BuildAccessPolicy(securitySettings, useDevelopmentAuthentication);
    options.AddPolicy(SecurityPolicies.AdminOnly, SecurityPolicyFactory.BuildAdminPolicy(securitySettings, useDevelopmentAuthentication));
});

builder.Services.AddMudServices();
builder.Services.AddHttpClient(nameof(ExchangeRateImportService));
builder.Services.Configure<HrKpiDataSourceOptions>(builder.Configuration.GetSection(HrKpiDataSourceOptions.SectionName));
builder.Services.Configure<HrKpiAccessOptions>(builder.Configuration.GetSection(HrKpiAccessOptions.SectionName));
builder.Services.Configure<FinanceCockpitAccessOptions>(builder.Configuration.GetSection(FinanceCockpitAccessOptions.SectionName));
builder.Services.Configure<AdminAccessOptions>(builder.Configuration.GetSection(AdminAccessOptions.SectionName));
builder.Services.Configure<LandingPageOptions>(builder.Configuration.GetSection(LandingPageOptions.SectionName));

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite("Data Source=trafag_exporter.db;Default Timeout=60"));

// Stateless Infrastruktur- und Connector-Services: Singleton.
builder.Services.AddSingleton<IHanaQueryService, HanaQueryService>();
builder.Services.AddSingleton<IExcelExportService, ExcelExportService>();
builder.Services.AddSingleton<ISharePointUploadService, SharePointUploadService>();
builder.Services.AddSingleton<ISapGatewayService, SapGatewayService>();
builder.Services.AddSingleton<IMappedSalesRecordComposer, MappedSalesRecordComposer>();
builder.Services.AddSingleton<ISapCompositionService, SapCompositionService>();
builder.Services.AddSingleton<ITransformationStrategy, CopyTransformationStrategy>();
builder.Services.AddSingleton<ITransformationStrategy, UppercaseTransformationStrategy>();
builder.Services.AddSingleton<ITransformationStrategy, LowercaseTransformationStrategy>();
builder.Services.AddSingleton<ITransformationStrategy, PrefixTransformationStrategy>();
builder.Services.AddSingleton<ITransformationStrategy, SuffixTransformationStrategy>();
builder.Services.AddSingleton<ITransformationStrategy, ReplaceTransformationStrategy>();
builder.Services.AddSingleton<ITransformationStrategy, ConstantTransformationStrategy>();
builder.Services.AddSingleton<ITransformationStrategy, NormalizeCurrencyCodeTransformationStrategy>();
builder.Services.AddSingleton<ICurrencyExchangeRateService, CurrencyExchangeRateService>();
builder.Services.AddSingleton<IExchangeRateImportService, ExchangeRateImportService>();
builder.Services.AddSingleton<IRecordTransformationStrategy, FirstNonEmptyRecordTransformationStrategy>();
builder.Services.AddSingleton<IRecordTransformationStrategy, ConvertCurrencyRecordTransformationStrategy>();
builder.Services.AddSingleton<ITransformationCatalog, TransformationCatalog>();
builder.Services.AddSingleton<IRecordTransformationService, RecordTransformationService>();
builder.Services.AddSingleton<IAppEventLogService, AppEventLogService>();
builder.Services.AddSingleton<IManagementCockpitService, ManagementCockpitService>();
builder.Services.AddSingleton<IHrKpiService, HrKpiService>();
builder.Services.AddSingleton<IManualExcelImportService, ManualExcelImportService>();
builder.Services.AddSingleton<IExportAuditCsvService, ExportAuditCsvService>();
builder.Services.AddSingleton<IConsolidatedExportService, ConsolidatedExportService>();
builder.Services.AddSingleton<IExportLogService, ExportLogService>();
builder.Services.AddSingleton<ICentralSalesRecordService, CentralSalesRecordService>();
builder.Services.AddSingleton<ICentralSalesDataProvider, CentralSalesDataProvider>();
builder.Services.AddSingleton<IConfigTransferService, ConfigTransferService>();
builder.Services.AddSingleton<IFinanceReconciliationService, FinanceReconciliationService>();
builder.Services.AddSingleton<IDatabaseSchemaMaintenanceService, DatabaseSchemaMaintenanceService>();
builder.Services.AddSingleton<IDatabaseSeedService, DatabaseSeedService>();
builder.Services.AddSingleton<IDatabaseInitializationService, DatabaseInitializationService>();
builder.Services.AddSingleton<IUiTextService, UiTextService>();
builder.Services.AddSingleton<IAccessSessionTracker, AccessSessionTracker>();
builder.Services.AddSingleton<ILandingPageSettingsService, LandingPageSettingsService>();
builder.Services.AddSingleton<INavigationMenuService, NavigationMenuService>();

// Datenquellen-Adapter (Strategy per ConnectionKind).
builder.Services.AddSingleton<IDataSourceAdapter, HanaDataSourceAdapter>();
builder.Services.AddSingleton<IDataSourceAdapter, SapGatewayDataSourceAdapter>();
builder.Services.AddSingleton<IDataSourceAdapter, ManualExcelDataSourceAdapter>();
builder.Services.AddSingleton<IDataSourceAdapterResolver, DataSourceAdapterResolver>();
builder.Services.AddSingleton<ISiteExportService, SiteExportService>();

// Orchestrator mit gemeinsamem Status ueber alle Circuits.
builder.Services.AddSingleton<ExportOrchestrationService>();
builder.Services.AddSingleton<TimerBackgroundService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<TimerBackgroundService>());

// UI-/Page-Services: Scoped = pro Blazor-Circuit.
builder.Services.AddScoped<ISettingsPageService, SettingsPageService>();
builder.Services.AddScoped<IStandortePageService, StandortePageService>();
builder.Services.AddScoped<IStandorteSapEditorService, StandorteSapEditorService>();
builder.Services.AddScoped<IManagementCockpitPageService, ManagementCockpitPageService>();
builder.Services.AddScoped<IDashboardPageService, DashboardPageService>();
builder.Services.AddScoped<ILogsPageService, LogsPageService>();
builder.Services.AddScoped<ITransformationsPageService, TransformationsPageService>();
builder.Services.AddScoped<IFinanceRulesPageService, FinanceRulesPageService>();
builder.Services.AddScoped<IPurchasingDataSourcePageService, PurchasingDataSourcePageService>();
builder.Services.AddScoped<IPurchasingDashboardService, PurchasingDashboardService>();
builder.Services.AddScoped<IPurchasingDataRefreshService, PurchasingDataRefreshService>();
builder.Services.AddScoped<IHrKpiAccessService, HrKpiAccessService>();
builder.Services.AddScoped<IFinanceCockpitAccessService, FinanceCockpitAccessService>();
builder.Services.AddScoped<IAdminAccessService, AdminAccessService>();

var app = builder.Build();
var pathBase = app.Configuration["ASPNETCORE_PATHBASE"];
if (!string.IsNullOrWhiteSpace(pathBase))
{
    app.UsePathBase(pathBase.Trim());
}

using (var scope = app.Services.CreateScope())
{
    var databaseInitialization = scope.ServiceProvider.GetRequiredService<IDatabaseInitializationService>();
    await databaseInitialization.InitializeAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapPost("/access/finance", async (HttpContext httpContext, IOptions<FinanceCockpitAccessOptions> options) =>
{
    var form = await httpContext.Request.ReadFormAsync();
    var settings = options.Value;
    var username = form["username"].ToString();
    var password = form["password"].ToString();
    if (MatchesAccess(settings.Enabled, settings.Username, settings.PasswordHash, settings.Password, username, password))
        AccessUnlockCookie.SetUnlocked(httpContext, AccessUnlockCookie.FinanceCookieName, settings.PasswordHash);

    return Results.Redirect(ResolveReturnUrl(httpContext, form["returnUrl"].ToString()));
}).DisableAntiforgery();

app.MapPost("/access/admin", async (HttpContext httpContext, IOptions<AdminAccessOptions> options) =>
{
    var form = await httpContext.Request.ReadFormAsync();
    var settings = options.Value;
    var username = form["username"].ToString();
    var password = form["password"].ToString();
    if (MatchesAccess(settings.Enabled, settings.Username, settings.PasswordHash, settings.Password, username, password))
        AccessUnlockCookie.SetUnlocked(httpContext, AccessUnlockCookie.AdminCookieName, settings.PasswordHash);

    return Results.Redirect(ResolveReturnUrl(httpContext, form["returnUrl"].ToString()));
}).DisableAntiforgery();

app.MapPost("/access/hr", async (HttpContext httpContext, IOptions<HrKpiAccessOptions> options) =>
{
    var form = await httpContext.Request.ReadFormAsync();
    var settings = options.Value;
    var username = form["username"].ToString();
    var password = form["password"].ToString();
    if (MatchesAccess(settings.Enabled, settings.Username, settings.PasswordHash, settings.Password, username, password))
        AccessUnlockCookie.SetUnlocked(httpContext, AccessUnlockCookie.HrCookieName, settings.PasswordHash);

    return Results.Redirect(ResolveReturnUrl(httpContext, form["returnUrl"].ToString()));
}).DisableAntiforgery();

app.MapRazorComponents<TrafagSalesExporter.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();

static bool MatchesAccess(bool enabled, string configuredUsername, string configuredHash, string configuredPassword, string username, string password)
{
    if (!enabled)
        return true;

    if (string.IsNullOrWhiteSpace(username) ||
        string.IsNullOrEmpty(password) ||
        !string.Equals(username.Trim(), configuredUsername.Trim(), StringComparison.Ordinal))
    {
        return false;
    }

    return !string.IsNullOrWhiteSpace(configuredHash)
        ? string.Equals(AccessPasswordSettingsWriter.HashPassword(password), configuredHash.Trim(), StringComparison.Ordinal)
        : string.Equals(password, configuredPassword, StringComparison.Ordinal);
}

static string ResolveReturnUrl(HttpContext httpContext, string returnUrl)
{
    if (Uri.TryCreate(returnUrl, UriKind.Absolute, out var absolute) &&
        string.Equals(absolute.Host, httpContext.Request.Host.Host, StringComparison.OrdinalIgnoreCase))
    {
        return absolute.PathAndQuery;
    }

    if (Uri.TryCreate(returnUrl, UriKind.Relative, out _))
        return returnUrl;

    return $"{httpContext.Request.PathBase}/";
}
