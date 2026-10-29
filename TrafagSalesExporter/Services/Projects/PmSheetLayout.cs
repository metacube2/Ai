using System.Globalization;

namespace TrafagSalesExporter.Services.Projects;

// Rechenhilfen der Smartsheet-Ansicht (2026-10-29): Datum <-> Pixel, Skala, sichtbare Zeilen, Zeile hoch/runter,
// Pfeilwege und die Termine nach einem Gantt-Ziehen. Ohne Datenbank und ohne Blazor, damit es getestet werden kann.
// Die Razor-Komponente rechnet nichts selbst; sie fragt hier nach.
public static class PmSheetLayout
{
    // Zeilenhoehe und Kopfhoehe muessen mit pm-sheet.css (--pm-sh-row, --pm-sh-head) uebereinstimmen.
    public const int RowHeight = 32;
    public const int HeaderHeight = 48;

    public static class Zooms
    {
        public const string Days = "days";
        public const string Weeks = "weeks";
        public const string Months = "months";
        public static readonly string[] All = [Days, Weeks, Months];
    }

    // Ziehmodi aus pm-gantt.js.
    public static class DragModes
    {
        public const string Move = "move";
        public const string Start = "start";
        public const string End = "end";
    }

    // Spalten des Rasters. Nummer und Aufgabe sind fest und lassen sich nicht ausblenden.
    public static class Columns
    {
        public const string Number = "num";
        public const string Title = "title";
        public const string Assignee = "assignee";
        public const string Status = "status";
        public const string Start = "start";
        public const string End = "end";
        public const string Duration = "duration";
        public const string Predecessors = "predecessors";
        public const string Percent = "percent";
        public const string Priority = "priority";
        public const string Risk = "risk";
        public const string Critical = "critical";

        public static readonly string[] Fixed = [Number, Title];
        public static readonly string[] Optional = [Assignee, Status, Start, End, Duration, Predecessors, Percent, Priority, Risk, Critical];

        public static int Width(string column) => column switch
        {
            Number => 46,
            Title => 300,
            Assignee => 150,
            Status => 130,
            Start => 96,
            End => 96,
            Duration => 64,
            Predecessors => 92,
            Percent => 104,
            Priority => 104,
            Risk => 92,
            Critical => 56,
            _ => 100
        };

        // Spalte -> Feld fuer UpdateSheetFieldAsync; leer = nicht bearbeitbar.
        public static string Field(string column) => column switch
        {
            Title => PmService.SheetFields.Title,
            Assignee => PmService.SheetFields.Assignee,
            Status => PmService.SheetFields.Status,
            Start => PmService.SheetFields.Start,
            End => PmService.SheetFields.End,
            Duration => PmService.SheetFields.Duration,
            Predecessors => PmService.SheetFields.Predecessors,
            Percent => PmService.SheetFields.Percent,
            Priority => PmService.SheetFields.Priority,
            _ => string.Empty
        };

        // Termin, Dauer und Fortschritt einer Sammelzeile kommen aus den Unteraufgaben.
        public static bool IsRolledUp(string column) => column is Start or End or Duration or Percent;
    }

    public static double DayWidth(string zoom) => zoom switch
    {
        Zooms.Days => 30,
        Zooms.Months => 4,
        _ => 12
    };

    // Sichtbarer Zeitraum: beginnt an einem Montag (Monatsanfang bei Monaten) mit Vorlauf und laeuft grosszuegig
    // ueber das letzte Ende hinaus, damit Balken nach rechts gezogen werden koennen.
    public static (DateTime From, DateTime To) Range(DateTime? start, DateTime? end, DateTime today, string zoom)
    {
        today = today.Date;
        var first = start is { } s && s.Date < today ? s.Date : today;
        var last = end is { } e && e.Date > today ? e.Date : today;
        if (start is null && end is null)
            last = today.AddDays(56);
        var (lead, tail) = zoom switch
        {
            Zooms.Days => (7, 42),
            Zooms.Months => (31, 184),
            _ => (14, 91)
        };
        var from = first.AddDays(-lead);
        var to = last.AddDays(tail);
        if (zoom == Zooms.Months)
        {
            from = new DateTime(from.Year, from.Month, 1);
            to = new DateTime(to.Year, to.Month, DateTime.DaysInMonth(to.Year, to.Month));
        }
        else
        {
            from = from.AddDays(-(((int)from.DayOfWeek + 6) % 7));
            to = to.AddDays((7 - (int)to.DayOfWeek) % 7);
        }
        return (from, to);
    }

    public static int Index(DateTime from, DateTime date) => (date.Date - from.Date).Days;

    public static double X(DateTime from, DateTime date, double dayWidth) => Index(from, date) * dayWidth;

    // Ende inklusive: ein Balken von Montag bis Montag ist einen Tag breit.
    public static double Width(DateTime start, DateTime end, double dayWidth) =>
        Math.Max(Index(start, end) + 1, 1) * dayWidth;

    public static DateTime DayAt(DateTime from, double x, double dayWidth) =>
        from.Date.AddDays(Math.Floor(x / dayWidth));

    public static string Px(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    // ---------- Skala ----------

    public sealed record Segment(double X, double Width, DateTime Day);

    // Obere Zeile: Monate (bei Monaten: Jahre).
    public static IReadOnlyList<Segment> TopScale(DateTime from, DateTime to, string zoom)
    {
        var dw = DayWidth(zoom);
        var result = new List<Segment>();
        var cursor = from.Date;
        while (cursor <= to)
        {
            var next = zoom == Zooms.Months ? new DateTime(cursor.Year + 1, 1, 1) : new DateTime(cursor.Year, cursor.Month, 1).AddMonths(1);
            var stop = next.AddDays(-1) < to ? next.AddDays(-1) : to.Date;
            result.Add(new Segment(X(from, cursor, dw), Width(cursor, stop, dw), cursor));
            cursor = next;
        }
        return result;
    }

    // Untere Zeile: Tage, Kalenderwochen (ab Montag) oder Monate.
    public static IReadOnlyList<Segment> BottomScale(DateTime from, DateTime to, string zoom)
    {
        var dw = DayWidth(zoom);
        var result = new List<Segment>();
        var cursor = from.Date;
        while (cursor <= to)
        {
            var next = zoom switch
            {
                Zooms.Days => cursor.AddDays(1),
                Zooms.Months => new DateTime(cursor.Year, cursor.Month, 1).AddMonths(1),
                _ => cursor.AddDays(7 - ((int)cursor.DayOfWeek + 6) % 7)
            };
            var stop = next.AddDays(-1) < to ? next.AddDays(-1) : to.Date;
            result.Add(new Segment(X(from, cursor, dw), Width(cursor, stop, dw), cursor));
            cursor = next;
        }
        return result;
    }

    // Freie Tage (Wochenende und Zuercher Feiertage) als zusammenhaengende Laeufe fuer die Schattierung.
    public static IReadOnlyList<(int Index, int Days)> NonWorkdayRuns(DateTime from, DateTime to)
    {
        var runs = new List<(int Index, int Days)>();
        var total = Index(from, to) + 1;
        for (var i = 0; i < total; i++)
        {
            if (PmSchedule.IsWorkday(from.AddDays(i)))
                continue;
            if (runs.Count > 0 && runs[^1].Index + runs[^1].Days == i)
                runs[^1] = (runs[^1].Index, runs[^1].Days + 1);
            else
                runs.Add((i, 1));
        }
        return runs;
    }

    // Tagesindizes der freien Tage, fuer die Vorschau in pm-gantt.js (gleiche Regeln wie der Server).
    public static string NonWorkdayIndexes(DateTime from, DateTime to) =>
        string.Join(",", NonWorkdayRuns(from, to).SelectMany(r => Enumerable.Range(r.Index, r.Days)));

    // Feiertage (freie Werktage), fuer den Tooltip der Schattierung.
    public static bool IsHoliday(DateTime day) =>
        day.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday && !PmSchedule.IsWorkday(day);

    // ---------- Zeilen ----------

    // Elternzeile im Raster wie PmService.OutlineParents: die naechste Zeile weiter oben mit kleinerer Ebene.
    public static Dictionary<int, int?> OutlineParents(IReadOnlyList<PmSheetRow> rows)
    {
        var result = new Dictionary<int, int?>();
        var stack = new List<PmSheetRow>();
        foreach (var row in rows)
        {
            while (stack.Count > 0 && stack[^1].Level >= row.Level)
                stack.RemoveAt(stack.Count - 1);
            result[row.Id] = stack.Count > 0 ? stack[^1].Id : null;
            stack.Add(row);
        }
        return result;
    }

    // Sichtbare Zeilen: zugeklappte Zweige verschwinden. Mit Filter bleiben passende Zeilen und ihre Elternzeilen,
    // damit die Gliederung lesbar bleibt.
    public static IReadOnlyList<PmSheetRow> VisibleRows(
        IReadOnlyList<PmSheetRow> rows, IReadOnlySet<int> collapsed, Func<PmSheetRow, bool>? filter)
    {
        var parents = OutlineParents(rows);
        HashSet<int>? keep = null;
        if (filter is not null)
        {
            keep = [];
            foreach (var row in rows.Where(filter))
            {
                int? id = row.Id;
                while (id is { } current && keep.Add(current))
                    id = parents[current];
            }
        }
        var result = new List<PmSheetRow>();
        foreach (var row in rows)
        {
            if (keep is not null && !keep.Contains(row.Id))
                continue;
            var hidden = false;
            for (var p = parents[row.Id]; p is { } parent; p = parents[parent])
            {
                if (collapsed.Contains(parent))
                {
                    hidden = true;
                    break;
                }
            }
            if (!hidden)
                result.Add(row);
        }
        return result;
    }

    // Ziel fuer MoveRowAsync: hoch = vor das vorige Geschwister, runter = vor das uebernaechste (null = ans Ende).
    // CanMove false, wenn die Zeile in ihrer Gruppe schon zuoberst bzw. zuunterst steht.
    public static (bool CanMove, int? BeforeId) MoveTarget(IReadOnlyList<PmSheetRow> rows, int id, bool up)
    {
        var parents = OutlineParents(rows);
        if (!parents.TryGetValue(id, out var parent))
            return (false, null);
        var siblings = rows.Where(r => parents[r.Id] == parent).Select(r => r.Id).ToList();
        var at = siblings.IndexOf(id);
        if (up)
            return at <= 0 ? (false, null) : (true, siblings[at - 1]);
        if (at >= siblings.Count - 1)
            return (false, null);
        return at + 2 < siblings.Count ? (true, siblings[at + 2]) : (true, null);
    }

    // Vorgaengertext um eine Nummer ergaenzen (Abhaengigkeit per Ziehen); null, wenn sie schon drin steht.
    public static string? AddPredecessor(string current, int number)
    {
        var parsed = PmSchedule.ParsePredecessors(current) ?? [];
        if (parsed.Any(p => p.Number == number))
            return null;
        return PmSchedule.FormatPredecessors(parsed.Append((number, 0)));
    }

    // Termine nach dem Loslassen. Verschieben behaelt die Dauer in Arbeitstagen, die Griffe aendern nur Start bzw.
    // Ende. Der Server rastet auf Arbeitstage und beachtet die Vorgaenger.
    public static (DateTime Start, DateTime End)? DropDates(DateTime from, PmSheetRow row, string mode, int startIndex, int endIndex)
    {
        if (row.Start is not { } oldStart || row.End is not { } oldEnd)
            return null;
        var start = from.Date.AddDays(startIndex);
        var end = from.Date.AddDays(endIndex);
        switch (mode)
        {
            case DragModes.Move:
                var newStart = PmSchedule.SnapForward(start);
                return (newStart, row.IsMilestone ? newStart : PmSchedule.EndFor(newStart, Math.Max(row.Duration, 1)));
            case DragModes.Start:
                return (start > oldEnd.Date ? oldEnd.Date : start, oldEnd.Date);
            case DragModes.End:
                return (oldStart.Date, end < oldStart.Date ? oldStart.Date : end);
            default:
                return null;
        }
    }

    // ---------- Abhaengigkeitspfeile ----------

    public const double MilestoneHalf = 8;

    // Linke und rechte Kante eines Balkens in Pixeln (Meilenstein: Raute um die Tagesmitte).
    public static (double Left, double Right) BarEdges(DateTime from, PmSheetRow row, double dayWidth)
    {
        var x = X(from, row.Start!.Value, dayWidth);
        if (row.IsMilestone)
        {
            var mid = x + dayWidth / 2;
            return (mid - MilestoneHalf, mid + MilestoneHalf);
        }
        return (x, x + Width(row.Start.Value, row.End!.Value, dayWidth));
    }

    // Rechtwinkliger Pfeil vom Ende des Vorgaengers zum Anfang des Nachfolgers. Liegt der Nachfolger zu weit
    // links, laeuft der Weg zwischen den Zeilen zurueck (wie in Smartsheet).
    public static string ArrowPath(int fromRow, double fromX, int toRow, double toX)
    {
        const double stub = 8;
        var y1 = fromRow * RowHeight + RowHeight / 2.0;
        var y2 = toRow * RowHeight + RowHeight / 2.0;
        var sb = new System.Text.StringBuilder();
        sb.Append("M").Append(Px(fromX)).Append(' ').Append(Px(y1));
        if (toX - fromX >= 2 * stub)
        {
            sb.Append(" H").Append(Px(fromX + stub)).Append(" V").Append(Px(y2)).Append(" H").Append(Px(toX));
        }
        else
        {
            var between = toRow >= fromRow ? y1 + RowHeight / 2.0 : y1 - RowHeight / 2.0;
            sb.Append(" H").Append(Px(fromX + stub)).Append(" V").Append(Px(between))
              .Append(" H").Append(Px(toX - stub)).Append(" V").Append(Px(y2)).Append(" H").Append(Px(toX));
        }
        return sb.ToString();
    }

    // ---------- Darstellung ----------

    public static string RiskClass(string level) => level.Length == 0 ? string.Empty : "pm-sh-risk-" + level.ToLowerInvariant();

    public static string DateText(DateTime? date) => date?.ToString("dd.MM.yy", CultureInfo.InvariantCulture) ?? string.Empty;

    public static string IsoDate(DateTime? date) => date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;

    public static int IsoWeek(DateTime day) => ISOWeek.GetWeekOfYear(day);
}
