using System.Text.Json;

namespace AgentMonitor.Core;

/// <summary>
/// Lernt, wie lange gleichartige Aktionen dauern, und schaetzt daraus die Dauer einer laufenden Aktion.
/// Claude Code meldet je Werkzeug keinen Fortschritt; der Prozentbalken einer Aktion ist deshalb
/// immer eine Schaetzung und wird in der Anzeige mit „≈“ gekennzeichnet.
/// Gemessen wird die Zeit vom Anfordern bis zum Ende (inklusive Sicherheitspruefung), weil genau
/// diese Zeit der Balken abbildet.
/// </summary>
public sealed class DurationEstimator
{
    /// <summary>So viele Messungen je Art werden behalten (die neuesten).</summary>
    public const int SamplesPerKey = 25;

    private readonly object _lock = new();
    private readonly Dictionary<string, List<double>> _samples = new(StringComparer.OrdinalIgnoreCase);

    public bool IsDirty { get; private set; }

    /// <summary>
    /// Art einer Aktion. Bei Befehlen zaehlen die ersten zwei Woerter („dotnet test“, „git push“),
    /// weil derselbe Befehl aehnlich lange dauert, verschiedene Befehle aber nicht.
    /// </summary>
    public static string Key(string? toolName, JsonElement? input)
    {
        var tool = toolName ?? "?";
        if (input is { ValueKind: JsonValueKind.Object } i)
        {
            if (tool is "Bash" or "PowerShell" && i.TryGetProperty("command", out var c) && c.ValueKind == JsonValueKind.String)
            {
                // Erster Teilbefehl, der nicht nur das Verzeichnis wechselt oder eine Variable setzt.
                var segments = (c.GetString() ?? "").Split(["&&", "||", ";", "|", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var segment = segments.FirstOrDefault(s => !IsSetup(s)) ?? segments.FirstOrDefault() ?? "";
                var words = segment.Split([' ', '\t', '\r'], StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => !w.Contains('=') && !w.StartsWith('&'))
                    .Take(2)
                    .Select(w => Path.GetFileName(w.Trim('"', '\'')));
                return $"{tool}:{string.Join(' ', words)}";
            }
            if (tool is "Agent" or "Task" && i.TryGetProperty("subagent_type", out var t) && t.ValueKind == JsonValueKind.String)
                return $"{tool}:{t.GetString()}";
        }
        return tool;
    }

    private static bool IsSetup(string segment)
    {
        var words = segment.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return true;
        // „cd …“ oder reine Zuweisungen („$a = 1“, „FOO=1“); „FOO=1 dotnet build“ ist dagegen ein Befehl.
        return words[0] is "cd" or "Set-Location" or "export" or "set" || words[0].StartsWith('$') ||
               words.All(w => w.Contains('=') || w == "=");
    }

    /// <summary>Geschaetzte Dauer in Sekunden (Median der letzten Messungen), oder null, wenn unbekannt.</summary>
    public double? Estimate(string key)
    {
        lock (_lock)
        {
            if (!_samples.TryGetValue(key, out var list) || list.Count == 0) return null;
            var sorted = list.OrderBy(x => x).ToArray();
            var mid = sorted.Length / 2;
            return sorted.Length % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
        }
    }

    public void Record(string key, double seconds)
    {
        if (double.IsNaN(seconds) || seconds < 0 || seconds > 6 * 3600) return;
        lock (_lock)
        {
            if (!_samples.TryGetValue(key, out var list)) _samples[key] = list = new();
            list.Add(Math.Round(seconds, 2));
            if (list.Count > SamplesPerKey) list.RemoveRange(0, list.Count - SamplesPerKey);
            IsDirty = true;
        }
    }

    /// <summary>
    /// Fortschritt in Prozent fuer eine laufende Aktion: bis zur geschaetzten Dauer linear bis 90 %,
    /// danach nur noch langsam gegen 95 %. 100 % gibt es erst, wenn die Aktion wirklich fertig ist.
    /// </summary>
    public static double Percent(TimeSpan elapsed, double estimateSeconds)
    {
        if (estimateSeconds <= 0) return 0;
        var r = Math.Max(0, elapsed.TotalSeconds) / estimateSeconds;
        return r <= 1 ? 90 * r : 90 + 5 * (1 - Math.Exp(-(r - 1)));
    }

    public string ToJson()
    {
        lock (_lock) return JsonSerializer.Serialize(_samples);
    }

    public void LoadJson(string json)
    {
        var data = JsonSerializer.Deserialize<Dictionary<string, List<double>>>(json);
        if (data is null) return;
        lock (_lock)
        {
            foreach (var (k, v) in data)
                _samples[k] = v.Where(x => x >= 0).TakeLast(SamplesPerKey).ToList();
        }
    }

    public void MarkSaved() => IsDirty = false;
}
