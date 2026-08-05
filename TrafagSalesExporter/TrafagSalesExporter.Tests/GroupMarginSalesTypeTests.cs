using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Auswertung des Artikelstammfeldes „Sales Type" (Indien: <c>OITM.U_Tasc_ST</c>) in der
/// Gruppenmarge. Fachliche Grundlage und Messwerte:
/// docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md.
/// </summary>
public class GroupMarginSalesTypeTests
{
    private static readonly IReadOnlyDictionary<(string MaterialKey, string ValuationArea), GroupStandardCost> NoGroupCosts
        = new Dictionary<(string, string), GroupStandardCost>();

    [Theory]
    [InlineData("FFM", SalesTypeRoles.OwnManufacturing)]
    [InlineData("ffm", SalesTypeRoles.OwnManufacturing)]
    [InlineData(" LRD ", SalesTypeRoles.GroupDistribution)]
    [InlineData("CM", SalesTypeRoles.ContractManufacturing)]
    public void ResolveSalesTypeRole_erkennt_die_drei_Rollen(string value, string expected)
    {
        Assert.Equal(expected, GroupMarginSupplierClassifier.ResolveSalesTypeRole(value));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("--")]
    [InlineData("XYZ")]
    public void ResolveSalesTypeRole_raet_nicht_bei_leerem_oder_unbekanntem_Wert(string? value)
    {
        // Der Platzhalter aus zwei Bindestrichen bedeutet im indischen Stamm „nicht gepflegt".
        Assert.Null(GroupMarginSupplierClassifier.ResolveSalesTypeRole(value));
    }

    [Theory]
    [InlineData("FFM")]
    [InlineData("CM")]
    [InlineData("LRD")]
    public void Resolve_ist_intern_wenn_der_Sales_Type_eine_Konzernrolle_nennt(string salesType)
    {
        // Alle drei Werte bezeichnen eine Konzernrolle, keinen Fremdbezug - also nie „Unklar",
        // auch ohne jedes Lieferantenfeld. Das ist der Kern der Aenderung: rund 5'830 TRIN-Zeilen
        // waren nur deshalb maskiert.
        var result = GroupMarginSupplierClassifier.Resolve(
            null, null, null, "TRIN", "PT000003", NoGroupCosts, salesType);

        Assert.Equal(GroupMarginSupplierClassifier.Internal, result);
    }

    [Fact]
    public void Resolve_bleibt_unklar_wenn_weder_Lieferant_noch_Sales_Type_vorliegt()
    {
        var result = GroupMarginSupplierClassifier.Resolve(
            null, null, null, "TRIN", "PT000003", NoGroupCosts, salesType: "");

        Assert.Equal(GroupMarginSupplierClassifier.Unclear, result);
    }

    [Fact]
    public void Resolve_laesst_einen_vorhandenen_Lieferantentext_vorgehen()
    {
        // Produktiv widersprechen sich beide Felder bei 10 TRIN-Artikeln (Sales Type FFM, aber
        // Lieferant gepflegt). Solange offen ist, welches Feld gilt, bleibt das Verhalten dieser
        // Zeilen unveraendert, statt es auf eine Vermutung umzustellen.
        var externalSupplier = GroupMarginSupplierClassifier.Resolve(
            "V0393", "Cenlub Systems", "IN", "TRIN", "PS000358", NoGroupCosts, "FFM");
        Assert.Equal(GroupMarginSupplierClassifier.External, externalSupplier);

        var internalSupplier = GroupMarginSupplierClassifier.Resolve(
            "V0078", "Trafag AG", "CH", "TRIN", "PT000003", NoGroupCosts, "FFM");
        Assert.Equal(GroupMarginSupplierClassifier.Internal, internalSupplier);
    }

    [Theory]
    [InlineData("FFM")]
    [InlineData("CM")]
    public void ResolveDeliveringEntity_ist_der_Standort_selbst_wenn_er_fertigt(string salesType)
    {
        // Bei CM fertigt Indien im Auftrag von Trafag AG - die Herstellkosten entstehen trotzdem
        // in Indien, liefernde Gesellschaft ist also TR IN.
        var entity = GroupMarginSupplierClassifier.ResolveDeliveringEntity(
            supplierName: null, tsc: "TRIN", normalizedMaterialKey: "PT000003",
            groupStandardCosts: NoGroupCosts, salesType: salesType);

        Assert.Equal(GroupStandardCostEntities.TrIn, entity);
    }

    [Fact]
    public void ResolveDeliveringEntity_ist_TR_AG_bei_Konzernvertrieb()
    {
        // Belegt am 2026-08-05: alle 93 LRD-Artikel mit gepflegtem Lieferanten zeigen auf
        // V0078 = Trafag AG, ohne Ausnahme.
        var entity = GroupMarginSupplierClassifier.ResolveDeliveringEntity(
            supplierName: null, tsc: "TRIN", normalizedMaterialKey: "57291",
            groupStandardCosts: NoGroupCosts, salesType: "LRD");

        Assert.Equal(GroupStandardCostEntities.TrAg, entity);
    }

    [Fact]
    public void ResolveDeliveringEntity_raet_keinen_Standort_bei_unbekanntem_TSC()
    {
        var entity = GroupMarginSupplierClassifier.ResolveDeliveringEntity(
            supplierName: null, tsc: "TRFR", normalizedMaterialKey: "X1",
            groupStandardCosts: NoGroupCosts, salesType: "FFM");

        Assert.Null(entity);
    }

    [Fact]
    public void Bestehendes_Verhalten_ohne_Sales_Type_bleibt_unveraendert()
    {
        // Regressionsschutz fuer die Standorte, deren Quelle das Feld nicht fuehrt: dort ist
        // salesType leer und es muss genau wie vorher klassifiziert werden.
        Assert.Equal(
            GroupMarginSupplierClassifier.Internal,
            GroupMarginSupplierClassifier.Resolve(null, "Trafag AG", "CH"));
        Assert.Equal(
            GroupMarginSupplierClassifier.External,
            GroupMarginSupplierClassifier.Resolve(null, "Fremdlieferant SA", "FR"));
        Assert.Equal(
            GroupMarginSupplierClassifier.Unclear,
            GroupMarginSupplierClassifier.Resolve(null, null, null, "TRES"));
        Assert.Equal(
            GroupMarginSupplierClassifier.Internal,
            GroupMarginSupplierClassifier.Resolve(null, null, null, "TRCH"));
    }
}
