# Andreas-Beschluss: lokale Standardkosten bei CH-Stamm-Nichttreffer

Stand: 2026-08-11
Status: vom Nutzer bestaetigt, im Code umgesetzt, getestet und separat committed; noch nicht deployed

## Quelle

Whisper-Transkript des Meetings mit Andreas vom 2026-08-11, Aufnahme ab 13:55 Uhr.
Relevante Passage: 06:31 bis 07:16.

Sinngemaesser Beschluss:

1. Ist der Artikel im Artikel-/Werkstamm der Trafag AG Schweiz enthalten, wird die
   Zeile als konzernintern beziehungsweise von `TR_AG` beliefert behandelt.
2. Ist der Artikel dort nicht enthalten, werden die Standardkosten der jeweiligen
   lokalen Gesellschaft verwendet.
3. Dieser Workaround soll fuer die Gesellschaften standardisiert werden.

Der Nutzer hat diese konkrete Aenderung nach dem Baseline-Commit `369d675`
ausdruecklich bestaetigt.

## Praezise technische Regel

Die bestehende Prioritaet bleibt erhalten:

1. CH/AT-TSC-Regel;
2. explizit gepflegter Supplier;
3. gepflegter Sales Type `FFM`, `CM` oder `LRD`;
4. Materialvergleich gegen `MARC`, Werk `1100`.

Nur in Stufe 4 gilt neu:

- MARC-Treffer: `SupplierType = Intern`, liefernde Gesellschaft `TR_AG`;
- sicherer MARC-Nichttreffer: `SupplierType = Lokal`, Kostenquelle
  `Standardkosten der lokalen Gesellschaft`;
- lokaler Standardpreis groesser null: Kostenbasis und Marge sind berechenbar;
- lokaler Standardpreis null: Status `Standardpreis fehlt`;
- fehlender Materialschluessel, fehlende TSC oder leerer MARC-Cache: weiterhin
  `Lieferant unklar`, weil kein belastbarer Nichttreffer vorliegt.

`Lokal` wurde bewusst nicht `Extern` genannt. Aus dem fehlenden CH-Stammtreffer
laesst sich ableiten, dass lokale Kosten gelten, aber nicht, ob der Artikel lokal
eingekauft oder lokal gefertigt wurde.

Der umschaltbare Alt-Modus `GroupStandardCosts` bleibt unveraendert. Er kennt die
neue lokale Nichttrefferregel nicht und bildet weiterhin den historischen Zustand ab.

## Produktive Auswirkungsmessung, read-only

Datenbasis: produktive SQLite-Datenbank, `96'298` Sales-Zeilen und `66'049`
MARC-Materialien fuer Werk 1100. Gemessen wurden ausschliesslich Fremdstandortzeilen
ohne Supplier-Felder und ohne erkannten Sales Type.

| TSC | Kandidaten | CH-intern | Lokal | Lokal mit Standardkosten | Lokal ohne Standardkosten | Material fehlt |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| TRDE | 7'332 | 175 | 7'157 | 4'905 | 2'252 | 0 |
| TRES | 5'712 | 3'282 | 2'430 | 1'372 | 1'058 | 0 |
| TRFR | 2'463 | 1'176 | 1'278 | 75 | 1'203 | 9 |
| TRIN | 142 | 46 | 68 | 66 | 2 | 28 |
| TRIT | 5'747 | 4'795 | 945 | 194 | 751 | 7 |
| TRUS | 1'554 | 1'343 | 145 | 137 | 8 | 66 |
| **Gesamt** | **22'950** | **10'817** | **12'023** | **6'749** | **5'274** | **110** |

Damit wechseln im neuen Code `12'023` bisher unklare Zeilen zur fachlichen
Kategorie `Lokal`. Bei `6'749` davon ist ein positiver lokaler Standardpreis
vorhanden. Diese Zahl belegt die Kostenverfuegbarkeit; Waehrung, Umsatzwert und
weitere Statusregeln koennen eine einzelne Zeile weiterhin offen halten.

## Umsetzung

- `GroupMarginSupplierClassifier`: neue Kategorie `Lokal` nur fuer belastbare
  MARC-Nichttreffer im neuen Modus.
- `GroupMarginCalculator`: Kostenquelle
  `Standardkosten der lokalen Gesellschaft`; lokaler Standardpreis bleibt die
  letzte Kostenregel.
- Management-Cockpit: separate Lokal-Zahl in Zusammenfassung und Landestabelle.
- Finance-Training und Excel-Hilfe: neue Regel erklaert.
- Cockpit und Excel verwenden weiterhin dieselbe zentrale Rechenklasse.

## Tests

Gezielter Release-Lauf:

- Klassifikation;
- Kostenbasis und Status;
- Schutz bei fehlendem Material beziehungsweise leerem Cache;
- unveraenderter Alt-Modus;
- Gleichheit zwischen Cockpit und Excel;
- Management-Zaehlung der lokalen Zeilen.

Ergebnis: `87/87` gezielte Margentests und der separate Lokalisierungstest gruen.
Anschliessend vollstaendige Release-Suite: `478/478` Tests gruen. Bekannte
bestehende Warnungen, insbesondere `NU1903` fuer
`Microsoft.AspNetCore.Authentication.Negotiate 8.0.24`, bleiben bestehen.

## Abschlussstand dieses Aenderungspakets

- separater Commit nach der produktiven Baseline `369d675`;
- kein Produktivdeploy;
- keine Aenderung der produktiven Datenbank;
- keine Ableitung weiterer Transkriptpunkte ohne einzelne Nutzerbestaetigung.
