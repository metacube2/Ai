# Agenten-Monitor: Paket zum Einrichten

Kleines Windows-Fenster, das live zeigt, was jede Claude-Code-Sitzung auf deinem Rechner gerade tut: Hauptagent und
Unteragenten, jede Aktion mit Balken und Prozent, Aufgaben-Fortschritt, Planverbrauch (5 Stunden und Woche) und Tokens
des Tages. Er hört nur zu. Claude arbeitet genauso weiter, wenn er läuft oder nicht.

Gebaut von Ingo Kohler (Trafag). Dieses Paket enthält das Programm samt .NET-Laufzeit. Du musst nichts installieren und
brauchst keine Administratorrechte.

## Einrichten

1. ZIP entpacken (irgendein Ordner, zum Beispiel `C:\Tools\AgentMonitor_Paket`). Den Ordner nach dem Einrichten nicht
   mehr brauchen; das Programm wird nach `%LOCALAPPDATA%\AgentMonitor\app` kopiert.
2. **`Setup-AgentMonitor.cmd` doppelklicken.** Falls Windows warnt („Der Computer wurde durch Windows geschützt“):
   „Weitere Informationen“ und „Trotzdem ausführen“. Das Programm ist nicht signiert.
3. Das Skript kopiert das Programm, trägt in `~\.claude\settings.json` die Hooks und die Statuszeile ein (vorher wird
   eine Sicherung `settings.json.before-agentmonitor-<Zeit>.bak` angelegt), und startet den Monitor.
4. **Alle offenen Claude-Sitzungen neu starten.** Hooks und Statuszeile gelten erst für neue Sitzungen.

Das Skript kann beliebig oft laufen (Aktualisierung). Bestehende Hooks anderer Werkzeuge in der `settings.json` bleiben
erhalten. Gibt es schon eine andere Statuszeile, bleibt sie, und das Skript meldet das; mit
`Setup-AgentMonitor.cmd -Force` ersetzt man sie, dann fehlt aber deren Anzeige.

## Prüfen

- Im Tray (Taskleiste unten rechts) erscheint das Symbol „Agenten-Monitor“, und das Fenster öffnet sich, sobald ein
  Agent arbeitet.
- Test ohne Claude: im Browser `http://127.0.0.1:47811/health` öffnen. Antwort 200 (leere Seite) heisst: läuft.
- In einer neuen Claude-Sitzung eine Frage stellen: Der Monitor zeigt „denkt“ und danach jede Aktion. Unten in Claude
  Code steht eine Zeile wie „Modell · 5h 34 % · Woche 12 % · Kontext 45 %“.

## Bedienung (kurz)

- Fenster: Nadel = immer im Vordergrund; es erscheint ohne Fokus zu stehlen, sobald ein Agent arbeitet, und blendet
  sich 8 Sekunden nach dem Ende aus. Schliessen blendet nur aus (Beenden über das Tray-Menü).
- Vergrössern: Strg + Mausrad, Strg + Plus/Minus, Strg + 0.
- Tray-Menü: Anzeigen, Modus, Grösse, Im Vordergrund, Klänge, „Beim Anmelden starten“, Beenden.
- Klänge für „Agent fertig“, „wartet auf dich“, „Fehler“: Systemsteuerung › Sound › Sounds › Programm „Agenten-Monitor“.
- Der Prozentbalken einer Aktion ist eine Schätzung nach der Dauer gleichartiger früherer Aktionen (der Monitor lernt
  sie). 100 % gibt es erst beim echten Ende.
- Den Planverbrauch (5 Stunden und Woche) meldet Claude Code nur bei manchen Abos. Fehlt er, steht dort
  „wird für dein Abo nicht gemeldet“. Das ist kein Fehler des Monitors.

## Entfernen

`Entfernen-AgentMonitor.cmd` doppelklicken. Es nimmt nur die eigenen Hooks und die eigene Statuszeile aus der
`settings.json` (Sicherung vorher), entfernt den Autostart und löscht das Programm. Einstellungen und gelernte
Dauern bleiben unter `%APPDATA%\AgentMonitor`. Danach Claude-Sitzungen neu starten.

## Für Claude: was du über den Monitor wissen musst

Diese Abschnitte sind für Claude-Sitzungen auf diesem Rechner geschrieben. Wer eine Sitzung bittet, den Monitor zu
betreuen, kann auf diese Datei verweisen.

**Was er ist.** Ein lokaler Empfänger auf `127.0.0.1:47811`. Claude Code schickt ihm über HTTP-Hooks jedes Ereignis
(Auftrag, Werkzeugaufruf, Ende) und über die Statuszeile Modell und Planverbrauch. Der Monitor gibt nie Inhalt zurück,
speichert keine Befehle oder Pfade und sendet nichts ins Netz. Gespeichert werden nur Einstellungen, die gelernten
Dauern (Aktionsart und Sekunden) und der letzte Planverbrauch unter `%APPDATA%\AgentMonitor`.

**Was du beachten musst.**

- Die Hooks und die Statuszeile stehen in der **globalen** `~/.claude/settings.json` und gelten für alle Sitzungen und
  Projekte auf diesem Rechner. Nicht entfernen, nicht umformatieren, nicht überschreiben. Änderungen an der
  `settings.json` immer als Zusammenführen (nur den eigenen Schlüssel ändern), nie als Neuschreiben der Datei.
- „**Hook error … ECONNREFUSED**“ bei jedem Schritt heisst: Der Monitor läuft nicht. Claude arbeitet normal weiter. Dann
  den Monitor starten (`%LOCALAPPDATA%\AgentMonitor\app\AgentMonitor.exe`, am besten über das Tray-Symbol oder Autostart)
  und **nicht** die Hooks löschen.
- `statusLine.command` läuft unter Windows in der Git-Bash. Pfade dort immer mit Schrägstrichen
  (`C:/Users/…/AgentMonitorStatus.exe`), Backslashes werden verschluckt.
- Nicht `SessionStart` als HTTP-Hook eintragen; dafür unterstützt Claude Code keine HTTP-Hooks.
- Die Statuszeile wird bei jeder Antwort gestartet. Sie muss schnell sein und darf nie leer ausgeben, sonst blendet
  Claude Code sie aus. Nichts an `AgentMonitorStatus.exe` ändern.
- Erfundene Probewerte für den Planverbrauch nie stehen lassen: Nach einem Test den Monitor neu starten.

**Einrichten, wenn der Benutzer dich darum bittet.**

1. Prüfen, ob das ZIP vollständig entpackt ist (`bin\AgentMonitor.exe` neben `Setup-AgentMonitor.ps1`).
2. In PowerShell ausführen: `powershell -NoProfile -ExecutionPolicy Bypass -File .\Setup-AgentMonitor.ps1`
   (kein Passwort, kein Administrator nötig).
3. Die Ausgabe lesen: „Hooks: 14 neu“ oder „14 waren schon da“, „Statuszeile eingetragen/aktualisiert“ oder eine
   Warnung zu einer fremden Statuszeile (dann den Benutzer fragen, ob mit `-Force` ersetzt werden soll), „Monitor läuft
   (Health 200)“.
4. Dem Benutzer sagen, dass offene Claude-Sitzungen neu gestartet werden müssen.
5. Bei Fehlern: nichts von Hand in der `settings.json` flicken; die Sicherung `settings.json.before-agentmonitor-*.bak`
   zeigt den Stand vor dem Skript.

**Fehlersuche.**

| Beobachtung | Ursache und Schritt |
| --- | --- |
| Fenster bleibt leer | Sitzung wurde vor dem Einrichten gestartet: Claude neu starten |
| `Hook error … ECONNREFUSED` | Monitor läuft nicht: Tray prüfen, `AgentMonitor.exe` starten |
| `http://127.0.0.1:47811/health` antwortet nicht | Port belegt oder Monitor beendet: Prozess `AgentMonitor` im Task-Manager prüfen, neu starten |
| Statuszeile fehlt | Sitzung neu starten; `settings.json` muss `statusLine.command` mit Schrägstrichen enthalten |
| Keine 5h-/Wochenbalken | Abo meldet das Fenster nicht oder noch keine Antwort seit dem Start: normal |
| Skript meldet „bin fehlt“ | ZIP nicht vollständig entpackt |
| SmartScreen-Warnung | unsigniertes Programm: „Weitere Informationen“, „Trotzdem ausführen“ |

## Technik

| Teil | Inhalt |
| --- | --- |
| `bin\AgentMonitor.exe` | WPF-Fenster, Tray, Empfänger (`POST /hook`, `POST /status`, `GET /health`), nur auf 127.0.0.1, Port 47811 |
| `bin\AgentMonitorStatus.exe` | Statuszeile: liest das JSON von Claude Code, gibt es an den Monitor weiter, schreibt die Zeile |
| Autostart | `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, Wert `AgentMonitor` (trägt der Monitor beim ersten Start selbst ein) |
| Einzelinstanz | ein zweiter Start beendet sich |
| Einstellungen | `%APPDATA%\AgentMonitor\settings.json`, `dauern.json`, `planverbrauch.json` |
| Hooks | 14 Ereignisse als `{"type":"http","url":"http://127.0.0.1:47811/hook","timeout":2}` |

Quellcode, Tests und Verlauf: Repo `AgentMonitor` (Ingo Kohler); dort steht in der README die ausführliche Fassung.
