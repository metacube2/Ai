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

    [Fact]
    public void IsPurchasingCatchUpDue_NachtlaufVerpasst_MorgensFaellig()
    {
        // Fall 19./20.11.: Worker um 21:20 beendet, erst um 06:21 wieder gestartet.
        var now = new DateTime(2026, 11, 20, 6, 22, 0);

        Assert.True(TimerSchedule.IsPurchasingCatchUpDue(now, 3, 0, enabled: true,
            lastSuccessLocal: new DateTime(2026, 11, 19, 17, 36, 0)));
    }

    [Fact]
    public void IsPurchasingCatchUpDue_GesternNachmittagFertig_HeuteVorDemSlot_NichtFaellig()
    {
        // 20.11. 15:10 fertig; am 21.11. um 01:00 ist der juengste Slot der vom 20.11. 03:00.
        Assert.False(TimerSchedule.IsPurchasingCatchUpDue(new DateTime(2026, 11, 21, 1, 0, 0), 3, 0, true,
            new DateTime(2026, 11, 20, 15, 10, 0)));
        // Nach dem Slot vom 21.11. ist er faellig, falls der Nachtlauf nicht kam.
        Assert.True(TimerSchedule.IsPurchasingCatchUpDue(new DateTime(2026, 11, 21, 7, 0, 0), 3, 0, true,
            new DateTime(2026, 11, 20, 15, 10, 0)));
    }

    [Fact]
    public void IsPurchasingCatchUpDue_OhneJedenErfolg_True()
    {
        Assert.True(TimerSchedule.IsPurchasingCatchUpDue(new DateTime(2026, 11, 20, 12, 5, 0), 3, 0, true, null));
    }

    [Fact]
    public void IsPurchasingCatchUpDue_TimerAus_False()
    {
        Assert.False(TimerSchedule.IsPurchasingCatchUpDue(new DateTime(2026, 11, 20, 13, 0, 0), 3, 0, enabled: false, lastSuccessLocal: null));
    }
}
