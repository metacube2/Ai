using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public class ManagementCockpitService : IManagementCockpitService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ICurrencyExchangeRateService _exchangeRateService;

    public ManagementCockpitService(IDbContextFactory<AppDbContext> dbFactory)
        : this(dbFactory, new CurrencyExchangeRateService(dbFactory))
    {
    }

    public ManagementCockpitService(IDbContextFactory<AppDbContext> dbFactory, ICurrencyExchangeRateService exchangeRateService)
    {
        _dbFactory = dbFactory;
        _exchangeRateService = exchangeRateService;
    }

    private static readonly List<ValueFieldDefinition> ValueFieldDefinitions =
    [
        new()
        {
            Key = ManagementCockpitValueFieldKeys.SalesPriceValue,
            Label = "Sales Price/Value",
            IsCurrencyAmount = true,
            CurrencySource = ValueCurrencySource.Sales
        },
        new()
        {
            Key = ManagementCockpitValueFieldKeys.StandardCostTotal,
            Label = "Quantity * Standard cost",
            IsCurrencyAmount = true,
            CurrencySource = ValueCurrencySource.StandardCost
        },
        new()
        {
            Key = ManagementCockpitValueFieldKeys.StandardCost,
            Label = "Standard cost",
            IsCurrencyAmount = true,
            CurrencySource = ValueCurrencySource.StandardCost
        },
        new()
        {
            Key = ManagementCockpitValueFieldKeys.Quantity,
            Label = "Quantity",
            IsCurrencyAmount = false,
            CurrencySource = ValueCurrencySource.None
        }
    ];

    private static class ProductAssignmentStatuses
    {
        public const string Assigned = "Zugeordnet";
        public const string Unassigned = "Nicht zugeordnet";
        public const string NoReference = "Nicht im TR-AG-Stamm";
        public const string MissingMaterial = "Material fehlt";
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

    public IReadOnlyList<ManagementCockpitValueFieldOption> GetValueFieldOptions()
        => ValueFieldDefinitions
            .Select(ToValueFieldOption)
            .ToList();

    public Task<ManagementCockpitResult> AnalyzeAsync(string filePath)
        => AnalyzeAsync(filePath, null);

    public Task<ManagementCockpitResult> AnalyzeAsync(string filePath, ManagementCockpitAnalysisOptions? options)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            throw new InvalidOperationException("Die ausgewählte Excel-Datei wurde nicht gefunden.");

        var aggregation = ResolveAggregation(options);
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

        ApplyAggregation(rows, aggregation);

        var result = new ManagementCockpitResult
        {
            FilePath = filePath,
            Summary = BuildSummary(rows, aggregation),
            Findings = BuildFindings(rows, aggregation),
            TopCustomers = BuildTopItems(rows, x => x.CustomerName, x => x.AggregatedValue),
            TopProductGroups = BuildTopItems(rows, x => x.ProductGroup, x => x.AggregatedValue),
            TopSalesEmployees = BuildTopItems(rows, x => x.SalesResponsibleEmployee, x => x.AggregatedValue),
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

    public Task<ManagementCockpitCentralResult> AnalyzeCentralAsync(int year, int? month)
        => AnalyzeCentralAsync(year, month, null);

    public async Task<ManagementCockpitCentralResult> AnalyzeCentralAsync(int year, int? month, ManagementCockpitAnalysisOptions? options)
    {
        var aggregation = ResolveAggregation(options);

        using var db = await _dbFactory.CreateDbContextAsync();
        var baseRows = await db.CentralSalesRecords
            .Select(r => new CentralCockpitRow
            {
                SourceSystem = r.SourceSystem,
                Land = r.Land,
                Tsc = r.Tsc,
                InvoiceNumber = r.InvoiceNumber,
                SalesCurrency = string.IsNullOrWhiteSpace(r.SalesCurrency) ? "-" : r.SalesCurrency,
                StandardCostCurrency = string.IsNullOrWhiteSpace(r.StandardCostCurrency) ? "-" : r.StandardCostCurrency,
                Quantity = r.Quantity,
                StandardCost = r.StandardCost,
                SalesValue = r.SalesPriceValue,
                PeriodDate = r.InvoiceDate ?? r.ExtractionDate
            })
            .ToListAsync();

        if (baseRows.Count == 0)
            throw new InvalidOperationException("Die zentrale Tabelle enthält noch keine Datensätze.");

        var aggregatedRows = baseRows
            .Select(row => BuildCentralAggregationRow(row, aggregation))
            .ToList();

        var scopedRows = ApplyCentralDimensionFilters(aggregatedRows, options)
            .ToList();

        var selectedRows = scopedRows
            .Where(r => r.PeriodDate.Year == year && (!month.HasValue || r.PeriodDate.Month == month.Value))
            .ToList();

        if (selectedRows.Count == 0)
            throw new InvalidOperationException("Für den gewählten Zeitraum gibt es keine Datensätze in der zentralen Tabelle.");

        var yearlyRows = scopedRows;

        var dailyBaseRows = selectedRows
            .Where(r => month.HasValue)
            .ToList();

        return new ManagementCockpitCentralResult
        {
            Filter = new ManagementCockpitCentralFilter
            {
                Year = year,
                Month = month,
                ValueField = aggregation.ValueField.Key,
                TargetCurrency = aggregation.TargetCurrency,
                Land = NormalizeOptionalFilter(options?.LandFilter),
                Tsc = NormalizeOptionalFilter(options?.TscFilter)
            },
            Summary = new ManagementCockpitCentralSummary
            {
                RowCount = selectedRows.Count,
                InvoiceCount = selectedRows.Select(x => x.InvoiceNumber).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                SiteCount = selectedRows.Select(x => x.Tsc).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                CountryCount = selectedRows.Select(x => x.Land).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                CurrencyCount = selectedRows.Select(x => x.DisplayCurrency).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                ValueFieldKey = aggregation.ValueField.Key,
                ValueFieldLabel = aggregation.ValueField.Label,
                DisplayCurrency = BuildDisplayCurrencyLabel(selectedRows.Select(x => x.DisplayCurrency)),
                ValueTotal = selectedRows.Sum(x => x.Value),
                MissingExchangeRateCount = selectedRows.Count(x => x.MissingExchangeRate),
                PeriodStart = selectedRows.Min(x => x.PeriodDate),
                PeriodEnd = selectedRows.Max(x => x.PeriodDate)
            },
            AdditionalValueFields = aggregation.AdditionalValueFields
                .Select(ToValueFieldOption)
                .ToList(),
            Notices = BuildCentralNotices(aggregation, selectedRows.Count(x => x.MissingExchangeRate), options),
            YearlyTotals = yearlyRows
                .GroupBy(x => new { x.PeriodDate.Year, x.DisplayCurrency })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.DisplayCurrency, StringComparer.OrdinalIgnoreCase)
                .Select(g => BuildTimeValueRow(g, aggregation, g.Key.Year.ToString(), g.Key.Year, null, null, g.Key.DisplayCurrency))
                .ToList(),
            MonthlyTotals = selectedRows
                .GroupBy(x => new { x.PeriodDate.Year, x.PeriodDate.Month, x.DisplayCurrency })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month)
                .ThenBy(g => g.Key.DisplayCurrency, StringComparer.OrdinalIgnoreCase)
                .Select(g => BuildTimeValueRow(g, aggregation, $"{g.Key.Year:D4}-{g.Key.Month:D2}", g.Key.Year, g.Key.Month, null, g.Key.DisplayCurrency))
                .ToList(),
            DailyTotals = dailyBaseRows
                .GroupBy(x => new { x.PeriodDate.Year, x.PeriodDate.Month, x.PeriodDate.Day, x.DisplayCurrency })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month)
                .ThenBy(g => g.Key.Day)
                .ThenBy(g => g.Key.DisplayCurrency, StringComparer.OrdinalIgnoreCase)
                .Select(g => BuildTimeValueRow(g, aggregation, $"{g.Key.Year:D4}-{g.Key.Month:D2}-{g.Key.Day:D2}", g.Key.Year, g.Key.Month, g.Key.Day, g.Key.DisplayCurrency))
                .ToList(),
            SourceSystemTotals = selectedRows
                .GroupBy(x => new { x.SourceSystem, x.DisplayCurrency })
                .OrderBy(g => g.Key.SourceSystem, StringComparer.OrdinalIgnoreCase)
                .ThenBy(g => g.Key.DisplayCurrency, StringComparer.OrdinalIgnoreCase)
                .Select(g => new ManagementCockpitDimensionValueRow
                {
                    Label = g.Key.SourceSystem,
                    Currency = g.Key.DisplayCurrency,
                    SalesValue = g.Sum(x => x.Value),
                    RowCount = g.Count(),
                    InvoiceCount = g.Select(x => x.InvoiceNumber).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Count()
                })
                .ToList(),
            CountryTotals = selectedRows
                .GroupBy(x => new { x.Land, x.DisplayCurrency })
                .OrderByDescending(g => g.Sum(x => x.Value))
                .ThenBy(g => g.Key.Land, StringComparer.OrdinalIgnoreCase)
                .ThenBy(g => g.Key.DisplayCurrency, StringComparer.OrdinalIgnoreCase)
                .Select(g => new ManagementCockpitDimensionValueRow
                {
                    Label = g.Key.Land,
                    Currency = g.Key.DisplayCurrency,
                    SalesValue = g.Sum(x => x.Value),
                    RowCount = g.Count(),
                    InvoiceCount = g.Select(x => x.InvoiceNumber).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Count()
                })
                .ToList()
        };
    }

    public async Task<ManagementFinanceSummaryResult> AnalyzeFinanceSummaryAsync(int year, string? countryKey, string? currency)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var financeRules = await db.FinanceRules
            .AsNoTracking()
            .Where(rule => rule.IsActive)
            .OrderBy(rule => rule.SortOrder)
            .ThenBy(rule => rule.Id)
            .ToListAsync();
        if (financeRules.Count == 0)
            financeRules = FinanceRuleEngine.CreateDefaultRules().ToList();

        var financeRuleEngine = new FinanceRuleEngine(financeRules);
        var records = await db.CentralSalesRecords
            .AsNoTracking()
            .Select(r => new SalesRecord
            {
                SourceSystem = r.SourceSystem,
                Land = r.Land,
                Tsc = r.Tsc,
                DocumentEntry = r.DocumentEntry,
                InvoiceNumber = r.InvoiceNumber,
                PositionOnInvoice = r.PositionOnInvoice,
                Material = r.Material,
                Name = r.Name,
                ProductGroup = r.ProductGroup,
                ProductHierarchyCode = r.ProductHierarchyCode,
                ProductHierarchyText = r.ProductHierarchyText,
                ProductFamilyCode = r.ProductFamilyCode,
                ProductFamilyText = r.ProductFamilyText,
                ProductDivisionCode = r.ProductDivisionCode,
                ProductDivisionText = r.ProductDivisionText,
                ProductMappingAssigned = r.ProductMappingAssigned,
                Quantity = r.Quantity,
                SupplierCountry = r.SupplierCountry,
                CustomerNumber = r.CustomerNumber,
                CustomerName = r.CustomerName,
                SalesCurrency = r.SalesCurrency,
                DocumentCurrency = r.DocumentCurrency,
                CompanyCurrency = r.CompanyCurrency,
                SalesPriceValue = r.SalesPriceValue,
                DocumentType = r.DocumentType,
                PostingDate = r.PostingDate,
                InvoiceDate = r.InvoiceDate,
                ExtractionDate = r.ExtractionDate
            })
            .ToListAsync();

        if (records.Count == 0)
            throw new InvalidOperationException("Die zentrale Tabelle enthaelt noch keine Datensaetze.");

        var allRows = records
            .Select(record =>
            {
                var resolvedCountryKey = ResolveFinanceCountryKey(record.Land, record.Tsc);
                var financeDate = financeRuleEngine.ResolveFinanceDate(record, resolvedCountryKey);
                var rawInclude = financeRuleEngine.ShouldInclude(record, resolvedCountryKey);
                var value = financeRuleEngine.ResolveNetSalesActual(record, resolvedCountryKey, rawInclude);
                var include = rawInclude && value != 0m;
                return new FinanceAggregationRow
                {
                    Year = financeDate.Year,
                    CountryKey = resolvedCountryKey,
                    Land = record.Land,
                    Tsc = record.Tsc,
                    SourceSystem = string.IsNullOrWhiteSpace(record.SourceSystem) ? "-" : record.SourceSystem,
                    Currency = ResolveFinanceCurrency(record),
                    Include = include,
                    Value = value,
                    RawSalesValue = record.SalesPriceValue,
                    Quantity = record.Quantity,
                    InvoiceNumber = record.InvoiceNumber,
                    DocumentType = record.DocumentType,
                    Material = record.Material,
                    ArticleName = record.Name,
                    ProductGroup = record.ProductGroup,
                    ProductHierarchyCode = record.ProductHierarchyCode,
                    ProductHierarchyText = record.ProductHierarchyText,
                    ProductFamilyCode = record.ProductFamilyCode,
                    ProductFamilyText = record.ProductFamilyText,
                    ProductDivisionCode = record.ProductDivisionCode,
                    ProductDivisionText = record.ProductDivisionText,
                    ProductMappingAssigned = record.ProductMappingAssigned,
                    CustomerName = record.CustomerName,
                    PostingDate = record.PostingDate,
                    InvoiceDate = record.InvoiceDate,
                    ExtractionDate = record.ExtractionDate
                };
            })
            .ToList();

        var yearOptions = allRows
            .Select(row => row.Year)
            .Distinct()
            .OrderBy(yearValue => yearValue)
            .ToList();
        if (year == 0)
            year = yearOptions.LastOrDefault();

        var countryFilter = NormalizeOptionalFilter(countryKey);
        var currencyFilter = NormalizeOptionalFilter(currency);
        var scopedRows = allRows
            .Where(row => row.Year == year)
            .Where(row => countryFilter is null || row.CountryKey.Equals(countryFilter, StringComparison.OrdinalIgnoreCase))
            .Where(row => currencyFilter is null || row.Currency.Equals(currencyFilter, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var summaryRows = scopedRows
            .GroupBy(row => new { row.Year, row.CountryKey, row.Currency })
            .OrderBy(group => group.Key.CountryKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(group => group.Key.Currency, StringComparer.OrdinalIgnoreCase)
            .Select(group => BuildFinanceSummaryRow(group.Key.Year, group.Key.CountryKey, group.Key.Currency, group))
            .ToList();

        var yearRows = allRows
            .Where(row => countryFilter is null || row.CountryKey.Equals(countryFilter, StringComparison.OrdinalIgnoreCase))
            .Where(row => currencyFilter is null || row.Currency.Equals(currencyFilter, StringComparison.OrdinalIgnoreCase))
            .GroupBy(row => new { row.Year, row.Currency })
            .OrderBy(group => group.Key.Year)
            .ThenBy(group => group.Key.Currency, StringComparer.OrdinalIgnoreCase)
            .Select(group => BuildFinanceSummaryRow(group.Key.Year, "Alle", group.Key.Currency, group))
            .ToList();

        var includedRows = scopedRows.Count(row => row.Include);
        var excludedRows = scopedRows.Count(row => !row.Include);
        var resultCurrencies = summaryRows
            .Select(row => row.Currency)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var notices = new List<string>
        {
            "Diese Sicht verwendet dieselbe FinanceRuleEngine wie das zentrale Excel-Blatt Finance Summary.",
            "Jahr, Land und Waehrung werden auf das Endergebnis angewendet.",
            "Finance-Jahr basiert auf PostingDate, danach InvoiceDate, danach ExtractionDate; DE-Regeln koennen das Jahr erzwingen.",
            "Include/Exclude, Gutschriften-Negierung und IT-Deduplizierung folgen den gepflegten Finance Regeln."
        };
        if (scopedRows.Count == 0)
        {
            notices.Insert(0, "Fuer die gewaehlten Finance-Filter gibt es keine Datensaetze im aktuellen Zentraldatenbestand.");
        }

        var references = await db.FinanceReferences
            .AsNoTracking()
            .Where(reference => reference.IsActive && reference.Year == year)
            .ToListAsync();
        var referenceByKey = references
            .GroupBy(reference => reference.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Select(reference => reference.CheckValue ?? reference.LocalCurrencyValue).FirstOrDefault(value => value.HasValue),
                StringComparer.OrdinalIgnoreCase);

        var dataStatusRows = await BuildFinanceDataStatusRowsAsync(db);
        var countryRows = BuildFinanceCountryStatusRows(scopedRows, referenceByKey);
        var productAssignmentRows = BuildProductAssignmentRows(scopedRows, allRows);

        return new ManagementFinanceSummaryResult
        {
            Filter = new ManagementFinanceSummaryFilter
            {
                Year = year,
                CountryKey = countryFilter,
                Currency = currencyFilter
            },
            YearOptions = yearOptions,
            CountryOptions = allRows
                .Select(row => row.CountryKey)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            CurrencyOptions = allRows
                .Select(row => row.Currency)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Rows = summaryRows,
            YearRows = yearRows,
            IncludedRows = includedRows,
            ExcludedRows = excludedRows,
            CountryCount = summaryRows.Select(row => row.CountryKey).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            CurrencyCount = resultCurrencies.Count,
            NetSalesActual = summaryRows.Sum(row => row.NetSalesActual),
            DisplayCurrency = BuildDisplayCurrencyLabel(resultCurrencies),
            Notices = notices,
            CountryRows = countryRows,
            DeviationRows = countryRows
                .Where(row => row.Difference.HasValue)
                .OrderByDescending(row => Math.Abs(row.Difference!.Value))
                .ToList(),
            DataStatusRows = dataStatusRows,
            CreditCandidates = BuildFinanceCreditCandidates(scopedRows),
            DataQualityRows = BuildFinanceDataQualityRows(scopedRows),
            ProductAssignmentSummary = BuildProductAssignmentSummary(productAssignmentRows),
            ProductAssignmentCountryRows = BuildProductAssignmentCountryRows(productAssignmentRows),
            ProductAssignmentRows = productAssignmentRows
        };
    }

    private static async Task<List<ManagementFinanceDataStatusRow>> BuildFinanceDataStatusRowsAsync(AppDbContext db)
    {
        var sites = await db.Sites
            .AsNoTracking()
            .OrderBy(site => site.Land)
            .ThenBy(site => site.TSC)
            .ToListAsync();
        var records = await db.CentralSalesRecords
            .AsNoTracking()
            .GroupBy(record => record.Tsc)
            .Select(group => new
            {
                Tsc = group.Key,
                RowCount = group.Count(),
                LatestStoredAtUtc = group.Max(record => record.StoredAtUtc),
                LatestExtractionDate = group.Max(record => record.ExtractionDate)
            })
            .ToListAsync();
        var logs = await db.ExportLogs
            .AsNoTracking()
            .GroupBy(log => log.TSC)
            .Select(group => new
            {
                Tsc = group.Key,
                LatestTimestamp = group.Max(log => log.Timestamp)
            })
            .ToListAsync();
        var latestLogTimes = logs.ToDictionary(x => x.Tsc, x => x.LatestTimestamp, StringComparer.OrdinalIgnoreCase);
        var latestLogs = await db.ExportLogs
            .AsNoTracking()
            .Where(log => logs.Select(x => x.LatestTimestamp).Contains(log.Timestamp))
            .ToListAsync();
        var recordByTsc = records.ToDictionary(x => x.Tsc, StringComparer.OrdinalIgnoreCase);
        var logByTsc = latestLogs
            .Where(log => latestLogTimes.TryGetValue(log.TSC, out var timestamp) && log.Timestamp == timestamp)
            .GroupBy(log => log.TSC, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(log => log.Id).First(), StringComparer.OrdinalIgnoreCase);

        return sites.Select(site =>
        {
            recordByTsc.TryGetValue(site.TSC, out var record);
            logByTsc.TryGetValue(site.TSC, out var log);
            return new ManagementFinanceDataStatusRow
            {
                Land = site.Land,
                Tsc = site.TSC,
                SourceSystem = site.SourceSystem,
                IsActive = site.IsActive,
                RowCount = record?.RowCount ?? 0,
                LatestStoredAtUtc = record?.LatestStoredAtUtc,
                LatestExtractionDate = record?.LatestExtractionDate,
                LatestExportAt = log?.Timestamp,
                LatestExportStatus = log?.Status ?? string.Empty,
                ManualImportFilePath = site.ManualImportFilePath,
                ManualImportLastUploadedAtUtc = site.ManualImportLastUploadedAtUtc
            };
        }).ToList();
    }

    private static List<ManagementFinanceCountryStatusRow> BuildFinanceCountryStatusRows(
        IReadOnlyCollection<FinanceAggregationRow> rows,
        IReadOnlyDictionary<string, decimal?> referenceByKey)
        => rows
            .GroupBy(row => new { row.Year, row.CountryKey, row.Currency })
            .OrderBy(group => group.Key.CountryKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(group => group.Key.Currency, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var rowList = group.ToList();
                referenceByKey.TryGetValue(group.Key.CountryKey, out var referenceValue);
                var actual = rowList.Sum(row => row.Value);
                var difference = referenceValue.HasValue ? actual - referenceValue.Value : (decimal?)null;
                return new ManagementFinanceCountryStatusRow
                {
                    Year = group.Key.Year,
                    CountryKey = group.Key.CountryKey,
                    Currency = group.Key.Currency,
                    IncludedRows = rowList.Count(row => row.Include),
                    ExcludedRows = rowList.Count(row => !row.Include),
                    NetSalesActual = actual,
                    SourceSystems = JoinDistinct(rowList.Select(row => row.SourceSystem)),
                    Tscs = JoinDistinct(rowList.Select(row => row.Tsc)),
                    ReferenceValue = referenceValue,
                    Difference = difference,
                    DifferencePercent = referenceValue is > 0m && difference.HasValue ? difference.Value / referenceValue.Value * 100m : null,
                    Status = BuildFinanceStatus(difference)
                };
            })
            .ToList();

    private static List<ManagementFinanceCreditCandidateRow> BuildFinanceCreditCandidates(IEnumerable<FinanceAggregationRow> rows)
        => rows
            .Where(row => row.Value < 0m || row.RawSalesValue < 0m || LooksLikeCreditDocument(row.DocumentType, row.InvoiceNumber))
            .GroupBy(row => new { row.CountryKey, row.Tsc, row.InvoiceNumber, row.DocumentType, row.Currency })
            .Select(group =>
            {
                var rowList = group.ToList();
                return new ManagementFinanceCreditCandidateRow
                {
                    CountryKey = group.Key.CountryKey,
                    Tsc = group.Key.Tsc,
                    InvoiceNumber = group.Key.InvoiceNumber,
                    DocumentType = group.Key.DocumentType,
                    Currency = group.Key.Currency,
                    NetSalesActual = rowList.Sum(row => row.Value),
                    Quantity = rowList.Sum(row => row.Quantity),
                    Reason = BuildCreditReason(rowList)
                };
            })
            .OrderBy(row => row.NetSalesActual)
            .Take(100)
            .ToList();

    private static List<ManagementFinanceDataQualityRow> BuildFinanceDataQualityRows(IReadOnlyCollection<FinanceAggregationRow> rows)
    {
        var rowCount = rows.Count;
        return new List<ManagementFinanceDataQualityRow>
        {
            BuildQualityRow("Fehlende Materialnummer", rows.Count(row => string.IsNullOrWhiteSpace(row.Material)), rowCount),
            BuildQualityRow("Fehlende ProductGroup", rows.Count(row => string.IsNullOrWhiteSpace(row.ProductGroup)), rowCount),
            BuildQualityRow("Fehlende Waehrung", rows.Count(row => string.IsNullOrWhiteSpace(row.Currency) || row.Currency == "-"), rowCount),
            BuildQualityRow("Fehlender Kunde", rows.Count(row => string.IsNullOrWhiteSpace(row.CustomerName)), rowCount),
            BuildQualityRow("Fehlendes Rechnungsdatum", rows.Count(row => !row.InvoiceDate.HasValue), rowCount),
            BuildQualityRow("Fehlendes Buchungsdatum", rows.Count(row => !row.PostingDate.HasValue), rowCount),
            BuildQualityRow("Nullwerte im Finance-Wert", rows.Count(row => row.Value == 0m), rowCount),
            BuildQualityRow("Ausgeschlossene Zeilen", rows.Count(row => !row.Include), rowCount)
        }
        .Where(row => row.Count > 0)
        .OrderByDescending(row => row.Count)
        .ThenBy(row => row.Issue, StringComparer.OrdinalIgnoreCase)
        .ToList();
    }

    private static List<ManagementProductAssignmentRow> BuildProductAssignmentRows(
        IReadOnlyCollection<FinanceAggregationRow> scopedRows,
        IReadOnlyCollection<FinanceAggregationRow> allRows)
    {
        var referenceByMaterial = allRows
            .Where(row => !string.IsNullOrWhiteSpace(row.Material))
            .Where(row => HasProductReference(row))
            .GroupBy(row => NormalizeMaterialKey(row.Material), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(row => IsAssignedProductReference(row))
                    .ThenBy(row => row.Tsc, StringComparer.OrdinalIgnoreCase)
                    .First(),
                StringComparer.OrdinalIgnoreCase);

        return scopedRows
            .GroupBy(row => new
            {
                MaterialKey = NormalizeMaterialKey(row.Material),
                row.Material,
                row.ArticleName,
                row.CountryKey,
                row.Tsc,
                row.SourceSystem,
                row.Currency
            })
            .Select(group =>
            {
                var material = group.Key.Material?.Trim() ?? string.Empty;
                referenceByMaterial.TryGetValue(group.Key.MaterialKey, out var reference);
                var status = BuildProductAssignmentStatus(material, reference);
                return new ManagementProductAssignmentRow
                {
                    Status = status,
                    CountryKey = group.Key.CountryKey,
                    Tsc = group.Key.Tsc,
                    SourceSystem = group.Key.SourceSystem,
                    Material = material,
                    ArticleName = group.Key.ArticleName,
                    ReferenceMaterial = reference?.Material ?? string.Empty,
                    ProductHierarchyCode = reference?.ProductHierarchyCode ?? string.Empty,
                    ProductHierarchyText = reference?.ProductHierarchyText ?? string.Empty,
                    ProductFamilyCode = reference?.ProductFamilyCode ?? string.Empty,
                    ProductFamilyText = reference?.ProductFamilyText ?? string.Empty,
                    ProductDivisionCode = reference?.ProductDivisionCode ?? string.Empty,
                    ProductDivisionText = reference?.ProductDivisionText ?? string.Empty,
                    ProductMappingAssigned = reference?.ProductMappingAssigned ?? string.Empty,
                    RowCount = group.Count(),
                    NetSalesActual = group.Sum(row => row.Value),
                    Currency = group.Key.Currency
                };
            })
            .OrderBy(row => ProductAssignmentStatusSort(row.Status))
            .ThenBy(row => row.CountryKey, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(row => Math.Abs(row.NetSalesActual))
            .ThenBy(row => row.Material, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static ManagementProductAssignmentSummary BuildProductAssignmentSummary(IReadOnlyCollection<ManagementProductAssignmentRow> rows)
        => new()
        {
            DistinctMaterialCount = rows.Count,
            MatchedMaterialCount = rows.Count(row => row.Status == ProductAssignmentStatuses.Assigned),
            UnassignedMaterialCount = rows.Count(row => row.Status == ProductAssignmentStatuses.Unassigned),
            MissingReferenceMaterialCount = rows.Count(row => row.Status == ProductAssignmentStatuses.NoReference),
            MissingMaterialNumberCount = rows.Count(row => row.Status == ProductAssignmentStatuses.MissingMaterial),
            ReferenceMaterialCount = rows
                .Where(row => !string.IsNullOrWhiteSpace(row.ReferenceMaterial))
                .Select(row => row.ReferenceMaterial)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count()
        };

    private static List<ManagementProductAssignmentCountryRow> BuildProductAssignmentCountryRows(IEnumerable<ManagementProductAssignmentRow> rows)
        => rows
            .GroupBy(row => new { row.CountryKey, row.Tsc })
            .OrderBy(group => group.Key.CountryKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(group => group.Key.Tsc, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var rowList = group.ToList();
                var matched = rowList.Count(row => row.Status == ProductAssignmentStatuses.Assigned);
                var relevant = rowList.Count(row => row.Status != ProductAssignmentStatuses.MissingMaterial);
                return new ManagementProductAssignmentCountryRow
                {
                    CountryKey = group.Key.CountryKey,
                    Tsc = group.Key.Tsc,
                    DistinctMaterialCount = rowList.Count,
                    MatchedMaterialCount = matched,
                    UnassignedMaterialCount = rowList.Count(row => row.Status == ProductAssignmentStatuses.Unassigned),
                    MissingReferenceMaterialCount = rowList.Count(row => row.Status == ProductAssignmentStatuses.NoReference),
                    MissingMaterialNumberCount = rowList.Count(row => row.Status == ProductAssignmentStatuses.MissingMaterial),
                    MatchPercent = relevant == 0 ? 0m : matched * 100m / relevant
                };
            })
            .ToList();

    private static string BuildProductAssignmentStatus(string material, FinanceAggregationRow? reference)
    {
        if (string.IsNullOrWhiteSpace(material))
            return ProductAssignmentStatuses.MissingMaterial;
        if (reference is null)
            return ProductAssignmentStatuses.NoReference;
        return IsAssignedProductReference(reference)
            ? ProductAssignmentStatuses.Assigned
            : ProductAssignmentStatuses.Unassigned;
    }

    private static bool HasProductReference(FinanceAggregationRow row)
        => !string.IsNullOrWhiteSpace(row.ProductHierarchyCode) ||
           !string.IsNullOrWhiteSpace(row.ProductFamilyCode) ||
           !string.IsNullOrWhiteSpace(row.ProductDivisionCode) ||
           !string.IsNullOrWhiteSpace(row.ProductMappingAssigned);

    private static bool IsAssignedProductReference(FinanceAggregationRow row)
        => IsTruthy(row.ProductMappingAssigned) &&
           !string.IsNullOrWhiteSpace(row.ProductDivisionCode) &&
           !string.Equals(row.ProductDivisionCode, "UNASS", StringComparison.OrdinalIgnoreCase);

    private static bool IsTruthy(string value)
        => value.Equals("X", StringComparison.OrdinalIgnoreCase) ||
           value.Equals("TRUE", StringComparison.OrdinalIgnoreCase) ||
           value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
           value.Equals("JA", StringComparison.OrdinalIgnoreCase);

    private static int ProductAssignmentStatusSort(string status) => status switch
    {
        ProductAssignmentStatuses.NoReference => 0,
        ProductAssignmentStatuses.Unassigned => 1,
        ProductAssignmentStatuses.MissingMaterial => 2,
        _ => 3
    };

    private static string NormalizeMaterialKey(string value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();

    private static ManagementFinanceDataQualityRow BuildQualityRow(string issue, int count, int totalRows)
    {
        var share = totalRows == 0 ? 0m : count / (decimal)totalRows;
        return new ManagementFinanceDataQualityRow
        {
            Issue = issue,
            Count = count,
            Severity = count == 0 ? "Info" : share >= 0.2m ? "Warning" : "Info"
        };
    }

    private static string BuildFinanceStatus(decimal? difference)
    {
        if (!difference.HasValue)
            return "Kein Sollwert";

        return Math.Abs(difference.Value) <= 1m ? "OK" : "Pruefen";
    }

    private static bool LooksLikeCreditDocument(string documentType, string invoiceNumber)
    {
        var text = $"{documentType} {invoiceNumber}".Trim();
        return text.Contains("credit", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("gutsch", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("storno", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("abono", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("rec", StringComparison.OrdinalIgnoreCase) ||
               invoiceNumber.StartsWith("GS", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildCreditReason(IEnumerable<FinanceAggregationRow> rows)
    {
        var rowList = rows.ToList();
        var reasons = new List<string>();
        if (rowList.Any(row => row.Value < 0m))
            reasons.Add("negativer Finance-Wert");
        if (rowList.Any(row => row.RawSalesValue < 0m))
            reasons.Add("negativer Rohwert");
        if (rowList.Any(row => LooksLikeCreditDocument(row.DocumentType, row.InvoiceNumber)))
            reasons.Add("Belegtyp/-nummer");
        return string.Join(", ", reasons.Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static string JoinDistinct(IEnumerable<string> values)
    {
        var distinct = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return distinct.Count == 0 ? "-" : string.Join(", ", distinct);
    }

    private static IEnumerable<CentralAggregationRow> ApplyCentralDimensionFilters(
        IEnumerable<CentralAggregationRow> rows,
        ManagementCockpitAnalysisOptions? options)
    {
        var landFilter = NormalizeOptionalFilter(options?.LandFilter);
        var tscFilter = NormalizeOptionalFilter(options?.TscFilter);

        return rows.Where(row =>
            (landFilter is null || string.Equals(row.Land, landFilter, StringComparison.OrdinalIgnoreCase)) &&
            (tscFilter is null || string.Equals(row.Tsc, tscFilter, StringComparison.OrdinalIgnoreCase)));
    }

    private static ManagementFinanceSummaryRow BuildFinanceSummaryRow(
        int year,
        string countryKey,
        string currency,
        IEnumerable<FinanceAggregationRow> rows)
    {
        var rowList = rows.ToList();
        return new ManagementFinanceSummaryRow
        {
            Year = year,
            CountryKey = countryKey,
            Currency = currency,
            IncludedRows = rowList.Count(row => row.Include),
            ExcludedRows = rowList.Count(row => !row.Include),
            NetSalesActual = rowList.Sum(row => row.Value)
        };
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

    private static IEnumerable<string> GetCandidateDirectories(ExportSettings settings)
    {
        yield return Path.Combine(AppContext.BaseDirectory, "output");

        if (!string.IsNullOrWhiteSpace(settings.LocalSiteExportFolder))
            yield return settings.LocalSiteExportFolder.Trim();

        if (!string.IsNullOrWhiteSpace(settings.LocalConsolidatedExportFolder))
            yield return settings.LocalConsolidatedExportFolder.Trim();
    }

    private AggregationSelection ResolveAggregation(ManagementCockpitAnalysisOptions? options)
    {
        var selectedField = ValueFieldDefinitions.FirstOrDefault(x =>
                string.Equals(x.Key, options?.ValueField, StringComparison.OrdinalIgnoreCase))
            ?? ValueFieldDefinitions.First(x => x.Key == ManagementCockpitValueFieldKeys.SalesPriceValue);

        var additionalFields = (options?.AdditionalValueFields ?? [])
            .Select(key => ValueFieldDefinitions.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase)))
            .Where(x => x is not null && !string.Equals(x.Key, selectedField.Key, StringComparison.OrdinalIgnoreCase))
            .Cast<ValueFieldDefinition>()
            .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        var targetCurrency = (options?.TargetCurrency ?? ManagementCockpitCurrencyOptions.Native).Trim().ToUpperInvariant();
        if (targetCurrency is not ManagementCockpitCurrencyOptions.Eur and not ManagementCockpitCurrencyOptions.Usd)
            targetCurrency = ManagementCockpitCurrencyOptions.Native;

        return new AggregationSelection(
            selectedField,
            additionalFields,
            targetCurrency,
            new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase));
    }

    private void ApplyAggregation(List<CockpitRow> rows, AggregationSelection aggregation)
    {
        foreach (var row in rows)
        {
            var value = ResolveValue(row, aggregation.ValueField);
            var currency = ResolveCurrency(row, aggregation.ValueField);
            var converted = ConvertValue(value, currency, aggregation.ValueField, aggregation, row.InvoiceDate ?? row.OrderDate ?? row.ExtractionDate);

            row.AggregatedValue = converted.Value;
            row.AggregatedCurrency = converted.DisplayCurrency;
            row.MissingExchangeRate = converted.MissingExchangeRate;
        }
    }

    private CentralAggregationRow BuildCentralAggregationRow(CentralCockpitRow row, AggregationSelection aggregation)
    {
        var value = ResolveValue(row, aggregation.ValueField);
        var currency = ResolveCurrency(row, aggregation.ValueField);
        var converted = ConvertValue(value, currency, aggregation.ValueField, aggregation, row.PeriodDate);
        var additionalValues = aggregation.AdditionalValueFields.ToDictionary(
            field => field.Key,
            field =>
            {
                var additionalValue = ResolveValue(row, field);
                var additionalCurrency = ResolveCurrency(row, field);
                return ConvertValue(additionalValue, additionalCurrency, field, aggregation, row.PeriodDate);
            },
            StringComparer.OrdinalIgnoreCase);

        return new CentralAggregationRow
        {
            SourceSystem = row.SourceSystem,
            Land = row.Land,
            Tsc = row.Tsc,
            InvoiceNumber = row.InvoiceNumber,
            PeriodDate = row.PeriodDate,
            Value = converted.Value,
            DisplayCurrency = converted.DisplayCurrency,
            MissingExchangeRate = converted.MissingExchangeRate,
            AdditionalValues = additionalValues
        };
    }

    private ConvertedValue ConvertValue(decimal value, string sourceCurrency, ValueFieldDefinition field, AggregationSelection aggregation, DateTime? effectiveDate)
    {
        if (!field.IsCurrencyAmount)
            return new ConvertedValue(value, "-", false);

        var normalizedSource = _exchangeRateService.NormalizeCurrencyCode(sourceCurrency);
        if (string.IsNullOrWhiteSpace(normalizedSource) || normalizedSource == "-")
        {
            normalizedSource = "-";
            if (aggregation.TargetCurrency != ManagementCockpitCurrencyOptions.Native)
                return new ConvertedValue(0m, aggregation.TargetCurrency, true);
        }

        if (aggregation.TargetCurrency == ManagementCockpitCurrencyOptions.Native)
            return new ConvertedValue(value, normalizedSource, false);

        if (string.Equals(normalizedSource, aggregation.TargetCurrency, StringComparison.OrdinalIgnoreCase))
            return new ConvertedValue(value, aggregation.TargetCurrency, false);

        var rateDate = (effectiveDate ?? DateTime.UtcNow).Date;
        var cacheKey = BuildRateCacheKey(normalizedSource, aggregation.TargetCurrency, rateDate);
        if (!aggregation.RateCache.TryGetValue(cacheKey, out var rate))
        {
            rate = _exchangeRateService.ResolveRate(normalizedSource, aggregation.TargetCurrency, rateDate);
            aggregation.RateCache[cacheKey] = rate;
        }

        if (!rate.HasValue)
            return new ConvertedValue(0m, aggregation.TargetCurrency, true);

        return new ConvertedValue(value * rate.Value, aggregation.TargetCurrency, false);
    }

    private static string BuildRateCacheKey(string fromCurrency, string toCurrency, DateTime date)
        => $"{fromCurrency}|{toCurrency}|{date:yyyy-MM-dd}";

    private static decimal ResolveValue(CockpitRow row, ValueFieldDefinition field)
        => field.Key switch
        {
            ManagementCockpitValueFieldKeys.Quantity => row.Quantity,
            ManagementCockpitValueFieldKeys.StandardCost => row.StandardCost,
            ManagementCockpitValueFieldKeys.StandardCostTotal => row.EstimatedCostTotal,
            _ => row.SalesValueTotal
        };

    private static decimal ResolveValue(CentralCockpitRow row, ValueFieldDefinition field)
        => field.Key switch
        {
            ManagementCockpitValueFieldKeys.Quantity => row.Quantity,
            ManagementCockpitValueFieldKeys.StandardCost => row.StandardCost,
            ManagementCockpitValueFieldKeys.StandardCostTotal => row.Quantity != 0m ? row.Quantity * row.StandardCost : row.StandardCost,
            _ => row.SalesValue
        };

    private static string ResolveCurrency(CockpitRow row, ValueFieldDefinition field)
        => field.CurrencySource switch
        {
            ValueCurrencySource.StandardCost => row.StandardCostCurrency,
            ValueCurrencySource.Sales => row.SalesCurrency,
            _ => "-"
        };

    private static string ResolveCurrency(CentralCockpitRow row, ValueFieldDefinition field)
        => field.CurrencySource switch
        {
            ValueCurrencySource.StandardCost => row.StandardCostCurrency,
            ValueCurrencySource.Sales => row.SalesCurrency,
            _ => "-"
        };

    private static string BuildDisplayCurrencyLabel(IEnumerable<string> currencies)
    {
        var distinct = currencies
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return distinct.Count switch
        {
            0 => "-",
            1 => distinct[0],
            _ => "Mixed"
        };
    }

    private static List<string> BuildCentralNotices(
        AggregationSelection aggregation,
        int missingExchangeRateCount,
        ManagementCockpitAnalysisOptions? options)
    {
        var notices = new List<string>
        {
            "Roh-Auswertung aus CentralSalesRecords.",
            $"Summenfeld: {aggregation.ValueField.Label}.",
            "Keine Intercompany-Bereinigung angewendet.",
            "Kein Budget- und kein Spartemapping angewendet.",
            "Periodenlogik basiert auf Invoice Date, falls vorhanden, sonst auf Extraction Date."
        };

        var landFilter = NormalizeOptionalFilter(options?.LandFilter);
        var tscFilter = NormalizeOptionalFilter(options?.TscFilter);
        if (landFilter is not null || tscFilter is not null)
        {
            notices.Add($"Filter aus Auswahl: Land {(landFilter ?? "alle")}, TSC {(tscFilter ?? "alle")}.");
        }

        if (aggregation.AdditionalValueFields.Count > 0)
            notices.Add($"Weitere Summenfelder: {string.Join(", ", aggregation.AdditionalValueFields.Select(x => x.Label))}.");

        if (!aggregation.ValueField.IsCurrencyAmount)
        {
            notices.Add("Das gewaehlte Summenfeld ist kein Waehrungsbetrag; die Anzeige-Waehrung wird ignoriert.");
        }
        else if (aggregation.TargetCurrency == ManagementCockpitCurrencyOptions.Native)
        {
            notices.Add("Keine Waehrungsumrechnung angewendet; Werte bleiben in der jeweiligen Quellwaehrung.");
        }
        else
        {
            notices.Add($"Betragswerte werden in {aggregation.TargetCurrency} angezeigt.");
            if (missingExchangeRateCount > 0)
                notices.Add($"{missingExchangeRateCount} Zeilen hatten keinen passenden Wechselkurs und sind in den Summen mit 0 enthalten.");
        }

        return notices;
    }

    private static string? NormalizeOptionalFilter(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ManagementCockpitTimeValueRow BuildTimeValueRow(
        IEnumerable<CentralAggregationRow> groupRows,
        AggregationSelection aggregation,
        string label,
        int? year,
        int? month,
        int? day,
        string currency)
    {
        var rows = groupRows.ToList();
        return new ManagementCockpitTimeValueRow
        {
            Label = label,
            Year = year,
            Month = month,
            Day = day,
            Currency = currency,
            SalesValue = rows.Sum(x => x.Value),
            AdditionalValues = BuildAdditionalValues(rows, aggregation),
            RowCount = rows.Count
        };
    }

    private static Dictionary<string, ManagementCockpitAggregatedFieldValue> BuildAdditionalValues(
        IReadOnlyCollection<CentralAggregationRow> rows,
        AggregationSelection aggregation)
    {
        var result = new Dictionary<string, ManagementCockpitAggregatedFieldValue>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in aggregation.AdditionalValueFields)
        {
            var values = rows
                .Select(row => row.AdditionalValues.TryGetValue(field.Key, out var value) ? value : new ConvertedValue(0m, "-", false))
                .ToList();

            result[field.Key] = new ManagementCockpitAggregatedFieldValue
            {
                FieldKey = field.Key,
                Label = field.Label,
                Currency = BuildDisplayCurrencyLabel(values.Select(x => x.DisplayCurrency)),
                Value = values.Sum(x => x.Value),
                MissingExchangeRateCount = values.Count(x => x.MissingExchangeRate)
            };
        }

        return result;
    }

    private static ManagementCockpitValueFieldOption ToValueFieldOption(ValueFieldDefinition field)
        => new()
        {
            Key = field.Key,
            Label = field.Label,
            IsCurrencyAmount = field.IsCurrencyAmount
        };

    private static CockpitRow ReadRow(IXLRangeRow row, IReadOnlyDictionary<string, int> headers)
    {
        var quantity = GetDecimal(row, headers, "quantity");
        var standardCost = GetDecimal(row, headers, "standardcost");
        var salesValue = GetDecimal(row, headers, "salespricevalue");
        var estimatedCostTotal = quantity != 0m ? quantity * standardCost : standardCost;

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
            StandardCostCurrency = GetText(row, headers, "standardcostcurrency"),
            SalesValueTotal = salesValue,
            SalesCurrency = GetText(row, headers, "salescurrency"),
            Incoterms2020 = GetText(row, headers, "incoterms2020"),
            SalesResponsibleEmployee = GetText(row, headers, "salesresponsibleemployee"),
            InvoiceDate = GetDate(row, headers, "invoicedate"),
            OrderDate = GetDate(row, headers, "orderdate"),
            Land = GetText(row, headers, "land"),
            EstimatedCostTotal = estimatedCostTotal,
            EstimatedMarginTotal = salesValue - estimatedCostTotal
        };
    }

    private static ManagementCockpitSummary BuildSummary(List<CockpitRow> rows, AggregationSelection aggregation)
    {
        var aggregatedTotal = rows.Sum(x => x.AggregatedValue);
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
            ValueFieldKey = aggregation.ValueField.Key,
            ValueFieldLabel = aggregation.ValueField.Label,
            DisplayCurrency = BuildDisplayCurrencyLabel(rows.Select(x => x.AggregatedCurrency)),
            MissingExchangeRateCount = rows.Count(x => x.MissingExchangeRate),
            AggregatedValueTotal = aggregatedTotal,
            SalesValueTotal = aggregatedTotal,
            EstimatedCostTotal = costTotal,
            EstimatedMarginTotal = marginTotal,
            EstimatedMarginPercent = salesTotal == 0 ? 0 : marginTotal / salesTotal * 100m,
            ServiceSharePercent = salesTotal == 0 ? 0 : serviceRows.Sum(x => x.SalesValueTotal) / salesTotal * 100m,
            MissingOrderDatePercent = rows.Count == 0 ? 0 : rows.Count(x => !x.OrderDate.HasValue) * 100m / rows.Count,
            MissingSupplierPercent = rows.Count == 0 ? 0 : rows.Count(x => string.IsNullOrWhiteSpace(x.SupplierName) && string.IsNullOrWhiteSpace(x.SupplierNumber)) * 100m / rows.Count
        };
    }

    private static List<ManagementCockpitFinding> BuildFindings(List<CockpitRow> rows, AggregationSelection aggregation)
    {
        var findings = new List<ManagementCockpitFinding>();
        var salesTotal = rows.Sum(x => x.AggregatedValue);
        var topCustomer = rows
            .Where(x => !string.IsNullOrWhiteSpace(x.CustomerName))
            .GroupBy(x => x.CustomerName, StringComparer.OrdinalIgnoreCase)
            .Select(g => new { Customer = g.Key, Sales = g.Sum(x => x.AggregatedValue) })
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

        var missingExchangeRateRows = rows.Count(x => x.MissingExchangeRate);
        if (missingExchangeRateRows > 0)
        {
            findings.Add(new ManagementCockpitFinding
            {
                Severity = "Warning",
                Title = "Fehlende Wechselkurse",
                Detail = $"{missingExchangeRateRows} Zeilen konnten nicht in die gewaehlte Anzeige-Waehrung umgerechnet werden."
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
        public string StandardCostCurrency { get; set; } = string.Empty;
        public decimal SalesValueTotal { get; set; }
        public string SalesCurrency { get; set; } = string.Empty;
        public string Incoterms2020 { get; set; } = string.Empty;
        public string SalesResponsibleEmployee { get; set; } = string.Empty;
        public DateTime? InvoiceDate { get; set; }
        public DateTime? OrderDate { get; set; }
        public string Land { get; set; } = string.Empty;
        public decimal EstimatedCostTotal { get; set; }
        public decimal EstimatedMarginTotal { get; set; }
        public decimal AggregatedValue { get; set; }
        public string AggregatedCurrency { get; set; } = string.Empty;
        public bool MissingExchangeRate { get; set; }
    }

    private class CentralCockpitRow
    {
        public string SourceSystem { get; set; } = string.Empty;
        public string Land { get; set; } = string.Empty;
        public string Tsc { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public string SalesCurrency { get; set; } = string.Empty;
        public string StandardCostCurrency { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal StandardCost { get; set; }
        public decimal SalesValue { get; set; }
        public DateTime PeriodDate { get; set; }
    }

    private class CentralAggregationRow
    {
        public string SourceSystem { get; set; } = string.Empty;
        public string Land { get; set; } = string.Empty;
        public string Tsc { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime PeriodDate { get; set; }
        public decimal Value { get; set; }
        public string DisplayCurrency { get; set; } = string.Empty;
        public bool MissingExchangeRate { get; set; }
        public Dictionary<string, ConvertedValue> AdditionalValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private class FinanceAggregationRow
    {
        public int Year { get; set; }
        public string CountryKey { get; set; } = string.Empty;
        public string Land { get; set; } = string.Empty;
        public string Tsc { get; set; } = string.Empty;
        public string SourceSystem { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public bool Include { get; set; }
        public decimal Value { get; set; }
        public decimal RawSalesValue { get; set; }
        public decimal Quantity { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string Material { get; set; } = string.Empty;
        public string ArticleName { get; set; } = string.Empty;
        public string ProductGroup { get; set; } = string.Empty;
        public string ProductHierarchyCode { get; set; } = string.Empty;
        public string ProductHierarchyText { get; set; } = string.Empty;
        public string ProductFamilyCode { get; set; } = string.Empty;
        public string ProductFamilyText { get; set; } = string.Empty;
        public string ProductDivisionCode { get; set; } = string.Empty;
        public string ProductDivisionText { get; set; } = string.Empty;
        public string ProductMappingAssigned { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public DateTime? PostingDate { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public DateTime ExtractionDate { get; set; }
    }

    private sealed record AggregationSelection(
        ValueFieldDefinition ValueField,
        IReadOnlyList<ValueFieldDefinition> AdditionalValueFields,
        string TargetCurrency,
        Dictionary<string, decimal?> RateCache);

    private sealed record ConvertedValue(decimal Value, string DisplayCurrency, bool MissingExchangeRate);

    private sealed class ValueFieldDefinition
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public bool IsCurrencyAmount { get; set; }
        public ValueCurrencySource CurrencySource { get; set; }
    }

    private enum ValueCurrencySource
    {
        None,
        Sales,
        StandardCost
    }
}
