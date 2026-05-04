# Last Change 2026-05-04

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
