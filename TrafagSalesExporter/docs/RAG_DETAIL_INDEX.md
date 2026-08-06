# RAG Detailindex

Stand: 2026-08-01

Diese Datei ist die zweite Navigationsebene hinter `docs/RAG_ROUTER.md`.
Detailquellen nur laden, wenn die jeweilige Kurzdatei nicht ausreicht oder ein
Audit-/Quellnachweis gefragt ist. Historische Zahlen nie ungeprueft als aktuellen
Stand verwenden.

## Finance-Detailquellen

| Bedarf | Datei |
| --- | --- |
| Fachentscheide | `docs/FINANCE_ENTSCHEIDE.md` |
| Schulung und Prozessgrafiken | `docs/FINANCE_SCHULUNG_FINANZ_2026-06-11.md` |
| Laenderformeln | `docs/FINANCE_BERECHNUNGSFORMELN_LAENDER_2026-05-19.md` |
| Technischer Datenfluss | `docs/FINANCE_DATENFLUSS_ANDREAS_2026-06-08.md` |
| Audit-CSV, Sales_All und Pruefbuch | `docs/FINANCE_DASHBOARD_PROZESSABLAUF_2026-06-30.md` |
| Waehrungs-/Kursworkflow | `docs/FINANCE_KURS_WORKFLOW_2026-06-09.md` |
| Budget-CHF-Fragen | `docs/FINANCE_BUDGET_CHF_FRAGEN_FINANZCHEF_2026-06-15.md` |
| Gruppenmarge | `docs/FINANCE_GRUPPENMARGE_2026-06-16.md` |
| Anzeigepruefung Finance (Pruefbuch-Marge, Statusfarbe) | `docs/FINANCE_ANZEIGE_PRUEFUNG_2026-08-06.md` |
| Gruppenmarge-Prozessgrafik | `docs/FINANCE_GRUPPENMARGE_PROZESSFLUSS_2026-07-27.svg` |
| Standardkosten CH/AT/DE | `docs/FINANCE_STANDARDKOSTEN_2026-07-14.md` |
| WAVWR/NETWR_HC SAP-Spezifikation | `docs/FINANCE_VBRP_WAVWR_SPEZ_2026-07-16.md` |
| CH/AT-Journal SAP-Spezifikation | `docs/FINANCE_JOURNAL_SAP_ODATA_SPEZ_2026-07-14.md` |
| B1-Journal-Import | `docs/FINANCE_B1_JOURNAL_IMPORT_2026-07-14.md` |
| B1-Konnektoren | `docs/FINANCE_SAP_B1_KONNEKTOREN_ANDREAS_2026-07-01.md` |
| Excel-Nachweis | `docs/FINANCE_DASHBOARD_NACHWEIS_2026-06-17.md` |
| Daten-Heartbeat | `docs/CODEX_ANWEISUNG_FINANCE_DATEN_HEARTBEAT_2026-07-13.md` |
| Standardkosten-Arbeitsnotiz | `docs/FINANCE_STANDARDKOSTEN_ARBEITSNOTIZ_2026-07-17.md` |
| Sitzung Andreas / TR AG-IT-IN | `docs/FINANCE_STANDARDKOSTEN_SITZUNG_ANDREAS_2026-07-27.md` |
| Supplier-Luecke | `docs/FINANCE_SUPPLIER_LUECKE_ANALYSE_2026-07-28.md` |
| Standort-Datenluecken | `docs/FINANCE_DATENLUECKEN_ANDREAS_2026-07-28.md` |
| CH/AT-2026 Root Cause | `docs/FINANCE_CHAT_2026_LUECKE_ROOTCAUSE_2026-07-28.md` |
| Issue-Log | `docs/FINANCE_ISSUE_LOG_ANDREAS_2026-07-28.md` |
| UK-/ES-Backfill | `docs/FINANCE_BACKFILL_UK_ES_2026-07-28.md` |
| Standort-Mailtexte/-status | `docs/FINANCE_FELDLUECKEN_MAILS_2026-07-31.md` |
| IT-Spezialfall | `docs/FINANCE_IT_VORGEHEN_2026-05-18.md` |
| UK-Spezialfall | `docs/FINANCE_UK_QUELLE_KORREKTUR_2026-05-18.md` |
| Spanien/Sage | `SAGE_SPAIN_EXPORT_2026-05-05.md` |

## Einkauf, Import, HR und Plattform

| Bedarf | Datei |
| --- | --- |
| Einkaufs-Hauptdoku/Historie | `docs/PURCHASING_DASHBOARD_2026-06-05.md` |
| Einkaufs-Formelkorrekturen | `docs/PURCHASING_DASHBOARD_KORREKTUREN_2026-07-06.md` |
| Marco-Umsetzungsplan | `docs/PURCHASING_DASHBOARD_UMSETZUNGSPLAN_MARCO_2026-07-09.md` |
| Marco-Review | `docs/PURCHASING_DASHBOARD_REVIEW_MARCO_2026-07-10.md` |
| Einkaufssitzung/Wuensche | `docs/PURCHASING_DASHBOARD_WUENSCHE_EINKAUF_2026-07-23.md` |
| Einkauf-Lokalisierung und Projektsuite | `docs/EINKAUF_LOKALISIERUNG_PROJEKTSUITE_2026-08-01.md` |
| Logistik-Stuecklisten-Dashboard | `docs/LOGISTIK_STUECKLISTEN_DASHBOARD_2026-08-01.md` |
| ZLO03/Materialverwendung | `docs/abap/README_LZCODE_WEBSERVICE.md` |
| Manual-Import-Details | `docs/MANUAL_IMPORT_DELTA_STAND_2026-05-21.md` |
| Spanien-rclone | `docs/SAGE_SPAIN_RCLONE_UPLOAD_GUIDE_2026-06-03.md` |
| Spanien Buchungsdatum/`PostingDate` fehlt | `docs/FINANCE_ES_BUCHUNGSDATUM_2026-08-03.md` |
| Export-SQL DE (unsere Query, Alphaplan) | `AlphaplanExportPackage/scripte/alphaplanExport.ps1`, `alphaplandeltaexport.ps1`; Einrichtung `AlphaplanExportPackage/scripte/ANLEITUNG_KORREKTUR_2026-06-24.md` |
| Export-SQL ES (unsere Query, Sage) | `SageSpainExportPackage/SageSpainFinalExportPackage/Export-SageSpainSalesCsv.ps1` und `Run-SpainRangeExportAndUpload-AllInOne.ps1` (Query steht ZWEIMAL) |
| Schema-Discovery Standort-DBs | `AlphaplanExportPackage/Run-AlphaplanDiscoveryAndUpload.ps1`, `AlphaplanExportPackage/scripte/alphaplan.ps1`; Sage-Auszug `obj/candidate_objects.csv` (bei 80 Objekten abgeschnitten) |
| Alphaplan Discovery | `docs/ALPHAPLAN_DISCOVERY_EXPORTER_GUIDE_2026-06-08.md` |
| Alphaplan SQL/rclone | `docs/ALPHAPLAN_SQL_RCLONE_KONZEPT_DE_2026-06-08.md` |
| HR-KPI-Nachdoku | `docs/HR_KPI_NACHDOKU_2026-05-13.md` |
| HR-KPI-Korrekturen | `docs/HR_KPI_KORREKTUREN_2026-07-06.md` |
| HR-KPI-Fachpruefung | `docs/HR_KPI_PRUEFUNG_SWISS_BEST_PRACTICES.md` |
| IIS-/Publish-Handoff | `docs/DEPLOYMENT_IIS_HANDOFF_2026-05-19.md` |
| Admin/Startseite | `docs/ADMIN_BEREICH_STARTSEITE_2026-05-21.md` |
| Architekturdiagramme | `docs/PROGRAMM_DIAGRAMME.md` |
| Produktsparten-Mapping | `docs/PRODUCT_SPARTEN_MAPPING_2026-05-27.md` |
| ABAP-Produktsparten-Provider | `docs/abap/README_PRODSPARTE.md` |
| Historischer lokaler Notbetrieb | `docs/LOCAL_DEV_SERVER_UEBERGANG_2026-05-21.md` |

## Live-Werkzeuge

### SAP ERP: SapProbe

- Ort: `.tmp_sap_probe/`; Build-Ziel
  `bin/x86/Release/net48/SapProbe.exe` ist nicht in Git.
- Default: `travt762.sap.trafag.com`, SID `T76`, Client 100. Produktion
  `travp762` nur bewusst per `--ashost` waehlen.
- Start: `.tmp_sap_probe/RunSapProbeInteractive.ps1 <befehl>`.
- Passwort interaktiv oder ueber `SAP_NCO_PASSWORD`/`SAP_T76_PASSWORD`; nie in
  Doku oder Git schreiben.

| Befehl | Zweck |
| --- | --- |
| `system-info` | Verbindung/System pruefen |
| `table-read` | Tabelleninhalte lesen |
| `table-fields` / `field-exists` | DDIC-Felder und Datenelemente pruefen |
| `function-info` / `function-search` / `rfc-call` | RFC-Bausteine untersuchen/aufrufen |
| `abap-read` / `abap-check` | ABAP lesen bzw. im System syntaxpruefen |
| `abap-write` / `abap-activate` | Schreiben/Aktivieren, nur mit `--confirm-write` |

Grenzen: DDIC-Strukturen/-Tabellen bleiben manuell in SE11, globale Klassen in
SE24/ADT und Gateway-Modell/EntitySets in SEGW. SapProbe darf SAP-Fakten
verifizieren, ersetzt diese Entwicklungsoberflaechen aber nicht.

### SAP B1/HANA: HanaQ

- Lokaler, derzeit nicht versionierter Helfer: `.tmp_tools/HanaQ/`.
- Build benoetigt SAP-HANA-.NET-Client unter
  `C:\Program Files\sap\hdbclient\dotnetcore\v2.1\`.
- Aufruf: `.tmp_tools/HanaQ/bin/Debug/net8.0/HanaQ.exe <TSC> <sqlFile>
  [dbPath]`.
- Verbindung, Schema und Credentials werden aus `Sites`,
  `SourceSystemDefinitions` und `HanaServers` der lokalen SQLite-Kopie
  aufgeloest; keine Passwoerter in SQL-Dateien schreiben.
- Guardrail: nur `SELECT`/`WITH`; Platzhalter `{schema}` bzw. `{SCHEMA}`.
- Prozent-/Fuellgradmessungen immer auf die fachliche Grundgesamtheit filtern,
  z. B. aktive Lagerartikel statt blind alle `OITM`-Zeilen.
- `LIKE 'U_%'` matcht wegen des Platzhalter-Unterstrichs auch `UserSign`/`UserText`:
  fuer UDF-Spalten `LIKE 'U\_%' ESCAPE '\'` schreiben. Schemavergleiche in
  `SYS.TABLE_COLUMNS` case-insensitiv (`UPPER(...) = UPPER(...)`), weil Schemanamen je
  Standort unterschiedlich geschrieben sind (`TRAFAG_LIVE` vs. `it01_p`).

### Standorte, die nur der Server erreicht: Server-Analyse

- Zweck: lesende Abfragen gegen Standortsysteme, die der Entwicklungsrechner nicht
  erreicht (Indiens HANA `20.197.20.60:30015`).
- **Ausgefuehrt wird von der laufenden Anwendung**, nicht von einem Werkzeug auf dem
  Server: `Services/ServerAnalysisBackgroundService.cs` prueft alle 20 Sekunden, ob
  im Anwendungsordner `_analysis/run.trigger` liegt, arbeitet dann
  `_analysis/sql/*.sql` ab und schreibt nach `_analysis/results`. Grund: auf
  `tragvapp401` sind `Invoke-Command`, `schtasks` und `C$` gesperrt und es gibt
  keinen RDP-Zugang; der Share ist aber beschreibbar. Der DNS-Name
  `trch-webapp-bidashboard` ist ein CNAME auf `tragvapp401` — mit dem Aliasnamen
  scheitert schon Kerberos.
- Fernbedienung dazu: **`docs/analyse/Run-ServerAnalysis.ps1`** `-Action Run | Fetch | Clean`
  (versioniert, nicht unter `.tmp_tools/`). Abfragen in `docs/analyse/sql/`, Belege der
  bisherigen Laeufe in `docs/analyse/ergebnisse/`.
- Regeln fuer SQL-Dateien: Dateiname beginnt mit dem TSC (`TRIN__01_...` ->
  `TRIN`), Statementtrenner ist eine Zeile ab `;;`, nur `SELECT`/`WITH`
  (`Services/ReadOnlySqlGuard.cs`), Platzhalter `{schema}`/`{SCHEMA}`, maximal 500
  Zeilen je Statement. **Zwei Bindestriche als Zeichenkettenliteral sind nicht
  moeglich** — sie gelten als Kommentar und der Guardrail lehnt ab.
- Hintergrund und Befunde: `docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md`.

## Suchindex

| Suchbegriffe | Ziel |
| --- | --- |
| `Finance Summary`, Soll/Ist, `FinanceRuleEngine` | `docs/rag/FINANCE.md` |
| Formel, Umrechnung, Marge, Magnetic Sense, GFS | `docs/rag/FINANCE_FORMELN.md` |
| Audit-CSV, Sales_All, Pruefbuch, Excel-Nachweis | `docs/FINANCE_DASHBOARD_PROZESSABLAUF_2026-06-30.md` |
| Gruppenmarge, STPRS, Kostenwaehrung | `docs/FINANCE_GRUPPENMARGE_2026-06-16.md` |
| WAVWR, NETWR_HC, Faktor 100 | `docs/FINANCE_VBRP_WAVWR_SPEZ_2026-07-16.md` |
| Supplier leer, Lieferant unklar | `docs/AKTUELLER_LIVEDATEN_STAND_2026-07-31.md` |
| Journal, BKPF/BSEG, OJDT/JDT1 | Finance-Journal-Dateien oben |
| TRUK, UK-Delta, Manual Excel | `docs/rag/MANUAL_IMPORT.md` |
| TRDE, Alphaplan | `docs/rag/MANUAL_IMPORT.md` |
| Einkauf, EKKO/EKPO/EKET, Spend | `docs/rag/PURCHASING.md` |
| Drilldown, Warengruppe, MARA, ABC/XYZ | `docs/rag/PURCHASING.md` |
| Albanisch, Tuerkisch, Klingonisch, Sprache, Projekte | `docs/EINKAUF_LOKALISIERUNG_PROJEKTSUITE_2026-08-01.md` |
| HR KPI, Rexx, Austritte, Absenzen | `docs/rag/HR_KPI.md` |
| IIS, Publish, BiDashboard, Firewall | `docs/rag/DEPLOYMENT.md` |
| Logistik, Stuecklisten-Dashboard, Top-Down, Bottom-Up, Bestand, LZ-Code | `docs/LOGISTIK_STUECKLISTEN_DASHBOARD_2026-08-01.md` |
| ZLO03, MaterialUsageSet, Gateway, ABAP | `docs/abap/README_LZCODE_WEBSERVICE.md` |
| Ansprechpartner, Mailadresse, Standortempfaenger | `docs/ANSPRECHPARTNER.md` |

## Historie und Wiederherstellung

- Dokumentstatus: `docs/MD_DOKUMENTENSTATUS_2026-05-20.md`
- Kanonische Detailhistorie: `docs/raw_md_archive/HISTORY_CANONICAL.md.raw`
- Originaldateien: `docs/raw_md_archive/original_history_raws.zip`
- Lastchange bis 2026-07-12:
  `docs/raw_md_archive/LASTCHANGE_ARCHIV_bis_2026-07-12.md`
- Lastchange 2026-07-13 bis 2026-07-30:
  `docs/raw_md_archive/LASTCHANGE_ARCHIV_2026-07-13_bis_2026-07-30.md`
- Vollstaendiger Router vor dieser Aufteilung:
  `docs/raw_md_archive/RAG_ROUTER_ARCHIV_2026-07-31.md`
