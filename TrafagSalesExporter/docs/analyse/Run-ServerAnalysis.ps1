<#
.SYNOPSIS
  Steuert die lesenden Server-Analysen: SQL-Dateien hinlegen, Lauf ausloesen, Ergebnisse holen.

.NOTES
  Dies ist der versionierte, gueltige Stand. Abfragen liegen in `sql\`, Belege in
  `ergebnisse\`. Bewusst NICHT unter `.tmp_tools\` - der Ordner ist gitignoriert und die
  Abfragen sind der Nachweis fuer eine fachliche Entscheidung (Klassifikation der
  TRIN-Zeilen), der einen Crash und einen frischen Clone ueberleben muss.

.DESCRIPTION
  Ausgefuehrt wird auf dem Applikationsserver, weil einzelne Standortsysteme nur von dort
  erreichbar sind. Geprueft am 2026-08-05:

    - Indien HANA 20.197.20.60:30015          -> vom Entwicklungsrechner TCP nicht erreichbar
    - \\trch-webapp-bidashboard\BiDashboard$  -> FullControl, Dateien kopieren geht
    - Invoke-Command / schtasks / C$ auf tragvapp401 -> Zugriff verweigert
    - RDP auf den Server                      -> nicht vorhanden

  Weil weder Remoteausfuehrung noch RDP zur Verfuegung steht, fuehrt die LAUFENDE ANWENDUNG
  die Abfragen aus: `ServerAnalysisBackgroundService` prueft alle 20 Sekunden, ob im Ordner
  `_analysis` die Datei `run.trigger` liegt, arbeitet dann `_analysis\sql\*.sql` ab und
  schreibt die Ergebnisse nach `_analysis\results`. Dieses Skript ist nur die Fernbedienung
  dazu; es kopiert Dateien und liest Ergebnisse.

  Regeln fuer die SQL-Dateien:
    - Dateiname beginnt mit dem Standort:  TRIN__01_beschreibung.sql  ->  Standort TRIN
    - Statements werden durch eine Zeile getrennt, die mit ;; beginnt
    - nur SELECT/WITH (ReadOnlySqlGuard); Kommentarzeilen mit -- sind erlaubt und werden
      vor der Pruefung entfernt, die erste davon ist die Beschriftung im Ergebnis
    - Platzhalter {schema} (Schreibweise wie konfiguriert) und {SCHEMA} (gross)
    - zwei Bindestriche als Zeichenkettenliteral sind nicht moeglich (gelten als Kommentar)
    - je Statement maximal 500 Zeilen Ergebnis

.PARAMETER Action
  Run    SQL-Dateien hochlegen, Lauf ausloesen, warten, Ergebnisse holen (Standard)
  Fetch  nur die Ergebnisse holen
  Clean  Ergebnisse, SQL-Dateien und verbrauchte Trigger auf dem Server entfernen

.PARAMETER Only
  Nur diese SQL-Datei(en) hochlegen, z. B. -Only 'TRIN__02*'. Ohne Angabe alle aus sql\.

.EXAMPLE
  .\Run-ServerAnalysis.ps1 -Action Run -Only 'TRIN__02*'
  .\Run-ServerAnalysis.ps1 -Action Clean
#>
[CmdletBinding()]
param(
  [ValidateSet('Run', 'Fetch', 'Clean')]
  [string]$Action = 'Run',

  [string]$Only = '*.sql',

  [string]$Share = '\\trch-webapp-bidashboard.trafagch.local\BiDashboard$',

  [int]$TimeoutMinutes = 5
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$Here         = $PSScriptRoot
$SqlDir       = Join-Path $Here 'sql'
$LocalResults = Join-Path $Here 'ergebnisse'
$RemoteDir    = Join-Path $Share '_analysis'
$RemoteSql    = Join-Path $RemoteDir 'sql'
$RemoteResults= Join-Path $RemoteDir 'results'
$Trigger      = Join-Path $RemoteDir 'run.trigger'

if (-not (Test-Path $Share)) { throw "Share nicht erreichbar: $Share" }

function Get-Results {
  if (-not (Test-Path $RemoteResults)) { Write-Warning "Kein Ergebnisordner: $RemoteResults"; return }
  $files = @(Get-ChildItem $RemoteResults -Filter '*.txt' -File)
  if ($files.Count -eq 0) { Write-Warning 'Ergebnisordner ist leer.'; return }
  if (-not (Test-Path $LocalResults)) { New-Item -ItemType Directory -Path $LocalResults | Out-Null }
  foreach ($f in $files) {
    Copy-Item $f.FullName (Join-Path $LocalResults $f.Name) -Force
    Write-Host ("abgeholt: {0,-45} {1,8} KB  {2}" -f $f.Name, [math]::Round($f.Length/1KB,1), $f.LastWriteTime.ToString('yyyy-MM-dd HH:mm'))
  }
  Write-Host "Lokal: $LocalResults" -ForegroundColor Green
}

switch ($Action) {

  'Run' {
    $files = @(Get-ChildItem $SqlDir -Filter $Only -File)
    if ($files.Count -eq 0) { throw "Keine SQL-Datei zu '$Only' in $SqlDir." }

    foreach ($f in $files) {
      if ($f.BaseName -notmatch '^[A-Za-z0-9]+_') {
        throw "Dateiname '$($f.Name)' beginnt nicht mit '<TSC>_' - die Anwendung ueberspringt die Datei."
      }
    }

    if (-not (Test-Path $RemoteSql)) { New-Item -ItemType Directory -Path $RemoteSql | Out-Null }
    # Alte SQL-Dateien entfernen: sonst laufen bei jedem Trigger auch erledigte Analysen mit.
    Get-ChildItem $RemoteSql -Filter '*.sql' -File | Remove-Item -Force
    foreach ($f in $files) {
      Copy-Item $f.FullName $RemoteSql -Force
      Write-Host "hochgelegt: $($f.Name)"
    }

    Set-Content -Path $Trigger -Value "ausgeloest $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') von $env:USERNAME" -Encoding utf8
    Write-Host 'Trigger gesetzt. Die Anwendung prueft alle 20 Sekunden.' -ForegroundColor Cyan

    $deadline = (Get-Date).AddMinutes($TimeoutMinutes)
    while ((Get-Date) -lt $deadline -and (Test-Path $Trigger)) { Start-Sleep -Seconds 5 }

    if (Test-Path $Trigger) {
      Write-Warning 'Trigger wurde nicht abgeholt. Laeuft der Anwendungspool? Ein Aufruf der Website startet ihn.'
      Write-Host '  https://trch-webapp-bidashboard.trafagch.local/BiDashboard/'
      return
    }

    Write-Host 'Lauf gestartet, warte auf die Ergebnisse ...' -ForegroundColor Cyan
    Start-Sleep -Seconds 5
    Get-Results
  }

  'Fetch' { Get-Results }

  'Clean' {
    foreach ($path in $RemoteResults, $RemoteSql) {
      if (Test-Path $path) { Remove-Item $path -Recurse -Force; Write-Host "entfernt: $path" }
    }
    Get-ChildItem $RemoteDir -Filter 'run.trigger*' -File -ErrorAction SilentlyContinue |
      ForEach-Object { Remove-Item $_.FullName -Force; Write-Host "entfernt: $($_.Name)" }
    Write-Host 'Der Ordner _analysis selbst bleibt - er ist die Schnittstelle zur Anwendung.' -ForegroundColor Green
  }
}
