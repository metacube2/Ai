using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public sealed class BomInheritanceTests
{
    // Schraube -> Gehaeuse -> Sensor A (Endprodukt), Gehaeuse -> Sensor B (Endprodukt), Schraube -> Sensor C.
    private static readonly (string, string)[] Pairs =
    [
        ("SCHRAUBE", "GEHAEUSE"),
        ("GEHAEUSE", "SENSOR-A"),
        ("GEHAEUSE", "SENSOR-B"),
        ("SCHRAUBE", "SENSOR-C"),
        ("DICHTUNG", "SENSOR-A")
    ];

    private static BomInheritanceResult Build(params (string, string, string)[] suppliers)
        => BomInheritanceService.Build(Pairs, suppliers, ["DICHTUNG"], new Dictionary<string, string> { ["SENSOR-A"] = "Sensor A" }, null);

    [Fact]
    public void Wirkung_Und_Tiefe_Ueber_Mehrere_Stufen()
    {
        var result = Build();

        var schraube = Assert.Single(result.ComponentImpacts, x => x.Material == "SCHRAUBE");
        Assert.Equal(2, schraube.DirectParents);
        Assert.Equal(4, schraube.Ancestors);
        Assert.Equal(3, schraube.TopProducts);
        Assert.Equal(2, schraube.Depth);
        Assert.Equal(3, result.TopProducts);
        Assert.Equal(1, result.Intermediates);
        Assert.Equal(2, result.MaxDepth);
        Assert.Equal("SCHRAUBE", result.ComponentImpacts[0].Material);
    }

    [Fact]
    public void Risiko_Erbt_Nach_Oben()
    {
        var result = Build(("SCHRAUBE", "100", "Lieferant X"), ("GEHAEUSE", "200", "Y"), ("GEHAEUSE", "300", "Z"));

        Assert.True(Assert.Single(result.ComponentImpacts, x => x.Material == "SCHRAUBE").SingleSource);
        Assert.False(Assert.Single(result.ComponentImpacts, x => x.Material == "GEHAEUSE").SingleSource);
        var dichtung = Assert.Single(result.ComponentImpacts, x => x.Material == "DICHTUNG");
        Assert.True(dichtung.NotPurchased);
        Assert.Equal(1, dichtung.OverdueItems);

        var sensorA = Assert.Single(result.ProductRisks, x => x.Material == "SENSOR-A");
        Assert.Equal("Sensor A", sensorA.Text);
        Assert.Equal(3, sensorA.ComponentsBelow);
        Assert.Equal(1, sensorA.SingleSourceComponents);
        Assert.Equal(1, sensorA.OverdueComponents);
        Assert.Equal(2, sensorA.Levels);
    }

    [Fact]
    public void Kreis_In_Den_Daten_Fuehrt_Nicht_Zur_Endlosschleife()
    {
        var result = BomInheritanceService.Build([("A", "B"), ("B", "A"), ("A", "C")], [], [], new Dictionary<string, string>(), null);

        Assert.Equal(2, result.Components);
        Assert.Single(result.ProductRisks, x => x.Material == "C");
    }

    [Fact]
    public void Fuehrende_Nullen_Werden_Entfernt()
        => Assert.Equal("36385", BomInheritanceService.Normalize(" 000000000000036385 "));
}
