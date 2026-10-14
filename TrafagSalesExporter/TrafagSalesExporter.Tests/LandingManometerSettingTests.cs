using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Security;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>Startseite: Manometer per Admin-Schalter, Standard aus, in der DB gespeichert (2026-10-14).</summary>
public sealed class LandingManometerSettingTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly DbContextOptions<AppDbContext> _options;

    public LandingManometerSettingTests()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var db = new AppDbContext(_options);
        db.Database.EnsureCreated();
    }

    private sealed class Factory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
    }

    private sealed class Env : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    private LandingPageSettingsService Create()
        => new(Options.Create(new LandingPageOptions()), new Env(), new Factory(_options));

    [Fact]
    public void Standard_Aus_Und_Schalter_Bleibt_Gespeichert()
    {
        Assert.False(Create().ShowManometer);

        Create().SetShowManometer(true);
        Assert.True(Create().ShowManometer);    // neue Instanz liest aus der DB, wie nach einem Neustart

        Create().SetShowManometer(false);
        Assert.False(Create().ShowManometer);
    }

    public void Dispose() => _connection.Dispose();
}
