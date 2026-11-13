using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace TrafagSalesExporter.Services.Shopfloor;

/// <summary>
/// Operations (Shopfloor), 2026-11-13, P76-Befund zum Paket Fabio 13.11. (saptasks/shopfloor_sap/P76_BEFUND_2026-11-13.md): Die Zahlen
/// im Excel der Teamleiter entstehen mit den Filtern der globalen Standardlayouts der SAP-Reports.
/// <list type="bullet">
/// <item>ZC22 (ZM_OFFENE_FAUF), Layout „/ P00 TX“: Systemstatus <c>E CP 'FREI DRUC RÜCK VOKL*'</c> (alle Vorgaenge rueckgemeldet,
/// Wareneingang fehlt) und Arbeitsplatz des naechsten Vorgangs <c>I NP 'M*'</c> (nur mit „Mit Vorgangsdaten“). Gilt fuer Auftragsvorrat
/// und PPD-Verzug (ShopAufSet Typ VR/VZ; Felder Sttxt und Arbpl, Arbpl nur mit Filter <c>Avo eq 'X'</c>).</item>
/// <item>ZC27 (ZPP_RM_MENGEN), Layout „/NIN“: Material <c>E CP</c> E*, D*, F*, C* (ShopRmSet, Filter <c>Matex</c>).</item>
/// </list>
/// Alle drei sind im Cockpit schaltbar (Shopfloor:Zc22StatusAusschluss, Zc22ArbplAusschluss, RmMaterialAusschluss); leer = aus, dann
/// rechnet das Cockpit wie ZC22/ZC27 ohne Filter. Muster wie ABAP CP: * beliebig viele Zeichen, + genau ein Zeichen, Gross- und
/// Kleinschreibung egal. Entscheide Ingo 13.11.: ZC27-Material und ZC22-Status nachbilden, ZC22-Arbeitsplatz bauen, Schalter aus.
/// </summary>
public sealed record ShopfloorLayoutFilter(IReadOnlyList<string> StatusAusschluss, IReadOnlyList<string> ArbplAusschluss)
{
    public static readonly ShopfloorLayoutFilter Aus = new([], []);

    public bool Aktiv => StatusAusschluss.Count > 0 || ArbplAusschluss.Count > 0;

    /// <summary>Arbeitsplatz anfordern (teuer im SAP: naechster Vorgang je Auftrag), nur wenn der Arbeitsplatzteil aktiv ist.</summary>
    public bool Vorgangsdaten => ArbplAusschluss.Count > 0;

    /// <summary>Kommagetrennte Musterliste (Leerzeichen gehoeren zum Muster, z. B. „FREI DRUC RÜCK VOKL*“).</summary>
    public static string[] Muster(string? text)
        => (text ?? string.Empty).Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>ABAP-CP-Vergleich: * = beliebig, + = ein Zeichen, ohne Gross-/Kleinschreibung.</summary>
    public static bool Passt(string wert, string muster)
    {
        var regex = "^" + string.Concat(muster.Select(c => c switch
        {
            '*' => ".*",
            '+' => ".",
            _ => Regex.Escape(c.ToString())
        })) + "$";
        return Regex.IsMatch(wert, regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline);
    }

    /// <summary>
    /// Zeile wie das Layout ausblenden? Status: ausgeblendet, wenn die Statuszeile auf ein Muster passt (E CP). Arbeitsplatz: ausgeblendet,
    /// wenn er gefuellt ist und auf ein Muster passt (I NP; leerer Arbeitsplatz bleibt sichtbar wie in der ALV-Liste).
    /// </summary>
    public bool Ausgeblendet(JsonElement row)
    {
        var sttxt = Text(row, "Sttxt");
        if (sttxt.Length > 0 && StatusAusschluss.Any(m => Passt(sttxt, m)))
            return true;
        var arbpl = Text(row, "Arbpl");
        return arbpl.Length > 0 && ArbplAusschluss.Any(m => Passt(arbpl, m));
    }

    /// <summary>
    /// Fehler statt stiller Fehlzahl: Ist der Filter aktiv, muss das System Sttxt (und mit Arbeitsplatzteil Arbpl) liefern. Fehlt die
    /// Eigenschaft bei Fertigungsauftraegen (Typ VF), ist der Transport mit den Layoutfeldern noch nicht im System.
    /// </summary>
    public void PruefeFelder(IReadOnlyList<JsonElement> rows)
    {
        if (!Aktiv)
            return;
        var fa = rows.Where(r => Text(r, "Typ") == "VF").ToList();
        if (fa.Count == 0)
            return;
        if (StatusAusschluss.Count > 0 && fa.Any(r => !r.TryGetProperty("Sttxt", out _)))
            throw new InvalidOperationException(
                "ShopAufSet liefert die Statuszeile (Sttxt) in diesem SAP-System noch nicht; Transport mit den ZC22-Layoutfeldern fehlt. " +
                "Shopfloor:Zc22StatusAusschluss leeren oder Transport importieren.");
        // Nach dem Transport ist Arbpl immer da (ohne Avo leer); belegt ist der Arbeitsplatz erst durch das Echo Avo = 'X'
        // (Plan saptasks/shopfloor_sap/PLAN_LAYOUTFILTER_2026-11-13.md, Hinweis F2).
        if (ArbplAusschluss.Count > 0 && fa.Any(r => Text(r, "Avo") != "X"))
            throw new InvalidOperationException(
                "ShopAufSet liefert den naechsten Arbeitsplatz (Avo = 'X') in diesem SAP-System noch nicht; Transport mit den ZC22-Layoutfeldern " +
                "fehlt. Shopfloor:Zc22ArbplAusschluss leeren oder Transport importieren.");
    }

    public JsonObject Beschreibung() => new()
    {
        ["status_ausschluss"] = new JsonArray(StatusAusschluss.Select(x => (JsonNode?)x).ToArray()),
        ["arbeitsplatz_ausschluss"] = new JsonArray(ArbplAusschluss.Select(x => (JsonNode?)x).ToArray())
    };

    private static string Text(JsonElement x, string name)
        => x.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()?.Trim() ?? string.Empty : string.Empty;
}
