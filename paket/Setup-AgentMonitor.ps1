# Richtet den Agenten-Monitor fuer den aktuellen Windows-Benutzer ein (ohne Adminrechte, ohne .NET-Installation).
#
#   1. kopiert das Programm nach %LOCALAPPDATA%\AgentMonitor\app
#   2. traegt in ~/.claude/settings.json die HTTP-Hooks und die Statuszeile ein (bestehende Eintraege bleiben)
#   3. startet den Monitor (er traegt sich beim ersten Start selbst in den Autostart ein)
#
# Aufruf: Setup-AgentMonitor.cmd (Doppelklick) oder
#   powershell -ExecutionPolicy Bypass -File .\Setup-AgentMonitor.ps1
# Mehrfaches Ausfuehren ist ungefaehrlich (Aktualisierung). Vorher wird eine Sicherung der settings.json angelegt.
[CmdletBinding()]
param(
    [string]$SettingsPath = (Join-Path $env:USERPROFILE '.claude\settings.json'),
    [string]$InstallDir = (Join-Path $env:LOCALAPPDATA 'AgentMonitor'),
    [switch]$NoStart,   # fuer Tests: Monitor nicht starten
    [switch]$Force      # eine fremde statusLine ueberschreiben
)

$ErrorActionPreference = 'Stop'
$HookUrl = 'http://127.0.0.1:47811/hook'
$HookEvents = @('UserPromptSubmit', 'PreToolUse', 'PostToolUse', 'PostToolUseFailure', 'SubagentStart', 'SubagentStop',
                'Stop', 'StopFailure', 'Notification', 'TaskCreated', 'TaskCompleted', 'PreCompact', 'PostCompact', 'SessionEnd')
# Nicht SessionStart: dafuer unterstuetzt Claude Code keine HTTP-Hooks.

$source = Join-Path $PSScriptRoot 'bin'
if (-not (Test-Path (Join-Path $source 'AgentMonitor.exe'))) {
    throw "Ordner 'bin' mit AgentMonitor.exe fehlt neben diesem Skript ($source). Das ZIP vollstaendig entpacken."
}

$appDir = Join-Path $InstallDir 'app'

# 1. Programm kopieren (laufenden Monitor vorher beenden, sonst sind die Dateien gesperrt)
Get-Process -Name 'AgentMonitor' -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -and $_.Path.StartsWith($InstallDir, [StringComparison]::OrdinalIgnoreCase) } |
    ForEach-Object { Write-Host "Beende laufenden Monitor (PID $($_.Id))"; $_ | Stop-Process -Force; $_.WaitForExit(5000) | Out-Null }
New-Item -ItemType Directory -Force -Path $appDir | Out-Null
Copy-Item -Path (Join-Path $source '*') -Destination $appDir -Recurse -Force
# Aus dem Internet geladenes ZIP: Dateien freigeben, sonst fragt Windows beim Start nach
Get-ChildItem -Path $appDir -Recurse -File | Unblock-File -ErrorAction SilentlyContinue
Write-Host "Programm kopiert nach $appDir"

# 2. settings.json zusammenfuehren
$statusExe = (Join-Path $appDir 'AgentMonitorStatus.exe') -replace '\\', '/'   # Git-Bash verschluckt Backslashes
$settingsDir = Split-Path $SettingsPath -Parent
if (-not (Test-Path $settingsDir)) {
    Write-Warning "Ordner $settingsDir fehlt. Ist Claude Code installiert und einmal gestartet? Lege ihn an."
    New-Item -ItemType Directory -Force -Path $settingsDir | Out-Null
}

if (Test-Path $SettingsPath) {
    $raw = [System.IO.File]::ReadAllText($SettingsPath)
    $backup = "$SettingsPath.before-agentmonitor-$(Get-Date -Format 'yyyyMMdd-HHmmss').bak"
    [System.IO.File]::WriteAllText($backup, $raw, (New-Object System.Text.UTF8Encoding($false)))
    Write-Host "Sicherung: $backup"
    $settings = if ($raw.Trim().Length -gt 0) { $raw | ConvertFrom-Json } else { [pscustomobject]@{} }
} else {
    $settings = [pscustomobject]@{}
}

function Set-Prop($obj, [string]$name, $value) {
    if ($obj.PSObject.Properties[$name]) { $obj.$name = $value }
    else { $obj | Add-Member -NotePropertyName $name -NotePropertyValue $value }
}

if (-not $settings.PSObject.Properties['hooks'] -or $null -eq $settings.hooks) { Set-Prop $settings 'hooks' ([pscustomobject]@{}) }
$added = 0
foreach ($evt in $HookEvents) {
    $groups = @()
    if ($settings.hooks.PSObject.Properties[$evt] -and $null -ne $settings.hooks.$evt) { $groups = @($settings.hooks.$evt) }
    $already = $false
    foreach ($g in $groups) { foreach ($h in @($g.hooks)) { if ($h.url -eq $HookUrl) { $already = $true } } }
    if (-not $already) {
        $new = [pscustomobject]@{ hooks = @([pscustomobject]@{ type = 'http'; url = $HookUrl; timeout = 2 }) }
        Set-Prop $settings.hooks $evt ([object[]](@($groups) + @($new)))
        $added++
    }
}
Write-Host "Hooks: $added neu eingetragen, $($HookEvents.Count - $added) waren schon da"

$ownStatus = [pscustomobject]@{ type = 'command'; command = $statusExe; refreshInterval = 15 }
$existing = if ($settings.PSObject.Properties['statusLine']) { $settings.statusLine } else { $null }
if ($null -eq $existing) {
    Set-Prop $settings 'statusLine' $ownStatus; Write-Host 'Statuszeile eingetragen'
} elseif ("$($existing.command)" -match 'AgentMonitorStatus\.exe') {
    Set-Prop $settings 'statusLine' $ownStatus; Write-Host 'Statuszeile aktualisiert (war schon vom Agenten-Monitor)'
} elseif ($Force) {
    Set-Prop $settings 'statusLine' $ownStatus; Write-Warning "Fremde Statuszeile ueberschrieben (-Force). Alt: $($existing | ConvertTo-Json -Compress)"
} else {
    Write-Warning ("Es gibt schon eine andere Statuszeile ($($existing.command)); sie bleibt unveraendert. " +
                   "Der Planverbrauch (5 Stunden/Woche) erscheint dann nicht im Monitor. Mit -Force ersetzen.")
}

$json = $settings | ConvertTo-Json -Depth 30
[System.IO.File]::WriteAllText($SettingsPath, $json, (New-Object System.Text.UTF8Encoding($false)))
# Gegenprobe: Datei muss wieder lesbar sein
$null = [System.IO.File]::ReadAllText($SettingsPath) | ConvertFrom-Json
Write-Host "settings.json geschrieben: $SettingsPath"

# 3. Starten
if ($NoStart) { Write-Host 'Start uebersprungen (-NoStart).'; return }
Start-Process -FilePath (Join-Path $appDir 'AgentMonitor.exe') -WorkingDirectory $appDir
Start-Sleep -Seconds 3
try {
    $r = Invoke-WebRequest -UseBasicParsing -Uri 'http://127.0.0.1:47811/health' -TimeoutSec 3
    Write-Host "Monitor laeuft (Health $($r.StatusCode))."
} catch {
    Write-Warning "Monitor antwortet nicht auf http://127.0.0.1:47811/health. Im Tray nachsehen oder $appDir\AgentMonitor.exe starten."
}
Write-Host ''
Write-Host 'Fertig. Bereits geoeffnete Claude-Sitzungen neu starten, damit Hooks und Statuszeile greifen.'
