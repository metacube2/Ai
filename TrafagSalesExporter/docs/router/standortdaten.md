# Unterrouter Standortdaten

Zurueck: `router.md`. Stand: 2026-08-17.

Exporte und Importe je Land, Feldluecken, Quellsysteme, Ansprechpartner.

## Die Regel, die diesem Ast vorangeht

**Fehlt ein Feld, liest zuerst unsere eigene Export-SQL es nicht.** Die Queries in
`AlphaplanExportPackage/` (DE) und `SageSpainExportPackage/` (ES) sind unsere. Erst pruefen,
dann fragen — das ist zweimal in einer Woche schiefgegangen. Details und die zweite Regel
(pruefen, ob die Information anderswo schon vorliegt) stehen in
`docs/FINANCE_FELDLUECKEN.md` Abschnitt 1.

## Zuerst laden

| Bedarf | Datei |
| --- | --- |
| Manual Import UK/ES/DE, Dedupe, Betriebsfallen | `docs/rag/MANUAL_IMPORT.md` |
| **Was fehlt je Standort und wer besitzt es** | `docs/FINANCE_FELDLUECKEN.md` |

## Je Land

| Land | Datei |
| --- | --- |
| **Spanien** — Sage, Export, rclone, Referenzwert | `docs/STANDORT_ES_SAGE.md` |
| Spanien, Buchungsdatum `PostingDate` im Detail | `docs/FINANCE_ES_BUCHUNGSDATUM_2026-08-03.md` |
| **Deutschland** — Alphaplan, CSV-Paar, ZIP-Import | `docs/STANDORT_DE_ALPHAPLAN.md` |
| **Indien** — Sales Type statt Preferred Vendor, Eigenfertigung | `docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md` |
| **Italien** — Pruefpfad Net Sales | `docs/FINANCE_IT_VORGEHEN_2026-05-18.md` |
| **UK** — Quellkorrektur | `docs/FINANCE_UK_QUELLE_KORREKTUR_2026-05-18.md` |
| **CH/AT und die B1-Laender** — Quellsysteme, Felder, Credentials, Firewall | `docs/QUELLSYSTEME_SAP_B1.md` |

## Weiteres

| Bedarf | Datei |
| --- | --- |
| Manual-Import- und Delta-Details, historisch | `docs/MANUAL_IMPORT_DELTA_STAND_2026-05-21.md` |
| **Ansprechpartner und Mailadressen je Standort** | `docs/ANSPRECHPARTNER.md` |
| Abfrage auf einem System, das nur der Server erreicht | `docs/router/plattform.md`, Abschnitt Serveranalyse |

## Fallen in diesem Ast

- **UK-Reimport nur ohne Jahresfilter starten.** Mit Jahresvorgabe vernichtet der Import
  das jeweils andere Jahr, weil vorher alle Zeilen des Standorts geloescht werden. Siehe
  `docs/rag/MANUAL_IMPORT.md`.
- **Spanien hat keine verdrahtete Spaltenzuordnung im Seed**, anders als UK und DE. Neue
  Spalten muessen in den Einstellungen zugeordnet werden.
- **Der Standortimport ersetzt den Bestand.** Ein einzelnes Delta darf nie isoliert als
  Vollbestand importiert werden.
- **Die Export-Query steht je Standort mehrfach** (ES zweimal plus Spiegelung, DE zweimal).
  Aenderungen immer ueberall nachziehen.

## Querverweise in Nachbaraeste

- Was mit den Zahlen im Dashboard passiert: `docs/router/finance.md`
- SAP-Seite und ABAP-Reports: `docs/router/sap.md`
