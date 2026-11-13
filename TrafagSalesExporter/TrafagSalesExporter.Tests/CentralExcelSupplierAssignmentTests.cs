using ClosedXML.Excel;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Mail Andreas Stoller 2026-11-13: Die Lieferantenfelder sind bei vielen Gesellschaften leer (CH, AT, DE, ES ganz), die
/// Gruppenmarge ordnet trotzdem zu. Sales_All zeigt deshalb die WIRKSAME Zuordnung: „Supplier Type“ und „Delivering
/// Entity“ im Blatt Sales (Spalten 55 und 56, dazu 57 Cost Source) und im Blatt Finance Details (Spalten 43 und 44),
/// additiv am Ende. Die Standortdateien bleiben unveraendert.
/// </summary>
public class CentralExcelSupplierAssignmentTests
{
    private const int SalesSupplierTypeColumn = 55;
    private const int SalesDeliveringEntityColumn = 56;
    private const int SalesCostSourceColumn = 57;
    private const int DetailsSupplierTypeColumn = 43;
    private const int DetailsDeliveringEntityColumn = 44;

    private static SalesRecord Record(string tsc, string land, string invoice, string supplierName = "", string salesType = "",
        decimal salesValue = 1000m, string supplierNumber = "")
        => new()
        {
            ExtractionDate = new DateTime(2026, 11, 13),
            PostingDate = new DateTime(2026, 6, 19),
            InvoiceDate = new DateTime(2026, 6, 19),
            Tsc = tsc,
            Land = land,
            InvoiceNumber = invoice,
            PositionOnInvoice = 1,
            Material = "44073",
            Name = "Pressure transmitter",
            Quantity = 1m,
            CustomerNumber = "C-1",
            CustomerName = "Kunde",
            CustomerCountry = "XX",
            SupplierNumber = supplierNumber,
            SupplierName = supplierName,
            SalesPriceValue = salesValue,
            SalesCurrency = "CHF",
            CompanyCurrency = "CHF",
            StandardCost = 600m,
            StandardCostCurrency = "CHF",
            SalesType = salesType
        };

    private static (string SupplierType, string Entity, string CostSource) SalesRow(IXLWorksheet sales, string invoice)
    {
        var row = sales.RowsUsed().Skip(1).Single(r => r.Cell(4).GetString() == invoice);
        return (row.Cell(SalesSupplierTypeColumn).GetString(), row.Cell(SalesDeliveringEntityColumn).GetString(),
            row.Cell(SalesCostSourceColumn).GetString());
    }

    [Fact]
    public void SalesAll_Shows_Effective_Supplier_Assignment_In_Sales_And_Finance_Details()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"trafag-supplier-assignment-{Guid.NewGuid():N}");
        var service = new ExcelExportService();
        var records = new List<SalesRecord>
        {
            // CH/AT: die Faktura kennt keinen Vorlieferanten, die Zeile gilt trotzdem als Trafag AG.
            Record("TRCH", "Schweiz", "CH-1"),
            Record("TRAT", "Oesterreich", "AT-1"),
            // Indien, eigene Fertigung: der Sales Type entscheidet, kein Lieferant noetig.
            Record("TRIN", "Indien", "IN-1", salesType: "FFM"),
            // UK-Fall aus der Mail: Nummer TR09 (sonst Trafag Controls India), Name Trafag AG. Es zaehlt der Name.
            Record("TRUK", "UK", "UK-1", supplierName: "Trafag AG", supplierNumber: "TR09"),
            Record("TRUK", "UK", "UK-2", supplierName: "Trafag Controls India Pvt Limited", supplierNumber: "TR09"),
            // Italien: Trafag Italia als Lieferant ist ein gueltiger interner Lieferant.
            Record("TRIT", "Italien", "IT-1", supplierName: "Trafag Italia S.r.l."),
            Record("TRIT", "Italien", "IT-2", supplierName: "STS ITALIA S.R.L."),
            // Konzernlieferant ohne eigene Konzernkostentabelle (ISS-018.7): Intern, aber keine liefernde Gesellschaft.
            Record("TRIT", "Italien", "IT-3", supplierName: "GFS Gesellschaft fuer Sensorik"),
            // Von den Finance-Regeln ausgeschlossen (Umsatz 0): keine Gruppenmargen-Zeile, Spalten bleiben leer.
            Record("TRCH", "Schweiz", "CH-0", salesValue: 0m)
        };

        try
        {
            var path = service.CreateConsolidatedExcelFile(outputDirectory, new DateTime(2026, 11, 13), records);

            using var workbook = new XLWorkbook(path);
            var sales = workbook.Worksheet("Sales");
            Assert.Equal("Trafag Sachnummer", sales.Cell(1, SalesSupplierTypeColumn - 1).GetString());   // nichts verrutscht
            Assert.Equal("Supplier Type", sales.Cell(1, SalesSupplierTypeColumn).GetString());
            Assert.Equal("Delivering Entity", sales.Cell(1, SalesDeliveringEntityColumn).GetString());
            Assert.Equal("Cost Source", sales.Cell(1, SalesCostSourceColumn).GetString());

            Assert.Equal(("Intern", "TR AG"), (SalesRow(sales, "CH-1").SupplierType, SalesRow(sales, "CH-1").Entity));
            Assert.Equal(("Intern", "TR AG"), (SalesRow(sales, "AT-1").SupplierType, SalesRow(sales, "AT-1").Entity));
            Assert.Equal(("Intern", "TR IN"), (SalesRow(sales, "IN-1").SupplierType, SalesRow(sales, "IN-1").Entity));
            Assert.Equal(("Intern", "TR AG"), (SalesRow(sales, "UK-1").SupplierType, SalesRow(sales, "UK-1").Entity));
            Assert.Equal(("Intern", "TR IN"), (SalesRow(sales, "UK-2").SupplierType, SalesRow(sales, "UK-2").Entity));
            Assert.Equal(("Intern", "TR IT"), (SalesRow(sales, "IT-1").SupplierType, SalesRow(sales, "IT-1").Entity));
            Assert.Equal(("Extern", string.Empty), (SalesRow(sales, "IT-2").SupplierType, SalesRow(sales, "IT-2").Entity));
            Assert.Equal(("Intern", string.Empty), (SalesRow(sales, "IT-3").SupplierType, SalesRow(sales, "IT-3").Entity));
            Assert.NotEqual(string.Empty, SalesRow(sales, "CH-1").CostSource);
            Assert.Equal((string.Empty, string.Empty, string.Empty), SalesRow(sales, "CH-0"));

            var details = workbook.Worksheet("Finance Details");
            Assert.Equal("Cost Timing", details.Cell(4, DetailsSupplierTypeColumn - 1).GetString());      // nichts verrutscht
            Assert.Equal("Supplier Type", details.Cell(4, DetailsSupplierTypeColumn).GetString());
            Assert.Equal("Delivering Entity", details.Cell(4, DetailsDeliveringEntityColumn).GetString());
            var detailRows = details.RowsUsed().Where(r => r.RowNumber() > 4).ToList();
            Assert.Equal(8, detailRows.Count);   // CH-0 fehlt hier wie bisher
            var uk1 = detailRows.Single(r => r.Cell(10).GetString() == "UK-1");
            Assert.Equal("Intern", uk1.Cell(DetailsSupplierTypeColumn).GetString());
            Assert.Equal("TR AG", uk1.Cell(DetailsDeliveringEntityColumn).GetString());
            Assert.All(detailRows, r => Assert.NotEqual(string.Empty, r.Cell(DetailsSupplierTypeColumn).GetString()));
        }
        finally
        {
            if (Directory.Exists(outputDirectory)) Directory.Delete(outputDirectory, recursive: true);
        }
    }

    [Fact]
    public void SiteFile_Keeps_Its_Columns()
    {
        // Die Standortdateien rechnen keine Gruppenmarge; ihr Blatt Sales endet weiter bei „Trafag Sachnummer“.
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"trafag-supplier-assignment-site-{Guid.NewGuid():N}");
        var service = new ExcelExportService();

        try
        {
            var path = service.CreateExcelFile(outputDirectory, "TRCH", new DateTime(2026, 11, 13), [Record("TRCH", "Schweiz", "CH-1")]);

            using var workbook = new XLWorkbook(path);
            var sales = workbook.Worksheet("Sales");
            Assert.Equal("Trafag Sachnummer", sales.Cell(1, SalesSupplierTypeColumn - 1).GetString());
            Assert.True(sales.Cell(1, SalesSupplierTypeColumn).IsEmpty());
        }
        finally
        {
            if (Directory.Exists(outputDirectory)) Directory.Delete(outputDirectory, recursive: true);
        }
    }

    [Fact]
    public void DeliveringEntity_Follows_The_Same_Rule_As_The_Cost_Path()
    {
        // Anzeige und Kostenpfad duerfen nicht auseinanderlaufen: gleiche Funktion, gleiche Argumente.
        var line = new GroupMarginLine
        {
            Tsc = "TRUK", SupplierNumber = "TR09", SupplierName = "Trafag AG", SupplierCountry = "CH",
            Material = "44073", Quantity = 10m, StandardCost = 100m, StandardCostCurrency = "CHF", NetSalesValue = 1535.5m
        };
        var evaluation = GroupMarginCalculator.Evaluate(line);
        Assert.Equal(GroupMarginSupplierClassifier.Internal, evaluation.SupplierType);
        Assert.Equal(GroupStandardCostEntities.TrAg, evaluation.DeliveringEntity);
        Assert.Equal("TR AG", GroupMarginCalculator.DeliveringEntityLabel(evaluation.DeliveringEntity));
        Assert.Equal(string.Empty, GroupMarginCalculator.DeliveringEntityLabel(null));
    }
}
