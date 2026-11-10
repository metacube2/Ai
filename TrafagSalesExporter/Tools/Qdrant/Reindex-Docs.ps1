<#
.SYNOPSIS
    Baut den Suchindex der Markdown-Dokumentation neu auf oder prueft, ob er aktuell ist.

.DESCRIPTION
    Startet bei Bedarf Qdrant, liest alle Markdown-Dateien des Repositories, schneidet sie
    an ihren Ueberschriften auf und schreibt sie in die Qdrant-Sammlung. Die Sammlung wird
    dabei vollstaendig ersetzt, damit geloeschte oder umbenannte Dateien nicht als Leichen
    im Index zurueckbleiben.

    Nach jeder inhaltlichen Aenderung an der Dokumentation ausfuehren. Ein veralteter Index
    widerspricht sonst still den Dateien, die er abbilden soll.

    Seit 2026-11-10 (Wochen_Todo 118) laeuft der Indexer als eigener Prozess, dessen Ausgaben
    in Protokolldateien gehen. Es zaehlt nur der Exitcode. Vorher machte PowerShell 5.1 aus dem
    harmlosen fastembed-Hinweis auf stderr einen Fehler, sobald der Aufrufer die Ausgabe
    umleitete (2>&1 oder *>), und das Skript brach ab (2026-10-21, 2026-10-29). Nach dem Lauf
    wird ausserdem die Punktzahl der Sammlung gegen die Abschnittszahl geprueft, damit ein
    halb gefuellter Index (2026-10-27: 1'248 von rund 4'400) nicht unbemerkt bleibt.

.PARAMETER NurPruefen
    Baut nichts. Meldet, ob der Index zu den Dateien passt. Exitcode 0 = aktuell, 3 = veraltet.

.PARAMETER WennVeraltet
    Prueft zuerst und baut nur neu, wenn der Index veraltet ist.

.EXAMPLE
    .\Tools\Qdrant\Reindex-Docs.ps1
.EXAMPLE
    .\Tools\Qdrant\Reindex-Docs.ps1 -NurPruefen
.EXAMPLE
    .\Tools\Qdrant\Reindex-Docs.ps1 -WennVeraltet
#>
[CmdletBinding()]
param(
    [string]$QdrantVerzeichnis = "C:\Users\koi\tools\qdrant",
    [string]$Sammlung = "trafag_docs",
    [switch]$NurPruefen,
    [switch]$WennVeraltet
)

$ErrorActionPreference = "Stop"

$hier = Split-Path -Parent $MyInvocation.MyCommand.Path
& (Join-Path $hier "Start-Qdrant.ps1") -QdrantVerzeichnis $QdrantVerzeichnis

$uv = "C:\Users\koi\.local\bin\uv.exe"
if (-not (Test-Path $uv)) {
    throw "uv wurde unter '$uv' nicht gefunden. Bitte Tools/Qdrant/README.md lesen."
}

# Denselben Modellcache verwenden wie der MCP-Server, damit das Modell nur einmal
# heruntergeladen wird und nicht im Temp-Verzeichnis landet.
$env:FASTEMBED_CACHE_PATH = Join-Path $QdrantVerzeichnis "models"
$env:QDRANT_URL = "http://127.0.0.1:6333"
$env:COLLECTION_NAME = $Sammlung
$env:PYTHONIOENCODING = "utf-8"

$skript = Join-Path $hier "index_docs.py"
$stand = Join-Path $QdrantVerzeichnis "reindex_stand_$Sammlung.json"
$protokoll = Join-Path $QdrantVerzeichnis "reindex.out.log"
$fehlerprotokoll = Join-Path $QdrantVerzeichnis "reindex.err.log"

function Invoke-Indexer {
    <#
        Startet index_docs.py als eigenen Prozess und gibt dessen Exitcode zurueck.
        stdout und stderr gehen in Dateien, nie durch die PowerShell-Pipeline: so kann eine
        Zeile auf stderr das Skript nicht abbrechen, egal wie der Aufrufer umleitet.
    #>
    param([string]$Zusatz = "", [switch]$Fortschritt)

    $argumente = "run --python 3.12 `"$skript`" --stand `"$stand`" $Zusatz".Trim()
    $prozess = Start-Process -FilePath $uv -ArgumentList $argumente -NoNewWindow -PassThru `
        -RedirectStandardOutput $protokoll -RedirectStandardError $fehlerprotokoll
    # Ohne diesen Zugriff ist ExitCode unter PowerShell 5.1 nach dem Ende leer.
    $null = $prozess.Handle

    $letzte = ""
    while (-not $prozess.HasExited) {
        Start-Sleep -Seconds 3
        if ($Fortschritt) {
            try {
                $zeile = Get-Content -Path $protokoll -Tail 1 -Encoding UTF8 -ErrorAction Stop
                if ($zeile -and $zeile -ne $letzte) { Write-Host $zeile; $letzte = $zeile }
            } catch {
                # Die Datei ist kurz gesperrt oder noch leer; der naechste Durchgang liest sie.
            }
        }
    }
    $prozess.WaitForExit()
    return [int]$prozess.ExitCode
}

function Get-Protokoll {
    if (Test-Path $protokoll) { return @(Get-Content -Path $protokoll -Encoding UTF8) }
    return @()
}

function Show-Fehlerprotokoll {
    if (Test-Path $fehlerprotokoll) {
        Write-Host "--- letzte Zeilen von $fehlerprotokoll ---"
        Get-Content -Path $fehlerprotokoll -Tail 15 -Encoding UTF8 | ForEach-Object { Write-Host $_ }
    }
}

if ($NurPruefen -or $WennVeraltet) {
    $code = Invoke-Indexer -Zusatz "--pruefen"
    Get-Protokoll | ForEach-Object { Write-Host $_ }
    if ($code -eq 0) {
        exit 0
    }
    if ($code -ne 3) {
        Show-Fehlerprotokoll
        throw "Die Pruefung des Index ist mit Exitcode $code fehlgeschlagen."
    }
    if ($NurPruefen) {
        Write-Host "Neu aufbauen mit: .\Tools\Qdrant\Reindex-Docs.ps1"
        exit 3
    }
    Write-Host "Index ist veraltet, baue neu auf."
}

$code = Invoke-Indexer -Fortschritt
$zeilen = Get-Protokoll
if ($code -ne 0) {
    $zeilen | Select-Object -Last 5 | ForEach-Object { Write-Host $_ }
    Show-Fehlerprotokoll
    throw "Die Indexierung ist mit Exitcode $code fehlgeschlagen. Der Index ist jetzt unvollstaendig; Lauf wiederholen."
}

# Zweite Sicherung neben dem Exitcode: Punktzahl in Qdrant gegen die Abschnittszahl im Protokoll.
$abschnitte = $null
foreach ($zeile in $zeilen) {
    if ($zeile -match '^(\d+) Dateien, (\d+) Abschnitte\.') { $abschnitte = [int]$Matches[2]; break }
}
$antwort = Invoke-RestMethod -Method Post -Uri "http://127.0.0.1:6333/collections/$Sammlung/points/count" `
    -ContentType "application/json" -Body '{"exact": true}' -TimeoutSec 30
$punkte = [int]$antwort.result.count
if ($null -eq $abschnitte) {
    throw "Im Protokoll $protokoll steht keine Abschnittszahl; der Lauf ist nicht nachvollziehbar."
}
if ($punkte -ne $abschnitte) {
    throw "Index unvollstaendig: $punkte Punkte in '$Sammlung', erwartet $abschnitte Abschnitte. Lauf wiederholen."
}

Write-Host "Index '$Sammlung' neu aufgebaut: $punkte Punkte = $abschnitte Abschnitte. Stand: $stand"
exit 0
