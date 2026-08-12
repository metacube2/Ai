param(
    [string]$ServerInstance = "localhost",
    [string]$Database = "",
    [string[]]$ObjectName = @(),
    [datetime]$FromDate = "2025-01-01",
    [datetime]$ToDate = "2026-01-01",
    [string]$OutputDirectory = (Join-Path $env:USERPROFILE "Desktop"),
    [int]$SampleRows = 500,
    [int]$MaxRowsPerObject = 0,
    [switch]$DiscoverOnly,
    [switch]$ExportCandidates,
    [switch]$IncludeSystemDatabases
)

$ErrorActionPreference = "Stop"

function New-Connection {
    param([string]$DbName)

    $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $builder["Data Source"] = $ServerInstance
    $builder["Initial Catalog"] = $DbName
    $builder["Integrated Security"] = $true
    $builder["TrustServerCertificate"] = $true
    $builder["Connect Timeout"] = 15
    return New-Object System.Data.SqlClient.SqlConnection($builder.ConnectionString)
}

function Invoke-DataTable {
    param(
        [string]$DbName,
        [string]$Sql,
        [hashtable]$Parameters = @{}
    )

    $conn = New-Connection $DbName
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $Sql
    $cmd.CommandTimeout = 300

    foreach ($key in $Parameters.Keys) {
        $param = $cmd.Parameters.Add("@$key", [System.Data.SqlDbType]::NVarChar, 4000)
        $param.Value = [string]$Parameters[$key]
    }

    $table = New-Object System.Data.DataTable
    try {
        $conn.Open()
        $reader = $cmd.ExecuteReader()
        $table.Load($reader)
    }
    finally {
        $conn.Dispose()
    }

    return $table
}

function Convert-ToCsvValue {
    param($Value)

    if ($null -eq $Value -or $Value -is [System.DBNull]) {
        return ""
    }

    if ($Value -is [datetime]) {
        $text = $Value.ToString("yyyy-MM-dd HH:mm:ss")
    }
    else {
        $text = [string]$Value
    }

    $text = $text.Replace('"', '""')
    return '"' + $text + '"'
}

function Export-QueryToCsv {
    param(
        [string]$DbName,
        [string]$Sql,
        [string]$Path
    )

    $conn = New-Connection $DbName
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $Sql
    $cmd.CommandTimeout = 0

    $writer = New-Object System.IO.StreamWriter($Path, $false, [System.Text.Encoding]::UTF8)
    $rowCount = 0

    try {
        $conn.Open()
        $reader = $cmd.ExecuteReader()

        $headers = for ($i = 0; $i -lt $reader.FieldCount; $i++) {
            Convert-ToCsvValue $reader.GetName($i)
        }
        $writer.WriteLine(($headers -join ";"))

        while ($reader.Read()) {
            $values = for ($i = 0; $i -lt $reader.FieldCount; $i++) {
                Convert-ToCsvValue $reader.GetValue($i)
            }
            $writer.WriteLine(($values -join ";"))
            $rowCount++
        }
    }
    finally {
        $writer.Dispose()
        $conn.Dispose()
    }

    return $rowCount
}

function Quote-NamePart {
    param([string]$Name)

    return "[" + $Name.Replace("]", "]]") + "]"
}

function Split-SqlObjectName {
    param([string]$Name)

    $parts = $Name.Split(".", 2)
    if ($parts.Count -eq 1) {
        return [pscustomobject]@{ SchemaName = "dbo"; ObjectName = $parts[0] }
    }

    return [pscustomobject]@{ SchemaName = $parts[0].Trim("[", "]"); ObjectName = $parts[1].Trim("[", "]") }
}

function Get-UserDatabases {
    $sql = @"
SELECT name
FROM sys.databases
WHERE state_desc = 'ONLINE'
  AND HAS_DBACCESS(name) = 1
  $(if ($IncludeSystemDatabases) { "" } else { "AND database_id > 4" })
ORDER BY name;
"@

    Invoke-DataTable "master" $sql | ForEach-Object { $_.name }
}

function Get-CandidateObjects {
    param([string]$DbName)

    $sql = @"
WITH object_columns AS (
    SELECT
        s.name AS SchemaName,
        o.name AS ObjectName,
        o.type_desc AS ObjectType,
        c.name AS ColumnName,
        t.name AS TypeName,
        c.max_length,
        c.precision,
        c.scale
    FROM sys.objects o
    JOIN sys.schemas s ON s.schema_id = o.schema_id
    JOIN sys.columns c ON c.object_id = o.object_id
    JOIN sys.types t ON t.user_type_id = c.user_type_id
    WHERE o.type IN ('U', 'V')
      AND o.is_ms_shipped = 0
),
scored AS (
    SELECT
        SchemaName,
        ObjectName,
        ObjectType,
        SUM(CASE WHEN LOWER(ObjectName) LIKE '%fact%' OR LOWER(ObjectName) LIKE '%invoice%' OR LOWER(ObjectName) LIKE '%venta%' OR LOWER(ObjectName) LIKE '%sales%' OR LOWER(ObjectName) LIKE '%albar%' OR LOWER(ObjectName) LIKE '%pedido%' THEN 5 ELSE 0 END) +
        SUM(CASE WHEN LOWER(ColumnName) LIKE '%fecha%' OR LOWER(ColumnName) LIKE '%date%' THEN 2 ELSE 0 END) +
        SUM(CASE WHEN LOWER(ColumnName) LIKE '%cliente%' OR LOWER(ColumnName) LIKE '%customer%' THEN 2 ELSE 0 END) +
        SUM(CASE WHEN LOWER(ColumnName) LIKE '%articulo%' OR LOWER(ColumnName) LIKE '%item%' OR LOWER(ColumnName) LIKE '%producto%' THEN 2 ELSE 0 END) +
        SUM(CASE WHEN LOWER(ColumnName) LIKE '%importe%' OR LOWER(ColumnName) LIKE '%neto%' OR LOWER(ColumnName) LIKE '%total%' OR LOWER(ColumnName) LIKE '%amount%' THEN 3 ELSE 0 END) +
        SUM(CASE WHEN LOWER(ColumnName) LIKE '%cantidad%' OR LOWER(ColumnName) LIKE '%quantity%' OR LOWER(ColumnName) LIKE '%unidades%' THEN 2 ELSE 0 END) AS Score,
        COUNT(*) AS ColumnCount,
        STRING_AGG(CONVERT(nvarchar(max), ColumnName), ', ') WITHIN GROUP (ORDER BY ColumnName) AS Columns
    FROM object_columns
    GROUP BY SchemaName, ObjectName, ObjectType
)
SELECT TOP (80)
    DB_NAME() AS DatabaseName,
    SchemaName,
    ObjectName,
    ObjectType,
    Score,
    ColumnCount,
    Columns
FROM scored
WHERE Score > 0
ORDER BY Score DESC, ObjectName;
"@

    Invoke-DataTable $DbName $sql
}

function Get-DateColumns {
    param(
        [string]$DbName,
        [string]$SchemaName,
        [string]$ObjectNameValue
    )

    $sql = @"
SELECT c.name AS ColumnName
FROM sys.objects o
JOIN sys.schemas s ON s.schema_id = o.schema_id
JOIN sys.columns c ON c.object_id = o.object_id
JOIN sys.types t ON t.user_type_id = c.user_type_id
WHERE s.name = @schema
  AND o.name = @object
  AND (
    t.name IN ('date', 'datetime', 'datetime2', 'smalldatetime')
    OR LOWER(c.name) LIKE '%fecha%'
    OR LOWER(c.name) LIKE '%date%'
  )
ORDER BY
  CASE
    WHEN LOWER(c.name) LIKE '%fact%' OR LOWER(c.name) LIKE '%invoice%' THEN 0
    WHEN LOWER(c.name) LIKE '%fecha%' OR LOWER(c.name) LIKE '%date%' THEN 1
    ELSE 2
  END,
  c.column_id;
"@

    Invoke-DataTable $DbName $sql @{ schema = $SchemaName; object = $ObjectNameValue } |
        ForEach-Object { $_.ColumnName }
}

function Build-SelectSql {
    param(
        [string]$SchemaName,
        [string]$ObjectNameValue,
        [string]$DateColumn,
        [int]$TopRows
    )

    $topClause = if ($TopRows -gt 0) { "TOP ($TopRows)" } else { "" }
    $qualified = "$(Quote-NamePart $SchemaName).$(Quote-NamePart $ObjectNameValue)"

    if ([string]::IsNullOrWhiteSpace($DateColumn)) {
        return "SELECT $topClause * FROM $qualified;"
    }

    $from = $FromDate.ToString("yyyy-MM-dd")
    $to = $ToDate.ToString("yyyy-MM-dd")
    $dateColumnSql = Quote-NamePart $DateColumn

    return @"
SELECT $topClause *
FROM $qualified
WHERE TRY_CONVERT(date, $dateColumnSql) >= CONVERT(date, '$from')
  AND TRY_CONVERT(date, $dateColumnSql) < CONVERT(date, '$to')
ORDER BY TRY_CONVERT(date, $dateColumnSql);
"@
}

function Normalize-FileName {
    param([string]$Value)

    return ($Value -replace '[\\/:*?"<>|]', '_')
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$runDirectory = Join-Path $OutputDirectory "Sage_SQL_CSV_Export_$timestamp"
New-Item -ItemType Directory -Path $runDirectory -Force | Out-Null

$databases = if ([string]::IsNullOrWhiteSpace($Database)) {
    @(Get-UserDatabases)
}
else {
    @($Database)
}

$summary = New-Object System.Collections.Generic.List[object]
$allCandidates = New-Object System.Collections.Generic.List[object]

foreach ($db in $databases) {
    Write-Host "Scanning database: $db"
    try {
        $candidates = @(Get-CandidateObjects $db)
        foreach ($candidate in $candidates) {
            $allCandidates.Add($candidate)
        }
    }
    catch {
        $summary.Add([pscustomobject]@{
            Database = $db
            Object = ""
            Action = "Discovery failed"
            Rows = 0
            File = ""
            Error = $_.Exception.Message
        })
    }
}

$candidatePath = Join-Path $runDirectory "candidate_objects.csv"
if ($allCandidates.Count -gt 0) {
    $allCandidates | Export-Csv -LiteralPath $candidatePath -NoTypeInformation -Encoding UTF8 -Delimiter ";"
}

if (-not $DiscoverOnly) {
    $objectsToExport = New-Object System.Collections.Generic.List[object]

    foreach ($name in $ObjectName) {
        if ([string]::IsNullOrWhiteSpace($name)) {
            continue
        }

        if ([string]::IsNullOrWhiteSpace($Database)) {
            throw "When -ObjectName is used, pass -Database as well."
        }

        $parsed = Split-SqlObjectName $name
        $objectsToExport.Add([pscustomobject]@{
            DatabaseName = $Database
            SchemaName = $parsed.SchemaName
            ObjectName = $parsed.ObjectName
        })
    }

    if ($ExportCandidates) {
        foreach ($candidate in ($allCandidates | Sort-Object DatabaseName, @{Expression="Score"; Descending=$true} | Select-Object -First 25)) {
            $objectsToExport.Add([pscustomobject]@{
                DatabaseName = $candidate.DatabaseName
                SchemaName = $candidate.SchemaName
                ObjectName = $candidate.ObjectName
            })
        }
    }

    foreach ($object in $objectsToExport) {
        $db = $object.DatabaseName
        $schema = $object.SchemaName
        $objectNameValue = $object.ObjectName

        try {
            $dateColumn = @(Get-DateColumns $db $schema $objectNameValue | Select-Object -First 1)[0]
            $limit = if ($MaxRowsPerObject -gt 0) { $MaxRowsPerObject } elseif ($ObjectName.Count -gt 0) { 0 } else { $SampleRows }
            $sql = Build-SelectSql $schema $objectNameValue $dateColumn $limit
            $fileName = Normalize-FileName "$db.$schema.$objectNameValue.csv"
            $path = Join-Path $runDirectory $fileName
            Write-Host "Exporting $db.$schema.$objectNameValue -> $path"
            $rows = Export-QueryToCsv $db $sql $path

            $summary.Add([pscustomobject]@{
                Database = $db
                Object = "$schema.$objectNameValue"
                Action = "Exported"
                Rows = $rows
                File = $path
                DateColumn = $dateColumn
                Error = ""
            })
        }
        catch {
            $summary.Add([pscustomobject]@{
                Database = $db
                Object = "$schema.$objectNameValue"
                Action = "Export failed"
                Rows = 0
                File = ""
                DateColumn = ""
                Error = $_.Exception.Message
            })
        }
    }
}

$summaryPath = Join-Path $runDirectory "export_summary.csv"
$summary | Export-Csv -LiteralPath $summaryPath -NoTypeInformation -Encoding UTF8 -Delimiter ";"

$readmePath = Join-Path $runDirectory "README.txt"
@"
Sage SQL CSV export
===================

Server instance: $ServerInstance
Database filter: $(if ($Database) { $Database } else { "(all accessible user databases)" })
From date: $($FromDate.ToString("yyyy-MM-dd"))
To date: $($ToDate.ToString("yyyy-MM-dd"))

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
"@ | Set-Content -LiteralPath $readmePath -Encoding UTF8

Write-Host ""
Write-Host "Created folder:"
Write-Host "  $runDirectory"
Write-Host ""
Write-Host "Main files:"
Write-Host "  $candidatePath"
Write-Host "  $summaryPath"
