using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class CreditDocumentDetectionTests
{
    /// <summary>
    /// ISS-026: Die Gutschrift- und Stornobelegarten aus dem Bestand muessen auch bei positivem
    /// Betrag als Gutschrift erkannt werden, nicht nur ueber das Vorzeichen.
    /// </summary>
    [Theory]
    [InlineData("G2", "90012345")]
    [InlineData("S1", "90012346")]
    [InlineData("S2", "90012347")]
    [InlineData("CRN", "400123")]
    [InlineData("crn", "400124")]
    [InlineData("REC", "R-2026-17")]
    [InlineData("Alphaplan CreditNote", "GU-1")]
    [InlineData("Credit Note", "AB-2026-3")]
    public void CreditDocumentTypes_AreRecognised(string documentType, string invoiceNumber)
        => Assert.True(ManagementCockpitService.LooksLikeCreditDocument(documentType, invoiceNumber));

    /// <summary>
    /// Der fruehere Teilstring "rec" traf Rechnungen, deren Belegart oder Nummer diese Buchstaben
    /// enthaelt. Rechnungsbelegarten duerfen nicht als Gutschrift gelten.
    /// </summary>
    [Theory]
    [InlineData("F2", "90011111")]
    [InlineData("F8", "90011112")]
    [InlineData("INV", "400001")]
    [InlineData("Invoice", "PREC-2026-1")]
    [InlineData("Alphaplan Invoice", "RE-2026-REC")]
    public void InvoiceDocumentTypes_AreNotCredits(string documentType, string invoiceNumber)
        => Assert.False(ManagementCockpitService.LooksLikeCreditDocument(documentType, invoiceNumber));
}
