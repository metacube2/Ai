using System.Text.Json;

namespace AgentMonitor.Core;

/// <summary>
/// Macht aus einem Werkzeugaufruf eine verstaendliche deutsche Beschreibung („liest X“,
/// „sucht „Y“ in Z“) und eine zweite, kleinere Detailzeile (z. B. der eigentliche Befehl).
/// Alles bleibt lokal; trotzdem wird gekuerzt, damit lange Befehle oder Texte die Anzeige nicht sprengen.
/// </summary>
public static class ToolSummary
{
    public const int MaxLength = 90;
    public const int MaxDetailLength = 140;

    public static string Describe(string? toolName, JsonElement? input)
    {
        var tool = toolName ?? "?";
        var i = input is { ValueKind: JsonValueKind.Object } o ? o : default;
        var has = i.ValueKind == JsonValueKind.Object;
        string? S(string n) => has ? First(i, n) : null;

        string s = tool switch
        {
            "Bash" or "PowerShell" => S("description") ?? Verb("führt aus:", S("command")),
            "Read" => Verb("liest", FileName(S("file_path"))),
            "Write" => Verb("schreibt", FileName(S("file_path"))),
            "Edit" => Verb("ändert", FileName(S("file_path"))),
            "NotebookEdit" => Verb("ändert Notebook", FileName(S("notebook_path"))),
            "Glob" => Verb("sucht Dateien", S("pattern")),
            "Grep" => S("pattern") is { } p ? $"sucht „{p}“" + (FileName(S("path")) is { } where ? $" in {where}" : "") : "durchsucht Dateien",
            "WebFetch" => Verb("lädt Webseite", Host(S("url"))),
            "WebSearch" => Verb("sucht im Web:", S("query")),
            "Agent" or "Task" => Verb($"startet Unteragent {S("subagent_type") ?? ""}".TrimEnd(), S("description") is { } d ? $"– {d}" : null),
            "Skill" => Verb("nutzt Skill", S("skill")),
            "SendMessage" => $"Nachricht an {S("to") ?? "?"}" + (S("summary") is { } sm ? $": {sm}" : ""),
            "TodoWrite" => TodoProgress(input) is { } tp ? $"aktualisiert Aufgabenliste ({tp.Done}/{tp.Total} erledigt)" : "aktualisiert Aufgabenliste",
            "TaskCreate" => Verb("legt Aufgabe an:", S("subject") ?? S("title")),
            "TaskUpdate" => TaskUpdateText(S("taskId") ?? S("task_id"), S("status")),
            "TaskList" or "TaskGet" => "liest Aufgabenliste",
            "Workflow" => Verb("startet Workflow", S("name")),
            "Artifact" => Verb("Artefakt:", S("action") ?? "veröffentlichen"),
            "AskUserQuestion" => "stellt dir eine Frage",
            "ToolSearch" => "lädt Werkzeuge",
            "ListAgents" => "listet Sitzungen",
            "advisor" => "fragt den Berater",
            "EnterPlanMode" or "ExitPlanMode" => "Planmodus",
            _ when tool.StartsWith("mcp__", StringComparison.Ordinal) => McpText(tool, has ? i : null),
            _ => S("description") ?? S("query") ?? S("name") ?? tool,
        };

        return Shorten(OneLine(s));
    }

    /// <summary>Zweite Zeile: das „Wie“ zur Beschreibung, z. B. der genaue Befehl oder der Ordner.</summary>
    public static string? Detail(string? toolName, JsonElement? input)
    {
        if (input is not { ValueKind: JsonValueKind.Object } i) return null;
        string? d = toolName switch
        {
            "Bash" or "PowerShell" => First(i, "description") is null ? null : First(i, "command"),
            "Read" or "Write" or "Edit" => Folder(First(i, "file_path")),
            "Grep" => First(i, "glob") is { } g ? $"Dateien: {g}" : null,
            "Glob" => Folder(First(i, "path")),
            "WebFetch" => First(i, "url") is { } u && Uri.TryCreate(u, UriKind.Absolute, out var uri) ? uri.AbsolutePath : null,
            _ => null,
        };
        return string.IsNullOrWhiteSpace(d) ? null : Shorten(OneLine(d), MaxDetailLength);
    }

    /// <summary>Hoechstlaenge des vollstaendigen Textes (Klick auf eine Aktion, Kopieren).</summary>
    public const int MaxFullLength = 4000;

    /// <summary>
    /// Vollstaendiger Text einer Aktion fuer die Detailansicht und zum Kopieren: der ganze Befehl, der
    /// ganze Pfad, das Suchmuster oder der Auftrag. Nur im Speicher, wird nie gespeichert.
    /// </summary>
    public static string? FullText(string? toolName, JsonElement? input)
    {
        if (input is not { ValueKind: JsonValueKind.Object } i) return null;
        string? Join(params string?[] parts)
        {
            var p = parts.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            return p.Length == 0 ? null : string.Join("\n", p);
        }
        var t = toolName switch
        {
            "Bash" or "PowerShell" => First(i, "command"),
            "Read" or "Write" or "Edit" or "NotebookEdit" => First(i, "file_path") ?? First(i, "notebook_path"),
            "Grep" => Join(First(i, "pattern"), First(i, "path") is { } p ? $"in {p}" : null, First(i, "glob") is { } g ? $"Dateien: {g}" : null),
            "Glob" => Join(First(i, "pattern"), First(i, "path") is { } p2 ? $"in {p2}" : null),
            "WebFetch" => First(i, "url"),
            "WebSearch" => First(i, "query"),
            "Agent" or "Task" => Join(First(i, "description"), First(i, "prompt")),
            _ => i.GetRawText(),
        };
        if (string.IsNullOrWhiteSpace(t)) return null;
        t = t.Replace("\r\n", "\n").Trim();
        return t.Length <= MaxFullLength ? t : t[..(MaxFullLength - 1)] + "…";
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

    /// <summary>Erste aussagekraeftige Zeile eines laengeren Textes (z. B. letzte Antwort des Agenten).</summary>
    public static string? FirstLine(string? text, int max = MaxLength)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim().TrimStart('#', '*', '-', '>', ' ').Trim();
            if (line.Length > 0) return Shorten(line, max);
        }
        return null;
    }

    public static string Shorten(string s, int max = MaxLength) =>
        s.Length <= max ? s : s[..(max - 1)].TrimEnd() + "…";

    private static string Verb(string verb, string? obj) => obj is null ? verb : $"{verb} {obj}";

    private static string TaskUpdateText(string? id, string? status) => status switch
    {
        "completed" => $"Aufgabe {id} erledigt",
        "in_progress" => $"beginnt Aufgabe {id}",
        "deleted" => $"löscht Aufgabe {id}",
        null => $"ändert Aufgabe {id}",
        _ => $"Aufgabe {id}: {status}",
    };

    private static string McpText(string tool, JsonElement? input)
    {
        var parts = tool.Split("__", StringSplitOptions.RemoveEmptyEntries);
        var server = parts.Length >= 2 ? parts[1] : tool;
        var action = parts.Length >= 3 ? parts[^1] : "";
        var friendlyServer = server switch
        {
            "claude-in-chrome" => "Browser",
            var x when x.Contains("Gmail", StringComparison.OrdinalIgnoreCase) => "Gmail",
            var x when x.Contains("Calendar", StringComparison.OrdinalIgnoreCase) => "Kalender",
            var x when x.Contains("Docs", StringComparison.OrdinalIgnoreCase) => "Docs",
            _ => server,
        };
        string? target = null;
        if (input is { ValueKind: JsonValueKind.Object } i)
            target = Host(First(i, "url")) ?? First(i, "query") ?? First(i, "text");
        return target is null ? $"{friendlyServer}: {action}" : $"{friendlyServer}: {action} {target}";
    }

    private static string? First(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(v.GetString())
            ? v.GetString()
            : null;

    private static string? FileName(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        var p = path.Replace('\\', '/').TrimEnd('/');
        var idx = p.LastIndexOf('/');
        return idx >= 0 ? p[(idx + 1)..] : p;
    }

    /// <summary>Die letzten zwei Ordner eines Pfads, z. B. „src/AgentMonitor.Core“.</summary>
    private static string? Folder(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        var parts = path.Replace('\\', '/').TrimEnd('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length <= 1) return null;
        var dirs = parts[..^1];
        return string.Join('/', dirs[Math.Max(0, dirs.Length - 2)..]);
    }

    private static string? Host(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var u) ? u.Host : null;

    private static string OneLine(string s)
    {
        var line = s.ReplaceLineEndings(" ").Trim();
        while (line.Contains("  ", StringComparison.Ordinal)) line = line.Replace("  ", " ", StringComparison.Ordinal);
        return line;
    }
}
