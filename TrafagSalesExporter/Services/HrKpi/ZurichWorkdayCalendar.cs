namespace TrafagSalesExporter.Services;

/// <summary>
/// Arbeitskalender fuer den Standort im Kanton Zuerich. Beruecksichtigt die neun
/// gesetzlichen, den Sonntagen gleichgestellten Feiertage. Lokale oder nur vertraglich
/// freie Tage (z.B. Berchtoldstag) sind bewusst nicht enthalten.
/// </summary>
public static class ZurichWorkdayCalendar
{
    public static int CountWorkdays(DateTime start, DateTime end)
    {
        if (end < start)
            (start, end) = (end, start);

        var holidaysByYear = Enumerable.Range(start.Year, end.Year - start.Year + 1)
            .ToDictionary(year => year, GetPublicHolidays);
        var days = 0;
        for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
        {
            if (date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday &&
                !holidaysByYear[date.Year].Contains(date))
            {
                days++;
            }
        }

        return days;
    }

    public static IReadOnlySet<DateTime> GetPublicHolidays(int year)
    {
        var easterSunday = CalculateGregorianEasterSunday(year);
        return new HashSet<DateTime>
        {
            new(year, 1, 1),          // Neujahr
            easterSunday.AddDays(-2), // Karfreitag
            easterSunday.AddDays(1),  // Ostermontag
            new(year, 5, 1),          // Tag der Arbeit
            easterSunday.AddDays(39), // Auffahrt
            easterSunday.AddDays(50), // Pfingstmontag
            new(year, 8, 1),          // Bundesfeiertag
            new(year, 12, 25),        // Weihnachtstag
            new(year, 12, 26)         // Stephanstag
        };
    }

    // Gregorianischer Ostertermin nach Meeus/Jones/Butcher.
    private static DateTime CalculateGregorianEasterSunday(int year)
    {
        var a = year % 19;
        var b = year / 100;
        var c = year % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = (19 * a + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + 2 * e + 2 * i - h - k) % 7;
        var m = (a + 11 * h + 22 * l) / 451;
        var month = (h + l - 7 * m + 114) / 31;
        var day = ((h + l - 7 * m + 114) % 31) + 1;
        return new DateTime(year, month, day);
    }
}
