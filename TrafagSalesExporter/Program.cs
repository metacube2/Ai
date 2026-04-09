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

builder.Services.AddSingleton<HanaQueryService>();
builder.Services.AddSingleton<ExcelExportService>();
builder.Services.AddSingleton<SharePointUploadService>();
builder.Services.AddSingleton<ExportOrchestrationService>();
builder.Services.AddSingleton<TimerBackgroundService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<TimerBackgroundService>());

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    using var db = await dbFactory.CreateDbContextAsync();
    await db.Database.EnsureCreatedAsync();
    AppDbContext.SeedIfEmpty(db);
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
