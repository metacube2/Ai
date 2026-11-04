using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

// HR-Cockpit, Reiter Interaktiv (Wunsch Ingo 2026-11-04, „analog verkauf/interaktiv, fuer HR und seine Anforderungen“).
// Nur Rechnung, keine Darstellung; die Ansichten liegen in Components/HrKpi/Interactive. Grundlage sind dieselben
// Zeilen wie im uebrigen Cockpit: Austritte aus Rexx (nur Strukturfilter, alle Jahre), aktive Mitarbeitende aus Rexx,
// Absenzen je Fall aus SAP PA2001. Doku docs/HR_KPI.md Abschnitt 9.

/// <summary>Ein Balken im Zeitraffer: Abteilung und relevante Austritte der letzten zwoelf Monate bis zum Bildmonat.</summary>
public sealed record HrRaceBar(string Key, int Value);
public sealed record HrRaceFrame(DateTime Month, IReadOnlyList<HrRaceBar> Bars, int Total);

/// <summary>
/// Eine Abteilung in einem Quartal. Rate = Austritte der letzten zwoelf Monate / durchschnittlicher Headcount
/// (fuenf Quartalsenden). Null, wenn das Zwoelfmonatsfenster vor dem Beginn der Austrittsdaten anfaengt.
/// </summary>
public sealed record HrGalaxyPoint(string Org, int Headcount, decimal AverageHeadcount, int RelevantExits, int AllExits, double? RatePercent);
public sealed record HrGalaxyFrame(DateTime QuarterEnd, IReadOnlyList<HrGalaxyPoint> Points, bool Partial);

/// <summary>Bindungskurve eines Eintrittsjahrgangs: Anteil, der nach n Monaten noch da ist (Kaplan-Meier).</summary>
public sealed record HrRetentionPoint(int Month, double Share);
public sealed record HrRetentionCurve(int Cohort, int Size, int Exits, IReadOnlyList<HrRetentionPoint> Points)
{
    /// <summary>Anteil, der im ersten Jahr gegangen ist; null, solange der Jahrgang noch keine zwoelf Monate beobachtet ist.</summary>
    public double? FirstYearLoss => Points.LastOrDefault(p => p.Month <= 12) is { } p && Points.Any(x => x.Month >= 12) ? 1 - p.Share : null;
}

public sealed record HrSickDay(DateTime Day, int Persons);
public sealed record HrWeekdayShare(DayOfWeek Day, int Starts, double Share);

/// <summary>
/// Fit-&amp;-Wohl-Ausloeser (Wunsch HR, Mail Sonja Richter 2026-02-03): ab der 6. Absenz oder ab 12 Krankheitstagen
/// (Arbeitstage) im Kalenderjahr. TriggerDate ist der Beginn der Absenz, mit der die Grenze erreicht wurde.
/// </summary>
public sealed record HrFitWohlCase(int Personalnummer, string Name, string Organisation, int Episodes, decimal Days, DateTime TriggerDate, string Reason);

/// <summary>Ferien am Stueck (Wunsch HR): laengster zusammenhaengender Ferienblock eines Jahres in Arbeitstagen.</summary>
public sealed record HrVacationPerson(int Personalnummer, string Name, string Organisation, decimal LongestTaken, decimal LongestPlanned, DateTime? TakenStart, DateTime? TakenEnd);

public sealed record HrSimMonth(DateTime Month, decimal Headcount, bool Projected);
public sealed record HrSimBase(int Year, DateTime Anchor, int RemainingMonths, int YtdRelevant, decimal HeadcountNow,
    IReadOnlyList<HrSimMonth> PastMonths, decimal PreviousYearRate, double RelevantPerMonth, double OtherPerMonth, double HiresPerMonth);
public sealed record HrSimResult(decimal YearRate, decimal ProjectedRelevant, decimal AverageHeadcount, decimal HeadcountYearEnd, IReadOnlyList<HrSimMonth> Months);

public static class HrKpiInteractive
{
    /// <summary>Kurze Krankheiten fuer das Wochentagsmuster: hoechstens so viele Arbeitstage.</summary>
    public const int ShortSickWorkdays = 3;
    public const int FitWohlEpisodeLimit = 6;
    public const decimal FitWohlDayLimit = 12m;
    /// <summary>Zwei Wochen am Stueck = zehn Arbeitstage.</summary>
    public const decimal VacationBlockWorkdays = 10m;

    public const string ReasonEpisodes = "episodes";
    public const string ReasonDays = "days";
    public const string ReasonBoth = "both";

    public const string TenureUnder1 = "t0";
    public const string Tenure1To3 = "t1";
    public const string Tenure3To5 = "t3";
    public const string Tenure5To10 = "t5";
    public const string TenureOver10 = "t10";
    public const string TenureUnknown = "t?";

    private static DateTime MonthStart(DateTime d) => new(d.Year, d.Month, 1);
    private static DateTime MonthEnd(DateTime d) => MonthStart(d).AddMonths(1).AddDays(-1);
    private static string Org(string value) => string.IsNullOrWhiteSpace(value) ? "Unbekannt" : value.Trim();
    private static string PersonKey(int? pernr, string name) => pernr.HasValue ? $"P{pernr.Value}" : $"N{name.Trim().ToUpperInvariant()}";
    private static bool IsFixed(string type) => string.Equals(type, "Festangestellt", StringComparison.OrdinalIgnoreCase);

    // ------------------------------------------------------------------ Kuendigungen im Zeitraffer

    /// <summary>
    /// Je Monat die Abteilungen nach Austritten der letzten zwoelf Monate (gleitend), distinct je Person.
    /// Erstes Bild nach zwoelf Monaten Daten, hoechstens <paramref name="maxFrames"/> Bilder bis zum Stichtag.
    /// </summary>
    public static IReadOnlyList<HrRaceFrame> ExitRace(IReadOnlyCollection<HrLeaverRow> leavers, DateTime anchor, bool relevantOnly, int top = 12, int maxFrames = 60)
    {
        var rows = leavers
            .Where(x => x.Austrittsdatum.HasValue && x.Austrittsdatum.Value.Date <= anchor.Date && (!relevantOnly || x.IstFluktuationsrelevant))
            .ToList();
        if (rows.Count == 0)
            return [];
        var last = MonthStart(anchor);
        var start = MonthStart(rows.Min(x => x.Austrittsdatum!.Value)).AddMonths(11);
        if (start > last) start = last;
        var earliest = last.AddMonths(-(maxFrames - 1));
        if (start < earliest) start = earliest;

        var frames = new List<HrRaceFrame>();
        for (var m = start; m <= last; m = m.AddMonths(1))
        {
            var windowStart = m.AddMonths(-11);
            var windowEnd = m == last ? anchor.Date : MonthEnd(m);
            var inWindow = rows.Where(x => x.Austrittsdatum!.Value.Date >= windowStart && x.Austrittsdatum.Value.Date <= windowEnd).ToList();
            var bars = inWindow
                .GroupBy(x => Org(x.Organisationseinheit), StringComparer.OrdinalIgnoreCase)
                .Select(g => new HrRaceBar(g.Key, g.Select(x => PersonKey(x.Personalnummer, x.NameVoll)).Distinct().Count()))
                .OrderByDescending(b => b.Value).ThenBy(b => b.Key, StringComparer.OrdinalIgnoreCase)
                .Take(top)
                .ToList();
            frames.Add(new HrRaceFrame(m, bars, inWindow.Select(x => PersonKey(x.Personalnummer, x.NameVoll)).Distinct().Count()));
        }
        return frames;
    }

    // ------------------------------------------------------------------ Austrittsfluss

    public static string TenureBand(decimal? months) => months switch
    {
        null => TenureUnknown,
        < 12 => TenureUnder1,
        < 36 => Tenure1To3,
        < 60 => Tenure3To5,
        < 120 => Tenure5To10,
        _ => TenureOver10
    };

    public static readonly string[] TenureOrder = [TenureUnder1, Tenure1To3, Tenure3To5, Tenure5To10, TenureOver10, TenureUnknown];

    /// <summary>Austrittsgrund einer Zeile, wie ihn das Cockpit fuer die Fluktuation einordnet.</summary>
    public static string ExitReason(HrLeaverRow row) => row.IstFluktuationsrelevant ? "Fluktuationsrelevant" : row.FluktuationAusschlussgrund ?? "Unklar";

    /// <summary>Fluss Austritte → Grund → Verweildauer; Werte sind Personen (distinct).</summary>
    public static (IReadOnlyList<SankeyNode> Nodes, IReadOnlyList<SankeyLink> Links) ExitFlow(IReadOnlyCollection<HrLeaverRow> leavers)
    {
        var people = leavers
            .GroupBy(x => PersonKey(x.Personalnummer, x.NameVoll))
            .Select(g => g.OrderByDescending(x => x.Austrittsdatum).First())
            .ToList();
        if (people.Count == 0)
            return ([], []);
        var links = people.GroupBy(ExitReason).Select(g => new SankeyLink("a:all", "r:" + g.Key, g.Count()))
            .Concat(people.GroupBy(x => (Reason: ExitReason(x), Band: TenureBand(x.VerweildauerMonate)))
                .Select(g => new SankeyLink("r:" + g.Key.Reason, "t:" + g.Key.Band, g.Count())))
            .ToList();
        var nodes = new List<SankeyNode> { new("a:all", "Austritte", 0, people.Count) };
        nodes.AddRange(people.GroupBy(ExitReason).Select(g => new SankeyNode("r:" + g.Key, g.Key, 1, g.Count())).OrderByDescending(n => n.Value));
        nodes.AddRange(people.GroupBy(x => TenureBand(x.VerweildauerMonate))
            .Select(g => new SankeyNode("t:" + g.Key, g.Key, 2, g.Count()))
            .OrderBy(n => Array.IndexOf(TenureOrder, n.Label)));
        return (nodes, links);
    }

    /// <summary>Austritte je Kalendermonat ueber alle Jahre (Index 0 = Januar), distinct je Person und Monat.</summary>
    public static int[] ExitMonthPattern(IReadOnlyCollection<HrLeaverRow> leavers, bool relevantOnly)
    {
        var counts = new int[12];
        foreach (var g in leavers
                     .Where(x => x.Austrittsdatum.HasValue && (!relevantOnly || x.IstFluktuationsrelevant))
                     .GroupBy(x => (Key: PersonKey(x.Personalnummer, x.NameVoll), x.Austrittsdatum!.Value.Year, x.Austrittsdatum.Value.Month)))
            counts[g.Key.Month - 1]++;
        return counts;
    }

    // ------------------------------------------------------------------ Abteilungs-Galaxie

    private sealed record Spell(string Key, string Org, DateTime? Entry, DateTime? Exit, bool Relevant);

    private static List<Spell> FixedSpells(IReadOnlyCollection<HrKpiEmployeeRow> employees, IReadOnlyCollection<HrLeaverRow> leavers)
    {
        var spells = employees
            .Where(x => IsFixed(x.Mitarbeitertyp))
            .Select(x => new Spell(PersonKey(x.Personalnummer, x.NameVoll), Org(x.Organisationseinheit), x.Eintrittsdatum?.Date, null, false))
            .ToList();
        spells.AddRange(leavers
            .Where(x => IsFixed(x.Mitarbeitertyp))
            .Select(x => new Spell(PersonKey(x.Personalnummer, x.NameVoll), Org(x.Organisationseinheit), x.Eintrittsdatum?.Date, x.Austrittsdatum?.Date, x.IstFluktuationsrelevant)));
        // Dieselbe Person mit demselben Eintritt nur einmal; der Austritt gewinnt.
        return spells
            .GroupBy(x => (x.Key, x.Entry))
            .Select(g => g.OrderByDescending(x => x.Exit.HasValue).First())
            .ToList();
    }

    private static bool ActiveOn(Spell s, DateTime day)
        => (!s.Entry.HasValue || s.Entry.Value <= day) && (!s.Exit.HasValue || s.Exit.Value >= day);

    /// <summary>
    /// Abteilungen je Quartal: Headcount der Festangestellten am Quartalsende, Austritte der letzten zwoelf Monate und
    /// Rate. Die Abteilung aktiver Personen ist die heutige; wer gewechselt hat, zaehlt rueckwirkend in der neuen.
    /// </summary>
    public static IReadOnlyList<HrGalaxyFrame> OrgGalaxy(IReadOnlyCollection<HrKpiEmployeeRow> employees, IReadOnlyCollection<HrLeaverRow> leavers, DateTime anchor, int quarters = 12, int topOrgs = 14)
    {
        var spells = FixedSpells(employees, leavers);
        if (spells.Count == 0)
            return [];
        var dataStart = leavers.Where(x => x.Austrittsdatum.HasValue).Select(x => x.Austrittsdatum!.Value.Date).DefaultIfEmpty(anchor.Date).Min();
        var orgs = spells
            .Where(s => ActiveOn(s, anchor.Date) || (s.Exit.HasValue && s.Exit.Value > anchor.Date.AddYears(-3)))
            .GroupBy(s => s.Org, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .Take(topOrgs)
            .Select(g => g.Key)
            .ToList();
        var lastQuarterStart = new DateTime(anchor.Year, ((anchor.Month - 1) / 3 * 3) + 1, 1);
        var frames = new List<HrGalaxyFrame>();
        for (var i = quarters - 1; i >= 0; i--)
        {
            var qStart = lastQuarterStart.AddMonths(-3 * i);
            var fullEnd = qStart.AddMonths(3).AddDays(-1);
            var partial = fullEnd > anchor.Date;
            var qEnd = partial ? anchor.Date : fullEnd;
            var windowStart = qEnd.AddYears(-1);
            var points = orgs.Select(org =>
            {
                var orgSpells = spells.Where(s => string.Equals(s.Org, org, StringComparison.OrdinalIgnoreCase)).ToList();
                int HeadcountOn(DateTime day) => orgSpells.Where(s => ActiveOn(s, day)).Select(s => s.Key).Distinct().Count();
                var hc = HeadcountOn(qEnd);
                var avg = (decimal)Enumerable.Range(0, 5).Select(k => HeadcountOn(qEnd.AddMonths(-3 * k))).Average();
                var exits = orgSpells.Where(s => s.Exit.HasValue && s.Exit.Value > windowStart && s.Exit.Value <= qEnd).ToList();
                var relevant = exits.Where(s => s.Relevant).Select(s => s.Key).Distinct().Count();
                double? rate = windowStart < dataStart || avg <= 0 ? null : (double)(relevant / avg * 100m);
                return new HrGalaxyPoint(org, hc, avg, relevant, exits.Select(s => s.Key).Distinct().Count(), rate);
            }).ToList();
            frames.Add(new HrGalaxyFrame(qEnd, points, partial));
        }
        return frames;
    }

    // ------------------------------------------------------------------ Bindungskurve

    private static int FullMonths(DateTime from, DateTime to)
    {
        var months = ((to.Year - from.Year) * 12) + to.Month - from.Month;
        if (to.Day < from.Day) months--;
        return Math.Max(0, months);
    }

    /// <summary>
    /// Kaplan-Meier je Eintrittsjahr: Anteil der Festangestellten eines Jahrgangs, der nach n vollen Monaten noch da ist.
    /// Ereignis = Austritt (bei <paramref name="relevantOnly"/> nur fluktuationsrelevante; andere Austritte gelten dann als
    /// zensiert). Jahrgaenge vor dem Beginn der Austrittsdaten fehlen, weil ihre fruehen Austritte nicht in der Datei stehen.
    /// </summary>
    public static IReadOnlyList<HrRetentionCurve> Retention(IReadOnlyCollection<HrKpiEmployeeRow> employees, IReadOnlyCollection<HrLeaverRow> leavers, DateTime anchor, bool relevantOnly, int cohorts = 6, int maxMonths = 60)
    {
        var spells = FixedSpells(employees, leavers).Where(s => s.Entry.HasValue && s.Entry.Value <= anchor.Date).ToList();
        var exitDates = leavers.Where(x => x.Austrittsdatum.HasValue).Select(x => x.Austrittsdatum!.Value.Date).ToList();
        if (spells.Count == 0 || exitDates.Count == 0)
            return [];
        var firstYear = Math.Max(exitDates.Min().Year, anchor.Year - cohorts + 1);
        var curves = new List<HrRetentionCurve>();
        for (var year = firstYear; year <= anchor.Year; year++)
        {
            var cohort = spells.Where(s => s.Entry!.Value.Year == year).Select(s =>
            {
                var left = s.Exit.HasValue && s.Exit.Value <= anchor.Date;
                var end = left ? s.Exit!.Value : anchor.Date;
                return (Months: FullMonths(s.Entry!.Value, end), Event: left && (!relevantOnly || s.Relevant));
            }).ToList();
            if (cohort.Count == 0)
                continue;
            var points = new List<HrRetentionPoint> { new(0, 1.0) };
            var share = 1.0;
            var horizon = Math.Min(maxMonths, cohort.Max(c => c.Months));
            for (var t = 0; t < horizon; t++)
            {
                var atRisk = cohort.Count(c => c.Months >= t);
                if (atRisk == 0)
                    break;
                var events = cohort.Count(c => c.Event && c.Months == t);
                share *= 1 - (double)events / atRisk;
                points.Add(new HrRetentionPoint(t + 1, share));
            }
            curves.Add(new HrRetentionCurve(year, cohort.Count, cohort.Count(c => c.Event), points));
        }
        return curves;
    }

    // ------------------------------------------------------------------ Krankheitsverhalten

    // Krankheit und Unfall (seit 2026-11-04): HR wertet Fit &amp; Wohl und die Absenzquote ueber „Krankheit &amp; Unfall“ aus.
    private static bool IsSick(HrAbsenceSapCase c) => HrKpiDashboardBuilder.AbsenceRateTypes.Contains(c.Abwesenheitsart);

    private static DateTime CaseEnd(HrAbsenceSapCase c, DateTime today) => c.Bis.Year >= 9000 ? (c.Von > today ? c.Von : today) : c.Bis.Date;

    /// <summary>Krankgemeldete Personen je Arbeitstag eines Jahres bis heute.</summary>
    public static IReadOnlyList<HrSickDay> SickCalendar(IReadOnlyCollection<HrAbsenceSapCase> cases, int year, DateTime today)
    {
        var yearStart = new DateTime(year, 1, 1);
        var yearEnd = new DateTime(year, 12, 31) < today.Date ? new DateTime(year, 12, 31) : today.Date;
        var byDay = new Dictionary<DateTime, HashSet<string>>();
        foreach (var c in cases.Where(IsSick))
        {
            var from = c.Von.Date < yearStart ? yearStart : c.Von.Date;
            var to = CaseEnd(c, today.Date);
            if (to > yearEnd) to = yearEnd;
            for (var d = from; d <= to; d = d.AddDays(1))
            {
                if (!byDay.TryGetValue(d, out var set))
                    byDay[d] = set = [];
                set.Add(c.Personalnummer.TrimStart('0'));
            }
        }
        var days = new List<HrSickDay>();
        if (yearEnd < yearStart)
            return days;
        var holidays = ZurichWorkdayCalendar.GetPublicHolidays(year);
        for (var d = yearStart; d <= yearEnd; d = d.AddDays(1))
        {
            if (d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || holidays.Contains(d))
                continue;
            days.Add(new HrSickDay(d, byDay.TryGetValue(d, out var set) ? set.Count : 0));
        }
        return days;
    }

    /// <summary>
    /// Wochentag des Beginns kurzer Krankheiten (hoechstens <see cref="ShortSickWorkdays"/> Arbeitstage) im Zeitraum.
    /// Ohne besonderes Muster laege jeder Tag bei 20 %.
    /// </summary>
    public static IReadOnlyList<HrWeekdayShare> ShortSickStartWeekdays(IReadOnlyCollection<HrAbsenceSapCase> cases, DateTime from, DateTime to, DateTime today)
    {
        var starts = cases
            .Where(c => IsSick(c) && c.Von.Date >= from.Date && c.Von.Date <= to.Date && c.Bis.Year < 9000)
            .Where(c => ZurichWorkdayCalendar.CountWorkdays(c.Von.Date, CaseEnd(c, today.Date)) <= ShortSickWorkdays)
            .Where(c => c.Von.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
            .GroupBy(c => (c.Personalnummer, c.Von.Date))
            .Select(g => g.Key.Date.DayOfWeek)
            .ToList();
        var total = starts.Count;
        return new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }
            .Select(day =>
            {
                var n = starts.Count(x => x == day);
                return new HrWeekdayShare(day, n, total == 0 ? 0 : (double)n / total);
            })
            .ToList();
    }

    /// <summary>Faelle einer Person zu Absenzen zusammenfassen: liegt zwischen zwei Faellen kein Arbeitstag, ist es eine Absenz.</summary>
    internal static List<List<HrAbsenceSapCase>> Episodes(IEnumerable<HrAbsenceSapCase> cases, DateTime today)
    {
        var episodes = new List<List<HrAbsenceSapCase>>();
        DateTime? lastEnd = null;
        foreach (var c in cases.OrderBy(c => c.Von).ThenBy(c => c.Bis))
        {
            var end = CaseEnd(c, today);
            if (lastEnd.HasValue && (c.Von.Date <= lastEnd.Value.AddDays(1) ||
                                     ZurichWorkdayCalendar.CountWorkdays(lastEnd.Value.AddDays(1), c.Von.Date.AddDays(-1)) == 0))
            {
                episodes[^1].Add(c);
                if (end > lastEnd.Value) lastEnd = end;
                continue;
            }
            episodes.Add([c]);
            lastEnd = end;
        }
        return episodes;
    }

    private static Dictionary<string, HrKpiEmployeeRow> EmployeesByNumber(IReadOnlyCollection<HrKpiEmployeeRow> employees)
        => employees.Where(x => x.Personalnummer.HasValue)
            .GroupBy(x => x.Personalnummer!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .ToDictionary(g => g.Key, g => g.First());

    /// <summary>
    /// Personen, die im Jahr die Fit-&amp;-Wohl-Grenze erreicht haben: 6. Absenz (Beginn im Jahr) oder 12 Krankheitstage
    /// (Arbeitstage nach SAP, anteilig auf das Jahr bis heute). Nur aktive, gefilterte Mitarbeitende.
    /// </summary>
    public static IReadOnlyList<HrFitWohlCase> FitWohl(IReadOnlyCollection<HrAbsenceSapCase> cases, IReadOnlyCollection<HrKpiEmployeeRow> employees, int year, DateTime today)
    {
        var yearStart = new DateTime(year, 1, 1);
        var yearEnd = new DateTime(year, 12, 31) < today.Date ? new DateTime(year, 12, 31) : today.Date;
        var people = EmployeesByNumber(employees);
        var result = new List<HrFitWohlCase>();
        foreach (var person in cases.Where(c => IsSick(c) && c.Von.Date <= yearEnd && CaseEnd(c, today.Date) >= yearStart)
                     .GroupBy(c => c.Personalnummer.TrimStart('0')))
        {
            if (!people.TryGetValue(person.Key, out var employee))
                continue;
            int count = 0;
            decimal days = 0;
            DateTime? episodeTrigger = null, dayTrigger = null;
            foreach (var episode in Episodes(person, today.Date))
            {
                var begin = episode.Min(c => c.Von.Date);
                if (begin >= yearStart)
                    count++;
                days += episode.Sum(c => HrKpiDashboardBuilder.ProratedDays(c with { Bis = CaseEnd(c, today.Date) }, yearStart, yearEnd));
                var marker = begin < yearStart ? yearStart : begin;
                if (episodeTrigger is null && count >= FitWohlEpisodeLimit) episodeTrigger = marker;
                if (dayTrigger is null && days >= FitWohlDayLimit) dayTrigger = marker;
            }
            if (episodeTrigger is null && dayTrigger is null)
                continue;
            var trigger = new[] { episodeTrigger, dayTrigger }.Where(x => x.HasValue).Min()!.Value;
            var reason = episodeTrigger == trigger && dayTrigger == trigger ? ReasonBoth : episodeTrigger == trigger ? ReasonEpisodes : ReasonDays;
            result.Add(new HrFitWohlCase(employee.Personalnummer!.Value, employee.NameVoll, Org(employee.Organisationseinheit), count, Math.Round(days, 1), trigger, reason));
        }
        return result.OrderBy(x => x.TriggerDate).ThenBy(x => x.Personalnummer).ToList();
    }

    /// <summary>
    /// Ferien am Stueck je aktiver Person: Ferienfaelle (0100) des Jahres, ohne Arbeitstag dazwischen zu einem Block
    /// verbunden; Laenge in Ferientagen nach SAP. Bezogen = Block endet spaetestens heute; geplant zaehlt alle Bloecke.
    /// </summary>
    public static IReadOnlyList<HrVacationPerson> VacationBlocks(IReadOnlyCollection<HrAbsenceSapCase> cases, IReadOnlyCollection<HrKpiEmployeeRow> employees, int year, DateTime today)
    {
        var yearStart = new DateTime(year, 1, 1);
        var yearEnd = new DateTime(year, 12, 31);
        var byPerson = cases.Where(c => c.Abwesenheitsart == HrKpiDashboardBuilder.VacationAbsenceType && c.Von.Date <= yearEnd && c.Bis.Date >= yearStart)
            .GroupBy(c => c.Personalnummer.TrimStart('0'))
            .ToDictionary(g => g.Key, g => g.ToList());
        var result = new List<HrVacationPerson>();
        foreach (var employee in employees.Where(x => x.Personalnummer.HasValue))
        {
            var key = employee.Personalnummer!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            decimal taken = 0, planned = 0;
            DateTime? takenStart = null, takenEnd = null;
            if (byPerson.TryGetValue(key, out var list))
            {
                foreach (var block in Episodes(list, today.Date))
                {
                    var length = block.Sum(c => HrKpiDashboardBuilder.ProratedDays(c, yearStart, yearEnd));
                    var begin = block.Min(c => c.Von.Date);
                    var end = block.Max(c => c.Bis.Date);
                    planned = Math.Max(planned, length);
                    if (end <= today.Date && length > taken)
                    {
                        taken = length;
                        takenStart = begin < yearStart ? yearStart : begin;
                        takenEnd = end > yearEnd ? yearEnd : end;
                    }
                }
            }
            result.Add(new HrVacationPerson(employee.Personalnummer.Value, employee.NameVoll, Org(employee.Organisationseinheit), Math.Round(taken, 1), Math.Round(planned, 1), takenStart, takenEnd));
        }
        return result;
    }

    // ------------------------------------------------------------------ Was waere wenn

    private static decimal MonthlyAverageHeadcount(IReadOnlyCollection<Spell> spells, int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        int On(DateTime d) => spells.Where(s => ActiveOn(s, d)).Select(s => s.Key).Distinct().Count();
        return (On(start) + On(end)) / 2m;
    }

    /// <summary>
    /// Ausgangslage des Simulators: Ist bis zum Stichtag (Austritte, Headcount je Monat wie im Cockpit) und als
    /// Vorschlag fuer die Regler die Monatsmittel der letzten zwoelf Monate.
    /// </summary>
    public static HrSimBase SimulatorBase(IReadOnlyCollection<HrKpiEmployeeRow> employees, IReadOnlyCollection<HrLeaverRow> leavers, DateTime anchor)
    {
        var spells = FixedSpells(employees, leavers);
        var year = anchor.Year;
        var past = Enumerable.Range(1, anchor.Month)
            .Select(m => new HrSimMonth(new DateTime(year, m, 1), MonthlyAverageHeadcount(spells, year, m), false))
            .ToList();
        var ytd = spells.Where(s => s.Relevant && s.Exit.HasValue && s.Exit.Value.Year == year && s.Exit.Value <= anchor.Date).Select(s => s.Key).Distinct().Count();
        var now = spells.Where(s => ActiveOn(s, anchor.Date)).Select(s => s.Key).Distinct().Count();
        var windowStart = anchor.Date.AddYears(-1);
        var exits12 = spells.Where(s => s.Exit.HasValue && s.Exit.Value > windowStart && s.Exit.Value <= anchor.Date).ToList();
        var relevant12 = exits12.Where(s => s.Relevant).Select(s => s.Key).Distinct().Count();
        var other12 = exits12.Where(s => !s.Relevant).Select(s => s.Key).Distinct().Count();
        var hires12 = spells.Where(s => s.Entry.HasValue && s.Entry.Value > windowStart && s.Entry.Value <= anchor.Date).Select(s => s.Key).Distinct().Count();
        var prevHeadcount = Enumerable.Range(1, 12).Select(m => MonthlyAverageHeadcount(spells, year - 1, m)).Average();
        var prevRelevant = spells.Where(s => s.Relevant && s.Exit.HasValue && s.Exit.Value.Year == year - 1).Select(s => s.Key).Distinct().Count();
        return new HrSimBase(year, anchor.Date, 12 - anchor.Month, ytd, now, past,
            prevHeadcount <= 0 ? 0 : prevRelevant / prevHeadcount,
            Math.Round(relevant12 / 12.0, 1), Math.Round(other12 / 12.0, 1), Math.Round(hires12 / 12.0, 1));
    }

    /// <summary>
    /// Restmonate bis Jahresende mit den Reglern fortschreiben: Headcount je Monat = Vormonat + Eintritte − Austritte,
    /// Jahresfluktuation = (Ist-Austritte + Restmonate × relevante je Monat) / Durchschnitt der zwoelf Monats-Headcounts.
    /// </summary>
    public static HrSimResult Simulate(HrSimBase b, double relevantPerMonth, double otherPerMonth, double hiresPerMonth)
    {
        var months = new List<HrSimMonth>(b.PastMonths);
        var hc = b.HeadcountNow;
        for (var i = 1; i <= b.RemainingMonths; i++)
        {
            var start = hc;
            hc = Math.Max(0, hc + (decimal)(hiresPerMonth - relevantPerMonth - otherPerMonth));
            months.Add(new HrSimMonth(new DateTime(b.Year, b.Anchor.Month, 1).AddMonths(i), (start + hc) / 2m, true));
        }
        var average = months.Count == 0 ? 0 : months.Average(m => m.Headcount);
        var relevant = b.YtdRelevant + (decimal)relevantPerMonth * b.RemainingMonths;
        return new HrSimResult(average <= 0 ? 0 : relevant / average, relevant, average, hc, months);
    }
}
