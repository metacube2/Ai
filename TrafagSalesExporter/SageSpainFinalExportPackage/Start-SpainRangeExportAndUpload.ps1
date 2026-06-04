param(
    [datetime]$FromDate = "2026-06-01",
    [datetime]$ToDate = "2026-06-04",
    [string]$ServerInstance = "localhost",
    [string]$Database = "Sage",
    [string]$BaseDirectory = "C:\Trafag\SageSpain",
    [string]$RcloneExe = "C:\Tools\rclone.exe",
    [string]$RcloneRemote = "trafag-bi",
    [string]$RcloneTarget = "Import/Finance/Spanien"
)

$ErrorActionPreference = "Stop"

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$workflowScript = Join-Path $scriptDirectory "Run-SpainExportAndUpload.ps1"

if (-not (Test-Path -LiteralPath $workflowScript)) {
    throw "Workflow script not found: $workflowScript"
}

if (-not (Test-Path -LiteralPath $RcloneExe)) {
    throw "rclone not found: $RcloneExe"
}

Write-Host "Starting Spain Sage range export and SharePoint upload..."
Write-Host "FromDate: $($FromDate.ToString("yyyy-MM-dd"))"
Write-Host "ToDate:   $($ToDate.ToString("yyyy-MM-dd"))"
Write-Host "Target:   ${RcloneRemote}:$RcloneTarget"

& $workflowScript `
    -ServerInstance $ServerInstance `
    -Database $Database `
    -ExportMode Range `
    -DateFilter LineRegistrationDate `
    -FromDate $FromDate `
    -ToDate $ToDate `
    -BaseDirectory $BaseDirectory `
    -RcloneExe $RcloneExe `
    -RcloneRemote $RcloneRemote `
    -RcloneTarget $RcloneTarget

if ($LASTEXITCODE -ne 0) {
    throw "Spain range export and upload failed with exit code $LASTEXITCODE"
}

Write-Host "Finished Spain Sage range export and SharePoint upload."
