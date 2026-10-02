# RAG Architecture

Stand: 2026-09-29 (Hell/Dunkel im Kurzstand ergaenzt; Abschnitt „Code-Architektur" vom
2026-09-28; der Kurzstand darunter stammt sonst vom 2026-05-27 und ist fachlich weiter gueltig,
beschreibt aber nicht die seither dazugekommenen Bereiche Einkauf, Journal, Marktsegmente,
Serveranalyse und HR)

## Code-Architektur: Befund vom 2026-09-28

Anlass war Ingos Frage, ob der Code sauber nach Clean Architecture umgesetzt, alles
entkoppelt und abstrahiert sei und ob die Service-Klasse der Controller im Sinne von MVC
sei. Die Zahlen sind am 2026-09-28 im Arbeitsbaum gemessen (Branch
`feature/supplier-overrides-trit`, Commit `65dde74`); sie veralten mit jeder Aenderung.

### Welches Muster es ist

Die Anwendung ist ein **Blazor-Server-Monolith in einem einzigen Projekt** mit den
Ordnern `Components/`, `Services/`, `Models/` und `Data/`, dazu das Testprojekt
`TrafagSalesExporter.Tests`. Es gibt keine Controller. Einzige HTTP-Endpunkte sind drei
`MapPost`-Aufrufe fuer die Zugangspruefung in `Program.cs` (`/access/finance`,
`/access/admin`, `/access/hr`).

Uebertragen auf MVC:

| MVC-Rolle | In dieser App |
|---|---|
| View | Markup der `.razor`-Komponente |
| Controller | `@code`-Block derselben `.razor`-Komponente (Ereignisse, Zustand, Aufrufe) |
| Model und Logik | `Services/*Service.cs` samt EF-Entities in `Models/` |

**Die Service-Klasse ist also nicht der Controller**, sondern Business-Logik und
Datenzugriff in einem. Die Controller-Rolle traegt der `@code`-Teil der Komponente. Das
ist fuer Blazor ueblich und fuer sich kein Mangel.

### Was traegt

- 31 Service-Interfaces unter `Services/I*.cs`; die Seiten injizieren ueberwiegend
  Interfaces.
- Eigene Page-Services als Presenter-Ersatz, etwa `StandortePageService`,
  `SettingsPageService`, `TransformationsPageService`, `PurchasingDataSourcePageService`.
- Nur eine Seite greift direkt auf die Datenbank zu: `Components/Pages/ManualImports.razor`
  injiziert `IDbContextFactory<AppDbContext>`.
- Datenzugriff durchgaengig ueber `IDbContextFactory`, dadurch gut testbar; 72 Testdateien.
- Die Quellsystem-Leser (SAP Gateway, HANA/B1) sind abstrahiert, zum Beispiel
  `ISapGatewayStockValueReader`.

### Was gegen Clean Architecture spricht

1. **Keine Schichtung nach Abhaengigkeitsrichtung.** Domain, Application und
   Infrastructure sind keine eigenen Projekte. Die EF-Entities in `Models/` sind zugleich
   das Domaenenmodell, und die Logik haengt direkt an EF Core und SQLite.
2. **Keine Repository-Schicht.** Rund 48 Services oeffnen den `AppDbContext` selbst und
   mischen LINQ-Abfragen mit Berechnung. Getrennt ist der Zugriff nur in drei
   `*Store`-Klassen: `PurchasingStockValueStore`, `SupplierMaterialOverrideStore`,
   `ForeignProcurementEvidenceStore`.
3. **Sehr grosse Klassen (Single Responsibility verletzt).**

   | Datei | Zeilen | davon `@code` |
   |---|---:|---:|
   | `Components/Pages/ManagementCockpit.razor` | 3'438 | 1'615 |
   | `Services/ManagementCockpitService.cs` | 3'022 | |
   | `Components/Pages/PurchasingDashboard.razor` | 2'875 | 2'053 |
   | `Services/PurchasingDashboardService.cs` | 2'122 | |
   | `Services/ExcelExportService.cs` | 1'750 | |
   | `Services/DatabaseSeedService.cs` | 1'733 | |
   | `Services/HrKpi/HrKpiDashboardBuilder.cs` | 1'671 | |

   `UiTextGeneratedTranslations.cs` (9'328 Zeilen) ist generierter Text und zaehlt nicht.
   Zehn Komponenten rechnen selbst mit `Where`/`GroupBy`, darunter `ManagementCockpit`,
   `PurchasingDashboard`, `FinanceComparison`, `MarketSegments` und `HrKpiDashboardTabs`.
4. **Aufgeweichte Abstraktion an einzelnen Stellen.**
   - `ManagementCockpitService` erzeugt im Konstruktor `new CurrencyExchangeRateService(dbFactory)`
     selbst, statt ihn injizieren zu lassen.
   - `PurchasingDashboardService` und `ExcelExportService` haben optionale bzw. mehrere
     Konstruktoren (`= null`, parameterloser Konstruktor mit nullbarem `_dbFactory`). Das
     verdeckt Abhaengigkeiten und laesst Pfade ohne Datenbank zu.
   - Konkrete Klassen ohne Interface werden direkt injiziert: `TimerBackgroundService` und
     `MarketSegmentPatternService` in Seiten, `ExportOrchestrationService`,
     `PurchasingRefreshRunner` und `PurchasingDashboardSnapshotCache` als Singletons.

### Einordnung und Empfehlung

Fuer eine interne Reporting-App mit einem Entwickler ist das ein **pragmatisch
geschichteter Monolith mit guter DI-Disziplin**. Die Wartungskosten entstehen nicht durch
fehlende Projektgrenzen, sondern durch die vier bis fuenf sehr grossen Klassen und Seiten.

Ein Umbau auf echte Clean Architecture mit getrennten Projekten waere gross und braechte
fachlich wenig. Mehr bringt, in dieser Reihenfolge:

1. Die `@code`-Logik aus `ManagementCockpit.razor` und `PurchasingDashboard.razor` in
   Page-Services verschieben, nach dem Vorbild von `StandortePageService`.
2. `ManagementCockpitService` und `PurchasingDashboardService` nach Tabs bzw.
   Kennzahlengruppen in kleinere Dienste aufteilen.
3. Versteckte Abhaengigkeiten aufloesen: `new CurrencyExchangeRateService` durch Injektion
   ersetzen, optionale Konstruktorparameter und Zusatzkonstruktoren entfernen.
4. Erst danach und nur wo es hilft: Lesezugriffe grosser Auswertungen in Stores buendeln.

Beruehrungspunkt: Das Tempo von `/einkauf` (`docs/PLATTFORM_TEMPO_2026-09-28.md`) haengt an
`PurchasingDashboardService`. Aufteilen und Beschleunigen dort nicht getrennt planen und
vorher in `docs/AGENT_COORDINATION.md` abstimmen.

## Kurzstand (2026-05-27)

- App sammelt Daten aus SAP OData, HANA/SAP B1, SharePoint und manuellen Excel-/CSV-Quellen.
- Zentrale Persistenz ueber `CentralSalesRecords`.
- Finance-Auswertung und zentrale Excel sollen dieselbe Regelengine verwenden.
- Produktsparten-Mapping ist als eigene Mapping-Schicht vorgesehen, nicht als versteckte Finance-Regel.
- Produktsparten-Referenz soll ueber SAP/ABAP bzw. Gateway als flache Tabelle geliefert werden.
- Diagramme und Anwenderdokus existieren fuer Keyuser-Prozess und technische Architektur.
- PERF-MUSTER 2026-07-23: `ManagementCockpitService` ist Singleton (Program.cs). Sein
  `LoadCentralRecordsAsync()` (kompletter `CentralSalesRecords`-Read, aktuell 84k Zeilen, waechst
  taeglich) wurde pro Cockpit-Seitenaufruf 2-4x redundant neu geladen (Init + je Tab). Fix: 10s-TTL-
  Cache um genau diesen Ladepunkt, NUR sicher weil alle Aufrufer die Liste rein lesend behandeln
  (Select/GroupBy/Where in neue Objekte, keine In-Place-Mutation der geteilten `SalesRecord`-
  Elemente) - bei einem Singleton-Cache mit mehreren gleichzeitigen Nutzern IMMER zuerst pruefen,
  ob Aufrufer schreibend auf die zurueckgegebene Liste zugreifen, bevor man cached. Details/
  Messwerte: `lastchange.md` Eintrag "PERFORMANCE-BEFUND COCKPIT 2026-07-23".
- HELL/DUNKEL seit 2026-09-29 (`5f4eb5f`, produktiv 13:51): Standard ist dunkel (Wunsch Ingo),
  Umschalter in der Kopfleiste, Wahl je Browser in `localStorage` (`trafag-theme`). Gesteuert in
  `Components/Layout/MainLayout.razor` (`PaletteDark`, `IsDarkMode`) und `wwwroot/js/theme.js`;
  `App.razor` setzt `data-theme` schon im `<head>`, damit nichts weiss aufblitzt. **Regel fuer neue
  Seitenstile:** Flaechen und Text ueber MudBlazor-Variablen (`--mud-palette-surface`,
  `--mud-palette-text-secondary`, `--mud-palette-lines-default`) oder halbtransparente Toene, keine
  festen hellen Hex-Werte. **Falle:** `Color="Color.Primary"` auf grossen Flaechen (z. B. `MudAppBar`)
  nimmt im Dunkelmodus das helle Schaltflaechen-Rot `#EF5350`; die Kopfleiste hat deshalb kein
  `Color` und nutzt `AppbarBackground` (`a397440`). Bewusst hell bleiben die 3D-Flaechen (im Dunkelmodus abgedunkelt) und der
  weisse Grund hinter den Schulungsbildern.
- NETZWERK seit 2026-10-02 (`1635b46`): Hintergrunddienst `NetworkProbeService` (nur Produktion, alle 5 Min., Schalter `NetworkProbe:Enabled`), `CircuitHandler` `ClientConnectionTracker` (nur /24), Tabellen `NetworkProbeResults`, `NetworkWatchTargets`, `NetworkClientEvents`. **Regel:** nie mit Anmeldedaten pruefen, nur feste Zielliste, keine Scans (`docs/NETZWERK_2026-10-02.md`).
- SEIT 2026-10-01 14:26 (`92e012e`, `102c20c`): Logo und „Dashboard“ direkt auf dem Orange (kein heller Kasten mehr); **Dimmer** links der Statuslampe setzt `--mud-palette-background/-surface/-drawer-background` am `<html>` (`trafagTheme.applyDim`, `localStorage` `trafag-dim`, 50 = bisher); **Statuslampe** mit zwei Lampen, die alle 800 ms wechseln; **Berndeutsch** (`gsw`, Anzeige BE) mit eigenem Katalog `UiTextBerneseTranslations` ueber alle 1'796 Schluessel, Rueckfall Deutsch, Glossar `docs/UI_BERNDEUTSCH_GLOSSAR.md`; neue Texte dort mitpflegen (Test `Bernese_Covers_Every_Catalogue_Key_And_Keeps_Placeholders`).
- SKIN seit 2026-10-01 (`7d1e5b5`, produktiv 14:14, Kasten seit 14:26 ueberholt): **Trafag CI** ist Standard, Leiste Trafag-Orange `#C8501E` (dunkel `#B5481B`, Schaltflaechen dunkel `#F08A4B`), links ein hellblauer Kasten mit dem schwarzen Logo `trafag.jpg` und „Cockpit“ gleich hoch (`.ci-brand` in `app.css`, `mix-blend-mode: multiply` gegen den weissen JPG-Grund). Das bisherige Rot heisst „klassisch“ und ist ueber den Paletten-Knopf in der Kopfleiste waehlbar; Wahl je Browser in `localStorage` (`trafag-skin`), `data-skin` am `<html>` schon im `<head>` gesetzt. Optik vor dem Commit als HTML-Nachbau mit Edge headless gegengeprueft, nicht in der laufenden App.

## Rohquellen Nur Bei Bedarf

- Diagramme: `docs/PROGRAMM_DIAGRAMME.md`
- Produktmapping: `docs/rag/PRODUCT_MAPPING.md`
- technischer Handoff und alter LLM-Systemkontext: `docs/raw_md_archive/HISTORY_CANONICAL.md.raw`
