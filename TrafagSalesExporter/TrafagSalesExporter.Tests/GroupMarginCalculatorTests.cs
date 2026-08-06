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
        string materialKey, decimal unitCost, string currency = "CHF")
        => new()
        {
            [(materialKey, TrAgArea)] = new GroupStandardCost
            {
                MaterialKey = materialKey,
                ValuationArea = TrAgArea,
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
    public void Lieferantentext_bestimmt_den_Typ_der_Sales_Type_die_Kostenbasis()
    {
        // Produktiv widersprechen sich beide Felder bei 10 TRIN-Artikeln (Sales Type gepflegt UND
        // Lieferant gepflegt). Das Verhalten ist dort ASYMMETRISCH und war es schon vor der
        // Zusammenfuehrung: fuer die Klassifikation gewinnt der Lieferantentext (die Zeile gilt
        // als extern), fuer die Kostenbasis gewinnt der Sales Type (LRD laesst sie offen).
        // Der Test haelt das absichtlich fest, statt es stillschweigend zu vereinheitlichen -
        // welches Feld gilt, ist mit Indien noch zu klaeren.
        // Siehe docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md Abschnitt 3b.
        var result = GroupMarginCalculator.Evaluate(
            Line(salesType: "LRD", supplierName: "Fremdlieferant GmbH", supplierCountry: "DE"));

        Assert.Equal(GroupMarginSupplierClassifier.External, result.SupplierType);
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
