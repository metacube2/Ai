namespace TrafagSalesExporter.Services;

/// <summary>
/// Taeglicher Schnappschuss der AD-Computerkennzahlen fuer den Unterreiter Verlauf (2026-10-02):
/// einmal je Tag ab 06:00 aus der Netzwerkpruefung aufgerufen. Speichert Kennzahlen, die Aenderungen
/// gegenueber dem letzten Stand (neu, entfernt, verschoben, deaktiviert, Betriebssystem) und den neuen Stand.
/// Nur Computer, keine Personen. Doku docs/NETZWERK_2026-10-02.md.
/// </summary>
public sealed class AdSnapshotService
{
    private readonly AdComputerService _ad;
    private readonly NetworkStore _store;
    private readonly ILogger<AdSnapshotService> _logger;
    private DateOnly _done;

    public AdSnapshotService(AdComputerService ad, NetworkStore store, ILogger<AdSnapshotService> logger)
    {
        _ad = ad;
        _store = store;
        _logger = logger;
    }

    public async Task TakeIfDueAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (!_ad.IsEnabled || _done == today || DateTime.Now.Hour < 6)
            return;
        if (await _store.HasAdMetricsAsync(today, ct))
        {
            _done = today;
            return;
        }
        var ad = await _ad.GetAsync();
        if (ad.Error is not null || ad.Computers.Count == 0)
            return;

        var now = DateTime.UtcNow;
        var states = ad.Computers
            .Select(c => new AdComputerState(c.Name, c.Container, c.Enabled, AdAnalysis.Lifecycle(c.OperatingSystem, c.OsVersion).Product))
            .ToList();
        var previous = await _store.LoadAdComputerStatesAsync(ct);
        var changes = previous.Count == 0 ? [] : AdInfraAnalysis.Diff(previous, states, today);
        await _store.SaveAdSnapshotAsync(today, AdInfraAnalysis.Metrics(ad, now), changes, states, ct);
        _done = today;
        _logger.LogInformation("AD-Schnappschuss {Day}: {Computers} Computer, {Changes} Aenderungen", today, states.Count, changes.Count);
    }
}
