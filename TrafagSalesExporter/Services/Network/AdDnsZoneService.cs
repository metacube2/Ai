using System.DirectoryServices;
using System.Runtime.Versioning;

namespace TrafagSalesExporter.Services;

/// <summary>
/// AD-integrierte DNS-Zonen nur lesend (DomainDnsZones, ForestDnsZones, CN=MicrosoftDNS,CN=System):
/// Alterungseinstellung der Zone (dnsProperty), statische und dynamische Eintraege, Alter der Zeitstempel,
/// Kandidaten fuer Scavenging und doppelte A-Eintraege. Nur mit <c>NetworkProbe:AdEnabled</c>; 1 Stunde gemerkt.
/// Doku docs/NETZWERK_2026-10-02.md.
/// </summary>
public sealed class AdDnsZoneService
{
    private const int DefaultIntervalHours = 168;
    private readonly AdComputerService _ad;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private AdDnsZoneResult? _cache;

    public AdDnsZoneService(AdComputerService ad)
    {
        _ad = ad;
    }

    public async Task<AdDnsZoneResult> GetAsync()
    {
        if (!_ad.IsEnabled)
            return new AdDnsZoneResult { Enabled = false };
        if (!OperatingSystem.IsWindows())
            return new AdDnsZoneResult { Enabled = true, Error = "Nur auf Windows-Servern moeglich." };
        await _gate.WaitAsync();
        try
        {
            if (_cache is { ReadAt: { } at } && at > DateTime.Now.AddHours(-1))
                return _cache;
            _cache = await Task.Run(Read);
            return _cache;
        }
        finally
        {
            _gate.Release();
        }
    }

    [SupportedOSPlatform("windows")]
    private static AdDnsZoneResult Read()
    {
        try
        {
            var notes = new List<string>();
            var (defaultNc, configNc, _) = AdInfrastructureService.NamingContexts();
            var forestNc = configNc.StartsWith("CN=Configuration,", StringComparison.OrdinalIgnoreCase) ? configNc["CN=Configuration,".Length..] : defaultNc;
            var partitions = new[]
            {
                ("DomainDnsZones", $"DC=DomainDnsZones,{defaultNc}"),
                ("ForestDnsZones", $"DC=ForestDnsZones,{forestNc}"),
                ("System (alt)", $"CN=MicrosoftDNS,CN=System,{defaultNc}")
            };
            var now = DateTime.UtcNow;
            var zones = new List<AdDnsZone>();
            var aRecords = new List<(string Ip, string Name)>();
            var oldest = new List<(string Name, string Zone, DateTime StampUtc)>();

            foreach (var (label, path) in partitions)
            {
                List<SearchResult> zoneRows;
                try
                {
                    zoneRows = AdInfrastructureService.Search($"LDAP://{path}", "(objectClass=dnsZone)", ["name", "distinguishedName", "dnsProperty"]);
                }
                catch (Exception ex)
                {
                    if (!label.StartsWith("System", StringComparison.Ordinal))
                        notes.Add($"{label}: {ex.Message}");
                    continue;
                }
                foreach (var z in zoneRows)
                {
                    var name = AdInfrastructureService.Text(z, "name");
                    if (name is "RootDNSServers" or "..TrustAnchors" || name.Length == 0)
                        continue;
                    bool? aging = null;
                    int? noRefresh = null, refresh = null;
                    foreach (var raw in z.Properties["dnsProperty"].Cast<object>().OfType<byte[]>())
                    {
                        if (AdInfraAnalysis.ParseDnsProperty(raw) is not { } p) continue;
                        if (p.Id == AdInfraAnalysis.DnsZoneAgingState) aging = p.Value != 0;
                        else if (p.Id == AdInfraAnalysis.DnsZoneNoRefresh) noRefresh = (int)p.Value;
                        else if (p.Id == AdInfraAnalysis.DnsZoneRefresh) refresh = (int)p.Value;
                    }
                    var staleBefore = now.AddHours(-((noRefresh ?? DefaultIntervalHours) + (refresh ?? DefaultIntervalHours)));
                    int nodes = 0, statics = 0, dynamics = 0, tomb = 0, stale = 0;
                    var buckets = new int[5];
                    foreach (var n in AdInfrastructureService.Search($"LDAP://{AdInfrastructureService.Text(z, "distinguishedName")}", "(objectClass=dnsNode)", ["name", "dnsRecord", "dNSTombstoned"]))
                    {
                        nodes++;
                        var host = AdInfrastructureService.Text(n, "name");
                        if (string.Equals(AdInfrastructureService.Text(n, "dNSTombstoned"), "True", StringComparison.OrdinalIgnoreCase))
                        {
                            tomb++;
                            continue;
                        }
                        foreach (var rec in n.Properties["dnsRecord"].Cast<object>().OfType<byte[]>().Select(AdInfraAnalysis.ParseDnsRecord).Where(r => r is not null))
                        {
                            if (rec!.Type == 0) continue;
                            if (rec.IsStatic) statics++;
                            else
                            {
                                dynamics++;
                                var stamp = rec.StampUtc!.Value;
                                var age = (now - stamp).TotalDays;
                                buckets[age <= 7 ? 0 : age <= 30 ? 1 : age <= 90 ? 2 : age <= 365 ? 3 : 4]++;
                                if (stamp < staleBefore)
                                {
                                    stale++;
                                    oldest.Add(($"{host}.{name}", name, stamp));
                                }
                            }
                            if (rec.IPv4 is { } ip && host != "@" && !host.StartsWith('_') && !host.Contains("DnsZones", StringComparison.OrdinalIgnoreCase)
                                && !name.StartsWith("_msdcs", StringComparison.OrdinalIgnoreCase))
                                aRecords.Add((ip, $"{host}.{name}"));
                        }
                    }
                    zones.Add(new AdDnsZone
                    {
                        Name = name, Partition = label, Aging = aging, NoRefreshHours = noRefresh, RefreshHours = refresh,
                        Nodes = nodes, Static = statics, Dynamic = dynamics, Tombstoned = tomb, Stale = stale, AgeBuckets = buckets
                    });
                }
            }

            var duplicates = aRecords.GroupBy(a => a.Ip)
                .Select(g => new AdDnsDuplicateName(g.Key, g.Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList()))
                .Where(d => d.Names.Count > 1)
                .OrderByDescending(d => d.Names.Count).ThenBy(d => d.Ip)
                .ToList();

            return new AdDnsZoneResult
            {
                Enabled = true,
                ReadAt = DateTime.Now,
                Zones = zones.OrderByDescending(z => z.Nodes).ToList(),
                DuplicateA = duplicates,
                OldestDynamic = oldest.OrderBy(o => o.StampUtc).Take(200).ToList(),
                Notes = notes
            };
        }
        catch (Exception ex)
        {
            return new AdDnsZoneResult { Enabled = true, ReadAt = DateTime.Now, Error = ex.Message };
        }
    }
}
