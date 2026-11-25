namespace TrafagSalesExporter.Modules.Lieferantenbewertung;

// Lieferantenbewertung Paket 2 (2026-11-19, Auftrag Ingo, Entwurf V6 von Armin, Freigabe Oscar 12.11. fuer Eingaben im
// Dashboard): Einkauf und Qualitaet pflegen Bewertungen, Zertifikate, Massnahmen, Reviews/Audits und Dokument-Links.
// Schluessel ist die SAP-Lieferantennummer (EKKO-LIFNR), der Name wird zur Anzeige mitgespeichert.

/// <summary>Eine datierte Bewertung der sieben Themenfelder. Jederzeit neu erfassbar, die Uebersicht zeigt die juengste (Armin 18.11., E2).</summary>
public sealed class LbBewertung
{
    public int Id { get; set; }
    public string LieferantNr { get; set; } = "";
    public string LieferantName { get; set; } = "";
    public DateOnly Datum { get; set; }
    public string Verantwortlich { get; set; } = "";
    public string ErfasstVon { get; set; } = "";
    public DateTime ErfasstAm { get; set; }
    public List<LbBewertungZeile> Zeilen { get; set; } = [];
}

public sealed class LbBewertungZeile
{
    public int Id { get; set; }
    public int BewertungId { get; set; }
    public string Themenfeld { get; set; } = "";
    /// <summary>1, 2, 4 oder 5 wie im Supplier Rating Template; null = nicht beurteilt.</summary>
    public int? Note { get; set; }
    public string Feststellung { get; set; } = "";
}

public sealed class LbZertifikat : ILbEintrag
{
    public int Id { get; set; }
    public string LieferantNr { get; set; } = "";
    public string LieferantName { get; set; } = "";
    public string Typ { get; set; } = "";
    public string Nummer { get; set; } = "";
    public bool Vorhanden { get; set; } = true;
    public DateOnly? GueltigBis { get; set; }
    public string Link { get; set; } = "";
    public string Bemerkung { get; set; } = "";
    public string GeaendertVon { get; set; } = "";
    public DateTime GeaendertAm { get; set; }
}

public sealed class LbMassnahme : ILbEintrag
{
    public int Id { get; set; }
    public string LieferantNr { get; set; } = "";
    public string LieferantName { get; set; } = "";
    public string Massnahme { get; set; } = "";
    public string Verantwortung { get; set; } = "";
    public DateOnly? Termin { get; set; }
    public string Status { get; set; } = LbKatalog.MassnahmeOffen;
    public string GeaendertVon { get; set; } = "";
    public DateTime GeaendertAm { get; set; }
}

/// <summary>Business Review oder Audit (V6 Reiter Lieferantenentwicklung).</summary>
public sealed class LbTermin : ILbEintrag
{
    public int Id { get; set; }
    public string LieferantNr { get; set; } = "";
    public string LieferantName { get; set; } = "";
    public string Art { get; set; } = "";
    public DateOnly? Datum { get; set; }
    public string Status { get; set; } = "Geplant";
    public string Ergebnis { get; set; } = "";
    public string Bemerkung { get; set; } = "";
    public string GeaendertVon { get; set; } = "";
    public DateTime GeaendertAm { get; set; }
}

/// <summary>Dokument als Link auf die zentrale Ablage; das Dashboard speichert keine Dateien (V6 Reiter Dokumente).</summary>
public sealed class LbDokument : ILbEintrag
{
    public int Id { get; set; }
    public string LieferantNr { get; set; } = "";
    public string LieferantName { get; set; } = "";
    public string Typ { get; set; } = "";
    public DateOnly? Datum { get; set; }
    public string Beschreibung { get; set; } = "";
    public string Link { get; set; } = "";
    public string GeaendertVon { get; set; } = "";
    public DateTime GeaendertAm { get; set; }
}

public sealed record LbThemenfeld(string Name, string Unterkriterien);

public enum LbZertifikatAmpel { Gueltig, LaeuftAb, Abgelaufen, Fehlt }

/// <summary>Uebersichtszeile je Lieferant: juengste Bewertung, schlechtester Zertifikatsstand, offene Massnahmen.</summary>
public sealed record LbUebersichtZeile(
    string LieferantNr,
    string LieferantName,
    LbBewertung? JuengsteBewertung,
    int AnzahlBewertungen,
    LbZertifikatAmpel? Zertifikate,
    int OffeneMassnahmen,
    LbTermin? NaechsterTermin);

public static class LbKatalog
{
    public const string MassnahmeOffen = "Offen";

    /// <summary>Sieben Themenfelder aus dem Supplier Rating Template, wie in V6 (Armin 02.11.).</summary>
    public static readonly IReadOnlyList<LbThemenfeld> Themenfelder =
    [
        new("Technology", "T1.1 Zusammenarbeit; T1.2 Ansprechpartner; T1.3 ISO 27001; T2.1 Produkt-/Prozesskompetenz; T2.2 Problemlösung / After Sales"),
        new("Quality", "Q1.1 Lieferqualität; Q2.1 ISO 9001; Q2.2 QS-Prozesse und Dokumentation"),
        new("Responsiveness", "R1.1 Terminflexibilität; R1.2 Mengenflexibilität; R2.1 Zusatzleistungen"),
        new("Resilience & Reliability", "D1.1 Liefertreue; D1.2 proaktive Verzögerungsinformation; D1.3 angemessene Lieferzeiten"),
        new("Procurement Costs", "C1.1 Kostensenkungsbemühungen; C1.2 Kosten und Konditionen; C1.3 Kostentransparenz / Kalkulation"),
        new("Sustainability", "S1.1 ökologische; S1.2 wirtschaftliche; S1.3 soziale Nachhaltigkeit"),
        new("Environment & Compliance", "E1.1 ISO 14001; E1.2 REACH/RoHS; E1.3 CMRT/EMRT; E1.4/1.5 CO₂-Berichte; E2.1 Fragebogen; E2.2 CoC; E2.3 Hinweisgebersystem; E2.4 ISO 45001"),
    ];

    /// <summary>Skala ohne 3, wie im Template: 1 ungenuegend, 2 Mindestanforderung, 4 gut, 5 sehr gut.</summary>
    public static readonly IReadOnlyList<(int Wert, string Text)> Skala =
        [(1, "1 ungenügend"), (2, "2 Mindestanforderung"), (4, "4 gut"), (5, "5 sehr gut")];

    public static readonly IReadOnlyList<string> ZertifikatTypen =
        ["ISO 9001", "ISO 14001", "ISO 45001", "ISO 27001", "IATF 16949", "ISO 13485", "REACH/RoHS", "CMRT/EMRT", "CoC", "Andere"];

    public static readonly IReadOnlyList<string> MassnahmeStatus = [MassnahmeOffen, "In Arbeit", "Erledigt"];

    public static readonly IReadOnlyList<string> TerminArten = ["Business Review", "Lieferantenaudit", "Prozessaudit", "Follow-up"];

    public static readonly IReadOnlyList<string> TerminStatus = ["Geplant", "Durchgeführt", "Abgeschlossen"];

    /// <summary>Vierstufiges Auditergebnis (V6, angelehnt an VDA 6.3, keine offizielle Bewertung).</summary>
    public static readonly IReadOnlyList<string> AuditErgebnisse =
        ["Keine wesentlichen Abweichungen", "Verbesserungsbedarf", "Kritische Abweichungen", "Offen / noch nicht bewertet"];

    public static readonly IReadOnlyList<string> DokumentTypen =
        ["Zertifikat", "Auditbericht", "Business Review / Meetingbericht", "Notiz", "Q2 / Reklamation", "Massnahmen / Sonstiges"];

    /// <summary>Ampel wie V6: abgelaufen, hoechstens 90 Tage, gueltig, fehlt.</summary>
    public static LbZertifikatAmpel Ampel(LbZertifikat z, DateOnly heute)
    {
        if (!z.Vorhanden)
            return LbZertifikatAmpel.Fehlt;
        if (z.GueltigBis is not { } bis)
            return LbZertifikatAmpel.Gueltig;
        if (bis < heute)
            return LbZertifikatAmpel.Abgelaufen;
        return bis <= heute.AddDays(90) ? LbZertifikatAmpel.LaeuftAb : LbZertifikatAmpel.Gueltig;
    }

    /// <summary>Schlechteste Ampel ueber alle Zertifikate eines Lieferanten; null ohne Eintrag.</summary>
    public static LbZertifikatAmpel? SchlechtesteAmpel(IEnumerable<LbZertifikat> zertifikate, DateOnly heute)
    {
        LbZertifikatAmpel? schlechteste = null;
        foreach (var z in zertifikate)
        {
            var a = Ampel(z, heute);
            if (schlechteste is null || Rang(a) > Rang(schlechteste.Value))
                schlechteste = a;
        }
        return schlechteste;
    }

    private static int Rang(LbZertifikatAmpel a) => a switch
    {
        LbZertifikatAmpel.Gueltig => 0,
        LbZertifikatAmpel.LaeuftAb => 1,
        LbZertifikatAmpel.Fehlt => 2,
        _ => 3,
    };

    /// <summary>Juengste Bewertung: hoechstes Bewertungsdatum, bei Gleichstand die zuletzt erfasste.</summary>
    public static LbBewertung? Juengste(IEnumerable<LbBewertung> bewertungen)
        => bewertungen.OrderByDescending(b => b.Datum).ThenByDescending(b => b.ErfasstAm).ThenByDescending(b => b.Id).FirstOrDefault();

    /// <summary>Nur http(s) wird als Link angeboten; Netzwerk- und Laufwerkpfade blockiert der Browser von https aus.</summary>
    public static bool IstWebLink(string? link)
        => Uri.TryCreate((link ?? "").Trim(), UriKind.Absolute, out var uri)
           && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);

    // 2026-11-25: Explorer „Als Pfad kopieren“ liefert "C:\...". Mit Anfuehrungszeichen gespeichert, oeffnete der kopierte Pfad
    // im Explorer nichts. Umschliessende gerade, typografische und einfache Anfuehrungszeichen und Leerraum werden entfernt.
    private static readonly char[] LinkRand = [' ', '\t', '"', '\'', '„', '“', '”', '‚', '‘', '’', '«', '»'];

    public static string BereinigeLink(string? link) => (link ?? "").Trim(LinkRand);

    public static string NormalisiereNr(string? nr)
    {
        var t = (nr ?? "").Trim();
        // SAP liefert LIFNR teils mit fuehrenden Nullen; der Cache des Dashboards ohne. Einheitlich ohne fuehrende Nullen.
        return t.All(char.IsDigit) ? t.TrimStart('0') is { Length: > 0 } s ? s : "0" : t.ToUpperInvariant();
    }
}

/// <summary>Gemeinsame Felder der gepflegten Eintraege je Lieferant.</summary>
public interface ILbEintrag
{
    int Id { get; set; }
    string LieferantNr { get; set; }
    string LieferantName { get; set; }
    string GeaendertVon { get; set; }
    DateTime GeaendertAm { get; set; }
}
