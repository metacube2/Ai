using System.Security.Cryptography;
using System.Text;

namespace VideoDoku.Core;

public enum StepState { Waiting, Running, Done, Failed, Skipped }

/// <summary>Fortschritt einer Szene fuer die Anzeige.</summary>
public sealed record SceneProgress(int Number, StepState Voice, StepState Render, double RenderFraction, string? Error = null);

/// <summary>Fortschritt des ganzen Laufs.</summary>
public sealed record BuildProgress(double Overall, string Message, SceneProgress? Scene = null);

/// <summary>
/// Erstellt aus einem Projekt ein MP4: je Szene Stimme holen (mit Zwischenspeicher), Szene rendern,
/// am Schluss zusammensetzen mit Untertiteln und Musik. Meldet den Fortschritt an die Oberflaeche
/// und optional an den Agenten-Monitor.
/// </summary>
public sealed class DokuBuilder
{
    private readonly Ffmpeg _ffmpeg;
    private readonly ITtsProvider _tts;

    public DokuBuilder(Ffmpeg ffmpeg, ITtsProvider tts)
    {
        _ffmpeg = ffmpeg;
        _tts = tts;
    }

    public sealed record Result(string VideoPath, string? SrtPath, TimeSpan Duration);

    /// <param name="workFolder">Zwischendateien (Stimmen, Szenen). Bleibt erhalten, damit ein zweiter Lauf schneller ist.</param>
    public async Task<Result> BuildAsync(
        DokuProject project, string voiceId, string language, string outputPath, string workFolder,
        RenderOptions options, IProgress<BuildProgress>? progress, MonitorReporter? monitor, CancellationToken ct)
    {
        if (!_ffmpeg.IsAvailable) throw new InvalidOperationException($"ffmpeg fehlt unter {Path.GetDirectoryName(_ffmpeg.FfmpegPath)}.");
        var missing = ScriptParser.CheckFiles(project);
        if (missing.Count > 0) throw new InvalidOperationException(string.Join(Environment.NewLine, missing));
        if (project.Scenes.Count == 0) throw new InvalidOperationException("Keine Szenen.");

        var voiceDir = Directory.CreateDirectory(Path.Combine(workFolder, "stimme")).FullName;
        var sceneDir = Directory.CreateDirectory(Path.Combine(workFolder, "szenen")).FullName;
        var n = project.Scenes.Count;
        // Gewichtung: Stimme 35 %, Szene 55 %, Schluss 10 %.
        double Overall(int doneVoices, int doneScenes, double sceneFraction, double final) =>
            Math.Clamp((0.35 * doneVoices + 0.55 * (doneScenes + sceneFraction)) / n + 0.10 * final, 0, 1);

        if (monitor is not null)
        {
            await monitor.StartAsync(project.Title, n);
            foreach (var s in project.Scenes) await monitor.SceneCreatedAsync(s.Number);
        }

        try
        {
            // 1. Stimmen holen (nacheinander: der Sprachdienst rechnet ohnehin nur eine Anfrage zur Zeit)
            var voicePaths = new Dictionary<int, string>();
            var voiceSeconds = new Dictionary<int, double>();
            var voicesDone = 0;
            foreach (var s in project.Scenes)
            {
                ct.ThrowIfCancellationRequested();
                if (!s.HasText)
                {
                    progress?.Report(new BuildProgress(Overall(++voicesDone, 0, 0, 0), $"Szene {s.Number}: kein Text",
                        new SceneProgress(s.Number, StepState.Skipped, StepState.Waiting, 0)));
                    continue;
                }

                var file = Path.Combine(voiceDir, CacheName(s.Text, voiceId, language, _tts.Name) + _tts.Extension);
                var stepId = $"stimme-{s.Number}";
                progress?.Report(new BuildProgress(Overall(voicesDone, 0, 0, 0), $"Szene {s.Number}/{n}: Stimme holen",
                    new SceneProgress(s.Number, StepState.Running, StepState.Waiting, 0)));
                if (monitor is not null) await monitor.StepStartAsync(stepId, "Stimme", $"Szene {s.Number}/{n}: Stimme holen");

                try
                {
                    if (!File.Exists(file) || new FileInfo(file).Length == 0)
                    {
                        var audio = await _tts.SpeakAsync(s.Text, voiceId, language, ct);
                        await File.WriteAllBytesAsync(file + ".tmp", audio, ct);
                        File.Move(file + ".tmp", file, overwrite: true);
                    }
                    var probe = await _ffmpeg.ProbeAsync(file, ct);
                    if (probe.Duration <= 0) throw new InvalidOperationException("Die gelieferte Stimme ist leer.");
                    voicePaths[s.Number] = file;
                    voiceSeconds[s.Number] = probe.Duration;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    if (monitor is not null) await monitor.StepEndAsync(stepId, "Stimme", false, ex.Message);
                    progress?.Report(new BuildProgress(Overall(voicesDone, 0, 0, 0), $"Szene {s.Number}: {ex.Message}",
                        new SceneProgress(s.Number, StepState.Failed, StepState.Waiting, 0, ex.Message)));
                    throw new InvalidOperationException($"Szene {s.Number}: Stimme konnte nicht geholt werden. {ex.Message}", ex);
                }

                if (monitor is not null) await monitor.StepEndAsync(stepId, "Stimme", true);
                progress?.Report(new BuildProgress(Overall(++voicesDone, 0, 0, 0), $"Szene {s.Number}/{n}: Stimme da",
                    new SceneProgress(s.Number, StepState.Done, StepState.Waiting, 0)));
            }

            // 2. Szenen rendern
            var segments = new List<string>();
            var cues = new List<Subtitles.Cue>();
            var t = TimeSpan.Zero;
            var scenesDone = 0;
            foreach (var s in project.Scenes)
            {
                ct.ThrowIfCancellationRequested();
                var voice = s.HasText ? voicePaths[s.Number] : null;
                double? clipSeconds = null;
                var clipAudio = false;
                if (s.Kind == VisualKind.Video)
                {
                    var p = await _ffmpeg.ProbeAsync(s.VisualPath, ct);
                    clipSeconds = p.Duration;
                    clipAudio = p.HasAudio;
                }
                var duration = FfmpegCommands.SceneDuration(s, s.HasText ? voiceSeconds[s.Number] : null, clipSeconds, options);
                var seg = Path.Combine(sceneDir, $"szene-{s.Number:000}.mp4");
                var stepId = $"szene-{s.Number}";
                var voiceState = s.HasText ? StepState.Done : StepState.Skipped;

                if (monitor is not null) await monitor.StepStartAsync(stepId, "Szene", $"Szene {s.Number}/{n}: rendern ({duration:0.0} s)");
                var sceneProgress = new Progress<double>(f => progress?.Report(new BuildProgress(
                    Overall(voicesDone, scenesDone, f, 0), $"Szene {s.Number}/{n}: rendern {f:P0}",
                    new SceneProgress(s.Number, voiceState, StepState.Running, f))));
                try
                {
                    await _ffmpeg.RunFfmpegAsync(FfmpegCommands.Scene(s, voice, clipAudio, duration, seg, options), duration, sceneProgress, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    if (monitor is not null) await monitor.StepEndAsync(stepId, "Szene", false, ex.Message);
                    progress?.Report(new BuildProgress(Overall(voicesDone, scenesDone, 0, 0), $"Szene {s.Number}: {ex.Message}",
                        new SceneProgress(s.Number, voiceState, StepState.Failed, 0, ex.Message)));
                    throw new InvalidOperationException($"Szene {s.Number} ({Path.GetFileName(s.VisualPath)}): {ex.Message}", ex);
                }

                if (s.HasText)
                    cues.AddRange(Subtitles.ForScene(s.Text, t + TimeSpan.FromSeconds(options.LeadIn), TimeSpan.FromSeconds(voiceSeconds[s.Number])));
                t += TimeSpan.FromSeconds(duration);
                segments.Add(seg);
                scenesDone++;
                if (monitor is not null)
                {
                    await monitor.StepEndAsync(stepId, "Szene", true);
                    await monitor.SceneDoneAsync(s.Number);
                }
                progress?.Report(new BuildProgress(Overall(voicesDone, scenesDone, 0, 0), $"Szene {s.Number}/{n}: fertig",
                    new SceneProgress(s.Number, voiceState, StepState.Done, 1)));
            }

            // 3. Zusammensetzen
            ct.ThrowIfCancellationRequested();
            var list = Path.Combine(workFolder, "szenen.txt");
            await File.WriteAllTextAsync(list, FfmpegCommands.ConcatList(segments), new UTF8Encoding(false), ct);
            string? srt = null;
            if (options.Subtitles && cues.Count > 0)
            {
                srt = Path.ChangeExtension(outputPath, ".srt");
                await File.WriteAllTextAsync(srt, Subtitles.ToSrt(cues), new UTF8Encoding(false), ct);
            }

            if (monitor is not null) await monitor.StepStartAsync("schluss", "Video", "Szenen zusammensetzen");
            var finalProgress = new Progress<double>(f => progress?.Report(new BuildProgress(
                Overall(voicesDone, scenesDone, 0, f), $"Video zusammensetzen {f:P0}")));
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
            await _ffmpeg.RunFfmpegAsync(
                FfmpegCommands.Final(list, project.MusicPath, srt, project.Title, outputPath, options),
                t.TotalSeconds, finalProgress, ct);
            if (monitor is not null)
            {
                await monitor.StepEndAsync("schluss", "Video", true);
                await monitor.FinishedAsync($"Video fertig: {Path.GetFileName(outputPath)} ({t:mm\\:ss})");
            }
            progress?.Report(new BuildProgress(1, $"Fertig: {Path.GetFileName(outputPath)} ({t:mm\\:ss})"));
            return new Result(outputPath, srt, t);
        }
        catch (OperationCanceledException)
        {
            if (monitor is not null) await monitor.FailedAsync("abgebrochen");
            throw;
        }
        catch (Exception ex)
        {
            if (monitor is not null) await monitor.FailedAsync(ex.Message);
            throw;
        }
        finally
        {
            if (monitor is not null) await monitor.EndAsync();
        }
    }

    /// <summary>Dateiname im Zwischenspeicher: gleicher Text mit gleicher Stimme wird nicht neu geholt.</summary>
    internal static string CacheName(string text, string voice, string language, string provider)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{provider}\n{voice}\n{language}\n{text}"));
        return Convert.ToHexString(hash, 0, 8).ToLowerInvariant();
    }
}
