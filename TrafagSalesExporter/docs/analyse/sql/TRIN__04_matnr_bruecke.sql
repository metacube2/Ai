-- Runde 4: Gibt es auf dem indischen Artikel die Trafag-Materialnummer?
--
-- Anlass: Bei LRD ist der Artikel in der Schweiz hergestellt und wird von Trafag AG bezogen
-- (Bestaetigung Ingo, 2026-08-05). Der lokale Wert INV1.StockPrice ist damit der
-- IC-Einkaufspreis, nicht die Herstellkostenbasis - genau der Wert, den die Gruppenmarge laut
-- Mappe1.xlsx ersetzen soll. Richtige Basis waere GroupStandardCosts (MBEW-STPRS,
-- Bewertungskreis 1100, CHF).
--
-- Problem, gemessen auf unserer Produktiv-DB: nur 34 von 135 Artikeln mit Lieferant Trafag AG
-- finden ueber die indische Artikelnummer einen Treffer in GroupStandardCosts. Die indischen
-- Nummern (PT000003, DM000001) sind TASC-Eigennummern, keine Trafag-MATNR.
--
-- Hypothese: Das UDF TASC_OMN ("Material No", FieldID 1) haelt die Trafag-Materialnummer und
-- ist damit die fehlende Bruecke. Weitere Kandidaten: TASC_OC ("Ordering Code"),
-- Tasc_CPN ("Customer Part No."), Tasc_DN ("Drawing No").
--
-- Diese Runde misst nur - sie entscheidet nichts. Ausgewertet wird gegen
-- GroupStandardCosts anschliessend lokal.

-- 1 Fuellgrad der Nummern-Kandidaten je Sales Type (Artikel mit Umsatz ab 2025)
SELECT
    COALESCE(itm."U_Tasc_ST", 'NULL') AS SALES_TYPE,
    COUNT(DISTINCT itm."ItemCode") AS ARTIKEL,
    COUNT(DISTINCT CASE WHEN COALESCE(itm."U_TASC_OMN", '') <> '' THEN itm."ItemCode" END) AS MIT_OMN,
    COUNT(DISTINCT CASE WHEN COALESCE(itm."U_TASC_OC", '') <> '' THEN itm."ItemCode" END) AS MIT_ORDERING_CODE,
    COUNT(DISTINCT CASE WHEN COALESCE(itm."U_Tasc_CPN", '') <> '' THEN itm."ItemCode" END) AS MIT_CUSTOMER_PN,
    COUNT(DISTINCT CASE WHEN COALESCE(itm."U_Tasc_DN", '') <> '' THEN itm."ItemCode" END) AS MIT_DRAWING_NO
FROM {schema}."OINV" h
INNER JOIN {schema}."INV1" p ON h."DocEntry" = p."DocEntry"
INNER JOIN {schema}."OITM" itm ON p."ItemCode" = itm."ItemCode"
WHERE h."CANCELED" = 'N' AND h."DocDate" >= '2025-01-01'
GROUP BY COALESCE(itm."U_Tasc_ST", 'NULL')
ORDER BY 2 DESC
;;
-- 2 Alle LRD-Artikel mit ihren Nummern - Grundlage fuer den Abgleich mit GroupStandardCosts
SELECT
    itm."ItemCode" AS ARTIKEL,
    COALESCE(itm."U_TASC_OMN", '') AS MATERIAL_NO,
    COALESCE(itm."U_TASC_OC", '') AS ORDERING_CODE,
    COALESCE(itm."CardCode", '') AS VENDOR,
    COUNT(*) AS RECHNUNGSZEILEN
FROM {schema}."OINV" h
INNER JOIN {schema}."INV1" p ON h."DocEntry" = p."DocEntry"
INNER JOIN {schema}."OITM" itm ON p."ItemCode" = itm."ItemCode"
WHERE h."CANCELED" = 'N' AND h."DocDate" >= '2025-01-01'
  AND itm."U_Tasc_ST" = 'LRD'
GROUP BY itm."ItemCode", COALESCE(itm."U_TASC_OMN", ''), COALESCE(itm."U_TASC_OC", ''), COALESCE(itm."CardCode", '')
ORDER BY 5 DESC
;;
-- 3 Sehen die Nummern wie eine Trafag-MATNR aus (Stichprobe mit Bezeichnung)
SELECT
    itm."ItemCode" AS ARTIKEL,
    itm."U_Tasc_ST" AS SALES_TYPE,
    COALESCE(itm."U_TASC_OMN", '') AS MATERIAL_NO,
    COALESCE(itm."U_TASC_OC", '') AS ORDERING_CODE,
    itm."ItemName" AS BEZEICHNUNG
FROM {schema}."OITM" itm
WHERE itm."ItemCode" IN ('PT000003', 'PT000010', 'DM000001', 'DM000083', 'PS000674', 'H90101')
ORDER BY 1
