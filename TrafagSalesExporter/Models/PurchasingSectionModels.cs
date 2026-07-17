using MudBlazor;

namespace TrafagSalesExporter.Models;

public sealed record PurchasingSectionKpi(string LabelDe, string LabelEn, string Value, string DetailDe, string DetailEn);

public sealed record PurchasingSectionChartRow(string Label, string Value, double Percent, string Color);

public sealed record PurchasingSectionStatusRow(string LabelDe, string LabelEn, string Value, string Icon, Color Color);

public sealed record PurchasingSectionDetailRow(string LabelDe, string LabelEn, string Value, string Dimension, string Source);

public sealed record PurchasingSupplierYearSpendRow(string Supplier, IReadOnlyDictionary<int, decimal> YearValues, decimal Total)
{
    /// <summary>
    /// Drilldown-Ebene je Lieferant (Marco/Armin-Review 2026-07-17): Spend des Lieferanten
    /// aufgerissen nach Warengruppe. Bevorzugt die aktuelle Materialstamm-Warengruppe
    /// (MaraMatkl), faellt auf die Beleg-Warengruppe (EKPO.Matkl) zurueck, solange SAP
    /// MARA-MATKL noch nicht liefert.
    /// </summary>
    public IReadOnlyList<PurchasingSpendGroupYearRow> MaterialGroups { get; init; } = [];
}

public sealed record PurchasingSpendGroupYearRow(string MaterialGroup, IReadOnlyDictionary<int, decimal> YearValues, decimal Total);
