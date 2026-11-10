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
/// <item>Abteilung ueber den Disponenten wie beim Verzug. <b>Kein Materialfilter</b>: Fabios Schnittstelle (1.xlsx, Schnittstelle
/// 02.11.) definiert den Vorrat ohne; seine Kontrollzahl SEH 159'661 (07.11.) passt nur so, mit „Material 1-99999“ liefert ZC22
/// fuer SEH 0 (P76, 09.11.).</item>
/// <item>Buckets ab dem Abfragetag D, kumuliert und einschliesslich Ueberfaelliger: total; 1 Mte bis Monatsende von D; 2 Mte bis
/// Ende Folgemonat; 3 Mte bis Ende uebernaechster Monat; offen_kw bis Sonntag der laufenden KW; SEH ziel bis D.</item>
/// <item>Werte auf den Abfragetag geschrieben; manuell erfasste Werte bleiben (ShopfloorSapMengen.BuildItems).</item>
/// </list>
/// Abgleich T76 2026-11-09: SEH 1'877 Auftraege / 179'981 Stk., je Auftrag gleich ZC22. Doku: docs/SHOPFLOOR_2026-10-07.md 19.
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
    /// Module mit Planmenge der naechsten 5 Arbeitstage (Feld plan5, Fabio: „Eckendtermin naechste 5 AT“, nur TX). Annahme 2026-11-10,
    /// von Fabio zu bestaetigen: ab dem Tag nach D bis zum 5. Arbeitstag (Mo bis Fr, ohne Feiertage), ohne Ueberfaellige.
    /// </summary>
    public static readonly string[] Plan5Module = ["tx"];

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

    public static string Filter(string plant, DateOnly day)
        => $"Werks eq '{plant}' and Datum eq '{day:yyyyMMdd}' and Typ eq 'VR'";

    /// <summary>Obergrenzen (einschliesslich) der kumulierten Buckets fuer den Abfragetag.</summary>
    public static (DateOnly M1, DateOnly M2, DateOnly M3, DateOnly KwEnde) Grenzen(DateOnly day)
    {
        var monatsEnde = new DateOnly(day.Year, day.Month, 1).AddMonths(1).AddDays(-1);
        var m2 = new DateOnly(day.Year, day.Month, 1).AddMonths(2).AddDays(-1);
        var m3 = new DateOnly(day.Year, day.Month, 1).AddMonths(3).AddDays(-1);
        var bisSonntag = ((int)DayOfWeek.Sunday - (int)day.DayOfWeek + 7) % 7;
        return (monatsEnde, m2, m3, day.AddDays(bisSonntag));
    }

    /// <summary>
    /// Werte fuer den Abfragetag aus den Zeilen von ShopAufSet Typ VR. Schreibt nichts. Zeilen ohne Typ VF/VP heissen, dass das
    /// System den Vorratsmodus noch nicht kennt (alter DPC liefert dann den Forecast mit FE/PA): Fehler statt falscher Zahl.
    /// </summary>
    public static JsonObject Compute(DateOnly day, IReadOnlyList<JsonElement> rows)
    {
        if (rows.Any(r => Text(r, "Typ") is not ("VF" or "VP")))
            throw new InvalidOperationException(
                "ShopAufSet kennt den Vorratsmodus (Typ VR) in diesem SAP-System noch nicht; Transport T76K912860 fehlt.");
        var g = Grenzen(day);
        var plan5Ende = Plan5Ende(day);
        var summen = Module.ToDictionary(m => m.Abteilung, _ => new long[7], StringComparer.Ordinal);   // total, 3m, 2m, 1m, kw, tag, plan5
        var auftraege = Module.ToDictionary(m => m.Abteilung, _ => 0, StringComparer.Ordinal);
        var ohneDatum = 0;
        foreach (var r in rows)
        {
            if (ShopfloorSapVerzug.Abteilung(Text(r, "Dispo")) is not { } abt)
                continue;
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
            if (eck > day && eck <= plan5Ende) s[6] += menge;
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
            detail[abt] = new JsonObject { ["auftraege"] = auftraege[abt], ["tag"] = s[5] };
        }
        return new JsonObject
        {
            ["day"] = day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["zeilen"] = rows.Count,
            ["ohne_eckendtermin"] = ohneDatum,
            ["grenzen"] = new JsonObject
            {
                ["1m"] = g.M1.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["2m"] = g.M2.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["3m"] = g.M3.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["kw"] = g.KwEnde.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["plan5"] = plan5Ende.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            },
            ["werte"] = werte,
            ["detail"] = detail
        };
    }

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
