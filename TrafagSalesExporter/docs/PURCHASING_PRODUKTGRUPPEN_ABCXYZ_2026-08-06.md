# Einkauf Spend: Produktgruppen und ABC/XYZ

Stand: 2026-10-29 (Ueberholt-Vermerke nachgetragen; Inhalt vom 2026-08-11, Ergaenzung NA vom 2026-10-15)

Aktueller Status: Die direkte SAP-OData-Strecke ist im Anwendungscode umgesetzt
und mit `464/464` Tests verifiziert. Die SAP-EntitySets fehlen am 2026-08-11 noch
im produktiven `$metadata`; deshalb ist dieser neue Stand noch nicht deployed.
Vollstaendiger Nachweis und Aktivierungsreihenfolge:
`docs/PURCHASING_PRODUCT_GROUP_SAP_DIRECT_2026-08-11.md`.
*Ueberholt am 2026-10-29: am 2026-08-11 15:51 deployed, die SAP-EntitySets sind seit 2026-08-12 produktiv aktiv
(Abschnitt „Produktiver Abschluss 2026-08-12“ in `docs/PURCHASING_PRODUCT_GROUP_SAP_DIRECT_2026-08-11.md`).*

Historischer Produktivstand: Die Excel-ZDISPO-Ergaenzung wurde am 2026-08-06
deployed. Sie bleibt bis zum gemeinsamen SAP-/App-Deploy auf dem Server aktiv.
*(ueberholt am 2026-08-12: Excel ist keine aktive Mappingquelle mehr.)*

*Ueberholt am 2026-10-26 (`0dc3c68`):* Die Einstiege „Produktgruppe : Lieferant“ und „Produktgruppe : Warengruppe“
ordnen jedes Einkaufsmaterial ueber seinen eigenen `MARC-DISPO` (Werk 1100) genau einer Gruppe zu, wie der Report
ZM_OFFENE_KDAUF. Die Kette ueber ZLO03 und die 1/n-Verteilung in den Entscheidungen 1 und 2 unten gelten fuer diese
Einstiege nicht mehr; bei mehreren gleichwertigen SAP-Regeln gewinnt die kleinste Gruppennummer. Seit 2026-10-27
(`0917d9e`) zeigt die Gruppe nur den SAP-Text. Details: `docs/PURCHASING_PRODUCT_GROUP_SAP_DIRECT_2026-08-11.md`,
Nachtraege 2026-10-26 und 2026-10-27.

## Klarstellung Warengruppe versus Produktgruppe (2026-08-11)

- **Warengruppe** kommt aus SAP: bevorzugt wird die aktuelle Materialstamm-
  Warengruppe `MARA001Set.Matkl`; nur wenn sie fehlt, wird die historische
  Beleg-Warengruppe `EKPOSet.Matkl` verwendet. Die deutschen Bezeichnungen
  stammen aus einer lokal im Code hinterlegten SAP-T023T-Listenausgabe.
- **Produktgruppe** ist eine abgeleitete Einkaufssicht und soll fachlich
  vollstaendig aus SAP kommen. Die Datenkette ist
  `EKPO-MATNR -> ZLO03-Komponente -> Kopfmaterial -> VknrDispo -> ZC23/ZDISPO-Produktgruppe`.
  Der neue Anwendungscode liest auch die letzte Zuordnung direkt aus SAP OData.
  Im produktiven Gateway fehlen dafuer noch die EntitySets.
- Der produktiv verifizierte Stand vom 2026-08-06 hatte `0` direkt geladene
  ZC23-Zuordnungen und `45` Zuordnungen aus `42` ZDISPO-Mustern. Die Anwendung
  uebernimmt diese SAP-ZDISPO-Referenzdaten derzeit technisch aus den beiden
  Exportdateien `zdispo_grp.xlsx` und `zdispo_spart.xlsx`. Die Exceldateien sind
  damit ein temporaerer Transportweg, nicht das fachlich fuehrende System.
- Die Exceldateien sind im neuen Code weder Importquelle noch Notfall-Fallback
  und werden nicht mehr in Build oder Publish kopiert.
- `check.xlsx` ist an dieser Zuordnung nicht beteiligt.

## Entscheidung 1: Produktgruppen-Aufriss

Der Reiter `Einkauf > Spend-Aufriss` bietet neu die Einstiegsperspektive
`Produktgruppe` mit der Kaskade:

`Produktgruppe -> Lieferant -> Material`

Die Zuordnung verwendet die vorhandene Datenkette:

`EKPO-MATNR -> ZLO03-Komponente -> verwendendes Kopfmaterial -> VknrDispo -> Produktname`

`VknrDispo` wird beim ZLO03-Load als eigene Cache-Spalte gespeichert. Full Load
und Delta lesen die Produktgruppenreferenz direkt aus SAP:

- SAP `ZDISPO_GRP`: `DISPO_KZ` (Disponent oder Muster) -> `DISPO`;
- SAP `ZDISPO_SPART`: `DISPO` -> lesbarer `DESCR`-Produktname;
- exakter Disponent gewinnt vor einem Sternmuster; bei mehreren Mustern gewinnt
  das laengste passende Muster;
- ohne SAP-Treffer bleibt `Disponent <Code>` sichtbar.

Nur Regeln mit Quelle `SAP OData: ...` werden ausgewertet. Alte Excel- und
manuelle Cachezeilen sind damit wirkungslos.

Solange ein Disponent keinen SAP-ZDISPO-Treffer hat, lautet die sichtbare Gruppe
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
- ohne Zuordnung: 100 Prozent auf `ohne Produktgruppe`;
- *seit 2026-10-15:* die SAP-Gruppe `NA` („N/A“, `ZDISPO_SPART`) ist ein Platzhalter fuer „keine Produktgruppe“
  und zaehlt neben einer echten Gruppe **nicht** mit (Fall B53173: vorher halb T2, halb NA, jetzt ganz T2).
  Nur unter `NA` verwendete Materialien bleiben unter `NA - N/A` sichtbar. Gleiche Regel in der Lieferkettenanalyse
  (Textliste der Gruppen). Test `LoadAsync_ProductGroupPerspective_Placeholder_NA_Takes_No_Share_Next_To_Real_Group`.

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
- Fuer `DISPO D5` fehlte in der bisherigen SAP-Listenausgabe ein `DESCR`; die GUI zeigt
  deshalb ehrlich den Code `D5`. Fachlich klaeren, ob ein Name nachzuliefern ist.
  *Ueberholt am 2026-10-01 (`3407868`): D1 und D5 haben im Dashboard Ersatztexte nach Vorgabe Ingo; ein spaeter in SAP
  gepflegter Text gewinnt.*
- SAP-EntitySets `ZDISPO_GRP` und `ZDISPO_SPART` aktivieren und live pruefen. *Erledigt 2026-08-12.*
- Nach Deploy den ZLO03-Full-Load ausfuehren, damit `VknrDispo` im Cache gefuellt
  ist. *Ueberholt am 2026-10-26: die Produktgruppe nutzt `VknrDispo` nicht mehr.*
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
