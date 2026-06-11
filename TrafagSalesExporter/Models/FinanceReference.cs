namespace TrafagSalesExporter.Models;

public class FinanceReference
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Year { get; set; } = 2025;
    public decimal? LocalCurrencyValue { get; set; }
    public decimal? CheckValue { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
