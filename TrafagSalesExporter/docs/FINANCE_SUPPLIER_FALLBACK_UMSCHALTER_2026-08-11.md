# Supplier-Fallback CH-Werkstamm: neuer Standard mit Alt-Umschalter

Stand: 2026-08-11

## Fachentscheid

Wenn bei einem Fremdstandort alle drei Supplier-Felder leer sind und auch kein
Sales Type (`FFM`, `CM`, `LRD`) entscheidet, wird die normalisierte Trafag-
Materialnummer gegen den Artikelstamm der Trafag AG Schweiz geprueft.

- Treffer in `MARC`, Werk `1100`: `SupplierType = Intern`, liefernde Gesellschaft
  `TR_AG`.
- Sicherer Nichttreffer bei geladenem MARC-Cache: `SupplierType = Lokal`; verwendet
  werden die Standardkosten der jeweiligen lokalen Gesellschaft.
- Explizit vorhandener Supplier: hat immer Vorrang.
- Ohne belastbaren Vergleich, etwa bei fehlender Materialnummer oder leerem Cache:
  `Unklar`.
- CH/AT selbst bleiben von dieser Fremdstandortregel unberuehrt; fuer sie gilt die
  vorhandene TSC-Regel.

`MARC/WERKS 1100` wurde bewusst statt der mandantenweiten `MARA` gewaehlt: MARC
belegt, dass das Material im CH-Werkstamm gefuehrt wird. Der Treffer ist der von
Ingo gewuenschte Konzern-Stammdaten-Fallback, aber kein Produktions- oder
Warenbewegungsnachweis.

## Gemessener Unterschied

Read-only gemessen auf der produktiven Datenbank mit 96.298 Sales-Zeilen und dem
SAP-Livestamm vom 2026-08-11:

| Kennzahl | Alt: MBEW 1100 | Neu: MARC Werk 1100 | Differenz |
| --- | ---: | ---: | ---: |
| CH-Materialien | 63.550 | 66.047 | +2.497 |
| interne Treffer in 22.840 relevanten Fallback-Zeilen | 10.097 | 10.817 | +720 |
| betroffene unterschiedliche Verkaufsmaterialien | - | 392 | +392 |
| entfallende bisherige Treffer | - | - | 0 |

Die 720 zusaetzlichen Zeilen entsprechen 3,2 % der Fallback-Kandidaten, 0,7 %
aller Sales-Zeilen und +7,1 % gegenueber den bisherigen Materialtreffern. Davon
entfallen 674 Zeilen auf TRIT, 28 auf TRFR, 10 auf TRUS und 8 auf TRDE.

## Umsetzung

Unter `Admin Bereich > Settings > Export Einstellungen` gibt es den Schalter
`Supplier-Fallback ohne Lieferantenangabe`:

- `Neu: CH-Werkstamm (MARC 1100)` ist der Default.
- `Alt: CH-Kostentabelle (MBEW 1100)` stellt das bisherige Verhalten wieder her.

Der Wert wird dauerhaft in `ExportSettings.SupplierFallbackMode` gespeichert und
im Konfigurationsexport/-import mitgefuehrt. Dashboard, Finance-Pruefbuch, zentrale
Excel und Nachweis-Excel verwenden denselben Modus.

Die neue Tabelle `GroupMaterialMasters` trennt den Werkstamm bewusst von
`GroupStandardCosts`: Ein MARC-Treffer darf intern klassifizieren, erzeugt aber
keine erfundene Kostenbasis. Echte Konzernkosten kommen weiterhin ausschliesslich
aus MBEW/`GroupStandardCosts`. Fehlen sie, greift die bestehende Kostenregelkette.

Beim CH/AT-SAP-Export liest `SapGatewayPlantMaterialReader` genau einmal
`MARCSet` mit `Matnr,Werks`, filtert Werk 1100 clientseitig und ersetzt den Cache
atomar. Liefert SAP keine Daten oder tritt ein Fehler auf, bleibt der bisherige
Cache erhalten. Ist nach einer Migration noch gar kein MARC-Cache vorhanden,
verwendet der neue Modus voruebergehend automatisch den alten MBEW-Fallback.

### Nachtrag aus dem Andreas-Meeting

Andreas bestaetigte im Meeting vom 2026-08-11, Transkript 06:31-07:16, auch den
zweiten Zweig: Ist der Artikel nicht im CH-Stamm enthalten, sind die Standardkosten
der jeweiligen Gesellschaft zu verwenden. Nach dem Baseline-Commit `369d675` wurde
diese Erweiterung vom Nutzer einzeln freigegeben und lokal umgesetzt.

Produktive read-only Messung: Von `22.950` relevanten Zeilen treffen `10.817` den
CH-Werkstamm. `12.023` belastbare Nichttreffer werden neu `Lokal`; `6.749` davon
haben positive lokale Standardkosten, `5.274` noch nicht. Weitere `110` Zeilen
haben keinen pruefbaren Materialschluessel und bleiben `Unklar`.

Detailnachweis:
`docs/FINANCE_ANDREAS_BESCHLUSS_LOKALE_STANDARDKOSTEN_2026-08-11.md`.

## Nachweis

- SAP-Live-Dry-Run: 66.047 MARC-1100-Materialien; alle 63.550 aktuellen
  MBEW-1100-Materialien enthalten; 0 Verlustfaelle.
- Gezielt: 87/87 Supplier-, Rechner-, Reader-, Schema- und
  Konfigurationstransfer-Tests gruen.
- Gesamt: 471/471 Tests gruen.
- Die vier neuen UI-Texte sind in ES, IT, HI, SQ, TR und Klingon ergaenzt.

Reproduzierbare Werkzeuge:

- `.tmp_tools/CompareSupplierFallback`: Alt/Neu-Differenz read-only messen.
- `.tmp_tools/RefreshChPlantMaterialMaster`: SAP-Bestand validieren; ohne
  `--apply` read-only, mit `--apply` atomarer Cache-Backfill und Auswahl des neuen
  Modus.

Aktualisierter Finance-Bericht:
`docs/Supplier_Laenderstatus_CH_AT_Pruefung_mit_Fallback_2026-08-11.docx`.
Die vorherige Datei war beim Regenerieren in Word geöffnet und wurde deshalb nicht
überschrieben; beide Fassungen bleiben erhalten.

## Deploymentstatus

Produktiv deployed am 2026-08-11 nach ausdruecklicher Freigabe des Nutzers trotz
des bekannten Einkaufsblockers. Vorher wurden `471/471` Release-Tests ausgefuehrt
und das konsistente Backup
`trafag_exporter.db.before-all-current-20260811-145332.bak` angelegt.

Produktiv read-only bestaetigt:

- `SupplierFallbackMode = ChPlantMaster`;
- `66.049` unterschiedliche MARC-Materialien fuer Werk 1100;
- alle `63.550` bisherigen MBEW-1100-Schluessel enthalten;
- Cachezahl vor und nach dem finalen App-Neustart identisch;
- `96.298` Sales-Zeilen unveraendert;
- Server-DLL und lokaler Release-Build bitgleich.

Vollstaendiger technischer Deploynachweis:
`docs/DEPLOY_GESAMTSTAND_2026-08-11.md`.

Der Nachtrag `Lokal bei MARC-Nichttreffer` ist erst nach diesem Deploy entstanden.
Er ist lokal implementiert, getestet und separat committed, aber noch nicht produktiv
deployed. Der Produktivstand verwendet fuer diese Nichttreffer weiterhin `Unklar`.
