namespace TrafagSalesExporter.Services;

// Reiter Verkauf, Unterreiter Interaktiv (Wunsch Ingo 2026-10-05): neun animierte Ansichten auf denselben Verkaufszeilen
// wie die übrigen Unterreiter (Finance-Regeln, ohne Konzernkunden, CHF). Nur Rechnung, keine Darstellung.
// Doku docs/VERKAUF_2026-10-02.md, Abschnitt „Interaktiv“.

/// <summary>Growth ist null ohne belastbares Vorquartal (erstes Quartal, Vorquartal ohne Umsatz oder angebrochen) und im angebrochenen Quartal.</summary>
public sealed record GalaxyPoint(string Key, string Name, string Country, decimal Revenue, double? Growth, int Invoices);
/// <summary>Partial: Quartal ist noch nicht vollstaendig (Referenzende liegt mitten darin); Umsatz und Wachstum sind dann nur vorlaeufig.</summary>
public sealed record GalaxyFrame(DateOnly Quarter, IReadOnlyList<GalaxyPoint> Points, bool Partial = false);

public sealed record RaceBar(string Key, string Name, decimal Value);
public sealed record RaceFrame(DateOnly Month, IReadOnlyList<RaceBar> Bars);

public sealed record SankeyNode(string Id, string Label, int Column, decimal Value);
public sealed record SankeyLink(string From, string To, decimal Value);

public sealed record SunburstNode(string Id, string Label, decimal Value, IReadOnlyList<SunburstNode> Children);

public sealed record SimulatorBase(int Year, decimal Ytd, decimal TotalRest, decimal? PreviousYear,
    IReadOnlyList<(string Division, decimal Remaining, decimal ForeignShare)> Divisions);
public sealed record SimulatorLever(string Division, double PricePercent, double VolumePercent);
public sealed record SimulatorResult(decimal Ytd, decimal BaseRest, decimal SimRest, decimal FxEffect,
    IReadOnlyList<(string Division, decimal BaseRest, decimal SimRest)> Divisions)
{
    public decimal BaseYear => Ytd + BaseRest;
    public decimal SimYear => Ytd + SimRest + FxEffect;
}

public sealed record RhythmCustomer(string Key, string Name, string Country, IReadOnlyList<DateOnly> Days, double MedianDays,
    DateOnly Last, DateOnly Expected, int OverdueDays, decimal Last12)
{
    /// <summary>Wie viele übliche Abstände der Kunde überfällig ist (0 = im Rhythmus).</summary>
    public double OverdueRatio => MedianDays > 0 ? Math.Max(0, OverdueDays) / MedianDays : 0;
}

public sealed record CalendarDay(DateOnly Day, decimal Value, int Invoices);

public sealed record NetworkNode(string Id, string Label, int Customers, decimal Revenue, double X, double Y, int Group);
public sealed record NetworkEdge(string A, string B, int Both, double Weight);

public sealed record LandscapeCell(DateOnly Month, string Division, decimal Value);

public static class SalesInteractive
{
    private static DateOnly QuarterStart(DateOnly d) => new(d.Year, (d.Month - 1) / 3 * 3 + 1, 1);
    private static DateOnly MonthStart(DateOnly d) => new(d.Year, d.Month, 1);

    /// <summary>
    /// Kunden-Galaxie: je Quartal ab Datenbeginn Umsatz, Veränderung zum Vorquartal in Prozent (begrenzt auf −100..300,
    /// null ohne vollstaendiges Vorquartal mit Umsatz und im angebrochenen Quartal) und Rechnungen; nur die <paramref name="top"/> Kunden der letzten 12 Monate, damit die Animation lesbar bleibt.
    /// </summary>
    public static IReadOnlyList<GalaxyFrame> Galaxy(IReadOnlyList<SalesFact> facts, DateOnly refEnd, DateOnly dataStart, int top = 60)
    {
        var last12 = facts.Where(f => f.Date >= refEnd.AddMonths(-12) && f.Date < refEnd);
        var keys = last12.GroupBy(f => f.CustomerKey).OrderByDescending(g => g.Sum(f => f.ValueChf)).Take(top)
            .ToDictionary(g => g.Key, g => (Name: SalesAnalytics.DisplayName(g.First().CustomerName, g.Key), Country: g.GroupBy(f => f.CustomerCountry).OrderByDescending(x => x.Sum(f => f.ValueChf)).First().Key));
        var byQuarter = facts.Where(f => keys.ContainsKey(f.CustomerKey) && f.Date >= dataStart && f.Date < refEnd)
            .GroupBy(f => (Q: QuarterStart(f.Date), f.CustomerKey))
            .ToDictionary(g => g.Key, g => (Value: g.Sum(f => f.ValueChf), Invoices: g.Select(f => f.InvoiceNumber).Distinct().Count()));
        var frames = new List<GalaxyFrame>();
        for (var q = QuarterStart(dataStart); q < refEnd; q = q.AddMonths(3))
        {
            // Angebrochenes Quartal (Referenzende mitten darin) und Vorquartal vor dem Datenbeginn sind nicht vergleichbar.
            var partial = q.AddMonths(3) > refEnd;
            var prevComplete = q.AddMonths(-3) >= dataStart;
            var points = keys.Select(k =>
            {
                var cur = byQuarter.GetValueOrDefault((q, k.Key));
                var prev = byQuarter.GetValueOrDefault((q.AddMonths(-3), k.Key));
                double? growth = !partial && prevComplete && prev.Value > 0
                    ? Math.Clamp((double)((cur.Value - prev.Value) / prev.Value * 100), -100, 300)
                    : null;
                return new GalaxyPoint(k.Key, k.Value.Name, k.Value.Country, cur.Value, growth, cur.Invoices);
            }).ToList();
            frames.Add(new GalaxyFrame(q, points, partial));
        }
        return frames;
    }

    /// <summary>Bar Chart Race: je Monat die Top 15 nach gleitender 3-Monats-Summe, Dimension Kunde, Artikel oder Land.</summary>
    public static IReadOnlyList<RaceFrame> Race(IReadOnlyList<SalesFact> facts, string dimension, DateOnly refEnd, DateOnly dataStart, int top = 15)
    {
        Func<SalesFact, string> key = dimension switch
        {
            "article" => f => f.Article.Length > 0 ? f.Article : f.Material,
            "country" => f => f.CustomerCountry.Length > 0 ? f.CustomerCountry : "–",
            _ => f => f.CustomerKey
        };
        var names = facts.GroupBy(key).ToDictionary(g => g.Key, g => dimension == "customer" ? SalesAnalytics.DisplayName(g.First().CustomerName, g.Key) : g.Key);
        var monthly = facts.Where(f => f.Date >= dataStart && f.Date < refEnd)
            .GroupBy(f => (M: MonthStart(f.Date), K: key(f))).ToDictionary(g => g.Key, g => g.Sum(f => f.ValueChf));
        var allKeys = monthly.Keys.Select(k => k.K).Distinct().ToList();
        var frames = new List<RaceFrame>();
        for (var m = MonthStart(dataStart).AddMonths(2); m < refEnd; m = m.AddMonths(1))
        {
            var mm = m;
            var bars = allKeys.Select(k => new RaceBar(k, names.GetValueOrDefault(k, k),
                    monthly.GetValueOrDefault((mm, k)) + monthly.GetValueOrDefault((mm.AddMonths(-1), k)) + monthly.GetValueOrDefault((mm.AddMonths(-2), k))))
                .Where(b => b.Value > 0).OrderByDescending(b => b.Value).Take(top).ToList();
            frames.Add(new RaceFrame(m, bars));
        }
        return frames;
    }

    /// <summary>Umsatzfluss 12 Monate: Gesellschaft → Sparte → Kundenland (Top 12 Länder, Rest „Übrige“).</summary>
    public static (IReadOnlyList<SankeyNode> Nodes, IReadOnlyList<SankeyLink> Links) Sankey(IReadOnlyList<SalesFact> facts, DateOnly refEnd, int topCountries = 12)
    {
        var rows = facts.Where(f => f.Date >= refEnd.AddMonths(-12) && f.Date < refEnd && f.ValueChf > 0).ToList();
        var countries = rows.GroupBy(f => f.CustomerCountry).OrderByDescending(g => g.Sum(f => f.ValueChf)).Take(topCountries).Select(g => g.Key).ToHashSet();
        string C(SalesFact f) => countries.Contains(f.CustomerCountry) ? (f.CustomerCountry.Length > 0 ? f.CustomerCountry : "–") : "Übrige";
        string D(SalesFact f) => f.Division.Length > 0 ? f.Division : "Nicht zugeordnet";
        var links = rows.GroupBy(f => ("t:" + f.Tsc, "d:" + D(f))).Select(g => new SankeyLink(g.Key.Item1, g.Key.Item2, g.Sum(f => f.ValueChf)))
            .Concat(rows.GroupBy(f => ("d:" + D(f), "c:" + C(f))).Select(g => new SankeyLink(g.Key.Item1, g.Key.Item2, g.Sum(f => f.ValueChf))))
            .Where(l => l.Value > 0).ToList();
        var nodes = rows.GroupBy(f => f.Tsc).Select(g => new SankeyNode("t:" + g.Key, g.Key, 0, g.Sum(f => f.ValueChf)))
            .Concat(rows.GroupBy(D).Select(g => new SankeyNode("d:" + g.Key, g.Key, 1, g.Sum(f => f.ValueChf))))
            .Concat(rows.GroupBy(C).Select(g => new SankeyNode("c:" + g.Key, g.Key, 2, g.Sum(f => f.ValueChf))))
            .OrderBy(n => n.Column).ThenByDescending(n => n.Value).ToList();
        return (nodes, links);
    }

    /// <summary>Sonnenstrahl 12 Monate: Gesellschaft → Sparte → Kunde (je Sparte Top 8, Rest „Übrige“).</summary>
    public static SunburstNode Sunburst(IReadOnlyList<SalesFact> facts, DateOnly refEnd, int topCustomers = 8)
    {
        var rows = facts.Where(f => f.Date >= refEnd.AddMonths(-12) && f.Date < refEnd && f.ValueChf > 0).ToList();
        var companies = rows.GroupBy(f => f.Tsc).Select(t =>
        {
            var divisions = t.GroupBy(f => f.Division.Length > 0 ? f.Division : "Nicht zugeordnet").Select(d =>
            {
                var customers = d.GroupBy(f => f.CustomerKey).Select(c => (Key: c.Key, Name: SalesAnalytics.DisplayName(c.First().CustomerName, c.Key), Value: c.Sum(f => f.ValueChf)))
                    .OrderByDescending(c => c.Value).ToList();
                var kids = customers.Take(topCustomers).Select(c => new SunburstNode($"{t.Key}|{d.Key}|{c.Key}", c.Name, c.Value, [])).ToList();
                var rest = customers.Skip(topCustomers).Sum(c => c.Value);
                if (rest > 0) kids.Add(new SunburstNode($"{t.Key}|{d.Key}|~", "Übrige", rest, []));
                return new SunburstNode($"{t.Key}|{d.Key}", d.Key, d.Sum(f => f.ValueChf), kids);
            }).OrderByDescending(n => n.Value).ToList();
            return new SunburstNode(t.Key, t.Key, t.Sum(f => f.ValueChf), divisions);
        }).OrderByDescending(n => n.Value).ToList();
        return new SunburstNode("", "Total", companies.Sum(c => c.Value), companies);
    }

    /// <summary>Basis des Simulators je Sparte: Rest der Hochrechnung wie im Controlling und Fremdwährungsanteil.</summary>
    public static SimulatorBase SimulatorBaseOf(IReadOnlyList<SalesFact> facts, DateOnly refEnd, DateOnly dataStart)
    {
        var total = ControllingAnalytics.Projection("*", facts, refEnd, dataStart);
        var divisions = facts.GroupBy(f => f.Division.Length > 0 ? f.Division : "Nicht zugeordnet").Select(g =>
        {
            var list = g.ToList();
            var all = list.Sum(f => f.ValueChf);
            var foreign = all == 0 ? 0 : list.Where(f => f.Currency.Length > 0 && f.Currency != "CHF").Sum(f => f.ValueChf) / all;
            return (Division: g.Key, Remaining: ControllingAnalytics.Projection(g.Key, list, refEnd, dataStart).Rest, ForeignShare: foreign);
        }).OrderByDescending(d => d.Remaining).ToList();
        return new SimulatorBase(total.Year, total.Ytd, total.Rest, total.PreviousYear, divisions);
    }

    /// <summary>
    /// Was-wäre-wenn: Preis- und Mengenhebel wirken auf die Restmonate der Sparte ("*" = alle ohne eigenen Hebel),
    /// der Währungshebel auf den Fremdwährungsanteil der simulierten Restmonate.
    /// </summary>
    public static SimulatorResult Simulate(SimulatorBase b, IReadOnlyList<SimulatorLever> levers, double fxPercent)
    {
        var rows = new List<(string, decimal, decimal)>();
        decimal sim = 0, foreign = 0, baseSum = 0;
        foreach (var d in b.Divisions)
        {
            var lever = levers.FirstOrDefault(l => l.Division == d.Division) ?? levers.FirstOrDefault(l => l.Division == "*");
            var factor = lever is null ? 1m : (decimal)((1 + lever.PricePercent / 100) * (1 + lever.VolumePercent / 100));
            var s = d.Remaining * factor;
            rows.Add((d.Division, d.Remaining, s));
            sim += s;
            baseSum += d.Remaining;
            foreign += s * d.ForeignShare;
        }
        // Rest, den die Summe der Sparten nicht abdeckt (je Sicht begrenztes Wachstum): wirkt wie die Hebel "Alle" mit.
        var diff = b.TotalRest - baseSum;
        var all = levers.FirstOrDefault(l => l.Division == "*");
        var diffFactor = all is null ? 1m : (decimal)((1 + all.PricePercent / 100) * (1 + all.VolumePercent / 100));
        return new SimulatorResult(b.Ytd, b.TotalRest, sim + diff * diffFactor, foreign * (decimal)(fxPercent / 100), rows);
    }

    /// <summary>
    /// Bestellrhythmus: Kunden mit mindestens <paramref name="minOrders"/> Bestelltagen; üblicher Abstand = Median der Abstände;
    /// erwartet = letzter Tag + Median; überfällig = Tage seit dem erwarteten Datum (Stichtag = letzter Datentag).
    /// </summary>
    public static IReadOnlyList<RhythmCustomer> Rhythm(IReadOnlyList<SalesFact> facts, DateOnly refEnd, int minOrders = 4)
    {
        var day = facts.Count == 0 ? refEnd : facts.Max(f => f.Date);
        return facts.Where(f => f.ValueChf > 0).GroupBy(f => f.CustomerKey).Select(g =>
        {
            var days = g.Select(f => f.Date).Distinct().Order().ToList();
            if (days.Count < minOrders) return null;
            var gaps = days.Zip(days.Skip(1), (a, b) => (double)(b.DayNumber - a.DayNumber)).Order().ToList();
            var median = gaps.Count % 2 == 1 ? gaps[gaps.Count / 2] : (gaps[gaps.Count / 2 - 1] + gaps[gaps.Count / 2]) / 2;
            // Ganze Tage, kaufmaennisch gerundet: erwartetes Datum, Anzeige und Verhaeltnis rechnen mit demselben Wert.
            median = Math.Max(1, Math.Round(median, MidpointRounding.AwayFromZero));
            var expected = days[^1].AddDays((int)median);
            var last12 = g.Where(f => f.Date >= refEnd.AddMonths(-12) && f.Date < refEnd).Sum(f => f.ValueChf);
            return new RhythmCustomer(g.Key, SalesAnalytics.DisplayName(g.First().CustomerName, g.Key),
                g.GroupBy(f => f.CustomerCountry).OrderByDescending(x => x.Count()).First().Key,
                days, median, days[^1], expected, day.DayNumber - expected.DayNumber, last12);
        }).OfType<RhythmCustomer>().OrderByDescending(c => c.OverdueRatio * (double)c.Last12).ThenByDescending(c => c.OverdueRatio).ThenByDescending(c => c.Last12).ThenBy(c => c.Key, StringComparer.Ordinal).ToList();
    }

    /// <summary>Umsatz und Rechnungen je Tag eines Jahres (für die Kalender-Heatmap).</summary>
    public static IReadOnlyList<CalendarDay> Calendar(IReadOnlyList<SalesFact> facts, int year)
        => facts.Where(f => f.Date.Year == year).GroupBy(f => f.Date)
            .Select(g => new CalendarDay(g.Key, g.Sum(f => f.ValueChf), g.Select(f => f.InvoiceNumber).Distinct().Count()))
            .OrderBy(d => d.Day).ToList();

    /// <summary>
    /// Artikel-Netzwerk 12 Monate: Knoten = Artikel mit den meisten Kunden, Kante = von mindestens <paramref name="minBoth"/>
    /// gemeinsamen Kunden gekauft; Lage über ein einfaches Kräfteverfahren (deterministisch, Ausgangslage auf einem Kreis).
    /// </summary>
    public static (IReadOnlyList<NetworkNode> Nodes, IReadOnlyList<NetworkEdge> Edges) ArticleNetwork(IReadOnlyList<SalesFact> facts, DateOnly refEnd, int top = 40, int minBoth = 3)
    {
        var rows = facts.Where(f => f.Date >= refEnd.AddMonths(-12) && f.Date < refEnd && f.ValueChf > 0
                                    && SalesAnalytics.IsComparableArticle(f.Material, f.Article)).ToList();
        string A(SalesFact f) => f.Article.Length > 0 ? f.Article : f.Material;
        var arts = rows.GroupBy(A).Select(g => (Id: g.Key, Customers: g.Select(f => f.CustomerKey).ToHashSet(), Revenue: g.Sum(f => f.ValueChf)))
            .OrderByDescending(a => a.Customers.Count).Take(top).ToList();
        var edges = new List<NetworkEdge>();
        for (var i = 0; i < arts.Count; i++)
            for (var j = i + 1; j < arts.Count; j++)
            {
                var both = arts[i].Customers.Count(c => arts[j].Customers.Contains(c));
                if (both >= minBoth)
                    edges.Add(new NetworkEdge(arts[i].Id, arts[j].Id, both, (double)both / Math.Min(arts[i].Customers.Count, arts[j].Customers.Count)));
            }
        var index = arts.Select((a, i) => (a.Id, i)).ToDictionary(x => x.Id, x => x.i);
        var n = arts.Count;
        var x = new double[n];
        var y = new double[n];
        for (var i = 0; i < n; i++) { x[i] = 500 + 300 * Math.Cos(2 * Math.PI * i / Math.Max(1, n)); y[i] = 300 + 220 * Math.Sin(2 * Math.PI * i / Math.Max(1, n)); }
        var k = Math.Sqrt(1000.0 * 600 / Math.Max(1, n)) * 0.6;
        for (var it = 0; it < 250; it++)
        {
            var dx = new double[n];
            var dy = new double[n];
            for (var i = 0; i < n; i++)
                for (var j = 0; j < n; j++)
                {
                    if (i == j) continue;
                    var ddx = x[i] - x[j]; var ddy = y[i] - y[j];
                    var dist = Math.Max(1, Math.Sqrt(ddx * ddx + ddy * ddy));
                    var rep = k * k / dist;
                    dx[i] += ddx / dist * rep; dy[i] += ddy / dist * rep;
                }
            foreach (var e in edges)
            {
                int a = index[e.A], b = index[e.B];
                var ddx = x[a] - x[b]; var ddy = y[a] - y[b];
                var dist = Math.Max(1, Math.Sqrt(ddx * ddx + ddy * ddy));
                var att = dist * dist / k * (0.5 + e.Weight);
                dx[a] -= ddx / dist * att; dy[a] -= ddy / dist * att;
                dx[b] += ddx / dist * att; dy[b] += ddy / dist * att;
            }
            var temp = 40.0 * (1 - it / 250.0) + 1;
            for (var i = 0; i < n; i++)
            {
                // Schwerkraft zur Mitte, damit lose Knoten nicht wegfliegen.
                dx[i] += (500 - x[i]) * 0.02; dy[i] += (300 - y[i]) * 0.02;
                var len = Math.Max(1, Math.Sqrt(dx[i] * dx[i] + dy[i] * dy[i]));
                x[i] = Math.Clamp(x[i] + dx[i] / len * Math.Min(len, temp), 30, 970);
                y[i] = Math.Clamp(y[i] + dy[i] / len * Math.Min(len, temp), 30, 570);
            }
        }
        // Gruppen über verbundene Komponenten.
        var group = Enumerable.Repeat(-1, n).ToArray();
        var g2 = 0;
        for (var i = 0; i < n; i++)
        {
            if (group[i] >= 0) continue;
            var stack = new Stack<int>([i]);
            while (stack.Count > 0)
            {
                var c = stack.Pop();
                if (group[c] >= 0) continue;
                group[c] = g2;
                foreach (var e in edges.Where(e => index[e.A] == c || index[e.B] == c))
                    stack.Push(index[e.A] == c ? index[e.B] : index[e.A]);
            }
            g2++;
        }
        var nodes = arts.Select((a, i) => new NetworkNode(a.Id, a.Id, a.Customers.Count, a.Revenue, x[i], y[i], group[i])).ToList();
        return (nodes, edges);
    }

    /// <summary>Alle Monate von Datenbeginn bis Referenzende, auch solche ohne Umsatz (sonst springt die Zeitachse der Landschaft).</summary>
    public static IReadOnlyList<DateOnly> Months(DateOnly dataStart, DateOnly refEnd)
    {
        var list = new List<DateOnly>();
        for (var m = MonthStart(dataStart); m < refEnd; m = m.AddMonths(1))
            list.Add(m);
        return list;
    }

    /// <summary>Saeulenhoehe der Landschaft in Pixel: Wurzel des Anteils am Maximum; negative Werte (Gutschriften) und Null ergeben die Mindesthoehe statt NaN.</summary>
    public static double LandscapeHeight(decimal value, decimal max)
        => value <= 0 || max <= 0 ? 1 : Math.Max(1, 180 * Math.Sqrt((double)(value / max)));

    /// <summary>3D-Landschaft: Umsatz je Monat und Sparte ab Datenbeginn.</summary>
    public static IReadOnlyList<LandscapeCell> Landscape(IReadOnlyList<SalesFact> facts, DateOnly refEnd, DateOnly dataStart)
        => facts.Where(f => f.Date >= dataStart && f.Date < refEnd)
            .GroupBy(f => (M: MonthStart(f.Date), D: f.Division.Length > 0 ? f.Division : "Nicht zugeordnet"))
            .Select(g => new LandscapeCell(g.Key.M, g.Key.D, g.Sum(f => f.ValueChf)))
            .ToList();
}
