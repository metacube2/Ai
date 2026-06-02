param(
    [string]$ServerInstance = "localhost",
    [string]$Database = "Sage",
    [ValidateSet("Full", "Range")]
    [string]$ExportMode = "Full",
    [ValidateSet("InvoiceDate", "LineRegistrationDate")]
    [string]$DateFilter = "InvoiceDate",
    [int]$Year = 2025,
    [datetime]$FromDate = "2025-01-01",
    [datetime]$ToDate = "2026-01-01",
    [string]$OutputDirectory = (Join-Path $env:USERPROFILE "Desktop"),
    [string]$OutputFileName = ""
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
        [string]$Sql,
        [string]$Path
    )

    $conn = New-Connection
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $Sql
    $cmd.CommandTimeout = 0

    $fromParameter = $cmd.Parameters.Add("@FromDate", [System.Data.SqlDbType]::Date)
    $fromParameter.Value = $FromDate.Date

    $toParameter = $cmd.Parameters.Add("@ToDate", [System.Data.SqlDbType]::Date)
    $toParameter.Value = $ToDate.Date

    $writer = New-Object System.IO.StreamWriter($Path, $false, [System.Text.Encoding]::UTF8)
    $rowCount = 0
    $salesSum = [decimal]0

    try {
        $conn.Open()
        $reader = $cmd.ExecuteReader()

        $headers = for ($i = 0; $i -lt $reader.FieldCount; $i++) {
            Convert-ToCsvValue $reader.GetName($i)
        }
        $writer.WriteLine(($headers -join ";"))

        $salesIndex = -1
        for ($i = 0; $i -lt $reader.FieldCount; $i++) {
            if ($reader.GetName($i) -eq "SalesPriceValue") {
                $salesIndex = $i
                break
            }
        }

        while ($reader.Read()) {
            $values = for ($i = 0; $i -lt $reader.FieldCount; $i++) {
                Convert-ToCsvValue $reader.GetValue($i)
            }
            $writer.WriteLine(($values -join ";"))
            $rowCount++

            if ($salesIndex -ge 0 -and -not $reader.IsDBNull($salesIndex)) {
                $salesSum += [decimal]$reader.GetValue($salesIndex)
            }
        }
    }
    finally {
        $writer.Dispose()
        $conn.Dispose()
    }

    return [pscustomobject]@{
        Rows = $rowCount
        SalesPriceValueSum = $salesSum
    }
}

if ($ExportMode -eq "Full") {
    $FromDate = [datetime]::new($Year, 1, 1)
    $ToDate = $FromDate.AddYears(1)
}
else {
    if (-not $PSBoundParameters.ContainsKey("ToDate")) {
        throw "Range export requires -ToDate. Example: -ExportMode Range -FromDate '2025-05-01' -ToDate '2025-06-01'"
    }
}

if ($ToDate.Date -le $FromDate.Date) {
    throw "ToDate must be later than FromDate. FromDate=$($FromDate.ToString("yyyy-MM-dd")), ToDate=$($ToDate.ToString("yyyy-MM-dd"))"
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$runDirectory = Join-Path $OutputDirectory "Sage_Spain_Sales_Export_$timestamp"
New-Item -ItemType Directory -Path $runDirectory -Force | Out-Null

if ([string]::IsNullOrWhiteSpace($OutputFileName)) {
    $fromToken = $FromDate.ToString("yyyyMMdd")
    $toToken = $ToDate.Date.AddDays(-1).ToString("yyyyMMdd")
    $kindToken = $ExportMode.ToLowerInvariant()
    $OutputFileName = "Spain_Sales_${kindToken}_${fromToken}_to_${toToken}.csv"
}

$csvPath = Join-Path $runDirectory $OutputFileName
$summaryPath = Join-Path $runDirectory ([System.IO.Path]::GetFileNameWithoutExtension($OutputFileName) + "_summary.txt")

$datePredicate = if ($DateFilter -eq "LineRegistrationDate") {
    "COALESCE(l.FechaRegistro, c.FechaFactura) >= @FromDate
  AND COALESCE(l.FechaRegistro, c.FechaFactura) < @ToDate"
} else {
    "c.FechaFactura >= @FromDate
  AND c.FechaFactura < @ToDate"
}

$sql = @"
SELECT
    'TRES' AS TSC,
    'Spanien' AS Land,
    'Sage' AS SourceSystem,
    c.CodigoEmpresa AS CompanyCode,
    c.EjercicioAlbaran AS DeliveryYear,
    c.SerieAlbaran AS DeliverySeries,
    c.NumeroAlbaran AS DeliveryNumber,
    c.EjercicioFactura AS InvoiceYear,
    c.SerieFactura AS InvoiceSeries,
    c.NumeroFactura AS InvoiceNumber,
    l.Orden AS PositionOnInvoice,
    l.LineasPosicion AS SourceLineId,
    l.CodigoArticulo AS Material,
    l.DescripcionArticulo AS Name,
    l.Descripcion2Articulo AS Description2,
    l.DescripcionLinea AS DescriptionLine,
    l.CodigoFamilia AS ProductGroup,
    l.CodigoSubfamilia AS ProductSubGroup,
    CAST(l.Unidades AS decimal(19, 6)) AS Quantity,
    c.CodigoCliente AS CustomerNumber,
    c.Nombre AS CustomerName,
    c.CodigoNacion AS CustomerCountryCode,
    c.Nacion AS CustomerCountry,
    CAST(l.PrecioCoste AS decimal(19, 6)) AS StandardCost,
    CAST(l.ImporteCoste AS decimal(19, 6)) AS StandardCostValue,
    'EUR' AS StandardCostCurrency,
    CAST(CASE
        WHEN c.TipoNuevaFra = 2 OR c.SerieFactura = 'REC' OR c.StatusAbono <> 0 THEN -ABS(l.ImporteNeto)
        ELSE l.ImporteNeto
    END AS decimal(19, 6)) AS SalesPriceValue,
    'EUR' AS SalesCurrency,
    'EUR' AS DocumentCurrency,
    'EUR' AS CompanyCurrency,
    c.CodigoDivisa AS SageCurrencyCode,
    CAST(CASE
        WHEN c.TipoNuevaFra = 2 OR c.SerieFactura = 'REC' OR c.StatusAbono <> 0 THEN -ABS(c.BaseImponible)
        ELSE c.BaseImponible
    END AS decimal(19, 6)) AS DocumentNetAmount,
    CAST(c.TotalIva AS decimal(19, 6)) AS DocumentVatAmount,
    CAST(c.ImporteFactura AS decimal(19, 6)) AS DocumentGrossAmount,
    c.FechaFactura AS InvoiceDate,
    c.FechaAlbaran AS DeliveryDate,
    l.FechaRegistro AS LineRegistrationDate,
    c.EjercicioPedido AS OrderYear,
    c.SeriePedido AS OrderSeries,
    c.NumeroPedido AS OrderNumber,
    c.SuPedido AS PurchaseOrderNumber,
    c.CodigoExportacion_ AS Incoterms2020,
    c.CondicionExportacion_ AS IncotermsText,
    c.CodigoComisionista AS SalesResponsibleEmployee,
    c.StatusAbono AS CreditStatus,
    c.NoFacturable AS NonBillable,
    c.TipoNuevaFra AS InvoiceType,
    c.StatusFacturado AS BillingStatus,
    CASE
        WHEN c.TipoNuevaFra = 2 OR c.SerieFactura = 'REC' OR c.StatusAbono <> 0 THEN 'Credit Note'
        ELSE 'Invoice'
    END AS DocumentType
FROM dbo.CabeceraAlbaranCliente c
JOIN dbo.LineasAlbaranCliente l
  ON l.CodigoEmpresa = c.CodigoEmpresa
 AND l.EjercicioAlbaran = c.EjercicioAlbaran
 AND l.SerieAlbaran = c.SerieAlbaran
 AND l.NumeroAlbaran = c.NumeroAlbaran
WHERE $datePredicate
ORDER BY
    c.FechaFactura,
    c.SerieFactura,
    c.NumeroFactura,
    l.Orden;
"@

$result = Export-QueryToCsv -Sql $sql -Path $csvPath

@"
Sage Spain Sales CSV export
===========================

Created: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Server instance: $ServerInstance
Database: $Database
Export mode: $ExportMode
Date filter mode: $DateFilter
From date: $($FromDate.ToString("yyyy-MM-dd"))
To date: $($ToDate.ToString("yyyy-MM-dd"))

Output:
$csvPath

Rows:
$($result.Rows)

SalesPriceValue sum:
$($result.SalesPriceValueSum)

Source:
dbo.CabeceraAlbaranCliente joined with dbo.LineasAlbaranCliente

Filter:
$datePredicate

Notes:
- Currency is set to EUR because Sage exports EnEuros_=-1 and CodigoDivisa is empty in the analysed rows.
- SalesPriceValue uses LineasAlbaranCliente.ImporteNeto; credit notes are forced negative.
- DocumentNetAmount uses CabeceraAlbaranCliente.BaseImponible; credit notes are forced negative.
- Credit notes are marked when TipoNuevaFra=2, SerieFactura='REC', or StatusAbono is non-zero.
- Full exports use the complete selected year.
- Range exports use the explicit FromDate/ToDate window.
"@ | Set-Content -LiteralPath $summaryPath -Encoding UTF8

Write-Host "Created:"
Write-Host "  $csvPath"
Write-Host "  $summaryPath"
Write-Host "Rows: $($result.Rows)"
Write-Host "SalesPriceValue sum: $($result.SalesPriceValueSum)"
