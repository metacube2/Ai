using System.Data;
using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Additive Auswertungen ueber die bestehenden Einkaufs- und ZLO03-Caches. Der Dienst schreibt
/// keine Daten und veraendert keine der bestehenden Dashboard-Berechnungen.
/// </summary>
public sealed class SupplyChainAnalysisService : ISupplyChainAnalysisService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public SupplyChainAnalysisService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<SupplyChainAnalysisResult> LoadAsync(
        SupplyChainAnalysisKind kind,
        SupplyChainAnalysisFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        filter ??= new SupplyChainAnalysisFilter();
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var conn = (SqliteConnection)db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(cancellationToken);

        var maps = await LoadProductGroupMapsAsync(conn, cancellationToken);
        var materialFacts = await LoadMaterialFactsAsync(conn, maps, cancellationToken);
        var materialTimestamp = await ReadMaxTimestampAsync(conn, "MaterialUsageCache", cancellationToken);
        var purchasingTimestamp = await ReadMaxTimestampAsync(conn, "PurchasingEkkoCache", cancellationToken);

        List<SupplyChainAnalysisRow> rows;
        var actualReceiptDateAvailable = false;
        string noticeDe;
        string noticeEn;

        switch (kind)
        {
            case SupplyChainAnalysisKind.MaterialDisposition:
            {
                var openOrders = await LoadOpenOrderFactsAsync(conn, cancellationToken);
                rows = materialFacts.Select(fact => BuildDispositionRow(fact, FindOpenOrder(openOrders, fact.MaterialKey))).ToList();
                noticeDe = "Endbestand und Dispositionswerte stammen aus ZMD04_CALC. Offene Bestellungen dienen als Beleg-Drill; sie werden nicht nochmals zum Endbestand addiert.";
                noticeEn = "Final stock and planning values come from ZMD04_CALC. Open orders are a document drill-down and are not added to final stock again.";
                break;
            }
            case SupplyChainAnalysisKind.PurchaseCoverage:
            {
                var openOrders = await LoadOpenOrderFactsAsync(conn, cancellationToken);
                rows = materialFacts.Select(fact => BuildCoverageRow(fact, FindOpenOrder(openOrders, fact.MaterialKey))).ToList();
                noticeDe = "Die Deckungsluecke verwendet den SAP-Endbestand. EKET zeigt dazu offene Bestellmenge und naechsten Plantermin, ohne Zugänge doppelt zu zaehlen.";
                noticeEn = "The coverage gap uses SAP final stock. EKET adds open order quantity and the next planned date without double-counting receipts.";
                break;
            }
            case SupplyChainAnalysisKind.MaterialDependency:
            {
                var dependencies = await LoadDependencyFactsAsync(conn, cancellationToken);
                rows = BuildDependencyRows(materialFacts, dependencies);
                noticeDe = "Lieferantenabhaengigkeit ist historisch beobachtet (EKKO/EKPO), nicht mit einer freigegebenen Bezugsquellenliste gleichzusetzen.";
                noticeEn = "Supplier dependency is historically observed (EKKO/EKPO) and is not equivalent to an approved source list.";
                break;
            }
            case SupplyChainAnalysisKind.PlanningParameterAudit:
                rows = BuildParameterAuditRows(materialFacts);
                noticeDe = "Die Regeln erzeugen Pruefauftraege, aber aendern keine Dispositionsparameter automatisch.";
                noticeEn = "The rules create review tasks but never change planning parameters automatically.";
                break;
            case SupplyChainAnalysisKind.DeliveryPerformance:
            {
                rows = await LoadDeliveryRowsAsync(conn, materialFacts, cancellationToken);
                noticeDe = "Belastbar verfuegbar ist das Plantermin-Risiko aus EKET. Eine echte Liefertermintreue/OTIF wird nicht berechnet, weil das Ist-Wareneingangsdatum aus EKBE/MSEG/MATDOC noch fehlt.";
                noticeEn = "The reliable measure currently available is planned-date risk from EKET. True on-time delivery/OTIF is not calculated because the actual goods-receipt date from EKBE/MSEG/MATDOC is still missing.";
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }

        var sourceMaterialCount = rows.Select(row => row.Material).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        // Suche, Disponent und Produktgruppe grenzen den Umfang ein und gelten fuer alles.
        // "Nur Handlungsbedarf" wirkt bewusst NUR auf Kennzahlen und Tabelle: der Schalter
        // entfernt genau die OK-Zeilen, die der gruene Prioritaetsbalken zaehlen soll. Wuerden
        // die Balken danach gezaehlt, stuende "Ohne akuten Hinweis" beim Standardaufruf immer
        // auf 0 und saehe wie eine Messung aus, obwohl es der Filter selbst waere.
        var scoped = ApplyScopeFilter(rows, filter);
        var filtered = scoped
            .Where(row => !filter.OnlyActionable || !row.RiskCode.Equals("OK", StringComparison.OrdinalIgnoreCase))
            .OrderBy(row => RiskRank(row.RiskCode))
            .ThenByDescending(row => row.ShortageValueChf + row.OpenOrderValueChf)
            .ThenBy(row => row.Material, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new SupplyChainAnalysisResult
        {
            Kind = kind,
            MaterialUsageLoadedAtUtc = materialTimestamp,
            PurchasingLoadedAtUtc = purchasingTimestamp,
            SourceMaterialCount = sourceMaterialCount,
            FilteredMaterialCount = filtered.Select(row => row.Material).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            ActualReceiptDateAvailable = actualReceiptDateAvailable,
            NoticeDe = noticeDe,
            NoticeEn = noticeEn,
            Kpis = BuildKpis(kind, filtered, sourceMaterialCount),
            RiskBuckets = BuildRiskBuckets(scoped),
            Rows = filtered.Take(1000).ToList()
        };
    }

    private static IReadOnlyList<SupplyChainAnalysisRow> ApplyScopeFilter(
        IEnumerable<SupplyChainAnalysisRow> rows,
        SupplyChainAnalysisFilter filter)
    {
        var query = rows;
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(row =>
                Contains(row.Material, search) || Contains(row.Description, search) ||
                Contains(row.Supplier, search) || Contains(row.IssueDe, search));
        }

        if (!string.IsNullOrWhiteSpace(filter.Dispatcher))
            query = query.Where(row => Contains(row.Dispatcher, filter.Dispatcher.Trim()));
        if (!string.IsNullOrWhiteSpace(filter.ProductGroup))
            query = query.Where(row => Contains(row.ProductGroup, filter.ProductGroup.Trim()));
        return query.ToList();
    }

    private static bool Contains(string value, string search)
        => value.Contains(search, StringComparison.OrdinalIgnoreCase);

    private static SupplyChainAnalysisRow BuildDispositionRow(MaterialFact fact, OpenOrderFact open)
    {
        var shortage = fact.HasFinalStock ? Math.Max(0m, -fact.FinalStock) : 0m;
        var (riskCode, riskDe, riskEn) = ClassifyDisposition(fact);
        return BuildBaseRow(fact, open) with
        {
            RiskCode = riskCode,
            RiskDe = riskDe,
            RiskEn = riskEn,
            ShortageQuantity = shortage,
            ShortageValueChf = shortage * fact.UnitCost,
            IssueDe = riskDe,
            IssueEn = riskEn,
            Source = "ZMD04_CALC + MARC + EKET"
        };
    }

    private static SupplyChainAnalysisRow BuildCoverageRow(MaterialFact fact, OpenOrderFact open)
    {
        string code;
        string de;
        string en;
        if (!fact.HasFinalStock)
            (code, de, en) = ("P3", "Endbestand wurde von SAP nicht geliefert", "Final stock was not supplied by SAP");
        else if (fact.FinalStock < 0m && open.OpenQuantity <= 0m)
            (code, de, en) = ("P1", "Deckungsluecke ohne offene Bestellung", "Coverage gap without an open order");
        else if (fact.FinalStock < 0m)
            (code, de, en) = ("P1", "Deckungsluecke trotz offener Bestellung", "Coverage gap despite an open order");
        else if (open.OverdueQuantity > 0m)
            (code, de, en) = ("P2", "Offene Menge mit ueberfaelligem Plantermin", "Open quantity with an overdue planned date");
        else if (fact.FinalStock < Math.Max(fact.SafetyStock, fact.ReorderPoint))
            (code, de, en) = ("P3", "Deckung unter Sicherheits-/Meldebestand", "Coverage below safety/reorder stock");
        else
            (code, de, en) = ("OK", "Kein akuter Deckungshinweis", "No immediate coverage issue");

        var shortage = fact.HasFinalStock ? Math.Max(0m, -fact.FinalStock) : 0m;
        return BuildBaseRow(fact, open) with
        {
            RiskCode = code,
            RiskDe = de,
            RiskEn = en,
            ShortageQuantity = shortage,
            ShortageValueChf = shortage * fact.UnitCost,
            IssueDe = de,
            IssueEn = en,
            Source = "ZMD04_CALC + EKPO/EKET"
        };
    }

    private static (string Code, string De, string En) ClassifyDisposition(MaterialFact fact)
    {
        if (!fact.HasFinalStock)
            return ("P3", "Endbestand wurde von SAP nicht geliefert", "Final stock was not supplied by SAP");
        if (fact.FinalStock < 0m && fact.FixedReceipts <= 0m)
            return ("P1", "Negativer Endbestand ohne festen Zugang", "Negative final stock without a fixed receipt");
        if (fact.FinalStock < 0m)
            return ("P2", "Negativer Endbestand", "Negative final stock");
        if (fact.FinalStock < fact.SafetyStock || fact.FinalStock < fact.ReorderPoint)
            return ("P3", "Unter Sicherheits-/Meldebestand", "Below safety/reorder stock");
        if (fact.Exclusive && fact.FinalStock <= 0m)
            return ("P3", "Exklusive Komponente ohne positive Deckung", "Exclusive component without positive coverage");
        return ("OK", "Kein akuter Dispositionshinweis", "No immediate planning issue");
    }

    private static List<SupplyChainAnalysisRow> BuildDependencyRows(
        IReadOnlyList<MaterialFact> materialFacts,
        IReadOnlyList<DependencyFact> dependencies)
    {
        var usage = materialFacts.ToDictionary(row => row.MaterialKey, StringComparer.OrdinalIgnoreCase);
        var rows = new List<SupplyChainAnalysisRow>();
        foreach (var dependency in dependencies)
        {
            usage.TryGetValue(dependency.MaterialKey, out var fact);
            var parentCount = fact?.ParentMaterialCount ?? 0;
            string code;
            string de;
            string en;
            if (dependency.SupplierCount == 1 && parentCount >= 5)
                (code, de, en) = ("P1", "Ein beobachteter Lieferant bei breiter Verwendung", "One observed supplier with broad usage");
            else if (dependency.SupplierCount == 1)
                (code, de, en) = ("P2", "Nur ein historisch beobachteter Lieferant", "Only one historically observed supplier");
            else if (dependency.TopSupplierSharePercent >= 80m)
                (code, de, en) = ("P3", "Hohe Spend-Konzentration auf einen Lieferanten", "High spend concentration on one supplier");
            else
                (code, de, en) = ("OK", "Mehrere beobachtete Lieferanten", "Multiple observed suppliers");

            var baseRow = fact is null
                ? EmptyRow(dependency.Material, dependency.Description)
                : BuildBaseRow(fact, OpenOrderFact.Empty);
            rows.Add(baseRow with
            {
                RiskCode = code,
                RiskDe = de,
                RiskEn = en,
                Supplier = dependency.TopSupplier,
                SupplierCount = dependency.SupplierCount,
                TopSupplierSharePercent = dependency.TopSupplierSharePercent,
                ParentMaterialCount = parentCount,
                OpenOrderValueChf = dependency.TotalSpendChf,
                IssueDe = de,
                IssueEn = en,
                Source = "EKKO/EKPO Historie + ZLO03"
            });
        }
        return rows;
    }

    private static List<SupplyChainAnalysisRow> BuildParameterAuditRows(IReadOnlyList<MaterialFact> facts)
    {
        var rows = new List<SupplyChainAnalysisRow>();
        foreach (var fact in facts)
        {
            AddIssue(fact.FinalStock < 0m && fact.SafetyStock <= 0m, "P1", "Negativer Endbestand ohne Sicherheitsbestand", "Negative final stock without safety stock");
            AddIssue(fact.FinalStock < 0m && fact.ReorderPoint <= 0m, "P1", "Negativer Endbestand ohne Meldebestand", "Negative final stock without reorder point");
            AddIssue(string.IsNullOrWhiteSpace(fact.MrpType), "P2", "Dispositionsmerkmal fehlt", "MRP type is missing");
            AddIssue(string.IsNullOrWhiteSpace(fact.ProcurementType), "P2", "Beschaffungsart fehlt", "Procurement type is missing");
            AddIssue((fact.MaterialStatus == "98" || fact.MaterialStatus == "99") && fact.ParentMaterialCount > 0,
                "P1", "Gesperrtes/auslaufendes Material wird noch verwendet", "Blocked/phasing-out material is still used");
            AddIssue(fact.FixedLotSize > 0m && fact.FinalStock < 0m, "P3", "Fixlosgroesse bei Deckungsluecke pruefen", "Review fixed lot size for coverage gap");

            void AddIssue(bool condition, string code, string de, string en)
            {
                if (!condition)
                    return;
                rows.Add(BuildBaseRow(fact, OpenOrderFact.Empty) with
                {
                    RiskCode = code,
                    RiskDe = de,
                    RiskEn = en,
                    IssueDe = de,
                    IssueEn = en,
                    Source = "MARC + MARA + ZMD04_CALC"
                });
            }
        }
        return rows;
    }

    private static SupplyChainAnalysisRow BuildBaseRow(MaterialFact fact, OpenOrderFact open)
        => new(
            fact.Material, fact.Description, fact.Dispatchers, fact.ProductGroups,
            "OK", "Kein Hinweis", "No issue", fact.Stock, fact.Consumption, fact.FixedReceipts, fact.PlannedReceipts,
            fact.FixedIssues, fact.PlannedIssues, fact.FinalStock, fact.SafetyStock, fact.ReorderPoint,
            0m, 0m, fact.HasUnitCost, open.OpenQuantity, open.OpenValueChf, open.OverdueQuantity, open.NextDeliveryDate,
            open.Suppliers, 0, 0m, fact.ParentMaterialCount, fact.Exclusive, fact.MrpType, fact.LotSize,
            fact.FixedLotSize, fact.ProcurementType, fact.MaterialStatus, fact.LzCode, "", "", "");

    private static SupplyChainAnalysisRow EmptyRow(string material, string description)
        => new(
            Material: material,
            Description: description,
            Dispatcher: "",
            ProductGroup: "",
            RiskCode: "OK",
            RiskDe: "Kein Hinweis",
            RiskEn: "No issue",
            Stock: 0m,
            Consumption: 0m,
            FixedReceipts: 0m,
            PlannedReceipts: 0m,
            FixedIssues: 0m,
            PlannedIssues: 0m,
            FinalStock: 0m,
            SafetyStock: 0m,
            ReorderPoint: 0m,
            ShortageQuantity: 0m,
            ShortageValueChf: 0m,
            HasUnitCost: false,
            OpenOrderQuantity: 0m,
            OpenOrderValueChf: 0m,
            OverdueQuantity: 0m,
            NextDeliveryDate: null,
            Supplier: "",
            SupplierCount: 0,
            TopSupplierSharePercent: 0m,
            ParentMaterialCount: 0,
            Exclusive: false,
            MrpType: "",
            LotSize: "",
            FixedLotSize: 0m,
            ProcurementType: "",
            MaterialStatus: "",
            LzCode: "",
            IssueDe: "",
            IssueEn: "",
            Source: "");

    private static OpenOrderFact FindOpenOrder(
        IReadOnlyDictionary<string, OpenOrderFact> facts,
        string materialKey)
        => facts.TryGetValue(materialKey, out var fact) ? fact : OpenOrderFact.Empty;

    private static IReadOnlyList<SupplyChainKpi> BuildKpis(
        SupplyChainAnalysisKind kind,
        IReadOnlyList<SupplyChainAnalysisRow> rows,
        int sourceMaterialCount)
    {
        var affected = rows.Select(row => row.Material).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        // Materialien mit echter Fehlmenge, fuer die keine Stueckkosten gepflegt sind. Ihr
        // Fehlwert ist unbekannt, nicht null — sonst meldet die Kachel eine zu kleine Summe,
        // ohne dass jemand erkennt, dass ein Teil des Fehlbestands gar nicht bewertet wurde.
        var unvaluedShortage = rows.Count(row => row.ShortageQuantity > 0m && !row.HasUnitCost);
        var shortageValueDetailDe = unvaluedShortage > 0
            ? $"Fehlmenge mal Stueckkosten; {unvaluedShortage:N0} ohne Stueckkosten nicht bewertet"
            : "Fehlmenge mal Stueckkosten";
        var shortageValueDetailEn = unvaluedShortage > 0
            ? $"shortage times unit cost; {unvaluedShortage:N0} not valued (unit cost missing)"
            : "shortage times unit cost";
        return kind switch
        {
            SupplyChainAnalysisKind.MaterialDisposition =>
            [
                new("Kritische Materialien", "Critical materials", affected.ToString("N0"), "im aktuellen Filter", "in the current filter"),
                new("Fehlmenge", "Shortage quantity", rows.Sum(row => row.ShortageQuantity).ToString("N0"), "negativer Endbestand", "negative final stock"),
                new("Fehlwert CHF", "Shortage value CHF", rows.Sum(row => row.ShortageValueChf).ToString("N0"), shortageValueDetailDe, shortageValueDetailEn),
                new("Datenumfang", "Data scope", sourceMaterialCount.ToString("N0"), "Materialien im ZLO03-Cache", "materials in the ZLO03 cache")
            ],
            SupplyChainAnalysisKind.PurchaseCoverage =>
            [
                new("Deckungshinweise", "Coverage issues", affected.ToString("N0"), "im aktuellen Filter", "in the current filter"),
                new("Offene Bestellmenge", "Open order quantity", rows.Sum(row => row.OpenOrderQuantity).ToString("N0"), "EKET Menge minus Wareneingang", "EKET quantity minus received quantity"),
                new("Ueberfaellige Menge", "Overdue quantity", rows.Sum(row => row.OverdueQuantity).ToString("N0"), "Plantermin vor heute", "planned date before today"),
                new("Offener Wert CHF", "Open value CHF", rows.Sum(row => row.OpenOrderValueChf).ToString("N0"), "aus offenem Mengenanteil", "from the open quantity share")
            ],
            SupplyChainAnalysisKind.MaterialDependency =>
            [
                new("Risikomaterialien", "Risk materials", affected.ToString("N0"), "historische Konzentration", "historical concentration"),
                new("Nur ein Lieferant", "Only one supplier", rows.Count(row => row.SupplierCount == 1).ToString("N0"), "im beobachteten Einkaufsbestand", "in observed purchasing history"),
                new("Breite Wirkung", "Broad impact", rows.Count(row => row.ParentMaterialCount >= 5).ToString("N0"), "mindestens 5 Elternmaterialien", "at least 5 parent materials"),
                new("Analysierter Spend CHF", "Analysed spend CHF", rows.Sum(row => row.OpenOrderValueChf).ToString("N0"), "historischer Bestellwert", "historical purchase-order value")
            ],
            SupplyChainAnalysisKind.PlanningParameterAudit =>
            [
                new("Pruefauftraege", "Review tasks", rows.Count.ToString("N0"), "Regeltreffer im Filter", "rule matches in the filter"),
                new("Betroffene Materialien", "Affected materials", affected.ToString("N0"), "eindeutige Materialnummern", "distinct material numbers"),
                new("Prioritaet P1", "Priority P1", rows.Count(row => row.RiskCode == "P1").ToString("N0"), "sofort pruefen", "review immediately"),
                new("Keine Auto-Aenderung", "No automatic change", "0", "nur fachliche Pruefung", "functional review only")
            ],
            SupplyChainAnalysisKind.DeliveryPerformance =>
            [
                new("Plantermin-Risiken", "Planned-date risks", affected.ToString("N0"), "offene Material-/Lieferantenfaelle", "open material/supplier cases"),
                new("Ueberfaellige Menge", "Overdue quantity", rows.Sum(row => row.OverdueQuantity).ToString("N0"), "EKET-Plantermin vor heute", "EKET planned date before today"),
                new("Offener Wert CHF", "Open value CHF", rows.Sum(row => row.OpenOrderValueChf).ToString("N0"), "offener Bestellwert", "open order value"),
                new("Ist-Termin-Abdeckung", "Actual-date coverage", "0%", "EKBE/MSEG/MATDOC fehlt", "EKBE/MSEG/MATDOC missing")
            ],
            _ => []
        };
    }

    private static IReadOnlyList<SupplyChainRiskBucket> BuildRiskBuckets(IReadOnlyList<SupplyChainAnalysisRow> rows)
        =>
        [
            new("P1 - sofort", "P1 - immediate", rows.Count(row => row.RiskCode == "P1"), "#c62828"),
            new("P2 - hoch", "P2 - high", rows.Count(row => row.RiskCode == "P2"), "#ef6c00"),
            new("P3 - pruefen", "P3 - review", rows.Count(row => row.RiskCode == "P3"), "#f9a825"),
            new("Ohne akuten Hinweis", "No immediate issue", rows.Count(row => row.RiskCode == "OK"), "#2e7d32")
        ];

    private static int RiskRank(string code) => code switch { "P1" => 0, "P2" => 1, "P3" => 2, _ => 3 };

    private static async Task<List<MaterialFact>> LoadMaterialFactsAsync(
        SqliteConnection conn,
        ProductGroupMaps maps,
        CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT
    CASE WHEN ltrim(upper(trim(Kompnr)), '0') = '' THEN '0' ELSE ltrim(upper(trim(Kompnr)), '0') END AS MaterialKey,
    MAX(trim(Kompnr)) AS Material,
    MAX(trim(KompnrMaktx)) AS Description,
    GROUP_CONCAT(DISTINCT trim(VknrDispo)) AS Dispatchers,
    MAX(CAST(REPLACE(REPLACE(NULLIF(trim(Labst), ''), ',', '.'), '''', '') AS REAL)) AS Stock,
    MAX(CAST(REPLACE(REPLACE(NULLIF(trim(Verbrauch), ''), ',', '.'), '''', '') AS REAL)) AS Consumption,
    MAX(CAST(REPLACE(REPLACE(NULLIF(trim(FesteZugang), ''), ',', '.'), '''', '') AS REAL)) AS FixedReceipts,
    MAX(CAST(REPLACE(REPLACE(NULLIF(trim(GeplZugang), ''), ',', '.'), '''', '') AS REAL)) AS PlannedReceipts,
    MAX(CAST(REPLACE(REPLACE(NULLIF(trim(FesteAbgang), ''), ',', '.'), '''', '') AS REAL)) AS FixedIssues,
    MAX(CAST(REPLACE(REPLACE(NULLIF(trim(GeplAbgang), ''), ',', '.'), '''', '') AS REAL)) AS PlannedIssues,
    MAX(CAST(REPLACE(REPLACE(NULLIF(trim(Endbestand), ''), ',', '.'), '''', '') AS REAL)) AS FinalStock,
    MAX(CASE WHEN trim(Endbestand) <> '' THEN 1 ELSE 0 END) AS HasFinalStock,
    MAX(CAST(REPLACE(REPLACE(NULLIF(trim(Stueckkosten), ''), ',', '.'), '''', '') AS REAL)) AS UnitCost,
    MAX(CASE WHEN trim(Stueckkosten) <> '' THEN 1 ELSE 0 END) AS HasUnitCost,
    MAX(CAST(REPLACE(REPLACE(NULLIF(trim(Minbe), ''), ',', '.'), '''', '') AS REAL)) AS ReorderPoint,
    MAX(CAST(REPLACE(REPLACE(NULLIF(trim(Eisbe), ''), ',', '.'), '''', '') AS REAL)) AS SafetyStock,
    MAX(CAST(REPLACE(REPLACE(NULLIF(trim(Bstfe), ''), ',', '.'), '''', '') AS REAL)) AS FixedLotSize,
    MAX(trim(Dismm)) AS MrpType,
    MAX(trim(Disls)) AS LotSize,
    MAX(trim(Beskz)) AS ProcurementType,
    MAX(trim(Mstae)) AS MaterialStatus,
    MAX(trim(Zzlzcod)) AS LzCode,
    COUNT(DISTINCT CASE WHEN trim(Vknr) <> '' THEN trim(Vknr) END) AS ParentMaterialCount,
    MAX(CASE WHEN Exklusiv <> 0 THEN 1 ELSE 0 END) AS Exclusive
FROM MaterialUsageCache
WHERE trim(Kompnr) <> ''
GROUP BY MaterialKey;";

        var rows = new List<MaterialFact>();
        await using var command = conn.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var dispatchers = reader.IsDBNull(3) ? "" : reader.GetString(3);
            rows.Add(new MaterialFact(
                reader.GetString(0), reader.GetString(1), reader.GetString(2), dispatchers,
                ResolveProductGroups(dispatchers, maps), Decimal(reader, 4), Decimal(reader, 5), Decimal(reader, 6),
                Decimal(reader, 7), Decimal(reader, 8), Decimal(reader, 9), Decimal(reader, 10), reader.GetInt32(11) != 0,
                Decimal(reader, 12), reader.GetInt32(13) != 0, Decimal(reader, 14), Decimal(reader, 15), Decimal(reader, 16),
                Text(reader, 17), Text(reader, 18), Text(reader, 19), Text(reader, 20), Text(reader, 21),
                Convert.ToInt32(reader.GetValue(22), CultureInfo.InvariantCulture), reader.GetInt32(23) != 0));
        }
        return rows;
    }

    private static async Task<Dictionary<string, OpenOrderFact>> LoadOpenOrderFactsAsync(
        SqliteConnection conn,
        CancellationToken cancellationToken)
    {
        var today = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var sql = $@"
WITH Schedule AS (
    SELECT Ebeln, Ebelp,
           SUM(MAX(CAST(Menge AS REAL) - CAST(Wemng AS REAL), 0)) AS OpenQty,
           SUM(CASE WHEN Eindt < '{today}' THEN MAX(CAST(Menge AS REAL) - CAST(Wemng AS REAL), 0) ELSE 0 END) AS OverdueQty,
           MIN(CASE WHEN CAST(Menge AS REAL) > CAST(Wemng AS REAL) THEN Eindt END) AS NextDate
    FROM PurchasingEketCache
    GROUP BY Ebeln, Ebelp
), ItemFacts AS (
    SELECT CASE WHEN ltrim(upper(trim(p.Matnr)), '0') = '' THEN '0' ELSE ltrim(upper(trim(p.Matnr)), '0') END AS MaterialKey,
           COALESCE(NULLIF(trim(k.SupplierName), ''), NULLIF(trim(k.Lifnr), ''), 'ohne Lieferant') AS Supplier,
           COALESCE(s.OpenQty, 0) AS OpenQty,
           COALESCE(s.OverdueQty, 0) AS OverdueQty,
           s.NextDate,
           CASE WHEN CAST(p.Menge AS REAL) = 0 THEN 0 ELSE
               (CASE WHEN COALESCE(k.Waers, '') IN ('', 'CHF') THEN CAST(p.Netwr AS REAL)
                     WHEN CAST(k.Wkurs AS REAL) > 0 THEN CAST(p.Netwr AS REAL) * CAST(k.Wkurs AS REAL)
                     WHEN CAST(k.Wkurs AS REAL) < 0 THEN CAST(p.Netwr AS REAL) / (-CAST(k.Wkurs AS REAL))
                     ELSE CAST(p.Netwr AS REAL) END) / CAST(p.Menge AS REAL) END AS UnitValueChf
    FROM PurchasingEkpoCache p
    JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
    LEFT JOIN Schedule s ON s.Ebeln = p.Ebeln AND s.Ebelp = p.Ebelp
    WHERE COALESCE(p.Loekz, '') = '' AND COALESCE(p.Elikz, '') <> 'X'
      AND COALESCE(p.Mstae, '') NOT IN ('98', '99')
      AND (COALESCE(k.Bstyp, '') = '' OR (k.Bstyp = 'F' AND COALESCE(k.Bsart, '') <> 'UB'))
)
SELECT MaterialKey, SUM(OpenQty), SUM(OpenQty * UnitValueChf), SUM(OverdueQty), MIN(NextDate), GROUP_CONCAT(DISTINCT Supplier)
FROM ItemFacts
WHERE OpenQty > 0
GROUP BY MaterialKey;";

        var result = new Dictionary<string, OpenOrderFact>(StringComparer.OrdinalIgnoreCase);
        await using var command = conn.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var key = reader.GetString(0);
            result[key] = new OpenOrderFact(Decimal(reader, 1), Decimal(reader, 2), Decimal(reader, 3),
                ParseDate(Text(reader, 4)), Text(reader, 5));
        }
        return result;
    }

    private static async Task<List<DependencyFact>> LoadDependencyFactsAsync(
        SqliteConnection conn,
        CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT CASE WHEN ltrim(upper(trim(p.Matnr)), '0') = '' THEN '0' ELSE ltrim(upper(trim(p.Matnr)), '0') END AS MaterialKey,
       MAX(trim(p.Matnr)) AS Material,
       MAX(trim(p.Txz01)) AS Description,
       COALESCE(NULLIF(trim(k.SupplierName), ''), NULLIF(trim(k.Lifnr), ''), 'ohne Lieferant') AS Supplier,
       SUM(CASE WHEN COALESCE(k.Waers, '') IN ('', 'CHF') THEN CAST(p.Netwr AS REAL)
                WHEN CAST(k.Wkurs AS REAL) > 0 THEN CAST(p.Netwr AS REAL) * CAST(k.Wkurs AS REAL)
                WHEN CAST(k.Wkurs AS REAL) < 0 THEN CAST(p.Netwr AS REAL) / (-CAST(k.Wkurs AS REAL))
                ELSE CAST(p.Netwr AS REAL) END) AS SpendChf
FROM PurchasingEkpoCache p
JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
WHERE trim(p.Matnr) <> '' AND COALESCE(p.Loekz, '') = ''
  AND (COALESCE(k.Bstyp, '') = '' OR (k.Bstyp = 'F' AND COALESCE(k.Bsart, '') <> 'UB'))
GROUP BY MaterialKey, Supplier;";

        var supplierRows = new List<DependencySupplierFact>();
        await using var command = conn.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            supplierRows.Add(new DependencySupplierFact(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), Decimal(reader, 4)));

        return supplierRows.GroupBy(row => row.MaterialKey, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var ordered = group.OrderByDescending(row => row.SpendChf).ToList();
                var total = ordered.Sum(row => row.SpendChf);
                return new DependencyFact(group.Key, ordered[0].Material, ordered[0].Description,
                    ordered.Count, ordered[0].Supplier, total == 0m ? 0m : ordered[0].SpendChf / total * 100m, total);
            })
            .ToList();
    }

    private static async Task<List<SupplyChainAnalysisRow>> LoadDeliveryRowsAsync(
        SqliteConnection conn,
        IReadOnlyList<MaterialFact> materialFacts,
        CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        var todaySql = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var sql = $@"
SELECT MAX(trim(p.Matnr)) AS Material,
       MAX(trim(p.Txz01)) AS Description,
       COALESCE(NULLIF(trim(k.SupplierName), ''), NULLIF(trim(k.Lifnr), ''), 'ohne Lieferant') AS Supplier,
       MIN(e.Eindt) AS NextDate,
       SUM(MAX(CAST(e.Menge AS REAL) - CAST(e.Wemng AS REAL), 0)) AS OpenQty,
       SUM(CASE WHEN e.Eindt < '{todaySql}' THEN MAX(CAST(e.Menge AS REAL) - CAST(e.Wemng AS REAL), 0) ELSE 0 END) AS OverdueQty,
       SUM(MAX(CAST(e.Menge AS REAL) - CAST(e.Wemng AS REAL), 0) *
           CASE WHEN CAST(p.Menge AS REAL) = 0 THEN 0 ELSE
               (CASE WHEN COALESCE(k.Waers, '') IN ('', 'CHF') THEN CAST(p.Netwr AS REAL)
                     WHEN CAST(k.Wkurs AS REAL) > 0 THEN CAST(p.Netwr AS REAL) * CAST(k.Wkurs AS REAL)
                     WHEN CAST(k.Wkurs AS REAL) < 0 THEN CAST(p.Netwr AS REAL) / (-CAST(k.Wkurs AS REAL))
                     ELSE CAST(p.Netwr AS REAL) END) / CAST(p.Menge AS REAL) END) AS OpenValueChf
FROM PurchasingEketCache e
JOIN PurchasingEkpoCache p ON p.Ebeln = e.Ebeln AND p.Ebelp = e.Ebelp
JOIN PurchasingEkkoCache k ON k.Ebeln = p.Ebeln
WHERE CAST(e.Menge AS REAL) > CAST(e.Wemng AS REAL)
  AND COALESCE(p.Loekz, '') = '' AND COALESCE(p.Elikz, '') <> 'X'
  AND COALESCE(p.Mstae, '') NOT IN ('98', '99')
  AND (COALESCE(k.Bstyp, '') = '' OR (k.Bstyp = 'F' AND COALESCE(k.Bsart, '') <> 'UB'))
GROUP BY CASE WHEN ltrim(upper(trim(p.Matnr)), '0') = '' THEN '0' ELSE ltrim(upper(trim(p.Matnr)), '0') END, Supplier;";

        var rows = new List<SupplyChainAnalysisRow>();
        var usage = materialFacts.ToDictionary(row => row.MaterialKey, StringComparer.OrdinalIgnoreCase);
        await using var command = conn.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var date = ParseDate(Text(reader, 3));
            var overdue = Decimal(reader, 5);
            string code;
            string de;
            string en;
            if (overdue > 0m)
                (code, de, en) = ("P1", "Plantermin ueberfaellig", "Planned date overdue");
            else if (date.HasValue && date.Value <= today.AddDays(30))
                (code, de, en) = ("P2", "Plantermin in den naechsten 30 Tagen", "Planned date within the next 30 days");
            else
                (code, de, en) = ("OK", "Spaeterer offener Plantermin", "Later open planned date");

            var material = Text(reader, 0);
            var baseRow = usage.TryGetValue(NormalizeMaterialKey(material), out var materialFact)
                ? BuildBaseRow(materialFact, OpenOrderFact.Empty)
                : EmptyRow(material, Text(reader, 1));
            rows.Add(baseRow with
            {
                Supplier = Text(reader, 2),
                NextDeliveryDate = date,
                OpenOrderQuantity = Decimal(reader, 4),
                OverdueQuantity = overdue,
                OpenOrderValueChf = Decimal(reader, 6),
                RiskCode = code,
                RiskDe = de,
                RiskEn = en,
                IssueDe = de,
                IssueEn = en,
                Source = "EKET Plantermin; Ist-Wareneingangsdatum fehlt"
            });
        }
        return rows;
    }

    private static async Task<ProductGroupMaps> LoadProductGroupMapsAsync(SqliteConnection conn, CancellationToken cancellationToken)
    {
        var manual = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        await using (var command = conn.CreateCommand())
        {
            command.CommandText = "SELECT Disponent, ProductGroup, ProductGroupText FROM PurchasingProductGroupMap;";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                manual[Text(reader, 0).Trim()] = GroupLabel(Text(reader, 1), Text(reader, 2));
        }

        var rules = new List<ProductGroupRule>();
        await using (var command = conn.CreateCommand())
        {
            command.CommandText = "SELECT DisponentPattern, ProductGroup, ProductGroupText FROM PurchasingSpendDisponentRule;";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                rules.Add(new ProductGroupRule(Text(reader, 0).Trim(), GroupLabel(Text(reader, 1), Text(reader, 2))));
        }
        return new ProductGroupMaps(manual, rules);
    }

    private static string ResolveProductGroups(string dispatcherList, ProductGroupMaps maps)
    {
        var groups = dispatcherList.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(dispatcher =>
            {
                if (maps.Manual.TryGetValue(dispatcher, out var manual))
                    return manual;
                var rule = maps.Rules
                    .Where(candidate => RuleMatches(candidate.Pattern, dispatcher))
                    .OrderBy(candidate => candidate.Pattern.EndsWith('*') ? 1 : 0)
                    .ThenByDescending(candidate => candidate.Pattern.Length)
                    .FirstOrDefault();
                return rule is null ? $"Disponent {dispatcher}" : rule.Label;
            })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase);
        return string.Join(", ", groups);
    }

    private static bool RuleMatches(string pattern, string dispatcher)
        => pattern.EndsWith('*')
            ? dispatcher.StartsWith(pattern[..^1], StringComparison.OrdinalIgnoreCase)
            : pattern.Equals(dispatcher, StringComparison.OrdinalIgnoreCase);

    private static string NormalizeMaterialKey(string material)
    {
        var value = material.Trim().ToUpperInvariant().TrimStart('0');
        return value.Length == 0 ? "0" : value;
    }

    private static string GroupLabel(string code, string text)
        => string.IsNullOrWhiteSpace(text) ? code.Trim() : $"{code.Trim()} - {text.Trim()}";

    private static async Task<DateTime?> ReadMaxTimestampAsync(SqliteConnection conn, string table, CancellationToken cancellationToken)
    {
        await using var command = conn.CreateCommand();
        command.CommandText = $"SELECT MAX(LastLoadedAtUtc) FROM {table};";
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null || value == DBNull.Value ? null : ParseDate(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
    }

    private static decimal Decimal(SqliteDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? 0m : Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    private static string Text(SqliteDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);

    private static DateTime? ParseDate(string value)
        => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed) ? parsed : null;

    private sealed record MaterialFact(
        string MaterialKey, string Material, string Description, string Dispatchers, string ProductGroups,
        decimal Stock, decimal Consumption, decimal FixedReceipts, decimal PlannedReceipts, decimal FixedIssues, decimal PlannedIssues,
        decimal FinalStock, bool HasFinalStock, decimal UnitCost, bool HasUnitCost, decimal ReorderPoint, decimal SafetyStock, decimal FixedLotSize,
        string MrpType, string LotSize, string ProcurementType, string MaterialStatus, string LzCode,
        int ParentMaterialCount, bool Exclusive);

    private sealed record OpenOrderFact(decimal OpenQuantity, decimal OpenValueChf, decimal OverdueQuantity, DateTime? NextDeliveryDate, string Suppliers)
    {
        public static readonly OpenOrderFact Empty = new(0m, 0m, 0m, null, "");
    }

    private sealed record DependencySupplierFact(string MaterialKey, string Material, string Description, string Supplier, decimal SpendChf);
    private sealed record DependencyFact(string MaterialKey, string Material, string Description, int SupplierCount, string TopSupplier, decimal TopSupplierSharePercent, decimal TotalSpendChf);
    private sealed record ProductGroupRule(string Pattern, string Label);
    private sealed record ProductGroupMaps(IReadOnlyDictionary<string, string> Manual, IReadOnlyList<ProductGroupRule> Rules);
}
