using System.Globalization;
using ClosedXML.Excel;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public class ManualExcelImportService : IManualExcelImportService
{
    private static readonly Dictionary<string, string> HeaderMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["extractiondate"] = nameof(SalesRecord.ExtractionDate),
        ["tsc"] = nameof(SalesRecord.Tsc),
        ["invoicenumber"] = nameof(SalesRecord.InvoiceNumber),
        ["positiononinvoice"] = nameof(SalesRecord.PositionOnInvoice),
        ["material"] = nameof(SalesRecord.Material),
        ["name"] = nameof(SalesRecord.Name),
        ["productgroup"] = nameof(SalesRecord.ProductGroup),
        ["quantity"] = nameof(SalesRecord.Quantity),
        ["suppliernumber"] = nameof(SalesRecord.SupplierNumber),
        ["suppliername"] = nameof(SalesRecord.SupplierName),
        ["suppliercountry"] = nameof(SalesRecord.SupplierCountry),
        ["customernumber"] = nameof(SalesRecord.CustomerNumber),
        ["customername"] = nameof(SalesRecord.CustomerName),
        ["customercountry"] = nameof(SalesRecord.CustomerCountry),
        ["customerindustry"] = nameof(SalesRecord.CustomerIndustry),
        ["standardcost"] = nameof(SalesRecord.StandardCost),
        ["standardcostcurrency"] = nameof(SalesRecord.StandardCostCurrency),
        ["purchaseordernumber"] = nameof(SalesRecord.PurchaseOrderNumber),
        ["salespricevalue"] = nameof(SalesRecord.SalesPriceValue),
        ["salescurrency"] = nameof(SalesRecord.SalesCurrency),
        ["incoterms2020"] = nameof(SalesRecord.Incoterms2020),
        ["salesresponsibleemployee"] = nameof(SalesRecord.SalesResponsibleEmployee),
        ["invoicedate"] = nameof(SalesRecord.InvoiceDate),
        ["orderdate"] = nameof(SalesRecord.OrderDate),
        ["land"] = nameof(SalesRecord.Land),
        ["documenttype"] = nameof(SalesRecord.DocumentType)
    };

    public Task<List<SalesRecord>> ReadSalesRecordsAsync(string filePath, Site site)
    {
        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidOperationException("Die Excel-Datei enthält kein Arbeitsblatt.");
        var usedRange = worksheet.RangeUsed()
            ?? throw new InvalidOperationException("Die Excel-Datei enthält keine Daten.");

        var headerRow = usedRange.FirstRow();
        var headerIndexes = BuildHeaderIndexMap(headerRow);
        var rows = new List<SalesRecord>();

        foreach (var row in usedRange.RowsUsed().Skip(1))
        {
            if (IsRowEmpty(row))
                continue;

            rows.Add(new SalesRecord
            {
                ExtractionDate = ReadDate(headerIndexes, row, nameof(SalesRecord.ExtractionDate)) ?? DateTime.UtcNow,
                Tsc = ReadString(headerIndexes, row, nameof(SalesRecord.Tsc), site.TSC),
                InvoiceNumber = ReadString(headerIndexes, row, nameof(SalesRecord.InvoiceNumber)),
                PositionOnInvoice = (int)Math.Round(ReadDecimal(headerIndexes, row, nameof(SalesRecord.PositionOnInvoice))),
                Material = ReadString(headerIndexes, row, nameof(SalesRecord.Material)),
                Name = ReadString(headerIndexes, row, nameof(SalesRecord.Name)),
                ProductGroup = ReadString(headerIndexes, row, nameof(SalesRecord.ProductGroup)),
                Quantity = ReadDecimal(headerIndexes, row, nameof(SalesRecord.Quantity)),
                SupplierNumber = ReadString(headerIndexes, row, nameof(SalesRecord.SupplierNumber)),
                SupplierName = ReadString(headerIndexes, row, nameof(SalesRecord.SupplierName)),
                SupplierCountry = ReadString(headerIndexes, row, nameof(SalesRecord.SupplierCountry)),
                CustomerNumber = ReadString(headerIndexes, row, nameof(SalesRecord.CustomerNumber)),
                CustomerName = ReadString(headerIndexes, row, nameof(SalesRecord.CustomerName)),
                CustomerCountry = ReadString(headerIndexes, row, nameof(SalesRecord.CustomerCountry)),
                CustomerIndustry = ReadString(headerIndexes, row, nameof(SalesRecord.CustomerIndustry)),
                StandardCost = ReadDecimal(headerIndexes, row, nameof(SalesRecord.StandardCost)),
                StandardCostCurrency = ReadString(headerIndexes, row, nameof(SalesRecord.StandardCostCurrency)),
                PurchaseOrderNumber = ReadString(headerIndexes, row, nameof(SalesRecord.PurchaseOrderNumber)),
                SalesPriceValue = ReadDecimal(headerIndexes, row, nameof(SalesRecord.SalesPriceValue)),
                SalesCurrency = ReadString(headerIndexes, row, nameof(SalesRecord.SalesCurrency)),
                Incoterms2020 = ReadString(headerIndexes, row, nameof(SalesRecord.Incoterms2020)),
                SalesResponsibleEmployee = ReadString(headerIndexes, row, nameof(SalesRecord.SalesResponsibleEmployee)),
                InvoiceDate = ReadDate(headerIndexes, row, nameof(SalesRecord.InvoiceDate)),
                OrderDate = ReadDate(headerIndexes, row, nameof(SalesRecord.OrderDate)),
                Land = ReadString(headerIndexes, row, nameof(SalesRecord.Land), site.Land),
                DocumentType = ReadString(headerIndexes, row, nameof(SalesRecord.DocumentType))
            });
        }

        return Task.FromResult(rows);
    }

    private static Dictionary<string, int> BuildHeaderIndexMap(IXLRangeRow headerRow)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var cell in headerRow.CellsUsed())
        {
            var normalizedHeader = NormalizeHeader(cell.GetString());
            if (string.IsNullOrWhiteSpace(normalizedHeader))
                continue;

            if (HeaderMap.TryGetValue(normalizedHeader, out var targetField))
                result[targetField] = cell.Address.ColumnNumber;
        }

        if (!result.ContainsKey(nameof(SalesRecord.InvoiceNumber)))
            throw new InvalidOperationException("Die Excel-Datei hat nicht das erwartete Exportformat. Spalte 'Invoice Number' fehlt.");

        return result;
    }

    private static bool IsRowEmpty(IXLRangeRow row)
        => row.CellsUsed().All(cell => string.IsNullOrWhiteSpace(cell.GetFormattedString()));

    private static string ReadString(Dictionary<string, int> headerIndexes, IXLRangeRow row, string fieldName, string fallback = "")
    {
        if (!headerIndexes.TryGetValue(fieldName, out var index))
            return fallback;

        var value = row.Cell(index).GetFormattedString().Trim();
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private static decimal ReadDecimal(Dictionary<string, int> headerIndexes, IXLRangeRow row, string fieldName)
    {
        if (!headerIndexes.TryGetValue(fieldName, out var index))
            return 0m;

        var cell = row.Cell(index);
        if (cell.TryGetValue<decimal>(out var decimalValue))
            return decimalValue;
        if (cell.TryGetValue<double>(out var doubleValue))
            return Convert.ToDecimal(doubleValue, CultureInfo.InvariantCulture);

        var text = cell.GetFormattedString().Trim();
        if (string.IsNullOrWhiteSpace(text))
            return 0m;

        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimalValue))
            return decimalValue;
        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.GetCultureInfo("de-CH"), out decimalValue))
            return decimalValue;
        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.GetCultureInfo("de-DE"), out decimalValue))
            return decimalValue;

        return 0m;
    }

    private static DateTime? ReadDate(Dictionary<string, int> headerIndexes, IXLRangeRow row, string fieldName)
    {
        if (!headerIndexes.TryGetValue(fieldName, out var index))
            return null;

        var cell = row.Cell(index);
        if (cell.TryGetValue<DateTime>(out var dateValue))
            return dateValue;

        var text = cell.GetFormattedString().Trim();
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var formats = new[]
        {
            "dd.MM.yyyy HH:mm:ss",
            "dd.MM.yyyy",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd",
            "O"
        };

        if (DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dateValue))
            return dateValue;
        if (DateTime.TryParse(text, CultureInfo.GetCultureInfo("de-CH"), DateTimeStyles.AssumeLocal, out dateValue))
            return dateValue;
        if (DateTime.TryParse(text, CultureInfo.GetCultureInfo("de-DE"), DateTimeStyles.AssumeLocal, out dateValue))
            return dateValue;

        return null;
    }

    private static string NormalizeHeader(string value)
    {
        var chars = value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray();
        return new string(chars);
    }
}
