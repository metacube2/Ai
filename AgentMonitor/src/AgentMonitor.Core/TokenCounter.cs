using System.Text;
using System.Text.Json;

namespace AgentMonitor.Core;

/// <summary>Token-Summen eines Tages.</summary>
public sealed record TokenTotals(long Input, long Output, long CacheWrite, long CacheRead)
{
    public long Total => Input + Output + CacheWrite + CacheRead;
    public static readonly TokenTotals Zero = new(0, 0, 0, 0);
}

/// <summary>
/// Zaehlt die Tokens des heutigen Tages aus den Gespraechsprotokollen (~/.claude/projects/**/*.jsonl).
/// Wichtig: Das Zeilenformat ist von Anthropic nicht dokumentiert, nur beobachtet (assistant-Eintraege mit
/// message.usage). Die Zahl ist deshalb eine Auskunft, kein Abrechnungswert; das Planlimit kommt aus der
/// Statuszeile. Eine Antwort kann mehrfach protokolliert sein (Streaming) – gezaehlt wird je message.id einmal,
/// mit dem zuletzt gesehenen (vollstaendigsten) Stand.
/// Liest Dateien inkrementell ab der zuletzt gelesenen Stelle.
/// </summary>
public sealed class TokenCounter
{
    private readonly string _root;
    private readonly Dictionary<string, long> _offsets = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, TokenTotals> _byMessage = new();
    private DateOnly _day;

    public TokenCounter(string projectsRoot) { _root = projectsRoot; }

    public static string DefaultRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude", "projects");

    public TokenTotals Today { get; private set; } = TokenTotals.Zero;

    /// <summary>Neue Zeilen einlesen. <paramref name="now"/> bestimmt den Tag (lokale Zeit).</summary>
    public TokenTotals Refresh(DateTimeOffset now)
    {
        var today = DateOnly.FromDateTime(now.LocalDateTime);
        if (today != _day)
        {
            _day = today;
            _offsets.Clear();
            _byMessage.Clear();
        }
        if (!Directory.Exists(_root)) return Today = TokenTotals.Zero;

        var since = today.ToDateTime(TimeOnly.MinValue);
        foreach (var file in Directory.EnumerateFiles(_root, "*.jsonl", SearchOption.AllDirectories))
        {
            try
            {
                var info = new FileInfo(file);
                if (info.LastWriteTime < since) continue;
                ReadNew(file, info.Length, today);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        long i = 0, o = 0, cw = 0, cr = 0;
        foreach (var t in _byMessage.Values) { i += t.Input; o += t.Output; cw += t.CacheWrite; cr += t.CacheRead; }
        return Today = new TokenTotals(i, o, cw, cr);
    }

    private void ReadNew(string file, long length, DateOnly today)
    {
        _offsets.TryGetValue(file, out var offset);
        if (length < offset) offset = 0; // Datei neu geschrieben
        if (length == offset) return;

        using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        fs.Seek(offset, SeekOrigin.Begin);
        using var reader = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1 << 16, leaveOpen: true);
        var consumed = offset;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            // Unvollstaendige letzte Zeile (wird gerade geschrieben): beim naechsten Mal erneut lesen.
            if (reader.EndOfStream && !EndsWithNewline(fs)) break;
            consumed += Encoding.UTF8.GetByteCount(line) + 1;
            if (!line.Contains("\"usage\"", StringComparison.Ordinal)) continue;
            Consume(line, today);
        }
        _offsets[file] = Math.Min(consumed, length);
    }

    private static bool EndsWithNewline(FileStream fs)
    {
        if (fs.Length == 0) return true;
        var pos = fs.Position;
        fs.Seek(-1, SeekOrigin.End);
        var b = fs.ReadByte();
        fs.Seek(pos, SeekOrigin.Begin);
        return b == '\n';
    }

    /// <summary>Eine Protokollzeile auswerten (fuer Tests oeffentlich).</summary>
    public void Consume(string line, DateOnly today)
    {
        try
        {
            using var doc = JsonDocument.Parse(line);
            var r = doc.RootElement;
            if (r.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String && t.GetString() != "assistant") return;
            if (r.TryGetProperty("timestamp", out var ts) && ts.ValueKind == JsonValueKind.String &&
                DateTimeOffset.TryParse(ts.GetString(), out var when) &&
                DateOnly.FromDateTime(when.LocalDateTime) != today) return;
            if (!r.TryGetProperty("message", out var msg) || msg.ValueKind != JsonValueKind.Object) return;
            if (!msg.TryGetProperty("usage", out var u) || u.ValueKind != JsonValueKind.Object) return;

            var id = msg.TryGetProperty("id", out var mid) && mid.ValueKind == JsonValueKind.String
                ? mid.GetString()!
                : (r.TryGetProperty("uuid", out var uu) ? uu.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString());

            _byMessage[id] = new TokenTotals(N(u, "input_tokens"), N(u, "output_tokens"),
                N(u, "cache_creation_input_tokens"), N(u, "cache_read_input_tokens"));
        }
        catch (JsonException) { }
    }

    /// <summary>Summe der gezaehlten Antworten (fuer Tests ohne Dateien).</summary>
    public TokenTotals Sum()
    {
        long i = 0, o = 0, cw = 0, cr = 0;
        foreach (var x in _byMessage.Values) { i += x.Input; o += x.Output; cw += x.CacheWrite; cr += x.CacheRead; }
        return new TokenTotals(i, o, cw, cr);
    }

    private static long N(JsonElement e, string n) =>
        e.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var x) ? x : 0;
}
