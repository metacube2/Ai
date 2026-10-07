using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TrafagSalesExporter.Services.Shopfloor;

/// <summary>Fachlicher Fehler mit HTTP-Status (entspricht self.err(code, msg) in server.py).</summary>
public sealed class ShopfloorException : Exception
{
    public int StatusCode { get; }
    public ShopfloorException(string message, int statusCode = 400) : base(message) => StatusCode = statusCode;
}

/// <summary>Kleine JSON-Helfer, die das lose Python-Verhalten (dict/None/Zahlen) nachbilden.</summary>
public static class SfJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver(),
        WriteIndented = false
    };

    public static string Dump(JsonNode? node) => node is null ? "null" : node.ToJsonString(Options);

    public static JsonObject ParseObject(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new JsonObject();
        return JsonNode.Parse(text) as JsonObject ?? new JsonObject();
    }

    public static JsonArray ParseArray(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new JsonArray();
        return JsonNode.Parse(text) as JsonArray ?? new JsonArray();
    }

    public static JsonNode? Clone(JsonNode? node) => node?.DeepClone();

    /// <summary>Paar [alt, neu] fuer den Verlauf.</summary>
    public static JsonArray Pair(JsonNode? oldValue, JsonNode? newValue)
    {
        var a = new JsonArray();
        a.Add(Clone(oldValue));
        a.Add(Clone(newValue));
        return a;
    }

    public static JsonObject CloneObject(JsonObject o)
    {
        var r = new JsonObject();
        foreach (var kv in o)
            r[kv.Key] = Clone(kv.Value);
        return r;
    }

    public static bool IsNumber(JsonNode? n) => n is JsonValue v && v.GetValueKind() == JsonValueKind.Number;

    public static double? AsDouble(JsonNode? n)
    {
        if (n is not JsonValue v || v.GetValueKind() != JsonValueKind.Number)
            return null;
        if (v.TryGetValue<JsonElement>(out var el)) return el.GetDouble();
        if (v.TryGetValue<long>(out var l)) return l;
        if (v.TryGetValue<int>(out var i)) return i;
        if (v.TryGetValue<double>(out var d)) return d;
        if (v.TryGetValue<decimal>(out var m)) return (double)m;
        return double.TryParse(v.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var p) ? p : null;
    }

    /// <summary>Entspricht Pythons str(x) fuer Vergleiche und Schluessel (None -> "None").</summary>
    public static string PyStr(JsonNode? n)
    {
        if (n is null) return "None";
        if (n is JsonValue v)
        {
            switch (v.GetValueKind())
            {
                case JsonValueKind.String: return v.GetValue<string>();
                case JsonValueKind.True: return "True";
                case JsonValueKind.False: return "False";
                case JsonValueKind.Null: return "None";
                default: return v.ToJsonString();
            }
        }
        return n.ToJsonString(Options);
    }

    /// <summary>Text eines Werts oder leer (None -> ""); entspricht str(x or "").</summary>
    public static string Str(JsonNode? n)
    {
        if (n is null) return string.Empty;
        if (n is JsonValue v)
        {
            switch (v.GetValueKind())
            {
                case JsonValueKind.String: return v.GetValue<string>();
                case JsonValueKind.Null: return string.Empty;
                case JsonValueKind.False: return string.Empty;
                case JsonValueKind.Number:
                    var d = AsDouble(v);
                    return d == 0 ? string.Empty : v.ToJsonString();
                default: return v.ToJsonString();
            }
        }
        return n.ToJsonString(Options);
    }

    /// <summary>Python-Wahrheitswert: None, "", 0, [], {} sind falsch.</summary>
    public static bool Truthy(JsonNode? n)
    {
        if (n is null) return false;
        if (n is JsonArray a) return a.Count > 0;
        if (n is JsonObject o) return o.Count > 0;
        if (n is JsonValue v)
        {
            switch (v.GetValueKind())
            {
                case JsonValueKind.String: return v.GetValue<string>().Length > 0;
                case JsonValueKind.Number: return AsDouble(v) != 0;
                case JsonValueKind.True: return true;
                default: return false;
            }
        }
        return true;
    }

    /// <summary>Gleichheit wie in Python (1 == 1.0, tiefer Vergleich, fehlend == None).</summary>
    public static bool Equal(JsonNode? a, JsonNode? b)
    {
        var aNull = a is null || (a is JsonValue av && av.GetValueKind() == JsonValueKind.Null);
        var bNull = b is null || (b is JsonValue bv && bv.GetValueKind() == JsonValueKind.Null);
        if (aNull || bNull) return aNull && bNull;
        if (IsNumber(a) && IsNumber(b)) return AsDouble(a) == AsDouble(b);
        if (a is JsonArray la && b is JsonArray lb)
        {
            if (la.Count != lb.Count) return false;
            for (var i = 0; i < la.Count; i++)
                if (!Equal(la[i], lb[i])) return false;
            return true;
        }
        if (a is JsonObject oa && b is JsonObject ob)
        {
            if (oa.Count != ob.Count) return false;
            foreach (var kv in oa)
                if (!ob.ContainsKey(kv.Key) || !Equal(kv.Value, ob[kv.Key])) return false;
            return true;
        }
        if (a is JsonValue x && b is JsonValue y)
        {
            if (x.GetValueKind() != y.GetValueKind()) return false;
            return x.GetValueKind() == JsonValueKind.String
                ? x.GetValue<string>() == y.GetValue<string>()
                : x.ToJsonString() == y.ToJsonString();
        }
        return false;
    }

    public static JsonNode? Get(JsonObject o, string key) => o.TryGetPropertyValue(key, out var v) ? v : null;

    public static string? GetString(JsonObject o, string key)
    {
        var n = Get(o, key);
        if (n is null) return null;
        if (n is JsonValue v && v.GetValueKind() == JsonValueKind.Null) return null;
        return PyStr(n);
    }

    public static JsonNode? Num(double d)
        => d == Math.Floor(d) && Math.Abs(d) < 1e15 ? JsonValue.Create((long)d) : JsonValue.Create(d);

    public static double? ParseFlexibleNumber(JsonNode? n)
    {
        if (n is null) return null;
        if (IsNumber(n)) return AsDouble(n);
        var s = PyStr(n).Replace("'", "").Replace("’", "").Replace(" ", "").Replace(",", ".");
        return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) && double.IsFinite(d) ? d : null;
    }
}

public sealed record ShopfloorFieldDef(string Key, string Label, string Type, string Src, string? Unit);

public sealed class ShopfloorModuleDef
{
    public string Key { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Short { get; init; } = string.Empty;
    public string? IdFormat { get; init; }
    public string? StatusField { get; init; }
    public string? DueField { get; init; }
    public string? DateField { get; init; }
    public List<string> Closed { get; init; } = [];
    public List<ShopfloorFieldDef> Fields { get; init; } = [];
}

public sealed class ShopfloorNotifyRule
{
    public List<string> Notify { get; init; } = [];
    public List<string> NotifyDone { get; init; } = [];
    public string Title { get; init; } = string.Empty;
}

/// <summary>
/// Module, Felder, Auswahllisten und Ablauf. Quelle ist shopfloor_config.json (einmalig aus
/// modules_config.py erzeugt, siehe GenerateConfig.py). /api/config liefert die Datei unveraendert aus.
/// </summary>
public sealed class ShopfloorConfig
{
    public static readonly string[] DefaultClosed = ["erledigt", "verfallen", "bestellt"];

    public string RawJson { get; }
    public List<ShopfloorModuleDef> ModuleList { get; } = [];
    public Dictionary<string, ShopfloorModuleDef> Modules { get; } = new(StringComparer.Ordinal);
    public List<string> ClosedStatus { get; } = [];
    public Dictionary<string, ShopfloorNotifyRule> NotifyRules { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, List<string>> ForecastPrefix { get; } = new(StringComparer.Ordinal);

    private ShopfloorConfig(string json)
    {
        RawJson = json;
        var root = JsonNode.Parse(json) as JsonObject ?? throw new InvalidDataException("Shopfloor-Konfiguration ist kein JSON-Objekt.");

        foreach (var m in root["modules"] as JsonArray ?? [])
        {
            if (m is not JsonObject mo) continue;
            var def = new ShopfloorModuleDef
            {
                Key = SfJson.GetString(mo, "key") ?? string.Empty,
                Kind = SfJson.GetString(mo, "kind") ?? string.Empty,
                Title = SfJson.GetString(mo, "title") ?? string.Empty,
                Short = SfJson.GetString(mo, "short") ?? string.Empty,
                IdFormat = SfJson.GetString(mo, "id_format"),
                StatusField = SfJson.GetString(mo, "status_field"),
                DueField = SfJson.GetString(mo, "due_field"),
                DateField = SfJson.GetString(mo, "date_field"),
                Closed = Strings(mo["closed"]),
                Fields = (mo["fields"] as JsonArray ?? []).OfType<JsonObject>().Select(f => new ShopfloorFieldDef(
                    SfJson.GetString(f, "key") ?? string.Empty,
                    SfJson.GetString(f, "label") ?? string.Empty,
                    SfJson.GetString(f, "type") ?? string.Empty,
                    SfJson.GetString(f, "src") ?? "manuell",
                    SfJson.GetString(f, "unit"))).ToList()
            };
            ModuleList.Add(def);
            Modules[def.Key] = def;
        }

        ClosedStatus.AddRange(Strings(root["closed_status"]));
        if (ClosedStatus.Count == 0)
            ClosedStatus.AddRange(DefaultClosed);

        if (root["notify_rules"] is JsonObject rules)
            foreach (var kv in rules)
                if (kv.Value is JsonObject r)
                    NotifyRules[kv.Key] = new ShopfloorNotifyRule
                    {
                        Notify = Strings(r["notify"]),
                        NotifyDone = Strings(r["notify_done"]),
                        Title = SfJson.GetString(r, "title") ?? string.Empty
                    };

        if (root["forecast_prefix"] is JsonObject fp)
            foreach (var kv in fp)
                ForecastPrefix[kv.Key] = Strings(kv.Value);
    }

    private static List<string> Strings(JsonNode? n)
        => (n as JsonArray ?? []).Select(x => SfJson.PyStr(x)).ToList();

    public static ShopfloorConfig FromJson(string json) => new(json);

    public static ShopfloorConfig Load(string path) => new(File.ReadAllText(path, System.Text.Encoding.UTF8));

    public bool IsClosed(string? status, ShopfloorModuleDef? mod = null)
    {
        var s = (status ?? string.Empty).Trim().ToLowerInvariant();
        var list = mod is { Closed.Count: > 0 } ? mod.Closed : ClosedStatus;
        return s.StartsWith("erl", StringComparison.Ordinal) || list.Contains(s);
    }
}
