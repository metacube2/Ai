using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TrafagSalesExporter.Services.Shopfloor;

/// <summary>
/// Operations (Shopfloor), 2026-11-09, Mail Fabio Palma (Punkt a): Auftragsvorrat, offene Menge der KW und (nur SEH) offene
/// Tagesmenge je Abteilung wie ZC22 (ZM_OFFENE_FAUF).
/// <list type="bullet">
/// <item>Quelle: ShopAufSet mit Filter Typ eq 'VR' (Auftrag T76K912860): dieselben offenen Fertigungs- und Planauftraege wie der
/// Verzug (Typ VZ, <see cref="ShopfloorSapVerzug"/>), aber ohne Eckendtermin-Grenze. Menge = Positionsmenge wie die ZC22-Spalte.</item>
/// <item>Abteilung ueber den Disponenten wie beim Verzug. Materialfilter 1-99999 nur fuer die Abteilungen aus
/// <c>Shopfloor:MaterialFilter</c> (seit 2026-11-13 TR5 und DW, Paket Fabio 13.11. Punkte 2 und 3, wie die ZC22-Varianten der
/// Teamleiter, z. B. /PPA EKX ALLE); SEH und TX ohne (SEH hat nur alphanumerische Materialien).</item>
/// <item>Buckets ab dem Abfragetag D, kumuliert und einschliesslich Ueberfaelliger. Seit 2026-11-13 rollend (Paket Fabio 13.11.
/// Punkt 1, die Spezifikation vom 07.11. mit Monatsende war falsch): SEH, TR5, TX Eckendtermin bis D + 1 / 2 / 3 Kalendermonate,
/// DW bis D + 4 / 8 / 12 Wochen. offen_kw bis Sonntag der laufenden KW; SEH ziel bis D; TX plan5 bis zum 5. Arbeitstag nach D,
/// ebenfalls kumuliert mit Ueberfaelligen und heutigen Auftraegen (Punkt 5).</item>
/// <item>Werte auf den Abfragetag geschrieben; manuell erfasste Werte bleiben (ShopfloorSapMengen.BuildItems).</item>
/// </list>
/// Abgleich T76 2026-11-09: SEH 1'877 Auftraege / 179'981 Stk., je Auftrag gleich ZC22. Doku: docs/SHOPFLOOR_2026-10-07.md 19, 22.
/// </summary>
public static class ShopfloorSapVorrat
{
    /// <summary>Abteilung (wie Verzug) und Modul im Cockpit.</summary>
    public static readonly (string Abteilung, string Modul)[] Module =
    [
        ("SEH", "seh"),
        ("TR5", "tr5"),
        ("TX", "tx"),
        ("DW", "dw")
    ];

    /// <summary>Module, die zusaetzlich die offene Tagesmenge (Feld ziel) aus SAP bekommen (Entscheid Fabio 07.11.: nur SEH).</summary>
    public static readonly string[] ZielModule = ["seh"];

    /// <summary>
    /// Module mit Planmenge der naechsten 5 Arbeitstage (Feld plan5, nur TX). Seit 2026-11-13 (Paket Fabio 13.11. Punkt 5) kumuliert:
    /// alles mit Eckendtermin bis zum 5. Arbeitstag nach D (Mo bis Fr), einschliesslich ueberfaelliger und heutiger Auftraege.
    /// Kontrolle Fabio 13.11.: ZC22 EL* bis 20.11. 7'083, Excel 7'048. Die Annahme vom 10.11. (ohne Ueberfaellige) ist ueberholt.
    /// </summary>
    public static readonly string[] Plan5Module = ["tx"];

    /// <summary>Module, deren Fenster in Wochen (4 / 8 / 12) statt in Kalendermonaten rechnen (Paket Fabio 13.11. Punkt 1: DW).</summary>
    public static readonly string[] WochenModule = ["dw"];

    /// <summary>Letzter Tag (einschliesslich) der naechsten 5 Arbeitstage nach <paramref name="day"/> (Mo bis Fr).</summary>
    public static DateOnly Plan5Ende(DateOnly day)
    {
        var d = day;
        var n = 0;
        while (n < 5)
        {
            d = d.AddDays(1);
            if (d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
                n++;
        }
        return d;
    }

    /// <summary>Felder, die der Vorrat fuer ein Modul schreibt (Stichtagswerte, Mail Fabio 2026-11-13).</summary>
    public static string[] Felder(string modul)
        => ["vorrat_total", "vorrat_3m", "vorrat_2m", "vorrat_1m", "offen_kw",
            .. ZielModule.Contains(modul) ? new[] { "ziel" } : [],
            .. Plan5Module.Contains(modul) ? new[] { "plan5" } : []];

    /// <summary>Filter fuer ShopAufSet Typ VR; <paramref name="vorgangsdaten"/> fordert den naechsten Arbeitsplatz an (Avo, teuer).</summary>
    public static string Filter(string plant, DateOnly day, bool vorgangsdaten = false)
        => $"Werks eq '{plant}' and Datum eq '{day:yyyyMMdd}' and Typ eq 'VR'" + (vorgangsdaten ? " and Avo eq 'X'" : string.Empty);

    /// <summary>
    /// Obergrenzen (einschliesslich) der kumulierten Buckets fuer den Abfragetag, rollend ab D (seit 2026-11-13): Kalendermonate
    /// (D + 1 / 2 / 3 Monate, am Monatsende auf den letzten Tag des Zielmonats gekuerzt) oder bei <paramref name="wochen"/>
    /// D + 28 / 56 / 84 Tage. offen_kw bis Sonntag der laufenden KW.
    /// </summary>
    public static (DateOnly M1, DateOnly M2, DateOnly M3, DateOnly KwEnde) Grenzen(DateOnly day, bool wochen = false)
    {
        var bisSonntag = ((int)DayOfWeek.Sunday - (int)day.DayOfWeek + 7) % 7;
        return wochen
            ? (day.AddDays(28), day.AddDays(56), day.AddDays(84), day.AddDays(bisSonntag))
            : (day.AddMonths(1), day.AddMonths(2), day.AddMonths(3), day.AddDays(bisSonntag));
    }

    /// <summary>
    /// Werte fuer den Abfragetag aus den Zeilen von ShopAufSet Typ VR. Schreibt nichts. Zeilen ohne Typ VF/VP heissen, dass das
    /// System den Vorratsmodus noch nicht kennt (alter DPC liefert dann den Forecast mit FE/PA): Fehler statt falscher Zahl.
    /// <paramref name="materialFilter"/>: Abteilungen, die nur Material 1-99999 zaehlen (Vorgabe keine). <paramref name="layout"/>:
    /// Filter des ZC22-Standardlayouts (Vorgabe aus), ausgeblendete Auftraege fallen aus allen Feldern weg.
    /// </summary>
    public static JsonObject Compute(DateOnly day, IReadOnlyList<JsonElement> rows, IReadOnlyCollection<string>? materialFilter = null,
        ShopfloorLayoutFilter? layout = null)
    {
        materialFilter ??= [];
        layout ??= ShopfloorLayoutFilter.Aus;
        if (rows.Any(r => Text(r, "Typ") is not ("VF" or "VP")))
            throw new InvalidOperationException(
                "ShopAufSet kennt den Vorratsmodus (Typ VR) in diesem SAP-System noch nicht; Transport T76K912860 fehlt.");
        layout.PruefeFelder(rows);
        var ausgeblendet = Module.ToDictionary(m => m.Abteilung, _ => (Auftraege: 0, Menge: 0L), StringComparer.Ordinal);
        var grenzen = Module.ToDictionary(m => m.Abteilung, m => Grenzen(day, WochenModule.Contains(m.Modul)), StringComparer.Ordinal);
        var plan5Ende = Plan5Ende(day);
        var summen = Module.ToDictionary(m => m.Abteilung, _ => new long[7], StringComparer.Ordinal);   // total, 3m, 2m, 1m, kw, tag, plan5
        var auftraege = Module.ToDictionary(m => m.Abteilung, _ => 0, StringComparer.Ordinal);
        var ohneDatum = 0;
        foreach (var r in rows)
        {
            if (ShopfloorSapVerzug.Abteilung(Text(r, "Dispo")) is not { } abt)
                continue;
            if (materialFilter.Contains(abt) && !ShopfloorSapVerzug.MaterialImBereich(Text(r, "Matnr")))
                continue;
            if (layout.Aktiv && layout.Ausgeblendet(r))
            {
                var a = ausgeblendet[abt];
                ausgeblendet[abt] = (a.Auftraege + 1, a.Menge + Int(r, "Menge"));
                continue;
            }
            var g = grenzen[abt];
            var s = summen[abt];
            var menge = Int(r, "Menge");
            auftraege[abt]++;
            s[0] += menge;
            if (!DateOnly.TryParseExact(Text(r, "Datum"), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var eck))
            {
                ohneDatum++;
                continue;
            }
            if (eck <= g.M3) s[1] += menge;
            if (eck <= g.M2) s[2] += menge;
            if (eck <= g.M1) s[3] += menge;
            if (eck <= g.KwEnde) s[4] += menge;
            if (eck <= day) s[5] += menge;
            if (eck <= plan5Ende) s[6] += menge;
        }
        var werte = new JsonObject();
        var detail = new JsonObject();
        foreach (var (abt, modul) in Module)
        {
            var s = summen[abt];
            var felder = new JsonObject
            {
                ["vorrat_total"] = s[0],
                ["vorrat_3m"] = s[1],
                ["vorrat_2m"] = s[2],
                ["vorrat_1m"] = s[3],
                ["offen_kw"] = s[4]
            };
            if (ZielModule.Contains(modul))
                felder["ziel"] = s[5];
            if (Plan5Module.Contains(modul))
                felder["plan5"] = s[6];
            werte[modul] = felder;
            var g = grenzen[abt];
            detail[abt] = new JsonObject
            {
                ["auftraege"] = auftraege[abt],
                ["tag"] = s[5],
                ["materialfilter"] = materialFilter.Contains(abt),
                ["layout_ausgeblendet"] = new JsonObject
                {
                    ["auftraege"] = ausgeblendet[abt].Auftraege,
                    ["menge"] = ausgeblendet[abt].Menge
                },
                ["grenzen"] = new JsonObject
                {
                    ["1m"] = Iso(g.M1),
                    ["2m"] = Iso(g.M2),
                    ["3m"] = Iso(g.M3),
                    ["kw"] = Iso(g.KwEnde)
                }
            };
        }
        return new JsonObject
        {
            ["day"] = Iso(day),
            ["zeilen"] = rows.Count,
            ["ohne_eckendtermin"] = ohneDatum,
            ["plan5_bis"] = Iso(plan5Ende),
            ["layoutfilter"] = layout.Beschreibung(),
            ["werte"] = werte,
            ["detail"] = detail
        };
    }

    private static string Iso(DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string Text(JsonElement x, string name)
        => x.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()?.Trim() ?? string.Empty : string.Empty;

    private static long Int(JsonElement x, string name)
        => x.TryGetProperty(name, out var v)
            ? v.ValueKind switch
            {
                JsonValueKind.Number when v.TryGetInt64(out var i) => i,
                JsonValueKind.String when decimal.TryParse(v.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var s) => (long)Math.Round(s),
                _ => 0
            }
            : 0;
}
