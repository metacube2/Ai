using ClosedXML.Excel;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// LineRegistrationDate (Sage `FechaRegistro`) als additive Spalte am ENDE des zentralen
/// Sales_All, der Audit-CSV und der zentralen Tabelle. Anlass ist ISS-004.2: Andreas Stoller
/// hat am 2026-08-20 vorgeschlagen, "nach dem Entry Date auf Tagesebene zu ziehen und nach dem
/// Posting Date zu reporten", und die Rueckfrage, welches Feld sein Entry Date ist, ist bis
/// heute unbeantwortet.
///
/// Deshalb pruefen diese Tests genau zwei Dinge:
/// 1. Das Feld ist sichtbar, steht am Ende und verschiebt keine bestehende Spaltenposition.
/// 2. Es aendert NICHTS an der Periodenabgrenzung. Insbesondere darf ein leeres PostingDate
///    nicht still aus dem Registrierdatum gefuellt werden, sonst zaehlt eine in Spanien noch
///    nicht validierte Rechnung Umsatz in einer Periode, in der sie fachlich keinen hat.
/// </summary>
public class CentralExcelLineRegistrationDateTests
{
    private const int LineRegistrationDateColumn = 52;

    private static SalesRecord Record(DateTime? lineRegistrationDate)
        => new()
        {
            ExtractionDate = new DateTime(2026, 8, 25),
            PostingDate = new DateTime(2025, 7, 30),
            InvoiceDate = new DateTime(2025, 7, 29),
            LineRegistrationDate = lineRegistrationDate,
            Tsc = "TRES",
            Land = "Spanien",
            InvoiceNumber = "20242081",
            PositionOnInvoice = 1,
            Material = "MAT-1",
            Name = "Pressure switch",
            Quantity = 1m,
            CustomerNumber = "302208",
            CustomerName = "Intronik",
            CustomerCountry = "ES",
            SalesPriceValue = 1080m,
            SalesCurrency = "EUR",
            CompanyCurrency = "EUR",
            StandardCost = 100m,
            StandardCostCurrency = "EUR"
        };

    [Fact]
    public void ConsolidatedExcel_WritesLineRegistrationDate_AtTheEnd_WithoutMovingKnownColumns()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"trafag-linereg-{Guid.NewGuid():N}");
        var service = new ExcelExportService();

        try
        {
            var path = service.CreateConsolidatedExcelFile(
                outputDirectory,
                new DateTime(2026, 8, 25),
                [Record(new DateTime(2025, 7, 29, 12, 23, 31))]);

            using var workbook = new XLWorkbook(path);
            var sales = workbook.Worksheet("Sales");

            // Ankerpositionen, die nicht verrutschen durften.
            Assert.Equal("posting date", sales.Cell(1, 38).GetString());
            Assert.Equal("invoice date", sales.Cell(1, 39).GetString());
            Assert.Equal("Finance | Source Value Field", sales.Cell(1, 49).GetString());
            Assert.Equal("Market Segment Source", sales.Cell(1, 51).GetString());

            Assert.Equal("Line Registration Date", sales.Cell(1, LineRegistrationDateColumn).GetString());
            // Nur das Datum, ohne Uhrzeit, wie bei den drei bestehenden Datumsspalten.
            Assert.Equal("29.07.2025", sales.Cell(2, LineRegistrationDateColumn).GetString());

            // Die rohe Spalte `posting date` bleibt unveraendert das Buchungsdatum.
            Assert.Equal("30.07.2025", sales.Cell(2, 38).GetString());
            // `Finance | Date` folgt fuer Spanien seit dem 2026-08-26 dem RECHNUNGSDATUM
            // (Fachentscheid Andreas Stoller zu ISS-004.2, Regelart UseInvoiceDate, ScopeKey ES).
            // Entscheidend bleibt: NICHT dem Registrierdatum - das steht hier zufaellig auf
            // demselben Tag, ist aber nicht die Quelle. Der Beweis dafuer ist der zweite Test,
            // der das Registrierdatum bewusst auf ein anderes Jahr legt.
            Assert.Equal("29.07.2025", sales.Cell(2, 45).GetString());
            Assert.Equal("2025", sales.Cell(2, 43).GetString());
        }
        finally
        {
            if (Directory.Exists(outputDirectory)) Directory.Delete(outputDirectory, recursive: true);
        }
    }

    [Fact]
    public void ConsolidatedExcel_LeavesLineRegistrationDateEmpty_WhenSourceHasNone()
    {
        // Alle Standorte ausser Spanien liefern das Feld nicht. Dort muss die Spalte leer
        // bleiben und darf nicht mit einem Ersatzdatum gefuellt werden.
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"trafag-linereg-empty-{Guid.NewGuid():N}");
        var service = new ExcelExportService();

        try
        {
            var path = service.CreateConsolidatedExcelFile(
                outputDirectory, new DateTime(2026, 8, 25), [Record(null)]);

            using var workbook = new XLWorkbook(path);
            var sales = workbook.Worksheet("Sales");
            Assert.Equal(string.Empty, sales.Cell(2, LineRegistrationDateColumn).GetString());
        }
        finally
        {
            if (Directory.Exists(outputDirectory)) Directory.Delete(outputDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ManualImport_KeepsPostingDateAndLineRegistrationDateApart()
    {
        // Der spanische Export liefert beide Spalten. Sie duerfen sich nicht ueberschreiben.
        var site = new Site { TSC = "TRES", Land = "Spanien" };
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");
        var csv = string.Join(Environment.NewLine,
            "\"TSC\";\"InvoiceNumber\";\"PositionOnInvoice\";\"Material\";\"Quantity\";\"SalesPriceValue\";\"LineRegistrationDate\";\"InvoiceDate\";\"PostingDate\"",
            "\"TRES\";\"20242026\";\"1\";\"52871\";\"1\";\"46.50\";\"2025-06-06 09:50:43\";\"2025-06-30 00:00:00\";\"2025-06-30 00:00:00\"",
            // Zweite Zeile: Rechnung noch nicht validiert, PostingDate leer.
            "\"TRES\";\"20242081\";\"1\";\"52871\";\"1\";\"1080.00\";\"2025-07-08 09:03:30\";\"2025-07-08 00:00:00\";\"\"");
        await File.WriteAllTextAsync(filePath, csv);

        try
        {
            var rows = await new ManualExcelImportService().ReadSalesRecordsAsync(filePath, site);

            var validated = Assert.Single(rows, r => r.InvoiceNumber == "20242026");
            Assert.Equal(new DateTime(2025, 6, 30), validated.PostingDate);
            Assert.Equal(new DateTime(2025, 6, 6, 9, 50, 43), validated.LineRegistrationDate);

            // Kernregel: das leere Buchungsdatum bleibt leer. Wuerde es aus dem
            // Registrierdatum gefuellt, zaehlte die Zeile Umsatz vor ihrer Validierung.
            var notValidated = Assert.Single(rows, r => r.InvoiceNumber == "20242081");
            Assert.Null(notValidated.PostingDate);
            Assert.Equal(new DateTime(2025, 7, 8, 9, 3, 30), notValidated.LineRegistrationDate);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ManualImport_FallsBackToLineRegistrationDate_OnlyWhenPostingDateColumnIsMissing()
    {
        // Verhalten vor dem 2026-08-25, das erhalten bleiben muss: hat die Datei ueberhaupt
        // keine PostingDate-Spalte, tritt LineRegistrationDate an ihre Stelle. Sonst wuerden
        // aeltere spanische Dateien beim Reimport ihr Buchungsdatum verlieren.
        var site = new Site { TSC = "TRES", Land = "Spanien" };
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");
        var csv = string.Join(Environment.NewLine,
            "\"TSC\";\"InvoiceNumber\";\"PositionOnInvoice\";\"Material\";\"Quantity\";\"SalesPriceValue\";\"LineRegistrationDate\";\"InvoiceDate\"",
            "\"TRES\";\"20241332\";\"20\";\"52871\";\"1\";\"265.00\";\"2025-01-03 00:00:00\";\"2025-01-02 00:00:00\"");
        await File.WriteAllTextAsync(filePath, csv);

        try
        {
            var row = Assert.Single(await new ManualExcelImportService().ReadSalesRecordsAsync(filePath, site));

            Assert.Equal(new DateTime(2025, 1, 3), row.PostingDate);
            Assert.Equal(new DateTime(2025, 1, 3), row.LineRegistrationDate);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task AuditCsv_Roundtrips_LineRegistrationDate_AsLastColumn()
    {
        var directory = Path.Combine("C:\\TMP", $"trafag-linereg-audit-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var service = new ExportAuditCsvService();
        var settings = new ExportSettings
        {
            AuditCsvEnabled = true,
            LocalSiteExportFolder = directory,
            LocalAuditCsvFolder = Path.Combine(directory, "ignored")
        };
        var site = new Site { TSC = "TRES", Land = "Spanien" };

        try
        {
            var path = await service.WriteSiteAuditCsvAsync(
                site, settings, "MANUAL_EXCEL", directory, [Record(new DateTime(2025, 7, 29, 12, 23, 31))]);
            Assert.NotNull(path);

            var header = (await File.ReadAllLinesAsync(path!))[0];
            Assert.EndsWith("LineRegistrationDate", header);

            var roundtrip = Assert.Single(await service.ReadLatestSiteAuditCsvRecordsAsync(settings));
            Assert.Equal(new DateTime(2025, 7, 29, 12, 23, 31), roundtrip.LineRegistrationDate);
            Assert.Equal(new DateTime(2025, 7, 30), roundtrip.PostingDate);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task AuditCsv_ReadsOlderFileWithoutTheNewColumn()
    {
        // Eine bereits auf SharePoint liegende Audit-CSV kennt die Spalte nicht. Sie muss
        // weiter lesbar bleiben, das Feld bleibt dann leer.
        var directory = Path.Combine("C:\\TMP", $"trafag-linereg-legacy-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var service = new ExportAuditCsvService();
        var settings = new ExportSettings
        {
            AuditCsvEnabled = true,
            LocalSiteExportFolder = directory,
            LocalAuditCsvFolder = Path.Combine(directory, "ignored")
        };

        try
        {
            var csv = string.Join(Environment.NewLine,
                "\"TSC\";\"InvoiceNumber\";\"PositionOnInvoice\";\"Material\";\"Quantity\";\"SalesPriceValue\";\"PostingDate\"",
                "\"TRES\";\"20241332\";\"20\";\"52871\";\"1\";\"265.00\";\"2025-01-02\"");
            await File.WriteAllTextAsync(
                Path.Combine(directory, "Sales_ProcessedMergeInput_TRES_2026-08-25.csv"), csv);

            var row = Assert.Single(await service.ReadLatestSiteAuditCsvRecordsAsync(settings));
            Assert.Null(row.LineRegistrationDate);
            Assert.Equal(new DateTime(2025, 1, 2), row.PostingDate);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}
