# Finance Gruppenmarge

Stand: 2026-06-16

## Zweck

Die neue Sicht `Management Analyse > Experten > Gruppenmarge` ist eine fachliche Pruefsicht fuer Gruppenmarge je Land, Sparte und Detailzeile. Sie ist noch kein final freigegebener Finance-Abschlusswert.

Ausloeser war `kosten.xlsx`, Blatt `Tabelle2`: Dort wurde die Frage erkennbar, ob die Marge nicht mit den lokalen Verkaufszeilenkosten, sondern aus Gruppensicht mit der richtigen Lieferanten-/Standardkostenbasis gerechnet werden muss.

## Fachliche Arbeitshypothese

- Externer Lieferant: Kostenbasis aus der Verkaufszeile bzw. aus dem verfuegbaren Standardkostenfeld der importierten Zeile.
- Interner Trafag-Lieferant: Kostenbasis soll fachlich auf dem internen Lieferanten-Standardpreis basieren, in SAP-Kontext `MBEW-STPRS`.
- Interne Lieferkette: MVP stoppt nach einer Ebene.
- Fehlende Kosten oder unklarer Lieferant werden nicht geschaetzt.
- Bei offener Kostenbasis ist die Gruppenmarge nicht belastbar.

## Aktuelle technische Umsetzung

- Neuer Reiter `Gruppenmarge` in der Management Analyse.
- Neuer Navigationspunkt `Management Analyse > Experten > Gruppenmarge`.
- Schnelluebersicht enthaelt einen Direktbutton zur Gruppenmarge.
- Datenbasis ist die gleiche Finance-gefilterte Ergebniszeile wie `Finance Summary`: nur `Finance Include = TRUE` und nach Finance-Regeln berechneter Netto-Ist-Wert.
- Lieferantentyp wird aus `SupplierNumber`, `SupplierName`, `SupplierCountry` heuristisch erkannt.
- Kostenbasis nutzt aktuell das im zentralen Modell vorhandene `StandardCost` mit Mengenmultiplikation.
- Detailtabelle ist auf 1000 Zeilen begrenzt; Summary-, Laender- und Spartenaggregation laufen ueber alle gefilterten Gruppenmargen-Zeilen.

## Anzeige- und Validierungsregeln

- `Umsatz` bleibt immer sichtbar.
- `Bekannte Kostenbasis` zeigt die aktuell berechenbare Kostenbasis.
- `Marge` und `%` werden nur angezeigt, wenn die Kostenbasis fuer die Zeile bzw. Gruppe vollstaendig geklaert ist.
- Wenn mindestens eine Zeile in Land/Sparte `Standardpreis fehlt` oder `Lieferant unklar` hat, zeigen `Marge` und `%` in der Aggregation `-`.
- Detailzeilen mit Status ungleich `OK` zeigen ebenfalls keine Marge.
- Spalte `Offen` zaehlt offene Kostenbasis, also `Standardpreis fehlt` und `Lieferant unklar`.

## Aktueller Datenbefund

Lokale Pruefung der zentralen Datenbank am 2026-06-16:

- AT/TRAT: fuer 2025 sind `StandardCost` und Supplier-Felder in den geprueften zentralen Zeilen leer bzw. 0. Gruppenmarge ist damit offen.
- CH/TRCH: fuer 2025 sind `StandardCost` und Supplier-Felder in den geprueften zentralen Zeilen leer bzw. 0. Gruppenmarge ist damit offen.
- IN/TRIN und IT/TRIT haben teilweise Standardkosten und interne Lieferanten, aber weiterhin offene Zeilen.
- ES/FR/UK/US zeigen bekannte Kostenbasis aus vorhandenen Standardkosten, haben aber ebenfalls offene Zeilen je nach Datenbestand.

Konsequenz: Die Sicht darf aktuell nicht als finale Marge interpretiert werden. Sie zeigt, welche Laender/Sparten eine belastbare Kostenbasis haben und wo Daten oder fachliche Regeln fehlen.

## Offene Entscheidungen

Das Multiple-Choice-Formular `docs/FINANCE_GRUPPENMARGE_MULTIPLE_CHOICE_2026-06-16.docx` soll von Andreas/Finance ausgefuellt werden. Es klaert insbesondere:

- Erkennung interner Lieferanten.
- Kostenbasis fuer externe Lieferanten.
- Kostenbasis fuer interne Lieferanten.
- Umgang mit mehrstufigen internen Lieferketten.
- Waehrung und Umrechnung fuer Gruppenmarge.
- Verhalten bei fehlenden Standardpreisen.
- Freigabeumfang des MVP.

## Naechste technische Schritte nach Fachfreigabe

- Falls externe Lieferanten eine andere Kostenquelle als `StandardCost` brauchen, neues Feld oder Mapping in `CentralSalesRecords` ergaenzen.
- Falls interne Lieferanten immer ueber SAP `MBEW-STPRS` laufen muessen, separate SAP-Kostenquelle bzw. Mapping anbinden.
- Lieferantenerkennung nicht nur heuristisch, sondern regel-/stammdatenbasiert pflegen.
- Tests fuer offene Kostenbasis und Aggregationsanzeige ergaenzen, sobald die finale Fachregel fixiert ist.

