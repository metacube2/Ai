using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public class CentralSalesRecordService : ICentralSalesRecordService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public CentralSalesRecordService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task ReplaceForSiteAsync(Site site, IEnumerable<SalesRecord> records)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.CentralSalesRecords.Where(r => r.SiteId == site.Id).ToListAsync();
        if (existing.Count > 0)
            db.CentralSalesRecords.RemoveRange(existing);

        var sourceSystem = string.IsNullOrWhiteSpace(site.SourceSystem) ? "SAP" : site.SourceSystem;
        db.CentralSalesRecords.AddRange(records.Select(record => new CentralSalesRecord
        {
            StoredAtUtc = DateTime.UtcNow,
            SiteId = site.Id,
            SourceSystem = sourceSystem,
            ExtractionDate = record.ExtractionDate,
            Tsc = record.Tsc,
            InvoiceNumber = record.InvoiceNumber,
            PositionOnInvoice = record.PositionOnInvoice,
            Material = record.Material,
            Name = record.Name,
            ProductGroup = record.ProductGroup,
            Quantity = record.Quantity,
            SupplierNumber = record.SupplierNumber,
            SupplierName = record.SupplierName,
            SupplierCountry = record.SupplierCountry,
            CustomerNumber = record.CustomerNumber,
            CustomerName = record.CustomerName,
            CustomerCountry = record.CustomerCountry,
            CustomerIndustry = record.CustomerIndustry,
            StandardCost = record.StandardCost,
            StandardCostCurrency = record.StandardCostCurrency,
            PurchaseOrderNumber = record.PurchaseOrderNumber,
            SalesPriceValue = record.SalesPriceValue,
            SalesCurrency = record.SalesCurrency,
            Incoterms2020 = record.Incoterms2020,
            SalesResponsibleEmployee = record.SalesResponsibleEmployee,
            InvoiceDate = record.InvoiceDate,
            OrderDate = record.OrderDate,
            Land = record.Land,
            DocumentType = record.DocumentType
        }));

        await db.SaveChangesAsync();
    }

    public async Task<List<SalesRecord>> GetAllAsync()
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.CentralSalesRecords
            .OrderBy(r => r.Land)
            .ThenBy(r => r.Tsc)
            .Select(r => new SalesRecord
            {
                ExtractionDate = r.ExtractionDate,
                Tsc = r.Tsc,
                InvoiceNumber = r.InvoiceNumber,
                PositionOnInvoice = r.PositionOnInvoice,
                Material = r.Material,
                Name = r.Name,
                ProductGroup = r.ProductGroup,
                Quantity = r.Quantity,
                SupplierNumber = r.SupplierNumber,
                SupplierName = r.SupplierName,
                SupplierCountry = r.SupplierCountry,
                CustomerNumber = r.CustomerNumber,
                CustomerName = r.CustomerName,
                CustomerCountry = r.CustomerCountry,
                CustomerIndustry = r.CustomerIndustry,
                StandardCost = r.StandardCost,
                StandardCostCurrency = r.StandardCostCurrency,
                PurchaseOrderNumber = r.PurchaseOrderNumber,
                SalesPriceValue = r.SalesPriceValue,
                SalesCurrency = r.SalesCurrency,
                Incoterms2020 = r.Incoterms2020,
                SalesResponsibleEmployee = r.SalesResponsibleEmployee,
                InvoiceDate = r.InvoiceDate,
                OrderDate = r.OrderDate,
                Land = r.Land,
                DocumentType = r.DocumentType
            })
            .ToListAsync();
    }
}
