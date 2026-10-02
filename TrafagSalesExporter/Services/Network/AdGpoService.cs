using System.DirectoryServices;
using System.Runtime.Versioning;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Gruppenrichtlinien nur lesend: GPO-Objekte aus CN=Policies,CN=System, Verknuepfungen (gPLink) an Domaene,
/// OUs und Standorten, Vererbungssperre (gPOptions = 1) und die Version in GPT.INI im SYSVOL (Dateilesen,
/// nach drei Fehlern abgebrochen). Nur mit <c>NetworkProbe:AdEnabled</c>; 30 Minuten gemerkt.
/// Doku docs/NETZWERK_2026-10-02.md.
/// </summary>
public sealed class AdGpoService
{
    private readonly AdComputerService _ad;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private AdGpoResult? _cache;

    public AdGpoService(AdComputerService ad)
    {
        _ad = ad;
    }

    public async Task<AdGpoResult> GetAsync()
    {
        if (!_ad.IsEnabled)
            return new AdGpoResult { Enabled = false };
        if (!OperatingSystem.IsWindows())
            return new AdGpoResult { Enabled = true, Error = "Nur auf Windows-Servern moeglich." };
        await _gate.WaitAsync();
        try
        {
            if (_cache is { ReadAt: { } at } && at > DateTime.Now.AddMinutes(-30))
                return _cache;
            var computers = await _ad.GetAsync();
            _cache = await Task.Run(() => Read(computers));
            return _cache;
        }
        finally
        {
            _gate.Release();
        }
    }

    [SupportedOSPlatform("windows")]
    private static AdGpoResult Read(AdComputerResult computers)
    {
        try
        {
            var notes = new List<string>();
            var (defaultNc, configNc, _) = AdInfrastructureService.NamingContexts();

            // Verknuepfungen sammeln: Domaene, OUs, Standorte.
            var targets = new List<(string Dn, string Label, string Kind, string GpLink, int Options)>();
            foreach (var r in AdInfrastructureService.Search($"LDAP://{defaultNc}", "(objectClass=*)", ["gPLink", "gPOptions"], SearchScope.Base))
                targets.Add((defaultNc, "(Domäne)", "Domäne", AdInfrastructureService.Text(r, "gPLink"), AdInfrastructureService.Int(r, "gPOptions") ?? 0));
            foreach (var r in AdInfrastructureService.Search($"LDAP://{defaultNc}", "(objectClass=organizationalUnit)", ["distinguishedName", "gPLink", "gPOptions"]))
            {
                var dn = AdInfrastructureService.Text(r, "distinguishedName");
                targets.Add((dn, AdAnalysis.ContainerLabel(dn), "OU", AdInfrastructureService.Text(r, "gPLink"), AdInfrastructureService.Int(r, "gPOptions") ?? 0));
            }
            try
            {
                foreach (var r in AdInfrastructureService.Search($"LDAP://CN=Sites,{configNc}", "(objectClass=site)", ["name", "distinguishedName", "gPLink"], SearchScope.OneLevel))
                    targets.Add((AdInfrastructureService.Text(r, "distinguishedName"), "Standort " + AdInfrastructureService.Text(r, "name"), "Standort", AdInfrastructureService.Text(r, "gPLink"), 0));
            }
            catch (Exception ex)
            {
                notes.Add("Standorte: " + ex.Message);
            }

            var links = targets
                .SelectMany(t => AdInfraAnalysis.ParseGpLink(t.GpLink).Select(l => (l.Guid, Link: new AdGpoLink(t.Dn, t.Label, t.Kind, l.Enabled, l.Enforced))))
                .GroupBy(x => x.Guid, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Link).ToList(), StringComparer.OrdinalIgnoreCase);

            var sysvolFailures = 0;
            var sysvolRead = 0;
            var gpos = new List<AdGpo>();
            foreach (var r in AdInfrastructureService.Search($"LDAP://CN=Policies,CN=System,{defaultNc}", "(objectClass=groupPolicyContainer)",
                         ["name", "displayName", "versionNumber", "flags", "gPCFileSysPath", "gPCMachineExtensionNames", "gPCUserExtensionNames", "whenCreated", "whenChanged"]))
            {
                var guid = AdInfrastructureService.Text(r, "name").ToUpperInvariant();
                int? sysvol = null;
                var path = AdInfrastructureService.Text(r, "gPCFileSysPath");
                if (path.Length > 0 && sysvolFailures < 3)
                {
                    try
                    {
                        var read = Task.Run(() => File.ReadAllText(Path.Combine(path, "GPT.INI")));
                        if (read.Wait(TimeSpan.FromSeconds(3)))
                        {
                            sysvol = AdInfraAnalysis.GptIniVersion(read.Result);
                            sysvolRead++;
                        }
                        else
                        {
                            sysvolFailures++;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (++sysvolFailures == 3)
                            notes.Add("SYSVOL: " + ex.GetBaseException().Message);
                    }
                }
                gpos.Add(new AdGpo
                {
                    Guid = guid,
                    Name = AdInfrastructureService.Text(r, "displayName") is { Length: > 0 } n ? n : guid,
                    CreatedUtc = AdInfrastructureService.Date(r, "whenCreated"),
                    ChangedUtc = AdInfrastructureService.Date(r, "whenChanged"),
                    AdVersion = AdInfrastructureService.Int(r, "versionNumber") ?? 0,
                    SysvolVersion = sysvol,
                    Flags = AdInfrastructureService.Int(r, "flags") ?? 0,
                    HasComputerSettings = AdInfraAnalysis.HasExtensions(AdInfrastructureService.Text(r, "gPCMachineExtensionNames")),
                    HasUserSettings = AdInfraAnalysis.HasExtensions(AdInfrastructureService.Text(r, "gPCUserExtensionNames")),
                    Links = links.GetValueOrDefault(guid) ?? []
                });
            }

            var computerCount = computers.Computers.GroupBy(c => c.Container, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
            var ous = targets.Where(t => t.Kind != "Standort")
                .Select(t => new AdOuNode(t.Dn, t.Label, t.Kind == "Domäne" ? 0 : t.Dn.Split(",OU=", StringSplitOptions.None).Length,
                    computerCount.GetValueOrDefault(t.Dn), AdInfraAnalysis.ParseGpLink(t.GpLink).Count(l => l.Enabled), t.Options == 1))
                .ToList();
            // Der Standardcontainer CN=Computers ist keine OU, wird aber als Knoten gebraucht.
            foreach (var (dn, n) in computerCount.Where(kv => !ous.Any(o => o.Dn.Equals(kv.Key, StringComparison.OrdinalIgnoreCase)) && AdAnalysis.IsDefaultComputersContainer(kv.Key)))
                ous.Add(new AdOuNode(dn, AdAnalysis.ContainerLabel(dn), 1, n, 0, false));

            return new AdGpoResult
            {
                Enabled = true,
                ReadAt = DateTime.Now,
                Gpos = gpos.OrderBy(g => g.Name).ToList(),
                Ous = ous.OrderBy(o => o.Dn.Length).ToList(),
                SysvolReadable = sysvolRead > 0,
                Notes = notes
            };
        }
        catch (Exception ex)
        {
            return new AdGpoResult { Enabled = true, ReadAt = DateTime.Now, Error = ex.Message };
        }
    }
}
