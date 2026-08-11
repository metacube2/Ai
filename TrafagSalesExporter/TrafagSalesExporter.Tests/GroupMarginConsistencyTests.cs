using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Dieselbe Zeile durch BEIDE Wege: Excel-Nachweis und Management-Cockpit muessen zum selben
/// Ergebnis kommen.
///
/// Der Test existiert wegen eines konkreten Fehlers: am 2026-08-05 wurde der Status
/// „Konzernkosten fehlen" eingebaut, kam aber nur im Excel-Nachweis an. Das Cockpit rief seine
/// gespiegelte Statusfunktion ohne das neue Kennzeichen auf und zeigte fuer dieselbe Zeile
/// weiter „Standardpreis fehlt". Reine Tests der Rechenklasse haetten das nicht gefunden - sie
/// waeren gruen geblieben, waehrend die Aufrufstelle das Ergebnis wegwirft. Deshalb geht dieser
/// Test durch die oeffentlichen Einstiegspunkte.
/// </summary>
public class GroupMarginConsistencyTests : IDisposable
{
    private const int Year = 2025;
    private static readonly DateTime PostingDate = new(Year, 3, 1);
    private static readonly DateTime ExtractionDate = new(Year, 12, 31);

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly ManagementCockpitService _cockpit;

    public GroupMarginConsistencyTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;

        using (var db = new AppDbContext(_options))
        {
            db.Database.EnsureCreated();
            db.Sites.Add(new Site
            {
                Id = 1,
                Schema = "test",
                TSC = "TRIN",
                Land = "Indien",
                SourceSystem = "SAGE",
                IsActive = true
            });
            // Das Cockpit setzt fuer Indien die Landeswaehrung INR. Damit Verkaufs- und
            // Kostenwaehrung uebereinstimmen, rechnet dieser Test durchgehend in INR - sonst
            // uebersteuert der Waehrungsschalter den Status, den wir hier vergleichen wollen.
            db.CurrencyExchangeRates.Add(new CurrencyExchangeRate
            {
                FromCurrency = "INR",
                ToCurrency = "CHF",
                Rate = 0.01m,
                ValidFrom = new DateTime(2024, 1, 1),
                IsActive = true
            });
            db.SaveChanges();
        }

        _cockpit = new ManagementCockpitService(new TestDbContextFactory(_options));
    }

    public void Dispose() => _connection.Dispose();

    [Theory]
    // Konzernvertrieb ohne Konzernkostentreffer: Kostenbasis bleibt offen, statt den
    // IC-Einkaufspreis als Herstellkosten auszuweisen. Genau der Fall, der auseinanderlief.
    [InlineData("LRD", "Konzernkosten fehlen", "Konzernkosten fehlen (lokaler Wert ist IC-Preis)", 0)]
    // Eigenfertigung am Standort: der lokale Standardpreis IST die Herstellkostenbasis.
    [InlineData("FFM", "OK", "Interner Standardpreis", 60)]
    [InlineData("CM", "OK", "Interner Standardpreis", 60)]
    // Ohne Sales Type bleibt der Lieferant unklar - unveraendertes Verhalten.
    [InlineData("", "Lieferant unklar", "Lieferant unklar", 60)]
    public async Task Excel_Nachweis_und_Cockpit_bewerten_dieselbe_Zeile_gleich(
        string salesType, string expectedStatus, string expectedCostSource, int expectedCostBasis)
    {
        var excel = BuildExcelRow(salesType);
        var cockpit = await BuildCockpitRowAsync(salesType);

        Assert.Equal(expectedStatus, excel.Status);
        Assert.Equal(expectedStatus, cockpit.Status);

        Assert.Equal(expectedCostSource, excel.CostSource);
        Assert.Equal(expectedCostSource, cockpit.CostSource);

        Assert.Equal(expectedCostBasis, excel.CostBasis);
        Assert.Equal(expectedCostBasis, cockpit.CostBasis);

        Assert.Equal(excel.SupplierType, cockpit.SupplierType);
    }

    [Fact]
    public async Task Offene_Kostenbasis_zaehlt_im_Cockpit_wie_im_Nachweis()
    {
        // Die Kennzahl „offene Kostenbasis" darf den neuen Status nicht uebergehen - sonst
        // meldet das Cockpit weniger offene Zeilen, als der Nachweis fuer dieselben Daten zeigt.
        await SeedCockpitRowAsync("LRD");

        var result = await _cockpit.AnalyzeFinanceSummaryAsync(Year, null, null);

        Assert.Equal(1, result.GroupMarginSummary.MissingCostRows);
        Assert.Equal(0m, result.GroupMarginSummary.CleanCostBasisPercent);
    }

    [Fact]
    public async Task Audit_Ledger_meldet_denselben_Status_wie_die_Gruppenmarge()
    {
        await SeedCockpitRowAsync("LRD");

        var result = await _cockpit.AnalyzeFinanceSummaryAsync(Year, null, null);

        var detail = Assert.Single(result.GroupMarginDetailRows);
        var ledger = Assert.Single(result.FinanceAuditLedgerRows);
        Assert.Equal(GroupMarginStatuses.GroupCostMissing, detail.Status);
        Assert.Equal(GroupMarginStatuses.GroupCostMissing, ledger.Status);
    }

    [Fact]
    public async Task MarcNichttreffer_WirdInExcelUndCockpitLokalMitLokalenStandardkosten()
    {
        await using (var db = new AppDbContext(_options))
        {
            db.GroupMaterialMasters.Add(new GroupMaterialMaster
            {
                MaterialKey = "OTHER-MATERIAL",
                Plant = "1100",
                RefreshedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var excel = BuildExcelRow("", useDatabaseSettings: true);
        var cockpit = await BuildCockpitRowAsync("");

        Assert.Equal(GroupMarginSupplierClassifier.Local, excel.SupplierType);
        Assert.Equal(GroupMarginSupplierClassifier.Local, cockpit.SupplierType);
        Assert.Equal(GroupMarginStatuses.Ok, excel.Status);
        Assert.Equal(GroupMarginStatuses.Ok, cockpit.Status);
        Assert.Equal("Standardkosten der lokalen Gesellschaft", excel.CostSource);
        Assert.Equal("Standardkosten der lokalen Gesellschaft", cockpit.CostSource);
        Assert.Equal(60m, excel.CostBasis);
        Assert.Equal(60m, cockpit.CostBasis);

        var result = await _cockpit.AnalyzeFinanceSummaryAsync(Year, null, null);
        Assert.Equal(1, result.GroupMarginSummary.LocalSupplierRows);
        Assert.Equal(0, result.GroupMarginSummary.UnclearSupplierRows);
        Assert.Equal(100m, result.GroupMarginSummary.CleanCostBasisPercent);
    }

    private ProofRow BuildExcelRow(string salesType, bool useDatabaseSettings = false)
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"trafag-groupmargin-{Guid.NewGuid():N}");
        try
        {
            var service = useDatabaseSettings
                ? new ExcelExportService(new TestDbContextFactory(_options))
                : new ExcelExportService();
            var path = service.CreateConsolidatedExcelFile(
                outputDirectory, ExtractionDate, [BuildSalesRecord(salesType)]);

            using var workbook = new XLWorkbook(path);
            var details = workbook.Worksheet("Gruppenmarge Details");
            return new ProofRow(
                Status: details.Cell(2, 2).GetString(),
                SupplierType: details.Cell(2, 13).GetString(),
                CostSource: details.Cell(2, 14).GetString(),
                CostBasis: details.Cell(2, 18).GetValue<decimal>());
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, recursive: true);
        }
    }

    private async Task<ProofRow> BuildCockpitRowAsync(string salesType)
    {
        await SeedCockpitRowAsync(salesType);
        var result = await _cockpit.AnalyzeFinanceSummaryAsync(Year, null, null);
        var row = Assert.Single(result.GroupMarginDetailRows);
        return new ProofRow(row.Status, row.SupplierType, row.CostSource, row.CostBasisValue);
    }

    private async Task SeedCockpitRowAsync(string salesType)
    {
        await using var db = new AppDbContext(_options);
        db.CentralSalesRecords.RemoveRange(db.CentralSalesRecords);
        await db.SaveChangesAsync();
        db.CentralSalesRecords.Add(BuildCentralRecord(salesType));
        await db.SaveChangesAsync();
    }

    // Beide Datensaetze beschreiben dieselbe Zeile: Indien, ohne jede Lieferantenangabe,
    // Menge 1, lokaler Standardpreis 60 INR, Umsatz 100 INR.
    private static SalesRecord BuildSalesRecord(string salesType) => new()
    {
        ExtractionDate = ExtractionDate,
        PostingDate = PostingDate,
        InvoiceDate = PostingDate,
        Tsc = "TRIN",
        Land = "Indien",
        InvoiceNumber = "INV-1",
        PositionOnInvoice = 1,
        Material = "IC15415",
        Name = "Pressure switch",
        SalesType = salesType,
        Quantity = 1m,
        SalesPriceValue = 100m,
        SalesCurrency = "INR",
        CompanyCurrency = "INR",
        StandardCost = 60m,
        StandardCostCurrency = "INR"
    };

    private static CentralSalesRecord BuildCentralRecord(string salesType) => new()
    {
        SiteId = 1,
        StoredAtUtc = DateTime.UtcNow,
        SourceSystem = "SAGE",
        ExtractionDate = ExtractionDate,
        PostingDate = PostingDate,
        InvoiceDate = PostingDate,
        Tsc = "TRIN",
        Land = "Indien",
        InvoiceNumber = "INV-1",
        PositionOnInvoice = 1,
        Material = "IC15415",
        Name = "Pressure switch",
        SalesType = salesType,
        Quantity = 1m,
        SalesPriceValue = 100m,
        SalesCurrency = "INR",
        StandardCost = 60m,
        StandardCostCurrency = "INR",
        DocumentType = "Invoice"
    };

    private sealed record ProofRow(string Status, string SupplierType, string CostSource, decimal CostBasis);

    private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new AppDbContext(options));
    }
}
