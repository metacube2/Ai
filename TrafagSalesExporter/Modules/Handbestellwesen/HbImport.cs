using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace TrafagSalesExporter.Modules.Handbestellwesen;

/// <summary>
/// Uebernahme der Altdaten aus dem MySQL/MariaDB-Dump der PHP-Anwendung (HeidiSQL oder mysqldump, Datenbank
/// trag_bestellwesen). Liest nur die INSERT-Befehle der acht bekannten Tabellen; alles andere im Dump wird ignoriert.
/// Import nur in eine leere Datenbank (2026-10-22, Konzept Abschnitt 6.3).
/// </summary>
public static class HbDumpParser
{
    /// <summary>Tabellenname -> Zeilen als Spaltenname/Wert (Wert null bei SQL-NULL).</summary>
    public static Dictionary<string, List<Dictionary<string, string?>>> Parse(string sql)
    {
        var result = new Dictionary<string, List<Dictionary<string, string?>>>(StringComparer.OrdinalIgnoreCase);
        var pos = 0;
        while (true)
        {
            var start = sql.IndexOf("INSERT INTO `", pos, StringComparison.OrdinalIgnoreCase);
            if (start < 0)
                break;
            pos = start + "INSERT INTO `".Length;
            var nameEnd = sql.IndexOf('`', pos);
            var table = sql[pos..nameEnd];
            pos = nameEnd + 1;

            // Spaltenliste ( `a`, `b` ) ist bei HeidiSQL und mysqldump --complete-insert vorhanden.
            var columns = new List<string>();
            SkipWhite(sql, ref pos);
            if (sql[pos] == '(')
            {
                var close = sql.IndexOf(')', pos);
                columns = sql[(pos + 1)..close].Split(',').Select(c => c.Trim().Trim('`')).ToList();
                pos = close + 1;
            }
            SkipWhite(sql, ref pos);
            if (!sql.AsSpan(pos).StartsWith("VALUES", StringComparison.OrdinalIgnoreCase))
                continue;
            pos += "VALUES".Length;

            if (!result.TryGetValue(table, out var rows))
                result[table] = rows = [];
            while (true)
            {
                SkipWhite(sql, ref pos);
                if (pos >= sql.Length || sql[pos] != '(')
                    break;
                pos++;
                var values = ReadTuple(sql, ref pos);
                var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < values.Count; i++)
                    row[i < columns.Count ? columns[i] : $"c{i}"] = values[i];
                rows.Add(row);
                SkipWhite(sql, ref pos);
                if (pos < sql.Length && sql[pos] == ',')
                {
                    pos++;
                    continue;
                }
                break; // ';'
            }
        }
        return result;
    }

    private static void SkipWhite(string s, ref int pos)
    {
        while (pos < s.Length && char.IsWhiteSpace(s[pos]))
            pos++;
    }

    /// <summary>Liest Werte bis zur schliessenden Klammer. Strings in '...' mit MySQL-Backslash-Escapes.</summary>
    private static List<string?> ReadTuple(string s, ref int pos)
    {
        var values = new List<string?>();
        while (pos < s.Length)
        {
            SkipWhite(s, ref pos);
            if (s[pos] == '\'')
            {
                pos++;
                var sb = new StringBuilder();
                while (pos < s.Length)
                {
                    var c = s[pos++];
                    if (c == '\\' && pos < s.Length)
                    {
                        var e = s[pos++];
                        sb.Append(e switch { 'n' => '\n', 'r' => '\r', 't' => '\t', '0' => '\0', 'Z' => '\x1a', _ => e });
                    }
                    else if (c == '\'')
                    {
                        if (pos < s.Length && s[pos] == '\'') { sb.Append('\''); pos++; }
                        else break;
                    }
                    else sb.Append(c);
                }
                values.Add(sb.ToString());
            }
            else
            {
                var start = pos;
                while (pos < s.Length && s[pos] != ',' && s[pos] != ')')
                    pos++;
                var raw = s[start..pos].Trim();
                values.Add(raw.Equals("NULL", StringComparison.OrdinalIgnoreCase) ? null : raw);
            }
            SkipWhite(s, ref pos);
            if (pos < s.Length && s[pos] == ',') { pos++; continue; }
            if (pos < s.Length && s[pos] == ')') { pos++; break; }
        }
        return values;
    }
}

/// <summary>Ergebnis einer Pruefung oder eines Imports.</summary>
public sealed class HbImportBericht
{
    public Dictionary<string, int> QuellZeilen { get; } = new(StringComparer.OrdinalIgnoreCase);
    public int Kreditoren { get; set; }
    public int Kostenstellen { get; set; }
    public int Sachkonten { get; set; }
    public int Mitarbeiter { get; set; }
    public int Zaehler { get; set; }
    public int Offen { get; set; }
    public int FreigabeOffen { get; set; }
    public int Eingegangen { get; set; }
    public int Geloescht { get; set; }
    public List<string> Hinweise { get; } = [];
    public bool Importiert { get; set; }

    internal void Hinweis(string text, int anzahl)
    {
        if (anzahl > 0)
            Hinweise.Add($"{anzahl:N0} {text}");
    }
}

public sealed class HbImportService(IDbContextFactory<HbDbContext> dbFactory, TimeProvider clock)
{
    private static readonly string[] Tabellen =
        ["tab_offenebestellungen", "archive", "delete_archive", "tab_bestellnummer", "tab_kreditorenstamm", "kurzzeichen", "tab_kostenstelle", "tab_sachkonto"];

    /// <summary>Liest den Dump und rechnet die Uebernahme durch, ohne zu schreiben.</summary>
    public Task<HbImportBericht> PruefenAsync(string sql) => Task.FromResult(Umsetzen(sql).Bericht);

    /// <summary>Schreibt die Altdaten in eine LEERE Datenbank (keine Bestellungen, keine Mitarbeiter).</summary>
    public async Task<HbImportBericht> ImportierenAsync(HbBenutzer benutzer, string sql)
    {
        if (!benutzer.IstAdmin)
            throw new HbException("Den Import darf nur ein Admin ausfuehren.");
        var u = Umsetzen(sql);
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.Bestellungen.AnyAsync() || await db.Mitarbeiter.AnyAsync())
            throw new HbException("Die Datenbank enthaelt schon Bestellungen oder Mitarbeiter. Import nur in eine leere Datenbank.");

        await using var tx = await db.Database.BeginTransactionAsync();
        db.Kreditoren.AddRange(u.Kreditoren);
        db.Kostenstellen.AddRange(u.Kostenstellen);
        db.Sachkonten.AddRange(u.Sachkonten);
        db.Mitarbeiter.AddRange(u.Mitarbeiter);
        db.Zaehler.AddRange(u.Zaehler);
        db.Bestellungen.AddRange(u.Bestellungen);
        db.Protokoll.Add(new HbProtokoll
        {
            Zeit = clock.GetLocalNow().DateTime,
            Benutzer = benutzer.Konto,
            Aktion = "Import Altdaten",
            Details = $"{u.Bestellungen.Count} Bestellungen, {u.Mitarbeiter.Count} Mitarbeiter, {u.Kreditoren.Count} Kreditoren"
        });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        u.Bericht.Importiert = true;
        return u.Bericht;
    }

    private sealed record Umsetzung(
        HbImportBericht Bericht, List<HbKreditor> Kreditoren, List<HbKostenstelle> Kostenstellen, List<HbSachkonto> Sachkonten,
        List<HbMitarbeiter> Mitarbeiter, List<HbZaehler> Zaehler, List<HbBestellung> Bestellungen);

    private static Umsetzung Umsetzen(string sql)
    {
        var daten = HbDumpParser.Parse(sql);
        var bericht = new HbImportBericht();
        foreach (var t in Tabellen)
            bericht.QuellZeilen[t] = daten.TryGetValue(t, out var r) ? r.Count : 0;
        var fehlend = Tabellen.Where(t => bericht.QuellZeilen[t] == 0).ToList();
        if (fehlend.Count > 0)
            bericht.Hinweise.Add("Ohne Zeilen im Dump: " + string.Join(", ", fehlend));
        List<Dictionary<string, string?>> Rows(string t) => daten.TryGetValue(t, out var r) ? r : [];

        // Stammdaten: bei doppelten Schluesseln gewinnt die Zeile mit der hoechsten id.
        var kreditoren = Rows("tab_kreditorenstamm").OrderBy(r => Int(r, "id"))
            .Select(r => new HbKreditor { Nr = T(r, "kreditor"), Name = T(r, "name"), Strasse = T(r, "strasse"), Plz = T(r, "plz"), Ort = T(r, "ort"), Land = T(r, "land") })
            .Where(k => k.Nr.Length > 0).ToList();
        bericht.Hinweis("doppelte Kreditor-Nummern (letzter Stand uebernommen)", kreditoren.Count - kreditoren.Select(k => k.Nr).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        kreditoren = kreditoren.GroupBy(k => k.Nr, StringComparer.OrdinalIgnoreCase).Select(g => g.Last()).ToList();

        var kostenstellen = Rows("tab_kostenstelle").OrderBy(r => Int(r, "id"))
            .Select(r => new HbKostenstelle { Nr = T(r, "kostenstelle"), Bezeichnung = T(r, "bezeichnung"), Leiter = T(r, "kurzzeichen") })
            .Where(k => k.Nr.Length > 0).GroupBy(k => k.Nr, StringComparer.OrdinalIgnoreCase).Select(g => g.Last()).ToList();
        var sachkonten = Rows("tab_sachkonto").OrderBy(r => Int(r, "id"))
            .Select(r => new HbSachkonto { Nr = T(r, "sachkonto"), Bezeichnung = T(r, "bezeichnung") })
            .Where(s => s.Nr.Length > 0).GroupBy(s => s.Nr, StringComparer.OrdinalIgnoreCase).Select(g => g.Last()).ToList();

        // Mitarbeiter: Windows-Konto vorbelegt mit dem Kurzzeichen (Trafag-Konten sind die Kurzzeichen, z. B. koi).
        var mitarbeiter = Rows("kurzzeichen").OrderBy(r => Int(r, "id"))
            .Select(r => new HbMitarbeiter
            {
                Kurzzeichen = T(r, "kurzzeichen"),
                Vorname = T(r, "vorname"),
                Nachname = T(r, "nachname"),
                Mail = T(r, "mail"),
                Konto = T(r, "kurzzeichen").ToLowerInvariant(),
                Limit = Betrag(T(r, "bestellberechtiung")) ?? 0m,
                Kostenstelle = T(r, "kostenstelle"),
                Aktiv = true
            })
            .Where(m => m.Kurzzeichen.Length > 0).ToList();
        bericht.Hinweis("doppelte Kurzzeichen (letzter Stand uebernommen)", mitarbeiter.Count - mitarbeiter.Select(m => m.Kurzzeichen).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        mitarbeiter = mitarbeiter.GroupBy(m => m.Kurzzeichen, StringComparer.OrdinalIgnoreCase).Select(g => g.Last()).ToList();
        foreach (var m in mitarbeiter.Where(m => string.IsNullOrWhiteSpace(m.Nachname)))
            m.Nachname = m.Kurzzeichen;

        // Zaehler aus tab_bestellnummer: "E" + JJ + Kostenstelle, nr = letzte vergebene Nummer.
        var zaehler = new Dictionary<(int, string), int>();
        var zaehlerUnlesbar = 0;
        foreach (var r in Rows("tab_bestellnummer"))
        {
            var praefix = T(r, "bestellnummer");
            if (praefix.Length < 4 || praefix[0] != 'E' || !int.TryParse(praefix.AsSpan(1, 2), out var jj))
            {
                zaehlerUnlesbar++;
                continue;
            }
            var key = (2000 + jj, praefix[3..]);
            zaehler[key] = Math.Max(zaehler.GetValueOrDefault(key), Int(r, "nr"));
        }
        bericht.Hinweis("Zaehler mit unlesbarem Praefix uebersprungen", zaehlerUnlesbar);

        // Bestellungen: Archiv zuerst (Wareneingang ist der endgueltige Stand), dann offene, dann geloeschte.
        var bestellungen = new List<HbBestellung>();
        var nummern = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var doppelt = 0;
        var ohneDatum = 0;
        var betragUnlesbar = 0;
        void Hinzu(HbBestellung b)
        {
            if (b.Bestellnummer.Length == 0 || !nummern.Add(b.Bestellnummer)) { doppelt++; return; }
            bestellungen.Add(b);
        }
        HbBestellung Basis(Dictionary<string, string?> r, HbStatus status)
        {
            var (besteller, freigeber) = Kurzzeichen(T(r, "kurzzeichen"));
            var wertText = T(r, "bestellwert");
            var wert = Betrag(wertText);
            if (wert is null && wertText.Length > 0) betragUnlesbar++;
            var wertAlsText = wert is null && wertText.Length > 0 ? wertText : null;
            var angelegt = Datum(T(r, "speicherdatum"));
            if (angelegt is null) ohneDatum++;
            var freigabe = Datum(T(r, "bestellfreigabe"));
            var rechnung = Betrag(T(r, "rechnungsbetrag"));
            return new HbBestellung
            {
                Bestellnummer = T(r, "bestellnummer").Replace(" ", string.Empty),
                KreditorNr = T(r, "kreditor"),
                Text = T(r, "bestelltext"),
                Bestellwert = wert ?? 0m,
                WertText = wertAlsText,
                BestellerKurzzeichen = besteller,
                FreigeberKurzzeichen = freigeber,
                Kostenstelle = T(r, "kostenstelle"),
                Sachkonto = T(r, "sachkonto"),
                Status = status,
                AngelegtAm = angelegt ?? new DateTime(2000, 1, 1),
                AngelegtVon = "import",
                FreigegebenAm = freigabe,
                FreigegebenVon = freigabe is null ? null : freigeber,
                Rechnungsbetrag = rechnung is > 0 ? rechnung : null,
                RechnungsDatum = rechnung is > 0 ? Datum(T(r, "rechnungsdatum")) : null,
                AltId = Int(r, "id")
            };
        }

        foreach (var r in Rows("archive"))
        {
            var b = Basis(r, HbStatus.Eingegangen);
            b.EingangAm = Datum(T(r, "wareneingang")) ?? Datum(T(r, "archivedatum"));
            b.EingangVon = "import";
            // 2026-10-22: erst nach dem Doubletten-Filter zaehlen; vorher zeigte "Pruefen" 26 Eingaenge zu viel.
            var vorher = bestellungen.Count;
            Hinzu(b);
            if (bestellungen.Count > vorher) bericht.Eingegangen++;
        }
        foreach (var r in Rows("tab_offenebestellungen"))
        {
            var eingang = Datum(T(r, "wareneingang"));
            var b = Basis(r, HbStatus.Offen);
            if (eingang is not null)
            {
                // Wareneingang gebucht, aber nie ins Archiv verschoben (offenebestellungen_nichtverschoben.php).
                b.Status = HbStatus.Eingegangen;
                b.EingangAm = eingang;
                b.EingangVon = "import";
            }
            else if (b.FreigeberKurzzeichen is not null && b.FreigegebenAm is null)
            {
                b.Status = HbStatus.FreigabeOffen;
            }
            var vorher = bestellungen.Count;
            Hinzu(b);
            if (bestellungen.Count > vorher)
            {
                if (b.Status == HbStatus.Offen) bericht.Offen++;
                else if (b.Status == HbStatus.FreigabeOffen) bericht.FreigabeOffen++;
                else bericht.Eingegangen++;
            }
        }
        foreach (var r in Rows("delete_archive"))
        {
            var b = Basis(r, HbStatus.Geloescht);
            b.GeloeschtAm = Datum(T(r, "deletedatum"));
            b.GeloeschtVon = "import";
            var vorher = bestellungen.Count;
            Hinzu(b);
            if (bestellungen.Count > vorher) bericht.Geloescht++;
        }

        // Zaehler nie kleiner als die hoechste uebernommene Nummer, sonst gaebe es Doubletten.
        foreach (var b in bestellungen)
        {
            if (b.Bestellnummer.Length < 8 || !int.TryParse(b.Bestellnummer.AsSpan(1, 2), out var jj) || !int.TryParse(b.Bestellnummer.AsSpan(b.Bestellnummer.Length - 4), out var nr))
                continue;
            var key = (2000 + jj, b.Bestellnummer[3..^4]);
            zaehler[key] = Math.Max(zaehler.GetValueOrDefault(key), nr);
        }

        bericht.Hinweis("Bestellnummern doppelt oder leer (nur der erste Stand uebernommen, Archiv vor offen vor geloescht)", doppelt);
        bericht.Hinweis("Bestellungen ohne lesbares Speicherdatum (als 01.01.2000 uebernommen)", ohneDatum);
        bericht.Hinweis("Bestellwerte als Text statt Betrag (z. B. KV, Service-Vertrag; Text bleibt erhalten, Betrag 0)", betragUnlesbar);
        var kreditorNr = kreditoren.Select(k => k.Nr).ToHashSet(StringComparer.OrdinalIgnoreCase);
        bericht.Hinweis("Bestellungen ohne Kreditor (im PHP nicht gewaehlt)", bestellungen.Count(b => b.KreditorNr.Length == 0));
        bericht.Hinweis("Bestellungen mit Kreditor, der nicht im Kreditorenstamm steht", bestellungen.Count(b => b.KreditorNr.Length > 0 && !kreditorNr.Contains(b.KreditorNr)));
        var kurz = mitarbeiter.Select(m => m.Kurzzeichen).ToHashSet(StringComparer.OrdinalIgnoreCase);
        bericht.Hinweis("offene Bestellungen mit Besteller ohne Mitarbeitereintrag", bestellungen.Count(b => b.Status is HbStatus.Offen or HbStatus.FreigabeOffen && !kurz.Contains(b.BestellerKurzzeichen)));
        bericht.Hinweis("offene Bestellungen aelter als ein Jahr", bestellungen.Count(b => b.Status is HbStatus.Offen or HbStatus.FreigabeOffen && b.AngelegtAm < DateTime.Today.AddYears(-1)));

        bericht.Kreditoren = kreditoren.Count;
        bericht.Kostenstellen = kostenstellen.Count;
        bericht.Sachkonten = sachkonten.Count;
        bericht.Mitarbeiter = mitarbeiter.Count;
        bericht.Zaehler = zaehler.Count;
        var zaehlerListe = zaehler.Select(z => new HbZaehler { Jahr = z.Key.Item1, Kostenstelle = z.Key.Item2, LetzteNummer = z.Value }).ToList();
        return new Umsetzung(bericht, kreditoren, kostenstellen, sachkonten, mitarbeiter, zaehlerListe, bestellungen);
    }

    // ---------- Umwandlung der PHP-Textwerte ----------

    private static string T(Dictionary<string, string?> r, string col)
    {
        var v = WebUtility.HtmlDecode(r.TryGetValue(col, out var raw) ? raw ?? string.Empty : string.Empty).Trim();
        // Im PHP-Formular hatte die erste <option> keinen value; wer nichts waehlte, speicherte den Platzhaltertext.
        return Platzhalter.Contains(v) ? string.Empty : v;
    }

    private static readonly HashSet<string> Platzhalter = new(StringComparer.OrdinalIgnoreCase)
        { "Kreditor", "Kostenstelle", "Sachkonto", "Kurzzeichen", "Berechtigte Person" };

    private static int Int(Dictionary<string, string?> r, string col)
        => int.TryParse(T(r, col), NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : 0;

    /// <summary>Besteller und Freigeber aus "abc," oder "abc,def".</summary>
    internal static (string Besteller, string? Freigeber) Kurzzeichen(string text)
    {
        var teile = text.Split(',', StringSplitOptions.TrimEntries);
        var freigeber = teile.Length > 1 && teile[1].Length > 0 ? teile[1] : null;
        return (teile[0], freigeber);
    }

    /// <summary>"000", "0", "---" und leer heissen "nicht gesetzt".</summary>
    internal static bool Leer(string text) => text.Length == 0 || text.Trim('0', '-', ' ').Length == 0;

    internal static decimal? Betrag(string text)
    {
        if (Leer(text)) return text == "0" ? 0m : null;
        var t = text.Replace("'", "").Replace("’", "").Replace(" ", "").Replace("CHF", "", StringComparison.OrdinalIgnoreCase).Replace("Fr.", "", StringComparison.OrdinalIgnoreCase).TrimEnd('-', '.');
        if (t.Contains(',') && !t.Contains('.')) t = t.Replace(',', '.');
        else t = t.Replace(",", "");
        return decimal.TryParse(t, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : null;
    }

    private static readonly string[] DatumsFormate =
    [
        "dddd dd MMMM yyyy HH:mm:ss", "dddd d MMMM yyyy HH:mm:ss", "dd.MM.yyyy HH:mm:ss", "d.M.yyyy HH:mm:ss", "dd.MM.yyyy",
        "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd"
    ];

    /// <summary>Englische PHP-Datumstexte ("Monday 05 January 2015 15:23:11") und deutsche Formate.</summary>
    internal static DateTime? Datum(string text)
    {
        if (Leer(text)) return null;
        return DateTime.TryParseExact(text.Trim(), DatumsFormate, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var d) ? d : null;
    }
}
