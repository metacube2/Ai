using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using TrafagSalesExporter.Security;

namespace TrafagSalesExporter.Services;

public interface ILandingPageSettingsService
{
    bool ShowWalkingLabFigure { get; }
    void SetShowWalkingLabFigure(bool value);
}

public sealed class LandingPageSettingsService : ILandingPageSettingsService
{
    private static readonly object FileLock = new();
    private readonly LandingPageOptions _options;
    private readonly IHostEnvironment _environment;

    public LandingPageSettingsService(IOptions<LandingPageOptions> options, IHostEnvironment environment)
    {
        _options = options.Value;
        _environment = environment;
    }

    public bool ShowWalkingLabFigure => _options.ShowWalkingLabFigure;

    public void SetShowWalkingLabFigure(bool value)
    {
        _options.ShowWalkingLabFigure = value;
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
            var section = root[LandingPageOptions.SectionName] as JsonObject;
            if (section is null)
            {
                section = new JsonObject();
                root[LandingPageOptions.SectionName] = section;
            }

            section[nameof(LandingPageOptions.ShowWalkingLabFigure)] = value;
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(path, root.ToJsonString(options), new UTF8Encoding(false));
        }
    }
}
