using ClosedXML.Excel;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public class ExcelExportService : IExcelExportService
{
    public string CreateExcelFile(string outputDirectory, string tsc, DateTime fileDate, List<SalesRecord> records)
    {
        Directory.CreateDirectory(outputDirectory);
        var fileName = $"Sales_{tsc}_{fileDate:yyyy-MM-dd}.xlsx";
        var fullPath = Path.Combine(outputDirectory, fileName);
        WriteWorkbook(fullPath, records, includeFinanceHelpSheet: false);
        return fullPath;
    }

    public string CreateConsolidatedExcelFile(string outputDirectory, DateTime fileDate, List<SalesRecord> records)
    {
        Directory.CreateDirectory(outputDirectory);
        var fileName = $"Sales_All_{fileDate:yyyy-MM-dd}.xlsx";
        var fullPath = Path.Combine(outputDirectory, fileName);
        WriteWorkbook(fullPath, records, includeFinanceHelpSheet: true);
        return fullPath;
    }

    public string CreateGenericExcelFile(string outputDirectory, string filePrefix, DateTime fileDate, string worksheetName, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        Directory.CreateDirectory(outputDirectory);
        var safePrefix = string.IsNullOrWhiteSpace(filePrefix) ? "Export" : filePrefix.Trim();
        var fileName = $"{safePrefix}_{fileDate:yyyy-MM-dd}.xlsx";
        var fullPath = Path.Combine(outputDirectory, fileName);
        WriteGenericWorkbook(fullPath, worksheetName, rows);
        return fullPath;
    }

    private static void WriteWorkbook(string fullPath, List<SalesRecord> records, bool includeFinanceHelpSheet)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Sales");

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
            "Document Type",
            "Finance | Year",
            "Finance | Country Key",
            "Finance | Date",
            "Finance | Net Sales Actual",
            "Finance | Currency",
            "Finance | Include",
            "Finance | Source Value Field"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
        }

        var row = 2;
        foreach (var record in records)
        {
            ws.Cell(row, 1).Value = record.ExtractionDate.ToString("dd.MM.yyyy HH:mm:ss");
            ws.Cell(row, 2).Value = record.Tsc;
            ws.Cell(row, 3).Value = record.DocumentEntry;
            ws.Cell(row, 4).Value = record.InvoiceNumber;
            ws.Cell(row, 5).Value = record.PositionOnInvoice;
            ws.Cell(row, 6).Value = record.Material;
            ws.Cell(row, 7).Value = record.Name;
            ws.Cell(row, 8).Value = record.ProductGroup;
            ws.Cell(row, 9).Value = record.Quantity;
            ws.Cell(row, 10).Value = record.SupplierNumber;
            ws.Cell(row, 11).Value = record.SupplierName;
            ws.Cell(row, 12).Value = record.SupplierCountry;
            ws.Cell(row, 13).Value = record.CustomerNumber;
            ws.Cell(row, 14).Value = record.CustomerName;
            ws.Cell(row, 15).Value = record.CustomerCountry;
            ws.Cell(row, 16).Value = record.CustomerIndustry;
            ws.Cell(row, 17).Value = record.StandardCost;
            ws.Cell(row, 18).Value = record.StandardCostCurrency;
            ws.Cell(row, 19).Value = record.PurchaseOrderNumber;
            ws.Cell(row, 20).Value = record.SalesPriceValue;
            ws.Cell(row, 21).Value = record.SalesCurrency;
            ws.Cell(row, 22).Value = record.DocumentCurrency;
            ws.Cell(row, 23).Value = record.DocumentTotalForeignCurrency;
            ws.Cell(row, 24).Value = record.DocumentTotalLocalCurrency;
            ws.Cell(row, 25).Value = record.VatSumForeignCurrency;
            ws.Cell(row, 26).Value = record.VatSumLocalCurrency;
            ws.Cell(row, 27).Value = record.DocumentRate;
            ws.Cell(row, 28).Value = record.CompanyCurrency;
            ws.Cell(row, 29).Value = record.Incoterms2020;
            ws.Cell(row, 30).Value = record.SalesResponsibleEmployee;
            ws.Cell(row, 31).Value = record.PostingDate?.ToString("dd.MM.yyyy") ?? string.Empty;
            ws.Cell(row, 32).Value = record.InvoiceDate?.ToString("dd.MM.yyyy") ?? string.Empty;
            ws.Cell(row, 33).Value = record.OrderDate?.ToString("dd.MM.yyyy") ?? string.Empty;
            ws.Cell(row, 34).Value = record.Land;
            ws.Cell(row, 35).Value = record.DocumentType;
            var financeDate = ResolveFinanceDate(record);
            ws.Cell(row, 36).Value = financeDate.Year;
            ws.Cell(row, 37).Value = ResolveFinanceCountryKey(record.Land, record.Tsc);
            ws.Cell(row, 38).Value = financeDate.ToString("dd.MM.yyyy");
            ws.Cell(row, 39).Value = record.SalesPriceValue;
            ws.Cell(row, 40).Value = ResolveFinanceCurrency(record);
            ws.Cell(row, 41).Value = record.SalesPriceValue != 0m ? "TRUE" : "FALSE";
            ws.Cell(row, 42).Value = "Sales Price/Value";
            row++;
        }

        ws.Columns().AdjustToContents();
        if (includeFinanceHelpSheet)
            AddFinanceHelpSheet(workbook);

        workbook.SaveAs(fullPath);
    }

    private static void AddFinanceHelpSheet(XLWorkbook workbook)
    {
        var ws = workbook.Worksheets.Add("Finance Filter Hilfe");
        ws.Cell(1, 1).Value = "Finance-Filter fuer Soll/Ist-Abgleich";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;

        var rows = new (string Label, string Value)[]
        {
            ("Ziel", "Diese Spalten bilden im Blatt Sales die zusammengehoerige Finance-Sicht fuer den Abgleich gegen check.xlsx."),
            ("1. Jahr filtern", "Finance | Year = gewuenschtes Jahr, z.B. 2025"),
            ("2. Land filtern", "Finance | Country Key = CH, AT, DE, ES, FR, IN, IT, UK oder US"),
            ("3. Gueltige Zeilen filtern", "Finance | Include = TRUE"),
            ("4. Summe bilden", "Finance | Net Sales Actual summieren"),
            ("Waehrung", "Finance | Currency zeigt die fuer den Finance-Abgleich fuehrende Hauswaehrung."),
            ("Datum", "Finance | Date verwendet PostingDate, danach InvoiceDate, danach ExtractionDate."),
            ("Wertquelle", "Finance | Source Value Field zeigt, aus welchem Rohfeld der Finance-Wert kommt."),
            ("Nicht verwenden", "Nicht Land, TSC, Document Total LC oder andere Betragsspalten fuer den CFO-Abgleich erraten."),
            ("Hinweis", "Offene fachliche Differenzen bleiben sichtbar; diese Excel-Sicht soll die gleiche Ist-Summe wie das Testprogramm reproduzieren.")
        };

        ws.Cell(3, 1).Value = "Feld";
        ws.Cell(3, 2).Value = "Anwendung";
        ws.Range(3, 1, 3, 2).Style.Font.Bold = true;

        for (var i = 0; i < rows.Length; i++)
        {
            ws.Cell(i + 4, 1).Value = rows[i].Label;
            ws.Cell(i + 4, 2).Value = rows[i].Value;
        }

        var financeColumns = new[]
        {
            "Finance | Year",
            "Finance | Country Key",
            "Finance | Date",
            "Finance | Net Sales Actual",
            "Finance | Currency",
            "Finance | Include",
            "Finance | Source Value Field"
        };

        var startRow = rows.Length + 6;
        ws.Cell(startRow, 1).Value = "Zusammengehoerige Spalten im Blatt Sales";
        ws.Cell(startRow, 1).Style.Font.Bold = true;
        for (var i = 0; i < financeColumns.Length; i++)
        {
            ws.Cell(startRow + 1 + i, 1).Value = financeColumns[i];
        }

        ws.Columns().AdjustToContents();
    }

    private static DateTime ResolveFinanceDate(SalesRecord record)
        => record.PostingDate ?? record.InvoiceDate ?? record.ExtractionDate;

    private static string ResolveFinanceCurrency(SalesRecord record)
        => ResolveFinanceCountryKey(record.Land, record.Tsc) switch
        {
            "CH" => "CHF",
            "AT" => "EUR",
            "DE" => "EUR",
            "ES" => "EUR",
            "FR" => "EUR",
            "IN" => "INR",
            "IT" => "EUR",
            "UK" => "GBP",
            "US" => "USD",
            _ => string.IsNullOrWhiteSpace(record.CompanyCurrency) ? record.SalesCurrency : record.CompanyCurrency
        };

    private static string ResolveFinanceCountryKey(string land, string tsc)
    {
        var normalizedLand = (land ?? string.Empty).Trim().ToUpperInvariant();
        var normalizedTsc = (tsc ?? string.Empty).Trim().ToUpperInvariant();

        if (normalizedLand is "AT" or "AUT" || normalizedLand.Contains("OESTER") || normalizedLand.Contains("OSTER") || normalizedLand.Contains("AUSTRIA")) return "AT";
        if (normalizedLand is "CH" or "CHE" || normalizedLand.Contains("SCHWE") || normalizedLand.Contains("SWITZER")) return "CH";
        if (normalizedLand.Contains("FRANK") || normalizedTsc.Contains("FR")) return "FR";
        if (normalizedLand.Contains("IND") || normalizedTsc.Contains("IN")) return "IN";
        if (normalizedLand.Contains("ITAL") || normalizedTsc.Contains("IT")) return "IT";
        if (normalizedLand.Contains("ENGL") || normalizedLand.Contains("KINGDOM") || normalizedTsc.Contains("UK") || normalizedTsc.Contains("GB")) return "UK";
        if (normalizedLand.Contains("USA") || normalizedLand.Contains("UNITED STATES") || normalizedTsc.Contains("US")) return "US";
        if (normalizedLand.Contains("DEUT") || normalizedTsc.Contains("DE")) return "DE";
        if (normalizedLand.Contains("SPAN") || normalizedTsc is "SE" or "ES") return "ES";

        return normalizedTsc.Replace("TR", string.Empty);
    }

    private static void WriteGenericWorkbook(string fullPath, string worksheetName, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        using var workbook = new XLWorkbook();
        var sheetName = string.IsNullOrWhiteSpace(worksheetName) ? "Export" : worksheetName.Trim();
        var ws = workbook.Worksheets.Add(sheetName.Length > 31 ? sheetName[..31] : sheetName);

        var headers = rows
            .SelectMany(r => r.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        for (var i = 0; i < headers.Count; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
        }

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            for (var colIndex = 0; colIndex < headers.Count; colIndex++)
            {
                row.TryGetValue(headers[colIndex], out var value);
                ws.Cell(rowIndex + 2, colIndex + 1).Value = value?.ToString() ?? string.Empty;
            }
        }

        ws.Columns().AdjustToContents();
        workbook.SaveAs(fullPath);
    }
}
