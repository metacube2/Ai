using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Die gemeinsame Rechnung der Gruppenmarge. Vorher stand sie zweimal da (Excel-Nachweis und
/// Cockpit) und war beim Einbau des Status „Konzernkosten fehlen" auseinandergelaufen.
/// </summary>
public class GroupMarginCalculatorTests
{
    private const string TrAgArea = "1100";

    private static GroupMarginLine Line(
        string? salesType = null,
        string? supplierName = null,
        string? supplierNumber = null,
        string? supplierCountry = null,
        string tsc = "TRIN",
        string material = "IC15415",
        string? groupMaterialNumber = null,
        decimal quantity = 1m,
        decimal standardCost = 60m,
        decimal netSalesValue = 100m)
        => new()
        {
            SupplierNumber = supplierNumber,
            SupplierName = supplierName,
            SupplierCountry = supplierCountry,
            Tsc = tsc,
            Material = material,
            GroupMaterialNumber = groupMaterialNumber,
            SalesType = salesType,
            Quantity = quantity,
            StandardCost = standardCost,
            StandardCostCurrency = "CHF",
            NetSalesValue = netSalesValue
        };

    private static Dictionary<(string MaterialKey, string ValuationArea), GroupStandardCost> GroupCosts(
        string materialKey, decimal unitCost, string currency = "CHF", string area = TrAgArea)
        => new()
        {
            [(materialKey, area)] = new GroupStandardCost
            {
                MaterialKey = materialKey,
                ValuationArea = area,
                UnitCost = unitCost,
                Currency = currency
            }
        };

    [Fact]
    public void Regelkette_hat_die_fachlich_gewollte_Reihenfolge()
    {
        // Die Reihenfolge IST die Fachregel - deshalb wird sie geprueft und nicht nur kommentiert.
        Assert.Equal(
            new[]
            {
                nameof(GroupMarginCostRules.GroupStandardCost),
                nameof(GroupMarginCostRules.GroupDistributionWithoutGroupCost),
                nameof(GroupMarginCostRules.LocalStandardCost)
            },
            GroupMarginCalculator.CostRules.Select(rule => rule.Name).ToArray());
    }

    [Fact]
    public void Letzte_Regel_trifft_immer_zu_und_schliesst_die_Kette_ab()
    {
        var context = new GroupMarginCostContext(
            "EGAL", new Dictionary<(string, string), GroupStandardCost>(), IsReversal: false);

        Assert.NotNull(GroupMarginCostRules.LocalStandardCost.TryResolve(Line(), context));
    }

    [Fact]
    public void Konzernkosten_gehen_vor_dem_lokalen_Standardpreis()
    {
        // LRD: liefernde Gesellschaft ist Trafag AG, und zum Material gibt es Konzernkosten.
        var result = GroupMarginCalculator.Evaluate(
            Line(salesType: "LRD", material: "PT000003"), GroupCosts("PT000003", 42m));

        Assert.True(result.IsGroupCost);
        Assert.False(result.IsGroupCostMissing);
        Assert.Equal(42m, result.CostBasis);
        Assert.Equal(GroupMarginCalculator.GroupCostSourceLabel, result.CostSource);
        Assert.Equal(GroupMarginStatuses.Ok, result.Status);
    }

    [Theory]
    [InlineData("TR IN", "TRIN", "INR", 40, "Konzernkosten TR IN (B1 StockPrice)")]
    [InlineData("TR IT", "TRIT", "EUR", 100, "Konzernkosten TR IT (B1 StockPrice)")]
    public void Konzernkosten_von_TR_IN_und_TR_IT_ersetzen_den_lokalen_IC_Preis(
        string supplierName,
        string area,
        string currency,
        decimal groupUnitCost,
        string expectedSource)
    {
        var result = GroupMarginCalculator.Evaluate(
            Line(
                supplierName: supplierName,
                supplierNumber: "IC-SUPPLIER",
                tsc: "TRDE",
                material: "A2",
                quantity: 1m,
                standardCost: 100m,
                netSalesValue: 200m),
            GroupCosts("A2", groupUnitCost, currency, area));

        Assert.True(result.IsGroupCost);
        Assert.Equal(GroupMarginSupplierClassifier.Internal, result.SupplierType);
        Assert.Equal(groupUnitCost, result.CostBasis);
        Assert.Equal(currency, result.CostCurrency);
        Assert.Equal(expectedSource, result.CostSource);
        Assert.Equal(GroupMarginStatuses.Ok, result.Status);
    }

    [Fact]
    public void LRD_ohne_Konzernkosten_laesst_die_Kostenbasis_offen_statt_den_IC_Preis_zu_nehmen()
    {
        // Der lokale Standardpreis waere hier der IC-Einkaufspreis. Eine Marge darauf saehe
        // plausibel aus und waere falsch - schlechter als eine erkennbar offene Zeile.
        var result = GroupMarginCalculator.Evaluate(Line(salesType: "LRD"));

        Assert.True(result.IsGroupCostMissing);
        Assert.Equal(0m, result.CostBasis);
        Assert.Equal(GroupMarginStatuses.GroupCostMissing, result.Status);
        Assert.Equal(GroupMarginStatuses.GroupCostMissingSource, result.CostSource);
    }

    [Theory]
    [InlineData("FFM")]
    [InlineData("CM")]
    public void Eigenfertigung_rechnet_mit_dem_lokalen_Standardpreis(string salesType)
    {
        // FFM und CM fertigt der Standort selbst - der lokale Standardpreis IST dort die
        // Herstellkostenbasis und wird nicht als IC-Preis behandelt.
        var result = GroupMarginCalculator.Evaluate(Line(salesType: salesType));

        Assert.False(result.IsGroupCostMissing);
        Assert.False(result.IsGroupCost);
        Assert.Equal(60m, result.CostBasis);
        Assert.Equal(GroupMarginSupplierClassifier.Internal, result.SupplierType);
        Assert.Equal(GroupMarginStatuses.Ok, result.Status);
    }

    [Fact]
    public void Trafag_Sachnummer_ist_der_Schluessel_zu_den_Konzernkosten()
    {
        // Indien fuehrt eine eigene Artikelnummer; die Konzernkosten haengen an der
        // Trafag-Sachnummer. Ueber die lokale Nummer wuerde nichts gefunden.
        var result = GroupMarginCalculator.Evaluate(
            Line(salesType: "LRD", material: "IC15415", groupMaterialNumber: "8896.10.10"),
            GroupCosts("8896.10.10", 42m));

        Assert.Equal("8896.10.10", result.MaterialKey);
        Assert.True(result.IsGroupCost);
        Assert.Equal(42m, result.CostBasis);
    }

    [Fact]
    public void Gutschrift_dreht_die_Kostenbasis_mit()
    {
        // Umsatz -100, Kosten +60 ergaebe -160 statt korrekt -40.
        var result = GroupMarginCalculator.Evaluate(Line(salesType: "FFM", quantity: -1m, netSalesValue: -100m));

        Assert.Equal(-60m, result.CostBasis);
    }

    [Fact]
    public void Der_Sales_Type_bestimmt_Typ_und_Kostenbasis_auch_gegen_den_Lieferantentext()
    {
        // Die frueher hier festgehaltene ASYMMETRIE ist mit dem Entscheid von Ingo vom
        // 2026-08-27 aufgeloest: Wo ein Sales Type gepflegt ist, gilt er fuer BEIDES - fuer die
        // Klassifikation und fuer die Kostenbasis. Vorher gewann fuer den Typ der
        // Lieferantentext (Zeile galt als extern) und fuer die Kostenbasis der Sales Type;
        // dieselbe Zeile war damit gleichzeitig extern und Konzernvertrieb.
        // Siehe docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md Abschnitt 3b.
        var result = GroupMarginCalculator.Evaluate(
            Line(salesType: "LRD", supplierName: "Fremdlieferant GmbH", supplierCountry: "DE"));

        Assert.Equal(GroupMarginSupplierClassifier.Internal, result.SupplierType);
        // LRD ohne Konzernkostentreffer bleibt bewusst offen: der lokale Preis waere dort der
        // IC-Einkaufspreis und wird nicht als Herstellkostenbasis verwendet.
        Assert.True(result.IsGroupCostMissing);
        Assert.Equal(0m, result.CostBasis);
        Assert.Equal(GroupMarginStatuses.GroupCostMissing, result.Status);
    }

    [Fact]
    public void Ein_externer_Lieferant_ohne_Sales_Type_rechnet_mit_der_Verkaufszeile()
    {
        // Gegenprobe zur Asymmetrie oben: ohne Sales Type bleibt alles wie bisher.
        var result = GroupMarginCalculator.Evaluate(
            Line(supplierName: "Fremdlieferant GmbH", supplierCountry: "DE"));

        Assert.Equal(GroupMarginSupplierClassifier.External, result.SupplierType);
        Assert.False(result.IsGroupCostMissing);
        Assert.Equal(60m, result.CostBasis);
        Assert.Equal("Kosten aus Verkaufszeile", result.CostSource);
    }

    [Fact]
    public void Ohne_Lieferant_und_ohne_Sales_Type_bleibt_der_Lieferant_unklar()
    {
        var result = GroupMarginCalculator.Evaluate(Line());

        Assert.Equal(GroupMarginSupplierClassifier.Unclear, result.SupplierType);
        Assert.Equal(GroupMarginStatuses.SupplierUnclear, result.Status);
    }

    [Fact]
    public void Neuer_ChWerkstammFallback_Klassifiziert_Intern_UndNutztOhneMbewLokaleKosten()
    {
        IReadOnlySet<string> chPlantMaterials = new HashSet<string>(StringComparer.Ordinal) { "IC15415" };

        var result = GroupMarginCalculator.Evaluate(
            Line(tsc: "TRIT"),
            chPlantMaterialKeys: chPlantMaterials,
            supplierFallbackMode: SupplierFallbackModes.ChPlantMaster);

        Assert.Equal(GroupMarginSupplierClassifier.Internal, result.SupplierType);
        Assert.False(result.IsGroupCost);
        Assert.Equal(60m, result.CostBasis);
        Assert.Equal(GroupMarginStatuses.Ok, result.Status);
    }

    [Fact]
    public void Neuer_ChWerkstammFallback_Nichttreffer_NutztLokaleStandardkostenMitBerechenbarerMarge()
    {
        IReadOnlySet<string> chPlantMaterials = new HashSet<string>(StringComparer.Ordinal) { "OTHER-MATERIAL" };

        var result = GroupMarginCalculator.Evaluate(
            Line(tsc: "TRIT", material: "LOCAL-MATERIAL", standardCost: 60m),
            chPlantMaterialKeys: chPlantMaterials,
            supplierFallbackMode: SupplierFallbackModes.ChPlantMaster);

        Assert.Equal(GroupMarginSupplierClassifier.Local, result.SupplierType);
        Assert.False(result.IsGroupCost);
        Assert.False(result.IsGroupCostMissing);
        Assert.Equal(60m, result.CostBasis);
        Assert.Equal("Standardkosten der lokalen Gesellschaft", result.CostSource);
        Assert.Equal(GroupMarginStatuses.Ok, result.Status);
    }

    [Fact]
    public void LokalerSupplierFallback_UebergehtChWerkstammUndNutztLokaleStandardkosten()
    {
        IReadOnlySet<string> chPlantMaterials = new HashSet<string>(StringComparer.Ordinal) { "IC15415" };
        var groupCosts = GroupCosts("IC15415", 25m);

        var result = GroupMarginCalculator.Evaluate(
            Line(tsc: "TRIT", material: "IC15415", standardCost: 60m), groupCosts,
            chPlantMaterialKeys: chPlantMaterials,
            supplierFallbackMode: SupplierFallbackModes.LocalStandardCosts);

        Assert.Equal(GroupMarginSupplierClassifier.Local, result.SupplierType);
        Assert.False(result.IsGroupCost);
        Assert.Equal(60m, result.CostBasis);
        Assert.Equal("Standardkosten der lokalen Gesellschaft", result.CostSource);
        Assert.Equal(GroupMarginStatuses.Ok, result.Status);
    }

    [Fact]
    public void SchweizerStprsSchalter_UebersteuertBeiInternemLieferantenNurDieKostenquelle()
    {
        var groupCosts = GroupCosts("IC15415", 50m);
        groupCosts[("IC15415", GroupStandardCostAreas.ByEntity[GroupStandardCostEntities.TrIt])] = new GroupStandardCost
        {
            MaterialKey = "IC15415",
            ValuationArea = GroupStandardCostAreas.ByEntity[GroupStandardCostEntities.TrIt],
            UnitCost = 120m,
            Currency = "EUR"
        };
        IReadOnlySet<string> chPlantMaterials = new HashSet<string>(StringComparer.Ordinal) { "IC15415" };
        var line = Line(tsc: "TRIT", material: "IC15415", supplierName: "Trafag Italia S.r.l.");

        var defaultResult = GroupMarginCalculator.Evaluate(line, groupCosts, chPlantMaterialKeys: chPlantMaterials);
        var swissResult = GroupMarginCalculator.Evaluate(
            line, groupCosts, chPlantMaterialKeys: chPlantMaterials,
            internalSupplierCostSourceMode: InternalSupplierCostSourceModes.SwissStprsForChPlantMaterial);

        Assert.Equal(GroupMarginSupplierClassifier.Internal, swissResult.SupplierType);
        Assert.Equal(120m, defaultResult.CostBasis);
        Assert.Equal("Konzernkosten TR IT (B1 StockPrice)", defaultResult.CostSource);
        Assert.Equal(50m, swissResult.CostBasis);
        Assert.Equal("CHF", swissResult.CostCurrency);
        Assert.Equal(GroupMarginCalculator.GroupCostSourceLabel, swissResult.CostSource);
    }

    [Fact]
    public void SchweizerStprsSchalter_GreiftNichtBeiExternemLieferanten()
    {
        // Der Schalter heisst "Kostenquelle bei INTERNEM Lieferanten". Bis zum 2026-09-02 pruefte
        // er das nicht und setzte die liefernde Gesellschaft bei jedem CH-Werkstammtreffer auf
        // TR_AG - auch fuer eine Zeile mit echtem Drittlieferanten. Damit erfand er einen
        // Konzernbezug, den die Klassifikation gerade NICHT gefunden hatte, und das fuer jedes
        // der rund 66'000 Materialien im CH-Werkstamm.
        var groupCosts = GroupCosts("IC15415", 50m);
        IReadOnlySet<string> chPlantMaterials = new HashSet<string>(StringComparer.Ordinal) { "IC15415" };
        var line = Line(
            tsc: "TRIT", material: "IC15415",
            supplierName: "ITEC S.R.L.", supplierNumber: "V4711", supplierCountry: "IT");

        var result = GroupMarginCalculator.Evaluate(
            line, groupCosts, chPlantMaterialKeys: chPlantMaterials,
            internalSupplierCostSourceMode: InternalSupplierCostSourceModes.SwissStprsForChPlantMaterial);

        Assert.Equal(GroupMarginSupplierClassifier.External, result.SupplierType);
        Assert.False(result.IsGroupCost);
        Assert.Equal(60m, result.CostBasis);
        Assert.Equal("Kosten aus Verkaufszeile", result.CostSource);
    }

    [Fact]
    public void SchweizerStprsSchalter_GreiftNichtBeiLokalerKlassifikationOhneLieferanten()
    {
        // Kombination zweier Schalter: der Fallback-Modus "LocalStandardCosts" sagt ausdruecklich,
        // dass ohne Lieferant die Kosten der verkaufenden Gesellschaft gelten - unabhaengig von
        // einem MARC-Treffer. Der STPRS-Schalter darf diese bewusste Entscheidung nicht durch die
        // Hintertuer wieder aufheben; sonst haetten sich die beiden Schalter widersprochen.
        var groupCosts = GroupCosts("IC15415", 50m);
        IReadOnlySet<string> chPlantMaterials = new HashSet<string>(StringComparer.Ordinal) { "IC15415" };

        var result = GroupMarginCalculator.Evaluate(
            Line(tsc: "TRDE", material: "IC15415"), groupCosts,
            chPlantMaterialKeys: chPlantMaterials,
            supplierFallbackMode: SupplierFallbackModes.LocalStandardCosts,
            internalSupplierCostSourceMode: InternalSupplierCostSourceModes.SwissStprsForChPlantMaterial);

        Assert.Equal(GroupMarginSupplierClassifier.Local, result.SupplierType);
        Assert.False(result.IsGroupCost);
        Assert.Equal(60m, result.CostBasis);
        Assert.Equal("Standardkosten der lokalen Gesellschaft", result.CostSource);
    }

    [Fact]
    public void Lokaler_Nichttreffer_OhneStandardkosten_BleibtAlsFehlendeKostenOffen()
    {
        IReadOnlySet<string> chPlantMaterials = new HashSet<string>(StringComparer.Ordinal) { "OTHER-MATERIAL" };

        var result = GroupMarginCalculator.Evaluate(
            Line(tsc: "TRIT", material: "LOCAL-MATERIAL", standardCost: 0m),
            chPlantMaterialKeys: chPlantMaterials,
            supplierFallbackMode: SupplierFallbackModes.ChPlantMaster);

        Assert.Equal(GroupMarginSupplierClassifier.Local, result.SupplierType);
        Assert.Equal(GroupMarginStatuses.StandardCostMissing, result.Status);
    }

    [Fact]
    public void Konzernkosten_fehlen_wird_vor_Standardpreis_fehlt_gemeldet()
    {
        // Beide Faelle haben Kostenbasis 0. Ein gemeinsames Label wuerde die Ursache verdecken:
        // hier IST ein Standardpreis vorhanden, er taugt nur nicht als Herstellkostenbasis.
        var result = GroupMarginCalculator.Evaluate(Line(salesType: "LRD", standardCost: 60m));

        Assert.Equal(0m, result.CostBasis);
        Assert.Equal(GroupMarginStatuses.GroupCostMissing, result.Status);
        Assert.NotEqual(GroupMarginStatuses.StandardCostMissing, result.Status);
    }

    [Fact]
    public void Fehlender_Kurs_ueberlagert_jede_andere_Aussage_zur_Zeile()
    {
        var result = GroupMarginCalculator.Evaluate(Line(salesType: "LRD"), hasExchangeRate: false);

        Assert.Equal(GroupMarginStatuses.ExchangeRateMissing, result.Status);
    }

    [Fact]
    public void Kein_Kostenbasiswert_bei_fehlendem_Standardpreis()
    {
        var result = GroupMarginCalculator.Evaluate(Line(salesType: "FFM", standardCost: 0m));

        Assert.Equal(GroupMarginStatuses.StandardCostMissing, result.Status);
    }

    [Fact]
    public void Umsatz_null_wird_als_eigener_Diagnosefall_gemeldet()
    {
        var result = GroupMarginCalculator.Evaluate(Line(salesType: "FFM", netSalesValue: 0m));

        Assert.Equal(GroupMarginStatuses.SalesMissing, result.Status);
    }

    [Fact]
    public void Alle_offenen_Stati_gelten_als_offen_und_sortieren_vor_OK()
    {
        foreach (var status in GroupMarginStatuses.Open)
        {
            Assert.True(GroupMarginStatuses.IsOpen(status), status);
            Assert.True(
                GroupMarginStatuses.Sort(status) < GroupMarginStatuses.Sort(GroupMarginStatuses.Ok), status);
        }

        // Umsatz fehlt ist kein Kostenbasisproblem, sortiert aber vor OK.
        Assert.False(GroupMarginStatuses.IsOpen(GroupMarginStatuses.SalesMissing));
        Assert.True(
            GroupMarginStatuses.Sort(GroupMarginStatuses.SalesMissing) <
            GroupMarginStatuses.Sort(GroupMarginStatuses.Ok));
    }

    [Fact]
    public void Konzernkosten_fehlen_zaehlt_als_offene_Kostenbasis()
    {
        // Genau das fehlte im Cockpit: der Status war da, galt aber nicht als offen.
        Assert.Contains(GroupMarginStatuses.GroupCostMissing, GroupMarginStatuses.Open);
    }

    [Fact]
    public void Fehlender_Kurs_sortiert_im_Audit_Ledger_ganz_nach_vorne()
    {
        Assert.Equal(0, GroupMarginStatuses.AuditLedgerSort(GroupMarginStatuses.ExchangeRateMissing));
        Assert.True(
            GroupMarginStatuses.AuditLedgerSort(GroupMarginStatuses.GroupCostMissing) <
            GroupMarginStatuses.AuditLedgerSort(GroupMarginStatuses.Ok));
    }

    [Fact]
    public void Materialschluessel_wird_wie_ueberall_normalisiert()
    {
        // Excel hatte hierfuer eine eigene Kopie, die bei einer Nummer aus lauter Nullen von der
        // gemeinsamen Fassung abwich. Jetzt gilt fuer beide Wege MaterialKeyNormalizer.
        Assert.Equal(
            MaterialKeyNormalizer.Normalize("000"),
            GroupMarginCalculator.ResolveGroupCostKey(Line(material: "000")));
        Assert.Equal("MAT1", GroupMarginCalculator.ResolveGroupCostKey(Line(material: " mat1 ")));
    }

    [Fact]
    public void Fehlende_Kostenbasis_und_abweichende_Kostenwaehrung_sind_nicht_dasselbe()
    {
        // Beide Faelle sind „offen", aber nur bei den ersten dreien fehlt die Kostenbasis. Wer
        // eine Marge rechnet, darf deshalb nicht IsOpen als Pruefung nehmen: bei abweichender
        // Kostenwaehrung ist die Kostenbasis bekannt, die CHF-Marge bleibt rechenbar.
        Assert.False(GroupMarginStatuses.IsCostBasisKnown(GroupMarginStatuses.StandardCostMissing));
        Assert.False(GroupMarginStatuses.IsCostBasisKnown(GroupMarginStatuses.SupplierUnclear));
        Assert.False(GroupMarginStatuses.IsCostBasisKnown(GroupMarginStatuses.GroupCostMissing));

        Assert.True(GroupMarginStatuses.IsCostBasisKnown(GroupMarginCostCurrencyConverter.OpenStatus));
        Assert.True(GroupMarginStatuses.IsCostBasisKnown(GroupMarginStatuses.Ok));
        Assert.True(GroupMarginStatuses.IsCostBasisKnown(GroupMarginStatuses.SalesMissing));

        // Jeder Status ohne Kostenbasis gilt zugleich als offen — sonst zaehlte die Kennzahl
        // „offene Kostenbasis" eine Zeile nicht mit, deren Marge leer bleibt.
        foreach (var status in new[]
                 {
                     GroupMarginStatuses.StandardCostMissing,
                     GroupMarginStatuses.SupplierUnclear,
                     GroupMarginStatuses.GroupCostMissing
                 })
        {
            Assert.True(GroupMarginStatuses.IsOpen(status), status);
        }
    }
}
