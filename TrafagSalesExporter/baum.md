# Dokumentationsbaum — Vollstaendigkeitsindex

Stand: 2026-08-17

**Diese Datei ist zur Pruefung da, nicht zum Lesen einer Aufgabe.** Fuer eine konkrete
Aufgabe gilt `router.md` -> Unterrouter -> Detaildatei. Hier steht jede Markdown-Datei des
Repositorys genau einmal, damit nichts unauffindbar wird.

Nicht enthalten: `whisper/python/Lib/site-packages/**` (33 Vendor-Lizenz- und
Vorlagendateien fremder Pakete). Diese sind Fremdcode und kein Projektwissen.

## Einstieg

| Datei | Rolle |
| --- | --- |
| `router.md` | einziger globaler Einstieg, Vorrangregeln und Themenaeste |
| `baum.md` | diese Datei, Vollstaendigkeitsindex |
| `AGENTS.md` | Einstieg fuer Codex, verweist auf `router.md` |
| `CLAUDE.md` | Einstieg fuer Claude Code, verweist auf `router.md` |
| `persona.md` | Arbeitsregeln, Tests, fachliche Grenzen |
| `lastchange.md` | aktueller Aenderungsstand |

## Unterrouter

| Datei | Ast |
| --- | --- |
| `docs/router/finance.md` | Finance |
| `docs/router/standortdaten.md` | Standortdaten und Exporte |
| `docs/router/einkauf.md` | Einkauf und Logistik |
| `docs/router/hr.md` | HR |
| `docs/router/plattform.md` | Plattform, Deployment, Werkzeuge |
| `docs/router/sap.md` | SAP und ABAP |
| `docs/router/projekt.md` | Projekt und Koordination |

## Finance

| Datei | Rolle |
| --- | --- |
| `docs/rag/FINANCE.md` | Kurzstand, Einstieg |
| `docs/rag/FINANCE_FORMELN.md` | Zeilenmechanik, Umrechnung, Marge |
| `docs/AKTUELLER_LIVEDATEN_STAND_2026-07-31.md` | zuletzt gepruefte Live-Zahlen, hat Vorrang |
| `docs/FINANCE_ENTSCHEIDE.md` | Fachentscheide Net Sales Actuals |
| `docs/FINANCE_BERECHNUNGSFORMELN_LAENDER_2026-05-19.md` | Detailregeln je Land |
| `docs/FINANCE_DASHBOARD_PROZESSABLAUF_2026-06-30.md` | Prozess, Audit-CSV, Sales_All, Pruefbuch |
| `docs/FINANCE_DATENFLUSS_ANDREAS_2026-06-08.md` | technischer Datenfluss |
| `docs/FINANCE_KURS_WORKFLOW_2026-06-09.md` | Waehrung und Kurse |
| `docs/FINANCE_GRUPPENMARGE_2026-06-16.md` | Gruppenmarge, Fachlogik |
| `docs/FINANCE_STANDARDKOSTEN.md` | **zusammengefuehrt**: Kostenbasis, Konzernkosten, SAP-Report |
| `docs/FINANCE_SUPPLIER.md` | **zusammengefuehrt**: Klassifikation, Laenderstatus, Fallback |
| `docs/FINANCE_JOURNAL.md` | **zusammengefuehrt**: Hauptbuch-Import, `FinanzJournalSet` |
| `docs/FINANCE_VBRP_WAVWR_SPEZ_2026-07-16.md` | SAP-Spezifikation WAVWR |
| `docs/FINANCE_DASHBOARD_NACHWEIS_2026-06-17.md` | Nachweis-Excel |
| `docs/FINANCE_SCHULUNG_FINANZ_2026-06-11.md` | Schulung |
| `docs/FINANCE_BUDGET_CHF_FRAGEN_FINANZCHEF_2026-06-15.md` | Budget-CHF-Fragenkatalog |
| `docs/FINANCE_ANZEIGE_PRUEFUNG_2026-08-06.md` | stimmt eine Anzeige? |
| `docs/FINANCE_INDIKATOREN_PRUEFUNG_2026-08-07.md` | welche Indikatoren echt rechnen |
| `docs/FINANCE_UK2025_WERTFEHLER_2026-08-10.md` | UK 2025, Stueckpreis statt Zeilenwert |
| `docs/FINANCE_OFFENE_PUNKTE_2026-08-12.md` | Begruendung und Fallen zum Issue-Log |
| `docs/MARKTSEGMENTE_RAILWAY_2026-08-13.md` | Marktsegmente und Marktumfrage |

## Standortdaten und Exporte

| Datei | Rolle |
| --- | --- |
| `docs/rag/MANUAL_IMPORT.md` | Kurzstand, Dedupe, Betriebsfallen |
| `docs/FINANCE_FELDLUECKEN.md` | **zusammengefuehrt**: was fehlt, wer es besitzt |
| `docs/STANDORT_ES_SAGE.md` | **zusammengefuehrt**: Spanien |
| `docs/FINANCE_ES_BUCHUNGSDATUM_2026-08-03.md` | Spanien, Buchungsdatum im Detail |
| `docs/STANDORT_DE_ALPHAPLAN.md` | **zusammengefuehrt**: Deutschland |
| `docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md` | Indien, Sales Type |
| `docs/FINANCE_IT_VORGEHEN_2026-05-18.md` | Italien |
| `docs/FINANCE_UK_QUELLE_KORREKTUR_2026-05-18.md` | UK |
| `docs/QUELLSYSTEME_SAP_B1.md` | **zusammengefuehrt**: B1 ueber HANA und SAP OData |
| `docs/MANUAL_IMPORT_DELTA_STAND_2026-05-21.md` | Manual-Import-Details, historisch |
| `docs/ANSPRECHPARTNER.md` | Empfaenger je Standort |
| `AlphaplanExportPackage/scripte/ANLEITUNG_KORREKTUR_2026-06-24.md` | Einrichtung DE-Server |
| `AlphaplanExportPackage/CLAUDE_ODATA_DASHBOARD_KONTEXT.md` | Kontextnotiz Alphaplan/OData |

## Einkauf und Logistik

| Datei | Rolle |
| --- | --- |
| `docs/rag/PURCHASING.md` | Kurzstand |
| `docs/PURCHASING_DASHBOARD_2026-06-05.md` | Hauptdoku, Formeln, Cache |
| `docs/EINKAUF_ANFORDERUNGEN_HISTORIE.md` | **zusammengefuehrt**: umgesetzt, offen, zurueckgestellt |
| `docs/EINKAUF_INDIKATOREN_PRUEFUNG_2026-08-07.md` | welche Indikatoren echt rechnen |
| `docs/PURCHASING_PRODUKTGRUPPEN_ABCXYZ_2026-08-06.md` | Produktgruppen, ZC23, ABC/XYZ |
| `docs/PURCHASING_PRODUCT_GROUP_SAP_DIRECT_2026-08-11.md` | Produktgruppen aus SAP OData |
| `docs/EINKAUF_LOGISTIK_SUPPLY_CHAIN_REITER_2026-08-06.md` | Supply-Chain-Reiter |
| `docs/LOGISTIK_STUECKLISTEN_DASHBOARD_2026-08-01.md` | Stuecklisten-Dashboard |
| `docs/EINKAUF_LOKALISIERUNG_PROJEKTSUITE_2026-08-01.md` | Sprachen, Projektsuite |

## HR

| Datei | Rolle |
| --- | --- |
| `docs/rag/HR_KPI.md` | Kurzstand, Zugang |
| `docs/HR_KPI.md` | **zusammengefuehrt**: Fachlogik, Formeln, Grenzen |
| `docs/HR_KPI_PRUEFUNG_SWISS_BEST_PRACTICES.md` | Fachpruefung |

## Plattform

| Datei | Rolle |
| --- | --- |
| `docs/rag/DEPLOYMENT.md` | aktuell verifizierter Produktivstand |
| `docs/DEPLOYMENT.md` | **zusammengefuehrt**: Verfahren, Konsole, Fallen |
| `docs/rag/ARCHITECTURE.md` | Architektur, Kurzstand |
| `docs/rag/ADMIN.md` | Admin, Kurzstand |
| `docs/rag/PROJECT.md` | Projektstand, Kurzstand |
| `docs/ADMIN_BEREICH_STARTSEITE_2026-05-21.md` | Admin und Startseite |
| `docs/ADMIN_MENUE_ZUSAMMENFUEHRUNG_2026-08-11.md` | Menuestruktur |
| `docs/REQUIREMENTS.md` | Gesamtfunktionalitaet, reverse-engineered |
| `docs/PROGRAMM_DIAGRAMME.md` | Diagramme |
| `docs/PAUSENSPIEL.md` | **zusammengefuehrt**: Nebenfeature |
| `docs/CCUSAGE_INSTALL_ANLEITUNG.md` | Werkzeuganleitung |
| `.tmp_sap_probe/ddic_lzcode/README.md` | Notiz im SapProbe-Arbeitsordner |

## SAP und ABAP

| Datei | Rolle |
| --- | --- |
| `docs/abap/README_LZCODE_WEBSERVICE.md` | ZLO03-Webservice |
| `docs/abap/README_PRODSPARTE.md` | Produktsparten-Provider |
| `docs/abap/README_PRODUCT_GROUP_SAP_ODATA.md` | Produktgruppen per OData, SEGW |
| `docs/abap/README_FIN_ANALYSE_STPRS_JOURNAL.md` | Analysereport STPRS und Journal |
| `docs/PRODUCT_SPARTEN_MAPPING_2026-05-27.md` | Produktsparten-Mapping |
| `docs/rag/PRODUCT_MAPPING.md` | Produktmapping, Kurzstand |
| `docs/PPWR_SAP_KLASSIFIZIERUNG_ANLAGEPROTOKOLL_2026-08-13.md` | PPWR und Stoffcompliance |
| `docs/SAP_KALKULATION_RUESTZEIT_BEARBEITUNGSZEIT_ANDREAS_2026-07-30.md` | Ruest- gegen Bearbeitungszeit |
| `spartenlogic/UEBERGABE_PRODUKTSPARTEN_ZUORDNUNG.md` | Uebergabe Spartenzuordnung |
| `saptasks/zzprdat-kontext.md` | ZZPRDAT-Arbeitsstand |
| `zlo03/BEFUND_SYSTEMABGLEICH_2026-08-03.md` | ZLO03-Systemabgleich |
| `zlo03/ZM_LZCODE20_OPT_fixes.md` | ZLO03-Codefixes |
| `zlo03/CLAUDE.md` | bereichsspezifische Arbeitsregeln ZLO03 |

## Projekt und Koordination

| Datei | Rolle |
| --- | --- |
| `docs/AGENT_COORDINATION.md` | **vor jeder Arbeit lesen**, Reservierungen |
| `projektmanagement/PROJEKTSTATUS.md` | Ingos Arbeitspakete `PM-01` ff. |
| `docs/INGO_TODOS_180_TAGE_2026-06-18.md` | 180-Tage-Roadmap |

## Historie — kein Sollstand

Diese Dateien beantworten **keine** Statusfrage. Sie belegen, was zu einem Zeitpunkt galt.

| Datei | Rolle |
| --- | --- |
| `docs/raw_md_archive/LASTCHANGE_ARCHIV_bis_2026-07-12.md` | Aenderungsstand bis 2026-07-12 |
| `docs/raw_md_archive/LASTCHANGE_ARCHIV_2026-07-13_bis_2026-07-30.md` | Aenderungsstand bis 2026-07-30 |
| `docs/raw_md_archive/RAG_ROUTER_ARCHIV_2026-07-31.md` | Routerstand vor der Aufteilung |
| `docs/raw_md_archive/RAG_KURZDATEIEN_ARCHIV_ueberholte_eintraege.md` | aus Kurzdateien entfernte Eintraege |
| `docs/raw_md_archive/HISTORY_CANONICAL.md.raw` | kanonische Detailhistorie (keine `.md`-Endung) |

## Ausserhalb der Fachdokumentation

| Datei | Warum hier |
| --- | --- |
| `whisper/README.md`, `whisper/ANLEITUNG.md`, `whisper/HANDOFF.md` | Transkriptionswerkzeug, kein Projektwissen; nicht versioniert |
| `docs/rag/init.md` | lokale Hilfsdatei, nicht versioniert |
| `whisper/python/Lib/site-packages/**` | Fremdcode, 33 Lizenz- und Vorlagendateien |

## Am 2026-08-17 zusammengefuehrt und geloescht

41 Dateien sind in 11 neue Dateien aufgegangen. Ihr Inhalt ist vollstaendig erhalten, ihre
Historie ueber `git log --follow` weiter erreichbar.

| Neue Datei | Ersetzt |
| --- | --- |
| `docs/FINANCE_STANDARDKOSTEN.md` | `FINANCE_STANDARDKOSTEN_2026-07-14`, `..._ARBEITSNOTIZ_2026-07-17`, `..._SITZUNG_ANDREAS_2026-07-27`, `FINANCE_ANDREAS_BESCHLUSS_LOKALE_STANDARDKOSTEN_2026-08-11`, `FINANCE_CHAT_2026_LUECKE_ROOTCAUSE_2026-07-28` |
| `docs/FINANCE_SUPPLIER.md` | `FINANCE_SUPPLIER_LUECKE_ANALYSE_2026-07-28`, `FINANCE_SUPPLIER_HANDOFF_2026-08-11`, `FINANCE_SUPPLIER_FALLBACK_UMSCHALTER_2026-08-11`, `SUPPLIER_LAENDERSTATUS_CH_AT_PRUEFUNG_2026-08-11` |
| `docs/FINANCE_JOURNAL.md` | `FINANCE_B1_JOURNAL_IMPORT_2026-07-14`, `FINANCE_JOURNAL_SAP_ODATA_SPEZ_2026-07-14` |
| `docs/QUELLSYSTEME_SAP_B1.md` | `FINANCE_SAP_B1_KONNEKTOREN_ANDREAS_2026-07-01` |
| `docs/FINANCE_FELDLUECKEN.md` | `FINANCE_FELDLUECKEN_MAILS_2026-07-31`, `FINANCE_FELDLUECKEN_STANDORTE_2026-07-30`, `FINANCE_DATENLUECKEN_ANDREAS_2026-07-28` |
| `docs/STANDORT_ES_SAGE.md` | `SAGE_SPAIN_EXPORT_2026-05-05` (Wurzel), `SAGE_SPAIN_RCLONE_UPLOAD_GUIDE_2026-06-03`, ES-Teil aus `FINANCE_BACKFILL_UK_ES_2026-07-28` |
| `docs/STANDORT_DE_ALPHAPLAN.md` | `ALPHAPLAN_DISCOVERY_EXPORTER_GUIDE_2026-06-08`, `ALPHAPLAN_SQL_RCLONE_KONZEPT_DE_2026-06-08` |
| `docs/EINKAUF_ANFORDERUNGEN_HISTORIE.md` | `PURCHASING_DASHBOARD_KORREKTUREN_2026-07-06`, `..._VORBEREITUNG_INGO_2026-07-09`, `..._UMSETZUNGSPLAN_MARCO_2026-07-09`, `..._REVIEW_MARCO_2026-07-10`, `..._WUENSCHE_EINKAUF_2026-07-23`, `..._WUENSCHE_EINKAUF_2026-07-30` |
| `docs/HR_KPI.md` | `HR_KPI_NACHDOKU_2026-05-13`, `HR_KPI_KORREKTUREN_2026-07-06`, `HR_KPI_FEIERTAGE_FILTERTEST_2026-08-06` |
| `docs/DEPLOYMENT.md` | `DEPLOYMENT_IIS_HANDOFF_2026-05-19`, `DEPLOY_KONSOLE_2026-08-07`, `DEPLOY_GESAMTSTAND_2026-08-11`, `LOCAL_DEV_SERVER_UEBERGANG_2026-05-21` |
| `docs/PAUSENSPIEL.md` | `PAUSENSPIEL_DROHNEN_KONZEPT_2026-08-07`, `PAUSENSPIEL_STUFE1_2026-08-07` |
| `router.md`, `docs/router/*`, `baum.md` | `RAG_ROUTER.md`, `RAG_DETAIL_INDEX.md`, `MD_DOKUMENTENSTATUS_2026-05-20.md` |

Ersatzlos entfernt, weil der Inhalt in einer aktuellen Datei aufgegangen ist:
`FINANCE_IMPORTPRUEFUNG_2026-07-29` (Betriebsfallen nach `docs/rag/MANUAL_IMPORT.md`),
`FINANCE_ISSUE_LOG_ANDREAS_2026-07-28` (fuehrend ist die TSV),
`FINANCE_BACKFILL_UK_ES_2026-07-28` (UK-Teil ueberholt),
`CODEX_ANWEISUNG_FINANCE_DATEN_HEARTBEAT_2026-07-13` (umgesetzt).
