using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TrafagSalesExporter.Modules.Handbestellwesen;

namespace TrafagSalesExporter.Tests;

// Handbestellwesen 2026-11-17 (Auftrag Ingo): naechtlicher AD-Abgleich. Nach den Antworten Ingos vom selben Tag: gesperrt,
// abgelaufen oder nicht im AD = inaktiv mit Vermerk; vom Abgleich Deaktivierte werden wieder aktiv, wenn das Konto im AD wieder
// aktiv ist; Altbestand mit aktivem AD-Konto wird AD-Eintrag; Kurzzeichen, Konto und Rollen bleiben; nichts wird geloescht;
// offene Bestellungen Deaktivierter weist ein Admin neu zu.
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
                new HbMitarbeiter { Kurzzeichen = "abc", Vorname = "A", Nachname = "Altbestand", Mail = "alt@trafag.com", Konto = "abc", Limit = 500m, Kostenstelle = "875" },
                new HbMitarbeiter { Kurzzeichen = "alt", Vorname = "Al", Nachname = "Ausgetreten", Konto = "alt", Limit = 800m },
                new HbMitarbeiter { Kurzzeichen = "ges", Vorname = "G", Nachname = "Gesperrt", Konto = "ges", AusAd = true, Limit = 900m },
                new HbMitarbeiter { Kurzzeichen = "weg", Vorname = "W", Nachname = "Geloescht", Konto = "weg", AusAd = true, Limit = 5000m },
                new HbMitarbeiter { Kurzzeichen = "neu", Vorname = "Anna", Nachname = "Alt", Mail = "anna.alt@trafag.com", Konto = "neu", AusAd = true,
                    Limit = 700m, Kostenstelle = "875", IstAdmin = true, IstWareneingang = true },
                new HbMitarbeiter { Kurzzeichen = "inakt", Nachname = "Inaktiv", Konto = "inakt", AusAd = true, Aktiv = false },
                new HbMitarbeiter { Kurzzeichen = "ohne", Nachname = "Ohne Konto", Konto = "" },
                new HbMitarbeiter { Kurzzeichen = "abl", Vorname = "B", Nachname = "Abgelaufen", Konto = "abl", AusAd = true },
                new HbMitarbeiter { Kurzzeichen = "gro", Vorname = "C", Nachname = "Gross", Konto = "TRAFAG\\GRO" },
                new HbMitarbeiter { Kurzzeichen = "RFI", Vorname = "R", Nachname = "Fi", Konto = "rfi", Limit = 300m },
                new HbMitarbeiter { Kurzzeichen = "rueck", Nachname = "Zurueck", Konto = "rueck", AusAd = true, Aktiv = false, AdDeaktiviert = true,
                    InaktivSeit = new DateTime(2026, 11, 10), InaktivGrund = "AD-Abgleich: im AD gesperrt", Limit = 400m },
                new HbMitarbeiter { Kurzzeichen = "manu", Nachname = "Vonhand", Konto = "manu", AusAd = true, Aktiv = false,
                    InaktivGrund = "von Hand deaktiviert (koi)" });
            db.Bestellungen.AddRange(
                Bestellung("E268750001", "weg", null, HbStatus.Offen),
                Bestellung("E268750002", "abc", "weg", HbStatus.FreigabeOffen),
                Bestellung("E268750003", "weg", null, HbStatus.Eingegangen),
                Bestellung("E268750004", "RFI", null, HbStatus.Offen));
            db.SaveChanges();
        }
        _ad.Setze(
            Konto("abc", "Anton", "Altmann", "anton.altmann@trafag.com"), Konto("ges", gesperrt: true), Konto("neu", "Anna", "Neu", "anna.neu@trafag.com"),
            Konto("inakt"), Konto("abl", "B", "Abgelaufen", ablauf: new DateTime(2026, 10, 31, 0, 0, 0, DateTimeKind.Utc)), Konto("gro", "C", "Gross"),
            Konto("rfi", "Ronny", "Fischer", "ronny.fischer@trafag.com"), Konto("rueck", "Rita", "Zurueck"), Konto("manu"), Konto("fuell1"), Konto("fuell2"));
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
    public async Task Vorschau_Wendet_Alle_Regeln_An()
    {
        var plan = await _abgleich.VorschauAsync(Admin);

        Assert.Null(plan.Fehler);
        Assert.Equal(11, plan.AdKonten);
        var p = plan.Posten.ToDictionary(x => x.Kurzzeichen);
        Assert.Equal((HbAdBefund.NichtImAd, HbAdAktion.Deaktivieren), (p["alt"].Befund, p["alt"].Aktion));
        Assert.Equal((HbAdBefund.Gesperrt, HbAdAktion.Deaktivieren), (p["ges"].Befund, p["ges"].Aktion));
        Assert.Equal((HbAdBefund.ImAdGeloescht, HbAdAktion.Deaktivieren), (p["weg"].Befund, p["weg"].Aktion));
        Assert.Equal((HbAdBefund.Abgelaufen, HbAdAktion.Deaktivieren), (p["abl"].Befund, p["abl"].Aktion));
        Assert.Equal((HbAdBefund.NameGeaendert, HbAdAktion.Nachfuehren), (p["neu"].Befund, p["neu"].Aktion));
        Assert.Equal((HbAdBefund.AltbestandImAd, HbAdAktion.Uebernehmen), (p["abc"].Befund, p["abc"].Aktion));
        Assert.Equal((HbAdBefund.AltbestandImAd, HbAdAktion.Uebernehmen), (p["gro"].Befund, p["gro"].Aktion));
        Assert.Equal((HbAdBefund.AltbestandImAd, HbAdAktion.Uebernehmen), (p["RFI"].Befund, p["RFI"].Aktion));
        Assert.Equal((HbAdBefund.WiederAktiv, HbAdAktion.Reaktivieren), (p["rueck"].Befund, p["rueck"].Aktion));
        // Von Hand Deaktivierte, inaktive ohne Vermerk und Eintraege ohne Konto: nichts.
        Assert.DoesNotContain("manu", p.Keys);
        Assert.DoesNotContain("inakt", p.Keys);
        Assert.DoesNotContain("ohne", p.Keys);
        Assert.Equal((4, 1, 3, 1), (plan.Deaktivierungen, plan.Reaktivierungen, plan.Uebernahmen, plan.Namensaenderungen));
        // Was an der Person haengt: offene Bestellung als Besteller, offene Freigabe; Eingegangene zaehlen nicht.
        Assert.Equal((1, 1), (p["weg"].OffenAlsBesteller, p["weg"].FreigabeOffenBei));
    }

    [Fact]
    public async Task Vorschau_Aendert_Nichts()
    {
        await _abgleich.VorschauAsync(Admin);

        await using var db = _factory.CreateDbContext();
        Assert.Equal(9, await db.Mitarbeiter.CountAsync(x => x.Aktiv));
        Assert.Equal("Alt", (await db.Mitarbeiter.SingleAsync(x => x.Kurzzeichen == "neu")).Nachname);
        Assert.Empty(await db.Protokoll.ToListAsync());
    }

    [Fact]
    public async Task Nachtlauf_Ohne_Schreiben_Aendert_Nichts_Und_Protokolliert_Die_Vorschau()
    {
        var e = await _abgleich.NachtlaufAsync();

        Assert.False(e.Geschrieben);
        await using var db = _factory.CreateDbContext();
        Assert.Equal(9, await db.Mitarbeiter.CountAsync(x => x.Aktiv));
        var p = Assert.Single(await db.Protokoll.ToListAsync());
        Assert.Equal((HbAdAbgleichService.AktionVorschau, HbAdAbgleichService.SystemKonto), (p.Aktion, p.Benutzer));
        Assert.Contains("alt (nicht im AD)", p.Details);
        Assert.Contains("weg (im AD gelöscht)", p.Details);
        Assert.Contains("rueck (im AD wieder aktiv)", p.Details);
        Assert.True(await _abgleich.HeuteErledigtAsync());
    }

    [Fact]
    public async Task Nachtlauf_Mit_Schreiben_Setzt_Vermerk_Uebernimmt_Altbestand_Und_Laesst_Kurzzeichen_Und_Rollen()
    {
        _optionen.AdAbgleich.Schreiben = true;

        var e = await _abgleich.NachtlaufAsync();

        Assert.True(e.Geschrieben);
        await using var db = _factory.CreateDbContext();
        var m = await db.Mitarbeiter.ToDictionaryAsync(x => x.Kurzzeichen);
        Assert.Equal(12, m.Count);
        foreach (var k in new[] { "alt", "ges", "weg", "abl" })
        {
            Assert.False(m[k].Aktiv);
            Assert.True(m[k].AdDeaktiviert);
            Assert.Equal(new DateTime(2026, 11, 17, 3, 0, 0), m[k].InaktivSeit);
        }
        Assert.Equal("AD-Abgleich: im AD gesperrt", m["ges"].InaktivGrund);
        Assert.Equal("AD-Abgleich: im AD abgelaufen", m["abl"].InaktivGrund);
        Assert.Equal(5000m, m["weg"].Limit);
        // Wieder aktiv, Vermerk weg, Name aus dem AD.
        Assert.Equal((true, false, null, null, "Zurueck"), (m["rueck"].Aktiv, m["rueck"].AdDeaktiviert, m["rueck"].InaktivSeit, m["rueck"].InaktivGrund, m["rueck"].Nachname));
        Assert.False(m["manu"].Aktiv);
        Assert.False(m["inakt"].Aktiv);
        Assert.True(m["ohne"].Aktiv);
        // Altbestand wird AD-Eintrag: Name und Mail aus dem AD, Kurzzeichen und Konto bleiben (RFI mit Konto rfi).
        Assert.Equal((true, "Anton", "Altmann", "anton.altmann@trafag.com", "abc", 500m), (m["abc"].AusAd, m["abc"].Vorname, m["abc"].Nachname, m["abc"].Mail, m["abc"].Konto, m["abc"].Limit));
        Assert.Equal((true, "RFI", "rfi", "Fischer"), (m["RFI"].AusAd, m["RFI"].Kurzzeichen, m["RFI"].Konto, m["RFI"].Nachname));
        Assert.Equal((true, "TRAFAG\\GRO"), (m["gro"].AusAd, m["gro"].Konto));
        Assert.Equal("RFI", (await db.Bestellungen.SingleAsync(x => x.Bestellnummer == "E268750004")).BestellerKurzzeichen);
        // AD-Eintrag: Name und Mail nachgefuehrt, Rollen unveraendert.
        var neu = m["neu"];
        Assert.Equal(("Anna", "Neu", "anna.neu@trafag.com"), (neu.Vorname, neu.Nachname, neu.Mail));
        Assert.Equal(("neu", "neu", 700m, "875", true, true, true), (neu.Kurzzeichen, neu.Konto, neu.Limit, neu.Kostenstelle, neu.IstAdmin, neu.IstWareneingang, neu.Aktiv));
        var protokoll = await db.Protokoll.ToListAsync();
        Assert.Equal(4, protokoll.Count(x => x.Aktion == "Mitarbeiter deaktiviert" && x.Benutzer == HbAdAbgleichService.SystemKonto));
        Assert.Contains(protokoll, x => x.Aktion == "Mitarbeiter deaktiviert" && x.Bezug == "weg" && x.Details.Contains("1 als Besteller, 1 Freigaben"));
        Assert.Single(protokoll, x => x.Aktion == "Mitarbeiter reaktiviert" && x.Bezug == "rueck");
        Assert.Equal(3, protokoll.Count(x => x.Aktion == "Mitarbeiter aus AD übernommen"));
        Assert.Single(protokoll, x => x.Aktion == "Mitarbeiter aus AD aktualisiert" && x.Bezug == "neu");
        Assert.Single(protokoll, x => x.Aktion == HbAdAbgleichService.AktionLauf);
    }

    [Fact]
    public async Task Wieder_Aktiv_Im_AD_Macht_Nur_Vom_Abgleich_Deaktivierte_Wieder_Aktiv()
    {
        _optionen.AdAbgleich.Schreiben = true;
        await _abgleich.NachtlaufAsync();
        _ad.Setze(Konto("abc"), Konto("alt"), Konto("ges"), Konto("weg"), Konto("neu", "Anna", "Neu", "anna.neu@trafag.com"), Konto("inakt"),
            Konto("abl"), Konto("gro"), Konto("rfi"), Konto("rueck"), Konto("manu"));

        var plan = await _abgleich.VorschauAsync(Admin);

        var wieder = plan.Posten.Where(x => x.Aktion == HbAdAktion.Reaktivieren).Select(x => x.Kurzzeichen).OrderBy(x => x).ToList();
        Assert.Equal(["abl", "alt", "ges", "weg"], wieder);
        await _abgleich.AusfuehrenAsync(Admin);
        await using var db = _factory.CreateDbContext();
        var m = await db.Mitarbeiter.ToDictionaryAsync(x => x.Kurzzeichen);
        Assert.True(m["ges"].Aktiv);
        Assert.True(m["alt"].AusAd);
        Assert.False(m["manu"].Aktiv);
        Assert.False(m["inakt"].Aktiv);
    }

    [Fact]
    public async Task Von_Hand_Gesetzter_Status_Loescht_Den_Vermerk()
    {
        _optionen.AdAbgleich.Schreiben = true;
        await _abgleich.NachtlaufAsync();
        var hb = new HbService(_factory, new OptionsStub(_optionen), _uhr);
        HbMitarbeiter Lade(string k) { using var db = _factory.CreateDbContext(); return db.Mitarbeiter.AsNoTracking().Single(x => x.Kurzzeichen == k); }

        var ges = Lade("ges");
        ges.Aktiv = true;
        await hb.SpeichereMitarbeiterAsync(Admin, ges);
        var abc = Lade("abc");
        abc.Aktiv = false;
        await hb.SpeichereMitarbeiterAsync(Admin, abc);

        Assert.Equal((true, false, null), (Lade("ges").Aktiv, Lade("ges").AdDeaktiviert, Lade("ges").InaktivGrund));
        Assert.Equal((false, false, "von Hand deaktiviert (admin)"), (Lade("abc").Aktiv, Lade("abc").AdDeaktiviert, Lade("abc").InaktivGrund));
        // abc ist im AD aktiv, wurde aber von Hand deaktiviert: der Abgleich laesst es inaktiv.
        var plan = await _abgleich.VorschauAsync(Admin);
        Assert.DoesNotContain(plan.Posten, x => x.Kurzzeichen == "abc");
    }

    [Fact]
    public async Task Aus_AD_Aktualisieren_Laesst_Das_Kurzzeichen()
    {
        _optionen.AdAbgleich.Schreiben = true;
        await _abgleich.NachtlaufAsync();
        var hb = new HbService(_factory, new OptionsStub(_optionen), _uhr, new SucheFake(new HbAdPerson("rfi", "Ronny", "Fischer-Neu", "rf@trafag.com", "Ronny Fischer")));
        await using var db = _factory.CreateDbContext();
        var rfi = await db.Mitarbeiter.AsNoTracking().SingleAsync(x => x.Kurzzeichen == "RFI");

        await hb.AktualisiereAusAdAsync(Admin, rfi.Id);

        await using var db2 = _factory.CreateDbContext();
        var neu = await db2.Mitarbeiter.SingleAsync(x => x.Id == rfi.Id);
        Assert.Equal(("RFI", "rfi", "Fischer-Neu"), (neu.Kurzzeichen, neu.Konto, neu.Nachname));
    }

    [Fact]
    public async Task Offene_Bestellungen_Deaktivierter_Weist_Ein_Admin_Neu_Zu()
    {
        _optionen.AdAbgleich.Schreiben = true;
        await _abgleich.NachtlaufAsync();
        var hb = new HbService(_factory, new OptionsStub(_optionen), _uhr);

        var offen = await hb.OffeneBeiDeaktiviertenAsync();

        Assert.Equal(2, offen.Count);
        var b1 = Assert.Single(offen, x => x.Bestellung.Bestellnummer == "E268750001");
        var b2 = Assert.Single(offen, x => x.Bestellung.Bestellnummer == "E268750002");
        Assert.Equal((true, false), (b1.BestellerInaktiv, b1.FreigeberInaktiv));
        Assert.Equal((false, true), (b2.BestellerInaktiv, b2.FreigeberInaktiv));

        await Assert.ThrowsAsync<HbException>(() => hb.NeuZuweisenAsync(new HbBenutzer("abc", null, false), b1.Bestellung.Id, "neu", null));
        await Assert.ThrowsAsync<HbException>(() => hb.NeuZuweisenAsync(Admin, b1.Bestellung.Id, "ges", null));      // inaktiv
        await Assert.ThrowsAsync<HbException>(() => hb.NeuZuweisenAsync(Admin, b2.Bestellung.Id, null, "ohne"));     // Limit 0
        await Assert.ThrowsAsync<HbException>(() => hb.NeuZuweisenAsync(Admin, b2.Bestellung.Id, null, "abc"));      // = Besteller
        await Assert.ThrowsAsync<HbException>(() => hb.NeuZuweisenAsync(Admin, b1.Bestellung.Id, null, "neu"));      // keine offene Freigabe
        await using (var db = _factory.CreateDbContext())
        {
            var eingegangen = await db.Bestellungen.SingleAsync(x => x.Bestellnummer == "E268750003");
            await Assert.ThrowsAsync<HbException>(() => hb.NeuZuweisenAsync(Admin, eingegangen.Id, "neu", null));
        }

        await hb.NeuZuweisenAsync(Admin, b1.Bestellung.Id, "neu", null);
        await hb.NeuZuweisenAsync(Admin, b2.Bestellung.Id, null, "neu");

        Assert.Empty(await hb.OffeneBeiDeaktiviertenAsync());
        await using var db2 = _factory.CreateDbContext();
        Assert.Equal("neu", (await db2.Bestellungen.SingleAsync(x => x.Id == b1.Bestellung.Id)).BestellerKurzzeichen);
        Assert.Equal("neu", (await db2.Bestellungen.SingleAsync(x => x.Id == b2.Bestellung.Id)).FreigeberKurzzeichen);
        Assert.Equal(2, await db2.Protokoll.CountAsync(x => x.Aktion == "Bestellung neu zugewiesen"));
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
        Assert.Equal(9, await db.Mitarbeiter.CountAsync(x => x.Aktiv));
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
        Assert.Equal(9, await db.Mitarbeiter.CountAsync(x => x.Aktiv));
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
            Assert.Equal(9, await db.Mitarbeiter.CountAsync(x => x.Aktiv));
            Assert.Single(await db.Protokoll.ToListAsync(), x => x.Aktion == HbAdAbgleichService.AktionGestoppt);
        }
        Assert.True(await _abgleich.HeuteErledigtAsync());

        var vonHand = await _abgleich.AusfuehrenAsync(Admin);

        Assert.True(vonHand.Geschrieben);
        await using var db2 = _factory.CreateDbContext();
        // 9 aktive, 4 deaktiviert, 1 wieder aktiv.
        Assert.Equal(6, await db2.Mitarbeiter.CountAsync(x => x.Aktiv));
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

    private sealed class SucheFake(params HbAdPerson[] personen) : IHbAdSuche
    {
        public Task<IReadOnlyList<HbAdPerson>> SucheAsync(string? begriff) =>
            Task.FromResult<IReadOnlyList<HbAdPerson>>(personen.Where(p => p.Konto.StartsWith(begriff ?? "", StringComparison.OrdinalIgnoreCase)).ToList());
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
