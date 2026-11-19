using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Entscheid Ingo 2026-11-19: Materialtext Trafag AG zuerst aus MAKTSet (Deutsch) im Mapping-Vorschlagslauf, ohne
/// SAP-Aenderung. Nur Testdaten, kein Aufruf gegen SAP.
/// </summary>
public class MaterialTextRefreshTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public MaterialTextRefreshTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var db = new AppDbContext(_options);
        db.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task MAKTSet_Parser_nimmt_nur_deutsche_Texte_der_gesuchten_Materialien()
    {
        const string json = """
        { "d": { "results": [
          { "Matnr": "000000000000053513", "Spras": "E", "Maktx": "Thermostat EN" },
          { "Matnr": "000000000000053513", "Spras": "D", "Maktx": "Thermostat 8DT80" },
          { "Matnr": "44073", "Spras": "DE", "Maktx": "Druckschalter PSL" },
          { "Matnr": "44073", "Spras": "D", "Maktx": "zweiter Text" },
          { "Matnr": "77777", "Spras": "D", "Maktx": "nicht im Werkstamm" },
          { "Matnr": "12345", "Spras": "D", "Maktx": "" }
        ] } }
        """;

        var texts = await SapGatewayMaterialTextReader.ParseGermanTextsAsync(
            new MemoryStream(Encoding.UTF8.GetBytes(json)), new HashSet<string>(["53513", "44073", "12345"]));

        Assert.Equal(2, texts.Count);
        Assert.Equal("Thermostat 8DT80", texts["53513"]);
        Assert.Equal("Druckschalter PSL", texts["44073"]);
    }

    [Fact]
    public void MAKTSet_URL_mit_Filter_Deutsch_und_ohne_Filter_als_Rueckfall()
    {
        var filtered = SapGatewayMaterialTextReader.BuildUrl("https://sap/srv/", withFilter: true);
        var plain = SapGatewayMaterialTextReader.BuildUrl("https://sap/srv", withFilter: false);

        Assert.StartsWith("https://sap/srv/MAKTSet?$format=json&$select=Matnr%2CSpras%2CMaktx", filtered);
        Assert.Contains("$filter=Spras%20eq%20%27D%27", filtered);
        Assert.EndsWith("&sap-language=DE", filtered);
        Assert.DoesNotContain("$filter", plain);
        Assert.EndsWith("&sap-language=DE", plain);
    }

    [Fact]
    public void ApplyTexts_setzt_Texte_und_laesst_Materialien_ohne_neuen_Text_unveraendert()
    {
        var masters = new[]
        {
            new GroupMaterialMaster { MaterialKey = "53513", MaterialText = "" },
            new GroupMaterialMaster { MaterialKey = "44073", MaterialText = "alter Text" }
        };

        var count = MaterialMappingSuggestionService.ApplyTexts(masters, new Dictionary<string, string> { ["53513"] = "Thermostat" });

        Assert.Equal(1, count);
        Assert.Equal("Thermostat", masters[0].MaterialText);
        Assert.Equal("alter Text", masters[1].MaterialText);
    }

    private async Task SeedAsync(bool withGatewaySite = true)
    {
        await using var db = new AppDbContext(_options);
        db.GroupMaterialMasters.Add(new GroupMaterialMaster { MaterialKey = "53513", Plant = "1100", RefreshedAtUtc = DateTime.UtcNow });
        db.SourceSystemDefinitions.Add(new SourceSystemDefinition
        {
            Code = "ZSCHWEIZ", DisplayName = "SAP", ConnectionKind = SourceSystemConnectionKinds.SapGateway,
            CentralServiceUrl = "https://sap.example/sap/opu/odata/sap/TEST_SRV", CentralUsername = "user", CentralPassword = "secret"
        });
        if (withGatewaySite)
            db.Sites.Add(new Site { TSC = "TRCH", Land = "Schweiz", Schema = "x", SourceSystem = "ZSCHWEIZ", IsActive = true });
        await db.SaveChangesAsync();
    }

    private MaterialMappingSuggestionService Service(ISapGatewayMaterialTextReader reader)
        => new(new Factory(_options), new EmptyProvider(), reader);

    [Fact]
    public async Task Vorschlagslauf_schreibt_MAKTSet_Texte_in_den_Werkstamm()
    {
        await SeedAsync();
        var reader = new FakeReader(new Dictionary<string, string> { ["53513"] = "Thermostat 8DT80" });

        var result = await Service(reader).RunAsync();

        Assert.Null(result.TextRefreshWarning);
        Assert.Equal(1, result.TextsLoaded);
        Assert.Equal(1, result.TrAgTexts);
        Assert.Equal("https://sap.example/sap/opu/odata/sap/TEST_SRV", reader.ServiceUrl);
        await using var db = new AppDbContext(_options);
        Assert.Equal("Thermostat 8DT80", (await db.GroupMaterialMasters.SingleAsync()).MaterialText);
    }

    [Fact]
    public async Task Vorschlagslauf_meldet_Ausfall_von_MAKTSet_und_laeuft_weiter()
    {
        await SeedAsync();

        var result = await Service(new FakeReader(null)).RunAsync();

        Assert.NotNull(result.TextRefreshWarning);
        Assert.Contains("MAKTSet", result.TextRefreshWarning);
        Assert.Equal(0, result.TextsLoaded);
    }

    [Fact]
    public async Task Vorschlagslauf_meldet_fehlende_Gateway_Site()
    {
        await SeedAsync(withGatewaySite: false);

        var result = await Service(new FakeReader(new Dictionary<string, string>())).RunAsync();

        Assert.Contains("keine aktive Site", result.TextRefreshWarning);
    }

    private sealed class FakeReader(IReadOnlyDictionary<string, string>? texts) : ISapGatewayMaterialTextReader
    {
        public string? ServiceUrl { get; private set; }

        public Task<IReadOnlyDictionary<string, string>> GetGermanTextsAsync(
            string serviceUrl, string username, string password, IReadOnlySet<string> wantedKeys, CancellationToken cancellationToken = default)
        {
            ServiceUrl = serviceUrl;
            if (texts is null)
                throw new HttpRequestException("SAP OData MAKTSet fehlgeschlagen (500 Internal Server Error).");
            return Task.FromResult(texts);
        }
    }

    private sealed class EmptyProvider : ICentralSalesDataProvider
    {
        public Task<List<SalesRecord>> GetRecordsAsync() => Task.FromResult(new List<SalesRecord>());
        public Task<List<SalesRecord>> GetLatestRecordsBySiteAsync() => Task.FromResult(new List<SalesRecord>());
        public Task<bool> UsesAuditCsvAsync() => Task.FromResult(false);
    }

    private sealed class Factory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
    }
}
