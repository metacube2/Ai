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
