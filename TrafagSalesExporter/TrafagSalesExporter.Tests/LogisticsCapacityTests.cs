using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public sealed class LogisticsCapacityTests
{
    private static readonly DateOnly Day = new(2026, 10, 5);

    [Fact]
    public void Kennzahlen_Nach_Nevas_Definition()
    {
        // 2 Personen x 4 h = 8 Personenstunden, 160 erledigt, 120 offen, Planzeit 3 min, verfuegbar 5 h.
        var e = new LogisticsCapacityEntry(Day, "Wareneingang", 5, 8, 3, 120, 160);

        Assert.Equal(20, e.Productivity);
        Assert.Equal(3, e.MinutesPerUnit);
        Assert.Equal(6, e.RequiredHours);
        Assert.Equal(120, e.LoadPercent!.Value, 6);
        Assert.Equal(1, e.Gap, 6);
    }

    [Fact]
    public void Ohne_Stunden_Keine_Division()
    {
        var e = new LogisticsCapacityEntry(Day, "MLE01", 0, 0, 0, 10, 0);

        Assert.Null(e.Productivity);
        Assert.Null(e.MinutesPerUnit);
        Assert.Null(e.LoadPercent);
        Assert.Equal(0, e.RequiredHours);
    }

    [Fact]
    public void Ruesten_Aus_Live_Nur_Positionen_Mit_Lieferung()
    {
        LiveTransferItem Ta(string delivery, bool confirmed)
            => new("100", "1", "0001", "601", delivery, "M", "1100", "001", "A", "916", "X", 1m, "ST", DateTime.Today, confirmed, null);

        var (open, done) = LogisticsCapacityStore.PickingFromLive([Ta("80001", false), Ta("80001", true), Ta("80002", true), Ta("", false)]);

        Assert.Equal(1, open);
        Assert.Equal(2, done);
    }

    [Fact]
    public void Bereiche_Nach_Unterlage()
    {
        Assert.Equal(8, LogisticsCapacityAreas.All.Length);
        Assert.Contains(LogisticsCapacityAreas.All, a => a.Area == LogisticsCapacityAreas.Picking);
    }
}
