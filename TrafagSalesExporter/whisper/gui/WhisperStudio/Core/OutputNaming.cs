using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace WhisperStudio.Core;

/// <summary>
/// Builds transcript names like "2026-10-22_1430_Einkauf.txt" instead of "audio.txt".
/// The name must be deterministic for a given file (recording time + title), otherwise
/// "Vorhandene ueberspringen" could never find the transcript of an earlier run.
/// </summary>
public static partial class OutputNaming
{
    public const string StampFormat = "yyyy-MM-dd_HHmm";

    // Words that say nothing about the meeting. Recorders and users name files
    // "audio", "aufnahme", "au3333dio" (renamed only to dodge collisions) or park them in "Data".
    private static readonly HashSet<string> GenericWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "audio", "recording", "record", "aufnahme", "aufzeichnung", "data", "daten", "input", "output",
        "desktop", "downloads", "download", "temp", "tmp", "neuer", "ordner", "new", "folder", "onedrive",
        "dokumente", "documents", "video", "videos", "musik", "music", "voice", "memo", "sprachmemo",
        "mic", "microphone", "mikrofon", "sound", "file", "datei", "test", "neu", "copy", "kopie",
        "users", "user", "source", "repos", "trafag", "ag", "whisper",
    };

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}_\d{4}(_|$)")]
    private static partial Regex StampedPrefix();

    [GeneratedRegex(@"[^\p{L}]+")]
    private static partial Regex NonLetters();

    public static bool IsGeneric(string name)
    {
        // Digits and separators are noise: "au3333dio" and "audio (2)" are both just "audio".
        var letters = NonLetters().Replace(name, " ").Trim();
        if (letters.Length == 0)
        {
            return true;
        }
        var joined = letters.Replace(" ", "");
        if (GenericWords.Contains(joined))
        {
            return true;
        }
        return letters.Split(' ', StringSplitOptions.RemoveEmptyEntries).All(GenericWords.Contains);
    }

    /// <summary>First meaningful name: the file name itself, else the nearest folder.</summary>
    public static string SuggestTitle(string mediaPath, int maxFolderLevels = 4)
    {
        var stem = Path.GetFileNameWithoutExtension(mediaPath);
        if (!IsGeneric(stem))
        {
            return Sanitize(stem);
        }
        var dir = new DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(mediaPath))!);
        for (var i = 0; i < maxFolderLevels && dir?.Parent != null; i++, dir = dir.Parent)
        {
            if (!IsGeneric(dir.Name))
            {
                return Sanitize(dir.Name);
            }
        }
        return "Transkript";
    }

    /// <summary>Recording time as the file system knows it. Editable in the studio.</summary>
    public static DateTime SuggestRecordedAt(string mediaPath) => File.GetLastWriteTime(mediaPath);

    public static string BuildStem(DateTime recordedAt, string title)
    {
        var clean = Sanitize(title);
        var stamp = recordedAt.ToString(StampFormat, CultureInfo.InvariantCulture);
        return clean.Length == 0 ? stamp : $"{stamp}_{clean}";
    }

    /// <summary>Stem for a media file; files already named by the studio keep their name.</summary>
    public static string StemFor(string mediaPath, DateTime recordedAt, string title)
    {
        var stem = Path.GetFileNameWithoutExtension(mediaPath);
        return StampedPrefix().IsMatch(stem) ? stem : BuildStem(recordedAt, title);
    }

    public static string Sanitize(string title)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(title.Length);
        foreach (var ch in title.Trim())
        {
            sb.Append(invalid.Contains(ch) ? '-' : ch);
        }
        var s = Regex.Replace(sb.ToString(), @"\s+", " ").Trim(' ', '.', '-', '_');
        return s.Length > 60 ? s[..60].TrimEnd(' ', '.', '-', '_') : s;
    }
}
