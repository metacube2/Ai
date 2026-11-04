using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

// HR-Cockpit, Reiter Interaktiv (2026-11-04): Rechnung der animierten Ansichten, docs/HR_KPI.md Abschnitt 9.
public sealed class HrKpiInteractiveTests
{
    private static HrLeaverRow Leaver(int pernr, string org, DateTime exit, DateTime? entry = null, bool relevant = true, string type = "Festangestellt")
        => new()
        {
            Personalnummer = pernr,
            NameVoll = $"P{pernr}",
            Organisationseinheit = org,
            Austrittsdatum = exit,
            Eintrittsdatum = entry,
            VerweildauerMonate = entry.HasValue ? Math.Round((decimal)(exit - entry.Value).TotalDays / 30.44m, 1) : null,
            Mitarbeitertyp = type,
            IstFluktuationsrelevant = relevant,
            IstArbeitnehmerkuendigung = relevant,
            FluktuationAusschlussgrund = relevant ? null : "Pensionierung",
            Austrittsjahr = exit.Year
        };

    private static HrKpiEmployeeRow Employee(int pernr, string org, DateTime? entry, decimal fte = 1m, string type = "Festangestellt")
        => new() { Personalnummer = pernr, NameVoll = $"E{pernr}", Organisationseinheit = org, Eintrittsdatum = entry, Fte = fte, Mitarbeitertyp = type, IstAktiv = true };

    private static HrAbsenceSapCase Case(int pernr, string type, DateTime von, DateTime bis, decimal days, decimal hours = 0)
        => new(pernr.ToString("00000000"), type, von, bis, "000", days, hours, (decimal)(bis - von).TotalDays + 1);

    [Fact]
    public void ExitRace_Zaehlt_Gleitend_Zwoelf_Monate_Je_Abteilung()
    {
        var leavers = new[]
        {
            Leaver(1, "A", new DateTime(2025, 1, 15)),
            Leaver(2, "A", new DateTime(2025, 6, 10)),
            Leaver(3, "B", new DateTime(2025, 12, 1)),
            Leaver(4, "B", new DateTime(2025, 12, 5), relevant: false)
        };

        var frames = HrKpiInteractive.ExitRace(leavers, new DateTime(2026, 1, 20), relevantOnly: true);

        // Erstes Bild nach zwoelf Monaten Daten (Dezember 2025), letztes im Stichtagsmonat.
        Assert.Equal(new DateTime(2025, 12, 1), frames[0].Month);
        Assert.Equal(new DateTime(2026, 1, 1), frames[^1].Month);
        Assert.Equal(3, frames[0].Total);
        // Januar 2026: Fenster Februar 2025 bis Stichtag, der Austritt vom 15.01.2025 faellt heraus.
        Assert.Equal(1, frames[^1].Bars.Single(b => b.Key == "A").Value);
        Assert.Equal(1, frames[^1].Bars.Single(b => b.Key == "B").Value);
        Assert.Equal(4, HrKpiInteractive.ExitRace(leavers, new DateTime(2025, 12, 31), relevantOnly: false)[^1].Total);
    }

    [Fact]
    public void ExitFlow_Fuehrt_Jede_Person_Ueber_Grund_Zur_Verweildauer()
    {
        var leavers = new[]
        {
            Leaver(1, "A", new DateTime(2025, 3, 1), new DateTime(2024, 9, 1)),
            Leaver(2, "A", new DateTime(2025, 3, 1), new DateTime(2015, 1, 1), relevant: false),
            Leaver(3, "B", new DateTime(2025, 3, 1))
        };

        var (nodes, links) = HrKpiInteractive.ExitFlow(leavers);

        Assert.Equal(3m, nodes.Single(n => n.Column == 0).Value);
        Assert.Equal(3m, nodes.Where(n => n.Column == 1).Sum(n => n.Value));
        Assert.Equal(3m, nodes.Where(n => n.Column == 2).Sum(n => n.Value));
        Assert.Equal(links.Where(l => l.From == "a:all").Sum(l => l.Value), links.Where(l => l.From.StartsWith("r:")).Sum(l => l.Value));
        Assert.Contains(nodes, n => n.Id == "t:" + HrKpiInteractive.TenureUnder1);
        Assert.Contains(nodes, n => n.Id == "t:" + HrKpiInteractive.TenureOver10);
        Assert.Contains(nodes, n => n.Id == "t:" + HrKpiInteractive.TenureUnknown);
    }

    [Fact]
    public void ExitMonthPattern_Zaehlt_Relevante_Austritte_Je_Kalendermonat()
    {
        var pattern = HrKpiInteractive.ExitMonthPattern(
            [Leaver(1, "A", new DateTime(2024, 3, 31)), Leaver(2, "A", new DateTime(2025, 3, 31)), Leaver(3, "A", new DateTime(2025, 12, 31), relevant: false)],
            relevantOnly: true);

        Assert.Equal(2, pattern[2]);
        Assert.Equal(0, pattern[11]);
    }

    [Fact]
    public void OrgGalaxy_Rechnet_Headcount_Und_Fluktuation_Der_Letzten_Zwoelf_Monate()
    {
        var employees = Enumerable.Range(1, 9).Select(i => Employee(i, "A", new DateTime(2020, 1, 1))).ToArray();
        var leavers = new[]
        {
            Leaver(100, "A", new DateTime(2023, 5, 31), new DateTime(2019, 1, 1)),
            Leaver(101, "A", new DateTime(2025, 6, 30), new DateTime(2019, 1, 1))
        };

        var frames = HrKpiInteractive.OrgGalaxy(employees, leavers, new DateTime(2025, 12, 31), quarters: 4);

        var last = frames[^1].Points.Single(p => p.Org == "A");
        Assert.False(frames[^1].Partial);
        Assert.Equal(9, last.Headcount);
        Assert.Equal(1, last.RelevantExits);
        // Mittlerer Headcount aus fuenf Quartalsenden: 10 (31.12.24, 31.03.25, 30.06.25) und 9 (30.09., 31.12.).
        Assert.Equal(9.6m, last.AverageHeadcount);
        Assert.Equal(1 / 9.6 * 100, last.RatePercent!.Value, 6);
    }

    [Fact]
    public void Retention_Rechnet_Kaplan_Meier_Je_Eintrittsjahr()
    {
        var anchor = new DateTime(2025, 12, 31);
        var employees = new[]
        {
            Employee(1, "A", new DateTime(2024, 1, 1)),
            Employee(2, "A", new DateTime(2024, 1, 1)),
            Employee(3, "A", new DateTime(2025, 6, 1))
        };
        var leavers = new[]
        {
            // Nach 6 vollen Monaten ausgetreten, einmal relevant, einmal Pensionierung.
            Leaver(10, "A", new DateTime(2024, 7, 15), new DateTime(2024, 1, 1)),
            Leaver(11, "A", new DateTime(2024, 7, 15), new DateTime(2024, 1, 1), relevant: false)
        };

        var all = HrKpiInteractive.Retention(employees, leavers, anchor, relevantOnly: false);
        var relevant = HrKpiInteractive.Retention(employees, leavers, anchor, relevantOnly: true);

        var c2024 = all.Single(c => c.Cohort == 2024);
        Assert.Equal(4, c2024.Size);
        Assert.Equal(0.5, c2024.Points.Single(p => p.Month == 7).Share, 6);
        Assert.Equal(0.5, c2024.FirstYearLoss!.Value, 6);
        // Nur relevante: die Pensionierung gilt als zensiert, also 1 von 4.
        Assert.Equal(0.75, relevant.Single(c => c.Cohort == 2024).Points.Single(p => p.Month == 7).Share, 6);
        // Jahrgang 2025 ist erst sieben Monate beobachtet, ein Ergebnis nach zwoelf Monaten gibt es noch nicht.
        Assert.Null(all.Single(c => c.Cohort == 2025).FirstYearLoss);
    }

    [Fact]
    public void SickCalendar_Zaehlt_Personen_Je_Arbeitstag_Ohne_Wochenende_Und_Feiertag()
    {
        var cases = new[]
        {
            // Do 31.07. bis Mo 04.08.2025: Fr 01.08. ist Bundesfeiertag, Wochenende faellt weg.
            Case(1, "0260", new DateTime(2025, 7, 31), new DateTime(2025, 8, 4), 2),
            Case(2, "0260", new DateTime(2025, 8, 4), new DateTime(2025, 8, 4), 1),
            Case(3, "0100", new DateTime(2025, 8, 4), new DateTime(2025, 8, 8), 5)
        };

        var days = HrKpiInteractive.SickCalendar(cases, 2025, new DateTime(2025, 12, 31));

        Assert.DoesNotContain(days, d => d.Day == new DateTime(2025, 8, 1));
        Assert.DoesNotContain(days, d => d.Day == new DateTime(2025, 8, 2));
        Assert.Equal(1, days.Single(d => d.Day == new DateTime(2025, 7, 31)).Persons);
        Assert.Equal(2, days.Single(d => d.Day == new DateTime(2025, 8, 4)).Persons);
        Assert.Equal(ZurichWorkdayCalendar.CountWorkdays(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31)), days.Count);
    }

    [Fact]
    public void ShortSickStartWeekdays_Nimmt_Nur_Kurze_Krankheiten()
    {
        var cases = new[]
        {
            Case(1, "0260", new DateTime(2025, 3, 3), new DateTime(2025, 3, 3), 1),   // Montag
            Case(2, "0260", new DateTime(2025, 3, 10), new DateTime(2025, 3, 11), 2), // Montag
            Case(3, "0260", new DateTime(2025, 3, 14), new DateTime(2025, 3, 14), 1), // Freitag
            Case(4, "0260", new DateTime(2025, 3, 4), new DateTime(2025, 3, 14), 9)   // zu lang
        };

        var shares = HrKpiInteractive.ShortSickStartWeekdays(cases, new DateTime(2025, 1, 1), new DateTime(2025, 12, 31), new DateTime(2025, 12, 31));

        Assert.Equal(2, shares.Single(s => s.Day == DayOfWeek.Monday).Starts);
        Assert.Equal(1, shares.Single(s => s.Day == DayOfWeek.Friday).Starts);
        Assert.Equal(0, shares.Single(s => s.Day == DayOfWeek.Tuesday).Starts);
        Assert.Equal(2.0 / 3, shares.Single(s => s.Day == DayOfWeek.Monday).Share, 6);
    }

    [Fact]
    public void FitWohl_Loest_Bei_Der_Sechsten_Absenz_Oder_Zwoelf_Tagen_Aus()
    {
        var employees = new[] { Employee(1, "A", new DateTime(2020, 1, 1)), Employee(2, "B", new DateTime(2020, 1, 1)), Employee(3, "B", new DateTime(2020, 1, 1)) };
        var cases = new List<HrAbsenceSapCase>();
        // Person 1: sechs eintaegige Absenzen an Dienstagen.
        for (var i = 0; i < 6; i++)
            cases.Add(Case(1, "0260", new DateTime(2025, 1, 7).AddDays(7 * i), new DateTime(2025, 1, 7).AddDays(7 * i), 1));
        // Person 2: zwei Faelle Fr und Mo gelten als eine Absenz; 12 Tage ab Mo 03.03. erreicht.
        cases.Add(Case(2, "0260", new DateTime(2025, 2, 28), new DateTime(2025, 2, 28), 1));
        cases.Add(Case(2, "0260", new DateTime(2025, 3, 3), new DateTime(2025, 3, 18), 12));
        // Person 3: fuenf Absenzen, 5 Tage: keine Ausloesung.
        for (var i = 0; i < 5; i++)
            cases.Add(Case(3, "0260", new DateTime(2025, 1, 7).AddDays(7 * i), new DateTime(2025, 1, 7).AddDays(7 * i), 1));

        var result = HrKpiInteractive.FitWohl(cases, employees, 2025, new DateTime(2025, 12, 31));

        var p1 = result.Single(r => r.Personalnummer == 1);
        Assert.Equal(HrKpiInteractive.ReasonEpisodes, p1.Reason);
        Assert.Equal(new DateTime(2025, 2, 11), p1.TriggerDate);
        var p2 = result.Single(r => r.Personalnummer == 2);
        Assert.Equal(1, p2.Episodes);
        Assert.Equal(13m, p2.Days);
        Assert.Equal(HrKpiInteractive.ReasonDays, p2.Reason);
        Assert.Equal(new DateTime(2025, 2, 28), p2.TriggerDate);
        Assert.DoesNotContain(result, r => r.Personalnummer == 3);
    }

    [Fact]
    public void VacationBlocks_Verbindet_Faelle_Ueber_Das_Wochenende()
    {
        var employees = new[] { Employee(1, "A", null), Employee(2, "A", null) };
        var cases = new[]
        {
            Case(1, "0100", new DateTime(2025, 7, 7), new DateTime(2025, 7, 11), 5),
            Case(1, "0100", new DateTime(2025, 7, 14), new DateTime(2025, 7, 18), 5),
            Case(2, "0100", new DateTime(2025, 7, 7), new DateTime(2025, 7, 11), 5),
            Case(2, "0100", new DateTime(2025, 7, 21), new DateTime(2025, 7, 25), 5),
            Case(2, "0100", new DateTime(2025, 12, 15), new DateTime(2025, 12, 26), 8)
        };

        var people = HrKpiInteractive.VacationBlocks(cases, employees, 2025, new DateTime(2025, 11, 30));

        Assert.Equal(10m, people.Single(p => p.Personalnummer == 1).LongestTaken);
        var p2 = people.Single(p => p.Personalnummer == 2);
        Assert.Equal(5m, p2.LongestTaken);
        // Der Dezemberblock ist am Stichtag erst geplant.
        Assert.Equal(8m, p2.LongestPlanned);
    }

    [Fact]
    public void Simulator_Schreibt_Headcount_Fort_Und_Rechnet_Jahresfluktuation()
    {
        var employees = Enumerable.Range(1, 100).Select(i => Employee(i, "A", new DateTime(2020, 1, 1))).ToArray();
        var b = HrKpiInteractive.SimulatorBase(employees, [], new DateTime(2026, 10, 15));

        Assert.Equal(2, b.RemainingMonths);
        Assert.Equal(100m, b.HeadcountNow);
        Assert.Equal(10, b.PastMonths.Count);

        var sim = HrKpiInteractive.Simulate(b, relevantPerMonth: 2, otherPerMonth: 0, hiresPerMonth: 0);

        // November (100 -> 98, Mittel 99) und Dezember (98 -> 96, Mittel 97); zehn Monate mit 100.
        Assert.Equal(96m, sim.HeadcountYearEnd);
        Assert.Equal(4m, sim.ProjectedRelevant);
        Assert.Equal((1000m + 99m + 97m) / 12m, sim.AverageHeadcount);
        Assert.Equal(4m / ((1000m + 99m + 97m) / 12m), sim.YearRate);
    }

    [Fact]
    public void FteWorkdays_Zaehlt_Erst_Ab_Eintritt()
    {
        var start = new DateTime(2025, 1, 1);
        var end = new DateTime(2025, 1, 31);
        var all = ZurichWorkdayCalendar.CountWorkdays(start, end);

        Assert.Equal(all, HrKpiDashboardBuilder.FteWorkdays(1m, new DateTime(2020, 1, 1), start, end, all));
        Assert.Equal(0.5m * ZurichWorkdayCalendar.CountWorkdays(new DateTime(2025, 1, 15), end),
            HrKpiDashboardBuilder.FteWorkdays(0.5m, new DateTime(2025, 1, 15), start, end, all));
        Assert.Equal(0m, HrKpiDashboardBuilder.FteWorkdays(1m, new DateTime(2025, 2, 1), start, end, all));
    }

    [Fact]
    public void Prognose_Rechnet_Angebrochenes_Quartal_Hoch()
    {
        var (elapsed, total) = HrKpiDashboardBuilder.QuarterProgress(2026, 4, new DateTime(2026, 11, 4));

        Assert.Equal(35, elapsed);
        Assert.Equal(92, total);
        Assert.Equal(0.01m * 92 / 35 * 4, HrKpiDashboardBuilder.ForecastFromQuarter(0.01m, elapsed, total));
        // Abgeschlossenes Quartal: unveraendert Quartalsrate x 4 (Vorgabe HR).
        var (fullElapsed, fullTotal) = HrKpiDashboardBuilder.QuarterProgress(2025, 4, new DateTime(2025, 12, 31));
        Assert.Equal(0.04m, HrKpiDashboardBuilder.ForecastFromQuarter(0.01m, fullElapsed, fullTotal));
    }
}
