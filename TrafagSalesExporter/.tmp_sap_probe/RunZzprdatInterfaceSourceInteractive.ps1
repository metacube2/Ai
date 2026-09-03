$ErrorActionPreference = 'Continue'
$probe = Join-Path $PSScriptRoot 'bin\x86\Release\net48\SapProbe.exe'
$log = Join-Path $PSScriptRoot 'zzprdat_interface_source_0903.log'
$outDir = Join-Path $PSScriptRoot 'zzprdat_live_0903'
if (Test-Path -LiteralPath $log) { Remove-Item -LiteralPath $log -Force }
New-Item -ItemType Directory -Path $outDir -Force | Out-Null
$securePassword = Read-Host 'SAP-Passwort fuer KOI in T76/100' -AsSecureString
$passwordPtr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
try {
    $env:SAP_NCO_PASSWORD = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPtr)
    Start-Transcript -Path $log -Force | Out-Null
    function Invoke-Probe {
        param([string] $Title, [string[]] $Arguments)
        Write-Host ''; Write-Host "=== $Title ===" -ForegroundColor Cyan
        $output = & $probe --no-password-prompt @Arguments 2>&1
        $code = $LASTEXITCODE
        foreach ($line in $output) { Write-Host ([string] $line) }
        Write-Host "Exit code: $code"
    }
    Invoke-Probe 'Interface-Pool' @(
        'abap-read','IF_EX_WORKORDER_UPDATE======IP','--latest','--with-includes',
        '--out',(Join-Path $outDir 'IF_EX_WORKORDER_UPDATE_IP.abap')
    )
    Invoke-Probe 'Interface Public Include' @(
        'abap-read','IF_EX_WORKORDER_UPDATE======IU','--latest',
        '--out',(Join-Path $outDir 'IF_EX_WORKORDER_UPDATE_IU.abap')
    )
    Invoke-Probe 'Vergleichsklasse Pool' @(
        'abap-read','ZCL_IM__IKO_CHECK_CO========CP','--latest','--with-includes',
        '--out',(Join-Path $outDir 'ZCL_IM__IKO_CHECK_CO_CP.abap')
    )
    Invoke-Probe 'Includes Interface-Pool' @(
        'table-read','D010INC','--fields','MASTER,INCLUDE',
        '--where',"MASTER = 'IF_EX_WORKORDER_UPDATE======IP'",'--rowcount','200'
    )
    Invoke-Probe 'Includes Vergleichsklasse' @(
        'table-read','D010INC','--fields','MASTER,INCLUDE',
        '--where',"MASTER = 'ZCL_IM__IKO_CHECK_CO========CP'",'--rowcount','200'
    )
}
finally {
    try { Stop-Transcript | Out-Null } catch {}
    $env:SAP_NCO_PASSWORD = $null
    if ($passwordPtr -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPtr) }
}
Write-Host ''; Write-Host "Quellpruefung beendet. Log: $log" -ForegroundColor Green
Read-Host 'Enter zum Schliessen'
