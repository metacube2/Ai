using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VideoDoku.Core;

/// <summary>Eine Stimme, die das Werkzeug anbieten kann.</summary>
public sealed record VoiceOption(string Provider, string Id, string Label)
{
    public override string ToString() => Label;
}

/// <summary>Quelle fuer gesprochenen Text. Liefert eine Audiodatei (MP3 oder WAV) als Bytes.</summary>
public interface ITtsProvider
{
    string Name { get; }
    /// <summary>Dateiendung der gelieferten Audiodaten, z. B. „.mp3“.</summary>
    string Extension { get; }
    Task<IReadOnlyList<VoiceOption>> VoicesAsync(CancellationToken ct);
    Task<byte[]> SpeakAsync(string text, string voiceId, string language, CancellationToken ct);
}

/// <summary>
/// Client fuer den Sprachdienst auf tts.ingok.ch (Qwen3-TTS auf dem Mac).
/// Schnittstelle laut Plan vom 17.11.2026:
/// GET  /voices                    → Stimmen, die fuer diesen Schluessel freigegeben sind
/// POST /speak {text,voice,language,format} → 200 mit Audio, oder 202 mit Job-ID bei langen Texten
/// GET  /jobs/{id}                 → Status;  GET /jobs/{id}/audio → Audio
/// Authentifizierung: Authorization: Bearer &lt;Schluessel&gt;.
/// </summary>
public sealed class IngokTtsClient : ITtsProvider
{
    public const string DefaultBaseUrl = "https://tts.ingok.ch/api/tts/v1/";

    private readonly HttpClient _http;
    private readonly TimeSpan _pollInterval;
    private readonly TimeSpan _jobTimeout;

    public string Name => "Sprachdienst tts.ingok.ch";
    public string Extension => ".mp3";

    public IngokTtsClient(HttpClient http, string apiKey, TimeSpan? pollInterval = null, TimeSpan? jobTimeout = null)
    {
        _http = http;
        _http.BaseAddress ??= new Uri(DefaultBaseUrl);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        _pollInterval = pollInterval ?? TimeSpan.FromSeconds(2);
        _jobTimeout = jobTimeout ?? TimeSpan.FromMinutes(15);
    }

    public async Task<IReadOnlyList<VoiceOption>> VoicesAsync(CancellationToken ct)
    {
        using var resp = await _http.GetAsync("voices", ct);
        await EnsureOk(resp, ct);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));

        // Toleriert sowohl ["horst", …] als auch {"voices":[{"id":"horst","name":"Horst"}, …]}.
        var root = doc.RootElement;
        var arr = root.ValueKind == JsonValueKind.Array ? root
            : root.TryGetProperty("voices", out var v) ? v : default;
        var list = new List<VoiceOption>();
        if (arr.ValueKind != JsonValueKind.Array) return list;
        foreach (var e in arr.EnumerateArray())
        {
            if (e.ValueKind == JsonValueKind.String) list.Add(new(Name, e.GetString()!, $"{e.GetString()} (Sprachdienst)"));
            else if (e.ValueKind == JsonValueKind.Object && e.TryGetProperty("id", out var id) && id.GetString() is { } sid)
            {
                var label = e.TryGetProperty("name", out var n) && n.GetString() is { } sn ? sn : sid;
                list.Add(new(Name, sid, $"{label} (Sprachdienst)"));
            }
        }
        return list;
    }

    public async Task<byte[]> SpeakAsync(string text, string voiceId, string language, CancellationToken ct)
    {
        var body = new SpeakRequest(text, voiceId, language, "mp3");
        using var resp = await _http.PostAsJsonAsync("speak", body, ct);

        if (resp.StatusCode == HttpStatusCode.Accepted)
        {
            var job = await resp.Content.ReadFromJsonAsync<JobInfo>(ct)
                      ?? throw new InvalidOperationException("Sprachdienst: Antwort 202 ohne Job-ID.");
            return await WaitForJobAsync(job.Id ?? job.JobId ?? throw new InvalidOperationException("Sprachdienst: Job-ID fehlt."), ct);
        }

        await EnsureOk(resp, ct);
        return await resp.Content.ReadAsByteArrayAsync(ct);
    }

    private async Task<byte[]> WaitForJobAsync(string id, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow + _jobTimeout;
        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(_pollInterval, ct);
            using var st = await _http.GetAsync($"jobs/{Uri.EscapeDataString(id)}", ct);
            await EnsureOk(st, ct);
            var info = await st.Content.ReadFromJsonAsync<JobInfo>(ct);
            var status = info?.Status?.ToLowerInvariant();
            if (status is "done" or "finished" or "completed" or "fertig")
            {
                using var audio = await _http.GetAsync($"jobs/{Uri.EscapeDataString(id)}/audio", ct);
                await EnsureOk(audio, ct);
                return await audio.Content.ReadAsByteArrayAsync(ct);
            }
            if (status is "failed" or "error" or "fehler")
                throw new InvalidOperationException($"Sprachdienst: Auftrag fehlgeschlagen ({info?.Error ?? "ohne Angabe"}).");
        }
        throw new TimeoutException("Sprachdienst: Auftrag wurde nicht rechtzeitig fertig.");
    }

    private static async Task EnsureOk(HttpResponseMessage resp, CancellationToken ct)
    {
        if (resp.IsSuccessStatusCode) return;
        var detail = "";
        try { detail = (await resp.Content.ReadAsStringAsync(ct)).Trim(); } catch (Exception) { }
        if (detail.Length > 200) detail = detail[..200];
        var msg = resp.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Schlüssel fehlt oder ist ungültig (401).",
            HttpStatusCode.Forbidden => "Diese Stimme ist für den Schlüssel nicht freigegeben (403).",
            HttpStatusCode.RequestEntityTooLarge => "Text zu lang für eine Anfrage (413).",
            (HttpStatusCode)429 => "Zu viele Anfragen, bitte kurz warten (429).",
            HttpStatusCode.NotFound => "Schnittstelle nicht gefunden (404). Ist der Sprachdienst schon eingerichtet?",
            _ => $"HTTP {(int)resp.StatusCode}.",
        };
        throw new HttpRequestException($"Sprachdienst: {msg} {detail}".Trim(), null, resp.StatusCode);
    }

    private sealed record SpeakRequest(
        [property: JsonPropertyName("text")] string Text,
        [property: JsonPropertyName("voice")] string Voice,
        [property: JsonPropertyName("language")] string Language,
        [property: JsonPropertyName("format")] string Format);

    private sealed class JobInfo
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("job_id")] public string? JobId { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("error")] public string? Error { get; set; }
    }
}
