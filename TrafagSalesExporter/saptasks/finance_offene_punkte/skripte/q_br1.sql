-- Geschaeftspartner mit TRAFAG im Namen (Lieferanten)
SELECT "CardCode", "CardType", "CardName" FROM "{SCHEMA}"."OCRD" WHERE UPPER("CardName") LIKE '%TRAFAG%' ORDER BY 1
;;
-- OITM SuppCatNum Fuellgrad gesamt und bei Standardlieferant Trafag
SELECT CASE WHEN UPPER(c."CardName") LIKE '%TRAFAG AG%' THEN 'Std TRAFAG AG' WHEN UPPER(c."CardName") LIKE '%TRAFAG%' THEN 'Std anderes Trafag' WHEN COALESCE(i."CardCode",'') = '' THEN 'Std leer' ELSE 'Std extern' END AS STD, COUNT(*) AS ARTIKEL, SUM(CASE WHEN COALESCE(i."SuppCatNum",'') <> '' THEN 1 ELSE 0 END) AS MIT_SUPPCAT FROM "{SCHEMA}"."OITM" i LEFT JOIN "{SCHEMA}"."OCRD" c ON c."CardCode" = i."CardCode" GROUP BY CASE WHEN UPPER(c."CardName") LIKE '%TRAFAG AG%' THEN 'Std TRAFAG AG' WHEN UPPER(c."CardName") LIKE '%TRAFAG%' THEN 'Std anderes Trafag' WHEN COALESCE(i."CardCode",'') = '' THEN 'Std leer' ELSE 'Std extern' END
;;
-- OSCN Katalognummern je Geschaeftspartner, Anzahl je Partner (Top)
SELECT s."CardCode", c."CardName", COUNT(*) AS N, COUNT(DISTINCT s."ItemCode") AS ART FROM "{SCHEMA}"."OSCN" s LEFT JOIN "{SCHEMA}"."OCRD" c ON c."CardCode" = s."CardCode" GROUP BY s."CardCode", c."CardName" ORDER BY 3 DESC LIMIT 15
;;
-- POR1/PDN1 Lieferantenkatalognummer (SubCatNum) in Bestell- und WE-Zeilen bei Trafag AG ab 2023
SELECT 'POR1' AS T, COUNT(*) AS ZEILEN, SUM(CASE WHEN COALESCE(p."SubCatNum",'') <> '' THEN 1 ELSE 0 END) AS MIT_SUBCAT, COUNT(DISTINCT p."ItemCode") AS ART FROM "{SCHEMA}"."OPOR" h INNER JOIN "{SCHEMA}"."POR1" p ON p."DocEntry" = h."DocEntry" INNER JOIN "{SCHEMA}"."OCRD" c ON c."CardCode" = h."CardCode" WHERE UPPER(c."CardName") LIKE '%TRAFAG AG%' AND h."DocDate" >= '2023-01-01'
;;
-- PDN1 SubCatNum bei Trafag AG ab 2023
SELECT 'PDN1' AS T, COUNT(*) AS ZEILEN, SUM(CASE WHEN COALESCE(p."SubCatNum",'') <> '' THEN 1 ELSE 0 END) AS MIT_SUBCAT, COUNT(DISTINCT p."ItemCode") AS ART FROM "{SCHEMA}"."OPDN" h INNER JOIN "{SCHEMA}"."PDN1" p ON p."DocEntry" = h."DocEntry" INNER JOIN "{SCHEMA}"."OCRD" c ON c."CardCode" = h."CardCode" WHERE UPPER(c."CardName") LIKE '%TRAFAG AG%' AND h."DocDate" >= '2023-01-01'
