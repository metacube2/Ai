using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Modules.Handbestellwesen;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

// Handbestellwesen 2026-10-22: Fachregeln, die die PHP-Anwendung nur im Browser oder gar nicht pruefte.
public sealed class HandbestellwesenServiceTests : IDisposable
{
    private readonly string _pfad = Path.Combine(Path.GetTempPath(), $"hb_test_{Guid.NewGuid():N}.db");
    private readonly HbService _hb;
    private readonly Factory _factory;

    public HandbestellwesenServiceTests()
    {
        _factory = new Factory(HbRegistration.VerbindungFuer(_pfad));
        using (var db = _factory.CreateDbContext())
        {
            db.Database.EnsureCreated();
            db.Kreditoren.Add(new HbKreditor { Nr = "65017", Name = "Muster AG", Ort = "Bern" });
            db.Kostenstellen.AddRange(new HbKostenstelle { Nr = "87", Bezeichnung = "Klein" }, new HbKostenstelle { Nr = "875", Bezeichnung = "Gross" });
            db.Sachkonten.Add(new HbSachkonto { Nr = "67099", Bezeichnung = "Diverses" });
            db.Mitarbeiter.AddRange(
                new HbMitarbeiter { Kurzzeichen = "abc", Nachname = "Besteller", Konto = "abc", Limit = 500m, Kostenstelle = "875" },
                new HbMitarbeiter { Kurzzeichen = "chf", Nachname = "Chefin", Konto = "chf", Limit = 10000m, Kostenstelle = "875" },
                new HbMitarbeiter { Kurzzeichen = "kln", Nachname = "Klein", Konto = "kln", Limit = 800m, Kostenstelle = "875" },
                new HbMitarbeiter { Kurzzeichen = "we", Nachname = "Wareneingang", Konto = "we", Limit = 0m, Kostenstelle = "87", IstWareneingang = true });
            db.SaveChanges();
        }
        var options = new OptionsMonitorStub(new HbOptions { DatabasePath = _pfad, AdminUsers = ["admin"] });
        _hb = new HbService(_factory, options, new FesteZeit(new DateTimeOffset(2026, 10, 22, 9, 0, 0, TimeSpan.Zero)));
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        foreach (var f in new[] { _pfad, _pfad + "-wal", _pfad + "-shm" })
            if (File.Exists(f)) File.Delete(f);
    }

    private Task<HbBenutzer> Wer(string konto) => _hb.BenutzerAsync("TRAFAG\\" + konto);

    private static HbBestellEingabe Eingabe(string kost, decimal wert, string? freigeber = null) => new()
    {
        KreditorNr = "65017", Text = "Div.", Bestellwert = wert, Kostenstelle = kost, Sachkonto = "67099", FreigeberKurzzeichen = freigeber
    };

    [Fact]
    public async Task Nummern_Je_Kostenstelle_Getrennt_87_Trifft_Nicht_875()
    {
        var abc = await Wer("abc");
        var a = await _hb.AnlegenAsync(abc, Eingabe("875", 100m));
        var b = await _hb.AnlegenAsync(abc, Eingabe("87", 100m));
        var c = await _hb.AnlegenAsync(abc, Eingabe("87", 100m));
        var d = await _hb.AnlegenAsync(abc, Eingabe("875", 100m));

        Assert.Equal("E268750001", a.Bestellnummer);
        Assert.Equal("E26870001", b.Bestellnummer);
        Assert.Equal("E26870002", c.Bestellnummer);
        Assert.Equal("E268750002", d.Bestellnummer);
        Assert.Equal("E26 875 0001", HbService.FormatNummer(a.Bestellnummer));
        Assert.Equal("E26 87 0002", HbService.FormatNummer(c.Bestellnummer));
    }

    [Fact]
    public async Task Gleichzeitige_Bestellungen_Bekommen_Verschiedene_Nummern()
    {
        var abc = await Wer("abc");
        var nummern = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => _hb.AnlegenAsync(abc, Eingabe("875", 10m))));
        Assert.Equal(20, nummern.Select(x => x.Bestellnummer).Distinct().Count());
        Assert.Contains(nummern, x => x.Bestellnummer == "E268750020");
    }

    [Fact]
    public async Task Ueber_Limit_Braucht_Berechtigte_Person_Derselben_Kostenstelle()
    {
        var abc = await Wer("abc");
        var ohne = await Assert.ThrowsAsync<HbException>(() => _hb.AnlegenAsync(abc, Eingabe("875", 600m)));
        Assert.Contains("über Ihrer Berechtigung", ohne.Message);
        // kln hat nur 800, darf 900 nicht freigeben; we sitzt auf einer anderen Kostenstelle.
        await Assert.ThrowsAsync<HbException>(() => _hb.AnlegenAsync(abc, Eingabe("875", 900m, "kln")));
        await Assert.ThrowsAsync<HbException>(() => _hb.AnlegenAsync(abc, Eingabe("875", 900m, "we")));

        var ok = await _hb.AnlegenAsync(abc, Eingabe("875", 900m, "chf"));
        Assert.Equal(HbStatus.FreigabeOffen, ok.Status);
        Assert.Equal("chf", ok.FreigeberKurzzeichen);
    }

    [Fact]
    public async Task Unter_Limit_Ohne_Freigabe_Offen()
    {
        var b = await _hb.AnlegenAsync(await Wer("abc"), Eingabe("875", 500m, "chf"));
        Assert.Equal(HbStatus.Offen, b.Status);
        Assert.Null(b.FreigeberKurzzeichen);
    }

    [Fact]
    public async Task Freigabe_Nur_Durch_Genannte_Person_Wareneingang_Erst_Danach()
    {
        var abc = await Wer("abc");
        var b = await _hb.AnlegenAsync(abc, Eingabe("875", 900m, "chf"));

        await Assert.ThrowsAsync<HbException>(() => _hb.WareneingangAsync(abc, b.Id));
        await Assert.ThrowsAsync<HbException>(() => _hb.FreigebenAsync(abc, b.Id));
        await Assert.ThrowsAsync<HbException>(async () => await _hb.FreigebenAsync(await Wer("kln"), b.Id));

        await _hb.FreigebenAsync(await Wer("chf"), b.Id);
        await Assert.ThrowsAsync<HbException>(async () => await _hb.WareneingangAsync(await Wer("kln"), b.Id));
        await _hb.WareneingangAsync(await Wer("we"), b.Id);

        var archiv = await _hb.BestellungenAsync(archiv: true);
        var x = Assert.Single(archiv);
        Assert.Equal(HbStatus.Eingegangen, x.Status);
        Assert.Equal("chf", x.FreigegebenVon);
        Assert.Equal("we", x.EingangVon);
        await Assert.ThrowsAsync<HbException>(() => _hb.LoeschenAsync(abc, b.Id));
    }

    [Fact]
    public async Task Loeschen_Nur_Besteller_Oder_Admin_Und_Protokolliert()
    {
        var b = await _hb.AnlegenAsync(await Wer("abc"), Eingabe("875", 100m));
        await Assert.ThrowsAsync<HbException>(async () => await _hb.LoeschenAsync(await Wer("chf"), b.Id));
        await _hb.LoeschenAsync(await Wer("admin"), b.Id);

        Assert.Empty(await _hb.BestellungenAsync(archiv: false));
        using var db = _factory.CreateDbContext();
        var aktionen = db.Protokoll.Where(x => x.Bezug == b.Bestellnummer).Select(x => x.Aktion + ":" + x.Benutzer).ToList();
        Assert.Equal(["Angelegt:abc", "Gelöscht:admin"], aktionen);
    }

    [Fact]
    public async Task Ohne_Mitarbeitereintrag_Keine_Bestellung_Und_Keine_Stammdaten()
    {
        var fremd = await Wer("niemand");
        Assert.Null(fremd.Mitarbeiter);
        Assert.False(fremd.IstAdmin);
        await Assert.ThrowsAsync<HbException>(() => _hb.AnlegenAsync(fremd, Eingabe("875", 10m)));
        await Assert.ThrowsAsync<HbException>(async () => await _hb.SpeichereSachkontoAsync(await Wer("abc"), new HbSachkonto { Nr = "1", Bezeichnung = "x" }, neu: true));
    }

    [Fact]
    public async Task Suche_Findet_Bestellnummer_Mit_Leerzeichen()
    {
        var b = await _hb.AnlegenAsync(await Wer("abc"), Eingabe("875", 100m));
        var treffer = await _hb.BestellungenAsync(archiv: false, suche: "E26 875 0001");
        Assert.Equal(b.Id, Assert.Single(treffer).Id);
        Assert.Empty(await _hb.BestellungenAsync(archiv: false, suche: "gibtsnicht"));
    }

    // Entscheid 2026-10-22: weiterbetreiben, aber festhalten, wer es wann wie viel braucht.
    [Fact]
    public async Task Nutzung_Je_Person_Kostenstelle_Monat_Ohne_Geloeschte_Mit_Aufrufen()
    {
        var abc = await Wer("abc");
        await _hb.AnlegenAsync(abc, Eingabe("875", 100m));
        await _hb.AnlegenAsync(abc, Eingabe("87", 50.5m));
        var weg = await _hb.AnlegenAsync(abc, Eingabe("875", 999m, "chf"));
        await _hb.LoeschenAsync(abc, weg.Id);
        await _hb.AnlegenAsync(await Wer("chf"), Eingabe("875", 2000m));

        await _hb.ProtokolliereAufrufAsync(abc);
        await _hb.ProtokolliereAufrufAsync(abc); // gleicher Tag zaehlt einmal
        await _hb.ProtokolliereAufrufAsync(await Wer("kln")); // nur geschaut, nichts bestellt

        var n = await _hb.NutzungAsync(await Wer("admin"), new DateTime(2026, 1, 1), new DateTime(2027, 1, 1));

        Assert.Equal(3, n.Bestellungen);
        Assert.Equal(2150.5m, n.Summe);
        Assert.Equal(2, n.Personen);
        var p = n.JePerson.Single(x => x.Schluessel == "abc");
        Assert.Equal((2, 150.5m, 1), (p.Bestellungen, p.Summe, p.Aufrufe));
        var k = n.JePerson.Single(x => x.Schluessel == "kln");
        Assert.Equal((0, 1), (k.Bestellungen, k.Aufrufe));
        Assert.Equal("875", n.JeKostenstelle[0].Schluessel);
        Assert.Equal(2100m, n.JeKostenstelle[0].Summe);
        var monat = Assert.Single(n.JeMonat);
        Assert.Equal(("2026-10", 3, 2), (monat.Monat, monat.Bestellungen, monat.Personen));

        await Assert.ThrowsAsync<HbException>(() => _hb.NutzungAsync(abc, new DateTime(2026, 1, 1), new DateTime(2027, 1, 1)));
    }

    [Fact]
    public void Menue_Enthaelt_Reiter_Handbestellwesen()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        using var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        new DatabaseSeedService().SeedDefaults(db);

        var item = db.NavigationMenuItems.Single(x => x.Key == "handbestellwesen");
        Assert.Null(item.ParentKey);
        Assert.Equal("handbestellwesen", item.Href);
    }

    private sealed class Factory(string verbindung) : IDbContextFactory<HbDbContext>
    {
        public HbDbContext CreateDbContext() => new(new DbContextOptionsBuilder<HbDbContext>().UseSqlite(verbindung).Options);
    }

    private sealed class OptionsMonitorStub(HbOptions value) : IOptionsMonitor<HbOptions>
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
