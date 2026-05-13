using ClosedXML.Excel;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class ManualExcelImportServiceTests
{
    [Fact]
    public async Task ReadSalesRecordsAsync_Reads_Expected_Columns_From_Exporter_Format()
    {
        var site = new Site
        {
            TSC = "TRCH",
            Land = "Schweiz"
        };
        var filePath = CreateWorkbook(workbook =>
        {
            var ws = workbook.Worksheets.Add("Sales");
            WriteHeaders(ws);
            ws.Cell(2, 1).Value = "15.04.2026 13:45:00";
            ws.Cell(2, 2).Value = "TRDE";
            ws.Cell(2, 3).Value = 12345;
            ws.Cell(2, 4).Value = "INV-100";
            ws.Cell(2, 5).Value = 7;
            ws.Cell(2, 6).Value = "MAT-1";
            ws.Cell(2, 7).Value = "Pressure Sensor";
            ws.Cell(2, 8).Value = "PG-A";
            ws.Cell(2, 9).Value = 2.5m;
            ws.Cell(2, 10).Value = "SUP-1";
            ws.Cell(2, 11).Value = "Supplier";
            ws.Cell(2, 12).Value = "DE";
            ws.Cell(2, 13).Value = "CUST-1";
            ws.Cell(2, 14).Value = "Customer";
            ws.Cell(2, 15).Value = "CH";
            ws.Cell(2, 16).Value = "Industry";
            ws.Cell(2, 17).Value = 10.25m;
            ws.Cell(2, 18).Value = "EUR";
            ws.Cell(2, 19).Value = "PO-1";
            ws.Cell(2, 20).Value = 21.40m;
            ws.Cell(2, 21).Value = "EUR";
            ws.Cell(2, 22).Value = "EUR";
            ws.Cell(2, 23).Value = 120.50m;
            ws.Cell(2, 24).Value = 110.25m;
            ws.Cell(2, 25).Value = 8.10m;
            ws.Cell(2, 26).Value = 7.45m;
            ws.Cell(2, 27).Value = 1.0925m;
            ws.Cell(2, 28).Value = "CHF";
            ws.Cell(2, 29).Value = "DAP";
            ws.Cell(2, 30).Value = "Alice";
            ws.Cell(2, 31).Value = "13.04.2026";
            ws.Cell(2, 32).Value = "14.04.2026";
            ws.Cell(2, 33).Value = "10.04.2026";
            ws.Cell(2, 34).Value = "Deutschland";
            ws.Cell(2, 35).Value = "Invoice";
        });

        try
        {
            var service = new ManualExcelImportService();

            var rows = await service.ReadSalesRecordsAsync(filePath, site);

            var row = Assert.Single(rows);
            Assert.Equal("TRDE", row.Tsc);
            Assert.Equal(12345, row.DocumentEntry);
            Assert.Equal("INV-100", row.InvoiceNumber);
            Assert.Equal(7, row.PositionOnInvoice);
            Assert.Equal("MAT-1", row.Material);
            Assert.Equal(2.5m, row.Quantity);
            Assert.Equal(10.25m, row.StandardCost);
            Assert.Equal(21.40m, row.SalesPriceValue);
            Assert.Equal("EUR", row.DocumentCurrency);
            Assert.Equal(120.50m, row.DocumentTotalForeignCurrency);
            Assert.Equal(110.25m, row.DocumentTotalLocalCurrency);
            Assert.Equal(8.10m, row.VatSumForeignCurrency);
            Assert.Equal(7.45m, row.VatSumLocalCurrency);
            Assert.Equal(1.0925m, row.DocumentRate);
            Assert.Equal("CHF", row.CompanyCurrency);
            Assert.Equal("Deutschland", row.Land);
            Assert.Equal("Invoice", row.DocumentType);
            Assert.Equal(new DateTime(2026, 4, 13), row.PostingDate);
            Assert.Equal(new DateTime(2026, 4, 14), row.InvoiceDate);
            Assert.Equal(new DateTime(2026, 4, 10), row.OrderDate);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ReadSalesRecordsAsync_Uses_Site_Fallbacks_When_Tsc_And_Land_Are_Missing()
    {
        var site = new Site
        {
            TSC = "TRCH",
            Land = "Schweiz"
        };
        var filePath = CreateWorkbook(workbook =>
        {
            var ws = workbook.Worksheets.Add("Sales");
            WriteHeaders(ws);
            ws.Cell(2, 3).Value = "INV-200";
            ws.Cell(2, 6).Value = "MAT-2";
        });

        try
        {
            var service = new ManualExcelImportService();

            var rows = await service.ReadSalesRecordsAsync(filePath, site);

            var row = Assert.Single(rows);
            Assert.Equal("TRCH", row.Tsc);
            Assert.Equal("Schweiz", row.Land);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ReadSalesRecordsAsync_Parses_German_Decimal_Text_And_Skips_Empty_Rows()
    {
        var site = new Site
        {
            TSC = "TRAT",
            Land = "Oesterreich"
        };
        var filePath = CreateWorkbook(workbook =>
        {
            var ws = workbook.Worksheets.Add("Sales");
            WriteHeaders(ws);
            ws.Cell(2, 3).Value = "INV-300";
            ws.Cell(2, 9).Value = "1,50";
            ws.Cell(2, 17).Value = "3,25";
            ws.Cell(2, 20).Value = "7,90";
            ws.Cell(3, 1).Value = "";
            ws.Cell(3, 2).Value = "";
            ws.Cell(3, 3).Value = "";
        });

        try
        {
            var service = new ManualExcelImportService();

            var rows = await service.ReadSalesRecordsAsync(filePath, site);

            var row = Assert.Single(rows);
            Assert.Equal(1.50m, row.Quantity);
            Assert.Equal(3.25m, row.StandardCost);
            Assert.Equal(7.90m, row.SalesPriceValue);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ReadSalesRecordsAsync_Throws_When_InvoiceNumber_Header_Is_Missing()
    {
        var site = new Site
        {
            TSC = "TRCH",
            Land = "Schweiz"
        };
        var filePath = CreateWorkbook(workbook =>
        {
            var ws = workbook.Worksheets.Add("Sales");
            ws.Cell(1, 1).Value = "TSC";
            ws.Cell(1, 2).Value = "Material";
            ws.Cell(2, 1).Value = "TRCH";
            ws.Cell(2, 2).Value = "MAT-3";
        });

        try
        {
            var service = new ManualExcelImportService();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReadSalesRecordsAsync(filePath, site));

            Assert.Contains("Invoice Number", ex.Message);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ReadSalesRecordsAsync_Uses_Configured_Manual_Excel_Mapping_For_German_Headers()
    {
        var site = new Site
        {
            TSC = "TRDE",
            Land = "Deutschland"
        };
        var filePath = CreateWorkbook(workbook =>
        {
            var ws = workbook.Worksheets.Add("Sales");
            var headers = new[]
            {
                "Export-Datum", "Firma", "Belegnummer", "Position", "ArtikelBezeichnung",
                "Warengruppen-Bezeichnung", "Anz. VE", "Lieferanten Nummer", "Name Lieferant",
                "Land Lieferant", "AdressNummer-Kunde", "Name Kunde", "Land Kunde", "Branche",
                "EinstandsPreis", "Währung", "BestellNummer", "NettoPreisEinzelX",
                "NettoPreisGesamtX", "Versandbedingung", "AdressNummer_V", "Belegdatum-Rechnung",
                "BelegDatum Auftrag", "ArtikelNummer"
            };

            for (var i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            ws.Cell(2, 1).Value = "28.04.2026";
            ws.Cell(2, 3).Value = "RE2610536";
            ws.Cell(2, 5).Value = "Kommentar ohne Position";

            ws.Cell(3, 1).Value = "28.04.2026";
            ws.Cell(3, 3).Value = "RE2610536";
            ws.Cell(3, 4).Value = 10;
            ws.Cell(3, 5).Value = "Drucktransmitter NAR";
            ws.Cell(3, 6).Value = "Drucktransmitter";
            ws.Cell(3, 7).Value = 100;
            ws.Cell(3, 8).Value = "60000";
            ws.Cell(3, 9).Value = "Trafag AG";
            ws.Cell(3, 10).Value = "Schweiz";
            ws.Cell(3, 11).Value = "11264";
            ws.Cell(3, 12).Value = "Hanning & Kahl GmbH & Co KG";
            ws.Cell(3, 13).Value = "Deutschland";
            ws.Cell(3, 14).Value = "00 Bahn";
            ws.Cell(3, 15).Value = 55m;
            ws.Cell(3, 16).Value = "EUR";
            ws.Cell(3, 18).Value = 82.8m;
            ws.Cell(3, 19).Value = "8’280.00";
            ws.Cell(3, 20).Value = "ab Lager";
            ws.Cell(3, 21).Value = "JR";
            ws.Cell(3, 22).Value = "27.04.2026";
            ws.Cell(3, 23).Value = "09.03.2026";
            ws.Cell(3, 24).Value = "8258.85.2317/55441";
        });

        var mappings = new List<ManualExcelColumnMapping>
        {
            Map(nameof(SalesRecord.ExtractionDate), "Export-Datum"),
            Map(nameof(SalesRecord.InvoiceNumber), "Belegnummer"),
            Map(nameof(SalesRecord.PositionOnInvoice), "Position"),
            Map(nameof(SalesRecord.Material), "ArtikelNummer"),
            Map(nameof(SalesRecord.Name), "ArtikelBezeichnung"),
            Map(nameof(SalesRecord.ProductGroup), "Warengruppen-Bezeichnung"),
            Map(nameof(SalesRecord.Quantity), "Anz. VE"),
            Map(nameof(SalesRecord.SupplierNumber), "Lieferanten Nummer"),
            Map(nameof(SalesRecord.SupplierName), "Name Lieferant"),
            Map(nameof(SalesRecord.SupplierCountry), "Land Lieferant"),
            Map(nameof(SalesRecord.CustomerNumber), "AdressNummer-Kunde"),
            Map(nameof(SalesRecord.CustomerName), "Name Kunde"),
            Map(nameof(SalesRecord.CustomerCountry), "Land Kunde"),
            Map(nameof(SalesRecord.CustomerIndustry), "Branche"),
            Map(nameof(SalesRecord.StandardCost), "EinstandsPreis"),
            Map(nameof(SalesRecord.StandardCostCurrency), "Währung"),
            Map(nameof(SalesRecord.SalesPriceValue), "NettoPreisGesamtX"),
            Map(nameof(SalesRecord.SalesCurrency), "Währung"),
            Map(nameof(SalesRecord.DocumentCurrency), "Währung"),
            Map(nameof(SalesRecord.CompanyCurrency), "Währung"),
            Map(nameof(SalesRecord.Incoterms2020), "Versandbedingung"),
            Map(nameof(SalesRecord.SalesResponsibleEmployee), "AdressNummer_V"),
            Map(nameof(SalesRecord.PostingDate), "Belegdatum-Rechnung"),
            Map(nameof(SalesRecord.InvoiceDate), "Belegdatum-Rechnung"),
            Map(nameof(SalesRecord.OrderDate), "BelegDatum Auftrag"),
            Map(nameof(SalesRecord.DocumentType), "=Manual Excel")
        };

        try
        {
            var service = new ManualExcelImportService();

            var rows = await service.ReadSalesRecordsAsync(filePath, site, mappings);

            var row = Assert.Single(rows);
            Assert.Equal("TRDE", row.Tsc);
            Assert.Equal("Deutschland", row.Land);
            Assert.Equal("RE2610536", row.InvoiceNumber);
            Assert.Equal(10, row.PositionOnInvoice);
            Assert.Equal("8258.85.2317/55441", row.Material);
            Assert.Equal(100m, row.Quantity);
            Assert.Equal(55m, row.StandardCost);
            Assert.Equal(8280m, row.SalesPriceValue);
            Assert.Equal("EUR", row.SalesCurrency);
            Assert.Equal("EUR", row.DocumentCurrency);
            Assert.Equal("EUR", row.CompanyCurrency);
            Assert.Equal(new DateTime(2026, 4, 27), row.PostingDate);
            Assert.Equal(new DateTime(2026, 4, 27), row.InvoiceDate);
            Assert.Equal(new DateTime(2026, 3, 9), row.OrderDate);
            Assert.Equal("Manual Excel", row.DocumentType);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ReadSalesRecordsAsync_Reads_Sage_Spain_Csv_Format()
    {
        var site = new Site
        {
            TSC = "TRES",
            Land = "Spanien"
        };
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");
        var csv = string.Join(Environment.NewLine,
            "\"TSC\";\"Land\";\"InvoiceNumber\";\"PositionOnInvoice\";\"Material\";\"Name\";\"ProductGroup\";\"Quantity\";\"CustomerNumber\";\"CustomerName\";\"CustomerCountry\";\"StandardCost\";\"StandardCostCurrency\";\"PurchaseOrderNumber\";\"SalesPriceValue\";\"SalesCurrency\";\"DocumentCurrency\";\"CompanyCurrency\";\"Incoterms2020\";\"SalesResponsibleEmployee\";\"LineRegistrationDate\";\"InvoiceDate\";\"DocumentType\"",
            "\"TRES\";\"Spanien\";\"20241332\";\"20\";\"52871\";\"ECL1.0AP\";\"TRANS\";\"1.000000\";\"302208\";\"INTRONIK AUTOMATIZACION E INST. SL\";\"ESPANA\";\"160.760000\";\"EUR\";\"PC240330\";\"265.000000\";\"EUR\";\"EUR\";\"EUR\";\"EXW\";\"1\";\"2025-01-03 00:00:00\";\"2025-01-02 00:00:00\";\"Invoice\"");
        await File.WriteAllTextAsync(filePath, csv);

        try
        {
            var service = new ManualExcelImportService();

            var rows = await service.ReadSalesRecordsAsync(filePath, site);

            var row = Assert.Single(rows);
            Assert.Equal("TRES", row.Tsc);
            Assert.Equal("Spanien", row.Land);
            Assert.Equal("20241332", row.InvoiceNumber);
            Assert.Equal(20, row.PositionOnInvoice);
            Assert.Equal("52871", row.Material);
            Assert.Equal("TRANS", row.ProductGroup);
            Assert.Equal(1m, row.Quantity);
            Assert.Equal(160.760000m, row.StandardCost);
            Assert.Equal(265.000000m, row.SalesPriceValue);
            Assert.Equal("EUR", row.SalesCurrency);
            Assert.Equal("EUR", row.DocumentCurrency);
            Assert.Equal("EUR", row.CompanyCurrency);
            Assert.Equal(new DateTime(2025, 1, 3), row.PostingDate);
            Assert.Equal(new DateTime(2025, 1, 2), row.InvoiceDate);
            Assert.Equal("Invoice", row.DocumentType);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ReadSalesRecordsAsync_Evaluates_Mapped_Multiply_Expression()
    {
        var site = new Site
        {
            TSC = "TRUK",
            Land = "England"
        };
        var filePath = CreateWorkbook(workbook =>
        {
            var ws = workbook.Worksheets.Add("Sales");
            ws.Cell(1, 1).Value = "Invoice Number";
            ws.Cell(1, 2).Value = "Position on invoice";
            ws.Cell(1, 3).Value = "Quantity";
            ws.Cell(1, 4).Value = "Sales Price/Value";
            ws.Cell(1, 5).Value = "invoice date";
            ws.Cell(2, 1).Value = "42885";
            ws.Cell(2, 2).Value = 9;
            ws.Cell(2, 3).Value = 7;
            ws.Cell(2, 4).Value = 123.45m;
            ws.Cell(2, 5).Value = "18.11.2025";
        });

        var mappings = new List<ManualExcelColumnMapping>
        {
            Map(nameof(SalesRecord.InvoiceNumber), "Invoice Number"),
            Map(nameof(SalesRecord.PositionOnInvoice), "Position on invoice"),
            Map(nameof(SalesRecord.Quantity), "Quantity"),
            Map(nameof(SalesRecord.SalesPriceValue), "=[Sales Price/Value]*[Quantity]"),
            Map(nameof(SalesRecord.SalesCurrency), "=GBP"),
            Map(nameof(SalesRecord.CompanyCurrency), "=GBP"),
            Map(nameof(SalesRecord.PostingDate), "invoice date"),
            Map(nameof(SalesRecord.DocumentType), "=Manual Excel")
        };

        try
        {
            var service = new ManualExcelImportService();

            var rows = await service.ReadSalesRecordsAsync(filePath, site, mappings);

            var row = Assert.Single(rows);
            Assert.Equal(864.15m, row.SalesPriceValue);
            Assert.Equal(7m, row.Quantity);
            Assert.Equal("GBP", row.SalesCurrency);
            Assert.Equal("GBP", row.CompanyCurrency);
            Assert.Equal(new DateTime(2025, 11, 18), row.PostingDate);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static string CreateWorkbook(Action<XLWorkbook> fillWorkbook)
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xlsx");
        using var workbook = new XLWorkbook();
        fillWorkbook(workbook);
        workbook.SaveAs(filePath);
        return filePath;
    }

    private static void WriteHeaders(IXLWorksheet ws)
    {
        var headers = new[]
        {
            "extraction date",
            "TSC",
            "Document Entry",
            "Invoice Number",
            "Position on invoice",
            "Material",
            "Name",
            "Product Group",
            "Quantity",
            "Supplier number",
            "Supplier name",
            "Supplier country",
            "Customer number",
            "Customer name",
            "Customer country",
            "Customer Industry",
            "Standard cost",
            "Standard Cost Currency",
            "Purchase Order number",
            "Sales Price/Value",
            "Sales Currency",
            "Document Currency",
            "Document Total FC",
            "Document Total LC",
            "VAT Sum FC",
            "VAT Sum LC",
            "Document Rate",
            "Company Currency",
            "Incoterms 2020",
            "Sales responsible employee",
            "posting date",
            "invoice date",
            "order date",
            "Land",
            "Document Type"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
        }
    }

    private static ManualExcelColumnMapping Map(string targetField, string sourceHeader)
        => new()
        {
            TargetField = targetField,
            SourceHeader = sourceHeader,
            IsActive = true
        };
}
