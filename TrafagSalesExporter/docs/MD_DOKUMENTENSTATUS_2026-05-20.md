# Markdown-Dokumentenstatus

Stand: 2026-05-20

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
| `docs/HR_KPI_NACHDOKU_2026-05-13.md` | HR-KPI technische/fachliche Nachdoku | Aktualisiert um 2026-05-20 Erweiterungen |
| `docs/PROGRAMM_DIAGRAMME.md` | Uebersicht Diagramme und technische Einordnung | Aktualisiert um neue Anwenderdokus |

## Aktuell als Detail-/Spezialdoku

| Datei | Rolle | Status |
| --- | --- | --- |
| `docs/FINANCE_BERECHNUNGSFORMELN_LAENDER_2026-05-19.md` | Detailregeln je Land | Behalten |
| `docs/FINANCE_HANDOFF_2026-05-18.md` | Finance-Handoff vor den 20.05.-Aenderungen | Behalten, mit neueren Nachtraegen lesen |
| `docs/FINANCE_IT_VORGEHEN_2026-05-18.md` | Italien-Pruefpfad | Behalten |
| `docs/FINANCE_UK_QUELLE_KORREKTUR_2026-05-18.md` | UK-Quellkorrektur | Behalten |
| `docs/HR_KPI_PRUEFUNG_SWISS_BEST_PRACTICES.md` | HR-KPI Formel-/Best-Practice-Pruefung | Behalten |
| `SAGE_SPAIN_EXPORT_2026-05-05.md` | Sage Spanien Export | Behalten |
| `LLM_SYSTEM_GUIDE.md` | Arbeits-/Systemkontext fuer LLM | Behalten |
| `persona.md` | Nutzer-/Projektkontext | Behalten |

## Historisch / nicht mehr fuehrend

Diese Dateien bleiben aus Nachvollziehbarkeitsgruenden erhalten, sind aber nicht mehr als aktueller Stand zu lesen:

| Datei | Grund |
| --- | --- |
| `docs/FINANCE_DASHBOARD_TODO_2026-05-15.md` | Urspruengliche Todo-Liste; Status wurde am 2026-05-20 aktualisiert |
| `docs/FINANCE_WELCHES_DOKUMENT_GILT_2026-05-15.md` | CFO-Dokumentenstand vom 15.05.; Anwenderdoku vom 20.05. ist hinzugekommen |
| `docs/FINANCE_ES_MAIL_ABWEICHUNG_2026-05-15.md` | Mail-/Abweichungsnotiz |
| `docs/FINANCE_IT_MAIL_ABWEICHUNG_2026-05-15.md` | Mail-/Abweichungsnotiz |
| `docs/FINANCE_UK_MAIL_ABWEICHUNG_2026-05-15.md` | Mail-/Abweichungsnotiz |

## Neue Word-/Bilddokumente seit 2026-05-20

| Datei | Zweck |
| --- | --- |
| `docs/HR_KPI_ANLEITUNG_HR_2026-05-20.docx` | Anwenderdoku fuer HR |
| `docs/FINANCE_COCKPIT_ANLEITUNG_FINANZ_2026-05-20.docx` | Anwenderdoku fuer Finance |
| `docs/hr_kpi_cockpit_preview.png` | neutrale HR-Cockpit-Vorschaugrafik fuer DOCX |
| `docs/finance_cockpit_preview.png` | neutrale Finance-Cockpit-Vorschaugrafik fuer DOCX |

## Bereinigung

Es wurden keine alten Markdown-Dateien geloescht. Grund: Viele enthalten historische Pruefwerte, Zwischenentscheide und konkrete Pfade, die fuer Rueckfragen oder Audits noch relevant sein koennen.

Stattdessen gilt:

- Aktuelle operative Orientierung ueber diese Statusdatei, `NEXT_STEPS_2026-04-15.md`, `HANDOFF_2026-04-15.md` und `lastchange.md`.
- Historische Mail-/Terminnotizen nur als Beleg lesen, nicht als aktuellen Produktstand.
- Alte offene Punkte wurden dort aktualisiert, wo sie durch Finance Summary, HR KPI Cockpit oder die Word-Anleitungen ueberholt sind.
