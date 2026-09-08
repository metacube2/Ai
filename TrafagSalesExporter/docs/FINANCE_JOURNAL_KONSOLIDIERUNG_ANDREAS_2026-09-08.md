# Journal fuer die Konsolidierung: Zielbild Andreas vom 2026-09-08

Stand: 2026-09-08. Ergaenzt `docs/FINANCE_JOURNAL.md`, ersetzt es nicht. Dort steht das
bestehende Feld-Mapping und die Technik; hier steht, was Andreas fuer die Konsolidierung
zusaetzlich braucht und was dafuer noch fehlt.

Grundlage sind Andreas' Feldliste und die Layoutskizze `db1_fin.xlsx`, beides am
2026-09-08 von Ingo weitergegeben.

## 1. Von 24 gewuenschten Feldern sind 22 bereits spezifiziert und gebaut

Andreas' Liste deckt sich fast vollstaendig mit dem Mapping in `docs/FINANCE_JOURNAL.md`.
Vorhanden sind Gesellschaft, Quellsystem, Journal Entry ID und Zeilen-ID, Buchungsdatum,
Geschaeftsjahr und Periode, Sachkonto und Kontobezeichnung, Soll und Haben, Betrag mit
Vorzeichen, lokale Waehrung und Betrag, Transaktionswaehrung und Betrag, Kostenstelle,
zweite Dimension, Buchungstext, Belegart, Quelldokumentnummer, das Manuell-Kennzeichen,
das Stornokennzeichen und der Extraktionszeitpunkt.

Zwei Dinge aus seinem Zielbild fehlen.

## 2. Luecke 1: Faelligkeitsdatum

In der Skizze heisst die Spalte `due date`. Im heutigen Modell
`Models/FinancialJournalEntry.cs` gibt es sie nicht.

Fachlich ist sie fuer eine Konsolidierung nachvollziehbar: Offene-Posten-Sichten und
Faelligkeitsstaffeln haengen daran. Technisch ist es eine Spalte mehr im HANA-Leser.

**Vor der Umsetzung zu pruefen, nicht zu raten:** ob das Datum in B1 an der Buchungszeile
oder am Beleg haengt und wie das Feld dort wirklich heisst. Genau dieser Fehlertyp hat bei
UK-2025 und bei ZC12 Zeit gekostet. Die Pruefung gehoert auf denselben Lauf wie Punkt 3 der
offenen Punkte in `FINANCE_JOURNAL.md`, also die Live-Verifikation der Spalten gegen
`fr01_p` und `TRAFAG_LIVE`.

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

Ob dort `OJDT` und `JDT1` liegen, prueft der Leser selbst und meldet es klar. Vom
Entwicklungsrechner ist die indische Quelle nicht erreichbar, der Produktivserver dagegen
schon. Der Test ist also ein Ladeversuch aus der Anwendung heraus, kein Entwicklungsschritt.

## 6. Der Datenbestand steht seit Juli still

Am 2026-09-08 produktiv gemessen:

| Standort | Zeilen | Von | Bis |
|---|---:|---|---|
| TRIT | 149'705 | 01.01.2025 | 16.07.2026 |
| TRUS | 19'599 | 01.01.2025 | 13.07.2026 |
| TRFR | 18'285 | 01.01.2025 | 13.07.2026 |
| TRIN | **264'911** | **01.01.2025** | **08.09.2026** |
| ZSCHWEIZ | 0 | — | — |

**Nachtrag vom selben Tag:** Ingo hat den Journalimport fuer Indien angestossen. Er hat
getragen — 264'911 Zeilen bis zum 08.09.2026. Damit ist belegt, dass `OJDT` und `JDT1` in
`TRAFAG_LIVE` liegen und die Anbindung funktioniert; ein Umbau des Importers war nicht
noetig. Frankreich, Italien und die USA tragen dagegen unveraendert den Ladestempel vom
14.07.2026, sind also nicht mitgelaufen.

Von neun Gesellschaften liefern heute vier Hauptbuchdaten. Fuer
eine Konsolidierung ist das die groessere Luecke als jedes fehlende Feld. CH/AT haengt
weiter am EntitySet `FinanzJournalSet`, siehe `ISS-006`.

## 7. Abgrenzung gegen `Sales_All`

Beides gehoert nicht in eine Datei. `Sales_All` ist die Vertriebssicht mit der
Rechnungsposition als kleinster Einheit; das Journal ist die Finanzsicht mit der
Buchungszeile. Eine Rechnung erzeugt mehrere Buchungszeilen, und Zahlungen, Lagerbuchungen,
Abschreibungen und manuelle Buchungen haben ueberhaupt keine Verkaufszeile. Zusammengefuehrt
waere beides doppelt gezaehlt.

Der Gewinn liegt in der Abstimmung: Der Erloes auf den GuV-Konten muss sich gegen den
Nettoumsatz in `Sales_All` abgleichen lassen. Diese Pruefung ist heute nicht fahrbar,
solange nur vier Gesellschaften geladen sind und drei davon nur bis Juli.

## 8. Finance_All: die Excel-Mappe zum Journal

Auf Ingos Vorgabe „alles in einem File, ein Feld ermoeglicht Sortierung nach
Gesellschaften, db1 auch rein" ist am 2026-09-08 `Finance_All_2026-09-08.xlsx`
entstanden, das Gegenstueck zu `Sales_All`. 452'500 Buchungszeilen, 36,4 MB, fuenf
Blaetter:

| Blatt | Inhalt |
|---|---|
| `Lesehinweis` | Was die Mappe ist, Abgrenzung zu `Sales_All`, die drei sichtbaren Luecken |
| `Journal Detail` | alle Buchungszeilen, `entity` als **erste** Spalte zum Sortieren und Filtern |
| `Journal Summary` | Summen je Gesellschaft, Konto und Periode, 9'181 Zeilen |
| `Konten` | 915 Konten je Gesellschaft — die Arbeitsliste fuer den Konzernkontenplan |
| `Datenstatus` | wer wie weit geladen ist, inklusive der fehlenden Schweiz |

Das Detailblatt folgt Andreas' Skizze: `entity`, `db/cr` mit 40 fuer Soll und 50 fuer
Haben, `accnt`, `acct desrc`, `amount` mit Vorzeichen, `entry text`, `posting date`,
`entry ID`. **`due date` und `Konzernkonto` sind als Spalten da, aber leer** — beides ist
noch nicht vorhanden und soll sichtbar fehlen statt stillschweigend zu verschwinden.

Das Blatt `Konten` ist der praktische Teil der Nachlieferung: Andreas fuellt dort die
Spalte `Konzernkonto` aus und hat damit den Kontenplan, ohne ihn von Hand aufzustellen.

**Falle beim Erzeugen:** Die Betragsspalten liegen in SQLite als TEXT. Ohne
`CAST(... AS REAL)` scheitert schon der Vergleich, und ein `>` auf Text liefert stillen
Unsinn. Dieselbe Falle wie bei `StandardCost`, siehe `docs/router/finance.md`.
