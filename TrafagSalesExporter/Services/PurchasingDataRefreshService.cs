using System.Data;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;

namespace TrafagSalesExporter.Services;

public sealed class PurchasingDataRefreshService : IPurchasingDataRefreshService
{
    private const int PageSize = 1000;
    // Anzahl Belege je OData-Request beim Delta ($filter=Ebeln eq 'A' or ...), begrenzt die URL-Laenge.
    private const int EbelnBatchSize = 20;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IAppEventLogService _logService;
    private readonly IPurchasingProductGroupSapReader _productGroupSapReader;
    private readonly PurchasingDashboardSnapshotCache? _dashboardSnapshotCache;

    private readonly ISapGatewayStockValueReader? _stockValueReader;
    private readonly IPurchasingStockValueStore? _stockValueStore;

    /// <summary>
    /// Obergrenze fuer den Lagerwert-Read. Siehe <see cref="RefreshStockValueSafeAsync"/> fuer
    /// die Begruendung (Haenger vom 2026-08-19, der jeden Einkauf-Lauf blockierte).
    /// </summary>
    private static readonly TimeSpan StockValueReadTimeout = TimeSpan.FromMinutes(10);

    public PurchasingDataRefreshService(
        IDbContextFactory<AppDbContext> dbFactory,
        IAppEventLogService logService,
        IPurchasingProductGroupSapReader productGroupSapReader,
        ISapGatewayStockValueReader? stockValueReader = null,
        IPurchasingStockValueStore? stockValueStore = null,
        PurchasingDashboardSnapshotCache? dashboardSnapshotCache = null)
    {
        _dbFactory = dbFactory;
        _logService = logService;
        _productGroupSapReader = productGroupSapReader;
        _stockValueReader = stockValueReader;
        _stockValueStore = stockValueStore;
        _dashboardSnapshotCache = dashboardSnapshotCache;
    }

    /// <summary>
    /// Liest den Lagerwert der Einkaufsteile nach und legt ihn in den Reader-Cache, damit die
    /// KPI-Kachel ihn ohne eigenen SAP-Zugriff anzeigen kann.
    ///
    /// WIRFT BEWUSST NICHT. Ein Ausfall dieses Reads darf den Einkauf-Lauf nicht abbrechen.
    /// Begruendung ist der Vorfall vom 2026-07-02, als ein 404 auf `MARA001Set` den Full Load
    /// abbrach und der Datenstand bis zum 2026-07-17 einfror.
    /// </summary>
    private async Task RefreshStockValueSafeAsync(
        PurchasingSapConnection connection, CancellationToken cancellationToken)
    {
        if (_stockValueReader is null)
            return;

        // HARTE ZEITGRENZE. Der `catch` unten faengt nur Ausnahmen, aber der Vorfall vom
        // 2026-08-19 war kein Fehler, sondern ein HAENGER: eine Endlosschleife im Reader
        // lief ohne Ausnahme und ohne Timeout weiter, und weil dieser Aufruf VOR dem
        // Statuseintrag steht, blieb jeder Einkauf-Lauf dauerhaft auf `Running`. Eine
        // Zeitgrenze ist die einzige Absicherung, die auch gegen einen kuenftigen Haenger
        // greift. Der Read kostet regulaer rund eine Minute (zwei Vollbestaende, je etwa
        // 28 s), zehn Minuten sind also reichlich und trotzdem endlich.
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(StockValueReadTimeout);

        try
        {
            var snapshot = await _stockValueReader.RefreshAsync(
                connection.BaseUrl,
                connection.Username,
                connection.Password,
                PurchasingDashboardService.PurchasingValuationArea,
                timeout.Token);

            // In die Datenbank schreiben, nicht nur in den Reader-Cache. Der Cache liegt im
            // Arbeitsspeicher eines Singletons und ist nach jedem Neustart des IIS-Workers
            // leer; genau daran stand die Kachel am 2026-08-24 wieder auf "wartet auf
            // Einkauf-Lauf", obwohl der Wert am 2026-08-21 gelesen worden war.
            if (_stockValueStore is not null)
                await _stockValueStore.SaveAsync(snapshot, cancellationToken);

            await _logService.WriteAsync("Purchasing", "Lagerwert aktualisiert",
                details: $"Bewertungskreis={snapshot.ValuationArea} | Disponenten={snapshot.Rows.Count} | " +
                         $"Materialien={snapshot.TotalMaterialCount:N0} | Gesamtwert={snapshot.TotalValue:N2} | " +
                         $"gespeichert={(_stockValueStore is not null ? "ja" : "nein")}");
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            // Eigene Zeitgrenze gerissen, nicht der Lauf abgebrochen. Das eigens zu melden
            // ist wichtig, weil genau dieser Fall vorher als stiller Haenger auftrat.
            await _logService.WriteAsync("Purchasing", "Lagerwert-Read wegen Zeitgrenze abgebrochen", "Warning",
                details: $"Der Read hat die Grenze von {StockValueReadTimeout.TotalMinutes:N0} Minuten " +
                         "ueberschritten und wurde abgebrochen. Der Einkauf-Lauf laeuft weiter und wird " +
                         "sauber abgeschlossen, die Lagerwert-Kachel bleibt auf dem vorherigen Stand.");
        }
        catch (Exception ex)
        {
            await _logService.WriteAsync("Purchasing", "Lagerwert konnte nicht gelesen werden", "Warning",
                details: $"{ex.GetType().Name}: {ex.Message} — Der Einkauf-Lauf laeuft trotzdem weiter, " +
                         "die Lagerwert-Kachel bleibt auf dem vorherigen Stand.");
        }
    }

    private static readonly TimeSpan ContractLzReadTimeout = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Laedt Mengenkontrakte (<c>EinkKontraktSet</c>) und LZ-/Sortiments-Codes (<c>EinkMatLzSet</c>)
    /// in ihre Caches. Beide werden bei jedem Lauf (Full und Delta) vollstaendig neu gelesen und
    /// ersetzt: Kontrakte sind wenige, die LZ-Codes einige zehntausend Zeilen.
    ///
    /// WIRFT BEWUSST NICHT, ausser der Lauf selbst wird abgebrochen. Beide Sets liegen bis zum
    /// Transport in SAP T76; in P76 antworten sie mit 404. Das darf den Einkauf-Lauf nicht kosten
    /// (Vorfall 2026-07-02, 404 auf MARA001Set). Bei Fehler oder leerer Antwort bleibt der
    /// bisherige Cache stehen, die Seite zeigt "Kontraktdaten noch nicht verfuegbar", solange er leer ist.
    /// Eigene Transaktion je Cache, nach dem Commit der Belegdaten.
    /// </summary>
    internal async Task<string> RefreshContractsAndLzSafeAsync(
        HttpClient client,
        string baseUrl,
        IReadOnlyDictionary<string, SupplierInfo> supplierNameMap,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ContractLzReadTimeout);

        try
        {
            SapEntitySetReader reader = (entitySet, select, filter, orderBy, token)
                => ReadAllRowsAsync(client, baseUrl, entitySet, select, filter, orderBy, token);
            var contracts = await PurchasingContractLoader.TryReadAsync(
                reader, PurchasingContractLoader.ContractSet, PurchasingContractLoader.ContractSelect, "Ebeln,Ebelp", timeout.Token);
            var lzCodes = await PurchasingContractLoader.TryReadAsync(
                reader, PurchasingContractLoader.LzSet, PurchasingContractLoader.LzSelect, "Matnr", timeout.Token);

            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var conn = (SqliteConnection)db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync(cancellationToken);

            var nowText = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            var parts = new List<string>();
            var warnings = new List<string>();

            if (contracts.Available)
            {
                var written = await PurchasingContractLoader.ReplaceContractsAsync(
                    conn, contracts.Rows, lifnr => ResolveSupplierName(supplierNameMap, lifnr, string.Empty), nowText, timeout.Token);
                parts.Add($"Kontraktpositionen={written:N0}");
            }
            else
            {
                warnings.Add(contracts.Warning);
                parts.Add("Kontrakte=nicht verfuegbar (Cache unveraendert)");
            }

            if (lzCodes.Available)
            {
                var written = await PurchasingContractLoader.ReplaceMaterialLzAsync(conn, lzCodes.Rows, nowText, timeout.Token);
                parts.Add($"LZ-Codes={written:N0}");
            }
            else
            {
                warnings.Add(lzCodes.Warning);
                parts.Add("LZ-Codes=nicht verfuegbar (Cache unveraendert)");
            }

            if (warnings.Count > 0)
                await _logService.WriteAsync("Purchasing", "Kontrakt-/LZ-Daten nicht aktualisiert", "Warning",
                    details: string.Join(" | ", warnings) + " Der Einkauf-Lauf laeuft normal weiter.");

            return string.Join(", ", parts) + ".";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            await _logService.WriteAsync("Purchasing", "Kontrakt-/LZ-Daten konnten nicht gelesen werden", "Warning",
                details: $"{ex.GetType().Name}: {ex.Message} — Der Einkauf-Lauf laeuft trotzdem weiter, " +
                         "die bisherigen Kontrakt- und LZ-Daten bleiben stehen.");
            return "Kontrakte/LZ-Codes=Fehler beim Lesen (Cache unveraendert).";
        }
    }

    /// <summary>Zeitbudget je Lauf fuer die Verwendungsabfrage (P76 schonen; der erste Aufbau verteilt sich auf mehrere Laeufe).</summary>
    private static readonly TimeSpan ComponentDispoBudget = TimeSpan.FromMinutes(8);
    private const int ComponentDispoParallelism = 3;

    /// <summary>
    /// Fragt je bestellter Komponente die verkuerzten Nummern und deren Disponenten ab (Produktgruppe im Spend-Aufriss,
    /// Punkt 4 Gespraech Armin 2026-10-08, siehe <see cref="PurchasingComponentDispoLoader"/>). WIRFT NICHT, ausser der
    /// Lauf selbst wird abgebrochen: ein Fehler laesst den bisherigen Stand stehen.
    /// </summary>
    internal async Task<string> RefreshComponentDispoSafeAsync(HttpClient client, string baseUrl, CancellationToken cancellationToken)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var conn = (SqliteConnection)db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync(cancellationToken);

            SapEntitySetReader reader = (entitySet, select, filter, orderBy, token)
                => ReadAllRowsAsync(client, baseUrl, entitySet, select, filter, orderBy, token);
            var result = await PurchasingComponentDispoLoader.RunAsync(
                conn, reader, ComponentDispoBudget, ComponentDispoParallelism, () => DateTime.UtcNow, cancellationToken);
            if (result.Failed > 0)
                await _logService.WriteAsync("Purchasing", "Produktgruppen-Verwendung teilweise nicht gelesen", "Warning",
                    details: $"{result.Failed:N0} Komponenten ohne Antwort, erster Fehler: {result.Warning} Der bisherige Stand bleibt stehen.");
            return $"Produktgruppen-Verwendung: {result.Checked:N0} Komponenten gefragt, {result.WithUsage:N0} mit verkuerzter Nummer, {result.Remaining:N0} offen fuer naechste Laeufe.";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            await _logService.WriteAsync("Purchasing", "Produktgruppen-Verwendung konnte nicht gelesen werden", "Warning",
                details: $"{ex.GetType().Name}: {ex.Message} — Der Einkauf-Lauf laeuft trotzdem weiter.");
            return "Produktgruppen-Verwendung=Fehler beim Lesen (Stand unveraendert).";
        }
    }

    public async Task<PurchasingDataRefreshStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var conn = (SqliteConnection)db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(cancellationToken);

        var status = await ReadLatestStatusAsync(conn, cancellationToken);
        status.EkkoRows = await CountTableAsync(conn, "PurchasingEkkoCache", cancellationToken);
        status.EkpoRows = await CountTableAsync(conn, "PurchasingEkpoCache", cancellationToken);
        status.EketRows = await CountTableAsync(conn, "PurchasingEketCache", cancellationToken);
        status.ProductGroupRows = await CountTableAsync(conn, "PurchasingSpendDisponentRule", cancellationToken);
        return status;
    }

    public async Task<PurchasingDataRefreshStatus> RunFullLoadAsync(DateTime? fromDate = null, CancellationToken cancellationToken = default)
    {
        var started = DateTime.UtcNow;
        await WriteStatusAsync("Full", "Running", started, null, fromDate, null, null, 0, 0, 0, "Full Load gestartet.", cancellationToken);
        await _logService.WriteAsync("Purchasing", "Einkauf Full Load gestartet", details: fromDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        try
        {
            var connection = await ResolveConnectionAsync(cancellationToken);
            using var client = CreateClient(connection.Username, connection.Password);
            var nowText = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            var ekkoFilter = fromDate.HasValue ? $"Bedat ge '{fromDate.Value:yyyy-MM-dd}'" : string.Empty;

            var ekkoRows = await ReadAllRowsAsync(client, connection.BaseUrl, "EKKOSet", "Ebeln,Bedat,Aedat,Lifnr,Bukrs,Bstyp,Bsart,Konnr,Waers,Wkurs", ekkoFilter, "Ebeln", cancellationToken);
            var ekpoRows = await ReadAllRowsAsync(client, connection.BaseUrl, "EKPOSet", "Ebeln,Ebelp,Matnr,Txz01,Matkl,Menge,Ktmng,Netwr,Loekz,Elikz,Bukrs,Werks", string.Empty, "Ebeln,Ebelp", cancellationToken);
            var eketRows = await ReadAllRowsAsync(client, connection.BaseUrl, "eketSet", "Ebeln,Ebelp,Etenr,Eindt,Menge,Wemng", string.Empty, "Ebeln,Ebelp,Etenr", cancellationToken);
            var materialStatusMap = await LoadMaterialMasterMapAsync(client, connection.BaseUrl, cancellationToken);
            var classificationMap = await LoadMaterialClassificationMapAsync(client, connection.BaseUrl, cancellationToken);
            var supplierNameMap = await LoadSupplierNameMapAsync(client, connection.BaseUrl, cancellationToken);
            var productGroupResult = await _productGroupSapReader.ReadAsync(
                connection.BaseUrl, connection.Username, connection.Password, cancellationToken);

            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var conn = (SqliteConnection)db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync(cancellationToken);

            await using var transaction = (SqliteTransaction)await conn.BeginTransactionAsync(cancellationToken);
            await ExecuteAsync(conn, transaction, "DELETE FROM PurchasingEkkoCache;", cancellationToken);
            await ExecuteAsync(conn, transaction, "DELETE FROM PurchasingEkpoCache;", cancellationToken);
            await ExecuteAsync(conn, transaction, "DELETE FROM PurchasingEketCache;", cancellationToken);
            await UpsertEkkoAsync(conn, transaction, ekkoRows, supplierNameMap, nowText, cancellationToken);
            await UpsertEkpoAsync(conn, transaction, ekpoRows, materialStatusMap, classificationMap, nowText, cancellationToken);
            await UpsertEketAsync(conn, transaction, eketRows, nowText, cancellationToken);
            await ReplaceProductGroupRulesAsync(conn, transaction, productGroupResult, nowText, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // Nach dem Commit, damit ein Fehler hier die bereits geschriebenen Belegdaten
            // nicht mehr gefaehrden kann.
            var contractLzMessage = await RefreshContractsAndLzSafeAsync(client, connection.BaseUrl, supplierNameMap, cancellationToken);
            contractLzMessage += " " + await RefreshComponentDispoSafeAsync(client, connection.BaseUrl, cancellationToken);
            await RefreshStockValueSafeAsync(connection, cancellationToken);

            var completed = DateTime.UtcNow;
            var materialTextCount = materialStatusMap.Values.Count(info => info.Maktx.Length > 0);
            var message = $"Full Load abgeschlossen: EKKO={ekkoRows.Count:N0}, EKPO={ekpoRows.Count:N0}, EKET={eketRows.Count:N0}, MARA-Status={materialStatusMap.Count:N0}, MAKT-Texte={materialTextCount:N0}, Klassifizierung={classificationMap.Count:N0}, LFA1-Namen={supplierNameMap.Count:N0}, SAP-Produktgruppen={productGroupResult.Rules.Count:N0} ({productGroupResult.SourceEntitySets}). {contractLzMessage}";
            await WriteStatusAsync("Full", "Success", started, completed, fromDate, null, completed, ekkoRows.Count, ekpoRows.Count, eketRows.Count, message, cancellationToken);
            _dashboardSnapshotCache?.Clear();
            await _logService.WriteAsync("Purchasing", "Einkauf Full Load erfolgreich", details: message);
            return await GetStatusAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var message = $"Full Load fehlgeschlagen: {ex.Message}";
            await WriteStatusAsync("Full", "Error", started, DateTime.UtcNow, fromDate, null, null, 0, 0, 0, message, cancellationToken);
            await _logService.WriteAsync("Purchasing", "Einkauf Full Load fehlgeschlagen", "Error", details: ex.ToString());
            return await GetStatusAsync(cancellationToken);
        }
    }

    public async Task<PurchasingDataRefreshStatus> RunDeltaAsync(DateTime? fromDate = null, CancellationToken cancellationToken = default)
    {
        var current = await GetStatusAsync(cancellationToken);
        var deltaFrom = fromDate ?? current.LastSuccessfulDeltaAtUtc ?? current.CompletedAtUtc ?? DateTime.UtcNow.AddDays(-7);
        var started = DateTime.UtcNow;
        await WriteStatusAsync("Delta", "Running", started, null, deltaFrom, null, current.LastSuccessfulDeltaAtUtc, current.EkkoRows, current.EkpoRows, current.EketRows, "Delta gestartet.", cancellationToken);

        try
        {
            var connection = await ResolveConnectionAsync(cancellationToken);
            using var client = CreateClient(connection.Username, connection.Password);
            var filter = $"Aedat ge '{deltaFrom:yyyy-MM-dd}'";
            var changedEkko = await ReadAllRowsAsync(client, connection.BaseUrl, "EKKOSet", "Ebeln,Bedat,Aedat,Lifnr,Bukrs,Bstyp,Bsart,Konnr,Waers,Wkurs", filter, "Ebeln", cancellationToken);
            var changedEbelns = changedEkko
                .Select(row => GetText(row, "Ebeln"))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Wareneingaenge aendern nur EKET.Wemng, nicht EKKO.Aedat. Ein reines Aedat-Delta
            // wuerde offene Werte dauerhaft veralten lassen. Deshalb zusaetzlich alle Belege
            // nachladen, die im Cache noch offene Mengen haben.
            var openEbelns = await LoadOpenOrderEbelnsAsync(cancellationToken);
            var ebelnKeys = changedEbelns
                .Union(openEbelns, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var ekpoRows = new List<Dictionary<string, object?>>();
            var eketRows = new List<Dictionary<string, object?>>();
            foreach (var chunk in ebelnKeys.Chunk(EbelnBatchSize))
            {
                var ebelnFilter = string.Join(" or ", chunk.Select(ebeln => $"Ebeln eq '{ebeln}'"));
                ekpoRows.AddRange(await ReadAllRowsAsync(client, connection.BaseUrl, "EKPOSet", "Ebeln,Ebelp,Matnr,Txz01,Matkl,Menge,Ktmng,Netwr,Loekz,Elikz,Bukrs,Werks", ebelnFilter, "Ebeln,Ebelp", cancellationToken));
                eketRows.AddRange(await ReadAllRowsAsync(client, connection.BaseUrl, "eketSet", "Ebeln,Ebelp,Etenr,Eindt,Menge,Wemng", ebelnFilter, "Ebeln,Ebelp,Etenr", cancellationToken));
            }

            var materialStatusMap = await LoadMaterialMasterMapAsync(client, connection.BaseUrl, cancellationToken);
            var classificationMap = await LoadMaterialClassificationMapAsync(client, connection.BaseUrl, cancellationToken);
            var supplierNameMap = await LoadSupplierNameMapAsync(client, connection.BaseUrl, cancellationToken);
            var productGroupResult = await _productGroupSapReader.ReadAsync(
                connection.BaseUrl, connection.Username, connection.Password, cancellationToken);

            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var conn = (SqliteConnection)db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync(cancellationToken);

            var nowText = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            await using var transaction = (SqliteTransaction)await conn.BeginTransactionAsync(cancellationToken);
            await UpsertEkkoAsync(conn, transaction, changedEkko, supplierNameMap, nowText, cancellationToken);
            await UpsertEkpoAsync(conn, transaction, ekpoRows, materialStatusMap, classificationMap, nowText, cancellationToken);
            await UpsertEketAsync(conn, transaction, eketRows, nowText, cancellationToken);
            // Stammdaten auf den GANZEN Cache anwenden, nicht nur auf die geholten Belege - sonst
            // wirkt eine im SAP nachgepflegte Warengruppe nie auf alte, abgeschlossene Bestellungen.
            // Begruendung ausfuehrlich in ApplyMaterialMasterToWholeCacheAsync.
            var reclassifiedRows = await ApplyMaterialMasterToWholeCacheAsync(
                conn, transaction, materialStatusMap, classificationMap, cancellationToken);
            await ReplaceProductGroupRulesAsync(conn, transaction, productGroupResult, nowText, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // Nach dem Commit, damit ein Fehler hier die bereits geschriebenen Belegdaten
            // nicht mehr gefaehrden kann.
            var contractLzMessage = await RefreshContractsAndLzSafeAsync(client, connection.BaseUrl, supplierNameMap, cancellationToken);
            contractLzMessage += " " + await RefreshComponentDispoSafeAsync(client, connection.BaseUrl, cancellationToken);
            await RefreshStockValueSafeAsync(connection, cancellationToken);

            var completed = DateTime.UtcNow;
            var status = await GetStatusAsync(cancellationToken);
            var message = $"Delta abgeschlossen: geaenderte Belege={changedEbelns.Count:N0}, offene Belege nachgeladen={openEbelns.Count:N0}, Belege gesamt={ebelnKeys.Count:N0}, EKPO={ekpoRows.Count:N0}, EKET={eketRows.Count:N0}, Stammdaten aktualisiert auf={reclassifiedRows:N0} Cachezeilen, SAP-Produktgruppen={productGroupResult.Rules.Count:N0} ({productGroupResult.SourceEntitySets}). {contractLzMessage}";
            await WriteStatusAsync("Delta", "Success", started, completed, deltaFrom, null, completed, status.EkkoRows, status.EkpoRows, status.EketRows, message, cancellationToken);
            _dashboardSnapshotCache?.Clear();
            await _logService.WriteAsync("Purchasing", "Einkauf Delta erfolgreich", details: message);
            return await GetStatusAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await WriteStatusAsync("Delta", "Error", started, DateTime.UtcNow, deltaFrom, null, current.LastSuccessfulDeltaAtUtc, current.EkkoRows, current.EkpoRows, current.EketRows, $"Delta fehlgeschlagen: {ex.Message}", cancellationToken);
            await _logService.WriteAsync("Purchasing", "Einkauf Delta fehlgeschlagen", "Error", details: ex.ToString());
            return await GetStatusAsync(cancellationToken);
        }
    }

    // Belege mit offener Menge im Cache (EKET.Menge > EKET.Wemng). Diese muessen im Delta
    // erneut geladen werden, weil Wareneingaenge EKKO.Aedat nicht anfassen.
    private async Task<List<string>> LoadOpenOrderEbelnsAsync(CancellationToken cancellationToken)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var conn = (SqliteConnection)db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(cancellationToken);

        var ebelns = new List<string>();
        await using var command = conn.CreateCommand();
        command.CommandText = @"
SELECT DISTINCT e.Ebeln
FROM PurchasingEketCache e
WHERE COALESCE(e.Ebeln, '') <> '' AND CAST(e.Menge AS REAL) > CAST(e.Wemng AS REAL);";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (!reader.IsDBNull(0))
                ebelns.Add(reader.GetString(0));
        }

        return ebelns;
    }

    private async Task<PurchasingSapConnection> ResolveConnectionAsync(CancellationToken cancellationToken)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var sap = await db.SourceSystemDefinitions.AsNoTracking().FirstOrDefaultAsync(x => x.Code == "SAP", cancellationToken)
            ?? throw new InvalidOperationException("SAP Quelle fehlt.");
        var site = await db.Sites.AsNoTracking().FirstOrDefaultAsync(x => x.TSC == PurchasingDataSourcePageService.PurchasingTsc, cancellationToken)
            ?? throw new InvalidOperationException("Einkauf SAP Site fehlt.");
        var serviceUrl = string.IsNullOrWhiteSpace(site.SapServiceUrl) ? sap.CentralServiceUrl : site.SapServiceUrl;
        var username = string.IsNullOrWhiteSpace(site.UsernameOverride) ? sap.CentralUsername : site.UsernameOverride;
        var password = string.IsNullOrWhiteSpace(site.PasswordOverride) ? sap.CentralPassword : site.PasswordOverride;
        if (string.IsNullOrWhiteSpace(serviceUrl) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("SAP URL oder Zugangsdaten fehlen.");
        return new PurchasingSapConnection(serviceUrl.TrimEnd('/') + "/", username, password);
    }

    private static HttpClient CreateClient(string username, string password)
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private static async Task<List<Dictionary<string, object?>>> ReadAllRowsAsync(HttpClient client, string baseUrl, string entitySet, string select, string filter, string orderBy, CancellationToken cancellationToken)
    {
        var rows = new List<Dictionary<string, object?>>();
        for (var skip = 0; ; skip += PageSize)
        {
            var url = $"{baseUrl}{entitySet}?$format=json&$top={PageSize}&$skip={skip}&$select={Uri.EscapeDataString(select)}";
            if (!string.IsNullOrWhiteSpace(orderBy))
                url += $"&$orderby={Uri.EscapeDataString(orderBy)}";
            if (!string.IsNullOrWhiteSpace(filter))
                url += $"&$filter={Uri.EscapeDataString(filter)}";

            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException($"SAP OData {entitySet} fehlgeschlagen ({(int)response.StatusCode} {response.ReasonPhrase}) URL={url} Antwort={TrimForLog(error)}");
            }
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var page = ParseRows(json);

            // NOTBREMSE gegen ignoriertes Paging. Mehrere Sets dieses Service ignorieren
            // $top/$skip und liefern immer den Vollbestand (MARCSet, MARA001Set, MAKTSet,
            // mbewSet — live verifiziert). Wird eines davon versehentlich hier durchgeleitet,
            // ist die Abbruchbedingung unten NIE erfuellt und die Schleife laeuft unendlich,
            // ohne Fehler und ohne Timeout, weil jede einzelne Anfrage erfolgreich ist.
            // Genau das hat vom 2026-08-19 bis 2026-08-21 jeden Einkauf-Lauf zum Haengen
            // gebracht (Lagerwert-Read, siehe SapGatewayStockValueReader). Mehr Zeilen als
            // angefordert ist der eindeutige Beweis dafuer, deshalb hier lauter Abbruch
            // statt stiller Endlosschleife.
            if (page.Count > PageSize)
                throw new InvalidOperationException(
                    $"SAP OData {entitySet} ignoriert $top: angefordert {PageSize} Zeilen, " +
                    $"geliefert {page.Count}. Dieses Set darf NICHT gepagt gelesen werden — " +
                    $"stattdessen ein einziger ungepagter Request mit clientseitigem Filter, " +
                    $"wie in SapGatewayPlantMaterialReader. URL={url}");

            if (page.Count == 0)
                return rows;
            rows.AddRange(page);
            if (page.Count < PageSize)
                return rows;
        }
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
                .ToDictionary(property => property.Name, property => ConvertJsonValue(property.Value), StringComparer.OrdinalIgnoreCase))
            .ToList();
    }

    private static async Task UpsertEkkoAsync(SqliteConnection conn, SqliteTransaction transaction, IReadOnlyList<Dictionary<string, object?>> rows, IReadOnlyDictionary<string, SupplierInfo> supplierNameMap, string loadedAtUtc, CancellationToken cancellationToken)
    {
        const string sql = @"
INSERT OR REPLACE INTO PurchasingEkkoCache (Ebeln, Bedat, Aedat, Lifnr, SupplierName, SupplierCountry, Bukrs, Bstyp, Bsart, Konnr, Waers, Wkurs, RawJson, LastLoadedAtUtc)
VALUES ($Ebeln, $Bedat, $Aedat, $Lifnr, $SupplierName, $SupplierCountry, $Bukrs, $Bstyp, $Bsart, $Konnr, $Waers, $Wkurs, $RawJson, $LastLoadedAtUtc);";
        foreach (var row in rows)
            await ExecuteWithParametersAsync(conn, transaction, sql, new()
            {
                ["$Ebeln"] = GetText(row, "Ebeln"),
                ["$Bedat"] = NormalizeSapDate(GetText(row, "Bedat")),
                ["$Aedat"] = NormalizeSapDate(GetText(row, "Aedat")),
                ["$Lifnr"] = GetText(row, "Lifnr"),
                ["$SupplierName"] = ResolveSupplierName(supplierNameMap, GetText(row, "Lifnr"), FirstNonEmpty(GetText(row, "SupplierName"), GetText(row, "Name1"), GetText(row, "Name"))),
                // Lieferantenland aus LFA1.Land1 (Region-Sicht). Leer, wenn LFA1 kein Land liefert.
                ["$SupplierCountry"] = ResolveSupplierCountry(supplierNameMap, GetText(row, "Lifnr")),
                ["$Bukrs"] = GetText(row, "Bukrs"),
                ["$Bstyp"] = GetText(row, "Bstyp"),
                ["$Bsart"] = GetText(row, "Bsart"),
                ["$Konnr"] = GetText(row, "Konnr"),
                ["$Waers"] = GetText(row, "Waers"),
                ["$Wkurs"] = GetText(row, "Wkurs"),
                ["$RawJson"] = JsonSerializer.Serialize(row),
                ["$LastLoadedAtUtc"] = loadedAtUtc
            }, cancellationToken);
    }

    private static async Task UpsertEkpoAsync(SqliteConnection conn, SqliteTransaction transaction, IReadOnlyList<Dictionary<string, object?>> rows, IReadOnlyDictionary<string, MaterialMasterInfo> materialStatusMap, IReadOnlyDictionary<string, MaterialClassification> classificationMap, string loadedAtUtc, CancellationToken cancellationToken)
    {
        const string sql = @"
INSERT OR REPLACE INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Txz01, Matkl, MaraMatkl, MaraAbc, MaraXyz, Maktx, Menge, Meins, Netwr, Loekz, Mstae, Elikz, Ktmng, RawJson, LastLoadedAtUtc)
VALUES ($Ebeln, $Ebelp, $Matnr, $Txz01, $Matkl, $MaraMatkl, $MaraAbc, $MaraXyz, $Maktx, $Menge, $Meins, $Netwr, $Loekz, $Mstae, $Elikz, $Ktmng, $RawJson, $LastLoadedAtUtc);";
        foreach (var row in rows)
            await ExecuteWithParametersAsync(conn, transaction, sql, new()
            {
                ["$Ebeln"] = GetText(row, "Ebeln"),
                ["$Ebelp"] = GetText(row, "Ebelp"),
                ["$Matnr"] = GetText(row, "Matnr"),
                ["$Txz01"] = GetText(row, "Txz01"),
                ["$Matkl"] = GetText(row, "Matkl"),
                // Aktuelle Warengruppe aus dem Materialstamm (Marco: Beleg-Matkl ist in alten
                // Belegen nur die Dummy-Gruppe "01"). Quelle MARA001Set.Matkl (seit 2026-07-23),
                // ueber Matnr gejoint. Im Materialstamm ist Matkl allerdings zu ~65 % leer und
                // ~24 % "01" - wo leer, greift im Dashboard der COALESCE-Fallback auf die
                // Beleg-Warengruppe.
                ["$MaraMatkl"] = ResolveMaterialGroup(materialStatusMap, GetText(row, "Matnr")),
                // ABC (MARC-MAABC, Werk 1100) und XYZ (ZCA_MAT_ABC_XYZ) je Material, ueber Matnr
                // gejoint. Leer, wo nicht klassifiziert.
                ["$MaraAbc"] = ResolveAbc(classificationMap, GetText(row, "Matnr")),
                ["$MaraXyz"] = ResolveXyz(classificationMap, GetText(row, "Matnr")),
                // Materialtext aus MAKT (Deutsch bevorzugt), ueber Matnr gejoint. Leer, wenn das
                // Material keinen Text hat; die Anzeige faellt dann auf die Materialnummer zurueck.
                ["$Maktx"] = ResolveMaterialText(materialStatusMap, GetText(row, "Matnr")),
                ["$Menge"] = GetText(row, "Menge"),
                ["$Meins"] = GetText(row, "Meins"),
                ["$Netwr"] = GetText(row, "Netwr"),
                ["$Loekz"] = GetText(row, "Loekz"),
                ["$Mstae"] = ResolveMaterialStatus(materialStatusMap, GetText(row, "Matnr")),
                ["$Elikz"] = GetText(row, "Elikz"),
                ["$Ktmng"] = GetText(row, "Ktmng"),
                ["$RawJson"] = JsonSerializer.Serialize(row),
                ["$LastLoadedAtUtc"] = loadedAtUtc
            }, cancellationToken);
    }

    /// <summary>
    /// Schreibt die Materialstamm-Attribute (Warengruppe, Materialstatus, ABC, XYZ) auf ALLE Zeilen
    /// im EKPO-Cache, nicht nur auf die im Delta geholten Belege. Liefert die Zahl der tatsaechlich
    /// geaenderten Zeilen.
    ///
    /// WARUM (Befund 2026-07-30): Das naechtliche Delta laedt nur geaenderte (<c>Aedat</c>) und noch
    /// offene Belege. Ein Material, das ausschliesslich auf alten, abgeschlossenen Bestellungen
    /// liegt, behielt damit dauerhaft seine alte Warengruppe - auch nachdem der Einkauf sie im
    /// SAP-Materialstamm nachgepflegt hatte. Genau das ist aber der Dummy-Fall (Warengruppe "01"
    /// oder leer, produktiv 34.6 % aller Bestellpositionen), und Marco wurde in der Sitzung vom
    /// 2026-07-30 zugesagt, dass sich Nachpflege im Dashboard auswirkt ("es wird sich auch immer
    /// aktualisieren, also das ist dann dynamisch"). Ohne diesen Schritt haette es dafuer jedes Mal
    /// einen Full Load gebraucht. Details:
    /// docs/PURCHASING_DASHBOARD_WUENSCHE_EINKAUF_2026-07-30.md Abschnitt 2.
    ///
    /// Kein zusaetzlicher SAP-Read: Beide Maps werden im Delta ohnehin vollstaendig geladen
    /// (<see cref="LoadMaterialMasterMapAsync"/> und <see cref="LoadMaterialClassificationMapAsync"/>
    /// nehmen keine Materialliste als Parameter, es sind dieselben Aufrufe wie im Full Load).
    ///
    /// Gilt seit 2026-08-18 auch fuer den Materialtext (<c>Maktx</c>): ohne diesen Nachzug haetten
    /// Materialien, die nur auf alten abgeschlossenen Bestellungen liegen, dauerhaft keinen Text.
    ///
    /// Umgesetzt ueber eine temporaere Staging-Tabelle und EIN UPDATE statt eines Statements je
    /// Zeile. Die Staging-Tabelle wird bewusst aus den im Cache VORHANDENEN Materialnummern
    /// gebaut (nicht aus den ~68'000 Map-Eintraegen) und die Zielwerte mit denselben
    /// Resolve-Funktionen wie beim Upsert ermittelt - damit ist die Matnr-Normalisierung
    /// garantiert identisch und muss nicht in SQL nachgebaut werden. Die WHERE-Klausel
    /// aktualisiert nur Zeilen, bei denen sich wirklich etwas aendert, sonst wuerden bei jedem
    /// Nachtlauf alle Cachezeilen umgeschrieben.
    /// </summary>
    internal static async Task<int> ApplyMaterialMasterToWholeCacheAsync(
        SqliteConnection conn,
        SqliteTransaction transaction,
        IReadOnlyDictionary<string, MaterialMasterInfo> materialStatusMap,
        IReadOnlyDictionary<string, MaterialClassification> classificationMap,
        CancellationToken cancellationToken)
    {
        // Ohne Stammdaten nichts tun. Sonst wuerde ein fehlgeschlagener/leerer Stammdaten-Read
        // die bereits vorhandenen Warengruppen im Cache flaechendeckend leerschreiben.
        if (materialStatusMap.Count == 0 && classificationMap.Count == 0)
            return 0;

        var cachedMaterials = new List<string>();
        await using (var readCommand = conn.CreateCommand())
        {
            readCommand.Transaction = transaction;
            readCommand.CommandText = "SELECT DISTINCT Matnr FROM PurchasingEkpoCache WHERE COALESCE(Matnr, '') <> '';";
            await using var reader = await readCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                if (!reader.IsDBNull(0))
                    cachedMaterials.Add(reader.GetString(0));
            }
        }

        if (cachedMaterials.Count == 0)
            return 0;

        await ExecuteAsync(conn, transaction, @"
CREATE TEMP TABLE IF NOT EXISTS PurchasingMaterialStaging (
    Matnr TEXT PRIMARY KEY,
    MaraMatkl TEXT NOT NULL DEFAULT '',
    MaraAbc TEXT NOT NULL DEFAULT '',
    MaraXyz TEXT NOT NULL DEFAULT '',
    Maktx TEXT NOT NULL DEFAULT '',
    Mstae TEXT NOT NULL DEFAULT ''
);", cancellationToken);
        // Die TEMP-Tabelle ueberlebt die Verbindung; eine aeltere Fassung ohne Maktx wuerde beim
        // INSERT scheitern. Deshalb bei fehlender Spalte einmalig neu anlegen.
        await EnsureStagingHasMaterialTextColumnAsync(conn, transaction, cancellationToken);
        await ExecuteAsync(conn, transaction, "DELETE FROM PurchasingMaterialStaging;", cancellationToken);

        const string insertSql = @"
INSERT OR REPLACE INTO PurchasingMaterialStaging (Matnr, MaraMatkl, MaraAbc, MaraXyz, Maktx, Mstae)
VALUES ($Matnr, $MaraMatkl, $MaraAbc, $MaraXyz, $Maktx, $Mstae);";
        foreach (var matnr in cachedMaterials)
            await ExecuteWithParametersAsync(conn, transaction, insertSql, new()
            {
                ["$Matnr"] = matnr,
                ["$MaraMatkl"] = ResolveMaterialGroup(materialStatusMap, matnr),
                ["$MaraAbc"] = ResolveAbc(classificationMap, matnr),
                ["$MaraXyz"] = ResolveXyz(classificationMap, matnr),
                ["$Maktx"] = ResolveMaterialText(materialStatusMap, matnr),
                ["$Mstae"] = ResolveMaterialStatus(materialStatusMap, matnr)
            }, cancellationToken);

        // Der Materialtext wird nur angefasst, wenn ueberhaupt einer geladen wurde. Sonst wuerde
        // ein ausgefallener MAKTSet-Read (der bewusst nicht mehr wirft, siehe
        // LoadMaterialTextMapAsync) alle bereits vorhandenen Texte flaechendeckend leeren -
        // dieselbe Schutzlogik wie oben fuer die Stammdaten insgesamt.
        var hasMaterialTexts = materialStatusMap.Values.Any(info => info.Maktx.Length > 0);
        var textAssignment = hasMaterialTexts
            ? "\n    Maktx     = (SELECT s.Maktx     FROM PurchasingMaterialStaging s WHERE s.Matnr = PurchasingEkpoCache.Matnr),"
            : string.Empty;
        var textCondition = hasMaterialTexts
            ? "\n        OR s.Maktx     <> COALESCE(PurchasingEkpoCache.Maktx, '')"
            : string.Empty;

        await using var updateCommand = conn.CreateCommand();
        updateCommand.Transaction = transaction;
        updateCommand.CommandText = @"
UPDATE PurchasingEkpoCache
SET MaraMatkl = (SELECT s.MaraMatkl FROM PurchasingMaterialStaging s WHERE s.Matnr = PurchasingEkpoCache.Matnr),
    MaraAbc   = (SELECT s.MaraAbc   FROM PurchasingMaterialStaging s WHERE s.Matnr = PurchasingEkpoCache.Matnr),
    MaraXyz   = (SELECT s.MaraXyz   FROM PurchasingMaterialStaging s WHERE s.Matnr = PurchasingEkpoCache.Matnr)," + textAssignment + @"
    Mstae     = (SELECT s.Mstae     FROM PurchasingMaterialStaging s WHERE s.Matnr = PurchasingEkpoCache.Matnr)
WHERE EXISTS (
    SELECT 1 FROM PurchasingMaterialStaging s
    WHERE s.Matnr = PurchasingEkpoCache.Matnr
      AND (s.MaraMatkl <> COALESCE(PurchasingEkpoCache.MaraMatkl, '')
        OR s.MaraAbc   <> COALESCE(PurchasingEkpoCache.MaraAbc, '')
        OR s.MaraXyz   <> COALESCE(PurchasingEkpoCache.MaraXyz, '')" + textCondition + @"
        OR s.Mstae     <> COALESCE(PurchasingEkpoCache.Mstae, '')));";
        return await updateCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Ergaenzt die TEMP-Staging-Tabelle um <c>Maktx</c>, falls sie in derselben Verbindung noch in
    /// der alten Form (ohne Materialtext) angelegt wurde. <c>CREATE TEMP TABLE IF NOT EXISTS</c>
    /// wuerde eine bestehende Tabelle sonst unveraendert lassen.
    /// </summary>
    private static async Task EnsureStagingHasMaterialTextColumnAsync(
        SqliteConnection conn,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var check = conn.CreateCommand();
        check.Transaction = transaction;
        check.CommandText = "SELECT COUNT(*) FROM pragma_table_info('PurchasingMaterialStaging') WHERE name = 'Maktx';";
        var exists = Convert.ToInt32(await check.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture) > 0;
        if (!exists)
            await ExecuteAsync(conn, transaction, "ALTER TABLE PurchasingMaterialStaging ADD COLUMN Maktx TEXT NOT NULL DEFAULT '';", cancellationToken);
    }

    private async Task<Dictionary<string, MaterialMasterInfo>> LoadMaterialMasterMapAsync(HttpClient client, string baseUrl, CancellationToken cancellationToken)
    {
        // Materialstamm-Attribute je Material, ueber EKPO.Matnr -> MARA.Matnr in den EKPO-Cache
        // uebernommen: Mstae (Materialstatus, fuer MSTAE-98/99-Filter), Matkl (aktuelle
        // Warengruppe aus dem Materialstamm, Wunsch Marco - Beleg-Matkl ist in alten Belegen nur
        // die Dummy-Gruppe "01") und Maktx (Materialtext, siehe LoadMaterialTextMapAsync).
        //
        // HISTORIE der Quelle:
        //  - bis 2026-07-17: MARA001Set (hatte Mstae).
        //  - 2026-07-17: SAP hatte Mstae aus MARA001Set entfernt ($select=Mstae -> 404),
        //    deshalb Umstellung auf maracalcSet (hatte Mstae, aber kein Matkl).
        //  - 2026-07-23: SAP hat MARA001Set um Matkl UND Mstae erweitert (Ingo). MARA001Set hat
        //    jetzt beide Felder in einem Set -> zurueck auf MARA001Set, maracalcSet nicht mehr
        //    noetig. Live verifiziert: MARA001Set ignoriert $top/$skip/$filter (liefert immer alle
        //    ~68'125 Zeilen, gleiches Verhalten wie maracalcSet/mbewSet), deshalb bewusst EIN
        //    ungepagter Request statt ReadAllRowsAsync — Paging wuerde sonst bei jedem "Blatt" den
        //    vollen Bestand erneut laden.
        var url = $"{baseUrl}MARA001Set?$format=json&$select={Uri.EscapeDataString("Matnr,Mstae,Matkl")}";
        using var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"SAP OData MARA001Set fehlgeschlagen ({(int)response.StatusCode} {response.ReasonPhrase}) URL={url} Antwort={TrimForLog(error)}");
        }

        var rows = ParseRows(await response.Content.ReadAsStringAsync(cancellationToken));
        var map = new Dictionary<string, MaterialMasterInfo>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            var key = NormalizeMatnr(GetText(row, "Matnr"));
            if (key.Length == 0)
                continue;
            map[key] = new MaterialMasterInfo(GetText(row, "Mstae"), GetText(row, "Matkl"));
        }

        // Materialtexte aus einer ZWEITEN Quelle (MAKT) in dieselbe Map mischen, damit die
        // nachgelagerten Aufrufer (Upsert und Cache-Nachzug) unveraendert mit einer Map arbeiten.
        var textMap = await LoadMaterialTextMapAsync(client, baseUrl, cancellationToken);
        foreach (var (key, text) in textMap)
        {
            map[key] = map.TryGetValue(key, out var info)
                ? info with { Maktx = text }
                // Text ohne MARA-Satz ist praktisch ausgeschlossen (MAKT haengt an MARA), wird
                // aber trotzdem uebernommen statt still verworfen.
                : new MaterialMasterInfo(string.Empty, string.Empty, text);
        }

        return map;
    }

    /// <summary>
    /// Materialtexte je Material aus <c>MAKTSet</c> (SAP-Tabelle MAKT), genau EIN Text je
    /// Materialnummer.
    ///
    /// WARUM EIN EIGENER READ: MARA fuehrt keinen Text. Der Materialtext liegt sprachabhaengig in
    /// MAKT mit dem Schluessel MATNR + SPRAS, deshalb ein zweiter Request statt einer Erweiterung
    /// des MARA-Selects.
    ///
    /// WARUM DER SPRACHFILTER PFLICHT IST (Messung 2026-08-18 auf T76/100, Report
    /// docs/abap/Z_PURCHASING_MAKTX_ANALYSE.abap): Zu den 3'682 Einkaufsmaterialien gibt es 6'390
    /// MAKT-Zeilen; 1'425 Materialien (rund 39 %) sind mehrsprachig gepflegt
    /// (DE 3'681, EN 1'426, FR 1'275, IT 8). MAKTSet liefert alle Sprachen. Wuerde man je Zeile in
    /// die Map schreiben, gaebe es zwar keine Zeilenvervielfachung (Dictionary), aber der
    /// angezeigte Text haenge von der Lesereihenfolge ab. Deshalb wird je Material deterministisch
    /// EIN Text gewaehlt: Deutsch, sonst Englisch, sonst die erste gelieferte Sprache. Deutsch ist
    /// mit 3'681 von 3'682 praktisch vollstaendig; der Rueckfall greift fuer genau ein Material.
    ///
    /// PAGING: MAKTSet ignoriert $top/$skip und liefert immer den Vollbestand (am 2026-08-18 an
    /// travp762 bestaetigt) - dasselbe Verhalten wie MARA001Set. Deshalb bewusst EIN ungepagter
    /// Request statt <see cref="ReadAllRowsAsync"/>; Paging wuerde bei jedem "Blatt" den vollen
    /// Bestand erneut laden.
    /// </summary>
    private async Task<Dictionary<string, string>> LoadMaterialTextMapAsync(HttpClient client, string baseUrl, CancellationToken cancellationToken)
    {
        // BEWUSST NICHT WERFEND, anders als die uebrigen Reads. Der Materialtext ist ein reines
        // Anzeigefeld; ein Problem daran darf den Einkauf-Lauf nicht abbrechen. Genau dieser Fall
        // hat schon einmal zwei Wochen Datenstillstand gekostet: Am 2026-07-02 lief der Full Load
        // in einen MARA001Set-404 und brach ab, BEVOR er LFA1 laden konnte - bis zum 2026-07-17
        // blieb dadurch der Stand vom 07.06. aktiv (siehe Nachtrag 2026-07-17 in
        // docs/PURCHASING_DASHBOARD_2026-06-05.md). Faellt MAKTSet aus, laeuft der Rest weiter und
        // die Materialebene zeigt so lange nur die Nummer.
        var url = $"{baseUrl}MAKTSet?$format=json&$select={Uri.EscapeDataString("Matnr,Spras,Maktx")}";
        try
        {
            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                await _logService.WriteAsync(
                    "Purchasing",
                    "Materialtexte konnten nicht geladen werden - Lauf geht ohne Text weiter",
                    "Warning",
                    details: $"MAKTSet {(int)response.StatusCode} {response.ReasonPhrase} URL={url} Antwort={TrimForLog(error)}");
                return [];
            }

            return SelectMaterialTexts(ParseRows(await response.Content.ReadAsStringAsync(cancellationToken)));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await _logService.WriteAsync(
                "Purchasing",
                "Materialtexte konnten nicht geladen werden - Lauf geht ohne Text weiter",
                "Warning",
                details: $"MAKTSet URL={url} Fehler={ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// Waehlt aus den MAKT-Zeilen je Materialnummer GENAU EINEN Text: Deutsch, sonst Englisch,
    /// sonst die erste gelieferte Sprache. Bei gleichrangigen Sprachen bleibt der zuerst gelesene
    /// Text stehen, damit das Ergebnis nicht von der Antwortreihenfolge abhaengt.
    /// Getrennt von <see cref="LoadMaterialTextMapAsync"/>, damit die Auswahl ohne SAP testbar ist.
    /// </summary>
    internal static Dictionary<string, string> SelectMaterialTexts(IEnumerable<Dictionary<string, object?>> rows)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        var chosenRank = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            var key = NormalizeMatnr(GetText(row, "Matnr"));
            if (key.Length == 0)
                continue;

            var text = GetText(row, "Maktx").Trim();
            if (text.Length == 0)
                continue;

            // Kleinerer Rang gewinnt.
            var rank = LanguageRank(GetText(row, "Spras"));
            if (chosenRank.TryGetValue(key, out var current) && current <= rank)
                continue;

            map[key] = text;
            chosenRank[key] = rank;
        }

        return map;
    }

    /// <summary>
    /// Sprachrang fuer die Textauswahl: Deutsch vor Englisch vor allem anderen. SAP liefert den
    /// Sprachschluessel je nach Service als ISO-Code ("DE") oder als einstelliges SAP-Kennzeichen
    /// ("D"), deshalb werden beide Schreibweisen akzeptiert.
    /// </summary>
    private static int LanguageRank(string spras) => spras.Trim().ToUpperInvariant() switch
    {
        "DE" or "D" => 0,
        "EN" or "E" => 1,
        _ => 2
    };

    internal sealed record MaterialMasterInfo(string Mstae, string Matkl, string Maktx = "");

    private static string ResolveMaterialStatus(IReadOnlyDictionary<string, MaterialMasterInfo> materialStatusMap, string matnr)
    {
        var key = NormalizeMatnr(matnr);
        return key.Length > 0 && materialStatusMap.TryGetValue(key, out var info) ? info.Mstae : string.Empty;
    }

    private static string ResolveMaterialGroup(IReadOnlyDictionary<string, MaterialMasterInfo> materialStatusMap, string matnr)
    {
        var key = NormalizeMatnr(matnr);
        return key.Length > 0 && materialStatusMap.TryGetValue(key, out var info) ? info.Matkl : string.Empty;
    }

    private static string ResolveMaterialText(IReadOnlyDictionary<string, MaterialMasterInfo> materialStatusMap, string matnr)
    {
        var key = NormalizeMatnr(matnr);
        return key.Length > 0 && materialStatusMap.TryGetValue(key, out var info) ? info.Maktx : string.Empty;
    }

    private static string NormalizeMatnr(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        var normalized = new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();
        var trimmed = normalized.TrimStart('0');
        return trimmed.Length == 0 ? normalized : trimmed;
    }

    private async Task<Dictionary<string, SupplierInfo>> LoadSupplierNameMapAsync(HttpClient client, string baseUrl, CancellationToken cancellationToken)
    {
        // LFA1 (Lieferantenstamm) liefert Lieferantenname UND Lieferantenland je Lieferanten-
        // nummer. Wird ueber EKKO.Lifnr -> LFA1.Lifnr in PurchasingEkkoCache.SupplierName /
        // SupplierCountry uebernommen. Land1 seit SAP-Erweiterung 2026-07-23 (fuer Region-Sicht).
        var rows = await ReadAllRowsAsync(client, baseUrl, "LFA1Set", "Lifnr,Name1,Land1", string.Empty, "Lifnr", cancellationToken);
        var map = new Dictionary<string, SupplierInfo>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            var key = NormalizeLifnr(GetText(row, "Lifnr"));
            if (key.Length == 0)
                continue;
            map[key] = new SupplierInfo(GetText(row, "Name1"), GetText(row, "Land1"));
        }

        return map;
    }

    internal sealed record SupplierInfo(string Name, string Country);

    private static string ResolveSupplierName(IReadOnlyDictionary<string, SupplierInfo> supplierNameMap, string lifnr, string fallback)
    {
        var key = NormalizeLifnr(lifnr);
        return key.Length > 0 && supplierNameMap.TryGetValue(key, out var info) && !string.IsNullOrWhiteSpace(info.Name)
            ? info.Name
            : fallback;
    }

    private static string ResolveSupplierCountry(IReadOnlyDictionary<string, SupplierInfo> supplierNameMap, string lifnr)
    {
        var key = NormalizeLifnr(lifnr);
        return key.Length > 0 && supplierNameMap.TryGetValue(key, out var info) ? info.Country : string.Empty;
    }

    /// <summary>
    /// Laedt je Material die ABC- (MARC-MAABC, Werk 1100) und XYZ-Klassifizierung
    /// (ZSTR_MAT_XYZSet -> ZCA_MAT_ABC_XYZ./ITS/CA_M_MAXYZ). Beides SAP-Erweiterung 2026-07-23.
    /// ABC ist SAP-Standard, XYZ ein /ITS/-Add-on. Schluessel ist die normalisierte Materialnummer.
    /// MARCSet liefert alle Werke -> auf 1100 filtern (dort sind die Trafag-AG-Werte gepflegt).
    /// Das XYZ-Set enthaelt nur die klassifizierten Materialien (kuratierte Teilmenge).
    /// </summary>
    private async Task<Dictionary<string, MaterialClassification>> LoadMaterialClassificationMapAsync(HttpClient client, string baseUrl, CancellationToken cancellationToken)
    {
        var map = new Dictionary<string, MaterialClassification>(StringComparer.Ordinal);

        // ABC aus MARCSet. ACHTUNG: MARCSet ignoriert $top/$skip (liefert immer alle ~68'559
        // Zeilen, live verifiziert 2026-07-23) - deshalb EIN ungepagter Request statt
        // ReadAllRowsAsync (das wuerde endlos paging-loopen). Werk 1100 wird client-seitig
        // gefiltert, weil das Set den serverseitigen $filter nicht zuverlaessig anwendet.
        var abcUrl = $"{baseUrl}MARCSet?$format=json&$select={Uri.EscapeDataString("Matnr,Werks,Maabc")}";
        using (var abcResponse = await client.GetAsync(abcUrl, cancellationToken))
        {
            if (!abcResponse.IsSuccessStatusCode)
            {
                var error = await abcResponse.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException($"SAP OData MARCSet fehlgeschlagen ({(int)abcResponse.StatusCode} {abcResponse.ReasonPhrase}) URL={abcUrl} Antwort={TrimForLog(error)}");
            }

            foreach (var row in ParseRows(await abcResponse.Content.ReadAsStringAsync(cancellationToken)))
            {
                if (GetText(row, "Werks") != "1100")
                    continue;
                var key = NormalizeMatnr(GetText(row, "Matnr"));
                if (key.Length == 0)
                    continue;
                map[key] = map.TryGetValue(key, out var existing)
                    ? existing with { Abc = GetText(row, "Maabc") }
                    : new MaterialClassification(GetText(row, "Maabc"), string.Empty);
            }
        }

        // XYZ aus dem eigenen Set (Methodenrumpf ZSTR_MAT_XYZ, honoriert $top/$skip/$filter) -
        // ReadAllRowsAsync ist hier korrekt.
        var xyzRows = await ReadAllRowsAsync(client, baseUrl, "ZSTR_MAT_XYZSet", "Matnr,Werks,Maxyz", "Werks eq '1100'", "Matnr", cancellationToken);
        foreach (var row in xyzRows)
        {
            var key = NormalizeMatnr(GetText(row, "Matnr"));
            if (key.Length == 0)
                continue;
            map[key] = map.TryGetValue(key, out var existing)
                ? existing with { Xyz = GetText(row, "Maxyz") }
                : new MaterialClassification(string.Empty, GetText(row, "Maxyz"));
        }

        return map;
    }

    internal sealed record MaterialClassification(string Abc, string Xyz);

    private static string ResolveAbc(IReadOnlyDictionary<string, MaterialClassification> map, string matnr)
    {
        var key = NormalizeMatnr(matnr);
        return key.Length > 0 && map.TryGetValue(key, out var c) ? c.Abc : string.Empty;
    }

    private static string ResolveXyz(IReadOnlyDictionary<string, MaterialClassification> map, string matnr)
    {
        var key = NormalizeMatnr(matnr);
        return key.Length > 0 && map.TryGetValue(key, out var c) ? c.Xyz : string.Empty;
    }

    private static string NormalizeLifnr(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        var normalized = new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();
        var trimmed = normalized.TrimStart('0');
        return trimmed.Length == 0 ? normalized : trimmed;
    }

    private static async Task UpsertEketAsync(SqliteConnection conn, SqliteTransaction transaction, IReadOnlyList<Dictionary<string, object?>> rows, string loadedAtUtc, CancellationToken cancellationToken)
    {
        const string sql = @"
INSERT OR REPLACE INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, RawJson, LastLoadedAtUtc)
VALUES ($Ebeln, $Ebelp, $Etenr, $Eindt, $Menge, $Wemng, $RawJson, $LastLoadedAtUtc);";
        foreach (var row in rows)
            await ExecuteWithParametersAsync(conn, transaction, sql, new()
            {
                ["$Ebeln"] = GetText(row, "Ebeln"),
                ["$Ebelp"] = GetText(row, "Ebelp"),
                ["$Etenr"] = GetText(row, "Etenr"),
                ["$Eindt"] = NormalizeSapDate(GetText(row, "Eindt")),
                ["$Menge"] = GetText(row, "Menge"),
                ["$Wemng"] = GetText(row, "Wemng"),
                ["$RawJson"] = JsonSerializer.Serialize(row),
                ["$LastLoadedAtUtc"] = loadedAtUtc
            }, cancellationToken);
    }

    private static async Task ReplaceProductGroupRulesAsync(
        SqliteConnection conn,
        SqliteTransaction transaction,
        PurchasingProductGroupSapResult result,
        string loadedAtUtc,
        CancellationToken cancellationToken)
    {
        // Erst nachdem SAP eine nicht-leere, validierte Liste geliefert hat, wird der alte
        // Cache innerhalb derselben Transaktion ersetzt. Ein fehlendes/defektes EntitySet kann
        // dadurch weder eine leere Produktgruppensicht noch einen Excel-Rueckfall erzeugen.
        await ExecuteAsync(conn, transaction, "DELETE FROM PurchasingSpendDisponentRule;", cancellationToken);
        const string sql = @"
INSERT INTO PurchasingSpendDisponentRule
    (DisponentPattern, ProductGroup, ProductGroupText, Source, UpdatedAtUtc)
VALUES
    ($DisponentPattern, $ProductGroup, $ProductGroupText, $Source, $UpdatedAtUtc);";
        foreach (var rule in result.Rules)
        {
            await ExecuteWithParametersAsync(conn, transaction, sql, new()
            {
                ["$DisponentPattern"] = rule.DisponentPattern,
                ["$ProductGroup"] = rule.ProductGroup,
                ["$ProductGroupText"] = rule.ProductGroupText,
                ["$Source"] = $"SAP OData: {result.SourceEntitySets}",
                ["$UpdatedAtUtc"] = loadedAtUtc
            }, cancellationToken);
        }
    }

    private async Task WriteStatusAsync(string mode, string status, DateTime? startedAtUtc, DateTime? completedAtUtc, DateTime? fromDate, DateTime? toDate, DateTime? lastSuccessfulDeltaAtUtc, int ekkoRows, int ekpoRows, int eketRows, string message, CancellationToken cancellationToken)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var conn = (SqliteConnection)db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(cancellationToken);

        // Endstatus (Success/Error) schliesst die eigene Running-Zeile ab, statt eine zweite Zeile
        // anzulegen. BEFUND 2026-09-28: jeder Lauf hinterliess zwei Zeilen mit gleichem Start, und
        // die Running-Zeile wurde beim naechsten Anwendungsstart als "Abgebrochen" markiert
        // (z. B. Id 48/49). Die Tabelle sah dadurch nach vielen Abbruechen aus, die es nie gab.
        // Findet sich keine passende Running-Zeile, wird wie bisher eine neue geschrieben.
        const string updateSql = @"
UPDATE PurchasingSyncState
SET Status = $Status, CompletedAtUtc = $CompletedAtUtc, FromDate = $FromDate, ToDate = $ToDate,
    LastSuccessfulDeltaAtUtc = $LastSuccessfulDeltaAtUtc, EkkoRows = $EkkoRows, EkpoRows = $EkpoRows,
    EketRows = $EketRows, Message = $Message
WHERE Id = (
    SELECT Id FROM PurchasingSyncState
    WHERE Mode = $Mode AND Status = 'Running' AND StartedAtUtc = $StartedAtUtc
    ORDER BY Id DESC LIMIT 1);";
        const string insertSql = @"
INSERT INTO PurchasingSyncState (Mode, Status, StartedAtUtc, CompletedAtUtc, FromDate, ToDate, LastSuccessfulDeltaAtUtc, EkkoRows, EkpoRows, EketRows, Message)
VALUES ($Mode, $Status, $StartedAtUtc, $CompletedAtUtc, $FromDate, $ToDate, $LastSuccessfulDeltaAtUtc, $EkkoRows, $EkpoRows, $EketRows, $Message);";

        if (!string.Equals(status, "Running", StringComparison.OrdinalIgnoreCase))
        {
            await using var update = conn.CreateCommand();
            update.CommandText = updateSql;
            foreach (var (name, value) in BuildStatusParameters())
                update.Parameters.AddWithValue(name, value ?? DBNull.Value);
            if (await update.ExecuteNonQueryAsync(cancellationToken) > 0)
                return;
        }

        await ExecuteWithParametersAsync(conn, null, insertSql, BuildStatusParameters(), cancellationToken);

        Dictionary<string, object?> BuildStatusParameters() => new()
        {
            ["$Mode"] = mode,
            ["$Status"] = status,
            ["$StartedAtUtc"] = FormatDateTime(startedAtUtc),
            ["$CompletedAtUtc"] = FormatDateTime(completedAtUtc),
            ["$FromDate"] = FormatDate(fromDate),
            ["$ToDate"] = FormatDate(toDate),
            ["$LastSuccessfulDeltaAtUtc"] = FormatDateTime(lastSuccessfulDeltaAtUtc),
            ["$EkkoRows"] = ekkoRows,
            ["$EkpoRows"] = ekpoRows,
            ["$EketRows"] = eketRows,
            ["$Message"] = message
        };
    }

    private static async Task<PurchasingDataRefreshStatus> ReadLatestStatusAsync(SqliteConnection conn, CancellationToken cancellationToken)
    {
        await using var command = conn.CreateCommand();
        command.CommandText = @"
SELECT Mode, Status, StartedAtUtc, CompletedAtUtc, FromDate, ToDate, LastSuccessfulDeltaAtUtc, EkkoRows, EkpoRows, EketRows, Message
FROM PurchasingSyncState
ORDER BY Id DESC
LIMIT 1;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return new PurchasingDataRefreshStatus { Status = "Empty", Message = "Noch kein Einkauf Full Load ausgefuehrt." };

        return new PurchasingDataRefreshStatus
        {
            Mode = reader.GetString(0),
            Status = reader.GetString(1),
            StartedAtUtc = ParseDateTime(reader.GetString(2)),
            CompletedAtUtc = ParseDateTime(reader.GetString(3)),
            FromDate = ParseDate(reader.GetString(4)),
            ToDate = ParseDate(reader.GetString(5)),
            LastSuccessfulDeltaAtUtc = ParseDateTime(reader.GetString(6)),
            EkkoRows = reader.GetInt32(7),
            EkpoRows = reader.GetInt32(8),
            EketRows = reader.GetInt32(9),
            Message = reader.GetString(10)
        };
    }

    private static async Task<int> CountTableAsync(SqliteConnection conn, string tableName, CancellationToken cancellationToken)
    {
        await using var command = conn.CreateCommand();
        command.CommandText = $"SELECT COUNT(1) FROM {tableName};";
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
    }

    private static async Task ExecuteAsync(SqliteConnection conn, SqliteTransaction transaction, string sql, CancellationToken cancellationToken)
    {
        await using var command = conn.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ExecuteWithParametersAsync(SqliteConnection conn, SqliteTransaction? transaction, string sql, Dictionary<string, object?> parameters, CancellationToken cancellationToken)
    {
        await using var command = conn.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var (key, value) in parameters)
            command.Parameters.AddWithValue(key, value ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
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

    private static string FirstNonEmpty(params string[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

    private static string TrimForLog(string value)
        => value.Length <= 1000 ? value : value[..1000] + "...";

    private static string? NormalizeSapDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
            return parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return DateTime.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed)
            ? parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : value;
    }

    private static string FormatDateTime(DateTime? value)
        => value?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty;

    private static string FormatDate(DateTime? value)
        => value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;

    private static DateTime? ParseDateTime(string value)
        => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed) ? parsed : null;

    private static DateTime? ParseDate(string value)
        => DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ? parsed : null;

    private sealed record PurchasingSapConnection(string BaseUrl, string Username, string Password);
}
