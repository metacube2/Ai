-- Runde 1: Wo steckt das Feld "Sales Type" im indischen SAP B1?
-- Anlass: Call mit RanVijay Kumar (Trafag India) am 2026-08-05. Lokal gefertigte Artikel
-- sind NICHT ueber den Preferred Vendor erkennbar, sondern ueber ein Feld "Sales Type" mit
-- den Werten "LRD" (Import von Trafag Schweiz, Weiterverkauf) und "full-fledged
-- manufacturing" (Produktion im indischen Werk). Der technische Spaltenname ist unbekannt
-- und wird hier ERMITTELT, nicht geraten.
--
-- Alle Statements sind reine SELECTs. Der Dateiname beginnt mit dem TSC (TRIN), damit
-- Analyse-Ausfuehren.cmd den Standort daraus ableitet. Statements sind durch eine Zeile
-- getrennt, die mit ;; beginnt. Faellt ein Statement aus, laufen die uebrigen weiter.

-- 1 Welche B1-Tabellen existieren ueberhaupt (Existenzpruefung vor allen Annahmen)
SELECT TABLE_NAME, RECORD_COUNT
FROM SYS.M_TABLES
WHERE SCHEMA_NAME = '{SCHEMA}'
  AND (TABLE_NAME LIKE 'OIT%' OR TABLE_NAME IN ('CUFD', 'UFD1', 'OUDG'))
ORDER BY TABLE_NAME
;;
-- 2 UDF-Woerterbuch: alle benutzerdefinierten Felder des Artikelstamms mit Beschriftung
-- Hier muss "Sales Type" als Descr auftauchen; AliasID ergibt den Spaltennamen U_<AliasID>.
-- Absichtlich SELECT * - die Spaltennamen von CUFD werden ebenfalls nicht geraten.
SELECT *
FROM {schema}."CUFD"
WHERE "TableID" = 'OITM'
;;
-- 3 UDF-Woerterbuch der Belegtabellen (falls das Feld an der Zeile statt am Artikel haengt)
SELECT *
FROM {schema}."CUFD"
WHERE "TableID" IN ('OINV', 'INV1', 'ORIN', 'RIN1')
;;
-- 4 Gueltige Werte der Artikelstamm-UDFs (hier muessen LRD und die Fertigungsvariante stehen)
SELECT *
FROM {schema}."UFD1"
WHERE "TableID" = 'OITM'
;;
-- 5 OITM-Spalten mit Namensbezug zu Sales Type, Herkunft oder Fertigung
-- Hinweis: der Unterstrich ist in LIKE ein Platzhalter - ohne ESCAPE matcht 'U_%' auch
-- UserSign/UserText/UpdateDate. Schemavergleich case-insensitiv, weil Schemanamen je
-- Standort unterschiedlich geschrieben sind (TRAFAG_LIVE vs. it01_p).
SELECT COLUMN_NAME, DATA_TYPE_NAME, LENGTH, POSITION
FROM SYS.TABLE_COLUMNS
WHERE UPPER(SCHEMA_NAME) = UPPER('{schema}')
  AND TABLE_NAME = 'OITM'
  AND (COLUMN_NAME LIKE 'U\_%' ESCAPE '\'
       OR UPPER(COLUMN_NAME) LIKE '%SALE%'
       OR UPPER(COLUMN_NAME) LIKE '%TYPE%'
       OR UPPER(COLUMN_NAME) LIKE '%ORIG%'
       OR UPPER(COLUMN_NAME) LIKE '%MANU%'
       OR UPPER(COLUMN_NAME) LIKE '%LRD%')
ORDER BY POSITION
;;
-- 6 Fertige Wertabfragen fuer Runde 2 (Ausgabe ist SQL-Text zum Kopieren, keine Auswertung)
SELECT 'SELECT "' || COLUMN_NAME || '" AS WERT, COUNT(*) AS ARTIKEL FROM {schema}."OITM" WHERE "InvntItem" = ''Y'' AND "validFor" = ''Y'' GROUP BY "' || COLUMN_NAME || '" ORDER BY 2 DESC' AS SQL_FUER_RUNDE_2
FROM SYS.TABLE_COLUMNS
WHERE UPPER(SCHEMA_NAME) = UPPER('{schema}')
  AND TABLE_NAME = 'OITM'
  AND COLUMN_NAME LIKE 'U\_%' ESCAPE '\'
ORDER BY POSITION
;;
-- 7 Pruefanker: zwei Artikel, deren Einordnung aus dem Call bzw. den Produktivdaten bekannt ist
-- PT000003/PT000010 tragen bei uns Lieferant Trafag AG/CH -> muessen laut Call "LRD" sein.
-- DM000001/DM000083 haben keinen Lieferanten -> muessen "full-fledged manufacturing" sein.
-- Absichtlich SELECT *: ohne bekannten Spaltennamen ist die Zeile breit, enthaelt aber
-- garantiert den gesuchten Wert. Damit ist Runde 1 im Idealfall schon beweisend.
SELECT *
FROM {schema}."OITM"
WHERE "ItemCode" IN ('PT000003', 'PT000010', 'DM000001', 'DM000083')
