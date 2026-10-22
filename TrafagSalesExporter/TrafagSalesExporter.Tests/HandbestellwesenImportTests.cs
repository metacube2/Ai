using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Modules.Handbestellwesen;

namespace TrafagSalesExporter.Tests;

// Altdaten-Import 2026-10-22: Dump der PHP-Anwendung (HeidiSQL-Format) -> Modul Handbestellwesen. Kuenstliche Daten.
public sealed class HandbestellwesenImportTests : IDisposable
{
    private const string Dump = """
        -- Exportiere Struktur von Tabelle trag_bestellwesen.archive
        INSERT INTO `archive` (`id`, `id_offenebestellungen`, `bestellnummer`, `kreditor`, `bestelltext`, `bestellwert`, `kurzzeichen`, `kostenstelle`, `sachkonto`, `bestellfreigabe`, `wareneingang`, `rechnungsbetrag`, `rechnungsdatum`, `speicherdatum`, `archivedatum`) VALUES
        	(1, '10', 'E268750001', '65017', 'Schrauben M4 \'rostfrei\'\nzweite Zeile', '120.50', 'abc,', '875', '67099', '000', 'Tuesday 06 January 2026 10:00:00', '---', '---', 'Monday 05 January 2026 09:00:00', 'Tuesday 06 January 2026 10:00:00'),
        	(2, '11', 'E268750002', 'Kreditor', 'Offerte', 'KV', 'abc,', '875', '67099', '000', 'Tuesday 06 January 2026 10:00:00', '---', '---', 'Monday 05 January 2026 09:05:00', 'Tuesday 06 January 2026 10:00:00');
        INSERT INTO `tab_offenebestellungen` (`id`, `bestellnummer`, `kreditor`, `bestelltext`, `bestellwert`, `kurzzeichen`, `kostenstelle`, `sachkonto`, `bestellfreigabe`, `wareneingang`, `rechnungsbetrag`, `rechnungsdatum`, `speicherdatum`) VALUES
        	(20, 'E268750003', '65017', 'Gross', '1\'500', 'abc,chf', '875', '67099', '000', '000', '0', '0', 'Wednesday 07 January 2026 08:00:00'),
        	(21, 'E268750001', '65017', 'Doublette', '1', 'abc,', '875', '67099', '000', '000', '0', '0', 'Wednesday 07 January 2026 08:00:00'),
        	(22, 'E26870005', '65017', 'Freigegeben', '900', 'abc,chf', '87', '67099', 'Thursday 08 January 2026 11:00:00', '000', '0', '0', 'Wednesday 07 January 2026 08:00:00');
        INSERT INTO `delete_archive` (`id`, `id_offenebestellungen`, `bestellnummer`, `kreditor`, `bestelltext`, `bestellwert`, `kurzzeichen`, `kostenstelle`, `sachkonto`, `bestellfreigabe`, `wareneingang`, `rechnungsbetrag`, `rechnungsdatum`, `speicherdatum`, `deletedatum`) VALUES
        	(30, '15', 'E268750004', '65017', 'Falsch', '10', 'abc,', '875', '67099', '000', '000', '0', '0', 'Monday 05 January 2026 09:10:00', 'Monday 05 January 2026 09:20:00');
        INSERT INTO `tab_bestellnummer` (`id`, `bestellnummer`, `nr`) VALUES
        	(1, 'E26875', 3),
        	(2, 'E2687', 2);
        INSERT INTO `tab_kreditorenstamm` (`id`, `kreditor`, `name`, `strasse`, `plz`, `ort`, `land`, `date`) VALUES
        	(1, '65017', 'Alt AG', 'Weg 1', '3000', 'Bern', 'CH', ''),
        	(2, '65017', 'M&uuml;ller AG', 'Weg 2', '3000', 'Bern', 'CH', '');
        INSERT INTO `kurzzeichen` (`id`, `kurzzeichen`, `vorname`, `nachname`, `bestellberechtiung`, `kostenstelle`, `mail`, `date`) VALUES
        	(1, 'ABC', 'Anna', 'Beispiel', 500, '875', 'a@example.com', ''),
        	(2, 'chf', 'Carl', 'Chef', 10000, '875', NULL, '');
        INSERT INTO `tab_kostenstelle` (`id`, `kostenstelle`, `bezeichnung`, `kurzzeichen`, `date`) VALUES
        	(1, '875', 'Entwicklung', 'chf', ''),
        	(2, '87', 'Klein', 'chf', '');
        INSERT INTO `tab_sachkonto` (`id`, `sachkonto`, `bezeichnung`, `date`) VALUES
        	(1, '67099', 'Diverses', '');
        """;

    private readonly string _pfad = Path.Combine(Path.GetTempPath(), $"hb_import_{Guid.NewGuid():N}.db");
    private readonly Factory _factory;
    private readonly HbImportService _import;

    public HandbestellwesenImportTests()
    {
        _factory = new Factory(HbRegistration.VerbindungFuer(_pfad));
        using var db = _factory.CreateDbContext();
        db.Database.EnsureCreated();
        _import = new HbImportService(_factory, TimeProvider.System);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        foreach (var f in new[] { _pfad, _pfad + "-wal", _pfad + "-shm" })
            if (File.Exists(f)) File.Delete(f);
    }

    private static readonly HbBenutzer Admin = new("admin", null, true);

    [Fact]
    public void Parser_Liest_Escapes_Null_Und_Spaltennamen()
    {
        var d = HbDumpParser.Parse(Dump);
        Assert.Equal("Schrauben M4 'rostfrei'\nzweite Zeile", d["archive"][0]["bestelltext"]);
        Assert.Equal("1'500", d["tab_offenebestellungen"][0]["bestellwert"]);
        Assert.Null(d["kurzzeichen"][1]["mail"]);
        Assert.Equal("500", d["kurzzeichen"][0]["bestellberechtiung"]);
        Assert.Equal(8, d.Count);
    }

    [Fact]
    public async Task Import_Uebernimmt_Status_Werte_Und_Zaehler()
    {
        var bericht = await _import.ImportierenAsync(Admin, Dump);

        Assert.True(bericht.Importiert);
        using var db = _factory.CreateDbContext();
        var b = db.Bestellungen.AsNoTracking().ToDictionary(x => x.Bestellnummer);
        Assert.Equal(5, b.Count); // Doublette E268750001 aus den offenen faellt weg, das Archiv gewinnt
        Assert.Equal(HbStatus.Eingegangen, b["E268750001"].Status);
        Assert.Equal(120.50m, b["E268750001"].Bestellwert);
        Assert.Equal(new DateTime(2026, 1, 5, 9, 0, 0), b["E268750001"].AngelegtAm);
        Assert.Equal(new DateTime(2026, 1, 6, 10, 0, 0), b["E268750001"].EingangAm);
        Assert.Null(b["E268750001"].Rechnungsbetrag);
        // "KV" bleibt als Text, Platzhalter "Kreditor" wird leer
        Assert.Equal("KV", b["E268750002"].WertText);
        Assert.Equal(0m, b["E268750002"].Bestellwert);
        Assert.Equal(string.Empty, b["E268750002"].KreditorNr);
        // offen ueber Limit mit Freigeber und ohne Freigabe -> Freigabe offen; 1'500 gelesen
        Assert.Equal(HbStatus.FreigabeOffen, b["E268750003"].Status);
        Assert.Equal(1500m, b["E268750003"].Bestellwert);
        Assert.Equal("chf", b["E268750003"].FreigeberKurzzeichen);
        // schon freigegeben -> offen, mit Freigabedatum
        Assert.Equal(HbStatus.Offen, b["E26870005"].Status);
        Assert.Equal(new DateTime(2026, 1, 8, 11, 0, 0), b["E26870005"].FreigegebenAm);
        Assert.Equal(HbStatus.Geloescht, b["E268750004"].Status);

        // Zaehler: nie kleiner als die hoechste uebernommene Nummer (87: Tabelle sagt 2, Bestellung hat 5)
        var z = db.Zaehler.AsNoTracking().ToDictionary(x => x.Kostenstelle);
        Assert.Equal(4, z["875"].LetzteNummer);
        Assert.Equal(5, z["87"].LetzteNummer);

        // Kreditor: letzter Stand, HTML-Entitaet aufgeloest; Mitarbeiter: Konto = Kurzzeichen klein
        Assert.Equal("Müller AG", db.Kreditoren.Single().Name);
        var abc = db.Mitarbeiter.Single(x => x.Kurzzeichen == "ABC");
        Assert.Equal(("abc", 500m), (abc.Konto, abc.Limit));
        Assert.Contains(bericht.Hinweise, h => h.Contains("ohne Kreditor"));
        Assert.Contains(bericht.Hinweise, h => h.Contains("doppelt"));
    }

    [Fact]
    public async Task Naechste_Nummer_Nach_Import_Kollidiert_Nicht()
    {
        await _import.ImportierenAsync(Admin, Dump);
        var hb = new HbService(_factory, new OptionsStub(new HbOptions()), new FesteZeit(new DateTimeOffset(2026, 10, 22, 9, 0, 0, TimeSpan.Zero)));
        var chf = await hb.BenutzerAsync("TRAFAG\\chf");
        var neu = await hb.AnlegenAsync(chf, new HbBestellEingabe { KreditorNr = "65017", Text = "neu", Bestellwert = 10m, Kostenstelle = "87", Sachkonto = "67099" });
        Assert.Equal("E26870006", neu.Bestellnummer);
    }

    [Fact]
    public async Task Import_Nur_In_Leere_Datenbank_Und_Nur_Admin()
    {
        await Assert.ThrowsAsync<HbException>(() => _import.ImportierenAsync(new HbBenutzer("abc", null, false), Dump));
        await _import.ImportierenAsync(Admin, Dump);
        await Assert.ThrowsAsync<HbException>(() => _import.ImportierenAsync(Admin, Dump));
    }

    [Fact]
    public async Task Pruefen_Schreibt_Nichts()
    {
        var bericht = await _import.PruefenAsync(Dump);
        Assert.False(bericht.Importiert);
        Assert.Equal(2, bericht.Eingegangen);
        Assert.Equal(1, bericht.Offen); // die Doublette E268750001 zaehlt nicht
        Assert.Equal(1, bericht.FreigabeOffen);
        using var db = _factory.CreateDbContext();
        Assert.Empty(db.Bestellungen);
    }

    [Fact]
    public async Task Spalte_WertText_Wird_In_Alter_Datenbank_Ergaenzt()
    {
        using var db = _factory.CreateDbContext();
        db.Database.ExecuteSqlRaw("CREATE TABLE Alt (x INTEGER); DROP TABLE Bestellung; CREATE TABLE Bestellung (Id INTEGER PRIMARY KEY);");
        await HbStartUndSicherung.ErgaenzeSpaltenAsync(db, CancellationToken.None);
        await HbStartUndSicherung.ErgaenzeSpaltenAsync(db, CancellationToken.None); // zweimal ohne Fehler
        db.Database.ExecuteSqlRaw("INSERT INTO Bestellung (Id, WertText) VALUES (1, 'KV');");
    }

    private sealed class Factory(string cs) : IDbContextFactory<HbDbContext>
    {
        public HbDbContext CreateDbContext() => new(new DbContextOptionsBuilder<HbDbContext>().UseSqlite(cs).Options);
    }

    private sealed class OptionsStub(HbOptions value) : Microsoft.Extensions.Options.IOptionsMonitor<HbOptions>
    {
        public HbOptions CurrentValue => value;
        public HbOptions Get(string? name) => value;
        public IDisposable? OnChange(Action<HbOptions, string?> listener) => null;
    }

    private sealed class FesteZeit(DateTimeOffset jetzt) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => jetzt;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
