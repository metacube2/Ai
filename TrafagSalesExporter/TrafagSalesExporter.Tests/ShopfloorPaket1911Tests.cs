using System.Text.Json;
using System.Text.Json.Nodes;
using ClosedXML.Excel;
using TrafagSalesExporter.Services.Shopfloor;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Paket Fabio 19.11.2026 (docs/SHOPFLOOR_2026-10-07.md 27): Tagesziel, Offen-Regel, Excel-Import der Rueckstandslisten und
/// Tagesziele, Arbeitskalender CH, Auftragsliste des Vorrats, naechster Vorgang im Forecast.
/// </summary>
public class ShopfloorPaket1911Tests
{
    private static List<JsonElement> Rows(string json) => JsonDocument.Parse(json).RootElement.EnumerateArray().ToList();

    // ---------------------------------------------------------------- Konfiguration
    [Fact]
    public void Abweichung_Braucht_Ein_Tagesziel_Und_Beschriftung_Wieder_Wunschtermin()
    {
        var cfg = JsonNode.Parse(File.ReadAllText(Path.Combine(ShopfloorTestEnv.ShopfloorDir, "shopfloor_config.json")))!;
        foreach (var m in cfg["modules"]!.AsArray().OfType<JsonObject>())
        {
            var key = (string)m["key"]!;
            foreach (var f in (m["fields"] as JsonArray ?? []).OfType<JsonObject>())
            {
                if (key is "seh" or "tr5" or "tx" or "dw" && (string?)f["key"] == "abw")
                    Assert.Equal(["prod", "ziel"], f["need"]!.AsArray().Select(x => (string)x!).ToArray());
                if (key.StartsWith("rs_") && (string?)f["key"] == "wunschtermin")
                    Assert.Equal("Wunschtermin", (string?)f["label"]);
            }
            if (key == "ppd")
                Assert.True((bool)m["tv_from_rs"]!);
        }
    }

    [Theory]
    [InlineData("erledigt", true)]
    [InlineData("Storniert", true)]
    [InlineData("storno", true)]
    [InlineData("Abgesagt", true)]
    [InlineData("FehlMat", false)]
    [InlineData("in Arbeit", false)]
    public void Storniert_Und_Abgesagt_Zaehlen_Geschlossen(string status, bool geschlossen)
    {
        var cfg = ShopfloorConfig.Load(Path.Combine(ShopfloorTestEnv.ShopfloorDir, "shopfloor_config.json"));
        Assert.Equal(geschlossen, cfg.IsClosed(status, cfg.Modules["rs_tx"]));
    }

    // ---------------------------------------------------------------- Kalender
    [Fact]
    public void Kalender_CH_Kennt_Feiertage_Und_Betriebsschliessung()
    {
        Assert.False(ShopfloorKalender.IstArbeitstag(new DateOnly(2026, 12, 28)));   // Sonderregel TFAIN
        Assert.False(ShopfloorKalender.IstArbeitstag(new DateOnly(2026, 5, 14)));    // Auffahrt
        Assert.False(ShopfloorKalender.IstArbeitstag(new DateOnly(2026, 11, 21)));   // Samstag
        Assert.True(ShopfloorKalender.IstArbeitstag(new DateOnly(2026, 11, 19)));
        Assert.True(ShopfloorKalender.IstArbeitstag(new DateOnly(2026, 8, 3)));
        Assert.True(ShopfloorKalender.IstArbeitstag(new DateOnly(2028, 1, 3)));    // ohne Tabelle: Mo bis Fr
        Assert.False(ShopfloorKalender.IstArbeitstag(new DateOnly(2029, 1, 2)));   // ohne Tabelle: 02.01. frei
        // 248 Arbeitstage 2026 und 254 2027 laut TFACS (P76)
        Assert.Equal(248, Enumerable.Range(0, 365).Count(i => ShopfloorKalender.IstArbeitstag(new DateOnly(2026, 1, 1).AddDays(i))));
        Assert.Equal(254, Enumerable.Range(0, 365).Count(i => ShopfloorKalender.IstArbeitstag(new DateOnly(2027, 1, 1).AddDays(i))));
    }

    [Fact]
    public void Plan5_Und_Letzter_Arbeitstag_Ueberspringen_Feiertage()
    {
        // Mi 23.12.2026: 24./25. und 28.-31.12. frei, 01.01. frei -> 5 Arbeitstage bis Fr 08.01.2027
        Assert.Equal(new DateOnly(2027, 1, 8), ShopfloorSapVorrat.Plan5Ende(new DateOnly(2026, 12, 23)));
        // Di 26.05.2026 (Pfingstmontag 25.05. frei): letzter Arbeitstag Fr 22.05.
        Assert.Equal(new DateOnly(2026, 5, 22), ShopfloorSapVerkauf.LetzterArbeitstag(new DateOnly(2026, 5, 26)));
        // Mo 04.01.2027: 01.01. frei, 28.-31.12. frei -> Mi 23.12.2026, Monat Dezember
        Assert.Equal(new DateOnly(2026, 12, 23), ShopfloorSapVerkauf.LetzterArbeitstag(new DateOnly(2027, 1, 4)));
    }

    // ---------------------------------------------------------------- Naechster Vorgang
    private const string VorratJson = """
        [{"Aufnr":"000001279556","Typ":"VF","Werks":"1100","Datum":"20261120","Matnr":"C15610","Dispo":"DS1","Menge":20,"Avo":"X","Vornr":"0800","Arbpl":"DSQ72","Sttxt":"FREI"},
         {"Aufnr":"000001279557","Typ":"VF","Werks":"1100","Datum":"20261125","Matnr":"C15610","Dispo":"DS1","Menge":20,"Avo":"X","Vornr":"0550","Arbpl":"","Sttxt":"FREI"},
         {"Aufnr":"0002526829","Typ":"VP","Werks":"1100","Datum":"20261125","Matnr":"58613","Dispo":"EL2","Menge":45,"Avo":"X","Vornr":"","Arbpl":""},
         {"Aufnr":"000001300000","Typ":"VF","Werks":"1100","Datum":"20261201","Matnr":"123456","Dispo":"EK1","Menge":5,"Avo":"","Vornr":"0010","Arbpl":"EVA72"}]
        """;

    [Fact]
    public void Naechster_Vorgang_Aus_Den_Vorratszeilen_Mit_Avo()
    {
        var nv = ShopfloorSapVorrat.NaechsterVorgang(Rows(VorratJson));
        Assert.Equal("0800 / DSQ72", nv["1279556"]);
        Assert.Equal("0550", nv["1279557"]);
        Assert.False(nv.ContainsKey("2526829"));   // Planauftrag
        Assert.False(nv.ContainsKey("1300000"));   // ohne Avo-Echo kein belegter Vorgang
    }

    [Fact]
    public void Forecast_Haengt_Naechsten_Vorgang_Nur_An_Fertigungsauftraege()
    {
        var nv = new Dictionary<string, string> { ["1279556"] = "0800 / DSQ72", ["2526829"] = "falsch" };
        var rows = ShopfloorSapSync.OrderRows(Rows("""
            [{"Aufnr":"000001279556","Typ":"FE","Werks":"1100","Datum":"20261120","Matnr":"C15610","Dispo":"DS1","Menge":20},
             {"Aufnr":"0002526829","Typ":"PA","Werks":"1100","Datum":"20261121","Matnr":"58613","Dispo":"EL2","Menge":45}]
            """), new DateOnly(2026, 12, 31), nv);
        Assert.Equal("0800 / DSQ72", (string?)rows[0]!["naechster_vorgang"]);
        Assert.Null(rows[1]!["naechster_vorgang"]);

        var ohne = ShopfloorSapSync.OrderRows(Rows("""[{"Aufnr":"1","Typ":"FE","Datum":"20261120","Matnr":"1","Dispo":"DS1","Menge":1}]"""),
            new DateOnly(2026, 12, 31));
        Assert.False(((JsonObject)ohne[0]!).ContainsKey("naechster_vorgang"));
    }

    [Fact]
    public void Forecast_Speichert_Naechsten_Vorgang()
    {
        using var env = new ShopfloorTestEnv();
        var monday = "2026-11-16";
        env.Store.ApplyPush(new ShopfloorPush
        {
            Items = [new JsonObject
            {
                ["module"] = "planauftraege",
                ["rows"] = ShopfloorTestEnv.Arr("""[{"day":"2026-11-18","auftrag":"1279556","material":"C15610","menge":20,"disponent":"DS1","naechster_vorgang":"0800 / DSQ72"}]""")
            }]
        }, "SAP");
        var fc = (JsonObject)env.Store.GetForecast(monday);
        var o = fc["orders"]!.AsArray().OfType<JsonObject>().Single();
        Assert.Equal("0800 / DSQ72", (string?)o["naechster_vorgang"]);
    }

    // ---------------------------------------------------------------- Auftragsliste Vorrat
    [Fact]
    public void Auftragsliste_Zeigt_Fenster_Und_Ausschluesse()
    {
        var layout = new ShopfloorLayoutFilter([], ["M*"]);
        var liste = ShopfloorSapVorrat.Auftraege(new DateOnly(2026, 11, 19), Rows("""
            [{"Aufnr":"000001","Typ":"VF","Datum":"20261120","Matnr":"64494","Dispo":"EL2","Menge":10,"Avo":"X","Vornr":"0600","Arbpl":"EL106","Sttxt":"FREI"},
             {"Aufnr":"000002","Typ":"VF","Datum":"20261120","Matnr":"64494","Dispo":"EL2","Menge":7,"Avo":"X","Vornr":"0900","Arbpl":"MLE08","Sttxt":"FREI"},
             {"Aufnr":"000003","Typ":"VP","Datum":"20270301","Matnr":"C123","Dispo":"EK1","Menge":3,"Avo":"X"},
             {"Aufnr":"000004","Typ":"VP","Datum":"20261130","Matnr":"1","Dispo":"XX9","Menge":3}]
            """), ["TR5"], layout);
        Assert.Equal(3, liste.Count);   // XX9 ohne Abteilung
        var a = liste[0];
        Assert.Equal("TX", (string?)a["abteilung"]);
        Assert.True((bool)a["zaehlt"]!);
        Assert.True((bool)a["bis_1m"]!);
        Assert.True((bool)a["bis_kw"]!);
        Assert.Equal("Layoutfilter ZC22", (string?)liste[1]["ausgeschlossen"]);
        Assert.False((bool)liste[1]["bis_1m"]!);
        Assert.Equal("Materialfilter 1-99999", (string?)liste[2]["ausgeschlossen"]);

        var arr = new JsonArray(liste.Select(x => (JsonNode?)x).ToArray());
        var csv = System.Text.Encoding.UTF8.GetString(ShopfloorSapVorrat.AuftraegeCsv("2026-11-19", arr)).TrimStart('﻿');
        var zeilen = csv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(4, zeilen.Length);
        Assert.StartsWith("stichtag;abteilung;typ;auftrag", zeilen[0]);
        Assert.Contains("2026-11-19;TX;Fertigungsauftrag;1;64494;EL2;2026-11-20;10;FREI;0600;EL106;x;", zeilen[1]);
    }

    [Fact]
    public void Auftragsliste_Erster_Lauf_Des_Tages_Bleibt()
    {
        using var env = new ShopfloorTestEnv();
        var eins = new List<JsonObject> { new() { ["auftrag"] = "1" } };
        var zwei = new List<JsonObject> { new() { ["auftrag"] = "2" }, new() { ["auftrag"] = "3" } };
        Assert.True((bool)env.Store.SaveVorratAuftraege("2026-11-19", eins, false)["gespeichert"]!);
        Assert.False((bool)env.Store.SaveVorratAuftraege("2026-11-19", zwei, false)["gespeichert"]!);
        Assert.Single(env.Store.GetVorratAuftraege("2026-11-19")!);
        Assert.True((bool)env.Store.SaveVorratAuftraege("2026-11-19", zwei, true)["gespeichert"]!);
        Assert.Equal(2, env.Store.GetVorratAuftraege("2026-11-19")!.Count);
        Assert.Null(env.Store.GetVorratAuftraege("2026-11-18"));
        Assert.Single(env.Store.VorratAuftraegeTage_());
    }

    // ---------------------------------------------------------------- Excel-Import
    private static MemoryStream Excel(Action<XLWorkbook> fill)
    {
        using var wb = new XLWorkbook();
        fill(wb);
        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    private static void RsBlatt(XLWorkbook wb, string name, params (string Nr, DateTime Datum, string Status, DateTime? Abschluss, double Menge)[] zeilen)
    {
        var ws = wb.AddWorksheet(name);
        ws.Cell("A4").Value = "Rückstands-Nr.:";
        var r = 5;
        foreach (var z in zeilen)
        {
            ws.Cell(r, "A").Value = z.Nr;
            ws.Cell(r, "B").Value = z.Datum;
            ws.Cell(r, "F").Value = "EL1";
            ws.Cell(r, "G").Value = 48035;
            ws.Cell(r, "O").Value = 10;
            ws.Cell(r, "T").Value = z.Menge;
            ws.Cell(r, "U").Value = 1;
            ws.Cell(r, "AR").Value = "x";
            ws.Cell(r, "AT").Value = z.Status;
            if (z.Abschluss is { } a) ws.Cell(r, "AU").Value = a;
            r++;
        }
        ws.Cell(r, "A").Value = "RS-TX-26-999";   // vorbelegte leere Nummer: kein Eintrag
    }

    private static void ZielBlatt(XLWorkbook wb, string name, string zielCol, params (DateTime Tag, double? Ziel)[] tage)
    {
        var ws = wb.AddWorksheet(name);
        ws.Cell("C6").Value = "Datum:";
        var r = 7;
        foreach (var (tag, ziel) in tage)
        {
            ws.Cell(r, "C").Value = tag;
            if (ziel is { } z) ws.Cell(r, zielCol).Value = z;
            r++;
        }
    }

    private static MemoryStream TestExcel() => Excel(wb =>
    {
        RsBlatt(wb, "3.1 RS");
        RsBlatt(wb, "4.1 RS");
        RsBlatt(wb, "5.1 RS",
            ("RS-TX-26-001", new DateTime(2025, 12, 16), "erledigt", new DateTime(2026, 1, 9), 1),   // Status gleich, Menge gleich
            ("RS-TX-26-500", new DateTime(2026, 11, 17), "FehlMat", new DateTime(2026, 11, 18), 30)); // neu, Widerspruch
        RsBlatt(wb, "6.1 RS");
        ZielBlatt(wb, "4. TR5", "L", (new DateTime(2026, 11, 16), 800), (new DateTime(2026, 11, 17), 800), (new DateTime(2026, 11, 18), null));
        ZielBlatt(wb, "5. TX", "M", (new DateTime(2026, 11, 16), 1300));
        ZielBlatt(wb, "6. DW", "L", (new DateTime(2026, 11, 16), 800));
    });

    [Fact]
    public void Excel_Liest_Rueckstandslisten_Und_Ziele_Wie_Der_Import_Vom_05_10()
    {
        var cfg = ShopfloorConfig.Load(Path.Combine(ShopfloorTestEnv.ShopfloorDir, "shopfloor_config.json"));
        using var xlsx = TestExcel();
        var stand = ShopfloorExcelImport.Lesen(xlsx, cfg);

        var tx = stand.Listen["rs_tx"];
        Assert.Equal(2, tx.Count);   // RS-TX-26-999 ohne Eintrag faellt weg
        var d = tx[1].Data;
        Assert.Equal("RS-TX-26-500", tx[1].Nr);
        Assert.Equal("2026-11-17", (string?)d["datum"]);
        Assert.Equal("48035", (string?)d["vknr"]);   // Text wie im Seed
        Assert.Equal("10", (string?)d["ka_pos"]);
        Assert.Equal(30, SfJson.AsDouble(d["menge_rs"]));
        Assert.Equal(["Einkauf"], d["verursacher"]!.AsArray().Select(x => (string)x!).ToArray());
        Assert.Equal(["Sonstiges"], d["komponenten"]!.AsArray().Select(x => (string)x!).ToArray());
        Assert.Equal("2026-11-18", (string?)d["abschluss"]);

        Assert.Equal(new[] { "dw", "tr5", "tx" }, stand.Ziele.Keys.OrderBy(k => k).ToArray());   // SEH-Ziel kommt aus SAP
        Assert.Equal(2, stand.Ziele["tr5"].Count);
        Assert.Equal(1300, stand.Ziele["tx"]["2026-11-16"]);
    }

    [Fact]
    public void Excel_Vorschau_Aendert_Nichts_Uebernahme_Sichert_Und_Loescht_Nichts()
    {
        using var env = new ShopfloorTestEnv(withSeed: true);
        // Handwert im Board: Excel-Zelle leer -> bleibt
        env.Write(con => env.Store.SaveRecord(con, "rs_tx", "RS-TX-26-001", new JsonObject { ["ticket"] = "T-1" }, "tl", "manuell"));
        env.Write(con => env.Store.SaveDaily(con, "tr5", "2026-11-17", new JsonObject { ["ziel"] = 700 }, "tl", "manuell"));
        var vorher = env.Store.GetRecords("rs_tx").Count;

        using (var x1 = TestExcel())
        {
            var vorschau = ShopfloorExcelImport.Ausfuehren(env.Store, x1, "fpa", false, Path.Combine(env.Dir, "backups"), "test.xlsx");
            var tx = vorschau["listen"]!.AsArray().OfType<JsonObject>().Single(l => (string?)l["module"] == "rs_tx");
            Assert.Equal(1, (int)tx["neu"]!);
            Assert.Equal(vorher - 1, (int)tx["nur_im_board"]!);
            Assert.Equal(1, (int)vorschau["widersprueche"]!);
            var tr5 = vorschau["ziele"]!.AsArray().OfType<JsonObject>().Single(l => (string?)l["module"] == "tr5");
            Assert.Equal(1, (int)tr5["geaendert"]!);   // 700 -> 800
            Assert.Null(vorschau["sicherung"]);
        }
        Assert.Equal(vorher, env.Store.GetRecords("rs_tx").Count);   // Vorschau schreibt nichts
        Assert.False(Directory.Exists(Path.Combine(env.Dir, "backups")));

        using (var x2 = TestExcel())
        {
            var ergebnis = ShopfloorExcelImport.Ausfuehren(env.Store, x2, "fpa", true, Path.Combine(env.Dir, "backups"), "test.xlsx");
            Assert.True(File.Exists((string)ergebnis["sicherung"]!));
        }
        var recs = env.Store.GetRecords("rs_tx").OfType<JsonObject>().ToList();
        Assert.Equal(vorher + 1, recs.Count);   // nichts geloescht
        var alt = recs.Single(r => (string?)r["nr"] == "RS-TX-26-001");
        Assert.Equal("T-1", (string?)alt["data"]!["ticket"]);   // leere Excel-Zelle leert nichts
        var neu = recs.Single(r => (string?)r["nr"] == "RS-TX-26-500");
        Assert.Equal("fpa", (string?)neu["updated_by"]);
        var daily = env.Store.GetDaily("tr5", "2026-11-16", "2026-11-17")["tr5"]!;
        Assert.Equal(800, SfJson.AsDouble(daily["2026-11-17"]!["data"]!["ziel"]));
        Assert.Equal(800, SfJson.AsDouble(daily["2026-11-16"]!["data"]!["ziel"]));
    }

    [Fact]
    public void Excel_Ohne_Rs_Blatt_Meldet_Fehler()
    {
        var cfg = ShopfloorConfig.Load(Path.Combine(ShopfloorTestEnv.ShopfloorDir, "shopfloor_config.json"));
        using var xlsx = Excel(wb => wb.AddWorksheet("anderes"));
        var ex = Assert.Throws<ShopfloorException>(() => ShopfloorExcelImport.Lesen(xlsx, cfg));
        Assert.Contains("3.1 RS", ex.Message);
    }
}
