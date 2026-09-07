using System.Globalization;
using System.Text;

namespace DeployConsole;

/// <summary>
/// Turns a finished run into the paragraph that goes into docs/rag/DEPLOYMENT.md,
/// in the shape that file already uses. Everything in it comes from a measurement -
/// what was not measured is named as not measured rather than left out.
/// </summary>
public static class ProtocolWriter
{
    // The apostrophe grouping used throughout docs/rag/DEPLOYMENT.md ("4'320'768 Bytes").
    // Pinned explicitly - ICU renders de-CH with U+2019, which would not match the file.
    private static readonly NumberFormatInfo Swiss = new()
    {
        NumberGroupSeparator = "'",
        NumberDecimalSeparator = ".",
    };

    public static string Build(DeployRequest request, DeployReport report)
    {
        var sb = new StringBuilder();
        var title = string.IsNullOrWhiteSpace(request.Title) ? "ohne Titel" : request.Title;

        sb.Append($"- Letzter produktiv verifizierter Deploy: **{report.StartedAt:yyyy-MM-dd HH:mm}, {title}**");
        if (!string.IsNullOrWhiteSpace(report.Commit))
        {
            sb.Append($", Funktionscommit `{report.Commit}`");
        }
        sb.AppendLine(request.TestsGreen && !string.IsNullOrWhiteSpace(request.TestCount)
            ? $", `{request.TestCount}` Tests gruen (Release-Lauf vor dem Publish)."
            : ", Testlauf NICHT bestaetigt.");

        if (report.WorkingTreeDirty)
        {
            sb.AppendLine($"  Hinweis: das Arbeitsverzeichnis war beim Deploy nicht sauber (Branch `{report.Branch}`).");
        }

        sb.AppendLine($"  `BiDashboard.dll` `{report.ServerDllWritten:dd.MM.yyyy HH:mm:ss}`, "
                      + $"`{Number(report.ServerDllBytes)}` Bytes, SHA256 `{report.ServerDllSha}`;");
        if (report.LocalDllSha is not null)
        {
            sb.AppendLine(report.LocalDllSha == report.ServerDllSha
                ? "  lokaler Release-Build und Server bitgleich."
                : report.PublishRan
                    ? "  **ACHTUNG: die ausgelieferte DLL ist nicht die Kopie des lokalen Release-Builds** - der Publish "
                      + "hat sie uebersprungen (PreserveNewest bei neuerer Zieldatei), es laeuft weiter die alte Version."
                    : "  Prueflauf: Server-DLL weicht vom lokalen Build ab (ohne Publish erwartbar).");
        }

        sb.AppendLine(report.PublishRan
            ? "  `app_offline.htm` gesetzt und danach auf `app_offline.htm.disabled` umbenannt."
            : "  PRUEFLAUF: kein Publish ausgefuehrt, `app_offline.htm` nicht gesetzt.");

        if (report.Routes.Count > 0)
        {
            var ok = report.Routes.Where(r => r.Status == "200").ToList();
            if (ok.Count > 0)
            {
                sb.AppendLine("  HTTPS `200`: " + string.Join(", ", ok.Select(r =>
                    $"{RouteLabel(r.Route)} (`{Number(r.Bytes)}` Bytes, `{r.Seconds.ToString("0.00", Swiss)} s`)")) + ".");
            }
            foreach (var bad in report.Routes.Where(r => r.Status != "200"))
            {
                sb.AppendLine($"  NICHT bestaetigt: {RouteLabel(bad.Route)} -> {bad.Error ?? bad.Status}.");
            }
        }
        else
        {
            sb.AppendLine("  Abrufpruefung nicht ausgefuehrt - Erreichbarkeit ist damit NICHT belegt.");
        }

        if (report.DatabaseBefore is null || report.DatabaseAfter is null)
        {
            sb.AppendLine($"  Produktiv-DB `{report.DatabaseFileName}` im Ziel NICHT gefunden - ueber ihren Zustand "
                          + "sagt dieses Protokoll nichts aus.");
        }
        else
        {
            var same = report.DatabaseBefore.Length == report.DatabaseAfter.Length
                       && report.DatabaseBefore.LastWriteUtc == report.DatabaseAfter.LastWriteUtc;
            sb.AppendLine(same
                ? $"  Produktiv-DB `{report.DatabaseFileName}` in Laenge und Schreibzeit unveraendert "
                  + $"(`{Number(report.DatabaseAfter.Length)}` Bytes, "
                  + $"`{report.DatabaseAfter.LastWriteUtc.ToLocalTime():dd.MM.yyyy HH:mm:ss}`)."
                : $"  ACHTUNG Produktiv-DB `{report.DatabaseFileName}` veraendert: vorher `{Number(report.DatabaseBefore.Length)}` Bytes / "
                  + $"`{report.DatabaseBefore.LastWriteUtc.ToLocalTime():dd.MM.yyyy HH:mm:ss}`, nachher "
                  + $"`{Number(report.DatabaseAfter.Length)}` Bytes / "
                  + $"`{report.DatabaseAfter.LastWriteUtc.ToLocalTime():dd.MM.yyyy HH:mm:ss}`.");
        }

        if (report.Diff is not null)
        {
            sb.AppendLine($"  Ziel: {report.Diff.AddedCount} Dateien neu, {report.Diff.ChangedCount} geaendert, "
                          + $"{report.Diff.UnchangedCount} unveraendert, {report.Diff.Vanished.Count} verschwunden.");
        }

        var found = report.Expected.Where(h => h.Found).Select(h => $"`{h.Needle}`").ToList();
        if (found.Count > 0)
        {
            sb.AppendLine("  Wirknachweis in der DLL: " + string.Join(", ", found) + ".");
        }
        var stillGone = report.Forbidden.Where(h => !h.Found).Select(h => $"`{h.Needle}`").ToList();
        if (stillGone.Count > 0)
        {
            sb.AppendLine("  Nicht mehr enthalten: " + string.Join(", ", stillGone) + ".");
        }

        if (report.Alarms.Count > 0)
        {
            sb.AppendLine("  **OFFENE ALARME:**");
            foreach (var alarm in report.Alarms)
            {
                sb.AppendLine($"  - {alarm}");
            }
        }

        sb.AppendLine("  Nicht durch dieses Werkzeug belegt: dass die geaenderten Seiten fuer einen angemeldeten "
                      + "Benutzer korrekt rendern (Routen hinter dem Finance-Unlock liefern das Passwortpanel), "
                      + "und der Testlauf selbst (Angabe oben ist manuell bestaetigt).");
        return sb.ToString();
    }

    private static string RouteLabel(string route) => string.IsNullOrWhiteSpace(route) ? "Startseite" : $"`/{route.TrimStart('/')}`";

    private static string Number(long value) => value.ToString("#,##0", Swiss);
}
