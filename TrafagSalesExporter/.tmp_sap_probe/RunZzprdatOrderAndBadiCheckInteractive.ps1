$ErrorActionPreference = 'Continue'

$probe = Join-Path $PSScriptRoot 'bin\x86\Release\net48\SapProbe.exe'
$log = Join-Path $PSScriptRoot 'zzprdat_order_badi_0903.log'
if (-not (Test-Path -LiteralPath $probe)) { throw "SapProbe.exe fehlt: $probe" }
if (Test-Path -LiteralPath $log) { Remove-Item -LiteralPath $log -Force }

$securePassword = Read-Host 'SAP-Passwort fuer KOI in T76/100' -AsSecureString
$passwordPtr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)

try {
    $env:SAP_NCO_PASSWORD = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPtr)
    Start-Transcript -Path $log -Force | Out-Null

    function Invoke-Probe {
        param([string] $Title, [string[]] $Arguments)
        Write-Host ''
        Write-Host "=== $Title ===" -ForegroundColor Cyan
        $output = & $probe --no-password-prompt @Arguments 2>&1
        $probeExitCode = $LASTEXITCODE
        foreach ($line in $output) {
            Write-Host ([string] $line)
        }
        Write-Host "Exit code: $probeExitCode"
    }

    Invoke-Probe 'Fertigungsauftrag 1194970 in AUFK' @(
        'table-read', 'AUFK', '--fields', 'AUFNR,AUART,OBJNR,ERDAT,ZZPRDAT',
        '--where', "AUFNR = '000001194970'", '--rowcount', '10'
    )
    Invoke-Probe 'Fertigungsauftrag 1194970 in AFKO' @(
        'table-read', 'AFKO', '--fields', 'AUFNR,GSTRP,GLTRP,GLTRS,FTRMI,GETRI',
        '--where', "AUFNR = '000001194970'", '--rowcount', '10'
    )
    Invoke-Probe 'Fertigungsauftrag 1194970 in AFPO' @(
        'table-read', 'AFPO', '--fields', 'AUFNR,POSNR,MATNR,KDAUF,KDPOS,DGLTP',
        '--where', "AUFNR = '000001194970'", '--rowcount', '20'
    )
    Invoke-Probe 'Aktuelle Systemstatus des Fertigungsauftrags 1194970' @(
        'table-read', 'JEST', '--fields', 'OBJNR,STAT,INACT,CHGNR',
        '--where', "OBJNR = 'OR000001194970'", '--rowcount', '100'
    )
    Invoke-Probe 'Statushistorie des Fertigungsauftrags 1194970' @(
        'table-read', 'JCDS', '--fields', 'OBJNR,STAT,CHGNR,UDATE,UTIME,INACT,CHIND,USNAM',
        '--where', "OBJNR = 'OR000001194970'", '--rowcount', '200'
    )
    Invoke-Probe 'Planauftraege zum Kundenauftrag 399566' @(
        'table-read', 'PLAF', '--fields', 'PLNUM,MATNR,PLWRK,PSTTR,PEDTR,KDAUF,KDPOS,AUFNR',
        '--where', "KDAUF = '0000399566'", '--rowcount', '200'
    )
    Invoke-Probe 'Details CMOD-Projekt ZPP00012' @(
        'table-read', 'MODACT', '--fields', 'NAME,TYP,MEMBER,DEVCLASS,KORRNUM,TRANNUM,MIGRATED,BADI_IMP',
        '--where', "NAME = 'ZPP00012'", '--rowcount', '50'
    )
    Invoke-Probe 'Felder der BAdI-Implementierungstabelle SXC_EXIT' @('table-fields', 'SXC_EXIT')
    Invoke-Probe 'Implementierungen von WORKORDER_UPDATE' @(
        'table-read', 'SXC_EXIT', '--fields', 'EXIT_NAME,IMP_NAME,ACTIVE',
        '--where', "EXIT_NAME = 'WORKORDER_UPDATE'", '--rowcount', '100'
    )
    Invoke-Probe 'Repositoryobjekte mit WORKORDER_UPDATE im Namen' @(
        'table-read', 'TADIR', '--fields', 'PGMID,OBJECT,OBJ_NAME,DEVCLASS',
        '--where', "OBJ_NAME LIKE '%WORKORDER_UPDATE%'", '--rowcount', '100'
    )
}
finally {
    try { Stop-Transcript | Out-Null } catch {}
    $env:SAP_NCO_PASSWORD = $null
    if ($passwordPtr -ne [IntPtr]::Zero) {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPtr)
    }
}

Write-Host ''
Write-Host "Pruefung beendet. Log: $log" -ForegroundColor Green
Read-Host 'Enter zum Schliessen'
