using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Pausenspiel-Reiter. Standard ist AUS: der Eintrag taucht links nicht auf und die
/// Seite laedt das Spiel nicht. Eingeschaltet wird unter Admin &gt; Settings.
///
/// Drei Ebenen zum Ausblenden, von hart nach weich:
/// 1. <see cref="Enabled"/> = false -> kein Menueintrag, und die Seite zeigt nur einen Hinweis.
/// 2. Menueintrag "pause-game" auf IsVisible = false -> Eintrag weg, Route bleibt.
/// 3. RequiredPolicy am Menueintrag -> Eintrag nur fuer bestimmte Personen.
/// </summary>
public sealed class PauseGameOptions
{
    public const string SectionName = "Pause";

    public bool Enabled { get; set; }
}

public interface IPauseGameSettingsService
{
    bool Enabled { get; }
    void SetEnabled(bool value);
}

/// <summary>
/// Schreibt den Schalter zurueck nach appsettings.json - dasselbe Vorgehen wie
/// <see cref="LandingPageSettingsService"/>. Die Options-Instanz ist ein Singleton,
/// die Aenderung wirkt also sofort und ohne Neustart.
/// </summary>
public sealed class PauseGameSettingsService : IPauseGameSettingsService
{
    private static readonly object FileLock = new();
    private readonly PauseGameOptions _options;
    private readonly IHostEnvironment _environment;

    public PauseGameSettingsService(IOptions<PauseGameOptions> options, IHostEnvironment environment)
    {
        _options = options.Value;
        _environment = environment;
    }

    public bool Enabled => _options.Enabled;

    public void SetEnabled(bool value)
    {
        _options.Enabled = value;
        SaveSetting(value);
    }

    private void SaveSetting(bool value)
    {
        var path = Path.Combine(_environment.ContentRootPath, "appsettings.json");

        lock (FileLock)
        {
            var json = File.Exists(path)
                ? File.ReadAllText(path, Encoding.UTF8)
                : "{}";

            var root = JsonNode.Parse(json)?.AsObject() ?? new JsonObject();
            var section = root[PauseGameOptions.SectionName] as JsonObject;
            if (section is null)
            {
                section = new JsonObject();
                root[PauseGameOptions.SectionName] = section;
            }

            section[nameof(PauseGameOptions.Enabled)] = value;
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(path, root.ToJsonString(options), new UTF8Encoding(false));
        }
    }
}
