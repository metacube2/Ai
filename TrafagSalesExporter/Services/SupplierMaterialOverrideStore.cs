using System.Globalization;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Laedt und wendet die uebergangsweise Lieferantenzuordnung an, siehe
/// <see cref="SupplierMaterialOverride"/>.
///
/// Die Liste liegt als eingebettete Ressource im DLL, nicht als Datei daneben: damit deckt der
/// bestehende Deploy-Nachweis ueber den DLL-Hash sie mit ab und es kann kein Stand entstehen, in
/// dem Programm und Liste auseinanderlaufen.
///
/// Zwei Regeln, die nicht verhandelbar sind:
/// 1. Ueberschrieben wird NIE. Der Ersatzwert greift nur, wenn die Quelle alle drei
///    Lieferantenfelder leer laesst.
/// 2. Der Standort muss uebereinstimmen. Eine TR-IT-Zuordnung wirkt nie auf einen anderen TSC,
///    auch wenn dort dieselbe Materialnummer vorkommt.
/// </summary>
public static class SupplierMaterialOverrideStore
{
    private const string ResourceSuffix = "supplier_overrides_TRIT_2026-08-26.csv";

    /// <summary>Klartextherkunft. Steht in jeder Zeile, damit die Datenlage belegbar bleibt.</summary>
    public const string SourceLabel =
        "Trafag Italia (Paola Castagna), Excel-Rueckmeldung 2026-08-26; B1-Artikelstamm noch nicht gepflegt";

    public sealed record ApplyResult(int RowsFilled, int MaterialsUsed, int RowsLeftEmpty);

    public sealed record SeedResult(bool Changed, int RowCount);

    /// <summary>
    /// Liest die eingebettete Liste. Format: Kopfzeile, danach
    /// <c>Tsc;Material;SupplierNumber;SupplierName;SupplierCountry</c>.
    /// Zeilen ohne Materialnummer oder ohne Lieferantennummer werden verworfen: in der
    /// Rueckmeldung aus Italien betrifft das eine Bonusgutschrift ohne Artikelbezug, die im
    /// Artikelstamm gar nicht existieren kann.
    /// </summary>
    public static IReadOnlyList<SupplierMaterialOverride> LoadEmbedded(DateTime nowUtc)
    {
        var assembly = typeof(SupplierMaterialOverrideStore).Assembly;
        var name = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(ResourceSuffix, StringComparison.OrdinalIgnoreCase));
        if (name is null)
            return Array.Empty<SupplierMaterialOverride>();

        using var stream = assembly.GetManifestResourceStream(name);
        if (stream is null)
            return Array.Empty<SupplierMaterialOverride>();

        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd(), nowUtc);
    }

    /// <summary>Trennt das Einlesen vom Ressourcenzugriff, damit Tests ohne Assembly auskommen.</summary>
    public static IReadOnlyList<SupplierMaterialOverride> Parse(string csv, DateTime nowUtc)
    {
        var result = new List<SupplierMaterialOverride>();
        var seen = new HashSet<(string Tsc, string MaterialKey)>();

        using var reader = new StringReader(csv);
        var isFirst = true;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (isFirst)
            {
                isFirst = false;
                if (line.StartsWith("Tsc;", StringComparison.OrdinalIgnoreCase))
                    continue;
            }

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split(';');
            if (parts.Length < 5)
                continue;

            var tsc = parts[0].Trim().ToUpperInvariant();
            var materialKey = MaterialKeyNormalizer.Normalize(parts[1]);
            var supplierNumber = parts[2].Trim();
            var supplierName = parts[3].Trim();
            var supplierCountry = parts[4].Trim().ToUpperInvariant();

            if (tsc.Length == 0 || materialKey.Length == 0 || supplierNumber.Length == 0)
                continue;

            // Erster Treffer gewinnt; eine doppelte Materialnummer waere ein Fehler in der
            // Rueckmeldung und darf die Zuordnung nicht unbestimmt machen.
            if (!seen.Add((tsc, materialKey)))
                continue;

            result.Add(new SupplierMaterialOverride
            {
                Tsc = tsc,
                MaterialKey = materialKey,
                SupplierNumber = supplierNumber,
                SupplierName = supplierName,
                SupplierCountry = supplierCountry,
                Source = SourceLabel,
                ImportedAtUtc = nowUtc
            });
        }

        return result;
    }

    /// <summary>
    /// Bringt den Datenbankbestand auf den Stand der eingebetteten Liste. Idempotent: steht der
    /// gleiche Bestand schon da, wird nicht geschrieben, damit ein Neustart keine Schreiblast und
    /// keine neuen Zeitstempel erzeugt.
    /// </summary>
    public static async Task<SeedResult> SeedAsync(
        AppDbContext db,
        IReadOnlyList<SupplierMaterialOverride> desired,
        CancellationToken cancellationToken = default)
    {
        var tscs = desired.Select(o => o.Tsc).Distinct().ToList();
        var existing = await db.SupplierMaterialOverrides
            .Where(o => tscs.Contains(o.Tsc))
            .ToListAsync(cancellationToken);

        if (IsSameContent(existing, desired))
            return new SeedResult(false, existing.Count);

        db.SupplierMaterialOverrides.RemoveRange(existing);
        await db.SaveChangesAsync(cancellationToken);

        db.SupplierMaterialOverrides.AddRange(desired);
        await db.SaveChangesAsync(cancellationToken);

        return new SeedResult(true, desired.Count);
    }

    private static bool IsSameContent(
        IReadOnlyCollection<SupplierMaterialOverride> existing,
        IReadOnlyCollection<SupplierMaterialOverride> desired)
    {
        if (existing.Count != desired.Count)
            return false;

        var lookup = existing.ToDictionary(o => (o.Tsc, o.MaterialKey));
        foreach (var wanted in desired)
        {
            if (!lookup.TryGetValue((wanted.Tsc, wanted.MaterialKey), out var found))
                return false;
            if (!string.Equals(found.SupplierNumber, wanted.SupplierNumber, StringComparison.Ordinal) ||
                !string.Equals(found.SupplierName, wanted.SupplierName, StringComparison.Ordinal) ||
                !string.Equals(found.SupplierCountry, wanted.SupplierCountry, StringComparison.Ordinal))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Fuellt die Lieferantenfelder der uebergebenen Zeilen aus dem Datenbankbestand. Aendert nur
    /// Zeilen, in denen alle drei Felder leer sind.
    /// </summary>
    public static async Task<ApplyResult> ApplyAsync(
        AppDbContext db,
        string? tsc,
        IReadOnlyCollection<SalesRecord> records,
        CancellationToken cancellationToken = default)
    {
        var site = tsc?.Trim().ToUpperInvariant() ?? string.Empty;
        if (site.Length == 0 || records.Count == 0)
            return new ApplyResult(0, 0, 0);

        var candidates = records.Where(IsSupplierEmpty).ToList();
        if (candidates.Count == 0)
            return new ApplyResult(0, 0, 0);

        var overrides = await db.SupplierMaterialOverrides
            .Where(o => o.Tsc == site)
            .ToListAsync(cancellationToken);
        if (overrides.Count == 0)
            return new ApplyResult(0, 0, candidates.Count);

        var lookup = overrides
            .GroupBy(o => o.MaterialKey)
            .ToDictionary(g => g.Key, g => g.First());

        var filled = 0;
        var usedMaterials = new HashSet<string>();
        foreach (var record in candidates)
        {
            var key = MaterialKeyNormalizer.Normalize(record.Material);
            if (key.Length == 0 || !lookup.TryGetValue(key, out var match))
                continue;

            record.SupplierNumber = match.SupplierNumber;
            record.SupplierName = match.SupplierName;
            record.SupplierCountry = match.SupplierCountry;
            filled++;
            usedMaterials.Add(key);
        }

        return new ApplyResult(filled, usedMaterials.Count, candidates.Count - filled);
    }

    /// <summary>
    /// Der Kernbefund aus docs/FINANCE_SUPPLIER.md Abschnitt 2: die drei Felder kommen immer
    /// gemeinsam oder gar nicht. Ein Ersatzwert darf deshalb nur greifen, wenn alle drei leer
    /// sind, nie um ein einzelnes Feld nachzubessern.
    /// </summary>
    private static bool IsSupplierEmpty(SalesRecord record)
        => string.IsNullOrWhiteSpace(record.SupplierNumber)
           && string.IsNullOrWhiteSpace(record.SupplierName)
           && string.IsNullOrWhiteSpace(record.SupplierCountry);

    /// <summary>Kurzfassung fuer das Ereignisprotokoll.</summary>
    public static string Describe(ApplyResult result)
        => string.Create(CultureInfo.InvariantCulture,
            $"Zeilen ergaenzt={result.RowsFilled} | Materialien={result.MaterialsUsed} | weiterhin ohne Lieferant={result.RowsLeftEmpty}");
}
