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
                    $"{baseUrl}EKPOSet?$format=json&$top=1000&$filter={Uri.EscapeDataString($"Ebeln ge '{firstEbeln}'")}",
                    cancellationToken);
                state.PositionSampleCount = ekpoRows.Count;
                state.EkpoLoaded = ekpoRows.Count > 0;

                var eketRows = await ReadRowsAsync(
                    client,
                    $"{baseUrl}eketSet?$format=json&$top=1000&$filter={Uri.EscapeDataString($"Ebeln ge '{firstEbeln}'")}",
                    cancellationToken);
                state.ScheduleSampleCount = eketRows.Count;
                state.EketLoaded = eketRows.Count > 0;

                ApplyEkpoMetrics(state, ekkoRows, ekpoRows);
                ApplyEketMetrics(state, ekpoRows, eketRows);
            }

            state.Message = state.EkpoLoaded && state.EketLoaded
                ? "SAP Einkaufsdaten inkl. EKPO/EKET geladen."
                : state.EkpoLoaded
                    ? "SAP Einkaufsdaten inkl. EKPO geladen; EKET liefert noch keine Termindaten."
                    : "EKKO ist live geladen; EKPO/EKET liefern aktuell noch keine Positionsdaten.";
        }
        catch (Exception ex)
        {
            state.Message = $"SAP Einkauf konnte nicht geladen werden: {ex.Message}";
        }

        return state;
    }

    private static void ApplyEkpoMetrics(
        PurchasingDashboardLiveState state,
        List<Dictionary<string, object?>> ekkoRows,
        List<Dictionary<string, object?>> ekpoRows)
    {
        if (ekpoRows.Count == 0)
            return;

        var supplierByEbeln = ekkoRows
            .Select(row => new { Ebeln = GetText(row, "Ebeln"), Lifnr = GetText(row, "Lifnr") })
            .Where(row => !string.IsNullOrWhiteSpace(row.Ebeln))
            .GroupBy(row => row.Ebeln, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Lifnr, StringComparer.OrdinalIgnoreCase);

        var enriched = ekpoRows
            .Select(row =>
            {
                var ebeln = GetText(row, "Ebeln");
                supplierByEbeln.TryGetValue(ebeln, out var supplier);
                var netwr = GetDecimal(row, "Netwr");
                var quantity = GetDecimal(row, "Menge");
                return new
                {
                    Ebeln = ebeln,
                    Supplier = string.IsNullOrWhiteSpace(supplier) ? "ohne Lieferant" : supplier,
                    Material = FirstNonEmpty(GetText(row, "Matnr"), GetText(row, "Txz01"), "ohne Artikel"),
                    MaterialGroup = FirstNonEmpty(GetText(row, "Matkl"), "ohne Warengruppe"),
                    NetValue = netwr,
                    Quantity = quantity
                };
            })
            .ToList();

        state.SpendChfSample = enriched.Sum(row => row.NetValue);
        state.TopSupplierLabel = BuildTopLabel(enriched.GroupBy(row => row.Supplier), row => row.NetValue, "Lieferant");
        state.TopMaterialGroupLabel = BuildTopLabel(enriched.GroupBy(row => row.MaterialGroup), row => row.NetValue, "Warengruppe");
        state.TopArticleLabel = BuildTopLabel(enriched.GroupBy(row => row.Material), row => row.NetValue, "Artikel");
        state.SpendChartRows = enriched
            .GroupBy(row => row.Supplier)
            .Select(group => new PurchasingLiveChartPoint($"Lief. {group.Key}", group.Sum(row => row.NetValue)))
            .OrderByDescending(row => row.Value)
            .Take(6)
            .ToList();
    }

    private static void ApplyEketMetrics(
        PurchasingDashboardLiveState state,
        List<Dictionary<string, object?>> ekpoRows,
        List<Dictionary<string, object?>> eketRows)
    {
        if (eketRows.Count == 0)
            return;

        var netPriceByPosition = ekpoRows
            .Select(row =>
            {
                var ebeln = GetText(row, "Ebeln");
                var ebelp = GetText(row, "Ebelp");
                var key = $"{ebeln}|{ebelp}";
                var quantity = GetDecimal(row, "Menge");
                var netValue = GetDecimal(row, "Netwr");
                var netPrice = quantity == 0 ? 0 : netValue / quantity;
                return new { key, netPrice };
            })
            .GroupBy(row => row.key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().netPrice, StringComparer.OrdinalIgnoreCase);

        var enriched = eketRows
            .Select(row =>
            {
                var ebeln = GetText(row, "Ebeln");
                var ebelp = GetText(row, "Ebelp");
                var key = $"{ebeln}|{ebelp}";
                netPriceByPosition.TryGetValue(key, out var netPrice);
                var quantity = GetDecimal(row, "Menge");
                var received = GetDecimal(row, "Wemng");
                var openQuantity = Math.Max(0, quantity - received);
                return new
                {
                    Ebeln = ebeln,
                    DueDate = TryParseSapDate(GetText(row, "Eindt")),
                    OpenQuantity = openQuantity,
                    OpenValue = openQuantity * netPrice
                };
            })
            .ToList();

        state.OpenQuantitySample = enriched.Sum(row => row.OpenQuantity);
        state.OpenValueSample = enriched.Sum(row => row.OpenValue);
        state.ContractValueSample = state.OpenValueSample;
        state.OpenValueChartRows = enriched
            .GroupBy(row => row.DueDate?.ToString("yyyy-MM") ?? "ohne Termin")
            .Select(group => new PurchasingLiveChartPoint(group.Key, group.Sum(row => row.OpenValue)))
            .OrderBy(row => row.Label)
            .Take(6)
            .ToList();
        state.ContractChartRows = state.OpenValueChartRows.ToList();
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

    private static decimal GetDecimal(Dictionary<string, object?> row, string key)
    {
        var text = GetText(row, key);
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            || decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
            ? value
            : 0m;
    }

    private static string FirstNonEmpty(params string[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

    private static string BuildTopLabel<T>(IEnumerable<IGrouping<string, T>> groups, Func<T, decimal> selector, string fallback)
    {
        var top = groups
            .Select(group => new { Label = group.Key, Value = group.Sum(selector) })
            .OrderByDescending(row => row.Value)
            .FirstOrDefault();
        return top is null ? fallback : $"{top.Label}: CHF {top.Value:N0}";
    }

    private static DateTime? TryParseSapDate(string value)
    {
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
            return parsed;

        return DateTime.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed)
            ? parsed
            : null;
    }
}
