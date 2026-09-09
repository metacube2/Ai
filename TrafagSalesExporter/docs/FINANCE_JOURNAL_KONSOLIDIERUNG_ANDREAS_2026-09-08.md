# Journal fuer die Konsolidierung: Zielbild Andreas vom 2026-09-08

Stand: 2026-09-08. Ergaenzt `docs/FINANCE_JOURNAL.md`, ersetzt es nicht. Dort steht das
bestehende Feld-Mapping und die Technik; hier steht, was Andreas fuer die Konsolidierung
zusaetzlich braucht und was dafuer noch fehlt.

Grundlage sind Andreas' Feldliste und die Layoutskizze `db1_fin.xlsx`, beides am
2026-09-08 von Ingo weitergegeben.

## 1. Faelligkeitsdatum vorbereitet; Konzernkonto und Quellbelegung bleiben offen

Andreas' Liste deckt sich fast vollstaendig mit dem Mapping in `docs/FINANCE_JOURNAL.md`.
Vorhanden sind Gesellschaft, Quellsystem, Journal Entry ID und Zeilen-ID, Buchungsdatum,
Geschaeftsjahr und Periode, Sachkonto und Kontobezeichnung, Soll und Haben, Betrag mit
Vorzeichen, lokale Waehrung und Betrag, Transaktionswaehrung und Betrag, Kostenstelle,
zweite Dimension, Buchungstext, Belegart, Quelldokumentnummer, das Manuell-Kennzeichen,
das Stornokennzeichen und der Extraktionszeitpunkt.

Das Faelligkeitsdatum ist inzwischen lokal implementiert und live in allen vier
B1-Gesellschaften zu 100 % belegt. Deploy und erneutes Laden stehen noch aus.
Kostenstelle und Dimension 2 existieren technisch, sind aber direkt in allen vier
Quellen leer; Dimension 2-5 sind inaktiv. Fuer das Konzernkonto fehlt weiterhin Andreas'
Mapping. Vollstaendiger Nachweis in `FINANCE_JOURNAL.md`, Abschnitt Live-Feldpruefung.

## 2. Faelligkeitsdatum: umgesetzt und produktiv, Nachladen offen

> **Deployed am 2026-09-08 um 14:02**, Commit `646a998`, `675/675` Release-Tests gruen,
> Blockkopie `trafag_exporter.db.before-journal-duedate-20260908-135738.bak`, Server-DLL
> und lokaler Release-Build bitgleich. Zwei Einschraenkungen bleiben: Die Spalte ist erst
> nach einem **erneuten Ladelauf** je Gesellschaft gefuellt, und der **SAP-Gateway-Leser
> fuer CH/AT ist nicht mit angepasst** — dort gibt es weiterhin kein Faelligkeitsdatum.
> Der Rest dieses Abschnitts beschreibt den Stand vor dem Deploy.

In der Skizze heisst die Spalte `due date`. `Models/FinancialJournalEntry.cs` fuehrt
dafuer jetzt `DueDate` als nullable Datum; Schema-Maintenance, HANA-Reader und Export
sind durchverbunden. Ohne Nachladen bleiben alte Datenbankzeilen leer.

**Live geprueft am 08.09.2026:** `JDT1.DueDate` ist in 469'661 Buchungszeilen ab 2025
ueber FR/IT/US/IN vollstaendig gefuellt. Das Datum der Buchungszeile weicht in 16'708
Faellen vom Kopfdatum `OJDT.DueDate` ab; deshalb wird ausdruecklich das Zeilendatum gelesen.
Es ist kein Zahlungs- oder Ausgleichsdatum. Diese zusaetzliche Fachfrage klaert Ingo noch.

## 3. Luecke 2: Konzernkonto-Mapping

Andreas' Skizze zeigt rechts eine Zuordnung lokales Konto zu Konzernkonto,
`100000 -> 1000` und `300000 -> 2000`. Das ist der Kern der Konsolidierung und in der
Anwendung heute **nicht vorhanden**.

Was dafuer noetig ist, in dieser Reihenfolge:

1. **Andreas liefert den Konzernkontenplan.** Ohne ihn laesst sich die Tabelle bauen, aber
   nicht fuellen. Ein Vorlagenblatt liegt als
   `docs/vorlagen/Konzernkonten_Mapping_Vorlage.csv` bereit.
2. Zuordnungstabelle je Gesellschaft, weil die lokalen Kontenplaene sich unterscheiden.
   Ein einziges globales Mapping wuerde nur gutgehen, wenn alle Gesellschaften denselben
   Kontenrahmen haetten; das ist bei FR, IT, US, IN und CH/AT nicht der Fall.
3. Gueltig-ab-Datum je Zuordnung. Kontenplaene aendern sich, und eine rueckwirkend
   geaenderte Zuordnung wuerde alte Abschluesse still verschieben.
4. Ein sichtbarer Rest: jede Buchungszeile ohne Zuordnung muss als solche erscheinen,
   nicht auf ein Sammelkonto fallen. Sonst sieht eine unvollstaendige Konsolidierung
   vollstaendig aus.

## 4. Einwand zur verdichteten Darstellung

**Nachtrag vom 2026-09-09.** Im Gespraech mit Andreas und Philipp Steiger hat Andreas eine
Minimalladung genannt (Buchungsdatum, Faelligkeitsdatum, Buchungs-ID, Konto,
Kontobezeichnung, Betrag, Buchungstext, Entity, dazu das Mapping auf das Konzernkonto)
und dabei **beide** Soll-Haben-Varianten vertreten: zuerst ein Soll-Haben-Kennzeichen,
spaeter zwei Betragsspalten Debit und Credit. Die Darstellung ist damit **nicht**
entschieden; Philipp schaut darauf, und was B1 beziehungsweise HANA im Standard liefert,
ist noch zu pruefen. Wortlaut: `docs/FINANCE_STANDARDKOSTEN_SCHNITT_ANDREAS_2026-09-09.md` Abschnitt 8.

Die untere Tabelle der Skizze presst eine Buchung in **eine** Zeile, mit `accnt db` und
`accnt cr` nebeneinander. Das funktioniert nur bei Buchungen mit genau zwei Zeilen.

Der Normalfall hat mehr: eine Ausgangsrechnung bucht Erloes, Mehrwertsteuer und Forderung,
also mindestens drei Zeilen, oft mehr. Diese lassen sich nicht verlustfrei auf eine Zeile
abbilden. Wer es doch tut, muss entweder Zeilen weglassen oder Betraege zusammenfassen, und
beides macht die Zahl fuer eine Konsolidierung unbrauchbar.

**Empfehlung:** die zeilenweise Darstellung (die obere, gelbe Tabelle) ist das
Lieferformat. Die verdichtete Sicht kann als Auswertung darueber entstehen, dort wo eine
Buchung tatsaechlich nur zwei Zeilen hat. Diesen Punkt vor einer Formatfestlegung mit
Andreas klaeren.

## 5. Indien braucht keinen Umbau, nur einen Lauf

Die Frage war, ob man Indien im Finance-Importer analog zum Vertrieb neu erstellen muss.
Muss man nicht. `FinancialJournalRefreshService.IsJournalSite` grenzt bewusst **nicht**
ueber den Quellsystem-Code ein, sondern ueber die Anschlussart. Indien ist mit
`ConnectionKind = Hana` und Schema `TRAFAG_LIVE` ein gueltiger Journalstandort; der Code
`SAGE` ist laut Kommentar im Dienst historisch und irrefuehrend, fachlich ist es B1.

Ob dort `OJDT` und `JDT1` liegen, prueft der Leser selbst und meldet es klar.
Die Feldprobe vom 08.09.2026 bestaetigt inzwischen auch den direkten lesenden Zugriff
vom Entwicklungsrechner auf Indien. Eine aeltere Nichterreichbarkeit gilt nicht mehr als
aktueller Blocker.

## 6. Alle vier B1-Gesellschaften sind am 08.09.2026 nachgeladen

Aktueller Export `Finance_All_2026-09-08.xlsx`: 469'629 Zeilen, FR 20'170,
IT 162'724, US 21'770 und IN 264'965, jeweils bis 08.09.2026. Die anschliessende
direkte HANA-Feldprobe zaehlt bereits 264'997 indische Zeilen; das ist eine spaetere
Quellmessung und kein erneuter Import. CH/AT fehlen weiterhin.

Der vorherige Vormittagsstand mit FR 18'285, IT 149'705 und US 19'599 Zeilen bis
Juli sowie IN 264'911 Zeilen bis September ist durch das Nachladen ueberholt.
Von neun Gesellschaften liefern vier Hauptbuchdaten. CH/AT haengt an der noch
ungeprueften Feldabbildung des vorhandenen `FinanzdataSchweizOeSet` gegen die
Erwartung `FinanzJournalSet`, siehe `ISS-006`. DE/UK/ES haben noch keine Journalquelle.

## 7. Abgrenzung gegen `Sales_All`

Beides gehoert nicht in eine Datei. `Sales_All` ist die Vertriebssicht mit der
Rechnungsposition als kleinster Einheit; das Journal ist die Finanzsicht mit der
Buchungszeile. Eine Rechnung erzeugt mehrere Buchungszeilen, und Zahlungen, Lagerbuchungen,
Abschreibungen und manuelle Buchungen haben ueberhaupt keine Verkaufszeile. Zusammengefuehrt
waere beides doppelt gezaehlt.

Der Gewinn liegt in der Abstimmung: Der Erloes auf den GuV-Konten muss sich gegen den
Nettoumsatz in `Sales_All` abgleichen lassen. Diese Pruefung ist
fuer die vier geladenen Gesellschaften grundsaetzlich pruefbar; eine gruppenweite
Abstimmung bleibt wegen der fehlenden Journalquellen unvollstaendig.

## 8. Finance_All: die Excel-Mappe zum Journal

Auf Ingos Vorgabe „alles in einem File, ein Feld ermoeglicht Sortierung nach
Gesellschaften, db1 auch rein" ist am 2026-09-08 `Finance_All_2026-09-08.xlsx`
entstanden, das Gegenstueck zu `Sales_All`. Nach dem Nachladen 469'629 Buchungszeilen.
Der erweiterte Generator erzeugt sechs Blaetter (Feldstatus neu):

| Blatt | Inhalt |
|---|---|
| `Lesehinweis` | Was die Mappe ist, Abgrenzung zu `Sales_All`, die drei sichtbaren Luecken |
| `Journal Detail` | alle Buchungszeilen, `entity` als **erste** Spalte zum Sortieren und Filtern |
| `Journal Summary` | Summen je Gesellschaft, Konto und Periode, 9'181 Zeilen |
| `Konten` | 915 Konten je Gesellschaft — die Arbeitsliste fuer den Konzernkontenplan |
| `Datenstatus` | wer wie weit geladen ist, inklusive der fehlenden Schweiz |
| `Feldstatus` | Belegung aller sechs diskutierten Felder je Gesellschaft, immer ueber den Gesamtbestand |

Das Detailblatt folgt Andreas' Skizze: `entity`, `db/cr` mit 40 fuer Soll und 50 fuer
Haben, `accnt`, `acct desrc`, `amount` mit Vorzeichen, `entry text`, `posting date`,
`entry ID`. **`due date` und `Konzernkonto` sind als Spalten da, aber leer** — beides ist
noch nicht vorhanden und soll sichtbar fehlen statt stillschweigend zu verschwinden.

Das Blatt `Konten` ist der praktische Teil der Nachlieferung: Andreas fuellt dort die
Spalte `Konzernkonto` aus und hat damit den Kontenplan, ohne ihn von Hand aufzustellen.

Erzeugt wird die Mappe mit `Tools/FinanceAll/finance_all_xlsx.py`. Schalter:
`--lokal` kopiert die Datenbank vor dem Lesen (dringend empfohlen), `--tage N` und
`--seit JJJJ-MM-TT` kuerzen das Detailblatt fuer schnelle Formattests, `--ziel` setzt
den Dateinamen. Laufzeiten am 2026-09-08 bei 469'629 Zeilen: vollstaendig mit `--lokal`
**4 min 10 s**, gekuerzt auf 30 Tage **49 s**; ohne `--lokal` waren es ueber zehn
Minuten, selbst fuer die gekuerzte Fassung. Begruendung und Messreihe:
`docs/router/plattform.md`, Abschnitt „Grosse Auswertungen".

**Falle beim Erzeugen:** Die Betragsspalten liegen in SQLite als TEXT. Ohne
`CAST(... AS REAL)` scheitert schon der Vergleich, und ein `>` auf Text liefert stillen
Unsinn. Dieselbe Falle wie bei `StandardCost`, siehe `docs/router/finance.md`.
