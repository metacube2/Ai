using System.Data;
using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;

namespace TrafagSalesExporter.Services;

/// <summary>Ein Knoten des Stuecklisten-Netzes (kompakt fuer den Browser).</summary>
public sealed record BomBrainNode(string Id, string Text, string Lz, int LzSort, int Risk, int Role, int Parents, int TopProducts);

/// <summary>Netz fuer die Darstellung: Knoten, Kanten Eltern -> Kind (Indizes), LZ-Codes in Sortierfolge.</summary>
public sealed record BomBrainData(IReadOnlyList<BomBrainNode> Nodes, IReadOnlyList<int[]> Links, IReadOnlyList<string> LzCodes, DateTime? LoadedAtUtc);

/// <summary>
/// Stuecklisten-Netz als „Gehirn“ (Wunsch Ingo 2026-10-16, Prototyp im Einkauf > Spend-Aufriss): dieselbe Teilmenge wie
/// „Verwendung &amp; Risiko“ (MaterialParentCache, nur Komponenten mit LZ-Code) plus LZ-Code je Material aus
/// PurchasingMaterialLzCache. Farbe umschaltbar im Browser: LZ-Code, Lieferantenrisiko, Stufe.
/// Risk: 0 unkritisch, 1 eine Quelle, 2 ueberfaellig, 3 kein Einkauf (24 Monate), -1 nur Eltern (nicht bewertet).
/// Role: 0 Endprodukt (nie Komponente), 1 Baugruppe (beides), 2 Komponente (nie Eltern).
/// </summary>
public static class BomBrainGraph
{
    public const string NoCode = "-";

    public static async Task<BomBrainData> LoadAsync(BomInheritanceService inheritance, IDbContextFactory<AppDbContext> dbFactory, CancellationToken ct = default)
    {
        var result = await inheritance.GetAsync(ct);
        var lz = new Dictionary<string, (string Code, int Sort)>(StringComparer.OrdinalIgnoreCase);
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            var conn = (SqliteConnection)db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Matnr, COALESCE(Lzcode,''), COALESCE(Lzsort,'') FROM PurchasingMaterialLzCache";
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var code = reader.GetString(1).Trim();
                if (code.Length == 0)
                    continue;
                var sort = int.TryParse(reader.GetString(2).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var s) ? s : 999;
                lz[BomInheritanceService.Normalize(reader.GetString(0))] = (code, sort);
            }
        }
        return Build(result, lz);
    }

    public static BomBrainData Build(BomInheritanceResult result, IReadOnlyDictionary<string, (string Code, int Sort)> lz)
    {
        var impacts = result.ComponentImpacts.ToDictionary(x => x.Material, StringComparer.OrdinalIgnoreCase);
        var components = result.ParentsOf.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var parents = result.ParentsOf.Values.SelectMany(x => x).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var ids = components.Concat(parents).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var nodes = new List<BomBrainNode>(ids.Count);
        foreach (var id in ids)
        {
            var isComponent = components.Contains(id);
            var isParent = parents.Contains(id);
            var role = !isComponent ? 0 : isParent ? 1 : 2;
            var risk = -1;
            var parentCount = 0;
            var tops = 0;
            if (impacts.TryGetValue(id, out var impact))
            {
                risk = impact.OverdueItems > 0 ? 2 : impact.SingleSource ? 1 : impact.NotPurchased ? 3 : 0;
                parentCount = impact.DirectParents;
                tops = impact.TopProducts;
            }
            else if (result.ParentsOf.TryGetValue(id, out var ps))
                parentCount = ps.Count;
            var (code, sort) = lz.TryGetValue(id, out var l) ? l : (NoCode, 1000);
            var text = result.Texts.TryGetValue(id, out var t) ? t : impact?.Text ?? string.Empty;
            index[id] = nodes.Count;
            nodes.Add(new BomBrainNode(id, text, code, sort, risk, role, parentCount, tops));
        }

        var links = new List<int[]>();
        foreach (var (component, ps) in result.ParentsOf)
            foreach (var parent in ps)
                if (index.TryGetValue(parent, out var p) && index.TryGetValue(component, out var c))
                    links.Add([p, c]);

        var codes = nodes.Where(n => n.Lz != NoCode).GroupBy(n => n.Lz, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Min(n => n.LzSort)).ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Key).ToList();
        return new BomBrainData(nodes, links, codes, result.LoadedAtUtc);
    }
}
