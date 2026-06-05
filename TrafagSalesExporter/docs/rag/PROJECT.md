# RAG Project

Stand: 2026-06-05

## Kurzstand

- Fuehrende App: `TrafagSalesExporter`, publiziert als `BiDashboard`.
- Letzter dokumentierter Stand: Finance-Schnelluebersicht, Expertenbereich, 3D-Datenanalyse und Spanien-Sage-All-in-one-rclone-Upload.
- Validierung laut Doku: Finance-Sitzungsstand `82/82` Tests gruen; spaetere UI-/Deploy-Schritte wurden einzeln umgesetzt und deployed.
- Letzter dokumentierter Finance-Deploy: 2026-06-05 auf `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`.
- Neu im Finance/Management-Cockpit: einfache Schnelluebersicht links sichtbar; tiefere Funktionen bleiben unter `Experten`.
- Neu in der Navigation: Menuebaum wird aus `NavigationMenuItems` gerendert; Admins koennen bestehende Punkte unter `Admin > Menuestruktur` umhaengen, sortieren und aus-/einblenden.
- Neu als Hauptbereich: `Einkauf` mit Einkaufswagen-Icon und erweitertem `Einkauf Dashboard`.
- Einkauf: `x.pbix` wurde als Vorlage analysiert; `/einkauf` enthaelt jetzt Struktur fuer Spend, offene Bestellungen, Mengenkontrakte, Lieferantenperformance, PBIX-Reportseiten und 3D-Simulation.
- Einkauf: `Einkauf > Datenquellen` pflegt die SAP/OData-Konfiguration grafisch und ist mit `EKKOSet`, `EKPOSet`, `eketSet`, `Data`, `Data2`, Joins und Zielmappings vorbefuellt. `/einkauf` laedt EKKO/EKPO/EKET live und zeigt eine echte, begrenzte SAP-Probe fuer Spend, offene Werte/Mengen und Kontrakt-Restwerte. Vollstaendige Jahresaggregation und Lieferantenperformance sind noch offen.
- Neu im Expertenbereich: `3D Datenanalyse` mit drehbarer 3D-Grafik, Achsen, Diagrammarten, Indikatorauswahl, Labelgroesse und Simulation per Schieberegler.
- Spanien: `Run-SpainRangeExportAndUpload-AllInOne.ps1` exportiert Sage-Range direkt und laedt CSV/Summary via rclone nach SharePoint `trafag-bi:Import/Finance/Spanien`.
- Spanien: Default-Range ist heute minus 7 Tage bis heute; `ToDate` ist exklusiv.
- Spanien: rclone-Fehler `Can't set -v and --log-level` im All-in-one-Script behoben; aktuelle Datei enthaelt kein `--verbose` im Upload.
- Spanien-Import: Ordner mit `Spain_Sales*.csv` werden komplett gelesen; Basis + taegliche Range-Dateien werden nach `SourceLineId` bzw. Invoice/Position/Material dedupliziert.
- Spanien-Delta-Sync wurde am 2026-06-05 deployed; `app_offline.htm` wurde fuer den Publish kurz gesetzt und danach entfernt.
- Fuer normale Weiterarbeit diese Datei plus den passenden Themen-RAG laden.

## Aktive Themen

- Finance Cockpit: `docs/rag/FINANCE.md`
- Manual Import: `docs/rag/MANUAL_IMPORT.md`
- Produktmapping: `docs/rag/PRODUCT_MAPPING.md`
- HR KPI: `docs/rag/HR_KPI.md`
- Deployment/IIS: `docs/rag/DEPLOYMENT.md`
- Admin/Startseite: `docs/rag/ADMIN.md`
- Einkauf: `docs/PURCHASING_DASHBOARD_2026-06-05.md`

## Rohquellen Nur Bei Bedarf

- kanonische Detailhistorie: `docs/raw_md_archive/HISTORY_CANONICAL.md.raw`
- exakte Originaldateien zur Wiederherstellung: `docs/raw_md_archive/original_history_raws.zip`
- Dokumentstatus: `docs/MD_DOKUMENTENSTATUS_2026-05-20.md`
