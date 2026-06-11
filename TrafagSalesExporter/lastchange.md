# Last Change

Stand: 2026-06-11

Diese Datei ist fuer tokenarme RAG-Nutzung komprimiert.

## Aktueller Kurzstand

- Fuehrender Kurzkontext: `docs/rag/PROJECT.md`.
- Themenrouter: `docs/RAG_ROUTER.md`.
- Neu deployed: Commit `1dbaa66 Add purchasing translations` auf `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`.
- Deploy-Status 2026-06-11: `BiDashboard.dll` Zeitstempel `11.06.2026 12:30:27`; `app_offline.htm` wurde nach Publish entfernt.
- Validierung vor Deploy aus sauberer Worktree-Kopie: `dotnet test TrafagSalesExporter.sln --verbosity minimal` mit `92/92` Tests gruen.
- Neu lokal/deployed: Einkaufsdashboard, Einkaufs-Datenquellen und relevante Einkauf-Hilfstexte sind fuer Spanisch, Italienisch und Hindi im UI-Sprachservice nachgezogen; Audit-CSV-Hilfstext ist nicht mehr englisch im Spanisch-/Hindi-Modus.
- Vorheriger Deploy 2026-06-11: Commit `f751295 Update finance training and dashboard UI`, `BiDashboard.dll` Zeitstempel `11.06.2026 12:04:53`.
- Neu lokal/deployed: Export-Dashboard-Manometer als fixes SVG mit Beschriftung; doppelte obere Tab-Baender im Management/Finance-Cockpit reduziert.
- Neu lokal dokumentiert: aktuelle Finance-Schulung `docs/FINANCE_SCHULUNG_FINANZ_2026-06-11.md` mit Prozessgrafiken fuer Export Dashboard, Audit-CSV-Auswertungsquelle und Waehrungs-/Kursfluss.
- Neue Schulungsgrafiken: `docs/FINANCE_PROZESS_EXPORT_DASHBOARD_2026-06-11.svg`, `docs/FINANCE_AUDIT_CSV_QUELLE_2026-06-11.svg`, `docs/FINANCE_WAEHRUNG_KURSFLUSS_2026-06-11.svg`.
- Neu lokal umgesetzt: Standortexporte koennen nach Mapping und Transformation eine lesbare Audit-CSV je Standort schreiben; zentrale Excel, Finance Summary und Management-Analyse koennen per Setting wahlweise aus den neuesten Audit-CSV statt aus `CentralSalesRecords` lesen.
- Letzter dokumentierter Code-Stand: CH/AT-Produktsparten-Fallback ueber `ProductDivisionMapSet` deployed; India/TRIN HANA-Route und Spanien-SharePoint-Pfad bleiben im Seed abgesichert.
- Letzte dokumentierte Validierung: `dotnet test TrafagSalesExporter.sln --verbosity minimal` mit `92/92` Tests gruen.
- Letzter dokumentierter Deploy: 2026-06-11 Einkaufs-Uebersetzungen nach `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`.
- Neu umgesetzt und deployed: `ZSCHWEIZ` nutzt zusaetzlich `M = ProductDivisionMapSet` und den Join `Z.Prodh = M.Paph1`; Produktfelder fallen per `FirstNonEmpty(P.*, M.*)` von Materialreferenz auf PAPH1-Mapping zurueck.
- Server-DB am 2026-06-10 aktualisiert: CH/AT neu importiert, `FetchedRecords=40'292`, `Assigned=36'953`, `UnassignedWithReference=0`; Backup: `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\trafag_exporter.db.before-productdivision-map-20260610-161022.bak`.
- Deploy-Status 2026-06-10: `BiDashboard.dll` Zeitstempel `10.06.2026 16:09:44`; `app_offline.htm` wurde entfernt.
- Neu umgesetzt und deployed: `TRIN`/Indien wird beim Seed auf `SourceSystem=SAGE`, Schema `TRAFAG_LIVE` und zentralen SAGE-HANA-Server `20.197.20.60:30015` repariert; Standort-User-/Passwort-Override bleibt erhalten.
- Server-DB am 2026-06-10 korrigiert: `TRIN -> SAGE -> 20.197.20.60:30015`, User-Override `TRAFAGCONTROLS`, Passwort-Override vorhanden. Backup: `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\trafag_exporter.db.before-india-sage-20260610-0825.bak`.
- Server-DB am 2026-06-10 korrigiert: Spanien (`TRSE`) zeigt im manuellen Import jetzt auf den SharePoint-Ordner `https://trafagag.sharepoint.com/sites/WorldwideBIPlatform/Import/Finance/Spanien` statt auf die Einzeldatei `Spain_Sales_2025.csv`, damit Basis- und Range-/Delta-CSV zusammen gelesen werden. Backup: `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\trafag_exporter.db.before-spain-folder-path-20260610-100627.bak`.
- Git-Commit India-Fix: `586adc3 Fix India SAGE HANA mapping`.
- Neu dokumentiert: Delta zum Produktsparten-Fallback-Deploy in `docs/DEPLOYMENT_IIS_HANDOFF_2026-05-19.md` und `spartenlogic/UEBERGABE_PRODUKTSPARTEN_ZUORDNUNG.md`.
- Neu lokal: Sparten-Finanzanalyse gruppiert standardmaessig nach `Produktsparte`; `Produktfamilie` und `PAPH1 Detail` bleiben als Umschaltoptionen erhalten.
- Neu lokal: Sparten-Finanzanalyse zeigt bei `Mixed`-Waehrung einen Warnhinweis, weil Summen/Anteile ueber mehrere Waehrungen fachlich nur eingeschraenkt belastbar sind.
- Neu lokal: Sparten-Finanzanalyse zeigt die groessten Treiber fuer `Nicht im TR-AG-Stamm`, damit hohe nicht zugeordnete Umsaetze nach Land/TSC/Material analysiert werden koennen.
- Neu dokumentiert: genauer Finance-Datenfluss fuer Andreas in `docs/FINANCE_DATENFLUSS_ANDREAS_2026-06-08.md`.
- Neu dokumentiert: isolierter Finance-Kursworkflow in `docs/FINANCE_KURS_WORKFLOW_2026-06-09.md` mit SVG-Visualisierung `docs/FINANCE_KURS_WORKFLOW_2026-06-09.svg`, vom Land/Quellwert ueber `CurrencyExchangeRates` bis zur zentralen Dashboard-Analyse.
- Doku-Bereinigung: historische Finance-Stubs und der alte Finance-Handoff wurden aus der aktiven Markdown-Struktur entfernt; Volltexte bleiben in `docs/raw_md_archive/HISTORY_CANONICAL.md.raw`. Die veraltete deutsche Spanien-rclone-Anleitung wurde durch den aktuellen All-in-one-Guide ersetzt.
- Neu dokumentiert: Alphaplan SQL/rclone Konzept Deutschland in `docs/ALPHAPLAN_SQL_RCLONE_KONZEPT_DE_2026-06-08.md`.
- Neu erstellt: Alphaplan Phase-1-Discovery-Paket `AlphaplanExportPackage` und `AlphaplanExportPackage.zip`.
- Neu dokumentiert: Alphaplan Discovery Exporter Guide in `docs/ALPHAPLAN_DISCOVERY_EXPORTER_GUIDE_2026-06-08.md`.
- Alphaplan-Ziel: DE-Server exportiert SQL-Discovery/Rohdaten lokal und laedt per `rclone` nach `trafag-bi:Import/Finance/Deutschland/AlphaplanRaw`; BiDashboard-Importanpassung ist separater Folgeschritt.
- Neu umgesetzt und deployed: Finance bekommt links eine einfache Schnelluebersicht; die bisherigen tieferen Analysefunktionen bleiben unter `Experten`.
- Neu umgesetzt und deployed: `Experten > 3D Datenanalyse` mit drehbarer 3D-Visualisierung, Achsenbeschriftung, waehlbaren Indikatoren, Diagrammarten und Simulation.
- Neu umgesetzt und deployed: 3D-Simulation mit Schiebereglern, u. a. fuer Wechselkurs-/Szenarioveraenderungen; Grafik reagiert in Echtzeit.
- Neu umgesetzt und deployed: 3D-Darstellung korrigiert fuer Canvas-Groesse, Achsen, Labelgroesse und breitere Indikatorauswahl.
- Bekannter Browser-Hinweis: 3D-Ansicht wurde in Chrome als korrekt bestaetigt; Firefox zeigte auf dem Client Probleme mit Interaktion/Groesse.
- Neu fuer Spanien: All-in-one-PS1 `SageSpainExportPackage/SageSpainFinalExportPackage/Run-SpainRangeExportAndUpload-AllInOne.ps1` erstellt; es exportiert Sage direkt per SQL-Range und laedt CSV/Summary via rclone nach SharePoint.
- Neu fuer Spanien: Standard-Range ist letzte 7 Tage bis heute; `FromDate`/`ToDate` koennen per Parameter ueberschrieben werden.
- Neu fuer Spanien: SharePoint-Ziel wird vor Export per rclone geprueft/angelegt: `trafag-bi:Import/Finance/Spanien`.
- Neu fuer Spanien: rclone-Uploadfehler `Can't set -v and --log-level` behoben; `--verbose` wurde aus dem All-in-one-Upload entfernt.
- Neu fuer Spanien: rclone wird automatisch an mehreren Standardpfaden gesucht, inkl. `C:\Tools\rclone.exe`, `C:\Tools\rclone\rclone.exe`, `C:\Tools\rclone\rclone\rclone.exe` und `PATH`.
- Wichtig fuer Spanien: Nur das All-in-one-Script benoetigt keine separate `Export-SageSpainSalesCsv.ps1`; der alte Wrapper `Run-SpainExportAndUpload.ps1` braucht weiterhin das Export-Script daneben.
- Neu fuer Spanien-Import: SharePoint-/lokale Ordner mit `Spain_Sales*.csv` werden komplett gelesen; Basisdateien und taegliche Range-/Delta-Dateien werden zu einem deduplizierten Gesamtstand zusammengefuehrt.
- Spanien-Dedupe-Regel: primaer `SourceLineId`, Fallback `TSC + InvoiceNumber + PositionOnInvoice + Material`.
- Neu dokumentiert: Spanien-rclone-Anleitung und Package-README auf den All-in-one-Workflow aktualisiert.
- Neu umgesetzt: ES-Referenz 2025 auf `3'082'320.18 EUR` korrigiert; alter Sollwert `3'102'333.61 EUR` als Referenz-/Excel-Fehler dokumentiert.
- Neu umgesetzt: `FinanceProbe` nutzt dieselbe korrigierte ES-Referenz.
- Neu umgesetzt: Wechselkurs-Anwendungsdatum in Settings konfigurierbar (`PostingDate`, `InvoiceDate`, `ExtractionDate`) und in Rohdaten-Diagnose sichtbar.
- Neu umgesetzt: CHF als Anzeige-Waehrung in Management Analyse verfuegbar.
- Neu umgesetzt: `Management Analyse > Laender` zeigt IC/2nd-party und `Ist ohne IC` als Diagnosewerte.
- Neu umgesetzt: Sparten-Materialabgleich normalisiert fuehrende Nullen.
- Neu umgesetzt: Warnhinweis bei >=90% nicht zugeordnet / nicht im TR-AG-Stamm, mit Test abgesichert.
- Neu erstellt: kompaktes Andreas-Memo `docs/FINANCE_MEMO_ANDREAS_2026-06-01.md`.
- Neu dokumentiert: Produktsparten-Mapping fuer Group Sales Report ueber TR-AG-Artikelstamm und separate Mapping-Tabelle.
- Neu dokumentiert: Upgreat-Firewall-Freigabe muss fuer den publizierten Webserver `10.120.1.17` erfolgen, nicht fuer den lokalen Entwicklungs-PC.
- Neu umgesetzt: `Management Analyse` im Finance Cockpit hat zusaetzliche Reiter fuer Laender, Datenstatus, Abweichungen, Gutschriften-Kandidaten und Datenqualitaet.
- Neu erstellt: ABAP-Arbeitsstand fuer Produktsparten-Mapping mit Provider-Klasse, ALV-Report und Mapping-Build-Report.
- Neu umgesetzt: Produktspartenfelder im Web-Datenmodell, Gateway-Join-Konfiguration fuer `ProductDivisionRefSet` und Excel-Ausgabe.
- Neu umgesetzt und deployed: Reiter `Zentrale Spartenzuordnung` in `Management Analyse`, der Finance-Materialien gegen die fuehrende TR-AG-/SAP-Referenz prueft.
- Neu umgesetzt und deployed: Reiter `Sparten-Finanzanalyse` in `Management Analyse`, der Umsatzabdeckung und Umsatz nach Produktsparte aus der zentralen Spartenzuordnung berechnet.
- Neu umgesetzt und deployed: `Management Analyse` ist in der linken Navigation aufklappbar; direkte Links springen in Finance Summary, Laender, Datenstatus, Abweichungen, Gutschriften, Datenqualitaet, Spartenanalyse und Rohdaten Diagnose.
- Neu umgesetzt und deployed: Spartenanalyse ist als Hauptreiter mit Unterreitern `Finanzanalyse` und `Zentrale Zuordnung` strukturiert.
- Neu umgesetzt und deployed: Sparten-Finanzanalyse kann nach `PAPH1 Detail`, `Produktfamilie` oder `Produktsparte` aggregieren, optional `Top 10` anzeigen und Laender mit Flaggen darstellen.
- Neu umgesetzt und deployed: Produktsparte zeigt visuelle Kategorie-Icons fuer Gas/Density, Pressure/Druck, Temperatur/Thermostat, Switch/Schalter, Access/Zubehoer, UNASS und Sonstige.
- Neu umgesetzt und deployed: Finance-Schulung hat einen neuen Tab `Spartenanalyse` mit Navigation, Gruppierung, Top 10, Flaggen, Icons und Statusinterpretation.
- Neu umgesetzt und deployed: Browser-Favicon `wwwroot/favicon.svg` und Head-Link in `Components/App.razor`.
- Letzter dokumentierter Finance-Deploy: 2026-06-04 nach 3D-Datenanalyse-/Schnelluebersicht-Anpassungen auf `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`.
- Aktueller Stand 2026-06-05: Spanien-Scriptfixes sind committed; Server muss die aktuelle All-in-one-PS1 verwenden, nicht alte Kopien mit `(1)` und nicht den alten Wrapper.
- Spanien-Delta-Sync im Dashboard-Import wurde am 2026-06-05 publiziert. Publish brauchte kurz `app_offline.htm`, weil `BiDashboard.dll` gesperrt war; danach wurde `app_offline.htm` wieder entfernt.
- Neu umgesetzt: Linker Menuebaum ist datengetrieben und ueber `Admin > Menuestruktur` umhaengbar/sortierbar; bestehende Punkte koennen in andere Untermenues verschoben werden.
- Neu umgesetzt: Neuer Hauptpunkt `Einkauf` mit Einkaufswagen-Icon und vorbereiteter Einstiegseite `Einkauf Dashboard`.
- Neu umgesetzt: `x.pbix` als Einkaufs-/SAP-Vorlage analysiert und `Einkauf Dashboard` auf Spend, offene Bestellungen, Kontrakte, Lieferantenperformance und PBIX-Vorlagenstruktur erweitert.
- Wichtig Einkauf: Aktuell ist die Seite fachlich strukturiert, aber noch nicht live an SAP/OData angebunden; fuer Echtwerte muessen Einkaufsquellen wie `EKKOSet`, `EKPOSet`, ggf. Termin-/Kontrakt- und Lieferantenbewertungsdaten gemappt werden.
- Neu umgesetzt: `Einkauf > Datenquellen` als grafische SAP/OData-Quellenpflege analog Finance/Standorte; vorbefuellt mit `EKKOSet`, `EKPOSet`, `eketSet`, Lieferanten- und Warengruppen-Mapping, Joins und Zielmappings.
- Neu umgesetzt: `Einkauf Dashboard > 3D Simulation` mit festen Canvas-Abmessungen, Achsenbeschriftung, Diagrammarten, Labelgroesse und Szenario-Slider fuer Preis-/Wechselkurswirkung.
- Letzte Validierung: `dotnet test TrafagSalesExporter.sln --verbosity minimal` mit `83/83` Tests gruen; Test prueft auch Einkaufs-SAP-Seed mit Quellen/Joins/Mappings.

## Nachtrag 2026-06-10 India / SAGE HANA / Deploy

Ausgangslage:

- Indien (`TRIN`) schlug in den Exportlogs mit `authentication failed` fehl.
- Vergleich mit den Anfangslogs zeigte: erfolgreiche Laeufe am 2026-04-16 liefen auf `20.197.20.60:30015`; neuere Fehllauefe liefen auf `travtrp0:30015`.
- Ursache war Konfigurationsdrift: `TRIN` stand auf `SourceSystem=BI1`, der India-Server hatte kein `SourceSystem`, und der zentrale `SAGE`-Server hatte keinen Host.

Umgesetzt:

- Seed-Reparatur in `DatabaseSeedService`: `TRIN`/Indien wird auf `SAGE`, Schema `TRAFAG_LIVE` und SAGE-HANA `20.197.20.60:30015` gesetzt.
- Regressionstest `InitializeAsync_Repairs_India_Sage_Hana_Mapping`.
- Produktive Server-DB mit Backup direkt auf der Freigabe repariert.

Validierung:

- `dotnet test TrafagSalesExporter.Tests\TrafagSalesExporter.Tests.csproj --verbosity minimal`
- Ergebnis: `84/84` Tests gruen.
- Server-DB-Pruefung: `TRIN -> SAGE -> 20.197.20.60:30015`, User-Override `TRAFAGCONTROLS`, Passwort-Override vorhanden.

Deploy:

- Release-Publish auf `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`.
- `BiDashboard.dll` Zeitstempel nach Deploy: `10.06.2026 08:20:25`.
- `app_offline.htm` wurde nach Publish/DB-Seed entfernt.
- Commit: `586adc3 Fix India SAGE HANA mapping`.

## Nachtrag 2026-06-10 Dokumentationsdelta

- Isolierter Kursworkflow angelegt: `docs/FINANCE_KURS_WORKFLOW_2026-06-09.md`.
- SVG-Visualisierung angelegt: `docs/FINANCE_KURS_WORKFLOW_2026-06-09.svg`.
- Aktive Markdown-Dokumente bereinigt: historische Finance-Stubs und die alte deutsche Spain-rclone-Anleitung entfernt.
- Fachinhalte bleiben erhalten: alte Volltexte liegen in `docs/raw_md_archive/HISTORY_CANONICAL.md.raw`.
- Spain-Finalpaket unter den bestehenden Spain-Paketordner verschoben: `SageSpainExportPackage/SageSpainFinalExportPackage/`.

## Nachtrag 2026-06-08 Alphaplan Deutschland / SQL Discovery / rclone Upload

Ziel:

- Deutschland nutzt Alphaplan.
- Vor einer finalen Importanpassung sollen direkt auf dem deutschen Alphaplan-/SQL-Server relevante Datenbanken, Tabellen und Views gefunden werden.
- Die Discovery-Ergebnisse sollen analog Spanien per `rclone` nach SharePoint hochgeladen werden.

Erstellte Dateien:

- `docs/ALPHAPLAN_SQL_RCLONE_KONZEPT_DE_2026-06-08.md`
- `docs/ALPHAPLAN_DISCOVERY_EXPORTER_GUIDE_2026-06-08.md`
- `AlphaplanExportPackage/Run-AlphaplanDiscoveryAndUpload.ps1`
- `AlphaplanExportPackage/README.txt`
- `AlphaplanExportPackage.zip`

SharePoint-Ziel:

```text
trafag-bi:Import/Finance/Deutschland/AlphaplanRaw
```

Grund fuer eigenen Rohdatenordner:

- Die Discovery-Dateien entsprechen noch nicht dem finalen Finance-Importformat.
- Der bestehende Deutschland-Import soll nicht gestoert werden.

Script-Funktion:

- SQL Server read-only scannen.
- Wenn `-Database` leer ist: alle erreichbaren User-Datenbanken scannen.
- Kandidaten fuer Rechnung, Faktura, Beleg, Umsatz, Verkauf, Position, Artikel, Material, Kunde, Betrag, Netto, Menge, Waehrung, Warengruppe, Gutschrift und Storno erkennen.
- `candidate_objects.csv` mit Score, Spalten, Datums-/Betrags-/Key-Kandidaten schreiben.
- `export_summary.csv` schreiben.
- Optional mit `-ExportSamples` kleine `sample_*.csv` aus Top-Kandidaten schreiben.
- Optional mit `-SkipUpload` ohne SharePoint laufen.
- Standardmaessig Upload per `rclone` nach SharePoint und Verifikation von `candidate_objects.csv`.

Wichtige Befehle fuer den DE-Server:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\Run-AlphaplanDiscoveryAndUpload.ps1 -SkipUpload
```

```powershell
.\Run-AlphaplanDiscoveryAndUpload.ps1 -Database "ALPHAPLAN" -ExportSamples
```

```powershell
$cred = Get-Credential
.\Run-AlphaplanDiscoveryAndUpload.ps1 -ServerInstance "SERVERNAME\INSTANCE" -Database "ALPHAPLAN" -SqlCredential $cred
```

Default-Pfade:

- Lokaler Arbeitsordner: `C:\Trafag\AlphaplanExport`
- Runs: `C:\Trafag\AlphaplanExport\out\Alphaplan_SQL_Discovery_YYYYMMDD_HHMMSS`
- Logs: `C:\Trafag\AlphaplanExport\logs`

rclone-Kontext:

- Erwarteter Remote: `trafag-bi`
- Remote soll auf `Shared Documents` von `https://trafagag.sharepoint.com/sites/WorldwideBIPlatform` zeigen.
- Fuer Phase 1 braucht nur der DE-Server ausgehend TCP 443 zu Microsoft 365/SharePoint.
- Der BiDashboard-Server braucht keinen direkten SQL-Zugriff auf Alphaplan.

Validierung:

- PowerShell-Syntax des Scripts lokal mit Parser geprueft: OK.
- Noch kein echter Lauf gegen den deutschen Alphaplan-Server.
- Noch keine BiDashboard-Importanpassung fuer Alphaplan-Rohdaten.

Naechster Schritt:

- ZIP auf DE-Server kopieren.
- Discovery zuerst mit `-SkipUpload` starten.
- `candidate_objects.csv` von DE/IT/Andreas pruefen lassen.
- Danach finale Alphaplan-View oder gemappten CSV-Export definieren.

## Nachtrag 2026-06-08 Finance Spartenanalyse / Datenfluss Andreas

Umgesetzt lokal:

- Sparten-Finanzanalyse gruppiert standardmaessig nach `Produktsparte`.
- Gruppierungsoptionen bleiben: `Produktsparte`, `Produktfamilie`, `PAPH1 Detail`.
- Bei `Mixed`-Waehrung wird ein Warnhinweis angezeigt, weil Umsaetze aus mehreren Waehrungen numerisch addiert werden.
- Zusaetzlich wird eine Tabelle `Groesste Treiber: Nicht im Stamm` angezeigt.
- Diese Tabelle zeigt Land, TSC, Material, Bezeichnung, Umsatz und Zeilenanzahl fuer die wichtigsten nicht gematchten Materialien.

Analyseergebnis:

- Finance Summary, Management Analyse und Spartenanalyse lesen aus `CentralSalesRecords`, nicht aus dem SharePoint-Zentral-Excel.
- `Nicht im TR-AG-Stamm` entsteht, wenn Materialnummern aus lokalen Systemen nicht gegen die TR-AG-Referenz `ProductDivisionRefSet` gematcht werden.
- In der lokalen Analyse war Indien der groesste Treiber, weil lokale/Sage/B1-Materialnummern wie `DM000010`, `DM000001`, `DM000018`, `IC15415` usw. nicht in der TR-AG-Referenz gefunden wurden.
- `Mixed`-Summen sind fachlich vorsichtig zu interpretieren, weil INR/EUR/CHF/USD ohne Zielwaehrungsauswahl addiert werden.

Dokumentiert:

- `docs/FINANCE_DATENFLUSS_ANDREAS_2026-06-08.md`

Wichtige Aussagen fuer Andreas:

- Standortexport: Daten holen, Transformationen anwenden, lokale Standort-Excel erstellen, `CentralSalesRecords` fuer Standort ersetzen, optional SharePoint-Upload.
- Zentrale Excel wird aus `CentralSalesRecords` erzeugt und ist nicht die Live-Quelle der Cockpit-Anzeige.
- Wechselkurse veraendern `CentralSalesRecords` normalerweise nicht; sie wirken in Anzeige-/Analyse-Sichten bei Zielwaehrung oder in expliziten `ConvertCurrency`-Transformationen.
- Sparteninformationen kommen fuehrend aus SAP/TR-AG `ProductDivisionRefSet`; CH/AT werden direkt damit geladen, andere Laender werden in der Analyse ueber Materialnummern gematcht.

Validierung:

```text
dotnet test TrafagSalesExporter.sln --no-restore --verbosity minimal
```

Ergebnis: `83/83` Tests gruen.

## Nachtrag 2026-06-05 Einkauf / PBIX

Quelle:

- Lokale Vorlage `x.pbix` wurde geoeffnet und die Report-Struktur ausgewertet.
- PBIX-Reportseiten: Beschaffungsvolumen CHF je Lieferant, Einkaufsvolumen als Lieferanten-Kuchen, Balken Lieferant/Warengruppe, Diagramm Warengruppe, Einkaufsvolumen CHF je Region, Preisentwicklung CHF und Matrix Volumen/Warengruppe.
- In der PBIX sichtbare Felder: `EKPOSet.Netwr CHF`, `EKKOSet.Bedat`, `Data.Name`, `Data (2).WG komplett`, `EKPOSet.Matnr`, `EKPOSet.Txz01` und `EKPOSet.Netwr CHF/Stk`.

Umgesetzt:

- Einkaufsseite `/einkauf` von Platzhalter zu fachlichem Cockpit erweitert.
- Tabs: `Uebersicht`, `Spend`, `Offene Bestellungen`, `Kontrakte`, `Lieferanten`, `PBIX Vorlage`, `3D Simulation`.
- Neuer Unterpunkt `Einkauf > Datenquellen` fuer die grafische SAP/OData-Konfiguration.
- Standardquellen: `EKKO -> EKKOSet`, `EKPO -> EKPOSet`, `EKET -> eketSet`, `LIEF -> Data`, `WG -> Data2`.
- Standardjoins: `EKKO.Ebeln = EKPO.Ebeln`, `EKPO.Ebeln,Ebelp = EKET.Ebeln,Ebelp`, `EKKO.Lifnr = LIEF.Lifnr`, `EKPO.Matkl = WG.Matkl`.
- Zusaetzlich zu den PBIX-Sichten wurden die vom Einkauf genannten SAP-Themen aufgenommen:
  - Spend total vergangen nach Jahr, Lieferant, Warengruppe, Artikel.
  - Offene Bestellwerte und Mengen nach Lieferant, Warengruppe, Artikel.
  - Offene Verpflichtungen / Mengenkontrakte nach Lieferant, Warengruppe, Artikel.
  - Lieferantenbewertung / Performance nach Lieferant, Warengruppe, Artikel.

Naechster technischer Schritt:

- Live-Anbindung der Einkaufsdatenquellen definieren und implementieren. Die UI ist vorbereitet; echte Kennzahlen brauchen noch SAP/OData-Feldmapping, Filterlogik und Aktualisierungsprozess.

## Nachtrag 2026-06-05 Spanien Sage / rclone Upload

Ziel:

- Spanien soll auf dem Sage-Server selbst exportieren und die Datei automatisch nach SharePoint laden.
- Nach dem alten Vollbestand werden kuenftig nur noch Range-/Delta-Exporte benoetigt.

Server-/rclone-Kontext:

- Spanien-Server laut Chat:
  - IP: `194.30.41.41`
  - Hostname: `WIN-4BJQJ9S1PVJ`
  - VPS im Netzwerkprovider von Spanien, Wartung durch Spanien.
- Microsoft-365/rclone-Berechtigung wurde durch Admin genehmigt; rclone-Remote-Konfiguration war danach erfolgreich.
- Zielordner:
  - Browser: `https://trafagag.sharepoint.com/sites/WorldwideBIPlatform/Shared%20Documents/Import/Finance/Spanien`
  - rclone: `trafag-bi:Import/Finance/Spanien`

Umgesetzt:

- Neues Einzel-Script `SageSpainExportPackage/SageSpainFinalExportPackage/Run-SpainRangeExportAndUpload-AllInOne.ps1`.
- Das Script macht alles in einem Lauf:
  - Datum pruefen.
  - rclone finden.
  - SharePoint-Ziel pruefen/erstellen.
  - Sage-SQL direkt lesen.
  - Range-CSV und Summary schreiben.
  - CSV/Summary per rclone hochladen.
  - Upload via `rclone lsf` verifizieren.
- Default:
  - `FromDate = heute - 7 Tage`
  - `ToDate = heute`
  - `ToDate` ist exklusiv.
- Parameter koennen ueberschrieben werden:

```powershell
.\Run-SpainRangeExportAndUpload-AllInOne.ps1 -FromDate "2026-06-01" -ToDate "2026-06-04"
```

rclone-Fix:

- Fehler im Serverlog:

```text
CRITICAL: Can't set -v and --log-level
```

- Ursache: rclone darf nicht gleichzeitig mit `--verbose` / `-v` und `--log-level INFO` gestartet werden.
- Fix im All-in-one-Script:
  - `--verbose` aus dem `rclone copy` Uploadblock entfernt.
  - `--log-level INFO` bleibt erhalten.
  - Bei rclone-Fehlern werden die letzten 80 Logzeilen direkt ausgegeben.

rclone-Pfade:

- Automatische Suche prueft:
  - expliziter Parameter `-RcloneExe`
  - `rclone.exe` im Scriptordner
  - `C:\Tools\rclone.exe`
  - `C:\Tools\rclone\rclone.exe`
  - `C:\Tools\rclone\rclone\rclone.exe`
  - `rclone` aus `PATH`

Wichtige Bedienregel:

- Fuer den Ein-Datei-Betrieb immer starten:

```powershell
.\Run-SpainRangeExportAndUpload-AllInOne.ps1
```

- Nicht starten:

```powershell
.\Run-SpainExportAndUpload.ps1
```

Dieser alte Wrapper erwartet daneben `Export-SageSpainSalesCsv.ps1` und ist nicht der gewuenschte Ein-Datei-Workflow.

Commits:

- `e55a86c Add Spain all-in-one export upload script`
- `8e0b696 Default Spain export range to last seven days`
- `af097ca Fix Spain all-in-one rclone upload`
- `3fd19a8 Detect nested Spain rclone executable`

## Nachtrag 2026-06-05 Spanien Delta-Sync im Dashboard-Import

Problem:

- Der Sage-Server laedt per rclone taeglich neue Delta-Dateien in den SharePoint-Ordner.
- Dateinamen sind z. B. `Spain_Sales_range_20260528_to_20260603.csv`.
- Bisher haette ein einzelnes Delta beim Standortexport den kompletten Spanienbestand ersetzt, wenn nur dieses Delta gelesen wird.

Umgesetzt:

- `ManualExcelDataSourceAdapter` erkennt Spanien-Ordner lokal und in SharePoint.
- Fuer Spanien werden alle `Spain_Sales*.csv` gelesen, nicht nur die neueste Datei.
- SharePoint-Auswahl akzeptiert Spanien-Dateien ohne `TRES` im Namen.
- Sortierung:
  - Basis-/Vollfiles zuerst.
  - danach `Spain_Sales_range_YYYYMMDD_to_YYYYMMDD.csv` nach Datumsbereich.
- `ManualExcelImportService` liest `SourceLineId` aus dem CSV.
- Vor dem Speichern wird Spanien dedupliziert:
  - primaer `SourceLineId`.
  - Fallback `TSC + InvoiceNumber + PositionOnInvoice + Material`.
- `CentralSalesRecords` werden weiterhin pro Standort ersetzt, aber mit dem zusammengesetzten und deduplizierten Gesamtstand aus Basis + Deltas.

Wichtige Bedienregel:

- Fuer Delta-Sync muss im Standort/Manuellen Import der Ordner hinterlegt sein, nicht eine einzelne Delta-Datei.
- Beispielordner lokal/testweise: `SageSpainExportPackage`.
- Beispiel SharePoint: `Import/Finance/Spanien`.

Validierung:

```text
dotnet test TrafagSalesExporter.sln --verbosity minimal --filter ManualExcel
```

Ergebnis: `12/12` Tests gruen.

```text
dotnet test TrafagSalesExporter.sln --verbosity minimal
```

Ergebnis: `83/83` Tests gruen.

Deploy:

- `dotnet publish .\TrafagSalesExporter.csproj -c Release --no-restore /p:PublishProfile=FolderProfile --verbosity minimal`
- Ziel: `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`
- Erster Publishversuch scheiterte wegen gesperrter `BiDashboard.dll`.
- Danach `app_offline.htm` gesetzt, Publish erfolgreich ausgefuehrt und `app_offline.htm` wieder entfernt.

## Nachtrag 2026-06-04 Finance Schnelluebersicht / Experten / 3D Datenanalyse

Ziel:

- Finance Dashboard war fuer Finance/Andreas zu unuebersichtlich.
- Bestehende Funktionen bleiben erhalten, werden aber als Expertenbereich eingeordnet.
- Neue fuehrende Navigation soll links klarer sein: einfache Uebersicht zuerst, tiefe Analysen darunter.

Umgesetzt und deployed:

- Finance-Schnelluebersicht links sichtbarer gemacht.
- Bestehende tiefe Funktionen unter `Experten` zusammengefasst.
- Neuer Expertenpunkt `3D Datenanalyse`.
- 3D-Datenanalyse:
  - drehbare 3D-Ansicht mit Maus.
  - Achsenbeschriftung fuer Zeit/Werte/Indikatoren.
  - waehlbare Indikatoren erweitert.
  - Diagrammarten erweitert, u. a. Balken, Linien und weitere Analyseformen.
  - Labelgroesse in der Grafik einstellbar.
  - Canvas-/Frame-Groesse korrigiert, damit die Grafik nicht eingequetscht ist.
  - Simulation mit Schiebereglern, u. a. fuer Wechselkurs-/Szenarioaenderungen.
  - Realtime-Aktualisierung der Grafik bei Parameterveraenderungen.

Bekannte Beobachtung:

- In Chrome sah die 3D-Ansicht korrekt aus.
- In Firefox gab es auf dem Client Interaktions-/Zoomprobleme; vorerst als Browser-Hinweis merken.

Commits:

- `40805e0 Simplify finance dashboard overview`
- `b44e8ba Expose quick finance overview in navigation`
- `a8dc565 Add finance 3D data analysis`
- `13a7331 Improve finance 3D controls and simulation`
- `9409174 Fix finance 3D scenario scaling`
- `fde7f6b Add finance 3D chart modes`
- `1049216 Label finance 3D axes`
- `e33a2fd Expand finance 3D indicators`
- `9c63c36 Fix finance 3D canvas sizing`
- `cad2140 Add finance 3D label size control`

## Nachtrag 2026-06-01 Finance-Sitzung Andreas

Umgesetzt:

- ES hat laut Sitzung keine echte Ist-Abweichung. `DatabaseSeedService` setzt `FinanceReference ES 2025` auf `3'082'320.18 EUR`; `CheckValue` wird fuer ES entfernt.
- `Tools/FinanceProbe` verwendet fuer den Spain-CSV-Check ebenfalls `3'082'320.18 EUR`.
- `Settings > Export Einstellungen` hat neu `Wechselkurse anwenden auf` mit Optionen:
  - `PostingDate / Buchungsdatum`
  - `InvoiceDate / Rechnungsdatum`
  - `ExtractionDate / Extraktionsdatum`
- `Management Analyse > Rohdaten Diagnose` zeigt `Kursdatum` bzw. das fuer Wechselkurse verwendete Datumsfeld.
- `Management Analyse` erlaubt `CHF` als Anzeige-Waehrung.
- `Management Analyse > Laender` zeigt zusaetzlich:
  - `IC/2nd-party`
  - `Ist ohne IC`
- Intercompany bleibt Diagnose: Der Standard-Ist wird nicht automatisch bereinigt.
- Sparten-Zuordnung normalisiert Materialnummern fuer den Vergleich gegen TR-AG-Referenz, insbesondere fuehrende Nullen.
- Bei >=90% Umsatz in `Nicht zugeordnet` + `Nicht im TR-AG-Stamm` erzeugt die Management-Analyse einen Warnhinweis mit Pruefpunkten (`ProductDivisionRefSet`, Join, fuehrende Nullen, lokale Materialnummern, letzter ZSCHWEIZ-Export).
- Der Warnhinweis ist per Test `AnalyzeFinanceSummaryAsync_Warns_When_Product_Assignment_Coverage_Is_Implausibly_Low` abgesichert.
- Bestehender Sparten-Test prueft weiterhin, dass `000MAT-OK` in der TR-AG-Referenz zu `MAT-OK` aus einem lokalen Standort matcht.

Dokumentiert:

- `docs/FINANCE_STATUS_OFFENE_PUNKTE_2026-06-01.md`
- `docs/FINANCE_MEMO_ANDREAS_2026-06-01.md`
- `docs/rag/FINANCE.md`
- `docs/FINANCE_ENTSCHEIDE.md`
- `docs/FINANCE_BERECHNUNGSFORMELN_LAENDER_2026-05-19.md`
- `SAGE_SPAIN_EXPORT_2026-05-05.md`

Validierung:

```text
dotnet test TrafagSalesExporter.sln --verbosity minimal --artifacts-path C:\TMP\trafag-test-artifacts-finance-session-proof
```

Ergebnis:

```text
82/82 Tests gruen
```

Offen / fachlich:

- Pro Standort bestaetigen, ob Intercompany bereits in der gelieferten Quelle herausgerechnet ist.
- Fuer Wechselkurse fachlich final bestaetigen, welches Datumsfeld fuehrend ist.
- Falls die Spartenanalyse weiterhin >90% ungeklaert bleibt, TR-AG-Referenz, `ProductDivisionRefSet`, Join und lokale Materialnummern mit Andreas/Kendra pruefen.

## Nachtrag 2026-05-29 Management Analyse UX / Spartenanalyse / Favicon

Umgesetzt und deployed:

- `Management Analyse` ist in der linken Navigation als `MudNavGroup` aufklappbar.
- Direkte Navigationspunkte:
  - `Finance Summary`
  - `Laender`
  - `Datenstatus`
  - `Abweichungen`
  - `Gutschriften`
  - `Datenqualitaet`
  - `Sparten-Finanzanalyse`
  - `Zentrale Spartenzuordnung`
  - `Rohdaten Diagnose`
- Die Navigation nutzt Query-Parameter (`section`, `division`), und `ManagementCockpit.razor` bindet diese auf feste Reiter-Indizes.
- Die bisherigen Top-Level-Reiter `Sparten-Finanzanalyse` und `Zentrale Spartenzuordnung` wurden in einen Top-Level-Reiter `Spartenanalyse` mit Unterreitern zusammengefuehrt:
  - `Finanzanalyse`
  - `Zentrale Zuordnung`
- `Sparten-Finanzanalyse` hat neue Controls:
  - Dropdown `Gruppierung`: `PAPH1 Detail`, `Produktfamilie`, `Produktsparte`
  - Button `Top 10 anzeigen` mit Filter-Icon
  - dynamische Spaltenausblendung je Gruppierung
- Aggregation:
  - Umsatz, Anteil, Zeilen und Laender werden je Gruppierung neu berechnet.
  - `Top 10` filtert nur die Anzeige, nicht die zugrunde liegende Berechnungsbasis.
  - Laender werden mit Flagge formatiert.
- Visuelle Produktsparte-Icons:
  - Gas/Density -> `Sensors`
  - Pressure/Druck -> `Compress`
  - Temp/Thermostat -> `DeviceThermostat`
  - Switch/Schalter -> `ToggleOn`
  - Access/Zubehoer -> `Extension`
  - UNASS/Nicht zugeordnet -> `HelpOutline`
  - sonst -> `Category`
- Finance-Schulung:
  - Neuer Schulungs-Tab `Spartenanalyse`.
  - Dokumentiert Navigation, Gruppierung, Top 10, Flaggen, Icons und Statusinterpretation.
- Browser:
  - Neues SVG-Favicon `wwwroot/favicon.svg`.
  - Eingebunden in `Components/App.razor` via `<link rel="icon" type="image/svg+xml" href="favicon.svg" />`.

Commits:

- `dc2bc7d Group division analysis tabs`
- `0a7aafb Add management analysis navigation group`
- `3c82747 Add division finance grouping controls`
- `18208cb Add product division category icons`
- `61de1be Document division analysis in finance training`
- `674c103 Expose management analysis tabs in navigation`
- `36ca822 Add browser favicon`

Validierungen:

- Mehrfach `dotnet test TrafagSalesExporter.sln --verbosity minimal` mit separaten Artefaktpfaden.
- Letzter dokumentierter Testlauf: `80/80` Tests gruen.
- Letzter Webserver-Deploy: `BiDashboard.dll` aktualisiert am `29.05.2026 13:47:36`.

## Nachtrag 2026-05-29 Produktsparten-Mapping Gateway/Web

SAP/Gateway:

- Bestehender Service wird verwendet: `ZPOWERBI_EINKAUF_SRV`.
- Service Root: `http://travt762.sap.trafag.com:8000/sap/opu/odata/sap/ZPOWERBI_EINKAUF_SRV/`.
- Neuer Entity Type/Entity Set:
  - `ProductDivisionRef`
  - `ProductDivisionRefSet`
- Entity Type basiert auf `ZSTR_PRODSPARTE_OUT`.
- Gateway-Test liefert Daten, Beispiel:
  - `Matnr = VCP1000`
  - `Paph1 = 9999`
  - `Wwpsp = UNASS`
  - `WwpspText = Nicht zugeordnet`
- Wichtig: `FINANZDATASCHWEI_GET_ENTITYSET` ist der bestehende Sales-EntitySet und muss den alten `ZSCHWEIZ`-Select behalten. Produktspartenlogik gehoert in `PRODUCTDIVISIONR_GET_ENTITYSET`.
- Fehler `/IWFND/MED/170` wurde als fehlender Slash zwischen Service und EntitySet identifiziert.

Web/App:

- Neue Felder in `SalesRecord` und `CentralSalesRecord`:
  - `ProductHierarchyCode`
  - `ProductHierarchyText`
  - `ProductFamilyCode`
  - `ProductFamilyText`
  - `ProductDivisionCode`
  - `ProductDivisionText`
  - `ProductMappingAssigned`
- `CentralSalesRecords` erhaelt die Spalten per Schema-Maintenance.
- `CentralSalesRecordService` liest/schreibt die Felder.
- Excel-Export fuehrt die Produktfelder im Blatt `Sales` direkt nach `Product Group`.
- Manual-Excel-Header-Mapping kennt die neuen Feldnamen.
- Lokale DB-Konfiguration fuer Standort `ZSCHWEIZ`:
  - Quelle `P`: `ProductDivisionRefSet`
  - Join: `Z.Matnr = P.Matnr`
  - Mappings: `P.Paph1`, `P.Paph1Text`, `P.Wwpfa`, `P.WwpfaText`, `P.Wwpsp`, `P.WwpspText`, `P.IsAssigned`
- Lokaler Neustart durchgefuehrt; `http://localhost:55416/` antwortet mit HTTP 200.
- Validierung: `dotnet test TrafagSalesExporter.sln --verbosity minimal --artifacts-path C:\TMP\trafag-test-artifacts-productmapping` mit `79/79` Tests gruen.

Offen:

- `ZSCHWEIZ` im Export Dashboard neu laufen lassen.
- Danach Fuellung der neuen Produktfelder und Quote `UNASS` pruefen.
- Fachliche Mapping-Luecken wie `0509`/`0540` spaeter mit Andreas/Kendra klaeren.
- Wenn `TR-AG Referenz = 0` angezeigt wird, ist die zentrale Referenz im Web noch leer. Dann `ZSCHWEIZ` nach aktivem `ProductDivisionRefSet`-Join erneut exportieren/laden.

## Nachtrag 2026-05-29 Zentrale Spartenzuordnung

Umgesetzt:

- Neuer Reiter in `Management Analyse`: `Zentrale Spartenzuordnung`.
- Fachlogik:
  - Andere Laender-ERPs sind fuer Produktsparten nicht fuehrend.
  - Fuehrend ist die TR-AG-/SAP-Referenz aus `ProductDivisionRefSet`.
  - Umsatzzeilen aus `CentralSalesRecords` werden ueber `Material` gegen die TR-AG-Referenz geprueft.
- Statuswerte:
  - `Zugeordnet`
  - `Nicht zugeordnet`
  - `Nicht im TR-AG-Stamm`
  - `Material fehlt`
- Der Reiter zeigt:
  - Summary-Kennzahlen
  - Abdeckung nach Land/TSC
  - Detailtabelle mit Land-Material links und TR-AG-MATNR/PAPH1/Familie/Sparte rechts.
- Die Sicht verwendet die bestehenden Finance-Filter fuer Jahr, Land und Waehrung.
- Noch keine persistente Mutation anderer Laenderzeilen; es ist bewusst eine Pruefansicht.

Technisch:

- Neue Modelle in `ManagementCockpitModels`.
- Produktzuordnungsanalyse in `ManagementCockpitService`.
- Neuer Reiter in `Components/Pages/ManagementCockpit.razor`.
- Test ergaenzt: `AnalyzeFinanceSummaryAsync_Builds_Central_Product_Assignment_Tab_Data`.
- Validierung: `dotnet test TrafagSalesExporter.sln --verbosity minimal --artifacts-path C:\TMP\trafag-test-artifacts-central-product-assignment` mit `80/80` Tests gruen.

## Nachtrag 2026-05-29 Sparten-Finanzanalyse

Umgesetzt:

- Neuer Reiter in `Management Analyse`: `Sparten-Finanzanalyse`.
- Grundlage sind die bestehenden Statuswerte aus `Zentrale Spartenzuordnung`, damit Materialstatus und Finanzwerte identisch abgegrenzt sind.
- Kennzahlen:
  - Gesamtumsatz
  - Zugeordneter Umsatz
  - Nicht zugeordneter Umsatz
  - Umsatz nicht im TR-AG-Stamm
- Tabellen:
  - Umsatz nach Produktsparte mit Produktsparte, Produktfamilie, PAPH1, Umsatz, Anteil, Materialanzahl, Zeilen und Laendern.
  - Umsatzabdeckung nach Land/TSC mit Gesamt, Zugeordnet, Nicht zugeordnet, Nicht im Stamm, Material fehlt und Abdeckungsquote.
- Seed-Fix:
  - SAP-Quelle `P = ProductDivisionRefSet` wird beim App-Start nicht mehr deaktiviert.
  - Join `Z.Matnr = P.Matnr` und Produktfeld-Mappings werden als Standard gepflegt.
- Server-DB nach Deploy geprueft:
  - `ProductRows = 36'847`
  - `TR-AG Referenzmaterialien = 6'805`
  - `ProductDivisionRefSet` aktiv.
- Deploy: `BiDashboard.dll` auf Server aktualisiert am `29.05.2026 10:42`.
- Validierung: `dotnet test TrafagSalesExporter.sln --verbosity minimal --artifacts-path C:\TMP\trafag-test-artifacts-division-finance` mit `80/80` Tests gruen.

## Nachtrag 2026-05-28 ABAP Produktsparten-Mapping

Erstellt:

- `docs/abap/ZCL_PRODSPARTE_PROVIDER.abap`
- `docs/abap/Z_PRODSPARTE_REPORT.abap`
- `docs/abap/Z_PRODSPARTE_MAP_BUILD.abap`
- `docs/abap/README_PRODSPARTE.md`

Dokumentierter Zielansatz:

- SAP TR AG bleibt Quelle der Wahrheit fuer `MATNR -> PAPH1 -> WWPFA -> WWPSP`.
- Mapping-Build liest reale CO-PA-Ableitungen aus `CE11000` und schreibt eindeutige Saetze in `ZPRODSPARTE_MAP`.
- Provider liest verkaufsrelevante Materialien aus `MVKE`, Texte aus SAP-Texttabellen und Mapping aus `ZPRODSPARTE_MAP`.
- ALV-Report und spaeter OData sollen dieselbe Provider-Methode verwenden.
- Nicht zugeordnete Materialien erhalten Fallback `UNASS` / `Nicht zugeordnet`.

Offen:

- `PAPH1 = MVKE-PRODH(5)` bestaetigen.
- Texttabellen `T25A0`/`T25A1` bestaetigen.
- Relevante `VKORG`/`VTWEG` fuer TR AG festlegen.
- `CE11000` als richtige CO-PA-Quelle bestaetigen.

## Nachtrag 2026-05-28 Finance Management Analyse Reiter

Umgesetzt:

- `Management Analyse` erweitert die bestehende `Finance Summary` um weitere Reiter im Cockpit-Stil.
- Neue Reiter:
  - `Laender`
  - `Datenstatus`
  - `Abweichungen`
  - `Gutschriften`
  - `Datenqualitaet`
- Grundlage sind vorhandene Daten aus `CentralSalesRecords`, `FinanceReferences`, `Sites` und `ExportLogs`.
- Keine neuen Fachregeln eingefuehrt:
  - Gutschriften-Reiter zeigt technische Kandidaten.
  - Datenqualitaet zeigt technische Pruefpunkte.
  - Produktsparten-/Produktfamilienlogik bleibt bis Kendra-Mapping offen.
- Test ergaenzt: `AnalyzeFinanceSummaryAsync_Builds_Dashboard_Tab_Data`.
- Validierung: `dotnet test TrafagSalesExporter.sln --verbosity minimal` mit `79/79` Tests gruen.

## Nachtrag 2026-05-27 Produktsparten-Mapping

Dokumentiert:

- Neue Detaildoku `docs/PRODUCT_SPARTEN_MAPPING_2026-05-27.md`.
- Neue RAG-Kurzdatei `docs/rag/PRODUCT_MAPPING.md`.
- Router-Eintrag fuer Themen `Group Sales Report`, `Produkthierarchie`, `Produktfamilie`, `Produktsparte`.
- Fachliche Annahme: Materialnummern aus Group Sales Report werden gegen TR-AG-Artikelstamm aufgeloest; nicht gefundene Artikel laufen unter `Sonstige/ohne Zuordnung`.
- Offene Sitzungspunkte: Quelle des Artikelstamms, Bedeutung von `Z.Prodh`, Mapping-Tabelle von Kendra, Range-/Prefix-Regeln, Historisierung.

## Volltext Bei Bedarf

Die kanonische Detailhistorie liegt hier:

```text
docs/raw_md_archive/HISTORY_CANONICAL.md.raw
```

Die frueheren Original-Volltexte liegen als Wiederherstellungs-Backup hier:

```text
docs/raw_md_archive/original_history_raws.zip
```

Nur laden, wenn genaue Chronologie, alte Zwischenstaende, Commit-Historie oder Audit-Spuren benoetigt werden.

## Nachtrag 2026-06-08 Einkauf Server-DB Restore

Server-DB wiederhergestellt und Einkauf-Full-Load abgeschlossen:

- Ursache: Runtime-DB wurde frueher beim Publish mitkopiert und hatte die produktive Server-DB geleert. Das Projektfile ist bereits korrigiert, DB/WAL/SHM werden nicht mehr publiziert.
- Server-DB zuerst aus lokaler Haupt-DB wiederhergestellt:
  - `CentralSalesRecords`: 75'089
  - Navigation und SAP-Credentials wieder vorhanden.
- Einkauf-Full-Load lokal gegen DB-Kopie ausgefuehrt, nicht direkt auf UNC:
  - Arbeitsordner: `C:\TMP\purchasing-fullload-20260607-205623`
  - `PurchasingEkkoCache`: 172'874
  - `PurchasingEkpoCache`: 233'921
  - `PurchasingEketCache`: 242'572
- Gefuellte DB auf Server kopiert.
- Alte SQLite-Sidecar-Dateien `trafag_exporter.db-wal` und `trafag_exporter.db-shm` auf dem Server gesichert und entfernt, weil sie nicht zur neuen Haupt-DB passten und `SQLite Error 11: database disk image is malformed` verursachten.
- Verifikation:
  - Server-DB read-only geprueft mit korrekten Counts.
  - HTTP-Check `https://trch-webapp-bidashboard.trafagch.local/BiDashboard/`: Status 200.

Backups auf Server:

- `trafag_exporter.db.before-restore-20260605-144709.bak`
- `trafag_exporter.db.before-purchasing-fullload-20260608-061149.bak`
- `trafag_exporter.db-wal.before-cleanup-20260608-065012.bak`
- `trafag_exporter.db-shm.before-cleanup-20260608-065012.bak`
