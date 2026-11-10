using System.Globalization;
using System.Text.Json;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Tabelle aus einer Rexx-Hub-Antwort: Spalten in der Reihenfolge des ersten Auftretens, Zeilen als Spaltenname -&gt; Wert.
/// Werte sind <see cref="string"/>, <see cref="decimal"/>, <see cref="bool"/> oder null; verschachtelte Objekte und Listen
/// bleiben als JSON-Text stehen.
/// </summary>
public sealed class RexxHubTable
{
    public List<string> Columns { get; } = [];
    public List<Dictionary<string, object?>> Rows { get; } = [];

    /// <summary>Haengt Zeilen an und nimmt neue Spalten in die Spaltenliste auf (Objekte koennen leere Felder weglassen).</summary>
    public void Append(RexxHubTable other)
    {
        foreach (var column in other.Columns)
            AddColumn(column);
        Rows.AddRange(other.Rows);
    }

    public void AddColumn(string column)
    {
        if (!Columns.Contains(column, StringComparer.Ordinal))
            Columns.Add(column);
    }

    /// <summary>
    /// Benennt Spalten nach <c>HrKpi:RexxHub:ColumnMap</c> um (technischer Name -&gt; Ueberschrift des Handexports).
    /// Gross-/Kleinschreibung der technischen Namen zaehlt nicht.
    /// </summary>
    public RexxHubTable WithColumnMap(IReadOnlyDictionary<string, string>? map)
    {
        if (map is null || map.Count == 0)
            return this;
        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in map)
        {
            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                lookup[key.Trim()] = value.Trim();
        }
        string Rename(string column) => lookup.TryGetValue(column, out var mapped) ? mapped : column;

        var result = new RexxHubTable();
        foreach (var column in Columns)
            result.Columns.Add(Rename(column));
        foreach (var row in Rows)
        {
            var renamed = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var (key, value) in row)
                renamed.TryAdd(Rename(key), value);
            result.Rows.Add(renamed);
        }
        return result;
    }
}

/// <summary>Eine Seite der Antwort: Daten plus was zum Blaettern und fuer den Support noetig ist.</summary>
public sealed record RexxHubPage(
    RexxHubTable Table,
    string? NextLink,
    int? CurrentPage,
    int? LastPage,
    int? Total,
    string? RequestId);

/// <summary>Die Antwort hat eine Form, die der Parser nicht kennt. Die Meldung nennt nur Schluessel, nie Werte.</summary>
public sealed class RexxHubFormatException(string message) : Exception(message);

/// <summary>
/// Setzt die JSON-Antwort von <c>GET /v1/datawarehouse/{id}</c> in Zeilen um. Laut Produktdokument (Kapitel 4) hat die
/// Antwort die Bloecke „data“, „_links“ und „_meta“; die genaue Form von „data“ und die Namen in „_meta“ stehen NICHT im
/// Dokument. Deshalb tolerant:
/// 1. data = Liste von Objekten (Schluessel = Spalten),
/// 2. data = Objekt mit Spaltenliste („columns“, „headers“, „header“, „fields“) und Zeilen („rows“, „values“, „records“,
///    „items“, „results“, „data“), Zeilen als Listen oder Objekte, oder nur mit einer Zeilenliste von Objekten,
/// 3. data = Liste von Listen mit separater Kopfzeile (Spaltenliste neben „data“ oder in „_meta“) oder mit der Kopfzeile
///    als erster Liste.
/// Steht die Liste ohne Umschlag an oberster Ebene, gilt sie als „data“.
/// </summary>
public static class RexxHubResponseParser
{
    private static readonly string[] DataKeys = ["data"];
    private static readonly string[] ColumnKeys = ["columns", "headers", "header", "fields", "columnNames", "column_names"];
    private static readonly string[] RowKeys = ["rows", "values", "records", "items", "results", "data"];
    private static readonly string[] ColumnNameKeys = ["label", "title", "name", "key", "field", "id"];
    private static readonly string[] CurrentPageKeys = ["current_page", "currentPage", "page", "current"];
    private static readonly string[] LastPageKeys = ["last_page", "lastPage", "max_page", "maxPage", "total_pages", "totalPages", "pages", "page_count", "pageCount"];
    // Bewusst ohne „count“: das kann auch die Zeilen dieser Seite meinen und wuerde dann jeden mehrseitigen Abruf abbrechen.
    private static readonly string[] TotalKeys = ["total", "total_count", "totalCount", "total_items", "totalItems", "items_total"];
    private static readonly string[] RequestIdKeys = ["request_id", "requestId", "requestID", "request-id", "requestid"];

    public static RexxHubPage ParsePage(string json)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            throw new RexxHubFormatException("Antwort ist kein gueltiges JSON.");
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Array)
                return new RexxHubPage(ParseData(root, null, null, "(Liste ohne Umschlag)"), null, null, null, null, null);
            if (root.ValueKind != JsonValueKind.Object)
                throw new RexxHubFormatException($"Antwortformat unbekannt: oberste Ebene ist {root.ValueKind}.");

            var meta = Property(root, "_meta");
            var links = Property(root, "_links");
            var data = Property(root, DataKeys);
            if (data is null)
                throw Unknown(root, null);

            var outsideColumns = Property(root, ColumnKeys) ?? (meta is { ValueKind: JsonValueKind.Object } m ? Property(m, ColumnKeys) : null);
            var table = ParseData(data.Value, outsideColumns, root, null);
            return new RexxHubPage(
                table,
                NextLink(links),
                meta is { ValueKind: JsonValueKind.Object } m1 ? Int(m1, CurrentPageKeys) : null,
                meta is { ValueKind: JsonValueKind.Object } m2 ? Int(m2, LastPageKeys) : null,
                meta is { ValueKind: JsonValueKind.Object } m3 ? Int(m3, TotalKeys) : null,
                RequestId(root));
        }
    }

    /// <summary>Request id aus „_meta“ (auch in Fehlerantworten), fuer den Support; null, wenn keine da ist.</summary>
    public static string? RequestId(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return null;
        var meta = Property(root, "_meta");
        var value = meta is { ValueKind: JsonValueKind.Object } m ? Property(m, RequestIdKeys) : Property(root, RequestIdKeys);
        return value is { } v && v.ValueKind is JsonValueKind.String or JsonValueKind.Number ? v.ToString() : null;
    }

    private static RexxHubTable ParseData(JsonElement data, JsonElement? outsideColumns, JsonElement? root, string? context)
    {
        if (data.ValueKind == JsonValueKind.Array)
            return ParseArray(data, outsideColumns, root, context);

        if (data.ValueKind == JsonValueKind.Object)
        {
            var columns = Property(data, ColumnKeys) ?? outsideColumns;
            var rows = Property(data, RowKeys);
            if (rows is { ValueKind: JsonValueKind.Array } list)
                return ParseArray(list, columns, root, context);
        }

        throw Unknown(root, data);
    }

    private static RexxHubTable ParseArray(JsonElement array, JsonElement? columns, JsonElement? root, string? context)
    {
        var table = new RexxHubTable();
        var items = array.EnumerateArray().ToList();
        var header = columns is { ValueKind: JsonValueKind.Array } c ? ColumnNames(c) : null;
        if (header is not null)
            header.ForEach(table.AddColumn);

        if (items.Count == 0)
            return table;

        if (items.All(x => x.ValueKind == JsonValueKind.Object))
        {
            foreach (var item in items)
            {
                var row = new Dictionary<string, object?>(StringComparer.Ordinal);
                foreach (var property in item.EnumerateObject())
                {
                    table.AddColumn(property.Name);
                    row[property.Name] = Value(property.Value);
                }
                table.Rows.Add(row);
            }
            return table;
        }

        if (items.All(x => x.ValueKind == JsonValueKind.Array))
        {
            var start = 0;
            if (header is null)
            {
                // Ohne separate Kopfzeile gilt die erste Liste als Kopfzeile, wenn sie nur aus Texten besteht.
                var first = items[0];
                if (first.GetArrayLength() == 0 || first.EnumerateArray().Any(x => x.ValueKind != JsonValueKind.String))
                    throw Unknown(root, array, "Liste von Listen ohne Kopfzeile");
                header = first.EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList();
                header.ForEach(table.AddColumn);
                start = 1;
            }
            for (var i = start; i < items.Count; i++)
            {
                var values = items[i].EnumerateArray().ToList();
                if (values.Count > header.Count)
                    throw new RexxHubFormatException($"Antwortformat unbekannt: Zeile {i + 1} hat {values.Count} Werte, die Kopfzeile nur {header.Count} Spalten.");
                var row = new Dictionary<string, object?>(StringComparer.Ordinal);
                for (var col = 0; col < values.Count; col++)
                    row.TryAdd(header[col], Value(values[col]));
                table.Rows.Add(row);
            }
            return table;
        }

        throw Unknown(root, array, context);
    }

    private static List<string>? ColumnNames(JsonElement columns)
    {
        var names = new List<string>();
        foreach (var column in columns.EnumerateArray())
        {
            if (column.ValueKind == JsonValueKind.String)
                names.Add(column.GetString() ?? string.Empty);
            else if (column.ValueKind == JsonValueKind.Object && Property(column, ColumnNameKeys) is { ValueKind: JsonValueKind.String } name)
                names.Add(name.GetString() ?? string.Empty);
            else
                return null;
        }
        return names;
    }

    /// <summary>JSON-Wert -&gt; Zellwert. Zahlen bleiben Zahlen, Texte bleiben Texte (auch „12:30“ und fuehrende Nullen).</summary>
    internal static object? Value(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number => value.TryGetDecimal(out var d)
            ? d
            : double.TryParse(value.GetRawText(), NumberStyles.Float, CultureInfo.InvariantCulture, out var f) && Math.Abs(f) < 7.9e28
                ? (decimal)f
                : value.GetRawText(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => value.GetRawText()
    };

    /// <summary>„_links.next“ als Text oder als Objekt mit „href“ (beides kommt bei REST-APIs vor; das Dokument zeigt es nicht).</summary>
    private static string? NextLink(JsonElement? links)
    {
        if (links is not { ValueKind: JsonValueKind.Object } l)
            return null;
        var next = Property(l, "next");
        return next switch
        {
            { ValueKind: JsonValueKind.String } s when !string.IsNullOrWhiteSpace(s.GetString()) => s.GetString(),
            { ValueKind: JsonValueKind.Object } o when Property(o, "href") is { ValueKind: JsonValueKind.String } h
                && !string.IsNullOrWhiteSpace(h.GetString()) => h.GetString(),
            _ => null
        };
    }

    private static int? Int(JsonElement obj, string[] keys)
    {
        var value = Property(obj, keys);
        if (value is null)
            return null;
        if (value.Value.ValueKind == JsonValueKind.Number && value.Value.TryGetInt32(out var number))
            return number;
        return value.Value.ValueKind == JsonValueKind.String &&
               int.TryParse(value.Value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static JsonElement? Property(JsonElement obj, params string[] names)
    {
        if (obj.ValueKind != JsonValueKind.Object)
            return null;
        foreach (var name in names)
        {
            foreach (var property in obj.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                    return property.Value;
            }
        }
        return null;
    }

    /// <summary>Fehler mit den Schluesseln der obersten Ebene und der Art von „data“, ohne einen einzigen Wert.</summary>
    private static RexxHubFormatException Unknown(JsonElement? root, JsonElement? data, string? detail = null)
    {
        var keys = root is { ValueKind: JsonValueKind.Object } r
            ? string.Join(", ", r.EnumerateObject().Select(p => p.Name).Take(20))
            : "(keine)";
        var dataKind = data switch
        {
            null => "fehlt",
            { ValueKind: JsonValueKind.Object } o => "Objekt mit Schluesseln " + string.Join(", ", o.EnumerateObject().Select(p => p.Name).Take(20)),
            { ValueKind: JsonValueKind.Array } a => "Liste mit " + string.Join("/", a.EnumerateArray().Select(x => x.ValueKind.ToString()).Distinct().Take(3)),
            { } other => other.ValueKind.ToString()
        };
        return new RexxHubFormatException(
            $"Antwortformat unbekannt: Schluessel oberste Ebene: {keys}; data: {dataKind}" + (detail is null ? "." : $" ({detail})."));
    }
}
