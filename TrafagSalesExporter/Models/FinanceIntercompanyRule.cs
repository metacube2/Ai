namespace TrafagSalesExporter.Models;

public class FinanceIntercompanyRule
{
    public int Id { get; set; }
    public string ScopeKey { get; set; } = string.Empty;
    public string CustomerNumber { get; set; } = string.Empty;
    public string CustomerNameContains { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
