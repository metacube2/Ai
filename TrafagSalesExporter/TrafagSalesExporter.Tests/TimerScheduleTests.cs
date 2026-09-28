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

    // ---- Nachhol-Delta Einkauf (Befund 2026-09-28: vom 10.09. bis 28.09. kein einziger Lauf) ----

    [Fact]
    public void IsPurchasingCatchUpDue_VorDemSlot_False()
    {
        var now = new DateTime(2026, 9, 28, 7, 0, 0);

        Assert.False(TimerSchedule.IsPurchasingCatchUpDue(now, 12, 0, enabled: true,
            lastSuccessLocal: new DateTime(2026, 9, 10, 13, 19, 0)));
    }

    [Fact]
    public void IsPurchasingCatchUpDue_NachDemSlot_LetzterErfolgVorTagen_True()
    {
        var now = new DateTime(2026, 9, 28, 13, 0, 0);

        Assert.True(TimerSchedule.IsPurchasingCatchUpDue(now, 12, 0, enabled: true,
            lastSuccessLocal: new DateTime(2026, 9, 10, 13, 19, 0)));
    }

    [Fact]
    public void IsPurchasingCatchUpDue_NachDemSlot_HeuteSchonErfolgreich_False()
    {
        var now = new DateTime(2026, 9, 28, 15, 0, 0);

        Assert.False(TimerSchedule.IsPurchasingCatchUpDue(now, 12, 0, enabled: true,
            lastSuccessLocal: new DateTime(2026, 9, 28, 13, 15, 0)));
    }

    [Fact]
    public void IsPurchasingCatchUpDue_GesternSpaetFertig_HeuteNachDemSlotAbZwanzigStunden()
    {
        var yesterdayLate = new DateTime(2026, 9, 27, 17, 0, 0);

        // 12:30 ist erst 19,5 Stunden spaeter: noch nicht faellig.
        Assert.False(TimerSchedule.IsPurchasingCatchUpDue(new DateTime(2026, 9, 28, 12, 30, 0), 12, 0, true, yesterdayLate));
        // 13:00 sind genau 20 Stunden: faellig.
        Assert.True(TimerSchedule.IsPurchasingCatchUpDue(new DateTime(2026, 9, 28, 13, 0, 0), 12, 0, true, yesterdayLate));
    }

    [Fact]
    public void IsPurchasingCatchUpDue_OhneJedenErfolg_NachDemSlot_True()
    {
        Assert.True(TimerSchedule.IsPurchasingCatchUpDue(new DateTime(2026, 9, 28, 12, 5, 0), 12, 0, true, null));
    }

    [Fact]
    public void IsPurchasingCatchUpDue_TimerAus_False()
    {
        Assert.False(TimerSchedule.IsPurchasingCatchUpDue(new DateTime(2026, 9, 28, 13, 0, 0), 12, 0, enabled: false, lastSuccessLocal: null));
    }
}
