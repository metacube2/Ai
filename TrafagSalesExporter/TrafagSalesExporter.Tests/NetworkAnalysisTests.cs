using System.Net;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public sealed class NetworkAnalysisTests
{
    private static readonly DateTime Now = new(2026, 10, 2, 10, 30, 0, DateTimeKind.Utc);
    private static readonly NetworkTarget Hana = new("hana:travtrp0", "HANA travtrp0", "HANA", "travtrp0", 30015, NetworkProbeKind.Tcp, Sites: ["TRIT"]);
    private static readonly NetworkTarget Sap = new("sap:travp762", "SAP TRAVP762", "SAP", "travp762", 8000, NetworkProbeKind.Http, "http://travp762:8000/sap/public/ping");

    [Fact]
    public void Status_Rechnet_Verfuegbarkeit_Und_Antwortzeit_Ueber_24_Stunden()
    {
        var results = new List<NetworkProbeResult>
        {
            new(Now.AddHours(-30), Hana.Key, false, null, ""),
            new(Now.AddHours(-2), Hana.Key, true, 10, "ip=10.194.65.22;tcp=30015"),
            new(Now.AddHours(-1), Hana.Key, true, 30, "ip=10.194.65.22;tcp=30015"),
            new(Now.AddMinutes(-5), Hana.Key, false, null, "ip=10.194.65.22;error=Zeitueberschreitung")
        };

        var s = Assert.Single(NetworkAnalysisService.Status([Hana], results, Now));

        Assert.Equal(2.0 / 3, s.Availability24h!.Value, 3);
        Assert.Equal(20, s.AvgLatency24h);
        Assert.False(s.Last!.Ok);
        Assert.Equal("10.194.65.22", s.Ips);
        Assert.False(s.Encrypted);
        Assert.Equal(24, s.Latency24h.Count);
    }

    [Fact]
    public void Http_Ohne_Tls_Gilt_Als_Unverschluesselt()
        => Assert.False(Assert.Single(NetworkAnalysisService.Status([Sap], [], Now)).Encrypted);

    [Fact]
    public void Heatmap_Hat_Ein_Feld_Je_Stunde()
    {
        var cells = NetworkAnalysisService.Heatmap(Hana.Key, [new(Now.AddMinutes(-10), Hana.Key, true, 5, "")], Now, 7);

        Assert.Equal(168, cells.Count);
        Assert.Equal(1.0, cells[^1].Availability);
        Assert.Null(cells[0].Availability);
    }

    [Fact]
    public void Exportfehler_Wird_Netz_Wenn_Ziel_Zur_Zeit_Nicht_Erreichbar_War()
    {
        var at = new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc);
        var results = new List<NetworkProbeResult> { new(at.AddMinutes(-5), Hana.Key, false, null, "") };

        Assert.Equal("netz", NetworkAnalysisService.Classify("Error", "Abfrage abgebrochen", "TRIT", at.ToLocalTime(), [Hana], results));
        Assert.Equal("daten", NetworkAnalysisService.Classify("Error", "Spalte Lifnr fehlt", "TRIT", at.ToLocalTime(), [Hana], []));
        Assert.Equal("netz", NetworkAnalysisService.Classify("Error", "Connection timed out", "TRIN", at, [Hana], []));
        Assert.Equal("ok", NetworkAnalysisService.Classify("Success", "", "TRIT", at, [Hana], []));
    }

    [Theory]
    [InlineData("HTTP 503 Service Unavailable", "netz")]
    [InlineData("The operation timed out", "netz")]
    [InlineData("Connection refused", "netz")]
    [InlineData("Zeitueberschreitung beim Abruf", "netz")]
    [InlineData("Spalte 15034 fehlt", "daten")]
    [InlineData("Unbekannter Host-Parameter in Abfrage", "daten")]
    [InlineData("Passwort reset erforderlich fuer Benutzer", "daten")]
    [InlineData("Verbindungsname ungueltig im Mapping", "daten")]
    [InlineData("Header dnsmasq fehlt", "daten")]
    public void Fehlertext_Wird_Nur_Bei_Echten_Netzmeldungen_Netz(string error, string expected)
        => Assert.Equal(expected, NetworkAnalysisService.Classify("Error", error, "TRXX", Now, [], []));

    [Fact]
    public void Umsatz_Ohne_Waehrung_Oder_Kurs_Wird_Ausgelassen_Und_Als_Unvollstaendig_Gemeldet()
    {
        var rows = new[] { ("TRCH", "CHF", 100m), ("TRDE", "EUR", 200m), ("TRDE", "", 50m), ("TRUK", "GBP", 70m) };
        var (chf, incomplete) = NetworkAnalysisService.RevenueInChf(rows, c => c == "EUR" ? 0.9m : null);

        Assert.Equal(100m, chf["TRCH"]);
        Assert.Equal(180m, chf["TRDE"]);
        Assert.False(chf.ContainsKey("TRUK"));
        Assert.Contains("TRDE", incomplete);
        Assert.Contains("TRUK", incomplete);
        Assert.DoesNotContain("TRCH", incomplete);
    }

    [Fact]
    public void Zertifikate_Werden_Aus_Der_Pruefung_Gelesen()
    {
        var tls = new NetworkTarget("tls:x", "X", "Microsoft 365", "x", 443, NetworkProbeKind.Tls);
        var status = NetworkAnalysisService.Status([tls], [new(Now, tls.Key, true, 40, "ip=1.2.3.4;tls=ja;notAfter=2026-11-01;subject=x.com;issuer=CA;tlsErrors=None")], Now);

        var cert = Assert.Single(NetworkAnalysisService.Certificates(status, Now));
        Assert.Equal(29, cert.DaysLeft);
        Assert.Equal("CA", cert.Issuer);
    }

    [Theory]
    [InlineData("10.120.5.77", "10.120.5.0/24")]
    [InlineData("::ffff:10.120.5.77", "10.120.5.0/24")]
    [InlineData("127.0.0.1", "lokal")]
    [InlineData("fe80::1", "IPv6")]
    public void Subnetz_Ohne_Volle_Adresse(string ip, string expected)
        => Assert.Equal(expected, ClientConnectionTracker.SubnetOf(IPAddress.Parse(ip)));

    [Fact]
    public void Subnetz_Ohne_Adresse_Ist_Unbekannt()
        => Assert.Equal("unbekannt", ClientConnectionTracker.SubnetOf(null));
}
