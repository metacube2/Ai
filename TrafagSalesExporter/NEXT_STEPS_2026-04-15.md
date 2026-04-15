# Next Steps

Stand: 2026-04-15

## 1. Erstes Ziel

Prüfen, ob die aktuelle Version beim Standort-Export noch in der zentralen SQLite-Speicherung hängen bleibt.

Wichtig:

- App neu starten
- denselben Standort erneut exportieren
- letzte sichtbare `Live-Status`-Meldung exakt notieren

Interessant sind vor allem diese Fälle:

- `Zentrale Tabelle: Batch x/y speichern...`
- `Zentrale Tabelle: Batch x/y abschliessen...`
- `Zentrale Tabelle aktualisiert`
- `Export erfolgreich`

## 2. Hauptverdächtiger

Datei:

- `Services/CentralSalesRecordService.cs`

Aktueller Stand:

- alte Sätze werden in eigener Transaktion gelöscht
- Inserts laufen in Batches von 25
- jeder Batch wird separat committed

Wenn es noch hängt, dort zuerst ansetzen.

## 3. Falls es weiter hängt

In dieser Reihenfolge prüfen:

1. Batchgröße weiter reduzieren
   - z. B. `10` statt `25`
2. Direkt vor und direkt nach `transaction.CommitAsync()` zusätzlich technische Logs setzen
3. Prüfen, ob parallel noch andere SQLite-Zugriffe laufen
4. Optional zentrale Speicherung vorübergehend per Setting deaktivierbar machen
5. Falls nötig zentrale Speicherung in separate DB-Datei auslagern

## 4. Dashboard / UI prüfen

Zu testen:

- `Excel öffnen` wird nach neuem erfolgreichen Export aktiv
- `Export erfolgreich` zeigt `Pfad=...`
- Dashboard-Live-Status setzt sich nach Abschluss sauber zurück

Dateien:

- `Components/Pages/Dashboard.razor`
- `Services/SiteExportService.cs`
- `Models/ExportLog.cs`

## 5. SAP-Funktionalität kurz gegenprüfen

Zu testen:

- `Quellen refreshen`
- `Felder aus Quellen laden`
- `Auto-Match`
- SAP-Export eines Standorts

Dateien:

- `Components/Pages/Standorte.razor`
- `Services/SapGatewayService.cs`
- `Services/SapCompositionService.cs`

## 6. Management Cockpit prüfen

Zu testen:

- vorhandene Excel-Datei auswählbar
- Analyse läuft
- Kennzahlen plausibel

Dateien:

- `Components/Pages/ManagementCockpit.razor`
- `Services/ManagementCockpitService.cs`

## 7. Wenn Stabilität vor Funktion geht

Sinnvolle pragmatische Zwischenlösung:

- zentrale SQLite-Speicherung per Setting abschaltbar machen
- Export lokal und zentral Excel weiter erlauben
- zentrale DB erst wieder aktivieren, wenn der Commit-Pfad stabil ist

## 8. Referenzdatei

Für den vollständigen Kontext zuerst lesen:

- `HANDOFF_2026-04-15.md`

