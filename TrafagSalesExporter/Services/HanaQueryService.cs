using Sap.Data.Hana;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public class HanaQueryService
{
    public List<SalesRecord> GetSalesRecords(HanaServer server,
        string schema, string tsc, string land, string dateFilter)
    {
        var connectionString = server.BuildConnectionString();
        var result = new List<SalesRecord>();

        using var connection = new HanaConnection(connectionString);
        connection.Open();

        var invoiceQuery = GetInvoiceQuery(schema, tsc, dateFilter);
        var creditNoteQuery = GetCreditNoteQuery(schema, tsc, dateFilter);

        result.AddRange(ReadRecords(connection, invoiceQuery, land));
        result.AddRange(ReadRecords(connection, creditNoteQuery, land));

        foreach (var record in result)
        {
            if (record.Material.Contains('/'))
            {
                var parts = record.Material.Split('/');
                record.Material = parts[^1];
            }
        }

        return result;
    }

    public void TestConnection(HanaServer server)
    {
        var connectionString = server.BuildConnectionString();
        using var connection = new HanaConnection(connectionString);
        connection.Open();
    }

    private static List<SalesRecord> ReadRecords(HanaConnection connection, string query, string land)
    {
        var records = new List<SalesRecord>();

        using var command = new HanaCommand(query, connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            records.Add(new SalesRecord
            {
                ExtractionDate = reader.GetDateTime(reader.GetOrdinal("extraction_date")),
                Tsc = reader.GetString(reader.GetOrdinal("tsc")),
                InvoiceNumber = reader["invoice_number"]?.ToString() ?? string.Empty,
                PositionOnInvoice = Convert.ToInt32(reader["invoice_position"]),
                InvoiceDate = reader.IsDBNull(reader.GetOrdinal("invoice_date")) ? null : reader.GetDateTime(reader.GetOrdinal("invoice_date")),
                Material = reader["material"]?.ToString() ?? string.Empty,
                Name = reader["material_name"]?.ToString() ?? string.Empty,
                ProductGroup = reader["product_group"]?.ToString() ?? string.Empty,
                Quantity = Convert.ToDecimal(reader["quantity"]),
                SupplierNumber = reader["supplier_number"]?.ToString() ?? string.Empty,
                SupplierName = reader["supplier_name"]?.ToString() ?? string.Empty,
                SupplierCountry = reader["supplier_country"]?.ToString() ?? string.Empty,
                CustomerNumber = reader["customer_number"]?.ToString() ?? string.Empty,
                CustomerName = reader["customer_name"]?.ToString() ?? string.Empty,
                CustomerCountry = reader["customer_country"]?.ToString() ?? string.Empty,
                CustomerIndustry = reader["customer_industry"]?.ToString() ?? string.Empty,
                StandardCost = Convert.ToDecimal(reader["standard_cost"]),
                StandardCostCurrency = reader["standard_cost_currency"]?.ToString() ?? string.Empty,
                PurchaseOrderNumber = reader["purchase_order_number"]?.ToString() ?? string.Empty,
                SalesPriceValue = Convert.ToDecimal(reader["sales_value"]),
                SalesCurrency = reader["sales_currency"]?.ToString() ?? string.Empty,
                Incoterms2020 = reader["incoterms_2020"]?.ToString() ?? string.Empty,
                SalesResponsibleEmployee = reader["sales_responsible"]?.ToString() ?? string.Empty,
                OrderDate = reader.IsDBNull(reader.GetOrdinal("order_date")) ? null : reader.GetDateTime(reader.GetOrdinal("order_date")),
                Land = land,
                DocumentType = reader["doc_type"]?.ToString() ?? string.Empty
            });
        }

        return records;
    }

    private static string GetInvoiceQuery(string schema, string tsc, string dateFilter) => $@"
SELECT
    CURRENT_TIMESTAMP AS extraction_date,
    '{tsc}' AS tsc,
    h.""DocNum"" AS invoice_number,
    p.""LineNum"" AS invoice_position,
    h.""DocDate"" AS invoice_date,
    p.""ItemCode"" AS material,
    p.""Dscription"" AS material_name,
    COALESCE(grp.""ItmsGrpNam"", '') AS product_group,
    p.""Quantity"" AS quantity,
    COALESCE(itm.""CardCode"", '') AS supplier_number,
    COALESCE(sup.""CardName"", '') AS supplier_name,
    COALESCE(sup_adr.""Country"", '') AS supplier_country,
    h.""CardCode"" AS customer_number,
    h.""CardName"" AS customer_name,
    COALESCE(cust_adr.""Country"", '') AS customer_country,
    COALESCE(ind.""IndName"", '') AS customer_industry,
    p.""StockPrice"" AS standard_cost,
    COALESCE(p.""Currency"", h.""DocCur"") AS standard_cost_currency,
    CASE WHEN p.""BaseType"" = 22
         THEN CAST(p.""BaseRef"" AS NVARCHAR(20))
         ELSE '' END AS purchase_order_number,
    p.""LineTotal"" AS sales_value,
    COALESCE(p.""Currency"", h.""DocCur"") AS sales_currency,
    '' AS incoterms_2020,
    COALESCE(emp.""SlpName"", '') AS sales_responsible,
    CASE WHEN p.""BaseType"" = 17
         THEN (SELECT o.""DocDate"" FROM {schema}.""ORDR"" o
               WHERE o.""DocEntry"" = p.""BaseEntry"")
         ELSE NULL END AS order_date,
    'INV' AS doc_type
FROM {schema}.""OINV"" h
INNER JOIN {schema}.""INV1"" p ON h.""DocEntry"" = p.""DocEntry""
LEFT JOIN {schema}.""OITM"" itm ON p.""ItemCode"" = itm.""ItemCode""
LEFT JOIN {schema}.""OITB"" grp ON itm.""ItmsGrpCod"" = grp.""ItmsGrpCod""
LEFT JOIN {schema}.""OCRD"" cust ON h.""CardCode"" = cust.""CardCode""
LEFT JOIN {schema}.""CRD1"" cust_adr ON h.""CardCode"" = cust_adr.""CardCode""
    AND cust_adr.""AdresType"" = 'B' AND cust_adr.""Address"" = h.""PayToCode""
LEFT JOIN {schema}.""OOND"" ind ON cust.""IndustryC"" = ind.""IndCode""
LEFT JOIN {schema}.""OCRD"" sup ON itm.""CardCode"" = sup.""CardCode""
    AND sup.""CardType"" = 'S'
LEFT JOIN {schema}.""CRD1"" sup_adr ON itm.""CardCode"" = sup_adr.""CardCode""
    AND sup_adr.""AdresType"" = 'B'
LEFT JOIN {schema}.""OSLP"" emp ON h.""SlpCode"" = emp.""SlpCode""
WHERE h.""CANCELED"" = 'N' AND h.""DocDate"" >= '{dateFilter}'
ORDER BY h.""DocDate"" DESC, h.""DocNum"", p.""LineNum""";

    private static string GetCreditNoteQuery(string schema, string tsc, string dateFilter) => $@"
SELECT
    CURRENT_TIMESTAMP AS extraction_date,
    '{tsc}' AS tsc,
    h.""DocNum"" AS invoice_number,
    p.""LineNum"" AS invoice_position,
    h.""DocDate"" AS invoice_date,
    p.""ItemCode"" AS material,
    p.""Dscription"" AS material_name,
    COALESCE(grp.""ItmsGrpNam"", '') AS product_group,
    p.""Quantity"" * -1 AS quantity,
    COALESCE(itm.""CardCode"", '') AS supplier_number,
    COALESCE(sup.""CardName"", '') AS supplier_name,
    COALESCE(sup_adr.""Country"", '') AS supplier_country,
    h.""CardCode"" AS customer_number,
    h.""CardName"" AS customer_name,
    COALESCE(cust_adr.""Country"", '') AS customer_country,
    COALESCE(ind.""IndName"", '') AS customer_industry,
    p.""StockPrice"" AS standard_cost,
    COALESCE(p.""Currency"", h.""DocCur"") AS standard_cost_currency,
    '' AS purchase_order_number,
    p.""LineTotal"" * -1 AS sales_value,
    COALESCE(p.""Currency"", h.""DocCur"") AS sales_currency,
    '' AS incoterms_2020,
    COALESCE(emp.""SlpName"", '') AS sales_responsible,
    NULL AS order_date,
    'CRN' AS doc_type
FROM {schema}.""ORIN"" h
INNER JOIN {schema}.""RIN1"" p ON h.""DocEntry"" = p.""DocEntry""
LEFT JOIN {schema}.""OITM"" itm ON p.""ItemCode"" = itm.""ItemCode""
LEFT JOIN {schema}.""OITB"" grp ON itm.""ItmsGrpCod"" = grp.""ItmsGrpCod""
LEFT JOIN {schema}.""OCRD"" cust ON h.""CardCode"" = cust.""CardCode""
LEFT JOIN {schema}.""CRD1"" cust_adr ON h.""CardCode"" = cust_adr.""CardCode""
    AND cust_adr.""AdresType"" = 'B' AND cust_adr.""Address"" = h.""PayToCode""
LEFT JOIN {schema}.""OOND"" ind ON cust.""IndustryC"" = ind.""IndCode""
LEFT JOIN {schema}.""OCRD"" sup ON itm.""CardCode"" = sup.""CardCode""
    AND sup.""CardType"" = 'S'
LEFT JOIN {schema}.""CRD1"" sup_adr ON itm.""CardCode"" = sup_adr.""CardCode""
    AND sup_adr.""AdresType"" = 'B'
LEFT JOIN {schema}.""OSLP"" emp ON h.""SlpCode"" = emp.""SlpCode""
WHERE h.""CANCELED"" = 'N' AND h.""DocDate"" >= '{dateFilter}'
ORDER BY h.""DocDate"" DESC, h.""DocNum"", p.""LineNum""";
}
