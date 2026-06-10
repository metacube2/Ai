# Markdown-Dokumentenstatus

Stand: 2026-06-10

RAG-Hinweis: Fuer tokenarme Kontextauswahl zuerst `docs/RAG_ROUTER.md` laden. Standardmaessig nur die Kurzdateien unter `docs/rag/` laden; diese Datei und andere Original-MDs nur bei Detail-/Auditbedarf.

Diese Datei ordnet die vorhandenen Markdown-Dateien ein. Ziel ist, alte Arbeitsnotizen nicht mit dem aktuellen Produktstand zu verwechseln.

## Aktuell fuehrend

| Datei | Rolle | Status |
| --- | --- | --- |
| `lastchange.md` | Laufende Aenderungshistorie | Fuehrend fuer Chronologie |
| `NEXT_STEPS_2026-04-15.md` | Aktuelle offene Punkte und naechste Schritte | Weiter pflegen |
| `HANDOFF_2026-04-15.md` | Technischer Handoff / Kontext fuer Weiterarbeit | Weiter pflegen |
| `docs/DEPLOYMENT_IIS_HANDOFF_2026-05-19.md` | IIS-/Server-Handoff | Aktuell fuer Deployment |
| `docs/LOCAL_DEV_SERVER_UEBERGANG_2026-05-21.md` | Lokaler Uebergangsserver auf Entwicklungs-PC | Aktuell bis IIS-Fix |
| `docs/FINANCE_ENTSCHEIDE.md` | Finance-Regeln und Kontrollpunkte | Aktuell fuehrend fuer Finance-Logik |
| `entscheide.md` | Kurzfassung der Finance-Fachentscheide | Aktuell als Kurzfassung |
| `docs/FINANCE_DATENFLUSS_ANDREAS_2026-06-08.md` | Technischer Finance-Datenfluss | Aktuell fuer End-to-end-Datenfluss |
| `docs/PRODUCT_SPARTEN_MAPPING_2026-05-27.md` | Produktsparten-Mapping fuer Group Sales Report | Aktuell fuehrend fuer neues Produktmapping-Thema |
| `docs/HR_KPI_NACHDOKU_2026-05-13.md` | HR-KPI technische/fachliche Nachdoku | Aktualisiert um 2026-05-20 Erweiterungen |
| `docs/PROGRAMM_DIAGRAMME.md` | Uebersicht Diagramme und technische Einordnung | Aktualisiert um neue Anwenderdokus |

## Aktuell als Detail-/Spezialdoku

| Datei | Rolle | Status |
| --- | --- | --- |
| `docs/FINANCE_BERECHNUNGSFORMELN_LAENDER_2026-05-19.md` | Detailregeln je Land | Behalten |
| `docs/FINANCE_KURS_WORKFLOW_2026-06-09.md` | Isolierter Workflow fuer Kurs-/Waehrungsanwendung vom Land bis Dashboard | Aktuell fuer Kursfragen; SVG daneben |
| `docs/FINANCE_IT_VORGEHEN_2026-05-18.md` | Italien-Pruefpfad | Behalten |
| `docs/FINANCE_UK_QUELLE_KORREKTUR_2026-05-18.md` | UK-Quellkorrektur | Behalten |
| `docs/SAGE_SPAIN_RCLONE_UPLOAD_GUIDE_2026-06-03.md` | Aktueller Spanien-rclone-All-in-one-Workflow | Ersetzt alte deutsche Anleitung vom 2026-06-03 |
| `docs/HR_KPI_PRUEFUNG_SWISS_BEST_PRACTICES.md` | HR-KPI Formel-/Best-Practice-Pruefung | Behalten |
| `SAGE_SPAIN_EXPORT_2026-05-05.md` | Sage Spanien Export | Behalten |
| `LLM_SYSTEM_GUIDE.md` | Arbeits-/Systemkontext fuer LLM | Behalten |
| `persona.md` | Nutzer-/Projektkontext | Behalten |

## Archiviert / aus aktiver Doku entfernt

Diese Dateien wurden am 2026-06-09 aus der aktiven Markdown-Struktur entfernt, weil sie nur noch historische Stubs oder durch neuere Dokus ersetzt waren. Fachinhalt bleibt erhalten:

| Entfernte Datei | Grund |
| --- | --- |
| `FINANCE_HANDOFF_2026-05-18.md` | Volltext liegt in `docs/raw_md_archive/HISTORY_CANONICAL.md.raw`; aktueller Kurzkontext steht in `docs/rag/FINANCE.md`. |
| `FINANCE_DASHBOARD_TODO_2026-05-15.md` | Volltext liegt in `docs/raw_md_archive/HISTORY_CANONICAL.md.raw`; aktueller Finance-Kontext steht in `docs/rag/FINANCE.md`. |
| `FINANCE_WELCHES_DOKUMENT_GILT_2026-05-15.md` | Volltext liegt in `docs/raw_md_archive/HISTORY_CANONICAL.md.raw`; aktueller Router ist `docs/RAG_ROUTER.md`. |
| `FINANCE_ES_MAIL_ABWEICHUNG_2026-05-15.md` | Volltext liegt in `docs/raw_md_archive/HISTORY_CANONICAL.md.raw`; aktueller ES-Kontext steht in `docs/rag/FINANCE.md` und `SAGE_SPAIN_EXPORT_2026-05-05.md`. |
| `FINANCE_IT_MAIL_ABWEICHUNG_2026-05-15.md` | Volltext liegt in `docs/raw_md_archive/HISTORY_CANONICAL.md.raw`; aktueller IT-Kontext steht in `docs/FINANCE_IT_VORGEHEN_2026-05-18.md`. |
| `FINANCE_UK_MAIL_ABWEICHUNG_2026-05-15.md` | Volltext liegt in `docs/raw_md_archive/HISTORY_CANONICAL.md.raw`; aktueller UK-Kontext steht in `docs/FINANCE_UK_QUELLE_KORREKTUR_2026-05-18.md`. |
| `SAGE_SPAIN_RCLONE_UPLOAD_ANLEITUNG_2026-06-03.md` | Veralteter zweiscriptiger Spanien-rclone-Workflow; aktueller Stand ist `docs/SAGE_SPAIN_RCLONE_UPLOAD_GUIDE_2026-06-03.md`. |

## Neue Word-/Bilddokumente seit 2026-05-20

| Datei | Zweck |
| --- | --- |
| `docs/HR_KPI_ANLEITUNG_HR_2026-05-20.docx` | Anwenderdoku fuer HR |
| `docs/FINANCE_COCKPIT_ANLEITUNG_FINANZ_2026-05-20.docx` | Anwenderdoku fuer Finance |
| `docs/hr_kpi_cockpit_preview.png` | neutrale HR-Cockpit-Vorschaugrafik fuer DOCX |
| `docs/finance_cockpit_preview.png` | neutrale Finance-Cockpit-Vorschaugrafik fuer DOCX |

## Bereinigung

Bereinigung 2026-06-09:

- Historische Finance-Stubs und der alte Finance-Handoff wurden aus der aktiven Doku entfernt, weil der Volltext im Raw-Archiv liegt.
- Die alte deutsche Spanien-rclone-Anleitung wurde entfernt, weil der aktuelle All-in-one-Workflow im Guide vom 2026-06-05 dokumentiert ist.
- Die Alphaplan-Konzept- und Anleitungsdateien vom 2026-06-08 wurden bewusst nicht veraendert.
- Delta 2026-06-10: Produktsparten-Fallback `ProductDivisionMapSet`, India/SAGE-HANA-Deploy und Server-DB-Seeds wurden in `docs/DEPLOYMENT_IIS_HANDOFF_2026-05-19.md`, `spartenlogic/UEBERGABE_PRODUKTSPARTEN_ZUORDNUNG.md`, `docs/rag/DEPLOYMENT.md`, `docs/rag/PROJECT.md` und `lastchange.md` nachdokumentiert.

Weiterhin gilt:

- Aktuelle operative Orientierung ueber diese Statusdatei, `NEXT_STEPS_2026-04-15.md`, `HANDOFF_2026-04-15.md` und `lastchange.md`.
- Historische Mail-/Terminnotizen nur im Raw-Archiv als Beleg lesen, nicht als aktuellen Produktstand.
- Alte offene Punkte wurden dort aktualisiert, wo sie durch Finance Summary, HR KPI Cockpit oder die Word-Anleitungen ueberholt sind.
