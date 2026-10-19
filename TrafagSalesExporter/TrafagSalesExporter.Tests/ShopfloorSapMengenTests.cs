using System.Text.Json;
using System.Text.Json.Nodes;
using TrafagSalesExporter.Services.Shopfloor;

namespace TrafagSalesExporter.Tests;

public class ShopfloorSapMengenTests
{
    [Fact]
    public void Abgleich_Schreibt_Heute_Und_Drei_Tage_Davor_Montag_Holt_Das_Wochenende()
    {
        // Seit 2026-10-19 Produktionstag statt Abfragetag: Montag 19.10. schreibt Fr 16., Sa 17., So 18. und Mo 19.
        Assert.Equal([new DateOnly(2026, 10, 16), new DateOnly(2026, 10, 17), new DateOnly(2026, 10, 18), new DateOnly(2026, 10, 19)],
            ShopfloorSapMengen.SyncDays(new DateOnly(2026, 10, 19)));
    }

    [Fact]
    public void Fpy_Woche_Ist_Die_KW_Des_Tages_Bis_Zu_Diesem_Tag()
    {
        var sonntag = ShopfloorSapMengen.FpyWeek(new DateOnly(2026, 10, 11));   // KW 41 ganz
        Assert.Equal(new DateOnly(2026, 10, 5), sonntag[0]);
        Assert.Equal(new DateOnly(2026, 10, 11), sonntag[^1]);
        Assert.Equal(7, sonntag.Count);
        var dienstag = ShopfloorSapMengen.FpyWeek(new DateOnly(2026, 10, 13)); // Mo und Di
        Assert.Equal([new DateOnly(2026, 10, 12), new DateOnly(2026, 10, 13)], dienstag);
    }

    [Fact]
    public async Task Werte_Je_Feld_Aus_Den_Sets_Mit_Den_Filtern_Wie_ZC27()
    {
        var calls = new List<(string Set, string Filter)>();
        Task<List<JsonElement>> Read(string set, string filter)
        {
            calls.Add((set, filter));
            string json;
            if (set == ShopfloorSapMengen.RmSet && filter.Contains("'MLE01'"))
                json = """[{"Arbpl":"MLE01","Lmnga":673,"Anzahl":30}]""";
            else if (set == ShopfloorSapMengen.RmSet && filter.Contains("'MLE08'"))
                json = """[{"Arbpl":"MLE08","Lmnga":465,"Anzahl":12},{"Arbpl":"MLE09","Lmnga":600,"Anzahl":14}]""";
            else if (set == ShopfloorSapMengen.ProdSet && filter.Contains("'20261012'"))   // FPY-Woche (Mo + Di)
                json = """[{"Dispo":"SE1","Gamng":1000,"Wemng":990,"Anzahl":4},{"Dispo":"SB2","Gamng":200,"Wemng":200,"Anzahl":1}]""";
            else if (set == ShopfloorSapMengen.ProdSet)
                json = """[{"Dispo":"SE1","Gamng":500,"Wemng":480,"Anzahl":2}]""";
            else
                json = "[]";
            return Task.FromResult(JsonDocument.Parse(json).RootElement.EnumerateArray().Select(e => e.Clone()).ToList());
        }

        var r = await ShopfloorSapMengen.ComputeAsync(new DateOnly(2026, 10, 13), "1100", Read);   // Produktionstag 13.10.

        var log = r["werte"]!["logistik"]!;
        Assert.Equal(673, (int)log["tr5_mle01"]!);
        Assert.Equal(1065, (int)log["tx_mle08"]!);
        Assert.Equal(0, (int)log["tx_mlm01"]!);
        Assert.Equal(0, (int)r["werte"]!["tr5"]!["prod"]!);
        Assert.Equal(0, (int)r["werte"]!["dw"]!["prod"]!);
        Assert.Equal("2026-10-13", (string)r["day"]!);
        Assert.Equal(480, (int)r["werte"]!["seh"]!["prod"]!);
        Assert.Equal(99.2, (double)r["werte"]!["seh"]!["fpy"]!, 3);   // 1190 / 1200

        // MLE08 und MLE09 sind ein gemeinsamer ZC27-Lauf, die uebrigen Arbeitsplaetze einzeln
        Assert.Contains(calls, c => c.Filter == "Werks eq '1100' and (Arbpl eq 'MLE08' or Arbpl eq 'MLE09') and (Ersda eq '20261013')");
        Assert.Contains(calls, c => c.Filter == "Werks eq '1100' and (Arbpl eq 'MLE01') and (Ersda eq '20261013')");
        // DW wie Logistik, Arbeitsplatz DL790 (Mail Fabio 2026-10-17)
        Assert.Contains(calls, c => c.Filter == "Werks eq '1100' and (Arbpl eq 'DL790') and (Ersda eq '20261013')");
        // TR5 wie ZC27-Variante /PPA EXX72: elf Arbeitsplaetze, Typencode 8*
        var tr5 = Assert.Single(calls, c => c.Filter.Contains("'EDF72'"));
        Assert.EndsWith("and Typcd eq '8*'", tr5.Filter);
        Assert.Equal(11, tr5.Filter.Split("Arbpl eq").Length - 1);
        Assert.Contains(calls, c => c.Set == ShopfloorSapMengen.ProdSet && c.Filter == "Werks eq '1100' and Dispo eq 'S*' and (Datum eq '20261013')");
        Assert.Contains(calls, c => c.Set == ShopfloorSapMengen.ProdSet && c.Filter.EndsWith("(Datum eq '20261012' or Datum eq '20261013')"));
    }

    [Fact]
    public async Task Tag_Ohne_Rueckmeldung_Schreibt_Keine_Nullen_Sondern_Entfernt_SAP_Werte()
    {
        Task<List<JsonElement>> Leer(string set, string filter) => Task.FromResult(new List<JsonElement>());
        var r = await ShopfloorSapMengen.ComputeAsync(new DateOnly(2026, 10, 17), "1100", Leer);   // Samstag
        Assert.True((bool)r["ohne_produktion"]!);
        Assert.Null(r["werte"]!["logistik"]!["tr5_mle01"]);

        using var env = new ShopfloorTestEnv();
        var alt = JsonNode.Parse("""{"day":"2026-10-17","werte":{"logistik":{"tr5_mle01":0},"dw":{"prod":0}}}""")!.AsObject();
        env.Store.ApplyPush(new ShopfloorPush { Items = ShopfloorSapMengen.BuildItems(alt, _ => null).Items }, "SAP");
        JsonObject? Existing(string m) => env.Store.GetDaily(m, "2026-10-17", "2026-10-17")[m]?["2026-10-17"] as JsonObject;
        env.Store.ApplyPush(new ShopfloorPush { Items = ShopfloorSapMengen.BuildItems(r, Existing).Items }, "SAP");
        Assert.Null(Existing("logistik"));   // Tagessatz mit den alten Nullen ist weg
        Assert.Null(Existing("dw"));
    }

    [Fact]
    public void Manuelle_Werte_Werden_Nicht_Ueberschrieben()
    {
        var computed = JsonNode.Parse("""
            {"day":"2026-10-14","werte":{"logistik":{"tr5_mle01":673,"tr5_mle02":526,"tx_mlm01":193},"seh":{"prod":480}}}
            """)!.AsObject();
        JsonObject? Existing(string module) => module == "logistik"
            ? JsonNode.Parse("""{"data":{"tr5_mle01":600,"tr5_mle02":500},"meta":{"tr5_mle02":{"src":"SAP"}}}""")!.AsObject()
            : null;

        var (items, skipped) = ShopfloorSapMengen.BuildItems(computed, Existing);

        Assert.Equal(["logistik.tr5_mle01"], skipped.Select(x => (string)x!));
        var logistik = items.Single(i => (string)i!["module"]! == "logistik")!["fields"]!.AsObject();
        Assert.Null(logistik["tr5_mle01"]);              // von Hand erfasst: bleibt
        Assert.Equal(526, (int)logistik["tr5_mle02"]!);   // frueher aus SAP: wird aktualisiert
        Assert.Equal(193, (int)logistik["tx_mlm01"]!);    // leer: wird gefuellt
        Assert.Equal("2026-10-14", (string)items.Single(i => (string)i!["module"]! == "seh")!["day"]!);
    }

    [Fact]
    public void Push_Mit_Den_Feldern_Der_Mengen_Wird_Gespeichert()
    {
        using var env = new ShopfloorTestEnv();
        var computed = JsonNode.Parse("""
            {"day":"2026-10-14","werte":{"logistik":{"tr5_mle01":673,"tr5_mle02":526,"dw_mle04":542,"tx_mlm01":193,"tx_mle08":1065},"tr5":{"prod":836},"seh":{"prod":480,"fpy":99.2}}}
            """)!.AsObject();
        var (items, _) = ShopfloorSapMengen.BuildItems(computed, _ => null);
        var res = env.Store.ApplyPush(new ShopfloorPush { Items = items }, "SAP");
        Assert.All(res, x => Assert.True((bool)x!["ok"]!));
        var log = env.Store.GetDaily("logistik", "2026-10-14", "2026-10-14")["logistik"]!["2026-10-14"]!;
        Assert.Equal(1065, (double)log["data"]!["tx_mle08"]!, 3);
        Assert.Equal("SAP", (string)log["meta"]!["tx_mle08"]!["src"]!);
        var seh = env.Store.GetDaily("seh", "2026-10-14", "2026-10-14")["seh"]!["2026-10-14"]!["data"]!;
        Assert.Equal(99.2, (double)seh["fpy"]!, 3);
    }
}
