using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TrafagSalesExporter.Services;

/// <summary>Standardpreis eines Materials, bereits auf EINE Einheit normiert.</summary>
public sealed record StandardCostEntry(decimal UnitCost, string Currency);

/// <summary>
/// Liest die Standardpreise (MBEW-STPRS) der CH/AT-Gesellschaften ueber das
/// SAP-OData-Gateway aus dem EntitySet <see cref="StandardCostEntitySet"/>.
///
/// Hintergrund: Der Umsatz-Service (`FinanzdataSchweizOeSet`) liefert keinen
/// Kostenwert; `StandardCost` war fuer ZSCHWEIZ deshalb hart auf 0 gemappt und
/// die Gruppenmarge fuer CH/AT nicht berechenbar. `mbewSet` ist im Service
/// bereits vorhanden (per Metadata-Cache und ABAP-Analysereport
/// `docs/abap/ZFIN_ANALYSE_STPRS_JOURNAL.abap` bestaetigt: 96.3 % der Materialien
/// im Bewertungskreis 1100 und 99.6 % in 1200 haben einen Standardpreis).
/// </summary>
public interface ISapGatewayStandardCostReader
{
    Task<IReadOnlyDictionary<StandardCostKey, StandardCostEntry>> GetStandardCostsAsync(
        string serviceUrl,
        string username,
        string password,
        IReadOnlyCollection<string> valuationAreas,
        string land,
        CancellationToken cancellationToken = default);
}

/// <summary>MBEW ist je Material UND Bewertungskreis verschluesselt — beides gehoert zum Schluessel.</summary>
public readonly record struct StandardCostKey(string MaterialKey, string ValuationArea);

public class SapGatewayStandardCostReader : ISapGatewayStandardCostReader
{
    /// <summary>Materialbewertung (MBEW). Existiert im Service ZPOWERBI_EINKAUF_SRV bereits.</summary>
    public const string StandardCostEntitySet = "mbewSet";

    private const int PageSize = 1000;
    private readonly ISapGatewayService _sapGatewayService;
    private readonly IAppEventLogService _appEventLogService;

    public SapGatewayStandardCostReader(
        ISapGatewayService sapGatewayService,
        IAppEventLogService appEventLogService)
    {
        _sapGatewayService = sapGatewayService;
        _appEventLogService = appEventLogService;
    }

    public async Task<IReadOnlyDictionary<StandardCostKey, StandardCostEntry>> GetStandardCostsAsync(
        string serviceUrl,
        string username,
        string password,
        IReadOnlyCollection<string> valuationAreas,
        string land,
        CancellationToken cancellationToken = default)
    {
        var areas = valuationAreas
            .Where(area => !string.IsNullOrWhiteSpace(area))
            .Select(area => area.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var result = new Dictionary<StandardCostKey, StandardCostEntry>();
        if (areas.Count == 0)
            return result;

        await EnsureEntitySetExistsAsync(serviceUrl, username, password, land, cancellationToken);

        var baseUrl = serviceUrl.TrimEnd('/') + "/";
        var filter = string.Join(" or ", areas.Select(area => $"Bwkey eq '{area}'"));
        await _appEventLogService.WriteAsync("SAP", "Standardpreis-Read gestartet", land: land,
            details: $"{baseUrl}{StandardCostEntitySet} | Filter={filter}");

        using var client = CreateClient(username, password);
        for (var skip = 0; ; skip += PageSize)
        {
            var url = $"{baseUrl}{StandardCostEntitySet}?$format=json&$top={PageSize}&$skip={skip}" +
                      $"&$orderby={Uri.EscapeDataString("Bwkey,Matnr")}" +
                      $"&$filter={Uri.EscapeDataString(filter)}";
            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"SAP OData {StandardCostEntitySet} fehlgeschlagen ({(int)response.StatusCode} {response.ReasonPhrase}) " +
                    $"URL={url} Antwort={TrimForLog(error)}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var page = ParseRows(json);
            foreach (var row in page)
            {
                var mapped = MapRow(row);
                if (mapped is null)
                    continue;
                // Doppelte Schluessel koennen durch Bewertungstypen (BWTAR) entstehen;
                // der erste Satz je Material/Bewertungskreis gewinnt.
                result.TryAdd(mapped.Value.Key, mapped.Value.Entry);
            }

            if (page.Count < PageSize)
                break;
        }

        await _appEventLogService.WriteAsync("SAP", "Standardpreis-Read beendet", land: land,
            details: $"{baseUrl}{StandardCostEntitySet} | Materialien={result.Count}");
        return result;
    }

    /// <summary>
    /// Pure Zeilen-Abbildung. WICHTIG: `STPRS` gilt pro `PEINH` Einheiten (Preiseinheit).
    /// Die Downstream-Margenlogik rechnet `Menge x StandardCost`, erwartet also einen
    /// STUECKpreis — deshalb wird hier durch die Preiseinheit geteilt. Ohne diese Division
    /// waere die Kostenbasis bei `PEINH = 100` um Faktor 100 zu hoch.
    /// </summary>
    public static (StandardCostKey Key, StandardCostEntry Entry)? MapRow(
        IReadOnlyDictionary<string, object?> row,
        string currency = "")
    {
        var material = MaterialKeyNormalizer.Normalize(GetText(row, "Matnr"));
        var valuationArea = GetText(row, "Bwkey");
        if (string.IsNullOrWhiteSpace(material) || string.IsNullOrWhiteSpace(valuationArea))
            return null;

        var standardPrice = ParseDecimal(GetText(row, "Stprs"));
        if (standardPrice <= 0m)
            return null;

        var priceUnit = ParseDecimal(GetText(row, "Peinh"));
        if (priceUnit <= 0m)
            priceUnit = 1m;

        var unitCost = standardPrice / priceUnit;
        return (new StandardCostKey(material, valuationArea), new StandardCostEntry(unitCost, currency));
    }

    private async Task EnsureEntitySetExistsAsync(
        string serviceUrl, string username, string password, string land, CancellationToken cancellationToken)
    {
        var entitySets = await _sapGatewayService.GetEntitySetsAsync(serviceUrl, username, password, cancellationToken);
        if (entitySets.Any(name => string.Equals(name, StandardCostEntitySet, StringComparison.OrdinalIgnoreCase)))
            return;

        await _appEventLogService.WriteAsync("SAP", "Standardpreis-EntitySet fehlt", "Error", land: land,
            details: $"{serviceUrl} | erwartet={StandardCostEntitySet}");
        throw new InvalidOperationException(
            $"Der SAP-Service enthaelt das EntitySet '{StandardCostEntitySet}' nicht. " +
            "Ohne Materialbewertung kann fuer CH/AT keine Kostenbasis ermittelt werden.");
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
