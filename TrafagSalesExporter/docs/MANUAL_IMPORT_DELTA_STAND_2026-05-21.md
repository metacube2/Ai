# Manual-Import und Delta-Stand

Stand: 2026-06-12

Diese Datei beschreibt, wie manuelle Excel-/CSV-Importe aktuell behandelt werden und wie neue Eintraege bzw. Delta-Dateien verarbeitet werden.

## Uebersicht

| Land / Standort | Quelle aktuell | Dateityp | Neue Eintraege / Deltas | Wie die App auswaehlt | Was beim Standortexport passiert | Finance-Wert |
| --- | --- | --- | --- | --- | --- | --- |
| UK / England `TRUK` | SharePoint-Ordner `Import/Finance/UK_B1` | Sage Excel `.xlsx` | Delta-faehig | Bei Jahreslauf: Jahresdatei fuer `TRUK` plus spaetere datierte Dateien `ddMMyy_TRUK.xlsx` oder `.csv` | Alle gefundenen Dateien werden gelesen und zusammen in `CentralSalesRecords` fuer `TRUK` ersetzt | `Sales Price/Value * Quantity`, Credit Notes negativ, GBP |
| Spanien `TRSE` / `TRES` | SharePoint-Ordner `Import/Finance/Spanien` / Sage CSV | `.csv` | Delta-faehig mit Basis + `Spain_Sales_range_YYYYMMDD_to_YYYYMMDD.csv` | Wenn Ordner: alle `Spain_Sales*.csv`, Basis zuerst, danach Range-Dateien nach Datum | Alle Dateien werden gelesen, nach `SourceLineId` bzw. Invoice/Position/Material dedupliziert, danach ersetzen die deduplizierten Spanien-Zeilen den bisherigen Spanien-Stand | Sage `SalesPriceValue`/`ImporteNeto`, REC/Abono/Credit negativ, EUR |
| Deutschland `TRDE` | Alphaplan CSV-Paar | `.csv` | Vollbestand plus 7-Tage-Delta im Unterordner `delta` | Ordner/SharePoint-Pfad enthaelt `invoice_headers.csv` und `invoice_lines.csv`; passende Paare werden rekursiv gefunden | Vollbestand und Delta werden gelesen, nach Alphaplan-Zeilen-ID dedupliziert, danach ersetzen die deduplizierten DE-Zeilen den bisherigen DE-Stand | `NettoPreisGesamt`, CreditNote/GS negativ, EUR |
| CH/AT `ZSCHWEIZ` | SAP OData | OData | Kein manueller Delta-Excel-Prozess | App liest SAP-Service | ZSCHWEIZ-Zeilen ersetzen bisherigen Stand | `NetwrHc`, CHF/EUR nach Land |
| FR / IT / US | HANA / SAP B1 | direkte DB | Kein manueller Delta-Excel-Prozess | App liest HANA nach Datum/Schema | Standortdaten werden neu aus HANA aufgebaut | B1 Positions-Netto, Credit Notes negativ |
| IN | HANA / Sage | direkte DB | Kein manueller Delta-Excel-Prozess | App liest HANA/Sage | Standortdaten werden neu aus Quelle aufgebaut | Hauswaehrung INR |

## UK / England Delta-Mechanik

UK ist aktuell am besten fuer laufende Delta-Lieferungen vorbereitet.

| Punkt | Aktuelle Logik |
| --- | --- |
| Standort | `England`, `TSC = TRUK`, `SourceSystem = MANUAL_EXCEL` |
| SharePoint-Ordner | `Import/Finance/UK_B1` |
| Basisdatei | Jahresdatei im SharePoint-Ordner, z. B. mit Jahr `2025` im Namen |
| Delta-Dateien | Datierte Dateien wie `010526_TRUK.xlsx` oder `010526_TRUK.csv` |
| Auswahl | Jahresdatei zuerst, danach alle spaeteren Delta-Dateien im gleichen Jahr |
| Import | App liest alle ausgewaehlten Dateien in einem Lauf zusammen |
| Persistenz | `CentralSalesRecords` fuer `TRUK` werden ersetzt, nicht blind additiv angehaengt |
| Audit-CSV | optional `Sales_ProcessedMergeInput_TRUK_<Datum>.csv` nach Mapping/Transformation |
| Nach Delta-Lieferung | Delta-Datei in den Ordner legen, `TRUK` exportieren, danach zentrale Excel neu erzeugen |

Wichtig:

- Der Ordnername `UK_B1` ist nur technisch/historisch. Fachlich ist UK Sage, nicht SAP B1.
- UK nutzt grafisches Manual-Excel-Mapping.
- Der Finance-Wert wird aus `Sales Price/Value * Quantity` gebildet.
- Credit Notes werden anhand erkennbarer Sage-Typen negativ gesetzt.

## Spanien

Spanien nutzt technisch ebenfalls `MANUAL_EXCEL`, fachlich aber Sage CSV.

Aktueller Implementierungsstand:

- Datei/Ordner kann ueber SharePoint oder lokal hinterlegt werden.
- Bei Spanien-Ordnern werden alle `Spain_Sales*.csv` gelesen, auch wenn der Dateiname kein `TRES` enthaelt.
- Basis-/Vollfiles werden vor Range-Dateien gelesen.
- Range-Dateien wie `Spain_Sales_range_20260528_to_20260603.csv` werden nach Range-Start/Ende sortiert.
- Die App dedupliziert die gelesenen Zeilen vor dem Speichern:
  - primaer ueber `SourceLineId`.
  - Fallback ueber `TSC + InvoiceNumber + PositionOnInvoice + Material`.
- Beim Standortexport ersetzt die App weiterhin den bisherigen Spanien-Stand in `CentralSalesRecords`, aber mit dem zuvor zusammengesetzten und deduplizierten Gesamtstand.
- Falls Audit-CSV aktiv ist, schreibt der Export zusaetzlich `Sales_ProcessedMergeInput_<TSC>_<Datum>.csv` in den Standort-Exportordner und laedt sie in denselben SharePoint-Landesordner wie die Standort-Excel.
- Wenn nur eine einzelne Delta-Datei direkt als Dateipfad hinterlegt wird, kann weiterhin nur dieses Delta gelesen werden. Fuer Delta-Sync muss deshalb der Ordner hinterlegt sein.

Finance-Logik:

- `SalesPriceValue = ImporteNeto`
- `REC`, Abono bzw. Credit-Faelle werden negativ.
- Waehrung ist EUR.

## Deutschland

Deutschland nutzt Alphaplan CSV-Paare aus `invoice_headers.csv` und `invoice_lines.csv`.

Aktueller Implementierungsstand:

- Standort `TRDE` ist als `MANUAL_EXCEL` vorbereitet.
- Der Vollbestand liegt direkt im Alphaplan-Ordner.
- Der 7-Tage-Rueckblick liegt im Unterordner `delta` mit denselben Dateinamen.
- Die App sucht lokal und in SharePoint rekursiv nach Paaren aus `invoice_headers.csv` und `invoice_lines.csv`.
- Header und Positionen werden ueber `BelegeID` verbunden.
- Reihenfolge: Vollbestand zuerst, danach Delta.
- Dedupe: primaer `SourceLineId = Alphaplan:<BelegePositionenID>`, sonst `TSC + InvoiceNumber + PositionOnInvoice + Material`.
- Delta-Zeilen gewinnen gegen Zeilen aus dem Vollbestand.
- Beim Standortexport ersetzt die App den bisherigen Deutschland-Stand in `CentralSalesRecords` mit dem zusammengesetzten, deduplizierten Gesamtstand.
- Der Datumsfilter kommt aus den Export-Einstellungen; Default ist ab `2025-01-01`.
- Das alte Alphaplan-Excel-Mapping bleibt technisch vorhanden, ist aber nicht der bevorzugte DE-Pfad.

Finance-Logik:

- `SalesPriceValue = NettoPreisGesamt` aus `invoice_lines.csv`.
- `DocumentTotal... = NettoPreisEndSumme` aus `invoice_headers.csv`.
- `VatSum... = BruttoPreisEndSumme - NettoPreisEndSumme`.
- `CreditNote` bzw. Gutschriften (`GS`/`G...`) werden negativ gerechnet.
- Waehrung ist fachlich aktuell EUR.
- `CustomerNumber = RechnungsAdressenID`; Kundenname und Kundenland sind im aktuellen CSV-Paar nicht enthalten.
- `Material = ArtikelNummer`. Das ist eine lokale Alphaplan-Artikelnummer, nicht automatisch eine TR-AG-/SAP-`MATNR`.

Offen:

- Finance/Munir muss bestaetigen, welche Kundenlaender und Filter fuer den offiziellen DE-Istwert gelten.
- Falls die Spartenanalyse fuer DE hohe Werte bei `Nicht im TR-AG-Stamm` zeigt, muss die Alphaplan-Artikelnummernlogik gegen TR-AG-/SAP-Materialnummern fachlich geklaert werden.

## Praktische Bedienreihenfolge

1. Neue Datei oder Delta-Datei im richtigen Ordner bereitstellen.
2. In `Manuelle Importe` Pfad pruefen bzw. Standort aktiv lassen.
3. Standortexport fuer das betroffene Land ausfuehren.
4. Falls Audit-CSV fuer Finance/Revision gebraucht wird, im Exportordner `Sales_ProcessedMergeInput_<TSC>_<Datum>.csv` pruefen.
5. Falls die zentrale Auswertung aus CSV erfolgen soll, in `Einstellungen > Export Einstellungen` den Schalter `Zentrale Auswertung aus Audit-CSV` setzen.
6. Danach `Zentrale Datei neu erzeugen` starten.
7. Im zentralen Excel `Finance Summary` und `Finance Details` pruefen.

## Merksatz

Manual-Importe ersetzen pro Standort den aktuellen Stand in `CentralSalesRecords`. Delta-Dateien muessen daher beim Import zusammen mit der passenden Basisdatei gelesen werden.

Aktueller Stand:

- UK: Basis plus Delta-Dateien.
- Spanien: Basis plus `Spain_Sales_range_*.csv`, wenn ein Ordner hinterlegt ist.
- Deutschland: Alphaplan Vollbestand plus `delta`-Unterordner, dedupliziert nach Alphaplan-Zeilen-ID.
- Audit-CSV ist ein zusaetzliches verarbeitetes Prueffile; es ersetzt nicht die originalen Standortdateien.
