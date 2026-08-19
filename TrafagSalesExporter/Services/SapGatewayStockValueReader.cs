using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TrafagSalesExporter.Services;

/// <summary>Lagerwert einer Disponentengruppe im Bewertungskreis, Stand des letzten Reads.</summary>
public sealed record StockValueByPlannerRow(string Planner, decimal Value, decimal Quantity, int MaterialCount);

/// <summary>
/// Ergebnis eines Lagerwert-Reads. <see cref="Rows"/> ist bewusst je Disponent aufgeschluesselt
/// und NICHT vorsummiert, damit die fachliche Abgrenzung (gehoert Disponent 004
/// "Betriebsmat/Einkau" dazu?) ohne SAP-Aenderung anpassbar bleibt.
/// </summary>
public sealed record StockValueSnapshot(
    string ValuationArea,
    IReadOnlyList<StockValueByPlannerRow> Rows,
    DateTime ReadAtUtc)
{
    public decimal TotalValue => Rows.Sum(row => row.Value);
    public int TotalMaterialCount => Rows.Sum(row => row.MaterialCount);

    /// <summary>Summe ueber genau die uebergebenen Disponenten. Unbekannte werden ignoriert.</summary>
    public decimal ValueForPlanners(IReadOnlyCollection<string> planners)
        => Rows.Where(row => planners.Contains(row.Planner, StringComparer.OrdinalIgnoreCase))
               .Sum(row => row.Value);

    public int MaterialCountForPlanners(IReadOnlyCollection<string> planners)
        => Rows.Where(row => planners.Contains(row.Planner, StringComparer.OrdinalIgnoreCase))
               .Sum(row => row.MaterialCount);
}

public interface ISapGatewayStockValueReader
{
    /// <summary>
    /// Liefert den zuletzt gelesenen Stand ohne SAP-Zugriff, oder <c>null</c>. Fuer die
    /// Anzeige gedacht: der eigentliche Read kostet ueber 100 MB und darf niemals an einem
    /// Seitenaufruf haengen.
    /// </summary>
    StockValueSnapshot? GetCached(string valuationArea);

    /// <summary>
    /// Liest den Lagerwert frisch aus SAP und legt ihn in den Cache. Teuer — nur aus dem
    /// Einkauf-Refresh aufrufen, nicht aus der Oberflaeche.
    /// </summary>
    Task<StockValueSnapshot> RefreshAsync(
        string serviceUrl,
        string username,
        string password,
        string valuationArea,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Liest den Lagerwert (MBEW-SALK3) je Disponent ueber das SAP-OData-Gateway.
///
/// FACHLICHER HINTERGRUND (docs/EINKAUF_LAGERWERT_2026-08-18.md): Armin hat den Lagerwert
/// der Einkaufsteile als KPI gewuenscht, abgegrenzt auf die Disponenten 001-005.
///
/// WARUM SALK3 UND NICHT LBKUM * STPRS: `SALK3` ist der gebuchte Bestandswert und ein
/// absoluter Betrag ohne Preiseinheit. Die Rechnung `Menge x Standardpreis` liefe dagegen in
/// die `PEINH`-Falle (Standardpreis gilt je X Stueck) und wuerde bei `PEINH = 100` um Faktor
/// 100 danebenliegen — derselbe Fehler, der bei der Gruppenmarge schon einmal auftrat.
///
/// WAS DIESER READER BEWUSST NICHT KANN: einen frei waehlbaren Stichtag. Dafuer braeuchte es
/// die Bewertungshistorie MBEWH, die mit 5,4 Mio Zeilen nicht ueber OData ladbar ist; dafuer
/// liegt der Entwurf eines serverseitig aggregierenden EntitySets unter
/// docs/abap/ZSTR_PURCH_STOCKVAL_GET_ENTITYSET.abap bereit. Dieser Reader liefert den
/// AKTUELLEN Stand — fachlich derselbe Zeitpunkt, den MB5L mit "Saldoauswahl lfd. Periode"
/// ausweist.
/// </summary>
public class SapGatewayStockValueReader : ISapGatewayStockValueReader
{
    /// <summary>Materialbewertung (MBEW). Im Service ZPOWERBI_EINKAUF_SRV vorhanden.</summary>
    public const string StockValueEntitySet = "mbewSet";

    /// <summary>Werksdaten (MARC). Traegt den Disponenten und ist im Service vorhanden.</summary>
    public const string PlantEntitySet = "MARCSet";

    /// <summary>
    /// Wie lange ein gelesener Stand als aktuell gilt. Der Lagerwert bewegt sich ueber den Tag
    /// nur langsam; ein taeglicher Stand aus dem Einkauf-Refresh genuegt fachlich.
    /// </summary>
    public static readonly TimeSpan CacheLifetime = TimeSpan.FromHours(20);

    private readonly IAppEventLogService _appEventLogService;
    private readonly Dictionary<string, StockValueSnapshot> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _cacheLock = new();

    public SapGatewayStockValueReader(IAppEventLogService appEventLogService)
    {
        _appEventLogService = appEventLogService;
    }

    public StockValueSnapshot? GetCached(string valuationArea)
    {
        if (string.IsNullOrWhiteSpace(valuationArea))
            return null;

        lock (_cacheLock)
        {
            if (!_cache.TryGetValue(valuationArea.Trim(), out var snapshot))
                return null;

            return DateTime.UtcNow - snapshot.ReadAtUtc > CacheLifetime ? null : snapshot;
        }
    }

    public async Task<StockValueSnapshot> RefreshAsync(
        string serviceUrl,
        string username,
        string password,
        string valuationArea,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(valuationArea))
            throw new ArgumentException("Ohne Bewertungskreis waere die Summe fachlich falsch, weil ueber " +
                                        "unterschiedliche Hauswaehrungen summiert wuerde.", nameof(valuationArea));

        var area = valuationArea.Trim();
        var baseUrl = serviceUrl.TrimEnd('/') + "/";
        using var client = CreateClient(username, password);

        await _appEventLogService.WriteAsync("SAP", "Lagerwert-Read gestartet",
            details: $"{baseUrl}{StockValueEntitySet} | Bwkey={area}");

        var plannerByMaterial = await LoadPlannerMapAsync(client, baseUrl, area, cancellationToken);
        var rows = await LoadValuationRowsAsync(client, baseUrl, area, cancellationToken);

        var snapshot = Aggregate(area, rows, plannerByMaterial, DateTime.UtcNow);

        lock (_cacheLock)
        {
            _cache[area] = snapshot;
        }

        await _appEventLogService.WriteAsync("SAP", "Lagerwert-Read beendet",
            details: $"{baseUrl}{StockValueEntitySet} | Bwkey={area} | Disponenten={snapshot.Rows.Count} | " +
                     $"Materialien={snapshot.TotalMaterialCount} | Wert={snapshot.TotalValue:N2}");

        return snapshot;
    }

    /// <summary>
    /// Reine Aggregation, ohne SAP. Faellt je Material auf den Disponenten aus MARC zurueck;
    /// Materialien ohne Disponenten laufen unter <see cref="WithoutPlanner"/>, damit sie in der
    /// Summe sichtbar bleiben statt stillschweigend zu verschwinden.
    /// </summary>
    internal static StockValueSnapshot Aggregate(
        string valuationArea,
        IEnumerable<Dictionary<string, object?>> valuationRows,
        IReadOnlyDictionary<string, string> plannerByMaterial,
        DateTime readAtUtc)
    {
        var accumulator = new Dictionary<string, (decimal Value, decimal Quantity, int Count)>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var row in valuationRows)
        {
            var material = MaterialKeyNormalizer.Normalize(GetText(row, "Matnr"));
            if (string.IsNullOrWhiteSpace(material))
                continue;

            // Getrennte Bewertung: SAP fuehrt je Material einen Kopfsatz (BWTAR leer) UND
            // Teilsaetze. Beide zu summieren wuerde den Wert verdoppeln. Am 2026-08-18 wurden
            // im Bewertungskreis 1100 zwar 0 Teilsaetze gemessen, die Einschraenkung steht
            // trotzdem hier, damit ein spaeter eingefuehrter Teilsatz nichts still verfaelscht.
            if (!string.IsNullOrWhiteSpace(GetText(row, "Bwtar")))
                continue;

            var value = ParseDecimal(GetText(row, "Salk3"));
            var quantity = ParseDecimal(GetText(row, "Lbkum"));

            var planner = plannerByMaterial.TryGetValue(material, out var found) && !string.IsNullOrWhiteSpace(found)
                ? found
                : WithoutPlanner;

            if (!accumulator.TryGetValue(planner, out var current))
                current = (0m, 0m, 0);

            accumulator[planner] = (current.Value + value, current.Quantity + quantity, current.Count + 1);
        }

        var rows = accumulator
            .Select(pair => new StockValueByPlannerRow(pair.Key, pair.Value.Value, pair.Value.Quantity, pair.Value.Count))
            .OrderByDescending(row => row.Value)
            .ToList();

        return new StockValueSnapshot(valuationArea, rows, readAtUtc);
    }

    /// <summary>Sammelbezeichnung fuer Materialien ohne gepflegten Disponenten.</summary>
    public const string WithoutPlanner = "(ohne Disponent)";

    /// <summary>
    /// Disponent je Material aus MARC. Gepagt gelesen — anders als `mbewSet` beherrscht
    /// `MARCSet` `$top`/`$skip`, der bestehende Einkauf-Loader nutzt das ebenso.
    /// </summary>
    private async Task<IReadOnlyDictionary<string, string>> LoadPlannerMapAsync(
        HttpClient client, string baseUrl, string valuationArea, CancellationToken cancellationToken)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        const int pageSize = 1000;
        var skip = 0;
        var sawDispoField = false;

        while (true)
        {
            var url = $"{baseUrl}{PlantEntitySet}?$format=json&$top={pageSize}&$skip={skip}" +
                      $"&$select=Matnr,Werks,Dispo&$filter={Uri.EscapeDataString($"Werks eq '{valuationArea}'")}";

            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                // Haeufigste Ursache: das EntitySet exponiert `Dispo` nicht. Das ist eine
                // SAP-seitige Frage und wird hier NICHT geraten, sondern klar gemeldet.
                throw new HttpRequestException(
                    $"SAP OData {PlantEntitySet} mit $select=Matnr,Werks,Dispo fehlgeschlagen " +
                    $"({(int)response.StatusCode} {response.ReasonPhrase}). Moeglicherweise liefert das " +
                    $"EntitySet das Feld 'Dispo' nicht — dann muss es in SEGW ergaenzt werden. " +
                    $"URL={url} Antwort={TrimForLog(error)}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var page = ParseRows(json);

            foreach (var row in page)
            {
                if (row.ContainsKey("Dispo"))
                    sawDispoField = true;

                var material = MaterialKeyNormalizer.Normalize(GetText(row, "Matnr"));
                if (string.IsNullOrWhiteSpace(material))
                    continue;

                map[material] = GetText(row, "Dispo");
            }

            if (page.Count < pageSize)
                break;

            skip += pageSize;
        }

        // Ein leeres Feld bei JEDER Zeile ist verdaechtig: dann liefert das Set die Spalte
        // zwar formal, aber ohne Inhalt, und die ganze Abgrenzung waere still wirkungslos.
        if (map.Count > 0 && !sawDispoField)
        {
            await _appEventLogService.WriteAsync("SAP", "Lagerwert: Disponent fehlt", "Warning",
                details: $"{baseUrl}{PlantEntitySet} liefert kein Feld 'Dispo'. Die Abgrenzung auf die " +
                         "Einkaufsdisponenten kann damit nicht gebildet werden.");
        }

        return map;
    }

    /// <summary>
    /// Bewertungszeilen aus MBEW. BEWUSST OHNE PAGINIERUNG: `mbewSet` ignoriert `$top`,
    /// `$skip` und `$orderby` und liefert bei jeder Anfrage den vollen Bestand (gemessen
    /// 2026-07-28: 68'543 Zeilen / 124 MB / ~28 s). Eine Paginierungsschleife wuerde hier
    /// endlos laufen — genau der Fehler, der den Standardpreis-Read einmal lahmgelegt hat.
    /// </summary>
    private async Task<List<Dictionary<string, object?>>> LoadValuationRowsAsync(
        HttpClient client, string baseUrl, string valuationArea, CancellationToken cancellationToken)
    {
        var filter = $"Bwkey eq '{valuationArea}'";
        var url = $"{baseUrl}{StockValueEntitySet}?$format=json&$filter={Uri.EscapeDataString(filter)}";

        using var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"SAP OData {StockValueEntitySet} fehlgeschlagen ({(int)response.StatusCode} {response.ReasonPhrase}) " +
                $"URL={url} Antwort={TrimForLog(error)}");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var rows = ParseRows(json);

        // Pruefen, ob der Bestandswert ueberhaupt geliefert wird. Fehlt `Salk3`, waere die
        // Kachel stumm 0 — ein stiller Nullwert ist schlimmer als ein klarer Fehler.
        if (rows.Count > 0 && !rows[0].ContainsKey("Salk3"))
        {
            var available = string.Join(", ", rows[0].Keys.OrderBy(key => key));
            throw new InvalidOperationException(
                $"Das EntitySet {StockValueEntitySet} liefert kein Feld 'Salk3' (Bestandswert). " +
                $"Ohne dieses Feld ist kein Lagerwert bildbar. Vorhandene Felder: {available}");
        }

        return rows;
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

    private static decimal ParseDecimal(string value)
        => decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0m;

    private static string TrimForLog(string value)
        => value.Length <= 500 ? value : value[..500];
}
