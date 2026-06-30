using System.Globalization;
using System.Text;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface IExportAuditCsvService
{
    Task<string?> WriteSiteAuditCsvAsync(
        Site site,
        ExportSettings settings,
        string sourceSystem,
        string fallbackOutputDirectory,
        IReadOnlyList<SalesRecord> records);

    Task<string?> WriteConsolidatedAuditCsvAsync(
        ExportSettings settings,
        DateTime fileDate,
        string fallbackOutputDirectory,
        IReadOnlyList<SalesRecord> records);

    Task<List<SalesRecord>> ReadLatestSiteAuditCsvRecordsAsync(ExportSettings settings);

    Task<IReadOnlyList<AuditCsvSnapshot>> ReadLatestSiteAuditCsvSnapshotsAsync(ExportSettings settings);

    Task<List<SalesRecord>> ReadLatestConsolidatedAuditCsvRecordsAsync(ExportSettings settings);

    string ResolveAuditCsvDirectory(ExportSettings settings, string? fallbackOutputDirectory = null);
}

public sealed record AuditCsvSnapshot(
    string Tsc,
    string Path,
    DateTime LastWriteTimeUtc,
    List<SalesRecord> Records);

public sealed class ExportAuditCsvService : IExportAuditCsvService
{
    private const char Delimiter = ';';
    private const string ProcessedMergeInputFilePrefix = "Sales_ProcessedMergeInput_";
    private const string ConsolidatedAuditFilePrefix = "Finance_Dashboard_Audit_All_";
    private const string LegacyFilePrefix = "Sales_";

    private static readonly string[] Headers =
    [
        "SourceSystem",
        "ExtractionDate",
        "TSC",
        "SourceLineId",
        "DocumentEntry",
        "InvoiceNumber",
        "PositionOnInvoice",
        "Material",
        "Name",
        "ProductGroup",
        "ProductHierarchyCode",
        "ProductHierarchyText",
        "ProductFamilyCode",
        "ProductFamilyText",
        "ProductDivisionCode",
        "ProductDivisionText",
        "ProductMappingAssigned",
        "Quantity",
        "SupplierNumber",
        "SupplierName",
        "SupplierCountry",
        "CustomerNumber",
        "CustomerName",
        "CustomerCountry",
        "CustomerIndustry",
        "StandardCost",
        "StandardCostCurrency",
        "PurchaseOrderNumber",
        "SalesPriceValue",
        "SalesCurrency",
        "DocumentCurrency",
        "DocumentTotalForeignCurrency",
        "DocumentTotalLocalCurrency",
        "VatSumForeignCurrency",
        "VatSumLocalCurrency",
        "DocumentRate",
        "CompanyCurrency",
        "Incoterms2020",
        "SalesResponsibleEmployee",
        "PostingDate",
        "InvoiceDate",
        "OrderDate",
        "Land",
        "DocumentType"
    ];

    public async Task<string?> WriteSiteAuditCsvAsync(
        Site site,
        ExportSettings settings,
        string sourceSystem,
        string fallbackOutputDirectory,
        IReadOnlyList<SalesRecord> records)
    {
        if (!settings.AuditCsvEnabled)
            return null;

        var directory = ResolveAuditCsvDirectory(settings, fallbackOutputDirectory);
        Directory.CreateDirectory(directory);

        var tsc = string.IsNullOrWhiteSpace(site.TSC) ? "UNKNOWN" : site.TSC.Trim();
        var fileName = $"{ProcessedMergeInputFilePrefix}{SanitizeFileNamePart(tsc)}_{DateTime.UtcNow:yyyy-MM-dd}.csv";
        var path = Path.Combine(directory, fileName);

        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        await writer.WriteLineAsync(string.Join(Delimiter, Headers.Select(Escape)));
        foreach (var record in records)
        {
            await writer.WriteLineAsync(string.Join(Delimiter, BuildRow(site, sourceSystem, record).Select(Escape)));
        }

        return path;
    }

    public async Task<string?> WriteConsolidatedAuditCsvAsync(
        ExportSettings settings,
        DateTime fileDate,
        string fallbackOutputDirectory,
        IReadOnlyList<SalesRecord> records)
    {
        if (!settings.AuditCsvEnabled)
            return null;

        var directory = string.IsNullOrWhiteSpace(fallbackOutputDirectory)
            ? ResolveConsolidatedAuditCsvDirectory(settings)
            : fallbackOutputDirectory.Trim();
        Directory.CreateDirectory(directory);

        var fileName = $"Finance_Dashboard_Audit_All_{fileDate:yyyy-MM-dd}.csv";
        var path = Path.Combine(directory, fileName);

        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        await writer.WriteLineAsync(string.Join(Delimiter, Headers.Select(Escape)));
        foreach (var record in records)
        {
            var site = new Site { TSC = record.Tsc, Land = record.Land };
            await writer.WriteLineAsync(string.Join(Delimiter, BuildRow(site, record.SourceSystem, record).Select(Escape)));
        }

        return path;
    }

    public async Task<List<SalesRecord>> ReadLatestSiteAuditCsvRecordsAsync(ExportSettings settings)
        => (await ReadLatestSiteAuditCsvSnapshotsAsync(settings))
            .SelectMany(snapshot => snapshot.Records)
            .ToList();

    public async Task<IReadOnlyList<AuditCsvSnapshot>> ReadLatestSiteAuditCsvSnapshotsAsync(ExportSettings settings)
    {
        var directory = ResolveAuditCsvDirectory(settings);
        if (!Directory.Exists(directory))
            return [];

        var latestFiles = EnumerateAuditCsvFiles(directory)
            .Select(path => new { Path = path, Tsc = ResolveTscFromFileName(path) })
            .Where(file => !string.IsNullOrWhiteSpace(file.Tsc))
            .GroupBy(file => file.Tsc, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderByDescending(file => File.GetLastWriteTimeUtc(file.Path))
                .ThenByDescending(file => IsProcessedMergeInputFile(file.Path))
                .ThenByDescending(file => Path.GetFileName(file.Path), StringComparer.OrdinalIgnoreCase)
                .First()
            )
            .OrderBy(file => file.Tsc, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var snapshots = new List<AuditCsvSnapshot>();
        foreach (var file in latestFiles)
        {
            snapshots.Add(new AuditCsvSnapshot(
                file.Tsc,
                file.Path,
                File.GetLastWriteTimeUtc(file.Path),
                await ReadFileAsync(file.Path)));
        }

        return snapshots;
    }

    public async Task<List<SalesRecord>> ReadLatestConsolidatedAuditCsvRecordsAsync(ExportSettings settings)
    {
        var directory = ResolveConsolidatedAuditCsvDirectory(settings);
        if (!Directory.Exists(directory))
            return [];

        var latestFile = Directory
            .EnumerateFiles(directory, $"{ConsolidatedAuditFilePrefix}*.csv", SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .ThenByDescending(file => file.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        return latestFile is null
            ? []
            : await ReadFileAsync(latestFile.FullName);
    }

    public string ResolveAuditCsvDirectory(ExportSettings settings, string? fallbackOutputDirectory = null)
    {
        if (!string.IsNullOrWhiteSpace(fallbackOutputDirectory))
            return fallbackOutputDirectory.Trim();

        if (!string.IsNullOrWhiteSpace(settings.LocalSiteExportFolder))
            return settings.LocalSiteExportFolder.Trim();

        return Path.Combine(AppContext.BaseDirectory, "output");
    }

    private static string ResolveConsolidatedAuditCsvDirectory(ExportSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.LocalConsolidatedExportFolder))
            return settings.LocalConsolidatedExportFolder.Trim();

        if (!string.IsNullOrWhiteSpace(settings.LocalSiteExportFolder))
            return settings.LocalSiteExportFolder.Trim();

        return Path.Combine(AppContext.BaseDirectory, "output");
    }

    private static IEnumerable<string> BuildRow(Site site, string sourceSystem, SalesRecord record)
    {
        yield return string.IsNullOrWhiteSpace(record.SourceSystem) ? sourceSystem : record.SourceSystem;
        yield return FormatDate(record.ExtractionDate);
        yield return record.Tsc;
        yield return record.SourceLineId;
        yield return FormatInt(record.DocumentEntry);
        yield return record.InvoiceNumber;
        yield return FormatInt(record.PositionOnInvoice);
        yield return record.Material;
        yield return record.Name;
        yield return record.ProductGroup;
        yield return record.ProductHierarchyCode;
        yield return record.ProductHierarchyText;
        yield return record.ProductFamilyCode;
        yield return record.ProductFamilyText;
        yield return record.ProductDivisionCode;
        yield return record.ProductDivisionText;
        yield return record.ProductMappingAssigned;
        yield return FormatDecimal(record.Quantity);
        yield return record.SupplierNumber;
        yield return record.SupplierName;
        yield return record.SupplierCountry;
        yield return record.CustomerNumber;
        yield return record.CustomerName;
        yield return record.CustomerCountry;
        yield return record.CustomerIndustry;
        yield return FormatDecimal(record.StandardCost);
        yield return record.StandardCostCurrency;
        yield return record.PurchaseOrderNumber;
        yield return FormatDecimal(record.SalesPriceValue);
        yield return record.SalesCurrency;
        yield return record.DocumentCurrency;
        yield return FormatDecimal(record.DocumentTotalForeignCurrency);
        yield return FormatDecimal(record.DocumentTotalLocalCurrency);
        yield return FormatDecimal(record.VatSumForeignCurrency);
        yield return FormatDecimal(record.VatSumLocalCurrency);
        yield return FormatDecimal(record.DocumentRate);
        yield return record.CompanyCurrency;
        yield return record.Incoterms2020;
        yield return record.SalesResponsibleEmployee;
        yield return FormatNullableDate(record.PostingDate);
        yield return FormatNullableDate(record.InvoiceDate);
        yield return FormatNullableDate(record.OrderDate);
        yield return string.IsNullOrWhiteSpace(record.Land) ? site.Land : record.Land;
        yield return record.DocumentType;
    }

    private static async Task<List<SalesRecord>> ReadFileAsync(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        var headerLine = await reader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(headerLine))
            return [];

        var headers = ParseLine(headerLine)
            .Select((value, index) => new { Header = NormalizeHeader(value), Index = index })
            .Where(x => !string.IsNullOrWhiteSpace(x.Header))
            .ToDictionary(x => x.Header, x => x.Index, StringComparer.OrdinalIgnoreCase);

        var records = new List<SalesRecord>();
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var values = ParseLine(line);
            records.Add(new SalesRecord
            {
                SourceSystem = GetText(values, headers, "SourceSystem"),
                ExtractionDate = GetDate(values, headers, "ExtractionDate") ?? File.GetLastWriteTime(path),
                Tsc = GetText(values, headers, "TSC"),
                SourceLineId = GetText(values, headers, "SourceLineId"),
                DocumentEntry = GetInt(values, headers, "DocumentEntry"),
                InvoiceNumber = GetText(values, headers, "InvoiceNumber"),
                PositionOnInvoice = GetInt(values, headers, "PositionOnInvoice"),
                Material = GetText(values, headers, "Material"),
                Name = GetText(values, headers, "Name"),
                ProductGroup = GetText(values, headers, "ProductGroup"),
                ProductHierarchyCode = GetText(values, headers, "ProductHierarchyCode"),
                ProductHierarchyText = GetText(values, headers, "ProductHierarchyText"),
                ProductFamilyCode = GetText(values, headers, "ProductFamilyCode"),
                ProductFamilyText = GetText(values, headers, "ProductFamilyText"),
                ProductDivisionCode = GetText(values, headers, "ProductDivisionCode"),
                ProductDivisionText = GetText(values, headers, "ProductDivisionText"),
                ProductMappingAssigned = GetText(values, headers, "ProductMappingAssigned"),
                Quantity = GetDecimal(values, headers, "Quantity"),
                SupplierNumber = GetText(values, headers, "SupplierNumber"),
                SupplierName = GetText(values, headers, "SupplierName"),
                SupplierCountry = GetText(values, headers, "SupplierCountry"),
                CustomerNumber = GetText(values, headers, "CustomerNumber"),
                CustomerName = GetText(values, headers, "CustomerName"),
                CustomerCountry = GetText(values, headers, "CustomerCountry"),
                CustomerIndustry = GetText(values, headers, "CustomerIndustry"),
                StandardCost = GetDecimal(values, headers, "StandardCost"),
                StandardCostCurrency = GetText(values, headers, "StandardCostCurrency"),
                PurchaseOrderNumber = GetText(values, headers, "PurchaseOrderNumber"),
                SalesPriceValue = GetDecimal(values, headers, "SalesPriceValue"),
                SalesCurrency = GetText(values, headers, "SalesCurrency"),
                DocumentCurrency = GetText(values, headers, "DocumentCurrency"),
                DocumentTotalForeignCurrency = GetDecimal(values, headers, "DocumentTotalForeignCurrency"),
                DocumentTotalLocalCurrency = GetDecimal(values, headers, "DocumentTotalLocalCurrency"),
                VatSumForeignCurrency = GetDecimal(values, headers, "VatSumForeignCurrency"),
                VatSumLocalCurrency = GetDecimal(values, headers, "VatSumLocalCurrency"),
                DocumentRate = GetDecimal(values, headers, "DocumentRate"),
                CompanyCurrency = GetText(values, headers, "CompanyCurrency"),
                Incoterms2020 = GetText(values, headers, "Incoterms2020"),
                SalesResponsibleEmployee = GetText(values, headers, "SalesResponsibleEmployee"),
                PostingDate = GetDate(values, headers, "PostingDate"),
                InvoiceDate = GetDate(values, headers, "InvoiceDate"),
                OrderDate = GetDate(values, headers, "OrderDate"),
                Land = GetText(values, headers, "Land"),
                DocumentType = GetText(values, headers, "DocumentType")
            });
        }

        return records;
    }

    private static string ResolveTscFromFileName(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        if (name.StartsWith(ProcessedMergeInputFilePrefix, StringComparison.OrdinalIgnoreCase))
            return ResolveTscFromSuffix(name[ProcessedMergeInputFilePrefix.Length..]);

        if (name.StartsWith(LegacyFilePrefix, StringComparison.OrdinalIgnoreCase))
            return ResolveTscFromSuffix(name[LegacyFilePrefix.Length..]);

        return string.Empty;
    }

    private static string ResolveTscFromSuffix(string suffix)
    {
        var lastUnderscore = suffix.LastIndexOf('_');
        return lastUnderscore <= 0 ? suffix : suffix[..lastUnderscore];
    }

    private static IEnumerable<string> EnumerateAuditCsvFiles(string directory)
        => Directory.EnumerateFiles(directory, $"{ProcessedMergeInputFilePrefix}*.csv", SearchOption.TopDirectoryOnly)
            .Concat(Directory.EnumerateFiles(directory, $"{LegacyFilePrefix}*.csv", SearchOption.TopDirectoryOnly)
                .Where(path => !IsProcessedMergeInputFile(path)));

    private static bool IsProcessedMergeInputFile(string path)
        => Path.GetFileName(path).StartsWith(ProcessedMergeInputFilePrefix, StringComparison.OrdinalIgnoreCase);

    private static string SanitizeFileNamePart(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray();
        return new string(chars);
    }

    private static string Escape(string? value)
    {
        var text = (value ?? string.Empty)
            .Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace('\r', ' ')
            .Replace('\n', ' ');
        if (text.Contains(Delimiter) || text.Contains('"') || text.Contains('\r') || text.Contains('\n'))
            return $"\"{text.Replace("\"", "\"\"")}\"";

        return text;
    }

    private static List<string> ParseLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (ch == Delimiter && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        values.Add(current.ToString());
        return values;
    }

    private static string NormalizeHeader(string value)
        => new(value.Where(char.IsLetterOrDigit).ToArray());

    private static string GetText(IReadOnlyList<string> values, IReadOnlyDictionary<string, int> headers, string header)
        => headers.TryGetValue(NormalizeHeader(header), out var index) && index >= 0 && index < values.Count
            ? values[index].Trim()
            : string.Empty;

    private static int GetInt(IReadOnlyList<string> values, IReadOnlyDictionary<string, int> headers, string header)
        => int.TryParse(GetText(values, headers, header), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;

    private static decimal GetDecimal(IReadOnlyList<string> values, IReadOnlyDictionary<string, int> headers, string header)
    {
        var text = GetText(values, headers, header);
        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var invariant))
            return invariant;

        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.GetCultureInfo("de-CH"), out var swiss))
            return swiss;

        return 0m;
    }

    private static DateTime? GetDate(IReadOnlyList<string> values, IReadOnlyDictionary<string, int> headers, string header)
    {
        var text = GetText(values, headers, header);
        if (string.IsNullOrWhiteSpace(text))
            return null;

        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var roundtrip))
            return roundtrip;

        if (DateTime.TryParse(text, CultureInfo.GetCultureInfo("de-CH"), DateTimeStyles.AssumeLocal, out var swiss))
            return swiss;

        return null;
    }

    private static string FormatInt(int value)
        => value.ToString(CultureInfo.InvariantCulture);

    private static string FormatDecimal(decimal value)
        => value.ToString(CultureInfo.InvariantCulture);

    private static string FormatDate(DateTime value)
        => value.ToString("O", CultureInfo.InvariantCulture);

    private static string FormatNullableDate(DateTime? value)
        => value?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty;
}
