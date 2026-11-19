using System.Globalization;
using System.Text.Json.Nodes;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Data.Sqlite;

namespace TrafagSalesExporter.Services.Shopfloor;

/// <summary>
/// Einlesen des Shopfloor-Excels der Teamleiter (Paket Fabio 19.11.2026, Entscheid 17.11.: die Datei
/// <c>PPA-Shop-Floor-File-2026</c> bleibt bis zum Go-live 01.01.2027 fuehrend und darf periodisch eingelesen werden).
/// Uebernommen werden die Rueckstandslisten (Module <c>rs_*</c>, Blatt und Spalten aus shopfloor_config.json) und die von Hand
/// gepflegten Tagesziele (Feld <c>ziel</c> ohne SAP-Quelle, heute TR5, TX, DW).
/// <list type="bullet">
/// <item>Erst Vorschau (neu / geaendert / unveraendert je Liste), dann Uebernahme; vor der Uebernahme eine Sicherung der Datenbank.</item>
/// <item>Es wird nichts geloescht: Eintraege, die nur im Board stehen, bleiben; eine leere Excel-Zelle leert kein Board-Feld.</item>
/// <item>Hat die Excel-Zelle einen Wert, gilt er (Excel ist fuehrend). Im Board geloeschte Eintraege werden nicht neu angelegt.</item>
/// </list>
/// Doku docs/SHOPFLOOR_2026-10-07.md Abschnitt 27.
/// </summary>
public static class ShopfloorExcelImport
{
    public const string Quelle = "Excel-Import";

    /// <summary>Gelesener Stand: je Liste die Eintraege (Nr, Werte), je Tagesmodul die Ziele je Tag.</summary>
    public sealed record Stand(
        Dictionary<string, List<(string Nr, JsonObject Data)>> Listen,
        Dictionary<string, SortedDictionary<string, double>> Ziele);

    internal sealed record Zelle(string Roh, bool IstText);

    /// <summary>Module, deren Tagesziel aus dem Excel kommt: Feld <c>ziel</c> ohne Quelle SAP/MES.</summary>
    public static IEnumerable<ShopfloorModuleDef> ZielModule(ShopfloorConfig cfg)
        => cfg.ModuleList.Where(m => m.Kind == "daily" && !string.IsNullOrEmpty(m.Sheet) &&
                                     m.Fields.Any(f => f.Key == "ziel" && f.Col is not null && f.Src == "manuell"));

    public static IEnumerable<ShopfloorModuleDef> ListenModule(ShopfloorConfig cfg)
        => cfg.ModuleList.Where(m => m.Kind == "list" && m.Key.StartsWith("rs_", StringComparison.Ordinal) && !string.IsNullOrEmpty(m.Sheet));

    // ------------------------------------------------------------ Lesen
    public static Stand Lesen(Stream xlsx, ShopfloorConfig cfg)
    {
        var listen = ListenModule(cfg).ToList();
        var ziele = ZielModule(cfg).ToList();
        var blaetter = listen.Concat(ziele).Select(m => m.Sheet!).ToHashSet(StringComparer.Ordinal);
        var raw = JsonNode.Parse(cfg.RawJson)!["modules"]!.AsArray().OfType<JsonObject>()
            .ToDictionary(m => SfJson.GetString(m, "key") ?? string.Empty, m => m, StringComparer.Ordinal);

        var daten = LeseBlaetter(xlsx, blaetter);
        var stand = new Stand(new(StringComparer.Ordinal), new(StringComparer.Ordinal));

        foreach (var m in listen)
        {
            if (!daten.TryGetValue(m.Sheet!, out var zeilen))
                throw new ShopfloorException($"Blatt «{m.Sheet}» fehlt in der Excel-Datei.");
            var felder = (raw[m.Key]["fields"] as JsonArray ?? []).OfType<JsonObject>().ToList();
            var liste = new List<(string, JsonObject)>();
            foreach (var (nrZeile, z) in zeilen.Where(kv => kv.Key > m.HeaderRow).OrderBy(kv => kv.Key))
            {
                var nr = Wert(z, "A")?.Roh.Trim() ?? string.Empty;
                if (nr.Length == 0)
                    continue;
                // nur Zeilen mit Eintrag: das Excel hat vorbelegte leere Nummern (Abschnitt 26.3)
                if (Wert(z, "B") is null && Wert(z, "G") is null && Wert(z, "T") is null)
                    continue;
                var data = new JsonObject();
                foreach (var f in felder)
                {
                    var key = SfJson.GetString(f, "key") ?? string.Empty;
                    var typ = SfJson.GetString(f, "type") ?? string.Empty;
                    if (typ == "calc")
                        continue;
                    if (typ == "flags")
                    {
                        if (f["cols"] is not JsonObject cols)
                            continue;
                        var flags = new JsonArray();
                        foreach (var kv in cols)
                        {
                            var c = Wert(z, SfJson.PyStr(kv.Value));
                            if (c is not null && c.Roh.Trim() is { Length: > 0 } t && t != "0")
                                flags.Add(kv.Key);
                        }
                        if (flags.Count > 0)
                            data[key] = flags;
                        continue;
                    }
                    var col = SfJson.GetString(f, "col");
                    if (col is null || Wert(z, col) is not { } zelle)
                        continue;
                    var v = Umwandeln(typ, zelle);
                    if (v is not null)
                        data[key] = v;
                }
                liste.Add((nr, data));
            }
            stand.Listen[m.Key] = liste;
        }

        foreach (var m in ziele)
        {
            if (!daten.TryGetValue(m.Sheet!, out var zeilen))
                throw new ShopfloorException($"Blatt «{m.Sheet}» fehlt in der Excel-Datei.");
            var zielCol = m.Fields.First(f => f.Key == "ziel").Col!;
            // Datumsspalte: Kopfzelle „Datum:“ in der Kopfzeile (Blaetter 3. bis 6.: Spalte C)
            var datumCol = zeilen.TryGetValue(m.HeaderRow, out var kopf)
                ? kopf.FirstOrDefault(kv => kv.Value.Roh.Trim().StartsWith("Datum", StringComparison.OrdinalIgnoreCase)).Key
                : null;
            datumCol ??= "C";
            var tage = new SortedDictionary<string, double>(StringComparer.Ordinal);
            foreach (var (_, z) in zeilen.Where(kv => kv.Key > m.HeaderRow))
            {
                if (Wert(z, datumCol) is not { IstText: false } d || !double.TryParse(d.Roh, NumberStyles.Float, CultureInfo.InvariantCulture, out var oa))
                    continue;
                if (Wert(z, zielCol) is not { } zz || Umwandeln("num", zz) is not JsonNode n || SfJson.AsDouble(n) is not double ziel)
                    continue;
                tage[Iso(DateTime.FromOADate(oa))] = ziel;
            }
            stand.Ziele[m.Key] = tage;
        }
        return stand;
    }

    private static Zelle? Wert(Dictionary<string, Zelle> z, string col) => z.TryGetValue(col, out var c) && c.Roh.Length > 0 ? c : null;

    private static string Iso(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>Wert typgerecht wie der Import vom 05.10.: Datum ISO, Zahl als Zahl, Text als Text (Nummern ohne „.0“).</summary>
    internal static JsonNode? Umwandeln(string typ, Zelle z)
    {
        var roh = z.Roh.Trim();
        if (roh.Length == 0)
            return null;
        if (typ == "date")
        {
            if (!z.IstText && double.TryParse(roh, NumberStyles.Float, CultureInfo.InvariantCulture, out var oa) && oa is > 1 and < 2958465)
                return Iso(DateTime.FromOADate(oa));
            return DateTime.TryParseExact(roh, ["d.M.yyyy", "dd.MM.yyyy", "d.M.yy", "yyyy-MM-dd"], CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var dt)
                ? Iso(dt)
                : roh;   // Text statt Datum (z. B. „ERLEDIGT“) unveraendert behalten
        }
        if (typ == "num")
        {
            if (double.TryParse(roh, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) && !z.IstText)
                return SfJson.Num(Math.Round(d, 6));
            var flex = SfJson.ParseFlexibleNumber(JsonValue.Create(roh));
            return flex is null ? roh : SfJson.Num(flex.Value);
        }
        if (!z.IstText && double.TryParse(roh, NumberStyles.Float, CultureInfo.InvariantCulture, out var zahl))
            return Math.Abs(zahl % 1) < 1e-9 ? ((long)Math.Round(zahl)).ToString(CultureInfo.InvariantCulture)
                                              : zahl.ToString(CultureInfo.InvariantCulture);
        return roh;
    }

    internal static Zelle ZelleText(string s) => new(s, true);
    internal static Zelle ZelleZahl(string s) => new(s, false);

    /// <summary>Liest nur die genannten Blaetter, zeilenweise (die Datei hat ueber 30 Blaetter und rund 12 MB).</summary>
    private static Dictionary<string, Dictionary<int, Dictionary<string, Zelle>>> LeseBlaetter(Stream xlsx, ISet<string> namen)
    {
        var result = new Dictionary<string, Dictionary<int, Dictionary<string, Zelle>>>(StringComparer.Ordinal);
        SpreadsheetDocument doc;
        try
        {
            doc = SpreadsheetDocument.Open(xlsx, false);
        }
        catch (Exception ex) when (ex is OpenXmlPackageException or InvalidDataException or FileFormatException)
        {
            throw new ShopfloorException("Die Datei ist keine Excel-Datei (.xlsx).");
        }
        using (doc)
        {
            var wb = doc.WorkbookPart ?? throw new ShopfloorException("Excel-Datei ohne Arbeitsmappe.");
            var sst = wb.SharedStringTablePart?.SharedStringTable?.Elements<SharedStringItem>().Select(i => i.InnerText).ToList() ?? [];
            foreach (var sheet in wb.Workbook.Sheets?.Elements<Sheet>() ?? [])
            {
                var name = sheet.Name?.Value ?? string.Empty;
                if (!namen.Contains(name) || sheet.Id?.Value is not { } id || wb.GetPartById(id) is not WorksheetPart wsp)
                    continue;
                var zeilen = new Dictionary<int, Dictionary<string, Zelle>>();
                using var reader = OpenXmlReader.Create(wsp);
                while (reader.Read())
                {
                    if (reader.ElementType != typeof(Row) || !reader.IsStartElement)
                        continue;
                    var row = (Row)reader.LoadCurrentElement()!;
                    var nr = (int)(row.RowIndex?.Value ?? 0);
                    var zellen = new Dictionary<string, Zelle>(StringComparer.Ordinal);
                    foreach (var c in row.Elements<Cell>())
                    {
                        var col = new string((c.CellReference?.Value ?? string.Empty).TakeWhile(char.IsLetter).ToArray());
                        if (col.Length == 0)
                            continue;
                        var t = c.DataType?.Value;
                        if (t == CellValues.Error)
                            continue;
                        string roh;
                        bool istText;
                        if (t == CellValues.SharedString)
                        {
                            roh = int.TryParse(c.CellValue?.Text, out var i) && i >= 0 && i < sst.Count ? sst[i] : string.Empty;
                            istText = true;
                        }
                        else if (t == CellValues.InlineString)
                        {
                            roh = c.InlineString?.InnerText ?? string.Empty;
                            istText = true;
                        }
                        else if (t == CellValues.String)
                        {
                            roh = c.CellValue?.Text ?? string.Empty;
                            istText = true;
                        }
                        else if (t == CellValues.Boolean)
                        {
                            roh = c.CellValue?.Text == "1" ? "x" : string.Empty;
                            istText = true;
                        }
                        else
                        {
                            roh = c.CellValue?.Text ?? string.Empty;
                            istText = false;
                        }
                        if (roh.Length > 0)
                            zellen[col] = new Zelle(roh, istText);
                    }
                    if (zellen.Count > 0)
                        zeilen[nr] = zellen;
                }
                result[name] = zeilen;
            }
        }
        return result;
    }

    // ------------------------------------------------------------ Vergleich und Uebernahme
    /// <summary>
    /// Vergleicht den gelesenen Stand mit dem Board; mit <paramref name="uebernehmen"/> schreibt er die Unterschiede (in einer
    /// Transaktion, Quelle „Excel-Import“). Ohne bleibt alles unveraendert (Vorschau).
    /// </summary>
    public static JsonObject Abgleich(ShopfloorStore store, Stand stand, string user, bool uebernehmen)
    {
        return store.Db.Write(con =>
        {
            var listen = new JsonArray();
            var widersprueche = 0;
            foreach (var (module, eintraege) in stand.Listen)
            {
                var mod = store.Config.Modules[module];
                var board = ShopfloorDb.Query(con, "SELECT nr, data, deleted FROM records WHERE module=?", module)
                    .ToDictionary(r => r.Str("nr") ?? string.Empty, r => (Data: SfJson.ParseObject(r.Str("data")), Deleted: r.Long("deleted") != 0),
                        StringComparer.Ordinal);
                int neu = 0, geaendert = 0, gleich = 0, geloescht = 0, status = 0;
                var beispieleNeu = new JsonArray();
                var beispieleGeaendert = new JsonArray();
                var excelNrs = new HashSet<string>(StringComparer.Ordinal);
                foreach (var (nr, data) in eintraege)
                {
                    excelNrs.Add(nr);
                    if (board.TryGetValue(nr, out var b) && b.Deleted)
                    {
                        geloescht++;
                        continue;
                    }
                    var changes = new JsonObject();
                    foreach (var kv in data)
                    {
                        var alt = b.Data is null ? null : SfJson.Get(b.Data, kv.Key);
                        if (!SfJson.Equal(alt, kv.Value))
                            changes[kv.Key] = kv.Value!.DeepClone();
                    }
                    if (b.Data is null)
                    {
                        neu++;
                        if (beispieleNeu.Count < 10) beispieleNeu.Add(nr);
                    }
                    else if (changes.Count > 0)
                    {
                        geaendert++;
                        if (mod.StatusField is { } sf && changes.ContainsKey(sf)) status++;
                        if (beispieleGeaendert.Count < 10)
                            beispieleGeaendert.Add(new JsonObject { ["nr"] = nr, ["felder"] = new JsonArray(changes.Select(kv => (JsonNode?)kv.Key).ToArray()) });
                    }
                    else
                        gleich++;
                    if (uebernehmen && (b.Data is null || changes.Count > 0))
                        store.SaveRecord(con, module, nr, b.Data is null ? SfJson.CloneObject(data) : changes, user, Quelle);
                    // Widerspruch nach der Uebernahme: Abschlussdatum gesetzt, Status nicht abgeschlossen (Entscheid Fabio 17.11.)
                    var nachher = b.Data is null ? data : Merge(b.Data, changes);
                    if (mod.StatusField is { } stf && SfJson.Truthy(SfJson.Get(nachher, "abschluss")) &&
                        !store.Config.IsClosed(SfJson.Str(SfJson.Get(nachher, stf)), mod))
                        widersprueche++;
                }
                listen.Add(new JsonObject
                {
                    ["module"] = module,
                    ["title"] = mod.Title,
                    ["excel"] = eintraege.Count,
                    ["neu"] = neu,
                    ["geaendert"] = geaendert,
                    ["status_geaendert"] = status,
                    ["unveraendert"] = gleich,
                    ["nur_im_board"] = board.Count(kv => !kv.Value.Deleted && !excelNrs.Contains(kv.Key)),
                    ["im_board_geloescht"] = geloescht,
                    ["beispiele_neu"] = beispieleNeu,
                    ["beispiele_geaendert"] = beispieleGeaendert
                });
            }

            var ziele = new JsonArray();
            foreach (var (module, tage) in stand.Ziele)
            {
                int neu = 0, geaendert = 0, gleich = 0;
                foreach (var (day, ziel) in tage)
                {
                    var row = ShopfloorDb.QueryOne(con, "SELECT data FROM daily WHERE module=? AND day=?", module, day);
                    var alt = row is null ? null : SfJson.Get(SfJson.ParseObject(row.Str("data")), "ziel");
                    var wert = SfJson.Num(ziel);
                    if (alt is null) neu++;
                    else if (SfJson.Equal(alt, wert)) { gleich++; continue; }
                    else geaendert++;
                    if (uebernehmen)
                        store.SaveDaily(con, module, day, new JsonObject { ["ziel"] = wert }, user, Quelle);
                }
                ziele.Add(new JsonObject
                {
                    ["module"] = module,
                    ["title"] = store.Config.Modules[module].Title,
                    ["tage"] = tage.Count,
                    ["von"] = tage.Count > 0 ? tage.Keys.First() : null,
                    ["bis"] = tage.Count > 0 ? tage.Keys.Last() : null,
                    ["neu"] = neu,
                    ["geaendert"] = geaendert,
                    ["unveraendert"] = gleich
                });
            }

            if (!uebernehmen)
                throw new VorschauAbbruch(new JsonObject { ["listen"] = listen, ["ziele"] = ziele, ["widersprueche"] = widersprueche });
            return new JsonObject { ["listen"] = listen, ["ziele"] = ziele, ["widersprueche"] = widersprueche };
        });
    }

    /// <summary>Vorschau laeuft im selben Schreibpfad und wird per Rollback verworfen, damit sie exakt der Uebernahme entspricht.</summary>
    private sealed class VorschauAbbruch(JsonObject ergebnis) : Exception("Vorschau")
    {
        public JsonObject Ergebnis { get; } = ergebnis;
    }

    /// <summary>Vorschau oder Uebernahme. Bei der Uebernahme vorher eine Sicherung in <paramref name="backupDir"/>.</summary>
    public static JsonObject Ausfuehren(ShopfloorStore store, Stream xlsx, string user, bool uebernehmen, string backupDir, string dateiname)
    {
        var stand = Lesen(xlsx, store.Config);
        JsonObject ergebnis;
        string? sicherung = null;
        if (uebernehmen)
        {
            sicherung = Sichern(store.Db, backupDir);
            ergebnis = Abgleich(store, stand, user, true);
        }
        else
        {
            try
            {
                ergebnis = Abgleich(store, stand, user, false);
            }
            catch (VorschauAbbruch v)
            {
                ergebnis = v.Ergebnis;
            }
        }
        ergebnis["ok"] = true;
        ergebnis["uebernommen"] = uebernehmen;
        ergebnis["datei"] = dateiname;
        ergebnis["sicherung"] = sicherung;
        return ergebnis;
    }

    /// <summary>Kopie der Datenbank vor der Uebernahme (SQLite-Backup, auch bei offenem WAL vollstaendig).</summary>
    public static string Sichern(ShopfloorDb db, string backupDir)
    {
        Directory.CreateDirectory(backupDir);
        var ziel = Path.Combine(backupDir, $"shopfloor-vor-excel-import-{DateTime.Now:yyyyMMdd-HHmmss}.db");
        using var src = db.Open();
        using var dst = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = ziel }.ToString());
        dst.Open();
        src.BackupDatabase(dst);
        return ziel;
    }

    private static JsonObject Merge(JsonObject a, JsonObject b)
    {
        var o = SfJson.CloneObject(a);
        foreach (var kv in b)
            o[kv.Key] = kv.Value?.DeepClone();
        return o;
    }
}
