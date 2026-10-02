using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Feste Liste bekannter Ziele (docs/DATENQUELLEN_FIREWALL_TRAGVAPP401_2026-09-10.md plus
/// Konfiguration plus eigene Ziele). Keine Netzbereiche, keine Portlisten, kein ICMP.
/// </summary>
public sealed class NetworkTargetCatalog
{
    private readonly NetworkStore _store;
    private readonly AdComputerService _ad;

    public NetworkTargetCatalog(NetworkStore store, AdComputerService ad)
    {
        _store = store;
        _ad = ad;
    }

    public async Task<IReadOnlyList<NetworkTarget>> LoadAsync(CancellationToken ct)
    {
        var targets = new List<NetworkTarget>();

        // SAP: nur der oeffentliche Ping ohne Anmeldung, nie OData mit dem Service-Benutzer.
        var sapRows = await _store.QueryAsync("SELECT SapServiceUrl, TSC FROM Sites WHERE COALESCE(SapServiceUrl,'') <> ''", null, ct);
        foreach (var group in sapRows
                     .Select(r => (Uri: Uri.TryCreate(Convert.ToString(r[0]), UriKind.Absolute, out var u) ? u : null, Tsc: Convert.ToString(r[1]) ?? ""))
                     .Where(x => x.Uri is not null)
                     .GroupBy(x => (x.Uri!.Scheme, x.Uri.Host, x.Uri.Port)))
        {
            var (scheme, host, port) = group.Key;
            targets.Add(new NetworkTarget($"sap:{host}", "SAP " + host.Split('.')[0].ToUpperInvariant(), "SAP", host, port, NetworkProbeKind.Http,
                $"{scheme}://{host}:{port}/sap/public/ping", Sites: ["ZSCHWEIZ", "Einkauf", "HR", "Logistik"]));
        }

        // HANA: nur Port-Test, nie eine Datenbanksitzung.
        var hanaRows = await _store.QueryAsync(@"
SELECT h.Host, h.Port, COALESCE(GROUP_CONCAT(s.TSC), '')
FROM HanaServers h LEFT JOIN Sites s ON s.HanaServerId = h.Id AND s.IsActive = 1
WHERE COALESCE(h.Host,'') <> '' GROUP BY h.Host, h.Port", null, ct);
        foreach (var r in hanaRows)
        {
            var host = Convert.ToString(r[0])!.Trim();
            var sites = (Convert.ToString(r[2]) ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct().ToList();
            targets.Add(new NetworkTarget($"hana:{host}", "HANA " + host, "HANA", host, Convert.ToInt32(r[1]), NetworkProbeKind.Tcp, Sites: sites));
        }

        // Microsoft 365 und EZB aus der Firewall-Doku vom 10.09.: TLS-Handshake, Zertifikat lesen.
        var manualSites = (await _store.QueryAsync("SELECT TSC FROM Sites WHERE SourceSystem = 'MANUAL_EXCEL' AND IsActive = 1", null, ct))
            .Select(r => Convert.ToString(r[0]) ?? "").ToList();
        targets.Add(new NetworkTarget("tls:trafagag.sharepoint.com", "SharePoint", "Microsoft 365", "trafagag.sharepoint.com", 443, NetworkProbeKind.Tls, Sites: manualSites));
        targets.Add(new NetworkTarget("tls:graph.microsoft.com", "Microsoft Graph", "Microsoft 365", "graph.microsoft.com", 443, NetworkProbeKind.Tls));
        targets.Add(new NetworkTarget("tls:login.microsoftonline.com", "Microsoft Login", "Microsoft 365", "login.microsoftonline.com", 443, NetworkProbeKind.Tls));
        targets.Add(new NetworkTarget("tls:www.ecb.europa.eu", "EZB Kurse", "Extern", "www.ecb.europa.eu", 443, NetworkProbeKind.Tls));

        // Domaenencontroller aus dem AD (nur wenn NetworkProbe:AdEnabled): nur TCP-Aufbau auf LDAP und Kerberos.
        if (_ad.IsEnabled)
        {
            var ad = await _ad.GetAsync();
            foreach (var dc in ad.Computers.Where(c => c.IsDomainController && c.Enabled && c.DnsHostName.Length > 0).OrderBy(c => c.Name))
            {
                targets.Add(new NetworkTarget($"dc-ldap:{dc.DnsHostName}", $"DC {dc.Name} LDAP", "Active Directory", dc.DnsHostName, 389, NetworkProbeKind.Tcp));
                targets.Add(new NetworkTarget($"dc-krb:{dc.DnsHostName}", $"DC {dc.Name} Kerberos", "Active Directory", dc.DnsHostName, 88, NetworkProbeKind.Tcp));
            }
        }

        foreach (var w in (await _store.LoadWatchTargetsAsync(ct)).Where(x => x.IsActive && x.Host.Length > 0))
            targets.Add(new NetworkTarget($"custom:{w.Id}", w.Label.Length > 0 ? w.Label : w.Host, "Eigene Ziele", w.Host, w.Port,
                NetworkProbeKind.Tcp, WorkCenter: w.WorkCenter.Length > 0 ? w.WorkCenter : null, Custom: true));

        return targets;
    }
}

/// <summary>
/// Prueft die Ziele alle 5 Minuten (Entscheid Ingo 2026-10-02: laufend, damit es Verlauf gibt),
/// nur in Produktion, mit Notschalter <c>NetworkProbe:Enabled</c>. Jede Pruefung hoechstens 5 s.
/// Antwortet ein HTTP-Ziel mit 401 oder 403, wird es bis zum Neustart nicht mehr geprueft.
/// </summary>
public sealed class NetworkProbeService : BackgroundService
{
    internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
    private readonly NetworkTargetCatalog _catalog;
    private readonly NetworkStore _store;
    private readonly IOptionsMonitor<NetworkProbeOptions> _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<NetworkProbeService> _logger;
    private readonly AdSnapshotService _snapshots;
    private readonly HashSet<string> _halted = new();
    private DateOnly _lastCleanup;

    public NetworkProbeService(NetworkTargetCatalog catalog, NetworkStore store, IOptionsMonitor<NetworkProbeOptions> options,
        IHostEnvironment environment, ILogger<NetworkProbeService> logger, AdSnapshotService snapshots)
    {
        _snapshots = snapshots;
        _catalog = catalog;
        _store = store;
        _options = options;
        _environment = environment;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_environment.IsProduction())
            return;
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            var options = _options.CurrentValue;
            if (options.Enabled)
            {
                try
                {
                    var targets = await _catalog.LoadAsync(stoppingToken);
                    var results = new List<NetworkProbeResult>();
                    foreach (var target in targets.Where(t => !_halted.Contains(t.Key)))
                        results.Add(await ProbeAsync(target, stoppingToken));
                    await _store.SaveProbeResultsAsync(results, stoppingToken);
                    if (_lastCleanup != DateOnly.FromDateTime(DateTime.Today))
                    {
                        _lastCleanup = DateOnly.FromDateTime(DateTime.Today);
                        await _store.CleanupAsync(options.RetentionDays, stoppingToken);
                    }
                    // Verlauf AD: einmal je Tag ab 06:00, nur mit NetworkProbe:AdEnabled.
                    await _snapshots.TakeIfDueAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Netzwerkpruefung fehlgeschlagen");
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(Math.Clamp(options.IntervalMinutes, 1, 60)), stoppingToken);
        }
    }

    internal async Task<NetworkProbeResult> ProbeAsync(NetworkTarget target, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        string ips;
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(target.Host, ct).WaitAsync(Timeout, ct);
            ips = string.Join(",", addresses.Select(a => a.ToString()).Take(4));
        }
        catch (Exception ex)
        {
            return new NetworkProbeResult(now, target.Key, false, null, $"dns=Fehler;error={Short(ex)}");
        }

        var watch = Stopwatch.StartNew();
        try
        {
            switch (target.Kind)
            {
                case NetworkProbeKind.Http:
                {
                    using var client = new HttpClient(new HttpClientHandler { UseDefaultCredentials = false, AllowAutoRedirect = false }) { Timeout = Timeout };
                    using var response = await client.GetAsync(target.Url, ct);
                    var ms = watch.Elapsed.TotalMilliseconds;
                    var code = (int)response.StatusCode;
                    if (code is 401 or 403)
                    {
                        _halted.Add(target.Key);
                        return new NetworkProbeResult(now, target.Key, false, ms, $"ip={ips};http={code};angehalten=ja");
                    }
                    return new NetworkProbeResult(now, target.Key, response.IsSuccessStatusCode, ms, $"ip={ips};http={code};tls=nein");
                }
                case NetworkProbeKind.Tls:
                {
                    using var tcp = new TcpClient();
                    await tcp.ConnectAsync(target.Host, target.Port, ct).AsTask().WaitAsync(Timeout, ct);
                    X509Certificate2? certificate = null;
                    var errors = SslPolicyErrors.None;
                    await using var ssl = new SslStream(tcp.GetStream(), false, (_, cert, _, e) =>
                    {
                        certificate = cert is null ? null : new X509Certificate2(cert);
                        errors = e;
                        return true; // nur lesen: das Zertifikat wird angezeigt, nicht vertraut
                    });
                    await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions { TargetHost = target.Host }, ct).WaitAsync(Timeout, ct);
                    var ms = watch.Elapsed.TotalMilliseconds;
                    var cert = certificate is null ? "" :
                        $";notAfter={certificate.NotAfter.ToUniversalTime():yyyy-MM-dd};subject={Clean(certificate.GetNameInfo(X509NameType.SimpleName, false))};issuer={Clean(certificate.GetNameInfo(X509NameType.SimpleName, true))};tlsErrors={errors}";
                    return new NetworkProbeResult(now, target.Key, true, ms, $"ip={ips};tls=ja{cert}");
                }
                default:
                {
                    using var tcp = new TcpClient();
                    await tcp.ConnectAsync(target.Host, target.Port, ct).AsTask().WaitAsync(Timeout, ct);
                    return new NetworkProbeResult(now, target.Key, true, watch.Elapsed.TotalMilliseconds, $"ip={ips};tcp={target.Port}");
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new NetworkProbeResult(now, target.Key, false, null, $"ip={ips};error={Short(ex)}");
        }
    }

    private static string Short(Exception ex)
        => Clean(ex is TimeoutException ? "Zeitueberschreitung" : ex.GetBaseException().Message);

    private static string Clean(string value) => value.Replace(';', ',').Replace('=', ':').Trim();
}
