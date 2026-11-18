# Standort Deutschland: Alphaplan-Export und Import

Stand: 2026-08-17. Zusammengefuehrt aus `ALPHAPLAN_DISCOVERY_EXPORTER_GUIDE_2026-06-08.md`
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
| `Name` | Artikeltext, aktuell aus dem Rich-Text-Feld der Belegposition |
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
  selektiert, aber nie aufgeloest), Lieferantenquelle, saubere Bezeichnung aus dem
  Artikelstamm statt des RTF-Felds (2'903 von 7'171 Texten mit Schriftmuell).
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

## Querverweise

- Manual-Import und Dedupe: `docs/rag/MANUAL_IMPORT.md`
- Feldluecken und Eigentuemerfrage: `docs/FINANCE_FELDLUECKEN.md`
- Standardkostenableitung DE: `docs/FINANCE_STANDARDKOSTEN.md`
