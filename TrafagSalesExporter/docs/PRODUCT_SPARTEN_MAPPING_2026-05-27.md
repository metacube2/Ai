# Produktsparten-Mapping Group Sales Report

Stand: 2026-05-27

## Anlass

Fuer die Zuordnung der Artikel aus dem Group Sales Report zu Produktbereichen wurde abgestimmt, die Zuordnung von TR AG als fuehrende Referenz zu verwenden.

Ziel ist, Artikel bzw. Materialnummern aus dem Group Sales Report in einem ersten Schritt folgenden Elementen zuzuordnen:

- Produkthierarchie
- Produktfamilie
- Produktsparte

## Vorgeschlagene Fachlogik

1. Materialnummer aus dem Group Sales Report lesen.
2. Materialnummer gegen Artikelstammdaten der TR AG aufloesen.
3. Produkthierarchie direkt aus den Artikelstammdaten uebernehmen.
4. Produktfamilie und Produktsparte anschliessend ueber eine separate Mapping-Tabelle ableiten.
5. Artikel ohne Treffer in den TR-AG-Stammdaten automatisch unter `Sonstige/ohne Zuordnung` fuehren.

## Mapping-Tabelle

Nach aktuellem Verstaendnis definiert die separate Mapping-Tabelle die Zuordnung von Produktgruppen bzw. Produkthierarchie-Bereichen zu Produktfamilien und Produktsparten.

Moegliche technische Regeln, die fachlich zu bestaetigen sind:

- exakte Codes
- Prefix-Regeln
- Von/Bis-Ranges
- Prioritaet bei ueberlappenden Bereichen
- Gueltigkeit nach Datum oder Version

Kendra kann laut Aufgabenbeschreibung weitere Details zur bestehenden Logik und zur konkreten Umsetzung geben.

## Aktueller technischer Stand Im Projekt

Im aktuellen Datenmodell existieren bereits:

- `Material`
- `ProductGroup`

Noch nicht explizit vorhanden sind:

- `Produkthierarchie`
- `Produktfamilie`
- `Produktsparte`

Relevante aktuelle Modelle:

- `Models/SalesRecord.cs`
- `Models/CentralSalesRecord.cs`

Hinweis aus der Code-Sichtung:

- SAP-Seed-Mapping nutzt aktuell `Z.Matnr` fuer `Material`.
- SAP-Seed-Mapping nutzt aktuell `Z.Prodh` fuer `ProductGroup`.
- Ob `Z.Prodh` fachlich bereits der gewuenschten Produkthierarchie entspricht, muss bestaetigt werden.

## Empfohlene Umsetzung

Die Produktspartenlogik sollte als eigene sichtbare Mapping-Schicht umgesetzt werden, nicht als versteckte Sonderlogik in Finance oder Management Cockpit.

Sinnvolle technische Bausteine:

- TR-AG-Artikelstamm-Quelle fuer Materialnummer -> Produkthierarchie.
- Mapping-Tabelle fuer Produkthierarchie/ProductGroup/Range -> Produktfamilie und Produktsparte.
- Fallback-Kategorie `Sonstige/ohne Zuordnung`.
- Export-/Excel-Spalten fuer die drei neuen Klassifikationen.
- Pruefansicht fuer nicht zugeordnete Materialnummern.

## Offene Fragen Fuer Andreas / Kendra

| Frage | Warum wichtig |
| --- | --- |
| Woher kommt der fuehrende TR-AG-Artikelstamm technisch? | Datenquelle und Aktualisierung festlegen |
| Welches Feld ist die eindeutige Materialnummer? | Normalisierung, fuehrende Nullen, Varianten |
| Ist `Z.Prodh` die gewuenschte Produkthierarchie? | Bestehendes Mapping evtl. wiederverwendbar |
| Wie sieht die bestehende Mapping-Tabelle aus? | Datenmodell und Importlogik festlegen |
| Werden Ranges, Prefixe oder exakte Werte verwendet? | Matching-Regeln eindeutig implementieren |
| Was gilt bei ueberlappenden Ranges? | Prioritaet / deterministisches Ergebnis |
| Soll die Zuordnung historisiert werden? | Reproduzierbarkeit alter Reports |
| Soll `Sonstige/ohne Zuordnung` nur im Report erscheinen oder auch in Daten persistiert werden? | Datenmodell und Auditierbarkeit |

## Abgrenzung

Dieser Task ist keine Finance-Soll/Ist-Regel. Die Klassifikation kann spaeter Finance- und Management-Auswertungen ergaenzen, sollte aber fachlich getrennt von Net-Sales-Abgrenzungen bleiben.

