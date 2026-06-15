# RAG Router

Stand: 2026-06-12

Zweck: Diese Datei zuerst laden. Danach nur die Dateien aus dem passenden Themenblock laden.

## Lade-Regel

1. Immer nur diese Router-Datei zuerst lesen.
2. Thema bestimmen.
3. Zuerst nur die passende Kurzdatei aus `docs/rag/` laden.
4. Rohquellen nur laden, wenn Details, alte Zahlen, Codepfade, Mailtexte oder Audit gefragt sind.

## Themen

| Thema | Wann laden | Standard laden |
| --- | --- | --- |
| Aktueller Stand | Projektstatus, letzte Aenderungen, offene Punkte | `docs/rag/PROJECT.md` |
| Finance Cockpit | Soll/Ist, Finance Summary, Regeln, Laenderlogik | `docs/rag/FINANCE.md` |
| Finance Spezialfaelle | IT, UK, ES, Abweichungen | `docs/rag/FINANCE.md` |
| Manual Import | UK-Deltas, Spanien Basis+Range, DE Alphaplan Full+Delta, Importprozess | `docs/rag/MANUAL_IMPORT.md` |
| HR KPI | HR Dashboard, Formeln, Datenqualitaet, Anwenderstand | `docs/rag/HR_KPI.md` |
| Deployment/IIS | Publish, Server, BiDashboard, TLS, lokaler Uebergang | `docs/rag/DEPLOYMENT.md` |
| Admin/Startseite | Admin Login, Sessions, Landing Page | `docs/rag/ADMIN.md` |
| Architektur | Systemuebersicht, Diagramme, technische Einordnung | `docs/rag/ARCHITECTURE.md` |
| Produktmapping | Group Sales Report, Produkthierarchie, Produktfamilie, Produktsparte | `docs/rag/PRODUCT_MAPPING.md` |

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
| `docs/FINANCE_KURS_WORKFLOW_2026-06-09.md` | isolierter Kurs-/Umrechnungsworkflow vom Land bis Dashboard |
| `docs/MANUAL_IMPORT_DELTA_STAND_2026-05-21.md` | Manual-Import-Details |
| `docs/HR_KPI_NACHDOKU_2026-05-13.md` | HR-KPI-Details |
| `docs/DEPLOYMENT_IIS_HANDOFF_2026-05-19.md` | IIS-/Publish-Details |
| `docs/LOCAL_DEV_SERVER_UEBERGANG_2026-05-21.md` | lokaler Server im Detail |
| `docs/ADMIN_BEREICH_STARTSEITE_2026-05-21.md` | Admin-/Landing-Details |
| `docs/PRODUCT_SPARTEN_MAPPING_2026-05-27.md` | Produktsparten-Mapping im Detail |

## Suchwoerter

| Suchwort | Thema |
| --- | --- |
| `Finance Summary`, `Soll/Ist`, `check.xlsx`, `FinanceRuleEngine` | Finance Cockpit |
| `Schulung`, `Training`, `Audit-CSV`, `Sales_ProcessedMergeInput`, `Auswertungsquelle`, `Wirtschaftspruefung` | `docs/FINANCE_SCHULUNG_FINANZ_2026-06-11.md` |
| `Wechselkurs`, `Umrechnungskurs`, `CurrencyExchangeRates`, `DocumentRate`, `ConvertCurrency`, `Anzeige-Waehrung` | `docs/FINANCE_KURS_WORKFLOW_2026-06-09.md` |
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
