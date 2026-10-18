namespace TrafagSalesExporter.Services;

/// <summary>
/// Textverarbeitung fuer Analyseskripte der Server-Analyse: Standort aus dem Dateinamen,
/// Aufteilen in Statements, Kommentare entfernen, Schemaplatzhalter ersetzen.
///
/// Bewusst frei von Datenbank- und Dateizugriff, damit das Verhalten ohne Server und ohne
/// Standortsystem testbar ist (siehe <c>ServerAnalysisScriptTests</c>).
/// </summary>
public static class ServerAnalysisScript
{
    /// <summary>Trennt Statements: eine Zeile, die mit ;; beginnt.</summary>
    public const string StatementSeparator = ";;";

    /// <summary>
    /// Standort (TSC) aus dem Dateinamen: alles vor dem ersten Unterstrich.
    /// <c>TRIN__01_salestype_discovery.sql</c> ergibt <c>TRIN</c>. Liefert null, wenn der Name
    /// dem Muster nicht folgt - dann wird die Datei uebersprungen statt gegen einen geratenen
    /// Standort ausgefuehrt.
    /// </summary>
    public static string? ResolveTsc(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        var baseName = Path.GetFileNameWithoutExtension(fileName.Trim());
        var separator = baseName.IndexOf('_');
        if (separator <= 0)
            return null;

        var tsc = baseName[..separator].Trim();
        return tsc.Length > 0 && tsc.All(char.IsLetterOrDigit) ? tsc.ToUpperInvariant() : null;
    }

    /// <summary>
    /// Zerlegt den Dateiinhalt in einzelne Statements. Rueckgabe je Statement: die Beschriftung
    /// (erste Kommentarzeile, fuer die Ergebnisdatei) und das um Kommentarzeilen bereinigte SQL.
    /// </summary>
    public static List<(string Label, string Sql)> SplitStatements(string? raw)
    {
        var result = new List<(string, string)>();
        if (string.IsNullOrWhiteSpace(raw))
            return result;

        var blocks = raw
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split("\n" + StatementSeparator, StringSplitOptions.None);

        foreach (var block in blocks)
        {
            var lines = block.Split('\n').Select(l => l.Trim()).ToList();
            var label = lines.FirstOrDefault(l => l.StartsWith("--", StringComparison.Ordinal))
                ?.TrimStart('-').Trim() ?? string.Empty;

            // Kommentarzeilen entfernen, damit der Guardrail nicht an '--' scheitert und
            // kein Kommentar ein Statement verdecken kann.
            var sql = string.Join(' ', lines
                .Where(l => l.Length > 0 && !l.StartsWith("--", StringComparison.Ordinal)))
                .Trim();

            if (sql.Length == 0)
                continue;

            result.Add((label.Length > 0 ? label : "ohne Beschriftung", sql));
        }

        return result;
    }

    /// <summary>
    /// Ersetzt die Schemaplatzhalter. <c>{schema}</c> bleibt in der Schreibweise der
    /// Konfiguration (HANA-Identifier sind case-sensitiv), <c>{SCHEMA}</c> wird gross
    /// geschrieben - fuer Systemsichten wie <c>SYS.TABLE_COLUMNS</c>.
    /// </summary>
    public static string ApplySchema(string sql, string schema)
    {
        var value = schema?.Trim() ?? string.Empty;
        return (sql ?? string.Empty)
            .Replace("{schema}", value, StringComparison.Ordinal)
            .Replace("{SCHEMA}", value.ToUpperInvariant(), StringComparison.Ordinal);
    }
}
