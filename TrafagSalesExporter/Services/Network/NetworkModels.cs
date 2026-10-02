namespace TrafagSalesExporter.Services;

/// <summary>
/// Reiter Netzwerk (Wunsch Ingo 2026-10-02). Notschalter wie bei Logistik live:
/// <c>NetworkProbe:Enabled</c> stoppt jede Pruefung, <c>NetworkProbe:AdEnabled</c> schaltet die
/// AD-Computerauswertung frei (Standard aus, bis die IT einverstanden ist).
/// </summary>
public sealed class NetworkProbeOptions
{
    public const string SectionName = "NetworkProbe";
    public bool Enabled { get; set; } = true;
    public int IntervalMinutes { get; set; } = 5;
    public int RetentionDays { get; set; } = 30;
    public bool AdEnabled { get; set; }
}

/// <summary>Art der Pruefung. Nie mit Anmeldedaten, nie Datenbank-Sitzungen, nie Scans.</summary>
public enum NetworkProbeKind
{
    /// <summary>Nur TCP-Verbindungsaufbau (HANA, Terminals).</summary>
    Tcp,
    /// <summary>HTTP GET ohne Anmeldung auf eine oeffentliche Pruefadresse (SAP /sap/public/ping).</summary>
    Http,
    /// <summary>TCP plus TLS-Handshake; liest das Zertifikat (SharePoint, Graph, EZB).</summary>
    Tls
}

/// <summary>Ein bekanntes Pruefziel aus Konfiguration, Firewall-Doku oder eigener Liste.</summary>
public sealed record NetworkTarget(
    string Key, string Label, string Group, string Host, int Port, NetworkProbeKind Kind,
    string? Url = null, string? WorkCenter = null, IReadOnlyList<string>? Sites = null, bool Custom = false);

/// <summary>Ein Pruefergebnis.</summary>
public sealed record NetworkProbeResult(
    DateTime TimestampUtc, string TargetKey, bool Ok, double? LatencyMs, string Detail);

/// <summary>Zertifikatsinformation aus dem TLS-Handshake.</summary>
public sealed record NetworkCertificateInfo(string TargetKey, string Subject, string Issuer, DateTime NotAfter, int DaysLeft);

/// <summary>Eigenes Pruefziel, z. B. ein Rueckmeldeterminal mit Arbeitsplatz.</summary>
public sealed class NetworkWatchTarget
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 443;
    public string WorkCenter { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
