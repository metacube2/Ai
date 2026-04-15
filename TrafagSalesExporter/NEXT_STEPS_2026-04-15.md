# Next Steps

Stand: 2026-04-15

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

Dateien:

- `Components/Pages/ManagementCockpit.razor`
- `Services/ManagementCockpitService.cs`

## 7. Referenzdatei

Fuer den vollstaendigen Kontext zuerst lesen:

- `HANDOFF_2026-04-15.md`
