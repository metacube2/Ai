using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace WhisperStudio.Core;

/// <summary>Minimal OpenAI-compatible chat client (llama.cpp llama-server, also others).</summary>
public sealed class LlmClient
{
    private static readonly HttpClient Http = new() { Timeout = Timeout.InfiniteTimeSpan };
    private readonly string _baseUrl;
    private readonly string _model;

    public LlmClient(string baseUrl, string model)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _model = model;
    }

    public async Task<bool> IsReachableAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            using var r = await Http.GetAsync(_baseUrl + "/models", cts.Token);
            return r.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Streams the answer; onToken gets each piece as it arrives.</summary>
    public async Task<string> ChatAsync(string system, string user, Action<string>? onToken, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(new
        {
            model = _model,
            stream = true,
            temperature = 0.2,
            messages = new object[]
            {
                new { role = "system", content = system },
                new { role = "user", content = user },
            },
        });
        using var req = new HttpRequestMessage(HttpMethod.Post, _baseUrl + "/chat/completions")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        using var resp = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!resp.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"KI-Endpunkt antwortet {(int)resp.StatusCode}: {await resp.Content.ReadAsStringAsync(ct)}");
        }
        using var reader = new StreamReader(await resp.Content.ReadAsStreamAsync(ct), Encoding.UTF8);
        var sb = new StringBuilder();
        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (!line.StartsWith("data:", StringComparison.Ordinal))
            {
                continue;
            }
            var data = line[5..].Trim();
            if (data == "[DONE]")
            {
                break;
            }
            using var doc = JsonDocument.Parse(data);
            var choices = doc.RootElement.GetProperty("choices");
            if (choices.GetArrayLength() == 0)
            {
                continue;
            }
            if (choices[0].TryGetProperty("delta", out var delta)
                && delta.TryGetProperty("content", out var content)
                && content.GetString() is { Length: > 0 } piece)
            {
                sb.Append(piece);
                onToken?.Invoke(piece);
            }
        }
        return sb.ToString();
    }
}

/// <summary>Prompts and chunking for protocol, translation and clean-up.</summary>
public static class AiTasks
{
    // Small local models on CPU: keep each request well inside the context window.
    public const int ChunkChars = 5000;

    private const string Rules =
        "Erfinde nichts. Was nicht im Text steht, lässt du weg. Namen, Fachbegriffe, Zahlen und Produktbezeichnungen " +
        "übernimmst du unverändert. Das Transkript stammt aus automatischer Spracherkennung (Schweizerdeutsch, " +
        "Hochdeutsch, Englisch) und enthält Erkennungsfehler.";

    public static readonly string[] Languages = { "Deutsch", "Englisch", "Französisch", "Italienisch", "Spanisch", "Portugiesisch", "Tschechisch", "Chinesisch" };

    /// <summary>Small models like to wrap the whole answer in a ```markdown fence.</summary>
    public static string StripFence(string answer)
    {
        var t = answer.Trim();
        if (!t.StartsWith("```", StringComparison.Ordinal) || !t.EndsWith("```", StringComparison.Ordinal) || t.Length < 6)
        {
            return t;
        }
        var firstNewline = t.IndexOf('\n');
        return firstNewline < 0 ? t : t[(firstNewline + 1)..^3].Trim();
    }

    public static List<string> Chunk(string text, int maxChars = ChunkChars)
    {
        var chunks = new List<string>();
        var sb = new StringBuilder();
        foreach (var line in text.Replace("\r\n", "\n").Split('\n'))
        {
            if (sb.Length > 0 && sb.Length + line.Length + 1 > maxChars)
            {
                chunks.Add(sb.ToString().Trim());
                sb.Clear();
            }
            // A single overlong line (Whisper without timestamps can produce one) is cut hard.
            var rest = line;
            while (rest.Length > maxChars)
            {
                chunks.Add(rest[..maxChars]);
                rest = rest[maxChars..];
            }
            sb.Append(rest).Append('\n');
        }
        if (sb.ToString().Trim().Length > 0)
        {
            chunks.Add(sb.ToString().Trim());
        }
        return chunks;
    }

    public static async Task<string> ProtocolAsync(LlmClient llm, string transcript, Action<string> onToken, Action<string> status, CancellationToken ct)
    {
        var chunks = Chunk(transcript);
        string material;
        if (chunks.Count == 1)
        {
            material = chunks[0];
        }
        else
        {
            // Map: notes per section, then reduce into one protocol.
            var notes = new StringBuilder();
            for (var i = 0; i < chunks.Count; i++)
            {
                status($"Abschnitt {i + 1}/{chunks.Count} wird zusammengefasst...");
                var n = await llm.ChatAsync(
                    "Du fasst Abschnitte eines Meeting-Transkripts auf Deutsch stichwortartig zusammen: Themen, Aussagen, Beschlüsse, Pendenzen mit Namen. " + Rules,
                    chunks[i], null, ct);
                notes.Append($"### Abschnitt {i + 1}\n{n.Trim()}\n\n");
            }
            material = notes.ToString();
            status("Protokoll wird aus den Abschnitten erstellt...");
        }
        return await llm.ChatAsync(
            "Du erstellst aus einem Meeting-Transkript ein Protokoll auf Deutsch in Markdown mit den Abschnitten " +
            "## Zusammenfassung (3 bis 6 Sätze), ## Besprochene Themen, ## Beschlüsse, ## Pendenzen (Tabelle: Was | Wer | Bis wann, " +
            "fehlende Angaben als 'nicht genannt'), ## Offene Fragen. " + Rules,
            material, onToken, ct);
    }

    public static async Task<string> TranslateAsync(LlmClient llm, string text, string targetLanguage, Action<string> onToken, Action<string> status, CancellationToken ct)
    {
        var chunks = Chunk(text);
        var sb = new StringBuilder();
        for (var i = 0; i < chunks.Count; i++)
        {
            status($"Übersetze Abschnitt {i + 1}/{chunks.Count}...");
            if (i > 0)
            {
                sb.Append("\n\n");
                onToken("\n\n");
            }
            sb.Append(await llm.ChatAsync(
                $"Du übersetzt den Text vollständig nach {targetLanguage}. Gib nur die Übersetzung aus, ohne Kommentar und ohne Einleitung. " + Rules,
                chunks[i], onToken, ct));
        }
        return sb.ToString();
    }

    public static async Task<string> CleanUpAsync(LlmClient llm, string text, Action<string> onToken, Action<string> status, CancellationToken ct)
    {
        var chunks = Chunk(text);
        var sb = new StringBuilder();
        for (var i = 0; i < chunks.Count; i++)
        {
            status($"Überarbeite Abschnitt {i + 1}/{chunks.Count}...");
            if (i > 0)
            {
                sb.Append("\n\n");
                onToken("\n\n");
            }
            sb.Append(await llm.ChatAsync(
                "Du überarbeitest ein Rohtranskript zu gut lesbarem Hochdeutsch mit normaler Zeichensetzung und Absätzen. " +
                "Inhalt nicht verändern, nichts weglassen, nichts zusammenfassen. Gib nur den überarbeiteten Text aus. " + Rules,
                chunks[i], onToken, ct));
        }
        return sb.ToString();
    }
}

/// <summary>Starts the portable llama-server from whisper\llm\ when it is installed there.</summary>
public sealed class LlamaHost : IDisposable
{
    private readonly StudioPaths _paths;
    private Process? _process;

    public LlamaHost(StudioPaths paths) => _paths = paths;

    public string ServerExe => Path.Combine(_paths.LlmDir, "bin", "llama-server.exe");
    public string? ModelFile => Directory.Exists(Path.Combine(_paths.LlmDir, "models"))
        ? Directory.EnumerateFiles(Path.Combine(_paths.LlmDir, "models"), "*.gguf").OrderBy(f => f).FirstOrDefault()
        : null;
    public bool IsInstalled => File.Exists(ServerExe) && ModelFile != null;
    public bool IsRunning => _process is { HasExited: false };

    public bool TryStart(int port, out string message)
    {
        if (IsRunning)
        {
            message = "läuft bereits";
            return true;
        }
        if (!IsInstalled)
        {
            message = $"nicht installiert (erwartet: {ServerExe} und ein .gguf-Modell in llm\\models)";
            return false;
        }
        var threads = Math.Max(2, Environment.ProcessorCount / 2);
        var psi = new ProcessStartInfo
        {
            FileName = ServerExe,
            // 8k context is enough for one chunk plus answer; threads = physical cores.
            Arguments = $"-m \"{ModelFile}\" -c 8192 -t {threads} --host 127.0.0.1 --port {port}",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(ServerExe)!,
        };
        _process = Process.Start(psi);
        // Drain the pipes, llama-server logs a lot and would block on a full buffer.
        _process!.OutputDataReceived += (_, _) => { };
        _process.ErrorDataReceived += (_, _) => { };
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
        message = $"gestartet mit {Path.GetFileName(ModelFile)}";
        return true;
    }

    public void Dispose()
    {
        try { if (IsRunning) _process!.Kill(entireProcessTree: true); } catch { /* best effort */ }
        _process?.Dispose();
    }
}
