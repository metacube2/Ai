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

        $stats = New-SupplierStatistics -Reader $reader

        while ($reader.Read()) {
            $values = for ($i = 0; $i -lt $reader.FieldCount; $i++) {
                Convert-ToCsvValue $reader.GetValue($i)
            }
            $writer.WriteLine(($values -join ";"))
            $rowCount++

            $rowSales = [decimal]0
            if ($salesIndex -ge 0 -and -not $reader.IsDBNull($salesIndex)) {
                $rowSales = [decimal]$reader.GetValue($salesIndex)
                $salesSum += $rowSales
            }

            Add-SupplierStatisticsRow -Statistics $stats -Reader $reader -RowSales $rowSales
        }
    }
    finally {
        $writer.Dispose()
        $conn.Dispose()
    }

    return [pscustomobject]@{
        Rows = $rowCount
        SalesPriceValueSum = $salesSum
        SupplierStatistics = $stats
    }
}

# Supplier fill rate for the summary file (ISS-003.3). Counts rows with a supplier, per
# ArticleType and per invoice year, and the most frequent suppliers.
function New-SupplierStatistics {
    param($Reader)

    $stats = @{
        SupplierNumberIndex = -1
        SupplierNameIndex = -1
        ArticleTypeIndex = -1
        InvoiceDateIndex = -1
        SupplierRows = 0
        ByArticleType = @{}
        ByInvoiceYear = @{}
        SupplierCounts = @{}
    }

    for ($i = 0; $i -lt $Reader.FieldCount; $i++) {
        switch ($Reader.GetName($i)) {
            "SupplierNumber" { $stats.SupplierNumberIndex = $i }
            "SupplierName" { $stats.SupplierNameIndex = $i }
            "ArticleType" { $stats.ArticleTypeIndex = $i }
            "InvoiceDate" { $stats.InvoiceDateIndex = $i }
        }
    }

    return $stats
}

function Get-ReaderText {
    param($Reader, [int]$Index)

    if ($Index -lt 0 -or $Reader.IsDBNull($Index)) {
        return ""
    }

    return ([string]$Reader.GetValue($Index)).Trim()
}

function Add-SupplierStatisticsRow {
    param($Statistics, $Reader, [decimal]$RowSales)

    $supplierNumber = Get-ReaderText -Reader $Reader -Index $Statistics.SupplierNumberIndex
    $hasSupplier = $supplierNumber.Length -gt 0
    if ($hasSupplier) {
        $Statistics.SupplierRows = $Statistics.SupplierRows + 1
        $supplierKey = $supplierNumber + " " + (Get-ReaderText -Reader $Reader -Index $Statistics.SupplierNameIndex)
        if ($Statistics.SupplierCounts.ContainsKey($supplierKey)) {
            $Statistics.SupplierCounts[$supplierKey] = $Statistics.SupplierCounts[$supplierKey] + 1
        }
        else {
            $Statistics.SupplierCounts[$supplierKey] = 1
        }
    }

    $articleType = Get-ReaderText -Reader $Reader -Index $Statistics.ArticleTypeIndex
    if ($articleType.Length -eq 0) {
        $articleType = "(none)"
    }
    if (-not $Statistics.ByArticleType.ContainsKey($articleType)) {
        $Statistics.ByArticleType[$articleType] = @{ Rows = 0; WithSupplier = 0; Sales = [decimal]0 }
    }
    $articleEntry = $Statistics.ByArticleType[$articleType]
    $articleEntry.Rows = $articleEntry.Rows + 1
    $articleEntry.Sales = $articleEntry.Sales + $RowSales
    if ($hasSupplier) {
        $articleEntry.WithSupplier = $articleEntry.WithSupplier + 1
    }

    $invoiceYear = "(no invoice date)"
    if ($Statistics.InvoiceDateIndex -ge 0 -and -not $Reader.IsDBNull($Statistics.InvoiceDateIndex)) {
        $invoiceValue = $Reader.GetValue($Statistics.InvoiceDateIndex)
        if ($invoiceValue -is [datetime]) {
            $invoiceYear = $invoiceValue.Year.ToString()
        }
    }
    if (-not $Statistics.ByInvoiceYear.ContainsKey($invoiceYear)) {
        $Statistics.ByInvoiceYear[$invoiceYear] = @{ Rows = 0; WithSupplier = 0; Sales = [decimal]0 }
    }
    $yearEntry = $Statistics.ByInvoiceYear[$invoiceYear]
    $yearEntry.Rows = $yearEntry.Rows + 1
    $yearEntry.Sales = $yearEntry.Sales + $RowSales
    if ($hasSupplier) {
        $yearEntry.WithSupplier = $yearEntry.WithSupplier + 1
    }
}

function Format-SupplierSummary {
    param($Statistics, [int]$Rows)

    $percent = 0
    if ($Rows -gt 0) {
        $percent = [math]::Round(100.0 * $Statistics.SupplierRows / $Rows, 1)
    }

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("Supplier (ISS-003.3):")
    $lines.Add("Rows with supplier: $($Statistics.SupplierRows) of $Rows ($percent %)")
    $lines.Add("By ArticleType (rows / with supplier / SalesPriceValue):")
    foreach ($key in ($Statistics.ByArticleType.Keys | Sort-Object)) {
        $entry = $Statistics.ByArticleType[$key]
        $lines.Add("  $key : $($entry.Rows) / $($entry.WithSupplier) / $($entry.Sales)")
    }
    $lines.Add("By invoice year (rows / with supplier / SalesPriceValue):")
    foreach ($key in ($Statistics.ByInvoiceYear.Keys | Sort-Object)) {
        $entry = $Statistics.ByInvoiceYear[$key]
        $lines.Add("  $key : $($entry.Rows) / $($entry.WithSupplier) / $($entry.Sales)")
    }
    $lines.Add("Top suppliers (rows):")
    $top = $Statistics.SupplierCounts.GetEnumerator() |
        Sort-Object -Property @{ Expression = "Value"; Descending = $true }, @{ Expression = "Key"; Descending = $false } |
        Select-Object -First 15
    foreach ($item in $top) {
        $lines.Add("  $($item.Key) : $($item.Value)")
    }
    $lines.Add("Rule: last goods receipt of the same article on or before the delivery date")
    $lines.Add("(dbo.LineasAlbaranProveedor / dbo.CabeceraAlbaranProveedor), only TipoArticulo 'M',")
    $lines.Add("only receipt lines with positive units.")

    return ($lines -join [Environment]::NewLine)
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
    f.FechaAsiento AS PostingDate,
    f.Asiento AS PostingDocument,
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
    END AS DocumentType,
    l.TipoArticulo AS ArticleType,
    wp.CodigoProveedor AS SupplierNumber,
    COALESCE(NULLIF(LTRIM(RTRIM(wp.RazonSocial)), ''), wp.Nombre) AS SupplierName,
    wp.SiglaNacion AS SupplierCountry,
    wp.Nacion AS SupplierCountryName,
    wp.FechaAlbaran AS SupplierReceiptDate,
    wp.ReceiptDocument AS SupplierReceiptDocument
FROM dbo.CabeceraAlbaranCliente c
JOIN dbo.LineasAlbaranCliente l
  ON l.CodigoEmpresa = c.CodigoEmpresa
 AND l.EjercicioAlbaran = c.EjercicioAlbaran
 AND l.SerieAlbaran = c.SerieAlbaran
 AND l.NumeroAlbaran = c.NumeroAlbaran
-- Buchungsdatum (PostingDate). Bis hierher liest der Export nur Lieferscheinkopf und
-- Lieferscheinposition, die kennen kein Buchungsdatum. Es liegt in dbo.FacturasTB als
-- FechaAsiento, dazu die Buchungsnummer Asiento.
--
-- Bewusst OUTER APPLY mit TOP 1 statt eines gewoehnlichen JOIN: im Schemaauszug vom
-- 05.05.2026 verteilen sich 3'788 FacturasTB-Zeilen auf 3'642 Rechnungsschluessel,
-- 70 Schluessel kommen also mehrfach vor, und bei 6 davon steht in den Doppelzeilen ein
-- unterschiedliches FechaAsiento. Ein JOIN wuerde fuer diese Rechnungen jede
-- Verkaufszeile vervielfachen und damit den spanischen Umsatz erhoehen. OUTER APPLY
-- liefert garantiert hoechstens eine Zeile, das ORDER BY macht die Auswahl bei den
-- Mehrfachtreffern reproduzierbar statt zufaellig. OUTER und nicht CROSS, damit Zeilen
-- ohne Buchung erhalten bleiben statt still aus dem Export zu fallen.
--
-- ACHTUNG, der Schluessel ist eine begruendete ANNAHME und noch nicht live geprueft:
-- der Schemaauszug ist bei 80 Objekten je Datenbank abgeschnitten und der echte
-- Rechnungskopf CabeceraFacturaCliente fehlt darin ganz. Vor dem produktiven Einsatz auf
-- dem spanischen Server pruefen, wie oft PostingDate leer bleibt und wie sich
-- Gutschriften verhalten (SerieFactura = 'REC' beziehungsweise StatusAbono <> 0).
-- Hintergrund: docs/FINANCE_ES_BUCHUNGSDATUM_2026-08-03.md, Issue ISS-004.2.
OUTER APPLY (
    SELECT TOP 1 t.FechaAsiento, t.Asiento
    FROM dbo.FacturasTB t
    WHERE t.CodigoEmpresa = c.CodigoEmpresa
      AND t.Ejercicio     = c.EjercicioFactura
      AND t.Serie         = c.SerieFactura
      AND t.Factura       = c.NumeroFactura
    ORDER BY t.FechaAsiento, t.Asiento
) f
-- Lieferant (Supplier), ISS-003.3, Entscheid Andreas Stoller 2026-11-12: es gilt der Lieferant
-- des letzten Wareneingangs desselben Artikels am oder vor dem Lieferdatum der Verkaufsposition.
-- OUTER APPLY mit TOP 1 wie beim Buchungsdatum: hoechstens eine Zeile je Verkaufsposition, keine
-- Vervielfachung des Umsatzes; OUTER, damit Positionen ohne Wareneingang erhalten bleiben.
-- Nur Lagerware (TipoArticulo = 'M') mit echtem Artikelcode; Fracht, Konzepte, Texte und '-'
-- bleiben leer. Nur Zugaenge mit positiver Menge, Ruecksendungen an den Lieferanten zaehlen nicht.
-- Join Kopf/Position ueber CodigoEmpresa/EjercicioAlbaran/SerieAlbaran/NumeroAlbaran: begruendete
-- Annahme aus dem Schemaauszug, geprueft mit Analyze-SpainSupplierLookup.ps1.
-- Hintergrund: docs/PLAN_ES_SUPPLIER_WARENEINGANG_2026-11-12.md im BiDashboard-Repository.
OUTER APPLY (
    SELECT TOP 1
        cp.CodigoProveedor,
        cp.RazonSocial,
        cp.Nombre,
        cp.SiglaNacion,
        cp.Nacion,
        cp.FechaAlbaran,
        CONCAT(cp.EjercicioAlbaran, '/', cp.SerieAlbaran, '/', cp.NumeroAlbaran) AS ReceiptDocument
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
WHERE $datePredicate
ORDER BY
    c.FechaFactura,
    c.SerieFactura,
    c.NumeroFactura,
    l.Orden;
"@

$result = Export-QueryToCsv -Sql $sql -Path $csvPath
$supplierSummary = Format-SupplierSummary -Statistics $result.SupplierStatistics -Rows $result.Rows

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
dbo.CabeceraAlbaranCliente joined with dbo.LineasAlbaranCliente, supplier from dbo.LineasAlbaranProveedor/dbo.CabeceraAlbaranProveedor (OUTER APPLY TOP 1)

Filter:
$datePredicate

$supplierSummary

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
