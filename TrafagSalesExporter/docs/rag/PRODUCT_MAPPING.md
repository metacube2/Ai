# RAG Product Mapping

Stand: 2026-05-27

## Kurzstand

- Neue Anforderung: Artikel aus dem Group Sales Report sollen anhand der TR-AG-Zuordnung klassifiziert werden.
- Ziel-Felder: `Produkthierarchie`, `Produktfamilie`, `Produktsparte`.
- SAP TR AG bleibt Quelle der Wahrheit.
- Dashboard soll KEDR-/KE30-Ableitung nicht in C# nachbauen.
- ABAP/Gateway soll eine flache Referenz liefern: `MATNR -> PAPH1 -> WWPFA -> WWPSP`.
- Nicht gefundene oder nicht eindeutig ableitbare Materialnummern laufen unter `Nicht zugeordnet`.

## Aktueller Code-Stand

- Vorhanden: `Material`, `ProductGroup`.
- Noch nicht vorhanden: explizite Felder fuer Produkthierarchie, Produktfamilie, Produktsparte.
- SAP-Seed-Mapping nutzt aktuell `Z.Matnr` -> `Material` und `Z.Prodh` -> `ProductGroup`.
- Zu klaeren: Ist `Z.Prodh` fachlich die Produkthierarchie?

## ABAP-Arbeitsstand

- `docs/abap/ZCL_PRODSPARTE_PROVIDER.abap`: Provider fuer ALV und spaeter OData.
- `docs/abap/Z_PRODSPARTE_REPORT.abap`: ALV-Testreport.
- `docs/abap/Z_PRODSPARTE_MAP_BUILD.abap`: baut `ZPRODSPARTE_MAP` aus `CE11000`.
- `docs/abap/README_PRODSPARTE.md`: DDIC- und Pruefhinweise.

## SAP-Zielbild

- `Z_PRODSPARTE_MAP_BUILD` liest reale CO-PA-Ableitungen aus `CE11000`.
- Eindeutige `PAPH1 -> WWPFA -> WWPSP` werden in `ZPRODSPARTE_MAP` gespeichert.
- Mehrdeutige PAPH1 werden protokolliert und nicht geschrieben.
- `ZCL_PRODSPARTE_PROVIDER` liest `MVKE-PRODH`, Texte und Mapping.
- OData-Service ruft spaeter dieselbe Provider-Klasse.

## Offene Punkte Fuer Sitzung

- Quelle und Format des TR-AG-Artikelstamms.
- Normalisierung der Materialnummern.
- Struktur der Mapping-Tabelle von Kendra.
- Matching-Regeln: exakt, Prefix, Range, Prioritaet.
- Historisierung der Zuordnung fuer reproduzierbare Reports.
- Pruefansicht fuer nicht zugeordnete Artikel.
- `PAPH1 = MVKE-PRODH(5)` fachlich/technisch bestaetigen.
- Richtige Texttabellen fuer `WWPFA`/`WWPSP` bestaetigen.
- VKORG/VTWEG fuer TR-AG-Referenzlauf bestaetigen.

## Rohquelle Nur Bei Bedarf

- Detaildoku: `docs/PRODUCT_SPARTEN_MAPPING_2026-05-27.md`
