# RAG Manual Import

Stand: 2026-06-05

## Kurzstand

- Manual-Importe ersetzen pro Standort den aktuellen Stand in `CentralSalesRecords`.
- Delta-Dateien muessen zusammen mit der passenden Basisdatei gelesen werden.
- UK liest Jahresdatei plus spaetere Deltas.
- ES/Spanien liest im Ordner alle `Spain_Sales*.csv`, also Basisdatei plus taegliche `Spain_Sales_range_YYYYMMDD_to_YYYYMMDD.csv`.
- Spanien-Deltas werden vor dem Speichern dedupliziert: zuerst `SourceLineId`, sonst Invoice/Position/Material.
- DE muss weiterhin Vollfiles liefern.

## Laender

| Standort | Quelle | Delta | Finance-Wert |
| --- | --- | --- | --- |
| UK / `TRUK` | SharePoint `Import/Finance/UK_B1`, Sage Excel | ja | `[Sales Price/Value] * [Quantity]`, Credit Notes negativ, GBP |
| ES / `TRSE`/`TRES` | Sage CSV `Spain_Sales*.csv` | ja, wenn Ordner mit Basis + Deltas | `SalesPriceValue`/`ImporteNeto`, REC/Credit negativ, EUR |
| DE / `TRDE` | Alphaplan Excel | nein | `NettoPreisGesamtX`, GS negativ, Ausschlussregeln |

## Bedienreihenfolge

1. Datei oder Delta im richtigen Ordner bereitstellen.
2. In `Manuelle Importe` Pfad/Standort pruefen.
3. Standortexport ausfuehren.
4. Zentrale Datei neu erzeugen.
5. `Finance Summary` und `Finance Details` pruefen.

## Spanien Delta-Sync

- SharePoint-Ordner: `Import/Finance/Spanien`.
- Dateimuster:
  - Basis/Vollfile: z. B. `Spain_Sales_2025.csv`.
  - Delta/Range: `Spain_Sales_range_20260528_to_20260603.csv`.
- Die App liest bei Spanien-Ordnern alle `Spain_Sales*.csv`, nicht nur die neueste Datei.
- Reihenfolge: Basisdateien zuerst, danach Range-Dateien nach Datum.
- Deduplizierung:
  - primaer `SourceLineId`.
  - Fallback `TSC + InvoiceNumber + PositionOnInvoice + Material`.
- Danach ersetzt die App den Spanien-Stand in `CentralSalesRecords` mit diesem deduplizierten Gesamtstand.

## Rohquellen Nur Bei Bedarf

- Detailstand: `docs/MANUAL_IMPORT_DELTA_STAND_2026-05-21.md`
- Workflow-Historie: `NEXT_STEPS_2026-04-15.md`
