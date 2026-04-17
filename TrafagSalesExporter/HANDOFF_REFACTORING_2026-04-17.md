# Handoff: DataSourceAdapter-Refactoring (2026-04-17)

**Branch:** `claude/review-trafag-tool-JONMq`
**Commit:** `82ac7df` ("DataSourceAdapter-Pattern + SiteExportService schlanker + Page-Services Scoped")
**Basis:** `main` @ `2a56ba5` (umfangreiches refactoring)

## Kontext fuer den naechsten LLM

Vorheriges Review hatte drei Architektur-Punkte beanstandet:
1. `SiteExportService` war zu gross (338 Zeilen, if/else auf ConnectionKind)
2. Fehlende Adapter-Abstraktion fuer Datenquellen (HANA / SAP_GATEWAY / MANUAL_EXCEL)
3. Alle Services Singleton, auch UI-nahe Page-Services

Dieses Refactoring adressiert alle drei Punkte. **Nicht** im Scope (absichtlich offen gelassen):
- SQL-Injection-Risiko in `HanaQueryService:191,204`
- `.GetAwaiter().GetResult()` Blocking in `HanaQueryService`
- Secret-Store-Integration
- Retry/Polly

## Was konkret geaendert wurde

### Neu: `Services/DataSources/`
| Datei | Zweck |
|---|---|
| `IDataSourceAdapter.cs` | Interface mit `ConnectionKind` + `FetchAsync(context)` |
| `DataSourceFetchContext.cs` | Input: Site, SourceDefinition, Settings, SharePointConfig, UpdateStatus |
| `DataSourceFetchResult.cs` | Output: Records + optionaler `ReferenceFilePath` (Manual Excel liefert Quell-Datei als Referenz) |
| `IDataSourceAdapterResolver.cs` + `DataSourceAdapterResolver.cs` | Dictionary-Lookup nach ConnectionKind |
| `HanaDataSourceAdapter.cs` | Baut `HanaServer` aus zentraler Config + Site-Overrides, ruft `IHanaQueryService.GetSalesRecords` |
| `SapGatewayDataSourceAdapter.cs` | Laedt SapSources/Joins/Mappings, ruft `ISapCompositionService.BuildSalesRecordsAsync` |
| `ManualExcelDataSourceAdapter.cs` | Lokale Datei oder SharePoint-Download, ruft `IManualExcelImportService.ReadSalesRecordsAsync` |
| `DataSourceCredentials.cs` | Interner Helper (FirstNonEmpty, Resolve, ResolveSapServiceUrl) |

### Geaendert: `Services/SiteExportService.cs`
338 -> 187 Zeilen. Jetzt reine Pipeline:
```
1. NormalizeSourceSystem
2. LoadExportConfigAsync (settings, spConfig, sourceDefinition, rules) - 1x DbContext
3. Resolve adapter per ConnectionKind
4. adapter.FetchAsync -> records (+ optional ReferenceFilePath)
5. Transform (_transformationService.Apply)
6. Excel erzeugen (falls Adapter keine Referenzdatei liefert)
7. CentralSalesRecordService.ReplaceForSiteAsync
8. UploadToSharePointIfConfiguredAsync
```
Entferntes Dead-Injection: `ISapGatewayService` (wurde konstruiert aber nie benutzt).

### Geaendert: `Program.cs`
- Adapter registriert (3x `AddSingleton<IDataSourceAdapter, ...>` + Resolver)
- **Page-Services auf Scoped** (`ISettingsPageService`, `IStandortePageService`, `IStandorteSapEditorService`, `IManagementCockpitPageService`, `IDashboardPageService`, `ILogsPageService`, `ITransformationsPageService`) — pro Blazor-Circuit
- `ExportOrchestrationService` bleibt bewusst Singleton (geteilter Export-Status ueber Circuits via `OnExportStatusChanged`)
- Stateless Connector-/Infra-Services bleiben Singleton

## Was der naechste LLM pruefen / testen soll

### 1. Build (ICH KONNTE NICHT BAUEN — kein dotnet SDK in der Sandbox)
```bash
cd TrafagSalesExporter
dotnet restore
dotnet build
```
Falls Fehler: hohe Wahrscheinlichkeit, dass ich ein `using` vergessen oder einen Interface-Namen vertippt habe. Kandidaten fuer Tippfehler: `DataSourceCredentials.FirstNonEmpty` in `SiteExportService.cs:181`, Adapter-Constructoren in `Services/DataSources/*.cs`.

### 2. Tests laufen lassen
```bash
cd TrafagSalesExporter
dotnet test
```
Bestehende Tests in `TrafagSalesExporter.Tests/` referenzieren **keinen** der refactorierten Services direkt (siehe grep: `SiteExportService|IDataSource` liefert keine Treffer in Tests). Sollten also gruen bleiben.

### 3. Manueller Smoke-Test der drei Quellsysteme
In der Blazor-UI (Standorte-Seite, Export-Button):
- **HANA-Standort**: Export starten — muss wie vorher Records aus HANA ziehen, Excel erzeugen, zentrale Tabelle aktualisieren, optional nach SharePoint uploaden.
- **SAP_GATEWAY-Standort**: Export starten — muss SAP-Quellen/Joins/Mappings laden, Records ueber `SapCompositionService` bauen.
- **MANUAL_EXCEL-Standort** (lokaler Pfad): Referenz-Excel wird gelesen, **keine** neue Excel-Datei erzeugt (Referenzdatei bleibt).
- **MANUAL_EXCEL-Standort** (SharePoint-Pfad, `/Shared Documents/...`): temporaerer Download, lesen, Temp-Datei wird im `finally` wieder geloescht.

**Verhaltens-Aequivalenz** zur vorherigen Implementierung ist das Pruefkriterium — keine neue Funktionalitaet, nur Struktur.

### 4. Captive-Dependency-Check
Scoped -> Singleton wuerde DI-Fehler werfen. Ich habe per grep verifiziert, dass kein Singleton eine `I*PageService` konsumiert. Wer das nochmal manuell pruefen moechte:
```bash
grep -rn "PageService" TrafagSalesExporter/Services/ | grep -v "PageService.cs"
```
Sollte nur Registrierungen in Program.cs und UI-Komponenten zeigen.

### 5. Erweiterbarkeit testen
Um ein viertes Quellsystem hinzuzufuegen, reicht jetzt:
1. Konstante in `Models/SourceSystemDefinition.cs::SourceSystemConnectionKinds`
2. Neuer `IDataSourceAdapter` in `Services/DataSources/`
3. `builder.Services.AddSingleton<IDataSourceAdapter, NeuerAdapter>();` in `Program.cs`

Kein Eingriff in `SiteExportService` noetig.

## Offene Themen fuer Follow-up-PRs

1. **SQL-Injection (kritisch)** — `HanaQueryService.cs:191,204`: `schema`, `tsc`, `dateFilter` via String-Interpolation. Auf `HanaCommand`-Parameter umstellen (Beispiel: `GetAvailableSchemas()` nutzt das bereits korrekt).
2. **Blocking async** — `HanaQueryService` hat 8x `.GetAwaiter().GetResult()`. In Blazor Server Deadlock-Risiko — auf echtes `async/await` migrieren.
3. **Tests fuer Adapter** — Unit-Tests fuer die drei neuen Adapter mit Fakes der Connector-Services waeren sinnvoll. `DataSourceAdapterResolver`-Test (Dictionary-Lookup, Fehler bei unbekanntem Kind) einfach zu schreiben.
4. **Retry-Layer** — HTTP-Requests zu SharePoint/SAP Gateway ohne Polly. Bei Netzflackern bricht Export ab.

## Dateien-Cheatsheet

```
TrafagSalesExporter/
├── Program.cs                                      [MOD: Lifetimes + Adapter-Registrierung]
├── Services/
│   ├── SiteExportService.cs                        [MOD: 338 -> 187 Zeilen, pure Pipeline]
│   └── DataSources/                                [NEU]
│       ├── IDataSourceAdapter.cs
│       ├── IDataSourceAdapterResolver.cs
│       ├── DataSourceAdapterResolver.cs
│       ├── DataSourceFetchContext.cs
│       ├── DataSourceFetchResult.cs
│       ├── DataSourceCredentials.cs
│       ├── HanaDataSourceAdapter.cs
│       ├── SapGatewayDataSourceAdapter.cs
│       └── ManualExcelDataSourceAdapter.cs
```
