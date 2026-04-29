using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Services;
using TrafagSalesExporter.Services.DataSources;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddHttpClient(nameof(ExchangeRateImportService));

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite("Data Source=trafag_exporter.db;Default Timeout=60"));

// Stateless Infrastruktur- und Connector-Services: Singleton.
builder.Services.AddSingleton<IHanaQueryService, HanaQueryService>();
builder.Services.AddSingleton<IExcelExportService, ExcelExportService>();
builder.Services.AddSingleton<ISharePointUploadService, SharePointUploadService>();
builder.Services.AddSingleton<ISapGatewayService, SapGatewayService>();
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
builder.Services.AddSingleton<IManualExcelImportService, ManualExcelImportService>();
builder.Services.AddSingleton<IConsolidatedExportService, ConsolidatedExportService>();
builder.Services.AddSingleton<IExportLogService, ExportLogService>();
builder.Services.AddSingleton<ICentralSalesRecordService, CentralSalesRecordService>();
builder.Services.AddSingleton<IConfigTransferService, ConfigTransferService>();
builder.Services.AddSingleton<IDatabaseSchemaMaintenanceService, DatabaseSchemaMaintenanceService>();
builder.Services.AddSingleton<IDatabaseSeedService, DatabaseSeedService>();
builder.Services.AddSingleton<IDatabaseInitializationService, DatabaseInitializationService>();
builder.Services.AddSingleton<IUiTextService, UiTextService>();

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

var app = builder.Build();

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
app.UseAntiforgery();

app.MapRazorComponents<TrafagSalesExporter.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
