using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface ICentralSalesDataProvider
{
    Task<List<SalesRecord>> GetRecordsAsync();
    Task<List<SalesRecord>> GetLatestRecordsBySiteAsync();
    Task<bool> UsesAuditCsvAsync();

    /// <summary>
    /// Kennzeichen des aktuellen Quellstands: aendert sich, sobald eine Audit-CSV neu geschrieben
    /// wird oder die zentrale Tabelle neue Zeilen bekommt. Billig zu bilden (Dateiliste bzw. eine
    /// Zaehlabfrage), damit ein Aufrufer grosse Datenmengen nur bei geaendertem Stand neu liest.
    /// <c>null</c> heisst: nicht bestimmbar, nicht zwischenspeichern.
    /// </summary>
    Task<string?> GetSourceStampAsync() => Task.FromResult<string?>(null);
}

public sealed class CentralSalesDataProvider : ICentralSalesDataProvider
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ICentralSalesRecordService _centralSalesRecordService;
    private readonly IExportAuditCsvService _auditCsvService;

    public CentralSalesDataProvider(
        IDbContextFactory<AppDbContext> dbFactory,
        ICentralSalesRecordService centralSalesRecordService,
        IExportAuditCsvService auditCsvService)
    {
        _dbFactory = dbFactory;
        _centralSalesRecordService = centralSalesRecordService;
        _auditCsvService = auditCsvService;
    }

    public async Task<string?> GetSourceStampAsync()
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var settings = await db.ExportSettings.AsNoTracking().FirstOrDefaultAsync() ?? new ExportSettings();
        if (!settings.UseAuditCsvAsCentralSource)
        {
            var count = await db.CentralSalesRecords.CountAsync();
            var maxId = count == 0 ? 0 : await db.CentralSalesRecords.MaxAsync(r => r.Id);
            var maxStored = count == 0 ? DateTime.MinValue : await db.CentralSalesRecords.MaxAsync(r => r.StoredAtUtc);
            return $"db|{count}|{maxId}|{maxStored:O}";
        }

        // Dieselben Ordner, aus denen ExportAuditCsvService liest: Standort-CSVs und konsolidierte
        // CSV. Jede neu geschriebene oder geloeschte CSV aendert das Kennzeichen.
        var siteDirectory = _auditCsvService.ResolveAuditCsvDirectory(settings);
        var consolidatedDirectory = !string.IsNullOrWhiteSpace(settings.LocalConsolidatedExportFolder)
            ? settings.LocalConsolidatedExportFolder.Trim()
            : siteDirectory;
        var files = new[] { siteDirectory, consolidatedDirectory }
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(Directory.Exists)
            .SelectMany(directory => new DirectoryInfo(directory).EnumerateFiles("*.csv"))
            .OrderBy(file => file.FullName, StringComparer.OrdinalIgnoreCase)
            .Select(file => $"{file.FullName}|{file.Length}|{file.LastWriteTimeUtc.Ticks}");
        return "csv|" + string.Join(";", files);
    }

    public async Task<List<SalesRecord>> GetRecordsAsync()
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var settings = await db.ExportSettings.AsNoTracking().FirstOrDefaultAsync() ?? new ExportSettings();
        if (!settings.UseAuditCsvAsCentralSource)
            return await _centralSalesRecordService.GetAllAsync();

        var siteRecords = await _auditCsvService.ReadLatestSiteAuditCsvRecordsAsync(settings);
        List<SalesRecord> records;
        if (siteRecords.Count == 0)
        {
            // Keine Standort-CSV: komplett auf die konsolidierte Audit-CSV zurueckfallen.
            records = await _auditCsvService.ReadLatestConsolidatedAuditCsvRecordsAsync(settings);
        }
        else
        {
            // Standort-CSV liegen nur fuer einen Teil der TSC vor. Die uebrigen TSC duerfen nicht
            // still verschwinden: fehlende TSC aus der konsolidierten Audit-CSV ergaenzen.
            records = new List<SalesRecord>(siteRecords);
            var coveredTsc = siteRecords
                .Where(r => !string.IsNullOrWhiteSpace(r.Tsc))
                .Select(r => r.Tsc.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var consolidated = await _auditCsvService.ReadLatestConsolidatedAuditCsvRecordsAsync(settings);
            records.AddRange(consolidated
                .Where(r => !string.IsNullOrWhiteSpace(r.Tsc) && !coveredTsc.Contains(r.Tsc.Trim())));
        }

        if (records.Count == 0)
        {
            var directory = _auditCsvService.ResolveAuditCsvDirectory(settings);
            throw new InvalidOperationException(
                $"Audit-CSV ist als zentrale Quelle aktiv, aber im Ordner '{directory}' wurden keine Sales_ProcessedMergeInput_*.csv- oder Finance_Dashboard_Audit_All_*.csv-Dateien gefunden.");
        }

        return records
            .OrderBy(r => r.Land)
            .ThenBy(r => r.Tsc)
            .ThenByDescending(r => r.InvoiceDate ?? DateTime.MinValue)
            .ThenBy(r => r.InvoiceNumber)
            .ThenBy(r => r.PositionOnInvoice)
            .ToList();
    }

    public async Task<List<SalesRecord>> GetLatestRecordsBySiteAsync()
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var settings = await db.ExportSettings.AsNoTracking().FirstOrDefaultAsync() ?? new ExportSettings();
        var snapshots = (await _auditCsvService.ReadLatestSiteAuditCsvSnapshotsAsync(settings))
            .Where(snapshot => snapshot.Records.Count > 0)
            .ToDictionary(snapshot => snapshot.Tsc, StringComparer.OrdinalIgnoreCase);

        var dbRows = await db.CentralSalesRecords
            .AsNoTracking()
            .ToListAsync();
        var dbByTsc = dbRows
            .Where(row => !string.IsNullOrWhiteSpace(row.Tsc))
            .GroupBy(row => row.Tsc, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        var tscKeys = dbByTsc.Keys
            .Concat(snapshots.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tsc => tsc, StringComparer.OrdinalIgnoreCase);
        var records = new List<SalesRecord>();

        foreach (var tsc in tscKeys)
        {
            dbByTsc.TryGetValue(tsc, out var dbGroup);
            snapshots.TryGetValue(tsc, out var snapshot);
            var latestDbStoredAtUtc = dbGroup?.Max(row => EnsureUtc(row.StoredAtUtc));
            var useCsv = snapshot is not null &&
                (latestDbStoredAtUtc is null ||
                 snapshot.LastWriteTimeUtc > latestDbStoredAtUtc.Value.AddSeconds(1));

            if (useCsv && snapshot is not null)
            {
                records.AddRange(snapshot.Records);
                continue;
            }

            if (dbGroup is not null)
                records.AddRange(dbGroup.Select(MapCentralRecord));
        }

        return records
            .OrderBy(r => r.Land)
            .ThenBy(r => r.Tsc)
            .ThenByDescending(r => r.InvoiceDate ?? DateTime.MinValue)
            .ThenBy(r => r.InvoiceNumber)
            .ThenBy(r => r.PositionOnInvoice)
            .ToList();
    }

    public async Task<bool> UsesAuditCsvAsync()
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var settings = await db.ExportSettings.AsNoTracking().FirstOrDefaultAsync() ?? new ExportSettings();
        return settings.UseAuditCsvAsCentralSource;
    }

    private static DateTime EnsureUtc(DateTime value)
        => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    private static SalesRecord MapCentralRecord(CentralSalesRecord r)
        => new()
        {
            SourceSystem = r.SourceSystem,
            ExtractionDate = r.ExtractionDate,
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
            SupplierNumber = r.SupplierNumber,
            SupplierName = r.SupplierName,
            SupplierCountry = r.SupplierCountry,
            SalesType = r.SalesType,
            GroupMaterialNumber = r.GroupMaterialNumber,
            CustomerNumber = r.CustomerNumber,
            CustomerName = r.CustomerName,
            CustomerCountry = r.CustomerCountry,
            CustomerIndustry = r.CustomerIndustry,
            StandardCost = r.StandardCost,
            StandardCostCurrency = r.StandardCostCurrency,
            StandardCostVariable = r.StandardCostVariable,
            StandardCostFixed = r.StandardCostFixed,
            PurchaseOrderNumber = r.PurchaseOrderNumber,
            SalesPriceValue = r.SalesPriceValue,
            SalesCurrency = r.SalesCurrency,
            DocumentCurrency = r.DocumentCurrency,
            DocumentTotalForeignCurrency = r.DocumentTotalForeignCurrency,
            DocumentTotalLocalCurrency = r.DocumentTotalLocalCurrency,
            VatSumForeignCurrency = r.VatSumForeignCurrency,
            VatSumLocalCurrency = r.VatSumLocalCurrency,
            DocumentRate = r.DocumentRate,
            CompanyCurrency = r.CompanyCurrency,
            Incoterms2020 = r.Incoterms2020,
            SalesResponsibleEmployee = r.SalesResponsibleEmployee,
            PostingDate = r.PostingDate,
            InvoiceDate = r.InvoiceDate,
            OrderDate = r.OrderDate,
            LineRegistrationDate = r.LineRegistrationDate,
            Land = r.Land,
            DocumentType = r.DocumentType
        };
}
