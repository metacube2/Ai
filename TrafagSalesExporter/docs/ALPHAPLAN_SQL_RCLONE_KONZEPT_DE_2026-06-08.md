# Alphaplan SQL/rclone Konzept Deutschland

Stand: 2026-06-08

Ziel: Auf dem deutschen Alphaplan-Server sollen SQL-Tabellen oder Views direkt als CSV exportiert und anschliessend mit `rclone` nach SharePoint hochgeladen werden. Der BiDashboard-/Finance-Import wird danach separat angepasst. Es werden in diesem Konzept keine Programmcode-Aenderungen am BiDashboard beschrieben.

## Umsetzungsstand 2026-06-08

- Phase-1-Discovery-Paket erstellt: `AlphaplanExportPackage`.
- Kopierbares ZIP erstellt: `AlphaplanExportPackage.zip`.
- Hauptscript: `AlphaplanExportPackage/Run-AlphaplanDiscoveryAndUpload.ps1`.
- Kurzanleitung im Paket: `AlphaplanExportPackage/README.txt`.
- Detailanleitung: `docs/ALPHAPLAN_DISCOVERY_EXPORTER_GUIDE_2026-06-08.md`.
- Status: PowerShell-Syntax lokal geprueft; noch kein Lauf auf dem deutschen Alphaplan-Server.
- Abgrenzung: Noch keine BiDashboard-Importanpassung und kein produktiver Deutschland-Import aus Alphaplan-Rohdaten.

## Kurzfazit

- Der Export soll auf dem Alphaplan-/SQL-Server in Deutschland laufen, nicht auf dem BiDashboard-Server.
- Der erste Schritt ist bewusst ein Rohdaten-Export von ausgewaehlten SQL-Tabellen oder Views.
- Upload erfolgt analog Spanien mit `rclone` nach SharePoint.
- Fuer Rohdaten sollte ein eigener SharePoint-Unterordner verwendet werden, damit der aktuelle Deutschland-Import nicht versehentlich falsche CSV-Dateien einliest.
- Spaeter kann aus den erkannten Alphaplan-Tabellen eine stabile Finance-Export-View oder ein finaler gemappter CSV-Export entstehen.

## Annahmen

- Alphaplan verwendet Microsoft SQL Server oder eine SQL-Server-kompatible Datenbank.
- Das Export-Script laeuft lokal auf dem deutschen Server mit Zugriff auf die Alphaplan-Datenbank.
- Der Export benoetigt nur Leserechte.
- `rclone` ist auf dem deutschen Server installiert und fuer die SharePoint-Dokumentbibliothek konfiguriert.

## Zielbild Datenfluss

1. Windows Task Scheduler startet das Alphaplan-Export-Script auf dem DE-Server.
2. Das Script verbindet sich lokal oder intern mit der Alphaplan-SQL-Datenbank.
3. Das Script exportiert definierte Tabellen/Views als CSV in einen lokalen Laufordner.
4. Das Script erstellt eine Summary-Datei mit Server, Datenbank, exportierten Objekten, Zeilenanzahl und Fehlern.
5. `rclone` prueft oder erstellt den SharePoint-Zielordner.
6. `rclone` laedt CSV und Summary nach SharePoint hoch.
7. Der BiDashboard-Import wird spaeter auf diese SharePoint-Dateien angepasst.

## SharePoint-Ziel

Empfohlen fuer Rohdaten:

`trafag-bi:Import/Finance/Deutschland/AlphaplanRaw`

Begruendung:

- Der aktuelle Deutschland-Import kann weiterhin unveraendert bleiben.
- Rohdaten-CSV aus Tabellen/Views haben noch nicht zwingend das finale Finance-Spaltenformat.
- Wenn der Import spaeter fertig ist, kann das Ziel entweder direkt weiterverwendet oder auf `Import/Finance/Deutschland` umgestellt werden.

Fuer einen spaeteren finalen Finance-CSV-Export waere passend:

`trafag-bi:Import/Finance/Deutschland`

## Export-Phasen

### Phase 1: Discovery

Ziel: Herausfinden, welche Alphaplan-Datenbanken, Tabellen und Views relevant sind.

Ergebnisdateien:

- `candidate_objects.csv`
- `export_summary.csv`
- optional kleine Beispiel-CSV je Kandidat

Kandidaten sollten anhand von Namen und Spalten gefunden werden, z. B.:

- Rechnung / Faktura / Beleg
- Auftrag / Lieferschein
- Position / Zeile
- Kunde / Adresse
- Artikel / Material
- Warengruppe / Produktgruppe
- Menge / Preis / Netto / Umsatz
- Datum / Belegdatum / Rechnungsdatum

### Phase 2: Rohdaten-Export

Ziel: Ausgewaehlte Alphaplan-Tabellen oder Views komplett oder zeitlich gefiltert exportieren.

Typische Exportdateien:

- `Alphaplan.<schema>.<table>.csv`
- `Alphaplan.<schema>.<view>.csv`
- `export_summary.csv`

Die Dateien muessen noch nicht dem Finance-Importformat entsprechen. Wichtig ist, dass Andreas/IT daraus die richtigen Tabellen, Schluessel und Felder erkennen kann.

### Phase 3: Finaler Finance-Export

Ziel: Sobald die Tabellenbeziehungen klar sind, wird eine stabile SQL-View oder ein finaler Query definiert, der direkt Finance-taugliche Spalten liefert.

Empfohlen ist eine read-only View, z. B.:

`vw_BiDashboard_FinanceSales_DE`

Diese View sollte pro Rechnungsposition eine Zeile liefern. Dann bleibt das Export-Script einfach und stabil, auch wenn Alphaplan intern mehrere Tabellen benoetigt.

## Benoetigte Informationen von Deutschland/IT

- SQL Server Hostname/Instanz.
- Datenbankname.
- Authentifizierung: Windows Integrated oder SQL User.
- Read-only Benutzer oder Service Account.
- Liste relevanter Tabellen/Views fuer Rechnungen und Rechnungspositionen.
- Datumsspalte fuer Voll-/Delta-Export.
- Kennzeichen fuer Rechnung, Gutschrift, Storno.
- Waehrungsfeld und Standardwaehrung.
- Netto-Umsatzfeld auf Positionsebene.
- Artikelnummer/Materialnummer.
- Kundenland und Kundenname.
- Produktgruppe/Warengruppe, falls in Alphaplan vorhanden.

## Minimal benoetigte Finance-Felder spaeter

Fuer den spaeteren Import reichen als Startpunkt:

| Zielfeld | Bedeutung |
| --- | --- |
| `TSC` | `TRDE` |
| `Land` | `Deutschland` |
| `SourceSystem` | `Alphaplan` |
| `InvoiceNumber` | Rechnungsnummer |
| `PositionOnInvoice` | Positionsnummer |
| `Material` | Artikel-/Materialnummer |
| `Name` | Artikeltext |
| `Quantity` | Menge |
| `CustomerNumber` | Kundennummer |
| `CustomerName` | Kundenname |
| `CustomerCountry` | Kundenland |
| `SalesPriceValue` | Nettoumsatz Positionszeile |
| `SalesCurrency` | Umsatzwaehrung |
| `PostingDate` | Buchungs-/Belegdatum |
| `InvoiceDate` | Rechnungsdatum |
| `DocumentType` | Rechnung/Gutschrift/Storno |

Weitere Felder wie Warengruppe, Lieferant, Incoterms, Auftrag, Branche und Kosten koennen spaeter ergaenzt werden.

## Delta oder Vollfile

Fuer den Anfang ist ein Vollfile pro Jahr am einfachsten:

- weniger Risiko bei geaenderten oder stornierten Belegen
- einfacher Abgleich gegen Alphaplan
- Import kann pro Standort/Jahr neu aufgebaut werden

Delta ist erst sinnvoll, wenn klar ist:

- welche Spalte neue/geaenderte Datensaetze erkennt
- wie Stornos und Korrekturen geliefert werden
- ob der Import vorhandene DE-Daten mergen oder ersetzen soll

Wichtig: Wenn der aktuelle Import Standortdaten komplett ersetzt, darf kein kleines Delta so importiert werden, als waere es ein Vollbestand.

## Sicherheit und Firewall

Da der Export auf dem deutschen Alphaplan-Server laeuft, muss der BiDashboard-Server keinen direkten SQL-Zugriff auf Alphaplan bekommen.

Benoetigt auf dem DE-Server:

- SQL Zugriff lokal/intern auf Alphaplan.
- Ausgehend HTTPS/TCP 443 zu Microsoft 365/SharePoint fuer `rclone`.
- Optional Zugriff fuer rclone OAuth/Device Login bei der Erstkonfiguration.

SQL Benutzer:

- nur read-only
- keine Schreibrechte
- idealerweise Zugriff nur auf benoetigte Views oder Tabellen

## Betrieb

Empfohlener lokaler Ordner auf dem DE-Server:

`C:\Trafag\AlphaplanExport`

Unterordner:

- `out` fuer CSV-Exports
- `logs` fuer Script- und rclone-Logs

Empfohlener Task:

- taeglich frueh morgens
- zuerst Voll-/Jahresfile, spaeter optional Range
- Exitcode und Logdatei pruefen
- Upload per `rclone lsf` verifizieren

## Abnahmekriterien

Der technische Export gilt als bereit, wenn:

- Discovery-Datei mit Kandidatentabellen/Views auf SharePoint liegt.
- Mindestens ein relevanter Tabellen-/View-Export auf SharePoint liegt.
- Summary-Datei Zeilenanzahl und Quelle zeigt.
- rclone Upload im Log ohne Fehler abgeschlossen ist.
- Deutschland/IT bestaetigt, welche Tabellen/Views fuer Finance korrekt sind.

Der Finance-Import ist danach ein separater Schritt.
