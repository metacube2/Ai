# RAG Project

Stand: 2026-06-05

## Kurzstand

- Fuehrende App: `TrafagSalesExporter`, publiziert als `BiDashboard`.
- Letzter dokumentierter Stand: Finance-Schnelluebersicht, Expertenbereich, 3D-Datenanalyse und Spanien-Sage-All-in-one-rclone-Upload.
- Validierung laut Doku: Finance-Sitzungsstand `82/82` Tests gruen; spaetere UI-/Deploy-Schritte wurden einzeln umgesetzt und deployed.
- Letzter dokumentierter Finance-Deploy: 2026-06-04 auf `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`.
- Neu im Finance/Management-Cockpit: einfache Schnelluebersicht links sichtbar; tiefere Funktionen bleiben unter `Experten`.
- Neu im Expertenbereich: `3D Datenanalyse` mit drehbarer 3D-Grafik, Achsen, Diagrammarten, Indikatorauswahl, Labelgroesse und Simulation per Schieberegler.
- Spanien: `Run-SpainRangeExportAndUpload-AllInOne.ps1` exportiert Sage-Range direkt und laedt CSV/Summary via rclone nach SharePoint `trafag-bi:Import/Finance/Spanien`.
- Spanien: Default-Range ist heute minus 7 Tage bis heute; `ToDate` ist exklusiv.
- Spanien: rclone-Fehler `Can't set -v and --log-level` im All-in-one-Script behoben; aktuelle Datei enthaelt kein `--verbose` im Upload.
- Spanien-Import: Ordner mit `Spain_Sales*.csv` werden komplett gelesen; Basis + taegliche Range-Dateien werden nach `SourceLineId` bzw. Invoice/Position/Material dedupliziert.
- Fuer normale Weiterarbeit diese Datei plus den passenden Themen-RAG laden.

## Aktive Themen

- Finance Cockpit: `docs/rag/FINANCE.md`
- Manual Import: `docs/rag/MANUAL_IMPORT.md`
- Produktmapping: `docs/rag/PRODUCT_MAPPING.md`
- HR KPI: `docs/rag/HR_KPI.md`
- Deployment/IIS: `docs/rag/DEPLOYMENT.md`
- Admin/Startseite: `docs/rag/ADMIN.md`

## Rohquellen Nur Bei Bedarf

- kanonische Detailhistorie: `docs/raw_md_archive/HISTORY_CANONICAL.md.raw`
- exakte Originaldateien zur Wiederherstellung: `docs/raw_md_archive/original_history_raws.zip`
- Dokumentstatus: `docs/MD_DOKUMENTENSTATUS_2026-05-20.md`
