# Finance Dashboard Nachweis-Excel

Stand: 2026-06-17

## Zweck

Finance soll neben dem Dashboard auch in Excel nachvollziehen koennen, wie die Dashboard-Ergebnisse entstehen. Deshalb erzeugt `Zentrale Datei neu erzeugen` zusaetzlich zur bestehenden zentralen Datei eine zweite Nachweis-Datei:

```text
Finance_Dashboard_Nachweis_<yyyy-MM-dd>.xlsx
```

Die bestehende Datei `Sales_All_<yyyy-MM-dd>.xlsx` bleibt unveraendert.

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

Wenn ein SharePoint-Ziel fuer zentrale Exporte konfiguriert ist, werden beide Dateien hochgeladen:

- `Sales_All_<yyyy-MM-dd>.xlsx`
- `Finance_Dashboard_Nachweis_<yyyy-MM-dd>.xlsx`

## Technische Stellen

- `Services/ConsolidatedExportService.cs`: erzeugt zentrale Datei und Nachweis im gleichen Output-Ordner.
- `Services/ExcelExportService.cs`: baut die Nachweis-Workbook-Struktur und Formeln.
- `Services/DashboardPageService.cs`: zeigt die letzte zentrale Datei und den letzten Dashboard-Nachweis im Export-Dashboard.
- `Components/Pages/Settings.razor`: UI-Text fuer den gemeinsamen waehlbaren Ordner.
