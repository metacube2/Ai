using ClosedXML.Excel;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class ExcelExportServiceTests
{
    [Fact]
    public void CreateDashboardProofExcelFile_Creates_Formula_Based_Proof_Workbook()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"trafag-proof-{Guid.NewGuid():N}");
        var service = new ExcelExportService();
        var records = new List<SalesRecord>
        {
            new()
            {
                ExtractionDate = new DateTime(2026, 6, 17),
                PostingDate = new DateTime(2025, 12, 31),
                Tsc = "TRCH",
                Land = "CH",
                InvoiceNumber = "INV-1",
                PositionOnInvoice = 1,
                Material = "000000000000000123",
                Name = "Pressure transmitter",
                Quantity = 2m,
                SupplierName = "External Supplier",
                SupplierCountry = "DE",
                CustomerName = "Customer AG",
                CustomerCountry = "CH",
                SalesPriceValue = 100m,
                SalesCurrency = "CHF",
                CompanyCurrency = "CHF",
                StandardCost = 10m,
                StandardCostCurrency = "CHF",
                ProductHierarchyCode = "H1",
                ProductHierarchyText = "Hierarchy",
                ProductFamilyCode = "F1",
                ProductFamilyText = "Family",
                ProductDivisionCode = "0005",
                ProductDivisionText = "Transmitters",
                ProductMappingAssigned = "X"
            }
        };

        try
        {
            var path = service.CreateDashboardProofExcelFile(outputDirectory, new DateTime(2026, 6, 17), records, useAuditCsvAsCentralSource: false);

            Assert.Equal(Path.Combine(outputDirectory, "Finance_Dashboard_Nachweis_2026-06-17.xlsx"), path);
            Assert.True(File.Exists(path));

            using var workbook = new XLWorkbook(path);
            Assert.Contains("Datenherkunft", workbook.Worksheets.Select(sheet => sheet.Name));
            Assert.Contains("Finance Summary", workbook.Worksheets.Select(sheet => sheet.Name));
            Assert.Contains("Finance Details", workbook.Worksheets.Select(sheet => sheet.Name));
            Assert.Contains("Soll Ist", workbook.Worksheets.Select(sheet => sheet.Name));
            Assert.Contains("Sparten Summary", workbook.Worksheets.Select(sheet => sheet.Name));
            Assert.Contains("Sparten Details", workbook.Worksheets.Select(sheet => sheet.Name));
            Assert.Contains("Gruppenmarge Summary", workbook.Worksheets.Select(sheet => sheet.Name));
            Assert.Contains("Gruppenmarge Details", workbook.Worksheets.Select(sheet => sheet.Name));
            Assert.Contains("Datenqualitaet", workbook.Worksheets.Select(sheet => sheet.Name));
            Assert.Contains("Formel Hilfe", workbook.Worksheets.Select(sheet => sheet.Name));

            var financeDetails = workbook.Worksheet("Finance Details");
            Assert.Equal(2025, financeDetails.Cell(2, 1).GetValue<int>());
            Assert.Equal("CH", financeDetails.Cell(2, 2).GetString());
            Assert.Equal("TRUE", financeDetails.Cell(2, 5).GetString());
            Assert.Equal(100m, financeDetails.Cell(2, 6).GetValue<decimal>());

            var financeSummary = workbook.Worksheet("Finance Summary");
            Assert.StartsWith("COUNTIFS('Finance Details'!$A:$A,A2", financeSummary.Cell(2, 4).FormulaA1);
            Assert.Contains("SUMIFS('Finance Details'!$F:$F", financeSummary.Cell(2, 5).FormulaA1);

            var divisionSummary = workbook.Worksheet("Sparten Summary");
            Assert.Contains("SUMIFS('Sparten Details'!$P:$P", divisionSummary.Cell(2, 7).FormulaA1);

            var groupMarginDetails = workbook.Worksheet("Gruppenmarge Details");
            Assert.Equal("OK", groupMarginDetails.Cell(2, 2).GetString());
            Assert.Equal("IF(B2=\"OK\",Q2-R2,\"\")", groupMarginDetails.Cell(2, 19).FormulaA1);

            var groupMarginSummary = workbook.Worksheet("Gruppenmarge Summary");
            Assert.Contains("COUNTIFS('Gruppenmarge Details'!$A:$A,A2", groupMarginSummary.Cell(2, 7).FormulaA1);
            Assert.Equal("IF(G2>0,\"\",E2-F2)", groupMarginSummary.Cell(2, 10).FormulaA1);

            var dataQuality = workbook.Worksheet("Datenqualitaet");
            Assert.Equal("COUNTA('Finance Details'!$A:$A)-1", dataQuality.Cell(2, 2).FormulaA1);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, recursive: true);
        }
    }

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
                .Where(row => row.Cell(43).GetValue<int>() == 2025)
                .Where(row => row.Cell(44).GetString() == "DE")
                .Where(row => row.Cell(48).GetString() == "TRUE")
                .ToList();

            Assert.Equal(2, includedGermanyRows.Count);
            Assert.Equal(80m, includedGermanyRows.Sum(row => row.Cell(46).GetValue<decimal>()));

            var details = workbook.Worksheet("Finance Details");
            var includedGermanyDetailRows = details.RowsUsed()
                .Where(row => row.RowNumber() > 4)
                .Where(row => row.Cell(1).GetValue<int>() == 2025)
                .Where(row => row.Cell(2).GetString() == "DE")
                .ToList();

            Assert.Equal(2, includedGermanyDetailRows.Count);
            Assert.Equal(80m, includedGermanyDetailRows.Sum(row => row.Cell(5).GetValue<decimal>()));
            Assert.All(includedGermanyDetailRows, row => Assert.Equal("Sales Price/Value", row.Cell(6).GetString()));
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
