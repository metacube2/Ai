# RAG Product Mapping

Stand: 2026-05-27

## Kurzstand

- Neue Anforderung: Artikel aus dem Group Sales Report sollen anhand der TR-AG-Zuordnung klassifiziert werden.
- Ziel-Felder: `Produkthierarchie`, `Produktfamilie`, `Produktsparte`.
- Produkthierarchie kommt direkt aus TR-AG-Artikelstammdaten.
- Produktfamilie und Produktsparte kommen danach ueber separate Mapping-Tabelle.
- Nicht gefundene Materialnummern laufen unter `Sonstige/ohne Zuordnung`.

## Aktueller Code-Stand

- Vorhanden: `Material`, `ProductGroup`.
- Noch nicht vorhanden: explizite Felder fuer Produkthierarchie, Produktfamilie, Produktsparte.
- SAP-Seed-Mapping nutzt aktuell `Z.Matnr` -> `Material` und `Z.Prodh` -> `ProductGroup`.
- Zu klaeren: Ist `Z.Prodh` fachlich die Produkthierarchie?

## Offene Punkte Fuer Sitzung

- Quelle und Format des TR-AG-Artikelstamms.
- Normalisierung der Materialnummern.
- Struktur der Mapping-Tabelle von Kendra.
- Matching-Regeln: exakt, Prefix, Range, Prioritaet.
- Historisierung der Zuordnung fuer reproduzierbare Reports.
- Pruefansicht fuer nicht zugeordnete Artikel.

## Rohquelle Nur Bei Bedarf

- Detaildoku: `docs/PRODUCT_SPARTEN_MAPPING_2026-05-27.md`

