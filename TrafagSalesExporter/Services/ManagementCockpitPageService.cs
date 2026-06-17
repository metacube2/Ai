using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface IManagementCockpitPageService
{
    Task<ManagementCockpitPageState> InitializeAsync(string? selectedFilePath, int selectedCentralYear);
    Task<List<ManagementCockpitFileOption>> LoadFilesAsync();
    Task<List<int>> LoadCentralYearsAsync();
    Task<ManagementCockpitResult> AnalyzeAsync(string filePath, ManagementCockpitAnalysisOptions options);
    Task<ManagementCockpitCentralResult> AnalyzeCentralAsync(int year, int? month, ManagementCockpitAnalysisOptions options);
    Task<ManagementFinanceSummaryResult> AnalyzeFinanceSummaryAsync(int year, string? countryKey, string? currency);
    Task<ManagementDecisionResult> BuildManagementDecisionsAsync(ManagementFinanceSummaryResult financeResult);
}

public sealed class ManagementCockpitPageService : IManagementCockpitPageService
{
    private readonly IManagementCockpitService _cockpitService;
    private readonly IPurchasingDashboardService _purchasingDashboardService;
    private readonly IHrKpiService _hrKpiService;

    public ManagementCockpitPageService(
        IManagementCockpitService cockpitService,
        IPurchasingDashboardService purchasingDashboardService,
        IHrKpiService hrKpiService)
    {
        _cockpitService = cockpitService;
        _purchasingDashboardService = purchasingDashboardService;
        _hrKpiService = hrKpiService;
    }

    public async Task<ManagementCockpitPageState> InitializeAsync(string? selectedFilePath, int selectedCentralYear)
    {
        var files = await _cockpitService.GetAvailableFilesAsync();
        var years = await _cockpitService.GetAvailableCentralYearsAsync();

        return new ManagementCockpitPageState
        {
            Files = files,
            ValueFieldOptions = _cockpitService.GetValueFieldOptions().ToList(),
            CentralYears = years,
            SelectedFilePath = selectedFilePath ?? files.FirstOrDefault()?.Path,
            SelectedCentralYear = selectedCentralYear == 0 ? years.LastOrDefault() : selectedCentralYear
        };
    }

    public Task<List<ManagementCockpitFileOption>> LoadFilesAsync()
        => _cockpitService.GetAvailableFilesAsync();

    public Task<List<int>> LoadCentralYearsAsync()
        => _cockpitService.GetAvailableCentralYearsAsync();

    public Task<ManagementCockpitResult> AnalyzeAsync(string filePath, ManagementCockpitAnalysisOptions options)
        => _cockpitService.AnalyzeAsync(filePath, options);

    public Task<ManagementCockpitCentralResult> AnalyzeCentralAsync(int year, int? month, ManagementCockpitAnalysisOptions options)
        => _cockpitService.AnalyzeCentralAsync(year, month, options);

    public Task<ManagementFinanceSummaryResult> AnalyzeFinanceSummaryAsync(int year, string? countryKey, string? currency)
        => _cockpitService.AnalyzeFinanceSummaryAsync(year, countryKey, currency);

    public async Task<ManagementDecisionResult> BuildManagementDecisionsAsync(ManagementFinanceSummaryResult financeResult)
    {
        var result = new ManagementDecisionResult();
        AddFinanceDecisions(result.Items, financeResult);
        await AddPurchasingDecisionsAsync(result.Items);
        await AddHrDecisionsAsync(result.Items, financeResult.Filter.Year);

        result.Items = result.Items
            .OrderByDescending(item => SeverityRank(item.Severity))
            .ThenByDescending(item => item.Score)
            .ThenBy(item => item.Area, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Topic, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return result;
    }

    private static void AddFinanceDecisions(List<ManagementDecisionItem> items, ManagementFinanceSummaryResult finance)
    {
        foreach (var row in finance.DeviationRows.Take(8))
        {
            var severity = Math.Abs(row.DifferencePercent ?? 0m) >= 5m ? "High" : "Medium";
            items.Add(new ManagementDecisionItem
            {
                Area = "Finance",
                Topic = $"Soll/Ist-Abweichung {row.CountryKey}",
                Severity = severity,
                Score = BuildScore(severity, Math.Abs(row.DifferencePercent ?? 0m), row.TotalRows),
                Metric = FormatDecisionAmount(row.Difference ?? 0m, row.Currency),
                Impact = "Finance-Abschluss und Management-Reporting koennen vom Referenzwert abweichen.",
                Recommendation = "Abweichung fachlich klaeren und Referenzwert oder Importlogik bestaetigen.",
                Decision = "Sollwert, Importumfang oder Finance-Regel freigeben.",
                Owner = "Finance",
                Source = "Finance Summary / Abweichungen",
                Link = "management-cockpit?section=deviations"
            });
        }

        if (finance.GroupMarginSummary.MissingCostRows > 0 || finance.GroupMarginSummary.UnclearSupplierRows > 0)
        {
            var openRows = finance.GroupMarginSummary.MissingCostRows + finance.GroupMarginSummary.UnclearSupplierRows;
            items.Add(new ManagementDecisionItem
            {
                Area = "Finance / Einkauf",
                Topic = "Gruppenmarge Kostenbasis offen",
                Severity = "High",
                Score = BuildScore("High", openRows, finance.GroupMarginSummary.RowCount),
                Metric = $"{openRows:N0} offene Zeilen",
                Impact = "Gruppenmarge ist nicht belastbar, solange Standardpreise oder Lieferantentypen fehlen.",
                Recommendation = "Kostenbasisregel und Lieferantenklassifikation gemeinsam festlegen.",
                Decision = "Welche Kostenquelle gilt fuer externe/interne Lieferanten?",
                Owner = "Finance / Einkauf",
                Source = "Gruppenmarge",
                Link = "management-cockpit?section=groupmargin"
            });
        }

        var unresolvedShare = finance.ProductFinanceSummary.UnassignedValuePercent +
                              finance.ProductFinanceSummary.MissingReferenceValuePercent;
        if (unresolvedShare >= 5m)
        {
            items.Add(new ManagementDecisionItem
            {
                Area = "Finance / Einkauf",
                Topic = "Spartenmapping Umsatzabdeckung",
                Severity = unresolvedShare >= 15m ? "High" : "Medium",
                Score = BuildScore(unresolvedShare >= 15m ? "High" : "Medium", unresolvedShare, finance.ProductAssignmentSummary.DistinctMaterialCount),
                Metric = $"{unresolvedShare:N1}% Umsatz nicht sauber zugeordnet",
                Impact = "Spartenumsatz und Managementsicht koennen verzerrt sein.",
                Recommendation = "Materialnummern ohne TR-AG-Referenz priorisiert klaeren.",
                Decision = "Mappingregel oder Stammdatenkorrektur freigeben.",
                Owner = "Einkauf / Product Management",
                Source = "Spartenanalyse",
                Link = "management-cockpit?section=division"
            });
        }

        foreach (var issue in finance.DataQualityRows.Where(row => row.Count > 0).Take(6))
        {
            items.Add(new ManagementDecisionItem
            {
                Area = "Finance",
                Topic = issue.Issue,
                Severity = issue.Severity,
                Score = BuildScore(issue.Severity, issue.Count, finance.IncludedRows + finance.ExcludedRows),
                Metric = $"{issue.Count:N0} Treffer",
                Impact = "Datenqualitaet kann Auswertung, Spartenlogik oder Margenberechnung beeinflussen.",
                Recommendation = "Korrekturquelle und Verantwortlichkeit festlegen.",
                Decision = "Bereinigung im Quellsystem oder Dashboard-Regel akzeptieren?",
                Owner = "Finance / Datenowner",
                Source = "Finance Datenqualitaet",
                Link = "management-cockpit?section=quality"
            });
        }
    }

    private async Task AddPurchasingDecisionsAsync(List<ManagementDecisionItem> items)
    {
        try
        {
            var purchasing = await _purchasingDashboardService.LoadAsync();
            if (!purchasing.EkkoLoaded)
            {
                items.Add(new ManagementDecisionItem
                {
                    Area = "Einkauf",
                    Topic = "Einkaufsdaten nicht vollstaendig geladen",
                    Severity = "Medium",
                    Score = 45,
                    Metric = string.IsNullOrWhiteSpace(purchasing.Message) ? "kein Live-/Cache-Stand" : purchasing.Message,
                    Impact = "Einkaufsrisiken und offene Bestellwerte koennen nicht belastbar bewertet werden.",
                    Recommendation = "Einkauf Full Load oder Datenquelle pruefen.",
                    Decision = "Datenrefresh fuer Einkauf freigeben.",
                    Owner = "Einkauf / IT",
                    Source = "Einkauf Cockpit",
                    Link = "einkauf"
                });
                return;
            }

            if (purchasing.OpenValueSample > 0m)
            {
                items.Add(new ManagementDecisionItem
                {
                    Area = "Einkauf",
                    Topic = "Offener Bestellwert",
                    Severity = purchasing.OpenValueSample >= 1_000_000m ? "High" : "Medium",
                    Score = BuildScore(purchasing.OpenValueSample >= 1_000_000m ? "High" : "Medium", purchasing.OpenValueSample / 100000m, purchasing.PurchaseOrderCount),
                    Metric = FormatDecisionAmount(purchasing.OpenValueSample, "CHF"),
                    Impact = "Gebundene Einkaufswerte koennen Liquiditaet und Lieferfaehigkeit beeinflussen.",
                    Recommendation = "Top-Lieferanten und Faelligkeiten im Einkauf Cockpit pruefen.",
                    Decision = "Priorisierung offener Bestellungen bestaetigen.",
                    Owner = "Einkauf",
                    Source = "Einkauf Cockpit / offene Werte",
                    Link = "einkauf"
                });
            }

            var deliveryRisk = purchasing.DeliveryRiskChartRows.Sum(row => row.Value);
            if (deliveryRisk > 0m)
            {
                items.Add(new ManagementDecisionItem
                {
                    Area = "Einkauf",
                    Topic = "Liefertermin-Risiko",
                    Severity = deliveryRisk >= 500_000m ? "High" : "Medium",
                    Score = BuildScore(deliveryRisk >= 500_000m ? "High" : "Medium", deliveryRisk / 100000m, purchasing.DeliveryRiskRows.Count),
                    Metric = FormatDecisionAmount(deliveryRisk, "CHF"),
                    Impact = "Ueberfaellige oder kurzfristige Lieferungen koennen Umsatz und Produktion beeintraechtigen.",
                    Recommendation = "Hotlist nach Lieferant/Artikel abarbeiten.",
                    Decision = "Eskalation bei kritischen Lieferanten freigeben.",
                    Owner = "Einkauf / Operations",
                    Source = "Einkauf Liefertermin-Risiko",
                    Link = "einkauf"
                });
            }

            var qualityIssues = purchasing.DataQualityChartRows.Sum(row => row.Value);
            if (qualityIssues > 0m)
            {
                items.Add(new ManagementDecisionItem
                {
                    Area = "Einkauf",
                    Topic = "Einkauf Datenqualitaet",
                    Severity = qualityIssues >= 1000m ? "High" : "Medium",
                    Score = BuildScore(qualityIssues >= 1000m ? "High" : "Medium", qualityIssues, purchasing.PositionSampleCount + purchasing.PurchaseOrderCount),
                    Metric = $"{qualityIssues:N0} Fehler",
                    Impact = "Lieferanten-, Warengruppen- und Artikelanalysen koennen unvollstaendig sein.",
                    Recommendation = "Pflichtfelder und SAP-Korrekturprozess definieren.",
                    Decision = "Datenqualitaetsregel fuer Einkauf verbindlich machen.",
                    Owner = "Einkauf / Stammdaten",
                    Source = "Einkauf Datenqualitaet",
                    Link = "einkauf"
                });
            }
        }
        catch (Exception ex)
        {
            items.Add(new ManagementDecisionItem
            {
                Area = "Einkauf",
                Topic = "Einkaufsentscheidungen nicht berechenbar",
                Severity = "Info",
                Score = 10,
                Metric = ex.Message,
                Impact = "Einkaufssignale fehlen im Entscheidungsradar.",
                Recommendation = "Einkauf Cockpit separat pruefen.",
                Decision = "Keine Managemententscheidung ohne Einkaufsdaten.",
                Owner = "Einkauf / IT",
                Source = "Einkauf Cockpit",
                Link = "einkauf"
            });
        }
    }

    private async Task AddHrDecisionsAsync(List<ManagementDecisionItem> items, int year)
    {
        try
        {
            var hr = await _hrKpiService.BuildAsync(new HrKpiOptions
            {
                Year = year,
                ManagementView = true
            });

            foreach (var light in hr.TrafficLights.Where(row => row.Status is "Rot" or "Gelb").Take(6))
            {
                var severity = light.Status == "Rot" ? "High" : "Medium";
                items.Add(new ManagementDecisionItem
                {
                    Area = "HR",
                    Topic = light.Area,
                    Severity = severity,
                    Score = BuildScore(severity, 1, 1),
                    Metric = light.Value,
                    Impact = "HR-Risiko kann Kapazitaet, Fuehrung oder Standortstabilitaet beeinflussen.",
                    Recommendation = "Aggregierte HR-Ampel mit HR besprechen; keine Personendaten im Management-Reiter.",
                    Decision = "Massnahmenpaket oder Beobachtung je HR-Thema freigeben.",
                    Owner = "HR / GL",
                    Source = "HR Ampel",
                    Link = "hr-kpi"
                });
            }

            foreach (var issue in hr.DataQualityIssues.Where(row => row.Count > 0).Take(4))
            {
                items.Add(new ManagementDecisionItem
                {
                    Area = "HR",
                    Topic = $"HR Datenqualitaet: {issue.Issue}",
                    Severity = issue.Severity,
                    Score = BuildScore(issue.Severity, issue.Count, 1),
                    Metric = $"{issue.Count:N0} Treffer",
                    Impact = "HR-KPIs koennen nur aggregiert belastbar interpretiert werden, wenn die Quelldaten vollstaendig sind.",
                    Recommendation = "HR-Dateien und Aktualitaet pruefen.",
                    Decision = "Datenkorrektur oder KPI-Einschraenkung bestaetigen.",
                    Owner = "HR",
                    Source = "HR Datenqualitaet",
                    Link = "hr-kpi"
                });
            }
        }
        catch (Exception ex)
        {
            items.Add(new ManagementDecisionItem
            {
                Area = "HR",
                Topic = "HR-Signale nicht berechenbar",
                Severity = "Info",
                Score = 10,
                Metric = ex.Message,
                Impact = "HR-Entscheidungen fehlen im Entscheidungsradar.",
                Recommendation = "HR KPI separat entsperren und Datenstand pruefen.",
                Decision = "Keine HR-Entscheidung ohne aggregierte HR-Ampel.",
                Owner = "HR",
                Source = "HR KPI",
                Link = "hr-kpi"
            });
        }
    }

    private static int BuildScore(string severity, decimal impact, int volume)
        => Math.Clamp(SeverityRank(severity) * 25 + (int)Math.Min(impact, 40m) + Math.Min(volume / 100, 20), 0, 100);

    private static int SeverityRank(string severity)
        => severity.ToUpperInvariant() switch
        {
            "HIGH" or "ERROR" or "ROT" => 3,
            "MEDIUM" or "WARNING" or "GELB" => 2,
            _ => 1
        };

    private static string FormatDecisionAmount(decimal value, string currency)
        => $"{value:N0} {currency}".Trim();
}

public sealed class ManagementCockpitPageState
{
    public List<ManagementCockpitFileOption> Files { get; set; } = [];
    public List<ManagementCockpitValueFieldOption> ValueFieldOptions { get; set; } = [];
    public List<int> CentralYears { get; set; } = [];
    public string? SelectedFilePath { get; set; }
    public int SelectedCentralYear { get; set; }
}
