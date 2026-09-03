$ErrorActionPreference = 'Continue'

$probe = Join-Path $PSScriptRoot 'bin\x86\Release\net48\SapProbe.exe'
$log = Join-Path $PSScriptRoot 'zzprdat_preflight_0903.log'
$resultsLog = Join-Path $PSScriptRoot 'zzprdat_preflight_results_0903.log'
$outDir = Join-Path $PSScriptRoot 'zzprdat_live_0903'

if (-not (Test-Path -LiteralPath $probe)) {
    throw "SapProbe.exe fehlt: $probe"
}

New-Item -ItemType Directory -Path $outDir -Force | Out-Null
if (Test-Path -LiteralPath $log) {
    Remove-Item -LiteralPath $log -Force
}
if (Test-Path -LiteralPath $resultsLog) {
    Remove-Item -LiteralPath $resultsLog -Force
}

$securePassword = Read-Host 'SAP-Passwort fuer KOI in T76/100' -AsSecureString
$passwordPtr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)

try {
    $env:SAP_NCO_PASSWORD = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPtr)
    Start-Transcript -Path $log -Force | Out-Null

    function Invoke-Probe {
        param(
            [Parameter(Mandatory)] [string] $Title,
            [Parameter(Mandatory)] [string[]] $Arguments
        )

        Write-Host ''
        Write-Host "=== $Title ===" -ForegroundColor Cyan
        "`r`n=== $Title ===" | Out-File -LiteralPath $resultsLog -Append -Encoding utf8
        & $probe --no-password-prompt @Arguments 2>&1 |
            Tee-Object -FilePath $resultsLog -Append
        Write-Host "Exit code: $LASTEXITCODE"
    }

    Invoke-Probe 'Verbindung T76/100' @('system-info')
    Invoke-Probe 'Feld AUFK-ZZPRDAT' @('table-fields', 'AUFK', 'ZZPRDAT')
    Invoke-Probe 'Repositoryeintrag WORKORDER_UPDATE' @(
        'table-read', 'TADIR', '--fields', 'PGMID,OBJECT,OBJ_NAME,DEVCLASS',
        '--where', "OBJ_NAME = 'WORKORDER_UPDATE'", '--rowcount', '20'
    )
    Invoke-Probe 'Felder der CMOD-Aktivierungstabelle MODACT' @('table-fields', 'MODACT')
    Invoke-Probe 'CMOD-Aktivierungen ZPP00012' @(
        'table-read', 'MODACT', '--fields', 'NAME',
        '--where', "NAME = 'ZPP00012'", '--rowcount', '20'
    )
    Invoke-Probe 'Kundenauftrag 399566' @(
        'table-read', 'VBAK', '--fields', 'VBELN,AUART,ERDAT,VKORG,VTWEG,SPART,OBJNR',
        '--where', "VBELN = '0000399566'", '--rowcount', '20'
    )
    Invoke-Probe 'AFPO-Feld KDAUF bestaetigen' @('table-fields', 'AFPO', 'KDAUF')
    Invoke-Probe 'Fertigungsauftraege zu Kundenauftrag 399566' @(
        'table-read', 'AFPO', '--fields', 'AUFNR,POSNR,KDAUF,KDPOS,DGLTP',
        '--where', "KDAUF = '0000399566'", '--rowcount', '200'
    )
    Invoke-Probe 'Referenzauftraege AUFK' @(
        'table-read', 'AUFK', '--fields', 'AUFNR,AUART,OBJNR,ERDAT,ZZPRDAT',
        '--where', "AUFNR = '000001214608' OR AUFNR = '000001216195' OR AUFNR = '000001214481' OR AUFNR = '000001214062'",
        '--rowcount', '20'
    )
    Invoke-Probe 'Referenzauftraege AFKO' @(
        'table-read', 'AFKO', '--fields', 'AUFNR,GSTRP,GLTRP,GLTRS',
        '--where', "AUFNR = '000001214608' OR AUFNR = '000001216195' OR AUFNR = '000001214481' OR AUFNR = '000001214062'",
        '--rowcount', '20'
    )

    foreach ($include in @('ZXCO1U11', 'ZXCO1U12', 'ZXCO1O01', 'ZXCO1I01')) {
        Invoke-Probe "Aktiver Quelltext $include" @(
            'abap-read', $include, '--latest', '--out', (Join-Path $outDir "$include.abap")
        )
    }
}
finally {
    try { Stop-Transcript | Out-Null } catch {}
    $env:SAP_NCO_PASSWORD = $null
    if ($passwordPtr -ne [IntPtr]::Zero) {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPtr)
    }
}

Write-Host ''
Write-Host "Vorpruefung beendet. Log: $log" -ForegroundColor Green
Read-Host 'Enter zum Schliessen'
