using Microsoft.Extensions.Options;

namespace TrafagSalesExporter.Services;

/// <summary>Leistung einer Person seit Tagesbeginn, nur nach Anmeldung sichtbar, nur im Speicher.</summary>
public sealed record LivePersonOutput(string User, int Confirmed, int Created, DateTime First, DateTime Last, double EstimatedHours)
{
    public double? PerHour => EstimatedHours > 0 ? Confirmed / EstimatedHours : null;
}

/// <summary>
/// Logistik live, Personen (HR-Freigabe laut Ingo 2026-10-05): Auswertung je TA-Benutzer. Ohne Anmeldung gehen nur
/// anonymisierte Positionen an die Oberfläche (<see cref="Anonymize"/>). Personenstunden sind eine Schätzung aus den
/// Quittierzeiten, keine Zeiterfassung. Doku docs/LOGISTIK_LIVE_2026-10-01.md.
/// </summary>
public static class LogisticsPeople
{
    /// <summary>Lücke, ab der zwischen zwei Quittierungen einer Person eine Pause angenommen wird.</summary>
    public static readonly TimeSpan BreakGap = TimeSpan.FromMinutes(15);

    /// <summary>Zeit, die für eine einzelne Quittierung ohne Nachbar mindestens gezählt wird.</summary>
    public static readonly TimeSpan MinimumSlot = TimeSpan.FromMinutes(2);

    /// <summary>Entfernt alle Benutzerfelder. Ohne Anmeldung muss jeder Darstellungsweg hierdurch.</summary>
    public static IReadOnlyList<LiveTransferItem> Anonymize(IEnumerable<LiveTransferItem> transfers)
        => transfers.Select(t => t.CreatedBy.Length == 0 && t.PickedBy.Length == 0 && t.ConfirmedBy.Length == 0
            ? t
            : t with { CreatedBy = "", PickedBy = "", ConfirmedBy = "" }).ToList();

    public static bool HasNames(IEnumerable<LiveTransferItem> transfers)
        => transfers.Any(t => t.ConfirmedBy.Length > 0 || t.CreatedBy.Length > 0 || t.PickedBy.Length > 0);

    /// <summary>Je Person: quittierte Positionen (Quittierer, sonst Entnahme), angelegte TA, geschätzte Stunden.</summary>
    public static IReadOnlyList<LivePersonOutput> Outputs(IEnumerable<LiveTransferItem> transfers)
    {
        var list = transfers.ToList();
        var created = list.Where(t => t.CreatedBy.Length > 0).GroupBy(t => t.CreatedBy).ToDictionary(g => g.Key, g => g.Select(t => t.Tanum).Distinct().Count());
        return list
            .Where(t => t.Confirmed && t.ConfirmedAt.HasValue)
            .Select(t => (User: t.ConfirmedBy.Length > 0 ? t.ConfirmedBy : t.PickedBy, At: t.ConfirmedAt!.Value))
            .Where(x => x.User.Length > 0)
            .GroupBy(x => x.User)
            .Select(g =>
            {
                var times = g.Select(x => x.At).Order().ToList();
                return new LivePersonOutput(g.Key, times.Count, created.GetValueOrDefault(g.Key), times[0], times[^1], EstimateHours(times));
            })
            .OrderByDescending(p => p.Confirmed)
            .ToList();
    }

    /// <summary>Summe der Arbeitsabschnitte: aufeinanderfolgende Quittierungen mit Lücke bis 15 Minuten gehören zusammen.</summary>
    public static double EstimateHours(IReadOnlyList<DateTime> sortedTimes)
    {
        if (sortedTimes.Count == 0) return 0;
        var total = TimeSpan.Zero;
        var start = sortedTimes[0];
        var last = sortedTimes[0];
        foreach (var t in sortedTimes.Skip(1))
        {
            if (t - last > BreakGap)
            {
                total += Max(last - start, MinimumSlot);
                start = t;
            }
            last = t;
        }
        total += Max(last - start, MinimumSlot);
        return total.TotalHours;
    }

    private static TimeSpan Max(TimeSpan a, TimeSpan b) => a > b ? a : b;
}

public sealed class LogisticsPeopleAccessOptions
{
    public const string SectionName = "LogisticsPeopleAccess";
    public string Username { get; set; } = "logistik";
    public string PasswordHash { get; set; } = string.Empty;
}

/// <summary>Freischaltung „Namen anzeigen“ je Browser-Verbindung (Circuit), Passwort als SHA-256-Hash wie HR KPI.</summary>
public sealed class LogisticsPeopleAccessService
{
    private readonly IOptionsMonitor<LogisticsPeopleAccessOptions> _options;

    public LogisticsPeopleAccessService(IOptionsMonitor<LogisticsPeopleAccessOptions> options) => _options = options;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.CurrentValue.PasswordHash);

    public bool IsUnlocked { get; private set; }

    public bool TryUnlock(string username, string password)
    {
        var o = _options.CurrentValue;
        IsUnlocked = IsConfigured
            && string.Equals((username ?? "").Trim(), o.Username.Trim(), StringComparison.OrdinalIgnoreCase)
            && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(AccessPasswordSettingsWriter.HashPassword(password ?? "")),
                System.Text.Encoding.UTF8.GetBytes(o.PasswordHash.Trim().ToUpperInvariant()));
        return IsUnlocked;
    }

    public void Lock() => IsUnlocked = false;
}
