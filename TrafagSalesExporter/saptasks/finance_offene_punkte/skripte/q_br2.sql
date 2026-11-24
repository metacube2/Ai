-- OSCN bei Trafag-Geschaeftspartnern je Partner
SELECT s."CardCode", c."CardType", c."CardName", COUNT(*) AS N, SUM(CASE WHEN COALESCE(s."Substitute",'') <> '' THEN 1 ELSE 0 END) AS MIT_NR FROM "{SCHEMA}"."OSCN" s INNER JOIN "{SCHEMA}"."OCRD" c ON c."CardCode" = s."CardCode" WHERE UPPER(c."CardName") LIKE '%TRAFAG%' GROUP BY s."CardCode", c."CardType", c."CardName" ORDER BY 4 DESC
;;
-- PAARE OSCN Trafag-Partner (Artikel|Partner|Katalognummer)
SELECT COUNT(*) AS N, STRING_AGG(s."ItemCode" || '|' || s."CardCode" || '|' || COALESCE(s."Substitute",''), '~') AS PAARE FROM "{SCHEMA}"."OSCN" s INNER JOIN "{SCHEMA}"."OCRD" c ON c."CardCode" = s."CardCode" WHERE UPPER(c."CardName") LIKE '%TRAFAG%' AND COALESCE(s."Substitute",'') <> ''
;;
-- PAARE OITM SuppCatNum bei Standardlieferant Trafag (Artikel|Lieferant|Katalognummer)
SELECT COUNT(*) AS N, STRING_AGG(i."ItemCode" || '|' || i."CardCode" || '|' || i."SuppCatNum", '~') AS PAARE FROM "{SCHEMA}"."OITM" i INNER JOIN "{SCHEMA}"."OCRD" c ON c."CardCode" = i."CardCode" WHERE UPPER(c."CardName") LIKE '%TRAFAG%' AND COALESCE(i."SuppCatNum",'') <> ''
