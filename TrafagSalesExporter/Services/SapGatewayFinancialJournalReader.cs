using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Liest Hauptbuch-Buchungszeilen fuer SAP-ECC-Gesellschaften (ZSCHWEIZ = CH/AT)
/// ueber das SAP-OData-Gateway aus einem konfigurierbaren Journal-EntitySet.
/// Das EntitySet muss auf SAP-Seite bereitgestellt werden (BKPF/BSEG plus
/// SKAT-Kontotext); die erwartete Felddefinition steht in
/// docs/FINANCE_JOURNAL.md. Fehlt das EntitySet oder eine Pflicht-Property im
/// Service, bricht der Reader mit einer klaren fachlichen Meldung ab.
/// </summary>
public interface ISapGatewayFinancialJournalReader
{
    Task<List<FinancialJournalEntry>> GetJournalEntriesAsync(
        string serviceUrl,
        string entitySet,
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
    /// <summary>Fallback, solange am Standort kein abweichender Journal-EntitySet-Name gepflegt ist.</summary>
    public const string DefaultJournalEntitySet = "FinanzJournalSet";

    public static readonly IReadOnlyList<string> RequiredFields =
    [
        "Bukrs", "Belnr", "Gjahr", "Buzei", "Budat", "Monat", "Blart", "Xblnr", "Stblg",
        "Hwaer", "Waers", "Hkont", "HkontTxt", "Shkzg", "Dmbtr", "Wrbtr", "Kostl", "Prctr",
        "Sgtxt", "Faedt"
    ];

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
        string entitySet,
        string username,
        string password,
        string tsc,
        string land,
        string sourceSystem,
        string dateFilter,
        CancellationToken cancellationToken = default)
    {
        var resolvedEntitySet = string.IsNullOrWhiteSpace(entitySet)
            ? DefaultJournalEntitySet
            : entitySet.Trim();
        var parsedDateFilter = ParseDateFilter(dateFilter);
        await EnsureEntitySetIsUsableAsync(
            serviceUrl, resolvedEntitySet, username, password, land, cancellationToken);

        var baseUrl = serviceUrl.TrimEnd('/') + "/";
        var windows = BuildMonthlyWindows(parsedDateFilter, DateTime.Today);
        await _appEventLogService.WriteAsync("SAP", "Journal-Read gestartet", land: land,
            details: $"{baseUrl}{resolvedEntitySet} | ab={parsedDateFilter:yyyy-MM-dd} | Monatsfenster={windows.Count}");

        using var client = CreateClient(username, password);
        var result = new List<FinancialJournalEntry>();
        foreach (var (windowStart, windowEndExclusive) in windows)
        {
            var filter = BuildMonthFilter(windowStart, windowEndExclusive);
            for (var skip = 0; ; skip += PageSize)
            {
                var url = $"{baseUrl}{resolvedEntitySet}?$format=json&$top={PageSize}&$skip={skip}" +
                          $"&$orderby={Uri.EscapeDataString("Bukrs,Gjahr,Belnr,Buzei")}" +
                          $"&$filter={Uri.EscapeDataString(filter)}";
                using var response = await client.GetAsync(url, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new HttpRequestException(
                        $"SAP OData {resolvedEntitySet} fehlgeschlagen ({(int)response.StatusCode} {response.ReasonPhrase}) " +
                        $"URL={url} Antwort={TrimForLog(error)}");
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var page = ParseRows(json);
                foreach (var row in page)
                    result.Add(MapRow(row, tsc, land, sourceSystem));

                if (page.Count > 0 && result.Count % 5000 < PageSize)
                {
                    await _appEventLogService.WriteDebugAsync("SAP", "Journal-Read liest Daten", land: land,
                        details: $"Monat={windowStart:yyyy-MM} | bisher gelesene Zeilen={result.Count}");
                }

                if (page.Count < PageSize)
                    break;
            }
        }

        await _appEventLogService.WriteAsync("SAP", "Journal-Read beendet", land: land,
            details: $"{baseUrl}{resolvedEntitySet} | Zeilen={result.Count} | Monatsfenster={windows.Count}");
        return result;
    }

    /// <summary>
    /// Zerlegt den Ladezeitraum in Kalendermonate.
    /// <para>
    /// Grund ist eine Messung vom 2026-09-11 gegen <c>T76/100</c>: der SAP-seitige Data
    /// Provider liest je Seite den gesamten gefilterten Bestand aus <c>BKPF</c> und
    /// <c>BSEG</c> neu und wirft alles ausser der angeforderten Seite weg. Ueber ein
    /// ganzes Jahr sind das rund 650 Seiten zu je gut sieben Sekunden, also etwa 80
    /// Minuten. Mit einem engeren Zeitfenster faellt die Arbeit je Seite entsprechend,
    /// und derselbe Abruf dauert Sekunden.
    /// </para>
    /// <para>
    /// Das letzte Fenster endet am Tag nach <paramref name="today"/>, damit heutige
    /// Buchungen mitkommen. Fenster sind halboffen: Beginn einschliesslich, Ende
    /// ausschliesslich, damit kein Beleg doppelt gelesen wird.
    /// </para>
    /// </summary>
    public static List<(DateTime Start, DateTime EndExclusive)> BuildMonthlyWindows(DateTime from, DateTime today)
    {
        var windows = new List<(DateTime, DateTime)>();
        var start = from.Date;
        var last = today.Date >= start ? today.Date : start;
        var limit = last.AddDays(1);

        while (start < limit)
        {
            var nextMonth = new DateTime(start.Year, start.Month, 1).AddMonths(1);
            var end = nextMonth < limit ? nextMonth : limit;
            windows.Add((start, end));
            start = end;
        }

        return windows;
    }

    /// <summary>
    /// Baut den OData-Filter fuer ein Monatsfenster.
    /// <para>
    /// <c>lt</c> statt <c>le</c> fuer die Obergrenze, weil die Fenster halboffen sind.
    /// Der SAP-Data-Provider fasst die beiden Bedingungen zu einem <c>BETWEEN</c>
    /// zusammen; ohne diese Zusammenfassung waeren sie in ABAP ODER-verknuepft und der
    /// Filter waere wirkungslos. Siehe <c>docs/abap/README_FIN_JOURNAL_ENTITYSET.md</c>.
    /// </para>
    /// </summary>
    public static string BuildMonthFilter(DateTime start, DateTime endExclusive)
        => $"Budat ge datetime'{start:yyyy-MM-dd}T00:00:00' and " +
           $"Budat lt datetime'{endExclusive:yyyy-MM-dd}T00:00:00'";

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
            DueDate = ParseSapDate(row.TryGetValue("Faedt", out var faedt) ? faedt : null),
            // Ausgleich in SAP ECC: BSEG-AUGDT/AUGBL. Bewusst NICHT in RequiredFields, weil
            // das produktive EntitySet die Felder heute nicht liefert; fehlen sie, bleibt der
            // Ausgleich leer statt den ganzen CH/AT-Import abzubrechen.
            ClearingDate = ParseSapDate(row.TryGetValue("Augdt", out var augdt) ? augdt : null),
            ClearingReference = GetText(row, "Augbl"),
            // ECC kennt je Position genau einen Ausgleichsbeleg; ein Mehrfachausgleich wie in
            // B1 (dort live bis zu 9 je Zeile) entsteht hier nicht.
            ClearingCount = string.IsNullOrWhiteSpace(GetText(row, "Augbl")) ? 0 : 1,
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

    public static List<string> FindMissingRequiredFields(IEnumerable<string> availableFields)
    {
        var available = availableFields.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return RequiredFields.Where(field => !available.Contains(field)).ToList();
    }

    private async Task EnsureEntitySetIsUsableAsync(
        string serviceUrl, string entitySet, string username, string password, string land,
        CancellationToken cancellationToken)
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

        if (!entitySets.Any(name => string.Equals(name, entitySet, StringComparison.OrdinalIgnoreCase)))
        {
            await _appEventLogService.WriteAsync("SAP", "Journal-EntitySet fehlt", "Error", land: land,
                details: $"{serviceUrl} | erwartet={entitySet} | vorhanden={string.Join(", ", entitySets.Take(20))}");
            throw new InvalidOperationException(
                $"Der SAP-Service enthaelt das konfigurierte Journal-EntitySet '{entitySet}' nicht. " +
                "Die Felddefinition steht in docs/FINANCE_JOURNAL.md.");
        }

        var fields = await _sapGatewayService.GetEntityFieldNamesAsync(
            serviceUrl, entitySet, username, password, cancellationToken);
        var missingFields = FindMissingRequiredFields(fields);
        if (missingFields.Count == 0)
            return;

        await _appEventLogService.WriteAsync("SAP", "Journal-EntitySet ungeeignet", "Error", land: land,
            details: $"{serviceUrl} | EntitySet={entitySet} | fehlende Felder={string.Join(", ", missingFields)}");
        throw new InvalidOperationException(
            $"Das SAP-EntitySet '{entitySet}' ist kein vollstaendiges Hauptbuch-Journal. " +
            $"Fehlende Properties: {string.Join(", ", missingFields)}. " +
            "Die Felddefinition steht in docs/FINANCE_JOURNAL.md.");
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
    /// <summary>
    /// SAP liefert ein leeres Datumsfeld je nach Serialisierung als `00000000`,
    /// `0001-01-01` oder `1753-01-01`. Ein solcher Initialwert ist kein Datum; bei einem
    /// Ausgleichsdatum saehe er sonst wie „ausgeglichen am 01.01.0001" aus.
    /// </summary>
    private static DateTime? NullIfInitial(DateTime value) => value.Year < 1900 ? null : value;

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
                return NullIfInitial(DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime.Date);
        }

        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
            return NullIfInitial(parsed.Date);
        return DateTime.TryParseExact(text, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed)
            ? NullIfInitial(parsed.Date)
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
