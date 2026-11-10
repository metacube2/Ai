using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using TrafagSalesExporter.Models;

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
        var lookup = MapLookup(map);
        if (lookup.Count == 0)
            return this;
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

    /// <summary>ColumnMap ohne leere Eintraege, technische Namen ohne Ruecksicht auf Gross-/Kleinschreibung.</summary>
    internal static Dictionary<string, string> MapLookup(IReadOnlyDictionary<string, string>? map)
    {
        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in map ?? new Dictionary<string, string>())
        {
            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                lookup[key.Trim()] = value.Trim();
        }
        return lookup;
    }
}

/// <summary>Eine Seite der Antwort: Daten plus was zum Blaettern, zur Vollstaendigkeitspruefung und fuer den Support noetig ist.</summary>
public sealed record RexxHubPage(
    RexxHubTable Table,
    string? NextLink,
    int? CurrentPage,
    int? LastPage,
    int? Total,
    string? RequestId)
{
    /// <summary>Kopfzeile einer Liste von Listen; der Abruf gibt sie an die Folgeseiten weiter.</summary>
    public IReadOnlyList<string>? Header { get; init; }

    /// <summary>Zeilen je Seite laut „_meta“, falls erkannt.</summary>
    public int? PageSize { get; init; }

    /// <summary>„count“/„Anzahl“ in „_meta“: kann Gesamtzahl oder Zeilen dieser Seite sein, deshalb nur als Untergrenze genutzt.</summary>
    public int? CountHint { get; init; }

    /// <summary>Zahlen in „_meta“ unter einem Namen mit „page“, die keiner bekannten Bedeutung zugeordnet sind (Name, Wert).</summary>
    public IReadOnlyList<(string Key, int Value)> UnknownPageNumbers { get; init; } = [];

    /// <summary>Ein Schluessel in „_links“, der nach naechster Seite aussieht, aber nicht lesbar ist.</summary>
    public string? UnreadableNextKey { get; init; }

    /// <summary>„_links.last“, falls vorhanden.</summary>
    public string? LastLink { get; init; }

    /// <summary>Schluesselnamen von „_meta“ und „_links“ (nur unverdaechtige Namen, nie Werte) fuer den Testabruf.</summary>
    public IReadOnlyList<string> MetaKeys { get; init; } = [];
    public IReadOnlyList<string> LinkKeys { get; init; } = [];
}

/// <summary>Was der Parser ausser der Antwort braucht: Kopfzeile von Seite 1, erkennbare Spalten und zusaetzliche Namen.</summary>
public sealed class RexxHubParseSettings
{
    /// <summary>Kopfzeile der vorigen Seite (Liste von Listen); wird weiterverwendet, statt neu zu raten.</summary>
    public IReadOnlyList<string>? CarriedHeader { get; init; }

    /// <summary>Gilt eine Ueberschrift als Spalte, die das Cockpit liest? Vorgabe: <see cref="RexxHubColumns.IsKnown"/> ohne ColumnMap.</summary>
    public Func<string, bool>? IsKnownHeader { get; init; }

    public IReadOnlyList<string> CurrentPageKeys { get; init; } = [];
    public IReadOnlyList<string> LastPageKeys { get; init; } = [];
    public IReadOnlyList<string> TotalKeys { get; init; } = [];
    public IReadOnlyList<string> PageSizeKeys { get; init; } = [];
    public IReadOnlyList<string> NextLinkKeys { get; init; } = [];

    /// <summary>Einstellungen aus den Optionen; die Kopfzeilen-Erkennung beruecksichtigt die ColumnMap.</summary>
    public static RexxHubParseSettings From(RexxHubOptions options, IReadOnlyList<string>? carriedHeader)
    {
        var map = RexxHubTable.MapLookup(options.ColumnMap);
        return new RexxHubParseSettings
        {
            CarriedHeader = carriedHeader,
            IsKnownHeader = header => RexxHubColumns.IsKnown(header) ||
                                      (map.TryGetValue(header.Trim(), out var mapped) && RexxHubColumns.IsKnown(mapped)),
            CurrentPageKeys = Clean(options.MetaCurrentPageKeys),
            LastPageKeys = Clean(options.MetaLastPageKeys),
            TotalKeys = Clean(options.MetaTotalKeys),
            PageSizeKeys = Clean(options.MetaPageSizeKeys),
            NextLinkKeys = Clean(options.NextLinkKeys)
        };
    }

    private static List<string> Clean(IEnumerable<string>? keys)
        => (keys ?? []).Where(k => !string.IsNullOrWhiteSpace(k)).Select(k => k.Trim()).ToList();
}

/// <summary>Die Antwort hat eine Form, die der Parser nicht kennt. Die Meldung nennt nur Schluessel, nie Werte.</summary>
public sealed class RexxHubFormatException(string message) : Exception(message);

/// <summary>
/// Setzt die JSON-Antwort von <c>GET /v1/datawarehouse/{id}</c> in Zeilen um. Laut Produktdokument (Kapitel 4) hat die
/// Antwort die Bloecke „data“, „_links“ und „_meta“ („Anzahl von Datensaetzen, Items pro Seite, aktuelle Seite und maximale
/// Seite“); die genaue Form von „data“ und die Namen in „_meta“ stehen NICHT im Dokument. Deshalb tolerant:
/// 1. data = Liste von Objekten (Schluessel = Spalten),
/// 2. data = Objekt mit Spaltenliste („columns“, „headers“, „header“, „fields“) und Zeilen („rows“, „values“, „records“,
///    „items“, „results“, „data“), Zeilen als Listen oder Objekte, oder nur mit einer Zeilenliste von Objekten,
/// 3. data = Liste von Listen mit separater Kopfzeile (Spaltenliste neben „data“ oder in „_meta“), mit der Kopfzeile der
///    vorigen Seite oder mit der Kopfzeile als erster Liste; Letzteres nur, wenn darin eine Spalte steht, die das Cockpit
///    liest (sonst koennte eine Datenzeile aus lauter Texten zur Kopfzeile werden und Werte als Spaltennamen erscheinen).
/// Steht die Liste ohne Umschlag an oberster Ebene, gilt sie als „data“.
/// </summary>
public static class RexxHubResponseParser
{
    private static readonly string[] DataKeys = ["data"];
    private static readonly string[] ColumnKeys = ["columns", "headers", "header", "fields", "columnNames", "column_names"];
    private static readonly string[] RowKeys = ["rows", "values", "records", "items", "results", "data"];
    private static readonly string[] ColumnNameKeys = ["label", "title", "name", "key", "field", "id"];
    private static readonly string[] CurrentPageKeys = ["current_page", "currentPage", "page", "current", "page_number", "pageNumber"];
    private static readonly string[] LastPageKeys =
        ["last_page", "lastPage", "max_page", "maxPage", "max_pages", "maxPages", "page_max", "pageMax", "total_pages", "totalPages",
         "pages", "page_count", "pageCount", "last"];
    // Bewusst ohne „count“: das kann auch die Zeilen dieser Seite meinen; es gilt nur als Untergrenze (CountKeys).
    private static readonly string[] TotalKeys = ["total", "total_count", "totalCount", "total_items", "totalItems", "items_total", "total_records", "totalRecords"];
    private static readonly string[] CountKeys = ["count", "anzahl"];
    private static readonly string[] PageSizeKeys = ["items_per_page", "itemsPerPage", "per_page", "perPage", "page_size", "pageSize", "limit"];
    // Verschachtelte Seitenangaben, z. B. „_meta“: {"pagination": {...}}.
    private static readonly string[] MetaScopeKeys = ["pagination", "paging", "page", "pages"];
    private static readonly string[] NextKeys = ["next", "next_page", "nextPage"];
    private static readonly string[] HrefKeys = ["href", "url", "link", "uri"];
    private static readonly string[] RequestIdKeys = ["request_id", "requestId", "requestID", "request-id", "requestid"];

    /// <summary>Schluessel, die in Meldungen und im Testabruf genannt werden duerfen: kurz, beginnt mit Buchstabe, ohne Leerzeichen.</summary>
    private static readonly Regex SafeKeyPattern = new(@"^[A-Za-z_][A-Za-z0-9_.\-]{0,39}$", RegexOptions.CultureInvariant);

    public static RexxHubPage ParsePage(string json, RexxHubParseSettings? settings = null)
    {
        settings ??= new RexxHubParseSettings();
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
            {
                var (bare, bareHeader) = ParseData(root, null, null, settings, "(Liste ohne Umschlag)");
                return new RexxHubPage(bare, null, null, null, null, null) { Header = bareHeader };
            }
            if (root.ValueKind != JsonValueKind.Object)
                throw new RexxHubFormatException($"Antwortformat unbekannt: oberste Ebene ist {root.ValueKind}.");

            var meta = Property(root, "_meta");
            var links = Property(root, "_links");
            var data = Property(root, DataKeys);
            if (data is null)
                throw Unknown(root, null);

            var outsideColumns = Property(root, ColumnKeys) ?? (meta is { ValueKind: JsonValueKind.Object } m ? Property(m, ColumnKeys) : null);
            var (table, header) = ParseData(data.Value, outsideColumns, root, settings, null);

            var scopes = MetaScopes(meta);
            var currentKeys = settings.CurrentPageKeys.Concat(CurrentPageKeys).ToArray();
            var lastKeys = settings.LastPageKeys.Concat(LastPageKeys).ToArray();
            var sizeKeys = settings.PageSizeKeys.Concat(PageSizeKeys).ToArray();
            var current = Int(scopes, currentKeys);
            var nextKeys = settings.NextLinkKeys.Concat(NextKeys).ToArray();
            var next = Link(links, nextKeys);
            return new RexxHubPage(
                table,
                next,
                current,
                Int(scopes, lastKeys),
                Int(scopes, settings.TotalKeys.Concat(TotalKeys).ToArray()),
                RequestId(root))
            {
                Header = header,
                PageSize = Int(scopes, sizeKeys),
                CountHint = Int(scopes, CountKeys),
                UnknownPageNumbers = UnknownPageNumbers(scopes, currentKeys.Concat(lastKeys).Concat(sizeKeys)),
                UnreadableNextKey = next is null ? UnreadableNext(links) : null,
                LastLink = Link(links, ["last"]),
                MetaKeys = SafeKeys(meta),
                LinkKeys = SafeKeys(links)
            };
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

    private static (RexxHubTable Table, IReadOnlyList<string>? Header) ParseData(
        JsonElement data, JsonElement? outsideColumns, JsonElement? root, RexxHubParseSettings settings, string? context)
    {
        if (data.ValueKind == JsonValueKind.Array)
            return ParseArray(data, outsideColumns, root, settings, context);

        if (data.ValueKind == JsonValueKind.Object)
        {
            var columns = Property(data, ColumnKeys) ?? outsideColumns;
            var rows = Property(data, RowKeys);
            if (rows is { ValueKind: JsonValueKind.Array } list)
                return ParseArray(list, columns, root, settings, context);
        }

        throw Unknown(root, data);
    }

    private static (RexxHubTable Table, IReadOnlyList<string>? Header) ParseArray(
        JsonElement array, JsonElement? columns, JsonElement? root, RexxHubParseSettings settings, string? context)
    {
        var table = new RexxHubTable();
        var items = array.EnumerateArray().ToList();
        var header = columns is { ValueKind: JsonValueKind.Array } c ? ColumnNames(c) : null;

        if (items.Count == 0)
        {
            header?.ForEach(table.AddColumn);
            return (table, header ?? settings.CarriedHeader);
        }

        if (items.All(x => x.ValueKind == JsonValueKind.Object))
        {
            header?.ForEach(table.AddColumn);
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
            return (table, null);
        }

        if (items.All(x => x.ValueKind == JsonValueKind.Array))
        {
            // Eine Spaltenliste, die da, aber nicht lesbar ist, ist ein Formatfehler: nicht auf die erste Zeile ausweichen.
            if (columns is not null && header is null)
                throw new RexxHubFormatException("Antwortformat unbekannt: Liste von Listen mit einer Spaltenliste, die nicht lesbar ist (erwartet Texte oder Objekte mit „label“/„name“).");

            var start = 0;
            if (header is null && settings.CarriedHeader is { Count: > 0 } carried)
            {
                // Folgeseite: Kopfzeile von Seite 1; wiederholt die Seite sie, wird sie uebersprungen.
                header = carried.ToList();
                if (IsSameHeader(items[0], header))
                    start = 1;
            }
            else if (header is null)
            {
                var first = items[0];
                var isKnown = settings.IsKnownHeader ?? RexxHubColumns.IsKnown;
                if (first.GetArrayLength() == 0 ||
                    first.EnumerateArray().Any(x => x.ValueKind != JsonValueKind.String) ||
                    !first.EnumerateArray().Any(x => isKnown(x.GetString() ?? string.Empty)))
                    throw Unknown(root, array, "Liste von Listen ohne erkennbare Kopfzeile");
                header = first.EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList();
                start = 1;
            }

            header!.ForEach(table.AddColumn);
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
            return (table, header);
        }

        throw Unknown(root, array, context);
    }

    private static bool IsSameHeader(JsonElement row, IReadOnlyList<string> header)
    {
        if (row.ValueKind != JsonValueKind.Array || row.GetArrayLength() != header.Count)
            return false;
        var i = 0;
        foreach (var cell in row.EnumerateArray())
        {
            if (cell.ValueKind != JsonValueKind.String || !string.Equals(cell.GetString(), header[i++], StringComparison.Ordinal))
                return false;
        }
        return true;
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

    /// <summary>„_meta“ selbst und verschachtelte Seitenangaben darin („pagination“ u. ae.).</summary>
    private static List<JsonElement> MetaScopes(JsonElement? meta)
    {
        var scopes = new List<JsonElement>();
        if (meta is not { ValueKind: JsonValueKind.Object } m)
            return scopes;
        scopes.Add(m);
        foreach (var property in m.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Object &&
                MetaScopeKeys.Any(k => string.Equals(k, property.Name, StringComparison.OrdinalIgnoreCase)))
                scopes.Add(property.Value);
        }
        return scopes;
    }

    /// <summary>Link als Text oder als Objekt mit „href“/„url“ (beides kommt bei REST-APIs vor; das Dokument zeigt es nicht).</summary>
    private static string? Link(JsonElement? links, string[] keys)
    {
        if (links is not { ValueKind: JsonValueKind.Object } l)
            return null;
        return LinkValue(Property(l, keys));
    }

    private static string? LinkValue(JsonElement? value) => value switch
    {
        { ValueKind: JsonValueKind.String } s when !string.IsNullOrWhiteSpace(s.GetString()) => s.GetString(),
        { ValueKind: JsonValueKind.Object } o when Property(o, HrefKeys) is { ValueKind: JsonValueKind.String } h
            && !string.IsNullOrWhiteSpace(h.GetString()) => h.GetString(),
        _ => null
    };

    /// <summary>Ein Schluessel mit „next“ in „_links“, der einen Wert hat, aber keinen lesbaren Link: dann lieber abbrechen.</summary>
    private static string? UnreadableNext(JsonElement? links)
    {
        if (links is not { ValueKind: JsonValueKind.Object } l)
            return null;
        foreach (var property in l.EnumerateObject())
        {
            if (property.Name.Contains("next", StringComparison.OrdinalIgnoreCase) &&
                property.Value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined or JsonValueKind.False) &&
                !(property.Value.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(property.Value.GetString())))
                return SafeKeyPattern.IsMatch(property.Name) ? property.Name : "(Name nicht angezeigt)";
        }
        return null;
    }

    private static List<(string, int)> UnknownPageNumbers(List<JsonElement> scopes, IEnumerable<string> knownKeys)
    {
        var known = knownKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var result = new List<(string, int)>();
        foreach (var scope in scopes)
        {
            foreach (var property in scope.EnumerateObject())
            {
                if (property.Name.Contains("page", StringComparison.OrdinalIgnoreCase) && !known.Contains(property.Name) &&
                    property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetInt32(out var number))
                    result.Add((SafeKeyPattern.IsMatch(property.Name) ? property.Name : "(Name nicht angezeigt)", number));
            }
        }
        return result;
    }

    private static int? Int(IEnumerable<JsonElement> scopes, string[] keys)
    {
        foreach (var scope in scopes)
        {
            if (Int(scope, keys) is { } value)
                return value;
        }
        return null;
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

    /// <summary>
    /// Nennbare Schluessel eines Objekts. Schluessel, die mit einer Ziffer beginnen, Leerzeichen/Komma enthalten oder lang sind,
    /// koennten Personalnummern oder Namen sein (z. B. ein nach Personalnummer geordnetes Woerterbuch) und werden nur gezaehlt.
    /// </summary>
    private static List<string> SafeKeys(JsonElement? obj)
    {
        if (obj is not { ValueKind: JsonValueKind.Object } o)
            return [];
        var names = o.EnumerateObject().Select(p => p.Name).ToList();
        var safe = names.Where(n => SafeKeyPattern.IsMatch(n)).Take(30).ToList();
        var hidden = names.Count - safe.Count;
        if (hidden > 0)
            safe.Add($"(+{hidden} nicht angezeigt)");
        return safe;
    }

    /// <summary>
    /// Fehler mit den Schluesseln der obersten Ebene (nur nennbare, siehe <see cref="SafeKeys"/>) und der Art von „data“, ohne
    /// einen einzigen Wert. Von einem data-Objekt werden nur die Anzahl der Schluessel und die bekannten Strukturnamen
    /// („columns“, „rows“ …) genannt, weil seine Schluessel selbst Personendaten sein koennen.
    /// </summary>
    private static RexxHubFormatException Unknown(JsonElement? root, JsonElement? data, string? detail = null)
    {
        var keys = root is { ValueKind: JsonValueKind.Object } ? string.Join(", ", SafeKeys(root)) : "(keine)";
        if (keys.Length == 0)
            keys = "(keine)";
        var dataKind = data switch
        {
            null => "fehlt",
            { ValueKind: JsonValueKind.Object } o => DescribeObject(o),
            { ValueKind: JsonValueKind.Array } a => "Liste mit " + string.Join("/", a.EnumerateArray().Select(x => x.ValueKind.ToString()).Distinct().Take(3)),
            { } other => other.ValueKind.ToString()
        };
        return new RexxHubFormatException(
            $"Antwortformat unbekannt: Schluessel oberste Ebene: {keys}; data: {dataKind}" + (detail is null ? "." : $" ({detail})."));
    }

    private static string DescribeObject(JsonElement obj)
    {
        var properties = obj.EnumerateObject().ToList();
        var structural = properties
            .Select(p => p.Name)
            .Where(n => ColumnKeys.Concat(RowKeys).Any(k => string.Equals(k, n, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        var kinds = string.Join("/", properties.Select(p => p.Value.ValueKind.ToString()).Distinct().Take(3));
        return $"Objekt mit {properties.Count} Schluesseln" +
               (properties.Count > 0 ? $" (Werte: {kinds})" : string.Empty) +
               (structural.Count > 0 ? $", davon bekannt: {string.Join(", ", structural)}" : ", davon keiner bekannt");
    }
}
