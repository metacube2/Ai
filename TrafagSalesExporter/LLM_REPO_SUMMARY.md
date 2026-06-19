# TrafagSalesExporter / BiDashboard — Kompakt-Kontext fuer LLM

Stand: 2026-06-19. Zweck: Diese **eine** Datei reicht aus, damit eine LLM neue
Features (z. B. einen datengetriebenen Gauge-Controller im Einkaufs- oder
Finance-Dashboard) bauen kann, ohne das ganze Repo zu kennen. Keine
Geheimnisse, keine Rohdaten, keine Redundanz — nur Architektur, Konventionen
und konkrete Erweiterungs-Rezepte.

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

## 6. Der bestehende „Manometer“-Gauge & Rezept fuer einen Gauge-Controller

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
