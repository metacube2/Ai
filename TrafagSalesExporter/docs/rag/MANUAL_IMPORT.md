# RAG Manual Import

Stand: 2026-08-17

## Kurzstand

- Manual-Importe ersetzen pro Standort den aktuellen Stand in `CentralSalesRecords`.
- Delta-Dateien muessen zusammen mit der passenden Basisdatei gelesen werden.
- UK liest Jahresdatei plus spaetere Deltas.
- BUGFIX 2026-07-13 (UK-Selbstfuetterung): Der Standortexport laedt eigene Ausgaben (`Sales_ProcessedMergeInput_<TSC>_*.csv` und `Sales_<TSC>_<yyyy-MM-dd>.xlsx`) in denselben SharePoint-Landesordner hoch, aus dem der Manual-Import liest. Seit ca. 30.06. (Audit-CSV produktiv) waehlte der UK-Import dadurch taeglich seine EIGENE Audit-CSV vom Vortag als "neueste TRUK-Datei" und ersetzte den UK-Bestand mit deren 2 Zeilen (Beweis: AppEventLog `Neueste SharePoint-Datei ausgewaehlt | UK_B1/Sales_ProcessedMergeInput_TRUK_*.csv`). Fix: `SharePointUploadService.IsOwnExportOutputFile` schliesst eigene Ausgaben aus der Kandidatenauswahl aus (SharePoint- und Lokalordner-Pfad).
- NEU 2026-07-13 (UK Basis+Delta im Tageslauf): Ohne explizites Importjahr las der Ordner-Import bisher NUR die neueste Datei — beim taeglichen Timer-Export wurde der UK-Bestand also durch das juengste Delta (`ddMMyy_TRUK.xlsx`, oft nur wenige Zeilen) ersetzt. Jetzt gilt auch ohne Jahresangabe das Basis+Delta-Modell: neueste Jahres-/Basisdatei plus alle neueren datierten Deltas werden zusammen gelesen und generisch dedupliziert (`SourceLineId`, sonst Invoice/Position/Material; spaetere Datei gewinnt). Gibt es keine Basisdatei, werden alle datierten Deltas gemeinsam gelesen.
- ES/Spanien liest im Ordner alle `Spain_Sales*.csv`, also Basisdatei plus taegliche `Spain_Sales_range_YYYYMMDD_to_YYYYMMDD.csv`.
- ES BUCHUNGSDATUM FEHLT KOMPLETT (Befund 2026-08-03, Prio von Andreas): `PostingDate` ist auf
  ALLEN 5'504 TRES-Zeilen leer — Spanien ist der einzige Standort ohne Buchungsdatum. Alle Zeilen
  fallen deshalb auf `InvoiceDate` zurueck, 231 davon eine Stufe weiter auf `ExtractionDate`
  (140'598.19 EUR, zaehlen pauschal im Exportjahr). Die aeltere Formulierung „231 Zeilen ohne
  jedes Datum" beschreibt nur diese Teilmenge, nicht das Problem. Ursache ist unsere eigene
  Query (s. Abschnitt „Skripthoheit"). Details, Kandidatenquelle und offene Fachentscheide:
  `docs/FINANCE_ES_BUCHUNGSDATUM_2026-08-03.md`.
- ES BUCHUNGSDATUM, STAND 2026-08-17: das Feld ist im Exportskript EINGEBAUT, aber noch NICHT
  in Spanien gelaufen. Bis der Standortexport dort neu laeuft und die Spalte `PostingDate` beim
  Standort Spanien zugeordnet ist, bleibt der Befund oben unveraendert gueltig. Die Zuordnung
  ist bei Spanien NICHT im Seed verdrahtet, anders als bei UK und DE — sie wird in den
  Einstellungen gepflegt. Der Join-Schluessel ist bis zur Messung eine Annahme.
- Spanien-Deltas werden vor dem Speichern dedupliziert: zuerst `SourceLineId`, sonst Invoice/Position/Material.
- DE/Alphaplan liest `invoice_headers.csv` + `invoice_lines.csv`; Vollbestand im Ordner plus 7-Tage-Delta im Unterordner `delta` werden zusammen gelesen. Seit 2026-07-03 werden zusaetzlich `Alphaplan*.zip` im SharePoint-Ordner automatisch entpackt und wie CSV-Paare ausgewertet.
- DE-Dedupe: primaer `BelegePositionenID` als `SourceLineId`, Fallback Invoice/Position/Material; Delta gewinnt gegen Vollbestand.
- DE-Material: `ArtikelNummer` bleibt lokale Alphaplan-Artikelnummer und ist nicht automatisch eine TR-AG-/SAP-`MATNR`.
- Wenn Audit-CSV aktiv ist, schreibt der Standortexport nach Mapping/Transformation zusaetzlich `Sales_ProcessedMergeInput_<TSC>_<Datum>.csv` in den Standort-Exportordner.
- Zentrale Auswertungen koennen per Setting aus den neuesten Audit-CSV je TSC statt direkt aus `CentralSalesRecords` lesen.

## Skripthoheit: die Export-SQL fuer DE und ES ist UNSERE

Befund 2026-08-03. Wer ein fehlendes Feld bei DE oder ES sieht, muss ZUERST hier schauen,
nicht den Standort anschreiben — sonst geht die Bitte an die falsche Stelle.

| Standort | Skript (in diesem Repo) | liest | Konsequenz |
| --- | --- | --- | --- |
| DE | `AlphaplanExportPackage/scripte/alphaplanExport.ps1` Z. 143-202, identisch in `alphaplandeltaexport.ps1` | nur `dbo.Belege` + `dbo.BelegePositionen` | Supplier, Kundenname/-land, saubere Bezeichnung fehlen, weil die Query sie nicht liest; `RechnungsAdressenID` wird selektiert, aber nie aufgeloest |
| ES | `SageSpainExportPackage/SageSpainFinalExportPackage/Export-SageSpainSalesCsv.ps1` Z. 184-188 und Z. 229-237, identisch in `Run-SpainRangeExportAndUpload-AllInOne.ps1` Z. 233-237 und Z. 278-286, gespiegelt in `scripts/Export-SageSpainSalesCsv.ps1` | `dbo.CabeceraAlbaranCliente` + `dbo.LineasAlbaranCliente`, seit 2026-08-17 zusaetzlich `dbo.FacturasTB` per `OUTER APPLY` | Buchungsdatum ist seit 2026-08-17 als `PostingDate` selektiert, aber noch nicht in Spanien gelaufen; bis dahin bleibt `PostingDate` auf allen TRES-Zeilen leer |

Die Skripte laufen auf den Standortservern (DE `localhost\SQL2012`/`ApDaten`), die Query
darin stammt aber von uns. Zwei Regeln daraus:

- **Bei ES immer BEIDE Stellen aendern** (Voll- und Range-Export), sonst laufen sie auseinander.
- **Keine Tabellen-/Spaltennamen raten.** Fuer `ApDaten` existiert keine Schemaliste, der
  Sage-Auszug `obj/candidate_objects.csv` ist bei 80 Objekten abgeschnitten und enthaelt
  `CabeceraFacturaCliente` nicht. Erst Schema live klaeren, dann Query erweitern.
- Die ES-Erweiterung um `PostingDate` vom 2026-08-17 ist KEINE Ausnahme von dieser Regel: der
  Schluessel steht als offen gekennzeichnete Annahme im Skript, damit er in Spanien gemessen
  werden kann. Erst die Messung dort macht ihn zur Tatsache. Vor allem die Zeilenzahl gegen den
  Vorlauf pruefen — steigt sie, trifft die Zuordnung mehrfach und der Umsatz waere zu hoch.

## Laender

| Standort | Quelle | Delta | Finance-Wert |
| --- | --- | --- | --- |
| UK / `TRUK` | SharePoint `Import/Finance/UK_B1`, Sage Excel | ja | `[Sales Price/Value] * [Quantity]`, Credit Notes negativ, GBP |
| ES / `TRSE`/`TRES` | Sage CSV `Spain_Sales*.csv` | ja, wenn Ordner mit Basis + Deltas | `SalesPriceValue`/`ImporteNeto`, REC/Credit negativ, EUR |
| DE / `TRDE` | Alphaplan CSV-Paar oder `Alphaplan*.zip` mit `invoice_headers.csv` + `invoice_lines.csv` | ja, Full + `delta`-Unterordner/Delta-ZIP | `NettoPreisGesamt`, CreditNote/GS negativ, EUR |

## Zwei Betriebsfallen, die Daten vernichten koennen

**1. UK-Reimport nur OHNE Jahresfilter starten.** Der Ordner `UK_B1` enthaelt Dateien
beider Jahre. `ReplaceForSiteAsync` loescht vorher **alle** Zeilen des Standorts, deshalb
vernichtet ein Import mit Jahresvorgabe das jeweils andere Jahr:

| `PreferredImportYear` | gelesene Dateien |
| --- | --- |
| **keins** (richtig) | `TRUK_2025.xlsx` **und** alle 2026er Dateien |
| 2025 | nur `TRUK_2025.xlsx` — die 2026er Zeilen gehen verloren |
| 2026 | alle 2026er **ohne** `TRUK_2025.xlsx` — 2025 geht verloren |

Kontrolle nach dem Reimport: die UK-Zeilenzahl muss ueber der Summe beider Jahre liegen,
nicht nur bei einem davon.

**2. Legendenzeile im Template.** Die erste Datenzeile in `TRUK_2025.xlsx` ist eine
Beschreibungszeile (`Tsc = "Subsidiary abbreviation / company identifier"`). Sie kam bei
jedem Reimport zurueck, weil der Adapter keinen Filter hatte.
`RemoveTemplateDescriptionRowsAsync` verwirft solche Zeilen jetzt vor der Deduplizierung
und protokolliert „Legendenzeile verworfen". Erkannt wird am TSC-Feld: echte Werte sind
kurze Codes ohne Leerzeichen, die Legende traegt dort einen ganzen Satz.

**Bewusst nicht gegen `site.TSC` verglichen:** Der Spanien-Standort heisst in `Sites`
`TRSE`, liefert in den Daten aber `TRES`. Ein Gleichheitstest wuerde dort saemtliche Zeilen
verwerfen.

## `Standard cost` und `Sales Price/Value` sind STUECKpreise

Die Margenlogik rechnet `Menge x StandardCost`. Waere die Spalte eine Zeilensumme, laege
die Marge um den Faktor Menge daneben. An 1'650 UK-Zeilen gegengeprueft: als Stueckpreis
gelesen sind 1'643 Zeilen plausibel (Margen 12–55 %), als Zeilensumme nur 1'253
(Margen 86–99 %).

`Sales Price/Value` ist ebenfalls ein Stueckpreis; das UK-Mapping fuehrt ihn durch
`=SageNetSales([Sales Price/Value], [Quantity], ...)`, und die Funktion rechnet
`amount * quantity`. Der Zeilenumsatz ist also `Menge x Sales Price/Value`.

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
- URSACHE DER DE-FELDLUECKEN (Befund 2026-08-03): Lieferant, Kundenname/-land und die saubere
  Artikelbezeichnung fehlen, weil **unsere eigene Export-Query** sie nicht liest — nicht weil
  Deutschland sie nicht liefern koennte. `AlphaplanExportPackage/scripte/alphaplanExport.ps1`
  Zeilen 143-202 und `alphaplandeltaexport.ps1` (identische Query) lesen nur `dbo.Belege` und
  `dbo.BelegePositionen`; `RechnungsAdressenID` wird selektiert, aber nie auf einen Namen
  aufgeloest. Blocker ist das fehlende Alphaplan-Schema fuer `ApDaten` (`candidate_objects.csv`
  im Repo-Root ist leer, `obj/candidate_objects.csv` ist Sage Spanien) — deshalb KEINE
  Tabellen-/Spaltennamen raten, sondern Schema-Auszug anfordern. Details und Mailstand:
  `docs/FINANCE_FELDLUECKEN.md` Abschnitt 6.
- Das alte Alphaplan-Excel-Mapping bleibt technisch vorhanden, ist aber nicht mehr der bevorzugte DE-Pfad. Produktiver DE-Pfad seit 2026-07-03: `Import/Finance/Deutschland/AlphaplanRaw`.

## Rohquellen Nur Bei Bedarf

- Detailstand: `docs/MANUAL_IMPORT_DELTA_STAND_2026-05-21.md`

