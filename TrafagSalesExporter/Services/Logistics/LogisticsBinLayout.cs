using System.Text.RegularExpressions;

namespace TrafagSalesExporter.Services;

/// <summary>Ein Lagerplatz, an dem heute etwas bewegt wurde, mit Lage im Regal (Gang, Feld, Ebene).</summary>
public sealed record LiveBin(
    string Lgnum, string Type, string Bin, string Aisle, int Column, int Level,
    int Picks, int PicksOpen, int Puts, int PutsOpen, decimal Quantity, DateTime? LastAt,
    IReadOnlyList<LiveTransferItem> Moves)
{
    public bool HasOpen => PicksOpen + PutsOpen > 0;
}

/// <summary>Ein Gang (Regalzeile) eines Lagertyps.</summary>
public sealed record LiveAisle(string Type, string Aisle, int Columns, int Levels, IReadOnlyList<LiveBin> Bins);

/// <summary>
/// Logistik live, 3D-Lagerplatzansicht (Wunsch Ingo 2026-10-05): aus den heutigen Transportauftragspositionen die
/// beruehrten Plaetze, nach Lagertyp und Gang gruppiert. Lagerplatznamen werden zerlegt („01-02-03“, „A-01-2“,
/// „A0102“, „010203“); was sich nicht zerlegen laesst, wird der Reihe nach in einen Gang gelegt. Schnittstellen-Lagertypen
/// (9xx, z. B. 916 Versandzone) sind keine Regale und stehen getrennt als Bodenzone. Doku docs/LOGISTIK_LIVE_2026-10-01.md.
/// </summary>
public static class LogisticsBinLayout
{
    private static readonly Regex Letters = new(@"^([A-Za-z]+)(\d+)$", RegexOptions.Compiled);
    private static readonly Regex TrailingDigits = new(@"(\d+)$", RegexOptions.Compiled);
    private static readonly Regex Digits = new(@"^(\d{2})(\d{2})(\d{2})$", RegexOptions.Compiled);

    public static bool IsInterim(string type) => type.StartsWith('9');

    /// <summary>Zerlegt einen Lagerplatz in Gang, Feld, Ebene; null, wenn kein Muster passt.</summary>
    public static (string Aisle, int Column, int Level)? Parse(string bin)
    {
        var b = (bin ?? "").Trim();
        if (b.Length == 0) return null;
        var parts = b.Split(['-', '/', '.', ' ', '_'], StringSplitOptions.RemoveEmptyEntries);
        // Gang-Feld-Ebene; Feld darf Buchstaben vor der Nummer haben (MLE04), Ebene als Zahl oder Buchstabe (B = 2).
        if (parts.Length >= 3 && Number(parts[1]) is { } c3 && Level(parts[2]) is { } l3)
            return (parts[0].ToUpperInvariant(), c3, l3);
        if (parts.Length == 2 && Number(parts[1]) is { } c2)
            return (parts[0].ToUpperInvariant(), c2, 1);
        var m = Letters.Match(b);
        if (m.Success)
        {
            // Buchstaben + Ziffern: die letzten zwei (bei drei Ziffern die letzte) sind die Ebene.
            var d = m.Groups[2].Value;
            var aisle = m.Groups[1].Value.ToUpperInvariant();
            return d.Length switch
            {
                <= 2 => (aisle, int.Parse(d), 1),
                3 => (aisle, int.Parse(d[..2]), int.Parse(d[2..])),
                <= 8 => (aisle, int.Parse(d[..^2]), int.Parse(d[^2..])),
                _ => null
            };
        }
        m = Digits.Match(b);
        if (m.Success)
            return (m.Groups[1].Value, int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value));
        return null;
    }

    private static int? Number(string part)
    {
        var m = TrailingDigits.Match(part);
        return m.Success && m.Value.Length <= 6 ? int.Parse(m.Value) : null;
    }

    private static int? Level(string part)
        => int.TryParse(part, out var n) ? n
            : part.Length == 1 && char.IsLetter(part[0]) ? char.ToUpperInvariant(part[0]) - 'A' + 1
            : null;

    /// <summary>Alle heute beruehrten Plaetze: Entnahme (von) und Einlagerung (nach), offen oder quittiert.</summary>
    public static IReadOnlyList<LiveBin> Bins(IEnumerable<LiveTransferItem> transfers)
    {
        var touches = new List<(string Lgnum, string Type, string Bin, LiveTransferItem T, bool Pick)>();
        foreach (var t in transfers)
        {
            if (t.FromBin.Trim().Length > 0) touches.Add((t.Lgnum, t.FromType.Trim(), t.FromBin.Trim(), t, true));
            if (t.ToBin.Trim().Length > 0) touches.Add((t.Lgnum, t.ToType.Trim(), t.ToBin.Trim(), t, false));
        }
        var result = new List<LiveBin>();
        foreach (var g in touches.GroupBy(x => (x.Lgnum, x.Type, x.Bin)))
        {
            var parsed = Parse(g.Key.Bin);
            var moves = g.Select(x => x.T).Distinct().OrderByDescending(x => x.ConfirmedAt ?? x.CreatedAt).ToList();
            result.Add(new LiveBin(g.Key.Lgnum, g.Key.Type, g.Key.Bin,
                parsed?.Aisle ?? "", parsed?.Column ?? 0, parsed?.Level ?? 1,
                g.Count(x => x.Pick), g.Count(x => x.Pick && !x.T.Confirmed),
                g.Count(x => !x.Pick), g.Count(x => !x.Pick && !x.T.Confirmed),
                g.Sum(x => x.T.Quantity), moves.Select(x => x.ConfirmedAt ?? x.CreatedAt).Max(), moves));
        }
        return result;
    }

    /// <summary>Regalgaenge der physischen Lagertypen; nicht zerlegbare Plaetze je Lagertyp der Reihe nach, 10 Felder je Ebene.</summary>
    public static IReadOnlyList<LiveAisle> Aisles(IEnumerable<LiveBin> bins, int maxAisles = 24)
    {
        var physical = bins.Where(b => !IsInterim(b.Type)).ToList();
        var aisles = new List<LiveAisle>();
        foreach (var g in physical.Where(b => b.Aisle.Length > 0).GroupBy(b => (b.Type, b.Aisle)))
        {
            // Felder und Ebenen verdichten: nur belegte Nummern, in ihrer Reihenfolge.
            var cols = g.Select(b => b.Column).Distinct().Order().Select((c, i) => (c, i)).ToDictionary(x => x.c, x => x.i + 1);
            var levels = g.Select(b => b.Level).Distinct().Order().Select((l, i) => (l, i)).ToDictionary(x => x.l, x => x.i + 1);
            var placed = g.Select(b => b with { Column = cols[b.Column], Level = levels[b.Level] }).ToList();
            aisles.Add(new LiveAisle(g.Key.Type, g.Key.Aisle, cols.Count, levels.Count, placed));
        }
        foreach (var g in physical.Where(b => b.Aisle.Length == 0).GroupBy(b => b.Type))
        {
            var ordered = g.OrderBy(b => b.Bin, StringComparer.OrdinalIgnoreCase).ToList();
            for (var chunk = 0; chunk * 40 < ordered.Count; chunk++)
            {
                var part = ordered.Skip(chunk * 40).Take(40).Select((b, i) => b with { Aisle = "~" + (chunk + 1), Column = i % 10 + 1, Level = i / 10 + 1 }).ToList();
                aisles.Add(new LiveAisle(g.Key, "~" + (chunk + 1), Math.Min(10, part.Count), (part.Count + 9) / 10, part));
            }
        }
        return aisles.OrderBy(a => a.Type, StringComparer.Ordinal).ThenBy(a => a.Aisle, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(a => a.Bins.Sum(b => b.Moves.Count)).Take(maxAisles)
            .OrderBy(a => a.Type, StringComparer.Ordinal).ThenBy(a => a.Aisle, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>Schnittstellen und Zonen (9xx) je Lagertyp: Anzahl Plaetze, Bewegungen, offen.</summary>
    public static IReadOnlyList<(string Type, int Bins, int Moves, int Open)> Zones(IEnumerable<LiveBin> bins)
        => bins.Where(b => IsInterim(b.Type)).GroupBy(b => b.Type)
            .Select(g => (g.Key, g.Count(), g.Sum(b => b.Picks + b.Puts), g.Sum(b => b.PicksOpen + b.PutsOpen)))
            .OrderBy(z => z.Key, StringComparer.Ordinal).ToList();
}
