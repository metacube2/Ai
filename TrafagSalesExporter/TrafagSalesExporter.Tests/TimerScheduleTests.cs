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

    // ---- Einkauf-Delta seit 2026-11-20 nachts (03:00) statt hinter dem 12:00-Export ----
    // Nachgeholt wird, wenn seit dem JUENGSTEN Slot kein Lauf erfolgreich war (vorher: Erfolg
    // aelter als 20 Stunden, was einen um 15:10 fertigen Lauf erst am naechsten Tag 11:10 faellig machte).

    [Fact]
    public void MostRecentSlot_NachDemSlot_Heute()
    {
        Assert.Equal(new DateTime(2026, 11, 20, 3, 0, 0),
            TimerSchedule.MostRecentSlot(new DateTime(2026, 11, 20, 9, 0, 0), 3, 0));
    }

    [Fact]
    public void MostRecentSlot_VorDemSlot_Gestern()
    {
        Assert.Equal(new DateTime(2026, 11, 19, 3, 0, 0),
            TimerSchedule.MostRecentSlot(new DateTime(2026, 11, 20, 2, 30, 0), 3, 0));
    }

    [Fact]
    public void IsPurchasingCatchUpDue_NachtlaufErfolgreich_TagsueberNichtFaellig()
    {
        var now = new DateTime(2026, 11, 21, 10, 0, 0);

        Assert.False(TimerSchedule.IsPurchasingCatchUpDue(now, 3, 0, enabled: true,
            lastSuccessLocal: new DateTime(2026, 11, 21, 3, 20, 0)));
    }

    // Seit 2026-11-23: Slot 00:00 (Wunsch Ingo), Nachholen nur bis 06:00, nie in den Arbeitstag.

    [Fact]
    public void IsPurchasingCatchUpDue_NachtlaufVerpasst_NachtsFaellig()
    {
        Assert.True(TimerSchedule.IsPurchasingCatchUpDue(new DateTime(2026, 11, 24, 2, 0, 0), 0, 0, enabled: true,
            lastSuccessLocal: new DateTime(2026, 11, 22, 0, 40, 0)));
    }

    [Fact]
    public void IsPurchasingCatchUpDue_NachtlaufVerpasst_TagsueberNichtFaellig()
    {
        // Fall 23.11.: Deploy 07:37, der Nachhol-Delta ab 07:38 blockierte die Arbeit.
        Assert.False(TimerSchedule.IsPurchasingCatchUpDue(new DateTime(2026, 11, 23, 7, 38, 0), 0, 0, enabled: true,
            lastSuccessLocal: new DateTime(2026, 11, 20, 15, 10, 0)));
        Assert.False(TimerSchedule.IsPurchasingCatchUpDue(new DateTime(2026, 11, 23, 6, 0, 0), 0, 0, true, null));
        Assert.True(TimerSchedule.IsPurchasingCatchUpDue(new DateTime(2026, 11, 23, 5, 59, 0), 0, 0, true, null));
    }

    [Fact]
    public void IsPurchasingCatchUpDue_HeuteNachtSchonErfolgreich_NichtFaellig()
    {
        Assert.False(TimerSchedule.IsPurchasingCatchUpDue(new DateTime(2026, 11, 24, 1, 30, 0), 0, 0, true,
            new DateTime(2026, 11, 24, 0, 25, 0)));
    }

    [Fact]
    public void Standardslot_ist_Mitternacht()
    {
        Assert.Equal(0, TimerSchedule.PurchasingDeltaDefaultHour);
        Assert.Equal(TimeSpan.FromHours(6), TimerSchedule.PurchasingCatchUpWindow);
    }

    [Fact]
    public void PurchasingSlotMode_Sonntag_FullLoad_sonst_Delta()
    {
        // 22.11.2026 ist ein Sonntag.
        Assert.Equal(PurchasingRefreshRunner.ModeFull, TimerSchedule.PurchasingSlotMode(new DateTime(2026, 11, 22, 3, 0, 0), DayOfWeek.Sunday));
        Assert.Equal(PurchasingRefreshRunner.ModeDelta, TimerSchedule.PurchasingSlotMode(new DateTime(2026, 11, 23, 3, 0, 0), DayOfWeek.Sunday));
        Assert.Equal(PurchasingRefreshRunner.ModeDelta, TimerSchedule.PurchasingSlotMode(new DateTime(2026, 11, 22, 3, 0, 0), null));
    }

    [Fact]
    public void IsPurchasingCatchUpDue_TimerAus_False()
    {
        Assert.False(TimerSchedule.IsPurchasingCatchUpDue(new DateTime(2026, 11, 20, 13, 0, 0), 3, 0, enabled: false, lastSuccessLocal: null));
    }
}
