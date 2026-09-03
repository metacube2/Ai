$ErrorActionPreference = 'Continue'
$probe = Join-Path $PSScriptRoot 'bin\x86\Release\net48\SapProbe.exe'
$log = Join-Path $PSScriptRoot 'zzprdat_header_structure_0903.log'
if (Test-Path -LiteralPath $log) { Remove-Item -LiteralPath $log -Force }
$securePassword = Read-Host 'SAP-Passwort fuer KOI in T76/100' -AsSecureString
$passwordPtr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
try {
    $env:SAP_NCO_PASSWORD = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPtr)
    Start-Transcript -Path $log -Force | Out-Null
    foreach ($argsForProbe in @(
        @('table-fields','COBAI_S_HEADER_DIALOG'),
        @('field-exists','COBAI_S_HEADER_DIALOG','AUFNR'),
        @('field-exists','COBAI_S_HEADER_DIALOG','GLTRP'),
        @('field-exists','COBAI_S_HEADER_DIALOG','ZZPRDAT'),
        @('field-exists','COBAI_S_HEADER_DIALOG','FTRMI')
    )) {
        $output = & $probe --no-password-prompt @argsForProbe 2>&1
        foreach ($line in $output) { Write-Host ([string] $line) }
        Write-Host "Exit code: $LASTEXITCODE"
    }
}
finally {
    try { Stop-Transcript | Out-Null } catch {}
    $env:SAP_NCO_PASSWORD = $null
    if ($passwordPtr -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPtr) }
}
Write-Host ''; Write-Host "Strukturpruefung beendet. Log: $log" -ForegroundColor Green
Read-Host 'Enter zum Schliessen'
