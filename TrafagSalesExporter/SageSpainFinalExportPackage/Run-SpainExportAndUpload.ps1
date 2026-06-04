param(
    [string]$ServerInstance = "localhost",
    [string]$Database = "Sage",
    [ValidateSet("Full", "Range")]
    [string]$ExportMode = "Range",
    [ValidateSet("InvoiceDate", "LineRegistrationDate")]
    [string]$DateFilter = "LineRegistrationDate",
    [int]$Year = 2025,
    [datetime]$FromDate = (Get-Date).Date.AddDays(-1),
    [datetime]$ToDate = (Get-Date).Date,
    [string]$BaseDirectory = "C:\Trafag\SageSpain",
    [string]$RcloneExe = "rclone",
    [string]$RcloneRemote = "trafag-bi",
    [string]$RcloneTarget = "Import/Finance/Spanien"
)

$ErrorActionPreference = "Stop"

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$exportScript = Join-Path $scriptDirectory "Export-SageSpainSalesCsv.ps1"
if (-not (Test-Path -LiteralPath $exportScript)) {
    throw "Export script not found: $exportScript"
}

$outputDirectory = Join-Path $BaseDirectory "out"
$logDirectory = Join-Path $BaseDirectory "logs"
New-Item -ItemType Directory -Force -Path $outputDirectory, $logDirectory | Out-Null

$exportArgs = @(
    "-ServerInstance", $ServerInstance,
    "-Database", $Database,
    "-ExportMode", $ExportMode,
    "-DateFilter", $DateFilter,
    "-Year", $Year,
    "-OutputDirectory", $outputDirectory
)

if ($ExportMode -eq "Range") {
    $exportArgs += @(
        "-FromDate", $FromDate.ToString("yyyy-MM-dd"),
        "-ToDate", $ToDate.ToString("yyyy-MM-dd")
    )
}

& $exportScript @exportArgs
if ($LASTEXITCODE -ne 0) {
    throw "Spain Sage export failed with exit code $LASTEXITCODE"
}

$latestRun = Get-ChildItem -LiteralPath $outputDirectory -Directory |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1
if ($null -eq $latestRun) {
    throw "No export run directory found in $outputDirectory"
}

$rcloneLog = Join-Path $logDirectory ("rclone-spain-" + (Get-Date -Format "yyyyMMdd") + ".log")
$target = "${RcloneRemote}:$RcloneTarget"

& $RcloneExe copy $latestRun.FullName $target `
    --include "*.csv" `
    --include "*_summary.txt" `
    --log-file $rcloneLog `
    --log-level INFO
if ($LASTEXITCODE -ne 0) {
    throw "rclone upload failed with exit code $LASTEXITCODE"
}

Write-Host "Spain export and upload finished."
Write-Host "Local export: $($latestRun.FullName)"
Write-Host "SharePoint target: $target"
Write-Host "rclone log: $rcloneLog"
