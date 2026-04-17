namespace TrafagSalesExporter.Models;

public class ManagementCockpitFileOption
{
    public string Path { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
}

public class ManagementCockpitSummary
{
    public string Land { get; set; } = string.Empty;
    public string Tsc { get; set; } = string.Empty;
    public DateTime? ExtractionDate { get; set; }
    public int RowCount { get; set; }
    public int InvoiceCount { get; set; }
    public int CustomerCount { get; set; }
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
}

public class ManagementCockpitCentralSummary
{
    public int RowCount { get; set; }
    public int InvoiceCount { get; set; }
    public int SiteCount { get; set; }
    public int CountryCount { get; set; }
    public int CurrencyCount { get; set; }
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
    public int RowCount { get; set; }
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
    public List<ManagementCockpitTimeValueRow> YearlyTotals { get; set; } = [];
    public List<ManagementCockpitTimeValueRow> MonthlyTotals { get; set; } = [];
    public List<ManagementCockpitTimeValueRow> DailyTotals { get; set; } = [];
    public List<ManagementCockpitDimensionValueRow> SourceSystemTotals { get; set; } = [];
    public List<ManagementCockpitDimensionValueRow> CountryTotals { get; set; } = [];
}
