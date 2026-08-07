namespace TrafagSalesExporter.Services;

/// <summary>
/// Statuswerte des Soll/Ist-Vergleichs je Land. Erzeugt werden sie in
/// <c>ManagementCockpitService.BuildFinanceStatus</c> und
/// <c>FinanceReconciliationService.BuildReferenceStatus</c>; verglichen wurden sie bisher als
/// Zeichenketten-Literale in der Razor und in <c>Tools/FinanceProbe</c>.
///
/// Warum das eine eigene Klasse ist: es gibt VIER Stati, aber die Schnelluebersicht zaehlte nur
/// zwei davon (<see cref="Ok"/> und <see cref="Check"/>). Laender ohne Sollwert oder ohne Daten
/// fielen in keine der beiden Kacheln. Fehlen die Sollwerte fuer das gewaehlte Jahr komplett,
/// stehen beide Kacheln auf 0 - das liest sich wie „alles sauber", heisst aber „nichts geprueft".
/// Genau das ist am 2026-08-07 produktiv gemessen worden: <c>FinanceReferences</c> enthaelt
/// ausschliesslich Zeilen fuer 2025, das Standardjahr der Seite ist aber das juengste Jahr in den
/// Daten (2026).
///
/// Deshalb steht hier nicht nur der Text, sondern auch die Antwort auf die Frage, ob ein Status
/// ueberhaupt eine Pruefung darstellt (<see cref="IsChecked"/>). Ein neuer Status wird genau
/// einmal hier eingetragen und ist damit in allen Kacheln beruecksichtigt.
/// </summary>
public static class FinanceCountryStatuses
{
    /// <summary>Sollwert vorhanden, Differenz innerhalb der Toleranz.</summary>
    public const string Ok = "OK";

    /// <summary>Sollwert vorhanden, Differenz ausserhalb der Toleranz (oder nicht berechenbar).</summary>
    public const string Check = "Pruefen";

    /// <summary>Fuer dieses Land und Jahr ist in <c>FinanceReferences</c> kein Sollwert gepflegt.</summary>
    public const string NoReference = "Kein Sollwert";

    /// <summary>Sollwert vorhanden, aber keine einzige Ist-Zeile im Filter.</summary>
    public const string NoData = "Keine Daten";

    /// <summary>
    /// Stati, bei denen tatsaechlich gegen einen Sollwert geprueft wurde. Nur diese duerfen als
    /// „geprueft" gezaehlt werden; alles andere ist eine Luecke, keine Freigabe.
    /// </summary>
    private static readonly string[] Verified = { Ok, Check };

    /// <summary>Wurde dieses Land ueberhaupt gegen einen Sollwert geprueft?</summary>
    public static bool IsChecked(string? status)
        => status is not null && Verified.Contains(status, StringComparer.Ordinal);

    /// <summary>
    /// Gegenstueck zu <see cref="IsChecked"/>: Land ohne belastbare Pruefung, also
    /// <see cref="NoReference"/> oder <see cref="NoData"/>. Diese Laender stecken in KEINER der
    /// Kacheln „Laender OK" und „Zu pruefen" und brauchen deshalb eine eigene.
    /// </summary>
    public static bool IsUnverified(string? status) => !IsChecked(status);
}
