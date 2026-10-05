using System.Net;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public sealed class WorldAnalyticsTests
{
    private static readonly DateOnly Today = new(2026, 10, 5);

    private static string GdeltLine(string fips, int quad, string root, double goldstein, double tone, int mentions)
    {
        var c = Enumerable.Repeat("", 61).ToArray();
        c[28] = root;
        c[29] = quad.ToString();
        c[30] = goldstein.ToString(System.Globalization.CultureInfo.InvariantCulture);
        c[31] = mentions.ToString();
        c[34] = tone.ToString(System.Globalization.CultureInfo.InvariantCulture);
        c[52] = "Ort";
        c[53] = fips;
        c[59] = "20261005063000";
        c[60] = "https://example.org/a";
        return string.Join('\t', c);
    }

    [Fact]
    public void Gdelt_Zeile_Fips_Wird_Iso()
    {
        var e = WorldParsers.ParseGdeltEvent(GdeltLine("GM", 4, "19", -10, -5.5, 30));

        Assert.NotNull(e);
        Assert.Equal("DE", e!.Country);
        Assert.Equal(4, e.QuadClass);
        Assert.Equal(-10, e.Goldstein);
        Assert.Equal(new DateTime(2026, 10, 5, 6, 30, 0, DateTimeKind.Utc), e.TimeUtc);
        Assert.Null(WorldParsers.ParseGdeltEvent(GdeltLine("XX", 1, "01", 1, 1, 1)));
    }

    [Fact]
    public void Gdelt_Aggregat_Zaehlt_Konflikt_Und_Protest()
    {
        var events = new[] { GdeltLine("SZ", 4, "19", -10, -5, 3), GdeltLine("SZ", 3, "14", -6.5, -2, 2), GdeltLine("SZ", 1, "04", 1, 3, 1) }
            .Select(WorldParsers.ParseGdeltEvent).OfType<WorldEvent>();

        var day = Assert.Single(WorldParsers.Aggregate(events));

        Assert.Equal("CH", day.Country);
        Assert.Equal(3, day.Events);
        Assert.Equal(2, day.Conflict);
        Assert.Equal(1, day.Protest);
        Assert.Equal(6, day.Mentions);
    }

    [Fact]
    public void Lastupdate_Und_Zeitstempel()
    {
        const string text = "123 abc http://data.gdeltproject.org/gdeltv2/20261005063000.export.CSV.zip\n456 def http://data.gdeltproject.org/gdeltv2/20261005063000.mentions.CSV.zip\n";

        var url = WorldParsers.LatestExportUrl(text);

        Assert.EndsWith("20261005063000.export.CSV.zip", url);
        Assert.Equal(new DateTime(2026, 10, 5, 6, 30, 0, DateTimeKind.Utc), WorldParsers.StampOf(url!));
        Assert.Equal(url, WorldParsers.ExportUrl(WorldParsers.StampOf(url!)!.Value));
    }

    [Fact]
    public void Fred_Csv_Ueberspringt_Fehlende_Werte()
    {
        var points = WorldParsers.ParseFredCsv("observation_date,PCOPPUSDM\n2026-06-01,9500.5\n2026-07-01,.\n2026-08-01,9800\n");

        Assert.Equal(2, points.Count);
        Assert.Equal(9800, points[^1].Item2);
    }

    [Fact]
    public void Eurostat_Json_Stat()
    {
        const string json = """{"value":{"0":101.5,"1":99.0},"dimension":{"time":{"category":{"index":{"2026-06":0,"2026-07":1}}}}}""";

        var points = WorldParsers.ParseEurostat(json);

        Assert.Equal(2, points.Count);
        Assert.Equal(new DateOnly(2026, 7, 1), points[1].Item1);
        Assert.Equal(99.0, points[1].Item2);
    }

    [Fact]
    public void Eurostat_Json_Stat_Arrayform()
    {
        // JSON-stat kennt value und index auch als Arrays (null = fehlender Wert).
        const string json = """{"value":[101.5,null,99.0],"dimension":{"time":{"category":{"index":["2026-05","2026-06","2026-07"]}}}}""";

        var points = WorldParsers.ParseEurostat(json);

        Assert.Equal(2, points.Count);
        Assert.Equal(new DateOnly(2026, 5, 1), points[0].Item1);
        Assert.Equal(new DateOnly(2026, 7, 1), points[1].Item1);
        Assert.Equal(99.0, points[1].Item2);
    }

    [Fact]
    public void Ereignisse_Doppelte_Adressen_Werden_Zusammengefasst()
    {
        var t = new DateTime(2026, 10, 5, 6, 0, 0, DateTimeKind.Utc);
        var events = new[]
        {
            new WorldEvent(t, "DE", "A", 4, "19", -10, -5, 10, "https://example.org/a"),
            new WorldEvent(t.AddMinutes(15), "DE", "B", 4, "19", -10, -5, 30, "https://example.org/A"),
            new WorldEvent(t, "FR", "C", 4, "19", -10, -5, 5, "https://example.org/a")
        };

        var result = WorldParsers.DedupeEvents(events);

        Assert.Equal(2, result.Count);
        Assert.Equal(30, result.Single(e => e.Country == "DE").Mentions);
    }

    [Fact]
    public void Abteilungskennzahl_Mittelt_Themen_Statt_Zu_Addieren()
    {
        WorldImpact I(string topic, double share, double signal) => new(WorldImpactAnalytics.Purchasing, topic, "x", "x", "", 1, share, signal, "t");
        var impacts = new[] { I("Lieferland", 1, -0.5), I("Rohstoff", 1, -0.5) };

        var score = WorldImpactAnalytics.DepartmentScores(impacts)[WorldImpactAnalytics.Purchasing];

        Assert.Equal(-50, score, 1);
    }

    [Fact]
    public void Imf_Wachstum_Je_Land()
    {
        const string json = """{"values":{"NGDP_RPCH":{"DEU":{"2026":0.9,"2027":1.4},"CHE":{"2026":1.3}}}}""";

        var growth = WorldParsers.ParseImf(json, ["DE", "CH", "IT"], 2026);

        Assert.Equal(2, growth.Count);
        Assert.Equal(1.4, growth.Single(g => g.Country == "DE").Year2);
        Assert.Null(growth.Single(g => g.Country == "CH").Year2);
    }

    [Fact]
    public void Firewall_Erkennung()
    {
        Assert.Equal(WorldSourceState.Blocked, WorldDataService.Classify(new HttpRequestException("no route")).State);
        Assert.Equal(WorldSourceState.Blocked, WorldDataService.Classify(new HttpRequestException("x", null, HttpStatusCode.Forbidden)).State);
        Assert.Equal(WorldSourceState.Blocked, WorldDataService.Classify(new TaskCanceledException()).State);
        Assert.Equal(WorldSourceState.Error, WorldDataService.Classify(new HttpRequestException("x", null, HttpStatusCode.InternalServerError)).State);
        Assert.Equal(WorldSourceState.Error, WorldDataService.Classify(new InvalidDataException("kaputt")).State);
    }

    [Fact]
    public void Rohstoff_Stichworte()
    {
        Assert.Equal("PNICKUSDM", WorldImpactAnalytics.CommodityOf("Membranen DMS Edelstahl"));
        Assert.Equal("PCOPPUSDM", WorldImpactAnalytics.CommodityOf("Kabel & Litzen"));
        Assert.Equal("DCOILBRENTEU", WorldImpactAnalytics.CommodityOf("Gehäuseteile Kunstst"));
        Assert.Null(WorldImpactAnalytics.CommodityOf("Büromaterial"));
    }

    private static IEnumerable<WorldEventDay> Days(string country, double conflictBase, double conflictNow)
    {
        for (var i = 3; i < 30; i++)
            yield return new WorldEventDay(Today.AddDays(-i), country, 100, (int)(100 * conflictBase), 0, 100, -100, 0);
        for (var i = 0; i < 3; i++)
            yield return new WorldEventDay(Today.AddDays(-i), country, 100, (int)(100 * conflictNow), 0, 100, -100, 0);
    }

    [Fact]
    public void Ereignissignal_Mehr_Konflikt_Ist_Negativ()
    {
        var calm = WorldImpactAnalytics.EventSignal(Days("CH", 0.1, 0.1), "CH", Today);
        var worse = WorldImpactAnalytics.EventSignal(Days("CH", 0.1, 0.4), "CH", Today);

        Assert.NotNull(calm);
        Assert.Equal(0, calm!.Value.Signal, 6);
        Assert.True(worse!.Value.Signal < -0.5);
        Assert.Null(WorldImpactAnalytics.EventSignal([], "CH", Today));
    }

    [Fact]
    public void Wirkung_Gewichtet_Mit_Unserem_Anteil()
    {
        var snap = new WorldSnapshot { EventDays = Days("DE", 0.1, 0.4).Concat(Days("CH", 0.1, 0.1)).ToList() };
        var exp = new WorldExposure { SalesByCountry = new Dictionary<string, decimal> { ["DE"] = 750, ["CH"] = 250 } };

        var impacts = WorldImpactAnalytics.Compute(snap, exp, Today);

        var de = impacts.Single(i => i.Subject == "DE");
        Assert.Equal(WorldImpactAnalytics.Sales, de.Department);
        Assert.Equal(0.75, de.ExposureShare, 6);
        Assert.True(de.Score < 0);
        Assert.Equal(0, impacts.Single(i => i.Subject == "CH").Score);
        Assert.Equal(de.Score, WorldImpactAnalytics.DepartmentScores(impacts)[WorldImpactAnalytics.Sales], 1);
    }

    [Fact]
    public void Waehrung_Schwaecher_Bei_Einnahmen_Ist_Gegenwind()
    {
        var exp = new WorldExposure
        {
            NetByCurrency = new Dictionary<string, decimal> { ["EUR"] = 1000, ["USD"] = -500 },
            CurrencyChange90 = new Dictionary<string, double> { ["EUR"] = -4, ["USD"] = -4 }
        };

        var impacts = WorldImpactAnalytics.Compute(new WorldSnapshot(), exp, Today);

        Assert.True(impacts.Single(i => i.Subject == "EUR").Signal < 0);
        Assert.True(impacts.Single(i => i.Subject == "USD").Signal > 0);
    }

    [Fact]
    public void Ezb_Kreuzkurs_Gegen_Chf()
    {
        const string xml = """<gesmes:Envelope xmlns:gesmes="http://www.gesmes.org/xml/2002-08-01" xmlns="http://www.ecb.int/vocabulary/2002-08-01/eurofxref"><Cube><Cube time="2026-10-02"><Cube currency="USD" rate="1.2"/><Cube currency="CHF" rate="0.9"/></Cube><Cube time="2026-07-06"><Cube currency="USD" rate="1.0"/><Cube currency="CHF" rate="0.9"/></Cube></Cube></gesmes:Envelope>""";

        var perEur = WorldParsers.ParseEcbHistory(xml);

        Assert.Equal(2, perEur["EUR"].Count);
        Assert.Equal(new DateOnly(2026, 7, 6), perEur["USD"][0].Item1);
        Assert.Equal(0, WorldParsers.ChangeAgainstChf(perEur, "EUR")!.Value, 6);
        Assert.Equal(-16.6667, WorldParsers.ChangeAgainstChf(perEur, "USD")!.Value, 3);
        Assert.Null(WorldParsers.ChangeAgainstChf(perEur, "INR"));
    }
}
