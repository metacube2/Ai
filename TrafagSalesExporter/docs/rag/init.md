# RAG-Einstieg: Ladereihenfolge und Abschlussregel

Stand: 2026-09-11

## Ladereihenfolge fuer eine neue Sitzung

1. `router.md` (Repo-Wurzel) — Themenaeste und Vorrangregeln, immer zuerst
2. Der passende Unterrouter unter `docs/router/`, von dort die Detaildatei oder die
   RAG-Kurzdatei aus `docs/rag/` (etwa `FINANCE.md`, `PURCHASING.md`, `PROJECT.md`,
   `MANUAL_IMPORT.md`, `DEPLOYMENT.md`). Das ist derselbe Ladeweg wie in `router.md`
   und `CLAUDE.md` (angeglichen am 2026-09-29).
3. `lastchange.md` (Repo-Wurzel) nur bei Bedarf: Aenderungsstand mit dem juengsten Abschnitt
   oben, aber **nicht** auf sieben Tage begrenzt, sondern rund 2'000 Zeilen einschliesslich
   Juli-Historie
4. `persona.md` (Repo-Wurzel) — Arbeitsregeln und fachliche Grenzen, sobald fachliche
   Verantwortung im Spiel ist
5. **Sobald SAP im Spiel ist: `saptasks/SAP_ARBEITSWEISE_UND_WERKZEUGE.md`, vor der
   ersten Aktion.** Dort steht, dass ein Agent SAP vollstaendig fernsteuern kann —
   Quelltext lesen und schreiben, Klassen aktivieren, DDIC-Objekte anlegen, Reports
   ausfuehren, OData ohne Anmeldung pruefen — und welche Fallen dabei Stunden kosten.
   Ohne diese Datei beginnt jede SAP-Aufgabe wieder mit der Frage, ob das ueberhaupt
   geht. Am 2026-09-10 hat allein `Sessions=0` zwei Stunden gekostet, weil die Ursache
   am Server lag und nicht am Arbeitsplatz.

Aelteres liegt im Archiv (`docs/raw_md_archive/`) und wird nicht standardmaessig geladen.
Den vollstaendigen Bestand listet `baum.md`; das ist ein Pruefindex, keine Lesereihenfolge.

Ist unklar, in welcher Datei eine Aussage steht, gibt es zusaetzlich das MCP-Werkzeug
`qdrant-find`: eine semantische Suche ueber alle Markdown-Dateien, eingerichtet in
`Tools/Qdrant/`. Sie ersetzt die Ladereihenfolge nicht, sondern findet den Einstiegspunkt,
und ihre Treffer sind Hinweise, keine Belege — die genannte Datei danach vollstaendig
lesen. Antwortet sie nicht, laeuft Qdrant nicht (`Tools/Qdrant/Start-Qdrant.ps1`).

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
6. Wurde Dokumentation geaendert: `Tools/Qdrant/Reindex-Docs.ps1` ausfuehren. Der
   Suchindex wird nicht automatisch nachgefuehrt und widerspricht sonst still den
   Dateien, die er abbildet — dieselbe Falle wie Punkt 4, nur eine Ebene tiefer.
