# Einkauf Spend: Produktgruppen und ABC/XYZ

Stand: 2026-08-06

Status: **ZDISPO-Ergaenzung produktiv deployed und technisch verifiziert am
2026-08-06, 13:57 MESZ** (Funktionscommit `0a8a4c9`; Grundfunktion
`bb009bf`). Die beiden gelieferten Referenzdateien `zdispo_grp.xlsx` und
`zdispo_spart.xlsx` sind eingebunden. Sie ergaenzen ausschliesslich den
Produktgruppen-Aufriss; bestehende manuelle Zuordnungen bleiben fuehrend und
werden nicht ueberschrieben.

## Entscheidung 1: Produktgruppen-Aufriss

Der Reiter `Einkauf > Spend-Aufriss` bietet neu die Einstiegsperspektive
`Produktgruppe` mit der Kaskade:

`Produktgruppe -> Lieferant -> Material`

Die Zuordnung verwendet die vorhandene Datenkette:

`EKPO-MATNR -> ZLO03-Komponente -> verwendendes Kopfmaterial -> VknrDispo -> Produktname`

`VknrDispo` wird beim ZLO03-Load neu als eigene Cache-Spalte gespeichert. Die
optionale Tabelle `PurchasingProductGroupMap` enthaelt die Referenz
`Disponent -> ProductGroup / ProductGroupText` mit Quelle `ZC23`.

Ergaenzend wird beim App-Start nur die separate Tabelle
`PurchasingSpendDisponentRule` aus den beiden ZDISPO-Dateien aktualisiert:

- `zdispo_grp.xlsx`: `DISPO_KZ` (Disponent oder Muster) -> `DISPO`;
- `zdispo_spart.xlsx`: `DISPO` -> lesbarer `DESCR`-Produktname;
- exakter Disponent gewinnt vor einem Sternmuster; bei mehreren Mustern gewinnt
  das laengste passende Muster;
- Prioritaet: manuelle `PurchasingProductGroupMap` vor ZDISPO vor
  `Disponent <Code>`.

Der Import schreibt keine Zeile der bestehenden manuellen Tabelle und ist nur
in der Perspektive `Produktgruppe` des Reiters `Spend-Aufriss` verdrahtet. Die
anderen Einkaufs- und Finance-Sichten bleiben unveraendert.

Solange ein Disponent weder einen manuellen noch einen ZDISPO-Treffer hat,
lautet die sichtbare Gruppe
`Disponent <Code>`. Materialien ohne ZLO03-/Disponenten-Zuordnung bleiben als
`ohne Produktgruppe` sichtbar. Es gibt keinen stillen Ausschluss.

## Entscheidung 2: Komponenten mit mehreren Produktgruppen

Eine Komponente kann in Kopfmaterialien mehrerer Produktgruppen vorkommen. Die
verbindliche Regel ist eine gleichmaessige, summenerhaltende Allokation:

- eine unterschiedliche Produktgruppe: 100 Prozent auf diese Gruppe;
- zwei unterschiedliche Produktgruppen: je 50 Prozent;
- `n` unterschiedliche Produktgruppen: je `1/n`;
- mehrere Disponenten derselben Produktgruppe zaehlen nur als eine Gruppe;
- mehrere ZDISPO-Zeilen fuer dasselbe Muster bleiben als getrennte Gruppen
  erhalten und werden ebenfalls `1/n` verteilt;
- ohne Zuordnung: 100 Prozent auf `ohne Produktgruppe`.

Damit gilt immer:

`Summe Produktgruppen-Spend = Spend des gewaehlten Zeitraums`

Die GUI zeigt zugeordneten und unzugeordneten Spend, Mehrfachverwendungen samt
betroffenem Spend sowie die Zahl gemappter und offener Disponenten. Die Regel
ist direkt im Reiter erklaert.

## Entscheidung 3: konkreter Nutzen von ABC/XYZ

ABC und XYZ bleiben als Klassifikation sichtbar, werden aber neu als gemeinsame
Massnahmenmatrix ausgewertet. ABC beschreibt die Wertbedeutung, XYZ die
Regelmaessigkeit beziehungsweise Planbarkeit des Bedarfs.

| Klasse | Fachlicher Pruefauftrag | Prioritaet |
| --- | --- | --- |
| AX | Rahmenvertrag, Lieferfaehigkeit und automatische Disposition pruefen | hoch |
| AY / AZ | Forecast, Sicherheitsbestand und Zweitquelle pruefen | hoch |
| BX | Mengen standardisieren/buendeln, Bestellrhythmus und Konditionen optimieren | mittel |
| BY / BZ | Losgroessen, Mindestmengen und schwankende Bedarfsursachen mit Disposition pruefen | mittel |
| CX | Prozesskosten mit Katalog, Automatisierung und Sammelbestellungen senken | optimieren |
| CY / CZ | Tail Spend buendeln, auslisten oder auf Standardalternativen umstellen | optimieren |
| Klasse fehlt/unbekannt | Stammdaten beziehungsweise Klassifikation pruefen | Datenpflege |

Die Matrix zeigt je Kombination Spend CHF, Zahl der Materialien und
Lieferanten. Sie erzeugt einen **Pruefauftrag**, keine automatische Aenderung
von Disposition, Sicherheitsbestand oder Lieferant.

## Noch offen vor fachlicher Produktivabnahme

- Einen bekannten Mehrfachverwendungsfall mit Einkauf und Disposition gegen
  ZDISPO pruefen.
- Fuer `DISPO D5` fehlt in `zdispo_spart.xlsx` ein `DESCR`; die GUI zeigt
  deshalb ehrlich den Code `D5`. Fachlich klaeren, ob ein Name nachzuliefern ist.
- Eigentuemmer und Aktualisierungsweg der beiden ZDISPO-Dateien festlegen.
- Nach Deploy den ZLO03-Full-Load ausfuehren, damit `VknrDispo` im Cache gefuellt
  ist.
- Summe des Produktgruppen-Aufrisses gegen den unverteilten Gesamt-Spend eines
  echten Zeitraums abstimmen.

## Technischer Nachweis

- Build: erfolgreich am 2026-08-06.
- Gezielte Einkaufs-/Schema-Tests: `47/47` erfolgreich.
- Lokalisierungstests: `6/6` erfolgreich.
- Gesamte Regression: `435/435` Tests erfolgreich.
- Produktivartefakt `BiDashboard.dll`: `4'136'448` Bytes, Zeitstempel
  `06.08.2026 13:57:11`, SHA256
  `0F1CB29F6F766C8CB71903D45B78DB48B3AB94FE58638837F5376E9D2A9B01C1`.
- `app_offline.htm` wurde fuer den Publish gesetzt und danach aus dem aktiven
  Namen entfernt. Startseite HTTPS `200` (`64'770` Bytes); direkter Aufruf
  `/BiDashboard/einkauf/aufriss` HTTPS `200` (`133'542` Bytes, warm `10.15 s`).
- Produktivschema lesend verifiziert: Spalte `MaterialUsageCache.VknrDispo`
  vorhanden, Tabellen `PurchasingProductGroupMap` und
  `PurchasingSpendDisponentRule` vorhanden. Aktuell tragen `105`
  ZLO03-Cachezeilen einen Disponenten. Die manuelle ZC23-Tabelle blieb bei `0`
  Zeilen; separat wurden `45` ZDISPO-Zuordnungen aus `42` Mustern geladen.
- Regressionstest belegt: Material M1 mit CHF 120 und zwei Produktgruppen wird
  CHF 60 / CHF 60 verteilt; gemeinsam mit zugeordnetem und unzugeordnetem Spend
  bleibt die Gesamtsumme CHF 250.
