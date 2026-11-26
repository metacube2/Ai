using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TrafagSalesExporter.Services.Shopfloor;

/// <summary>
/// Shopfloor Welle 2 und Punkt 9 (Mails Fabio 24.11. und 26.11.2026), SAP-Auftrag T76K912954:
/// <list type="bullet">
/// <item><c>ShopKdTerminSet</c>: je Kundenauftragsposition eine Zeile Typ KD (Material, Typencode, Auftragsmenge, Wunsch-, Bereitstellungs-
/// und Liefertermin der letzten bestaetigten Einteilung, SOBKZ) und je Plan-/Fertigungsauftrag eine Zeile PA/FA (Endtermin, Verfuegbarkeit).</item>
/// <item>TVAV und Angebote fuellen sich nach Kundenauftrag und Position; bei SOBKZ E kommen Fertigungs- und Planauftraege bei jedem Lauf
/// neu aus SAP, sonst bleiben sie Handeingabe. Rueckstandslisten bekommen eigene Felder fuer die VF/WE-Termine (ohne Excel-Spalte, damit der
/// Excel-Import sie nicht ueberschreibt).</item>
/// <item><c>ShopMatBezugSet</c>: Nenner der Materialverfuegbarkeit je KW (verschiedene Materialien mit 201/261, Mo bis Sa).</item>
/// </list>
/// Schalter <c>Shopfloor:SapKdTerminEnabled</c> und <c>Shopfloor:SapNennerEnabled</c>, beide aus bis zum Import nach P76.
/// </summary>
public static class ShopfloorSapKdTermin
{
    public const string Set = "ShopKdTerminSet";
    public const string NennerSet = "ShopMatBezugSet";
    public const string Transport = "T76K912954";

    public static readonly string[] Module = ["tvav", "angebote", "rs_seh", "rs_tr5", "rs_tx", "rs_dw"];

    public sealed record Auftrag(string Typ, string Aufnr, int Menge, string Ende, string Verfueg, string Beskz);

    public sealed record Position(string Vbeln, string Posnr, string Werks, string Matnr, string Zztyp, int Kwmeng,
        string Wunsch, string Bereit, string Liefer, int Bmeng, int BmengLetzte, string Sobkz, List<Auftrag> Fa, List<Auftrag> Pa)
    {
        public bool Einzelfertigung => Sobkz == "E";
    }

    /// <summary>Kundenauftrag ohne fuehrende Nullen; leer bleibt leer.</summary>
    public static string NormVbeln(string? s)
    {
        var t = (s ?? string.Empty).Trim();
        return t.Length > 0 && t.All(char.IsDigit) ? t.TrimStart('0') : t.ToUpperInvariant();
    }

    /// <summary>Position als Zahl ohne fuehrende Nullen („000010“, „10“, „10.0“ -> „10“); unlesbar -> leer.</summary>
    public static string NormPosnr(string? s)
    {
        var t = (s ?? string.Empty).Trim();
        return double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) && d > 0 && d == Math.Floor(d)
            ? ((long)d).ToString(CultureInfo.InvariantCulture)
            : string.Empty;
    }

    public static string Filter(string werks, string? vbeln = null)
        => string.IsNullOrWhiteSpace(vbeln) ? $"Werks eq '{werks}'" : $"Werks eq '{werks}' and Vbeln eq '{NormVbeln(vbeln)}'";

    public static string NennerFilter(string werks, DateOnly von, DateOnly bis)
        => $"Werks eq '{werks}' and Von eq '{von:yyyyMMdd}' and Bis eq '{bis:yyyyMMdd}'";

    /// <summary>
    /// Zeilen des Sets je Position zusammenfassen. Planauftraege mit Beschaffungsart aus <paramref name="paBeskzAusschluss"/> fallen weg
    /// (Entscheid Fabio E2: nur Eigenfertigung, also F); Fertigungsauftraege nur offen (ELIKZ und DNREL leer).
    /// </summary>
    public static Dictionary<(string Vbeln, string Posnr), Position> Parse(IEnumerable<JsonElement> rows, IReadOnlyCollection<string> paBeskzAusschluss)
    {
        var kd = new Dictionary<(string, string), Position>();
        var auf = new List<(string Vbeln, string Posnr, Auftrag A, string Elikz, string Dnrel)>();
        foreach (var x in rows)
        {
            var key = (NormVbeln(Text(x, "Vbeln")), NormPosnr(Text(x, "Posnr")));
            if (key.Item1.Length == 0 || key.Item2.Length == 0) continue;
            var typ = Text(x, "Typ");
            if (typ == "KD")
                kd[key] = new Position(key.Item1, key.Item2, Text(x, "Werks"), Material(Text(x, "Matnr")), Text(x, "Zztyp"), Int(x, "Kwmeng"),
                    Iso(Text(x, "Wunsch")), Iso(Text(x, "Bereit")), Iso(Text(x, "Liefer")), Int(x, "Bmeng"), Int(x, "BmengLetzte"),
                    Text(x, "Sobkz"), [], []);
            else if (typ is "FA" or "PA")
                auf.Add((key.Item1, key.Item2,
                    new Auftrag(typ, Text(x, "Aufnr").TrimStart('0'), Int(x, "Menge"), Iso(Text(x, "Ende")), Iso(Text(x, "Verfueg")), Text(x, "Beskz")),
                    Text(x, "Elikz"), Text(x, "Dnrel")));
        }
        foreach (var (v, p, a, elikz, dnrel) in auf)
        {
            if (!kd.TryGetValue((v, p), out var pos)) continue;
            if (a.Typ == "FA" && elikz.Length == 0 && dnrel.Length == 0) pos.Fa.Add(a);
            if (a.Typ == "PA" && !paBeskzAusschluss.Contains(a.Beskz, StringComparer.OrdinalIgnoreCase)) pos.Pa.Add(a);
        }
        foreach (var pos in kd.Values)
        {
            pos.Fa.Sort((l, r) => string.CompareOrdinal(l.Aufnr, r.Aufnr));
            pos.Pa.Sort((l, r) => string.CompareOrdinal(l.Aufnr, r.Aufnr));
        }
        return kd;
    }

    /// <summary>
    /// Feldwerte fuer einen Eintrag des Moduls. <paramref name="lauf"/> = Abgleich im SAP-Lauf (nur, was „bei jedem Lauf neu“ gilt), sonst
    /// Nachschlagen nach Eingabe von Kundenauftrag/Position. Bei Angeboten sind VkNr, Typencode und Menge Handfelder des Verkaufs: sie werden nur
    /// gefuellt, wenn sie leer sind. Ohne Kundeneinzelfertigung bleiben Fertigungs- und Planauftrag unangetastet (Handeingabe).
    /// </summary>
    public static JsonObject Felder(string modul, Position p, JsonObject? aktuell, bool lauf)
    {
        var f = new JsonObject();
        bool Leer(string k) => aktuell is null || !SfJson.Truthy(SfJson.Get(aktuell, k));
        if (modul is "tvav" or "angebote")
        {
            if (!lauf)
            {
                var nurLeer = modul == "angebote";
                if (!nurLeer || Leer("vknr")) f["vknr"] = p.Matnr;
                if (!nurLeer || Leer("typencode")) f["typencode"] = p.Zztyp;
                if (!nurLeer || Leer("menge")) f["menge"] = p.Kwmeng;
            }
            f["termin_sap"] = p.Liefer.Length > 0 ? p.Liefer : null;
            f["sobkz"] = p.Sobkz.Length > 0 ? p.Sobkz : null;
            if (p.Einzelfertigung)
            {
                f["fa"] = p.Fa.Count > 0 ? string.Join(", ", p.Fa.Select(a => a.Aufnr)) : null;
                f["pl_auf"] = p.Pa.Count > 0 ? string.Join(", ", p.Pa.Select(a => a.Aufnr)) : null;
            }
            return f;
        }
        if (modul.StartsWith("rs_", StringComparison.Ordinal))
        {
            f["wunsch_sap"] = p.Wunsch.Length > 0 ? p.Wunsch : null;
            f["bereit_sap"] = p.Bereit.Length > 0 ? p.Bereit : null;
            f["liefer_sap"] = p.Liefer.Length > 0 ? p.Liefer : null;
            var t = AuftraegeText(p);
            f["auftraege_sap"] = t.Length > 0 ? t : null;
        }
        return f;
    }

    /// <summary>„FA 1241683 (30 Stk.): Ende 24.04.2026, verfügbar 28.04.2026“ je Auftrag, Fertigungsauftraege zuerst.</summary>
    public static string AuftraegeText(Position p)
        => string.Join("\n", p.Fa.Concat(p.Pa).Select(a =>
            $"{a.Typ} {a.Aufnr} ({a.Menge} Stk.): Ende {De(a.Ende)}, verfügbar {De(a.Verfueg)}"));

    /// <summary>Nur die Felder, die sich gegenueber dem gespeicherten Eintrag aendern (null loescht nur, wenn vorher ein Wert stand).</summary>
    public static JsonObject Aenderungen(JsonObject felder, JsonObject aktuell)
    {
        var ch = new JsonObject();
        foreach (var kv in felder)
        {
            var alt = SfJson.Get(aktuell, kv.Key);
            if (kv.Value is null && !SfJson.Truthy(alt)) continue;
            if (SfJson.PyStr(alt) == SfJson.PyStr(kv.Value)) continue;
            ch[kv.Key] = SfJson.Clone(kv.Value);
        }
        return ch;
    }

    /// <summary>Antwort fuer das Nachschlagen im Formular (GET sap/kdtermin): Status ok / nicht_gefunden, Felder je Position.</summary>
    public static JsonObject Nachschlagen(string modul, string vbeln, string posnr, Dictionary<(string, string), Position> positionen, JsonObject? aktuell)
    {
        var key = (NormVbeln(vbeln), NormPosnr(posnr));
        if (!positionen.TryGetValue(key, out var p))
            return new JsonObject
            {
                ["status"] = "nicht_gefunden",
                ["meldung"] = positionen.Keys.Any(k => k.Item1 == key.Item1)
                    ? $"Position {key.Item2} gibt es im Kundenauftrag {key.Item1} im Werk nicht."
                    : $"Kundenauftrag {key.Item1} im Werk nicht gefunden."
            };
        return new JsonObject
        {
            ["status"] = "ok",
            ["felder"] = Felder(modul, p, aktuell, false),
            ["einzelfertigung"] = p.Einzelfertigung,
            ["auftraege"] = AuftraegeText(p)
        };
    }

    /// <summary>
    /// Abgleich im SAP-Lauf: fuer offene Eintraege der Module mit Kundenauftrag und Position die Felder, die „bei jedem Lauf neu“ gelten, als
    /// Push-Items (Quelle SAP, keine Mail). Eine Position, die das Set im Werksmodus nicht liefert (kein offener Bedarf), bleibt unangetastet.
    /// </summary>
    public static (List<JsonObject> Items, int Geprueft, int Treffer) Abgleich(
        IEnumerable<(string Modul, string Nr, JsonObject Daten)> eintraege, ShopfloorConfig cfg, Dictionary<(string, string), Position> positionen)
    {
        var items = new List<JsonObject>();
        int geprueft = 0, treffer = 0;
        foreach (var (modul, nr, d) in eintraege)
        {
            if (!cfg.Modules.TryGetValue(modul, out var mod)) continue;
            if (!string.IsNullOrEmpty(mod.StatusField) && cfg.IsClosed(SfJson.Str(SfJson.Get(d, mod.StatusField)), mod)) continue;
            var key = (NormVbeln(SfJson.Str(SfJson.Get(d, "ka_nr"))), NormPosnr(SfJson.Str(SfJson.Get(d, "ka_pos"))));
            if (key.Item1.Length == 0 || key.Item2.Length == 0) continue;
            geprueft++;
            if (!positionen.TryGetValue(key, out var p)) continue;
            treffer++;
            var felder = Felder(modul, p, d, true);
            // nur Felder, die das Modul kennt (Konfiguration ohne neue Felder: nichts schreiben)
            foreach (var k in felder.Select(kv => kv.Key).ToList())
                if (!mod.Fields.Any(f => f.Key == k)) felder.Remove(k);
            var ch = Aenderungen(felder, d);
            if (ch.Count > 0)
                items.Add(new JsonObject { ["module"] = modul, ["nr"] = nr, ["fields"] = ch });
        }
        return (items, geprueft, treffer);
    }

    // ---------------------------------------------------------------- Nenner

    /// <summary>
    /// Abgeschlossene Wochen (Montag) des laufenden ISO-Jahres, fuer die der SAP-Nenner fehlt; die letzte abgeschlossene Woche immer
    /// (spaete Buchungen). Die laufende Woche nie.
    /// </summary>
    public static List<DateOnly> NennerWochen(DateOnly heute, IReadOnlySet<string> mitSapWert)
    {
        var laufend = heute.AddDays(-(((int)heute.DayOfWeek + 6) % 7));
        var letzte = laufend.AddDays(-7);
        var jahr = ISOWeek.GetYear(letzte.ToDateTime(TimeOnly.MinValue));
        var start = DateOnly.FromDateTime(ISOWeek.ToDateTime(jahr, 1, DayOfWeek.Monday));
        var result = new List<DateOnly>();
        for (var m = start; m <= letzte; m = m.AddDays(7))
            if (m == letzte || !mitSapWert.Contains(ShopfloorZd05.KwKey(m.ToDateTime(TimeOnly.MinValue))))
                result.Add(m);
        return result;
    }

    public static int? NennerAnzahl(IEnumerable<JsonElement> rows)
    {
        foreach (var x in rows)
            return Int(x, "Anzahl");
        return null;
    }

    // ---------------------------------------------------------------- Hilfen
    private static string De(string iso) => iso.Length == 10 ? $"{iso[8..10]}.{iso[5..7]}.{iso[..4]}" : "–";

    private static string Iso(string yyyymmdd) => ShopfloorSapSync.IsoDate(yyyymmdd);

    private static string Material(string matnr)
    {
        var m = matnr.Trim();
        return m.Length > 0 && m.All(char.IsDigit) ? m.TrimStart('0') : m;
    }

    private static string Text(JsonElement x, string name)
        => x.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()?.Trim() ?? string.Empty : string.Empty;

    private static int Int(JsonElement x, string name)
        => x.TryGetProperty(name, out var v)
            ? v.ValueKind switch
            {
                JsonValueKind.Number when v.TryGetInt32(out var i) => i,
                JsonValueKind.String when int.TryParse(v.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var s) => s,
                _ => 0
            }
            : 0;
}
