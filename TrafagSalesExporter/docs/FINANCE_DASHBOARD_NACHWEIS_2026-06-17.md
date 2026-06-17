# Finance Dashboard Nachweis-Excel

Stand: 2026-06-17

## Zweck

Finance soll neben dem Dashboard auch in Excel nachvollziehen koennen, wie die Dashboard-Ergebnisse entstehen. Deshalb erzeugt `Zentrale Datei neu erzeugen` zusaetzlich zur bestehenden zentralen Datei eine zweite Nachweis-Datei:

```text
Finance_Dashboard_Nachweis_<yyyy-MM-dd>.xlsx
```

Die bestehende Datei `Sales_All_<yyyy-MM-dd>.xlsx` bleibt unveraendert.

Bei grossen zentralen Datenmengen wird der Nachweis nicht als eine grosse Datei, sondern als mehrere kleinere Excel-Dateien erzeugt:

```text
Finance_Dashboard_Nachweis_<TSC>_<Land>_<yyyy-MM-dd>.xlsx
Finance_Dashboard_Nachweis_<TSC>_<Land>_Teil01_<yyyy-MM-dd>.xlsx
Finance_Dashboard_Nachweis_<TSC>_<Land>_Teil02_<yyyy-MM-dd>.xlsx
```

Seit 2026-06-17 wird zusaetzlich eine zentrale Audit-CSV fuer Finance erzeugt:

```text
Finance_Dashboard_Audit_All_<yyyy-MM-dd>.csv
```

Diese Datei enthaelt die aufbereiteten Audit-/Merge-Felder inkl. Produktsparte, Produktfamilie, Produktmapping-Status, Umsatz, Waehrungen und Kostenbasis-Felder.

## Ablage

Beide Dateien werden in denselben waehlbaren lokalen Ordner geschrieben:

1. `ExportSettings.LocalConsolidatedExportFolder`, wenn gesetzt.
2. Sonst `ExportSettings.LocalSiteExportFolder`, wenn gesetzt.
3. Sonst `output` unter dem Programmverzeichnis.

Im UI heisst die Einstellung jetzt `Lokaler Pfad Zentrale Datei und Nachweis`.

## Arbeitsblaetter

- `Datenherkunft`: Datum, Quelle, Zeilenzaehler, Laender/Waehrungen.
- `Finance Summary`: Formel-Summary ueber `Finance Details`.
- `Finance Details`: zentrale Detailzeilen mit Finance-Regeln, Include-Flag und Net Sales Actual.
- `Soll Ist`: Referenzwerte aus `FinanceReferences` gegen Finance-Ist per Formel.
- `Sparten Summary`: Formel-Summary ueber `Sparten Details`.
- `Sparten Details`: Material-/Spartenstatus aus der zentralen TR-AG-/SAP-Zuordnung.
- `Gruppenmarge Summary`: Formel-Summary ueber `Gruppenmarge Details`.
- `Gruppenmarge Details`: Umsatz, Kostenbasis, Status und Zeilenmarge.
- `Datenqualitaet`: COUNTIF/COUNTIFS-Pruefungen auf offene Datenpunkte.
- `Formel Hilfe`: Kurzerklaerung der Blaetter.

## Formelprinzip

Die Summary-Blaetter enthalten Excel-Formeln wie `SUMIFS`, `COUNTIFS` und `IF` auf die Detailblaetter. Damit kann Andreas in Excel pruefen, ob die Summen aus den Detailzeilen entstehen.

Die Gruppenmarge bleibt bewusst leer, wenn offene Kostenbasis vorhanden ist. In der Summary wird `Margin Value` und `Margin %` nur berechnet, wenn `Open Cost Rows = 0`.

## SharePoint

Wenn ein SharePoint-Ziel fuer zentrale Exporte konfiguriert ist, werden die zentralen Dateien hochgeladen:

- `Sales_All_<yyyy-MM-dd>.xlsx`
- `Finance_Dashboard_Nachweis_<yyyy-MM-dd>.xlsx`
- `Finance_Dashboard_Audit_All_<yyyy-MM-dd>.csv`

Produktiv ist `SharePointConfigs.CentralExportFolder` auf `/Import/Finance/Alle` gesetzt. Damit landen die konsolidierten Dateien im zentralen Finance-Ordner `Import/Finance/Alle`, waehrend die Standortexporte weiterhin in den jeweiligen Laenderordnern bleiben.

Vor dem zentralen Export prueft die App pro aktivem Standort die neueste `Sales_ProcessedMergeInput_*`-CSV im SharePoint-/Auditpfad und vergleicht sie mit dem DB-Stand in `CentralSalesRecords`. Fuer `Sales_All_*`, `Finance_Dashboard_Nachweis_*` und `Finance_Dashboard_Audit_All_*` wird pro Land/TSC der jeweils neueste Stand verwendet.

Die Spalte `Letzte Aenderung` im Export Dashboard > `Zentrale Datei` kommt vom lokalen Datei-Zeitstempel des erzeugten Server-Files (`FileInfo.LastWriteTime`). Sie ist nicht der SharePoint-Modified-Zeitstempel.

Die zentrale Audit-CSV nutzt bewusst kein `Sales_*`-Praefix. Grund: `Sales_ProcessedMergeInput_*` und historische `Sales_*`-CSV werden als zentrale Audit-Input-Dateien erkannt. Die neue `Finance_Dashboard_Audit_All_*`-Datei ist ein Nachweis-/Exportartefakt und darf nicht als weiteres TSC/Land erneut eingelesen werden.

## Laufzeitverhalten bei grossen Datenmengen

Seit 2026-06-17 wird der zentrale SharePoint-Upload progressiv ausgefuehrt:

1. Neueste Laenderdateien pruefen und lokal synchronisieren.
2. `Sales_All_<Datum>.xlsx` erzeugen und sofort nach SharePoint `Import/Finance/Alle` laden.
3. `Finance_Dashboard_Audit_All_<Datum>.csv` erzeugen und nach SharePoint laden.
4. `Finance_Dashboard_Nachweis_<Datum>.xlsx` oder mehrere kleinere `Finance_Dashboard_Nachweis_<TSC>_<Land>_<Datum>.xlsx` erzeugen und nach SharePoint laden.

Der Grund ist die Dateigroesse und Workbook-Komplexitaet: Das Nachweis-Excel enthaelt mehrere grosse Detailblaetter mit Formeln und kann bei sehr grossen Zentraldaten sehr lange laufen. Damit Finance trotzdem Excel-Dateien erhaelt, wird ab mehr als `50'000` Zentralzeilen automatisch partitioniert:

- Gruppierung primaer pro `TSC` und `Land`.
- Maximal ca. `25'000` Detailzeilen pro Nachweis-Workbook.
- Falls ein Land/TSC groesser ist, entstehen mehrere Dateien mit `_Teil01`, `_Teil02` usw.

Die zentrale `Finance_Dashboard_Audit_All_<Datum>.csv` bleibt weiterhin der vollstaendige Detailnachweis ueber alle Laender. Die kleinen Excel-Nachweise sind fuer die gezielte Finance-Pruefung pro Land/TSC gedacht.

## Pruefstand 2026-06-17

Nach abgeschlossener Server-Generierung wurden die erzeugten Nachweis-Excel-Dateien im produktiven Output-Ordner gegen die zentrale Audit-CSV geprueft:

- Audit-Datei: `Finance_Dashboard_Audit_All_2026-06-17.csv`
- Nachweis-Dateien: `Finance_Dashboard_Nachweis_*_2026-06-17.xlsx`
- Audit-Gesamtzeilen: `112'749`
- Nachweis-Gesamtzeilen ueber alle Excel-Dateien: `112'749`
- Ergebnis: je TSC `delta=0`, keine Scope-Fehler bei TSC/Land.

Gepruefte Zeilen je Nachweis-Datei:

| Datei | Detailzeilen | Finance Include TRUE | TSC-Pruefung | Land-Pruefung |
| --- | ---: | ---: | --- | --- |
| `Finance_Dashboard_Nachweis_TRAT_AT_2026-06-17.xlsx` | `2'562` | `2'557` | OK | OK |
| `Finance_Dashboard_Nachweis_TRCH_CH_Teil01_2026-06-17.xlsx` | `25'000` | `23'828` | OK | OK |
| `Finance_Dashboard_Nachweis_TRCH_CH_Teil02_2026-06-17.xlsx` | `25'000` | `23'384` | OK | OK |
| `Finance_Dashboard_Nachweis_TRCH_CH_Teil03_2026-06-17.xlsx` | `18'372` | `15'995` | OK | OK |
| `Finance_Dashboard_Nachweis_TRDE_Deutschland_2026-06-17.xlsx` | `4'534` | `4'145` | OK | OK |
| `Finance_Dashboard_Nachweis_TRES_Spanien_2026-06-17.xlsx` | `9'254` | `7'952` | OK | OK |
| `Finance_Dashboard_Nachweis_TRFR_Frankreich_2026-06-17.xlsx` | `2'399` | `2'322` | OK | OK |
| `Finance_Dashboard_Nachweis_TRIN_Indien_2026-06-17.xlsx` | `6'384` | `6'384` | OK | OK |
| `Finance_Dashboard_Nachweis_TRIT_Italien_2026-06-17.xlsx` | `17'896` | `17'159` | OK | OK |
| `Finance_Dashboard_Nachweis_TRUK_England_2026-06-17.xlsx` | `4` | `4` | OK | OK |
| `Finance_Dashboard_Nachweis_TRUS_USA_2026-06-17.xlsx` | `1'344` | `1'327` | OK | OK |

Vergleich je TSC gegen die Audit-CSV:

| TSC | Audit-CSV | Nachweis-Excel | Delta |
| --- | ---: | ---: | ---: |
| TRAT | `2'562` | `2'562` | `0` |
| TRCH | `68'372` | `68'372` | `0` |
| TRDE | `4'534` | `4'534` | `0` |
| TRES | `9'254` | `9'254` | `0` |
| TRFR | `2'399` | `2'399` | `0` |
| TRIN | `6'384` | `6'384` | `0` |
| TRIT | `17'896` | `17'896` | `0` |
| TRUK | `4` | `4` | `0` |
| TRUS | `1'344` | `1'344` | `0` |

Die Workbook-Struktur wurde ebenfalls geprueft: `Datenherkunft`, `Finance Summary`, `Finance Details`, `Soll Ist`, `Sparten Summary`, `Sparten Details`, `Gruppenmarge Summary`, `Gruppenmarge Details`, `Datenqualitaet` und `Formel Hilfe` sind vorhanden. Die Kernformeln in `Finance Summary`, `Sparten Summary` und `Gruppenmarge Summary` verweisen per `SUMIFS`/`COUNTIFS` auf die jeweiligen Detailblaetter.

Filterbezug zum Dashboard: Die Nachweis-Dateien enthalten in den Detail- und Summary-Blaettern `Year`, `Country Key`, `Currency` und je nach Blatt `TSC` sowie Sparte. Damit kann Finance dieselbe Sicht wie im Dashboard nachstellen, indem im Excel nach Jahr, Land/Country Key und Waehrung gefiltert wird.

## Technische Stellen

- `Services/ConsolidatedExportService.cs`: erzeugt zentrale Datei, zentrale Audit-CSV und Nachweis-Excel im gleichen Output-Ordner und laedt die Dateien progressiv nach SharePoint hoch. Das Nachweis-Excel wird bei mehr als `50'000` Zentralzeilen in kleine Dateien pro TSC/Land mit maximal ca. `25'000` Zeilen partitioniert.
- `Services/ExportAuditCsvService.cs`: schreibt Standort-Audit-CSV fuer `Sales_ProcessedMergeInput_*` und zentrale Nachweis-CSV `Finance_Dashboard_Audit_All_*`.
- `Services/ExcelExportService.cs`: baut die Nachweis-Workbook-Struktur, Formeln und optionale Scope-Dateinamen.
- `Services/DashboardPageService.cs`: zeigt die letzte zentrale Datei und den letzten Dashboard-Nachweis im Export-Dashboard.
- `Components/Pages/Settings.razor`: UI-Text fuer den gemeinsamen waehlbaren Ordner.
