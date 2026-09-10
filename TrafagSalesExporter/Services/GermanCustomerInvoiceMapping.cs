using System.Text.Json;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Bounded repair of the September 2026 raw export. An invoice AND its internal
/// address ID must match. No general numerical join or extrapolation to new invoices.
/// The embedded evidence travels with the deployment and remains auditable.
/// </summary>
internal static class GermanCustomerInvoiceMapping
{
    private static readonly Lazy<Dictionary<(string, string), Entry>> Entries = new(() =>
    {
        using var stream = typeof(GermanCustomerInvoiceMapping).Assembly.GetManifestResourceStream(
            "TrafagSalesExporter.Data.de_customer_invoice_map_2026-09-10.json")
            ?? throw new InvalidOperationException("DE-Kundenbelegbruecke fehlt im Build.");
        return (JsonSerializer.Deserialize<List<Entry>>(stream) ?? [])
            .ToDictionary(x => (x.InvoiceNumber, x.InternalId));
    });

    internal static Entry? Find(string invoice, string internalId)
    {
        var entry = Entries.Value.GetValueOrDefault((invoice.Trim(), internalId.Trim()));
        // Historical uniqueness does not establish temporal stability: two internal
        // IDs already changed external number. Candidates remain in the audit only.
        return entry?.Basis == "Belegnachweis" ? entry : null;
    }

    internal sealed record Entry(string InvoiceNumber, string InternalId,
        string CustomerNumber, string CustomerName, string CustomerCountry,
        string CustomerIndustry, string Basis);
}
