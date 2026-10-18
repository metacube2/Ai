# RAG Architecture

Stand: 2026-05-27

## Kurzstand

- App sammelt Daten aus SAP OData, HANA/SAP B1, SharePoint und manuellen Excel-/CSV-Quellen.
- Zentrale Persistenz ueber `CentralSalesRecords`.
- Finance-Auswertung und zentrale Excel sollen dieselbe Regelengine verwenden.
- Produktsparten-Mapping ist als eigene Mapping-Schicht vorgesehen, nicht als versteckte Finance-Regel.
- Produktsparten-Referenz soll ueber SAP/ABAP bzw. Gateway als flache Tabelle geliefert werden.
- Diagramme und Anwenderdokus existieren fuer Keyuser-Prozess und technische Architektur.
- PERF-MUSTER 2026-07-23: `ManagementCockpitService` ist Singleton (Program.cs). Sein
  `LoadCentralRecordsAsync()` (kompletter `CentralSalesRecords`-Read, aktuell 84k Zeilen, waechst
  taeglich) wurde pro Cockpit-Seitenaufruf 2-4x redundant neu geladen (Init + je Tab). Fix: 10s-TTL-
  Cache um genau diesen Ladepunkt, NUR sicher weil alle Aufrufer die Liste rein lesend behandeln
  (Select/GroupBy/Where in neue Objekte, keine In-Place-Mutation der geteilten `SalesRecord`-
  Elemente) - bei einem Singleton-Cache mit mehreren gleichzeitigen Nutzern IMMER zuerst pruefen,
  ob Aufrufer schreibend auf die zurueckgegebene Liste zugreifen, bevor man cached. Details/
  Messwerte: `lastchange.md` Eintrag "PERFORMANCE-BEFUND COCKPIT 2026-07-23".

## Rohquellen Nur Bei Bedarf

- Diagramme: `docs/PROGRAMM_DIAGRAMME.md`
- Produktmapping: `docs/rag/PRODUCT_MAPPING.md`
- technischer Handoff und alter LLM-Systemkontext: `docs/raw_md_archive/HISTORY_CANONICAL.md.raw`
