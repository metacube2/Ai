using System.Globalization;
using System.Text.RegularExpressions;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Liest aus dem Serverprotokoll (stdout-Log der App), ob gerade ein Einkauf-Lauf laeuft.
///
/// Anlass 2026-11-20: Der Deploy um 12:51 hat einen Einkauf-Delta abgebrochen, der seit 12:31
/// lief; die Pruefung vor dem Deploy sah nur die kleine <c>-wal</c>-Datei. Der Delta liest aber
/// zuerst lange aus SAP und schreibt erst am Ende, die WAL verraet ihn also nicht. Der Abbruch
/// fuehrte zum Nachhol-Lauf 13:39 bis 15:10 in der Arbeitszeit.
///
/// Die Datei hat keine Abhaengigkeit zur App, weil das Deploy-Werkzeug
/// (<c>Tools/DeployConsole</c>) sie als Quelldatei mit einbindet. Die Texte unten muessen zu den
/// Log-Zeilen in <see cref="PurchasingRefreshRunner"/> und <see cref="TimerBackgroundService"/> passen.
/// </summary>
public static class PurchasingRunLog
{
    /// <summary>Laeufer, die laenger als das ohne Ende-Zeile im Log stehen, gelten als tot (alter Code ohne Ende-Zeile).</summary>
    public static readonly TimeSpan DefaultMaxRunAge = TimeSpan.FromHours(3);

    private static readonly Regex StartPattern = new(
        @"Einkauf-Delta \(naechtlich\) gestartet|Einkauf-Delta Nachhol-Lauf gestartet|Einkauf-Lauf (Full|Delta) gestartet",
        RegexOptions.CultureInvariant);

    private static readonly Regex EndPattern = new(
        @"Einkauf-Lauf (Full|Delta) beendet|Einkauf-Lauf (Full|Delta) ist unerwartet abgebrochen|Fehler beim naechtlichen Einkauf-Delta|Application is shutting down",
        RegexOptions.CultureInvariant);

    private static readonly Regex TimePattern = new(@" um (?<t>[0-9./: ]+[0-9])", RegexOptions.CultureInvariant);

    private static readonly string[] TimeFormats =
    [
        "M/d/yyyy H:mm:ss", "M/d/yyyy h:mm:ss tt", "dd.MM.yyyy HH:mm:ss", "d.M.yyyy H:mm:ss"
    ];

    public sealed record State(bool Running, DateTime? StartedLocal, string? StartLine, string Reason);

    /// <summary>
    /// Wertet die Zeilen EINES Prozess-Logs aus (die neueste stdout-Datei). Massgeblich ist die
    /// letzte Start- und die letzte Ende-Zeile. Steht nach dem letzten Start kein Ende, laeuft ein
    /// Lauf, ausser der Start ist aelter als <paramref name="maxRunAge"/>. Eine Startzeit, die sich
    /// nicht lesen laesst, gilt vorsichtshalber als laufend.
    /// </summary>
    public static State Evaluate(IEnumerable<string> lines, DateTime nowLocal, TimeSpan maxRunAge)
    {
        var lastStartIndex = -1;
        string? lastStartLine = null;
        var lastEndIndex = -1;
        var index = 0;
        foreach (var raw in lines)
        {
            var line = raw.TrimEnd('\r');
            if (StartPattern.IsMatch(line) && !line.Contains("nicht gestartet", StringComparison.Ordinal))
            {
                lastStartIndex = index;
                lastStartLine = line.Trim();
            }
            else if (EndPattern.IsMatch(line))
            {
                lastEndIndex = index;
            }
            index++;
        }

        if (lastStartIndex < 0)
            return new State(false, null, null, "Kein Einkauf-Lauf im aktuellen Serverprotokoll gestartet.");
        if (lastEndIndex > lastStartIndex)
            return new State(false, ParseStart(lastStartLine!), lastStartLine, "Letzter Einkauf-Lauf ist beendet.");

        var started = ParseStart(lastStartLine!);
        if (started is null)
            return new State(true, null, lastStartLine, "Einkauf-Lauf gestartet, kein Ende im Protokoll, Startzeit unlesbar.");
        if (nowLocal - started.Value > maxRunAge)
            return new State(false, started, lastStartLine,
                $"Start {started:dd.MM. HH:mm} ohne Ende-Zeile, aelter als {maxRunAge.TotalHours:0} Stunden: gilt als beendet.");

        return new State(true, started, lastStartLine,
            $"Einkauf-Lauf seit {started:HH:mm} ({(nowLocal - started.Value).TotalMinutes:0} Minuten), noch kein Ende im Protokoll.");
    }

    private static DateTime? ParseStart(string line)
    {
        var match = TimePattern.Match(line);
        if (!match.Success)
            return null;
        var text = match.Groups["t"].Value.Trim();
        return DateTime.TryParseExact(text, TimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value)
            ? value
            : null;
    }
}
