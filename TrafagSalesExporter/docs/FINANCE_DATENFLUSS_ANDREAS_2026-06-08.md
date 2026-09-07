# Finance Datenfluss fuer Andreas

Stand: 2026-06-12

Zweck: Diese Notiz beschreibt den tatsaechlichen technischen Datenfluss im Finance Cockpit: wo Daten geholt werden, wann Felder veraendert werden, wann Wechselkurse wirken, wie die zentrale Excel entsteht und welche Quelle die Sparteninformationen liefert.

Fokus nur Wechselkurs/Kursanwendung: `docs/FINANCE_KURS_WORKFLOW_2026-06-09.md`.
Aktuelle Finance-Schulung: `docs/FINANCE_SCHULUNG_FINANZ_2026-06-11.md`.
Prozessgrafiken:

- `docs/FINANCE_PROZESS_EXPORT_DASHBOARD_2026-06-11.svg`
- `docs/FINANCE_AUDIT_CSV_QUELLE_2026-06-11.svg`

![Finance Prozessfluss](FINANCE_PROZESS_EXPORT_DASHBOARD_2026-06-11.svg)

![Audit-CSV als zentrale Auswertungsquelle](FINANCE_AUDIT_CSV_QUELLE_2026-06-11.svg)

## Kurzfazit

- Finance Summary, Management Analyse und Spartenanalyse lesen nicht aus dem SharePoint-Excel. Sie lesen entweder aus `CentralSalesRecords` oder, wenn aktiviert, aus den neuesten verarbeiteten Audit-CSV je Standort.
- Fuer Finance/Revision gibt es einen Audit-CSV-Modus. Standortexporte koennen nach Mapping und Transformation je Standort eine CSV schreiben; per Setting koennen zentrale Excel, Finance Summary, Soll/Ist und Management-Analyse aus diesen CSV statt aus `CentralSalesRecords` lesen.
- Die Audit-CSV heisst `Sales_ProcessedMergeInput_<TSC>_<yyyy-MM-dd>.csv`. Der Name markiert bewusst, dass es das verarbeitete Merge-Eingangsfile ist, nicht die originale Standortdatei.
- Das SharePoint-Excel `Sales_All_*.xlsx` ist ein Export-/Ablageergebnis, nicht die Live-Quelle der Cockpit-Anzeige.
- Jeder Standortexport ersetzt in `CentralSalesRecords` nur die Daten dieses Standorts.
- Die zentrale Excel wird danach aus der aktuell gewaehlten zentralen Auswertungsquelle erzeugt: Standard `CentralSalesRecords`, optional Audit-CSV.
- Wechselkurse veraendern den Standortexport und `CentralSalesRecords` normalerweise nicht. Sie wirken in Analyse-/Anzeige-Sichten, wenn eine Zielwaehrung wie CHF/EUR/USD ausgewaehlt ist, oder in explizit konfigurierten Transformationen.
- Sparteninformationen kommen fuehrend aus SAP/TR-AG `ProductDivisionRefSet`. Aktuell werden sie beim ZSCHWEIZ-/CH-/AT-Export direkt mitgeladen. Andere Laender werden in der Analyse ueber ihre Materialnummer gegen diese TR-AG-Referenz gematcht.

## Technischer Ablauf Standortexport

Ausloeser:

- Export Dashboard: einzelner Standort oder `Alle exportieren`.
- Timer, falls aktiv.
- Intern: `ExportOrchestrationService` ruft `SiteExportService.ExportAsync(...)`.

Ablauf pro Standort:

1. Standort und Quellsystem werden geladen.
2. Adapter wird nach `ConnectionKind` gewaehlt:
   - `HanaDataSourceAdapter` fuer BI1/HANA.
   - `SapGatewayDataSourceAdapter` fuer SAP OData.
   - `ManualExcelDataSourceAdapter` fuer Excel/CSV/SharePoint-Dateien.
3. Rohdaten werden als `SalesRecord`-Liste aufgebaut.
4. Aktive `FieldTransformationRules` fuer das Quellsystem werden angewendet.
5. Falls `Audit-CSV je Standort schreiben` aktiv ist, wird eine verarbeitete CSV geschrieben:
   - Datei: `Sales_ProcessedMergeInput_<TSC>_<Datum>.csv`.
   - Ordner: gleicher lokaler Ordner wie die Standort-Excel.
   - Inhalt: Daten nach Mapping und Transformation.
6. Eine lokale Standort-Excel `Sales_<TSC>_<Datum>.xlsx` wird erzeugt.
7. `CentralSalesRecords` wird fuer diesen Standort ersetzt:
   - alte Saetze mit `SiteId = Standort` loeschen.
   - neue Saetze einfuegen.
8. Falls SharePoint komplett konfiguriert ist, werden Standort-Excel und Audit-CSV in den Landesordner hochgeladen.

Wichtig: Die Reihenfolge ist zuerst Daten holen, dann Transformationen, dann Audit-CSV, dann lokale Excel, dann zentrale Tabelle, dann SharePoint-Upload. Der SharePoint-Upload entscheidet nicht, was in der Cockpit-Anzeige erscheint.

## Zentrale Auswertungsquelle

Der Schalter liegt in der App unter:

```text
Einstellungen > Export Einstellungen > Audit-CSV / nachvollziehbarer Datenfluss
```

| Schalter | Wirkung |
| --- | --- |
| `Audit-CSV je Standort schreiben` | Standortexport schreibt `Sales_ProcessedMergeInput_*.csv` nach Mapping und Transformation. |
| `Zentrale Auswertung aus Audit-CSV` | Finance Summary, Management Analyse, Soll/Ist und zentrale Excel lesen die neuesten Audit-CSV je TSC statt `CentralSalesRecords`. |
| `Lokaler Standardpfad Standort-Dateien` | Ordner fuer Standort-Excel und Audit-CSV. |

Wenn `Zentrale Auswertung aus Audit-CSV` aktiv ist, sucht die App im Standort-Exportordner je TSC die neueste passende CSV. Wenn keine `Sales_ProcessedMergeInput_*.csv` vorhanden ist, ist der Audit-Modus nicht auswertbar.

## Datenquellen pro Quelltyp

### HANA / SAP B1

Betroffen:

| Land | TSC | Schema | Quelle |
| --- | --- | --- | --- |
| Frankreich | `TRFR` | `fr01_p` / lokal teils `FR01_p` | BI1/HANA |
| Italien | `TRIT` | `it01_p` | BI1/HANA |
| USA | `TRUS` | `us01_p` | BI1/HANA |
| Indien | `TRIN` | `TRAFAG_LIVE` | HANA/Sage-Quelle |

Standard-B1-Abfrage:

- Rechnungen aus `OINV` + `INV1`.
- Gutschriften aus `ORIN` + `RIN1`.
- Stornierte Belege werden ausgeschlossen: `CANCELED = 'N'`.
- Datumsfilter auf `DocDate >= ExportSettings.DateFilter`.

Wichtige Feldbelegung:

| Zielfeld | HANA/B1-Feld |
| --- | --- |
| `PostingDate` | `OINV.DocDate` / `ORIN.DocDate` |
| `InvoiceDate` | `OINV.TaxDate` / `ORIN.TaxDate` |
| `Material` | `INV1.ItemCode` / `RIN1.ItemCode` |
| `Name` | `INV1.Dscription` / `RIN1.Dscription` |
| `ProductGroup` | `OITB.ItmsGrpNam` |
| `Quantity` | Invoice positiv, Credit Note negativ |
| `SalesPriceValue` | `LineTotal`, bei Credit Note negativ |
| `SalesCurrency` | `OADM.MainCurncy` |
| `DocumentCurrency` | `DocCur` |
| `DocumentTotalForeignCurrency` | `DocTotalFC`, bei Credit Note negativ |
| `DocumentTotalLocalCurrency` | `DocTotal`, bei Credit Note negativ |
| `VatSumForeignCurrency` | `VatSumFC`, bei Credit Note negativ |
| `VatSumLocalCurrency` | `VatSum`, bei Credit Note negativ |
| `DocumentRate` | `DocRate` |
| `CompanyCurrency` | `OADM.MainCurncy` |
| `DocumentType` | `INV` oder `CRN` |

Italien hat zusaetzlich einen HANA-Filter:

- `AcctCode LIKE '47005%'`
- `AcctCode NOT LIKE '4700504%'`
- bestimmte `CardCode`-Ausschluesse.

Diese HANA-Filter greifen bereits beim Datenholen, also vor Transformation, Excel und `CentralSalesRecords`.

### SAP OData / ZSCHWEIZ

Betroffen:

| Land | TSC | Quelle |
| --- | --- | --- |
| CH / AT | `ZSCHWEIZ` | SAP OData `ZPOWERBI_EINKAUF_SRV` |

Warnstand 2026-06-15: Diese aktuell konfigurierte alte Service-URL liefert fuer `ProductDivisionRefSet` auf `travp762` 42'501 Zeilen, aber alle mit `Wwpsp=UNASS`, `IsAssigned=false`, `0008=0`. Fuer den neuen vollstaendigen Referenzservice muss die Dashboard-Konfiguration auf die korrekte neue SAP-Service-URL zeigen. Der Import bricht bei einem solchen komplett unzugeordneten Referenzlauf ab, damit bestehende Dashboard-Daten nicht ueberschrieben werden.

Aktive OData-Quellen:

| Alias | EntitySet | Zweck |
| --- | --- | --- |
| `Z` | `FinanzdataSchweizOeSet` | Verkaufs-/Finance-Zeilen CH/AT |
| `P` | `ProductDivisionRefSet` | zentrale TR-AG-Spartenreferenz |

Inaktive Reservequelle im Seed:

| Alias | EntitySet | Status |
| --- | --- | --- |
| `M` | `ProductDivisionMapSet` | inaktiv; alter PAPH1-Fallback bleibt nur als Rueckfallkonfiguration erhalten |

Aktiver Join:

```text
Z.Matnr = P.Matnr
```

Beim OData-Import-Join wird `Matnr` auf beiden Seiten gleich normalisiert: trimmen, Grossschreibung, Whitespace entfernen, fuehrende Nullen entfernen. Dadurch matcht SAP `000000000000000006` gegen Umsatzmaterial `6`.

Der alte Join `Z.Prodh = M.Paph1` ist inaktiv.

Wichtige Feldbelegung:

| Zielfeld | Quelle |
| --- | --- |
| `Tsc` | `Z.Tsc` |
| `Land` | `Z.Land1` |
| `InvoiceNumber` | `Z.Vbeln` |
| `PositionOnInvoice` | `Z.Posnr` |
| `PostingDate` | `Z.Fkdat` |
| `InvoiceDate` | `Z.Fkdat` |
| `Material` | `Z.Matnr` |
| `Name` | `Z.Arktx` |
| `ProductGroup` | `Z.Prodh` |
| `SalesPriceValue` | `Z.NetwrHc` |
| `SalesCurrency` | `Z.Hwaer` |
| `DocumentCurrency` | `Z.Waerk` |
| `DocumentTotalForeignCurrency` | `Z.NetwrDc` |
| `DocumentTotalLocalCurrency` | `Z.NetwrHc` |
| `VatSumForeignCurrency` | `0` |
| `VatSumLocalCurrency` | `0` |
| `DocumentRate` | `Z.Kurrf` |
| `CompanyCurrency` | `Z.Hwaer` |
| `DocumentType` | `Z.Fkart` |

Spartenfelder aus `P = ProductDivisionRefSet`:

| Zielfeld | Quelle |
| --- | --- |
| `ProductHierarchyCode` | `P.Paph1` |
| `ProductHierarchyText` | `P.Paph1Text` |
| `ProductFamilyCode` | `P.Wwpfa` |
| `ProductFamilyText` | `P.WwpfaText` |
| `ProductDivisionCode` | `P.Wwpsp` |
| `ProductDivisionText` | `P.WwpspText` |
| `ProductMappingAssigned` | `P.IsAssigned` |

Diese Spartenfelder werden beim ZSCHWEIZ-Export direkt in `CentralSalesRecords` gespeichert.

### Manuelle Excel/CSV / SharePoint-Dateien

Betroffen:

| Land | Beispiel | Quelle |
| --- | --- | --- |
| UK | `TRUK` | SharePoint-Ordner `Import/Finance/UK_B1`, fachlich Sage |
| Spanien | `TRES` / lokal auch `TRSE` in alten Daten | Sage CSV / SharePoint `Import/Finance/Spanien` |
| Deutschland | `TRDE` | Alphaplan CSV-Paar `invoice_headers.csv`/`invoice_lines.csv`, Full + `delta` |

Ablauf:

- Datei wird lokal, per UNC oder aus SharePoint geladen.
- Bei SharePoint-Ordnern wird die passende neueste Datei bzw. bei Spanien alle `Spain_Sales*.csv` gelesen.
- Bei Deutschland/Alphaplan werden `invoice_headers.csv` und `invoice_lines.csv` als Paar gelesen; Vollbestand plus `delta` werden dedupliziert zusammengefuehrt.
- Spalten werden ueber manuelle Mappings oder Headernamen auf `SalesRecord` gemappt.
- UK nutzt `SageNetSales([Sales Price/Value], [Quantity], [Document Type], ...)`.
- Spanien-Deltas werden nach `SourceLineId`, sonst `TSC + InvoiceNumber + PositionOnInvoice + Material`, dedupliziert.
- Alphaplan-Deltas werden primaer nach `SourceLineId = Alphaplan:<BelegePositionenID>` dedupliziert; `ArtikelNummer` bleibt lokale Alphaplan-Artikelnummer und ist nicht automatisch TR-AG-/SAP-`MATNR`.

Manuelle Dateien koennen Spartenfelder enthalten, wenn sie passende Spalten liefern. Im aktuellen fachlichen Zielbild sind lokale ERP-Sparten aber nicht fuehrend; die Spartenanalyse matched gegen TR-AG/SAP-Referenz.

## Wann werden Felder veraendert?

### Beim Datenholen / Mapping

Die meisten Felder werden beim Import initial gesetzt:

- HANA/B1: SQL-Abfrage erzeugt `SalesRecord` direkt.
- SAP OData: OData-Zeilen werden per Mapping/Join in `SalesRecord` geschrieben.
- Manual Excel/CSV: Excel-/CSV-Spalten werden per Mapping in `SalesRecord` geschrieben.

Beispiele fuer aktive Veraenderung beim Holen:

- B1-Gutschriften werden bereits negativ geladen:
  - `Quantity * -1`
  - `LineTotal * -1`
  - `DocTotal * -1`
  - `VatSum * -1`
- ZSCHWEIZ setzt `VatSum* = 0`, weil Nettowerte aus `NetwrHc`/`NetwrDc` kommen.
- ZSCHWEIZ haengt Spartenfelder aus `ProductDivisionRefSet` per Join an.
- UK berechnet den Positionswert aus Stueckpreis mal Menge.
- Spanien setzt Credit Notes / REC negativ.

### Nach dem Datenholen: Transformationen

Danach laufen aktive `FieldTransformationRules`:

- Einfache Feldtransformationen kopieren/normalisieren Werte von `SourceField` nach `TargetField`.
- `FirstNonEmpty` setzt ein Zielfeld aus dem ersten nicht-leeren Kandidatenfeld.
- `ConvertCurrency` kann einen Betrag ueber die Kurstabelle in eine Zielwaehrung umrechnen und optional ein Ziel-Waehrungsfeld setzen.

Wichtig: Transformationen laufen vor lokaler Standort-Excel und vor `CentralSalesRecords`. Falls eine aktive Transformation ein Feld veraendert, wird der veraenderte Wert in Excel und Datenbank gespeichert.

### Beim Schreiben in CentralSalesRecords

Es wird fachlich nichts neu berechnet. Die App speichert die nach Import und Transformation vorliegenden `SalesRecord`-Werte.

Pro Standort:

```text
DELETE CentralSalesRecords WHERE SiteId = <Standort>
INSERT neue Records fuer diesen Standort
```

## Wann wirkt der Wechselkurs?

Es gibt drei getrennte Faelle.

Detail nur zum Kursfluss vom Land bis zur zentralen Dashboard-Analyse: `docs/FINANCE_KURS_WORKFLOW_2026-06-09.md`.

### 1. Standard-Finance-Soll/Ist und Finance Summary

Kein allgemeiner Wechselkurs wird angewendet.

- Fuehrend ist die Hauswaehrung / lokale Finance-Waehrung.
- `Finance Summary` nutzt `SalesPriceValue` nach Finance-Regeln.
- Anzeige bleibt nach vorhandener Waehrung gruppiert.
- Wenn mehrere Waehrungen im Scope sind, zeigt die UI `Mixed`.

Das gilt fuer:

- Management Analyse > Finance Summary.
- Management Analyse > Spartenanalyse.
- Zentrale Excel-Blatt `Finance Summary`.
- Zentrale Excel-Blatt `Finance Details`.

### 2. Management Analyse / Rohdaten-Diagnose mit Zielwaehrung

Wenn in Analyse-Sichten eine Zielwaehrung wie `CHF`, `EUR` oder `USD` ausgewaehlt wird, rechnet die UI-Anzeige zur Laufzeit um.

Quelle:

- `CurrencyExchangeRates`
- gewaehltes `ExchangeRateDateField` aus Settings:
  - `PostingDate`
  - `InvoiceDate`
  - `ExtractionDate`

Die Umrechnung passiert beim Anzeigen/Aggregieren in `ManagementCockpitService`, nicht beim Standortexport und nicht beim Erzeugen der zentralen Excel.

Wenn kein Kurs gefunden wird:

- Anzeige-Wert wird fuer diese Zeile `0` in der Zielwaehrung.
- `MissingExchangeRate` wird gezaehlt und in Diagnosehinweisen angezeigt.

### 3. Explizite Transformation `ConvertCurrency`

Falls eine aktive `FieldTransformationRule` vom Typ `ConvertCurrency` gepflegt ist, passiert die Umrechnung beim Standortexport vor `CentralSalesRecords`.

Dann wird das konfigurierte Zielfeld dauerhaft mit dem umgerechneten Betrag beschrieben.

Standardlogik fuer Datum bei `ConvertCurrency`:

- konfiguriertes `dateField`, falls angegeben.
- sonst `InvoiceDate`.
- sonst `OrderDate`.
- sonst `ExtractionDate`.

## Finance-Regeln und Finance-Wert

Die Finance-Sicht wird nicht durch Excel-Formeln bestimmt, sondern durch `FinanceRuleEngine`.

Zeitliche Abgrenzung:

```text
FinanceDate = PostingDate
sonst InvoiceDate
sonst ExtractionDate
```

Ausnahme:

- DE kann per Finance-Regel auf Jahr 2025 gezwungen werden; der neue Alphaplan-Import liest trotzdem Full + `delta` und filtert danach ueber die Export-/Finance-Datumslogik.

Include/Exclude:

- Regeln koennen Zeilen ausschliessen.
- Regeln koennen Werte negativ setzen.
- Regeln koennen IT-Duplikate mit leerem Supplier Country deduplizieren.

Aktuelle Default-/Fachregeln:

- DE:
  - Alphaplan Full + `delta`.
  - Positionswert aus `NettoPreisGesamt`.
  - `CreditNote`/GS/Gutschriften negativ.
  - `ArtikelNummer` ist lokale Materialnummer; Spartenmatch gegen TR-AG muss fachlich plausibilisiert werden.
- IT:
  - `CustomerName` enthaelt `Trafag Italia` ausschliessen.
  - IT-Zeilen mit leerem Supplier Country deduplizieren.

Der Finance-Wert ist:

```text
Net Sales Actual = SalesPriceValue nach FinanceRuleEngine
```

## Zentrale Excel

Ausloeser:

- Export Dashboard > `Zentrale Datei neu erzeugen`.
- Nach `Alle exportieren` automatisch am Ende.

Ablauf:

1. `ConsolidatedExportService` liest alle Saetze aus der zentralen Auswertungsquelle:
   - Standard: `CentralSalesRecords`.
   - Audit-Modus: neueste `Sales_ProcessedMergeInput_*.csv` je TSC.
2. `ExcelExportService.CreateConsolidatedExcelFile(...)` erzeugt `Sales_All_<Datum>.xlsx`.
3. Die Datei wird lokal geschrieben.
4. Falls SharePoint konfiguriert ist, wird sie hochgeladen.

Ziel lokal:

- `ExportSettings.LocalConsolidatedExportFolder`, falls gesetzt.
- sonst `ExportSettings.LocalSiteExportFolder`, falls gesetzt.
- sonst `<App-Verzeichnis>/output`.

Ziel SharePoint:

- Wenn `CentralExportFolder` gesetzt ist: direkt dorthin.
- Sonst: `ExportFolder/Alle`.

Die zentrale Excel enthaelt:

- Blatt `Finance Summary`.
- Blatt `Finance Details`.
- Blatt `Sales`.
- optional Hilfeblatt.

Wichtig:

- `Finance Summary` im Excel wird beim Schreiben aus den Records berechnet.
- Es liest nicht aus einem vorherigen SharePoint-Excel.
- Je nach Setting sind diese Records entweder DB-Eintraege oder die neuesten verarbeiteten Audit-CSV.
- Wechselkurs-Zielwaehrung aus der UI wird dabei nicht angewendet.

## Finance Summary und Spartenanalyse in der App

Die App-Anzeigen lesen direkt aus der zentralen Auswertungsquelle:

```text
Standard: CentralSalesRecords
Audit-Modus: neueste Sales_ProcessedMergeInput_*.csv je TSC
```

Nicht aus:

```text
SharePoint Sales_All_*.xlsx
```

Das bedeutet:

- Lokal zeigt die App lokale DB-Daten oder lokale Audit-CSV, je nach Setting.
- Publizierter Server zeigt Server-DB-Daten oder Server-Audit-CSV, je nach Setting.
- Wenn lokale und Server-Auswertungsquelle gleich sind, sehen beide gleich aus.
- Ein SharePoint-Upload veraendert die App-Anzeige nicht.

## Spartenanalyse: genaue Logik

### Quelle der Spartenreferenz

Fuehrend ist `ProductDivisionRefSet` aus SAP/TR-AG.

Technisch landet diese Referenz aktuell ueber den ZSCHWEIZ-Export in `CentralSalesRecords`, weil ZSCHWEIZ die Quelle `P = ProductDivisionRefSet` per `Z.Matnr = P.Matnr` joint.

Die Analyse baut daraus eine Referenz:

```text
Materialnummer -> PAPH1 / Produktfamilie / Produktsparte / IsAssigned
```

Materialnummern werden normalisiert:

- Trim.
- Grossschreibung.
- Leerzeichen entfernen.
- fuehrende Nullen entfernen.

### Status in der Spartenanalyse

| Status | Bedeutung |
| --- | --- |
| `Zugeordnet` | Material in TR-AG-Referenz gefunden und `ProductMappingAssigned` ist wahr, Produktsparte ist nicht leer und nicht `UNASS`. |
| `Übrige` | Material in TR-AG-Referenz gefunden und `ProductDivisionCode = 0008`. Das ist eine gueltige Sammel-Sparte, kein Fehler. |
| `Nicht zugeordnet` | Material in TR-AG-Referenz gefunden, aber Spartenwert leer/`UNASS`/nicht assigned. |
| `Nicht im TR-AG-Stamm` | Materialnummer aus lokalem Finance-Umsatz hat keinen Treffer in der TR-AG-Referenz. |
| `Material fehlt` | Finance-Zeile hat keine Materialnummer. |

Bei `Übrige` ist `ProductFamilyCode`/`Wwpfa` leer erlaubt; `ProductFamilyText`, `ProductDivisionText` und `ProductDivisionCode=0008` tragen die gueltige Sammelkategorie.

### Warum aktuell viele `Nicht im Stamm` entstehen koennen

Die aktuellen Daten zeigen Produktfelder nur bei CH/AT direkt gefuellt. Andere Laender werden ueber Materialnummern gegen die TR-AG-Referenz gematcht.

Wenn lokale Artikelnummern nicht identisch mit TR-AG-Materialnummern sind, entstehen `Nicht im TR-AG-Stamm`.

Typische Ursachen:

- lokale Materialnummern statt TR-AG-MATNR.
- Sage-/B1-Artikelnummern, die nicht im TR-AG-Stamm existieren.
- Indien-Artikel wie `DM000010`, `DM000001`, `PT000003`, `IC15415`, die lokal hohe Werte tragen, aber keinen Treffer in der TR-AG-Referenz haben.
- fehlende oder unvollstaendige `ProductDivisionRefSet`-Fuellung.
- Materialnummernformat trotz Normalisierung nicht kompatibel.

## Datenflussdiagramm Textform

```text
Standort Export starten
  |
  +-- HANA/B1: OINV/INV1 + ORIN/RIN1 lesen
  |       -> Gutschriften bereits negativ
  |       -> IT-HANA-Filter bereits in SQL
  |
  +-- SAP OData ZSCHWEIZ:
  |       -> FinanzdataSchweizOeSet lesen
  |       -> ProductDivisionRefSet lesen
  |       -> Join Z.Matnr = P.Matnr
  |       -> Spartenfelder in SalesRecord
  |
  +-- Manual Excel/CSV/SharePoint:
          -> Datei(en) laden
          -> Mapping/SageNetSales/Spain-Dedupe

SalesRecord-Liste
  |
  +-- FieldTransformationRules anwenden
  |       -> optional Feldkopien, FirstNonEmpty, ConvertCurrency
  |
  +-- optional Audit-CSV Sales_ProcessedMergeInput_<TSC>_<Datum>.csv schreiben
  |       -> verarbeitete Daten fuer Finance/Revision
  |
  +-- Standort-Excel Sales_<TSC>_<Datum>.xlsx lokal schreiben
  |
  +-- CentralSalesRecords fuer SiteId ersetzen
  |
  +-- Standort-Excel und Audit-CSV optional nach SharePoint hochladen

Finance Summary / Spartenanalyse
  |
  +-- liest zentrale Auswertungsquelle
  |       -> Standard: CentralSalesRecords
  |       -> optional: neueste Sales_ProcessedMergeInput_*.csv je TSC
  +-- FinanceRuleEngine rechnet Include/Exclude/Net Sales Actual
  +-- Spartenanalyse matched lokale Materialien gegen TR-AG-Referenz aus den Records

Zentrale Excel
  |
  +-- liest zentrale Auswertungsquelle
  +-- erzeugt Sales_All_<Datum>.xlsx lokal
  +-- erzeugt Finance Summary / Finance Details im Excel
  +-- laedt Datei optional nach SharePoint
```

## Wichtige Klarstellungen fuer Finance

1. SharePoint ist Ablage und Quelle fuer manuelle Dateien, aber nicht Live-Quelle der Finance Summary.
2. `CentralSalesRecords` ist der operative zentrale Datenbestand der App, solange nicht Audit-CSV als zentrale Auswertungsquelle aktiv ist.
3. Sparten kommen fachlich aus TR-AG/SAP `ProductDivisionRefSet`, nicht aus lokalen ERP-Sparten.
4. CH/AT bekommen Spartenfelder direkt beim ZSCHWEIZ-Export.
5. Andere Laender bekommen Sparten in der Analyse nur, wenn ihre Materialnummern zur TR-AG-Referenz matchen.
6. Wechselkurse sind keine stille Vorverarbeitung fuer den Standard-Soll/Ist-Abgleich.
7. `Mixed` bedeutet: mehrere Waehrungen im Filter. Prozentwerte auf `Mixed` sind nur eingeschraenkt interpretierbar; fuer belastbare Spartenanteile nach Wert muss Land oder Waehrung gefiltert werden.
8. Die zentrale Excel wird nach den Standortexporten aus der gewaehlten zentralen Auswertungsquelle erstellt. Sie ist Ergebnis, nicht Eingang.
