using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace TrafagSalesExporter.Modules.Handbestellwesen;

/// <summary>
/// Fachlogik des Handbestellwesens. Alle Pruefungen laufen hier auf dem Server; die PHP-Anwendung pruefte das
/// Bestelllimit nur im Browser und liess Freigeben und Loeschen fuer jeden zu.
/// </summary>
public sealed class HbService(IDbContextFactory<HbDbContext> dbFactory, IOptionsMonitor<HbOptions> options, TimeProvider clock)
{
    // Nummernvergabe nacheinander je Datenbank; zusaetzlich schuetzt der eindeutige Index auf Bestellnummer.
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> WriteLocks = new(StringComparer.OrdinalIgnoreCase);

    private DateTime Jetzt => clock.GetLocalNow().DateTime;

    // ---------- Benutzer ----------

    /// <summary>Windows-Login ohne Domaene, klein geschrieben (TRAFAG\Koi -> koi).</summary>
    public static string KurzKonto(string? login)
    {
        var name = (login ?? string.Empty).Trim();
        var sep = name.LastIndexOf('\\');
        if (sep >= 0) name = name[(sep + 1)..];
        var at = name.IndexOf('@');
        if (at > 0) name = name[..at];
        return name.ToLowerInvariant();
    }

    public async Task<HbBenutzer> BenutzerAsync(string? login)
    {
        var konto = KurzKonto(login);
        await using var db = await dbFactory.CreateDbContextAsync();
        var ma = konto.Length == 0 ? null : await db.Mitarbeiter.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Aktiv && x.Konto.ToLower() == konto);
        var admin = options.CurrentValue.AdminUsers.Any(a => KurzKonto(a) == konto) || ma?.IstAdmin == true;
        return new HbBenutzer(konto, ma, admin && konto.Length > 0);
    }

    // ---------- Lesen ----------

    public async Task<List<HbBestellung>> BestellungenAsync(bool archiv, string? suche = null)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var q = db.Bestellungen.AsNoTracking();
        q = archiv
            ? q.Where(x => x.Status == HbStatus.Eingegangen || x.Status == HbStatus.Ausgebucht)
            : q.Where(x => x.Status == HbStatus.Offen || x.Status == HbStatus.FreigabeOffen);
        var liste = await q.OrderByDescending(x => x.Id).ToListAsync();
        if (string.IsNullOrWhiteSpace(suche))
            return liste;

        // Volltextsuche wie im PHP-Filter, aber Leerzeichen in der Bestellnummer duerfen mitgetippt werden.
        var s = suche.Trim();
        var ohneLeer = s.Replace(" ", string.Empty);
        bool Treffer(string? v) => !string.IsNullOrEmpty(v) && v.Contains(s, StringComparison.OrdinalIgnoreCase);
        return liste.Where(x =>
                x.Bestellnummer.Contains(ohneLeer, StringComparison.OrdinalIgnoreCase) ||
                Treffer(x.KreditorNr) || Treffer(x.Text) || Treffer(x.BestellerKurzzeichen) ||
                Treffer(x.FreigeberKurzzeichen) || Treffer(x.Kostenstelle) || Treffer(x.Sachkonto) || Treffer(x.WertText) ||
                Treffer(x.Bestellwert.ToString(CultureInfo.InvariantCulture)))
            .ToList();
    }

    public async Task<List<HbKreditor>> KreditorenAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Kreditoren.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<List<HbMitarbeiter>> MitarbeiterAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Mitarbeiter.AsNoTracking().OrderBy(x => x.Kurzzeichen).ToListAsync();
    }

    public async Task<List<HbKostenstelle>> KostenstellenAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Kostenstellen.AsNoTracking().OrderBy(x => x.Nr).ToListAsync();
    }

    public async Task<List<HbSachkonto>> SachkontenAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Sachkonten.AsNoTracking().OrderBy(x => x.Nr).ToListAsync();
    }

    /// <summary>Personen, die diesen Betrag freigeben duerfen. Standard wie PHP: jede aktive Person mit genuegend Limit,
    /// dieselbe Kostenstelle zuerst. Mit <see cref="HbOptions.FreigabeNurGleicheKostenstelle"/> nur dieselbe Kostenstelle
    /// (checkberechtigung.php). Entscheid Ingo 2026-10-27: 929 von 1'032 Bestellungen der letzten 12 Monate haetten mit der
    /// strengen Regel keinen Freigeber gefunden, weil nur 12 von 109 Mitarbeitenden eine Kostenstelle haben.</summary>
    public async Task<List<HbMitarbeiter>> FreigeberAsync(string kostenstelle, decimal bestellwert, string? ohneKurzzeichen = null)
    {
        var nurGleiche = options.CurrentValue.FreigabeNurGleicheKostenstelle;
        var alle = await MitarbeiterAsync();
        return alle.Where(x => x.Aktiv && x.Limit >= bestellwert && (!nurGleiche || x.Kostenstelle == kostenstelle) &&
                               !string.Equals(x.Kurzzeichen, ohneKurzzeichen, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Kostenstelle == kostenstelle ? 0 : 1)
            .ToList();
    }

    // ---------- Bestellnummer ----------

    /// <summary>E + JJ + Kostenstelle + 4-stellige Nummer, wie bisher.</summary>
    public static string BildeNummer(int jahr, string kostenstelle, int nummer)
        => $"E{jahr % 100:D2}{kostenstelle}{nummer:D4}";

    /// <summary>Anzeige mit Leerzeichen: E26 875 0012.</summary>
    public static string FormatNummer(string? nummer)
    {
        if (string.IsNullOrEmpty(nummer) || nummer.Length < 8)
            return nummer ?? string.Empty;
        return $"{nummer[..3]} {nummer[3..^4]} {nummer[^4..]}";
    }

    // ---------- Schreiben ----------

    public async Task<HbBestellung> AnlegenAsync(HbBenutzer benutzer, HbBestellEingabe e)
    {
        if (benutzer.Mitarbeiter is null && !benutzer.IstAdmin)
            throw new HbException($"Für das Konto „{benutzer.Konto}“ gibt es keinen Mitarbeitereintrag. Bitte beim Admin melden.");

        var text = (e.Text ?? string.Empty).Trim();
        if (text.Length == 0) throw new HbException("Bitte einen Bestelltext eingeben.");
        if (e.Bestellwert is not > 0) throw new HbException("Bitte einen Bestellwert grösser als 0 eingeben.");
        var wert = decimal.Round(e.Bestellwert.Value, 2);

        // Nur ein Admin darf fuer jemand anderen bestellen; sonst bestellt der Angemeldete selbst.
        var besteller = benutzer.IstAdmin && !string.IsNullOrWhiteSpace(e.BestellerKurzzeichen)
            ? e.BestellerKurzzeichen.Trim()
            : benutzer.Kurzzeichen;
        if (besteller.Length == 0) throw new HbException("Bitte ein Kurzzeichen wählen.");

        var sperre = WriteLocks.GetOrAdd(DbSchluessel(), _ => new SemaphoreSlim(1, 1));
        await sperre.WaitAsync();
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var ma = await db.Mitarbeiter.AsNoTracking().FirstOrDefaultAsync(x => x.Kurzzeichen == besteller && x.Aktiv)
                     ?? throw new HbException($"Kurzzeichen „{besteller}“ ist nicht erfasst oder nicht aktiv.");
            if (!await db.Kreditoren.AnyAsync(x => x.Nr == e.KreditorNr)) throw new HbException("Bitte einen Kreditor wählen.");
            if (!await db.Kostenstellen.AnyAsync(x => x.Nr == e.Kostenstelle)) throw new HbException("Bitte eine Kostenstelle wählen.");
            if (!await db.Sachkonten.AnyAsync(x => x.Nr == e.Sachkonto)) throw new HbException("Bitte ein Sachkonto wählen.");

            string? freigeber = null;
            if (wert > ma.Limit)
            {
                if (string.IsNullOrWhiteSpace(e.FreigeberKurzzeichen))
                    throw new HbException($"Der Bestellwert liegt über Ihrer Berechtigung von CHF {ma.Limit:N2}. Bitte eine berechtigte Person für die Freigabe wählen.");
                var berechtigte = await FreigeberAsync(e.Kostenstelle, wert, besteller);
                freigeber = berechtigte.FirstOrDefault(x => string.Equals(x.Kurzzeichen, e.FreigeberKurzzeichen, StringComparison.OrdinalIgnoreCase))?.Kurzzeichen
                            ?? throw new HbException("Die gewählte Person darf diesen Betrag für diese Kostenstelle nicht freigeben.");
            }

            var jetzt = Jetzt;
            await using var tx = await db.Database.BeginTransactionAsync();
            var z = await db.Zaehler.FirstOrDefaultAsync(x => x.Jahr == jetzt.Year && x.Kostenstelle == e.Kostenstelle);
            if (z is null)
            {
                z = new HbZaehler { Jahr = jetzt.Year, Kostenstelle = e.Kostenstelle, LetzteNummer = 0 };
                db.Zaehler.Add(z);
            }
            z.LetzteNummer++;

            var b = new HbBestellung
            {
                Bestellnummer = BildeNummer(jetzt.Year, e.Kostenstelle, z.LetzteNummer),
                KreditorNr = e.KreditorNr,
                Text = text,
                Bestellwert = wert,
                BestellerKurzzeichen = ma.Kurzzeichen,
                FreigeberKurzzeichen = freigeber,
                Kostenstelle = e.Kostenstelle,
                Sachkonto = e.Sachkonto,
                Status = freigeber is null ? HbStatus.Offen : HbStatus.FreigabeOffen,
                AngelegtAm = jetzt,
                AngelegtVon = benutzer.Konto
            };
            db.Bestellungen.Add(b);
            Protokolliere(db, benutzer, "Angelegt", b.Bestellnummer,
                $"Kreditor {b.KreditorNr}, CHF {b.Bestellwert.ToString("0.00", CultureInfo.InvariantCulture)}, Besteller {b.BestellerKurzzeichen}" +
                (freigeber is null ? string.Empty : $", Freigabe durch {freigeber}"));
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return b;
        }
        finally
        {
            sperre.Release();
        }
    }

    public Task FreigebenAsync(HbBenutzer benutzer, int id) => AendereAsync(benutzer, id, "Freigegeben", b =>
    {
        if (b.Status != HbStatus.FreigabeOffen) throw new HbException("Diese Bestellung wartet nicht auf eine Freigabe.");
        if (!benutzer.IstAdmin && !string.Equals(benutzer.Kurzzeichen, b.FreigeberKurzzeichen, StringComparison.OrdinalIgnoreCase))
            throw new HbException($"Nur {b.FreigeberKurzzeichen} kann diese Bestellung freigeben.");
        b.Status = HbStatus.Offen;
        b.FreigegebenAm = Jetzt;
        b.FreigegebenVon = benutzer.Konto;
    });

    public Task WareneingangAsync(HbBenutzer benutzer, int id) => AendereAsync(benutzer, id, "Ware eingegangen", b =>
    {
        if (b.Status == HbStatus.FreigabeOffen) throw new HbException("Die Bestellung ist noch nicht freigegeben.");
        if (b.Status != HbStatus.Offen) throw new HbException("Diese Bestellung ist nicht mehr offen.");
        if (!benutzer.IstWareneingang && !string.Equals(benutzer.Kurzzeichen, b.BestellerKurzzeichen, StringComparison.OrdinalIgnoreCase))
            throw new HbException("Den Wareneingang buchen kann der Besteller, der Wareneingang oder ein Admin.");
        b.Status = HbStatus.Eingegangen;
        b.EingangAm = Jetzt;
        b.EingangVon = benutzer.Konto;
    });

    public Task LoeschenAsync(HbBenutzer benutzer, int id) => AendereAsync(benutzer, id, "Gelöscht", b =>
    {
        if (b.Status is not (HbStatus.Offen or HbStatus.FreigabeOffen)) throw new HbException("Nur offene Bestellungen ohne Wareneingang können gelöscht werden.");
        if (!benutzer.IstAdmin && !string.Equals(benutzer.Kurzzeichen, b.BestellerKurzzeichen, StringComparison.OrdinalIgnoreCase))
            throw new HbException("Löschen kann nur der Besteller oder ein Admin.");
        b.Status = HbStatus.Geloescht;
        b.GeloeschtAm = Jetzt;
        b.GeloeschtVon = benutzer.Konto;
    });

    private async Task AendereAsync(HbBenutzer benutzer, int id, string aktion, Action<HbBestellung> aenderung)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var b = await db.Bestellungen.FirstOrDefaultAsync(x => x.Id == id) ?? throw new HbException("Bestellung nicht gefunden.");
        aenderung(b);
        Protokolliere(db, benutzer, aktion, b.Bestellnummer, string.Empty);
        await db.SaveChangesAsync();
    }

    // ---------- Nutzung (Entscheid 2026-10-22: weiterbetreiben, aber festhalten, wer es wann wie viel braucht) ----------

    public const string AktionAufruf = "Aufruf";

    /// <summary>Haelt fest, dass jemand das Handbestellwesen geoeffnet hat; hoechstens ein Eintrag je Konto und Tag.</summary>
    public async Task ProtokolliereAufrufAsync(HbBenutzer benutzer)
    {
        if (benutzer.Konto.Length == 0)
            return;
        var heute = Jetzt.Date;
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.Protokoll.AnyAsync(x => x.Aktion == AktionAufruf && x.Benutzer == benutzer.Konto && x.Zeit >= heute))
            return;
        Protokolliere(db, benutzer, AktionAufruf, string.Empty, benutzer.Kurzzeichen);
        await db.SaveChangesAsync();
    }

    /// <summary>Bestellungen (ohne geloeschte) und Aufrufe im Zeitraum [von, bis), je Person, Kostenstelle und Monat. Nur Admin.</summary>
    public async Task<HbNutzung> NutzungAsync(HbBenutzer benutzer, DateTime von, DateTime bis)
    {
        NurAdmin(benutzer);
        await using var db = await dbFactory.CreateDbContextAsync();
        var bestellungen = await db.Bestellungen.AsNoTracking()
            .Where(x => x.AngelegtAm >= von && x.AngelegtAm < bis && x.Status != HbStatus.Geloescht)
            .ToListAsync();
        var aufrufe = await db.Protokoll.AsNoTracking()
            .Where(x => x.Aktion == AktionAufruf && x.Zeit >= von && x.Zeit < bis)
            .ToListAsync();
        var mitarbeiter = await db.Mitarbeiter.AsNoTracking().ToListAsync();

        string NameVon(string kurz) => mitarbeiter.FirstOrDefault(m => m.Kurzzeichen == kurz)?.Name ?? string.Empty;
        string KurzVonKonto(string konto) => mitarbeiter.FirstOrDefault(m => m.Konto == konto)?.Kurzzeichen ?? konto;

        var aufrufeJeKurz = aufrufe.GroupBy(a => KurzVonKonto(a.Benutzer))
            .ToDictionary(g => g.Key, g => (Anzahl: g.Count(), Erste: g.Min(x => x.Zeit), Letzte: g.Max(x => x.Zeit)), StringComparer.OrdinalIgnoreCase);
        var bestellJeKurz = bestellungen.GroupBy(b => b.BestellerKurzzeichen, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var jePerson = bestellJeKurz.Keys.Union(aufrufeJeKurz.Keys, StringComparer.OrdinalIgnoreCase)
            .Select(kurz =>
            {
                var b = bestellJeKurz.GetValueOrDefault(kurz) ?? [];
                aufrufeJeKurz.TryGetValue(kurz, out var a);
                var zeiten = b.Select(x => x.AngelegtAm).ToList();
                if (a.Anzahl > 0) { zeiten.Add(a.Erste); zeiten.Add(a.Letzte); }
                return new HbNutzungZeile(kurz, NameVon(kurz), b.Count, b.Sum(x => x.Bestellwert), a.Anzahl,
                    zeiten.Count == 0 ? null : zeiten.Min(), zeiten.Count == 0 ? null : zeiten.Max());
            })
            .OrderByDescending(x => x.Bestellungen).ThenByDescending(x => x.Aufrufe).ThenBy(x => x.Schluessel)
            .ToList();

        var jeKost = bestellungen.GroupBy(b => b.Kostenstelle)
            .Select(g => new HbNutzungZeile(g.Key, string.Empty, g.Count(), g.Sum(x => x.Bestellwert), 0,
                g.Min(x => x.AngelegtAm), g.Max(x => x.AngelegtAm)))
            .OrderByDescending(x => x.Summe)
            .ToList();

        var jeMonat = bestellungen.GroupBy(b => b.AngelegtAm.ToString("yyyy-MM", CultureInfo.InvariantCulture))
            .Select(g => new HbNutzungMonat(g.Key, g.Count(), g.Sum(x => x.Bestellwert),
                g.Select(x => x.BestellerKurzzeichen).Distinct(StringComparer.OrdinalIgnoreCase).Count()))
            .OrderBy(x => x.Monat)
            .ToList();

        return new HbNutzung(von, bis, bestellungen.Count, bestellungen.Sum(x => x.Bestellwert),
            jePerson.Count(x => x.Bestellungen > 0), jePerson, jeKost, jeMonat);
    }

    // ---------- Bereinigung (Auftrag Ingo 2026-10-22: bereinige die Datensaetze) ----------

    /// <summary>
    /// Doppelklick-Doubletten: gleiche Bestellung (Besteller, Kreditor, Text, Wert, Wert-Text) innerhalb von 5 Minuten
    /// zweimal gespeichert, mit zwei Nummern. Behalten wird der weitere Stand (eingegangen vor offen), bei Gleichstand die
    /// fruehere. Alte offene: angelegt vor dem Stichtag und nie als eingegangen gebucht.
    /// </summary>
    public async Task<HbBereinigung> BereinigungVorschauAsync(HbBenutzer benutzer, DateTime stichtag)
    {
        NurAdmin(benutzer);
        await using var db = await dbFactory.CreateDbContextAsync();
        var aktiv = await db.Bestellungen.AsNoTracking()
            .Where(x => x.Status != HbStatus.Geloescht && x.Status != HbStatus.Ausgebucht).ToListAsync();
        var doubletten = new List<(HbBestellung, HbBestellung)>();
        var storniert = new HashSet<int>();
        foreach (var gruppe in aktiv.GroupBy(x => (x.BestellerKurzzeichen.ToLowerInvariant(), x.KreditorNr, x.Text.Trim().ToLowerInvariant(), x.Bestellwert, x.WertText ?? "")))
        {
            var liste = gruppe.OrderBy(x => x.AngelegtAm).ThenBy(x => x.Id).ToList();
            for (var i = 1; i < liste.Count; i++)
            {
                var a = liste[i - 1];
                var b = liste[i];
                if (storniert.Contains(a.Id) || (b.AngelegtAm - a.AngelegtAm).TotalMinutes > 5)
                    continue;
                var behalten = b.Status == HbStatus.Eingegangen && a.Status != HbStatus.Eingegangen ? b : a;
                var storno = ReferenceEquals(behalten, a) ? b : a;
                doubletten.Add((behalten, storno));
                storniert.Add(storno.Id);
            }
        }
        var alte = aktiv.Where(x => (x.Status == HbStatus.Offen || x.Status == HbStatus.FreigabeOffen) && x.AngelegtAm < stichtag && !storniert.Contains(x.Id))
            .OrderBy(x => x.AngelegtAm).ToList();
        return new HbBereinigung(doubletten, alte, stichtag);
    }

    public async Task<(int Doubletten, int Ausgebucht)> BereinigenAsync(HbBenutzer benutzer, DateTime stichtag, bool doubletten, bool alteOffene)
    {
        var vorschau = await BereinigungVorschauAsync(benutzer, stichtag);
        await using var db = await dbFactory.CreateDbContextAsync();
        var jetzt = Jetzt;
        var nD = 0;
        var nA = 0;
        if (doubletten)
        {
            foreach (var (behalten, storno) in vorschau.Doubletten)
            {
                var b = await db.Bestellungen.FirstAsync(x => x.Id == storno.Id);
                b.Status = HbStatus.Geloescht;
                b.GeloeschtAm = jetzt;
                b.GeloeschtVon = benutzer.Konto;
                Protokolliere(db, benutzer, "Bereinigung: Doublette storniert", b.Bestellnummer,
                    $"doppelt gespeichert zu {behalten.Bestellnummer}; vorheriger Status {storno.Status}");
                nD++;
            }
        }
        if (alteOffene)
        {
            foreach (var alt in vorschau.AlteOffene)
            {
                var b = await db.Bestellungen.FirstAsync(x => x.Id == alt.Id);
                b.Status = HbStatus.Ausgebucht;
                Protokolliere(db, benutzer, "Bereinigung: alte offene ausgebucht", b.Bestellnummer,
                    $"angelegt {b.AngelegtAm:dd.MM.yyyy}, Stichtag {stichtag:dd.MM.yyyy}, vorheriger Status {alt.Status}");
                nA++;
            }
        }
        await db.SaveChangesAsync();
        return (nD, nA);
    }

    // ---------- Stammdaten (nur Admin) ----------

    public async Task SpeichereKreditorAsync(HbBenutzer benutzer, HbKreditor k, bool neu)
    {
        NurAdmin(benutzer);
        k.Nr = k.Nr.Trim();
        if (k.Nr.Length == 0 || string.IsNullOrWhiteSpace(k.Name)) throw new HbException("Kreditor-Nr. und Name sind Pflicht.");
        await SpeichereAsync(benutzer, db => db.Kreditoren, k, neu, x => x.Nr == k.Nr, "Kreditor", k.Nr);
    }

    public async Task SpeichereKostenstelleAsync(HbBenutzer benutzer, HbKostenstelle k, bool neu)
    {
        NurAdmin(benutzer);
        k.Nr = k.Nr.Trim();
        if (k.Nr.Length == 0 || !k.Nr.All(char.IsAsciiDigit)) throw new HbException("Die Kostenstelle muss eine Nummer sein.");
        await SpeichereAsync(benutzer, db => db.Kostenstellen, k, neu, x => x.Nr == k.Nr, "Kostenstelle", k.Nr);
    }

    public async Task SpeichereSachkontoAsync(HbBenutzer benutzer, HbSachkonto s, bool neu)
    {
        NurAdmin(benutzer);
        s.Nr = s.Nr.Trim();
        if (s.Nr.Length == 0) throw new HbException("Die Sachkonto-Nr. ist Pflicht.");
        await SpeichereAsync(benutzer, db => db.Sachkonten, s, neu, x => x.Nr == s.Nr, "Sachkonto", s.Nr);
    }

    public async Task SpeichereMitarbeiterAsync(HbBenutzer benutzer, HbMitarbeiter m)
    {
        NurAdmin(benutzer);
        m.Kurzzeichen = m.Kurzzeichen.Trim();
        m.Konto = KurzKonto(m.Konto);
        if (m.Kurzzeichen.Length == 0 || string.IsNullOrWhiteSpace(m.Nachname)) throw new HbException("Kurzzeichen und Nachname sind Pflicht.");
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.Mitarbeiter.AnyAsync(x => x.Kurzzeichen == m.Kurzzeichen && x.Id != m.Id))
            throw new HbException($"Das Kurzzeichen „{m.Kurzzeichen}“ gibt es schon.");
        if (m.Konto.Length > 0 && await db.Mitarbeiter.AnyAsync(x => x.Konto == m.Konto && x.Id != m.Id))
            throw new HbException($"Das Windows-Konto „{m.Konto}“ ist schon einem anderen Kurzzeichen zugeordnet.");
        if (m.Id == 0) db.Mitarbeiter.Add(m); else db.Mitarbeiter.Update(m);
        Protokolliere(db, benutzer, m.Id == 0 ? "Mitarbeiter angelegt" : "Mitarbeiter geändert", m.Kurzzeichen,
            $"Limit CHF {m.Limit.ToString("0.00", CultureInfo.InvariantCulture)}, KoSt {m.Kostenstelle}, Konto {m.Konto}, aktiv {m.Aktiv}");
        await db.SaveChangesAsync();
    }

    // Loeschen (Wunsch Ingo 2026-10-27, wie im PHP). Physisch geloescht wird nur, was keine Bestellung (und bei der
    // Kostenstelle kein Nummernzaehler) mehr braucht; sonst blieben Bestellungen mit unbekanntem Kreditor, Besteller usw.
    // zurueck, wie die 50 offenen Bestellungen ohne Mitarbeitereintrag aus dem PHP. Mitarbeitende mit Bestellungen werden
    // stattdessen deaktiviert.

    public async Task LoescheKreditorAsync(HbBenutzer benutzer, string nr)
    {
        NurAdmin(benutzer);
        await using var db = await dbFactory.CreateDbContextAsync();
        var k = await db.Kreditoren.FirstOrDefaultAsync(x => x.Nr == nr) ?? throw new HbException($"Kreditor {nr} nicht gefunden.");
        var n = await db.Bestellungen.CountAsync(x => x.KreditorNr == nr);
        if (n > 0) throw new HbException($"Kreditor {nr} kommt in {n:N0} Bestellungen vor und kann nicht gelöscht werden.");
        db.Kreditoren.Remove(k);
        Protokolliere(db, benutzer, "Kreditor gelöscht", nr, k.Name);
        await db.SaveChangesAsync();
    }

    public async Task LoescheKostenstelleAsync(HbBenutzer benutzer, string nr)
    {
        NurAdmin(benutzer);
        await using var db = await dbFactory.CreateDbContextAsync();
        var k = await db.Kostenstellen.FirstOrDefaultAsync(x => x.Nr == nr) ?? throw new HbException($"Kostenstelle {nr} nicht gefunden.");
        var n = await db.Bestellungen.CountAsync(x => x.Kostenstelle == nr);
        if (n > 0) throw new HbException($"Kostenstelle {nr} kommt in {n:N0} Bestellungen vor und kann nicht gelöscht werden.");
        var ma = await db.Mitarbeiter.CountAsync(x => x.Kostenstelle == nr);
        if (ma > 0) throw new HbException($"Kostenstelle {nr} ist bei {ma} Mitarbeitenden eingetragen. Bitte dort zuerst ändern.");
        db.Kostenstellen.Remove(k);
        db.Zaehler.RemoveRange(db.Zaehler.Where(x => x.Kostenstelle == nr));
        Protokolliere(db, benutzer, "Kostenstelle gelöscht", nr, k.Bezeichnung);
        await db.SaveChangesAsync();
    }

    public async Task LoescheSachkontoAsync(HbBenutzer benutzer, string nr)
    {
        NurAdmin(benutzer);
        await using var db = await dbFactory.CreateDbContextAsync();
        var s = await db.Sachkonten.FirstOrDefaultAsync(x => x.Nr == nr) ?? throw new HbException($"Sachkonto {nr} nicht gefunden.");
        var n = await db.Bestellungen.CountAsync(x => x.Sachkonto == nr);
        if (n > 0) throw new HbException($"Sachkonto {nr} kommt in {n:N0} Bestellungen vor und kann nicht gelöscht werden.");
        db.Sachkonten.Remove(s);
        Protokolliere(db, benutzer, "Sachkonto gelöscht", nr, s.Bezeichnung);
        await db.SaveChangesAsync();
    }

    /// <summary>Ohne Bestellungen: loeschen. Mit Bestellungen: deaktivieren (bleibt fuer Archiv und Suche erhalten).
    /// Rueckgabe true = geloescht, false = deaktiviert.</summary>
    public async Task<bool> LoescheMitarbeiterAsync(HbBenutzer benutzer, int id)
    {
        NurAdmin(benutzer);
        await using var db = await dbFactory.CreateDbContextAsync();
        var m = await db.Mitarbeiter.FirstOrDefaultAsync(x => x.Id == id) ?? throw new HbException("Mitarbeiter nicht gefunden.");
        if (string.Equals(m.Konto, benutzer.Konto, StringComparison.OrdinalIgnoreCase))
            throw new HbException("Den eigenen Eintrag kann man nicht löschen.");
        var n = await db.Bestellungen.CountAsync(x => x.BestellerKurzzeichen == m.Kurzzeichen || x.FreigeberKurzzeichen == m.Kurzzeichen);
        if (n > 0)
        {
            m.Aktiv = false;
            Protokolliere(db, benutzer, "Mitarbeiter deaktiviert", m.Kurzzeichen, $"statt gelöscht, {n} Bestellungen");
            await db.SaveChangesAsync();
            return false;
        }
        db.Mitarbeiter.Remove(m);
        Protokolliere(db, benutzer, "Mitarbeiter gelöscht", m.Kurzzeichen, $"{m.Vorname} {m.Nachname}".Trim());
        await db.SaveChangesAsync();
        return true;
    }

    /// <summary>Vorlage fuer einen neuen Mitarbeitereintrag aus dem AD. Kurzzeichen = Windows-Konto (so sind alle 109
    /// Eintraege gepflegt, Stand 27.10.). Null, wenn das Konto schon eingetragen ist.</summary>
    public async Task<HbMitarbeiter?> MitarbeiterAusAdAsync(HbAdPerson p)
    {
        var konto = KurzKonto(p.Konto);
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.Mitarbeiter.AnyAsync(x => x.Konto == konto || x.Kurzzeichen == konto)) return null;
        return new HbMitarbeiter { Kurzzeichen = konto, Konto = konto, Vorname = p.Vorname, Nachname = p.Nachname.Length > 0 ? p.Nachname : p.Anzeige, Mail = p.Mail, Aktiv = true };
    }

    private async Task SpeichereAsync<T>(HbBenutzer benutzer, Func<HbDbContext, DbSet<T>> set, T eintrag, bool neu,
        System.Linq.Expressions.Expression<Func<T, bool>> gleich, string art, string nr) where T : class
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var vorhanden = await set(db).AnyAsync(gleich);
        if (neu && vorhanden) throw new HbException($"{art} {nr} gibt es schon.");
        if (!neu && !vorhanden) throw new HbException($"{art} {nr} nicht gefunden.");
        if (neu) set(db).Add(eintrag); else set(db).Update(eintrag);
        Protokolliere(db, benutzer, neu ? $"{art} angelegt" : $"{art} geändert", nr, string.Empty);
        await db.SaveChangesAsync();
    }

    private static void NurAdmin(HbBenutzer benutzer)
    {
        if (!benutzer.IstAdmin) throw new HbException("Stammdaten pflegen darf nur ein Admin.");
    }

    private void Protokolliere(HbDbContext db, HbBenutzer benutzer, string aktion, string bezug, string details)
        => db.Protokoll.Add(new HbProtokoll { Zeit = Jetzt, Benutzer = benutzer.Konto, Aktion = aktion, Bezug = bezug, Details = details });

    private string DbSchluessel() => options.CurrentValue.DatabasePath;
}
