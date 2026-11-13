using System.Security.Cryptography;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using TrafagSalesExporter.Services.Shopfloor;

namespace TrafagSalesExporter.Tests;

/// <summary>Mail Fabio 2026-11-13: Stichtagswerte nur aus dem ersten SAP-Lauf des Tages, Morgenwerte nachladen, Cache-Busting.</summary>
public class ShopfloorStichtagTests
{
    private const string Tag = "2026-11-11";

    private static JsonObject Vorrat(long total, long ziel, long kw = 100) => ShopfloorTestEnv.Obj(
        "{\"day\":\"" + Tag + "\",\"werte\":{\"seh\":{\"vorrat_total\":" + total + ",\"vorrat_3m\":10,\"vorrat_2m\":9,\"vorrat_1m\":8," +
        "\"offen_kw\":" + kw + ",\"ziel\":" + ziel + "}," +
        "\"tx\":{\"vorrat_total\":500,\"vorrat_3m\":1,\"vorrat_2m\":1,\"vorrat_1m\":1,\"offen_kw\":1,\"plan5\":5939}}}");

    private static JsonObject Day(ShopfloorTestEnv env, string module)
        => (JsonObject)env.Store.GetDaily(module, Tag, Tag)[module]![Tag]!["data"]!;

    [Fact]
    public void Erster_Lauf_Schreibt_Spaetere_Laeufe_Lassen_Die_Stichtagswerte_Stehen()
    {
        using var env = new ShopfloorTestEnv();
        var morgen = Vorrat(156471, 2655);
        ShopfloorStichtag.Write(env.Store, morgen, false);
        Assert.Empty((JsonArray)morgen["eingefroren"]!);

        // 12:45 oder Neustart nach einem Deploy: SAP liefert den Rest des Tages
        var mittag = Vorrat(158386, 1718, 5935);
        ShopfloorStichtag.Write(env.Store, mittag, false);

        var seh = Day(env, "seh");
        Assert.Equal(156471, (long)seh["vorrat_total"]!);
        Assert.Equal(2655, (long)seh["ziel"]!);
        Assert.Equal(100, (long)seh["offen_kw"]!);
        Assert.Equal(5939, (long)Day(env, "tx")["plan5"]!);
        Assert.Contains("seh.ziel", ((JsonArray)mittag["eingefroren"]!).Select(x => (string)x!));
        Assert.Equal(1718, (long)mittag["werte"]!["seh"]!["ziel"]!);   // Vorschau zeigt weiter den SAP-Wert
    }

    [Fact]
    public void Ersetzen_Schreibt_Trotzdem()
    {
        using var env = new ShopfloorTestEnv();
        ShopfloorStichtag.Write(env.Store, Vorrat(156471, 2655), false);
        ShopfloorStichtag.Write(env.Store, Vorrat(158386, 1718), true);
        Assert.Equal(1718, (long)Day(env, "seh")["ziel"]!);
    }

    [Fact]
    public void Leeres_Feld_Wird_Auch_Spaeter_Noch_Gefuellt()
    {
        // 04:00 ausgefallen (Entscheid Ingo 13.11.): der naechste Lauf setzt den Wert, danach gilt er
        using var env = new ShopfloorTestEnv();
        env.Store.ApplyPush(new ShopfloorPush
        {
            Items = [new JsonObject { ["module"] = "seh", ["day"] = Tag, ["fields"] = new JsonObject { ["prod"] = 1130 } }]
        }, "SAP");
        ShopfloorStichtag.Write(env.Store, Vorrat(156723, 1907), false);
        Assert.Equal(1907, (long)Day(env, "seh")["ziel"]!);
        Assert.Equal(1130, (long)Day(env, "seh")["prod"]!);
    }

    [Fact]
    public void Produzierte_Mengen_Laufen_Weiter_Nach()
    {
        using var env = new ShopfloorTestEnv();
        ShopfloorStichtag.Write(env.Store, Vorrat(156471, 2655), false);
        JsonObject? Existing(string m) => env.Store.GetDaily(m, Tag, Tag)[m]?[Tag] as JsonObject;
        foreach (var prod in new[] { 1130, 2547 })
        {
            var mengen = ShopfloorTestEnv.Obj("{\"day\":\"" + Tag + "\",\"werte\":{\"seh\":{\"prod\":" + prod + "}}}");
            env.Store.ApplyPush(new ShopfloorPush { Items = ShopfloorSapMengen.BuildItems(mengen, Existing).Items }, "SAP");
        }
        Assert.Equal(2547, (long)Day(env, "seh")["prod"]!);
        Assert.Equal(2655, (long)Day(env, "seh")["ziel"]!);
    }

    [Fact]
    public void Morgenwerte_Kommen_Aus_Dem_Verlauf_Zurueck()
    {
        using var env = new ShopfloorTestEnv();
        // Stand vor dem Fix: jeder Lauf ueberschrieb (ersetzen = alter Ablauf)
        ShopfloorStichtag.Write(env.Store, Vorrat(156471, 2655), true);
        ShopfloorStichtag.Write(env.Store, Vorrat(158386, 1718), true);
        ShopfloorStichtag.Write(env.Store, Vorrat(157479, 408), true);
        Assert.Equal(408, (long)Day(env, "seh")["ziel"]!);

        var day = DateOnly.Parse(Tag);
        var vorschau = ShopfloorStichtag.RestoreMorning(env.Store, day, day, false);
        Assert.False((bool)vorschau["geschrieben"]!);
        Assert.Equal(408, (long)Day(env, "seh")["ziel"]!);
        var ziel = ((JsonArray)vorschau["zeilen"]!).Single(z => (string)z!["modul"]! == "seh" && (string)z["feld"]! == "ziel")!;
        Assert.Equal(2655, (long)ziel["morgenwert"]!);
        Assert.Equal("zurueckgesetzt", (string)ziel["aktion"]!);

        var res = ShopfloorStichtag.RestoreMorning(env.Store, day, day, true);
        Assert.True((bool)res["geschrieben"]!);
        Assert.Equal(2655, (long)Day(env, "seh")["ziel"]!);
        Assert.Equal(156471, (long)Day(env, "seh")["vorrat_total"]!);
        Assert.Equal(5939, (long)Day(env, "tx")["plan5"]!);

        // zweiter Aufruf: alles gleich, nichts zu tun
        var nochmal = ShopfloorStichtag.RestoreMorning(env.Store, day, day, true);
        Assert.Equal(0, (int)nochmal["zurueckzusetzen"]!);
        Assert.All((JsonArray)nochmal["zeilen"]!, z => Assert.Equal("gleich", (string)z!["aktion"]!));
    }

    [Fact]
    public void Tag_Ohne_Verlauf_Bleibt_Unberuehrt()
    {
        using var env = new ShopfloorTestEnv();
        var day = DateOnly.Parse(Tag);
        var res = ShopfloorStichtag.RestoreMorning(env.Store, day, day, true);
        Assert.Empty((JsonArray)res["zeilen"]!);
        Assert.False((bool)res["geschrieben"]!);
    }

    [Fact]
    public void Vorrat_Und_Verzug_Mit_Materialfilter_TR5_DW_Und_Untergrenze_2026()
    {
        // Paket Fabio 13.11.: TR5 und DW nur Material 1-99999 (Vorrat und Verzug), Verzug ab 01.01.2026 (Entscheid Ingo)
        var options = new ShopfloorOptions();
        Assert.Equal(["TR5", "DW"], options.MaterialFilterListe());
        Assert.Equal(new DateOnly(2026, 1, 1), options.VerzugAbDatum());
        Assert.False(options.SapCoPaHzdat);
        var appsettings = JsonNode.Parse(File.ReadAllText(Path.Combine(ShopfloorTestEnv.ProjectRoot, "appsettings.json")))!["Shopfloor"]!;
        Assert.True((bool)appsettings["SapVerzugEnabled"]!);
        Assert.Equal("TR5,DW", (string)appsettings["MaterialFilter"]!);
        Assert.Equal("2026-01-01", (string)appsettings["VerzugAb"]!);
        Assert.Null(appsettings["VerzugMaterialFilter"]);
    }

    [Fact]
    public void IndexHtml_Laedt_AppJs_Und_StyleCss_Mit_Aktueller_Version()
    {
        // Cache-Busting (Fabio 13.11.): v = erste 8 Zeichen SHA256 von app.js + style.css ohne CR. Aendert sich eine der beiden
        // Dateien, faellt dieser Test, bis index.html die neue Version traegt (Wert steht in der Fehlermeldung).
        var dir = Path.Combine(ShopfloorTestEnv.ProjectRoot, "wwwroot", "shopfloor");
        var bytes = File.ReadAllBytes(Path.Combine(dir, "app.js")).Concat(File.ReadAllBytes(Path.Combine(dir, "style.css")))
            .Where(b => b != (byte)'\r').ToArray();
        var soll = Convert.ToHexString(SHA256.HashData(bytes))[..8].ToLowerInvariant();
        var html = File.ReadAllText(Path.Combine(dir, "index.html"));
        Assert.Equal(soll, Regex.Match(html, @"app\.js\?v=([0-9a-f]+)").Groups[1].Value);
        Assert.Equal(soll, Regex.Match(html, @"style\.css\?v=([0-9a-f]+)").Groups[1].Value);
    }
}
