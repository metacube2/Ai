# DE-Kundenzuordnung: Nachweis, Korrektur und Export

Stand 09.09.2026. Fachlicher Nachweis: `docs/BAHNMARKT_DE_ANBINDUNG_2026-09-08.md`.

Die eingebettete Belegbruecke ist eine begrenzte Reparatur des vorhandenen Bestands.
Sie ersetzt keinen aktuellen Alphaplan-Kundenstamm mit beiden Nummernkreisen.
Der App-Leser verwendet nur `Basis = Belegnachweis` und verlangt zugleich die
Rechnungsnummer und die interne Adress-ID. Eine vorhandene fachliche Quellspalte
hat immer Vorrang. Historische ID-Ableitungen werden ausschliesslich als Vorschlaege
ausgegeben; offene interne Nummern erhalten `ALPHAPLAN-ID:`. Keine Lieferantennummern
oder blossen Namensaehnlichkeiten zum Kundenabgleich verwenden.

## Reproduzieren

1. `build_mapping.py --sales <unveraenderte TRDE-CSV> --evidence <DE-Beispiel.xlsx>`
   erzeugt die JSON-Belegbruecke und die DE-Pruefmappe. Python 3.12 und openpyxl via uv.
   Weitere Quellen: `docs/2025_DataExport_DE.xlsx`, `docs/Sales_TRDE*.xlsx`, lokale
   Alphaplan-Rohkoepfe und `kundenstamm_DE_20260908.csv`. Das geloeschte Beispiel wurde
   beim Lauf aus der Git-Historie gelesen, nicht aus dem Produktivimport ersetzt.
2. `dotnet run --project Tools/DeCustomerMapping/Apply -c Release -- <db> <mapping.json> <arbeitsordner>`
   speichert den Vorherbestand und die DE-Segmentzuordnungen. Geheimnisse werden nicht
   ausgegeben. Fuer den Produktivlauf eine frische Vorherpruefung verwenden.
3. Dieselben Argumente plus `--validate <quell.csv> <server-output-ordner>` pruefen
   Zeilenmultimenge und Finanzwerte numerisch und erzeugen lokale Dateien. Unterschiedliche
   Dezimalskalen wie `2` und `2.0` sind kein Wertunterschied. Bei echten Unterschieden
   bricht das Werkzeug ab. Die vorhandene Sales-Excel wird nur an Kunden-/Segmentzellen
   korrigiert, keine Finanzblaetter mit Defaultregeln neu berechnen.
4. `--apply` statt `--validate` aktualisiert ausschliesslich vier Kundenfelder und die
   belegten reinen Bahnbranchen in einer Transaktion. Bestaetigte Gegenentscheide bleiben
   erhalten. Kein Loeschen/Neuladen von Verkaufszeilen. Dateien werden anschliessend
   ueber einen temporaeren Dateinamen bereitgestellt. DB und Dateisystem sind keine
   gemeinsame Transaktion: bei vorhandenem `database-applied.txt` nie blind erneut starten.
5. CSV und alle Finanz-/Lieferantenwerte gegen die Vorhersicherung pruefen. Rohail-Mappe
   mit `Report <konsistenter-snapshot.db> <ausgabe.xlsx>` erzeugen. Der Dienst ist derselbe
   wie beim Excel-Knopf auf `/marktsegmente`; Jahr und Standort sind hier ungefiltert.
6. `Publish <snapshot.db> <Sales_ProcessedMergeInput_TRDE_*.csv> <Sales_TRDE_*.xlsx>`
   legt neue Dateien im konfigurierten DE-SharePoint-Ordner an. Vorhandene Dateien werden
   nicht ersetzt. `--verify-only` liest vorhandene Dateien zurueck: CSV bytegleich,
   Excel alle Zellwerte und Formeln gleich. SharePoint kann Klassifizierungsmetadaten
   und Beziehungen ergaenzen, weshalb ein ZIP-Gesamthash kein Inhaltsnachweis ist.

## Verwandt: Namenskorrektur der Artikelbezeichnung

`Tools/DeNameFix` zieht die Spalte `Name` nach, wenn sich die RTF-Bereinigung geaendert hat,
und `Tools/DeNameFixFiles` korrigiert dieselbe Spalte in der bereits veroeffentlichten CSV
und Sales-Excel. `Publish` kennt dafuer `--ersetzen`; das ueberschreibt eine vorhandene
SharePoint-Datei bewusst und ist nur fuer eine Korrektur desselben Tages gedacht, bei der
die Vorversion gesichert und der Unterschied nachgewiesen ist. Fachlicher Nachweis:
`docs/STANDORT_DE_ALPHAPLAN.md` Abschnitt 8.

## Lokale Datenbankkopie fuer grosse Auswertungen

Die am 09.09. verwendete Kopie wurde unter SQLite-Schreibsperre erstellt: zuerst
`PRAGMA wal_checkpoint(TRUNCATE)` mit erfolgreichem Ergebnis, dann `BEGIN IMMEDIATE`.
Unter der Sperre musste die WAL-Datei weiterhin leer sein; sonst Abbruch. Erst dann
wurde die Hauptdatei lokal kopiert, die Sperre im finally freigegeben und lokal
`PRAGMA quick_check` ausgefuehrt. Dadurch koennen waehrend der Kopie keine neuen
Schreibvorgaenge oder Checkpoints die Hauptdatei veraendern. Die Sperre blockiert
Schreiber nur fuer die Kopierdauer. Eine freie Blockkopie einer aktiven WAL-Datenbank
oder allein ein gleicher Zeitstempel ist kein konsistenter Snapshot-Nachweis.

## Offen

3'066 Zeilen bleiben produktiv ohne fachliche Kundenzuordnung: 2'427 historische
Kandidaten, 624 ohne Kandidat und 15 Konfliktzeilen. Fuer weitere Freigaben ist ein
aktueller Quellabgleich `RechnungsAdressenID -> AdressNummer-Kunde` erforderlich.
Die 171 Vorschlaege anderer Standorte werden durch den DE-Stamm nicht bestaetigt.
