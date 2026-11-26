<#
.SYNOPSIS
    Startet die Doku-Suche im Browser (suche_server.py) und gibt ihre Adresse aus.

.DESCRIPTION
    Seit 2026-11-26. Startet bei Bedarf Qdrant ueber Start-Qdrant.ps1, danach den kleinen
    Suchdienst Tools/Qdrant/suche_server.py im Hintergrund. Der Dienst bettet die Frage mit
    demselben Modell ein wie der Indexer (query_embed) und fragt Qdrant; die Seite zeigt die
    Treffer mit Score, Datei, Abschnitt und Textausschnitt.

    Der Dienst bindet ausschliesslich an 127.0.0.1, Port 6340 oder den naechsten freien bis 6349.
    Port und PID stehen in <QdrantVerzeichnis>\suche_dienst.json, die Ausgaben in
    suche.out.log und suche.err.log daneben. Laeuft der Dienst schon, gibt das Skript nur die
    Adresse aus. Keine Administratorrechte. Seit 2026-11-26 ruft die geplante Aufgabe
    "Qdrant Doku-Suche (TrafagSalesExporter)" dieses Skript bei jeder Anmeldung auf (README 3).

    Gestartet wird zuerst mit "uv run --offline", damit nichts aus dem Internet geladen wird;
    nur wenn das scheitert (Paket fehlt im uv-Cache), einmal ohne --offline wie Reindex-Docs.ps1.

    Beenden: Stop-Process -Id (Get-Content C:\Users\koi\tools\qdrant\suche_dienst.json | ConvertFrom-Json).pid

.EXAMPLE
    .\Tools\Qdrant\Start-Suche.ps1
#>
[CmdletBinding()]
param(
    [string]$QdrantVerzeichnis = "C:\Users\koi\tools\qdrant",
    [string]$Sammlung = "trafag_docs",
    [int]$Port = 6340,
    [int]$WarteSekunden = 120
)

$ErrorActionPreference = "Stop"

$hier = Split-Path -Parent $MyInvocation.MyCommand.Path
$skript = Join-Path $hier "suche_server.py"
$zustand = Join-Path $QdrantVerzeichnis "suche_dienst.json"
$protokoll = Join-Path $QdrantVerzeichnis "suche.out.log"
$fehlerprotokoll = Join-Path $QdrantVerzeichnis "suche.err.log"
$uv = "C:\Users\koi\.local\bin\uv.exe"

function Get-DienstStand {
    # Fragt /api/stand ab und gibt das Ergebnis nur zurueck, wenn sich wirklich die Doku-Suche meldet.
    param([int]$P, [int]$Timeout = 5)
    try {
        $s = Invoke-RestMethod -Uri "http://127.0.0.1:$P/api/stand" -TimeoutSec $Timeout
        if ($s.dienst -eq "trafag-doku-suche") { return $s }
    } catch {
        # Port frei oder belegt durch etwas anderes.
    }
    return $null
}

function Find-LaufendenDienst {
    $kandidaten = @()
    if (Test-Path $zustand) {
        try { $kandidaten += [int]((Get-Content -Path $zustand -Raw -Encoding UTF8 | ConvertFrom-Json).port) } catch { }
    }
    $kandidaten += $Port..($Port + 9)
    foreach ($p in ($kandidaten | Select-Object -Unique)) {
        # Nur pruefen, wenn auf dem Port ueberhaupt jemand lauscht; das spart die Wartezeit je Port.
        $lauscht = Get-NetTCPConnection -LocalAddress 127.0.0.1 -LocalPort $p -State Listen -ErrorAction SilentlyContinue
        if (-not $lauscht) { continue }
        $s = Get-DienstStand -P $p -Timeout 20
        if ($s) { return @{ Port = $p; Stand = $s } }
    }
    return $null
}

$laufend = Find-LaufendenDienst
if ($laufend) {
    Write-Host "Doku-Suche laeuft bereits (PID $($laufend.Stand.pid)): http://127.0.0.1:$($laufend.Port)/"
    exit 0
}

if (-not (Test-Path $uv)) {
    throw "uv wurde unter '$uv' nicht gefunden. Bitte Tools/Qdrant/README.md lesen."
}

# Qdrant bei Bedarf starten. Scheitert das, startet die Suche trotzdem und zeigt auf der Seite,
# dass Qdrant fehlt, statt hier abzubrechen.
try {
    & (Join-Path $hier "Start-Qdrant.ps1") -QdrantVerzeichnis $QdrantVerzeichnis
} catch {
    Write-Warning "Qdrant liess sich nicht starten: $($_.Exception.Message)"
}

$env:FASTEMBED_CACHE_PATH = Join-Path $QdrantVerzeichnis "models"
$env:QDRANT_URL = "http://127.0.0.1:6333"
$env:COLLECTION_NAME = $Sammlung
$env:QDRANT_VERZEICHNIS = $QdrantVerzeichnis
$env:PYTHONIOENCODING = "utf-8"

function Start-Dienst {
    # Eigener Prozess, Ausgaben in Dateien, nie durch die PowerShell-Pipeline: der fastembed-Hinweis
    # auf stderr bricht sonst unter PowerShell 5.1 ab (docs/rag/LEARNINGS.md, 2026-10-21).
    param([switch]$Offline)
    if (Test-Path $zustand) { Remove-Item -Path $zustand -Force -Confirm:$false }
    $schalter = if ($Offline) { "--offline " } else { "" }
    $argumente = "run $schalter--python 3.12 `"$skript`" --port $Port --zustand `"$zustand`""
    $prozess = Start-Process -FilePath $uv -ArgumentList $argumente -WorkingDirectory $hier -WindowStyle Hidden `
        -PassThru -RedirectStandardOutput $protokoll -RedirectStandardError $fehlerprotokoll

    $ende = (Get-Date).AddSeconds($WarteSekunden)
    $adresse = $null
    while ((Get-Date) -lt $ende) {
        Start-Sleep -Milliseconds 700
        if ($prozess.HasExited) { return $null }
        if (-not $adresse -and (Test-Path $zustand)) {
            try { $adresse = (Get-Content -Path $zustand -Raw -Encoding UTF8 | ConvertFrom-Json).adresse } catch { }
        }
        if ($adresse) {
            $p = ([uri]$adresse).Port
            $s = Get-DienstStand -P $p -Timeout 20
            if ($s -and $s.modell_zustand -ne "laedt") { return @{ Adresse = $adresse; Stand = $s } }
        }
    }
    if ($adresse) { return @{ Adresse = $adresse; Stand = $null } }
    # Nach Ablauf der Wartezeit ohne Adresse: haengenden Versuch beenden, bevor ein zweiter startet.
    Stop-Process -Id $prozess.Id -Force -Confirm:$false -ErrorAction SilentlyContinue
    return $null
}

$ergebnis = Start-Dienst -Offline
if (-not $ergebnis) {
    Write-Warning "Start mit --offline gescheitert, zweiter Versuch ohne --offline (uv darf Paketlisten abfragen)."
    if (Test-Path $fehlerprotokoll) { Get-Content -Path $fehlerprotokoll -Tail 5 -Encoding UTF8 | ForEach-Object { Write-Host $_ } }
    $ergebnis = Start-Dienst
}
if (-not $ergebnis) {
    if (Test-Path $fehlerprotokoll) {
        Write-Host "--- letzte Zeilen von $fehlerprotokoll ---"
        Get-Content -Path $fehlerprotokoll -Tail 15 -Encoding UTF8 | ForEach-Object { Write-Host $_ }
    }
    throw "Die Doku-Suche ist nicht gestartet. Protokolle: $protokoll, $fehlerprotokoll"
}

$s = $ergebnis.Stand
if ($null -eq $s) {
    Write-Warning "Der Dienst laeuft, das Modell war nach $WarteSekunden s aber noch nicht geladen."
} elseif ($s.modell_zustand -eq "fehler") {
    Write-Warning "Modell nicht geladen: $($s.modell_fehler)"
} elseif (-not $s.qdrant_erreichbar) {
    Write-Warning "Die Suche laeuft, Qdrant aber nicht: $($s.qdrant_fehler)"
} else {
    Write-Host "Index: $($s.punkte) Punkte in '$($s.sammlung)'."
}
Write-Host "Doku-Suche bereit: $($ergebnis.Adresse)"
exit 0
