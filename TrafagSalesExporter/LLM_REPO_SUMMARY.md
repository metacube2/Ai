# TrafagSalesExporter / BiDashboard — Kompakt-Kontext fuer LLM

Stand: 2026-06-19. Zweck: Diese **eine** Datei reicht aus, damit eine LLM
**jede** Aufgabe an dieser App einordnen kann — neue Features, Erweiterungen,
Korrekturen/Bugfixes, Refactorings oder einfach Verstaendnisfragen — ohne das
ganze Repo zu kennen. Der Gauge/Manometer ist nur **ein Beispiel** von vielen
(Abschnitt 6). Enthalten: Architektur + Konventionen, ein
**Erweiterungs-/Korrektur-Playbook** (Abschnitt 5b: „welche Aufgabe fasst welche
Datei an“), **echter Schluessel-Code** (6b) und ein **RAG-komprimierter
Signaturen-Katalog** des restlichen Codes (6c). Keine Geheimnisse, keine
Rohdaten. Volle Implementierungen sind bewusst NICHT enthalten — die LLM fordert
sie ueber den Katalog gezielt nach („brauche `Service.MethodeXy`“).

**Arbeitsweise fuer die LLM:** 1) Aufgabentyp in Abschnitt 5b nachschlagen →
betroffene Dateien/Services. 2) Konventionen (Abschnitt 3) einhalten. 3) Fehlt
konkrete Logik, ueber Katalog 6c die genaue Methode/Datei anfordern, statt zu
raten. Gilt fuer alle Bereiche: Einkauf, Finance, HR, Export, Import,
Transformationen, Navigation, Admin/Security.

---

## 1. Was die App ist

- **.NET 8 Blazor Server** App (interactive server render mode), UI-Framework
  **MudBlazor 7.15**. Sprache C#, `Nullable` + `ImplicitUsings` aktiviert.
- Projektname `TrafagSalesExporter`, **AssemblyName = `BiDashboard`**,
  RootNamespace `TrafagSalesExporter`.
- Sammelt Verkaufs-/Einkaufsdaten aus mehreren Quellen (SAP OData Gateway,
  SAP HANA / SAP B1, SharePoint, manuelle Excel/CSV), normalisiert sie in eine
  zentrale Tabelle und stellt Dashboards + Excel-/CSV-Exporte bereit.
- Persistenz: **SQLite** via EF Core 8 (`Data Source=trafag_exporter.db`).
- Auth: Windows/IIS Negotiate in Prod; im Development ein Bypass-Handler
  (`DevelopmentAuthenticationHandler`). Bereichsschutz (Finance/HR/Admin) ueber
  Cookie-Unlock + Policies.

### Tech-Stack-Fakten (fuer Code, der kompiliert)
- `net8.0`, `<PlatformTarget>x64</PlatformTarget>`.
- NuGet: `MudBlazor 7.15.0`, `Microsoft.EntityFrameworkCore.Sqlite 8.0.11`,
  `ClosedXML 0.104.2` (Excel), `Microsoft.Graph 5.80` (SharePoint),
  `Azure.Identity`, `Microsoft.AspNetCore.Authentication.Negotiate`.
- SAP HANA Client DLL wird per `<Reference HintPath=...>` referenziert
  (Pfad via MSBuild-Property `HanaClientDll`); nicht im NuGet.
- Tests: separates Projekt `TrafagSalesExporter.Tests` (xUnit-Stil), Lauf:
  `dotnet test TrafagSalesExporter.sln`. Aktueller Stand 101/101 gruen.

---

## 2. Architektur & Datenfluss

```
SAP OData Gateway ┐
SAP HANA / B1     ├─ IDataSourceAdapter (Strategy je ConnectionKind)
Manual Excel/CSV  ┘        │
SharePoint  ──────────────┤
                          ▼
        IMappedSalesRecordComposer / RecordTransformationService
        (Feld-Mapping + Transformations-Regeln + Waehrungsumrechnung)
                          ▼
                CentralSalesRecords  (zentrale EF-Tabelle, SQLite)
                          ▼
   ┌──────────────┬───────────────┬──────────────┬───────────────┐
   Excel-Export   Finance-Cockpit  HR-KPI         Einkaufs-Dash
   (ClosedXML)    (Soll/Ist,Regeln)(Rexx-Quelle)  (eigener SAP-Cache)
```

Wichtig:
- **Eine** Regelengine (`FinanceRuleEngine`) soll fuer Finance-Auswertung und
  zentrale Excel gelten.
- Produktsparten-Mapping ist eine **eigene Mapping-Schicht**, keine versteckte
  Finance-Regel; Referenz kommt als flache SAP/ABAP-Tabelle
  (`ProductDivisionRefSet`).
- Das **Einkaufs-Dashboard** zieht NICHT aus `CentralSalesRecords`, sondern aus
  einem eigenen SAP-OData-Full/Delta-Cache in SQLite-Tabellen
  `PurchasingEkkoCache` / `PurchasingEkpoCache` / `PurchasingEketCache`
  (EKKO=Kopf, EKPO=Position, EKET=Einteilung). Befuellt von
  `PurchasingDataRefreshService` (Full Load + 7-Tage-Delta).

---

## 3. Projektstruktur & Konventionen

```
Program.cs                 DI-Registrierung + Auth + /access/*-Endpoints
Components/
  App.razor, Routes.razor, _Imports.razor
  Layout/  (MainLayout, NavMenu aus DB-Menuebaum)
  Pages/   *.razor  ← eine Datei pro Seite, @page-Routen
  <Bereich>/  bereichsspezifische Teilkomponenten
Services/   I<Name>Service.cs (Interface) + <Name>Service.cs (Impl)
  DataSources/  Adapter-Strategy
Models/     POCOs + EF-Entities + UI-Record-Modelle
Data/AppDbContext.cs
Security/   Options + Policies + Dev-Auth-Handler
docs/rag/   token-arme RAG-Kurzdateien (Router: docs/RAG_ROUTER.md)
wwwroot/    css/app.css, js/  (three.min.js fuer 3D, download.js, finance3d.js)
```

### Konventionen, die jede Erweiterung einhalten muss
1. **Interface + Impl getrennt**: jeder Service hat `IXxxService` und `XxxService`.
2. **DI-Lifetimes** (in `Program.cs`):
   - **Singleton** = stateless Infrastruktur/Connectors/Orchestrator
     (HANA, Excel, SAP-Gateway, Transformationen, Caches, Export).
   - **Scoped** = UI-/Page-Services (ein `…PageService`/Dashboard-Service pro
     Blazor-Circuit). Beispiel: `IPurchasingDashboardService`,
     `IDashboardPageService`.
   - Neue Services dort registrieren, sonst DI-Fehler zur Laufzeit.
3. **DbContext nie injizieren** — immer `IDbContextFactory<AppDbContext>` und
   `await using var db = await _dbFactory.CreateDbContextAsync(ct);`.
   (DbContext ist nicht thread-safe; Singletons wuerden ihn sonst teilen.)
4. **Page-Service-Pattern**: `.razor` haelt nur UI + Feldstate; Datenlogik
   liegt im zugehoerigen Scoped-Service, der ein `…LiveState`/Result-Objekt
   zurueckgibt. Razor ruft `await Service.LoadAsync(filter)` in
   `OnInitializedAsync`/bei Filteraenderung.
5. **Zweisprachigkeit**: UI-Texte ueber `T("Deutsch", "English")`
   (`IUiTextService UiText`, lokale Hilfsmethode `T`). Technische Feldnamen
   (SAP-Entity-Sets, MATNR, EKKO…) bleiben unuebersetzt.
6. **Records fuer DTOs**: `public sealed record Xxx(...)` fuer Chart-Punkte,
   Filter, Zeilen (siehe `PurchasingLiveChartPoint(string Label, decimal Value)`).
7. **Navigation kommt aus der DB**: Menuepunkte sind `NavigationMenuItem`-Zeilen,
   gerendert von `NavigationMenuService` / `NavMenu`. Admins haengen sie unter
   `Admin > Menuestruktur` um. Neue Seite = neue `@page`-Route **und** ein
   Seed-/Menue-Eintrag, sonst ist sie nur per URL erreichbar.
8. **CancellationToken** durchreichen; async ueberall.

---

## 4. Datenmodell (EF DbSets in `AppDbContext`)

`HanaServers`, `SourceSystemDefinitions`, `Sites`, `SharePointConfigs`,
`ExportSettings`, `ExportLogs`, `AppEventLogs`, `FieldTransformationRules`,
`CurrencyExchangeRates`, `FinanceReferences`, `FinanceIntercompanyRules`,
`FinanceRules`, `SapSourceDefinitions`, `SapJoinDefinitions`,
`SapFieldMappings`, `ManualExcelColumnMappings`, **`CentralSalesRecords`**
(zentrale Faktentabelle), `NavigationMenuItems`.

Einkauf-Cache-Tabellen (`PurchasingEkko/Ekpo/EketCache`) werden NICHT als EF-
Entity gefuehrt, sondern per roher `SqliteConnection` gelesen/geschrieben
(`PurchasingDataRefreshService`). Schema-Anlage in
`DatabaseInitializationService(.SchemaSql).cs`; Seed in `DatabaseSeedService`.

---

## 5. Dashboards (Ziele fuer neue Features)

- **Einkauf** `/einkauf` (`Components/Pages/PurchasingDashboard.razor`, ~2.4k
  Zeilen, viele Unterrouten: `/einkauf/spend`, `/offene-bestellungen`,
  `/kontrakte`, `/lieferanten`, `/ideen…`, `/kennzahlen`, `/pbix`, `/3d`).
  Datenquelle: `IPurchasingDashboardService.LoadAsync(PurchasingDashboardFilter)`
  → `PurchasingDashboardLiveState` (KPIs, Spend-Matrix Lieferant×Jahr,
  Chart-Rows, Ideen-Analysezeilen). Filter: Zeitraum 2020–heute,
  Loeschkennzeichen-/MARA-MSTAE-Ausschluss. Refresh via
  `IPurchasingDataRefreshService` (Full/Delta).
- **Finance Cockpit** `FinanceComparison.razor` / `ManagementCockpit.razor`:
  Soll/Ist, Finance Summary, `FinanceRuleEngine`, Laenderlogik, Audit-CSV-Quelle.
- **HR KPI** `HrKpi.razor` (+ `HrKpiDashboardBuilder`).
- Gemeinsames Muster: Hero-Panel (MudPaper) → Filter-Panel → KPI-Cards
  (`MudGrid`/`MudItem` mit `MudIcon`) → Sektionen (`MudTable`, Charts, Panels).

---

## 5b. Erweiterungs- & Korrektur-Playbook (fuer JEDE Aufgabe)

Aufgabentyp → wo anfassen. Immer Konventionen aus Abschnitt 3 einhalten
(Interface+Impl, Lifetime, `IDbContextFactory`, Page-Service-Pattern,
`T(de,en)`, `CancellationToken`). Fehlt Detaillogik → ueber Katalog 6c die
genaue Methode anfordern.

| Aufgabe | Anfassen / Vorgehen |
| --- | --- |
| **Neue Kennzahl/KPI oder Chart** in einem Dashboard | Feld in `…LiveState`/Result-Klasse ergaenzen → im zugehoerigen Scoped-Service (`…DashboardService`/`…PageService`) in `LoadAsync` berechnen → in der `.razor` als `MudPaper`/`MudIcon`-KPI-Card oder `MudTable`/Chart anzeigen. **Nie** im Razor rechnen. |
| **Neues Visual/Widget** (Gauge, Ampel, Tile…) | Wiederverwendbare Komponente unter `Components/Shared/`; Wert kommt aus Page-Service. Beispiel-Rezept = Abschnitt 6. |
| **Neue Seite / Unterbereich** | `.razor` mit `@page "/route"` in `Components/Pages/` → passenden Scoped Page-Service + Interface anlegen → in `Program.cs` `AddScoped` → Menue-Eintrag (`NavigationMenuItem`-Seed in `DatabaseSeedService`, sonst nur per URL). |
| **Menue/Navigation aendern** | `NavigationMenuItems` (DB) via `INavigationMenuService`; UI `Admin > Menuestruktur`. Icons via `NavigationIconResolver`. |
| **Neue Datenquelle anbinden** | `IDataSourceAdapter`-Impl in `Services/DataSources/` (Strategy je `ConnectionKind`) → in `DataSourceAdapterResolver` + `Program.cs` registrieren. SAP-OData: `ISapGatewayService`; HANA: `IHanaQueryService`; Excel: `IManualExcelImportService`. |
| **Feld-Mapping / Transformation** | Regel = `FieldTransformationRule`; Strategie = `ITransformationStrategy`-Impl (in `Program.cs` registrieren), Katalog in `TransformationCatalog`. Anwendung via `IRecordTransformationService.Apply`. |
| **Finance-Berechnung korrigieren** | `FinanceRuleEngine` + `IFinanceReconciliationService` (Soll/Ist); Laenderformeln siehe `docs/FINANCE_BERECHNUNGSFORMELN_LAENDER_2026-05-19.md`. Waehrung: `ICurrencyExchangeRateService.ResolveRate`. |
| **Export (Excel/CSV) anpassen** | `IExcelExportService` (ClosedXML), `IConsolidatedExportService`, Audit-CSV `IExportAuditCsvService`, Orchestrierung `ExportOrchestrationService` / `SiteExportService`. |
| **Einkauf-Datenstand/Refresh** | `IPurchasingDataRefreshService` (Full/Delta, roher SQLite-Cache EKKO/EKPO/EKET); Auswertung `IPurchasingDashboardService`. |
| **DB-Feld/Tabelle hinzufuegen** | EF-Entity in `Models/` + DbSet in `AppDbContext` → Schema in `DatabaseInitializationService(.SchemaSql).cs` (kein klassisches Migrations-Setup; Schema wird beim Start sichergestellt via `DatabaseSchemaMaintenanceService.EnsureSchema`) → Seed in `DatabaseSeedService`. |
| **Bug/Korrektur in bestehendem Verhalten** | Symptom → Dashboard/Service in Abschnitt 5/6c lokalisieren → genaue Methode ueber Katalog anfordern → Fix + Test im `TrafagSalesExporter.Tests`-Projekt (es gibt pro Service meist eine `…Tests.cs`). |
| **Uebersetzung/Text** | `T("de","en")` in der `.razor`; zentrale Texte via `IUiTextService`. Technische SAP-Feldnamen NICHT uebersetzen. |
| **Zugriff/Login/Security** | `Security/*Options.cs` + `SecurityPolicyFactory`; Unlock-Endpoints `/access/{finance,admin,hr}` in `Program.cs`; Sessions `IAccessSessionTracker`. |
| **Logging/Diagnose** | `IAppEventLogService.WriteAsync(...)`; Anzeige `Logs.razor` / `InteractiveDiagnostics.razor`. |
| **Hintergrund-/Timer-Job** | `TimerBackgroundService` (HostedService) + `ExportOrchestrationService` (Singleton, geteilter Status). |

Wenn die Aufgabe nicht in die Tabelle passt: Bereich grob zuordnen
(Einkauf/Finance/HR/Export/Import/Infra), dann im Katalog 6c den passenden
Service suchen und dessen Methode anfordern.

---

## 6. Beispiel-Rezept: datengetriebener Gauge-Controller (eines von vielen)

> Dies ist ein **konkretes Muster-Beispiel** fuer „neues Visual + Wert aus
> Page-Service“. Dieselbe Mechanik gilt fuer jedes andere Widget/KPI.

**Heutiger Stand:** Auf `ExportDashboard.razor` gibt es ein rein dekoratives
Manometer als statisches Inline-**SVG** mit CSS-Keyframe-Animation
(`@keyframes manometer-sweep`, Klassen `.manometer-*`). Die Nadel
(`.manometer-needle`, `transform-origin: 105px 98px`) sweept fest 0→100 — sie
ist **nicht datengebunden**. Es gibt noch keinen wiederverwendbaren Gauge.

**Ziel „neuer Gauge-Controller“:** eine wiederverwendbare, datengetriebene
Gauge-Komponente, deren Nadelwinkel aus einem berechneten Wert (z. B.
Spend-Auslastung, Termintreue, Datenqualitaet) kommt. Empfohlenes Vorgehen,
konsistent zu den Konventionen oben:

1. **Komponente** `Components/Shared/GaugeController.razor` (neuer Ordner
   `Shared` ist ok). Parameter:
   ```csharp
   [Parameter] public double Value { get; set; }        // Ist-Wert
   [Parameter] public double Min { get; set; } = 0;
   [Parameter] public double Max { get; set; } = 100;
   [Parameter] public string CaptionDe { get; set; } = "";
   [Parameter] public string CaptionEn { get; set; } = "";
   ```
   Nadelwinkel berechnen (Halbkreis 0..180°, Mapping wie das Manometer von
   −90°..+90°): `var pct = Math.Clamp((Value-Min)/(Max-Min), 0, 1);`
   `var angle = -90 + pct * 180;` und per Inline-`transform:rotate(@angle deg)`
   statt CSS-Animation auf `.manometer-needle` legen. SVG-Pfade/Ticks/Labels
   aus `ExportDashboard.razor` (Zeilen ~41–59) als Vorlage wiederverwenden; die
   `@keyframes`-Animation weglassen.
2. **Wert liefern** im passenden **Scoped Page-/Dashboard-Service** (nicht im
   Razor rechnen): z. B. neues Feld in `PurchasingDashboardLiveState` wie
   `public double SpendUtilizationPercent { get; set; }`, im
   `PurchasingDashboardService.LoadAsync` aus dem Einkauf-Cache berechnen.
3. **Einbinden** in `PurchasingDashboard.razor` (oder Finance) im Hero-/KPI-
   Bereich: `<GaugeController Value="@_liveState.SpendUtilizationPercent"
   CaptionDe="Spend-Auslastung" CaptionEn="Spend utilisation" />`.
4. **Texte** ueber `T(...)`, **Lifetime/Registrierung** unveraendert (Komponente
   braucht keine DI-Registrierung; ein neuer Service schon → in `Program.cs`
   als `AddScoped` eintragen).
5. **Test**: bei neuer Berechnungslogik einen Test im Tests-Projekt ergaenzen
   (Muster: `PurchasingDashboardServiceTests`-Stil), `dotnet test` gruen halten.

Damit ist der Gauge wiederverwendbar (Einkauf, Finance, HR) und respektiert die
Trennung UI ↔ Page-Service.

---

## 6b. Schluessel-Code (echt, eingedampft)

Diese Snippets sind echter Code aus dem Repo (gekuerzt), als Vorlage zum
Mitbauen. Der restliche Code ist **nicht** hier — siehe Katalog in Abschnitt 6c
und fordere gezielt einzelne Methoden/Dateien nach.

### 6b.1 Bestehender Manometer (statisch, `ExportDashboard.razor`)
Vorlage fuer den neuen Gauge. Nadel = `.manometer-needle`, Drehpunkt
`transform-origin: 105px 98px`. Heute fest per CSS-Keyframe animiert (nicht
datengebunden):
```razor
<svg class="manometer-svg" viewBox="0 0 210 118" role="img" aria-label="Export activity manometer">
  <path class="manometer-outer" d="M25 98 A80 80 0 0 1 185 98" />
  <path class="manometer-inner" d="M47 98 A58 58 0 0 1 163 98" />
  <!-- 5 Ticks + 5 Labels 0/25/50/75/100, hier gekuerzt -->
  <text class="manometer-caption" x="105" y="113">EXPORT</text>
  <g class="manometer-needle"><line class="needle-line" x1="105" y1="98" x2="105" y2="38" /></g>
  <circle class="manometer-hub" cx="105" cy="98" r="11" />
</svg>
```
```css
.manometer-needle { transform-box: view-box; transform-origin: 105px 98px;
  animation: manometer-sweep 5.8s infinite cubic-bezier(.45,0,.25,1); }
@keyframes manometer-sweep { 0%{transform:rotate(-52deg);} 11%{transform:rotate(18deg);} 100%{transform:rotate(...);} }
```
**Fuer den datengebundenen Gauge:** `animation` weglassen, stattdessen
`style="transform: rotate(@(_angle)deg)"` auf `.manometer-needle`, mit
`_angle = -90 + Math.Clamp((Value-Min)/(Max-Min),0,1) * 180`.

### 6b.2 Dashboard-Service-Muster (`PurchasingDashboardService.cs`, gekuerzt)
Zeigt: DbContext **nur** via Factory, Cache-First, `…LiveState` zurueckgeben,
roher SQLite-Zugriff fuer die Einkauf-Cache-Tabellen.
```csharp
public sealed class PurchasingDashboardService : IPurchasingDashboardService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    public PurchasingDashboardService(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<PurchasingDashboardLiveState> LoadAsync(
        PurchasingDashboardFilter? filter = null, CancellationToken ct = default)
    {
        var state = new PurchasingDashboardLiveState();
        filter ??= BuildDefaultFilter();            // 2020-01-01 .. heute
        state.PeriodFrom = filter.FromDate; state.PeriodTo = filter.ToDate;

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        if (await TryLoadCacheStateAsync(db, state, filter, ct)) return state; // Cache-First
        // Fallback: SAP-Quelle aus SourceSystemDefinitions/Sites lesen, sonst Message setzen
        return state;
    }
    // Helper bauen SQL dynamisch, z.B. Loekz/Mstae-Ausschluss:
    // ActiveItemFilterSql(filter, "p") -> "COALESCE(p.Loekz,'')='' AND COALESCE(p.Mstae,'')=''"
}
```
> Eigene Gauge-Kennzahl: neues `double`-Feld in `PurchasingDashboardLiveState`
> ergaenzen und in `LoadAsync` aus dem Cache berechnen (z. B. Auslastung =
> Spend_lfd_Jahr / Jahresbudget).

### 6b.3 DI-Registrierung (immer noetig fuer neue Services, `Program.cs`)
```csharp
builder.Services.AddScoped<IPurchasingDashboardService, PurchasingDashboardService>(); // Page/UI = Scoped
builder.Services.AddSingleton<ISapGatewayService, SapGatewayService>();                // stateless = Singleton
```

---

## 6c. RAG-Code-Katalog (Signaturen-Index — „ich brauche Methode xy“)

Der restliche Quellcode ist hier nur als **Signatur-Index** komprimiert. Wenn
du eine Implementierung brauchst, nenne **Service + Methode** (z. B. „brauche
`SiteExportService.ExportAsync`“) und fordere genau diese Datei/Methode an.

**Daten/SAP/Export**
- `IHanaQueryService`: `GetSalesRecordsAsync(server,schema,tsc,land,dateFilter,ct)`,
  `GetMappedSalesRecordsAsync(...sources,joins,mappings...)`,
  `GetAvailableSchemasAsync`/`…TablesAsync`/`GetTableFieldNamesAsync`,
  `TestConnectionDetailedAsync`.
- `ISapGatewayService`: `GetEntitySetsAsync`, `GetEntityFieldNamesAsync`,
  `GetEntityRowsAsync(serviceUrl,entitySet,user,pw,filter?,ct)`, `TestConnectionAsync`.
- `ISapCompositionService`: `BuildSalesRecordsAsync(...)` (Join SAP-Sources→SalesRecord).
- `IMappedSalesRecordComposer`: SalesRecord aus Feld-Mappings zusammensetzen.
- `IManualExcelImportService`: `ReadSalesRecordsAsync(filePath, site)`.
- `ISharePointUploadService`: `UploadAsync(...)`, `DownloadToTempFileAsync(...)`,
  `ResolveLatestFileInFolderAsync(...)`, `ResolveManualImportFilesInFolderAsync(...)`.
- `IExcelExportService`: `CreateExcelFile`, `CreateConsolidatedExcelFile`,
  `CreateDashboardProofExcelFile`, `CreateGenericExcelFile` (ClosedXML).
- `IExportAuditCsvService`: Audit-CSV `Sales_ProcessedMergeInput_<TSC>_<Datum>.csv`.
- `ISiteExportService`: `ExportAsync(site, updateStatus?, preferredImportYear?)`.
- `IConsolidatedExportService`: `ExportAsync(updateStatus?)` (zentrale Excel).
- `ExportOrchestrationService` (Singleton) + `TimerBackgroundService` (HostedService): geplante Laeufe.

**Transformation/Waehrung**
- `IRecordTransformationService`: `Apply(records, rules)`.
- `ITransformationStrategy` / `IRecordTransformationStrategy`: Strategie-Impls
  (Copy, Uppercase, Lowercase, Prefix, Suffix, Replace, Constant,
  NormalizeCurrencyCode, FirstNonEmpty, ConvertCurrency).
- `ITransformationCatalog`: `TransformationCatalogItem{Key,RuleScope,Description,TypeName,SourceFile,CodeSnippet}`.
- `ICurrencyExchangeRateService`: `ResolveRate(from,to,date?)`, `NormalizeCurrencyCode(code?)`.
- `IExchangeRateImportService`: `RefreshEcbRatesAsync(ct)` → `ExchangeRateImportResult`.

**Zentral/Finance/HR/Management**
- `ICentralSalesRecordService`: `ReplaceForSiteAsync(site,records,update?)`, `GetAllAsync()`.
- `ICentralSalesDataProvider`: zentrale Lese-Quelle fuer Auswertungen.
- `IFinanceReconciliationService`: Soll/Ist-Abgleich (FinanceRuleEngine).
- `FinanceRuleEngine` (Klasse): Regelauswertung Finance/Excel.
- `IManagementCockpitService`: `GetAvailableFilesAsync`, `AnalyzeAsync(filePath,options?)`,
  `AnalyzeCentralAsync(year,month?,options?)`, `AnalyzeFinanceSummaryAsync(year,countryKey?,currency?)`.
- `IHrKpiService` + `HrKpiDashboardBuilder`: HR-KPI-Auswertung (Rexx-Quelle).
- `IPurchasingDashboardService` / `IPurchasingDataRefreshService` /
  `IPurchasingDataSourcePageService`: siehe Abschnitt 5 + IPurchasingDashboardService.cs.

**Infrastruktur/Navigation/Logs/DB**
- `INavigationMenuService`: `GetItemsAsync`, `SaveItemsAsync(items)`, `ResetToDefaultsAsync`.
- `IAppEventLogService`: `WriteAsync(category,message,level,siteId?,land?,details?)`, `WriteDebugAsync(...)`.
- `IExportLogService`: `WriteAsync(ExportLog)`.
- `IUiTextService`: Liefert `T(de,en)`-Texte/Sprachzustand.
- `IConfigTransferService`: `ExportJsonAsync(includeSecrets)`, `ImportJsonAsync(json)`.
- `IDatabaseInitializationService` / `…SchemaMaintenanceService(EnsureSchema)` /
  `…SeedService(SeedDefaults)`: DB-Anlage/Seed (Schema-SQL inkl. Purchasing-Cache).
- Zugriff/Access: `IAccessSessionTracker`, `ILandingPageSettingsService`,
  `IHrKpiAccessService`, `IFinanceCockpitAccessService`, `IAdminAccessService`.

**Page-Services (Scoped, je `.razor`)** — Interface liegt jeweils in derselben
Impl-Datei, Muster `LoadAsync()` → State, `SaveAsync(state)`:
`SettingsPageService`, `StandortePageService`, `StandorteSapEditorService`,
`DashboardPageService`, `LogsPageService`, `TransformationsPageService`,
`FinanceRulesPageService`, `ManagementCockpitPageService`,
`PurchasingDataSourcePageService`.

**Models (EF-Entities + UI-Records)** — Felder bei Bedarf anfordern:
`CentralSalesRecord` (zentrale Fakten), `SalesRecord`, `Site`, `HanaServer`,
`SourceSystemDefinition`, `SapSourceDefinition`/`SapJoinDefinition`/`SapFieldMapping`,
`ManualExcelColumnMapping`, `CurrencyExchangeRate`, `FinanceReference`,
`FinanceRule`/`FinanceIntercompanyRule`, `ExportSettings`/`ExportLog`,
`AppEventLog`, `FieldTransformationRule`, `NavigationMenuItem`, `SharePointConfig`,
`PurchasingAnalysisRow`/`PurchasingSectionModels`, `ManagementCockpitModels`,
`HrKpiModels`, `ConfigTransferPackage`.

---

## 7. Build / Test / Deploy

- Build/Run: Standard `dotnet build` / `dotnet run` im Ordner
  `TrafagSalesExporter`. SAP-HANA-DLL-Warnung ist ok, wenn kein HANA-Client
  installiert ist (nur HANA-Quellen betroffen).
- Tests: `dotnet test TrafagSalesExporter.sln --verbosity minimal`.
- Deploy: IIS-Publish als `BiDashboard`
  (`\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`); Details siehe
  `docs/rag/DEPLOYMENT.md`. Vor Prod-Refresh muss die SAP-Service-URL
  (`ZPOWERBI_EINKAUF_SRV`, Produktsparten-Service) gesetzt/verifiziert sein.

---

## 8. Wo mehr steht (nur bei Bedarf nachfordern)

Wenn die LLM Detail braucht, das hier fehlt, gezielt **eine** dieser Dateien
anfordern (Token sparen, nicht alles laden):

| Thema | Datei |
| --- | --- |
| Aktueller Projektstand/Changelog | `docs/rag/PROJECT.md` |
| Architektur-Kurz | `docs/rag/ARCHITECTURE.md` |
| Finance-Regeln/Soll-Ist | `docs/rag/FINANCE.md` |
| HR KPI | `docs/rag/HR_KPI.md` |
| Manual Import (UK/ES/DE) | `docs/rag/MANUAL_IMPORT.md` |
| Produktsparten-Mapping | `docs/rag/PRODUCT_MAPPING.md` |
| Deployment/IIS | `docs/rag/DEPLOYMENT.md` |
| Einkauf-Detail | `docs/PURCHASING_DASHBOARD_2026-06-05.md` |
| RAG-Einstieg/Router | `docs/RAG_ROUTER.md`, `LLM_SYSTEM_GUIDE.md` |

**Faustregel fuer die arbeitende LLM:** UI-Logik in `.razor`, Daten-/
Rechenlogik in einem Scoped-Service, DbContext nur via Factory, neue Services in
`Program.cs` registrieren, Texte zweisprachig via `T(...)`, Tests gruen halten.
