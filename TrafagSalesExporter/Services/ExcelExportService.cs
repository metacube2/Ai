using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public class ExcelExportService : IExcelExportService
{
    private readonly IDbContextFactory<AppDbContext>? _dbFactory;

    public ExcelExportService()
    {
    }

    public ExcelExportService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public string CreateExcelFile(string outputDirectory, string tsc, DateTime fileDate, List<SalesRecord> records)
    {
        Directory.CreateDirectory(outputDirectory);
        var fileName = $"Sales_{tsc}_{fileDate:yyyy-MM-dd}.xlsx";
        var fullPath = Path.Combine(outputDirectory, fileName);
        WriteWorkbookWithConfiguredRules(fullPath, records, includeFinanceHelpSheet: false);
        return fullPath;
    }

    public string CreateConsolidatedExcelFile(string outputDirectory, DateTime fileDate, List<SalesRecord> records)
    {
        Directory.CreateDirectory(outputDirectory);
        var fileName = $"Sales_All_{fileDate:yyyy-MM-dd}.xlsx";
        var fullPath = Path.Combine(outputDirectory, fileName);
        WriteWorkbookWithConfiguredRules(fullPath, records, includeFinanceHelpSheet: true);
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
        => WriteWorkbook(fullPath, records, includeFinanceHelpSheet, FinanceRuleEngine.CreateDefaultRules());

    private void WriteWorkbookWithConfiguredRules(string fullPath, List<SalesRecord> records, bool includeFinanceHelpSheet)
        => WriteWorkbook(fullPath, records, includeFinanceHelpSheet, LoadFinanceRules());

    private IReadOnlyList<FinanceRule> LoadFinanceRules()
    {
        if (_dbFactory is null)
            return FinanceRuleEngine.CreateDefaultRules();

        using var db = _dbFactory.CreateDbContext();
        var rules = db.FinanceRules
            .AsNoTracking()
            .Where(rule => rule.IsActive)
            .OrderBy(rule => rule.SortOrder)
            .ThenBy(rule => rule.Id)
            .ToList();

        return rules.Count == 0 ? FinanceRuleEngine.CreateDefaultRules() : rules;
    }

    private static void WriteWorkbook(string fullPath, List<SalesRecord> records, bool includeFinanceHelpSheet, IReadOnlyList<FinanceRule> financeRules)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Sales");
        var financeRuleEngine = new FinanceRuleEngine(financeRules);

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
            "Product Hierarchy Code",
            "Product Hierarchy Text",
            "Product Family Code",
            "Product Family Text",
            "Product Division Code",
            "Product Division Text",
            "Product Mapping Assigned",
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
            ws.Cell(row, 9).Value = record.ProductHierarchyCode;
            ws.Cell(row, 10).Value = record.ProductHierarchyText;
            ws.Cell(row, 11).Value = record.ProductFamilyCode;
            ws.Cell(row, 12).Value = record.ProductFamilyText;
            ws.Cell(row, 13).Value = record.ProductDivisionCode;
            ws.Cell(row, 14).Value = record.ProductDivisionText;
            ws.Cell(row, 15).Value = record.ProductMappingAssigned;
            ws.Cell(row, 16).Value = record.Quantity;
            ws.Cell(row, 17).Value = record.SupplierNumber;
            ws.Cell(row, 18).Value = record.SupplierName;
            ws.Cell(row, 19).Value = record.SupplierCountry;
            ws.Cell(row, 20).Value = record.CustomerNumber;
            ws.Cell(row, 21).Value = record.CustomerName;
            ws.Cell(row, 22).Value = record.CustomerCountry;
            ws.Cell(row, 23).Value = record.CustomerIndustry;
            ws.Cell(row, 24).Value = record.StandardCost;
            ws.Cell(row, 25).Value = record.StandardCostCurrency;
            ws.Cell(row, 26).Value = record.PurchaseOrderNumber;
            ws.Cell(row, 27).Value = record.SalesPriceValue;
            ws.Cell(row, 28).Value = record.SalesCurrency;
            ws.Cell(row, 29).Value = record.DocumentCurrency;
            ws.Cell(row, 30).Value = record.DocumentTotalForeignCurrency;
            ws.Cell(row, 31).Value = record.DocumentTotalLocalCurrency;
            ws.Cell(row, 32).Value = record.VatSumForeignCurrency;
            ws.Cell(row, 33).Value = record.VatSumLocalCurrency;
            ws.Cell(row, 34).Value = record.DocumentRate;
            ws.Cell(row, 35).Value = record.CompanyCurrency;
            ws.Cell(row, 36).Value = record.Incoterms2020;
            ws.Cell(row, 37).Value = record.SalesResponsibleEmployee;
            ws.Cell(row, 38).Value = record.PostingDate?.ToString("dd.MM.yyyy") ?? string.Empty;
            ws.Cell(row, 39).Value = record.InvoiceDate?.ToString("dd.MM.yyyy") ?? string.Empty;
            ws.Cell(row, 40).Value = record.OrderDate?.ToString("dd.MM.yyyy") ?? string.Empty;
            ws.Cell(row, 41).Value = record.Land;
            ws.Cell(row, 42).Value = record.DocumentType;
            var financeCountryKey = ResolveFinanceCountryKey(record.Land, record.Tsc);
            var financeDate = financeRuleEngine.ResolveFinanceDate(record, financeCountryKey);
            var financeInclude = financeRuleEngine.ShouldInclude(record, financeCountryKey);
            var financeNetSalesActual = financeRuleEngine.ResolveNetSalesActual(record, financeCountryKey, financeInclude);
            ws.Cell(row, 43).Value = financeDate.Year;
            ws.Cell(row, 44).Value = financeCountryKey;
            ws.Cell(row, 45).Value = financeDate.ToString("dd.MM.yyyy");
            ws.Cell(row, 46).Value = financeNetSalesActual;
            ws.Cell(row, 47).Value = ResolveFinanceCurrency(record);
            ws.Cell(row, 48).Value = financeInclude && financeNetSalesActual != 0m ? "TRUE" : "FALSE";
            ws.Cell(row, 49).Value = financeInclude
                ? "Sales Price/Value"
                : financeRuleEngine.ResolveExclusionReason(record, financeCountryKey);
            row++;
        }

        ws.Columns().AdjustToContents();
        if (includeFinanceHelpSheet)
        {
            AddFinanceSummarySheet(workbook, records, financeRules);
            AddFinanceDetailsSheet(workbook, records, financeRules);
            AddFinanceHelpSheet(workbook);
        }

        workbook.SaveAs(fullPath);
    }

    private static void AddFinanceSummarySheet(XLWorkbook workbook, List<SalesRecord> records, IReadOnlyList<FinanceRule> financeRules)
    {
        var ws = workbook.Worksheets.Add("Finance Summary");
        var financeRuleEngine = new FinanceRuleEngine(financeRules);
        ws.Position = 1;
        ws.Cell(1, 1).Value = "Finance Summary";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(2, 1).Value = "Diese Summen verwenden dieselbe Finance-Sicht wie die Spalten Finance | ... im Blatt Sales.";

        var headers = new[]
        {
            "Year",
            "Country Key",
            "Currency",
            "Included Rows",
            "Net Sales Actual",
            "Excluded Rows",
            "Hinweis"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(4, i + 1).Value = headers[i];
            ws.Cell(4, i + 1).Style.Font.Bold = true;
        }

        var summaryRows = records
            .Select(record =>
            {
                var countryKey = ResolveFinanceCountryKey(record.Land, record.Tsc);
                var financeDate = financeRuleEngine.ResolveFinanceDate(record, countryKey);
                var rawInclude = financeRuleEngine.ShouldInclude(record, countryKey);
                var value = financeRuleEngine.ResolveNetSalesActual(record, countryKey, rawInclude);
                var include = rawInclude && value != 0m;
                return new
                {
                    Year = financeDate.Year,
                    CountryKey = countryKey,
                    Currency = ResolveFinanceCurrency(record),
                    Include = include,
                    Value = value
                };
            })
            .GroupBy(row => new { row.Year, row.CountryKey, row.Currency })
            .OrderBy(group => group.Key.Year)
            .ThenBy(group => group.Key.CountryKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(group => group.Key.Currency, StringComparer.OrdinalIgnoreCase)
            .Select(group => new
            {
                group.Key.Year,
                group.Key.CountryKey,
                group.Key.Currency,
                IncludedRows = group.Count(row => row.Include),
                NetSalesActual = group.Sum(row => row.Value),
                ExcludedRows = group.Count(row => !row.Include)
            })
            .ToList();

        var rowIndex = 5;
        foreach (var row in summaryRows)
        {
            ws.Cell(rowIndex, 1).Value = row.Year;
            ws.Cell(rowIndex, 2).Value = row.CountryKey;
            ws.Cell(rowIndex, 3).Value = row.Currency;
            ws.Cell(rowIndex, 4).Value = row.IncludedRows;
            ws.Cell(rowIndex, 5).Value = row.NetSalesActual;
            ws.Cell(rowIndex, 6).Value = row.ExcludedRows;
            ws.Cell(rowIndex, 7).Value = BuildFinanceSummaryHint(row.CountryKey);
            rowIndex++;
        }

        ws.Column(5).Style.NumberFormat.Format = "#,##0.00";
        ws.Columns().AdjustToContents();
    }

    private static void AddFinanceDetailsSheet(XLWorkbook workbook, List<SalesRecord> records, IReadOnlyList<FinanceRule> financeRules)
    {
        var ws = workbook.Worksheets.Add("Finance Details");
        var financeRuleEngine = new FinanceRuleEngine(financeRules);
        ws.Position = 2;

        ws.Cell(1, 1).Value = "Finance Details";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(2, 1).Value = "Diese Zeilen fuehren zur Summe im Blatt Finance Summary. Summe ueber Net Sales Actual bilden.";

        var headers = new[]
        {
            "Year",
            "Country Key",
            "Currency",
            "Finance Date",
            "Net Sales Actual",
            "Source Value Field",
            "TSC",
            "Land",
            "Document Type",
            "Invoice Number",
            "Position on invoice",
            "Document Entry",
            "Material",
            "Name",
            "Quantity",
            "Customer number",
            "Customer name",
            "Customer country",
            "Supplier number",
            "Supplier name",
            "Supplier country",
            "posting date",
            "invoice date",
            "Sales Price/Value",
            "Sales Currency",
            "Document Currency",
            "Document Total FC",
            "Document Total LC",
            "Company Currency"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(4, i + 1).Value = headers[i];
            ws.Cell(4, i + 1).Style.Font.Bold = true;
        }

        var rowIndex = 5;
        foreach (var record in records)
        {
            var countryKey = ResolveFinanceCountryKey(record.Land, record.Tsc);
            var financeDate = financeRuleEngine.ResolveFinanceDate(record, countryKey);
            var rawInclude = financeRuleEngine.ShouldInclude(record, countryKey);
            var netSalesActual = financeRuleEngine.ResolveNetSalesActual(record, countryKey, rawInclude);
            var include = rawInclude && netSalesActual != 0m;

            if (!include)
                continue;

            ws.Cell(rowIndex, 1).Value = financeDate.Year;
            ws.Cell(rowIndex, 2).Value = countryKey;
            ws.Cell(rowIndex, 3).Value = ResolveFinanceCurrency(record);
            ws.Cell(rowIndex, 4).Value = financeDate.ToString("dd.MM.yyyy");
            ws.Cell(rowIndex, 5).Value = netSalesActual;
            ws.Cell(rowIndex, 6).Value = "Sales Price/Value";
            ws.Cell(rowIndex, 7).Value = record.Tsc;
            ws.Cell(rowIndex, 8).Value = record.Land;
            ws.Cell(rowIndex, 9).Value = record.DocumentType;
            ws.Cell(rowIndex, 10).Value = record.InvoiceNumber;
            ws.Cell(rowIndex, 11).Value = record.PositionOnInvoice;
            ws.Cell(rowIndex, 12).Value = record.DocumentEntry;
            ws.Cell(rowIndex, 13).Value = record.Material;
            ws.Cell(rowIndex, 14).Value = record.Name;
            ws.Cell(rowIndex, 15).Value = record.Quantity;
            ws.Cell(rowIndex, 16).Value = record.CustomerNumber;
            ws.Cell(rowIndex, 17).Value = record.CustomerName;
            ws.Cell(rowIndex, 18).Value = record.CustomerCountry;
            ws.Cell(rowIndex, 19).Value = record.SupplierNumber;
            ws.Cell(rowIndex, 20).Value = record.SupplierName;
            ws.Cell(rowIndex, 21).Value = record.SupplierCountry;
            ws.Cell(rowIndex, 22).Value = record.PostingDate?.ToString("dd.MM.yyyy") ?? string.Empty;
            ws.Cell(rowIndex, 23).Value = record.InvoiceDate?.ToString("dd.MM.yyyy") ?? string.Empty;
            ws.Cell(rowIndex, 24).Value = record.SalesPriceValue;
            ws.Cell(rowIndex, 25).Value = record.SalesCurrency;
            ws.Cell(rowIndex, 26).Value = record.DocumentCurrency;
            ws.Cell(rowIndex, 27).Value = record.DocumentTotalForeignCurrency;
            ws.Cell(rowIndex, 28).Value = record.DocumentTotalLocalCurrency;
            ws.Cell(rowIndex, 29).Value = record.CompanyCurrency;
            rowIndex++;
        }

        ws.Column(5).Style.NumberFormat.Format = "#,##0.00";
        ws.Column(24).Style.NumberFormat.Format = "#,##0.00";
        ws.Column(27).Style.NumberFormat.Format = "#,##0.00";
        ws.Column(28).Style.NumberFormat.Format = "#,##0.00";
        ws.Columns().AdjustToContents();
    }

    private static string BuildFinanceSummaryHint(string countryKey)
        => countryKey.ToUpperInvariant() switch
        {
            "DE" => "DE Alphaplan Jahresfile 2025: Weiterberechnungen ausgeschlossen; GS negativ, GS2510095 2024.",
            "IT" => "IT: Trafag Italia ausgeschlossen; doppelte Blank-Supplier-Zeilen nur einmal.",
            "UK" => "UK: Sage/Manual Excel, Credit Notes negativ.",
            "ES" => "ES: Sage CSV/Manual Excel, REC/Credit Notes negativ.",
            _ => string.Empty
        };

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
            ("Detailblatt", "Finance Details enthaelt nur die Zeilen, die zur Summe im Blatt Finance Summary fuehren."),
            ("Waehrung", "Finance | Currency zeigt die fuer den Finance-Abgleich fuehrende Hauswaehrung."),
            ("Datum", "Finance | Date verwendet PostingDate, danach InvoiceDate, danach ExtractionDate. DE Alphaplan wird als Jahresfile 2025 behandelt."),
            ("Wertquelle", "Finance | Source Value Field zeigt, aus welchem Rohfeld der Finance-Wert kommt."),
            ("DE-Sonderregel", "Fuer DE gilt die Deutschland-Rueckmeldung: Trafag AG und Magnetic Sense ausgeschlossen, GS-Gutschriften negativ, GS2510095 nicht in 2025."),
            ("IT-Sonderregel", "Fuer IT wird Trafag Italia im Finance-Wert ausgeschlossen; doppelte IT-Zeilen ohne Supplier country werden nur einmal gezaehlt."),
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
