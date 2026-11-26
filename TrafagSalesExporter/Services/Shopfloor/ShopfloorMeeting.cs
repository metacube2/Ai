using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.Data.Sqlite;

namespace TrafagSalesExporter.Services.Shopfloor;

/// <summary>
/// Meeting-Modus „nur Abweichungen“ (Paket Fabio 24.11.2026 Punkt 6, freigegeben 25.11., SEH ausgenommen laut Mail 26.11.).
/// <list type="bullet">
/// <item>Bewertet wird die produzierte Tagesmenge (<c>daily.prod</c>) gegen das Tagesziel (<c>daily.ziel</c>) des letzten Tages
/// VOR dem Stichtag, an dem beide Werte stehen (Stand Vortag wie der Radar; der laufende Tag ist am Morgen noch unvollstaendig).</item>
/// <item>gelb ab <c>gelb_unter_pct</c> unter dem Ziel, rot ab <c>rot_unter_pct</c> darunter, Hinweis ab <c>hinweis_ueber_pct</c>
/// darueber; Grenzen zaehlen mit („ab 10 %“ heisst −10,0 % ist gelb).</item>
/// <item>Fehlt Menge oder Ziel oder ist der letzte Wert aelter als <c>max_alter_tage</c> Kalendertage, gibt es keine Bewertung
/// („keine_daten“). Ein fehlender Wert zaehlt nie als „unter Ziel“ (TX hat seit Oktober keine MES-Mengen).</item>
/// <item>Serie: Anzahl der letzten Tage mit Menge und Ziel (innerhalb von 14 Kalendertagen vor dem Stichtag), die ohne Unterbruch
/// unter dem Ziel liegen. Ab <c>tage_in_folge</c> wird hervorgehoben. Tage ohne Werte (Wochenende, Feiertag) werden uebersprungen.</item>
/// </list>
/// Alle Schwellen stehen in <c>shopfloor_config.json</c> unter <c>meeting_modus</c>; ohne Eintrag gelten die Werte unten.
/// </summary>
public static class ShopfloorMeeting
{
    public sealed record Schwellen(
        double GelbUnterPct,
        double RotUnterPct,
        double HinweisUeberPct,
        int TageInFolge,
        int MaxAlterTage,
        IReadOnlyList<string> Abteilungen,
        IReadOnlyList<string> Ausnehmen,
        IReadOnlyList<string> Zuerst);

    public static readonly Schwellen Standard = new(10, 20, 20, 3, 4,
        ["seh", "tr5", "tx", "dw"], ["seh"], ["zd05", "rs_seh", "rs_tr5", "rs_tx", "rs_dw"]);

    private const int SerieFensterTage = 14;

    public static Schwellen Lesen(string rawConfigJson)
    {
        var root = SfJson.ParseObject(rawConfigJson);
        if (root["meeting_modus"] is not JsonObject mm) return Standard;
        double D(string k, double d) => SfJson.AsDouble(mm[k]) ?? d;
        IReadOnlyList<string> L(string k, IReadOnlyList<string> d)
            => mm[k] is JsonArray a ? a.Select(x => SfJson.Str(x)).Where(x => x.Length > 0).ToList() : d;
        return new Schwellen(
            D("gelb_unter_pct", Standard.GelbUnterPct),
            D("rot_unter_pct", Standard.RotUnterPct),
            D("hinweis_ueber_pct", Standard.HinweisUeberPct),
            (int)D("tage_in_folge", Standard.TageInFolge),
            (int)D("max_alter_tage", Standard.MaxAlterTage),
            L("abteilungen", Standard.Abteilungen),
            L("ausnehmen", Standard.Ausnehmen),
            L("zuerst", Standard.Zuerst));
    }

    /// <summary>Stufe einer Tagesmenge gegen das Ziel: rot, gelb, hinweis, ok oder keine_daten.</summary>
    public static string Stufe(double? prod, double? ziel, Schwellen s)
    {
        if (prod is null || ziel is null || ziel.Value <= 0) return "keine_daten";
        var abw = Abweichung(prod.Value, ziel.Value);
        if (abw <= -s.RotUnterPct) return "rot";
        if (abw <= -s.GelbUnterPct) return "gelb";
        if (abw >= s.HinweisUeberPct) return "hinweis";
        return "ok";
    }

    /// <summary>Abweichung in Prozent, auf eine Nachkommastelle gerundet (so wird auch die Grenze verglichen).</summary>
    public static double Abweichung(double prod, double ziel) => Math.Round((prod - ziel) / ziel * 100, 1);

    public static JsonObject Compute(SqliteConnection con, string stichtag, Schwellen s, Func<string, string>? titel = null)
    {
        var tag = ShopfloorZd05.ParseDay(stichtag);
        var von = tag.AddDays(-SerieFensterTage).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var rows = new JsonArray();
        foreach (var modul in s.Abteilungen)
        {
            var row = new JsonObject { ["key"] = modul, ["label"] = titel?.Invoke(modul) ?? modul.ToUpperInvariant() };
            if (s.Ausnehmen.Contains(modul, StringComparer.Ordinal))
            {
                row["stufe"] = "ausgenommen";
                rows.Add(row);
                continue;
            }

            // Tage mit Menge UND Ziel, juengster zuerst, nur vor dem Stichtag
            var tage = new List<(string Tag, double Prod, double Ziel)>();
            foreach (var r in ShopfloorDb.Query(con, "SELECT day, data FROM daily WHERE module=? AND day>=? AND day<? ORDER BY day DESC",
                         modul, von, stichtag))
            {
                var d = SfJson.ParseObject(r.Str("data"));
                var p = SfJson.AsDouble(SfJson.Get(d, "prod"));
                var z = SfJson.AsDouble(SfJson.Get(d, "ziel"));
                if (p is null || z is null || z.Value <= 0) continue;
                tage.Add((r.Str("day"), p.Value, z.Value));
            }

            var letzter = tage.Count > 0 ? tage[0] : default;
            var zuAlt = tage.Count == 0
                || (tag - ShopfloorZd05.ParseDay(letzter.Tag)).TotalDays > s.MaxAlterTage;
            if (zuAlt)
            {
                row["stufe"] = "keine_daten";
                row["letzter_tag"] = tage.Count > 0 ? letzter.Tag : null;
                rows.Add(row);
                continue;
            }

            var serie = 0;
            foreach (var t in tage)
            {
                if (t.Prod < t.Ziel) serie++;
                else break;
            }

            row["tag"] = letzter.Tag;
            row["prod"] = letzter.Prod;
            row["ziel"] = letzter.Ziel;
            row["abw_pct"] = Abweichung(letzter.Prod, letzter.Ziel);
            row["stufe"] = Stufe(letzter.Prod, letzter.Ziel, s);
            row["serie"] = serie;
            row["serie_alarm"] = serie >= s.TageInFolge;
            rows.Add(row);
        }

        return new JsonObject
        {
            ["stichtag"] = stichtag,
            ["schwellen"] = new JsonObject
            {
                ["gelb_unter_pct"] = s.GelbUnterPct,
                ["rot_unter_pct"] = s.RotUnterPct,
                ["hinweis_ueber_pct"] = s.HinweisUeberPct,
                ["tage_in_folge"] = s.TageInFolge,
                ["max_alter_tage"] = s.MaxAlterTage
            },
            ["zuerst"] = new JsonArray(s.Zuerst.Select(x => (JsonNode?)JsonValue.Create(x)).ToArray()),
            ["rows"] = rows
        };
    }
}
