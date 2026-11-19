namespace TrafagSalesExporter.Services.Shopfloor;

/// <summary>
/// Arbeitskalender Werk 1100 (Entscheid Fabio 17.11.2026: echter Trafag-Kalender mit Feiertagen und Betriebsschliessungen statt
/// „nur 01./02.01.“). Quelle: SAP-Fabrikkalender <c>CH</c> von Werk 1100, in P76 gelesen am 2026-11-17 (<c>T001W-FABKL</c>,
/// <c>TFACD</c>, <c>TFACS</c>, Sonderregel <c>TFAIN</c> 28. bis 30.12.2026), docs/SHOPFLOOR_2026-10-07.md 26.4. Samstag und Sonntag
/// sind nie Arbeitstag. Fuer Jahre ohne Tabelle (ab 2028) gilt Mo bis Fr ohne 01./02.01.; dann die Tabelle aus P76 nachtragen.
/// </summary>
public static class ShopfloorKalender
{
    /// <summary>Mo bis Fr ohne Arbeitstag laut Fabrikkalender CH (P76), je Jahr vollstaendig.</summary>
    private static readonly HashSet<DateOnly> Frei =
    [
        new(2026, 1, 1), new(2026, 1, 2), new(2026, 4, 3), new(2026, 4, 6), new(2026, 5, 1), new(2026, 5, 14), new(2026, 5, 25),
        new(2026, 12, 24), new(2026, 12, 25), new(2026, 12, 28), new(2026, 12, 29), new(2026, 12, 30), new(2026, 12, 31),
        new(2027, 1, 1), new(2027, 3, 26), new(2027, 3, 29), new(2027, 5, 6), new(2027, 5, 17), new(2027, 12, 24), new(2027, 12, 31)
    ];

    /// <summary>Jahre, fuer die <see cref="Frei"/> vollstaendig ist.</summary>
    public static readonly int[] Jahre = [2026, 2027];

    public static bool IstArbeitstag(DateOnly d)
    {
        if (d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return false;
        if (Jahre.Contains(d.Year))
            return !Frei.Contains(d);
        return !(d.Month == 1 && d.Day <= 2);
    }
}
