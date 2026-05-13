using Sap.Data.Hana;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public class HanaQueryService : IHanaQueryService
{
    private const string TscParameterName = "tsc";
    private const string DateFilterParameterName = "dateFilter";
    private readonly IAppEventLogService _appEventLogService;
    private readonly IMappedSalesRecordComposer _composer;

    public HanaQueryService(IAppEventLogService appEventLogService, IMappedSalesRecordComposer composer)
    {
        _appEventLogService = appEventLogService;
        _composer = composer;
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

    public async Task<List<string>> GetAvailableTablesAsync(HanaServer server, string schema, CancellationToken cancellationToken = default)
    {
        var connectionString = server.BuildConnectionString();
        using var connection = new HanaConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        const string query = """
            SELECT table_name
            FROM sys.tables
            WHERE schema_name = :schema
            UNION
            SELECT view_name AS table_name
            FROM sys.views
            WHERE schema_name = :schema
            ORDER BY table_name;
            """;

        using var command = new HanaCommand(query, connection);
        command.Parameters.Add(new HanaParameter("schema", HanaDbType.NVarChar) { Value = schema.Trim().ToUpperInvariant() });
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var tables = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var table = reader["table_name"]?.ToString()?.Trim();
            if (!string.IsNullOrWhiteSpace(table))
                tables.Add(table);
        }

        return tables;
    }

    public async Task<List<string>> GetTableFieldNamesAsync(HanaServer server, string schema, string tableName, CancellationToken cancellationToken = default)
    {
        var connectionString = server.BuildConnectionString();
        using var connection = new HanaConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        const string query = """
            SELECT column_name
            FROM sys.table_columns
            WHERE schema_name = :schema AND table_name = :table
            UNION
            SELECT column_name
            FROM sys.view_columns
            WHERE schema_name = :schema AND view_name = :table
            ORDER BY column_name;
            """;

        using var command = new HanaCommand(query, connection);
        command.Parameters.Add(new HanaParameter("schema", HanaDbType.NVarChar) { Value = schema.Trim().ToUpperInvariant() });
        command.Parameters.Add(new HanaParameter("table", HanaDbType.NVarChar) { Value = tableName.Trim().ToUpperInvariant() });
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var fields = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var field = reader["column_name"]?.ToString()?.Trim();
            if (!string.IsNullOrWhiteSpace(field))
                fields.Add(field);
        }

        return fields;
    }

    public async Task<List<SalesRecord>> GetMappedSalesRecordsAsync(
        HanaServer server,
        string schema,
        Site site,
        IReadOnlyList<SapSourceDefinition> sources,
        IReadOnlyList<SapJoinDefinition> joins,
        IReadOnlyList<SapFieldMapping> mappings,
        string dateFilter,
        CancellationToken cancellationToken = default)
    {
        var activeSources = sources
            .Where(s => s.IsActive)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Id)
            .ToList();

        if (activeSources.Count == 0)
            throw new InvalidOperationException($"Standort '{site.Land}' hat keine aktiven HANA-Quellen.");
        if (!mappings.Any(m => m.IsActive))
            throw new InvalidOperationException($"Standort '{site.Land}' hat keine aktiven HANA-Feldmappings.");

        var connectionString = server.BuildConnectionString();
        using var connection = new HanaConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var parsedDateFilter = ParseDateFilter(dateFilter);
        var sourceRows = new Dictionary<string, List<Dictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in activeSources)
        {
            await _appEventLogService.WriteDebugAsync("HANA", "Mapping-Quelle wird gelesen", site.Id, site.Land,
                $"Alias={source.Alias} | Tabelle/View={source.EntitySet}");
            sourceRows[source.Alias] = await ReadMappedSourceRowsAsync(connection, schema, source.EntitySet, parsedDateFilter, cancellationToken);
            await _appEventLogService.WriteDebugAsync("HANA", "Mapping-Quelle gelesen", site.Id, site.Land,
                $"Alias={source.Alias} | Tabelle/View={source.EntitySet} | Zeilen={sourceRows[source.Alias].Count}");
        }

        return _composer.Compose(site, activeSources, joins, mappings, sourceRows, "HANA");
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
                DocumentEntry = Convert.ToInt32(reader["document_entry"]),
                InvoiceNumber = reader["invoice_number"]?.ToString() ?? string.Empty,
                PositionOnInvoice = Convert.ToInt32(reader["invoice_position"]),
                PostingDate = reader.IsDBNull(reader.GetOrdinal("posting_date")) ? null : reader.GetDateTime(reader.GetOrdinal("posting_date")),
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
                DocumentCurrency = reader["document_currency"]?.ToString() ?? string.Empty,
                DocumentTotalForeignCurrency = Convert.ToDecimal(reader["document_total_fc"]),
                DocumentTotalLocalCurrency = Convert.ToDecimal(reader["document_total_lc"]),
                VatSumForeignCurrency = Convert.ToDecimal(reader["vat_sum_fc"]),
                VatSumLocalCurrency = Convert.ToDecimal(reader["vat_sum_lc"]),
                DocumentRate = Convert.ToDecimal(reader["document_rate"]),
                CompanyCurrency = reader["company_currency"]?.ToString() ?? string.Empty,
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

    private static async Task<List<Dictionary<string, object?>>> ReadMappedSourceRowsAsync(
        HanaConnection connection,
        string schema,
        string tableName,
        DateTime dateFilter,
        CancellationToken cancellationToken)
    {
        var schemaPrefix = BuildSchemaPrefix(schema);
        var tableIdentifier = BuildIdentifier(tableName);
        var hasFkdat = await HasColumnAsync(connection, schema, tableName, "FKDAT", cancellationToken);
        var query = hasFkdat
            ? $@"SELECT * FROM {schemaPrefix}{tableIdentifier} WHERE ""FKDAT"" >= :{DateFilterParameterName}"
            : $@"SELECT * FROM {schemaPrefix}{tableIdentifier}";

        using var command = new HanaCommand(query, connection);
        if (hasFkdat)
            command.Parameters.Add(new HanaParameter(DateFilterParameterName, HanaDbType.Date) { Value = dateFilter.Date });

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<Dictionary<string, object?>>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            rows.Add(row);
        }

        return rows;
    }

    private static async Task<bool> HasColumnAsync(HanaConnection connection, string schema, string tableName, string columnName, CancellationToken cancellationToken)
    {
        const string query = """
            SELECT COUNT(*) AS cnt
            FROM (
                SELECT column_name
                FROM sys.table_columns
                WHERE schema_name = :schema AND table_name = :table AND column_name = :column
                UNION ALL
                SELECT column_name
                FROM sys.view_columns
                WHERE schema_name = :schema AND view_name = :table AND column_name = :column
            ) x;
            """;

        using var command = new HanaCommand(query, connection);
        command.Parameters.Add(new HanaParameter("schema", HanaDbType.NVarChar) { Value = schema.Trim().ToUpperInvariant() });
        command.Parameters.Add(new HanaParameter("table", HanaDbType.NVarChar) { Value = tableName.Trim().ToUpperInvariant() });
        command.Parameters.Add(new HanaParameter("column", HanaDbType.NVarChar) { Value = columnName.Trim().ToUpperInvariant() });
        var count = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(count) > 0;
    }

    private static string GetInvoiceQuery(string schema)
    {
        var schemaPrefix = BuildSchemaPrefix(schema);
        return $@"
SELECT
    CURRENT_TIMESTAMP AS extraction_date,
    :{TscParameterName} AS tsc,
    h.""DocEntry"" AS document_entry,
    h.""DocNum"" AS invoice_number,
    p.""LineNum"" AS invoice_position,
    h.""DocDate"" AS posting_date,
    h.""TaxDate"" AS invoice_date,
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
    COALESCE(adm.""MainCurncy"", '') AS standard_cost_currency,
    CASE WHEN p.""BaseType"" = 22
         THEN CAST(p.""BaseRef"" AS NVARCHAR(20))
         ELSE '' END AS purchase_order_number,
    p.""LineTotal"" AS sales_value,
    COALESCE(adm.""MainCurncy"", '') AS sales_currency,
    COALESCE(h.""DocCur"", '') AS document_currency,
    COALESCE(h.""DocTotalFC"", 0) AS document_total_fc,
    COALESCE(h.""DocTotal"", 0) AS document_total_lc,
    COALESCE(h.""VatSumFC"", 0) AS vat_sum_fc,
    COALESCE(h.""VatSum"", 0) AS vat_sum_lc,
    COALESCE(h.""DocRate"", 0) AS document_rate,
    COALESCE(adm.""MainCurncy"", '') AS company_currency,
    '' AS incoterms_2020,
    COALESCE(emp.""SlpName"", '') AS sales_responsible,
    CASE WHEN p.""BaseType"" = 17
         THEN (SELECT o.""DocDate"" FROM {schemaPrefix}""ORDR"" o
               WHERE o.""DocEntry"" = p.""BaseEntry"")
         ELSE NULL END AS order_date,
    'INV' AS doc_type
FROM {schemaPrefix}""OINV"" h
INNER JOIN {schemaPrefix}""INV1"" p ON h.""DocEntry"" = p.""DocEntry""
CROSS JOIN {schemaPrefix}""OADM"" adm
LEFT JOIN {schemaPrefix}""OITM"" itm ON p.""ItemCode"" = itm.""ItemCode""
LEFT JOIN {schemaPrefix}""OITB"" grp ON itm.""ItmsGrpCod"" = grp.""ItmsGrpCod""
LEFT JOIN {schemaPrefix}""OCRD"" cust ON h.""CardCode"" = cust.""CardCode""
LEFT JOIN {schemaPrefix}""CRD1"" cust_adr ON h.""CardCode"" = cust_adr.""CardCode""
    AND cust_adr.""AdresType"" = 'B' AND cust_adr.""Address"" = h.""PayToCode""
LEFT JOIN {schemaPrefix}""OOND"" ind ON cust.""IndustryC"" = ind.""IndCode""
LEFT JOIN {schemaPrefix}""OCRD"" sup ON itm.""CardCode"" = sup.""CardCode""
    AND sup.""CardType"" = 'S'
LEFT JOIN {schemaPrefix}""CRD1"" sup_adr ON itm.""CardCode"" = sup_adr.""CardCode""
    AND sup_adr.""AdresType"" = 'B'
LEFT JOIN {schemaPrefix}""OSLP"" emp ON h.""SlpCode"" = emp.""SlpCode""
WHERE h.""CANCELED"" = 'N' AND h.""DocDate"" >= :{DateFilterParameterName}
ORDER BY h.""DocDate"" DESC, h.""DocNum"", p.""LineNum""";
    }

    private static string GetCreditNoteQuery(string schema)
    {
        var schemaPrefix = BuildSchemaPrefix(schema);
        return $@"
SELECT
    CURRENT_TIMESTAMP AS extraction_date,
    :{TscParameterName} AS tsc,
    h.""DocEntry"" AS document_entry,
    h.""DocNum"" AS invoice_number,
    p.""LineNum"" AS invoice_position,
    h.""DocDate"" AS posting_date,
    h.""TaxDate"" AS invoice_date,
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
    COALESCE(adm.""MainCurncy"", '') AS standard_cost_currency,
    '' AS purchase_order_number,
    p.""LineTotal"" * -1 AS sales_value,
    COALESCE(adm.""MainCurncy"", '') AS sales_currency,
    COALESCE(h.""DocCur"", '') AS document_currency,
    COALESCE(h.""DocTotalFC"", 0) * -1 AS document_total_fc,
    COALESCE(h.""DocTotal"", 0) * -1 AS document_total_lc,
    COALESCE(h.""VatSumFC"", 0) * -1 AS vat_sum_fc,
    COALESCE(h.""VatSum"", 0) * -1 AS vat_sum_lc,
    COALESCE(h.""DocRate"", 0) AS document_rate,
    COALESCE(adm.""MainCurncy"", '') AS company_currency,
    '' AS incoterms_2020,
    COALESCE(emp.""SlpName"", '') AS sales_responsible,
    NULL AS order_date,
    'CRN' AS doc_type
FROM {schemaPrefix}""ORIN"" h
INNER JOIN {schemaPrefix}""RIN1"" p ON h.""DocEntry"" = p.""DocEntry""
CROSS JOIN {schemaPrefix}""OADM"" adm
LEFT JOIN {schemaPrefix}""OITM"" itm ON p.""ItemCode"" = itm.""ItemCode""
LEFT JOIN {schemaPrefix}""OITB"" grp ON itm.""ItmsGrpCod"" = grp.""ItmsGrpCod""
LEFT JOIN {schemaPrefix}""OCRD"" cust ON h.""CardCode"" = cust.""CardCode""
LEFT JOIN {schemaPrefix}""CRD1"" cust_adr ON h.""CardCode"" = cust_adr.""CardCode""
    AND cust_adr.""AdresType"" = 'B' AND cust_adr.""Address"" = h.""PayToCode""
LEFT JOIN {schemaPrefix}""OOND"" ind ON cust.""IndustryC"" = ind.""IndCode""
LEFT JOIN {schemaPrefix}""OCRD"" sup ON itm.""CardCode"" = sup.""CardCode""
    AND sup.""CardType"" = 'S'
LEFT JOIN {schemaPrefix}""CRD1"" sup_adr ON itm.""CardCode"" = sup_adr.""CardCode""
    AND sup_adr.""AdresType"" = 'B'
LEFT JOIN {schemaPrefix}""OSLP"" emp ON h.""SlpCode"" = emp.""SlpCode""
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

    private static string BuildSchemaPrefix(string identifier)
    {
        var value = identifier?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("HANA-Schema darf nicht leer sein.");

        foreach (var ch in value)
        {
            if (!(char.IsLetterOrDigit(ch) || ch == '_'))
                throw new InvalidOperationException($"Ungueltiger HANA-Identifier: '{identifier}'.");
        }

        return $"{value}.";
    }

    private static string BuildIdentifier(string identifier)
    {
        var value = identifier?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("HANA-Identifier darf nicht leer sein.");

        foreach (var ch in value)
        {
            if (!(char.IsLetterOrDigit(ch) || ch == '_'))
                throw new InvalidOperationException($"Ungueltiger HANA-Identifier: '{identifier}'.");
        }

        return $"\"{value}\"";
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
