# RAG Project

Stand: 2026-09-28 (Tempo, Nachtlauf, HR und Lagerwert-Verlauf ergaenzt; uebriger Kurzstand vom 2026-09-07)

Kanonischer Live-Abgleich fuer UK-2025, Supplier-Felder, Konzern-Standardkosten
und Einkauf-Delta: `docs/AKTUELLER_LIVEDATEN_STAND_2026-07-31.md`.
Bei Abweichungen hat dessen direkt gepruefter Stand Vorrang.

## Kurzstand

- **2026-09-28, nach Ingos Ferien:** Tempo der Webapp (groesster Kritikpunkt) produktiv verbessert:
  Oberflaeche erscheint sofort, Management-Cockpit lokal 41 s -> 3,3 s, Export-Dashboard 2,3 s ->
  0,07 s, Server-Einkaufsberechnung 105 s -> 24 s durch SQLite-Cache. Einkauf-Lauf fiel vom 10.09. bis
  28.09. wegen IIS-Leerlauf aus, ist per Selbstaufruf und Nachhol-Delta abgesichert und lief am 28.09.
  wieder; App-Pool-Dauerbetrieb offen als ISS-017 bei der IT. HR-Antworten von Sonja umgesetzt.
  Lagerwert-Verlauf je Woche produktiv. Finance_All-Nachtlauf weiterhin unfertig und nicht
  ausgeliefert. Details: `projektmanagement/PROJEKTSTATUS.md` PM-04, PM-07, PM-09.

- ZZPRDAT (Produktionsdatum im Fertigungsauftrag) ist gebaut und auf sieben Wegen
  nachgetestet: Paket `ZPP1`, Transportauftrag `T76K912490`, bewusst **nicht
  freigegeben**, in P76 nichts geaendert. **Am 2026-09-07 ist ein Konstruktionsfehler
  gefunden und am selben Tag behoben worden:** `BEFORE_UPDATE` lief bei jedem Sichern und
  fuellte dadurch laengst freigegebene Altauftraege mit dem heutigen Eckendtermin. Jetzt
  wird `AFKO-FTRMI` vorher gelesen; war der Auftrag schon freigegeben, passiert nichts.
  Am 2026-09-07 sind **alle sieben Wege plus der Altauftragsfall und Write-once** auf dem
  korrigierten Stand nachgemessen, direkt aus `AUFK` statt ueber den Nachweisreport, der den
  Fehler verdeckt haette; die Klasse liegt in der Aufgabe `T76K912491`. Auch der
  Diagnosebericht `Z_ZZPRDAT_CHECK` ist am 07.09.2026 korrigiert, in T76 aktiviert und
  geprueft: sachliche Momentaufnahme statt Write-once-Behauptung, Werk-/Leerfilter vor
  Trefferlimit, kein unbeschraenkter Lauf mit `p_max <= 0`. Offen bleiben das Absenden
  des Anschreibens und die fachliche Abnahme inklusive des realen Druckablaufs gegenueber
  V2. Befund und Messungen: `saptasks/ZZPRDAT_TRANSPORTPLAN.md` Abschnitt 6a. Einstieg bleibt
  dieselbe Datei.

- LIVE-PRUEFUNG 2026-07-31: UK-2025 ist produktiv vorhanden (1'867 Zeilen);
  `GroupStandardCosts` ist mit 63'506 TR-AG-Werten gefuellt; Supplier bleibt bei
  CH/AT/DE/ES komplett leer. Einkauf-Delta-Fix ist deployed, aber zum
  Pruefzeitpunkt gab es noch keinen produktiven Delta-Lauf nach dem Deploy.
  Details und genaue Vorrangregel:
  `docs/AKTUELLER_LIVEDATEN_STAND_2026-07-31.md`.

- Fuehrende App: `TrafagSalesExporter`, publiziert als `BiDashboard`.
- Die einfache Projektverwaltung ist als unterster Hauptnavigationseintrag
  `Projekte` unter `/projekte` eingebaut. Sie verwaltet Status, Prioritaet,
  Verantwortung, Termine, Fortschritt, Notizen und Archivierung in SQLite.
  Aktueller Funktions- und Lokalisierungsstand:
  `docs/EINKAUF_LOKALISIERUNG_PROJEKTSUITE_2026-08-01.md`.
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
- Dokumentstatus: `baum.md`
