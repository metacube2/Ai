using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

/// <summary>Basis-URL (z. B. „https://…/rexx_hub/“) und Bearer-Token. ToString zeigt den Token nie.</summary>
public sealed record RexxHubConnection(string BaseUrl, string Token)
{
    public override string ToString() => $"RexxHubConnection {{ BaseUrl = {BaseUrl}, Token = *** }}";
}

/// <summary>Fehler beim Abruf, mit verstaendlicher deutscher Meldung, ohne Token und ohne Antwortinhalt.</summary>
public sealed class RexxHubException(string message, int? statusCode = null, string? requestId = null, bool transient = false) : Exception(message)
{
    public int? StatusCode { get; } = statusCode;
    public string? RequestId { get; } = requestId;

    /// <summary>
    /// Voruebergehend (429, 500, 502, 503, 504, Zeitueberschreitung, Netz): der Tagesdienst versucht es im naechsten Durchgang
    /// noch einmal. Alles andere (401, 403, 404, 422, Format, Blaettern) wartet bis morgen.
    /// </summary>
    public bool Transient { get; } = transient;

    internal static bool IsTransientStatus(int status) => status is 429 or 500 or 502 or 503 or 504;
}

/// <summary>Ergebnis einer Abfrage: alle Seiten zusammen, ColumnMap schon angewendet.</summary>
public sealed record RexxHubQueryResult(
    int QueryId,
    int HttpStatus,
    RexxHubTable Table,
    int Pages,
    string? RequestId,
    TimeSpan Duration)
{
    /// <summary>Zeilen der ersten Seite und Zeilen je Seite laut „_meta“: damit laesst sich „Zeilenanzahl pro Seite“ in Rexx pruefen.</summary>
    public int FirstPageRows { get; init; }
    public int? PageSize { get; init; }

    /// <summary>Schluesselnamen von „_meta“ und „_links“ der ersten Seite, nie Werte: damit laesst sich das Blaettern vor dem Umschalten pruefen.</summary>
    public IReadOnlyList<string> MetaKeys { get; init; } = [];
    public IReadOnlyList<string> LinkKeys { get; init; } = [];
}

/// <summary>Woher der Abruf Basis-URL und Token nimmt; im Betrieb aus der Cockpit-Datenbank, im Test ein Fake.</summary>
public interface IRexxHubConnectionSource
{
    Task<RexxHubConnection> ResolveAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Zugang aus <c>SourceSystemDefinitions</c> mit Code <c>REXXHUB</c> (Admin &gt; Settings), wie der SAP-Abruf den Eintrag
/// „SAP“ liest. Das Modell hat keine eigenen Felder fuer eine API: „Zentrale SAP Service URL“ (<c>CentralServiceUrl</c>)
/// traegt die Basis-URL, „Zentrales Passwort“ (<c>CentralPassword</c>) den Token, weil es das einzige verdeckte Feld ist.
/// Das URL-Feld zeigt der Dialog nur bei Anschlussart „SAP Gateway“ (<c>Settings.razor</c>). Ein inaktiver Eintrag gilt
/// als nicht eingerichtet.
/// </summary>
public sealed class RexxHubDbConnectionSource(IDbContextFactory<AppDbContext> dbFactory, IOptions<RexxHubOptions> options) : IRexxHubConnectionSource
{
    public async Task<RexxHubConnection> ResolveAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entry = await db.SourceSystemDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == RexxHubOptions.SourceSystemCode, cancellationToken);
        return RexxHubClient.ToConnection(entry, options.Value.AllowHttp);
    }
}

/// <summary>
/// HTTP-Abruf einer Rexx-Data-Warehouse-Abfrage ueber den Rexx Hub (<c>GET v1/datawarehouse/{id}</c>, Produktdokument
/// Kapitel 6 und 11). Was das Dokument vorgibt und hier umgesetzt ist:
/// - Authentifizierung „Authorization: Bearer &lt;token&gt;“ (Kapitel 7), Antwort als JSON angefordert (Kapitel 4).
/// - Hoechstens EINE Anfrage gleichzeitig ueber alle API-Benutzer (Kapitel 9): eine Sperre fuer den ganzen Prozess, die
///   Tagesabruf und Testabruf teilen; sie gilt fuer alle Seiten einer Abfrage.
/// - 429: kurz warten und hoechstens zweimal wiederholen. Jede Anfrage zaehlt gegen 50'000 pro Monat.
/// - Fehlercodes 400/401/403/404/422/429/500/504 mit Bedeutung aus Kapitel 4.
/// Offen (nicht im Dokument): Namen der Seiten-Parameter und von „_meta“, Form von „data“; siehe
/// <see cref="RexxHubResponseParser"/> und <see cref="RexxHubOptions"/>.
/// </summary>
public sealed class RexxHubClient
{
    /// <summary>Prozessweit: Rexx erlaubt nur eine Anfrage gleichzeitig (File-Lock mit hoechstens 3 s Wartezeit).</summary>
    private static readonly SemaphoreSlim Gate = new(1, 1);

    /// <summary>Hoechstens so viele Wiederholungen nach 429.</summary>
    internal const int MaxRetriesOn429 = 2;

    private readonly HttpMessageHandler _handler;

    /// <summary>Warten vor einer Wiederholung; im Test ersetzt, damit nichts wirklich wartet.</summary>
    internal Func<TimeSpan, CancellationToken, Task> Delay { get; set; } = Task.Delay;

    public RexxHubClient()
        : this(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(10) })
    {
    }

    internal RexxHubClient(HttpMessageHandler handler)
    {
        _handler = handler;
    }

    /// <summary>Zugang aus dem Settings-Eintrag pruefen; Meldungen nennen das Feld in der Oberflaeche.</summary>
    internal static RexxHubConnection ToConnection(SourceSystemDefinition? entry, bool allowHttp = false)
    {
        if (entry is null)
            throw new RexxHubException($"Quellsystem {RexxHubOptions.SourceSystemCode} fehlt. Unter Admin > Settings > Quellsysteme anlegen: Code {RexxHubOptions.SourceSystemCode}, Anschlussart SAP Gateway, Basis-URL im URL-Feld, Token im Passwortfeld.");
        if (!entry.IsActive)
            throw new RexxHubException($"Quellsystem {RexxHubOptions.SourceSystemCode} ist nicht aktiv (Admin > Settings).");
        var url = (entry.CentralServiceUrl ?? string.Empty).Trim();
        if (url.Length == 0)
            throw new RexxHubException($"Quellsystem {RexxHubOptions.SourceSystemCode}: Basis-URL fehlt (Feld „Zentrale SAP Service URL“, sichtbar bei Anschlussart SAP Gateway).");
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            throw new RexxHubException($"Quellsystem {RexxHubOptions.SourceSystemCode}: Basis-URL ist keine https-Adresse.");
        RequireHttps(uri, allowHttp);
        var token = (entry.CentralPassword ?? string.Empty).Trim();
        if (token.Length == 0)
            throw new RexxHubException($"Quellsystem {RexxHubOptions.SourceSystemCode}: Token fehlt (Feld „Zentrales Passwort“).");
        return new RexxHubConnection(url, token);
    }

    /// <summary>Holt alle Seiten einer Abfrage. Wirft <see cref="RexxHubException"/> oder <see cref="RexxHubFormatException"/>.</summary>
    public async Task<RexxHubQueryResult> FetchQueryAsync(
        RexxHubConnection connection, int queryId, RexxHubOptions options, CancellationToken cancellationToken = default)
    {
        var watch = Stopwatch.StartNew();
        var baseUri = new Uri(connection.BaseUrl.Trim().TrimEnd('/') + "/");
        // Auch hier, nicht nur beim Lesen des Settings-Eintrags: der Token geht nur ueber https (ausser mit AllowHttp).
        RequireHttps(baseUri, options.AllowHttp);
        var firstUri = BuildFirstUri(baseUri, queryId, options);

        await Gate.WaitAsync(cancellationToken);
        try
        {
            using var client = new HttpClient(_handler, disposeHandler: false)
            {
                Timeout = TimeSpan.FromSeconds(Math.Max(5, options.TimeoutSeconds))
            };
            var table = new RexxHubTable();
            var uri = firstUri;
            var visited = new HashSet<string>(StringComparer.Ordinal);
            string? requestId = null;
            int? total = null;
            int? countHint = null;
            var status = 0;
            var maxPages = Math.Max(1, options.MaxPages);
            var localPage = 1;
            IReadOnlyList<string>? header = null;
            RexxHubPage? firstPage = null;

            for (var pages = 1; ; pages++)
            {
                if (pages > maxPages)
                    throw new RexxHubException($"Abfrage #{queryId}: nach {maxPages} Seiten immer noch weitere Seiten. Abbruch (HrKpi:RexxHub:MaxPages; Zeilenanzahl pro Seite in Rexx erhoehen).", status, requestId);
                if (!visited.Add(uri.AbsoluteUri))
                    throw new RexxHubException($"Abfrage #{queryId}: die naechste Seite zeigt auf eine schon gelesene Adresse. Blaettern wirkt nicht; Abbruch.", status, requestId);

                var (code, body) = await SendAsync(client, uri, connection.Token, queryId, options.RetryDelaySeconds, cancellationToken);
                status = code;
                RexxHubPage page;
                try
                {
                    page = RexxHubResponseParser.ParsePage(body, RexxHubParseSettings.From(options, header));
                }
                catch (RexxHubFormatException ex)
                {
                    throw new RexxHubFormatException($"Abfrage #{queryId}: {ex.Message}");
                }

                firstPage ??= page;
                header = page.Header ?? header;
                table.Append(page.Table);
                requestId = page.RequestId ?? requestId;
                total = page.Total ?? total;
                countHint = page.CountHint ?? countHint;
                var current = page.CurrentPage ?? localPage;

                if (page.UnreadableNextKey is { } badKey)
                    throw new RexxHubException(
                        $"Abfrage #{queryId}: „_links.{badKey}“ sieht nach der naechsten Seite aus, ist aber nicht lesbar. Damit nichts halb geschrieben wird, Abbruch " +
                        "(Name ggf. unter HrKpi:RexxHub:NextLinkKeys eintragen).", status, requestId);

                Uri? next = null;
                if (page.LastPage is { } lastKnown && current >= lastKnown)
                {
                    // Letzte Seite laut „_meta“: fertig, auch wenn ein Link „next“ dasteht (manche Server zeigen dann auf sich selbst).
                }
                else if (page.NextLink is not null)
                {
                    // Zeigt „next“ ohne bekannte letzte Seite auf die eigene Adresse, bricht die Pruefung „schon gelesen“ oben ab:
                    // ob die Liste vollstaendig ist, laesst sich dann nicht feststellen (Abhilfe: MetaLastPageKeys einstellen).
                    next = ResolveNext(baseUri, uri, page.NextLink, queryId);
                }
                else if (page.LastPage is { } last && current < last)
                {
                    if (string.IsNullOrWhiteSpace(options.PageParameter))
                        throw new RexxHubException(
                            $"Abfrage #{queryId}: die Antwort hat {last} Seiten, aber keinen Link auf die naechste. Damit nichts halb geschrieben wird, Abbruch. " +
                            "Abhilfe: in Rexx die Zeilenanzahl pro Seite erhoehen oder HrKpi:RexxHub:PageParameter setzen.", status, page.RequestId ?? requestId);
                    next = WithQuery(uri, options.PageParameter.Trim(), (current + 1).ToString(CultureInfo.InvariantCulture));
                }

                if (next is null)
                {
                    CheckComplete(queryId, page, current, uri, table.Rows.Count, total, countHint, status, requestId);
                    watch.Stop();
                    return new RexxHubQueryResult(queryId, status, table.WithColumnMap(options.ColumnMap), pages, requestId, watch.Elapsed)
                    {
                        FirstPageRows = firstPage.Table.Rows.Count,
                        PageSize = firstPage.PageSize,
                        MetaKeys = firstPage.MetaKeys,
                        LinkKeys = firstPage.LinkKeys
                    };
                }

                uri = next;
                localPage = current + 1;
            }
        }
        catch (RexxHubException ex)
        {
            throw new RexxHubException(Scrub(ex.Message, connection.Token), ex.StatusCode, ex.RequestId, ex.Transient);
        }
        finally
        {
            Gate.Release();
        }
    }

    /// <summary>
    /// Vollstaendigkeit am Ende: Gesamtzahl muss passen, „count“ darf nicht groesser sein als das Gelieferte. Ohne letzte Seite
    /// und ohne Gesamtzahl wird abgebrochen, wenn eine unbekannte Seitenangabe weitere Seiten nennt, „_links.last“ woanders
    /// hinzeigt oder die Seite genau voll ist (dann kaeme vermutlich noch eine; Pruefung 2026-11-10).
    /// </summary>
    private static void CheckComplete(
        int queryId, RexxHubPage page, int current, Uri uri, int rows, int? total, int? countHint, int status, string? requestId)
    {
        const string Tail = " Damit nichts halb geschrieben wird, Abbruch (Namen unter HrKpi:RexxHub:Meta…Keys/NextLinkKeys eintragen oder Zeilenanzahl pro Seite in Rexx erhoehen).";
        if (total is { } expected && expected != rows)
            throw new RexxHubException($"Abfrage #{queryId}: Rexx meldet {expected} Datensaetze, geliefert wurden {rows}. Damit nichts halb geschrieben wird, Abbruch.", status, requestId);
        if (countHint is { } count && count > rows)
            throw new RexxHubException($"Abfrage #{queryId}: „_meta“ nennt eine Anzahl von {count}, geliefert wurden {rows}.{Tail}", status, requestId);
        if (page.LastPage is not null || total is not null)
            return;
        if (page.UnknownPageNumbers.FirstOrDefault(x => x.Value > current) is { Key: not null } unknown)
            throw new RexxHubException($"Abfrage #{queryId}: „_meta.{unknown.Key}“ = {unknown.Value} nach Seite {current}, aber kein Link auf die naechste Seite.{Tail}", status, requestId);
        if (page.LastLink is { } lastLink && Uri.TryCreate(uri, lastLink.Trim(), out var lastUri) &&
            !string.Equals(lastUri.PathAndQuery, uri.PathAndQuery, StringComparison.Ordinal))
            throw new RexxHubException($"Abfrage #{queryId}: „_links.last“ zeigt auf eine andere Seite, aber es gibt keinen Link auf die naechste.{Tail}", status, requestId);
        if (page.PageSize is > 0 and var size && page.Table.Rows.Count >= size)
            throw new RexxHubException($"Abfrage #{queryId}: die letzte gelesene Seite ist mit {size} Zeilen genau voll, aber es gibt weder einen Link auf die naechste noch eine Seitenzahl.{Tail}", status, requestId);
    }

    /// <summary>Nur https, ausser <c>HrKpi:RexxHub:AllowHttp</c> ist gesetzt (nur fuer Tests).</summary>
    internal static void RequireHttps(Uri uri, bool allowHttp)
    {
        if (uri.Scheme == Uri.UriSchemeHttps || (allowHttp && uri.Scheme == Uri.UriSchemeHttp))
            return;
        throw new RexxHubException(
            $"Basis-URL des Quellsystems {RexxHubOptions.SourceSystemCode} ist {uri.Scheme}://, nicht https://. Token und HR-Daten gehen nur verschluesselt; " +
            "Adresse unter Admin > Settings korrigieren (http nur mit HrKpi:RexxHub:AllowHttp fuer einen Test).");
    }

    internal static Uri BuildFirstUri(Uri baseUri, int queryId, RexxHubOptions options, DateTime? today = null)
    {
        var day = (today ?? DateTime.Today).Date;
        var template = string.IsNullOrWhiteSpace(options.PathTemplate) ? "v1/datawarehouse/{0}" : options.PathTemplate.Trim();
        var relative = string.Format(CultureInfo.InvariantCulture, template, queryId).TrimStart('/');
        var uri = new Uri(baseUri, relative);
        foreach (var (key, value) in options.QueryParameters ?? [])
        {
            if (!string.IsNullOrWhiteSpace(key))
                uri = WithQuery(uri, key.Trim(), ExpandDate(value ?? string.Empty, day));
        }
        if (!string.IsNullOrWhiteSpace(options.PageSizeParameter) && options.PageSize > 0)
            uri = WithQuery(uri, options.PageSizeParameter.Trim(), options.PageSize.ToString(CultureInfo.InvariantCulture));
        return uri;
    }

    /// <summary>„{heute}“ und „{jahresanfang}“ in einem Parameterwert als Datum im Format des Produktdokuments (2009-11-04).</summary>
    internal static string ExpandDate(string value, DateTime today)
        => value
            .Replace("{heute}", today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{jahresanfang}", new DateTime(today.Year, 1, 1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);

    /// <summary>Setzt oder ersetzt einen Query-Parameter.</summary>
    internal static Uri WithQuery(Uri uri, string name, string value)
    {
        var builder = new UriBuilder(uri);
        var parts = (builder.Query.StartsWith('?') ? builder.Query[1..] : builder.Query)
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !string.Equals(Uri.UnescapeDataString(p.Split('=')[0]), name, StringComparison.Ordinal))
            .ToList();
        parts.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}");
        builder.Query = string.Join("&", parts);
        return builder.Uri;
    }

    /// <summary>
    /// Folgt <c>_links.next</c> nur auf denselben Host wie die Basis-URL, sonst ginge der Token an einen fremden Server.
    /// Schema und Port kommen immer von der Basis-URL: steht Rexx hinter einem TLS-Terminator, der intern http-Links erzeugt
    /// („http://host/…“ bei Basis „https://host/…“), wird auf https umgeschrieben statt abgebrochen; ueber http geht der
    /// Token so nie.
    /// </summary>
    internal static Uri ResolveNext(Uri baseUri, Uri current, string link, int queryId)
    {
        if (!Uri.TryCreate(current, link.Trim(), out var next) || next.Scheme is not ("http" or "https"))
            throw new RexxHubException($"Abfrage #{queryId}: Link auf die naechste Seite ist keine gueltige Adresse.");
        if (!string.Equals(next.Host, baseUri.Host, StringComparison.OrdinalIgnoreCase))
            throw new RexxHubException(
                $"Abfrage #{queryId}: Link auf die naechste Seite zeigt auf einen anderen Server ({next.Scheme}://{next.Host}:{next.Port}, " +
                $"Basis {baseUri.Scheme}://{baseUri.Host}:{baseUri.Port}); aus Sicherheitsgruenden nicht gefolgt.");
        if (next.Scheme == baseUri.Scheme && next.Port == baseUri.Port)
            return next;
        return new UriBuilder(next) { Scheme = baseUri.Scheme, Port = baseUri.Port }.Uri;
    }

    private async Task<(int Status, string Body)> SendAsync(
        HttpClient client, Uri uri, string token, int queryId, int retryDelaySeconds, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(request, cancellationToken);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new RexxHubException($"Abfrage #{queryId}: keine Antwort innerhalb von {client.Timeout.TotalSeconds:0} s.", transient: true);
            }
            catch (HttpRequestException ex)
            {
                throw new RexxHubException($"Abfrage #{queryId}: Rexx Hub nicht erreichbar ({ex.Message}).", transient: true);
            }

            using (response)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                var code = (int)response.StatusCode;
                if (response.StatusCode == HttpStatusCode.TooManyRequests && attempt < MaxRetriesOn429)
                {
                    await Delay(RetryWait(response, retryDelaySeconds), cancellationToken);
                    continue;
                }
                if (!response.IsSuccessStatusCode)
                    throw new RexxHubException(StatusMessage(code, queryId, ErrorRequestId(body)), code, ErrorRequestId(body), RexxHubException.IsTransientStatus(code));

                var mediaType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
                if (mediaType.Contains("xml", StringComparison.OrdinalIgnoreCase))
                    throw new RexxHubException($"Abfrage #{queryId}: Rexx antwortet mit XML statt JSON ({mediaType}). Ausgabeformat des Endpunkts auf JSON stellen.", code);
                return (code, body);
            }
        }
    }

    /// <summary>Retry-After von Rexx, sonst die eingestellte Wartezeit; hoechstens 60 s.</summary>
    private static TimeSpan RetryWait(HttpResponseMessage response, int retryDelaySeconds)
    {
        var wait = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(Math.Max(0, retryDelaySeconds));
        return wait > TimeSpan.FromSeconds(60) ? TimeSpan.FromSeconds(60) : wait < TimeSpan.Zero ? TimeSpan.Zero : wait;
    }

    /// <summary>Request id aus einer Fehlerantwort, falls sie JSON mit „_meta“ ist; der Inhalt selbst wird nie weitergegeben.</summary>
    private static string? ErrorRequestId(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            return RexxHubResponseParser.RequestId(document.RootElement);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Meldungen je Statuscode nach Produktdokument Kapitel 4; bewusst ohne Antwortinhalt (koennte Personendaten tragen).</summary>
    internal static string StatusMessage(int status, int queryId, string? requestId)
    {
        var text = status switch
        {
            400 => "Anfrage ungueltig (400). Pfad (HrKpi:RexxHub:PathTemplate), Seiten-Parameter und QueryParameters pruefen.",
            401 => "Endpunkt nicht freigegeben (401). In Rexx unter Tools > Rexx Hub den Endpunkt Data Warehouse fuer den API-Benutzer freigeben und den API-Benutzer aktiv schalten.",
            // Das Dokument nennt fuer 403 nur den ungueltigen Token. Ob gesperrte IP oder Verfallsdatum (Kapitel 3.1) auch 403
            // geben, steht nicht drin; deshalb nur als Moeglichkeit genannt.
            403 => "Token ungueltig (403). Token im Quellsystem REXXHUB (Admin > Settings, Feld Passwort) mit dem Token des API-Benutzers in Rexx vergleichen. Moeglich, aber nicht dokumentiert: Zugriff von einer nicht freigegebenen IP-Adresse oder abgelaufener API-Benutzer.",
            404 => $"Abfrage-ID {queryId} unbekannt oder nicht freigegeben (404). Pruefen, ob die Data-Warehouse-Abfrage #{queryId} existiert und ueber den Hub abrufbar ist, und ob Basis-URL und Pfad stimmen.",
            422 => "Zu viele Felder (422, „Please reduce the requested fields“). Export-Felder des Endpunkts in Rexx reduzieren.",
            429 => $"Anfragelimit (429), auch nach {MaxRetriesOn429} Wiederholungen. Rexx erlaubt eine Anfrage gleichzeitig ueber alle API-Benutzer und 50'000 pro Monat.",
            500 => "Interner Fehler bei Rexx (500). Mit der request id an den Rexx-Support.",
            504 => "Zeitueberschreitung bei Rexx (504). Spaeter erneut versuchen.",
            _ => $"Rexx Hub antwortete mit HTTP {status}."
        };
        return $"Abfrage #{queryId}: {text}" + (string.IsNullOrWhiteSpace(requestId) ? string.Empty : $" request id {requestId}.");
    }

    /// <summary>Token aus einer Meldung entfernen, falls ihn eine Fremdmeldung doch enthaelt.</summary>
    internal static string Scrub(string message, string token)
        => string.IsNullOrEmpty(token) || token.Length < 4 ? message : message.Replace(token, "***", StringComparison.Ordinal);
}
