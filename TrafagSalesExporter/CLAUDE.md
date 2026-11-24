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

**Eine Ausnahme, und nur diese:** Eine Kurzauskunft nach Abschnitt 3a aendert nichts und
braucht deshalb weder Lektuere noch Eintrag. Sobald doch etwas geaendert wird, gilt dieser
Abschnitt vollstaendig, auch mitten in der Arbeit.

`docs/AGENT_COORDINATION.md` enthaelt seit dem 2026-09-09 nur laufende Arbeit und einen
Kurzindex der letzten sieben Tage. Abgeschlossenes steht im Volltext in
`docs/AGENT_COORDINATION_HISTORIE.md`; die ist Nachschlagewerk und keine Pflichtlektuere.
Abgeschlossene Eintraege gehoeren beim naechsten Aufraeumen dorthin, nicht in die
Hauptdatei.

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

### 3a. Kurzauskunft: Antwort im Chat, ohne Commit und ohne Nachfuehrung

Die Abschlussregel gilt fuer **erledigte Aufgaben**, nicht fuer Fragen. Eine reine Frage
ist mit einer Antwort im Chat erledigt.

**Kurzauskunft** heisst: im Repository und im Produktivsystem aendert sich nichts. Kein
Commit, keine Doku, kein Issue-Log, kein Eintrag in `docs/AGENT_COORDINATION.md`, und die
Koordinationsdatei muss dafuer auch nicht vorher gelesen werden. Erlaubt sind Lesen,
Suchen, Messen und Rechnen; Dateien unter `.tmp_tools/` oder im Scratchpad zaehlen nicht
als Aenderung.

Wann welcher Modus gilt:

| Fall | Modus |
| --- | --- |
| Ingo sagt „nur Auskunft", „nur schauen", „nur sagen" | Kurzauskunft |
| Ingo sagt „vollstaendig" oder „nachdokumentieren" | vollstaendiger Abschluss nach Abschnitt 3 |
| Nichts gesagt, und die Antwort aendert keine Datei | Kurzauskunft |
| Nichts gesagt, aber Code, Daten, Produktivstand oder eine Dokumentationsdatei aendern sich | vollstaendiger Abschluss nach Abschnitt 3 |

Zwei Grenzen, damit der Modus nichts verschluckt:

1. **Ein dauerhafter Befund gehoert festgehalten, auch wenn die Frage kurz war.** Eine
   Messung, ein Entscheid oder ein widerspruechlicher Doku-Stand, der beim Nachsehen
   auffaellt, wird im Chat ausdruecklich benannt, mit dem Angebot, ihn nachzufuehren. Was
   nur im Chat steht, ist fuer die naechste Sitzung verloren; das Angebot ersetzt also
   nicht die Nachfuehrung, es ueberlaesst Ingo nur die Entscheidung darueber.
2. **Sobald doch etwas geaendert wird, gilt Abschnitt 2 und 3 vollstaendig.** Ein
   angefangener Fix ist keine Kurzauskunft mehr, auch wenn die Frage klein anfing.

Grund fuer die Regel, festgehalten am 2026-09-09: Ohne diesen Modus kostete jede kleine
Frage denselben Abschlussaufwand wie ein Produktivfix, weil es nur „alles nachfuehren"
oder „Regel brechen" gab.

## 4. Arbeitsweise

Arbeitsregeln, Testerwartungen und fachliche Grenzen stehen in `persona.md`.

Bereichsspezifische `CLAUDE.md`-Dateien, zum Beispiel `zlo03/CLAUDE.md`, gelten zusaetzlich
zu dieser Root-Datei.

## 5. Deploy: immer mit dem Deploy-Werkzeug

Wunsch Ingo vom 2026-11-02: **Deployt wird immer mit dem Werkzeug**, ohne dass Ingo es jedes Mal
erwaehnen muss. Der Aufruf geht auch im Auto-Modus durch (am 2026-11-02 so gelaufen), wenn er genau
so und allein abgesetzt wird, als Bash aus dem Projektordner:

```
dotnet run --project .tmp_tools/DeployHeadless -c Release > .tmp_deploy_<JJJJ-MM-TT>_<thema>.log 2>&1
```

Kein `;`, `&&`, `|`, kein vorangestelltes `cd`, kein zweiter Befehl daneben. Nie selbst
`dotnet publish` aufrufen.

Vorher:

1. Release-Worktree auf den Commit ziehen (`git checkout --detach <commit>`) und dort die Tests
   gruen laufen lassen.
2. Pruefen, dass kein Einkauf-Delta laeuft. Laesst sich das nicht pruefen, Ingo fragen.
3. Monitor-Fenster oeffnen.

Nachher: Zeile mit „Commit“ im Protokoll gegen den gewuenschten Commit pruefen. Der Alarm
„-wal/-shm verschwunden“ ist harmlos, wenn beide Dateien wieder da sind.

Einzelheiten stehen in `docs/rag/DEPLOYMENT.md` oben und in `router.md` im Abschnitt Auto-Modus.
