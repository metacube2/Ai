using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace WhisperStudio.Core;

public sealed record Segment(
    [property: JsonPropertyName("start")] double Start,
    [property: JsonPropertyName("end")] double End,
    [property: JsonPropertyName("text")] string Text);

/// <summary>The .json sidecar written by scripts/transcribe.py.</summary>
public sealed class TranscriptMeta
{
    [JsonPropertyName("source")] public string Source { get; set; } = "";
    [JsonPropertyName("model")] public string Model { get; set; } = "";
    [JsonPropertyName("task")] public string Task { get; set; } = "transcribe";
    [JsonPropertyName("language")] public string Language { get; set; } = "";
    [JsonPropertyName("duration")] public double Duration { get; set; }
    [JsonPropertyName("created")] public string Created { get; set; } = "";
    [JsonPropertyName("segments")] public List<Segment> Segments { get; set; } = new();

    /// <summary>
    /// Finds an earlier transcript of <paramref name="mediaPath"/> in <paramref name="folder"/> by the
    /// source recorded in its .json. Needed for resume: the file name depends on the title, and a title
    /// typed by hand is not remembered, so the name alone would miss it and transcribe again.
    /// </summary>
    public static string? FindExistingTranscript(string mediaPath, string folder)
    {
        if (!Directory.Exists(folder))
        {
            return null;
        }
        var full = Path.GetFullPath(mediaPath);
        foreach (var json in Directory.EnumerateFiles(folder, "*.json"))
        {
            var meta = TryLoad(json);
            var txt = Path.ChangeExtension(json, ".txt");
            if (meta != null && string.Equals(meta.Source, full, StringComparison.OrdinalIgnoreCase)
                && File.Exists(txt) && new FileInfo(txt).Length > 0)
            {
                return txt;
            }
        }
        return null;
    }

    public static TranscriptMeta? TryLoad(string jsonPath)
    {
        try
        {
            return File.Exists(jsonPath) ? JsonSerializer.Deserialize<TranscriptMeta>(File.ReadAllText(jsonPath, Encoding.UTF8)) : null;
        }
        catch
        {
            return null;
        }
    }
}

public static class TranscriptExport
{
    public static string FormatSrtTime(double seconds) => FormatTime(seconds, ',');
    public static string FormatVttTime(double seconds) => FormatTime(seconds, '.');

    private static string FormatTime(double seconds, char msSeparator)
    {
        var ts = TimeSpan.FromMilliseconds(Math.Round(Math.Max(0, seconds) * 1000));
        return string.Create(CultureInfo.InvariantCulture,
            $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}{msSeparator}{ts.Milliseconds:000}");
    }

    public static string ToSrt(IReadOnlyList<Segment> segments)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < segments.Count; i++)
        {
            var s = segments[i];
            sb.Append(i + 1).Append('\n')
              .Append(FormatSrtTime(s.Start)).Append(" --> ").Append(FormatSrtTime(s.End)).Append('\n')
              .Append(s.Text).Append("\n\n");
        }
        return sb.ToString();
    }

    public static string ToVtt(IReadOnlyList<Segment> segments)
    {
        var sb = new StringBuilder("WEBVTT\n\n");
        foreach (var s in segments)
        {
            sb.Append(FormatVttTime(s.Start)).Append(" --> ").Append(FormatVttTime(s.End)).Append('\n')
              .Append(s.Text).Append("\n\n");
        }
        return sb.ToString();
    }

    /// <summary>Plain text of a transcript file without the "Source:/Model:" header block.</summary>
    public static string BodyOf(string txt)
    {
        var lines = txt.Replace("\r\n", "\n").Split('\n');
        var start = 0;
        if (lines.Length > 0 && lines[0].StartsWith("Source:", StringComparison.Ordinal))
        {
            while (start < lines.Length && lines[start].Length > 0)
            {
                start++;
            }
        }
        return string.Join("\n", lines.Skip(start)).Trim();
    }

    /// <summary>Word document: title, one meta line, then the text (with times if segments are given).</summary>
    public static void WriteDocx(string path, string title, string metaLine, string body, IReadOnlyList<Segment>? segments)
    {
        using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
        var main = doc.AddMainDocumentPart();
        var bodyEl = new Body();
        bodyEl.Append(Para(title, bold: true, sizeHalfPoints: 32));
        bodyEl.Append(Para(metaLine, color: "666666", sizeHalfPoints: 18));
        if (segments is { Count: > 0 })
        {
            foreach (var s in segments)
            {
                var p = new Paragraph();
                p.Append(Run($"[{FormatSrtTime(s.Start)[..8]}]  ", color: "888888", sizeHalfPoints: 18));
                p.Append(Run(s.Text));
                bodyEl.Append(p);
            }
        }
        else
        {
            foreach (var line in body.Replace("\r\n", "\n").Split('\n'))
            {
                bodyEl.Append(Para(line));
            }
        }
        main.Document = new Document(bodyEl);
        main.Document.Save();
    }

    private static Paragraph Para(string text, bool bold = false, string? color = null, int? sizeHalfPoints = null)
        => new(Run(text, bold, color, sizeHalfPoints));

    private static Run Run(string text, bool bold = false, string? color = null, int? sizeHalfPoints = null)
    {
        var props = new RunProperties(new RunFonts { Ascii = "Segoe UI", HighAnsi = "Segoe UI" });
        if (bold) props.Append(new Bold());
        if (color != null) props.Append(new Color { Val = color });
        if (sizeHalfPoints != null) props.Append(new FontSize { Val = sizeHalfPoints.Value.ToString(CultureInfo.InvariantCulture) });
        return new Run(props, new Text(text) { Space = SpaceProcessingModeValues.Preserve });
    }

    /// <summary>Writes the chosen formats next to the .txt; returns the files written.</summary>
    public static List<string> ExportAll(string txtPath, bool srt, bool vtt, bool docx)
    {
        var written = new List<string>();
        var jsonPath = Path.ChangeExtension(txtPath, ".json");
        var meta = TranscriptMeta.TryLoad(jsonPath);
        var segments = meta?.Segments ?? new List<Segment>();
        // transcribe.py writes the .txt before the .json. A .txt that is newer was edited
        // in the studio; DOCX then has to carry the edited text. SRT/VTT need the segment
        // times and stay on the original recognition.
        var edited = meta != null && File.GetLastWriteTimeUtc(txtPath) > File.GetLastWriteTimeUtc(jsonPath).AddSeconds(5);
        if (srt && segments.Count > 0)
        {
            var p = Path.ChangeExtension(txtPath, ".srt");
            File.WriteAllText(p, ToSrt(segments), new UTF8Encoding(false));
            written.Add(p);
        }
        if (vtt && segments.Count > 0)
        {
            var p = Path.ChangeExtension(txtPath, ".vtt");
            File.WriteAllText(p, ToVtt(segments), new UTF8Encoding(false));
            written.Add(p);
        }
        if (docx)
        {
            var p = Path.ChangeExtension(txtPath, ".docx");
            var title = Path.GetFileNameWithoutExtension(txtPath).Replace('_', ' ');
            var metaLine = meta is null
                ? $"Transkript, exportiert {DateTime.Now:dd.MM.yyyy HH:mm}"
                : $"Modell {meta.Model}, Sprache {meta.Language}, Dauer {TimeSpan.FromSeconds(meta.Duration):hh\\:mm\\:ss}";
            WriteDocx(p, title, metaLine, BodyOf(File.ReadAllText(txtPath, Encoding.UTF8)), segments.Count > 0 && !edited ? segments : null);
            written.Add(p);
        }
        return written;
    }
}
