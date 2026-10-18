-- Runde 5: Findet die schreibweisenunabhaengige Spaltensuche beide UDFs?
--
-- Anlass: HanaQueryService.HasColumnAsync schreibt den Spaltennamen gross und vergleicht exakt.
-- Indiens UDFs sind aber gemischt geschrieben - "U_Tasc_ST" neben "U_TASC_OMN". Eine
-- Gross-Schreibung haette "U_TASC_ST" gesucht, nichts gefunden und das Feld fuer Indien still
-- nie selektiert: die Auswertung waere wirkungslos geblieben, ohne Fehlermeldung.
--
-- Diese Abfrage ist genau die, die ResolveColumnNameAsync ausfuehrt. Erwartet werden ZWEI
-- Zeilen mit der Originalschreibweise, denn HANA behandelt in Anfuehrungszeichen gesetzte
-- Bezeichner case-sensitiv - im SELECT muss der gefundene Name stehen, nicht der gesuchte.

-- 1 Schreibweisenunabhaengige Suche mit Rueckgabe des tatsaechlichen Namens
SELECT COLUMN_NAME AS GEFUNDENER_NAME, DATA_TYPE_NAME, POSITION
FROM SYS.TABLE_COLUMNS
WHERE UPPER(SCHEMA_NAME) = UPPER('{schema}')
  AND UPPER(TABLE_NAME) = 'OITM'
  AND UPPER(COLUMN_NAME) IN ('U_TASC_ST', 'U_TASC_OMN')
ORDER BY 1
;;
-- 2 Gegenprobe: die exakte Suche nach der grossgeschriebenen Variante findet NICHTS
SELECT COUNT(*) AS TREFFER_MIT_GROSSSCHREIBUNG
FROM SYS.TABLE_COLUMNS
WHERE UPPER(SCHEMA_NAME) = UPPER('{schema}')
  AND TABLE_NAME = 'OITM'
  AND COLUMN_NAME = 'U_TASC_ST'
;;
-- 3 Und das SELECT selbst mit der Originalschreibweise liefert Werte
SELECT
    itm."ItemCode" AS ARTIKEL,
    COALESCE(itm."U_Tasc_ST", '') AS SALES_TYPE,
    COALESCE(itm."U_TASC_OMN", '') AS MATERIAL_NO
FROM {schema}."OITM" itm
WHERE itm."ItemCode" IN ('PT000003', 'DM000001', 'IC15415')
ORDER BY 1
