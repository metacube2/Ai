param(
    [string]$ServerInstance = "localhost",
    [string]$Database = "Sage",
    [ValidateSet("Full", "Range")]
    [string]$ExportMode = "Range",
    [ValidateSet("InvoiceDate", "LineRegistrationDate")]
    [string]$DateFilter = "LineRegistrationDate",
    [int]$Year = 2025,
    [datetime]$FromDate = (Get-Date).Date.AddDays(-7),
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

if (-not (Get-Command $RcloneExe -ErrorAction SilentlyContinue)) {
    throw "rclone executable not found: $RcloneExe"
}

$target = "${RcloneRemote}:$RcloneTarget"
$rcloneLog = Join-Path $logDirectory ("rclone-spain-" + (Get-Date -Format "yyyyMMdd") + ".log")

Write-Host "Checking SharePoint target with rclone: $target"
& $RcloneExe mkdir $target --log-file $rcloneLog --log-level INFO
if ($LASTEXITCODE -ne 0) {
    throw "Could not create/check SharePoint target '$target'. rclone exit code $LASTEXITCODE. Log: $rcloneLog"
}

& $RcloneExe lsf $target --max-depth 1 --log-file $rcloneLog --log-level INFO | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "SharePoint target '$target' is not reachable. rclone exit code $LASTEXITCODE. Log: $rcloneLog"
}

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

$filesToUpload = Get-ChildItem -LiteralPath $latestRun.FullName -File |
    Where-Object { $_.Name -like "*.csv" -or $_.Name -like "*_summary.txt" }
if ($filesToUpload.Count -eq 0) {
    throw "No CSV or summary files found for upload in $($latestRun.FullName)"
}

Write-Host "Uploading $($filesToUpload.Count) file(s) to SharePoint target: $target"

& $RcloneExe copy $latestRun.FullName $target `
    --include "*.csv" `
    --include "*_summary.txt" `
    --verbose `
    --log-file $rcloneLog `
    --log-level INFO
if ($LASTEXITCODE -ne 0) {
    throw "rclone upload failed with exit code $LASTEXITCODE"
}

foreach ($file in $filesToUpload) {
    $uploadedMatch = & $RcloneExe lsf $target --files-only --include $file.Name --log-file $rcloneLog --log-level INFO
    if ($LASTEXITCODE -ne 0) {
        throw "Could not verify uploaded file '$($file.Name)' in '$target'. rclone exit code $LASTEXITCODE. Log: $rcloneLog"
    }

    if (-not ($uploadedMatch | Where-Object { $_ -eq $file.Name })) {
        throw "Upload verification failed. File '$($file.Name)' was not listed in '$target'. Log: $rcloneLog"
    }
}

Write-Host "Spain export and upload finished."
Write-Host "Local export: $($latestRun.FullName)"
Write-Host "SharePoint target: $target"
Write-Host "rclone log: $rcloneLog"
