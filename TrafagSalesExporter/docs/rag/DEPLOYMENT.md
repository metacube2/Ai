# RAG Deployment

Stand: 2026-07-31

## Kurzstand

- Letzter produktiv verifizierter Deploy: 2026-07-31, Commit `4a3271b`
  (Spend-Matrix-Kontrast), Doku-Commit `c71885b`, `346/346` Tests gruen.
  `BiDashboard.dll` `31.07.2026 11:29:08`, `3'226'112` Bytes, SHA256
  `FE63A2970CAB1CAC400E8B178244686C75B4BE0293560A0417346CAC389B791E`;
  Release-Build und Server bitgleich. `app_offline.htm` gesetzt/entfernt,
  Produktiv-DB unveraendert, Port 443 offen, authentifizierter HTTPS-Aufruf
  `200` mit Titel `Trafag Finanze/Sales Management Cockpit`.

- Letzter produktiv verifizierter Code-Deploy: 2026-07-30, Commit `66a34da`,
  `BiDashboard.dll` `30.07.2026 14:51:54`, `3'223'552` Bytes. Inhalt:
  Einkauf-Delta von `Sites.IsActive` entkoppelt. Live-Abgleich am 2026-07-31
  10:21 MESZ: Fix ausgeliefert, aber noch kein produktiver Delta-Status nach dem
  Deploy vorhanden; Details:
  `docs/AKTUELLER_LIVEDATEN_STAND_2026-07-31.md`.

- Letzter Deploy: 2026-07-24, IIS-Hosting ROLLBACK zurueck auf `outofprocess` (ca. 1h nach dem
  Wechsel auf `inprocess` meldete Ingo schleichend zunehmende Verlangsamung ueberall, nicht nur
  Einkauf - siehe `lastchange.md`). Commit `410cf70` (gleicher Deploy: Ladebalken-Commit `f7ef248`
  im Einkauf-Filterbereich). Publish nach `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`;
  `app_offline.htm` gesetzt/entfernt; produktive `BiDashboard.dll` `24.07.2026 14:39:20`; Port 443
  -> True; HTTP 401 auf `/einkauf/spend`. Ursache der Verlangsamung noch offen - Ingo soll melden,
  ob Neustart genuegt oder es wiederkehrt.
- Vorheriger Deploy: 2026-07-24, IIS-Hosting zurueck auf `inprocess` (war seit 20.05.2026 faelschlich
  `outofprocess` stehen geblieben - siehe `lastchange.md`). Commit `4d2c6d3`; Publish nach
  `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`; `app_offline.htm` gesetzt/entfernt;
  produktive `BiDashboard.dll` `24.07.2026 13:20:07`; Port 443 -> True; HTTP 401 auf
  `/einkauf/spend` und `/diag.txt` (kein 500.30/502.5); DB unveraendert. Anlass: Ingo meldete
  haengende Seite/"Attempting to reconnect" beim Reiterwechsel, ueberall in der App. ROLLBACK:
  `web.config` Zeile zurueck auf `hostingModel="outofprocess"`, redeployen.
- Vorheriger Deploy: 2026-07-23 (fuenfter Deploy des Tages), Region-Balkendiagramm im Einkauf-Spend.
  Commit `c17b573 Add "volume by procurement region" chart to purchasing Spend tab`; `268/268`
  Tests gruen; Publish nach `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`;
  `app_offline.htm` gesetzt/entfernt; produktive `BiDashboard.dll` `23.07.2026 14:27:40`, Laenge
  `3'092'480`; Port 443 -> True; DB unveraendert. Erste neue Einkauf-Sicht (Volumen nach
  Beschaffungsregion); Werte fuellen sich erst mit dem naechsten Full Load. VknrDispo live
  bestaetigt. Details: `docs/PURCHASING_DASHBOARD_WUENSCHE_EINKAUF_2026-07-23.md`.
- Vorheriger Deploy: 2026-07-23 (vierter Deploy des Tages), Einkauf-Loader liest Land/ABC/XYZ.
  Commit `4d08da6 Load supplier country, ABC and XYZ classification into purchasing cache`;
  `268/268` Tests gruen; Publish nach `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`;
  `app_offline.htm` gesetzt/entfernt; produktive `BiDashboard.dll` `23.07.2026 13:50:06`, Laenge
  `3'088'896`; Port 443 -> True; DB unveraendert (neue Cache-Spalten `SupplierCountry`/`MaraAbc`/
  `MaraXyz` additiv beim App-Start). Neue SAP-Felder (LFA1.Land1, MARC.Maabc, ZSTR_MAT_XYZSet)
  werden in den Einkauf-Cache geladen; fuellen sich erst mit dem naechsten Full Load. Kein UI.
  Details: `docs/PURCHASING_DASHBOARD_WUENSCHE_EINKAUF_2026-07-23.md`.
- Vorheriger Deploy: 2026-07-23 (dritter Deploy des Tages), Loader zurueck auf MARA001Set fuer
  Matkl+Mstae. Commit `83eb149 Switch purchasing material-master loader back to MARA001Set for
  Matkl+Mstae`; `268/268` Tests gruen; Publish nach `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`;
  `app_offline.htm` gesetzt/entfernt; produktive `BiDashboard.dll` `23.07.2026 10:51:46`, Laenge
  `3'081'728`; Port 443 -> True; DB unveraendert. SAP hat MARA001Set um Matkl+Mstae erweitert;
  Loader liest jetzt beide daraus (ein ungepagter Request). NACHSORGE: Einkauf-Full-Load noetig,
  damit MaraMatkl im Cache gefuellt wird (mit Marco/Andreas abstimmen). Details:
  `docs/rag/PURCHASING.md`.
- Vorheriger Deploy: 2026-07-23 (zweiter Deploy des Tages), Balkendiagramm "Volumen nach Warengruppe"
  im Einkauf-Reiter Spend. Commit `bd47e63 Add "volume by material group" bar chart to purchasing
  Spend tab`; `268/268` Tests gruen; Publish nach `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`;
  `app_offline.htm` gesetzt/entfernt; produktive `BiDashboard.dll` `23.07.2026 10:17:40`, Laenge
  `3'081'728`; Port 443 -> True; DB unveraendert. Reine C#/Razor-Aenderung. WG-Datenlage bleibt
  offener Punkt (MARA-MATKL 0 %, Beleg-Matkl 99,6 % Sammelgruppe "01"). Details:
  `docs/rag/PURCHASING.md`.
- Vorheriger Deploy: 2026-07-23 (erster Deploy des Tages), Fix numerische Materialnummern in der Stuecklistenanalyse (C#-Seite).
  Commit `431f339 Fix numeric material numbers returning zero rows in BOM analysis`; `268/268`
  Tests gruen; Publish nach `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`; `app_offline.htm`
  gesetzt/entfernt; produktive `BiDashboard.dll` `23.07.2026 08:36:28`, Laenge `3'076'608`; Port 443
  -> True; DB unveraendert. C# paddet numerische Materialnummern jetzt auf 18 Stellen
  (`NormalizeMaterialToken`). ABAP-Teil des Fixes (Rohwert+MATN1 statt ALPHA) ist NICHT Teil dieses
  Deploys und muss erneut auf travt762 UND travp762 nachgezogen werden. Details:
  `docs/abap/README_LZCODE_WEBSERVICE.md` Nachtrag 2026-07-23.
- Vorheriger Deploy: 2026-07-22 (zweiter Deploy des Tages), Option "Auch geloeschte Materialien" in
  der Stuecklistenanalyse. Commit `bacc614 Add option to include deletion-flagged materials in
  BOM analysis`; `267/267` Tests gruen; Publish nach
  `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`; `app_offline.htm` gesetzt/entfernt;
  produktive `BiDashboard.dll` `22.07.2026 14:26:01`, Laenge `3'076'096`; `Test-NetConnection ...
  -Port 443` -> True; DB unveraendert. Reine App-Aenderung; der zugehoerige ABAP-Fix (Richtung-
  Suffix `ALLE`, LVORM-Bypass in Schritt 1) ist NICHT Teil dieses Deploys und muss weiterhin
  manuell in SE80 auf travt762 UND travp762 nachgezogen werden. Details: `lastchange.md`,
  `docs/abap/README_LZCODE_WEBSERVICE.md` Nachtrag 2026-07-22d.
- Vorheriger Deploy: 2026-07-22 (erster Deploy des Tages), Bereichs-Syntax `35-40` im Materialfeld der Stuecklistenanalyse.
  Commit `7d061d9 Support material number ranges (35-40) in BOM analysis material filter`;
  `265/265` Tests gruen; Publish nach `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`;
  `app_offline.htm` gesetzt/entfernt; produktive `BiDashboard.dll` `22.07.2026 13:22:34`, Laenge
  `3'075'584`; `Test-NetConnection ... -Port 443` -> True; DB unveraendert. Reine App-Aenderung
  (C#-seitige `$filter`-Konstruktion); die parallel gefundenen ABAP-Fixes (ALPHA-Konvertierung
  fuer Vknr/Kompnr, Quelltabelle ZPOWERBI_VC_TXT statt ZAT_VC) sind NICHT Teil dieses Deploys und
  muessen weiterhin manuell in SE80 auf travt762 UND travp762 nachgezogen werden. Details:
  `lastchange.md`, `docs/abap/README_LZCODE_WEBSERVICE.md`.
- Vorheriger Deploy: 2026-07-21, neuer Root-Reiter Logistik > Stuecklistenanalyse (ZLO03-Webservice).
  Commit `a314881 Add ZLO03 BOM-analysis webservice: SAP entity methods, C# loader, Logistik tab`;
  `260/260` Tests gruen; Publish nach `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`
  via `dotnet publish -c Release -p:PublishProfile=FolderProfile`; `app_offline.htm` gesetzt/
  entfernt; produktive `BiDashboard.dll` `21.07.2026 15:04:46`, Laenge `3'075'072`;
  `Test-NetConnection ... -Port 443` -> True; DB `trafag_exporter.db` unveraendert (`14:50:23`,
  neue Tabellen `MaterialUsageCache`/`MaterialParentCache`/`MaterialUsageSyncState` kommen
  additiv beim naechsten App-Start ueber die Schema-Maintenance). Inhalt: SAP-seitig zwei neue
  OData-EntitySets am bestehenden Service `ZPOWERBI_EINKAUF_SRV` (DPC_EXT-Methodenruempfe ohne
  eigene Klasse, in SEGW bereits aktiviert), C#-seitig `MaterialUsageDataRefreshService` mit
  dynamischer EntitySet-Namensaufloesung, neue Seite `BomAnalysis.razor`. Details:
  `docs/abap/README_LZCODE_WEBSERVICE.md`, `lastchange.md`. NACHSORGE: Erster echter Load-Test
  ueber die neue Seite gegen `travt762` (TEST) erwartet 0 Zeilen (ZAT_VC dort leer); echte Daten
  erst nach travt/travp-Umstellung (bekannter offener Punkt, hier nicht angefasst).
- Davor: 2026-07-17, drei Deploys am selben Tag (Details/Commits in `lastchange.md`): (1) `846e3f8` Deckungsbeitrag-Felder additiv vorbereitet + Excel-Felddokumentation, DLL `08:53:22`, `255/255`; (2) `3a4efb5` Einkauf-Spend-Drilldown + maracalcSet-Fix (MARA-Umbau), DLL `10:05:07`, `257/257`; (3) `c34e593` Button-Umbenennung „Alle Standorte laden", DLL `10:41:31`, `257/257`. Jeweils app_offline-Ablauf, Port 443 verifiziert, DB unveraendert (neue Spalten additiv beim App-Start). Zusaetzlich Datenlauf (kein Deploy): Einkauf-Full-Load gegen die Server-DB, `SupplierName` 99.99 % gefuellt.
- Vorheriger dokumentierter Deploy: 2026-07-16 (zweiter Deploy des Tages), NETWR_HC-Skalierungsfehler-Kompensation. Commit `d53498d Compensate NETWR_HC x100 SAP scaling bug on foreign-currency ZSCHWEIZ rows`; `246/246` Tests gruen; Publish nach `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`; `app_offline.htm` gesetzt/entfernt; produktive `BiDashboard.dll` `16.07.2026 16:09:24`, Laenge `2'949'120`; `Test-NetConnection ... -Port 443` -> True; DB unveraendert (Schreibzeit vor dem Publish). Inhalt: `SapCompositionService.CorrectHouseCurrencyScaling` korrigiert `NetwrHc` (-> `SalesPriceValue`/`DocumentTotalLocalCurrency`) fuer ZSCHWEIZ-Zeilen mit Fremdwaehrung nur, wenn das Hochskalieren um Faktor 100 naeher an `NetwrDc * Kurrf` liegt als der Rohwert — selbstdeaktivierend, kein blindes `*100`. NACHSORGE: TRCH/TRAT muss erneut importiert werden, damit die bestehenden ~40'506 Zeilen den korrigierten Umsatz bekommen (reine Code-Aenderung wirkt sonst nur auf zukuenftige Importe). Details: `docs/FINANCE_VBRP_WAVWR_SPEZ_2026-07-16.md` Abschnitt 14.
- Vorheriger Deploy: 2026-07-16, CH/AT-Kostenbasis ueber VBRP-WAVWR/MBEW-STPRS statt fest `=0`. Commits `565eae2 Map CH/AT StandardCost from VBRP-WAVWR/MBEW-STPRS instead of hardcoded 0`, `c2efbad Document production verification...`; `243/243` Tests gruen; Publish nach `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`; `app_offline.htm` gesetzt/entfernt; produktive `BiDashboard.dll` `16.07.2026 14:52:51`, Laenge `2'948'608`; `Test-NetConnection ... -Port 443` -> True; DB unveraendert bis zum anschliessenden Nutzer-Reimport. Nach Reimport (User-getriggert, 15:08-15:09 Uhr): `StandardCost`-Fuellgrad TRCH `96.5 %`, TRAT `99.9 %`. Details/Verifikation: `docs/FINANCE_VBRP_WAVWR_SPEZ_2026-07-16.md` Abschnitt 12+13, `docs/rag/FINANCE.md`. Dabei entdeckt (separat, nicht Teil dieses Deploys): `NETWR_HC`/`SalesPriceValue` ist bei Fremdwaehrungszeilen (~38.5 % der CH/AT-Zeilen) exakt Faktor 100 zu klein — offenes Thema fuer den SAP-Entwickler.
- Vorheriger dokumentierter Deploy: 2026-07-15 (zweiter Deploy des Tages), TR AG als liefernde Gesellschaft fuer die Gruppenmarge. Commit `5efeed7 Add TR AG delivering-company group cost for margin calculation`; `240/240` Tests gruen; Publish nach `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`; `app_offline.htm` gesetzt/entfernt; produktive `BiDashboard.dll` `15.07.2026 11:22:32`, Laenge `2'947'584`; `Test-NetConnection ... -Port 443` -> True; DB unveraendert (neue Tabelle `GroupStandardCosts` kommt additiv beim App-Start ueber die Schema-Maintenance, keine Migration). Inhalt: neue Tabelle `GroupStandardCosts` (MBEW-STPRS Bewertungskreis 1100 = TR AG, CHF), befuellt als Nebeneffekt des ohnehin laufenden CH/AT-SAP-Imports (`SapGatewayDataSourceAdapter.PersistGroupStandardCostsAsync`); `GroupMarginSupplierClassifier.ResolveDeliveringEntity` erkennt TR AG/TR IT/TR IN am `SupplierName`-Klartext (Stichprobe 8'995 interne Zeilen, 0 Kollisionen); TR-AG-gelieferte Gruppenmarge-Zeilen nutzen jetzt die echte Konzernkostenbasis statt lokaler Verkaufszeilen-Kosten, unabhaengig vom verkaufenden Land, verdrahtet in Dashboard/Pruefbuch UND zentraler/Nachweis-Excel. TR IN/TR IT NICHT geloest: TR IT live gegen SAP B1 geprueft (Schema `IT01_P` via BI1-HANA) — kein befuellter Standardkosten-Wert je Material (`OITM.PrdStdCst`/`AvgPrice` = 0 trotz realem Lagerbestand), offene Frage an Andreas/TR-IT-Controlling; TR IN vom Entwicklungsrechner aus nicht erreichbar. Details: `docs/FINANCE_GRUPPENMARGE_2026-06-16.md` Nachtrag 2026-07-15 Teil 2.
- Vorheriger Deploy: 2026-07-15 (erster Deploy des Tages), Gruppenmarge-Kostenwaehrungsschalter + Gruppenmarge-Blaetter im zentralen Excel, plus HR-KPI-Absenzschwellen/Prognose. Commits `3838a16 Add HR KPI absence thresholds and range-based turnover forecast` und `08f5572 Add group-margin cost-currency switch and central Excel margin sheets`; `226/226` Tests gruen; Publish nach `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`; `app_offline.htm` gesetzt/entfernt; produktive `BiDashboard.dll` `15.07.2026 08:53:47`, Laenge `2'935'296`; `Test-NetConnection ... -Port 443` -> True; DB unveraendert (neue Spalte `ExportSettings.GroupMarginCostCurrencyMode` kommt additiv beim App-Start ueber die Schema-Maintenance, keine Migration, Default `Mask`). Inhalt: (1) neues Setting `GroupMarginCostCurrencyMode` (Mask/Convert) fuer Gruppenmarge bei abweichender Kostenwaehrung, zentrale Logik `GroupMarginCostCurrencyConverter`, verdrahtet in Dashboard/Pruefbuch (`ManagementCockpitService`) UND Excel (`ExcelExportService`); `Pruefbuch.MarginOriginal/%` jetzt nullable; (2) zentrales `Sales_All_*.xlsx` traegt jetzt auch `Gruppenmarge Summary`/`Gruppenmarge Details`; (3) HR-KPI: konfigurierbare Krankenquote-Ampelschwellen + rot/Error-Stufe, Von/Bis-Range ohne explizites Jahr zeigt Monats-/Quartals-/YTD-/Prognosekacheln. Offene Punkte je Thema wie gewohnt in `docs/rag/FINANCE.md` „Offene Fachpunkte" und `docs/HR_KPI_NACHDOKU_2026-05-13.md`. NICHT geloest: Konzern-STPRS der liefernden Gesellschaft (Andreas-Fragen A/B).
- Vorheriger dokumentierter Deploy: 2026-07-14 (vierter Deploy des Tages), Kostenbasis der Gruppenmarge fuer CH/AT und DE. Commit `8e0f51e Fill group-margin cost basis for CH/AT and Germany`; `203/203` Tests gruen; produktive `BiDashboard.dll` `14.07.2026 17:30:30`, Laenge `2'923'008`; `app_offline.htm` gesetzt/entfernt; Port 443 erreichbar; DB unveraendert. Inhalt: CH/AT liest Standardpreise aus `mbewSet` (im SAP-Service bereits vorhanden, kein neues SAP-Objekt noetig), Schluessel Material + Bewertungskreis (CH=1100, AT=1200); DE leitet den Einstandswert aus `NettoPreisGesamt - RohertragGesamt` des Alphaplan-Exports ab; beides als STUECKpreis (STPRS/PEINH bzw. Zeilensumme/Menge), weil die Margenlogik mit der Menge multipliziert. Guardrail: faellt das Kostenlesen aus, laeuft der Umsatzimport weiter. Details: `docs/FINANCE_STANDARDKOSTEN_2026-07-14.md`. NACHSORGE: nach dem naechsten Export die Kostenquote fuer ZSCHWEIZ/TRDE gegen die SAP-Erwartung (96.5 %) pruefen.
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
