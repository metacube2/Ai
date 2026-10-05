namespace TrafagSalesExporter.Services;

// Reiter Weltlage (Wunsch Ingo 2026-10-05, „erster Wurf“). Externe, frei abrufbare Daten ohne Anmeldung, verknuepft
// mit unserem Exposure je Abteilung. Doku docs/WELTLAGE_2026-10-05.md.

/// <summary>Zustand einer externen Quelle; <see cref="Blocked"/> heisst: nicht erreichbar, vermutlich Firewall.</summary>
public enum WorldSourceState { Pending, Ok, Blocked, Error }

public sealed record WorldSourceStatus(string Key, string Name, string Host, WorldSourceState State, string Message, DateTime? FetchedAtUtc);

/// <summary>Ereignislage eines Landes an einem Tag (aus den GDELT-15-Minuten-Dateien aggregiert).</summary>
public sealed record WorldEventDay(DateOnly Day, string Country, int Events, int Conflict, int Protest, long Mentions, double ToneSum, double GoldsteinSum)
{
    public double ConflictShare => Events == 0 ? 0 : (double)Conflict / Events;
    public double AvgTone => Events == 0 ? 0 : ToneSum / Events;
    public double AvgGoldstein => Events == 0 ? 0 : GoldsteinSum / Events;
}

/// <summary>Ein einzelnes, stark beachtetes Ereignis mit Quelle (nur Link, keine Artikeltexte).</summary>
public sealed record WorldEvent(DateTime TimeUtc, string Country, string Place, int QuadClass, string RootCode, double Goldstein, double Tone, int Mentions, string Url);

/// <summary>Eine Zeitreihe (Rohstoff, Finanzstress, Industrieproduktion ...).</summary>
public sealed record WorldSeries(string Key, string Name, string Unit, IReadOnlyList<(DateOnly Date, double Value)> Points)
{
    public double? Last => Points.Count == 0 ? null : Points[^1].Value;

    /// <summary>Veraenderung in Prozent gegen den Wert vor etwa <paramref name="days"/> Tagen.</summary>
    public double? ChangePercent(int days)
    {
        if (Points.Count < 2) return null;
        var target = Points[^1].Date.AddDays(-days);
        var before = Points.LastOrDefault(p => p.Date <= target);
        return before.Date == default || before.Value == 0 ? null : (Points[^1].Value - before.Value) / Math.Abs(before.Value) * 100;
    }
}

/// <summary>BIP-Wachstum (IMF, real, in Prozent) je Land und Jahr.</summary>
public sealed record WorldGrowth(string Country, double? Year1, double? Year2, int FirstYear);

public sealed class WorldSnapshot
{
    public DateTime? UpdatedAtUtc { get; init; }
    public IReadOnlyList<WorldSourceStatus> Sources { get; init; } = [];
    public IReadOnlyList<WorldEventDay> EventDays { get; init; } = [];
    public IReadOnlyList<WorldEvent> TopEvents { get; init; } = [];
    public IReadOnlyList<WorldSeries> Series { get; init; } = [];
    public IReadOnlyList<WorldSeries> Industry { get; init; } = [];
    public IReadOnlyList<WorldGrowth> Growth { get; init; } = [];
}

/// <summary>Ein Punkt auf dem Radar: Wirkung eines Weltthemas auf eine Abteilung.</summary>
public sealed record WorldImpact(
    string Department, string Topic, string Subject, string Title, string Detail,
    decimal ExposureChf, double ExposureShare, double Signal, string Source)
{
    /// <summary>Wirkung in Punkten: Anteil unseres Geschaefts mal Signal (-1 bis +1), mal 100.</summary>
    public double Score => Math.Round(ExposureShare * Signal * 100, 2);
}
