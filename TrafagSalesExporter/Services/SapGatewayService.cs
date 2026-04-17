using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace TrafagSalesExporter.Services;

public class SapGatewayService : ISapGatewayService
{
    private static readonly XNamespace AppNs = "http://www.w3.org/2007/app";
    private static readonly XNamespace EdmNs = "http://docs.oasis-open.org/odata/ns/edm";
    private readonly IAppEventLogService _appEventLogService;

    public SapGatewayService(IAppEventLogService appEventLogService)
    {
        _appEventLogService = appEventLogService;
    }

    public async Task TestConnectionAsync(string serviceUrl, string username, string password, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(username, password);
        var baseUrl = BuildServiceUri(serviceUrl);
        await _appEventLogService.WriteAsync("SAP", "Gateway-Verbindungstest gestartet", details: baseUrl);
        using var response = await client.GetAsync(baseUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        await _appEventLogService.WriteAsync("SAP", "Gateway-Verbindungstest erfolgreich", details: $"{baseUrl} | HTTP {(int)response.StatusCode}");
    }

    public async Task<List<string>> GetEntitySetsAsync(string serviceUrl, string username, string password, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(username, password);
        var baseUrl = BuildServiceUri(serviceUrl);
        await _appEventLogService.WriteAsync("SAP", "Entity-Set-Refresh gestartet", details: baseUrl);

        var entitySets = await TryReadEntitySetsFromServiceRootAsync(client, baseUrl, cancellationToken);
        if (entitySets.Count > 0)
        {
            await _appEventLogService.WriteAsync("SAP", "Entity Sets aus Service-Root geladen", details: $"{baseUrl} | Count={entitySets.Count}");
            return entitySets;
        }

        var metadataEntitySets = await ReadEntitySetsFromMetadataAsync(client, baseUrl, cancellationToken);
        await _appEventLogService.WriteAsync("SAP", "Entity Sets aus $metadata geladen", details: $"{baseUrl} | Count={metadataEntitySets.Count}");
        return metadataEntitySets;
    }

    public async Task<List<string>> GetEntityFieldNamesAsync(string serviceUrl, string entitySet, string username, string password, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(username, password);
        var baseUrl = BuildServiceUri(serviceUrl);
        await _appEventLogService.WriteDebugAsync("SAP", "Feldliste aus $metadata laden", details: $"{baseUrl} | EntitySet={entitySet}");

        using var response = await client.GetAsync($"{baseUrl}$metadata", cancellationToken);
        response.EnsureSuccessStatusCode();

        var xml = await response.Content.ReadAsStringAsync(cancellationToken);
        var document = XDocument.Parse(xml);

        var entitySetElement = document
            .Descendants()
            .FirstOrDefault(x => string.Equals(x.Name.LocalName, "EntitySet", StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.Attribute("Name")?.Value, entitySet, StringComparison.OrdinalIgnoreCase));

        var entityTypeFullName = entitySetElement?.Attribute("EntityType")?.Value;
        if (string.IsNullOrWhiteSpace(entityTypeFullName))
            return [];

        var typeName = entityTypeFullName.Split('.').LastOrDefault();
        if (string.IsNullOrWhiteSpace(typeName))
            return [];

        var entityTypeElement = document
            .Descendants()
            .FirstOrDefault(x => string.Equals(x.Name.LocalName, "EntityType", StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.Attribute("Name")?.Value, typeName, StringComparison.OrdinalIgnoreCase));

        if (entityTypeElement is null)
            return [];

        return entityTypeElement
            .Elements()
            .Where(x => string.Equals(x.Name.LocalName, "Property", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Attribute("Name")?.Value ?? string.Empty)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<List<Dictionary<string, object?>>> GetEntityRowsAsync(string serviceUrl, string entitySet, string username, string password, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(username, password);
        var requestUrl = $"{BuildServiceUri(serviceUrl)}{entitySet}?$format=json";
        await _appEventLogService.WriteAsync("SAP", "Entity-Read gestartet", details: requestUrl);
        using var response = await client.GetAsync(requestUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("d", out var dNode))
            return [];

        if (!dNode.TryGetProperty("results", out var resultsNode) || resultsNode.ValueKind != JsonValueKind.Array)
            return [];

        var rows = new List<Dictionary<string, object?>>();
        var counter = 0;
        foreach (var item in resultsNode.EnumerateArray())
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in item.EnumerateObject())
            {
                row[property.Name] = ConvertJsonValue(property.Value);
            }

            rows.Add(row);
            counter++;
            if (counter % 250 == 0)
            {
                await _appEventLogService.WriteDebugAsync("SAP", "Entity-Read liest Daten",
                    details: $"{requestUrl} | Bisher gelesene Zeilen={counter}");
            }
        }

        await _appEventLogService.WriteAsync("SAP", "Entity-Read beendet", details: $"{requestUrl} | Zeilen={rows.Count}");
        return rows;
    }

    private static HttpClient CreateClient(string username, string password)
    {
        var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(15);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/atomsvc+xml"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));
        return client;
    }

    private static string BuildServiceUri(string serviceUrl)
    {
        var trimmed = serviceUrl.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new InvalidOperationException("SAP Service URL darf nicht leer sein.");

        var entityPathMarker = "/sap/opu/odata/sap/";
        var markerIndex = trimmed.IndexOf(entityPathMarker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex >= 0)
        {
            var servicePath = trimmed[(markerIndex + entityPathMarker.Length)..].Trim('/');
            var parts = servicePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                trimmed = $"{trimmed[..(markerIndex + entityPathMarker.Length)]}{parts[0]}/";
            }
        }

        return trimmed.EndsWith('/') ? trimmed : $"{trimmed}/";
    }

    private static async Task<List<string>> TryReadEntitySetsFromServiceRootAsync(HttpClient client, string baseUrl, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(baseUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        var xml = await response.Content.ReadAsStringAsync(cancellationToken);
        var document = XDocument.Parse(xml);

        return document
            .Descendants(AppNs + "collection")
            .Select(x => x.Attribute("href")?.Value ?? string.Empty)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static async Task<List<string>> ReadEntitySetsFromMetadataAsync(HttpClient client, string baseUrl, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync($"{baseUrl}$metadata", cancellationToken);
        response.EnsureSuccessStatusCode();

        var xml = await response.Content.ReadAsStringAsync(cancellationToken);
        var document = XDocument.Parse(xml);

        return document
            .Descendants(EdmNs + "EntitySet")
            .Select(x => x.Attribute("Name")?.Value ?? string.Empty)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static object? ConvertJsonValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.ToString(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        _ => element.ToString()
    };
}
