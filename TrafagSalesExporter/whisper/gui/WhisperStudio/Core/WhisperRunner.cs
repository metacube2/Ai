using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace WhisperStudio.Core;

public sealed record RunOptions(string Language, string Model, string Task, bool Timestamps);

public sealed class RunResult
{
    public int ExitCode { get; set; } = -1;
    public string? WrittenPath { get; set; }
    public bool Cancelled { get; set; }
}

/// <summary>
/// Runs transcribe.ps1 for one file and turns its stdout protocol into events:
/// "PROGRESS pct done total", "Written: path", everything else is log.
/// </summary>
public sealed class WhisperRunner
{
    public const int ExitSkipped = 3;

    private readonly StudioPaths _paths;
    private Process? _process;

    public WhisperRunner(StudioPaths paths) => _paths = paths;

    public event Action<double, double>? Progress; // percent, audio seconds total
    public event Action<string>? Log;

    public static bool TryParseProgress(string line, out double percent, out double totalSeconds)
    {
        percent = totalSeconds = 0;
        const string marker = "PROGRESS ";
        if (!line.StartsWith(marker, StringComparison.Ordinal))
        {
            return false;
        }
        var parts = line[marker.Length..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 3
            && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out percent)
            && double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out totalSeconds);
    }

    public async Task<RunResult> RunAsync(string mediaPath, string outputTxt, RunOptions o, CancellationToken ct)
    {
        var result = new RunResult();
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = _paths.Root,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var a in new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", _paths.TranscribeScript, mediaPath,
                                   "-OutputFile", outputTxt, "-Language", o.Language, "-Model", o.Model, "-Task", o.Task })
        {
            psi.ArgumentList.Add(a);
        }
        if (o.Timestamps)
        {
            psi.ArgumentList.Add("-Timestamps");
        }

        using var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
        _process = p;
        p.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not { } line)
            {
                return;
            }
            if (TryParseProgress(line, out var pct, out var total))
            {
                Progress?.Invoke(pct, total);
                return;
            }
            const string written = "Written: ";
            if (line.StartsWith(written, StringComparison.Ordinal))
            {
                result.WrittenPath = line[written.Length..].Trim();
            }
            Log?.Invoke(line);
        };
        p.ErrorDataReceived += (_, e) => { if (e.Data != null) Log?.Invoke(e.Data); };

        p.Start();
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        using (ct.Register(() => { try { p.Kill(entireProcessTree: true); } catch { /* already gone */ } }))
        {
            await p.WaitForExitAsync(CancellationToken.None);
        }
        // WaitForExitAsync returns once the streams are drained, so "Written:" has arrived.
        result.ExitCode = p.ExitCode;
        result.Cancelled = ct.IsCancellationRequested;
        _process = null;
        return result;
    }
}
