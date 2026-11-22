# Finance Dashboard: offene Punkte

Stand: 2026-08-12

**Einzige gueltige Quelle fuer den Status je Punkt ist
`docs/Issue_Log_Konsolidiert_2026-08-12.tsv`.** Diese Datei erklaert die Struktur und die
fachliche Begruendung; die Tabelle dort traegt die Spalten des Issue-Logs und laesst sich
direkt nach Excel kopieren. Wenn beide auseinanderlaufen, gilt das TSV.

Alle Zahlen sind am 2026-08-12 read-only gegen die produktive Datenbank gemessen, nicht aus
aelteren Notizen uebernommen.

## Konsolidierung: aus 18 Einzelpunkten wurden 12 Issues

Zwei Punkte sind entfallen, weil die Live-Pruefung sie als erledigt gezeigt hat, obwohl
aeltere Notizen sie noch als offen fuehrten. Genau dieser Effekt ist der Grund fuer die
Konsolidierung.

| Gruppe | Enthaelt | Warum zusammen |
| --- | --- | --- |
| `ISS-003` Lieferant und Gruppenmarge | Rohdatenluecke, lokale Standardkosten ohne Preis, DE/ES-Quelle, CH/AT-Herstellerregel, Umschalter je Land | Alle fuenf haengen an derselben Ursache: die Quelle liefert keinen Lieferanten. Sie brauchen aber verschiedene Owner, deshalb Unterpunkte statt einer Zeile |
| `ISS-004` TR ES Datenqualitaet | Luecke Januar bis Mai 2026, fehlendes Buchungsdatum, Laendercodes | Ein Standort, eine Exportquelle, ein Ansprechpartner |
| `ISS-007` Kostenbasis und Bewertungsmethoden | B1-Upgrade-Nachpruefung, TR IT Moving Average | Beide fragen, wie belastbar die Kostenbasis nach dem Upgrade ist |

Getrennt geblieben sind Punkte mit eigenem Owner und eigener Entscheidung, etwa Budget-CHF,
die Fachfreigabe der Gruppenmarge und die Innenumsatzfrage.

## Was die Live-Pruefung heute korrigiert hat

- **Laendercodes Spanien sind erledigt.** Aeltere Notizen fuehren „BEHOBEN im Code, Deploy
  offen". Tatsaechlich ist `NormalizeCountryCodeTransformationStrategy` in der
  ausgelieferten Server-DLL nachgewiesen, und Spanien zeigt in den Produktivdaten
  ISO-2-Codes: `ES` 3'955, `BR` 244, `PT` 233, `PE` 202, `MX` 195. Kein ES-Wert ist laenger
  als zwei Zeichen. Der Reimport hat also stattgefunden.
- **Die verwaiste Legendenzeile ist weg.** `CentralSalesRecords` enthaelt genau neun
  TSC-Werte. Der Eintrag `TSC = "Subsidiary abbreviation / company identifier"` existiert
  nicht mehr, obwohl mehrere Dateien ihn noch als offen fuehren.
- **Die Supplier-Quote war veraltet.** Nicht 77'466 von 95'396, sondern 78'057 von 96'298
  Zeilen ohne jedes Lieferantenfeld; 18'241 Zeilen (18,9 %) sind vollstaendig.
- **Ein hoher Punkt fehlte ganz:** der Datenzufluss fuer AT, CH und FR steht seit dem
  2026-07-31. Am 2026-08-12 unveraendert bestaetigt. Das ist jetzt `ISS-001`.

## Falle bei eigenen Abfragen

`CentralSalesRecords.StandardCost` ist eine **TEXT**-Spalte. In SQLite ist jeder Text
groesser als jede Zahl, deshalb liefert `StandardCost > 0` fuer **jede** Zeile wahr und damit
scheinbar 100 % Fuellgrad in allen Laendern. Nur mit `CAST(... AS REAL)` messen. Richtig
gemessen am 2026-08-12: FR 51,7 %, DE 68,7 %, ES 81,0 %, US 90,0 %, UK 93,5 %, IT 95,7 %,
CH 96,6 %, IN 99,4 %, AT 99,9 %. Dasselbe gilt fuer `PostingDate`, ebenfalls TEXT.

## Bewusst nicht in dieser Liste

Einkauf und Logistik, also ZLO03, ZC12, ZZPRDAT und die Produktgruppen-Sets
`ZDISPO_GRP`/`ZDISPO_SPART`, stehen in `docs/rag/PURCHASING.md` und
`docs/PURCHASING_PRODUCT_GROUP_SAP_DIRECT_2026-08-11.md`. Die beiden SAP-Sets sind seit dem
2026-08-12 aktiv und liefern HTTP 200; der Einkauf-Delta lief um 10:03:42 MESZ mit `Success`.
