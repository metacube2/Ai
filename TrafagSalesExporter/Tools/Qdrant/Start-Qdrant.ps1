<#
.SYNOPSIS
    Startet die lokale Qdrant-Instanz fuer die Dokumentationssuche.

.DESCRIPTION
    Qdrant laeuft als gewoehnliches Benutzerprogramm, es sind keine Administratorrechte
    noetig. Der Dienst bindet ausschliesslich an 127.0.0.1 und ist damit nicht aus dem
    Firmennetz erreichbar. Laeuft bereits eine Instanz, passiert nichts.
#>
[CmdletBinding()]
param(
    [string]$QdrantVerzeichnis = "C:\Users\koi\tools\qdrant"
)

$ErrorActionPreference = "Stop"

$exe = Join-Path $QdrantVerzeichnis "qdrant.exe"
if (-not (Test-Path $exe)) {
    throw "qdrant.exe wurde unter '$exe' nicht gefunden. Bitte Tools/Qdrant/README.md lesen."
}

$laufend = Get-Process qdrant -ErrorAction SilentlyContinue
if ($laufend) {
    Write-Host "Qdrant laeuft bereits (PID $($laufend.Id))."
} else {
    Start-Process -FilePath $exe `
        -WorkingDirectory $QdrantVerzeichnis `
        -WindowStyle Hidden `
        -RedirectStandardOutput (Join-Path $QdrantVerzeichnis "qdrant.out.log") `
        -RedirectStandardError (Join-Path $QdrantVerzeichnis "qdrant.err.log")
    Write-Host "Qdrant gestartet."
}

# Auf Bereitschaft warten, statt blind weiterzulaufen.
$bereit = $false
for ($i = 0; $i -lt 20; $i++) {
    try {
        $antwort = Invoke-RestMethod -Uri "http://127.0.0.1:6333/healthz" -TimeoutSec 3
        if ($antwort) { $bereit = $true; break }
    } catch {
        Start-Sleep -Milliseconds 500
    }
}

if (-not $bereit) {
    throw "Qdrant antwortet nicht auf http://127.0.0.1:6333/healthz. Log: $QdrantVerzeichnis\qdrant.err.log"
}

Write-Host "Qdrant ist bereit unter http://127.0.0.1:6333 (Weboberflaeche: /dashboard)."
