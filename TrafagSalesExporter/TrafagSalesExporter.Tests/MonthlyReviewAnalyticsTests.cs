using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Monthly Review Gesellschaften, Phase 1 (2026-11-05): Berichtsmonat aus den Daten statt festem Oktober, Veraenderungen aus
/// exakten Monatssummen, Prozent nur gegen positive Basis, Vorjahr vor Datenbeginn = fehlend (Spezifikation Abschnitt 6).
/// </summary>
public sealed class MonthlyReviewAnalyticsTests
{
    private static SalesFact Fact(DateOnly date, decimal chf, string tsc = "TRCH", string customer = "Kunde A")
        => new(date, tsc, tsc == "TRCH" ? "CH" : "IT", SalesAnalytics.CustomerKey(customer, tsc, "1"), customer, "DE",
            "M1", "Artikel", "Transmitters", chf, 1, "R-" + tsc + date.DayNumber);

    /// <summary>Je Monat ein Beleg am 15., Wert aus der Funktion (Jahr, Monat).</summary>
    private static List<SalesFact> Monthly(DateOnly from, DateOnly toExclusive, Func<int, int, decimal> value, string tsc = "TRCH", string customer = "Kunde A")
    {
        var list = new List<SalesFact>();
        for (var m = from; m < toExclusive; m = m.AddMonths(1))
            list.Add(Fact(new DateOnly(m.Year, m.Month, 15), value(m.Year, m.Month), tsc, customer));
        return list;
    }

    private static SalesDataset Dataset(IReadOnlyList<SalesFact> facts, DateOnly refEnd, DateOnly dataStart)
        => new()
        {
            Facts = facts,
            ReferenceEnd = refEnd,
            DataStart = dataStart,
            CompareMonths = SalesAnalytics.CompareMonths(dataStart, refEnd),
            LoadedAt = DateTime.Now
        };

    [Fact]
    public void Berichtsmonat_Kommt_Aus_Dem_Referenzende_Nicht_Aus_Festem_Oktober()
    {
        var p = MonthlyReviewPeriod.From(new DateOnly(2026, 11, 1), new DateOnly(2025, 1, 1));

        Assert.Equal(new DateOnly(2026, 10, 1), p.ReportMonth);
        Assert.Equal("10.2026", p.MonthLabel);
        Assert.Equal("10.2025", p.PreviousMonthLabel);
        Assert.Equal("01–10.2026", p.YtdLabel);
        Assert.Equal("01–10.2025", p.PreviousYtdLabel);
        Assert.Equal("11–12.2026", p.ForecastMonthsLabel);
        Assert.Equal("11.2025–10.2026", p.Last12Label);
        Assert.Equal("11.2026–10.2027", p.Next12Label);
        Assert.True(p.PreviousYtdKnown);

        var march = MonthlyReviewPeriod.From(new DateOnly(2026, 4, 1), new DateOnly(2025, 1, 1));
        Assert.Equal("03.2026", march.MonthLabel);
        Assert.Equal("01–03.2026", march.YtdLabel);
        Assert.Equal("04–12.2026", march.ForecastMonthsLabel);
    }

    [Fact]
    public void Jahreswechsel_Dezember_Ohne_Prognosemonate_Januar_Neues_Jahr()
    {
        var dec = MonthlyReviewPeriod.From(new DateOnly(2027, 1, 1), new DateOnly(2025, 1, 1));
        Assert.Equal(2026, dec.Year);
        Assert.Equal("12.2026", dec.MonthLabel);
        Assert.Equal("01–12.2026", dec.YtdLabel);
        Assert.False(dec.HasForecastMonths);
        Assert.Equal(string.Empty, dec.ForecastMonthsLabel);

        var jan = MonthlyReviewPeriod.From(new DateOnly(2027, 2, 1), new DateOnly(2025, 1, 1));
        Assert.Equal(2027, jan.Year);
        Assert.Equal("01.2027", jan.MonthLabel);
        Assert.Equal("01–01.2027", jan.YtdLabel);
        Assert.Equal(1, jan.MonthsYtd);
    }

    [Fact]
    public void Prozent_Nur_Gegen_Positive_Basis()
    {
        Assert.Equal(10m, MonthlyReviewAnalytics.Pct(110m, 100m));
        Assert.Equal(-25m, MonthlyReviewAnalytics.Pct(75m, 100m));
        // Spezifikation 6.2 Nr. 14: Basis 0 ist "–", nicht 0 %.
        Assert.Null(MonthlyReviewAnalytics.Pct(5m, 0m));
        // Nr. 15: negative Basis (Gutschriftenmonat) ergibt kein sinnloses Vorzeichen.
        Assert.Null(MonthlyReviewAnalytics.Pct(5m, -91011m));
        Assert.Null(MonthlyReviewAnalytics.Pct(5m, null));
    }

    [Fact]
    public void Monat_YTD_Und_Vorjahr_Aus_Exakten_Monatssummen()
    {
        // 2025: 1'000 je Monat, 2026: 1'333.33 je Monat; Berichtsmonat 10.2026, Daten ab 01.2025.
        var facts = Monthly(new DateOnly(2025, 1, 1), new DateOnly(2026, 11, 1), (y, _) => y == 2025 ? 1000m : 1333.33m);
        var p = MonthlyReviewPeriod.From(new DateOnly(2026, 11, 1), new DateOnly(2025, 1, 1));

        var s = MonthlyReviewAnalytics.Scope("TRCH", facts, p);

        Assert.Equal(1333.33m, s.Month);
        Assert.Equal(1000m, s.MonthPrevious);
        Assert.Equal(13333.3m, s.Ytd);
        Assert.Equal(10000m, s.YtdPrevious);
        // Aus den exakten Summen (Spezifikation 6.2 Nr. 16), nicht aus gerundeten Mio-Werten.
        Assert.Equal((13333.3m - 10000m) / 10000m * 100m, s.YtdChange);
        Assert.Equal(33.333m, s.MonthChange);
        // 12 Monate 11.2025 bis 10.2026: zwei Monate 2025 und zehn Monate 2026.
        Assert.Equal(2 * 1000m + 10 * 1333.33m, s.Last12);
        Assert.Equal(12, s.Spark.Count);
        Assert.Equal(1333.33m, s.Spark[^1]);
    }

    [Fact]
    public void Nach_Dem_Berichtsmonat_Prognose_Davor_Ist_Und_Jahresende_Ist_Plus_Prognose()
    {
        var facts = Monthly(new DateOnly(2025, 1, 1), new DateOnly(2026, 11, 1), (y, _) => y == 2025 ? 1000m : 1100m);
        var p = MonthlyReviewPeriod.From(new DateOnly(2026, 11, 1), new DateOnly(2025, 1, 1));

        var s = MonthlyReviewAnalytics.Scope("TRCH", facts, p);

        Assert.All(Enumerable.Range(0, 10), i => Assert.NotNull(s.Actual[i]));
        Assert.All(Enumerable.Range(0, 10), i => Assert.Null(s.Forecast[i]));
        Assert.Null(s.Actual[10]);
        Assert.Null(s.Actual[11]);
        // November und Dezember: Vorjahresmonat mal Wachstum YTD (1.1).
        Assert.Equal(1100m, s.Forecast[10]);
        Assert.Equal(1100m, s.Forecast[11]);
        Assert.Equal(s.Ytd + 2200m, s.ProjectedYear);
        Assert.Equal(12000m, s.PreviousYear);
        Assert.Equal(10m, Math.Round(s.YearChange!.Value, 6));
    }

    [Fact]
    public void Vorjahr_Vor_Dem_Datenbeginn_Ist_Fehlend_Nicht_Null()
    {
        // Daten erst ab 06.2025: Vorjahresmonat 03.2025 und Vorjahres-YTD fehlen.
        var facts = Monthly(new DateOnly(2025, 6, 1), new DateOnly(2026, 4, 1), (_, _) => 500m);
        var p = MonthlyReviewPeriod.From(new DateOnly(2026, 4, 1), new DateOnly(2025, 6, 1));

        var s = MonthlyReviewAnalytics.Scope("TRCH", facts, p);

        Assert.Null(s.MonthPrevious);
        Assert.Null(s.MonthChange);
        Assert.Null(s.YtdPrevious);
        Assert.Null(s.YtdChange);
        Assert.False(s.Last12Complete);
        Assert.Null(s.Forecast12Change);
        Assert.All(Enumerable.Range(0, 5), i => Assert.Null(s.Previous[i]));
        Assert.Equal(500m, s.Previous[5]);
    }

    [Fact]
    public void Bester_Und_Schwaechster_Monat_Nur_Aus_Ist()
    {
        var facts = Monthly(new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 1), (_, m) => m switch { 1 => 300m, 2 => 900m, _ => 100m });
        var p = MonthlyReviewPeriod.From(new DateOnly(2026, 4, 1), new DateOnly(2026, 1, 1));

        var s = MonthlyReviewAnalytics.Scope("TRCH", facts, p);

        Assert.Equal((1, 900m), s.BestMonth!.Value);
        Assert.Equal((2, 100m), s.WorstMonth!.Value);
    }

    [Fact]
    public void Build_Gruppe_Ist_Summe_Der_Gesellschaften_Und_Reihenfolge_Wie_Entwurf()
    {
        var facts = new List<SalesFact>();
        facts.AddRange(Monthly(new DateOnly(2025, 1, 1), new DateOnly(2026, 11, 1), (_, _) => 200m, "TRIT", "Kunde Italien"));
        facts.AddRange(Monthly(new DateOnly(2025, 1, 1), new DateOnly(2026, 11, 1), (_, _) => 1000m, "TRCH", "Kunde A"));
        facts.AddRange(Monthly(new DateOnly(2025, 1, 1), new DateOnly(2026, 11, 1), (_, _) => 50m, "TRXX", "Kunde A"));
        var data = Dataset(facts, new DateOnly(2026, 11, 1), new DateOnly(2025, 1, 1));

        var r = MonthlyReviewAnalytics.Build(data);

        Assert.Equal(new[] { "TRCH", "TRIT", "TRXX" }, r.Companies.Select(c => c.Code));
        Assert.Equal(r.Companies.Sum(c => c.Ytd), r.Group.Ytd);
        Assert.Equal(12500m, r.Group.Ytd);
        Assert.Equal("10.2026", r.Period.MonthLabel);
        // Kunde A kauft bei TRCH und TRXX: zwei Kunden, einer bei mehreren Gesellschaften.
        Assert.Equal(2, r.Customers.CustomersLast12);
        Assert.Equal(1, r.Customers.MultiCompanyCustomers);
        Assert.Single(r.Customers.Top("TRIT"));
        Assert.Equal("Kunde Italien", r.Customers.Top("TRIT")[0].Name);
        Assert.NotNull(r.Company("trit"));
    }
}
