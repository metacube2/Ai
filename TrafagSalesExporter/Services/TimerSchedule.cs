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
}
