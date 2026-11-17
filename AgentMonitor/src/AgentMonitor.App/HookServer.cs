using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AgentMonitor.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AgentMonitor.App;

/// <summary>
/// Nimmt die HTTP-Hooks von Claude Code auf 127.0.0.1 entgegen. Antwortet immer sofort mit
/// 200 und leerem Inhalt: Ein Hook-Ergebnis mit Inhalt koennte Claude beeinflussen
/// (z. B. Zusatzkontext bei UserPromptSubmit), das soll der Monitor nie tun.
/// </summary>
public sealed class HookServer : IAsyncDisposable
{
    private readonly MonitorState _state;
    private readonly MonitorSettings _settings;
    private WebApplication? _app;

    public DateTimeOffset? LastEvent { get; private set; }
    public long EventCount { get; private set; }
    public string? StartError { get; private set; }

    public HookServer(MonitorState state, MonitorSettings settings)
    {
        _state = state;
        _settings = settings;
    }

    public async Task StartAsync()
    {
        try
        {
            var builder = WebApplication.CreateSlimBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.ConfigureKestrel(o =>
            {
                o.Listen(IPAddress.Loopback, _settings.Port);
                o.Limits.MaxRequestBodySize = 4 * 1024 * 1024;
            });

            var app = builder.Build();
            app.MapPost("/hook", HandleAsync);
            app.MapPost("/status", HandleStatusAsync);
            app.MapGet("/health", () => Results.Text("ok"));
            await app.StartAsync();
            _app = app;
        }
        catch (Exception ex)
        {
            StartError = ex.Message;
        }
    }

    private async Task<IResult> HandleAsync(HttpRequest request)
    {
        try
        {
            using var reader = new StreamReader(request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();
            if (_settings.DebugLog) WriteDebug(body);

            var ev = HookEvent.Parse(body);
            if (ev is not null)
            {
                _state.Apply(ev, DateTimeOffset.Now);
                LastEvent = DateTimeOffset.Now;
                EventCount++;
            }
        }
        catch (Exception)
        {
            // Nie einen Fehler an Claude Code zurueckgeben.
        }
        return Results.StatusCode(StatusCodes.Status200OK);
    }

    /// <summary>Meldungen des Statuszeilen-Programms: Modell und Planverbrauch.</summary>
    private async Task<IResult> HandleStatusAsync(HttpRequest request)
    {
        try
        {
            using var reader = new StreamReader(request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();
            if (_settings.DebugLog) WriteDebug(body);
            if (StatusInfo.Parse(body) is { } st)
            {
                _state.ApplyStatus(st, DateTimeOffset.Now);
                LastStatus = DateTimeOffset.Now;
            }
        }
        catch (Exception)
        {
        }
        return Results.StatusCode(StatusCodes.Status200OK);
    }

    public DateTimeOffset? LastStatus { get; private set; }

    private static void WriteDebug(string body)
    {
        try
        {
            Directory.CreateDirectory(MonitorSettings.Folder);
            var line = JsonSerializer.Serialize(new { t = DateTimeOffset.Now, body });
            File.AppendAllText(Path.Combine(MonitorSettings.Folder, "debug-events.jsonl"), line + Environment.NewLine);
        }
        catch (Exception)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            try { await _app.StopAsync(); } catch (Exception) { }
            await _app.DisposeAsync();
        }
    }
}
