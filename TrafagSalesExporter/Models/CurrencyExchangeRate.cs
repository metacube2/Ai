namespace TrafagSalesExporter.Models;

public class CurrencyExchangeRate
{
    public int Id { get; set; }
    public string FromCurrency { get; set; } = string.Empty;
    public string ToCurrency { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public DateTime ValidFrom { get; set; } = DateTime.UtcNow.Date;
    public DateTime? ValidTo { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
