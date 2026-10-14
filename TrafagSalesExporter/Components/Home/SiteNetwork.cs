using System.Globalization;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Components.Home;

/// <summary>Zustand eines Landesknotens in der Startseitengrafik.</summary>
public enum SiteNodeState { Ok, Stale, Error, Unknown }

/// <summary>Ein Land in der Startseitengrafik (ein Standort kann mehrere Laender abdecken, z. B. Schweiz/Oesterreich).</summary>
public sealed record SiteNode(string Code, string Name, string Source, SiteNodeState State, DateTime? LastAt, int Rows, string Tsc);

/// <summary>
/// Startseite (Wunsch Ingo 2026-10-14): Standorte mit Live-Zustand aus dem Exportprotokoll statt des festen Bildes.
/// Reine Logik, ohne Datenbank, damit sie testbar ist. Anzeige in SiteNetworkGraphic.razor.
/// </summary>
public static class SiteNetwork
{
    /// <summary>Automatische Quellen gelten bis 36 Stunden als aktuell (taeglicher Lauf plus Spielraum).</summary>
    public static readonly TimeSpan FreshAuto = TimeSpan.FromHours(36);

    /// <summary>Manuelle Excel-Importe kommen monatlich; bis 40 Tage gelten sie als aktuell.</summary>
    public static readonly TimeSpan FreshManual = TimeSpan.FromDays(40);

    private const string ManualSource = "MANUAL_EXCEL";

    /// <summary>Feste Reihenfolge im Kreis (Uhrzeigersinn ab oben links), damit Laender ihren Platz behalten.</summary>
    public static readonly string[] Order = ["CH", "DE", "FR", "UK", "US", "IN", "ES", "IT", "AT"];

    private static readonly (string Needle, string Code, string Name)[] Countries =
    [
        ("schweiz", "CH", "Schweiz"), ("oesterreich", "AT", "Österreich"), ("österreich", "AT", "Österreich"),
        ("deutschland", "DE", "Deutschland"), ("frankreich", "FR", "Frankreich"), ("italien", "IT", "Italien"),
        ("spanien", "ES", "Spanien"), ("england", "UK", "Grossbritannien"), ("grossbritannien", "UK", "Grossbritannien"),
        ("usa", "US", "USA"), ("indien", "IN", "Indien"), ("tschechien", "CZ", "Tschechien"), ("china", "CN", "China")
    ];

    /// <summary>Laender eines Standorts aus dem Feld Land; leer fuer technische Eintraege wie „Einkauf SAP“.</summary>
    public static IReadOnlyList<(string Code, string Name)> CountriesOf(string land)
    {
        var l = (land ?? string.Empty).ToLowerInvariant();
        var result = new List<(string, string)>();
        foreach (var (needle, code, name) in Countries)
            if (l.Contains(needle, StringComparison.Ordinal) && result.All(r => r.Item1 != code))
                result.Add((code, name));
        return result;
    }

    public static string SourceLabel(string source) => (source ?? string.Empty).ToUpperInvariant() switch
    {
        "SAP" => "SAP",
        "BI1" => "SAP Business One",
        "SAGE" => "Sage",
        ManualSource => "Excel-Import",
        var s => s
    };

    /// <summary>Zustand je Land aus den aktiven Standorten und dem jeweils letzten Protokolleintrag.</summary>
    public static List<SiteNode> Build(IEnumerable<Site> sites, IReadOnlyDictionary<int, ExportLog> latestBySite, DateTime nowUtc)
    {
        var nodes = new Dictionary<string, SiteNode>(StringComparer.Ordinal);
        foreach (var site in sites.Where(s => s.IsActive))
        {
            latestBySite.TryGetValue(site.Id, out var log);
            var state = StateOf(site.SourceSystem, log, nowUtc);
            foreach (var (code, name) in CountriesOf(site.Land))
            {
                var node = new SiteNode(code, name, SourceLabel(site.SourceSystem), state, log?.Timestamp, log?.RowCount ?? 0, site.TSC);
                // Mehrere Standorte im selben Land: der schlechtere Zustand gewinnt
                if (!nodes.TryGetValue(code, out var existing) || Rank(state) > Rank(existing.State))
                    nodes[code] = node;
            }
        }
        return nodes.Values
            .OrderBy(n => Array.IndexOf(Order, n.Code) is var i && i >= 0 ? i : 100)
            .ThenBy(n => n.Code, StringComparer.Ordinal)
            .ToList();
    }

    internal static SiteNodeState StateOf(string source, ExportLog? log, DateTime nowUtc)
    {
        if (log is null) return SiteNodeState.Unknown;
        if (string.Equals(log.Status, "Error", StringComparison.OrdinalIgnoreCase)) return SiteNodeState.Error;
        if (!string.Equals(log.Status, "OK", StringComparison.OrdinalIgnoreCase)) return SiteNodeState.Stale;
        var fresh = string.Equals(source, ManualSource, StringComparison.OrdinalIgnoreCase) ? FreshManual : FreshAuto;
        // Zeitstempel im Protokoll sind lokale Serverzeit
        var age = nowUtc - DateTime.SpecifyKind(log.Timestamp, DateTimeKind.Local).ToUniversalTime();
        return age <= fresh ? SiteNodeState.Ok : SiteNodeState.Stale;
    }

    private static int Rank(SiteNodeState s) => s switch
    {
        SiteNodeState.Ok => 0,
        SiteNodeState.Unknown => 1,
        SiteNodeState.Stale => 2,
        _ => 3
    };

    // ------------------------------------------------------------------ Geometrie (viewBox 1000 x 560)

    /// <summary>Startzeiten der drei Datenpakete je aktueller Leitung (hier statt in .razor wegen des Uebersetzungstests).</summary>
    // Negativer Versatz: die Pakete sind sofort unterwegs, statt bis zu ihrem Start bei (0, 0) zu stehen
    public static readonly string[] PacketBegins = ["0s", "-0.8s", "-1.6s"];

    public const double Cx = 500, Cy = 280, Rx = 395, Ry = 212, HubR = 112, NodeR = 36;

    /// <summary>Position des Knotens i von n auf der Ellipse, erster Knoten oben links.</summary>
    public static (double X, double Y) Position(int i, int n)
    {
        var a = (-90 - 180.0 / Math.Max(n, 1) + 360.0 * i / Math.Max(n, 1)) * Math.PI / 180;
        return (Cx + Rx * Math.Cos(a), Cy + Ry * Math.Sin(a));
    }

    /// <summary>Leicht gebogene Leitung vom Knotenrand zum Rand der Mitte, Richtung Mitte (fuer animateMotion).</summary>
    public static string LinePath(double x, double y)
    {
        var dx = Cx - x;
        var dy = Cy - y;
        var len = Math.Sqrt(dx * dx + dy * dy);
        var ux = dx / len;
        var uy = dy / len;
        var sx = x + ux * (NodeR + 6);
        var sy = y + uy * (NodeR + 6);
        var ex = Cx - ux * (HubR + 14);
        var ey = Cy - uy * (HubR + 14);
        // Kontrollpunkt auf Hoehe des Knotens: die Leitung laeuft erst waagrecht und biegt dann zur Mitte, wie im Vorbild
        var mx = (sx + ex) / 2;
        return FormattableString.Invariant($"M{sx:0.#},{sy:0.#} Q{mx:0.#},{sy:0.#} {ex:0.#},{ey:0.#}");
    }

    public static string F(double v) => v.ToString("0.#", CultureInfo.InvariantCulture);

    /// <summary>
    /// Kleine Flagge als SVG (Windows zeigt Flaggen-Emojis nur als Buchstaben). Vereinfachte Formen, 24 x 16 bei (x, y).
    /// </summary>
    public static string Flag(string code, double x, double y)
    {
        const double w = 24, h = 16;
        string R(double rx, double ry, double rw, double rh, string fill)
            => $"<rect x=\"{F(x + rx)}\" y=\"{F(y + ry)}\" width=\"{F(rw)}\" height=\"{F(rh)}\" fill=\"{fill}\"/>";
        string Vert(params string[] c) => string.Concat(c.Select((f, i) => R(i * w / c.Length, 0, w / c.Length + 0.2, h, f)));
        string Hor(params string[] c) => string.Concat(c.Select((f, i) => R(0, i * h / c.Length, w, h / c.Length + 0.2, f)));
        var body = code switch
        {
            "CH" => R(0, 0, w, h, "#d52b1e") + R(10, 3, 4, 10, "#fff") + R(7, 6, 10, 4, "#fff"),
            "AT" => Hor("#ed2939", "#fff", "#ed2939"),
            "DE" => Hor("#000", "#dd0000", "#ffce00"),
            "FR" => Vert("#0055a4", "#fff", "#ef4135"),
            "IT" => Vert("#009246", "#fff", "#ce2b37"),
            "ES" => R(0, 0, w, h, "#aa151b") + R(0, 4, w, 8, "#f1bf00"),
            "IN" => Hor("#ff9933", "#fff", "#138808") + $"<circle cx=\"{F(x + 12)}\" cy=\"{F(y + 8)}\" r=\"2\" fill=\"none\" stroke=\"#000080\" stroke-width=\"0.8\"/>",
            "UK" => R(0, 0, w, h, "#012169")
                    + $"<path d=\"M{F(x)},{F(y)} L{F(x + w)},{F(y + h)} M{F(x + w)},{F(y)} L{F(x)},{F(y + h)}\" stroke=\"#fff\" stroke-width=\"3\"/>"
                    + $"<path d=\"M{F(x)},{F(y)} L{F(x + w)},{F(y + h)} M{F(x + w)},{F(y)} L{F(x)},{F(y + h)}\" stroke=\"#c8102e\" stroke-width=\"1\"/>"
                    + R(9.5, 0, 5, h, "#fff") + R(0, 5.5, w, 5, "#fff") + R(10.5, 0, 3, h, "#c8102e") + R(0, 6.5, w, 3, "#c8102e"),
            "US" => Hor("#b22234", "#fff", "#b22234", "#fff", "#b22234", "#fff", "#b22234") + R(0, 0, 10, 8.6, "#3c3b6e"),
            "CZ" => R(0, 0, w, 8, "#fff") + R(0, 8, w, 8, "#d7141a") + $"<path d=\"M{F(x)},{F(y)} L{F(x + 12)},{F(y + 8)} L{F(x)},{F(y + 16)} Z\" fill=\"#11457e\"/>",
            _ => R(0, 0, w, h, "#555")
        };
        return body + $"<rect x=\"{F(x)}\" y=\"{F(y)}\" width=\"{F(w)}\" height=\"{F(h)}\" fill=\"none\" stroke=\"rgba(255,255,255,.35)\" stroke-width=\"0.6\" rx=\"1\"/>";
    }
}
