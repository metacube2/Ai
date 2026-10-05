using System.DirectoryServices;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Liest die AD-Infrastruktur nur lesend: Domaene (Funktionsebene, Papierkorb, krbtgt-Passwortalter,
/// Passwortrichtlinien, Vertrauensstellungen), Standorte und Standortverknuepfungen, Replikation je DC
/// (konstruiertes Attribut msDS-NCReplInboundNeighbors per LDAP an den DC, je DC hoechstens 10 s),
/// Zertifizierungsstellen, Zertifikate der Server mit HTTP-Dienstnamen (TLS-Handshake 443, je 3 s),
/// Dienstkonten (gMSA/sMSA), doppelte SPNs und Drucker. Keine Personen, keine Geheimnisse.
/// Nur mit <c>NetworkProbe:AdEnabled</c>; Ergebnis 30 Minuten gemerkt. Doku docs/NETZWERK_2026-10-02.md.
/// </summary>
public sealed class AdInfrastructureService
{
    private static readonly TimeSpan CacheTime = TimeSpan.FromMinutes(30);

    /// <summary>Fehlerergebnisse hoechstens eine Minute merken, erfolgreiche so lange wie <paramref name="normal"/>.</summary>
    internal static TimeSpan CacheWindow(string? error, TimeSpan normal) => error is null ? normal : TimeSpan.FromMinutes(1);

    /// <summary>System.DirectoryServices liefert Datumswerte als UTC mit Kind Unspecified; ToUniversalTime wuerde sie als Ortszeit lesen.</summary>
    internal static DateTime ToUtc(DateTime d) => d.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(d, DateTimeKind.Utc) : d.ToUniversalTime();
    private readonly AdComputerService _ad;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private AdInfraResult? _cache;

    public AdInfrastructureService(AdComputerService ad)
    {
        _ad = ad;
    }

    public async Task<AdInfraResult> GetAsync()
    {
        if (!_ad.IsEnabled)
            return new AdInfraResult { Enabled = false };
        if (!OperatingSystem.IsWindows())
            return new AdInfraResult { Enabled = true, Error = "Nur auf Windows-Servern moeglich." };

        await _gate.WaitAsync();
        try
        {
            if (_cache is { ReadAt: { } at } && at > DateTime.Now - CacheWindow(_cache.Error, CacheTime))
                return _cache;
            var computers = await _ad.GetAsync();
            _cache = await ReadAsync(computers);
            return _cache;
        }
        finally
        {
            _gate.Release();
        }
    }

    [SupportedOSPlatform("windows")]
    private static async Task<AdInfraResult> ReadAsync(AdComputerResult computers)
    {
        try
        {
            var notes = new List<string>();
            var (defaultNc, configNc, schemaNc) = await Task.Run(NamingContexts);
            var domain = await Task.Run(() => ReadDomain(defaultNc, configNc, schemaNc, notes));
            var dcs = computers.Computers.Where(c => c.IsDomainController && c.Enabled && c.DnsHostName.Length > 0).ToList();
            var (sites, links) = await Task.Run(() => ReadSites(configNc, notes, dcs));
            var dcSite = sites.SelectMany(s => s.Dcs.Select(d => (Dc: d, Site: s.Name))).GroupBy(x => x.Dc, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Site, StringComparer.OrdinalIgnoreCase);
            var replication = await Task.WhenAll(dcs.Select(dc => ReadReplicationAsync(dc, dcSite.GetValueOrDefault(dc.Name) ?? "", [defaultNc, configNc, schemaNc])));
            var cas = await Task.Run(() => ReadCas(configNc, notes));
            var webHosts = computers.Computers
                .Where(c => c.DnsHostName.Length > 0 && AdAnalysis.IsActiveRecently(c, DateTime.UtcNow)
                            && c.Spns.Any(s => s.StartsWith("HTTP/", StringComparison.OrdinalIgnoreCase)))
                .Select(c => c.DnsHostName).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(h => h).Take(40).ToList();
            var webCerts = await ReadWebCertsAsync(webHosts);
            var serviceAccounts = await Task.Run(() => ReadServiceAccounts(notes));
            var spnDuplicates = AdInfraAnalysis.SpnDuplicates(
                computers.Computers.Select(c => (c.Name, (IEnumerable<string>)c.Spns))
                    .Concat(serviceAccounts.Spns.Select(x => (x.Owner, (IEnumerable<string>)x.Spns))));
            var printers = await Task.Run(() => ReadPrinters(notes));

            return new AdInfraResult
            {
                Enabled = true,
                ReadAt = DateTime.Now,
                Domain = domain,
                Sites = sites,
                SiteLinks = links,
                Replication = replication.OrderBy(r => r.Dc).ToList(),
                CertificateAuthorities = cas,
                WebCertificates = webCerts,
                ServiceAccounts = serviceAccounts.Accounts,
                SpnDuplicates = spnDuplicates,
                Printers = printers,
                Notes = notes
            };
        }
        catch (Exception ex)
        {
            return new AdInfraResult { Enabled = true, ReadAt = DateTime.Now, Error = ex.Message };
        }
    }

    [SupportedOSPlatform("windows")]
    internal static (string Default, string Config, string Schema) NamingContexts()
    {
        using var rootDse = new DirectoryEntry("LDAP://RootDSE");
        string P(string n) => Convert.ToString(rootDse.Properties[n].Value) ?? "";
        return (P("defaultNamingContext"), P("configurationNamingContext"), P("schemaNamingContext"));
    }

    [SupportedOSPlatform("windows")]
    internal static List<SearchResult> Search(string path, string filter, string[] props, SearchScope scope = SearchScope.Subtree)
    {
        using var root = new DirectoryEntry(path);
        using var searcher = new DirectorySearcher(root, filter, props, scope) { PageSize = scope == SearchScope.Base ? 0 : 1000 };
        using var results = searcher.FindAll();
        return results.Cast<SearchResult>().ToList();
    }

    internal static string Text(SearchResult r, string p) => r.Properties[p].Count > 0 ? Convert.ToString(r.Properties[p][0]) ?? "" : "";

    internal static int? Int(SearchResult r, string p)
        => r.Properties[p].Count > 0 && int.TryParse(Convert.ToString(r.Properties[p][0]), out var v) ? v : null;

    internal static long? Long(SearchResult r, string p) => r.Properties[p].Count > 0 && r.Properties[p][0] is long l ? l : null;

    internal static DateTime? FileTime(SearchResult r, string p)
        => Long(r, p) is { } ft && ft > 0 && ft < DateTime.MaxValue.ToFileTimeUtc() ? DateTime.FromFileTimeUtc(ft) : null;

    internal static DateTime? Date(SearchResult r, string p) => r.Properties[p].Count > 0 && r.Properties[p][0] is DateTime d ? ToUtc(d) : null;

    /// <summary>GPO-Version (versionNumber): vorzeichenlose 32 Bit, AD liefert sie als int und damit ab 32768 Benutzerversionen negativ.</summary>
    internal static long? Version(SearchResult r, string p)
        => r.Properties[p].Count == 0 ? null : r.Properties[p][0] switch { int i => (long)unchecked((uint)i), long l => l, var o => long.TryParse(Convert.ToString(o), out var v) ? v : null };

    [SupportedOSPlatform("windows")]
    private static AdDomainInfo ReadDomain(string defaultNc, string configNc, string schemaNc, List<string> notes)
    {
        int? domainLevel = null, forestLevel = null, schemaVersion = null, tombstone = null, quota = null;
        bool? recycle = null;
        var krbtgt = new List<AdKrbtgt>();
        var policies = new List<AdPasswordPolicy>();
        var trusts = new List<AdTrust>();
        var name = string.Join(".", defaultNc.Split(',').Select(p => p.Trim()).Where(p => p.StartsWith("DC=", StringComparison.OrdinalIgnoreCase)).Select(p => p[3..]));

        void Try(string what, Action a)
        {
            try { a(); } catch (Exception ex) { notes.Add($"{what}: {ex.Message}"); }
        }

        Try("Funktionsebene", () =>
        {
            using var rootDse = new DirectoryEntry("LDAP://RootDSE");
            domainLevel = int.TryParse(Convert.ToString(rootDse.Properties["domainFunctionality"].Value), out var d) ? d : null;
            forestLevel = int.TryParse(Convert.ToString(rootDse.Properties["forestFunctionality"].Value), out var f) ? f : null;
        });
        Try("Schema", () => schemaVersion = Search($"LDAP://{schemaNc}", "(objectClass=*)", ["objectVersion"], SearchScope.Base).Select(r => Int(r, "objectVersion")).FirstOrDefault());
        Try("Papierkorb", () => recycle = Search($"LDAP://CN=Recycle Bin Feature,CN=Optional Features,CN=Directory Service,CN=Windows NT,CN=Services,{configNc}",
            "(objectClass=*)", ["msDS-EnabledFeatureBL"], SearchScope.Base).Any(r => r.Properties["msDS-EnabledFeatureBL"].Count > 0));
        Try("Tombstone", () => tombstone = Search($"LDAP://CN=Directory Service,CN=Windows NT,CN=Services,{configNc}", "(objectClass=*)", ["tombstoneLifetime"], SearchScope.Base)
            .Select(r => Int(r, "tombstoneLifetime")).FirstOrDefault());

        Try("Domaenenrichtlinie", () =>
        {
            var r = Search($"LDAP://{defaultNc}", "(objectClass=*)",
                ["ms-DS-MachineAccountQuota", "minPwdLength", "pwdHistoryLength", "maxPwdAge", "lockoutThreshold", "pwdProperties"], SearchScope.Base).First();
            quota = Int(r, "ms-DS-MachineAccountQuota");
            var maxAge = Long(r, "maxPwdAge");
            policies.Add(new AdPasswordPolicy("Domänenrichtlinie", null, Int(r, "minPwdLength") ?? 0, Int(r, "pwdHistoryLength") ?? 0,
                maxAge is { } m && m != long.MinValue && m != 0 ? TimeSpan.FromTicks(-m).TotalDays : null,
                Int(r, "lockoutThreshold") ?? 0, ((Int(r, "pwdProperties") ?? 0) & 1) != 0, 0));
        });
        // krbtgt ist ein Systemkonto der Domaene, keine Person: nur Name und Passwortalter.
        Try("krbtgt", () => krbtgt.AddRange(Search($"LDAP://{defaultNc}", "(|(sAMAccountName=krbtgt)(sAMAccountName=krbtgt_*))", ["sAMAccountName", "pwdLastSet"])
            .Select(r => new AdKrbtgt(Text(r, "sAMAccountName"), FileTime(r, "pwdLastSet")))));
        Try("Feinkoernige Passwortrichtlinien", () => policies.AddRange(Search($"LDAP://CN=Password Settings Container,CN=System,{defaultNc}", "(objectClass=msDS-PasswordSettings)",
                ["name", "msDS-PasswordSettingsPrecedence", "msDS-MinimumPasswordLength", "msDS-PasswordHistoryLength", "msDS-MaximumPasswordAge",
                 "msDS-LockoutThreshold", "msDS-PasswordComplexityEnabled", "msDS-PSOAppliesTo"])
            .Select(r => new AdPasswordPolicy(Text(r, "name"), Int(r, "msDS-PasswordSettingsPrecedence"), Int(r, "msDS-MinimumPasswordLength") ?? 0,
                Int(r, "msDS-PasswordHistoryLength") ?? 0, Long(r, "msDS-MaximumPasswordAge") is { } m && m != 0 && m != long.MinValue ? TimeSpan.FromTicks(-m).TotalDays : null,
                Int(r, "msDS-LockoutThreshold") ?? 0, string.Equals(Text(r, "msDS-PasswordComplexityEnabled"), "True", StringComparison.OrdinalIgnoreCase),
                r.Properties["msDS-PSOAppliesTo"].Count))));
        Try("Vertrauensstellungen", () => trusts.AddRange(Search($"LDAP://CN=System,{defaultNc}", "(objectClass=trustedDomain)",
                ["trustPartner", "name", "trustDirection", "trustType", "trustAttributes"])
            .Select(r => new AdTrust(Text(r, "trustPartner") is { Length: > 0 } t ? t : Text(r, "name"),
                AdInfraAnalysis.TrustDirection(Int(r, "trustDirection") ?? 0), AdInfraAnalysis.TrustType(Int(r, "trustType") ?? 0), Int(r, "trustAttributes") ?? 0))));

        return new AdDomainInfo
        {
            DomainName = name,
            DomainLevel = domainLevel,
            ForestLevel = forestLevel,
            SchemaVersion = schemaVersion,
            RecycleBin = recycle,
            TombstoneDays = tombstone,
            MachineAccountQuota = quota,
            Krbtgt = krbtgt.OrderBy(k => k.Name).ToList(),
            PasswordPolicies = policies,
            Trusts = trusts.OrderBy(t => t.Name).ToList()
        };
    }

    [SupportedOSPlatform("windows")]
    private static (IReadOnlyList<AdSiteInfo>, IReadOnlyList<AdSiteLinkInfo>) ReadSites(string configNc, List<string> notes, IReadOnlyList<AdComputer> dcs)
    {
        try
        {
            var sitesPath = $"LDAP://CN=Sites,{configNc}";
            // Nur Serverobjekte, die auf ein vorhandenes, aktiviertes DC-Computerkonto verweisen (serverReference);
            // verwaiste Serverobjekte alter DCs zaehlen sonst als DC des Standorts.
            var dcDns = dcs.Select(d => $"CN={d.Name},{d.Container}").ToHashSet(StringComparer.OrdinalIgnoreCase);
            var servers = Search(sitesPath, "(objectClass=server)", ["name", "distinguishedName", "serverReference"])
                .Where(r => dcDns.Count == 0 || dcDns.Contains(Text(r, "serverReference")))
                .Select(r => (Name: Text(r, "name"), Site: AdAnalysis.FirstRdnValue(AdAnalysis.ParentDn(AdAnalysis.ParentDn(Text(r, "distinguishedName"))))))
                .ToList();
            var subnets = Search($"LDAP://CN=Subnets,CN=Sites,{configNc}", "(objectClass=subnet)", ["name", "siteObject"])
                .Select(r => (Cidr: Text(r, "name"), Site: AdAnalysis.FirstRdnValue(Text(r, "siteObject"))))
                .ToList();
            var sites = Search(sitesPath, "(objectClass=site)", ["name"], SearchScope.OneLevel)
                .Select(r => Text(r, "name"))
                .Select(s => new AdSiteInfo(s,
                    servers.Where(x => x.Site.Equals(s, StringComparison.OrdinalIgnoreCase)).Select(x => x.Name).OrderBy(x => x).ToList(),
                    subnets.Where(x => x.Site.Equals(s, StringComparison.OrdinalIgnoreCase)).Select(x => x.Cidr).OrderBy(x => x).ToList()))
                .OrderBy(s => s.Name)
                .ToList();
            var links = Search($"LDAP://CN=Inter-Site Transports,CN=Sites,{configNc}", "(objectClass=siteLink)", ["name", "cost", "replInterval", "siteList"])
                .Select(r => new AdSiteLinkInfo(Text(r, "name"), Int(r, "cost") ?? 0, Int(r, "replInterval") ?? 0,
                    r.Properties["siteList"].Cast<object>().Select(x => AdAnalysis.FirstRdnValue(Convert.ToString(x) ?? "")).OrderBy(x => x).ToList()))
                .OrderBy(l => l.Name)
                .ToList();
            return (sites, links);
        }
        catch (Exception ex)
        {
            notes.Add("Standorte: " + ex.Message);
            return ([], []);
        }
    }

    /// <summary>Fragt beim DC selbst die eingehenden Replikationspartner je Partition ab (nur LDAP 389, hoechstens 10 s).</summary>
    [SupportedOSPlatform("windows")]
    private static async Task<AdDcReplication> ReadReplicationAsync(AdComputer dc, string site, string[] partitions)
    {
        try
        {
            var links = await Task.Run(() =>
            {
                var list = new List<AdReplicationLink>();
                foreach (var nc in partitions.Where(p => p.Length > 0))
                {
                    var r = Search($"LDAP://{dc.DnsHostName}/{nc}", "(objectClass=*)", ["msDS-NCReplInboundNeighbors"], SearchScope.Base).FirstOrDefault();
                    if (r is null) continue;
                    foreach (var xml in r.Properties["msDS-NCReplInboundNeighbors"].Cast<object>().Select(x => Convert.ToString(x) ?? ""))
                        if (AdInfraAnalysis.ParseReplNeighbor(xml, dc.Name) is { } l)
                            list.Add(l);
                }
                return list;
            }).WaitAsync(TimeSpan.FromSeconds(10));
            return new AdDcReplication(dc.Name, site, null, links);
        }
        catch (TimeoutException)
        {
            return new AdDcReplication(dc.Name, site, "Keine Antwort in 10 s", []);
        }
        catch (Exception ex)
        {
            return new AdDcReplication(dc.Name, site, ex.Message, []);
        }
    }

    [SupportedOSPlatform("windows")]
    private static IReadOnlyList<AdCaInfo> ReadCas(string configNc, List<string> notes)
    {
        var list = new List<AdCaInfo>();
        var pki = $"CN=Public Key Services,CN=Services,{configNc}";
        try
        {
            foreach (var r in Search($"LDAP://CN=Enrollment Services,{pki}", "(objectClass=pKIEnrollmentService)", ["name", "dNSHostName", "cACertificate", "certificateTemplates"]))
                list.AddRange(Certs(r).DefaultIfEmpty(null).Select(c => new AdCaInfo(Text(r, "name"), Text(r, "dNSHostName"), "Ausstellende CA",
                    c?.Subject ?? "", c?.NotAfter.ToUniversalTime(), r.Properties["certificateTemplates"].Count)));
            foreach (var r in Search($"LDAP://CN=Certification Authorities,{pki}", "(objectClass=certificationAuthority)", ["name", "cACertificate"]))
                list.AddRange(Certs(r).Select(c => new AdCaInfo(Text(r, "name"), "", "Stamm-CA", c.Subject, c.NotAfter.ToUniversalTime(), 0)));
        }
        catch (Exception ex)
        {
            notes.Add("Zertifizierungsstellen: " + ex.Message);
        }
        return list.OrderBy(c => c.NotAfterUtc ?? DateTime.MaxValue).ToList();

        static IEnumerable<X509Certificate2> Certs(SearchResult r)
        {
            foreach (var raw in r.Properties["cACertificate"].Cast<object>().OfType<byte[]>())
            {
                X509Certificate2? cert = null;
                try { cert = new X509Certificate2(raw); } catch { }
                if (cert is not null) yield return cert;
            }
        }
    }

    /// <summary>TLS-Handshake auf 443 zu Servern mit HTTP-Dienstnamen; Zertifikat wird nur gelesen, nicht geprueft.</summary>
    private static async Task<IReadOnlyList<AdWebCert>> ReadWebCertsAsync(IReadOnlyList<string> hosts)
    {
        using var limit = new SemaphoreSlim(8);
        var tasks = hosts.Select(async host =>
        {
            await limit.WaitAsync();
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                using var tcp = new TcpClient();
                await tcp.ConnectAsync(host, 443, cts.Token);
                await using var ssl = new SslStream(tcp.GetStream(), false, (_, _, _, _) => true);
                await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions { TargetHost = host }, cts.Token);
                var cert = ssl.RemoteCertificate is null ? null : new X509Certificate2(ssl.RemoteCertificate);
                return new AdWebCert(host, cert?.Subject ?? "", cert?.Issuer ?? "", cert?.NotAfter.ToUniversalTime(), cert is null ? "kein Zertifikat" : null);
            }
            catch (Exception ex)
            {
                return new AdWebCert(host, "", "", null, ex is OperationCanceledException ? "keine Antwort auf 443" : ex.GetBaseException().Message);
            }
            finally
            {
                limit.Release();
            }
        });
        return (await Task.WhenAll(tasks)).OrderBy(c => c.NotAfterUtc ?? DateTime.MaxValue).ToList();
    }

    [SupportedOSPlatform("windows")]
    private static (IReadOnlyList<AdServiceAccount> Accounts, IReadOnlyList<(string Owner, IReadOnlyList<string> Spns)> Spns) ReadServiceAccounts(List<string> notes)
    {
        try
        {
            var rows = Search("LDAP://" + NamingContexts().Default,
                "(|(objectClass=msDS-GroupManagedServiceAccount)(objectClass=msDS-ManagedServiceAccount))",
                ["name", "objectClass", "pwdLastSet", "msDS-ManagedPasswordInterval", "servicePrincipalName", "whenCreated"]);
            var accounts = rows.Select(r => new AdServiceAccount(Text(r, "name"),
                    r.Properties["objectClass"].Cast<object>().Any(o => string.Equals(Convert.ToString(o), "msDS-GroupManagedServiceAccount", StringComparison.OrdinalIgnoreCase)) ? "gMSA" : "sMSA",
                    FileTime(r, "pwdLastSet"), Int(r, "msDS-ManagedPasswordInterval"), r.Properties["servicePrincipalName"].Count, Date(r, "whenCreated")))
                .OrderBy(a => a.Name).ToList();
            var spns = rows.Select(r => (Text(r, "name"), (IReadOnlyList<string>)r.Properties["servicePrincipalName"].Cast<object>().Select(x => Convert.ToString(x) ?? "").ToList())).ToList();
            return (accounts, spns);
        }
        catch (Exception ex)
        {
            notes.Add("Dienstkonten: " + ex.Message);
            return ([], []);
        }
    }

    [SupportedOSPlatform("windows")]
    private static IReadOnlyList<AdPrinter> ReadPrinters(List<string> notes)
    {
        try
        {
            return Search("LDAP://" + NamingContexts().Default, "(objectCategory=printQueue)",
                    ["printerName", "serverName", "location", "driverName", "printColor", "printDuplexSupported", "whenChanged"])
                .Select(r => new AdPrinter(Text(r, "printerName"), Text(r, "serverName"), Text(r, "location"), Text(r, "driverName"),
                    string.Equals(Text(r, "printColor"), "True", StringComparison.OrdinalIgnoreCase),
                    string.Equals(Text(r, "printDuplexSupported"), "True", StringComparison.OrdinalIgnoreCase), Date(r, "whenChanged")))
                .OrderBy(p => p.Server).ThenBy(p => p.Name)
                .ToList();
        }
        catch (Exception ex)
        {
            notes.Add("Drucker: " + ex.Message);
            return [];
        }
    }
}
