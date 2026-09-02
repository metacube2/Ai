# RAG-Einstieg: Ladereihenfolge und Abschlussregel

Stand: 2026-09-02

## Ladereihenfolge fuer eine neue Sitzung

1. `router.md` (Repo-Wurzel) — Themenaeste und Vorrangregeln, immer zuerst
2. `lastchange.md` (Repo-Wurzel) — kompakt, nur die letzten rund 7 Tage und offene Punkte
3. Je nach Thema die passende Kurzdatei aus `docs/rag/`, etwa `FINANCE.md`,
   `PURCHASING.md`, `PROJECT.md`, `MANUAL_IMPORT.md`, `DEPLOYMENT.md`
4. `persona.md` (Repo-Wurzel) — Arbeitsregeln und fachliche Grenzen, sobald fachliche
   Verantwortung im Spiel ist

Aelteres liegt im Archiv (`docs/raw_md_archive/`) und wird nicht standardmaessig geladen.
Den vollstaendigen Bestand listet `baum.md`; das ist ein Pruefindex, keine Lesereihenfolge.

## Abschlussregel: committen und Doku nachfuehren

**Jede abgeschlossene Aufgabe endet mit einem Commit und nachgefuehrter Dokumentation.**
Nicht erst beim Deploy, nicht erst auf Nachfrage. Die verbindliche Fassung mit Begruendung
steht in `CLAUDE.md` Abschnitt 3; hier die Kurzform:

1. Committen — Code und Tests als Funktionscommit, Doku dazu oder als eigener Commit.
2. Doku vollstaendig nachfuehren. Ein angehaengter Nachtrag genuegt NICHT, solange Titel,
   Kopfstand oder fruehere Abschnitte derselben Datei etwas anderes behaupten. Ueberholte
   Aussagen als ueberholt markieren, die datierte Historie nicht umschreiben.
3. Statusquelle mitnehmen: `docs/Issue_Log_Konsolidiert_2026-08-12.tsv` und, wo betroffen,
   `projektmanagement/Wochen_Todo.tsv` samt neu erzeugter `.xlsx`.
4. **Diese Kurzdateien hier selbst pruefen.** Sie werden zu Sitzungsbeginn geladen; eine
   veraltete Aussage wirkt hier staerker als in einer Detaildatei, weil sie die naechste
   Sitzung falsch startet.
5. `docs/AGENT_COORDINATION.md` abschliessen und die Reservierung freigeben.
