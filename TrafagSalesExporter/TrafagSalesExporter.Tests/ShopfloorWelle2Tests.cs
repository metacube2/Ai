using System.Text.Json;
using System.Text.Json.Nodes;
using TrafagSalesExporter.Services.Shopfloor;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Welle 2 / Punkt 9 (Fabio 24.11. und 26.11.2026): ShopKdTerminSet und ShopMatBezugSet im Board. Die Zeilen sind erfunden (keine Kunden- oder
/// Auftragsdaten), im Format der Antwort aus T76 (Vbeln/Aufnr/Matnr ohne Nullen, Posnr sechsstellig, Datum JJJJMMTT).
/// </summary>
public class ShopfloorWelle2Tests
{
    private static List<JsonElement> Rows(string json) => JsonDocument.Parse(json).RootElement.EnumerateArray().Select(e => e.Clone()).ToList();

    private const string Beispiel = """
    [
      {"Vbeln":"900001","Posnr":"000010","Typ":"KD","Aufnr":"","Werks":"1100","Matnr":"12345","Zztyp":"NAH 8254","Kwmeng":90,"Wunsch":"20261201","Bereit":"20261210","Liefer":"20261214","Bmeng":90,"BmengLetzte":30,"Sobkz":"E"},
      {"Vbeln":"900001","Posnr":"000010","Typ":"FA","Aufnr":"1000002","Werks":"1100","Menge":30,"Ende":"20261204","Verfueg":"20261208","Elikz":"","Dnrel":""},
      {"Vbeln":"900001","Posnr":"000010","Typ":"FA","Aufnr":"1000001","Werks":"1100","Menge":30,"Ende":"20261130","Verfueg":"20261202","Elikz":"X","Dnrel":""},
      {"Vbeln":"900001","Posnr":"000010","Typ":"FA","Aufnr":"1000003","Werks":"1100","Menge":10,"Ende":"20261130","Verfueg":"20261202","Elikz":"","Dnrel":"X"},
      {"Vbeln":"900001","Posnr":"000010","Typ":"PA","Aufnr":"2000001","Werks":"1100","Menge":30,"Ende":"20261207","Verfueg":"20261209","Beskz":"E"},
      {"Vbeln":"900001","Posnr":"000010","Typ":"PA","Aufnr":"2000002","Werks":"1100","Menge":30,"Ende":"20261207","Verfueg":"20261209","Beskz":"F"},
      {"Vbeln":"900001","Posnr":"000020","Typ":"KD","Aufnr":"","Werks":"1100","Matnr":"55555","Zztyp":"ECT 1","Kwmeng":5,"Wunsch":"20261201","Bereit":"","Liefer":"","Bmeng":0,"BmengLetzte":0,"Sobkz":""},
      {"Vbeln":"900001","Posnr":"000020","Typ":"FA","Aufnr":"1000009","Werks":"1100","Menge":5,"Ende":"20261204","Verfueg":"20261208","Elikz":"","Dnrel":""}
    ]
    """;

    private static Dictionary<(string, string), ShopfloorSapKdTermin.Position> Pos() => ShopfloorSapKdTermin.Parse(Rows(Beispiel), ["F"]);

    [Theory]
    [InlineData("0000900001", "900001")]
    [InlineData(" 900001 ", "900001")]
    [InlineData("", "")]
    public void Kundenauftrag_Wird_Ohne_Fuehrende_Nullen_Verglichen(string ein, string aus) => Assert.Equal(aus, ShopfloorSapKdTermin.NormVbeln(ein));

    [Theory]
    [InlineData("000010", "10")]
    [InlineData("10", "10")]
    [InlineData("10.0", "10")]
    [InlineData("x", "")]
    public void Position_Wird_Als_Zahl_Verglichen(string ein, string aus) => Assert.Equal(aus, ShopfloorSapKdTermin.NormPosnr(ein));

    [Fact]
    public void Parse_Nimmt_Nur_Offene_FA_Und_Planauftraege_Ohne_Fremdbeschaffung()
    {
        var p = Pos()[("900001", "10")];
        Assert.True(p.Einzelfertigung);
        Assert.Equal(new[] { "1000002" }, p.Fa.Select(a => a.Aufnr));             // ELIKZ X und DNREL X fallen weg
        Assert.Equal(new[] { "2000001" }, p.Pa.Select(a => a.Aufnr));             // BESKZ F faellt weg (E2)
        Assert.Equal("2026-12-14", p.Liefer);
        Assert.Equal(90, p.Bmeng);
        var ohneAusschluss = ShopfloorSapKdTermin.Parse(Rows(Beispiel), []);
        Assert.Equal(2, ohneAusschluss[("900001", "10")].Pa.Count);               // Ausschluss ist Konfiguration
    }

    [Fact]
    public void Tvav_Nachschlagen_Fuellt_Alle_Felder_Und_FA_PA_Nur_Bei_Einzelfertigung()
    {
        var r = ShopfloorSapKdTermin.Nachschlagen("tvav", "0000900001", "10", Pos(), null);
        Assert.Equal("ok", (string)r["status"]!);
        var f = r["felder"]!.AsObject();
        Assert.Equal("12345", (string)f["vknr"]!);
        Assert.Equal("NAH 8254", (string)f["typencode"]!);
        Assert.Equal(90, (int)f["menge"]!);
        Assert.Equal("2026-12-14", (string)f["termin_sap"]!);
        Assert.Equal("1000002", (string)f["fa"]!);
        Assert.Equal("2000001", (string)f["pl_auf"]!);

        var lager = ShopfloorSapKdTermin.Nachschlagen("tvav", "900001", "000020", Pos(), null)["felder"]!.AsObject();
        Assert.False(lager.ContainsKey("fa"));                                    // Lagerfertigung: FA/PA bleiben Handeingabe
        Assert.False(lager.ContainsKey("pl_auf"));
        Assert.Null(lager["termin_sap"]);
    }

    [Fact]
    public void Angebote_Ueberschreiben_Die_Handfelder_Des_Verkaufs_Nicht()
    {
        var aktuell = new JsonObject { ["vknr"] = "VK-EIGEN", ["menge"] = 100 };
        var f = ShopfloorSapKdTermin.Nachschlagen("angebote", "900001", "10", Pos(), aktuell)["felder"]!.AsObject();
        Assert.False(f.ContainsKey("vknr"));
        Assert.False(f.ContainsKey("menge"));
        Assert.Equal("NAH 8254", (string)f["typencode"]!);                       // leer -> gefuellt
        Assert.Equal("1000002", (string)f["fa"]!);
    }

    [Fact]
    public void Nicht_Gefunden_Unterscheidet_Auftrag_Und_Position()
    {
        Assert.Equal("nicht_gefunden", (string)ShopfloorSapKdTermin.Nachschlagen("tvav", "123", "10", Pos(), null)["status"]!);
        Assert.Contains("Kundenauftrag 123", (string)ShopfloorSapKdTermin.Nachschlagen("tvav", "123", "10", Pos(), null)["meldung"]!);
        Assert.Contains("Position 30", (string)ShopfloorSapKdTermin.Nachschlagen("tvav", "900001", "30", Pos(), null)["meldung"]!);
    }

    [Fact]
    public void Rueckstandsliste_Bekommt_Termine_Und_Auftraege_In_Eigenen_Feldern()
    {
        var f = ShopfloorSapKdTermin.Felder("rs_tr5", Pos()[("900001", "10")], new JsonObject(), true);
        Assert.Equal("2026-12-01", (string)f["wunsch_sap"]!);
        Assert.Equal("2026-12-10", (string)f["bereit_sap"]!);
        Assert.Equal("2026-12-14", (string)f["liefer_sap"]!);
        Assert.Equal("FA 1000002 (30 Stk.): Ende 04.12.2026, verfügbar 08.12.2026\nPA 2000001 (30 Stk.): Ende 07.12.2026, verfügbar 09.12.2026",
            (string)f["auftraege_sap"]!);
        Assert.False(f.ContainsKey("wunschtermin"));                              // Excel-Felder bleiben dem Import
        Assert.False(f.ContainsKey("fa"));
    }

    [Fact]
    public void Abgleich_Schreibt_Nur_Offene_Eintraege_Mit_Treffer_Und_Nur_Aenderungen()
    {
        var cfg = ShopfloorConfig.Load(Path.Combine(ShopfloorTestEnv.ShopfloorDir, "shopfloor_config.json"));
        var eintraege = new List<(string, string, JsonObject)>
        {
            ("tvav", "TV-1", new JsonObject { ["ka_nr"] = "900001", ["ka_pos"] = "10", ["status"] = "offen", ["vknr"] = "HAND", ["fa"] = "alt" }),
            ("tvav", "TV-2", new JsonObject { ["ka_nr"] = "900001", ["ka_pos"] = "10", ["status"] = "erledigt" }),
            ("tvav", "TV-3", new JsonObject { ["ka_nr"] = "777", ["ka_pos"] = "10" }),                       // Werksmodus ohne Treffer: unangetastet
            ("tvav", "TV-4", new JsonObject { ["ka_nr"] = "900001", ["ka_pos"] = "20", ["fa"] = "HANDFA", ["sobkz"] = null }),
            ("rs_tx", "RS-1", new JsonObject { ["ka_nr"] = "900001", ["ka_pos"] = "000010" })
        };
        var (items, geprueft, treffer) = ShopfloorSapKdTermin.Abgleich(eintraege, cfg, Pos());
        Assert.Equal(4, geprueft);
        Assert.Equal(3, treffer);
        var tv1 = items.Single(i => (string)i["nr"]! == "TV-1")["fields"]!.AsObject();
        Assert.False(tv1.ContainsKey("vknr"));                                    // im Lauf nur, was bei jedem Lauf neu gilt
        Assert.Equal("1000002", (string)tv1["fa"]!);
        Assert.Equal("E", (string)tv1["sobkz"]!);
        Assert.DoesNotContain(items, i => (string)i["nr"]! == "TV-4");            // Lagerfertigung ohne Termin: nichts zu aendern
        Assert.Contains(items, i => (string)i["nr"]! == "RS-1");
    }

    [Fact]
    public void Nenner_Wochen_Nur_Abgeschlossen_Und_Die_Letzte_Immer()
    {
        var heute = new DateOnly(2026, 1, 21);   // Mittwoch KW 4
        var wochen = ShopfloorSapKdTermin.NennerWochen(heute, new HashSet<string> { "2026-W02" });
        Assert.Equal(new[] { new DateOnly(2025, 12, 29), new DateOnly(2026, 1, 12) }, wochen);   // KW 1 fehlt, KW 2 vorhanden, KW 3 immer, KW 4 laeuft
        Assert.Equal("Werks eq '1100' and Von eq '20260112' and Bis eq '20260117'",
            ShopfloorSapKdTermin.NennerFilter("1100", new DateOnly(2026, 1, 12), new DateOnly(2026, 1, 17)));
    }

    [Fact]
    public void Nenner_Sap_Ist_Standard_Handwert_Geht_Vor_Und_Gleicher_Handwert_Faellt_Weg()
    {
        using var env = new ShopfloorTestEnv();
        env.Write(con =>
        {
            Assert.True(ShopfloorZd05.SaveWeekSap(con, "2026-W47", 1525));
            Assert.False(ShopfloorZd05.SaveWeekSap(con, "2026-W47", 1525));     // unveraendert
            ShopfloorZd05.SaveWeek(con, "2026-W48", new JsonObject { ["nenner"] = 1400 }, "koi");
            ShopfloorZd05.SaveWeekSap(con, "2026-W48", 1500);
            ShopfloorZd05.SaveWeek(con, "2026-W46", new JsonObject { ["nenner"] = 1300 }, "koi");
            ShopfloorZd05.SaveWeekSap(con, "2026-W46", 1300);                   // gleich: Handwert faellt weg
            return true;
        });
        var weeks = env.Write(con => ShopfloorZd05.History(con))["weeks"]!.AsArray().Select(w => w!.AsObject()).ToDictionary(w => (string)w["kw"]!);
        Assert.Equal("sap", (string)weeks["2026-W47"]["nenner_quelle"]!);
        Assert.Equal(1525, (double)weeks["2026-W47"]["nenner_eff"]!);
        Assert.Equal("hand_ueber_sap", (string)weeks["2026-W48"]["nenner_quelle"]!);
        Assert.Equal(1400, (double)weeks["2026-W48"]["nenner_eff"]!);
        Assert.Equal("sap", (string)weeks["2026-W46"]["nenner_quelle"]!);
        // Handeingabe gleich SAP-Wert gilt nicht als ueberschrieben
        env.Write(con => ShopfloorZd05.SaveWeek(con, "2026-W47", new JsonObject { ["nenner"] = "1'525" }, "koi"));
        var w47 = env.Write(con => ShopfloorZd05.History(con))["weeks"]!.AsArray().Select(w => w!.AsObject()).Single(w => (string)w["kw"]! == "2026-W47");
        Assert.Equal("sap", (string)w47["nenner_quelle"]!);
    }

    [Fact]
    public void Konfiguration_Hat_Die_Neuen_Felder_Ohne_Excel_Spalte()
    {
        var cfg = ShopfloorConfig.Load(Path.Combine(ShopfloorTestEnv.ShopfloorDir, "shopfloor_config.json"));
        foreach (var (modul, keys) in new[]
                 {
                     ("tvav", new[] { "pl_auf", "sobkz" }), ("angebote", new[] { "termin_sap", "fa", "pl_auf", "sobkz" }),
                     ("rs_tr5", new[] { "wunsch_sap", "bereit_sap", "liefer_sap", "auftraege_sap" })
                 })
            foreach (var k in keys)
            {
                var f = cfg.Modules[modul].Fields.Single(x => x.Key == k);
                Assert.Equal("sap", f.Src);
                Assert.Null(f.Col);                                                // kein Excel-Import auf diese Felder
            }
        Assert.Contains("T76K912954", ShopfloorSapSync.NotFoundHint(ShopfloorSapKdTermin.Set));
        Assert.Contains("T76K912954", ShopfloorSapSync.NotFoundHint(ShopfloorSapKdTermin.NennerSet));
    }
}
