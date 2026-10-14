using TrafagSalesExporter.Components.Home;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Tests;

/// <summary>Startseitengrafik: Laender je Standort und Zustand aus dem Exportprotokoll (2026-10-14).</summary>
public class SiteNetworkTests
{
    private static readonly DateTime Now = new(2026, 10, 14, 10, 0, 0, DateTimeKind.Utc);

    private static ExportLog Log(int siteId, string status, double hoursAgo)
        => new() { SiteId = siteId, Status = status, RowCount = 100, Timestamp = Now.AddHours(-hoursAgo).ToLocalTime() };

    [Theory]
    [InlineData("Schweiz/Oesterreich", "CH,AT")]
    [InlineData("Deutschland", "DE")]
    [InlineData("England", "UK")]
    [InlineData("Einkauf SAP", "")]
    public void Laender_Aus_Dem_Feld_Land(string land, string expected)
        => Assert.Equal(expected, string.Join(',', SiteNetwork.CountriesOf(land).Select(c => c.Code)));

    [Fact]
    public void Zustand_Und_Reihenfolge()
    {
        var sites = new[]
        {
            new Site { Id = 1, TSC = "ZSCHWEIZ", Land = "Schweiz/Oesterreich", SourceSystem = "SAP", IsActive = true },
            new Site { Id = 2, TSC = "TRFR", Land = "Frankreich", SourceSystem = "BI1", IsActive = true },
            new Site { Id = 3, TSC = "TRIN", Land = "Indien", SourceSystem = "SAGE", IsActive = true },
            new Site { Id = 4, TSC = "TRDE", Land = "Deutschland", SourceSystem = "MANUAL_EXCEL", IsActive = true },
            new Site { Id = 5, TSC = "TRUS", Land = "USA", SourceSystem = "BI1", IsActive = true },
            new Site { Id = 6, TSC = "PURCHASING_SAP", Land = "Einkauf SAP", SourceSystem = "SAP", IsActive = false },
            new Site { Id = 7, TSC = "TRIT", Land = "Italien", SourceSystem = "BI1", IsActive = true }
        };
        var logs = new Dictionary<int, ExportLog>
        {
            [1] = Log(1, "OK", 4),
            [2] = Log(2, "OK", 50),          // automatisch, aelter als 36 h: veraltet
            [3] = Log(3, "Error", 2),
            [4] = Log(4, "OK", 24 * 20),     // manuell, 20 Tage: noch aktuell
            [7] = Log(7, "Empty", 2)
        };
        var nodes = SiteNetwork.Build(sites, logs, Now);

        Assert.Equal("CH,DE,FR,US,IN,IT,AT", string.Join(',', nodes.Select(n => n.Code)));
        var state = nodes.ToDictionary(n => n.Code, n => n.State);
        Assert.Equal(SiteNodeState.Ok, state["CH"]);
        Assert.Equal(SiteNodeState.Ok, state["AT"]);
        Assert.Equal(SiteNodeState.Ok, state["DE"]);
        Assert.Equal(SiteNodeState.Stale, state["FR"]);
        Assert.Equal(SiteNodeState.Error, state["IN"]);
        Assert.Equal(SiteNodeState.Unknown, state["US"]);
        Assert.Equal(SiteNodeState.Stale, state["IT"]);
        Assert.Equal("Excel-Import", nodes.Single(n => n.Code == "DE").Source);
    }

    [Fact]
    public void Flagge_Und_Leitung_Sind_Gueltiges_Svg()
    {
        foreach (var code in SiteNetwork.Order)
            Assert.StartsWith("<rect", SiteNetwork.Flag(code, 0, 0));
        Assert.Matches(@"^M[\d.]+,[\d.]+ Q[\d.]+,[\d.]+ [\d.]+,[\d.]+$", SiteNetwork.LinePath(105, 280));
    }
}
