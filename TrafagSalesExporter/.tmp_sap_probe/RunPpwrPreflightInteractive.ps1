$ErrorActionPreference = 'Stop'

$exe = Join-Path $PSScriptRoot 'bin\x86\Release\net48\SapProbe.exe'
$log = Join-Path $PSScriptRoot 'ppwr_preflight.log'

if (-not (Test-Path -LiteralPath $exe)) {
    throw "SapProbe fehlt: $exe"
}

$securePassword = Read-Host 'Passwort fuer SAP T76 Benutzer KOI' -AsSecureString
$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)

try {
    $env:SAP_T76_PASSWORD = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    Start-Transcript -LiteralPath $log -Force | Out-Null

    function Invoke-Probe([string[]] $ProbeArgs) {
        Write-Host "`n>>> SapProbe $($ProbeArgs -join ' ')"
        & $exe --client 090 @ProbeArgs 2>&1 | ForEach-Object { Write-Host $_ }
        Write-Host "<<< Exit $LASTEXITCODE"
    }

    Invoke-Probe @('system-info')
    Invoke-Probe @('table-read', 'CABN', '--fields', 'ATINN,ATNAM,ATFOR,ANZST,ANZDZ,ADZHL', '--where', "ATNAM LIKE 'ZPPWR%'", '--rowcount', '100', '--format', 'csv')
    Invoke-Probe @('table-read', 'CABN', '--fields', 'ATINN,ATNAM,ATFOR,ANZST,ANZDZ,ADZHL', '--where', "ATNAM LIKE 'ZCOMP%'", '--rowcount', '100', '--format', 'csv')
    Invoke-Probe @('function-search', 'BAPI_CHARACT*')
    Invoke-Probe @('function-search', 'BAPI_CLASS*')
    Invoke-Probe @('function-info', 'BAPI_CHARACT_CREATE')
    Invoke-Probe @('function-info', 'BAPI_CHARACT_GETDETAIL')
    Invoke-Probe @('function-info', 'BAPI_CLASS_CREATE')
    Invoke-Probe @('function-info', 'BAPI_CLASS_GETDETAIL')
    Invoke-Probe @('function-info', 'BAPI_TRANSACTION_COMMIT')
    Invoke-Probe @('function-info', 'RFC_ABAP_INSTALL_AND_RUN')
}
finally {
    if ($null -ne $bstr) {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
    Remove-Item Env:SAP_T76_PASSWORD -ErrorAction SilentlyContinue
    try { Stop-Transcript | Out-Null } catch { }
}

Write-Host ''
Write-Host "Vorpruefung beendet. Protokoll: $log"
Read-Host 'Enter zum Schliessen'
