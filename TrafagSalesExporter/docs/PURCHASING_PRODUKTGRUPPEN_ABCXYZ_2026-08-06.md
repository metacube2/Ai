# Einkauf Spend: Produktgruppen und ABC/XYZ

Stand: 2026-08-06

Status: **produktiv deployed und technisch verifiziert am 2026-08-06, 12:31 MESZ**
(Funktionscommit `bb009bf`). Die fachlichen
ZC23-Bezeichnungen sind im Repository nicht vorhanden und werden deshalb nicht
erfunden. Bis die Referenzliste eingespielt ist, zeigt die Anwendung den
belegten Disponenten-Code.

## Entscheidung 1: Produktgruppen-Aufriss

Der Reiter `Einkauf > Spend-Aufriss` bietet neu die Einstiegsperspektive
`Produktgruppe` mit der Kaskade:

`Produktgruppe -> Lieferant -> Material`

Die Zuordnung verwendet die vorhandene Datenkette:

`EKPO-MATNR -> ZLO03-Komponente -> verwendendes Kopfmaterial -> VknrDispo -> ZC23-Produktgruppe`

`VknrDispo` wird beim ZLO03-Load neu als eigene Cache-Spalte gespeichert. Die
optionale Tabelle `PurchasingProductGroupMap` enthaelt die Referenz
`Disponent -> ProductGroup / ProductGroupText` mit Quelle `ZC23`.

Solange ein Disponent keinen ZC23-Eintrag hat, lautet die sichtbare Gruppe
`Disponent <Code>`. Materialien ohne ZLO03-/Disponenten-Zuordnung bleiben als
`ohne Produktgruppe` sichtbar. Es gibt keinen stillen Ausschluss.

## Entscheidung 2: Komponenten mit mehreren Produktgruppen

Eine Komponente kann in Kopfmaterialien mehrerer Produktgruppen vorkommen. Die
verbindliche Regel ist eine gleichmaessige, summenerhaltende Allokation:

- eine unterschiedliche Produktgruppe: 100 Prozent auf diese Gruppe;
- zwei unterschiedliche Produktgruppen: je 50 Prozent;
- `n` unterschiedliche Produktgruppen: je `1/n`;
- mehrere Disponenten derselben Produktgruppe zaehlen nur als eine Gruppe;
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

- ZC23-Referenzliste mit den echten Bezeichnungen in
  `PurchasingProductGroupMap` einspielen und Eigentuemmer der Pflege festlegen.
- Einen bekannten Mehrfachverwendungsfall mit Einkauf und Disposition gegen
  ZC23 pruefen.
- Nach Deploy den ZLO03-Full-Load ausfuehren, damit `VknrDispo` im Cache gefuellt
  ist.
- Summe des Produktgruppen-Aufrisses gegen den unverteilten Gesamt-Spend eines
  echten Zeitraums abstimmen.

## Technischer Nachweis

- Build: erfolgreich am 2026-08-06.
- Gezielte Einkaufs-/Schema-Tests: `47/47` erfolgreich.
- Lokalisierungstests: `6/6` erfolgreich.
- Gesamte Regression: `435/435` Tests erfolgreich.
- Produktivartefakt `BiDashboard.dll`: `4'120'064` Bytes, Zeitstempel
  `06.08.2026 12:31:27`, SHA256
  `B5C72496A7A4E11AC38675D840A5DF9DBABA6999517DD70FE3D7C0CE07BAEC3C`.
- `app_offline.htm` wurde fuer den Publish gesetzt und danach aus dem aktiven
  Namen entfernt. Startseite HTTP `200` (`64'755` Bytes); direkter Aufruf
  `/BiDashboard/einkauf/aufriss` HTTP `200` (`133'577` Bytes, warm `8.43 s`).
- Produktivschema lesend verifiziert: Spalte `MaterialUsageCache.VknrDispo`
  vorhanden, Tabelle `PurchasingProductGroupMap` vorhanden. Aktuell tragen
  `105` ZLO03-Cachezeilen einen Disponenten; die ZC23-Mappingtabelle enthaelt
  noch `0` Zeilen. Das ist die verbleibende fachliche Datenluecke.
- Regressionstest belegt: Material M1 mit CHF 120 und zwei Produktgruppen wird
  CHF 60 / CHF 60 verteilt; gemeinsam mit zugeordnetem und unzugeordnetem Spend
  bleibt die Gesamtsumme CHF 250.
