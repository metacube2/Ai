namespace TrafagSalesExporter.Models;

/// <summary>
/// Datenverantwortung je Datentopf (Dataowner), 2026-11-03: Wunsch des Chefs nach der Abschlussrunde Dashboard,
/// Auftrag Ingo „Eingabeform fuer Dataowner, wo ich vorlaeufige User eintragen kann“. Wer ist fuer welche Daten
/// verantwortlich, mit Stellvertretung, Key User und System; Status vorlaeufig oder bestaetigt.
/// Doku: docs/SITZUNG_ABSCHLUSSRUNDE_DASHBOARD_2026-11-03.md Abschnitt 3 und 6.
/// </summary>
public class DataOwner
{
    public int Id { get; set; }
    public string DataDomain { get; set; } = string.Empty;
    public string SourceSystem { get; set; } = string.Empty;
    public string Responsible { get; set; } = string.Empty;
    public string Deputy { get; set; } = string.Empty;
    public string KeyUser { get; set; } = string.Empty;
    public string Status { get; set; } = DataOwnerStatus.Provisional;
    public string Note { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}

public static class DataOwnerStatus
{
    public const string Provisional = "Vorlaeufig";
    public const string Confirmed = "Bestaetigt";
}
