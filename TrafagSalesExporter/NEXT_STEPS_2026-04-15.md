# Next Steps

Stand: 2026-04-15

## Nachtrag 2026-04-16

Seit dem letzten Stand kamen mehrere groessere Erweiterungen dazu. Die offenen Punkte unten muessen deshalb im neuen Kontext gelesen werden.

## 0. Neuer Ist-Stand

Zusaetzlich zum alten Stand ist jetzt vorhanden:

- manueller Standort-Import ueber `MANUAL_EXCEL`
- Dashboard mit `Alle exportieren`, `Zentrale Datei neu erzeugen` und zentralem `Excel oeffnen`
- Roh-Auswertung im `Management Cockpit` direkt aus `CentralSalesRecords`
- erweitertes Transformationssystem mit `Value`- und `Record`-Regeln
- HANA-Schema-Lookup im Standortdialog
- Testprojekt mit aktuell 18 gruenden Tests

## 1. Status

Der Export geht jetzt wieder durch.

Die zuletzt gefundene Hauptursache war nicht mehr ein reiner SQLite-Lock beim Batch-Insert, sondern ein kaputter FK-Schemazustand in der bestehenden DB:

- SQLite referenzierte in mindestens einer Tabelle noch `main.Sites_old`
- dadurch scheiterte `SaveChangesAsync()` beim Schreiben z. B. in `AppEventLogs` oder `ExportLogs`
- sichtbarer Effekt: Export blieb nach `Zentrale Tabelle: ... Datensaetze gespeichert.` haengen

## 2. Umgesetzter Fix

Umgesetzt wurde:

- Dashboard-Live-Status liest waehrend laufendem Export nicht mehr staendig aus `AppEventLogs`, sondern nutzt den In-Memory-Status des `ExportOrchestrationService`
- SQLite `Default Timeout` in `Program.cs` auf `60` erhoeht
- `CentralSalesRecordService` setzt nach den Batches explizit `Zentrale Tabelle aktualisiert`
- `DatabaseInitializationService` repariert beim App-Start automatisch Tabellen, deren FK-SQL noch `Sites_old` referenziert

Betroffene Dateien:

- `Program.cs`
- `Components/Pages/Dashboard.razor`
- `Services/CentralSalesRecordService.cs`
- `Services/DatabaseInitializationService.cs`

## 3. Was noch getestet werden sollte

Kurz gegenpruefen:

- Export eines Standorts erneut
- `Excel oeffnen` nach erfolgreichem Export
- `Export erfolgreich` inkl. `Pfad=...`
- Dashboard-Live-Status setzt sich nach Abschluss sauber zurueck
- `Alle exportieren`
- `Zentrale Datei neu erzeugen`
- zentrale Datei im Dashboard oeffnen

## 3a. Manuellen Excel-Import pruefen

Zu testen:

- Standort auf `MANUAL_EXCEL` stellen
- Excel im Standort hochladen
- Standort exportieren
- pruefen, ob `CentralSalesRecords` fuer diesen Standort ersetzt wurden
- pruefen, ob der zentrale Export den Standort korrekt enthaelt

Dateien:

- `Components/Pages/Standorte.razor`
- `Services/ManualExcelImportService.cs`
- `Services/SiteExportService.cs`

## 3b. HANA-Schema-Lookup pruefen

Zu testen:

- bei `BI1`-Standort `Schemas laden`
- bei `SAGE`-Standort `Schemas laden`
- wird ein plausibles B1-Schema angeboten?
- funktioniert danach Export ohne manuelle Schema-Eingabe?
- zeigt England / Spezialstandort jetzt schneller, wenn Schema oder Rechte nicht passen?

Dateien:

- `Components/Pages/Standorte.razor`
- `Services/HanaQueryService.cs`

## 4. Falls wieder ein Fehler auftritt

In dieser Reihenfolge pruefen:

1. Exakte Fehlermeldung aus `AppEventLogs` bzw. Console notieren
2. Pruefen, ob die Reparaturlogik beim Start gelaufen ist
3. Pruefen, ob noch weitere Tabellen mit veralteter FK-Referenz existieren
4. Erst danach wieder am Batch-/Commit-Pfad der zentralen Speicherung arbeiten

## 5. SAP-Funktionalitaet kurz gegenpruefen

Zu testen:

- `Quellen refreshen`
- `Felder aus Quellen laden`
- `Auto-Match`
- SAP-Export eines Standorts

Dateien:

- `Components/Pages/Standorte.razor`
- `Services/SapGatewayService.cs`
- `Services/SapCompositionService.cs`

## 6. Management Cockpit pruefen

Zu testen:

- vorhandene Excel-Datei auswaehlbar
- Analyse laeuft
- Kennzahlen plausibel
- Roh-Auswertung aus `CentralSalesRecords` laeuft
- Jahr/Monat-Filter funktionieren
- Summen nach Quelle / Land plausibel

Dateien:

- `Components/Pages/ManagementCockpit.razor`
- `Services/ManagementCockpitService.cs`

## 6a. Fachlich bewusst noch offen

Noch nicht final umsetzen ohne Rueckmeldung Fachseite:

- Intercompany-Filter
- CHF-Umrechnung / Wechselkurse
- Budgetvergleich
- Gruppenlogik
- Spartenlogik
- Margenlogik

Diese Punkte sollen spaeter moeglichst dynamisch auf dem neuen Transformations-/Mapping-Ansatz aufsetzen, aber aktuell nicht hart geraten werden.

## 6b. Naechste sinnvolle Testkandidaten

Wenn weiter in Tests investiert wird, sind die naechsten Kandidaten:

- `ExportOrchestrationService`
- spaeter evtl. SQLite-nahe Integrationstests fuer `DatabaseInitializationService`

Aktueller Teststatus:

- `dotnet test TrafagSalesExporter.sln --verbosity minimal`
- erfolgreich
- `18/18` Tests gruen

## 7. Referenzdatei

Fuer den vollstaendigen Kontext zuerst lesen:

- `HANDOFF_2026-04-15.md`
