using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Security;

namespace TrafagSalesExporter.Services;

public interface ILandingPageSettingsService
{
    bool ShowWalkingLabFigure { get; }
    void SetShowWalkingLabFigure(bool value);
    bool PlayWelcomeSound { get; }

    /// <summary>Manometer oben auf der Startseite (2026-10-14, Standard aus, gespeichert in ExportSettings).</summary>
    bool ShowManometer { get; }
    void SetShowManometer(bool value);
}

public sealed class LandingPageSettingsService : ILandingPageSettingsService
{
    private static readonly object FileLock = new();
    private readonly LandingPageOptions _options;
    private readonly IHostEnvironment _environment;

    private readonly IDbContextFactory<AppDbContext>? _dbFactory;
    private bool? _showManometer;

    public LandingPageSettingsService(IOptions<LandingPageOptions> options, IHostEnvironment environment,
        IDbContextFactory<AppDbContext>? dbFactory = null)
    {
        _options = options.Value;
        _environment = environment;
        _dbFactory = dbFactory;
    }

    // Einmal gelesen und gemerkt; der Schalter aendert sich nur ueber SetShowManometer.
    public bool ShowManometer
    {
        get
        {
            if (_showManometer is { } cached) return cached;
            try
            {
                using var db = _dbFactory?.CreateDbContext();
                _showManometer = db?.ExportSettings.AsNoTracking().Select(s => s.ShowLandingManometer).FirstOrDefault() ?? false;
            }
            catch (Exception)
            {
                return false;   // Startseite darf an der DB nicht scheitern; nicht merken, beim naechsten Aufruf neu lesen
            }
            return _showManometer.Value;
        }
    }

    public void SetShowManometer(bool value)
    {
        if (_dbFactory is null) throw new InvalidOperationException("Cockpit-Datenbank nicht verfuegbar.");
        using var db = _dbFactory.CreateDbContext();
        var settings = db.ExportSettings.FirstOrDefault() ?? db.ExportSettings.Add(new TrafagSalesExporter.Models.ExportSettings()).Entity;
        settings.ShowLandingManometer = value;
        db.SaveChanges();
        _showManometer = value;
    }

    public bool ShowWalkingLabFigure => _options.ShowWalkingLabFigure;
    public bool PlayWelcomeSound => _options.PlayWelcomeSound;

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
