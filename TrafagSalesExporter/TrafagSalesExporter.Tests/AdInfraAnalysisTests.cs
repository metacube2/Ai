using System.Buffers.Binary;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public sealed class AdInfraAnalysisTests
{
    private static readonly DateTime Now = new(2026, 10, 2, 8, 0, 0, DateTimeKind.Utc);

    private static byte[] DnsRecord(ushort type, uint hoursSince1601, byte[] data)
    {
        var raw = new byte[24 + data.Length];
        BinaryPrimitives.WriteUInt16LittleEndian(raw.AsSpan(0), (ushort)data.Length);
        BinaryPrimitives.WriteUInt16LittleEndian(raw.AsSpan(2), type);
        raw[4] = 5;
        BinaryPrimitives.WriteUInt32BigEndian(raw.AsSpan(12), 1200);
        BinaryPrimitives.WriteUInt32LittleEndian(raw.AsSpan(20), hoursSince1601);
        data.CopyTo(raw, 24);
        return raw;
    }

    [Fact]
    public void Dns_Eintrag_Mit_Zeitstempel_Und_Ip()
    {
        var hours = (uint)(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc) - new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalHours;
        var rec = AdInfraAnalysis.ParseDnsRecord(DnsRecord(1, hours, [10, 120, 16, 13]))!;

        Assert.Equal(1, rec.Type);
        Assert.Equal(1200u, rec.TtlSeconds);
        Assert.False(rec.IsStatic);
        Assert.Equal(new DateTime(2026, 9, 1), rec.StampUtc);
        Assert.Equal("10.120.16.13", rec.IPv4);
        Assert.True(AdInfraAnalysis.ParseDnsRecord(DnsRecord(1, 0, [1, 2, 3, 4]))!.IsStatic);
        Assert.Null(AdInfraAnalysis.ParseDnsRecord([1, 2, 3]));
    }

    [Fact]
    public void Dns_Zoneneigenschaft_Id_Und_Wert()
    {
        var raw = new byte[25];
        BinaryPrimitives.WriteUInt32LittleEndian(raw.AsSpan(0), 4);
        BinaryPrimitives.WriteUInt32LittleEndian(raw.AsSpan(12), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(raw.AsSpan(16), AdInfraAnalysis.DnsZoneNoRefresh);
        BinaryPrimitives.WriteUInt32LittleEndian(raw.AsSpan(20), 168);

        Assert.Equal((AdInfraAnalysis.DnsZoneNoRefresh, 168u), AdInfraAnalysis.ParseDnsProperty(raw));
    }

    [Fact]
    public void Gplink_Mit_Deaktiviert_Und_Erzwungen()
    {
        var links = AdInfraAnalysis.ParseGpLink(
            "[LDAP://cn={31B2F340-016D-11D2-945F-00C04FB984F9},cn=policies,cn=system,DC=x,DC=local;0][LDAP://cn={6ac1786c-016f-11d2-945f-00c04fb984f9},cn=policies,cn=system,DC=x;3]");

        Assert.Equal(2, links.Count);
        Assert.Equal(("{31B2F340-016D-11D2-945F-00C04FB984F9}", true, false), links[0]);
        Assert.Equal(("{6AC1786C-016F-11D2-945F-00C04FB984F9}", false, true), links[1]);
        Assert.Empty(AdInfraAnalysis.ParseGpLink(" "));
    }

    [Fact]
    public void Gpo_Version_Und_Gpt_Ini()
    {
        Assert.Equal((3, 17), AdInfraAnalysis.SplitGpoVersion((3 << 16) | 17));
        Assert.Equal(196625, AdInfraAnalysis.GptIniVersion("[General]\r\nVersion=196625\r\ndisplayName=Neue GPO"));
        Assert.Null(AdInfraAnalysis.GptIniVersion("[General]"));
    }

    [Fact]
    public void Gpo_Zustand_Mit_Vorrang()
    {
        var link = new AdGpoLink("OU=A,DC=x", "A", "OU", true, false);
        Assert.Equal("aus", AdInfraAnalysis.GpoState(new AdGpo { Flags = 3, Links = [link] }));
        Assert.Equal("unverknuepft", AdInfraAnalysis.GpoState(new AdGpo { HasComputerSettings = true }));
        Assert.Equal("verknuepfung-aus", AdInfraAnalysis.GpoState(new AdGpo { HasComputerSettings = true, Links = [link with { Enabled = false }] }));
        Assert.Equal("leer", AdInfraAnalysis.GpoState(new AdGpo { Links = [link] }));
        Assert.Equal("version", AdInfraAnalysis.GpoState(new AdGpo { HasUserSettings = true, AdVersion = 5, SysvolVersion = 4, Links = [link] }));
        Assert.Equal("ok", AdInfraAnalysis.GpoState(new AdGpo { HasUserSettings = true, AdVersion = 5, SysvolVersion = 5, Links = [link] }));
    }

    [Fact]
    public void Replikationspartner_Aus_Xml()
    {
        const string xml = "<DS_REPL_NEIGHBOR><pszNamingContext>DC=trafagch,DC=local</pszNamingContext>"
            + "<pszSourceDsaDN>CN=NTDS Settings,CN=TRAGVDC401,CN=Servers,CN=Datacenter,CN=Sites,CN=Configuration,DC=trafagch,DC=local</pszSourceDsaDN>"
            + "<ftimeLastSyncSuccess>2026-10-02T06:44:52Z</ftimeLastSyncSuccess><ftimeLastSyncAttempt>2026-10-02T06:44:52Z</ftimeLastSyncAttempt>"
            + "<dwLastSyncResult>0</dwLastSyncResult><cNumConsecutiveSyncFailures>0</cNumConsecutiveSyncFailures></DS_REPL_NEIGHBOR>";
        var l = AdInfraAnalysis.ParseReplNeighbor(xml, "TRAGVDC400")!;

        Assert.Equal("TRAGVDC401", l.Source);
        Assert.Equal("Domäne", l.Partition);
        Assert.Equal(new DateTime(2026, 10, 2, 6, 44, 52, DateTimeKind.Utc), l.LastSuccessUtc);
        Assert.Equal("ok", AdInfraAnalysis.ReplicationState(l, Now));
        Assert.Equal("alt", AdInfraAnalysis.ReplicationState(l, Now.AddDays(2)));
        Assert.Equal("fehler", AdInfraAnalysis.ReplicationState(l with { Failures = 3, LastResult = 1722 }, Now));
        Assert.Null(AdInfraAnalysis.ParseReplNeighbor("kein xml", "X"));
    }

    [Fact]
    public void Doppelte_Spns_Ueber_Konten()
    {
        var d = Assert.Single(AdInfraAnalysis.SpnDuplicates(
        [
            ("SRV1", ["HTTP/web.trafag.local", "HOST/SRV1"]),
            ("SRV2", ["http/web.trafag.local"]),
            ("SRV3", ["HOST/SRV3"])
        ]));
        Assert.Equal(["SRV1", "SRV2"], d.Owners);
    }

    [Fact]
    public void Schnappschuss_Vergleich()
    {
        var day = new DateOnly(2026, 10, 2);
        var before = new[]
        {
            new AdComputerState("A", "OU=Clients,DC=x", true, "Windows 10 Pro 22H2"),
            new AdComputerState("B", "OU=Clients,DC=x", true, "Windows 11 Pro 24H2"),
            new AdComputerState("C", "OU=Clients,DC=x", true, "Windows 11 Pro 24H2")
        };
        var after = new[]
        {
            new AdComputerState("A", "OU=Clients,DC=x", true, "Windows 11 Pro 24H2"),
            new AdComputerState("B", "OU=Deaktiviert,DC=x", false, "Windows 11 Pro 24H2"),
            new AdComputerState("D", "OU=Clients,DC=x", true, "Windows 11 Pro 24H2")
        };
        var changes = AdInfraAnalysis.Diff(before, after, day);

        Assert.Contains(changes, c => c.Name == "A" && c.Change == "betriebssystem");
        Assert.Contains(changes, c => c.Name == "B" && c.Change == "deaktiviert");
        Assert.Contains(changes, c => c.Name == "B" && c.Change == "verschoben" && c.Detail == "Clients → Deaktiviert");
        Assert.Contains(changes, c => c.Name == "C" && c.Change == "entfernt");
        Assert.Contains(changes, c => c.Name == "D" && c.Change == "neu");
        Assert.Equal(5, changes.Count);
    }

    [Fact]
    public void Countdown_Nur_Kuenftige_Supportenden_Im_Fenster()
    {
        AdComputer Pc(string os, string version, bool enabled = true) => new("X", os, Now, Now.AddYears(-1), enabled) { OsVersion = version };
        var list = AdInfraAnalysis.Countdowns(
        [
            Pc("Windows 10 Enterprise 2016 LTSB", "10.0 (14393)"),
            Pc("Windows 10 Enterprise 2016 LTSB", "10.0 (14393)"),
            Pc("Windows 10 Enterprise 2016 LTSB", "10.0 (14393)", enabled: false),
            Pc("Windows 7 Professional", "6.1 (7601)"),
            Pc("Windows Server 2022 Standard", "10.0 (20348)")
        ], DateOnly.FromDateTime(Now));

        var c = Assert.Single(list);
        Assert.Equal(new DateOnly(2026, 10, 13), c.End);
        Assert.Equal(11, c.DaysLeft);
        Assert.Equal(2, c.ActiveDevices);
    }

    [Fact]
    public void Kennzahlen_Ohne_Laps_Wenn_Nicht_Lesbar()
    {
        var ad = new AdComputerResult { Enabled = true, Computers = [new AdComputer("A", "Windows 11 Pro", Now, Now, true) { OsVersion = "10.0 (26100)" }] };
        var m = AdInfraAnalysis.Metrics(ad, Now);

        Assert.Equal(1, m["computer"]);
        Assert.Equal(1, m["aktiv30"]);
        Assert.False(m.ContainsKey("ohneLaps"));
    }

    [Theory]
    [InlineData(7, "Windows Server 2016")]
    [InlineData(10, "Windows Server 2025")]
    [InlineData(null, "?")]
    public void Funktionsebene_Lesbar(int? level, string expected) => Assert.Equal(expected, AdInfraAnalysis.LevelName(level));
}
