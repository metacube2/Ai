using System.Net;
using System.Text;
using VideoDoku.Core;

namespace VideoDoku.Tests;

public class ScriptParserTests
{
    private const string Base = @"C:\Doku";

    [Fact]
    public void Liest_Kopf_Szenen_Text_Pause_und_Dauer()
    {
        var r = ScriptParser.Parse("""
            # Kommentar
            titel: Unser Ausflug
            musik: musik/ruhig.mp3

            [bild: bilder/01.jpg]
            Erste Zeile
            zweite Zeile.

            Neuer Absatz.
            [pause: 1,5]

            [clip: clips/see.mp4]

            [bild: bilder/02.png]
            [dauer: 6]
            """, Base);

        Assert.Empty(r.Errors);
        var p = r.Project;
        Assert.Equal("Unser Ausflug", p.Title);
        Assert.Equal(@"C:\Doku\musik\ruhig.mp3", p.MusicPath);
        Assert.Equal(3, p.Scenes.Count);

        Assert.Equal(VisualKind.Image, p.Scenes[0].Kind);
        Assert.Equal("Erste Zeile zweite Zeile.\n\nNeuer Absatz.", p.Scenes[0].Text);
        Assert.Equal(1.5, p.Scenes[0].PauseAfter);

        Assert.Equal(VisualKind.Video, p.Scenes[1].Kind);
        Assert.False(p.Scenes[1].HasText);

        Assert.Equal(6, p.Scenes[2].SilentSeconds);
        Assert.Equal(3, p.Scenes[2].Number);
    }

    [Fact]
    public void Endung_schlaegt_Stichwort()
    {
        var r = ScriptParser.Parse("[bild: a.mp4]\n[clip: b.jpg]", Base);
        Assert.Equal(VisualKind.Video, r.Project.Scenes[0].Kind);
        Assert.Equal(VisualKind.Image, r.Project.Scenes[1].Kind);
    }

    [Fact]
    public void Meldet_Fehler_mit_Zeilennummer()
    {
        var r = ScriptParser.Parse("Text ohne Szene\n[pause: 2]\n[bild: a.xyz]\n[pause: abc]", Base);
        Assert.Contains(r.Errors, e => e.StartsWith("Zeile 1:"));
        Assert.Contains(r.Errors, e => e.StartsWith("Zeile 2:"));
        Assert.Contains(r.Errors, e => e.StartsWith("Zeile 3:") && e.Contains(".xyz"));
        Assert.Contains(r.Errors, e => e.StartsWith("Zeile 4:"));
    }

    [Fact]
    public void Leeres_Skript_ist_ein_Fehler() =>
        Assert.Contains(ScriptParser.Parse("titel: x", Base).Errors, e => e.Contains("keine Szene"));

    [Fact]
    public void Skript_schreiben_und_wieder_lesen_ergibt_dasselbe()
    {
        var original = ScriptParser.Parse("""
            titel: Rundlauf
            musik: musik/a.mp3

            [bild: bilder/01.jpg]
            Erster Absatz.

            Zweiter Absatz.
            [pause: 2]

            [clip: clips/b.mp4]
            Text zum Clip.

            [bild: bilder/02.png]
            [dauer: 6]
            """, Base).Project;

        var text = ScriptWriter.Write(original, Base);
        var again = ScriptParser.Parse(text, Base);

        Assert.Empty(again.Errors);
        Assert.Equal(original.Title, again.Project.Title);
        Assert.Equal(original.MusicPath, again.Project.MusicPath);
        Assert.Equal(original.Scenes.Count, again.Project.Scenes.Count);
        for (var i = 0; i < original.Scenes.Count; i++)
        {
            var a = original.Scenes[i];
            var b = again.Project.Scenes[i];
            Assert.Equal(a.VisualPath, b.VisualPath);
            Assert.Equal(a.Kind, b.Kind);
            Assert.Equal(a.Text, b.Text);
            Assert.Equal(a.PauseAfter, b.PauseAfter);
            Assert.Equal(a.SilentSeconds, b.SilentSeconds);
        }
        Assert.Contains("[bild: bilder/01.jpg]", text);
    }

    [Fact]
    public void Absolute_Pfade_bleiben()
    {
        var r = ScriptParser.Parse("[bild: \"D:\\Fotos\\a.jpg\"]", Base);
        Assert.Equal(@"D:\Fotos\a.jpg", r.Project.Scenes[0].VisualPath);
    }
}

public class SubtitleTests
{
    [Fact]
    public void Teilt_an_Satzenden_und_kuerzt_lange_Saetze()
    {
        var lang = string.Join(' ', Enumerable.Repeat("Wort", 40));
        var chunks = Subtitles.Chunk($"Kurz. Noch ein Satz! {lang}.");
        Assert.Equal("Kurz.", chunks[0]);
        Assert.Equal("Noch ein Satz!", chunks[1]);
        Assert.All(chunks, c => Assert.True(c.Length <= Subtitles.MaxCueChars));
    }

    [Fact]
    public void Zeiten_nach_Zeichen_verteilt_und_lueckenlos()
    {
        var cues = Subtitles.ForScene("Aaaa. Bbbbbbbbb.", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(3));
        Assert.Equal(2, cues.Count);
        Assert.Equal(TimeSpan.FromSeconds(10), cues[0].Start);
        Assert.Equal(cues[0].End, cues[1].Start);
        Assert.Equal(13, cues[1].End.TotalSeconds, 2);
        Assert.True(cues[1].End - cues[1].Start > cues[0].End - cues[0].Start);
    }

    [Fact]
    public void Srt_Format()
    {
        var srt = Subtitles.ToSrt([new Subtitles.Cue(TimeSpan.FromMilliseconds(1500), TimeSpan.FromSeconds(62.25), "Hallo")]);
        Assert.Equal("1\n00:00:01,500 --> 00:01:02,250\nHallo\n\n", srt);
    }

    [Fact]
    public void Lange_Untertitel_zweizeilig() =>
        Assert.Contains('\n', Subtitles.Wrap("Dies ist ein ziemlich langer Untertitel mit vielen Wörtern"));
}

public class FfmpegCommandTests
{
    private static readonly RenderOptions O = new();

    private static Scene Img(string text = "Hallo") => new() { Number = 1, VisualPath = @"C:\a.jpg", Kind = VisualKind.Image, Text = text };
    private static Scene Clip(string text = "") => new() { Number = 2, VisualPath = @"C:\b.mp4", Kind = VisualKind.Video, Text = text };

    [Fact]
    public void Dauer_aus_Stimme_Clip_oder_Standbild()
    {
        Assert.Equal(0.4 + 3 + 0.8, FfmpegCommands.SceneDuration(Img(), 3, null, O), 3);
        Assert.Equal(7.5, FfmpegCommands.SceneDuration(Clip(), null, 7.5, O), 3);
        Assert.Equal(4, FfmpegCommands.SceneDuration(Img(""), null, null, O), 3);
    }

    [Fact]
    public void Bildszene_mit_Stimme()
    {
        var a = string.Join(' ', FfmpegCommands.Scene(Img(), @"C:\v.mp3", false, 4.2, @"C:\o.mp4", O));
        Assert.Contains("-loop 1", a);
        Assert.Contains("adelay=400|400", a);
        Assert.Contains("fade=t=out:st=3.8", a);
        Assert.Contains("-t 4.2", a);
        Assert.Contains("libx264", a);
        Assert.EndsWith(@"C:\o.mp4", a);
    }

    [Fact]
    public void Clip_mit_Stimme_mischt_Originalton_leise_darunter()
    {
        var a = string.Join(' ', FfmpegCommands.Scene(Clip("Text"), @"C:\v.mp3", true, 5, @"C:\o.mp4", O));
        Assert.Contains("volume=0.15", a);
        Assert.Contains("amix=inputs=2", a);
        Assert.Contains("tpad=stop_mode=clone", a);
    }

    [Fact]
    public void Ohne_Ton_wird_Stille_erzeugt()
    {
        var a = string.Join(' ', FfmpegCommands.Scene(Img(""), null, false, 4, @"C:\o.mp4", O));
        Assert.Contains("anullsrc", a);
    }

    [Fact]
    public void Zoom_nur_wenn_gewuenscht()
    {
        var mit = string.Join(' ', FfmpegCommands.Scene(Img(), @"C:\v.mp3", false, 4, @"C:\o.mp4", new RenderOptions { KenBurns = true }));
        var ohne = string.Join(' ', FfmpegCommands.Scene(Img(), @"C:\v.mp3", false, 4, @"C:\o.mp4", O));
        Assert.Contains("zoompan", mit);
        Assert.DoesNotContain("zoompan", ohne);
    }

    [Fact]
    public void Schluss_mit_Musik_und_Untertiteln()
    {
        var a = string.Join(' ', FfmpegCommands.Final(@"C:\l.txt", @"C:\m.mp3", @"C:\s.srt", "Titel", @"C:\out.mp4", O));
        Assert.Contains("-stream_loop -1 -i C:\\m.mp3", a);
        Assert.Contains("sidechaincompress", a);
        Assert.Contains("-map 2:s -c:s mov_text", a);
        Assert.Contains("+faststart", a);

        var ohne = string.Join(' ', FfmpegCommands.Final(@"C:\l.txt", null, null, "T", @"C:\out.mp4", O));
        Assert.Contains("-map 0:a -c:a copy", ohne);
        Assert.DoesNotContain("mov_text", ohne);
    }

    [Fact]
    public void Concat_Liste_maskiert_Apostroph()
    {
        Assert.Equal("file 'C:/a/it'\\''s.mp4'\n", FfmpegCommands.ConcatList([@"C:\a\it's.mp4"]));
    }
}

public class TtsClientTests
{
    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<string> Calls { get; } = new();
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct)
        {
            Calls.Add($"{r.Method} {r.RequestUri!.AbsolutePath} {r.Headers.Authorization}");
            return Task.FromResult(respond(r));
        }
    }

    private static HttpResponseMessage Json(HttpStatusCode code, string json) =>
        new(code) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task Kurzer_Text_liefert_Audio_direkt_mit_Bearer()
    {
        var h = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2, 3]) });
        var c = new IngokTtsClient(new HttpClient(h), "geheim");
        var audio = await c.SpeakAsync("Hallo", "horst", "de", CancellationToken.None);
        Assert.Equal([1, 2, 3], audio);
        Assert.Equal("POST /api/tts/v1/speak Bearer geheim", h.Calls.Single());
    }

    [Fact]
    public async Task Langer_Text_ueber_Job()
    {
        var polls = 0;
        var h = new FakeHandler(r => r.RequestUri!.AbsolutePath switch
        {
            "/api/tts/v1/speak" => Json(HttpStatusCode.Accepted, """{"id":"j1"}"""),
            "/api/tts/v1/jobs/j1" => Json(HttpStatusCode.OK, ++polls < 2 ? """{"status":"running"}""" : """{"status":"done"}"""),
            "/api/tts/v1/jobs/j1/audio" => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([9]) },
            _ => new HttpResponseMessage(HttpStatusCode.NotFound),
        });
        var c = new IngokTtsClient(new HttpClient(h), "k", pollInterval: TimeSpan.FromMilliseconds(1));
        Assert.Equal([9], await c.SpeakAsync("lang", "horst", "de", CancellationToken.None));
        Assert.Equal(2, polls);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "401")]
    [InlineData(HttpStatusCode.Forbidden, "403")]
    [InlineData((HttpStatusCode)429, "429")]
    public async Task Fehler_werden_verstaendlich(HttpStatusCode code, string expected)
    {
        var c = new IngokTtsClient(new HttpClient(new FakeHandler(_ => Json(code, "{}"))), "k");
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => c.SpeakAsync("x", "horst", "de", CancellationToken.None));
        Assert.Contains(expected, ex.Message);
    }

    [Fact]
    public async Task Stimmenliste_in_zwei_Formen()
    {
        var a = new IngokTtsClient(new HttpClient(new FakeHandler(_ => Json(HttpStatusCode.OK, """["horst"]"""))), "k");
        Assert.Equal("horst", (await a.VoicesAsync(CancellationToken.None)).Single().Id);
        var b = new IngokTtsClient(new HttpClient(new FakeHandler(_ => Json(HttpStatusCode.OK, """{"voices":[{"id":"horst","name":"Horst"}]}"""))), "k");
        Assert.StartsWith("Horst", (await b.VoicesAsync(CancellationToken.None)).Single().Label);
    }
}
