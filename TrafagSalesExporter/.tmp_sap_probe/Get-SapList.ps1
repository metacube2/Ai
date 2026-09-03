<#
.SYNOPSIS
    Liest die klassische ABAP-Listenausgabe einer Sitzung als lesbaren Text.

.DESCRIPTION
    Eine klassische WRITE-Liste besteht aus GuiLabel-Elementen mit Spalten- und
    Zeilenkoordinate. Dieses Skript setzt sie wieder zu Zeilen zusammen. Damit lassen sich
    Reportergebnisse vollstaendig auswerten, ohne einen Screenshot anzufordern.

    Fuer ALV-Grids gilt das nicht; dort ist GetCellValue der Weg.

.EXAMPLE
    .\.tmp_sap_probe\Get-SapList.ps1 -Transaktion SE38
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Transaktion
)

$ErrorActionPreference = 'Stop'

$dump = Join-Path $env:TEMP ("sapliste_{0}.txt" -f [Guid]::NewGuid().ToString('N'))
try {
    cscript.exe //nologo (Join-Path $PSScriptRoot 'SapGuiDumpSession.vbs') $Transaktion $dump | Out-Null
    if (-not (Test-Path $dump)) {
        throw "Kein Bildschirmbaum erzeugt. Gibt es eine $Transaktion-Sitzung in T76/100?"
    }

    $zeilen = @{}
    foreach ($l in (Get-Content -LiteralPath $dump)) {
        if ($l -match 'lbl\[(\d+),(\d+)\].*\| Text=(.*)$') {
            $sp = [int]$Matches[1]
            $ze = [int]$Matches[2]
            if (-not $zeilen.ContainsKey($ze)) { $zeilen[$ze] = @{} }
            $zeilen[$ze][$sp] = $Matches[3]
        }
    }

    if ($zeilen.Count -eq 0) {
        Write-Host "Keine Listenzeilen gefunden. Steht die Sitzung wirklich auf einer Liste?" -ForegroundColor Yellow
        return
    }

    foreach ($ze in ($zeilen.Keys | Sort-Object)) {
        $s = ''
        foreach ($sp in ($zeilen[$ze].Keys | Sort-Object)) {
            $s += (' ' * [Math]::Max(0, $sp - $s.Length)) + $zeilen[$ze][$sp]
        }
        if ($s.Trim()) { $s }
    }
}
finally {
    Remove-Item -LiteralPath $dump -ErrorAction SilentlyContinue
}
