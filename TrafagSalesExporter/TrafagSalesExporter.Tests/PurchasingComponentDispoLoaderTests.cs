using Microsoft.Data.Sqlite;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Produktgruppe ueber ZLO03 (Punkt 4 Gespraech Armin 2026-10-08): Auswahl der faelligen Komponenten, Filter an SAP,
/// Schreiben auch bei 0 Treffern und Verhalten bei Lesefehlern. SAP wird durch einen Delegaten ersetzt.
/// </summary>
public class PurchasingComponentDispoLoaderTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc);
    private readonly SqliteConnection _connection;

    public PurchasingComponentDispoLoaderTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        Execute("CREATE TABLE PurchasingEkpoCache (Ebeln TEXT, Ebelp TEXT, Matnr TEXT);");
        Execute(DatabaseSchemaSql.GetPurchasingComponentDispoCacheCreateSql());
        Execute(DatabaseSchemaSql.GetPurchasingComponentDispoStateCreateSql());
    }

    public void Dispose() => _connection.Dispose();

    private void Execute(string sql)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private long Scalar(string sql)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(command.ExecuteScalar());
    }

    private static Dictionary<string, object?> Row(string vknr, string dispo)
        => new() { ["Richtung"] = "BOTTOMUP", ["Vknr"] = vknr, ["Kompnr"] = "x", ["VknrDispo"] = dispo };

    [Fact]
    public void Filter_Pads_Numeric_Material_And_Includes_Deleted_Headers()
    {
        Assert.Equal("Richtung eq 'BOTTOMUPD' and Kompnr eq '000000000000004711'", PurchasingComponentDispoLoader.BuildFilter("4711"));
        Assert.Equal("Richtung eq 'BOTTOMUPD' and Kompnr eq 'R85012'", PurchasingComponentDispoLoader.BuildFilter("R85012"));
    }

    [Fact]
    public void ParseRows_Normalizes_Keys_And_Drops_Duplicates_And_Rows_Without_Vknr()
    {
        var usages = PurchasingComponentDispoLoader.ParseRows("000000000000004711",
            [Row("000000000000002217", ""), Row("2217", "019"), Row("", "001"), Row("V9", "el1")]);

        Assert.Equal(2, usages.Count);
        Assert.All(usages, usage => Assert.Equal("4711", usage.Kompnr));
        Assert.Equal("019", Assert.Single(usages, usage => usage.Vknr == "2217").VknrDispo);   // Zeile mit Disponent gewinnt
        Assert.Equal("EL1", Assert.Single(usages, usage => usage.Vknr == "V9").VknrDispo);
    }

    [Fact]
    public async Task RunAsync_Asks_Each_Purchased_Material_Once_And_Records_Zero_Hits()
    {
        Execute("INSERT INTO PurchasingEkpoCache VALUES ('1','10','000000000000004711'), ('1','20','4711'), ('2','10','R1'), ('3','10',''), ('4','10','R2');");
        var asked = new List<string>();
        SapEntitySetReader reader = (set, select, filter, orderBy, token) =>
        {
            Assert.Equal(PurchasingComponentDispoLoader.UsageSet, set);
            lock (asked) asked.Add(filter);
            return Task.FromResult(filter.Contains("4711")
                ? new List<Dictionary<string, object?>> { Row("V1", "001"), Row("V2", "002") }
                : new List<Dictionary<string, object?>>());
        };

        var result = await PurchasingComponentDispoLoader.RunAsync(_connection, reader, TimeSpan.FromMinutes(1), 2, () => Now, CancellationToken.None);

        Assert.Equal(3, asked.Count);                      // 4711 nur einmal, leere Nummer gar nicht
        Assert.Equal(3, result.Checked);
        Assert.Equal(1, result.WithUsage);
        Assert.Equal(0, result.Remaining);
        Assert.Equal(2, Scalar("SELECT COUNT(*) FROM PurchasingComponentDispoCache WHERE Kompnr = '4711';"));
        Assert.Equal(3, Scalar("SELECT COUNT(*) FROM PurchasingComponentDispoState;"));

        // Innerhalb der Wiederholfrist wird nichts erneut gefragt, danach wieder.
        asked.Clear();
        await PurchasingComponentDispoLoader.RunAsync(_connection, reader, TimeSpan.FromMinutes(1), 2, () => Now.AddDays(1), CancellationToken.None);
        Assert.Empty(asked);
        await PurchasingComponentDispoLoader.RunAsync(_connection, reader, TimeSpan.FromMinutes(1), 2, () => Now.AddDays(8), CancellationToken.None);
        Assert.Equal(3, asked.Count);
    }

    [Fact]
    public async Task RunAsync_Keeps_Old_Usage_When_Sap_Fails_And_Stops_After_A_Failed_Block()
    {
        Execute("INSERT INTO PurchasingEkpoCache VALUES ('1','10','R1');");
        Execute("INSERT INTO PurchasingComponentDispoCache VALUES ('R1','V1','001','2026-09-01');");
        SapEntitySetReader reader = (set, select, filter, orderBy, token)
            => throw new HttpRequestException("SAP OData ZSTR_LZCODE_USAGESet fehlgeschlagen (404 Not Found)");

        var result = await PurchasingComponentDispoLoader.RunAsync(_connection, reader, TimeSpan.FromMinutes(1), 2, () => Now, CancellationToken.None);

        Assert.Equal(0, result.Checked);
        Assert.Equal(1, result.Failed);
        Assert.Equal(1, result.Remaining);
        Assert.Contains("404", result.Warning);
        Assert.Equal(1, Scalar("SELECT COUNT(*) FROM PurchasingComponentDispoCache;"));
        Assert.Equal(0, Scalar("SELECT COUNT(*) FROM PurchasingComponentDispoState;"));
    }

    [Fact]
    public async Task RunAsync_Stops_At_The_Time_Budget_And_Leaves_The_Rest_For_The_Next_Run()
    {
        for (var i = 0; i < 200; i++)
            Execute($"INSERT INTO PurchasingEkpoCache VALUES ('1','{i}','R{i:000}');");
        var clock = Now;
        SapEntitySetReader reader = (set, select, filter, orderBy, token) =>
        {
            lock (this) clock = clock.AddSeconds(1);
            return Task.FromResult(new List<Dictionary<string, object?>>());
        };

        var result = await PurchasingComponentDispoLoader.RunAsync(_connection, reader, TimeSpan.FromSeconds(30), 1, () => clock, CancellationToken.None);

        // Das Budget wird vor jedem Block geprueft (25 Anfragen je paralleler Anfrage): nach dem ersten Block sind
        // 25 s verbraucht, der zweite laeuft noch, danach ist Schluss.
        Assert.Equal(50, result.Checked);
        Assert.Equal(150, result.Remaining);
    }
}
