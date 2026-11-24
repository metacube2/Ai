codes = [c for c in open('italien162_codes.txt').read().split('\n') if c]
lst = ','.join("'" + c.replace("'", "''") + "'" for c in codes)
q = f"""-- TRIT 162 Artikel: Rechnungs- und Gutschriftzeilen ab 2025 mit/ohne Wareneingang am oder vor Belegdatum, Lieferant des letzten WE
SELECT x."WE", x."VC", COUNT(*) AS ZEILEN, SUM(x."LT") AS EUR FROM (
 SELECT p."ItemCode", p."LineTotal" AS "LT",
  CASE WHEN w."vc" IS NULL THEN 'ohne WE' ELSE 'mit WE' END AS "WE", COALESCE(w."vc", '-') AS "VC"
 FROM "{{SCHEMA}}"."OINV" h INNER JOIN "{{SCHEMA}}"."INV1" p ON p."DocEntry" = h."DocEntry"
 LEFT JOIN (
  SELECT y."de", y."ln", y."vc" FROM (
   SELECT p2."DocEntry" AS "de", p2."LineNum" AS "ln", o2."CardCode" AS "vc",
     ROW_NUMBER() OVER (PARTITION BY p2."DocEntry", p2."LineNum" ORDER BY o2."DocDate" DESC, o2."DocEntry" DESC) AS "rn"
   FROM "{{SCHEMA}}"."OINV" h2 INNER JOIN "{{SCHEMA}}"."INV1" p2 ON p2."DocEntry" = h2."DocEntry"
   INNER JOIN "{{SCHEMA}}"."PDN1" g2 ON g2."ItemCode" = p2."ItemCode" AND g2."Quantity" > 0
   INNER JOIN "{{SCHEMA}}"."OPDN" o2 ON o2."DocEntry" = g2."DocEntry" AND o2."CANCELED" = 'N' AND o2."DocDate" <= h2."DocDate"
   WHERE h2."CANCELED" = 'N' AND h2."DocDate" >= '2025-01-01' AND UPPER(p2."ItemCode") IN ({lst})
  ) y WHERE y."rn" = 1
 ) w ON w."de" = p."DocEntry" AND w."ln" = p."LineNum"
 WHERE h."CANCELED" = 'N' AND h."DocDate" >= '2025-01-01' AND UPPER(p."ItemCode") IN ({lst})
) x GROUP BY x."WE", x."VC" ORDER BY 3 DESC"""
open('q_it162b.sql', 'w', encoding='utf-8').write(q + '\n')
