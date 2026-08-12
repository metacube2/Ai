Sage SQL CSV export
===================

Server instance: localhost
Database filter: Sage
From date: 2025-01-01
To date: 2026-01-01

Files:
- candidate_objects.csv: SQL tables/views that look relevant for sales/invoices.
- export_summary.csv: export status and row counts.
- *.csv: exported samples or selected full exports.

Recommended workflow:
1. Run discovery first:
   .\Export-SageSqlCsv.ps1 -DiscoverOnly
2. Send candidate_objects.csv to Trafag/IT for selection.
3. Export selected objects:
   .\Export-SageSqlCsv.ps1 -Database "DATABASE_NAME" -ObjectName "schema.table_or_view"
4. If the selected object is very large, add:
   -FromDate "2025-01-01" -ToDate "2026-01-01" -MaxRowsPerObject 100000

The script only reads data. It does not change SQL Server or Sage.
