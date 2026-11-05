using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Monthly Review, Abschnitt 4 Q-Faelle aus SAP (QmMeldungSet, 2026-11-05): Lesen der OData-Antwort, Zuordnung Kunde ->
/// Gesellschaft, Alter ab Anlage bis Abrufdatum, Kennzahlen, Liste, CSV und Verhalten des Dienstes (Schalter, Fehler, Cache).
/// </summary>
public sealed class MonthlyReviewQmTests
{
    private static readonly DateOnly Reference = new(2026, 11, 5);

    // Beispielantwort im Format des Gateways (OData V2): alles Edm.String ausser Menge (Edm.Decimal, als Zeichenkette).
    private const string SampleJson = """
        {"d":{"results":[
          {"__metadata":{"type":"ZPOWERBI_EINKAUF_SRV.QmMeldung"},"Qmnum":"000300012345","Qmart":"Q1","Status":"Rücksendung offen","Sysstat":"OSNO","Kunnr":"0000500731","Kunde":"Trafag Controls India Pvt. Ltd.","Matnr":"000000000000064776","Maktx":"NAH 8254","Menge":"5.000","Mengeneinheit":"ST","Fehlerbild":"Leckage","Angelegt":"20260701","Ruecklieferung":"","Produktion":"20260115","Massnahme":"20260710","Bearbeiter":"MUELLER","Produktlinie":"Dichtewächter"},
          {"Qmnum":"000300012346","Qmart":"Q1","Status":"in Bearbeitung","Sysstat":"OSNO","Kunnr":"0000123456","Kunde":"Siemens AG","Matnr":"H05039","Maktx":"Transmitter","Menge":"1","Mengeneinheit":"ST","Fehlerbild":"Signal","Angelegt":"20261020","Ruecklieferung":"20261101","Produktion":"00000000","Massnahme":"","Bearbeiter":"MEIER","Produktlinie":"TR5"},
          {"Qmnum":"000300012347","Qmart":"Q1","Status":"Wareneingang","Sysstat":"MOFN MMAB","Kunnr":"1","Kunde":"Erledigt AG","Matnr":"1","Maktx":"x","Menge":"","Mengeneinheit":"","Fehlerbild":"","Angelegt":"20260101","Ruecklieferung":"","Produktion":"","Massnahme":"","Bearbeiter":"","Produktlinie":""},
          {"Qmnum":"000300012348","Qmart":"Q2","Status":"in Bearbeitung","Sysstat":"OSNO","Kunnr":"2","Kunde":"Lieferant AG","Matnr":"1","Maktx":"x","Menge":"2","Mengeneinheit":"ST","Fehlerbild":"","Angelegt":"20260901","Ruecklieferung":"","Produktion":"","Massnahme":"","Bearbeiter":"","Produktlinie":""},
          {"Qmnum":"000300012345","Qmart":"Q1","Status":"Rücksendung offen","Sysstat":"OSNO","Kunnr":"0000500731","Kunde":"Trafag Controls India Pvt. Ltd.","Matnr":"64776","Maktx":"NAH 8254","Menge":"5.000","Mengeneinheit":"ST","Fehlerbild":"Leckage","Angelegt":"20260701","Ruecklieferung":"","Produktion":"","Massnahme":"","Bearbeiter":"MUELLER","Produktlinie":"Dichtewächter"}
        ]}}
        """;

    private static MonthlyReviewQmRow Row(string number, DateOnly? created, string status = "in Bearbeitung", string company = "", string customer = "Kunde",
        string processor = "", string type = "Q1", string sys = "OSNO", string kunnr = "", string productLine = "")
        => new(number, type, status, sys, kunnr, customer, "1", "Text " + number, 1m, "ST", "", created, null, null, null, processor, productLine);

    private static MonthlyReviewQmCase Case(string number, int? age, string status = "in Bearbeitung", string company = "Endkunde (TRCH)", string processor = "")
        => new(Row(number, age is null ? null : Reference.AddDays(-age.Value), status, processor: processor), company, age, null, null);

    [Fact]
    public void Parse_Liest_Odata_V2_Mit_Dezimal_Als_Text_Und_Datum_Jjjjmmtt()
    {
        var rows = MonthlyReviewQm.Parse(SampleJson);

        Assert.Equal(5, rows.Count);
        var first = rows[0];
        Assert.Equal("300012345", first.Number);
        Assert.Equal("Q1", first.Type);
        Assert.Equal("64776", first.Material);
        Assert.Equal(5m, first.Quantity);
        Assert.Equal(new DateOnly(2026, 7, 1), first.Created);
        Assert.Null(first.Returned);
        Assert.Equal(new DateOnly(2026, 7, 10), first.MeasureCreated);
        Assert.Equal("Dichtewächter", first.ProductLine);
        Assert.Null(rows[1].Production);           // 00000000
        Assert.Equal("H05039", rows[1].Material);  // alphanumerisch bleibt
        Assert.Null(rows[2].Quantity);             // leer
    }

    [Fact]
    public void Parse_Ohne_Results_Meldet_Fehler()
        => Assert.Throws<InvalidOperationException>(() => MonthlyReviewQm.Parse("""{"error":{"message":{"value":"x"}}}"""));

    [Theory]
    [InlineData("", "")]
    [InlineData("00000000", "")]
    [InlineData("20261231", "2026-12-31")]
    [InlineData("2026-03-04", "2026-03-04")]
    [InlineData("20261340", "")]
    public void Datum_Jjjjmmtt_Oder_Leer(string value, string expected)
        => Assert.Equal(expected, MonthlyReviewQm.Date(value)?.ToString("yyyy-MM-dd") ?? "");

    [Theory]
    [InlineData("0000500731", "", "TRIN")]                                 // Kundennummer aus KpiLiefertreueSync, fuehrende Nullen
    [InlineData("302906", "Trafag spol. s r.o.", "TRCZ")]
    [InlineData("19914", "GFS Germany", "GFS")]
    [InlineData("", "Trafag Gesellschaft m.b.H.", "TRAT")]                 // Namensanfang aus dem Entwurf
    [InlineData("777", "TRAFAG (UK) LTD", "TRUK")]
    [InlineData("777", "Trafag España S.L.", "TRES")]
    [InlineData("777", "Trafag Espana S.L.", "TRES")]
    [InlineData("777", "Trafag Japan Ltd", "TRJP")]
    [InlineData("777", "TRAFAG AG", "TRCH intern")]
    [InlineData("777", "Trafag GmbH", "TRDE")]
    [InlineData("777", "Siemens AG", "Endkunde (TRCH)")]
    [InlineData("", "", "Endkunde (TRCH)")]
    public void Zuordnung_Kundennummer_Vor_Namensanfang_Sonst_Endkunde(string kunnr, string name, string expected)
        => Assert.Equal(expected, MonthlyReviewQm.AssignCompany(kunnr, name, MonthlyReviewQm.CompanyMap(new MonthlyReviewOptions())));

    [Fact]
    public void Konfigurierte_Zuordnung_Geht_Den_Vorgaben_Vor()
    {
        var options = new MonthlyReviewOptions
        {
            QmCompanyCustomers = [new MonthlyReviewCompanyCustomer { Company = "TRUK", CustomerNumbers = ["00123"] },
                                  new MonthlyReviewCompanyCustomer { Company = "TRUS", CustomerNumbers = ["500731"] }]
        };
        var map = MonthlyReviewQm.CompanyMap(options);

        Assert.Equal("TRUK", MonthlyReviewQm.AssignCompany("123", "Irgendwer", map));
        Assert.Equal("TRUS", MonthlyReviewQm.AssignCompany("500731", "Trafag Controls India", map));
        Assert.Equal("TRAT", MonthlyReviewQm.AssignCompany("9", "Trafag Gesellschaft", map));
    }

    [Fact]
    public void BuildCases_Filtert_Art_Und_Abschluss_Zaehlt_Doppelte_Einmal_Und_Rechnet_Alter()
    {
        var cases = MonthlyReviewQm.BuildCases(MonthlyReviewQm.Parse(SampleJson), new MonthlyReviewOptions(), Reference);

        Assert.Equal(2, cases.Count);                    // ohne MMAB, ohne Q2, Doppelte einmal
        var india = cases.Single(c => c.Number == "300012345");
        Assert.Equal("TRIN", india.Company);
        Assert.Equal(Reference.DayNumber - new DateOnly(2026, 7, 1).DayNumber, india.AgeDays);
        Assert.Equal(9, india.DaysToMeasure);
        Assert.Null(india.DaysSinceReturn);
        var siemens = cases.Single(c => c.Number == "300012346");
        Assert.Equal("Endkunde (TRCH)", siemens.Company);
        Assert.Equal(16, siemens.AgeDays);
        Assert.Equal(4, siemens.DaysSinceReturn);

        // Ohne Artenfilter zaehlt auch Q2.
        var all = MonthlyReviewQm.BuildCases(MonthlyReviewQm.Parse(SampleJson), new MonthlyReviewOptions { QmNotificationTypes = [] }, Reference);
        Assert.Equal(3, all.Count);
    }

    [Fact]
    public void Stats_Durchschnitt_Median_Ueber90_Aeltester_Und_Ruecksendung()
    {
        var cases = new[]
        {
            Case("1", 10, "Rücksendung offen"), Case("2", 90), Case("3", 91, "Ruecksendung offen"), Case("4", 200), Case("5", null)
        };

        var s = MonthlyReviewQm.Stats(cases);

        Assert.Equal(5, s.Count);
        Assert.Equal((10 + 90 + 91 + 200) / 4.0, s.AverageDays);
        Assert.Equal(90.5, s.MedianDays);                // gerade Anzahl: Mittel der zwei mittleren
        Assert.Equal(2, s.Over90);                       // 90 zaehlt nicht, 91 schon
        Assert.Equal("4", s.Oldest!.Number);
        Assert.Equal(200, s.MaxDays);
        Assert.Equal(2, s.ReturnOpen);
        Assert.Equal(0.4, s.Over90Share);

        var empty = MonthlyReviewQm.Stats([]);
        Assert.Null(empty.AverageDays);
        Assert.Null(empty.MedianDays);
        Assert.Null(empty.Oldest);
        Assert.Null(empty.Over90Share);
    }

    [Fact]
    public void Altersklassen_Grenzen_Und_Faelle_Ohne_Anlagedatum_Zaehlen_Nicht()
    {
        var buckets = MonthlyReviewQm.ByAge([Case("a", 0), Case("b", 30), Case("c", 31), Case("d", 90), Case("e", 91), Case("f", 180), Case("g", 181), Case("h", null)]);

        // 0 und 30 | 31 | 90 | 91 und 180 | 181; ohne Anlagedatum nicht gezaehlt.
        Assert.Equal(new[] { 2, 1, 1, 2, 1 }, buckets.Select(b => b.Count));
        Assert.Equal(new[] { "≤ 30 Tage", "31–60 Tage", "61–90 Tage", "91–180 Tage", "> 180 Tage" }, buckets.Select(b => b.Label));
    }

    [Fact]
    public void TRCH_Umfasst_Endkunden_Und_Trafag_Ag_Intern()
    {
        var cases = new[] { Case("1", 1, company: "Endkunde (TRCH)"), Case("2", 1, company: "TRCH intern"), Case("3", 1, company: "TRIN") };

        Assert.Equal(2, MonthlyReviewQm.ForCompany(cases, "TRCH").Count);
        Assert.Single(MonthlyReviewQm.ForCompany(cases, "trin"));
        Assert.Empty(MonthlyReviewQm.ForCompany(cases, "TRAT"));
    }

    [Fact]
    public void Liste_Sortiert_Alter_Absteigend_Leerwerte_Am_Ende_Und_Filtert()
    {
        var cases = new[]
        {
            Case("1", 10, processor: "MEIER"), Case("2", null, processor: "MEIER"), Case("3", 300, "Rücksendung offen", processor: "MUELLER"), Case("4", 50, company: "TRIN")
        };

        var byAge = MonthlyReviewQm.Query(cases, null, null, null, null, MonthlyReviewQm.SortColumn.Age, true);
        Assert.Equal(new[] { "3", "4", "1", "2" }, byAge.Select(c => c.Number));
        var ascending = MonthlyReviewQm.Query(cases, null, null, null, null, MonthlyReviewQm.SortColumn.Age, false);
        Assert.Equal(new[] { "1", "4", "3", "2" }, ascending.Select(c => c.Number));

        Assert.Equal(new[] { "1", "2" }, MonthlyReviewQm.Query(cases, null, null, "MEIER", null, MonthlyReviewQm.SortColumn.Number, false).Select(c => c.Number));
        Assert.Single(MonthlyReviewQm.Query(cases, "TRIN", null, null, null, MonthlyReviewQm.SortColumn.Age, true));
        Assert.Single(MonthlyReviewQm.Query(cases, null, "Rücksendung offen", null, null, MonthlyReviewQm.SortColumn.Age, true));
        Assert.Equal("3", MonthlyReviewQm.Query(cases, null, null, null, "mueller", MonthlyReviewQm.SortColumn.Age, true).Single().Number);
    }

    [Fact]
    public void Csv_Mit_Bom_Semikolon_Datum_Und_Quoting()
    {
        var row = new MonthlyReviewQmRow("300012345", "Q1", "offen", "OSNO", "500731", "Kunde; mit \"Zeichen\"", "64776", "NAH", 5m, "ST",
            "Leck", new DateOnly(2026, 7, 1), null, null, null, "MUELLER", "TR5");
        var bytes = MonthlyReviewQm.ToCsv([new MonthlyReviewQmCase(row, "TRIN", 127, null, null)]);

        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3));
        var text = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        var lines = text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.StartsWith("Meldung;Meldungsart;Status;", lines[0]);
        Assert.Contains("\"Kunde; mit \"\"Zeichen\"\"\"", lines[1]);
        Assert.Contains(";01.07.2026;127;", lines[1]);
        Assert.Contains(";5;ST;", lines[1]);
    }

    // ---------- Dienst ----------

    private sealed class Monitor(MonthlyReviewOptions value) : IOptionsMonitor<MonthlyReviewOptions>
    {
        public MonthlyReviewOptions CurrentValue => value;
        public MonthlyReviewOptions Get(string? name) => value;
        public IDisposable? OnChange(Action<MonthlyReviewOptions, string?> listener) => null;
    }

    private sealed class NoServices : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private static IReadOnlyList<JsonElement> Elements(string json) => MonthlyReviewQm.Results(JsonDocument.Parse(json).RootElement).ToList();

    private static MonthlyReviewQmService Service(MonthlyReviewOptions options, Func<IReadOnlyList<JsonElement>> fetch, Func<DateTime> now, Action? onFetch = null)
        => new(new NoServices(), new Monitor(options), NullLogger<MonthlyReviewQmService>.Instance)
        {
            FetchOverride = (_, _) =>
            {
                onFetch?.Invoke();
                return Task.FromResult(fetch());
            },
            Now = now
        };

    [Fact]
    public async Task Ausgeschaltet_Kein_Abruf_Und_Zustand_Disabled()
    {
        var calls = 0;
        var service = Service(new MonthlyReviewOptions(), () => Elements(SampleJson), () => DateTime.Now, () => calls++);

        var snapshot = await service.GetAsync();

        Assert.Equal(MonthlyReviewQmState.Disabled, snapshot.State);
        Assert.Equal(0, calls);
        Assert.False(new MonthlyReviewOptions().QmEnabled);   // Vorgabe aus, bis der Transport in P76 ist
    }

    [Fact]
    public async Task Eingeschaltet_Liefert_Faelle_Und_Merkt_Erfolg()
    {
        var calls = 0;
        var now = new DateTime(2026, 11, 5, 8, 0, 0);
        var service = Service(new MonthlyReviewOptions { QmEnabled = true, QmCacheMinutes = 15 }, () => Elements(SampleJson), () => now, () => calls++);

        var first = await service.GetAsync();
        now = now.AddMinutes(10);
        var second = await service.GetAsync();
        now = now.AddMinutes(10);
        var third = await service.GetAsync();

        Assert.Equal(MonthlyReviewQmState.Ok, first.State);
        Assert.Equal(2, first.Cases.Count);
        Assert.Equal(5, first.RowsDelivered);
        Assert.Equal(new DateOnly(2026, 11, 5), first.Reference);
        Assert.Same(first, second);
        Assert.NotSame(first, third);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task Fehler_Wird_Panel_Statt_Ausnahme_Und_Nur_Kurz_Gemerkt()
    {
        var calls = 0;
        var now = new DateTime(2026, 11, 5, 8, 0, 0);
        var service = Service(new MonthlyReviewOptions { QmEnabled = true, QmErrorCacheSeconds = 60 },
            () => throw new HttpRequestException("SAP OData QmMeldungSet: HTTP 404 Not Found (Set vermutlich noch nicht in diesem SAP-System)."),
            () => now, () => calls++);

        var first = await service.GetAsync();
        now = now.AddSeconds(30);
        await service.GetAsync();
        now = now.AddSeconds(61);
        await service.GetAsync();

        Assert.Equal(MonthlyReviewQmState.Error, first.State);
        Assert.Contains("404", first.Error);
        Assert.Empty(first.Cases);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task Leere_Antwort_Ergibt_Zustand_Empty()
    {
        var service = Service(new MonthlyReviewOptions { QmEnabled = true }, () => Elements("""{"d":{"results":[]}}"""), () => DateTime.Now);

        var snapshot = await service.GetAsync();

        Assert.Equal(MonthlyReviewQmState.Empty, snapshot.State);
        Assert.Equal(0, snapshot.RowsDelivered);
    }

    [Fact]
    public async Task Seitenweise_Lesen_Stoppt_Wenn_Top_Ignoriert_Wird_Und_Setzt_Filter()
    {
        var page = "{\"d\":{\"results\":[" + string.Join(",", Enumerable.Range(1, 5000).Select(i => $"{{\"Qmnum\":\"{i}\",\"Qmart\":\"Q1\"}}")) + "]}}";
        var urls = new List<string>();

        var rows = await MonthlyReviewQmService.ReadAllAsync((url, _) =>
        {
            urls.Add(url);
            return Task.FromResult(page);
        }, "https://sap/sap/opu/odata/sap/ZPOWERBI_EINKAUF_SRV/", new MonthlyReviewOptions { QmFilter = "Qmart eq 'Q1'" }, CancellationToken.None);

        Assert.Equal(5000, rows.Count);
        Assert.Equal(2, urls.Count);                     // zweite Seite brachte nichts Neues: Ende
        Assert.StartsWith("https://sap/sap/opu/odata/sap/ZPOWERBI_EINKAUF_SRV/QmMeldungSet?$format=json&$top=5000&$skip=0", urls[0]);
        Assert.Contains("$skip=5000", urls[1]);
        Assert.Contains("$filter=Qmart%20eq%20%27Q1%27", urls[0]);

        var small = await MonthlyReviewQmService.ReadAllAsync((url, _) => Task.FromResult(SampleJson), "https://sap/x", new MonthlyReviewOptions(), CancellationToken.None);
        Assert.Equal(4, small.Count);                    // eine Seite, doppelte Meldung einmal
    }
}
