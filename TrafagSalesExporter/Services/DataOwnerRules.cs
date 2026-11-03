using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Regeln fuer die Seite Datenverantwortung (2026-11-03): Eingaben bereinigen und pruefen, Startbelegung aus der
/// Abschlussrunde vom 03.11. (alles vorlaeufig). Bewusst statisch und ohne DI-Registrierung, damit Program.cs
/// unberuehrt bleibt; die Seite arbeitet mit der DbContext-Factory.
/// </summary>
public static class DataOwnerRules
{
    public const int TextMax = 200;
    public const int NoteMax = 1000;

    /// <summary>Schneidet Leerraum ab, begrenzt die Laengen und setzt einen gueltigen Status.</summary>
    public static void Normalize(DataOwner entry)
    {
        entry.DataDomain = Limit(entry.DataDomain, TextMax);
        entry.SourceSystem = Limit(entry.SourceSystem, TextMax);
        entry.Responsible = Limit(entry.Responsible, TextMax);
        entry.Deputy = Limit(entry.Deputy, TextMax);
        entry.KeyUser = Limit(entry.KeyUser, TextMax);
        entry.Note = Limit(entry.Note, NoteMax);
        entry.Status = entry.Status == DataOwnerStatus.Confirmed ? DataOwnerStatus.Confirmed : DataOwnerStatus.Provisional;
    }

    /// <summary>Datentopf und Verantwortlich sind Pflicht; true, wenn der Eintrag gespeichert werden darf.</summary>
    public static bool IsValid(DataOwner entry)
        => !string.IsNullOrWhiteSpace(entry.DataDomain) && !string.IsNullOrWhiteSpace(entry.Responsible);

    /// <summary>
    /// Vorlaeufige Startbelegung aus der Abschlussrunde vom 03.11.2026. Wird nur einmal eingefuegt, wenn die Tabelle
    /// neu angelegt wird; danach pflegt Ingo die Eintraege selbst.
    /// </summary>
    public static IReadOnlyList<DataOwner> InitialEntries() =>
    [
        Entry(10, "Produktionsdaten", "SAP PP", "Dragan Radanovic", "", "", "In der Produktion keine bekannten Key User."),
        Entry(20, "Logistikdaten", "SAP WM", "Neva Acikalin", "", "", "Separat von der Produktion (Ingo, 03.11.)."),
        Entry(30, "Materialstamm", "SAP MM", "Armin Güntensperger", "", "", "Ingo, 03.11.; in der Sitzung zuerst bei Fabio (Team Stefan) genannt."),
        Entry(40, "Debitorenstamm", "SAP SD", "Fabio Palma", "", "", "Verkaufsadministration."),
        Entry(50, "Lieferanten- und Einkaufsdaten", "SAP MM", "Marco Widmer", "", "Marco Widmer", "SAP-Key-User MM."),
        Entry(60, "HR-Daten", "Rexx, SAP HR", "Nadja Brandenberger", "Sonja Richter", "", ""),
        Entry(70, "Finance-Daten", "SAP FI/CO, Tochtergesellschaften", "Andreas Stoller", "", "", "In der Sitzung nicht besprochen, zu bestaetigen."),
    ];

    private static DataOwner Entry(int sort, string domain, string system, string responsible, string deputy, string keyUser, string note) => new()
    {
        SortOrder = sort,
        DataDomain = domain,
        SourceSystem = system,
        Responsible = responsible,
        Deputy = deputy,
        KeyUser = keyUser,
        Note = note,
        Status = DataOwnerStatus.Provisional,
        UpdatedAtUtc = new DateTime(2026, 11, 3, 0, 0, 0, DateTimeKind.Utc),
        UpdatedBy = "Abschlussrunde 03.11.2026",
    };

    private static string Limit(string? value, int max)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return trimmed.Length > max ? trimmed[..max] : trimmed;
    }
}
