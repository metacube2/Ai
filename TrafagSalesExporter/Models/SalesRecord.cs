namespace TrafagSalesExporter.Models;

public class SalesRecord
{
    public DateTime ExtractionDate { get; set; }
    public string TSC { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public int PositionOnInvoice { get; set; }
    public string Material { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ProductGroup { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string SupplierNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string SupplierCountry { get; set; } = string.Empty;
    public string CustomerNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerCountry { get; set; } = string.Empty;
    public string CustomerIndustry { get; set; } = string.Empty;
    public decimal StandardCost { get; set; }
    public string StandardCostCurrency { get; set; } = string.Empty;
    public string PurchaseOrderNumber { get; set; } = string.Empty;
    public decimal SalesPriceValue { get; set; }
    public string SalesCurrency { get; set; } = string.Empty;
    public string Incoterms2020 { get; set; } = string.Empty;
    public string SalesResponsibleEmployee { get; set; } = string.Empty;
    public DateTime? InvoiceDate { get; set; }
    public DateTime? OrderDate { get; set; }
    public string Land { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
}
