using System.Diagnostics;
using System.Globalization;
using VideoDoku.Core;

namespace VideoDoku.Tests;

/// <summary>
/// Erstellt ein echtes Video mit dem installierten ffmpeg. Die Stimme kommt von einer Ersatzquelle,
/// die per ffmpeg einen Ton erzeugt (Laenge abhaengig von der Textlaenge). Wird uebersprungen,
/// wenn ffmpeg nicht installiert ist.
/// </summary>
public class EndToEndTests
{
    private sealed class ToneTts(Ffmpeg ff) : ITtsProvider
    {
        public int Calls;
        public string Name => "Testton";
        public string Extension => ".wav";
        public Task<IReadOnlyList<VoiceOption>> VoicesAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<VoiceOption>>([new VoiceOption(Name, "ton", "Ton")]);

        public async Task<byte[]> SpeakAsync(string text, string voiceId, string language, CancellationToken ct)
        {
            Interlocked.Increment(ref Calls);
            var seconds = Math.Max(1, text.Length / 15.0).ToString("0.##", CultureInfo.InvariantCulture);
            var tmp = Path.Combine(Path.GetTempPath(), $"ton-{Guid.NewGuid():N}.wav");
            await Run(ff.FfmpegPath, "-hide_banner", "-y", "-loglevel", "error", "-f", "lavfi", "-i", $"sine=frequency=440:duration={seconds}", tmp);
            var bytes = await File.ReadAllBytesAsync(tmp, ct);
            File.Delete(tmp);
            return bytes;
        }
    }

    private static async Task Run(string exe, params string[] args)
    {
        var psi = new ProcessStartInfo(exe) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi)!;
        var err = await p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync();
        if (p.ExitCode != 0) throw new InvalidOperationException(err);
    }

    [Fact]
    public async Task Ganzes_Video_mit_Bildern_Clip_Untertiteln_und_Musik()
    {
        var ff = new Ffmpeg(Ffmpeg.DefaultBinFolder);
        if (!ff.IsAvailable) return; // ffmpeg nicht installiert: nichts zu pruefen

        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "vd-e2e-" + Guid.NewGuid().ToString("N")[..8])).FullName;
        try
        {
            // Testmaterial: zwei Bilder (eins hochkant), ein Clip mit Ton, Musik
            await Run(ff.FfmpegPath, "-y", "-loglevel", "error", "-f", "lavfi", "-i", "color=c=navy:s=1280x720", "-frames:v", "1", Path.Combine(dir, "a.png"));
            await Run(ff.FfmpegPath, "-y", "-loglevel", "error", "-f", "lavfi", "-i", "color=c=darkgreen:s=600x900", "-frames:v", "1", Path.Combine(dir, "b.jpg"));
            await Run(ff.FfmpegPath, "-y", "-loglevel", "error", "-f", "lavfi", "-i", "testsrc=size=640x360:rate=25:duration=3",
                "-f", "lavfi", "-i", "sine=frequency=220:duration=3", "-shortest", "-c:v", "libx264", "-c:a", "aac", Path.Combine(dir, "c.mp4"));
            await Run(ff.FfmpegPath, "-y", "-loglevel", "error", "-f", "lavfi", "-i", "sine=frequency=330:duration=4", Path.Combine(dir, "musik.mp3"));

            var script = """
                titel: Probe
                musik: musik.mp3

                [bild: a.png]
                Erster Satz zur Probe. Und noch ein zweiter.

                [clip: c.mp4]
                Text über dem Clip.

                [bild: b.jpg]
                [dauer: 2]

                [clip: c.mp4]
                """;
            var r = ScriptParser.Parse(script, dir);
            Assert.Empty(r.Errors);

            var tts = new ToneTts(ff);
            var builder = new DokuBuilder(ff, tts);
            var output = Path.Combine(dir, "probe.mp4");
            var reports = new List<BuildProgress>();
            var progress = new SyncProgress<BuildProgress>(reports.Add);

            var result = await builder.BuildAsync(r.Project, "ton", "de", output, Path.Combine(dir, "arbeit"),
                new RenderOptions { KenBurns = true, Preset = "ultrafast" }, progress, null, CancellationToken.None);

            Assert.True(File.Exists(output));
            Assert.NotNull(result.SrtPath);
            Assert.Contains("Erster Satz zur Probe.", await File.ReadAllTextAsync(result.SrtPath!));
            Assert.Equal(1.0, reports[^1].Overall, 3);
            Assert.Equal(2, tts.Calls);

            var probe = await ff.ProbeAsync(output, CancellationToken.None);
            Assert.True(probe.HasVideo && probe.HasAudio);
            Assert.InRange(probe.Duration, result.Duration.TotalSeconds - 0.5, result.Duration.TotalSeconds + 0.5);

            // Zweiter Lauf: Stimmen kommen aus dem Zwischenspeicher.
            await builder.BuildAsync(r.Project, "ton", "de", output, Path.Combine(dir, "arbeit"),
                new RenderOptions { Preset = "ultrafast" }, null, null, CancellationToken.None);
            Assert.Equal(2, tts.Calls);
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch (IOException) { }
        }
    }

    /// <summary>Progress&lt;T&gt; meldet ueber den SynchronizationContext; im Test sofort und geordnet melden.</summary>
    private sealed class SyncProgress<T>(Action<T> onReport) : IProgress<T>
    {
        public void Report(T value) => onReport(value);
    }
}
