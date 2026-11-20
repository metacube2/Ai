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
    public const string RuleDeSlash = "DE Typencode/Sachnummer";

    // Nummern nach diesen Woertern sind keine Sachnummer DIESES Artikels (alte Ausfuehrung, Nachfolger, Kundennummer).
    // Fehltreffer 19.11.2026: TRDE 8256.29.2315/60077 nahm 47269 hinter „ersetzt“.
    private static readonly Regex ExcludingPrefix = new(
        @"(?:\bersetzt(?:\s+durch)?|\bersatz\s+f(?:ue|ü)r|\balt|\bfr(?:ue|ü)her|\breplace[sd]?(?:\s+by)?|\bsuperseded(?:\s+by)?|\bold|" +
        @"\bkunden[\w-]*|\bkd\.?\s*-?\s*nr|\bart\.?\s*-?\s*nr|\bartikel-?\s*nr|\bcustomer[\w\s-]*no)\.?\s*[:.\-]?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    public const string TextSimilarity = "Textaehnlichkeit (Trigramme)";

    // Kandidat fuer eine Sachnummer: mindestens 4 Ziffern, optional mit Punkten/Bindestrichen gegliedert (53513,
    // 008896.10.10, 1234-56). Rein alphabetische Woerter sind nie eine Sachnummer.
    private static readonly Regex NumberToken = new(@"(?<![A-Za-z0-9])\d[\d.\-]{3,}\d(?![A-Za-z0-9])", RegexOptions.Compiled);
    private static readonly Regex WordToken = new(@"[A-Za-z0-9]+", RegexOptions.Compiled);

    public static IReadOnlyList<MaterialMapping> Suggest(
        IEnumerable<MaterialMappingCandidate> candidates,
        IReadOnlyDictionary<string, string> trAgMaterials,
        DateTime nowUtc,
        IReadOnlyDictionary<string, string>? trItMaterials = null)
    {
        var index = new TextIndex(trAgMaterials);
        // Paket B (Plan docs/FINANCE_PLAN_MAPPING_STUFE2_2026-11-20.md, 20.11.2026): Ausloeser Andreas 18.11., Italien liefert an
        // Spanien/Frankreich/UK Artikel, die Trafag AG nicht kennt. Fuer Kandidaten mit liefernder Gesellschaft TR IT zuerst
        // gegen die Artikel von TR IT (nur solche mit Kosten), nur als Textvorschlag.
        var itIndex = trItMaterials is { Count: > 0 } ? new TextIndex(trItMaterials) : null;
        var result = new List<MaterialMapping>();
        foreach (var candidate in candidates
                     .Where(item => !MaterialMappingStore.IsExcludedTsc(item.Tsc))
                     .OrderBy(item => item.Tsc, StringComparer.Ordinal)
                     .ThenBy(item => MaterialKeyNormalizer.Normalize(item.LocalMaterial), StringComparer.Ordinal))
        {
            var localKey = MaterialKeyNormalizer.Normalize(candidate.LocalMaterial);
            if (localKey.Length == 0 || trAgMaterials.ContainsKey(localKey))
                continue;

            var numberHits = NumbersInText(candidate.LocalText ?? string.Empty)
                .Where(key => key != localKey && trAgMaterials.ContainsKey(key))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            MaterialMapping? suggestion = null;
            if (itIndex is not null && trItMaterials is not null &&
                GroupMarginCalculator.DeliveringEntityFromLabel(candidate.DeliveringEntity) == GroupStandardCostEntities.TrIt &&
                !trItMaterials.ContainsKey(localKey) &&
                itIndex.FindBest(candidate.LocalText ?? string.Empty) is { } itHit && itHit.Confidence >= MinimumTextConfidence)
            {
                result.Add(CreateDelivering(candidate, localKey, GroupMarginCalculator.DeliveringEntityLabel(GroupStandardCostEntities.TrIt),
                    itHit.MaterialKey, trItMaterials[itHit.MaterialKey], itHit.Confidence, nowUtc));
                continue;
            }

            var slashKey = DeSlashKey(candidate.Tsc, candidate.LocalMaterial);
            if (slashKey is not null && trAgMaterials.ContainsKey(slashKey))
            {
                // Entscheid Ingo 19.11.2026 14:20: TRDE fuehrt Nummern als Typencode/Sachnummer (8258.85.2317/55441).
                suggestion = Create(candidate, localKey, slashKey, trAgMaterials[slashKey],
                    MaterialMappingMethods.Rule, RuleDeSlash, 100, nowUtc);
            }
            else if (numberHits.Count == 1)
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

    /// <summary>
    /// Sachnummer-Kandidaten im Text, ohne Nummern hinter „ersetzt“, „alt“, „replaces“, Kunden-Artikelnummern usw.
    /// Massgeblich ist der Text zwischen der vorigen Nummer (bzw. dem Textanfang) und dieser Nummer.
    /// </summary>
    public static IEnumerable<string> NumbersInText(string text)
    {
        var previousEnd = 0;
        foreach (Match match in NumberToken.Matches(text))
        {
            var before = text[previousEnd..match.Index];
            previousEnd = match.Index + match.Length;
            if (ExcludingPrefix.IsMatch(before))
                continue;
            var key = MaterialKeyNormalizer.Normalize(match.Value);
            if (key.Length > 0)
                yield return key;
        }
    }

    /// <summary>TRDE: Nummer nach dem letzten Schraegstrich der lokalen Nummer, normalisiert; sonst null.</summary>
    public static string? DeSlashKey(string? tsc, string? localMaterial)
    {
        if (!string.Equals(tsc?.Trim(), "TRDE", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(localMaterial))
            return null;
        var slash = localMaterial.LastIndexOf('/');
        if (slash <= 0 || slash == localMaterial.Length - 1)
            return null;
        var key = MaterialKeyNormalizer.Normalize(localMaterial[(slash + 1)..]);
        return key.Length == 0 ? null : key;
    }

    public const string TextSimilarityTrIt = "Textaehnlichkeit (Trigramme) gegen TR IT";

    /// <summary>Paket B: Vorschlag auf die Artikelnummer der liefernden Gesellschaft, Gruppennummer bleibt leer.</summary>
    private static MaterialMapping CreateDelivering(
        MaterialMappingCandidate candidate, string localKey, string entityLabel, string deliveringKey, string deliveringText,
        int confidence, DateTime nowUtc)
    {
        var mapping = Create(candidate, localKey, string.Empty, deliveringText, MaterialMappingMethods.Text, TextSimilarityTrIt,
            confidence, nowUtc);
        mapping.DeliveringEntity = entityLabel;
        mapping.DeliveringMaterialNumber = deliveringKey;
        return mapping;
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
    int TextsLoaded = 0,
    // Zaehlung Regel DE Typencode/Sachnummer (Entscheid Ingo 19.11.2026 14:20): Treffer und deren Umsatz (Hauswaehrung EUR).
    int DeSlashRuleHits = 0,
    decimal DeSlashRuleNetSales = 0m);

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

        var financeRules = await db.FinanceRules.AsNoTracking().Where(rule => rule.IsActive).ToListAsync(cancellationToken);
        var rules = financeRules.Count == 0
            ? FinanceRuleEngine.CreateDefaultRules()
            : financeRules.OrderBy(rule => rule.SortOrder).ThenBy(rule => rule.Id).ToList();
        var candidates = BuildCandidates(records, costs, plantKeys, settings, foreign, mappings, rules);
        var now = DateTime.UtcNow;
        // Entscheid Ingo 19.11.2026 14:20: Regelzeilen wirken nur bestaetigt; nie geprueft bestaetigte zurueck auf Vorschlag.
        await MaterialMappingStore.ResetUnreviewedRuleRowsAsync(db, now, cancellationToken);
        var suggestions = MaterialMappingSuggester.Suggest(candidates, plant, now, BuildTrItMaterials(records, costs));
        var deSlash = suggestions.Where(item => item.MethodDetail == MaterialMappingSuggester.RuleDeSlash).ToList();
        var (inserted, updated, protectedRows) = await MaterialMappingStore.UpsertSuggestionsAsync(db, suggestions, now, cancellationToken);
        // Auftrag Ingo 19.11.2026: alle Artikel mit Mapping-Bedarf sichtbar, auch ohne Vorschlag (Status offen).
        await MaterialMappingStore.SyncOpenRowsAsync(db, candidates, now, cancellationToken);
        // Entscheid Ingo 19.11.2026: TRCH/TRAT sind kein Mapping-Fall; alte Zeilen wegraeumen (bestaetigte/Hand bleiben, wirken nie).
        await MaterialMappingStore.RemoveExcludedTscRowsAsync(db, cancellationToken);
        return new MaterialMappingSuggestionRunResult(
            candidates.Count, suggestions.Count, inserted, updated, protectedRows, plant.Count, plant.Values.Count(text => text.Length > 0),
            textWarning, textsLoaded, deSlash.Count, deSlash.Sum(item => item.NetSales));
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

    /// <summary>
    /// Paket B: Artikel von TR IT als Ziel fuer den Textvorschlag, Schluessel wie im Kostenaufbau (Gruppennummer, sonst Material),
    /// Text aus den eigenen Verkaufszeilen. Nur Artikel mit positiven Kosten TR IT, sonst braechte ein Mapping keine Kosten.
    /// </summary>
    public static IReadOnlyDictionary<string, string> BuildTrItMaterials(
        IEnumerable<SalesRecord> records,
        IReadOnlyDictionary<(string MaterialKey, string ValuationArea), GroupStandardCost> costs)
    {
        var area = GroupStandardCostAreas.ByEntity[GroupStandardCostEntities.TrIt];
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var record in records.Where(record => string.Equals(record.Tsc?.Trim(), "TRIT", StringComparison.OrdinalIgnoreCase)))
        {
            var key = MaterialKeyNormalizer.Normalize(string.IsNullOrWhiteSpace(record.GroupMaterialNumber)
                ? record.Material
                : record.GroupMaterialNumber);
            var text = record.Name?.Trim() ?? string.Empty;
            if (key.Length == 0 || text.Length == 0 || result.ContainsKey(key))
                continue;
            if (costs.TryGetValue((key, area), out var cost) && cost.UnitCost > 0m)
                result[key] = text;
        }

        return result;
    }

    /// <summary>Artikel mit „nicht gefunden“ je (TSC, Nummer), mit Gewicht. Getrennt, damit testbar ohne Datenbank.</summary>
    public static List<MaterialMappingCandidate> BuildCandidates(
        IEnumerable<SalesRecord> records,
        IReadOnlyDictionary<(string MaterialKey, string ValuationArea), GroupStandardCost> costs,
        IReadOnlySet<string> plantKeys,
        ExportSettings settings,
        IReadOnlySet<string> foreignProcurementMaterialKeys,
        MaterialMappingLookup mappings,
        IReadOnlyList<FinanceRule>? financeRules = null)
    {
        // Mit Finance-Regeln: nur Zeilen, die auch in Sales_All ein Issue Topic tragen (Finance | Include = TRUE),
        // Gewicht = Finance Net Sales Actual. Ohne Regeln (Tests): alle Zeilen mit Sales Price/Value.
        var lines = financeRules is null
            ? records.Select(record => (Record: record, NetSales: record.SalesPriceValue))
            : ExcelExportService.FinanceIncludedRecords(records, financeRules);
        var hits = new List<(SalesRecord Record, string Entity, decimal NetSales)>();
        foreach (var (record, netSales) in lines)
        {
            if (string.IsNullOrWhiteSpace(record.Tsc) || MaterialKeyNormalizer.Normalize(record.Material).Length == 0 ||
                MaterialMappingStore.IsExcludedTsc(record.Tsc))
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
                    NetSalesValue = netSales
                },
                costs,
                chPlantMaterialKeys: plantKeys,
                supplierFallbackMode: settings.SupplierFallbackMode,
                internalSupplierCostSourceMode: settings.InternalSupplierCostSourceMode,
                foreignProcurementMaterialKeys: foreignProcurementMaterialKeys,
                marcForeignProcurementMode: settings.MarcForeignProcurementMode,
                materialMappings: mappings);
            if (NotFoundTopics.Contains(evaluation.IssueTopic))
                hits.Add((record, GroupMarginCalculator.DeliveringEntityLabel(evaluation.DeliveringEntity), netSales));
        }

        return hits
            .GroupBy(hit => (MaterialMappingLookup.NormalizeTsc(hit.Record.Tsc), MaterialKeyNormalizer.Normalize(hit.Record.Material)))
            .Select(group => new MaterialMappingCandidate(
                group.Key.Item1,
                group.First().Record.Material.Trim(),
                group.Select(hit => hit.Record.Name?.Trim() ?? string.Empty).FirstOrDefault(name => name.Length > 0) ?? string.Empty,
                group.Select(hit => hit.Entity).FirstOrDefault(entity => entity.Length > 0) ?? string.Empty,
                group.Count(),
                group.Sum(hit => hit.NetSales),
                group.Select(hit => hit.Record.CompanyCurrency?.Trim() ?? string.Empty).FirstOrDefault(currency => currency.Length > 0) ?? string.Empty))
            .ToList();
    }
}
