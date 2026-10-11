using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class TimerScheduleTests
{
    [Fact]
    public void ComputeNextRun_BeforeSlot_ReturnsToday()
    {
        var now = new DateTime(2026, 7, 8, 9, 0, 0);

        var next = TimerSchedule.ComputeNextRun(now, 12, 0);

        Assert.Equal(new DateTime(2026, 7, 8, 12, 0, 0), next);
    }

    [Fact]
    public void ComputeNextRun_AfterSlot_ReturnsTomorrow()
    {
        var now = new DateTime(2026, 7, 8, 13, 0, 0);

        var next = TimerSchedule.ComputeNextRun(now, 12, 0);

        Assert.Equal(new DateTime(2026, 7, 9, 12, 0, 0), next);
    }

    [Fact]
    public void IsCatchUpDue_TimerDisabled_False()
    {
        var now = new DateTime(2026, 7, 8, 13, 0, 0);

        Assert.False(TimerSchedule.IsCatchUpDue(now, 12, 0, enabled: false, lastRunLocal: null));
    }

    [Fact]
    public void IsCatchUpDue_BeforeSlot_False()
    {
        var now = new DateTime(2026, 7, 8, 11, 59, 0);

        Assert.False(TimerSchedule.IsCatchUpDue(now, 12, 0, enabled: true, lastRunLocal: null));
    }

    [Fact]
    public void IsCatchUpDue_AfterSlot_NoPreviousRun_True()
    {
        var now = new DateTime(2026, 7, 8, 13, 0, 0);

        Assert.True(TimerSchedule.IsCatchUpDue(now, 12, 0, enabled: true, lastRunLocal: null));
    }

    [Fact]
    public void IsCatchUpDue_AfterSlot_AlreadyRanToday_False()
    {
        var now = new DateTime(2026, 7, 8, 13, 0, 0);
        var ranToday = new DateTime(2026, 7, 8, 12, 0, 5);

        Assert.False(TimerSchedule.IsCatchUpDue(now, 12, 0, enabled: true, lastRunLocal: ranToday));
    }

    [Fact]
    public void IsCatchUpDue_AfterSlot_LastRunYesterday_True()
    {
        var now = new DateTime(2026, 7, 8, 13, 0, 0);
        var ranYesterday = new DateTime(2026, 7, 7, 12, 0, 3);

        Assert.True(TimerSchedule.IsCatchUpDue(now, 12, 0, enabled: true, lastRunLocal: ranYesterday));
    }
}
