-- TRFR INV je Belegart, Artikel leer, Konto
SELECT 'INV' AS T, h."DocType", CASE WHEN COALESCE(p."ItemCode",'') = '' THEN 'ohne Artikel' ELSE 'mit Artikel' END AS ART, p."AcctCode", COUNT(*) AS N, SUM(p."LineTotal") AS LT FROM "{SCHEMA}"."OINV" h INNER JOIN "{SCHEMA}"."INV1" p ON p."DocEntry" = h."DocEntry" WHERE h."DocEntry" IN (6333) GROUP BY h."DocType", CASE WHEN COALESCE(p."ItemCode",'') = '' THEN 'ohne Artikel' ELSE 'mit Artikel' END, p."AcctCode" ORDER BY 5 DESC
;;
-- TRFR INV alle Belege ab 2025 je DocType
SELECT 'INV' AS T, h."DocType", COUNT(DISTINCT h."DocEntry") AS BELEGE, COUNT(*) AS ZEILEN FROM "{SCHEMA}"."OINV" h INNER JOIN "{SCHEMA}"."INV1" p ON p."DocEntry" = h."DocEntry" WHERE h."DocDate" >= '2025-01-01' GROUP BY h."DocType"
;;
-- TRFR CRN je Belegart, Artikel leer, Konto
SELECT 'CRN' AS T, h."DocType", CASE WHEN COALESCE(p."ItemCode",'') = '' THEN 'ohne Artikel' ELSE 'mit Artikel' END AS ART, p."AcctCode", COUNT(*) AS N, SUM(p."LineTotal") AS LT FROM "{SCHEMA}"."ORIN" h INNER JOIN "{SCHEMA}"."RIN1" p ON p."DocEntry" = h."DocEntry" WHERE h."DocEntry" IN (449,450,524,544,574,581,592,624,639,646) GROUP BY h."DocType", CASE WHEN COALESCE(p."ItemCode",'') = '' THEN 'ohne Artikel' ELSE 'mit Artikel' END, p."AcctCode" ORDER BY 5 DESC
;;
-- TRFR CRN alle Belege ab 2025 je DocType
SELECT 'CRN' AS T, h."DocType", COUNT(DISTINCT h."DocEntry") AS BELEGE, COUNT(*) AS ZEILEN FROM "{SCHEMA}"."ORIN" h INNER JOIN "{SCHEMA}"."RIN1" p ON p."DocEntry" = h."DocEntry" WHERE h."DocDate" >= '2025-01-01' GROUP BY h."DocType"
