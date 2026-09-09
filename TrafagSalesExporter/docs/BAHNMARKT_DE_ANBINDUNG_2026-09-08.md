# Bahnmarkt Deutschland: Auswertung, Branchenfund und der korrekte Kundenschluessel

Stand: 2026-09-09, 14:00. Anlass war Rohail Munirs Anfrage nach der Datenaufbereitung fuer
den Bahnmarkt, mit dem Zusatz, dass Patrik die Zuordnung nicht gemacht hat und trotzdem
etwas vorzeigbar sein muss.

**Kurzfassung des heutigen Stands:** Der Kundenschluessel fuer Deutschland ist geloest.
Rohails Rechnungsliste vom 09.09. liefert zu jeder Rechnungsnummer die fachliche
Adressnummer; 7'592 der 7'622 Verkaufszeilen sind damit zugeordnet und 24 Bahnkunden
belegt. Die Abschnitte 2 bis 8 beschreiben den Weg dorthin und enthalten Zwischenstaende,
die inzwischen ueberholt sind. **Massgeblich ist Abschnitt 9.** Der produktive Nachzug in
Datenbank und Dashboard steht noch aus.

Ergaenzt `docs/MARKTSEGMENTE_RAILWAY_2026-08-13.md` und
`docs/KONZEPT_RAILWAY_EXPORT_2026-09-01.md`, ersetzt beide nicht.

## 1. Was geliefert wurde

`Bahnmarkt_Datenaufbereitung_2026-09-08.xlsx` in der Repository-Wurzel, gelesen read-only
aus der produktiven Datenbank. Vier Blaetter: `Lesehinweis`, `Umsatz je Standort`,
`Bahnkunden` mit Autofilter und `Datenluecken`.

Bahnumsatz 2026 bis zum 08.09.:

| Standort | Kunden | Umsatz |
|---|---:|---:|
| TRIN | 1 | 26'298'606 INR |
| TRCH | 54 | 1'834'842 CHF |
| TRES | 7 | 641'928 EUR |
| TRIT | 28 | 281'087 EUR |
| TRFR | 12 | 230'594 EUR |
| TRUK | 8 | 208'194 GBP |
| TRAT | 5 | 176'169 EUR |

Bewusst **keine Gesamtsumme**: die Waehrungen stehen nebeneinander, eine Addition darueber
waere keine sinnvolle Zahl. Derselbe Fehlertyp, den die Finance-Fachpruefung vom
2026-09-07 als A1 und A3 im Cockpit gefunden hat.

**Vorbehalt in jeder Zeile:** Von 172 Zuordnungen ist genau **eine** bestaetigt, ein Kunde
bei TRIN. Die uebrigen 171 sind maschinelle Vorschlaege aus dem Namensabgleich mit der
Marktumfrage. Der Abgleich ist nachweislich fehlbar; er hat „BROT" auf „K.S. & BROTHERS"
gezogen. Die Spalte `Status` sagt das bei jeder Zeile, damit die Zahlen nicht als geprueft
weiterwandern.

## 2. Warum Deutschland fehlt, und warum das kein Datenproblem des Standorts ist

TRDE hat **null** Railway-Zuordnungen, obwohl die Anfrage aus Deutschland kommt. Ursache:
In allen 7'615 deutschen Verkaufszeilen ist `CustomerName` leer, die Kundennummer dagegen zu
100 Prozent gefuellt. Der Namensabgleich konnte dort nichts finden.

Die Ursache liegt **nicht** bei Alphaplan, sondern in unserem Export. `alphaplanExport.ps1`
liest `dbo.Belege` und die Positionen und gibt `RechnungsAdressenID` mit, loest den
Fremdschluessel aber nie auf die Adresstabelle auf. Der Name existiert also im Quellsystem
und wird nur nicht abgeholt.

Groesse des blinden Flecks: **359 deutsche Kunden mit 2'931'057 EUR in 2026**, dazu 424
Kunden mit 4'098'279 EUR in 2025 — vollstaendig ausserhalb der Bahnauswertung.

Ein Nebenbefund erklaert, warum trotzdem deutsche Firmen in der Auswertung auftauchen: In
der Marktumfrage ist **Deutschland mit 67 Eintraegen das groesste Land**. 40 davon sind mit
einem Verkaufskunden verknuepft, aber **kein einziger mit TRDE** — 18 haengen an Italien,
17 an der Schweiz, 3 an Oesterreich, je einer an UK und Spanien. Deutsche Bahnkunden sind
also erfasst, ihr Umsatz erscheint nur unter anderen Gesellschaften.

## 3. Der Fund: Alphaplan pflegt eine Branche

Rohail hat am 2026-09-08 einen Adressexport geliefert, 6'788 Adressen. Abgelegt als
`AlphaplanExportPackage/kundenstamm_DE_20260908.csv` mit Nummer, Name, Suchname, Land,
Branche und einem abgeleiteten Bahn-Kennzeichen.

**99 Adressen tragen als Branche `00 Bahn` oder `05 rw Railways / Bahntechnik`**, darunter
Deutsche Bahn, Siemens Mobility, Knorr-Bremse, Bombardier Transportation, Wabtec, MAHLE
Industrial Thermal Systems, Behr und Schoema.

Das ist **gepflegte Klassifizierung aus dem Quellsystem**, kein Namensabgleich. Fuer
Deutschland waere die Zuordnung damit belastbarer als in jedem anderen Standort — die
anderen acht haben kein solches Feld.

## 4. Korrektur: `AdressNummer-Kunde` ist bereits der richtige Schluessel

Ingos Screenshot und der im Repository hinterlegte deutsche Excel-Aufbau belegen am
2026-09-08 dieselbe Feldtrennung:

| Quellspalte | Bedeutung | Ziel in der App |
|---|---|---|
| `AdressNummer-Kunde` / `AdressNummer` | fachliche Kundennummer | `CustomerNumber` |
| `Name Kunde` | Kundenname | `CustomerName` |
| `Land Kunde` | Kundenland | `CustomerCountry` |
| `Branche` | gepflegte Kundenbranche | `CustomerIndustry` |
| `Lieferanten Nummer` / `Lieferant` | Lieferant des Artikels | `SupplierNumber`; **niemals** Kundenschluessel |

Die Standardzuordnung der App war dafuer bereits richtig: `DatabaseSeedService` und der
Excel-Import mappen `AdressNummer-Kunde` auf `CustomerNumber` und die Lieferantenspalten
separat. Falsch war die Schlussfolgerung aus dem spaeter gebauten Zwei-Dateien-Rohexport:
Dessen Kopfdatei enthaelt nur `RechnungsAdressenID`, also die interne Datenbank-ID. Der
Spezialimport hat diese technische ID ersatzweise als `CustomerNumber` gespeichert. Daher
kam in der zentralen Datenbank der Bereich 10 bis 14016 zustande. Das beweist keinen
fehlenden Quellschluessel, sondern einen Informationsverlust dieses Rohexportwegs.

Konkrete Gegenprobe: Die im Screenshot sichtbare `AdressNummer 55013` steht in Rohails
Kundenstamm exakt bei `Siemens Mobility Rail Equipment (Tianjin) Ltd.` und traegt die
Branche `00 Bahn`.

## 5. Richtiger weiterer Weg

1. Fuer den naechsten deutschen Lauf den bestehenden Excel-Export mit
   `AdressNummer-Kunde`, `Name Kunde`, `Land Kunde` und `Branche` verwenden.
2. Den Zwei-Dateien-Rohexport erst wieder verwenden, wenn seine Kopfdatei dieselben vier
   fachlichen Felder liefert. `RechnungsAdressenID` darf nicht mehr als fachliche
   Kundennummer ausgegeben werden.
3. Nach dem Deutschland-Import die 99 Zeilen mit gepflegter Bahnbranche ueber
   `TSC + CustomerNumber` gegen Rohails Kundenstamm abgleichen und als Quelle
   `Alphaplan Kundenstamm / Branche` kennzeichnen.

## 6. Was danach moeglich wird

Nach einem neuen Deutschland-Import stehen fachliche Kundennummer, Name, Land und Branche
an den Verkaufszeilen. Die 99 Bahnbranchen-Eintraege koennen dann ohne Namensheuristik als
belastbare TRDE-Zuordnungen uebernommen werden. Fuer andere TSC gilt Rohails deutscher
Kundenstamm nicht automatisch: dort bleibt eine Zuordnung ueber lokale Kundennummer oder
eine fachliche Bestaetigung erforderlich.

Die zwei im UI-Screenshot sichtbaren Vorschlaege `FAIVELEY TRANSPORT ITALIA S.P.A.` und
`CAF CONSTRUCC. Y AUXL. DE FERROC.` kommen in Rohails deutschem Kundenstamm nicht vor.
Diese beiden konkreten Vorschlaege werden durch die neue Tabelle daher **nicht** bestaetigt.

## 7. Umsetzungsstand am 08.09.2026

### Korrektur nach erweitertem Schluesselabgleich am 09.09.2026

Die fruehere Aussage, in den vorhandenen Dateien gebe es keine Bruecke, war zu pauschal.
`docs/2025_DataExport_DE.xlsx` enthaelt 6'198 belegte Zeilen mit Belegnummer und fachlicher
Adressnummer. Fuenf historische `Sales_TRDE`-Arbeitsmappen und das kleine DE-Beispiel
wurden ebenfalls abgeglichen. Zusammen ergeben sie 1'573 eindeutige Belegnummern.
Ueber `Belegnummer` zur Rohkopfdatei entsteht die Verbindung zu `RechnungsAdressenID`.

Gemessen gegen den heruntergeladenen Sales-Stand vom 08.09.2026, 7'615 Zeilen:

| Weg | Zeilen | Aussage |
|---|---:|---|
| Direkte Belegnummer zur fachlichen Kundennummer | 4'549 | Belegbezogener Nachweis vorhanden |
| Ueber historische interne ID mit einer beobachteten fachlichen Nummer | 2'427 | Kandidat fuer Nachzug; zeitliche Stabilitaet noch pruefen |
| Keine eindeutige Nummer | 639 | 150 interne IDs in der Restliste |

Alle 6'976 ermittelten Nummern haben einen Namen im Adressexport. Das ist **noch kein
produktiver Nachzug und keine bewiesene Vollabdeckung**. Zwei interne IDs sind historisch
mehrdeutig: `3295` -> `12028`/`12717` (Sonepar), `10783` -> `14529`/`40334` (EMS).
Bei vorhandener Belegnummer wird die belegbezogene Nummer verwendet, sonst bleibt der
Konflikt offen. Gleicher Firmenname beweist keine identische Adresse oder Nummer.

Reproduzierbare Pruefung und Excel mit Adressbruecke und Restliste:
`.tmp_tools/DeKeys0909/check.py`, `.tmp_tools/DeKeys0909/DE_Kundenschluessel_Pruefung.xlsx`.
Fuer vollstaendige und dauerhafte Zuordnung braucht es den aktuellen Alphaplan-Abgleich
`RechnungsAdressenID` -> fachliche Adressnummer, mindestens fuer die 150 offenen IDs,
und die Klaerung der beobachteten Nummernwechsel. Keine Produktivdaten geaendert.

Die produktive Standortkonfiguration und der SharePoint-Ordner wurden direkt geprueft.
TRDE liest aus `Import/Finance/Deutschland/AlphaplanRaw`. Dort liegen der Vollbestand
`invoice_headers.csv`/`invoice_lines.csv`, taegliche Delta-ZIP-Dateien bis einschliesslich
08.09.2026 und die daraus erzeugten Sales-Dateien. Die aktuelle Kopfdatei enthaelt aber
weiterhin nur `RechnungsAdressenID`; auch die erzeugte Datei vom 08.09.2026 hat leere
Felder fuer Kundenname, Kundenland und Branche.

Der App-Leser ist seit dem Deploy um 23:51 produktiv erweitert: Sobald die Alphaplan-Kopfdatei
`AdressNummer-Kunde` oder `AdressNummer`, `Name Kunde`, `Land Kunde` und `Branche`
liefert, werden diese Werte in `CustomerNumber`, `CustomerName`, `CustomerCountry` und
`CustomerIndustry` uebernommen. Ist eine fachliche Nummernspalte vorhanden, faellt eine
leere Einzelzeile bewusst nicht auf `RechnungsAdressenID` zurueck. Ein Regressionstest
mit `55013`, Siemens Mobility und `00 Bahn` ist gruen; der Gesamtlauf bestand mit
680/680 Tests. Funktionscommit: `872fca9`.

Beim anschliessenden TRDE-Lauf uebernimmt die App Kunden mit den exakten Branchen
`00 Bahn` oder `05 rw Railways / Bahntechnik` automatisch als bestaetigtes Segment
`Railway`. Bestehende bestaetigte menschliche Entscheide auf ein anderes Segment werden
nicht ueberschrieben. Bei der heutigen Quelle bleibt diese Automatik wirkungslos, weil
`CustomerIndustry` leer ist; dadurch kann der falsche interne Nummernkreis keine
Bahnzuordnung ausloesen.

Der Produktivbestand wurde bewusst noch nicht ersetzt und die 99 Bahnzuordnungen wurden
noch nicht aktiviert. Der Grund ist fachlich zwingend: Ein Teil der externen Nummern ab
10000 liegt zufaellig im Bereich der derzeit gespeicherten internen IDs. Eine vorzeitige
Zuordnung wuerde daher Umsatz fremder Kunden als Railway ausweisen. Der letzte Quellschritt
ist eine neue Alphaplan-Kopfdatei mit beiden Schluesseln und den vier Kundenfeldern. Danach
folgen gesicherter TRDE-Neulauf, Fuellgradpruefung und die kundennummerngenaue Uebernahme
der 99 Bahnbranchen.

## 8. Produktiver Nachzug und Rohail-Dateien am 09.09.2026

**Dieser Abschnitt ersetzt die frueheren Offenmeldungen zum gesamten DE-Nachzug.**
Die historische ID-Bruecke ist weiterhin kein Beweis fuer zeitliche Stabilitaet.
Deshalb wurden nur direkte Belegnachweise automatisch uebernommen.

| Nachweis | Ergebnis |
|---|---:|
| DE-Verkaufszeilen unveraendert | 7'615 |
| Fachliche Kundennummer, Name, Land und Branche direkt belegt nachgezogen | 4'549 |
| Historisch ableitbar, produktiv weiterhin offen | 2'427 |
| Ohne Kandidat | 624 |
| Mehrdeutige Zuordnung ohne direkten Beleg | 15 |
| Produktiv weiterhin ohne fachliche Zuordnung | 3'066 |
| Neue bestaetigte DE-Railway-Kunden | 19 |
| Offene Vorschlaege anderer Standorte, unveraendert | 171 |

Rechnungsnummer **und** interne Adress-ID muessen zur eingebetteten Belegbruecke
passen. Neue Rechnungen werden nicht aus einer historischen internen ID abgeleitet.
Die 3'066 offenen internen Nummern sind als `ALPHAPLAN-ID:<ID>` gekennzeichnet,
damit keine zufaellige Ueberschneidung mit fachlichen Kundennummern entsteht.
Vorhandene fachliche Nummernspalten im neuen Alphaplan-Export haben weiter Vorrang.
Die Branchenpruefung verwendet jetzt wirklich Gleichheit: `00 Bahn, 13 Flugzeugbau`
wird nicht automatisch bestaetigt. Der vorherige StartsWith-Code widersprach der Doku.

Funktionscommit `8a0cee6`, produktiv am 09.09. um 07:20, 687/687 Release-Tests gruen.
DLL lokal/Server bitgleich, vier Routen HTTP 200, Deploy ohne Alarm. Keine komplette
Standortersetzung: ausschliesslich Kundenfelder und DE-Segmentzuordnungen in einer
Transaktion nachgezogen. Vorherbestand und Segmente liegen in `.tmp_tools/DeApply0909/`.
Der erste Vergleich brach wegen unterschiedlicher Dezimalskalen ab; der korrigierte
numerische Vergleich bestand vor der Datenmutation. Ein langsamer Excel-Nachlauf
wurde nach belegtem DB-Commit beendet; CSV und Excel wurden danach separat geprueft
bereitgestellt. Die Sales-Excel wurde aus dem vorhandenen Export durch Aenderung der
Kunden-/Segmentzellen erzeugt, nicht mit ungeprueften Finance-Defaultregeln neu berechnet.

Nachweis am konsistenten Produktivsnapshot: SQLite-Checkpoint erfolgreich, Kopie
unter Schreibsperre bei leerer WAL, `quick_check=ok`. Alle 7'615 Zeilen behalten ihre
Finanzwerte, Mengen und Lieferantenschluessel; auch die Dashboard-CSV ist dagegen
geprueft. In der Sales-Excel sind ausser Kunden-/Segmentzellen alle Zellwerte und
Formeln unveraendert. Dashboardquelle im Serverordner `output` **zum Stand 07:20**:
`Sales_ProcessedMergeInput_TRDE_2026-09-09.csv`, SHA256
`F0F6B78D194EF48B44D68619AB218D92E61EB6F52B17B49B9BB030C46643994C`.
**Diese Pruefsumme ist seit dem 09.09. 09:33 ueberholt.** Der Nachzug der
RTF-Artikelbezeichnung (`46e8b5d`) hat in derselben Datei 3'089 Namenszellen
ersetzt; die Datei auf dem Server traegt seither eine andere Pruefsumme. Die
Angabe bleibt als Beleg fuer den damaligen Stand stehen.
`Sales_TRDE_2026-09-09.xlsx` liegt daneben. Beide Dateien sind ausserdem im
konfigurierten SharePoint-Ordner `Import/Finance/Deutschland/AlphaplanRaw` hochgeladen
und zurueckgelesen: CSV bytegleich, Excel saemtliche Zellwerte und Formeln gleich.
SharePoint hat an der Excel Klassifizierungsbeziehungen ergaenzt.

### Dateien zum Weitergeben

- `Bahnmarkt_Rohail_2026-09-09.xlsx`: aktuelle Gesamtauswertung mit demselben Dienst
  wie der Exportknopf auf `/marktsegmente`, alle Jahre und Standorte. Acht Blaetter:
  171 Vorschlaege, 20 bestaetigte Kunden (19 DE + 1 IN), 286 Umsatzsummenzeilen nach
  Jahr/Waehrung, 3'928 Detailzeilen, Marktumfrage und Datenluecken. Keine Kostenfelder.
- `Bahnmarkt_DE_Kundenzuordnung_2026-09-09.xlsx`: DE-Pruefmappe mit direktem
  Belegnachweis, historischen Kandidaten, Restliste und nach Jahr/Waehrung getrenntem
  Railway-Umsatz. Historische Kandidaten erscheinen nur im eigenen Vorschlagsblatt
  mit aufgeloestem Namen; im Verkaufsblatt bleiben sie offen.
- `Tools/DeCustomerMapping/README.md`: reproduzierbarer Ablauf fuer Nachzug,
  Gegenpruefung, Export und SharePoint-Verifikation.

Kein Versand an Rohail oder andere Personen erfolgt. MD und PM-08 im Wochen-Todo
nachgefuehrt. Beim Wochen-Todo wurde ausserdem die durch unsere Regeneration vom
08.09. verlorene Erlaeuterungsseite und der vorherige Stand der uebrigen
Zusammenfassung wiederhergestellt; die aktuelle TSV bleibt fuehrend.

**Fuer vollstaendige Zuordnung weiterhin erforderlich:** aktueller Alphaplan-Abgleich
zwischen interner RechnungsAdressenID und fachlicher Adressnummer, einschliesslich
Sonepar/EMS-Nummernwechsel. Eine vollstaendig gefuellte Kundenliste ist noch nicht belegt.

> Diese Forderung ist am 09.09.2026 um 12:51 erfuellt worden. Siehe Abschnitt 9.

## 9. Rohails Rechnungsliste schliesst die Luecke, 2026-09-09

Rohail hat um 12:51 `Rechnungen_20260909.xlsx` geliefert, 60'539 Rechnungen. Die Datei
traegt in Spalte H `Adressnr._R` die **fachliche** Kundennummer neben `Rech.-Nr.`. Damit
existiert die Bruecke, die Abschnitt 8 noch als offen gemeldet hat. Der Weg ueber die
interne `RechnungsAdressenID` wird dafuer nicht mehr gebraucht.

Der Beweis, dass es die fachliche und nicht die interne Nummer ist: `RE2611082` fuehrt auf
`13859`, und diese Nummer steht im Kundenstamm bei „Magnetic Sense Automotive GmbH" mit der
Branche `29 Drehmomentanwendungen` — genau den Werten, die Rohails Zeile ebenfalls traegt.
Die internen IDs liegen dagegen im Bereich 9 bis 13990.

### Vier Pruefungen

| Pruefung | Ergebnis |
|---|---|
| Abdeckung der 7'615 DE-Verkaufszeilen | 7'531, also 98,9 Prozent |
| Uebereinstimmung mit dem bestehenden Belegnachweis | 4'455 gleich, 40 abweichend |
| Unabhaengiger Gegentest gegen `docs/2025_DataExport_DE.xlsx` | 2'912 von 2'922, also 99,66 Prozent |
| Neue Adressnummern im Kundenstamm auffindbar | 358 von 358, keine Waise |

Die 40 Abweichungen sind ausschliesslich die drei bekannten Doppelnummern: Sonepar
(12028/12717), EMS (14529/40334) und neu Magnetic Sense (13561 „GmbH" gegen 13859
„Automotive GmbH"). Alle drei sind **keine** Bahnkunden und aendern die Bahnauswertung
nicht. Welche der beiden Nummern fachlich richtig ist, ist offen und gehoert mit Rohail
geklaert; die Mappe weist beide im Blatt `Nummernkonflikte` aus.

### Was Rohails Datei nicht liefert

**Gutschriften.** Die Liste enthaelt 46'372 `RE`-, 8'936 `R0`- und 5'231 `R9`-Belege, aber
keinen einzigen `GS`-Beleg. 84 Verkaufszeilen bleiben deshalb ohne Eintrag in der Bruecke.
Fuer 54 davon gilt weiter die Nummer aus dem frueheren Belegnachweis, sodass nur 30 Zeilen
wirklich offen bleiben. Ein Auszug fuer Gutschriften wuerde auch diese schliessen.

**Eine brauchbare Branchenspalte.** In 5'785 Zeilen steht dort eine Zahl statt einer
Branche, ueberwiegend bei Adressen ohne gepflegte Stammbranche; Alphaplan verschiebt beim
Export die letzten Spalten. Die Branche wird deshalb aus dem Kundenstamm gezogen, nicht aus
der Rechnungsliste. Die Spalten H, J, L und T sind davon nicht betroffen, was die 98,8
Prozent Laenderuebereinstimmung gegen den Kundenstamm belegen.

### Neu erzeugte Pruefmappe

`Bahnmarkt_DE_Kundenzuordnung_2026-09-09.xlsx` ist auf dieser Grundlage neu gebaut. Der
vorherige Stand liegt als
`.tmp_tools/DeBridge0909/Bahnmarkt_DE_Kundenzuordnung_2026-09-09.vor-rohail-bruecke.xlsx`.

| Kennzahl | vorher | jetzt |
|---|---:|---:|
| Verkaufszeilen | 7'615 | 7'622 |
| mit fachlicher Kundennummer | 4'549 | 7'592 |
| ohne Zuordnung | 3'066 | 30 |
| Bahnkunden mit reiner Bahnbranche | 19 | 24 |
| Bahnzeilen | — | 657 |

Die Spalte „vorher" ist der produktive Stand von 07:20 mit 7'615 Zeilen, die Spalte
„jetzt" der Mappenstand mit 7'622 Zeilen; die sieben zusaetzlichen sind die Rechnungen
`RE2611081` und `RE2611082`, die nach dem Archivstand dazugekommen sind.

Deutscher Bahnumsatz, erstmals belegbar: **2025 rund 578'636 EUR** bei 19 Kunden und
**2026 bis zum 09.09. rund 432'237 EUR** bei 18 Kunden. Neu sichtbar sind unter anderem
Wabtec Germany, Knorr-Bremse, Noske-Kaeser, Autokuehler und die Eisenbahngesellschaft
Ostfriesland-Oldenburg.

Jede Zeile traegt ihren Nachweis. Die Verteilung ist 4'455 „Belegnachweis und
Rechnungsliste, uebereinstimmend", 3'043 „Rechnungsliste Rohail 09.09.", 54 „frueherer
Belegnachweis, Gutschrift", 40 Nummernkonflikte und 30 offen.

### Wie die Mappe gebaut wurde

`Tools/BahnmarktDeWorkbook` liest die Artikelbezeichnung **nicht** selbst, sondern
ueber `ManualExcelDataSourceAdapter` und `ManualExcelImportService`, also denselben Weg wie
der produktive Import. Damit traegt die Mappe denselben Text wie die produktiven Dateien
seit dem Nachzug um 09:33; der Schriftrest ist von 3'089 Zeilen auf null gefallen. Zwei
Abbruchbedingungen sind eingebaut: verbleibender RTF-Schriftrest und der Verlust einer
bereits belegten Kundennummer.

Zeilenquelle ist der produktive Leser ueber `ManualExcelDataSourceAdapter`, nicht die
archivierte CSV. Das ist kein Detail: dem um 07:22 abgelegten Archivstand fehlten bereits
die Rechnungen `RE2611081` und `RE2611082` mit sieben Positionen. Die CSV dient nur noch
als Gegenprobe, und eine dort vorhandene, in der Mappe fehlende Zeile bricht den Lauf ab.

Gegenprobe nach dem Bau: alle 7'615 Zeilen des Archivstands tragen unveraenderte Menge,
`SalesPriceValue` und Waehrung; null Abweichungen, sieben zusaetzliche Zeilen.

### Was noch offen ist

1. **Der produktive Nachzug ist nicht erfolgt.** Datenbank und Dashboard weisen die 3'043
   Zeilen weiter als `ALPHAPLAN-ID:<ID>` aus. Die Mappe ist damit bewusst weiter als der
   Produktivstand; der Lesehinweis sagt das in der Datei selbst.
2. **Gutschriftenauszug** von Rohail erbitten, dann sind es 100 statt 99,6 Prozent.
3. **Die drei Doppelnummern** fachlich klaeren.
4. **Der dauerhafte Weg bleibt der Join in der Quelle.** `alphaplanExport.ps1` Zeile 164
   liest `FROM dbo.Belege` ohne Verbindung zur Adresstabelle. Solange das so bleibt, ist
   jede Bruecke eine Momentaufnahme und muss bei neuen Rechnungen wiederholt werden.
5. Nicht versendet. Die Datei liegt bereit, an Rohail ist nichts gegangen.