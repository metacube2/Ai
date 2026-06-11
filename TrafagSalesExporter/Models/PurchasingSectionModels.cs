using MudBlazor;

namespace TrafagSalesExporter.Models;

public sealed record PurchasingSectionKpi(string LabelDe, string LabelEn, string Value, string DetailDe, string DetailEn);

public sealed record PurchasingSectionChartRow(string Label, string Value, double Percent, string Color);

public sealed record PurchasingSectionStatusRow(string LabelDe, string LabelEn, string Value, string Icon, Color Color);

public sealed record PurchasingSectionDetailRow(string LabelDe, string LabelEn, string Value, string Dimension, string Source);
