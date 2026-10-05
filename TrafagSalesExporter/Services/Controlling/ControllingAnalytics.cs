namespace TrafagSalesExporter.Services;

/// <summary>
/// Umsatzbruecke Vorjahr zu Vergleichszeitraum in CHF (Unterreiter Controlling, Eigeninitiative 2026-10-05).
/// Summe aller Effekte = Aktuell - Vorjahr; was keiner Regel zuzuordnen ist, steht in <see cref="Other"/>.
/// </summary>
public sealed record ControllingBridge(string Scope, decimal Previous, decimal Volume, decimal Mix, decimal Price,
    decimal NewItems, decimal LostItems, decimal Currency, decimal Other, decimal Current)
{
    public decimal Change => Current - Previous;
}

/// <summary>Hochrechnung des laufenden Kalenderjahrs je Sicht.</summary>
public sealed record ControllingProjection(string Scope, int Year, decimal Ytd, decimal PreviousYtd, decimal Rest, decimal? PreviousYear,
    double Growth, IReadOnlyList<decimal> Actual, IReadOnlyList<decimal> Forecast, IReadOnlyList<decimal> PreviousMonths)
{
    public decimal Projected => Ytd + Rest;
    public decimal? ChangePercent => PreviousYear is { } p && p != 0 ? (Projected - p) / p * 100m : null;
}

/// <summary>
/// Reine Rechnung fuer den Unterreiter Controlling, auf denselben Verkaufszeilen wie der Reiter Verkauf
/// (Finance-Regeln, ohne Konzernkunden). Doku docs/CONTROLLING_2026-10-05.md.
/// <para>Preis-, Mengen-, Mix- und Waehrungseffekt je Gesellschaft und Artikel (Artikelpreise in Lokalwaehrung):
/// Menge = Veraenderung der Stueckzahl mal Durchschnittspreis des Vorjahres; Mix = Artikelmengeneffekt minus Menge;
/// Preis = Preisveraenderung je Artikel mal aktuelle Menge zum Vorjahreskurs; Waehrung = aktueller Lokalumsatz mal
/// Kursveraenderung (Kurs je Waehrung und Zeitraum = CHF-Summe geteilt durch Lokalsumme); neue und weggefallene Artikel
/// separat; Zeilen ohne Menge (Leistungen, Gutschriften ohne Stueck) und Rundung in "Uebrige".</para>
/// </summary>
public static class ControllingAnalytics
{
    private sealed record Agg(decimal Qty, decimal Local, decimal Chf, string Currency);

    private static Dictionary<string, decimal> Rates(IEnumerable<SalesFact> facts)
        => facts.Where(f => f.ValueLocal != 0 && f.Currency.Length > 0)
            .GroupBy(f => f.Currency)
            .ToDictionary(g => g.Key, g =>
            {
                var local = g.Sum(f => f.ValueLocal);
                return local == 0 ? 0m : g.Sum(f => f.ValueChf) / local;
            });

    public static ControllingBridge Bridge(string scope, IReadOnlyList<SalesFact> facts, DateOnly refEnd, int compareMonths)
    {
        var curFrom = refEnd.AddMonths(-compareMonths);
        var prevTo = refEnd.AddMonths(-12);
        var prevFrom = prevTo.AddMonths(-compareMonths);
        var cur = facts.Where(f => f.Date >= curFrom && f.Date < refEnd).ToList();
        var prev = facts.Where(f => f.Date >= prevFrom && f.Date < prevTo).ToList();
        var fxCur = Rates(cur);
        var fxPrev = Rates(prev);

        Dictionary<string, Agg> Group(IEnumerable<SalesFact> rows) => rows
            .Where(f => f.Material.Length > 0)
            .GroupBy(f => f.Tsc + "|" + MaterialKeyNormalizer.Normalize(f.Material))
            .ToDictionary(g => g.Key, g => new Agg(g.Sum(f => f.Quantity), g.Sum(f => f.ValueLocal), g.Sum(f => f.ValueChf), g.First().Currency));
        var c = Group(cur);
        var p = Group(prev);

        decimal price = 0, newItems = 0, lost = 0, currency = 0;
        // Mengen- und Mixeffekt je Gesellschaft, weil Stueckzahlen nur innerhalb einer Gesellschaft vergleichbar sind.
        var perCompany = new Dictionary<string, (decimal QtyCur, decimal QtyPrev, decimal ValuePrev, decimal VolMat)>();
        foreach (var key in c.Keys.Union(p.Keys))
        {
            var tsc = key[..key.IndexOf('|')];
            c.TryGetValue(key, out var a);
            p.TryGetValue(key, out var b);
            var hasCur = a is { Qty: > 0, Local: > 0 };
            var hasPrev = b is { Qty: > 0, Local: > 0 };
            if (hasCur && hasPrev)
            {
                var fp = fxPrev.GetValueOrDefault(b!.Currency);
                var fc = fxCur.GetValueOrDefault(a!.Currency);
                var pricePrev = b.Local / b.Qty;
                var priceCur = a.Local / a.Qty;
                price += (priceCur - pricePrev) * a.Qty * fp;
                currency += a.Local * (fc - fp);
                var v = perCompany.GetValueOrDefault(tsc);
                perCompany[tsc] = (v.QtyCur + a.Qty, v.QtyPrev + b.Qty, v.ValuePrev + b.Local * fp, v.VolMat + (a.Qty - b.Qty) * pricePrev * fp);
            }
            else if (hasCur && !hasPrev && b is null)
            {
                var fp = fxPrev.GetValueOrDefault(a!.Currency, fxCur.GetValueOrDefault(a.Currency));
                var fc = fxCur.GetValueOrDefault(a.Currency);
                newItems += a.Local * fp;
                currency += a.Local * (fc - fp);
            }
            else if (hasPrev && !hasCur && a is null)
            {
                lost -= b!.Chf;
            }
        }
        decimal volume = 0, mix = 0;
        foreach (var (_, v) in perCompany)
        {
            var avgPrev = v.QtyPrev == 0 ? 0 : v.ValuePrev / v.QtyPrev;
            var vol = (v.QtyCur - v.QtyPrev) * avgPrev;
            volume += vol;
            mix += v.VolMat - vol;
        }
        var previousTotal = prev.Sum(f => f.ValueChf);
        var currentTotal = cur.Sum(f => f.ValueChf);
        var other = currentTotal - previousTotal - (volume + mix + price + newItems + lost + currency);
        return new ControllingBridge(scope, Round(previousTotal), Round(volume), Round(mix), Round(price), Round(newItems), Round(lost), Round(currency), Round(other), Round(currentTotal));
    }

    private static decimal Round(decimal v) => Math.Round(v, 0);

    /// <summary>
    /// Hochrechnung des Kalenderjahrs von <paramref name="refEnd"/>: Ist bis zum letzten vollstaendigen Monat plus die Restmonate
    /// aus dem gleichen Monat im Vorjahr mal Wachstum (Ist seit Jahresbeginn gegen denselben Zeitraum im Vorjahr, 0.5 bis 2).
    /// Das volle Vorjahr nur, wenn die Daten es ganz enthalten.
    /// </summary>
    public static ControllingProjection Projection(string scope, IReadOnlyList<SalesFact> facts, DateOnly refEnd, DateOnly dataStart)
    {
        var year = refEnd.AddDays(-1).Year;
        var jan = new DateOnly(year, 1, 1);
        var janPrev = jan.AddYears(-1);
        var monthly = facts.GroupBy(f => new DateOnly(f.Date.Year, f.Date.Month, 1)).ToDictionary(g => g.Key, g => g.Sum(f => f.ValueChf));
        decimal Month(DateOnly m) => monthly.GetValueOrDefault(m);
        var actual = new List<decimal>();
        var forecast = new List<decimal>();
        var previous = new List<decimal>();
        decimal ytd = 0, ytdPrev = 0;
        for (var m = jan; m < refEnd; m = m.AddMonths(1))
        {
            ytd += Month(m);
            ytdPrev += Month(m.AddYears(-1));
        }
        var growth = ytdPrev > 0 ? Math.Clamp(ytd / ytdPrev, 0.5m, 2m) : 1m;
        decimal rest = 0;
        for (var i = 0; i < 12; i++)
        {
            var m = jan.AddMonths(i);
            previous.Add(Month(m.AddYears(-1)));
            if (m < refEnd)
            {
                actual.Add(Month(m));
                forecast.Add(0);
            }
            else
            {
                actual.Add(0);
                var f = Math.Max(0, Month(m.AddYears(-1)) * growth);
                forecast.Add(f);
                rest += f;
            }
        }
        decimal? fullPrev = dataStart <= janPrev ? Enumerable.Range(0, 12).Sum(i => Month(janPrev.AddMonths(i))) : null;
        return new ControllingProjection(scope, year, Round(ytd), Round(ytdPrev), Round(rest), fullPrev is null ? null : Round(fullPrev.Value),
            (double)growth, actual, forecast, previous);
    }
}
