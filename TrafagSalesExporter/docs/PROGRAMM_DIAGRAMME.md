# Programm-Diagramme

Stand: 2026-05-20

## Nachtrag Anwenderdokus 2026-05-20

Neben den technischen SVG-Diagrammen gibt es jetzt zwei gestaltete Word-Anleitungen mit eingebetteten neutralen Cockpit-Vorschaugrafiken:

- `docs/HR_KPI_ANLEITUNG_HR_2026-05-20.docx`
- `docs/FINANCE_COCKPIT_ANLEITUNG_FINANZ_2026-05-20.docx`
- `docs/hr_kpi_cockpit_preview.png`
- `docs/finance_cockpit_preview.png`

Die Word-Dokus sind fuer Anwender gedacht. Die SVG-/Markdown-Dokumente bleiben fuer technische Nachvollziehbarkeit, Architektur und Pruefpfade.

## Empfohlene Diagrammarten

Fuer das Programm bieten sich zwei Diagrammarten an:

- **User Story Map** fuer Rollen, fachliche Aktivitaeten und Ausbaustufen.
- **Swimlane-Prozessdiagramm** fuer den Ablauf von Quellsystem bis Finance-Abgleich, weil Verantwortung und Datenfluss getrennt sichtbar werden.

## Dateien

- `docs/FINANCE_ENTSCHEIDE.md`
  - dokumentiert die verbindlichen Financechef-Entscheide fuer Waehrung, Budgetkurse, Nettofakturawert, Buchungsdatum, Gutschriften und Intercompany
  - ist die fachliche Grundlage fuer FinanceProbe und den Soll/Ist-Abgleich

- `docs/program-user-stories.svg`
  - zeigt Finance, Power User/Admin und IT/SAP als Rollen
  - ordnet Stories nach Quellenpflege, Mapping, Import, Konsolidierung, Finance-Abgleich und Betrieb
  - markiert Kernfunktionen, naechsten Ausbau und Kontrollpunkte

- `docs/program-process-plan.svg`
  - zeigt den Prozess als Swimlanes
  - enthaelt SAP ZSCHWEIZ, SAP OData, SAP HANA/BI1, Manual Excel/CSV
  - zeigt den zentralen Weg ueber grafisches Mapping, `MappedSalesRecordComposer`, `CentralSalesRecords`, Finance-Abgleich und Export
  - markiert bewusste Rest-Doppelspuren wie HANA-B1-Legacy und den offenen Ausbau fuer Finance-Regelpflege

- `docs/finance-land-algorithms.svg`
  - zeigt fuer Finance den buchhalterischen Fluss je Land
  - beschreibt Quelle, Mapping, Hauswaehrung, Nettofakturawert, Buchungsdatum, IC-Ausweis und Sollvergleich
  - macht sichtbar, dass der Algorithmus regelbasiert ist und nicht auf einzelne Testzahlen frisiert wurde

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
- `PostingDate` ist die fuehrende Jahresabgrenzung. Falls eine Quelle kein Buchungsdatum liefert, faellt der Code auf `InvoiceDate` und danach `ExtractionDate` zurueck.
- Hauswaehrung ist fuehrend. CHF wird als Budgetkurs-Kandidat gerechnet, nicht als Tageskurs-Standard.
- Belegkopfwerte wie `DocTotal - VatSum` werden nicht blind pro Position multipliziert. Der Code erkennt wiederholte Headerwerte und zeigt Positionswert sowie deduplizierten Belegwert als Kandidaten.
- Der ausgewaehlte Finance-Wert ist daher ein Ist-Kandidat, nicht pauschal immer eine Positionssumme.

## FinanceProbe starten

Normale Ansicht:

```powershell
dotnet run --project .\Tools\FinanceProbe\FinanceProbe.csproj --urls http://127.0.0.1:5099
```

Danach im Browser:

```text
http://127.0.0.1:5099/finance
```

Export-/Prueflaeufe:

```text
http://127.0.0.1:5099/run/export-all
http://127.0.0.1:5099/run/consolidated
http://127.0.0.1:5099/run/export/TRUK
```

Wenn der Build-Output durch ein laufendes Programm gesperrt ist, zuerst den alten `dotnet`-Prozess beenden oder ohne Rebuild die vorhandene DLL starten:

```powershell
dotnet .\Tools\FinanceProbe\bin\Debug\net8.0\FinanceProbe.dll --urls http://127.0.0.1:5099
```

Hinweis fuer das Testprogramm:

- FinanceProbe verwendet Console-Logging, damit lokale Windows-EventLog-Rechte den Prueflauf nicht abbrechen.
- Falls Visual Studio oder ein alter `dotnet`-Prozess DLLs sperrt, den Prozess beenden und danach neu bauen/starten.
- Der aktuelle Entwicklungs-Pruefstand wurde zusaetzlich mit einem separaten Output unter `.codex\memories\financeprobe_check\out` gebaut, um Build-Locks zu umgehen.

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
