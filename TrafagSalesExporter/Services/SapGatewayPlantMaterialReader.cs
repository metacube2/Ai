using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TrafagSalesExporter.Services;

public interface ISapGatewayPlantMaterialReader
{
    Task<IReadOnlySet<string>> GetMaterialKeysAsync(
        string serviceUrl,
        string username,
        string password,
        string plant,
        string land,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Liest den werksbezogenen Materialstamm aus MARC. MARCSet ignoriert auf dem
/// produktiven Gateway $top/$skip/$filter; deshalb wird der Bestand genau einmal
/// gelesen und Werk 1100 clientseitig gefiltert.
/// </summary>
public sealed class SapGatewayPlantMaterialReader : ISapGatewayPlantMaterialReader
{
    public const string EntitySet = "MARCSet";
    // Live 2026-08-11: 66'047 Materialien in Werk 1100. Falls SAP kuenftig doch
    // paginiert oder nur einen Ausschnitt liefert, darf dieser nie den Vollcache ersetzen.
    public const int MinimumExpectedChPlantMaterials = 50_000;

    private readonly ISapGatewayService _sapGatewayService;
    private readonly IAppEventLogService _appEventLogService;

    public SapGatewayPlantMaterialReader(
        ISapGatewayService sapGatewayService,
        IAppEventLogService appEventLogService)
    {
        _sapGatewayService = sapGatewayService;
        _appEventLogService = appEventLogService;
    }

    public async Task<IReadOnlySet<string>> GetMaterialKeysAsync(
        string serviceUrl,
        string username,
        string password,
        string plant,
        string land,
        CancellationToken cancellationToken = default)
    {
        var normalizedPlant = plant?.Trim() ?? string.Empty;
        if (normalizedPlant.Length == 0)
            return new HashSet<string>(StringComparer.Ordinal);

        var entitySets = await _sapGatewayService.GetEntitySetsAsync(
            serviceUrl, username, password, cancellationToken);
        if (!entitySets.Any(name => string.Equals(name, EntitySet, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Der SAP-Service enthaelt das EntitySet '{EntitySet}' nicht.");

        var baseUrl = serviceUrl.TrimEnd('/') + "/";
        var url = $"{baseUrl}{EntitySet}?$format=json&$select={Uri.EscapeDataString("Matnr,Werks")}";
        await _appEventLogService.WriteAsync("SAP", "CH-Werkstamm-Read gestartet", land: land,
            details: $"{baseUrl}{EntitySet} | Werk={normalizedPlant}");

        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"SAP OData {EntitySet} fehlgeschlagen ({(int)response.StatusCode} {response.ReasonPhrase}) " +
                $"URL={url} Antwort={TrimForLog(error)}");
        }

        var result = ParseMaterialKeys(await response.Content.ReadAsStringAsync(cancellationToken), normalizedPlant);
        await _appEventLogService.WriteAsync("SAP", "CH-Werkstamm-Read beendet", land: land,
            details: $"{baseUrl}{EntitySet} | Werk={normalizedPlant} | Materialien={result.Count}");
        return result;
    }

    public static HashSet<string> ParseMaterialKeys(string json, string plant)
    {
        using var document = JsonDocument.Parse(json);
        var result = new HashSet<string>(StringComparer.Ordinal);
        if (!document.RootElement.TryGetProperty("d", out var d) ||
            !d.TryGetProperty("results", out var rows) ||
            rows.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var row in rows.EnumerateArray())
        {
            if (!row.TryGetProperty("Werks", out var werks) ||
                !string.Equals(werks.GetString()?.Trim(), plant, StringComparison.OrdinalIgnoreCase) ||
                !row.TryGetProperty("Matnr", out var matnr))
                continue;

            var key = MaterialKeyNormalizer.Normalize(matnr.GetString());
            if (key.Length > 0)
                result.Add(key);
        }

        return result;
    }

    private static string TrimForLog(string value)
        => value.Length <= 500 ? value : value[..500];
}
