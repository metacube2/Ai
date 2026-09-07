namespace TrafagSalesExporter.Services;

/// <summary>
/// Guardrail fuer ad-hoc Analyseabfragen: erlaubt ausschliesslich lesende Statements.
///
/// Hintergrund: Die Server-Analyse (<see cref="ServerAnalysisBackgroundService"/>) fuehrt
/// SQL aus Dateien gegen die Quellsysteme der Standorte aus. Diese Systeme sind fremde
/// Produktivsysteme (SAP Business One der Landesgesellschaften) - dort darf unter keinen
/// Umstaenden geschrieben werden. Die Pruefung ist absichtlich streng und arbeitet mit einer
/// Positivliste: was nicht zweifelsfrei als Lesestatement erkannt wird, wird abgelehnt.
/// </summary>
public static class ReadOnlySqlGuard
{
    private static readonly string[] AllowedStarts = { "SELECT", "WITH" };

    /// <summary>
    /// Statementtrenner und Kommentarzeichen, die ein zweites Statement einschmuggeln koennten.
    /// Ein Semikolon ist erlaubt, solange es das Statement nur abschliesst (siehe Pruefung).
    /// </summary>
    private static readonly string[] ForbiddenSequences =
    {
        "--", "/*", "*/"
    };

    public static bool IsAllowed(string? sql) => Validate(sql) is null;

    /// <summary>
    /// Liefert null, wenn das Statement zugelassen ist, sonst den Ablehnungsgrund im Klartext
    /// (wird in die Ergebnisdatei geschrieben, damit die Ablehnung nachvollziehbar ist).
    /// </summary>
    public static string? Validate(string? sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return "leeres Statement";

        var value = sql.Trim();

        if (!AllowedStarts.Any(s => value.StartsWith(s, StringComparison.OrdinalIgnoreCase)))
            return "beginnt nicht mit SELECT oder WITH";

        foreach (var forbidden in ForbiddenSequences)
        {
            if (value.Contains(forbidden, StringComparison.Ordinal))
                return $"enthaelt '{forbidden}' - Kommentare werden vor der Pruefung entfernt";
        }

        // Ein abschliessendes Semikolon ist unschaedlich. Ein Semikolon MITTEN im Statement
        // koennte ein zweites, schreibendes Statement anhaengen und wird deshalb abgelehnt.
        var withoutTrailing = value.TrimEnd(';', ' ', '\t', '\r', '\n');
        if (withoutTrailing.Contains(';', StringComparison.Ordinal))
            return "enthaelt ein Semikolon innerhalb des Statements";

        return null;
    }
}
