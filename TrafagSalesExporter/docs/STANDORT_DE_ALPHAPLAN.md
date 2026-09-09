# Standort Deutschland: Alphaplan-Export und Import

Stand: 2026-09-09 (Abschnitt 8 neu, Abschnitte 5 und 6 nachgefuehrt).
Zusammengefuehrt aus `ALPHAPLAN_DISCOVERY_EXPORTER_GUIDE_2026-06-08.md`
und `ALPHAPLAN_SQL_RCLONE_KONZEPT_DE_2026-06-08.md`.

**Die Export-SQL gehoert uns**, nicht Deutschland. Fehlt ein Feld, liest zuerst unsere
Query es nicht — siehe `docs/FINANCE_FELDLUECKEN.md` Abschnitt 1 und 6.

## 1. Aktueller Weg: CSV-Paar plus Delta

Der Export laeuft **auf dem deutschen Alphaplan-/SQL-Server**, nicht auf dem
BiDashboard-Server. Damit braucht der BiDashboard-Server keinen SQL-Zugriff auf Alphaplan.

Erzeugt werden zwei Dateien:

| Datei | Inhalt |
| --- | --- |
| `invoice_headers.csv` | Rechnungskoepfe, Belegkopfwert `NettoPreisEndSumme` |
| `invoice_lines.csv` | Rechnungspositionen, Finance-Wert `NettoPreisGesamt` |

Der Vollbestand liegt im Alphaplan-Ordner, der 7-Tage-Rueckblick im Unterordner `delta`
mit **denselben Dateinamen**.

Verarbeitung durch die App:

- Header und Positionen werden ueber `BelegeID` verbunden.
- Dedupe primaer ueber `BelegePositionenID` als `SourceLineId = Alphaplan:<id>`, sonst
  ueber Invoice, Position und Material. **Bei gleicher Zeile gewinnt das Delta gegen den
  Vollbestand.**
- Der Import **ersetzt** den DE-Bestand in `CentralSalesRecords` durch das
  zusammengesetzte, deduplizierte Ergebnis.
- Gutschriften werden negativ gerechnet.

**Ein einzelnes Delta darf nie isoliert als Standortbestand importiert werden**, weil der
Standortimport den DE-Bestand ersetzt. Der Vollbestand muss im Root des Ordners liegen.

## 2. SharePoint und ZIP-Import

Produktiver Pfad: `Import/Finance/Deutschland/AlphaplanRaw`.
`TRDE.ManualImportFilePath` zeigt dorthin.

Der Import erkennt dort neben direkten CSV-Paaren auch `Alphaplan*.zip`. ZIPs werden
heruntergeladen, temporaer entpackt und rekursiv nach dem Dateipaar durchsucht. ZIPs mit
`Delta` im Dateinamen werden wie ein Delta-Unterordner behandelt.

**Parser-Besonderheit:** Alphaplan-CSV wird **ohne Quote-Sonderbehandlung** gelesen, weil
Artikeltexte unescaped doppelte Anfuehrungszeichen enthalten koennen. Semikolon bleibt
Trennzeichen.

**Betriebsregel:** Der Alphaplan-Task auf dem DE-Server muss **vor** dem BiDashboard-Timer
laufen. Am 2026-07-02 lag der ZIP-Upload gegen 13:10 Zuerich und damit nach dem Timer um
12:00 — die Daten des Tages fehlten dadurch.

Produktivnachweis 2026-07-03: SharePoint-Import lieferte `6'612` DE-Zeilen, davon
`4'547` fuer 2025 und `2'065` fuer 2026.

## 3. Skripte

| Datei | Zweck |
| --- | --- |
| `AlphaplanExportPackage/scripte/alphaplanExport.ps1` | Vollexport, enthaelt die Query |
| `AlphaplanExportPackage/scripte/alphaplandeltaexport.ps1` | Delta-Export, **identische Query** |
| `AlphaplanExportPackage/scripte/fullquery.sql` | Query als eigenstaendige Datei |
| `AlphaplanExportPackage/Run-AlphaplanDiscoveryAndUpload.ps1` | Schema-Discovery, historisch |
| `AlphaplanExportPackage/scripte/ANLEITUNG_KORREKTUR_2026-06-24.md` | Einrichtung auf dem DE-Server |

**Die Query steht zweimal.** Aenderungen immer an beiden Stellen nachziehen, sonst laufen
Voll- und Delta-Export auseinander.

Gelesen werden ausschliesslich `dbo.Belege` und `dbo.BelegePositionen`.

## 4. Betrieb auf dem DE-Server

Empfohlener Ordner `C:\Trafag\AlphaplanExport` mit Unterordnern `out` fuer CSV und `logs`
fuer Script- und rclone-Logs. Taeglicher Task frueh morgens, Exitcode und Logdatei pruefen,
Upload per `rclone lsf` verifizieren.

Benoetigt auf dem DE-Server: lokaler read-only SQL-Zugriff auf Alphaplan (keine
Schreibrechte, idealerweise nur auf die benoetigten Views) und ausgehend HTTPS/443 zu
Microsoft 365 fuer `rclone`.

## 5. Feldabbildung

| Zielfeld | Quelle / Bedeutung |
| --- | --- |
| `TSC` / `Land` / `SourceSystem` | `TRDE` / `Deutschland` / `Alphaplan` |
| `InvoiceNumber`, `PositionOnInvoice` | Rechnungs- und Positionsnummer |
| `Material` | `ArtikelNummer`, **lokale** Alphaplan-Nummer |
| `Name` | Artikeltext aus dem RTF-Feld der Belegposition; Schrift- und Farbtabelle werden beim Import entfernt, siehe Abschnitt 8 |
| `Quantity` | Menge |
| `CustomerNumber` | Kundennummer |
| `SalesPriceValue` | `NettoPreisGesamt` der Position |
| `PostingDate`, `InvoiceDate` | Buchungs- und Rechnungsdatum |
| `DocumentType` | Rechnung, Gutschrift, Storno |
| `StandardCost` | abgeleitet: `NettoPreisGesamt - RohertragGesamt`, geteilt durch die Menge |

**`ArtikelNummer` ist nicht automatisch identisch mit der TR-AG-/SAP-`MATNR`.** Das ist
seit 2026-06-01 unbelegt und die einzige echte Fachfrage an Deutschland. Da die
Produktsparte zentral ueber die Materialnummer gegen die TR-AG-Referenz gematcht wird,
haengt die Spartenabdeckung genau daran.

## 6. Offene Punkte

- **Blocker: fehlendes Alphaplan-Schema.** Es gibt keine Tabellen- und Spaltenliste fuer
  `ApDaten`. `candidate_objects.csv` im Repo-Root ist nur eine Kopfzeile,
  `obj/candidate_objects.csv` ist Sage Spanien. Die DB liegt auf `localhost\SQL2012` des
  DE-Servers hinter einem DPAPI-gebundenen Credential. **Keine Tabellennamen erfinden** —
  benoetigt wird ein read-only `INFORMATION_SCHEMA.COLUMNS`-Auszug, gefiltert auf
  `%Adress%`, `%Artikel%`, `%Liefer%`, `%Kunde%`.
- Danach die Query selbst erweitern: Kundenname und -land (`RechnungsAdressenID` wird
  selektiert, aber nie aufgeloest) und die Lieferantenquelle. Der frueher hier genannte
  Punkt „saubere Bezeichnung aus dem Artikelstamm statt des RTF-Felds“ ist **ueberholt**:
  Der Schriftmuell entstand nicht in der Quelle, sondern beim Lesen, und ist seit dem
  09.09.2026 im Import behoben (Abschnitt 8). Ein Artikelstammtext ist dafuer nicht noetig.
- Fachfrage an Deutschland: Ist `ArtikelNummer` gleich der TR-AG-/SAP-`MATNR`?
- Offen, ob Alphaplan ueberhaupt einen Lieferanten auf der **Verkaufszeile** fuehrt oder
  nur einen Hauptlieferanten im Artikelstamm.

Ansprechpartner: Rohail, siehe `docs/ANSPRECHPARTNER.md`. Die DE-Korrespondenz laeuft auf
Deutsch.

## 7. Historisch: die Discovery-Phase

Das urspruengliche Phase-1-Paket
(`Run-AlphaplanDiscoveryAndUpload.ps1`) scannte SQL-Datenbanken, Tabellen und Views,
bewertete Kandidaten und schrieb `candidate_objects.csv` und `export_summary.csv`.
**Das ist nicht mehr der aktive Pfad** — die App liest das finale Header-/Line-Paarformat.
Das Discovery-Skript bleibt nur als Werkzeug fuer eine erneute Schemaerhebung nuetzlich.

## 8. RTF-Schriftmuell in der Artikelbezeichnung, Ursache und Korrektur

Alphaplan speichert `ArtikelBezeichnung` als RTF. Die Export-SQL nimmt das Feld roh mit und
ersetzt nur Zeilenumbrueche und Semikolons
(`AlphaplanExportPackage/scripte/alphaplanExport.ps1`, Zeile 183). Der Import hat die
RTF-Steuerworte und die Klammern entfernt, **nicht aber den Inhalt der Zielgruppen**
`\fonttbl` und `\colortbl`. Uebrig blieben die Schriftnamen, und die Bezeichnung begann mit
`MS Shell Dlg, Microsoft Sans Serif, , ,`. Die Kommas stehen dort, weil die Export-SQL die
Semikolons der RTF-Tabellen durch Kommas ersetzt.

Gemessen am geprueften Produktivsnapshot vom 09.09.2026: **3'089 von 7'615 TRDE-Zeilen**,
davon 2'922 mit `MS Shell Dlg` und 167 nur mit `Microsoft Sans Serif`. Betroffen war
ausschliesslich die Spalte `Name`. `Material`, `CustomerName`, `SupplierName` und
`ProductGroup` waren sauber, und kein anderer Standort war betroffen, weil nur Alphaplan
RTF liefert. Sichtbar war der Rest in jeder Dashboardanzeige der Artikelbezeichnung, in
`Sales_ProcessedMergeInput_TRDE_*.csv` als Feld 9, in `Sales_TRDE_*.xlsx` und in
`Bahnmarkt_DE_Kundenzuordnung_2026-09-09.xlsx`, dort in Spalte G der Blaetter
`DE_Verkaeufe`, `Offene_Zuordnungen` und `Historisch_abgeleitet`.

**Falle:** Die Gruppen muessen klammerbalanciert entfernt werden. Ein einfaches
`{\fonttbl.*?}` endet an der ersten schliessenden Klammer und laesst den zweiten
Schrifteintrag stehen — genau das Muster der 167 Zeilen, die nur `Microsoft Sans Serif`
tragen. Wer nur die 2'922 verschwinden sieht, haelt den Rest fuer eine Quelleigenheit.

Korrigiert in `NormalizeAlphaplanText` (`Services/ManualExcelImportService.cs`): Die
Zielgruppen `fonttbl`, `colortbl`, `stylesheet`, `listtable`, `listoverridetable`, `info`,
`generator`, `themedata`, `colorschememapping` und `datastore` sowie jedes Zusatzziel
`{\*\...}` werden klammerbalanciert entfernt, bevor die Steuerworte fallen. Maskierte
Klammern `\{` und `\}` verschieben die Zaehlung nicht. Regressionstest mit zwei echten
Belegtexten, einer mit zwei Schrifteintraegen und einer mit einem. Funktionscommit
`eb8cd43`, 688/688 Release-Tests gruen.

Den Bestand zieht `Tools/DeNameFix` nach. Das Werkzeug liest die Rohdateien mit demselben
Importdienst und demselben SharePoint-Adapter wie der produktive Lauf, gleicht ueber Beleg,
Rechnungsnummer, Position und Artikelnummer ab und aendert ausschliesslich die Spalte
`Name` in einer Transaktion. Zwei Sicherungen greifen: der neue Wert muss ein Endstueck des
gespeicherten Wertes sein, sonst Abbruch, und die Datenbank darf sich zwischen Probelauf
und Anwendung nicht geaendert haben. Kein Loeschen und kein Neuladen von Verkaufszeilen.
Eine komplette Standortersetzung wuerde `ExtractionDate` und die abgeleiteten
Standardkosten mitziehen und den Nachweis „nur `Name` hat sich geaendert“ unmoeglich
machen.

Probelauf gegen den Snapshot vom 09.09.2026: 7'615 von 7'615 Zeilen ueber den
Zeilenschluessel getroffen, genau 3'089 geplante Aenderungen, null Verstoesse gegen die
Endstueck-Regel, null mehrdeutige Rohzeilen und kein verbleibender Schriftrest. Mit der
lokalen Rohkopie vom 12.06.2026 allein waeren nur 2'684 Zeilen erreichbar gewesen; die
restlichen 405 stecken in den Delta-Archiven auf SharePoint. Deshalb liest das Werkzeug im
Regelfall mit `--sharepoint` denselben Ordner wie der Import.

**Offen am 09.09.2026: Deploy und produktiver Nachzug.** Der Firmenshare
`\\trch-webapp-bidashboard.trafagch.local\BiDashboard$` war an diesem Abend nicht
erreichbar, der DNS-Name loeste nicht auf. Bis der Deploy laeuft, erzeugt jeder neue DE-Import den Schriftrest erneut; der Nachzug allein
wuerde also nicht halten. Der Standort muss dafuer nichts liefern, die Ursache lag
vollstaendig bei uns.

Die Standort-Mails aus 07/2026 (`docs/mails/Build-StandortMails.ps1`) nennen weiterhin
2'903 von 7'171 Texten mit Formatierungstext. Das ist der Stand des damaligen Versands und
wird als datierte Historie nicht umgeschrieben.

## Querverweise

- Manual-Import und Dedupe: `docs/rag/MANUAL_IMPORT.md`
- Feldluecken und Eigentuemerfrage: `docs/FINANCE_FELDLUECKEN.md`
- Standardkostenableitung DE: `docs/FINANCE_STANDARDKOSTEN.md`
