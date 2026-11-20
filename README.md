# Agenten-Monitor

Kleines Windows-Fenster, das live zeigt, was jede Claude-Code-Sitzung auf diesem Rechner gerade tut:
Hauptagent und Unteragenten, jede Aktion mit Balken und Prozent, Aufgaben-Fortschritt, Planverbrauch
(5 Stunden und Woche) und Tokens des Tages. Gebaut am 17.11.2026 auf Wunsch von Ingo.

Stand: 17.11.2026 abends. 49 Tests grün. Installiert und im Autostart auf Ingos Windows-Laptop.
Nachtrag 20.11.2026: Paket für andere Benutzer (ZIP mit Laufzeit, Einrichtungsskript, Anleitung auch für Claude), erstes Paket für Fabio, siehe „Paket für andere Benutzer“.

| Ort | Inhalt |
| --- | --- |
| `C:\Users\koi\source\repos\AgentMonitor` | eigenes Repo, hier wird entwickelt |
| GitHub `metacube2/Ai`, Branch `werkzeuge/agentmonitor-videodoku`, Ordner `AgentMonitor/` | Kopie mit Verlauf (git subtree), **öffentlich**, noch nicht in `main` |
| Mac: `/Applications/Agenten-Monitor.app`, Repo `agentmonitor-mac` (privat, GitHub und Gitea) | Swift-Fassung mit derselben Logik, gebaut von der Mac-Sitzung *lagoon*; am 18.11. auf den Windows-Stand `598b536` gebracht: 89 Tests, Commit `f46de38`, Build 6 installiert. Bewusste Abweichungen stehen in der Mac-README (u. a. Bash und PowerShell teilen einen Lernschlüssel, Klänge aus `/System/Library/Sounds`, Statuszeile alle 10 s) |

Nachziehen in den Branch: im Arbeitsverzeichnis `..\Ai-werkzeuge`
`git subtree pull --prefix=AgentMonitor C:/Users/koi/source/repos/AgentMonitor main`, dann `git push`.

### Offen (Stand 17.11. abends)

| Punkt | Stand |
| --- | --- |
| Planverbrauch mit echten Werten | seit 17.11. 16:40 da (Woche 22–23 %). Das 5-Stunden-Fenster meldet Claude Code fuer Ingos Abo nicht (laut Berichten Dritter fiel es am 30.10.2026 fuer Ultra weg); der Monitor zeigt dann „wird für dein Abo nicht gemeldet“ |
| Klänge | eingetragen, von Ingo noch nicht gehört |
| Mac: Hooks und Statuszeile | Ingo führt `werkzeuge/hooks-einrichten.sh` in der Sitzung *lagoon* selbst aus |
| Merge nach `main` | Entscheid Ingo |

## Was er zeigt

| Bereich | Inhalt | Quelle |
| --- | --- | --- |
| Sitzung | Projekt (letzter Ordner von `cwd`), Titel, Modell (z. B. „Fable 5.2“) | Hooks, Statuszeile |
| Agent | Phase: denkt / arbeitet / wartet auf dich / fertig / Fehler / bereit, Laufzeit, Ergebnis (erste Zeile der letzten Antwort) | Hooks |
| Aufgabenbalken | echter Fortschritt „3/7 Aufgaben“, Zahl im Balken; bei Fable dunkleres Grün | `TaskCreated`/`TaskCompleted`, `TodoWrite` |
| Rundenbalken | ohne Aufgabenliste: geschaetzte Dauer der laufenden Runde (Auftrag bis fertig, gelernt je „Runde:main“ bzw. Unteragent-Typ, Startwert 120 s); verpasster Beginn zaehlt ab dem ersten Werkzeug und lernt nicht | `UserPromptSubmit`/`SubagentStart` bis `Stop`/`SubagentStop` |
| Aktion | deutsche Beschreibung („liest X“, „sucht „Y“ in Z“), Befehl als Detailzeile, Dauer, Balken mit Prozent | `PreToolUse`/`PostToolUse` |
| Planverbrauch | Balken 5 Stunden und Woche, Erneuerungszeit; grün < 70 %, orange ab 70 %, rot ab 90 %; letzter Stand bleibt nach Neustart („Stand von …“) | Statuszeile (`rate_limits`) |
| Tokens heute | Summe Eingabe, Ausgabe, Cache schreiben, Cache lesen | Protokolle `~/.claude/projects/**/*.jsonl` |

### Prozent je Aktion ist eine Schätzung

Claude Code meldet je Werkzeug keinen Fortschritt. Der Balken einer Aktion füllt sich deshalb nach der
gelernten Dauer gleichartiger Aktionen („wie beim Kopieren im Norton Commander“, Wunsch Ingo):

- Art einer Aktion: Werkzeugname; bei Befehlen die ersten zwei Wörter des ersten echten Teilbefehls
  (`cd`, Zuweisungen und `$…` werden übersprungen), bei Unteragenten der Typ.
- Gelernt wird die Wanduhrzeit vom Anfordern bis zum Ende (inklusive Sicherheitsprüfung des Auto-Modus),
  Median der letzten 25 Läufe. Fehlschläge lernen nicht mit.
- Rückfall: Median desselben Werkzeugs, sonst Startwert (Read 1 s, Write/Edit/Grep 2 s, Bash/Web 10 s,
  Agent 120 s, sonst 5 s).
- Verlauf: bis zur Schätzung linear bis 90 %, danach langsam gegen höchstens 95 %. 100 % erst beim echten Ende.

### Tokens heute sind eine Auskunft

Das Zeilenformat der Protokolle ist von Anthropic nicht dokumentiert, nur beobachtet (`type: assistant`,
`message.usage`). Gezählt wird je `message.id` einmal (Streaming schreibt Antworten mehrfach), nur mit
Zeitstempel von heute, inkrementell. Massgeblich für das Limit sind die Prozentbalken aus der Statuszeile.

## Bedienung

- Fenster: Nadel = immer offen bzw. aufpoppen; Minimieren, Maximieren (auch Doppelklick auf die Kopfzeile),
  Schliessen = ausblenden. Grösse an allen Rändern ziehbar.
- Aufpoppen: erscheint ohne Fokus zu stehlen, sobald ein Agent denkt oder arbeitet, blendet sich 8 s nach dem
  Ende aus (nicht, solange die Maus darüber ist oder es minimiert ist).
- Vergrössern: Strg+Mausrad, Strg+Plus/Minus, Strg+0 (140 %), Tray-Menü „Grösse“ (100–250 %).
- Tray-Menü: Anzeigen, Modus, Grösse, Im Vordergrund, Klänge, Klänge auswählen …, Beim Anmelden starten, Beenden.
- Klänge: eigene Windows-Klangereignisse „Agent fertig“, „Agent wartet auf dich“, „Agent mit Fehler
  abgebrochen“, „Aufgabe erledigt“ (Letzteres standardmässig stumm). Auswahl in der Systemsteuerung unter
  Sound › Sounds › Programm „Agenten-Monitor“.

## Einrichtung (so auf Ingos Laptop)

1. App: `dotnet publish src/AgentMonitor.App -c Release -o %LOCALAPPDATA%\AgentMonitor\app`
2. Statuszeile: `dotnet publish src/AgentMonitor.StatusLine -c Release -r win-x64 --self-contained false -p:PublishReadyToRun=true -o %LOCALAPPDATA%\AgentMonitor\status`
3. In `~/.claude/settings.json`:
   - `hooks`: für `UserPromptSubmit`, `PreToolUse`, `PostToolUse`, `PostToolUseFailure`, `SubagentStart`,
     `SubagentStop`, `Stop`, `StopFailure`, `Notification`, `TaskCreated`, `TaskCompleted`, `PreCompact`,
     `PostCompact`, `SessionEnd` je `{"type":"http","url":"http://127.0.0.1:47811/hook","timeout":2}`.
     **Nicht** `SessionStart`: dafür unterstützt Claude Code keine HTTP-Hooks.
   - `statusLine`: `{"type":"command","command":"…\\AgentMonitor\\status\\AgentMonitorStatus.exe","refreshInterval":15}`
4. Beim ersten Start trägt sich der Monitor in den Autostart ein (`HKCU\…\Run`, `--hidden`).

Wichtig: Läuft der Monitor nicht, arbeitet Claude normal weiter, zeigt aber bei jedem Schritt einen Hinweis
„Hook error … ECONNREFUSED“ (getestet). Deshalb Autostart. Laufende Sitzungen übernehmen eine neue
Statuszeile erst nach einem Neustart.

## Paket für andere Benutzer (seit 20.11.2026)

Für Kolleginnen und Kollegen ohne Entwicklungsumgebung (erster Empfänger: Fabio). Ordner `paket/`:

| Datei | Zweck |
| --- | --- |
| `Baue-Paket.ps1` | baut App und Statuszeile eigenständig (`win-x64`, mit .NET-Laufzeit, ReadyToRun), legt Skripte und Anleitung dazu und schreibt das ZIP nach `Downloads\AgentMonitor_Paket_<Datum>.zip` (rund 80 MB, entpackt 196 MB, ohne `.pdb`) |
| `Setup-AgentMonitor.ps1` / `.cmd` | kopiert nach `%LOCALAPPDATA%\AgentMonitor\app`, führt Hooks (14 Ereignisse) und `statusLine` in `~/.claude/settings.json` **zusammen** (Sicherung vorher, fremde Hooks und fremde Statuszeile bleiben, `-Force` ersetzt die Statuszeile, `-NoStart` für Tests), startet den Monitor, prüft `/health` |
| `Entfernen-AgentMonitor.ps1` / `.cmd` | nimmt nur die eigenen Einträge aus der `settings.json`, Autostart und Programm weg |
| `ANLEITUNG.md` | für den Benutzer und für Claude (was er ist, was nie angefasst werden darf, ECONNREFUSED, Einrichten durch Claude, Fehlersuche) |

Geprüft am 20.11. gegen Kopien von Ingos `settings.json` und eine Testdatei mit fremden Hooks: außer `hooks` und
`statusLine` bleibt jeder Schlüssel gleich, ein zweiter Lauf ändert nichts, Entfernen lässt Fremdes stehen, die Datei
wird ohne BOM geschrieben. **Nicht geprüft:** ein Lauf auf einem anderen Rechner als Ingos Laptop (SmartScreen,
Skript-Sperre über `.cmd`, andere Claude-Code-Version).

Skripte bleiben reines ASCII (Windows PowerShell 5.1 liest BOM-lose Dateien als ANSI), `.cmd` mit CRLF.
Ein Test der Statuszeile darf nie gegen Port 47811 laufen, wenn ein Monitor offen ist (er übernimmt die Werte als
Planverbrauch): Umgebungsvariable `AGENTMONITOR_PORT=47999` setzen.

## Aufbau

| Projekt | Inhalt |
| --- | --- |
| `AgentMonitor.Core` | `HookEvent` (Ereignis lesen), `MonitorState` (Sitzung → Agenten → Aktionen, Phasen, Aufgaben, Planfenster, Klang-Signale, Aufräumen), `ToolSummary` (Beschreibungen), `DurationEstimator` (Schätzung), `StatusInfo` (Statuszeile), `TokenCounter` (Protokolle) |
| `AgentMonitor.App` | WPF-Fenster, Kestrel-Empfang auf `127.0.0.1:47811` (`POST /hook`, `POST /status`, `GET /health`, Antwort immer 200 ohne Inhalt), Tray, Klänge, Einstellungen |
| `AgentMonitor.StatusLine` | `AgentMonitorStatus.exe`: liest das Statuszeilen-JSON, gibt es an `/status` weiter, schreibt „Fable 5.2 · 5h 34 % · Woche 12 % · Kontext 45 %“ |
| `AgentMonitor.Tests` | 47 xUnit-Tests |

Dateien unter `%APPDATA%\AgentMonitor`: `settings.json`, `dauern.json` (nur Aktionsart und Sekunden),
`planverbrauch.json`. Nichts verlässt den Rechner; Befehle und Pfade werden nur angezeigt, nicht gespeichert.

## Fallstricke (gelernt)

- `.gitignore` von `dotnet new gitignore` enthält `*.app` (macOS) und erfasst unter Windows den Ordner
  `src/AgentMonitor.App`. Ausnahme `!src/AgentMonitor.App/` ist eingetragen. Nach Commits mit `git ls-files` prüfen.
- HTTP-Hooks dürfen nie Inhalt zurückgeben (bei `UserPromptSubmit` würde er zu Kontext).
- Rahmenloses WPF-Fenster (`WindowStyle=None`): Ohne `WindowChrome` lässt es sich nur über die Ecke ziehen, und
  Minimieren/Maximieren fehlen. Beides hat Ingo vermisst; Maximieren füllt von Hand den Arbeitsbereich des
  aktuellen Bildschirms. Per UI-Automation nachgemessen (Bildschirm links 1920×1032, Wiederherstellen exakt).
- `statusLine.command` laeuft unter Windows in der Git-Bash: Backslashes im Pfad werden verschluckt („command not found“), die Statuszeile lief deshalb bis 17.11. 16:40 nie. Immer Schraegstriche: `C:/Users/…/AgentMonitorStatus.exe`.
- Prozentzahl im Balken: weiss nur auf dunklen Fuellungen (`BarBlue` #1D4ED8, `BarGreen` #047857, `BarOrange` #B45309, `BarRed` #B91C1C), auf den hellen Phasenfarben war sie unlesbar.
- Die Statuszeile von Claude Code (`statusLine`) wird bei jeder Antwort aufgerufen: Programm muss schnell starten
  (ReadyToRun, rund 0,2 s) und darf nie leer ausgeben, sonst blendet Claude die Zeile aus.
- Probewerte nie stehen lassen: Nach einem Test mit erfundenen Planwerten Monitor neu starten, sonst zeigt er
  sie als echten Verbrauch an.
- Diese Hooks und die Statuszeile gelten global für alle Claude-Sitzungen auf dem Laptop, auch fürs Dashboard
  (Hinweis in `TrafagSalesExporter/docs/AGENT_COORDINATION.md`).
