using System.DirectoryServices;
using System.Globalization;
using System.Runtime.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace TrafagSalesExporter.Modules.Handbestellwesen;

// Handbestellwesen 2026-11-17 (Auftrag Ingo nach der Sitzung mit Oscar): naechtlicher Abgleich des Mitarbeiterverzeichnisses
// mit dem AD. Wer im AD gesperrt oder nicht (mehr) vorhanden ist, wird inaktiv; bei AD-Eintraegen werden Name und Mail
// nachgefuehrt. Rollen (Limit, Kostenstelle, Admin, Wareneingang) bleiben unberuehrt, geloescht wird nichts. Nach den Antworten
// Ingos vom selben Tag: abgelaufen = wie gesperrt, vom Abgleich Deaktivierte werden wieder aktiv, Altbestand mit aktivem AD-Konto
// wird AD-Eintrag. Das AD wird nur gelesen (DirectorySearcher), nie geschrieben. Konzept docs/HANDBESTELLWESEN_KONZEPT_2026-10-22.md 6.11/6.12.

public sealed class HbAdAbgleichOptions
{
    /// <summary>Nachtlauf an: liest das AD und schreibt das Ergebnis ins Protokoll.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Aus = der Nachtlauf aendert nichts und schreibt nur die Vorschau ins Protokoll (so vom 17.11. 11:28 bis zum
    /// Scharfschalten am selben Tag). Der Knopf „Jetzt ausfuehren“ eines Admins wirkt unabhaengig davon.</summary>
    public bool Schreiben { get; set; }

    /// <summary>Fruehester Start am Tag (Ortszeit, "HH:mm"). Ein verpasster Lauf wird am selben Tag nachgeholt.</summary>
    public string Uhrzeit { get; set; } = "02:30";

    /// <summary>Der Nachtlauf aendert nichts, wenn mehr Personen deaktiviert wuerden (Schutz gegen eine unvollstaendige
    /// AD-Antwort). Ein Admin kann nach Sicht der Vorschau von Hand ausfuehren.</summary>
    public int MaxDeaktivierungen { get; set; } = 10;

    /// <summary>Liefert das AD weniger Personenkonten, gilt die Abfrage als unvollstaendig: Abbruch ohne Aenderung.</summary>
    public int MinAdKonten { get; set; } = 50;
}

/// <summary>Personenkonto aus dem AD. Gesperrt = im AD deaktiviert (userAccountControl ACCOUNTDISABLE), nicht die
/// voruebergehende Sperre nach falschen Passwoertern.</summary>
public sealed record HbAdKonto(string Konto, string Vorname, string Nachname, string Mail, string Anzeige, bool Gesperrt, DateTime? AblaufUtc);

/// <summary>Ergebnis einer AD-Abfrage. Konten = null heisst: nicht lesbar (nie als „niemand im AD“ deuten).</summary>
public sealed record HbAdStand(IReadOnlyDictionary<string, HbAdKonto>? Konten, string? Fehler)
{
    public bool Ok => Konten is not null && Fehler is null;
}

public interface IHbAdVerzeichnis
{
    Task<HbAdStand> LadeAlleAsync(CancellationToken ct = default);
}

/// <summary>
/// Liest alle Personenkonten des AD, auch gesperrte, ohne Trefferbegrenzung (seitenweise). Bewusst nicht ueber
/// <see cref="HbAdSuche"/>: die liefert bei jedem Fehler eine leere Liste, hoechstens 15 Treffer und nur aktive Konten.
/// </summary>
public sealed class HbAdVerzeichnis(ILogger<HbAdVerzeichnis> logger) : IHbAdVerzeichnis
{
    // Groesster Wert, den DateTime.FromFileTimeUtc annimmt (31.12.9999).
    private const long MaxFileTime = 2650467743999999999;

    public async Task<HbAdStand> LadeAlleAsync(CancellationToken ct = default)
    {
        if (!OperatingSystem.IsWindows())
            return new HbAdStand(null, "Das AD ist nur unter Windows lesbar.");
        try
        {
            var konten = await Task.Run(() => OperatingSystem.IsWindows() ? Lade() : new Dictionary<string, HbAdKonto>(), ct)
                .WaitAsync(TimeSpan.FromSeconds(120), ct);
            return new HbAdStand(konten, null);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning("Handbestellwesen: AD-Abgleich, AD nicht lesbar: {Message}", ex.Message);
            return new HbAdStand(null, ex.Message);
        }
    }

    [SupportedOSPlatform("windows")]
    private static Dictionary<string, HbAdKonto> Lade()
    {
        using var root = new DirectoryEntry();
        using var searcher = new DirectorySearcher(root, "(&(objectCategory=person)(objectClass=user))",
            ["sAMAccountName", "givenName", "sn", "mail", "displayName", "userAccountControl", "accountExpires"])
        {
            PageSize = 1000,
            ClientTimeout = TimeSpan.FromSeconds(90),
            ServerTimeLimit = TimeSpan.FromSeconds(90)
        };
        var konten = new Dictionary<string, HbAdKonto>(StringComparer.OrdinalIgnoreCase);
        using var treffer = searcher.FindAll();
        foreach (SearchResult r in treffer)
        {
            string Wert(string name) => r.Properties[name].Count > 0 ? Convert.ToString(r.Properties[name][0], CultureInfo.InvariantCulture) ?? "" : "";
            var konto = Wert("sAMAccountName").Trim().ToLowerInvariant();
            if (konto.Length == 0)
                continue;
            var uac = r.Properties["userAccountControl"].Count > 0 ? Convert.ToInt32(r.Properties["userAccountControl"][0], CultureInfo.InvariantCulture) : 0;
            DateTime? ablauf = null;
            if (r.Properties["accountExpires"].Count > 0 && r.Properties["accountExpires"][0] is long a && a > 0 && a < MaxFileTime)
                ablauf = DateTime.FromFileTimeUtc(a);
            var anzeige = Wert("displayName");
            konten[konto] = new HbAdKonto(konto, Wert("givenName"), Wert("sn"), Wert("mail"), anzeige.Length > 0 ? anzeige : konto,
                Gesperrt: (uac & 0x2) != 0, ablauf);
        }
        return konten;
    }
}

public enum HbAdBefund
{
    /// <summary>Konto im AD deaktiviert.</summary>
    Gesperrt,
    /// <summary>Altbestand-Eintrag, dessen Konto im AD nicht vorkommt.</summary>
    NichtImAd,
    /// <summary>AD-Eintrag, dessen Konto im AD nicht mehr vorkommt (beim Erfassen war es aktiv im AD).</summary>
    ImAdGeloescht,
    /// <summary>Konto im AD abgelaufen (Ablaufdatum vorbei). Wie gesperrt behandelt (Entscheid Ingo 2026-11-17).</summary>
    Abgelaufen,
    /// <summary>AD-Eintrag mit geaendertem Vornamen, Nachnamen oder Mail im AD.</summary>
    NameGeaendert,
    /// <summary>Altbestand-Eintrag, dessen Konto im AD aktiv ist: wird AD-Eintrag (Entscheid Ingo 2026-11-17).</summary>
    AltbestandImAd,
    /// <summary>Vom Abgleich deaktiviert, Konto im AD wieder aktiv: wird wieder aktiv (Entscheid Ingo 2026-11-17).</summary>
    WiederAktiv
}

public enum HbAdAktion
{
    Keine,
    Deaktivieren,
    Reaktivieren,
    /// <summary>Name und Mail eines AD-Eintrags nachfuehren.</summary>
    Nachfuehren,
    /// <summary>Altbestand wird AD-Eintrag: Name und Mail aus dem AD, Kurzzeichen und Konto bleiben.</summary>
    Uebernehmen
}

public sealed record HbAdAbgleichPosten(
    int MitarbeiterId,
    string Kurzzeichen,
    string Name,
    bool AusAd,
    HbAdBefund Befund,
    HbAdAktion Aktion,
    string Details,
    int OffenAlsBesteller,
    int FreigabeOffenBei,
    string? NeuVorname = null,
    string? NeuNachname = null,
    string? NeuMail = null)
{
    public bool Deaktivieren => Aktion == HbAdAktion.Deaktivieren;

    public string BefundText => Befund switch
    {
        HbAdBefund.Gesperrt => "im AD gesperrt",
        HbAdBefund.NichtImAd => "nicht im AD",
        HbAdBefund.ImAdGeloescht => "im AD gelöscht",
        HbAdBefund.Abgelaufen => "im AD abgelaufen",
        HbAdBefund.NameGeaendert => "Name/Mail geändert",
        HbAdBefund.AltbestandImAd => "Altbestand, im AD aktiv",
        HbAdBefund.WiederAktiv => "im AD wieder aktiv",
        _ => Befund.ToString()
    };

    public string AktionText => Aktion switch
    {
        HbAdAktion.Deaktivieren => "wird inaktiv",
        HbAdAktion.Reaktivieren => "wird wieder aktiv",
        HbAdAktion.Nachfuehren => "Name/Mail nachführen",
        HbAdAktion.Uebernehmen => "wird AD-Eintrag",
        _ => "keine"
    };
}

public sealed record HbAdAbgleichPlan(DateTime Zeit, int AdKonten, int AktiveMitarbeiter, IReadOnlyList<HbAdAbgleichPosten> Posten, string? Fehler)
{
    public int Deaktivierungen => Posten.Count(p => p.Aktion == HbAdAktion.Deaktivieren);
    public int Reaktivierungen => Posten.Count(p => p.Aktion == HbAdAktion.Reaktivieren);
    public int Namensaenderungen => Posten.Count(p => p.Aktion == HbAdAktion.Nachfuehren);
    public int Uebernahmen => Posten.Count(p => p.Aktion == HbAdAktion.Uebernehmen);
    public int Aenderungen => Posten.Count(p => p.Aktion != HbAdAktion.Keine);

    public string Zusammenfassung =>
        $"{Deaktivierungen} zu deaktivieren, {Reaktivierungen} wieder zu aktivieren, {Uebernahmen} aus dem Altbestand zu übernehmen, " +
        $"{Namensaenderungen} Namen/Mail nachzuführen";
}

public sealed record HbAdAbgleichErgebnis(HbAdAbgleichPlan Plan, bool Geschrieben, string Meldung);

public sealed class HbAdAbgleichService(
    IDbContextFactory<HbDbContext> dbFactory,
    IHbAdVerzeichnis verzeichnis,
    IOptionsMonitor<HbOptions> options,
    TimeProvider clock,
    ILogger<HbAdAbgleichService> logger)
{
    public const string AktionLauf = "AD-Abgleich";
    public const string AktionVorschau = "AD-Abgleich Vorschau";
    public const string AktionGestoppt = "AD-Abgleich gestoppt";
    public const string AktionAbbruch = "AD-Abgleich abgebrochen";
    public const string SystemKonto = "ad-abgleich";

    private DateTime Jetzt => clock.GetLocalNow().DateTime;

    /// <summary>Was ein Abgleich jetzt aendern wuerde. Aendert nichts.</summary>
    public async Task<HbAdAbgleichPlan> VorschauAsync(HbBenutzer benutzer, CancellationToken ct = default)
    {
        NurAdmin(benutzer);
        return await PlanAsync(ct);
    }

    /// <summary>Abgleich von Hand durch einen Admin, nach Sicht der Vorschau. Die Grenze <see cref="HbAdAbgleichOptions.MaxDeaktivierungen"/>
    /// gilt hier nicht, die Pruefung der AD-Antwort schon. Wirkt unabhaengig von <see cref="HbAdAbgleichOptions.Schreiben"/>.</summary>
    public async Task<HbAdAbgleichErgebnis> AusfuehrenAsync(HbBenutzer benutzer, CancellationToken ct = default)
    {
        NurAdmin(benutzer);
        var plan = await PlanAsync(ct);
        return await SchreibeAsync(plan, benutzer.Konto, grenze: null, schreiben: true, ct);
    }

    /// <summary>Nachtlauf: mit Grenze, und nur wenn <see cref="HbAdAbgleichOptions.Schreiben"/> an ist.</summary>
    public async Task<HbAdAbgleichErgebnis> NachtlaufAsync(CancellationToken ct = default)
    {
        var o = options.CurrentValue.AdAbgleich;
        var plan = await PlanAsync(ct);
        return await SchreibeAsync(plan, SystemKonto, Math.Max(0, o.MaxDeaktivierungen), o.Schreiben, ct);
    }

    /// <summary>Die letzten Protokolleintraege des Abgleichs, neueste zuerst.</summary>
    public async Task<List<HbProtokoll>> LetzteLaeufeAsync(HbBenutzer benutzer, int anzahl = 10)
    {
        NurAdmin(benutzer);
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Protokoll.AsNoTracking()
            .Where(x => x.Aktion == AktionLauf || x.Aktion == AktionVorschau || x.Aktion == AktionGestoppt || x.Aktion == AktionAbbruch)
            .OrderByDescending(x => x.Zeit).ThenByDescending(x => x.Id)
            .Take(anzahl)
            .ToListAsync();
    }

    /// <summary>Ist heute schon ein Nachtlauf abgeschlossen (Vorschau, ausgefuehrt oder wegen der Grenze gestoppt)? Ein Abbruch
    /// wegen nicht lesbarem AD zaehlt nicht, der Lauf wird spaeter wiederholt.</summary>
    public async Task<bool> HeuteErledigtAsync(CancellationToken ct = default)
    {
        var heute = Jetzt.Date;
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Protokoll.AnyAsync(x => x.Zeit >= heute && x.Benutzer == SystemKonto &&
            (x.Aktion == AktionLauf || x.Aktion == AktionVorschau || x.Aktion == AktionGestoppt), ct);
    }

    internal async Task<HbAdAbgleichPlan> PlanAsync(CancellationToken ct)
    {
        var stand = await verzeichnis.LadeAlleAsync(ct);
        var minimum = Math.Max(1, options.CurrentValue.AdAbgleich.MinAdKonten);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var mitarbeiter = await db.Mitarbeiter.AsNoTracking().ToListAsync(ct);
        var aktive = mitarbeiter.Count(x => x.Aktiv);
        if (!stand.Ok)
            return new HbAdAbgleichPlan(Jetzt, 0, aktive, [], $"AD nicht lesbar: {stand.Fehler}");
        if (stand.Konten!.Count < minimum)
            return new HbAdAbgleichPlan(Jetzt, stand.Konten.Count, aktive, [],
                $"Das AD lieferte nur {stand.Konten.Count} Personenkonten (mindestens {minimum} erwartet), die Abfrage gilt als unvollständig.");
        var offene = await OffeneAsync(db, ct);
        return new HbAdAbgleichPlan(Jetzt, stand.Konten.Count, aktive, Plane(mitarbeiter, stand.Konten, offene, clock.GetUtcNow().UtcDateTime), null);
    }

    /// <summary>
    /// Regeln (Auftrag Ingo 2026-11-17, Antworten auf die Fragen am selben Tag), nur fuer Eintraege mit Konto:
    /// aktiv und Konto nicht im AD, gesperrt oder abgelaufen = deaktivieren (mit Vermerk); aktiv und AD-Eintrag mit anderem Namen
    /// oder anderer Mail = nachfuehren; aktiv und Altbestand mit aktivem AD-Konto = wird AD-Eintrag (Name und Mail aus dem AD);
    /// inaktiv mit Vermerk des Abgleichs und Konto im AD wieder aktiv = wieder aktiv. Kurzzeichen und Konto aendert der Abgleich
    /// nie, Bestellungen verweisen auf das Kurzzeichen. Das AD wird nur gelesen.
    /// </summary>
    public static List<HbAdAbgleichPosten> Plane(IEnumerable<HbMitarbeiter> mitarbeiter, IReadOnlyDictionary<string, HbAdKonto> ad,
        IReadOnlyDictionary<string, (int Besteller, int Freigeber)> offene, DateTime jetztUtc)
    {
        var posten = new List<HbAdAbgleichPosten>();
        foreach (var m in mitarbeiter.OrderBy(x => x.Kurzzeichen, StringComparer.OrdinalIgnoreCase))
        {
            var konto = HbService.KurzKonto(m.Konto);
            if (konto.Length == 0)
                continue;
            offene.TryGetValue(m.Kurzzeichen, out var o);
            ad.TryGetValue(konto, out var k);
            var abgelaufen = k?.AblaufUtc is { } ablauf && ablauf <= jetztUtc;
            var adAktiv = k is not null && !k.Gesperrt && !abgelaufen;

            HbAdAbgleichPosten Posten(HbAdBefund befund, HbAdAktion aktion, string details, bool mitNamen = false)
            {
                var p = new HbAdAbgleichPosten(m.Id, m.Kurzzeichen, m.Name, m.AusAd, befund, aktion, details, o.Besteller, o.Freigeber);
                return mitNamen && k is not null
                    ? p with { NeuVorname = k.Vorname, NeuNachname = Nachname(k), NeuMail = k.Mail }
                    : p;
            }

            if (!m.Aktiv)
            {
                if (m.AdDeaktiviert && adAktiv)
                    posten.Add(Posten(HbAdBefund.WiederAktiv, HbAdAktion.Reaktivieren, $"Konto {konto} ist im AD wieder aktiv.", mitNamen: true));
                continue;
            }
            if (k is null)
            {
                posten.Add(m.AusAd
                    ? Posten(HbAdBefund.ImAdGeloescht, HbAdAktion.Deaktivieren, $"Konto {konto} war im AD und ist dort nicht mehr vorhanden.")
                    : Posten(HbAdBefund.NichtImAd, HbAdAktion.Deaktivieren, $"Konto {konto} ist im AD nicht vorhanden."));
                continue;
            }
            if (k.Gesperrt)
            {
                posten.Add(Posten(HbAdBefund.Gesperrt, HbAdAktion.Deaktivieren, $"Konto {konto} ist im AD gesperrt."));
                continue;
            }
            if (abgelaufen)
            {
                posten.Add(Posten(HbAdBefund.Abgelaufen, HbAdAktion.Deaktivieren, $"Konto {konto} ist im AD seit {k.AblaufUtc:dd.MM.yyyy} abgelaufen."));
                continue;
            }
            var neu = $"{k.Vorname} {Nachname(k)}, {k.Mail}".Replace("  ", " ");
            if (!m.AusAd)
            {
                posten.Add(Posten(HbAdBefund.AltbestandImAd, HbAdAktion.Uebernehmen,
                    $"Altbestand wird AD-Eintrag: {m.Vorname} {m.Nachname}, {m.Mail} -> {neu}".Replace("  ", " "), mitNamen: true));
                continue;
            }
            if (!string.Equals(m.Vorname, k.Vorname, StringComparison.Ordinal) || !string.Equals(m.Nachname, Nachname(k), StringComparison.Ordinal) ||
                !string.Equals(m.Mail, k.Mail, StringComparison.OrdinalIgnoreCase))
                posten.Add(Posten(HbAdBefund.NameGeaendert, HbAdAktion.Nachfuehren,
                    $"{m.Vorname} {m.Nachname}, {m.Mail} -> {neu}".Replace("  ", " "), mitNamen: true));
        }
        return posten;
    }

    private static string Nachname(HbAdKonto k) => k.Nachname.Length > 0 ? k.Nachname : k.Anzeige;

    /// <summary>Offene Bestellungen je Kurzzeichen: als Besteller (offen oder Freigabe offen) und als gewaehlte Freigabeperson
    /// (Freigabe offen). Damit sieht der Admin, was an einer Person haengt, bevor sie inaktiv wird.</summary>
    private static async Task<Dictionary<string, (int Besteller, int Freigeber)>> OffeneAsync(HbDbContext db, CancellationToken ct)
    {
        var liste = await db.Bestellungen.AsNoTracking()
            .Where(x => x.Status == HbStatus.Offen || x.Status == HbStatus.FreigabeOffen)
            .Select(x => new { x.BestellerKurzzeichen, x.FreigeberKurzzeichen, x.Status })
            .ToListAsync(ct);
        var ergebnis = new Dictionary<string, (int Besteller, int Freigeber)>(StringComparer.OrdinalIgnoreCase);
        foreach (var b in liste)
        {
            if (!string.IsNullOrEmpty(b.BestellerKurzzeichen))
            {
                ergebnis.TryGetValue(b.BestellerKurzzeichen, out var e);
                ergebnis[b.BestellerKurzzeichen] = (e.Besteller + 1, e.Freigeber);
            }
            if (b.Status == HbStatus.FreigabeOffen && !string.IsNullOrEmpty(b.FreigeberKurzzeichen))
            {
                ergebnis.TryGetValue(b.FreigeberKurzzeichen, out var e);
                ergebnis[b.FreigeberKurzzeichen] = (e.Besteller, e.Freigeber + 1);
            }
        }
        return ergebnis;
    }

    private async Task<HbAdAbgleichErgebnis> SchreibeAsync(HbAdAbgleichPlan plan, string wer, int? grenze, bool schreiben, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        if (plan.Fehler is not null)
        {
            var abbruch = $"{plan.Fehler} Nichts geändert.";
            Protokoll(db, wer, AktionAbbruch, "", abbruch);
            await db.SaveChangesAsync(ct);
            logger.LogWarning("Handbestellwesen: {Meldung}", abbruch);
            return new HbAdAbgleichErgebnis(plan, false, abbruch);
        }

        var zusammenfassung = $"{plan.AdKonten} AD-Konten und {plan.AktiveMitarbeiter} aktive Mitarbeitende geprüft: {plan.Zusammenfassung}.";
        if (grenze is { } g && plan.Deaktivierungen > g)
        {
            var gestoppt = $"{zusammenfassung} Gestoppt: mehr als {g} Deaktivierungen, nichts geändert. Ein Admin kann nach Sicht der " +
                           "Vorschau von Hand ausführen.";
            Protokoll(db, wer, AktionGestoppt, "", Mit(gestoppt, plan));
            await db.SaveChangesAsync(ct);
            logger.LogWarning("Handbestellwesen: {Meldung}", gestoppt);
            return new HbAdAbgleichErgebnis(plan, false, gestoppt);
        }
        if (!schreiben)
        {
            var vorschau = $"{zusammenfassung} Nur Vorschau (Schreiben aus), nichts geändert.";
            Protokoll(db, wer, AktionVorschau, "", Mit(vorschau, plan));
            await db.SaveChangesAsync(ct);
            return new HbAdAbgleichErgebnis(plan, false, vorschau);
        }

        var ids = plan.Posten.Select(p => p.MitarbeiterId).ToHashSet();
        var eintraege = await db.Mitarbeiter.Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        int deaktiviert = 0, reaktiviert = 0, uebernommen = 0, nachgefuehrt = 0;
        foreach (var p in plan.Posten)
        {
            if (!eintraege.TryGetValue(p.MitarbeiterId, out var m))
                continue;
            switch (p.Aktion)
            {
                case HbAdAktion.Deaktivieren when m.Aktiv:
                    m.Aktiv = false;
                    m.AdDeaktiviert = true;
                    m.InaktivSeit = Jetzt;
                    m.InaktivGrund = $"AD-Abgleich: {p.BefundText}";
                    deaktiviert++;
                    var haengt = p.OffenAlsBesteller + p.FreigabeOffenBei > 0
                        ? $" Offen: {p.OffenAlsBesteller} als Besteller, {p.FreigabeOffenBei} Freigaben."
                        : "";
                    Protokoll(db, wer, "Mitarbeiter deaktiviert", m.Kurzzeichen, $"AD-Abgleich ({p.BefundText}): {p.Details}{haengt}");
                    break;
                case HbAdAktion.Reaktivieren when !m.Aktiv && m.AdDeaktiviert:
                    m.Aktiv = true;
                    m.AdDeaktiviert = false;
                    m.InaktivSeit = null;
                    m.InaktivGrund = null;
                    UebernimmNamen(m, p);
                    reaktiviert++;
                    Protokoll(db, wer, "Mitarbeiter reaktiviert", m.Kurzzeichen, $"AD-Abgleich: {p.Details}");
                    break;
                case HbAdAktion.Uebernehmen when m.Aktiv && !m.AusAd:
                    UebernimmNamen(m, p);
                    uebernommen++;
                    Protokoll(db, wer, "Mitarbeiter aus AD übernommen", m.Kurzzeichen, $"AD-Abgleich: {p.Details}");
                    break;
                case HbAdAktion.Nachfuehren when m.Aktiv && m.AusAd:
                    UebernimmNamen(m, p);
                    nachgefuehrt++;
                    Protokoll(db, wer, "Mitarbeiter aus AD aktualisiert", m.Kurzzeichen, $"AD-Abgleich: {p.Details}");
                    break;
            }
        }
        var meldung = $"{plan.AdKonten} AD-Konten und {plan.AktiveMitarbeiter} aktive Mitarbeitende geprüft: {deaktiviert} deaktiviert, " +
                      $"{reaktiviert} wieder aktiv, {uebernommen} aus dem Altbestand übernommen, {nachgefuehrt} Namen/Mail nachgeführt.";
        Protokoll(db, wer, AktionLauf, "", meldung);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Handbestellwesen: {Meldung}", meldung);
        return new HbAdAbgleichErgebnis(plan, true, meldung);
    }

    /// <summary>Name und Mail aus dem AD; der Eintrag gilt danach als AD-Eintrag. Kurzzeichen und Konto bleiben.</summary>
    private static void UebernimmNamen(HbMitarbeiter m, HbAdAbgleichPosten p)
    {
        if (p.NeuNachname is null)
            return;
        m.Vorname = p.NeuVorname ?? "";
        m.Nachname = p.NeuNachname;
        m.Mail = p.NeuMail ?? "";
        m.AusAd = true;
    }

    private static string Mit(string text, HbAdAbgleichPlan plan)
    {
        var liste = string.Join(", ", plan.Posten.Where(p => p.Aktion is HbAdAktion.Deaktivieren or HbAdAktion.Reaktivieren)
            .Select(p => $"{p.Kurzzeichen} ({p.BefundText})"));
        var gesamt = liste.Length > 0 ? $"{text} Betroffen: {liste}." : text;
        return gesamt.Length > 4000 ? gesamt[..4000] + " …" : gesamt;
    }

    private void Protokoll(HbDbContext db, string wer, string aktion, string bezug, string details)
        => db.Protokoll.Add(new HbProtokoll { Zeit = Jetzt, Benutzer = wer, Aktion = aktion, Bezug = bezug, Details = details });

    private static void NurAdmin(HbBenutzer benutzer)
    {
        if (!benutzer.IstAdmin) throw new HbException("Den AD-Abgleich darf nur ein Admin ausführen.");
    }
}

/// <summary>
/// Nachtlauf: prueft alle 15 Minuten, ob heute ab <see cref="HbAdAbgleichOptions.Uhrzeit"/> schon ein Abgleich abgeschlossen ist,
/// und holt einen verpassten Lauf nach (Server neu gestartet, Laptop-Muster wie ISS-017). Ist das AD nicht lesbar, folgt der
/// naechste Versuch fruehestens nach zwei Stunden.
/// </summary>
internal sealed class HbAdAbgleichJob(
    HbAdAbgleichService abgleich,
    IOptionsMonitor<HbOptions> options,
    TimeProvider clock,
    ILogger<HbAdAbgleichJob> logger) : BackgroundService
{
    private DateTime? _letzterVersuch;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Die Datenbank legt HbStartUndSicherung beim Start an; erst danach pruefen.
            await Task.Delay(TimeSpan.FromMinutes(2), clock, stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PruefeUndLaufeAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Handbestellwesen: AD-Abgleich fehlgeschlagen");
                }
                await Task.Delay(TimeSpan.FromMinutes(15), clock, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    /// <summary>true = ein Lauf wurde gestartet.</summary>
    internal async Task<bool> PruefeUndLaufeAsync(CancellationToken ct)
    {
        var o = options.CurrentValue;
        if (!o.Enabled || !o.AdAbgleich.Enabled)
            return false;
        var jetzt = clock.GetLocalNow().DateTime;
        if (!TimeSpan.TryParseExact(o.AdAbgleich.Uhrzeit, @"hh\:mm", CultureInfo.InvariantCulture, out var ab))
            ab = new TimeSpan(2, 30, 0);
        if (jetzt.TimeOfDay < ab)
            return false;
        if (_letzterVersuch is { } versuch && versuch.Date == jetzt.Date && jetzt - versuch < TimeSpan.FromHours(2))
            return false;
        if (await abgleich.HeuteErledigtAsync(ct))
            return false;
        _letzterVersuch = jetzt;
        var ergebnis = await abgleich.NachtlaufAsync(ct);
        logger.LogInformation("Handbestellwesen: Nachtlauf AD-Abgleich: {Meldung}", ergebnis.Meldung);
        return true;
    }
}
