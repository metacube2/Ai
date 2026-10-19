using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TrafagSalesExporter.Services.Shopfloor;

/// <summary>
/// Operations (Shopfloor), 2026-10-16, Mail Fabio „Datenabfragen J5, J8, J11“: Mengen aus SAP.
/// <list type="bullet">
/// <item>J5 Logistik: ZC27-Logik (ShopRmSet, Gutmenge je Arbeitsplatz nach Erfassungsdatum ERSDA, ohne Stornos, nur der
/// letzte Vorgang an den gewaehlten Arbeitsplaetzen). Jede Gruppe ist ein eigener ZC27-Lauf, wie der Teamleiter ihn macht.</item>
/// <item>J8 TR5: dieselbe Logik mit der ZC27-Variante /PPA EXX72 (elf Arbeitsplaetze, Typencode 8*), gelesen in T76 am 2026-10-16.</item>
/// <item>J8 SEH: ShopProdSet, WE-Menge der Fertigungsauftraege mit Istendtermin am Tag, Disponent S* (wie COOIS /SEH 001).</item>
/// <item>DW: ZC27-Logik am Arbeitsplatz DL790 (Mail Fabio 2026-10-17, Punkt 2).</item>
/// <item>J11 FPY SEH: WE-Menge / Gesamtmenge der Auftraege mit Istendtermin in der KW des Tages, bis zu diesem Tag.</item>
/// </list>
/// Seit 2026-10-19 (Feedback Logistik ueber Fabio): Eintrag vom Tag D = Produktionstag D (ERSDA bzw. Istendtermin = D),
/// nicht mehr Abfragetag mit Stand D-1. Die Montagsregel Freitag bis Sonntag entfaellt. Der automatische Abgleich schreibt
/// die letzten Tage neu (<see cref="SyncDays"/>), damit Wochenende und Nachbuchungen stimmen. Manuell erfasste Werte
/// werden nicht ueberschrieben. ABAP: docs/abap/ZSHOP_RM_ADD.abap. Doku: saptasks/shopfloor_sap/NOTES.md.
/// </summary>
public static class ShopfloorSapMengen
{
    public const string RmSet = "ShopRmSet";
    public const string ProdSet = "ShopProdSet";
    public const string SehDispo = "S*";

    public sealed record RmGroup(string Module, string Field, string[] Arbpl, string? Typcd = null);

    /// <summary>Zuordnung laut Mail Fabio vom 2026-10-16 (Punkt 4) und ZC27-Variante /PPA EXX72.</summary>
    public static readonly RmGroup[] RmGroups =
    [
        new("logistik", "tr5_mle01", ["MLE01"]),
        new("logistik", "tr5_mle02", ["MLE02"]),
        new("logistik", "tx_mlm01", ["MLM01"]),
        new("logistik", "dw_mle04", ["MLE04"]),
        new("logistik", "tx_mle08", ["MLE08", "MLE09"]),
        new("tr5", "prod", ["EDF72", "EDB72", "EAC72", "EAP72", "EAN72", "EAM72", "EDD72", "EEX72", "EAS72", "EDS72", "EVA72"], "8*"),
        new("dw", "prod", ["DL790"])
    ];

    /// <summary>Wie viele Tage vor heute der automatische Abgleich neu schreibt. 3 deckt am Montag Freitag bis Sonntag ab.</summary>
    public const int SyncDaysBack = 3;

    /// <summary>Produktionstage, die der automatische Abgleich am Tag <paramref name="today"/> schreibt: heute (laufend) und
    /// die drei Tage davor. Am Wochenende laeuft kein Abgleich; am Montag kommen so Freitag, Samstag und Sonntag nach.</summary>
    public static List<DateOnly> SyncDays(DateOnly today)
        => Enumerable.Range(0, SyncDaysBack + 1).Select(i => today.AddDays(i - SyncDaysBack)).ToList();

    /// <summary>KW (Montag bis Sonntag) des Tages, nur bis zu diesem Tag.</summary>
    public static List<DateOnly> FpyWeek(DateOnly day)
    {
        var monday = day.AddDays(-(((int)day.DayOfWeek + 6) % 7));
        return Enumerable.Range(0, 7).Select(monday.AddDays).Where(d => d <= day).ToList();
    }

    /// <summary>Rechnet alle Werte fuer den Produktionstag D. Schreibt nichts. <paramref name="read"/> liest ein Set mit Filter.</summary>
    public static async Task<JsonObject> ComputeAsync(DateOnly day, string plant,
        Func<string, string, Task<List<JsonElement>>> read)
    {
        List<DateOnly> days = [day];
        var values = new JsonObject();
        var detail = new JsonArray();
        var zeilen = new Dictionary<string, int>(StringComparer.Ordinal);   // Zeilen aus SAP je Bereich

        var ersda = Or("Ersda", days);
        foreach (var g in RmGroups)
        {
            var filter = $"Werks eq '{plant}' and ({string.Join(" or ", g.Arbpl.Select(a => $"Arbpl eq '{a}'"))}) and {ersda}"
                         + (g.Typcd is null ? string.Empty : $" and Typcd eq '{g.Typcd}'");
            var rows = await read(RmSet, filter);
            zeilen[g.Module] = zeilen.GetValueOrDefault(g.Module) + rows.Count;
            var menge = rows.Sum(r => Int(r, "Lmnga"));
            Put(values, g.Module, g.Field, menge);
            var je = new JsonObject();
            foreach (var r in rows.GroupBy(r => Text(r, "Arbpl")).OrderBy(x => x.Key, StringComparer.Ordinal))
                je[r.Key] = r.Sum(x => Int(x, "Lmnga"));
            detail.Add(new JsonObject
            {
                ["feld"] = $"{g.Module}.{g.Field}",
                ["arbeitsplaetze"] = string.Join("+", g.Arbpl),
                ["typencode"] = g.Typcd,
                ["menge"] = menge,
                ["rueckmeldungen"] = rows.Sum(r => Int(r, "Anzahl")),
                ["je_arbeitsplatz"] = je
            });
        }

        var seh = await read(ProdSet, $"Werks eq '{plant}' and Dispo eq '{SehDispo}' and {Or("Datum", days)}");
        zeilen["seh"] = zeilen.GetValueOrDefault("seh") + seh.Count;
        var sehMenge = seh.Sum(r => Int(r, "Wemng"));
        Put(values, "seh", "prod", sehMenge);
        detail.Add(new JsonObject
        {
            ["feld"] = "seh.prod",
            ["disponent"] = SehDispo,
            ["menge"] = sehMenge,
            ["auftraege"] = seh.Sum(r => Int(r, "Anzahl"))
        });

        var week = FpyWeek(day);
        List<JsonElement> fpyRows = week.Count == 0 ? [] : await read(ProdSet, $"Werks eq '{plant}' and Dispo eq '{SehDispo}' and {Or("Datum", week)}");
        var gesamt = fpyRows.Sum(r => Int(r, "Gamng"));
        var we = fpyRows.Sum(r => Int(r, "Wemng"));
        if (gesamt > 0)
            Put(values, "seh", "fpy", Math.Round(100.0 * we / gesamt, 1));
        detail.Add(new JsonObject
        {
            ["feld"] = "seh.fpy",
            ["woche"] = week.Count == 0 ? null : $"{week[0]:yyyy-MM-dd} bis {week[^1]:yyyy-MM-dd}",
            ["we_menge"] = we,
            ["gesamtmenge"] = gesamt,
            ["auftraege"] = fpyRows.Sum(r => Int(r, "Anzahl"))
        });

        // Bereich ohne jede Rueckmeldung bzw. Istende am Tag (Wochenende, Feiertag): keine Nullen schreiben, sondern frueher
        // aus SAP geschriebene Werte entfernen (null loescht das Feld, ein leerer Tagessatz verschwindet). Je Bereich, weil
        // SEH auch samstags Istenden hat (17.10.2026: SEH 751, Logistik nichts). Hat ein Bereich Zeilen, bleibt 0 eine 0.
        var leer = new JsonArray();
        foreach (var (module, node) in values)
        {
            if (zeilen.GetValueOrDefault(module) > 0)
                continue;
            leer.Add(module);
            foreach (var key in ((JsonObject)node!).Select(kv => kv.Key).ToList())
                ((JsonObject)node!)[key] = null;
        }
        var ohneProduktion = leer.Count == values.Count;

        return new JsonObject
        {
            ["day"] = Iso(day),
            ["ohne_produktion"] = ohneProduktion,
            ["bereiche_ohne_rueckmeldung"] = leer,
            ["quelltage"] = new JsonArray(days.Select(d => (JsonNode?)Iso(d)).ToArray()),
            ["werte"] = values,
            ["detail"] = detail
        };
    }

    /// <summary>
    /// Push-Items aus den Werten; Felder mit einem manuell erfassten Wert (Wert da, aber ohne SAP-Quelle) bleiben aus.
    /// <paramref name="existing"/> liefert je Modul den Tagessatz (data, meta) oder null.
    /// </summary>
    public static (JsonArray Items, JsonArray Skipped) BuildItems(JsonObject computed, Func<string, JsonObject?> existing)
    {
        var day = (string)computed["day"]!;
        var items = new JsonArray();
        var skipped = new JsonArray();
        foreach (var (module, node) in (JsonObject)computed["werte"]!)
        {
            var row = existing(module);
            var data = row?["data"] as JsonObject;
            var meta = row?["meta"] as JsonObject;
            var fields = new JsonObject();
            foreach (var (key, value) in (JsonObject)node!)
            {
                var hasValue = data?[key] is not null;
                var fromSap = string.Equals((string?)meta?[key]?["src"], "SAP", StringComparison.OrdinalIgnoreCase);
                if (hasValue && !fromSap)
                {
                    skipped.Add($"{module}.{key}");
                    continue;
                }
                fields[key] = value?.DeepClone();
            }
            if (fields.Count > 0)
                items.Add(new JsonObject { ["module"] = module, ["day"] = day, ["fields"] = fields });
        }
        return (items, skipped);
    }

    private static string Or(string property, IEnumerable<DateOnly> days)
        => "(" + string.Join(" or ", days.Select(d => $"{property} eq '{d:yyyyMMdd}'")) + ")";

    private static void Put(JsonObject values, string module, string field, JsonNode value)
    {
        if (values[module] is not JsonObject m)
            values[module] = m = new JsonObject();
        m[field] = value;
    }

    private static string Iso(DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string Text(JsonElement x, string name)
        => x.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()?.Trim() ?? string.Empty : string.Empty;

    private static int Int(JsonElement x, string name)
        => x.TryGetProperty(name, out var v)
            ? v.ValueKind switch
            {
                JsonValueKind.Number when v.TryGetInt32(out var i) => i,
                JsonValueKind.String when int.TryParse(v.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var s) => s,
                _ => 0
            }
            : 0;
}
