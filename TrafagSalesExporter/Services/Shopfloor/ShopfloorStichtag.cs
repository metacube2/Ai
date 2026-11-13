using System.Globalization;
using System.Text.Json.Nodes;

namespace TrafagSalesExporter.Services.Shopfloor;

/// <summary>
/// Operations (Shopfloor), Mail Fabio Palma 2026-11-13, Punkt 1: Stichtagswerte (Auftragsvorrat total/3/2/1 Mte, offene KW, SEH
/// offene Tagesmenge <c>ziel</c>, TX <c>plan5</c>, PPD-Verzug) gelten so, wie sie der erste erfolgreiche SAP-Lauf des Tages geliefert
/// hat (normal 04:00; Entscheid Ingo 13.11.: faellt 04:00 aus, der naechste Lauf). Bis zum 13.11. haben die Laeufe 12:45 und die
/// Laeufe nach jedem Neustart sie ueberschrieben; die Tagesleistung SEH stand deshalb bei 214 bis 2'335 %.
/// <para>Das Einfrieren selbst macht <see cref="ShopfloorSapMengen.Freeze"/>. Diese Klasse holt die Morgenwerte vergangener Tage
/// aus dem Verlauf (Tabelle <c>history</c>) zurueck: je Tag, Modul und Feld der erste SAP-Wert. Neu rechnen geht nicht, weil
/// ShopAufSet nur die heute offenen Auftraege kennt.</para>
/// Endpunkte <c>GET/POST shopfloor/api/sap/morgenwerte?von=JJJJ-MM-TT&amp;bis=JJJJ-MM-TT</c>. Doku: docs/SHOPFLOOR_2026-10-07.md 21.
/// </summary>
public static class ShopfloorStichtag
{
    /// <summary>Module und Felder, die als Stichtagswerte gelten.</summary>
    public static IEnumerable<(string Modul, string[] Felder)> Felder()
    {
        foreach (var (_, modul) in ShopfloorSapVorrat.Module)
            yield return (modul, ShopfloorSapVorrat.Felder(modul));
        yield return (ShopfloorSapVerzug.Module, ShopfloorSapVerzug.Abteilungen.Select(a => a.Feld).ToArray());
    }

    /// <summary>
    /// Schreibt berechnete Stichtagswerte (Vorrat oder Verzug, <c>{ day, werte }</c>) ueber ApplyPush als SAP. Felder, die fuer den
    /// Tag schon einen SAP-Wert haben, bleiben stehen und stehen unter <c>eingefroren</c>; <paramref name="ersetzen"/> schreibt
    /// trotzdem. <c>werte</c> im Ergebnis bleibt fuer die Anzeige vollstaendig.
    /// </summary>
    public static void Write(ShopfloorStore store, JsonObject result, bool ersetzen)
    {
        var dayIso = (string)result["day"]!;
        JsonObject? Existing(string module) => store.GetDaily(module, dayIso, dayIso)[module]?[dayIso] as JsonObject;
        var toWrite = (JsonObject)result.DeepClone();
        result["eingefroren"] = ersetzen ? new JsonArray() : ShopfloorSapMengen.Freeze(toWrite, Existing);
        var (items, skipped) = ShopfloorSapMengen.BuildItems(toWrite, Existing, store.Config.IsSapField);
        result["uebersprungen_manuell"] = skipped;
        result["ergebnis"] = items.Count == 0 ? new JsonArray() : store.ApplyPush(new ShopfloorPush { Items = items }, "SAP");
        result["geschrieben"] = true;
    }

    /// <summary>
    /// Vergleicht je Tag von <paramref name="von"/> bis <paramref name="bis"/> den aktuellen Wert mit dem ersten SAP-Wert des Tages
    /// und schreibt ihn mit <paramref name="write"/> als SAP zurueck. Nur Felder, deren aktueller Wert aus SAP stammt; von Hand
    /// erfasste Werte bleiben. Tage ohne SAP-Eintrag im Verlauf bleiben, wie sie sind.
    /// </summary>
    public static JsonObject RestoreMorning(ShopfloorStore store, DateOnly von, DateOnly bis, bool write)
    {
        if (bis < von)
            (von, bis) = (bis, von);
        if (bis.DayNumber - von.DayNumber > 62)
            throw new ArgumentException("hoechstens 62 Tage auf einmal");
        var zeilen = new JsonArray();
        var items = new JsonArray();
        for (var d = von; d <= bis; d = d.AddDays(1))
        {
            var day = d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            foreach (var (modul, felder) in Felder())
            {
                var first = store.FirstSapValues(modul, day, felder);
                if (first.Count == 0)
                    continue;
                var row = store.GetDaily(modul, day, day)[modul]?[day] as JsonObject;
                var data = row?["data"] as JsonObject;
                var meta = row?["meta"] as JsonObject;
                var fields = new JsonObject();
                foreach (var (key, node) in first)
                {
                    var morgen = node!["wert"]!;
                    var aktuell = data?[key];
                    var ausSap = string.Equals((string?)meta?[key]?["src"], "SAP", StringComparison.OrdinalIgnoreCase);
                    var gleich = aktuell is not null && SfJson.Equal(aktuell, morgen);
                    var setzen = !gleich && (aktuell is null || ausSap);
                    zeilen.Add(new JsonObject
                    {
                        ["tag"] = day, ["modul"] = modul, ["feld"] = key,
                        ["morgenwert"] = morgen.DeepClone(), ["von"] = (string?)node["ts"],
                        ["aktuell"] = aktuell?.DeepClone(),
                        ["aktion"] = gleich ? "gleich" : setzen ? "zurueckgesetzt" : "manuell, bleibt"
                    });
                    if (setzen)
                        fields[key] = morgen.DeepClone();
                }
                if (fields.Count > 0)
                    items.Add(new JsonObject { ["module"] = modul, ["day"] = day, ["fields"] = fields });
            }
        }
        var result = new JsonObject
        {
            ["ok"] = true,
            ["von"] = von.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["bis"] = bis.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["zeilen"] = zeilen,
            ["zurueckzusetzen"] = items.Sum(i => ((JsonObject)i!["fields"]!).Count),
            ["geschrieben"] = false
        };
        if (write && items.Count > 0)
        {
            result["ergebnis"] = store.ApplyPush(new ShopfloorPush { Items = items }, "SAP");
            result["geschrieben"] = true;
        }
        return result;
    }
}
