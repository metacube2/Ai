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
