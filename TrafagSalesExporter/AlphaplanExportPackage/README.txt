Alphaplan SQL Discovery Exporter
================================

Purpose
-------

Run this package on the German Alphaplan SQL Server machine.

It performs Phase 1 discovery only:

- scan accessible SQL Server databases
- identify tables/views that look relevant for finance, invoices, sales, customers, articles and amounts
- write candidate_objects.csv
- write export_summary.csv
- optionally write small sample_*.csv files
- optionally upload the run folder to SharePoint with rclone

The script only reads SQL Server metadata/data. It does not change Alphaplan, SQL Server or BiDashboard.


Default SharePoint target
-------------------------

The default rclone target is:

trafag-bi:Import/Finance/Deutschland/AlphaplanRaw

Use this raw folder first so the existing Germany import is not disturbed.


Typical commands
----------------

Open PowerShell on the Alphaplan server in this package folder.

Allow script execution for this PowerShell window:

Set-ExecutionPolicy -Scope Process Bypass

Run discovery and upload with defaults:

.\Run-AlphaplanDiscoveryAndUpload.ps1

Run discovery for one known database:

.\Run-AlphaplanDiscoveryAndUpload.ps1 -Database "ALPHAPLAN"

Run discovery without SharePoint upload:

.\Run-AlphaplanDiscoveryAndUpload.ps1 -SkipUpload

Run discovery and include small samples from top candidate tables/views:

.\Run-AlphaplanDiscoveryAndUpload.ps1 -Database "ALPHAPLAN" -ExportSamples

Use another SQL Server instance:

.\Run-AlphaplanDiscoveryAndUpload.ps1 -ServerInstance "SERVERNAME\INSTANCE" -Database "ALPHAPLAN"

Use SQL authentication:

$cred = Get-Credential
.\Run-AlphaplanDiscoveryAndUpload.ps1 -ServerInstance "SERVERNAME\INSTANCE" -Database "ALPHAPLAN" -SqlCredential $cred

Use another rclone remote name:

.\Run-AlphaplanDiscoveryAndUpload.ps1 -RcloneRemote "YOUR_REMOTE"

Use another rclone executable:

.\Run-AlphaplanDiscoveryAndUpload.ps1 -RcloneExe "C:\Tools\rclone\rclone.exe"


Output
------

Default local folder:

C:\Trafag\AlphaplanExport\out\Alphaplan_SQL_Discovery_YYYYMMDD_HHMMSS

Main files:

- candidate_objects.csv
- export_summary.csv
- README.txt
- sample_*.csv when -ExportSamples is used


rclone prerequisites
--------------------

rclone must already be configured on the Alphaplan server.

Expected remote:

trafag-bi

The remote should point to the "Shared Documents" document library of:

https://trafagag.sharepoint.com/sites/WorldwideBIPlatform

Quick rclone checks:

rclone lsd trafag-bi:
rclone lsd trafag-bi:"Import/Finance"
rclone lsd trafag-bi:"Import/Finance/Deutschland"


Recommended Phase 1 workflow
----------------------------

1. Run discovery without samples:

   .\Run-AlphaplanDiscoveryAndUpload.ps1 -SkipUpload

2. Check candidate_objects.csv locally.

3. If the result looks plausible, run with upload:

   .\Run-AlphaplanDiscoveryAndUpload.ps1

4. If Andreas/DE IT needs examples, run samples:

   .\Run-AlphaplanDiscoveryAndUpload.ps1 -ExportSamples

5. Use candidate_objects.csv to identify the correct invoice header, invoice line, customer, article and credit note/storno objects.


Notes
-----

- If -Database is empty, all accessible user databases are scanned.
- If the SQL user has limited permissions, candidate_objects.csv may be empty or incomplete.
- Use a read-only SQL user or Windows account.
- For Phase 1, no BiDashboard import mapping is required.

