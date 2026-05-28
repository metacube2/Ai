# Last Change

Stand: 2026-05-27

Diese Datei ist fuer tokenarme RAG-Nutzung komprimiert.

## Aktueller Kurzstand

- Fuehrender Kurzkontext: `docs/rag/PROJECT.md`.
- Themenrouter: `docs/RAG_ROUTER.md`.
- Letzter dokumentierter Stand aus dem Roharchiv: Rebase/Push synchron mit `origin/main`, Head `d853f53 Add published HR KPI workflow fixes`.
- Letzte dokumentierte Validierung: Build erfolgreich, Tests `78/78` gruen.
- Neu dokumentiert: Produktsparten-Mapping fuer Group Sales Report ueber TR-AG-Artikelstamm und separate Mapping-Tabelle.
- Neu dokumentiert: Upgreat-Firewall-Freigabe muss fuer den publizierten Webserver `10.120.1.17` erfolgen, nicht fuer den lokalen Entwicklungs-PC.
- Neu umgesetzt: `Management Analyse` im Finance Cockpit hat zusaetzliche Reiter fuer Laender, Datenstatus, Abweichungen, Gutschriften-Kandidaten und Datenqualitaet.
- Neu erstellt: ABAP-Arbeitsstand fuer Produktsparten-Mapping mit Provider-Klasse, ALV-Report und Mapping-Build-Report.

## Nachtrag 2026-05-28 ABAP Produktsparten-Mapping

Erstellt:

- `docs/abap/ZCL_PRODSPARTE_PROVIDER.abap`
- `docs/abap/Z_PRODSPARTE_REPORT.abap`
- `docs/abap/Z_PRODSPARTE_MAP_BUILD.abap`
- `docs/abap/README_PRODSPARTE.md`

Dokumentierter Zielansatz:

- SAP TR AG bleibt Quelle der Wahrheit fuer `MATNR -> PAPH1 -> WWPFA -> WWPSP`.
- Mapping-Build liest reale CO-PA-Ableitungen aus `CE11000` und schreibt eindeutige Saetze in `ZPRODSPARTE_MAP`.
- Provider liest verkaufsrelevante Materialien aus `MVKE`, Texte aus SAP-Texttabellen und Mapping aus `ZPRODSPARTE_MAP`.
- ALV-Report und spaeter OData sollen dieselbe Provider-Methode verwenden.
- Nicht zugeordnete Materialien erhalten Fallback `UNASS` / `Nicht zugeordnet`.

Offen:

- `PAPH1 = MVKE-PRODH(5)` bestaetigen.
- Texttabellen `T25A0`/`T25A1` bestaetigen.
- Relevante `VKORG`/`VTWEG` fuer TR AG festlegen.
- `CE11000` als richtige CO-PA-Quelle bestaetigen.

## Nachtrag 2026-05-28 Finance Management Analyse Reiter

Umgesetzt:

- `Management Analyse` erweitert die bestehende `Finance Summary` um weitere Reiter im Cockpit-Stil.
- Neue Reiter:
  - `Laender`
  - `Datenstatus`
  - `Abweichungen`
  - `Gutschriften`
  - `Datenqualitaet`
- Grundlage sind vorhandene Daten aus `CentralSalesRecords`, `FinanceReferences`, `Sites` und `ExportLogs`.
- Keine neuen Fachregeln eingefuehrt:
  - Gutschriften-Reiter zeigt technische Kandidaten.
  - Datenqualitaet zeigt technische Pruefpunkte.
  - Produktsparten-/Produktfamilienlogik bleibt bis Kendra-Mapping offen.
- Test ergaenzt: `AnalyzeFinanceSummaryAsync_Builds_Dashboard_Tab_Data`.
- Validierung: `dotnet test TrafagSalesExporter.sln --verbosity minimal` mit `79/79` Tests gruen.

## Nachtrag 2026-05-27 Produktsparten-Mapping

Dokumentiert:

- Neue Detaildoku `docs/PRODUCT_SPARTEN_MAPPING_2026-05-27.md`.
- Neue RAG-Kurzdatei `docs/rag/PRODUCT_MAPPING.md`.
- Router-Eintrag fuer Themen `Group Sales Report`, `Produkthierarchie`, `Produktfamilie`, `Produktsparte`.
- Fachliche Annahme: Materialnummern aus Group Sales Report werden gegen TR-AG-Artikelstamm aufgeloest; nicht gefundene Artikel laufen unter `Sonstige/ohne Zuordnung`.
- Offene Sitzungspunkte: Quelle des Artikelstamms, Bedeutung von `Z.Prodh`, Mapping-Tabelle von Kendra, Range-/Prefix-Regeln, Historisierung.

## Volltext Bei Bedarf

Die kanonische Detailhistorie liegt hier:

```text
docs/raw_md_archive/HISTORY_CANONICAL.md.raw
```

Die frueheren Original-Volltexte liegen als Wiederherstellungs-Backup hier:

```text
docs/raw_md_archive/original_history_raws.zip
```

Nur laden, wenn genaue Chronologie, alte Zwischenstaende, Commit-Historie oder Audit-Spuren benoetigt werden.
