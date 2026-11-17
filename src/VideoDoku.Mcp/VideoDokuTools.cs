using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using VideoDoku.Core;

namespace VideoDoku.Mcp;

/// <summary>
/// Werkzeuge fuer die KI. Alles, was die Oberflaeche kann, geht auch hier: Stimmen, PowerPoint oder
/// Skript oeffnen, Projekt aus Bildern/Clips anlegen, Texte aendern, Skript speichern, Video erstellen,
/// Fortschritt abfragen, warten, abbrechen, einzelne Sprachdatei erzeugen, Oberflaeche oeffnen.
/// Antworten sind JSON. Fehler gehen als McpException mit Klartext zurueck – andere Ausnahmen zeigt
/// das MCP-SDK der KI nur als „An error occurred“ (beim ersten Test am 17.11. so passiert).
/// </summary>
[McpServerToolType]
public sealed class VideoDokuTools(DokuService svc)
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static string Out(object o) => JsonSerializer.Serialize(o, Json);

    private static string Safe(Func<string> work)
    {
        try { return work(); }
        catch (McpException) { throw; }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { throw new McpException(ex.Message, ex); }
    }

    private static async Task<string> SafeAsync(Func<Task<string>> work)
    {
        try { return await work(); }
        catch (McpException) { throw; }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { throw new McpException(ex.Message, ex); }
    }

    [McpServerTool(Name = "videodoku_status"), Description(
        "Zustand des Video-Werkzeugs: ist ffmpeg da, ist ein Schlüssel für den Sprachdienst gespeichert, welche Stimmen gibt es, " +
        "welche Projekte und Aufträge sind offen. Gut als erster Aufruf.")]
    public Task<string> Status(CancellationToken ct) => SafeAsync(async () =>
    {
        var voices = await svc.VoicesAsync(ct);
        return Out(new
        {
            ffmpeg = svc.Ffmpeg.IsAvailable ? svc.Ffmpeg.FfmpegPath : "FEHLT",
            sprachdienst = svc.Settings.ServiceUrl,
            schluessel_gespeichert = Windows.AppSettings.LoadKey() is not null,
            hinweis_sprachdienst = voices.ServiceError,
            stimmen = voices.Voices.Select(v => new { id = v.Id, name = v.Label }),
            projekte = svc.Projects.Select(p => new { id = p.Id, titel = p.Project.Title, szenen = p.Project.Scenes.Count, quelle = p.SourcePath }),
            auftraege = svc.Jobs.Select(j => JobView(j, false)),
        });
    });

    [McpServerTool(Name = "videodoku_list_voices"), Description(
        "Listet die Stimmen: zuerst die des Sprachdienstes tts.ingok.ch (z. B. horst, natürlich klingend), dann lokale Windows-Stimmen (z. B. Karsten).")]
    public Task<string> ListVoices(CancellationToken ct) => SafeAsync(async () =>
    {
        var v = await svc.VoicesAsync(ct);
        return Out(new { stimmen = v.Voices.Select(x => new { id = x.Id, name = x.Label, quelle = x.Provider }), hinweis = v.ServiceError });
    });

    [McpServerTool(Name = "videodoku_open"), Description(
        "Öffnet eine PowerPoint-Datei (.pptx: sichtbare Folien werden als Bilder exportiert, Sprechernotizen werden zum Text) " +
        "oder ein Skript (.txt). Liefert die Projekt-Id und alle Szenen.")]
    public Task<string> Open(
        [Description("Vollständiger Pfad zur .pptx- oder Skriptdatei.")] string path,
        CancellationToken ct) => SafeAsync(async () =>
    {
        var p = await svc.OpenAsync(path, ct);
        return Out(ProjectView(p, ScriptParser.CheckFiles(p.Project)));
    });

    public sealed class SceneInput
    {
        [Description("Pfad zu Bild (.jpg/.png …) oder Videoclip (.mp4 …), absolut oder relativ zum Ordner.")]
        public string Visual { get; set; } = "";
        [Description("Sprechertext. Leer: Bild steht still (silent_seconds), Clip läuft mit Originalton.")]
        public string? Text { get; set; }
        [Description("Pause nach dem Sprechen in Sekunden (Standard 0.8).")]
        public double? PauseAfter { get; set; }
        [Description("Dauer eines Bildes ohne Text in Sekunden (Standard 4).")]
        public double? SilentSeconds { get; set; }
    }

    [McpServerTool(Name = "videodoku_create_project"), Description(
        "Legt eine freie Doku aus Bildern und Clips an: schreibt skript.txt in den Ordner und öffnet das Projekt. " +
        "Fehlende Dateien werden in fehlende_dateien gemeldet. Danach mit videodoku_build ein Video erstellen.")]
    public string CreateProject(
        [Description("Ordner, in dem das Skript (und später das Video) liegt.")] string folder,
        [Description("Titel der Doku.")] string title,
        [Description("Szenen in Reihenfolge.")] SceneInput[] scenes,
        [Description("Optional: Hintergrundmusik (mp3/wav), wird beim Sprechen automatisch leiser.")] string? music = null,
        [Description("Optional: Dateiname des Skripts, Standard skript.txt.")] string? scriptName = null) => Safe(() =>
    {
        var dir = Directory.CreateDirectory(Path.GetFullPath(folder)).FullName;
        var project = new DokuProject { Title = title, BaseFolder = dir, MusicPath = music is null ? null : Resolve(dir, music) };
        foreach (var s in scenes)
        {
            var visual = Resolve(dir, s.Visual);
            var ext = Path.GetExtension(visual).ToLowerInvariant();
            var kind = ScriptParser.VideoExtensions.Contains(ext) ? VisualKind.Video
                : ScriptParser.ImageExtensions.Contains(ext) ? VisualKind.Image
                : throw new InvalidOperationException($"Unbekannter Dateityp „{ext}“: {s.Visual}");
            project.Scenes.Add(new Scene
            {
                Number = project.Scenes.Count + 1,
                VisualPath = visual,
                Kind = kind,
                Text = ScriptParser.Normalize(s.Text ?? ""),
                PauseAfter = s.PauseAfter ?? 0.8,
                SilentSeconds = s.SilentSeconds ?? 4,
                Origin = $"Szene {project.Scenes.Count + 1}",
            });
        }
        var script = Path.Combine(dir, string.IsNullOrWhiteSpace(scriptName) ? "skript.txt" : scriptName);
        ScriptWriter.Save(project, script);
        var open = svc.Add(project, script, script);
        open.OwnsScript = true;
        return Out(ProjectView(open, ScriptParser.CheckFiles(project)));
    });

    [McpServerTool(Name = "videodoku_get_project"), Description("Zeigt ein geöffnetes Projekt mit allen Szenen und Texten.")]
    public string GetProject([Description("Projekt-Id, z. B. p1.")] string projectId) => Safe(() =>
    {
        var p = svc.Get(projectId);
        return Out(ProjectView(p, ScriptParser.CheckFiles(p.Project)));
    });

    [McpServerTool(Name = "videodoku_set_scene"), Description(
        "Ändert eine Szene: Sprechertext, Pause danach oder Standdauer ohne Text. Nicht angegebene Werte bleiben. " +
        "Absätze im Text mit einer Leerzeile trennen. Ein mit videodoku_create_project angelegtes Skript wird automatisch mitgespeichert; " +
        "bei geöffneten Skripten oder PowerPoint mit videodoku_save_script speichern.")]
    public string SetScene(
        [Description("Projekt-Id.")] string projectId,
        [Description("Szenennummer ab 1.")] int scene,
        [Description("Neuer Sprechertext (leer = ohne Stimme).")] string? text = null,
        [Description("Pause nach dem Sprechen in Sekunden.")] double? pauseAfter = null,
        [Description("Standdauer ohne Text in Sekunden.")] double? silentSeconds = null) => Safe(() =>
    {
        var p = svc.Get(projectId);
        var s = p.Project.Scenes.FirstOrDefault(x => x.Number == scene)
                ?? throw new InvalidOperationException($"Szene {scene} gibt es nicht (1–{p.Project.Scenes.Count}).");
        if (text is not null) s.Text = ScriptParser.Normalize(text);
        if (pauseAfter is { } pa) s.PauseAfter = Math.Clamp(pa, 0, 60);
        if (silentSeconds is { } ss) s.SilentSeconds = Math.Clamp(ss, 1, 600);
        AutoSave(p);
        return Out(SceneView(s));
    });

    [McpServerTool(Name = "videodoku_set_project"), Description("Ändert Titel oder Hintergrundmusik eines Projekts. music=\"\" entfernt die Musik.")]
    public string SetProject(
        [Description("Projekt-Id.")] string projectId,
        [Description("Neuer Titel.")] string? title = null,
        [Description("Pfad zur Musik, oder leer zum Entfernen.")] string? music = null) => Safe(() =>
    {
        var p = svc.Get(projectId);
        if (title is not null) p.Project.Title = title;
        if (music is not null) p.Project.MusicPath = music.Length == 0 ? null : Resolve(p.Project.BaseFolder, music);
        AutoSave(p);
        return Out(ProjectView(p, ScriptParser.CheckFiles(p.Project)));
    });

    [McpServerTool(Name = "videodoku_save_script"), Description(
        "Speichert das Projekt als Skriptdatei (z. B. nach Textänderungen), damit es später wieder geöffnet " +
        "oder in der Oberfläche angesehen werden kann.")]
    public string SaveScript(
        [Description("Projekt-Id.")] string projectId,
        [Description("Zielpfad .txt; Standard: das bisherige Skript bzw. <Quelle>_skript.txt.")] string? path = null) => Safe(() =>
    {
        var p = svc.Get(projectId);
        var target = Path.GetFullPath(path ?? p.ScriptPath ??
            Path.Combine(Path.GetDirectoryName(p.SourcePath) ?? "", Path.GetFileNameWithoutExtension(p.SourcePath) + "_skript.txt"));
        ScriptWriter.Save(p.Project, target);
        p.ScriptPath = target;
        return Out(new { gespeichert = target });
    });

    [McpServerTool(Name = "videodoku_build"), Description(
        "Startet die Videoerstellung im Hintergrund und liefert sofort eine Auftrags-Id. Fortschritt mit videodoku_job, " +
        "auf das Ende warten mit videodoku_wait. Der Fortschritt erscheint auch im Agenten-Monitor.")]
    public Task<string> Build(
        [Description("Projekt-Id.")] string projectId,
        [Description("Stimme: Id oder Teil des Namens (z. B. horst, Karsten). Ohne Angabe: Sprachdienst, sonst Windows.")] string? voice = null,
        [Description("Ziel-MP4; Standard: neben der Quelle.")] string? output = null,
        [Description("Untertitel als abschaltbare Spur und .srt-Datei (Standard ja).")] bool subtitles = true,
        [Description("Sanfter Zoom auf Bildern; für Folien besser aus (Standard: aus bei PowerPoint, sonst an).")] bool? kenBurns = null,
        CancellationToken ct = default) => SafeAsync(async () =>
    {
        var p = svc.Get(projectId);
        var missing = ScriptParser.CheckFiles(p.Project);
        if (missing.Count > 0) throw new InvalidOperationException(string.Join(" ", missing));
        var isSlides = p.SourcePath.EndsWith(".pptx", StringComparison.OrdinalIgnoreCase) || p.SourcePath.EndsWith(".ppt", StringComparison.OrdinalIgnoreCase);
        var options = new RenderOptions { Subtitles = subtitles, KenBurns = kenBurns ?? !isSlides };
        var job = await svc.StartBuildAsync(p, voice, output, options, ct);
        return Out(JobView(job, false));
    });

    [McpServerTool(Name = "videodoku_job"), Description("Stand eines Video-Auftrags: Prozent, Meldung, Stand je Szene, Ergebnis oder Fehler.")]
    public string JobStatus([Description("Auftrags-Id, z. B. j1.")] string jobId) =>
        Safe(() => Out(JobView(svc.GetJob(jobId), detailed: true)));

    [McpServerTool(Name = "videodoku_wait"), Description(
        "Wartet, bis der Auftrag fertig, abgebrochen oder fehlgeschlagen ist, höchstens timeout_seconds (Standard 300). " +
        "Läuft er danach noch, einfach erneut aufrufen.")]
    public Task<string> Wait(
        [Description("Auftrags-Id.")] string jobId,
        [Description("Höchstens so lange warten (Sekunden, 1–1800).")] int timeoutSeconds = 300,
        CancellationToken ct = default) => SafeAsync(async () =>
    {
        var job = svc.GetJob(jobId);
        if (job.Task is { } t)
            await Task.WhenAny(t, Task.Delay(TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 1, 1800)), ct));
        return Out(JobView(job, detailed: true));
    });

    [McpServerTool(Name = "videodoku_cancel"), Description("Bricht einen laufenden Video-Auftrag ab.")]
    public string Cancel([Description("Auftrags-Id.")] string jobId) => Safe(() =>
    {
        var job = svc.GetJob(jobId);
        job.Cts.Cancel();
        return Out(new { auftrag = job.Id, abbruch_angefordert = true });
    });

    [McpServerTool(Name = "videodoku_speak"), Description(
        "Erzeugt nur eine Sprachdatei aus Text (ohne Video), z. B. für eine Folie oder zum Probehören. " +
        "Sprachdienst liefert MP3, Windows-Stimmen WAV; die Endung wird passend gesetzt.")]
    public Task<string> Speak(
        [Description("Text.")] string text,
        [Description("Zielpfad ohne oder mit Endung.")] string outputPath,
        [Description("Stimme: Id oder Teil des Namens.")] string? voice = null,
        CancellationToken ct = default) => SafeAsync(async () =>
    {
        var (v, provider) = await svc.ResolveVoiceAsync(voice, ct);
        var bytes = await provider.SpeakAsync(text, v.Id, svc.Settings.Language, ct);
        var target = Path.ChangeExtension(Path.GetFullPath(outputPath), provider.Extension);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        await File.WriteAllBytesAsync(target, bytes, ct);
        var probe = await svc.Ffmpeg.ProbeAsync(target, ct);
        return Out(new { datei = target, stimme = v.Label, sekunden = Math.Round(probe.Duration, 1) });
    });

    [McpServerTool(Name = "videodoku_open_ui"), Description(
        "Öffnet die Oberfläche des Video-Werkzeugs für den Menschen, optional gleich mit einer Datei (Skript, PowerPoint).")]
    public string OpenUi([Description("Optional: Datei zum Öffnen.")] string? path = null) => Safe(() =>
    {
        DokuService.OpenInUi(path is null ? null : Path.GetFullPath(path));
        return Out(new { geoeffnet = true, datei = path });
    });

    /// <summary>Vom Werkzeug angelegte Skripte immer aktuell halten (fremde Skripte nie ungefragt ueberschreiben).</summary>
    private static void AutoSave(DokuService.OpenProject p)
    {
        if (p.OwnsScript && p.ScriptPath is { } path) ScriptWriter.Save(p.Project, path);
    }

    // ------------------------------------------------------------------ Darstellung

    private static object ProjectView(DokuService.OpenProject p, IReadOnlyList<string> missing) => new
    {
        projekt = p.Id,
        titel = p.Project.Title,
        quelle = p.SourcePath,
        skript = p.ScriptPath,
        musik = p.Project.MusicPath,
        szenen = p.Project.Scenes.Select(SceneView),
        zeichen_gesamt = p.Project.Scenes.Sum(s => s.Text.Length),
        fehlende_dateien = missing.Count > 0 ? missing : null,
    };

    private static object SceneView(Scene s) => new
    {
        nr = s.Number,
        herkunft = s.Origin,
        art = s.Kind == VisualKind.Video ? "clip" : "bild",
        datei = s.VisualPath,
        text = s.Text,
        pause = s.PauseAfter,
        standdauer_ohne_text = s.HasText ? (double?)null : s.SilentSeconds,
    };

    private static object JobView(DokuService.Job j, bool detailed) => new
    {
        auftrag = j.Id,
        projekt = j.ProjectId,
        zustand = j.State,
        prozent = j.Percent,
        meldung = j.Message,
        stimme = j.Voice,
        ziel = j.Output,
        laufzeit_s = Math.Round(((j.Ended ?? DateTimeOffset.Now) - j.Started).TotalSeconds, 1),
        fehler = j.Error,
        ergebnis = j.Result is { } r ? new { video = r.VideoPath, untertitel = r.SrtPath, dauer = r.Duration.ToString(@"mm\:ss") } : null,
        szenen = detailed
            ? j.Scenes.Values.OrderBy(s => s.Number).Select(s => new
            {
                nr = s.Number,
                stimme = s.Voice.ToString(),
                szene = s.Render.ToString(),
                fehler = s.Error,
            })
            : null,
    };

    private static string Resolve(string folder, string path)
    {
        path = path.Trim().Trim('"');
        return Path.IsPathRooted(path) ? Path.GetFullPath(path) : Path.GetFullPath(Path.Combine(folder, path));
    }
}
