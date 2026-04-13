using ClosedXML.Excel;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public class ExcelExportService : IExcelExportService
{
    public string CreateExcelFile(string outputDirectory, string tsc, DateTime fileDate, List<SalesRecord> records)
    {
        Directory.CreateDirectory(outputDirectory);
        var fileName = $"Sales_{tsc}_{fileDate:yyyy-MM-dd}.xlsx";
        var fullPath = Path.Combine(outputDirectory, fileName);
        WriteWorkbook(fullPath, records);
        return fullPath;
    }

    public string CreateConsolidatedExcelFile(string outputDirectory, DateTime fileDate, List<SalesRecord> records)
    {
        Directory.CreateDirectory(outputDirectory);
        var fileName = $"Sales_All_{fileDate:yyyy-MM-dd}.xlsx";
        var fullPath = Path.Combine(outputDirectory, fileName);
        WriteWorkbook(fullPath, records);
        return fullPath;
    }

    private static void WriteWorkbook(string fullPath, List<SalesRecord> records)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Sales");

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
        foreach (var record in records)
        {
            ws.Cell(row, 1).Value = record.ExtractionDate.ToString("dd.MM.yyyy HH:mm:ss");
            ws.Cell(row, 2).Value = record.Tsc;
            ws.Cell(row, 3).Value = record.InvoiceNumber;
            ws.Cell(row, 4).Value = record.PositionOnInvoice;
            ws.Cell(row, 5).Value = record.Material;
            ws.Cell(row, 6).Value = record.Name;
            ws.Cell(row, 7).Value = record.ProductGroup;
            ws.Cell(row, 8).Value = record.Quantity;
            ws.Cell(row, 9).Value = record.SupplierNumber;
            ws.Cell(row, 10).Value = record.SupplierName;
            ws.Cell(row, 11).Value = record.SupplierCountry;
            ws.Cell(row, 12).Value = record.CustomerNumber;
            ws.Cell(row, 13).Value = record.CustomerName;
            ws.Cell(row, 14).Value = record.CustomerCountry;
            ws.Cell(row, 15).Value = record.CustomerIndustry;
            ws.Cell(row, 16).Value = record.StandardCost;
            ws.Cell(row, 17).Value = record.StandardCostCurrency;
            ws.Cell(row, 18).Value = record.PurchaseOrderNumber;
            ws.Cell(row, 19).Value = record.SalesPriceValue;
            ws.Cell(row, 20).Value = record.SalesCurrency;
            ws.Cell(row, 21).Value = record.Incoterms2020;
            ws.Cell(row, 22).Value = record.SalesResponsibleEmployee;
            ws.Cell(row, 23).Value = record.InvoiceDate?.ToString("dd.MM.yyyy") ?? string.Empty;
            ws.Cell(row, 24).Value = record.OrderDate?.ToString("dd.MM.yyyy") ?? string.Empty;
            ws.Cell(row, 25).Value = record.Land;
            ws.Cell(row, 26).Value = record.DocumentType;
            row++;
        }

        ws.Columns().AdjustToContents();
        workbook.SaveAs(fullPath);
    }
}
