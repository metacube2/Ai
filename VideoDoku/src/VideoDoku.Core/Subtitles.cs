using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace VideoDoku.Core;

/// <summary>Erzeugt SRT-Untertitel aus dem Sprechertext. Zeiten werden nach Zeichenanzahl verteilt.</summary>
public static partial class Subtitles
{
    public const int MaxCueChars = 84;

    public sealed record Cue(TimeSpan Start, TimeSpan End, string Text);

    [GeneratedRegex(@"(?<=[\.\!\?…:;])\s+")]
    private static partial Regex SentenceSplit();

    /// <summary>Teilt einen Text in Stuecke von hoechstens <see cref="MaxCueChars"/> Zeichen, bevorzugt an Satzenden.</summary>
    public static IReadOnlyList<string> Chunk(string text)
    {
        var result = new List<string>();
        foreach (var sentence in SentenceSplit().Split(text.ReplaceLineEndings(" ").Trim()))
        {
            var s = sentence.Trim();
            if (s.Length == 0) continue;
            if (s.Length <= MaxCueChars) { result.Add(s); continue; }

            // Lange Saetze an Wortgrenzen teilen, moeglichst an Kommas.
            var words = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();
            foreach (var w in words)
            {
                if (sb.Length > 0 && sb.Length + 1 + w.Length > MaxCueChars)
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(w);
                if (w.EndsWith(',') && sb.Length > MaxCueChars * 0.6)
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }
            }
            if (sb.Length > 0) result.Add(sb.ToString());
        }
        return result;
    }

    /// <summary>Verteilt die Stuecke eines Textes auf den Zeitraum, in dem gesprochen wird.</summary>
    public static IReadOnlyList<Cue> ForScene(string text, TimeSpan speechStart, TimeSpan speechDuration)
    {
        var chunks = Chunk(text);
        if (chunks.Count == 0 || speechDuration <= TimeSpan.Zero) return [];
        var total = chunks.Sum(c => c.Length);
        var cues = new List<Cue>(chunks.Count);
        var t = speechStart;
        foreach (var c in chunks)
        {
            var d = TimeSpan.FromTicks((long)(speechDuration.Ticks * (double)c.Length / total));
            cues.Add(new Cue(t, t + d, c));
            t += d;
        }
        return cues;
    }

    public static string ToSrt(IEnumerable<Cue> cues)
    {
        var sb = new StringBuilder();
        var n = 1;
        foreach (var c in cues)
        {
            sb.Append(n++).Append('\n');
            sb.Append(Stamp(c.Start)).Append(" --> ").Append(Stamp(c.End)).Append('\n');
            sb.Append(Wrap(c.Text)).Append("\n\n");
        }
        return sb.ToString();
    }

    /// <summary>Lange Untertitel auf zwei Zeilen umbrechen, moeglichst in der Mitte.</summary>
    internal static string Wrap(string text)
    {
        if (text.Length <= 42) return text;
        var mid = text.Length / 2;
        var left = text.LastIndexOf(' ', mid);
        var right = text.IndexOf(' ', mid);
        var cut = left < 0 ? right : right < 0 ? left : (mid - left <= right - mid ? left : right);
        return cut <= 0 ? text : text[..cut] + "\n" + text[(cut + 1)..];
    }

    internal static string Stamp(TimeSpan t) =>
        string.Create(CultureInfo.InvariantCulture, $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00},{t.Milliseconds:000}");
}
