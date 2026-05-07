using System.Globalization;
using System.Reflection;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public class ManualExcelImportService : IManualExcelImportService
{
    private static readonly Dictionary<string, PropertyInfo> SalesRecordProperties = typeof(SalesRecord)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, string> HeaderMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["extractiondate"] = nameof(SalesRecord.ExtractionDate),
        ["tsc"] = nameof(SalesRecord.Tsc),
        ["documententry"] = nameof(SalesRecord.DocumentEntry),
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
        ["documentcurrency"] = nameof(SalesRecord.DocumentCurrency),
        ["documenttotalfc"] = nameof(SalesRecord.DocumentTotalForeignCurrency),
        ["documenttotalforeigncurrency"] = nameof(SalesRecord.DocumentTotalForeignCurrency),
        ["documenttotallc"] = nameof(SalesRecord.DocumentTotalLocalCurrency),
        ["documenttotallocalcurrency"] = nameof(SalesRecord.DocumentTotalLocalCurrency),
        ["vatsumfc"] = nameof(SalesRecord.VatSumForeignCurrency),
        ["vatsumforeigncurrency"] = nameof(SalesRecord.VatSumForeignCurrency),
        ["vatsumlc"] = nameof(SalesRecord.VatSumLocalCurrency),
        ["vatsumlocalcurrency"] = nameof(SalesRecord.VatSumLocalCurrency),
        ["documentrate"] = nameof(SalesRecord.DocumentRate),
        ["companycurrency"] = nameof(SalesRecord.CompanyCurrency),
        ["incoterms2020"] = nameof(SalesRecord.Incoterms2020),
        ["salesresponsibleemployee"] = nameof(SalesRecord.SalesResponsibleEmployee),
        ["invoicedate"] = nameof(SalesRecord.InvoiceDate),
        ["orderdate"] = nameof(SalesRecord.OrderDate),
        ["land"] = nameof(SalesRecord.Land),
        ["documenttype"] = nameof(SalesRecord.DocumentType)
    };

    private readonly IDbContextFactory<AppDbContext>? _dbFactory;

    public ManualExcelImportService()
    {
    }

    public ManualExcelImportService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<SalesRecord>> ReadSalesRecordsAsync(string filePath, Site site)
    {
        var mappings = await LoadMappingsAsync(site.Id);
        return ReadSalesRecords(filePath, site, mappings);
    }

    public Task<List<SalesRecord>> ReadSalesRecordsAsync(string filePath, Site site, IReadOnlyList<ManualExcelColumnMapping> mappings)
        => Task.FromResult(ReadSalesRecords(filePath, site, mappings));

    private async Task<List<ManualExcelColumnMapping>> LoadMappingsAsync(int siteId)
    {
        if (_dbFactory is null || siteId <= 0)
            return [];

        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.ManualExcelColumnMappings
            .AsNoTracking()
            .Where(m => m.SiteId == siteId && m.IsActive)
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.Id)
            .ToListAsync();
    }

    private static List<SalesRecord> ReadSalesRecords(string filePath, Site site, IReadOnlyList<ManualExcelColumnMapping> mappings)
    {
        if (string.Equals(Path.GetExtension(filePath), ".csv", StringComparison.OrdinalIgnoreCase))
            return ReadCsvSalesRecords(filePath, site, mappings);

        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidOperationException("Die Excel-Datei enthaelt kein Arbeitsblatt.");
        var usedRange = worksheet.RangeUsed()
            ?? throw new InvalidOperationException("Die Excel-Datei enthaelt keine Daten.");

        var headerRow = usedRange.FirstRow();
        var activeMappings = mappings
            .Where(m => m.IsActive && !string.IsNullOrWhiteSpace(m.TargetField) && !string.IsNullOrWhiteSpace(m.SourceHeader))
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.Id)
            .ToList();

        return activeMappings.Count > 0
            ? ReadMappedRows(usedRange, headerRow, site, activeMappings)
            : ReadDefaultRows(usedRange, headerRow, site);
    }

    private static List<SalesRecord> ReadCsvSalesRecords(string filePath, Site site, IReadOnlyList<ManualExcelColumnMapping> mappings)
    {
        using var parser = new TextFieldParser(filePath)
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = false
        };
        parser.SetDelimiters(";");

        var header = parser.ReadFields()
            ?? throw new InvalidOperationException("Die CSV-Datei enthaelt keine Kopfzeile.");

        var activeMappings = mappings
            .Where(m => m.IsActive && !string.IsNullOrWhiteSpace(m.TargetField) && !string.IsNullOrWhiteSpace(m.SourceHeader))
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.Id)
            .ToList();

        return activeMappings.Count > 0
            ? ReadMappedCsvRows(parser, header, site, activeMappings)
            : ReadDefaultCsvRows(parser, header, site);
    }

    private static List<SalesRecord> ReadDefaultCsvRows(TextFieldParser parser, string[] header, Site site)
    {
        var headerIndexes = BuildHeaderIndexMap(header);
        var rows = new List<SalesRecord>();

        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields();
            if (fields is null || IsCsvRowEmpty(fields))
                continue;

            rows.Add(new SalesRecord
            {
                ExtractionDate = ReadDate(headerIndexes, fields, nameof(SalesRecord.ExtractionDate)) ?? DateTime.UtcNow,
                Tsc = ReadString(headerIndexes, fields, nameof(SalesRecord.Tsc), site.TSC),
                DocumentEntry = (int)Math.Round(ReadDecimal(headerIndexes, fields, nameof(SalesRecord.DocumentEntry))),
                InvoiceNumber = ReadString(headerIndexes, fields, nameof(SalesRecord.InvoiceNumber)),
                PositionOnInvoice = (int)Math.Round(ReadDecimal(headerIndexes, fields, nameof(SalesRecord.PositionOnInvoice))),
                Material = ReadString(headerIndexes, fields, nameof(SalesRecord.Material)),
                Name = ReadString(headerIndexes, fields, nameof(SalesRecord.Name)),
                ProductGroup = ReadString(headerIndexes, fields, nameof(SalesRecord.ProductGroup)),
                Quantity = ReadDecimal(headerIndexes, fields, nameof(SalesRecord.Quantity)),
                SupplierNumber = ReadString(headerIndexes, fields, nameof(SalesRecord.SupplierNumber)),
                SupplierName = ReadString(headerIndexes, fields, nameof(SalesRecord.SupplierName)),
                SupplierCountry = ReadString(headerIndexes, fields, nameof(SalesRecord.SupplierCountry)),
                CustomerNumber = ReadString(headerIndexes, fields, nameof(SalesRecord.CustomerNumber)),
                CustomerName = ReadString(headerIndexes, fields, nameof(SalesRecord.CustomerName)),
                CustomerCountry = ReadString(headerIndexes, fields, nameof(SalesRecord.CustomerCountry)),
                CustomerIndustry = ReadString(headerIndexes, fields, nameof(SalesRecord.CustomerIndustry)),
                StandardCost = ReadDecimal(headerIndexes, fields, nameof(SalesRecord.StandardCost)),
                StandardCostCurrency = ReadString(headerIndexes, fields, nameof(SalesRecord.StandardCostCurrency)),
                PurchaseOrderNumber = ReadString(headerIndexes, fields, nameof(SalesRecord.PurchaseOrderNumber)),
                SalesPriceValue = ReadDecimal(headerIndexes, fields, nameof(SalesRecord.SalesPriceValue)),
                SalesCurrency = ReadString(headerIndexes, fields, nameof(SalesRecord.SalesCurrency)),
                DocumentCurrency = ReadString(headerIndexes, fields, nameof(SalesRecord.DocumentCurrency)),
                DocumentTotalForeignCurrency = ReadDecimal(headerIndexes, fields, nameof(SalesRecord.DocumentTotalForeignCurrency)),
                DocumentTotalLocalCurrency = ReadDecimal(headerIndexes, fields, nameof(SalesRecord.DocumentTotalLocalCurrency)),
                VatSumForeignCurrency = ReadDecimal(headerIndexes, fields, nameof(SalesRecord.VatSumForeignCurrency)),
                VatSumLocalCurrency = ReadDecimal(headerIndexes, fields, nameof(SalesRecord.VatSumLocalCurrency)),
                DocumentRate = ReadDecimal(headerIndexes, fields, nameof(SalesRecord.DocumentRate)),
                CompanyCurrency = ReadString(headerIndexes, fields, nameof(SalesRecord.CompanyCurrency)),
                Incoterms2020 = ReadString(headerIndexes, fields, nameof(SalesRecord.Incoterms2020)),
                SalesResponsibleEmployee = ReadString(headerIndexes, fields, nameof(SalesRecord.SalesResponsibleEmployee)),
                InvoiceDate = ReadDate(headerIndexes, fields, nameof(SalesRecord.InvoiceDate)),
                OrderDate = ReadDate(headerIndexes, fields, nameof(SalesRecord.OrderDate)),
                Land = ReadString(headerIndexes, fields, nameof(SalesRecord.Land), site.Land),
                DocumentType = ReadString(headerIndexes, fields, nameof(SalesRecord.DocumentType))
            });
        }

        return rows;
    }

    private static List<SalesRecord> ReadMappedCsvRows(
        TextFieldParser parser,
        string[] header,
        Site site,
        IReadOnlyList<ManualExcelColumnMapping> mappings)
    {
        var headerIndexes = BuildRawHeaderIndexMap(header);
        foreach (var mapping in mappings.Where(m => m.IsRequired))
        {
            if (mapping.SourceHeader.Trim().StartsWith('='))
                continue;

            if (!TryResolveHeaderIndex(headerIndexes, mapping.SourceHeader, out _))
                throw new InvalidOperationException($"Pflichtspalte '{mapping.SourceHeader}' fuer Zielfeld '{mapping.TargetField}' fehlt.");
        }

        var rows = new List<SalesRecord>();
        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields();
            if (fields is null || IsCsvRowEmpty(fields))
                continue;

            var record = new SalesRecord
            {
                ExtractionDate = DateTime.UtcNow,
                Tsc = site.TSC,
                Land = site.Land,
                DocumentType = "Manual Excel"
            };

            foreach (var mapping in mappings)
            {
                if (!SalesRecordProperties.TryGetValue(mapping.TargetField, out var property))
                    continue;

                var value = ReadMappedValue(headerIndexes, fields, mapping.SourceHeader);
                SetPropertyValue(record, property, value);
            }

            if (record.ExtractionDate == default)
                record.ExtractionDate = DateTime.UtcNow;
            if (string.IsNullOrWhiteSpace(record.Tsc))
                record.Tsc = site.TSC;
            if (string.IsNullOrWhiteSpace(record.Land))
                record.Land = site.Land;
            if (string.IsNullOrWhiteSpace(record.DocumentType))
                record.DocumentType = "Manual Excel";

            if (!IsMeaningfulMappedRecord(record))
                continue;

            rows.Add(record);
        }

        return rows;
    }

    private static List<SalesRecord> ReadDefaultRows(IXLRange usedRange, IXLRangeRow headerRow, Site site)
    {
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
                DocumentEntry = (int)Math.Round(ReadDecimal(headerIndexes, row, nameof(SalesRecord.DocumentEntry))),
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
                DocumentCurrency = ReadString(headerIndexes, row, nameof(SalesRecord.DocumentCurrency)),
                DocumentTotalForeignCurrency = ReadDecimal(headerIndexes, row, nameof(SalesRecord.DocumentTotalForeignCurrency)),
                DocumentTotalLocalCurrency = ReadDecimal(headerIndexes, row, nameof(SalesRecord.DocumentTotalLocalCurrency)),
                VatSumForeignCurrency = ReadDecimal(headerIndexes, row, nameof(SalesRecord.VatSumForeignCurrency)),
                VatSumLocalCurrency = ReadDecimal(headerIndexes, row, nameof(SalesRecord.VatSumLocalCurrency)),
                DocumentRate = ReadDecimal(headerIndexes, row, nameof(SalesRecord.DocumentRate)),
                CompanyCurrency = ReadString(headerIndexes, row, nameof(SalesRecord.CompanyCurrency)),
                Incoterms2020 = ReadString(headerIndexes, row, nameof(SalesRecord.Incoterms2020)),
                SalesResponsibleEmployee = ReadString(headerIndexes, row, nameof(SalesRecord.SalesResponsibleEmployee)),
                InvoiceDate = ReadDate(headerIndexes, row, nameof(SalesRecord.InvoiceDate)),
                OrderDate = ReadDate(headerIndexes, row, nameof(SalesRecord.OrderDate)),
                Land = ReadString(headerIndexes, row, nameof(SalesRecord.Land), site.Land),
                DocumentType = ReadString(headerIndexes, row, nameof(SalesRecord.DocumentType))
            });
        }

        return rows;
    }

    private static List<SalesRecord> ReadMappedRows(
        IXLRange usedRange,
        IXLRangeRow headerRow,
        Site site,
        IReadOnlyList<ManualExcelColumnMapping> mappings)
    {
        var headerIndexes = BuildRawHeaderIndexMap(headerRow);
        foreach (var mapping in mappings.Where(m => m.IsRequired))
        {
            if (mapping.SourceHeader.Trim().StartsWith('='))
                continue;

            if (!TryResolveHeaderIndex(headerIndexes, mapping.SourceHeader, out _))
                throw new InvalidOperationException($"Pflichtspalte '{mapping.SourceHeader}' fuer Zielfeld '{mapping.TargetField}' fehlt.");
        }

        var rows = new List<SalesRecord>();
        foreach (var row in usedRange.RowsUsed().Skip(1))
        {
            if (IsRowEmpty(row))
                continue;

            var record = new SalesRecord
            {
                ExtractionDate = DateTime.UtcNow,
                Tsc = site.TSC,
                Land = site.Land,
                DocumentType = "Manual Excel"
            };

            foreach (var mapping in mappings)
            {
                if (!SalesRecordProperties.TryGetValue(mapping.TargetField, out var property))
                    continue;

                var value = ReadMappedValue(headerIndexes, row, mapping.SourceHeader);
                SetPropertyValue(record, property, value);
            }

            if (record.ExtractionDate == default)
                record.ExtractionDate = DateTime.UtcNow;
            if (string.IsNullOrWhiteSpace(record.Tsc))
                record.Tsc = site.TSC;
            if (string.IsNullOrWhiteSpace(record.Land))
                record.Land = site.Land;
            if (string.IsNullOrWhiteSpace(record.DocumentType))
                record.DocumentType = "Manual Excel";

            if (!IsMeaningfulMappedRecord(record))
                continue;

            rows.Add(record);
        }

        return rows;
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

    private static Dictionary<string, int> BuildHeaderIndexMap(string[] header)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < header.Length; i++)
        {
            var normalizedHeader = NormalizeHeader(header[i]);
            if (string.IsNullOrWhiteSpace(normalizedHeader))
                continue;

            if (HeaderMap.TryGetValue(normalizedHeader, out var targetField))
                result[targetField] = i;
        }

        if (!result.ContainsKey(nameof(SalesRecord.InvoiceNumber)))
            throw new InvalidOperationException("Die CSV-Datei hat nicht das erwartete Exportformat. Spalte 'Invoice Number' fehlt.");

        return result;
    }

    private static Dictionary<string, int> BuildRawHeaderIndexMap(IXLRangeRow headerRow)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var cell in headerRow.CellsUsed())
        {
            var header = cell.GetString().Trim();
            if (string.IsNullOrWhiteSpace(header))
                continue;

            result[header] = cell.Address.ColumnNumber;
            result[NormalizeHeader(header)] = cell.Address.ColumnNumber;
        }

        return result;
    }

    private static Dictionary<string, int> BuildRawHeaderIndexMap(string[] header)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < header.Length; i++)
        {
            var value = header[i].Trim();
            if (string.IsNullOrWhiteSpace(value))
                continue;

            result[value] = i;
            result[NormalizeHeader(value)] = i;
        }

        return result;
    }

    private static bool TryResolveHeaderIndex(Dictionary<string, int> headerIndexes, string sourceHeader, out int index)
    {
        var trimmed = sourceHeader.Trim();
        return headerIndexes.TryGetValue(trimmed, out index) ||
               headerIndexes.TryGetValue(NormalizeHeader(trimmed), out index);
    }

    private static object? ReadMappedValue(Dictionary<string, int> headerIndexes, IXLRangeRow row, string sourceHeader)
    {
        var trimmed = sourceHeader.Trim();
        if (trimmed.StartsWith('='))
            return trimmed[1..];

        return TryResolveHeaderIndex(headerIndexes, trimmed, out var index)
            ? row.Cell(index).GetFormattedString().Trim()
            : null;
    }

    private static object? ReadMappedValue(Dictionary<string, int> headerIndexes, string[] fields, string sourceHeader)
    {
        var trimmed = sourceHeader.Trim();
        if (trimmed.StartsWith('='))
            return trimmed[1..];

        return TryResolveHeaderIndex(headerIndexes, trimmed, out var index) && index < fields.Length
            ? fields[index].Trim()
            : null;
    }

    private static bool IsRowEmpty(IXLRangeRow row)
        => row.CellsUsed().All(cell => string.IsNullOrWhiteSpace(cell.GetFormattedString()));

    private static bool IsCsvRowEmpty(string[] fields)
        => fields.All(string.IsNullOrWhiteSpace);

    private static string ReadString(Dictionary<string, int> headerIndexes, IXLRangeRow row, string fieldName, string fallback = "")
    {
        if (!headerIndexes.TryGetValue(fieldName, out var index))
            return fallback;

        var value = row.Cell(index).GetFormattedString().Trim();
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private static string ReadString(Dictionary<string, int> headerIndexes, string[] fields, string fieldName, string fallback = "")
    {
        if (!headerIndexes.TryGetValue(fieldName, out var index) || index >= fields.Length)
            return fallback;

        var value = fields[index].Trim();
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

        return ParseDecimal(cell.GetFormattedString().Trim());
    }

    private static decimal ReadDecimal(Dictionary<string, int> headerIndexes, string[] fields, string fieldName)
    {
        return !headerIndexes.TryGetValue(fieldName, out var index) || index >= fields.Length
            ? 0m
            : ParseDecimal(fields[index].Trim());
    }

    private static DateTime? ReadDate(Dictionary<string, int> headerIndexes, IXLRangeRow row, string fieldName)
    {
        if (!headerIndexes.TryGetValue(fieldName, out var index))
            return null;

        var cell = row.Cell(index);
        if (cell.TryGetValue<DateTime>(out var dateValue))
            return dateValue;

        return ParseDate(cell.GetFormattedString().Trim());
    }

    private static DateTime? ReadDate(Dictionary<string, int> headerIndexes, string[] fields, string fieldName)
    {
        return !headerIndexes.TryGetValue(fieldName, out var index) || index >= fields.Length
            ? null
            : ParseDate(fields[index].Trim());
    }

    private static void SetPropertyValue(SalesRecord record, PropertyInfo property, object? value)
    {
        try
        {
            var text = value?.ToString()?.Trim() ?? string.Empty;

            if (property.PropertyType == typeof(string))
            {
                property.SetValue(record, text);
                return;
            }

            if (property.PropertyType == typeof(int))
            {
                property.SetValue(record, (int)Math.Round(ParseDecimal(text)));
                return;
            }

            if (property.PropertyType == typeof(decimal))
            {
                property.SetValue(record, ParseDecimal(text));
                return;
            }

            if (property.PropertyType == typeof(DateTime?))
            {
                property.SetValue(record, ParseDate(text));
                return;
            }

            if (property.PropertyType == typeof(DateTime))
                property.SetValue(record, ParseDate(text) ?? default);
        }
        catch
        {
            // Einzelne fehlerhafte Zellen duerfen den kompletten manuellen Import nicht abbrechen.
        }
    }

    private static decimal ParseDecimal(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0m;

        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.GetCultureInfo("de-CH"), out var decimalValue))
            return decimalValue;
        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.GetCultureInfo("de-DE"), out decimalValue))
            return decimalValue;
        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimalValue))
            return decimalValue;

        return 0m;
    }

    private static DateTime? ParseDate(string text)
    {
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

        if (DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dateValue))
            return dateValue;
        if (DateTime.TryParse(text, CultureInfo.GetCultureInfo("de-CH"), DateTimeStyles.AssumeLocal, out dateValue))
            return dateValue;
        if (DateTime.TryParse(text, CultureInfo.GetCultureInfo("de-DE"), DateTimeStyles.AssumeLocal, out dateValue))
            return dateValue;

        return null;
    }

    private static bool IsMeaningfulMappedRecord(SalesRecord record)
        => record.PositionOnInvoice != 0 ||
           record.Quantity != 0m ||
           record.SalesPriceValue != 0m ||
           !string.IsNullOrWhiteSpace(record.Material);

    private static string NormalizeHeader(string value)
    {
        var chars = value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray();
        return new string(chars);
    }
}
