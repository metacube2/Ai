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
