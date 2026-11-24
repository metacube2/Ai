using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Data.Sqlite;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Erzeugt die Mappe <c>Finance_All</c>: eine Zeile je Buchungszeile aus dem Hauptbuch aller
/// Gesellschaften.
///
/// Das ist die Portierung von <c>Tools/FinanceAll/finance_all_xlsx.py</c> nach C#, damit die
/// Datei im Nachtlauf der Anwendung entsteht statt von Hand. **Das Python-Werkzeug bleibt
/// bestehen** und ist weiterhin der Weg fuer gekuerzte Ad-hoc-Auszuege (<c>--tage</c>,
/// <c>--seit</c>) und die Referenz, gegen die diese Umsetzung gegengerechnet wird.
///
/// Warum <see cref="OpenXmlWriter"/> und nicht ClosedXML, das die Anwendung sonst ueberall
/// verwendet: Das Blatt <c>Journal Detail</c> hat rund 470'000 Zeilen mal 26 Spalten. ClosedXML
/// baut die vollstaendige Mappe im Arbeitsspeicher auf, bevor es sie schreibt; das sind rund
/// zwoelf Millionen Zellobjekte und damit mehrere Gigabyte im IIS-Worker. Der Writer hier
/// stroemt die Zeilen dagegen direkt in die Datei und haelt nie mehr als eine Zeile.
///
/// Zeichenketten werden bewusst als <c>inlineStr</c> geschrieben statt ueber eine gemeinsame
/// Zeichenkettentabelle. Eine solche Tabelle muesste alle eindeutigen Werte im Speicher halten,
/// und die Spalte <c>entry text</c> ist freier Text mit hoher Streuung. Die Datei wird dadurch
/// etwas groesser, der Speicherbedarf bleibt aber unabhaengig von der Zeilenzahl.
/// </summary>
public static class FinanceAllWorkbookWriter
{
    private const string KopfFarbe = "FF1F4E79";
    private const string WarnFarbe = "FFFFF2CC";

    // Formatindizes aus BuildStylesheet, in derselben Reihenfolge.
    private const uint StilStandard = 0;
    private const uint StilKopf = 1;
    private const uint StilTitel = 2;
    private const uint StilKlein = 3;
    private const uint StilUeberschrift = 4;
    private const uint StilWarnung = 5;
    private const uint StilFliesstext = 6;

    private static readonly string[] SpaltenBuchstaben = Enumerable
        .Range(0, 40)
        .Select(SpaltenName)
        .ToArray();

    public sealed record Ergebnis(long Buchungszeilen, long Bytes, string Pfad);

    /// <summary>
    /// Schreibt die Mappe nach <paramref name="zielPfad"/>.
    /// </summary>
    /// <param name="datenbankPfad">Pfad zur SQLite-Datei. Wird ausschliesslich lesend geoeffnet.</param>
    /// <param name="seit">
    /// Begrenzt NUR das Blatt <c>Journal Detail</c> auf Buchungen ab diesem Datum. Summen,
    /// Konten, Datenstatus und Feldstatus bleiben vollstaendig, genau wie im Python-Werkzeug.
    /// </param>
    public static Ergebnis Schreibe(string datenbankPfad, string zielPfad, DateOnly? seit = null)
    {
        var verzeichnis = Path.GetDirectoryName(Path.GetFullPath(zielPfad));
        if (!string.IsNullOrEmpty(verzeichnis))
            Directory.CreateDirectory(verzeichnis);

        var verbindung = new SqliteConnectionStringBuilder
        {
            DataSource = datenbankPfad,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString();

        using var con = new SqliteConnection(verbindung);
        con.Open();

        var journalSpalten = LiesSpaltennamen(con, "FinancialJournalEntries");

        // Alte Produktivstaende bleiben lesbar, bis Schema und Journalimport erneuert sind.
        // Dieselbe Absicherung wie spalte() im Python-Werkzeug.
        string Spalte(string name, string fehlt = "NULL")
            => journalSpalten.Contains(name) ? name : fehlt;

        var dueSql = Spalte("DueDate");
        var clearingSql = Spalte("ClearingDate");
        var clearingRefSql = Spalte("ClearingReference");
        var reconSql = Spalte("ReconciliationDate");
        var clearingAnzahlSql = Spalte("ClearingCount", "0");
        var clearingStornoSql = Spalte("IsClearingCancelled", "0");

        var seitText = seit?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        using var doc = SpreadsheetDocument.Create(zielPfad, SpreadsheetDocumentType.Workbook);
        var wbPart = doc.AddWorkbookPart();

        var stylesPart = wbPart.AddNewPart<WorkbookStylesPart>();
        stylesPart.Stylesheet = BaueStylesheet();
        stylesPart.Stylesheet.Save();

        var blaetter = new List<(string Name, string Id)>();

        blaetter.Add(SchreibeLesehinweis(wbPart, seitText));
        var anzahl = SchreibeJournalDetail(
            wbPart, con, seitText, dueSql, clearingSql, reconSql,
            clearingRefSql, clearingAnzahlSql, clearingStornoSql);
        blaetter.Add(anzahl.Blatt);
        blaetter.Add(SchreibeJournalSummary(wbPart, con));
        blaetter.Add(SchreibeKonten(wbPart, con));
        blaetter.Add(SchreibeDatenstatus(wbPart, con));
        blaetter.Add(SchreibeFeldstatus(
            wbPart, con, dueSql, clearingSql, reconSql,
            clearingRefSql, clearingAnzahlSql, clearingStornoSql));

        var workbook = new Workbook();
        var sheets = new Sheets();
        uint sheetId = 1;
        foreach (var (name, id) in blaetter)
            sheets.Append(new Sheet { Name = name, SheetId = sheetId++, Id = id });
        workbook.Append(sheets);
        wbPart.Workbook = workbook;
        wbPart.Workbook.Save();

        doc.Dispose();

        var laenge = new FileInfo(zielPfad).Length;
        return new Ergebnis(anzahl.Zeilen, laenge, zielPfad);
    }

    private static HashSet<string> LiesSpaltennamen(SqliteConnection con, string tabelle)
    {
        var namen = new HashSet<string>(StringComparer.Ordinal);
        using var cmd = con.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({tabelle})";
        using var leser = cmd.ExecuteReader();
        while (leser.Read())
            namen.Add(leser.GetString(1));
        return namen;
    }

    // --- Lesehinweis --------------------------------------------------------
    // Der Text ist bewusst wortgleich zum Python-Werkzeug. Wer ihn hier aendert, muss ihn
    // dort ebenfalls aendern, sonst widersprechen sich zwei Mappen desselben Namens.
    private static (string, string) SchreibeLesehinweis(WorkbookPart wbPart, string? seit)
    {
        var part = wbPart.AddNewPart<WorksheetPart>();
        using (var w = OpenXmlWriter.Create(part))
        {
            w.WriteStartElement(new Worksheet());
            SchreibeSpaltenbreiten(w, new double[] { 118 });
            w.WriteStartElement(new SheetData());

            void Zeile(string text, uint stil = StilFliesstext)
                => SchreibeTextzeile(w, text, stil);

            Zeile("Finance_All - Hauptbuch aller Gesellschaften", StilTitel);
            Zeile($"Stand {DateTime.Now.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture)}, " +
                  "gelesen aus der zentralen Datenbank", StilKlein);
            Zeile("");
            Zeile("Was diese Mappe ist", StilUeberschrift);
            Zeile("Die Finanzsicht: eine Zeile je Buchungszeile aus dem Hauptbuch. Das Gegenstueck zu " +
                  "Sales_All, das die Vertriebssicht mit der Rechnungsposition als kleinster Einheit " +
                  "zeigt. Beides gehoert nicht in eine Datei: eine Rechnung erzeugt mehrere " +
                  "Buchungszeilen, und Zahlungen, Lagerbuchungen, Abschreibungen und manuelle " +
                  "Buchungen haben ueberhaupt keine Verkaufszeile.");
            Zeile("");
            if (seit is not null)
            {
                Zeile("ACHTUNG: gekuerzter Auszug", StilUeberschrift);
                Zeile($"Das Blatt 'Journal Detail' enthaelt nur Buchungen ab {seit}. Die Blaetter " +
                      "'Journal Summary', 'Konten' und 'Datenstatus' sind vollstaendig. Fuer die " +
                      "vollstaendige Mappe das Skript ohne --seit und ohne --tage aufrufen.", StilWarnung);
                Zeile("");
            }

            Zeile("Blaetter", StilUeberschrift);
            Zeile("Journal Detail - Buchungszeilen. Spalte entity zuerst, damit nach Gesellschaft " +
                  "sortiert und gefiltert werden kann.");
            Zeile("Journal Summary - Summen je Gesellschaft, Konto und Periode.");
            Zeile("Konten - jedes Konto je Gesellschaft einmal. Das ist die Arbeitsliste fuer das " +
                  "Konzernkonto-Mapping: Spalte Konzernkonto ausfuellen und zurueckgeben.");
            Zeile("Datenstatus - welche Gesellschaft wie weit geladen ist.");
            Zeile("Feldstatus - Belegung je Gesellschaft im gesamten geladenen Journalbestand.");
            Zeile("");
            Zeile("Was fehlt, und zwar sichtbar", StilUeberschrift);
            Zeile("Die Schweiz und Oesterreich fehlen. Ihr Hauptbuch kommt aus SAP; das dortige " +
                  "EntitySet heisst FinanzdataSchweizOeSet, der Leser sucht aber weiterhin nach " +
                  "FinanzJournalSet. Siehe ISS-006.", StilWarnung);
            Zeile("due date zeigt das Faelligkeitsdatum der Buchungszeile, kein Zahlungsdatum. " +
                  "Nach Erweiterung des Imports muessen die Gesellschaften erneut geladen werden; " +
                  "die Belegung steht im Blatt Feldstatus.");
            Zeile("clearing date ist das Ausgleichsdatum der Buchungszeile und ausdruecklich kein " +
                  "fertiges date paid: ein Ausgleich kann aus einer Teilzahlung, einer Gutschrift " +
                  "oder einer manuellen Zuordnung entstehen. Eine leere Zelle heisst 'nicht (oder " +
                  "noch nicht) ausgeglichen', nicht 'nicht geladen'. recon date ist das Datum des " +
                  "juengsten zugehoerigen Ausgleichsvorgangs, clearing ref seine Nummer, " +
                  "Ausgleiche deren Anzahl und clearing storniert weist zurueckgenommene " +
                  "Ausgleiche aus. Steht in Ausgleiche eine Zahl groesser 1, ist die Zeile " +
                  "mehrfach ausgeglichen worden und recon date ist nur der letzte Vorgang. " +
                  "Welche dieser Bedeutungen die fuehrende Kennzahl werden soll, entscheidet " +
                  "Finance.", StilWarnung);
            Zeile("Das Zahlungsdatum des Zahlungsbelegs selbst (ORCT/OVPM) ist bewusst noch nicht " +
                  "angebunden; dafuer fehlt die live gemessene Verknuepfung ueber ITR1.", StilWarnung);
            Zeile("Die Ausgleichsspalten stammen aus SAP B1. Fuer die Schweiz und Oesterreich " +
                  "(SAP ECC) koennen nur clearing date und clearing ref belegt werden; recon " +
                  "date, Ausgleiche und clearing storniert bleiben dort bauartbedingt leer und " +
                  "nicht etwa mangels Daten.", StilWarnung);
            Zeile("Der Konzernkontenplan mit Zuordnungen je Gesellschaft liegt noch nicht vor; " +
                  "Konzernkonto bleibt deshalb leer. Kostenstelle und dimension 2 werden aus " +
                  "den Buchungszeilen uebernommen; leere Quellfelder bleiben leer.", StilWarnung);
            Zeile("");
            Zeile("Betraege und Waehrungen", StilUeberschrift);
            Zeile("Die Spalte amount ist der Betrag mit Vorzeichen in der Landeswaehrung: Soll " +
                  "positiv, Haben negativ. Es gibt bewusst keine Summe ueber Gesellschaften " +
                  "hinweg, weil die Waehrungen unterschiedlich sind.");
            Zeile("Das Kennzeichen db/cr folgt der Skizze vom 08.09.2026: 40 fuer Soll, 50 fuer Haben.");

            w.WriteEndElement(); // SheetData
            w.WriteEndElement(); // Worksheet
        }

        return ("Lesehinweis", wbPart.GetIdOfPart(part));
    }

    // --- Journal Detail -----------------------------------------------------
    private static ((string, string) Blatt, long Zeilen) SchreibeJournalDetail(
        WorkbookPart wbPart, SqliteConnection con, string? seit,
        string dueSql, string clearingSql, string reconSql,
        string clearingRefSql, string clearingAnzahlSql, string clearingStornoSql)
    {
        var kopf = new[]
        {
            "entity", "db/cr", "accnt", "acct desrc", "amount", "currency", "entry text",
            "posting date", "entry ID", "line ID", "due date", "clearing date",
            "recon date", "clearing ref", "Ausgleiche", "clearing storniert",
            "Konzernkonto", "fiscal year", "period", "cost center", "dimension 2",
            "doc type", "source document", "manual", "reversal", "source system"
        };
        var breiten = new double[]
        {
            10, 8, 14, 34, 16, 10, 42, 13, 16, 9, 12, 13, 12, 13, 11, 16, 14, 11, 8, 14,
            14, 10, 18, 9, 9, 13
        };

        var part = wbPart.AddNewPart<WorksheetPart>();
        long anzahl = 0;

        using (var w = OpenXmlWriter.Create(part))
        {
            w.WriteStartElement(new Worksheet());
            SchreibeFixierung(w);
            SchreibeSpaltenbreiten(w, breiten);
            w.WriteStartElement(new SheetData());
            SchreibeKopfzeile(w, kopf, 1);

            using var cmd = con.CreateCommand();
            cmd.CommandText =
                "SELECT Tsc, CAST(DebitAmount AS REAL) AS Soll, CAST(CreditAmount AS REAL) AS Haben," +
                " AccountCode, AccountName, CAST(SignedAmountLocal AS REAL) AS Betrag," +
                " LocalCurrency, LineMemo, PostingDate, JournalEntryId, JournalEntryLineId," +
                " FiscalYear, FiscalPeriod, CostCenter, Dimension2, TransactionType," +
                " SourceDocumentNumber, IsManual, IsReversal, SourceSystem, " + dueSql + " AS DueDate," +
                " " + clearingSql + " AS ClearingDate, " + reconSql + " AS ReconciliationDate," +
                " " + clearingRefSql + " AS ClearingReference," +
                " " + clearingAnzahlSql + " AS ClearingCount," +
                " " + clearingStornoSql + " AS IsClearingCancelled" +
                " FROM FinancialJournalEntries" +
                (seit is null ? "" : " WHERE PostingDate >= $seit") +
                " ORDER BY Tsc, PostingDate, JournalEntryId, JournalEntryLineId";
            if (seit is not null)
                cmd.Parameters.AddWithValue("$seit", seit);

            using var leser = cmd.ExecuteReader();
            var s = new Zeilenschreiber(w);
            var zeilennummer = 2;
            while (leser.Read())
            {
                s.Start(zeilennummer++);
                s.Auto(leser, 0);                                  // entity
                s.Zahl(Lies(leser, 1) > 0 ? 40 : 50);              // db/cr
                s.Auto(leser, 3);                                  // accnt
                s.Auto(leser, 4);                                  // acct desrc
                s.Auto(leser, 5);                                  // amount
                s.Auto(leser, 6);                                  // currency
                s.Auto(leser, 7);                                  // entry text
                s.Datum(leser, 8);                                 // posting date
                s.Auto(leser, 9);                                  // entry ID
                s.Auto(leser, 10);                                 // line ID
                s.Datum(leser, 20);                                // due date
                s.Datum(leser, 21);                                // clearing date
                s.Datum(leser, 22);                                // recon date
                s.TextOderLeer(leser, 23);                         // clearing ref
                s.ZahlOhneNullwert(leser, 24);                     // Ausgleiche
                s.Ja(leser, 25, "");                               // clearing storniert
                s.Leer();                                          // Konzernkonto
                s.Auto(leser, 11);                                 // fiscal year
                s.Auto(leser, 12);                                 // period
                s.Auto(leser, 13);                                 // cost center
                s.Auto(leser, 14);                                 // dimension 2
                s.Auto(leser, 15);                                 // doc type
                s.Auto(leser, 16);                                 // source document
                s.Ja(leser, 17, "nein");                           // manual
                s.Ja(leser, 18, "nein");                           // reversal
                s.Auto(leser, 19);                                 // source system
                s.Ende();
                anzahl++;
            }

            w.WriteEndElement(); // SheetData
            w.WriteEndElement(); // Worksheet
        }

        return (("Journal Detail", wbPart.GetIdOfPart(part)), anzahl);
    }

    private static double Lies(SqliteDataReader leser, int spalte)
        => leser.IsDBNull(spalte) ? 0d : leser.GetDouble(spalte);

    // --- Journal Summary ----------------------------------------------------
    private static (string, string) SchreibeJournalSummary(WorkbookPart wbPart, SqliteConnection con)
    {
        var kopf = new[]
        {
            "entity", "accnt", "acct desrc", "fiscal year", "period", "currency",
            "Soll", "Haben", "Saldo", "Zeilen", "Konzernkonto"
        };
        var breiten = new double[] { 10, 14, 34, 11, 8, 10, 16, 16, 16, 10, 14 };

        return SchreibeAbfrageblatt(wbPart, con, "Journal Summary", kopf, breiten,
            "SELECT Tsc, AccountCode, MAX(AccountName) AS Name, FiscalYear, FiscalPeriod," +
            " MAX(LocalCurrency) AS Waehrung, SUM(CAST(DebitAmount AS REAL)) AS Soll," +
            " SUM(CAST(CreditAmount AS REAL)) AS Haben," +
            " SUM(CAST(SignedAmountLocal AS REAL)) AS Saldo, COUNT(*) AS Zeilen" +
            " FROM FinancialJournalEntries" +
            " GROUP BY Tsc, AccountCode, FiscalYear, FiscalPeriod" +
            " ORDER BY Tsc, AccountCode, FiscalYear, FiscalPeriod",
            (s, leser) =>
            {
                for (var i = 0; i <= 9; i++)
                    s.Auto(leser, i);
                s.Leer(); // Konzernkonto, wartet auf den Konzernkontenplan
            });
    }

    // --- Konten -------------------------------------------------------------
    private static (string, string) SchreibeKonten(WorkbookPart wbPart, SqliteConnection con)
    {
        var kopf = new[]
        {
            "entity", "accnt", "acct desrc", "currency", "Zeilen", "Saldo gesamt",
            "Konzernkonto", "Konzernkontobezeichnung"
        };
        var breiten = new double[] { 10, 14, 40, 10, 10, 18, 14, 34 };

        return SchreibeAbfrageblatt(wbPart, con, "Konten", kopf, breiten,
            "SELECT Tsc, AccountCode, MAX(AccountName) AS Name, MAX(LocalCurrency) AS Waehrung," +
            " COUNT(*) AS Zeilen, SUM(CAST(SignedAmountLocal AS REAL)) AS Saldo" +
            " FROM FinancialJournalEntries GROUP BY Tsc, AccountCode ORDER BY Tsc, AccountCode",
            (s, leser) =>
            {
                for (var i = 0; i <= 5; i++)
                    s.Auto(leser, i);
                s.Leer(); // Konzernkonto
                s.Leer(); // Konzernkontobezeichnung
            });
    }

    // --- Datenstatus --------------------------------------------------------
    private static (string, string) SchreibeDatenstatus(WorkbookPart wbPart, SqliteConnection con)
    {
        var kopf = new[]
        {
            "entity", "Quellsystem", "Zeilen", "Buchungsdatum von", "bis", "zuletzt geladen", "Hinweis"
        };
        var breiten = new double[] { 10, 14, 12, 18, 14, 22, 62 };

        var part = wbPart.AddNewPart<WorksheetPart>();
        using (var w = OpenXmlWriter.Create(part))
        {
            w.WriteStartElement(new Worksheet());
            SchreibeFixierung(w);
            SchreibeSpaltenbreiten(w, breiten);
            w.WriteStartElement(new SheetData());
            SchreibeKopfzeile(w, kopf, 1);

            var vorhanden = new HashSet<string>(StringComparer.Ordinal);
            var zeilennummer = 2;
            using (var cmd = con.CreateCommand())
            {
                cmd.CommandText =
                    "SELECT Tsc, MAX(SourceSystem) AS Quelle, COUNT(*) AS Zeilen, MIN(PostingDate) AS Von," +
                    " MAX(PostingDate) AS Bis, MAX(StoredAtUtc) AS Geladen" +
                    " FROM FinancialJournalEntries GROUP BY Tsc ORDER BY Tsc";
                using var leser = cmd.ExecuteReader();
                var s = new Zeilenschreiber(w);
                while (leser.Read())
                {
                    if (!leser.IsDBNull(0))
                        vorhanden.Add(leser.GetString(0));
                    s.Start(zeilennummer++);
                    s.Auto(leser, 0);
                    s.Auto(leser, 1);
                    s.Auto(leser, 2);
                    s.Datum(leser, 3);
                    s.Datum(leser, 4);
                    s.Gekuerzt(leser, 5, 19);
                    s.Leer(); // Hinweis bleibt bei geladenen Gesellschaften leer
                    s.Ende();
                }
            }

            if (!vorhanden.Contains("ZSCHWEIZ"))
            {
                var s = new Zeilenschreiber(w);
                s.Start(zeilennummer);
                s.Wert("ZSCHWEIZ");
                s.Wert("SAP");
                s.Zahl(0);
                s.Leer();
                s.Leer();
                s.Leer();
                s.Wert("Fehlt: der Leser sucht FinanzJournalSet, in P76 heisst das EntitySet " +
                       "FinanzdataSchweizOeSet (ISS-006)");
                s.Ende();
            }

            w.WriteEndElement(); // SheetData
            w.WriteEndElement(); // Worksheet
        }

        return ("Datenstatus", wbPart.GetIdOfPart(part));
    }

    // --- Feldstatus ---------------------------------------------------------
    private static (string, string) SchreibeFeldstatus(
        WorkbookPart wbPart, SqliteConnection con,
        string dueSql, string clearingSql, string reconSql,
        string clearingRefSql, string clearingAnzahlSql, string clearingStornoSql)
    {
        var kopf = new[] { "entity", "Feld", "Zeilen gesamt", "Gefuellt", "Leer", "Hinweis" };
        var breiten = new double[] { 10, 18, 16, 14, 14, 80 };

        // (Anzeigename, SQL-Ausdruck, Hinweis) - wortgleich zum Python-Werkzeug.
        var felder = new (string Feld, string Sql, string Hinweis)[]
        {
            ("fiscal year", "FiscalYear", "Geschaeftsjahr laut Import."),
            ("period", "FiscalPeriod", "Periode laut Import."),
            ("cost center", "CostCenter", "B1: Verteilungsregel der Dimension 1; ohne Quellwert leer."),
            ("dimension 2", "Dimension2", "B1: Verteilungsregel der Dimension 2; ohne Quellwert leer."),
            ("due date", dueSql, "Faelligkeitsdatum; fehlende Werte ggf. durch erneutes Laden nachziehen."),
            ("clearing date", clearingSql,
                "Ausgleichsdatum der Buchungszeile (B1 JDT1.MthDate). Leer heisst nicht ausgeglichen, " +
                "nicht ungeladen: nur ausgeglichene Zeilen tragen ein Datum."),
            ("recon date", reconSql,
                "Datum des zugehoerigen Ausgleichsvorgangs (B1 OITR.ReconDate). Ohne Ausgleich leer."),
            ("clearing ref", clearingRefSql,
                "Nummer des Ausgleichs, der zu recon date gehoert. Nicht JDT1.IntrnMatch: das Feld " +
                "ist live durchgehend 0 oder -1, also ungepflegt."),
            ("Ausgleiche", clearingAnzahlSql,
                "Anzahl der Ausgleichsvorgaenge zur Buchungszeile; Leer zaehlt die Zeilen ohne " +
                "Ausgleich. Groesser 1 heisst Teilausgleich, recon date ist dann nur der letzte davon."),
            ("clearing storniert", clearingStornoSql,
                "Gefuellt = mindestens ein zugehoeriger Ausgleich ist storniert (B1 OITR.Canceled Y " +
                "oder C). Die Spalte Leer zaehlt hier die nicht stornierten Zeilen."),
            ("Konzernkonto", "NULL", "Wartet auf Konzernkontenplan und Zuordnung je Gesellschaft.")
        };

        // Zwei Zaehlweisen, genau wie im Python: numerische Felder ueber > 0, alle anderen
        // ueber "nicht leer nach TRIM".
        var numerisch = new HashSet<string>(StringComparer.Ordinal)
            { "fiscal year", "period", "Ausgleiche", "clearing storniert" };
        var zaehler = string.Join(", ", felder.Select((f, i) => numerisch.Contains(f.Feld)
            ? $"SUM(CASE WHEN CAST({f.Sql} AS REAL) > 0 THEN 1 ELSE 0 END) AS f{i}"
            : $"SUM(CASE WHEN NULLIF(TRIM({f.Sql}), '') IS NOT NULL THEN 1 ELSE 0 END) AS f{i}"));

        var part = wbPart.AddNewPart<WorksheetPart>();
        using (var w = OpenXmlWriter.Create(part))
        {
            w.WriteStartElement(new Worksheet());
            SchreibeFixierung(w);
            SchreibeSpaltenbreiten(w, breiten);
            w.WriteStartElement(new SheetData());
            SchreibeKopfzeile(w, kopf, 1);

            using var cmd = con.CreateCommand();
            cmd.CommandText = "SELECT Tsc, COUNT(*) AS Gesamt, " + zaehler +
                              " FROM FinancialJournalEntries GROUP BY Tsc ORDER BY Tsc";
            using var leser = cmd.ExecuteReader();
            var s = new Zeilenschreiber(w);
            var zeilennummer = 2;
            while (leser.Read())
            {
                var tsc = leser.IsDBNull(0) ? "" : leser.GetString(0);
                var gesamt = leser.IsDBNull(1) ? 0L : leser.GetInt64(1);
                for (var i = 0; i < felder.Length; i++)
                {
                    var gefuellt = leser.IsDBNull(2 + i) ? 0L : leser.GetInt64(2 + i);
                    s.Start(zeilennummer++);
                    s.Wert(tsc);
                    s.Wert(felder[i].Feld);
                    s.Zahl(gesamt);
                    s.Zahl(gefuellt);
                    s.Zahl(gesamt - gefuellt);
                    s.Wert(felder[i].Hinweis);
                    s.Ende();
                }
            }

            w.WriteEndElement(); // SheetData
            w.WriteEndElement(); // Worksheet
        }

        return ("Feldstatus", wbPart.GetIdOfPart(part));
    }

    // --- gemeinsame Bausteine -----------------------------------------------
    private static (string, string) SchreibeAbfrageblatt(
        WorkbookPart wbPart, SqliteConnection con, string name,
        string[] kopf, double[] breiten, string sql,
        Action<Zeilenschreiber, SqliteDataReader> zeile)
    {
        var part = wbPart.AddNewPart<WorksheetPart>();
        using (var w = OpenXmlWriter.Create(part))
        {
            w.WriteStartElement(new Worksheet());
            SchreibeFixierung(w);
            SchreibeSpaltenbreiten(w, breiten);
            w.WriteStartElement(new SheetData());
            SchreibeKopfzeile(w, kopf, 1);

            using var cmd = con.CreateCommand();
            cmd.CommandText = sql;
            using var leser = cmd.ExecuteReader();
            var s = new Zeilenschreiber(w);
            var zeilennummer = 2;
            while (leser.Read())
            {
                s.Start(zeilennummer++);
                zeile(s, leser);
                s.Ende();
            }

            w.WriteEndElement(); // SheetData
            w.WriteEndElement(); // Worksheet
        }

        return (name, wbPart.GetIdOfPart(part));
    }

    private static void SchreibeFixierung(OpenXmlWriter w)
    {
        // Reihenfolge im Worksheet ist vorgeschrieben: sheetViews vor cols vor sheetData.
        var ansicht = new SheetView { WorkbookViewId = 0 };
        ansicht.Append(new Pane
        {
            VerticalSplit = 1D,
            TopLeftCell = "A2",
            ActivePane = PaneValues.BottomLeft,
            State = PaneStateValues.Frozen
        });
        var ansichten = new SheetViews();
        ansichten.Append(ansicht);
        w.WriteElement(ansichten);
    }

    private static void SchreibeSpaltenbreiten(OpenXmlWriter w, double[] breiten)
    {
        var spalten = new Columns();
        for (var i = 0; i < breiten.Length; i++)
        {
            spalten.Append(new Column
            {
                Min = (uint)(i + 1),
                Max = (uint)(i + 1),
                Width = breiten[i],
                CustomWidth = true
            });
        }
        w.WriteElement(spalten);
    }

    private static void SchreibeKopfzeile(OpenXmlWriter w, string[] kopf, int zeilennummer)
    {
        var s = new Zeilenschreiber(w);
        s.Start(zeilennummer);
        foreach (var text in kopf)
            s.Wert(text, StilKopf);
        s.Ende();
    }

    private static void SchreibeTextzeile(OpenXmlWriter w, string text, uint stil)
    {
        var s = new Zeilenschreiber(w);
        s.StartOhneNummer();
        if (text.Length > 0)
            s.Wert(text, stil);
        s.Ende();
    }

    /// <summary>
    /// Schreibt eine Zeile Zelle fuer Zelle direkt in den Strom. Haelt bewusst keinen Zustand
    /// ausser der Spaltenposition, damit der Speicherbedarf unabhaengig von der Zeilenzahl bleibt.
    /// </summary>
    internal sealed class Zeilenschreiber
    {
        private readonly OpenXmlWriter _w;
        private int _spalte;
        private int _zeile;
        private bool _mitNummer;

        public Zeilenschreiber(OpenXmlWriter w) => _w = w;

        public void Start(int zeilennummer)
        {
            _zeile = zeilennummer;
            _spalte = 0;
            _mitNummer = true;
            _w.WriteStartElement(new Row { RowIndex = (uint)zeilennummer });
        }

        public void StartOhneNummer()
        {
            _spalte = 0;
            _mitNummer = false;
            _w.WriteStartElement(new Row());
        }

        public void Ende() => _w.WriteEndElement();

        public void Leer() => _spalte++;

        public void Wert(string? text, uint stil = StilStandard)
        {
            var index = _spalte++;
            if (string.IsNullOrEmpty(text))
                return;

            var zelle = new Cell { DataType = CellValues.InlineString, StyleIndex = stil };
            if (_mitNummer)
                zelle.CellReference = SpaltenBuchstaben[index] + _zeile.ToString(CultureInfo.InvariantCulture);
            zelle.Append(new InlineString(new Text(text)));
            _w.WriteElement(zelle);
        }

        public void Zahl(double wert, uint stil = StilStandard)
        {
            var index = _spalte++;
            var zelle = new Cell
            {
                DataType = CellValues.Number,
                StyleIndex = stil,
                CellValue = new CellValue(wert.ToString("R", CultureInfo.InvariantCulture))
            };
            if (_mitNummer)
                zelle.CellReference = SpaltenBuchstaben[index] + _zeile.ToString(CultureInfo.InvariantCulture);
            _w.WriteElement(zelle);
        }

        /// <summary>Feld als Text, leer bei NULL.</summary>
        public void Text(SqliteDataReader leser, int spalte)
            => Wert(leser.IsDBNull(spalte) ? null : leser.GetValue(spalte)?.ToString());

        public void TextOderLeer(SqliteDataReader leser, int spalte)
            => Text(leser, spalte);

        /// <summary>
        /// Uebernimmt den Wert in der Form, die SQLite liefert: Zahlen als Zahlenzelle, Text als
        /// Textzelle, NULL als leere Zelle.
        ///
        /// Das ist kein Schoenheitsdetail. Das Python-Werkzeug reicht die Werte unveraendert an
        /// openpyxl weiter, und dort wird aus einem INTEGER wie <c>FiscalYear</c> oder
        /// <c>line ID</c> eine echte Zahlenzelle. Wer sie stattdessen als Text schreibt, bekommt
        /// in Excel Linksbuendigkeit, eine andere Sortierung und einen Zahlenfilter, der nicht
        /// greift - die Mappe saehe gleich aus und verhielte sich anders.
        /// </summary>
        public void Auto(SqliteDataReader leser, int spalte)
        {
            if (leser.IsDBNull(spalte))
            {
                Leer();
                return;
            }

            switch (leser.GetValue(spalte))
            {
                case double d: Zahl(d); break;
                case long l: Zahl(l); break;
                case int i: Zahl(i); break;
                case decimal m: Zahl((double)m); break;
                case var anderes: Wert(anderes?.ToString()); break;
            }
        }

        /// <summary>Zahl, bei NULL bleibt die Zelle leer statt eine 0 vorzutaeuschen.</summary>
        public void ZahlOderLeer(SqliteDataReader leser, int spalte)
        {
            if (leser.IsDBNull(spalte))
            {
                Leer();
                return;
            }

            var wert = leser.GetValue(spalte);
            if (wert is double d) { Zahl(d); return; }
            if (wert is long l) { Zahl(l); return; }
            if (wert is int i) { Zahl(i); return; }
            if (double.TryParse(wert?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var p))
            {
                Zahl(p);
                return;
            }

            Wert(wert?.ToString());
        }

        /// <summary>
        /// Zahl, aber eine Null bleibt eine LEERE Zelle. Das Python-Werkzeug schreibt hier
        /// <c>r["ClearingCount"] or ""</c>, und in Python ist die Null unwahr - aus einer 0 wird
        /// dort also eine leere Zelle, nicht die Zahl 0. Fachlich ist das richtig: "kein
        /// Ausgleichsvorgang" ist etwas anderes als "null Vorgaenge gezaehlt", und der Feldstatus
        /// zaehlt diese Zeilen ausdruecklich als leer.
        /// </summary>
        public void ZahlOhneNullwert(SqliteDataReader leser, int spalte)
        {
            if (leser.IsDBNull(spalte))
            {
                Leer();
                return;
            }

            var wert = leser.GetValue(spalte);
            var zahl = wert switch
            {
                double d => d,
                long l => l,
                int i => i,
                _ => double.TryParse(wert?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var p)
                    ? p
                    : 0d
            };

            if (zahl == 0d)
            {
                Leer();
                return;
            }

            Zahl(zahl);
        }

        /// <summary>Datumsfeld auf die ersten zehn Zeichen gekuerzt, wie im Python-Werkzeug.</summary>
        public void Datum(SqliteDataReader leser, int spalte) => Gekuerzt(leser, spalte, 10);

        public void Gekuerzt(SqliteDataReader leser, int spalte, int laenge)
        {
            if (leser.IsDBNull(spalte))
            {
                Leer();
                return;
            }

            var text = leser.GetValue(spalte)?.ToString() ?? "";
            Wert(text.Length > laenge ? text[..laenge] : text);
        }

        /// <summary>"ja" wenn gesetzt, sonst <paramref name="sonst"/>.</summary>
        public void Ja(SqliteDataReader leser, int spalte, string sonst)
        {
            var gesetzt = !leser.IsDBNull(spalte) && IstWahr(leser.GetValue(spalte));
            Wert(gesetzt ? "ja" : sonst);
        }

        private static bool IstWahr(object? wert) => wert switch
        {
            null => false,
            bool b => b,
            long l => l != 0,
            int i => i != 0,
            double d => d != 0,
            string s => s.Length > 0 && s != "0" &&
                        !s.Equals("false", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static string SpaltenName(int index)
    {
        var name = "";
        var rest = index;
        do
        {
            name = (char)('A' + rest % 26) + name;
            rest = rest / 26 - 1;
        } while (rest >= 0);
        return name;
    }

    private static Stylesheet BaueStylesheet()
    {
        var fonts = new Fonts(
            new Font(new FontSize { Val = 11D }, new FontName { Val = "Calibri" }),
            new Font(new Bold(), new FontSize { Val = 11D },
                new DocumentFormat.OpenXml.Spreadsheet.Color { Rgb = "FFFFFFFF" },
                new FontName { Val = "Calibri" }),
            new Font(new Bold(), new FontSize { Val = 16D },
                new DocumentFormat.OpenXml.Spreadsheet.Color { Rgb = KopfFarbe },
                new FontName { Val = "Calibri" }),
            new Font(new Italic(), new FontSize { Val = 9D },
                new DocumentFormat.OpenXml.Spreadsheet.Color { Rgb = "FF595959" },
                new FontName { Val = "Calibri" }),
            new Font(new Bold(), new FontSize { Val = 11D }, new FontName { Val = "Calibri" }))
        { Count = 5 };

        var fills = new Fills(
            new Fill(new PatternFill { PatternType = PatternValues.None }),
            new Fill(new PatternFill { PatternType = PatternValues.Gray125 }),
            new Fill(new PatternFill(
                new ForegroundColor { Rgb = KopfFarbe },
                new BackgroundColor { Indexed = 64U })
            { PatternType = PatternValues.Solid }),
            new Fill(new PatternFill(
                new ForegroundColor { Rgb = WarnFarbe },
                new BackgroundColor { Indexed = 64U })
            { PatternType = PatternValues.Solid }))
        { Count = 4 };

        var borders = new Borders(
            new Border(new LeftBorder(), new RightBorder(), new TopBorder(),
                new BottomBorder(), new DiagonalBorder()))
        { Count = 1 };

        var cellStyleFormats = new CellStyleFormats(
            new CellFormat { NumberFormatId = 0, FontId = 0, FillId = 0, BorderId = 0 })
        { Count = 1 };

        // Reihenfolge muss zu den Stil-Konstanten oben passen.
        var cellFormats = new CellFormats(
            // 0 Standard
            new CellFormat { FontId = 0, FillId = 0, BorderId = 0, FormatId = 0 },
            // 1 Kopfzeile
            new CellFormat
            {
                FontId = 1,
                FillId = 2,
                BorderId = 0,
                FormatId = 0,
                ApplyFont = true,
                ApplyFill = true,
                ApplyAlignment = true,
                Alignment = new Alignment { Vertical = VerticalAlignmentValues.Center, WrapText = true }
            },
            // 2 Titel
            new CellFormat
            {
                FontId = 2,
                FillId = 0,
                BorderId = 0,
                FormatId = 0,
                ApplyFont = true,
                ApplyAlignment = true,
                Alignment = new Alignment { Vertical = VerticalAlignmentValues.Top, WrapText = true }
            },
            // 3 Kleingedrucktes
            new CellFormat
            {
                FontId = 3,
                FillId = 0,
                BorderId = 0,
                FormatId = 0,
                ApplyFont = true,
                ApplyAlignment = true,
                Alignment = new Alignment { Vertical = VerticalAlignmentValues.Top, WrapText = true }
            },
            // 4 Zwischenueberschrift
            new CellFormat
            {
                FontId = 4,
                FillId = 0,
                BorderId = 0,
                FormatId = 0,
                ApplyFont = true,
                ApplyAlignment = true,
                Alignment = new Alignment { Vertical = VerticalAlignmentValues.Top, WrapText = true }
            },
            // 5 Warnung
            new CellFormat
            {
                FontId = 0,
                FillId = 3,
                BorderId = 0,
                FormatId = 0,
                ApplyFill = true,
                ApplyAlignment = true,
                Alignment = new Alignment { Vertical = VerticalAlignmentValues.Top, WrapText = true }
            },
            // 6 Fliesstext
            new CellFormat
            {
                FontId = 0,
                FillId = 0,
                BorderId = 0,
                FormatId = 0,
                ApplyAlignment = true,
                Alignment = new Alignment { Vertical = VerticalAlignmentValues.Top, WrapText = true }
            })
        { Count = 7 };

        return new Stylesheet(fonts, fills, borders, cellStyleFormats, cellFormats);
    }
}
