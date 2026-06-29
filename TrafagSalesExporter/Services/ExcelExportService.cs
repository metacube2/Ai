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

    public string CreateDashboardProofExcelFile(string outputDirectory, DateTime fileDate, List<SalesRecord> records, bool useAuditCsvAsCentralSource, string? fileScope = null)
    {
        Directory.CreateDirectory(outputDirectory);
        var scopePart = BuildSafeFileScope(fileScope);
        var fileName = string.IsNullOrWhiteSpace(scopePart)
            ? $"Finance_Dashboard_Nachweis_{fileDate:yyyy-MM-dd}.xlsx"
            : $"Finance_Dashboard_Nachweis_{scopePart}_{fileDate:yyyy-MM-dd}.xlsx";
        var fullPath = Path.Combine(outputDirectory, fileName);
        WriteDashboardProofWorkbook(fullPath, records, fileDate, useAuditCsvAsCentralSource, LoadFinanceRules(), LoadFinanceReferences());
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

    private IReadOnlyList<FinanceReference> LoadFinanceReferences()
    {
        if (_dbFactory is null)
            return [];

        using var db = _dbFactory.CreateDbContext();
        return db.FinanceReferences
            .AsNoTracking()
            .Where(reference => reference.IsActive)
            .OrderBy(reference => reference.Year)
            .ThenBy(reference => reference.Key)
            .ToList();
    }

    private static string BuildSafeFileScope(string? fileScope)
    {
        if (string.IsNullOrWhiteSpace(fileScope))
            return string.Empty;

        var safe = new string(fileScope
            .Trim()
            .Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '_')
            .ToArray());
        return safe.Trim('_');
    }

    private static void WriteDashboardProofWorkbook(
        string fullPath,
        List<SalesRecord> records,
        DateTime fileDate,
        bool useAuditCsvAsCentralSource,
        IReadOnlyList<FinanceRule> financeRules,
        IReadOnlyList<FinanceReference> financeReferences)
    {
        using var workbook = new XLWorkbook();
        var financeRows = BuildFinanceProofRows(records, financeRules);
        var divisionRows = BuildDivisionProofRows(financeRows);
        var groupMarginRows = BuildGroupMarginProofRows(financeRows);

        AddProofDataLineageSheet(workbook, records, financeRows, fileDate, useAuditCsvAsCentralSource);
        AddProofFinanceSummarySheet(workbook, financeRows);
        AddProofFinanceDetailsSheet(workbook, financeRows);
        AddProofReferenceSheet(workbook, financeReferences, financeRows);
        AddProofDivisionSummarySheet(workbook, divisionRows);
        AddProofDivisionDetailsSheet(workbook, divisionRows);
        AddProofGroupMarginSummarySheet(workbook, groupMarginRows);
        AddProofGroupMarginDetailsSheet(workbook, groupMarginRows);
        AddProofDataQualitySheet(workbook);
        AddProofHelpSheet(workbook);

        workbook.SaveAs(fullPath);
    }

    private static List<FinanceProofRow> BuildFinanceProofRows(List<SalesRecord> records, IReadOnlyList<FinanceRule> financeRules)
    {
        var financeRuleEngine = new FinanceRuleEngine(financeRules);
        return records
            .Select(record =>
            {
                var countryKey = ResolveFinanceCountryKey(record.Land, record.Tsc);
                var financeDate = financeRuleEngine.ResolveFinanceDate(record, countryKey);
                var rawInclude = financeRuleEngine.ShouldInclude(record, countryKey);
                var netSalesActual = financeRuleEngine.ResolveNetSalesActual(record, countryKey, rawInclude);
                var include = rawInclude && netSalesActual != 0m;
                var exclusionReason = include ? string.Empty : financeRuleEngine.ResolveExclusionReason(record, countryKey);
                return new FinanceProofRow(
                    record,
                    financeDate.Year,
                    countryKey,
                    financeDate,
                    include,
                    netSalesActual,
                    ResolveFinanceCurrency(record),
                    include ? "Sales Price/Value" : exclusionReason);
            })
            .OrderBy(row => row.Year)
            .ThenBy(row => row.CountryKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.Currency, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.Record.Tsc, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.Record.InvoiceNumber, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.Record.PositionOnInvoice)
            .ToList();
    }

    private static List<DivisionProofRow> BuildDivisionProofRows(IReadOnlyCollection<FinanceProofRow> financeRows)
    {
        var referenceByMaterial = financeRows
            .Select(row => row.Record)
            .Where(record => !string.IsNullOrWhiteSpace(record.Material))
            .Where(HasProductReference)
            .GroupBy(record => NormalizeMaterialKey(record.Material), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(IsAssignedProductReference)
                    .ThenBy(record => record.Tsc, StringComparer.OrdinalIgnoreCase)
                    .First(),
                StringComparer.OrdinalIgnoreCase);

        return financeRows
            .Where(row => row.Include)
            .GroupBy(row => new
            {
                MaterialKey = NormalizeMaterialKey(row.Record.Material),
                row.Record.Material,
                row.Record.Name,
                row.Year,
                row.CountryKey,
                row.Record.Tsc,
                row.Currency
            })
            .Select(group =>
            {
                referenceByMaterial.TryGetValue(group.Key.MaterialKey, out var reference);
                var material = group.Key.Material?.Trim() ?? string.Empty;
                return new DivisionProofRow(
                    group.Key.Year,
                    BuildProductAssignmentStatus(material, reference),
                    group.Key.CountryKey,
                    group.Key.Tsc,
                    group.Key.Currency,
                    material,
                    group.Key.Name,
                    reference?.Material ?? string.Empty,
                    reference?.ProductDivisionCode ?? string.Empty,
                    reference?.ProductDivisionText ?? string.Empty,
                    reference?.ProductFamilyCode ?? string.Empty,
                    reference?.ProductFamilyText ?? string.Empty,
                    reference?.ProductHierarchyCode ?? string.Empty,
                    reference?.ProductHierarchyText ?? string.Empty,
                    group.Count(),
                    group.Sum(row => row.NetSalesActual));
            })
            .OrderBy(row => row.Year)
            .ThenBy(row => row.CountryKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => ProductAssignmentStatusSort(row.Status))
            .ThenByDescending(row => Math.Abs(row.NetSalesActual))
            .ThenBy(row => row.Material, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<GroupMarginProofRow> BuildGroupMarginProofRows(IEnumerable<FinanceProofRow> financeRows)
        => financeRows
            .Where(row => row.Include)
            .Select(row =>
            {
                var supplierType = ResolveSupplierType(row.Record);
                var costBasis = ResolveGroupMarginCostBasis(row.Record);
                var status = ResolveGroupMarginStatus(row.NetSalesActual, supplierType, costBasis);
                return new GroupMarginProofRow(
                    row.Year,
                    status,
                    row.CountryKey,
                    row.Record.Tsc,
                    row.Currency,
                    row.Record.InvoiceNumber,
                    row.Record.PositionOnInvoice,
                    row.Record.Material,
                    row.Record.Name,
                    row.Record.SupplierNumber,
                    row.Record.SupplierName,
                    row.Record.SupplierCountry,
                    supplierType,
                    ResolveGroupMarginCostSource(supplierType),
                    row.Record.Quantity,
                    row.Record.StandardCost,
                    row.NetSalesActual,
                    costBasis,
                    row.Record.ProductDivisionCode,
                    row.Record.ProductDivisionText);
            })
            .OrderBy(row => GroupMarginStatusSort(row.Status))
            .ThenBy(row => row.Year)
            .ThenBy(row => row.CountryKey, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(row => Math.Abs(row.SalesValue))
            .ToList();

    private static void AddProofDataLineageSheet(
        XLWorkbook workbook,
        IReadOnlyCollection<SalesRecord> records,
        IReadOnlyCollection<FinanceProofRow> financeRows,
        DateTime fileDate,
        bool useAuditCsvAsCentralSource)
    {
        var ws = workbook.Worksheets.Add("Datenherkunft");
        ws.Cell(1, 1).Value = "Dashboard Nachweis";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;

        var rows = new (string Label, object? Value)[]
        {
            ("Datei-Datum", fileDate.ToString("yyyy-MM-dd")),
            ("Erstellt am", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")),
            ("Zentrale Quelle", useAuditCsvAsCentralSource ? "Audit-CSV Sales_ProcessedMergeInput_*.csv" : "CentralSalesRecords"),
            ("Rohzeilen", records.Count),
            ("Finance Include TRUE", financeRows.Count(row => row.Include)),
            ("Finance Include FALSE", financeRows.Count(row => !row.Include)),
            ("Laender", string.Join(", ", financeRows.Select(row => row.CountryKey).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x))),
            ("Waehrungen", string.Join(", ", financeRows.Select(row => row.Currency).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x))),
            ("Hinweis", "Summary-Blaetter enthalten Excel-Formeln auf die Detailblaetter. Dashboard und Nachweis nutzen dieselbe FinanceRuleEngine.")
        };

        ws.Cell(3, 1).Value = "Feld";
        ws.Cell(3, 2).Value = "Wert";
        ws.Range(3, 1, 3, 2).Style.Font.Bold = true;
        for (var i = 0; i < rows.Length; i++)
        {
            ws.Cell(i + 4, 1).Value = rows[i].Label;
            ws.Cell(i + 4, 2).Value = rows[i].Value?.ToString() ?? string.Empty;
        }

        var rowIndex = rows.Length + 7;
        ws.Cell(rowIndex, 1).Value = "TSC";
        ws.Cell(rowIndex, 2).Value = "Land";
        ws.Cell(rowIndex, 3).Value = "Zeilen";
        ws.Cell(rowIndex, 4).Value = "Finance Include TRUE";
        ws.Cell(rowIndex, 5).Value = "Letzte ExtractionDate";
        ws.Range(rowIndex, 1, rowIndex, 5).Style.Font.Bold = true;
        rowIndex++;

        foreach (var group in financeRows.GroupBy(row => new { row.Record.Tsc, row.Record.Land }).OrderBy(group => group.Key.Tsc))
        {
            ws.Cell(rowIndex, 1).Value = group.Key.Tsc;
            ws.Cell(rowIndex, 2).Value = group.Key.Land;
            ws.Cell(rowIndex, 3).Value = group.Count();
            ws.Cell(rowIndex, 4).Value = group.Count(row => row.Include);
            ws.Cell(rowIndex, 5).Value = group.Max(row => row.Record.ExtractionDate).ToString("dd.MM.yyyy HH:mm:ss");
            rowIndex++;
        }

        FormatProofSheet(ws, rowIndex - 1, 5, autoFit: true);
    }

    private static void AddProofFinanceDetailsSheet(XLWorkbook workbook, IReadOnlyList<FinanceProofRow> rows)
    {
        var ws = workbook.Worksheets.Add("Finance Details");
        var headers = new[]
        {
            "Year", "Country Key", "Currency", "Finance Date", "Include", "Net Sales Actual", "Source Value Field",
            "TSC", "Land", "SourceSystem", "Document Type", "Invoice Number", "Position", "Document Entry",
            "Material", "Name", "Quantity", "Customer number", "Customer name", "Customer country",
            "Supplier number", "Supplier name", "Supplier country", "Posting Date", "Invoice Date",
            "Sales Price/Value", "Sales Currency", "Document Currency", "Document Total FC", "Document Total LC",
            "VAT Sum FC", "VAT Sum LC", "Company Currency", "Product Division Code", "Product Division Text",
            "Product Family Code", "Product Family Text", "Product Hierarchy Code", "Product Hierarchy Text",
            "Standard Cost", "Standard Cost Currency"
        };
        WriteHeaders(ws, headers);

        var rowIndex = 2;
        foreach (var row in rows)
        {
            var record = row.Record;
            ws.Cell(rowIndex, 1).Value = row.Year;
            ws.Cell(rowIndex, 2).Value = row.CountryKey;
            ws.Cell(rowIndex, 3).Value = row.Currency;
            ws.Cell(rowIndex, 4).Value = row.FinanceDate.ToString("dd.MM.yyyy");
            ws.Cell(rowIndex, 5).Value = row.Include ? "TRUE" : "FALSE";
            ws.Cell(rowIndex, 6).Value = row.NetSalesActual;
            ws.Cell(rowIndex, 7).Value = row.SourceValueField;
            ws.Cell(rowIndex, 8).Value = record.Tsc;
            ws.Cell(rowIndex, 9).Value = record.Land;
            ws.Cell(rowIndex, 10).Value = record.SourceSystem;
            ws.Cell(rowIndex, 11).Value = record.DocumentType;
            ws.Cell(rowIndex, 12).Value = record.InvoiceNumber;
            ws.Cell(rowIndex, 13).Value = record.PositionOnInvoice;
            ws.Cell(rowIndex, 14).Value = record.DocumentEntry;
            ws.Cell(rowIndex, 15).Value = record.Material;
            ws.Cell(rowIndex, 16).Value = record.Name;
            ws.Cell(rowIndex, 17).Value = record.Quantity;
            ws.Cell(rowIndex, 18).Value = record.CustomerNumber;
            ws.Cell(rowIndex, 19).Value = record.CustomerName;
            ws.Cell(rowIndex, 20).Value = record.CustomerCountry;
            ws.Cell(rowIndex, 21).Value = record.SupplierNumber;
            ws.Cell(rowIndex, 22).Value = record.SupplierName;
            ws.Cell(rowIndex, 23).Value = record.SupplierCountry;
            ws.Cell(rowIndex, 24).Value = record.PostingDate?.ToString("dd.MM.yyyy") ?? string.Empty;
            ws.Cell(rowIndex, 25).Value = record.InvoiceDate?.ToString("dd.MM.yyyy") ?? string.Empty;
            ws.Cell(rowIndex, 26).Value = record.SalesPriceValue;
            ws.Cell(rowIndex, 27).Value = record.SalesCurrency;
            ws.Cell(rowIndex, 28).Value = record.DocumentCurrency;
            ws.Cell(rowIndex, 29).Value = record.DocumentTotalForeignCurrency;
            ws.Cell(rowIndex, 30).Value = record.DocumentTotalLocalCurrency;
            ws.Cell(rowIndex, 31).Value = record.VatSumForeignCurrency;
            ws.Cell(rowIndex, 32).Value = record.VatSumLocalCurrency;
            ws.Cell(rowIndex, 33).Value = record.CompanyCurrency;
            ws.Cell(rowIndex, 34).Value = record.ProductDivisionCode;
            ws.Cell(rowIndex, 35).Value = record.ProductDivisionText;
            ws.Cell(rowIndex, 36).Value = record.ProductFamilyCode;
            ws.Cell(rowIndex, 37).Value = record.ProductFamilyText;
            ws.Cell(rowIndex, 38).Value = record.ProductHierarchyCode;
            ws.Cell(rowIndex, 39).Value = record.ProductHierarchyText;
            ws.Cell(rowIndex, 40).Value = record.StandardCost;
            ws.Cell(rowIndex, 41).Value = record.StandardCostCurrency;
            rowIndex++;
        }

        ws.Columns(6, 6).Style.NumberFormat.Format = "#,##0.00";
        ws.Columns(17, 17).Style.NumberFormat.Format = "#,##0.00";
        ws.Columns(26, 32).Style.NumberFormat.Format = "#,##0.00";
        ws.Columns(40, 40).Style.NumberFormat.Format = "#,##0.00";
        FormatProofSheet(ws, Math.Max(1, rowIndex - 1), headers.Length);
    }

    private static void AddProofFinanceSummarySheet(XLWorkbook workbook, IReadOnlyCollection<FinanceProofRow> rows)
    {
        var ws = workbook.Worksheets.Add("Finance Summary");
        var headers = new[] { "Year", "Country Key", "Currency", "Included Rows", "Net Sales Actual", "Excluded Rows" };
        WriteHeaders(ws, headers);
        var summaryRows = rows
            .GroupBy(row => new { row.Year, row.CountryKey, row.Currency })
            .OrderBy(group => group.Key.Year)
            .ThenBy(group => group.Key.CountryKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(group => group.Key.Currency, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var rowIndex = 2;
        foreach (var group in summaryRows)
        {
            ws.Cell(rowIndex, 1).Value = group.Key.Year;
            ws.Cell(rowIndex, 2).Value = group.Key.CountryKey;
            ws.Cell(rowIndex, 3).Value = group.Key.Currency;
            ws.Cell(rowIndex, 4).FormulaA1 = $"COUNTIFS('Finance Details'!$A:$A,A{rowIndex},'Finance Details'!$B:$B,B{rowIndex},'Finance Details'!$C:$C,C{rowIndex},'Finance Details'!$E:$E,\"TRUE\")";
            ws.Cell(rowIndex, 5).FormulaA1 = $"SUMIFS('Finance Details'!$F:$F,'Finance Details'!$A:$A,A{rowIndex},'Finance Details'!$B:$B,B{rowIndex},'Finance Details'!$C:$C,C{rowIndex},'Finance Details'!$E:$E,\"TRUE\")";
            ws.Cell(rowIndex, 6).FormulaA1 = $"COUNTIFS('Finance Details'!$A:$A,A{rowIndex},'Finance Details'!$B:$B,B{rowIndex},'Finance Details'!$C:$C,C{rowIndex},'Finance Details'!$E:$E,\"FALSE\")";
            rowIndex++;
        }

        ws.Columns(5, 5).Style.NumberFormat.Format = "#,##0.00";
        FormatProofSheet(ws, Math.Max(1, rowIndex - 1), headers.Length, autoFit: true);
    }

    private static void AddProofReferenceSheet(
        XLWorkbook workbook,
        IReadOnlyCollection<FinanceReference> references,
        IReadOnlyCollection<FinanceProofRow> financeRows)
    {
        var ws = workbook.Worksheets.Add("Soll Ist");
        var headers = new[] { "Year", "Country Key", "Label", "Reference", "Actual", "Difference", "Status", "Notes" };
        WriteHeaders(ws, headers);
        var referenceRows = references
            .Where(reference => financeRows.Any(row => row.Year == reference.Year) || reference.Year >= 2025)
            .OrderBy(reference => reference.Year)
            .ThenBy(reference => reference.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var rowIndex = 2;
        foreach (var reference in referenceRows)
        {
            ws.Cell(rowIndex, 1).Value = reference.Year;
            ws.Cell(rowIndex, 2).Value = reference.Key;
            ws.Cell(rowIndex, 3).Value = reference.Label;
            if ((reference.CheckValue ?? reference.LocalCurrencyValue).HasValue)
                ws.Cell(rowIndex, 4).Value = (reference.CheckValue ?? reference.LocalCurrencyValue)!.Value;
            ws.Cell(rowIndex, 5).FormulaA1 = $"SUMIFS('Finance Details'!$F:$F,'Finance Details'!$A:$A,A{rowIndex},'Finance Details'!$B:$B,B{rowIndex},'Finance Details'!$E:$E,\"TRUE\")";
            ws.Cell(rowIndex, 6).FormulaA1 = $"IF(D{rowIndex}=\"\",\"\",E{rowIndex}-D{rowIndex})";
            ws.Cell(rowIndex, 7).FormulaA1 = $"IF(D{rowIndex}=\"\",\"Keine Referenz\",IF(ABS(F{rowIndex})<=1,\"OK\",\"Pruefen\"))";
            ws.Cell(rowIndex, 8).Value = reference.Notes;
            rowIndex++;
        }

        ws.Columns(4, 6).Style.NumberFormat.Format = "#,##0.00";
        FormatProofSheet(ws, Math.Max(1, rowIndex - 1), headers.Length, autoFit: true);
    }

    private static void AddProofDivisionDetailsSheet(XLWorkbook workbook, IReadOnlyList<DivisionProofRow> rows)
    {
        var ws = workbook.Worksheets.Add("Sparten Details");
        var headers = new[]
        {
            "Year", "Status", "Country Key", "TSC", "Currency", "Material", "Name", "Reference Material",
            "Product Division Code", "Product Division Text", "Product Family Code", "Product Family Text",
            "Product Hierarchy Code", "Product Hierarchy Text", "Row Count", "Net Sales Actual"
        };
        WriteHeaders(ws, headers);

        var rowIndex = 2;
        foreach (var row in rows)
        {
            ws.Cell(rowIndex, 1).Value = row.Year;
            ws.Cell(rowIndex, 2).Value = row.Status;
            ws.Cell(rowIndex, 3).Value = row.CountryKey;
            ws.Cell(rowIndex, 4).Value = row.Tsc;
            ws.Cell(rowIndex, 5).Value = row.Currency;
            ws.Cell(rowIndex, 6).Value = row.Material;
            ws.Cell(rowIndex, 7).Value = row.ArticleName;
            ws.Cell(rowIndex, 8).Value = row.ReferenceMaterial;
            ws.Cell(rowIndex, 9).Value = row.ProductDivisionCode;
            ws.Cell(rowIndex, 10).Value = row.ProductDivisionText;
            ws.Cell(rowIndex, 11).Value = row.ProductFamilyCode;
            ws.Cell(rowIndex, 12).Value = row.ProductFamilyText;
            ws.Cell(rowIndex, 13).Value = row.ProductHierarchyCode;
            ws.Cell(rowIndex, 14).Value = row.ProductHierarchyText;
            ws.Cell(rowIndex, 15).Value = row.RowCount;
            ws.Cell(rowIndex, 16).Value = row.NetSalesActual;
            rowIndex++;
        }

        ws.Columns(16, 16).Style.NumberFormat.Format = "#,##0.00";
        FormatProofSheet(ws, Math.Max(1, rowIndex - 1), headers.Length);
    }

    private static void AddProofDivisionSummarySheet(XLWorkbook workbook, IReadOnlyCollection<DivisionProofRow> rows)
    {
        var ws = workbook.Worksheets.Add("Sparten Summary");
        var headers = new[] { "Year", "Country Key", "TSC", "Currency", "Product Division Code", "Product Division Text", "Net Sales Actual", "Row Count", "Detail Lines" };
        WriteHeaders(ws, headers);
        var summaryRows = rows
            .GroupBy(row => new { row.Year, row.CountryKey, row.Tsc, row.Currency, row.ProductDivisionCode, row.ProductDivisionText })
            .OrderBy(group => group.Key.Year)
            .ThenBy(group => group.Key.CountryKey, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(group => Math.Abs(group.Sum(row => row.NetSalesActual)))
            .ToList();

        var rowIndex = 2;
        foreach (var group in summaryRows)
        {
            ws.Cell(rowIndex, 1).Value = group.Key.Year;
            ws.Cell(rowIndex, 2).Value = group.Key.CountryKey;
            ws.Cell(rowIndex, 3).Value = group.Key.Tsc;
            ws.Cell(rowIndex, 4).Value = group.Key.Currency;
            ws.Cell(rowIndex, 5).Value = group.Key.ProductDivisionCode;
            ws.Cell(rowIndex, 6).Value = group.Key.ProductDivisionText;
            ws.Cell(rowIndex, 7).FormulaA1 = $"SUMIFS('Sparten Details'!$P:$P,'Sparten Details'!$A:$A,A{rowIndex},'Sparten Details'!$C:$C,B{rowIndex},'Sparten Details'!$D:$D,C{rowIndex},'Sparten Details'!$E:$E,D{rowIndex},'Sparten Details'!$I:$I,E{rowIndex})";
            ws.Cell(rowIndex, 8).FormulaA1 = $"SUMIFS('Sparten Details'!$O:$O,'Sparten Details'!$A:$A,A{rowIndex},'Sparten Details'!$C:$C,B{rowIndex},'Sparten Details'!$D:$D,C{rowIndex},'Sparten Details'!$E:$E,D{rowIndex},'Sparten Details'!$I:$I,E{rowIndex})";
            ws.Cell(rowIndex, 9).FormulaA1 = $"COUNTIFS('Sparten Details'!$A:$A,A{rowIndex},'Sparten Details'!$C:$C,B{rowIndex},'Sparten Details'!$D:$D,C{rowIndex},'Sparten Details'!$E:$E,D{rowIndex},'Sparten Details'!$I:$I,E{rowIndex})";
            rowIndex++;
        }

        ws.Columns(7, 7).Style.NumberFormat.Format = "#,##0.00";
        FormatProofSheet(ws, Math.Max(1, rowIndex - 1), headers.Length, autoFit: true);
    }

    private static void AddProofGroupMarginDetailsSheet(XLWorkbook workbook, IReadOnlyList<GroupMarginProofRow> rows)
    {
        var ws = workbook.Worksheets.Add("Gruppenmarge Details");
        var headers = new[]
        {
            "Year", "Status", "Country Key", "TSC", "Currency", "Invoice Number", "Position", "Material", "Name",
            "Supplier Number", "Supplier Name", "Supplier Country", "Supplier Type", "Cost Source", "Quantity",
            "Unit Cost", "Sales Value", "Known Cost Basis", "Margin Value", "Margin %", "Product Division Code",
            "Product Division Text"
        };
        WriteHeaders(ws, headers);

        var rowIndex = 2;
        foreach (var row in rows)
        {
            ws.Cell(rowIndex, 1).Value = row.Year;
            ws.Cell(rowIndex, 2).Value = row.Status;
            ws.Cell(rowIndex, 3).Value = row.CountryKey;
            ws.Cell(rowIndex, 4).Value = row.Tsc;
            ws.Cell(rowIndex, 5).Value = row.Currency;
            ws.Cell(rowIndex, 6).Value = row.InvoiceNumber;
            ws.Cell(rowIndex, 7).Value = row.PositionOnInvoice;
            ws.Cell(rowIndex, 8).Value = row.Material;
            ws.Cell(rowIndex, 9).Value = row.ArticleName;
            ws.Cell(rowIndex, 10).Value = row.SupplierNumber;
            ws.Cell(rowIndex, 11).Value = row.SupplierName;
            ws.Cell(rowIndex, 12).Value = row.SupplierCountry;
            ws.Cell(rowIndex, 13).Value = row.SupplierType;
            ws.Cell(rowIndex, 14).Value = row.CostSource;
            ws.Cell(rowIndex, 15).Value = row.Quantity;
            ws.Cell(rowIndex, 16).Value = row.UnitCost;
            ws.Cell(rowIndex, 17).Value = row.SalesValue;
            ws.Cell(rowIndex, 18).Value = row.CostBasisValue;
            ws.Cell(rowIndex, 19).FormulaA1 = $"IF(B{rowIndex}=\"OK\",Q{rowIndex}-R{rowIndex},\"\")";
            ws.Cell(rowIndex, 20).FormulaA1 = $"IF(B{rowIndex}=\"OK\",IF(Q{rowIndex}=0,\"\",S{rowIndex}/Q{rowIndex}),\"\")";
            ws.Cell(rowIndex, 21).Value = row.ProductDivisionCode;
            ws.Cell(rowIndex, 22).Value = row.ProductDivisionText;
            rowIndex++;
        }

        ws.Columns(15, 19).Style.NumberFormat.Format = "#,##0.00";
        ws.Columns(20, 20).Style.NumberFormat.Format = "0.0%";
        FormatProofSheet(ws, Math.Max(1, rowIndex - 1), headers.Length);
    }

    private static void AddProofGroupMarginSummarySheet(XLWorkbook workbook, IReadOnlyCollection<GroupMarginProofRow> rows)
    {
        var ws = workbook.Worksheets.Add("Gruppenmarge Summary");
        var headers = new[]
        {
            "Year", "Country Key", "TSC", "Currency", "Sales Value", "Known Cost Basis", "Open Cost Rows",
            "Internal Rows", "External Rows", "Margin Value", "Margin %"
        };
        WriteHeaders(ws, headers);
        var summaryRows = rows
            .GroupBy(row => new { row.Year, row.CountryKey, row.Tsc, row.Currency })
            .OrderBy(group => group.Key.Year)
            .ThenBy(group => group.Key.CountryKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(group => group.Key.Tsc, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var rowIndex = 2;
        foreach (var group in summaryRows)
        {
            ws.Cell(rowIndex, 1).Value = group.Key.Year;
            ws.Cell(rowIndex, 2).Value = group.Key.CountryKey;
            ws.Cell(rowIndex, 3).Value = group.Key.Tsc;
            ws.Cell(rowIndex, 4).Value = group.Key.Currency;
            ws.Cell(rowIndex, 5).FormulaA1 = $"SUMIFS('Gruppenmarge Details'!$Q:$Q,'Gruppenmarge Details'!$A:$A,A{rowIndex},'Gruppenmarge Details'!$C:$C,B{rowIndex},'Gruppenmarge Details'!$D:$D,C{rowIndex},'Gruppenmarge Details'!$E:$E,D{rowIndex})";
            ws.Cell(rowIndex, 6).FormulaA1 = $"SUMIFS('Gruppenmarge Details'!$R:$R,'Gruppenmarge Details'!$A:$A,A{rowIndex},'Gruppenmarge Details'!$C:$C,B{rowIndex},'Gruppenmarge Details'!$D:$D,C{rowIndex},'Gruppenmarge Details'!$E:$E,D{rowIndex})";
            ws.Cell(rowIndex, 7).FormulaA1 = $"COUNTIFS('Gruppenmarge Details'!$A:$A,A{rowIndex},'Gruppenmarge Details'!$C:$C,B{rowIndex},'Gruppenmarge Details'!$D:$D,C{rowIndex},'Gruppenmarge Details'!$E:$E,D{rowIndex},'Gruppenmarge Details'!$B:$B,\"Standardpreis fehlt\")+COUNTIFS('Gruppenmarge Details'!$A:$A,A{rowIndex},'Gruppenmarge Details'!$C:$C,B{rowIndex},'Gruppenmarge Details'!$D:$D,C{rowIndex},'Gruppenmarge Details'!$E:$E,D{rowIndex},'Gruppenmarge Details'!$B:$B,\"Lieferant unklar\")";
            ws.Cell(rowIndex, 8).FormulaA1 = $"COUNTIFS('Gruppenmarge Details'!$A:$A,A{rowIndex},'Gruppenmarge Details'!$C:$C,B{rowIndex},'Gruppenmarge Details'!$D:$D,C{rowIndex},'Gruppenmarge Details'!$E:$E,D{rowIndex},'Gruppenmarge Details'!$M:$M,\"Intern\")";
            ws.Cell(rowIndex, 9).FormulaA1 = $"COUNTIFS('Gruppenmarge Details'!$A:$A,A{rowIndex},'Gruppenmarge Details'!$C:$C,B{rowIndex},'Gruppenmarge Details'!$D:$D,C{rowIndex},'Gruppenmarge Details'!$E:$E,D{rowIndex},'Gruppenmarge Details'!$M:$M,\"Extern\")";
            ws.Cell(rowIndex, 10).FormulaA1 = $"IF(G{rowIndex}>0,\"\",E{rowIndex}-F{rowIndex})";
            ws.Cell(rowIndex, 11).FormulaA1 = $"IF(G{rowIndex}>0,\"\",IF(E{rowIndex}=0,\"\",J{rowIndex}/E{rowIndex}))";
            rowIndex++;
        }

        ws.Columns(5, 6).Style.NumberFormat.Format = "#,##0.00";
        ws.Columns(10, 10).Style.NumberFormat.Format = "#,##0.00";
        ws.Columns(11, 11).Style.NumberFormat.Format = "0.0%";
        FormatProofSheet(ws, Math.Max(1, rowIndex - 1), headers.Length, autoFit: true);
    }

    private static void AddProofDataQualitySheet(XLWorkbook workbook)
    {
        var ws = workbook.Worksheets.Add("Datenqualitaet");
        var headers = new[] { "Check", "Count", "Hinweis" };
        WriteHeaders(ws, headers);
        var rows = new (string Check, string Formula, string Note)[]
        {
            ("Finance Detailzeilen", "COUNTA('Finance Details'!$A:$A)-1", "Alle Detailzeilen im Nachweis."),
            ("Finance Include TRUE", "COUNTIF('Finance Details'!$E:$E,\"TRUE\")", "Zeilen, die in Finance Summary eingehen."),
            ("Finance Include FALSE", "COUNTIF('Finance Details'!$E:$E,\"FALSE\")", "Ausgeschlossene oder Nullwert-Zeilen."),
            ("Fehlende Materialnummer", "COUNTIFS('Finance Details'!$O:$O,\"\",'Finance Details'!$A:$A,\"<>\")", "Erschwert Spartenanalyse."),
            ("Fehlender Lieferant", "COUNTIFS('Finance Details'!$U:$U,\"\",'Finance Details'!$V:$V,\"\",'Finance Details'!$W:$W,\"\",'Finance Details'!$A:$A,\"<>\")", "Erschwert Gruppenmarge."),
            ("StandardCost = 0", "COUNTIFS('Finance Details'!$AN:$AN,0,'Finance Details'!$E:$E,\"TRUE\")", "Erschwert Gruppenmarge."),
            ("Sparten nicht im TR-AG-Stamm", "COUNTIF('Sparten Details'!$B:$B,\"Nicht im TR-AG-Stamm\")", "Lokales Material ohne zentrale Referenz."),
            ("Sparten Material fehlt", "COUNTIF('Sparten Details'!$B:$B,\"Material fehlt\")", "Finance-Zeile ohne Materialnummer."),
            ("Gruppenmarge offene Kostenbasis", "COUNTIF('Gruppenmarge Details'!$B:$B,\"Standardpreis fehlt\")+COUNTIF('Gruppenmarge Details'!$B:$B,\"Lieferant unklar\")", "Marge ist fuer diese Zeilen nicht belastbar.")
        };

        for (var i = 0; i < rows.Length; i++)
        {
            var rowIndex = i + 2;
            ws.Cell(rowIndex, 1).Value = rows[i].Check;
            ws.Cell(rowIndex, 2).FormulaA1 = rows[i].Formula;
            ws.Cell(rowIndex, 3).Value = rows[i].Note;
        }

        FormatProofSheet(ws, rows.Length + 1, headers.Length, autoFit: true);
    }

    private static void AddProofHelpSheet(XLWorkbook workbook)
    {
        var ws = workbook.Worksheets.Add("Formel Hilfe");
        ws.Cell(1, 1).Value = "Finance Dashboard Nachweis";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        var rows = new (string Sheet, string Purpose)[]
        {
            ("Finance Details", "Alle zentralen Detailzeilen mit Finance-Regeln, Include-Flag und Net Sales Actual."),
            ("Finance Summary", "SUMIFS/COUNTIFS ueber Finance Details."),
            ("Soll Ist", "SUMIFS gegen Finance Details und Vergleich mit gepflegten FinanceReferences/check.xlsx-Werten."),
            ("Sparten Details", "Material-/Land-Sicht mit TR-AG-Referenzstatus."),
            ("Sparten Summary", "SUMIFS/COUNTIFS ueber Sparten Details."),
            ("Gruppenmarge Details", "Zeilenbasis fuer bekannte Kostenbasis und offene Kostenbasis."),
            ("Gruppenmarge Summary", "SUMIFS/COUNTIFS ueber Gruppenmarge Details; Marge bleibt leer, wenn offene Kostenbasis vorhanden ist."),
            ("Datenqualitaet", "COUNTIF/COUNTIFS-Pruefungen auf die Detailblaetter.")
        };

        ws.Cell(3, 1).Value = "Blatt";
        ws.Cell(3, 2).Value = "Erklaerung";
        ws.Range(3, 1, 3, 2).Style.Font.Bold = true;
        for (var i = 0; i < rows.Length; i++)
        {
            ws.Cell(i + 4, 1).Value = rows[i].Sheet;
            ws.Cell(i + 4, 2).Value = rows[i].Purpose;
        }
        FormatProofSheet(ws, rows.Length + 3, 2, autoFit: true);
    }

    private static void WriteHeaders(IXLWorksheet ws, IReadOnlyList<string> headers)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
        }
    }

    private static void FormatProofSheet(IXLWorksheet ws, int lastRow, int lastColumn, bool autoFit = false)
    {
        ws.SheetView.FreezeRows(1);
        if (lastRow >= 1 && lastColumn >= 1)
            ws.Range(1, 1, lastRow, lastColumn).SetAutoFilter();
        if (autoFit)
        {
            ws.Columns(1, lastColumn).AdjustToContents();
        }
        else
        {
            ws.Columns(1, lastColumn).Width = 16;
            ws.Column(1).Width = 10;
        }
    }

    private static bool HasProductReference(SalesRecord record)
        => !string.IsNullOrWhiteSpace(record.ProductHierarchyCode) ||
           !string.IsNullOrWhiteSpace(record.ProductFamilyCode) ||
           !string.IsNullOrWhiteSpace(record.ProductDivisionCode) ||
           !string.IsNullOrWhiteSpace(record.ProductMappingAssigned);

    private static bool IsAssignedProductReference(SalesRecord record)
        => IsTruthy(record.ProductMappingAssigned) &&
           !string.IsNullOrWhiteSpace(record.ProductDivisionCode) &&
           !record.ProductDivisionCode.Equals("UNASS", StringComparison.OrdinalIgnoreCase);

    private static bool IsMiscProductDivision(SalesRecord record)
        => record.ProductDivisionCode.Equals("0008", StringComparison.OrdinalIgnoreCase);

    private static string BuildProductAssignmentStatus(string material, SalesRecord? reference)
    {
        if (string.IsNullOrWhiteSpace(material))
            return "Material fehlt";
        if (reference is null)
            return "Nicht im TR-AG-Stamm";
        if (IsMiscProductDivision(reference))
            return "Uebrige";
        return IsAssignedProductReference(reference) ? "Zugeordnet" : "Nicht zugeordnet";
    }

    private static int ProductAssignmentStatusSort(string status)
        => status switch
        {
            "Nicht im TR-AG-Stamm" => 0,
            "Nicht zugeordnet" => 1,
            "Material fehlt" => 2,
            "Uebrige" => 3,
            "Zugeordnet" => 4,
            _ => 5
        };

    private static bool IsTruthy(string value)
    {
        var normalized = (value ?? string.Empty).Trim().ToUpperInvariant();
        return normalized is "X" or "TRUE" or "1" or "Y" or "YES";
    }

    private static string NormalizeMaterialKey(string value)
    {
        var normalized = new string((value ?? string.Empty)
            .Trim()
            .ToUpperInvariant()
            .Where(ch => !char.IsWhiteSpace(ch))
            .ToArray());
        var withoutLeadingZeros = normalized.TrimStart('0');
        return string.IsNullOrWhiteSpace(withoutLeadingZeros) ? normalized : withoutLeadingZeros;
    }

    private static string ResolveSupplierType(SalesRecord record)
        => GroupMarginSupplierClassifier.Resolve(record.SupplierNumber, record.SupplierName, record.SupplierCountry);

    private static decimal ResolveGroupMarginCostBasis(SalesRecord record)
        => record.Quantity != 0m ? Math.Abs(record.Quantity) * Math.Abs(record.StandardCost) : Math.Abs(record.StandardCost);

    private static string ResolveGroupMarginCostSource(string supplierType)
        => supplierType switch
        {
            "Intern" => "Interner Standardpreis",
            "Extern" => "Kosten aus Verkaufszeile",
            _ => "Lieferant unklar"
        };

    private static string ResolveGroupMarginStatus(decimal salesValue, string supplierType, decimal costBasis)
    {
        if (supplierType == "Unklar")
            return "Lieferant unklar";
        if (costBasis == 0m)
            return "Standardpreis fehlt";
        if (salesValue == 0m)
            return "Umsatz fehlt";
        return "OK";
    }

    private static int GroupMarginStatusSort(string status)
        => status switch
        {
            "Standardpreis fehlt" => 0,
            "Lieferant unklar" => 1,
            "Umsatz fehlt" => 2,
            _ => 3
        };

    private sealed record FinanceProofRow(
        SalesRecord Record,
        int Year,
        string CountryKey,
        DateTime FinanceDate,
        bool Include,
        decimal NetSalesActual,
        string Currency,
        string SourceValueField);

    private sealed record DivisionProofRow(
        int Year,
        string Status,
        string CountryKey,
        string Tsc,
        string Currency,
        string Material,
        string ArticleName,
        string ReferenceMaterial,
        string ProductDivisionCode,
        string ProductDivisionText,
        string ProductFamilyCode,
        string ProductFamilyText,
        string ProductHierarchyCode,
        string ProductHierarchyText,
        int RowCount,
        decimal NetSalesActual);

    private sealed record GroupMarginProofRow(
        int Year,
        string Status,
        string CountryKey,
        string Tsc,
        string Currency,
        string InvoiceNumber,
        int PositionOnInvoice,
        string Material,
        string ArticleName,
        string SupplierNumber,
        string SupplierName,
        string SupplierCountry,
        string SupplierType,
        string CostSource,
        decimal Quantity,
        decimal UnitCost,
        decimal SalesValue,
        decimal CostBasisValue,
        string ProductDivisionCode,
        string ProductDivisionText);

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
