using ClosedXML.Excel;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Sales_All: Spalten „Issue Topic“ und „Mapping“ additiv am Ende der Blaetter Sales (58, 59) und Finance Details (45, 46),
/// Issue Topic als weitere Gruppierung im Blatt Lieferantenzuordnung (letzte Spalte). Bestehende Spalten bleiben, wo sie
/// waren; Kostenwerte aendern sich durch die Spalte nicht (Matrix Ingo 19.11.2026).
/// </summary>
public class CentralExcelIssueTopicTests
{
    private static SalesRecord Record(string tsc, string invoice, string material, string supplierName = "", decimal standardCost = 600m)
        => new()
        {
            ExtractionDate = new DateTime(2026, 11, 19),
            PostingDate = new DateTime(2026, 6, 19),
            InvoiceDate = new DateTime(2026, 6, 19),
            Tsc = tsc,
            Land = tsc == "TRIT" ? "Italien" : "UK",
            InvoiceNumber = invoice,
            PositionOnInvoice = 1,
            Material = material,
            Name = "Pressure transmitter",
            Quantity = 1m,
            CustomerNumber = "C-1",
            CustomerName = "Kunde",
            SupplierName = supplierName,
            SalesPriceValue = 1000m,
            SalesCurrency = "EUR",
            CompanyCurrency = "EUR",
            StandardCost = standardCost,
            StandardCostCurrency = "EUR"
        };

    [Fact]
    public void SalesAll_Shows_Issue_Topic_And_Mapping_At_The_End_Without_Moving_Columns()
    {
        var path = Path.Combine(Path.GetTempPath(), $"trafag-issue-topic-{Guid.NewGuid():N}.xlsx");
        var records = new List<SalesRecord>
        {
            Record("TRUK", "UK-1", "UK-777", "Trafag AG"),                         // 2: nicht gefunden
            Record("TRUK", "UK-2", "UK-888", "Trafag AG"),                         // gemappt, Kosten TR AG
            Record("TRIT", "IT-1", "I53513", "Trafag Controls India Pvt Ltd"),     // I-Praefix, Kosten TR IN
            Record("TRUK", "UK-3", "UK-999", "Trafag AG", standardCost: 0m)        // 7: nicht gefunden, lokal 0
        };
        var costs = new Dictionary<(string, string), GroupStandardCost>
        {
            [("53513", "TRIN")] = new() { MaterialKey = "53513", ValuationArea = "TRIN", UnitCost = 100m, Currency = "EUR" },
            [("44073", "1100")] = new() { MaterialKey = "44073", ValuationArea = "1100", UnitCost = 300m, Currency = "EUR" }
        };
        var mappings = new MaterialMappingLookup(
        [
            new MaterialMapping { Tsc = "TRUK", LocalMaterialKey = "UK-888", GroupMaterialNumber = "44073",
                Method = MaterialMappingMethods.Manual, Status = MaterialMappingStatuses.Confirmed }
        ]);

        try
        {
            ExcelExportService.WriteWorkbook(path, records, includeFinanceHelpSheet: true, FinanceRuleEngine.CreateDefaultRules(),
                groupStandardCosts: costs, chPlantMaterialKeys: new HashSet<string>(["44073", "FILLER"]),
                supplierFallbackMode: SupplierFallbackModes.ChPlantMaster, materialMappings: mappings);

            using var workbook = new XLWorkbook(path);
            var sales = workbook.Worksheet("Sales");
            Assert.Equal("Cost Source", sales.Cell(1, 57).GetString());          // nichts verrutscht
            Assert.Equal("Issue Topic", sales.Cell(1, 58).GetString());
            Assert.Equal("Mapping", sales.Cell(1, 59).GetString());
            IXLRow Sales(string invoice) => sales.RowsUsed().Skip(1).Single(r => r.Cell(4).GetString() == invoice);
            Assert.Equal(("2", ""), (Sales("UK-1").Cell(58).GetString(), Sales("UK-1").Cell(59).GetString()));
            Assert.Equal(("", "44073 (Hand bestaetigt)"), (Sales("UK-2").Cell(58).GetString(), Sales("UK-2").Cell(59).GetString()));
            Assert.Equal("Konzernkosten TR AG (MBEW-STPRS)", Sales("UK-2").Cell(57).GetString());
            Assert.Equal("", Sales("IT-1").Cell(58).GetString());
            Assert.StartsWith("53513 (Regel I-Praefix TRIT", Sales("IT-1").Cell(59).GetString());
            Assert.Equal("7", Sales("UK-3").Cell(58).GetString());
            // Die Quelle bleibt unveraendert: Trafag Sachnummer der Zeile ist leer, auch wenn gemappt.
            Assert.Equal(string.Empty, Sales("UK-2").Cell(54).GetString());

            var details = workbook.Worksheet("Finance Details");
            Assert.Equal("Delivering Entity", details.Cell(4, 44).GetString());
            Assert.Equal("Issue Topic", details.Cell(4, 45).GetString());
            Assert.Equal("Mapping", details.Cell(4, 46).GetString());
            var uk1 = details.RowsUsed().Single(r => r.RowNumber() > 4 && r.Cell(10).GetString() == "UK-1");
            Assert.Equal("2", uk1.Cell(45).GetString());

            var assignment = workbook.Worksheet(ExcelExportService.SupplierAssignmentSheetName);
            for (var i = 0; i < ExcelExportService.SupplierAssignmentSheetHeaders.Length; i++)
                Assert.Equal(ExcelExportService.SupplierAssignmentSheetHeaders[i], assignment.Cell(4, i + 1).GetString());
            Assert.Equal("2026-06", assignment.Cell(4, 9).GetString());          // Monate beginnen weiter in Spalte I
            Assert.Equal(ExcelExportService.SupplierAssignmentIssueTopicHeader, assignment.Cell(4, 10).GetString());
            var ukGroups = assignment.RowsUsed().Where(r => r.RowNumber() > 4 && r.Cell(1).GetString() == "TRUK").ToList();
            Assert.Equal(["", "2", "7"], ukGroups.Select(r => r.Cell(10).GetString()).OrderBy(code => code).ToArray());

            var help = workbook.Worksheet("Finance Filter Hilfe");
            Assert.Contains(help.CellsUsed(), cell => cell.GetString() == "Issue Topic");
            Assert.Contains(help.CellsUsed(), cell => cell.GetString() == "Mapping");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
