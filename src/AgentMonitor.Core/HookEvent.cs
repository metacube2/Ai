using System.Text.Json;

namespace AgentMonitor.Core;

/// <summary>
/// Ein Ereignis, wie es ein Claude-Code-Hook (type "http") als JSON schickt.
/// Es werden nur die Felder gelesen, die der Monitor braucht; unbekannte Felder werden ignoriert.
/// </summary>
public sealed record HookEvent
{
    public required string Event { get; init; }
    public required string SessionId { get; init; }
    public string? AgentId { get; init; }
    public string? AgentType { get; init; }
    public string? ToolName { get; init; }
    public string? ToolUseId { get; init; }
    public JsonElement? ToolInput { get; init; }
    public string? Cwd { get; init; }
    public string? SessionTitle { get; init; }
    public string? Error { get; init; }
    public bool IsInterrupt { get; init; }
    public long? DurationMs { get; init; }
    public string? NotificationType { get; init; }
    public string? Message { get; init; }
    public string? TaskId { get; init; }
    public string? TaskSubject { get; init; }
    public string? Source { get; init; }

    /// <summary>Liest ein Hook-Ereignis. Gibt null zurueck, wenn Pflichtfelder fehlen.</summary>
    public static HookEvent? Parse(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object) return null;
        var ev = Str(root, "hook_event_name");
        var session = Str(root, "session_id");
        if (string.IsNullOrWhiteSpace(ev) || string.IsNullOrWhiteSpace(session)) return null;

        JsonElement? toolInput = root.TryGetProperty("tool_input", out var ti) ? ti.Clone() : null;

        return new HookEvent
        {
            Event = ev,
            SessionId = session,
            AgentId = NullIfEmpty(Str(root, "agent_id")),
            AgentType = NullIfEmpty(Str(root, "agent_type")),
            ToolName = Str(root, "tool_name"),
            ToolUseId = Str(root, "tool_use_id"),
            ToolInput = toolInput,
            Cwd = Str(root, "cwd"),
            SessionTitle = NullIfEmpty(Str(root, "session_title")),
            Error = Str(root, "error"),
            IsInterrupt = root.TryGetProperty("is_interrupt", out var ii) && ii.ValueKind == JsonValueKind.True,
            DurationMs = root.TryGetProperty("duration_ms", out var d) && d.TryGetInt64(out var ms) ? ms : null,
            NotificationType = Str(root, "notification_type"),
            Message = Str(root, "message"),
            TaskId = Str(root, "task_id") ?? Str(root, "taskId"),
            TaskSubject = Str(root, "task_subject") ?? Str(root, "subject") ?? Str(root, "title"),
            Source = Str(root, "source"),
        };
    }

    public static HookEvent? Parse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return Parse(doc.RootElement);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) ? v.ValueKind switch
        {
            JsonValueKind.String => v.GetString(),
            JsonValueKind.Number => v.GetRawText(),
            _ => null,
        } : null;

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;
}
