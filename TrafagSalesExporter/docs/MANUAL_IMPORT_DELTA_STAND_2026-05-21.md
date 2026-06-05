# Manual-Import und Delta-Stand

Stand: 2026-06-05

Diese Datei beschreibt, wie manuelle Excel-/CSV-Importe aktuell behandelt werden und wie neue Eintraege bzw. Delta-Dateien verarbeitet werden.

## Uebersicht

| Land / Standort | Quelle aktuell | Dateityp | Neue Eintraege / Deltas | Wie die App auswaehlt | Was beim Standortexport passiert | Finance-Wert |
| --- | --- | --- | --- | --- | --- | --- |
| UK / England `TRUK` | SharePoint-Ordner `Import/Finance/UK_B1` | Sage Excel `.xlsx` | Delta-faehig | Bei Jahreslauf: Jahresdatei fuer `TRUK` plus spaetere datierte Dateien `ddMMyy_TRUK.xlsx` oder `.csv` | Alle gefundenen Dateien werden gelesen und zusammen in `CentralSalesRecords` fuer `TRUK` ersetzt | `Sales Price/Value * Quantity`, Credit Notes negativ, GBP |
| Spanien `TRSE` / `TRES` | SharePoint-Ordner `Import/Finance/Spanien` / Sage CSV | `.csv` | Delta-faehig mit Basis + `Spain_Sales_range_YYYYMMDD_to_YYYYMMDD.csv` | Wenn Ordner: alle `Spain_Sales*.csv`, Basis zuerst, danach Range-Dateien nach Datum | Alle Dateien werden gelesen, nach `SourceLineId` bzw. Invoice/Position/Material dedupliziert, danach ersetzen die deduplizierten Spanien-Zeilen den bisherigen Spanien-Stand | Sage `SalesPriceValue`/`ImporteNeto`, REC/Abono/Credit negativ, EUR |
| Deutschland `TRDE` | Alphaplan Excel | `.xlsx` | Vollfile/Jahresfile erforderlich, keine Deltas | Pfad/Datei am Standort hinterlegt | Datei wird gelesen, DE-Zeilen ersetzen bisherigen DE-Stand | `NettoPreisGesamtX`, Finance-Regeln: Ausschluesse, GS negativ, 2025-Zwang |
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
- Wenn nur eine einzelne Delta-Datei direkt als Dateipfad hinterlegt wird, kann weiterhin nur dieses Delta gelesen werden. Fuer Delta-Sync muss deshalb der Ordner hinterlegt sein.

Finance-Logik:

- `SalesPriceValue = ImporteNeto`
- `REC`, Abono bzw. Credit-Faelle werden negativ.
- Waehrung ist EUR.

## Deutschland

Deutschland nutzt Alphaplan Excel.

Aktueller Implementierungsstand:

- Standort `TRDE` ist als `MANUAL_EXCEL` vorbereitet.
- Quelle ist ein Jahres-/Vollfile, aktuell fuer 2025.
- Finance-Regeln erzwingen DE fachlich auf 2025.
- Deutschland muss immer den kompletten relevanten Datenstand liefern.
- Delta-Dateien sind fuer Deutschland nicht vorgesehen.
- Beim Standortexport ersetzt die App den bisherigen Deutschland-Stand in `CentralSalesRecords`.
- Wenn versehentlich nur eine Delta-Datei als Pfad hinterlegt wird, wuerde die App technisch nur dieses Delta lesen und damit den bisherigen Deutschland-Stand ersetzen.
- Es gibt aktuell keine explizite Sperre, die eine Deutschland-Delta-Datei erkennt und ablehnt.

Finance-Logik:

- `SalesPriceValue = NettoPreisGesamtX`
- Ausschluesse gemaess Finance-Regeln:
  - `CustomerName = Trafag AG`
  - `CustomerName contains Magnetic Sense`
  - `InvoiceNumber = GS2510095`
- `InvoiceNumber starts with GS` wird negativ gerechnet.

Offen:

- Finance/Munir muss bestaetigen, welche Kundenlaender und Filter fuer den offiziellen DE-Istwert gelten.

## Praktische Bedienreihenfolge

1. Neue Datei oder Delta-Datei im richtigen Ordner bereitstellen.
2. In `Manuelle Importe` Pfad pruefen bzw. Standort aktiv lassen.
3. Standortexport fuer das betroffene Land ausfuehren.
4. Danach `Zentrale Datei neu erzeugen` starten.
5. Im zentralen Excel `Finance Summary` und `Finance Details` pruefen.

## Merksatz

Manual-Importe ersetzen pro Standort den aktuellen Stand in `CentralSalesRecords`. Delta-Dateien muessen daher beim Import zusammen mit der passenden Basisdatei gelesen werden. Das ist aktuell nur fuer UK vorgesehen. Spanien und Deutschland muessen immer Vollfiles liefern.

Wichtig: Fuer Spanien und Deutschland ist das fachlich/prozessual so vorgesehen und durch den Ersetzungsmechanismus praktisch erforderlich. Eine technische Validierung, die Delta-Dateien fuer ES/DE aktiv blockiert, ist aktuell noch nicht eingebaut.
