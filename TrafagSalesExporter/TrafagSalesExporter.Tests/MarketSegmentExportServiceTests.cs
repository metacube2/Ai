using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Der Excel-Export der Marktsegmente, ausgeloest ueber den Knopf auf `/marktsegmente`.
/// Konzept: `docs/KONZEPT_RAILWAY_EXPORT_2026-09-01.md`.
///
/// Die Faelle sichern genau die Zusagen, die im Konzept als Abnahmekriterium stehen: dass
/// ein Vorschlag nie als bestaetigt erscheint, dass ueber Waehrungen nicht summiert wird,
/// dass die Jahresregel dieselbe ist wie im zentralen Excel und dass leere Blaetter sagen,
/// was leer bedeutet.
/// </summary>
public class MarketSegmentExportServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public MarketSegmentExportServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;

        using var db = new AppDbContext(_options);
        db.Database.EnsureCreated();
        // CentralSalesRecords haengt per Fremdschluessel an Sites.
        db.Sites.Add(new Site
        {
            Id = 1,
            HanaServerId = null,
            Schema = "test",
            TSC = "TEST",
            Land = "Testland",
            SourceSystem = "SAP",
            IsActive = true
        });
        db.SaveChanges();
    }

    private MarketSegmentExportService CreateService() => new(new TestDbContextFactory(_options));

    private static CentralSalesRecord Line(
        string tsc,
        string customerNumber,
        string customerName,
        decimal value,
        string currency,
        string family = "Pressostat",
        DateTime? postingDate = null,
        string country = "CH")
        => new()
        {
            SiteId = 1,
            Tsc = tsc,
            Land = country,
            CustomerNumber = customerNumber,
            CustomerName = customerName,
            CustomerCountry = country,
            SalesPriceValue = value,
            SalesCurrency = currency,
            ProductFamilyText = family,
            Material = "P-1000",
            InvoiceNumber = "R-1",
            Quantity = 1m,
            PostingDate = postingDate,
            ExtractionDate = new DateTime(2026, 8, 13),
            DocumentType = "Invoice"
        };

    private static CustomerMarketSegment Assignment(
        string tsc, string customerNumber, string name, bool confirmed)
        => new()
        {
            Tsc = tsc,
            CustomerNumber = customerNumber,
            CustomerName = name,
            Segment = "Railway",
            IsConfirmed = confirmed,
            Source = "Marktumfrage Railway 2026-05",
            ProposalNote = confirmed ? string.Empty : "Namenstreffer",
            UpdatedAtUtc = new DateTime(2026, 8, 20, 10, 0, 0, DateTimeKind.Utc)
        };

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> Sheet(
        IReadOnlyList<ExcelSheetData> sheets, string name)
        => sheets.Single(sheet => sheet.SheetName == name).Rows;

    [Fact]
    public async Task Export_HasAllEightSheetsInTheAgreedOrder()
    {
        var sheets = await CreateService().BuildSheetsAsync(null, null, "TRAFAG\\ingo");

        Assert.Equal(
            ["Anleitung", "Pruefung", "Bestaetigt", "Verworfen (Protokoll)",
             "Umsatz_Summen", "Umsatz_Detail", "Marktumfrage", "Datenluecken"],
            sheets.Select(sheet => sheet.SheetName).ToArray());
    }

    [Fact]
    public async Task Proposal_StaysOutOfTheConfirmedSheet()
    {
        using (var db = new AppDbContext(_options))
        {
            db.CustomerMarketSegments.AddRange(
                Assignment("TRCH", "10042", "Stadler Rail AG", confirmed: false),
                Assignment("TRCH", "20001", "Bahn Technik AG", confirmed: true));
            db.CentralSalesRecords.AddRange(
                Line("TRCH", "10042", "Stadler Rail AG", 100m, "CHF"),
                Line("TRCH", "20001", "Bahn Technik AG", 200m, "CHF"));
            db.SaveChanges();
        }

        var sheets = await CreateService().BuildSheetsAsync(null, null, "TRAFAG\\ingo");

        var proposals = Sheet(sheets, "Pruefung");
        var confirmed = Sheet(sheets, "Bestaetigt");

        Assert.Equal("10042", Assert.Single(proposals)["Kundennummer"]);
        Assert.Equal("Vorschlag", proposals[0]["Status"]);
        Assert.Equal("20001", Assert.Single(confirmed)["Kundennummer"]);
        Assert.Equal("Bestaetigt", confirmed[0]["Status"]);
    }

    [Fact]
    public async Task Totals_SplitByCurrencyInsteadOfAddingThemUp()
    {
        using (var db = new AppDbContext(_options))
        {
            db.CustomerMarketSegments.Add(Assignment("TRCH", "10042", "Stadler Rail AG", confirmed: true));
            db.CentralSalesRecords.AddRange(
                Line("TRCH", "10042", "Stadler Rail AG", 100m, "CHF"),
                Line("TRCH", "10042", "Stadler Rail AG", 40m, "EUR"),
                Line("TRCH", "10042", "Stadler Rail AG", 60m, "EUR"));
            db.SaveChanges();
        }

        var totals = Sheet(await CreateService().BuildSheetsAsync(null, null, "TRAFAG\\ingo"), "Umsatz_Summen");

        Assert.Equal(2, totals.Count);
        var chf = totals.Single(row => (string?)row["Waehrung"] == "CHF");
        var eur = totals.Single(row => (string?)row["Waehrung"] == "EUR");
        Assert.Equal(100m, chf["Umsatz"]);
        Assert.Equal(100m, eur["Umsatz"]);
        // Ausdruecklich decimal: `SearchCustomersAsync` musste fuer seine Summe auf double
        // ausweichen, weil SQLite kein SUM ueber decimal kann. Hier wird nach dem Laden im
        // Speicher summiert. Wandert die Summe je wieder in die Datenbank, soll dieser Test
        // laut werden statt still weiter zu bestehen.
        Assert.IsType<decimal>(chf["Umsatz"]);
        Assert.Equal(1, chf["Verkaufszeilen"]);
        Assert.Equal(2, eur["Verkaufszeilen"]);
        // Die Summe 200 ueber beide Waehrungen darf nirgends als eine Zahl auftauchen.
        Assert.DoesNotContain(totals, row => Equals(row["Umsatz"], 200m));
    }

    [Fact]
    public async Task PruefungCarriesNoSalesTotal_BecauseACustomerCanBillInTwoCurrencies()
    {
        using (var db = new AppDbContext(_options))
        {
            db.CustomerMarketSegments.Add(Assignment("TRCH", "10042", "Stadler Rail AG", confirmed: false));
            db.CentralSalesRecords.AddRange(
                Line("TRCH", "10042", "Stadler Rail AG", 100m, "CHF"),
                Line("TRCH", "10042", "Stadler Rail AG", 40m, "EUR"));
            db.SaveChanges();
        }

        var row = Assert.Single(Sheet(await CreateService().BuildSheetsAsync(null, null, "TRAFAG\\ingo"), "Pruefung"));

        Assert.False(row.ContainsKey("Umsatz"));
        Assert.False(row.ContainsKey("Waehrung"));
        Assert.Equal(2, row["Verkaufszeilen"]);
    }

    [Fact]
    public async Task YearFilter_FollowsPostingDateThenInvoiceDateThenExtractionDate()
    {
        using (var db = new AppDbContext(_options))
        {
            db.CustomerMarketSegments.Add(Assignment("TRCH", "10042", "Stadler Rail AG", confirmed: true));
            db.CentralSalesRecords.AddRange(
                Line("TRCH", "10042", "Stadler Rail AG", 100m, "CHF", postingDate: new DateTime(2025, 5, 4)),
                // Ohne Buchungsdatum zaehlt das Extraktionsdatum 2026.
                Line("TRCH", "10042", "Stadler Rail AG", 70m, "CHF"));
            db.SaveChanges();
        }

        var service = CreateService();

        var y2025 = Sheet(await service.BuildSheetsAsync(2025, null, "TRAFAG\\ingo"), "Umsatz_Summen");
        var y2026 = Sheet(await service.BuildSheetsAsync(2026, null, "TRAFAG\\ingo"), "Umsatz_Summen");

        Assert.Equal(100m, Assert.Single(y2025)["Umsatz"]);
        Assert.Equal(2025, y2025[0]["Jahr"]);
        Assert.Equal(70m, Assert.Single(y2026)["Umsatz"]);
        Assert.Equal(2026, y2026[0]["Jahr"]);
    }

    [Fact]
    public async Task BroadPortfolioCustomer_IsMarkedInsteadOfDecided()
    {
        using (var db = new AppDbContext(_options))
        {
            db.CustomerMarketSegments.AddRange(
                Assignment("TRCH", "10042", "Siemens AG", confirmed: false),
                Assignment("TRCH", "20001", "Bahn Technik AG", confirmed: false));
            db.CentralSalesRecords.AddRange(
                Line("TRCH", "10042", "Siemens AG", 10m, "CHF", family: "Pressostat"),
                Line("TRCH", "10042", "Siemens AG", 10m, "CHF", family: "Transmitter"),
                Line("TRCH", "10042", "Siemens AG", 10m, "CHF", family: "Thermostat"),
                Line("TRCH", "10042", "Siemens AG", 10m, "CHF", family: "Sensor"),
                Line("TRCH", "20001", "Bahn Technik AG", 10m, "CHF", family: "Pressostat"));
            db.SaveChanges();
        }

        var rows = Sheet(await CreateService().BuildSheetsAsync(null, null, "TRAFAG\\ingo"), "Pruefung");

        var broad = rows.Single(row => (string?)row["Kundennummer"] == "10042");
        var narrow = rows.Single(row => (string?)row["Kundennummer"] == "20001");
        Assert.Equal(4, broad["Produktsparten"]);
        Assert.Equal("Breites Sortiment, bitte einzeln pruefen", broad["Warnung"]);
        Assert.Equal(string.Empty, narrow["Warnung"]);
        // Der Export markiert nur, er entscheidet nicht: das Segment bleibt unveraendert.
        Assert.Equal("Railway", broad["Segment"]);
    }

    [Fact]
    public async Task RemovalLog_SplitsTheDetailsTextIntoColumnsAndKeepsUnknownShapesWhole()
    {
        using (var db = new AppDbContext(_options))
        {
            db.AppEventLogs.AddRange(
                new AppEventLog
                {
                    Timestamp = new DateTime(2026, 8, 30, 9, 0, 0, DateTimeKind.Utc),
                    Category = "Marktsegment",
                    Message = "Segment entfernt",
                    Details = "TRCH/10042 | war Railway/Vorschlag"
                },
                new AppEventLog
                {
                    Timestamp = new DateTime(2026, 8, 29, 9, 0, 0, DateTimeKind.Utc),
                    Category = "Marktsegment",
                    Message = "Segment entfernt",
                    Details = "unerwartetes Format"
                },
                // Fremde Ereignisse gehoeren nicht in das Blatt.
                new AppEventLog
                {
                    Timestamp = new DateTime(2026, 8, 28, 9, 0, 0, DateTimeKind.Utc),
                    Category = "Marktsegment",
                    Message = "Vorschlag bestaetigt",
                    Details = "TRCH/20001 | Railway"
                });
            db.SaveChanges();
        }

        var rows = Sheet(await CreateService().BuildSheetsAsync(null, null, "TRAFAG\\ingo"), "Verworfen (Protokoll)");

        Assert.Equal(2, rows.Count);
        Assert.Equal("TRCH", rows[0]["TSC"]);
        Assert.Equal("10042", rows[0]["Kundennummer"]);
        Assert.Equal("Railway/Vorschlag", rows[0]["Vorheriger Stand"]);
        // Nicht raten: passt der Text nicht auf das Muster, bleibt er unzerlegt stehen.
        Assert.Equal(string.Empty, rows[1]["TSC"]);
        Assert.Equal("unerwartetes Format", rows[1]["Vorheriger Stand"]);
        // AppEventLog fuehrt keinen Nutzer, deshalb darf es die Spalte nicht geben.
        Assert.False(rows[0].ContainsKey("Nutzer"));
    }

    [Fact]
    public async Task EmptySheet_SaysWhatEmptyMeansInsteadOfShowingNothing()
    {
        var sheets = await CreateService().BuildSheetsAsync(null, null, "TRAFAG\\ingo");

        var confirmed = Assert.Single(Sheet(sheets, "Bestaetigt"));
        Assert.Contains("noch nichts fachlich geprueft", (string?)confirmed[MarketSegmentExportService.HintColumn]);
    }

    [Fact]
    public async Task DataGaps_ShowTheMissingCustomerNameInsteadOfZeroSales()
    {
        using (var db = new AppDbContext(_options))
        {
            db.CentralSalesRecords.AddRange(
                Line("TRDE", "70001", string.Empty, 500m, "EUR", country: string.Empty),
                Line("TRDE", "70002", string.Empty, 300m, "EUR", country: string.Empty),
                Line("TRCH", "10042", "Stadler Rail AG", 100m, "CHF"));
            db.MarketSurveyEntries.AddRange(
                new MarketSurveyEntry { SurveyName = "Railway 2026-05", CustomerName = "Stadler", LinkedCustomerNumber = "10042" },
                new MarketSurveyEntry { SurveyName = "Railway 2026-05", CustomerName = "Interessent", LinkedCustomerNumber = string.Empty });
            db.SaveChanges();
        }

        var rows = Sheet(await CreateService().BuildSheetsAsync(null, null, "TRAFAG\\ingo"), "Datenluecken");

        var withoutName = rows.Single(row =>
            (string?)row["Schluessel"] == "TRDE" && (string?)row["Kennzahl"] == "Zeilen ohne Kundenname");
        Assert.Equal(2, withoutName["Wert"]);

        var chWithoutName = rows.Single(row =>
            (string?)row["Schluessel"] == "TRCH" && (string?)row["Kennzahl"] == "Zeilen ohne Kundenname");
        Assert.Equal(0, chWithoutName["Wert"]);

        var unlinked = rows.Single(row =>
            (string?)row["Schluessel"] == "Railway 2026-05" && (string?)row["Kennzahl"] == "Zeilen ohne verknuepften Kunden");
        Assert.Equal(1, unlinked["Wert"]);
    }

    [Fact]
    public async Task SiteFilter_LimitsTheExportButTheInstructionSheetSaysSo()
    {
        using (var db = new AppDbContext(_options))
        {
            db.CustomerMarketSegments.AddRange(
                Assignment("TRCH", "10042", "Stadler Rail AG", confirmed: false),
                Assignment("TRIT", "30001", "Ferrovie SRL", confirmed: false));
            db.CentralSalesRecords.AddRange(
                Line("TRCH", "10042", "Stadler Rail AG", 100m, "CHF"),
                Line("TRIT", "30001", "Ferrovie SRL", 80m, "EUR", country: "IT"));
            db.SaveChanges();
        }

        var sheets = await CreateService().BuildSheetsAsync(null, "TRIT", "TRAFAG\\ingo");

        var proposals = Sheet(sheets, "Pruefung");
        Assert.Equal("30001", Assert.Single(proposals)["Kundennummer"]);

        var instructions = Sheet(sheets, "Anleitung");
        Assert.Equal("TRIT", instructions.Single(row => (string?)row["Feld"] == "Gewaehlter Standort")["Wert"]);
        Assert.Contains("nicht angewendet",
            (string?)instructions.Single(row => (string?)row["Feld"] == "Namensfilter")["Wert"]);
    }

    [Fact]
    public async Task Instructions_NameTheGeneratorAndTheYearRule()
    {
        var instructions = Sheet(await CreateService().BuildSheetsAsync(2026, null, "TRAFAG\\ingo"), "Anleitung");

        Assert.Equal("TRAFAG\\ingo", instructions.Single(row => (string?)row["Feld"] == "Erzeugt von")["Wert"]);
        Assert.Equal("2026", instructions.Single(row => (string?)row["Feld"] == "Gewaehltes Jahr")["Wert"]);
        Assert.Contains("Buchungsdatum",
            (string?)instructions.Single(row => (string?)row["Feld"] == "Periodenregel")["Wert"]);
        Assert.IsType<DateTime>(instructions.Single(row => (string?)row["Feld"] == "Erzeugt am")["Wert"]);
    }

    [Fact]
    public async Task Detail_CarriesNoCostOrMarginFields()
    {
        using (var db = new AppDbContext(_options))
        {
            db.CustomerMarketSegments.Add(Assignment("TRCH", "10042", "Stadler Rail AG", confirmed: true));
            db.CentralSalesRecords.Add(Line("TRCH", "10042", "Stadler Rail AG", 100m, "CHF"));
            db.SaveChanges();
        }

        var row = Assert.Single(Sheet(await CreateService().BuildSheetsAsync(null, null, "TRAFAG\\ingo"), "Umsatz_Detail"));

        Assert.Equal("P-1000", row["Material"]);
        Assert.Equal(100m, row["Umsatz"]);
        Assert.Equal("CHF", row["Waehrung"]);
        foreach (var forbidden in new[] { "StandardCost", "Standardkosten", "Marge", "Kosten" })
            Assert.False(row.ContainsKey(forbidden));
    }

    /// <summary>
    /// Der Weg bis zur fertigen Datei, nicht nur bis zur Blattliste. Ein Blatt ohne Zeilen
    /// haette keine Kopfzeile, und ein zu langer oder doppelter Blattname liesse ClosedXML
    /// still umbenennen; beides faellt erst beim Zuruecklesen auf.
    /// </summary>
    [Fact]
    public async Task Workbook_IsReadableAndKeepsEverySheetName()
    {
        using (var db = new AppDbContext(_options))
        {
            db.CustomerMarketSegments.Add(Assignment("TRCH", "10042", "Stadler Rail AG", confirmed: false));
            db.CentralSalesRecords.Add(Line("TRCH", "10042", "Stadler Rail AG", 100m, "CHF"));
            db.SaveChanges();
        }

        var sheets = await CreateService().BuildSheetsAsync(null, null, "TRAFAG\\ingo");
        var bytes = new ExcelExportService().CreateWorkbookBytes(sheets);

        using var stream = new MemoryStream(bytes);
        using var workbook = new ClosedXML.Excel.XLWorkbook(stream);

        Assert.Equal(
            ["Anleitung", "Pruefung", "Bestaetigt", "Verworfen (Protokoll)",
             "Umsatz_Summen", "Umsatz_Detail", "Marktumfrage", "Datenluecken"],
            workbook.Worksheets.Select(sheet => sheet.Name).ToArray());

        var pruefung = workbook.Worksheet("Pruefung");
        Assert.Equal("TSC", pruefung.Cell(1, 1).GetString());
        Assert.Equal("10042", pruefung.Cell(2, 2).GetString());

        // Auch ein leeres Blatt traegt eine lesbare Aussage statt einer leeren Flaeche.
        Assert.Equal(MarketSegmentExportService.HintColumn, workbook.Worksheet("Bestaetigt").Cell(1, 1).GetString());
    }

    public void Dispose() => _connection.Dispose();

    private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new AppDbContext(options));
    }
}
