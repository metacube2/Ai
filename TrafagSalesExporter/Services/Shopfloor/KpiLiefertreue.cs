using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using ClosedXML.Excel;

namespace TrafagSalesExporter.Services.Shopfloor;

/// <summary>Eine Zeile aus C_SLSORDDELIVPERFANLYTSQRY (Q1, Auftragspositionen).</summary>
public sealed record LtOrderRow(
    string SalesOrder, string SalesOrderItem, string SalesOrderDate, string CommittedDeliveryDate,
    string CmtdDelivDateYearMonth, string ReqdDelivDateYearMonth, string EarliestReqdDelivDate, string DeliveryStatus,
    string SoldToParty, string Product, string ProductHierarchyNode, double NetAmount, double DelayDays);

/// <summary>Eine Zeile aus C_DELIVITMPERFANLYTSQRY (Q2, Lieferpositionen mit Ist-Warenausgang).</summary>
public sealed record LtDeliveryRow(string ActualGoodsMovementDate, string ReferenceSDDocument, string ReferenceSDDocumentItem);

/// <summary>Ausschluss (Tabelle kpi_exclusion): Typ "Kunde" oder "Material".</summary>
public sealed record LtExclusion(long Id, string Typ, string Schluessel, string Bezeichnung, string Grund);

/// <summary>
/// Liefertreue OTD / OTIF / OTP der Trafag AG (VKORG 1100), Spezifikation Fabio Palma 2026-10-14.
/// Port von docs/operations_kpi/Referenz/extract_liefertreue.js (Berechnung) und build_liefertreue.py (Seite, Excel).
/// Massgebend fuer die Abnahme ist die Referenzlogik, nicht der Spezifikationstext: Arbeitstage sind Mo-Fr ohne
/// Feiertagskalender, der Median ist das obere Mittelelement, Betraege werden wie Math.round gerundet.
/// Ergebnisform ("LT") wie window.__lt der Referenz: cut, year, stand, otd[], otp[], pg{}, list[], names{}.
/// Doku: docs/OPERATIONS_KPI_2026-10-14.md.
/// </summary>
public static class KpiLiefertreue
{
    public static readonly string[] MonthShort = ["Jan", "Feb", "Mär", "Apr", "Mai", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dez"];
    public static readonly string[] MonthLong = ["Januar", "Februar", "März", "April", "Mai", "Juni", "Juli", "August", "September", "Oktober", "November", "Dezember"];

    private static readonly JsonSerializerOptions Compact = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    // ------------------------------------------------------------------ Hilfen wie in der Referenz

    /// <summary>Datum aus OData auf JJJJMMTT: "20260108", "/Date(1767830400000)/" oder "2026-01-08...".</summary>
    public static string Norm(string? v)
    {
        if (string.IsNullOrEmpty(v)) return string.Empty;
        if (v.Length == 8 && v.All(char.IsAsciiDigit)) return v;
        var p = v.IndexOf('(');
        if (p >= 0)
        {
            var end = p + 1;
            if (end < v.Length && v[end] == '-') end++;
            while (end < v.Length && char.IsAsciiDigit(v[end])) end++;
            if (long.TryParse(v.AsSpan(p + 1, end - p - 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var ms))
                return DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        }
        var s = v.Replace("-", string.Empty);
        return s.Length > 8 ? s[..8] : s;
    }

    /// <summary>Arbeitstage Mo-Fr von a nach b (positiv = b spaeter), ohne Feiertage. null wenn ein Datum fehlt.</summary>
    public static int? At(string? a, string? b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b) ||
            string.CompareOrdinal(a, "1990") < 0 || string.CompareOrdinal(b, "1990") < 0)
            return null;
        if (!TryDate(a, out var x) || !TryDate(b, out var y)) return null;
        var sg = y >= x ? 1 : -1;
        var s = sg > 0 ? x : y;
        var e = sg > 0 ? y : x;
        var days = e.DayNumber - s.DayNumber;
        var full = days / 7;
        var n = full * 5;
        var c = s.AddDays(full * 7);
        while (c < e)
        {
            c = c.AddDays(1);
            if (c.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday) n++;
        }
        return sg * n;
    }

    private static bool TryDate(string s, out DateOnly d)
    {
        d = default;
        if (s.Length < 8) return false;
        if (!int.TryParse(s.AsSpan(0, 4), NumberStyles.None, CultureInfo.InvariantCulture, out var yy) ||
            !int.TryParse(s.AsSpan(4, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var mm) ||
            !int.TryParse(s.AsSpan(6, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var dd))
            return false;
        // JS new Date(y, m-1, d) rollt ueber (z. B. 00 -> Vormonat); fuer gueltige SAP-Daten ohne Bedeutung.
        try
        {
            d = new DateOnly(yy, 1, 1).AddMonths(mm - 1).AddDays(dd - 1);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    /// <summary>Median wie die Referenz: sortiert, Element floor(n/2) (oberes Mittelelement, kein Mittelwert).</summary>
    public static int? Med(IReadOnlyCollection<int> a)
    {
        if (a.Count == 0) return null;
        var s = a.OrderBy(x => x).ToList();
        return s[s.Count / 2];
    }

    /// <summary>Math.round von JavaScript (halbe Werte Richtung +unendlich), nicht Bankrundung.</summary>
    public static long JsRound(double v) => double.IsFinite(v) ? (long)Math.Floor(v + 0.5) : 0;

    private static string Key(string so, string item) => StripZeros(so) + "/" + StripZeros(item);

    private static string StripZeros(string v)
    {
        var t = (v ?? string.Empty).Trim();
        if (t.Length == 0 || !t.All(char.IsAsciiDigit)) return t;
        t = t.TrimStart('0');
        return t.Length == 0 ? "0" : t;
    }

    /// <summary>Reihenfolge von Object.keys/entries: ganzzahlige Schluessel aufsteigend, dann die uebrigen in Einfuegereihenfolge.</summary>
    private static IEnumerable<string> JsKeyOrder(IEnumerable<string> insertionOrder)
    {
        var all = insertionOrder.ToList();
        var idx = all.Where(IsArrayIndex).OrderBy(k => ulong.Parse(k, CultureInfo.InvariantCulture));
        return idx.Concat(all.Where(k => !IsArrayIndex(k)));
    }

    private static bool IsArrayIndex(string k)
        => k.Length is > 0 and <= 10 && k.All(char.IsAsciiDigit) && (k == "0" || k[0] != '0')
           && ulong.TryParse(k, NumberStyles.None, CultureInfo.InvariantCulture, out var v) && v < 4294967295UL;

    // ------------------------------------------------------------------ Berechnung

    private sealed class Item
    {
        public string So = "", It = "", Cd = "", Cm = "", Rd = "", Rm = "", St = "", Cu = "", Pr = "", Pg = "", Ist = "", Ex = "", Grp = "";
        public long Amt;
        public bool NoGi, OkCs, OkCt, OkRs, OkRt;
        public int? Lc, Lr, Wlz, Blz;
    }

    private sealed class Group
    {
        public int N, Ok, Rn, Rok, Kn, Kok, NC, OkC;
        public List<int> W = [];
    }

    /// <summary>
    /// Berechnet den Report fuer den Berichtsmonat <paramref name="cut"/> (JJJJMM, Zeitraum Januar bis cut).
    /// <paramref name="certProducts"/>: Materialien mit "EN10204"/"EN 10204" im Text; <paramref name="trafagCustomers"/>:
    /// Auftraggeber mit "Trafag" im Namen (nur fuer den Filter Kundengruppe). Namen werden nicht hier aufgeloest.
    /// </summary>
    public static JsonObject Compute(IEnumerable<LtOrderRow> rows, IEnumerable<LtDeliveryRow> deliveries, string cut, DateOnly stand,
        IEnumerable<LtExclusion> exclusions, IEnumerable<string> certProducts, IEnumerable<string> trafagCustomers,
        IReadOnlyDictionary<string, string>? hierarchyTexts = null)
    {
        var year = cut[..4];
        var excl = exclusions.ToList();
        var exC = excl.Where(e => e.Typ.Equals("Kunde", StringComparison.OrdinalIgnoreCase)).Select(e => e.Schluessel.Trim()).ToHashSet(StringComparer.Ordinal);
        // Konzerninterne Ausschluesse zaehlen fuer den Filter als Trafag-Gesellschaft (Referenz: alle ausser 19914 GFS).
        var exIntern = excl.Where(e => e.Typ.Equals("Kunde", StringComparison.OrdinalIgnoreCase) && e.Grund.Contains("konzern", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Schluessel.Trim()).ToHashSet(StringComparer.Ordinal);
        var exP = certProducts.Select(p => p.Trim())
            .Concat(excl.Where(e => e.Typ.Equals("Material", StringComparison.OrdinalIgnoreCase)).Select(e => e.Schluessel.Trim()))
            .ToHashSet(StringComparer.Ordinal);
        var ic = trafagCustomers.Where(id => !exC.Contains(id)).ToHashSet(StringComparer.Ordinal);

        // Ist-Warenausgang: letzter WA je Auftragsposition
        var gi = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var x in deliveries)
        {
            var a = Norm(x.ActualGoodsMovementDate);
            if (a.Length == 0 || string.CompareOrdinal(a, "2000") < 0) continue;
            var k = Key(x.ReferenceSDDocument, x.ReferenceSDDocumentItem);
            if (!gi.TryGetValue(k, out var cur) || string.CompareOrdinal(a, cur) > 0) gi[k] = a;
        }

        var items = new List<Item>();
        foreach (var r in rows)
        {
            var k = Key(r.SalesOrder, r.SalesOrderItem);
            var done = r.DeliveryStatus == "C";
            var hasGi = gi.TryGetValue(k, out var giDate);
            var i = new Item
            {
                So = r.SalesOrder, It = r.SalesOrderItem, Cd = Norm(r.CommittedDeliveryDate), Cm = r.CmtdDelivDateYearMonth ?? "",
                Rd = Norm(r.EarliestReqdDelivDate), Rm = r.ReqdDelivDateYearMonth ?? "", St = r.DeliveryStatus ?? "", Cu = r.SoldToParty ?? "",
                Pr = r.Product ?? "", Pg = string.IsNullOrEmpty(r.ProductHierarchyNode) ? "(leer)" : r.ProductHierarchyNode,
                Amt = JsRound(r.NetAmount), Ist = done && hasGi ? giDate! : "", NoGi = done && !hasGi
            };
            if (i.Ist.Length > 0)
            {
                i.Lc = At(i.Cd, i.Ist);
                i.Lr = At(i.Rd, i.Ist);
                // JS vergleicht null wie 0 (null <= 0 ist wahr): fehlt ein Termin, gilt die Position wie in der Referenz als puenktlich.
                i.OkCs = (i.Lc ?? 0) <= 0; i.OkCt = (i.Lc ?? 0) < 5; i.OkRs = (i.Lr ?? 0) <= 0; i.OkRt = (i.Lr ?? 0) < 5;
            }
            else if (i.NoGi)
            {
                // Fallback ohne Lieferung: SAP-Verzug DelivToCmtdDelivDelayInDays (NaN zaehlt als nicht puenktlich)
                var ok = r.DelayDays <= 0;
                i.OkCs = i.OkCt = i.OkRs = i.OkRt = ok;
            }
            i.Ex = exC.Contains(i.Cu) ? "K" : exP.Contains(i.Pr) ? "Z" : "";
            i.Grp = ic.Contains(i.Cu) || exIntern.Contains(i.Cu) ? "I" : "E";
            i.Wlz = At(Norm(r.SalesOrderDate), i.Rd);
            i.Blz = At(Norm(r.SalesOrderDate), i.Cd);
            items.Add(i);
        }

        var y1 = year + "01";
        bool InC(Item i) => string.CompareOrdinal(i.Cm, y1) >= 0 && string.CompareOrdinal(i.Cm, cut) <= 0;
        bool InR(Item i) => string.CompareOrdinal(i.Rm, y1) >= 0 && string.CompareOrdinal(i.Rm, cut) <= 0;

        var m = new SortedDictionary<string, int[]>(StringComparer.Ordinal);   // n, s, t, e5, ng
        var p = new SortedDictionary<string, int[]>(StringComparer.Ordinal);   // n, s, t
        var o = new Dictionary<string, (string Max, int N, int S, int T)>(StringComparer.Ordinal);
        foreach (var i in items)
        {
            if (i.Ex.Length > 0) continue;
            if (i.Cm.Length > 0 && i.Cm.StartsWith(year, StringComparison.Ordinal))
            {
                var cur = o.TryGetValue(i.So, out var v) ? v : ("0", 0, 0, 0);
                if (string.CompareOrdinal(i.Cm, cur.Item1) > 0) cur.Item1 = i.Cm;
                cur.Item2++;
                if (i.OkCs) cur.Item3++;
                if (i.OkCt) cur.Item4++;
                o[i.So] = cur;
            }
            if (InC(i))
            {
                if (!m.TryGetValue(i.Cm, out var mm)) m[i.Cm] = mm = new int[5];
                mm[0]++;
                if (i.OkCs) mm[1]++;
                if (i.OkCt) mm[2]++;
                if (i.Lc is <= -5) mm[3]++;
                if (i.NoGi) mm[4]++;
            }
            if (InR(i))
            {
                if (!p.TryGetValue(i.Rm, out var pp)) p[i.Rm] = pp = new int[3];
                pp[0]++;
                if (i.OkRs) pp[1]++;
                if (i.OkRt) pp[2]++;
            }
        }
        var om = new Dictionary<string, int[]>(StringComparer.Ordinal);
        foreach (var x in o.Values)
        {
            if (string.CompareOrdinal(x.Max, cut) > 0) continue;
            if (!om.TryGetValue(x.Max, out var a)) om[x.Max] = a = new int[3];
            a[0]++;
            if (x.S == x.N) a[1]++;
            if (x.T == x.N) a[2]++;
        }

        var otd = new JsonArray();
        foreach (var (k, v) in m)
        {
            var a = om.TryGetValue(k, out var oo) ? oo : new int[3];
            otd.Add(string.Join(';', k, v[0], v[1], v[2], a[0], a[1], a[2], v[3], v[4]));
        }
        var otp = new JsonArray();
        foreach (var (k, v) in p)
            otp.Add(string.Join(';', k, v[0], v[1], v[2]));

        // Produktgruppen / Machbarkeit Wunschtermin
        var blOrder = new List<string>();
        var bl = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        foreach (var i in items)
            if (i.Ex.Length == 0 && InC(i) && i.Blz is { } b)
            {
                if (!bl.TryGetValue(i.Pg, out var blList)) { bl[i.Pg] = blList = []; blOrder.Add(i.Pg); }
                blList.Add(b);
            }
        var allMed = Med(bl.Values.SelectMany(v => v).ToList());
        var refMed = bl.ToDictionary(kv => kv.Key, kv => kv.Value.Count >= 20 ? Med(kv.Value) : allMed, StringComparer.Ordinal);
        var gOrder = new List<string>();
        var g = new Dictionary<string, Group>(StringComparer.Ordinal);
        var wm = new SortedDictionary<string, int[]>(StringComparer.Ordinal);   // rn, rok, kn, kok
        int tRn = 0, tRok = 0, tKn = 0, tKok = 0;
        foreach (var i in items)
        {
            if (i.Ex.Length > 0) continue;
            if (!g.TryGetValue(i.Pg, out var gg)) { g[i.Pg] = gg = new Group(); gOrder.Add(i.Pg); }
            if (InC(i)) { gg.NC++; if (i.OkCt) gg.OkC++; }
            if (!InR(i) || i.Wlz is not { } wlz) continue;
            var reference = refMed.TryGetValue(i.Pg, out var rv) && rv is not null ? rv : allMed;
            var real = wlz >= (reference ?? 0);
            gg.N++;
            if (i.OkRt) gg.Ok++;
            gg.W.Add(wlz);
            if (!wm.TryGetValue(i.Rm, out var w)) wm[i.Rm] = w = new int[4];
            if (real) { gg.Rn++; w[0]++; tRn++; if (i.OkRt) { gg.Rok++; w[1]++; tRok++; } }
            else { gg.Kn++; w[2]++; tKn++; if (i.OkRt) { gg.Kok++; w[3]++; tKok++; } }
        }
        var pgM = new JsonArray();
        foreach (var (k, w) in wm) pgM.Add(string.Join(';', k, w[0], w[1], w[2], w[3]));
        var pgG = new JsonArray();
        foreach (var k in JsKeyOrder(gOrder).Where(k => g[k].N > 0).OrderByDescending(k => g[k].N))
        {
            var gg = g[k];
            var hasBl = bl.TryGetValue(k, out var blk);
            var refV = refMed.TryGetValue(k, out var rv) && rv is not null ? rv : allMed;
            pgG.Add(string.Join(';', k, hierarchyTexts is not null && hierarchyTexts.TryGetValue(k, out var t) ? t : "",
                gg.N, gg.Ok, gg.Rn, gg.Rok, gg.Kn, gg.Kok, Med(gg.W)?.ToString(CultureInfo.InvariantCulture) ?? "",
                hasBl ? Med(blk!)?.ToString(CultureInfo.InvariantCulture) ?? "" : "",
                refV?.ToString(CultureInfo.InvariantCulture) ?? "", gg.NC, gg.OkC, hasBl ? blk!.Count : 0));
        }

        // Abweichungsliste: zu spaet (nicht OTD) und zu frueh (>= 5 AT), inklusive Ausschluesse
        var list = new JsonArray();
        foreach (var i in items.Where(i => InC(i) && (!i.OkCt || i.Lc is <= -5)))
            list.Add(string.Join(';', i.So, i.It, i.Cd, i.Ist, I(i.Lc), i.St, i.Cu, i.Pr, i.Amt.ToString(CultureInfo.InvariantCulture),
                i.Grp, i.Ex, i.Rd, I(i.Lr), i.NoGi ? "1" : "0", i.OkCt ? "F" : "L"));

        return new JsonObject
        {
            ["done"] = true,
            ["otd"] = otd,
            ["otp"] = otp,
            ["pg"] = new JsonObject
            {
                ["allMed"] = allMed,
                ["tot"] = new JsonObject { ["rn"] = tRn, ["rok"] = tRok, ["kn"] = tKn, ["kok"] = tKok },
                ["m"] = pgM,
                ["g"] = pgG
            },
            ["list"] = list,
            ["names"] = new JsonObject { ["c"] = new JsonObject(), ["p"] = new JsonObject() },
            ["cut"] = cut,
            ["year"] = year,
            ["stand"] = stand.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["positionen"] = items.Count
        };

        static string I(int? v) => v?.ToString(CultureInfo.InvariantCulture) ?? "";
    }

    /// <summary>Kunden und Materialien der Abweichungsliste (fuer die Namensaufloesung).</summary>
    public static (List<string> Customers, List<string> Products) ListKeys(JsonObject lt)
    {
        var rows = Rows(lt, "list");
        return (rows.Select(x => x[6]).Distinct().ToList(), rows.Select(x => x[7]).Distinct().ToList());
    }

    private static List<string[]> Rows(JsonObject lt, string name)
        => (lt[name] as JsonArray ?? []).Select(x => (x?.GetValue<string>() ?? string.Empty).Split(';')).ToList();

    // ------------------------------------------------------------------ Kennzahlen (Kurzfazit wie build_liefertreue.py)

    public sealed record Summary(string Period, string Stand, string Cut,
        double OtdTol, double OtdStrict, int OtdBase, double OtifTol, double OtifStrict, int OtifBase,
        double OtpTol, double OtpStrict, int OtpBase, string LastMonth, double OtdLast, double OtifLast, double OtpLast, int Early5, int LateInEval);

    public static Summary Summarize(JsonObject lt)
    {
        var cut = lt["cut"]!.GetValue<string>();
        var lm = int.Parse(cut.AsSpan(4, 2), CultureInfo.InvariantCulture);
        var otd = Rows(lt, "otd");
        var otp = Rows(lt, "otp");
        var list = Rows(lt, "list");
        int S(List<string[]> a, int i) => a.Sum(x => int.Parse(x[i], CultureInfo.InvariantCulture));
        static double P(int a, int b) => b == 0 ? 0 : Math.Round(100.0 * a / b, 1);
        int n = S(otd, 1), st = S(otd, 2), tl = S(otd, 3), on = S(otd, 4), os = S(otd, 5), ot = S(otd, 6);
        int pn = S(otp, 1), ps = S(otp, 2), pt = S(otp, 3);
        var last = otd.Count > 0 ? otd[^1] : ["", "0", "0", "0", "0", "0", "0", "0", "0"];
        var lastP = otp.Count > 0 ? otp[^1] : ["", "0", "0", "0"];
        int L(string[] x, int i) => int.Parse(x[i], CultureInfo.InvariantCulture);
        var stand = DateOnly.ParseExact(lt["stand"]!.GetValue<string>(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
        return new Summary(Period(lm) + " " + cut[..4], stand.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture), cut,
            P(tl, n), P(st, n), n, P(ot, on), P(os, on), on, P(pt, pn), P(ps, pn), pn,
            MonthShort[lm - 1], P(L(last, 3), L(last, 1)), P(L(last, 6), Math.Max(1, L(last, 4))), P(L(lastP, 3), L(lastP, 1)),
            S(otd, 7), list.Count(x => x[10].Length == 0 && x[14] == "L"));
    }

    private static string Period(int lm) => lm > 1 ? $"Jan – {MonthShort[lm - 1]}" : "Jan";

    // ------------------------------------------------------------------ Seite (Port von build_liefertreue.py)

    /// <summary>Fuellt die Seitenvorlage (board_template.html) mit den Daten; Platzhalter wie in der Referenz.</summary>
    public static string BuildBoard(JsonObject lt, string template, IReadOnlyDictionary<string, string>? extra = null)
    {
        var cut = lt["cut"]!.GetValue<string>();
        var year = lt["year"]!.GetValue<string>();
        var lm = int.Parse(cut.AsSpan(4, 2), CultureInfo.InvariantCulture);
        var stand = DateOnly.ParseExact(lt["stand"]!.GetValue<string>(), "yyyy-MM-dd", CultureInfo.InvariantCulture).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        var period = Period(lm);
        var otd = Rows(lt, "otd");
        var otp = Rows(lt, "otp");
        string Col(List<string[]> a, int i) => "[" + string.Join(',', a.Select(x => x[i])) + "]";
        var months = otd.Select(x => MonthShort[int.Parse(x[0].AsSpan(4, 2), CultureInfo.InvariantCulture) - 1]);
        var d = "const D={\n  m:[" + string.Join(", ", months.Select(x => JsonSerializer.Serialize(x, Compact))) + "]" +
                $",\n  items:{Col(otd, 1)}, strict:{Col(otd, 2)}, tol:{Col(otd, 3)}, early:{Col(otd, 7)},\n" +
                $"  orders:{Col(otd, 4)}, otifS:{Col(otd, 5)}, otifT:{Col(otd, 6)},\n" +
                $"  pItems:{Col(otp, 1)}, pS:{Col(otp, 2)}, pT:{Col(otp, 3)}\n}};";
        var pg = "const PG=" + (lt["pg"] ?? new JsonObject()).ToJsonString(Compact) + ";";

        var names = lt["names"] as JsonObject;
        var nc = names?["c"] as JsonObject ?? new JsonObject();
        var np = names?["p"] as JsonObject ?? new JsonObject();
        var l = Rows(lt, "list");
        var cus = l.Select(x => x[6]).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList();
        var prs = l.Select(x => x[7]).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList();
        var ci = cus.Select((c, i) => (c, i)).ToDictionary(x => x.c, x => x.i, StringComparer.Ordinal);
        var pi = prs.Select((c, i) => (c, i)).ToDictionary(x => x.c, x => x.i, StringComparer.Ordinal);
        static JsonNode? Toi(string v) => v.Length == 0 ? JsonValue.Create("") : JsonValue.Create(int.Parse(v, CultureInfo.InvariantCulture));
        var late = new JsonArray();
        foreach (var x in l)
            late.Add(new JsonArray(x[0], x[1], x[2], x[3], Toi(x[4]), x[5], ci[x[6]], pi[x[7]], int.Parse(x[8], CultureInfo.InvariantCulture),
                x[9], x[10], x[11], Toi(x[12]), int.Parse(x[13], CultureInfo.InvariantCulture), x[14]));
        var lateObj = new JsonObject
        {
            ["cus"] = new JsonArray(cus.Select(c => (JsonNode?)new JsonArray(c, Name(nc, c))).ToArray()),
            ["prs"] = new JsonArray(prs.Select(p => (JsonNode?)new JsonArray(p, Name(np, p))).ToArray()),
            ["late"] = late
        };
        var lateJs = "const LATE=" + lateObj.ToJsonString(Compact) + ";";

        var s = template;
        var repl = new List<(string, string)>
        {
            ("__PERIOD__", period), ("__YEAR__", year), ("__STAND__", stand), ("__LASTM__", MonthShort[lm - 1]),
            ("__LASTMONTHNAME__", MonthLong[lm - 1]), ("__NEXTMONTHNAME__", MonthLong[lm % 12])
        };
        if (extra is not null) repl.AddRange(extra.Select(kv => (kv.Key, kv.Value)));
        foreach (var (k, v) in repl) s = s.Replace(k, v, StringComparison.Ordinal);
        return s.Replace("/*__D__*/", d, StringComparison.Ordinal)
                .Replace("/*__PG__*/", pg, StringComparison.Ordinal)
                .Replace("/*__LATE__*/", lateJs, StringComparison.Ordinal);
    }

    private static string Name(JsonObject names, string key) => names[key]?.GetValue<string>() ?? "";

    // ------------------------------------------------------------------ Excel-Abweichungsliste (Port von build_liefertreue.py)

    public static byte[] ExportXlsx(JsonObject lt)
    {
        var cut = lt["cut"]!.GetValue<string>();
        var year = lt["year"]!.GetValue<string>();
        var lm = int.Parse(cut.AsSpan(4, 2), CultureInfo.InvariantCulture);
        var stand = DateOnly.ParseExact(lt["stand"]!.GetValue<string>(), "yyyy-MM-dd", CultureInfo.InvariantCulture).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        var names = lt["names"] as JsonObject;
        var nc = names?["c"] as JsonObject ?? new JsonObject();
        var np = names?["p"] as JsonObject ?? new JsonObject();
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Abweichungen OTD");
        string[] head = ["Art", "Auftrag", "Position", "Kunde Nr.", "Kunde", "Kundengruppe", "In Auswertung", "Ausschlussgrund", "Material", "Materialtext",
            "Bestätigter Termin", "Monat", "Ist-WA (letzter)", "Abweichung AT", "Wunschliefertermin", "Abweichung Wunsch AT", "Status", "Wert CHF", "Hinweis"];
        for (var c = 0; c < head.Length; c++)
        {
            var cell = ws.Cell(1, c + 1);
            cell.Value = head[c];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F5F8B");
        }
        var stt = new Dictionary<string, string> { ["C"] = "geliefert", ["B"] = "teilgeliefert", ["A"] = "offen" };
        var xr = new Dictionary<string, string> { ["K"] = "Gesellschaft ausgeschlossen", ["Z"] = "Zertifikat EN10204", [""] = "" };
        var row = 2;
        foreach (var x in Rows(lt, "list"))
        {
            var cd = TryDate(x[2], out var cDate) && string.CompareOrdinal(x[2], "1990") > 0 ? cDate : (DateOnly?)null;
            object?[] v =
            [
                x[14] == "L" ? "zu spät" : "zu früh", Num(x[0]), Num(x[1]), x[6], Name(nc, x[6]), x[9] == "I" ? "Trafag-Gesellschaft" : "Extern",
                x[10].Length > 0 ? "nein" : "ja", xr.GetValueOrDefault(x[10], x[10]), x[7], Name(np, x[7]),
                cd, cd?.ToString("yyyy-MM", CultureInfo.InvariantCulture) ?? "", Dt(x[3]), x[4].Length > 0 ? Num(x[4]) : null, Dt(x[11]),
                x[12].Length > 0 ? Num(x[12]) : null, stt.GetValueOrDefault(x[5], x[5]), Num(x[8]), x[13] == "1" ? "kein WA gefunden, SAP-Kennzahl" : ""
            ];
            for (var c = 0; c < v.Length; c++)
            {
                var cell = ws.Cell(row, c + 1);
                switch (v[c])
                {
                    case null: break;
                    case DateOnly dd: cell.Value = dd.ToDateTime(TimeOnly.MinValue); cell.Style.DateFormat.Format = "DD.MM.YYYY"; break;
                    case long n: cell.Value = n; break;
                    case string str: cell.Value = str; break;
                }
            }
            ws.Cell(row, 18).Style.NumberFormat.Format = "#,##0";
            row++;
        }
        double[] widths = [9, 10, 8, 10, 38, 18, 12, 24, 12, 38, 12, 9, 12, 12, 12, 14, 13, 11, 26];
        for (var c = 0; c < widths.Length; c++) ws.Column(c + 1).Width = widths[c];
        ws.SheetView.FreezeRows(1);
        ws.Range(1, 1, Math.Max(1, row - 1), head.Length).SetAutoFilter();
        var d2 = wb.AddWorksheet("Definition");
        string[] defs =
        [
            "Eigene Berechnung: letzter Ist-Warenausgang aller Lieferungen der Position (C_DELIVITMPERFANLYTSQRY) gegen bestätigten Liefertermin bzw. Wunschtermin (C_SLSORDDELIVPERFANLYTSQRY). Arbeitstage Mo–Fr ohne Feiertage.",
            "OTD: vollständig geliefert und Ist-WA höchstens 4 AT nach bestätigtem Termin. OTIF: alle Positionen des Auftrags OTD. OTP: wie OTD, gegen Wunschliefertermin.",
            "Zu spät: ≥ 5 AT nach bestätigtem Termin oder offen/teilgeliefert. Zu früh: Ist-WA ≥ 5 AT vor bestätigtem Termin.",
            "Nicht in der Auswertung: Kunden und Materialien der Ausschlusstabelle (Operations > KPI), Zertifikate EN10204.",
            $"Zeitraum: bestätigter Termin {Period(lm)} {year}, VKORG 1100, Stand {stand}."
        ];
        for (var i = 0; i < defs.Length; i++) d2.Cell(i + 1, 1).Value = defs[i];
        d2.Column(1).Width = 150;
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();

        static object? Num(string s) => long.TryParse(s, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var n) ? n : s;
        static object? Dt(string s) => TryDate(s, out var dd) && string.CompareOrdinal(s, "1990") > 0 ? dd : null;
    }
}
