param(
    [int]$Seconds,
    [string]$OutputFile = 'recording.wav',
    [string]$Device,
    [switch]$ListDevices
)

$ErrorActionPreference = 'Stop'
$Root = $PSScriptRoot
$env:TEMP = Join-Path $Root 'temp'
$env:TMP = Join-Path $Root 'temp'
New-Item -ItemType Directory -Force -Path $env:TEMP, (Join-Path $Root 'input') | Out-Null

$Python = Join-Path $Root 'python\python.exe'
$Script = Join-Path $Root 'scripts\record_mic.py'

if ($ListDevices) {
    & $Python $Script --seconds 1 --list-devices
    exit $LASTEXITCODE
}

if (-not $Seconds) { throw 'Seconds is required unless -ListDevices is used.' }
$argsList = @($Script, '--seconds', $Seconds, '--output', $OutputFile)
if ($Device) { $argsList += @('--device', $Device) }
& $Python @argsList
exit $LASTEXITCODE
