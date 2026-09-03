$ErrorActionPreference = 'Continue'
$probe = Join-Path $PSScriptRoot 'bin\x86\Release\net48\SapProbe.exe'
$programCsv = Join-Path $PSScriptRoot 'zzprdat_rtts_program.csv'
$log = Join-Path $PSScriptRoot 'zzprdat_rtts_0903.log'
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
        return $code
    }
    $infoCode = Invoke-Probe 'RFC_ABAP_INSTALL_AND_RUN Schnittstelle' @(
        'function-info','RFC_ABAP_INSTALL_AND_RUN'
    )
    if ($infoCode -eq 0) {
        Invoke-Probe 'RTTS-Komponenten COBAI_S_HEADER_DIALOG' @(
            'rfc-call','RFC_ABAP_INSTALL_AND_RUN','--table',"PROGRAM=$programCsv",
            '--confirm-write','--max-table-rows','200'
        ) | Out-Null
    }
}
finally {
    try { Stop-Transcript | Out-Null } catch {}
    $env:SAP_NCO_PASSWORD = $null
    if ($passwordPtr -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPtr) }
}
Write-Host ''; Write-Host "RTTS-Pruefung beendet. Log: $log" -ForegroundColor Green
Read-Host 'Enter zum Schliessen'
