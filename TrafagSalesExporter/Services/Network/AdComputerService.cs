using System.DirectoryServices;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Options;

namespace TrafagSalesExporter.Services;

/// <summary>Ein Computerkonto aus dem AD. Nie Personen, nie Passwoerter oder Schluessel selbst.</summary>
public sealed record AdComputer(string Name, string OperatingSystem, DateTime? LastLogonUtc, DateTime? CreatedUtc, bool Enabled)
{
    public string DnsHostName { get; init; } = string.Empty;
    public string OsVersion { get; init; } = string.Empty;
    public DateTime? ChangedUtc { get; init; }
    public DateTime? PwdLastSetUtc { get; init; }
    /// <summary>Distinguished Name des Containers, in dem das Konto liegt (OU oder CN=Computers).</summary>
    public string Container { get; init; } = string.Empty;
    /// <summary>Dienstklassen aus servicePrincipalName (MSSQLSvc, HTTP, TERMSRV ...), ohne HOST.</summary>
    public IReadOnlyList<string> Services { get; init; } = [];
    /// <summary>Vollstaendige servicePrincipalName-Eintraege (fuer doppelte SPNs).</summary>
    public IReadOnlyList<string> Spns { get; init; } = [];
    public bool IsDomainController { get; init; }
    /// <summary>userAccountControl TRUSTED_FOR_DELEGATION (0x80000).</summary>
    public bool UnconstrainedDelegation { get; init; }
    /// <summary>userAccountControl TRUSTED_TO_AUTH_FOR_DELEGATION (0x1000000).</summary>
    public bool ProtocolTransition { get; init; }
    /// <summary>Anzahl Eintraege in msDS-AllowedToDelegateTo (eingeschraenkte Delegation).</summary>
    public int ConstrainedTargets { get; init; }
    /// <summary>Nur das Ablaufdatum des LAPS-Passworts (alt oder Windows LAPS), nie das Passwort.</summary>
    public DateTime? LapsExpiryUtc { get; init; }
    /// <summary>Anzahl sichtbarer BitLocker-Wiederherstellungsobjekte; der Schluessel selbst wird nicht gelesen.</summary>
    public int BitLockerKeys { get; init; }
}

/// <summary>Ein AD-Subnetz aus der Standortkonfiguration.</summary>
public sealed record AdSubnet(string Cidr, string Site);

/// <summary>Eine OU mit der Angabe, ob direkt eine Gruppenrichtlinie verknuepft ist.</summary>
public sealed record AdOu(string DistinguishedName, bool HasGpoLink);

/// <summary>DNS-Aufloesung eines aktiven Computers.</summary>
public sealed record AdDnsEntry(string Name, string DnsHostName, IReadOnlyList<string> Ips, bool Failed)
{
    /// <summary>IPv6-Adressen; ein Host nur mit IPv6 ist aufloesbar, hat aber keine IPv4 in <see cref="Ips"/>.</summary>
    public IReadOnlyList<string> Ipv6 { get; init; } = [];

    /// <summary>Weder IPv4 noch IPv6 aufloesbar (oder Abfrage fehlgeschlagen).</summary>
    public bool Unresolvable => Failed || (Ips.Count == 0 && Ipv6.Count == 0);
}

public sealed class AdComputerResult
{
    public bool Enabled { get; init; }
    public string? Error { get; init; }
    public DateTime? ReadAt { get; init; }
    public IReadOnlyList<AdComputer> Computers { get; init; } = [];
    public IReadOnlyList<AdSubnet> Subnets { get; init; } = [];
    public IReadOnlyList<AdOu> Ous { get; init; } = [];
    /// <summary>Hinweise, welche Teilabfrage nicht lesbar war (z. B. BitLocker ohne Rechte).</summary>
    public IReadOnlyList<string> Notes { get; init; } = [];
    /// <summary>Bei mindestens einem Computer ist ein LAPS-Ablaufdatum lesbar; sonst fehlt das Leserecht oder LAPS.</summary>
    public bool LapsReadable => Computers.Any(c => c.LapsExpiryUtc is not null);
    /// <summary>Mindestens ein BitLocker-Objekt sichtbar; sonst fehlt das Leserecht oder es liegen keine Schluessel im AD.</summary>
    public bool BitLockerReadable => Computers.Any(c => c.BitLockerKeys > 0);
    /// <summary>Aktiviert, letzte Anmeldung bekannt und aelter als <paramref name="days"/> Tage (gleiche Definition wie <see cref="AdAnalysis.IsInactive"/>).</summary>
    public IEnumerable<AdComputer> Stale(int days) => Computers.Where(c => AdAnalysis.IsInactive(c, DateTime.UtcNow, days));
}

public sealed class AdDnsResult
{
    public DateTime? ReadAt { get; init; }
    public IReadOnlyList<AdDnsEntry> Entries { get; init; } = [];
}

/// <summary>
/// Liest Computerkonten aus dem AD, nur lesend und OHNE Personen (Entscheid Ingo 2026-10-02:
/// „nur AD-Computer ohne Personen“; Erlaubnis der IT laut Ingo am 2026-10-02, eingeschaltet 07:34).
/// Dazu OUs, AD-Subnetze, Ablaufdatum LAPS und die Zahl der BitLocker-Objekte, nie deren Inhalt.
/// Schalter <c>NetworkProbe:AdEnabled</c>. Ergebnis eine Stunde gemerkt. Doku docs/NETZWERK_2026-10-02.md.
/// </summary>
public sealed class AdComputerService
{
    private static readonly string[] ComputerProps =
    [
        "name", "dNSHostName", "operatingSystem", "operatingSystemVersion", "lastLogonTimestamp", "whenCreated", "whenChanged",
        "pwdLastSet", "userAccountControl", "distinguishedName", "servicePrincipalName", "msDS-AllowedToDelegateTo",
        "ms-Mcs-AdmPwdExpirationTime", "msLAPS-PasswordExpirationTime"
    ];

    private readonly IOptionsMonitor<NetworkProbeOptions> _options;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly SemaphoreSlim _dnsGate = new(1, 1);
    private AdComputerResult? _cache;
    private AdDnsResult? _dnsCache;

    public AdComputerService(IOptionsMonitor<NetworkProbeOptions> options)
    {
        _options = options;
    }

    public bool IsEnabled => _options.CurrentValue.AdEnabled;

    public async Task<AdComputerResult> GetAsync()
    {
        if (!_options.CurrentValue.AdEnabled)
            return new AdComputerResult { Enabled = false };
        if (!OperatingSystem.IsWindows())
            return new AdComputerResult { Enabled = true, Error = "Nur auf Windows-Servern moeglich." };

        await _gate.WaitAsync();
        try
        {
            // Fehler hoechstens eine Minute merken, damit ein kurzer Ausfall nicht eine Stunde lang angezeigt wird.
            if (_cache is { ReadAt: { } at } && at > DateTime.Now - AdInfrastructureService.CacheWindow(_cache.Error, TimeSpan.FromHours(1)))
                return _cache;
            _cache = await Task.Run(Read);
            return _cache;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Loest die DNS-Namen der aktiven Computer (Anmeldung in den letzten 44 Tagen, 30 plus Replikationsverzoegerung) auf. Nur Anfragen
    /// an den DNS-Server, kein Zugriff auf die Geraete. Hoechstens 16 gleichzeitig, je 3 s.
    /// </summary>
    public async Task<AdDnsResult> ResolveDnsAsync()
    {
        var ad = await GetAsync();
        if (!ad.Enabled || ad.Error is not null)
            return new AdDnsResult();

        await _dnsGate.WaitAsync();
        try
        {
            if (_dnsCache is { ReadAt: { } at } && at > DateTime.Now.AddHours(-1))
                return _dnsCache;
            var active = ad.Computers
                .Where(c => c.DnsHostName.Length > 0 && AdAnalysis.IsActiveRecently(c, DateTime.UtcNow))
                .ToList();
            using var limit = new SemaphoreSlim(16);
            var tasks = active.Select(async c =>
            {
                await limit.WaitAsync();
                try
                {
                    var ips = await Dns.GetHostAddressesAsync(c.DnsHostName).WaitAsync(TimeSpan.FromSeconds(3));
                    return new AdDnsEntry(c.Name, c.DnsHostName,
                        ips.Where(i => i.AddressFamily == AddressFamily.InterNetwork).Select(i => i.ToString()).Distinct().ToList(), false)
                    {
                        Ipv6 = ips.Where(i => i.AddressFamily == AddressFamily.InterNetworkV6).Select(i => i.ToString()).Distinct().ToList()
                    };
                }
                catch
                {
                    return new AdDnsEntry(c.Name, c.DnsHostName, [], true);
                }
                finally
                {
                    limit.Release();
                }
            });
            _dnsCache = new AdDnsResult { ReadAt = DateTime.Now, Entries = await Task.WhenAll(tasks) };
            return _dnsCache;
        }
        finally
        {
            _dnsGate.Release();
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static AdComputerResult Read()
    {
        try
        {
            var notes = new List<string>();
            using var rootDse = new DirectoryEntry("LDAP://RootDSE");
            var configNc = Convert.ToString(rootDse.Properties["configurationNamingContext"].Value) ?? "";

            var bitLocker = ReadBitLocker(notes);

            using var root = new DirectoryEntry();
            using var searcher = new DirectorySearcher(root, "(objectCategory=computer)", ComputerProps)
            {
                PageSize = 500,
                SizeLimit = 20000
            };
            var list = new List<AdComputer>();
            using (var results = searcher.FindAll())
            {
                foreach (SearchResult r in results)
                {
                    string Text(string p) => r.Properties[p].Count > 0 ? Convert.ToString(r.Properties[p][0]) ?? "" : "";
                    DateTime? FileTime(string p) => r.Properties[p].Count > 0 && r.Properties[p][0] is long ft && ft > 0 && ft < DateTime.MaxValue.ToFileTimeUtc()
                        ? DateTime.FromFileTimeUtc(ft) : null;
                    DateTime? Date(string p) => r.Properties[p].Count > 0 && r.Properties[p][0] is DateTime d ? AdInfrastructureService.ToUtc(d) : null;
                    var uac = r.Properties["userAccountControl"].Count > 0 ? Convert.ToInt32(r.Properties["userAccountControl"][0]) : 0;
                    var dn = Text("distinguishedName");
                    var spns = r.Properties["servicePrincipalName"].Cast<object>().Select(x => Convert.ToString(x) ?? "").Where(x => x.Length > 0).ToList();
                    var services = spns
                        .Select(x => x.Split('/')[0])
                        .Where(x => x.Length > 0 && !x.Equals("HOST", StringComparison.OrdinalIgnoreCase) && !x.Equals("RestrictedKrbHost", StringComparison.OrdinalIgnoreCase))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    list.Add(new AdComputer(Text("name"), Text("operatingSystem"), FileTime("lastLogonTimestamp"), Date("whenCreated"), (uac & 0x2) == 0)
                    {
                        DnsHostName = Text("dNSHostName"),
                        OsVersion = Text("operatingSystemVersion"),
                        ChangedUtc = Date("whenChanged"),
                        PwdLastSetUtc = FileTime("pwdLastSet"),
                        Container = AdAnalysis.ParentDn(dn),
                        Services = services,
                        Spns = spns,
                        // 0x2000 SERVER_TRUST_ACCOUNT (DC), 0x4000000 PARTIAL_SECRETS_ACCOUNT (RODC, z. B. AzureADKerberos).
                        IsDomainController = (uac & 0x2000) != 0 || (uac & 0x4000000) != 0,
                        UnconstrainedDelegation = (uac & 0x80000) != 0,
                        ProtocolTransition = (uac & 0x1000000) != 0,
                        ConstrainedTargets = r.Properties["msDS-AllowedToDelegateTo"].Count,
                        LapsExpiryUtc = FileTime("msLAPS-PasswordExpirationTime") ?? FileTime("ms-Mcs-AdmPwdExpirationTime"),
                        BitLockerKeys = bitLocker.TryGetValue(dn, out var n) ? n : 0
                    });
                }
            }

            return new AdComputerResult
            {
                Enabled = true,
                ReadAt = DateTime.Now,
                Computers = list.OrderBy(x => x.Name).ToList(),
                Ous = ReadOus(notes),
                Subnets = configNc.Length > 0 ? ReadSubnets(configNc, notes) : [],
                Notes = notes
            };
        }
        catch (Exception ex)
        {
            return new AdComputerResult { Enabled = true, ReadAt = DateTime.Now, Error = ex.Message };
        }
    }

    /// <summary>Zaehlt BitLocker-Objekte je Computer. Gelesen wird nur der DN, nie das Wiederherstellungspasswort.</summary>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static Dictionary<string, int> ReadBitLocker(List<string> notes)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var root = new DirectoryEntry();
            using var searcher = new DirectorySearcher(root, "(objectClass=msFVE-RecoveryInformation)", ["distinguishedName"]) { PageSize = 500 };
            using var results = searcher.FindAll();
            foreach (SearchResult r in results)
            {
                var parent = AdAnalysis.ParentDn(r.Properties["distinguishedName"].Count > 0 ? Convert.ToString(r.Properties["distinguishedName"][0]) ?? "" : "");
                result[parent] = result.GetValueOrDefault(parent) + 1;
            }
        }
        catch (Exception ex)
        {
            notes.Add("BitLocker: " + ex.Message);
        }
        return result;
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static IReadOnlyList<AdOu> ReadOus(List<string> notes)
    {
        try
        {
            using var root = new DirectoryEntry();
            using var searcher = new DirectorySearcher(root, "(objectClass=organizationalUnit)", ["distinguishedName", "gPLink"]) { PageSize = 500 };
            using var results = searcher.FindAll();
            return results.Cast<SearchResult>()
                .Select(r => new AdOu(
                    r.Properties["distinguishedName"].Count > 0 ? Convert.ToString(r.Properties["distinguishedName"][0]) ?? "" : "",
                    AdInfraAnalysis.HasActiveGpoLink(r.Properties["gPLink"].Count > 0 ? Convert.ToString(r.Properties["gPLink"][0]) : null)))
                .ToList();
        }
        catch (Exception ex)
        {
            notes.Add("OUs: " + ex.Message);
            return [];
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static IReadOnlyList<AdSubnet> ReadSubnets(string configNc, List<string> notes)
    {
        try
        {
            using var root = new DirectoryEntry($"LDAP://CN=Subnets,CN=Sites,{configNc}");
            using var searcher = new DirectorySearcher(root, "(objectClass=subnet)", ["name", "siteObject"]) { PageSize = 500 };
            using var results = searcher.FindAll();
            return results.Cast<SearchResult>()
                .Select(r => new AdSubnet(
                    r.Properties["name"].Count > 0 ? Convert.ToString(r.Properties["name"][0]) ?? "" : "",
                    AdAnalysis.FirstRdnValue(r.Properties["siteObject"].Count > 0 ? Convert.ToString(r.Properties["siteObject"][0]) ?? "" : "")))
                .Where(x => x.Cidr.Length > 0)
                .OrderBy(x => x.Cidr)
                .ToList();
        }
        catch (Exception ex)
        {
            notes.Add("Subnetze: " + ex.Message);
            return [];
        }
    }
}
