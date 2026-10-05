namespace TrafagSalesExporter.Services;

/// <summary>Unser Geschaeft je Land, Rohstoff und Waehrung, wie es in die Weltlage-Rechnung eingeht (alles CHF, 12 Monate).</summary>
public sealed class WorldExposure
{
    public IReadOnlyDictionary<string, decimal> SalesByCountry { get; init; } = new Dictionary<string, decimal>();
    public IReadOnlyDictionary<string, decimal> PurchaseByCountry { get; init; } = new Dictionary<string, decimal>();
    public IReadOnlyDictionary<string, decimal> OpenOrdersByCountry { get; init; } = new Dictionary<string, decimal>();
    public IReadOnlyDictionary<string, decimal> PurchaseByCommodity { get; init; } = new Dictionary<string, decimal>();
    /// <summary>Verkauf minus Einkauf je Fremdwaehrung in CHF (positiv = wir nehmen mehr ein als wir ausgeben).</summary>
    public IReadOnlyDictionary<string, decimal> NetByCurrency { get; init; } = new Dictionary<string, decimal>();
    /// <summary>Kursveraenderung der Waehrung gegen CHF in Prozent ueber 90 Tage (positiv = Waehrung staerker).</summary>
    public IReadOnlyDictionary<string, double> CurrencyChange90 { get; init; } = new Dictionary<string, double>();
    /// <summary>Standortlaender mit Umsatzanteil der Gesellschaft als Groessenmass (keine Personendaten).</summary>
    public IReadOnlyDictionary<string, decimal> SiteByCountry { get; init; } = new Dictionary<string, decimal>();
    /// <summary>
    /// True: <see cref="OpenOrdersByCountry"/> ist der offene Restwert (offene Menge aus den Einteilungen mal Stueckpreis);
    /// False: Bestellwert ganzer offener Positionen (Rueckfall, wenn keine Einteilungen im Cache sind).
    /// </summary>
    public bool OpenOrdersByQuantity { get; init; }
    public string? SalesError { get; init; }
    public string? PurchasingError { get; init; }
}

/// <summary>
/// Weltlage-Rechnung (2026-10-05, erster Wurf): Signal je Land, Rohstoff und Waehrung von -1 (schlecht fuer uns) bis +1,
/// gewichtet mit unserem Anteil. Alle Schwellen sind bewusst einfach und hier an einer Stelle, damit man sie aendern kann.
/// </summary>
public static class WorldImpactAnalytics
{
    public const string Sales = "Verkauf";
    public const string Purchasing = "Einkauf";
    public const string Finance = "Finance";
    public const string Logistics = "Logistik";
    public const string Hr = "HR";
    public static readonly string[] Departments = [Sales, Purchasing, Finance, Logistics, Hr];

    /// <summary>Stichworte im Warengruppen- oder Positionstext je Rohstoff (FRED-Reihe). Annahme, keine Stueckliste.</summary>
    public static readonly (string Series, string[] Keywords)[] CommodityKeywords =
    [
        ("PNICKUSDM", ["edelstahl", "inox", "1.4", "nickel", "membran"]),
        ("PCOPPUSDM", ["kupfer", "messing", "kabel", "litze", "copper", "brass"]),
        ("PALUMUSDM", ["alu", "aluminium", "aluminum"]),
        ("PIORECRUSDM", ["stahl", "guss", "stanz", "schraub", "mutter", "feder", "steel"]),
        ("DCOILBRENTEU", ["kunststof", "kunstst", "plast", "dichtung", "beutel", "o-ring"])
    ];

    public static string? CommodityOf(string text)
    {
        var t = (text ?? "").ToLowerInvariant();
        foreach (var (series, words) in CommodityKeywords)
            if (words.Any(w => t.Contains(w, StringComparison.Ordinal)))
                return series;
        return null;
    }

    private static double Clamp(double v) => Math.Max(-1, Math.Min(1, v));

    /// <summary>Ereignissignal: letzte 3 Tage gegen die 27 Tage davor (Ton, Konfliktanteil) plus Abzug fuer hohe Konfliktlage.</summary>
    public static (double Signal, double ConflictNow, double ToneNow, int Events)? EventSignal(IEnumerable<WorldEventDay> days, string country, DateOnly today)
    {
        var list = days.Where(d => d.Country == country).ToList();
        var now = list.Where(d => d.Day > today.AddDays(-3)).ToList();
        var baseDays = list.Where(d => d.Day <= today.AddDays(-3) && d.Day > today.AddDays(-30)).ToList();
        var eventsNow = now.Sum(d => d.Events);
        if (eventsNow < 20) return null;
        double Share(List<WorldEventDay> l) => l.Sum(d => d.Events) == 0 ? 0 : (double)l.Sum(d => d.Conflict) / l.Sum(d => d.Events);
        double Tone(List<WorldEventDay> l) => l.Sum(d => d.Events) == 0 ? 0 : l.Sum(d => d.ToneSum) / l.Sum(d => d.Events);
        var conflictNow = Share(now);
        var toneNow = Tone(now);
        var hasBase = baseDays.Sum(d => d.Events) >= 50;
        var deltaTone = hasBase ? toneNow - Tone(baseDays) : 0;
        var deltaConflict = hasBase ? conflictNow - Share(baseDays) : 0;
        var signal = deltaTone / 4 - deltaConflict * 3 - Math.Max(0, conflictNow - 0.25) * 2;
        return (Clamp(signal), conflictNow, toneNow, eventsNow);
    }

    /// <summary>Konjunktursignal: Industrieproduktion gegen Vorjahr (Eurostat) und Wachstumsprognose (IMF), Mittel der vorhandenen.</summary>
    public static (double Signal, double? IndustryYoY, double? Growth)? EconomySignal(WorldSnapshot snap, string country)
    {
        double? yoy = null;
        var ind = snap.Industry.FirstOrDefault(s => s.Key == country);
        if (ind is { Points.Count: > 12 })
        {
            var last = ind.Points[^1];
            var before = ind.Points.LastOrDefault(p => p.Date <= last.Date.AddMonths(-12));
            if (before.Value > 0) yoy = (last.Value - before.Value) / before.Value * 100;
        }
        var growth = snap.Growth.FirstOrDefault(g => g.Country == country)?.Year1;
        var parts = new List<double>();
        if (yoy is { } y) parts.Add(Clamp(y / 6));
        if (growth is { } g) parts.Add(Clamp((g - 1.5) / 3));
        return parts.Count == 0 ? null : (parts.Average(), yoy, growth);
    }

    /// <summary>Finanzstress-Index (0 = normal, ueber 1 = angespannt) als Signal -1..+1; dieselbe Skala wie alle anderen Signale.</summary>
    public static double StressSignal(double index) => Clamp(-index / 2);

    /// <summary>Kursveraenderung in Prozent gegen CHF als Signal; Einnahmen (Nettoposition positiv) leiden unter schwacher Fremdwaehrung, Ausgaben profitieren.</summary>
    public static double CurrencySignal(double changePercent, decimal net) => Clamp(changePercent / 8 * Math.Sign(net));

    /// <summary>Preisveraenderung 90 Tage; steigende Preise sind fuer den Einkauf schlecht.</summary>
    public static double? CostSignal(WorldSeries? s) => s?.ChangePercent(90) is { } c ? Clamp(-c / 20) : null;

    /// <summary>Lage eines Landes: Mittel aus Ereignis- und Konjunktursignal, mit Begruendung.</summary>
    public static (double Signal, string Detail)? CountrySignal(WorldSnapshot snap, string c, DateOnly today)
    {
        var ev = EventSignal(snap.EventDays, c, today);
        var eco = EconomySignal(snap, c);
        if (ev is null && eco is null) return null;
        var parts = new List<double>();
        var details = new List<string>();
        if (ev is { } e) { parts.Add(e.Signal); details.Add($"Ereignisse 3 Tage: {e.Events:N0}, Konfliktanteil {e.ConflictNow:P0}, Ton {e.ToneNow:0.0}"); }
        if (eco is { } k)
        {
            parts.Add(k.Signal);
            if (k.IndustryYoY is { } y) details.Add($"Industrieproduktion {y:+0.0;-0.0} % gegen Vorjahr");
            if (k.Growth is { } g) details.Add($"BIP-Prognose {g:0.0} %");
        }
        return (parts.Average(), string.Join("; ", details));
    }

    public static IReadOnlyList<WorldImpact> Compute(WorldSnapshot snap, WorldExposure exp, DateOnly today)
    {
        var result = new List<WorldImpact>();
        Dictionary<string, (double Signal, string Detail)?> countrySignal = [];
        (double Signal, string Detail)? Country(string c)
        {
            if (!countrySignal.TryGetValue(c, out var hit))
                countrySignal[c] = hit = CountrySignal(snap, c, today);
            return hit;
        }

        void ByCountry(string department, IReadOnlyDictionary<string, decimal> exposure, string topic, int take)
        {
            var total = exposure.Values.Where(v => v > 0).Sum();
            if (total <= 0) return;
            foreach (var (c, v) in exposure.Where(e => e.Value > 0).OrderByDescending(e => e.Value).Take(take))
            {
                if (Country(c) is not { } s) continue;
                var share = (double)(v / total);
                result.Add(new WorldImpact(department, topic, c, c, s.Detail, v, share, s.Signal, "GDELT, Eurostat, IMF"));
            }
        }

        ByCountry(Sales, exp.SalesByCountry, "Absatzmarkt", 15);
        ByCountry(Purchasing, exp.PurchaseByCountry, "Lieferland", 10);
        // Ohne Einteilungen im Cache nur der Bestellwert ganzer offener Positionen: so benennen, nicht als offenen Restwert ausgeben.
        ByCountry(Logistics, exp.OpenOrdersByCountry, exp.OpenOrdersByQuantity ? "Lieferweg" : "Lieferweg (Bestellwert offener Positionen)", 10);
        ByCountry(Hr, exp.SiteByCountry, "Standort", 10);

        // Einkauf: Rohstoffe ueber Stichworte in Warengruppen- und Positionstexten.
        var commodityTotal = exp.PurchaseByCommodity.Values.Sum();
        var purchaseTotal = exp.PurchaseByCountry.Values.Where(v => v > 0).Sum();
        foreach (var (key, v) in exp.PurchaseByCommodity)
        {
            var s = snap.Series.FirstOrDefault(x => x.Key == key);
            if (CostSignal(s) is not { } sig || purchaseTotal <= 0) continue;
            result.Add(new WorldImpact(Purchasing, "Rohstoff", key, s!.Name, $"Preis {s.ChangePercent(90):+0.0;-0.0} % in 90 Tagen, zuletzt {s.Last:N0} {s.Unit}", v, (double)(v / purchaseTotal), sig, "FRED"));
        }

        // Logistik: Oelpreis als Frachtkostentreiber, gewichtet mit offenen Bestellungen.
        var brent = snap.Series.FirstOrDefault(x => x.Key == "DCOILBRENTEU");
        var openTotal = exp.OpenOrdersByCountry.Values.Sum();
        if (CostSignal(brent) is { } bs && brent is not null)
            result.Add(new WorldImpact(Logistics, "Fracht", "DCOILBRENTEU", brent.Name, $"Brent {brent.ChangePercent(90):+0.0;-0.0} % in 90 Tagen, zuletzt {brent.Last:N0} {brent.Unit}", openTotal, 0.3, bs, "FRED"));

        // Finance: Waehrungen mit Nettoposition, Finanzstress.
        var netTotal = exp.NetByCurrency.Values.Sum(v => Math.Abs(v));
        foreach (var (cur, net) in exp.NetByCurrency.Where(n => n.Value != 0).OrderByDescending(n => Math.Abs(n.Value)).Take(8))
        {
            if (!exp.CurrencyChange90.TryGetValue(cur, out var change) || netTotal == 0) continue;
            var sig = CurrencySignal(change, net);
            result.Add(new WorldImpact(Finance, "Währung", cur, cur, $"{cur} {change:+0.0;-0.0} % gegen CHF in 90 Tagen, Netto {(net >= 0 ? "Einnahmen" : "Ausgaben")} {Math.Abs(net):N0} CHF", Math.Abs(net), (double)(Math.Abs(net) / netTotal), sig, "EZB"));
        }
        var stress = snap.Series.FirstOrDefault(x => x.Key == "STLFSI4");
        if (stress?.Last is { } st)
            result.Add(new WorldImpact(Finance, "Finanzmarkt", "STLFSI4", stress.Name, $"Index {st:0.00} (0 = normal, über 1 = angespannt)", 0, 0.5, StressSignal(st), "FRED"));

        return result.Where(r => !double.IsNaN(r.Signal)).ToList();
    }

    /// <summary>
    /// Kennzahl je Abteilung in Punkten (-100..+100), negativ = Gegenwind. Die Anteile gelten je Thema (Land, Rohstoff, Waehrung)
    /// und ueberlappen sich zwischen Themen; deshalb wird je Thema summiert (Anteile eines Themas ergeben hoechstens 1) und
    /// ueber die Themen der Abteilung gemittelt, statt alle Punkte zu addieren.
    /// </summary>
    public static IReadOnlyDictionary<string, double> DepartmentScores(IEnumerable<WorldImpact> impacts)
    {
        var list = impacts.ToList();
        return Departments.ToDictionary(d => d, d =>
        {
            var perTopic = list.Where(i => i.Department == d).GroupBy(i => i.Topic).Select(g => g.Sum(i => i.Score)).ToList();
            return perTopic.Count == 0 ? 0 : Math.Round(perTopic.Average(), 1);
        });
    }
}
