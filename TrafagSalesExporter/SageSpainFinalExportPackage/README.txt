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

If the SQL instance or database name differs:

.\Export-SageSpainSalesCsv.ps1 -ServerInstance "localhost" -Database "Sage" -ExportMode Full -Year 2025
