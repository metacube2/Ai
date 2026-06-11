# Finance Entscheide Soll/Ist 2025

Stand: 2026-05-20

## Nachtrag 2026-05-20

Diese Kurzfassung bleibt gueltig fuer die Finance-Fachentscheide. Fuer den aktuellen Cockpit-/Anwenderstand gelten zusaetzlich:

- `Management Analyse` fuehrt jetzt mit dem Reiter `Finance Summary`.
- `Finance Summary` verwendet dieselbe `FinanceRuleEngine` wie das zentrale Excel.
- Filter fuer Jahr, Land und Waehrung wirken auf das Finance-Endergebnis.
- DE 2026 wird wegen DE/Alphaplan-2025-Zwang als leerer Zustand mit Hinweis dargestellt.
- Anwenderdoku: `docs/FINANCE_COCKPIT_ANLEITUNG_FINANZ_2026-05-20.docx`.
- Dokumentenstatus: `docs/MD_DOKUMENTENSTATUS_2026-05-20.md`.

Dieses Dokument extrahiert die erkennbaren Fragen und Entscheide aus der Abstimmung. Es dient als Arbeitsgrundlage fuer die Umsetzung im Finance-Abgleich und fuer die naechste Klaerung mit Andreas/Finance.

## Entscheide

| Thema | Frage | Entscheid |
| --- | --- | --- |
| Fuehrende Waehrung je Land | Welche Waehrung ist im Landessystem je Land fuehrend: Belegwaehrung, Hauswaehrung oder etwas anderes? | Immer Hauswaehrung. Rechnungen werden in der Hauswaehrung ausgewertet. |
| CHF-Umrechnung | Mit welchem Kurs wird nach CHF umgerechnet: Monatskurs, Tageskurs, Jahresdurchschnitt oder SNB-Tageskurs? | Budgetkurse verwenden. Keine SNB-Tageskurse fuer den Standardabgleich. |
| Aggregation | Wird zuerst pro Beleg/Position summiert und danach umgerechnet, oder wird jede Zeile einzeln umgerechnet und danach summiert? | Pro Artikel bzw. Belegposition rechnen. |
| Indien | In Indien kommen CHF, EUR, GBP, INR, JPY und USD vor. Muss vorher nach CHF umgerechnet oder nach Waehrung getrennt werden? | Indien immer in indischen Rupien auswerten. Fuehrend ist INR. |
| Italien / Intercompany | Soll IT mit Intercompany-Abzug gerechnet werden? Falls ja, nach welchen Kunden/Kriterien? | Hauswaehrung verwenden. Intercompany wird separat abgegrenzt und ausgewiesen. |
| Wertbasis | Welche Basis soll fuer Net Sales Actuals je Land verwendet werden? | Nettofakturawert. |
| Jahresabgrenzung | Fuer das Jahr 2025: Nach welchem Datum wird abgegrenzt? | Buchungsdatum. |
| Gutschriften / Storno | Wie werden Gutschriften und Storno behandelt? | Gutschriften separat ausweisen. Sie haben eigene Rechnungsnummern bzw. Rechnungspositionen. Behandlung immer ueber Artikelnummer/Positionslogik, da alles andere zu komplex wird. |
| Intercompany / 2nd Party | Wie werden Intercompany-Kunden abgegrenzt? | Im zweiten Schritt als neues Auswahlfeld fuer Intercompany bzw. 2nd-party-Kunde. Regeln einmalig hinterlegen, weil sie sich kaum aendern. |

## Intercompany-Regeln

Intercompany bzw. 2nd-party soll ueber stabile Kundenmarker erkannt werden.

Aktuell erkennbare Marker:

- `MAGNETS SENSE`
- `MAGNETIC SENSE`
- `TRAFAG`
- `GESELLSCHAFT FUER SENSORIK`
- `GESELLSCHAFT FUR SENSORIK`

Bewertung:

- Treffer auf diese Marker gelten als Intercompany bzw. 2nd-party.
- Alle anderen Kunden gelten standardmaessig als 3rd-party.
- Weitere Uebersetzungen, lokale Schreibweisen oder Kundennummern muessen bei Bedarf ergaenzt werden.

## Umsetzungsfolge

1. Standard-Ist je Land in Hauswaehrung und auf Basis Nettofakturawert berechnen.
2. Jahresfilter 2025 ueber Buchungsdatum anwenden.
3. Werte pro Artikel bzw. Belegposition berechnen und danach summieren.
4. Gutschriften separat sichtbar machen, aber positionsbasiert behandeln.
5. Intercompany/2nd-party als eigenes Auswahlfeld bzw. eigene Sicht ergaenzen.
6. CHF-Sicht nur mit Budgetkursen als separate Kontrollsicht aufbauen.

## Offene Punkte

| Punkt | Klaerung |
| --- | --- |
| Intercompany-Kundenliste | Finale Liste der Kundennummern, Namen und lokalen Schreibweisen je Land bestaetigen. |
| Italien | Abgrenzung mit/ohne Intercompany fachlich gegen Rhino/check.xlsx pruefen. |
| Budgetkurse | Quelle und Gueltigkeit der Budgetkurse je Jahr festlegen. |
| Gutschriften | Sicherstellen, dass alle Quellsysteme Gutschriften mit eigener Rechnungsnummer/Position liefern oder sauber markierbar sind. |
