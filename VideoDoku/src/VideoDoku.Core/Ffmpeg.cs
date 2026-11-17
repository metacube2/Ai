using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace VideoDoku.Core;

/// <summary>Ruft ffmpeg und ffprobe auf und meldet den Fortschritt.</summary>
public sealed class Ffmpeg
{
    public string FfmpegPath { get; }
    public string FfprobePath { get; }

    public Ffmpeg(string binFolder)
    {
        FfmpegPath = Path.Combine(binFolder, "ffmpeg.exe");
        FfprobePath = Path.Combine(binFolder, "ffprobe.exe");
    }

    /// <summary>Standardort: %LOCALAPPDATA%\VideoDoku\ffmpeg\bin.</summary>
    public static string DefaultBinFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VideoDoku", "ffmpeg", "bin");

    public bool IsAvailable => File.Exists(FfmpegPath) && File.Exists(FfprobePath);

    public sealed record ProbeResult(double Duration, bool HasVideo, bool HasAudio);

    public async Task<ProbeResult> ProbeAsync(string path, CancellationToken ct)
    {
        var (code, stdout, stderr) = await RunAsync(FfprobePath, FfmpegCommands.Probe(path), null, null, ct);
        if (code != 0) throw new InvalidOperationException($"ffprobe konnte {Path.GetFileName(path)} nicht lesen: {stderr.Trim()}");
        double duration = 0;
        bool video = false, audio = false;
        foreach (var line in stdout.Split('\n', StringSplitOptions.TrimEntries))
        {
            if (line.StartsWith("duration=", StringComparison.Ordinal) &&
                double.TryParse(line[9..], NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) duration = d;
            else if (line == "codec_type=video") video = true;
            else if (line == "codec_type=audio") audio = true;
        }
        return new ProbeResult(duration, video, audio);
    }

    /// <summary>
    /// Fuehrt ffmpeg aus. <paramref name="expectedSeconds"/> dient zur Umrechnung des Fortschritts (0..1).
    /// </summary>
    public async Task RunFfmpegAsync(IReadOnlyList<string> args, double expectedSeconds, IProgress<double>? progress, CancellationToken ct)
    {
        void OnLine(string line)
        {
            // -progress pipe:1 liefert z. B. „out_time_us=1234567“.
            if (progress is null || expectedSeconds <= 0) return;
            if (line.StartsWith("out_time_us=", StringComparison.Ordinal) &&
                long.TryParse(line.AsSpan(12), NumberStyles.Integer, CultureInfo.InvariantCulture, out var us) && us >= 0)
                progress.Report(Math.Clamp(us / 1_000_000.0 / expectedSeconds, 0, 1));
        }

        var (code, _, stderr) = await RunAsync(FfmpegPath, args, OnLine, null, ct);
        if (code != 0)
            throw new InvalidOperationException("ffmpeg ist mit Fehler beendet: " + LastLines(stderr, 6));
        progress?.Report(1);
    }

    private static async Task<(int Code, string Stdout, string Stderr)> RunAsync(
        string exe, IReadOnlyList<string> args, Action<string>? onStdoutLine, string? workDir, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(exe)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        if (workDir is not null) psi.WorkingDirectory = workDir;
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var p = new Process { StartInfo = psi };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        p.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            stdout.AppendLine(e.Data);
            onStdoutLine?.Invoke(e.Data);
        };
        p.ErrorDataReceived += (_, e) => { if (e.Data is not null) stderr.AppendLine(e.Data); };

        p.Start();
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        try
        {
            await p.WaitForExitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            try { p.Kill(entireProcessTree: true); } catch (Exception) { }
            throw;
        }
        p.WaitForExit(); // Restliche Ausgaben einsammeln
        return (p.ExitCode, stdout.ToString(), stderr.ToString());
    }

    private static string LastLines(string text, int n)
    {
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join(" | ", lines.Skip(Math.Max(0, lines.Length - n)));
    }
}
