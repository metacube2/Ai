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
    /// Mindestalter des letzten erfolgreichen Einkauf-Laufs, ab dem ein Nachhol-Delta startet.
    /// 20 statt 24 Stunden, damit ein Lauf, der gestern etwas spaeter fertig wurde, heute nach
    /// dem Slot trotzdem faellig ist; kurz genug, dass es hoechstens einen Versuch pro Tag gibt.
    /// </summary>
    public static readonly TimeSpan PurchasingCatchUpMinAge = TimeSpan.FromHours(20);

    /// <summary>
    /// True, wenn das Einkauf-Delta ausserhalb des planmaessigen Slots nachgeholt werden soll.
    ///
    /// BEFUND 2026-09-28: Vom 10.09. bis 28.09. lief KEIN einziger Einkauf-Lauf. Das Delta
    /// startete nur im 12:00-Slot und erst nach dem Verkaufsexport. Der IIS-Worker wird aber im
    /// Leerlauf beendet: an den meisten Tagen lebte er um 12:00 nicht (dann Nachhol-Lauf ohne
    /// Einkauf), am 18.09. und 25.09. starb er mitten im Export, bevor das Delta begann.
    ///
    /// Die urspruengliche Sorge bleibt gewahrt: kein SAP-Lauf bei JEDEM beliebigen Neustart.
    /// Nachgeholt wird nur nach dem heutigen Slot und nur, wenn der letzte ERFOLGREICHE Lauf
    /// mindestens <see cref="PurchasingCatchUpMinAge"/> zurueckliegt. Ein abgebrochener Lauf
    /// zaehlt bewusst nicht als erfolgreich, sonst bliebe der Ausfall wieder unsichtbar.
    /// </summary>
    /// <param name="now">Aktuelle lokale Zeit.</param>
    /// <param name="hour">Geplante Stunde.</param>
    /// <param name="minute">Geplante Minute.</param>
    /// <param name="enabled">Timer aktiviert.</param>
    /// <param name="lastSuccessLocal">Ende des letzten erfolgreichen Einkauf-Laufs, lokal, oder null.</param>
    public static bool IsPurchasingCatchUpDue(DateTime now, int hour, int minute, bool enabled, DateTime? lastSuccessLocal)
    {
        if (!enabled)
            return false;

        var todayRun = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0, now.Kind);
        if (now < todayRun)
            return false;

        return lastSuccessLocal is null || now - lastSuccessLocal.Value >= PurchasingCatchUpMinAge;
    }
}
