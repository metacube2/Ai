$ErrorActionPreference = 'Stop'

$exe = Join-Path $PSScriptRoot 'bin\x86\Release\net48\SapProbe.exe'
$log = Join-Path $PSScriptRoot 'sap_probe_last_run.log'

if (-not (Test-Path -LiteralPath $exe)) {
    Write-Host "SapProbe.exe was not found:"
    Write-Host $exe
    Read-Host "Press Enter to close"
    exit 2
}

if (Test-Path -LiteralPath $log) {
    Remove-Item -LiteralPath $log -Force
}

Start-Transcript -Path $log -Force | Out-Null
try {
    & $exe @args
    $exitCode = $LASTEXITCODE
    Write-Host ''
    Write-Host "Exit code: $exitCode"
}
finally {
    Stop-Transcript | Out-Null
}

if (Test-Path -LiteralPath $log) {
    $content = Get-Content -LiteralPath $log -Raw
    $content = [regex]::Replace($content, '(?m)^Password for .*$','Password prompt: [masked input omitted]')
    Set-Content -LiteralPath $log -Value $content -Encoding UTF8
}

Write-Host ''
Write-Host "Log file: $log"
Read-Host "Press Enter to close"
exit $exitCode
