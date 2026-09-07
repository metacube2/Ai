using Sap.Data.Hana;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

/// <summary>
/// SAP-B1-Hauptbuch-Leser: liest Journalzeilen aus JDT1/OJDT (plus OACT-Kontonamen
/// und OADM-Hauswaehrung) fuer den Finance-Journal-Import. Bewusst OHNE den
/// IT-Umsatzkontenfilter aus der Sales-Strecke — das Journal ist das volle Hauptbuch.
/// </summary>
public class HanaFinancialJournalReader : IFinancialJournalReader
{
    private const string DateFilterParameterName = "dateFilter";
    private readonly IAppEventLogService _appEventLogService;

    public HanaFinancialJournalReader(IAppEventLogService appEventLogService)
    {
        _appEventLogService = appEventLogService;
    }

    public async Task<List<FinancialJournalEntry>> GetJournalEntriesAsync(
        HanaServer server,
        string schema,
        string tsc,
        string land,
        string sourceSystem,
        string dateFilter,
        CancellationToken cancellationToken = default)
    {
        var connectionString = server.BuildConnectionString();
        var query = GetJournalQuery(schema);
        var parsedDateFilter = ParseDateFilter(dateFilter);
        var result = new List<FinancialJournalEntry>();

        await _appEventLogService.WriteAsync("HANA", "Journal-Query gestartet", land: land,
            details: $"Schema={schema} | TSC={tsc} | dateFilter={parsedDateFilter:yyyy-MM-dd}");

        using var connection = new HanaConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureJournalTablesExistAsync(connection, schema, land, cancellationToken);

        using var command = new HanaCommand(query, connection);
        command.Parameters.Add(new HanaParameter(DateFilterParameterName, HanaDbType.Date) { Value = parsedDateFilter.Date });
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var counter = 0;
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(CreateEntry(
                tsc: tsc,
                land: land,
                companySchema: schema,
                sourceSystem: sourceSystem,
                extractionDate: reader.GetDateTime(reader.GetOrdinal("extraction_date")),
                journalEntryId: reader["trans_id"]?.ToString() ?? string.Empty,
                journalEntryLineId: Convert.ToInt32(reader["line_id"]),
                postingDate: reader.IsDBNull(reader.GetOrdinal("posting_date")) ? null : reader.GetDateTime(reader.GetOrdinal("posting_date")),
                accountCode: reader["account_code"]?.ToString() ?? string.Empty,
                accountName: reader["account_name"]?.ToString() ?? string.Empty,
                debit: Convert.ToDecimal(reader["debit_lc"]),
                credit: Convert.ToDecimal(reader["credit_lc"]),
                fcDebit: Convert.ToDecimal(reader["debit_fc"]),
                fcCredit: Convert.ToDecimal(reader["credit_fc"]),
                localCurrency: reader["local_currency"]?.ToString() ?? string.Empty,
                transactionCurrency: reader["transaction_currency"]?.ToString() ?? string.Empty,
                costCenter: reader["cost_center"]?.ToString() ?? string.Empty,
                dimension2: reader["dimension2"]?.ToString() ?? string.Empty,
                lineMemo: reader["line_memo"]?.ToString() ?? string.Empty,
                transactionType: reader["transaction_type"]?.ToString() ?? string.Empty,
                sourceDocumentNumber: reader["source_document"]?.ToString() ?? string.Empty,
                stornoToTrans: reader["storno_to_trans"]?.ToString() ?? string.Empty,
                autoStorno: reader["auto_storno"]?.ToString() ?? string.Empty));

            counter++;
            if (counter % 5000 == 0)
            {
                await _appEventLogService.WriteDebugAsync("HANA", "Journal-Query liest Daten", land: land,
                    details: $"Bisher gelesene Zeilen={counter}");
            }
        }

        await _appEventLogService.WriteAsync("HANA", "Journal-Query beendet", land: land,
            details: $"Schema={schema} | TSC={tsc} | Zeilen={result.Count}");
        return result;
    }

    /// <summary>
    /// Pure Fabrikmethode fuer eine Journalzeile: leitet Vorzeichenbetrag,
    /// Geschaeftsjahr/-periode sowie Manuell-/Storno-Kennzeichen deterministisch ab.
    /// </summary>
    public static FinancialJournalEntry CreateEntry(
        string tsc,
        string land,
        string companySchema,
        string sourceSystem,
        DateTime extractionDate,
        string journalEntryId,
        int journalEntryLineId,
        DateTime? postingDate,
        string accountCode,
        string accountName,
        decimal debit,
        decimal credit,
        decimal fcDebit,
        decimal fcCredit,
        string localCurrency,
        string transactionCurrency,
        string costCenter,
        string dimension2,
        string lineMemo,
        string transactionType,
        string sourceDocumentNumber,
        string stornoToTrans,
        string autoStorno)
    {
        var normalizedTransType = transactionType?.Trim() ?? string.Empty;
        var hasStornoReference = !string.IsNullOrWhiteSpace(stornoToTrans) &&
                                 stornoToTrans.Trim() is not ("0" or "-1");

        return new FinancialJournalEntry
        {
            StoredAtUtc = DateTime.UtcNow,
            ExtractionDate = extractionDate,
            Tsc = tsc?.Trim() ?? string.Empty,
            Land = land?.Trim() ?? string.Empty,
            CompanySchema = companySchema?.Trim() ?? string.Empty,
            SourceSystem = sourceSystem?.Trim() ?? string.Empty,
            JournalEntryId = journalEntryId?.Trim() ?? string.Empty,
            JournalEntryLineId = journalEntryLineId,
            PostingDate = postingDate,
            FiscalYear = postingDate?.Year ?? 0,
            FiscalPeriod = postingDate?.Month ?? 0,
            AccountCode = accountCode?.Trim() ?? string.Empty,
            AccountName = accountName?.Trim() ?? string.Empty,
            DebitAmount = debit,
            CreditAmount = credit,
            SignedAmountLocal = debit - credit,
            LocalCurrency = localCurrency?.Trim() ?? string.Empty,
            TransactionCurrency = transactionCurrency?.Trim() ?? string.Empty,
            SignedAmountTransaction = fcDebit - fcCredit,
            CostCenter = costCenter?.Trim() ?? string.Empty,
            Dimension2 = dimension2?.Trim() ?? string.Empty,
            LineMemo = lineMemo?.Trim() ?? string.Empty,
            TransactionType = normalizedTransType,
            SourceDocumentNumber = sourceDocumentNumber?.Trim() ?? string.Empty,
            IsManual = normalizedTransType == "30",
            IsReversal = hasStornoReference ||
                         string.Equals(autoStorno?.Trim(), "Y", StringComparison.OrdinalIgnoreCase)
        };
    }

    public static string GetJournalQuery(string schema)
    {
        var schemaPrefix = BuildSchemaPrefix(schema);
        return $@"
SELECT
    CURRENT_TIMESTAMP AS extraction_date,
    j.""TransId"" AS trans_id,
    j.""Line_ID"" AS line_id,
    h.""RefDate"" AS posting_date,
    COALESCE(j.""Account"", '') AS account_code,
    COALESCE(a.""AcctName"", '') AS account_name,
    COALESCE(j.""Debit"", 0) AS debit_lc,
    COALESCE(j.""Credit"", 0) AS credit_lc,
    COALESCE(j.""FCDebit"", 0) AS debit_fc,
    COALESCE(j.""FCCredit"", 0) AS credit_fc,
    COALESCE(adm.""MainCurncy"", '') AS local_currency,
    COALESCE(j.""FCCurrency"", '') AS transaction_currency,
    COALESCE(j.""ProfitCode"", '') AS cost_center,
    COALESCE(j.""OcrCode2"", '') AS dimension2,
    COALESCE(j.""LineMemo"", '') AS line_memo,
    COALESCE(h.""TransType"", '') AS transaction_type,
    COALESCE(h.""BaseRef"", '') AS source_document,
    COALESCE(CAST(h.""StornoToTr"" AS NVARCHAR(20)), '') AS storno_to_trans,
    COALESCE(h.""AutoStorno"", 'N') AS auto_storno
FROM {schemaPrefix}""JDT1"" j
INNER JOIN {schemaPrefix}""OJDT"" h ON j.""TransId"" = h.""TransId""
CROSS JOIN {schemaPrefix}""OADM"" adm
LEFT JOIN {schemaPrefix}""OACT"" a ON j.""Account"" = a.""AcctCode""
WHERE h.""RefDate"" >= :{DateFilterParameterName}
ORDER BY h.""RefDate"", j.""TransId"", j.""Line_ID""";
    }

    /// <summary>
    /// Prueft vor dem Lesen, ob das Schema die B1-Journaltabellen hat. Ohne diesen Check
    /// wuerde ein Nicht-B1-Schema mit einem rohen SQL-Fehler abbrechen; so bekommt der
    /// Anwender eine klare fachliche Meldung.
    /// </summary>
    private async Task EnsureJournalTablesExistAsync(
        HanaConnection connection, string schema, string land, CancellationToken cancellationToken)
    {
        const string query = """
            SELECT COUNT(DISTINCT table_name) AS cnt
            FROM sys.tables
            WHERE schema_name = :schema AND table_name IN ('OJDT', 'JDT1')
            """;

        using var command = new HanaCommand(query, connection);
        command.Parameters.Add(new HanaParameter("schema", HanaDbType.NVarChar) { Value = schema.Trim().ToUpperInvariant() });
        var found = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        if (found >= 2)
            return;

        await _appEventLogService.WriteAsync("HANA", "Journaltabellen fehlen", "Error", land: land,
            details: $"Schema={schema} | gefundene B1-Journaltabellen={found} (erwartet OJDT und JDT1)");
        throw new InvalidOperationException(
            $"Im Schema '{schema}' wurden die SAP-B1-Journaltabellen 'OJDT'/'JDT1' nicht gefunden. " +
            "Dieser Standort ist keine B1-Hauptbuchquelle oder das Schema ist falsch konfiguriert.");
    }

    private static DateTime ParseDateFilter(string dateFilter)
    {
        if (DateTime.TryParse(dateFilter, out var parsed))
            return parsed.Date;

        throw new InvalidOperationException($"Ungueltiger Journal-DateFilter: '{dateFilter}'. Erwartet wird ein parsebares Datum.");
    }

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
}
