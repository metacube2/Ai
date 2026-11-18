<#
Check the supplier lookup for the Spain sales export (ISS-003.3). Read-only: only SELECT
statements against dbo.CabeceraAlbaranCliente, dbo.LineasAlbaranCliente,
dbo.LineasAlbaranProveedor, dbo.CabeceraAlbaranProveedor and the SQL Server catalog views.
It does not change Sage or SQL Server.

Rule being checked (decision Andreas Stoller, 2026-11-12): the supplier of a sales line is
the supplier of the last goods receipt of the same article on or before the delivery date,
only for stock articles (TipoArticulo 'M'), only receipt lines with positive units.

Background: docs/PLAN_ES_SUPPLIER_WARENEINGANG_2026-11-12.md in the BiDashboard repository,
section 5 (parts A to L).

Sales lines in scope are the two populations of the existing Spain files:
- R1: invoice date from R1From (incl.) to R1To (excl.), like the 2025 base file and the
  January to May 2026 file;
- R2: line registration date from R2From (incl.) to ToDate (excl.), like the daily files.
#>

param(
    [string]$ServerInstance = "localhost",
    [string]$Database = "Sage",
    [datetime]$R1From = "2025-01-01",
    [datetime]$R1To = "2026-06-01",
    [datetime]$R2From = "2026-05-28",
    [datetime]$ToDate = (Get-Date).Date,
    [string]$OutputDirectory = ""
)

$ErrorActionPreference = "Stop"

function New-Connection {
    $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $builder["Data Source"] = $ServerInstance
    $builder["Initial Catalog"] = $Database
    $builder["Integrated Security"] = $true
    $builder["TrustServerCertificate"] = $true
    $builder["Connect Timeout"] = 15
    return New-Object System.Data.SqlClient.SqlConnection($builder.ConnectionString)
}

function Invoke-Query {
    param([System.Data.SqlClient.SqlConnection]$Connection, [string]$Sql)

    $cmd = $Connection.CreateCommand()
    $cmd.CommandText = $Sql
    $cmd.CommandTimeout = 0
    [void]$cmd.Parameters.AddWithValue("@R1From", $R1From.Date)
    [void]$cmd.Parameters.AddWithValue("@R1To", $R1To.Date)
    [void]$cmd.Parameters.AddWithValue("@R2From", $R2From.Date)
    [void]$cmd.Parameters.AddWithValue("@ToDate", $ToDate.Date)
    $reader = $cmd.ExecuteReader()
    $table = New-Object System.Data.DataTable
    $table.Load($reader)
    return ,$table
}

function Write-Section {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$Name,
        [string]$Title,
        [string]$Sql
    )

    Write-Host ""
    Write-Host "=== $Name. $Title ==="
    $watch = [System.Diagnostics.Stopwatch]::StartNew()
    $table = Invoke-Query -Connection $Connection -Sql $Sql
    $watch.Stop()
    $table | Format-Table -AutoSize | Out-String -Width 400 | Write-Host
    Write-Host ("Rows: {0}, seconds: {1:0.0}" -f $table.Rows.Count, $watch.Elapsed.TotalSeconds)

    if (-not [string]::IsNullOrWhiteSpace($OutputDirectory)) {
        $path = Join-Path $OutputDirectory ("SupplierCheck_" + $Name + ".csv")
        $table | Select-Object * -ExcludeProperty RowError, RowState, Table, ItemArray, HasErrors |
            Export-Csv -LiteralPath $path -Delimiter ";" -NoTypeInformation -Encoding UTF8
    }
}

if (-not [string]::IsNullOrWhiteSpace($OutputDirectory)) {
    New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
}

# Sales lines of both populations, with the same credit note sign as the export.
$salesCte = @"
WITH s AS (
    SELECT
        l.CodigoEmpresa,
        l.CodigoArticulo,
        l.TipoArticulo,
        l.LineasPosicion,
        COALESCE(c.FechaAlbaran, c.FechaFactura) AS SaleDate,
        CASE
            WHEN c.TipoNuevaFra = 2 OR c.SerieFactura = 'REC' OR c.StatusAbono <> 0 THEN -ABS(l.ImporteNeto)
            ELSE l.ImporteNeto
        END AS NetValue
    FROM dbo.CabeceraAlbaranCliente c
    JOIN dbo.LineasAlbaranCliente l
      ON l.CodigoEmpresa = c.CodigoEmpresa
     AND l.EjercicioAlbaran = c.EjercicioAlbaran
     AND l.SerieAlbaran = c.SerieAlbaran
     AND l.NumeroAlbaran = c.NumeroAlbaran
    WHERE (c.FechaFactura >= @R1From AND c.FechaFactura < @R1To)
       OR (COALESCE(l.FechaRegistro, c.FechaFactura) >= @R2From
           AND COALESCE(l.FechaRegistro, c.FechaFactura) < @ToDate)
)
"@

# Gate of the export: stock article with a real article code.
$gate = "s.TipoArticulo = 'M' AND NULLIF(LTRIM(RTRIM(s.CodigoArticulo)), '') IS NOT NULL AND s.CodigoArticulo <> '-'"

# Last receipt on or before the sale date (rule V1), without the gate.
$v1Apply = @"
OUTER APPLY (
    SELECT TOP 1 cp.CodigoProveedor, cp.RazonSocial, cp.Nombre, cp.SiglaNacion, cp.Nacion
    FROM dbo.LineasAlbaranProveedor lp
    JOIN dbo.CabeceraAlbaranProveedor cp
      ON cp.CodigoEmpresa    = lp.CodigoEmpresa
     AND cp.EjercicioAlbaran = lp.EjercicioAlbaran
     AND cp.SerieAlbaran     = lp.SerieAlbaran
     AND cp.NumeroAlbaran    = lp.NumeroAlbaran
    WHERE lp.CodigoEmpresa  = s.CodigoEmpresa
      AND lp.CodigoArticulo = s.CodigoArticulo
      AND lp.Unidades > 0
      AND cp.FechaAlbaran < DATEADD(day, 1, CAST(s.SaleDate AS date))
    ORDER BY cp.FechaAlbaran DESC, cp.EjercicioAlbaran DESC, cp.SerieAlbaran DESC,
             cp.NumeroAlbaran DESC, lp.Orden DESC
) v1
"@

# Last receipt at any time (rule V2), for comparison only.
$v2Apply = @"
OUTER APPLY (
    SELECT TOP 1 cp.CodigoProveedor
    FROM dbo.LineasAlbaranProveedor lp
    JOIN dbo.CabeceraAlbaranProveedor cp
      ON cp.CodigoEmpresa    = lp.CodigoEmpresa
     AND cp.EjercicioAlbaran = lp.EjercicioAlbaran
     AND cp.SerieAlbaran     = lp.SerieAlbaran
     AND cp.NumeroAlbaran    = lp.NumeroAlbaran
    WHERE lp.CodigoEmpresa  = s.CodigoEmpresa
      AND lp.CodigoArticulo = s.CodigoArticulo
      AND lp.Unidades > 0
    ORDER BY cp.FechaAlbaran DESC
) v2
"@

$resultCase = @"
CASE
    WHEN NULLIF(LTRIM(RTRIM(s.CodigoArticulo)), '') IS NULL OR s.CodigoArticulo = '-' THEN '0 no article code'
    WHEN v1.CodigoProveedor IS NOT NULL THEN '1 receipt on or before sale'
    WHEN v2.CodigoProveedor IS NOT NULL THEN '2 receipt only after sale'
    WHEN anyr.Found = 1 THEN '3 only non-positive receipt lines'
    ELSE '4 no receipt at all'
END
"@

Write-Host "Connecting to $ServerInstance / $Database ..."
Write-Host ("R1 invoice date {0:yyyy-MM-dd} to {1:yyyy-MM-dd} (excl.), R2 registration date {2:yyyy-MM-dd} to {3:yyyy-MM-dd} (excl.)" -f $R1From, $R1To, $R2From, $ToDate)
$conn = New-Connection
$conn.Open()

try {
    Write-Section -Connection $conn -Name "A" -Title "Data types of the columns used" -Sql @"
SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_NAME IN ('LineasAlbaranProveedor', 'CabeceraAlbaranProveedor', 'LineasAlbaranCliente')
  AND COLUMN_NAME IN ('CodigoEmpresa', 'EjercicioAlbaran', 'SerieAlbaran', 'NumeroAlbaran', 'CodigoArticulo',
                      'CodigoProveedor', 'RazonSocial', 'Nombre', 'SiglaNacion', 'Nacion', 'FechaAlbaran',
                      'Unidades', 'Orden', 'TipoArticulo', 'StatusAbono', 'FechaRegistro', 'LineasPosicion')
ORDER BY TABLE_NAME, COLUMN_NAME
"@

    Write-Section -Connection $conn -Name "B" -Title "Indexes of the purchase tables (one row per key column)" -Sql @"
SELECT t.name AS TableName, i.name AS IndexName, i.type_desc AS IndexType, i.is_unique AS IsUnique,
       i.is_primary_key AS IsPrimaryKey, ic.key_ordinal AS KeyOrdinal, c.name AS ColumnName
FROM sys.indexes i
JOIN sys.tables t ON t.object_id = i.object_id
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 0
JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE t.name IN ('LineasAlbaranProveedor', 'CabeceraAlbaranProveedor')
ORDER BY t.name, i.name, ic.key_ordinal
"@

    Write-Section -Connection $conn -Name "C" -Title "Sizes, header key uniqueness, returns" -Sql @"
SELECT
  (SELECT COUNT(*) FROM dbo.LineasAlbaranProveedor) AS PurchaseLines,
  (SELECT COUNT(*) FROM dbo.CabeceraAlbaranProveedor) AS PurchaseHeaders,
  (SELECT COUNT(*) FROM (SELECT CodigoEmpresa, EjercicioAlbaran, SerieAlbaran, NumeroAlbaran
                         FROM dbo.CabeceraAlbaranProveedor
                         GROUP BY CodigoEmpresa, EjercicioAlbaran, SerieAlbaran, NumeroAlbaran
                         HAVING COUNT(*) > 1) d) AS DuplicateHeaderKeys,
  (SELECT COUNT(*) FROM dbo.LineasAlbaranProveedor lp
   WHERE NOT EXISTS (SELECT 1 FROM dbo.CabeceraAlbaranProveedor cp
                     WHERE cp.CodigoEmpresa = lp.CodigoEmpresa AND cp.EjercicioAlbaran = lp.EjercicioAlbaran
                       AND cp.SerieAlbaran = lp.SerieAlbaran AND cp.NumeroAlbaran = lp.NumeroAlbaran)) AS LinesWithoutHeader,
  (SELECT MIN(FechaAlbaran) FROM dbo.CabeceraAlbaranProveedor) AS FirstReceipt,
  (SELECT MAX(FechaAlbaran) FROM dbo.CabeceraAlbaranProveedor) AS LastReceipt,
  (SELECT COUNT(*) FROM dbo.LineasAlbaranProveedor WHERE Unidades <= 0) AS LinesUnitsNotPositive,
  (SELECT COUNT(*) FROM dbo.CabeceraAlbaranProveedor WHERE StatusAbono <> 0) AS HeadersStatusAbono
"@

    Write-Section -Connection $conn -Name "D" -Title "Sales lines by article type and lookup result (without gate)" -Sql @"
$salesCte
SELECT s.TipoArticulo, $resultCase AS Result, COUNT(*) AS Lines, SUM(s.NetValue) AS NetValue
FROM s
$v1Apply
$v2Apply
OUTER APPLY (
    SELECT TOP 1 1 AS Found
    FROM dbo.LineasAlbaranProveedor lp
    WHERE lp.CodigoEmpresa = s.CodigoEmpresa AND lp.CodigoArticulo = s.CodigoArticulo
) anyr
GROUP BY s.TipoArticulo, $resultCase
ORDER BY 1, 2
"@

    Write-Section -Connection $conn -Name "E" -Title "Top 25 suppliers by sales lines (with gate)" -Sql @"
$salesCte
SELECT TOP 25 v1.CodigoProveedor, MAX(v1.RazonSocial) AS RazonSocial, MAX(v1.Nombre) AS Nombre,
       MAX(v1.SiglaNacion) AS SiglaNacion, MAX(v1.Nacion) AS Nacion, COUNT(*) AS Lines, SUM(s.NetValue) AS NetValue
FROM s
$v1Apply
WHERE $gate AND v1.CodigoProveedor IS NOT NULL
GROUP BY v1.CodigoProveedor
ORDER BY COUNT(*) DESC
"@

    Write-Section -Connection $conn -Name "F" -Title "Suppliers with TRAFAG in the name (all receipts)" -Sql @"
SELECT cp.CodigoProveedor, cp.RazonSocial, cp.Nombre, cp.SiglaNacion, cp.Nacion,
       COUNT(*) AS Receipts, MAX(cp.FechaAlbaran) AS LastReceipt
FROM dbo.CabeceraAlbaranProveedor cp
WHERE cp.RazonSocial LIKE '%TRAFAG%' OR cp.Nombre LIKE '%TRAFAG%'
GROUP BY cp.CodigoProveedor, cp.RazonSocial, cp.Nombre, cp.SiglaNacion, cp.Nacion
ORDER BY COUNT(*) DESC
"@

    Write-Section -Connection $conn -Name "G" -Title "Sold codes outside the gate that also appear in goods receipts" -Sql @"
$salesCte
, codes AS (
    SELECT s.CodigoArticulo, s.TipoArticulo, COUNT(*) AS SalesLines, SUM(s.NetValue) AS NetValue
    FROM s
    WHERE NOT ($gate)
    GROUP BY s.CodigoArticulo, s.TipoArticulo
)
SELECT codes.CodigoArticulo, codes.TipoArticulo, codes.SalesLines, codes.NetValue, pl.PurchaseLines,
       topsup.CodigoProveedor AS MostFrequentSupplier, topsup.RazonSocial
FROM codes
CROSS APPLY (
    SELECT COUNT(*) AS PurchaseLines
    FROM dbo.LineasAlbaranProveedor lp
    WHERE lp.CodigoArticulo = codes.CodigoArticulo
) pl
OUTER APPLY (
    SELECT TOP 1 cp.CodigoProveedor, cp.RazonSocial
    FROM dbo.LineasAlbaranProveedor lp
    JOIN dbo.CabeceraAlbaranProveedor cp
      ON cp.CodigoEmpresa    = lp.CodigoEmpresa
     AND cp.EjercicioAlbaran = lp.EjercicioAlbaran
     AND cp.SerieAlbaran     = lp.SerieAlbaran
     AND cp.NumeroAlbaran    = lp.NumeroAlbaran
    WHERE lp.CodigoArticulo = codes.CodigoArticulo
    GROUP BY cp.CodigoProveedor, cp.RazonSocial
    ORDER BY COUNT(*) DESC
) topsup
WHERE pl.PurchaseLines > 0
ORDER BY codes.SalesLines DESC
"@

    Write-Section -Connection $conn -Name "H1" -Title "Articles with more than one supplier since R1From" -Sql @"
SELECT COUNT(*) AS ArticlesWithSeveralSuppliers
FROM (
    SELECT lp.CodigoEmpresa, lp.CodigoArticulo
    FROM dbo.LineasAlbaranProveedor lp
    JOIN dbo.CabeceraAlbaranProveedor cp
      ON cp.CodigoEmpresa    = lp.CodigoEmpresa
     AND cp.EjercicioAlbaran = lp.EjercicioAlbaran
     AND cp.SerieAlbaran     = lp.SerieAlbaran
     AND cp.NumeroAlbaran    = lp.NumeroAlbaran
    WHERE cp.FechaAlbaran >= @R1From AND lp.Unidades > 0
    GROUP BY lp.CodigoEmpresa, lp.CodigoArticulo
    HAVING COUNT(DISTINCT cp.CodigoProveedor) > 1
) d
"@

    Write-Section -Connection $conn -Name "H2" -Title "20 examples of articles with several suppliers" -Sql @"
SELECT TOP 20 lp.CodigoArticulo, COUNT(DISTINCT cp.CodigoProveedor) AS Suppliers,
       MIN(cp.FechaAlbaran) AS FirstReceipt, MAX(cp.FechaAlbaran) AS LastReceipt
FROM dbo.LineasAlbaranProveedor lp
JOIN dbo.CabeceraAlbaranProveedor cp
  ON cp.CodigoEmpresa    = lp.CodigoEmpresa
 AND cp.EjercicioAlbaran = lp.EjercicioAlbaran
 AND cp.SerieAlbaran     = lp.SerieAlbaran
 AND cp.NumeroAlbaran    = lp.NumeroAlbaran
WHERE cp.FechaAlbaran >= @R1From AND lp.Unidades > 0
GROUP BY lp.CodigoArticulo
HAVING COUNT(DISTINCT cp.CodigoProveedor) > 1
ORDER BY COUNT(DISTINCT cp.CodigoProveedor) DESC, lp.CodigoArticulo
"@

    Write-Section -Connection $conn -Name "I" -Title "Row count with and without the supplier lookup (must be equal)" -Sql @"
$salesCte
SELECT
  (SELECT COUNT(*) FROM s) AS SalesLines,
  (SELECT COUNT(*) FROM s
   $v1Apply) AS SalesLinesWithLookup
"@

    Write-Section -Connection $conn -Name "J" -Title "Run time of the export query (count only), R1 and R2" -Sql @"
SELECT 'R1' AS Run, COUNT(*) AS ExportRows
FROM dbo.CabeceraAlbaranCliente c
JOIN dbo.LineasAlbaranCliente l
  ON l.CodigoEmpresa = c.CodigoEmpresa
 AND l.EjercicioAlbaran = c.EjercicioAlbaran
 AND l.SerieAlbaran = c.SerieAlbaran
 AND l.NumeroAlbaran = c.NumeroAlbaran
OUTER APPLY (
    SELECT TOP 1 cp.CodigoProveedor
    FROM dbo.LineasAlbaranProveedor lp
    JOIN dbo.CabeceraAlbaranProveedor cp
      ON cp.CodigoEmpresa    = lp.CodigoEmpresa
     AND cp.EjercicioAlbaran = lp.EjercicioAlbaran
     AND cp.SerieAlbaran     = lp.SerieAlbaran
     AND cp.NumeroAlbaran    = lp.NumeroAlbaran
    WHERE l.TipoArticulo = 'M'
      AND NULLIF(LTRIM(RTRIM(l.CodigoArticulo)), '') IS NOT NULL
      AND l.CodigoArticulo <> '-'
      AND lp.CodigoEmpresa  = l.CodigoEmpresa
      AND lp.CodigoArticulo = l.CodigoArticulo
      AND lp.Unidades > 0
      AND cp.FechaAlbaran < DATEADD(day, 1, CAST(COALESCE(c.FechaAlbaran, c.FechaFactura) AS date))
    ORDER BY cp.FechaAlbaran DESC, cp.EjercicioAlbaran DESC, cp.SerieAlbaran DESC,
             cp.NumeroAlbaran DESC, lp.Orden DESC
) wp
WHERE c.FechaFactura >= @R1From AND c.FechaFactura < @R1To
UNION ALL
SELECT 'R2' AS Run, COUNT(*) AS ExportRows
FROM dbo.CabeceraAlbaranCliente c
JOIN dbo.LineasAlbaranCliente l
  ON l.CodigoEmpresa = c.CodigoEmpresa
 AND l.EjercicioAlbaran = c.EjercicioAlbaran
 AND l.SerieAlbaran = c.SerieAlbaran
 AND l.NumeroAlbaran = c.NumeroAlbaran
OUTER APPLY (
    SELECT TOP 1 cp.CodigoProveedor
    FROM dbo.LineasAlbaranProveedor lp
    JOIN dbo.CabeceraAlbaranProveedor cp
      ON cp.CodigoEmpresa    = lp.CodigoEmpresa
     AND cp.EjercicioAlbaran = lp.EjercicioAlbaran
     AND cp.SerieAlbaran     = lp.SerieAlbaran
     AND cp.NumeroAlbaran    = lp.NumeroAlbaran
    WHERE l.TipoArticulo = 'M'
      AND NULLIF(LTRIM(RTRIM(l.CodigoArticulo)), '') IS NOT NULL
      AND l.CodigoArticulo <> '-'
      AND lp.CodigoEmpresa  = l.CodigoEmpresa
      AND lp.CodigoArticulo = l.CodigoArticulo
      AND lp.Unidades > 0
      AND cp.FechaAlbaran < DATEADD(day, 1, CAST(COALESCE(c.FechaAlbaran, c.FechaFactura) AS date))
    ORDER BY cp.FechaAlbaran DESC, cp.EjercicioAlbaran DESC, cp.SerieAlbaran DESC,
             cp.NumeroAlbaran DESC, lp.Orden DESC
) wp
WHERE COALESCE(l.FechaRegistro, c.FechaFactura) >= @R2From
  AND COALESCE(l.FechaRegistro, c.FechaFactura) < @ToDate
"@

    Write-Section -Connection $conn -Name "K" -Title "Sales lines outside both populations (count only)" -Sql @"
SELECT 'registered before R2From, invoiced on or after R1To' AS GapType, COUNT(*) AS Lines,
       SUM(CASE WHEN c.TipoNuevaFra = 2 OR c.SerieFactura = 'REC' OR c.StatusAbono <> 0
                THEN -ABS(l.ImporteNeto) ELSE l.ImporteNeto END) AS NetValue
FROM dbo.CabeceraAlbaranCliente c
JOIN dbo.LineasAlbaranCliente l
  ON l.CodigoEmpresa = c.CodigoEmpresa
 AND l.EjercicioAlbaran = c.EjercicioAlbaran
 AND l.SerieAlbaran = c.SerieAlbaran
 AND l.NumeroAlbaran = c.NumeroAlbaran
WHERE COALESCE(l.FechaRegistro, c.FechaFactura) < @R2From
  AND c.FechaFactura >= @R1To AND c.FechaFactura < @ToDate
UNION ALL
SELECT 'registered from R1From to R2From, never invoiced' AS GapType, COUNT(*) AS Lines,
       SUM(l.ImporteNeto) AS NetValue
FROM dbo.CabeceraAlbaranCliente c
JOIN dbo.LineasAlbaranCliente l
  ON l.CodigoEmpresa = c.CodigoEmpresa
 AND l.EjercicioAlbaran = c.EjercicioAlbaran
 AND l.SerieAlbaran = c.SerieAlbaran
 AND l.NumeroAlbaran = c.NumeroAlbaran
WHERE c.FechaFactura IS NULL
  AND l.FechaRegistro >= @R1From AND l.FechaRegistro < @R2From
"@

    Write-Section -Connection $conn -Name "L" -Title "SourceLineId (LineasPosicion) unique and filled" -Sql @"
$salesCte
SELECT COUNT(*) AS SalesLines,
       COUNT(DISTINCT s.LineasPosicion) AS DistinctSourceLineIds,
       SUM(CASE WHEN NULLIF(CAST(s.LineasPosicion AS nvarchar(100)), '') IS NULL THEN 1 ELSE 0 END) AS EmptySourceLineIds
FROM s
"@
}
finally {
    $conn.Dispose()
}

Write-Host ""
Write-Host "Done. Please send the screen output (and the CSV files, if -OutputDirectory was used)."
