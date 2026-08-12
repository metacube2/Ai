# BiDashboard (TrafagSalesExporter) — Requirements / Gesamtfunktionalität

Stand: 2026-07-10 · Status: **reverse-engineered** aus Code und bestehender Doku (nicht
vorwärts geschrieben). Zweck: eine zentrale Anforderungssicht auf die *gesamte* Anwendung, aus
der sich künftige Änderungen ableiten lassen. Detailverhalten und Historie stehen in den
verlinkten Themen-Docs; dieses Dokument ist die Klammer darüber.

Konventionen:
- Anforderungen sind mit `REQ-<BEREICH>-<Nr>` nummeriert und im Präsens/Soll formuliert.
- „Offen" markiert bewusst noch nicht umgesetzte oder fachlich ungeklärte Punkte.
- Technische Namen (Tabellen, EntitySets, Felder, Routen) sind bewusst nicht übersetzt.

---

## 1. Zweck und Kontext

Die Anwendung `TrafagSalesExporter` (produktiv publiziert als **`BiDashboard`**) ist ein
Business-Intelligence-/Reporting-Cockpit der Trafag-Gruppe. Sie konsolidiert Verkaufs-, Finanz-,
HR- und Einkaufsdaten aus mehreren Länder-/Quellsystemen in eine lokale Persistenz und stellt sie
als geführte Cockpits, Excel-Nachweise und Analyseansichten bereit.

- **REQ-CTX-1** Das System vereinheitlicht Daten aus SAP OData, SAP HANA / SAP Business One,
  SharePoint sowie manuellen Excel-/CSV-Quellen zu einer gemeinsamen Auswertungsbasis.
- **REQ-CTX-2** Zentrale Persistenz der Verkaufszeilen ist die Tabelle `CentralSalesRecords`.
- **REQ-CTX-3** Dieselbe Regel-/Transformationsengine liefert sowohl die Dashboard-Zahlen als auch
  den zentralen Excel-Export (eine Wahrheit für Anzeige und Nachweis).
- **REQ-CTX-4** Führende fachliche Zahl ist die lokale Hauswährung des jeweiligen Landessystems;
  Konzern-/CHF-Sichten sind additive Kontroll-/Reportingsichten, kein stiller Ersatz.

## 2. Rollen, Zugriff und Startseite

- **REQ-ACC-1** Die Startseite (`/`) ist neutral, ohne Login erreichbar, mit Trafag-naher Optik
  (Schrift, Manometer, optionales Strichmännchen; Schalter im Admin).
- **REQ-ACC-2** Der Admin-Bereich ist ein eigener Hauptmenüpunkt mit eigenem App-internem Login
  (`AdminAccess`), unabhängig vom Finance-Cockpit-Passwort; er darf nicht durch den
  Finance-Login blockiert werden.
- **REQ-ACC-3** Das Finance-Cockpit ist passwortgeschützt (eigener Unlock).
- **REQ-ACC-4** Das HR-KPI-Cockpit hat einen eigenen HR-Login; zusätzlich zum primären Benutzer
  `hr` sind weitere Admin-User über `HrKpiAccess.AdminUsers` zugelassen (aktuell `hradmin`).
  Passwörter liegen nur als Hash in `appsettings.json`.
- **REQ-ACC-5** Unbekannte URLs leiten über einen Blazor-`NotFound`-Handler auf die Startseite um.

## 3. Systemarchitektur (Ist)

- **REQ-ARC-1** ASP.NET-/Blazor-Anwendung (Server), publiziert als IIS-Webanwendung im
  `BiDashboard`-Schema nach `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`.
- **REQ-ARC-2** Persistenz in SQLite (`trafag_exporter.db`); die DB-Datei wird **nicht** mitpubliziert
  (Sidecar-Dateien `-wal`/`-shm` beim manuellen Ersetzen konsistent behandeln).
- **REQ-ARC-3** Schema wird beim Start über eine idempotente Schema-Maintenance nachgezogen
  (`AddColumnIfMissing`, Create-If-Not-Exists, RawJson-Backfills) — keine klassischen Migrations.
- **REQ-ARC-4** Navigation wird datengetrieben aus `NavigationMenuItems` gerendert und ist im
  Admin pflegbar (siehe REQ-ADM).
- **REQ-ARC-5** Mehrsprachigkeit über einen zentralen UI-Textservice (u.a. DE/EN, punktuell ES/IT/HI);
  technische Feld-/EntitySet-Namen bleiben unübersetzt.
- **REQ-ARC-6** SAP-Gateway-Aufrufe haben ein Timeout von 5 Minuten; große OData-Mengen werden
  seitenweise (`$top`/`$skip`/`$orderby`) gelesen (SAP-Seitenlimit 1000).

## 4. Datenquellen und Länder

- **REQ-SRC-1** Quellsysteme werden zentral in `SourceSystemDefinitions` gepflegt (u.a. `SAP` mit
  „Zentrale SAP Service URL", zentralem User/Passwort). Standorte (`Sites`) können die zentrale
  URL/Zugangsdaten optional überschreiben.
- **REQ-SRC-2** Länder-/Standort-Kurzregeln (führend je Land):

  | Land | TSC | Quelle | Wertbasis / Besonderheit |
  |---|---|---|---|
  | CH/AT | ZSCHWEIZ | SAP OData `FinanzdataSchweizOeSet` | Trennung über Buchungskreis/Reporting-Land |
  | DE | TRDE | Alphaplan CSV-Paar `invoice_headers.csv`+`invoice_lines.csv` (+ `Alphaplan*.zip`) | `NettoPreisGesamt`, GS negativ, EUR |
  | ES | TRSE/TRES | Sage CSV `Spain_Sales*.csv` (Basis + Range-Deltas) | `ImporteNeto`, REC/Credit negativ, EUR |
  | IT | — | Hauswährung, `Trafag Italia` ausgeschlossen | Duplikatlogik bei leerem Supplier-Country |
  | UK | TRUK | SharePoint `Import/Finance/UK_B1`, Sage Excel | `[Sales Price/Value]*[Quantity]`, Credit negativ, GBP |
  | IN | TRIN | SAGE/HANA `20.197.20.60:30015`, Schema `TRAFAG_LIVE` | INR Hauswährung, User-Override `TRAFAGCONTROLS` |

- **REQ-SRC-3** Jahresabgrenzung über `PostingDate`, Fallback `InvoiceDate`, dann `ExtractionDate`.
- **REQ-SRC-4** Wertbasis ist der Nettofakturawert pro Position; Gutschriften/Storno laufen als
  negative Beleg-/Positionszeilen.
- **REQ-SRC-5** Import-Guardrails: Ein SAP-Standort mit 0 Umsatzzeilen oder eine große
  Produktsparten-Referenz mit 0 Zuordnungen darf bestehende Dashboard-Daten **nicht** überschreiben.

## 5. Funktionsbereiche

### 5.1 Standorte & SAP/OData-Quellenpflege (`/standorte`)
- **REQ-STD-1** Pflege von Standorten (`Sites`, `TSC`), Quellsystem-Zuordnung und pro Standort
  optionalem SAP-URL-/Zugangs-Override.
- **REQ-STD-2** Grafische Pflege der SAP-Quellen, Joins und Zielmappings (`SapSourceDefinitions`,
  `SapJoinDefinitions`, `SapFieldMappings`); EntitySets können abgefragt/gecacht werden.

### 5.2 Manuelle Importe (`/manual-imports`)
- **REQ-IMP-1** Manuelle Importe ersetzen pro Standort den aktuellen Stand in `CentralSalesRecords`.
- **REQ-IMP-2** Delta-Dateien werden zusammen mit der Basisdatei gelesen und dedupliziert (primär
  `SourceLineId`, Fallback Invoice/Position/Material; Delta gewinnt gegen Vollbestand).
- **REQ-IMP-3** DE/Alphaplan liest CSV-Paare (Full + `delta`-Unterordner) und automatisch
  `Alphaplan*.zip` aus dem SharePoint-Ordner; ES liest alle `Spain_Sales*.csv`; UK Jahresdatei + Deltas.
- **REQ-IMP-4** Lokale Materialnummern (z.B. Alphaplan `ArtikelNummer`) sind nicht garantiert
  identisch mit der TR-AG-/SAP-`MATNR`; nicht gematchte Nummern bleiben fachlich
  „Nicht im TR-AG-Stamm".

### 5.3 Export-Dashboard & Audit-CSV (`/export-dashboard`)
- **REQ-EXP-1** Standortexporte erzeugen je Land ein Excel und laden es in den passenden
  SharePoint-Landesordner.
- **REQ-EXP-2** Der zentrale Export lädt progressiv nach SharePoint `Import/Finance/Alle`:
  `Sales_All_<Datum>.xlsx`, `Finance_Dashboard_Audit_All_<Datum>.csv`, danach Nachweis-Excel
  (bei >50'000 Zeilen als Teil-Dateien pro TSC/Land, je ca. ≤25'000 Zeilen).
- **REQ-EXP-3** Audit-CSV-Modus (optional): Standortexport schreibt nach Mapping/Transformation
  zusätzlich `Sales_ProcessedMergeInput_<TSC>_<Datum>.csv`. Zentrale Auswertungen können per
  Setting aus diesen CSV je TSC lesen, mit Fallback auf die zentrale `Finance_Dashboard_Audit_All_*.csv`.
- **REQ-EXP-4** Ohne Audit-CSV-Schalter lesen zentrale Auswertungen `CentralSalesRecords`. Steht
  der Schalter an und fehlen sowohl Standort- als auch zentrale CSV, ist die zentrale Auswertung
  nicht ausführbar (bewusst kein stiller Nulldurchlauf).

### 5.4 Transformationen & Finance-Regeln (`/transformations`, `/finance-rules`)
- **REQ-RUL-1** Feld-Transformationen (u.a. `ConvertCurrency`) und Finance-Regeln sind im Admin
  pflegbar; `RuleScope` unterscheidet Wert-/Zeilenregeln.
- **REQ-RUL-2** `DocumentRate` aus dem ERP ist ein gespeichertes Quellfeld; die App-Kurstabelle
  greift nur bei Anzeige-Währung, expliziter `ConvertCurrency`-Transformation oder Budget-CHF.
- **REQ-RUL-3** Die DE-`ForceYear`-Regel (2025) ist deaktiviert; DE folgt dem Fakturierungsdatum
  (reversibel im Admin).

### 5.5 Finance Cockpit / Finance Summary (`/management-cockpit`, `/finance-cockpit/*`)
- **REQ-FIN-1** Führende Sicht ist `Finance Summary` (KPI-Karten/Summen wie im zentralen Excel),
  gespeist aus derselben `FinanceRuleEngine`.
- **REQ-FIN-2** Filter für Jahr, Land und Währung wirken auf das Finance-Endergebnis.
- **REQ-FIN-3** Standard-Ist ist inklusive Positionen; Intercompany/2nd-party wird separat
  ausgewiesen („Ist ohne IC").
- **REQ-FIN-4** `Finance Vergleich` (Soll/Ist) und Referenzwerte je Land sind vorhanden
  (ES-2025-Referenz korrigiert auf `3'082'320.18 EUR`).
- **REQ-FIN-5** Excel-Nachweis „Zentrale Datei neu erzeugen" erzeugt `Finance_Dashboard_Nachweis_*`
  mit Formel-Summaries (`SUMIFS`/`COUNTIFS`/`IF`) und Detailblättern; Zahlen müssen mit den
  Dashboard-Reitern übereinstimmen (Abgleich je TSC `delta=0`).

### 5.6 Management-Analyse (Reiter im Management-Cockpit)
- **REQ-MAN-1** Die Management-Analyse ist Diagnose-/Plausibilitätssicht, nicht die führende
  Finance-Zahl. Sie ist links aufklappbar; Direktlinks springen per Query-Parameter in die Reiter.
- **REQ-MAN-2** Reiter: `Finance Summary`, `Länder` (Ist/IC/Ist-ohne-IC/Soll/Differenz/Status/
  Quelle/TSC), `Datenstatus`, `Abweichungen`, `Gutschriften`-Kandidaten, `Datenqualität`,
  `Spartenanalyse` (Unterreiter `Finanzanalyse` + `Zentrale Zuordnung`), `Gruppenmarge`,
  `Finance Pivot`, `Finance Prüfbuch`, `Rohdaten Diagnose`.
- **REQ-MAN-3** Jeder Datenreiter hat einen `Export to Excel`-Button, der die sichtbaren Tabellen
  in-memory als mehrblättrige `.xlsx` baut und direkt im Browser herunterlädt (typisierte
  Zahlen/Datum, Spalten per Reflection).
- **REQ-MAN-4** `Finance Pivot` (nach `sta.xlsx`) bietet Excel-ähnliche Filter für `Jahr`,
  `MTD Monat`, `TSC` auf Monats-/Tagesmatrix, YTD/MTD-Kacheln und den `Finance_Pivot`-Export;
  TSC-Auswahl aggregiert tagesgenau nur den gewählten Standort.
- **REQ-MAN-5** `Finance Prüfbuch` ist eine zeilenbasierte Excel-Prüfsicht (Originalbetrag/-währung,
  CHF-Kurs/-Betrag, Kursquelle/-jahr, Kunde, Material, Lieferant, Lieferantentyp, Standardkosten,
  Kostenbasis CHF, Marge CHF, Prüfstatus, Datenquelle) mit eigenem Export inkl. `Gruppenmarge Detail`.

### 5.7 Produktsparten-Mapping
- **REQ-PSM-1** SAP TR AG ist die Quelle der Wahrheit; die Ableitung `MATNR -> PAPH1 -> WWPFA ->
  WWPSP` liefert das SAP-Gateway als flache Referenz (`ProductDivisionRefSet`), sie wird **nicht**
  in C# nachgebaut.
- **REQ-PSM-2** Import-Join `Z.Matnr = P.Matnr` normalisiert Materialnummern beidseitig (Trim,
  Groß, Whitespace weg, führende Nullen weg). `ProductDivisionMapSet` (`M`) bleibt als inaktiver
  Rückfall im Seed.
- **REQ-PSM-3** Statuslogik: `Zugeordnet`, `Übrige` (`ProductDivisionCode=0008`, gültige Sammel-
  Sparte), `Nicht zugeordnet` (`UNASS`/leer trotz Referenz), `Nicht im TR-AG-Stamm` (kein Treffer),
  `Material fehlt` (Zeile ohne Matnr) — `Übrige` niemals mit `Nicht zugeordnet` zusammenwerfen.
- **REQ-PSM-4** Gruppierung nach `PAPH1 Detail`, `Produktfamilie`, `Produktsparte`; `Top 10`
  filtert nur die Anzeige, nicht die Summary; Länderflaggen und Sparten-Icons rein visuell.
- **REQ-PSM-5 (offen)** Komponenten-Fallback (`ZPOWERBI_VC_TXT-KOMPNR -> MATNR`, Sparte vom
  Kopfmaterial) nur wenn alle Kopfmaterialien dieselbe `WWPSP` ergeben; in OData noch nicht wirksam.

### 5.8 Gruppenmarge, Wechselkurse, Budget-CHF
- **REQ-GRP-1** `Gruppenmarge` ist bis zur Fachfreigabe nur Prüfsicht. Interner/Intercompany-
  Lieferant = Name oder Nummer enthält „Trafag" (plus GFS/Gesellschaft für Sensorik); Logik zentral
  in `GroupMarginSupplierClassifier` mit Wortgrenzen-Matching (kein Fehlmatch „Triton"/„Trinity").
- **REQ-GRP-2** `Group Margin = Umsatz + echte Konzern-Herstellkosten` der liefernden Gesellschaft
  (nicht IC-Verrechnungspreis). Externe Lieferanten nutzen Kosten der Verkaufszeile; interne Kette
  nur eine Iteration; fehlende Standardkosten werden als `Missing` markiert (nicht 50%, nicht geschätzt).
- **REQ-GRP-3 (offen)** Echte Konzern-Standardkosten je Liefergesellschaft (TR AG = MBEW-STPRS,
  TR IN = SAP B1, TR IT) sind noch nicht angebunden; `Marge Original`/`%` mischen Währungen, wenn
  `StandardCostCurrency` abweicht (`Marge CHF` korrekt).
- **REQ-CUR-1** Group-Currency-(CHF)-Umschalter: lokale Währung bleibt führend/Default; bei
  Aktivierung werden Ist **und** Soll je Zeile mit dem Kurs des **eigenen** Finance-Jahres nach CHF
  umgerechnet; Zeilen ohne Kurs bleiben lokal und werden per Notice ausgewiesen.
- **REQ-CUR-2** Das Anwendungsdatum des Wechselkurses ist in den Einstellungen konfigurierbar und
  wird in der Rohdaten-Diagnose angezeigt.
- **REQ-CUR-3 (offen)** Budget-CHF ist Kontroll-/Reportingkandidat; Spaltenumfang und Kursbasis
  (Budget- vs. Ist-Jahreskurs) sind mit Finance final zu entscheiden.

### 5.9 3D-Datenanalyse (Experten)
- **REQ-3D-1** Drehbare 3D-Ansicht mit Achsen (Zeit/Wert/Indikator), wählbaren Diagrammarten,
  einstellbarer Labelgröße, Szenario-Schieberegler (u.a. Wechselkurs) mit Realtime-Neuberechnung
  und `Sparten-Kreis je Land`. Zweck ist Visualisierung/Simulation, kein offizieller Soll/Ist-Wert.

### 5.10 HR-KPI-Cockpit (`/hr-kpi`)
- **REQ-HR-1** Datenquelle sind Rexx-/SAP-Dateien aus einem konfigurierbaren Datenordner
  (`HrKpi:DataFolder`, pro Lauf anpassbar).
- **REQ-HR-2** Funktionen: Anleitung, Datenfrische/-status, Ampeln, Periodenvergleich,
  Datenqualität, Austritte, Absenzen, Managementsicht (anonymisiert Personennamen), Drucken/PDF.
- **REQ-HR-3** `Fluktuation YTD` = fluktuationsrelevante Austritte 01.01.–Stichtag / durchschnittlicher
  Headcount im gleichen Zeitraum (laufendes Jahr bis heute/Bis-Stichtag, Vorjahre bis 31.12.).
  Fluktuations-Kacheln haben Hover-Formeln und thematische Farbcodierung.
- **REQ-HR-4** Krankenquoten-Nenner kappt beim laufenden Jahr auf heute; Vorjahresvergleich nutzt
  eine eigene ungefilterte Vergleichsliste; Datenqualitäts-Hinweise (SAP-Duplikate, Name-Join-Trefferquote).

### 5.11 Einkaufsdashboard (`/einkauf/*`)
- **REQ-PUR-1** SAP-Einkaufsdaten (EKKO/EKPO/EKET) werden per OData in lokale Cache-Tabellen
  geladen (`PurchasingEkkoCache`/`EkpoCache`/`EketCache`, Sync-Status `PurchasingSyncState`);
  das Cockpit liest Cache-first mit begrenzter Live-Probe als Fallback.
- **REQ-PUR-2** Voll- und Delta-Load (`Einkauf > Ideen > Einkauf-Datenservice`); Delta lädt neben
  geänderten Belegen (`Aedat`) zusätzlich alle Belege mit im Cache noch offener Menge nach
  (Wareneingänge ändern `Aedat` nicht), gebatcht.
- **REQ-PUR-3** CHF-Bewertung: `EKPO.Netwr` ist Belegwährung; Bewertung über `EKKO.Waers`/`Wkurs`
  (Wkurs>0 multiplizieren, <0 dividieren). Verifiziert: EUR ~65% der Belege, Wkurs 1.10 positiv.
- **REQ-PUR-4** Beleg-Mix-Trennung (`OrdersOnly`, Default an): Spend/offene KPIs zählen nur echte
  Bestellungen (`Bstyp='F'` ohne `Bsart='UB'`); Anfragen (A/AN), Kontrakte (K/MK), Umlagerungen
  (UB) sind ausgeschlossen. Leerer `Bstyp` (Bestandsdaten) wird bewusst eingeschlossen.
- **REQ-PUR-5** Endgelieferte Positionen (`Elikz='X'`) zählen nicht als offen (`ExcludeEndDelivered`,
  Default an). Löschkennzeichen: `EKPO.Loekz` gesetzt oder `MARA-MSTAE in (98,99)` (aus `MARA001Set`).
- **REQ-PUR-6** Offene Positionen nutzen eine eigene Periode nur mit Untergrenze (kein Abschneiden
  des zukünftigen Zulaufs); Liefertermin-Risiko-Buckets `Überfällig`/`0-7`/`8-30`/`Später`.
- **REQ-PUR-7** Ansichten/Routen: Dashboard, `Spend`, `Offene Bestellungen` (inkl. überfälliger
  Wert/Anzahl), `Kontrakte` (Restwert als Konnr-Abruf-Näherung gekennzeichnet), `Lieferanten`,
  `Ideen` (+ Unterseiten), `Kennzahlen-Katalog`, `PBIX Vorlage`, `3D Simulation`, `Datenquellen`.
- **REQ-PUR-8** Lieferantennamen aus `LFA1Set` (`Name1`) über normalisierten `Lifnr`-Join; fehlt
  der Name, bleibt die Nummer sichtbar (keine erfundenen Labels).
- **REQ-PUR-9** Excel-ähnliche Kaskadierungsmatrix Lieferant × Jahr (Standard 2020 bis dynamisch
  aktuelles Jahr, Top 40 nach Gesamt-Spend); Preisentwicklung je Artikel als mengengewichteter
  Ø-Stückpreis mit YoY-Trend.
- **REQ-PUR-10 (offen)** Marco-Anforderungen (Aufrisse Zeit/Produktgruppe/Materialgruppe/Kreditor/
  Region/Materialnummer, Lager/Kosten, Lieferantenperformance) siehe
  `docs/PURCHASING_DASHBOARD_UMSETZUNGSPLAN_MARCO_2026-07-09.md` und Vorbereitung in
  `docs/PURCHASING_DASHBOARD_VORBEREITUNG_INGO_2026-07-09.md`. Genutzte Zusatzquellen (MARC/MBEW/
  EKBE/LFA1-Adresse/QM) sind großteils im OData-Service verfügbar; echte Luecken: T023T/T024D-Texte,
  RESB. Property-Rollout auf travp762: `Bstyp`/`Bsart`/`Elikz` noch nachzuziehen (`Ktmng` vorhanden).

### 5.12 Admin (`/admin/sessions`, `/admin/menu-structure`, `/settings`)
- **REQ-ADM-1** Admin-Sessions/Login-Verwaltung unter eigenem Login.
- **REQ-ADM-2** `Menüstruktur`: Menüpunkte aus `NavigationMenuItems` umhängen, sortieren,
  aus-/einblenden (inkl. der Einkaufs-Unterpunkte).
- **REQ-ADM-3** `Einstellungen`: Quellsysteme (zentrale SAP-URL/User/Passwort), Wechselkurse,
  Export-Einstellungen (Audit-CSV-Schalter, Exportordner, Kursanwendungsdatum), Feature-Schalter
  (z.B. Strichmännchen).

### 5.13 Betrieb & Diagnose (`/logs`, `/diagnostics/interactive`, `/source-viewer`)
- **REQ-OPS-1** Zentrales Logging (`ExportLogs`) mit sichtbarer Log-Ansicht.
- **REQ-OPS-2** Interaktive Diagnose und `Source Viewer` zur Prüfung der Roh-/Quelldaten.
- **REQ-OPS-3** Schulungsseiten für Finance (`/finance-cockpit/schulung`) und HR (`/hr-kpi/schulung`).

## 6. Übergreifende Anforderungen

- **REQ-X-1** Jede Änderung wird über `dotnet test TrafagSalesExporter.sln` grün abgesichert
  (aktueller Stand: 155/155 lokal); neue Logik bekommt Tests.
- **REQ-X-2** Deploy nur nach Rücksprache: `app_offline.htm` setzen, `dotnet publish -c Release`
  auf den UNC-Share, `app_offline.htm` entfernen, Port 443 prüfen; DB nicht mitpublizieren.
- **REQ-X-3** Schema-Erweiterungen nur additiv über die Schema-Maintenance (Spalten ergänzen +
  RawJson-Backfill), keine destruktiven Migrations.
- **REQ-X-4** Excel-Exporte sind entweder serverseitige Nachweisdateien (SharePoint) oder
  in-memory Browser-Downloads; Zahlen bleiben typisiert.
- **REQ-X-5** Firewall-/Netz-Ziele (SAP OData `10.194.64.29:8000`, HANA BI1 `10.194.65.22:30015`,
  India HANA `20.197.20.60:30015`, SharePoint/Graph `trafagag.sharepoint.com:443`) werden vom
  produktiven Webserver aus benötigt, nicht vom Entwickler-PC.

## 7. Kern-Datenmodell (Auszug)

- `CentralSalesRecords` — konsolidierte Verkaufszeilen inkl. Produktfelder
  (`ProductHierarchy/Family/Division Code+Text`, `ProductMappingAssigned`), Beträge/Kurse.
- `Sites`, `SourceSystemDefinitions`, `SapSourceDefinitions`, `SapJoinDefinitions`,
  `SapFieldMappings` — Standort-/Quellkonfiguration.
- `NavigationMenuItems` — datengetriebene Navigation.
- `ExportSettings`, `ExportLogs`, `CurrencyExchangeRate…` — Betrieb/Export/Kurse.
- `PurchasingEkkoCache`/`EkpoCache`/`EketCache`, `PurchasingSyncState` — Einkaufs-Cache.
- HR-KPI liest dateibasiert (kein zentrales DB-Cache-Muster wie Einkauf).

## 8. Nicht-Ziele / Abgrenzung

- **REQ-NG-1** S/4HANA Compatibility Check und RPC-/RFC-Themen liegen außerhalb (Lucas).
- **REQ-NG-2** Infrastruktur, Security, Server, Netzwerk liegen außerhalb (Alex/Ramon/Upgreat).
- **REQ-NG-3** Fachliche Entscheide/Freigaben (Finance-Definitionen, IC-/2nd-party-Abgrenzung,
  Budget-CHF, Gruppenmarge, Einkaufs-Kontraktdefinition) verbleiben bei den Fachverantwortlichen;
  das System bildet sie ab, entscheidet sie nicht.
- **REQ-NG-4** Die App baut keine SAP-KEDR-/KE30-Ableitung in C# nach; Ableitungen liefert SAP.

## 9. Referenz-Detaildokumente

- Einstieg/Kurzstand: `docs/rag/PROJECT.md`
- Architektur: `docs/rag/ARCHITECTURE.md`, Diagramme `docs/PROGRAMM_DIAGRAMME.md`
- Finance: `docs/rag/FINANCE.md`, Formeln `docs/FINANCE_BERECHNUNGSFORMELN_LAENDER_2026-05-19.md`,
  Kurs-Workflow `docs/FINANCE_KURS_WORKFLOW_2026-06-09.md`, Gruppenmarge `docs/FINANCE_GRUPPENMARGE_2026-06-16.md`
- Produktmapping: `docs/rag/PRODUCT_MAPPING.md`, `docs/PRODUCT_SPARTEN_MAPPING_2026-05-27.md`
- Manual Import: `docs/rag/MANUAL_IMPORT.md`
- HR-KPI: `docs/rag/HR_KPI.md`, `docs/HR_KPI_NACHDOKU_2026-05-13.md`
- Einkauf: `docs/PURCHASING_DASHBOARD_2026-06-05.md` (+ Umsetzungsplan/Vorbereitung 2026-07-09)
- Deployment/IIS: `docs/rag/DEPLOYMENT.md`, `docs/DEPLOYMENT_IIS_HANDOFF_2026-05-19.md`
- Admin/Startseite: `docs/rag/ADMIN.md`, `docs/ADMIN_BEREICH_STARTSEITE_2026-05-21.md`

---

*Hinweis: Dieses Dokument ist reverse-engineered und beschreibt den Ist-Stand als Anforderungen.
Bei Abweichungen zwischen Dokument und Code gilt der Code; dann dieses Dokument nachziehen.*
