using Sap.Data.Hana;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public class HanaQueryService : IHanaQueryService
{
    private const string TscParameterName = "tsc";
    private const string DateFilterParameterName = "dateFilter";
    private readonly IAppEventLogService _appEventLogService;

    public HanaQueryService(IAppEventLogService appEventLogService)
    {
        _appEventLogService = appEventLogService;
    }

    public async Task<List<SalesRecord>> GetSalesRecordsAsync(HanaServer server,
        string schema, string tsc, string land, string dateFilter, CancellationToken cancellationToken = default)
    {
        var connectionString = server.BuildConnectionString();
        var result = new List<SalesRecord>();

        try
        {
            await _appEventLogService.WriteAsync("HANA", "Verbindungsaufbau gestartet", land: land,
                details: $"Server={server.GetConnectionStringPreview()} | Schema={schema} | TSC={tsc}");

            using var connection = new HanaConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await _appEventLogService.WriteAsync("HANA", "Verbindung erfolgreich", land: land,
                details: $"Schema={schema} | TSC={tsc}");

            var invoiceQuery = GetInvoiceQuery(schema);
            var creditNoteQuery = GetCreditNoteQuery(schema);
            var parsedDateFilter = ParseDateFilter(dateFilter);

            await _appEventLogService.WriteAsync("HANA", "Invoice-Query gestartet", land: land,
                details: BuildQueryLogDetails(invoiceQuery, schema, tsc, parsedDateFilter));
            var invoiceRecords = await ReadRecordsAsync(connection, invoiceQuery, tsc, parsedDateFilter, land, "Invoice", cancellationToken);
            result.AddRange(invoiceRecords);
            await _appEventLogService.WriteAsync("HANA", "Invoice-Query beendet", land: land, details: $"Zeilen={invoiceRecords.Count}");

            await _appEventLogService.WriteAsync("HANA", "Credit-Query gestartet", land: land,
                details: BuildQueryLogDetails(creditNoteQuery, schema, tsc, parsedDateFilter));
            var creditRecords = await ReadRecordsAsync(connection, creditNoteQuery, tsc, parsedDateFilter, land, "Credit", cancellationToken);
            result.AddRange(creditRecords);
            await _appEventLogService.WriteAsync("HANA", "Credit-Query beendet", land: land, details: $"Zeilen={creditRecords.Count}");
        }
        catch (Exception ex)
        {
            await _appEventLogService.WriteAsync("HANA", "HANA-Abfrage fehlgeschlagen", "Error", land: land, details: ex.ToString());
            throw;
        }

        foreach (var record in result)
        {
            if (record.Material.Contains('/'))
            {
                var parts = record.Material.Split('/');
                record.Material = parts[^1];
            }
        }

        return result;
    }

    public async Task<ConnectionTestResult> TestConnectionDetailedAsync(HanaServer server, CancellationToken cancellationToken = default)
    {
        var testResult = new ConnectionTestResult
        {
            TestedAtUtc = DateTime.UtcNow,
            ConnectionStringPreview = server.GetConnectionStringPreview(),
            Stage = "Verbindungsaufbau"
        };

        try
        {
            await _appEventLogService.WriteAsync("HANA", "Verbindungstest gestartet",
                details: testResult.ConnectionStringPreview);
            var connectionString = server.BuildConnectionString();
            using var connection = new HanaConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            testResult.Stage = "Ping-Query";
            using var command = new HanaCommand("SELECT 1 FROM DUMMY", connection);
            await command.ExecuteScalarAsync(cancellationToken);

            testResult.Success = true;
            testResult.Stage = "OK";
            await _appEventLogService.WriteAsync("HANA", "Verbindungstest erfolgreich",
                details: testResult.ConnectionStringPreview);
            return testResult;
        }
        catch (Exception ex)
        {
            testResult.Success = false;
            testResult.ErrorMessage = ex.Message;
            testResult.ExceptionType = ex.GetType().Name;
            await _appEventLogService.WriteAsync("HANA", "Verbindungstest fehlgeschlagen", "Error",
                details: $"{testResult.ConnectionStringPreview}{Environment.NewLine}{ex}");
            return testResult;
        }
    }

    public async Task TestConnectionAsync(HanaServer server, CancellationToken cancellationToken = default)
    {
        var connectionString = server.BuildConnectionString();
        using var connection = new HanaConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
    }

    public async Task<List<string>> GetAvailableSchemasAsync(HanaServer server, CancellationToken cancellationToken = default)
    {
        var connectionString = server.BuildConnectionString();
        using var connection = new HanaConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        const string query = """
            SELECT schema_name
            FROM (
                SELECT schema_name, COUNT(DISTINCT table_name) AS required_table_count
                FROM sys.tables
                WHERE table_name IN ('OINV', 'INV1', 'ORIN', 'RIN1', 'OCRD', 'OITM')
                GROUP BY schema_name
            ) t
            WHERE required_table_count >= 4
            ORDER BY schema_name;
            """;

        using var command = new HanaCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var schemas = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var schema = reader["schema_name"]?.ToString()?.Trim();
            if (!string.IsNullOrWhiteSpace(schema))
                schemas.Add(schema);
        }

        return schemas;
    }

    private async Task<List<SalesRecord>> ReadRecordsAsync(HanaConnection connection, string query, string tsc, DateTime dateFilter, string land, string queryName, CancellationToken cancellationToken)
    {
        var records = new List<SalesRecord>();

        using var command = new HanaCommand(query, connection);
        command.Parameters.Add(new HanaParameter(TscParameterName, HanaDbType.NVarChar) { Value = tsc });
        command.Parameters.Add(new HanaParameter(DateFilterParameterName, HanaDbType.Date) { Value = dateFilter.Date });
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var counter = 0;

        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(new SalesRecord
            {
                ExtractionDate = reader.GetDateTime(reader.GetOrdinal("extraction_date")),
                Tsc = reader.GetString(reader.GetOrdinal("tsc")),
                InvoiceNumber = reader["invoice_number"]?.ToString() ?? string.Empty,
                PositionOnInvoice = Convert.ToInt32(reader["invoice_position"]),
                InvoiceDate = reader.IsDBNull(reader.GetOrdinal("invoice_date")) ? null : reader.GetDateTime(reader.GetOrdinal("invoice_date")),
                Material = reader["material"]?.ToString() ?? string.Empty,
                Name = reader["material_name"]?.ToString() ?? string.Empty,
                ProductGroup = reader["product_group"]?.ToString() ?? string.Empty,
                Quantity = Convert.ToDecimal(reader["quantity"]),
                SupplierNumber = reader["supplier_number"]?.ToString() ?? string.Empty,
                SupplierName = reader["supplier_name"]?.ToString() ?? string.Empty,
                SupplierCountry = reader["supplier_country"]?.ToString() ?? string.Empty,
                CustomerNumber = reader["customer_number"]?.ToString() ?? string.Empty,
                CustomerName = reader["customer_name"]?.ToString() ?? string.Empty,
                CustomerCountry = reader["customer_country"]?.ToString() ?? string.Empty,
                CustomerIndustry = reader["customer_industry"]?.ToString() ?? string.Empty,
                StandardCost = Convert.ToDecimal(reader["standard_cost"]),
                StandardCostCurrency = reader["standard_cost_currency"]?.ToString() ?? string.Empty,
                PurchaseOrderNumber = reader["purchase_order_number"]?.ToString() ?? string.Empty,
                SalesPriceValue = Convert.ToDecimal(reader["sales_value"]),
                SalesCurrency = reader["sales_currency"]?.ToString() ?? string.Empty,
                Incoterms2020 = reader["incoterms_2020"]?.ToString() ?? string.Empty,
                SalesResponsibleEmployee = reader["sales_responsible"]?.ToString() ?? string.Empty,
                OrderDate = reader.IsDBNull(reader.GetOrdinal("order_date")) ? null : reader.GetDateTime(reader.GetOrdinal("order_date")),
                Land = land,
                DocumentType = reader["doc_type"]?.ToString() ?? string.Empty
            });

            counter++;
            if (counter % 250 == 0)
            {
                await _appEventLogService.WriteDebugAsync("HANA", $"{queryName}-Query liest Daten", land: land,
                    details: $"Bisher gelesene Zeilen={counter}");
            }
        }

        return records;
    }

    private static string GetInvoiceQuery(string schema)
    {
        var quotedSchema = QuoteIdentifier(schema);
        return $@"
SELECT
    CURRENT_TIMESTAMP AS extraction_date,
    :{TscParameterName} AS tsc,
    h.""DocNum"" AS invoice_number,
    p.""LineNum"" AS invoice_position,
    h.""DocDate"" AS invoice_date,
    p.""ItemCode"" AS material,
    p.""Dscription"" AS material_name,
    COALESCE(grp.""ItmsGrpNam"", '') AS product_group,
    p.""Quantity"" AS quantity,
    COALESCE(itm.""CardCode"", '') AS supplier_number,
    COALESCE(sup.""CardName"", '') AS supplier_name,
    COALESCE(sup_adr.""Country"", '') AS supplier_country,
    h.""CardCode"" AS customer_number,
    h.""CardName"" AS customer_name,
    COALESCE(cust_adr.""Country"", '') AS customer_country,
    COALESCE(ind.""IndName"", '') AS customer_industry,
    p.""StockPrice"" AS standard_cost,
    COALESCE(p.""Currency"", h.""DocCur"") AS standard_cost_currency,
    CASE WHEN p.""BaseType"" = 22
         THEN CAST(p.""BaseRef"" AS NVARCHAR(20))
         ELSE '' END AS purchase_order_number,
    p.""LineTotal"" AS sales_value,
    COALESCE(p.""Currency"", h.""DocCur"") AS sales_currency,
    '' AS incoterms_2020,
    COALESCE(emp.""SlpName"", '') AS sales_responsible,
    CASE WHEN p.""BaseType"" = 17
         THEN (SELECT o.""DocDate"" FROM {quotedSchema}.""ORDR"" o
               WHERE o.""DocEntry"" = p.""BaseEntry"")
         ELSE NULL END AS order_date,
    'INV' AS doc_type
FROM {quotedSchema}.""OINV"" h
INNER JOIN {quotedSchema}.""INV1"" p ON h.""DocEntry"" = p.""DocEntry""
LEFT JOIN {quotedSchema}.""OITM"" itm ON p.""ItemCode"" = itm.""ItemCode""
LEFT JOIN {quotedSchema}.""OITB"" grp ON itm.""ItmsGrpCod"" = grp.""ItmsGrpCod""
LEFT JOIN {quotedSchema}.""OCRD"" cust ON h.""CardCode"" = cust.""CardCode""
LEFT JOIN {quotedSchema}.""CRD1"" cust_adr ON h.""CardCode"" = cust_adr.""CardCode""
    AND cust_adr.""AdresType"" = 'B' AND cust_adr.""Address"" = h.""PayToCode""
LEFT JOIN {quotedSchema}.""OOND"" ind ON cust.""IndustryC"" = ind.""IndCode""
LEFT JOIN {quotedSchema}.""OCRD"" sup ON itm.""CardCode"" = sup.""CardCode""
    AND sup.""CardType"" = 'S'
LEFT JOIN {quotedSchema}.""CRD1"" sup_adr ON itm.""CardCode"" = sup_adr.""CardCode""
    AND sup_adr.""AdresType"" = 'B'
LEFT JOIN {quotedSchema}.""OSLP"" emp ON h.""SlpCode"" = emp.""SlpCode""
WHERE h.""CANCELED"" = 'N' AND h.""DocDate"" >= :{DateFilterParameterName}
ORDER BY h.""DocDate"" DESC, h.""DocNum"", p.""LineNum""";
    }

    private static string GetCreditNoteQuery(string schema)
    {
        var quotedSchema = QuoteIdentifier(schema);
        return $@"
SELECT
    CURRENT_TIMESTAMP AS extraction_date,
    :{TscParameterName} AS tsc,
    h.""DocNum"" AS invoice_number,
    p.""LineNum"" AS invoice_position,
    h.""DocDate"" AS invoice_date,
    p.""ItemCode"" AS material,
    p.""Dscription"" AS material_name,
    COALESCE(grp.""ItmsGrpNam"", '') AS product_group,
    p.""Quantity"" * -1 AS quantity,
    COALESCE(itm.""CardCode"", '') AS supplier_number,
    COALESCE(sup.""CardName"", '') AS supplier_name,
    COALESCE(sup_adr.""Country"", '') AS supplier_country,
    h.""CardCode"" AS customer_number,
    h.""CardName"" AS customer_name,
    COALESCE(cust_adr.""Country"", '') AS customer_country,
    COALESCE(ind.""IndName"", '') AS customer_industry,
    p.""StockPrice"" AS standard_cost,
    COALESCE(p.""Currency"", h.""DocCur"") AS standard_cost_currency,
    '' AS purchase_order_number,
    p.""LineTotal"" * -1 AS sales_value,
    COALESCE(p.""Currency"", h.""DocCur"") AS sales_currency,
    '' AS incoterms_2020,
    COALESCE(emp.""SlpName"", '') AS sales_responsible,
    NULL AS order_date,
    'CRN' AS doc_type
FROM {quotedSchema}.""ORIN"" h
INNER JOIN {quotedSchema}.""RIN1"" p ON h.""DocEntry"" = p.""DocEntry""
LEFT JOIN {quotedSchema}.""OITM"" itm ON p.""ItemCode"" = itm.""ItemCode""
LEFT JOIN {quotedSchema}.""OITB"" grp ON itm.""ItmsGrpCod"" = grp.""ItmsGrpCod""
LEFT JOIN {quotedSchema}.""OCRD"" cust ON h.""CardCode"" = cust.""CardCode""
LEFT JOIN {quotedSchema}.""CRD1"" cust_adr ON h.""CardCode"" = cust_adr.""CardCode""
    AND cust_adr.""AdresType"" = 'B' AND cust_adr.""Address"" = h.""PayToCode""
LEFT JOIN {quotedSchema}.""OOND"" ind ON cust.""IndustryC"" = ind.""IndCode""
LEFT JOIN {quotedSchema}.""OCRD"" sup ON itm.""CardCode"" = sup.""CardCode""
    AND sup.""CardType"" = 'S'
LEFT JOIN {quotedSchema}.""CRD1"" sup_adr ON itm.""CardCode"" = sup_adr.""CardCode""
    AND sup_adr.""AdresType"" = 'B'
LEFT JOIN {quotedSchema}.""OSLP"" emp ON h.""SlpCode"" = emp.""SlpCode""
WHERE h.""CANCELED"" = 'N' AND h.""DocDate"" >= :{DateFilterParameterName}
ORDER BY h.""DocDate"" DESC, h.""DocNum"", p.""LineNum""";
    }

    private static DateTime ParseDateFilter(string dateFilter)
    {
        if (DateTime.TryParse(dateFilter, out var parsed))
            return parsed.Date;

        throw new InvalidOperationException($"Ungueltiger HANA-DateFilter: '{dateFilter}'. Erwartet wird ein parsebares Datum.");
    }

    private static string BuildQueryLogDetails(string query, string schema, string tsc, DateTime dateFilter)
        => $"{query}{Environment.NewLine}-- schema={schema}; tsc={tsc}; dateFilter={dateFilter:yyyy-MM-dd}";

    private static string QuoteIdentifier(string identifier)
    {
        var value = identifier?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("HANA-Schema darf nicht leer sein.");

        foreach (var ch in value)
        {
            if (!(char.IsLetterOrDigit(ch) || ch == '_'))
                throw new InvalidOperationException($"Ungueltiger HANA-Identifier: '{identifier}'.");
        }

        return $@"""{value}""";
    }
}

public class ConnectionTestResult
{
    public bool Success { get; set; }
    public DateTime TestedAtUtc { get; set; }
    public string Stage { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string ExceptionType { get; set; } = string.Empty;
    public string ConnectionStringPreview { get; set; } = string.Empty;
}
