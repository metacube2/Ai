# RAG Architecture

Stand: 2026-05-27

## Kurzstand

- App sammelt Daten aus SAP OData, HANA/SAP B1, SharePoint und manuellen Excel-/CSV-Quellen.
- Zentrale Persistenz ueber `CentralSalesRecords`.
- Finance-Auswertung und zentrale Excel sollen dieselbe Regelengine verwenden.
- Diagramme und Anwenderdokus existieren fuer Keyuser-Prozess und technische Architektur.

## Rohquellen Nur Bei Bedarf

- Diagramme: `docs/PROGRAMM_DIAGRAMME.md`
- technischer Handoff und alter LLM-Systemkontext: `docs/raw_md_archive/HISTORY_CANONICAL.md.raw`
