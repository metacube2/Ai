using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Liest Hauptbuch-Buchungszeilen fuer SAP-ECC-Gesellschaften (ZSCHWEIZ = CH/AT)
/// ueber das SAP-OData-Gateway aus dem EntitySet <see cref="JournalEntitySet"/>.
/// Das EntitySet muss auf SAP-Seite bereitgestellt werden (BKPF/BSEG plus
/// SKAT-Kontotext); die erwartete Felddefinition steht in
/// docs/FINANCE_JOURNAL_SAP_ODATA_SPEZ_2026-07-14.md. Fehlt das EntitySet im
/// Service, bricht der Reader mit einer klaren fachlichen Meldung ab.
/// </summary>
public interface ISapGatewayFinancialJournalReader
{
    Task<List<FinancialJournalEntry>> GetJournalEntriesAsync(
        string serviceUrl,
        string username,
        string password,
        string tsc,
        string land,
        string sourceSystem,
        string dateFilter,
        CancellationToken cancellationToken = default);
}

public class SapGatewayFinancialJournalReader : ISapGatewayFinancialJournalReader
{
    /// <summary>Erwartetes EntitySet im ZSCHWEIZ-Service; Name ist mit der ABAP-Spez abgestimmt.</summary>
    public const string JournalEntitySet = "FinanzJournalSet";

    private const int PageSize = 1000;
    private readonly ISapGatewayService _sapGatewayService;
    private readonly IAppEventLogService _appEventLogService;

    public SapGatewayFinancialJournalReader(
        ISapGatewayService sapGatewayService,
        IAppEventLogService appEventLogService)
    {
        _sapGatewayService = sapGatewayService;
        _appEventLogService = appEventLogService;
    }

    public async Task<List<FinancialJournalEntry>> GetJournalEntriesAsync(
        string serviceUrl,
        string username,
        string password,
        string tsc,
        string land,
        string sourceSystem,
        string dateFilter,
        CancellationToken cancellationToken = default)
    {
        var parsedDateFilter = ParseDateFilter(dateFilter);
        await EnsureEntitySetExistsAsync(serviceUrl, username, password, land, cancellationToken);

        var baseUrl = serviceUrl.TrimEnd('/') + "/";
        var filter = $"Budat ge datetime'{parsedDateFilter:yyyy-MM-dd}T00:00:00'";
        await _appEventLogService.WriteAsync("SAP", "Journal-Read gestartet", land: land,
            details: $"{baseUrl}{JournalEntitySet} | Filter={filter}");

        using var client = CreateClient(username, password);
        var result = new List<FinancialJournalEntry>();
        for (var skip = 0; ; skip += PageSize)
        {
            var url = $"{baseUrl}{JournalEntitySet}?$format=json&$top={PageSize}&$skip={skip}" +
                      $"&$orderby={Uri.EscapeDataString("Bukrs,Gjahr,Belnr,Buzei")}" +
                      $"&$filter={Uri.EscapeDataString(filter)}";
            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"SAP OData {JournalEntitySet} fehlgeschlagen ({(int)response.StatusCode} {response.ReasonPhrase}) " +
                    $"URL={url} Antwort={TrimForLog(error)}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var page = ParseRows(json);
            foreach (var row in page)
                result.Add(MapRow(row, tsc, land, sourceSystem));

            if (page.Count > 0 && result.Count % 5000 < PageSize)
            {
                await _appEventLogService.WriteDebugAsync("SAP", "Journal-Read liest Daten", land: land,
                    details: $"Bisher gelesene Zeilen={result.Count}");
            }

            if (page.Count < PageSize)
                break;
        }

        await _appEventLogService.WriteAsync("SAP", "Journal-Read beendet", land: land,
            details: $"{baseUrl}{JournalEntitySet} | Zeilen={result.Count}");
        return result;
    }

    /// <summary>
    /// Pure Zeilen-Mapping-Methode (BKPF/BSEG-Felder aus dem OData-JSON) — analog zur
    /// B1-Fabrikmethode. Soll/Haben kommt aus `Shkzg` (S = Soll, H = Haben) mit den
    /// Betraegen `Dmbtr` (Hauswaehrung) und `Wrbtr` (Belegwaehrung); der Vorzeichenbetrag
    /// ist Soll positiv, Haben negativ.
    /// </summary>
    public static FinancialJournalEntry MapRow(
        IReadOnlyDictionary<string, object?> row, string tsc, string land, string sourceSystem)
    {
        var bukrs = GetText(row, "Bukrs");
        var belnr = GetText(row, "Belnr");
        var gjahr = ParseInt(GetText(row, "Gjahr"));
        var isDebit = string.Equals(GetText(row, "Shkzg"), "S", StringComparison.OrdinalIgnoreCase);
        var amountLocal = ParseDecimal(GetText(row, "Dmbtr"));
        var amountDocument = ParseDecimal(GetText(row, "Wrbtr"));
        var blart = GetText(row, "Blart");
        var localCurrency = GetText(row, "Hwaer");
        var documentCurrency = GetText(row, "Waers");

        return new FinancialJournalEntry
        {
            StoredAtUtc = DateTime.UtcNow,
            ExtractionDate = DateTime.UtcNow,
            Tsc = tsc?.Trim() ?? string.Empty,
            Land = land?.Trim() ?? string.Empty,
            CompanySchema = string.Empty,
            CompanyCode = bukrs,
            SourceSystem = sourceSystem?.Trim() ?? string.Empty,
            // BELNR ist erst mit Buchungskreis + Geschaeftsjahr eindeutig.
            JournalEntryId = $"{bukrs}/{gjahr}/{belnr}",
            JournalEntryLineId = ParseInt(GetText(row, "Buzei")),
            PostingDate = ParseSapDate(row.TryGetValue("Budat", out var budat) ? budat : null),
            FiscalYear = gjahr,
            FiscalPeriod = ParseInt(GetText(row, "Monat")),
            AccountCode = GetText(row, "Hkont").TrimStart('0'),
            AccountName = GetText(row, "HkontTxt"),
            DebitAmount = isDebit ? amountLocal : 0m,
            CreditAmount = isDebit ? 0m : amountLocal,
            SignedAmountLocal = isDebit ? amountLocal : -amountLocal,
            LocalCurrency = localCurrency,
            // Nur echte Fremdwaehrungsbelege als Transaktionswaehrung ausweisen.
            TransactionCurrency = string.Equals(documentCurrency, localCurrency, StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : documentCurrency,
            SignedAmountTransaction = isDebit ? amountDocument : -amountDocument,
            CostCenter = GetText(row, "Kostl").TrimStart('0'),
            Dimension2 = GetText(row, "Prctr").TrimStart('0'),
            LineMemo = GetText(row, "Sgtxt"),
            TransactionType = blart,
            SourceDocumentNumber = GetText(row, "Xblnr"),
            // Annahme (mit Finance zu bestaetigen): Belegart SA = manuelle Sachkontenbuchung.
            IsManual = string.Equals(blart, "SA", StringComparison.OrdinalIgnoreCase),
            // Storniert, wenn eine Storno-Belegnummer (BKPF-STBLG) gesetzt ist.
            IsReversal = !string.IsNullOrWhiteSpace(GetText(row, "Stblg"))
        };
    }

    private async Task EnsureEntitySetExistsAsync(
        string serviceUrl, string username, string password, string land, CancellationToken cancellationToken)
    {
        List<string> entitySets;
        try
        {
            entitySets = await _sapGatewayService.GetEntitySetsAsync(serviceUrl, username, password, cancellationToken);
        }
        catch (Exception ex)
        {
            await _appEventLogService.WriteAsync("SAP", "Journal-Metadata-Probe fehlgeschlagen", "Error",
                land: land, details: $"{serviceUrl} | {ex.Message}");
            throw;
        }

        if (entitySets.Any(name => string.Equals(name, JournalEntitySet, StringComparison.OrdinalIgnoreCase)))
            return;

        await _appEventLogService.WriteAsync("SAP", "Journal-EntitySet fehlt", "Error", land: land,
            details: $"{serviceUrl} | erwartet={JournalEntitySet} | vorhanden={string.Join(", ", entitySets.Take(20))}");
        throw new InvalidOperationException(
            $"Der SAP-Service enthaelt das EntitySet '{JournalEntitySet}' noch nicht. " +
            "Das Hauptbuch-EntitySet muss zuerst auf SAP-Seite bereitgestellt werden " +
            "(Felddefinition: docs/FINANCE_JOURNAL_SAP_ODATA_SPEZ_2026-07-14.md).");
    }

    private static HttpClient CreateClient(string username, string password)
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private static List<Dictionary<string, object?>> ParseRows(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("d", out var d) ||
            !d.TryGetProperty("results", out var results) ||
            results.ValueKind != JsonValueKind.Array)
            return [];

        return results.EnumerateArray()
            .Select(item => item.EnumerateObject()
                .Where(property => property.Name != "__metadata")
                .ToDictionary(
                    property => property.Name,
                    property => (object?)(property.Value.ValueKind switch
                    {
                        JsonValueKind.String => property.Value.GetString(),
                        JsonValueKind.Null => null,
                        _ => property.Value.ToString()
                    }),
                    StringComparer.OrdinalIgnoreCase))
            .ToList();
    }

    private static string GetText(IReadOnlyDictionary<string, object?> row, string key)
        => row.TryGetValue(key, out var value) ? value?.ToString()?.Trim() ?? string.Empty : string.Empty;

    private static int ParseInt(string value)
        => int.TryParse(value?.TrimStart('0'), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;

    private static decimal ParseDecimal(string value)
        => decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0m;

    /// <summary>Parst SAP-OData-Daten: /Date(epochms)/, ISO-Strings und yyyyMMdd.</summary>
    public static DateTime? ParseSapDate(object? value)
    {
        if (value is null)
            return null;
        var text = value.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return null;

        if (text.StartsWith("/Date(", StringComparison.Ordinal) && text.EndsWith(")/", StringComparison.Ordinal))
        {
            var epochRaw = text[6..^2];
            var separator = epochRaw.IndexOfAny(['+', '-'], 1);
            if (separator > 0)
                epochRaw = epochRaw[..separator];
            if (long.TryParse(epochRaw, out var ms))
                return DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime.Date;
        }

        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
            return parsed.Date;
        return DateTime.TryParseExact(text, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed)
            ? parsed.Date
            : null;
    }

    private static DateTime ParseDateFilter(string dateFilter)
    {
        if (DateTime.TryParse(dateFilter, out var parsed))
            return parsed.Date;

        throw new InvalidOperationException($"Ungueltiger Journal-DateFilter: '{dateFilter}'. Erwartet wird ein parsebares Datum.");
    }

    private static string TrimForLog(string value)
        => value.Length <= 500 ? value : value[..500];
}
