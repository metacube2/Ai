using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using TrafagSalesExporter.Services.Shopfloor;

namespace TrafagSalesExporter.Tests;

/// <summary>Liefertreue OTD/OTIF/OTP gegen die Referenz von Fabio (docs/operations_kpi/Referenz, Stand 13.10.2026).</summary>
public class KpiLiefertreueTests
{
    private static readonly string RefDir = Path.Combine(ShopfloorTestEnv.ProjectRoot, "docs", "operations_kpi", "Referenz");

    private static JsonObject Abnahme() => (JsonObject)JsonNode.Parse(File.ReadAllText(Path.Combine(RefDir, "abnahme_daten_2026-01_bis_09.json")))!;

    [Fact]
    public void Abnahmedaten_Ergeben_Die_Abnahmewerte_Der_Spezifikation()
    {
        var s = KpiLiefertreue.Summarize(Abnahme());
        Assert.Equal((90.6, 80.7, 16134), (s.OtdTol, s.OtdStrict, s.OtdBase));
        Assert.Equal((89.9, 78.4, 9047), (s.OtifTol, s.OtifStrict, s.OtifBase));
        Assert.Equal((73.0, 57.8, 15975), (s.OtpTol, s.OtpStrict, s.OtpBase));
        Assert.Equal(2236, s.Early5);
        Assert.Equal("Jan – Sep 2026", s.Period);
    }

    [Fact]
    public void Seite_Entspricht_Der_Beispielseite_Der_Referenz()
    {
        // Git checkt die Referenz je nach Einstellung mit CRLF aus (Release-Worktree des Deploys); verglichen wird mit LF.
        var template = File.ReadAllText(Path.Combine(RefDir, "board_template.html")).Replace("\r\n", "\n");
        // Die Beispielseite ist zusaetzlich in doctype/head eingepackt (Artifact-Huelle), die Vorlage nicht.
        var expected = File.ReadAllText(Path.Combine(RefDir, "board_beispiel_2026-09.html")).Replace("\r\n", "\n");
        expected = expected[expected.IndexOf("<title>", StringComparison.Ordinal)..expected.LastIndexOf("\n</html>", StringComparison.Ordinal)];
        var actual = KpiLiefertreue.BuildBoard(Abnahme(), template);

        // Daten-Bloecke einzeln vergleichen (JSON nach dem Parsen, Escape-Unterschiede sind gleichgueltig)
        Assert.Equal(Block(expected, "const D="), Block(actual, "const D="));
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(Block(expected, "const PG=")[9..^1]), JsonNode.Parse(Block(actual, "const PG=")[9..^1])));
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(Block(expected, "const LATE=")[11..^1]), JsonNode.Parse(Block(actual, "const LATE=")[11..^1])));
        // Rest der Seite (ohne Daten-Bloecke) zeichengleich
        Assert.Equal(Strip(expected), Strip(actual));
    }

    private static string Block(string html, string start)
    {
        var i = html.IndexOf(start, StringComparison.Ordinal);
        Assert.True(i >= 0, start);
        var end = start == "const D=" ? html.IndexOf("};", i, StringComparison.Ordinal) + 2 : html.IndexOf(";\n", i, StringComparison.Ordinal) + 1;
        return html[i..end];
    }

    private static string Strip(string html)
    {
        foreach (var s in new[] { "const D=", "const PG=", "const LATE=" })
            html = html.Replace(Block(html, s), s);
        return html.Replace("\r\n", "\n").TrimEnd();
    }

    [Theory]
    [InlineData("20261009", "20261009", 0)]
    [InlineData("20261009", "20261012", 1)]     // Fr -> Mo
    [InlineData("20261012", "20261009", -1)]
    [InlineData("20261005", "20261019", 10)]
    [InlineData("20261010", "20261011", 0)]     // Sa -> So
    [InlineData("00000000", "20261011", null)]
    [InlineData("", "20261011", null)]
    public void Arbeitstage_Mo_Bis_Fr_Wie_Die_Referenz(string a, string b, int? expected)
        => Assert.Equal(expected, KpiLiefertreue.At(a, b));

    [Fact]
    public void Median_Ist_Das_Obere_Mittelelement()
    {
        Assert.Equal(3, KpiLiefertreue.Med([1, 2, 3, 4]));
        Assert.Equal(2, KpiLiefertreue.Med([3, 1, 2]));
        Assert.Null(KpiLiefertreue.Med([]));
    }

    [Theory]
    [InlineData("20260108", "20260108")]
    [InlineData("/Date(1767830400000)/", "20260108")]
    [InlineData("2026-01-08T00:00:00", "20260108")]
    public void Datum_Wird_Normalisiert(string v, string expected) => Assert.Equal(expected, KpiLiefertreue.Norm(v));

    [Fact]
    public void JsRound_Rundet_Halbe_Nach_Oben()
    {
        Assert.Equal(3, KpiLiefertreue.JsRound(2.5));
        Assert.Equal(-2, KpiLiefertreue.JsRound(-2.5));
    }

    private static LtOrderRow Pos(string so, string item, string cmtd, string status, string cust = "100", string product = "M1", string reqd = "", double delay = double.NaN)
        => new(so, item, "20260105", cmtd, cmtd[..6], (reqd.Length > 0 ? reqd : cmtd)[..6], reqd.Length > 0 ? reqd : cmtd, status, cust, product, "8736", 100.4, delay);

    [Fact]
    public void Kennzahlen_Fuer_Einfache_Faelle()
    {
        var rows = new[]
        {
            Pos("1", "10", "20260302", "C"),                        // WA 4 AT spaeter: OTD mit Toleranz, nicht streng
            Pos("1", "20", "20260302", "C"),                        // WA am Termin: puenktlich
            Pos("2", "10", "20260302", "A"),                        // offen: nicht puenktlich
            Pos("3", "10", "20260302", "C", delay: 0),              // kein WA: Fallback SAP-Verzug 0 -> puenktlich
            Pos("4", "10", "20260320", "C"),                        // WA 10 AT frueher: zu frueh
            Pos("5", "10", "20260302", "C", cust: "302906"),        // ausgeschlossener Kunde
            Pos("6", "10", "20260302", "C", product: "H70201"),     // Zertifikat
            Pos("7", "10", "20260402", "C")                         // nach dem Berichtsmonat
        };
        var deliveries = new[]
        {
            new LtDeliveryRow("20260306", "0000000001", "000010"),
            new LtDeliveryRow("20260302", "1", "20"),
            new LtDeliveryRow("20260306", "4", "10"),
            new LtDeliveryRow("20260302", "5", "10"),
            new LtDeliveryRow("20260302", "6", "10")
        };
        var excl = new[] { new LtExclusion(1, "Kunde", "302906", "Trafag Spol. s.r.o. (CZ)", "konzerninterne Lieferungen") };
        var lt = KpiLiefertreue.Compute(rows, deliveries, "202603", new DateOnly(2026, 4, 2), excl, ["H70201"], ["302906"]);

        // Monat;faellig;streng;Toleranz;Auftraege;OTIF streng;OTIF Tol.;zu frueh;ohne WA
        Assert.Equal("202603;5;3;4;4;2;3;1;1", lt["otd"]![0]!.GetValue<string>());
        var list = lt["list"]!.AsArray().Select(x => x!.GetValue<string>().Split(';')).ToList();
        Assert.Contains(list, x => x[0] == "2" && x[14] == "L");                 // offen = zu spaet
        Assert.Contains(list, x => x[0] == "4" && x[14] == "F" && x[4] == "-10"); // zu frueh
        Assert.DoesNotContain(list, x => x[0] == "5");                            // ausgeschlossen, aber puenktlich: nicht in der Liste
        var summary = KpiLiefertreue.Summarize(lt);
        Assert.Equal(80.0, summary.OtdTol);
    }

    [Fact]
    public void Excel_Export_Hat_Eine_Zeile_Je_Abweichung()
    {
        var bytes = KpiLiefertreue.ExportXlsx(Abnahme());
        using var wb = new ClosedXML.Excel.XLWorkbook(new MemoryStream(bytes));
        Assert.Equal(Abnahme()["list"]!.AsArray().Count + 1, wb.Worksheet(1).LastRowUsed()!.RowNumber());
    }
}
