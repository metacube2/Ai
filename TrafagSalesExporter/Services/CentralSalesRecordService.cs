using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public class CentralSalesRecordService : ICentralSalesRecordService
{
    private const int BatchSize = 25;

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IAppEventLogService _appEventLogService;

    public CentralSalesRecordService(IDbContextFactory<AppDbContext> dbFactory, IAppEventLogService appEventLogService)
    {
        _dbFactory = dbFactory;
        _appEventLogService = appEventLogService;
    }

    public async Task ReplaceForSiteAsync(Site site, IEnumerable<SalesRecord> records, Action<string>? updateStatus = null)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var recordList = records.ToList();

        await db.Database.OpenConnectionAsync();
        var connection = (SqliteConnection)db.Database.GetDbConnection();

        try
        {
            updateStatus?.Invoke("Zentrale Tabelle: bestehende Saetze zaehlen...");
            var existingCount = await CountExistingAsync(connection, site.Id);

            if (existingCount > 0)
            {
                updateStatus?.Invoke("Zentrale Tabelle: alte Saetze loeschen...");
                await DeleteExistingAsync(connection, site.Id);
            }

            updateStatus?.Invoke("Zentrale Tabelle: neue Saetze vorbereiten...");
            await InsertRecordsInCommittedBatchesAsync(connection, site, recordList, updateStatus);

            await _appEventLogService.WriteAsync(
                "Export",
                "Zentrale Tabelle aktualisiert",
                siteId: site.Id,
                land: site.Land,
                details: $"Geloescht={existingCount} | Neu={recordList.Count}");
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    public async Task<List<SalesRecord>> GetAllAsync()
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.CentralSalesRecords
            .OrderBy(r => r.Land)
            .ThenBy(r => r.Tsc)
            .Select(r => new SalesRecord
            {
                ExtractionDate = r.ExtractionDate,
                Tsc = r.Tsc,
                InvoiceNumber = r.InvoiceNumber,
                PositionOnInvoice = r.PositionOnInvoice,
                Material = r.Material,
                Name = r.Name,
                ProductGroup = r.ProductGroup,
                Quantity = r.Quantity,
                SupplierNumber = r.SupplierNumber,
                SupplierName = r.SupplierName,
                SupplierCountry = r.SupplierCountry,
                CustomerNumber = r.CustomerNumber,
                CustomerName = r.CustomerName,
                CustomerCountry = r.CustomerCountry,
                CustomerIndustry = r.CustomerIndustry,
                StandardCost = r.StandardCost,
                StandardCostCurrency = r.StandardCostCurrency,
                PurchaseOrderNumber = r.PurchaseOrderNumber,
                SalesPriceValue = r.SalesPriceValue,
                SalesCurrency = r.SalesCurrency,
                Incoterms2020 = r.Incoterms2020,
                SalesResponsibleEmployee = r.SalesResponsibleEmployee,
                InvoiceDate = r.InvoiceDate,
                OrderDate = r.OrderDate,
                Land = r.Land,
                DocumentType = r.DocumentType
            })
            .ToListAsync();
    }

    private static async Task<int> CountExistingAsync(SqliteConnection connection, int siteId)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM CentralSalesRecords WHERE SiteId = $siteId;";
        command.Parameters.AddWithValue("$siteId", siteId);
        var scalar = await command.ExecuteScalarAsync();
        return scalar is null or DBNull ? 0 : Convert.ToInt32(scalar);
    }

    private static async Task DeleteExistingAsync(SqliteConnection connection, int siteId)
    {
        await using var transaction = connection.BeginTransaction();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "DELETE FROM CentralSalesRecords WHERE SiteId = $siteId;";
        command.Parameters.AddWithValue("$siteId", siteId);
        await command.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
    }

    private static async Task InsertRecordsInCommittedBatchesAsync(
        SqliteConnection connection,
        Site site,
        IReadOnlyList<SalesRecord> records,
        Action<string>? updateStatus)
    {
        var sourceSystem = string.IsNullOrWhiteSpace(site.SourceSystem) ? "SAP" : site.SourceSystem;
        var total = records.Count;
        var totalBatches = Math.Max(1, (int)Math.Ceiling(total / (double)BatchSize));
        var processed = 0;

        for (var batchIndex = 0; batchIndex < totalBatches; batchIndex++)
        {
            updateStatus?.Invoke($"Zentrale Tabelle: Batch {batchIndex + 1}/{totalBatches} speichern...");

            await using var transaction = connection.BeginTransaction();
            await using var command = CreateInsertCommand(connection, transaction);

            var batchRecords = records
                .Skip(batchIndex * BatchSize)
                .Take(BatchSize);

            foreach (var record in batchRecords)
            {
                SetInsertParameters(command, site, sourceSystem, record);
                await command.ExecuteNonQueryAsync();
                processed++;
            }

            updateStatus?.Invoke($"Zentrale Tabelle: Batch {batchIndex + 1}/{totalBatches} abschliessen...");
            await transaction.CommitAsync();
        }

        updateStatus?.Invoke($"Zentrale Tabelle: {processed} Datensaetze gespeichert.");
    }

    private static SqliteCommand CreateInsertCommand(SqliteConnection connection, SqliteTransaction transaction)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO CentralSalesRecords (
                StoredAtUtc, SiteId, SourceSystem, ExtractionDate, Tsc, InvoiceNumber, PositionOnInvoice,
                Material, Name, ProductGroup, Quantity, SupplierNumber, SupplierName, SupplierCountry,
                CustomerNumber, CustomerName, CustomerCountry, CustomerIndustry, StandardCost,
                StandardCostCurrency, PurchaseOrderNumber, SalesPriceValue, SalesCurrency, Incoterms2020,
                SalesResponsibleEmployee, InvoiceDate, OrderDate, Land, DocumentType
            )
            VALUES (
                $storedAtUtc, $siteId, $sourceSystem, $extractionDate, $tsc, $invoiceNumber, $positionOnInvoice,
                $material, $name, $productGroup, $quantity, $supplierNumber, $supplierName, $supplierCountry,
                $customerNumber, $customerName, $customerCountry, $customerIndustry, $standardCost,
                $standardCostCurrency, $purchaseOrderNumber, $salesPriceValue, $salesCurrency, $incoterms2020,
                $salesResponsibleEmployee, $invoiceDate, $orderDate, $land, $documentType
            );
            """;

        command.Parameters.Add("$storedAtUtc", SqliteType.Text);
        command.Parameters.Add("$siteId", SqliteType.Integer);
        command.Parameters.Add("$sourceSystem", SqliteType.Text);
        command.Parameters.Add("$extractionDate", SqliteType.Text);
        command.Parameters.Add("$tsc", SqliteType.Text);
        command.Parameters.Add("$invoiceNumber", SqliteType.Text);
        command.Parameters.Add("$positionOnInvoice", SqliteType.Integer);
        command.Parameters.Add("$material", SqliteType.Text);
        command.Parameters.Add("$name", SqliteType.Text);
        command.Parameters.Add("$productGroup", SqliteType.Text);
        command.Parameters.Add("$quantity", SqliteType.Real);
        command.Parameters.Add("$supplierNumber", SqliteType.Text);
        command.Parameters.Add("$supplierName", SqliteType.Text);
        command.Parameters.Add("$supplierCountry", SqliteType.Text);
        command.Parameters.Add("$customerNumber", SqliteType.Text);
        command.Parameters.Add("$customerName", SqliteType.Text);
        command.Parameters.Add("$customerCountry", SqliteType.Text);
        command.Parameters.Add("$customerIndustry", SqliteType.Text);
        command.Parameters.Add("$standardCost", SqliteType.Real);
        command.Parameters.Add("$standardCostCurrency", SqliteType.Text);
        command.Parameters.Add("$purchaseOrderNumber", SqliteType.Text);
        command.Parameters.Add("$salesPriceValue", SqliteType.Real);
        command.Parameters.Add("$salesCurrency", SqliteType.Text);
        command.Parameters.Add("$incoterms2020", SqliteType.Text);
        command.Parameters.Add("$salesResponsibleEmployee", SqliteType.Text);
        command.Parameters.Add("$invoiceDate", SqliteType.Text);
        command.Parameters.Add("$orderDate", SqliteType.Text);
        command.Parameters.Add("$land", SqliteType.Text);
        command.Parameters.Add("$documentType", SqliteType.Text);

        return command;
    }

    private static void SetInsertParameters(SqliteCommand command, Site site, string sourceSystem, SalesRecord record)
    {
        command.Parameters["$storedAtUtc"].Value = DateTime.UtcNow.ToString("O");
        command.Parameters["$siteId"].Value = site.Id;
        command.Parameters["$sourceSystem"].Value = sourceSystem;
        command.Parameters["$extractionDate"].Value = record.ExtractionDate.ToString("O");
        command.Parameters["$tsc"].Value = record.Tsc ?? string.Empty;
        command.Parameters["$invoiceNumber"].Value = record.InvoiceNumber ?? string.Empty;
        command.Parameters["$positionOnInvoice"].Value = record.PositionOnInvoice;
        command.Parameters["$material"].Value = record.Material ?? string.Empty;
        command.Parameters["$name"].Value = record.Name ?? string.Empty;
        command.Parameters["$productGroup"].Value = record.ProductGroup ?? string.Empty;
        command.Parameters["$quantity"].Value = record.Quantity;
        command.Parameters["$supplierNumber"].Value = record.SupplierNumber ?? string.Empty;
        command.Parameters["$supplierName"].Value = record.SupplierName ?? string.Empty;
        command.Parameters["$supplierCountry"].Value = record.SupplierCountry ?? string.Empty;
        command.Parameters["$customerNumber"].Value = record.CustomerNumber ?? string.Empty;
        command.Parameters["$customerName"].Value = record.CustomerName ?? string.Empty;
        command.Parameters["$customerCountry"].Value = record.CustomerCountry ?? string.Empty;
        command.Parameters["$customerIndustry"].Value = record.CustomerIndustry ?? string.Empty;
        command.Parameters["$standardCost"].Value = record.StandardCost;
        command.Parameters["$standardCostCurrency"].Value = record.StandardCostCurrency ?? string.Empty;
        command.Parameters["$purchaseOrderNumber"].Value = record.PurchaseOrderNumber ?? string.Empty;
        command.Parameters["$salesPriceValue"].Value = record.SalesPriceValue;
        command.Parameters["$salesCurrency"].Value = record.SalesCurrency ?? string.Empty;
        command.Parameters["$incoterms2020"].Value = record.Incoterms2020 ?? string.Empty;
        command.Parameters["$salesResponsibleEmployee"].Value = record.SalesResponsibleEmployee ?? string.Empty;
        command.Parameters["$invoiceDate"].Value = record.InvoiceDate?.ToString("O") ?? (object)DBNull.Value;
        command.Parameters["$orderDate"].Value = record.OrderDate?.ToString("O") ?? (object)DBNull.Value;
        command.Parameters["$land"].Value = record.Land ?? string.Empty;
        command.Parameters["$documentType"].Value = record.DocumentType ?? string.Empty;
    }
}
