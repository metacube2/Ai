using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

/// <summary>Verfahren einer Mapping-Zeile (Plan Abschnitt 5, Stufen 2 bis 4).</summary>
public static class MaterialMappingMethods
{
    /// <summary>Stufe 2, feste Regel: wirkt sofort (Entscheid Ingo 19.11.2026, E5).</summary>
    public const string Rule = "Regel";
    /// <summary>Stufe 3, Textaehnlichkeit: wirkt nur im Status bestaetigt.</summary>
    public const string Text = "Text";
    /// <summary>Stufe 4, von Hand (Gesellschaft oder Finance): wirkt im Status bestaetigt.</summary>
    public const string Manual = "Hand";

    public static string Normalize(string? value)
        => value?.Trim().ToUpperInvariant() switch
        {
            "REGEL" or "RULE" => Rule,
            "TEXT" => Text,
            _ => Manual
        };
}

/// <summary>Status einer Mapping-Zeile. Die Texte stehen so im Excel und auf der Seite; nicht umbenennen.</summary>
public static class MaterialMappingStatuses
{
    public const string Proposed = "Vorschlag";
    public const string Confirmed = "bestaetigt";
    public const string Rejected = "abgelehnt";

    public static readonly IReadOnlyList<string> All = [Proposed, Confirmed, Rejected];

    /// <summary>Liest auch Schreibweisen mit Umlaut und englische Werte; unbekannt ergibt null.</summary>
    public static string? Normalize(string? value)
        => value?.Trim().ToLowerInvariant() switch
        {
            null or "" or "vorschlag" or "proposed" or "proposal" => Proposed,
            "bestaetigt" or "bestätigt" or "confirmed" => Confirmed,
            "abgelehnt" or "rejected" => Rejected,
            _ => null
        };
}

/// <summary>Wirksamer Treffer der Mapping-Tabelle fuer eine Zeile.</summary>
public sealed record MaterialMappingHit(string GroupMaterialNumber, string Method);

/// <summary>
/// Nachschlagewerk (TSC, normalisierte lokale Nummer) auf die Gruppennummer, nur mit WIRKSAMEN Zeilen. Unveraenderlich,
/// damit Export und Cockpit dieselbe Sicht rechnen.
/// </summary>
public sealed class MaterialMappingLookup
{
    private readonly Dictionary<(string Tsc, string Key), MaterialMappingHit> _hits;

    public MaterialMappingLookup(IEnumerable<MaterialMapping> mappings)
    {
        _hits = new Dictionary<(string, string), MaterialMappingHit>();
        foreach (var mapping in mappings.Where(MaterialMappingStore.IsEffective))
        {
            var key = (NormalizeTsc(mapping.Tsc), MaterialKeyNormalizer.Normalize(mapping.LocalMaterialKey));
            _hits.TryAdd(key, new MaterialMappingHit(mapping.GroupMaterialNumber.Trim(),
                $"{mapping.Method} {mapping.Status}".Trim()));
        }
    }

    public static MaterialMappingLookup Empty { get; } = new([]);

    public int Count => _hits.Count;

    public bool TryResolve(string? tsc, string? localMaterial, out MaterialMappingHit hit)
    {
        var key = MaterialKeyNormalizer.Normalize(localMaterial);
        if (key.Length > 0 && _hits.TryGetValue((NormalizeTsc(tsc), key), out var found))
        {
            hit = found;
            return true;
        }

        hit = null!;
        return false;
    }

    internal static string NormalizeTsc(string? tsc) => tsc?.Trim().ToUpperInvariant() ?? string.Empty;
}

/// <summary>Eine Zeile aus einer importierten Mapping-Excel, mit Zeilennummer fuer Fehlermeldungen.</summary>
public sealed record MaterialMappingImportRow(
    int RowNumber,
    string Tsc,
    string LocalMaterial,
    string LocalText,
    string GroupMaterialNumber,
    string GroupText,
    string DeliveringEntity,
    string DeliveringMaterialNumber,
    string Method,
    string MethodDetail,
    string Confidence,
    string Status,
    string ReviewedBy);

public sealed record MaterialMappingImportResult(
    bool Success, int Inserted, int Updated, int SkippedEmpty, IReadOnlyList<string> Errors);

/// <summary>Gewicht einer lokalen Nummer: Zeilen und Summe Sales Price/Value in Hauswaehrung.</summary>
public sealed record MaterialMappingWeight(int Rows, decimal NetSales, string Currency, string LocalText);

/// <summary>
/// Lesen, Anwenden, Import und Export der Mapping-Tabelle (Entscheide Ingo 19.11.2026: E5, E6). Regeln aus der
/// italienischen Lieferanten-Uebergangsliste uebernommen: nie ueberschreiben, nur fuer den eingetragenen TSC.
/// </summary>
public static class MaterialMappingStore
{
    /// <summary>
    /// E5: Regeltreffer wirken sofort, Text- und Handzeilen nur im Status bestaetigt, abgelehnte nie. Ohne Gruppennummer
    /// wirkt keine Zeile.
    /// </summary>
    public static bool IsEffective(MaterialMapping mapping)
        => !string.IsNullOrWhiteSpace(mapping.GroupMaterialNumber) &&
           !string.Equals(mapping.Status, MaterialMappingStatuses.Rejected, StringComparison.Ordinal) &&
           (string.Equals(mapping.Status, MaterialMappingStatuses.Confirmed, StringComparison.Ordinal) ||
            string.Equals(mapping.Method, MaterialMappingMethods.Rule, StringComparison.Ordinal));

    public static MaterialMappingLookup LoadLookup(AppDbContext db)
        => new(db.MaterialMappings.AsNoTracking().ToList());

    public static async Task<MaterialMappingLookup> LoadLookupAsync(AppDbContext db, CancellationToken cancellationToken = default)
        => new(await db.MaterialMappings.AsNoTracking().ToListAsync(cancellationToken));

    /// <summary>Gewichte je (TSC, normalisierte Nummer) aus den Verkaufszeilen.</summary>
    public static Dictionary<(string Tsc, string Key), MaterialMappingWeight> ComputeWeights(IEnumerable<SalesRecord> records)
        => records
            .Where(record => !string.IsNullOrWhiteSpace(record.Tsc) && MaterialKeyNormalizer.Normalize(record.Material).Length > 0)
            .GroupBy(record => (MaterialMappingLookup.NormalizeTsc(record.Tsc), MaterialKeyNormalizer.Normalize(record.Material)))
            .ToDictionary(
                group => group.Key,
                group => new MaterialMappingWeight(
                    group.Count(),
                    group.Sum(record => record.SalesPriceValue),
                    group.Select(record => record.CompanyCurrency?.Trim() ?? string.Empty)
                        .FirstOrDefault(currency => currency.Length > 0) ?? string.Empty,
                    group.Select(record => record.Name?.Trim() ?? string.Empty)
                        .FirstOrDefault(name => name.Length > 0) ?? string.Empty));

    /// <summary>
    /// Import einer Mapping-Excel. Alles oder nichts: ein Fehler (doppelter Schluessel in der Datei, fremder TSC, fehlende
    /// Nummer, unbekannter Status, Sicherheit ausserhalb 0 bis 100) verwirft die ganze Datei, damit nie ein halber Stand
    /// wirkt. Leere Zeilen werden uebersprungen. Bestehende Zeilen desselben (TSC, Nummer) werden aktualisiert.
    /// </summary>
    public static async Task<MaterialMappingImportResult> ImportAsync(
        AppDbContext db,
        IReadOnlyList<MaterialMappingImportRow> rows,
        IReadOnlySet<string> knownTscs,
        string userName,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var skipped = 0;
        var valid = new List<(MaterialMappingImportRow Row, string Tsc, string Key, string Status, string Method, int Confidence)>();
        var known = knownTscs.Select(MaterialMappingLookup.NormalizeTsc).ToHashSet(StringComparer.Ordinal);

        foreach (var row in rows)
        {
            if (IsEmpty(row))
            {
                skipped++;
                continue;
            }

            var tsc = MaterialMappingLookup.NormalizeTsc(row.Tsc);
            var key = MaterialKeyNormalizer.Normalize(row.LocalMaterial);
            if (tsc.Length == 0 || key.Length == 0)
            {
                errors.Add($"Zeile {row.RowNumber}: TSC und lokale Artikelnummer sind Pflicht.");
                continue;
            }
            if (!known.Contains(tsc))
            {
                errors.Add($"Zeile {row.RowNumber}: TSC '{row.Tsc.Trim()}' ist keine bekannte Gesellschaft.");
                continue;
            }

            var status = MaterialMappingStatuses.Normalize(row.Status);
            if (status is null)
            {
                errors.Add($"Zeile {row.RowNumber}: Status '{row.Status.Trim()}' ist unbekannt (erlaubt: Vorschlag, bestaetigt, abgelehnt).");
                continue;
            }
            if (status != MaterialMappingStatuses.Rejected && string.IsNullOrWhiteSpace(row.GroupMaterialNumber))
            {
                errors.Add($"Zeile {row.RowNumber}: Gruppen-Artikelnummer fehlt.");
                continue;
            }

            var method = MaterialMappingMethods.Normalize(row.Method);
            var confidence = method == MaterialMappingMethods.Text ? 0 : 100;
            if (!string.IsNullOrWhiteSpace(row.Confidence))
            {
                if (!int.TryParse(row.Confidence.Trim(), System.Globalization.NumberStyles.Integer,
                        System.Globalization.CultureInfo.InvariantCulture, out confidence) ||
                    confidence is < 0 or > 100)
                {
                    errors.Add($"Zeile {row.RowNumber}: Sicherheit '{row.Confidence.Trim()}' liegt nicht zwischen 0 und 100.");
                    continue;
                }
            }

            valid.Add((row, tsc, key, status, method, confidence));
        }

        foreach (var duplicate in valid.GroupBy(item => (item.Tsc, item.Key)).Where(group => group.Count() > 1))
            errors.Add($"Doppelter Schluessel {duplicate.Key.Tsc} / {duplicate.Key.Key} in den Zeilen " +
                       string.Join(", ", duplicate.Select(item => item.Row.RowNumber)) + ".");

        if (errors.Count > 0)
            return new MaterialMappingImportResult(false, 0, 0, skipped, errors);

        var existing = await db.MaterialMappings.ToListAsync(cancellationToken);
        var byKey = existing.ToDictionary(mapping => (MaterialMappingLookup.NormalizeTsc(mapping.Tsc), mapping.LocalMaterialKey));
        var inserted = 0;
        var updated = 0;
        foreach (var item in valid)
        {
            if (!byKey.TryGetValue((item.Tsc, item.Key), out var mapping))
            {
                mapping = new MaterialMapping { Tsc = item.Tsc, LocalMaterialKey = item.Key, CreatedAtUtc = nowUtc };
                db.MaterialMappings.Add(mapping);
                byKey[(item.Tsc, item.Key)] = mapping;
                inserted++;
            }
            else
            {
                updated++;
            }

            var statusChanged = !string.Equals(mapping.Status, item.Status, StringComparison.Ordinal);
            mapping.LocalMaterial = item.Row.LocalMaterial.Trim();
            mapping.LocalText = item.Row.LocalText.Trim();
            mapping.GroupMaterialNumber = item.Row.GroupMaterialNumber.Trim();
            mapping.GroupText = item.Row.GroupText.Trim();
            mapping.DeliveringEntity = item.Row.DeliveringEntity.Trim();
            mapping.DeliveringMaterialNumber = item.Row.DeliveringMaterialNumber.Trim();
            mapping.Method = item.Method;
            mapping.MethodDetail = item.Row.MethodDetail.Trim();
            mapping.Confidence = item.Confidence;
            mapping.Status = item.Status;
            if (item.Status != MaterialMappingStatuses.Proposed)
            {
                var reviewer = item.Row.ReviewedBy.Trim();
                if (reviewer.Length > 0)
                    mapping.ReviewedBy = reviewer;
                else if (statusChanged || mapping.ReviewedBy.Length == 0)
                    mapping.ReviewedBy = userName;
                if (statusChanged || mapping.ReviewedAtUtc is null)
                    mapping.ReviewedAtUtc = nowUtc;
            }
            mapping.UpdatedAtUtc = nowUtc;
        }

        await db.SaveChangesAsync(cancellationToken);
        return new MaterialMappingImportResult(true, inserted, updated, skipped, []);
    }

    /// <summary>Setzt den Status einer Zeile (Seite: Bestaetigen/Ablehnen) mit Pruefer und Zeitpunkt.</summary>
    public static async Task<bool> SetStatusAsync(
        AppDbContext db, int id, string status, string userName, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var normalized = MaterialMappingStatuses.Normalize(status) ?? throw new ArgumentException($"Unbekannter Status '{status}'.");
        var mapping = await db.MaterialMappings.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (mapping is null)
            return false;
        if (normalized == MaterialMappingStatuses.Confirmed && string.IsNullOrWhiteSpace(mapping.GroupMaterialNumber))
            throw new InvalidOperationException("Eine Zeile ohne Gruppen-Artikelnummer kann nicht bestaetigt werden.");

        mapping.Status = normalized;
        mapping.ReviewedBy = normalized == MaterialMappingStatuses.Proposed ? string.Empty : userName;
        mapping.ReviewedAtUtc = normalized == MaterialMappingStatuses.Proposed ? null : nowUtc;
        mapping.UpdatedAtUtc = nowUtc;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Uebernimmt Vorschlaege aus dem Vorschlagslauf. Zeilen mit Status bestaetigt oder abgelehnt und Handzeilen werden NIE
    /// angefasst; bestehende Vorschlaege werden durch den neuen Lauf ersetzt (aktuelle Sicherheit, aktuelles Gewicht).
    /// </summary>
    public static async Task<(int Inserted, int Updated, int Protected)> UpsertSuggestionsAsync(
        AppDbContext db, IReadOnlyList<MaterialMapping> suggestions, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var existing = await db.MaterialMappings.ToListAsync(cancellationToken);
        var byKey = existing.ToDictionary(mapping => (MaterialMappingLookup.NormalizeTsc(mapping.Tsc), mapping.LocalMaterialKey));
        int inserted = 0, updated = 0, protectedRows = 0;
        foreach (var suggestion in suggestions)
        {
            var key = (MaterialMappingLookup.NormalizeTsc(suggestion.Tsc), MaterialKeyNormalizer.Normalize(suggestion.LocalMaterialKey));
            if (byKey.TryGetValue(key, out var mapping))
            {
                if (mapping.Status != MaterialMappingStatuses.Proposed || mapping.Method == MaterialMappingMethods.Manual)
                {
                    protectedRows++;
                    continue;
                }

                Copy(suggestion, mapping);
                mapping.UpdatedAtUtc = nowUtc;
                updated++;
                continue;
            }

            var created = new MaterialMapping { Tsc = key.Item1, LocalMaterialKey = key.Item2, CreatedAtUtc = nowUtc, UpdatedAtUtc = nowUtc };
            Copy(suggestion, created);
            db.MaterialMappings.Add(created);
            byKey[key] = created;
            inserted++;
        }

        await db.SaveChangesAsync(cancellationToken);
        return (inserted, updated, protectedRows);
    }

    private static void Copy(MaterialMapping source, MaterialMapping target)
    {
        target.LocalMaterial = source.LocalMaterial;
        target.LocalText = source.LocalText;
        target.GroupMaterialNumber = source.GroupMaterialNumber;
        target.GroupText = source.GroupText;
        target.DeliveringEntity = source.DeliveringEntity;
        target.DeliveringMaterialNumber = source.DeliveringMaterialNumber;
        target.Method = source.Method;
        target.MethodDetail = source.MethodDetail;
        target.Confidence = source.Confidence;
        target.Status = MaterialMappingStatuses.Proposed;
        target.Rows = source.Rows;
        target.NetSales = source.NetSales;
        target.Currency = source.Currency;
    }

    private static bool IsEmpty(MaterialMappingImportRow row)
        => string.IsNullOrWhiteSpace(row.Tsc) &&
           string.IsNullOrWhiteSpace(row.LocalMaterial) &&
           string.IsNullOrWhiteSpace(row.GroupMaterialNumber) &&
           string.IsNullOrWhiteSpace(row.LocalText) &&
           string.IsNullOrWhiteSpace(row.Status);
}

/// <summary>
/// Excel der Mapping-Tabelle: dieselbe Datei fuer den Export neben Sales_All, den Download auf der Seite und den Import.
/// Spalten nach Plan Abschnitt 5 und Auftrag Ingo 19.11.2026. Nummern werden als Text geschrieben (fuehrende Nullen).
/// </summary>
public static class MaterialMappingExcel
{
    public const string SheetName = "Mapping";

    public static readonly string[] Headers =
    [
        "TSC", "Local Material", "Local Text", "Group Material Number", "Text Trafag AG",
        "Delivering Entity", "Delivering Material Number", "Method", "Method Detail", "Confidence",
        "Status", "Reviewed By", "Reviewed At", "Effective", "Rows", "Net Sales (local currency)", "Currency"
    ];

    /// <summary>
    /// Arbeitsmappe aus den Tabellenzeilen. <paramref name="weights"/> ersetzt die gespeicherten Gewichte, wenn vorhanden
    /// (Export neben Sales_All: Gewicht aus demselben Datenstand).
    /// </summary>
    public static byte[] CreateWorkbookBytes(
        IEnumerable<MaterialMapping> mappings,
        IReadOnlyDictionary<(string Tsc, string Key), MaterialMappingWeight>? weights = null)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add(SheetName);
        for (var i = 0; i < Headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = Headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
        }

        var ordered = mappings
            .Select(mapping =>
            {
                var weight = weights is not null &&
                             weights.TryGetValue((MaterialMappingLookup.NormalizeTsc(mapping.Tsc), mapping.LocalMaterialKey), out var w)
                    ? w
                    : new MaterialMappingWeight(mapping.Rows, mapping.NetSales, mapping.Currency, mapping.LocalText);
                return (Mapping: mapping, Weight: weight);
            })
            .OrderBy(item => item.Mapping.Tsc, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(item => Math.Abs(item.Weight.NetSales))
            .ThenBy(item => item.Mapping.LocalMaterialKey, StringComparer.Ordinal);

        var row = 2;
        foreach (var (mapping, weight) in ordered)
        {
            SetText(ws.Cell(row, 1), mapping.Tsc);
            SetText(ws.Cell(row, 2), mapping.LocalMaterial.Length > 0 ? mapping.LocalMaterial : mapping.LocalMaterialKey);
            SetText(ws.Cell(row, 3), mapping.LocalText.Length > 0 ? mapping.LocalText : weight.LocalText);
            SetText(ws.Cell(row, 4), mapping.GroupMaterialNumber);
            SetText(ws.Cell(row, 5), mapping.GroupText);
            SetText(ws.Cell(row, 6), mapping.DeliveringEntity);
            SetText(ws.Cell(row, 7), mapping.DeliveringMaterialNumber);
            SetText(ws.Cell(row, 8), mapping.Method);
            SetText(ws.Cell(row, 9), mapping.MethodDetail);
            ws.Cell(row, 10).Value = mapping.Confidence;
            SetText(ws.Cell(row, 11), mapping.Status);
            SetText(ws.Cell(row, 12), mapping.ReviewedBy);
            if (mapping.ReviewedAtUtc is { } reviewedAt)
            {
                ws.Cell(row, 13).Value = reviewedAt.ToLocalTime();
                ws.Cell(row, 13).Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
            }
            SetText(ws.Cell(row, 14), MaterialMappingStore.IsEffective(mapping) ? "ja" : "nein");
            ws.Cell(row, 15).Value = weight.Rows;
            ws.Cell(row, 16).Value = weight.NetSales;
            SetText(ws.Cell(row, 17), weight.Currency);
            row++;
        }

        ws.Column(16).Style.NumberFormat.Format = "#,##0.00";
        ws.Range(1, 1, Math.Max(1, row - 1), Headers.Length).SetAutoFilter();
        ws.SheetView.FreezeRows(1);
        ws.Columns().AdjustToContents(1, Math.Min(row, 500));

        var help = workbook.Worksheets.Add("Hilfe");
        help.Cell(1, 1).Value = "Mapping lokale Artikelnummer auf Gruppen-Artikelnummer (Sachnummer Trafag AG)";
        help.Cell(1, 1).Style.Font.Bold = true;
        var lines = new[]
        {
            "Wirksam (Spalte Effective = ja): Regelzeilen sofort, Text- und Handzeilen nur im Status bestaetigt, abgelehnte nie (Entscheid Ingo 19.11.2026).",
            "Eine Zeile wirkt nur fuer ihren TSC und nur dort, wo die Quelle keine Gruppen-Artikelnummer liefert; sie ueberschreibt nie.",
            "Import: alle Zeilen oder keine. Abgelehnt werden doppelte Schluessel (TSC + lokale Nummer), unbekannte TSC, fehlende Nummern, unbekannter Status.",
            "Status: Vorschlag, bestaetigt oder abgelehnt. Confidence 0 bis 100: 100 bei Regel, sonst Wert der Textaehnlichkeit.",
            "Rows und Net Sales: Gewicht aus dem Datenstand des Exports (Sales Price/Value in Hauswaehrung), damit zuerst das Grosse geprueft wird."
        };
        for (var i = 0; i < lines.Length; i++)
            help.Cell(3 + i, 1).Value = lines[i];
        help.Column(1).Width = 140;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>Liest eine Mapping-Excel (erstes Blatt mit der Kopfzeile). Spalten werden ueber den Kopf gefunden.</summary>
    public static IReadOnlyList<MaterialMappingImportRow> Parse(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.FirstOrDefault(sheet => sheet.Name == SheetName) ?? workbook.Worksheets.First();
        var headerRow = ws.Row(1);
        var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerRow.CellsUsed())
            columns.TryAdd(cell.GetString().Trim(), cell.Address.ColumnNumber);

        foreach (var required in new[] { "TSC", "Local Material", "Group Material Number", "Status" })
            if (!columns.ContainsKey(required))
                throw new InvalidOperationException($"Spalte '{required}' fehlt in der Kopfzeile.");

        string Read(IXLRow row, string header)
            => columns.TryGetValue(header, out var column) ? row.Cell(column).GetFormattedString().Trim() : string.Empty;

        var result = new List<MaterialMappingImportRow>();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
        for (var number = 2; number <= lastRow; number++)
        {
            var row = ws.Row(number);
            result.Add(new MaterialMappingImportRow(
                number,
                Read(row, "TSC"),
                Read(row, "Local Material"),
                Read(row, "Local Text"),
                Read(row, "Group Material Number"),
                Read(row, "Text Trafag AG"),
                Read(row, "Delivering Entity"),
                Read(row, "Delivering Material Number"),
                Read(row, "Method"),
                Read(row, "Method Detail"),
                Read(row, "Confidence"),
                Read(row, "Status"),
                Read(row, "Reviewed By")));
        }

        return result;
    }

    private static void SetText(IXLCell cell, string? value)
    {
        cell.Value = value ?? string.Empty;
        cell.Style.NumberFormat.Format = "@";
    }
}
