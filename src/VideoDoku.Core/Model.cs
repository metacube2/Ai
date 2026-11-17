namespace VideoDoku.Core;

public enum VisualKind { Image, Video }

/// <summary>Eine Szene: ein Bild (oder eine Folie) bzw. ein Videoclip mit Sprechertext.</summary>
public sealed class Scene
{
    public required int Number { get; init; }
    public required string VisualPath { get; init; }
    public required VisualKind Kind { get; init; }
    /// <summary>Sprechertext; leer bedeutet: keine Stimme (Bild steht <see cref="SilentSeconds"/> lang, Clip laeuft ganz).</summary>
    public string Text { get; set; } = "";
    /// <summary>Pause nach dem Sprechen, in Sekunden.</summary>
    public double PauseAfter { get; set; } = 0.8;
    /// <summary>Dauer einer Bildszene ohne Text.</summary>
    public double SilentSeconds { get; set; } = 4.0;
    /// <summary>Optionale Herkunft fuer Fehlermeldungen (z. B. „Zeile 12“ oder „Folie 3“).</summary>
    public string? Origin { get; init; }

    public bool HasText => !string.IsNullOrWhiteSpace(Text);
}

public sealed class DokuProject
{
    public string Title { get; set; } = "Video-Doku";
    public List<Scene> Scenes { get; } = new();
    public string? MusicPath { get; set; }
    /// <summary>Ordner, gegen den relative Pfade im Skript aufgeloest werden.</summary>
    public string BaseFolder { get; set; } = "";
}

/// <summary>Einstellungen fuer einen Renderlauf.</summary>
public sealed class RenderOptions
{
    public int Width { get; init; } = 1920;
    public int Height { get; init; } = 1080;
    public int Fps { get; init; } = 30;
    /// <summary>Sanfter Zoom auf Standbildern (fuer freie Dokus schoen, fuer Folien eher aus).</summary>
    public bool KenBurns { get; init; }
    public bool Subtitles { get; init; } = true;
    /// <summary>Lautstaerke der Hintergrundmusik (0..1), wird beim Sprechen zusaetzlich abgesenkt.</summary>
    public double MusicVolume { get; init; } = 0.22;
    /// <summary>Lautstaerke des Originaltons eines Clips, waehrend gesprochen wird.</summary>
    public double ClipVolumeUnderVoice { get; init; } = 0.15;
    /// <summary>Stimme setzt so viele Sekunden nach Szenenbeginn ein.</summary>
    public double LeadIn { get; init; } = 0.4;
    public double Fade { get; init; } = 0.4;
    public int Crf { get; init; } = 20;
    public string Preset { get; init; } = "veryfast";
}
