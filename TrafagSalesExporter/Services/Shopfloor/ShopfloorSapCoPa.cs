using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TrafagSalesExporter.Services.Shopfloor;

/// <summary>
/// Operations (Shopfloor), 2026-11-10, Mail Fabio Palma vom 09.11. (Punkt d): Auftragseingang und Umsatz IST aus CO-PA.
/// <list type="bullet">
/// <item>Quelle: CoPaSumSet (ZPOWERBI_EINKAUF_SRV, Auftrag T76K912866): CE11000, Waehrungstyp 01, Monatssummen je Satzart
/// (A Auftragseingang, F Faktura) und Produktsparte WWPSP; Netto = VV100 + VV110 - VV200 - VV210 - VV220 - VV250 - VV230 - VV240
/// wie der KE30-Bericht Z0001-000006 (P76 09.2026 Bukrs 1100: F 7'878'114.69, A 6'531'197.94 = Fabios Kontrollwerte).</item>
/// <item>ae_aktuell / umsatz_ist: Summe aller Sparten; ae_tx / u_tx: Sparte 0005 Transmitter (TR5 + TX); ae_dw / u_dw: Sparte 0003 DW.
/// ae_rest / u_rest rechnet die Konfiguration.</item>
/// <item>Konvention wie Verkauf J4 (<see cref="ShopfloorSapVerkauf"/>): Eintrag vom Tag D = Stand bis D-1. Periode = Monat des
/// letzten Arbeitstags vor D (seit 2026-11-13, Paket Fabio 13.11. Punkt 9; vorher Monat des Vortags).</item>
/// <item>Stichtag nach HZDAT (Angelegt am) bis D-1, nicht BUDAT (Paket Fabio 13.11. Punkt 8, Kontrollwerte je Tag
/// docs/shopfloor_paket_2026-11-13/). Braucht die Eigenschaft Hzdat am Set; Schalter <c>Shopfloor:SapCoPaHzdat</c>. Ohne Schalter
/// summiert das Set die ganze Periode (Stand der Abfrage).</item>
/// </list>
/// Doku: docs/SHOPFLOOR_2026-10-07.md Abschnitte 20, 22.
/// </summary>
public static class ShopfloorSapCoPa
{
    public const string Set = "CoPaSumSet";
    public const string Bukrs = "1100";
    public const string SparteTransmitter = "0005";
    public const string SparteDw = "0003";

    public static string Filter(string bukrs, DateOnly day, bool hzdat = false)
    {
        var monat = ShopfloorSapVerkauf.LetzterArbeitstag(day);
        var filter = $"Bukrs eq '{bukrs}' and Gjahr eq '{monat:yyyy}' and Perde eq '{monat.Month:000}'";
        return hzdat ? filter + $" and Hzdat le '{day.AddDays(-1):yyyyMMdd}'" : filter;
    }

    /// <summary>Werte fuer den Abfragetag aus den Zeilen von CoPaSumSet. Schreibt nichts.</summary>
    public static JsonObject Compute(DateOnly day, IReadOnlyList<JsonElement> rows)
    {
        decimal Sum(string vrgar, string? sparte)
            => rows.Where(r => Text(r, "Vrgar") == vrgar && (sparte is null || Sparte(Text(r, "Wwpsp")) == sparte))
                   .Sum(r => Dec(r, "Netto"));
        var monat = ShopfloorSapVerkauf.LetzterArbeitstag(day);
        var verkauf = new JsonObject
        {
            ["ae_aktuell"] = Round(Sum("A", null)),
            ["ae_tx"] = Round(Sum("A", SparteTransmitter)),
            ["ae_dw"] = Round(Sum("A", SparteDw)),
            ["umsatz_ist"] = Round(Sum("F", null)),
            ["u_tx"] = Round(Sum("F", SparteTransmitter)),
            ["u_dw"] = Round(Sum("F", SparteDw))
        };
        return new JsonObject
        {
            ["day"] = day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["monat"] = monat.ToString("yyyy-MM", CultureInfo.InvariantCulture),
            ["zeilen"] = rows.Count,
            ["werte"] = new JsonObject { ["verkauf"] = verkauf }
        };
    }

    /// <summary>Produktsparte ohne Leerzeichen, auf 4 Stellen mit fuehrenden Nullen (CE11000 schreibt 0003, 0005).</summary>
    public static string Sparte(string wwpsp)
    {
        var s = wwpsp.Trim();
        return s.Length is > 0 and < 4 && s.All(char.IsAsciiDigit) ? s.PadLeft(4, '0') : s;
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static string Text(JsonElement x, string name)
        => x.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()?.Trim() ?? string.Empty : string.Empty;

    private static decimal Dec(JsonElement x, string name)
        => x.TryGetProperty(name, out var v)
            ? v.ValueKind switch
            {
                JsonValueKind.Number when v.TryGetDecimal(out var d) => d,
                JsonValueKind.String when decimal.TryParse(v.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var s) => s,
                _ => 0m
            }
            : 0m;
}
