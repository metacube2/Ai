<#
Diagnose why PostingDate/PostingDocument stay empty in the range export.
Read-only. Does not touch CabeceraAlbaranCliente, LineasAlbaranCliente or
FacturasTB, only SELECTs against them.

Background: docs/FINANCE_ES_BUCHUNGSDATUM_2026-08-03.md section 8, Issue ISS-004.2.
The export joins dbo.FacturasTB via OUTER APPLY on
CodigoEmpresa / Ejercicio / Serie / Factura against the invoice fields of
CabeceraAlbaranCliente (EjercicioFactura / SerieFactura / NumeroFactura).
That key is an assumption, not yet verified against the live database.
On the first real run all 58 exported rows had an empty PostingDate.
This script measures, for the same date window, how many invoice keys from
the delivery note header actually have a matching row in FacturasTB, and
prints sample rows from both sides so a type/format mismatch (padding,
leading zeros, trailing spaces, numeric vs text) becomes visible.
#>

param(
    [string]$ServerInstance = "localhost",
    [string]$Database = "Sage",
    [datetime]$FromDate = (Get-Date).Date.AddDays(-7),
    [datetime]$ToDate = (Get-Date).Date,
    [string]$OutputCsv = ""
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
    param([System.Data.SqlClient.SqlConnection]$Connection, [string]$Sql, [hashtable]$Parameters = @{})

    $cmd = $Connection.CreateCommand()
    $cmd.CommandText = $Sql
    $cmd.CommandTimeout = 0
    foreach ($key in $Parameters.Keys) {
        [void]$cmd.Parameters.AddWithValue($key, $Parameters[$key])
    }
    $reader = $cmd.ExecuteReader()
    $table = New-Object System.Data.DataTable
    $table.Load($reader)
    return $table
}

Write-Host "Verbinde zu $ServerInstance / $Database ..."
$conn = New-Connection
$conn.Open()

try {
    Write-Host ""
    Write-Host "=== 1. Existiert CabeceraFacturaCliente (der echte Rechnungskopf)? ==="
    $tableCheck = Invoke-Query -Connection $conn -Sql @"
SELECT TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME IN ('CabeceraFacturaCliente', 'FacturasTB', 'FacturasSII', 'CabeceraAlbaranCliente', 'LineasAlbaranCliente')
ORDER BY TABLE_NAME
"@
    $tableCheck | Format-Table -AutoSize | Out-String | Write-Host

    Write-Host ""
    Write-Host "=== 2. Spalten von dbo.FacturasTB ==="
    $facturasCols = Invoke-Query -Connection $conn -Sql @"
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'FacturasTB'
ORDER BY ORDINAL_POSITION
"@
    $facturasCols | Format-Table -AutoSize | Out-String | Write-Host

    Write-Host ""
    Write-Host "=== 2b. Spalten von dbo.CabeceraAlbaranCliente (nur GUID-/Id-Spalten) ==="
    $headerCols = Invoke-Query -Connection $conn -Sql @"
SELECT COLUMN_NAME, DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'CabeceraAlbaranCliente'
  AND (DATA_TYPE = 'uniqueidentifier' OR COLUMN_NAME LIKE '%Factura%' OR COLUMN_NAME LIKE '%Id%')
ORDER BY ORDINAL_POSITION
"@
    $headerCols | Format-Table -AutoSize | Out-String | Write-Host

    Write-Host ""
    Write-Host "=== 3. Rechnungsschluessel aus CabeceraAlbaranCliente im Zeitfenster $FromDate .. $ToDate ==="
    $headerKeys = Invoke-Query -Connection $conn -Sql @"
SELECT DISTINCT
    c.CodigoEmpresa,
    c.EjercicioFactura,
    c.SerieFactura,
    c.NumeroFactura,
    c.FechaFactura
FROM dbo.CabeceraAlbaranCliente c
WHERE c.FechaFactura >= @FromDate AND c.FechaFactura < @ToDate
"@ -Parameters @{ "@FromDate" = $FromDate.Date; "@ToDate" = $ToDate.Date }

    Write-Host "Distinkte Rechnungsschluessel im Zeitfenster: $($headerKeys.Rows.Count)"

    Write-Host ""
    Write-Host "=== 4. Treffer gegen FacturasTB mit dem heutigen Join-Schluessel ==="
    $matchStats = Invoke-Query -Connection $conn -Sql @"
SELECT
    COUNT(*) AS KeysTotal,
    SUM(CASE WHEN f.FechaAsiento IS NOT NULL THEN 1 ELSE 0 END) AS KeysMatched,
    SUM(CASE WHEN f.FechaAsiento IS NULL THEN 1 ELSE 0 END) AS KeysUnmatched
FROM (
    SELECT DISTINCT
        c.CodigoEmpresa, c.EjercicioFactura, c.SerieFactura, c.NumeroFactura
    FROM dbo.CabeceraAlbaranCliente c
    WHERE c.FechaFactura >= @FromDate AND c.FechaFactura < @ToDate
) k
OUTER APPLY (
    SELECT TOP 1 t.FechaAsiento
    FROM dbo.FacturasTB t
    WHERE t.CodigoEmpresa = k.CodigoEmpresa
      AND t.Ejercicio     = k.EjercicioFactura
      AND t.Serie          = k.SerieFactura
      AND t.Factura         = k.NumeroFactura
) f
"@ -Parameters @{ "@FromDate" = $FromDate.Date; "@ToDate" = $ToDate.Date }
    $matchStats | Format-Table -AutoSize | Out-String | Write-Host

    Write-Host ""
    Write-Host "=== 5. Beispiel-Rechnungsschluessel aus CabeceraAlbaranCliente (erste 10) ==="
    $headerKeys | Select-Object -First 10 | Format-Table -AutoSize | Out-String | Write-Host

    Write-Host ""
    Write-Host "=== 6. Beispiel-Zeilen aus FacturasTB (erste 10, gleicher Zeitraum ueber FechaAsiento) ==="
    $facturasSample = Invoke-Query -Connection $conn -Sql @"
SELECT TOP 10 CodigoEmpresa, Ejercicio, Serie, Factura, FechaAsiento, Asiento
FROM dbo.FacturasTB
WHERE FechaAsiento >= DATEADD(day, -30, @FromDate) AND FechaAsiento < @ToDate
ORDER BY FechaAsiento DESC
"@ -Parameters @{ "@FromDate" = $FromDate.Date; "@ToDate" = $ToDate.Date }
    $facturasSample | Format-Table -AutoSize | Out-String | Write-Host

    Write-Host ""
    Write-Host "=== 6b. Erste 5 Rechnungsschluessel gegen FacturasTB ueber CifDni/Kundennummer und Datum gesucht ==="
    Write-Host "(unabhaengig vom angenommenen Serie/Factura-Schluessel, reine Naeherung ueber Datum)"
    $dateOnlyMatch = Invoke-Query -Connection $conn -Sql @"
SELECT TOP 10
    c.CodigoEmpresa, c.EjercicioFactura, c.SerieFactura, c.NumeroFactura, c.FechaFactura,
    t.Ejercicio, t.Serie, t.Factura, t.FechaFactura AS FacturasTB_FechaFactura, t.FechaAsiento
FROM dbo.CabeceraAlbaranCliente c
LEFT JOIN dbo.FacturasTB t
  ON t.CodigoEmpresa = c.CodigoEmpresa
 AND t.FechaFactura   = c.FechaFactura
WHERE c.FechaFactura >= @FromDate AND c.FechaFactura < @ToDate
ORDER BY c.FechaFactura
"@ -Parameters @{ "@FromDate" = $FromDate.Date; "@ToDate" = $ToDate.Date }
    $dateOnlyMatch | Select-Object -First 10 | Format-Table -AutoSize | Out-String | Write-Host

    Write-Host ""
    Write-Host "=== 7. TipoMov/TipoIngreso-Aufteilung in FacturasTB, mit Anteil gefuellter Serie ==="
    Write-Host "(zeigt, ob Serie nur bei einem bestimmten Bewegungstyp gefuellt ist)"
    $movTypes = Invoke-Query -Connection $conn -Sql @"
SELECT TOP 15
    TipoMov,
    TipoIngreso,
    COUNT(*) AS Anzahl,
    SUM(CASE WHEN Serie IS NOT NULL AND LTRIM(RTRIM(Serie)) <> '' THEN 1 ELSE 0 END) AS SerieGefuellt,
    MAX(FechaFactura) AS LetzteFechaFactura
FROM dbo.FacturasTB
GROUP BY TipoMov, TipoIngreso
ORDER BY COUNT(*) DESC
"@
    $movTypes | Format-Table -AutoSize | Out-String | Write-Host

    Write-Host ""
    Write-Host "=== 8. Letztes tatsaechlich gebuchtes Rechnungsdatum (Serie gefuellt) ==="
    $lastPosted = Invoke-Query -Connection $conn -Sql @"
SELECT MAX(FechaFactura) AS LetzteGebuchteFechaFactura, MAX(FechaAsiento) AS LetzterBuchungslauf
FROM dbo.FacturasTB
WHERE Serie IS NOT NULL AND LTRIM(RTRIM(Serie)) <> ''
"@
    $lastPosted | Format-Table -AutoSize | Out-String | Write-Host
    $lastPostedRow = $lastPosted | Select-Object -First 1

    if ($null -ne $lastPostedRow.LetzteGebuchteFechaFactura) {
        $olderTo = [datetime]$lastPostedRow.LetzteGebuchteFechaFactura
        $olderFrom = $olderTo.AddDays(-7)

        Write-Host ""
        Write-Host "=== 9. Schluesseltest auf einem BEREITS gebuchten Zeitfenster $olderFrom .. $olderTo ==="
        Write-Host "(testet, ob Ejercicio/Serie/Factura passt, sobald wirklich gebucht wurde)"
        $olderMatch = Invoke-Query -Connection $conn -Sql @"
SELECT
    COUNT(*) AS KeysTotal,
    SUM(CASE WHEN f.FechaAsiento IS NOT NULL THEN 1 ELSE 0 END) AS KeysMatched
FROM (
    SELECT DISTINCT
        c.CodigoEmpresa, c.EjercicioFactura, c.SerieFactura, c.NumeroFactura
    FROM dbo.CabeceraAlbaranCliente c
    WHERE c.FechaFactura >= @OlderFrom AND c.FechaFactura <= @OlderTo
) k
OUTER APPLY (
    SELECT TOP 1 t.FechaAsiento
    FROM dbo.FacturasTB t
    WHERE t.CodigoEmpresa = k.CodigoEmpresa
      AND t.Ejercicio     = k.EjercicioFactura
      AND t.Serie          = k.SerieFactura
      AND t.Factura         = k.NumeroFactura
) f
"@ -Parameters @{ "@OlderFrom" = $olderFrom; "@OlderTo" = $olderTo }
        $olderMatch | Format-Table -AutoSize | Out-String | Write-Host
    }
    else {
        Write-Host "Keine Zeile mit gefuellter Serie in FacturasTB gefunden."
    }

    if ($OutputCsv -ne "") {
        $headerKeys | Export-Csv -Path $OutputCsv -Delimiter ";" -NoTypeInformation -Encoding UTF8
        Write-Host ""
        Write-Host "Rechnungsschluessel-Liste geschrieben nach: $OutputCsv"
    }

    Write-Host ""
    Write-Host "=== Fazit ==="
    $summaryRow = $matchStats | Select-Object -First 1
    $total = [int]$summaryRow.KeysTotal
    $matched = [int]$summaryRow.KeysMatched
    if ($total -eq 0) {
        Write-Host "Keine Rechnungsschluessel im Zeitfenster gefunden. FromDate/ToDate pruefen."
    }
    elseif ($matched -eq 0) {
        Write-Host "0 von $total Schluesseln treffen in FacturasTB. Vergleiche Abschnitt 5 und 6 von Hand:"
        Write-Host "typische Ursachen sind ein anderer Spaltentyp (Zahl vs. Text), fuehrende Nullen oder"
        Write-Host "Leerzeichen in Serie, oder ein falsches Feld fuer Ejercicio/EjercicioFactura."
    }
    else {
        Write-Host "$matched von $total Schluesseln treffen ($([math]::Round(100.0 * $matched / $total, 1)) %)."
    }
}
finally {
    $conn.Dispose()
}
