using System.Text.Json.Nodes;
using Microsoft.Data.Sqlite;
using TrafagSalesExporter.Services.Shopfloor;

namespace TrafagSalesExporter.Tests;

/// <summary>Temporaere Shopfloor-Datenbank mit der echten Konfiguration (Services/Shopfloor/shopfloor_config.json).</summary>
internal sealed class ShopfloorTestEnv : IDisposable
{
    public static string ProjectRoot { get; } = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
    public static string ShopfloorDir { get; } = Path.Combine(ProjectRoot, "Services", "Shopfloor");

    public string Dir { get; } = Path.Combine(Path.GetTempPath(), "sf_test_" + Guid.NewGuid().ToString("N"));
    public ShopfloorStore Store { get; }
    public ShopfloorDb Db { get; }

    public ShopfloorTestEnv(bool withSeed = false)
    {
        Directory.CreateDirectory(Dir);
        var config = ShopfloorConfig.Load(Path.Combine(ShopfloorDir, "shopfloor_config.json"));
        Db = new ShopfloorDb(Path.Combine(Dir, "shopfloor.db"));
        Db.Initialize(withSeed ? Path.Combine(ShopfloorDir, "seed", "shopfloor.seed.db") : null);
        Store = new ShopfloorStore(Db, config, () => "https://test/shopfloor/index.html");
    }

    public T Write<T>(Func<SqliteConnection, T> work) => Db.Write(work);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(Dir, true); } catch (IOException) { }
    }

    public static JsonObject Obj(string json) => (JsonObject)JsonNode.Parse(json)!;
    public static JsonArray Arr(string json) => (JsonArray)JsonNode.Parse(json)!;
}
