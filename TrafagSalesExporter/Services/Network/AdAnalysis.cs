using System.Net;
using System.Text.RegularExpressions;

namespace TrafagSalesExporter.Services;

/// <summary>Supportende eines Betriebssystems. <see cref="Product"/> ist die lesbare Bezeichnung mit Version.</summary>
public sealed record AdLifecycle(string Product, DateOnly? EndOfSupport, DateOnly? EsuEnd = null);

/// <summary>Zustand eines Computers fuer die 3D-Ansicht und die Zaehler; jeder Computer genau einmal.</summary>
public enum AdHealth { Ok, Inactive, Disabled, Unsupported }

/// <summary>Eine OU als Turm in der 3D-Ansicht.</summary>
public sealed record AdOuTower(string Container, string Label, int Ok, int Inactive, int Disabled, int Unsupported, bool HasGpoLink, bool IsDefaultContainer)
{
    public int Total => Ok + Inactive + Disabled + Unsupported;
}

/// <summary>Ein /24-Netzbereich, der in keinem AD-Subnetz liegt.</summary>
public sealed record AdUncoveredRange(string Range, int Computers, int ClientSessions, IReadOnlyList<string> Examples, bool Partial = false);

/// <summary>Mehrere Computer mit derselben IP im DNS.</summary>
public sealed record AdDuplicateIp(string Ip, IReadOnlyList<string> Names);

/// <summary>
/// Reine Auswertungen der AD-Computerdaten (Reiter Netzwerk, Unterreiter AD, 2026-10-02). Testbar ohne AD.
/// Supportdaten: Microsoft Lifecycle, Stand 2026-10; bei neuen Versionen hier nachtragen.
/// </summary>
public static class AdAnalysis
{
    public const int InactiveDays = 90;
    public const int PasswordDays = 60;
    /// <summary>lastLogonTimestamp wird nur alle 9 bis 14 Tage aktualisiert: 30 Tage Aktivitaet plus 14 Tage Replikationsverzoegerung.</summary>
    public const int ActiveDays = 44;
    /// <summary>Konten juenger als 30 Tage gelten nicht als "nie angemeldet".</summary>
    public const int NeverUsedMinAgeDays = 30;

    private static readonly Regex BuildPattern = new(@"\((\d+)\)", RegexOptions.Compiled);

    public static int? Build(string osVersion)
    {
        var m = BuildPattern.Match(osVersion ?? "");
        return m.Success && int.TryParse(m.Groups[1].Value, out var b) ? b : null;
    }

    /// <summary>
    /// Windows 10 oder 11 nach der Build-Nummer aus operatingSystemVersion ("10.0 (22631)"); das Feld operatingSystem nennt
    /// auf Windows-11-Geraeten oft noch "Windows 10". Ohne Build entscheidet der Text. 0 = kein Windows 10/11.
    /// </summary>
    private static int ClientFamily(string os, string osVersion, int? build)
    {
        if (build is { } b && (osVersion ?? "").TrimStart().StartsWith("10.0", StringComparison.Ordinal))
        {
            if (b >= 22000) return 11;
            if (b >= 10240) return 10;
        }
        if (os.Contains("Windows 11", StringComparison.OrdinalIgnoreCase)) return 11;
        if (os.Contains("Windows 10", StringComparison.OrdinalIgnoreCase)) return 10;
        return 0;
    }

    private static string WithYear(string name, int year) => name.Contains(year.ToString(), StringComparison.Ordinal) ? name : $"{name} {year}";

    public static AdLifecycle Lifecycle(string os, string osVersion)
    {
        os ??= "";
        var build = Build(osVersion);
        bool Has(string s) => os.Contains(s, StringComparison.OrdinalIgnoreCase);
        static DateOnly D(int y, int m, int d) => new(y, m, d);

        if (Has("Server"))
        {
            if (Has("2003")) return new(os, D(2015, 7, 14));
            if (Has("2008")) return new(os, D(2020, 1, 14));
            // Server 2012 (R2): erweiterte Updates (ESU) noch bis 13.10.2026.
            if (Has("2012")) return new(os, D(2023, 10, 10), D(2026, 10, 13));
            if (Has("2016")) return new(os, D(2027, 1, 12));
            if (Has("2019")) return new(os, D(2029, 1, 9));
            if (Has("2022")) return new(os, D(2031, 10, 14));
            if (Has("2025")) return new(os, D(2034, 10, 10));
            return new(os, null);
        }
        var family = ClientFamily(os, osVersion, build);
        if (family == 0)
        {
            if (Has("XP")) return new(os, D(2014, 4, 8));
            if (Has("Vista")) return new(os, D(2017, 4, 11));
            if (Has("Windows 7")) return new(os, D(2020, 1, 14));
            if (Has("Windows 8.1")) return new(os, D(2023, 1, 10));
            if (Has("Windows 8")) return new(os, D(2016, 1, 12));
            return new(os.Length > 0 ? os : "?", null);
        }

        // Das Feld operatingSystem nennt auf Windows 11 teils "Windows 10": Name nach dem Build richtigstellen.
        var name = family == 11 ? Regex.Replace(os, "Windows 10", "Windows 11", RegexOptions.IgnoreCase) : os;
        var enterprise = Has("Enterprise") || Has("Education");
        var ltsc = Has("LTSC") || Has("LTSB");
        var iot = Has("IoT");
        if (family == 10)
        {
            if (ltsc)
                return build switch
                {
                    10240 => new(WithYear(name, 2015), D(2025, 10, 14)),
                    14393 => new(WithYear(name, 2016), D(2026, 10, 13)),
                    17763 => new(WithYear(name, 2019), D(2029, 1, 9)),
                    19044 => new(WithYear(name, 2021), iot ? D(2032, 1, 13) : D(2027, 1, 12)),
                    _ => new(name, null)
                };
            // Windows 10 22H2: Standardsupport Oktober 2025 vorbei, Sicherheitsupdates nur noch ueber ESU bis 13.10.2026.
            return new($"{name} {Win10Release(build)}".TrimEnd(), D(2025, 10, 14), build == 19045 ? D(2026, 10, 13) : null);
        }
        if (ltsc && build == 26100)
            return new(WithYear(name, 2024), iot ? D(2034, 10, 10) : D(2029, 10, 9));
        var label = $"{name} {Win11Release(build)}".TrimEnd();
        return build switch
        {
            22000 => new(label, enterprise ? D(2024, 10, 8) : D(2023, 10, 10)),
            22621 => new(label, enterprise ? D(2025, 10, 14) : D(2024, 10, 8)),
            22631 => new(label, enterprise ? D(2026, 11, 10) : D(2025, 11, 11)),
            26100 => new(label, enterprise ? D(2027, 10, 12) : D(2026, 10, 13)),
            26200 => new(label, enterprise ? D(2028, 10, 10) : D(2027, 10, 12)),
            _ => new(label, null)
        };
    }

    /// <summary>Standardsupport vorbei, aber erweiterte Sicherheitsupdates (ESU) laufen noch: "nur ESU".</summary>
    public static bool IsEsuOnly(AdLifecycle l, DateOnly today)
        => l.EndOfSupport is { } e && e < today && l.EsuEnd is { } esu && esu >= today;

    private static string Win10Release(int? build) => build switch
    {
        10240 => "1507", 10586 => "1511", 14393 => "1607", 15063 => "1703", 16299 => "1709", 17134 => "1803", 17763 => "1809",
        18362 => "1903", 18363 => "1909", 19041 => "2004", 19042 => "20H2", 19043 => "21H1", 19044 => "21H2", 19045 => "22H2",
        null => "", _ => $"Build {build}"
    };

    private static string Win11Release(int? build) => build switch
    {
        22000 => "21H2", 22621 => "22H2", 22631 => "23H2", 26100 => "24H2", 26200 => "25H2", null => "", _ => $"Build {build}"
    };

    /// <summary>"aus" = Support abgelaufen, "bald" = endet in 180 Tagen, "ok", "unbekannt".</summary>
    public static string LifecycleClass(DateOnly? end, DateOnly today)
        => end is null ? "unbekannt" : end < today ? "aus" : end <= today.AddDays(180) ? "bald" : "ok";

    /// <summary>
    /// Die eine Definition von "inaktiv": aktiviertes Konto mit bekannter letzter Anmeldung, die laenger als
    /// <paramref name="days"/> Tage zurueckliegt. Nie angemeldete Konten siehe <see cref="IsNeverUsed"/>.
    /// </summary>
    public static bool IsInactive(AdComputer c, DateTime nowUtc, int days = InactiveDays)
        => c.Enabled && c.LastLogonUtc is { } l && l < nowUtc.AddDays(-days);

    /// <summary>Aktiviertes Konto ohne jede Anmeldung, das schon aelter als 30 Tage ist.</summary>
    public static bool IsNeverUsed(AdComputer c, DateTime nowUtc)
        => c.Enabled && c.LastLogonUtc is null && c.CreatedUtc is { } created && created < nowUtc.AddDays(-NeverUsedMinAgeDays);

    /// <summary>Angemeldet in den letzten <see cref="ActiveDays"/> Tagen (30 plus 14 Tage Ungenauigkeit von lastLogonTimestamp).</summary>
    public static bool IsActiveRecently(AdComputer c, DateTime nowUtc) => c.Enabled && c.LastLogonUtc > nowUtc.AddDays(-ActiveDays);

    public static AdHealth Health(AdComputer c, DateTime nowUtc)
    {
        if (!c.Enabled) return AdHealth.Disabled;
        if (LifecycleClass(Lifecycle(c.OperatingSystem, c.OsVersion).EndOfSupport, DateOnly.FromDateTime(nowUtc)) == "aus") return AdHealth.Unsupported;
        return IsInactive(c, nowUtc) ? AdHealth.Inactive : AdHealth.Ok;
    }

    /// <summary>Aktiv benutzt, aber das Computerpasswort wurde seit 60 Tagen nicht erneuert: oft verlorene Vertrauensstellung.</summary>
    public static bool PasswordStale(AdComputer c, DateTime nowUtc)
        => IsActiveRecently(c, nowUtc) && c.PwdLastSetUtc is { } p && p < nowUtc.AddDays(-PasswordDays);

    /// <summary>Uneingeschraenkte Delegation ausserhalb der Domaenencontroller ist ein bekanntes Angriffsziel.</summary>
    public static bool RiskyDelegation(AdComputer c) => c.Enabled && !c.IsDomainController && (c.UnconstrainedDelegation || c.ProtocolTransition);

    public static bool LapsMissing(AdComputer c)
        => c.Enabled && !c.IsDomainController && c.OperatingSystem.Contains("Windows", StringComparison.OrdinalIgnoreCase) && c.LapsExpiryUtc is null;

    /// <summary>LAPS vorhanden, aber das Passwort ist seit ueber 7 Tagen abgelaufen und wurde nicht erneuert.</summary>
    public static bool LapsOverdue(AdComputer c, DateTime nowUtc)
        => IsActiveRecently(c, nowUtc) && c.LapsExpiryUtc is { } e && e < nowUtc.AddDays(-7);

    public static string ParentDn(string dn)
    {
        if (string.IsNullOrEmpty(dn)) return "";
        for (var i = 0; i < dn.Length; i++)
        {
            if (dn[i] == '\\') { i++; continue; }
            if (dn[i] == ',') return dn[(i + 1)..];
        }
        return "";
    }

    public static string FirstRdnValue(string dn)
    {
        var first = dn.Length == 0 ? "" : dn[..(dn.Length - ParentDn(dn).Length)].TrimEnd(',');
        var eq = first.IndexOf('=');
        return eq < 0 ? first : first[(eq + 1)..].Replace("\\", "");
    }

    /// <summary>"OU=Clients,OU=Werk,DC=x,DC=local" -> "Werk / Clients".</summary>
    public static string ContainerLabel(string containerDn)
    {
        var parts = new List<string>();
        var rest = containerDn;
        while (rest.Length > 0 && !rest.StartsWith("DC=", StringComparison.OrdinalIgnoreCase))
        {
            parts.Add(FirstRdnValue(rest));
            rest = ParentDn(rest);
        }
        parts.Reverse();
        return parts.Count == 0 ? "(Domäne)" : string.Join(" / ", parts);
    }

    public static bool IsDefaultComputersContainer(string containerDn)
        => containerDn.StartsWith("CN=Computers,DC=", StringComparison.OrdinalIgnoreCase);

    public static IReadOnlyList<AdOuTower> Towers(IReadOnlyList<AdComputer> computers, IReadOnlyList<AdOu> ous, DateTime nowUtc)
    {
        var gpo = ous.ToDictionary(o => o.DistinguishedName, o => o.HasGpoLink, StringComparer.OrdinalIgnoreCase);
        return computers
            .GroupBy(c => c.Container, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var h = g.Select(c => Health(c, nowUtc)).ToList();
                return new AdOuTower(g.Key, ContainerLabel(g.Key),
                    h.Count(x => x == AdHealth.Ok), h.Count(x => x == AdHealth.Inactive), h.Count(x => x == AdHealth.Disabled), h.Count(x => x == AdHealth.Unsupported),
                    gpo.GetValueOrDefault(g.Key), IsDefaultComputersContainer(g.Key));
            })
            .OrderByDescending(t => t.Total)
            .ToList();
    }

    /// <summary>Bekannte Dienstklassen aus servicePrincipalName lesbar zusammengefasst.</summary>
    public static string ServiceGroup(string spnClass) => spnClass.ToUpperInvariant() switch
    {
        var v when v.StartsWith("VEEAM") => "Backup (Veeam)",
        "MSSQLSVC" => "SQL Server",
        "HTTP" => "Web (HTTP)",
        "TERMSRV" => "Remotedesktop",
        "WSMAN" => "WinRM",
        "LDAP" or "GC" or "E3514235-4B06-11D1-AB04-00C04FC2DCD2" => "Domänencontroller",
        "DNS" => "DNS",
        "EXCHANGEMDB" or "EXCHANGERFR" or "EXCHANGEAB" or "SMTP" or "SMTPSVC" or "IMAP" or "IMAP4" or "POP" or "POP3" => "Mail/Exchange",
        "FIMSERVICE" or "MSOMSDKSVC" or "MSOMHSVC" => "Monitoring/Verwaltung",
        "VMMS" or "MICROSOFT VIRTUAL CONSOLE SERVICE" or "MICROSOFT VIRTUAL SYSTEM MIGRATION SERVICE" or "HYPER-V REPLICA SERVICE" => "Hyper-V",
        "CIFS" => "Dateifreigabe",
        "MSSERVERCLUSTER" or "MSSERVERCLUSTERMGMTAPI" or "MSCLUSTERVIRTUALSERVER" => "Cluster",
        "WSUS" => "WSUS",
        "RPC" or "RPCSS" or "DFSR-12F9A27C-BF97-4787-9364-D31B6C55EB04" or "NTFRS-88F5D2BD-B646-11D2-A6D3-00C04FC9B232" => "",
        _ => spnClass
    };

    public static bool InCidr(IPAddress ip, string cidr)
    {
        var slash = cidr.IndexOf('/');
        if (slash < 0 || !IPAddress.TryParse(cidr[..slash], out var net) || !int.TryParse(cidr[(slash + 1)..], out var bits)) return false;
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        var a = ip.GetAddressBytes();
        var b = net.GetAddressBytes();
        if (a.Length != b.Length || bits < 0 || bits > a.Length * 8) return false;
        for (var i = 0; i < a.Length && bits > 0; i++, bits -= 8)
        {
            var mask = bits >= 8 ? 0xFF : (byte)(0xFF << (8 - bits));
            if ((a[i] & mask) != (b[i] & mask)) return false;
        }
        return true;
    }

    /// <summary>Wie viele der 256 Adressen eines /24-Bereichs ("a.b.c.0/24") liegen in AD-Subnetzen.</summary>
    public static int CoveredAddresses24(string range24, IReadOnlyList<AdSubnet> subnets)
    {
        var slash = range24.IndexOf('/');
        if (slash < 0 || !IPAddress.TryParse(range24[..slash], out var net) || net.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) return 0;
        var b = net.GetAddressBytes();
        var count = 0;
        for (var i = 0; i < 256; i++)
        {
            var ip = new IPAddress([b[0], b[1], b[2], (byte)i]);
            if (subnets.Any(s => InCidr(ip, s.Cidr))) count++;
        }
        return count;
    }

    /// <summary>
    /// Netzbereiche (/24), in denen Computer (laut DNS) oder Browser-Sitzungen gesehen werden, die nicht vollstaendig in
    /// AD-Subnetzen liegen. Deckt ein AD-Subnetz einen /24 nur teilweise ab, ist der Bereich als
    /// <see cref="AdUncoveredRange.Partial"/> markiert. Solche Geraete melden sich an einem beliebigen Domaenencontroller an.
    /// </summary>
    public static IReadOnlyList<AdUncoveredRange> UncoveredRanges(
        IReadOnlyList<AdDnsEntry> dns, IReadOnlyList<NetworkSubnetStat> clientSubnets, IReadOnlyList<AdSubnet> subnets)
    {
        if (subnets.Count == 0) return [];
        bool Covered(IPAddress ip) => subnets.Any(s => InCidr(ip, s.Cidr));
        var ranges = new Dictionary<string, (HashSet<string> Names, int Sessions)>();
        foreach (var e in dns)
        foreach (var ip in e.Ips)
        {
            if (!IPAddress.TryParse(ip, out var a) || a.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork || Covered(a)) continue;
            var key = Range24(a);
            if (!ranges.TryGetValue(key, out var v)) ranges[key] = v = (new HashSet<string>(StringComparer.OrdinalIgnoreCase), 0);
            v.Names.Add(e.Name);
        }
        foreach (var s in clientSubnets)
        {
            var slash = s.Subnet.IndexOf('/');
            if (slash < 0 || !IPAddress.TryParse(s.Subnet[..slash], out var a) || a.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) continue;
            var key = Range24(a);
            var v = ranges.TryGetValue(key, out var x) ? x : (new HashSet<string>(StringComparer.OrdinalIgnoreCase), 0);
            ranges[key] = (v.Item1, v.Item2 + s.Circuits);
        }
        return ranges
            .Select(r => (r, covered: CoveredAddresses24(r.Key, subnets)))
            .Where(x => x.covered < 256)
            .Select(x => new AdUncoveredRange(x.r.Key, x.r.Value.Names.Count, x.r.Value.Sessions, x.r.Value.Names.OrderBy(n => n).Take(5).ToList(), x.covered > 0))
            .OrderByDescending(r => r.Computers + r.ClientSessions)
            .ToList();
    }

    private static string Range24(IPAddress a)
    {
        var b = a.GetAddressBytes();
        return $"{b[0]}.{b[1]}.{b[2]}.0/24";
    }

    public static IReadOnlyList<AdDuplicateIp> DuplicateIps(IReadOnlyList<AdDnsEntry> dns)
        => dns.SelectMany(e => e.Ips.Select(ip => (ip, e.Name)))
            .GroupBy(x => x.ip)
            .Where(g => g.Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
            .Select(g => new AdDuplicateIp(g.Key, g.Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n).ToList()))
            .OrderByDescending(d => d.Names.Count).ThenBy(d => d.Ip)
            .ToList();

    /// <summary>Neu angelegte Konten je Monat ueber die letzten <paramref name="months"/> Monate.</summary>
    public static IReadOnlyList<(DateOnly Month, int Created)> CreatedPerMonth(IReadOnlyList<AdComputer> computers, DateTime nowUtc, int months)
    {
        var first = new DateOnly(nowUtc.Year, nowUtc.Month, 1).AddMonths(-(months - 1));
        var counts = computers.Where(c => c.CreatedUtc is not null)
            .GroupBy(c => new DateOnly(c.CreatedUtc!.Value.Year, c.CreatedUtc.Value.Month, 1))
            .ToDictionary(g => g.Key, g => g.Count());
        return Enumerable.Range(0, months).Select(i => first.AddMonths(i)).Select(m => (m, counts.GetValueOrDefault(m))).ToList();
    }
}
