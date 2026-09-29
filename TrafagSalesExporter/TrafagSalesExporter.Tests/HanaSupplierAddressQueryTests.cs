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
    /// ISS-021: Der provisorische Italien-Filter vom 2026-05-18 schloss sechs echte Kunden aus
    /// (1,84 Mio. EUR 2025) und glich damit nur die Doppelzeilen aus. Der Kontenfilter 47005 bleibt,
    /// ein Kundenausschluss darf nicht wiederkommen.
    /// </summary>
    [Theory]
    [InlineData("invoice")]
    [InlineData("credit")]
    public void ItalyQuery_KeepsTheRevenueAccountFilterWithoutCustomerExclusion(string kind)
    {
        var query = kind == "invoice"
            ? HanaQueryService.GetInvoiceQuery("it01_p", null, null, "BillToDef")
            : HanaQueryService.GetCreditNoteQuery("it01_p", null, null, "BillToDef");

        Assert.Contains(@"p.""AcctCode"" LIKE '47005%'", query);
        Assert.Contains(@"p.""AcctCode"" NOT LIKE '4700504%'", query);
        Assert.DoesNotContain(@"""CardCode"" NOT IN", query);
        Assert.DoesNotContain("C_IT01_0022987", query);
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
