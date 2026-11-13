/* ---------------------------------------------------------------------------
   Alphaplan (ApDaten): Schemaauszug fuer Kundenfelder und Lieferant
   Stand 2026-11-13, nur lesend (INFORMATION_SCHEMA, keine Daten, keine Aenderung)

   Wozu: Der Verkaufsexport (alphaplanExport.ps1 / alphaplandeltaexport.ps1) liest
   nur dbo.Belege und dbo.BelegePositionen. Zwei Dinge fehlen deshalb in der
   Gruppenauswertung:
     1. Kundennummer und Kundenname: mitgegeben wird nur die interne
        RechnungsAdressenID. Seit dem 09.09.2026 stehen neue Rechnungen dadurch
        ohne fachliche Kundennummer und ohne Namen da (ISS-016).
     2. Lieferant je Artikel: wird gar nicht gelesen, Deutschland hat 0 Prozent
        Lieferantenangaben (ISS-003.3).
   Um beide Verknuepfungen sauber zu ergaenzen, brauchen wir die echten Tabellen-
   und Spaltennamen. Bitte nichts raten und nichts umbenennen: nur die drei
   Abfragen ausfuehren (SQL Server Management Studio, Datenbank ApDaten) und die
   Ergebnisse als Excel oder CSV zurueckschicken.
--------------------------------------------------------------------------- */

/* 1. Tabellen und Spalten rund um Adressen, Kunden, Artikel, Lieferanten, Kreditoren */
SELECT t.TABLE_SCHEMA, t.TABLE_NAME, c.ORDINAL_POSITION, c.COLUMN_NAME, c.DATA_TYPE, c.CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.TABLES t
JOIN INFORMATION_SCHEMA.COLUMNS c
  ON  c.TABLE_SCHEMA = t.TABLE_SCHEMA
  AND c.TABLE_NAME   = t.TABLE_NAME
WHERE t.TABLE_TYPE = 'BASE TABLE'
  AND (   t.TABLE_NAME LIKE '%Adress%'
       OR t.TABLE_NAME LIKE '%Kunde%'
       OR t.TABLE_NAME LIKE '%Artikel%'
       OR t.TABLE_NAME LIKE '%Liefer%'
       OR t.TABLE_NAME LIKE '%Kredit%')
ORDER BY t.TABLE_NAME, c.ORDINAL_POSITION;

/* 2. Gegenprobe: welche Tabellen tragen die Schluessel, auf die Belege und
      Positionen zeigen (Adresse, Artikel, Lieferant)?                          */
SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE COLUMN_NAME IN ('AdressenID', 'RechnungsAdressenID', 'AdressNummer', 'ArtikelID', 'ArtikelNummer',
                      'LieferantenID', 'LieferantID', 'HauptLieferantID', 'KreditorenID')
   OR COLUMN_NAME LIKE '%Lieferant%'
ORDER BY TABLE_NAME, COLUMN_NAME;

/* 3. Spalten der beiden Tabellen, die der Export heute liest */
SELECT TABLE_NAME, ORDINAL_POSITION, COLUMN_NAME, DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('Belege', 'BelegePositionen')
ORDER BY TABLE_NAME, ORDINAL_POSITION;
