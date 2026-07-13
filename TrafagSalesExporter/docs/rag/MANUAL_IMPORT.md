# RAG Manual Import

Stand: 2026-07-13

## Kurzstand

- Manual-Importe ersetzen pro Standort den aktuellen Stand in `CentralSalesRecords`.
- Delta-Dateien muessen zusammen mit der passenden Basisdatei gelesen werden.
- UK liest Jahresdatei plus spaetere Deltas.
- BUGFIX 2026-07-13 (UK-Selbstfuetterung): Der Standortexport laedt eigene Ausgaben (`Sales_ProcessedMergeInput_<TSC>_*.csv` und `Sales_<TSC>_<yyyy-MM-dd>.xlsx`) in denselben SharePoint-Landesordner hoch, aus dem der Manual-Import liest. Seit ca. 30.06. (Audit-CSV produktiv) waehlte der UK-Import dadurch taeglich seine EIGENE Audit-CSV vom Vortag als "neueste TRUK-Datei" und ersetzte den UK-Bestand mit deren 2 Zeilen (Beweis: AppEventLog `Neueste SharePoint-Datei ausgewaehlt | UK_B1/Sales_ProcessedMergeInput_TRUK_*.csv`). Fix: `SharePointUploadService.IsOwnExportOutputFile` schliesst eigene Ausgaben aus der Kandidatenauswahl aus (SharePoint- und Lokalordner-Pfad).
- NEU 2026-07-13 (UK Basis+Delta im Tageslauf): Ohne explizites Importjahr las der Ordner-Import bisher NUR die neueste Datei — beim taeglichen Timer-Export wurde der UK-Bestand also durch das juengste Delta (`ddMMyy_TRUK.xlsx`, oft nur wenige Zeilen) ersetzt. Jetzt gilt auch ohne Jahresangabe das Basis+Delta-Modell: neueste Jahres-/Basisdatei plus alle neueren datierten Deltas werden zusammen gelesen und generisch dedupliziert (`SourceLineId`, sonst Invoice/Position/Material; spaetere Datei gewinnt). Gibt es keine Basisdatei, werden alle datierten Deltas gemeinsam gelesen.
- ES/Spanien liest im Ordner alle `Spain_Sales*.csv`, also Basisdatei plus taegliche `Spain_Sales_range_YYYYMMDD_to_YYYYMMDD.csv`.
- Spanien-Deltas werden vor dem Speichern dedupliziert: zuerst `SourceLineId`, sonst Invoice/Position/Material.
- DE/Alphaplan liest `invoice_headers.csv` + `invoice_lines.csv`; Vollbestand im Ordner plus 7-Tage-Delta im Unterordner `delta` werden zusammen gelesen. Seit 2026-07-03 werden zusaetzlich `Alphaplan*.zip` im SharePoint-Ordner automatisch entpackt und wie CSV-Paare ausgewertet.
- DE-Dedupe: primaer `BelegePositionenID` als `SourceLineId`, Fallback Invoice/Position/Material; Delta gewinnt gegen Vollbestand.
- DE-Material: `ArtikelNummer` bleibt lokale Alphaplan-Artikelnummer und ist nicht automatisch eine TR-AG-/SAP-`MATNR`.
- Wenn Audit-CSV aktiv ist, schreibt der Standortexport nach Mapping/Transformation zusaetzlich `Sales_ProcessedMergeInput_<TSC>_<Datum>.csv` in den Standort-Exportordner.
- Zentrale Auswertungen koennen per Setting aus den neuesten Audit-CSV je TSC statt direkt aus `CentralSalesRecords` lesen.

## Laender

| Standort | Quelle | Delta | Finance-Wert |
| --- | --- | --- | --- |
| UK / `TRUK` | SharePoint `Import/Finance/UK_B1`, Sage Excel | ja | `[Sales Price/Value] * [Quantity]`, Credit Notes negativ, GBP |
| ES / `TRSE`/`TRES` | Sage CSV `Spain_Sales*.csv` | ja, wenn Ordner mit Basis + Deltas | `SalesPriceValue`/`ImporteNeto`, REC/Credit negativ, EUR |
| DE / `TRDE` | Alphaplan CSV-Paar oder `Alphaplan*.zip` mit `invoice_headers.csv` + `invoice_lines.csv` | ja, Full + `delta`-Unterordner/Delta-ZIP | `NettoPreisGesamt`, CreditNote/GS negativ, EUR |

## Bedienreihenfolge

1. Datei oder Delta im richtigen Ordner bereitstellen.
2. In `Manuelle Importe` Pfad/Standort pruefen.
3. Standortexport ausfuehren.
4. Optional Audit-CSV im Standort-Exportordner pruefen.
5. Zentrale Auswertungsquelle bewusst setzen: DB oder Audit-CSV.
6. Zentrale Datei neu erzeugen.
7. `Finance Summary` und `Finance Details` pruefen.

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

## Deutschland Alphaplan Full/Delta

- Erwartetes Paar je Ordner: `invoice_headers.csv` und `invoice_lines.csv`.
- Vollbestand liegt im Standortordner; 7-Tage-Rueckblick liegt im Unterordner `delta`.
- Lokal und in SharePoint werden passende Paare rekursiv gesucht; in SharePoint werden zusaetzlich `Alphaplan*.zip` erkannt, temporaer entpackt und deren CSV-Paare eingelesen.
- Header und Positionen werden ueber `BelegeID` verbunden.
- Dedupe: primaer `SourceLineId = Alphaplan:<BelegePositionenID>`, sonst Invoice/Position/Material; Delta-Zeilen gewinnen.
- `SalesPriceValue = NettoPreisGesamt`; `DocumentTotal... = NettoPreisEndSumme`; `CreditNote`/GS/Gutschriften werden negativ gerechnet.
- `CustomerNumber = RechnungsAdressenID`; Kundenname und Kundenland sind im aktuellen CSV-Paar nicht enthalten.
- `Material = ArtikelNummer`; diese lokale Alphaplan-Nummer ist nicht garantiert identisch mit TR-AG-/SAP-`MATNR`.
- Das alte Alphaplan-Excel-Mapping bleibt technisch vorhanden, ist aber nicht mehr der bevorzugte DE-Pfad. Produktiver DE-Pfad seit 2026-07-03: `Import/Finance/Deutschland/AlphaplanRaw`.

## Rohquellen Nur Bei Bedarf

- Detailstand: `docs/MANUAL_IMPORT_DELTA_STAND_2026-05-21.md`
- Workflow-Historie: `NEXT_STEPS_2026-04-15.md`

