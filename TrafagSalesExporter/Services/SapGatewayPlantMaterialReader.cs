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

    /// <summary>
    /// Wie <see cref="GetMaterialKeysAsync"/>, zusaetzlich mit deutschem Materialtext je Material, sofern das EntitySet die
    /// Property <see cref="SapGatewayPlantMaterialReader.MaterialTextProperty"/> fuehrt; sonst ist der Text leer.
    /// </summary>
    Task<IReadOnlyDictionary<string, string>> GetMaterialsAsync(
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

    /// <summary>
    /// PLATZHALTER (2026-11-19, Plan Mapping/Issue Topic): Name der Property fuer den deutschen Materialtext (MAKT-MAKTX mit
    /// SPRAS = 'D' fest im DPC, nie sy-langu) am <see cref="EntitySet"/>. Das Gateway fuehrt sie heute nicht; die Erweiterung
    /// in T76 beauftragt Ingo separat. Der Reader fragt sie nur ab, wenn $metadata sie zeigt; eine unbekannte Property im
    /// $select ergaebe sonst 400 und der Werkstamm-Abzug fiele taeglich aus. Nach dem Import ist nichts mehr zu konfigurieren.
    /// </summary>
    public const string MaterialTextProperty = "Maktx";

    public async Task<IReadOnlySet<string>> GetMaterialKeysAsync(
        string serviceUrl,
        string username,
        string password,
        string plant,
        string land,
        CancellationToken cancellationToken = default)
        => (await GetMaterialsAsync(serviceUrl, username, password, plant, land, cancellationToken))
            .Keys.ToHashSet(StringComparer.Ordinal);

    public async Task<IReadOnlyDictionary<string, string>> GetMaterialsAsync(
        string serviceUrl,
        string username,
        string password,
        string plant,
        string land,
        CancellationToken cancellationToken = default)
    {
        var normalizedPlant = plant?.Trim() ?? string.Empty;
        if (normalizedPlant.Length == 0)
            return new Dictionary<string, string>(StringComparer.Ordinal);

        var entitySets = await _sapGatewayService.GetEntitySetsAsync(
            serviceUrl, username, password, cancellationToken);
        if (!entitySets.Any(name => string.Equals(name, EntitySet, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Der SAP-Service enthaelt das EntitySet '{EntitySet}' nicht.");

        var withText = false;
        try
        {
            var fields = await _sapGatewayService.GetEntityFieldNamesAsync(
                serviceUrl, EntitySet, username, password, cancellationToken);
            withText = fields.Any(name => string.Equals(name, MaterialTextProperty, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Der Text ist eine Anreicherung; ohne $metadata bleibt der bisherige Abzug unveraendert.
            await _appEventLogService.WriteAsync("SAP", "CH-Werkstamm: Feldliste nicht lesbar, Abzug ohne Materialtext",
                "Warning", land: land, details: ex.Message);
        }

        var baseUrl = serviceUrl.TrimEnd('/') + "/";
        var url = BuildUrl(baseUrl, withText);
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

        var result = ParseMaterials(await response.Content.ReadAsStringAsync(cancellationToken), normalizedPlant);
        await _appEventLogService.WriteAsync("SAP", "CH-Werkstamm-Read beendet", land: land,
            details: $"{baseUrl}{EntitySet} | Werk={normalizedPlant} | Materialien={result.Count} | " +
                     $"Materialtext={(withText ? result.Values.Count(text => text.Length > 0).ToString() : "nicht geliefert")}");
        return result;
    }

    /// <summary>
    /// URL des Abzugs. Mit Text: Property im $select und feste Anmeldesprache Deutsch (sap-language=DE, LEARNINGS 18.11.:
    /// ohne feste Sprache lief eine Gateway-Anfrage englisch und verwarf Saetze ohne englischen Text).
    /// </summary>
    internal static string BuildUrl(string baseUrl, bool withText)
        => withText
            ? $"{baseUrl}{EntitySet}?$format=json&$select={Uri.EscapeDataString("Matnr,Werks," + MaterialTextProperty)}&sap-language=DE"
            : $"{baseUrl}{EntitySet}?$format=json&$select={Uri.EscapeDataString("Matnr,Werks")}";

    public static HashSet<string> ParseMaterialKeys(string json, string plant)
        => ParseMaterials(json, plant).Keys.ToHashSet(StringComparer.Ordinal);

    /// <summary>Materialschluessel des Werks mit Text (leer, wenn die Property fehlt). Erster nicht leerer Text gewinnt.</summary>
    public static Dictionary<string, string> ParseMaterials(string json, string plant)
    {
        using var document = JsonDocument.Parse(json);
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
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
            if (key.Length == 0)
                continue;

            var text = row.TryGetProperty(MaterialTextProperty, out var textElement) &&
                       textElement.ValueKind == JsonValueKind.String
                ? textElement.GetString()?.Trim() ?? string.Empty
                : string.Empty;
            if (!result.TryGetValue(key, out var existing) || (existing.Length == 0 && text.Length > 0))
                result[key] = text;
        }

        return result;
    }

    private static string TrimForLog(string value)
        => value.Length <= 500 ? value : value[..500];
}
