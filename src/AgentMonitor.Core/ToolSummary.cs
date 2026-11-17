using System.Text.Json;

namespace AgentMonitor.Core;

/// <summary>
/// Macht aus einem Werkzeugaufruf eine kurze, gefahrlose Beschreibung fuer die Anzeige.
/// Grundsatz: nur so viel zeigen, wie zum Erkennen noetig ist. Dateipfade werden auf den
/// Dateinamen gekuerzt, URLs auf den Host, Befehle und Muster auf eine feste Laenge.
/// </summary>
public static class ToolSummary
{
    public const int MaxLength = 70;

    public static string Describe(string? toolName, JsonElement? input)
    {
        var tool = toolName ?? "?";
        if (input is not { ValueKind: JsonValueKind.Object } i) return "";

        string? s = tool switch
        {
            "Bash" or "PowerShell" => First(i, "description") ?? First(i, "command"),
            "Read" or "Write" or "Edit" or "NotebookEdit" => FileName(First(i, "file_path") ?? First(i, "notebook_path")),
            "Glob" => First(i, "pattern"),
            "Grep" => First(i, "pattern"),
            "WebFetch" => Host(First(i, "url")),
            "WebSearch" => First(i, "query"),
            "Agent" or "Task" => Join(First(i, "subagent_type"), First(i, "description")),
            "Skill" => First(i, "skill"),
            "SendMessage" => Join("an " + First(i, "to"), First(i, "summary")),
            "TodoWrite" => TodoText(i),
            "TaskCreate" => First(i, "subject") ?? First(i, "title"),
            "TaskUpdate" => Join(First(i, "taskId") ?? First(i, "task_id"), First(i, "status")),
            "Workflow" => First(i, "name") ?? "Workflow-Skript",
            "Artifact" => First(i, "action") ?? "publish",
            _ when tool.StartsWith("mcp__", StringComparison.Ordinal) => McpName(tool),
            _ => First(i, "description") ?? First(i, "query") ?? First(i, "name"),
        };

        return Shorten(OneLine(s ?? ""));
    }

    /// <summary>Kurzer Anzeigename eines Werkzeugs, bei MCP ohne Praefix.</summary>
    public static string DisplayName(string? toolName)
    {
        if (string.IsNullOrEmpty(toolName)) return "?";
        if (!toolName.StartsWith("mcp__", StringComparison.Ordinal)) return toolName;
        var parts = toolName.Split("__", StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 3 ? parts[^1] : toolName;
    }

    /// <summary>Fortschritt aus einer TodoWrite-Liste: (erledigt, gesamt), oder null.</summary>
    public static (int Done, int Total)? TodoProgress(JsonElement? input)
    {
        if (input is not { ValueKind: JsonValueKind.Object } i) return null;
        if (!i.TryGetProperty("todos", out var todos) || todos.ValueKind != JsonValueKind.Array) return null;
        int total = 0, done = 0;
        foreach (var t in todos.EnumerateArray())
        {
            total++;
            if (t.TryGetProperty("status", out var st) && st.ValueKind == JsonValueKind.String &&
                st.GetString() == "completed") done++;
        }
        return total == 0 ? null : (done, total);
    }

    public static string Shorten(string s, int max = MaxLength) =>
        s.Length <= max ? s : s[..(max - 1)].TrimEnd() + "…";

    private static string TodoText(JsonElement i) =>
        TodoProgress(i) is { } p ? $"Aufgabenliste {p.Done}/{p.Total}" : "Aufgabenliste";

    private static string? First(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(v.GetString())
            ? v.GetString()
            : null;

    private static string? Join(string? a, string? b) =>
        (a, b) switch
        {
            (null, null) => null,
            (null, _) => b,
            (_, null) => a,
            _ => $"{a}: {b}",
        };

    private static string? FileName(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        var p = path.Replace('\\', '/').TrimEnd('/');
        var idx = p.LastIndexOf('/');
        return idx >= 0 ? p[(idx + 1)..] : p;
    }

    private static string? Host(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var u) ? u.Host : null;

    private static string McpName(string tool)
    {
        var parts = tool.Split("__", StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 ? parts[1] : tool;
    }

    private static string OneLine(string s)
    {
        var line = s.ReplaceLineEndings(" ").Trim();
        while (line.Contains("  ", StringComparison.Ordinal)) line = line.Replace("  ", " ", StringComparison.Ordinal);
        return line;
    }
}
