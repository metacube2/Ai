using System.Data;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Laedt MaterialUsageSet/MaterialParentSet - siehe docs/abap/README_LZCODE_WEBSERVICE.md und
/// docs/abap/ZCL_LZCODE_PROVIDER.abap. Diese EntitySets existieren im Gateway-Service
/// ZPOWERBI_EINKAUF_SRV noch NICHT (Entwurf fuer Lucas); dieser Dienst prueft das per
/// Metadaten-Abfrage und meldet fachlich klar, wenn sie fehlen, statt einen rohen HTTP-Fehler
/// zu zeigen (gleiches Muster wie FinancialJournalRefreshService fuer FinanzJournalSet).
/// Nutzt dieselbe SAP-Verbindung wie der Einkauf-Full-Load (Site PURCHASING_SAP), weil beide
/// EntitySets am selben Gateway-Service haengen.
/// </summary>
public sealed class MaterialUsageDataRefreshService : IMaterialUsageDataRefreshService
{
    private const int PageSize = 1000;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ISapGatewayService _sapGatewayService;
    private readonly IAppEventLogService _logService;

    public MaterialUsageDataRefreshService(
        IDbContextFactory<AppDbContext> dbFactory,
        ISapGatewayService sapGatewayService,
        IAppEventLogService logService)
    {
        _dbFactory = dbFactory;
        _sapGatewayService = sapGatewayService;
        _logService = logService;
    }

    public async Task<MaterialUsageRefreshStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var conn = (SqliteConnection)db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(cancellationToken);

        return await ReadLatestStatusAsync(conn, cancellationToken);
    }

    public async Task<MaterialUsageRefreshStatus> RunFullLoadAsync(bool topDown = true, string? materialFilter = null, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var started = DateTime.UtcNow;
        await WriteStatusAsync("Running", started, null, 0, 0, "Full Load gestartet.", cancellationToken);
        await _logService.WriteAsync("MaterialUsage", "Stuecklistenanalyse Full Load gestartet",
            details: (topDown ? "Richtung=TOPDOWN" : "Richtung=BOTTOMUP") +
                     (includeDeleted ? " | InklGeloescht=true" : string.Empty) +
                     (string.IsNullOrWhiteSpace(materialFilter) ? string.Empty : $" | Materialfilter={materialFilter}"));

        try
        {
            var connection = await ResolveConnectionAsync(cancellationToken);

            // EntitySet-Namen dynamisch aufloesen: SEGW hat die Sets nach den DDIC-Strukturen
            // benannt (ZSTR_LZCODE_USAGE..., exakte Schreibweise inkl. Set-Suffix/Kuerzung je
            // nach Anlage), nicht nach dem urspruenglichen Doku-Vorschlag MaterialUsageSet.
            // Der normalisierte Vergleich (ohne Unterstriche, case-insensitiv) findet beide
            // Namenswelten, ohne dass hier je nach SEGW-Stand nachgezogen werden muss.
            var entitySets = await _sapGatewayService.GetEntitySetsAsync(connection.BaseUrl, connection.Username, connection.Password, cancellationToken);
            var usageSetName = ResolveEntitySetName(entitySets, "lzcodeusage", "materialusage");
            var parentSetName = ResolveEntitySetName(entitySets, "lzcodeparent", "materialparent");
            if (usageSetName is null || parentSetName is null)
            {
                var missingMessage = "SAP-Gateway-Service liefert (noch) kein passendes EntitySet fuer die Stuecklistenanalyse " +
                    $"(gesucht: Name enthaelt LZCODE_USAGE/LZCODE_PARENT oder MaterialUsage/MaterialParent; verfuegbar: {entitySets.Count} Sets). " +
                    "SEGW-Anlage laut docs/abap/README_LZCODE_WEBSERVICE.md pruefen, ggf. Gateway-Metadaten-Cache leeren. Kein Datenschaden, kein Retry noetig.";
                await WriteStatusAsync("Error", started, DateTime.UtcNow, 0, 0, missingMessage, cancellationToken);
                await _logService.WriteAsync("MaterialUsage", "Stuecklistenanalyse Full Load uebersprungen - EntitySet fehlt", "Warning", details: missingMessage);
                return await GetStatusAsync(cancellationToken);
            }

            // Die SAP-Seite ERZWINGT einen Vknr-/Kompnr-Filter (Guard gegen versehentliche
            // Vollselektion auf MARA, analog Report-Meldung "Bitte Selektion eingeben").
            // Ein Full Load ohne konkrete Materialliste schickt deshalb bewusst den
            // Catch-all "gt ''" mit - das ist die explizite "ja, wirklich alles"-Ansage.
            var materialProperty = topDown ? "Vknr" : "Kompnr";
            var selections = BuildMaterialClauses(materialProperty, materialFilter);
            var sapRichtung = BuildRichtungValue(topDown, includeDeleted);

            using var client = CreateClient(connection.Username, connection.Password);

            // Eine Anfrage JE Materialnummer - Begruendung in BuildMaterialClauses (die frueher
            // gebaute gemeinsame OR-Gruppe lieferte bei Mehrfacheingabe 0 Zeilen). Deduplizierung
            // ueber (Richtung, Vknr, Kompnr), weil sich zwei Bereichsangaben ueberlappen koennen
            // und MaterialUsageCache per INSERT (nicht UPSERT) gefuellt wird - ohne das waere
            // dieselbe Zeile mehrfach im Cache.
            var usageRows = new List<Dictionary<string, object?>>();
            var seenUsageKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var tokensWithoutHit = new List<string>();
            foreach (var selection in selections)
            {
                var usageFilter = $"Richtung eq '{sapRichtung}' and {selection.Clause}";
                var tokenRows = await ReadAllRowsAsync(client, connection.BaseUrl, usageSetName, usageFilter, cancellationToken);
                foreach (var row in tokenRows)
                {
                    if (seenUsageKeys.Add(string.Join('|', GetText(row, "Richtung"), GetText(row, "Vknr"), GetText(row, "Kompnr"))))
                        usageRows.Add(row);
                }

                if (tokenRows.Count == 0 && !string.IsNullOrEmpty(selection.Token))
                    tokensWithoutHit.Add(selection.Token);
            }

            var parentRows = topDown
                ? await ReadAllRowsAsync(client, connection.BaseUrl, parentSetName, "Kompnr gt ''", cancellationToken)
                : [];

            var nowText = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var conn = (SqliteConnection)db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync(cancellationToken);

            await using var transaction = (SqliteTransaction)await conn.BeginTransactionAsync(cancellationToken);
            await ExecuteAsync(conn, transaction, "DELETE FROM MaterialUsageCache WHERE Richtung = $Richtung;",
                new() { ["$Richtung"] = topDown ? "TOPDOWN" : "BOTTOMUP" }, cancellationToken);
            await UpsertUsageAsync(conn, transaction, usageRows, nowText, cancellationToken);
            if (topDown)
            {
                await ExecuteAsync(conn, transaction, "DELETE FROM MaterialParentCache;", null, cancellationToken);
                await UpsertParentAsync(conn, transaction, parentRows, nowText, cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken);

            var completed = DateTime.UtcNow;
            var message = $"Full Load abgeschlossen: {usageSetName}={usageRows.Count:N0}, {parentSetName}={parentRows.Count:N0}.";
            if (selections.Count > 1)
                message += $" Einzeln abgefragt: {selections.Count:N0} Materialnummern.";

            // Rueckmeldung, WELCHE Nummern leer geblieben sind. In der Sitzung 2026-07-30 war
            // genau das das Problem: "Full Load abgeschlossen" sah nach Erfolg aus, obwohl keine
            // einzige Zeile kam, und es war nicht erkennbar, ob das an einer Nummer oder an allen
            // lag. Fuer die TR5-Aufgabe (welche Komponenten werden nirgends verbaut?) ist diese
            // Liste sogar das eigentliche Ergebnis.
            if (tokensWithoutHit.Count > 0)
                message += $" Ohne Treffer ({tokensWithoutHit.Count:N0}): {FormatTokenList(tokensWithoutHit)}.";

            if (usageRows.Count == 0 && !string.IsNullOrWhiteSpace(materialFilter) && !includeDeleted)
                message += topDown
                    ? " Hinweis: 0 Zeilen bei Top-Down kann bedeuten, dass das eingegebene Kopfmaterial " +
                      "loeschvorgemerkt ist (MARA-LVORM) - dann hilft 'Auch geloeschte Materialien' " +
                      "oder eine Suche ueber Bottom-Up (Komponente statt Kopfmaterial)."
                    : " Hinweis: 0 Zeilen bei Bottom-Up heisst, dass zu keiner der angegebenen Komponenten " +
                      "eine Verwendung gefunden wurde. Vor dem Schluss 'wird nirgends verbaut' bitte mit " +
                      "'Auch geloeschte Materialien' gegenpruefen - loeschvorgemerkte Kopfmaterialien " +
                      "werden sonst ausgeblendet.";
            await WriteStatusAsync("Success", started, completed, usageRows.Count, parentRows.Count, message, cancellationToken);
            await _logService.WriteAsync("MaterialUsage", "Stuecklistenanalyse Full Load erfolgreich", details: message);
            return await GetStatusAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var message = $"Full Load fehlgeschlagen: {ex.Message}";
            await WriteStatusAsync("Error", started, DateTime.UtcNow, 0, 0, message, cancellationToken);
            await _logService.WriteAsync("MaterialUsage", "Stuecklistenanalyse Full Load fehlgeschlagen", "Error", details: ex.ToString());
            return await GetStatusAsync(cancellationToken);
        }
    }

    public async Task<List<MaterialUsagePreviewRow>> GetCachedUsageRowsAsync(string? materialFilter = null, int limit = 200, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var conn = (SqliteConnection)db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(cancellationToken);

        // Suchfeld akzeptiert dieselben Trennzeichen wie die Ladeeingabe, damit man die eben
        // geladene Excel-Spalte auch zum Filtern wiederverwenden kann statt Nummer fuer Nummer
        // zu suchen. Je Token ein LIKE-Paar auf Vknr/Kompnr, mit OR verknuepft.
        var tokens = ParseMaterialTokens(materialFilter);
        var whereClause = tokens.Count == 0
            ? string.Empty
            : "WHERE " + string.Join(" OR ", tokens.Select((_, i) => $"Vknr LIKE $Filter{i} OR Kompnr LIKE $Filter{i}"));

        await using var command = conn.CreateCommand();
        command.CommandText = $@"
SELECT Richtung, Vknr, Kompnr, KompnrMaktx, KompnrMeins, Menge, Exklusiv,
       Labst, Endbestand, Stueckkosten, WertEndbestand, Mstae, Zzlzcod
FROM MaterialUsageCache
{whereClause}
ORDER BY Vknr, Kompnr
LIMIT $Limit;";
        for (var i = 0; i < tokens.Count; i++)
            command.Parameters.AddWithValue($"$Filter{i}", "%" + tokens[i] + "%");
        command.Parameters.AddWithValue("$Limit", limit);

        var rows = new List<MaterialUsagePreviewRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new MaterialUsagePreviewRow(
                Richtung: reader.GetString(0),
                Vknr: reader.GetString(1),
                Kompnr: reader.GetString(2),
                KompnrMaktx: reader.GetString(3),
                KompnrMeins: reader.GetString(4),
                Menge: reader.GetString(5),
                Exklusiv: !reader.IsDBNull(6) && reader.GetInt32(6) != 0,
                Labst: reader.GetString(7),
                Endbestand: reader.GetString(8),
                Stueckkosten: reader.GetString(9),
                WertEndbestand: reader.GetString(10),
                Mstae: reader.GetString(11),
                Zzlzcod: reader.GetString(12)));
        }

        return rows;
    }

    private async Task<MaterialUsageSapConnection> ResolveConnectionAsync(CancellationToken cancellationToken)
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
        return new MaterialUsageSapConnection(serviceUrl.TrimEnd('/') + "/", username, password);
    }

    private static HttpClient CreateClient(string username, string password)
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private static async Task<List<Dictionary<string, object?>>> ReadAllRowsAsync(HttpClient client, string baseUrl, string entitySet, string filter, CancellationToken cancellationToken)
    {
        var rows = new List<Dictionary<string, object?>>();
        for (var skip = 0; ; skip += PageSize)
        {
            var url = $"{baseUrl}{entitySet}?$format=json&$top={PageSize}&$skip={skip}";
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

    /// <summary>
    /// Findet in der EntitySet-Liste des Gateway-Service das Set, dessen normalisierter Name
    /// (nur Buchstaben/Ziffern, lowercase) eine der Suchnadeln enthaelt. Noetig, weil der
    /// exakte SEGW-Name nicht festgelegt ist: je nach Anlage heisst das Set
    /// "ZSTR_LZCODE_USAGESet", "ZSTR_LZCODE_USAGE" oder "MaterialUsageSet".
    /// </summary>
    public static string? ResolveEntitySetName(IReadOnlyCollection<string> entitySets, params string[] normalizedNeedles)
    {
        foreach (var candidate in entitySets)
        {
            var normalized = new string(candidate.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
            if (normalizedNeedles.Any(normalized.Contains))
                return candidate;
        }

        return null;
    }

    /// <summary>
    /// Laenge einer SAP-Materialnummer (MATNR CHAR18 auf travp762, per SapProbe 2026-07-23
    /// verifiziert: MARA speichert "000000000000002217").
    /// </summary>
    private const int MatnrLength = 18;

    /// <summary>
    /// Trennzeichen der Materialnummern-Eingabe: Komma, Semikolon und jede Art von Whitespace
    /// (Leerzeichen, Tab, Zeilenumbruch). Damit laesst sich eine Excel-Spalte direkt einfuegen -
    /// Wunsch Marco aus der Sitzung 2026-07-30: "Mit SAP kannst du die so reinkopieren, und dann
    /// tut es sich untereinander", waehrend hier bisher pro Nummer ein Komma noetig war.
    /// Der Whitespace-Split ist eindeutig, weil Materialnummern selbst keine Leerzeichen
    /// enthalten (Ingo in derselben Sitzung: "da hast du eh immer zusammenhaengende Nummern").
    /// Die Bereichsschreibweise "35-40" bleibt unberuehrt, weil sie kein Trennzeichen enthaelt;
    /// "35 - 40" mit Leerzeichen wuerde dagegen in drei Tokens zerfallen - bewusst nicht
    /// unterstuetzt, weil ein Bindestrich als eigenes Token ohnehin unbrauchbar waere.
    /// </summary>
    private static readonly char[] MaterialTokenSeparators = [',', ';', ' ', '\t', '\r', '\n'];

    /// <summary>
    /// Zerlegt die Benutzereingabe in einzelne Materialnummern-Tokens. Duplikate werden
    /// entfernt: beim Einfuegen aus Excel landet leicht derselbe Block zweimal in der Maske
    /// (in der Sitzung 2026-07-30 genau so passiert - "Hast du mehrmals das gleiche kopiert?"),
    /// und jedes Duplikat waere eine zusaetzliche SAP-Anfrage ohne neue Zeilen.
    /// </summary>
    public static List<string> ParseMaterialTokens(string? materialFilter) =>
        (materialFilter ?? string.Empty)
            .Split(MaterialTokenSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>
    /// Baut je Eingabe-Token EINE eigene $filter-Teilbedingung fuer Vknr/Kompnr. Ein Token mit
    /// genau einem Bindestrich und nicht-leeren Seiten (z.B. "35-40") wird als Bereich (ge/le)
    /// interpretiert, alles andere als Einzeltreffer (eq). SAP-seitig ist fuer den Bereich kein
    /// Zusatzaufwand noetig: das Gateway-Framework fasst "ge X and le Y" auf demselben Property
    /// beim Parsen von it_filter_select_options zu einer klassischen Select-Options-Bereichszeile
    /// zusammen, die der bestehende ABAP-Code (LOOP ueber select_options, generische
    /// RANGE-Tabelle) unveraendert verarbeitet.
    ///
    /// WARUM EINE BEDINGUNG JE TOKEN und nicht mehr eine gemeinsame OR-Gruppe
    /// "(Kompnr eq 'A' or Kompnr eq 'B')": In der Sitzung 2026-07-30 wurde live reproduziert,
    /// dass eine Mehrfacheingabe (Bottom-Up, mehrere kommagetrennte Nummern) zwar
    /// "Full Load abgeschlossen" meldet, aber 0 Zeilen liefert, waehrend dieselben Nummern
    /// einzeln Treffer haben. Der Aufrufer stellt deshalb je Token eine eigene Anfrage und
    /// fuehrt die Ergebnisse zusammen. Das umgeht die Umwandlung der gemischten
    /// and/or-Filterstruktur in Select-Options im Gateway vollstaendig - unabhaengig davon,
    /// wo genau sie schiefgeht (ABAP-seitig NICHT verifiziert, siehe
    /// docs/PURCHASING_DASHBOARD_WUENSCHE_EINKAUF_2026-07-30.md Abschnitt 5b) - und liefert als
    /// Nebeneffekt die Trefferzahl JE Nummer, was fuer die TR5-Aufgabe (welche Komponenten haben
    /// ueberhaupt keine Verwendung?) genau die gesuchte Information ist.
    ///
    /// Rein numerische Materialnummern werden mit fuehrenden Nullen auf 18 Stellen gebracht
    /// (NormalizeMaterialToken). Grund (Befund 2026-07-23, an travp762 verifiziert): das /IWBEP-
    /// Gateway liefert Filterwerte ROH an die selbstgeschriebene GET_ENTITYSET-Methode, also OHNE
    /// die MATNR-Konvertierung. MARA/ZPOWERBI_VC_TXT speichern intern aber zero-padded
    /// ("000000000000002217"), sodass die Kurzform "2217" in Schritt 1 (SELECT FROM mara) keinen
    /// Treffer fand und die Methode mit 0 Zeilen abbrach. Alphanumerische Nummern (z.B. "D15019",
    /// "C34882") bleiben unveraendert - die speichert MARA linksbuendig, nicht zero-padded.
    ///
    /// Ohne Eingabe bleibt es bei genau einer Anfrage mit dem Catch-all "gt ''" - die explizite
    /// "ja, wirklich alles"-Ansage gegen den SAP-seitigen Pflichtfilter.
    /// </summary>
    public static List<MaterialSelectionClause> BuildMaterialClauses(string materialProperty, string? materialFilter)
    {
        var tokens = ParseMaterialTokens(materialFilter);
        if (tokens.Count == 0)
            return [new MaterialSelectionClause(string.Empty, $"{materialProperty} gt ''")];

        return tokens.Select(token =>
        {
            var rangeParts = token.Split('-', StringSplitOptions.TrimEntries);
            if (rangeParts.Length == 2 && rangeParts[0].Length > 0 && rangeParts[1].Length > 0)
                return new MaterialSelectionClause(
                    token,
                    $"({materialProperty} ge '{EscapeODataLiteral(NormalizeMaterialToken(rangeParts[0]))}' and {materialProperty} le '{EscapeODataLiteral(NormalizeMaterialToken(rangeParts[1]))}')");

            return new MaterialSelectionClause(
                token,
                $"{materialProperty} eq '{EscapeODataLiteral(NormalizeMaterialToken(token))}'");
        }).ToList();
    }

    /// <summary>
    /// Bringt eine rein numerische Materialnummer auf die interne SAP-Darstellung (18 Stellen,
    /// fuehrende Nullen). Alphanumerische Werte (mind. ein Nicht-Ziffer-Zeichen) und bereits
    /// >= 18 Zeichen lange Werte bleiben unveraendert.
    /// </summary>
    public static string NormalizeMaterialToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return token;
        if (token.Length >= MatnrLength)
            return token;
        if (!token.All(char.IsDigit))
            return token;
        return token.PadLeft(MatnrLength, '0');
    }

    private static string EscapeODataLiteral(string value) => value.Replace("'", "''");

    /// <summary>
    /// Nummern fuer die Statusmeldung aufzaehlen, aber gedeckelt: bei einer eingefuegten
    /// Excel-Spalte koennen hunderte Nummern ohne Treffer sein, und die Meldung landet in
    /// AppEventLog und in einer MudAlert - beides soll lesbar bleiben.
    /// </summary>
    private const int MaxListedTokens = 20;

    private static string FormatTokenList(IReadOnlyList<string> tokens)
    {
        if (tokens.Count <= MaxListedTokens)
            return string.Join(", ", tokens);

        return string.Join(", ", tokens.Take(MaxListedTokens)) +
               $" ... (+{tokens.Count - MaxListedTokens:N0} weitere)";
    }

    /// <summary>
    /// Baut den Richtung-Wert fuer den $filter. Ein Suffix "D" (ohne DDIC-Aenderung
    /// transportiert, siehe docs/abap/README_LZCODE_WEBSERVICE.md) bezieht auch
    /// loeschvorgemerkte Kopf-/Filtermaterialien (MARA-LVORM) mit ein, analog
    /// Report-Checkbox p_lvorm. Ohne diese Option liefert Top-Down fuer alte, numerische
    /// Vknr wie "2217" 0 Zeilen, obwohl die Verwendung in ZPOWERBI_VC_TXT noch vorhanden
    /// ist (Befund 2026-07-22). BEWUSST NUR EIN ZEICHEN: das EDM-Property Richtung ist
    /// CHAR10-typisiert (facet maxlength=10) und wird vom Gateway-Framework VOR dem
    /// ABAP-Methodenaufruf validiert - "TOPDOWNALLE" (11 Zeichen) wurde mit HTTP 400
    /// abgelehnt, bevor der eigene Code ueberhaupt lief (live verifiziert 2026-07-22,
    /// zweiter Anlauf). "TOPDOWND"/"BOTTOMUPD" (8/9 Zeichen) passen sicher. Das von SAP
    /// zurueckgegebene Richtung-Feld bleibt normalisiert ("TOPDOWN"/"BOTTOMUP", ohne Suffix).
    /// </summary>
    public static string BuildRichtungValue(bool topDown, bool includeDeleted)
        => (topDown ? "TOPDOWN" : "BOTTOMUP") + (includeDeleted ? "D" : string.Empty);

    private static List<Dictionary<string, object?>> ParseRows(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("d", out var d) ||
            !d.TryGetProperty("results", out var results) ||
            results.ValueKind != JsonValueKind.Array)
            return [];

        // Property-Keys ohne Unterstriche ablegen: je nach SEGW-Anlage heissen die OData-
        // Properties "VknrMstae" (CamelCase-Konvention wie bei EKKOSet/WavwrDc) ODER roh
        // "VKNR_MSTAE" (Strukturfeldname). Das Dictionary ist ohnehin case-insensitiv;
        // mit dem Unterstrich-Strip treffen die GetText("VknrMstae")-Reads beide Varianten.
        return results.EnumerateArray()
            .Select(item => item.EnumerateObject()
                .Where(property => property.Name != "__metadata")
                .ToDictionary(
                    property => property.Name.Replace("_", string.Empty),
                    property => ConvertJsonValue(property.Value),
                    StringComparer.OrdinalIgnoreCase))
            .ToList();
    }

    private static async Task UpsertUsageAsync(SqliteConnection conn, SqliteTransaction transaction, IReadOnlyList<Dictionary<string, object?>> rows, string loadedAtUtc, CancellationToken cancellationToken)
    {
        const string sql = @"
INSERT INTO MaterialUsageCache (
    Richtung, Vknr, VknrMstae, VknrVerbrauch, Kompnr, KompnrMaktx, KompnrMeins, Menge, Exklusiv,
    Verbrauch, Labst, FesteZugang, GeplZugang, FesteAbgang, GeplAbgang, Endbestand, Omeng, Mkmng,
    Stueckkosten, WertFesteZug, WertGeplZug, WertFesteAbg, WertGeplAbg, WertEndbestand, Owert, Omkwr,
    Dismm, Minbe, Disls, Bstfe, Eisbe, Mstae, Mstav, Beskz, Zzlzcod, Zzlzcodsort, Baugruppe, Waers,
    RawJson, LastLoadedAtUtc)
VALUES (
    $Richtung, $Vknr, $VknrMstae, $VknrVerbrauch, $Kompnr, $KompnrMaktx, $KompnrMeins, $Menge, $Exklusiv,
    $Verbrauch, $Labst, $FesteZugang, $GeplZugang, $FesteAbgang, $GeplAbgang, $Endbestand, $Omeng, $Mkmng,
    $Stueckkosten, $WertFesteZug, $WertGeplZug, $WertFesteAbg, $WertGeplAbg, $WertEndbestand, $Owert, $Omkwr,
    $Dismm, $Minbe, $Disls, $Bstfe, $Eisbe, $Mstae, $Mstav, $Beskz, $Zzlzcod, $Zzlzcodsort, $Baugruppe, $Waers,
    $RawJson, $LastLoadedAtUtc);";

        foreach (var row in rows)
            await ExecuteWithParametersAsync(conn, transaction, sql, new()
            {
                ["$Richtung"] = GetText(row, "Richtung"),
                ["$Vknr"] = GetText(row, "Vknr"),
                ["$VknrMstae"] = GetText(row, "VknrMstae"),
                ["$VknrVerbrauch"] = GetText(row, "VknrVerbrauch"),
                ["$Kompnr"] = GetText(row, "Kompnr"),
                ["$KompnrMaktx"] = GetText(row, "KompnrMaktx"),
                ["$KompnrMeins"] = GetText(row, "KompnrMeins"),
                ["$Menge"] = GetText(row, "Menge"),
                ["$Exklusiv"] = IsTruthy(GetText(row, "Exklusiv")) ? 1 : 0,
                ["$Verbrauch"] = GetText(row, "Verbrauch"),
                ["$Labst"] = GetText(row, "Labst"),
                ["$FesteZugang"] = GetText(row, "FesteZugang"),
                ["$GeplZugang"] = GetText(row, "GeplZugang"),
                ["$FesteAbgang"] = GetText(row, "FesteAbgang"),
                ["$GeplAbgang"] = GetText(row, "GeplAbgang"),
                ["$Endbestand"] = GetText(row, "Endbestand"),
                ["$Omeng"] = GetText(row, "Omeng"),
                ["$Mkmng"] = GetText(row, "Mkmng"),
                ["$Stueckkosten"] = GetText(row, "Stueckkosten"),
                ["$WertFesteZug"] = GetText(row, "WertFesteZug"),
                ["$WertGeplZug"] = GetText(row, "WertGeplZug"),
                ["$WertFesteAbg"] = GetText(row, "WertFesteAbg"),
                ["$WertGeplAbg"] = GetText(row, "WertGeplAbg"),
                ["$WertEndbestand"] = GetText(row, "WertEndbestand"),
                ["$Owert"] = GetText(row, "Owert"),
                ["$Omkwr"] = GetText(row, "Omkwr"),
                ["$Dismm"] = GetText(row, "Dismm"),
                ["$Minbe"] = GetText(row, "Minbe"),
                ["$Disls"] = GetText(row, "Disls"),
                ["$Bstfe"] = GetText(row, "Bstfe"),
                ["$Eisbe"] = GetText(row, "Eisbe"),
                ["$Mstae"] = GetText(row, "Mstae"),
                ["$Mstav"] = GetText(row, "Mstav"),
                ["$Beskz"] = GetText(row, "Beskz"),
                ["$Zzlzcod"] = GetText(row, "Zzlzcod"),
                ["$Zzlzcodsort"] = GetText(row, "Zzlzcodsort"),
                ["$Baugruppe"] = IsTruthy(GetText(row, "Baugruppe")) ? 1 : 0,
                ["$Waers"] = GetText(row, "Waers"),
                ["$RawJson"] = JsonSerializer.Serialize(row),
                ["$LastLoadedAtUtc"] = loadedAtUtc
            }, cancellationToken);
    }

    private static async Task UpsertParentAsync(SqliteConnection conn, SqliteTransaction transaction, IReadOnlyList<Dictionary<string, object?>> rows, string loadedAtUtc, CancellationToken cancellationToken)
    {
        const string sql = @"
INSERT OR REPLACE INTO MaterialParentCache (Kompnr, ElternMatnr, LastLoadedAtUtc)
VALUES ($Kompnr, $ElternMatnr, $LastLoadedAtUtc);";
        foreach (var row in rows)
            await ExecuteWithParametersAsync(conn, transaction, sql, new()
            {
                ["$Kompnr"] = GetText(row, "Kompnr"),
                ["$ElternMatnr"] = GetText(row, "ElternMatnr"),
                ["$LastLoadedAtUtc"] = loadedAtUtc
            }, cancellationToken);
    }

    private async Task WriteStatusAsync(string status, DateTime? startedAtUtc, DateTime? completedAtUtc, int usageRows, int parentRows, string message, CancellationToken cancellationToken)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var conn = (SqliteConnection)db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(cancellationToken);
        const string sql = @"
INSERT INTO MaterialUsageSyncState (Status, StartedAtUtc, CompletedAtUtc, UsageRows, ParentRows, Message)
VALUES ($Status, $StartedAtUtc, $CompletedAtUtc, $UsageRows, $ParentRows, $Message);";
        await ExecuteWithParametersAsync(conn, null, sql, new()
        {
            ["$Status"] = status,
            ["$StartedAtUtc"] = FormatDateTime(startedAtUtc),
            ["$CompletedAtUtc"] = FormatDateTime(completedAtUtc),
            ["$UsageRows"] = usageRows,
            ["$ParentRows"] = parentRows,
            ["$Message"] = message
        }, cancellationToken);
    }

    private static async Task<MaterialUsageRefreshStatus> ReadLatestStatusAsync(SqliteConnection conn, CancellationToken cancellationToken)
    {
        await using var command = conn.CreateCommand();
        command.CommandText = @"
SELECT Status, StartedAtUtc, CompletedAtUtc, UsageRows, ParentRows, Message
FROM MaterialUsageSyncState
ORDER BY Id DESC
LIMIT 1;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return new MaterialUsageRefreshStatus { Status = "Empty", Message = "Noch kein Stuecklistenanalyse Full Load ausgefuehrt." };

        return new MaterialUsageRefreshStatus
        {
            Status = reader.GetString(0),
            StartedAtUtc = ParseDateTime(reader.GetString(1)),
            CompletedAtUtc = ParseDateTime(reader.GetString(2)),
            UsageRows = reader.GetInt32(3),
            ParentRows = reader.GetInt32(4),
            Message = reader.GetString(5)
        };
    }

    private static async Task ExecuteAsync(SqliteConnection conn, SqliteTransaction? transaction, string sql, Dictionary<string, object?>? parameters, CancellationToken cancellationToken)
    {
        await using var command = conn.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        if (parameters is not null)
            foreach (var (key, value) in parameters)
                command.Parameters.AddWithValue(key, value ?? DBNull.Value);
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

    private static bool IsTruthy(string value)
        => value.Equals("X", StringComparison.OrdinalIgnoreCase) ||
           value.Equals("TRUE", StringComparison.OrdinalIgnoreCase) ||
           value.Equals("1", StringComparison.OrdinalIgnoreCase);

    private static string TrimForLog(string value)
        => value.Length <= 1000 ? value : value[..1000] + "...";

    private static string FormatDateTime(DateTime? value)
        => value?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty;

    private static DateTime? ParseDateTime(string value)
        => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed) ? parsed : null;

    private sealed record MaterialUsageSapConnection(string BaseUrl, string Username, string Password);
}
