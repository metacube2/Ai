-- TRUS INV je Belegart, Artikel leer, Konto
SELECT 'INV' AS T, h."DocType", CASE WHEN COALESCE(p."ItemCode",'') = '' THEN 'ohne Artikel' ELSE 'mit Artikel' END AS ART, p."AcctCode", COUNT(*) AS N, SUM(p."LineTotal") AS LT FROM "{SCHEMA}"."OINV" h INNER JOIN "{SCHEMA}"."INV1" p ON p."DocEntry" = h."DocEntry" WHERE h."DocEntry" IN (287,1056,1213,1583,1755,2416,2702,2818,3111) GROUP BY h."DocType", CASE WHEN COALESCE(p."ItemCode",'') = '' THEN 'ohne Artikel' ELSE 'mit Artikel' END, p."AcctCode" ORDER BY 5 DESC
;;
-- TRUS INV alle Belege ab 2025 je DocType
SELECT 'INV' AS T, h."DocType", COUNT(DISTINCT h."DocEntry") AS BELEGE, COUNT(*) AS ZEILEN FROM "{SCHEMA}"."OINV" h INNER JOIN "{SCHEMA}"."INV1" p ON p."DocEntry" = h."DocEntry" WHERE h."DocDate" >= '2025-01-01' GROUP BY h."DocType"
;;
-- TRUS CRN je Belegart, Artikel leer, Konto
SELECT 'CRN' AS T, h."DocType", CASE WHEN COALESCE(p."ItemCode",'') = '' THEN 'ohne Artikel' ELSE 'mit Artikel' END AS ART, p."AcctCode", COUNT(*) AS N, SUM(p."LineTotal") AS LT FROM "{SCHEMA}"."ORIN" h INNER JOIN "{SCHEMA}"."RIN1" p ON p."DocEntry" = h."DocEntry" WHERE h."DocEntry" IN (118,154,378,379,447,468,610,1055,1056,1057,1058,1059,1060,1061,1062,1063,1064,1065,1066,1067,1068,1069,1070,1072,1073,1074,1075,1076,1077,1078,1079,1080,1081,1082,1099,1129,1143,1191,1196,1197,1198,1199,1200,1234,1274,1306) GROUP BY h."DocType", CASE WHEN COALESCE(p."ItemCode",'') = '' THEN 'ohne Artikel' ELSE 'mit Artikel' END, p."AcctCode" ORDER BY 5 DESC
;;
-- TRUS CRN alle Belege ab 2025 je DocType
SELECT 'CRN' AS T, h."DocType", COUNT(DISTINCT h."DocEntry") AS BELEGE, COUNT(*) AS ZEILEN FROM "{SCHEMA}"."ORIN" h INNER JOIN "{SCHEMA}"."RIN1" p ON p."DocEntry" = h."DocEntry" WHERE h."DocDate" >= '2025-01-01' GROUP BY h."DocType"
