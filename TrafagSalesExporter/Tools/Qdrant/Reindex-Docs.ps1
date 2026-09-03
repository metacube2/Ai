<#
.SYNOPSIS
    Baut den Suchindex der Markdown-Dokumentation neu auf.

.DESCRIPTION
    Startet bei Bedarf Qdrant, liest alle Markdown-Dateien des Repositories, schneidet sie
    an ihren Ueberschriften auf und schreibt sie in die Qdrant-Sammlung. Die Sammlung wird
    dabei vollstaendig ersetzt, damit geloeschte oder umbenannte Dateien nicht als Leichen
    im Index zurueckbleiben.

    Nach jeder inhaltlichen Aenderung an der Dokumentation ausfuehren. Ein veralteter Index
    widerspricht sonst still den Dateien, die er abbilden soll.
#>
[CmdletBinding()]
param(
    [string]$QdrantVerzeichnis = "C:\Users\koi\tools\qdrant",
    [string]$Sammlung = "trafag_docs"
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

& $uv run --python 3.12 (Join-Path $hier "index_docs.py")
if ($LASTEXITCODE -ne 0) {
    throw "Die Indexierung ist mit Exitcode $LASTEXITCODE fehlgeschlagen."
}

Write-Host "Index '$Sammlung' neu aufgebaut."
