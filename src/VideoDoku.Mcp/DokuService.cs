using System.Collections.Concurrent;
using System.Diagnostics;
using VideoDoku.Core;
using VideoDoku.Windows;

namespace VideoDoku.Mcp;

/// <summary>
/// Haelt geoeffnete Projekte und laufende Video-Auftraege, solange der MCP-Server laeuft
/// (also solange die Claude-Sitzung lebt). Ein Auftrag laeuft im Hintergrund; die KI fragt den
/// Stand mit videodoku_job ab oder wartet mit videodoku_wait.
/// </summary>
public sealed class DokuService
{
    public sealed class OpenProject
    {
        public required string Id { get; init; }
        public required DokuProject Project { get; init; }
        public required string SourcePath { get; init; }
        public string? ScriptPath { get; set; }
        /// <summary>Skript wurde vom Werkzeug selbst angelegt: Aenderungen werden automatisch hineingeschrieben.</summary>
        public bool OwnsScript { get; set; }
    }

    public sealed class Job
    {
        public required string Id { get; init; }
        public required string ProjectId { get; init; }
        public required string Output { get; init; }
        public required string Voice { get; init; }
        public DateTimeOffset Started { get; } = DateTimeOffset.Now;
        public DateTimeOffset? Ended { get; set; }
        public string State { get; set; } = "laeuft";
        public double Percent { get; set; }
        public string Message { get; set; } = "startet";
        public string? Error { get; set; }
        public DokuBuilder.Result? Result { get; set; }
        public ConcurrentDictionary<int, SceneProgress> Scenes { get; } = new();
        public CancellationTokenSource Cts { get; } = new();
        public Task? Task { get; set; }
    }

    private readonly ConcurrentDictionary<string, OpenProject> _projects = new();
    private readonly ConcurrentDictionary<string, Job> _jobs = new();
    private readonly WindowsVoiceProvider _windows = new();
    private int _projectCounter, _jobCounter;

    public Ffmpeg Ffmpeg { get; } = new(Ffmpeg.DefaultBinFolder);
    public AppSettings Settings { get; } = AppSettings.Load();

    // ------------------------------------------------------------------ Stimmen

    public IngokTtsClient? Service()
    {
        var key = AppSettings.LoadKey();
        return key is null ? null
            : new IngokTtsClient(new HttpClient { BaseAddress = new Uri(Settings.ServiceUrl), Timeout = TimeSpan.FromMinutes(5) }, key);
    }

    public sealed record VoiceList(IReadOnlyList<VoiceOption> Voices, string? ServiceError);

    public async Task<VoiceList> VoicesAsync(CancellationToken ct)
    {
        var list = new List<VoiceOption>();
        string? error = null;
        if (Service() is { } s)
        {
            try { list.AddRange(await s.VoicesAsync(ct)); }
            catch (Exception ex) { error = ex.Message; }
        }
        else
        {
            error = "Kein Schlüssel für den Sprachdienst gespeichert (in der Oberfläche unter „Schlüssel“ eintragen). Nur Windows-Stimmen verfügbar.";
        }
        list.AddRange(await _windows.VoicesAsync(ct));
        return new VoiceList(list, error);
    }

    /// <summary>Stimme nach Id oder Teil des Namens („horst“, „Karsten“). Ohne Angabe: Sprachdienst vor Windows.</summary>
    public async Task<(VoiceOption Voice, ITtsProvider Provider)> ResolveVoiceAsync(string? wanted, CancellationToken ct)
    {
        var voices = (await VoicesAsync(ct)).Voices;
        VoiceOption? v = null;
        if (!string.IsNullOrWhiteSpace(wanted))
        {
            v = voices.FirstOrDefault(x => x.Id.Equals(wanted, StringComparison.OrdinalIgnoreCase))
                ?? voices.FirstOrDefault(x => x.Label.Contains(wanted, StringComparison.OrdinalIgnoreCase));
            if (v is null)
                throw new InvalidOperationException($"Stimme „{wanted}“ nicht gefunden. Verfügbar: {string.Join(", ", voices.Select(x => x.Label))}");
        }
        v ??= voices.FirstOrDefault(x => x.Provider != _windows.Name)
              ?? voices.FirstOrDefault(x => x.Label.Contains("Karsten", StringComparison.OrdinalIgnoreCase))
              ?? voices.FirstOrDefault()
              ?? throw new InvalidOperationException("Keine Stimme verfügbar.");
        ITtsProvider provider = v.Provider == _windows.Name ? _windows : Service() ?? throw new InvalidOperationException("Sprachdienst ohne Schlüssel.");
        return (v, provider);
    }

    // ------------------------------------------------------------------ Projekte

    public async Task<OpenProject> OpenAsync(string path, CancellationToken ct)
    {
        path = Path.GetFullPath(path);
        if (!File.Exists(path)) throw new FileNotFoundException($"Datei nicht gefunden: {path}");
        var ext = Path.GetExtension(path).ToLowerInvariant();
        DokuProject project;
        string? script = null;
        if (ext is ".pptx" or ".ppt")
        {
            var slides = Path.Combine(WorkFolderFor(DefaultOutput(path)), "folien");
            project = await RunStaAsync(() => PowerPointImport.Import(path, slides, null, ct));
        }
        else
        {
            var r = ScriptParser.ParseFile(path);
            if (r.Project.Scenes.Count == 0) throw new InvalidOperationException(string.Join(" ", r.Errors));
            project = r.Project;
            script = path;
        }
        var open = new OpenProject { Id = $"p{Interlocked.Increment(ref _projectCounter)}", Project = project, SourcePath = path, ScriptPath = script };
        _projects[open.Id] = open;
        return open;
    }

    public OpenProject Add(DokuProject project, string sourcePath, string? scriptPath)
    {
        var open = new OpenProject { Id = $"p{Interlocked.Increment(ref _projectCounter)}", Project = project, SourcePath = sourcePath, ScriptPath = scriptPath };
        _projects[open.Id] = open;
        return open;
    }

    public OpenProject Get(string projectId) =>
        _projects.TryGetValue(projectId, out var p) ? p
            : throw new InvalidOperationException($"Projekt „{projectId}“ ist nicht geöffnet. Erst videodoku_open oder videodoku_create_project aufrufen.");

    public IEnumerable<OpenProject> Projects => _projects.Values.OrderBy(p => p.Id);

    public static string DefaultOutput(string source) =>
        Path.Combine(Path.GetDirectoryName(source) ?? "", Path.GetFileNameWithoutExtension(source) + ".mp4");

    public static string WorkFolderFor(string output) =>
        Path.Combine(Path.GetDirectoryName(output) ?? "", Path.GetFileNameWithoutExtension(output) + "_arbeit");

    // ------------------------------------------------------------------ Auftraege

    public async Task<Job> StartBuildAsync(OpenProject open, string? voice, string? output, RenderOptions options, CancellationToken ct)
    {
        if (!Ffmpeg.IsAvailable) throw new InvalidOperationException($"ffmpeg fehlt unter {Ffmpeg.DefaultBinFolder}.");
        if (_jobs.Values.Any(j => j.State == "laeuft"))
            throw new InvalidOperationException("Es läuft schon ein Video-Auftrag. Erst warten (videodoku_wait) oder abbrechen (videodoku_cancel).");

        var (v, provider) = await ResolveVoiceAsync(voice, ct);
        var outPath = Path.GetFullPath(string.IsNullOrWhiteSpace(output) ? DefaultOutput(open.SourcePath) : output);
        if (!outPath.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)) outPath += ".mp4";

        var job = new Job { Id = $"j{Interlocked.Increment(ref _jobCounter)}", ProjectId = open.Id, Output = outPath, Voice = v.Label };
        _jobs[job.Id] = job;
        var progress = new InlineProgress<BuildProgress>(p =>
        {
            job.Percent = Math.Round(p.Overall * 100, 1);
            job.Message = p.Message;
            if (p.Scene is { } sp) job.Scenes[sp.Number] = sp;
        });

        job.Task = Task.Run(async () =>
        {
            using var monitor = new MonitorReporter(Path.GetDirectoryName(outPath) ?? "");
            try
            {
                var builder = new DokuBuilder(Ffmpeg, provider);
                job.Result = await builder.BuildAsync(open.Project, v.Id, Settings.Language, outPath, WorkFolderFor(outPath),
                    options, progress, monitor, job.Cts.Token);
                job.State = "fertig";
                job.Percent = 100;
            }
            catch (OperationCanceledException)
            {
                job.State = "abgebrochen";
            }
            catch (Exception ex)
            {
                job.State = "fehler";
                job.Error = ex.Message;
            }
            finally
            {
                job.Ended = DateTimeOffset.Now;
            }
        });
        return job;
    }

    public Job GetJob(string jobId) =>
        _jobs.TryGetValue(jobId, out var j) ? j : throw new InvalidOperationException($"Auftrag „{jobId}“ unbekannt.");

    public IEnumerable<Job> Jobs => _jobs.Values.OrderBy(j => j.Started);

    // ------------------------------------------------------------------ Hilfen

    public static void OpenInUi(string? path)
    {
        var exe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VideoDoku", "app", "VideoDoku.exe");
        if (!File.Exists(exe)) throw new FileNotFoundException($"Oberfläche nicht installiert: {exe}");
        var psi = new ProcessStartInfo(exe) { UseShellExecute = true };
        if (path is not null) psi.ArgumentList.Add(path);
        Process.Start(psi);
    }

    public static Task<T> RunStaAsync<T>(Func<T> work)
    {
        var tcs = new TaskCompletionSource<T>();
        var t = new Thread(() =>
        {
            try { tcs.SetResult(work()); }
            catch (Exception ex) { tcs.SetException(ex); }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.IsBackground = true;
        t.Start();
        return tcs.Task;
    }

    /// <summary>Meldet sofort im aufrufenden Thread (ohne SynchronizationContext).</summary>
    private sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}
