using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
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

    private static PurchasingDashboardFilter BuildDefaultFilter()
    {
        var today = DateTime.Today;
        return new PurchasingDashboardFilter(new DateTime(today.Year - 2, 1, 1), today);
    }

    public async Task<PurchasingDashboardLiveState> LoadAsync(PurchasingDashboardFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var state = new PurchasingDashboardLiveState();
        filter ??= BuildDefaultFilter();
        state.PeriodFrom = filter.FromDate;
        state.PeriodTo = filter.ToDate;

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            if (await TryLoadCacheStateAsync(db, state, filter, cancellationToken))
                return state;

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

    private static async Task<bool> TryLoadCacheStateAsync(AppDbContext db, PurchasingDashboardLiveState state, PurchasingDashboardFilter filter, CancellationToken cancellationToken)
    {
        var conn = (SqliteConnection)db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync(cancellationToken);

        var from = filter.FromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var to = filter.ToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var ekkoPeriod = $"Bedat >= '{from}' AND Bedat <= '{to}'";
        var joinedEkkoPeriod = $"k.Bedat >= '{from}' AND k.Bedat <= '{to}'";
        var eketPeriod = $"e.Eindt >= '{from}' AND e.Eindt <= '{to}'";

        var cacheEkkoRows = await ExecuteScalarIntAsync(conn, "SELECT COUNT(1) FROM PurchasingEkkoCache;", cancellationToken);
        var cacheEkpoRows = await ExecuteScalarIntAsync(conn, "SELECT COUNT(1) FROM PurchasingEkpoCache;", cancellationToken);
        var cacheEketRows = await ExecuteScalarIntAsync(conn, "SELECT COUNT(1) FROM PurchasingEketCache;", cancellationToken);
        if (cacheEkkoRows <= 0 || cacheEkpoRows <= 0 || cacheEketRows <= 0)
            return false;

        var latestStatus = await ReadCacheStatusAsync(conn, cancellationToken);
        state.UsesCache = true;
        state.SapReachable = true;
        state.EkkoLoaded = true;
        state.EkpoLoaded = true;
        state.EketLoaded = true;
        state.PurchaseOrderCount = await ExecuteScalarIntAsync(conn, $"SELECT COUNT(1) FROM PurchasingEkkoCache WHERE {ekkoPeriod};", cancellationToken);
        state.PositionSampleCount = await ExecuteScalarIntAsync(conn, $@"
SELECT COUNT(1)
FROM PurchasingEkpoCache p
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
WHERE {joinedEkkoPeriod};", cancellationToken);
        state.ScheduleSampleCount = await ExecuteScalarIntAsync(conn, $"SELECT COUNT(1) FROM PurchasingEketCache e WHERE {eketPeriod};", cancellationToken);
        state.SupplierCount = await ExecuteScalarIntAsync(conn, $"SELECT COUNT(DISTINCT Lifnr) FROM PurchasingEkkoCache WHERE Lifnr <> '' AND {ekkoPeriod};", cancellationToken);
        state.LatestOrderDate = await ExecuteScalarDateAsync(conn, $"SELECT MAX(Bedat) FROM PurchasingEkkoCache WHERE {ekkoPeriod};", cancellationToken);
        state.SpendChfSample = await ExecuteScalarDecimalAsync(conn, $@"
SELECT COALESCE(SUM(CAST(p.Netwr AS REAL)), 0)
FROM PurchasingEkpoCache p
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
WHERE COALESCE(p.Loekz, '') = '' AND {joinedEkkoPeriod};", cancellationToken);
        state.OpenQuantitySample = await ExecuteScalarDecimalAsync(conn, $"SELECT COALESCE(SUM(MAX(CAST(e.Menge AS REAL) - CAST(e.Wemng AS REAL), 0)), 0) FROM PurchasingEketCache e WHERE {eketPeriod};", cancellationToken);
        state.OpenValueSample = await ExecuteScalarDecimalAsync(conn, @"
SELECT COALESCE(SUM(MAX(CAST(e.Menge AS REAL) - CAST(e.Wemng AS REAL), 0) *
    CASE WHEN CAST(p.Menge AS REAL) = 0 THEN 0 ELSE CAST(p.Netwr AS REAL) / CAST(p.Menge AS REAL) END), 0)
FROM PurchasingEketCache e
LEFT JOIN PurchasingEkpoCache p ON p.Ebeln = e.Ebeln AND p.Ebelp = e.Ebelp
WHERE COALESCE(p.Loekz, '') = '' AND " + eketPeriod + ";", cancellationToken);
        state.ContractValueSample = state.OpenValueSample;
        state.TopSupplierLabel = await ExecuteTopLabelAsync(conn, @"
SELECT COALESCE(k.Lifnr, 'ohne Lieferant') AS Label, SUM(CAST(p.Netwr AS REAL)) AS Value
FROM PurchasingEkpoCache p
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
WHERE COALESCE(p.Loekz, '') = '' AND " + joinedEkkoPeriod + @"
GROUP BY COALESCE(k.Lifnr, 'ohne Lieferant')
ORDER BY Value DESC
LIMIT 1;", "Lieferant", cancellationToken);
        state.TopMaterialGroupLabel = await ExecuteTopLabelAsync(conn, @"
SELECT COALESCE(NULLIF(Matkl, ''), 'ohne Warengruppe') AS Label, SUM(CAST(Netwr AS REAL)) AS Value
FROM PurchasingEkpoCache p
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
WHERE COALESCE(p.Loekz, '') = '' AND " + joinedEkkoPeriod + @"
GROUP BY COALESCE(NULLIF(Matkl, ''), 'ohne Warengruppe')
ORDER BY Value DESC
LIMIT 1;", "Warengruppe", cancellationToken);
        state.TopArticleLabel = await ExecuteTopLabelAsync(conn, @"
SELECT COALESCE(NULLIF(Matnr, ''), NULLIF(Txz01, ''), 'ohne Artikel') AS Label, SUM(CAST(Netwr AS REAL)) AS Value
FROM PurchasingEkpoCache p
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
WHERE COALESCE(p.Loekz, '') = '' AND " + joinedEkkoPeriod + @"
GROUP BY COALESCE(NULLIF(Matnr, ''), NULLIF(Txz01, ''), 'ohne Artikel')
ORDER BY Value DESC
LIMIT 1;", "Artikel", cancellationToken);
        state.SpendChartRows = await ExecuteChartRowsAsync(conn, @"
SELECT 'Lieferant ' || COALESCE(NULLIF(k.Lifnr, ''), 'ohne Lieferant') AS Label, SUM(CAST(p.Netwr AS REAL)) AS Value
FROM PurchasingEkpoCache p
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
WHERE COALESCE(p.Loekz, '') = '' AND " + joinedEkkoPeriod + @"
GROUP BY COALESCE(NULLIF(k.Lifnr, ''), 'ohne Lieferant')
ORDER BY Value DESC
LIMIT 6;", cancellationToken);
        state.OpenValueChartRows = await ExecuteChartRowsAsync(conn, @"
SELECT COALESCE(substr(e.Eindt, 1, 7), 'ohne Termin') AS Label,
       SUM(MAX(CAST(e.Menge AS REAL) - CAST(e.Wemng AS REAL), 0) *
           CASE WHEN CAST(p.Menge AS REAL) = 0 THEN 0 ELSE CAST(p.Netwr AS REAL) / CAST(p.Menge AS REAL) END) AS Value
FROM PurchasingEketCache e
LEFT JOIN PurchasingEkpoCache p ON p.Ebeln = e.Ebeln AND p.Ebelp = e.Ebelp
WHERE COALESCE(p.Loekz, '') = '' AND " + eketPeriod + @"
GROUP BY COALESCE(substr(e.Eindt, 1, 7), 'ohne Termin')
ORDER BY Label
LIMIT 6;", cancellationToken);
        state.ContractChartRows = state.OpenValueChartRows.ToList();
        await ApplyIdeaAnalyticsAsync(conn, state, joinedEkkoPeriod, eketPeriod, cancellationToken);
        state.CacheStatus = latestStatus.Status;
        state.CacheCompletedAtUtc = latestStatus.CompletedAtUtc;
        state.Message = $"Einkauf Cache geladen fuer {filter.Label}: EKKO={state.PurchaseOrderCount:N0}, EKPO={state.PositionSampleCount:N0}, EKET={state.ScheduleSampleCount:N0}. {latestStatus.Message}";
        return true;
    }

    private static async Task ApplyIdeaAnalyticsAsync(SqliteConnection conn, PurchasingDashboardLiveState state, string joinedEkkoPeriod, string eketPeriod, CancellationToken cancellationToken)
    {
        state.DeliveryRiskChartRows = await ExecuteChartRowsAsync(conn, @"
WITH open_rows AS (
    SELECT
        CASE
            WHEN date(e.Eindt) < date('now', 'localtime') THEN 'Ueberfaellig'
            WHEN date(e.Eindt) <= date('now', 'localtime', '+7 day') THEN '0-7 Tage'
            WHEN date(e.Eindt) <= date('now', 'localtime', '+30 day') THEN '8-30 Tage'
            ELSE 'Spaeter'
        END AS Label,
        MAX(CAST(e.Menge AS REAL) - CAST(e.Wemng AS REAL), 0) *
            CASE WHEN CAST(p.Menge AS REAL) = 0 THEN 0 ELSE CAST(p.Netwr AS REAL) / CAST(p.Menge AS REAL) END AS OpenValue
    FROM PurchasingEketCache e
    LEFT JOIN PurchasingEkpoCache p ON p.Ebeln = e.Ebeln AND p.Ebelp = e.Ebelp
    WHERE COALESCE(p.Loekz, '') = '' AND " + eketPeriod + @" AND MAX(CAST(e.Menge AS REAL) - CAST(e.Wemng AS REAL), 0) > 0
)
SELECT Label, SUM(OpenValue) AS Value
FROM open_rows
GROUP BY Label
ORDER BY CASE Label WHEN 'Ueberfaellig' THEN 1 WHEN '0-7 Tage' THEN 2 WHEN '8-30 Tage' THEN 3 ELSE 4 END;", cancellationToken);
        state.DeliveryRiskRows = await ExecuteAnalysisRowsAsync(conn, @"
SELECT
    COALESCE(NULLIF(k.Lifnr, ''), 'ohne Lieferant') || ' / ' || COALESCE(NULLIF(p.Matnr, ''), NULLIF(p.Txz01, ''), 'ohne Artikel') AS Label,
    'CHF ' || printf('%,.0f', SUM(MAX(CAST(e.Menge AS REAL) - CAST(e.Wemng AS REAL), 0) *
        CASE WHEN CAST(p.Menge AS REAL) = 0 THEN 0 ELSE CAST(p.Netwr AS REAL) / CAST(p.Menge AS REAL) END)) AS Value,
    'Faellig ' || COALESCE(MIN(e.Eindt), 'ohne Termin') AS Detail,
    CASE WHEN MIN(date(e.Eindt)) < date('now', 'localtime') THEN 'High' ELSE 'Medium' END AS Severity
FROM PurchasingEketCache e
LEFT JOIN PurchasingEkpoCache p ON p.Ebeln = e.Ebeln AND p.Ebelp = e.Ebelp
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = e.Ebeln
WHERE COALESCE(p.Loekz, '') = '' AND " + eketPeriod + @" AND MAX(CAST(e.Menge AS REAL) - CAST(e.Wemng AS REAL), 0) > 0
GROUP BY COALESCE(NULLIF(k.Lifnr, ''), 'ohne Lieferant'), COALESCE(NULLIF(p.Matnr, ''), NULLIF(p.Txz01, ''), 'ohne Artikel')
ORDER BY SUM(MAX(CAST(e.Menge AS REAL) - CAST(e.Wemng AS REAL), 0) *
    CASE WHEN CAST(p.Menge AS REAL) = 0 THEN 0 ELSE CAST(p.Netwr AS REAL) / CAST(p.Menge AS REAL) END) DESC
LIMIT 10;", cancellationToken);

        state.PriceVarianceRows = await ExecuteAnalysisRowsAsync(conn, @"
WITH priced AS (
    SELECT
        COALESCE(NULLIF(k.Lifnr, ''), 'ohne Lieferant') AS Supplier,
        COALESCE(NULLIF(p.Matnr, ''), NULLIF(p.Txz01, ''), 'ohne Artikel') AS Article,
        substr(k.Bedat, 1, 4) AS Year,
        SUM(CAST(p.Netwr AS REAL)) AS Value,
        SUM(CAST(p.Menge AS REAL)) AS Quantity
    FROM PurchasingEkpoCache p
    LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
    WHERE COALESCE(p.Loekz, '') = '' AND CAST(p.Menge AS REAL) > 0 AND k.Bedat IS NOT NULL AND k.Bedat <> '' AND " + joinedEkkoPeriod + @"
    GROUP BY Supplier, Article, Year
),
current_year AS (
    SELECT Supplier, Article, Value, Quantity, Value / Quantity AS UnitPrice
    FROM priced
    WHERE Year = '2026'
),
previous_year AS (
    SELECT Supplier, Article, Value / Quantity AS UnitPrice
    FROM priced
    WHERE Year = '2025' AND Quantity > 0
)
SELECT
    c.Supplier || ' / ' || c.Article AS Label,
    printf('%.1f%%', ((c.UnitPrice - p.UnitPrice) / p.UnitPrice) * 100.0) AS Value,
    'Wirkung CHF ' || printf('%,.0f', (c.UnitPrice - p.UnitPrice) * c.Quantity) AS Detail,
    CASE WHEN ((c.UnitPrice - p.UnitPrice) / p.UnitPrice) >= 0.10 THEN 'High'
         WHEN ((c.UnitPrice - p.UnitPrice) / p.UnitPrice) >= 0.03 THEN 'Medium'
         ELSE 'Low' END AS Severity
FROM current_year c
JOIN previous_year p ON p.Supplier = c.Supplier AND p.Article = c.Article
WHERE p.UnitPrice > 0 AND c.UnitPrice > p.UnitPrice
ORDER BY (c.UnitPrice - p.UnitPrice) * c.Quantity DESC
LIMIT 10;", cancellationToken);
        state.PriceVarianceChartRows = await ExecuteChartRowsAsync(conn, @"
WITH priced AS (
    SELECT
        COALESCE(NULLIF(k.Lifnr, ''), 'ohne Lieferant') AS Supplier,
        COALESCE(NULLIF(p.Matnr, ''), NULLIF(p.Txz01, ''), 'ohne Artikel') AS Article,
        substr(k.Bedat, 1, 4) AS Year,
        SUM(CAST(p.Netwr AS REAL)) AS Value,
        SUM(CAST(p.Menge AS REAL)) AS Quantity
    FROM PurchasingEkpoCache p
    LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
    WHERE COALESCE(p.Loekz, '') = '' AND CAST(p.Menge AS REAL) > 0 AND k.Bedat IS NOT NULL AND k.Bedat <> '' AND " + joinedEkkoPeriod + @"
    GROUP BY Supplier, Article, Year
),
delta AS (
    SELECT c.Supplier, (c.Value / c.Quantity - p.Value / p.Quantity) * c.Quantity AS Impact
    FROM priced c
    JOIN priced p ON p.Supplier = c.Supplier AND p.Article = c.Article
    WHERE c.Year = '2026' AND p.Year = '2025' AND p.Quantity > 0 AND c.Quantity > 0 AND c.Value / c.Quantity > p.Value / p.Quantity
)
SELECT Supplier, SUM(Impact) AS Value
FROM delta
GROUP BY Supplier
ORDER BY Value DESC
LIMIT 6;", cancellationToken);

        state.SpendConcentrationChartRows = await ExecuteChartRowsAsync(conn, @"
SELECT 'Lieferant ' || COALESCE(NULLIF(k.Lifnr, ''), 'ohne Lieferant') AS Label, SUM(CAST(p.Netwr AS REAL)) AS Value
FROM PurchasingEkpoCache p
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
WHERE COALESCE(p.Loekz, '') = '' AND " + joinedEkkoPeriod + @"
GROUP BY COALESCE(NULLIF(k.Lifnr, ''), 'ohne Lieferant')
ORDER BY Value DESC
LIMIT 10;", cancellationToken);
        var totalSpend = state.SpendChfSample <= 0 ? 1 : state.SpendChfSample;
        var concentrationRows = await ExecuteAnalysisRowsAsync(conn, @"
SELECT
    COALESCE(NULLIF(k.Lifnr, ''), 'ohne Lieferant') AS Label,
    'CHF ' || printf('%,.0f', SUM(CAST(p.Netwr AS REAL))) AS Value,
    COUNT(DISTINCT COALESCE(NULLIF(p.Matkl, ''), 'ohne Warengruppe')) || ' Warengruppen' AS Detail,
    CASE WHEN SUM(CAST(p.Netwr AS REAL)) > 1000000 THEN 'High'
         WHEN SUM(CAST(p.Netwr AS REAL)) > 250000 THEN 'Medium'
         ELSE 'Low' END AS Severity
FROM PurchasingEkpoCache p
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
WHERE COALESCE(p.Loekz, '') = '' AND " + joinedEkkoPeriod + @"
GROUP BY COALESCE(NULLIF(k.Lifnr, ''), 'ohne Lieferant')
ORDER BY SUM(CAST(p.Netwr AS REAL)) DESC
LIMIT 10;", cancellationToken);
        state.SpendConcentrationRows = concentrationRows
            .Select((row, index) => row with { Detail = $"{row.Detail} | Rang {index + 1} | Anteil {CalculateSupplierShare(state.SpendConcentrationChartRows, row.Label, totalSpend):N1}%" })
            .ToList();

        state.DataQualityChartRows = await ExecuteChartRowsAsync(conn, @"
SELECT 'fehlender Lieferant' AS Label, COUNT(*) AS Value FROM PurchasingEkkoCache WHERE COALESCE(NULLIF(Lifnr, ''), '') = ''
UNION ALL
SELECT 'fehlende Warengruppe', COUNT(*) FROM PurchasingEkpoCache WHERE COALESCE(NULLIF(Matkl, ''), '') = ''
UNION ALL
SELECT 'fehlender Artikel/Text', COUNT(*) FROM PurchasingEkpoCache WHERE COALESCE(NULLIF(Matnr, ''), NULLIF(Txz01, ''), '') = ''
UNION ALL
SELECT 'Nullmenge', COUNT(*) FROM PurchasingEkpoCache WHERE CAST(Menge AS REAL) = 0
UNION ALL
SELECT 'Nullwert', COUNT(*) FROM PurchasingEkpoCache WHERE CAST(Netwr AS REAL) = 0;", cancellationToken);
        state.DataQualityRows = await ExecuteAnalysisRowsAsync(conn, @"
SELECT 'Fehlender Lieferant' AS Label, COUNT(*) || ' Belege' AS Value, 'EKKO.Lifnr leer' AS Detail, CASE WHEN COUNT(*) > 0 THEN 'High' ELSE 'Low' END AS Severity FROM PurchasingEkkoCache WHERE COALESCE(NULLIF(Lifnr, ''), '') = ''
UNION ALL
SELECT 'Fehlende Warengruppe', COUNT(*) || ' Positionen', 'EKPO.Matkl leer', CASE WHEN COUNT(*) > 0 THEN 'Medium' ELSE 'Low' END FROM PurchasingEkpoCache WHERE COALESCE(NULLIF(Matkl, ''), '') = ''
UNION ALL
SELECT 'Fehlender Artikel/Text', COUNT(*) || ' Positionen', 'EKPO.Matnr und Txz01 leer', CASE WHEN COUNT(*) > 0 THEN 'High' ELSE 'Low' END FROM PurchasingEkpoCache WHERE COALESCE(NULLIF(Matnr, ''), NULLIF(Txz01, ''), '') = ''
UNION ALL
SELECT 'Nullmenge', COUNT(*) || ' Positionen', 'EKPO.Menge = 0', CASE WHEN COUNT(*) > 0 THEN 'Medium' ELSE 'Low' END FROM PurchasingEkpoCache WHERE CAST(Menge AS REAL) = 0
UNION ALL
SELECT 'Nullwert', COUNT(*) || ' Positionen', 'EKPO.Netwr = 0', CASE WHEN COUNT(*) > 0 THEN 'Medium' ELSE 'Low' END FROM PurchasingEkpoCache WHERE CAST(Netwr AS REAL) = 0;", cancellationToken);
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

    private static async Task<int> ExecuteScalarIntAsync(SqliteConnection conn, string sql, CancellationToken cancellationToken)
    {
        await using var command = conn.CreateCommand();
        command.CommandText = sql;
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(value ?? 0, CultureInfo.InvariantCulture);
    }

    private static async Task<decimal> ExecuteScalarDecimalAsync(SqliteConnection conn, string sql, CancellationToken cancellationToken)
    {
        await using var command = conn.CreateCommand();
        command.CommandText = sql;
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToDecimal(value ?? 0, CultureInfo.InvariantCulture);
    }

    private static async Task<DateTime?> ExecuteScalarDateAsync(SqliteConnection conn, string sql, CancellationToken cancellationToken)
    {
        await using var command = conn.CreateCommand();
        command.CommandText = sql;
        var value = Convert.ToString(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
        return string.IsNullOrWhiteSpace(value) ? null : TryParseSapDate(value);
    }

    private static async Task<string> ExecuteTopLabelAsync(SqliteConnection conn, string sql, string fallback, CancellationToken cancellationToken)
    {
        await using var command = conn.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return fallback;

        var label = reader.GetString(0);
        var value = Convert.ToDecimal(reader.GetValue(1), CultureInfo.InvariantCulture);
        return $"{label}: CHF {value:N0}";
    }

    private static async Task<List<PurchasingLiveChartPoint>> ExecuteChartRowsAsync(SqliteConnection conn, string sql, CancellationToken cancellationToken)
    {
        var rows = new List<PurchasingLiveChartPoint>();
        await using var command = conn.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var label = reader.GetString(0);
            var value = Convert.ToDecimal(reader.GetValue(1), CultureInfo.InvariantCulture);
            rows.Add(new PurchasingLiveChartPoint(label, value));
        }

        return rows;
    }

    private static async Task<List<PurchasingIdeaAnalysisRow>> ExecuteAnalysisRowsAsync(SqliteConnection conn, string sql, CancellationToken cancellationToken)
    {
        var rows = new List<PurchasingIdeaAnalysisRow>();
        await using var command = conn.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new PurchasingIdeaAnalysisRow(
                reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                reader.IsDBNull(3) ? string.Empty : reader.GetString(3)));
        }

        return rows;
    }

    private static decimal CalculateSupplierShare(IReadOnlyList<PurchasingLiveChartPoint> rows, string supplier, decimal totalSpend)
    {
        var value = rows
            .Where(row => row.Label.Equals(supplier, StringComparison.OrdinalIgnoreCase) || row.Label.Equals($"Lieferant {supplier}", StringComparison.OrdinalIgnoreCase))
            .Sum(row => row.Value);
        return totalSpend <= 0 ? 0 : value / totalSpend * 100m;
    }

    private static async Task<(string Status, DateTime? CompletedAtUtc, string Message)> ReadCacheStatusAsync(SqliteConnection conn, CancellationToken cancellationToken)
    {
        await using var command = conn.CreateCommand();
        command.CommandText = "SELECT Status, CompletedAtUtc, Message FROM PurchasingSyncState ORDER BY Id DESC LIMIT 1;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return ("Cache", null, string.Empty);

        var completedText = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
        var completed = DateTime.TryParse(completedText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed
            : (DateTime?)null;
        return (reader.GetString(0), completed, reader.GetString(2));
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
