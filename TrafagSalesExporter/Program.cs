using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddHttpClient(nameof(ExchangeRateImportService));

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite("Data Source=trafag_exporter.db;Default Timeout=60"));

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
builder.Services.AddSingleton<ISiteExportService, SiteExportService>();
builder.Services.AddSingleton<IConsolidatedExportService, ConsolidatedExportService>();
builder.Services.AddSingleton<IExportLogService, ExportLogService>();
builder.Services.AddSingleton<ICentralSalesRecordService, CentralSalesRecordService>();
builder.Services.AddSingleton<IConfigTransferService, ConfigTransferService>();
builder.Services.AddSingleton<IDatabaseSchemaMaintenanceService, DatabaseSchemaMaintenanceService>();
builder.Services.AddSingleton<IDatabaseSeedService, DatabaseSeedService>();
builder.Services.AddSingleton<IDatabaseInitializationService, DatabaseInitializationService>();
builder.Services.AddSingleton<ISettingsPageService, SettingsPageService>();
builder.Services.AddSingleton<IStandortePageService, StandortePageService>();
builder.Services.AddSingleton<IStandorteSapEditorService, StandorteSapEditorService>();
builder.Services.AddSingleton<IManagementCockpitPageService, ManagementCockpitPageService>();
builder.Services.AddSingleton<IDashboardPageService, DashboardPageService>();
builder.Services.AddSingleton<ILogsPageService, LogsPageService>();
builder.Services.AddSingleton<ITransformationsPageService, TransformationsPageService>();
builder.Services.AddSingleton<IUiTextService, UiTextService>();
builder.Services.AddSingleton<ExportOrchestrationService>();
builder.Services.AddSingleton<TimerBackgroundService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<TimerBackgroundService>());

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
