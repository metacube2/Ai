using System.Text.Json;
using TrafagSalesExporter.Services.Shopfloor;

namespace TrafagSalesExporter.Tests;

public class ShopfloorSapCoPaTests
{
    private static List<JsonElement> Rows(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.EnumerateArray().Select(e => e.Clone()).ToList();
    }

    [Theory]
    [InlineData(2026, 10, 1, "Bukrs eq '1100' and Gjahr eq '2026' and Perde eq '009'")]   // Monatserster: Vormonat
    [InlineData(2026, 11, 10, "Bukrs eq '1100' and Gjahr eq '2026' and Perde eq '011'")]
    [InlineData(2027, 1, 1, "Bukrs eq '1100' and Gjahr eq '2026' and Perde eq '012'")]
    public void Filter_MonatDesVortags(int y, int m, int d, string erwartet)
        => Assert.Equal(erwartet, ShopfloorSapCoPa.Filter("1100", new DateOnly(y, m, d)));

    [Fact]
    public void Compute_September_Wie_Fabios_Kontrolle()
    {
        // P76 Periode 009.2026, Bukrs 1100, Netto je Satzart und Sparte (SE16 CE11000, 10.11.2026)
        var r = ShopfloorSapCoPa.Compute(new DateOnly(2026, 10, 1), Rows("""
            [
              {"Vrgar":"A","Wwpsp":"0001","Netto":"3728.31"},
              {"Vrgar":"A","Wwpsp":"0002","Netto":"601723.05"},
              {"Vrgar":"A","Wwpsp":"0003","Netto":"2768728.51"},
              {"Vrgar":"A","Wwpsp":"0004","Netto":"214738.99"},
              {"Vrgar":"A","Wwpsp":"0005","Netto":"2684433.17"},
              {"Vrgar":"A","Wwpsp":"0007","Netto":"70670.95"},
              {"Vrgar":"A","Wwpsp":"0008","Netto":"186892.96"},
              {"Vrgar":"A","Wwpsp":"0010","Netto":"282.00"},
              {"Vrgar":"F","Wwpsp":"0001","Netto":"4635.75"},
              {"Vrgar":"F","Wwpsp":"0002","Netto":"517191.48"},
              {"Vrgar":"F","Wwpsp":"0003","Netto":"3613791.49"},
              {"Vrgar":"F","Wwpsp":"0004","Netto":"109562.87"},
              {"Vrgar":"F","Wwpsp":"0005","Netto":"2940107.89"},
              {"Vrgar":"F","Wwpsp":"0007","Netto":"60098.31"},
              {"Vrgar":"F","Wwpsp":"0008","Netto":"632726.90"}
            ]
            """));
        var v = r["werte"]!["verkauf"]!;
        Assert.Equal(6531197.94m, (decimal)v["ae_aktuell"]!);
        Assert.Equal(7878114.69m, (decimal)v["umsatz_ist"]!);
        Assert.Equal(2684433.17m, (decimal)v["ae_tx"]!);
        Assert.Equal(2768728.51m, (decimal)v["ae_dw"]!);
        Assert.Equal(2940107.89m, (decimal)v["u_tx"]!);
        Assert.Equal(3613791.49m, (decimal)v["u_dw"]!);
        Assert.Equal("2026-09", (string)r["monat"]!);
    }

    [Theory]
    [InlineData("3", "0003")]
    [InlineData(" 0005 ", "0005")]
    [InlineData("000003", "000003")]
    public void Sparte_Normalisiert(string roh, string erwartet)
        => Assert.Equal(erwartet, ShopfloorSapCoPa.Sparte(roh));
}
