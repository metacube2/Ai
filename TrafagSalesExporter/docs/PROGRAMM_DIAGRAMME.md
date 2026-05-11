# Programm-Diagramme

## Empfohlene Diagrammarten

Fuer das Programm bieten sich zwei Diagrammarten an:

- **User Story Map** fuer Rollen, fachliche Aktivitaeten und Ausbaustufen.
- **Swimlane-Prozessdiagramm** fuer den Ablauf von Quellsystem bis Finance-Abgleich, weil Verantwortung und Datenfluss getrennt sichtbar werden.

## Dateien

- `docs/program-user-stories.svg`
  - zeigt Finance, Power User/Admin und IT/SAP als Rollen
  - ordnet Stories nach Quellenpflege, Mapping, Import, Konsolidierung, Finance-Abgleich und Betrieb
  - markiert Kernfunktionen, naechsten Ausbau und Kontrollpunkte

- `docs/program-process-plan.svg`
  - zeigt den Prozess als Swimlanes
  - enthaelt SAP ZSCHWEIZ, SAP OData, SAP HANA/BI1, Manual Excel/CSV
  - zeigt den zentralen Weg ueber grafisches Mapping, `MappedSalesRecordComposer`, `CentralSalesRecords`, Finance-Abgleich und Export
  - markiert bewusste Rest-Doppelspuren wie HANA-B1-Legacy und den offenen Ausbau fuer Finance-Regelpflege

## Abgleich gegen Quellcode

Die Diagramme wurden gegen folgende Codebereiche abgeglichen:

- `Program.cs`: registrierte Adapter und Services
- `Services/DataSources/*`: HANA, SAP Gateway und Manual Excel/CSV Adapter
- `Services/SiteExportService.cs`: Standortexport, Transformation, Excel-Erzeugung, zentrale Speicherung, SharePoint-Upload
- `Services/ExportOrchestrationService.cs`: Export aller aktiven Standorte und anschliessender konsolidierter Export
- `Services/ConsolidatedExportService.cs`: zentrale Datei aus `CentralSalesRecords`
- `Services/MappedSalesRecordComposer.cs`: gemeinsame Mapping-Engine fuer SAP OData und generisches HANA-Mapping
- `Services/FinanceReconciliationService.cs`: Soll/Ist-Kandidaten, Budgetkurse, IC-Regeln und Ampelstatus
- `Services/DatabaseSeedService.cs`: Seed fuer Quellsysteme, ZSCHWEIZ, Finance-Referenzen, Budgetkurse und IC-Regeln
- `Data/AppDbContext.cs`: relevante Tabellen

Wichtige Praezisierung aus dem Code:

- `SalesPriceValue` wird im Finance-Abgleich positionsweise summiert.
- Belegkopfwerte wie `DocTotal - VatSum` werden vor der Summierung pro Beleg dedupliziert.
- Der ausgewaehlte Finance-Wert ist daher ein Ist-Kandidat, nicht pauschal immer eine Positionssumme.

## Einsatz

Die SVG-Dateien koennen direkt im Browser geoeffnet, in Markdown verlinkt oder in Praesentationen eingefuegt werden.

## Nachtrag Manual Excel/CSV 2026-05-08

Die Diagramme zeigen Manual Excel/CSV als Quelle. Die aktuelle Detailregel dazu ist:

- Eine konkrete lokale Datei wird gelesen; die erzeugte Exportdatei wird im gleichen lokalen Ordner abgelegt.
- Eine konkrete SharePoint-Datei wird gelesen; die erzeugte Exportdatei wird im gleichen SharePoint-Ordner abgelegt.
- Eine SharePoint-Ordnerreferenz wird als dynamische Quelle behandelt.
- Bei SharePoint-Ordnern wird die neueste passende `.xlsx`/`.csv` gesucht.
- Fuer England/TRUK gilt das Dateimuster `ddMMyy_TRUK.xlsx`, z. B. `010526_TRUK.xlsx`.
- Die Ordnerlogik ist generisch fuer Manual-Quellen, nicht hart nur fuer England implementiert.
