using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class MaterialUsageDataRefreshServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TestDbContextFactory _dbFactory;

    public MaterialUsageDataRefreshServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using (var db = new AppDbContext(options))
        {
            db.Database.EnsureCreated();
            new DatabaseSchemaMaintenanceService().EnsureSchema(db);
            db.SourceSystemDefinitions.Add(new SourceSystemDefinition
            {
                Code = "SAP",
                DisplayName = "SAP OData",
                ConnectionKind = SourceSystemConnectionKinds.SapGateway,
                IsActive = true,
                CentralServiceUrl = "http://travt762:8000/sap/opu/odata/sap/ZPOWERBI_EINKAUF_SRV/",
                CentralUsername = "user",
                CentralPassword = "pass"
            });
            db.Sites.Add(new Site
            {
                Schema = string.Empty,
                TSC = PurchasingDataSourcePageService.PurchasingTsc,
                Land = "Einkauf SAP",
                SourceSystem = "SAP",
                IsActive = false
            });
            db.SaveChanges();
        }

        _dbFactory = new TestDbContextFactory(options);
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task RunFullLoadAsync_Reports_Error_Without_Throwing_When_EntitySets_Missing()
    {
        // Solange die SEGW-Anlage fehlt (oder der Metadaten-Cache alt ist), findet die
        // dynamische Namensaufloesung kein passendes Set. Der Refresh darf dabei nicht
        // crashen, sondern muss fachlich klar melden, was gesucht wurde - analog zum
        // bestehenden Muster fuer FinanzJournalSet.
        var service = new MaterialUsageDataRefreshService(_dbFactory, new FakeSapGatewayService([]), new NoopAppEventLogService());

        var status = await service.RunFullLoadAsync();

        Assert.Equal("Error", status.Status);
        Assert.Contains("EntitySet", status.Message);
        Assert.Contains("LZCODE_USAGE", status.Message);
        Assert.Equal(0, status.UsageRows);
        Assert.Equal(0, status.ParentRows);
    }

    [Fact]
    public void ResolveEntitySetName_Findet_Segw_Strukturnamen_Und_Doku_Namen()
    {
        // SEGW hat die Sets nach den DDIC-Strukturen benannt (exakte Schreibweise je nach
        // Anlage unbekannt); die Doku schlug urspruenglich MaterialUsageSet vor. Der
        // normalisierte Vergleich muss beide Welten treffen und Fremd-Sets ignorieren.
        var segwSets = new List<string> { "EKKOSet", "maracalcSet", "ZSTR_LZCODE_USAGESet", "ZSTR_LZCODE_PARENTSet" };
        Assert.Equal("ZSTR_LZCODE_USAGESet",
            MaterialUsageDataRefreshService.ResolveEntitySetName(segwSets, "lzcodeusage", "materialusage"));
        Assert.Equal("ZSTR_LZCODE_PARENTSet",
            MaterialUsageDataRefreshService.ResolveEntitySetName(segwSets, "lzcodeparent", "materialparent"));

        var dokuSets = new List<string> { "MaterialUsageSet", "MaterialParentSet" };
        Assert.Equal("MaterialUsageSet",
            MaterialUsageDataRefreshService.ResolveEntitySetName(dokuSets, "lzcodeusage", "materialusage"));

        Assert.Null(MaterialUsageDataRefreshService.ResolveEntitySetName(
            new List<string> { "EKKOSet" }, "lzcodeusage", "materialusage"));
    }

    [Fact]
    public void BuildMaterialClauses_Ohne_Wert_Liefert_Genau_Eine_Catchall_Abfrage()
    {
        foreach (var input in new[] { (string?)null, "  " })
        {
            var clauses = MaterialUsageDataRefreshService.BuildMaterialClauses("Vknr", input);
            var single = Assert.Single(clauses);
            Assert.Equal("Vknr gt ''", single.Clause);
            // Leeres Token: der Catch-all darf nie als "Nummer ohne Treffer" gemeldet werden.
            Assert.Equal(string.Empty, single.Token);
        }
    }

    [Fact]
    public void BuildMaterialClauses_Liefert_Eine_Abfrage_Je_Nummer()
    {
        // Kernaenderung 2026-07-30: nicht mehr eine gemeinsame OR-Gruppe, sondern eine eigene
        // Bedingung je Nummer. Die OR-Gruppe lieferte bei Mehrfacheingabe reproduzierbar 0 Zeilen,
        // waehrend dieselben Nummern einzeln Treffer hatten (Sitzung mit Marco und Armin).
        var clauses = MaterialUsageDataRefreshService.BuildMaterialClauses("Vknr", "2217, C34882");

        Assert.Collection(clauses,
            // Numerische Nummern werden auf 18 Stellen gepaddet (Befund 2026-07-23: SAP speichert
            // MARA/ZPOWERBI_VC_TXT zero-padded, die Kurzform fand sonst keinen Treffer).
            first =>
            {
                Assert.Equal("2217", first.Token);
                Assert.Equal("Vknr eq '000000000000002217'", first.Clause);
            },
            // Alphanumerische Nummern (C34882) bleiben unveraendert.
            second =>
            {
                Assert.Equal("C34882", second.Token);
                Assert.Equal("Vknr eq 'C34882'", second.Clause);
            });
    }

    [Fact]
    public void BuildMaterialClauses_Bereich_Wird_Als_Ge_Le_Gebaut_Und_Gepaddet()
    {
        // Range-Syntax "35-40" (Ingo-Anforderung 2026-07-22): das SAP-Gateway-Framework fasst
        // "ge X and le Y" auf demselben Property beim Parsen von it_filter_select_options zu
        // einer klassischen Select-Options-Bereichszeile zusammen. Beide Grenzen numerisch -> padden.
        var single = Assert.Single(MaterialUsageDataRefreshService.BuildMaterialClauses("Kompnr", "35-40"));
        Assert.Equal("(Kompnr ge '000000000000000035' and Kompnr le '000000000000000040')", single.Clause);
        Assert.Equal("35-40", single.Token);
    }

    [Fact]
    public void BuildMaterialClauses_Mischt_Einzelwerte_Und_Bereiche()
    {
        var clauses = MaterialUsageDataRefreshService.BuildMaterialClauses("Vknr", "2217, 35-40, C34882");

        Assert.Equal(
            [
                "Vknr eq '000000000000002217'",
                "(Vknr ge '000000000000000035' and Vknr le '000000000000000040')",
                "Vknr eq 'C34882'"
            ],
            clauses.Select(c => c.Clause));
    }

    [Fact]
    public void BuildMaterialClauses_Escaped_Hochkomma_Im_Wert()
    {
        var single = Assert.Single(MaterialUsageDataRefreshService.BuildMaterialClauses("Vknr", "A'B"));
        Assert.Equal("Vknr eq 'A''B'", single.Clause);
    }

    [Fact]
    public void ParseMaterialTokens_Akzeptiert_Excel_Spalte_Mit_Zeilenumbruechen()
    {
        // Wunsch Marco 2026-07-30: eine Spalte aus Excel direkt einfuegen koennen, statt pro
        // Nummer manuell ein Komma zu setzen. Excel liefert beim Kopieren Zeilenumbrueche
        // (\r\n) bzw. Tabs zwischen Spalten.
        Assert.Equal(
            ["2217", "C34882", "D15019"],
            MaterialUsageDataRefreshService.ParseMaterialTokens("2217\r\nC34882\r\nD15019"));

        Assert.Equal(
            ["2217", "C34882"],
            MaterialUsageDataRefreshService.ParseMaterialTokens("2217\tC34882"));
    }

    [Fact]
    public void ParseMaterialTokens_Behandelt_Komma_Semikolon_Und_Leerzeichen_Gleichwertig()
    {
        var expected = new[] { "2217", "C34882", "D15019" };

        Assert.Equal(expected, MaterialUsageDataRefreshService.ParseMaterialTokens("2217,C34882,D15019"));
        Assert.Equal(expected, MaterialUsageDataRefreshService.ParseMaterialTokens("2217; C34882; D15019"));
        Assert.Equal(expected, MaterialUsageDataRefreshService.ParseMaterialTokens("2217 C34882 D15019"));
        // Gemischt und mit ueberzaehligen Trennzeichen - so sieht eine echte Copy-Paste-Eingabe aus.
        Assert.Equal(expected, MaterialUsageDataRefreshService.ParseMaterialTokens("  2217,,  C34882 ;\r\n D15019  "));
    }

    [Fact]
    public void ParseMaterialTokens_Entfernt_Duplikate()
    {
        // In der Sitzung genau so passiert ("Hast du mehrmals das gleiche kopiert?"). Jedes
        // Duplikat waere eine zusaetzliche SAP-Anfrage ohne neue Zeilen.
        Assert.Equal(
            ["2217", "C34882"],
            MaterialUsageDataRefreshService.ParseMaterialTokens("2217, C34882, 2217, c34882"));
    }

    [Fact]
    public void ParseMaterialTokens_Laesst_Bereichsschreibweise_Zusammen()
    {
        // "35-40" darf durch den neuen Whitespace-Split nicht zerfallen, sonst waere die
        // Bereichsabfrage kaputt.
        Assert.Equal(["35-40"], MaterialUsageDataRefreshService.ParseMaterialTokens("35-40"));
        Assert.Equal(["2217", "35-40"], MaterialUsageDataRefreshService.ParseMaterialTokens("2217\n35-40"));
    }

    [Fact]
    public void NormalizeMaterialToken_Paddet_Nur_Numerische_Kurzformen()
    {
        Assert.Equal("000000000000002217", MaterialUsageDataRefreshService.NormalizeMaterialToken("2217"));
        Assert.Equal("000000000000000035", MaterialUsageDataRefreshService.NormalizeMaterialToken("35"));
        // Alphanumerisch bleibt unveraendert (MARA speichert linksbuendig).
        Assert.Equal("D15019", MaterialUsageDataRefreshService.NormalizeMaterialToken("D15019"));
        Assert.Equal("C34882", MaterialUsageDataRefreshService.NormalizeMaterialToken("C34882"));
        // Bereits 18-stellig (oder laenger) bleibt unveraendert.
        Assert.Equal("000000000000002217", MaterialUsageDataRefreshService.NormalizeMaterialToken("000000000000002217"));
        // Leer bleibt leer.
        Assert.Equal("", MaterialUsageDataRefreshService.NormalizeMaterialToken(""));
    }

    [Fact]
    public void BuildRichtungValue_Ohne_IncludeDeleted_Liefert_Reinen_Wert()
    {
        Assert.Equal("TOPDOWN", MaterialUsageDataRefreshService.BuildRichtungValue(topDown: true, includeDeleted: false));
        Assert.Equal("BOTTOMUP", MaterialUsageDataRefreshService.BuildRichtungValue(topDown: false, includeDeleted: false));
    }

    [Fact]
    public void BuildRichtungValue_Mit_IncludeDeleted_Haengt_Ein_Zeichen_An()
    {
        // Loeschvorgemerkte Materialien einbeziehen (Wunsch Ingo 2026-07-22, Befund: alte
        // numerische Vknr wie "2217" wurden sonst durch den MARA-LVORM-Filter aus Schritt 1
        // ausgeblendet, obwohl die Verwendung in ZPOWERBI_VC_TXT noch vorhanden war).
        // NUR EIN ZEICHEN ("D"), nicht "ALLE": das EDM-Property Richtung ist CHAR10-typisiert,
        // "TOPDOWNALLE" (11 Zeichen) wurde vom Gateway-Framework mit HTTP 400 "violates facet
        // information 'maxlength=10'" abgelehnt, noch bevor der ABAP-Code lief (live verifiziert
        // 2026-07-22, zweiter Anlauf). "TOPDOWND"/"BOTTOMUPD" (8/9 Zeichen) passen sicher.
        Assert.Equal("TOPDOWND", MaterialUsageDataRefreshService.BuildRichtungValue(topDown: true, includeDeleted: true));
        Assert.Equal("BOTTOMUPD", MaterialUsageDataRefreshService.BuildRichtungValue(topDown: false, includeDeleted: true));
        Assert.True(MaterialUsageDataRefreshService.BuildRichtungValue(topDown: true, includeDeleted: true).Length <= 10);
        Assert.True(MaterialUsageDataRefreshService.BuildRichtungValue(topDown: false, includeDeleted: true).Length <= 10);
    }

    [Fact]
    public async Task GetStatusAsync_Returns_Empty_Before_Any_Load()
    {
        var service = new MaterialUsageDataRefreshService(_dbFactory, new FakeSapGatewayService([]), new NoopAppEventLogService());

        var status = await service.GetStatusAsync();

        Assert.Equal("Empty", status.Status);
    }

    private sealed class FakeSapGatewayService(List<string> entitySets) : ISapGatewayService
    {
        public Task TestConnectionAsync(string serviceUrl, string username, string password, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<List<string>> GetEntitySetsAsync(string serviceUrl, string username, string password, CancellationToken cancellationToken = default)
            => Task.FromResult(entitySets);

        public Task<List<string>> GetEntityFieldNamesAsync(string serviceUrl, string entitySet, string username, string password, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<string>());

        public Task<List<Dictionary<string, object?>>> GetEntityRowsAsync(
            string serviceUrl,
            string entitySet,
            string username,
            string password,
            string? filter = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new List<Dictionary<string, object?>>());
    }

    private sealed class NoopAppEventLogService : IAppEventLogService
    {
        public Task WriteAsync(string category, string message, string level = "Info", int? siteId = null, string? land = null, string? details = null)
            => Task.CompletedTask;

        public Task WriteDebugAsync(string category, string message, int? siteId = null, string? land = null, string? details = null)
            => Task.CompletedTask;
    }

    private sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;

        public TestDbContextFactory(DbContextOptions<AppDbContext> options) => _options = options;

        public AppDbContext CreateDbContext() => new(_options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new AppDbContext(_options));
    }
}
