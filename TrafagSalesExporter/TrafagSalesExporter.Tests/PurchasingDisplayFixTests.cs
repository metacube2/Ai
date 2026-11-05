using System.Globalization;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>Anzeige-Fixes aus der Einkauf-Pruefung 2026-11-05 (F8, F9), ohne Blazor-Rendering pruefbar.</summary>
public class PurchasingDisplayFixTests
{
    [Theory]
    [InlineData("SAP-Datenbestand", PurchasingSectionDataState.Loaded)]
    [InlineData("SAP-Stichprobe", PurchasingSectionDataState.Loaded)]
    [InlineData("Stored SAP data", PurchasingSectionDataState.Loaded)]
    [InlineData("Wartet auf SAP", PurchasingSectionDataState.Waiting)]
    [InlineData("wartet auf EKPO", PurchasingSectionDataState.Waiting)]
    [InlineData("", PurchasingSectionDataState.Waiting)]
    [InlineData("Simulation", PurchasingSectionDataState.Simulation)]
    public void Section_Source_Is_Classified_By_Loaded_State_Not_By_The_Old_Live_Literal(string source, PurchasingSectionDataState expected)
        => Assert.Equal(expected, PurchasingSectionSource.Classify(source));

    [Fact]
    public void Section_With_Loaded_Sap_Data_Is_Not_Waiting_For_Sap()
    {
        // F8: seit d5bc321 heisst die Quelle "SAP-Datenbestand"; die Section zeigte trotzdem "Wartet auf SAP".
        var rows = new[]
        {
            new PurchasingSectionDetailRow("Offener Wert", "Open value", "CHF 1", "EKET", "SAP-Datenbestand"),
            new PurchasingSectionDetailRow("Faelligkeit", "Due date", "-", "EKET", "Wartet auf SAP")
        };
        Assert.Equal(PurchasingSectionDataState.Loaded, PurchasingSectionSource.Resolve(rows));
        Assert.Equal(PurchasingSectionDataState.Simulation, PurchasingSectionSource.Resolve(
            [new PurchasingSectionDetailRow("Spend", "Spend", "1", "x", "Simulation"), rows[1]]));
        Assert.Equal(PurchasingSectionDataState.Waiting, PurchasingSectionSource.Resolve([rows[1]]));
    }

    [Theory]
    [InlineData(0.04, 100, "< 0.1 %")]
    [InlineData(0.05, 100, "0.1 %")]
    [InlineData(0, 100, "0 %")]
    [InlineData(25, 100, "25 %")]
    [InlineData(1, 3, "33.3 %")]
    [InlineData(5, 0, "-")]
    public void Material_Share_Shows_Small_Values_As_Less_Than_Zero_Point_One(double value, double total, string expected)
        => Assert.Equal(expected, PurchasingMaterialShareQuery.FormatShare((decimal)value, (decimal)total, CultureInfo.InvariantCulture));
}
