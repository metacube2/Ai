using ClosedXML.Excel;
using Microsoft.Extensions.Options;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public sealed class HrKpiServiceTests : IDisposable
{
    private readonly string _folder;
    private readonly HrKpiService _service;

    public HrKpiServiceTests()
    {
        _folder = Path.Combine(Path.GetTempPath(), "trafag-hr-kpi-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        WriteFixtureFiles(_folder);

        _service = new HrKpiService(Options.Create(new HrKpiDataSourceOptions
        {
            DataFolder = _folder
        }));
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public void HrKpiOptions_Default_Exit_Year_Is_Empty()
    {
        Assert.Null(new HrKpiOptions().Year);
    }

    [Fact]
    public async Task Date_Range_Overrides_Conflicting_Year_In_All_Turnover_Metrics()
    {
        var options = new HrKpiOptions { DataFolder = _folder, FromDate = new(2025, 3, 1), ToDate = new(2025, 3, 31) };
        var expected = await _service.BuildAsync(options);
        options.Year = 2024;
        var actual = await _service.BuildAsync(options);
        Assert.Equal(expected.TurnoverMetrics.Select(x => (x.Label, x.Value, x.Detail)),
            actual.TurnoverMetrics.Select(x => (x.Label, x.Value, x.Detail)));
        Assert.Equal(expected.PeriodComparisonMetrics.Select(x => (x.Label, x.Value)),
            actual.PeriodComparisonMetrics.Select(x => (x.Label, x.Value)));
    }

    [Fact]
    public async Task Ytd_Includes_Leavers_And_Headcount_Before_Selection_Start()
    {
        RewriteEmployeeRows(Enumerable.Range(1, 3).Select(i => new object?[]
        { 4000 + i, $"Stable, {i}", "Org A", "100 / Org A", "Engineer", "n", new DateTime(2020, 1, 1), "Aktiv", "0:00", 25, 0, 0, 100000, "CHF" }).ToArray());
        RewriteLeaverRows([
            [5001, "Leaving, January", "Org A", "Engineer", "Inaktiv", new DateTime(2025, 1, 15), new DateTime(2020, 1, 1), "Kündigung AN"],
            [5002, "Leaving, June", "Org A", "Engineer", "Inaktiv", new DateTime(2025, 6, 15), new DateTime(2020, 1, 1), "Kündigung AN"]
        ]);
        foreach (var startMonth in new[] { 1, 4, 6 })
        {
            var result = await _service.BuildAsync(new HrKpiOptions { DataFolder = _folder, FromDate = new(2025, startMonth, 1), ToDate = new(2025, 6, 30) });
            Assert.Equal("2", result.TurnoverMetrics.Single(x => x.Label == "Austritte YTD").Value);
            Assert.Equal("4", result.TurnoverMetrics.Single(x => x.Label == "HC Basis YTD").Value);
            Assert.Equal(0.5m.ToString("P1"), result.TurnoverMetrics.Single(x => x.Label == "Fluktuation YTD").Value);
            Assert.Equal(startMonth == 1 ? 2 : 1, result.Leavers.Count);
        }
    }

    [Fact]
    public async Task Calendar_Quarter_Includes_Earlier_Months_But_Selection_Does_Not()
    {
        AppendLeaverRow(5001, "Leaving, January", "Org A", "Engineer", new(2025, 1, 15), new(2020, 1, 1), "Kündigung AN");
        var result = await _service.BuildAsync(new HrKpiOptions { DataFolder = _folder, FromDate = new(2025, 3, 1), ToDate = new(2025, 3, 31) });
        Assert.Single(result.Leavers);
        Assert.Equal("2", result.TurnoverMetrics.Single(x => x.Label == "Austritte Quartal").Value);
        Assert.Equal("1", result.TurnoverMetrics.Single(x => x.Label == "Austritte relevant").Value);
    }

    [Fact]
    public async Task BuildAsync_Applies_Organisation_Filter_To_Absences()
    {
        var result = await _service.BuildAsync(new HrKpiOptions
        {
            DataFolder = _folder,
            Year = 2025,
            Organisationseinheit = "Org A"
        });

        Assert.All(result.Employees, row => Assert.Equal("Org A", row.Organisationseinheit));
        var absence = Assert.Single(result.Absences);
        Assert.Equal(1001, absence.Personalnummer);
        Assert.Equal(1.0m, absence.KrankheitstageGesamt);
    }

    [Fact]
    public async Task BuildAsync_Uses_Date_Range_Instead_Of_Year_For_Leavers()
    {
        var result = await _service.BuildAsync(new HrKpiOptions
        {
            DataFolder = _folder,
            Year = 2024,
            FromDate = new DateTime(2025, 3, 1),
            ToDate = new DateTime(2025, 3, 31)
        });

        var relevant = Assert.Single(result.FluctuationRelevantLeavers);
        Assert.Equal(1001, relevant.Personalnummer);
        Assert.DoesNotContain(result.Leavers, row => row.Austrittsdatum?.Year == 2024);
    }

    [Fact]
    public async Task BuildAsync_With_Empty_Exit_Year_Includes_All_Leaver_Years()
    {
        var result = await _service.BuildAsync(new HrKpiOptions
        {
            DataFolder = _folder,
            Year = null
        });

        Assert.Contains(2025, result.ExitYearOptions);
        Assert.Contains(2024, result.ExitYearOptions);
        Assert.Contains(result.Leavers, row => row.Austrittsdatum?.Year == 2025);
        Assert.Contains(result.Leavers, row => row.Austrittsdatum?.Year == 2024);
    }

    [Fact]
    public async Task BuildAsync_Employee_Only_Filters_Do_Not_Distort_Turnover_Denominator()
    {
        var result = await _service.BuildAsync(new HrKpiOptions
        {
            DataFolder = _folder,
            Year = 2025,
            KostenstelleText = "100 / Org A"
        });

        var activeHeadcount = Assert.Single(result.Metrics, metric => metric.Label == "Headcount aktiv");
        Assert.Equal("1", activeHeadcount.Value);

        var turnoverHeadcount = Assert.Single(result.TurnoverMetrics, metric => metric.Label == "HC Basis YTD");
        Assert.Equal(3.0m.ToString("N1"), turnoverHeadcount.Value);
        Assert.Contains(result.Notices, notice => notice.Contains("nicht die Fluktuation", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task BuildAsync_Uses_Average_Headcount_For_Turnover_Formulas()
    {
        RewriteEmployeeRows(
        [
            [4001, "Stable, Anna", "Org A", "100 / Org A", "Engineer", "n", new DateTime(2020, 1, 1), "Aktiv", "0:00", 25, 0, 0, 100000, "CHF"],
            [4002, "Stable, Bruno", "Org A", "100 / Org A", "Engineer", "n", new DateTime(2020, 1, 1), "Aktiv", "0:00", 25, 0, 0, 100000, "CHF"],
            [4003, "Stable, Carla", "Org A", "100 / Org A", "Engineer", "n", new DateTime(2020, 1, 1), "Aktiv", "0:00", 25, 0, 0, 100000, "CHF"]
        ]);
        RewriteLeaverRows(
        [
            [5001, "Leaving, Lea", "Org A", "Engineer", "Inaktiv", new DateTime(2025, 6, 30), new DateTime(2025, 1, 1), "Kündigung AN"]
        ]);

        var result = await _service.BuildAsync(new HrKpiOptions
        {
            DataFolder = _folder,
            Year = 2025
        });

        var avgYearHeadcount = Assert.Single(result.TurnoverMetrics, metric => metric.Label == "HC Jahr bis Stichtag");
        Assert.Equal(3.5m.ToString("N1"), avgYearHeadcount.Value);

        var yearRate = Assert.Single(result.TurnoverMetrics, metric => metric.Label == "Fluktuation YTD");
        Assert.Equal((1m / 3.5m).ToString("P1"), yearRate.Value);
        Assert.Equal("01.01.-31.12. / Avg HC YTD", yearRate.Detail);
    }

    [Fact]
    public async Task BuildAsync_Uses_Distinct_Persons_In_Turnover_Visuals()
    {
        AppendLeaverRow(
            1001,
            "Alpha, Anna",
            "Org A",
            "Engineer",
            new DateTime(2025, 3, 20),
            new DateTime(2020, 1, 1),
            "Arbeitnehmer Kuendigung");

        var result = await _service.BuildAsync(new HrKpiOptions
        {
            DataFolder = _folder,
            Year = 2025
        });

        Assert.Equal(1, result.TurnoverVisuals.MonthlyRelevantLeavers[2].Count);
        Assert.Equal(1, result.TurnoverVisuals.RelevantByOrganisation.Single(row => row.Label == "Org A").Count);
        Assert.Equal(3, result.TurnoverVisuals.FunnelSteps.Single(row => row.Label == "Austritte Total").Count);
    }

    [Fact]
    public async Task BuildAsync_Excludes_Missing_Personalnummer_From_Distinct_Headcount_And_Uses_Fte_Fallback()
    {
        var result = await _service.BuildAsync(new HrKpiOptions
        {
            DataFolder = _folder,
            Year = 2025
        });

        var headcount = Assert.Single(result.Metrics, metric => metric.Label == "Headcount aktiv");
        Assert.Equal("3", headcount.Value);

        var fallbackEmployee = Assert.Single(result.Employees, row => row.NameVoll == "Fallback, Fiona");
        Assert.Null(fallbackEmployee.BeschaeftigungsgradProzent);
        Assert.Equal(0.5m, fallbackEmployee.Fte);

        var absenceRate = Assert.Single(result.AbsenceMetrics, metric => metric.Label == "Krankenquote");
        Assert.Contains("FTE", absenceRate.Detail);

        Assert.Contains(result.Notices, notice => notice.Contains("ohne Personalnummer", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Notices, notice => notice.Contains("FTE aus der Rexx-Sollzeit", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task BuildAsync_Classifies_Turnover_Relevance_And_Visuals()
    {
        var result = await _service.BuildAsync(new HrKpiOptions
        {
            DataFolder = _folder,
            Year = 2025
        });

        Assert.Equal(3, result.Leavers.Count);
        Assert.Single(result.Leavers, row => row.IstFluktuationsrelevant);
        Assert.Contains(result.Leavers, row => row.FluktuationAusschlussgrund == "Kuendigung durch Trafag");
        Assert.Contains(result.Leavers, row => row.FluktuationAusschlussgrund == "Praktikant");
        Assert.Equal(1, result.TurnoverVisuals.MonthlyRelevantLeavers[2].Count);
    }

    [Fact]
    public async Task BuildAsync_Recognizes_Rexx_Kuendigung_AN_And_AG()
    {
        RewriteLeaverRows(
        [
            [3001, "Employee, Eva", "Org A", "Engineer", "Inaktiv", new DateTime(2025, 6, 1), new DateTime(2020, 1, 1), "Kündigung AN"],
            [3002, "Employer, Emil", "Org A", "Engineer", "Inaktiv", new DateTime(2025, 6, 2), new DateTime(2020, 1, 1), "Kündigung AG"],
            [3003, "Retired, Rita", "Org A", "Engineer", "Inaktiv", new DateTime(2025, 6, 3), new DateTime(2020, 1, 1), "Ruhestand"]
        ]);

        var result = await _service.BuildAsync(new HrKpiOptions
        {
            DataFolder = _folder,
            Year = 2025
        });

        Assert.Equal("1", Assert.Single(result.TurnoverMetrics, metric => metric.Label == "Austritte AN-Kuendigung").Value);
        Assert.Equal("1", Assert.Single(result.TurnoverMetrics, metric => metric.Label == "Austritte relevant").Value);
        Assert.Contains(result.Leavers, row => row.Austrittsart == "Kündigung AG" && row.FluktuationAusschlussgrund == "Kuendigung durch Trafag");
        Assert.Contains(result.Leavers, row => row.Austrittsart == "Ruhestand" && row.FluktuationAusschlussgrund == "Pensionierung");
    }

    [Fact]
    public async Task BuildAsync_Excludes_Configured_Test_Persons_From_All_Hr_Kpi_Views()
    {
        RewriteEmployeeRows(
        [
            [1001, "Alpha, Anna", "Org A", "100 / Org A", "Engineer", "n", new DateTime(2020, 1, 1), "Aktiv", "0:00", 25, 0, 0, 100000, "CHF"],
            [9001, "Jolie, Angelina", "Test", "999 / Test", "Engineer", "n", new DateTime(2020, 1, 1), "Aktiv", "0:00", 25, 0, 0, 100000, "CHF"],
            [9002, "Brad Pitt", "Test", "999 / Test", "Engineer", "n", new DateTime(2020, 1, 1), "Aktiv", "0:00", 25, 0, 0, 100000, "CHF"],
            [9003, "Peter Muster", "Test", "999 / Test", "Engineer", "n", new DateTime(2020, 1, 1), "Aktiv", "0:00", 25, 0, 0, 100000, "CHF"]
        ]);
        WriteWorkbook(Path.Combine(_folder, "Abwesenheitinstunden.xlsx"),
            [
                "Personalnummer", "Nachname, Vorname (Link Personal)", "Organisation", "Stelle", "Personal Status",
                "Krankheit angetreten (Stunden Ind.)", "Krank nicht buchbar angetreten (Stunden Ind.)"
            ],
            [
                [1001, "Alpha, Anna", "Org A", "Engineer", "Aktiv", 8.4, 0],
                [9004, "ICT Trafag", "Test", "Engineer", "Aktiv", 8.4, 0]
            ]);
        RewriteLeaverRows(
        [
            [1001, "Alpha, Anna", "Org A", "Engineer", "Inaktiv", new DateTime(2025, 3, 10), new DateTime(2020, 1, 1), "Arbeitnehmer Kuendigung"],
            [9005, "Empfänger Reminder", "Test", "Engineer", "Inaktiv", new DateTime(2025, 3, 10), new DateTime(2020, 1, 1), "Arbeitnehmer Kuendigung"]
        ]);

        var result = await _service.BuildAsync(new HrKpiOptions
        {
            DataFolder = _folder,
            Year = 2025
        });

        Assert.DoesNotContain(result.Employees, row => row.NameVoll.Contains("Angelina", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.Employees, row => row.NameVoll.Contains("Brad Pitt", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.Employees, row => row.NameVoll.Contains("Peter Muster", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.Absences, row => row.Name.Contains("ICT Trafag", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.Leavers, row => row.NameVoll.Contains("Reminder", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Notices, notice => notice.Contains("Testpersonen", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task BuildAsync_PeriodComparison_Shows_Real_PreviousYear_When_Year_Selected()
    {
        // H1: Vorjahresvergleich darf nicht 0 sein, wenn ein Austrittsjahr gewaehlt ist.
        // Fixture enthaelt einen fluktuationsrelevanten Austritt 2024 (Fallback, Fiona) und 2025 (Alpha, Anna).
        var result = await _service.BuildAsync(new HrKpiOptions
        {
            DataFolder = _folder,
            Year = 2025
        });

        var leaverKpi = Assert.Single(result.PeriodComparisonMetrics, m => m.Label == "Austritte 2025");
        Assert.Equal("Vorjahr 1", leaverKpi.Detail);

        var rateKpi = Assert.Single(result.PeriodComparisonMetrics, m => m.Label == "Fluktuation 2025");
        Assert.DoesNotContain("Vorjahr 0.0%", rateKpi.Detail);
    }

    [Fact]
    public async Task BuildAsync_Absence_Rate_Caps_Period_At_Today_For_Current_Year()
    {
        // H2: Beim laufenden Jahr endet der Nenner-Zeitraum heute, nicht am 31.12.
        var currentYear = DateTime.Today.Year;
        var result = await _service.BuildAsync(new HrKpiOptions
        {
            DataFolder = _folder,
            Year = currentYear
        });

        var rate = Assert.Single(result.AbsenceMetrics, m => m.Label == "Krankenquote");
        Assert.Contains(DateTime.Today.ToString("dd.MM.yyyy"), rate.Detail);
        Assert.DoesNotContain($"31.12.{currentYear}", rate.Detail);
    }

    [Fact]
    public async Task BuildAsync_Date_Range_Shows_Quarter_Forecast_Without_Explicit_Year()
    {
        var result = await _service.BuildAsync(new HrKpiOptions
        {
            DataFolder = _folder,
            Year = null,
            FromDate = new DateTime(2025, 3, 1),
            ToDate = new DateTime(2025, 3, 31)
        });

        Assert.Contains(result.TurnoverMetrics, metric => metric.Label == "Fluktuation Quartal");
        var forecast = Assert.Single(result.TurnoverMetrics, metric => metric.Label == "Fluktuation Prognose");
        Assert.Equal("Quartalsrate x 4, nur Schaetzung", forecast.Detail);
    }

    [Fact]
    public async Task BuildAsync_Uses_Configured_Absence_Thresholds_For_Status_Color()
    {
        var service = new HrKpiService(Options.Create(new HrKpiDataSourceOptions
        {
            DataFolder = _folder,
            AbsenceYellowThresholdPercent = 0.1m,
            AbsenceRedThresholdPercent = 50m
        }));

        var result = await service.BuildAsync(new HrKpiOptions
        {
            DataFolder = _folder,
            Year = 2025
        });

        var absenceRate = Assert.Single(result.AbsenceMetrics, metric => metric.Label == "Krankenquote");
        Assert.Equal("Warning", absenceRate.Severity);
        Assert.Contains("Gelb < 50.0%", absenceRate.Detail);

        var absenceStatus = Assert.Single(result.TrafficLights, item => item.Area == "Krankenquote");
        Assert.Equal("Gelb", absenceStatus.Status);
    }

    [Fact]
    public async Task BuildAsync_Flags_Krankenquote_As_Unreliable_When_Period_Filter_Set_But_Absences_Have_No_Dates()
    {
        // Praxisfall 2026-07-29: Rexx-Export "Abwesenheitinstunden.xlsx" hat keine Datumsfelder je
        // Zeile (die "(Zeitraum)"-Spalten sind Marker des juengsten Ereignisses, keine
        // Aggregationsfenster - Stunden sind kumulativ). Waehlt man dennoch einen engen Zeitraum
        // (z.B. Juni), bleibt der Zaehler ungefiltert, waehrend der Nenner schrumpft - die Quote
        // wird dadurch massiv ueberhoeht (Praxisfall: 20% statt 3-4%, weil ein Mitarbeiter seine
        // Stunden aus Maerz zeigte, obwohl Juni gewaehlt war). Die Kachel muss das klar kennzeichnen
        // statt eine falsch praezise Prozentzahl zu zeigen.
        var result = await _service.BuildAsync(new HrKpiOptions
        {
            DataFolder = _folder,
            FromDate = new DateTime(2025, 6, 1),
            ToDate = new DateTime(2025, 6, 30)
        });

        var absenceRate = Assert.Single(result.AbsenceMetrics, metric => metric.Label == "Krankenquote");
        Assert.Equal("Zeitraum nicht bestimmbar", absenceRate.Value);
        Assert.False(result.AbsenceRatesReliable); // Auch Tabellen/Druck muessen die Quote maskieren.
        Assert.Equal("Warning", absenceRate.Severity);
        Assert.Contains("ACHTUNG", absenceRate.Detail);

        var totalDays = Assert.Single(result.AbsenceMetrics, metric => metric.Label == "Krankheitstage Gesamt");
        Assert.Equal("Warning", totalDays.Severity);
        Assert.Contains("ACHTUNG", totalDays.Detail);

        var overviewDays = Assert.Single(result.Metrics, metric => metric.Label == "Krankheitstage");
        Assert.Equal("Absenzquote: Zeitraum nicht bestimmbar", overviewDays.Detail);
        Assert.Equal("Warning", overviewDays.Severity);

        var trafficLight = Assert.Single(result.TrafficLights, item => item.Area == "Krankenquote");
        Assert.Equal("Gelb", trafficLight.Status);
    }

    [Fact]
    public async Task BuildAsync_Shows_Real_Krankenquote_When_No_Period_Filter_Is_Set()
    {
        // Ohne Zeitraumfilter besteht das Zaehler/Nenner-Mismatch-Problem nicht in derselben Form -
        // die Kachel soll weiterhin eine normale Prozentzahl zeigen, nicht die Warnung.
        var result = await _service.BuildAsync(new HrKpiOptions
        {
            DataFolder = _folder
        });

        var absenceRate = Assert.Single(result.AbsenceMetrics, metric => metric.Label == "Krankenquote");
        Assert.NotEqual("Zeitraum nicht bestimmbar", absenceRate.Value);
        Assert.EndsWith("%", absenceRate.Value);
    }

    [Fact]
    public void ZurichWorkdayCalendar_Subtracts_All_Nine_Statutory_Holidays()
    {
        var holidays2026 = ZurichWorkdayCalendar.GetPublicHolidays(2026);

        Assert.Equal(9, holidays2026.Count);
        Assert.Contains(new DateTime(2026, 1, 1), holidays2026);
        Assert.Contains(new DateTime(2026, 4, 3), holidays2026);  // Karfreitag
        Assert.Contains(new DateTime(2026, 4, 6), holidays2026);  // Ostermontag
        Assert.Contains(new DateTime(2026, 5, 1), holidays2026);
        Assert.Contains(new DateTime(2026, 5, 14), holidays2026); // Auffahrt
        Assert.Contains(new DateTime(2026, 5, 25), holidays2026); // Pfingstmontag
        Assert.Contains(new DateTime(2026, 8, 1), holidays2026);
        Assert.Contains(new DateTime(2026, 12, 25), holidays2026);
        Assert.Contains(new DateTime(2026, 12, 26), holidays2026);

        // Mi 01.04. bis Di 07.04. hat fuenf Wochentage; Karfreitag und Ostermontag
        // sind gesetzliche ZH-Feiertage, also bleiben drei Arbeitstage.
        Assert.Equal(3, ZurichWorkdayCalendar.CountWorkdays(
            new DateTime(2026, 4, 1),
            new DateTime(2026, 4, 7)));
    }

    [Fact]
    public async Task BuildAsync_All_128_Global_Filter_Combinations_Keep_Every_Visible_Block_Consistent()
    {
        var baseline = await _service.BuildAsync(new HrKpiOptions { DataFolder = _folder });
        const int filterCount = 7;

        for (var mask = 0; mask < (1 << filterCount); mask++)
        {
            var options = new HrKpiOptions { DataFolder = _folder };
            if ((mask & (1 << 0)) != 0) options.Organisationseinheit = "Org A";
            if ((mask & (1 << 1)) != 0) options.KostenstelleText = "100 / Org A";
            if ((mask & (1 << 2)) != 0) options.Mitarbeitertyp = "Festangestellt";
            if ((mask & (1 << 3)) != 0) options.EntryYear = 2020;
            if ((mask & (1 << 4)) != 0) options.GlzAmpel = "Rot";
            if ((mask & (1 << 5)) != 0) options.RestferienAmpel = "Rot";
            if ((mask & (1 << 6)) != 0) options.SearchText = "Alpha";

            var result = await _service.BuildAsync(options);
            var expectedEmployees = baseline.Employees.Where(row =>
                    (options.Organisationseinheit is null || row.Organisationseinheit == options.Organisationseinheit) &&
                    (options.KostenstelleText is null || row.KostenstelleText == options.KostenstelleText) &&
                    (options.Mitarbeitertyp is null || row.Mitarbeitertyp == options.Mitarbeitertyp) &&
                    (!options.EntryYear.HasValue || row.Eintrittsdatum?.Year == options.EntryYear) &&
                    (options.GlzAmpel is null || row.GlzAmpel == options.GlzAmpel) &&
                    (options.RestferienAmpel is null || row.RestferienAmpel == options.RestferienAmpel) &&
                    (options.SearchText is null || row.NameVoll.Contains(options.SearchText, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            var expectedEmployeeNumbers = expectedEmployees
                .Where(row => row.Personalnummer.HasValue)
                .Select(row => row.Personalnummer!.Value)
                .ToHashSet();

            // Kostenstelle/GLZ/Restferien existieren nicht stabil in der Austrittsdatei und
            // duerfen deshalb den Fluktuations-Nenner/-Zaehler nicht still verzerren.
            var expectedLeavers = baseline.Leavers.Where(row =>
                    (options.Organisationseinheit is null || row.Organisationseinheit == options.Organisationseinheit) &&
                    (options.Mitarbeitertyp is null || row.Mitarbeitertyp == options.Mitarbeitertyp) &&
                    (!options.EntryYear.HasValue || row.Eintrittsdatum?.Year == options.EntryYear) &&
                    (options.SearchText is null || row.NameVoll.Contains(options.SearchText, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            Assert.Equal(
                expectedEmployees.Select(row => row.NameVoll).OrderBy(value => value),
                result.Employees.Select(row => row.NameVoll).OrderBy(value => value));
            Assert.Equal(
                baseline.Absences.Where(row => row.Personalnummer.HasValue && expectedEmployeeNumbers.Contains(row.Personalnummer.Value))
                    .Select(row => row.Personalnummer).OrderBy(value => value),
                result.Absences.Select(row => row.Personalnummer).OrderBy(value => value));
            Assert.Equal(
                expectedLeavers.Select(row => row.NameVoll).OrderBy(value => value),
                result.Leavers.Select(row => row.NameVoll).OrderBy(value => value));

            AssertEveryVisibleBlockIsConsistent(result, $"Filtermaske {mask}");
        }
    }

    [Fact]
    public async Task BuildAsync_Combined_Period_Turnover_And_Person_Filters_Keep_All_Visible_Blocks_Consistent()
    {
        var result = await _service.BuildAsync(new HrKpiOptions
        {
            DataFolder = _folder,
            Year = 2024, // Von/Bis hat gemaess UI-Vertrag Vorrang.
            FromDate = new DateTime(2025, 1, 1),
            ToDate = new DateTime(2025, 12, 31),
            EntryYear = 2020,
            Organisationseinheit = "Org A",
            KostenstelleText = "100 / Org A",
            Mitarbeitertyp = "Festangestellt",
            FluktuationFilter = "Fluktuationsrelevant",
            GlzAmpel = "Rot",
            RestferienAmpel = "Rot",
            SearchText = "Alpha"
        });

        Assert.Equal("Alpha, Anna", Assert.Single(result.Employees).NameVoll);
        Assert.Equal("Alpha, Anna", Assert.Single(result.Absences).Name);
        Assert.Equal("Alpha, Anna", Assert.Single(result.Leavers).NameVoll);
        Assert.Contains(result.TurnoverMetrics, metric => metric.Label == "Fluktuation YTD");
        AssertEveryVisibleBlockIsConsistent(result, "kombinierte Filter");
    }

    // ---- Vorgaben HR (Sonja Richter, Antwort auf Ingos Fragen vom 2026-08-19) ----

    [Theory]
    [InlineData(2026, 2, 5, "Gruen")]
    [InlineData(2026, 3, 5.5, "Rot")]
    [InlineData(2026, 4, 0.5, "Rot")]
    [InlineData(2026, 4, 0, "Gruen")]
    [InlineData(2026, 12, 3, "Rot")]
    public void Restferien_Ampel_Q1_Bis_Fuenf_Tage_Gruen_Ab_Q2_Jeder_Resttag_Rot(int year, int month, double rest, string expected)
    {
        Assert.Equal(expected, HrKpiDashboardBuilder.ResolveRestferienAmpel((decimal)rest, new DateTime(year, month, 15)));
    }

    [Fact]
    public void Krankheit_Wird_Ab_Dem_61_Tag_Als_Langzeitkrankheit_Gezaehlt()
    {
        var sixtyDays = HrKpiDashboardBuilder.ClassifySickness(60m * HrKpiDashboardBuilder.HoursPerWorkday);
        var sixtyOneDays = HrKpiDashboardBuilder.ClassifySickness(61m * HrKpiDashboardBuilder.HoursPerWorkday);

        Assert.False(sixtyDays.IstLangzeitkrank);
        Assert.Equal(60m * HrKpiDashboardBuilder.HoursPerWorkday, sixtyDays.KurzStd);
        Assert.True(sixtyOneDays.IstLangzeitkrank);
        // Die ganze Krankheit wird lang, nicht erst die Tage ab dem 61.
        Assert.Equal(0m, sixtyOneDays.KurzStd);
        Assert.Equal(61m * HrKpiDashboardBuilder.HoursPerWorkday, sixtyOneDays.LangStd);
    }

    [Fact]
    public async Task BuildAsync_Liest_Die_Aus_OData_Geschriebene_SapDatei()
    {
        // Die Datei, die der Tagesabruf aus HrKpiSet schreibt, muss der bestehende Leser verstehen.
        SapGatewayHrKpiReader.WriteWorkbook(
        [
            new HrKpiSapRow("00001001", "2026", "09", "1100", "CH01", "0001", "1", "15", "", 60m, "2",
                "50000123", "50000999", 1.5m, 2m, "01")
        ], Path.Combine(_folder, "HR_KPI_Export.xlsx"));

        var result = await _service.BuildAsync(new HrKpiOptions { DataFolder = _folder });

        var alpha = Assert.Single(result.Employees, row => row.Personalnummer == 1001);
        Assert.Equal(60m, alpha.BeschaeftigungsgradProzent);
        Assert.Equal(2m, alpha.BuTage);
        Assert.Equal(1.5m, alpha.NbuTage);
        Assert.Equal("15", alpha.Mitarbeiterkreis);
        Assert.Equal("50000123", alpha.Planstelle);
    }

    [Theory]
    [InlineData(0.0499, false)]
    [InlineData(0.05, true)]
    [InlineData(0.0501, true)]
    public void Absenzampel_Ist_Ab_Fuenf_Prozent_Rot(double rate, bool expected)
    {
        // HR 2026-09-30: bis 4,99 % gelb, ab 5 % rot.
        Assert.Equal(expected, HrKpiDashboardBuilder.ReachesAbsenceRed((decimal)rate, 5m));
    }

    [Fact]
    public void Krankheitstag_Hat_Acht_Stunden()
    {
        // Trafag rechnet mit 8,0 h je Tag (Ingo 2026-09-30); mit den frueheren 8,4 h waeren
        // 488 Stunden nur 58,1 Tage gewesen und die Person nicht langzeitkrank.
        Assert.Equal(8.0m, HrKpiDashboardBuilder.HoursPerWorkday);
        Assert.True(HrKpiDashboardBuilder.ClassifySickness(488m).IstLangzeitkrank);
        Assert.False(HrKpiDashboardBuilder.ClassifySickness(487m).IstLangzeitkrank);
    }

    [Fact]
    public async Task BuildAsync_Nimmt_Krankheit_Und_Ferien_Je_Fall_Aus_SAP()
    {
        // Rexx-Absenzen wuerden 1001 mit 61 Tagen langzeitkrank machen; mit SAP-Datei zaehlen nur die Faelle.
        WriteWorkbook(Path.Combine(_folder, "Abwesenheitinstunden.xlsx"),
            [
                "Personalnummer", "Nachname, Vorname (Link Personal)", "Organisation", "Stelle", "Personal Status",
                "Krankheit angetreten (Stunden Ind.)", "Krank nicht buchbar angetreten (Stunden Ind.)"
            ],
            [[1001, "Alpha, Anna", "Org A", "Engineer", "Aktiv", 488.0, 0]]);
        SapGatewayHrAbsenceReader.WriteWorkbook(
            [
                // Mo 03.03. bis Fr 07.03.2025: 5 Tage, 40 h.
                new("00001001", "0260", new(2025, 3, 3), new(2025, 3, 7), "000", 5m, 40m, 5m),
                // Ueber den Jahreswechsel: 5 Arbeitstage (02.01. ist in Zuerich kein Feiertag), 2 davon 2025, also 12,8 von 32 h.
                new("00001001", "0230", new(2025, 12, 30), new(2026, 1, 6), "000", 4m, 32m, 8m),
                // 2024 und Arzt (0210) zaehlen nicht, unbekannte Person auch nicht.
                new("00001001", "0260", new(2024, 5, 6), new(2024, 5, 7), "000", 2m, 16m, 2m),
                new("00001001", "0210", new(2025, 4, 1), new(2025, 4, 1), "000", 0.5m, 4m, 1m),
                new("00009999", "0260", new(2025, 3, 3), new(2025, 3, 3), "000", 1m, 8m, 1m),
                new("00001002", "0100", new(2025, 7, 7), new(2025, 7, 11), "000", 5m, 40m, 5m),
                new("00001002", "0400", new(2025, 8, 1), new(2025, 8, 1), "000", 1m, 8m, 1m)
            ],
            Path.Combine(_folder, "HR_Absenzen_SAP.xlsx"));

        var result = await _service.BuildAsync(new HrKpiOptions { DataFolder = _folder, Year = 2025 });

        var alpha = Assert.Single(result.Absences, row => row.Personalnummer == 1001);
        Assert.Equal(52.8m, alpha.KrankheitGesamtStd);
        Assert.Equal(6.6m, alpha.KrankheitstageGesamt);
        Assert.False(alpha.IstLangzeitkrank);
        Assert.DoesNotContain(result.Absences, row => row.Personalnummer == 9999);
        Assert.True(result.AbsenceRatesReliable);
        Assert.Equal("5.0", Assert.Single(result.TimeVacationMetrics, m => m.Label == "Ferien bezogen im Zeitraum").Value);
        Assert.Equal("1.0", Assert.Single(result.TimeVacationMetrics, m => m.Label == "Kompensation im Zeitraum").Value);
        Assert.Contains(result.Notices, n => n.StartsWith("Krankheit aus SAP PA2001"));
    }

    [Fact]
    public async Task BuildAsync_Ohne_SAP_Absenzdatei_Rechnet_Wie_Bisher_Aus_Rexx()
    {
        var result = await _service.BuildAsync(new HrKpiOptions { DataFolder = _folder });

        Assert.DoesNotContain(result.Notices, n => n.StartsWith("Krankheit aus SAP PA2001"));
        Assert.DoesNotContain(result.TimeVacationMetrics, m => m.Label == "Ferien bezogen im Zeitraum");
    }

    [Fact]
    public async Task BuildAsync_Beschriftet_Unfalltage_Als_Laufenden_Monat()
    {
        // HrKpiSet liefert BU/NBU nur fuer den laufenden Monat (Entscheid 2026-10-01).
        var result = await _service.BuildAsync(new HrKpiOptions { DataFolder = _folder });

        var unfall = Assert.Single(result.AbsenceMetrics, metric => metric.Label == "Unfalltage laufender Monat");
        Assert.Contains("nur laufender Monat", unfall.Detail);
        Assert.DoesNotContain(result.AbsenceMetrics, metric => metric.Label == "Unfalltage Total");
    }

    [Theory]
    [InlineData("ja", 8.1)]
    [InlineData(" Ja ", 8.1)]
    [InlineData("nein", 8.0)]
    [InlineData("", 8.0)]
    public void Kader_Mit_Leitung_Ja_Hat_8_1_Stunden(string leitung, double expected)
    {
        // HR 2026-09-30: Kader 8,1 h, sonst 8,0 h. Kader = Rexx "Leitung j/n" = ja (Ingo 2026-10-01).
        Assert.Equal((decimal)expected, HrKpiDashboardBuilder.HoursPerWorkdayFor(leitung));
    }

    [Fact]
    public async Task BuildAsync_Rechnet_Krankheitstage_Fuer_Kader_Mit_8_1_Stunden()
    {
        WriteWorkbook(Path.Combine(_folder, "Abwesenheitinstunden.xlsx"),
            [
                "Personalnummer", "Nachname, Vorname (Link Personal)", "Organisation", "Stelle", "Leitung j/n", "Personal Status",
                "Krankheit angetreten (Stunden Ind.)", "Krank nicht buchbar angetreten (Stunden Ind.)"
            ],
            [
                // 488 h: bei 8,0 h genau 61 Tage (langzeitkrank), bei 8,1 h nur 60,2 Tage.
                [1001, "Alpha, Anna", "Org A", "Teamleiterin", "ja", "Aktiv", 488.0, 0],
                [1002, "Beta, Bruno", "Org B", "Engineer", "nein", "Aktiv", 488.0, 0]
            ]);

        var result = await _service.BuildAsync(new HrKpiOptions { DataFolder = _folder });

        var kader = Assert.Single(result.Absences, row => row.Personalnummer == 1001);
        Assert.False(kader.IstLangzeitkrank);
        Assert.Equal(60.2m, kader.KrankheitstageGesamt);
        var normal = Assert.Single(result.Absences, row => row.Personalnummer == 1002);
        Assert.True(normal.IstLangzeitkrank);
        Assert.Equal(61m, normal.KrankheitstageGesamt);
    }

    [Fact]
    public async Task BuildAsync_Zaehlt_Beide_Rexx_Felder_Und_Teilt_Nach_61_Tagen()
    {
        WriteWorkbook(Path.Combine(_folder, "Abwesenheitinstunden.xlsx"),
            [
                "Personalnummer", "Nachname, Vorname (Link Personal)", "Organisation", "Stelle", "Personal Status",
                "Krankheit angetreten (Stunden Ind.)", "Krank nicht buchbar angetreten (Stunden Ind.)"
            ],
            [
                // 40 + 21 Tage aus zwei Rexx-Feldern = 61 Tage: langzeitkrank.
                [1001, "Alpha, Anna", "Org A", "Engineer", "Aktiv", 40m * HrKpiDashboardBuilder.HoursPerWorkday, 21m * HrKpiDashboardBuilder.HoursPerWorkday],
                [1002, "Beta, Bruno", "Org B", "Engineer", "Aktiv", 16.0, 0]
            ]);

        var result = await _service.BuildAsync(new HrKpiOptions { DataFolder = _folder });

        var alpha = Assert.Single(result.Absences, row => row.Personalnummer == 1001);
        Assert.True(alpha.IstLangzeitkrank);
        Assert.Equal(61m, alpha.KrankheitstageLang);
        Assert.Equal(0m, alpha.KrankheitstageKurz);
        var beta = Assert.Single(result.Absences, row => row.Personalnummer == 1002);
        Assert.False(beta.IstLangzeitkrank);
        Assert.Equal(2m, beta.KrankheitstageKurz);

        var longSick = Assert.Single(result.AbsenceMetrics, metric => metric.Label == "Krankheit Lang");
        Assert.Equal("61.0", longSick.Value.Replace("’", "").Replace("'", ""));
        Assert.StartsWith("1 Langzeitkranke", longSick.Detail);
    }

    [Fact]
    public async Task BuildAsync_Schliesst_Reminderprofile_Ohne_Fte_Und_Ohne_Sollzeit_Aus()
    {
        RewriteEmployeeRows(
        [
            [1001, "Alpha, Anna", "Org A", "100 / Org A", "Engineer", "n", new DateTime(2020, 1, 1), "Aktiv", "0:00", 25, 0, 0, 100000, "CHF"],
            [1002, "Beta, Bruno", "Org B", "200 / Org B", "Engineer", "n", new DateTime(2024, 2, 1), "Aktiv", "0:00", 25, 0, 0, 90000, "CHF"],
            [3001, "Reminder, Profil", "Org A", "100 / Org A", "ICT", "n", new DateTime(2023, 1, 1), "Aktiv", "0:00", 0, 0, 0, 0, "CHF"],
            [3002, "Unbekannt, Join", "Org A", "100 / Org A", "Engineer", "n", new DateTime(2023, 1, 1), "Aktiv", "0:00", 25, 0, 0, 80000, "CHF"]
        ]);
        WriteWorkbook(Path.Combine(_folder, "Exportkommengehen.xlsx"),
            ["Nachname, Vorname (Link Personal)", "Geburtsdatum", "Arbeitszeitmodell", "O taegliche Sollarbeitszeit (Woche)"],
            [
                ["Alpha, Anna", new DateTime(1990, 1, 1), "Vollzeit", 8.0],
                ["Beta, Bruno", new DateTime(1991, 1, 1), "Teilzeit", 4.0],
                // Reminderprofil: in der Zeitdatei vorhanden, aber ohne Sollzeit und ohne SAP-Zeile.
                ["Reminder, Profil", new DateTime(2000, 1, 1), "", ""]
                // "Unbekannt, Join" fehlt in der Zeitdatei: das ist KEIN Beleg fuer ein Reminderprofil.
            ]);

        var result = await _service.BuildAsync(new HrKpiOptions { DataFolder = _folder });

        Assert.DoesNotContain(result.Employees, row => row.Personalnummer == 3001);
        Assert.Contains(result.Employees, row => row.Personalnummer == 3002);
        Assert.Contains(result.Notices, notice => notice.StartsWith("1 aktive Zeilen ohne SAP-Beschaeftigungsgrad und ohne Rexx-Sollzeit"));
    }

    [Fact]
    public async Task BuildAsync_Zeigt_Gleitende_Prognose_Und_Vorjahr_Neben_Der_Quartalsprognose()
    {
        var result = await _service.BuildAsync(new HrKpiOptions { DataFolder = _folder, Year = 2025 });

        Assert.Single(result.TurnoverMetrics, metric => metric.Label == "Fluktuation Prognose");
        var rolling = Assert.Single(result.TurnoverMetrics, metric => metric.Label == "Fluktuation Prognose gleitend");
        // Letzte zwoelf Monate bis 31.12.2025: nur Anna (AN-Kuendigung) zaehlt; Bruno ist AG, Tom Praktikant.
        Assert.StartsWith("Letzte 12 Monate: 1 relevante Austritte", rolling.Detail);
        var previousYear = Assert.Single(result.TurnoverMetrics, metric => metric.Label == "Fluktuation Vorjahr");
        // Vorjahr 2024 ist trotz Jahresfilter 2025 sichtbar: Fiona trat am 15.12.2024 aus.
        Assert.StartsWith("2024: 1 relevante Austritte", previousYear.Detail);
        Assert.Contains("Prognose", previousYear.Detail);
        Assert.Contains("gleitend", previousYear.Detail);
    }

    private static void AssertEveryVisibleBlockIsConsistent(HrKpiResult result, string because)
    {
        var employeeNumbers = result.Employees
            .Where(row => row.Personalnummer.HasValue)
            .Select(row => row.Personalnummer!.Value)
            .ToHashSet();
        var employeeNames = result.Employees.Select(row => row.NameVoll).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var leaverNumbers = result.Leavers
            .Where(row => row.Personalnummer.HasValue)
            .Select(row => row.Personalnummer!.Value)
            .ToHashSet();

        Assert.Equal(6, result.Metrics.Count);
        Assert.True(result.TurnoverMetrics.Count >= 6, because);
        Assert.Equal(7, result.AbsenceMetrics.Count);
        Assert.Equal(8, result.TimeVacationMetrics.Count);
        Assert.Equal(4, result.PeriodComparisonMetrics.Count);
        Assert.Equal(5, result.TrafficLights.Count);
        // Fuenf Rexx-/SAP-Dateien plus die SAP-Absenzen je Fall (HR_KPI.md 8.7), auch wenn sie fehlt.
        Assert.Equal(6, result.FileStatuses.Count);

        Assert.Equal(employeeNumbers.Count, result.HeadcountByOrganisation.Sum(row => row.Count));
        Assert.All(result.CriticalTimeBalances, row => Assert.Contains(row.NameVoll, employeeNames));
        Assert.All(result.CriticalAbsences, row => Assert.Contains(row.NameVoll, employeeNames));
        Assert.Equal(result.Absences.Sum(row => row.KrankheitstageGesamt), result.AbsenceByOrganisation.Sum(row => row.Value));
        Assert.All(result.FluctuationRelevantLeavers, row => Assert.Contains(row.Personalnummer!.Value, leaverNumbers));

        var funnelTotal = Assert.Single(result.TurnoverVisuals.FunnelSteps, row => row.Label == "Austritte Total");
        Assert.Equal(leaverNumbers.Count, funnelTotal.Count);
        Assert.Equal(
            leaverNumbers.Count,
            result.LeaversByOrganisation.Sum(row => row.Count));
    }

    private static void WriteFixtureFiles(string folder)
    {
        WriteWorkbook(Path.Combine(folder, "Saldiperstichdatum.xlsx"),
            [
                "Personalnummer", "Nachname, Vorname (Link Personal)", "Organisation", "Kostenstelle", "Stelle",
                "Leitung j/n", "Eintrittsdatum", "Personal Status", "Stunden Saldo", "Urlaubsanspruch",
                "Urlaub Rest", "Ferien ausstehend (Tage)", "Lohn", "Lohn Waehrung"
            ],
            [
                [1001, "Alpha, Anna", "Org A", "100 / Org A", "Engineer", "n", new DateTime(2020, 1, 1), "Aktiv", "120:00", 25, 8, 2, 100000, "CHF"],
                [1002, "Beta, Bruno", "Org B", "200 / Org B", "Engineer", "n", new DateTime(2024, 2, 1), "Aktiv", "10:00", 25, 4, 1, 90000, "CHF"],
                [1003, "Fallback, Fiona", "Org B", "200 / Org B", "Engineer", "n", new DateTime(2025, 1, 15), "Aktiv", "0:00", 25, 3, 0, 70000, "CHF"],
                ["", "NoNumber, Nora", "Org A", "100 / Org A", "Engineer", "n", new DateTime(2025, 2, 1), "Aktiv", "0:00", 25, 1, 0, 65000, "CHF"],
                [1004, "Inactive, Ivan", "Org A", "100 / Org A", "Engineer", "n", new DateTime(2021, 1, 1), "Inaktiv", "0:00", 25, 0, 0, 65000, "CHF"]
            ]);

        WriteWorkbook(Path.Combine(folder, "Exportkommengehen.xlsx"),
            ["Nachname, Vorname (Link Personal)", "Geburtsdatum", "Arbeitszeitmodell", "O taegliche Sollarbeitszeit (Woche)"],
            [
                ["Alpha, Anna", new DateTime(1990, 1, 1), "Vollzeit", 8.0],
                ["Beta, Bruno", new DateTime(1991, 1, 1), "Teilzeit", 4.0],
                ["Fallback, Fiona", new DateTime(1992, 1, 1), "Teilzeit", 4.0],
                ["NoNumber, Nora", new DateTime(1993, 1, 1), "Vollzeit", 8.0]
            ]);

        WriteWorkbook(Path.Combine(folder, "HR_KPI_Export.xlsx"),
            [
                "Personalnummer", "Buchungskreis", "Personalbereich", "Personalteilbereich", "Mitarbeitergruppe",
                "Mitarbeiterkreis", "Teilzeitkraft", "Beschaeftigungsgrad %", "Geschlecht", "Planstelle",
                "Stellenschluessel", "Nichtberufsunfall Tage", "Berufsunfall Tage", "Abrechnungskreis"
            ],
            [
                [1001, "CH01", "PB", "PTB", "MG", "MK", "Nein", 100, 2, "P1", "S1", 0, 0, "A"],
                [1002, "CH01", "PB", "PTB", "MG", "MK", "Ja", 50, 1, "P2", "S2", 0, 0, "A"]
            ]);

        WriteWorkbook(Path.Combine(folder, "Abwesenheitinstunden.xlsx"),
            [
                "Personalnummer", "Nachname, Vorname (Link Personal)", "Organisation", "Stelle", "Personal Status",
                "Krankheit angetreten (Stunden Ind.)", "Krank nicht buchbar angetreten (Stunden Ind.)"
            ],
            [
                [1001, "Alpha, Anna", "Org A", "Engineer", "Aktiv", 8.4, 0],
                [1002, "Beta, Bruno", "Org B", "Engineer", "Aktiv", 16.8, 0],
                [9999, "External, Elsa", "Org X", "Engineer", "Aktiv", 84, 0]
            ]);

        WriteWorkbook(Path.Combine(folder, "Personalausgeschieden.xlsx"),
            [
                "Personalnummer", "Nachname, Vorname (Link Personal)", "Organisation-1", "Stelle-1",
                "Personal Status", "Austrittsdatum", "Eintrittsdatum", "Austrittsart"
            ],
            [
                [1001, "Alpha, Anna", "Org A", "Engineer", "Inaktiv", new DateTime(2025, 3, 10), new DateTime(2020, 1, 1), "Arbeitnehmer Kuendigung"],
                [1002, "Beta, Bruno", "Org B", "Engineer", "Inaktiv", new DateTime(2025, 4, 5), new DateTime(2024, 2, 1), "Kuendigung Arbeitgeber"],
                [2001, "Trainee, Tom", "Org A", "Praktikant", "Inaktiv", new DateTime(2025, 5, 5), new DateTime(2025, 1, 1), "Arbeitnehmer Kuendigung"],
                [1003, "Fallback, Fiona", "Org B", "Engineer", "Inaktiv", new DateTime(2024, 12, 15), new DateTime(2025, 1, 15), "Arbeitnehmer Kuendigung"]
            ]);
    }

    private void AppendLeaverRow(
        int personalNumber,
        string name,
        string organisation,
        string position,
        DateTime exitDate,
        DateTime entryDate,
        string exitType)
    {
        var path = Path.Combine(_folder, "Personalausgeschieden.xlsx");
        using var workbook = new XLWorkbook(path);
        var sheet = workbook.Worksheets.First();
        var row = sheet.LastRowUsed()!.RowNumber() + 1;

        sheet.Cell(row, 1).Value = personalNumber;
        sheet.Cell(row, 2).Value = name;
        sheet.Cell(row, 3).Value = organisation;
        sheet.Cell(row, 4).Value = position;
        sheet.Cell(row, 5).Value = "Inaktiv";
        sheet.Cell(row, 6).Value = exitDate;
        sheet.Cell(row, 7).Value = entryDate;
        sheet.Cell(row, 8).Value = exitType;

        workbook.Save();
    }

    private void RewriteLeaverRows(object?[][] rows)
    {
        WriteWorkbook(Path.Combine(_folder, "Personalausgeschieden.xlsx"),
            [
                "Personalnummer", "Nachname, Vorname (Link Personal)", "Organisation-1", "Stelle-1",
                "Personal Status", "Austrittsdatum", "Eintrittsdatum", "Austrittsart"
            ],
            rows);
    }

    private void RewriteEmployeeRows(object?[][] rows)
    {
        WriteWorkbook(Path.Combine(_folder, "Saldiperstichdatum.xlsx"),
            [
                "Personalnummer", "Nachname, Vorname (Link Personal)", "Organisation", "Kostenstelle", "Stelle",
                "Leitung j/n", "Eintrittsdatum", "Personal Status", "Stunden Saldo", "Urlaubsanspruch",
                "Urlaub Rest", "Ferien ausstehend (Tage)", "Lohn", "Lohn Waehrung"
            ],
            rows);
    }

    private static void WriteWorkbook(string path, string[] headers, object?[][] rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Sheet1");

        for (var column = 0; column < headers.Length; column++)
            sheet.Cell(1, column + 1).Value = headers[column];

        for (var row = 0; row < rows.Length; row++)
        {
            for (var column = 0; column < rows[row].Length; column++)
                sheet.Cell(row + 2, column + 1).Value = XLCellValue.FromObject(rows[row][column]);
        }

        workbook.SaveAs(path);
    }
}
