using System.Data;
using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;

namespace TrafagSalesExporter.Services;

/// <summary>Eine Komponente mit ihrer Wirkung nach oben und dem Lieferantenrisiko.</summary>
public sealed record BomComponentImpact(
    string Material, string Text, int DirectParents, int Ancestors, int TopProducts, int Depth,
    int Suppliers, string SupplierNames, int OverdueItems)
{
    public bool SingleSource => Suppliers == 1;
    public bool NotPurchased => Suppliers == 0;
    public bool AtRisk => SingleSource || OverdueItems > 0;
}

/// <summary>Ein Endprodukt (oberste Ebene der Teilmenge) mit dem Risiko, das von unten erbt.</summary>
public sealed record BomProductRisk(
    string Material, string Text, int ComponentsBelow, int Levels, int SingleSourceComponents, int OverdueComponents)
{
    public int RiskComponents => SingleSourceComponents + OverdueComponents;
}

/// <summary>Ergebnis fuer die Seite; unveraenderlich.</summary>
public sealed class BomInheritanceResult
{
    public DateTime? LoadedAtUtc { get; init; }
    public int Pairs { get; init; }
    public int Components { get; init; }
    public int Parents { get; init; }
    public int Intermediates { get; init; }
    public int TopProducts { get; init; }
    public int MaxDepth { get; init; }
    public IReadOnlyList<BomComponentImpact> ComponentImpacts { get; init; } = [];
    public IReadOnlyList<BomProductRisk> ProductRisks { get; init; } = [];
    public IReadOnlyDictionary<string, IReadOnlyList<string>> ParentsOf { get; init; } = new Dictionary<string, IReadOnlyList<string>>();
    public IReadOnlyDictionary<string, string> Texts { get; init; } = new Dictionary<string, string>();
}

/// <summary>
/// Verwendung und Risiko ueber mehrere Stufen (Wunsch Ingo 2026-10-01: Stuecklistenanalyse
/// erweitern, „wo die Daten reichen“). Quelle ist <c>MaterialParentCache</c> aus dem LZ-Code-Report:
/// er enthaelt NUR Komponenten mit LZ-Code (gemessen 2026-10-01: 25'279 Paare, 4'590 Komponenten,
/// 8'420 Eltern, 1'941 beides; ueber ihn erreicht man nur 6 % des CH-Umsatzes 2025). Deshalb hier
/// bewusst kein Umsatz und keine Gleichteile, sondern Wirkungsbreite, Tiefe und das Lieferantenrisiko
/// aus dem Einkaufscache (Lieferanten der letzten 24 Monate, ueberfaellige offene Einteilungen),
/// vererbt nach oben. Doku: docs/LOGISTIK_STUECKLISTE_VERWENDUNG_RISIKO_2026-10-01.md.
/// Nur lesend gegen die App-Datenbank; kein SAP-Zugriff.
/// </summary>
public sealed class BomInheritanceService
{
    internal const int SupplierLookbackMonths = 24;
    internal const int OverdueLookbackDays = 365;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private (string Stamp, BomInheritanceResult Result)? _cache;

    public BomInheritanceService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<BomInheritanceResult> GetAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var conn = (SqliteConnection)db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync(ct);

            var stamp = await ScalarAsync(conn,
                "SELECT COALESCE((SELECT MAX(LastLoadedAtUtc) FROM MaterialParentCache),'') || '|' || COALESCE((SELECT MAX(LastLoadedAtUtc) FROM PurchasingEkkoCache),'') || '|' || date('now')", ct);
            if (_cache is { } cached && cached.Stamp == stamp)
                return cached.Result;

            var pairs = new List<(string Component, string Parent)>();
            DateTime? loadedAt = null;
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Kompnr, ElternMatnr, LastLoadedAtUtc FROM MaterialParentCache";
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    var component = Normalize(reader.GetString(0));
                    var parent = Normalize(reader.GetString(1));
                    if (component.Length > 0 && parent.Length > 0 && component != parent)
                        pairs.Add((component, parent));
                    if (loadedAt is null && DateTime.TryParse(reader.GetString(2), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var at))
                        loadedAt = at;
                }
            }

            var since = DateTime.Today.AddMonths(-SupplierLookbackMonths).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var suppliers = new List<(string Material, string Supplier, string Name)>();
            var texts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT p.Matnr, k.Lifnr, COALESCE(k.SupplierName,''), COALESCE(p.Maktx, p.Txz01, '')
FROM PurchasingEkpoCache p JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
WHERE COALESCE(p.Loekz,'') = '' AND COALESCE(k.Bstyp,'F') = 'F' AND k.Bedat >= $since";
                cmd.Parameters.AddWithValue("$since", since);
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    var material = Normalize(reader.GetString(0));
                    if (material.Length == 0)
                        continue;
                    suppliers.Add((material, reader.GetString(1).Trim(), reader.GetString(2).Trim()));
                    var text = reader.GetString(3).Trim();
                    if (text.Length > 0)
                        texts.TryAdd(material, text);
                }
            }

            var overdue = new List<string>();
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT p.Matnr
FROM PurchasingEketCache e
JOIN PurchasingEkpoCache p ON p.Ebeln = e.Ebeln AND p.Ebelp = e.Ebelp
WHERE COALESCE(p.Loekz,'') = '' AND COALESCE(p.Elikz,'False') NOT IN ('True','X','1')
  AND e.Eindt < $today AND e.Eindt >= $from AND CAST(e.Wemng AS REAL) < CAST(e.Menge AS REAL)";
                cmd.Parameters.AddWithValue("$today", DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                // Nur Liefertermine der letzten 12 Monate: aeltere offene Einteilungen sind fast immer nie
                // geschlossene Altbestellungen (gemessen 2026-10-01: 41'434 ohne Grenze, bis 2002 zurueck).
                cmd.Parameters.AddWithValue("$from", DateTime.Today.AddDays(-OverdueLookbackDays).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                    overdue.Add(Normalize(reader.GetString(0)));
            }

            // Texte fuer Eigenfertigung und Endprodukte aus den Schweizer Verkaufszeilen.
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Material, MAX(Name) FROM CentralSalesRecords WHERE Tsc = 'TRCH' AND COALESCE(Name,'') <> '' GROUP BY Material";
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                    texts.TryAdd(Normalize(reader.GetString(0)), reader.GetString(1).Trim());
            }

            var result = Build(pairs, suppliers, overdue, texts, loadedAt);
            _cache = (stamp, result);
            return result;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Reine Rechnung, ohne Datenbank, damit sie testbar ist.</summary>
    internal static BomInheritanceResult Build(
        IReadOnlyCollection<(string Component, string Parent)> pairs,
        IEnumerable<(string Material, string Supplier, string Name)> suppliers,
        IEnumerable<string> overdueMaterials,
        IReadOnlyDictionary<string, string> texts,
        DateTime? loadedAt)
    {
        var parentsOf = pairs.GroupBy(x => x.Component, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(x => x.Parent).Distinct(StringComparer.OrdinalIgnoreCase).ToList(), StringComparer.OrdinalIgnoreCase);
        var childrenOf = pairs.GroupBy(x => x.Parent, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Component).Distinct(StringComparer.OrdinalIgnoreCase).ToList(), StringComparer.OrdinalIgnoreCase);
        var components = parentsOf.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var allParents = childrenOf.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var tops = allParents.Where(p => !components.Contains(p)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var supplierSets = suppliers.GroupBy(x => x.Material, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Where(x => x.Supplier.Length > 0)
                .GroupBy(x => x.Supplier).Select(s => s.First().Name.Length > 0 ? s.First().Name : s.Key).ToList(), StringComparer.OrdinalIgnoreCase);
        var overdueCount = overdueMaterials.GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        // Tiefe nach oben und Vorfahren, mit Merker und Kreisschutz.
        var depthUp = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int DepthUp(string material, HashSet<string> path)
        {
            if (depthUp.TryGetValue(material, out var known))
                return known;
            if (!parentsOf.TryGetValue(material, out var parents) || !path.Add(material))
                return 0;
            var depth = 1 + parents.Where(p => !path.Contains(p)).Select(p => DepthUp(p, path)).DefaultIfEmpty(0).Max();
            path.Remove(material);
            depthUp[material] = depth;
            return depth;
        }

        HashSet<string> Ancestors(string material)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var stack = new Stack<string>([material]);
            while (stack.Count > 0)
                if (parentsOf.TryGetValue(stack.Pop(), out var parents))
                    foreach (var p in parents)
                        if (seen.Add(p))
                            stack.Push(p);
            seen.Remove(material);
            return seen;
        }

        HashSet<string> Descendants(string material)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var stack = new Stack<string>([material]);
            while (stack.Count > 0)
                if (childrenOf.TryGetValue(stack.Pop(), out var children))
                    foreach (var c in children)
                        if (seen.Add(c))
                            stack.Push(c);
            seen.Remove(material);
            return seen;
        }

        var impacts = components.Select(c =>
            {
                var ancestors = Ancestors(c);
                var names = supplierSets.TryGetValue(c, out var list) ? list : [];
                return new BomComponentImpact(
                    c, texts.TryGetValue(c, out var t) ? t : string.Empty,
                    parentsOf[c].Count, ancestors.Count, ancestors.Count(tops.Contains),
                    DepthUp(c, new HashSet<string>(StringComparer.OrdinalIgnoreCase)),
                    names.Count, string.Join(", ", names.Take(3)), overdueCount.TryGetValue(c, out var o) ? o : 0);
            })
            .OrderByDescending(x => x.TopProducts).ThenByDescending(x => x.Ancestors).ThenBy(x => x.Material)
            .ToList();
        var impactByMaterial = impacts.ToDictionary(x => x.Material, StringComparer.OrdinalIgnoreCase);

        var products = tops.Select(p =>
            {
                var below = Descendants(p).Where(components.Contains).ToList();
                var levels = below.Count == 0 ? 0 : below.Max(b => DepthBelow(b, p));
                return new BomProductRisk(
                    p, texts.TryGetValue(p, out var t) ? t : string.Empty, below.Count, Math.Max(1, levels),
                    below.Count(b => impactByMaterial[b].SingleSource),
                    below.Count(b => impactByMaterial[b].OverdueItems > 0));
            })
            .OrderByDescending(x => x.RiskComponents).ThenByDescending(x => x.ComponentsBelow).ThenBy(x => x.Material)
            .ToList();

        // Abstand einer Komponente bis zum Endprodukt: kuerzester Weg nach oben.
        int DepthBelow(string component, string top)
        {
            var queue = new Queue<(string, int)>([(component, 0)]);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { component };
            while (queue.Count > 0)
            {
                var (m, d) = queue.Dequeue();
                if (string.Equals(m, top, StringComparison.OrdinalIgnoreCase))
                    return d;
                if (parentsOf.TryGetValue(m, out var ps))
                    foreach (var p in ps)
                        if (seen.Add(p))
                            queue.Enqueue((p, d + 1));
            }
            return 0;
        }

        return new BomInheritanceResult
        {
            LoadedAtUtc = loadedAt,
            Pairs = pairs.Count,
            Components = components.Count,
            Parents = allParents.Count,
            Intermediates = allParents.Count(components.Contains),
            TopProducts = tops.Count,
            MaxDepth = impacts.Count == 0 ? 0 : impacts.Max(x => x.Depth),
            ComponentImpacts = impacts,
            ProductRisks = products,
            ParentsOf = parentsOf,
            Texts = texts
        };
    }

    internal static string Normalize(string? material) => (material ?? string.Empty).Trim().TrimStart('0');

    private static async Task<string> ScalarAsync(SqliteConnection conn, string sql, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        return Convert.ToString(await cmd.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture) ?? string.Empty;
    }
}
