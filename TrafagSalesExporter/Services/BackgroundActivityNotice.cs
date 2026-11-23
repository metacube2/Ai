namespace TrafagSalesExporter.Services;

/// <summary>
/// Text fuer den Hinweis oben im Dashboard, solange ein schwerer Hintergrundlauf die Datenbank
/// belastet (Wunsch Ingo 2026-11-20: „Achtung, ... laeuft, ist langsam“). Anlass war der
/// Einkauf-Nachhol-Lauf vom 20.11. 13:39 bis 15:10, waehrend dessen das Dashboard zaeh war, ohne
/// dass jemand wusste warum.
///
/// Erfasst sind die Laeufe, die die App selbst kennt: Einkauf-Lauf ueber den Runner (seit
/// 2026-11-20 auch der Nachtlauf) und der Verkaufsexport. Die Wochensicherung der Datenbank laeuft
/// im Deploy-Werkzeug ausserhalb der App und ist hier nicht sichtbar.
/// </summary>
public static class BackgroundActivityNotice
{
    public sealed record Notice(string German, string English);

    public static Notice? Describe(PurchasingRefreshRunInfo purchasing, bool exportRunning, DateTime nowUtc)
    {
        var parts = new List<(string De, string En)>();

        if (purchasing.IsRunning)
        {
            var since = purchasing.StartedAtUtc?.ToLocalTime().ToString("HH:mm") ?? "?";
            var minutes = purchasing.StartedAtUtc is { } start ? Math.Max(0, (int)(nowUtc - start).TotalMinutes) : 0;
            var mode = purchasing.Mode == PurchasingRefreshRunner.ModeFull ? "Full Load" : "Delta";
            parts.Add((
                $"Einkaufsdaten werden aus SAP aktualisiert ({mode}, seit {since}, {minutes} Min.)",
                $"Purchasing data is being refreshed from SAP ({mode}, since {since}, {minutes} min)"));
        }

        if (exportRunning)
            parts.Add(("Verkaufsexport laeuft", "Sales export running"));

        if (parts.Count == 0)
            return null;

        return new Notice(
            $"Achtung: {string.Join(" und ", parts.Select(p => p.De))}. Das Dashboard kann in dieser Zeit langsamer reagieren.",
            $"Note: {string.Join(" and ", parts.Select(p => p.En))}. The dashboard may respond more slowly meanwhile.");
    }
}
