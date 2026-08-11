using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

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
        return new PurchasingDashboardFilter(new DateTime(Math.Max(2020, today.Year - 6), 1, 1), today);
    }

    private static string SupplierLabelSql(string lifnrExpression, string supplierNameExpression = "''")
        => $@"CASE
            WHEN COALESCE(NULLIF({supplierNameExpression}, ''), '') <> '' THEN {supplierNameExpression}
            WHEN COALESCE(NULLIF({lifnrExpression}, ''), '') = '' THEN 'ohne Lieferant'
            ELSE {lifnrExpression}
        END";

    // MARA-MSTAE-Werte, die ein Material als zur Loeschung vorgemerkt / gesperrt kennzeichnen.
    private static readonly string[] DeletedMaterialStatusCodes = ["98", "99"];

    // Jahresachse fuer Spend/Matrix: fixe Untergrenze (fachliche Vorgabe), dynamische Obergrenze,
    // damit die Sicht beim Jahreswechsel nicht still das aktuelle Jahr verliert.
    private const int MinSpendYear = 2020;

    private static int MaxSpendYear(PurchasingDashboardFilter filter)
        => Math.Max(DateTime.Today.Year, filter.ToDate.Year);

    // Bewertet einen Netto-Belegwert (EKPO.Netwr in Belegwaehrung) nach CHF.
    // - Belegwaehrung CHF oder leer: unveraendert.
    // - Fremdwaehrung mit positivem EKKO.Wkurs: multiplizieren (direkte Notierung).
    // - Fremdwaehrung mit negativem Wkurs: dividieren (SAP-Konvention indirekte Notierung).
    // WICHTIG: Die WKURS-Richtung ist gegen echte SAP-Daten zu verifizieren. Solange praktisch
    // alle Belege CHF sind, wirkt ausschliesslich der CHF-Zweig und die Werte bleiben unveraendert.
    private static string ChfValueSql(string netwrExpr, string waersExpr, string wkursExpr)
        => $@"(CASE
            WHEN COALESCE({waersExpr}, '') IN ('', 'CHF') THEN CAST({netwrExpr} AS REAL)
            WHEN CAST({wkursExpr} AS REAL) > 0 THEN CAST({netwrExpr} AS REAL) * CAST({wkursExpr} AS REAL)
            WHEN CAST({wkursExpr} AS REAL) < 0 THEN CAST({netwrExpr} AS REAL) / (-CAST({wkursExpr} AS REAL))
            ELSE CAST({netwrExpr} AS REAL)
        END)";

    // Netto-Stueckwert in CHF: CHF-bewerteter Positionswert / Bestellmenge (0 bei Nullmenge).
    private static string ChfUnitPriceSql(string waersExpr = "k.Waers", string wkursExpr = "k.Wkurs")
        => $@"(CASE WHEN CAST(p.Menge AS REAL) = 0 THEN 0
            ELSE {ChfValueSql("p.Netwr", waersExpr, wkursExpr)} / CAST(p.Menge AS REAL) END)";

    // Vorexpandierte CHF-Ausdruecke fuer die (identischen) Spend-/Stueckwert-Stellen. Setzt voraus,
    // dass die jeweilige Query PurchasingEkkoCache als k joint (fuer k.Waers/k.Wkurs).
    private static readonly string ChfNetValue = ChfValueSql("p.Netwr", "k.Waers", "k.Wkurs");
    private static readonly string ChfUnitPrice = ChfUnitPriceSql();

    private static string ActiveItemFilterSql(PurchasingDashboardFilter filter, string itemAlias)
    {
        // Offen-Sichten: EKPO.Loekz gesetzt ODER Material-Status MARA-MSTAE in (98, 99).
        // Mstae wird beim Full Load/Delta aus MARA001Set ueber EKPO.Matnr -> MARA.Matnr in
        // PurchasingEkpoCache.Mstae uebernommen. Fuer den kuenftigen Zulauf ist ein heute
        // auslaufendes/gesperrtes Material relevant und bleibt deshalb ausgeschlossen.
        if (!filter.ExcludeDeletedItems)
            return "1 = 1";

        var statusList = string.Join(", ", DeletedMaterialStatusCodes.Select(code => $"'{code}'"));
        return $"COALESCE({itemAlias}.Loekz, '') = '' AND COALESCE({itemAlias}.Mstae, '') NOT IN ({statusList})";
    }

    // Spend-Sichten (Marco-Review 2026-07-10): Der heutige Materialstatus (MARA-MSTAE 98/99)
    // darf den historischen Spend nicht filtern — ein 2023 eingekaufter Artikel behaelt seinen
    // Spend-Anteil, auch wenn er heute Status 99 hat. Stornierte Positionen (EKPO.Loekz) bleiben
    // ausgeschlossen, weil sie nie beschafft wurden.
    private static string SpendItemFilterSql(PurchasingDashboardFilter filter, string itemAlias)
        => filter.ExcludeDeletedItems ? $"COALESCE({itemAlias}.Loekz, '') = ''" : "1 = 1";

    // Belegtyp-Filter (Marcos Trennung): nur echte Bestellungen (EKKO.Bstyp='F') ohne Umlagerung
    // (EKKO.Bsart='UB'); schliesst Anfragen (A) und Kontrakt-Belege (K) aus. Leerer Bstyp
    // (z.B. Bestandsdaten vor dem naechsten Full Load) wird bewusst eingeschlossen, damit die
    // Zahlen beim Modell-/Feld-Rollout nicht schlagartig auf 0 fallen. headerAlias = EKKO ('k').
    private static string OrderTypeFilterSql(PurchasingDashboardFilter filter, string headerAlias)
    {
        if (!filter.OrdersOnly)
            return "1 = 1";

        return $"(COALESCE({headerAlias}.Bstyp, '') = '' OR ({headerAlias}.Bstyp = 'F' AND COALESCE({headerAlias}.Bsart, '') <> 'UB'))";
    }


    public async Task<PurchasingDashboardLiveState> LoadAsync(PurchasingDashboardFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var state = new PurchasingDashboardLiveState();
        filter ??= BuildDefaultFilter();
        state.PeriodFrom = filter.FromDate;
        state.PeriodTo = filter.ToDate;
        state.SpendYears = Enumerable.Range(filter.FromDate.Year, filter.ToDate.Year - filter.FromDate.Year + 1)
            .Where(year => year >= MinSpendYear && year <= MaxSpendYear(filter))
            .ToList();

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
                ApplyEketMetrics(state, ekkoRows, ekpoRows, eketRows);
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
        // Offene Positionen sind eine Stand-heute-Sicht und bewusst ZEITRAUMUNABHAENGIG
        // (Marco-Review 2026-07-10): weder Von- noch Bis-Filter schneiden offene Einteilungen ab.
        // Keine Untergrenze, sonst verschwinden alte ueberfaellige Rueckstaende; keine Obergrenze,
        // sonst faellt der zukuenftige Zulauf heraus (K3).
        // Endgelieferte Positionen (EKPO.Elikz='X') zaehlen nicht als offen (M7); da alle
        // Offen-Queries EKPO als p joinen, ist der Ausschluss hier zentral eingehaengt.
        var endDelivered = filter.ExcludeEndDelivered ? " AND COALESCE(p.Elikz, '') <> 'X'" : string.Empty;
        var eketOpenPeriod = $"1 = 1{endDelivered}";
        // Ueberfaellig: offene Einteilung, deren Liefertermin bereits in der Vergangenheit liegt.
        var eketOverduePeriod = $"{eketOpenPeriod} AND date(e.Eindt) < date('now', 'localtime')";
        // Zwei Positionsfilter (beide inkl. Belegtyp-Trennung auf k):
        // - Spend/Historie: nur stornierte Positionen (Loekz) raus, heutiger Materialstatus egal.
        // - Offen/Zulauf: zusaetzlich MARA-MSTAE 98/99 raus.
        var spendItemFilter = $"({SpendItemFilterSql(filter, "p")}) AND ({OrderTypeFilterSql(filter, "k")})";
        var openItemFilter = $"({ActiveItemFilterSql(filter, "p")}) AND ({OrderTypeFilterSql(filter, "k")})";
        state.SpendYears = Enumerable.Range(filter.FromDate.Year, filter.ToDate.Year - filter.FromDate.Year + 1)
            .Where(year => year >= MinSpendYear && year <= MaxSpendYear(filter))
            .ToList();

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
        state.PurchaseOrderCount = await ExecuteScalarIntAsync(conn, $@"
SELECT COUNT(DISTINCT k.Ebeln)
FROM PurchasingEkkoCache k
JOIN PurchasingEkpoCache p ON p.Ebeln = k.Ebeln
WHERE {joinedEkkoPeriod} AND {spendItemFilter};", cancellationToken);
        state.PositionSampleCount = await ExecuteScalarIntAsync(conn, $@"
SELECT COUNT(1)
FROM PurchasingEkpoCache p
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
WHERE {joinedEkkoPeriod} AND {spendItemFilter};", cancellationToken);
        state.ScheduleSampleCount = await ExecuteScalarIntAsync(conn, $"SELECT COUNT(1) FROM PurchasingEketCache e WHERE {eketPeriod};", cancellationToken);
        state.SupplierCount = await ExecuteScalarIntAsync(conn, $@"
SELECT COUNT(DISTINCT k.Lifnr)
FROM PurchasingEkkoCache k
JOIN PurchasingEkpoCache p ON p.Ebeln = k.Ebeln
WHERE k.Lifnr <> '' AND {joinedEkkoPeriod} AND {spendItemFilter} AND CAST(p.Netwr AS REAL) <> 0;", cancellationToken);
        state.LatestOrderDate = await ExecuteScalarDateAsync(conn, $"SELECT MAX(Bedat) FROM PurchasingEkkoCache WHERE {ekkoPeriod};", cancellationToken);
        state.SpendChfSample = await ExecuteScalarDecimalAsync(conn, @"
SELECT COALESCE(SUM(" + ChfValueSql("p.Netwr", "k.Waers", "k.Wkurs") + @"), 0)
FROM PurchasingEkpoCache p
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
WHERE " + spendItemFilter + " AND " + joinedEkkoPeriod + ";", cancellationToken);
        // Offene Menge: gleiche Join-/Filterstruktur wie offener Wert, damit Menge und Wert
        // konsistent dieselben (aktiven, nicht geloeschten) Positionen abbilden.
        state.OpenQuantitySample = await ExecuteScalarDecimalAsync(conn, @"
SELECT COALESCE(SUM(MAX(CAST(e.Menge AS REAL) - CAST(e.Wemng AS REAL), 0)), 0)
FROM PurchasingEketCache e
LEFT JOIN PurchasingEkpoCache p ON p.Ebeln = e.Ebeln AND p.Ebelp = e.Ebelp
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = e.Ebeln
WHERE " + openItemFilter + " AND " + eketOpenPeriod + ";", cancellationToken);
        state.OpenValueSample = await ExecuteScalarDecimalAsync(conn, @"
SELECT COALESCE(SUM(MAX(CAST(e.Menge AS REAL) - CAST(e.Wemng AS REAL), 0) *
    " + ChfUnitPriceSql() + @"), 0)
FROM PurchasingEketCache e
LEFT JOIN PurchasingEkpoCache p ON p.Ebeln = e.Ebeln AND p.Ebelp = e.Ebelp
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = e.Ebeln
WHERE " + openItemFilter + " AND " + eketOpenPeriod + ";", cancellationToken);
        // Kontrakt-Restwert: offener Restwert nur fuer Abrufe zu Rahmenkontrakten (EKKO.Konnr gesetzt),
        // nicht mehr als blosse Kopie des offenen Bestellwerts. Ohne Konnr-Daten bleibt der Wert 0
        // und signalisiert fachlich korrekt, dass noch keine Kontrakte abgegrenzt sind.
        state.ContractValueSample = await ExecuteScalarDecimalAsync(conn, @"
SELECT COALESCE(SUM(MAX(CAST(e.Menge AS REAL) - CAST(e.Wemng AS REAL), 0) *
    " + ChfUnitPriceSql() + @"), 0)
FROM PurchasingEketCache e
LEFT JOIN PurchasingEkpoCache p ON p.Ebeln = e.Ebeln AND p.Ebelp = e.Ebelp
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = e.Ebeln
WHERE " + openItemFilter + " AND " + eketOpenPeriod + " AND COALESCE(k.Konnr, '') <> '';", cancellationToken);
        // Ueberfaelliger offener Wert/Menge und Anzahl ueberfaelliger Positionen. Gleiche Join-/
        // Filterstruktur wie der offene Wert, zusaetzlich Liefertermin in der Vergangenheit.
        state.OverdueValueSample = await ExecuteScalarDecimalAsync(conn, @"
SELECT COALESCE(SUM(MAX(CAST(e.Menge AS REAL) - CAST(e.Wemng AS REAL), 0) *
    " + ChfUnitPriceSql() + @"), 0)
FROM PurchasingEketCache e
LEFT JOIN PurchasingEkpoCache p ON p.Ebeln = e.Ebeln AND p.Ebelp = e.Ebelp
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = e.Ebeln
WHERE " + openItemFilter + " AND " + eketOverduePeriod + @"
  AND MAX(CAST(e.Menge AS REAL) - CAST(e.Wemng AS REAL), 0) > 0;", cancellationToken);
        state.OverdueQuantitySample = await ExecuteScalarDecimalAsync(conn, @"
SELECT COALESCE(SUM(MAX(CAST(e.Menge AS REAL) - CAST(e.Wemng AS REAL), 0)), 0)
FROM PurchasingEketCache e
LEFT JOIN PurchasingEkpoCache p ON p.Ebeln = e.Ebeln AND p.Ebelp = e.Ebelp
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = e.Ebeln
WHERE " + openItemFilter + " AND " + eketOverduePeriod + @"
  AND MAX(CAST(e.Menge AS REAL) - CAST(e.Wemng AS REAL), 0) > 0;", cancellationToken);
        state.OverduePositionCount = await ExecuteScalarIntAsync(conn, @"
SELECT COUNT(DISTINCT e.Ebeln || '|' || e.Ebelp)
FROM PurchasingEketCache e
LEFT JOIN PurchasingEkpoCache p ON p.Ebeln = e.Ebeln AND p.Ebelp = e.Ebelp
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = e.Ebeln
WHERE " + openItemFilter + " AND " + eketOverduePeriod + @"
  AND MAX(CAST(e.Menge AS REAL) - CAST(e.Wemng AS REAL), 0) > 0;", cancellationToken);
        state.OverduePositionRows = await ExecuteAnalysisRowsAsync(conn, @"
SELECT
    " + SupplierLabelSql("k.Lifnr", "k.SupplierName") + @" || ' / ' || COALESCE(NULLIF(p.Matnr, ''), NULLIF(p.Txz01, ''), 'ohne Artikel') AS Label,
    'CHF ' || printf('%,.0f', SUM(MAX(CAST(e.Menge AS REAL) - CAST(e.Wemng AS REAL), 0) *
        " + ChfUnitPrice + @")) AS Value,
    'Faellig seit ' || COALESCE(MIN(e.Eindt), 'ohne Termin') AS Detail,
    'High' AS Severity
FROM PurchasingEketCache e
LEFT JOIN PurchasingEkpoCache p ON p.Ebeln = e.Ebeln AND p.Ebelp = e.Ebelp
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = e.Ebeln
WHERE " + openItemFilter + " AND " + eketOverduePeriod + @"
  AND MAX(CAST(e.Menge AS REAL) - CAST(e.Wemng AS REAL), 0) > 0
GROUP BY " + SupplierLabelSql("k.Lifnr", "k.SupplierName") + @", COALESCE(NULLIF(p.Matnr, ''), NULLIF(p.Txz01, ''), 'ohne Artikel')
ORDER BY SUM(MAX(CAST(e.Menge AS REAL) - CAST(e.Wemng AS REAL), 0) *
    " + ChfUnitPrice + @") DESC
LIMIT 10;", cancellationToken);
        state.TopSupplierLabel = await ExecuteTopLabelAsync(conn, @"
SELECT " + SupplierLabelSql("k.Lifnr", "k.SupplierName") + @" AS Label, SUM(" + ChfNetValue + @") AS Value
FROM PurchasingEkpoCache p
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
WHERE " + spendItemFilter + " AND " + joinedEkkoPeriod + @"
GROUP BY Label
ORDER BY Value DESC
LIMIT 1;", "Lieferant", cancellationToken);
        state.TopMaterialGroupLabel = await ExecuteTopLabelAsync(conn, @"
SELECT COALESCE(NULLIF(Matkl, ''), 'ohne Warengruppe') AS Label, SUM(" + ChfNetValue + @") AS Value
FROM PurchasingEkpoCache p
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
WHERE " + spendItemFilter + " AND " + joinedEkkoPeriod + @"
GROUP BY COALESCE(NULLIF(Matkl, ''), 'ohne Warengruppe')
ORDER BY Value DESC
LIMIT 1;", "Warengruppe", cancellationToken, PurchasingMaterialGroupTextCatalog.Resolve);
        state.TopArticleLabel = await ExecuteTopLabelAsync(conn, @"
SELECT
    COALESCE(NULLIF(p.Matnr, ''), NULLIF(p.Txz01, ''), 'ohne Artikel') || ' | ' ||
    " + SupplierLabelSql("k.Lifnr", "k.SupplierName") + @" || ' | Monat ' ||
    COALESCE(substr(k.Bedat, 1, 7), 'ohne Datum') AS Label,
    SUM(" + ChfNetValue + @") AS Value
FROM PurchasingEkpoCache p
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
WHERE " + spendItemFilter + " AND " + joinedEkkoPeriod + @"
GROUP BY COALESCE(NULLIF(p.Matnr, ''), NULLIF(p.Txz01, ''), 'ohne Artikel'), " + SupplierLabelSql("k.Lifnr", "k.SupplierName") + @", COALESCE(substr(k.Bedat, 1, 7), 'ohne Datum')
ORDER BY Value DESC
LIMIT 1;", "Artikel", cancellationToken);
        state.SpendChartRows = await ExecuteChartRowsAsync(conn, @"
SELECT " + SupplierLabelSql("k.Lifnr", "k.SupplierName") + @" AS Label, SUM(" + ChfNetValue + @") AS Value
FROM PurchasingEkpoCache p
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
WHERE " + spendItemFilter + " AND " + joinedEkkoPeriod + @"
GROUP BY Label
ORDER BY Value DESC
LIMIT 6;", cancellationToken);
        // Volumen je Warengruppe (PowerBI "Diagramm Vol./WG"). Gleiche COALESCE-Logik und
        // gleicher spendItemFilter/Zeitraum wie die Lieferant-Spend-Matrix, damit die Summen
        // konsistent sind. Warengruppe kommt bewusst aus MaraMatkl (Materialstamm), Fallback
        // Beleg-Matkl. Label wird per PurchasingMaterialGroupTextCatalog (T023T-Text von Ingo,
        // 24.07.2026) auf "Code - Text" angereichert; unbekannte Codes bleiben roher Code.
        state.MaterialGroupSpendRows = (await ExecuteChartRowsAsync(conn, @"
SELECT COALESCE(NULLIF(p.MaraMatkl, ''), NULLIF(p.Matkl, ''), 'ohne Warengruppe') AS Label,
       SUM(" + ChfNetValue + @") AS Value
FROM PurchasingEkpoCache p
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
WHERE " + spendItemFilter + " AND " + joinedEkkoPeriod + @"
GROUP BY Label
ORDER BY Value DESC
LIMIT 12;", cancellationToken))
            .Select(row => row with { Label = PurchasingMaterialGroupTextCatalog.Resolve(row.Label) })
            .ToList();
        // Volumen je Beschaffungsregion (Lieferantenland LFA1.Land1 -> EKKO.SupplierCountry).
        // PowerBI "Eink.Vol. CHF / Region". Gleicher Filter/Zeitraum wie oben.
        state.RegionSpendRows = await ExecuteChartRowsAsync(conn, @"
SELECT COALESCE(NULLIF(k.SupplierCountry, ''), 'ohne Land') AS Label,
       SUM(" + ChfNetValue + @") AS Value
FROM PurchasingEkpoCache p
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
WHERE " + spendItemFilter + " AND " + joinedEkkoPeriod + @"
GROUP BY Label
ORDER BY Value DESC
LIMIT 12;", cancellationToken);
        // Volumen je Belegwaehrung (Marco-Wunsch 2026-07-30). Gleicher Filter/Zeitraum wie die
        // Bloecke oben, damit die Summen konsistent bleiben; zusaetzlich die Originalsumme in der
        // Belegwaehrung selbst. Kein SAP-Feld und kein Full Load noetig - Waers/Wkurs sind im
        // EKKO-Cache und werden ohnehin fuer ChfNetValue gebraucht.
        state.CurrencySpendRows = await ExecuteCurrencySpendRowsAsync(conn, joinedEkkoPeriod, spendItemFilter, cancellationToken);
        state.SupplierYearSpendRows = await ExecuteSupplierYearSpendRowsAsync(conn, filter, spendItemFilter, cancellationToken);
        // Reiter „Spend-Aufriss": mehrstufige Kaskade + Region-Kuchen + ABC/XYZ. Die Kaskade nutzt
        // vorhandene Cache-Daten (Beleg-WG/Matnr); Region/ABC/XYZ fuellen sich erst nach dem
        // naechsten Einkauf-Full-Load. Alle laufen nur beim Datenladen (OnInitialized/Filter),
        // nicht pro Render.
        state.SpendPerspectiveRows = await ExecuteSpendPerspectivesAsync(conn, filter, spendItemFilter, cancellationToken);
        var productGroupResult = await ExecuteProductGroupPerspectiveAsync(conn, filter, spendItemFilter, cancellationToken);
        // Produktgruppe ist eine eigene, summenerhaltend allokierte Perspektive und kann deshalb
        // nicht durch das einfache SQL-Grouping der vier direkten Einkaufsdimensionen laufen.
        state.SpendPerspectiveRows.Insert(
            Math.Min(3, state.SpendPerspectiveRows.Count),
            productGroupResult.Perspective);
        state.ProductGroupAllocation = productGroupResult.Summary;
        // Lieferanten-Perspektive bleibt der Standardeinstieg und damit die Quelle fuer die
        // bisherige, nicht umschaltbare Kaskadenanzeige.
        state.SpendCascadeRows = state.SpendPerspectiveRows
            .FirstOrDefault(perspective => perspective.Key == "supplier")?.Rows.ToList() ?? [];
        state.RegionByMaterialGroupRows = await ExecuteRegionByMaterialGroupRowsAsync(conn, joinedEkkoPeriod, spendItemFilter, cancellationToken);
        state.AbcSpendRows = await ExecuteChartRowsAsync(conn, @"
SELECT COALESCE(NULLIF(p.MaraAbc, ''), 'ohne ABC') AS Label, SUM(" + ChfNetValue + @") AS Value
FROM PurchasingEkpoCache p
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
WHERE " + spendItemFilter + " AND " + joinedEkkoPeriod + @"
GROUP BY Label
ORDER BY Value DESC
LIMIT 12;", cancellationToken);
        state.XyzSpendRows = await ExecuteChartRowsAsync(conn, @"
SELECT COALESCE(NULLIF(p.MaraXyz, ''), 'ohne XYZ') AS Label, SUM(" + ChfNetValue + @") AS Value
FROM PurchasingEkpoCache p
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
WHERE " + spendItemFilter + " AND " + joinedEkkoPeriod + @"
GROUP BY Label
ORDER BY Value DESC
LIMIT 12;", cancellationToken);
        state.AbcXyzActionRows = await ExecuteAbcXyzActionRowsAsync(
            conn,
            joinedEkkoPeriod,
            spendItemFilter,
            cancellationToken);
        state.CurrentYearSupplierSpendRows = await ExecuteChartRowsAsync(conn, @"
SELECT " + SupplierLabelSql("k.Lifnr", "k.SupplierName") + @" AS Label, SUM(" + ChfNetValue + @") AS Value
FROM PurchasingEkpoCache p
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
WHERE " + spendItemFilter + " AND k.Bedat >= '" + DateTime.Today.Year.ToString(CultureInfo.InvariantCulture) + @"-01-01' AND k.Bedat <= '" + DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + @"'
GROUP BY Label
ORDER BY Value DESC
LIMIT 10;", cancellationToken);
        state.OpenValueChartRows = await ExecuteChartRowsAsync(conn, @"
SELECT COALESCE(substr(e.Eindt, 1, 7), 'ohne Termin') AS Label,
       SUM(MAX(CAST(e.Menge AS REAL) - CAST(e.Wemng AS REAL), 0) *
           " + ChfUnitPrice + @") AS Value
FROM PurchasingEketCache e
LEFT JOIN PurchasingEkpoCache p ON p.Ebeln = e.Ebeln AND p.Ebelp = e.Ebelp
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = e.Ebeln
WHERE " + openItemFilter + " AND " + eketOpenPeriod + @"
GROUP BY COALESCE(substr(e.Eindt, 1, 7), 'ohne Termin')
ORDER BY Label
LIMIT 6;", cancellationToken);
        // Gleiche Grundmenge wie der Kontrakt-Restwert oben (nur Abrufe mit EKKO.Konnr). Ohne
        // diese Bedingung zaehlte die Kachel "Restwert" nur Kontraktabrufe, waehrend Diagramm
        // und "Top Verpflichtung" daneben alle offenen Bestellungen zeigten — die beiden Zahlen
        // im selben Reiter liessen sich dann nicht gegeneinander abstimmen.
        state.CommitmentDetailChartRows = await ExecuteChartRowsAsync(conn, @"
SELECT
    " + SupplierLabelSql("k.Lifnr", "k.SupplierName") + @" || ' | ' ||
    COALESCE(NULLIF(p.Matnr, ''), NULLIF(p.Txz01, ''), 'ohne Artikel') AS Label,
    SUM(MAX(CAST(e.Menge AS REAL) - CAST(e.Wemng AS REAL), 0) *
        " + ChfUnitPrice + @") AS Value
FROM PurchasingEketCache e
LEFT JOIN PurchasingEkpoCache p ON p.Ebeln = e.Ebeln AND p.Ebelp = e.Ebelp
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = e.Ebeln
WHERE " + openItemFilter + " AND " + eketOpenPeriod + @" AND MAX(CAST(e.Menge AS REAL) - CAST(e.Wemng AS REAL), 0) > 0
  AND COALESCE(k.Konnr, '') <> ''
GROUP BY " + SupplierLabelSql("k.Lifnr", "k.SupplierName") + @", COALESCE(NULLIF(p.Matnr, ''), NULLIF(p.Txz01, ''), 'ohne Artikel')
ORDER BY Value DESC
LIMIT 6;", cancellationToken);
        // Bewusst KEIN Rueckfall auf OpenValueChartRows: gibt es keine Kontraktabrufe, ist das
        // leere Diagramm die richtige Aussage und passt zum Restwert 0. Der fruehere Rueckfall
        // zeigte in genau diesem Fall alle offenen Bestellungen unter der Ueberschrift Kontrakte.
        state.ContractChartRows = state.CommitmentDetailChartRows.ToList();
        state.TopCommitmentLabel = state.CommitmentDetailChartRows.Count > 0
            ? $"{state.CommitmentDetailChartRows[0].Label}: CHF {state.CommitmentDetailChartRows[0].Value:N0}"
            : string.Empty;
        await ApplyIdeaAnalyticsAsync(conn, state, joinedEkkoPeriod, eketOpenPeriod, spendItemFilter, openItemFilter, cancellationToken);
        state.CacheStatus = latestStatus.Status;
        state.CacheCompletedAtUtc = latestStatus.CompletedAtUtc;
        state.Message = $"Einkauf Cache geladen fuer {filter.Label}: EKKO={state.PurchaseOrderCount:N0}, EKPO={state.PositionSampleCount:N0}, EKET={state.ScheduleSampleCount:N0}. {latestStatus.Message}";
        return true;
    }

    private static async Task ApplyIdeaAnalyticsAsync(SqliteConnection conn, PurchasingDashboardLiveState state, string joinedEkkoPeriod, string eketOpenPeriod, string spendItemFilter, string openItemFilter, CancellationToken cancellationToken)
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
            " + ChfUnitPrice + @" AS OpenValue
    FROM PurchasingEketCache e
    LEFT JOIN PurchasingEkpoCache p ON p.Ebeln = e.Ebeln AND p.Ebelp = e.Ebelp
    LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = e.Ebeln
    WHERE " + openItemFilter + " AND " + eketOpenPeriod + @" AND MAX(CAST(e.Menge AS REAL) - CAST(e.Wemng AS REAL), 0) > 0
)
SELECT Label, SUM(OpenValue) AS Value
FROM open_rows
GROUP BY Label
ORDER BY CASE Label WHEN 'Ueberfaellig' THEN 1 WHEN '0-7 Tage' THEN 2 WHEN '8-30 Tage' THEN 3 ELSE 4 END;", cancellationToken);
        state.DeliveryRiskRows = await ExecuteAnalysisRowsAsync(conn, @"
SELECT
    " + SupplierLabelSql("k.Lifnr", "k.SupplierName") + @" || ' / ' || COALESCE(NULLIF(p.Matnr, ''), NULLIF(p.Txz01, ''), 'ohne Artikel') AS Label,
    'CHF ' || printf('%,.0f', SUM(MAX(CAST(e.Menge AS REAL) - CAST(e.Wemng AS REAL), 0) *
        " + ChfUnitPrice + @")) AS Value,
    'Faellig ' || COALESCE(MIN(e.Eindt), 'ohne Termin') AS Detail,
    CASE WHEN MIN(date(e.Eindt)) < date('now', 'localtime') THEN 'High' ELSE 'Medium' END AS Severity
FROM PurchasingEketCache e
LEFT JOIN PurchasingEkpoCache p ON p.Ebeln = e.Ebeln AND p.Ebelp = e.Ebelp
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = e.Ebeln
WHERE " + openItemFilter + " AND " + eketOpenPeriod + @" AND MAX(CAST(e.Menge AS REAL) - CAST(e.Wemng AS REAL), 0) > 0
GROUP BY " + SupplierLabelSql("k.Lifnr", "k.SupplierName") + @", COALESCE(NULLIF(p.Matnr, ''), NULLIF(p.Txz01, ''), 'ohne Artikel')
ORDER BY SUM(MAX(CAST(e.Menge AS REAL) - CAST(e.Wemng AS REAL), 0) *
    " + ChfUnitPrice + @") DESC
LIMIT 10;", cancellationToken);

        state.PriceVarianceRows = await ExecuteAnalysisRowsAsync(conn, @"
WITH priced AS (
    SELECT
        " + SupplierLabelSql("k.Lifnr", "k.SupplierName") + @" AS Supplier,
        COALESCE(NULLIF(p.Matnr, ''), NULLIF(p.Txz01, ''), 'ohne Artikel') AS Article,
        substr(k.Bedat, 1, 4) AS Year,
        MIN(CASE WHEN CAST(p.Menge AS REAL) = 0 THEN NULL ELSE " + ChfNetValue + @" / CAST(p.Menge AS REAL) END) AS UnitPrice
    FROM PurchasingEkpoCache p
    LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
    WHERE " + spendItemFilter + " AND CAST(p.Menge AS REAL) > 0 AND k.Bedat IS NOT NULL AND k.Bedat <> '' AND " + joinedEkkoPeriod + @"
    GROUP BY Supplier, Article, Year
)
SELECT
    Supplier || ' / ' || Article AS Label,
    'CHF ' || printf('%.2f', UnitPrice) AS Value,
    'Jahr ' || Year || ' | PowerBI: Min(Netwr CHF/Stk)' AS Detail,
    CASE WHEN UnitPrice > 1000 THEN 'High'
         WHEN UnitPrice > 100 THEN 'Medium'
         ELSE 'Low' END AS Severity
FROM priced
WHERE UnitPrice IS NOT NULL
ORDER BY Year DESC, UnitPrice DESC
LIMIT 10;", cancellationToken);
        // Preisentwicklung als mengengewichteter Durchschnitts-Stueckpreis (CHF) je Jahr.
        // Frueher: MIN ueber alle Artikel -> zeigte praktisch immer den billigsten Cent-Artikel
        // und war als Preisindex fachlich aussagelos.
        state.PriceVarianceChartRows = await ExecuteChartRowsAsync(conn, @"
SELECT substr(k.Bedat, 1, 4) AS Year,
       CASE WHEN SUM(CAST(p.Menge AS REAL)) = 0 THEN 0
            ELSE SUM(" + ChfNetValue + @") / SUM(CAST(p.Menge AS REAL)) END AS Value
FROM PurchasingEkpoCache p
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
WHERE " + spendItemFilter + " AND CAST(p.Menge AS REAL) > 0 AND k.Bedat IS NOT NULL AND k.Bedat <> '' AND " + joinedEkkoPeriod + @"
GROUP BY Year
ORDER BY Year;", cancellationToken);
        state.PriceTrendChartRows = state.PriceVarianceChartRows.ToList();
        // Preisentwicklung je Artikel: Top-N-Artikel nach Spend, mengengewichteter Durchschnitts-
        // Stueckpreis je Jahr, plus Trend gegenueber dem letzten Vorjahr mit Daten. Das entspricht
        // der PBIX-Artikel-Achse und ist fachlich aussagekraeftiger als ein Minimum ueber alle Artikel.
        state.ArticlePriceTrendRows = await ExecuteArticlePriceTrendRowsAsync(conn, joinedEkkoPeriod, spendItemFilter, cancellationToken);

        state.SpendConcentrationChartRows = await ExecuteChartRowsAsync(conn, @"
SELECT " + SupplierLabelSql("k.Lifnr", "k.SupplierName") + @" AS Label, SUM(" + ChfNetValue + @") AS Value
FROM PurchasingEkpoCache p
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
WHERE " + spendItemFilter + " AND " + joinedEkkoPeriod + @"
GROUP BY Label
ORDER BY Value DESC
LIMIT 10;", cancellationToken);
        var totalSpend = state.SpendChfSample <= 0 ? 1 : state.SpendChfSample;
        var concentrationRows = await ExecuteAnalysisRowsAsync(conn, @"
SELECT
    " + SupplierLabelSql("k.Lifnr", "k.SupplierName") + @" AS Label,
    'CHF ' || printf('%,.0f', SUM(" + ChfNetValue + @")) AS Value,
    COUNT(DISTINCT COALESCE(NULLIF(p.Matkl, ''), 'ohne Warengruppe')) || ' Warengruppen' AS Detail,
    CASE WHEN SUM(" + ChfNetValue + @") > 1000000 THEN 'High'
         WHEN SUM(" + ChfNetValue + @") > 250000 THEN 'Medium'
         ELSE 'Low' END AS Severity
FROM PurchasingEkpoCache p
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
WHERE " + spendItemFilter + " AND " + joinedEkkoPeriod + @"
GROUP BY Label
ORDER BY SUM(" + ChfNetValue + @") DESC
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
            .Select(row => new { Ebeln = GetText(row, "Ebeln"), Lifnr = FormatSupplierLabel(GetText(row, "Lifnr")) })
            .Where(row => !string.IsNullOrWhiteSpace(row.Ebeln))
            .GroupBy(row => row.Ebeln, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Lifnr, StringComparer.OrdinalIgnoreCase);
        var monthByEbeln = ekkoRows
            .Select(row => new { Ebeln = GetText(row, "Ebeln"), Month = TryParseSapDate(GetText(row, "Bedat"))?.ToString("yyyy-MM") ?? "ohne Datum" })
            .Where(row => !string.IsNullOrWhiteSpace(row.Ebeln))
            .GroupBy(row => row.Ebeln, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Month, StringComparer.OrdinalIgnoreCase);

        var enriched = ekpoRows
            .Select(row =>
            {
                var ebeln = GetText(row, "Ebeln");
                supplierByEbeln.TryGetValue(ebeln, out var supplier);
                monthByEbeln.TryGetValue(ebeln, out var month);
                var netwr = GetDecimal(row, "Netwr");
                var quantity = GetDecimal(row, "Menge");
                return new
                {
                    Ebeln = ebeln,
                    Supplier = string.IsNullOrWhiteSpace(supplier) ? "ohne Lieferant" : supplier,
                    Month = string.IsNullOrWhiteSpace(month) ? "ohne Datum" : month,
                    Material = FirstNonEmpty(GetText(row, "Matnr"), GetText(row, "Txz01"), "ohne Artikel"),
                    MaterialGroup = PurchasingMaterialGroupTextCatalog.Resolve(FirstNonEmpty(GetText(row, "Matkl"), "ohne Warengruppe")),
                    NetValue = netwr,
                    Quantity = quantity
                };
            })
            .ToList();

        state.SpendChfSample = enriched.Sum(row => row.NetValue);
        state.TopSupplierLabel = BuildTopLabel(enriched.GroupBy(row => row.Supplier), row => row.NetValue, "Lieferant");
        state.TopMaterialGroupLabel = BuildTopLabel(enriched.GroupBy(row => row.MaterialGroup), row => row.NetValue, "Warengruppe");
        state.TopArticleLabel = BuildTopLabel(enriched.GroupBy(row => $"{row.Material} | {row.Supplier} | Monat {row.Month}"), row => row.NetValue, "Artikel");
        state.SpendChartRows = enriched
            .GroupBy(row => row.Supplier)
            .Select(group => new PurchasingLiveChartPoint(group.Key, group.Sum(row => row.NetValue)))
            .OrderByDescending(row => row.Value)
            .Take(6)
            .ToList();
    }

    private static void ApplyEketMetrics(
        PurchasingDashboardLiveState state,
        List<Dictionary<string, object?>> ekkoRows,
        List<Dictionary<string, object?>> ekpoRows,
        List<Dictionary<string, object?>> eketRows)
    {
        if (eketRows.Count == 0)
            return;

        var supplierByEbeln = ekkoRows
            .Select(row => new { Ebeln = GetText(row, "Ebeln"), Lifnr = FormatSupplierLabel(GetText(row, "Lifnr")) })
            .Where(row => !string.IsNullOrWhiteSpace(row.Ebeln))
            .GroupBy(row => row.Ebeln, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Lifnr, StringComparer.OrdinalIgnoreCase);
        var itemByPosition = ekpoRows
            .Select(row =>
            {
                var ebeln = GetText(row, "Ebeln");
                var ebelp = GetText(row, "Ebelp");
                return new
                {
                    key = $"{ebeln}|{ebelp}",
                    Article = FirstNonEmpty(GetText(row, "Matnr"), GetText(row, "Txz01"), "ohne Artikel")
                };
            })
            .GroupBy(row => row.key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Article, StringComparer.OrdinalIgnoreCase);
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
                itemByPosition.TryGetValue(key, out var article);
                supplierByEbeln.TryGetValue(ebeln, out var supplier);
                var quantity = GetDecimal(row, "Menge");
                var received = GetDecimal(row, "Wemng");
                var openQuantity = Math.Max(0, quantity - received);
                return new
                {
                    Ebeln = ebeln,
                    Supplier = string.IsNullOrWhiteSpace(supplier) ? "ohne Lieferant" : supplier,
                    Article = string.IsNullOrWhiteSpace(article) ? "ohne Artikel" : article,
                    DueDate = TryParseSapDate(GetText(row, "Eindt")),
                    OpenQuantity = openQuantity,
                    OpenValue = openQuantity * netPrice
                };
            })
            .ToList();

        state.OpenQuantitySample = enriched.Sum(row => row.OpenQuantity);
        state.OpenValueSample = enriched.Sum(row => row.OpenValue);
        // ACHTUNG, Notweg ohne Cache (nur erreichbar, wenn eine der drei Cachetabellen leer ist):
        // hier steht der Kontrakt-Restwert weiterhin als blosse Kopie des offenen Bestellwerts,
        // weil die Live-Stichprobe EKKO.Konnr nicht mitliest. Im Cachepfad ist das seit K4 getrennt
        // (siehe ContractValueSample oben). Wer diesen Pfad ausbaut, muss Konnr mitselektieren.
        state.ContractValueSample = state.OpenValueSample;
        state.OpenValueChartRows = enriched
            .GroupBy(row => row.DueDate?.ToString("yyyy-MM") ?? "ohne Termin")
            .Select(group => new PurchasingLiveChartPoint(group.Key, group.Sum(row => row.OpenValue)))
            .OrderBy(row => row.Label)
            .Take(6)
            .ToList();
        state.CommitmentDetailChartRows = enriched
            .Where(row => row.OpenValue > 0)
            .GroupBy(row => $"{row.Supplier} | {row.Article} | faellig {row.DueDate?.ToString("yyyy-MM") ?? "ohne Termin"}")
            .Select(group => new PurchasingLiveChartPoint(group.Key, group.Sum(row => row.OpenValue)))
            .OrderByDescending(row => row.Value)
            .Take(6)
            .ToList();
        state.ContractChartRows = state.CommitmentDetailChartRows.Count > 0
            ? state.CommitmentDetailChartRows.ToList()
            : state.OpenValueChartRows.ToList();
        state.TopCommitmentLabel = state.CommitmentDetailChartRows.Count > 0
            ? $"{state.CommitmentDetailChartRows[0].Label}: CHF {state.CommitmentDetailChartRows[0].Value:N0}"
            : string.Empty;
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

    private static async Task<string> ExecuteTopLabelAsync(SqliteConnection conn, string sql, string fallback, CancellationToken cancellationToken, Func<string, string>? transformLabel = null)
    {
        await using var command = conn.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return fallback;

        var label = reader.GetString(0);
        if (transformLabel is not null)
            label = transformLabel(label);
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

    /// <summary>
    /// Volumen je Belegwaehrung (<c>EKKO.Waers</c>), CHF-bewertet plus Summe in der Belegwaehrung
    /// selbst. Belege ohne Waehrungskennzeichen laufen unter „ohne Waehrung" und werden nicht
    /// stillschweigend zu CHF - sonst waere eine Datenluecke als Schweizer Volumen getarnt.
    /// Siehe <see cref="PurchasingCurrencySpendRow"/> zur Abgrenzung gegen die Beschaffungsregion.
    /// </summary>
    private static async Task<List<PurchasingCurrencySpendRow>> ExecuteCurrencySpendRowsAsync(
        SqliteConnection conn,
        string joinedEkkoPeriod,
        string spendItemFilter,
        CancellationToken cancellationToken)
    {
        var rows = new List<PurchasingCurrencySpendRow>();
        await using var command = conn.CreateCommand();
        command.CommandText = @"
SELECT COALESCE(NULLIF(k.Waers, ''), 'ohne Waehrung') AS Currency,
       SUM(" + ChfNetValue + @") AS ChfValue,
       SUM(CAST(p.Netwr AS REAL)) AS OriginalValue
FROM PurchasingEkpoCache p
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
WHERE " + spendItemFilter + " AND " + joinedEkkoPeriod + @"
GROUP BY Currency
ORDER BY ChfValue DESC
LIMIT 12;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new PurchasingCurrencySpendRow(
                reader.GetString(0),
                Convert.ToDecimal(reader.GetValue(1), CultureInfo.InvariantCulture),
                Convert.ToDecimal(reader.GetValue(2), CultureInfo.InvariantCulture)));
        }

        return rows;
    }

    private static async Task<List<PurchasingSupplierYearSpendRow>> ExecuteSupplierYearSpendRowsAsync(
        SqliteConnection conn,
        PurchasingDashboardFilter filter,
        string spendItemFilter,
        CancellationToken cancellationToken)
    {
        var years = Enumerable.Range(filter.FromDate.Year, filter.ToDate.Year - filter.FromDate.Year + 1)
            .Where(year => year >= MinSpendYear && year <= MaxSpendYear(filter))
            .ToList();
        if (years.Count == 0)
            return [];

        var from = filter.FromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var to = filter.ToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var maxSpendYear = MaxSpendYear(filter);
        var rowsBySupplier = new Dictionary<string, Dictionary<int, decimal>>(StringComparer.OrdinalIgnoreCase);

        await using var command = conn.CreateCommand();
        command.CommandText = @"
SELECT " + SupplierLabelSql("k.Lifnr", "k.SupplierName") + @" AS Supplier,
       CAST(substr(k.Bedat, 1, 4) AS INTEGER) AS Year,
       SUM(" + ChfValueSql("p.Netwr", "k.Waers", "k.Wkurs") + @") AS Value
FROM PurchasingEkpoCache p
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
WHERE " + spendItemFilter + @"
  AND k.Bedat >= '" + from + @"'
  AND k.Bedat <= '" + to + @"'
  AND CAST(substr(k.Bedat, 1, 4) AS INTEGER) BETWEEN " + MinSpendYear + " AND " + maxSpendYear + @"
GROUP BY Supplier, Year
ORDER BY Supplier, Year;";

        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var supplier = reader.IsDBNull(0) ? "ohne Lieferant" : reader.GetString(0);
                var year = Convert.ToInt32(reader.GetValue(1), CultureInfo.InvariantCulture);
                var value = Convert.ToDecimal(reader.GetValue(2), CultureInfo.InvariantCulture);
                if (!rowsBySupplier.TryGetValue(supplier, out var values))
                {
                    values = [];
                    rowsBySupplier[supplier] = values;
                }

                values[year] = value;
            }
        }

        var groupsBySupplier = await ExecuteSupplierGroupYearRowsAsync(conn, filter, spendItemFilter, years, cancellationToken);

        return rowsBySupplier
            .Select(row => new PurchasingSupplierYearSpendRow(
                row.Key,
                years.ToDictionary(year => year, year => row.Value.TryGetValue(year, out var value) ? value : 0m),
                row.Value.Values.Sum())
            {
                MaterialGroups = groupsBySupplier.TryGetValue(row.Key, out var groups) ? groups : []
            })
            .OrderByDescending(row => row.Total)
            .Take(40)
            .ToList();
    }

    /// <summary>
    /// Drilldown-Ebene der Spend-Matrix (Marco/Armin 2026-07-17): Spend je Lieferant nach
    /// Warengruppe und Jahr. Warengruppe = aktuelle Materialstamm-Gruppe (MaraMatkl), Fallback
    /// Beleg-Warengruppe (EKPO.Matkl), solange SAP MARA-MATKL noch nicht im Service liefert.
    /// </summary>
    private static async Task<Dictionary<string, List<PurchasingSpendGroupYearRow>>> ExecuteSupplierGroupYearRowsAsync(
        SqliteConnection conn,
        PurchasingDashboardFilter filter,
        string spendItemFilter,
        IReadOnlyList<int> years,
        CancellationToken cancellationToken)
    {
        var from = filter.FromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var to = filter.ToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var maxSpendYear = MaxSpendYear(filter);
        var valuesBySupplierGroup = new Dictionary<string, Dictionary<string, Dictionary<int, decimal>>>(StringComparer.OrdinalIgnoreCase);

        await using var command = conn.CreateCommand();
        command.CommandText = @"
SELECT " + SupplierLabelSql("k.Lifnr", "k.SupplierName") + @" AS Supplier,
       COALESCE(NULLIF(p.MaraMatkl, ''), NULLIF(p.Matkl, ''), 'ohne Warengruppe') AS MaterialGroup,
       CAST(substr(k.Bedat, 1, 4) AS INTEGER) AS Year,
       SUM(" + ChfValueSql("p.Netwr", "k.Waers", "k.Wkurs") + @") AS Value
FROM PurchasingEkpoCache p
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
WHERE " + spendItemFilter + @"
  AND k.Bedat >= '" + from + @"'
  AND k.Bedat <= '" + to + @"'
  AND CAST(substr(k.Bedat, 1, 4) AS INTEGER) BETWEEN " + MinSpendYear + " AND " + maxSpendYear + @"
GROUP BY Supplier, MaterialGroup, Year
ORDER BY Supplier, MaterialGroup, Year;";

        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var supplier = reader.IsDBNull(0) ? "ohne Lieferant" : reader.GetString(0);
                var materialGroup = reader.IsDBNull(1) ? "ohne Warengruppe" : PurchasingMaterialGroupTextCatalog.Resolve(reader.GetString(1));
                var year = Convert.ToInt32(reader.GetValue(2), CultureInfo.InvariantCulture);
                var value = Convert.ToDecimal(reader.GetValue(3), CultureInfo.InvariantCulture);

                if (!valuesBySupplierGroup.TryGetValue(supplier, out var groups))
                {
                    groups = new Dictionary<string, Dictionary<int, decimal>>(StringComparer.OrdinalIgnoreCase);
                    valuesBySupplierGroup[supplier] = groups;
                }

                if (!groups.TryGetValue(materialGroup, out var values))
                {
                    values = [];
                    groups[materialGroup] = values;
                }

                values[year] = value;
            }
        }

        var articlesBySupplierGroup = await ExecuteSupplierGroupArticleYearRowsAsync(conn, filter, spendItemFilter, years, cancellationToken);

        return valuesBySupplierGroup.ToDictionary(
            supplier => supplier.Key,
            supplier => supplier.Value
                .Select(group => new PurchasingSpendGroupYearRow(
                    group.Key,
                    years.ToDictionary(year => year, year => group.Value.TryGetValue(year, out var value) ? value : 0m),
                    group.Value.Values.Sum())
                {
                    Articles = articlesBySupplierGroup.TryGetValue((supplier.Key, group.Key), out var articles) ? articles : []
                })
                .OrderByDescending(group => group.Total)
                .ToList(),
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Deckelung der Artikel je (Lieferant, Warengruppe) in der Spend-Matrix. Bewusst hoeher als
    /// die 10 der Aufriss-Kaskade: hier wird gezielt EINE Warengruppe eines Lieferanten geoeffnet,
    /// nicht der ganze Baum, und fuer die Dummy-Suche will Marco die Liste sehen und nicht nach
    /// zehn Zeilen abgeschnitten bekommen. Der Rest landet in einer "uebrige (n)"-Zeile, damit
    /// Warengruppensumme = Summe der Artikelzeilen bleibt (Pivot-Eigenschaft).
    /// </summary>
    private const int SpendMatrixArticleCap = 25;

    /// <summary>
    /// Dritte Ebene der Spend-Matrix: Spend je Lieferant/Warengruppe/Materialnummer und Jahr
    /// (Entscheid Marco, Sitzung 2026-07-30). Dieselbe Warengruppen- und Artikellogik wie die
    /// Aufriss-Kaskade (<see cref="ExecuteSpendCascadeRowsAsync"/>), damit beide Sichten
    /// dieselben Zahlen zeigen: Warengruppe aus dem Materialstamm mit Fallback auf die
    /// Beleg-Warengruppe, Artikel = Matnr mit Fallback auf den Bestelltext.
    /// </summary>
    private static async Task<Dictionary<(string Supplier, string MaterialGroup), List<PurchasingSpendArticleYearRow>>> ExecuteSupplierGroupArticleYearRowsAsync(
        SqliteConnection conn,
        PurchasingDashboardFilter filter,
        string spendItemFilter,
        IReadOnlyList<int> years,
        CancellationToken cancellationToken)
    {
        var from = filter.FromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var to = filter.ToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var maxSpendYear = MaxSpendYear(filter);
        var values = new Dictionary<(string, string, string), Dictionary<int, decimal>>();

        await using var command = conn.CreateCommand();
        command.CommandText = @"
SELECT " + SupplierLabelSql("k.Lifnr", "k.SupplierName") + @" AS Supplier,
       COALESCE(NULLIF(p.MaraMatkl, ''), NULLIF(p.Matkl, ''), 'ohne Warengruppe') AS MaterialGroup,
       COALESCE(NULLIF(p.Matnr, ''), NULLIF(p.Txz01, ''), 'ohne Artikel') AS Article,
       CAST(substr(k.Bedat, 1, 4) AS INTEGER) AS Year,
       SUM(" + ChfValueSql("p.Netwr", "k.Waers", "k.Wkurs") + @") AS Value
FROM PurchasingEkpoCache p
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
WHERE " + spendItemFilter + @"
  AND k.Bedat >= '" + from + @"'
  AND k.Bedat <= '" + to + @"'
  AND CAST(substr(k.Bedat, 1, 4) AS INTEGER) BETWEEN " + MinSpendYear + " AND " + maxSpendYear + @"
GROUP BY Supplier, MaterialGroup, Article, Year;";

        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var key = (
                    reader.IsDBNull(0) ? "ohne Lieferant" : reader.GetString(0),
                    reader.IsDBNull(1) ? "ohne Warengruppe" : PurchasingMaterialGroupTextCatalog.Resolve(reader.GetString(1)),
                    reader.IsDBNull(2) ? "ohne Artikel" : reader.GetString(2));

                if (!values.TryGetValue(key, out var yearValues))
                {
                    yearValues = [];
                    values[key] = yearValues;
                }

                yearValues[Convert.ToInt32(reader.GetValue(3), CultureInfo.InvariantCulture)] =
                    Convert.ToDecimal(reader.GetValue(4), CultureInfo.InvariantCulture);
            }
        }

        return values
            .GroupBy(entry => (entry.Key.Item1, entry.Key.Item2))
            .ToDictionary(
                group => group.Key,
                group => CapArticles(group.Select(entry => new PurchasingSpendArticleYearRow(
                        entry.Key.Item3,
                        years.ToDictionary(year => year, year => entry.Value.TryGetValue(year, out var value) ? value : 0m),
                        entry.Value.Values.Sum()))
                    .OrderByDescending(article => article.Total)
                    .ToList(),
                    years));
    }

    /// <summary>
    /// Kappt die Artikelliste auf <see cref="SpendMatrixArticleCap"/> und fasst den Rest zu einer
    /// "uebrige (n)"-Zeile zusammen - je Jahr aufsummiert, damit die Jahresspalten weiterhin
    /// aufgehen und nicht nur die Gesamtspalte.
    /// </summary>
    private static List<PurchasingSpendArticleYearRow> CapArticles(
        List<PurchasingSpendArticleYearRow> articles,
        IReadOnlyList<int> years)
    {
        if (articles.Count <= SpendMatrixArticleCap)
            return articles;

        var kept = articles.Take(SpendMatrixArticleCap).ToList();
        var rest = articles.Skip(SpendMatrixArticleCap).ToList();
        kept.Add(new PurchasingSpendArticleYearRow(
            $"uebrige ({rest.Count:N0})",
            years.ToDictionary(year => year, year => rest.Sum(article => article.YearValues.TryGetValue(year, out var value) ? value : 0m)),
            rest.Sum(article => article.Total),
            IsRemainder: true));
        return kept;
    }

    /// <summary>
    /// Eine Aufriss-Dimension: <see cref="Sql"/> ist der SELECT-Ausdruck, der das Label liefert
    /// (die Query joint <c>PurchasingEkpoCache p</c> mit <c>PurchasingEkkoCache k</c>).
    /// <see cref="ResolveMaterialGroupText"/> schaltet die Textauflösung „Code - Text" ueber
    /// <see cref="PurchasingMaterialGroupTextCatalog"/> zu, die nur fuer Warengruppen gilt.
    /// </summary>
    private sealed record SpendDimension(
        string Key,
        string LabelDe,
        string LabelEn,
        string Sql,
        bool ResolveMaterialGroupText = false);

    /// <summary>
    /// Eine Perspektive = Einstiegsdimension plus die Reihenfolge, in der weiter aufgerissen wird.
    /// <see cref="Caps"/> deckelt jede Ebene auf Top-N; der Rest landet in einer
    /// „uebrige (n)"-Zeile, damit Elternsumme = Summe der Kinder bleibt.
    /// </summary>
    private sealed record SpendPerspective(
        string Key,
        string LabelDe,
        string LabelEn,
        IReadOnlyList<SpendDimension> Levels,
        IReadOnlyList<int> Caps);

    private static readonly SpendDimension SupplierDimension =
        new("supplier", "Lieferant", "Supplier", SupplierLabelSql("k.Lifnr", "k.SupplierName"));

    private static readonly SpendDimension MaterialGroupDimension =
        new("materialgroup", "Warengruppe", "Material group",
            "COALESCE(NULLIF(p.MaraMatkl, ''), NULLIF(p.Matkl, ''), 'ohne Warengruppe')",
            ResolveMaterialGroupText: true);

    private static readonly SpendDimension ArticleDimension =
        new("article", "Material", "Material",
            "COALESCE(NULLIF(p.Matnr, ''), NULLIF(p.Txz01, ''), 'ohne Artikel')");

    private static readonly SpendDimension RegionDimension =
        new("region", "Beschaffungsregion", "Procurement region",
            "COALESCE(NULLIF(k.SupplierCountry, ''), 'ohne Land')");

    private static readonly SpendDimension CurrencyDimension =
        new("currency", "Waehrung", "Currency",
            "COALESCE(NULLIF(k.Waers, ''), 'ohne Waehrung')");

    /// <summary>
    /// Die waehlbaren Einstiegsperspektiven im Reiter „Spend-Aufriss".
    ///
    /// Diese Liste ist die Antwort auf die am 2026-07-24 bewusst offen gelassene Rueckfrage
    /// („flexible Einstiegsdimension - Question 2 unbeantwortet"). Marco hat sie in der Sitzung
    /// 2026-07-30 selbst beantwortet: „dass ich wie quasi den hierarchischen Aufriss waehlen kann",
    /// mit den Perspektiven Lieferant, Beschaffungsregion, Warengruppe und Waehrung, und als
    /// Beispielkette ausdruecklich „nach Beschaffungsregion, dann Lieferant, dann Warengruppen und
    /// wieder Material".
    ///
    /// Die Deckelungen sind je Perspektive eigen: bei wenigen Einstiegswerten (Region, Waehrung)
    /// darf die erste Ebene klein sein und die Tiefe grosszuegiger, bei vielen (Lieferant) ist es
    /// umgekehrt. Insgesamt bleibt das Produkt der Deckelungen in derselben Groessenordnung, damit
    /// der serverseitig gerenderte Baum bei &gt;230k Positionen nicht explodiert.
    /// </summary>
    private static readonly IReadOnlyList<SpendPerspective> SpendPerspectives =
    [
        new("supplier", "Lieferant", "Supplier",
            [SupplierDimension, MaterialGroupDimension, ArticleDimension],
            [40, 15, 10]),
        new("region", "Beschaffungsregion", "Procurement region",
            [RegionDimension, SupplierDimension, MaterialGroupDimension, ArticleDimension],
            [12, 15, 10, 8]),
        new("materialgroup", "Warengruppe", "Material group",
            [MaterialGroupDimension, SupplierDimension, ArticleDimension],
            [20, 15, 10]),
        new("currency", "Waehrung", "Currency",
            [CurrencyDimension, SupplierDimension, MaterialGroupDimension, ArticleDimension],
            [8, 15, 10, 8])
    ];

    /// <summary>
    /// Eine Zeile des Aufriss-Groupings. <see cref="Keys"/> traegt die Labels der Ebenen in der
    /// Reihenfolge der gewaehlten Perspektive - dadurch ist der Baumaufbau von der konkreten
    /// Dimensionsfolge unabhaengig.
    /// </summary>
    private readonly record struct CascadeRow(string[] Keys, int Year, decimal Value);

    /// <summary>
    /// Baut alle Perspektiven des Reiters „Spend-Aufriss". Je Perspektive ein SQL-Grouping,
    /// Baumaufbau in C#. Alle Perspektiven werden beim Datenladen vorberechnet, damit das
    /// Umschalten in der UI ohne Reload und ohne erneute DB-Runde funktioniert.
    /// </summary>
    private static async Task<List<PurchasingSpendPerspectiveResult>> ExecuteSpendPerspectivesAsync(
        SqliteConnection conn,
        PurchasingDashboardFilter filter,
        string spendItemFilter,
        CancellationToken cancellationToken)
    {
        var results = new List<PurchasingSpendPerspectiveResult>();
        foreach (var perspective in SpendPerspectives)
        {
            var rows = await ExecuteSpendCascadeRowsAsync(conn, filter, spendItemFilter, perspective, cancellationToken);
            results.Add(new PurchasingSpendPerspectiveResult(
                perspective.Key,
                perspective.LabelDe,
                perspective.LabelEn,
                perspective.Levels.Select(level => level.LabelDe).ToList(),
                perspective.Levels.Select(level => level.LabelEn).ToList(),
                rows));
        }

        return results;
    }

    private sealed record ProductGroupPerspectiveBuildResult(
        PurchasingSpendPerspectiveResult Perspective,
        PurchasingProductGroupAllocationSummary Summary);

    /// <summary>
    /// Produktgruppen-Aufriss ueber die belegte Kette Einkaufsmaterial (EKPO-MATNR) ->
    /// ZLO03-Komponente (KOMPNR) -> Disponent des verwendenden Kopfmaterials (VKNR_DISPO) ->
    /// SAP-ZC23/ZDISPO-Bezeichnung im lokalen OData-Cache PurchasingSpendDisponentRule.
    ///
    /// Eine Komponente kann in Kopfmaterialien mehrerer Produktgruppen vorkommen. Sie wird dann
    /// zu gleichen Teilen auf die UNTERSCHIEDLICHEN Gruppen verteilt. Das ist bewusst keine
    /// Verbrauchsschaetzung: ohne freigegebenen Zurechnungsschluessel ist 1/n die einzige
    /// neutrale Regel, die keine Gruppe bevorzugt und den Gesamtspend exakt erhaelt.
    /// </summary>
    private static async Task<ProductGroupPerspectiveBuildResult> ExecuteProductGroupPerspectiveAsync(
        SqliteConnection conn,
        PurchasingDashboardFilter filter,
        string spendItemFilter,
        CancellationToken cancellationToken)
    {
        var levelsDe = (IReadOnlyList<string>)["Produktgruppe", "Lieferant", "Material"];
        var levelsEn = (IReadOnlyList<string>)["Product group", "Supplier", "Material"];
        var years = Enumerable.Range(filter.FromDate.Year, filter.ToDate.Year - filter.FromDate.Year + 1)
            .Where(year => year >= MinSpendYear && year <= MaxSpendYear(filter))
            .ToList();
        var emptyPerspective = new PurchasingSpendPerspectiveResult(
            "productgroup", "Produktgruppe", "Product group", levelsDe, levelsEn, []);
        if (years.Count == 0)
            return new ProductGroupPerspectiveBuildResult(emptyPerspective, PurchasingProductGroupAllocationSummary.Empty);

        var usageReady = await TableExistsAsync(conn, "MaterialUsageCache", cancellationToken) &&
                         await ColumnExistsAsync(conn, "MaterialUsageCache", "VknrDispo", cancellationToken);
        if (!usageReady)
        {
            // Der Einkauf bleibt voll sichtbar, auch wenn der ZLO03-Cache noch nicht migriert oder
            // geladen ist. Keine Zeile wird still verworfen oder einer erfundenen Gruppe gegeben.
            var fallback = new SpendPerspective(
                "productgroup", "Produktgruppe", "Product group",
                [new SpendDimension("productgroup", "Produktgruppe", "Product group", "'ohne Produktgruppe'"), SupplierDimension, ArticleDimension],
                [20, 15, 10]);
            var fallbackRows = await ExecuteSpendCascadeRowsAsync(conn, filter, spendItemFilter, fallback, cancellationToken);
            var fallbackUnassignedSpend = fallbackRows.Sum(row => row.Total);
            var fallbackUnassignedMaterials = await CountSpendMaterialsAsync(conn, filter, spendItemFilter, cancellationToken);
            return new ProductGroupPerspectiveBuildResult(
                emptyPerspective with { Rows = fallbackRows },
                new PurchasingProductGroupAllocationSummary(0m, fallbackUnassignedSpend, 0m, 0, fallbackUnassignedMaterials, 0, 0, 0));
        }

        var hasSapProductGroupRules = await TableExistsAsync(conn, "PurchasingSpendDisponentRule", cancellationToken);
        var sapProductGroupCtes = hasSapProductGroupRules
            ? @"SapProductGroupDispatchers AS (
    SELECT DISTINCT trim(VknrDispo) AS Disponent
    FROM MaterialUsageCache
    WHERE COALESCE(trim(VknrDispo), '') <> ''
),
SapProductGroupCandidates AS (
    SELECT d.Disponent,
           r.ProductGroup,
           r.ProductGroupText,
           DENSE_RANK() OVER (
               PARTITION BY d.Disponent
               ORDER BY
                   CASE WHEN upper(trim(r.DisponentPattern)) = upper(d.Disponent) THEN 0 ELSE 1 END,
                   length(trim(r.DisponentPattern)) DESC
           ) AS MatchRank
    FROM SapProductGroupDispatchers d
    INNER JOIN PurchasingSpendDisponentRule r
        ON upper(trim(r.DisponentPattern)) = upper(d.Disponent)
        OR (
            substr(trim(r.DisponentPattern), -1, 1) = '*'
            AND upper(d.Disponent) LIKE upper(substr(trim(r.DisponentPattern), 1, length(trim(r.DisponentPattern)) - 1)) || '%'
        )
    WHERE upper(trim(r.Source)) LIKE 'SAP ODATA%'
),
SapProductGroupResolved AS (
    SELECT DISTINCT Disponent, ProductGroup, ProductGroupText
    FROM SapProductGroupCandidates
    WHERE MatchRank = 1
),
"
            : string.Empty;
        var mapJoin = hasSapProductGroupRules
            ? "LEFT JOIN SapProductGroupResolved x ON upper(x.Disponent) = upper(trim(u.VknrDispo))"
            : string.Empty;
        var mappedCode = hasSapProductGroupRules ? "COALESCE(NULLIF(trim(x.ProductGroup), ''), '')" : "''";
        var mappedText = hasSapProductGroupRules ? "COALESCE(NULLIF(trim(x.ProductGroupText), ''), '')" : "''";
        var productGroupLabel = $@"CASE
            WHEN {mappedCode} <> '' AND {mappedText} <> '' THEN {mappedCode} || ' - ' || {mappedText}
            WHEN {mappedCode} <> '' THEN {mappedCode}
            ELSE 'Disponent ' || trim(u.VknrDispo)
        END";
        var usageMaterialKey = NormalizeMaterialKeySql("u.Kompnr");
        var purchasingMaterialKey = NormalizeMaterialKeySql("p.Matnr");
        var from = filter.FromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var to = filter.ToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var maxSpendYear = MaxSpendYear(filter);
        var usageGroupsCte = $@"
{sapProductGroupCtes}UsageGroups AS (
    SELECT DISTINCT {usageMaterialKey} AS MaterialKey,
           {productGroupLabel} AS ProductGroup
    FROM MaterialUsageCache u
    {mapJoin}
    WHERE COALESCE(trim(u.Kompnr), '') <> ''
      AND COALESCE(trim(u.VknrDispo), '') <> ''
),
GroupCounts AS (
    SELECT MaterialKey, COUNT(*) AS GroupCount
    FROM UsageGroups
    GROUP BY MaterialKey
)";

        var rows = new List<CascadeRow>();
        await using (var command = conn.CreateCommand())
        {
            command.CommandText = $@"
WITH {usageGroupsCte},
AllocatedPositions AS (
    SELECT COALESCE(ug.ProductGroup, 'ohne Produktgruppe') AS ProductGroup,
           {SupplierLabelSql("k.Lifnr", "k.SupplierName")} AS Supplier,
           {ArticleDimension.Sql} AS Article,
           CAST(substr(k.Bedat, 1, 4) AS INTEGER) AS Year,
           {ChfValueSql("p.Netwr", "k.Waers", "k.Wkurs")} /
               CASE WHEN COALESCE(gc.GroupCount, 0) > 0 THEN gc.GroupCount ELSE 1 END AS AllocatedValue
    FROM PurchasingEkpoCache p
    LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
    LEFT JOIN UsageGroups ug ON ug.MaterialKey = {purchasingMaterialKey}
    LEFT JOIN GroupCounts gc ON gc.MaterialKey = {purchasingMaterialKey}
    WHERE {spendItemFilter}
      AND k.Bedat >= '{from}' AND k.Bedat <= '{to}'
      AND CAST(substr(k.Bedat, 1, 4) AS INTEGER) BETWEEN {MinSpendYear} AND {maxSpendYear}
)
SELECT ProductGroup, Supplier, Article, Year, SUM(AllocatedValue) AS Value
FROM AllocatedPositions
GROUP BY ProductGroup, Supplier, Article, Year;";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new CascadeRow(
                    [reader.GetString(0), reader.GetString(1), reader.GetString(2)],
                    reader.GetInt32(3),
                    Convert.ToDecimal(reader.GetValue(4), CultureInfo.InvariantCulture)));
            }
        }

        decimal assignedSpend;
        decimal unassignedSpend;
        decimal multiGroupSpend;
        int assignedMaterials;
        int unassignedMaterials;
        int multiGroupMaterials;
        await using (var summaryCommand = conn.CreateCommand())
        {
            summaryCommand.CommandText = $@"
WITH {usageGroupsCte},
PositionFacts AS (
    SELECT {purchasingMaterialKey} AS MaterialKey,
           {ChfValueSql("p.Netwr", "k.Waers", "k.Wkurs")} AS Value,
           COALESCE(gc.GroupCount, 0) AS GroupCount
    FROM PurchasingEkpoCache p
    LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
    LEFT JOIN GroupCounts gc ON gc.MaterialKey = {purchasingMaterialKey}
    WHERE {spendItemFilter}
      AND k.Bedat >= '{from}' AND k.Bedat <= '{to}'
)
SELECT
    COALESCE(SUM(CASE WHEN GroupCount > 0 THEN Value ELSE 0 END), 0),
    COALESCE(SUM(CASE WHEN GroupCount = 0 THEN Value ELSE 0 END), 0),
    COALESCE(SUM(CASE WHEN GroupCount > 1 THEN Value ELSE 0 END), 0),
    COUNT(DISTINCT CASE WHEN GroupCount > 0 THEN MaterialKey END),
    COUNT(DISTINCT CASE WHEN GroupCount = 0 THEN MaterialKey END),
    COUNT(DISTINCT CASE WHEN GroupCount > 1 THEN MaterialKey END)
FROM PositionFacts;";
            await using var reader = await summaryCommand.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            assignedSpend = Convert.ToDecimal(reader.GetValue(0), CultureInfo.InvariantCulture);
            unassignedSpend = Convert.ToDecimal(reader.GetValue(1), CultureInfo.InvariantCulture);
            multiGroupSpend = Convert.ToDecimal(reader.GetValue(2), CultureInfo.InvariantCulture);
            assignedMaterials = reader.GetInt32(3);
            unassignedMaterials = reader.GetInt32(4);
            multiGroupMaterials = reader.GetInt32(5);
        }

        var mappedDispatchers = 0;
        var unmappedDispatchers = 0;
        await using (var mappingCommand = conn.CreateCommand())
        {
            mappingCommand.CommandText = hasSapProductGroupRules
                ? $@"
WITH {sapProductGroupCtes}MappedDispatchers AS (
    SELECT trim(u.VknrDispo) AS Disponent,
           {mappedCode} AS ProductGroup
    FROM MaterialUsageCache u
    {mapJoin}
    WHERE COALESCE(trim(u.VknrDispo), '') <> ''
)
SELECT
    COUNT(DISTINCT CASE WHEN ProductGroup <> '' THEN Disponent END),
    COUNT(DISTINCT CASE WHEN ProductGroup = '' THEN Disponent END)
FROM MappedDispatchers;"
                : @"
SELECT 0, COUNT(DISTINCT trim(VknrDispo))
FROM MaterialUsageCache
WHERE COALESCE(trim(VknrDispo), '') <> '';";
            await using var reader = await mappingCommand.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            mappedDispatchers = reader.GetInt32(0);
            unmappedDispatchers = reader.GetInt32(1);
        }

        var perspectiveRows = BuildCascadeLevel(rows, 0, years, [20, 15, 10]);
        return new ProductGroupPerspectiveBuildResult(
            emptyPerspective with { Rows = perspectiveRows },
            new PurchasingProductGroupAllocationSummary(
                assignedSpend,
                unassignedSpend,
                multiGroupSpend,
                assignedMaterials,
                unassignedMaterials,
                multiGroupMaterials,
                mappedDispatchers,
                unmappedDispatchers));
    }

    private static string NormalizeMaterialKeySql(string expression)
        => $@"CASE
            WHEN COALESCE(trim({expression}), '') = '' THEN ''
            WHEN ltrim(upper(trim({expression})), '0') = '' THEN '0'
            ELSE ltrim(upper(trim({expression})), '0')
        END";

    private static async Task<int> CountSpendMaterialsAsync(
        SqliteConnection conn,
        PurchasingDashboardFilter filter,
        string spendItemFilter,
        CancellationToken cancellationToken)
    {
        var from = filter.FromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var to = filter.ToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return await ExecuteScalarIntAsync(conn, $@"
SELECT COUNT(DISTINCT {NormalizeMaterialKeySql("p.Matnr")})
FROM PurchasingEkpoCache p
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
WHERE {spendItemFilter} AND k.Bedat >= '{from}' AND k.Bedat <= '{to}';", cancellationToken);
    }

    private static async Task<bool> TableExistsAsync(SqliteConnection conn, string tableName, CancellationToken cancellationToken)
    {
        await using var command = conn.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM sqlite_master WHERE type='table' AND name=$Name;";
        command.Parameters.AddWithValue("$Name", tableName);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture) > 0;
    }

    private static async Task<bool> ColumnExistsAsync(
        SqliteConnection conn,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        await using var command = conn.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{tableName.Replace("\"", "\"\"")}\");";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Verbindet die beiden bisher isolierten Klassifikationen zu einer Einkaufs-Massnahmenmatrix:
    /// ABC beschreibt die Wertbedeutung, XYZ die Regelmaessigkeit/Planbarkeit des Bedarfs. Die
    /// Empfehlungen sind bewusst operative Pruefauftraege, keine automatische Disposition.
    /// </summary>
    private static async Task<List<PurchasingAbcXyzActionRow>> ExecuteAbcXyzActionRowsAsync(
        SqliteConnection conn,
        string joinedEkkoPeriod,
        string spendItemFilter,
        CancellationToken cancellationToken)
    {
        var rows = new List<PurchasingAbcXyzActionRow>();
        await using var command = conn.CreateCommand();
        command.CommandText = @"
SELECT UPPER(COALESCE(NULLIF(trim(p.MaraAbc), ''), '-')) AS Abc,
       UPPER(COALESCE(NULLIF(trim(p.MaraXyz), ''), '-')) AS Xyz,
       SUM(" + ChfNetValue + @") AS SpendChf,
       COUNT(DISTINCT COALESCE(NULLIF(trim(p.Matnr), ''), NULLIF(trim(p.Txz01), ''), 'ohne Material')) AS MaterialCount,
       COUNT(DISTINCT COALESCE(NULLIF(trim(k.Lifnr), ''), 'ohne Lieferant')) AS SupplierCount
FROM PurchasingEkpoCache p
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
WHERE " + spendItemFilter + " AND " + joinedEkkoPeriod + @"
GROUP BY Abc, Xyz
ORDER BY SpendChf DESC;";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var abc = reader.GetString(0);
            var xyz = reader.GetString(1);
            var recommendation = ResolveAbcXyzRecommendation(abc, xyz);
            rows.Add(new PurchasingAbcXyzActionRow(
                abc == "-" || xyz == "-" ? $"{abc}/{xyz} (unvollstaendig)" : $"{abc}{xyz}",
                abc,
                xyz,
                Convert.ToDecimal(reader.GetValue(2), CultureInfo.InvariantCulture),
                reader.GetInt32(3),
                reader.GetInt32(4),
                recommendation.ActionDe,
                recommendation.ActionEn,
                recommendation.Severity));
        }

        return rows;
    }

    private static (string ActionDe, string ActionEn, string Severity) ResolveAbcXyzRecommendation(string abc, string xyz)
    {
        if (abc == "-" || xyz == "-")
        {
            return (
                "Stammdaten nachpflegen; ohne beide Klassen ist keine belastbare Beschaffungsstrategie ableitbar.",
                "Complete master data; without both classes no reliable procurement strategy can be derived.",
                "Warning");
        }

        return (abc, xyz) switch
        {
            ("A", "X") => (
                "Strategisch absichern: Rahmenvertrag, Lieferfaehigkeit und automatische Disposition pruefen.",
                "Secure strategically: review framework agreement, supply capability and automated planning.",
                "High"),
            ("A", "Y") or ("A", "Z") => (
                "Versorgungsrisiko priorisieren: Forecast, Sicherheitsbestand und Zweitquelle pruefen.",
                "Prioritize supply risk: review forecast, safety stock and a second source.",
                "High"),
            ("B", "X") => (
                "Standardisieren und Mengen buendeln; Bestellrhythmus und Konditionen optimieren.",
                "Standardize and bundle volumes; optimize order cadence and terms.",
                "Medium"),
            ("B", "Y") or ("B", "Z") => (
                "Losgroessen, Mindestmengen und schwankende Bedarfsursachen mit dem Disponenten pruefen.",
                "Review lot sizes, minimum quantities and volatile demand drivers with the planner.",
                "Medium"),
            ("C", "X") => (
                "Prozesskosten senken: Katalog, Automatisierung und Sammelbestellungen pruefen.",
                "Reduce process cost: review catalog, automation and consolidated orders.",
                "Info"),
            ("C", "Y") or ("C", "Z") => (
                "Tail Spend reduzieren: buendeln, auslisten oder auf Standardalternativen umstellen.",
                "Reduce tail spend: bundle, phase out or switch to standard alternatives.",
                "Info"),
            _ => (
                "Klassifikation fachlich pruefen und anschliessend einer Beschaffungsstrategie zuordnen.",
                "Validate the classification and then assign a procurement strategy.",
                "Warning")
        };
    }

    /// <summary>
    /// Mehrstufiger Spend-Aufriss fuer EINE Perspektive (Reiter „Spend-Aufriss", Einstiegsdimension
    /// waehlbar seit 2026-07-30), je Ebene auf Top-N gekappt mit „uebrige (n)"-Restzeile, sodass
    /// Elternsumme = Summe der Kinder bleibt. Ein SQL-Grouping, Baumaufbau in C#. Warengruppe wie
    /// in der Spend-Matrix: MaraMatkl, Fallback Beleg-Matkl.
    /// </summary>
    private static async Task<List<PurchasingSpendCascadeNode>> ExecuteSpendCascadeRowsAsync(
        SqliteConnection conn,
        PurchasingDashboardFilter filter,
        string spendItemFilter,
        SpendPerspective perspective,
        CancellationToken cancellationToken)
    {
        var years = Enumerable.Range(filter.FromDate.Year, filter.ToDate.Year - filter.FromDate.Year + 1)
            .Where(year => year >= MinSpendYear && year <= MaxSpendYear(filter))
            .ToList();
        if (years.Count == 0)
            return [];

        var from = filter.FromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var to = filter.ToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var maxSpendYear = MaxSpendYear(filter);
        var rows = new List<CascadeRow>();

        // Ein Label-Ausdruck je Ebene, in der Reihenfolge der Perspektive. Die Aliasnamen sind
        // positionsbasiert (Level0..LevelN), damit derselbe Ausdruck (z.B. Lieferant) in mehreren
        // Perspektiven auf unterschiedlicher Ebene stehen kann.
        var levelAliases = perspective.Levels.Select((_, index) => $"Level{index}").ToList();
        var selectList = string.Join(",\n       ", perspective.Levels
            .Select((level, index) => $"{level.Sql} AS {levelAliases[index]}"));

        await using var command = conn.CreateCommand();
        command.CommandText = @"
SELECT " + selectList + @",
       CAST(substr(k.Bedat, 1, 4) AS INTEGER) AS Year,
       SUM(" + ChfValueSql("p.Netwr", "k.Waers", "k.Wkurs") + @") AS Value
FROM PurchasingEkpoCache p
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
WHERE " + spendItemFilter + @"
  AND k.Bedat >= '" + from + @"'
  AND k.Bedat <= '" + to + @"'
  AND CAST(substr(k.Bedat, 1, 4) AS INTEGER) BETWEEN " + MinSpendYear + " AND " + maxSpendYear + @"
GROUP BY " + string.Join(", ", levelAliases) + @", Year;";

        var levelCount = perspective.Levels.Count;
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var keys = new string[levelCount];
                for (var index = 0; index < levelCount; index++)
                {
                    var raw = reader.IsDBNull(index) ? string.Empty : reader.GetString(index);
                    // Die COALESCE-Ausdruecke setzen bereits sprechende Platzhalter; NULL kann nur
                    // noch aus dem LEFT JOIN auf den Bestellkopf kommen (Position ohne Kopf im
                    // Cache). Dann lieber "ohne <Dimension>" als eine leere Zeile im Baum.
                    if (string.IsNullOrWhiteSpace(raw))
                        raw = "ohne " + perspective.Levels[index].LabelDe;
                    keys[index] = perspective.Levels[index].ResolveMaterialGroupText
                        ? PurchasingMaterialGroupTextCatalog.Resolve(raw)
                        : raw;
                }

                rows.Add(new CascadeRow(
                    keys,
                    Convert.ToInt32(reader.GetValue(levelCount), CultureInfo.InvariantCulture),
                    Convert.ToDecimal(reader.GetValue(levelCount + 1), CultureInfo.InvariantCulture)));
            }
        }

        return BuildCascadeLevel(rows, 0, years, perspective.Caps);
    }

    private static List<PurchasingSpendCascadeNode> BuildCascadeLevel(
        IReadOnlyList<CascadeRow> rows,
        int depth,
        IReadOnlyList<int> years,
        IReadOnlyList<int> caps)
    {
        var cap = caps[depth];
        var isLeaf = depth == caps.Count - 1;
        var groups = rows
            .GroupBy(row => row.Keys[depth])
            .Select(group => new
            {
                Label = group.Key,
                Total = group.Sum(row => row.Value),
                YearValues = years.ToDictionary(year => year, year => group.Where(row => row.Year == year).Sum(row => row.Value)),
                Rows = (IReadOnlyList<CascadeRow>)group.ToList()
            })
            .OrderByDescending(group => group.Total)
            .ToList();

        var nodes = new List<PurchasingSpendCascadeNode>();
        foreach (var group in groups.Take(cap))
        {
            var children = isLeaf
                ? (IReadOnlyList<PurchasingSpendCascadeNode>)[]
                : BuildCascadeLevel(group.Rows, depth + 1, years, caps);
            nodes.Add(new PurchasingSpendCascadeNode(group.Label, group.YearValues, group.Total, children));
        }

        // Rest ueber die Deckelung hinaus in EINER „uebrige"-Zeile buendeln (ohne weitere Kinder),
        // damit die Elternsumme exakt erhalten bleibt.
        if (groups.Count > cap)
        {
            var rest = groups.Skip(cap).ToList();
            var restYear = years.ToDictionary(year => year, year => rest.Sum(group => group.YearValues[year]));
            var restTotal = rest.Sum(group => group.Total);
            nodes.Add(new PurchasingSpendCascadeNode($"uebrige ({rest.Count})", restYear, restTotal, []));
        }

        return nodes;
    }

    /// <summary>
    /// Region-Anteil je (Top-)Warengruppe fuer die Kuchendiagramme im Spend-Aufriss. Ein
    /// SQL-Grouping (Warengruppe x Region), dann Top-Warengruppen und je Gruppe Top-Regionen mit
    /// „uebrige"-Rest, sodass die Summe der Slices = Gruppensumme bleibt. Region = Lieferantenland
    /// (SupplierCountry); fuellt sich erst mit dem naechsten Einkauf-Full-Load.
    /// </summary>
    private static async Task<List<PurchasingRegionPieGroup>> ExecuteRegionByMaterialGroupRowsAsync(
        SqliteConnection conn,
        string joinedEkkoPeriod,
        string spendItemFilter,
        CancellationToken cancellationToken)
    {
        const int topGroupCount = 6;
        const int topRegionCount = 8;
        var valuesByGroup = new Dictionary<string, Dictionary<string, decimal>>(StringComparer.OrdinalIgnoreCase);

        await using var command = conn.CreateCommand();
        command.CommandText = @"
SELECT COALESCE(NULLIF(p.MaraMatkl, ''), NULLIF(p.Matkl, ''), 'ohne Warengruppe') AS MaterialGroup,
       COALESCE(NULLIF(k.SupplierCountry, ''), 'ohne Land') AS Region,
       SUM(" + ChfNetValue + @") AS Value
FROM PurchasingEkpoCache p
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
WHERE " + spendItemFilter + " AND " + joinedEkkoPeriod + @"
GROUP BY MaterialGroup, Region;";

        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var group = reader.IsDBNull(0) ? "ohne Warengruppe" : PurchasingMaterialGroupTextCatalog.Resolve(reader.GetString(0));
                var region = reader.IsDBNull(1) ? "ohne Land" : reader.GetString(1);
                var value = Convert.ToDecimal(reader.GetValue(2), CultureInfo.InvariantCulture);
                if (!valuesByGroup.TryGetValue(group, out var regions))
                {
                    regions = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                    valuesByGroup[group] = regions;
                }

                regions[region] = regions.TryGetValue(region, out var existing) ? existing + value : value;
            }
        }

        return valuesByGroup
            .Select(group => new
            {
                group.Key,
                Total = group.Value.Values.Sum(),
                Regions = group.Value
            })
            .Where(group => group.Total > 0m)
            .OrderByDescending(group => group.Total)
            .Take(topGroupCount)
            .Select(group => new PurchasingRegionPieGroup(
                group.Key,
                group.Total,
                BuildRegionSlices(group.Regions, topRegionCount)))
            .ToList();
    }

    private static List<PurchasingLiveChartPoint> BuildRegionSlices(IReadOnlyDictionary<string, decimal> regions, int topRegionCount)
    {
        var ordered = regions
            .Select(region => new PurchasingLiveChartPoint(region.Key, region.Value))
            .OrderByDescending(region => region.Value)
            .ToList();
        if (ordered.Count <= topRegionCount)
            return ordered;

        var slices = ordered.Take(topRegionCount).ToList();
        var rest = ordered.Skip(topRegionCount).ToList();
        slices.Add(new PurchasingLiveChartPoint($"uebrige ({rest.Count})", rest.Sum(region => region.Value)));
        return slices;
    }

    // Preisentwicklung je Artikel: Top-N-Artikel nach CHF-Spend, danach mengengewichteter
    // Durchschnitts-Stueckpreis (CHF) je Jahr. Der YoY-Trend vergleicht das letzte Jahr mit Daten
    // gegen das davor liegende Jahr mit Daten (nicht zwingend das direkte Vorjahr).
    private static async Task<List<PurchasingIdeaAnalysisRow>> ExecuteArticlePriceTrendRowsAsync(
        SqliteConnection conn,
        string joinedEkkoPeriod,
        string spendItemFilter,
        CancellationToken cancellationToken)
    {
        const int topArticleCount = 8;
        var pricesByArticle = new Dictionary<string, SortedDictionary<int, decimal>>(StringComparer.OrdinalIgnoreCase);

        await using var command = conn.CreateCommand();
        command.CommandText = @"
WITH article_spend AS (
    SELECT COALESCE(NULLIF(p.Matnr, ''), NULLIF(p.Txz01, ''), 'ohne Artikel') AS Article,
           SUM(" + ChfNetValue + @") AS TotalSpend
    FROM PurchasingEkpoCache p
    LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
    WHERE " + spendItemFilter + " AND CAST(p.Menge AS REAL) > 0 AND k.Bedat IS NOT NULL AND k.Bedat <> '' AND " + joinedEkkoPeriod + @"
    GROUP BY Article
    ORDER BY TotalSpend DESC
    LIMIT " + topArticleCount.ToString(CultureInfo.InvariantCulture) + @"
)
SELECT COALESCE(NULLIF(p.Matnr, ''), NULLIF(p.Txz01, ''), 'ohne Artikel') AS Article,
       CAST(substr(k.Bedat, 1, 4) AS INTEGER) AS Year,
       CASE WHEN SUM(CAST(p.Menge AS REAL)) = 0 THEN 0
            ELSE SUM(" + ChfNetValue + @") / SUM(CAST(p.Menge AS REAL)) END AS Price
FROM PurchasingEkpoCache p
LEFT JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
JOIN article_spend a ON a.Article = COALESCE(NULLIF(p.Matnr, ''), NULLIF(p.Txz01, ''), 'ohne Artikel')
WHERE " + spendItemFilter + " AND CAST(p.Menge AS REAL) > 0 AND k.Bedat IS NOT NULL AND k.Bedat <> '' AND " + joinedEkkoPeriod + @"
GROUP BY Article, Year
ORDER BY Article, Year;";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var article = reader.IsDBNull(0) ? "ohne Artikel" : reader.GetString(0);
            var year = Convert.ToInt32(reader.GetValue(1), CultureInfo.InvariantCulture);
            var price = Convert.ToDecimal(reader.GetValue(2), CultureInfo.InvariantCulture);
            if (price <= 0)
                continue;

            if (!pricesByArticle.TryGetValue(article, out var byYear))
            {
                byYear = [];
                pricesByArticle[article] = byYear;
            }

            byYear[year] = price;
        }

        var rows = new List<PurchasingIdeaAnalysisRow>();
        foreach (var (article, byYear) in pricesByArticle)
        {
            if (byYear.Count == 0)
                continue;

            var years = byYear.Keys.ToList();
            var currentYear = years[^1];
            var currentPrice = byYear[currentYear];
            var detail = $"Jahr {currentYear}";
            var severity = "Medium";

            if (years.Count >= 2)
            {
                var previousYear = years[^2];
                var previousPrice = byYear[previousYear];
                if (previousPrice > 0)
                {
                    var changePercent = (currentPrice - previousPrice) / previousPrice * 100m;
                    var arrow = changePercent > 0 ? "+" : string.Empty;
                    detail = $"{previousYear}: CHF {previousPrice:N2} -> {currentYear}: CHF {currentPrice:N2} | {arrow}{changePercent:N1}%";
                    // High = Preis deutlich gestiegen (Kostenrisiko), Low = gesunken (Einsparung).
                    severity = changePercent > 2m ? "High" : changePercent < -2m ? "Low" : "Medium";
                }
            }

            rows.Add(new PurchasingIdeaAnalysisRow(
                article,
                $"CHF {currentPrice:N2}",
                detail,
                severity));
        }

        return rows
            .OrderByDescending(row => row.Severity == "High")
            .ThenBy(row => row.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();
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
        // SAP/OData liefert Zahlen invariant formatiert. Ein CurrentCulture-Fallback koennte je
        // nach Serverkultur "1.234" falsch als 1234 statt 1.234 interpretieren.
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0m;
    }

    private static string FirstNonEmpty(params string[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

    private static string FormatSupplierLabel(string supplierNumber)
        => string.IsNullOrWhiteSpace(supplierNumber)
            ? "ohne Lieferant"
            : supplierNumber;

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
