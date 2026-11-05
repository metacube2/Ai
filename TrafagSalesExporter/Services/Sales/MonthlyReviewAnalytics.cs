using System.Globalization;

namespace TrafagSalesExporter.Services;

// Verkauf, Unterreiter "Monthly Review Gesellschaften" (Auftrag Ingo 2026-11-04, Entwurf Patrick Hertach), Phase 1:
// Gruppen- und Gesellschaftsansicht live aus denselben Verkaufszeilen wie der Reiter Verkauf (Finance-Regeln, ohne
// Konzernkunden, CHF zum Kurs des Belegdatums). Doku docs/monthly_review/MONTHLY_REVIEW_2026-11-04.md.
//
// Bewusst anders als der Entwurf maske.html (Spezifikation Abschnitt 6):
// - Berichtsmonat, YTD, Vorjahresmonat und Prognosemonate kommen aus SalesDataset.ReferenceEnd (letzter vollstaendiger
//   Monat), nie aus festen Indizes oder einem festen Oktober (6.1 Nr. 1 bis 3).
// - Veraenderungen werden aus den exakten Monatssummen gerechnet, nicht aus gerundeten Mio-Feldern (6.2 Nr. 16).
// - Prozent nur gegen eine positive Basis; Basis 0 oder negativ ergibt "keine Angabe" statt 0 % oder eines sinnlosen
//   Vorzeichens (6.2 Nr. 14 und 15).
// - Vorjahreswerte vor dem Datenbeginn sind fehlende Daten, keine Nullumsaetze (LEARNINGS 2026-10-02, +57 %).

/// <summary>Berichtszeitraum der Monthly Review, abgeleitet aus dem Ende des letzten vollstaendigen Monats.</summary>
public sealed record MonthlyReviewPeriod(DateOnly ReportMonth, DateOnly ReferenceEnd, DateOnly DataStart)
{
    public int Year => ReportMonth.Year;
    public DateOnly YearStart => new(Year, 1, 1);
    public int MonthsYtd => ReportMonth.Month;
    public DateOnly PreviousMonth => ReportMonth.AddYears(-1);
    /// <summary>Vorjahresmonat liegt ganz in den Daten.</summary>
    public bool PreviousMonthKnown => PreviousMonth >= DataStart;
    /// <summary>Vorjahres-YTD liegt ganz in den Daten (Januar des Vorjahrs ab Datenbeginn).</summary>
    public bool PreviousYtdKnown => YearStart.AddYears(-1) >= DataStart;
    /// <summary>Die letzten 12 Monate liegen ganz in den Daten.</summary>
    public bool Last12Known => ReferenceEnd.AddMonths(-12) >= DataStart;
    public bool HasForecastMonths => ReportMonth.Month < 12;

    public string MonthLabel => ReportMonth.ToString("MM.yyyy", CultureInfo.InvariantCulture);
    public string PreviousMonthLabel => PreviousMonth.ToString("MM.yyyy", CultureInfo.InvariantCulture);
    public string YtdLabel => $"01–{ReportMonth:MM}.{Year}";
    public string PreviousYtdLabel => $"01–{ReportMonth:MM}.{Year - 1}";
    /// <summary>Restmonate des Jahres mit statistischer Prognose, z. B. "11–12.2026"; leer im Dezember.</summary>
    public string ForecastMonthsLabel => HasForecastMonths ? $"{ReportMonth.AddMonths(1):MM}–12.{Year}" : string.Empty;
    public string Last12Label => $"{ReferenceEnd.AddMonths(-12):MM.yyyy}–{ReportMonth:MM.yyyy}";
    public string Next12Label => $"{ReferenceEnd:MM.yyyy}–{ReferenceEnd.AddMonths(11):MM.yyyy}";

    public static MonthlyReviewPeriod From(DateOnly referenceEnd, DateOnly dataStart)
    {
        var refEnd = new DateOnly(referenceEnd.Year, referenceEnd.Month, 1);
        return new MonthlyReviewPeriod(refEnd.AddMonths(-1), refEnd, dataStart);
    }
}

/// <summary>Kennzahlen einer Sicht (Gruppe oder Gesellschaft) fuer Kacheln, Scorecard und Monatsgrafik.</summary>
public sealed record MonthlyReviewScopeStats(
    string Code,
    decimal Month,
    decimal? MonthPrevious,
    decimal Ytd,
    decimal? YtdPrevious,
    decimal Last12,
    bool Last12Complete,
    decimal ProjectedYear,
    decimal? PreviousYear,
    decimal Forecast12,
    // 12 Monate des Berichtsjahrs: Ist bis zum Berichtsmonat, danach null.
    IReadOnlyList<decimal?> Actual,
    // 12 Monate des Berichtsjahrs: statistische Prognose nach dem Berichtsmonat, davor null.
    IReadOnlyList<decimal?> Forecast,
    // 12 Monate des Vorjahrs; null, wo der Monat vor dem Datenbeginn liegt.
    IReadOnlyList<decimal?> Previous,
    // Die letzten 12 Monate bis zum Berichtsmonat (Verlaufslinie).
    IReadOnlyList<decimal> Spark)
{
    public decimal? MonthChange => MonthlyReviewAnalytics.Pct(Month, MonthPrevious);
    public decimal? YtdChange => MonthlyReviewAnalytics.Pct(Ytd, YtdPrevious);
    public decimal? YearChange => MonthlyReviewAnalytics.Pct(ProjectedYear, PreviousYear);
    public decimal? Forecast12Change => Last12Complete ? MonthlyReviewAnalytics.Pct(Forecast12, Last12) : null;

    /// <summary>Bester und schwaechster Ist-Monat des Berichtsjahrs (bei Gleichstand der erste).</summary>
    public (int MonthIndex, decimal Value)? BestMonth => Extreme(true);
    public (int MonthIndex, decimal Value)? WorstMonth => Extreme(false);

    private (int, decimal)? Extreme(bool best)
    {
        (int, decimal)? hit = null;
        for (var i = 0; i < Actual.Count; i++)
        {
            if (Actual[i] is not { } v) continue;
            if (hit is null || (best ? v > hit.Value.Item2 : v < hit.Value.Item2))
                hit = (i, v);
        }
        return hit;
    }
}

/// <summary>Kunden der Gruppe (12 Monate) und Kundenbewegung je Quartal.</summary>
public sealed record MonthlyReviewCustomerView(
    int CustomersLast12,
    int MultiCompanyCustomers,
    IReadOnlyList<SalesCustomerSummary> Customers,
    IReadOnlyList<SalesQuarterMovement> Movements)
{
    /// <summary>Juengstes Quartal, fuer das Verluste schon bekannt sind (6 Monate nach Quartalsende).</summary>
    public SalesQuarterMovement? LatestKnownQuarter => Movements.LastOrDefault(m => m.LostKnown);

    /// <summary>Top-Kunden mit Beteiligung der Gesellschaft (null = Gruppe), nach Umsatz 12 Monate.</summary>
    public IReadOnlyList<SalesCustomerSummary> Top(string? company, int count = 10)
        => Customers.Where(c => c.Last12 > 0 && (company is null || c.Companies.Contains(company, StringComparer.OrdinalIgnoreCase)))
            .OrderByDescending(c => c.Last12)
            .Take(count)
            .ToList();
}

/// <summary>Fertig gerechnete Monthly Review fuer die Seite.</summary>
public sealed record MonthlyReviewResult(
    MonthlyReviewPeriod Period,
    MonthlyReviewScopeStats Group,
    IReadOnlyList<MonthlyReviewScopeStats> Companies,
    MonthlyReviewCustomerView Customers)
{
    public MonthlyReviewScopeStats? Company(string code)
        => Companies.FirstOrDefault(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase));
}

/// <summary>Reine Rechnung der Monthly Review (Phase 1), testbar ohne Daten.</summary>
public static class MonthlyReviewAnalytics
{
    public const string GroupCode = "GRP";

    /// <summary>Reihenfolge der BI-Gesellschaften wie im Entwurf; weitere Gesellschaften in den Daten folgen alphabetisch.</summary>
    public static readonly IReadOnlyList<string> CompanyOrder = ["TRCH", "TRIN", "TRIT", "TRUS", "TRUK", "TRES", "TRAT", "TRDE", "TRFR"];

    /// <summary>Anzeigename je Gesellschaft (Deutsch, Englisch); unbekannte Codes zeigen nur den Code.</summary>
    public static readonly IReadOnlyDictionary<string, (string German, string English)> CompanyNames =
        new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            [GroupCode] = ("Gruppe", "Group"),
            ["TRCH"] = ("Schweiz", "Switzerland"),
            ["TRIN"] = ("Indien", "India"),
            ["TRIT"] = ("Italien", "Italy"),
            ["TRUS"] = ("USA", "USA"),
            ["TRUK"] = ("Grossbritannien", "United Kingdom"),
            ["TRES"] = ("Spanien", "Spain"),
            ["TRAT"] = ("Österreich", "Austria"),
            ["TRDE"] = ("Deutschland", "Germany"),
            ["TRFR"] = ("Frankreich", "France"),
        };

    /// <summary>Veraenderung in Prozent; nur gegen eine positive Basis, sonst null ("–").</summary>
    public static decimal? Pct(decimal value, decimal? basis)
        => basis is { } b && b > 0 ? (value - b) / b * 100m : null;

    public static MonthlyReviewResult Build(SalesDataset data)
    {
        var period = MonthlyReviewPeriod.From(data.ReferenceEnd, data.DataStart);
        var facts = data.Facts;
        var group = Scope(GroupCode, facts, period, data.CompareMonths);
        var companies = facts.GroupBy(f => f.Tsc, StringComparer.OrdinalIgnoreCase)
            .Select(g => Scope(g.Key.ToUpperInvariant(), g.ToList(), period, data.CompareMonths))
            .OrderBy(c => Order(c.Code))
            .ThenBy(c => c.Code, StringComparer.Ordinal)
            .ToList();
        var customers = SalesAnalytics.Customers(facts, period.ReferenceEnd, data.CompareMonths);
        var movements = SalesAnalytics.Movements(facts, period.ReferenceEnd, 6, data.DataStart);
        var view = new MonthlyReviewCustomerView(
            customers.Count(c => c.Last12 > 0),
            customers.Count(c => c.Last12 > 0 && c.Companies.Count > 1),
            customers,
            movements);
        return new MonthlyReviewResult(period, group, companies, view);
    }

    private static int Order(string code)
    {
        for (var i = 0; i < CompanyOrder.Count; i++)
            if (string.Equals(CompanyOrder[i], code, StringComparison.OrdinalIgnoreCase))
                return i;
        return CompanyOrder.Count;
    }

    /// <summary>Kennzahlen einer Sicht. <paramref name="facts"/> sind schon auf die Sicht gefiltert.</summary>
    public static MonthlyReviewScopeStats Scope(string code, IReadOnlyList<SalesFact> facts, MonthlyReviewPeriod period, int compareMonths = 12)
    {
        var refEnd = period.ReferenceEnd;
        var monthly = facts.Where(f => f.Date < refEnd)
            .GroupBy(f => new DateOnly(f.Date.Year, f.Date.Month, 1))
            .ToDictionary(g => g.Key, g => g.Sum(f => f.ValueChf));
        decimal Month(DateOnly m) => monthly.GetValueOrDefault(m);
        decimal? Known(DateOnly m) => m >= period.DataStart ? Month(m) : null;

        var actual = new List<decimal?>(12);
        var previous = new List<decimal?>(12);
        decimal ytd = 0, ytdPrev = 0;
        for (var i = 0; i < 12; i++)
        {
            var m = period.YearStart.AddMonths(i);
            actual.Add(m <= period.ReportMonth ? Month(m) : null);
            previous.Add(Known(m.AddYears(-1)));
            if (m <= period.ReportMonth)
            {
                ytd += Month(m);
                ytdPrev += Month(m.AddYears(-1));
            }
        }

        var projection = ControllingAnalytics.Projection(code, facts, refEnd, period.DataStart);
        var forecast = projection.Forecast.Select((v, i) => period.YearStart.AddMonths(i) > period.ReportMonth ? (decimal?)v : null).ToList();
        var forecast12 = SalesAnalytics.Forecast(code, facts, refEnd, period.DataStart, compareMonths).Forecast.Sum(v => v.Value);
        var spark = Enumerable.Range(0, 12).Select(i => Month(refEnd.AddMonths(-12 + i))).ToList();

        return new MonthlyReviewScopeStats(
            code,
            Month(period.ReportMonth),
            period.PreviousMonthKnown ? Month(period.PreviousMonth) : null,
            ytd,
            period.PreviousYtdKnown ? ytdPrev : null,
            spark.Sum(),
            period.Last12Known,
            ytd + projection.Rest,
            projection.PreviousYear,
            forecast12,
            actual,
            forecast,
            previous,
            spark);
    }
}
