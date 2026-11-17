using System.Net.Http.Json;

namespace VideoDoku.Core;

/// <summary>
/// Meldet den Fortschritt eines Renderlaufs zusaetzlich an den Agenten-Monitor (127.0.0.1:47811).
/// Nutzt dieselbe Ereignisform wie die Claude-Code-Hooks, damit der Monitor nichts Neues lernen muss:
/// je Szene eine Aufgabe (Prozentbalken) und je Schritt eine Aktion. Fehler werden still ignoriert;
/// laeuft der Monitor nicht, merkt man davon nichts.
/// </summary>
public sealed class MonitorReporter : IDisposable
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromMilliseconds(800) };
    private readonly string _session = "videodoku-" + Guid.NewGuid().ToString("N")[..12];
    private readonly string _cwd;
    private readonly Uri _url;

    public MonitorReporter(string outputFolder, int port = 47811)
    {
        _cwd = Path.Combine(outputFolder, "VideoDoku");
        _url = new Uri($"http://127.0.0.1:{port}/hook");
    }

    public Task StartAsync(string title, int scenes) =>
        SendAsync(new { hook_event_name = "UserPromptSubmit", session_title = $"Video: {title} ({scenes} Szenen)" });

    public Task SceneCreatedAsync(int number) =>
        SendAsync(new { hook_event_name = "TaskCreated", task_id = $"szene-{number}" });

    public Task SceneDoneAsync(int number) =>
        SendAsync(new { hook_event_name = "TaskCompleted", task_id = $"szene-{number}" });

    public Task StepStartAsync(string id, string step, string description) =>
        SendAsync(new { hook_event_name = "PreToolUse", tool_name = step, tool_use_id = id, tool_input = new { description } });

    public Task StepEndAsync(string id, string step, bool ok, string? error = null) =>
        ok ? SendAsync(new { hook_event_name = "PostToolUse", tool_name = step, tool_use_id = id })
           : SendAsync(new { hook_event_name = "PostToolUseFailure", tool_name = step, tool_use_id = id, error = error ?? "Fehler" });

    public Task FinishedAsync(string message) =>
        SendAsync(new { hook_event_name = "Stop", last_assistant_message = message });

    public Task FailedAsync(string error) =>
        SendAsync(new { hook_event_name = "StopFailure", error });

    public Task EndAsync() => SendAsync(new { hook_event_name = "SessionEnd", reason = "other" });

    private async Task SendAsync(object payload)
    {
        try
        {
            // Gemeinsame Felder ergaenzen, ohne fuer jedes Ereignis einen eigenen Typ zu brauchen.
            var dict = payload.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(payload));
            dict["session_id"] = _session;
            dict["cwd"] = _cwd;
            using var resp = await _http.PostAsJsonAsync(_url, dict);
        }
        catch (Exception)
        {
            // Monitor ist Komfort, kein Muss.
        }
    }

    public void Dispose() => _http.Dispose();
}
