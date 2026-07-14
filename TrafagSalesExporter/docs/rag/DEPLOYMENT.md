# RAG Deployment

Stand: 2026-07-14

## Kurzstand

- Letzter dokumentierter Deploy: 2026-07-14 (vierter Deploy des Tages), Kostenbasis der Gruppenmarge fuer CH/AT und DE. Commit `8e0f51e Fill group-margin cost basis for CH/AT and Germany`; `203/203` Tests gruen; produktive `BiDashboard.dll` `14.07.2026 17:30:30`, Laenge `2'923'008`; `app_offline.htm` gesetzt/entfernt; Port 443 erreichbar; DB unveraendert. Inhalt: CH/AT liest Standardpreise aus `mbewSet` (im SAP-Service bereits vorhanden, kein neues SAP-Objekt noetig), Schluessel Material + Bewertungskreis (CH=1100, AT=1200); DE leitet den Einstandswert aus `NettoPreisGesamt - RohertragGesamt` des Alphaplan-Exports ab; beides als STUECKpreis (STPRS/PEINH bzw. Zeilensumme/Menge), weil die Margenlogik mit der Menge multipliziert. Guardrail: faellt das Kostenlesen aus, laeuft der Umsatzimport weiter. Details: `docs/FINANCE_STANDARDKOSTEN_2026-07-14.md`. NACHSORGE: nach dem naechsten Export die Kostenquote fuer ZSCHWEIZ/TRDE gegen die SAP-Erwartung (96.5 %) pruefen.
- Vorheriger Deploy: 2026-07-14 (dritter Deploy des Tages), CH/AT im Journal-Import. Commit `935561f Add CH/AT general-ledger journal import via SAP OData`; `189/189` Tests gruen; produktive `BiDashboard.dll` `14.07.2026 11:24:26`, Laenge `2'907'136`; `app_offline.htm` gesetzt/entfernt; Port 443 erreichbar; DB unveraendert (Spalte `CompanyCode` kommt additiv beim App-Start). Inhalt: neuer OData-Reader `SapGatewayFinancialJournalReader` fuer das ECC-Hauptbuch (`BKPF`/`BSEG`) ueber EntitySet `FinanzJournalSet`; Routing nach Anschlussart; `CompanyCode` (= Bukrs) trennt CH/AT; Menuetitel auf `Journal Import` verallgemeinert. WICHTIG: Das EntitySet fehlt auf `travp762` noch — SAP-Spez fuer Lucas: `docs/FINANCE_JOURNAL_SAP_ODATA_SPEZ_2026-07-14.md`; bis zum SAP-Rollout meldet der CH/AT-Lauf eine klare Fehlermeldung, alle anderen Gesellschaften laden normal.
- Vorheriger Deploy: 2026-07-14 (zweiter Deploy des Tages), Indien im B1-Journal-Import. Commit `2977c74 Include India in B1 journal import`; `186/186` Tests gruen; produktive `BiDashboard.dll` `14.07.2026 10:33:06`, Laenge `2'893'824`; `app_offline.htm` gesetzt/entfernt; Port 443 erreichbar; DB unveraendert. Inhalt: Indien ist fachlich SAP B1, aber unter dem irrefuehrenden Quellsystem-Code `SAGE` konfiguriert — die Journal-Standortauswahl grenzt daher ueber die Anschlussart HANA + Schema ein statt ueber den Code (`IsJournalSite`), der Reader prueft vorab per `sys.tables`, ob `OJDT`/`JDT1` existieren. Umfang jetzt FR/IT/US/IN. NACHSORGE: ersten Indien-Lauf ueber `Finance Cockpit > B1 Journal Import` fahren und bestaetigen, dass `OJDT`/`JDT1` in `TRAFAG_LIVE` vorhanden sind. OFFEN: CH/AT-Journal (SAP OData, `BKPF`/`BSEG`/`ACDOCA`) braucht eigenen Reader plus neues EntitySet auf SAP-Seite.
- Vorheriger Deploy: 2026-07-14, B1-Journal-Import (Hauptbuch-Buchungszeilen) in neue, separate Tabelle `FinancialJournalEntries`. Commit `8db6350 Add B1 journal import into separate FinancialJournalEntries table`; `dotnet test TrafagSalesExporter.sln --verbosity minimal` mit `185/185` gruen; Publish nach `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`; `app_offline.htm` gesetzt und wieder entfernt; produktive `BiDashboard.dll` Zeitstempel `14.07.2026 08:27:29`, Laenge `2'885'120`; `Test-NetConnection ... -Port 443` erfolgreich; DB unveraendert publiziert (neue Tabelle wird additiv beim naechsten App-Start ueber die Schema-Maintenance angelegt, keine Migration). Inhalt: neuer Reiter `Finance Cockpit > B1 Journal Import` (`/finance-journal-import`) laedt je B1-Gesellschaft (FR/IT/US, Quellsystem `BI1`) Hauptbuchzeilen aus `OJDT`/`JDT1`/`OACT`/`OADM` nach der Feld-Prioliste von Andreas; komplett getrennt vom Sales-Datenfluss (`CentralSalesRecords` unveraendert), eigenes Logging in `AppEventLogs` statt `ExportLogs`. NACHSORGE: vor dem ersten echten Produktivlauf B1-Spaltennamen (`ProfitCode`, `OcrCode2`, `FCCurrency`, `StornoToTr`, `AutoStorno`) einmal live gegen `fr01_p` proben; danach ersten Journal-Load ueber die neue Seite fahren. Details: `docs/FINANCE_B1_JOURNAL_IMPORT_2026-07-14.md`.
- Vorheriger dokumentierter Deploy: 2026-07-13, Daten-Heartbeat-Ausbau (Exportlauf-Streifen + 7-Tage-Glaettung) plus UK-Selbstfuetterungs-Fix. Commits `78d2772 Add export-run stripe and 7-day smoothing to data heartbeat`, `2a94395 Fix UK manual import reading its own export outputs`; `dotnet test TrafagSalesExporter.sln --verbosity minimal` mit `176/176` gruen; Publish nach `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`; `app_offline.htm` gesetzt und wieder entfernt; produktive `BiDashboard.dll` Zeitstempel `13.07.2026 21:03:09`, Laenge `2'836'992`; `Test-NetConnection ... -Port 443` erfolgreich; DB unveraendert (keine Migration). Inhalt: (1) Heartbeat zeigt zweiten Streifen `Exportlauf` aus `ExportLogs` je Tag/TSC (trennt Update-Ausfall von echter Geschaeftsflaute) plus Schalter `7-Tage-Summe`; (2) Rootcause fuer nahezu leeres UK behoben — Manual-Import las die eigene hochgeladene Audit-CSV/Excel als "neueste TRUK-Datei" und ersetzte den Bestand damit taeglich; `IsOwnExportOutputFile` schliesst eigene Ausgaben aus, Ordner-Import ohne Jahresangabe liest jetzt Basis+alle neueren Deltas statt nur der neuesten Datei. NACHSORGE: UK-Export einmal laufen lassen und Bestand/Wert der Rechnung 0000043747 fachlich pruefen; ZSCHWEIZ-2026-Daten fehlen weiterhin SAP-seitig komplett (separates Thema, kein Deploy-Fix). Details: `docs/rag/FINANCE.md`, `docs/rag/MANUAL_IMPORT.md`.
- Vorheriger dokumentierter Deploy: 2026-07-10, Einkaufs-Korrekturen aus zwei Reviews (Marco) plus
  neue Felder/Logik. Commit `335907c` (Spitze; enthaelt auch `6ed61e3` Beleg-Mix + `REQUIREMENTS.md`);
  `dotnet test` `157/157` gruen; Publish nach `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`;
  `app_offline.htm` gesetzt/entfernt; produktive `BiDashboard.dll` Zeitstempel `10.07.2026 14:17:01`,
  Laenge `2'782'208`; DB unveraendert; `Test-NetConnection ... -Port 443` -> True. Inhalt: Beleg-Mix-
  Trennung (`Bstyp`/`Bsart`), Elikz-Ausschluss offener Werte, `Ktmng`/`Elikz`/`Bstyp`/`Bsart`
  persistiert; Verpflichtungen/offene Werte zeitraumunabhaengig (Stand heute); Loeschkennzeichen-Split
  (MSTAE 98/99 nur bei offenen Werten, nicht im historischen Spend); ueberfaellige Positionen;
  Preisentwicklung je Artikel; Kachel-/Label-Fixes; Lieferanten-Register folgt Zeitraum.
  **WICHTIG/RISIKO:** Der Loader-`$select` fordert jetzt `Bstyp`/`Bsart` (EKKO) und `Elikz` (EKPO).
  Diese Properties fehlen auf **travp762** noch im OData-Modell (nur `Ktmng` vorhanden; per Probe
  2026-07-10 bestaetigt). Ein Einkauf-Full-/Delta-Load gegen travp762 wuerde daher fehlschlagen bzw.
  den Cache leeren -> **erst nach P-Modell-Rollout laden**; solange die zentrale SAP-URL auf dem
  Test-Modell (mit Feldern) steht, ist der Load ok. Details: `docs/PURCHASING_DASHBOARD_REVIEW_MARCO_2026-07-10.md`,
  `docs/PURCHASING_DASHBOARD_VORBEREITUNG_INGO_2026-07-09.md` (A0).
- Vorheriger dokumentierter Deploy: 2026-07-07, Einkaufs- und HR-Dashboard-Formel-/Logik-Korrekturen (Review).
- Deploy 2026-07-07: Commit `1afac2f Fix purchasing and HR dashboard formula/logic issues`; `dotnet test TrafagSalesExporter.sln --verbosity minimal` mit `141/141` Tests gruen; Publish nach `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`; `app_offline.htm` gesetzt und wieder entfernt; produktive `BiDashboard.dll` Zeitstempel `07.07.2026 07:00:42`, Laenge `2'761'728`; `Test-NetConnection ... -Port 443` erfolgreich; produktive DB unveraendert vorhanden. Inhalt Einkauf: CHF-Bewertung via `EKKO.Waers/Wkurs`, Delta laedt Belege mit offener Menge nach (Wareneingaenge aendern `Aedat` nicht) + Batching, Zukunfts-Zulauf nicht mehr am Bis-Filter abgeschnitten, Kontrakt-Restwert via `Konnr` statt Kopie des offenen Werts, dynamische Jahresachse, gewichteter Preistrend, Label-/Filter-Fixes. Inhalt HR: Vorjahresvergleich aus ungefilterter Austrittsliste (war immer 0), Krankenquoten-Nenner auf heute gekappt bzw. aus Absenzdaten abgeleitet, Top-Absenzen pro Person aggregiert, YTD-konsistenter Fluktuations-Nenner, neue Datenqualitaets-Hinweise (SAP-Duplikate, Name-Join). NACHSORGE: einmal Einkauf-Full-Load noetig (`Einkauf > Ideen > Einkauf-Datenservice`), damit `Waers`/`Wkurs`/`Konnr` real gefuellt werden; Backfill deckt Bestandsdaten aus `RawJson` ab; HR braucht keinen Reload. Details: `docs/PURCHASING_DASHBOARD_KORREKTUREN_2026-07-06.md`, `docs/HR_KPI_KORREKTUREN_2026-07-06.md`.
- Vorheriger dokumentierter Deploy: 2026-07-02, Finance-Logik-Korrekturen (Review).
- Deploy 2026-07-02: Commit `5c9749c Fix finance dashboard correctness issues`; `dotnet test TrafagSalesExporter.sln --verbosity minimal` mit `136/136` Tests gruen; Publish nach `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`; `app_offline.htm` gesetzt und wieder entfernt; Port 443 erreichbar. Inhalt: Gutschriften-Vorzeichen im Excel-Nachweis, Classifier-Wortgrenzen, Audit-CSV-TSC-Fallback, Export-Quellenkonsistenz, Group-CHF pro Zeilenjahr + Missing-Rate-Hinweis. Offen/latent: Waehrungsmischung `Marge Original`/`%`.
- Vorheriger Deploy: 2026-07-02, Einkaufs-Lieferantennamen aus LFA1.
- Deploy 2026-07-02: Commit `d5f329b Resolve purchasing supplier names from LFA1`; `dotnet test TrafagSalesExporter.sln --verbosity minimal` mit `130/130` Tests gruen; Publish nach `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`; produktive `BiDashboard.dll` Zeitstempel `02.07.2026 09:24:51`, Laenge `2'748'928`; `app_offline.htm` gesetzt und wieder entfernt; `Test-NetConnection ... -Port 443` erfolgreich. `PurchasingDataRefreshService` liest jetzt `LFA1Set` (`Lifnr,Name1`) und fuellt `PurchasingEkkoCache.SupplierName`. NACHSORGE: einmal Einkauf-Full-Load noetig, damit `SupplierName` gefuellt wird.
- Vorheriger dokumentierter Deploy: 2026-07-01, HR-Fluktuations-Kachel-Hovertexte.
- Deploy 2026-07-01: Commit `874a61c Add HR turnover metric tooltips`; `dotnet test TrafagSalesExporter.sln --verbosity minimal` mit `125/125` Tests gruen; Publish nach `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`; produktive `BiDashboard.dll` Zeitstempel `01.07.2026 08:20:54`, Laenge `2'741'760`; `app_offline.htm` entfernt; `Test-NetConnection trch-webapp-bidashboard.trafagch.local -Port 443` erfolgreich.
- Vorheriger dokumentierter Deploy: 2026-07-01, Finance Pivot Filter und HR-Fluktuations-Kachelfarben.
- Deploy 2026-07-01: Commit `7aec787 Clarify HR turnover metric cards`; HR-Fluktuations-Kacheln klarer beschriftet, thematisch farbig hinterlegt und `Fluktuation YTD` fachlich als 01.01. bis Stichtag abgegrenzt.
- Deploy 2026-07-01: Commit `723a60c Add finance pivot filters`; Finance Pivot hat Excel-aehnliche Filter fuer `Jahr`, `MTD Monat` und `TSC`; produktive `BiDashboard.dll` Zeitstempel `01.07.2026 07:07:36`.
- Deploy 2026-06-30, Fallback auf zentrale `Finance_Dashboard_Audit_All_*.csv` fuer Finance Pruefbuch / Audit-CSV-Quelle.
- Deploy-Fix 2026-06-30 nach Finance-Pruefbuch-Fehler: Ursache war aktive Audit-CSV-Quelle ohne sichtbare `Sales_ProcessedMergeInput_*.csv` im produktiven App-Output; vorhanden war `Finance_Dashboard_Audit_All_2026-06-18.csv`. Code-Fix: `CentralSalesDataProvider` liest zuerst Standort-CSV und faellt danach auf `ExportAuditCsvService.ReadLatestConsolidatedAuditCsvRecordsAsync()` zurueck. Commit `214989f Fallback to consolidated audit CSV`.
- Produktive DB-Settings nach Fix 2026-06-30: `AuditCsvEnabled=1`, `UseAuditCsvAsCentralSource=1`, `LocalSiteExportFolder=''`. Damit nutzt die App ihren Content-Root-Output `C:\inetpub\wwwcust\BiDashboard\output`; externer Zugriff ueber `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\output`.
- Produktive DLL nach Fix-Deploy 2026-06-30: `BiDashboard.dll`, Zeitstempel `30.06.2026 11:06:57`, Laenge `2'674'176`. Neues stdout-Log `stdout_20260630090804_7156.log`: App startet in Production, Content root `C:\inetpub\wwwcust\BiDashboard`; kein neuer Audit-CSV-Fehler im Log.
- Deploy-Ablauf 2026-06-30: `app_offline.htm` gesetzt, `dotnet publish TrafagSalesExporter.csproj -c Release -o \\trch-webapp-bidashboard.trafagch.local\BiDashboard$ --verbosity minimal`, danach `app_offline.htm` entfernt. Zweiter kurzer Publish fuer Navigation-Seed `finance-audit-ledger`.
- Servercheck nach Deploy 2026-06-30: `Test-Path ...\app_offline.htm` -> `False`; `Test-NetConnection trch-webapp-bidashboard.trafagch.local -Port 443` -> `TcpTestSucceeded : True`.
- Produktive DLL nach Deploy 2026-06-30: `BiDashboard.dll`, Zeitstempel `30.06.2026 10:29:09`, Laenge `2'672'640`.
- Vorherige produktive DB-Settings am 2026-06-30 wurden kurzzeitig auf `LocalSiteExportFolder=\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\output` gesetzt; das wurde nach dem Fehler wieder auf leer korrigiert, damit der IIS-Prozess seinen lokalen Output-Ordner verwendet.
- `TrafagSalesExporter` wird als ASP.NET/IIS-Webanwendung im bisherigen `BiDashboard`-Schema publiziert.
- Vorheriger dokumentierter Deploy: 2026-06-29, Commits `4805317`–`6856a62`.
- Publish-Ziel: `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`.
- Letzte Validierung vor Deploy: `dotnet test TrafagSalesExporter.sln --verbosity minimal`, Ergebnis `124/124` Tests gruen.
- Deploy-Ablauf 2026-06-29: `app_offline.htm` gesetzt, `dotnet publish TrafagSalesExporter.csproj -c Release -o \\trch-webapp-bidashboard.trafagch.local\BiDashboard$ --verbosity minimal`, danach `app_offline.htm` entfernt.
- Servercheck nach Deploy: `Test-Path ...\app_offline.htm` -> `False`; `Test-NetConnection trch-webapp-bidashboard.trafagch.local -Port 443` -> `TcpTestSucceeded : True`.
- Produktive DLL nach Deploy: `BiDashboard.dll`, Zeitstempel `29.06.2026 11:36:03`.
- Deployede Aenderungen (Andreas-Entscheide + heutige Arbeit): Gruppenmarge interner Lieferant = Name/Nummer enthaelt „Trafag"; DE-Finance-Jahr folgt Fakturierungsdatum (DE-ForceYear-2025-Regel deaktiviert); Group-Currency-(CHF)-Umschalter im Management-Cockpit; per-Reiter „Export to Excel"-Buttons; Schnellübersicht Sparten-Abdeckung inkl. Uebrige + Datenstand-Zeitzonen-Fix; Alphaplan-PSCredential-Fix; HomeRedirect.
- WICHTIG nach diesem Deploy: DE-Daten neu importieren, damit 2026er DE-Rechnungen ins Finance-Jahr 2026 wandern (vorher auf 2025 gezwungen). Group-Currency nutzt vorhandene Jahreskurse (Budgetkurse); Kursbasis ggf. final mit Finance bestaetigen.
- Vorheriger Deploy: 2026-06-26, Commits `6943a66`–`3d5a23d`, DLL `26.06.2026 07:47:25` (Schnellübersicht-Fixes, Alphaplan, HomeRedirect, MARA-MSTAE).
- Vorheriger Deploy: 2026-06-18 Einkaufsdashboard-Matrix und Einkaufsfilter, Commit `4f45805`, DLL `18.06.2026 09:29:11`.
- Vorheriger Deploy 2026-06-17: zentraler Finance-Audit-/Nachweisexport, Commit `65f2ded Upload central finance audit exports`.
- Vorheriger Deploy 2026-06-16: HR-Admin, Finance-3D-Spartenkreis und Gruppenmarge.
- Vorheriger Deploy 2026-06-11: Finance-Schulung/Dashboard-UI, Commit `f751295`, `BiDashboard.dll` `11.06.2026 12:04:53`.
- Naechster lokaler Deploy-Kandidat: neues Produktsparten-Mapping fuer den vollstaendigen SAP-OData-Referenzservice. Seed-Ziel: `ZSCHWEIZ` Quellen `Z:FinanzdataSchweizOeSet`, `P:ProductDivisionRefSet` aktiv, `M:ProductDivisionMapSet` inaktiv; aktiver Join nur `Z.Matnr=P.Matnr`, mit beidseitiger Matnr-Normalisierung im Import.
- OData nach SAP-Fix geprueft: `ZPOWERBI_EINKAUF_SRV/ProductDivisionRefSet` auf `travp762` liefert `48'897` Zeilen, `48'895` assigned, `8'715` Uebrige/`0008`, `2` UNASS. `FinanzdataSchweizOeSet` liefert `30'642` Zeilen fuer 2025 und `0` fuer 2026.
- Nach Deploy dieses Stands und nach korrekter SAP-Service-URL muss `ZSCHWEIZ` neu exportiert/importiert werden, damit `CentralSalesRecords` die neuen direkten `P.*`-Produktfelder und Status `Übrige` erhaelt.
- Schutz im Code: SAP-Import bricht ab, wenn `ProductDivisionRefSet` eine grosse Referenz mit 0 zugeordneten Sparten liefert oder wenn ein SAP-Standort 0 Umsatzzeilen liefert; bestehende Dashboard-Daten werden dann nicht ueberschrieben.
- CH/AT-Import nach Deploy: `FetchedRecords=40'292`, `Assigned=36'953`, `UnassignedWithReference=0`.
- DB-Backup vor Produktsparten-Seed/Import: `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\trafag_exporter.db.before-productdivision-map-20260610-161022.bak`.
- Produktive India-DB-Konfiguration nach Seed: `TRIN -> SAGE -> 20.197.20.60:30015`, Schema `TRAFAG_LIVE`, User-Override `TRAFAGCONTROLS`.
- DB-Backup vor India-Seed: `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\trafag_exporter.db.before-india-sage-20260610-0825.bak`.
- Lokaler Uebergangsserver: `http://172.16.9.185:5000` im Trafag-Netz, IP kann wechseln.
- Lokale URLs bleiben `https://localhost:55415` und `http://localhost:55416`.
- Fuer andere PCs nutzt der Uebergang bewusst HTTP auf Port `5000`.

## Serverproblem

- Lokaler HTTPS-Smoke-Test per `Invoke-WebRequest` scheitert weiterhin mit Empfangs-/TLS-Fehler; Publish und Share-/DB-Pruefungen sind davon getrennt.
- Aelterer dokumentierter Befund: TLS fordert Client-Zertifikat.
- IT soll IIS SSL Settings pruefen: Client certificates `Ignore` oder hoechstens `Accept`, nicht `Require`.

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
- lokaler Server: `docs/LOCAL_DEV_SERVER_UEBERGANG_2026-05-21.md`
