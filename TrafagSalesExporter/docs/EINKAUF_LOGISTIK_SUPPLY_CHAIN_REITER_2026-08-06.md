# Neue Einkauf-/Logistik-Reiter

Stand: 2026-08-06

Status: lokal umgesetzt und getestet; noch nicht committed oder deployed.

## Ziel und Abgrenzung

Die Erweiterung ist rein additiv. Bestehende Seiten fuer Spend, offene
Bestellungen, Lieferanten, Spend-Aufriss und Stuecklistenanalyse werden weder
ersetzt noch in ihrer Berechnungslogik veraendert. Die neuen Seiten verwenden
einen eigenen, nur lesenden `SupplyChainAnalysisService` und eigene Routen.

## Neue Reiter

| Bereich | Reiter | Route | Datenbasis |
| --- | --- | --- | --- |
| Logistik | Materialdisposition & Fehlteile | `/logistik/materialdisposition` | ZLO03/ZMD04_CALC, MARC/MARA, EKET-Drill |
| Einkauf | Bestellbedarf & Deckung | `/einkauf/bestellbedarf` | ZMD04_CALC, EKPO, EKET, EKKO |
| Einkauf | Materialabhaengigkeit | `/einkauf/materialabhaengigkeit` | historische EKKO/EKPO-Lieferanten plus ZLO03-Verwendungsbreite |
| Logistik | Dispositionspruefung | `/logistik/dispositionspruefung` | MARC/MARA und ZMD04_CALC |
| Einkauf | Lieferperformance | `/einkauf/lieferperformance` | belastbarer EKET-Plantermin-Rueckstand; Ist-Termin bewusst als Datenluecke |

Jede Route besitzt eigene Kennzahlen, Prioritaetsbalken und Detailtabelle.
Suche, Disponent, Produktgruppe und `Nur Handlungsbedarf` werden vor Kennzahlen,
Balken und Detailzeilen angewendet. Die Detailtabelle ist auf die 1'000
wichtigsten Treffer begrenzt; Kennzahlen und Balken verwenden den gesamten
gefilterten Ergebnisumfang.

## Fachliche Regeln

### Materialdisposition

- P1: negativer Endbestand ohne festen Zugang.
- P2: negativer Endbestand mit festem Zugang.
- P3: unter Sicherheits-/Meldebestand, exklusive Komponente ohne positive
  Deckung oder fehlender SAP-Endbestand.
- Fehlmenge = `max(0, -Endbestand)`; Fehlwert = Fehlmenge mal Stueckkosten.
- Bestand und Verbrauch werden je Komponente dedupliziert. Bestandswerte werden
  nicht ueber mehrere Stuecklistenverwendungen summiert.
- Ein leerer Endbestand bleibt eine Datenluecke und wird nicht als echter
  Nullbestand oder als Fehlmenge gerechnet.

### Bestellbedarf und Deckung

- Der Endbestand aus `ZMD04_CALC` bleibt fuehrend.
- EKET liefert offene Bestellmenge, ueberfaellige Menge und naechsten
  Plantermin als Beleg-Drill.
- EKET-Zugaenge werden nicht nochmals zum Endbestand addiert; dadurch entsteht
  keine Doppelzaehlung.
- Offener Wert wird aus dem offenen Mengenanteil und dem CHF-bewerteten
  Positionsstueckwert gebildet.

### Materialabhaengigkeit

- Lieferantenzahl und Top-Lieferantenanteil basieren auf der beobachteten
  Bestellhistorie in EKKO/EKPO.
- Ein beobachteter Lieferant plus mindestens fuenf Elternmaterialien ist P1;
  ein Lieferant ohne breite Verwendung P2; Top-Anteil ab 80 Prozent P3.
- Das ist keine freigegebene Bezugsquellenliste und wird in der GUI entsprechend
  bezeichnet.

### Dispositionspruefung

Getrennte Pruefauftraege entstehen fuer:

- negativen Endbestand ohne Sicherheitsbestand,
- negativen Endbestand ohne Meldebestand,
- fehlendes Dispositionsmerkmal,
- fehlende Beschaffungsart,
- Materialstatus 98/99 bei noch vorhandener Verwendung,
- Fixlosgroesse bei bestehender Deckungsluecke.

Die Auswertung schreibt keine Parameter nach SAP zurueck.

### Lieferperformance

Der Reiter zeigt nur die heute belegbare Plantermin-Sicht: offene Menge,
ueberfaellige Menge, Plantermin und offenen Wert aus EKET/EKPO. Eine echte
Liefertermintreue oder OTIF wird nicht berechnet, weil das
Ist-Wareneingangsdatum nicht im aktuellen Cache liegt. Dafuer ist eine spaetere
Erweiterung um `EKBE`, `MSEG` oder `MATDOC` erforderlich. Die GUI zeigt deshalb
`Ist-Termin-Abdeckung 0 %` und `Quelle fehlt` statt einer scheinbar genauen KPI.

## Produktgruppen

Die neuen Reiter lesen dieselben additiven Zuordnungen wie der Spend-Aufriss:

1. manuelle `PurchasingProductGroupMap` bleibt fuehrend,
2. `PurchasingSpendDisponentRule` aus `zdispo*.xlsx` ist nur Fallback,
3. exakte Regel gewinnt vor einem Sternmuster,
4. ohne Namen bleibt `Disponent <Code>` sichtbar.

Keine Zuordnungstabelle wird durch die neuen Seiten geschrieben oder ersetzt.

## Technische Umsetzung

- UI: `Components/Pages/SupplyChainAnalysis.razor`
- Modelle: `Models/SupplyChainAnalysisModels.cs`
- Read-only-Service: `Services/SupplyChainAnalysisService.cs`
- DI: `Program.cs`
- additive Navigation: `Services/DatabaseSeedService.cs`
- eigener Sprachkatalog fuer `es`, `it`, `hi`, `sq`, `tr` und `tlh`:
  `Services/SupplyChainUiTextCatalog.cs` und
  `Services/SupplyChainUiTextGeneratedTranslations.cs`
- Regressionstests: `TrafagSalesExporter.Tests/SupplyChainAnalysisServiceTests.cs`

## Testgrenzen und Betrieb

- Sieben neue Berechnungstests decken Deduplizierung, Fehlwert, Deckungslogik,
  Filterwirkung, Single-Supplier-Risiko, getrennte Parameterpruefauftraege,
  fehlenden Endbestand und die OTIF-Datenluecke ab.
- Ein zusaetzlicher Sprachkatalogtest prueft alle neuen GUI-Schluessel in allen
  sechs Oberflaechensprachen und die Platzhaltertreue.
- Fuer eine unternehmensweite Aussage ist weiterhin ein vollstaendiger
  ZLO03-Full-Load erforderlich. Der zuletzt dokumentierte Produktivstand hatte
  nur 105 ZLO03-Zeilen mit Disponent.
- Echte Lieferperformance bleibt bis zur Anbindung eines Ist-Wareneingangsdatums
  fachlich offen.
