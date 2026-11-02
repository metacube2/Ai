using Microsoft.Data.Sqlite;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Materialanteil je Produktgruppe (Spend-Aufriss Phase 2, Entscheide Ingo 2026-11-02): Absagegrund, Disponenten 001 bis 006,
/// Termin bestaetigt je Monat, Schreiben nur bei vollstaendigem Lesen, drei Stufen. SAP wird durch einen Delegaten ersetzt.
/// </summary>
public class PurchasingMaterialShareTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public PurchasingMaterialShareTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public void Dispose() => _connection.Dispose();

    private long Scalar(string sql)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(command.ExecuteScalar());
    }

    private static Dictionary<string, object?> Sales(string matnr, string edatu, string bmeng, string netwr, string gruppe, string abgru = "")
        => new(StringComparer.OrdinalIgnoreCase)
        {
            ["Matnr"] = matnr, ["Edatu"] = edatu, ["Bmeng"] = bmeng, ["Netwr1"] = netwr,
            ["Dispo"] = "D10", ["DispoDes"] = gruppe, ["Abgru"] = abgru
        };

    private static Dictionary<string, object?> Bom(string vknr, string idnrk, string menge, string stk, string matkl, string dispo = "001")
        => new(StringComparer.OrdinalIgnoreCase)
        {
            ["Vknr"] = vknr, ["Idnrk"] = idnrk, ["Menge"] = menge, ["Meins"] = "ST", ["Stk"] = stk,
            ["Wert"] = (decimal.Parse(menge, System.Globalization.CultureInfo.InvariantCulture) * decimal.Parse(stk, System.Globalization.CultureInfo.InvariantCulture)).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["Dispo"] = dispo, ["Matkl"] = matkl, ["Maktx"] = "Teil " + idnrk
        };

    [Fact]
    public void Absagegrund_Faellt_Weg_Und_Einteilungen_Werden_Je_Monat_Summiert()
    {
        var result = PurchasingMaterialShareLoader.AggregateSales(
        [
            Sales("000000000000046123", "20261003", "10", "1000", "FP_DICHTEWÄCH"),
            Sales("46123", "/Date(1760313600000)/", "5", "500", "FP_DICHTEWÄCH"),          // 13.10.2025 als Gateway-Datum
            Sales("46123", "20261015", "7", "700", "FP_DICHTEWÄCH", abgru: "Z1"),        // abgesagt: zaehlt nicht
            Sales("46123", "20261120", "2", "200", "FP_DICHTEWÄCH")
        ]);

        Assert.Equal(3, result.Count);
        var oktober = result[new PurchasingMaterialShareLoader.SalesKey("46123", "202610", "FP_DICHTEWÄCH")];
        Assert.Equal(10m, oktober.Bmeng);
        Assert.Equal(1000m, oktober.Netwr);
        Assert.Contains(result.Keys, k => k.Monat == "202510");
    }

    [Fact]
    public void Monatsfenster_Gehen_Vom_Januar_Des_Vorjahres_Bis_Dezember()
    {
        var windows = PurchasingMaterialShareLoader.MonthWindows(new DateOnly(2026, 11, 2));
        Assert.Equal(24, windows.Count);
        Assert.Equal((new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31)), windows[0]);
        Assert.Equal((new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 31)), windows[^1]);
        Assert.Equal("Werk eq '1100' and Datvon eq '20260201' and Datbis eq '20260228' and Mata eq ''",
            PurchasingMaterialShareLoader.SalesFilter(new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28)));
    }

    [Fact]
    public void Stuecklistenfilter_Fragt_Disponenten_001_Bis_006()
    {
        var filter = PurchasingMaterialShareLoader.BomFilter(["46123", "46124"]);
        Assert.Equal("Werks eq '1100' and (Vknr eq '46123' or Vknr eq '46124') and (Dispo eq '001' or Dispo eq '002' or Dispo eq '003' or Dispo eq '004' or Dispo eq '005' or Dispo eq '006')", filter);
    }

    [Fact]
    public async Task Lauf_Schreibt_Verkauf_Und_Stueckliste_Und_Ersetzt_Den_Stand()
    {
        var calls = new List<string>();
        SapSingleRequestReader reader = (set, _, filter, _) =>
        {
            calls.Add(set);
            if (set == PurchasingMaterialShareLoader.SalesSet)
                return Task.FromResult(filter.Contains("20261001", StringComparison.Ordinal)
                    ? new List<Dictionary<string, object?>> { Sales("46123", "20261003", "10", "1000", "FP_DW") }
                    : new List<Dictionary<string, object?>>());
            return Task.FromResult(filter.Contains("'46123'", StringComparison.Ordinal)
                ? new List<Dictionary<string, object?>> { Bom("46123", "B63485", "2", "1.5", "10.08.00"), Bom("46123", "K50057", "1", "0.002", "20.01.00", "006") }
                : new List<Dictionary<string, object?>>());
        };

        var result = await PurchasingMaterialShareLoader.RunAsync(_connection, reader, new DateOnly(2026, 11, 2), () => new DateTime(2026, 11, 2, 5, 0, 0, DateTimeKind.Utc), CancellationToken.None);

        Assert.True(result.Written);
        Assert.Equal(PurchasingMaterialShareLoader.BomSet, calls[0]);                     // Probe zuerst
        Assert.Equal(24, calls.Count(c => c == PurchasingMaterialShareLoader.SalesSet));
        Assert.Equal(1, Scalar("SELECT COUNT(*) FROM PurchasingMaterialShareSales"));
        Assert.Equal(2, Scalar("SELECT COUNT(*) FROM PurchasingMaterialShareBom"));

        var rows = await PurchasingMaterialShareQuery.LoadAsync(_connection, "202610", "202610", CancellationToken.None);
        var gruppe = Assert.Single(rows);
        Assert.Equal("FP_DW", gruppe.Label);
        Assert.Equal(1000m, gruppe.Revenue);
        Assert.Equal(30.02m, gruppe.Value);                                                 // 10 x (2 x 1.5) + 10 x (1 x 0.002)
        var leiterplatte = gruppe.Children.First();
        Assert.Equal(30m, leiterplatte.Value);
        var teil = Assert.Single(leiterplatte.Children);
        Assert.Equal(20m, teil.Quantity);
        Assert.Equal(1.5m, teil.UnitCost);
        Assert.Empty(await PurchasingMaterialShareQuery.LoadAsync(_connection, "202611", "202612", CancellationToken.None));
    }

    [Fact]
    public async Task Ohne_Set_In_SAP_Wird_Nichts_Gelesen_Und_Der_Stand_Bleibt()
    {
        await PurchasingMaterialShareLoader.EnsureSchemaAsync(_connection, CancellationToken.None);
        using (var command = _connection.CreateCommand())
        {
            command.CommandText = "INSERT INTO PurchasingMaterialShareSales (Vknr, Monat, Produktgruppe, Dispo, Bmeng, Netwr, LastLoadedAtUtc) VALUES ('1','202610','G','D',1,1,'x');";
            command.ExecuteNonQuery();
        }
        var calls = 0;
        SapSingleRequestReader reader = (_, _, _, _) =>
        {
            calls++;
            throw new HttpRequestException("SAP OData ZloTopDownSet fehlgeschlagen (404 Not Found)");
        };

        var result = await PurchasingMaterialShareLoader.RunAsync(_connection, reader, new DateOnly(2026, 11, 2), () => DateTime.UtcNow, CancellationToken.None);

        Assert.False(result.Written);
        Assert.Equal(1, calls);                                                             // keine ZC23-Runde
        Assert.Equal(1, Scalar("SELECT COUNT(*) FROM PurchasingMaterialShareSales"));
    }

    [Fact]
    public async Task Fehlender_Stuecklistenblock_Laesst_Den_Stand_Stehen()
    {
        var bomCalls = 0;
        SapSingleRequestReader reader = (set, _, _, _) =>
        {
            if (set == PurchasingMaterialShareLoader.SalesSet)
                return Task.FromResult(new List<Dictionary<string, object?>> { Sales("46123", "20261003", "10", "1000", "FP_DW") });
            bomCalls++;
            if (bomCalls == 1)
                return Task.FromResult(new List<Dictionary<string, object?>>());              // Probe ok
            throw new HttpRequestException("Zeitueberschreitung");
        };

        var result = await PurchasingMaterialShareLoader.RunAsync(_connection, reader, new DateOnly(2026, 11, 2), () => DateTime.UtcNow, CancellationToken.None);

        Assert.False(result.Written);
        Assert.Equal(1, result.FailedChunks);
    }

    [Fact]
    public void Ohne_Produktgruppe_Und_Monatsliste()
    {
        var rows = PurchasingMaterialShareQuery.Build([("1", "", 1m, 10m)], new Dictionary<string, List<PurchasingMaterialShareQuery.BomRow>>());
        Assert.Equal(PurchasingMaterialShareQuery.NoProductGroup, Assert.Single(rows).Label);
        Assert.Equal(["202511", "202512", "202601"], PurchasingMaterialShareQuery.Months("202511", "202601"));
    }
}
