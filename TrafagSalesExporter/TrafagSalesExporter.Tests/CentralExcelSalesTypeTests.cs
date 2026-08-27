using ClosedXML.Excel;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// ISS-013: `Sales Type` und `Trafag Sachnummer` sind die beiden Rohfelder aus dem
/// Artikelstamm der Quelle (Indien: `OITM."U_Tasc_ST"` und `OITM."U_TASC_OMN"`), die die
/// Gruppenmarge steuern. Sie standen bis zum 2026-08-27 in keinem Blatt, obwohl die
/// Klassifikation an ihnen haengt: wer nur ins Sales_All schaute, konnte nicht sehen, WARUM
/// eine indische Zeile so gerechnet wird. Das Issue-Log fuehrte das als
/// „Fehleinschaetzung Indiens beim Lesen in Excel".
///
/// Geprueft wird beides, weil der Kopf allein nichts beweist: die Spalten stehen am ENDE
/// (bestehende Formeln zeigen auf feste Spaltenpositionen), und sie tragen den Rohwert.
/// </summary>
public class CentralExcelSalesTypeTests
{
    private const int SalesTypeColumn = 53;
    private const int GroupMaterialNumberColumn = 54;

    private static SalesRecord Record(string salesType, string groupMaterialNumber)
        => new()
        {
            ExtractionDate = new DateTime(2026, 8, 27),
            PostingDate = new DateTime(2025, 7, 30),
            InvoiceDate = new DateTime(2025, 7, 29),
            Tsc = "TRIN",
            Land = "Indien",
            InvoiceNumber = "IN-4711",
            PositionOnInvoice = 1,
            Material = "LOCAL-1",
            Name = "Pressure transmitter",
            Quantity = 1m,
            CustomerNumber = "C-1",
            CustomerName = "Kunde",
            CustomerCountry = "IN",
            SalesPriceValue = 1000m,
            SalesCurrency = "INR",
            CompanyCurrency = "INR",
            StandardCost = 600m,
            StandardCostCurrency = "INR",
            SalesType = salesType,
            GroupMaterialNumber = groupMaterialNumber
        };

    [Fact]
    public void ConsolidatedExcel_Writes_SalesType_And_GroupMaterialNumber_AtTheEnd()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"trafag-salestype-{Guid.NewGuid():N}");
        var service = new ExcelExportService();

        try
        {
            var path = service.CreateConsolidatedExcelFile(
                outputDirectory, new DateTime(2026, 8, 27), [Record("LRD", "52535")]);

            using var workbook = new XLWorkbook(path);
            var sales = workbook.Worksheet("Sales");

            Assert.Equal("Sales Type", sales.Cell(1, SalesTypeColumn).GetString());
            Assert.Equal("Trafag Sachnummer", sales.Cell(1, GroupMaterialNumberColumn).GetString());
            Assert.Equal("LRD", sales.Cell(2, SalesTypeColumn).GetString());
            Assert.Equal("52535", sales.Cell(2, GroupMaterialNumberColumn).GetString());

            // Die Spalte davor bleibt, wo sie war; nichts ist verrutscht.
            Assert.Equal("Line Registration Date", sales.Cell(1, SalesTypeColumn - 1).GetString());
        }
        finally
        {
            if (Directory.Exists(outputDirectory)) Directory.Delete(outputDirectory, recursive: true);
        }
    }

    [Fact]
    public void ConsolidatedExcel_LeavesBothColumnsEmpty_WhenTheSourceDoesNotDeliverThem()
    {
        // Italien fuehrt den Sales Type bei allen Zeilen leer (gemessen 2026-08-27). Leer darf
        // nicht als Aussage gelesen werden, deshalb bleibt die Zelle leer statt einer Deutung.
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"trafag-salestype-{Guid.NewGuid():N}");
        var service = new ExcelExportService();

        try
        {
            var path = service.CreateConsolidatedExcelFile(
                outputDirectory, new DateTime(2026, 8, 27), [Record(string.Empty, string.Empty)]);

            using var workbook = new XLWorkbook(path);
            var sales = workbook.Worksheet("Sales");

            Assert.Equal(string.Empty, sales.Cell(2, SalesTypeColumn).GetString());
            Assert.Equal(string.Empty, sales.Cell(2, GroupMaterialNumberColumn).GetString());
        }
        finally
        {
            if (Directory.Exists(outputDirectory)) Directory.Delete(outputDirectory, recursive: true);
        }
    }

    [Fact]
    public void ProofSheet_GruppenmargeDetails_Shows_SalesType_And_GroupMaterialNumber()
    {
        // Im Nachweisblatt stehen sie hinter den DB-Spalten, damit die Formeln in Spalte S und T
        // (Margin Value, Margin %) auf ihren Spaltenbuchstaben bleiben.
        const int detailSalesTypeColumn = 27;
        const int detailGroupMaterialColumn = 28;

        var outputDirectory = Path.Combine(Path.GetTempPath(), $"trafag-salestype-proof-{Guid.NewGuid():N}");
        var service = new ExcelExportService();

        try
        {
            var path = service.CreateDashboardProofExcelFile(
                outputDirectory, new DateTime(2026, 8, 27), [Record("LRD", "52535")], useAuditCsvAsCentralSource: false);

            using var workbook = new XLWorkbook(path);
            var details = workbook.Worksheet("Gruppenmarge Details");

            Assert.Equal("Sales Type", details.Cell(1, detailSalesTypeColumn).GetString());
            Assert.Equal("Trafag Sachnummer", details.Cell(1, detailGroupMaterialColumn).GetString());
            Assert.Equal("LRD", details.Cell(2, detailSalesTypeColumn).GetString());
            Assert.Equal("52535", details.Cell(2, detailGroupMaterialColumn).GetString());
        }
        finally
        {
            if (Directory.Exists(outputDirectory)) Directory.Delete(outputDirectory, recursive: true);
        }
    }
}
