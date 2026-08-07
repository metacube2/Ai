# RAG Deployment

Stand: 2026-08-07

## Kurzstand

- Letzter produktiv verifizierter Deploy: **2026-08-07 10:22, Finance-Indikatoren
  ehrlich gemacht**, Funktionscommits `0c8cff5` und `b2e7c4f`, `455/455` Tests gruen
  (Release-Lauf vor dem Publish). `BiDashboard.dll` `07.08.2026 10:21:53`,
  `4'320'768` Bytes, SHA256
  `B43A9E4B49ADC3186A1DC7216F61E2C220BF5541C9A4180FBA9C51C7CA80E43D`; lokaler
  Release-Build und Server bitgleich. `app_offline.htm` gesetzt und danach
  umbenannt. HTTPS `200`: Startseite (`68'411` Bytes), `/management-cockpit`
  (`69'490`), `/finance-cockpit/vergleich` (`69'539`). Produktiv-DB in Laenge und
  Schreibzeit unveraendert (`339'210'240` Bytes, `07.08.2026 08:49:20`).
  Wirknachweis in der DLL: `FinanceCountryStatuses`, `IsExcludedByRule`,
  `MissingRateRowCount`, `DistinctMaterialNumberCount`, `MaterialKeys`,
  `GetAvailableReferenceYearsAsync` sowie die Literale `Nicht geprueft`,
  `Jahresumsatz`, `Pruefzeilen`; `YtdSalesChf` und `Passt gegen Soll` sind nicht
  mehr enthalten.
  **ACHTUNG zur Reichweite dieses Nachweises:** beide Finance-Routen liegen hinter
  dem Finance-Unlock und liefern von der Entwicklungsmaschine aus das
  Passwortpanel, nicht die Seite (geprueft: Antwort enthaelt `Finance Cockpit` und
  `Passwort`, aber nicht `Schnelluebersicht`). Der `200` belegt Erreichbarkeit,
  **nicht** dass die geaenderten Kacheln rendern — dafuer ist ein angemeldeter
  Sichtprueflauf noetig.
  **Behebt neun Indikatoren**, u. a. `Laender OK`/`Zu pruefen`, die zwei von vier
  Status verschwiegen (produktiv gemessen: `FinanceReferences` hat nur Zeilen fuer
  `2025`, Standardjahr der Seite ist `2026` — beide Kacheln standen auf `0`),
  Waehrungsmischung ohne Hinweis, Finance-Pivot auf ungefilterten Zeilen, und
  „Passt gegen Soll" als fest verdrahtete Ergebnisbehauptung.
  Details: `docs/FINANCE_INDIKATOREN_PRUEFUNG_2026-08-07.md`.

- Deploy davor: **2026-08-07 08:40, Einkauf-Indikatoren
  ehrlich gemacht**, Funktionscommit `eef6374`, `449/449` Tests gruen (Release-Lauf
  vor dem Publish). `BiDashboard.dll` `07.08.2026 08:40:33`, `4'293'632` Bytes,
  SHA256 `214C51E3D08479847813D49B04ED754D6AE5DA614CF458E806BE4AF256BD093A`;
  lokaler Release-Build und Server bitgleich. `app_offline.htm` gesetzt und danach
  auf `app_offline.htm.disabled` umbenannt. HTTPS `200` mit Inhalt: Startseite
  (`68'466` Bytes), `/einkauf/lieferanten` (`101'751`, warm `8.46 s`),
  `/einkauf/kontrakte` (`102'019`), `/einkauf/bestellbedarf` (`92'159`),
  `/logistik/materialdisposition` (`81'070`). Produktiv-DB in Laenge und
  Schreibzeit unveraendert (`339'210'240` Bytes, `07.08.2026 08:00:54`).
  Wirknachweis in der ausgelieferten DLL: `HasUnitCost`, `ApplyScopeFilter`,
  `LatestAverageUnitPriceLabel` sowie die Literale
  `Bewertungsdaten (EKBE/QM) nicht angebunden` und `Letztes Bestelldatum`;
  `Simulation bis Bewertungsdaten kommen` ist nicht mehr enthalten.
  **Behebt sechs Indikatoren, die eine erfundene oder falsch beschriftete Zahl
  zeigten** (u. a. `Performance Score` als Konstante aus zwoelf
  Simulationszeilen, `Preisindikator` als Gesamt-Spend unter Stueckpreis-Label,
  Kontrakt-KPI und -Diagramm mit verschiedenen Grundmengen, gruener Risikobalken
  strukturell `0`) und macht fehlende Stueckkosten im Fehlwert sichtbar.
  Details: `docs/EINKAUF_INDIKATOREN_PRUEFUNG_2026-08-07.md`.
  **WICHTIG fuer kuenftige Deploys:** Publish ueber
  `dotnet publish -c Release -o <UNC>`, NICHT ueber `/p:PublishProfile=FolderProfile` —
  das Profil hat `DeleteExistingFiles=true` und das Zielverzeichnis enthaelt
  `trafag_exporter.db` samt aller `.bak`-Sicherungen.

- Deploy davor: **2026-08-06 15:11, fuenf neue Supply-Chain-Reiter
  (Einkauf/Logistik)**, Funktionscommit `01af1b8`, `446/446` Tests gruen (vor dem Commit
  nachgerechnet, nicht uebernommen). `BiDashboard.dll` `06.08.2026 15:11:34`, `4'291'072`
  Bytes, SHA256 `29B9DFC6F46F74840431966E82040066F7B66FDD3AC8F12F73B4DF8F04761A61`.
  `app_offline.htm` gesetzt und wieder entfernt. Startseite und **alle fuenf neuen Routen**
  liefern HTTPS `200` mit Inhalt: `/logistik/materialdisposition` (81'078 Bytes),
  `/logistik/dispositionspruefung` (82'156), `/einkauf/bestellbedarf` (92'149),
  `/einkauf/materialabhaengigkeit` (102'922), `/einkauf/lieferperformance` (110'214).
  Wirknachweis in der DLL: `SupplyChainAnalysisService`, `SupplyChainAnalysisKind`,
  `SupplyChainUiTextCatalog`, `DeliveryPerformance`.
  Die Aenderung ist additiv: `Program.cs` +1 Zeile (DI), `DatabaseSeedService`,
  `NavigationIconResolver` und `UiTextService` zusammen +17 Zeilen; bestehende Einkaufs-,
  Spend-, Lieferanten- und Stuecklisten-Reiter unveraendert. Der Dienst ist rein lesend
  (kein `INSERT`/`UPDATE`/`DELETE`).
  **Bewusst keine OTIF-Kennzahl:** das Ist-Wareneingangsdatum aus EKBE/MSEG/MATDOC fehlt,
  deshalb weist die Lieferperformance nur das Plantermin-Risiko aus EKET aus und benennt
  die Luecke, statt eine Zahl zu schaetzen.
  Produktiv-DB: `339'210'240` Bytes / `06.08.2026 15:11:04` gegenueber `339'197'952` /
  `12:40:26` davor — die Aenderung stammt aus dem laufenden Betrieb bzw. dem WAL-Flush beim
  Herunterfahren, nicht aus dem Publish (keine Migration in diesem Stand).
  Details: `docs/EINKAUF_LOGISTIK_SUPPLY_CHAIN_REITER_2026-08-06.md`.

- Deploy davor: **2026-08-06 14:24, HR-ZH-Feiertage
  und Filtervertrag**, Funktionscommit `9435a5d`, `438/438` Tests gruen.
  `BiDashboard.dll` `06.08.2026 14:24:10`, `4'137'472` Bytes, SHA256
  `B8391FBFC69DBB6B45F93D1D6AF3D8FC621C34FD11405C14A0E52BF98397B7B0`.
  `app_offline.htm` gesetzt und danach aus dem aktiven Namen entfernt;
  Startseite und `/BiDashboard/hr-kpi` liefern HTTPS `200`. Details:
  `docs/HR_KPI_FEIERTAGE_FILTERTEST_2026-08-06.md`.

- Letzter produktiv verifizierter Deploy: **2026-08-06 13:57, ZDISPO-Zusatz
  nur fuer den Einkauf Spend-Aufriss**, Funktionscommit `0a8a4c9`, `435/435`
  Tests gruen. `BiDashboard.dll` `06.08.2026 13:57:11`, `4'136'448` Bytes,
  SHA256 `0F1CB29F6F766C8CB71903D45B78DB48B3AB94FE58638837F5376E9D2A9B01C1`.
  `app_offline.htm` gesetzt und danach aus dem aktiven Namen entfernt;
  Startseite HTTPS `200` (`64'770` Bytes),
  `/BiDashboard/einkauf/aufriss` HTTPS `200` (`133'542` Bytes, warm `10.15 s`).
  Produktiv lesend belegt: `45` Zeilen in `PurchasingSpendDisponentRule` aus
  `42` Mustern, `0` Zeilen in der unveraenderten manuellen
  `PurchasingProductGroupMap`, `105` ZLO03-Zeilen mit Disponent. Beide
  ZDISPO-XLSX-Dateien liegen im Publish-Verzeichnis. Details:
  `docs/PURCHASING_PRODUKTGRUPPEN_ABCXYZ_2026-08-06.md`.

- Letzter produktiv verifizierter Deploy: **2026-08-06 12:31, Einkauf
  Produktgruppen und ABC/XYZ**, Funktionscommit `bb009bf`, `435/435` Tests
  gruen. `BiDashboard.dll` `06.08.2026 12:31:27`, `4'120'064` Bytes, SHA256
  `B5C72496A7A4E11AC38675D840A5DF9DBABA6999517DD70FE3D7C0CE07BAEC3C`.
  `app_offline.htm` gesetzt und danach aus dem aktiven Namen entfernt;
  Startseite HTTP `200` (`64'755` Bytes),
  `/BiDashboard/einkauf/aufriss` HTTP `200` (`133'577` Bytes, warm `8.43 s`).
  Additive Produktivmigration lesend belegt: `MaterialUsageCache.VknrDispo`
  und `PurchasingProductGroupMap` vorhanden; `105` Usage-Zeilen mit Disponent,
  ZC23-Map noch leer. Wirknachweis in der DLL: Typen
  `PurchasingProductGroupAllocationSummary`, `PurchasingAbcXyzActionRow` und
  `PurchasingProductGroupMap` enthalten. Die produktive Haupt-DB blieb beim
  Publish bei `339'185'664` Bytes / `06.08.2026 12:27:49`; die additive
  Migration liegt im aktiven WAL. Details:
  `docs/PURCHASING_PRODUKTGRUPPEN_ABCXYZ_2026-08-06.md`.

- Deploy davor: **2026-08-06 11:06, Finance-Anzeige durchgesehen**,
  Commit `d9d9a4f`, `433/433` Tests gruen. `BiDashboard.dll` `06.08.2026 11:06:26`,
  `4'057'600` Bytes, SHA256
  `E6CCF3C4AC6484DC8605338004A949835184DF67B9C9AEDFA6E13103C86FAF7E`. `app_offline.htm`
  gesetzt und wieder entfernt, `https://…/BiDashboard/` liefert HTTP `200` (64'720 Bytes),
  Produktiv-DB in Laenge und Schreibzeit unveraendert (`339'140'608` Bytes,
  `06.08.2026 09:17:59`). Wirknachweis im Deploy-Artefakt: `IsCostBasisKnown` und
  `CostBasisUnknown` sind in der ausgelieferten DLL enthalten (zur SHA-Pruefung siehe
  den Hinweis weiter unten).
  **Behebt einen produktiven Anzeigefehler:** das Finance-Pruefbuch liess die Marge nur bei
  der Waehrungsmaske leer. Eine fehlende Kostenbasis laeuft als 0 durch, also wies die Spalte
  `Marge CHF` den vollen Umsatz und 100 % aus — neben einem Status, der „Lieferant unklar"
  bzw. „Konzernkosten fehlen" sagte. Naeherungsweise ~71'900 von 96'059 Zeilen betroffen
  (Tabelle im Cockpit und Excel-Export `Finance_Pruefbuch`; der zentrale Excel-Nachweis war
  korrekt, dort steht die Marge als Blattformel mit `WENN(Status=OK)`).
  Details: `docs/FINANCE_ANZEIGE_PRUEFUNG_2026-08-06.md`.

- Deploy davor: **2026-08-06 09:41, Gruppenmarge in einer Klasse**,
  Commit `515ab9d` (`GroupMarginCalculator`: Lieferantentyp, Kostenbasis, Kostenquelle und Status
  fuer Excel-Nachweis UND Cockpit aus einer Hand; Kostenbasisregeln als geordnete Kette),
  `431/431` Tests gruen. `BiDashboard.dll` `06.08.2026 09:41:56`, `4'054'528` Bytes, SHA256
  `CF750722BE3D9AA9377B77D4A9B5C53969D9F7326136D4313CFF557C3D54AA3D`. `app_offline.htm` gesetzt
  und wieder entfernt, `https://…/BiDashboard/` liefert HTTP `200` (64'735 Bytes), Produktiv-DB
  in Laenge und Schreibzeit unveraendert (`339'140'608` Bytes, `06.08.2026 09:17:59`).
  Wirknachweis im Deploy-Artefakt: `GroupMarginCalculator`, `GroupMarginCostRules`,
  `GroupDistributionWithoutGroupCost` und `GroupMarginLine` sind in der ausgelieferten DLL
  enthalten. Der Deploy behebt eine seit 2026-08-05 15:48 produktive Abweichung: das Cockpit
  zeigte fuer LRD-Zeilen ohne Konzernkostentreffer „Standardpreis fehlt", der Excel-Nachweis
  „Konzernkosten fehlen", und die Kennzahl „offene Kostenbasis" zaehlte diese Zeilen nicht mit.
  Details: `docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md` Abschnitt 7d.

  **Hinweis zur SHA-Pruefung:** der Build ist NICHT deterministisch — zwei Uebersetzungen
  derselben Quelle ergeben verschiedene Hashes (MVID). Der Vergleich „Server gleich lokaler
  Build" belegt deshalb nur, dass beide aus demselben Zwischenstand (`obj/`) kopiert wurden.
  Fuer den inhaltlichen Nachweis dient die Typenpruefung in der DLL.

- Wirkung am Produktivbestand geprueft (2026-08-06 09:45): der TRIN-Export vom selben Tag 06:54
  fuellt die neuen Felder — **6'664 von 7'094 TRIN-Zeilen tragen einen Sales Type (93,9 %)**,
  3'625 eine Trafag-Sachnummer (`FFM` 5'923, `LRD` 718, `CM` 23, leer 430). Alle anderen
  Standorte stehen erwartungsgemaess auf 0. Von 718 `LRD`-Zeilen finden 581 die Schweizer
  Konzernkosten (ueber die lokale Artikelnummer waeren es 4), 137 erhalten den Status
  `Konzernkosten fehlen`; 5'868 `FFM`/`CM`-Zeilen wechseln von „Lieferant unklar" auf intern.

- Deploy davor: **2026-08-05 15:48, Sales Type und
  Trafag-Sachnummer im Export** (`SalesType`/`GroupMaterialNumber` aus dem Artikelstamm,
  Klassifikation und Konzernkostenschluessel darauf umgestellt), `406/406` Tests gruen.
  `BiDashboard.dll` `05.08.2026 15:48:20`, `4'045'824` Bytes, SHA256
  `0C65C9971460EE47A9C1999FB328E43BEBC63AB71AE7EFCD6D07010588A4E5EF`; Release-Build und
  Server bitgleich. `app_offline.htm` gesetzt und entfernt, HTTP `200`. Additive Migration
  wirksam: `CentralSalesRecords.SalesType` und `.GroupMaterialNumber` sind produktiv als
  `TEXT NOT NULL DEFAULT ''` vorhanden. Gefuellt seit dem TRIN-Export 2026-08-06 06:54
  (Nachweis oben). Details: `docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md`.

- Deploy davor: 2026-08-05 10:59, **Server-Analyse**, Commit `cc72e6d`
  (`ServerAnalysisBackgroundService`: lesende Diagnoseabfragen gegen Standort-B1,
  ausgeloest ueber eine Triggerdatei in `_analysis`), `385/385` Tests gruen.
  `BiDashboard.dll` `05.08.2026 10:59:50`, `4'037'632` Bytes, SHA256
  `56AFD5AF156CD496A0EF42DFC5CF2E1FA724299BB632F3202FE0132131161B41`;
  Release-Build und Server bitgleich. Produktiv-DB in Laenge und Schreibzeit
  unveraendert (`338'472'960` Bytes, `03.08.2026 12:26:05`), `app_offline.htm`
  gesetzt und wieder entfernt, `https://…/BiDashboard/` liefert HTTP `200`
  (64'755 Bytes). Wirknachweis: Triggerlauf 11:13 und 11:20 haben Ergebnisdateien
  erzeugt, Protokollkategorie `Server-Analyse` zeigt Start/Ende ohne Fehler.
  Details: `docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md`.

- Deploy davor: 2026-08-03, Commit `9e28086`
  (`Logistik > Stuecklistenanalyse`: Top-Down-/Bottom-Up-Dashboard), `353/353`
  Tests gruen. `BiDashboard.dll` `03.08.2026 06:59:38`, `4'024'832` Bytes,
  SHA256 `8D5586E5536C83A9EDB409472C332D190488898C3FE8E8DB2097C3131779B554`;
  Release-Build und Server bitgleich. Produktiv-DB in Laenge, Schreibzeit und
  SHA256 unveraendert, `app_offline.htm` entfernt, Port 443 offen und der
  authentifizierte Aufruf von `/BiDashboard/logistik/stuecklistenanalyse`
  liefert HTTP `200`.

## Deployment-Historie

- Ersetzte Deploy-Zwischenstaende stehen in `lastchange.md` und den Archiven unter
  `docs/raw_md_archive/`; diese Kurzdatei fuehrt nur den aktuell verifizierten Deploy.

## Upgreat Firewall

- Upgreat muss den neuen Webserver freischalten, nicht den lokalen Entwicklungs-PC.
- Webserver / Source:
  - `trch-webapp-bidashboard.trafagch.local`
  - `tragvapp401.trafagch.local`
  - `10.120.1.17`
- Bekannte Ziele:
  - HANA Internal / BI1: `10.194.65.22:30015`
  - India HANA: `20.197.20.60:30015`
  - SAP OData / ZSCHWEIZ: `10.194.64.29:8000`
  - SharePoint / Graph: `trafagag.sharepoint.com:443`
- Offen: vollstaendige Standortliste aus produktiver App-Konfiguration exportieren/pruefen.

## Rohquellen Nur Bei Bedarf

- IIS-Handoff: `docs/DEPLOYMENT_IIS_HANDOFF_2026-05-19.md`
- historischer lokaler Uebergangsserver: `docs/LOCAL_DEV_SERVER_UEBERGANG_2026-05-21.md`
