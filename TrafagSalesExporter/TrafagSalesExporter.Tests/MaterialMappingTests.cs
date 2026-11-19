using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Mapping-Tabelle Artikelnummern (Entscheide Ingo 19.11.2026, E5/E6): Importregeln, Statuswechsel, Vorschlagslauf,
/// Mapping-Excel, Werkstamm-Text aus dem Gateway und Schema-Migration.
/// </summary>
public class MaterialMappingTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 19, 10, 0, 0, DateTimeKind.Utc);
    private static readonly IReadOnlySet<string> KnownTscs = new HashSet<string>(["TRUK", "TRDE", "TRIT"], StringComparer.OrdinalIgnoreCase);
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public MaterialMappingTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var db = new AppDbContext(_options);
        db.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    private AppDbContext Db() => new(_options);

    private static MaterialMappingImportRow Row(int number, string tsc, string local, string group, string status = "bestaetigt",
        string method = "Hand", string confidence = "", string reviewedBy = "")
        => new(number, tsc, local, "Lokaler Text", group, "Text AG", "", "", method, "", confidence, status, reviewedBy);

    // ----- Import -----

    [Fact]
    public async Task Import_speichert_gueltige_Zeilen_und_ueberspringt_leere()
    {
        await using var db = Db();
        var rows = new[]
        {
            Row(2, "TRUK", "UK-1", "53513"),
            new MaterialMappingImportRow(3, "", "", "", "", "", "", "", "", "", "", "", ""),
            Row(4, "truk", "000UK-2", "44073", status: "Vorschlag", method: "Text", confidence: "73")
        };

        var result = await MaterialMappingStore.ImportAsync(db, rows, KnownTscs, "koi", Now);

        Assert.True(result.Success);
        Assert.Equal((2, 0, 1), (result.Inserted, result.Updated, result.SkippedEmpty));
        var stored = await db.MaterialMappings.AsNoTracking().OrderBy(m => m.LocalMaterialKey).ToListAsync();
        Assert.Equal(["UK-1", "UK-2"], stored.Select(m => m.LocalMaterialKey).ToArray());
        Assert.Equal("koi", stored[0].ReviewedBy);
        Assert.Equal(MaterialMappingStatuses.Confirmed, stored[0].Status);
        Assert.Equal(100, stored[0].Confidence);
        Assert.Equal(("TRUK", 73, MaterialMappingStatuses.Proposed, MaterialMappingMethods.Text),
            (stored[1].Tsc, stored[1].Confidence, stored[1].Status, stored[1].Method));
        Assert.Null(stored[1].ReviewedAtUtc);
    }

    [Fact]
    public async Task Import_lehnt_doppelte_Schluessel_ab_und_speichert_nichts()
    {
        await using var db = Db();
        // 0UK-1 und UK-1 sind nach der Normalisierung derselbe Schluessel.
        var rows = new[] { Row(2, "TRUK", "UK-1", "53513"), Row(3, "TRUK", "0UK-1", "44073"), Row(4, "TRDE", "N10.0", "12345") };

        var result = await MaterialMappingStore.ImportAsync(db, rows, KnownTscs, "koi", Now);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, error => error.Contains("Doppelter Schluessel") && error.Contains("2, 3"));
        Assert.Empty(await db.MaterialMappings.ToListAsync());
    }

    [Fact]
    public async Task Import_lehnt_fremden_TSC_unbekannten_Status_fehlende_Nummer_und_Sicherheit_ab()
    {
        await using var db = Db();
        var rows = new[]
        {
            Row(2, "TRXX", "A", "1"),
            Row(3, "TRUK", "B", "1", status: "ok"),
            Row(4, "TRUK", "C", ""),
            Row(5, "TRUK", "D", "1", confidence: "120"),
            Row(6, "TRUK", "E", "1")
        };

        var result = await MaterialMappingStore.ImportAsync(db, rows, KnownTscs, "koi", Now);

        Assert.False(result.Success);
        Assert.Equal(4, result.Errors.Count);
        Assert.Contains(result.Errors, error => error.StartsWith("Zeile 2:") && error.Contains("TRXX"));
        Assert.Contains(result.Errors, error => error.StartsWith("Zeile 3:"));
        Assert.Contains(result.Errors, error => error.StartsWith("Zeile 4:"));
        Assert.Contains(result.Errors, error => error.StartsWith("Zeile 5:"));
        Assert.Empty(await db.MaterialMappings.ToListAsync());   // auch die gueltige Zeile 6 nicht
    }

    [Fact]
    public async Task Import_aktualisiert_bestehende_Zeile_desselben_Schluessels()
    {
        await using (var db = Db())
            await MaterialMappingStore.ImportAsync(db, [Row(2, "TRUK", "UK-1", "53513", status: "Vorschlag", method: "Text", confidence: "55")], KnownTscs, "koi", Now);
        await using (var db = Db())
        {
            var result = await MaterialMappingStore.ImportAsync(db, [Row(2, "TRUK", "UK-1", "53514", reviewedBy: "Andreas")], KnownTscs, "koi", Now.AddDays(1));
            Assert.Equal((0, 1), (result.Inserted, result.Updated));
        }

        await using var check = Db();
        var mapping = await check.MaterialMappings.SingleAsync();
        Assert.Equal(("53514", MaterialMappingStatuses.Confirmed, "Andreas"), (mapping.GroupMaterialNumber, mapping.Status, mapping.ReviewedBy));
        Assert.Equal(Now.AddDays(1), mapping.ReviewedAtUtc);
    }

    [Fact]
    public async Task Status_setzen_merkt_Pruefer_und_bestaetigt_nie_ohne_Nummer()
    {
        await using var db = Db();
        db.MaterialMappings.AddRange(
            new MaterialMapping { Tsc = "TRUK", LocalMaterialKey = "A", GroupMaterialNumber = "1", Method = "Text", Status = "Vorschlag", CreatedAtUtc = Now, UpdatedAtUtc = Now },
            new MaterialMapping { Tsc = "TRUK", LocalMaterialKey = "B", GroupMaterialNumber = "", Method = "Text", Status = "Vorschlag", CreatedAtUtc = Now, UpdatedAtUtc = Now });
        await db.SaveChangesAsync();
        var ids = await db.MaterialMappings.OrderBy(m => m.LocalMaterialKey).Select(m => m.Id).ToListAsync();

        Assert.True(await MaterialMappingStore.SetStatusAsync(db, ids[0], MaterialMappingStatuses.Confirmed, "koi", Now));
        await Assert.ThrowsAsync<InvalidOperationException>(() => MaterialMappingStore.SetStatusAsync(db, ids[1], MaterialMappingStatuses.Confirmed, "koi", Now));

        var confirmed = await db.MaterialMappings.AsNoTracking().SingleAsync(m => m.Id == ids[0]);
        Assert.Equal(("koi", (DateTime?)Now), (confirmed.ReviewedBy, confirmed.ReviewedAtUtc));
    }

    [Fact]
    public async Task Vorschlagslauf_ueberschreibt_keine_bestaetigten_abgelehnten_oder_Handzeilen()
    {
        await using var db = Db();
        db.MaterialMappings.AddRange(
            new MaterialMapping { Tsc = "TRUK", LocalMaterialKey = "A", GroupMaterialNumber = "1", Method = "Text", Status = MaterialMappingStatuses.Confirmed, CreatedAtUtc = Now, UpdatedAtUtc = Now },
            new MaterialMapping { Tsc = "TRUK", LocalMaterialKey = "B", GroupMaterialNumber = "1", Method = "Text", Status = MaterialMappingStatuses.Rejected, CreatedAtUtc = Now, UpdatedAtUtc = Now },
            new MaterialMapping { Tsc = "TRUK", LocalMaterialKey = "C", GroupMaterialNumber = "1", Method = "Text", Status = MaterialMappingStatuses.Proposed, CreatedAtUtc = Now, UpdatedAtUtc = Now });
        await db.SaveChangesAsync();
        MaterialMapping Suggestion(string key) => new() { Tsc = "TRUK", LocalMaterialKey = key, LocalMaterial = key, GroupMaterialNumber = "2", Method = "Text", Confidence = 80 };

        var (inserted, updated, protectedRows) = await MaterialMappingStore.UpsertSuggestionsAsync(
            db, [Suggestion("A"), Suggestion("B"), Suggestion("C"), Suggestion("D")], Now);

        Assert.Equal((1, 1, 2), (inserted, updated, protectedRows));
        var stored = await db.MaterialMappings.AsNoTracking().OrderBy(m => m.LocalMaterialKey).ToListAsync();
        Assert.Equal(["1", "1", "2", "2"], stored.Select(m => m.GroupMaterialNumber).ToArray());
    }

    // ----- Excel -----

    [Fact]
    public void Excel_Export_und_Import_sind_rundlauffaehig_und_haben_die_Plan_Spalten()
    {
        var mappings = new[]
        {
            new MaterialMapping { Tsc = "TRUK", LocalMaterial = "000123", LocalMaterialKey = "123", LocalText = "PT 8251", GroupMaterialNumber = "008896.10.10",
                GroupText = "Druck", Method = MaterialMappingMethods.Text, Confidence = 81, Status = MaterialMappingStatuses.Proposed }
        };
        var weights = new Dictionary<(string, string), MaterialMappingWeight> { [("TRUK", "123")] = new(7, 1234.5m, "GBP", "PT 8251") };

        var bytes = MaterialMappingExcel.CreateWorkbookBytes(mappings, weights);

        using (var workbook = new XLWorkbook(new MemoryStream(bytes)))
        {
            var ws = workbook.Worksheet(MaterialMappingExcel.SheetName);
            for (var i = 0; i < MaterialMappingExcel.Headers.Length; i++)
                Assert.Equal(MaterialMappingExcel.Headers[i], ws.Cell(1, i + 1).GetString());
            Assert.Equal("000123", ws.Cell(2, 2).GetString());          // fuehrende Nullen bleiben
            Assert.Equal("008896.10.10", ws.Cell(2, 4).GetString());
            Assert.Equal("nein", ws.Cell(2, 14).GetString());           // Textvorschlag wirkt nicht
            Assert.Equal(7, ws.Cell(2, 15).GetValue<int>());
            Assert.Equal(1234.5m, ws.Cell(2, 16).GetValue<decimal>());
        }

        var parsed = Assert.Single(MaterialMappingExcel.Parse(new MemoryStream(bytes)));
        Assert.Equal(("TRUK", "000123", "008896.10.10", "Vorschlag", "81"),
            (parsed.Tsc, parsed.LocalMaterial, parsed.GroupMaterialNumber, parsed.Status, parsed.Confidence));
    }

    [Fact]
    public void Excel_Import_ohne_Pflichtspalte_bricht_mit_Meldung_ab()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Mapping");
        ws.Cell(1, 1).Value = "TSC";
        ws.Cell(1, 2).Value = "Local Material";
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var error = Assert.Throws<InvalidOperationException>(() => MaterialMappingExcel.Parse(stream));
        Assert.Contains("Group Material Number", error.Message);
    }

    [Fact]
    public void Gewichte_zaehlen_Zeilen_und_Umsatz_je_TSC_und_Nummer()
    {
        var records = new[]
        {
            new SalesRecord { Tsc = "TRUK", Material = "0123", SalesPriceValue = 100m, CompanyCurrency = "GBP", Name = "A" },
            new SalesRecord { Tsc = "truk", Material = "123", SalesPriceValue = -20m, CompanyCurrency = "GBP" },
            new SalesRecord { Tsc = "TRDE", Material = "123", SalesPriceValue = 5m, CompanyCurrency = "EUR" }
        };

        var weights = MaterialMappingStore.ComputeWeights(records);

        Assert.Equal(new MaterialMappingWeight(2, 80m, "GBP", "A"), weights[("TRUK", "123")]);
        Assert.Equal(1, weights[("TRDE", "123")].Rows);
    }

    // ----- Vorschlagslauf -----

    private static readonly IReadOnlyDictionary<string, string> TrAg = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["8896.10.10"] = "Drucktransmitter NAT 8252.74 0-10 bar",
        ["53513"] = "Thermostat 8DT80 Kapillar 2m",
        ["44073"] = "Druckschalter PSL 30 Membran",
        ["12345"] = ""
    };

    private static MaterialMappingCandidate Candidate(string tsc, string material, string text)
        => new(tsc, material, text, "TR AG", 3, 500m, "EUR");

    [Fact]
    public void Regel_Trafag_Nummer_im_Text_ergibt_Sicherheit_100()
    {
        var result = Assert.Single(MaterialMappingSuggester.Suggest([Candidate("TRDE", "N10.0", "Sensor 008896.10.10 Ersatz")], TrAg, Now));

        Assert.Equal(("8896.10.10", MaterialMappingMethods.Rule, 100, MaterialMappingStatuses.Proposed),
            (result.GroupMaterialNumber, result.Method, result.Confidence, result.Status));
        Assert.True(MaterialMappingStore.IsEffective(result));   // Regel wirkt sofort
    }

    [Fact]
    public void Mehrere_Trafag_Nummern_im_Text_sind_keine_Regel()
    {
        var suggestions = MaterialMappingSuggester.Suggest(
            [Candidate("TRDE", "N11.0", "Thermostat 53513 oder 44073")], TrAg, Now);

        Assert.All(suggestions, suggestion => Assert.NotEqual(MaterialMappingMethods.Rule, suggestion.Method));
    }

    [Fact]
    public void Textaehnlichkeit_findet_den_aehnlichsten_Text_als_Vorschlag_und_ist_wiederholgenau()
    {
        var candidates = new[] { Candidate("TRUK", "UK-9", "Pressure transmitter NAT 8252.74 0-10 bar") };

        var first = Assert.Single(MaterialMappingSuggester.Suggest(candidates, TrAg, Now));
        var second = Assert.Single(MaterialMappingSuggester.Suggest(candidates, TrAg, Now));

        Assert.Equal("8896.10.10", first.GroupMaterialNumber);
        Assert.Equal(MaterialMappingMethods.Text, first.Method);
        Assert.InRange(first.Confidence, MaterialMappingSuggester.MinimumTextConfidence, 99);
        Assert.False(MaterialMappingStore.IsEffective(first));   // Text wirkt erst bestaetigt
        Assert.Equal((first.GroupMaterialNumber, first.Confidence), (second.GroupMaterialNumber, second.Confidence));
    }

    [Fact]
    public void Ohne_Aehnlichkeit_und_bei_gleicher_Nummer_kein_Vorschlag()
    {
        var suggestions = MaterialMappingSuggester.Suggest(
            [Candidate("TRUK", "UK-1", "Zertifikat Kalibrierung"), Candidate("TRUK", "53513", "Thermostat")], TrAg, Now);

        Assert.Empty(suggestions);
    }

    [Fact]
    public void Kandidaten_sind_die_Artikel_mit_nicht_gefundener_Nummer()
    {
        var records = new[]
        {
            new SalesRecord { Tsc = "TRUK", Material = "UK-1", Name = "Transmitter", SupplierName = "Trafag AG", StandardCost = 5m, Quantity = 1m, SalesPriceValue = 10m },
            new SalesRecord { Tsc = "TRUK", Material = "UK-1", Name = "Transmitter", SupplierName = "Trafag AG", StandardCost = 5m, Quantity = 1m, SalesPriceValue = 30m },
            new SalesRecord { Tsc = "TRUK", Material = "53513", Name = "Thermostat", SupplierName = "Trafag AG", StandardCost = 5m, Quantity = 1m, SalesPriceValue = 10m },
            new SalesRecord { Tsc = "TRUK", Material = "EXT-1", Name = "Kabel", SupplierName = "Lapp", StandardCost = 5m, Quantity = 1m, SalesPriceValue = 10m }
        };
        var costs = new Dictionary<(string, string), GroupStandardCost>
        {
            [("53513", "1100")] = new() { MaterialKey = "53513", ValuationArea = "1100", UnitCost = 3m, Currency = "CHF" }
        };

        var candidates = MaterialMappingSuggestionService.BuildCandidates(
            records, costs, new HashSet<string>(["53513"]), new ExportSettings(), new HashSet<string>(), MaterialMappingLookup.Empty);

        var candidate = Assert.Single(candidates);
        Assert.Equal(("TRUK", "UK-1", 2, 40m, "TR AG"), (candidate.Tsc, candidate.LocalMaterial, candidate.Rows, candidate.NetSales, candidate.DeliveringEntity));
    }

    // ----- Werkstamm-Text aus dem Gateway -----

    [Fact]
    public void Werkstamm_liest_den_Materialtext_wenn_die_Property_geliefert_wird()
    {
        const string json = """
        { "d": { "results": [
          { "Matnr": "000000000000053513", "Werks": "1100", "Maktx": "Thermostat 8DT80" },
          { "Matnr": "53513", "Werks": "1100", "Maktx": "" },
          { "Matnr": "44073", "Werks": "1100" },
          { "Matnr": "99999", "Werks": "1200", "Maktx": "anderes Werk" }
        ] } }
        """;

        var materials = SapGatewayPlantMaterialReader.ParseMaterials(json, "1100");

        Assert.Equal(2, materials.Count);
        Assert.Equal("Thermostat 8DT80", materials["53513"]);
        Assert.Equal(string.Empty, materials["44073"]);
        Assert.Equal(materials.Keys.OrderBy(k => k), SapGatewayPlantMaterialReader.ParseMaterialKeys(json, "1100").OrderBy(k => k));
    }

    [Fact]
    public void Werkstamm_URL_fragt_den_Text_nur_mit_fester_deutscher_Sprache_ab()
    {
        Assert.Equal("https://sap/x/MARCSet?$format=json&$select=Matnr%2CWerks",
            SapGatewayPlantMaterialReader.BuildUrl("https://sap/x/", withText: false));
        var withText = SapGatewayPlantMaterialReader.BuildUrl("https://sap/x/", withText: true);
        Assert.Contains("Matnr%2CWerks%2C" + SapGatewayPlantMaterialReader.MaterialTextProperty, withText);
        Assert.EndsWith("&sap-language=DE", withText);
    }

    // ----- Schema-Migration -----

    [Fact]
    public void Schema_Migration_ergaenzt_Text_Spalte_und_Mapping_Tabelle_in_alter_Datenbank()
    {
        using (var command = _connection.CreateCommand())
        {
            command.CommandText = """
                DROP TABLE MaterialMappings;
                DROP TABLE GroupMaterialMasters;
                CREATE TABLE GroupMaterialMasters (Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, MaterialKey TEXT NOT NULL, Plant TEXT NOT NULL, RefreshedAtUtc TEXT NOT NULL);
                INSERT INTO GroupMaterialMasters (MaterialKey, Plant, RefreshedAtUtc) VALUES ('53513', '1100', '2026-11-18');
                """;
            command.ExecuteNonQuery();
        }

        using (var db = Db())
            new DatabaseSchemaMaintenanceService().EnsureSchema(db);

        using var check = Db();
        var master = check.GroupMaterialMasters.AsNoTracking().Single();
        Assert.Equal(("53513", string.Empty), (master.MaterialKey, master.MaterialText));
        check.MaterialMappings.Add(new MaterialMapping { Tsc = "TRUK", LocalMaterialKey = "A", CreatedAtUtc = Now, UpdatedAtUtc = Now });
        check.SaveChanges();
        check.MaterialMappings.Add(new MaterialMapping { Tsc = "TRUK", LocalMaterialKey = "A", CreatedAtUtc = Now, UpdatedAtUtc = Now });
        Assert.Throws<DbUpdateException>(() => check.SaveChanges());   // eindeutiger Index (TSC, Nummer)
    }
}
