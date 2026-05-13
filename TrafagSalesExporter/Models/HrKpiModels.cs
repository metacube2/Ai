namespace TrafagSalesExporter.Models;

public sealed class HrKpiOptions
{
    public string DataFolder { get; set; } = HrKpiDataSourceOptions.DefaultFolder;
    public int Year { get; set; } = DateTime.Today.Year;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? EntryYear { get; set; }
    public string? Organisationseinheit { get; set; }
    public string? KostenstelleText { get; set; }
    public string? Mitarbeitertyp { get; set; }
    public string? FluktuationFilter { get; set; }
    public string? GlzAmpel { get; set; }
    public string? RestferienAmpel { get; set; }
    public string? SearchText { get; set; }
}

public sealed class HrKpiDataSourceOptions
{
    public const string SectionName = "HrKpi";
    public const string DefaultFolder = @"C:\temp";

    public string DataFolder { get; set; } = DefaultFolder;
    public string MainFile { get; set; } = "Saldiperstichdatum.xlsx";
    public string TimeFile { get; set; } = "Exportkommengehen.xlsx";
    public string SapFile { get; set; } = "HR_KPI_Export.xlsx";
    public string AbsenceFile { get; set; } = "Abwesenheitinstunden.xlsx";
    public string LeaverFile { get; set; } = "Personalausgeschieden.xlsx";

    public HrKpiDataSourceOptions Normalize()
        => new()
        {
            DataFolder = NormalizeText(DataFolder, DefaultFolder),
            MainFile = NormalizeText(MainFile, "Saldiperstichdatum.xlsx"),
            TimeFile = NormalizeText(TimeFile, "Exportkommengehen.xlsx"),
            SapFile = NormalizeText(SapFile, "HR_KPI_Export.xlsx"),
            AbsenceFile = NormalizeText(AbsenceFile, "Abwesenheitinstunden.xlsx"),
            LeaverFile = NormalizeText(LeaverFile, "Personalausgeschieden.xlsx")
        };

    private static string NormalizeText(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}

public sealed class HrKpiResult
{
    public HrKpiOptions Options { get; set; } = new();
    public List<HrKpiFileStatus> FileStatuses { get; set; } = [];
    public List<string> Notices { get; set; } = [];
    public List<string> OrganisationOptions { get; set; } = [];
    public List<string> KostenstelleOptions { get; set; } = [];
    public List<int> EntryYearOptions { get; set; } = [];
    public List<string> MitarbeitertypOptions { get; set; } = [];
    public List<HrKpiMetric> Metrics { get; set; } = [];
    public List<HrKpiMetric> TurnoverMetrics { get; set; } = [];
    public List<HrKpiMetric> AbsenceMetrics { get; set; } = [];
    public List<HrKpiMetric> TimeVacationMetrics { get; set; } = [];
    public List<HrKpiEmployeeRow> Employees { get; set; } = [];
    public List<HrAbsenceRow> Absences { get; set; } = [];
    public List<HrLeaverRow> Leavers { get; set; } = [];
    public List<HrKpiGroupValue> HeadcountByOrganisation { get; set; } = [];
    public List<HrKpiEmployeeRow> CriticalTimeBalances { get; set; } = [];
    public List<HrLeaverRow> FluctuationRelevantLeavers { get; set; } = [];
    public HrTurnoverVisuals TurnoverVisuals { get; set; } = new();
}

public sealed class HrKpiFileStatus
{
    public string Label { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool Exists { get; set; }
    public int RowCount { get; set; }
    public string? Message { get; set; }
}

public sealed class HrKpiMetric
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string Severity { get; set; } = "Normal";
}

public sealed class HrKpiGroupValue
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public int Count { get; set; }
    public string Color { get; set; } = "#607d8b";
    public decimal Percent { get; set; }
}

public sealed class HrTurnoverVisuals
{
    public decimal YearRatePercent { get; set; }
    public string YearRateLabel { get; set; } = "0.0%";
    public string GaugeColor { get; set; } = "#2e7d32";
    public decimal GaugeRotationDegrees { get; set; }
    public List<HrKpiGroupValue> FunnelSteps { get; set; } = [];
    public List<HrKpiGroupValue> ExclusionReasons { get; set; } = [];
    public List<HrKpiGroupValue> RelevantByOrganisation { get; set; } = [];
    public List<HrKpiGroupValue> MonthlyRelevantLeavers { get; set; } = [];
}

public sealed class HrKpiEmployeeRow
{
    public int? Personalnummer { get; set; }
    public string NameVoll { get; set; } = string.Empty;
    public string Vorname { get; set; } = string.Empty;
    public string Nachname { get; set; } = string.Empty;
    public string Organisationseinheit { get; set; } = string.Empty;
    public string KostenstelleText { get; set; } = string.Empty;
    public int? Kostenstelle { get; set; }
    public string Stelle { get; set; } = string.Empty;
    public string Leitung { get; set; } = string.Empty;
    public DateTime? Eintrittsdatum { get; set; }
    public DateTime? Geburtsdatum { get; set; }
    public int? AlterJahre { get; set; }
    public string Altersgruppe { get; set; } = "Unbekannt";
    public string GeschlechtText { get; set; } = "Unbekannt";
    public decimal? BeschaeftigungsgradProzent { get; set; }
    public decimal Fte { get; set; }
    public bool IstTeilzeit { get; set; }
    public int? Dienstjahre { get; set; }
    public bool IstAktiv { get; set; }
    public string Mitarbeitertyp { get; set; } = "Festangestellt";
    public decimal StundenSaldo { get; set; }
    public string GlzAmpel { get; set; } = "Gruen";
    public decimal UrlaubRest { get; set; }
    public decimal Urlaubsanspruch { get; set; }
    public decimal FerienAusstehend { get; set; }
    public decimal Ferientage { get; set; }
    public string RestferienAmpel { get; set; } = "Gruen";
    public decimal Bruttolohn { get; set; }
    public string LohnWaehrung { get; set; } = string.Empty;
    public decimal BuTage { get; set; }
    public decimal NbuTage { get; set; }
    public string Buchungskreis { get; set; } = string.Empty;
    public string Personalbereich { get; set; } = string.Empty;
    public string Personalteilbereich { get; set; } = string.Empty;
    public string Mitarbeitergruppe { get; set; } = string.Empty;
    public string Mitarbeiterkreis { get; set; } = string.Empty;
    public string Planstelle { get; set; } = string.Empty;
    public string SollStelle { get; set; } = string.Empty;
    public DateTime Periode { get; set; } = new(DateTime.Today.Year, DateTime.Today.Month, 1);
}

public sealed class HrAbsenceRow
{
    public int? Personalnummer { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Organisationseinheit { get; set; } = string.Empty;
    public string Stelle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal KrankheitKurzStd { get; set; }
    public decimal KrankheitLangStd { get; set; }
    public decimal KrankheitGesamtStd { get; set; }
    public decimal KrankheitstageGesamt { get; set; }
    public decimal KrankheitstageKurz { get; set; }
    public decimal KrankheitstageLang { get; set; }
    public decimal KrankenquoteMa { get; set; }
}

public sealed class HrLeaverRow
{
    public int? Personalnummer { get; set; }
    public string NameVoll { get; set; } = string.Empty;
    public string Vorname { get; set; } = string.Empty;
    public string Nachname { get; set; } = string.Empty;
    public string Organisationseinheit { get; set; } = string.Empty;
    public string Stelle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? Austrittsdatum { get; set; }
    public DateTime? Eintrittsdatum { get; set; }
    public decimal? VerweildauerMonate { get; set; }
    public string Austrittsart { get; set; } = string.Empty;
    public string AustrittsartNormalisiert { get; set; } = string.Empty;
    public string Mitarbeitertyp { get; set; } = "Festangestellt";
    public bool IstArbeitnehmerkuendigung { get; set; }
    public bool IstFluktuationAusgeschlossen { get; set; }
    public bool IstFluktuationsrelevant { get; set; }
    public string? FluktuationAusschlussgrund { get; set; }
    public DateTime? Austrittsmonat { get; set; }
    public int? Austrittsjahr { get; set; }
}
