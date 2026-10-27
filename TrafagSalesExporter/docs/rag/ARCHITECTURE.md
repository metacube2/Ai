# RAG Architecture

Stand: 2026-10-27 (neuer Abschnitt „Gesamtlandkarte“ mit allen Bereichen, Landkarte `docs/LOESUNGSLANDKARTE_2026-10-27.html`); davor 2026-10-22 nur Archivverweis; fachlich 2026-10-19 (Operations-Zeile: Mengen auf dem Produktionstag, DW prod; 2026-10-16 Operations-Zeile: SAP-Mengen, offen fuer alle; Menue-Hervorhebung, Handbuch-Bilder, Stuecklisten-Gehirn; 2026-10-15 Startseite, CPU-Anzeige, Nutzungsmessung; 2026-10-14 Operations-Zeile: Feld-Mapping; 2026-10-06 Benutzerhandbuch, Icon-Pruefung, Pruefbefund-Fixes ergaenzt; Abschnitt „Code-Architektur" vom
2026-09-28; der Kurzstand darunter stammt sonst vom 2026-05-27 und ist fachlich weiter gueltig.
*Ueberholt am 2026-10-27:* Der fruehere Hinweis, die Datei beschreibe Einkauf, Journal,
Marktsegmente, Serveranalyse und HR nicht, gilt nicht mehr; diese Bereiche stehen jetzt im
Abschnitt „Gesamtlandkarte“ direkt unter diesem Kopf)

## Gesamtlandkarte: alle gebauten Loesungen (Stand 2026-10-27)

Zusammengestellt am 2026-10-27 aus `router.md`, den Unterroutern und dem Kurzstand unten, nicht
gegen den Code gemessen. Die interaktive Fassung mit Linien je Zusammenhang liegt in
`docs/LOESUNGSLANDKARTE_2026-10-27.html` (im Browser oeffnen, eine Kachel anklicken). Details stehen
jeweils in der Datei, die der Unterrouter nennt.

Fluss von links nach rechts: **Quelle -> Anbindung -> Daten und Regeln -> Modul -> Nutzung.**

| Bereich | Quelle | Anbindung | Daten und Regeln | Modul im Dashboard | Nutzung |
| --- | --- | --- | --- | --- | --- |
| Finance | SAP P76, SAP B1/HANA (CH/AT, B1-Laender), Sage ES, Alphaplan DE, Excel UK/IT/IN | B1-Leser, Exportpakete ES/DE (eigene SQL), Manual Import, Export-Orchestrierung (`TimerBackgroundService`) | `CentralSalesRecords`, `FinanceRuleEngine` mit Intercompany, Kurstabelle, Standardkosten und Supplier, Produktsparten-Mapping | Finance Cockpit (Management Analyse, Soll/Ist, Gruppenmarge, Finance Details), Controlling, Marktsegmente und Railway, Verkauf (mit Interaktiv) | Finance (Andreas), Geschaeftsleitung; Excel `Sales_All`, Nachweis-Excel |
| Journal | SAP P76 (CH/AT) | Journal-Set (Transport mit `HrKpiSet`) | Hauptbuchzeilen, Faelligkeitsdatum vorbereitet, Konzernkonto-Mapping offen | — (Excel) | `Finance_All` fuer die Konsolidierung; Automatik unkommittiert im Arbeitsbaum |
| Weltlage | GDELT, FRED, Eurostat, IMF, EZB | `WorldDataService` (nur Produktion) | Tabellen `World*`, verknuepft mit Verkauf, Einkaufscache und Kursen | Weltlage unter Finance Cockpit | Geschaeftsleitung, Abteilungen |
| Einkauf | SAP P76 | `ZPOWERBI_EINKAUF_SRV` (EKPO, `EinkKontraktSet`, `EinkMatLzSet`, Produktgruppen ZDISPO) | Einkaufscache (Delta 45 bis 55 Min.), Lagerwert MBEW/MBEWH, Stuecklistenanalyse aus dem ZLO03-Webservice | Einkauf (Spend-Aufriss ungedeckelt, Kontrakte, Lagerwert, Produktgruppen ABC/XYZ), Einkauf Interaktiv, Supply Chain, Verwendung und Risiko mit Stuecklisten-Gehirn | Einkauf und Disposition; Power BI liest denselben Service direkt |
| Logistik live | SAP P76 | `LogTaSet`, `LogLiefSet`, `LogRueckSet` (nur bei offener Seite) | keine Speicherung, kurzer Merker | Logistik live mit 3D-Lagerplaetzen | Lager und Kommissionierung |
| Handbestellwesen | Active Directory, Altbestand aus der PHP-Anwendung | AD-Kontopruefung im Server | Stamm Mitarbeitende mit `AusAd` (AD gesperrt, Altbestand pflegbar), Freigabe umschaltbar | `Modules/Handbestellwesen/` (ersetzt die PHP-Anwendung) | Besteller und Freigeber |
| HR | SAP P76 | `HrKpiSet`, `HrAbsenzSet` (Tagesabruf) | HR-Datenordner, `HrKpiDashboardBuilder` | HR-KPI-Cockpit (eigener Zugang) | HR |
| Operations | SAP P76, Fabios Konfigurationspakete | `ShopZd05Set`, `ShopAufSet`, `ShopRmSet`, `ShopProdSet`; CDS-Services Liefertreue | eigene `shopfloor.db` (Tagessaetze, `kpi_snapshot`) | Operations Shopfloor, Operations KPI (OTD/OTIF/OTP) | Produktion (Fabio) |
| Plattform | Active Directory, feste Netzwerkzielliste | `NetworkProbeService`, `AdSnapshotService` | Tabellen `Network*`, `Pm*`, `Forum*`, `Usage*` | Netzwerk, Trafag Projekte, Trafag Reddit, Benutzerhandbuch, Admin und Startseite | IT, alle Mitarbeitenden |
| Serveranalyse | Systeme, die nur der Server erreicht | Abfragen ueber den Server | — | — | Abschnitt Serveranalyse in `docs/router/plattform.md` |

**SAP-Eigenentwicklungen (T76 -> P76):** ZLO03 (`Z_ZLO03_TURBO2`, Webservice `ZM_LZCODE20_OPT`,
speist die Stuecklistenanalyse; FIX 19 holt Compliance-Spalten aus PPWR `Z_PACK_REGULATION`), ZC23
Materialanteil und Arbeit CZ mit Fiori-App, `ZM_OFFENE_KDAUF_OPT` (schreibt `ZPOWERBI_KDAUF` fuer
Power BI), `ZM_OFFENE_FAUF` Bedarfsverursacher, ZZPRDAT, Konditionsart ZRL2, Produktsparten- und
Produktgruppen-Provider sowie die Gateway-Klassen (MPC/DPC) aller Sets oben. Einstieg `docs/router/sap.md`.

**Werkzeuge:** DeployHeadless mit Monitor (`docs/rag/DEPLOYMENT.md`), SapProbe und GUI-Skripte
(`saptasks/SAP_ARBEITSWEISE_UND_WERKZEUGE.md`), Qdrant-Dokusuche (`Tools/Qdrant/`),
Wochen_Todo-Generator, `Tools/HandbuchDocx` und `Tools/HandbuchGrafik`.

Abgrenzung zur Themenlandkarte vom 2026-10-15 (`docs/THEMENLANDKARTE_2026-10-15.svg`): jene zeigt
Themen, Ansprechpartner und Blocker, diese hier den technischen Datenfluss der gebauten Loesungen.

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
- WELTLAGE (2026-10-05): `WorldDataService` (Singleton und Hintergrunddienst, nur Produktion, HttpClient "Weltlage") holt GDELT, FRED, Eurostat, IMF in eigene Tabellen `World*`; `WorldImpactService` verknuepft mit `SalesDataService`, Einkaufscache und Kurstabelle; reine Rechnung `WorldImpactAnalytics`; Seiten `/weltlage*` offen fuer alle; nicht erreichbare Quelle = "Firewall blockiert" (`docs/WELTLAGE_2026-10-05.md`).
- CONTROLLING (`cb08f18`): `ControllingAnalytics` (Umsatzbruecke, Hochrechnung) auf den Verkaufszeilen von `SalesDataService` (jetzt mit Lokalwert und Waehrung); Seite `/finance-cockpit/controlling` ohne Passwort; offene Fragen nur in `docs/CONTROLLING_2026-10-05.md` und auf der Seite, nicht im Issue-Log (Eigeninitiative).
- OPERATIONS (SHOPFLOOR) (2026-10-07/08, produktiv 2026-10-08): PPA-Shopfloor als statische JS-Oberflaeche `wwwroot/shopfloor` im iframe der Seite `/operations`, Backend in C# (`Services/Shopfloor`, eigene SQLite `shopfloor/shopfloor.db`, Endpunkte `/shopfloor/api/*`). Zugriff ueber Middleware `UseShopfloorAccess` (vor `UseStaticFiles`): Windows-Konto in `Shopfloor:AllowedUsers` oder Login `operations` (Hash, Data-Protection-Cookie). Statische Dateien mit `Cache-Control: no-cache`. SAP-Abgleich `ShopfloorSapSync` (ShopZd05Set, ShopAufSet) seit 2026-10-09 eingeschaltet. Feld-Mapping und Abteilung je Disponent seit 2026-10-14 aus `docs/Shopfloor_SAP_Feldmapping_2026-10-14.xlsx` (`shopfloor_config.json`: `sap` je Feld, `forecast_prefix`). Seit 2026-10-14 ist `operations` eine Menuegruppe mit KPI (`/operations/kpi`, `/operations/kpi/liefertreue`: `KpiLiefertreue`, `KpiLiefertreueSync`, Tabellen `kpi_snapshot`/`kpi_exclusion` in `shopfloor.db`, Seiten unter `wwwroot/shopfloor/kpi`, Doku `docs/OPERATIONS_KPI_2026-10-14.md`) und Shopfloor (`/operations`). Doku `docs/SHOPFLOOR_2026-10-07.md`.
  *Nachtrag 2026-10-16:* `ShopfloorSapMengen` im `ShopfloorSapSync` (Schalter `Shopfloor:SapMengenEnabled`, an): `ShopRmSet` (ZC27-Logik) und
  `ShopProdSet` (Istendtermin) -> logistik.*, tr5.prod, seh.prod, seh.fpy; Vorschau `GET shopfloor/api/sap/mengen?tag=`. Leerer Tagessatz
  wird geloescht. `ShopfloorConfig` uebernimmt `planned_source` aus Fabios Paketen nach `src`. `Shopfloor:OpenForAll = true`
  (Admin-Schalter `operations.login_required` hat Vorrang), `SapSyncTimes` 04:00/12:45. Forecast-Abteilung DS (Dichtesensoren).
  *Nachtrag 2026-10-19:* Mengen auf dem **Produktionstag** (ERSDA/Istendtermin), Abgleich schreibt heute + 3 Tage zurueck (`SyncDays`), Bereich ohne SAP-Zeile bleibt leer; neu `dw.prod` (DL790). POST auf `shopfloor/api` braucht Header `X-User`.
- STARTSEITE UND KOPFLEISTE (2026-10-14): Standortgrafik `Components/Home/SiteNetworkGraphic.razor` (Logik `SiteNetwork.cs`, Zustand je Land aus `Sites` + juengstem `ExportLogs`-Eintrag, CSS `sng-`), ersetzt `mainimage.png`; Manometer ueber `ExportSettings.ShowLandingManometer` (Admin-Schalter, DB statt appsettings). CPU-Pegelanzeige `Components/ServerLoadMeter.razor` mit statischem `Services/ServerLoadSampler.cs` (GetSystemTimes, Prozesszeit, ein Takt fuer alle). Anonyme Nutzungsmessung statisch `Services/PageUsageTracker.cs` (Hook `MainLayout.LocationChanged`, Tabellen `UsagePageViews`/`UsagePageUsers` legt der Dienst selbst an, Tages-HMAC ohne Namen), Auswertung `/admin/nutzung`. Alle drei bewusst ohne Registrierung in `Program.cs` (dort liegen fremde Finance_All-Aenderungen).
- TRAFAG PROJEKTE (2026-10-09): ersetzt die Poor Man's Project Management Suite. `/projekte` und `/projekte/{KEY}` (`Components/Pages/Projects.razor`, `ProjectWorkspace.razor`, `Components/Projects/*`, `Services/Projects/*`), Tabellen `Pm*` ueber Schema-SQL, Live-Hinweis ueber Singleton `PmNotifier`, Rechte im Service (alle sehen, Mitglieder bearbeiten, Leitung verwaltet). Doku `docs/TRAFAG_PROJEKTE_2026-10-09.md`.
- TRAFAG REDDIT (2026-10-09): Forum `/forum` (`Components/Pages/Forum.razor`, `Components/Forum/*`, `Services/Forum/*`), Tabellen `Forum*` in der App-SQLite ueber Schema-SQL, Autor = Windows-Login mit AD-`displayName` (`ForumUserDirectory`), Live-Hinweis ueber Singleton `ForumNotifier`, Markdown nur als kodierter Teilumfang. Fuer alle sichtbar. Doku `docs/TRAFAG_REDDIT_2026-10-09.md`.
- BENUTZERHANDBUCH (2026-10-06, `fbe30b9`, produktiv 10:16): Reiter `/handbuch` auf oberster Ebene (`user-manual`, Sortierung 5), Seite `UserManual.razor`, Parser `UserManualContent` liest `wwwroot/handbuch/kapitel/*.md`; dieselben Dateien baut `Tools/HandbuchDocx` zur Word-Datei (Trafag-CI). Doku `docs/handbuch/README.md`.
  *Nachtrag 2026-10-16:* Bildzeile `![Unterschrift](bilder/datei.svg)` in Parser, Reiter (`.um-figure`) und `Tools/HandbuchDocx` (gleichnamige PNG); Prozessgrafik aus `Tools/HandbuchGrafik`.
- MENUE-ICONS (2026-10-06, `d7270a5`): jedes Seed-Icon muss in `NavigationIconResolver` stehen, sonst grauer Kreis; Test `SeedDefaults_JedesMenueIcon_Ist_Aufgeloest`.
- PRUEFBEFUND-FIXES (2026-10-05, `3ad0dae`, produktiv 2026-10-06 10:16): Elikz-Filter an allen Stellen, OrdersOnly auch in Einkauf Interaktiv und Weltlage, Fehlerergebnisse nur 1 Minute gemerkt (Logistik, AD, Einkauf Interaktiv), Referenzmonat nach letztem Werktag.
- EINKAUF INTERAKTIV (2026-10-05): `PurchasingInteractiveService` bildet Bestellpositionen als `SalesFact` ab, Seite `PurchasingInteractivePage` (`/einkauf/interaktiv/{View}`) nutzt die Verkauf-Ansichten mit `Purchasing=true`.
- VERKAUF INTERAKTIV (2026-10-05): `SalesInteractive` (reine Rechnung), `Components/Sales/Interactive/*View.razor`, Seite `SalesInteractivePage` (`/verkauf/interaktiv/{View}`), Animation ueber CSS-Uebergaenge und PeriodicTimer.
- VERKAUF (`e2fb8ce`): `SalesDataService` (Singleton, gemerkt je Quellstand) baut aus `ICentralSalesDataProvider` mit `FinanceRuleEngine` und Intercompany-Regeln Verkaufszeilen in CHF; reine Logik `SalesAnalytics`; Seiten `/verkauf*` hinter der Finance-Freischaltung (`Routes.razor`). Keine Marge, keine Verkäuferauswertung (`docs/VERKAUF_2026-10-02.md`).
- NETZWERK MIGRATION/BERICHT (`7d31df5`, produktiv 2026-10-02 12:00): Seiten `NetworkMigration.razor`, `NetworkReport.razor` (PDF nur ueber Browser-Druck, kein Versand), Geraetelandkarte und Altlasten-Score; Logik `AdInfraAnalysis.MigrationWaves`, `LegacyScores`, `DevicesPerSite`.
- NETZWERK AD-INFRASTRUKTUR (`50b6c4a`): `AdInfrastructureService`, `AdGpoService`, `AdDnsZoneService` (je gemerkt), reine Logik `AdInfraAnalysis`; `AdSnapshotService` taeglich aus `NetworkProbeService`, Tabellen `NetworkAdMetrics`, `NetworkAdChanges`, `NetworkAdComputers`. CSS-Praefix `dir-`, nie `ad-`.
- NETZWERK AD (`b57d20f`, produktiv 2026-10-02 08:07): `AdComputerService` liest Computer, OUs, AD-Subnetze, LAPS-Ablaufdatum und BitLocker-Anzahl, nie Geheimnisse; reine Logik in `AdAnalysis`; DCs als Pruefziele im `NetworkTargetCatalog`.
- NETZWERK seit 2026-10-02 (`1635b46`): Hintergrunddienst `NetworkProbeService` (nur Produktion, alle 5 Min., Schalter `NetworkProbe:Enabled`), `CircuitHandler` `ClientConnectionTracker` (nur /24), Tabellen `NetworkProbeResults`, `NetworkWatchTargets`, `NetworkClientEvents`. **Regel:** nie mit Anmeldedaten pruefen, nur feste Zielliste, keine Scans (`docs/NETZWERK_2026-10-02.md`).
- SEIT 2026-10-01 14:26 (`92e012e`, `102c20c`): Logo und „Dashboard“ direkt auf dem Orange (kein heller Kasten mehr); **Dimmer** links der Statuslampe setzt `--mud-palette-background/-surface/-drawer-background` am `<html>` (`trafagTheme.applyDim`, `localStorage` `trafag-dim`, 50 = bisher); **Statuslampe** mit zwei Lampen, die alle 800 ms wechseln; **Berndeutsch** (`gsw`, Anzeige BE) mit eigenem Katalog `UiTextBerneseTranslations` ueber alle 1'796 Schluessel, Rueckfall Deutsch, Glossar `docs/UI_BERNDEUTSCH_GLOSSAR.md`; neue Texte dort mitpflegen (Test `Bernese_Covers_Every_Catalogue_Key_And_Keeps_Placeholders`).
- SKIN seit 2026-10-01 (`7d1e5b5`, produktiv 14:14, Kasten seit 14:26 ueberholt): **Trafag CI** ist Standard, Leiste Trafag-Orange `#C8501E` (dunkel `#B5481B`, Schaltflaechen dunkel `#F08A4B`), links ein hellblauer Kasten mit dem schwarzen Logo `trafag.jpg` und „Cockpit“ gleich hoch (`.ci-brand` in `app.css`, `mix-blend-mode: multiply` gegen den weissen JPG-Grund). Das bisherige Rot heisst „klassisch“ und ist ueber den Paletten-Knopf in der Kopfleiste waehlbar; Wahl je Browser in `localStorage` (`trafag-skin`), `data-skin` am `<html>` schon im `<head>` gesetzt. Optik vor dem Commit als HTML-Nachbau mit Edge headless gegengeprueft, nicht in der laufenden App.

## Rohquellen Nur Bei Bedarf

- Diagramme: `docs/PROGRAMM_DIAGRAMME.md`
- Produktmapping: `docs/rag/PRODUCT_MAPPING.md`
- technischer Handoff und alter LLM-Systemkontext: `HISTORY_CANONICAL.md.raw`, nur noch in Git (`docs/archiv/README.md`)

- MENUE-HERVORHEBUNG (2026-10-13/16): `NavMenuNode.razor` ordnet Menueschluessel CSS-Klassen zu (`.nav-hl`, `nav-hl-r1..r5` gedeckte Rottoene je Hauptbereich, `nav-hl-home` pulsiert, `nav-hl-grey`/`nav-hl-admin`), Animationen in `app.css`, `prefers-reduced-motion` beachtet.
- STUECKLISTEN-GEHIRN (2026-10-16): `Components/Pages/PurchasingBomBrain.razor` zuoberst im Spend-Aufriss, Daten `Services/Logistics/BomBrainGraph.cs` (aus `BomInheritanceService` + `PurchasingMaterialLzCache`), Darstellung `wwwroot/js/bombrain.js` mit three.js (ein Linien-, ein Punktpuffer, feste Anordnung). Keine Registrierung in `Program.cs` noetig.