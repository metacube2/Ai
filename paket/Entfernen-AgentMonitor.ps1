# Entfernt den Agenten-Monitor wieder: Hooks und Statuszeile aus ~/.claude/settings.json, Autostart, Programm.
# Fremde Eintraege in der settings.json bleiben unberuehrt. Eine Sicherung wird vorher angelegt.
# Aufruf: Entfernen-AgentMonitor.cmd oder powershell -ExecutionPolicy Bypass -File .\Entfernen-AgentMonitor.ps1
[CmdletBinding()]
param(
    [string]$SettingsPath = (Join-Path $env:USERPROFILE '.claude\settings.json'),
    [string]$InstallDir = (Join-Path $env:LOCALAPPDATA 'AgentMonitor'),
    [switch]$KeepFiles,    # Programmdateien behalten
    [switch]$NoRegistry    # fuer Tests: Registry nicht anfassen
)

$ErrorActionPreference = 'Stop'
$HookUrl = 'http://127.0.0.1:47811/hook'

Get-Process -Name 'AgentMonitor', 'AgentMonitorStatus' -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -and $_.Path.StartsWith($InstallDir, [StringComparison]::OrdinalIgnoreCase) } |
    ForEach-Object { $_ | Stop-Process -Force }

if (Test-Path $SettingsPath) {
    $raw = [System.IO.File]::ReadAllText($SettingsPath)
    $backup = "$SettingsPath.before-agentmonitor-entfernen-$(Get-Date -Format 'yyyyMMdd-HHmmss').bak"
    [System.IO.File]::WriteAllText($backup, $raw, (New-Object System.Text.UTF8Encoding($false)))
    $settings = $raw | ConvertFrom-Json
    $removed = 0
    if ($settings.PSObject.Properties['hooks'] -and $null -ne $settings.hooks) {
        foreach ($p in @($settings.hooks.PSObject.Properties)) {
            $keep = @()
            foreach ($g in @($p.Value)) {
                $inner = @(@($g.hooks) | Where-Object { $_.url -ne $HookUrl })
                if ($inner.Count -lt @($g.hooks).Count) { $removed++ }
                if ($inner.Count -gt 0) { $g.hooks = [object[]]$inner; $keep += $g }
            }
            if ($keep.Count -gt 0) { $settings.hooks.($p.Name) = [object[]]$keep }
            else { $settings.hooks.PSObject.Properties.Remove($p.Name) }
        }
        if (@($settings.hooks.PSObject.Properties).Count -eq 0) { $settings.PSObject.Properties.Remove('hooks') }
    }
    if ($settings.PSObject.Properties['statusLine'] -and "$($settings.statusLine.command)" -match 'AgentMonitorStatus\.exe') {
        $settings.PSObject.Properties.Remove('statusLine'); Write-Host 'Statuszeile entfernt'
    }
    [System.IO.File]::WriteAllText($SettingsPath, ($settings | ConvertTo-Json -Depth 30), (New-Object System.Text.UTF8Encoding($false)))
    Write-Host "Hooks entfernt: $removed. Sicherung: $backup"
}

if (-not $NoRegistry) {
    Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'AgentMonitor' -ErrorAction SilentlyContinue
    Write-Host 'Autostart entfernt'
}
if (-not $KeepFiles -and (Test-Path $InstallDir)) {
    Remove-Item -Recurse -Force (Join-Path $InstallDir 'app') -ErrorAction SilentlyContinue
    Write-Host "Programm geloescht: $(Join-Path $InstallDir 'app')"
}
Write-Host "Einstellungen und gelernte Dauern bleiben unter $env:APPDATA\AgentMonitor (bei Bedarf von Hand loeschen)."
Write-Host 'Claude-Sitzungen neu starten.'
