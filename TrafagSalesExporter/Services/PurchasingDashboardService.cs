using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;

namespace TrafagSalesExporter.Services;

public sealed class PurchasingDashboardService : IPurchasingDashboardService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public PurchasingDashboardService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<PurchasingDashboardLiveState> LoadAsync(CancellationToken cancellationToken = default)
    {
        var state = new PurchasingDashboardLiveState();

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var sap = await db.SourceSystemDefinitions.AsNoTracking().FirstOrDefaultAsync(x => x.Code == "SAP", cancellationToken);
            var site = await db.Sites.AsNoTracking().FirstOrDefaultAsync(x => x.TSC == PurchasingDataSourcePageService.PurchasingTsc, cancellationToken);
            if (sap is null || site is null)
            {
                state.Message = "SAP Einkaufsquelle ist noch nicht konfiguriert.";
                return state;
            }

            var serviceUrl = string.IsNullOrWhiteSpace(site.SapServiceUrl) ? sap.CentralServiceUrl : site.SapServiceUrl;
            var username = string.IsNullOrWhiteSpace(site.UsernameOverride) ? sap.CentralUsername : site.UsernameOverride;
            var password = string.IsNullOrWhiteSpace(site.PasswordOverride) ? sap.CentralPassword : site.PasswordOverride;
            if (string.IsNullOrWhiteSpace(serviceUrl) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                state.Message = "SAP URL oder Zugangsdaten fehlen.";
                return state;
            }

            using var client = CreateClient(username, password);
            var baseUrl = serviceUrl.TrimEnd('/') + "/";
            var currentYear = DateTime.Today.Year;
            var ekkoFilter = Uri.EscapeDataString($"Bedat ge '{currentYear}-01-01'");
            var ekkoCount = await ReadCountAsync(
                client,
                $"{baseUrl}EKKOSet/$count?$filter={ekkoFilter}",
                cancellationToken);
            var ekkoRows = await ReadRowsAsync(
                client,
                $"{baseUrl}EKKOSet?$format=json&$top=1000&$filter={ekkoFilter}&$select=Ebeln,Bedat,Lifnr",
                cancellationToken);

            state.SapReachable = true;
            state.EkkoLoaded = ekkoRows.Count > 0;
            state.PurchaseOrderCount = ekkoCount ?? ekkoRows.Count;
            state.SupplierCount = ekkoRows
                .Select(row => GetText(row, "Lifnr"))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
            state.LatestOrderDate = ekkoRows
                .Select(row => TryParseSapDate(GetText(row, "Bedat")))
                .Where(date => date.HasValue)
                .Select(date => date!.Value)
                .OrderByDescending(date => date)
                .Cast<DateTime?>()
                .FirstOrDefault();

            var firstEbeln = ekkoRows.Select(row => GetText(row, "Ebeln")).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
            if (!string.IsNullOrWhiteSpace(firstEbeln))
            {
                var ekpoRows = await ReadRowsAsync(
                    client,
                    $"{baseUrl}EKPOSet?$format=json&$top=50&$filter={Uri.EscapeDataString($"Ebeln eq '{firstEbeln}'")}",
                    cancellationToken);
                state.PositionSampleCount = ekpoRows.Count;
                state.EkpoLoaded = ekpoRows.Count > 0;

                var eketRows = await ReadRowsAsync(
                    client,
                    $"{baseUrl}eketSet?$format=json&$top=50&$filter={Uri.EscapeDataString($"Ebeln eq '{firstEbeln}'")}",
                    cancellationToken);
                state.ScheduleSampleCount = eketRows.Count;
                state.EketLoaded = eketRows.Count > 0;
            }

            state.Message = state.EkpoLoaded
                ? "SAP Einkaufsdaten geladen."
                : "EKKO ist live geladen; EKPO/EKET liefern aktuell noch keine Positionsdaten.";
        }
        catch (Exception ex)
        {
            state.Message = $"SAP Einkauf konnte nicht geladen werden: {ex.Message}";
        }

        return state;
    }

    private static HttpClient CreateClient(string username, string password)
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private static async Task<List<Dictionary<string, object?>>> ReadRowsAsync(HttpClient client, string url, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return [];

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("d", out var d) ||
            !d.TryGetProperty("results", out var results) ||
            results.ValueKind != JsonValueKind.Array)
            return [];

        return results.EnumerateArray()
            .Select(item => item.EnumerateObject()
                .Where(property => property.Name != "__metadata")
                .ToDictionary(property => property.Name, property => ConvertJsonValue(property.Value), StringComparer.OrdinalIgnoreCase))
            .ToList();
    }

    private static async Task<int?> ReadCountAsync(HttpClient client, string url, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        return int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;
    }

    private static object? ConvertJsonValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number when value.TryGetDecimal(out var number) => number,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        _ => value.ToString()
    };

    private static string GetText(Dictionary<string, object?> row, string key)
        => row.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty : string.Empty;

    private static DateTime? TryParseSapDate(string value)
    {
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
            return parsed;

        return DateTime.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed)
            ? parsed
            : null;
    }
}
