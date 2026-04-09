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

builder.Services.AddScoped<CryptoService>();
builder.Services.AddScoped<HanaQueryService>();
builder.Services.AddScoped<ExcelExportService>();
builder.Services.AddScoped<SharePointUploadService>();
builder.Services.AddScoped<ExportOrchestrationService>();
builder.Services.AddHostedService<TimerBackgroundService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    var cryptoService = scope.ServiceProvider.GetRequiredService<CryptoService>();
    await using var db = await dbFactory.CreateDbContextAsync();
    await db.Database.EnsureCreatedAsync();
    await DbInitializer.SeedDefaultsAsync(db, cryptoService);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<TrafagSalesExporter.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
