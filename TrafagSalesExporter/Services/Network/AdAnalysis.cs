using System.Net;
using System.Text.RegularExpressions;

namespace TrafagSalesExporter.Services;

/// <summary>Supportende eines Betriebssystems. <see cref="Product"/> ist die lesbare Bezeichnung mit Version.</summary>
public sealed record AdLifecycle(string Product, DateOnly? EndOfSupport);

/// <summary>Zustand eines Computers fuer die 3D-Ansicht und die Zaehler; jeder Computer genau einmal.</summary>
public enum AdHealth { Ok, Inactive, Disabled, Unsupported }

/// <summary>Eine OU als Turm in der 3D-Ansicht.</summary>
public sealed record AdOuTower(string Container, string Label, int Ok, int Inactive, int Disabled, int Unsupported, bool HasGpoLink, bool IsDefaultContainer)
{
    public int Total => Ok + Inactive + Disabled + Unsupported;
}

/// <summary>Ein /24-Netzbereich, der in keinem AD-Subnetz liegt.</summary>
public sealed record AdUncoveredRange(string Range, int Computers, int ClientSessions, IReadOnlyList<string> Examples);

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

    private static readonly Regex BuildPattern = new(@"\((\d+)\)", RegexOptions.Compiled);

    public static int? Build(string osVersion)
    {
        var m = BuildPattern.Match(osVersion ?? "");
        return m.Success && int.TryParse(m.Groups[1].Value, out var b) ? b : null;
    }

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
            if (Has("2012")) return new(os, D(2023, 10, 10));
            if (Has("2016")) return new(os, D(2027, 1, 12));
            if (Has("2019")) return new(os, D(2029, 1, 9));
            if (Has("2022")) return new(os, D(2031, 10, 14));
            if (Has("2025")) return new(os, D(2034, 10, 10));
            return new(os, null);
        }
        if (Has("XP")) return new(os, D(2014, 4, 8));
        if (Has("Vista")) return new(os, D(2017, 4, 11));
        if (Has("Windows 7")) return new(os, D(2020, 1, 14));
        if (Has("Windows 8.1")) return new(os, D(2023, 1, 10));
        if (Has("Windows 8")) return new(os, D(2016, 1, 12));

        var enterprise = Has("Enterprise") || Has("Education");
        var ltsc = Has("LTSC") || Has("LTSB");
        var iot = Has("IoT");
        if (Has("Windows 10"))
        {
            if (ltsc)
                return build switch
                {
                    10240 => new($"{os} 2015", D(2025, 10, 14)),
                    14393 => new($"{os} 2016", D(2026, 10, 13)),
                    17763 => new($"{os} 2019", D(2029, 1, 9)),
                    19044 => new($"{os} 2021", iot ? D(2032, 1, 13) : D(2027, 1, 12)),
                    _ => new(os, null)
                };
            return new($"{os} {Win10Release(build)}".TrimEnd(), D(2025, 10, 14));
        }
        if (Has("Windows 11"))
        {
            if (ltsc && build == 26100)
                return new($"{os} 2024", iot ? D(2034, 10, 10) : D(2029, 10, 9));
            var name = $"{os} {Win11Release(build)}".TrimEnd();
            return build switch
            {
                22000 => new(name, enterprise ? D(2024, 10, 8) : D(2023, 10, 10)),
                22621 => new(name, enterprise ? D(2025, 10, 14) : D(2024, 10, 8)),
                22631 => new(name, enterprise ? D(2026, 11, 10) : D(2025, 11, 11)),
                26100 => new(name, enterprise ? D(2027, 10, 12) : D(2026, 10, 13)),
                26200 => new(name, enterprise ? D(2028, 10, 10) : D(2027, 10, 12)),
                _ => new(name, null)
            };
        }
        return new(os.Length > 0 ? os : "?", null);
    }

    private static string Win10Release(int? build) => build switch
    {
        19041 => "2004", 19042 => "20H2", 19043 => "21H1", 19044 => "21H2", 19045 => "22H2", null => "", _ => $"Build {build}"
    };

    private static string Win11Release(int? build) => build switch
    {
        22000 => "21H2", 22621 => "22H2", 22631 => "23H2", 26100 => "24H2", 26200 => "25H2", null => "", _ => $"Build {build}"
    };

    /// <summary>"aus" = Support abgelaufen, "bald" = endet in 180 Tagen, "ok", "unbekannt".</summary>
    public static string LifecycleClass(DateOnly? end, DateOnly today)
        => end is null ? "unbekannt" : end < today ? "aus" : end <= today.AddDays(180) ? "bald" : "ok";

    public static bool IsInactive(AdComputer c, DateTime nowUtc, int days = InactiveDays)
        => c.Enabled && (c.LastLogonUtc is null || c.LastLogonUtc < nowUtc.AddDays(-days));

    public static bool IsActiveRecently(AdComputer c, DateTime nowUtc) => c.Enabled && c.LastLogonUtc > nowUtc.AddDays(-30);

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

    /// <summary>
    /// Netzbereiche (/24), in denen Computer (laut DNS) oder Browser-Sitzungen gesehen werden, die in keinem
    /// AD-Subnetz liegen. Solche Geraete melden sich an einem beliebigen Domaenencontroller an.
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
            if (!IPAddress.TryParse(ip, out var a) || Covered(a)) continue;
            var key = Range24(a);
            if (!ranges.TryGetValue(key, out var v)) ranges[key] = v = (new HashSet<string>(StringComparer.OrdinalIgnoreCase), 0);
            v.Names.Add(e.Name);
        }
        foreach (var s in clientSubnets)
        {
            var slash = s.Subnet.IndexOf('/');
            if (slash < 0 || !IPAddress.TryParse(s.Subnet[..slash], out var a) || Covered(a)) continue;
            var key = Range24(a);
            var v = ranges.TryGetValue(key, out var x) ? x : (new HashSet<string>(StringComparer.OrdinalIgnoreCase), 0);
            ranges[key] = (v.Item1, v.Item2 + s.Circuits);
        }
        return ranges
            .Select(r => new AdUncoveredRange(r.Key, r.Value.Names.Count, r.Value.Sessions, r.Value.Names.OrderBy(n => n).Take(5).ToList()))
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
