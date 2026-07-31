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

- DEPLOYED 2026-07-17 (Commit `3a4efb5`, `257/257` Tests gruen, DLL `17.07.2026 10:05:07`, Laenge `3'006'464`, Port 443 erreichbar): EINKAUF/Spend-Reiter hat einen Drilldown Lieferant -> Warengruppe/Jahr bekommen (Feedback-Runde Marco/Armin, Leitplanke "ein Punkt nach dem anderen"). Warengruppe folgt Marcos Vorgabe aus dem Materialstamm (`MaraMatkl`, neue additive Spalte), Fallback auf die Beleg-Warengruppe solange SAP `Matkl` im MARA-Set nicht liefert. WICHTIGER NEBENBEFUND, produktionskritisch behoben: SAP hat das MARA-Set umgebaut, `MARA001Set` liefert `Mstae` nicht mehr (404) — der naechste Einkauf-Full-Load/Delta waere sonst fehlgeschlagen; Fix liest jetzt `maracalcSet` (ungepagt, wie `mbewSet` ignoriert es `$top`/`$skip`). ABC/XYZ-Weg geklaert (MARC-MAABC + XYZ-Tabelle + vorhandener Report), Umsetzung bewusst erst nach Spend-Abnahme. NACHSORGE: Einkauf Full Load einmal laufen lassen, damit `Mstae` wieder gefuellt wird. Details: `docs/PURCHASING_DASHBOARD_2026-06-05.md` Nachtrag 2026-07-17, `docs/PURCHASING_DASHBOARD_UMSETZUNGSPLAN_MARCO_2026-07-09.md`.
- DEPLOYED 2026-07-15 (Teil 2, Commit `5efeed7`, `240/240` Tests, DLL `15.07.2026 11:22:32`, Laenge `2'947'584`, Port 443 erreichbar): TR AG als liefernde Gesellschaft fuer die Gruppenmarge angebunden (neue Tabelle `GroupStandardCosts`, MBEW-STPRS Bewertungskreis 1100, befuellt beim CH/AT-SAP-Import; Lieferant->Gesellschaft ueber `SupplierName`-Klartext). TR-AG-gelieferte Zeilen nutzen jetzt die echte Konzernkostenbasis statt lokaler Verkaufszeilen-Kosten, egal welches Land verkauft. DB unveraendert (neue Tabelle additiv beim App-Start). TR IN/TR IT bleiben offen (TR IT live geprueft: SAP B1 pflegt keinen Standardkosten-Wert je Material; TR IN vom Entwicklungsrechner nicht erreichbar). Details: `docs/rag/FINANCE.md`, `docs/FINANCE_GRUPPENMARGE_2026-06-16.md`.
- DEPLOYED 2026-07-15 (Commits `3838a16` + `08f5572`, `226/226` Tests, DLL `15.07.2026 08:53:47`, Laenge `2'935'296`, Port 443 erreichbar): (1) Schalter `GroupMarginCostCurrencyMode` (Mask/Convert) fuer Gruppenmarge bei abweichender Kostenwaehrung — wirkt identisch auf Dashboard/Pruefbuch und zentrale Excel/Nachweis-Excel; Fachentscheid D fuer Andreas damit per Vergleich beider Varianten entscheidbar. (2) Zentrales `Sales_All_*.xlsx` enthaelt neu die Blaetter `Gruppenmarge Summary`/`Gruppenmarge Details`. (3) HR-KPI: konfigurierbare Krankenquote-Ampelschwellen + rot/Error-Stufe, Von/Bis-Range ohne explizites Jahr zeigt Prognosekacheln. DB unveraendert (neue Spalte additiv beim App-Start, Default `Mask`). NICHT geloest: Konzern-STPRS der liefernden Gesellschaft (Andreas A/B, siehe `docs/rag/FINANCE.md` „Offene Fachpunkte"). Details: `docs/rag/FINANCE.md`, Nachtrag in `docs/FINANCE_GRUPPENMARGE_2026-06-16.md`.


- Neu umgesetzt und deployed 2026-07-14: Journal-Import (Hauptbuch) als eigener Import in die separate Tabelle `FinancialJournalEntries` — Seite `Finance Cockpit > Journal Import` (`/finance-journal-import`, Seed `finance-journal-import`). Umfang: alle SAP-B1-Gesellschaften ueber HANA (FR, IT, US, Indien) plus CH/AT (`ZSCHWEIZ`) ueber SAP OData. Der Sales-Datenfluss bleibt unveraendert. Offene Abhaengigkeit: das OData-EntitySet `FinanzJournalSet` fuer CH/AT existiert auf SAP-Seite noch nicht (Spez fuer das SAP-Team: `docs/FINANCE_JOURNAL_SAP_ODATA_SPEZ_2026-07-14.md`). Details/Feldmapping: `docs/FINANCE_B1_JOURNAL_IMPORT_2026-07-14.md`, Kurzstand: `docs/rag/FINANCE.md`.
- Neu lokal umgesetzt 2026-07-13: Finance-Daten-Heartbeat im Management-Cockpit. Neuer Experten-Reiter `Daten-Heartbeat` / `Data heartbeat` unter `management-cockpit?section=heartbeat`, Navigation-Seed `finance-heartbeat`. Inline-SVG visualisiert Tageszeilen je TSC/Land und trennt echte Update-Luecken von Wochenenden/normalen Nicht-Buchungstagen; Excel-Export und Tests ergaenzt. Details: `docs/rag/FINANCE.md`.
- Fuehrende App: `TrafagSalesExporter`, publiziert als `BiDashboard`.
- Nahtloser Einstieg nach Chatwechsel: diese Datei (`docs/rag/PROJECT.md`) ist jetzt selbst der laufend gepflegte Kurzstand.
- NOCH OFFEN (Finance, eigene Features): echte Konzern-Standardkosten je Liefergesellschaft (MBEW-STPRS / SAP B1) fuer korrekte Gruppenmarge gemaess `Mappe1.xlsx`; Budget-CHF-Spaltenumfang (a.docx Q3).
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
