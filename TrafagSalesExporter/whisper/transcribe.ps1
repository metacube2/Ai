param(
    [Parameter(Mandatory=$true, Position=0)]
    [string]$InputFile,

    [string]$OutputFile,
    [ValidateSet('auto','de','en')]
    [string]$Language = 'auto',
    [string]$Model = 'medium',
    [switch]$Timestamps,
    [ValidateSet('transcribe','translate')]
    [string]$Task = 'transcribe'
)

$ErrorActionPreference = 'Stop'
$Root = $PSScriptRoot
$env:HF_HOME = Join-Path $Root 'cache\hf'
$env:HF_HUB_CACHE = Join-Path $Root 'cache\hf\hub'
$env:XDG_CACHE_HOME = Join-Path $Root 'cache'
$env:HF_HUB_DISABLE_SYMLINKS_WARNING = '1'
$env:PIP_CACHE_DIR = Join-Path $Root 'cache\pip'
$env:TEMP = Join-Path $Root 'temp'
$env:TMP = Join-Path $Root 'temp'
# Stream progress lines to the GUI as they happen instead of at exit.
$env:PYTHONUNBUFFERED = '1'

New-Item -ItemType Directory -Force -Path $env:HF_HOME, $env:HF_HUB_CACHE, $env:PIP_CACHE_DIR, $env:TEMP, (Join-Path $Root 'output') | Out-Null

$Python = Join-Path $Root 'python\python.exe'
$Script = Join-Path $Root 'scripts\transcribe.py'
$argsList = @($Script, $InputFile, '--model', $Model, '--language', $Language)
if ($OutputFile) { $argsList += @('--output', $OutputFile) }
if ($Timestamps) { $argsList += '--timestamps' }
if ($Task -ne 'transcribe') { $argsList += @('--task', $Task) }

& $Python @argsList
exit $LASTEXITCODE
