using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TrafagSalesExporter.Services;

public interface ISapGatewayMaterialTextReader
{
    /// <summary>Deutsche Materialtexte (MAKT, SPRAS D) je normalisierter Materialnummer, nur fuer <paramref name="wantedKeys"/>.</summary>
    Task<IReadOnlyDictionary<string, string>> GetGermanTextsAsync(
        string serviceUrl, string username, string password, IReadOnlySet<string> wantedKeys,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Liest deutsche Materialtexte aus <c>MAKTSet</c> (Entscheid Ingo 2026-11-19: zuerst MAKTSet statt einer SAP-Aenderung).
/// Nur im Mapping-Vorschlagslauf (Knopf), nie im taeglichen Export.
///
/// Laut saptasks/zeichnungsaenderung/NOTES.md (02.11.) liefert MAKTSet den ganzen MAKT-Bestand (rund 68'000 Materialien,
/// alle Sprachen) und ignoriert $filter, $top und $skip. Deshalb: ein Aufruf mit $filter Spras eq 'D' (wirkt, falls das Set
/// ihn doch auswertet), feste Anmeldesprache DE, $select nur Matnr, Spras, Maktx; Sprache wird in der App noch einmal
/// geprueft. Speichersparend: die Antwort wird als Strom geparst (kein zweiter String), behalten werden nur deutsche
/// Texte der gesuchten Materialien.
/// </summary>
public sealed class SapGatewayMaterialTextReader : ISapGatewayMaterialTextReader
{
    public const string EntitySet = "MAKTSet";

    public async Task<IReadOnlyDictionary<string, string>> GetGermanTextsAsync(
        string serviceUrl, string username, string password, IReadOnlySet<string> wantedKeys,
        CancellationToken cancellationToken = default)
    {
        // Erst mit $filter; liefert das nichts Deutsches oder lehnt das Set den Filter ab (Sprachschluessel im OData je nach
        // Dienst "D" oder "DE"), einmal ohne Filter wiederholen und nur in der App filtern.
        try
        {
            var filtered = await ReadAsync(BuildUrl(serviceUrl, withFilter: true), username, password, wantedKeys, cancellationToken);
            if (filtered.Count > 0)
                return filtered;
        }
        catch (HttpRequestException)
        {
        }

        return await ReadAsync(BuildUrl(serviceUrl, withFilter: false), username, password, wantedKeys, cancellationToken);
    }

    private static async Task<IReadOnlyDictionary<string, string>> ReadAsync(
        string url, string username, string password, IReadOnlySet<string> wantedKeys, CancellationToken cancellationToken)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"SAP OData {EntitySet} fehlgeschlagen ({(int)response.StatusCode} {response.ReasonPhrase}). " +
                (error.Length <= 300 ? error : error[..300]));
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await ParseGermanTextsAsync(stream, wantedKeys, cancellationToken);
    }

    internal static string BuildUrl(string serviceUrl, bool withFilter)
        => $"{serviceUrl.TrimEnd('/')}/{EntitySet}?$format=json" +
           $"&$select={Uri.EscapeDataString("Matnr,Spras,Maktx")}" +
           (withFilter ? $"&$filter={Uri.EscapeDataString("Spras eq 'D'")}" : string.Empty) +
           "&sap-language=DE";

    /// <summary>
    /// Nur deutsche Zeilen (Spras „D“ oder „DE“), nur gesuchte Materialien, erster nicht leerer Text gewinnt.
    /// Getrennt vom Abruf, damit ohne SAP testbar.
    /// </summary>
    public static async Task<IReadOnlyDictionary<string, string>> ParseGermanTextsAsync(
        Stream stream, IReadOnlySet<string> wantedKeys, CancellationToken cancellationToken = default)
    {
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!document.RootElement.TryGetProperty("d", out var d) ||
            !d.TryGetProperty("results", out var rows) ||
            rows.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var row in rows.EnumerateArray())
        {
            var spras = Text(row, "Spras").ToUpperInvariant();
            if (spras is not ("D" or "DE"))
                continue;
            var key = MaterialKeyNormalizer.Normalize(Text(row, "Matnr"));
            if (key.Length == 0 || !wantedKeys.Contains(key) || result.ContainsKey(key))
                continue;
            var text = Text(row, "Maktx");
            if (text.Length > 0)
                result[key] = text;
        }

        return result;
    }

    private static string Text(JsonElement row, string property)
        => row.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()?.Trim() ?? string.Empty
            : string.Empty;
}
