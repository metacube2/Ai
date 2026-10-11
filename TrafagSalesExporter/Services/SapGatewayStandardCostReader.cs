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

        // BEWUSST OHNE PAGINIERUNG, ein einziger Aufruf.
        //
        // `mbewSet` ignoriert `$top`, `$skip` UND `$orderby` und liefert bei jeder Anfrage den
        // vollen Bestand (am Produktivsystem gemessen 2026-07-28: 68'543 Zeilen / 124 MB /
        // ~28 s je Anfrage, unabhaengig von den Paginierungsparametern).
        //
        // Die fruehere Schleife brach erst ab, wenn eine Seite weniger als 1000 Zeilen
        // hatte. Bei 68'543 Zeilen pro "Seite" konnte das nie eintreten - sie lief endlos und
        // uebertrug in jeder Runde erneut 124 MB. Das ist die Ursache des als "haengend"
        // dokumentierten Standardpreis-Reads und der Grund, warum `GroupStandardCosts` seit
        // dem 2026-07-15 leer blieb (siehe docs/FINANCE_GRUPPENMARGE_2026-06-16.md).
        var url = $"{baseUrl}{StandardCostEntitySet}?$format=json&$filter={Uri.EscapeDataString(filter)}";
        using var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"SAP OData {StandardCostEntitySet} fehlgeschlagen ({(int)response.StatusCode} {response.ReasonPhrase}) " +
                $"URL={url} Antwort={TrimForLog(error)}");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var rows = ParseRows(json);
        foreach (var row in rows)
        {
            var mapped = MapRow(row);
            if (mapped is null)
                continue;
            // Doppelte Schluessel koennen durch Bewertungstypen (BWTAR) entstehen;
            // der erste Satz je Material/Bewertungskreis gewinnt.
            result.TryAdd(mapped.Value.Key, mapped.Value.Entry);
        }

        // Vollstaendigkeit gegen `$count` pruefen. Sollte der Service kuenftig doch
        // paginieren, bekaemen wir sonst stillschweigend nur einen Ausschnitt - genau die
        // Art Fehler, die hier schon zweimal unbemerkt blieb.
        var expectedRows = await TryGetRowCountAsync(client, baseUrl, filter, cancellationToken);
        if (expectedRows is not null && rows.Count < expectedRows.Value)
        {
            await _appEventLogService.WriteAsync("SAP", "Standardpreis-Read unvollstaendig", "Warning", land: land,
                details: $"{baseUrl}{StandardCostEntitySet} | erwartet={expectedRows.Value} | gelesen={rows.Count} | " +
                         "Der Service liefert offenbar nicht mehr den vollen Bestand - Paginierung pruefen.");
        }

        await _appEventLogService.WriteAsync("SAP", "Standardpreis-Read beendet", land: land,
            details: $"{baseUrl}{StandardCostEntitySet} | Zeilen={rows.Count} | Materialien={result.Count}" +
                     (expectedRows is not null ? $" | laut $count={expectedRows.Value}" : ""));
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

    /// <summary>
    /// Erwartete Zeilenzahl ueber <c>$count</c>. Nur zur Plausibilisierung - schlaegt der
    /// Aufruf fehl, ist das kein Grund, den Import scheitern zu lassen.
    /// </summary>
    private static async Task<int?> TryGetRowCountAsync(
        HttpClient client, string baseUrl, string filter, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"{baseUrl}{StandardCostEntitySet}/$count?$filter={Uri.EscapeDataString(filter)}";
            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var text = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();
            return int.TryParse(text, out var count) ? count : null;
        }
        catch
        {
            return null;
        }
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
