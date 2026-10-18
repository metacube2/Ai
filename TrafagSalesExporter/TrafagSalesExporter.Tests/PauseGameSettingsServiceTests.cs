using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class PauseGameSettingsServiceTests : IDisposable
{
    private readonly string _root;

    public PauseGameSettingsServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "pausegame-settings-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { }
    }

    private (PauseGameSettingsService Service, PauseGameOptions Options, string Path) Create(string existingJson)
    {
        var path = Path.Combine(_root, "appsettings.json");
        File.WriteAllText(path, existingJson, new UTF8Encoding(false));
        var options = new PauseGameOptions();
        var service = new PauseGameSettingsService(Options.Create(options), new StubEnvironment(_root));
        return (service, options, path);
    }

    [Fact]
    public void Pause_Game_Is_Off_Until_Somebody_Turns_It_On()
    {
        // Der Reiter darf nicht von selbst auftauchen: ohne Eintrag in der
        // Konfiguration bleibt er aus.
        var (service, _, _) = Create("{}");
        Assert.False(service.Enabled);
    }

    [Fact]
    public void Toggling_Writes_The_Flag_And_Keeps_Every_Other_Setting()
    {
        // Der Schalter schreibt in dieselbe appsettings.json, in der auch die
        // Passworthashes und die HR-Pfade stehen. Ginge dabei etwas verloren, waere
        // der Schaden deutlich groesser als ein fehlender Spielreiter.
        var before = """
            {
              "Security": { "Enabled": false, "AccessGroups": [ "TRAFAG\\Users" ] },
              "FinanceCockpitAccess": { "Username": "finance", "PasswordHash": "ABC123" },
              "LandingPage": { "ShowWalkingLabFigure": false }
            }
            """;
        var (service, options, path) = Create(before);

        service.SetEnabled(true);

        Assert.True(service.Enabled);
        Assert.True(options.Enabled);          // wirkt sofort, ohne Neustart

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement;
        Assert.True(root.GetProperty("Pause").GetProperty("Enabled").GetBoolean());
        Assert.Equal("ABC123", root.GetProperty("FinanceCockpitAccess").GetProperty("PasswordHash").GetString());
        Assert.Equal("finance", root.GetProperty("FinanceCockpitAccess").GetProperty("Username").GetString());
        Assert.False(root.GetProperty("LandingPage").GetProperty("ShowWalkingLabFigure").GetBoolean());
        Assert.Equal("TRAFAG\\Users", root.GetProperty("Security").GetProperty("AccessGroups")[0].GetString());
    }

    [Fact]
    public void Turning_It_Off_Again_Survives_A_Restart()
    {
        // Ein Neustart liest die Datei neu. Der Schalter muss also wirklich in der
        // Datei stehen und nicht nur im Speicher.
        var (service, _, path) = Create("""{ "Pause": { "Enabled": true } }""");

        service.SetEnabled(false);

        var fresh = new PauseGameSettingsService(Options.Create(ReadOptions(path)), new StubEnvironment(_root));
        Assert.False(fresh.Enabled);
    }

    [Fact]
    public void An_Existing_Pause_Section_Is_Updated_Not_Duplicated()
    {
        var (service, _, path) = Create("""{ "Pause": { "Enabled": false, "Kommentar": "bleibt" } }""");

        service.SetEnabled(true);

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var pause = doc.RootElement.GetProperty("Pause");
        Assert.True(pause.GetProperty("Enabled").GetBoolean());
        Assert.Equal("bleibt", pause.GetProperty("Kommentar").GetString());
        Assert.Equal(2, pause.EnumerateObject().Count());
    }

    private static PauseGameOptions ReadOptions(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var enabled = doc.RootElement.TryGetProperty("Pause", out var section)
                      && section.TryGetProperty("Enabled", out var flag)
                      && flag.GetBoolean();
        return new PauseGameOptions { Enabled = enabled };
    }

    private sealed class StubEnvironment : IHostEnvironment
    {
        public StubEnvironment(string root) => ContentRootPath = root;
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; }
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
