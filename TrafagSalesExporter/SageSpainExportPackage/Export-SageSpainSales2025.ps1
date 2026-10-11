$scriptPath = Join-Path $PSScriptRoot "Export-SageSqlCsv.ps1"

& $scriptPath `
    -Database "Sage" `
    -ObjectName @(
        "dbo.CabeceraAlbaranCliente",
        "dbo.LineasAlbaranCliente",
        "dbo.EstadisVenta",
        "dbo.EstadisVentaTallas",
        "dbo.FacturasTB",
        "dbo.MovimientosFacturas",
        "dbo.Vis_RTDV_EfectosFactura"
    ) `
    -FromDate "2025-01-01" `
    -ToDate "2026-01-01"
