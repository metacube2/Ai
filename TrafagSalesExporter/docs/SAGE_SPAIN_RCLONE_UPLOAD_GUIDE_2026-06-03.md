# Sage Spain Rclone Upload Guide

Status: 2026-06-03

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

Recommended folder:

```text
C:\Trafag\SageSpain
```

Required files:

```text
Export-SageSpainSalesCsv.ps1
Run-SpainExportAndUpload.ps1
```

The files are included in:

```text
SageSpainFinalExportPackage.zip
```

## Install rclone

If `winget` is available:

```powershell
winget install Rclone.Rclone
```

Alternatively, install the rclone ZIP manually, for example to:

```text
C:\Tools\rclone\rclone.exe
```

Test the installation:

```powershell
rclone version
```

If `rclone` is not in the PATH, use the full path later:

```powershell
C:\Tools\rclone\rclone.exe version
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

Default: daily delta run, yesterday until today:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
cd C:\Trafag\SageSpain
.\Run-SpainExportAndUpload.ps1
```

Explicit date range:

```powershell
.\Run-SpainExportAndUpload.ps1 -ExportMode Range -DateFilter LineRegistrationDate -FromDate "2026-06-02" -ToDate "2026-06-03"
```

Full export with upload:

```powershell
.\Run-SpainExportAndUpload.ps1 -ExportMode Full -Year 2025
```

If rclone is not in the PATH:

```powershell
.\Run-SpainExportAndUpload.ps1 -RcloneExe "C:\Tools\rclone\rclone.exe"
```

If the rclone remote has another name:

```powershell
.\Run-SpainExportAndUpload.ps1 -RcloneRemote "YOUR_REMOTE_NAME"
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
  -Argument "-NoProfile -ExecutionPolicy Bypass -File C:\Trafag\SageSpain\Run-SpainExportAndUpload.ps1"

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
  -Argument "-NoProfile -ExecutionPolicy Bypass -File C:\Trafag\SageSpain\Run-SpainExportAndUpload.ps1 -RcloneExe C:\Tools\rclone\rclone.exe"
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
- Use `-RcloneExe "C:\Tools\rclone\rclone.exe"`.

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
