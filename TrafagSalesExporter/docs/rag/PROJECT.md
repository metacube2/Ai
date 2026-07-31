# RAG Project

Stand: 2026-07-31

Kanonischer Live-Abgleich fuer UK-2025, Supplier-Felder, Konzern-Standardkosten
und Einkauf-Delta: `docs/AKTUELLER_LIVEDATEN_STAND_2026-07-31.md`.
Bei Abweichungen hat dessen direkt gepruefter Stand Vorrang.

## Kurzstand

- LIVE-PRUEFUNG 2026-07-31: UK-2025 ist produktiv vorhanden (1'867 Zeilen);
  `GroupStandardCosts` ist mit 63'506 TR-AG-Werten gefuellt; Supplier bleibt bei
  CH/AT/DE/ES komplett leer. Einkauf-Delta-Fix ist deployed, aber zum
  Pruefzeitpunkt gab es noch keinen produktiven Delta-Lauf nach dem Deploy.
  Details und genaue Vorrangregel:
  `docs/AKTUELLER_LIVEDATEN_STAND_2026-07-31.md`.

- Fuehrende App: `TrafagSalesExporter`, publiziert als `BiDashboard`.
- Aktuelle fachliche Detailstaende und offene Punkte stehen ausschliesslich in
  den Themen-Kurzdateien und im kanonischen Live-Abgleich; alte Deploy-Nachweise
  stehen in `lastchange.md` beziehungsweise `docs/raw_md_archive/`.
- Management-/Roadmap-Doku neu: `docs/INGO_TODOS_180_TAGE_2026-06-18.docx`, Quelle `docs/INGO_TODOS_180_TAGE_2026-06-18.md`. Sie beschreibt Ingos 180-Tage-Fokus: Sales Management Cockpit/Data-Lake als Prioritaet 1, HR Dashboard und Einkaufs Dashboard als Prioritaet 2/3, Q3/Q4-Meilensteine, Abhaengigkeiten, Risiken und naechste Schritte.
- Abgrenzung fuer 180 Tage: S/4HANA Compatibility Check/RPC-/RFC-Themen bleiben bei Lucas; Infrastruktur/Security/Server/Netzwerk bleiben bei Alex/Ramon/Upgreat. Ingo bleibt bei Analytics, BI, Reporting-/Z-Funktionsbezug und .NET/ASP-Webseiten.
- Wichtig DE/Sparten: Alphaplan `ArtikelNummer` wird als lokale Materialnummer importiert, aber nicht als garantiert identische TR-AG-/SAP-`MATNR` normalisiert. Nicht gematchte Nummern erscheinen weiterhin als `Nicht im TR-AG-Stamm`.
- Aktuelle Finance-Schulung: `docs/FINANCE_SCHULUNG_FINANZ_2026-06-11.md` mit Prozessgrafiken fuer Exportfluss, Audit-CSV-Auswertungsquelle und Waehrungsumrechnung.
- India/TRIN: produktive Server-DB steht auf `TRIN -> SAGE -> 20.197.20.60:30015`, Schema `TRAFAG_LIVE`, User-Override `TRAFAGCONTROLS`.
- Fuer normale Weiterarbeit diese Datei plus den passenden Themen-RAG laden.

## Aktive Themen

- Finance Cockpit: `docs/rag/FINANCE.md`
- Manual Import: `docs/rag/MANUAL_IMPORT.md`
- Produktmapping: `docs/rag/PRODUCT_MAPPING.md`
- HR KPI: `docs/rag/HR_KPI.md`
- Deployment/IIS: `docs/rag/DEPLOYMENT.md`
- Admin/Startseite: `docs/rag/ADMIN.md`
- Einkauf: `docs/rag/PURCHASING.md`
- 180-Tage-Roadmap Ingo: `docs/INGO_TODOS_180_TAGE_2026-06-18.md`

## Rohquellen Nur Bei Bedarf

- kanonische Detailhistorie: `docs/raw_md_archive/HISTORY_CANONICAL.md.raw`
- exakte Originaldateien zur Wiederherstellung: `docs/raw_md_archive/original_history_raws.zip`
- Dokumentstatus: `docs/MD_DOKUMENTENSTATUS_2026-05-20.md`
