# RAG Project

Stand: 2026-06-15

## Kurzstand

- Fuehrende App: `TrafagSalesExporter`, publiziert als `BiDashboard`.
- Neu lokal umgesetzt: Deutschland/Alphaplan liest das finale CSV-Paar `invoice_headers.csv` + `invoice_lines.csv`; Vollbestand im Ordner plus 7-Tage-Delta im Unterordner `delta` werden zusammen gelesen und per Alphaplan-Zeilen-ID dedupliziert.
- Neu lokal umgesetzt: Produktsparten-Mapping ist auf den neuen vollstaendigen SAP-OData-Referenzservice vorbereitet. `ProductDivisionRefSet` bleibt fuehrend, `ProductDivisionMapSet` ist im Seed inaktiv, Produktfelder kommen direkt aus `P.*`, und `Übrige`/Code `0008` ist eigene gueltige Kategorie.
- Neu lokal umgesetzt: OData-Import-Join normalisiert `Matnr` beidseitig wie die Analyse, inkl. Entfernen fuehrender Nullen.
- Kritischer Live-Check 2026-06-15: Die aktuell konfigurierte URL `ZPOWERBI_EINKAUF_SRV/ProductDivisionRefSet` auf `travp762` ist noch nicht der neue vollstaendige Referenzservice. Sie liefert 42'501 Zeilen, aber `Assigned=0`, `UNASS=42'501`, `0008=0`.
- Neu lokal umgesetzt: Guardrail im SAP-Import bricht ab, wenn `ProductDivisionRefSet` eine grosse Referenz mit 0 zugeordneten Sparten liefert; so werden Dashboard-Daten nicht mit `Nicht zugeordnet` ueberschrieben. SAP-Gateway-Timeout ist 5 Minuten.
- Validierung lokal 2026-06-15: `dotnet test TrafagSalesExporter.sln --verbosity minimal` mit `97/97` Tests gruen.
- Wichtig DE/Sparten: Alphaplan `ArtikelNummer` wird als lokale Materialnummer importiert, aber nicht als garantiert identische TR-AG-/SAP-`MATNR` normalisiert. Nicht gematchte Nummern erscheinen weiterhin als `Nicht im TR-AG-Stamm`.
- Letzter dokumentierter Deploy: 2026-06-11, Commit `1dbaa66 Add purchasing translations`, `BiDashboard.dll` Zeitstempel `11.06.2026 12:30:27`.
- Letzte Validierung: `dotnet test TrafagSalesExporter.sln --verbosity minimal` mit `92/92` Tests gruen.
- Neu deployed: Einkaufsdashboard und `Einkauf > Datenquellen` haben erweiterte UI-Uebersetzungen fuer Spanisch, Italienisch und Hindi; technische Feldnamen wie SAP-Entity-Sets bleiben bewusst unveraendert.
- Neu lokal: Audit-CSV-Modus fuer Finance/Revision. Standortexporte schreiben optional nach Mapping/Transformation je Standort `Sales_ProcessedMergeInput_<TSC>_<Datum>.csv`; zentrale Excel, Finance Summary, Soll/Ist und Management-Analyse koennen per Setting aus den neuesten Standort-CSV statt aus der internen DB lesen.
- Aktuelle Finance-Schulung: `docs/FINANCE_SCHULUNG_FINANZ_2026-06-11.md` mit Prozessgrafiken fuer Exportfluss, Audit-CSV-Auswertungsquelle und Waehrungsumrechnung.
- Produktsparten-Komponentenfallback 2026-06-11: `ZCL_PRODSPARTE_PROVIDER=>GET_DATA` soll Komponenten aus `ZPOWERBI_VC_TXT` ueber eindeutige Kopfmaterial-Produktsparte zusaetzlich in `ProductDivisionRefSet` liefern; Prod `travp762` liefert die EntitySets, aber der CSV-Abgleich zeigt noch 804/804 Komponenten ohne Treffer, daher direkter SAP-Provider-Lauf als naechster Pruefpunkt.
- Vorheriges UI-Delta 2026-06-11: Export-Dashboard-Manometer als fixes SVG mit Beschriftung; doppelte obere Finance-/Management-Tabbaender reduziert.
- Aktueller lokaler Stand: CH/AT-Produktsparten-Fallback ueber `ProductDivisionMapSet` ist abgeloest und inaktiv; vor Produktiv-Refresh muss aber die neue SAP-Service-URL gesetzt/verifiziert werden. India/TRIN SAGE-HANA-Fix und Spanien-SharePoint-Pfad bleiben abgesichert.
- Vorheriger Deploy: 2026-06-10 Produktsparten-Fallback auf `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`.
- Vorheriger Produktsparten-Server-Import: 40'292 CH/AT-Datensaetze, 36'953 assigned, 0 `UnassignedWithReference`; nach Deploy des neuen lokalen Stands muss `ZSCHWEIZ` neu exportiert/importiert werden.
- India/TRIN: produktive Server-DB steht auf `TRIN -> SAGE -> 20.197.20.60:30015`, Schema `TRAFAG_LIVE`, User-Override `TRAFAGCONTROLS`.
- Doku-Delta: `docs/FINANCE_KURS_WORKFLOW_2026-06-09.md` plus SVG; alte Finance-Stubs aus aktiver Markdown-Struktur entfernt, Volltexte bleiben im Raw-Archiv.
- Neu im Finance/Management-Cockpit: einfache Schnelluebersicht links sichtbar; tiefere Funktionen bleiben unter `Experten`.
- Neu in der Navigation: Menuebaum wird aus `NavigationMenuItems` gerendert; Admins koennen bestehende Punkte unter `Admin > Menuestruktur` umhaengen, sortieren und aus-/einblenden.
- Neu als Hauptbereich: `Einkauf` mit Einkaufswagen-Icon und erweitertem `Einkauf Dashboard`.
- Einkauf: `x.pbix` wurde als Vorlage analysiert; die frueheren Tabs wurden in linke Navigationspunkte unter `Einkauf` aufgeteilt: Dashboard, Spend, offene Bestellungen, Kontrakte, Lieferanten, Ideen, Kennzahlen-Katalog, PBIX Vorlage und 3D Simulation.
- Einkauf: `Einkauf > Datenquellen` pflegt die SAP/OData-Konfiguration grafisch und ist mit `EKKOSet`, `EKPOSet`, `eketSet`, `Data`, `Data2`, Joins und Zielmappings vorbefuellt. `/einkauf` liest jetzt den Einkauf-Cache aus SAP-Full-Load; Stand 2026-06-05: EKKO 172'874, EKPO 233'921, EKET 242'572 Zeilen. Delta-Refresh ist unter `Einkauf > Ideen > Einkauf-Datenservice` vorbereitet.
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
