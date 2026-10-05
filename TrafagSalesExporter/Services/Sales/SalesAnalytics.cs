using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Reine Auswertungen fuer den Reiter Verkauf (2026-10-02), testbar ohne Daten. Zeitfenster enden immer am
/// <see cref="SalesDataset.ReferenceEnd"/> (letzter vollstaendiger Monat), damit ein angebrochener Monat nichts verzerrt.
/// </summary>
public static class SalesAnalytics
{
    private static readonly Regex LegalSuffix = new(
        @"\b(GMBH|AG|SPA|S P A|SRL|S R L|LTD|LIMITED|INC|SA|SAS|SL|S L|SLU|BV|NV|OY|AB|AS|KG|CO|CORP|PVT|PRIVATE|LLC|PLC|SRO|S R O|SP Z O O|ZOO)\b",
        RegexOptions.Compiled);

    /// <summary>Gruppenweiter Kundenschluessel: Name ohne Rechtsform, Satzzeichen und Akzente; ohne Name TSC und Nummer.</summary>
    public static string CustomerKey(string? name, string? tsc, string? number)
    {
        var text = (name ?? "").Trim();
        if (text.Length == 0)
        {
            // Ohne Name und ohne Nummer wuerde "TSC#" fuer alle diese Zeilen einen leer wirkenden Schluessel ergeben:
            // erkennbarer Sammelschluessel je Gesellschaft.
            var num = (number ?? "").Trim();
            return $"{(tsc ?? "").Trim().ToUpperInvariant()}#{(num.Length == 0 ? "(ohne Nummer)" : num)}";
        }
        var formD = text.ToUpperInvariant().Replace("&", " AND ").Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(char.IsLetterOrDigit(ch) ? ch : ' ');
        }
        var cleaned = LegalSuffix.Replace(sb.ToString(), " ");
        cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
        return cleaned.Length == 0 ? text.ToUpperInvariant() : cleaned;
    }

    /// <summary>Anzeigename; Kunden ohne Namen in der Quelle erscheinen mit ihrem Schluessel (Gesellschaft#Nummer).</summary>
    public static string DisplayName(string name, string key) => string.IsNullOrWhiteSpace(name) ? key : name;

    /// <summary>Letzter Werktag (Mo-Fr) des Monats, in dem <paramref name="day"/> liegt.</summary>
    public static DateOnly LastBusinessDayOfMonth(DateOnly day)
    {
        var last = new DateOnly(day.Year, day.Month, 1).AddMonths(1).AddDays(-1);
        while (last.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            last = last.AddDays(-1);
        return last;
    }

    public static DateOnly ReferenceEnd(IEnumerable<SalesFact> facts, DateOnly today)
    {
        var max = facts.Select(f => f.Date).Where(d => d <= today).DefaultIfEmpty(today).Max();
        var firstOfMonth = new DateOnly(max.Year, max.Month, 1);
        // Vollstaendig, wenn der juengste Beleg den letzten Werktag des Monats erreicht (das Monatsende kann ein Wochenende sein).
        // Spaetere Monate gibt es nicht, denn max ist das juengste Datum.
        return max >= LastBusinessDayOfMonth(max) ? firstOfMonth.AddMonths(1) : firstOfMonth;
    }

    /// <summary>Kundenland als ISO-2 in der Schreibweise der Weltkarte: UK wird GB, EL wird GR.</summary>
    public static string NormalizeCountry(string? country)
    {
        var c = (country ?? "").Trim().ToUpperInvariant();
        return c switch { "UK" => "GB", "EL" => "GR", _ => c };
    }

    private static decimal Sum(IEnumerable<SalesFact> facts, DateOnly from, DateOnly to)
        => facts.Where(f => f.Date >= from && f.Date < to).Sum(f => f.ValueChf);

    /// <summary>Spaetester Datenbeginn (Monatsanfang) aller Gesellschaften; davor fehlen Daten mindestens einer Gesellschaft.</summary>
    public static DateOnly DataStart(IEnumerable<SalesFact> facts)
    {
        var starts = facts.GroupBy(f => f.Tsc).Select(g => g.Min(f => f.Date)).ToList();
        var latest = starts.Count == 0 ? DateOnly.MinValue : starts.Max();
        // Beginnt die juengste Gesellschaft erst nach dem 7. des Monats, ist dieser Monat angebrochen: erster voller Monat danach.
        var first = new DateOnly(latest.Year, latest.Month, 1);
        return latest.Day > 7 ? first.AddMonths(1) : first;
    }

    /// <summary>Monate fuer den Vorjahresvergleich: hoechstens 12, und der Vorjahreszeitraum muss ganz in den Daten liegen.</summary>
    public static int CompareMonths(DateOnly dataStart, DateOnly refEnd)
    {
        var available = (refEnd.Year - dataStart.Year) * 12 + refEnd.Month - dataStart.Month;
        return Math.Clamp(available - 12, 1, 12);
    }

    public static IReadOnlyList<SalesCustomerSummary> Customers(IReadOnlyList<SalesFact> facts, DateOnly refEnd, int compareMonths = 12)
    {
        var l12 = refEnd.AddMonths(-12);
        var cur = refEnd.AddMonths(-compareMonths);
        var prevEnd = refEnd.AddMonths(-12);
        var prevStart = prevEnd.AddMonths(-compareMonths);
        var l6 = refEnd.AddMonths(-6);
        var p24 = refEnd.AddMonths(-24);
        return facts.Where(f => f.Date < refEnd)
            .GroupBy(f => f.CustomerKey)
            .Select(g =>
            {
                var name = DisplayName(g.GroupBy(f => f.CustomerName).OrderByDescending(n => n.Sum(f => f.ValueChf)).First().Key, g.Key);
                var country = g.Where(f => f.CustomerCountry.Length > 0).GroupBy(f => f.CustomerCountry)
                    .OrderByDescending(c => c.Sum(f => f.ValueChf)).Select(c => c.Key).FirstOrDefault() ?? "";
                return new SalesCustomerSummary(g.Key, name, country,
                    g.Select(f => f.Tsc).Distinct().OrderBy(t => t).ToList(),
                    Sum(g, l12, refEnd), Sum(g, cur, refEnd), Sum(g, prevStart, prevEnd), Sum(g, l6, refEnd),
                    PurchaseDates(g).Min, PurchaseDates(g).Max,
                    g.Select(f => f.Tsc + "|" + f.InvoiceNumber).Distinct().Count(),
                    g.Where(f => f.Date >= p24).GroupBy(f => f.Division).Select(d => (d.Key, d.Sum(f => f.ValueChf)))
                        .Where(d => d.Item2 > 0).OrderByDescending(d => d.Item2).ToList());
            })
            .OrderByDescending(c => c.Last12)
            .ToList();
    }

    /// <summary>Erster und letzter Kauf nur aus Zeilen mit positivem Umsatz; Gutschriften sind kein Kauf. Nur Gutschriften: alle Zeilen.</summary>
    private static (DateOnly Min, DateOnly Max) PurchaseDates(IEnumerable<SalesFact> rows)
    {
        var list = rows.ToList();
        var pos = list.Where(f => f.ValueChf > 0).Select(f => f.Date).ToList();
        var dates = pos.Count > 0 ? pos : list.Select(f => f.Date).ToList();
        return (dates.Min(), dates.Max());
    }

    public static IReadOnlyList<SalesCompanyChange> CompanyChanges(IReadOnlyList<SalesFact> facts, DateOnly refEnd, int compareMonths)
        => facts.GroupBy(f => f.Tsc)
            .Select(g => new SalesCompanyChange(g.Key, Sum(g, refEnd.AddMonths(-compareMonths), refEnd), Sum(g, refEnd.AddMonths(-12 - compareMonths), refEnd.AddMonths(-12))))
            .OrderByDescending(c => c.Current)
            .ToList();

    /// <summary>Kunden mit Rueckgang im Vergleichszeitraum: Vorjahr mindestens <paramref name="minPrevious"/> CHF und Rueckgang ab 30 %, oder seit 6 Monaten kein Umsatz.</summary>
    public static IReadOnlyList<SalesDeclineItem> Declines(IReadOnlyList<SalesCustomerSummary> customers, decimal minPrevious = 10000m, decimal dropPercent = 30m)
        => customers.Where(c => c.Previous >= minPrevious)
            .Select(c => c.Last6 <= 0 && c.Current < c.Previous ? new SalesDeclineItem(c, "eingeschlafen", c.Previous - Math.Max(0, c.Current))
                : c.ChangePercent <= -dropPercent ? new SalesDeclineItem(c, "rueckgang", c.Previous - c.Current) : null)
            .Where(d => d is not null)
            .Select(d => d!)
            .OrderByDescending(d => d.LostChf)
            .ToList();

    private static DateOnly QuarterStart(DateOnly d) => new(d.Year, (d.Month - 1) / 3 * 3 + 1, 1);

    private static string QuarterLabel(DateOnly q) => $"{q.Year} Q{(q.Month - 1) / 3 + 1}";

    /// <summary>
    /// Neue und verlorene Kunden je Quartal (die letzten <paramref name="quarters"/> vollstaendigen Quartale). Verloren heisst:
    /// letzter Kauf im Quartal und seither nichts mehr; endgueltig erst, wenn danach mindestens 12 Monate vergangen sind.
    /// </summary>
    public static IReadOnlyList<SalesQuarterMovement> Movements(IReadOnlyList<SalesFact> facts, DateOnly refEnd, int quarters = 12, DateOnly? dataStart = null)
    {
        // Ende des letzten vollstaendigen Quartals (exklusiv).
        var lastQuarterEnd = QuarterStart(refEnd);
        var byCustomer = facts.Where(f => f.Date < refEnd).GroupBy(f => f.CustomerKey)
            .Where(g => g.Any(f => f.ValueChf > 0))
            .Select(g => (Key: g.Key, Name: DisplayName(g.First().CustomerName, g.Key), First: PurchaseDates(g).Min, Last: PurchaseDates(g).Max, Facts: g.ToList()))
            .ToList();
        var firstDate = facts.Select(f => f.Date).DefaultIfEmpty(refEnd).Min();
        // "Neu" ist erst belastbar, wenn davor mindestens 12 Monate Daten liegen; sonst ist jeder Kunde scheinbar neu.
        var reliableFrom = (dataStart ?? new DateOnly(firstDate.Year, firstDate.Month, 1)).AddMonths(12);
        var result = new List<SalesQuarterMovement>();
        for (var q = lastQuarterEnd.AddMonths(-3 * quarters); q < lastQuarterEnd; q = q.AddMonths(3))
        {
            var end = q.AddMonths(3);
            if (q < QuarterStart(firstDate)) continue;
            var news = byCustomer.Where(c => c.First >= q && c.First < end).ToList();
            // Verloren erst ab 6 Monaten nach Quartalsende zeigen; vorher hat fast jeder Kunde einfach noch nicht wieder bestellt.
            var lostKnown = end.AddMonths(6) <= refEnd;
            var lost = lostKnown ? byCustomer.Where(c => c.Last >= q && c.Last < end).ToList() : [];
            result.Add(new SalesQuarterMovement(QuarterLabel(q),
                news.Count, news.Sum(c => c.Facts.Where(f => f.Date < c.First.AddMonths(12)).Sum(f => f.ValueChf)),
                lost.Count, lost.Sum(c => c.Facts.Where(f => f.Date >= c.Last.AddMonths(-12)).Sum(f => f.ValueChf)),
                end.AddMonths(12) <= refEnd, q >= reliableFrom, lostKnown,
                news.OrderByDescending(c => c.Facts.Sum(f => f.ValueChf)).Take(8).Select(c => c.Name).ToList(),
                lost.OrderByDescending(c => c.Facts.Sum(f => f.ValueChf)).Take(8).Select(c => c.Name).ToList(),
                // Umsatz der ersten 12 Monate ist nur vollstaendig, wenn auch der letzte Neukunde des Quartals 12 Monate im Datenbestand hat.
                end.AddMonths(12) <= refEnd));
        }
        return result;
    }

    public static SalesConcentrationResult Concentration(IReadOnlyList<SalesFact> facts, DateOnly refEnd)
    {
        var from = refEnd.AddMonths(-12);
        var window = facts.Where(f => f.Date >= from && f.Date < refEnd).ToList();
        var values = window.GroupBy(f => f.CustomerKey).Select(g => g.Sum(f => f.ValueChf)).Where(v => v > 0).OrderByDescending(v => v).ToList();
        var total = values.Sum();
        double Share(int n) => total <= 0 ? 0 : (double)(values.Take(n).Sum() / total);
        var curve = new List<(int, double)>();
        decimal running = 0;
        var for80 = 0;
        for (var i = 0; i < values.Count; i++)
        {
            running += values[i];
            var s = total <= 0 ? 0 : (double)(running / total);
            if (for80 == 0 && s >= 0.8) for80 = i + 1;
            if (i < 200 || i % Math.Max(1, values.Count / 200) == 0 || i == values.Count - 1)
                curve.Add((i + 1, s));
        }
        // Ohne positiven Umsatz (total = 0) gibt es keine Kunden in der Kurve: CustomersFor80 bleibt 0, alle Anteile 0.
        var hhi = total <= 0 ? 0 : values.Sum(v => Math.Pow((double)(v / total) * 100, 2));
        var perCompany = window.GroupBy(f => f.Tsc)
            .Select(g =>
            {
                var vals = g.GroupBy(f => f.CustomerKey).Select(c => c.Sum(f => f.ValueChf)).Where(v => v > 0).OrderByDescending(v => v).ToList();
                var t = vals.Sum();
                return (g.Key, t == 0 ? 0 : (double)(vals.Take(10).Sum() / t), vals.Count);
            })
            .OrderByDescending(x => x.Item2)
            .ToList();
        return new SalesConcentrationResult(curve, Share(1), Share(5), Share(10), Share(20), for80, hhi, total, values.Count, perCompany);
    }

    private static bool UsableDivision(string d) => d.Length > 0 && !d.Equals("Nicht zugeordnet", StringComparison.OrdinalIgnoreCase)
        && !d.Equals("Others", StringComparison.OrdinalIgnoreCase)
        && !d.Equals("Other", StringComparison.OrdinalIgnoreCase) && !d.Equals("Übrige", StringComparison.OrdinalIgnoreCase)
        && !d.All(char.IsDigit);

    /// <summary>Regeln "wer A kauft, kauft auch B" ueber 24 Monate, ab <paramref name="minBoth"/> gemeinsamen Kunden.</summary>
    public static IReadOnlyList<SalesCrossSellRule> CrossSellRules(IReadOnlyList<SalesFact> facts, DateOnly refEnd, int minBoth = 5)
    {
        var baskets = Baskets(facts, refEnd);
        var customers = Math.Max(1, baskets.Count);
        var count = baskets.SelectMany(b => b.Value).GroupBy(d => d).ToDictionary(g => g.Key, g => g.Count());
        var rules = new List<SalesCrossSellRule>();
        foreach (var a in count.Keys)
        foreach (var b in count.Keys.Where(b => b != a))
        {
            var both = baskets.Count(x => x.Value.Contains(a) && x.Value.Contains(b));
            if (both < minBoth) continue;
            var confidence = (double)both / count[a];
            rules.Add(new SalesCrossSellRule(a, b, both, count[a], confidence, confidence / ((double)count[b] / customers)));
        }
        return rules.OrderByDescending(r => r.Confidence).ThenByDescending(r => r.Both).ToList();
    }

    private static Dictionary<string, HashSet<string>> Baskets(IReadOnlyList<SalesFact> facts, DateOnly refEnd)
        => facts.Where(f => f.Date >= refEnd.AddMonths(-24) && f.Date < refEnd && f.ValueChf > 0 && UsableDivision(f.Division))
            .GroupBy(f => f.CustomerKey)
            .ToDictionary(g => g.Key, g => g.Select(f => f.Division).ToHashSet());

    /// <summary>Ansaetze je Kunde: Sparte B fehlt, obwohl Kunden mit seiner Sparte A sie oft kaufen (Konfidenz ab 40 %, Lift ueber 1).</summary>
    public static IReadOnlyList<SalesCrossSellOpportunity> CrossSellOpportunities(IReadOnlyList<SalesFact> facts, DateOnly refEnd, double minConfidence = 0.4)
    {
        var rules = CrossSellRules(facts, refEnd).Where(r => r.Confidence >= minConfidence && r.Lift > 1).ToList();
        var baskets = Baskets(facts, refEnd);
        var window = facts.Where(f => f.Date >= refEnd.AddMonths(-24) && f.Date < refEnd).ToList();
        var revenue = window.GroupBy(f => f.CustomerKey).ToDictionary(g => g.Key, g => (Name: g.First().CustomerName, Value: g.Sum(f => f.ValueChf)));
        var typical = window.Where(f => f.ValueChf > 0).GroupBy(f => f.Division)
            .ToDictionary(g => g.Key, g => Median(g.GroupBy(f => f.CustomerKey).Select(c => c.Sum(f => f.ValueChf)).ToList()));
        var result = new List<SalesCrossSellOpportunity>();
        foreach (var (customer, divisions) in baskets)
        {
            var best = rules.Where(r => divisions.Contains(r.From) && !divisions.Contains(r.To))
                .GroupBy(r => r.To).Select(g => g.OrderByDescending(r => r.Confidence).First());
            foreach (var r in best)
                result.Add(new SalesCrossSellOpportunity(customer, DisplayName(revenue[customer].Name, customer), r.To, r.From, r.Confidence, revenue[customer].Value, typical.GetValueOrDefault(r.To)));
        }
        return result.OrderByDescending(o => (double)o.CustomerRevenue * o.Confidence).ToList();
    }

    private static decimal Median(List<decimal> values)
    {
        if (values.Count == 0) return 0;
        values.Sort();
        return values.Count % 2 == 1 ? values[values.Count / 2] : (values[values.Count / 2 - 1] + values[values.Count / 2]) / 2;
    }

    private static decimal Percentile(IReadOnlyList<decimal> sorted, double p)
    {
        if (sorted.Count == 0) return 0;
        var pos = p * (sorted.Count - 1);
        var lo = (int)Math.Floor(pos);
        var hi = (int)Math.Ceiling(pos);
        return sorted[lo] + (sorted[hi] - sorted[lo]) * (decimal)(pos - lo);
    }

    /// <summary>Stueckpreis in CHF je Artikel und Kunde (12 Monate, nur positive Menge und Wert), ab <paramref name="minCustomers"/> Kunden.</summary>
    private static readonly Regex PlaceholderMaterial = new(@"9{4,}", RegexOptions.Compiled);
    private static readonly Regex ServiceArticle = new(@"CERTIF|ZERTIFIKAT|LAVORAZION|MANUFACTURING|INSPECTION|PRUEF|PRÜF|FREIGHT|FRACHT|VERSAND|PORTO|SHIPPING|POSTAGE|TRANSPORT|VERPACKUNG|PACKING|SERVICE|DIENSTLEIST|SPESE|SURCHARGE|ZUSCHLAG",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>Platzhalter-Materialnummern (viele Neunen) und Leistungen (Zertifikate, Bearbeitung, Fracht ...) haben keinen vergleichbaren Stueckpreis.</summary>
    public static bool IsComparableArticle(string material, string article)
        => !PlaceholderMaterial.IsMatch(material) && !ServiceArticle.IsMatch(article);

    public static IReadOnlyList<SalesPriceSpread> PriceSpreads(IReadOnlyList<SalesFact> facts, DateOnly refEnd, int minCustomers = 5)
        => facts.Where(f => f.Date >= refEnd.AddMonths(-12) && f.Date < refEnd && f.Quantity > 0 && f.ValueChf > 0 && f.Material.Length > 0
                            && IsComparableArticle(f.Material, f.Article))
            .GroupBy(f => MaterialKeyNormalizer.Normalize(f.Material))
            .Select(g =>
            {
                var points = g.GroupBy(f => f.CustomerKey)
                    .Select(c => new SalesPricePoint(DisplayName(c.First().CustomerName, c.Key), c.First().CustomerCountry, c.First().Tsc, c.Sum(f => f.Quantity), c.Sum(f => f.ValueChf) / c.Sum(f => f.Quantity)))
                    .OrderBy(p => p.UnitPriceChf).ToList();
                var prices = points.Select(p => p.UnitPriceChf).ToList();
                return new SalesPriceSpread(g.First().Material, g.GroupBy(f => f.Article).OrderByDescending(a => a.Count()).First().Key,
                    points.Count, g.Sum(f => f.Quantity), g.Sum(f => f.ValueChf),
                    Percentile(prices, 0.1), Percentile(prices, 0.5), Percentile(prices, 0.9),
                    prices.FirstOrDefault(), prices.LastOrDefault(), points);
            })
            .Where(s => s.Customers >= minCustomers && s.P10 > 0)
            .OrderByDescending(s => (double)s.Ratio * Math.Log10((double)s.Revenue + 10))
            .ToList();

    public static IReadOnlyList<SalesMonthValue> Monthly(IEnumerable<SalesFact> facts, DateOnly from, DateOnly to)
    {
        var sums = facts.Where(f => f.Date >= from && f.Date < to)
            .GroupBy(f => new DateOnly(f.Date.Year, f.Date.Month, 1)).ToDictionary(g => g.Key, g => g.Sum(f => f.ValueChf));
        var list = new List<SalesMonthValue>();
        for (var m = new DateOnly(from.Year, from.Month, 1); m < to; m = m.AddMonths(1))
            list.Add(new SalesMonthValue(m, sums.GetValueOrDefault(m)));
        return list;
    }

    /// <summary>
    /// Saisonindex (Monatsanteil am Jahresmittel aus bis zu drei vollen Jahren) und Prognose der naechsten 12 Monate:
    /// gleicher Monat im Vorjahr mal Wachstum (letzte 12 Monate zu den 12 davor). Rueckrechnung des Verfahrens auf das
    /// letzte Jahr ergibt die mittlere Abweichung (MAPE) als Guete.
    /// </summary>
    /// <remarks>Das Wachstum ist auf 0.5 bis 2 begrenzt, und zwar je Aufruf: die Summe der Prognosen je Gesellschaft oder Sparte
    /// ist deshalb nicht gleich der Prognose des Gesamts.</remarks>
    public static SalesForecast Forecast(string dimension, IReadOnlyList<SalesFact> facts, DateOnly refEnd, DateOnly? dataStart = null, int compareMonths = 12)
    {
        var history = Monthly(facts, refEnd.AddMonths(-36), refEnd);
        if (dataStart is { } ds && ds > refEnd.AddMonths(-36))
        {
            // Monate vor dem Datenbeginn sind keine Nullumsaetze, sondern fehlende Daten: nicht anzeigen, nicht verwenden.
            history = history.Where(v => v.Month >= ds).ToList();
        }
        var index = new double[12];
        var years = Enumerable.Range(0, 3).Select(y => history.SkipLast(y * 12).TakeLast(12).ToList()).Where(y => y.Count == 12 && y.Sum(v => v.Value) > 0)
            .GroupBy(y => y[0].Month).Select(g => g.First()).ToList();
        for (var m = 0; m < 12; m++)
        {
            var ratios = years.Select(y =>
            {
                var avg = y.Average(v => v.Value);
                var hit = y.FirstOrDefault(v => v.Month.Month == m + 1);
                return avg == 0 || hit is null ? 1.0 : (double)(hit.Value / avg);
            }).ToList();
            index[m] = ratios.Count == 0 ? 1 : ratios.Average();
        }
        decimal Window(int monthsBackEnd, int length) => history.Where(v => v.Month >= refEnd.AddMonths(-monthsBackEnd - length) && v.Month < refEnd.AddMonths(-monthsBackEnd)).Sum(v => v.Value);
        var last12 = Window(0, 12);
        var current = Window(0, compareMonths);
        var previous = Window(12, compareMonths);
        var growth = previous > 0 ? current / previous : 1m;
        growth = Math.Clamp(growth, 0.5m, 2m);
        var lastYear = history.Where(v => v.Month >= refEnd.AddMonths(-12)).ToList();
        var forecast = lastYear.Select(v => new SalesMonthValue(v.Month.AddMonths(12), Math.Max(0, v.Value * growth))).ToList();
        double? mape = null;
        // Rueckrechnung braucht drei volle Jahre: Wachstum aus Jahr 1 zu 2 auf Jahr 2 angewendet, mit Jahr 3 verglichen.
        if (history.Count >= 36 && Window(24, 12) > 0 && Window(12, 12) > 0)
        {
            var g = Math.Clamp(Window(12, 12) / Window(24, 12), 0.5m, 2m);
            var errors = lastYear.Select(v => (actual: v.Value, predicted: (history.FirstOrDefault(h => h.Month == v.Month.AddMonths(-12))?.Value ?? 0) * g))
                .Where(x => x.actual > 0).Select(x => Math.Abs((double)((x.actual - x.predicted) / x.actual))).ToList();
            mape = errors.Count == 0 ? null : errors.Average();
        }
        return new SalesForecast(dimension, history, forecast, index, mape, current, previous);
    }

    public static IReadOnlyList<SalesCountryValue> Countries(IReadOnlyList<SalesFact> facts, DateOnly refEnd, int months = 24, DateOnly? dataStart = null)
    {
        var from = refEnd.AddMonths(-months);
        if (dataStart is { } ds && ds > from) from = ds;
        var l12 = refEnd.AddMonths(-12);
        return facts.Where(f => f.Date >= from && f.Date < refEnd && f.CustomerCountry.Length == 2)
            .GroupBy(f => f.CustomerCountry)
            .Select(g => new SalesCountryValue(g.Key, g.Where(f => f.Date >= l12).Sum(f => f.ValueChf),
                g.Where(f => f.Date >= l12).Select(f => f.CustomerKey).Distinct().Count(),
                Monthly(g, from, refEnd).Select(v => v.Value).ToList()))
            .Where(c => c.Last12 > 0)
            .OrderByDescending(c => c.Last12)
            .ToList();
    }

    /// <summary>Warenfluss: Land der verkaufenden Gesellschaft zu Kundenland, 12 Monate.</summary>
    public static IReadOnlyList<SalesFlow> Flows(IReadOnlyList<SalesFact> facts, DateOnly refEnd)
        => facts.Where(f => f.Date >= refEnd.AddMonths(-12) && f.Date < refEnd && f.CustomerCountry.Length == 2)
            .GroupBy(f => (From: CompanyCountry(f.CountryKey), To: f.CustomerCountry))
            .Select(g => new SalesFlow(g.Key.From, g.Key.To, g.Sum(f => f.ValueChf)))
            .Where(f => f.Last12 > 0 && f.FromCountry != f.ToCountry)
            .OrderByDescending(f => f.Last12)
            .ToList();

    /// <summary>Finance-Laenderschluessel der Gesellschaft als ISO-2 (UK -> GB).</summary>
    public static string CompanyCountry(string countryKey) => countryKey.ToUpperInvariant() switch
    {
        "UK" => "GB",
        var k => k
    };
}
