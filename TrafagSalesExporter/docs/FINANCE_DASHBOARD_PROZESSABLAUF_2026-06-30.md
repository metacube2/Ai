# Prozessablauf Finance Dashboard

Stand: 2026-06-30

## Kurzfazit

Das Finance Dashboard arbeitet nicht direkt aus dem zentralen Excel `Sales_All_*`.

Die operative Dashboard-Quelle ist entweder:

- die zentrale Datenbank `CentralSalesRecords`, oder
- bei aktiviertem Audit-Modus die neuesten `Sales_ProcessedMergeInput_*.csv` je Standort.

Aktueller produktiver Stand: Die zentrale Auswertung ist auf Audit-CSV umgestellt.

Die Reihenfolge fuer Dashboard, Finance Summary, Management Analyse und `Finance Pruefbuch` ist:

1. bevorzugt die neuesten `Sales_ProcessedMergeInput_*.csv` je TSC,
2. falls keine Standort-CSV vorhanden sind, die neueste zentrale `Finance_Dashboard_Audit_All_*.csv`,
3. ohne Audit-CSV-Modus die zentrale Datenbank `CentralSalesRecords`.

`Sales_All_*.xlsx` ist der zentrale Excel-Export/Nachweis fuer Finance, aber nicht die Live-Quelle der Dashboard-Reiter.

## 1. Rohdaten je Standort

Die Standorte liefern ihre Finance-Daten nach SharePoint, zum Beispiel in den Ordner:

```text
Import/Finance/Frankreich
```

Die Daten kommen je nach Standort entweder:

- per `rclone` vom Land,
- per manuellem Upload,
- automatisch aus SAP B1 / HANA / Sage / SAP OData,
- oder aus einem lokalen bzw. SharePoint-basierten Importprozess.

Beispiel fuer eine Standortdatei:

```text
Sales_TRFR_2026-04-16.xlsx
```

Beispiel-Link Frankreich:

```text
https://trafagag.sharepoint.com/sites/WorldwideBIPlatform/Shared%20Documents/Forms/AllItems.aspx?id=%2Fsites%2FWorldwideBIPlatform%2FShared%20Documents%2FImport%2FFinance%2FFrankreich&viewid=7a89b0d8%2D1a5e%2D41dc%2D8973%2Df58a849951f7&newTargetListUrl=%2Fsites%2FWorldwideBIPlatform%2FShared%20Documents&viewpath=%2Fsites%2FWorldwideBIPlatform%2FShared%20Documents%2FForms%2FAllItems%2Easpx
```

## 2. Standortexport / Aufbereitung je Land

Im Export Dashboard gibt es neben jedem Land einen Export-Button.

Dieser Export liest die Rohdaten des Standortes, wendet Mapping und Transformationen an und erzeugt daraus die aufbereitete Dashboard-Datei fuer den Standort.

Normalerweise sollen alle Laender aktualisiert werden. Dafuer wird oben der Button:

```text
Alle exportieren
```

verwendet.

Dabei passiert pro Standort:

```text
Rohdaten lesen
-> Mapping auf Dashboard-Felder
-> Transformationen anwenden
-> Standort-Excel erzeugen
-> Audit-CSV schreiben
-> zentrale Standortdaten aktualisieren
-> optional Upload nach SharePoint
```

Beispiel fuer die aufbereitete Audit-/Merge-Datei:

```text
Sales_ProcessedMergeInput_TRFR_2026-06-17.csv
```

Diese Datei ist fuer Finance wichtig, weil sie die verarbeiteten Daten nach Mapping und Transformation zeigt. Sie ist damit der lesbare Nachweis, welche Zeilen in die zentrale Auswertung eingehen.

## 3. Zentrale Dashboard-Quelle

Alle Standorte haben nach dem Export eine Datei nach folgendem Muster:

```text
Sales_ProcessedMergeInput_<TSC>_<Datum>.csv
```

Beispiele:

```text
Sales_ProcessedMergeInput_TRFR_2026-06-17.csv
Sales_ProcessedMergeInput_TRDE_2026-06-17.csv
Sales_ProcessedMergeInput_ZSCHWEIZ_2026-06-17.csv
```

Wenn die Einstellung `Zentrale Auswertung aus Audit-CSV` aktiv ist, liest das Dashboard die neuesten `Sales_ProcessedMergeInput_*.csv` je TSC und setzt daraus intern die zentrale Sicht zusammen.

Aktueller produktiver Serverpfad:

```text
C:\inetpub\wwwcust\BiDashboard\output
```

Von aussen ist derselbe Ordner ueber die Admin-Freigabe sichtbar:

```text
\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\output
```

Aktuell aktiv:

```text
Audit-CSV je Standort schreiben = ja
Zentrale Auswertung aus Audit-CSV = ja
LocalSiteExportFolder = leer
```

Damit gilt:

```text
Dashboard / Finance Summary / Management Analyse / Finance Pruefbuch
-> liest bevorzugt je Standort die neuesten Sales_ProcessedMergeInput_*.csv
-> faellt bei fehlenden Standort-CSV auf Finance_Dashboard_Audit_All_*.csv zurueck
-> konsolidiert die Daten intern fuer die Anzeige
```

## 4. Zentrales Excel fuer Finance

Mit dem Button:

```text
Zentrale Datei neu erzeugen
```

wird das zentrale konsolidierte Excel aller Standorte erzeugt.

Dieses File liegt in SharePoint unter:

```text
Import/Finance/Alle
```

Beispiel:

```text
Sales_All_2026-06-18.xlsx
```

Beim gleichen Lauf wird auch die zentrale Audit-CSV erzeugt:

```text
Finance_Dashboard_Audit_All_2026-06-18.csv
```

Beide Dateien liegen lokal im gleichen zentralen Output-Ordner und werden, wenn SharePoint korrekt konfiguriert ist, in denselben SharePoint-Ordner hochgeladen:

```text
Import/Finance/Alle
```

Wichtig:

`Sales_All_*.xlsx` ist der zentrale Excel-Export und Nachweis der konsolidierten Daten. Die Dashboard-Anzeigen lesen jedoch nicht direkt aus diesem Excel.

Das zentrale Excel wird aus derselben zentralen Auswertungslogik erzeugt, ist aber nicht die Live-Quelle der Dashboard-Reiter.

Richtig ist:

```text
Dashboard-Anzeige
-> liest aus CentralSalesRecords oder Audit-CSV

Sales_All_*.xlsx
-> Excel-Export/Nachweis fuer Finance

Finance_Dashboard_Audit_All_*.csv
-> zentrale maschinenlesbare Detaildatei, Fallback-Quelle fuer Dashboard/Pruefbuch
```

Der Vorteil dieser Trennung: Das Dashboard ist nicht davon abhaengig, ob jemand das zentrale Excel bereits neu erzeugt hat. Sobald die Standortdaten bzw. Audit-CSV aktualisiert sind, kann die zentrale Analyse daraus arbeiten.

## 5. Umrechnung in CHF

Die Umrechnung in CHF passiert nicht automatisch beim Laenderexport und nicht automatisch beim Erzeugen von `Sales_All_*`.

Standardmaessig bleiben die Werte in der jeweiligen Landes- bzw. Hauswaehrung.

Beispiele:

| Land | Standard-Waehrung |
| --- | --- |
| Schweiz | CHF |
| Frankreich | EUR |
| Deutschland | EUR |
| Italien | EUR |
| Spanien | EUR |
| UK | GBP |
| USA | USD |
| Indien | INR |

Eine CHF-Umrechnung erfolgt nur in speziellen Anzeige- oder Transformationspfaden:

- im Management Cockpit mit dem Schalter `Group-Waehrung (CHF)`,
- in Analyse-/Diagnose-Sichten mit Anzeige-Waehrung `CHF`,
- wenn explizit eine `ConvertCurrency`-Transformation konfiguriert ist,
- oder in separaten Budget-CHF-/Kontrollsichten, falls fachlich freigegeben.

Wichtig fuer Finance:

`DocumentRate` aus SAP/B1 ist ein Quellfeld aus dem Landessystem. Es wird gespeichert, aber nicht automatisch fuer die Dashboard-CHF-Umrechnung verwendet. Die App-Umrechnung nutzt die gepflegte Kurstabelle `CurrencyExchangeRates`.

## 6. Finance Pruefbuch und Nachweisdateien

Fuer Andreas / Finance gibt es im Management Cockpit den Reiter:

```text
Management Analyse > Experten > Finance Pruefbuch
```

Dieser Reiter ist bewusst keine Zusammenfassung, sondern eine Excel-aehnliche Detailanzeige.

Er zeigt zeilenbasiert:

- Land,
- TSC,
- Jahr,
- Beleg und Position,
- Kunde,
- Material,
- Originalbetrag,
- Originalwaehrung,
- Kurs nach CHF,
- Betrag CHF,
- Kursquelle,
- Lieferant,
- Lieferantentyp intern/extern,
- Standardkosten,
- Kostenbasis CHF,
- Marge CHF,
- Pruefstatus,
- Datenquelle.

Damit kann Finance die Dashboard-Werte in Excel-Logik nachvollziehen:

```text
Originalbetrag
* Kurs nach CHF
= Betrag CHF
```

und fuer die Gruppenmarge:

```text
Umsatz
- Kostenbasis
= Gruppenmarge
```

Der Reiter hat einen eigenen Button:

```text
Export to Excel
```

Damit kann Andreas die sichtbare Prueflogik als Excel herunterladen und ausserhalb des Dashboards nachrechnen.

### Berechnungslogik im Finance Pruefbuch

Aktueller Stand 30.06.2026:

```text
Betrag CHF
= Originalbetrag * Kurs nach CHF
```

```text
Kostenbasis CHF
= Kostenbasis Original * Standardkosten-Kurs nach CHF
```

```text
Marge CHF
= Betrag CHF - Kostenbasis CHF
```

```text
Marge %
= Marge Original / Originalbetrag
```

Gutschriften und Retouren (negativer Netto-Umsatz):

```text
Kostenbasis kehrt mit dem Vorzeichen des Umsatzes um
Umsatz -100, Stueckkosten 60 -> Kostenbasis -60 -> Marge -40
```

Korrigiert am 30.06.2026: Frueher wurde die Kostenbasis immer positiv gerechnet
(`Abs(Menge) * Abs(Standardkosten)`). Bei einer Gutschrift ergab das faelschlich
`-100 - (+60) = -160` statt korrekt `-40`. Jetzt folgt die Kostenbasis dem Vorzeichen
des Netto-Umsatzes (bei Umsatz 0 dem Mengenvorzeichen), sodass sich Verkauf und
Gutschrift zur Netto-Marge ausgleichen. Betrifft `Finance Pruefbuch` und `Gruppenmarge`.

Wenn fuer eine Originalwaehrung kein CHF-Kurs gefunden wird, wird die Zeile nicht still falsch berechnet. Der Status zeigt dann `Kurs fehlt`.

Wenn Lieferant oder Standardkostenbasis fachlich nicht sauber bestimmbar sind, wird dies ebenfalls im Status sichtbar, zum Beispiel `Lieferant unklar` oder `Standardpreis fehlt`.

Wichtig fuer die fachliche Pruefung:

- Das Pruefbuch ist die richtige Sicht fuer Einzelzeilen und Nachvollziehbarkeit.
- Es verwendet die gepflegte Kurstabelle `CurrencyExchangeRates`.
- Fuer die CHF-Umrechnung wird aktuell ein Jahreskurs per `31.12.<Jahr>` verwendet, kein Tageskurs.
- Die Berechnungen sind technisch getestet; die fachliche Abnahme der Kurse, Kostenbasis und Lieferantenlogik muss Finance/Andreas anhand von Stichproben bestaetigen.

Bekannte, noch offene Punkte (kein Fehler in `Marge CHF`, aber zu beachten):

- `Marge Original` und `Marge %` rechnen Umsatz und Kostenbasis in ihren jeweiligen Originalwaehrungen. Wenn Verkaufswaehrung und Standardkostenwaehrung abweichen, mischen diese beiden Spalten zwei Waehrungen. `Marge CHF` ist korrekt, weil dort beide Seiten getrennt nach CHF umgerechnet werden.
- Der `Finance Pivot` rechnet bei aktivem Schalter `Group-Waehrung (CHF)` historische Jahre mit dem Kurs des gewaehlten Jahres statt mit dem jeweiligen Jahreskurs. Ohne Group-Schalter (Normalfall) ist der Pivot korrekt mit dem eigenen Jahreskurs je Zeile.

## 7. Finance Pivot nach Andreas' Excel `sta.xlsx`

Am 30.06.2026 wurde zusaetzlich der neue Reiter eingebaut:

```text
Management Analyse > Experten > Finance Pivot
```

Grundlage war Andreas' Excel-Datei `sta.xlsx`, Blatt `piv`.

Der Reiter ist keine Detailpruefung wie das `Finance Pruefbuch`, sondern eine Pivot-Sicht fuer schnelle Summen- und Monatskontrolle.

Er zeigt:

- KPI `YTD Umsatz`,
- KPI `MTD Umsatz`,
- Monatsmatrix nach `YYYY / MM / TSC`,
- Tagesmatrix fuer den gewaehlten Monat nach `MM / D / Jahr`,
- Excel-aehnliche Filter fuer Jahr, Monat und TSC,
- Excel-Export `Finance_Pivot_*.xlsx`.

Die Monatsmatrix entspricht fachlich:

```text
Summe Net Sales in CHF
gruppiert nach Jahr, Monat und TSC
```

Die Tagesmatrix entspricht fachlich:

```text
Summe Net Sales in CHF
gruppiert nach Monat, Tag und Jahr
```

Der Excel-Export enthaelt zwei Blaetter:

| Blatt | Inhalt |
| --- | --- |
| `Monate nach TSC` | Monats-Pivot mit TSC-Spalten und Gesamtergebnis |
| `Tage nach Jahr` | Tages-Pivot fuer den gewaehlten Monat mit Jahr-Spalten und Gesamtergebnis |

Berechnung:

```text
Net Sales in CHF
= Net Sales Original * CHF-Jahreskurs
```

Es werden nur Finance-Zeilen mit `Include = true` und vorhandenem CHF-Kurs in die Pivot-Summen aufgenommen.

Filterstand 01.07.2026:

- `Jahr`: grenzt Monatsmatrix, Tages-Jahrspalten, YTD und MTD ein; `Alle Jahre` zeigt die komplette Pivot-Historie.
- `MTD Monat`: waehlt den Monat fuer Tagesmatrix und MTD-KPI.
- `TSC`: grenzt Monatsmatrix, Tagesmatrix, YTD, MTD und Export auf einen Standort ein; `Alle TSC` zeigt die Gesamtsicht.
- Der Excel-Export verwendet denselben Filterzustand wie der sichtbare Reiter.

Wichtig:

- Der Pivot ist fuer Summenkontrolle und Vergleich gegen Andreas' Excel gedacht.
- Das `Finance Pruefbuch` bleibt die bessere Sicht, wenn einzelne Rechnungen, Positionen, Kurse oder Kostenbasis geprueft werden muessen.
- Zeilen ohne CHF-Kurs erscheinen nicht in der Pivot-Summe; sie sind im Pruefbuch ueber den Status sichtbar.

Technischer Stand:

- Commit fuer den neuen Pivot-Reiter: `790863c Add finance pivot tab`.
- Deploy am 30.06.2026 auf `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`.
- Server-DLL nach Deploy: `BiDashboard.dll`, Zeitstempel `30.06.2026 14:14:37`.
- Testlauf: `dotnet test TrafagSalesExporter.sln --verbosity minimal`, `124/124` Tests bestanden.

## 8. Zentrale Nachweisdateien

Beim Button `Zentrale Datei neu erzeugen` entstehen mehrere zentrale Dateien im Ordner:

```text
Import/Finance/Alle
```

Die wichtigsten Dateien sind:

| Datei | Zweck | Dashboard-Quelle |
| --- | --- | --- |
| `Sales_All_*.xlsx` | konsolidierter Excel-Export aller Standorte, zentraler Finance-Nachweis | Nein |
| `Finance_Dashboard_Audit_All_*.csv` | maschinenlesbare Detaildatei aller Standorte | Ja, als Fallback wenn keine Standort-CSV vorhanden sind |
| `Finance_Dashboard_Nachweis_*.xlsx` | Excel-Nachweisdateien fuer Finance/Andreas | Nein |

Reihenfolge beim Button `Zentrale Datei neu erzeugen`:

```text
Neueste Laenderdateien pruefen
-> zentrale Daten zusammenstellen
-> Sales_All_*.xlsx erzeugen
-> Sales_All_*.xlsx nach SharePoint laden
-> Finance_Dashboard_Audit_All_*.csv erzeugen
-> Finance_Dashboard_Audit_All_*.csv nach SharePoint laden
-> Finance_Dashboard_Nachweis_*.xlsx erzeugen und hochladen
```

Wichtig:

Die vielen `Finance_Dashboard_Nachweis_*.xlsx` Dateien sind nur fuer Finance/Andreas zum Pruefen und Herunterladen. Sie werden wegen der grossen Datenmenge pro TSC/Land bzw. in Teilen erzeugt.

Das Dashboard, das `Finance Pruefbuch` und der neue `Finance Pivot` lesen diese Nachweis-Excel-Dateien nicht direkt.

## 9. Rolle der wichtigsten Dateien

| Datei | Bedeutung |
| --- | --- |
| `Sales_TRFR_2026-04-16.xlsx` | Roh-/Standortdatei aus Land oder Quellsystem |
| `Sales_ProcessedMergeInput_TRFR_2026-06-17.csv` | aufbereitete Audit-/Merge-Datei nach Mapping und Transformation |
| `Sales_All_2026-06-18.xlsx` | zentraler Excel-Export/Nachweis aller Standorte |
| `Finance_Dashboard_Audit_All_<Datum>.csv` | zentrale maschinenlesbare Detaildatei, Fallback-Quelle fuer Dashboard/Pruefbuch |
| `Finance_Dashboard_Nachweis_<TSC>_<Land>_<Datum>.xlsx` | Excel-Nachweis mit Detailblaettern und Formel-Summaries fuer Finance; bei grossen Datenmengen mehrere Dateien/Teile |

## 10. Merksatz

```text
Die Standort-CSV sind die operative Dashboard-Quelle.
Das Sales_All-Excel ist der zentrale Finance-Nachweis.
Die Finance_Dashboard_Audit_All-CSV ist die zentrale maschinenlesbare Detailquelle und Fallback-Quelle.
Die Finance_Dashboard_Nachweis-Excel sind nur Pruef- und Download-Dateien fuer Finance.
Das Finance Pruefbuch macht die Dashboard-Logik zeilenweise in Excel-Form sichtbar.
Der Finance Pivot macht Andreas' Excel-Pivot als Dashboard-Reiter nachpruefbar.
```
