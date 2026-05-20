using ClosedXML.Excel;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class ExcelExportServiceTests
{
    [Fact]
    public void CreateConsolidatedExcelFile_Uses_Germany_Finance_Response_Rules()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"trafag-export-{Guid.NewGuid():N}");
        var service = new ExcelExportService();
        var records = new List<SalesRecord>
        {
            CreateGermanyRecord("Normal GmbH", "Deutschland", "RE250001", 100m),
            CreateGermanyRecord("Trafag AG", "Schweiz", "RE250002", 40m),
            CreateGermanyRecord("Magnetic Sense GmbH", "Deutschland", "RE250003", 30m),
            CreateGermanyRecord("Normal GmbH", "Deutschland", "GS2510096", 20m),
            CreateGermanyRecord("Normal GmbH", "Deutschland", "GS2510095", 10m)
        };

        try
        {
            var path = service.CreateConsolidatedExcelFile(outputDirectory, new DateTime(2026, 5, 20), records);

            using var workbook = new XLWorkbook(path);
            var summary = workbook.Worksheet("Finance Summary");
            var deSummaryRow = summary.RowsUsed()
                .Where(row => row.RowNumber() > 4)
                .Single(row => row.Cell(1).GetValue<int>() == 2025 && row.Cell(2).GetString() == "DE");

            Assert.Equal(2, deSummaryRow.Cell(4).GetValue<int>());
            Assert.Equal(80m, deSummaryRow.Cell(5).GetValue<decimal>());
            Assert.Equal(3, deSummaryRow.Cell(6).GetValue<int>());

            var sales = workbook.Worksheet("Sales");
            var includedGermanyRows = sales.RowsUsed()
                .Skip(1)
                .Where(row => row.Cell(36).GetValue<int>() == 2025)
                .Where(row => row.Cell(37).GetString() == "DE")
                .Where(row => row.Cell(41).GetString() == "TRUE")
                .ToList();

            Assert.Equal(2, includedGermanyRows.Count);
            Assert.Equal(80m, includedGermanyRows.Sum(row => row.Cell(39).GetValue<decimal>()));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, recursive: true);
        }
    }

    private static SalesRecord CreateGermanyRecord(string customerName, string customerCountry, string invoiceNumber, decimal value)
        => new()
        {
            ExtractionDate = new DateTime(2026, 5, 20),
            Tsc = "TRDE",
            Land = "Deutschland",
            InvoiceNumber = invoiceNumber,
            PositionOnInvoice = 1,
            CustomerName = customerName,
            CustomerCountry = customerCountry,
            SalesPriceValue = value,
            SalesCurrency = "EUR",
            CompanyCurrency = "EUR",
            DocumentType = "Alphaplan Excel"
        };
}
