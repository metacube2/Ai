# Baut das Paket fuer andere Benutzer: eigenstaendiges Programm (mit .NET-Laufzeit), Skripte, Anleitung, ZIP.
# Aufruf aus dem Repo-Ordner: powershell -ExecutionPolicy Bypass -File .\paket\Baue-Paket.ps1 [-ZipPfad <Datei>]
[CmdletBinding()]
param(
    [string]$ZipPfad = (Join-Path $env:USERPROFILE ("Downloads\AgentMonitor_Paket_{0}.zip" -f (Get-Date -Format 'yyyy-MM-dd')))
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$work = Join-Path ([System.IO.Path]::GetTempPath()) ("AgentMonitor_Paket_" + [guid]::NewGuid().ToString('N'))
$pkg = Join-Path $work 'AgentMonitor_Paket'
$bin = Join-Path $pkg 'bin'
New-Item -ItemType Directory -Force -Path $bin | Out-Null

Push-Location $repo
try {
    # Beide Programme in denselben Ordner: gleiche Laufzeit, die Dateien werden gemeinsam genutzt
    dotnet publish src/AgentMonitor.App -c Release -r win-x64 --self-contained true -p:PublishReadyToRun=true -o $bin --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Build der App fehlgeschlagen' }
    dotnet publish src/AgentMonitor.StatusLine -c Release -r win-x64 --self-contained true -p:PublishReadyToRun=true -o $bin --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Build der Statuszeile fehlgeschlagen' }
} finally { Pop-Location }

Get-ChildItem $bin -Filter '*.pdb' | Remove-Item -Force
foreach ($f in 'Setup-AgentMonitor.ps1', 'Setup-AgentMonitor.cmd', 'Entfernen-AgentMonitor.ps1', 'Entfernen-AgentMonitor.cmd', 'ANLEITUNG.md') {
    Copy-Item (Join-Path $PSScriptRoot $f) $pkg
}
Set-Content -Path (Join-Path $pkg 'VERSION.txt') -Encoding UTF8 -Value @(
    "Agenten-Monitor Paket vom $(Get-Date -Format 'yyyy-MM-dd HH:mm')",
    "Repo-Stand: $(git -C $repo rev-parse --short HEAD)")

if (Test-Path $ZipPfad) { Remove-Item $ZipPfad -Force }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($pkg, $ZipPfad, [System.IO.Compression.CompressionLevel]::Optimal, $true)
Remove-Item -Recurse -Force $work
"{0}  ({1:N1} MB)" -f $ZipPfad, ((Get-Item $ZipPfad).Length / 1MB)
