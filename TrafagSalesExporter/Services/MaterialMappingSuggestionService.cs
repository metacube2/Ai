using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services.DataSources;

namespace TrafagSalesExporter.Services;

/// <summary>Ein Artikel, fuer den ein Mapping gesucht wird (Issue Topic 1, 2, 6 oder 7: bei der liefernden Gesellschaft nicht gefunden).</summary>
public sealed record MaterialMappingCandidate(
    string Tsc,
    string LocalMaterial,
    string LocalText,
    string DeliveringEntity,
    int Rows,
    decimal NetSales,
    string Currency);

/// <summary>
/// Vorschlagslauf des Mappings (AP3, Plan Abschnitt 5), rein und deterministisch: kein Sprachmodell, kein Netzaufruf,
/// gleiche Eingabe ergibt gleiche Vorschlaege. Laeuft nur per Knopf auf der Mapping-Seite, nie im taeglichen Export.
///
/// Stufe 1 (Nummer normalisiert gleich) braucht keine Zeile und ist im Kandidatenkreis schon ausgeschlossen. Stufe 2,
/// die festen Schluesselregeln I-Praefix (TRIT) und indische Artikelnummer, wirkt direkt in der Rechnung und erzeugt
/// ebenfalls keine Zeile. Hier entstehen:
/// <list type="bullet">
/// <item>Regel „Trafag-Nummer im Text“: der lokale Artikeltext enthaelt GENAU EINE Nummer, die im CH-Werkstamm steht
/// (Deutschland fuehrt laut Besprechung 18.11. die Trafag-Nummer im Materialtext). Sicherheit 100, wirkt sofort (E5).</item>
/// <item>Text: unscharfe Aehnlichkeit zwischen lokalem Text und deutschem Materialtext Trafag AG (Zeichen-Trigramme,
/// Dice-Koeffizient, Kandidaten ueber gemeinsame Woerter). Sicherheit = Aehnlichkeit in Prozent; wirkt erst bestaetigt.</item>
/// </list>
/// </summary>
public static class MaterialMappingSuggester
{
    /// <summary>Text-Vorschlaege unter dieser Sicherheit werden nicht ausgegeben (zu unsicher, nur Laerm fuer die Pruefer).</summary>
    public const int MinimumTextConfidence = 40;

    public const string RuleNumberInText = "Trafag-Nummer im Text";
    public const string TextSimilarity = "Textaehnlichkeit (Trigramme)";

    // Kandidat fuer eine Sachnummer: mindestens 4 Ziffern, optional mit Punkten/Bindestrichen gegliedert (53513,
    // 008896.10.10, 1234-56). Rein alphabetische Woerter sind nie eine Sachnummer.
    private static readonly Regex NumberToken = new(@"(?<![A-Za-z0-9])\d[\d.\-]{3,}\d(?![A-Za-z0-9])", RegexOptions.Compiled);
    private static readonly Regex WordToken = new(@"[A-Za-z0-9]+", RegexOptions.Compiled);

    public static IReadOnlyList<MaterialMapping> Suggest(
        IEnumerable<MaterialMappingCandidate> candidates,
        IReadOnlyDictionary<string, string> trAgMaterials,
        DateTime nowUtc)
    {
        var index = new TextIndex(trAgMaterials);
        var result = new List<MaterialMapping>();
        foreach (var candidate in candidates
                     .OrderBy(item => item.Tsc, StringComparer.Ordinal)
                     .ThenBy(item => MaterialKeyNormalizer.Normalize(item.LocalMaterial), StringComparer.Ordinal))
        {
            var localKey = MaterialKeyNormalizer.Normalize(candidate.LocalMaterial);
            if (localKey.Length == 0 || trAgMaterials.ContainsKey(localKey))
                continue;

            var numberHits = NumberToken.Matches(candidate.LocalText ?? string.Empty)
                .Select(match => MaterialKeyNormalizer.Normalize(match.Value))
                .Where(key => key.Length > 0 && key != localKey && trAgMaterials.ContainsKey(key))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            MaterialMapping? suggestion = null;
            if (numberHits.Count == 1)
            {
                suggestion = Create(candidate, localKey, numberHits[0], trAgMaterials[numberHits[0]],
                    MaterialMappingMethods.Rule, RuleNumberInText, 100, nowUtc);
            }
            else
            {
                var best = index.FindBest(candidate.LocalText ?? string.Empty);
                if (best is { } hit && hit.Confidence >= MinimumTextConfidence)
                {
                    var detail = numberHits.Count > 1
                        ? $"{TextSimilarity}; mehrere Trafag-Nummern im Text: {string.Join(", ", numberHits)}"
                        : TextSimilarity;
                    suggestion = Create(candidate, localKey, hit.MaterialKey, trAgMaterials[hit.MaterialKey],
                        MaterialMappingMethods.Text, detail, hit.Confidence, nowUtc);
                }
            }

            if (suggestion is not null)
                result.Add(suggestion);
        }

        return result;
    }

    private static MaterialMapping Create(
        MaterialMappingCandidate candidate, string localKey, string groupKey, string groupText,
        string method, string detail, int confidence, DateTime nowUtc)
        => new()
        {
            Tsc = MaterialMappingLookup.NormalizeTsc(candidate.Tsc),
            LocalMaterial = candidate.LocalMaterial.Trim(),
            LocalMaterialKey = localKey,
            LocalText = candidate.LocalText?.Trim() ?? string.Empty,
            GroupMaterialNumber = groupKey,
            GroupText = groupText,
            DeliveringEntity = candidate.DeliveringEntity,
            Method = method,
            MethodDetail = detail,
            Confidence = confidence,
            Status = MaterialMappingStatuses.Proposed,
            Rows = candidate.Rows,
            NetSales = candidate.NetSales,
            Currency = candidate.Currency,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };

    /// <summary>Normalisiert Text fuer den Vergleich: Grossbuchstaben, Umlaute ausgeschrieben, nur Buchstaben/Ziffern/Leerzeichen.</summary>
    public static string NormalizeText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var upper = text.ToUpperInvariant()
            .Replace("Ä", "AE").Replace("Ö", "OE").Replace("Ü", "UE").Replace("ß", "SS");
        var decomposed = upper.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;
            builder.Append(char.IsLetterOrDigit(ch) ? ch : ' ');
        }

        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>Dice-Koeffizient der Zeichen-Trigramme zweier normalisierter Texte, 0 bis 1.</summary>
    public static double TrigramDice(string normalizedA, string normalizedB)
    {
        var a = Trigrams(normalizedA);
        var b = Trigrams(normalizedB);
        if (a.Count == 0 || b.Count == 0)
            return 0d;
        var common = a.Count(b.Contains);
        return 2d * common / (a.Count + b.Count);
    }

    private static HashSet<string> Trigrams(string normalized)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        var padded = $"  {normalized} ";
        for (var i = 0; i + 3 <= padded.Length; i++)
            set.Add(padded.Substring(i, 3));
        return set;
    }

    /// <summary>Wortindex ueber die Materialtexte Trafag AG, damit nicht jeder Text mit allen 66'000 verglichen wird.</summary>
    private sealed class TextIndex
    {
        private const int MaxPostingsPerWord = 2_000;
        private readonly Dictionary<string, List<string>> _postings = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _normalizedByKey = new(StringComparer.Ordinal);

        public TextIndex(IReadOnlyDictionary<string, string> materials)
        {
            foreach (var (key, text) in materials.OrderBy(item => item.Key, StringComparer.Ordinal))
            {
                var normalized = NormalizeText(text);
                if (normalized.Length == 0)
                    continue;
                _normalizedByKey[key] = normalized;
                foreach (var word in Words(normalized))
                {
                    if (!_postings.TryGetValue(word, out var list))
                        _postings[word] = list = [];
                    list.Add(key);
                }
            }
        }

        public (string MaterialKey, int Confidence)? FindBest(string localText)
        {
            var normalized = NormalizeText(localText);
            if (normalized.Length == 0)
                return null;

            // Sehr haeufige Woerter (z. B. "TRANSMITTER") tragen kaum Information und sprengen die Kandidatenmenge.
            var candidates = Words(normalized)
                .Where(word => _postings.TryGetValue(word, out var list) && list.Count <= MaxPostingsPerWord)
                .SelectMany(word => _postings[word])
                .ToHashSet(StringComparer.Ordinal);

            string? bestKey = null;
            var bestScore = 0d;
            foreach (var key in candidates.OrderBy(key => key, StringComparer.Ordinal))
            {
                var score = TrigramDice(normalized, _normalizedByKey[key]);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestKey = key;
                }
            }

            return bestKey is null ? null : (bestKey, (int)Math.Round(bestScore * 100d, MidpointRounding.AwayFromZero));
        }

        private static IEnumerable<string> Words(string normalized)
            => WordToken.Matches(normalized).Select(match => match.Value).Where(word => word.Length >= 3).Distinct(StringComparer.Ordinal);
    }
}

public interface IMaterialMappingSuggestionService
{
    Task<MaterialMappingSuggestionRunResult> RunAsync(CancellationToken cancellationToken = default);
}

public sealed record MaterialMappingSuggestionRunResult(
    int Candidates, int Suggestions, int Inserted, int Updated, int Protected, int TrAgMaterials, int TrAgTexts,
    // Ergebnis des MAKTSet-Abrufs: null = Texte frisch geladen, sonst die Meldung fuer die Seite (Lauf ging mit dem
    // gespeicherten Stand weiter).
    string? TextRefreshWarning = null,
    int TextsLoaded = 0);

/// <summary>
/// Vorschlagslauf per Knopf: rechnet die zentralen Verkaufszeilen mit denselben Einstellungen wie Sales_All durch, sammelt
/// die Artikel mit Issue Topic 1, 2, 6 oder 7 (nicht gefunden) und schreibt Vorschlaege in die Mapping-Tabelle.
/// Bestaetigte, abgelehnte und Handzeilen bleiben unangetastet.
/// </summary>
public sealed class MaterialMappingSuggestionService : IMaterialMappingSuggestionService
{
    private static readonly HashSet<string> NotFoundTopics = new(StringComparer.Ordinal)
    {
        IssueTopics.NoSupplierNotFound, IssueTopics.SupplierNotFound, IssueTopics.NoSupplierUseZero, IssueTopics.SupplierUseZero
    };

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ICentralSalesDataProvider _centralSalesDataProvider;
    private readonly ISapGatewayMaterialTextReader _textReader;

    // Der Text-Leser ist optional, damit keine weitere Registrierung in Program.cs noetig ist (Datei von der
    // Einkauf-Sitzung reserviert); ohne Registrierung gilt der Gateway-Leser.
    public MaterialMappingSuggestionService(
        IDbContextFactory<AppDbContext> dbFactory,
        ICentralSalesDataProvider centralSalesDataProvider,
        ISapGatewayMaterialTextReader? textReader = null)
    {
        _dbFactory = dbFactory;
        _centralSalesDataProvider = centralSalesDataProvider;
        _textReader = textReader ?? new SapGatewayMaterialTextReader();
    }

    public async Task<MaterialMappingSuggestionRunResult> RunAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var (textsLoaded, textWarning) = await RefreshTrAgTextsAsync(db, cancellationToken);
        var settings = await db.ExportSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken) ?? new ExportSettings();
        var records = settings.UseAuditCsvAsCentralSource
            ? await _centralSalesDataProvider.GetRecordsAsync()
            : await _centralSalesDataProvider.GetLatestRecordsBySiteAsync();

        var costs = (await db.GroupStandardCosts.AsNoTracking().ToListAsync(cancellationToken))
            .GroupBy(cost => (cost.MaterialKey, cost.ValuationArea))
            .ToDictionary(group => group.Key, group => group.First());
        var plant = (await db.GroupMaterialMasters.AsNoTracking().Where(row => row.Plant == "1100").ToListAsync(cancellationToken))
            .GroupBy(row => row.MaterialKey, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().MaterialText ?? string.Empty, StringComparer.Ordinal);
        var plantKeys = plant.Keys.ToHashSet(StringComparer.Ordinal);
        var foreign = ForeignProcurementEvidenceStore.LoadMaterialKeys(db);
        var mappings = await MaterialMappingStore.LoadLookupAsync(db, cancellationToken);

        var candidates = BuildCandidates(records, costs, plantKeys, settings, foreign, mappings);
        var now = DateTime.UtcNow;
        var suggestions = MaterialMappingSuggester.Suggest(candidates, plant, now);
        var (inserted, updated, protectedRows) = await MaterialMappingStore.UpsertSuggestionsAsync(db, suggestions, now, cancellationToken);
        return new MaterialMappingSuggestionRunResult(
            candidates.Count, suggestions.Count, inserted, updated, protectedRows, plant.Count, plant.Values.Count(text => text.Length > 0),
            textWarning, textsLoaded);
    }

    /// <summary>
    /// Entscheid Ingo 2026-11-19: deutsche Materialtexte Trafag AG aus MAKTSet ueber den Gateway-Weg der CH-Site (aktive
    /// Site mit SAP-Gateway-Anschluss, TRCH bevorzugt) holen und in GroupMaterialMasters.MaterialText fuer Werk 1100
    /// schreiben. Faellt der Abruf aus, bleibt der gespeicherte Stand und die Seite zeigt die Meldung.
    /// </summary>
    private async Task<(int Loaded, string? Warning)> RefreshTrAgTextsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        try
        {
            var masters = await db.GroupMaterialMasters.Where(row => row.Plant == "1100").ToListAsync(cancellationToken);
            if (masters.Count == 0)
                return (0, "Materialtexte nicht geladen: der CH-Werkstamm (Werk 1100) ist leer. Zuerst den CH/AT-Export laufen lassen.");

            var sites = await db.Sites.AsNoTracking().Where(site => site.IsActive).ToListAsync(cancellationToken);
            var definitions = await db.SourceSystemDefinitions.AsNoTracking().ToListAsync(cancellationToken);
            var gateway = sites
                .Select(site => (Site: site, Definition: definitions.FirstOrDefault(definition =>
                    string.Equals(definition.Code, site.SourceSystem, StringComparison.OrdinalIgnoreCase))))
                .Where(item => item.Definition is not null &&
                               string.Equals(item.Definition.ConnectionKind, SourceSystemConnectionKinds.SapGateway, StringComparison.OrdinalIgnoreCase) &&
                               !string.IsNullOrWhiteSpace(DataSourceCredentials.ResolveSapServiceUrl(item.Site, item.Definition)))
                .OrderBy(item => string.Equals(item.Site.TSC, "TRCH", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(item => item.Site.Id)
                .FirstOrDefault();
            if (gateway.Site is null)
                return (0, "Materialtexte nicht geladen: keine aktive Site mit SAP-Gateway-Anschluss (CH/AT) gefunden.");

            var serviceUrl = DataSourceCredentials.ResolveSapServiceUrl(gateway.Site, gateway.Definition!);
            var credentials = DataSourceCredentials.Resolve(gateway.Site, gateway.Definition!);
            if (string.IsNullOrWhiteSpace(credentials.Username) || string.IsNullOrWhiteSpace(credentials.Password))
                return (0, $"Materialtexte nicht geladen: fuer {gateway.Site.TSC} fehlen die SAP-Zugangsdaten.");

            var keys = masters.Select(row => row.MaterialKey).ToHashSet(StringComparer.Ordinal);
            var texts = await _textReader.GetGermanTextsAsync(serviceUrl, credentials.Username, credentials.Password, keys, cancellationToken);
            if (texts.Count == 0)
                return (0, "Materialtexte nicht geladen: MAKTSet lieferte keinen deutschen Text zum CH-Werkstamm. Gespeicherter Stand bleibt.");

            var applied = ApplyTexts(masters, texts);
            await db.SaveChangesAsync(cancellationToken);
            return (applied, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return (0, $"Materialtexte nicht geladen (MAKTSet): {ex.Message}. Der Lauf rechnet mit dem gespeicherten Stand.");
        }
    }

    /// <summary>Setzt die Texte; Materialien ohne neuen Text behalten ihren bisherigen. Liefert die Zahl der Materialien mit Text.</summary>
    public static int ApplyTexts(IEnumerable<GroupMaterialMaster> masters, IReadOnlyDictionary<string, string> texts)
    {
        var count = 0;
        foreach (var master in masters)
        {
            if (texts.TryGetValue(master.MaterialKey, out var text) && text.Length > 0)
            {
                if (!string.Equals(master.MaterialText, text, StringComparison.Ordinal))
                    master.MaterialText = text;
                count++;
            }
        }

        return count;
    }

    /// <summary>Artikel mit „nicht gefunden“ je (TSC, Nummer), mit Gewicht. Getrennt, damit testbar ohne Datenbank.</summary>
    public static List<MaterialMappingCandidate> BuildCandidates(
        IEnumerable<SalesRecord> records,
        IReadOnlyDictionary<(string MaterialKey, string ValuationArea), GroupStandardCost> costs,
        IReadOnlySet<string> plantKeys,
        ExportSettings settings,
        IReadOnlySet<string> foreignProcurementMaterialKeys,
        MaterialMappingLookup mappings)
    {
        var hits = new List<(SalesRecord Record, string Entity)>();
        foreach (var record in records)
        {
            if (string.IsNullOrWhiteSpace(record.Tsc) || MaterialKeyNormalizer.Normalize(record.Material).Length == 0)
                continue;

            var evaluation = GroupMarginCalculator.Evaluate(
                new GroupMarginLine
                {
                    SupplierNumber = record.SupplierNumber,
                    SupplierName = record.SupplierName,
                    SupplierCountry = record.SupplierCountry,
                    Tsc = record.Tsc,
                    Material = record.Material,
                    GroupMaterialNumber = record.GroupMaterialNumber,
                    SalesType = record.SalesType,
                    Quantity = record.Quantity,
                    StandardCost = record.StandardCost,
                    StandardCostCurrency = record.StandardCostCurrency,
                    NetSalesValue = record.SalesPriceValue
                },
                costs,
                chPlantMaterialKeys: plantKeys,
                supplierFallbackMode: settings.SupplierFallbackMode,
                internalSupplierCostSourceMode: settings.InternalSupplierCostSourceMode,
                foreignProcurementMaterialKeys: foreignProcurementMaterialKeys,
                marcForeignProcurementMode: settings.MarcForeignProcurementMode,
                materialMappings: mappings);
            if (NotFoundTopics.Contains(evaluation.IssueTopic))
                hits.Add((record, GroupMarginCalculator.DeliveringEntityLabel(evaluation.DeliveringEntity)));
        }

        return hits
            .GroupBy(hit => (MaterialMappingLookup.NormalizeTsc(hit.Record.Tsc), MaterialKeyNormalizer.Normalize(hit.Record.Material)))
            .Select(group => new MaterialMappingCandidate(
                group.Key.Item1,
                group.First().Record.Material.Trim(),
                group.Select(hit => hit.Record.Name?.Trim() ?? string.Empty).FirstOrDefault(name => name.Length > 0) ?? string.Empty,
                group.Select(hit => hit.Entity).FirstOrDefault(entity => entity.Length > 0) ?? string.Empty,
                group.Count(),
                group.Sum(hit => hit.Record.SalesPriceValue),
                group.Select(hit => hit.Record.CompanyCurrency?.Trim() ?? string.Empty).FirstOrDefault(currency => currency.Length > 0) ?? string.Empty))
            .ToList();
    }
}
