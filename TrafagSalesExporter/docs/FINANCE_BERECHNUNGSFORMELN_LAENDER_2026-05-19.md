# Finance Berechnungsformeln pro Land

Stand: 2026-06-12

Nachtrag 2026-06-12:

- DE/Alphaplan nutzt jetzt das CSV-Paar `invoice_headers.csv`/`invoice_lines.csv` mit Full + `delta`; aktiver Positionswert ist `NettoPreisGesamt`.
- Alphaplan `ArtikelNummer` bleibt lokale Artikelnummer und ist nicht automatisch TR-AG-/SAP-`MATNR`.

Nachtrag 2026-06-11:

- ES-Referenz 2025 wurde nach Finance-Sitzung auf `3'082'320.18 EUR` korrigiert. Der alte Wert `3'102'333.61 EUR` war ein Referenz-/Excel-Fehler.
- In Management-Analysen ist das Wechselkurs-Anwendungsdatum konfigurierbar: `PostingDate`, `InvoiceDate` oder `ExtractionDate`.
- Sparten-Materialabgleich normalisiert fuehrende Nullen und warnt bei >=90% ungeklaerter Abdeckung.
- Zentrale Finance-Auswertungen koennen optional aus den neuesten `Sales_ProcessedMergeInput_*.csv` je TSC statt aus `CentralSalesRecords` lesen. Die Formel bleibt gleich; nur die Datenquelle wird per Setting umgeschaltet.

Zweck: Dieses Dokument beschreibt die aktuell im Programm verwendeten Formeln fuer den Soll/Ist-Vergleich 2025. Es ist fuer eine zweite KI oder eine fachliche Gegenpruefung geschrieben.

## Gemeinsame Vergleichslogik

Die echte Webseite `/finance-cockpit/vergleich` und das Testprogramm `/finance` verwenden beide `FinanceReconciliationService`.

Quelle fuer den Ist-Wert ist die zentrale Auswertungsquelle:

- Standard: `CentralSalesRecords`.
- Audit-Modus: neueste `Sales_ProcessedMergeInput_*.csv` je TSC.

Die Jahresabgrenzung ist:

```text
Jahr = Year(PostingDate ?? InvoiceDate ?? ExtractionDate)
```

Fuer 2025 werden die Zeilen je Land gruppiert. Die Gruppierung laeuft ueber `ResolveReferenceKey(Land, Tsc)`.

Pro Land berechnet das Programm mehrere Kandidaten:

```text
SalesPriceValue
  = Sum(SalesPriceValue aus zentraler Auswertungsquelle)

DocTotalFC - VatSumFC
  = Sum(DocumentTotalForeignCurrency - VatSumForeignCurrency)
  = pro Beleg dedupliziert

Nettofakturawert Hauswaehrung pro Position
  = Sum(DocumentTotalLocalCurrency - VatSumLocalCurrency)
  = ueber alle Positionen

Nettofakturawert Hauswaehrung pro Beleg dedupliziert
  = Sum(DocumentTotalLocalCurrency - VatSumLocalCurrency)
  = pro Beleg nur einmal

Nettofakturawert Hauswaehrung -> CHF Budget 2025
  = Sum((DocumentTotalLocalCurrency - VatSumLocalCurrency) * Budgetkurs nach CHF)
```

Belegschluessel fuer Deduplizierung:

```text
DocumentEntry > 0:
  Tsc + DocumentType + DocumentEntry
sonst:
  Tsc + DocumentType + InvoiceNumber
```

Auswahl des Haupt-Ist-Werts:

```text
1. Kandidat mit IsPreferred = true
2. sonst Nettofakturawert Hauswaehrung pro Position
3. sonst Nettofakturawert Hauswaehrung pro Beleg dedupliziert
4. sonst SalesPriceValue
```

`IsPreferred` wird so gesetzt:

```text
SalesPriceValue:
  bevorzugt, wenn Belegkopfwerte wiederholt aussehen und SalesPriceValue != 0

Nettofakturawert Hauswaehrung pro Position:
  bevorzugt, wenn Belegkopfwerte nicht wiederholt aussehen

Nettofakturawert Hauswaehrung pro Beleg dedupliziert:
  bevorzugt, wenn Belegkopfwerte wiederholt aussehen und SalesPriceValue == 0
```

Vergleich gegen Soll:

```text
ReferenceValue = FinanceReference.CheckValue ?? FinanceReference.LocalCurrencyValue
Differenz = IstValue - ReferenceValue
Status OK, wenn Abs(Differenz) <= 1
```

Intercompany-Abzug ist aktuell nur Diagnose:

```text
ValueExcludingIntercompany = Value - IntercompanyValue
```

Der IC-Abzug veraendert die Originaldaten und den Haupt-Ist-Wert nicht.

## Quellen und Formeln pro Land

| Land | Key | Quelle | Hauswaehrung | Soll/Referenz 2025 | Aktueller Haupt-Ist-Wert |
|---|---:|---|---|---:|---|
| Schweiz | CH | SAP OData `ZSCHWEIZ`, falls importiert | CHF | leer | kein Sollwert im Seed |
| Oesterreich | AT | SAP OData `ZSCHWEIZ`, falls importiert | EUR | 3'443'863 | gemeinsame Logik |
| Deutschland | DE | nur falls Daten in `CentralSalesRecords` vorhanden | EUR | 3'635'923 | gemeinsame Logik |
| Spanien | ES | Sage SQL CSV / Manual Excel | EUR | 3'082'320.18 | SalesPriceValue aus Sage `ImporteNeto` |
| Frankreich | FR | SAP B1/HANA Schema `fr01_p` | EUR | CheckValue 1'471'218 | SalesPriceValue / B1 Positions-Netto |
| Indien | IN | Sage/HANA `TRAFAG_LIVE` | INR | CheckValue 750'936'591 | Hauswaehrung INR |
| Italien | IT | SAP B1/HANA Schema `it01_p` | EUR | 7'669'840 | B1 Positions-Netto mit provisorischem Filter |
| UK | UK | Manual Excel aus Sage, Ordnername `UK_B1` nur technisch | GBP | 3'538'972 | SageNetSales in GBP |
| USA | US | SAP B1/HANA Schema `us01_p` | USD | CheckValue 3'749'865 | SalesPriceValue / B1 Positions-Netto |

Hinweis: Der Nutzer schrieb frueher teilweise `AZ`; im Programm ist der Referenz-Key fuer Oesterreich `AT`.

## CH und AT

Aktuelle Quelle:

```text
SourceSystem = SAP
TSC = ZSCHWEIZ
EntitySet = FinanzdataSchweizOeSet
```

Aktuelles Mapping:

```text
SalesPriceValue              = Z.NetwrHc
SalesCurrency                = Z.Hwaer
DocumentCurrency             = Z.Waerk
DocumentTotalForeignCurrency = Z.NetwrDc
DocumentTotalLocalCurrency   = Z.NetwrHc
VatSumForeignCurrency        = 0
VatSumLocalCurrency          = 0
PostingDate                  = Z.Fkdat
InvoiceDate                  = Z.Fkdat
DocumentType                 = Z.Fkart
```

Formel:

```text
Ist = Sum(Z.NetwrHc)
```

Da `VatSumLocalCurrency = 0` und `DocumentTotalLocalCurrency = SalesPriceValue = Z.NetwrHc`, fuehren `SalesPriceValue` und `Nettofakturawert Hauswaehrung` auf denselben fachlichen Wert, sofern die SAP-OData-Zeilen positionsbezogen sind.

Offen:

```text
CH hat aktuell keinen FinanceReference-Sollwert.
AT hat Soll 3'443'863 EUR.
Ob FKDAT fuer diese Quelle fachlich Buchungsdatum oder Fakturadatum ist, muss bei Bedarf noch bestaetigt werden.
```

## DE

Aktuelle Quelle:

```text
SourceSystem = MANUAL_EXCEL
Fachlich = Alphaplan CSV-Paar
TSC = TRDE
Land = Deutschland
Aktueller Datei-/Teststand = invoice_headers.csv + invoice_lines.csv, optional delta\invoice_headers.csv + delta\invoice_lines.csv
```

Aktueller Referenzwert:

```text
FinanceReference.DE.LocalCurrencyValue = 3'635'923
Hauswaehrung = EUR
```

Aktuelles Import-Mapping:

```text
SalesPriceValue = invoice_lines.NettoPreisGesamt
DocumentTotal... = invoice_headers.NettoPreisEndSumme
VatSum... = invoice_headers.BruttoPreisEndSumme - NettoPreisEndSumme
InvoiceNumber = Belegnummer
PositionOnInvoice = ZeilenPosition
Material = ArtikelNummer
Name = ArtikelBezeichnung
Quantity = BEAnzahl
CustomerNumber = RechnungsAdressenID
PurchaseOrderNumber = BestellNummer oder IhrAuftrag
SalesCurrency / DocumentCurrency / CompanyCurrency = EUR
PostingDate / InvoiceDate = Datum oder BelegDatum
SourceLineId = Alphaplan:<BelegePositionenID>
DocumentType = Alphaplan Invoice oder Alphaplan CreditNote
```

Technischer Ablauf:

```text
DE ist als manueller CSV-/SharePoint-Standort vorbereitet.
Nach Upload/Pfad setzen und Aktivieren werden Vollbestand und delta-Unterordner gelesen.
Die gelesenen Zeilen werden nach SourceLineId dedupliziert und in CentralSalesRecords gespeichert.
Die zentrale Excel enthaelt danach DE-Zeilen mit Finance | Country Key = DE.
```

Historischer Befund aus `docs/2025_DataExport_DE.xlsx`:

```text
Zeilen: 6'198 Datenzeilen
Summe NettoPreisGesamtX komplett: 4'154'690.05 EUR
Nur Land Kunde = Deutschland: 3'455'276.64 EUR
Deutschland + China: 3'647'592.44 EUR
Sollwert DE: 3'635'923.00 EUR
```

Offen:

- Finance muss bestaetigen, welche Kundenlaender fuer DE zum offiziellen Ist gehoeren.
- Alphaplan `ArtikelNummer` ist eine lokale Artikelnummer und nicht automatisch eine TR-AG-/SAP-`MATNR`; bei schlechter Spartenabdeckung braucht es eine Mapping-/Nummernlogik.
- Manager-Input nennt Warengruppen-Codes und Versandbedingungs-Codes, im Excel sind aktuell primär Bezeichnungen/Texte sichtbar.
- Falls nach Codes gefiltert werden soll, braucht der Export eigene Code-Spalten oder eine eindeutige Mapping-Tabelle Text -> Code.

## ES

Aktuelle Quelle:

```text
SourceSystem = MANUAL_EXCEL / Sage CSV
TSC = TRES
Land = Spanien
```

Exportskripte:

```text
scripts/Export-SageSpainSalesCsv.ps1
SageSpainExportPackage/SageSpainFinalExportPackage/Export-SageSpainSalesCsv.ps1
```

Sage-SQL-Formel im Export:

```text
SalesPriceValue = LineasAlbaranCliente.ImporteNeto

wenn c.TipoNuevaFra = 2
  oder c.SerieFactura = 'REC'
  oder c.StatusAbono <> 0:
    SalesPriceValue = -ABS(LineasAlbaranCliente.ImporteNeto)
sonst:
    SalesPriceValue = LineasAlbaranCliente.ImporteNeto

SalesCurrency = EUR
DocumentCurrency = EUR
CompanyCurrency = EUR
```

Formel im Vergleich:

```text
Ist ES = Sum(SalesPriceValue)
Soll ES = 3'082'320.18 EUR
```

Bekannter Stand:

```text
Ist ca. 3'082'320.18 EUR
Differenz ca. 0.00 EUR
```

Offen:

```text
Die fruehere Abweichung entstand aus einem falschen Soll-/Referenzwert.
Falls Audit gefragt ist, muss die Herkunft des alten Werts 3'102'333.61 EUR nachvollzogen werden.
```

## FR

Aktuelle Quelle:

```text
SourceSystem = BI1
HANA Schema = fr01_p
SAP B1 Tabellen = OINV/INV1 und ORIN/RIN1
```

B1-HANA-Formel im Import:

```text
Invoices:
  SalesPriceValue = INV1.LineTotal
  Quantity        = INV1.Quantity
  PostingDate     = OINV.DocDate
  InvoiceDate     = OINV.TaxDate
  DocumentTotalLC = OINV.DocTotal
  VatSumLC        = OINV.VatSum

Credit Notes:
  SalesPriceValue = RIN1.LineTotal * -1
  Quantity        = RIN1.Quantity * -1
  PostingDate     = ORIN.DocDate
  InvoiceDate     = ORIN.TaxDate
  DocumentTotalLC = ORIN.DocTotal * -1
  VatSumLC        = ORIN.VatSum * -1
```

Kein landesspezifischer Kontenfilter fuer FR.

Formel im Vergleich:

```text
Ist FR = Sum(SalesPriceValue)
Soll FR = CheckValue 1'471'218 EUR
```

Bekannter Stand:

```text
Ist ca. 1'471'218.44 EUR
Differenz ca. +0.44 EUR
```

## IN

Aktuelle Quelle:

```text
SourceSystem = SAGE
HANA Schema = TRAFAG_LIVE
TSC = TRIN
Hauswaehrung = INR
```

Formel im Vergleich:

```text
Ist IN = bevorzugter Kandidat in Hauswaehrung INR
Soll IN = CheckValue 750'936'591 INR
```

Praktischer Effekt:

```text
Die INR-Hauswaehrung ist fuer den Soll/Ist-Vergleich fuehrend.
CHF-Budgetwert ist nur ein Kandidat, nicht der Hauptvergleich.
```

Bekannter Stand:

```text
Ist ca. 750'936'591.38 INR
Differenz ca. +0.38 INR
```

Offen:

```text
Die genaue Sage/HANA-Quelltabellenformel fuer IN ist in dieser Datei nicht separat aufgeschluesselt.
Gegenpruefung sollte in CentralSalesRecords kontrollieren, welcher Kandidat als preferred gewaehlt wird.
```

## IT

Aktuelle Quelle:

```text
SourceSystem = BI1
HANA Schema = it01_p
SAP B1 Tabellen = OINV/INV1 und ORIN/RIN1
```

B1-HANA-Grundformel wie FR:

```text
Invoices:
  SalesPriceValue = INV1.LineTotal
  PostingDate     = OINV.DocDate

Credit Notes:
  SalesPriceValue = RIN1.LineTotal * -1
  PostingDate     = ORIN.DocDate
```

Zusaetzlicher provisorischer Filter in `HanaQueryService.BuildRevenueAccountFilter`:

```sql
AND p."AcctCode" LIKE '47005%'
AND p."AcctCode" NOT LIKE '4700504%'
AND h."CardCode" NOT IN (
  'C_IT01_0022987',
  'C_IT01_0306928',
  'C_IT01_0306138',
  'C_IT01_0309653',
  'C_IT01_0304885',
  'C_IT01_0306475'
)
```

Formel im Vergleich:

```text
Ist IT = Sum(SalesPriceValue) nach obigem B1-Filter und IT-Finance-Abgrenzung
Soll IT = 7'669'840 EUR
```

Zusaetzliche IT-Finance-Abgrenzung, Stand 2026-05-20:

```text
1. CustomerName enthaelt "Trafag Italia" => aus IT-Finance-Ist ausschliessen.
2. IT-Zeilen mit leerem Supplier country => identische Zeile nur einmal zaehlen.
```

Diese Methode ist gemaess Finance-Leiter fachlich korrekt. Die alte Kundenausschluss-Kombination traf 2025 zufaellig naeher, ist aber nicht die zukunftssichere Methode.

Bekannter Stand:

```text
Ist ca. 7'669'641.47 EUR
Differenz ca. -198.53 EUR
```

Wichtiger Pruefhinweis:

```text
Dieser IT-Filter ist noch hart in der HANA-Abfrage codiert.
Er ist ein Arbeitsfilter aus Screenshot/Cache und muss spaeter in eine konfigurierbare Site-/Source-Regel verschoben werden,
wenn Italien die fachliche B1/Rhino-Regel bestaetigt.
```

## UK

Aktuelle Quelle:

```text
SourceSystem = MANUAL_EXCEL
Fachlich = Sage
TSC = TRUK
SharePoint-Ordner = Import/Finance/UK_B1
```

Wichtig:

```text
Der Ordnername UK_B1 ist nur technisch. UK ist fachlich Sage, nicht SAP B1.
```

Aktuelles Manual-Excel-Mapping:

```text
SalesPriceValue = SageNetSales([Sales Price/Value], [Quantity], [Document Type], [DocumentType], [Type])
SalesCurrency   = GBP
DocumentCurrency = GBP
CompanyCurrency = GBP
PostingDate     = invoice date
InvoiceDate     = invoice date
```

`SageNetSales(...)` im Importer:

```text
netLineAmount = amount * quantity

wenn DocumentType CREDIT, CREDIT NOTE, CREDITNOTE, ABONO, GUTSCHRIFT, CRN oder CN enthaelt:
  SalesPriceValue = -ABS(netLineAmount)
sonst:
  SalesPriceValue = netLineAmount
```

Formel im Vergleich:

```text
Ist UK = Sum(SalesPriceValue) in GBP
Soll UK = LocalCurrencyValue 3'538'972 GBP
```

Bekannter Stand:

```text
Ist ca. 3'533'710.09 GBP
Differenz ca. -5'261.91 GBP
```

Offen:

```text
UK-Abweichung ueber Sage-Exportvollstaendigkeit, Discounts, Freight/Charges,
Gutschriften und 2nd-party-Abgrenzung pruefen.
```

## US

Aktuelle Quelle:

```text
SourceSystem = BI1
HANA Schema = us01_p
SAP B1 Tabellen = OINV/INV1 und ORIN/RIN1
Hauswaehrung = USD
```

B1-HANA-Formel wie FR:

```text
Invoices:
  SalesPriceValue = INV1.LineTotal
  PostingDate     = OINV.DocDate

Credit Notes:
  SalesPriceValue = RIN1.LineTotal * -1
  PostingDate     = ORIN.DocDate
```

Formel im Vergleich:

```text
Ist US = Sum(SalesPriceValue) in USD
Soll US = CheckValue 3'749'865 USD
```

Bekannter Stand:

```text
Ist ca. 3'749'865.33 USD
Differenz ca. +0.33 USD
```

## Was nicht verwechselt werden darf

```text
Soll/Ist-Vergleich:
  FinanceReconciliationService, gleiche Logik in Webseite und Testprogramm.

Import/Transformation:
  SiteExportService liest Quelle -> wendet RecordTransformationService.Apply(records, rules) an -> schreibt CentralSalesRecords.

UK/ES Sage-Korrektur:
  in bestehendem Import-/Exportpfad umgesetzt, nicht als separate nachtraegliche Vergleichsklasse.

IT:
  aktuell noch provisorischer Hardcode in HanaQueryService fuer Schema it01_p.
```

## Code-Stellen fuer Gegenpruefung

```text
Services/FinanceReconciliationService.cs
  globale Kandidaten, Auswahl, Soll/Ist-Differenz, Jahrabgrenzung

Services/HanaQueryService.cs
  SAP B1/HANA Importformel fuer FR/IT/US und IT-Sonderfilter

Services/ManualExcelImportService.cs
  SageNetSales(...) fuer UK Manual Excel Import

Services/DatabaseSeedService.cs
  FinanceReference-Sollwerte, UK Mapping, ZSCHWEIZ Mapping, Default-Waehrungen

scripts/Export-SageSpainSalesCsv.ps1
SageSpainExportPackage/SageSpainFinalExportPackage/Export-SageSpainSalesCsv.ps1
  Spanien Sage SQL Export und Gutschrift-Logik
```
