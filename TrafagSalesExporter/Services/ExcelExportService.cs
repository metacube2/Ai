using ClosedXML.Excel;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public class ExcelExportService
{
    public string CreateFile(string baseDirectory, string land, string tsc, List<SalesRecord> records)
    {
        var outputDirectory = Path.Combine(baseDirectory, "exports", land);
        Directory.CreateDirectory(outputDirectory);

        var fileName = $"Sales_{tsc}_{DateTime.UtcNow:yyyy-MM-dd}.xlsx";
        var filePath = Path.Combine(outputDirectory, fileName);

        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("Sales");

        var headers = new[]
        {
            "extraction date",
            "TSC",
            "Invoice Number",
            "Position on invoice",
            "Material",
            "Name",
            "Product Group",
            "Quantity",
            "Supplier number",
            "Supplier name",
            "Supplier country",
            "Customer number",
            "Customer name",
            "Customer country",
            "Customer Industry",
            "Standard cost",
            "Standard Cost Currency",
            "Purchase Order number",
            "Sales Price/Value",
            "Sales Currency",
            "Incoterms 2020",
            "Sales responsible employee",
            "invoice date",
            "order date",
            "Land",
            "Document Type"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
        }

        var row = 2;
        foreach (var r in records)
        {
            ws.Cell(row, 1).Value = r.ExtractionDate.ToString("dd.MM.yyyy HH:mm:ss");
            ws.Cell(row, 2).Value = r.TSC;
            ws.Cell(row, 3).Value = r.InvoiceNumber;
            ws.Cell(row, 4).Value = r.PositionOnInvoice;
            ws.Cell(row, 5).Value = r.Material;
            ws.Cell(row, 6).Value = r.Name;
            ws.Cell(row, 7).Value = r.ProductGroup;
            ws.Cell(row, 8).Value = r.Quantity;
            ws.Cell(row, 9).Value = r.SupplierNumber;
            ws.Cell(row, 10).Value = r.SupplierName;
            ws.Cell(row, 11).Value = r.SupplierCountry;
            ws.Cell(row, 12).Value = r.CustomerNumber;
            ws.Cell(row, 13).Value = r.CustomerName;
            ws.Cell(row, 14).Value = r.CustomerCountry;
            ws.Cell(row, 15).Value = r.CustomerIndustry;
            ws.Cell(row, 16).Value = r.StandardCost;
            ws.Cell(row, 17).Value = r.StandardCostCurrency;
            ws.Cell(row, 18).Value = r.PurchaseOrderNumber;
            ws.Cell(row, 19).Value = r.SalesPriceValue;
            ws.Cell(row, 20).Value = r.SalesCurrency;
            ws.Cell(row, 21).Value = r.Incoterms2020;
            ws.Cell(row, 22).Value = r.SalesResponsibleEmployee;
            ws.Cell(row, 23).Value = r.InvoiceDate?.ToString("dd.MM.yyyy") ?? string.Empty;
            ws.Cell(row, 24).Value = r.OrderDate?.ToString("dd.MM.yyyy") ?? string.Empty;
            ws.Cell(row, 25).Value = r.Land;
            ws.Cell(row, 26).Value = r.DocumentType;
            row++;
        }

        ws.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
        return filePath;
    }
}
