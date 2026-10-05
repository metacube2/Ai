using System.Net;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public sealed class AdAnalysisTests
{
    private static readonly DateTime Now = new(2026, 10, 2, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now);

    private static AdComputer Pc(string name, string os = "Windows 11 Pro", string version = "10.0 (26100)", int? logonDaysAgo = 1, bool enabled = true)
        => new(name, os, logonDaysAgo is { } d ? Now.AddDays(-d) : null, Now.AddYears(-2), enabled) { OsVersion = version, Container = "OU=Clients,DC=trafag,DC=local" };

    [Theory]
    [InlineData("Windows XP Professional", "5.1 (2600)", "aus")]
    [InlineData("Windows 7 Professional", "6.1 (7601)", "aus")]
    [InlineData("Windows 10 Pro", "10.0 (19045)", "aus")]
    [InlineData("Windows 10 Enterprise 2016 LTSB", "10.0 (14393)", "bald")]
    [InlineData("Windows 10 IoT Enterprise LTSC", "10.0 (19044)", "ok")]
    [InlineData("Windows 11 Pro", "10.0 (26100)", "bald")]
    [InlineData("Windows 11 Enterprise", "10.0 (26100)", "ok")]
    [InlineData("Windows 11 Pro", "10.0 (22631)", "aus")]
    [InlineData("Windows Server 2012 R2 Standard", "6.3 (9600)", "aus")]
    [InlineData("Windows Server 2016 Standard", "10.0 (14393)", "bald")]
    [InlineData("Windows Server 2022 Standard", "10.0 (20348)", "ok")]
    [InlineData("Linux", "", "unbekannt")]
    public void Lebenszyklus_Nach_Version(string os, string version, string expected)
        => Assert.Equal(expected, AdAnalysis.LifecycleClass(AdAnalysis.Lifecycle(os, version).EndOfSupport, Today));

    [Fact]
    public void Windows11_Bekommt_Release_Bezeichnung()
        => Assert.Equal("Windows 11 Pro 24H2", AdAnalysis.Lifecycle("Windows 11 Pro", "10.0 (26100)").Product);

    [Fact]
    public void Zustand_Zaehlt_Jeden_Computer_Einmal_Mit_Vorrang()
    {
        Assert.Equal(AdHealth.Disabled, AdAnalysis.Health(Pc("A", "Windows XP Professional", "5.1 (2600)", enabled: false), Now));
        Assert.Equal(AdHealth.Unsupported, AdAnalysis.Health(Pc("B", "Windows 7 Professional", "6.1 (7601)", logonDaysAgo: 400), Now));
        Assert.Equal(AdHealth.Inactive, AdAnalysis.Health(Pc("C", logonDaysAgo: 91), Now));
        // Nie angemeldet ist keine Inaktivitaet (eigene Zaehlung IsNeverUsed), sonst zaehlen die Seiten verschieden.
        Assert.Equal(AdHealth.Ok, AdAnalysis.Health(Pc("D", logonDaysAgo: null), Now));
        Assert.Equal(AdHealth.Ok, AdAnalysis.Health(Pc("E", "Windows 11 Enterprise"), Now));
    }

    [Theory]
    [InlineData("Windows 10 Pro", "10.0 (22631)", "Windows 11 Pro 23H2")]
    [InlineData("Windows 10 Enterprise", "10.0 (26100)", "Windows 11 Enterprise 24H2")]
    [InlineData("Windows 10 Pro", "10.0 (19045)", "Windows 10 Pro 22H2")]
    [InlineData("Windows 10 Pro", "10.0 (22000)", "Windows 11 Pro 21H2")]
    [InlineData("Windows 11 Pro", "", "Windows 11 Pro")]
    public void Windows_Version_Entscheidet_Der_Build_Vor_Dem_Text(string os, string version, string expected)
        => Assert.Equal(expected, AdAnalysis.Lifecycle(os, version).Product);

    [Fact]
    public void Windows11_Mit_Text_Windows10_Hat_Windows11_Supportende()
    {
        var life = AdAnalysis.Lifecycle("Windows 10 Pro", "10.0 (22631)");
        Assert.Equal(new DateOnly(2025, 11, 11), life.EndOfSupport);
        Assert.Equal("aus", AdAnalysis.LifecycleClass(life.EndOfSupport, Today));
        // Windows 11 24H2 Pro endet erst 2026-10-13, Windows 10 22H2 war schon 2025 abgelaufen.
        Assert.Equal(new DateOnly(2026, 10, 13), AdAnalysis.Lifecycle("Windows 10 Pro", "10.0 (26100)").EndOfSupport);
    }

    [Fact]
    public void Windows10_22h2_Und_Server2012_Sind_Nur_Esu_Bis_Oktober_2026()
    {
        Assert.True(AdAnalysis.IsEsuOnly(AdAnalysis.Lifecycle("Windows 10 Pro", "10.0 (19045)"), Today));
        Assert.True(AdAnalysis.IsEsuOnly(AdAnalysis.Lifecycle("Windows Server 2012 R2 Standard", "6.3 (9600)"), Today));
        Assert.False(AdAnalysis.IsEsuOnly(AdAnalysis.Lifecycle("Windows 10 Pro", "10.0 (19045)"), new DateOnly(2026, 10, 14)));
        Assert.False(AdAnalysis.IsEsuOnly(AdAnalysis.Lifecycle("Windows Server 2016 Standard", "10.0 (14393)"), Today));
    }

    [Fact]
    public void Ltsc_Name_Doppelt_Das_Jahr_Nicht()
    {
        Assert.Equal("Windows 10 Enterprise 2016 LTSB", AdAnalysis.Lifecycle("Windows 10 Enterprise 2016 LTSB", "10.0 (14393)").Product);
        Assert.Equal("Windows 10 Enterprise LTSC 2019", AdAnalysis.Lifecycle("Windows 10 Enterprise LTSC", "10.0 (17763)").Product);
    }

    [Fact]
    public void Inaktiv_Hat_Eine_Definition_Und_Nie_Angemeldet_Ist_Getrennt()
    {
        var never = Pc("N", logonDaysAgo: null);
        Assert.False(AdAnalysis.IsInactive(never, Now));
        Assert.True(AdAnalysis.IsNeverUsed(never, Now));
        Assert.False(AdAnalysis.IsNeverUsed(never with { CreatedUtc = Now.AddDays(-10) }, Now));
        Assert.False(AdAnalysis.IsNeverUsed(never with { CreatedUtc = null }, Now));
        Assert.True(AdAnalysis.IsInactive(Pc("A", logonDaysAgo: 91), Now));
        Assert.False(AdAnalysis.IsInactive(Pc("B", logonDaysAgo: 89), Now));
        Assert.False(AdAnalysis.IsInactive(Pc("C", logonDaysAgo: 400, enabled: false), Now));

        var result = new AdComputerResult { Enabled = true, Computers = [never, Pc("A", logonDaysAgo: 91), Pc("B")] };
        var stale = Assert.Single(result.Stale(90));
        Assert.Equal("A", stale.Name);
    }

    [Fact]
    public void Aktiv_Heisst_44_Tage_Wegen_Der_Ungenauigkeit_Des_Zeitstempels()
    {
        Assert.True(AdAnalysis.IsActiveRecently(Pc("A", logonDaysAgo: 43), Now));
        Assert.True(AdAnalysis.IsActiveRecently(Pc("B", logonDaysAgo: 35), Now));
        Assert.False(AdAnalysis.IsActiveRecently(Pc("C", logonDaysAgo: 45), Now));
        Assert.False(AdAnalysis.IsActiveRecently(Pc("D", logonDaysAgo: null), Now));
        Assert.False(AdAnalysis.IsActiveRecently(Pc("E", logonDaysAgo: 1, enabled: false), Now));
    }

    [Theory]
    [InlineData("[LDAP://cn={31B2F340-016D-11D2-945F-00C04FB984F9},cn=policies,cn=system,DC=x,DC=local;0]", true)]
    [InlineData("[LDAP://cn={31B2F340-016D-11D2-945F-00C04FB984F9},cn=policies,cn=system,DC=x,DC=local;1]", false)]
    [InlineData("[LDAP://cn={31B2F340-016D-11D2-945F-00C04FB984F9},cn=policies,cn=system,DC=x,DC=local;1][LDAP://cn={6AC1786C-016F-11D2-945F-00C04FB984F9},cn=policies,cn=system,DC=x,DC=local;2]", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Ou_Hat_Gpo_Link_Nur_Bei_Aktiver_Verknuepfung(string? gpLink, bool expected)
        => Assert.Equal(expected, AdInfraAnalysis.HasActiveGpoLink(gpLink));

    [Fact]
    public void Teilweise_Abgedeckter_24er_Wird_Als_Teilweise_Gemeldet_Und_Voll_Abgedeckter_Nicht()
    {
        var clients = new[] { new NetworkSubnetStat("10.1.1.0/24", 3, 0, 0), new NetworkSubnetStat("10.1.2.0/24", 5, 0, 0), new NetworkSubnetStat("10.1.3.0/24", 2, 0, 0) };
        var subnets = new[] { new AdSubnet("10.1.1.0/25", "A"), new AdSubnet("10.1.2.0/25", "A"), new AdSubnet("10.1.2.128/25", "A") };
        var ranges = AdAnalysis.UncoveredRanges([], clients, subnets);

        // 10.1.2.0/24 ist durch zwei /25 voll abgedeckt, 10.1.1.0 liegt im AD-Subnetz, die obere Haelfte aber nicht.
        Assert.DoesNotContain(ranges, r => r.Range == "10.1.2.0/24");
        Assert.True(Assert.Single(ranges, r => r.Range == "10.1.1.0/24").Partial);
        Assert.False(Assert.Single(ranges, r => r.Range == "10.1.3.0/24").Partial);
        Assert.Equal(128, AdAnalysis.CoveredAddresses24("10.1.1.0/24", subnets));
    }

    [Fact]
    public void Dns_Eintrag_Im_Teilweise_Abgedeckten_Bereich_Ist_Teilweise()
    {
        var dns = new[] { new AdDnsEntry("A", "a", ["10.1.1.200"], false) };
        var range = Assert.Single(AdAnalysis.UncoveredRanges(dns, [], [new AdSubnet("10.1.1.0/25", "A")]));
        Assert.True(range.Partial);
        Assert.Equal(1, range.Computers);
    }

    [Fact]
    public void Dns_Nur_Mit_Ipv6_Ist_Aufloesbar()
    {
        Assert.False(new AdDnsEntry("A", "a", [], false) { Ipv6 = ["fd00::5"] }.Unresolvable);
        Assert.True(new AdDnsEntry("B", "b", [], false).Unresolvable);
        Assert.True(new AdDnsEntry("C", "c", ["10.0.0.1"], true).Unresolvable);
    }

    [Fact]
    public void Wirkende_Gpos_Sperre_Wirkt_Auf_Alle_Ebenen_Darueber_Und_Reihenfolge_Aus_Gplink()
    {
        const string root = "DC=x,DC=local", a = "OU=A,DC=x,DC=local", b = "OU=B,OU=A,DC=x,DC=local", c = "OU=C,OU=B,OU=A,DC=x,DC=local";
        static AdGpoLink L(string dn, bool enforced = false, int order = 0) => new(dn, dn, "OU", true, enforced, order);
        var gpos = new[]
        {
            new AdGpo { Guid = "1", Name = "Domaene", Links = [L(root)] },
            new AdGpo { Guid = "2", Name = "DomaeneErzwungen", Links = [L(root, true)] },
            new AdGpo { Guid = "3", Name = "A", Links = [L(a)] },
            new AdGpo { Guid = "4", Name = "B zweite", Links = [L(b, order: 1)] },
            new AdGpo { Guid = "5", Name = "B erste", Links = [L(b, order: 0)] },
            new AdGpo { Guid = "6", Name = "C", Links = [L(c)] }
        };
        var ous = new[]
        {
            new AdOuNode(root, "(Domaene)", 0, 0, 0, false), new AdOuNode(a, "A", 1, 0, 1, false),
            new AdOuNode(b, "B", 2, 0, 2, true), new AdOuNode(c, "C", 3, 1, 1, false)
        };

        // B sperrt: von C aus sind A und die nicht erzwungene Domaenen-GPO weg, die erzwungene bleibt.
        var names = AdInfraAnalysis.EffectiveGpos(gpos, ous, c).Select(x => x.Gpo.Name).ToList();
        Assert.Equal(["DomaeneErzwungen", "B erste", "B zweite", "C"], names);
        // Ohne Sperre darueber wirkt alles.
        Assert.Equal(["Domaene", "DomaeneErzwungen", "A"], AdInfraAnalysis.EffectiveGpos(gpos, ous, a).Select(x => x.Gpo.Name).Take(3).ToList());
    }

    [Fact]
    public void Passwort_Alt_Nur_Bei_Aktiven_Geraeten()
    {
        Assert.True(AdAnalysis.PasswordStale(Pc("A") with { PwdLastSetUtc = Now.AddDays(-61) }, Now));
        Assert.False(AdAnalysis.PasswordStale(Pc("B", logonDaysAgo: 100) with { PwdLastSetUtc = Now.AddDays(-200) }, Now));
        Assert.False(AdAnalysis.PasswordStale(Pc("C") with { PwdLastSetUtc = Now.AddDays(-10) }, Now));
    }

    [Fact]
    public void Delegation_Und_Laps_Ohne_Domaenencontroller()
    {
        Assert.True(AdAnalysis.RiskyDelegation(Pc("SRV") with { UnconstrainedDelegation = true }));
        Assert.False(AdAnalysis.RiskyDelegation(Pc("DC1") with { UnconstrainedDelegation = true, IsDomainController = true }));
        Assert.True(AdAnalysis.LapsMissing(Pc("A")));
        Assert.False(AdAnalysis.LapsMissing(Pc("B") with { LapsExpiryUtc = Now.AddDays(10) }));
        Assert.False(AdAnalysis.LapsMissing(Pc("C", "Linux", "")));
        Assert.True(AdAnalysis.LapsOverdue(Pc("D") with { LapsExpiryUtc = Now.AddDays(-20) }, Now));
    }

    [Fact]
    public void Dn_Wird_Zerlegt_Auch_Mit_Escape()
    {
        Assert.Equal("OU=Clients,DC=x,DC=local", AdAnalysis.ParentDn(@"CN=PC\,1,OU=Clients,DC=x,DC=local"));
        Assert.Equal("Werk / Clients", AdAnalysis.ContainerLabel("OU=Clients,OU=Werk,DC=x,DC=local"));
        Assert.Equal("Default-First-Site", AdAnalysis.FirstRdnValue("CN=Default-First-Site,CN=Sites,CN=Configuration,DC=x"));
        Assert.True(AdAnalysis.IsDefaultComputersContainer("CN=Computers,DC=x,DC=local"));
    }

    [Fact]
    public void Tuerme_Je_Container_Mit_Gpo_Angabe()
    {
        var computers = new[] { Pc("A"), Pc("B", logonDaysAgo: 200), Pc("C") with { Container = "CN=Computers,DC=trafag,DC=local" } };
        var towers = AdAnalysis.Towers(computers, [new AdOu("OU=Clients,DC=trafag,DC=local", true)], Now);

        Assert.Equal(2, towers.Count);
        Assert.Equal(2, towers[0].Total);
        Assert.Equal(1, towers[0].Inactive);
        Assert.True(towers[0].HasGpoLink);
        Assert.True(towers[1].IsDefaultContainer);
    }

    [Theory]
    [InlineData("10.120.5.77", "10.120.0.0/16", true)]
    [InlineData("10.121.5.77", "10.120.0.0/16", false)]
    [InlineData("172.16.9.4", "172.16.8.0/23", true)]
    [InlineData("172.16.10.4", "172.16.8.0/23", false)]
    public void Cidr_Enthaelt(string ip, string cidr, bool expected)
        => Assert.Equal(expected, AdAnalysis.InCidr(IPAddress.Parse(ip), cidr));

    [Fact]
    public void Bereiche_Ohne_Ad_Subnetz_Aus_Dns_Und_Browsern()
    {
        var dns = new[] { new AdDnsEntry("A", "a", ["10.120.5.7"], false), new AdDnsEntry("B", "b", ["192.168.7.9"], false) };
        var clients = new[] { new NetworkSubnetStat("172.16.9.0/24", 4, 1, 0) };
        var ranges = AdAnalysis.UncoveredRanges(dns, clients, [new AdSubnet("10.120.0.0/16", "Bubikon")]);

        Assert.Equal(2, ranges.Count);
        Assert.Contains(ranges, r => r.Range == "192.168.7.0/24" && r.Computers == 1);
        Assert.Contains(ranges, r => r.Range == "172.16.9.0/24" && r.ClientSessions == 4);
    }

    [Fact]
    public void Doppelte_Ips()
    {
        var dns = new[] { new AdDnsEntry("A", "a", ["10.0.0.5"], false), new AdDnsEntry("B", "b", ["10.0.0.5"], false), new AdDnsEntry("C", "c", ["10.0.0.6"], false) };
        var d = Assert.Single(AdAnalysis.DuplicateIps(dns));
        Assert.Equal(["A", "B"], d.Names);
    }

    [Fact]
    public void Neue_Konten_Je_Monat_Ueber_Fenster()
    {
        var list = AdAnalysis.CreatedPerMonth([Pc("A") with { CreatedUtc = new DateTime(2026, 9, 3) }, Pc("B") with { CreatedUtc = new DateTime(2026, 9, 20) }], Now, 3);
        Assert.Equal(3, list.Count);
        Assert.Equal((new DateOnly(2026, 9, 1), 2), list[1]);
    }

    [Fact]
    public void Laps_Und_BitLocker_Ohne_Jedes_Datum_Gelten_Als_Nicht_Lesbar()
    {
        var none = new AdComputerResult { Enabled = true, Computers = [Pc("A"), Pc("B")] };
        var some = new AdComputerResult { Enabled = true, Computers = [Pc("A") with { LapsExpiryUtc = Now, BitLockerKeys = 1 }, Pc("B")] };

        Assert.False(none.LapsReadable);
        Assert.False(none.BitLockerReadable);
        Assert.True(some.LapsReadable);
        Assert.True(some.BitLockerReadable);
    }

    [Theory]
    [InlineData("VeeamGuestHelperSvc", "Backup (Veeam)")]
    [InlineData("MSSQLSvc", "SQL Server")]
    [InlineData("HOST", "HOST")]
    public void Dienstklassen_Werden_Gruppiert(string spn, string expected)
        => Assert.Equal(expected, AdAnalysis.ServiceGroup(spn));
}
