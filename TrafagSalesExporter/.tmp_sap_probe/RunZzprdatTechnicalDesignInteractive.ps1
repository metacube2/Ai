$ErrorActionPreference = 'Continue'

$probe = Join-Path $PSScriptRoot 'bin\x86\Release\net48\SapProbe.exe'
$log = Join-Path $PSScriptRoot 'zzprdat_technical_design_0903.log'
$outDir = Join-Path $PSScriptRoot 'zzprdat_live_0903'
if (-not (Test-Path -LiteralPath $probe)) { throw "SapProbe.exe fehlt: $probe" }
New-Item -ItemType Directory -Path $outDir -Force | Out-Null
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
        foreach ($line in $output) { Write-Host ([string] $line) }
        Write-Host "Exit code: $probeExitCode"
    }

    foreach ($field in @('DWERK','PSMNG','MEINS','LGORT','VERID')) {
        Invoke-Probe "AFPO-Feld $field" @('table-fields', 'AFPO', $field)
    }
    Invoke-Probe 'Referenzdaten Auftrag 1194970' @(
        'table-read', 'AFPO', '--fields', 'AUFNR,POSNR,MATNR,DWERK,PSMNG,MEINS,LGORT,VERID,KDAUF,KDPOS,DGLTP',
        '--where', "AUFNR = '000001194970'", '--rowcount', '20'
    )
    Invoke-Probe 'BAPI_PRODORD_CREATE Schnittstelle' @('function-info', 'BAPI_PRODORD_CREATE')
    Invoke-Probe 'BAPI_PRODORD_RELEASE Schnittstelle' @('function-info', 'BAPI_PRODORD_RELEASE')
    Invoke-Probe 'BAPI_PRODORD_CHANGE Schnittstelle' @('function-info', 'BAPI_PRODORD_CHANGE')
    Invoke-Probe 'BAPI_TRANSACTION_COMMIT Schnittstelle' @('function-info', 'BAPI_TRANSACTION_COMMIT')

    Invoke-Probe 'WORKORDER_UPDATE klassische Implementierungen' @(
        'table-read', 'SXC_EXIT', '--fields', 'EXIT_NAME,IMP_NAME,FLT_VAL',
        '--where', "EXIT_NAME = 'WORKORDER_UPDATE'", '--rowcount', '100'
    )
    Invoke-Probe 'Felder SXC_ATTR' @('table-fields', 'SXC_ATTR')
    Invoke-Probe 'Methoden IF_EX_WORKORDER_UPDATE' @(
        'table-read', 'SEOCOMPO', '--fields', 'CLSNAME,CMPNAME,CMPTYPE,MTDTYPE',
        '--where', "CLSNAME = 'IF_EX_WORKORDER_UPDATE'", '--rowcount', '100'
    )
    Invoke-Probe 'Unterkomponenten BEFORE_UPDATE' @(
        'table-read', 'SEOSUBCO', '--where', "CLSNAME = 'IF_EX_WORKORDER_UPDATE' AND CMPNAME = 'BEFORE_UPDATE'", '--rowcount', '100'
    )
    Invoke-Probe 'Unterkomponenten IN_UPDATE' @(
        'table-read', 'SEOSUBCO', '--where', "CLSNAME = 'IF_EX_WORKORDER_UPDATE' AND CMPNAME = 'IN_UPDATE'", '--rowcount', '100'
    )
    Invoke-Probe 'Interfacepool IF_EX_WORKORDER_UPDATE' @(
        'abap-read', 'IF_EX_WORKORDER_UPDATE===========IP', '--latest', '--with-includes',
        '--out', (Join-Path $outDir 'IF_EX_WORKORDER_UPDATE_IP.abap')
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
Write-Host "Technikpruefung beendet. Log: $log" -ForegroundColor Green
Read-Host 'Enter zum Schliessen'
