using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Auswahl der Belege im Einkauf-Delta. Hintergrund 2026-11-20: SAP ignoriert den Aedat-Filter auf
/// EKKOSet, der Delta lud deshalb alle 178'505 Belege nach und lief 90 Minuten.
/// </summary>
public class PurchasingDeltaSelectionTests
{
    private static readonly HashSet<string> Cache = new(StringComparer.OrdinalIgnoreCase)
    {
        "4500000001", "4500000002", "4500000003"
    };

    [Fact]
    public void Alter_bekannter_Beleg_wird_nicht_nachgeladen()
    {
        var result = PurchasingDeltaSelection.SelectChanged(
            [("4500000001", "2025-03-14")], Cache, new DateTime(2026, 11, 19));

        Assert.Empty(result);
    }

    [Fact]
    public void Neuer_Beleg_wird_immer_geladen_auch_mit_altem_Datum()
    {
        var result = PurchasingDeltaSelection.SelectChanged(
            [("4500009999", "2025-03-14")], Cache, new DateTime(2026, 11, 19));

        Assert.Equal(["4500009999"], result);
    }

    [Fact]
    public void Bekannter_Beleg_ab_Stichtag_wird_geladen()
    {
        var result = PurchasingDeltaSelection.SelectChanged(
            [("4500000002", "2026-11-19"), ("4500000003", "2026-11-18")], Cache, new DateTime(2026, 11, 19, 17, 36, 0));

        Assert.Equal(["4500000002"], result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("kein Datum")]
    [InlineData("00000000")]
    public void Unlesbares_Datum_gilt_als_geaendert(string aedat)
    {
        var result = PurchasingDeltaSelection.SelectChanged(
            [("4500000001", aedat)], Cache, new DateTime(2026, 11, 19));

        Assert.Equal(["4500000001"], result);
    }

    [Fact]
    public void Doppelte_Belege_nur_einmal()
    {
        var result = PurchasingDeltaSelection.SelectChanged(
            [("4500009999", "2026-11-20"), ("4500009999", "2026-11-20"), ("", "2026-11-20")], Cache, new DateTime(2026, 11, 19));

        Assert.Equal(["4500009999"], result);
    }

    [Theory]
    [InlineData("2026-11-19", 2026, 11, 19)]
    [InlineData("2026-11-19T00:00:00", 2026, 11, 19)]
    [InlineData("20261119", 2026, 11, 19)]
    [InlineData("/Date(1763510400000)/", 2025, 11, 19)]
    public void Sap_Datumsformate(string text, int y, int m, int d)
    {
        Assert.Equal(new DateTime(y, m, d), PurchasingDeltaSelection.ParseSapDate(text));
    }
}
