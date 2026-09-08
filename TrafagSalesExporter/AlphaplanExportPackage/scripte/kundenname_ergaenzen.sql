/* ---------------------------------------------------------------------------
   Alphaplan: Kundenname in den Export aufnehmen
   Stand 2026-09-08

   Warum: In allen 7'615 deutschen Verkaufszeilen der zentralen Datenbank ist
   CustomerName leer, die Kundennummer dagegen zu 100 Prozent gefuellt. Der
   Namensabgleich fuer die Marktsegmente findet in Deutschland deshalb nichts,
   und TRDE hat null Railway-Zuordnungen - bei 359 Kunden und 2'931'057 EUR
   Umsatz allein in 2026.

   Die Ursache liegt nicht in Alphaplan, sondern im Export: alphaplanExport.ps1
   liest ausschliesslich dbo.Belege und die Positionen. Der Fremdschluessel
   RechnungsAdressenID wird zwar mitgegeben, aber nie auf die Adresstabelle
   aufgeloest. Der Name existiert also, er wird nur nicht mitgenommen.

   Beides laeuft NUR auf dem deutschen Alphaplan-Server, weil die Datenbank dort
   als localhost\SQL2012 / ApDaten haengt. Beide Abfragen sind rein lesend.
--------------------------------------------------------------------------- */

/* --- Schritt 1: die Adresstabelle finden ---------------------------------
   Zuerst muss belegt sein, wie die Tabelle und ihre Spalten wirklich heissen.
   Nicht raten: genau dieser Fehlertyp hat bei UK-2025 und beim CH/AT-Report
   schon einmal Zeit gekostet. Ergebnis bitte als Text zurueckschicken.        */

SELECT t.TABLE_NAME, c.COLUMN_NAME, c.DATA_TYPE, c.CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.TABLES t
JOIN INFORMATION_SCHEMA.COLUMNS c
  ON  c.TABLE_SCHEMA = t.TABLE_SCHEMA
  AND c.TABLE_NAME   = t.TABLE_NAME
WHERE t.TABLE_TYPE = 'BASE TABLE'
  AND (t.TABLE_NAME LIKE '%Adress%' OR t.TABLE_NAME LIKE '%Kunde%')
ORDER BY t.TABLE_NAME, c.ORDINAL_POSITION;

/* Zur Gegenprobe: welche Tabelle traegt ueberhaupt den Schluessel, auf den
   Belege.RechnungsAdressenID zeigt?                                          */

SELECT TABLE_NAME, COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE COLUMN_NAME IN ('AdressenID', 'RechnungsAdressenID')
ORDER BY TABLE_NAME;


/* --- Schritt 2: der ergaenzte Kopfexport ---------------------------------
   Erst ausfuehren, wenn Schritt 1 die echten Namen geliefert hat. Unten stehen
   dbo.Adressen, Name1 und LandKuerzel als ANNAHME und sind entsprechend zu
   ersetzen. Die bisherigen achtzehn Spalten bleiben unveraendert in ihrer
   Reihenfolge, damit der bestehende Import nicht bricht; die beiden neuen
   haengen hinten an.

   Danach in alphaplanExport.ps1 die Variable $headerLine um
   ";KundenName;KundenLand" ergaenzen und $headersQuery durch diese Fassung
   ersetzen.                                                                   */

SET NOCOUNT ON;
SELECT
  CASE WHEN b.BelegTyp = 5 THEN 'Invoice'
       WHEN b.BelegTyp = 6 THEN 'CreditNote'
       ELSE 'Other' END,
  b.BelegeID,
  b.BelegTyp,
  b.Belegnummer,
  b.Datum,
  b.RechnungsAdressenID,
  b.WaehrungenID,
  b.ZahlungsBedingungenID,
  b.NettoPreisEndSumme,
  b.BruttoPreisEndSumme,
  b.IstStorniert,
  b.IstArchiviert,
  REPLACE(REPLACE(REPLACE(ISNULL(CONVERT(nvarchar(max), b.ExterneBelegNummer), ''), CHAR(13), ' '), CHAR(10), ' '), ';', ','),
  REPLACE(REPLACE(REPLACE(ISNULL(CONVERT(nvarchar(max), b.BestellNummer), ''), CHAR(13), ' '), CHAR(10), ' '), ';', ','),
  REPLACE(REPLACE(REPLACE(ISNULL(CONVERT(nvarchar(max), b.IhrAuftrag), ''), CHAR(13), ' '), CHAR(10), ' '), ';', ','),
  REPLACE(REPLACE(REPLACE(ISNULL(CONVERT(nvarchar(max), b.KostenStelle), ''), CHAR(13), ' '), CHAR(10), ' '), ';', ','),
  REPLACE(REPLACE(REPLACE(ISNULL(CONVERT(nvarchar(max), b.KostenTraeger), ''), CHAR(13), ' '), CHAR(10), ' '), ';', ','),
  b.UUID,
  /* NEU, Spaltennamen nach Schritt 1 anpassen: */
  REPLACE(REPLACE(REPLACE(ISNULL(CONVERT(nvarchar(max), a.Name1), ''), CHAR(13), ' '), CHAR(10), ' '), ';', ','),
  REPLACE(REPLACE(REPLACE(ISNULL(CONVERT(nvarchar(max), a.LandKuerzel), ''), CHAR(13), ' '), CHAR(10), ' '), ';', ',')
FROM dbo.Belege b
LEFT JOIN dbo.Adressen a
  ON a.AdressenID = b.RechnungsAdressenID
WHERE b.BelegTyp IN (5, 6)
  AND b.BelegeID <> 0
ORDER BY b.Datum, b.BelegeID;

/* LEFT JOIN mit Absicht: eine fehlende Adresse darf keine Rechnung aus dem
   Export werfen. Sie erscheint dann mit leerem Namen, und das ist eine
   Datenluecke, die man sehen soll, statt einer Zeile, die stillschweigend
   verschwindet.                                                               */
