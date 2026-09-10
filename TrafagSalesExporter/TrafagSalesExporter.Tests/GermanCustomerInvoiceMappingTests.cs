using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class GermanCustomerInvoiceMappingTests
{
    [Theory]
    [InlineData("00 Bahn", true)]
    [InlineData("05 rw Railways / Bahntechnik", true)]
    [InlineData("00 Bahn, 13 Flugzeugbau", false)]
    [InlineData("00 Bahntechnik ungeklaert", false)]
    [InlineData("", false)]
    public void RailwayRequiresUnambiguousIndustry(string industry, bool expected)
        => Assert.Equal(expected, CentralSalesRecordService.IsGermanRailwayIndustry(industry));

    [Fact]
    public void ProvenInvoiceResolvesOnlyItsOwnInternalAddress()
    {
        var entry = GermanCustomerInvoiceMapping.Find("GS2510095", "4044");
        Assert.NotNull(entry);
        Assert.Equal("12602", entry.CustomerNumber);
        Assert.Equal("Belegnachweis", entry.Basis);
        Assert.Null(GermanCustomerInvoiceMapping.Find("GS2510095", "12602"));
    }

    [Fact]
    public void UnknownInvoiceNeverUsesInternalIdAsCustomerNumber()
    {
        Assert.Null(GermanCustomerInvoiceMapping.Find("NEW-INVOICE", "55013"));
        Assert.Null(GermanCustomerInvoiceMapping.Find("RE2510053", "not-the-matching-id"));
        // Frueher stand hier RE2610696 mit 419 als Beispiel fuer ein unbekanntes Paar.
        // Rohails Rechnungsliste vom 2026-09-09 belegt es inzwischen, siehe Test unten.
        Assert.Null(GermanCustomerInvoiceMapping.Find("RE2611082", "9999"));
        Assert.Null(GermanCustomerInvoiceMapping.Find("GS9999999", "4044"));
    }

    [Fact]
    public void InvoiceListEvidenceResolvesPreviouslyOpenInvoices()
    {
        // Aus Rohails Rechnungsliste vom 2026-09-09: die Rechnung galt vorher nur als
        // historisch ableitbar und traegt jetzt einen Belegnachweis.
        var entry = GermanCustomerInvoiceMapping.Find("RE2610696", "419");
        Assert.NotNull(entry);
        Assert.Equal("10340", entry.CustomerNumber);
        Assert.Equal("D", entry.CustomerCountry);
        Assert.Equal("Belegnachweis", entry.Basis);
        // Die Adresse gilt nur fuer ihre eigene Rechnung, nicht fuer eine fremde.
        Assert.Null(GermanCustomerInvoiceMapping.Find("RE2610697", "419"));
    }
}
