namespace TrafagSalesExporter.Models;

public class SalesRecord
{
    public DateTime ExtractionDate { get; set; }
    public string SourceSystem { get; set; } = string.Empty;
    public string Tsc { get; set; } = string.Empty;
    public string SourceLineId { get; set; } = string.Empty;
    public int DocumentEntry { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int PositionOnInvoice { get; set; }
    public string Material { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ProductGroup { get; set; } = string.Empty;
    public string ProductHierarchyCode { get; set; } = string.Empty;
    public string ProductHierarchyText { get; set; } = string.Empty;
    public string ProductFamilyCode { get; set; } = string.Empty;
    public string ProductFamilyText { get; set; } = string.Empty;
    public string ProductDivisionCode { get; set; } = string.Empty;
    public string ProductDivisionText { get; set; } = string.Empty;
    public string ProductMappingAssigned { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string SupplierNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string SupplierCountry { get; set; } = string.Empty;
    // Verrechnungspreisliche Rolle des Standorts fuer diesen Artikel, Rohwert aus der Quelle
    // (Indiens SAP B1: OITM."U_Tasc_ST" - FFM/LRD/CM). Bewusst der Rohwert und nicht die
    // Auslegung, damit im Audit-CSV nachvollziehbar bleibt, woher eine Klassifikation kommt.
    // Siehe docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md.
    public string SalesType { get; set; } = string.Empty;
    // Trafag-Sachnummer, falls die Quelle neben ihrer eigenen Artikelnummer auch die
    // Konzernnummer fuehrt (Indien: OITM."U_TASC_OMN"). Schluessel fuer die
    // Konzern-Standardkosten; ohne sie findet ein Standort mit eigener Nummerierung die
    // Konzernkosten nicht.
    public string GroupMaterialNumber { get; set; } = string.Empty;
    public string CustomerNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerCountry { get; set; } = string.Empty;
    public string CustomerIndustry { get; set; } = string.Empty;
    public decimal StandardCost { get; set; }
    public string StandardCostCurrency { get; set; } = string.Empty;
    // Fix/variabel-Split des Standardpreises (Stueckpreis, gleiche Waehrung wie StandardCost).
    // Null = Quelle liefert keinen Split; nur dann bleibt der Deckungsbeitrag offen.
    public decimal? StandardCostVariable { get; set; }
    public decimal? StandardCostFixed { get; set; }
    public string PurchaseOrderNumber { get; set; } = string.Empty;
    public decimal SalesPriceValue { get; set; }
    public string SalesCurrency { get; set; } = string.Empty;
    public string DocumentCurrency { get; set; } = string.Empty;
    public decimal DocumentTotalForeignCurrency { get; set; }
    public decimal DocumentTotalLocalCurrency { get; set; }
    public decimal VatSumForeignCurrency { get; set; }
    public decimal VatSumLocalCurrency { get; set; }
    public decimal DocumentRate { get; set; }
    public string CompanyCurrency { get; set; } = string.Empty;
    public string Incoterms2020 { get; set; } = string.Empty;
    public string SalesResponsibleEmployee { get; set; } = string.Empty;
    public DateTime? PostingDate { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public DateTime? OrderDate { get; set; }
    public string Land { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
}
