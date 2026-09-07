using ClosedXML.Excel;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Tests fuer die Anreicherung der zentralen Excel-Dateien: Sparten-Fallback ueber die
/// Materialnummer (wie in den Dashboards) und CHF-Umrechnung je Finance-Jahr.
/// Die Records selbst duerfen dabei NICHT veraendert werden — dieselbe Liste geht
/// nach dem Excel in die Audit-CSV, und die muss die Rohdaten zeigen.
/// </summary>
public class CentralExcelEnrichmentTests
{
    private sealed class FakeExchangeRateService : ICurrencyExchangeRateService
    {
        public decimal? ResolveRate(string fromCurrency, string toCurrency, DateTime? effectiveDate)
        {
            var from = NormalizeCurrencyCode(fromCurrency);
            if (string.Equals(from, "CHF", StringComparison.OrdinalIgnoreCase))
                return 1m;
            if (string.Equals(from, "EUR", StringComparison.OrdinalIgnoreCase))
                return 0.95m;
            return null;
        }

        public string NormalizeCurrencyCode(string? currencyCode)
            => currencyCode?.Trim().ToUpperInvariant() ?? string.Empty;
    }

    private static SalesRecord SwissReferenceRecord() => new()
    {
        ExtractionDate = new DateTime(2026, 7, 15),
        PostingDate = new DateTime(2025, 3, 10),
        InvoiceDate = new DateTime(2025, 3, 10),
        Tsc = "ZSCHWEIZ",
        Land = "CH",
        InvoiceNumber = "90000001",
        PositionOnInvoice = 1,
        Material = "43125",
        Name = "Pressostat",
        Quantity = 1m,
        SalesPriceValue = 200m,
        SalesCurrency = "CHF",
        CompanyCurrency = "CHF",
        ProductDivisionCode = "0005",
        ProductDivisionText = "Transmitters",
        ProductFamilyCode = "F1",
        ProductFamilyText = "Family",
        ProductHierarchyCode = "H1",
        ProductHierarchyText = "Hierarchy",
        ProductMappingAssigned = "X"
    };

    private static SalesRecord FrenchRecordWithoutDivision() => new()
    {
        ExtractionDate = new DateTime(2026, 7, 15),
        PostingDate = new DateTime(2025, 4, 2),
        InvoiceDate = new DateTime(2025, 4, 2),
        Tsc = "TRFR",
        Land = "FR",
        InvoiceNumber = "FR-1",
        PositionOnInvoice = 1,
        // Fuehrende Nullen wie im B1-Export — muss trotzdem auf 43125 matchen.
        Material = "000043125",
        Name = "Pressostat",
        Quantity = 2m,
        SalesPriceValue = 500m,
        SalesCurrency = "EUR",
        CompanyCurrency = "EUR"
    };

    // ---------- Enricher (pur) ----------

    [Fact]
    public void ResolveFallback_InheritsAcrossCountries_ViaNormalizedMaterial()
    {
        var swiss = SwissReferenceRecord();
        var french = FrenchRecordWithoutDivision();
        var pool = ProductReferenceEnricher.BuildReferenceByMaterial([swiss, french]);

        var reference = ProductReferenceEnricher.ResolveFallback(french, pool);

        Assert.NotNull(reference);
        Assert.Equal("0005", reference!.ProductDivisionCode);
    }

    [Fact]
    public void ResolveFallback_ReturnsNull_WhenRecordHasOwnReference()
    {
        var swiss = SwissReferenceRecord();
        var pool = ProductReferenceEnricher.BuildReferenceByMaterial([swiss]);

        Assert.Null(ProductReferenceEnricher.ResolveFallback(swiss, pool));
    }

    [Fact]
    public void ResolveFallback_ReturnsNull_ForUnknownMaterial()
    {
        var pool = ProductReferenceEnricher.BuildReferenceByMaterial([SwissReferenceRecord()]);
        var unknown = FrenchRecordWithoutDivision();
        unknown.Material = "99999";

        Assert.Null(ProductReferenceEnricher.ResolveFallback(unknown, pool));
    }

    [Fact]
    public void BuildReferenceByMaterial_PrefersAssignedReference()
    {
        var unassigned = SwissReferenceRecord();
        unassigned.Tsc = "AAAA";
        unassigned.ProductDivisionCode = "UNASS";
        unassigned.ProductMappingAssigned = string.Empty;
        var assigned = SwissReferenceRecord();

        var pool = ProductReferenceEnricher.BuildReferenceByMaterial([unassigned, assigned]);

        Assert.Equal("ZSCHWEIZ", pool["43125"].Tsc);
        Assert.Equal("0005", pool["43125"].ProductDivisionCode);
    }

    // ---------- Sales_All Workbook ----------

    [Fact]
    public void ConsolidatedExcel_InheritsDivision_WritesChf_AndKeepsRecordsUntouched()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"trafag-central-enrich-{Guid.NewGuid():N}");
        var service = new ExcelExportService(null!, new FakeExchangeRateService());
        var swiss = SwissReferenceRecord();
        var french = FrenchRecordWithoutDivision();

        try
        {
            var path = service.CreateConsolidatedExcelFile(
                outputDirectory, new DateTime(2026, 7, 15), [swiss, french]);

            // Die Quelle der Audit-CSV bleibt roh: keine Mutation der Records.
            Assert.Equal(string.Empty, french.ProductDivisionCode);

            using var workbook = new XLWorkbook(path);
            var sales = workbook.Worksheet("Sales");
            var frenchRow = FindRowByValue(sales, column: 2, value: "TRFR", firstRow: 2);
            Assert.Equal("0005", sales.Cell(frenchRow, 13).GetString());
            Assert.Equal("Transmitters", sales.Cell(frenchRow, 14).GetString());
            // Assigned bleibt leer -> als geerbt erkennbar.
            Assert.Equal(string.Empty, sales.Cell(frenchRow, 15).GetString());

            var details = workbook.Worksheet("Finance Details");
            Assert.Equal("Product Division Code", details.Cell(4, 30).GetString());
            Assert.Equal("CHF Rate", details.Cell(4, 32).GetString());
            Assert.Equal("Net Sales CHF", details.Cell(4, 33).GetString());

            var frenchDetailRow = FindRowByValue(details, column: 7, value: "TRFR", firstRow: 5);
            Assert.Equal("0005", details.Cell(frenchDetailRow, 30).GetString());
            Assert.Equal(0.95, details.Cell(frenchDetailRow, 32).GetDouble(), precision: 6);
            Assert.Equal(475.0, details.Cell(frenchDetailRow, 33).GetDouble(), precision: 6);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, recursive: true);
        }
    }

    // ---------- Nachweis-Excel ----------

    [Fact]
    public void ProofWorkbook_FinanceDetails_CarriesInheritedDivision_AndChf()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"trafag-proof-enrich-{Guid.NewGuid():N}");
        var service = new ExcelExportService(null!, new FakeExchangeRateService());

        try
        {
            var path = service.CreateDashboardProofExcelFile(
                outputDirectory,
                new DateTime(2026, 7, 15),
                [SwissReferenceRecord(), FrenchRecordWithoutDivision()],
                useAuditCsvAsCentralSource: true);

            using var workbook = new XLWorkbook(path);
            var details = workbook.Worksheet("Finance Details");
            Assert.Equal("CHF Rate", details.Cell(1, 42).GetString());
            Assert.Equal("Net Sales CHF", details.Cell(1, 43).GetString());

            var frenchRow = FindRowByValue(details, column: 8, value: "TRFR", firstRow: 2);
            Assert.Equal("0005", details.Cell(frenchRow, 34).GetString());
            Assert.Equal(0.95, details.Cell(frenchRow, 42).GetDouble(), precision: 6);

            var swissRow = FindRowByValue(details, column: 8, value: "ZSCHWEIZ", firstRow: 2);
            Assert.Equal(1.0, details.Cell(swissRow, 42).GetDouble(), precision: 6);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, recursive: true);
        }
    }

    private static int FindRowByValue(IXLWorksheet worksheet, int column, string value, int firstRow)
    {
        for (var row = firstRow; row <= firstRow + 50; row++)
        {
            if (worksheet.Cell(row, column).GetString() == value)
                return row;
        }

        throw new InvalidOperationException($"Kein '{value}' in Spalte {column} ab Zeile {firstRow} gefunden.");
    }
}
