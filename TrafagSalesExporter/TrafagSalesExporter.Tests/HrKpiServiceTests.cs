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

        var turnoverHeadcount = Assert.Single(result.TurnoverMetrics, metric => metric.Label == "Headcount Festangestellt");
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

        var avgYearHeadcount = Assert.Single(result.TurnoverMetrics, metric => metric.Label == "Avg Headcount Jahr");
        Assert.Equal(3.5m.ToString("N1"), avgYearHeadcount.Value);

        var yearRate = Assert.Single(result.TurnoverMetrics, metric => metric.Label == "Fluktuation Jahr Effektiv %");
        Assert.Equal((1m / 3.5m).ToString("P1"), yearRate.Value);
        Assert.Equal("Austritte Jahr / Avg HC Jahr", yearRate.Detail);
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
        Assert.Contains(result.Notices, notice => notice.Contains("FTE-Fallback", StringComparison.OrdinalIgnoreCase));
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

        Assert.Equal("1", Assert.Single(result.TurnoverMetrics, metric => metric.Label == "Austritte Arbeitnehmerkuendigung").Value);
        Assert.Equal("1", Assert.Single(result.TurnoverMetrics, metric => metric.Label == "Austritte Fluktuationsrelevant").Value);
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
                ["Alpha, Anna", new DateTime(1990, 1, 1), "Vollzeit", 8.4],
                ["Beta, Bruno", new DateTime(1991, 1, 1), "Teilzeit", 4.2],
                ["Fallback, Fiona", new DateTime(1992, 1, 1), "Teilzeit", 4.2],
                ["NoNumber, Nora", new DateTime(1993, 1, 1), "Vollzeit", 8.4]
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
