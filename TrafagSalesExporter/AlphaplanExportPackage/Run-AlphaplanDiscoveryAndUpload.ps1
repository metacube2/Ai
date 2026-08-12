param(
    [string]$ServerInstance = "localhost",
    [string]$Database = "",
    [System.Management.Automation.PSCredential]$SqlCredential,
    [string]$BaseDirectory = "C:\Trafag\AlphaplanExport",
    [int]$MaxCandidatesPerDatabase = 80,
    [switch]$ExportSamples,
    [int]$MaxSampleObjects = 15,
    [int]$SampleRows = 200,
    [switch]$IncludeSystemDatabases,
    [switch]$SkipUpload,
    [string]$RcloneExe = "C:\Tools\rclone.exe",
    [string]$RcloneRemote = "trafag-bi",
    [string]$RcloneTarget = "Import/Finance/Deutschland/AlphaplanRaw"
)

$ErrorActionPreference = "Stop"

function New-Connection {
    param([string]$DbName)

    $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $builder["Data Source"] = $ServerInstance
    $builder["Initial Catalog"] = $DbName
    $builder["TrustServerCertificate"] = $true
    $builder["Connect Timeout"] = 15

    if ($null -ne $SqlCredential) {
        $builder["Integrated Security"] = $false
        $builder["User ID"] = $SqlCredential.UserName
        $builder["Password"] = $SqlCredential.GetNetworkCredential().Password
    }
    else {
        $builder["Integrated Security"] = $true
    }

    return New-Object System.Data.SqlClient.SqlConnection($builder.ConnectionString)
}

function Add-SqlParameter {
    param(
        [System.Data.SqlClient.SqlCommand]$Command,
        [string]$Name,
        $Value
    )

    if ($Value -is [int]) {
        $parameter = $Command.Parameters.Add("@$Name", [System.Data.SqlDbType]::Int)
        $parameter.Value = $Value
        return
    }

    if ($Value -is [datetime]) {
        $parameter = $Command.Parameters.Add("@$Name", [System.Data.SqlDbType]::DateTime)
        $parameter.Value = $Value
        return
    }

    $textParameter = $Command.Parameters.Add("@$Name", [System.Data.SqlDbType]::NVarChar, 4000)
    if ($null -eq $Value) {
        $textParameter.Value = [System.DBNull]::Value
    }
    else {
        $textParameter.Value = [string]$Value
    }
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
        Add-SqlParameter -Command $cmd -Name $key -Value $Parameters[$key]
    }

    $table = New-Object System.Data.DataTable
    try {
        $conn.Open()
        $reader = $cmd.ExecuteReader()
        $table.Load($reader)
        $reader.Dispose()
    }
    finally {
        $cmd.Dispose()
        $conn.Dispose()
    }

    return $table
}

function Convert-DataRowToObject {
    param([System.Data.DataRow]$Row)

    $props = [ordered]@{}
    foreach ($column in $Row.Table.Columns) {
        $value = $Row[$column.ColumnName]
        if ($null -eq $value -or $value -is [System.DBNull]) {
            $props[$column.ColumnName] = ""
        }
        else {
            $props[$column.ColumnName] = $value
        }
    }

    return [pscustomobject]$props
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

        $reader.Dispose()
    }
    finally {
        $writer.Dispose()
        $cmd.Dispose()
        $conn.Dispose()
    }

    return $rowCount
}

function Quote-NamePart {
    param([string]$Name)

    return "[" + $Name.Replace("]", "]]") + "]"
}

function Normalize-FileName {
    param([string]$Value)

    $safe = ($Value -replace '[\\/:*?"<>|]', '_')
    if ($safe.Length -gt 150) {
        return $safe.Substring(0, 150)
    }

    return $safe
}

function Get-UserDatabases {
    $systemFilter = if ($IncludeSystemDatabases) { "" } else { "AND database_id > 4" }
    $sql = @"
SELECT name
FROM sys.databases
WHERE state_desc = 'ONLINE'
  AND HAS_DBACCESS(name) = 1
  $systemFilter
ORDER BY name;
"@

    $table = Invoke-DataTable "master" $sql
    $result = New-Object System.Collections.Generic.List[string]
    foreach ($row in $table.Rows) {
        $result.Add([string]$row["name"])
    }

    return @($result)
}

function Get-CandidateObjects {
    param([string]$DbName)

    $sql = @"
WITH object_columns AS (
    SELECT
        o.object_id AS ObjectId,
        s.name AS SchemaName,
        o.name AS ObjectName,
        o.type_desc AS ObjectType,
        c.name AS ColumnName,
        t.name AS TypeName,
        c.column_id AS ColumnId
    FROM sys.objects o
    JOIN sys.schemas s ON s.schema_id = o.schema_id
    JOIN sys.columns c ON c.object_id = o.object_id
    JOIN sys.types t ON t.user_type_id = c.user_type_id
    WHERE o.type IN ('U', 'V')
      AND o.is_ms_shipped = 0
),
scored AS (
    SELECT
        ObjectId,
        SchemaName,
        ObjectName,
        ObjectType,
        COUNT(*) AS ColumnCount,
        SUM(
            CASE WHEN LOWER(ObjectName) LIKE '%rechnung%' OR LOWER(ObjectName) LIKE '%fakt%' OR LOWER(ObjectName) LIKE '%invoice%' OR LOWER(ObjectName) LIKE '%fact%' OR LOWER(ObjectName) LIKE '%beleg%' THEN 8 ELSE 0 END +
            CASE WHEN LOWER(ObjectName) LIKE '%umsatz%' OR LOWER(ObjectName) LIKE '%verkauf%' OR LOWER(ObjectName) LIKE '%sales%' OR LOWER(ObjectName) LIKE '%revenue%' THEN 6 ELSE 0 END +
            CASE WHEN LOWER(ObjectName) LIKE '%position%' OR LOWER(ObjectName) LIKE '%zeile%' OR LOWER(ObjectName) LIKE '%line%' THEN 4 ELSE 0 END +
            CASE WHEN LOWER(ObjectName) LIKE '%auftrag%' OR LOWER(ObjectName) LIKE '%order%' THEN 4 ELSE 0 END +
            CASE WHEN LOWER(ObjectName) LIKE '%artikel%' OR LOWER(ObjectName) LIKE '%material%' OR LOWER(ObjectName) LIKE '%item%' OR LOWER(ObjectName) LIKE '%produkt%' THEN 5 ELSE 0 END +
            CASE WHEN LOWER(ObjectName) LIKE '%kunde%' OR LOWER(ObjectName) LIKE '%debitor%' OR LOWER(ObjectName) LIKE '%customer%' OR LOWER(ObjectName) LIKE '%adresse%' THEN 4 ELSE 0 END +
            CASE WHEN LOWER(ObjectName) LIKE '%gutschrift%' OR LOWER(ObjectName) LIKE '%storno%' OR LOWER(ObjectName) LIKE '%credit%' THEN 4 ELSE 0 END +
            CASE WHEN LOWER(ColumnName) LIKE '%datum%' OR LOWER(ColumnName) LIKE '%date%' OR LOWER(ColumnName) LIKE '%zeit%' THEN 2 ELSE 0 END +
            CASE WHEN LOWER(ColumnName) LIKE '%rechnung%' OR LOWER(ColumnName) LIKE '%fakt%' OR LOWER(ColumnName) LIKE '%invoice%' OR LOWER(ColumnName) LIKE '%beleg%' THEN 2 ELSE 0 END +
            CASE WHEN LOWER(ColumnName) LIKE '%kunde%' OR LOWER(ColumnName) LIKE '%debitor%' OR LOWER(ColumnName) LIKE '%customer%' OR LOWER(ColumnName) LIKE '%adresse%' THEN 2 ELSE 0 END +
            CASE WHEN LOWER(ColumnName) LIKE '%artikel%' OR LOWER(ColumnName) LIKE '%material%' OR LOWER(ColumnName) LIKE '%item%' OR LOWER(ColumnName) LIKE '%produkt%' OR LOWER(ColumnName) LIKE '%artnr%' THEN 2 ELSE 0 END +
            CASE WHEN LOWER(ColumnName) LIKE '%betrag%' OR LOWER(ColumnName) LIKE '%netto%' OR LOWER(ColumnName) LIKE '%umsatz%' OR LOWER(ColumnName) LIKE '%amount%' OR LOWER(ColumnName) LIKE '%price%' OR LOWER(ColumnName) LIKE '%preis%' OR LOWER(ColumnName) LIKE '%summe%' THEN 3 ELSE 0 END +
            CASE WHEN LOWER(ColumnName) LIKE '%menge%' OR LOWER(ColumnName) LIKE '%anzahl%' OR LOWER(ColumnName) LIKE '%quantity%' OR LOWER(ColumnName) LIKE '%qty%' THEN 2 ELSE 0 END +
            CASE WHEN LOWER(ColumnName) LIKE '%waehr%' OR LOWER(ColumnName) LIKE '%currency%' OR LOWER(ColumnName) LIKE '%whrg%' THEN 1 ELSE 0 END +
            CASE WHEN LOWER(ColumnName) LIKE '%warengruppe%' OR LOWER(ColumnName) LIKE '%produktgruppe%' OR LOWER(ColumnName) LIKE '%productgroup%' THEN 1 ELSE 0 END
        ) AS Score
    FROM object_columns
    GROUP BY ObjectId, SchemaName, ObjectName, ObjectType
)
SELECT TOP (@MaxCandidates)
    DB_NAME() AS DatabaseName,
    s.SchemaName,
    s.ObjectName,
    s.ObjectType,
    s.Score,
    s.ColumnCount,
    ISNULL((
        SELECT SUM(p.row_count)
        FROM sys.dm_db_partition_stats p
        WHERE p.object_id = s.ObjectId
          AND p.index_id IN (0, 1)
    ), 0) AS RowCountEstimate,
    STUFF((
        SELECT ', ' + oc.ColumnName
        FROM object_columns oc
        WHERE oc.ObjectId = s.ObjectId
          AND (
              oc.TypeName IN ('date', 'datetime', 'datetime2', 'smalldatetime')
              OR LOWER(oc.ColumnName) LIKE '%datum%'
              OR LOWER(oc.ColumnName) LIKE '%date%'
              OR LOWER(oc.ColumnName) LIKE '%zeit%'
          )
        ORDER BY oc.ColumnId
        FOR XML PATH(''), TYPE
    ).value('.', 'nvarchar(max)'), 1, 2, '') AS DateColumnCandidates,
    STUFF((
        SELECT ', ' + oc.ColumnName
        FROM object_columns oc
        WHERE oc.ObjectId = s.ObjectId
          AND (
              LOWER(oc.ColumnName) LIKE '%betrag%'
              OR LOWER(oc.ColumnName) LIKE '%netto%'
              OR LOWER(oc.ColumnName) LIKE '%umsatz%'
              OR LOWER(oc.ColumnName) LIKE '%amount%'
              OR LOWER(oc.ColumnName) LIKE '%price%'
              OR LOWER(oc.ColumnName) LIKE '%preis%'
              OR LOWER(oc.ColumnName) LIKE '%summe%'
          )
        ORDER BY oc.ColumnId
        FOR XML PATH(''), TYPE
    ).value('.', 'nvarchar(max)'), 1, 2, '') AS AmountColumnCandidates,
    STUFF((
        SELECT ', ' + oc.ColumnName
        FROM object_columns oc
        WHERE oc.ObjectId = s.ObjectId
          AND (
              LOWER(oc.ColumnName) LIKE '%rechnung%'
              OR LOWER(oc.ColumnName) LIKE '%fakt%'
              OR LOWER(oc.ColumnName) LIKE '%beleg%'
              OR LOWER(oc.ColumnName) LIKE '%kunde%'
              OR LOWER(oc.ColumnName) LIKE '%debitor%'
              OR LOWER(oc.ColumnName) LIKE '%artikel%'
              OR LOWER(oc.ColumnName) LIKE '%material%'
              OR LOWER(oc.ColumnName) LIKE '%position%'
          )
        ORDER BY oc.ColumnId
        FOR XML PATH(''), TYPE
    ).value('.', 'nvarchar(max)'), 1, 2, '') AS KeyColumnCandidates,
    STUFF((
        SELECT ', ' + oc.ColumnName
        FROM object_columns oc
        WHERE oc.ObjectId = s.ObjectId
        ORDER BY oc.ColumnId
        FOR XML PATH(''), TYPE
    ).value('.', 'nvarchar(max)'), 1, 2, '') AS Columns
FROM scored s
WHERE s.Score > 0
ORDER BY s.Score DESC, RowCountEstimate DESC, s.ObjectName;
"@

    $table = Invoke-DataTable $DbName $sql @{ MaxCandidates = $MaxCandidatesPerDatabase }
    $result = New-Object System.Collections.Generic.List[object]
    foreach ($row in $table.Rows) {
        $result.Add((Convert-DataRowToObject $row))
    }

    return @($result)
}

function Build-SampleSql {
    param(
        [string]$SchemaName,
        [string]$ObjectName,
        [int]$Rows
    )

    $safeRows = [Math]::Max(1, $Rows)
    $qualifiedName = "$(Quote-NamePart $SchemaName).$(Quote-NamePart $ObjectName)"
    return "SELECT TOP ($safeRows) * FROM $qualifiedName;"
}

function Resolve-RcloneExecutable {
    param([string]$ConfiguredPath)

    $scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
    $candidates = @(
        $ConfiguredPath,
        (Join-Path $scriptDirectory "rclone.exe"),
        "C:\Tools\rclone.exe",
        "C:\Tools\rclone\rclone.exe",
        "C:\Tools\rclone\rclone\rclone.exe",
        "rclone"
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }

        $command = Get-Command $candidate -ErrorAction SilentlyContinue
        if ($null -ne $command) {
            return $command.Source
        }
    }

    throw "rclone executable not found. Checked: $($candidates -join ', ')"
}

function Throw-RcloneError {
    param(
        [string]$Message,
        [string]$LogPath
    )

    if (Test-Path -LiteralPath $LogPath) {
        Write-Host ""
        Write-Host "Last rclone log lines:"
        Get-Content -LiteralPath $LogPath -Tail 80 | ForEach-Object { Write-Host $_ }
    }

    throw "$Message Log: $LogPath"
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$outputDirectory = Join-Path $BaseDirectory "out"
$logDirectory = Join-Path $BaseDirectory "logs"
New-Item -ItemType Directory -Force -Path $outputDirectory, $logDirectory | Out-Null

$runDirectory = Join-Path $outputDirectory "Alphaplan_SQL_Discovery_$timestamp"
New-Item -ItemType Directory -Path $runDirectory -Force | Out-Null

$summary = New-Object System.Collections.Generic.List[object]
$allCandidates = New-Object System.Collections.Generic.List[object]

Write-Host "Alphaplan SQL discovery"
Write-Host "Server instance: $ServerInstance"
Write-Host "Database filter: $(if ([string]::IsNullOrWhiteSpace($Database)) { '(all accessible user databases)' } else { $Database })"
Write-Host "Run directory: $runDirectory"

$databases = if ([string]::IsNullOrWhiteSpace($Database)) {
    @(Get-UserDatabases)
}
else {
    @($Database)
}

if ($databases.Count -eq 0) {
    throw "No accessible databases found."
}

foreach ($db in $databases) {
    Write-Host "Scanning database: $db"

    try {
        $candidates = @(Get-CandidateObjects $db)
        foreach ($candidate in $candidates) {
            $allCandidates.Add($candidate)
        }

        $summary.Add([pscustomobject]@{
            Created = (Get-Date -Format "yyyy-MM-dd HH:mm:ss")
            Database = $db
            Object = ""
            Action = "Discovery completed"
            Rows = $candidates.Count
            File = "candidate_objects.csv"
            Error = ""
        })
    }
    catch {
        $summary.Add([pscustomobject]@{
            Created = (Get-Date -Format "yyyy-MM-dd HH:mm:ss")
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
    $allCandidates |
        Sort-Object DatabaseName, @{ Expression = "Score"; Descending = $true }, ObjectName |
        Export-Csv -LiteralPath $candidatePath -NoTypeInformation -Encoding UTF8 -Delimiter ";"
}
else {
    "" | Set-Content -LiteralPath $candidatePath -Encoding UTF8
}

if ($ExportSamples -and $allCandidates.Count -gt 0) {
    $sampleObjects = @(
        $allCandidates |
            Sort-Object @{ Expression = "Score"; Descending = $true }, DatabaseName, ObjectName |
            Select-Object -First $MaxSampleObjects
    )

    foreach ($candidate in $sampleObjects) {
        $db = [string]$candidate.DatabaseName
        $schema = [string]$candidate.SchemaName
        $objectName = [string]$candidate.ObjectName
        $fileName = Normalize-FileName "sample_$db.$schema.$objectName.csv"
        $samplePath = Join-Path $runDirectory $fileName

        try {
            Write-Host "Exporting sample: $db.$schema.$objectName"
            $sql = Build-SampleSql -SchemaName $schema -ObjectName $objectName -Rows $SampleRows
            $rows = Export-QueryToCsv -DbName $db -Sql $sql -Path $samplePath

            $summary.Add([pscustomobject]@{
                Created = (Get-Date -Format "yyyy-MM-dd HH:mm:ss")
                Database = $db
                Object = "$schema.$objectName"
                Action = "Sample exported"
                Rows = $rows
                File = $fileName
                Error = ""
            })
        }
        catch {
            $summary.Add([pscustomobject]@{
                Created = (Get-Date -Format "yyyy-MM-dd HH:mm:ss")
                Database = $db
                Object = "$schema.$objectName"
                Action = "Sample export failed"
                Rows = 0
                File = $fileName
                Error = $_.Exception.Message
            })
        }
    }
}

$summaryPath = Join-Path $runDirectory "export_summary.csv"
$summary | Export-Csv -LiteralPath $summaryPath -NoTypeInformation -Encoding UTF8 -Delimiter ";"

$readmePath = Join-Path $runDirectory "README.txt"
@"
Alphaplan SQL discovery export
==============================

Created: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Server instance: $ServerInstance
Database filter: $(if ([string]::IsNullOrWhiteSpace($Database)) { "(all accessible user databases)" } else { $Database })
Export samples: $ExportSamples
Sample rows: $SampleRows

Files:
- candidate_objects.csv: SQL tables/views that look relevant for finance, invoices, sales, customers, articles or amounts.
- export_summary.csv: discovery and optional sample export status.
- sample_*.csv: optional small samples from top candidate objects when -ExportSamples is used.

Important:
- The script only reads SQL Server metadata and data.
- It does not change Alphaplan or SQL Server.
- Sample exports are limited with SELECT TOP.
- This is Phase 1 only. The BiDashboard import mapping is a separate step.

Recommended next step:
Send candidate_objects.csv and export_summary.csv to the Alphaplan/DE key user or IT.
They should identify the correct invoice header, invoice line, customer, article and credit note/storno objects.
"@ | Set-Content -LiteralPath $readmePath -Encoding UTF8

if (-not $SkipUpload) {
    $resolvedRclone = Resolve-RcloneExecutable -ConfiguredPath $RcloneExe
    $target = "${RcloneRemote}:$RcloneTarget"
    $rcloneLog = Join-Path $logDirectory ("rclone-alphaplan-discovery-" + (Get-Date -Format "yyyyMMdd") + ".log")

    Write-Host "Using rclone: $resolvedRclone"
    Write-Host "Checking SharePoint target: $target"
    & $resolvedRclone mkdir $target --log-file $rcloneLog --log-level INFO
    if ($LASTEXITCODE -ne 0) {
        Throw-RcloneError -Message "Could not create/check SharePoint target '$target'. rclone exit code $LASTEXITCODE." -LogPath $rcloneLog
    }

    $targetListing = & $resolvedRclone lsf $target --max-depth 1 --log-file $rcloneLog --log-level INFO
    if ($LASTEXITCODE -ne 0) {
        Throw-RcloneError -Message "SharePoint target '$target' is not reachable. rclone exit code $LASTEXITCODE." -LogPath $rcloneLog
    }

    Write-Host "SharePoint target reachable. Existing items: $(@($targetListing).Count)"
    Write-Host "Uploading discovery folder to SharePoint target: $target"
    & $resolvedRclone copy $runDirectory $target `
        --include "*.csv" `
        --include "*.txt" `
        --log-file $rcloneLog `
        --log-level INFO
    if ($LASTEXITCODE -ne 0) {
        Throw-RcloneError -Message "rclone upload failed with exit code $LASTEXITCODE." -LogPath $rcloneLog
    }

    $uploadedCandidate = & $resolvedRclone lsf $target --files-only --include "candidate_objects.csv" --log-file $rcloneLog --log-level INFO
    if ($LASTEXITCODE -ne 0 -or -not ($uploadedCandidate | Where-Object { $_ -eq "candidate_objects.csv" })) {
        Throw-RcloneError -Message "Upload verification failed. candidate_objects.csv was not listed in '$target'." -LogPath $rcloneLog
    }

    Write-Host "Upload verified."
    Write-Host "rclone log: $rcloneLog"
}
else {
    Write-Host "Upload skipped because -SkipUpload was used."
}

Write-Host ""
Write-Host "Alphaplan discovery finished."
Write-Host "Local export: $runDirectory"
Write-Host "Candidates: $candidatePath"
Write-Host "Summary: $summaryPath"
Write-Host "Candidate count: $($allCandidates.Count)"
Write-Host "Upload target: $(if ($SkipUpload) { '(skipped)' } else { "${RcloneRemote}:$RcloneTarget" })"

