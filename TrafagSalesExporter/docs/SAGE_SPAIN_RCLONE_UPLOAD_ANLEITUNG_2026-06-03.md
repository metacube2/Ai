# Sage Spanien Rclone Upload Anleitung

Stand: 2026-06-03

Ziel: Der Sage-Server in Spanien erzeugt die Sales-CSV lokal und lädt die Datei danach automatisch in den SharePoint-Ordner fuer den Dashboard-Import.

## Zielordner

SharePoint URL:

```text
https://trafagag.sharepoint.com/sites/WorldwideBIPlatform/Shared%20Documents/Import/Finance/Spanien
```

Technischer Ordner:

```text
Shared Documents/Import/Finance/Spanien
```

Empfohlener rclone-Zielpfad:

```text
trafag-bi:Import/Finance/Spanien
```

Dabei zeigt `trafag-bi` auf die Dokumentbibliothek `Shared Documents` der SharePoint-Site:

```text
https://trafagag.sharepoint.com/sites/WorldwideBIPlatform
```

## Benötigte Dateien Auf Dem Spanien-Server

Empfohlener Ordner:

```text
C:\Trafag\SageSpain
```

Dateien:

```text
Export-SageSpainSalesCsv.ps1
Run-SpainExportAndUpload.ps1
```

Die Dateien liegen im Paket:

```text
SageSpainFinalExportPackage.zip
```

## rclone Installieren

Falls `winget` vorhanden ist:

```powershell
winget install Rclone.Rclone
```

Alternativ rclone ZIP manuell installieren, z.B. nach:

```text
C:\Tools\rclone\rclone.exe
```

Danach testen:

```powershell
rclone version
```

Falls `rclone` nicht im PATH ist, später den vollständigen Pfad verwenden:

```powershell
C:\Tools\rclone\rclone.exe version
```

## rclone Remote Einrichten

Auf dem Spanien-Server:

```powershell
rclone config
```

Empfohlene Eingaben:

```text
n
name> trafag-bi
Storage> onedrive
```

Danach Microsoft Login durchführen.

Wichtig:

- Site: `WorldwideBIPlatform`
- Dokumentbibliothek: `Shared Documents`
- Der rclone-Remote `trafag-bi` soll auf die Dokumentbibliothek `Shared Documents` zeigen.

## rclone Testen

```powershell
rclone lsd trafag-bi:
rclone lsd trafag-bi:"Import"
rclone lsd trafag-bi:"Import/Finance"
rclone lsd trafag-bi:"Import/Finance/Spanien"
```

Wenn der letzte Befehl den Ordner ohne Fehler zeigt, ist der Zielpfad korrekt.

## Manueller Export Ohne Upload

Full Export 2025:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
cd C:\Trafag\SageSpain
.\Export-SageSpainSalesCsv.ps1 -ExportMode Full -Year 2025 -OutputDirectory C:\Trafag\SageSpain\out
```

Delta/Range Export:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
cd C:\Trafag\SageSpain
.\Export-SageSpainSalesCsv.ps1 -ExportMode Range -DateFilter LineRegistrationDate -FromDate "2026-06-02" -ToDate "2026-06-03" -OutputDirectory C:\Trafag\SageSpain\out
```

Hinweis:

- `ToDate` ist exklusiv.
- Der Zeitraum `"2026-06-02"` bis `"2026-06-03"` exportiert den 2. Juni.
- Für tägliche Deltas ist `LineRegistrationDate` sinnvoll, weil neue oder geänderte Zeilen nach Registrierungsdatum kommen.

## Export Und Upload Zusammen Starten

Standard: täglicher Delta-Lauf, gestern bis heute:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
cd C:\Trafag\SageSpain
.\Run-SpainExportAndUpload.ps1
```

Expliziter Zeitraum:

```powershell
.\Run-SpainExportAndUpload.ps1 -ExportMode Range -DateFilter LineRegistrationDate -FromDate "2026-06-02" -ToDate "2026-06-03"
```

Full Export mit Upload:

```powershell
.\Run-SpainExportAndUpload.ps1 -ExportMode Full -Year 2025
```

Wenn rclone nicht im PATH ist:

```powershell
.\Run-SpainExportAndUpload.ps1 -RcloneExe "C:\Tools\rclone\rclone.exe"
```

Wenn der rclone-Remote anders heisst:

```powershell
.\Run-SpainExportAndUpload.ps1 -RcloneRemote "MEIN_REMOTE_NAME"
```

## Was Wird Hochgeladen?

Das Wrapper-Script lädt aus dem neuesten Exportordner:

```text
*.csv
*_summary.txt
```

Ziel:

```text
trafag-bi:Import/Finance/Spanien
```

Das Script ändert keine Daten in Sage und keine Daten in SQL Server.

## Windows Task Scheduler

Empfohlener täglicher Lauf, z.B. 02:00 Uhr:

```powershell
$action = New-ScheduledTaskAction `
  -Execute "powershell.exe" `
  -Argument "-NoProfile -ExecutionPolicy Bypass -File C:\Trafag\SageSpain\Run-SpainExportAndUpload.ps1"

$trigger = New-ScheduledTaskTrigger -Daily -At 02:00

Register-ScheduledTask `
  -TaskName "Trafag Spain Sage Export Upload" `
  -Action $action `
  -Trigger $trigger `
  -Description "Exports Sage Spain sales CSV and uploads it to SharePoint via rclone"
```

Wenn rclone nicht im PATH ist:

```powershell
$action = New-ScheduledTaskAction `
  -Execute "powershell.exe" `
  -Argument "-NoProfile -ExecutionPolicy Bypass -File C:\Trafag\SageSpain\Run-SpainExportAndUpload.ps1 -RcloneExe C:\Tools\rclone\rclone.exe"
```

## Kontrolle Nach Dem Lauf

Lokal:

```powershell
Get-ChildItem C:\Trafag\SageSpain\out -Directory | Sort-Object LastWriteTime -Descending | Select-Object -First 1
Get-ChildItem C:\Trafag\SageSpain\logs
```

SharePoint:

```powershell
rclone ls trafag-bi:"Import/Finance/Spanien"
```

Im Browser prüfen:

```text
https://trafagag.sharepoint.com/sites/WorldwideBIPlatform/Shared%20Documents/Import/Finance/Spanien
```

## Fehlerbilder

`rclone: command not found`

- rclone ist nicht im PATH.
- Lösung: `-RcloneExe "C:\Tools\rclone\rclone.exe"` verwenden.

`directory not found`

- Remote zeigt nicht auf `Shared Documents` oder Zielordner ist anders.
- Mit `rclone lsd trafag-bi:` und `rclone lsd trafag-bi:"Import/Finance"` prüfen.

`Access denied`

- Microsoft Login oder SharePoint-Berechtigung fehlt.
- Der Windows-User des geplanten Tasks muss Zugriff auf rclone-Konfiguration und SharePoint haben.

Leere Delta-Datei:

- Zeitraum prüfen.
- `ToDate` ist exklusiv.
- Bei täglichem Lauf für gestern bis heute ist das korrekt.
