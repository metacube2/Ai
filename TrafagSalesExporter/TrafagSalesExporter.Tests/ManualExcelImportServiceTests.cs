using ClosedXML.Excel;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class ManualExcelImportServiceTests
{
    [Fact]
    public async Task ReadSalesRecordsAsync_Reads_Expected_Columns_From_Exporter_Format()
    {
        var site = new Site
        {
            TSC = "TRCH",
            Land = "Schweiz"
        };
        var filePath = CreateWorkbook(workbook =>
        {
            var ws = workbook.Worksheets.Add("Sales");
            WriteHeaders(ws);
            ws.Cell(2, 1).Value = "15.04.2026 13:45:00";
            ws.Cell(2, 2).Value = "TRDE";
            ws.Cell(2, 3).Value = "INV-100";
            ws.Cell(2, 4).Value = 7;
            ws.Cell(2, 5).Value = "MAT-1";
            ws.Cell(2, 6).Value = "Pressure Sensor";
            ws.Cell(2, 7).Value = "PG-A";
            ws.Cell(2, 8).Value = 2.5m;
            ws.Cell(2, 9).Value = "SUP-1";
            ws.Cell(2, 10).Value = "Supplier";
            ws.Cell(2, 11).Value = "DE";
            ws.Cell(2, 12).Value = "CUST-1";
            ws.Cell(2, 13).Value = "Customer";
            ws.Cell(2, 14).Value = "CH";
            ws.Cell(2, 15).Value = "Industry";
            ws.Cell(2, 16).Value = 10.25m;
            ws.Cell(2, 17).Value = "EUR";
            ws.Cell(2, 18).Value = "PO-1";
            ws.Cell(2, 19).Value = 21.40m;
            ws.Cell(2, 20).Value = "EUR";
            ws.Cell(2, 21).Value = "DAP";
            ws.Cell(2, 22).Value = "Alice";
            ws.Cell(2, 23).Value = "14.04.2026";
            ws.Cell(2, 24).Value = "10.04.2026";
            ws.Cell(2, 25).Value = "Deutschland";
            ws.Cell(2, 26).Value = "Invoice";
        });

        try
        {
            var service = new ManualExcelImportService();

            var rows = await service.ReadSalesRecordsAsync(filePath, site);

            var row = Assert.Single(rows);
            Assert.Equal("TRDE", row.Tsc);
            Assert.Equal("INV-100", row.InvoiceNumber);
            Assert.Equal(7, row.PositionOnInvoice);
            Assert.Equal("MAT-1", row.Material);
            Assert.Equal(2.5m, row.Quantity);
            Assert.Equal(10.25m, row.StandardCost);
            Assert.Equal(21.40m, row.SalesPriceValue);
            Assert.Equal("Deutschland", row.Land);
            Assert.Equal("Invoice", row.DocumentType);
            Assert.Equal(new DateTime(2026, 4, 14), row.InvoiceDate);
            Assert.Equal(new DateTime(2026, 4, 10), row.OrderDate);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ReadSalesRecordsAsync_Uses_Site_Fallbacks_When_Tsc_And_Land_Are_Missing()
    {
        var site = new Site
        {
            TSC = "TRCH",
            Land = "Schweiz"
        };
        var filePath = CreateWorkbook(workbook =>
        {
            var ws = workbook.Worksheets.Add("Sales");
            WriteHeaders(ws);
            ws.Cell(2, 3).Value = "INV-200";
            ws.Cell(2, 5).Value = "MAT-2";
        });

        try
        {
            var service = new ManualExcelImportService();

            var rows = await service.ReadSalesRecordsAsync(filePath, site);

            var row = Assert.Single(rows);
            Assert.Equal("TRCH", row.Tsc);
            Assert.Equal("Schweiz", row.Land);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ReadSalesRecordsAsync_Parses_German_Decimal_Text_And_Skips_Empty_Rows()
    {
        var site = new Site
        {
            TSC = "TRAT",
            Land = "Oesterreich"
        };
        var filePath = CreateWorkbook(workbook =>
        {
            var ws = workbook.Worksheets.Add("Sales");
            WriteHeaders(ws);
            ws.Cell(2, 3).Value = "INV-300";
            ws.Cell(2, 8).Value = "1,50";
            ws.Cell(2, 16).Value = "3,25";
            ws.Cell(2, 19).Value = "7,90";
            ws.Cell(3, 1).Value = "";
            ws.Cell(3, 2).Value = "";
            ws.Cell(3, 3).Value = "";
        });

        try
        {
            var service = new ManualExcelImportService();

            var rows = await service.ReadSalesRecordsAsync(filePath, site);

            var row = Assert.Single(rows);
            Assert.Equal(1.50m, row.Quantity);
            Assert.Equal(3.25m, row.StandardCost);
            Assert.Equal(7.90m, row.SalesPriceValue);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ReadSalesRecordsAsync_Throws_When_InvoiceNumber_Header_Is_Missing()
    {
        var site = new Site
        {
            TSC = "TRCH",
            Land = "Schweiz"
        };
        var filePath = CreateWorkbook(workbook =>
        {
            var ws = workbook.Worksheets.Add("Sales");
            ws.Cell(1, 1).Value = "TSC";
            ws.Cell(1, 2).Value = "Material";
            ws.Cell(2, 1).Value = "TRCH";
            ws.Cell(2, 2).Value = "MAT-3";
        });

        try
        {
            var service = new ManualExcelImportService();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReadSalesRecordsAsync(filePath, site));

            Assert.Contains("Invoice Number", ex.Message);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static string CreateWorkbook(Action<XLWorkbook> fillWorkbook)
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xlsx");
        using var workbook = new XLWorkbook();
        fillWorkbook(workbook);
        workbook.SaveAs(filePath);
        return filePath;
    }

    private static void WriteHeaders(IXLWorksheet ws)
    {
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
        }
    }
}
