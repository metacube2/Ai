# RAG Router

Stand: 2026-07-17

Zweck: Diese Datei zuerst laden. Danach nur die Dateien aus dem passenden Themenblock laden.

## Lade-Regel

1. Immer nur diese Router-Datei zuerst lesen.
2. `lastchange.md` (Repo-Root) laden: letzte ~7 Tage + offene Punkte, bewusst kompakt.
3. Thema bestimmen; die passende Kurzdatei aus `docs/rag/` laden.
4. Rohquellen nur laden, wenn Details, alte Zahlen, Codepfade, Mailtexte oder Audit gefragt sind.
5. Arbeitsregeln/Grenzen (Tests, Doku-Pflicht, fachliche Verantwortung): `persona.md` (Repo-Root).

## Themen

| Thema | Wann laden | Standard laden |
| --- | --- | --- |
| Aktueller Stand | Projektstatus, letzte Aenderungen, offene Punkte | `docs/rag/PROJECT.md` |
| Finance Cockpit | Soll/Ist, Finance Summary, Regeln, Laenderlogik | `docs/rag/FINANCE.md` |
| Finance Prozess / Excel-Nachweis | Dashboard-Datenfluss, Audit-CSV, Sales_All, Finance Pruefbuch, Andreas-Nachvollziehbarkeit | `docs/FINANCE_DASHBOARD_PROZESSABLAUF_2026-06-30.md` |
| Finance Spezialfaelle | IT, UK, ES, Abweichungen | `docs/rag/FINANCE.md` |
| Manual Import | UK-Deltas, Spanien Basis+Range, DE Alphaplan Full+Delta, Importprozess | `docs/rag/MANUAL_IMPORT.md` |
| HR KPI | HR Dashboard, Formeln, Datenqualitaet, Anwenderstand | `docs/rag/HR_KPI.md` |
| Deployment/IIS | Publish, Server, BiDashboard, TLS, lokaler Uebergang | `docs/rag/DEPLOYMENT.md` |
| Admin/Startseite | Admin Login, Sessions, Landing Page | `docs/rag/ADMIN.md` |
| Architektur | Systemuebersicht, Diagramme, technische Einordnung | `docs/rag/ARCHITECTURE.md` |
| Produktmapping | Group Sales Report, Produkthierarchie, Produktfamilie, Produktsparte | `docs/rag/PRODUCT_MAPPING.md` |
| Einkauf | Einkaufsdashboard, EKKO/EKPO/EKET, Lieferanten, offene Bestellungen/Kontrakte, Spend, Drilldown | `docs/rag/PURCHASING.md` |
| 180-Tage-Roadmap Ingo | Management-Doku, Aufgaben Ingo, Sales/Data-Lake, HR/Einkauf, Abhaengigkeiten | `docs/INGO_TODOS_180_TAGE_2026-06-18.md` |

## Rohquellen Nur Bei Bedarf

| Datei | Nur laden fuer |
| --- | --- |
| `docs/raw_md_archive/HISTORY_CANONICAL.md.raw` | kanonische Detailhistorie mit Quellenangaben |
| `docs/raw_md_archive/original_history_raws.zip` | exakte Originaldateien nur zur Wiederherstellung, nicht fuer RAG laden |
| `docs/MD_DOKUMENTENSTATUS_2026-05-20.md` | Einordnung alter Dokumente |
| `docs/FINANCE_ENTSCHEIDE.md` | Finance-Entscheide im Detail |
| `docs/FINANCE_SCHULUNG_FINANZ_2026-06-11.md` | aktuelle Finance-Schulung, Prozessgrafiken, Audit-CSV und Waehrungsfluss |
| `docs/FINANCE_BERECHNUNGSFORMELN_LAENDER_2026-05-19.md` | Formeln pro Land |
| `docs/FINANCE_DATENFLUSS_ANDREAS_2026-06-08.md` | technischer Finance-Datenfluss inklusive Audit-CSV |
| `docs/FINANCE_DASHBOARD_PROZESSABLAUF_2026-06-30.md` | Prozessablauf Finance Dashboard, operative Audit-CSV-Quelle, Rolle von `Sales_All`, Finance Pruefbuch |
| `docs/FINANCE_KURS_WORKFLOW_2026-06-09.md` | isolierter Kurs-/Umrechnungsworkflow vom Land bis Dashboard |
| `docs/FINANCE_BUDGET_CHF_FRAGEN_FINANZCHEF_2026-06-15.md` | offene Finance-Fragen fuer Budget-CHF-Spalten |
| `docs/FINANCE_BUDGET_CHF_MULTIPLE_CHOICE_2026-06-16.docx` | Multiple-Choice-Entscheidungsbogen fuer Finanzchef |
| `docs/INGO_TODOS_180_TAGE_2026-06-18.md` | editierbare Quelle fuer die 180-Tage-Roadmap von Ingo |
| `docs/INGO_TODOS_180_TAGE_2026-06-18.docx` | Word-Fassung der 180-Tage-Roadmap fuer Management-/IT-Dokumentation |
| `docs/MANUAL_IMPORT_DELTA_STAND_2026-05-21.md` | Manual-Import-Details |
| `docs/HR_KPI_NACHDOKU_2026-05-13.md` | HR-KPI-Details |
| `docs/DEPLOYMENT_IIS_HANDOFF_2026-05-19.md` | IIS-/Publish-Details |
| `docs/LOCAL_DEV_SERVER_UEBERGANG_2026-05-21.md` | lokaler Server im Detail |
| `docs/ADMIN_BEREICH_STARTSEITE_2026-05-21.md` | Admin-/Landing-Details |
| `docs/PRODUCT_SPARTEN_MAPPING_2026-05-27.md` | Produktsparten-Mapping im Detail |
| `docs/FINANCE_GRUPPENMARGE_2026-06-16.md` | Gruppenmarge-Fachlogik, Andreas-Entscheide, Kostenwaehrungsschalter (Entscheid D) im Detail |
| `docs/FINANCE_STANDARDKOSTEN_2026-07-14.md` | Standardkosten-/MBEW-STPRS-Anbindung CH/AT und DE im Detail, inkl. Nachtrag 2026-07-16 (haengender mbewSet-Import, Kostenquoten-Verifikation) |
| `docs/FINANCE_VBRP_WAVWR_SPEZ_2026-07-16.md` | SAP-OData-Spezifikation `Wavwr`-Feld in `FinanzdataSchweizOeSet` (Ersatz fuer haengenden mbewSet-Scan, CH/AT-Kostenbasis) |
| `docs/FINANCE_JOURNAL_SAP_ODATA_SPEZ_2026-07-14.md` | SAP-OData-Spezifikation `FinanzJournalSet` fuer CH/AT-Journal-Import |
| `docs/FINANCE_B1_JOURNAL_IMPORT_2026-07-14.md` | B1-Journal-Import (Hauptbuch) Feldmapping im Detail |
| `docs/FINANCE_SAP_B1_KONNEKTOREN_ANDREAS_2026-07-01.md` | SAP-B1-Konnektoren-Uebersicht fuer Andreas |
| `docs/FINANCE_DASHBOARD_NACHWEIS_2026-06-17.md` | Aufbau/Formeln der `Finance_Dashboard_Nachweis_*.xlsx` im Detail |
| `docs/CODEX_ANWEISUNG_FINANCE_DATEN_HEARTBEAT_2026-07-13.md` | Umsetzungsanweisung Daten-Heartbeat im Detail |
| `docs/PURCHASING_DASHBOARD_2026-06-05.md` | Einkaufs-Hauptdoku mit allen Nachtraegen (Historie, Reviews, Fixes) |
| `docs/PURCHASING_DASHBOARD_KORREKTUREN_2026-07-06.md` | Einkaufs-Formel-/Logik-Korrekturen 2026-07-06 im Detail |
| `docs/PURCHASING_DASHBOARD_VORBEREITUNG_INGO_2026-07-09.md` | Vorbereitung Einkauf-Review durch Ingo |
| `docs/PURCHASING_DASHBOARD_UMSETZUNGSPLAN_MARCO_2026-07-09.md` | Umsetzungsplan aus Marcos Einkauf-Review |
| `docs/PURCHASING_DASHBOARD_REVIEW_MARCO_2026-07-10.md` | Marcos Einkauf-Review im Detail, inkl. travp762-Feldrisiko |
| `docs/FINANCE_STANDARDKOSTEN_ARBEITSNOTIZ_2026-07-17.md` | Arbeitsnotiz Standardkosten/Margenreporting mit Andreas (Stichproben, fix/variabel-Frage) |
| `docs/FINANCE_IT_VORGEHEN_2026-05-18.md` | IT/Italien-Finance-Vorgehen im Detail |
| `docs/FINANCE_UK_QUELLE_KORREKTUR_2026-05-18.md` | UK-Quellkorrektur (Sage) im Detail |
| `SAGE_SPAIN_EXPORT_2026-05-05.md` (Repo-Root) | Spanien-Sage-Export im Detail |
| `docs/SAGE_SPAIN_RCLONE_UPLOAD_GUIDE_2026-06-03.md` | Spanien-rclone-Upload-Anleitung (All-in-one) |
| `docs/HR_KPI_KORREKTUREN_2026-07-06.md` | HR-KPI-Formel-/Logik-Korrekturen 2026-07-06 im Detail |
| `docs/HR_KPI_PRUEFUNG_SWISS_BEST_PRACTICES.md` | HR-KPI-Pruefung gegen Schweizer Best Practices |
| `docs/ALPHAPLAN_DISCOVERY_EXPORTER_GUIDE_2026-06-08.md` | Alphaplan-Discovery-Exporter (DE-Server-Seite) |
| `docs/ALPHAPLAN_SQL_RCLONE_KONZEPT_DE_2026-06-08.md` | Alphaplan-SQL/rclone-Konzept Deutschland |
| `AlphaplanExportPackage/CLAUDE_ODATA_DASHBOARD_KONTEXT.md` | Kontextdatei im Alphaplan-Exportpaket (DE-Server) |
| `AlphaplanExportPackage/scripte/ANLEITUNG_KORREKTUR_2026-06-24.md` | Korrektur-Anleitung Alphaplan-Exportskripte |
| `docs/REQUIREMENTS.md` | Anforderungsuebersicht/Backlog |
| `docs/PROGRAMM_DIAGRAMME.md` | Programm-/Ablaufdiagramme |
| `docs/abap/README_FIN_ANALYSE_STPRS_JOURNAL.md` | ABAP-Analysereport STPRS/Journal (CH/AT-Nachweise) |
| `docs/abap/README_PRODSPARTE.md` | ABAP-Produktsparten-Provider |
| `spartenlogic/UEBERGABE_PRODUKTSPARTEN_ZUORDNUNG.md` | Uebergabe-Doku Produktsparten-Zuordnung (Analyse-Historie) |
| `docs/CCUSAGE_INSTALL_ANLEITUNG.md` | Tooling (ccusage), nicht projektfachlich |
| `docs/raw_md_archive/LASTCHANGE_ARCHIV_bis_2026-07-12.md` | archivierte `lastchange.md`-Eintraege bis 2026-07-12 (verbatim) |
| `docs/raw_md_archive/RAG_KURZDATEIEN_ARCHIV_ueberholte_eintraege.md` | archivierte, durch neuere ersetzte Kurzstand-Eintraege aus FINANCE/PROJECT/DEPLOYMENT (verbatim) |

## Entfernt 2026-07-15 / nur noch im Raw-Archiv

Acht Dateien wurden am 2026-07-15 aus der aktiven Struktur entfernt (reine Pointer-Stubs
oder inhaltlich vollstaendig durch spaetere Entscheide/Features ersetzt: ES-Referenz,
DE-Jahr, IC-Diagnose, Gruppenmarge sind laengst umgesetzt). Volltext liegt in
`docs/raw_md_archive/HISTORY_CANONICAL.md.raw` (Bloecke H324-H331); aktueller Stand steht
in `docs/rag/FINANCE.md` und `docs/FINANCE_GRUPPENMARGE_2026-06-16.md`. Details/Gruende:
`docs/MD_DOKUMENTENSTATUS_2026-05-20.md` Abschnitt „Archiviert / aus aktiver Doku entfernt".

| Entfernte Datei | Ersetzt durch |
| --- | --- |
| `HANDOFF_2026-04-15.md`, `NEXT_STEPS_2026-04-15.md`, `LLM_SYSTEM_GUIDE.md` (root) | diese Router-Datei selbst |
| `entscheide.md` (root) | `docs/FINANCE_ENTSCHEIDE.md` |
| `docs/FINANCE_STATUS_OFFENE_PUNKTE_2026-06-01.md` | `docs/rag/FINANCE.md` „Offene Fachpunkte" |
| `docs/FINANCE_MEMO_ANDREAS_2026-06-01.md` | `docs/rag/FINANCE.md`, `docs/FINANCE_ENTSCHEIDE.md` |
| `docs/FINANCE_SITZUNGSPUNKTE_ANDREAS_2026-06-02.md` | `docs/rag/FINANCE.md`, `docs/FINANCE_ENTSCHEIDE.md` |
| `docs/HANDOFF_2026-06-16.md` | `docs/rag/PROJECT.md` |

## Suchwoerter

| Suchwort | Thema |
| --- | --- |
| `Finance Summary`, `Soll/Ist`, `check.xlsx`, `FinanceRuleEngine` | Finance Cockpit |
| `Finance Pruefbuch`, `Pruefbuch`, `Sales_All`, `Sales_ProcessedMergeInput`, `Andreas`, `Excel-Nachweis`, `Dashboard-Quelle` | `docs/FINANCE_DASHBOARD_PROZESSABLAUF_2026-06-30.md` |
| `Schulung`, `Training`, `Audit-CSV`, `Sales_ProcessedMergeInput`, `Auswertungsquelle`, `Wirtschaftspruefung` | `docs/FINANCE_SCHULUNG_FINANZ_2026-06-11.md` |
| `Wechselkurs`, `Umrechnungskurs`, `CurrencyExchangeRates`, `DocumentRate`, `ConvertCurrency`, `Anzeige-Waehrung` | `docs/FINANCE_KURS_WORKFLOW_2026-06-09.md` |
| `Budget-CHF`, `Budgetkurs`, `Net Sales Actual CHF Budget`, `Finanzchef`, `Multiple Choice` | `docs/FINANCE_BUDGET_CHF_FRAGEN_FINANZCHEF_2026-06-15.md` / `docs/FINANCE_BUDGET_CHF_MULTIPLE_CHOICE_2026-06-16.docx` |
| `180 Tage`, `Roadmap`, `Ingo`, `Sales Cockpit`, `Data-Lake`, `Einkaufs Dashboard`, `HR Dashboard`, `Management-Doku` | `docs/INGO_TODOS_180_TAGE_2026-06-18.md` |
| `TRUK`, `UK_B1`, `Delta`, `Manual Excel` | Manual Import / Finance Spezialfaelle |
| `TRDE`, `Alphaplan`, `invoice_headers`, `invoice_lines`, `BelegePositionenID`, `NettoPreisGesamt`, `ArtikelNummer`, `MATNR` | Finance Cockpit / Manual Import |
| `TRSE`, `Spain`, `Sage`, `ImporteNeto` | Finance Spezialfaelle |
| `TRIN`, `Indien`, `India`, `SAGE`, `20.197.20.60`, `TRAFAGCONTROLS` | Deployment/IIS oder Finance Spezialfaelle |
| `Spain rclone`, `Spanien SharePoint`, `Run-SpainRangeExportAndUpload-AllInOne`, `trafag-bi` | `docs/SAGE_SPAIN_RCLONE_UPLOAD_GUIDE_2026-06-03.md` |
| `3D Datenanalyse`, `Experten`, `Schnelluebersicht`, `Simulation`, `Wechselkurs-Schieberegler` | Finance Cockpit |
| `HR KPI`, `Rexx`, `Austritte`, `Absenzen` | HR KPI |
| `IIS`, `BiDashboard`, `Publish`, `TLS`, `Client certificate` | Deployment/IIS |
| `Upgreat`, `Firewall`, `Freigabe`, `10.120.1.17`, `30015`, `8000` | Deployment/IIS |
| `Admin Bereich`, `AdminAccess`, `LandingPage` | Admin/Startseite |
| `Group Sales Report`, `Produkthierarchie`, `Produktfamilie`, `Produktsparte`, `Z.Prodh` | Produktmapping |
| `Gruppenmarge`, `Standardkosten`, `STPRS`, `MBEW`, `Kostenwaehrung`, `GroupMarginCostCurrencyMode` | `docs/FINANCE_GRUPPENMARGE_2026-06-16.md` / `docs/FINANCE_STANDARDKOSTEN_2026-07-14.md` |
| `WAVWR`, `VBRP`, `mbewSet haengt`, `Standardpreis-Read`, `GroupStandardCosts` | `docs/FINANCE_VBRP_WAVWR_SPEZ_2026-07-16.md` / `docs/FINANCE_STANDARDKOSTEN_2026-07-14.md` Nachtrag 2026-07-16 |
| `Journal Import`, `Hauptbuch`, `BKPF`, `BSEG`, `OJDT`, `JDT1`, `FinancialJournalEntries`, `FinanzJournalSet` | `docs/FINANCE_B1_JOURNAL_IMPORT_2026-07-14.md` / `docs/FINANCE_JOURNAL_SAP_ODATA_SPEZ_2026-07-14.md` |
| `Daten-Heartbeat`, `Heartbeat`, `ExportLogs`, `Datenkontinuitaet` | `docs/CODEX_ANWEISUNG_FINANCE_DATEN_HEARTBEAT_2026-07-13.md` / Finance Cockpit |
| `EKKO`, `EKPO`, `EKET`, `Einkauf`, `Lieferanten`, `offene Bestellungen`, `Kontrakte`, `Spend` | Einkauf |
| `Drilldown`, `Warengruppe`, `MaraMatkl`, `maracalc`, `MARA001Set`, `Mstae`, `LFA1`, `SupplierName`, `ABC`, `XYZ`, `MAABC` | Einkauf |
| `Deckungsbeitrag`, `DB %`, `StandardCostVariable`, `StandardCostFixed`, `fix/variabel`, `ContributionMarginCalculator` | Finance Cockpit / `docs/FINANCE_STANDARDKOSTEN_ARBEITSNOTIZ_2026-07-17.md` |
| `NETWR_HC`, `Kurrf`, `Faktor 100`, `CorrectHouseCurrencyScaling` | `docs/FINANCE_VBRP_WAVWR_SPEZ_2026-07-16.md` |
| `Supplier-Felder leer`, `Lieferant unklar`, `GroupMarginSupplierClassifier` | Finance Cockpit / `docs/FINANCE_GRUPPENMARGE_2026-06-16.md` |
| `travt762`, `travp762`, `Test-Server`, `SapServiceUrl` | Finance Cockpit + Einkauf (offener Punkt in beiden) |
