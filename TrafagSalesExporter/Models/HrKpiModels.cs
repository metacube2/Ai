namespace TrafagSalesExporter.Models;

public sealed class HrKpiOptions
{
    public string DataFolder { get; set; } = HrKpiDataSourceOptions.DefaultFolder;
    public int? Year { get; set; }
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
    public bool ManagementView { get; set; }
}

public sealed class HrKpiDataSourceOptions
{
    public const string SectionName = "HrKpi";
    public const string DefaultFolder = @"C:\temp";

    public string DataFolder { get; set; } = DefaultFolder;
    public string MainFile { get; set; } = "Saldiperstichdatum.xlsx";
    public string TimeFile { get; set; } = "Exportkommengehen.xlsx";
    public string SapFile { get; set; } = "HR_KPI_Export.xlsx";
    // Abwesenheiten je Fall aus SAP PA2001 (HrAbsenzSet), docs/HR_KPI.md 8.7.
    public string SapAbsenceFile { get; set; } = "HR_Absenzen_SAP.xlsx";
    public string AbsenceFile { get; set; } = "Abwesenheitinstunden.xlsx";
    public string LeaverFile { get; set; } = "Personalausgeschieden.xlsx";
    // Rexx-Gesamtabfrage #775 (seit 2026-11-04, docs/HR_KPI.md 8.13). Liegt sie im Datenordner, ersetzt sie
    // MainFile, TimeFile, AbsenceFile und LeaverFile; sonst laufen die vier Einzeldateien wie bisher.
    public string GesamtFile { get; set; } = "Hrcockpitgesamtdashboard.xlsx";
    public decimal AbsenceYellowThresholdPercent { get; set; } = 3m;
    public decimal AbsenceRedThresholdPercent { get; set; } = 5m;

    public HrKpiDataSourceOptions Normalize()
    {
        var yellow = AbsenceYellowThresholdPercent <= 0 ? 3m : AbsenceYellowThresholdPercent;
        var red = AbsenceRedThresholdPercent <= yellow ? 5m : AbsenceRedThresholdPercent;

        return new()
        {
            DataFolder = NormalizeText(DataFolder, DefaultFolder),
            MainFile = NormalizeText(MainFile, "Saldiperstichdatum.xlsx"),
            TimeFile = NormalizeText(TimeFile, "Exportkommengehen.xlsx"),
            SapFile = NormalizeText(SapFile, "HR_KPI_Export.xlsx"),
            SapAbsenceFile = NormalizeText(SapAbsenceFile, "HR_Absenzen_SAP.xlsx"),
            AbsenceFile = NormalizeText(AbsenceFile, "Abwesenheitinstunden.xlsx"),
            LeaverFile = NormalizeText(LeaverFile, "Personalausgeschieden.xlsx"),
            GesamtFile = NormalizeText(GesamtFile, "Hrcockpitgesamtdashboard.xlsx"),
            AbsenceYellowThresholdPercent = yellow,
            AbsenceRedThresholdPercent = red
        };
    }

    private static string NormalizeText(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}

public sealed class HrKpiResult
{
    public bool AbsenceRatesReliable { get; set; } = true;
    public HrKpiOptions Options { get; set; } = new();
    public List<HrKpiFileStatus> FileStatuses { get; set; } = [];
    public List<string> Notices { get; set; } = [];
    public List<string> OrganisationOptions { get; set; } = [];
    public List<string> KostenstelleOptions { get; set; } = [];
    public List<int> ExitYearOptions { get; set; } = [];
    public List<int> EntryYearOptions { get; set; } = [];
    public List<string> MitarbeitertypOptions { get; set; } = [];
    public List<HrKpiMetric> Metrics { get; set; } = [];
    public List<HrKpiMetric> TurnoverMetrics { get; set; } = [];
    public List<HrKpiMetric> AbsenceMetrics { get; set; } = [];
    public List<HrKpiMetric> TimeVacationMetrics { get; set; } = [];
    public List<HrKpiMetric> PeriodComparisonMetrics { get; set; } = [];
    public List<HrKpiTrafficLight> TrafficLights { get; set; } = [];
    public List<HrKpiDataQualityIssue> DataQualityIssues { get; set; } = [];
    public List<HrKpiGroupValue> LeaversByType { get; set; } = [];
    public List<HrKpiGroupValue> LeaversByOrganisation { get; set; } = [];
    public List<HrKpiGroupValue> AbsenceByOrganisation { get; set; } = [];
    public List<HrKpiEmployeeRow> CriticalAbsences { get; set; } = [];
    public List<HrKpiEmployeeRow> Employees { get; set; } = [];
    public List<HrAbsenceRow> Absences { get; set; } = [];
    public List<HrLeaverRow> Leavers { get; set; } = [];
    public List<HrKpiGroupValue> HeadcountByOrganisation { get; set; } = [];
    public List<HrKpiEmployeeRow> CriticalTimeBalances { get; set; } = [];
    public List<HrLeaverRow> FluctuationRelevantLeavers { get; set; } = [];
    public HrTurnoverVisuals TurnoverVisuals { get; set; } = new();
    // Reiter Interaktiv (2026-11-04, docs/HR_KPI.md Abschnitt 9): Austritte aller Jahre (nur Strukturfilter wie der
    // Vorjahresvergleich), SAP-Absenzen je Fall der gefilterten aktiven Personen und der Stichtag der Fluktuation.
    public List<HrLeaverRow> TimelineLeavers { get; set; } = [];
    // Aktive Mitarbeitende fuer Headcount-Zeitreihen: dieselben Filter wie die Fluktuation (ohne Kostenstelle, GLZ und
    // Restferien, docs/HR_KPI.md Abschnitt 6), damit Zaehler (TimelineLeavers) und Nenner zusammenpassen.
    public List<HrKpiEmployeeRow> TimelineEmployees { get; set; } = [];
    public List<TrafagSalesExporter.Services.HrAbsenceSapCase> AbsenceCases { get; set; } = [];
    public DateTime AnchorDate { get; set; } = DateTime.Today;
}

public sealed class HrKpiFileStatus
{
    public string Label { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool Exists { get; set; }
    public int RowCount { get; set; }
    public string? Message { get; set; }
    public DateTime? LastModified { get; set; }
    public int? AgeDays { get; set; }
    public string FreshnessStatus { get; set; } = "Unbekannt";
}

public sealed class HrKpiTrafficLight
{
    public string Area { get; set; } = string.Empty;
    public string Status { get; set; } = "Gruen";
    public string Value { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
}

public sealed class HrKpiDataQualityIssue
{
    public string Severity { get; set; } = "Info";
    public string Area { get; set; } = string.Empty;
    public string Issue { get; set; } = string.Empty;
    public int Count { get; set; }
    public string Detail { get; set; } = string.Empty;
}

public sealed class HrKpiMetric
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string Severity { get; set; } = "Normal";
    public string Theme { get; set; } = "Default";
    public string HelpText { get; set; } = string.Empty;
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
    public string RateTitle { get; set; } = "Fluktuation Auswahl";
    public decimal YearRatePercent { get; set; }
    public string YearRateLabel { get; set; } = "0.0%";
    public string GaugeColor { get; set; } = "#2e7d32";
    public decimal GaugeRotationDegrees { get; set; }
    public string TimelineTitle { get; set; } = "Relevante Austritte";
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
    // Weder SAP-Beschaeftigungsgrad noch Rexx-Sollzeit: laut HR ein Reminderprofil wie ICT,
    // keine echte Person. Wird aus allen Kennzahlen ausgeschlossen.
    public bool IstReminderprofil { get; set; }
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
    public DateTime? VonDatum { get; set; }
    public DateTime? BisDatum { get; set; }
    public decimal KrankheitKurzStd { get; set; }
    public decimal KrankheitLangStd { get; set; }
    public decimal KrankheitGesamtStd { get; set; }
    public decimal KrankheitstageGesamt { get; set; }
    public decimal KrankheitstageKurz { get; set; }
    public decimal KrankheitstageLang { get; set; }
    public decimal KrankenquoteMa { get; set; }
    // Ab dem 61. Krankheitstag gilt die Krankheit als Langzeitkrankheit (Sonja Richter, HR,
    // Antwort auf Ingos Fragen vom 2026-08-19). Gezaehlt wird die Summe je Person im Export.
    public bool IstLangzeitkrank { get; set; }
    // 8,0 h, fuer Kader (Rexx „Leitung j/n“ = ja) 8,1 h; siehe HrKpiDashboardBuilder.HoursPerWorkdayFor.
    public decimal StundenProArbeitstag { get; set; } = 8.0m;
    // Rexx #744 „Krankheit angetreten (Zeitraum)“ und „Krank nicht buchbar angetreten (Zeitraum)“: Zeitraum des juengsten
    // Falls je Person; dient seit 2026-11-04 nur dazu, in SAP noch fehlende juengste Krankheitsfaelle zu ergaenzen.
    public string RexxKrankheitZeitraum { get; set; } = string.Empty;
    public string RexxKrankNichtBuchbarZeitraum { get; set; } = string.Empty;
    // Seit 2026-11-04 (#775): juengster Unfall der Person, ergaenzt fehlende SAP-Unfaelle (0290) wie bei Krankheit.
    public string RexxUnfallZeitraum { get; set; } = string.Empty;
    // Herkunft der Zeile: true, wenn sie ein aus Rexx ergaenzter Krankheitsfall ist (SAP hatte ihn noch nicht).
    public bool AusRexxErgaenzt { get; set; }
    // Seit 2026-11-04: Zeile ist ein Fall mit Datum (SAP oder aus Rexx ergaenzt); Kurz/Lang steht dann schon fest
    // (je Krankheit ab dem 61. Tag) und wird nicht mehr aus der Summe je Person abgeleitet.
    public bool JeFall { get; set; }
    // Unfall (SAP 0290); zaehlt in die Absenzquote, nicht in die 60-Tage-Regel.
    public bool IstUnfall { get; set; }
    public decimal UnfallStd { get; set; }
    public decimal UnfalltageGesamt { get; set; }
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
