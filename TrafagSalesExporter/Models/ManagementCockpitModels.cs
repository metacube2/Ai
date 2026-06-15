namespace TrafagSalesExporter.Models;

public class ManagementCockpitFileOption
{
    public string Path { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
}

public static class ManagementCockpitValueFieldKeys
{
    public const string SalesPriceValue = nameof(SalesPriceValue);
    public const string Quantity = nameof(Quantity);
    public const string StandardCost = nameof(StandardCost);
    public const string StandardCostTotal = nameof(StandardCostTotal);
}

public static class ManagementCockpitCurrencyOptions
{
    public const string Native = "NATIVE";
    public const string Chf = "CHF";
    public const string Eur = "EUR";
    public const string Usd = "USD";
}

public class ManagementCockpitValueFieldOption
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsCurrencyAmount { get; set; }
}

public class ManagementCockpitAnalysisOptions
{
    public string ValueField { get; set; } = ManagementCockpitValueFieldKeys.SalesPriceValue;
    public List<string> AdditionalValueFields { get; set; } = [];
    public string TargetCurrency { get; set; } = ManagementCockpitCurrencyOptions.Native;
    public string? LandFilter { get; set; }
    public string? TscFilter { get; set; }
}

public class ManagementCockpitSummary
{
    public string Land { get; set; } = string.Empty;
    public string Tsc { get; set; } = string.Empty;
    public DateTime? ExtractionDate { get; set; }
    public int RowCount { get; set; }
    public int InvoiceCount { get; set; }
    public int CustomerCount { get; set; }
    public string ValueFieldKey { get; set; } = ManagementCockpitValueFieldKeys.SalesPriceValue;
    public string ValueFieldLabel { get; set; } = "Sales Price/Value";
    public string DisplayCurrency { get; set; } = string.Empty;
    public int MissingExchangeRateCount { get; set; }
    public decimal AggregatedValueTotal { get; set; }
    public decimal SalesValueTotal { get; set; }
    public decimal EstimatedCostTotal { get; set; }
    public decimal EstimatedMarginTotal { get; set; }
    public decimal EstimatedMarginPercent { get; set; }
    public decimal ServiceSharePercent { get; set; }
    public decimal MissingOrderDatePercent { get; set; }
    public decimal MissingSupplierPercent { get; set; }
}

public class ManagementCockpitFinding
{
    public string Severity { get; set; } = "Info";
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
}

public class ManagementCockpitTopItem
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal SharePercent { get; set; }
}

public class ManagementCockpitResult
{
    public string FilePath { get; set; } = string.Empty;
    public ManagementCockpitSummary Summary { get; set; } = new();
    public List<ManagementCockpitFinding> Findings { get; set; } = [];
    public List<ManagementCockpitTopItem> TopCustomers { get; set; } = [];
    public List<ManagementCockpitTopItem> TopProductGroups { get; set; } = [];
    public List<ManagementCockpitTopItem> TopSalesEmployees { get; set; } = [];
    public Dictionary<string, int> DataQualityCounts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class ManagementCockpitCentralFilter
{
    public int Year { get; set; }
    public int? Month { get; set; }
    public string ValueField { get; set; } = ManagementCockpitValueFieldKeys.SalesPriceValue;
    public string TargetCurrency { get; set; } = ManagementCockpitCurrencyOptions.Native;
    public string? Land { get; set; }
    public string? Tsc { get; set; }
}

public class ManagementCockpitCentralSummary
{
    public int RowCount { get; set; }
    public int InvoiceCount { get; set; }
    public int SiteCount { get; set; }
    public int CountryCount { get; set; }
    public int CurrencyCount { get; set; }
    public string ValueFieldKey { get; set; } = ManagementCockpitValueFieldKeys.SalesPriceValue;
    public string ValueFieldLabel { get; set; } = "Sales Price/Value";
    public string DisplayCurrency { get; set; } = string.Empty;
    public decimal ValueTotal { get; set; }
    public int MissingExchangeRateCount { get; set; }
    public string ExchangeRateDateField { get; set; } = ExchangeRateDateFields.PostingDate;
    public string ExchangeRateDateLabel { get; set; } = "PostingDate / Buchungsdatum";
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
}

public class ManagementCockpitTimeValueRow
{
    public string Label { get; set; } = string.Empty;
    public int? Year { get; set; }
    public int? Month { get; set; }
    public int? Day { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal SalesValue { get; set; }
    public Dictionary<string, ManagementCockpitAggregatedFieldValue> AdditionalValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public int RowCount { get; set; }
}

public class ManagementCockpitAggregatedFieldValue
{
    public string FieldKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public int MissingExchangeRateCount { get; set; }
}

public class ManagementCockpitDimensionValueRow
{
    public string Label { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public decimal SalesValue { get; set; }
    public int RowCount { get; set; }
    public int InvoiceCount { get; set; }
}

public class ManagementCockpitCentralResult
{
    public ManagementCockpitCentralFilter Filter { get; set; } = new();
    public ManagementCockpitCentralSummary Summary { get; set; } = new();
    public List<string> Notices { get; set; } = [];
    public List<ManagementCockpitValueFieldOption> AdditionalValueFields { get; set; } = [];
    public List<ManagementCockpitTimeValueRow> YearlyTotals { get; set; } = [];
    public List<ManagementCockpitTimeValueRow> MonthlyTotals { get; set; } = [];
    public List<ManagementCockpitTimeValueRow> DailyTotals { get; set; } = [];
    public List<ManagementCockpitDimensionValueRow> SourceSystemTotals { get; set; } = [];
    public List<ManagementCockpitDimensionValueRow> CountryTotals { get; set; } = [];
}

public class ManagementFinanceSummaryFilter
{
    public int Year { get; set; }
    public string? CountryKey { get; set; }
    public string? Currency { get; set; }
}

public class ManagementFinanceSummaryRow
{
    public int Year { get; set; }
    public string CountryKey { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public int IncludedRows { get; set; }
    public int ExcludedRows { get; set; }
    public int TotalRows { get; set; }
    public decimal NetSalesActual { get; set; }
    public decimal NetSalesActualExcludingIntercompany { get; set; }
    public decimal IntercompanyValue { get; set; }
    public decimal IntercompanySharePercent { get; set; }
    public decimal Quantity { get; set; }
    public decimal CreditValue { get; set; }
    public int CreditRows { get; set; }
    public decimal IncludeRatePercent { get; set; }
    public decimal ExcludeRatePercent { get; set; }
}

public class ManagementFinanceCountryStatusRow : ManagementFinanceSummaryRow
{
    public string SourceSystems { get; set; } = string.Empty;
    public string Tscs { get; set; } = string.Empty;
    public decimal? ReferenceValue { get; set; }
    public decimal? Difference { get; set; }
    public decimal? DifferencePercent { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ManagementFinanceDataStatusRow
{
    public string Land { get; set; } = string.Empty;
    public string Tsc { get; set; } = string.Empty;
    public string SourceSystem { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int RowCount { get; set; }
    public DateTime? LatestStoredAtUtc { get; set; }
    public DateTime? LatestExtractionDate { get; set; }
    public DateTime? LatestExportAt { get; set; }
    public string LatestExportStatus { get; set; } = string.Empty;
    public string ManualImportFilePath { get; set; } = string.Empty;
    public DateTime? ManualImportLastUploadedAtUtc { get; set; }
}

public class ManagementFinanceCreditCandidateRow
{
    public string CountryKey { get; set; } = string.Empty;
    public string Tsc { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public decimal NetSalesActual { get; set; }
    public decimal Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class ManagementFinanceDataQualityRow
{
    public string Issue { get; set; } = string.Empty;
    public int Count { get; set; }
    public string Severity { get; set; } = "Info";
}

public class ManagementProductAssignmentSummary
{
    public int DistinctMaterialCount { get; set; }
    public int MatchedMaterialCount { get; set; }
    public int MiscMaterialCount { get; set; }
    public int UnassignedMaterialCount { get; set; }
    public int MissingReferenceMaterialCount { get; set; }
    public int MissingMaterialNumberCount { get; set; }
    public int ReferenceMaterialCount { get; set; }
}

public class ManagementProductFinanceSummary
{
    public decimal TotalValue { get; set; }
    public decimal AssignedValue { get; set; }
    public decimal MiscValue { get; set; }
    public decimal UnassignedValue { get; set; }
    public decimal MissingReferenceValue { get; set; }
    public decimal MissingMaterialValue { get; set; }
    public decimal AssignedValuePercent { get; set; }
    public decimal MiscValuePercent { get; set; }
    public decimal UnassignedValuePercent { get; set; }
    public decimal MissingReferenceValuePercent { get; set; }
    public string DisplayCurrency { get; set; } = string.Empty;
}

public class ManagementProductDivisionFinanceRow
{
    public string ProductDivisionCode { get; set; } = string.Empty;
    public string ProductDivisionText { get; set; } = string.Empty;
    public string ProductFamilyCode { get; set; } = string.Empty;
    public string ProductFamilyText { get; set; } = string.Empty;
    public string ProductHierarchyCode { get; set; } = string.Empty;
    public string ProductHierarchyText { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public decimal NetSalesActual { get; set; }
    public decimal SharePercent { get; set; }
    public int MaterialCount { get; set; }
    public int RowCount { get; set; }
    public string Countries { get; set; } = string.Empty;
}

public class ManagementProductFinanceCountryRow
{
    public string CountryKey { get; set; } = string.Empty;
    public string Tsc { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public decimal TotalValue { get; set; }
    public decimal AssignedValue { get; set; }
    public decimal MiscValue { get; set; }
    public decimal UnassignedValue { get; set; }
    public decimal MissingReferenceValue { get; set; }
    public decimal MissingMaterialValue { get; set; }
    public decimal AssignedValuePercent { get; set; }
}

public class ManagementProductAssignmentCountryRow
{
    public string CountryKey { get; set; } = string.Empty;
    public string Tsc { get; set; } = string.Empty;
    public int DistinctMaterialCount { get; set; }
    public int MatchedMaterialCount { get; set; }
    public int MiscMaterialCount { get; set; }
    public int UnassignedMaterialCount { get; set; }
    public int MissingReferenceMaterialCount { get; set; }
    public int MissingMaterialNumberCount { get; set; }
    public decimal MatchPercent { get; set; }
}

public class ManagementProductAssignmentRow
{
    public string Status { get; set; } = string.Empty;
    public string CountryKey { get; set; } = string.Empty;
    public string Tsc { get; set; } = string.Empty;
    public string SourceSystem { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public string ArticleName { get; set; } = string.Empty;
    public string ReferenceMaterial { get; set; } = string.Empty;
    public string ProductHierarchyCode { get; set; } = string.Empty;
    public string ProductHierarchyText { get; set; } = string.Empty;
    public string ProductFamilyCode { get; set; } = string.Empty;
    public string ProductFamilyText { get; set; } = string.Empty;
    public string ProductDivisionCode { get; set; } = string.Empty;
    public string ProductDivisionText { get; set; } = string.Empty;
    public string ProductMappingAssigned { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public decimal NetSalesActual { get; set; }
    public string Currency { get; set; } = string.Empty;
}

public class ManagementFinanceSummaryResult
{
    public ManagementFinanceSummaryFilter Filter { get; set; } = new();
    public List<string> Notices { get; set; } = [];
    public List<int> YearOptions { get; set; } = [];
    public List<string> CountryOptions { get; set; } = [];
    public List<string> CurrencyOptions { get; set; } = [];
    public List<ManagementFinanceSummaryRow> Rows { get; set; } = [];
    public List<ManagementFinanceSummaryRow> YearRows { get; set; } = [];
    public List<ManagementFinanceSummaryRow> YearCountryRows { get; set; } = [];
    public int IncludedRows { get; set; }
    public int ExcludedRows { get; set; }
    public int CountryCount { get; set; }
    public int CurrencyCount { get; set; }
    public decimal NetSalesActual { get; set; }
    public string DisplayCurrency { get; set; } = string.Empty;
    public List<ManagementFinanceCountryStatusRow> CountryRows { get; set; } = [];
    public List<ManagementFinanceCountryStatusRow> DeviationRows { get; set; } = [];
    public List<ManagementFinanceDataStatusRow> DataStatusRows { get; set; } = [];
    public List<ManagementFinanceCreditCandidateRow> CreditCandidates { get; set; } = [];
    public List<ManagementFinanceDataQualityRow> DataQualityRows { get; set; } = [];
    public ManagementProductFinanceSummary ProductFinanceSummary { get; set; } = new();
    public List<ManagementProductDivisionFinanceRow> ProductDivisionFinanceRows { get; set; } = [];
    public List<ManagementProductFinanceCountryRow> ProductFinanceCountryRows { get; set; } = [];
    public ManagementProductAssignmentSummary ProductAssignmentSummary { get; set; } = new();
    public List<ManagementProductAssignmentCountryRow> ProductAssignmentCountryRows { get; set; } = [];
    public List<ManagementProductAssignmentRow> ProductAssignmentRows { get; set; } = [];
}
