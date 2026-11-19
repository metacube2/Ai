using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Issue Topic nach der Matrix von Ingo (19.11.2026, Spalten no Supplier / no Mat Number Match / no Cost), dazu die
/// Indien-Schluesselregel (AP1) und die Anwendung der Mapping-Tabelle (AP4). Plan docs/FINANCE_PLAN_MAPPING_ISSUE_TOPIC_2026-11-18.md.
/// Jeder Fall prueft zusaetzlich, dass Kosten, Kostenquelle und Status der heutigen Rechnung entsprechen.
/// </summary>
public class GroupMarginIssueTopicTests
{
    private const string TrAgArea = "1100";
    private const string TrInArea = "TRIN";

    private static GroupMarginLine Line(
        string tsc = "TRFR",
        string material = "53513",
        string? supplierName = null,
        string? supplierNumber = null,
        string? salesType = null,
        string? groupMaterialNumber = null,
        decimal standardCost = 60m,
        decimal quantity = 1m)
        => new()
        {
            SupplierName = supplierName,
            SupplierNumber = supplierNumber,
            Tsc = tsc,
            Material = material,
            GroupMaterialNumber = groupMaterialNumber,
            SalesType = salesType,
            Quantity = quantity,
            StandardCost = standardCost,
            StandardCostCurrency = "EUR",
            NetSalesValue = 100m
        };

    private static Dictionary<(string MaterialKey, string ValuationArea), GroupStandardCost> Costs(
        params (string Key, string Area, decimal Cost)[] entries)
        => entries.ToDictionary(
            entry => (entry.Key, entry.Area),
            entry => new GroupStandardCost { MaterialKey = entry.Key, ValuationArea = entry.Area, UnitCost = entry.Cost, Currency = "CHF" });

    private static HashSet<string> Plant(params string[] keys) => new(keys.Concat(["FILLER"]), StringComparer.Ordinal);

    private static GroupMarginEvaluation Evaluate(
        GroupMarginLine line,
        Dictionary<(string, string), GroupStandardCost>? costs = null,
        HashSet<string>? plant = null,
        MaterialMappingLookup? mappings = null)
        => GroupMarginCalculator.Evaluate(
            line, costs ?? Costs(), chPlantMaterialKeys: plant ?? Plant(),
            supplierFallbackMode: SupplierFallbackModes.ChPlantMaster, materialMappings: mappings);

    [Fact]
    public void Leer_Lieferant_vorhanden_Artikel_gefunden_Kosten_vorhanden()
    {
        var result = Evaluate(Line(supplierName: "Trafag AG"), Costs(("53513", TrAgArea, 40m)), Plant("53513"));

        Assert.Equal(IssueTopics.None, result.IssueTopic);
        Assert.True(result.IsGroupCost);
        Assert.Equal(40m, result.CostBasis);
    }

    [Fact]
    public void Topic1_kein_Lieferant_nicht_im_Werkstamm_lokale_Kosten()
    {
        var result = Evaluate(Line(), plant: Plant("99999"));

        Assert.Equal(IssueTopics.NoSupplierNotFound, result.IssueTopic);
        Assert.Equal(GroupMarginSupplierClassifier.Local, result.SupplierType);
        Assert.Equal(60m, result.CostBasis);
        Assert.Equal(GroupMarginStatuses.Ok, result.Status);
    }

    [Fact]
    public void Topic2_Lieferant_intern_Artikel_dort_nicht_gefunden_lokale_Kosten()
    {
        var result = Evaluate(Line(tsc: "TRUK", material: "UK-777", supplierName: "Trafag AG"), Costs(("53513", TrAgArea, 40m)), Plant("53513"));

        Assert.Equal(IssueTopics.SupplierNotFound, result.IssueTopic);
        Assert.Equal("Interner Standardpreis", result.CostSource);
        Assert.Equal(60m, result.CostBasis);
        Assert.Equal(GroupMarginStatuses.Ok, result.Status);
    }

    [Theory]
    [InlineData(true)]   // im CH-Werkstamm, aber ohne Kostenzeile
    [InlineData(false)]  // Kostenzeile mit Preis 0
    public void Topic3_Lieferant_Artikel_gefunden_Kosten_fehlen_lokale_Kosten(bool onlyInPlant)
    {
        var costs = onlyInPlant ? Costs() : Costs(("53513", TrAgArea, 0m));
        var result = Evaluate(Line(tsc: "TRUK", supplierName: "Trafag AG"), costs, Plant("53513"));

        Assert.Equal(IssueTopics.SupplierFoundNoCost, result.IssueTopic);
        Assert.Equal(60m, result.CostBasis);
        Assert.Equal("Interner Standardpreis", result.CostSource);
    }

    [Fact]
    public void Topic4_kein_Lieferant_bei_Trafag_AG_gefunden_ohne_Kosten_lokale_Kosten()
    {
        var result = Evaluate(Line(), Costs(), Plant("53513"));

        Assert.Equal(IssueTopics.NoSupplierFoundNoCost, result.IssueTopic);
        Assert.Equal(GroupMarginSupplierClassifier.Internal, result.SupplierType);
        Assert.Equal(60m, result.CostBasis);   // heutiger Rueckfall auf die lokalen Kosten, unveraendert
        Assert.Equal(GroupMarginStatuses.Ok, result.Status);
    }

    [Fact]
    public void Topic5_kein_Lieferant_bei_Trafag_AG_gefunden_mit_Kosten_TR_AG()
    {
        var result = Evaluate(Line(), Costs(("53513", TrAgArea, 40m)), Plant("53513"));

        Assert.Equal(IssueTopics.NoSupplierTrAgCost, result.IssueTopic);
        Assert.Equal(GroupMarginCalculator.GroupCostSourceLabel, result.CostSource);
        Assert.Equal(40m, result.CostBasis);
    }

    [Fact]
    public void Topic6_kein_Lieferant_nicht_gefunden_lokale_Kosten_null()
    {
        var result = Evaluate(Line(standardCost: 0m), plant: Plant("99999"));

        Assert.Equal(IssueTopics.NoSupplierUseZero, result.IssueTopic);
        Assert.Equal(0m, result.CostBasis);
        Assert.Equal(GroupMarginStatuses.StandardCostMissing, result.Status);
    }

    [Fact]
    public void Topic7_Lieferant_nicht_gefunden_lokale_Kosten_null()
    {
        var result = Evaluate(Line(tsc: "TRUK", material: "UK-777", supplierName: "Trafag AG", standardCost: 0m), plant: Plant("53513"));

        Assert.Equal(IssueTopics.SupplierUseZero, result.IssueTopic);
        Assert.Equal(0m, result.CostBasis);
        Assert.Equal(GroupMarginStatuses.StandardCostMissing, result.Status);
    }

    [Fact]
    public void Gefundener_Artikel_bleibt_3_bzw_4_auch_wenn_lokale_Kosten_null_sind()
    {
        // Korrektur Ingo 19.11.2026: die Matrix gilt woertlich; 6/7 nur ohne Match.
        var withSupplier = Evaluate(Line(tsc: "TRUK", supplierName: "Trafag AG", standardCost: 0m), plant: Plant("53513"));
        var withoutSupplier = Evaluate(Line(standardCost: 0m), plant: Plant("53513"));

        Assert.Equal(IssueTopics.SupplierFoundNoCost, withSupplier.IssueTopic);
        Assert.Equal(IssueTopics.NoSupplierFoundNoCost, withoutSupplier.IssueTopic);
        Assert.Equal(GroupMarginStatuses.StandardCostMissing, withSupplier.Status);
        Assert.Equal(GroupMarginStatuses.StandardCostMissing, withoutSupplier.Status);
        Assert.Equal(0m, withoutSupplier.CostBasis);
    }

    [Fact]
    public void Vorlaeufig8_Indien_LRD_ohne_Konzernkosten_Kosten_bleiben_offen()
    {
        var result = Evaluate(Line(tsc: "TRIN", material: "IC1", salesType: "LRD"));

        Assert.Equal(IssueTopics.IndiaLrdWithoutGroupCost, result.IssueTopic);
        Assert.True(result.IsGroupCostMissing);
        Assert.Equal(GroupMarginStatuses.GroupCostMissing, result.Status);
    }

    [Fact]
    public void Vorlaeufig9_Lieferant_unklar_ohne_Materialnummer()
    {
        var result = Evaluate(Line(material: ""), plant: Plant("53513"));

        Assert.Equal(IssueTopics.SupplierUnclear, result.IssueTopic);
        Assert.Equal(GroupMarginStatuses.SupplierUnclear, result.Status);
    }

    [Fact]
    public void Vorlaeufig10_interner_Lieferant_ohne_Kostenquelle()
    {
        var result = Evaluate(Line(tsc: "TRDE", supplierName: "GFS Gesellschaft fuer Sensorik"));

        Assert.Equal(GroupMarginSupplierClassifier.Internal, result.SupplierType);
        Assert.Equal(IssueTopics.InternalWithoutCostSource, result.IssueTopic);
        Assert.Equal(60m, result.CostBasis);
    }

    [Fact]
    public void Externer_Lieferant_ist_kein_Issue_ausser_ohne_lokale_Kosten()
    {
        Assert.Equal(IssueTopics.None, Evaluate(Line(supplierName: "Bosch GmbH")).IssueTopic);
        Assert.Equal(IssueTopics.SupplierUseZero, Evaluate(Line(supplierName: "Bosch GmbH", standardCost: 0m)).IssueTopic);
    }

    [Fact]
    public void CH_AT_mit_Kosten_TR_AG_ist_kein_Issue()
    {
        var result = Evaluate(Line(tsc: "TRCH"), Costs(("53513", TrAgArea, 40m)), Plant("53513"));

        Assert.Equal(IssueTopics.None, result.IssueTopic);
    }

    [Fact]
    public void Matrix_enthaelt_alle_Codes_genau_einmal()
    {
        Assert.Equal(
            ["", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10"],
            IssueTopics.Matrix.Select(entry => entry.Code).ToArray());
    }

    // ----- AP1: I-Praefix TRIT -----

    [Fact]
    public void TRIT_Ware_aus_Indien_findet_Kosten_ohne_vorangestelltes_I()
    {
        var result = Evaluate(Line(tsc: "TRIT", material: "I53513", supplierName: "Trafag Controls India Pvt Ltd"),
            Costs(("53513", TrInArea, 100m)));

        Assert.True(result.IsGroupCost);
        Assert.Equal(100m, result.CostBasis);
        Assert.Equal("Konzernkosten TR IN (B1 StockPrice)", result.CostSource);
        Assert.Equal(IssueTopics.None, result.IssueTopic);
        Assert.Contains(IssueTopics.ItalianIndiaPrefixRuleLabel, result.Mapping);
    }

    [Fact]
    public void I_Praefix_gilt_nicht_fuer_andere_Gesellschaften_und_nicht_fuer_andere_Lieferanten()
    {
        var costs = Costs(("53513", TrInArea, 100m), ("53513", TrAgArea, 40m));

        var uk = Evaluate(Line(tsc: "TRUK", material: "I53513", supplierName: "Trafag Controls India Pvt Ltd"), costs);
        var itFromTrAg = Evaluate(Line(tsc: "TRIT", material: "I53513", supplierName: "Trafag AG"), costs);

        Assert.False(uk.IsGroupCost);
        Assert.Equal(IssueTopics.SupplierNotFound, uk.IssueTopic);
        Assert.False(itFromTrAg.IsGroupCost);
        Assert.Equal(string.Empty, itFromTrAg.Mapping);
    }

    [Fact]
    public void Voller_Schluessel_geht_vor_dem_abgeschnittenen()
    {
        // Echte indische Nummern koennen selbst mit I beginnen (IR...); sie trafen schon vorher.
        var result = Evaluate(Line(tsc: "TRIT", material: "IR100", supplierName: "Trafag India"),
            Costs(("IR100", TrInArea, 70m), ("R100", TrInArea, 999m)));

        Assert.Equal(70m, result.CostBasis);
        Assert.Equal(string.Empty, result.Mapping);
    }

    // ----- AP4: Mapping-Tabelle -----

    private static MaterialMapping Mapping(string tsc, string local, string group, string method, string status)
        => new()
        {
            Tsc = tsc, LocalMaterial = local, LocalMaterialKey = MaterialKeyNormalizer.Normalize(local),
            GroupMaterialNumber = group, Method = method, Status = status
        };

    [Fact]
    public void Bestaetigtes_Mapping_fuellt_die_Gruppennummer_und_findet_die_Konzernkosten()
    {
        var mappings = new MaterialMappingLookup([Mapping("TRUK", "UK-777", "53513", MaterialMappingMethods.Text, MaterialMappingStatuses.Confirmed)]);
        var result = Evaluate(Line(tsc: "TRUK", material: "UK-777", supplierName: "Trafag AG"),
            Costs(("53513", TrAgArea, 40m)), Plant("53513"), mappings);

        Assert.True(result.IsGroupCost);
        Assert.Equal(40m, result.CostBasis);
        Assert.Equal(IssueTopics.None, result.IssueTopic);
        Assert.StartsWith("53513 (Text bestaetigt", result.Mapping);
    }

    [Theory]
    [InlineData(MaterialMappingMethods.Text, MaterialMappingStatuses.Proposed, false)]
    [InlineData(MaterialMappingMethods.Text, MaterialMappingStatuses.Rejected, false)]
    [InlineData(MaterialMappingMethods.Manual, MaterialMappingStatuses.Proposed, false)]
    [InlineData(MaterialMappingMethods.Rule, MaterialMappingStatuses.Proposed, true)]
    [InlineData(MaterialMappingMethods.Rule, MaterialMappingStatuses.Rejected, false)]
    [InlineData(MaterialMappingMethods.Manual, MaterialMappingStatuses.Confirmed, true)]
    public void Texttreffer_wirken_nur_bestaetigt_Regeltreffer_sofort(string method, string status, bool effective)
    {
        var mappings = new MaterialMappingLookup([Mapping("TRUK", "UK-777", "53513", method, status)]);
        var result = Evaluate(Line(tsc: "TRUK", material: "UK-777", supplierName: "Trafag AG"),
            Costs(("53513", TrAgArea, 40m)), Plant("53513"), mappings);

        Assert.Equal(effective, result.IsGroupCost);
    }

    [Fact]
    public void Mapping_wirkt_nur_fuer_den_eigenen_TSC_und_ueberschreibt_nie()
    {
        var mappings = new MaterialMappingLookup([Mapping("TRUK", "UK-777", "53513", MaterialMappingMethods.Rule, MaterialMappingStatuses.Confirmed)]);
        var costs = Costs(("53513", TrAgArea, 40m), ("11111", TrAgArea, 10m));

        var otherTsc = Evaluate(Line(tsc: "TRUS", material: "UK-777", supplierName: "Trafag AG"), costs, Plant("53513"), mappings);
        var sourceNumber = Evaluate(Line(tsc: "TRUK", material: "UK-777", supplierName: "Trafag AG", groupMaterialNumber: "11111"),
            costs, Plant("53513"), mappings);

        Assert.False(otherTsc.IsGroupCost);
        Assert.Equal(10m, sourceNumber.CostBasis);   // Gruppennummer der Quelle gewinnt
        Assert.Equal(string.Empty, sourceNumber.Mapping);
    }
}
