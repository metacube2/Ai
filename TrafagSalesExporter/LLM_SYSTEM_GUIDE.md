# TrafagSalesExporter LLM System Guide

Stand: 2026-04-17

Diese Datei ist fuer andere LLMs gedacht, die das Projekt schnell verstehen und daraus Architekturtexte, Visualisierungen, Ablaufdiagramme oder UI-/Datenflussgrafiken erzeugen sollen.

## Zweck des Systems

`TrafagSalesExporter` ist eine Blazor Server App auf `.NET 8`, die Verkaufsdaten aus mehreren Quellsystemen in ein gemeinsames Zielschema ueberfuehrt.

Quellsysteme:

- `HANA`-basierte Systeme wie `BI1` und `SAGE`
- `SAP_GATEWAY` ueber OData
- `MANUAL_EXCEL` aus hochgeladenen oder referenzierten Excel-Dateien

Zielbild:

- jede Quelle wird in `SalesRecord` normalisiert
- Standortdaten koennen lokal als Excel exportiert werden
- alle Datensaetze werden in `CentralSalesRecords` gespeichert
- eine zentrale konsolidierte Datei wird aus dem zentralen Datenbestand erzeugt
- ein `Management Cockpit` analysiert sowohl exportierte Dateien als auch zentrale Rohdaten

## Technologie-Stack

- UI: Blazor Server + MudBlazor
- Datenbank: SQLite (`trafag_exporter.db`)
- Excel lesen/schreiben: ClosedXML
- SAP HANA Zugriff: `Sap.Data.Hana.Core.v2.1.dll`
- SAP Gateway / OData: eigener Service ueber HTTP
- SharePoint Upload/Download: Microsoft Graph + Azure Identity
- Tests: xUnit

## Einstiegspunkte

Wichtige Dateien:

- [Program.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Program.cs)
- [Data/AppDbContext.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Data/AppDbContext.cs)
- [Components/Layout/NavMenu.razor](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Components/Layout/NavMenu.razor)

`Program.cs` registriert fast die komplette Architektur ueber DI und fuehrt beim Start `DatabaseInitializationService.InitializeAsync()` aus.

## Hauptseiten

Navigation:

- `/` Dashboard
- `/standorte`
- `/transformations`
- `/management-cockpit`
- `/settings`
- `/logs`

Dateien:

- [Components/Pages/Dashboard.razor](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Components/Pages/Dashboard.razor)
- [Components/Pages/Standorte.razor](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Components/Pages/Standorte.razor)
- [Components/Pages/Transformations.razor](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Components/Pages/Transformations.razor)
- [Components/Pages/ManagementCockpit.razor](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Components/Pages/ManagementCockpit.razor)
- [Components/Pages/Settings.razor](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Components/Pages/Settings.razor)
- [Components/Pages/Logs.razor](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Components/Pages/Logs.razor)

Kurzrollen:

- `Dashboard`: Einzel-Export, Alle exportieren, zentrale Datei neu erzeugen, Live-Status
- `Standorte`: Standortpflege, zentrale HANA-Technik, SAP-Konfiguration pro Standort, manueller Excel-Import
- `Transformations`: feldweise und record-basierte Regeln
- `Management Cockpit`: Dateianalyse und Rohanalyse aus `CentralSalesRecords`
- `Settings`: SharePoint, Exportpfade, Quellsysteme, Wechselkurse, Config Import/Export
- `Logs`: technische Ereignisprotokolle

## Kernmodelle

Wichtige Entity-Klassen:

- [Models/Site.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Models/Site.cs)
- [Models/SourceSystemDefinition.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Models/SourceSystemDefinition.cs)
- [Models/HanaServer.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Models/HanaServer.cs)
- [Models/SalesRecord.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Models/SalesRecord.cs)
- [Models/CentralSalesRecord.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Models/CentralSalesRecord.cs)
- [Models/FieldTransformationRule.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Models/FieldTransformationRule.cs)
- [Models/SapSourceDefinition.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Models/SapSourceDefinition.cs)
- [Models/SapJoinDefinition.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Models/SapJoinDefinition.cs)
- [Models/SapFieldMapping.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Models/SapFieldMapping.cs)
- [Models/SharePointConfig.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Models/SharePointConfig.cs)
- [Models/ExportSettings.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Models/ExportSettings.cs)
- [Models/ExportLog.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Models/ExportLog.cs)
- [Models/AppEventLog.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Models/AppEventLog.cs)
- [Models/CurrencyExchangeRate.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Models/CurrencyExchangeRate.cs)

Wichtige Relationen:

- `Site -> HanaServer` optional
- `Site -> SapSourceDefinitions`
- `Site -> SapJoinDefinitions`
- `Site -> SapFieldMappings`
- `Site -> CentralSalesRecords`
- `SourceSystemDefinition` ist zentrale Stammdatenquelle fuer Quellsysteme

## Datenbanktabellen

`AppDbContext` enthaelt:

- `HanaServers`
- `SourceSystemDefinitions`
- `Sites`
- `SharePointConfigs`
- `ExportSettings`
- `ExportLogs`
- `AppEventLogs`
- `FieldTransformationRules`
- `CurrencyExchangeRates`
- `SapSourceDefinitions`
- `SapJoinDefinitions`
- `SapFieldMappings`
- `CentralSalesRecords`

## Architekturrollen der Services

### Export / Orchestrierung

- [Services/ExportOrchestrationService.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Services/ExportOrchestrationService.cs)
- [Services/SiteExportService.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Services/SiteExportService.cs)
- [Services/ConsolidatedExportService.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Services/ConsolidatedExportService.cs)
- [Services/CentralSalesRecordService.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Services/CentralSalesRecordService.cs)
- [Services/ExportLogService.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Services/ExportLogService.cs)

Rollen:

- `ExportOrchestrationService` steuert UI-nahe Exportlaeufe und Live-Status
- `SiteExportService` entscheidet anhand des Quellsystems, wie ein Standort gelesen wird
- `CentralSalesRecordService` ersetzt zentrale Saetze pro Standort
- `ConsolidatedExportService` erzeugt die zentrale Datei

### Datenquellen

- [Services/HanaQueryService.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Services/HanaQueryService.cs)
- [Services/SapGatewayService.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Services/SapGatewayService.cs)
- [Services/SapCompositionService.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Services/SapCompositionService.cs)
- [Services/ManualExcelImportService.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Services/ManualExcelImportService.cs)
- [Services/SharePointUploadService.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Services/SharePointUploadService.cs)

Rollen:

- `HanaQueryService`: SQL gegen SAP B1/HANA-nahe Schemata
- `SapGatewayService`: OData-Metadaten und Reads
- `SapCompositionService`: Mehrquellen-/Join-/Mapping-Aufbau fuer SAP
- `ManualExcelImportService`: Import im Exportformat aus `.xlsx`
- `SharePointUploadService`: Upload fuer Exportdateien und Download fuer manuelle Excel-Dateien

### Transformation / Mapping

- [Services/TransformationCatalog.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Services/TransformationCatalog.cs)
- [Services/TransformationStrategies.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Services/TransformationStrategies.cs)
- [Services/RecordTransformationService.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Services/RecordTransformationService.cs)
- [Services/CurrencyExchangeRateService.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Services/CurrencyExchangeRateService.cs)
- [Services/ExchangeRateImportService.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Services/ExchangeRateImportService.cs)

Rollen:

- `Value`-Transformationen fuer einzelne Felder
- `Record`-Transformationen fuer zeilenweite Regeln
- Wechselkursimport und -umrechnung

### Reporting / Monitoring / Infrastruktur

- [Services/ManagementCockpitService.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Services/ManagementCockpitService.cs)
- [Services/AppEventLogService.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Services/AppEventLogService.cs)
- [Services/ConfigTransferService.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Services/ConfigTransferService.cs)
- [Services/DatabaseInitializationService.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Services/DatabaseInitializationService.cs)
- [Services/TimerBackgroundService.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Services/TimerBackgroundService.cs)

## Der wichtigste technische Ablauf

### 1. Standort-Export

Pfad:

`Dashboard/Standorte -> ExportOrchestrationService -> SiteExportService`

`SiteExportService` unterscheidet drei Modi:

1. `SAP_GATEWAY`
   - SAP-Quellen lesen
   - SAP-Joins anwenden
   - SAP-Feldmappings auf `SalesRecord`
   - Transformationen anwenden
   - Standort-Excel erzeugen
   - `CentralSalesRecords` ersetzen
   - optional SharePoint-Upload

2. `HANA`
   - effektive zentrale HANA-Konfiguration laden
   - optionale Standort-Credential-Overrides anwenden
   - SQL in HANA ausfuehren
   - `SalesRecord` erzeugen
   - Transformationen anwenden
   - Standort-Excel erzeugen
   - `CentralSalesRecords` ersetzen
   - optional SharePoint-Upload

3. `MANUAL_EXCEL`
   - `ManualImportFilePath` auswerten
   - wenn lokal/UNC vorhanden: lokal lesen
   - wenn SharePoint-Referenz: via Graph temp herunterladen
   - Excel in `SalesRecord` lesen
   - Transformationen anwenden
   - keine neue Standortdatei erzeugen, bestehende Excel dient als Eingabe
   - `CentralSalesRecords` ersetzen

### 2. Konsolidierter Export

Pfad:

`Dashboard -> ExportOrchestrationService -> ConsolidatedExportService`

Semantik aktuell:

- die zentrale Datei basiert fachlich auf `CentralSalesRecords`
- `ExportAllAsync()` sammelt zwar auch `consolidatedRecords`, aber die zentrale Exportsemantik ist historisch noch nicht vollkommen bereinigt

### 3. Management Cockpit

Zwei Betriebsarten:

1. Dateibasiert
   - vorhandene `.xlsx` waehlen
   - Datei mit ClosedXML lesen
   - Summenfeld waehlen
   - Anzeige-Waehrung waehlen
   - Kennzahlen, Top-Listen, Datenqualitaet, Findings erzeugen

2. Zentraldatenbasiert
   - direkt aus `CentralSalesRecords`
   - Jahr/Monat Filter
   - Summenfeld waehlen
   - optionale weitere Summenfelder fuer Zeitreihen waehlen
   - Anzeige-Waehrung waehlen
   - Rohsicht ohne Intercompany-, Budget- oder Spartelogik

Aktuelle Summenfelder:

- `Sales Price/Value`
- `Quantity`
- `Standard cost`
- `Quantity * Standard cost`

Aktuelle Anzeige-Waehrungen:

- `EUR`
- `USD`
- `Original`

Die Waehrungsumrechnung nutzt `CurrencyExchangeRateService`. Bei `Original` bleiben Werte in Quellwaehrungen gruppiert. Nicht-betragliche Summenfelder wie `Quantity` haben keine Waehrung. Fehlende Wechselkurse werden gezaehlt und in Hinweisen bzw. Findings sichtbar; betroffene Werte werden in der Zielwaehrung mit `0` einbezogen.

## Quellsystemlogik

### SourceSystemDefinition

`SourceSystemDefinition` ist die fuehrende Wahrheit fuer:

- `Code`
- `DisplayName`
- `ConnectionKind`
- `IsActive`
- `CentralUsername`
- `CentralPassword`
- `CentralServiceUrl` fuer SAP

Anschlussarten:

- `HANA`
- `SAP_GATEWAY`
- `MANUAL_EXCEL`

### HANA

Fachliche Logik:

- zentrale technische HANA-Konfiguration pro Quellsystem
- keine separaten Vollverbindungen pro Standort
- Standort speichert nur Fachdaten plus optionale Username-/Password-Overrides

Schema-Lookup:

- in `Standorte` gibt es jetzt `Schemas laden`
- Lookup fragt `sys.tables` in HANA ab
- eingeschraenkt auf typische B1-Schemas mit Tabellen wie `OINV`, `INV1`, `ORIN`, `RIN1`, `OCRD`, `OITM`

### SAP

Fachliche Logik:

- zentrale SAP Service URL in `SourceSystemDefinition.CentralServiceUrl`
- Standort kann `SapServiceUrl` als Override pflegen
- pro Standort gibt es SAP-Quellen, Joins und Feldmappings

### Manual Excel

Fachliche Logik:

- `Site.ManualImportFilePath` kann sein:
  - lokaler Windows-Pfad
  - UNC-Pfad
  - SharePoint-URL
  - SharePoint-Pfad unterhalb der konfigurierten Site
- Standortdaten werden aus der Excel eingelesen und in `CentralSalesRecords` uebernommen
- SharePoint dient hier als Eingangsquelle, nicht nur als Exportziel

## Transformationen

Das System unterscheidet:

- `Value`-Transformationen
- `Record`-Transformationen

Beispiele:

- `Copy`
- `Uppercase`
- `Lowercase`
- `Prefix`
- `Suffix`
- `Replace`
- `Constant`
- `NormalizeCurrencyCode`
- `FirstNonEmpty`
- `ConvertCurrency`

Technischer Ablauf:

- Regeln liegen in `FieldTransformationRules`
- `TransformationCatalog` meldet verfuegbare Strategien an die UI
- `RecordTransformationService` wendet record-basierte Strategien an

## Wechselkurse

Vorhanden:

- `CurrencyExchangeRates`
- `ExchangeRateImportService` fuer ECB-Tageskurse
- `NormalizeCurrencyCode`
- `ConvertCurrency`
- `ManagementCockpitService` kann betragliche Cockpit-Kennzahlen in `EUR` oder `USD` umrechnen

Wichtig:

- die Rohsicht im `Management Cockpit` kann jetzt Anzeige-Waehrungen nutzen
- `CHF` ist im Cockpit aktuell nicht als direkte Anzeige-Waehrung in der UI angeboten
- CHF bleibt weiterhin Teil des allgemeinen Transformationssystems
- fachlich ist noch zu klaeren, ob CHF als Standard- oder zusaetzliche Cockpit-Anzeige-Waehrung gebraucht wird

## SharePoint-Rolle im Gesamtsystem

`SharePointConfig` enthaelt:

- `SiteUrl`
- `ExportFolder`
- `CentralExportFolder`
- `TenantId`
- `ClientId`
- `ClientSecret`

Verwendung:

- Upload von Standort-Exporten
- Upload der zentralen Datei
- Download von manuellen Excel-Dateien fuer `MANUAL_EXCEL`

Wichtig:

- die App arbeitet gegen dieselbe SharePoint-Site, die in `Settings` konfiguriert ist
- fuer `MANUAL_EXCEL` muessen Referenzen auf derselben Site aufloesbar sein

## Startinitialisierung / Migrationen

Kritische Datei:

- [Services/DatabaseInitializationService.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Services/DatabaseInitializationService.cs)

Aktuelle Rolle:

- `EnsureCreated`
- Schema-Ergaenzungen per `ALTER TABLE`
- Tabellen-Rebuilds bei Legacy-Schemas
- FK-Reparaturen
- Stammdaten-Seeding
- empfohlene Transformationsregeln

Bekannte Architekturrealitaet:

- das ist funktional hilfreich, aber kein sauberes Migrationssystem
- die Startlogik traegt produktive Schema-Reparaturverantwortung
- das ist einer der wichtigsten technischen Risikobloecke

Bereits gehaertete Fehlerbilder:

- kaputte FK-Referenzen auf `Sites_old`
- kaputte FK-Referenzen auf `HanaServers_repair_old`
- Legacy-Credential-Spalten in `ExportSettings`
- Legacy-Credential-Spalten in `HanaServers`
- verschobene Spalten im `Sites_old -> Sites`-Kopierpfad

## Config Import / Export

Dateien:

- [Services/ConfigTransferService.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Services/ConfigTransferService.cs)
- [Models/ConfigTransferPackage.cs](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/Models/ConfigTransferPackage.cs)

Aktueller Stand:

- JSON Export/Import fuer Konfiguration
- Secrets optional
- `SourceSystemDefinitions` im aktuellen Modell enthalten
- HANA-Technik ohne HANA-Credentials
- Standort-Overrides bleiben erhalten

Wichtige Punkte:

- Import laeuft jetzt transaktional
- alte `ConnectionKind`-lose Formate bekommen Fallbacks
- `CentralSalesRecords` werden nicht mehr blind geloescht
- bestehende zentrale Laufzeitdaten werden fuer weiterhin vorhandene Standorte remappt

## Logging

Es gibt zwei Log-Ebenen:

- `ExportLogs` fuer fachliche Exporthistorie
- `AppEventLogs` fuer technische und UI-nahe Ereignisse

Die `Logs`-Seite liest vor allem `AppEventLogs`.

## Tests

Testprojekt:

- [TrafagSalesExporter.Tests](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/TrafagSalesExporter.Tests)

Aktuell vorhandene Schwerpunkte:

- Transformationen
- Record-Transformationen
- TransformationCatalog
- CurrencyExchangeRateService
- ExchangeRateImportService
- ManualExcelImportService
- ManagementCockpitService
- ConfigTransferService
- DatabaseInitializationService

`ManagementCockpitServiceTests` decken inzwischen auch ab:

- zentrale Analyse nach Jahr/Monat
- Tages-, Monats-, Jahres-, Quellen- und Laenderwerte
- waehlbare Summenfelder
- Waehrungsumrechnung in EUR
- Wechselkurs-Caching
- Mengen-Auswertung ohne Waehrungsumrechnung
- Zusatz-Summenfelder in Zeitreihen

Wichtig:

- es gibt aktuell keine echten UI-Komponententests mit `bUnit`
- es gibt keine Browser-E2E-Tests mit `Playwright`
- viele Button-Aktionen sind nur indirekt ueber Services und Persistenz getestet

## Bekannte offene Architekturfragen

Fuer andere LLMs wichtig, damit Visualisierungen nicht zu glatt oder zu idealisiert werden:

1. `DatabaseInitializationService` ist ein produktiver Reparatur-/Migrationslayer, nicht nur Bootstrap.
2. `Settings.razor` und `Standorte.razor` enthalten weiterhin relativ viel Anwendungslogik.
3. Die Semantik der konsolidierten Datei ist historisch teilweise doppelt angelegt.
4. Das `Management Cockpit` ist noch kein voll generalisierter Reporting-Layer.
5. SharePoint ist sowohl Exportziel als auch bei `MANUAL_EXCEL` mittlerweile moegliche Eingangsquelle.

## Empfohlene Diagramme fuer andere LLMs

### 1. Kontextdiagramm

Zeige:

- Benutzer
- Blazor App
- SQLite
- SAP HANA
- SAP Gateway
- lokale Dateisystempfade
- SharePoint

### 2. Komponenten-/Service-Diagramm

Gruppiere:

- UI
- Orchestrierung
- Quelladapter
- Transformation
- Persistenz
- Reporting

### 3. Datenflussdiagramm pro Quelltyp

Je ein separater Flow fuer:

- HANA
- SAP Gateway
- Manual Excel lokal
- Manual Excel SharePoint

### 4. ER-Diagramm

Fokussiere auf:

- `SourceSystemDefinition`
- `HanaServer`
- `Site`
- `SapSourceDefinition`
- `SapJoinDefinition`
- `SapFieldMapping`
- `CentralSalesRecord`
- `FieldTransformationRule`

### 5. Sequenzdiagramm fuer Export

Wichtige Stationen:

- Dashboard
- ExportOrchestrationService
- SiteExportService
- spezifischer Quellservice
- Transformation
- CentralSalesRecordService
- Excel/SharePoint
- ExportLog/AppEventLog

## Prompt-Vorlage fuer ein anderes LLM

Wenn ein anderes LLM daraus Visualisierungen erzeugen soll, funktioniert diese Anweisung gut:

> Lies `LLM_SYSTEM_GUIDE.md` als primaeren Systemkontext. Erzeuge daraus ein Architekturdiagramm, ein Datenflussdiagramm fuer HANA/SAP/MANUAL_EXCEL, ein ER-Diagramm der wichtigsten Tabellen und ein Sequenzdiagramm fuer `ExportAsync`. Achte darauf, dass `DatabaseInitializationService` produktive Reparaturlogik enthaelt und dass `MANUAL_EXCEL` sowohl lokal als auch ueber SharePoint gelesen werden kann.

## Weitere Kontextdateien

Zusatzkontext fuer Verlauf und Risiken:

- [HANDOFF_2026-04-15.md](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/HANDOFF_2026-04-15.md)
- [NEXT_STEPS_2026-04-15.md](C:/Users/koi/source/repos/Ai/TrafagSalesExporter/NEXT_STEPS_2026-04-15.md)

Diese beiden Dateien sind wichtig, wenn ein anderes LLM nicht nur Struktur, sondern auch historische Umbauten, Risiken und Prioritaeten verstehen soll.
