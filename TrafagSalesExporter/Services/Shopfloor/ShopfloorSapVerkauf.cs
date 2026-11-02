using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TrafagSalesExporter.Services.Shopfloor;

/// <summary>
/// Operations (Shopfloor), 2026-11-02, Paket Fabio vom 2026-11-01, Job J4: erwarteter Umsatz des Monats und Gutschriften aus
/// Fiori «Umsatz prüfen» (C_SALESANALYTICSQRY_1_CDS, CHF, Kurstyp M, Verkaufsorganisation 1100). Ersetzt ZC23 als Quelle von
/// verkauf.umsatz_plan (Entscheid F. Palma).
/// <list type="bullet">
/// <item>umsatz_plan = Umsatz (SlsVolumeNetAmtInDspCrcy) + offene Lieferungen (OpnOutbDelivsForInvcNetAmtInDC)
/// + offene Auftraege (OpenSalesOrdersNetAmountInDC).</item>
/// <item>gutschriften = CustCrdtMemoNetAmtInDspCrcy.</item>
/// </list>
/// Konvention des Verkaufs: Eintrag vom Tag D = Stand bis D-1, Monat = Monat des Vortags (am Monatsersten der Vormonat).
/// Kontrolle Fabio 01.11.2026: 10/2026 8'827'624 bzw. 110'412 CHF. Manuell erfasste Werte werden nicht ueberschrieben.
/// Doku: docs/SHOPFLOOR_2026-10-07.md Abschnitt 16.
/// </summary>
public static class ShopfloorSapVerkauf
{
    public const string Service = "/sap/opu/odata/sap/C_SALESANALYTICSQRY_1_CDS/";
    public const string Set = "C_SALESANALYTICSQRY_1(P_DisplayCurrency='CHF',P_ExchangeRateType='M')/Results";
    public const string SalesOrganization = "1100";

    public const string Umsatz = "SlsVolumeNetAmtInDspCrcy";
    public const string OffeneLieferungen = "OpnOutbDelivsForInvcNetAmtInDC";
    public const string OffeneAuftraege = "OpenSalesOrdersNetAmountInDC";
    public const string Gutschriften = "CustCrdtMemoNetAmtInDspCrcy";

    /// <summary>Monat (JJJJMM) des Vortags: am Monatsersten der Vormonat.</summary>
    public static string ReportMonth(DateOnly day)
        => day.AddDays(-1).ToString("yyyyMM", CultureInfo.InvariantCulture);

    /// <summary>
    /// Abfrage nur mit den Kennzahlen im $select: die analytische Query verdichtet dann auf eine Zeile fuer den ganzen Monat.
    /// </summary>
    public static string Url(string gatewayRoot, string sapClient, string month)
        => $"{gatewayRoot.TrimEnd('/')}{Service}{Set}?sap-client={sapClient}&$format=json" +
           $"&$select={Umsatz},{OffeneLieferungen},{OffeneAuftraege},{Gutschriften}" +
           "&$filter=" + KpiLiefertreueSync.JsEncode($"SalesOrganization eq '{SalesOrganization}' and BillingDocDateYearMonth eq '{month}'");

    /// <summary>Werte fuer den Tag D aus den Zeilen der Query. Schreibt nichts. Ohne Zeile bleiben die Werte leer.</summary>
    public static JsonObject Compute(DateOnly day, IReadOnlyList<JsonElement> rows)
    {
        var umsatz = rows.Sum(r => Amount(r, Umsatz));
        var lieferungen = rows.Sum(r => Amount(r, OffeneLieferungen));
        var auftraege = rows.Sum(r => Amount(r, OffeneAuftraege));
        var gutschriften = rows.Sum(r => Amount(r, Gutschriften));
        var values = new JsonObject();
        if (rows.Count > 0)
            values["verkauf"] = new JsonObject
            {
                ["umsatz_plan"] = Math.Round(umsatz + lieferungen + auftraege, 2),
                ["gutschriften"] = Math.Round(gutschriften, 2)
            };
        return new JsonObject
        {
            ["day"] = day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["monat"] = ReportMonth(day),
            ["zeilen"] = rows.Count,
            ["werte"] = values,
            ["detail"] = new JsonObject
            {
                ["umsatz"] = Math.Round(umsatz, 2),
                ["offene_lieferungen"] = Math.Round(lieferungen, 2),
                ["offene_auftraege"] = Math.Round(auftraege, 2),
                ["gutschriften"] = Math.Round(gutschriften, 2)
            }
        };
    }

    /// <summary>Betrag aus der OData-Antwort; Edm.Decimal kommt in OData V2 als Zeichenkette.</summary>
    internal static decimal Amount(JsonElement r, string name)
        => r.TryGetProperty(name, out var v)
            ? v.ValueKind switch
            {
                JsonValueKind.Number => v.GetDecimal(),
                JsonValueKind.String when decimal.TryParse(v.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var d) => d,
                _ => 0m
            }
            : 0m;
}
