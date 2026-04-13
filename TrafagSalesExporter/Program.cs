using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite("Data Source=trafag_exporter.db"));

builder.Services.AddSingleton<IHanaQueryService, HanaQueryService>();
builder.Services.AddSingleton<IExcelExportService, ExcelExportService>();
builder.Services.AddSingleton<ISharePointUploadService, SharePointUploadService>();
builder.Services.AddSingleton<ITransformationStrategy, CopyTransformationStrategy>();
builder.Services.AddSingleton<ITransformationStrategy, UppercaseTransformationStrategy>();
builder.Services.AddSingleton<ITransformationStrategy, LowercaseTransformationStrategy>();
builder.Services.AddSingleton<ITransformationStrategy, PrefixTransformationStrategy>();
builder.Services.AddSingleton<ITransformationStrategy, SuffixTransformationStrategy>();
builder.Services.AddSingleton<ITransformationStrategy, ReplaceTransformationStrategy>();
builder.Services.AddSingleton<ITransformationStrategy, ConstantTransformationStrategy>();
builder.Services.AddSingleton<IRecordTransformationService, RecordTransformationService>();
builder.Services.AddSingleton<ISiteExportService, SiteExportService>();
builder.Services.AddSingleton<IConsolidatedExportService, ConsolidatedExportService>();
builder.Services.AddSingleton<IExportLogService, ExportLogService>();
builder.Services.AddSingleton<IDatabaseInitializationService, DatabaseInitializationService>();
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
