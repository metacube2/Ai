using System.IO;
using System.IO.Compression;
using System.Text;
using NAudio.Wave;
using WhisperStudio.Core;
using Xunit;

namespace WhisperStudio.Tests;

public class OutputNamingTests
{
    [Theory]
    [InlineData("audio", true)]
    [InlineData("au3333dio", true)]
    [InlineData("audio (2)", true)]
    [InlineData("Data", true)]
    [InlineData("Neuer Ordner", true)]
    [InlineData("Einkauf", false)]
    [InlineData("Kickoff Lieferanten", false)]
    [InlineData("einklauf", false)]
    public void IsGeneric(string name, bool expected) => Assert.Equal(expected, OutputNaming.IsGeneric(name));

    [Fact]
    public void Title_comes_from_first_meaningful_folder_when_file_is_called_audio()
    {
        // Ingo's real layout: ...\Desktop\einklauf\Data\audio.wav
        var root = Path.Combine(Path.GetTempPath(), "ws_" + Guid.NewGuid().ToString("N"), "Desktop", "einklauf", "Data");
        Directory.CreateDirectory(root);
        var file = Path.Combine(root, "audio.wav");
        File.WriteAllBytes(file, new byte[] { 0 });
        Assert.Equal("einklauf", OutputNaming.SuggestTitle(file));
    }

    [Fact]
    public void Title_is_file_name_when_meaningful()
        => Assert.Equal("Kickoff Lieferanten", OutputNaming.SuggestTitle(@"C:\x\Data\Kickoff Lieferanten.m4a"));

    [Fact]
    public void Stem_is_date_time_title_and_deterministic()
    {
        var at = new DateTime(2026, 10, 22, 14, 30, 59);
        Assert.Equal("2026-10-22_1430_Einkauf", OutputNaming.StemFor(@"C:\a\audio.wav", at, "Einkauf"));
        Assert.Equal(OutputNaming.StemFor(@"C:\a\audio.wav", at, "Einkauf"), OutputNaming.StemFor(@"C:\a\audio.wav", at, "Einkauf"));
    }

    [Fact]
    public void Studio_recordings_keep_their_name()
        => Assert.Equal("2026-10-22_0915_Daily", OutputNaming.StemFor(@"C:\w\input\2026-10-22_0915_Daily.wav", DateTime.Now, "Anders"));

    [Fact]
    public void Sanitize_removes_invalid_characters_and_caps_length()
    {
        Assert.Equal("Einkauf- Q4-Plan", OutputNaming.Sanitize("Einkauf: Q4/Plan"));
        Assert.True(OutputNaming.Sanitize(new string('x', 200)).Length <= 60);
        Assert.Equal("2026-10-22_1430", OutputNaming.BuildStem(new DateTime(2026, 10, 22, 14, 30, 0), "  "));
    }
}

public class ExportTests
{
    private static readonly List<Segment> Segs = new()
    {
        new(0, 2.5, "Guten Morgen."),
        new(3661.25, 3663.004, "Punkt zwei."),
    };

    [Fact]
    public void Srt_format()
        => Assert.Equal("1\n00:00:00,000 --> 00:00:02,500\nGuten Morgen.\n\n2\n01:01:01,250 --> 01:01:03,004\nPunkt zwei.\n\n", TranscriptExport.ToSrt(Segs));

    [Fact]
    public void Vtt_format()
        => Assert.StartsWith("WEBVTT\n\n00:00:00.000 --> 00:00:02.500\nGuten Morgen.\n\n01:01:01.250", TranscriptExport.ToVtt(Segs));

    [Fact]
    public void Body_strips_header_block()
        => Assert.Equal("Hallo\nWelt", TranscriptExport.BodyOf("Source: x\nModel: tiny\nDetected language: de (1.00)\n\nHallo\nWelt\n"));

    [Fact]
    public void Body_without_header_is_unchanged()
        => Assert.Equal("Nur Text", TranscriptExport.BodyOf("Nur Text\n"));

    [Fact]
    public void ExportAll_writes_srt_vtt_and_a_valid_docx_from_the_sidecar()
    {
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "ws_" + Guid.NewGuid().ToString("N"))).FullName;
        var txt = Path.Combine(dir, "2026-10-22_1430_Einkauf.txt");
        File.WriteAllText(txt, "Source: a\nModel: tiny\n\nGuten Morgen.\nPunkt zwei.\n");
        File.WriteAllText(Path.ChangeExtension(txt, ".json"),
            "{\"model\":\"tiny\",\"language\":\"de\",\"duration\":3700,\"segments\":[{\"start\":0,\"end\":2.5,\"text\":\"Guten Morgen.\"}]}",
            Encoding.UTF8);
        File.SetLastWriteTimeUtc(txt, DateTime.UtcNow.AddMinutes(-1)); // not edited

        var files = TranscriptExport.ExportAll(txt, srt: true, vtt: true, docx: true);

        Assert.Equal(3, files.Count);
        Assert.Contains("Guten Morgen.", File.ReadAllText(Path.ChangeExtension(txt, ".srt")));
        using var zip = ZipFile.OpenRead(Path.ChangeExtension(txt, ".docx"));
        using var reader = new StreamReader(zip.GetEntry("word/document.xml")!.Open());
        var xml = reader.ReadToEnd();
        Assert.Contains("Guten Morgen.", xml);
        Assert.Contains("[00:00:00]", xml);
    }

    [Fact]
    public void Docx_uses_edited_text_when_txt_is_newer_than_sidecar()
    {
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "ws_" + Guid.NewGuid().ToString("N"))).FullName;
        var txt = Path.Combine(dir, "t.txt");
        var json = Path.ChangeExtension(txt, ".json");
        File.WriteAllText(json, "{\"segments\":[{\"start\":0,\"end\":1,\"text\":\"roh\"}]}");
        File.SetLastWriteTimeUtc(json, DateTime.UtcNow.AddMinutes(-10));
        File.WriteAllText(txt, "Source: a\n\nkorrigiert\n");

        TranscriptExport.ExportAll(txt, srt: false, vtt: false, docx: true);

        using var zip = ZipFile.OpenRead(Path.ChangeExtension(txt, ".docx"));
        using var reader = new StreamReader(zip.GetEntry("word/document.xml")!.Open());
        var xml = reader.ReadToEnd();
        Assert.Contains("korrigiert", xml);
        Assert.DoesNotContain(">roh<", xml);
    }
}

public class RunnerAndAiTests
{
    [Fact]
    public void Parses_progress_line()
    {
        Assert.True(WhisperRunner.TryParseProgress("PROGRESS 43.5 120 3600", out var pct, out var total));
        Assert.Equal(43.5, pct);
        Assert.Equal(3600, total);
        Assert.False(WhisperRunner.TryParseProgress("Written: x", out _, out _));
    }

    [Fact]
    public void Chunks_respect_limit_and_keep_all_text()
    {
        var lines = Enumerable.Range(0, 400).Select(i => $"Zeile {i} mit etwas Inhalt aus dem Meeting.").ToList();
        var text = string.Join("\n", lines);
        var chunks = AiTasks.Chunk(text, 1000);
        Assert.All(chunks, c => Assert.True(c.Length <= 1000));
        Assert.Equal(lines.Count, chunks.Sum(c => c.Split('\n').Length));
    }

    [Theory]
    [InlineData("```markdown\n## A\ntext\n```", "## A\ntext")]
    [InlineData("## A\ntext", "## A\ntext")]
    [InlineData("vorher ```code``` nachher", "vorher ```code``` nachher")]
    public void Strips_outer_code_fence(string input, string expected) => Assert.Equal(expected, AiTasks.StripFence(input));

    [Fact]
    public void Overlong_single_line_is_cut()
    {
        var chunks = AiTasks.Chunk(new string('a', 2500), 1000);
        Assert.Equal(3, chunks.Count);
        Assert.Equal(2500, chunks.Sum(c => c.Length));
    }
}

public class RecorderMixTests
{
    private static string WriteTone(string path, int rate, int channels, double seconds, float amp)
    {
        var fmt = WaveFormat.CreateIeeeFloatWaveFormat(rate, channels);
        using var w = new WaveFileWriter(path, fmt);
        var frames = (int)(rate * seconds);
        for (var i = 0; i < frames; i++)
        {
            var v = (float)(amp * Math.Sin(2 * Math.PI * 440 * i / rate));
            for (var c = 0; c < channels; c++) w.WriteSample(v);
        }
        return path;
    }

    [Fact]
    public void Mixes_mic_and_system_into_16k_mono_of_full_length()
    {
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "ws_" + Guid.NewGuid().ToString("N"))).FullName;
        var mic = WriteTone(Path.Combine(dir, "mic.wav"), 48000, 2, 3.0, 0.3f);
        var sys = WriteTone(Path.Combine(dir, "sys.wav"), 44100, 2, 3.0, 0.3f);
        var target = Path.Combine(dir, "out.wav");

        Recorder.Mix(mic, sys, target);

        using var r = new WaveFileReader(target);
        Assert.Equal(16000, r.WaveFormat.SampleRate);
        Assert.Equal(1, r.WaveFormat.Channels);
        Assert.Equal(16, r.WaveFormat.BitsPerSample);
        Assert.InRange(r.TotalTime.TotalSeconds, 2.9, 3.1);
    }

    [Fact]
    public void Mic_only_works()
    {
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "ws_" + Guid.NewGuid().ToString("N"))).FullName;
        var mic = WriteTone(Path.Combine(dir, "mic.wav"), 48000, 1, 2.0, 0.3f);
        var target = Path.Combine(dir, "out.wav");
        Recorder.Mix(mic, null, target);
        using var r = new WaveFileReader(target);
        Assert.InRange(r.TotalTime.TotalSeconds, 1.9, 2.1);
    }
}

/// <summary>Runs only when WS_LLM_URL points at a running llama-server (manual check, not CI).</summary>
public class LlmIntegrationTests
{
    private static string? Url => Environment.GetEnvironmentVariable("WS_LLM_URL");

    [Fact]
    public async Task Protocol_from_short_transcript()
    {
        if (Url is null) return;
        var llm = new LlmClient(Url, "local");
        Assert.True(await llm.IsReachableAsync());
        var streamed = new StringBuilder();
        var transcript = "Guten Morgen zusammen. Heute geht es um die Lieferzeiten beim Lieferanten Meier. " +
                         "Anna klärt bis Freitag, ob die Drucksensoren früher geliefert werden können. " +
                         "Wir beschliessen, die Bestellung für November um zwei Wochen vorzuziehen.";
        var result = await AiTasks.ProtocolAsync(llm, transcript, t => streamed.Append(t), _ => { }, CancellationToken.None);
        File.WriteAllText(Path.Combine(Path.GetTempPath(), "ws_protokoll_test.md"), result);
        Assert.Equal(result, streamed.ToString());
        result = AiTasks.StripFence(result);
        Assert.Contains("Anna", result);
        Assert.Contains("Pendenzen", result);
    }
}

/// <summary>Runs only when WS_WHISPER_MEDIA names a short speech file (manual check, uses the tiny model).</summary>
public class WhisperIntegrationTests
{
    [Fact]
    public async Task Runner_writes_txt_and_json_and_exports_follow()
    {
        var media = Environment.GetEnvironmentVariable("WS_WHISPER_MEDIA");
        if (media is null) return;
        var paths = StudioPaths.Find();
        Assert.NotNull(paths);
        var outTxt = Path.Combine(Path.GetTempPath(), "ws_" + Guid.NewGuid().ToString("N"), OutputNaming.BuildStem(new DateTime(2026, 10, 22, 14, 30, 0), "Integration") + ".txt");
        var runner = new WhisperRunner(paths!);
        var progress = new List<double>();
        runner.Progress += (p, _) => progress.Add(p);

        var result = await runner.RunAsync(media, outTxt, new RunOptions("de", "tiny", "transcribe", false), CancellationToken.None);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(outTxt, result.WrittenPath);
        Assert.True(File.Exists(Path.ChangeExtension(outTxt, ".json")));
        Assert.Equal(100, progress.Last());
        var files = TranscriptExport.ExportAll(outTxt, srt: true, vtt: true, docx: true);
        Assert.Equal(3, files.Count);
        Assert.StartsWith("1\n00:00:00", File.ReadAllText(Path.ChangeExtension(outTxt, ".srt")));
    }
}

public class ResumeTests
{
    [Fact]
    public void Finds_earlier_transcript_by_source_even_with_other_title()
    {
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "ws_" + Guid.NewGuid().ToString("N"))).FullName;
        var media = Path.Combine(dir, "audio.wav");
        var txt = Path.Combine(dir, "2026-10-22_1430_Von Hand geaendert.txt");
        File.WriteAllText(txt, "Text");
        File.WriteAllText(Path.ChangeExtension(txt, ".json"),
            System.Text.Json.JsonSerializer.Serialize(new { source = media.ToUpperInvariant(), segments = Array.Empty<object>() }));
        Assert.Equal(txt, TranscriptMeta.FindExistingTranscript(media, dir));
        Assert.Null(TranscriptMeta.FindExistingTranscript(Path.Combine(dir, "anderes.wav"), dir));
    }
}

/// <summary>Runs only with WS_RECORD=1: records 3 s from the default mic plus system sound.</summary>
public class RecorderLiveTests
{
    [Fact]
    public async Task Records_mic_and_loopback_for_three_seconds()
    {
        if (Environment.GetEnvironmentVariable("WS_RECORD") != "1") return;
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "ws_" + Guid.NewGuid().ToString("N"))).FullName;
        using var rec = new Recorder();
        rec.Start(null, withSystemAudio: true, dir);
        await Task.Delay(3000);
        var target = await rec.StopAsync(Path.Combine(dir, "out.wav"));
        using var r = new WaveFileReader(target);
        Assert.Equal(16000, r.WaveFormat.SampleRate);
        Assert.Equal(1, r.WaveFormat.Channels);
        Assert.InRange(r.TotalTime.TotalSeconds, 2.7, 3.5);
        Assert.Single(Directory.GetFiles(dir, "*.wav")); // temp tracks removed
    }
}
