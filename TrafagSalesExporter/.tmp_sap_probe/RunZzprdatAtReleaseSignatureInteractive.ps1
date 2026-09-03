$ErrorActionPreference = 'Continue'
$probe = Join-Path $PSScriptRoot 'bin\x86\Release\net48\SapProbe.exe'
$log = Join-Path $PSScriptRoot 'zzprdat_at_release_signature_0903.log'
if (-not (Test-Path -LiteralPath $probe)) { throw "SapProbe.exe fehlt: $probe" }
if (Test-Path -LiteralPath $log) { Remove-Item -LiteralPath $log -Force }

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

    Invoke-Probe 'Parameter AT_RELEASE' @(
        'table-read', 'SEOSUBCO', '--where',
        "CLSNAME = 'IF_EX_WORKORDER_UPDATE' AND CMPNAME = 'AT_RELEASE'", '--rowcount', '100'
    )
    Invoke-Probe 'Alle Felder SEOSUBCO' @('table-fields', 'SEOSUBCO')
    Invoke-Probe 'Existenz SEOSUBCODF' @('table-fields', 'SEOSUBCODF')
    Invoke-Probe 'Parameterdefinitionen AT_RELEASE' @(
        'table-read', 'SEOSUBCODF', '--where',
        "CLSNAME = 'IF_EX_WORKORDER_UPDATE' AND CMPNAME = 'AT_RELEASE'", '--rowcount', '100'
    )
    Invoke-Probe 'Aktivstatus Z_IKO_CHECK_CO' @(
        'table-read', 'SXC_ATTR', '--fields', 'IMP_NAME,ACTIVE,ANAME,ADATE,ATIME,UNAME,UDATE,UTIME,MIG_ENHNAME',
        '--where', "IMP_NAME = 'Z_IKO_CHECK_CO'", '--rowcount', '20'
    )
    Invoke-Probe 'Felder SXC_CLASS' @('table-fields', 'SXC_CLASS')
    Invoke-Probe 'Implementierungsklasse Z_IKO_CHECK_CO' @(
        'table-read', 'SXC_CLASS', '--where', "IMP_NAME = 'Z_IKO_CHECK_CO'", '--rowcount', '20'
    )
    Invoke-Probe 'Methoden-RFCs suchen' @('function-search', 'SEO*METHOD*', '--max-table-rows', '100')
}
finally {
    try { Stop-Transcript | Out-Null } catch {}
    $env:SAP_NCO_PASSWORD = $null
    if ($passwordPtr -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPtr) }
}
Write-Host ''; Write-Host "Signaturpruefung beendet. Log: $log" -ForegroundColor Green
Read-Host 'Enter zum Schliessen'
