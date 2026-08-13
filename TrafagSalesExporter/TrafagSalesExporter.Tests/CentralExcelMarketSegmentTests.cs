using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Marktsegment im zentralen Excel. Der Kopfzeilentest ist Absicht: er schlaegt fehl, wenn
/// jemand eine Spalte in der MITTE einfuegt, weil dann alle nachfolgenden Positionen
/// verrutschen und der zentrale Excel-Nachweis mit seinen Blattformeln still falsch wird.
/// </summary>
public class CentralExcelMarketSegmentTests
{
    private const int MarketSegmentColumn = 50;
    private const int MarketSegmentSourceColumn = 51;

    private static SalesRecord Record(string tsc, string customerNumber, string customerName)
        => new()
        {
            ExtractionDate = new DateTime(2026, 8, 12),
            PostingDate = new DateTime(2026, 7, 1),
            Tsc = tsc,
            Land = tsc == "TRCH" ? "CH" : "IT",
            InvoiceNumber = $"INV-{customerNumber}",
            PositionOnInvoice = 1,
            Material = "MAT-1",
            Name = "Pressure switch",
            Quantity = 1m,
            CustomerNumber = customerNumber,
            CustomerName = customerName,
            CustomerCountry = "CH",
            SalesPriceValue = 100m,
            SalesCurrency = "CHF",
            CompanyCurrency = "CHF",
            StandardCost = 10m,
            StandardCostCurrency = "CHF"
        };

    [Fact]
    public void ConsolidatedExcel_HasSegmentColumnsAtTheEnd_AndKeepsExistingPositions()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"trafag-segment-head-{Guid.NewGuid():N}");
        var service = new ExcelExportService();

        try
        {
            var path = service.CreateConsolidatedExcelFile(
                outputDirectory, new DateTime(2026, 8, 12), [Record("TRCH", "10042", "Stadler Rail AG")]);

            using var workbook = new XLWorkbook(path);
            var sales = workbook.Worksheet("Sales");

            // Bestehende Ankerpositionen, die nicht verrutschen durften.
            Assert.Equal("TSC", sales.Cell(1, 2).GetString());
            Assert.Equal("Customer number", sales.Cell(1, 20).GetString());
            Assert.Equal("Customer Industry", sales.Cell(1, 23).GetString());
            Assert.Equal("Finance | Source Value Field", sales.Cell(1, 49).GetString());

            // Neue Spalten stehen am Ende.
            Assert.Equal("Market Segment", sales.Cell(1, MarketSegmentColumn).GetString());
            Assert.Equal("Market Segment Source", sales.Cell(1, MarketSegmentSourceColumn).GetString());
            Assert.Equal(string.Empty, sales.Cell(1, MarketSegmentSourceColumn + 1).GetString());
        }
        finally
        {
            if (Directory.Exists(outputDirectory)) Directory.Delete(outputDirectory, recursive: true);
        }
    }

    [Fact]
    public void ConsolidatedExcel_WithoutMapping_LeavesSegmentEmpty()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"trafag-segment-empty-{Guid.NewGuid():N}");
        var service = new ExcelExportService();

        try
        {
            var path = service.CreateConsolidatedExcelFile(
                outputDirectory, new DateTime(2026, 8, 12), [Record("TRCH", "10042", "Stadler Rail AG")]);

            using var workbook = new XLWorkbook(path);
            var sales = workbook.Worksheet("Sales");
            Assert.Equal(string.Empty, sales.Cell(2, MarketSegmentColumn).GetString());
            Assert.Equal(string.Empty, sales.Cell(2, MarketSegmentSourceColumn).GetString());
        }
        finally
        {
            if (Directory.Exists(outputDirectory)) Directory.Delete(outputDirectory, recursive: true);
        }
    }

    [Fact]
    public void ConsolidatedExcel_FillsSegmentOnlyForMappedCustomer()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        using (var db = new AppDbContext(options))
        {
            db.Database.EnsureCreated();
            db.CustomerMarketSegments.Add(new CustomerMarketSegment
            {
                Tsc = "TRCH",
                CustomerNumber = "10042",
                CustomerName = "Stadler Rail AG",
                Segment = "Railway",
                Source = "Marktumfrage Railway 2026-05, bestaetigt",
                UpdatedAtUtc = new DateTime(2026, 8, 12, 12, 0, 0, DateTimeKind.Utc)
            });
            db.SaveChanges();
        }

        var outputDirectory = Path.Combine(Path.GetTempPath(), $"trafag-segment-map-{Guid.NewGuid():N}");
        var service = new ExcelExportService(new TestDbContextFactory(options));

        try
        {
            var path = service.CreateConsolidatedExcelFile(
                outputDirectory,
                new DateTime(2026, 8, 12),
                [
                    Record("TRCH", "10042", "Stadler Rail AG"),
                    // Gleiche Nummer, anderer Standort: darf NICHT als Railway gelten.
                    Record("TRIT", "10042", "Irgendwer SRL"),
                    Record("TRCH", "99999", "Nicht zugeordnet AG")
                ]);

            using var workbook = new XLWorkbook(path);
            var sales = workbook.Worksheet("Sales");

            var mapped = FindRow(sales, "INV-10042", "TRCH");
            Assert.Equal("Railway", sales.Cell(mapped, MarketSegmentColumn).GetString());
            Assert.Equal("Marktumfrage Railway 2026-05, bestaetigt",
                sales.Cell(mapped, MarketSegmentSourceColumn).GetString());

            var otherSite = FindRow(sales, "INV-10042", "TRIT");
            Assert.Equal(string.Empty, sales.Cell(otherSite, MarketSegmentColumn).GetString());

            var unmapped = FindRow(sales, "INV-99999", "TRCH");
            Assert.Equal(string.Empty, sales.Cell(unmapped, MarketSegmentColumn).GetString());
        }
        finally
        {
            if (Directory.Exists(outputDirectory)) Directory.Delete(outputDirectory, recursive: true);
        }
    }

    private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new AppDbContext(options));
    }

    private static int FindRow(IXLWorksheet sheet, string invoiceNumber, string tsc)
    {
        var lastRow = sheet.LastRowUsed()!.RowNumber();
        for (var row = 2; row <= lastRow; row++)
        {
            if (sheet.Cell(row, 4).GetString() == invoiceNumber && sheet.Cell(row, 2).GetString() == tsc)
                return row;
        }

        throw new InvalidOperationException($"Zeile {invoiceNumber}/{tsc} nicht gefunden.");
    }
}
