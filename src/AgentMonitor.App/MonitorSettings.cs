using System;
using System.IO;
using System.Text.Json;

namespace AgentMonitor.App;

public enum DisplayMode
{
    /// <summary>Fenster springt auf, sobald ein Agent denkt oder arbeitet, und blendet sich danach aus.</summary>
    Popup,
    /// <summary>Fenster bleibt dauerhaft offen.</summary>
    Pinned,
}

/// <summary>Einstellungen unter %APPDATA%\AgentMonitor\settings.json.</summary>
public sealed class MonitorSettings
{
    public const int DefaultPort = 47811;

    public DisplayMode Mode { get; set; } = DisplayMode.Popup;
    public int AutoHideSeconds { get; set; } = 8;
    public bool Topmost { get; set; } = true;
    public int Port { get; set; } = DefaultPort;
    public double? Left { get; set; }
    public double? Top { get; set; }
    public double Width { get; set; } = 620;
    public double Height { get; set; } = 820;

    /// <summary>Vergroesserung der ganzen Anzeige (1.0 = 100 %). Standard gross, gut lesbar mit Brille.</summary>
    public double Scale { get; set; } = 1.4;

    public const double MinScale = 0.8, MaxScale = 2.5;

    /// <summary>Beim ersten Start einmal den Autostart einrichten; danach gilt, was im Tray-Menue gewaehlt ist.</summary>
    public bool AutostartInitialized { get; set; }

    /// <summary>
    /// Nur zur Fehlersuche: rohe Hook-Ereignisse in eine Datei schreiben. Standard aus, weil die
    /// Rohdaten Befehle und Pfade enthalten.
    /// </summary>
    public bool DebugLog { get; set; }

    public static string Folder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AgentMonitor");

    private static string FilePath => Path.Combine(Folder, "settings.json");

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static MonitorSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<MonitorSettings>(File.ReadAllText(FilePath)) ?? new();
        }
        catch (Exception)
        {
            // Kaputte Datei: mit Standardwerten weiter, beim naechsten Speichern wird sie ersetzt.
        }
        return new();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Json));
        }
        catch (Exception)
        {
            // Einstellungen sind Komfort; ein Schreibfehler darf den Monitor nicht stoppen.
        }
    }
}
