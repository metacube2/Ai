# Last Change 2026-05-04

## Manual Excel/CSV SharePoint-Ordner und Quellordner-Export 2026-05-08

Umgesetzte Anpassungen:

- Manual Excel/CSV Quellen erzeugen nun immer eine neue Exportdatei; die Quelldatei wird nicht als Exportdatei weitergereicht.
- Lokale Manual-Dateien schreiben die neue Exportdatei in denselben lokalen Ordner wie die Quelldatei.
- SharePoint-Manual-Dateien schreiben die neue Exportdatei in denselben SharePoint-Ordner wie die Quelldatei.
- SharePoint-Referenzen ohne Dateiendung werden als Ordner behandelt.
- Bei SharePoint-Ordnern sucht die App die neueste passende Excel-/CSV-Datei fuer den Standort.
- Fuer datierte Dateien wird das Muster `ddMMyy_TSC.xlsx` bzw. `ddMMyy_TSC.csv` ausgewertet.
- Beispiel England/UK:
  - Ordner: `https://trafagag.sharepoint.com/sites/WorldwideBIPlatform/Import/Finance/UK_B1`
  - `010526_TRUK.xlsx` wird vor `010426_TRUK.xlsx` gewaehlt.
  - Falls kein Datum aus dem Dateinamen gelesen werden kann, faellt die Auswahl auf das SharePoint-Aenderungsdatum zurueck.

Technischer Befund aus den Logs:

- Spanien konnte die SharePoint-Datei lesen (`4'341` Zeilen), fiel danach aber auf einen ungueltigen lokalen Pfad, weil die URL als lokale Exportdatei behandelt wurde.
- Fehlerpfad war sinngemaess `...\https:\trafagag.sharepoint.com\...\Spain_Sales_2025.csv`.
- Deutschland hatte keinen manuellen Dateipfad hinterlegt.
- England/TRUK zeigte lokal versehentlich auf die Deutschland-Alphaplan-Datei; die lokale DB wurde auf den UK_B1-Ordner korrigiert.

Codeaenderungen:

- `DataSourceFetchResult` enthaelt optionale Overrides fuer lokalen Output-Ordner und SharePoint-Zielordner.
- `ManualExcelDataSourceAdapter` erkennt SharePoint-Dateien vs. SharePoint-Ordner und waehlt bei Ordnern die neueste passende Datei.
- `SharePointUploadService` kann den neuesten passenden Datei-Eintrag in einem SharePoint-Ordner aufloesen.
- `SiteExportService` nutzt fuer Manual-Quellen den Quellordner als Zielordner.
- `StandortePageService` erlaubt fuer Manual-Importe nun auch SharePoint-Ordnerreferenzen.
- Standort-UI-Hilfetext wurde entsprechend angepasst.
- `DatabaseSeedService` repariert England/TRUK auf den UK_B1-Ordner, wenn der Manual-Pfad leer ist.

Letzte technische Verifikation:

```text
dotnet test .\TrafagSalesExporter.Tests\TrafagSalesExporter.Tests.csproj --no-restore --verbosity minimal
```

Ergebnis:

- Tests erfolgreich, `55/55`
- Bekannte MudBlazor-Analyzerwarnungen zu `Dense` bleiben bestehen.

## FinanceProbe erweitert fuer alle Finance-Referenzen 2026-05-08

Umgesetzte Anpassungen:

- FinanceProbe zeigt nun alle aktiven `FinanceReferences` fuer 2025, auch wenn noch kein aktiver/importierter Standort dazu Daten liefert.
- Damit werden auch Laender wie AT, CH, CN, CZ, GFS, JP, MS, MSA, PL und RU sichtbar als `Keine Daten`, bis Ist-Daten vorhanden sind.
- Zusaetzliche Sektion `Datenabdeckung je Standort`:
  - Standort / TSC
  - Quellsystem und Anschlussart
  - Manual-Datei- oder SharePoint-Pfad
  - Aktivstatus
  - Anzahl 2025-Zeilen in `CentralSalesRecords`
  - Summe `SalesPriceValue`
  - Waehrungen
  - importierte Periode
  - letzter Exportstatus und Hinweis
- Referenzschluessel-Erkennung wurde fuer CH/AT praezisiert:
  - `AT`, `AUT`, `Oesterreich`/`Austria` -> `AT`
  - `CH`, `CHE`, `Schweiz`/`Switzerland` -> `CH`
- Damit koennen Zeilen aus `ZSCHWEIZ` mit `LAND1 = AT` fachlich Oesterreich zugeordnet werden.

Verifikation:

- `Tools/FinanceProbe` Build erfolgreich.
- Haupttests wurden mit separatem Output/Obj-Pfad ausgefuehrt, damit die laufende App nicht stoert.

## Mapper-/Finance-Konfiguration konsolidiert 2026-05-07

Umgesetzte Aufraeumarbeiten:

- Die doppelte SAP-OData/HANA-Mapping-Engine wurde entfernt.
- Neuer gemeinsamer Service: `MappedSalesRecordComposer`.
- `SapCompositionService` und `HanaQueryService.GetMappedSalesRecordsAsync` laden ihre Quellen weiterhin separat, nutzen danach aber denselben Composer fuer:
  - Primaerquelle
  - Left Joins
  - `SapFieldMapping` nach `SalesRecord`
  - Konstanten wie `=SAP` / `=HANA`
  - Datums-/Zahlenkonvertierung
- Der alte HANA-B1-Pfad fuer `OINV/INV1/ORIN/RIN1` bleibt bewusst bestehen, damit BI1/SAGE ohne grafisches Mapping weiter laufen.
- Die SAP-Mapping-Normalisierung liegt nur noch in `StandorteSapEditorService`; `StandortePageService` ruft diesen Service beim Speichern auf.
- Der tote Parameter im konsolidierten Export wurde entfernt. `ConsolidatedExportService.ExportAsync()` liest eindeutig aus `CentralSalesRecords`.
- Manueller Import erlaubt in UI und Service jetzt `.xlsx` und `.csv`.

Finance-Konfiguration:

- Neue Tabelle `FinanceReferences` fuer Soll-/check.xlsx-Referenzen je Jahr.
- Neue Tabelle `FinanceIntercompanyRules` fuer 2nd-party/IC-Erkennung nach `ScopeKey`, Kundennummer oder Namensmarker.
- Budgetkurse 2025 werden in `CurrencyExchangeRates` mit `Notes = Budget 2025` geseedet.
- `FinanceReconciliationService` liest Sollwerte, Budgetkurse und IC-Regeln aus der DB.
- Config-Export/-Import enthaelt jetzt `FinanceReferences` und `FinanceIntercompanyRules`.

Noch bewusst offen:

- HANA-B1-Spezialpfad und generischer HANA-Mapper laufen parallel. Das ist aktuell noetig fuer bestehende BI1/SAGE-Standorte ohne Mapping.
- Manual Excel hat weiterhin Header-Automatik und grafisches Mapping. Naechster Aufraeumpunkt waere eine gemeinsame Import-Mapping-Engine.

Letzte technische Verifikation:

```text
dotnet build .\TrafagSalesExporter.csproj --no-restore -p:UseAppHost=false --verbosity minimal
dotnet test .\TrafagSalesExporter.Tests\TrafagSalesExporter.Tests.csproj --no-restore --verbosity minimal
```

Ergebnis:

- Build erfolgreich
- Tests erfolgreich, `52/52`
- Bekannte MudBlazor-Analyzerwarnungen zu `Dense` bleiben bestehen.

## SAP OData / ZSCHWEIZ / HANA Mapping 2026-05-07

Aktueller Entscheid:

- `ZSCHWEIZ` wird nicht direkt als SAP-HANA-Spezialfall gelesen.
- `ZSCHWEIZ` wird ueber den bestehenden SAP-OData/Gateway-Pfad gelesen.
- Der grafische Quellen- und Feldmapper bleibt dafuer aktiv.
- Feldinfos muessen nicht hart codiert werden, solange der Gateway-Service `$metadata` fuer das EntitySet liefert.

Quellsystem-Namen wurden zur Entwirrung geschaerft:

- Code `SAP` bleibt technisch bestehen, DisplayName ist jetzt `SAP OData`.
- Code `SAP_HANA` bleibt fuer direkte HANA-Tabellen/Views bestehen, DisplayName ist jetzt `SAP HANA Tables/Views`.
- Bestehende Konfigurationen bleiben dadurch kompatibel.

Seed / Vorkonfiguration:

- Standort `ZSCHWEIZ` / Land `Schweiz/Oesterreich` wird als inaktiver Standort angelegt bzw. repariert.
- `SourceSystem = SAP`.
- Quelle: Alias `Z`, EntitySet `ZSCHWEIZSet`.
- Mapping ist grafisch editierbar und wird auf die Felder der Tabelle `ZSCHWEIZ` gesetzt.
- Die Seed-/Repair-Logik zieht Quelle und Mapping auch bei bereits vorhandener ZSCHWEIZ-Konfiguration nach; manuelles Mapping ist nur noetig, wenn die Gateway-Feldnamen vom erwarteten `ZSCHWEIZ`-Layout abweichen.

Wichtig fuer die UI:

1. App neu starten, damit Seed/Repair laeuft.
2. `Settings -> Quellsysteme`: `SAP` sollte als `SAP OData` erscheinen.
3. `Standorte -> ZSCHWEIZ`:
   - Quellsystem `SAP OData (SAP)`
   - SAP Service URL Override auf den finalen OData-Service fuer `ZSCHWEIZ` setzen, falls die zentrale SAP-URL noch auf `ZPOWERBI_EINKAUF_SRV` zeigt.
   - `Entity Sets refreshen`.
   - Quelle `Z` soll auf `ZSCHWEIZSet` zeigen.
   - `Felder aus Quellen laden`.
   - Mapping kontrollieren.

ABAP / SAP:

- ABAP-Report liegt in `report.abap`.
- Report fuellt Tabelle `ZSCHWEIZ` aus Buchungskreis `1100` = Schweiz und `1200` = Oesterreich.
- `LAND1` ist Reporting-Land aus Buchungskreis.
- `CUSTOMER_LAND` ist Kundenland aus `KNA1-LAND1`.
- Upsert erfolgt per `MODIFY zschweiz FROM TABLE`.

Letzte technische Verifikation:

```text
dotnet build .\TrafagSalesExporter.csproj --no-restore -p:UseAppHost=false --verbosity minimal
dotnet test .\TrafagSalesExporter.Tests\TrafagSalesExporter.Tests.csproj --no-restore --verbosity minimal
```

Ergebnis:

- Build erfolgreich
- Tests erfolgreich, `50/50`

## Finance-Abgrenzung: Antworten Andreas 2026-05-07

Fachliche Vorgabe nach Rueckmeldung:

- Net Sales Actuals werden in Hauswaehrung gerechnet.
- Massgebend ist der Nettofakturawert.
- Umrechnung nach CHF erfolgt mit Budgetkursen, nicht mit Tageskursen.
- Umrechnung/Summierung soll pro Artikel bzw. Belegposition erfolgen.
- Indien wird in INR betrachtet.
- Italien wird in Hauswaehrung betrachtet; Intercompany-/2nd-party-Abgrenzung wird separat angeschaut.
- UK wird in GBP betrachtet.
- Gutschriften haben eigene Rechnungsnummern/Rechnungspositionen und sollen ueber Artikelnummern/Positionen behandelt werden.
- Intercompany soll im zweiten Schritt als 2nd-party/3rd-party-Klassifikation pflegbar werden.
- Genannte 2nd-party/Intercompany-Indikatoren: Trafag, Magnetic Sense/Magnets Sense, Gesellschaft fuer Sensorik; Nummern/Uebersetzungen koennen je Land abweichen.

Budgetkurse 2025 fuer CHF-Ausweis:

```text
USD/CHF = 0.85
EUR/CHF = 0.95
GBP/CHF = 1.13
CHF/INR = 90.91
CHF/CZK = 25.64
PLN/CHF = 0.22
CHF/JPY = 156.25
```

Umsetzung in der FinanceProbe:

- Auswahl der Ist-Variante bevorzugt nun `Nettofakturawert Hauswaehrung` (`DocTotal - VatSum`).
- `Sales Price/Value` bleibt als Vergleichsvariante sichtbar.
- Zusaetzlicher Kandidat `Nettofakturawert Hauswaehrung -> CHF Budget 2025`.
- Referenz in der Oberflaeche wird als `check.xlsx Sollwert` bezeichnet, nicht mehr als fuehrende Power-BI-Referenz.
- Intercompany-Anzeige wurde fachlich als `2nd-party/IC` beschriftet; Regeln werden jetzt in `FinanceIntercompanyRules` geseedet und per Config exportiert/importiert.

## Finance Probe / Sales-Abgrenzung

Ziel der heutigen Arbeit:

- separate kleine Pruef-GUI fuer Finanz-/Sales-Abgrenzungen bauen
- moeglichst viel Logik aus dem Hauptprogramm wiederverwenden
- verschiedene Summenlogiken pro Land nebeneinander sichtbar machen
- gegen `check.xlsx` vergleichen

Wichtiges fachliches Verstaendnis nach Klaerung im Chat:

- `check.xlsx` kommt von Rhino und enthaelt die Soll-Zahlen von Andreas.
- Aus den Landessystemen kommt der Ist-Wert.
- Power BI soll in der fachlichen Kommunikation nicht als fuehrende Referenz genannt werden.
- Ziel ist nicht, zufaellig die passendste technische Variante zu nehmen, sondern je Land/System die fachlich korrekte Abgrenzungslogik zu klaeren.

## Commit

Rollback-Commit fuer die Finance-Probe wurde erstellt:

```text
15dec06 Add finance reconciliation probe
```

Dieser Commit enthaelt gezielt:

- `Services/FinanceReconciliationService.cs`
- `Tools/FinanceProbe/FinanceProbe.csproj`
- `Tools/FinanceProbe/Program.cs`
- DI-Registrierung in `Program.cs`
- Dashboard nutzt den ausgelagerten Finance-Service
- `TrafagSalesExporter.csproj` schliesst `Tools/**` aus dem Hauptprojekt aus
- `TrafagSalesExporter.sln` enthaelt das neue Tool-Projekt

Andere bereits vorhandene Worktree-Aenderungen wurden nicht mitcommitted.

## Neues Tool

Neues separates Probe-GUI:

```text
Tools/FinanceProbe
```

Start:

```powershell
dotnet run --project Tools\FinanceProbe\FinanceProbe.csproj --urls http://localhost:55417
```

URL:

```text
http://localhost:55417/finance
```

Aktueller Start im Chat:

- Probe-GUI wurde auf `localhost:55417` gestartet
- HTTP `200` bestaetigt

Hinweis Netzwerk:

- Start mit `localhost` ist nur lokal auf dem Laptop erreichbar.
- Andere im Trafag-Netz koennen es so normalerweise nicht ueber Laptop-IP oeffnen.
- Fuer Netzwerkzugriff waere `http://0.0.0.0:55417` noetig.
- Probe-GUI hat aktuell keine Authentifizierung, daher nicht unkontrolliert im Netzwerk freigeben.

## FinanceReconciliationService

Neue wiederverwendbare Logik:

```text
Services/FinanceReconciliationService.cs
```

Interface:

```csharp
IFinanceReconciliationService
```

Aktuelle Funktion:

```csharp
Task<List<NetSalesReferenceRow>> BuildNetSalesReferenceRowsAsync(int year = 2025)
```

Logik:

- liest `CentralSalesRecords`
- filtert Jahr ueber `InvoiceDate`, fallback `ExtractionDate`
- gruppiert pro Referenz-Key/Land
- berechnet Kandidaten:
  - `SalesPriceValue`
  - `DocTotalFC - VatSumFC`
  - `DocTotal - VatSum`
- Belegkopfwerte werden vor Summierung dedupliziert:
  - bevorzugt `TSC + DocumentType + DocumentEntry`
  - fallback `TSC + DocumentType + InvoiceNumber`
- erkennt aktuell Intercompany nur pragmatisch fuer IT/TRIT anhand bekannter Kunden
- liefert pro Kandidat Wert, Waehrung, IC-Wert, Differenzen

## FinanceProbe Darstellung

Die Tabelle zeigt aktuell:

- Status
- Firma
- gewaehlte Abgrenzung
- Ist-Waehrung
- Ist 2025
- Referenz-Waehrung
- Referenz
- Excel LC
- Excel CHF
- Excel Power BI
- Excel Status
- Differenz
- Differenz ohne IC
- Waehrung
- Zeilen
- Varianten aufklappbar

Wichtig:

- Die Bezeichnung `Power BI` ist in der Probe-Oberflaeche noch sichtbar, weil `check.xlsx` diese Spalte enthaelt.
- Fachlich soll in Kommunikation gegen Andreas aber `check.xlsx` / Soll-Zahl genannt werden, nicht Power BI als fuehrende Referenz.
- Eine sinnvolle naechste UI-Bereinigung waere, die Spalte/Labels in der Probe auf `Excel Sollwert` oder `Rhino Sollwert` umzubenennen.

## Probe-Output vom 2026-05-04 09:55

Zusammenfassung:

```text
8 Standorte
4 OK
1 Pruefen
3 Keine Daten
Excel-Referenzen gelesen: 17
```

Befunde:

### CH

- Keine Ist-Daten
- keine sichtbare Soll-Zahl

### DE

- Keine Ist-Zeilen aus Systemdaten
- Soll/LC aus Excel vorhanden:
  - Referenz ca. `3'635'923`
  - Excel LC `3'635'922.91`
  - Excel CHF `3'407'000.00`

Offen:

- Quelle fuer DE klaeren
- evtl. MANUAL_EXCEL oder noch nicht exportiert

### ES

- Keine Ist-Zeilen aus Systemdaten
- Soll/LC aus Excel vorhanden:
  - Referenz ca. `3'102'334`
  - Excel LC `3'102'333.61`
  - Excel CHF `2'907'000.00`

Offen:

- Quelle fuer ES klaeren
- evtl. MANUAL_EXCEL oder noch nicht exportiert

### FR

- Status OK
- gewaehlte Abgrenzung: `Sales Price/Value`
- Ist-Waehrung: `EUR`
- Ist: `1'471'218.44`
- Soll/Referenz: `1'471'218.00`
- Differenz: `0.44`
- Zeilen: `1649`

Befund:

- FR passt praktisch exakt mit `Sales Price/Value` in EUR.

Offene Frage an Andreas:

- Ist `Sales Price/Value` in EUR fuer FR fachlich korrekt?

### IN

- Status OK
- gewaehlte Abgrenzung: `Sales Price/Value`
- Ist-Waehrungen: `CHF, EUR, GBP, INR, JPY, USD`
- Ist: `750'936'591.38`
- Soll/Referenz: `750'936'591.00`
- Differenz: `0.38`
- Zeilen: `4000`

Befund:

- IN passt rechnerisch fast exakt, aber Waehrungen sind gemischt.

Offene Frage an Andreas:

- Ist diese gemischte Summe fachlich korrekt?
- Oder muss nach CHF umgerechnet bzw. nach Waehrung getrennt werden?

### IT

- Status Pruefen
- gewaehlte Abgrenzung: `DocTotal - VatSum`
- Ist-Waehrung: `EUR`
- Ist: `11'866'896.53`
- Soll/Referenz LC: `7'669'840.00`
- Differenz: `4'197'056.53`
- Differenz ohne IC: `3'733.67`
- Zeilen: `15883`

Befund:

- IT liegt ohne IC-Abzug stark daneben.
- Mit erkanntem IC-Abzug ist die Differenz sehr klein.

Offene Frage an Andreas:

- Soll IT mit Intercompany-Abzug gerechnet werden?
- Falls ja: nach welchen Kunden/Kriterien erkennt Finance Intercompany?

### UK

- Status OK
- gewaehlte Abgrenzung: `Sales Price/Value`
- Ist-Waehrung: `USD`
- Ist: `3'749'865.33`
- Soll/Referenz: `3'749'865.00`
- Differenz: `0.33`
- Zeilen: `942`

Befund:

- UK passt praktisch exakt mit `Sales Price/Value` in USD.

Offene Frage an Andreas:

- Ist USD fuer UK korrekt?
- Oder muss fuer offizielles Reporting nach CHF umgerechnet werden?

### US

- Status OK
- gewaehlte Abgrenzung: `Sales Price/Value`
- Ist-Waehrung: `USD`
- Ist: `3'749'865.33`
- Soll/Referenz: `3'749'865.00`
- Differenz: `0.33`
- Zeilen: `942`

Befund:

- US zeigt denselben Ist-Wert wie UK.
- Das wirkt auffaellig und sollte fachlich/technisch geprueft werden.

Offene Frage:

- Welche Quelle und Logik ist fuer US korrekt?
- Ist US im aktuellen System richtig zugeordnet?

## Word-Datei fuer Andreas

Erstellt:

```text
FINANZ_OFFENE_FRAGEN_ANDREAS.docx
```

Inhalt:

- kurze Mail an Andreas
- `check.xlsx` als Soll-Zahl von Andreas/Rhino formuliert
- Power BI fachlich nicht als Referenz genannt
- bisherige Befunde pro Land:
  - FR
  - IN
  - IT
  - UK
  - US
  - DE / ES
- offene Fragen zu:
  - Waehrung und CHF-Umrechnung
  - Umsatzdefinition
  - Periodenabgrenzung
  - Gutschriften/Storno
  - Intercompany
  - Entscheid-Tabelle pro Land

## Markdown-Datei fuer Andreas

Erstellt/angepasst:

```text
FINANZ_FRAGEN_ANDREAS.md
```

Aktuelle Formulierung:

- `check.xlsx` kommt von Rhino und enthaelt Soll-Zahlen von Andreas.
- Landessysteme liefern Ist-Werte.
- offen ist, welche fachliche Logik pro Land/System zur Soll-Zahl fuehren soll.
- Power BI ist nicht mehr als fuehrende Referenz formuliert.

## Verifikation

Ausgefuehrt:

```powershell
dotnet build .\TrafagSalesExporter.csproj --verbosity minimal
dotnet build .\Tools\FinanceProbe\FinanceProbe.csproj --verbosity minimal
dotnet test .\TrafagSalesExporter.Tests\TrafagSalesExporter.Tests.csproj --verbosity minimal
```

Ergebnis:

- Hauptprojekt baut erfolgreich
- FinanceProbe baut erfolgreich
- Tests erfolgreich
- `48/48` Tests gruen

Bekannte Warnungen:

- `NU1900` im Probe-Build, weil NuGet-Sicherheitsdaten wegen Netzwerk/nuget.org nicht geladen werden konnten
- bekannte MudBlazor Analyzer-Warnungen zu `Dense`

## Offene sinnvolle naechste Schritte

1. In der Probe-UI `Power BI`-Labels fachlich bereinigen:
   - z. B. `Excel Sollwert` / `Rhino Sollwert`
2. Andreas' Antworten in eine Konfiguration ueberfuehren:
   - Land/System
   - Summenlogik
   - System-Waehrung
   - CHF-Umrechnung ja/nein
   - Periodendatum
   - IC-Regel
3. DE/ES Quelle klaeren:
   - aktuell keine Ist-Daten
4. US/UK Doppelwert pruefen:
   - US zeigt denselben Ist-Wert wie UK
5. IT Intercompany-Regel fachlich bestaetigen
6. Wenn Regeln bestaetigt sind:
   - Finance-Probe erweitert anzeigen
   - spaeter produktiv ins Hauptprogramm uebernehmen

---

## Nachtrag 2026-05-04: Excel-Spaltenmapper fuer manuelle Land-Excel-Dateien

Ausloeser:

- Deutschland hat ein eigenes Excel-Beispiel geliefert.
- Das Format entspricht nicht dem bisherigen Standard-Excel-Import.
- Ziel war, nicht fuer jedes Land statischen Spezialcode zu schreiben, sondern die Spaltenzuordnung konfigurierbar zu machen.

Beispielhafte deutsche Spalten:

- `Export-Datum`
- `Firma`
- `Belegnummer`
- `Position`
- `ArtikelBezeichnung`
- `Warengruppen-Bezeichnung`
- `Anz. VE`
- `Lieferanten Nummer`
- `Name Lieferant`
- `Land Lieferant`
- `AdressNummer-Kunde`
- `Name Kunde`
- `Land Kunde`
- `Branche`
- `EinstandsPreis`
- `Währung`
- `BestellNummer`
- `NettoPreisEinzelX`
- `NettoPreisGesamtX`
- `Versandbedingung`
- `AdressNummer_V`
- `Belegdatum-Rechnung`
- `BelegDatum Auftrag`
- `ArtikelNummer`

Wichtige fachliche/technische Interpretation fuer Deutschland:

- `NettoPreisGesamtX` wird als `SalesPriceValue` verwendet.
- `Währung` wird fuer `SalesCurrency`, `DocumentCurrency`, `CompanyCurrency` und `StandardCostCurrency` verwendet.
- `Belegdatum-Rechnung` wird als `InvoiceDate` verwendet.
- `BelegDatum Auftrag` wird als `OrderDate` verwendet.
- `ArtikelNummer` wird als `Material` verwendet.
- Kommentar-/Info-Zeilen ohne echte Position und ohne Betrag werden beim Import ignoriert.

## Neue Datenstruktur

Neue Tabelle / neues Model:

```text
ManualExcelColumnMappings
Models/ManualExcelColumnMapping.cs
```

Felder:

- `SiteId`
- `TargetField`
- `SourceHeader`
- `IsRequired`
- `IsActive`
- `SortOrder`

Zweck:

- Pro Standort kann festgelegt werden, welche Excel-Spalte auf welches internes `SalesRecord`-Feld gemappt wird.
- Konstanten sind moeglich, wenn `SourceHeader` mit `=` beginnt, z. B. `=Manual Excel`.

## Geaenderte Hauptlogik

Geaendert:

```text
Services/ManualExcelImportService.cs
```

Neue Logik:

- Beim manuellen Excel-Import werden zuerst aktive `ManualExcelColumnMappings` des Standorts geladen.
- Wenn Mapping-Zeilen vorhanden sind, wird dieses Mapping verwendet.
- Wenn kein Mapping vorhanden ist, laeuft weiterhin die bisherige statische Standarderkennung.
- Damit bleiben bestehende manuelle Excel-Imports abwaertskompatibel.

Wichtig:

- Der Mapper ersetzt nicht die fachliche Finanzlogik.
- Er sorgt nur dafuer, dass fremde Excel-Spalten korrekt in die internen Felder geschrieben werden.
- Welche Summe spaeter fuer Finance gilt, muss weiterhin fachlich entschieden werden.

## Geaenderte Standort-UI

Geaendert:

```text
Components/Pages/Standorte.razor
Services/StandortePageService.cs
```

In der Standortbearbeitung fuer manuelle Excel-Standorte gibt es neu:

- Bereich `Excel-Spaltenmapping`
- Button `Spalten aus Excel laden`
- Button `Auto-Match`
- Button `Mapping hinzufuegen`
- Tabelle mit:
  - Zielfeld
  - Excel-Spalte / Konstante
  - Pflicht
  - Aktiv
  - Loeschen

Auto-Match erkennt aktuell u. a. die deutschen Spalten und schlaegt passende Zuordnungen vor.

## Config-Export / Import

Geaendert:

```text
Services/ConfigTransferService.cs
Models/ConfigTransferPackage.cs
```

Neu:

- `ManualExcelColumnMappings` werden im Konfigurationspaket mit exportiert.
- Beim Import werden die Mapping-Zeilen wieder hergestellt.

Damit kann die Konfiguration spaeter zwischen Umgebungen mitgenommen werden.

## Datenbank-Schema

Geaendert:

```text
Data/AppDbContext.cs
Services/DatabaseInitializationService.SchemaSql.cs
Services/DatabaseSchemaMaintenanceService.cs
```

Neu:

- `DbSet<ManualExcelColumnMapping>`
- `CREATE TABLE ManualExcelColumnMappings`
- Schema-Wartung legt die Tabelle nachtraeglich an, falls sie in einer bestehenden DB fehlt.
- Beim Loeschen eines Standorts werden dessen manuelle Excel-Mappings mit geloescht.

## Deutschland lokal eingerichtet

Am 2026-05-04 wurde Deutschland in der lokalen Datenbank direkt ohne UI eingerichtet.

Lokale DB:

```text
C:\Users\koi\source\repos\Ai\TrafagSalesExporter\trafag_exporter.db
```

Gefundener/konfigurierter Standort:

```text
Id=8
TSC=TRDE
Land=Deutschland
SourceSystem=MANUAL_EXCEL
```

Aktive Mapping-Zeilen:

```text
26
```

Konkrete Zuordnung fuer DE:

```text
ExtractionDate           <- Export-Datum
InvoiceNumber            <- Belegnummer
PositionOnInvoice        <- Position
Material                 <- ArtikelNummer
Name                     <- ArtikelBezeichnung
ProductGroup             <- Warengruppen-Bezeichnung
Quantity                 <- Anz. VE
SupplierNumber           <- Lieferanten Nummer
SupplierName             <- Name Lieferant
SupplierCountry          <- Land Lieferant
CustomerNumber           <- AdressNummer-Kunde
CustomerName             <- Name Kunde
CustomerCountry          <- Land Kunde
CustomerIndustry         <- Branche
StandardCost             <- EinstandsPreis
StandardCostCurrency     <- Währung
PurchaseOrderNumber      <- BestellNummer
SalesPriceValue          <- NettoPreisGesamtX
SalesCurrency            <- Währung
DocumentCurrency         <- Währung
CompanyCurrency          <- Währung
Incoterms2020            <- Versandbedingung
SalesResponsibleEmployee <- AdressNummer_V
InvoiceDate              <- Belegdatum-Rechnung
OrderDate                <- BelegDatum Auftrag
DocumentType             <- =Manual Excel
```

Wichtig fuer Rollback/Umzug:

- Diese DE-Einrichtung wurde direkt in `trafag_exporter.db` gespeichert.
- Die DB-Aenderung ist kein Git-Commit-Inhalt, weil SQLite-Datenbankdaten normalerweise nicht sauber versioniert werden.
- Der Code fuer den Mapper ist aktuell im Worktree vorhanden, aber noch nicht committed.
- Wenn die DB zurueckgerollt oder neu erstellt wird, muss das DE-Mapping erneut ueber die UI, Config-Import oder ein Hilfsskript eingerichtet werden.

## Tests

Ergaenzt:

```text
TrafagSalesExporter.Tests/ManualExcelImportServiceTests.cs
```

Neuer Test:

```text
ReadSalesRecordsAsync_Uses_Configured_Manual_Excel_Mapping_For_German_Headers
```

Der Test prueft:

- deutsches Excel-Headerformat
- Kommentarzeile ohne echte Position wird ignoriert
- echte Belegposition wird importiert
- `NettoPreisGesamtX` mit Schweizer Tausenderzeichen wird korrekt als Dezimalzahl gelesen
- Waehrung `EUR` wird in Sales-/Document-/Company-Currency uebernommen
- Rechnungsdatum und Auftragsdatum werden korrekt gelesen

Letzter bekannter Teststand nach Mapper-Arbeit:

```text
dotnet build .\TrafagSalesExporter.csproj --verbosity minimal
dotnet build .\Tools\FinanceProbe\FinanceProbe.csproj --verbosity minimal
dotnet test .\TrafagSalesExporter.Tests\TrafagSalesExporter.Tests.csproj --verbosity minimal --no-restore
```

Ergebnis:

- Hauptprojekt baut erfolgreich
- FinanceProbe baut erfolgreich
- Tests erfolgreich
- `49/49` Tests gruen

Bekannte Warnung:

- `NU1900`, weil NuGet-Sicherheitsdaten wegen Netzwerk/nuget.org nicht geladen werden konnten

## Aktueller Laufstand

Die Haupt-App war nach der DE-Konfiguration erreichbar:

```text
http://localhost:55416/standorte
HTTP 200
```

Hinweis:

- Der Browser kann geschlossen sein, waehrend der Serverprozess weiterlaeuft.
- Wenn ein Build wegen gesperrter Dateien fehlschlaegt, zuerst den laufenden `TrafagSalesExporter`-Prozess beenden.

## Noch offen nach Excel-Spaltenmapper

1. Mapper-Code committen, sobald der aktuelle Stand als Rollback-Punkt gesichert werden soll.
2. In der Standort-UI Deutschland oeffnen und visuell pruefen, ob die 26 Mapping-Zeilen angezeigt werden.
3. Mit echtem DE-Excel einen Importlauf testen.
4. Danach Finance-Probe erneut pruefen:
   - ob DE nicht mehr `Keine Daten` ist
   - ob `SalesPriceValue` gegen Soll aus `check.xlsx` passt
5. Falls weitere Laender eigene Excel-Formate liefern:
   - nicht statischen Code bauen
   - neues Mapping pro Standort pflegen
6. Klaeren, ob DE fachlich `NettoPreisGesamtX` in EUR als Ist-Wert verwenden soll oder ob CHF-Umrechnung noetig ist.

---

## Nachtrag 2026-05-05: FinanceProbe Ampel, Spanien v2 und Deutschland-Beispielfile

### FinanceProbe Management-Ansicht

Das Testprogramm `Tools/FinanceProbe` wurde fuer das Finance-Meeting erweitert.

URL lokal:

```text
http://localhost:55417/finance
```

Neue Ansicht:

- `Meeting Ampel 2025`
- Ampel pro Land:
  - Gruen: Zahl passt rechnerisch gegen Referenz
  - Gelb: Differenz oder fachliche Abgrenzung offen
  - Grau: keine belastbaren Importdaten
- Anzeige pro Land:
  - Ist
  - Soll / Referenz
  - Differenz
  - passender technischer Wert
  - Waehrung / CHF-Hinweis
  - kurze fachliche Begruendung

Wichtig zur Waehrung:

- Wenn Quelle `CHF` liefert, kann CHF direkt gezeigt werden.
- Wenn Quelle `EUR`, `USD`, `GBP`, `INR` usw. liefert, ist es Mandanten-/Originalwaehrung.
- CHF-Ausweis braucht dann eine separate FX-Regel bzw. offiziellen Umrechnungskurs.

### Spanien v2 im Testprogramm

Spanien wird im FinanceProbe nicht mehr nur als normaler Zentralimport betrachtet.

Direkter CSV-Check:

```text
sagespain/v2/Spain_Sales_2025.csv
```

Gelesene Werte:

- Zeilen: `4'341`
- Ist 2025 / `SalesPriceValue`: `3'082'320.18`
- Waehrung: `EUR`
- Soll aus `check.xlsx`: `3'102'333.61`
- Differenz: `-20'013.43`

Status:

- Ampel: Gelb / Pruefen
- Grund: Export technisch lesbar, aber Differenz zu `check.xlsx` offen.

Offen fuer Spanien:

- korrekte Datumsabgrenzung (`FechaFactura` vs. Alternativen)
- Serien `REG`, `LAT`, `PRO`, `REC`
- Behandlung von Gutschriften / `REC`
- offizielle Sage-Auswertung mit identischem Filter zur Sollzahl

### Deutschland-Beispielfile

Neues File im Projektordner:

```text
DE_Beispiel_Export_Daten.xlsx
```

Hinweis:

- Der Benutzer hatte zuerst `.xls` genannt, vorhanden ist `.xlsx`.
- Das File ist als Beispielfile zu behandeln, nicht als finale Jahresdatei.

Technischer Check:

- relevante Spalte: `NettoPreisGesamtX`
- Mapping-Ziel: `SalesPriceValue`
- Betragszeilen: `2`
- Summe `NettoPreisGesamtX`: `8'290.70`
- Waehrung: `EUR`

Einbau im FinanceProbe:

- eigener Abschnitt `Germany Excel sample check`
- zeigt Datei, Zeilenzahl, Summe und Referenz aus `check.xlsx`
- markiert explizit, dass die Differenz nur Sample-Charakter hat
- in der Management-Ampel wird Deutschland weiter nicht als OK gewertet, solange kein finaler DE-Jahresexport/import vorliegt

Fachliche Interpretation fuer Deutschland:

- Das Mapping funktioniert technisch.
- `NettoPreisGesamtX` kann als Kandidat fuer `SalesPriceValue` gelesen werden.
- Das Beispielfile darf nicht gegen die Jahresreferenz `3'635'922.91` als finale Ist-Zahl verwendet werden.
- Fuer das Meeting ist die Aussage:
  - Deutschland-Format ist technisch verstanden.
  - Finale DE-Zahl fehlt noch.
  - Benoetigt wird ein vollstaendiger DE-Jahresfile 2025 oder ein bestaetigter Importlauf.

### Verifikation 2026-05-05

Ausgefuehrt:

```text
dotnet build .\Tools\FinanceProbe\FinanceProbe.csproj --verbosity minimal --no-restore
dotnet test .\TrafagSalesExporter.Tests\TrafagSalesExporter.Tests.csproj --verbosity minimal --no-restore
```

Ergebnis:

- FinanceProbe Build erfolgreich
- Tests erfolgreich
- `50/50` Tests gruen
- Web UI liefert `HTTP 200`
- FinanceProbe enthaelt:
  - `Meeting Ampel 2025`
  - `Spain CSV direct check`
  - `Germany Excel sample check`
