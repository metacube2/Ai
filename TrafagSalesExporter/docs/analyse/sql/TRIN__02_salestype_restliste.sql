-- Runde 2: Verteilung des Sales Type und die Restliste fuer RanVijay
-- Feld ermittelt in Runde 1: OITM."U_Tasc_ST" (UDF Tasc_ST, Beschriftung "Sales Type").
-- Zulaessige Werte laut UFD1: FFM = Full Fledged Manufacturing, LRD = Limited Risk
-- Distributor, CM = Contract Manufacturing, sowie ein Platzhalterwert fuer ungepflegt.
--
-- Grundgesamtheit ist bewusst NICHT der gesamte Artikelstamm, sondern die Artikel MIT
-- Rechnungszeilen ab 2025 - das ist die Menge, die im Dashboard ueberhaupt erscheint.
-- Der Zeitraum entspricht dem Exportumfang.
--
-- Hinweis zum Guardrail: der Platzhalterwert besteht aus zwei Bindestrichen und darf hier
-- nicht als Literal stehen, weil zwei Bindestriche als SQL-Kommentar gelten. Ungepflegte
-- Artikel werden deshalb ueber NOT IN ('FFM','LRD','CM') erfasst.

-- 1 Verteilung Sales Type nach Pflegezustand des Preferred Vendor
SELECT
    COALESCE(itm."U_Tasc_ST", 'NULL') AS SALES_TYPE,
    CASE WHEN COALESCE(itm."CardCode", '') = '' THEN 'ohne Vendor' ELSE 'mit Vendor' END AS VENDOR,
    COUNT(DISTINCT itm."ItemCode") AS ARTIKEL,
    COUNT(*) AS RECHNUNGSZEILEN
FROM {schema}."OINV" h
INNER JOIN {schema}."INV1" p ON h."DocEntry" = p."DocEntry"
INNER JOIN {schema}."OITM" itm ON p."ItemCode" = itm."ItemCode"
WHERE h."CANCELED" = 'N' AND h."DocDate" >= '2025-01-01'
GROUP BY COALESCE(itm."U_Tasc_ST", 'NULL'),
         CASE WHEN COALESCE(itm."CardCode", '') = '' THEN 'ohne Vendor' ELSE 'mit Vendor' END
ORDER BY 3 DESC
;;
-- 2 RESTLISTE: Artikel, die einen Preferred Vendor brauchen und keinen haben
SELECT
    itm."ItemCode" AS ARTIKEL,
    itm."ItemName" AS BEZEICHNUNG,
    itm."U_Tasc_ST" AS SALES_TYPE,
    COUNT(*) AS RECHNUNGSZEILEN,
    SUM(p."Quantity") AS MENGE
FROM {schema}."OINV" h
INNER JOIN {schema}."INV1" p ON h."DocEntry" = p."DocEntry"
INNER JOIN {schema}."OITM" itm ON p."ItemCode" = itm."ItemCode"
WHERE h."CANCELED" = 'N' AND h."DocDate" >= '2025-01-01'
  AND COALESCE(itm."CardCode", '') = ''
  AND itm."U_Tasc_ST" IN ('LRD', 'CM')
GROUP BY itm."ItemCode", itm."ItemName", itm."U_Tasc_ST"
ORDER BY 4 DESC
;;
-- 3 Ungepflegter Sales Type: Anzahl (diese Artikel sind fachlich noch gar nicht eingeordnet)
SELECT
    COUNT(DISTINCT itm."ItemCode") AS ARTIKEL_OHNE_SALES_TYPE,
    COUNT(*) AS RECHNUNGSZEILEN
FROM {schema}."OINV" h
INNER JOIN {schema}."INV1" p ON h."DocEntry" = p."DocEntry"
INNER JOIN {schema}."OITM" itm ON p."ItemCode" = itm."ItemCode"
WHERE h."CANCELED" = 'N' AND h."DocDate" >= '2025-01-01'
  AND (itm."U_Tasc_ST" IS NULL OR itm."U_Tasc_ST" NOT IN ('FFM', 'LRD', 'CM'))
;;
-- 4 Ungepflegter Sales Type: die betroffenen Artikel
SELECT
    itm."ItemCode" AS ARTIKEL,
    itm."ItemName" AS BEZEICHNUNG,
    COALESCE(itm."U_Tasc_ST", 'NULL') AS SALES_TYPE,
    COALESCE(itm."CardCode", '') AS VENDOR,
    COUNT(*) AS RECHNUNGSZEILEN
FROM {schema}."OINV" h
INNER JOIN {schema}."INV1" p ON h."DocEntry" = p."DocEntry"
INNER JOIN {schema}."OITM" itm ON p."ItemCode" = itm."ItemCode"
WHERE h."CANCELED" = 'N' AND h."DocDate" >= '2025-01-01'
  AND (itm."U_Tasc_ST" IS NULL OR itm."U_Tasc_ST" NOT IN ('FFM', 'LRD', 'CM'))
GROUP BY itm."ItemCode", itm."ItemName", COALESCE(itm."U_Tasc_ST", 'NULL'), COALESCE(itm."CardCode", '')
ORDER BY 5 DESC
;;
-- 5 Gegenprobe: FFM-Artikel, bei denen trotzdem ein Vendor steht (Widerspruch oder Sonderfall)
SELECT
    COUNT(DISTINCT itm."ItemCode") AS FFM_MIT_VENDOR
FROM {schema}."OINV" h
INNER JOIN {schema}."INV1" p ON h."DocEntry" = p."DocEntry"
INNER JOIN {schema}."OITM" itm ON p."ItemCode" = itm."ItemCode"
WHERE h."CANCELED" = 'N' AND h."DocDate" >= '2025-01-01'
  AND itm."U_Tasc_ST" = 'FFM'
  AND COALESCE(itm."CardCode", '') <> ''
;;
-- 6 Fuellgrad des Feldes auf dem gesamten aktiven Lagerartikelstamm
SELECT
    COALESCE("U_Tasc_ST", 'NULL') AS SALES_TYPE,
    COUNT(*) AS ARTIKEL
FROM {schema}."OITM"
WHERE "InvntItem" = 'Y' AND "validFor" = 'Y'
GROUP BY COALESCE("U_Tasc_ST", 'NULL')
ORDER BY 2 DESC
