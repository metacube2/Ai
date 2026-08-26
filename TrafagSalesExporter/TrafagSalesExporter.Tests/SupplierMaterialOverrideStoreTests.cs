using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Sichert die uebergangsweise Lieferantenzuordnung ab. Sie ist die Antwort auf die Rueckmeldung
/// von Trafag Italia vom 2026-08-26: die 941 fehlenden Lieferanten sind fachlich geklaert, aber
/// noch nicht im B1-Artikelstamm gepflegt.
///
/// Die beiden Regeln, an denen die Loesung steht oder faellt, sind hier hart abgesichert:
/// es wird nie ein vorhandener Wert ueberschrieben, und eine Zuordnung wirkt nie ueber den
/// Standort hinaus.
/// </summary>
public class SupplierMaterialOverrideStoreTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public SupplierMaterialOverrideStoreTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var command = _connection.CreateCommand();
        command.CommandText = DatabaseSchemaSql.GetSupplierMaterialOverridesCreateSql();
        command.ExecuteNonQuery();
    }

    public void Dispose() => _connection.Dispose();

    private AppDbContext CreateDb() => new(_options);

    private static SalesRecord Row(string material, string tsc = "TRIT") => new()
    {
        Tsc = tsc,
        Material = material,
        SupplierNumber = string.Empty,
        SupplierName = string.Empty,
        SupplierCountry = string.Empty
    };

    private const string Csv = """
        Tsc;Material;SupplierNumber;SupplierName;SupplierCountry
        TRIT;53424;S_CH01_0065180;Trafag AG;CH
        TRIT;GC11901;S_IT01_0000042;ITEC S.R.L.;IT
        """;

    [Fact]
    public void Parse_Liest_Kopfzeile_Weg_Und_Normalisiert_Den_Schluessel()
    {
        var parsed = SupplierMaterialOverrideStore.Parse(Csv, DateTime.UtcNow);

        Assert.Equal(2, parsed.Count);
        Assert.Equal("53424", parsed[0].MaterialKey);
        Assert.Equal("Trafag AG", parsed[0].SupplierName);
        Assert.Equal("CH", parsed[0].SupplierCountry);
        Assert.Contains("Paola Castagna", parsed[0].Source);
    }

    [Fact]
    public void Parse_Verwirft_Zeilen_Ohne_Material_Oder_Ohne_Lieferantennummer()
    {
        // Die Rueckmeldung aus Italien enthaelt eine Bonusgutschrift ohne Artikelnummer
        // ("PREMIO PER RAGGIUNGIMENTO FATTURATO"). Sie kann im Artikelstamm nicht existieren
        // und darf deshalb auch keine Zuordnung erzeugen.
        var csv = """
            Tsc;Material;SupplierNumber;SupplierName;SupplierCountry
            TRIT;;S_CH01_0070540;Trafag Italia S.r.l.;IT
            TRIT;53424;;Trafag AG;CH
            TRIT;53459;S_CH01_0065180;Trafag AG;CH
            """;

        var parsed = SupplierMaterialOverrideStore.Parse(csv, DateTime.UtcNow);

        Assert.Single(parsed);
        Assert.Equal("53459", parsed[0].MaterialKey);
    }

    [Fact]
    public void Parse_Nimmt_Bei_Doppeltem_Material_Den_Ersten_Treffer()
    {
        var csv = """
            Tsc;Material;SupplierNumber;SupplierName;SupplierCountry
            TRIT;53424;S_CH01_0065180;Trafag AG;CH
            TRIT;53424;S_IT01_0000042;ITEC S.R.L.;IT
            """;

        var parsed = SupplierMaterialOverrideStore.Parse(csv, DateTime.UtcNow);

        Assert.Single(parsed);
        Assert.Equal("Trafag AG", parsed[0].SupplierName);
    }

    [Fact]
    public async Task ApplyAsync_Fuellt_Nur_Zeilen_Mit_Drei_Leeren_Feldern()
    {
        using var db = CreateDb();
        await SupplierMaterialOverrideStore.SeedAsync(db, SupplierMaterialOverrideStore.Parse(Csv, DateTime.UtcNow));

        var leer = Row("53424");
        var gepflegt = Row("53424");
        gepflegt.SupplierNumber = "S_IT01_0000999";
        gepflegt.SupplierName = "ECHTER LIEFERANT AUS B1";
        gepflegt.SupplierCountry = "IT";

        var result = await SupplierMaterialOverrideStore.ApplyAsync(db, "TRIT", new[] { leer, gepflegt });

        Assert.Equal(1, result.RowsFilled);
        Assert.Equal("Trafag AG", leer.SupplierName);
        // Der gepflegte Wert bleibt unangetastet: sobald Italien den Artikelstamm nachzieht,
        // gewinnt wieder die Quelle und die Uebergangsliste wird wirkungslos.
        Assert.Equal("ECHTER LIEFERANT AUS B1", gepflegt.SupplierName);
    }

    [Fact]
    public async Task ApplyAsync_Wirkt_Nicht_Auf_Einen_Anderen_Standort()
    {
        using var db = CreateDb();
        await SupplierMaterialOverrideStore.SeedAsync(db, SupplierMaterialOverrideStore.Parse(Csv, DateTime.UtcNow));

        var fremd = Row("53424", "TRIN");

        var result = await SupplierMaterialOverrideStore.ApplyAsync(db, "TRIN", new[] { fremd });

        Assert.Equal(0, result.RowsFilled);
        Assert.Equal(string.Empty, fremd.SupplierName);
    }

    [Fact]
    public async Task ApplyAsync_Zaehlt_Die_Weiterhin_Offenen_Zeilen()
    {
        using var db = CreateDb();
        await SupplierMaterialOverrideStore.SeedAsync(db, SupplierMaterialOverrideStore.Parse(Csv, DateTime.UtcNow));

        var bekannt = Row("53424");
        var unbekannt = Row("99999");

        var result = await SupplierMaterialOverrideStore.ApplyAsync(db, "TRIT", new[] { bekannt, unbekannt });

        Assert.Equal(1, result.RowsFilled);
        Assert.Equal(1, result.MaterialsUsed);
        Assert.Equal(1, result.RowsLeftEmpty);
    }

    [Fact]
    public async Task SeedAsync_Schreibt_Beim_Zweiten_Lauf_Nicht_Erneut()
    {
        var desired = SupplierMaterialOverrideStore.Parse(Csv, DateTime.UtcNow);

        using (var db = CreateDb())
        {
            var first = await SupplierMaterialOverrideStore.SeedAsync(db, desired);
            Assert.True(first.Changed);
            Assert.Equal(2, first.RowCount);
        }

        using (var db = CreateDb())
        {
            var second = await SupplierMaterialOverrideStore.SeedAsync(db, desired);
            Assert.False(second.Changed);
        }
    }

    [Fact]
    public async Task SeedAsync_Ersetzt_Den_Bestand_Wenn_Sich_Ein_Lieferant_Aendert()
    {
        using (var db = CreateDb())
            await SupplierMaterialOverrideStore.SeedAsync(db, SupplierMaterialOverrideStore.Parse(Csv, DateTime.UtcNow));

        var geaendert = SupplierMaterialOverrideStore.Parse(
            Csv.Replace("Trafag AG", "Trafag Italia S.r.l."), DateTime.UtcNow);

        using (var db = CreateDb())
        {
            var result = await SupplierMaterialOverrideStore.SeedAsync(db, geaendert);
            Assert.True(result.Changed);
        }

        using (var db = CreateDb())
        {
            var row = Row("53424");
            await SupplierMaterialOverrideStore.ApplyAsync(db, "TRIT", new[] { row });
            Assert.Equal("Trafag Italia S.r.l.", row.SupplierName);
        }
    }

    [Fact]
    public void LoadEmbedded_Liefert_Die_Vollstaendige_Liste_Aus_Italien()
    {
        var loaded = SupplierMaterialOverrideStore.LoadEmbedded(DateTime.UtcNow);

        // 942 Zeilen in der Rueckmeldung, davon eine Bonusgutschrift ohne Artikelnummer.
        Assert.Equal(941, loaded.Count);
        Assert.All(loaded, o => Assert.Equal("TRIT", o.Tsc));
        Assert.All(loaded, o => Assert.False(string.IsNullOrWhiteSpace(o.SupplierCountry)));
        Assert.Equal(449, loaded.Count(o => o.SupplierName == "Trafag AG"));
        Assert.Equal(103, loaded.Count(o => o.SupplierName == "Trafag Italia S.r.l."));
    }

    [Fact]
    public void LoadEmbedded_Klassifiziert_Die_Internen_Lieferanten_Wie_Erwartet()
    {
        var loaded = SupplierMaterialOverrideStore.LoadEmbedded(DateTime.UtcNow);

        // Der Zweck der ganzen Uebung: aus diesen Namen muss die bestehende Klassifikation die
        // liefernde Gesellschaft ableiten koennen, sonst bringt die Zuordnung keine Konzernkosten.
        Assert.Equal(
            GroupStandardCostEntities.TrAg,
            GroupMarginSupplierClassifier.ResolveDeliveringEntity("Trafag AG", "TRIT"));
        Assert.Equal(
            GroupStandardCostEntities.TrIt,
            GroupMarginSupplierClassifier.ResolveDeliveringEntity("Trafag Italia S.r.l.", "TRIT"));
        Assert.Null(
            GroupMarginSupplierClassifier.ResolveDeliveringEntity("ITEC S.R.L.", "TRIT"));

        var intern = loaded.Count(o =>
            GroupMarginSupplierClassifier.ResolveDeliveringEntity(o.SupplierName, "TRIT") is not null);
        Assert.Equal(552, intern);
    }
}
