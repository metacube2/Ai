# Verbindliche Arbeitsregeln fuer Claude

Diese Datei gilt fuer das gesamte Repository.

## 1. Einstieg in die Dokumentation

**`router.md` in der Repository-Wurzel ist der einzige globale Einstieg.**

Ladeweg fuer eine Aufgabe: `router.md` -> ein Unterrouter unter `docs/router/` -> die
Detaildatei. Mehr soll nicht noetig sein. Nicht wahllos Dateien aus `docs/` lesen.

`baum.md` listet den vollstaendigen Bestand und dient der Vollstaendigkeitspruefung, nicht
dem Einlesen in eine Aufgabe.

Direkt gepruefte Live-Fakten haben Vorrang vor historischen Notizen. Die uebrigen
Vorrangregeln stehen in `router.md`.

## 2. Agentenkoordination ist Pflicht

Vor jeder Analyse, Dateianderung, Codeanderung, Testausfuehrung, App-Startaktion oder jedem
Deployment zuerst `docs/AGENT_COORDINATION.md` vollstaendig lesen.

Danach:

1. Pruefen, ob ein anderer Agent den Bereich oder gemeinsame Dateien reserviert hat.
2. Vor eigener Arbeit den eigenen Auftrag, die betroffenen Dateien und den Status dort
   eintragen.
3. Keine reservierten oder fremd geaenderten Dateien ohne Abstimmung bearbeiten.
4. Beim Abschluss Ergebnis, geaenderte Dateien, Tests und Deploystatus nachtragen und die
   Reservierung freigeben.
5. Eintraege mit `abgeschlossen`, `deployed`, `frei` oder `Historie` sind keine aktuell
   laufende Arbeit.

Diese Schritte duerfen auch dann nicht uebersprungen werden, wenn der Auftrag bereits in
einem Chat beschrieben wurde.

## 3. Abschluss einer Aufgabe: committen und Doku nachfuehren

**Jede abgeschlossene Aufgabe endet mit einem Commit und nachgefuehrter Dokumentation.**
Nicht erst beim Deploy, nicht erst auf Nachfrage.

Zum Abschluss gehoeren:

1. **Committen.** Quellcode und Tests gehoeren in einen Funktionscommit, die Dokumentation
   entweder dazu oder in einen eigenen Doku-Commit. Ein produktiver Stand ohne Commit ist
   nicht reproduzierbar und hat keinen Rollback-Punkt — genau das ist beim Deploy vom
   2026-09-01 passiert und dort protokolliert.
2. **Doku nachfuehren, und zwar vollstaendig.** Es genuegt nicht, einen Nachtrag unten
   anzuhaengen. Wenn Titel, Kopfstand („Stand: ...") oder fruehere Abschnitte derselben
   Datei danach etwas anderes behaupten, ist die Datei nicht nachgefuehrt, sondern
   widerspruechlich. Ueberholte Aussagen als ueberholt markieren, statt sie stehen zu
   lassen oder die datierte Historie umzuschreiben.
3. **Die Statusquelle mitnehmen.** Aendert sich der Stand eines Issues, gehoert das in
   `docs/Issue_Log_Konsolidiert_2026-08-12.tsv` und, wo betroffen, in
   `projektmanagement/Wochen_Todo.tsv` samt neu erzeugter `.xlsx`. Nach Vorrangregel 2 ist
   der Issue-Log die gueltige Statusquelle; wenn eine Arbeitsnotiz mehr weiss als er, ist
   das ein Fehler.
4. **Die RAG-Kurzdateien pruefen.** `docs/rag/*.md` werden zu Sitzungsbeginn geladen. Eine
   veraltete Aussage wirkt dort staerker als in einer Detaildatei, weil sie die naechste
   Sitzung falsch startet.
5. **`docs/AGENT_COORDINATION.md` abschliessen** mit Ergebnis, geaenderten Dateien, Tests
   und Deploystatus, und die Reservierung freigeben.
6. **Fehler in `docs/rag/LEARNINGS.md` eintragen.** Ist bei der Aufgabe ein Fehler passiert oder
   ein alter wiederholt worden, gehoert eine Zeile dorthin, im selben Commit wie die Korrektur.

Beispiele, warum diese Regel existiert: UK galt in drei Dateien als „strukturell ohne
Lieferantenfelder", obwohl die Spalten immer gefuellt und nur nie gemappt waren. Das
spanische Buchungsdatum war seit dem 2026-08-26 entschieden und deployed, waehrend Titel und
Abschnitt 1 derselben Datei weiter „fehlendes Buchungsdatum" sagten und der Issue-Log neun
Tage hinterherhinkte. Beides ist am 2026-09-02 aufgefallen und korrigiert worden.

## 4. Arbeitsweise

Arbeitsregeln, Testerwartungen und fachliche Grenzen stehen in `persona.md`.

Bereichsspezifische `CLAUDE.md`-Dateien, zum Beispiel `zlo03/CLAUDE.md`, gelten zusaetzlich
zu dieser Root-Datei.
