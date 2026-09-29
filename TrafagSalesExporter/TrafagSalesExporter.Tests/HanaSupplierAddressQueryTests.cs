using System.Text.RegularExpressions;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class HanaSupplierAddressQueryTests
{
    public static TheoryData<string, string?> Queries() => new()
    {
        { "invoice", "BillToDef" },
        { "invoice", null },
        { "credit", "BillToDef" },
        { "credit", null },
    };

    private static string Build(string kind, string? billToDefault)
        => kind == "invoice"
            ? HanaQueryService.GetInvoiceQuery("IT01_P", null, null, billToDefault)
            : HanaQueryService.GetCreditNoteQuery("IT01_P", null, null, billToDefault);

    /// <summary>
    /// ISS-021: Der Join auf alle Rechnungsadressen des Lieferanten verdoppelte in Italien jede
    /// Position von Trafag AG (Adressen CH und DE). Jede Verknuepfung mit CRD1 fuer den Lieferanten
    /// muss auf hoechstens eine Zeile je Lieferant beschraenkt sein.
    /// </summary>
    [Theory]
    [MemberData(nameof(Queries))]
    public void SupplierAddress_IsLimitedToOneAddressPerSupplier(string kind, string? billToDefault)
    {
        var query = Build(kind, billToDefault);

        Assert.DoesNotMatch(new Regex(@"sup_adr ON itm\.""CardCode"" = sup_adr\.""CardCode""\s+AND sup_adr\.""AdresType"" = 'B'\s*\r?\n"), query);
        Assert.Contains(@"GROUP BY ""CardCode""", query);
        Assert.Contains(@"sup_adr_any ON itm.""CardCode"" = sup_adr_any.""CardCode""", query);

        if (billToDefault is null)
        {
            Assert.DoesNotContain("BillToDef", query);
            Assert.Contains(@"COALESCE(sup_adr_any.""Country"", '') AS supplier_country", query);
        }
        else
        {
            Assert.Contains(@"AND sup_adr.""Address"" = sup.""BillToDef""", query);
            Assert.Contains(@"COALESCE(sup_adr.""Country"", sup_adr_any.""Country"", '') AS supplier_country", query);
        }
    }

    /// <summary>
    /// Die Kundenadresse war schon richtig ueber PayToCode auf eine Adresse beschraenkt und darf
    /// bei dieser Korrektur nicht verloren gehen.
    /// </summary>
    [Theory]
    [MemberData(nameof(Queries))]
    public void CustomerAddress_StaysLimitedToThePayToAddress(string kind, string? billToDefault)
        => Assert.Contains(@"cust_adr.""Address"" = h.""PayToCode""", Build(kind, billToDefault));
}
