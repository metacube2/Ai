using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public sealed class LogisticsProductivityTests
{
    private static LiveTransferItem Ta(string tanum, string delivery, int createdMinute, int? confirmedMinute, string lgnum = "100")
        => new(lgnum, tanum, "0001", "601", delivery, "36385", "1100", "001", "A-01-1", "916", "0080001234", 1m, "ST",
            new DateTime(2026, 10, 5, 8, createdMinute, 0), confirmedMinute.HasValue,
            confirmedMinute.HasValue ? new DateTime(2026, 10, 5, 8, 0, 0).AddMinutes(confirmedMinute.Value) : null);

    [Fact]
    public void Ruestvorgang_Je_Lieferung_Erster_Angelegt_Bis_Letzte_Quittierung()
    {
        var runs = LogisticsProductivity.PickingRuns(
        [
            Ta("1", "0080001234", 0, 20),
            Ta("2", "0080001234", 5, 45),
            Ta("3", "0080001235", 10, null),
            Ta("4", "", 0, 5)
        ]);

        Assert.Equal(2, runs.Count);
        var done = runs.Single(r => r.Delivery == "80001234");
        Assert.True(done.Done);
        Assert.Equal(45, done.Minutes(new DateTime(2026, 10, 5, 12, 0, 0)), 6);
        var open = runs.Single(r => r.Delivery == "80001235");
        Assert.False(open.Done);
        Assert.Equal(50, open.Minutes(new DateTime(2026, 10, 5, 9, 0, 0)), 6);
    }

    [Fact]
    public void Leistung_Je_Stunde_Und_Lagernummer()
    {
        var output = LogisticsProductivity.HourlyOutput(
        [
            Ta("1", "A", 0, 10, "100"),
            Ta("2", "A", 0, 70, "100"),
            Ta("3", "B", 0, 15, "110"),
            Ta("4", "B", 0, null, "110")
        ]);

        Assert.Equal(3, output.Count);
        Assert.Equal(1, output.Single(o => o.Lgnum == "100" && o.Hour == 8).Confirmed);
        Assert.Equal(1, output.Single(o => o.Lgnum == "100" && o.Hour == 9).Confirmed);
        Assert.Equal(1, output.Single(o => o.Lgnum == "110").Confirmed);
    }

    [Fact]
    public void Quantile()
    {
        Assert.Null(LogisticsProductivity.Quantile([], 0.5));
        Assert.Equal(20, LogisticsProductivity.Quantile([10, 20, 30], 0.5));
        Assert.Equal(26, LogisticsProductivity.Quantile([10, 20, 30], 0.8)!.Value, 6);
    }
}
