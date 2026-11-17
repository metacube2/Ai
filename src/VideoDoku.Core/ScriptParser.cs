using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace VideoDoku.Core;

/// <summary>
/// Liest ein einfaches Skript fuer freie Dokus:
/// <code>
/// titel: Unser Ausflug
/// musik: musik/ruhig.mp3
///
/// [bild: bilder/01.jpg]
/// Sprechertext, gern ueber mehrere Zeilen.
///
/// [clip: clips/see.mp4]
/// Text zum Clip. Ohne Text laeuft der Clip mit Originalton ganz durch.
/// [pause: 2]
///
/// [bild: bilder/02.jpg]
/// [dauer: 6]
/// </code>
/// Zeilen mit # sind Kommentare. Relative Pfade gelten ab dem Ordner der Skriptdatei.
/// </summary>
public static partial class ScriptParser
{
    public static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".bmp", ".webp", ".gif", ".tif", ".tiff"];
    public static readonly string[] VideoExtensions = [".mp4", ".mov", ".m4v", ".mkv", ".avi", ".webm", ".wmv"];

    public sealed record Result(DokuProject Project, IReadOnlyList<string> Errors);

    [GeneratedRegex(@"^\[\s*(?<key>bild|foto|folie|clip|video|pause|dauer)\s*:\s*(?<value>.*?)\s*\]$", RegexOptions.IgnoreCase)]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"^(?<key>titel|musik)\s*:\s*(?<value>.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex HeaderRegex();

    public static Result ParseFile(string path) =>
        Parse(File.ReadAllText(path, Encoding.UTF8), Path.GetDirectoryName(Path.GetFullPath(path)) ?? "");

    public static Result Parse(string text, string baseFolder)
    {
        var project = new DokuProject { BaseFolder = baseFolder };
        var errors = new List<string>();
        Scene? current = null;
        var buffer = new StringBuilder();
        var lineNo = 0;

        void Flush()
        {
            if (current is not null) current.Text = Normalize(buffer.ToString());
            buffer.Clear();
        }

        foreach (var raw in text.ReplaceLineEndings("\n").Split('\n'))
        {
            lineNo++;
            var line = raw.Trim();
            if (line.StartsWith('#')) continue;

            var tag = TagRegex().Match(line);
            if (tag.Success)
            {
                var key = tag.Groups["key"].Value.ToLowerInvariant();
                var value = tag.Groups["value"].Value;
                switch (key)
                {
                    case "pause":
                    case "dauer":
                        if (current is null)
                        {
                            errors.Add($"Zeile {lineNo}: [{key}] steht vor der ersten Szene.");
                        }
                        else if (!TryNumber(value, out var sec) || sec < 0 || sec > 600)
                        {
                            errors.Add($"Zeile {lineNo}: „{value}“ ist keine gültige Sekundenzahl.");
                        }
                        else if (key == "pause") current.PauseAfter = sec;
                        else current.SilentSeconds = sec;
                        break;

                    default:
                        Flush();
                        var full = Resolve(baseFolder, value);
                        var kind = key is "clip" or "video" ? VisualKind.Video : VisualKind.Image;
                        // Endung schlaegt das Stichwort: [bild: x.mp4] ist ein Clip.
                        var ext = Path.GetExtension(full).ToLowerInvariant();
                        if (VideoExtensions.Contains(ext)) kind = VisualKind.Video;
                        else if (ImageExtensions.Contains(ext)) kind = VisualKind.Image;
                        else errors.Add($"Zeile {lineNo}: unbekannter Dateityp „{ext}“ bei {value}.");

                        current = new Scene
                        {
                            Number = project.Scenes.Count + 1,
                            VisualPath = full,
                            Kind = kind,
                            Origin = $"Zeile {lineNo}",
                        };
                        project.Scenes.Add(current);
                        break;
                }
                continue;
            }

            if (current is null)
            {
                var header = HeaderRegex().Match(line);
                if (header.Success)
                {
                    var key = header.Groups["key"].Value.ToLowerInvariant();
                    var value = header.Groups["value"].Value.Trim();
                    if (key == "titel") project.Title = value;
                    else project.MusicPath = Resolve(baseFolder, value);
                    continue;
                }
                if (line.Length > 0)
                    errors.Add($"Zeile {lineNo}: Text vor der ersten Szene wird ignoriert. Fehlt eine Zeile wie [bild: datei.jpg]?");
                continue;
            }

            buffer.AppendLine(line);
        }
        Flush();

        if (project.Scenes.Count == 0) errors.Add("Das Skript enthält keine Szene ([bild: …] oder [clip: …]).");
        return new Result(project, errors);
    }

    /// <summary>Prueft, ob alle Dateien vorhanden sind.</summary>
    public static IReadOnlyList<string> CheckFiles(DokuProject project)
    {
        var errors = new List<string>();
        foreach (var s in project.Scenes)
            if (!File.Exists(s.VisualPath)) errors.Add($"Szene {s.Number} ({s.Origin}): Datei fehlt: {s.VisualPath}");
        if (project.MusicPath is { } m && !File.Exists(m)) errors.Add($"Musik fehlt: {m}");
        return errors;
    }

    /// <summary>Zeilen zu einem Fliesstext; Leerzeilen trennen Absaetze (fuer die Stimme: Satzpause).</summary>
    internal static string Normalize(string text)
    {
        var paragraphs = text.ReplaceLineEndings("\n")
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(p => string.Join(' ', p.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)))
            .Where(p => p.Length > 0);
        return string.Join("\n\n", paragraphs);
    }

    private static string Resolve(string baseFolder, string path)
    {
        path = path.Trim().Trim('"');
        return Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(baseFolder, path));
    }

    private static bool TryNumber(string s, out double value) =>
        double.TryParse(s.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
}
