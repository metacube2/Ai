using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TrafagSalesExporter.Modules.Lieferantenbewertung;

namespace TrafagSalesExporter.Tests;

// Lieferantenbewertung Paket 2 (2026-11-19): Verlauf mit juengster Bewertung in der Uebersicht (Armin 18.11., E2),
// Zertifikatsampel wie V6, Schreibrecht nur EditUsers/Admin, eigener Reiter unter Einkauf.
public sealed class LieferantenbewertungTests : IDisposable
{
    private static readonly DateOnly Heute = new(2026, 11, 19);
    private readonly string _pfad = Path.Combine(Path.GetTempPath(), $"lb_test_{Guid.NewGuid():N}.db");
    private readonly LbService _lb;
    private readonly LbBenutzer _editor = new("koi", false);
    private readonly LbBenutzer _leser = new("xyz", false);

    public LieferantenbewertungTests()
    {
        var factory = new Factory(LbRegistration.VerbindungFuer(_pfad));
        _lb = new LbService(factory, new OptionsStub(new LbOptions { EditUsers = ["KOI "] }),
            new FesteZeit(new DateTimeOffset(2026, 11, 19, 10, 0, 0, TimeSpan.Zero)));
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { File.Delete(_pfad); } catch (IOException) { }
    }

    private static IEnumerable<LbBewertungZeile> Noten(params int?[] noten)
        => LbKatalog.Themenfelder.Select((f, i) => new LbBewertungZeile { Themenfeld = f.Name, Note = i < noten.Length ? noten[i] : null });

    [Fact]
    public void Ampel_Folgt_V6_Abgelaufen_Hoechstens90Tage_Gueltig_Fehlt()
    {
        Assert.Equal(LbZertifikatAmpel.Abgelaufen, LbKatalog.Ampel(new LbZertifikat { GueltigBis = Heute.AddDays(-1) }, Heute));
        Assert.Equal(LbZertifikatAmpel.LaeuftAb, LbKatalog.Ampel(new LbZertifikat { GueltigBis = Heute }, Heute));
        Assert.Equal(LbZertifikatAmpel.LaeuftAb, LbKatalog.Ampel(new LbZertifikat { GueltigBis = Heute.AddDays(90) }, Heute));
        Assert.Equal(LbZertifikatAmpel.Gueltig, LbKatalog.Ampel(new LbZertifikat { GueltigBis = Heute.AddDays(91) }, Heute));
        Assert.Equal(LbZertifikatAmpel.Gueltig, LbKatalog.Ampel(new LbZertifikat { GueltigBis = null }, Heute));
        Assert.Equal(LbZertifikatAmpel.Fehlt, LbKatalog.Ampel(new LbZertifikat { Vorhanden = false, GueltigBis = Heute.AddYears(1) }, Heute));

        Assert.Null(LbKatalog.SchlechtesteAmpel([], Heute));
        Assert.Equal(LbZertifikatAmpel.Abgelaufen, LbKatalog.SchlechtesteAmpel(
            [new LbZertifikat { GueltigBis = Heute.AddYears(1) }, new LbZertifikat { Vorhanden = false }, new LbZertifikat { GueltigBis = Heute.AddDays(-5) }], Heute));
    }

    [Theory]
    [InlineData("0000123456", "123456")]
    [InlineData(" 123456 ", "123456")]
    [InlineData("0000", "0")]
    [InlineData("ab12", "AB12")]
    public void Lieferantennummer_Ohne_Fuehrende_Nullen(string roh, string erwartet)
        => Assert.Equal(erwartet, LbKatalog.NormalisiereNr(roh));

    [Fact]
    public void Sieben_Themenfelder_Und_Skala_Ohne_Drei()
    {
        Assert.Equal(7, LbKatalog.Themenfelder.Count);
        Assert.Equal([1, 2, 4, 5], LbKatalog.Skala.Select(s => s.Wert));
    }

    [Fact]
    public async Task Neue_Bewertung_Behaelt_Verlauf_Und_Uebersicht_Zeigt_Juengste()
    {
        await _lb.NeueBewertungAsync(_editor, "0000100200", "Alpha AG", new DateOnly(2026, 3, 1), "Armin", Noten(2, 2));
        await _lb.NeueBewertungAsync(_editor, "100200", "Alpha AG", new DateOnly(2026, 11, 1), "Armin", Noten(4, 5));
        // Spaeter erfasst, aber aelteres Bewertungsdatum: darf die juengste nicht verdraengen.
        await _lb.NeueBewertungAsync(_editor, "100200", "Alpha AG", new DateOnly(2026, 6, 1), "Marco", Noten(1));

        var daten = await _lb.LadeAsync();
        Assert.Equal(3, daten.Bewertungen.Count);
        var zeile = Assert.Single(LbService.Uebersicht(daten, Heute));
        Assert.Equal("100200", zeile.LieferantNr);
        Assert.Equal(3, zeile.AnzahlBewertungen);
        Assert.Equal(new DateOnly(2026, 11, 1), zeile.JuengsteBewertung!.Datum);
        Assert.Equal(4, zeile.JuengsteBewertung.Zeilen.Single(z => z.Themenfeld == "Technology").Note);
        Assert.Equal(5, zeile.JuengsteBewertung.Zeilen.Single(z => z.Themenfeld == "Quality").Note);
        Assert.Equal("koi", zeile.JuengsteBewertung.ErfasstVon);
    }

    [Fact]
    public async Task Note_Drei_Gilt_Als_Nicht_Beurteilt_Und_Leere_Bewertung_Wird_Abgelehnt()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _lb.NeueBewertungAsync(_editor, "1", "X", Heute, "", Noten()));
        await Assert.ThrowsAsync<ArgumentException>(() => _lb.NeueBewertungAsync(_editor, "1", "X", Heute, "", Noten(3)));

        await _lb.NeueBewertungAsync(_editor, "1", "X", Heute, "", Noten(3, 5));
        var b = Assert.Single((await _lb.LadeAsync()).Bewertungen);
        Assert.Null(b.Zeilen.Single(z => z.Themenfeld == "Technology").Note);
        Assert.Equal(5, b.Zeilen.Single(z => z.Themenfeld == "Quality").Note);
    }

    [Fact]
    public async Task Schreiben_Nur_Fuer_EditUsers_Und_Admins()
    {
        Assert.True(_lb.DarfBearbeiten(_editor));
        Assert.True(_lb.DarfBearbeiten(new LbBenutzer("irgendwer", true)));
        Assert.False(_lb.DarfBearbeiten(_leser));
        Assert.False(_lb.DarfBearbeiten(new LbBenutzer("", false)));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _lb.NeueBewertungAsync(_leser, "1", "X", Heute, "", Noten(4)));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _lb.SpeichereAsync(_leser, new LbZertifikat { LieferantNr = "1", Typ = "ISO 9001" }));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _lb.LoescheAsync<LbZertifikat>(_leser, 1));
    }

    [Fact]
    public async Task Eintraege_Anlegen_Aendern_Loeschen_Und_Uebersicht_Zaehlt()
    {
        await _lb.SpeichereAsync(_editor, new LbZertifikat { LieferantNr = "0042", LieferantName = "Beta", Typ = "ISO 9001", GueltigBis = Heute.AddDays(30) });
        await _lb.SpeichereAsync(_editor, new LbMassnahme { LieferantNr = "42", LieferantName = "Beta", Massnahme = "8D-Bericht anfordern" });
        await _lb.SpeichereAsync(_editor, new LbMassnahme { LieferantNr = "42", LieferantName = "Beta", Massnahme = "Alt", Status = "Erledigt" });
        await _lb.SpeichereAsync(_editor, new LbTermin { LieferantNr = "42", LieferantName = "Beta", Art = "Lieferantenaudit", Datum = Heute.AddDays(10) });
        await _lb.SpeichereAsync(_editor, new LbDokument { LieferantNr = "42", LieferantName = "Beta", Typ = "Zertifikat", Link = @"\\server\ablage\iso.pdf" });
        await Assert.ThrowsAsync<ArgumentException>(() => _lb.SpeichereAsync(_editor, new LbMassnahme { LieferantNr = "42" }));
        await Assert.ThrowsAsync<ArgumentException>(() => _lb.SpeichereAsync(_editor, new LbDokument { LieferantNr = "42", Typ = "Notiz" }));

        var daten = await _lb.LadeAsync();
        var zeile = Assert.Single(LbService.Uebersicht(daten, Heute));
        Assert.Equal("42", zeile.LieferantNr);
        Assert.Null(zeile.JuengsteBewertung);
        Assert.Equal(LbZertifikatAmpel.LaeuftAb, zeile.Zertifikate);
        Assert.Equal(1, zeile.OffeneMassnahmen);
        Assert.Equal("Lieferantenaudit", zeile.NaechsterTermin!.Art);
        Assert.Equal("koi", daten.Zertifikate.Single().GeaendertVon);

        var z = daten.Zertifikate.Single();
        z.GueltigBis = Heute.AddYears(2);
        await _lb.SpeichereAsync(_editor, z);
        Assert.Equal(LbZertifikatAmpel.Gueltig, LbService.Uebersicht(await _lb.LadeAsync(), Heute).Single().Zertifikate);

        await _lb.LoescheAsync<LbZertifikat>(_editor, z.Id);
        Assert.Empty((await _lb.LadeAsync()).Zertifikate);
    }

    [Fact]
    public async Task Bewertung_Loeschen_Entfernt_Auch_Die_Zeilen()
    {
        var id = await _lb.NeueBewertungAsync(_editor, "7", "Gamma", Heute, "", Noten(4, 4, 4));
        await _lb.LoescheAsync<LbBewertung>(_editor, id);

        await using var db = new LbDbContext(new DbContextOptionsBuilder<LbDbContext>().UseSqlite(LbRegistration.VerbindungFuer(_pfad)).Options);
        Assert.Empty(db.Bewertungen);
        Assert.Empty(db.BewertungZeilen);
    }

    [Fact]
    public void Eigener_Reiter_Unter_Einkauf_Mit_Klasse_Und_Eingabeteil()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var seite = File.ReadAllText(Path.Combine(root, "Components", "Pages", "PurchasingDashboard.razor"));
        Assert.Contains("@page \"/einkauf/lieferantenbewertung\"", seite, StringComparison.Ordinal);
        var abschnitt = seite[seite.IndexOf("case \"lieferantenbewertung\":", StringComparison.Ordinal)..];
        abschnitt = abschnitt[..abschnitt.IndexOf("break;", StringComparison.Ordinal)];
        Assert.Contains("<PurchasingSupplierClass", abschnitt, StringComparison.Ordinal);
        Assert.Contains("LieferantenbewertungPanel", abschnitt, StringComparison.Ordinal);
        // Die Klasse steht nur noch auf dem neuen Reiter, nicht mehr doppelt unter „Lieferanten“.
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(seite, "<PurchasingSupplierClass"));

        var seed = File.ReadAllText(Path.Combine(root, "Services", "DatabaseSeedService.cs"));
        Assert.Contains("\"purchasing-supplier-evaluation\", \"purchasing\", \"Lieferantenbewertung\"", seed, StringComparison.Ordinal);
        Assert.Contains("\"einkauf/lieferantenbewertung\"", seed, StringComparison.Ordinal);
        Assert.Contains("AddLieferantenbewertung(", File.ReadAllText(Path.Combine(root, "Program.cs")), StringComparison.Ordinal);
    }

    private sealed class Factory(string verbindung) : IDbContextFactory<LbDbContext>
    {
        public LbDbContext CreateDbContext() => new(new DbContextOptionsBuilder<LbDbContext>().UseSqlite(verbindung).Options);
    }

    private sealed class OptionsStub(LbOptions value) : IOptionsMonitor<LbOptions>
    {
        public LbOptions CurrentValue => value;
        public LbOptions Get(string? name) => value;
        public IDisposable? OnChange(Action<LbOptions, string?> listener) => null;
    }

    private sealed class FesteZeit(DateTimeOffset jetzt) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => jetzt;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
