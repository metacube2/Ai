namespace TrafagSalesExporter.Modules.Handbestellwesen;

// Handbestellwesen 2026-10-22 (Auftrag Ingo): Abloesung der PHP-Anwendung, Konzept docs/HANDBESTELLWESEN_KONZEPT_2026-10-22.md.
// Tabellen entsprechen fachlich denen der PHP-Anwendung, aber mit echten Datums- und Betragsspalten statt Text und "000".

public sealed class HbOptions
{
    public const string SectionName = "Handbestellwesen";

    /// <summary>Aus: kein Menuepunkt-Inhalt, Seite meldet "nicht aktiviert", keine Datenbank.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Leer = &lt;ContentRoot&gt;\hbdaten\handbestellwesen.db. Relative Pfade gelten ab ContentRoot.</summary>
    public string DatabasePath { get; set; } = string.Empty;

    /// <summary>Windows-Konten (ohne Domaene) mit Adminrecht, auch ohne Mitarbeitereintrag. Zum Einrichten.</summary>
    public List<string> AdminUsers { get; set; } = [];

    /// <summary>Tagesbackups so viele Tage behalten.</summary>
    public int BackupKeepDays { get; set; } = 60;

    /// <summary>Freigabe ueber dem Limit (Entscheid Ingo 2026-10-27): aus = wie PHP, jede aktive Person mit genuegend
    /// Limit darf freigeben; an = nur Personen derselben Kostenstelle (setzt gepflegte Kostenstellen voraus).</summary>
    public bool FreigabeNurGleicheKostenstelle { get; set; }
}

public enum HbStatus
{
    /// <summary>Bestellt, keine Freigabe noetig oder Freigabe erteilt; wartet auf Wareneingang.</summary>
    Offen = 0,
    /// <summary>Bestellwert ueber dem Limit des Bestellers, Freigabe steht aus.</summary>
    FreigabeOffen = 1,
    /// <summary>Ware eingegangen, entspricht dem Archiv der PHP-Anwendung.</summary>
    Eingegangen = 2,
    /// <summary>Geloescht (nur ohne Wareneingang), entspricht delete_archive.</summary>
    Geloescht = 3,
    /// <summary>Bereinigung 2026-10-22: alte offene Bestellung ohne gebuchten Wareneingang, administrativ abgeschlossen.</summary>
    Ausgebucht = 4
}

/// <summary>Vorschau der Bereinigung: Doppelklick-Doubletten und alte offene Bestellungen.</summary>
public sealed record HbBereinigung(
    List<(HbBestellung Behalten, HbBestellung Storno)> Doubletten,
    List<HbBestellung> AlteOffene,
    DateTime Stichtag);

public sealed class HbBestellung
{
    public int Id { get; set; }
    /// <summary>E + JJ + Kostenstelle + 4-stellige laufende Nummer, ohne Leerzeichen gespeichert.</summary>
    public string Bestellnummer { get; set; } = string.Empty;
    public string KreditorNr { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public decimal Bestellwert { get; set; }
    /// <summary>Altdaten: Text statt Betrag im PHP-Feld (z. B. "KV" fuer Kostenvoranschlag, "Service-Vertrag"); dann ist Bestellwert 0.</summary>
    public string? WertText { get; set; }
    public string BestellerKurzzeichen { get; set; } = string.Empty;
    public string? FreigeberKurzzeichen { get; set; }
    public string Kostenstelle { get; set; } = string.Empty;
    public string Sachkonto { get; set; } = string.Empty;
    public HbStatus Status { get; set; }
    public DateTime AngelegtAm { get; set; }
    public string AngelegtVon { get; set; } = string.Empty;
    public DateTime? FreigegebenAm { get; set; }
    public string? FreigegebenVon { get; set; }
    public DateTime? EingangAm { get; set; }
    public string? EingangVon { get; set; }
    public decimal? Rechnungsbetrag { get; set; }
    public DateTime? RechnungsDatum { get; set; }
    public DateTime? GeloeschtAm { get; set; }
    public string? GeloeschtVon { get; set; }
    /// <summary>id aus der PHP-Datenbank, nur bei uebernommenen Datensaetzen.</summary>
    public int? AltId { get; set; }
}

/// <summary>Zaehler je Jahr und Kostenstelle (ersetzt tab_bestellnummer mit seinem LIKE-Praefix).</summary>
public sealed class HbZaehler
{
    public int Jahr { get; set; }
    public string Kostenstelle { get; set; } = string.Empty;
    public int LetzteNummer { get; set; }
}

public sealed class HbKreditor
{
    public string Nr { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Strasse { get; set; } = string.Empty;
    public string Plz { get; set; } = string.Empty;
    public string Ort { get; set; } = string.Empty;
    public string Land { get; set; } = string.Empty;
}

public sealed class HbMitarbeiter
{
    public int Id { get; set; }
    public string Kurzzeichen { get; set; } = string.Empty;
    public string Vorname { get; set; } = string.Empty;
    public string Nachname { get; set; } = string.Empty;
    public string Mail { get; set; } = string.Empty;
    /// <summary>Windows-Konto ohne Domaene (z. B. "koi"). Verbindet die Anmeldung mit dem Kurzzeichen.</summary>
    public string Konto { get; set; } = string.Empty;
    /// <summary>Bestellberechtigung in CHF (bestellberechtiung der PHP-Anwendung).</summary>
    public decimal Limit { get; set; }
    public string Kostenstelle { get; set; } = string.Empty;
    public bool IstWareneingang { get; set; }
    public bool IstAdmin { get; set; }
    public bool Aktiv { get; set; } = true;

    public string Name => $"{Vorname} {Nachname}".Trim();
}

public sealed class HbKostenstelle
{
    public string Nr { get; set; } = string.Empty;
    public string Bezeichnung { get; set; } = string.Empty;
    /// <summary>Kurzzeichen des Kostenstellenleiters.</summary>
    public string Leiter { get; set; } = string.Empty;
}

public sealed class HbSachkonto
{
    public string Nr { get; set; } = string.Empty;
    public string Bezeichnung { get; set; } = string.Empty;
}

/// <summary>Wer hat wann was getan. Ersetzt das gemeinsame Basic-Auth-Login ohne Nachvollziehbarkeit.</summary>
public sealed class HbProtokoll
{
    public long Id { get; set; }
    public DateTime Zeit { get; set; }
    public string Benutzer { get; set; } = string.Empty;
    public string Aktion { get; set; } = string.Empty;
    public string Bezug { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

/// <summary>Angemeldeter Benutzer mit seinen Rechten im Handbestellwesen.</summary>
public sealed record HbBenutzer(string Konto, HbMitarbeiter? Mitarbeiter, bool IstAdmin)
{
    public bool IstWareneingang => IstAdmin || Mitarbeiter?.IstWareneingang == true;
    public string Kurzzeichen => Mitarbeiter?.Kurzzeichen ?? string.Empty;
}

public sealed class HbBestellEingabe
{
    public string KreditorNr { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public decimal? Bestellwert { get; set; }
    public string BestellerKurzzeichen { get; set; } = string.Empty;
    public string Kostenstelle { get; set; } = string.Empty;
    public string? FreigeberKurzzeichen { get; set; }
    public string Sachkonto { get; set; } = string.Empty;
}

/// <summary>Nutzung je Person oder Kostenstelle im Zeitraum (Entscheid 2026-10-22: weiterbetreiben, aber Nutzung festhalten).</summary>
public sealed record HbNutzungZeile(string Schluessel, string Name, int Bestellungen, decimal Summe, int Aufrufe, DateTime? Erste, DateTime? Letzte);

public sealed record HbNutzungMonat(string Monat, int Bestellungen, decimal Summe, int Personen);

public sealed record HbNutzung(
    DateTime Von, DateTime Bis, int Bestellungen, decimal Summe, int Personen,
    List<HbNutzungZeile> JePerson, List<HbNutzungZeile> JeKostenstelle, List<HbNutzungMonat> JeMonat);

/// <summary>Fachlicher Fehler mit Meldung fuer die Oberflaeche.</summary>
public sealed class HbException(string message) : Exception(message);
