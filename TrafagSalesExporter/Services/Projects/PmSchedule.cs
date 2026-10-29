namespace TrafagSalesExporter.Services.Projects;

// Terminierung der Smartsheet-Ansicht (2026-10-29, Wunsch Ingo). Reine Rechenregeln ohne Datenbank, damit
// Gantt-Ziehen, Raster, Aufgabenpanel, Kalender und Wochen_Todo-Abgleich dieselben Regeln nutzen:
// - Termine sind Arbeitstage (Mo bis Fr ohne die Zuercher Feiertage, ZurichWorkdayCalendar); Ende inklusive.
// - Dauer = Arbeitstage von Start bis Ende; Meilenstein = Dauer 0, Start = Ende.
// - Ende-Anfang mit Versatz: der Nachfolger beginnt am n-ten Arbeitstag nach dem Ende des Vorgaengers
//   (n = 1 + Versatz). Der Nachfolger folgt in beide Richtungen, wie in Smartsheet mit Abhaengigkeiten.
// - Eine Sammelzeile (Epic, Aufgabe mit Unteraufgaben) wird nicht gezogen; Start und Ende kommen aus den Kindern.
// - Zyklen werden beim Anlegen einer Abhaengigkeit abgewiesen.
public static class PmSchedule
{
    public sealed class Node
    {
        public int Id { get; init; }
        public int? RollupParentId { get; init; }
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
        public bool Milestone { get; init; }
        public int Percent { get; set; }
        public bool HasChildren { get; set; }
        public bool Critical { get; set; }
        public int FloatDays { get; set; }
    }

    public sealed record Link(int PredecessorId, int SuccessorId, int LagDays);

    // ---------- Arbeitstage ----------

    private static readonly Dictionary<int, IReadOnlySet<DateTime>> HolidayCache = [];
    private static readonly object HolidayLock = new();

    public static bool IsWorkday(DateTime date)
    {
        date = date.Date;
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return false;
        IReadOnlySet<DateTime> holidays;
        lock (HolidayLock)
        {
            if (!HolidayCache.TryGetValue(date.Year, out holidays!))
            {
                holidays = ZurichWorkdayCalendar.GetPublicHolidays(date.Year);
                HolidayCache[date.Year] = holidays;
            }
        }
        return !holidays.Contains(date);
    }

    // Ein Termin auf einem freien Tag rueckt auf den naechsten Arbeitstag.
    public static DateTime SnapForward(DateTime date)
    {
        date = date.Date;
        while (!IsWorkday(date))
            date = date.AddDays(1);
        return date;
    }

    public static DateTime SnapBackward(DateTime date)
    {
        date = date.Date;
        while (!IsWorkday(date))
            date = date.AddDays(-1);
        return date;
    }

    // n Arbeitstage weiter (n darf negativ sein); der Ausgangstag muss kein Arbeitstag sein.
    public static DateTime AddWorkdays(DateTime date, int days)
    {
        date = date.Date;
        var step = days >= 0 ? 1 : -1;
        var remaining = Math.Abs(days);
        while (remaining > 0)
        {
            date = date.AddDays(step);
            if (IsWorkday(date))
                remaining--;
        }
        return date;
    }

    // Arbeitstage von start bis end, beide eingeschlossen; 0, wenn end vor start liegt.
    public static int Duration(DateTime start, DateTime end) =>
        end.Date < start.Date ? 0 : ZurichWorkdayCalendar.CountWorkdays(start.Date, end.Date);

    // Ende zu einer Dauer in Arbeitstagen; Dauer 0 (Meilenstein) und 1 ergeben denselben Tag.
    public static DateTime EndFor(DateTime start, int duration) =>
        duration <= 1 ? SnapForward(start) : AddWorkdays(SnapForward(start), duration - 1);

    // Fruehester Start eines Nachfolgers: der (1 + Versatz)-te Arbeitstag nach dem Ende des Vorgaengers.
    public static DateTime EarliestStartAfter(DateTime predecessorEnd, int lagDays) =>
        SnapForward(AddWorkdays(predecessorEnd, Math.Max(1 + lagDays, 0)));

    // ---------- Abhaengigkeiten ----------

    // true, wenn die neue Kante predecessor -> successor einen Kreis schliessen wuerde.
    public static bool WouldCreateCycle(IEnumerable<Link> links, int predecessorId, int successorId)
    {
        if (predecessorId == successorId)
            return true;
        var next = links.ToLookup(l => l.PredecessorId, l => l.SuccessorId);
        var seen = new HashSet<int>();
        var stack = new Stack<int>();
        stack.Push(successorId);
        while (stack.Count > 0)
        {
            var id = stack.Pop();
            if (id == predecessorId)
                return true;
            if (!seen.Add(id))
                continue;
            foreach (var s in next[id])
                stack.Push(s);
        }
        return false;
    }

    // Verschiebt Nachfolger auf ihren fruehesten Start (Dauer bleibt), rechnet Sammelzeilen neu und markiert
    // den kritischen Pfad. Gibt die Ids zurueck, deren Start oder Ende sich geaendert hat.
    public static IReadOnlySet<int> Apply(IReadOnlyList<Node> nodes, IReadOnlyList<Link> links)
    {
        var byId = nodes.ToDictionary(n => n.Id);
        var before = nodes.ToDictionary(n => n.Id, n => (n.Start, n.End));
        foreach (var n in nodes)
            n.HasChildren = false;
        foreach (var n in nodes)
            if (n.RollupParentId is { } p && byId.TryGetValue(p, out var parent))
                parent.HasChildren = true;

        var valid = links.Where(l => byId.ContainsKey(l.PredecessorId) && byId.ContainsKey(l.SuccessorId)).ToList();
        foreach (var id in TopologicalOrder(nodes.Select(n => n.Id), valid))
        {
            var node = byId[id];
            if (node.HasChildren)
                continue;
            DateTime? earliest = null;
            foreach (var l in valid.Where(l => l.SuccessorId == id))
            {
                var pred = byId[l.PredecessorId];
                var predEnd = EffectiveEnd(pred, byId, nodes);
                if (predEnd is null)
                    continue;
                var candidate = EarliestStartAfter(predEnd.Value, l.LagDays);
                if (earliest is null || candidate > earliest)
                    earliest = candidate;
            }
            if (earliest is null)
                continue;
            var duration = node.Milestone ? 0 : node.Start is { } s && node.End is { } e ? Math.Max(Duration(s, e), 1) : 1;
            node.Start = earliest.Value;
            node.End = node.Milestone ? earliest.Value : EndFor(earliest.Value, duration);
        }

        Rollup(nodes, byId);
        MarkCritical(nodes, valid, byId);

        return nodes.Where(n => before[n.Id].Start != n.Start || before[n.Id].End != n.End).Select(n => n.Id).ToHashSet();
    }

    // Ende einer Zeile fuer ihre Nachfolger: bei einer Sammelzeile das spaeteste Ende der Kinder.
    private static DateTime? EffectiveEnd(Node node, Dictionary<int, Node> byId, IReadOnlyList<Node> nodes)
    {
        if (!node.HasChildren)
            return node.End;
        var ends = Descendants(node.Id, nodes).Where(n => !n.HasChildren && n.End is not null).Select(n => n.End!.Value).ToList();
        return ends.Count == 0 ? node.End : ends.Max();
    }

    private static IEnumerable<Node> Descendants(int id, IReadOnlyList<Node> nodes)
    {
        foreach (var child in nodes.Where(n => n.RollupParentId == id))
        {
            yield return child;
            foreach (var deeper in Descendants(child.Id, nodes))
                yield return deeper;
        }
    }

    private static List<int> TopologicalOrder(IEnumerable<int> ids, List<Link> links)
    {
        var all = ids.ToList();
        var incoming = all.ToDictionary(i => i, _ => 0);
        foreach (var l in links)
            incoming[l.SuccessorId]++;
        var queue = new Queue<int>(all.Where(i => incoming[i] == 0));
        var order = new List<int>();
        var next = links.ToLookup(l => l.PredecessorId, l => l.SuccessorId);
        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            order.Add(id);
            foreach (var s in next[id])
                if (--incoming[s] == 0)
                    queue.Enqueue(s);
        }
        // Ein Kreis (nur bei alten Daten moeglich) bleibt unberuehrt statt endlos zu laufen.
        order.AddRange(all.Where(i => !order.Contains(i)));
        return order;
    }

    // Sammelzeilen: frueheste Kinder-Start, spaetestes Kinder-Ende, Fortschritt nach Dauer gewichtet.
    private static void Rollup(IReadOnlyList<Node> nodes, Dictionary<int, Node> byId)
    {
        foreach (var parent in nodes.Where(n => n.HasChildren).OrderByDescending(n => Depth(n, byId)))
        {
            var children = nodes.Where(n => n.RollupParentId == parent.Id).ToList();
            var dated = children.Where(c => c.Start is not null && c.End is not null).ToList();
            if (dated.Count > 0)
            {
                parent.Start = dated.Min(c => c.Start!.Value);
                parent.End = dated.Max(c => c.End!.Value);
            }
            var weight = 0.0;
            var sum = 0.0;
            foreach (var c in children)
            {
                var w = c.Start is { } s && c.End is { } e ? Math.Max(Duration(s, e), 1) : 1;
                weight += w;
                sum += w * c.Percent;
            }
            parent.Percent = weight > 0 ? (int)Math.Round(sum / weight) : 0;
        }
    }

    private static int Depth(Node node, Dictionary<int, Node> byId)
    {
        var depth = 0;
        var current = node;
        while (current.RollupParentId is { } p && byId.TryGetValue(p, out var parent) && depth < 10)
        {
            depth++;
            current = parent;
        }
        return depth;
    }

    // Kritischer Pfad: Rueckwaertsrechnung in Arbeitstagen vom Projektende. Puffer 0 = kritisch.
    private static void MarkCritical(IReadOnlyList<Node> nodes, List<Link> links, Dictionary<int, Node> byId)
    {
        var leaves = nodes.Where(n => !n.HasChildren && n.Start is not null && n.End is not null).ToList();
        foreach (var n in nodes)
        {
            n.Critical = false;
            n.FloatDays = 0;
        }
        if (leaves.Count == 0)
            return;
        var projectEnd = leaves.Max(n => n.End!.Value);
        var leafIds = leaves.Select(n => n.Id).ToHashSet();
        var leafLinks = links.Where(l => leafIds.Contains(l.PredecessorId) && leafIds.Contains(l.SuccessorId)).ToList();
        var latestFinish = new Dictionary<int, DateTime>();
        var order = TopologicalOrder(leafIds, leafLinks);
        order.Reverse();
        foreach (var id in order)
        {
            var node = byId[id];
            var lf = projectEnd;
            foreach (var l in leafLinks.Where(l => l.PredecessorId == id))
            {
                var succ = byId[l.SuccessorId];
                var succDuration = succ.Milestone ? 0 : Duration(succ.Start!.Value, succ.End!.Value);
                var succLf = latestFinish[l.SuccessorId];
                var succLs = succDuration <= 1 ? succLf : AddWorkdays(succLf, -(succDuration - 1));
                var candidate = AddWorkdays(succLs, -(1 + l.LagDays));
                if (candidate < lf)
                    lf = candidate;
            }
            latestFinish[id] = lf;
            var slack = lf < node.End!.Value ? 0 : Duration(node.End.Value, lf) - 1;
            node.FloatDays = Math.Max(slack, 0);
            node.Critical = node.FloatDays == 0;
        }
        foreach (var parent in nodes.Where(n => n.HasChildren))
            parent.Critical = Descendants(parent.Id, nodes).Any(c => !c.HasChildren && c.Critical);
    }

    // ---------- Vorgaenger-Text wie in Smartsheet: "12, 15+2" (Nummern der Aufgaben, Versatz in Arbeitstagen) ----------

    public static string FormatPredecessors(IEnumerable<(int Number, int LagDays)> predecessors) =>
        string.Join(", ", predecessors.OrderBy(p => p.Number).Select(p =>
            p.LagDays == 0 ? p.Number.ToString() : $"{p.Number}{(p.LagDays > 0 ? "+" : "")}{p.LagDays}"));

    // Liest "12, 15FS+2, 17-1"; gibt null bei einem Formatfehler zurueck.
    public static IReadOnlyList<(int Number, int LagDays)>? ParsePredecessors(string? text)
    {
        var result = new List<(int, int)>();
        if (string.IsNullOrWhiteSpace(text))
            return result;
        foreach (var raw in text.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var part = raw.Replace(" ", string.Empty).ToUpperInvariant();
            // Schluessel wie LOG-12: nur die Nummer zaehlt.
            var dash = part.IndexOf('-');
            if (dash > 0 && char.IsLetter(part[0]))
                part = part[(dash + 1)..];
            var i = 0;
            while (i < part.Length && char.IsDigit(part[i]))
                i++;
            if (i == 0 || !int.TryParse(part[..i], out var number))
                return null;
            var rest = part[i..];
            if (rest.StartsWith("FS", StringComparison.Ordinal))
                rest = rest[2..];
            var lag = 0;
            if (rest.Length > 0)
            {
                if (rest.EndsWith('T') || rest.EndsWith('D'))
                    rest = rest[..^1];
                if (!int.TryParse(rest, out lag))
                    return null;
            }
            result.Add((number, lag));
        }
        return result;
    }
}
