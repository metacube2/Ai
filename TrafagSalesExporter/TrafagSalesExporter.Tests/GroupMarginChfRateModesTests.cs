using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Tests;

public class GroupMarginChfRateModesTests
{
    [Fact]
    public void ResolveRateDate_UsesTodayForExistingDailyDefault()
    {
        var today = new DateTime(2026, 8, 28);

        Assert.Equal(today, GroupMarginChfRateModes.ResolveRateDate(
            GroupMarginChfRateModes.CurrentDailyRate, 2025, today));
    }

    [Fact]
    public void ResolveRateDate_UsesFinanceYearEndForReproducibleMode()
    {
        Assert.Equal(new DateTime(2025, 12, 31), GroupMarginChfRateModes.ResolveRateDate(
            GroupMarginChfRateModes.FinanceYearEndRate, 2025, new DateTime(2026, 8, 28)));
    }

    [Fact]
    public void Normalize_UsesExistingDailyDefaultForUnknownValue()
    {
        Assert.Equal(GroupMarginChfRateModes.CurrentDailyRate, GroupMarginChfRateModes.Normalize("anything"));
    }
}
