using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public sealed class LogisticsBinLayoutTests
{
    private static LiveTransferItem Ta(string tanum, string fromType, string fromBin, string toType, string toBin, bool confirmed, int minute = 0)
        => new("100", tanum, "0001", "601", "", "36385", "1100", fromType, fromBin, toType, toBin, 2m, "ST",
            new DateTime(2026, 10, 5, 8, minute, 0), confirmed, confirmed ? new DateTime(2026, 10, 5, 9, minute, 0) : null);

    [Theory]
    [InlineData("01-02-03", "01", 2, 3)]
    [InlineData("A-01-2", "A", 1, 2)]
    [InlineData("A-07", "A", 7, 1)]
    [InlineData("A0102", "A", 1, 2)]
    [InlineData("b05", "B", 5, 1)]
    [InlineData("010203", "01", 2, 3)]
    [InlineData("1-008-B", "1", 8, 2)]
    [InlineData("BP-MLE04-2", "BP", 4, 2)]
    [InlineData("DL20004", "DL", 200, 4)]
    [InlineData("EDB0102", "EDB", 1, 2)]
    public void Platzname_Wird_Zerlegt(string bin, string aisle, int column, int level)
    {
        var p = LogisticsBinLayout.Parse(bin);

        Assert.Equal((aisle, column, level), p);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0080001234")]
    [InlineData("WE-ZONE")]
    public void Ohne_Muster_Null(string bin) => Assert.Null(LogisticsBinLayout.Parse(bin));

    [Fact]
    public void Entnahme_Und_Einlagerung_Je_Platz()
    {
        var bins = LogisticsBinLayout.Bins(
        [
            Ta("1", "001", "A-01-1", "916", "0080001234", confirmed: true),
            Ta("2", "001", "A-01-1", "916", "0080001234", confirmed: false, minute: 5),
            Ta("3", "902", "WE", "001", "A-01-1", confirmed: true, minute: 9)
        ]);

        var a = bins.Single(b => b.Bin == "A-01-1");
        Assert.Equal(2, a.Picks);
        Assert.Equal(1, a.PicksOpen);
        Assert.Equal(1, a.Puts);
        Assert.True(a.HasOpen);
        Assert.Equal(3, a.Moves.Count);
        Assert.Equal(("A", 1, 1), (a.Aisle, a.Column, a.Level));
    }

    [Fact]
    public void Gaenge_Ohne_Schnittstellen_Und_Verdichtet()
    {
        var bins = LogisticsBinLayout.Bins(
        [
            Ta("1", "001", "A-03-1", "916", "0080001234", true),
            Ta("2", "001", "A-17-4", "916", "0080001234", true),
            Ta("3", "001", "X", "916", "0080001235", true)
        ]);

        var aisles = LogisticsBinLayout.Aisles(bins);
        var zones = LogisticsBinLayout.Zones(bins);

        var a = aisles.Single(x => x.Aisle == "A");
        Assert.Equal(2, a.Columns);
        Assert.Equal(2, a.Levels);
        Assert.Contains(aisles, x => x.Aisle == "~1" && x.Bins.Single().Bin == "X");
        Assert.DoesNotContain(aisles, x => x.Type == "916");
        Assert.Equal(("916", 2, 3, 0), zones.Single());
    }
}
