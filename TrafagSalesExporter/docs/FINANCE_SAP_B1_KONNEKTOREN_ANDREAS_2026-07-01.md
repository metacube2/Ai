# Finance SAP B1 Konnektoren - Abstimmungsnotiz Andreas

Stand: 2026-07-01

Zweck: kurze technische und fachliche Grundlage fuer die Abstimmung mit Andreas/Finance zu den SAP-B1-/HANA-Konnektoren im Finance Dashboard.

## Kurzfazit

- SAP B1 wird in der App technisch ueber den `HANA`-Adapter gelesen, nicht ueber eine separate B1-API.
- Die technische Verbindung wird zentral pro Quellsystem gepflegt: `HanaServers` + `SourceSystemDefinitions`.
- Der Standort speichert die fachliche Zuordnung: Schema, TSC, Land, Quellsystem und optionale Username-/Password-Overrides.
- Fuer die Standard-B1-Laender liest die App Rechnungen aus `OINV`/`INV1` und Gutschriften aus `ORIN`/`RIN1`.
- Stornierte Belege werden ausgeschlossen (`CANCELED = 'N'`).
- Der Datumsfilter kommt aus `ExportSettings.DateFilter` und wirkt auf `DocDate`.
- Gutschriften werden bereits beim Import negativ gedreht.
- Die operative Dashboard-Quelle ist produktiv aktuell Audit-CSV bzw. deren Fallback, nicht das zentrale `Sales_All_*.xlsx`.

## Beteiligte App-Konfiguration

Die B1-Anbindung steht nicht in `appsettings.json`. Dort sind nur globale App-/Security-Themen wie `FinanceCockpitAccess` gepflegt.

Relevante Tabellen/Modelle:

| Bereich | Modell/Tabelle | Bedeutung |
| --- | --- | --- |
| Quellsystem | `SourceSystemDefinitions` | Code, Anzeigename, Anschlussart (`HANA`, `SAP_GATEWAY`, `MANUAL_EXCEL`) |
| HANA-Technik | `HanaServers` | Host, Port, DatabaseName, SSL, Zusatzparameter je Quellsystem |
| Standort | `Sites` | Schema, TSC, Land, SourceSystem, optionale Credentials |
| freie Quellen | `SapSourceDefinitions` | optionale HANA-Tabellen/Views bzw. SAP-EntitySets je Standort |
| Joins | `SapJoinDefinitions` | optionale Join-Definitionen fuer freie Quellen |
| Feldmapping | `SapFieldMappings` | Mapping auf `SalesRecord` |

Wichtige Codepfade:

| Datei | Rolle |
| --- | --- |
| `Services/DataSources/HanaDataSourceAdapter.cs` | waehlt HANA-Konfiguration, Credentials und ruft HANA-Abfrage auf |
| `Services/HanaQueryService.cs` | Standard-B1-Query und freie HANA-Quellen |
| `Services/DataSources/DataSourceCredentials.cs` | Credential-Fallback Standort-Override vor zentralem Quellsystem |
| `Services/DatabaseSeedService.cs` | Default-/Reparaturwerte fuer Quellsysteme, Standorte und HANA-Server |
| `Components/Pages/Standorte.razor` | UI fuer zentrale HANA-Technik und Standortpflege |
| `Components/Pages/Settings.razor` | UI fuer Quellsysteme und Anschlussart |

## Aktuelle B1-/HANA-Quellen fuer Finance

| Land | TSC | SourceSystem | Anschluss | Schema | Host/Route laut Seed/Doku | Bemerkung |
| --- | --- | --- | --- | --- | --- | --- |
| Frankreich | `TRFR` | `BI1` | HANA/B1 | `fr01_p` | zentraler BI1-HANA-Server, Seed `travtrp0:30015`; produktive Firewall-Doku nennt BI1 `10.194.65.22:30015` | Standard-B1-Logik |
| Italien | `TRIT` | `BI1` | HANA/B1 | `it01_p` | zentraler BI1-HANA-Server | Standard-B1-Logik plus harter IT-Konten-/Kundenausschluss im Query |
| USA | `TRUS` | `BI1` | HANA/B1 | `us01_p` | zentraler BI1-HANA-Server | Standard-B1-Logik |
| Indien | `TRIN` | `SAGE` | HANA/Sage-Quelle | `TRAFAG_LIVE` | `20.197.20.60:30015` | fachlich keine klassische BI1-B1-Quelle; produktiv repariert auf SAGE-Route, User-Override `TRAFAGCONTROLS` dokumentiert |

Hinweis: `UK_B1` ist nur ein Ordnername fuer UK. UK ist fachlich Sage/Manual Excel und wird nicht ueber den SAP-B1-HANA-Konnektor gelesen.

## Standard-B1-Abfrage

Die Standard-B1-Strecke wird verwendet, wenn fuer einen HANA-Standort keine eigenen aktiven `SapSourceDefinitions` plus `SapFieldMappings` gepflegt sind.

Gelesene B1-Tabellen:

| Belegtyp | Header | Positionen |
| --- | --- | --- |
| Rechnung | `OINV` | `INV1` |
| Gutschrift | `ORIN` | `RIN1` |

Zusaetzliche Stammdaten-/Hilfstabellen im Query:

| Tabelle | Zweck |
| --- | --- |
| `OADM` | Hauswaehrung / Company Currency |
| `OITM` | Artikelstamm, Lieferant |
| `OITB` | Artikelgruppe |
| `OCRD` | Kunde/Lieferant |
| `CRD1` | Kunden-/Lieferantenland |
| `OOND` | Branche |
| `OSLP` | Sales Responsible |
| `ORDR` | OrderDate bei Positionsbezug auf Auftrag |

Filter:

```text
h."CANCELED" = 'N'
h."DocDate" >= ExportSettings.DateFilter
```

## Zentrale Feldbelegung aus B1

| Zielfeld im Dashboard | B1-Feld / Logik |
| --- | --- |
| `PostingDate` | `OINV.DocDate` / `ORIN.DocDate` |
| `InvoiceDate` | `OINV.TaxDate` / `ORIN.TaxDate` |
| `Material` | `INV1.ItemCode` / `RIN1.ItemCode` |
| `Name` | `INV1.Dscription` / `RIN1.Dscription` |
| `ProductGroup` | `OITB.ItmsGrpNam` |
| `Quantity` | Rechnung positiv, Gutschrift `* -1` |
| `SupplierNumber` | `OITM.CardCode` |
| `SupplierName` | Lieferant aus `OCRD.CardName` |
| `SupplierCountry` | Lieferantenadresse aus `CRD1.Country` |
| `CustomerNumber` | `OINV.CardCode` / `ORIN.CardCode` |
| `CustomerName` | `OINV.CardName` / `ORIN.CardName` |
| `CustomerCountry` | Kundenadresse aus `CRD1.Country` |
| `CustomerIndustry` | `OOND.IndName` |
| `StandardCost` | `INV1.StockPrice` / `RIN1.StockPrice` |
| `StandardCostCurrency` | `OADM.MainCurncy` |
| `PurchaseOrderNumber` | bei `BaseType = 22`: `BaseRef` |
| `SalesPriceValue` | `LineTotal`, bei Gutschrift `LineTotal * -1` |
| `SalesCurrency` | `OADM.MainCurncy` |
| `DocumentCurrency` | `DocCur` |
| `DocumentTotalForeignCurrency` | `DocTotalFC`, bei Gutschrift negativ |
| `DocumentTotalLocalCurrency` | `DocTotal`, bei Gutschrift negativ |
| `VatSumForeignCurrency` | `VatSumFC`, bei Gutschrift negativ |
| `VatSumLocalCurrency` | `VatSum`, bei Gutschrift negativ |
| `DocumentRate` | `DocRate` |
| `CompanyCurrency` | `OADM.MainCurncy` |
| `SalesResponsibleEmployee` | `OSLP.SlpName` |
| `OrderDate` | aus `ORDR.DocDate`, wenn Position auf Auftrag basiert |
| `DocumentType` | `INV` oder `CRN` |

Nach dem Lesen wird bei `Material` ein Slash-Sonderfall bereinigt: wenn ein Slash vorkommt, wird der letzte Teil nach `/` verwendet.

## Italien-Sonderlogik

Nur fuer Schema `it01_p` wird in `HanaQueryService.BuildRevenueAccountFilter(...)` ein zusaetzlicher Query-Filter angewendet:

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

Bewertung fuer Andreas:

- Der Kontenfilter zielt auf `47005 - Ricavi vendite e prestazioni`.
- Der Ausschluss von `4700504%` entfernt `autofattura`-Konten.
- Die sechs ausgeschlossenen Kunden sind historisch als Arbeitsfilter dokumentiert.
- Diese Logik ist noch hart codiert und sollte fachlich final bestaetigt oder in eine pflegbare Finance-Regel/Konfiguration ueberfuehrt werden.

## Credential- und Verbindungslogik

Credential-Aufloesung:

```text
Username = Site.UsernameOverride, sonst SourceSystemDefinition.CentralUsername
Password = Site.PasswordOverride, sonst SourceSystemDefinition.CentralPassword
```

Technische HANA-Verbindung:

```text
ServerNode = Host:Port
UserName = aufgeloester Username
Password = aufgeloestes Password
DatabaseName = optional
encrypt / sslValidateCertificate = optional
AdditionalParams = optional, Semikolon-getrennt
```

Wichtig fuer Betrieb/Firewall:

- Der Webserver muss die HANA-Ziele erreichen, nicht der lokale Entwickler-PC.
- In der Deployment-Doku sind als bekannte Ziele genannt:
  - BI1/HANA intern: `10.194.65.22:30015`
  - India HANA: `20.197.20.60:30015`
  - SAP OData / ZSCHWEIZ: `10.194.64.29:8000`

## Rolle im Finance-Datenfluss

Beim Standortexport:

```text
HANA/B1 lesen
-> SalesRecord-Liste bilden
-> Transformationen anwenden
-> Sales_ProcessedMergeInput_<TSC>_<Datum>.csv schreiben, wenn Audit-CSV aktiv
-> Standort-Excel schreiben
-> CentralSalesRecords fuer Standort ersetzen
-> optional Upload nach SharePoint
```

Produktiver Auswertungsstand laut Finance-Doku:

```text
AuditCsvEnabled = 1
UseAuditCsvAsCentralSource = 1
LocalSiteExportFolder = leer
```

Damit lesen `Finance Summary`, `Management Analyse`, `Finance Pruefbuch` und `Finance Pivot` bevorzugt die neuesten `Sales_ProcessedMergeInput_*.csv` je TSC. Falls diese fehlen, wird auf die neueste zentrale `Finance_Dashboard_Audit_All_*.csv` zurueckgefallen. `Sales_All_*.xlsx` ist Nachweis/Export, aber nicht die Live-Quelle der Dashboard-Reiter.

## Abgrenzung zu SAP OData / ZSCHWEIZ

SAP OData ist ein anderer Konnektor (`SAP_GATEWAY`) und nicht Teil der B1-HANA-Standardstrecke.

Betroffen:

| Land | TSC | Quelle |
| --- | --- | --- |
| CH/AT | `ZSCHWEIZ` | SAP Gateway/OData `ZPOWERBI_EINKAUF_SRV` |

Aktive OData-Quellen:

| Alias | EntitySet | Zweck |
| --- | --- | --- |
| `Z` | `FinanzdataSchweizOeSet` | Verkaufs-/Finance-Zeilen CH/AT |
| `P` | `ProductDivisionRefSet` | fuehrende TR-AG-Spartenreferenz |

Diese Quelle liefert auch die zentrale Produktsparte-Referenz, gegen die andere Laender in der Analyse ueber Materialnummern gematcht werden.

## Punkte fuer kurze Abstimmung mit Andreas/Finance

1. Bestaetigen: Fuer FR/IT/US ist `LineTotal` aus B1-Positionen die fuehrende Net-Sales-Basis.
2. Bestaetigen: `DocDate` ist fuer B1-Laender die fuehrende Jahres-/Posting-Abgrenzung.
3. Bestaetigen: Gutschriften sollen weiterhin als negative Positionszeilen aus `ORIN`/`RIN1` laufen.
4. Italien klaeren: Ist der harte `47005%`/`not 4700504%`/Kundenausschluss fachlich final oder nur Uebergang?
5. Indien klaeren: Ist `TRIN -> SAGE -> 20.197.20.60:30015`, Schema `TRAFAG_LIVE`, weiterhin die richtige Finance-Quelle?
6. Credentials klaeren: zentrale Credentials je Quellsystem oder Standort-Overrides je Land beibehalten?
7. Firewall klaeren: BI1/HANA, India HANA und SAP OData vom produktiven Webserver aus freigegeben?
8. Gruppenmarge klaeren: Fuer echte Konzern-Standardkosten fehlen noch Quellen wie MBEW-STPRS bzw. SAP B1 je Liefergesellschaft; das ist nicht durch den bestehenden Net-Sales-B1-Konnektor erledigt.

## Offene technische Verbesserungen

- IT-Sonderfilter aus hartem Code in pflegbare Konfiguration/Finance-Regel verschieben.
- Produktive B1-/HANA-Konfiguration als exportiertes Config-Paket dokumentieren, damit Host/Port/Schemas/Credentials-Overrides nicht nur aus Seed/Doku rekonstruiert werden.
- Verbindungstest je Quellsystem/Standort fuer Andreas-Sitzung vorbereiten: BI1, SAGE/India, SAP OData.
- Mapping-/Audit-Auszug je B1-Land bereitstellen: Beispielzeilen aus `Sales_ProcessedMergeInput_<TSC>_*.csv` mit `InvoiceNumber`, `LineTotal`, `DocumentType`, `Currency`, `PostingDate`.
