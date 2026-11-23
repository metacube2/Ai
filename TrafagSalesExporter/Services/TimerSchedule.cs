namespace TrafagSalesExporter.Services;

/// <summary>
/// Reine, wall-clock-freie Zeitlogik fuer den Timer-Export. Ausgelagert aus
/// <see cref="TimerBackgroundService"/>, damit die Planung (naechster Lauf) und die
/// Nachhol-Entscheidung (verpasster Slot) ohne Hintergrunddienst/DI testbar sind.
/// </summary>
public static class TimerSchedule
{
    /// <summary>
    /// Naechster planmaessiger Laufzeitpunkt: heute um Stunde/Minute, sonst morgen,
    /// falls dieser Zeitpunkt heute bereits erreicht/ueberschritten ist.
    /// </summary>
    public static DateTime ComputeNextRun(DateTime now, int hour, int minute)
    {
        var todayRun = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0, now.Kind);
        return todayRun <= now ? todayRun.AddDays(1) : todayRun;
    }

    /// <summary>
    /// True, wenn ein Nachhol-Lauf faellig ist: Timer aktiv, der heutige Slot ist bereits
    /// erreicht/ueberschritten und heute fand noch kein Timer-Lauf statt. So wird ein Slot,
    /// zu dem der Prozess nicht lief (z.B. nach einem Deploy ohne Warm-up), beim naechsten
    /// Start einmalig nachgeholt statt bis zum Folgetag verloren zu gehen.
    /// </summary>
    /// <param name="now">Aktuelle lokale Zeit.</param>
    /// <param name="hour">Geplante Stunde.</param>
    /// <param name="minute">Geplante Minute.</param>
    /// <param name="enabled">Timer aktiviert.</param>
    /// <param name="lastRunLocal">Letzter Timer-Lauf in lokaler Zeit, oder null.</param>
    public static bool IsCatchUpDue(DateTime now, int hour, int minute, bool enabled, DateTime? lastRunLocal)
    {
        if (!enabled)
            return false;

        var todayRun = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0, now.Kind);
        if (now < todayRun)
            return false;

        // Heute noch kein Lauf -> nachholen. lastRunLocal.Date == heute bedeutet: schon gelaufen.
        return lastRunLocal is null || lastRunLocal.Value.Date < now.Date;
    }

    /// <summary>
    /// Slot des Einkauf-Deltas, wenn <c>Purchasing:DeltaHour</c>/<c>DeltaMinute</c> nichts anderes sagen.
    ///
    /// Seit 2026-11-20 nachts statt hinter dem 12:00-Export (Auftrag Ingo: ein durch Deploy
    /// abgebrochener Mittagslauf wurde um 13:39 nachgeholt und lief 90 Minuten in der Arbeitszeit).
    /// Seit 2026-11-23 um Mitternacht statt 03:00 (Wunsch Ingo, nachdem der Nachhol-Lauf nach dem Deploy
    /// am Montagmorgen ueber eine Stunde lief und die Arbeit blockierte).
    /// </summary>
    public const int PurchasingDeltaDefaultHour = 0;

    /// <summary>
    /// Nachgeholt wird nur in diesem Fenster nach dem Slot (Standard 00:00 bis 06:00). Danach wartet ein
    /// verpasster Lauf auf die naechste Nacht oder einen Klick in der Oberflaeche, statt in den Arbeitstag
    /// zu fallen (Fall 2026-11-23: Deploy 07:37, Nachhol-Delta ab 07:38 blockierte die Arbeit).
    /// </summary>
    public static readonly TimeSpan PurchasingCatchUpWindow = TimeSpan.FromHours(6);

    /// <summary>Minute zum <see cref="PurchasingDeltaDefaultHour"/>.</summary>
    public const int PurchasingDeltaDefaultMinute = 0;

    /// <summary>
    /// Art des Einkauf-Laufs im Nachtslot: am Tag <paramref name="weeklyFullLoadDay"/> ein Full Load, sonst ein Delta.
    ///
    /// Seit 2026-11-23: Das echte Delta laedt nur neue und offene Belege. Aenderungen an abgeschlossenen
    /// Bestellungen (Preis, Loeschkennzeichen, Storno eines Wareneingangs, neue Position) kaemen sonst nie
    /// an; bis zum 20.11. erledigte das unbeabsichtigt der Delta, der in Wahrheit alles neu lud.
    /// </summary>
    public static string PurchasingSlotMode(DateTime slotLocal, DayOfWeek? weeklyFullLoadDay)
        => weeklyFullLoadDay == slotLocal.DayOfWeek ? "Full" : "Delta";

    /// <summary>
    /// Juengster Slot, der schon erreicht ist: heute um Stunde/Minute, sonst gestern.
    /// </summary>
    public static DateTime MostRecentSlot(DateTime now, int hour, int minute)
    {
        var todayRun = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0, now.Kind);
        return todayRun <= now ? todayRun : todayRun.AddDays(-1);
    }

    /// <summary>
    /// True, wenn das Einkauf-Delta ausserhalb des planmaessigen Slots nachgeholt werden soll:
    /// seit dem juengsten Slot gab es keinen erfolgreichen Lauf.
    ///
    /// BEFUND 2026-09-28: Vom 10.09. bis 28.09. lief KEIN einziger Einkauf-Lauf, weil der
    /// IIS-Worker zur Slotzeit nicht lebte. BEFUND 2026-11-20: Der Worker endete am 19.11. um 21:20
    /// und startete erst am 20.11. um 06:21; ein Nachtslot kann also ausfallen und wird dann beim
    /// naechsten Start nachgeholt.
    ///
    /// Vorher galt „letzter Erfolg mindestens 20 Stunden alt“. Mit dem Nachtslot haette ein Lauf,
    /// der um 15:10 fertig wurde, einen verpassten Nachtslot erst am Folgetag um 11:10 nachgeholt,
    /// also wieder mitten im Tag. Ein abgebrochener Lauf zaehlt weiterhin nicht als erfolgreich.
    /// Dass ein gescheiterter Nachholversuch nicht alle 15 Minuten wiederholt wird, regelt der
    /// <see cref="TimerBackgroundService"/> (hoechstens ein Versuch je Slot und Prozess).
    /// </summary>
    /// <param name="now">Aktuelle lokale Zeit.</param>
    /// <param name="hour">Stunde des Delta-Slots.</param>
    /// <param name="minute">Minute des Delta-Slots.</param>
    /// <param name="enabled">Timer aktiviert.</param>
    /// <param name="lastSuccessLocal">Ende des letzten erfolgreichen Einkauf-Laufs, lokal, oder null.</param>
    public static bool IsPurchasingCatchUpDue(DateTime now, int hour, int minute, bool enabled, DateTime? lastSuccessLocal)
    {
        if (!enabled)
            return false;

        var slot = MostRecentSlot(now, hour, minute);
        if (now - slot >= PurchasingCatchUpWindow)
            return false;

        return lastSuccessLocal is null || lastSuccessLocal.Value < slot;
    }
}
