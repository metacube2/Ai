# DE-Kundenfelder produktiv nachziehen

Stand 09.09.2026. Fachlicher Nachweis: `docs/BAHNMARKT_DE_ANBINDUNG_2026-09-08.md`
Abschnitt 9.

Zieht die fachliche Kundennummer, den Namen, das Land und die Branche an die deutschen
Verkaufszeilen nach. Grundlage ist Rohails Rechnungsliste, die zu jeder Rechnungsnummer die
fachliche `Adressnr._R` fuehrt.

## Was angefasst wird, und was nicht

Angefasst werden **ausschliesslich** Zeilen ohne fachliche Nummer, also leere oder mit
`ALPHAPLAN-ID:` beginnende. Eine bereits belegte Nummer wird nie ueberschrieben. Daraus
folgt zweierlei:

- Die 4'549 im ersten Lauf belegten Zeilen bleiben unberuehrt.
- Die 40 Zeilen der drei Doppelnummern Sonepar, EMS und Magnetic Sense bleiben ebenfalls
  unberuehrt. Dort steht eine fachliche Entscheidung zwischen zwei Firmen aus, und die
  gehoert nicht in ein Werkzeug.

Geaendert werden vier Spalten in `CentralSalesRecords` und Eintraege in
`CustomerMarketSegments`. Keine Zeile wird geloescht, angelegt oder umsortiert, kein
Finanzfeld angefasst.

## Aufruf

```
dotnet run --project Tools/DeCustomerBackfill -c Release -- \
  <db> <rechnungen.xlsx> <kundenstamm.csv> <arbeitsordner> [--apply]
```

Ohne `--apply` wird nur gemessen, der Vorherstand gesichert und der Plan geschrieben. Erst
`--apply` schreibt, und zwar in einer Transaktion.

**Betriebsfalle aus dem Lauf vom 09.09.:** ein Nur-Lese-Zugriff scheitert ueber SMB mit
`SQLite Error 14`. Den Probelauf deshalb gegen eine konsistente lokale Kopie fahren und erst
die Anwendung gegen den Produktivpfad.

## Fuenf Sicherungen

1. **Doppellaufsperre.** Ein vorhandenes `database-applied.txt` im Arbeitsordner bricht ab.
2. **Vorherstand.** Beim ersten Lauf werden `before-rows.json` und `before-segments.json`
   geschrieben. Weicht der Live-Stand beim Anwenden davon ab, bricht der Lauf ab.
3. **Zeilenweise Sperre.** Jedes `UPDATE` verlangt die alte Kundennummer und einen leeren
   Namen. Trifft es nicht genau eine Zeile, wird die Transaktion zurueckgerollt.
4. **Kundenstamm-Pflicht.** Eine Adressnummer ohne Eintrag im Kundenstamm bricht ab, statt
   eine Zeile ohne Namen zu hinterlassen.
5. **Gegenzaehlung.** Nach dem Commit muessen Zeilenzahl und Summe `SalesPriceValue`
   unveraendert sein, sonst wirft das Werkzeug.

Bestaetigte Gegenentscheide auf ein anderes Segment als `Railway` bleiben erhalten; das
Werkzeug meldet sie, statt sie zu ueberschreiben.

## Ergebnis des Probelaufs vom 09.09.2026

Gegen eine Kopie der verifizierten Produktivdatenbank:

| Messung | Wert |
|---|---:|
| TRDE-Zeilen | 7'615 |
| mit fachlicher Nummer vorher | 4'549 |
| nachgezogen | 3'036 |
| mit fachlicher Nummer nachher | 7'585 |
| offen geblieben (Gutschriften ohne Bruecke) | 30 |
| betroffene Kundennummern | 358 |
| neue Railway-Segmente | 5, von 19 auf 24 |
| `SalesPriceValue` vorher und nachher | 7'029'335.84 |

## Danach

Die Datenbank allein reicht nicht. Das Dashboard liest
`Sales_ProcessedMergeInput_TRDE_*.csv`, deshalb muessen die veroeffentlichten Dateien im
Serverordner `output` und im SharePoint-Ordner `Import/Finance/Deutschland/AlphaplanRaw`
danach ebenfalls nachgezogen werden.
