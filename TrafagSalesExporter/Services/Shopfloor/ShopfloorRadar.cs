using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.Data.Sqlite;

namespace TrafagSalesExporter.Services.Shopfloor;

/// <summary>
/// Radar Kundenverfuegbarkeit: Verzug durch Einkauf und GFS je Sparte (Paket Fabio 24.11.2026, Punkt 5, Vorlage Marco Widmer).
/// <list type="bullet">
/// <item>Prod. Menge YTD = Summe der Tagesproduktion (<c>daily.prod</c>) vom 01.01. bis zum Vortag des Stichtags. Marcos Excel-Spalte
/// „Produzierte Jahresmenge kumuliert“ am Vortag ist dieselbe Summe (am 29.10.2026 nachgerechnet: alle vier Sparten = Zeile 28.10.).</item>
/// <item>Verzug Einkauf / GFS YTD = Summe „Menge im Rueckstand“ der RS-Zeilen mit Eintragsdatum im Jahr bis zum Stichtag, mit Ursache
/// „Einkauf“ bzw. „GFS DE“ in „Verursacher / beteiligt“; abgeschlossene Zeilen zaehlen mit.</item>
/// <item>% = (Einkauf + GFS) ÷ Prod. Menge YTD; eine Zeile mit beiden Ursachen steht in beiden Spalten, zaehlt im Total aber nur einmal.</item>
/// </list>
/// </summary>
public static class ShopfloorRadar
{
    public static readonly (string Sparte, string Label, string Prod, string Rs)[] Sparten =
    [
        ("seh", "Sensoren (SEH)", "seh", "rs_seh"),
        ("tr5", "TR5", "tr5", "rs_tr5"),
        ("tx", "TX", "tx", "rs_tx"),
        ("dw", "DW", "dw", "rs_dw")
    ];

    public const string Einkauf = "Einkauf";
    public const string Gfs = "GFS DE";

    public static JsonObject Compute(SqliteConnection con, string stichtag)
    {
        var tag = ShopfloorZd05.ParseDay(stichtag);
        var jan1 = $"{tag.Year}-01-01";
        var vortag = tag.AddDays(-1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var rows = new JsonArray();
        double tProd = 0, tEk = 0, tGfs = 0, tVz = 0;
        int tZeilen = 0, tBeide = 0;
        foreach (var (sparte, label, prodModule, rsModule) in Sparten)
        {
            double prod = 0;
            var tage = 0;
            foreach (var r in ShopfloorDb.Query(con, "SELECT data FROM daily WHERE module=? AND day>=? AND day<=?", prodModule, jan1, vortag))
            {
                var p = SfJson.AsDouble(SfJson.Get(SfJson.ParseObject(r.Str("data")), "prod"));
                if (p is null) continue;
                prod += p.Value;
                tage++;
            }
            var (ek, gfs, verzug, zeilen, beide) = Verzug(
                ShopfloorDb.Query(con, "SELECT data FROM records WHERE module=? AND deleted=0", rsModule)
                    .Select(r => SfJson.ParseObject(r.Str("data"))),
                jan1, stichtag);
            rows.Add(Row(sparte, label, prod, ek, gfs, verzug, zeilen, beide, tage));
            tProd += prod; tEk += ek; tGfs += gfs; tVz += verzug; tZeilen += zeilen; tBeide += beide;
        }
        rows.Add(Row("total", "Total", tProd, tEk, tGfs, tVz, tZeilen, tBeide, null));
        var stand = ShopfloorDb.QueryOne(con,
            "SELECT max(updated_at) u FROM records WHERE module IN ('rs_seh','rs_tr5','rs_tx','rs_dw') AND deleted=0")?.Str("u");
        return new JsonObject
        {
            ["stichtag"] = stichtag,
            ["prod_bis"] = vortag,
            ["rs_stand"] = stand,
            ["rows"] = rows
        };
    }

    /// <summary>Summen der RS-Zeilen mit Eintragsdatum <paramref name="von"/> bis <paramref name="bis"/> (je einschliesslich).</summary>
    public static (double Einkauf, double Gfs, double Verzug, int Zeilen, int Beide) Verzug(IEnumerable<JsonObject> records, string von, string bis)
    {
        double ek = 0, gfs = 0, verzug = 0;
        int zeilen = 0, beide = 0;
        foreach (var d in records)
        {
            var datum = SfJson.PyStr(SfJson.Get(d, "datum"));
            if (datum.Length < 10) continue;
            datum = datum[..10];
            if (string.CompareOrdinal(datum, von) < 0 || string.CompareOrdinal(datum, bis) > 0) continue;
            var v = Verursacher(SfJson.Get(d, "verursacher"));
            var isEk = v.Contains(Einkauf);
            var isGfs = v.Contains(Gfs);
            if (!isEk && !isGfs) continue;
            var menge = SfJson.AsDouble(SfJson.Get(d, "menge_rs")) ?? 0;
            if (isEk) ek += menge;
            if (isGfs) gfs += menge;
            if (isEk && isGfs) beide++;
            verzug += menge;
            zeilen++;
        }
        return (ek, gfs, verzug, zeilen, beide);
    }

    private static HashSet<string> Verursacher(JsonNode? n)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (n is JsonArray a)
            foreach (var x in a)
                set.Add(SfJson.PyStr(x).Trim());
        else if (n is not null)
            foreach (var s in SfJson.PyStr(n).Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                set.Add(s);
        return set;
    }

    private static JsonObject Row(string key, string label, double prod, double ek, double gfs, double verzug, int zeilen, int beide, int? tage)
        => new()
        {
            ["key"] = key,
            ["label"] = label,
            ["prod_ytd"] = prod,
            ["prod_tage"] = tage,
            ["verzug_einkauf"] = ek,
            ["verzug_gfs"] = gfs,
            ["verzug_total"] = verzug,
            ["zeilen"] = zeilen,
            ["zeilen_beide"] = beide,
            ["prozent"] = prod > 0 ? Math.Round(verzug / prod * 100, 2) : null
        };
}
