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

    public PurchasingDataRefreshService(IDbContextFactory<AppDbContext> dbFactory, IAppEventLogService logService)
    {
        _dbFactory = dbFactory;
        _logService = logService;
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
            var materialStatusMap = await LoadMaterialStatusMapAsync(client, connection.BaseUrl, cancellationToken);
            var classificationMap = await LoadMaterialClassificationMapAsync(client, connection.BaseUrl, cancellationToken);
            var supplierNameMap = await LoadSupplierNameMapAsync(client, connection.BaseUrl, cancellationToken);

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
            await transaction.CommitAsync(cancellationToken);

            var completed = DateTime.UtcNow;
            var message = $"Full Load abgeschlossen: EKKO={ekkoRows.Count:N0}, EKPO={ekpoRows.Count:N0}, EKET={eketRows.Count:N0}, MARA-Status={materialStatusMap.Count:N0}, Klassifizierung={classificationMap.Count:N0}, LFA1-Namen={supplierNameMap.Count:N0}.";
            await WriteStatusAsync("Full", "Success", started, completed, fromDate, null, completed, ekkoRows.Count, ekpoRows.Count, eketRows.Count, message, cancellationToken);
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

            var materialStatusMap = await LoadMaterialStatusMapAsync(client, connection.BaseUrl, cancellationToken);
            var classificationMap = await LoadMaterialClassificationMapAsync(client, connection.BaseUrl, cancellationToken);
            var supplierNameMap = await LoadSupplierNameMapAsync(client, connection.BaseUrl, cancellationToken);

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
            await transaction.CommitAsync(cancellationToken);

            var completed = DateTime.UtcNow;
            var status = await GetStatusAsync(cancellationToken);
            var message = $"Delta abgeschlossen: geaenderte Belege={changedEbelns.Count:N0}, offene Belege nachgeladen={openEbelns.Count:N0}, Belege gesamt={ebelnKeys.Count:N0}, EKPO={ekpoRows.Count:N0}, EKET={eketRows.Count:N0}, Stammdaten aktualisiert auf={reclassifiedRows:N0} Cachezeilen.";
            await WriteStatusAsync("Delta", "Success", started, completed, deltaFrom, null, completed, status.EkkoRows, status.EkpoRows, status.EketRows, message, cancellationToken);
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
INSERT OR REPLACE INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Txz01, Matkl, MaraMatkl, MaraAbc, MaraXyz, Menge, Meins, Netwr, Loekz, Mstae, Elikz, Ktmng, RawJson, LastLoadedAtUtc)
VALUES ($Ebeln, $Ebelp, $Matnr, $Txz01, $Matkl, $MaraMatkl, $MaraAbc, $MaraXyz, $Menge, $Meins, $Netwr, $Loekz, $Mstae, $Elikz, $Ktmng, $RawJson, $LastLoadedAtUtc);";
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
    /// (<see cref="LoadMaterialStatusMapAsync"/> und <see cref="LoadMaterialClassificationMapAsync"/>
    /// nehmen keine Materialliste als Parameter, es sind dieselben Aufrufe wie im Full Load).
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
    Mstae TEXT NOT NULL DEFAULT ''
);", cancellationToken);
        await ExecuteAsync(conn, transaction, "DELETE FROM PurchasingMaterialStaging;", cancellationToken);

        const string insertSql = @"
INSERT OR REPLACE INTO PurchasingMaterialStaging (Matnr, MaraMatkl, MaraAbc, MaraXyz, Mstae)
VALUES ($Matnr, $MaraMatkl, $MaraAbc, $MaraXyz, $Mstae);";
        foreach (var matnr in cachedMaterials)
            await ExecuteWithParametersAsync(conn, transaction, insertSql, new()
            {
                ["$Matnr"] = matnr,
                ["$MaraMatkl"] = ResolveMaterialGroup(materialStatusMap, matnr),
                ["$MaraAbc"] = ResolveAbc(classificationMap, matnr),
                ["$MaraXyz"] = ResolveXyz(classificationMap, matnr),
                ["$Mstae"] = ResolveMaterialStatus(materialStatusMap, matnr)
            }, cancellationToken);

        await using var updateCommand = conn.CreateCommand();
        updateCommand.Transaction = transaction;
        updateCommand.CommandText = @"
UPDATE PurchasingEkpoCache
SET MaraMatkl = (SELECT s.MaraMatkl FROM PurchasingMaterialStaging s WHERE s.Matnr = PurchasingEkpoCache.Matnr),
    MaraAbc   = (SELECT s.MaraAbc   FROM PurchasingMaterialStaging s WHERE s.Matnr = PurchasingEkpoCache.Matnr),
    MaraXyz   = (SELECT s.MaraXyz   FROM PurchasingMaterialStaging s WHERE s.Matnr = PurchasingEkpoCache.Matnr),
    Mstae     = (SELECT s.Mstae     FROM PurchasingMaterialStaging s WHERE s.Matnr = PurchasingEkpoCache.Matnr)
WHERE EXISTS (
    SELECT 1 FROM PurchasingMaterialStaging s
    WHERE s.Matnr = PurchasingEkpoCache.Matnr
      AND (s.MaraMatkl <> COALESCE(PurchasingEkpoCache.MaraMatkl, '')
        OR s.MaraAbc   <> COALESCE(PurchasingEkpoCache.MaraAbc, '')
        OR s.MaraXyz   <> COALESCE(PurchasingEkpoCache.MaraXyz, '')
        OR s.Mstae     <> COALESCE(PurchasingEkpoCache.Mstae, '')));";
        return await updateCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<Dictionary<string, MaterialMasterInfo>> LoadMaterialStatusMapAsync(HttpClient client, string baseUrl, CancellationToken cancellationToken)
    {
        // Materialstamm-Attribute je Material, ueber EKPO.Matnr -> MARA.Matnr in den EKPO-Cache
        // uebernommen: Mstae (Materialstatus, fuer MSTAE-98/99-Filter) und Matkl (aktuelle
        // Warengruppe aus dem Materialstamm, Wunsch Marco - Beleg-Matkl ist in alten Belegen nur
        // die Dummy-Gruppe "01").
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

        return map;
    }

    internal sealed record MaterialMasterInfo(string Mstae, string Matkl);

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

    private async Task WriteStatusAsync(string mode, string status, DateTime? startedAtUtc, DateTime? completedAtUtc, DateTime? fromDate, DateTime? toDate, DateTime? lastSuccessfulDeltaAtUtc, int ekkoRows, int ekpoRows, int eketRows, string message, CancellationToken cancellationToken)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var conn = (SqliteConnection)db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(cancellationToken);
        const string sql = @"
INSERT INTO PurchasingSyncState (Mode, Status, StartedAtUtc, CompletedAtUtc, FromDate, ToDate, LastSuccessfulDeltaAtUtc, EkkoRows, EkpoRows, EketRows, Message)
VALUES ($Mode, $Status, $StartedAtUtc, $CompletedAtUtc, $FromDate, $ToDate, $LastSuccessfulDeltaAtUtc, $EkkoRows, $EkpoRows, $EketRows, $Message);";
        await ExecuteWithParametersAsync(conn, null, sql, new()
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
        }, cancellationToken);
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
