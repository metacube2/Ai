$ErrorActionPreference = 'Stop'

$tool = Join-Path $PSScriptRoot 'ZzprdatOrderTool\bin\x86\Release\net48\ZzprdatOrderTool.exe'
$log = Join-Path $PSScriptRoot 'zzprdat_order_metadata_0903.log'

if (-not (Test-Path -LiteralPath $tool)) {
    throw "Werkzeug fehlt: $tool"
}

Start-Transcript -Path $log -Force | Out-Null
try {
    Write-Host 'SAP-Passwort jetzt eingeben und mit Enter bestaetigen:' -ForegroundColor Yellow
    & $tool
    $exitCode = $LASTEXITCODE
}
finally {
    Stop-Transcript | Out-Null
}

Write-Host ''
Write-Host "Exit code: $exitCode; Log: $log"
Read-Host 'Enter zum Schliessen'
