# Quellsysteme: SAP B1 ueber HANA und SAP OData

Stand: 2026-08-17. Verdichtet aus `FINANCE_SAP_B1_KONNEKTOREN_ANDREAS_2026-07-01.md`.

Technische Grundlage der Verkaufsstrecke fuer die HANA-Laender. Fuer das Hauptbuch siehe
`docs/FINANCE_JOURNAL.md`, fuer die Manual-Excel-Laender `docs/rag/MANUAL_IMPORT.md`.

## Kurzfassung

- SAP B1 wird technisch ueber den **HANA-Adapter** gelesen, nicht ueber eine separate
  B1-API.
- Die Verbindung wird zentral je Quellsystem gepflegt (`HanaServers`,
  `SourceSystemDefinitions`), der Standort haelt die fachliche Zuordnung (`Sites`).
- Rechnungen kommen aus `OINV`/`INV1`, Gutschriften aus `ORIN`/`RIN1`.
- Stornierte Belege werden ausgeschlossen (`CANCELED = 'N'`).
- Gutschriften werden bereits **beim Import** negativ gedreht.
- Datumsfilter aus `ExportSettings.DateFilter`, wirkt auf `DocDate`.

## Quellen je Land

| Land | TSC | SourceSystem | Schema | Host | Bemerkung |
| --- | --- | --- | --- | --- | --- |
| Frankreich | `TRFR` | `BI1` | `fr01_p` | BI1-HANA | Standard-B1-Logik |
| Italien | `TRIT` | `BI1` | `it01_p` | BI1-HANA | plus harter Konten-/Kundenausschluss |
| USA | `TRUS` | `BI1` | `us01_p` | BI1-HANA | Standard-B1-Logik |
| Indien | `TRIN` | `SAGE` | `TRAFAG_LIVE` | `20.197.20.60:30015` | fachlich B1, historisch als `SAGE` angeschrieben; User-Override `TRAFAGCONTROLS` |
| CH/AT | `ZSCHWEIZ` | `SAP_GATEWAY` | — | `ZPOWERBI_EINKAUF_SRV` | anderer Konnektor, nicht B1 |

`UK_B1` ist **nur ein Ordnername** fuer UK. UK ist fachlich Sage/Manual Excel und laeuft
nicht ueber den B1-HANA-Konnektor.

Aktive OData-Quellen fuer CH/AT:

| Alias | EntitySet | Zweck |
| --- | --- | --- |
| `Z` | `FinanzdataSchweizOeSet` | Verkaufs-/Finance-Zeilen CH/AT |
| `P` | `ProductDivisionRefSet` | fuehrende TR-AG-Spartenreferenz |

## Standard-B1-Abfrage

Wird verwendet, wenn fuer einen HANA-Standort keine eigenen aktiven
`SapSourceDefinitions` plus `SapFieldMappings` gepflegt sind.

Hilfstabellen im Query: `OADM` (Hauswaehrung), `OITM` (Artikelstamm, Lieferant), `OITB`
(Artikelgruppe), `OCRD` (Geschaeftspartner), `CRD1` (Land), `OOND` (Branche), `OSLP`
(Sales Responsible), `ORDR` (Auftragsdatum).

### Feldbelegung

| Zielfeld | B1-Feld / Logik |
| --- | --- |
| `PostingDate` | `DocDate` |
| `InvoiceDate` | `TaxDate` |
| `Material` | `ItemCode` |
| `Name` | `Dscription` |
| `ProductGroup` | `OITB.ItmsGrpNam` |
| `Quantity` | Rechnung positiv, Gutschrift `* -1` |
| `SupplierNumber` | `OITM.CardCode` |
| `SupplierName` | `OCRD.CardName` |
| `SupplierCountry` | `CRD1.Country` |
| `CustomerNumber` / `CustomerName` | `CardCode` / `CardName` |
| `CustomerCountry` | `CRD1.Country` |
| `CustomerIndustry` | `OOND.IndName` |
| `StandardCost` | `INV1.StockPrice` / `RIN1.StockPrice` |
| `StandardCostCurrency` | `OADM.MainCurncy` |
| `PurchaseOrderNumber` | bei `BaseType = 22`: `BaseRef` |
| `SalesPriceValue` | `LineTotal`, Gutschrift negativ |
| `SalesCurrency`, `CompanyCurrency` | `OADM.MainCurncy` |
| `DocumentCurrency` | `DocCur` |
| `DocumentTotalForeignCurrency` / `...LocalCurrency` | `DocTotalFC` / `DocTotal`, Gutschrift negativ |
| `VatSumForeignCurrency` / `...LocalCurrency` | `VatSumFC` / `VatSum`, Gutschrift negativ |
| `DocumentRate` | `DocRate` |
| `SalesResponsibleEmployee` | `OSLP.SlpName` |
| `OrderDate` | `ORDR.DocDate`, wenn die Position auf einem Auftrag basiert |
| `DocumentType` | `INV` oder `CRN` |

Nachbearbeitung: enthaelt `Material` einen Slash, wird der letzte Teil nach `/` verwendet.

## Italien-Sonderlogik

Nur fuer Schema `it01_p`, in `HanaQueryService.BuildRevenueAccountFilter(...)`:

```sql
AND p."AcctCode" LIKE '47005%'
AND p."AcctCode" NOT LIKE '4700504%'
AND h."CardCode" NOT IN (
  'C_IT01_0022987', 'C_IT01_0306928', 'C_IT01_0306138',
  'C_IT01_0309653', 'C_IT01_0304885', 'C_IT01_0306475'
)
```

- Der Kontenfilter zielt auf `47005 — Ricavi vendite e prestazioni`.
- Der Ausschluss `4700504%` entfernt `autofattura`-Konten.
- Die sechs Kunden sind historisch als Arbeitsfilter dokumentiert.

**Offen:** Diese Logik ist hart codiert und sollte fachlich final bestaetigt oder in eine
pflegbare Finance-Regel ueberfuehrt werden.

## Credentials und Verbindung

```text
Username = Site.UsernameOverride, sonst SourceSystemDefinition.CentralUsername
Password = Site.PasswordOverride, sonst SourceSystemDefinition.CentralPassword
```

HANA-Verbindung: `ServerNode = Host:Port`, dazu optional `DatabaseName`, `encrypt`,
`sslValidateCertificate` und semikolongetrennte `AdditionalParams`.

**Fuer Betrieb und Firewall wichtig:** Der **Webserver** muss die Ziele erreichen, nicht
der Entwickler-PC. Bekannte Ziele:

| Ziel | Adresse |
| --- | --- |
| BI1/HANA intern | `10.194.65.22:30015` |
| India HANA | `20.197.20.60:30015` |
| SAP OData / ZSCHWEIZ | `10.194.64.29:8000` |

## Rolle im Finance-Datenfluss

```text
HANA/B1 lesen
-> SalesRecord-Liste bilden
-> Transformationen anwenden
-> Sales_ProcessedMergeInput_<TSC>_<Datum>.csv schreiben, wenn Audit-CSV aktiv
-> Standort-Excel schreiben
-> CentralSalesRecords fuer Standort ersetzen
-> optional Upload nach SharePoint
```

Produktiver Auswertungsstand: `AuditCsvEnabled = 1`, `UseAuditCsvAsCentralSource = 1`,
`LocalSiteExportFolder` leer. Damit lesen Finance Summary, Management Analyse, Finance
Pruefbuch und Finance Pivot bevorzugt die neuesten `Sales_ProcessedMergeInput_*.csv` je
TSC, sonst die neueste zentrale `Finance_Dashboard_Audit_All_*.csv`.
**`Sales_All_*.xlsx` ist Nachweis und Export, aber nicht die Live-Quelle der Reiter.**

## Codepfade

| Datei | Rolle |
| --- | --- |
| `Services/DataSources/HanaDataSourceAdapter.cs` | waehlt Konfiguration und Credentials, ruft die Abfrage auf |
| `Services/HanaQueryService.cs` | Standard-B1-Query und freie HANA-Quellen |
| `Services/DataSources/DataSourceCredentials.cs` | Credential-Fallback Standort vor Quellsystem |
| `Services/DatabaseSeedService.cs` | Default- und Reparaturwerte |

Konfigurationstabellen: `SourceSystemDefinitions`, `HanaServers`, `Sites`,
`SapSourceDefinitions`, `SapJoinDefinitions`, `SapFieldMappings`.

## Offene Punkte

- Italien-Sonderfilter aus hartem Code in pflegbare Konfiguration ueberfuehren.
- Produktive B1-/HANA-Konfiguration als exportiertes Config-Paket dokumentieren, damit
  Host, Port, Schemas und Credential-Overrides nicht nur aus Seed und Doku
  rekonstruierbar sind.
- Fachlich bestaetigen: `LineTotal` als fuehrende Net-Sales-Basis, `DocDate` als
  Jahresabgrenzung, Gutschriften als negative Positionszeilen.

## Querverweise

- Hauptbuch: `docs/FINANCE_JOURNAL.md`
- Standardkosten aus `INV1.StockPrice`: `docs/FINANCE_STANDARDKOSTEN.md`
- Manual-Excel-Laender DE/UK/ES: `docs/rag/MANUAL_IMPORT.md`
