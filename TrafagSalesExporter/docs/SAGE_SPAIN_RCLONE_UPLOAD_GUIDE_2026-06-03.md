# Sage Spain Rclone Upload Guide

Status: 2026-06-05

Purpose: The Sage server in Spain creates the sales CSV locally and then automatically uploads the file to the SharePoint folder used by the dashboard import.

## Target Folder

SharePoint URL:

```text
https://trafagag.sharepoint.com/sites/WorldwideBIPlatform/Shared%20Documents/Import/Finance/Spanien
```

Technical folder:

```text
Shared Documents/Import/Finance/Spanien
```

Recommended rclone target path:

```text
trafag-bi:Import/Finance/Spanien
```

The rclone remote `trafag-bi` should point to the `Shared Documents` document library of this SharePoint site:

```text
https://trafagag.sharepoint.com/sites/WorldwideBIPlatform
```

## Required Files On The Spain Server

Recommended folder for export output and logs:

```text
C:\Trafag\SageSpain
```

Recommended script folder:

```text
C:\Tools\rclone
```

Required file for the current single-file workflow:

```text
Run-SpainRangeExportAndUpload-AllInOne.ps1
```

This all-in-one script does not require `Export-SageSpainSalesCsv.ps1` or `Run-SpainExportAndUpload.ps1`.

## Install rclone

If `winget` is available:

```powershell
winget install Rclone.Rclone
```

Alternatively, install the rclone ZIP manually, for example to one of these paths:

```text
C:\Tools\rclone\rclone.exe
C:\Tools\rclone\rclone\rclone.exe
```

Test the installation:

```powershell
rclone version
```

If `rclone` is not in the PATH, use the full path later:

```powershell
C:\Tools\rclone\rclone.exe version
```

The current all-in-one script auto-detects rclone in:

```text
C:\Tools\rclone.exe
C:\Tools\rclone\rclone.exe
C:\Tools\rclone\rclone\rclone.exe
```

## Configure The rclone Remote

On the Spain server:

```powershell
rclone config
```

Recommended input:

```text
n
name> trafag-bi
Storage> onedrive
```

Then complete the Microsoft login.

Important:

- Site: `WorldwideBIPlatform`
- Document library: `Shared Documents`
- The rclone remote `trafag-bi` should point to the document library `Shared Documents`.

## Test rclone

```powershell
rclone lsd trafag-bi:
rclone lsd trafag-bi:"Import"
rclone lsd trafag-bi:"Import/Finance"
rclone lsd trafag-bi:"Import/Finance/Spanien"
```

If the last command lists the folder without an error, the target path is correct.

## Manual Export Without Upload

Full export for 2025:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
cd C:\Trafag\SageSpain
.\Export-SageSpainSalesCsv.ps1 -ExportMode Full -Year 2025 -OutputDirectory C:\Trafag\SageSpain\out
```

Delta/range export:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
cd C:\Trafag\SageSpain
.\Export-SageSpainSalesCsv.ps1 -ExportMode Range -DateFilter LineRegistrationDate -FromDate "2026-06-02" -ToDate "2026-06-03" -OutputDirectory C:\Trafag\SageSpain\out
```

Notes:

- `ToDate` is exclusive.
- The range `"2026-06-02"` to `"2026-06-03"` exports June 2.
- For daily delta exports, `LineRegistrationDate` is recommended because it captures newly registered or changed lines.

## Run Export And Upload Together

Current recommended command: one file, range export and upload.

Default: last 7 days until today. `ToDate` is exclusive.

```powershell
Set-ExecutionPolicy -Scope Process Bypass
cd C:\Tools\rclone
.\Run-SpainRangeExportAndUpload-AllInOne.ps1
```

Explicit date range:

```powershell
.\Run-SpainRangeExportAndUpload-AllInOne.ps1 -FromDate "2026-06-01" -ToDate "2026-06-04"
```

If rclone is in a non-standard location:

```powershell
.\Run-SpainRangeExportAndUpload-AllInOne.ps1 -RcloneExe "C:\Tools\rclone\rclone\rclone.exe"
```

Older two-file wrapper, only use if both scripts are present in the same folder:

```powershell
.\Run-SpainExportAndUpload.ps1
```

This older wrapper requires:

```text
Export-SageSpainSalesCsv.ps1
Run-SpainExportAndUpload.ps1
```

## What Gets Uploaded?

The wrapper script uploads these files from the newest export folder:

```text
*.csv
*_summary.txt
```

Target:

```text
trafag-bi:Import/Finance/Spanien
```

The script does not change any data in Sage or SQL Server.

## Windows Task Scheduler

Recommended daily run, for example at 02:00:

```powershell
$action = New-ScheduledTaskAction `
  -Execute "powershell.exe" `
  -Argument "-NoProfile -ExecutionPolicy Bypass -File C:\Tools\rclone\Run-SpainRangeExportAndUpload-AllInOne.ps1"

$trigger = New-ScheduledTaskTrigger -Daily -At 02:00

Register-ScheduledTask `
  -TaskName "Trafag Spain Sage Export Upload" `
  -Action $action `
  -Trigger $trigger `
  -Description "Exports Sage Spain sales CSV and uploads it to SharePoint via rclone"
```

If rclone is not in the PATH:

```powershell
$action = New-ScheduledTaskAction `
  -Execute "powershell.exe" `
  -Argument "-NoProfile -ExecutionPolicy Bypass -File C:\Tools\rclone\Run-SpainRangeExportAndUpload-AllInOne.ps1 -RcloneExe C:\Tools\rclone\rclone\rclone.exe"
```

## Check After The Run

Local output:

```powershell
Get-ChildItem C:\Trafag\SageSpain\out -Directory | Sort-Object LastWriteTime -Descending | Select-Object -First 1
Get-ChildItem C:\Trafag\SageSpain\logs
```

SharePoint via rclone:

```powershell
rclone ls trafag-bi:"Import/Finance/Spanien"
```

Browser check:

```text
https://trafagag.sharepoint.com/sites/WorldwideBIPlatform/Shared%20Documents/Import/Finance/Spanien
```

## Common Issues

`rclone: command not found`

- rclone is not in the PATH.
- Use `-RcloneExe "C:\Tools\rclone\rclone.exe"` or `-RcloneExe "C:\Tools\rclone\rclone\rclone.exe"`.

`Export script not found`

- You started the older wrapper `Run-SpainExportAndUpload.ps1`.
- For the single-file workflow start `Run-SpainRangeExportAndUpload-AllInOne.ps1`.

`CRITICAL: Can't set -v and --log-level`

- The server is running an old copy of the script that still contains `--verbose`.
- Remove the line `--verbose \`` from the rclone `copy` block, or replace the file with the current all-in-one script.
- The corrected upload block keeps `--log-level INFO` and does not use `--verbose`.

`directory not found`

- The remote may not point to `Shared Documents`, or the target folder may be different.
- Check with `rclone lsd trafag-bi:` and `rclone lsd trafag-bi:"Import/Finance"`.

`Access denied`

- Microsoft login or SharePoint permissions are missing.
- The Windows user running the scheduled task must have access to the rclone configuration and to SharePoint.

Empty delta file:

- Check the date range.
- `ToDate` is exclusive.
- For a daily run, yesterday until today is correct.
