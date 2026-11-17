using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TrafagSalesExporter.Modules.Handbestellwesen;

namespace TrafagSalesExporter.Tests;

// Handbestellwesen 2026-11-17 (Auftrag Ingo): naechtlicher AD-Abgleich. Gesperrte und nicht im AD vorhandene Konten werden
// inaktiv, Namen der AD-Eintraege werden nachgefuehrt, Rollen bleiben, niemand wird wieder aktiv, nichts wird geloescht.
public sealed class HandbestellwesenAdAbgleichTests : IDisposable
{
    private readonly string _pfad = Path.Combine(Path.GetTempPath(), $"hb_ad_{Guid.NewGuid():N}.db");
    private readonly Factory _factory;
    private readonly HbOptions _optionen;
    private readonly AdFake _ad = new();
    private readonly Uhr _uhr = new(new DateTimeOffset(2026, 11, 17, 3, 0, 0, TimeSpan.Zero));
    private readonly HbAdAbgleichService _abgleich;
    private static readonly HbBenutzer Admin = new("admin", null, true);

    public HandbestellwesenAdAbgleichTests()
    {
        _factory = new Factory(HbRegistration.VerbindungFuer(_pfad));
        using (var db = _factory.CreateDbContext())
        {
            db.Database.EnsureCreated();
            db.Mitarbeiter.AddRange(
                new HbMitarbeiter { Kurzzeichen = "abc", Vorname = "A", Nachname = "Altbestand", Konto = "abc", Limit = 500m, Kostenstelle = "875" },
                new HbMitarbeiter { Kurzzeichen = "alt", Vorname = "Al", Nachname = "Ausgetreten", Konto = "alt", Limit = 800m },
                new HbMitarbeiter { Kurzzeichen = "ges", Vorname = "G", Nachname = "Gesperrt", Konto = "ges", AusAd = true, Limit = 900m },
                new HbMitarbeiter { Kurzzeichen = "weg", Vorname = "W", Nachname = "Geloescht", Konto = "weg", AusAd = true, Limit = 5000m },
                new HbMitarbeiter { Kurzzeichen = "neu", Vorname = "Anna", Nachname = "Alt", Mail = "anna.alt@trafag.com", Konto = "neu", AusAd = true,
                    Limit = 700m, Kostenstelle = "875", IstAdmin = true, IstWareneingang = true },
                new HbMitarbeiter { Kurzzeichen = "inakt", Nachname = "Inaktiv", Konto = "inakt", AusAd = true, Aktiv = false },
                new HbMitarbeiter { Kurzzeichen = "ohne", Nachname = "Ohne Konto", Konto = "" },
                new HbMitarbeiter { Kurzzeichen = "abl", Vorname = "B", Nachname = "Abgelaufen", Konto = "abl", AusAd = true },
                new HbMitarbeiter { Kurzzeichen = "gro", Vorname = "C", Nachname = "Gross", Konto = "TRAFAG\\GRO" });
            db.Bestellungen.AddRange(
                Bestellung("E268750001", "weg", null, HbStatus.Offen),
                Bestellung("E268750002", "abc", "weg", HbStatus.FreigabeOffen),
                Bestellung("E268750003", "weg", null, HbStatus.Eingegangen));
            db.SaveChanges();
        }
        _ad.Setze(
            Konto("abc"), Konto("ges", gesperrt: true), Konto("neu", "Anna", "Neu", "anna.neu@trafag.com"), Konto("inakt"),
            Konto("abl", "B", "Abgelaufen", ablauf: new DateTime(2026, 10, 31, 0, 0, 0, DateTimeKind.Utc)), Konto("gro", "C", "Gross"),
            Konto("fuell1"), Konto("fuell2"));
        _optionen = new HbOptions { DatabasePath = _pfad, AdminUsers = ["admin"], AdAbgleich = new HbAdAbgleichOptions { MinAdKonten = 3, MaxDeaktivierungen = 10 } };
        _abgleich = new HbAdAbgleichService(_factory, _ad, new OptionsStub(_optionen), _uhr, NullLogger<HbAdAbgleichService>.Instance);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        foreach (var f in new[] { _pfad, _pfad + "-wal", _pfad + "-shm" })
            if (File.Exists(f)) File.Delete(f);
    }

    [Fact]
    public async Task Vorschau_Deaktiviert_Gesperrte_Und_Fehlende_Und_Fuehrt_Namen_Nach()
    {
        var plan = await _abgleich.VorschauAsync(Admin);

        Assert.Null(plan.Fehler);
        Assert.Equal(8, plan.AdKonten);
        var p = plan.Posten.ToDictionary(x => x.Kurzzeichen);
        Assert.Equal((HbAdBefund.NichtImAd, true), (p["alt"].Befund, p["alt"].Deaktivieren));
        Assert.Equal((HbAdBefund.Gesperrt, true), (p["ges"].Befund, p["ges"].Deaktivieren));
        Assert.Equal((HbAdBefund.ImAdGeloescht, true), (p["weg"].Befund, p["weg"].Deaktivieren));
        Assert.Equal((HbAdBefund.NameGeaendert, false), (p["neu"].Befund, p["neu"].Deaktivieren));
        Assert.Equal((HbAdBefund.Abgelaufen, false), (p["abl"].Befund, p["abl"].Deaktivieren));
        // Altbestand im AD, inaktive Eintraege, Eintraege ohne Konto und Konto mit Domaene und Grossschreibung: nichts.
        Assert.DoesNotContain("abc", p.Keys);
        Assert.DoesNotContain("inakt", p.Keys);
        Assert.DoesNotContain("ohne", p.Keys);
        Assert.DoesNotContain("gro", p.Keys);
        Assert.Equal(3, plan.Deaktivierungen);
        Assert.Equal(1, plan.Namensaenderungen);
        // Was an der Person haengt: offene Bestellung als Besteller, offene Freigabe; Eingegangene zaehlen nicht.
        Assert.Equal((1, 1), (p["weg"].OffenAlsBesteller, p["weg"].FreigabeOffenBei));
    }

    [Fact]
    public async Task Vorschau_Aendert_Nichts()
    {
        await _abgleich.VorschauAsync(Admin);

        await using var db = _factory.CreateDbContext();
        Assert.Equal(8, await db.Mitarbeiter.CountAsync(x => x.Aktiv));
        Assert.Equal("Alt", (await db.Mitarbeiter.SingleAsync(x => x.Kurzzeichen == "neu")).Nachname);
        Assert.Empty(await db.Protokoll.ToListAsync());
    }

    [Fact]
    public async Task Nachtlauf_Ohne_Schreiben_Aendert_Nichts_Und_Protokolliert_Die_Vorschau()
    {
        var e = await _abgleich.NachtlaufAsync();

        Assert.False(e.Geschrieben);
        await using var db = _factory.CreateDbContext();
        Assert.Equal(8, await db.Mitarbeiter.CountAsync(x => x.Aktiv));
        var p = Assert.Single(await db.Protokoll.ToListAsync());
        Assert.Equal((HbAdAbgleichService.AktionVorschau, HbAdAbgleichService.SystemKonto), (p.Aktion, p.Benutzer));
        Assert.Contains("alt (nicht im AD)", p.Details);
        Assert.Contains("weg (im AD gelöscht)", p.Details);
        Assert.True(await _abgleich.HeuteErledigtAsync());
    }

    [Fact]
    public async Task Nachtlauf_Mit_Schreiben_Deaktiviert_Fuehrt_Namen_Nach_Und_Laesst_Rollen()
    {
        _optionen.AdAbgleich.Schreiben = true;

        var e = await _abgleich.NachtlaufAsync();

        Assert.True(e.Geschrieben);
        await using var db = _factory.CreateDbContext();
        var m = await db.Mitarbeiter.ToDictionaryAsync(x => x.Kurzzeichen);
        Assert.False(m["alt"].Aktiv);
        Assert.False(m["ges"].Aktiv);
        Assert.False(m["weg"].Aktiv);
        Assert.True(m["abc"].Aktiv);
        Assert.True(m["abl"].Aktiv);
        Assert.True(m["ohne"].Aktiv);
        Assert.False(m["inakt"].Aktiv);
        // Nichts geloescht, Limit bleibt (auch bei Deaktivierten).
        Assert.Equal(9, m.Count);
        Assert.Equal(5000m, m["weg"].Limit);
        // Name und Mail aus dem AD, Konto, Kurzzeichen und Rollen unveraendert.
        var neu = m["neu"];
        Assert.Equal(("Anna", "Neu", "anna.neu@trafag.com"), (neu.Vorname, neu.Nachname, neu.Mail));
        Assert.Equal(("neu", "neu", 700m, "875", true, true, true), (neu.Kurzzeichen, neu.Konto, neu.Limit, neu.Kostenstelle, neu.IstAdmin, neu.IstWareneingang, neu.Aktiv));
        var protokoll = await db.Protokoll.ToListAsync();
        Assert.Equal(3, protokoll.Count(x => x.Aktion == "Mitarbeiter deaktiviert" && x.Benutzer == HbAdAbgleichService.SystemKonto));
        Assert.Contains(protokoll, x => x.Aktion == "Mitarbeiter deaktiviert" && x.Bezug == "weg" && x.Details.Contains("1 als Besteller, 1 Freigaben"));
        Assert.Single(protokoll, x => x.Aktion == "Mitarbeiter aus AD aktualisiert" && x.Bezug == "neu");
        Assert.Single(protokoll, x => x.Aktion == HbAdAbgleichService.AktionLauf);
    }

    [Fact]
    public async Task Wieder_Aktiv_Im_AD_Macht_Niemanden_Automatisch_Aktiv()
    {
        _optionen.AdAbgleich.Schreiben = true;
        await _abgleich.NachtlaufAsync();
        _ad.Setze(Konto("abc"), Konto("alt"), Konto("ges"), Konto("weg"), Konto("neu", "Anna", "Neu", "anna.neu@trafag.com"), Konto("inakt"), Konto("abl"), Konto("gro"));

        var plan = await _abgleich.VorschauAsync(Admin);

        Assert.DoesNotContain(plan.Posten, x => x.Kurzzeichen is "alt" or "ges" or "weg" or "inakt");
        await using var db = _factory.CreateDbContext();
        Assert.False((await db.Mitarbeiter.SingleAsync(x => x.Kurzzeichen == "ges")).Aktiv);
    }

    [Fact]
    public async Task AD_Nicht_Lesbar_Bricht_Ohne_Aenderung_Ab_Und_Gilt_Nicht_Als_Erledigt()
    {
        _optionen.AdAbgleich.Schreiben = true;
        _ad.Fehler = "Server nicht erreichbar";

        var e = await _abgleich.NachtlaufAsync();

        Assert.False(e.Geschrieben);
        Assert.Contains("AD nicht lesbar", e.Meldung);
        await using var db = _factory.CreateDbContext();
        Assert.Equal(8, await db.Mitarbeiter.CountAsync(x => x.Aktiv));
        Assert.Single(await db.Protokoll.ToListAsync(), x => x.Aktion == HbAdAbgleichService.AktionAbbruch);
        Assert.False(await _abgleich.HeuteErledigtAsync());
    }

    [Fact]
    public async Task Zu_Wenige_AD_Konten_Gelten_Als_Unvollstaendig()
    {
        _optionen.AdAbgleich.Schreiben = true;
        _optionen.AdAbgleich.MinAdKonten = 100;

        var e = await _abgleich.NachtlaufAsync();

        Assert.False(e.Geschrieben);
        Assert.Contains("unvollständig", e.Meldung);
        await using var db = _factory.CreateDbContext();
        Assert.Equal(8, await db.Mitarbeiter.CountAsync(x => x.Aktiv));
    }

    [Fact]
    public async Task Grenze_Stoppt_Den_Nachtlauf_Aber_Nicht_Den_Admin()
    {
        _optionen.AdAbgleich.Schreiben = true;
        _optionen.AdAbgleich.MaxDeaktivierungen = 2;

        var nacht = await _abgleich.NachtlaufAsync();

        Assert.False(nacht.Geschrieben);
        await using (var db = _factory.CreateDbContext())
        {
            Assert.Equal(8, await db.Mitarbeiter.CountAsync(x => x.Aktiv));
            Assert.Single(await db.Protokoll.ToListAsync(), x => x.Aktion == HbAdAbgleichService.AktionGestoppt);
        }
        Assert.True(await _abgleich.HeuteErledigtAsync());

        var vonHand = await _abgleich.AusfuehrenAsync(Admin);

        Assert.True(vonHand.Geschrieben);
        await using var db2 = _factory.CreateDbContext();
        Assert.Equal(5, await db2.Mitarbeiter.CountAsync(x => x.Aktiv));
        Assert.Contains(await db2.Protokoll.ToListAsync(), x => x.Aktion == "Mitarbeiter deaktiviert" && x.Benutzer == "admin");
    }

    [Fact]
    public async Task Nur_Admins_Duerfen_Vorschau_Und_Ausfuehren()
    {
        var abc = new HbBenutzer("abc", null, false);

        await Assert.ThrowsAsync<HbException>(() => _abgleich.VorschauAsync(abc));
        await Assert.ThrowsAsync<HbException>(() => _abgleich.AusfuehrenAsync(abc));
        await Assert.ThrowsAsync<HbException>(() => _abgleich.LetzteLaeufeAsync(abc));
    }

    [Fact]
    public async Task Job_Laeuft_Einmal_Am_Tag_Ab_Der_Uhrzeit_Und_Holt_Verpasstes_Nach()
    {
        var job = new HbAdAbgleichJob(_abgleich, new OptionsStub(_optionen), _uhr, NullLogger<HbAdAbgleichJob>.Instance);

        _uhr.Jetzt = new DateTimeOffset(2026, 11, 17, 2, 0, 0, TimeSpan.Zero);
        Assert.False(await job.PruefeUndLaufeAsync(default));
        // Verpasst (Server war um 02:30 nicht da): um 06:20 nachgeholt.
        _uhr.Jetzt = new DateTimeOffset(2026, 11, 17, 6, 20, 0, TimeSpan.Zero);
        Assert.True(await job.PruefeUndLaufeAsync(default));
        _uhr.Jetzt = new DateTimeOffset(2026, 11, 17, 22, 0, 0, TimeSpan.Zero);
        Assert.False(await job.PruefeUndLaufeAsync(default));
        _uhr.Jetzt = new DateTimeOffset(2026, 11, 18, 2, 45, 0, TimeSpan.Zero);
        Assert.True(await job.PruefeUndLaufeAsync(default));
    }

    [Fact]
    public async Task Job_Wiederholt_Nach_AD_Fehler_Fruehestens_Nach_Zwei_Stunden()
    {
        var job = new HbAdAbgleichJob(_abgleich, new OptionsStub(_optionen), _uhr, NullLogger<HbAdAbgleichJob>.Instance);
        _ad.Fehler = "Zeitueberschreitung";

        _uhr.Jetzt = new DateTimeOffset(2026, 11, 17, 3, 0, 0, TimeSpan.Zero);
        Assert.True(await job.PruefeUndLaufeAsync(default));
        _uhr.Jetzt = new DateTimeOffset(2026, 11, 17, 4, 0, 0, TimeSpan.Zero);
        Assert.False(await job.PruefeUndLaufeAsync(default));
        _ad.Fehler = null;
        _uhr.Jetzt = new DateTimeOffset(2026, 11, 17, 5, 1, 0, TimeSpan.Zero);
        Assert.True(await job.PruefeUndLaufeAsync(default));
        Assert.True(await _abgleich.HeuteErledigtAsync());
    }

    [Fact]
    public async Task Job_Laeuft_Nicht_Wenn_Ausgeschaltet()
    {
        _optionen.AdAbgleich.Enabled = false;
        var job = new HbAdAbgleichJob(_abgleich, new OptionsStub(_optionen), _uhr, NullLogger<HbAdAbgleichJob>.Instance);

        Assert.False(await job.PruefeUndLaufeAsync(default));
    }

    private static HbAdKonto Konto(string konto, string vorname = "", string nachname = "", string mail = "", bool gesperrt = false, DateTime? ablauf = null)
        => new(konto, vorname, nachname.Length > 0 ? nachname : konto, mail, konto, gesperrt, ablauf);

    private static HbBestellung Bestellung(string nr, string besteller, string? freigeber, HbStatus status) => new()
    {
        Bestellnummer = nr, KreditorNr = "65017", Text = "Div.", Bestellwert = 100m, BestellerKurzzeichen = besteller,
        FreigeberKurzzeichen = freigeber, Kostenstelle = "875", Sachkonto = "67099", Status = status,
        AngelegtAm = new DateTime(2026, 11, 10), AngelegtVon = besteller
    };

    private sealed class AdFake : IHbAdVerzeichnis
    {
        private Dictionary<string, HbAdKonto> _konten = new(StringComparer.OrdinalIgnoreCase);
        public string? Fehler { get; set; }

        public void Setze(params HbAdKonto[] konten) => _konten = konten.ToDictionary(x => x.Konto, StringComparer.OrdinalIgnoreCase);

        public Task<HbAdStand> LadeAlleAsync(CancellationToken ct = default)
            => Task.FromResult(Fehler is null ? new HbAdStand(_konten, null) : new HbAdStand(null, Fehler));
    }

    private sealed class Factory(string verbindung) : IDbContextFactory<HbDbContext>
    {
        public HbDbContext CreateDbContext() => new(new DbContextOptionsBuilder<HbDbContext>().UseSqlite(verbindung).Options);
    }

    private sealed class OptionsStub(HbOptions value) : IOptionsMonitor<HbOptions>
    {
        public HbOptions CurrentValue => value;
        public HbOptions Get(string? name) => value;
        public IDisposable? OnChange(Action<HbOptions, string?> listener) => null;
    }

    private sealed class Uhr(DateTimeOffset jetzt) : TimeProvider
    {
        public DateTimeOffset Jetzt { get; set; } = jetzt;
        public override DateTimeOffset GetUtcNow() => Jetzt;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
