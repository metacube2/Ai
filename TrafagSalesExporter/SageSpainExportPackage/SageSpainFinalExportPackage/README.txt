Sage Spain final sales export candidate
======================================

Run on the Spain Sage SQL Server machine.

PowerShell commands:

Set-ExecutionPolicy -Scope Process Bypass
.\Export-SageSpainSalesCsv.ps1

Full export, default year 2025:

.\Export-SageSpainSalesCsv.ps1 -ExportMode Full -Year 2025

Range export, explicit window:

.\Export-SageSpainSalesCsv.ps1 -ExportMode Range -FromDate "2025-05-01" -ToDate "2025-06-01"

Range export by registration date, useful for new/changed records registered in a period:

.\Export-SageSpainSalesCsv.ps1 -ExportMode Range -DateFilter LineRegistrationDate -FromDate "2025-05-01" -ToDate "2025-06-01"

Output folder on Desktop:

Sage_Spain_Sales_Export_YYYYMMDD_HHMMSS

Files created:

- Spain_Sales_full_YYYY0101_to_YYYY1231.csv for full export
- Spain_Sales_range_YYYYMMDD_to_YYYYMMDD.csv for range export
- Matching *_summary.txt file

The script only reads SQL Server data. It does not change Sage or SQL Server.

Default source:

- Database: Sage
- Header: dbo.CabeceraAlbaranCliente
- Lines: dbo.LineasAlbaranCliente
- Full export date filter: CabeceraAlbaranCliente.FechaFactura from YYYY-01-01 to next YYYY-01-01
- Range export date filter: explicit FromDate/ToDate
- DateFilter InvoiceDate uses CabeceraAlbaranCliente.FechaFactura
- DateFilter LineRegistrationDate uses LineasAlbaranCliente.FechaRegistro, with fallback to FechaFactura
- Sales value: LineasAlbaranCliente.ImporteNeto
- Posting date: dbo.FacturasTB.FechaAsiento, joined per invoice, exported as PostingDate
- Posting document: dbo.FacturasTB.Asiento, exported as PostingDocument


Posting date (new columns PostingDate and PostingDocument)
==========================================================

Added 2026-08-17 for issue ISS-004.2. Until then Spain was the only site without a
posting date, so every Spanish row fell back to the invoice date for the fiscal year.

The delivery note header and lines do not carry a posting date. It lives in
dbo.FacturasTB as FechaAsiento, together with the posting document number Asiento.
The lookup uses OUTER APPLY with TOP 1 on purpose, not a plain JOIN: some invoice keys
appear more than once in FacturasTB, and a JOIN would multiply the sales lines and
inflate the Spanish revenue. OUTER APPLY returns at most one row per sales line, and
rows without a posting stay in the export with an empty PostingDate.

The join key is CodigoEmpresa, Ejercicio, Serie, Factura against the invoice fields of
CabeceraAlbaranCliente. It is a reasoned assumption, not yet verified against the live
Spanish database. Please check on first run:

1. How many exported rows have an empty PostingDate.
2. Whether PostingDate and InvoiceDate differ, and by how much.
3. How credit notes behave (SerieFactura = 'REC' or StatusAbono <> 0).
4. That the exported row count did NOT grow compared with the previous run. A higher
   count means the lookup returned more than one row and must be corrected.

Background: docs/FINANCE_ES_BUCHUNGSDATUM_2026-08-03.md in the BiDashboard repository.


If the SQL instance or database name differs:

.\Export-SageSpainSalesCsv.ps1 -ServerInstance "localhost" -Database "Sage" -ExportMode Full -Year 2025


Automatic upload to SharePoint with rclone
==========================================

Target SharePoint folder:

https://trafagag.sharepoint.com/sites/WorldwideBIPlatform/Shared%20Documents/Import/Finance/Spanien

Decoded folder path:

Shared Documents/Import/Finance/Spanien

Recommended rclone setup on the Spain Sage server:

1. Install rclone.
2. Run:

rclone config

3. Create a new remote for the SharePoint document library.

Recommended remote name:

trafag-bi

The remote should point to the document library root "Shared Documents" of:

https://trafagag.sharepoint.com/sites/WorldwideBIPlatform

Then this target path is used by the wrapper script:

trafag-bi:Import/Finance/Spanien

Test rclone:

rclone lsd trafag-bi:
rclone lsd trafag-bi:"Import/Finance"
rclone lsd trafag-bi:"Import/Finance/Spanien"

Run range export and upload with default window last 7 days until today:

.\Run-SpainExportAndUpload.ps1

Explicit range:

.\Run-SpainExportAndUpload.ps1 -ExportMode Range -DateFilter LineRegistrationDate -FromDate "2026-06-02" -ToDate "2026-06-03"

Simple starter script with default window last 7 days until today:

.\Start-SpainRangeExportAndUpload.ps1

Same starter script with another range:

.\Start-SpainRangeExportAndUpload.ps1 -FromDate "2026-06-01" -ToDate "2026-06-04"

Single-file all-in-one range export and upload.
This file does not require Export-SageSpainSalesCsv.ps1 or Run-SpainExportAndUpload.ps1:

.\Run-SpainRangeExportAndUpload-AllInOne.ps1

Default date window:

- FromDate = today - 35 days
- ToDate = today
- ToDate is exclusive

Widened from 7 to 35 days on 2026-08-17 (ISS-004.2 follow-up): posting in
FacturasTB lags roughly 2-3 weeks behind the invoice date, so a 7-day window
never re-exported an invoice after it finally got a PostingDate. The app
dedupes Spain rows by SourceLineId and the newer delta row wins, so the wider,
overlapping window is safe and does not create duplicates. Verified live with
Analyze-SpainPostingDateKey.ps1: the OUTER APPLY key matched 53 of 53 already
posted invoices (100%) once the compared window actually contained posted
data; on 2026-08-17 the most recent posted FechaFactura was 2026-07-30.

Override the all-in-one default date window:

.\Run-SpainRangeExportAndUpload-AllInOne.ps1 -FromDate "2026-06-01" -ToDate "2026-06-04"

The all-in-one script checks/creates the SharePoint folder before export, uploads the generated CSV and summary, and verifies that the uploaded files are listed in SharePoint.

rclone.exe lookup for the all-in-one script:

- explicit -RcloneExe parameter
- rclone.exe in the same folder as the script
- C:\Tools\rclone.exe
- C:\Tools\rclone\rclone.exe
- C:\Tools\rclone\rclone\rclone.exe
- rclone from PATH

Known rclone upload issue:

If the log says:

CRITICAL: Can't set -v and --log-level

then the server is running an old script copy that still contains:

--verbose `

Remove that line from the rclone copy block or replace the file with the current Run-SpainRangeExportAndUpload-AllInOne.ps1.

Full export and upload:

.\Run-SpainExportAndUpload.ps1 -ExportMode Full -Year 2025

If the rclone remote has another name:

.\Run-SpainExportAndUpload.ps1 -RcloneRemote "YOUR_REMOTE_NAME"

If rclone.exe is not in PATH:

.\Run-SpainExportAndUpload.ps1 -RcloneExe "C:\Tools\rclone\rclone.exe"

Suggested Windows Task Scheduler command:

powershell.exe -NoProfile -ExecutionPolicy Bypass -File C:\Trafag\SageSpain\Run-SpainExportAndUpload.ps1

Important:

- The export script only reads SQL Server data.
- rclone only uploads the generated CSV and matching summary file.
- For daily deltas use ExportMode Range with DateFilter LineRegistrationDate.
- ToDate is exclusive.
