using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Tests fuer die Auswahl des Materialtexts aus MAKT
/// (<see cref="PurchasingDataRefreshService.SelectMaterialTexts"/>).
///
/// Hintergrund (Messung 2026-08-18 auf T76/100, Report docs/abap/Z_PURCHASING_MAKTX_ANALYSE.abap):
/// MAKT ist ueber MATNR + SPRAS geschluesselt und liefert je Material mehrere Zeilen. Zu den 3'682
/// Einkaufsmaterialien gibt es 6'390 MAKT-Zeilen, 1'425 Materialien (rund 39 %) sind mehrsprachig
/// gepflegt. MAKTSet liefert alle Sprachen auf einmal. Ohne eine deterministische Auswahl haenge
/// der angezeigte Text von der Antwortreihenfolge ab. Deutsch ist mit 3'681 von 3'682 die richtige
/// Leitsprache; der Rueckfall greift fuer genau ein Material.
/// </summary>
public class PurchasingMaterialTextTests
{
    private static Dictionary<string, object?> Row(string matnr, string spras, string maktx) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Matnr"] = matnr,
            ["Spras"] = spras,
            ["Maktx"] = maktx
        };

    [Fact]
    public void SelectMaterialTexts_Prefers_German_Over_Other_Languages()
    {
        // Der Regelfall bei 39 % der Materialien: mehrere Sprachen, Deutsch muss gewinnen -
        // unabhaengig davon, in welcher Reihenfolge SAP die Zeilen liefert.
        var map = PurchasingDataRefreshService.SelectMaterialTexts(
        [
            Row("B64880", "EN", "PCBA HYBRID DENSITY english"),
            Row("B64880", "FR", "PCBA HYBRID DENSITY francais"),
            Row("B64880", "DE", "PCBA HYBRID DENSITY 6.5...20mA 56KG/m3")
        ]);

        Assert.Equal("PCBA HYBRID DENSITY 6.5...20mA 56KG/m3", map["B64880"]);
    }

    [Fact]
    public void SelectMaterialTexts_Falls_Back_To_English_When_German_Missing()
    {
        // Genau ein Material im Einkauf hat keinen deutschen Text. Dafuer greift diese Stufe.
        var map = PurchasingDataRefreshService.SelectMaterialTexts(
        [
            Row("X1", "FR", "texte francais"),
            Row("X1", "EN", "english text")
        ]);

        Assert.Equal("english text", map["X1"]);
    }

    [Fact]
    public void SelectMaterialTexts_Falls_Back_To_Any_Language_As_Last_Resort()
    {
        var map = PurchasingDataRefreshService.SelectMaterialTexts([Row("X2", "IT", "testo italiano")]);

        Assert.Equal("testo italiano", map["X2"]);
    }

    [Fact]
    public void SelectMaterialTexts_Keeps_First_Text_When_Languages_Rank_Equally()
    {
        // Zwei gleichrangige Sprachen (beide Rang "sonstige"): das Ergebnis muss stabil sein und
        // darf nicht bei jedem Load zwischen FR und IT wechseln.
        var map = PurchasingDataRefreshService.SelectMaterialTexts(
        [
            Row("X3", "FR", "zuerst gelesen"),
            Row("X3", "IT", "danach gelesen")
        ]);

        Assert.Equal("zuerst gelesen", map["X3"]);
    }

    [Fact]
    public void SelectMaterialTexts_Accepts_Single_Letter_Sap_Language_Keys()
    {
        // SAP liefert den Sprachschluessel je nach Service als ISO-Code ("DE") oder als
        // einstelliges Kennzeichen ("D"). Beide muessen als Deutsch gelten, sonst faellt die
        // Auswahl still auf eine Fremdsprache zurueck.
        var map = PurchasingDataRefreshService.SelectMaterialTexts(
        [
            Row("X4", "E", "english text"),
            Row("X4", "D", "deutscher Text")
        ]);

        Assert.Equal("deutscher Text", map["X4"]);
    }

    [Fact]
    public void SelectMaterialTexts_Normalizes_Material_Number_Like_The_Cache_Join()
    {
        // Der EKPO-Cache haelt Materialnummern SAP-intern zero-padded. Der Schluessel muss
        // identisch zu NormalizeMatnr sein, sonst findet der Join den Text nie.
        var map = PurchasingDataRefreshService.SelectMaterialTexts(
        [
            Row("000000000000002217", "DE", "Sensorgehaeuse")
        ]);

        Assert.Equal("Sensorgehaeuse", map["2217"]);
    }

    [Fact]
    public void SelectMaterialTexts_Ignores_Empty_Text_And_Empty_Material()
    {
        // Ein leerer Text darf einen vorhandenen nicht verdraengen und keine leeren Eintraege
        // erzeugen - sonst zeigt der Drilldown "Nummer - " mit leerem Anhang.
        var map = PurchasingDataRefreshService.SelectMaterialTexts(
        [
            Row("X5", "DE", "   "),
            Row("X5", "EN", "english text"),
            Row("", "DE", "ohne Materialnummer")
        ]);

        Assert.Equal("english text", map["X5"]);
        Assert.Single(map);
    }
}
