Sage Spain final sales export candidate
======================================

Run on the Spain Sage SQL Server machine.

PowerShell commands:

Set-ExecutionPolicy -Scope Process Bypass
.\Export-SageSpainSalesCsv.ps1

Output folder on Desktop:

Sage_Spain_Sales_Export_YYYYMMDD_HHMMSS

Files created:

- Spain_Sales_2025.csv
- Spain_Sales_2025_summary.txt

The script only reads SQL Server data. It does not change Sage or SQL Server.

Default source:

- Database: Sage
- Header: dbo.CabeceraAlbaranCliente
- Lines: dbo.LineasAlbaranCliente
- Date filter: CabeceraAlbaranCliente.FechaFactura from 2025-01-01 to 2026-01-01
- Sales value: LineasAlbaranCliente.ImporteNeto

If the SQL instance or database name differs:

.\Export-SageSpainSalesCsv.ps1 -ServerInstance "localhost" -Database "Sage"
