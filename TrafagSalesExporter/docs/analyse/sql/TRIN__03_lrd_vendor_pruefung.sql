-- Runde 3: Bestimmt LRD allein die liefernde Gesellschaft?
-- Entscheidende Frage vor der Mail an Indien UND vor der Codeaenderung:
--
-- Wenn alle LRD-Artikel mit gepflegtem Vendor auf Trafag AG (V0078) zeigen, dann ist LRD
-- gleichbedeutend mit "Bezug von TR AG" und wir koennen die liefernde Gesellschaft aus dem
-- Feld ableiten - genau wie bei CH/AT aus dem TSC. Dann brauchen die 30 LRD-Artikel OHNE
-- Vendor gar keine Pflege und die Restliste schrumpft auf die 2 CM-Artikel.
--
-- Zeigen dagegen einige LRD-Artikel auf indische Fremdlieferanten, ist LRD nicht eindeutig,
-- der Vendor wird wirklich gebraucht und die 32er-Liste bleibt bestehen.
--
-- Zweite Frage im selben Lauf: die 130 Artikel ohne Sales Type zerfallen in solche MIT und
-- solche OHNE Vendor. Wer schon einen Vendor hat, ist fuer die Gruppenmarge klassifiziert -
-- der fehlende Sales Type aendert daran nichts. Nur die ohne beides sind eine echte Luecke.

-- 1 Sales Type gegen den konkreten Vendor: zeigt LRD immer auf dieselbe Gesellschaft
SELECT
    COALESCE(itm."U_Tasc_ST", 'NULL') AS SALES_TYPE,
    itm."CardCode" AS VENDOR,
    COUNT(DISTINCT itm."ItemCode") AS ARTIKEL,
    COUNT(*) AS RECHNUNGSZEILEN
FROM {schema}."OINV" h
INNER JOIN {schema}."INV1" p ON h."DocEntry" = p."DocEntry"
INNER JOIN {schema}."OITM" itm ON p."ItemCode" = itm."ItemCode"
WHERE h."CANCELED" = 'N' AND h."DocDate" >= '2025-01-01'
  AND COALESCE(itm."CardCode", '') <> ''
GROUP BY COALESCE(itm."U_Tasc_ST", 'NULL'), itm."CardCode"
ORDER BY 1, 3 DESC
;;
-- 2 Namen und Land der beteiligten Lieferanten (ist V0078 wirklich Trafag AG Schweiz)
SELECT DISTINCT
    itm."U_Tasc_ST" AS SALES_TYPE,
    sup."CardCode" AS VENDOR,
    sup."CardName" AS VENDOR_NAME,
    COALESCE(adr."Country", '') AS LAND
FROM {schema}."OITM" itm
INNER JOIN {schema}."OCRD" sup ON itm."CardCode" = sup."CardCode" AND sup."CardType" = 'S'
LEFT JOIN {schema}."CRD1" adr ON sup."CardCode" = adr."CardCode" AND adr."AdresType" = 'B'
WHERE itm."U_Tasc_ST" IN ('LRD', 'CM')
ORDER BY 1, 2
;;
-- 3 Die 2 CM-Artikel ohne Vendor im Detail (echte Fremdfertigung, Lieferant unbekannt)
SELECT
    itm."ItemCode" AS ARTIKEL,
    itm."ItemName" AS BEZEICHNUNG,
    itm."U_Tasc_ST" AS SALES_TYPE,
    COUNT(*) AS RECHNUNGSZEILEN
FROM {schema}."OINV" h
INNER JOIN {schema}."INV1" p ON h."DocEntry" = p."DocEntry"
INNER JOIN {schema}."OITM" itm ON p."ItemCode" = itm."ItemCode"
WHERE h."CANCELED" = 'N' AND h."DocDate" >= '2025-01-01'
  AND itm."U_Tasc_ST" = 'CM'
  AND COALESCE(itm."CardCode", '') = ''
GROUP BY itm."ItemCode", itm."ItemName", itm."U_Tasc_ST"
ORDER BY 4 DESC
;;
-- 4 Ohne Sales Type UND ohne Vendor: die echte Luecke unter den 130
SELECT
    itm."ItemCode" AS ARTIKEL,
    itm."ItemName" AS BEZEICHNUNG,
    COUNT(*) AS RECHNUNGSZEILEN
FROM {schema}."OINV" h
INNER JOIN {schema}."INV1" p ON h."DocEntry" = p."DocEntry"
INNER JOIN {schema}."OITM" itm ON p."ItemCode" = itm."ItemCode"
WHERE h."CANCELED" = 'N' AND h."DocDate" >= '2025-01-01'
  AND (itm."U_Tasc_ST" IS NULL OR itm."U_Tasc_ST" NOT IN ('FFM', 'LRD', 'CM'))
  AND COALESCE(itm."CardCode", '') = ''
GROUP BY itm."ItemCode", itm."ItemName"
ORDER BY 3 DESC
;;
-- 5 Die 10 FFM-Artikel mit Vendor im Detail (Widerspruch oder Zukaufteil)
SELECT
    itm."ItemCode" AS ARTIKEL,
    itm."ItemName" AS BEZEICHNUNG,
    itm."CardCode" AS VENDOR,
    COUNT(*) AS RECHNUNGSZEILEN
FROM {schema}."OINV" h
INNER JOIN {schema}."INV1" p ON h."DocEntry" = p."DocEntry"
INNER JOIN {schema}."OITM" itm ON p."ItemCode" = itm."ItemCode"
WHERE h."CANCELED" = 'N' AND h."DocDate" >= '2025-01-01'
  AND itm."U_Tasc_ST" = 'FFM'
  AND COALESCE(itm."CardCode", '') <> ''
GROUP BY itm."ItemCode", itm."ItemName", itm."CardCode"
ORDER BY 4 DESC
