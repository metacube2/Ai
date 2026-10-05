using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public sealed class SalesAnalyticsTests
{
    private static readonly DateOnly RefEnd = new(2026, 10, 1);

    private static SalesFact Fact(string customer, DateOnly date, decimal chf, string division = "Transmitters", string country = "DE",
        string tsc = "TRCH", string material = "M1", decimal qty = 1)
        => new(date, tsc, tsc == "TRCH" ? "CH" : "IT", customer, customer, country, material, "Artikel " + material, division, chf, qty, "R-" + date.DayNumber);

    [Theory]
    [InlineData("Siemens AG", "SIEMENS")]
    [InlineData("Siemens Schweiz AG", "SIEMENS SCHWEIZ")]
    [InlineData("ABB S.p.A.", "ABB")]
    [InlineData("Müller & Co. GmbH", "MULLER AND")]
    public void Kundenschluessel_Ohne_Rechtsform_Und_Akzente(string name, string expected)
        => Assert.Equal(expected, SalesAnalytics.CustomerKey(name, "TRCH", "1"));

    [Fact]
    public void Kundenschluessel_Ohne_Name_Und_Nummer_Hat_Erkennbaren_Sammelschluessel_Je_Gesellschaft()
    {
        var a = SalesAnalytics.CustomerKey("", "TRIT", "");
        var b = SalesAnalytics.CustomerKey(null, "TRCH", null);

        Assert.Equal("TRIT#(ohne Nummer)", a);
        Assert.NotEqual(a, b);
        Assert.NotEqual("TRIT#", a);
    }

    [Fact]
    public void Datenbeginn_Angebrochener_Monat_Zaehlt_Nicht()
    {
        var facts = new[] { Fact("A", new DateOnly(2025, 1, 6), 10, tsc: "TRCH"), Fact("B", new DateOnly(2025, 1, 20), 10, tsc: "TRIT") };

        Assert.Equal(new DateOnly(2025, 2, 1), SalesAnalytics.DataStart(facts));
    }

    [Theory]
    [InlineData("UK", "GB")]
    [InlineData("el", "GR")]
    [InlineData(" de ", "DE")]
    [InlineData(null, "")]
    public void Kundenland_Wird_Vereinheitlicht(string? raw, string expected)
        => Assert.Equal(expected, SalesAnalytics.NormalizeCountry(raw));

    [Fact]
    public void Referenz_Monat_Mit_Wochenende_Am_Ende_Ist_Vollstaendig_Am_Letzten_Werktag()
    {
        // 31.5.2026 ist ein Sonntag, letzter Werktag Freitag 29.5.
        Assert.Equal(new DateOnly(2026, 5, 29), SalesAnalytics.LastBusinessDayOfMonth(new DateOnly(2026, 5, 10)));
        Assert.Equal(new DateOnly(2026, 6, 1), SalesAnalytics.ReferenceEnd([Fact("A", new DateOnly(2026, 5, 29), 1)], new DateOnly(2026, 6, 2)));
        Assert.Equal(new DateOnly(2026, 5, 1), SalesAnalytics.ReferenceEnd([Fact("A", new DateOnly(2026, 5, 28), 1)], new DateOnly(2026, 6, 2)));
    }

    [Fact]
    public void Erster_Und_Letzter_Kauf_Ohne_Gutschriften()
    {
        var facts = new[]
        {
            Fact("A", new DateOnly(2025, 1, 5), -20), Fact("A", new DateOnly(2025, 3, 5), 100),
            Fact("A", new DateOnly(2026, 2, 5), 50), Fact("A", new DateOnly(2026, 8, 5), -10)
        };

        var a = SalesAnalytics.Customers(facts, RefEnd).Single();

        Assert.Equal(new DateOnly(2025, 3, 5), a.FirstDate);
        Assert.Equal(new DateOnly(2026, 2, 5), a.LastDate);
    }

    [Fact]
    public void Konzentration_Ohne_Umsatz_Ist_Leer_Und_Ohne_Unendlich()
    {
        var c = SalesAnalytics.Concentration([Fact("A", new DateOnly(2026, 5, 1), -5)], RefEnd);

        Assert.Equal(0, c.Customers);
        Assert.Equal(0, c.CustomersFor80);
        Assert.Equal(0, c.Top1);
        Assert.Equal(0, c.Hhi);
    }

    [Fact]
    public void Kundenschluessel_Ohne_Name_Aus_Gesellschaft_Und_Nummer()
        => Assert.Equal("TRIT#4711", SalesAnalytics.CustomerKey("", "trit", "4711"));

    [Fact]
    public void Referenz_Ist_Ende_Des_Letzten_Vollstaendigen_Monats()
    {
        Assert.Equal(new DateOnly(2026, 9, 1), SalesAnalytics.ReferenceEnd([Fact("A", new DateOnly(2026, 9, 17), 1)], new DateOnly(2026, 10, 2)));
        Assert.Equal(new DateOnly(2026, 10, 1), SalesAnalytics.ReferenceEnd([Fact("A", new DateOnly(2026, 9, 30), 1)], new DateOnly(2026, 10, 2)));
    }

    [Fact]
    public void Kunden_Zwoelf_Monate_Und_Vorjahr()
    {
        var facts = new[]
        {
            Fact("A", new DateOnly(2026, 3, 1), 100, tsc: "TRCH"),
            Fact("A", new DateOnly(2025, 3, 1), 300, tsc: "TRIT"),
            Fact("A", new DateOnly(2026, 10, 5), 999),
            Fact("B", new DateOnly(2026, 9, 1), 50)
        };
        var a = SalesAnalytics.Customers(facts, RefEnd).Single(c => c.Key == "A");

        Assert.Equal(100, a.Last12);
        Assert.Equal(300, a.Previous);
        Assert.Equal(100, a.Current);
        Assert.Equal(["TRCH", "TRIT"], a.Companies);
        Assert.Equal(-66.667m, Math.Round(a.ChangePercent!.Value, 3));
    }

    [Fact]
    public void Rueckgang_Und_Eingeschlafen()
    {
        var facts = new[]
        {
            Fact("Fall", new DateOnly(2025, 5, 1), 20000), Fact("Fall", new DateOnly(2026, 8, 1), 10000),
            Fact("Schlaf", new DateOnly(2025, 5, 1), 20000), Fact("Schlaf", new DateOnly(2025, 12, 1), 3000),
            Fact("Gut", new DateOnly(2025, 5, 1), 20000), Fact("Gut", new DateOnly(2026, 8, 1), 19000),
            Fact("Klein", new DateOnly(2025, 5, 1), 500)
        };
        var d = SalesAnalytics.Declines(SalesAnalytics.Customers(facts, RefEnd));

        Assert.Equal(2, d.Count);
        Assert.Equal("Schlaf", d[0].Customer.Key);
        Assert.Equal("eingeschlafen", d[0].Kind);
        Assert.Equal(17000, d[0].LostChf);
        Assert.Equal("rueckgang", d[1].Kind);
    }

    [Fact]
    public void Neue_Und_Verlorene_Kunden_Je_Quartal()
    {
        var facts = new[]
        {
            Fact("Alt", new DateOnly(2023, 1, 10), 10), Fact("Alt", new DateOnly(2024, 2, 1), 40),
            Fact("Neu", new DateOnly(2025, 4, 5), 70), Fact("Neu", new DateOnly(2026, 9, 1), 10)
        };
        var moves = SalesAnalytics.Movements(facts, RefEnd);

        var q1 = moves.Single(m => m.Quarter == "2024 Q1");
        Assert.Equal(1, q1.LostCustomers);
        Assert.True(q1.LostFinal);
        Assert.Equal(40, q1.LostRevenue12);
        var q2 = moves.Single(m => m.Quarter == "2025 Q2");
        Assert.Equal(1, q2.NewCustomers);
        Assert.Equal(70, q2.NewRevenue12);
        Assert.DoesNotContain(moves, m => m.Quarter == "2026 Q4");
    }

    [Fact]
    public void Konzentration_Top_Anteile_Und_80_Prozent()
    {
        var facts = new[] { Fact("A", new DateOnly(2026, 5, 1), 700), Fact("B", new DateOnly(2026, 5, 1), 200), Fact("C", new DateOnly(2026, 5, 1), 100) };
        var c = SalesAnalytics.Concentration(facts, RefEnd);

        Assert.Equal(0.7, c.Top1, 3);
        Assert.Equal(2, c.CustomersFor80);
        Assert.Equal(49 * 100 + 4 * 100 + 1 * 100, c.Hhi, 0);
        Assert.Equal(3, c.Customers);
    }

    [Fact]
    public void Cross_Selling_Regel_Und_Ansatz()
    {
        var facts = new List<SalesFact>();
        for (var i = 0; i < 6; i++)
        {
            facts.Add(Fact("K" + i, new DateOnly(2026, 2, 1), 100, "Transmitters"));
            facts.Add(Fact("K" + i, new DateOnly(2026, 2, 1), 50, "Pressure Switches"));
        }
        // Kunden nur mit Thermostaten senken die Grundrate von Pressure Switches, damit der Lift ueber 1 liegt.
        for (var i = 0; i < 3; i++)
            facts.Add(Fact("T" + i, new DateOnly(2026, 2, 1), 80, "Thermostats"));
        facts.Add(Fact("Ziel", new DateOnly(2026, 2, 1), 5000, "Transmitters"));
        facts.Add(Fact("Ziel", new DateOnly(2026, 2, 1), 10, "Others"));

        var rule = SalesAnalytics.CrossSellRules(facts, RefEnd).Single(r => r.From == "Transmitters" && r.To == "Pressure Switches");
        Assert.Equal(6, rule.Both);
        Assert.Equal(6.0 / 7, rule.Confidence, 3);
        var opp = Assert.Single(SalesAnalytics.CrossSellOpportunities(facts, RefEnd));
        Assert.Equal("Ziel", opp.CustomerKey);
        Assert.Equal("Pressure Switches", opp.Division);
        Assert.Equal(50, opp.TypicalRevenue);
    }

    [Fact]
    public void Preisstreuung_Je_Artikel_Mit_Perzentilen()
    {
        var facts = new[]
        {
            Fact("A", new DateOnly(2026, 5, 1), 100, qty: 10), Fact("B", new DateOnly(2026, 5, 1), 200, qty: 10),
            Fact("C", new DateOnly(2026, 5, 1), 300, qty: 10), Fact("D", new DateOnly(2026, 5, 1), 400, qty: 10),
            Fact("E", new DateOnly(2026, 5, 1), -50, qty: 1)
        };
        var s = Assert.Single(SalesAnalytics.PriceSpreads(facts, RefEnd, minCustomers: 4));

        Assert.Equal(4, s.Customers);
        Assert.Equal(10, s.Min);
        Assert.Equal(40, s.Max);
        Assert.Equal(25, s.Median);
        Assert.Equal(13m, s.P10);
    }

    [Fact]
    public void Prognose_Mit_Saison_Und_Wachstum()
    {
        var facts = new List<SalesFact>();
        foreach (var year in new[] { 2023, 2024, 2025 })
        for (var m = 1; m <= 12; m++)
        {
            var date = new DateOnly(year, m, 1).AddMonths(9);
            var factor = year == 2025 ? 1.1m : year == 2024 ? 1.0m : 1.0m;
            facts.Add(Fact("A", date, (m == 3 ? 200 : 100) * factor));
        }
        var f = SalesAnalytics.Forecast("*", facts, RefEnd);

        Assert.Equal(36, f.History.Count);
        Assert.Equal(12, f.Forecast.Count);
        Assert.Equal(new DateOnly(2026, 10, 1), f.Forecast[0].Month);
        Assert.True(f.SeasonIndex.Max() > 1.5);
        Assert.Equal(f.History[24].Value * 1.1m, f.Forecast[0].Value, 2);
    }

    [Fact]
    public void Laender_Und_Warenfluss()
    {
        var facts = new[]
        {
            Fact("A", new DateOnly(2026, 5, 1), 100, country: "DE", tsc: "TRCH"),
            Fact("B", new DateOnly(2026, 5, 1), 50, country: "CH", tsc: "TRCH"),
            Fact("C", new DateOnly(2026, 5, 1), 30, country: "", tsc: "TRCH")
        };
        var countries = SalesAnalytics.Countries(facts, RefEnd);
        Assert.Equal(["DE", "CH"], countries.Select(c => c.Country));
        Assert.Equal(24, countries[0].Monthly.Count);
        var flow = Assert.Single(SalesAnalytics.Flows(facts, RefEnd));
        Assert.Equal(("CH", "DE", 100m), (flow.FromCountry, flow.ToCountry, flow.Last12));
    }

    [Fact]
    public void Datenbeginn_Und_Vergleich_Gleicher_Zeitraeume()
    {
        var facts = new[] { Fact("A", new DateOnly(2025, 1, 6), 10, tsc: "TRCH"), Fact("B", new DateOnly(2025, 1, 5), 10, tsc: "TRIT"), Fact("C", new DateOnly(2026, 9, 3), 10) };
        var start = SalesAnalytics.DataStart(facts);
        Assert.Equal(new DateOnly(2025, 1, 1), start);
        // Jan 2025 bis Sep 2026 = 21 Monate, davon 9 vergleichbar: Jan-Sep 2026 gegen Jan-Sep 2025.
        // Jan 2025 bis Sep 2026 = 21 Monate, davon 9 vergleichbar: Jan-Sep 2026 gegen Jan-Sep 2025.
        Assert.Equal(9, SalesAnalytics.CompareMonths(start, RefEnd));
        Assert.Equal(12, SalesAnalytics.CompareMonths(new DateOnly(2023, 1, 1), RefEnd));

        var c = SalesAnalytics.Customers([Fact("X", new DateOnly(2025, 2, 1), 100), Fact("X", new DateOnly(2025, 11, 1), 999), Fact("X", new DateOnly(2026, 2, 1), 150)], RefEnd, 9).Single();
        Assert.Equal(150, c.Current);
        Assert.Equal(100, c.Previous);
        Assert.Equal(50m, c.ChangePercent);
    }

    [Fact]
    public void Prognose_Ohne_Monate_Vor_Datenbeginn_Und_Ohne_Rueckrechnung()
    {
        var facts = new List<SalesFact>();
        for (var m = new DateOnly(2025, 1, 1); m < RefEnd; m = m.AddMonths(1))
            facts.Add(Fact("A", m, m.Year == 2026 ? 120 : 100));
        var f = SalesAnalytics.Forecast("*", facts, RefEnd, new DateOnly(2025, 1, 1), 9);

        Assert.Equal(new DateOnly(2025, 1, 1), f.History[0].Month);
        Assert.Equal(21, f.History.Count);
        Assert.Null(f.BacktestMape);
        Assert.Equal(1.2m * 100, f.Forecast[0].Value);
    }

    [Fact]
    public void Neu_Im_Ersten_Datenjahr_Nicht_Belastbar()
    {
        var facts = new[] { Fact("A", new DateOnly(2025, 2, 1), 10), Fact("B", new DateOnly(2026, 2, 1), 10) };
        var moves = SalesAnalytics.Movements(facts, RefEnd, dataStart: new DateOnly(2025, 1, 1));

        Assert.False(moves.Single(m => m.Quarter == "2025 Q1").NewReliable);
        Assert.True(moves.Single(m => m.Quarter == "2026 Q1").NewReliable);
    }

    [Theory]
    [InlineData("E99999", "Special device", false)]
    [InlineData("M_IT01_002303", "CERTIFICATI", false)]
    [InlineData("45226", "NAT4.0 PRESSURE TRANSMITTER", true)]
    public void Vergleichbare_Artikel_Ohne_Platzhalter_Und_Leistungen(string material, string article, bool expected)
        => Assert.Equal(expected, SalesAnalytics.IsComparableArticle(material, article));

    [Fact]
    public void Verluste_Erst_Sechs_Monate_Nach_Quartalsende()
    {
        var facts = new[] { Fact("A", new DateOnly(2025, 1, 5), 10), Fact("B", new DateOnly(2026, 8, 1), 10), Fact("C", new DateOnly(2026, 2, 1), 10) };
        var moves = SalesAnalytics.Movements(facts, RefEnd);

        Assert.False(moves.Single(m => m.Quarter == "2026 Q3").LostKnown);
        Assert.Equal(0, moves.Single(m => m.Quarter == "2026 Q3").LostCustomers);
        Assert.True(moves.Single(m => m.Quarter == "2026 Q1").LostKnown);
        Assert.Equal(1, moves.Single(m => m.Quarter == "2026 Q1").LostCustomers);
    }
}
