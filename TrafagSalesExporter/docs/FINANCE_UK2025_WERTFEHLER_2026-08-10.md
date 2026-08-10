# UK 2025: die Zeilen sind da, die Betraege sind zu klein — Faktor 9

Stand: 2026-08-10. Diagnose **vor** dem Fix festgehalten.

## Kurzfassung

Der UK-Backfill vom 2026-07-28 (`docs/FINANCE_BACKFILL_UK_ES_2026-07-28.md`) hat die
fehlenden 2025er Zeilen geliefert und wurde als erledigt verbucht. Die **Betraege** sind
aber Stueckpreise statt Zeilenwerte:

| UK 2025 | GBP |
| --- | ---: |
| Sollwert Finance (`FINANCE_BERECHNUNGSFORMELN_LAENDER_2026-05-19.md`, Spalte „Soll/Referenz 2025") | **3'538'972** |
| Ist im Dashboard (Export 2026-08-09) | **394'439** |
| Anteil | **11 %** |
| Rekonstruktion Stueckpreis x Menge | **3'529'862** = 99.74 % des Solls |

Betroffen sind **1'241 von 1'867 Zeilen** (66.5 %) — alle mit Menge > 1. Die 626 Zeilen mit
Menge 1 sind zufaellig richtig, weil dort Stueckpreis = Zeilenwert.

Damit ist der Themenlisten-Punkt „Daten TR UK fuer 2025 fehlen" **nicht erledigt**. Richtig
ist: Zeilen vollstaendig, Betraege falsch.

## Wie es entstanden ist

Vier Schritte, jeder einzeln nachvollziehbar:

1. **Die Quelldatei enthielt bereits Stueckpreise.** `Sales_TRUK_2026-05-11.xlsx` ist ein
   App-Export vom 2026-05-11 — also **vor** der Umstellung des UK-Mappings auf
   `SageNetSales` (dokumentiert 2026-05-19). Gemessen an der Originaldatei:

   | Mengenband | Zeilen | Ø Zeilenwert | Ø Wert/Menge |
   | --- | ---: | ---: | ---: |
   | = 1 | 627 | 225.38 | 225.38 |
   | 2 - 10 | 873 | 236.30 | 81.54 |
   | 11 - 100 | 355 | 122.07 | 4.80 |
   | > 100 | 26 | 179.57 | 0.90 |

   Der Zeilenwert waechst **nicht** mit der Menge. Summe der Spalte: `395'605.82`.

2. **`BuildUkBaseFile` hat ein zweites Mal geteilt.** Das Werkzeug rechnete
   `Zeilenwert / Menge`, um die Multiplikation des Mappings vorzukompensieren. Die Spalte war
   aber schon ein Stueckpreis — die Kompensation war doppelt.

3. **Der Import multiplizierte zurueck** (`ManualExcelImportService.cs:741`,
   `netLineAmount = amount * quantity`) und stellte damit exakt die Dateiwerte wieder her.

4. **Die Kontrollrechnung konnte das nicht merken.** Sie verglich
   `Summe original 395'605.82` gegen `Summe nach Reimport-Simulation 395'605.82` — sie belegt
   „wir reproduzieren die Datei", nicht „die Datei ist richtig". Genau diese Luecke hat den
   Fehler durchgelassen.

## Drei unabhaengige Belege

Damit die Aussage nicht an einer einzigen Rechnung haengt:

1. **Gegen den Sollwert:** Stueckpreis x Menge = `3'529'862` = 99.74 % von `3'538'972`. Der
   Sollwert ist von Finance und stammt nicht aus dieser Analyse.
2. **Gegen die Kostenbasis:** UK 2025 zeigt eine Marge von **−502.7 %** (Umsatz `395'468`
   gegen Standardkosten x Menge `2'383'327`), 65.7 % der Zeilen unter Kosten. Alle anderen
   Standorte 2025: TRCH +16.3 %, TRDE +32.9 %, TRIT +33.8 %, TRES +37.9 %, TRAT +38.6 %,
   TRUS +44.6 %, TRFR +47.0 %, TRIN +49.0 %. Der Ausschlag ist UK-2025-spezifisch.
3. **Gegen das eigene Folgejahr:** derselbe Artikel bei Menge 1 kostet in beiden Jahren
   praktisch gleich (Median-Faktor 1.09x, z. B. `53061` 237.44 -> 238.27, `41179`
   428.39 -> 432.05). Das Preisniveau ist also richtig, es fehlt nur die Multiplikation.
   Und UK **2026** skaliert korrekt mit der Menge (250 bei Menge 1, 27'936 bei Menge > 100),
   weil diese Zeilen nach der Mapping-Umstellung importiert wurden.

## Warum es dringend ist

Der **Soll/Ist-Vergleich laeuft auf 2025** — `FinanceReferences` enthaelt ausschliesslich
2025er Werte (gemessen 2026-08-07,
`docs/FINANCE_INDIKATOREN_PRUEFUNG_2026-08-07.md`). UK wird dort also mit 11 % seines
Sollwerts verglichen und muss eine Abweichung von rund −89 % zeigen.

## Der Fix

`TRUK_2025.xlsx` neu aus `Sales_TRUK_2026-05-11.xlsx` bauen, **ohne** die Division —
`Sales Price/Value` unveraendert durchreichen. `SageNetSales` rechnet dann Stueckpreis x
Menge und trifft den Zeilenwert. Weg C ist seit `d77b5b4` deployed, die 2025er Jahresdatei
wird also weiterhin gelesen.

**Abnahmekriterium, unabhaengig von dieser Analyse:**

- UK 2025 Ist ≈ `3'538'972` GBP (Toleranz: die 14 bekannten Quelldubletten, `1'166.66` x
  Menge, sowie Rundung)
- Marge UK 2025 dreht von −502.7 % auf einen plausiblen positiven Wert im Bereich der
  anderen Standorte
- Zeilenzahl bleibt bei 1'867 (die Korrektur aendert Werte, nicht Zeilen)

## Zweiter Befund: die Legendenzeile haengt dauerhaft in den Produktivdaten

Der Datensatz mit `TSC = "Subsidiary abbreviation / company identifier"` (Land England,
Menge 0, Wert 0) steht noch im Export vom 2026-08-09 — er stammt aus dem Backfill vom
2026-07-28 (dort als eigener Fehler festgehalten).

Der Codefix `9c0451e` verhindert nur den **Neuimport**. Entfernen wird er die Zeile nie:
Manual-Importe ersetzen den Bestand **je TSC**, und dieser Satz traegt eine TSC, die kein
Import je anfasst. Er ist damit verwaist und taucht in jeder TSC-Gruppierung als eigener
„Standort" auf. Wertmaessig 0.00, aber er verfaelscht jede Gruppierung.

Aufraeumen erfordert ein gezieltes `DELETE` auf `CentralSalesRecords` — bewusst als eigener
Schritt, nicht im Rahmen des Wertfixes.

## Reproduktion

Alle Zahlen dieses Dokuments stammen aus zwei Dateien, ohne Datenbankzugriff:

- `all.xlsx`, Blatt `Sales` (= `xl/worksheets/sheet3.xml`), 96'233 Datenzeilen, Export vom
  2026-08-09 21:51. Spalten: `B` TSC, `F` Material, `P` Quantity, `X` Standard cost,
  `AA` Sales Price/Value, `AQ` Finance | Year, `AT` Finance | Net Sales Actual.
  Kopfzeile **und** Legendenzeile 2 nicht mitzaehlen.
- `bin/Debug/net8.0/output/Sales_TRUK_2026-05-11.xlsx` als Originalquelle des Backfills.

Gegenprobe zur Methode: dieselbe Multiplikationsprobe auf UK **2026** ergibt einen
Faktor 89.8x — Unsinn, und genau deshalb der Beleg, dass dort schon Zeilenwerte stehen.
