# Finance Dashboard Todo

Stand: 2026-05-15

Ziel: Aufbau eines modernen, uebersichtlichen Intranet-Dashboards fuer das Group Sales Reporting, zugaenglich fuer alle freigegebenen Benutzer.

## Todo

| Prio | Aufgabe | Status |
| --- | --- | --- |
| 1 | Fuehrendes CFO-Dokument verwenden: `FINANCE_CHEF_SUMMARY_2026-05-15.docx` | Offen |
| 1 | Offene Finance-Entscheide mit Andreas/Finance durchgehen | Offen |
| 1 | Italien-Abweichung klaeren: Berechnungsart, Deduplizierung, Intercompany | Offen |
| 1 | Italien IC-Diagnose besprechen: Trafag, Magnetic Sense/Magnets Sense und Gesellschaft fuer Sensorik erklaeren einen grossen Teil, aber nicht die ganze Abweichung | Offen |
| 1 | Deutschland: finalen Jahresfile 2025 beschaffen | Offen |
| 2 | UK/England: Jahresvollstaendigkeit und Restdifferenz pruefen | Offen |
| 2 | CH/AT: Sollzuordnung und Trennung final bestaetigen | Offen |
| 2 | Spanien und Oesterreich: kleinere Differenzen klaeren | Offen |
| 2 | Intercompany-/2nd-party-Kundenliste final definieren | Offen |
| 3 | Budgetkurse je Jahr als Quelle festlegen | Offen |
| 3 | Dashboard-Sicht fuer CFO: nur Laender mit Abweichung und Handlungsbedarf anzeigen | In Arbeit |
| 3 | Detailansicht je Land mit Berechnungsart und Pruefspur behalten | In Arbeit |
| 3 | Freigabe-/Berechtigungskonzept fuer Intranet-Dashboard klaeren | Offen |

## Naechster Termin

Ziel im Termin:
- Deutschland und Spanien muss finales Excel schicken  (Rohali 2 mal nachgehakt warte auf finales File)
- Grundlogik bestaetigen: Hauswaehrung, Nettofakturawert, Buchungsdatum, Berechnung pro Belegposition.
- Offene Laenderabweichungen priorisieren.
- Pro Land festlegen, welche Datenquelle und Berechnungslogik final gilt.

## IT / Intercompany Diagnose

Aktuelle Diagnose fuer Italien:

| Kennzahl | Wert |
| --- | ---: |
| IT Ist vor IC-Abzug | 14.704.336,29 EUR |
| Rhino/check.xlsx Soll | 7.669.840,00 EUR |
| Abweichung vor IC | +7.034.496,29 EUR |
| Erkannter IC-/2nd-party-Abzug | 4.397.746,90 EUR |
| IT Ist exkl. IC | 10.306.589,39 EUR |
| Restabweichung nach IC | +2.636.749,39 EUR |

Verwendete IC-/2nd-party-Marker:

- `TRAFAG`
- `MAGNETIC SENSE`
- `MAGNETS SENSE`
- `GESELLSCHAFT FUER SENSORIK`
- `GESELLSCHAFT FUR SENSORIK`

Bewertung:

- Intercompany/2nd-party erklaert einen grossen Teil der IT-Abweichung.
- Die Summe passt dadurch deutlich besser, aber noch nicht vollstaendig.
- Restabweichung weiter pruefen: Summenlogik, Beleg/Position-Deduplizierung, Gutschriften/Storno und weitere lokale IC-Kunden oder Schreibweisen.
