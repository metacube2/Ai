<#
Runs the daily Alphaplan delta export and uploads a dated ZIP to SharePoint with rclone.
Designed for Windows Task Scheduler.

One-time credential setup -- run this ONCE manually on the target machine AS the Windows
user that the Task Scheduler job runs as:

  $cred = Get-Credential
  $cred | Export-Clixml -Path "<path-to-this-scripts-folder>\alphaplan-sql-cred.xml"

The XML is DPAPI-encrypted: it only works on the same machine and under the same Windows
user that created it. Re-create it if the service account or machine changes.

Manual test:
  powershell.exe -ExecutionPolicy Bypass -File "<path-to-scripts-folder>\runAlphaplanDailyDelta.ps1"
#>

param(
    [string]$ServerInstance   = "localhost\SQL2012",
    [string]$Database         = "ApDaten",
    [string]$DeltaScript      = "",   # default: alphaplandeltaexport.ps1 in the same folder
    [string]$CredentialPath   = "",   # default: alphaplan-sql-cred.xml in the same folder
    [string]$OutputDirectory  = "",   # default: <ScriptRoot>\AlphaplanDeltaExport
    [string]$ZipPath          = "",   # default: <ScriptRoot>\AlphaplanDeltaExport_yyyyMMdd.zip
    [int]   $DaysBack         = 7,
    [string]$RcloneExe        = "C:\Tools\rclone.exe",
    [string]$RcloneRemote     = "trafag-bi:Import/Finance/Deutschland/AlphaplanRaw",
    [switch]$NoCheckCertificate
)

$ErrorActionPreference = "Stop"

# Resolve all defaults relative to this script's folder, not C:\temp
$scriptDir = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($DeltaScript))     { $DeltaScript    = Join-Path $scriptDir "alphaplandeltaexport.ps1" }
if ([string]::IsNullOrWhiteSpace($CredentialPath))  { $CredentialPath = Join-Path $scriptDir "alphaplan-sql-cred.xml" }
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) { $OutputDirectory = Join-Path $scriptDir "AlphaplanDeltaExport" }

# Date stamp in ZIP name so each run creates a new file and does not overwrite previous exports
$dateStamp = Get-Date -Format "yyyyMMdd"
if ([string]::IsNullOrWhiteSpace($ZipPath)) { $ZipPath = Join-Path $scriptDir "AlphaplanDeltaExport_$dateStamp.zip" }

$zipFileName  = Split-Path $ZipPath -Leaf
$RcloneTarget = "$RcloneRemote/$zipFileName"

$logDir    = Join-Path $scriptDir "logs"
New-Item -ItemType Directory -Force -Path $logDir | Out-Null
$stamp     = Get-Date -Format "yyyyMMdd_HHmmss"
$runLog    = Join-Path $logDir "alphaplan-delta-run-$stamp.log"
$rcloneLog = Join-Path $logDir "rclone-alphaplan-delta-upload-$stamp.log"

function Write-Log {
    param([string]$Message)
    $line = "[{0}] {1}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $Message
    $line | Tee-Object -FilePath $runLog -Append
}

try {
    Write-Log "Starting daily Alphaplan delta export"
    Write-Log "Server:          $ServerInstance"
    Write-Log "Database:        $Database"
    Write-Log "DaysBack:        $DaysBack"
    Write-Log "DeltaScript:     $DeltaScript"
    Write-Log "CredentialPath:  $CredentialPath"
    Write-Log "OutputDirectory: $OutputDirectory"
    Write-Log "ZipPath:         $ZipPath"
    Write-Log "RcloneTarget:    $RcloneTarget"

    if (-not (Test-Path $DeltaScript))    { throw "Delta script not found: $DeltaScript" }
    if (-not (Test-Path $RcloneExe))      { throw "rclone.exe not found: $RcloneExe" }
    if (-not (Test-Path $CredentialPath)) {
        throw ("Credential file not found: $CredentialPath`n" +
               "Create it once on this machine as the Task Scheduler user:`n" +
               "  `$cred = Get-Credential`n" +
               "  `$cred | Export-Clixml -Path '$CredentialPath'")
    }

    $cred = Import-Clixml -Path $CredentialPath
    if ($null -eq $cred) { throw "Credential import returned null from: $CredentialPath" }

    Write-Log "Running delta export"
    # Call the delta script DIRECTLY in this process (not via powershell.exe -File).
    # PSCredential is a .NET object and cannot be serialized across a new-process boundary;
    # passing it via "powershell.exe -File script.ps1 -SqlCredential $cred" delivers $null.
    & $DeltaScript `
        -ServerInstance  $ServerInstance `
        -Database        $Database `
        -SqlCredential   $cred `
        -OutputDirectory $OutputDirectory `
        -DaysBack        $DaysBack `
        -NoZip
    # Exceptions from the delta script propagate naturally (ErrorActionPreference = Stop).

    if (-not (Test-Path $OutputDirectory)) { throw "Output directory missing after export: $OutputDirectory" }

    Write-Log "Creating ZIP: $ZipPath"
    if (Test-Path $ZipPath) { Remove-Item $ZipPath -Force }
    Compress-Archive -Path (Join-Path $OutputDirectory "*") -DestinationPath $ZipPath -Force
    if (-not (Test-Path $ZipPath)) { throw "ZIP was not created: $ZipPath" }
    $zipSize = (Get-Item $ZipPath).Length
    Write-Log "ZIP size: $zipSize bytes"
    if ($zipSize -le 0) { throw "ZIP is empty: $ZipPath" }

    Write-Log "Uploading to SharePoint via rclone: $RcloneTarget"
    $rcloneArgs = @("copyto", $ZipPath, $RcloneTarget, "--log-file", $rcloneLog, "--log-level", "INFO")
    if ($NoCheckCertificate) { $rcloneArgs += "--no-check-certificate" }
    & $RcloneExe @rcloneArgs
    if ($LASTEXITCODE -ne 0) { throw "rclone upload failed with exit code $LASTEXITCODE. See $rcloneLog" }

    Write-Log "Verifying upload (listing remote folder)"
    $lsArgs = @("lsf", $RcloneRemote, "-l")
    if ($NoCheckCertificate) { $lsArgs += "--no-check-certificate" }
    & $RcloneExe @lsArgs | Tee-Object -FilePath $runLog -Append

    Write-Log "Done"
    exit 0
}
catch {
    Write-Log "ERROR: $($_.Exception.Message)"
    if (Test-Path $rcloneLog) {
        Write-Log "Last rclone log lines:"
        Get-Content $rcloneLog -Tail 40 | Tee-Object -FilePath $runLog -Append
    }
    exit 1
}
