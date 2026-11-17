using System.Text.Json;

namespace AgentMonitor.Core;

/// <summary>Ein Verbrauchsfenster des Abos (5 Stunden oder 7 Tage), wie /usage es zeigt.</summary>
public sealed record UsageWindow(double UsedPercent, DateTimeOffset? ResetsAt);

/// <summary>
/// Was die Statuszeile von Claude Code liefert (dokumentiert unter code.claude.com/docs/en/statusline):
/// Modell, Kontextfuellung, Kosten und – nur bei Pro/Max-Abos und erst nach der ersten Antwort –
/// die Planfenster rate_limits.five_hour / seven_day mit used_percentage und resets_at (Unix-Sekunden).
/// </summary>
public sealed record StatusInfo
{
    public required string SessionId { get; init; }
    public string? ModelId { get; init; }
    public string? ModelName { get; init; }
    public double? ContextPercent { get; init; }
    public double? CostUsd { get; init; }
    public UsageWindow? FiveHour { get; init; }
    public UsageWindow? SevenDay { get; init; }
    public string? Cwd { get; init; }

    public static StatusInfo? Parse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var r = doc.RootElement;
            if (r.ValueKind != JsonValueKind.Object) return null;
            var session = Str(r, "session_id");
            if (string.IsNullOrWhiteSpace(session)) return null;

            string? modelId = null, modelName = null;
            if (r.TryGetProperty("model", out var m))
            {
                if (m.ValueKind == JsonValueKind.Object)
                {
                    modelId = Str(m, "id");
                    modelName = Str(m, "display_name");
                }
                else if (m.ValueKind == JsonValueKind.String) modelId = m.GetString();
            }

            double? ctx = null;
            if (r.TryGetProperty("context_window", out var cw) && cw.ValueKind == JsonValueKind.Object)
                ctx = Num(cw, "used_percentage");

            double? cost = null;
            if (r.TryGetProperty("cost", out var c) && c.ValueKind == JsonValueKind.Object)
                cost = Num(c, "total_cost_usd");

            UsageWindow? five = null, week = null;
            if (r.TryGetProperty("rate_limits", out var rl) && rl.ValueKind == JsonValueKind.Object)
            {
                five = Window(rl, "five_hour");
                week = Window(rl, "seven_day");
            }

            var cwd = Str(r, "cwd");
            if (cwd is null && r.TryGetProperty("workspace", out var ws) && ws.ValueKind == JsonValueKind.Object)
                cwd = Str(ws, "current_dir");

            return new StatusInfo
            {
                SessionId = session,
                ModelId = modelId,
                ModelName = modelName,
                ContextPercent = ctx,
                CostUsd = cost,
                FiveHour = five,
                SevenDay = week,
                Cwd = cwd,
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Kurzer Modellname fuer die Anzeige: „Fable 5.2“, „Opus 5.5“ …</summary>
    public static string? ShortModel(string? id, string? displayName)
    {
        if (!string.IsNullOrWhiteSpace(displayName)) return displayName.Replace("Claude ", "", StringComparison.OrdinalIgnoreCase).Trim();
        if (string.IsNullOrWhiteSpace(id)) return null;
        // claude-opus-5-5 → Opus 5.5
        var parts = id.Replace("claude-", "", StringComparison.OrdinalIgnoreCase).Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return id;
        var name = char.ToUpperInvariant(parts[0][0]) + parts[0][1..];
        var ver = string.Join('.', parts.Skip(1).TakeWhile(p => p.All(char.IsDigit) && p.Length <= 2));
        return ver.Length > 0 ? $"{name} {ver}" : name;
    }

    /// <summary>Modellfamilie in Kleinbuchstaben (fable, opus, sonnet, haiku), fuer Farben.</summary>
    public static string? Family(string? id, string? displayName)
    {
        var s = (id ?? displayName ?? "").ToLowerInvariant();
        foreach (var f in new[] { "fable", "opus", "sonnet", "haiku" })
            if (s.Contains(f, StringComparison.Ordinal)) return f;
        return null;
    }

    private static UsageWindow? Window(JsonElement rl, string name)
    {
        if (!rl.TryGetProperty(name, out var w) || w.ValueKind != JsonValueKind.Object) return null;
        var used = Num(w, "used_percentage");
        if (used is null) return null;
        DateTimeOffset? reset = null;
        if (Num(w, "resets_at") is { } epoch && epoch > 0)
            reset = DateTimeOffset.FromUnixTimeSeconds((long)epoch).ToLocalTime();
        return new UsageWindow(Math.Clamp(used.Value, 0, 100), reset);
    }

    private static string? Str(JsonElement e, string n) =>
        e.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static double? Num(JsonElement e, string n) =>
        e.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var d) ? d : null;
}
