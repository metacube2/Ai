using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public class ManagementCockpitService : IManagementCockpitService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public ManagementCockpitService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<ManagementCockpitFileOption>> GetAvailableFilesAsync()
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var settings = await db.ExportSettings.FirstOrDefaultAsync() ?? new ExportSettings();
        var exportLogs = await db.ExportLogs
            .Where(x => x.Status == "OK" && !string.IsNullOrWhiteSpace(x.FilePath))
            .OrderByDescending(x => x.Timestamp)
            .Take(200)
            .ToListAsync();

        var files = new Dictionary<string, ManagementCockpitFileOption>(StringComparer.OrdinalIgnoreCase);

        foreach (var log in exportLogs)
        {
            if (!File.Exists(log.FilePath))
                continue;

            files[log.FilePath] = new ManagementCockpitFileOption
            {
                Path = log.FilePath,
                DisplayName = $"{log.Land} | {log.TSC} | {Path.GetFileName(log.FilePath)}",
                LastModified = File.GetLastWriteTime(log.FilePath)
            };
        }

        foreach (var directory in GetCandidateDirectories(settings))
        {
            if (!Directory.Exists(directory))
                continue;

            foreach (var file in Directory.EnumerateFiles(directory, "*.xlsx", SearchOption.TopDirectoryOnly))
            {
                if (files.ContainsKey(file))
                    continue;

                var fileName = Path.GetFileName(file);
                files[file] = new ManagementCockpitFileOption
                {
                    Path = file,
                    DisplayName = fileName,
                    LastModified = File.GetLastWriteTime(file)
                };
            }
        }

        return files.Values
            .OrderByDescending(x => x.LastModified)
            .ThenBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public Task<ManagementCockpitResult> AnalyzeAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            throw new InvalidOperationException("Die ausgewählte Excel-Datei wurde nicht gefunden.");

        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.First();
        var usedRange = worksheet.RangeUsed() ?? throw new InvalidOperationException("Die Excel-Datei enthält keine Daten.");

        var headerRow = usedRange.FirstRow();
        var headers = headerRow.Cells()
            .Select((cell, index) => new { Index = index + 1, Header = NormalizeHeader(cell.GetString()) })
            .Where(x => !string.IsNullOrWhiteSpace(x.Header))
            .ToDictionary(x => x.Header, x => x.Index, StringComparer.OrdinalIgnoreCase);

        var rows = new List<CockpitRow>();
        foreach (var row in usedRange.RowsUsed().Skip(1))
        {
            if (row.CellsUsed().All(c => string.IsNullOrWhiteSpace(c.GetString())))
                continue;

            rows.Add(ReadRow(row, headers));
        }

        if (rows.Count == 0)
            throw new InvalidOperationException("Die Excel-Datei enthält keine auswertbaren Datenzeilen.");

        var result = new ManagementCockpitResult
        {
            FilePath = filePath,
            Summary = BuildSummary(rows),
            Findings = BuildFindings(rows),
            TopCustomers = BuildTopItems(rows, x => x.CustomerName, x => x.SalesValueTotal),
            TopProductGroups = BuildTopItems(rows, x => x.ProductGroup, x => x.SalesValueTotal),
            TopSalesEmployees = BuildTopItems(rows, x => x.SalesResponsibleEmployee, x => x.SalesValueTotal),
            DataQualityCounts = BuildDataQualityCounts(rows)
        };

        return Task.FromResult(result);
    }

    public async Task<List<int>> GetAvailableCentralYearsAsync()
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var years = await db.CentralSalesRecords
            .Select(r => r.InvoiceDate.HasValue ? r.InvoiceDate.Value.Year : r.ExtractionDate.Year)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        return years;
    }

    public async Task<ManagementCockpitCentralResult> AnalyzeCentralAsync(int year, int? month)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var baseRows = await db.CentralSalesRecords
            .Select(r => new CentralCockpitRow
            {
                SourceSystem = r.SourceSystem,
                Land = r.Land,
                Tsc = r.Tsc,
                InvoiceNumber = r.InvoiceNumber,
                SalesCurrency = string.IsNullOrWhiteSpace(r.SalesCurrency) ? "-" : r.SalesCurrency,
                SalesValue = r.SalesPriceValue,
                PeriodDate = r.InvoiceDate ?? r.ExtractionDate
            })
            .ToListAsync();

        if (baseRows.Count == 0)
            throw new InvalidOperationException("Die zentrale Tabelle enthält noch keine Datensätze.");

        var selectedRows = baseRows
            .Where(r => r.PeriodDate.Year == year && (!month.HasValue || r.PeriodDate.Month == month.Value))
            .ToList();

        if (selectedRows.Count == 0)
            throw new InvalidOperationException("Für den gewählten Zeitraum gibt es keine Datensätze in der zentralen Tabelle.");

        var yearlyRows = baseRows
            .Where(r => r.PeriodDate.Year == 2025 || r.PeriodDate.Year == 2026)
            .ToList();

        var dailyBaseRows = selectedRows
            .Where(r => month.HasValue)
            .ToList();

        return new ManagementCockpitCentralResult
        {
            Filter = new ManagementCockpitCentralFilter
            {
                Year = year,
                Month = month
            },
            Summary = new ManagementCockpitCentralSummary
            {
                RowCount = selectedRows.Count,
                InvoiceCount = selectedRows.Select(x => x.InvoiceNumber).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                SiteCount = selectedRows.Select(x => x.Tsc).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                CountryCount = selectedRows.Select(x => x.Land).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                CurrencyCount = selectedRows.Select(x => x.SalesCurrency).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                PeriodStart = selectedRows.Min(x => x.PeriodDate),
                PeriodEnd = selectedRows.Max(x => x.PeriodDate)
            },
            Notices =
            [
                "Roh-Auswertung aus CentralSalesRecords.",
                "Keine Intercompany-Bereinigung angewendet.",
                "Keine CHF-Umrechnung angewendet. Umsatz bleibt in Sales Currency.",
                "Kein Budget- und kein Spartemapping angewendet.",
                "Periodenlogik basiert auf Invoice Date, falls vorhanden, sonst auf Extraction Date."
            ],
            YearlyTotals = yearlyRows
                .GroupBy(x => new { x.PeriodDate.Year, x.SalesCurrency })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.SalesCurrency, StringComparer.OrdinalIgnoreCase)
                .Select(g => new ManagementCockpitTimeValueRow
                {
                    Label = g.Key.Year.ToString(),
                    Year = g.Key.Year,
                    Currency = g.Key.SalesCurrency,
                    SalesValue = g.Sum(x => x.SalesValue),
                    RowCount = g.Count()
                })
                .ToList(),
            MonthlyTotals = selectedRows
                .GroupBy(x => new { x.PeriodDate.Year, x.PeriodDate.Month, x.SalesCurrency })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month)
                .ThenBy(g => g.Key.SalesCurrency, StringComparer.OrdinalIgnoreCase)
                .Select(g => new ManagementCockpitTimeValueRow
                {
                    Label = $"{g.Key.Year:D4}-{g.Key.Month:D2}",
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Currency = g.Key.SalesCurrency,
                    SalesValue = g.Sum(x => x.SalesValue),
                    RowCount = g.Count()
                })
                .ToList(),
            DailyTotals = dailyBaseRows
                .GroupBy(x => new { x.PeriodDate.Year, x.PeriodDate.Month, x.PeriodDate.Day, x.SalesCurrency })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month)
                .ThenBy(g => g.Key.Day)
                .ThenBy(g => g.Key.SalesCurrency, StringComparer.OrdinalIgnoreCase)
                .Select(g => new ManagementCockpitTimeValueRow
                {
                    Label = $"{g.Key.Year:D4}-{g.Key.Month:D2}-{g.Key.Day:D2}",
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Day = g.Key.Day,
                    Currency = g.Key.SalesCurrency,
                    SalesValue = g.Sum(x => x.SalesValue),
                    RowCount = g.Count()
                })
                .ToList(),
            SourceSystemTotals = selectedRows
                .GroupBy(x => new { x.SourceSystem, x.SalesCurrency })
                .OrderBy(g => g.Key.SourceSystem, StringComparer.OrdinalIgnoreCase)
                .ThenBy(g => g.Key.SalesCurrency, StringComparer.OrdinalIgnoreCase)
                .Select(g => new ManagementCockpitDimensionValueRow
                {
                    Label = g.Key.SourceSystem,
                    Currency = g.Key.SalesCurrency,
                    SalesValue = g.Sum(x => x.SalesValue),
                    RowCount = g.Count(),
                    InvoiceCount = g.Select(x => x.InvoiceNumber).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Count()
                })
                .ToList(),
            CountryTotals = selectedRows
                .GroupBy(x => new { x.Land, x.SalesCurrency })
                .OrderByDescending(g => g.Sum(x => x.SalesValue))
                .ThenBy(g => g.Key.Land, StringComparer.OrdinalIgnoreCase)
                .ThenBy(g => g.Key.SalesCurrency, StringComparer.OrdinalIgnoreCase)
                .Select(g => new ManagementCockpitDimensionValueRow
                {
                    Label = g.Key.Land,
                    Currency = g.Key.SalesCurrency,
                    SalesValue = g.Sum(x => x.SalesValue),
                    RowCount = g.Count(),
                    InvoiceCount = g.Select(x => x.InvoiceNumber).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Count()
                })
                .ToList()
        };
    }

    private static IEnumerable<string> GetCandidateDirectories(ExportSettings settings)
    {
        yield return Path.Combine(AppContext.BaseDirectory, "output");

        if (!string.IsNullOrWhiteSpace(settings.LocalSiteExportFolder))
            yield return settings.LocalSiteExportFolder.Trim();

        if (!string.IsNullOrWhiteSpace(settings.LocalConsolidatedExportFolder))
            yield return settings.LocalConsolidatedExportFolder.Trim();
    }

    private static CockpitRow ReadRow(IXLRangeRow row, IReadOnlyDictionary<string, int> headers)
    {
        var quantity = GetDecimal(row, headers, "quantity");
        var standardCost = GetDecimal(row, headers, "standardcost");
        var salesValue = GetDecimal(row, headers, "salespricevalue");
        var estimatedCostTotal = quantity > 0 ? quantity * standardCost : standardCost;

        return new CockpitRow
        {
            ExtractionDate = GetDate(row, headers, "extractiondate"),
            Tsc = GetText(row, headers, "tsc"),
            InvoiceNumber = GetText(row, headers, "invoicenumber"),
            PositionOnInvoice = GetText(row, headers, "positiononinvoice"),
            Material = GetText(row, headers, "material"),
            Name = GetText(row, headers, "name"),
            ProductGroup = GetText(row, headers, "productgroup"),
            Quantity = quantity,
            SupplierNumber = GetText(row, headers, "suppliernumber"),
            SupplierName = GetText(row, headers, "suppliername"),
            SupplierCountry = GetText(row, headers, "suppliercountry"),
            CustomerNumber = GetText(row, headers, "customernumber"),
            CustomerName = GetText(row, headers, "customername"),
            CustomerCountry = GetText(row, headers, "customercountry"),
            CustomerIndustry = GetText(row, headers, "customerindustry"),
            StandardCost = standardCost,
            SalesValueTotal = salesValue,
            Incoterms2020 = GetText(row, headers, "incoterms2020"),
            SalesResponsibleEmployee = GetText(row, headers, "salesresponsibleemployee"),
            InvoiceDate = GetDate(row, headers, "invoicedate"),
            OrderDate = GetDate(row, headers, "orderdate"),
            Land = GetText(row, headers, "land"),
            EstimatedCostTotal = estimatedCostTotal,
            EstimatedMarginTotal = salesValue - estimatedCostTotal
        };
    }

    private static ManagementCockpitSummary BuildSummary(List<CockpitRow> rows)
    {
        var salesTotal = rows.Sum(x => x.SalesValueTotal);
        var costTotal = rows.Sum(x => x.EstimatedCostTotal);
        var marginTotal = rows.Sum(x => x.EstimatedMarginTotal);
        var serviceRows = rows.Where(x =>
            x.ProductGroup.Contains("service", StringComparison.OrdinalIgnoreCase) ||
            x.Name.Contains("port", StringComparison.OrdinalIgnoreCase) ||
            x.Name.Contains("zeugnis", StringComparison.OrdinalIgnoreCase)).ToList();

        return new ManagementCockpitSummary
        {
            Land = rows.Select(x => x.Land).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "-",
            Tsc = rows.Select(x => x.Tsc).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "-",
            ExtractionDate = rows.Select(x => x.ExtractionDate).FirstOrDefault(x => x.HasValue),
            RowCount = rows.Count,
            InvoiceCount = rows.Select(x => x.InvoiceNumber).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            CustomerCount = rows.Select(x => x.CustomerName).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            SalesValueTotal = salesTotal,
            EstimatedCostTotal = costTotal,
            EstimatedMarginTotal = marginTotal,
            EstimatedMarginPercent = salesTotal == 0 ? 0 : marginTotal / salesTotal * 100m,
            ServiceSharePercent = salesTotal == 0 ? 0 : serviceRows.Sum(x => x.SalesValueTotal) / salesTotal * 100m,
            MissingOrderDatePercent = rows.Count == 0 ? 0 : rows.Count(x => !x.OrderDate.HasValue) * 100m / rows.Count,
            MissingSupplierPercent = rows.Count == 0 ? 0 : rows.Count(x => string.IsNullOrWhiteSpace(x.SupplierName) && string.IsNullOrWhiteSpace(x.SupplierNumber)) * 100m / rows.Count
        };
    }

    private static List<ManagementCockpitFinding> BuildFindings(List<CockpitRow> rows)
    {
        var findings = new List<ManagementCockpitFinding>();
        var salesTotal = rows.Sum(x => x.SalesValueTotal);
        var topCustomer = rows
            .Where(x => !string.IsNullOrWhiteSpace(x.CustomerName))
            .GroupBy(x => x.CustomerName, StringComparer.OrdinalIgnoreCase)
            .Select(g => new { Customer = g.Key, Sales = g.Sum(x => x.SalesValueTotal) })
            .OrderByDescending(x => x.Sales)
            .FirstOrDefault();

        if (topCustomer is not null && salesTotal > 0)
        {
            var share = topCustomer.Sales / salesTotal * 100m;
            findings.Add(new ManagementCockpitFinding
            {
                Severity = share >= 50 ? "Warning" : "Info",
                Title = "Kundenkonzentration",
                Detail = $"{topCustomer.Customer} trägt {share:F1}% des Umsatzes."
            });
        }

        var zeroValueRows = rows.Where(x => x.SalesValueTotal == 0 || x.StandardCost == 0).ToList();
        if (zeroValueRows.Count > 0)
        {
            findings.Add(new ManagementCockpitFinding
            {
                Severity = zeroValueRows.Count >= Math.Max(3, rows.Count / 10) ? "Warning" : "Info",
                Title = "Nullwerte in Kosten oder Umsatz",
                Detail = $"{zeroValueRows.Count} Zeilen haben 0 in Umsatz oder Standard Cost und sollten fachlich geprüft werden."
            });
        }

        var missingOrderDates = rows.Count(x => !x.OrderDate.HasValue);
        if (missingOrderDates > 0)
        {
            findings.Add(new ManagementCockpitFinding
            {
                Severity = missingOrderDates > rows.Count / 2 ? "Warning" : "Info",
                Title = "Fehlende Durchlaufzeit",
                Detail = $"{missingOrderDates} von {rows.Count} Zeilen haben kein Order Date. Time-to-Invoice ist nur eingeschränkt beurteilbar."
            });
        }

        var orderLeadTimes = rows
            .Where(x => x.OrderDate.HasValue && x.InvoiceDate.HasValue)
            .Select(x => (x.InvoiceDate!.Value - x.OrderDate!.Value).TotalDays)
            .Where(x => x >= 0)
            .ToList();
        if (orderLeadTimes.Count > 0)
        {
            findings.Add(new ManagementCockpitFinding
            {
                Severity = orderLeadTimes.Average() > 120 ? "Warning" : "Info",
                Title = "Durchschnittliche Fakturierungszeit",
                Detail = $"Zwischen Order Date und Invoice Date liegen im Schnitt {orderLeadTimes.Average():F0} Tage."
            });
        }

        var missingIndustries = rows.Count(x => string.IsNullOrWhiteSpace(x.CustomerIndustry));
        if (missingIndustries > 0)
        {
            findings.Add(new ManagementCockpitFinding
            {
                Severity = missingIndustries > rows.Count / 2 ? "Warning" : "Info",
                Title = "Stammdatenlücke Customer Industry",
                Detail = $"{missingIndustries} Zeilen haben keine Customer Industry. Marktsegment-Analysen sind dadurch unvollständig."
            });
        }

        var missingIncoterms = rows.Count(x => string.IsNullOrWhiteSpace(x.Incoterms2020));
        if (missingIncoterms > 0)
        {
            findings.Add(new ManagementCockpitFinding
            {
                Severity = missingIncoterms > rows.Count / 2 ? "Info" : "Info",
                Title = "Incoterms unvollständig",
                Detail = $"{missingIncoterms} Zeilen haben keine Incoterms-Angabe."
            });
        }

        if (findings.Count == 0)
        {
            findings.Add(new ManagementCockpitFinding
            {
                Severity = "Info",
                Title = "Keine auffälligen Datenqualitätsprobleme",
                Detail = "Die Datei ist für eine erste Standortbeurteilung konsistent genug."
            });
        }

        return findings;
    }

    private static List<ManagementCockpitTopItem> BuildTopItems(
        List<CockpitRow> rows,
        Func<CockpitRow, string> keySelector,
        Func<CockpitRow, decimal> valueSelector)
    {
        var total = rows.Sum(valueSelector);
        return rows
            .Select(x => new { Label = keySelector(x), Value = valueSelector(x) })
            .Where(x => !string.IsNullOrWhiteSpace(x.Label))
            .GroupBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
            .Select(g => new ManagementCockpitTopItem
            {
                Label = g.Key,
                Value = g.Sum(x => x.Value),
                SharePercent = total == 0 ? 0 : g.Sum(x => x.Value) / total * 100m
            })
            .OrderByDescending(x => x.Value)
            .Take(5)
            .ToList();
    }

    private static Dictionary<string, int> BuildDataQualityCounts(List<CockpitRow> rows)
    {
        return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Fehlende Supplier"] = rows.Count(x => string.IsNullOrWhiteSpace(x.SupplierName) && string.IsNullOrWhiteSpace(x.SupplierNumber)),
            ["Fehlende Customer Industry"] = rows.Count(x => string.IsNullOrWhiteSpace(x.CustomerIndustry)),
            ["Fehlende Order Date"] = rows.Count(x => !x.OrderDate.HasValue),
            ["Fehlende Invoice Date"] = rows.Count(x => !x.InvoiceDate.HasValue),
            ["Null Umsatz/Kosten"] = rows.Count(x => x.SalesValueTotal == 0 || x.StandardCost == 0)
        };
    }

    private static string NormalizeHeader(string value)
    {
        var chars = value
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray();
        return new string(chars);
    }

    private static string GetText(IXLRangeRow row, IReadOnlyDictionary<string, int> headers, string key)
        => headers.TryGetValue(key, out var index) ? row.Cell(index).GetString().Trim() : string.Empty;

    private static decimal GetDecimal(IXLRangeRow row, IReadOnlyDictionary<string, int> headers, string key)
    {
        if (!headers.TryGetValue(key, out var index))
            return 0m;

        var text = row.Cell(index).GetFormattedString().Trim();
        if (decimal.TryParse(text, out var direct))
            return direct;
        if (decimal.TryParse(text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var invariant))
            return invariant;
        if (decimal.TryParse(text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.GetCultureInfo("de-CH"), out var local))
            return local;
        return 0m;
    }

    private static DateTime? GetDate(IXLRangeRow row, IReadOnlyDictionary<string, int> headers, string key)
    {
        if (!headers.TryGetValue(key, out var index))
            return null;

        var cell = row.Cell(index);
        if (cell.DataType == XLDataType.DateTime)
            return cell.GetDateTime();

        var text = cell.GetString().Trim();
        if (string.IsNullOrWhiteSpace(text))
            return null;

        if (DateTime.TryParse(text, out var direct))
            return direct;
        if (DateTime.TryParse(text, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeLocal, out var invariant))
            return invariant;
        if (DateTime.TryParse(text, System.Globalization.CultureInfo.GetCultureInfo("de-CH"), System.Globalization.DateTimeStyles.AssumeLocal, out var local))
            return local;
        return null;
    }

    private class CockpitRow
    {
        public DateTime? ExtractionDate { get; set; }
        public string Tsc { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public string PositionOnInvoice { get; set; } = string.Empty;
        public string Material { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ProductGroup { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string SupplierNumber { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string SupplierCountry { get; set; } = string.Empty;
        public string CustomerNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerCountry { get; set; } = string.Empty;
        public string CustomerIndustry { get; set; } = string.Empty;
        public decimal StandardCost { get; set; }
        public decimal SalesValueTotal { get; set; }
        public string Incoterms2020 { get; set; } = string.Empty;
        public string SalesResponsibleEmployee { get; set; } = string.Empty;
        public DateTime? InvoiceDate { get; set; }
        public DateTime? OrderDate { get; set; }
        public string Land { get; set; } = string.Empty;
        public decimal EstimatedCostTotal { get; set; }
        public decimal EstimatedMarginTotal { get; set; }
    }

    private class CentralCockpitRow
    {
        public string SourceSystem { get; set; } = string.Empty;
        public string Land { get; set; } = string.Empty;
        public string Tsc { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public string SalesCurrency { get; set; } = string.Empty;
        public decimal SalesValue { get; set; }
        public DateTime PeriodDate { get; set; }
    }
}
