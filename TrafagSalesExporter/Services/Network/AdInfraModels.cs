namespace TrafagSalesExporter.Services;

// Modelle der Netzwerk-Unterreiter AD-Infrastruktur, Gruppenrichtlinien, DNS und Verlauf (Wunsch Ingo 2026-10-02).
// Nur Computer-, Konfigurations- und Systemobjekte, keine Personen; Geheimnisse werden nie gelesen.

/// <summary>Eine eingehende Replikationsverbindung eines DCs fuer eine Partition.</summary>
public sealed record AdReplicationLink(string Dc, string Source, string Partition, DateTime? LastSuccessUtc, DateTime? LastAttemptUtc, int Failures, int LastResult, string Message);

/// <summary>Replikationsstand eines Domaenencontrollers; <see cref="Error"/> wenn er nicht abfragbar war.</summary>
public sealed record AdDcReplication(string Dc, string Site, string? Error, IReadOnlyList<AdReplicationLink> Links);

public sealed record AdSiteLinkInfo(string Name, int Cost, int IntervalMinutes, IReadOnlyList<string> Sites);

public sealed record AdSiteInfo(string Name, IReadOnlyList<string> Dcs, IReadOnlyList<string> Subnets);

public sealed record AdKrbtgt(string Name, DateTime? PwdLastSetUtc);

public sealed record AdPasswordPolicy(string Name, int? Precedence, int MinLength, int HistoryLength, double? MaxAgeDays, int LockoutThreshold, bool Complexity, int AppliesToCount);

public sealed record AdTrust(string Name, string Direction, string Type, int Attributes);

public sealed class AdDomainInfo
{
    public string DomainName { get; init; } = "";
    public int? DomainLevel { get; init; }
    public int? ForestLevel { get; init; }
    public int? SchemaVersion { get; init; }
    public bool? RecycleBin { get; init; }
    public int? TombstoneDays { get; init; }
    public int? MachineAccountQuota { get; init; }
    public IReadOnlyList<AdKrbtgt> Krbtgt { get; init; } = [];
    public IReadOnlyList<AdPasswordPolicy> PasswordPolicies { get; init; } = [];
    public IReadOnlyList<AdTrust> Trusts { get; init; } = [];
}

/// <summary>Zertifizierungsstelle aus der AD-Konfiguration (Enterprise-CA oder vertrauenswuerdige Stamm-CA).</summary>
public sealed record AdCaInfo(string Name, string Host, string Kind, string Subject, DateTime? NotAfterUtc, int Templates);

/// <summary>Zertifikat eines Servers mit HTTP-Dienstnamen, aus dem TLS-Handshake auf Port 443.</summary>
public sealed record AdWebCert(string Host, string Subject, string Issuer, DateTime? NotAfterUtc, string? Error);

public sealed record AdServiceAccount(string Name, string Kind, DateTime? PwdLastSetUtc, int? IntervalDays, int SpnCount, DateTime? CreatedUtc);

public sealed record AdSpnDuplicate(string Spn, IReadOnlyList<string> Owners);

public sealed record AdPrinter(string Name, string Server, string Location, string Driver, bool Color, bool Duplex, DateTime? ChangedUtc);

public sealed class AdInfraResult
{
    public bool Enabled { get; init; }
    public string? Error { get; init; }
    public DateTime? ReadAt { get; init; }
    public AdDomainInfo Domain { get; init; } = new();
    public IReadOnlyList<AdSiteInfo> Sites { get; init; } = [];
    public IReadOnlyList<AdSiteLinkInfo> SiteLinks { get; init; } = [];
    public IReadOnlyList<AdDcReplication> Replication { get; init; } = [];
    public IReadOnlyList<AdCaInfo> CertificateAuthorities { get; init; } = [];
    public IReadOnlyList<AdWebCert> WebCertificates { get; init; } = [];
    public IReadOnlyList<AdServiceAccount> ServiceAccounts { get; init; } = [];
    public IReadOnlyList<AdSpnDuplicate> SpnDuplicates { get; init; } = [];
    public IReadOnlyList<AdPrinter> Printers { get; init; } = [];
    public IReadOnlyList<string> Notes { get; init; } = [];
}

/// <summary>Verknuepfung einer GPO mit Domaene, OU oder Standort.</summary>
public sealed record AdGpoLink(string TargetDn, string TargetLabel, string Kind, bool Enabled, bool Enforced);

public sealed class AdGpo
{
    public string Guid { get; init; } = "";
    public string Name { get; init; } = "";
    public DateTime? CreatedUtc { get; init; }
    public DateTime? ChangedUtc { get; init; }
    public int AdVersion { get; init; }
    public int? SysvolVersion { get; init; }
    /// <summary>flags: 1 Benutzerteil aus, 2 Computerteil aus, 3 ganz aus.</summary>
    public int Flags { get; init; }
    public bool HasComputerSettings { get; init; }
    public bool HasUserSettings { get; init; }
    public IReadOnlyList<AdGpoLink> Links { get; init; } = [];
}

/// <summary>OU-Knoten fuer den Sonnenstrahl: Pfad, Computer, eigene GPOs, Vererbung blockiert.</summary>
public sealed record AdOuNode(string Dn, string Label, int Depth, int Computers, int OwnGpos, bool BlocksInheritance);

public sealed class AdGpoResult
{
    public bool Enabled { get; init; }
    public string? Error { get; init; }
    public DateTime? ReadAt { get; init; }
    public IReadOnlyList<AdGpo> Gpos { get; init; } = [];
    public IReadOnlyList<AdOuNode> Ous { get; init; } = [];
    public bool SysvolReadable { get; init; }
    public IReadOnlyList<string> Notes { get; init; } = [];
}

public sealed class AdDnsZone
{
    public string Name { get; init; } = "";
    public string Partition { get; init; } = "";
    public bool? Aging { get; init; }
    public int? NoRefreshHours { get; init; }
    public int? RefreshHours { get; init; }
    public int Nodes { get; init; }
    public int Static { get; init; }
    public int Dynamic { get; init; }
    public int Tombstoned { get; init; }
    /// <summary>Dynamische Eintraege, aelter als NoRefresh + Refresh (Kandidaten fuer Scavenging).</summary>
    public int Stale { get; init; }
    /// <summary>Alter dynamischer Eintraege: bis 7, 30, 90, 365 Tage, aelter.</summary>
    public IReadOnlyList<int> AgeBuckets { get; init; } = [0, 0, 0, 0, 0];
}

public sealed record AdDnsDuplicateName(string Ip, IReadOnlyList<string> Names);

public sealed class AdDnsZoneResult
{
    public bool Enabled { get; init; }
    public string? Error { get; init; }
    public DateTime? ReadAt { get; init; }
    public IReadOnlyList<AdDnsZone> Zones { get; init; } = [];
    public IReadOnlyList<AdDnsDuplicateName> DuplicateA { get; init; } = [];
    public IReadOnlyList<(string Name, string Zone, DateTime StampUtc)> OldestDynamic { get; init; } = [];
    public IReadOnlyList<string> Notes { get; init; } = [];
}

/// <summary>Taeglicher Kennzahlwert fuer den Verlauf.</summary>
public sealed record AdMetricPoint(DateOnly Day, string Metric, double Value);

/// <summary>Aenderung an einem Computerkonto zwischen zwei Schnappschuessen.</summary>
public sealed record AdComputerChange(DateOnly Day, string Name, string Change, string Detail);

/// <summary>Zustand eines Computers im Schnappschuss (fuer den Vergleich mit dem Vortag).</summary>
public sealed record AdComputerState(string Name, string Container, bool Enabled, string Product);

/// <summary>Countdown bis zum Supportende eines Produkts.</summary>
public sealed record AdCountdown(string Product, DateOnly End, int DaysLeft, int ActiveDevices);
