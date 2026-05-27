# RAG Manual Import

Stand: 2026-05-27

## Kurzstand

- Manual-Importe ersetzen pro Standort den aktuellen Stand in `CentralSalesRecords`.
- Delta-Dateien muessen zusammen mit der passenden Basisdatei gelesen werden.
- Das ist aktuell nur fuer UK vorgesehen.
- ES und DE muessen Vollfiles liefern.

## Laender

| Standort | Quelle | Delta | Finance-Wert |
| --- | --- | --- | --- |
| UK / `TRUK` | SharePoint `Import/Finance/UK_B1`, Sage Excel | ja | `[Sales Price/Value] * [Quantity]`, Credit Notes negativ, GBP |
| ES / `TRSE`/`TRES` | Sage CSV | nein | `ImporteNeto`, REC/Credit negativ, EUR |
| DE / `TRDE` | Alphaplan Excel | nein | `NettoPreisGesamtX`, GS negativ, Ausschlussregeln |

## Bedienreihenfolge

1. Datei oder Delta im richtigen Ordner bereitstellen.
2. In `Manuelle Importe` Pfad/Standort pruefen.
3. Standortexport ausfuehren.
4. Zentrale Datei neu erzeugen.
5. `Finance Summary` und `Finance Details` pruefen.

## Rohquellen Nur Bei Bedarf

- Detailstand: `docs/MANUAL_IMPORT_DELTA_STAND_2026-05-21.md`
- Workflow-Historie: `NEXT_STEPS_2026-04-15.md`

