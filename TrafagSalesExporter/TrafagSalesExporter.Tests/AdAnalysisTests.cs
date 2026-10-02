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
        Assert.Equal(AdHealth.Inactive, AdAnalysis.Health(Pc("D", logonDaysAgo: null), Now));
        Assert.Equal(AdHealth.Ok, AdAnalysis.Health(Pc("E", "Windows 11 Enterprise"), Now));
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
}
