using System.Buffers.Binary;
using System.Globalization;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace TrafagSalesExporter.Services;

/// <summary>Ein DNS-Eintrag aus dem AD-Attribut dnsRecord (MS-DNSP 2.3.2.2).</summary>
public sealed record AdDnsRecord(int Type, uint TtlSeconds, uint TimestampHours, byte[] Data)
{
    public bool IsStatic => TimestampHours == 0;
    public DateTime? StampUtc => TimestampHours == 0 ? null : new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddHours(TimestampHours);
    public string? IPv4 => Type == 1 && Data.Length >= 4 ? $"{Data[0]}.{Data[1]}.{Data[2]}.{Data[3]}" : null;
}

/// <summary>
/// Reine Auswertungen fuer AD-Infrastruktur, Gruppenrichtlinien, DNS und Verlauf (2026-10-02). Testbar ohne AD.
/// Binaerformate nach MS-DNSP (dnsRecord, dnsProperty); GPO-Version: obere 16 Bit Benutzer, untere 16 Bit Computer.
/// </summary>
public static class AdInfraAnalysis
{
    public const int DnsZoneNoRefresh = 0x10;
    public const int DnsZoneRefresh = 0x20;
    public const int DnsZoneAgingState = 0x40;

    public static AdDnsRecord? ParseDnsRecord(byte[] raw)
    {
        if (raw is null || raw.Length < 24) return null;
        var length = BinaryPrimitives.ReadUInt16LittleEndian(raw.AsSpan(0, 2));
        var type = BinaryPrimitives.ReadUInt16LittleEndian(raw.AsSpan(2, 2));
        var ttl = BinaryPrimitives.ReadUInt32BigEndian(raw.AsSpan(12, 4));
        var stamp = BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(20, 4));
        var data = raw.Length >= 24 + length ? raw.AsSpan(24, length).ToArray() : raw.AsSpan(24).ToArray();
        return new AdDnsRecord(type, ttl, stamp, data);
    }

    /// <summary>Liest Id und DWORD-Wert aus einem dnsProperty-Eintrag der Zone.</summary>
    public static (int Id, uint Value)? ParseDnsProperty(byte[] raw)
    {
        if (raw is null || raw.Length < 20) return null;
        var length = BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(0, 4));
        var id = (int)BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(16, 4));
        uint value = 0;
        var available = Math.Min((int)Math.Min(length, 4u), raw.Length - 20);
        for (var i = 0; i < available; i++)
            value |= (uint)raw[20 + i] << (8 * i);
        return (id, value);
    }

    private static readonly Regex GpLinkPattern = new(@"\[LDAP://([^;\]]+);(\d+)\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>"[LDAP://cn={GUID},cn=policies,...;2]" -> GUID, aktiv, erzwungen (Option 1 = deaktiviert, 2 = erzwungen).</summary>
    public static IReadOnlyList<(string Guid, bool Enabled, bool Enforced)> ParseGpLink(string? gpLink)
        => GpLinkPattern.Matches(gpLink ?? "")
            .Select(m =>
            {
                var dn = m.Groups[1].Value;
                var g = Regex.Match(dn, @"\{[0-9A-Fa-f\-]{36}\}");
                var opt = int.TryParse(m.Groups[2].Value, out var o) ? o : 0;
                return (g.Success ? g.Value.ToUpperInvariant() : dn, (opt & 1) == 0, (opt & 2) != 0);
            })
            .ToList();

    public static (int User, int Computer) SplitGpoVersion(int version) => ((version >> 16) & 0xFFFF, version & 0xFFFF);

    public static int? GptIniVersion(string? text)
    {
        var m = Regex.Match(text ?? "", @"^\s*Version\s*=\s*(\d+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        return m.Success && int.TryParse(m.Groups[1].Value, out var v) ? v : null;
    }

    /// <summary>Hat die GPO Einstellungen? Erweiterungsliste "[{...}{...}]" mit mindestens einer GUID.</summary>
    public static bool HasExtensions(string? names) => !string.IsNullOrWhiteSpace(names) && names.Contains('{');

    public static string GpoState(AdGpo g)
    {
        if (g.Flags == 3) return "aus";
        if (g.Links.Count == 0) return "unverknuepft";
        if (g.Links.All(l => !l.Enabled)) return "verknuepfung-aus";
        if (!g.HasComputerSettings && !g.HasUserSettings) return "leer";
        if (g.SysvolVersion is { } s && s != g.AdVersion) return "version";
        return "ok";
    }

    public static string LevelName(int? level) => level switch
    {
        0 => "Windows 2000",
        1 => "Windows Server 2003 interim",
        2 => "Windows Server 2003",
        3 => "Windows Server 2008",
        4 => "Windows Server 2008 R2",
        5 => "Windows Server 2012",
        6 => "Windows Server 2012 R2",
        7 => "Windows Server 2016",
        10 => "Windows Server 2025",
        null => "?",
        _ => $"Stufe {level}"
    };

    public static string TrustDirection(int direction) => direction switch
    {
        1 => "eingehend", 2 => "ausgehend", 3 => "beidseitig", _ => "deaktiviert"
    };

    public static string TrustType(int type) => type switch
    {
        1 => "Windows NT", 2 => "Active Directory", 3 => "Kerberos (MIT)", 4 => "DCE", 5 => "Entra ID", _ => $"Typ {type}"
    };

    /// <summary>"ok", "alt" (letzte erfolgreiche Replikation aelter 24 h) oder "fehler".</summary>
    public static string ReplicationState(AdReplicationLink l, DateTime nowUtc)
    {
        if (l.Failures > 0 || l.LastResult != 0) return "fehler";
        if (l.LastSuccessUtc is null || l.LastSuccessUtc < nowUtc.AddHours(-24)) return "alt";
        return "ok";
    }

    public static IReadOnlyList<AdSpnDuplicate> SpnDuplicates(IEnumerable<(string Owner, IEnumerable<string> Spns)> owners)
        => owners
            .SelectMany(o => o.Spns.Select(s => (Spn: s.Trim(), o.Owner)))
            .Where(x => x.Spn.Length > 0)
            .GroupBy(x => x.Spn, StringComparer.OrdinalIgnoreCase)
            .Select(g => new AdSpnDuplicate(g.Key, g.Select(x => x.Owner).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList()))
            .Where(d => d.Owners.Count > 1)
            .OrderBy(d => d.Spn)
            .ToList();

    /// <summary>Vergleicht zwei Schnappschuesse der Computerkonten.</summary>
    public static IReadOnlyList<AdComputerChange> Diff(IReadOnlyList<AdComputerState> previous, IReadOnlyList<AdComputerState> current, DateOnly day)
    {
        var prev = previous.GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var curr = current.GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var changes = new List<AdComputerChange>();
        foreach (var c in curr.Values)
        {
            if (!prev.TryGetValue(c.Name, out var p))
            {
                changes.Add(new(day, c.Name, "neu", AdAnalysis.ContainerLabel(c.Container)));
                continue;
            }
            if (p.Enabled && !c.Enabled) changes.Add(new(day, c.Name, "deaktiviert", ""));
            if (!p.Enabled && c.Enabled) changes.Add(new(day, c.Name, "aktiviert", ""));
            if (!string.Equals(p.Container, c.Container, StringComparison.OrdinalIgnoreCase))
                changes.Add(new(day, c.Name, "verschoben", $"{AdAnalysis.ContainerLabel(p.Container)} → {AdAnalysis.ContainerLabel(c.Container)}"));
            if (!string.Equals(p.Product, c.Product, StringComparison.OrdinalIgnoreCase))
                changes.Add(new(day, c.Name, "betriebssystem", $"{p.Product} → {c.Product}"));
        }
        foreach (var p in prev.Values.Where(p => !curr.ContainsKey(p.Name)))
            changes.Add(new(day, p.Name, "entfernt", AdAnalysis.ContainerLabel(p.Container)));
        return changes.OrderBy(c => c.Change).ThenBy(c => c.Name).ToList();
    }

    /// <summary>Produkte mit aktiven Geraeten, deren Support in den naechsten <paramref name="days"/> Tagen endet.</summary>
    public static IReadOnlyList<AdCountdown> Countdowns(IReadOnlyList<AdComputer> computers, DateOnly today, int days = 365)
        => computers.Where(c => c.Enabled)
            .Select(c => AdAnalysis.Lifecycle(c.OperatingSystem, c.OsVersion))
            .Where(l => l.EndOfSupport is { } e && e >= today && e <= today.AddDays(days))
            .GroupBy(l => (l.Product, End: l.EndOfSupport!.Value))
            .Select(g => new AdCountdown(g.Key.Product, g.Key.End, g.Key.End.DayNumber - today.DayNumber, g.Count()))
            .OrderBy(c => c.End).ThenByDescending(c => c.ActiveDevices)
            .ToList();

    /// <summary>Kennzahlen eines Tages fuer den Verlauf (Namen sind die Schluessel in NetworkAdMetrics).</summary>
    public static IReadOnlyDictionary<string, double> Metrics(AdComputerResult ad, DateTime nowUtc)
    {
        var all = ad.Computers;
        var m = new Dictionary<string, double>
        {
            ["computer"] = all.Count,
            ["aktiv30"] = all.Count(c => AdAnalysis.IsActiveRecently(c, nowUtc)),
            ["ohneSupport"] = all.Count(c => AdAnalysis.Health(c, nowUtc) == AdHealth.Unsupported),
            ["inaktiv90"] = all.Count(c => AdAnalysis.IsInactive(c, nowUtc) && c.LastLogonUtc is not null),
            ["deaktiviert"] = all.Count(c => !c.Enabled),
            ["passwortAlt"] = all.Count(c => AdAnalysis.PasswordStale(c, nowUtc)),
            ["delegation"] = all.Count(AdAnalysis.RiskyDelegation),
            ["standardcontainer"] = all.Count(c => AdAnalysis.IsDefaultComputersContainer(c.Container))
        };
        if (ad.LapsReadable) m["ohneLaps"] = all.Count(AdAnalysis.LapsMissing);
        // Fuer den Migrationsplaner: aktive Geraete je Produkt mit Supportende bis in zwei Jahren.
        foreach (var w in MigrationWaves(all, DateOnly.FromDateTime(nowUtc)))
            m["os:" + w.Product] = m.GetValueOrDefault("os:" + w.Product) + w.Devices.Count;
        m["altlasten"] = LegacyScores(all, nowUtc, ad.LapsReadable).Sum(s => s.Score);
        return m;
    }

    /// <summary>
    /// Liest einen Eintrag des konstruierten Attributs msDS-NCReplInboundNeighbors (XML je Partner).
    /// Der Partnername ist der Servername aus pszSourceDsaDN (CN=NTDS Settings,CN=<Server>,...).
    /// </summary>
    public static AdReplicationLink? ParseReplNeighbor(string xml, string dc)
    {
        try
        {
            var x = XElement.Parse(xml);
            string V(string n) => x.Element(n)?.Value ?? "";
            DateTime? T(string n) => DateTime.TryParse(V(n), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var d) && d.Year > 1601 ? d : null;
            var source = AdAnalysis.FirstRdnValue(AdAnalysis.ParentDn(V("pszSourceDsaDN")));
            var nc = V("pszNamingContext");
            var partition = nc.StartsWith("CN=Schema", StringComparison.OrdinalIgnoreCase) ? "Schema"
                : nc.StartsWith("CN=Configuration", StringComparison.OrdinalIgnoreCase) ? "Konfiguration"
                : nc.StartsWith("DC=DomainDnsZones", StringComparison.OrdinalIgnoreCase) ? "DomainDnsZones"
                : nc.StartsWith("DC=ForestDnsZones", StringComparison.OrdinalIgnoreCase) ? "ForestDnsZones"
                : "Domäne";
            return new AdReplicationLink(dc, source, partition, T("ftimeLastSyncSuccess"), T("ftimeLastSyncAttempt"),
                int.TryParse(V("cNumConsecutiveSyncFailures"), out var f) ? f : 0,
                int.TryParse(V("dwLastSyncResult"), out var r) ? r : 0, "");
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Punkte je Grund fuer den Altlasten-Score (Netzwerk, Active Directory, 2026-10-02).</summary>
    public static readonly IReadOnlyDictionary<string, int> ScoreWeights = new Dictionary<string, int>
    {
        ["delegation"] = 5,
        ["ohneSupport"] = 3,
        ["supportBald"] = 2,
        ["inaktiv"] = 2,
        ["passwortAlt"] = 2,
        ["nieAngemeldet"] = 1,
        ["langeDeaktiviert"] = 1,
        ["ohneLaps"] = 1
    };

    /// <summary>Gruende eines Computers fuer den Altlasten-Score.</summary>
    public static IEnumerable<string> ScoreReasons(AdComputer c, DateTime nowUtc, bool lapsReadable)
    {
        var today = DateOnly.FromDateTime(nowUtc);
        if (AdAnalysis.RiskyDelegation(c)) yield return "delegation";
        if (c.Enabled)
        {
            var cls = AdAnalysis.LifecycleClass(AdAnalysis.Lifecycle(c.OperatingSystem, c.OsVersion).EndOfSupport, today);
            if (cls == "aus") yield return "ohneSupport";
            else if (cls == "bald") yield return "supportBald";
        }
        if (AdAnalysis.IsInactive(c, nowUtc) && c.LastLogonUtc is not null) yield return "inaktiv";
        if (c.Enabled && c.LastLogonUtc is null && c.CreatedUtc < nowUtc.AddDays(-30)) yield return "nieAngemeldet";
        if (AdAnalysis.PasswordStale(c, nowUtc)) yield return "passwortAlt";
        if (!c.Enabled && c.ChangedUtc < nowUtc.AddDays(-365)) yield return "langeDeaktiviert";
        if (lapsReadable && AdAnalysis.LapsMissing(c)) yield return "ohneLaps";
    }

    public static IReadOnlyList<AdOuScore> LegacyScores(IReadOnlyList<AdComputer> computers, DateTime nowUtc, bool lapsReadable)
        => computers.GroupBy(c => c.Container, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var parts = g.SelectMany(c => ScoreReasons(c, nowUtc, lapsReadable))
                    .GroupBy(r => r).ToDictionary(r => r.Key, r => r.Count() * ScoreWeights[r.Key]);
                return new AdOuScore(g.Key, AdAnalysis.ContainerLabel(g.Key), g.Count(), parts.Values.Sum(), parts);
            })
            .Where(s => s.Score > 0)
            .OrderByDescending(s => s.Score).ThenBy(s => s.Label)
            .ToList();

    /// <summary>Aktive Geraete, deren Support abgelaufen ist oder in <paramref name="days"/> Tagen endet, je Produkt und Datum.</summary>
    public static IReadOnlyList<AdMigrationWave> MigrationWaves(IReadOnlyList<AdComputer> computers, DateOnly today, int days = 730)
        => computers.Where(c => c.Enabled)
            .Select(c => (c, l: AdAnalysis.Lifecycle(c.OperatingSystem, c.OsVersion)))
            .Where(x => x.l.EndOfSupport is { } e && e <= today.AddDays(days))
            .GroupBy(x => (x.l.Product, End: x.l.EndOfSupport!.Value))
            .Select(g => new AdMigrationWave(g.Key.Product, g.Key.End, g.Key.End.DayNumber - today.DayNumber, g.Select(x => x.c).OrderBy(c => c.Name).ToList()))
            .OrderBy(w => w.End).ThenByDescending(w => w.Devices.Count)
            .ToList();

    /// <summary>Ordnet die DNS-IPs aktiver Computer den AD-Subnetzen und damit Standorten zu; "" = kein Standort.</summary>
    public static IReadOnlyList<AdSiteDevices> DevicesPerSite(IReadOnlyList<AdDnsEntry> dns, IReadOnlyList<AdSubnet> subnets)
    {
        var bySite = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in dns)
        {
            var ip = e.Ips.Select(i => System.Net.IPAddress.TryParse(i, out var a) ? a : null).FirstOrDefault(a => a is not null);
            if (ip is null) continue;
            // Laengstes passendes Subnetz gewinnt.
            var site = subnets.Where(s => AdAnalysis.InCidr(ip, s.Cidr))
                .OrderByDescending(s => int.TryParse(s.Cidr.Split('/').Last(), out var b) ? b : 0)
                .Select(s => s.Site).FirstOrDefault() ?? "";
            if (!bySite.TryGetValue(site, out var list)) bySite[site] = list = [];
            list.Add(e.Name);
        }
        return bySite.Select(kv => new AdSiteDevices(kv.Key, kv.Value.Count, kv.Value.OrderBy(n => n).Take(5).ToList()))
            .OrderByDescending(s => s.Devices).ToList();
    }
}
