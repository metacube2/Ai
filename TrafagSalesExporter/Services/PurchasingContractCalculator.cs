using System.Globalization;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Eine Mengenkontrakt-Position so, wie sie aus dem Cache <c>PurchasingContractCache</c> kommt
/// (Zahlen bereits als Dezimalwerte, Datum bereits geparst).
/// </summary>
public sealed record PurchasingContractItem(
    string Ebeln,
    string Ebelp,
    string Bukrs,
    string Bsart,
    string Lifnr,
    string SupplierName,
    string Matnr,
    string Text,
    string Unit,
    string Currency,
    decimal ExchangeRate,
    DateTime? ValidTo,
    string DeletionFlag,
    decimal TargetQuantity,
    decimal NetPrice,
    decimal PriceUnit,
    decimal CalledQuantity);

/// <summary>Berechnete Kontraktposition mit offener Menge und offenem Wert.</summary>
public sealed record PurchasingContractRow(
    string Ebeln,
    string Ebelp,
    string Lifnr,
    string Supplier,
    string Matnr,
    string Text,
    string Unit,
    string Currency,
    decimal TargetQuantity,
    decimal CalledQuantity,
    decimal OpenQuantity,
    decimal OpenValueOriginal,
    decimal OpenValueChf,
    DateTime? ValidTo,
    bool IsExpired,
    bool MissingRate);

public sealed record PurchasingContractSummary(
    decimal TotalChf,
    decimal ExpiredChf,
    int ContractCount,
    int ExpiredContractCount,
    int ItemCount,
    int ExpiredItemCount,
    int MissingRateItemCount)
{
    public static PurchasingContractSummary Empty { get; } = new(0m, 0m, 0, 0, 0, 0, 0);
}

/// <summary>Ergebnis der Auswertung: eingerechnete Positionen plus Zaehler der ausgeschlossenen.</summary>
public sealed record PurchasingContractEvaluation(
    IReadOnlyList<PurchasingContractRow> Rows,
    PurchasingContractSummary Summary,
    int OtherDocTypeItemCount,
    int OtherCompanyItemCount)
{
    public static PurchasingContractEvaluation Empty { get; } = new([], PurchasingContractSummary.Empty, 0, 0);
}

/// <summary>
/// Rechenregeln fuer den offenen Mengenkontraktwert (wie SAP ME3L).
///
/// Je Kontraktposition: offene Menge = Zielmenge (EKPO.KTMNG) minus abgerufene Menge
/// (Summe EKAB.MENGE), nie negativ. Offener Wert = offene Menge mal Nettopreis je Preiseinheit
/// (EKPO.NETPR / EKPO.PEINH), in Kontraktwaehrung; danach CHF-Bewertung mit derselben Regel wie
/// die uebrigen Einkaufskennzahlen (<c>PurchasingDashboardService.ChfValueSql</c>). Abgelaufene
/// Kontrakte (Laufzeitende EKKO.KDATE vor heute) zaehlen bewusst MIT und werden getrennt
/// ausgewiesen, weil ME3L sie ebenfalls zeigt.
/// </summary>
public static class PurchasingContractCalculator
{
    /// <summary>Belegart Mengenkontrakt. Wertkontrakte (WK) haben keine Zielmenge und zaehlen nicht mit.</summary>
    public const string QuantityContractDocType = "MK";

    /// <summary>Buchungskreis, dessen Hauswaehrung CHF ist; EKKO.WKURS rechnet in die Hauswaehrung des Buchungskreises.</summary>
    public const string ContractCompanyCode = "1100";

    public static decimal OpenQuantity(decimal targetQuantity, decimal calledQuantity)
        => Math.Max(targetQuantity - calledQuantity, 0m);

    /// <summary>Preiseinheit 0 oder leer gilt als 1 (SAP fuehrt PEINH nicht immer).</summary>
    public static decimal UnitPrice(decimal netPrice, decimal priceUnit)
        => netPrice / (priceUnit <= 0m ? 1m : priceUnit);

    /// <summary>
    /// Gleiche Regel wie <c>ChfValueSql</c>: CHF oder leere Waehrung unveraendert; positiver Kurs
    /// multipliziert; negativer Kurs teilt durch den Absolutwert; Kurs 0 bei Fremdwaehrung bleibt
    /// 1:1 und wird ueber <paramref name="rateMissing"/> gemeldet (nicht belastbar).
    /// </summary>
    public static decimal ToChf(decimal value, string? currency, decimal exchangeRate, out bool rateMissing)
    {
        rateMissing = false;
        var code = (currency ?? string.Empty).Trim();
        if (code.Length == 0 || code.Equals("CHF", StringComparison.OrdinalIgnoreCase))
            return value;
        if (exchangeRate > 0m)
            return value * exchangeRate;
        if (exchangeRate < 0m)
            return value / -exchangeRate;
        rateMissing = true;
        return value;
    }

    /// <summary>
    /// Liest eine SAP-Dezimalzahl als Text: Punkt als Dezimaltrenner, optional nachgestelltes
    /// Minus ("1.075-", ABAP-Stringkonvertierung), leer oder unlesbar ergibt 0.
    /// </summary>
    public static decimal ParseSapDecimal(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0m;
        var value = text.Trim();
        var negative = false;
        if (value.EndsWith('-'))
        {
            negative = true;
            value = value[..^1].Trim();
        }
        else if (value.StartsWith('-'))
        {
            negative = true;
            value = value[1..].Trim();
        }

        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
            return 0m;
        return negative ? -parsed : parsed;
    }

    /// <summary>
    /// Datum als <c>yyyyMMdd</c> oder <c>yyyy-MM-dd</c>. Leer und <c>00000000</c> ergeben null
    /// ("kein Datum"), damit ein leeres Laufzeitende nicht als abgelaufen gilt.
    /// </summary>
    public static DateTime? ParseSapDate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        var value = text.Trim();
        if (value.All(c => c == '0'))
            return null;
        if (DateTime.TryParseExact(value, ["yyyyMMdd", "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return parsed.Date;
        return null;
    }

    /// <summary>Abgelaufen = Laufzeitende liegt vor heute. Ohne Datum (oder 31.12.9999) nie abgelaufen.</summary>
    public static bool IsExpired(DateTime? validTo, DateTime today)
        => validTo is { } date && date.Date < today.Date;

    public static PurchasingContractRow Calculate(PurchasingContractItem item, DateTime today)
    {
        var openQuantity = OpenQuantity(item.TargetQuantity, item.CalledQuantity);
        var openValue = openQuantity * UnitPrice(item.NetPrice, item.PriceUnit);
        var chf = ToChf(openValue, item.Currency, item.ExchangeRate, out var rateMissing);
        // Ohne Menge ist der fehlende Kurs belanglos, dann nicht als Warnung zaehlen.
        if (openValue == 0m)
            rateMissing = false;

        var supplier = !string.IsNullOrWhiteSpace(item.SupplierName)
            ? item.SupplierName
            : string.IsNullOrWhiteSpace(item.Lifnr) ? "ohne Lieferant" : item.Lifnr;
        return new PurchasingContractRow(
            item.Ebeln,
            item.Ebelp,
            item.Lifnr,
            supplier,
            item.Matnr,
            item.Text,
            item.Unit,
            string.IsNullOrWhiteSpace(item.Currency) ? "CHF" : item.Currency.Trim(),
            item.TargetQuantity,
            item.CalledQuantity,
            openQuantity,
            openValue,
            chf,
            item.ValidTo,
            IsExpired(item.ValidTo, today),
            rateMissing);
    }

    /// <summary>
    /// Wertet die Kontraktpositionen aus. Eingerechnet werden nur Mengenkontrakte (Belegart MK)
    /// des Buchungskreises 1100, ohne Loeschkennzeichen. Alles andere wird nur gezaehlt, damit
    /// eine abweichende Belegart die Zahl nicht still verfaelscht.
    /// </summary>
    public static PurchasingContractEvaluation Evaluate(IEnumerable<PurchasingContractItem> items, DateTime today)
    {
        var rows = new List<PurchasingContractRow>();
        var otherType = 0;
        var otherCompany = 0;
        foreach (var item in items)
        {
            if (!string.IsNullOrWhiteSpace(item.DeletionFlag))
                continue;
            if (!string.IsNullOrWhiteSpace(item.Bukrs) &&
                !item.Bukrs.Trim().Equals(ContractCompanyCode, StringComparison.OrdinalIgnoreCase))
            {
                otherCompany++;
                continue;
            }

            if (!item.Bsart.Trim().Equals(QuantityContractDocType, StringComparison.OrdinalIgnoreCase))
            {
                otherType++;
                continue;
            }

            rows.Add(Calculate(item, today));
        }

        return new PurchasingContractEvaluation(rows, Summarize(rows), otherType, otherCompany);
    }

    public static PurchasingContractSummary Summarize(IReadOnlyCollection<PurchasingContractRow> rows)
    {
        var open = rows.Where(row => row.OpenValueChf > 0m).ToList();
        var expired = open.Where(row => row.IsExpired).ToList();
        return new PurchasingContractSummary(
            open.Sum(row => row.OpenValueChf),
            expired.Sum(row => row.OpenValueChf),
            open.Select(row => row.Ebeln).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            expired.Select(row => row.Ebeln).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            open.Count,
            expired.Count,
            rows.Count(row => row.MissingRate));
    }
}
