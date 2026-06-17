# Finance Dashboard Nachweis-Excel

Stand: 2026-06-17

## Zweck

Finance soll neben dem Dashboard auch in Excel nachvollziehen koennen, wie die Dashboard-Ergebnisse entstehen. Deshalb erzeugt `Zentrale Datei neu erzeugen` zusaetzlich zur bestehenden zentralen Datei eine zweite Nachweis-Datei:

```text
Finance_Dashboard_Nachweis_<yyyy-MM-dd>.xlsx
```

Die bestehende Datei `Sales_All_<yyyy-MM-dd>.xlsx` bleibt unveraendert.

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
4. Optional `Finance_Dashboard_Nachweis_<Datum>.xlsx` erzeugen und nach SharePoint laden.

Der Grund ist die Dateigroesse: Das Nachweis-Excel enthaelt mehrere grosse Detailblaetter mit Formeln und kann bei sehr grossen Zentraldaten sehr lange laufen. Damit der produktive zentrale Export nicht blockiert, wird das Nachweis-Excel im gleichen Buttonlauf nur bis `50'000` Zentralzeilen erzeugt. Bei groesseren Datenmengen wird es uebersprungen und als Warnung in den App-Logs dokumentiert.

In diesem Fall ist `Finance_Dashboard_Audit_All_<Datum>.csv` der vollstaendige Detailnachweis fuer Finance. Sie enthaelt alle zentralen Audit-/Merge-Zeilen inkl. Sparten- und Kostenbasisfeldern und ist fuer grosse Datenmengen belastbarer als ein formelbasiertes Excel-Workbook.

## Technische Stellen

- `Services/ConsolidatedExportService.cs`: erzeugt zentrale Datei, zentrale Audit-CSV und optional Nachweis im gleichen Output-Ordner und laedt die Dateien progressiv nach SharePoint hoch. Das Nachweis-Excel wird bei mehr als `50'000` Zentralzeilen im Inline-Lauf uebersprungen.
- `Services/ExportAuditCsvService.cs`: schreibt Standort-Audit-CSV fuer `Sales_ProcessedMergeInput_*` und zentrale Nachweis-CSV `Finance_Dashboard_Audit_All_*`.
- `Services/ExcelExportService.cs`: baut die Nachweis-Workbook-Struktur und Formeln.
- `Services/DashboardPageService.cs`: zeigt die letzte zentrale Datei und den letzten Dashboard-Nachweis im Export-Dashboard.
- `Components/Pages/Settings.razor`: UI-Text fuer den gemeinsamen waehlbaren Ordner.
