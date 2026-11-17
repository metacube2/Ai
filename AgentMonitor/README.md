# Agenten-Monitor

Kleines Windows-Fenster, das live zeigt, was jede Claude-Code-Sitzung auf diesem Rechner gerade tut:
Hauptagent und Unteragenten, jede Aktion mit Balken und Prozent, Aufgaben-Fortschritt, Planverbrauch
(5 Stunden und Woche) und Tokens des Tages. Gebaut am 17.11.2026 auf Wunsch von Ingo; eine Mac-Fassung in
Swift mit derselben Logik baut die Mac-Sitzung *lagoon* (`~/Projects/Git/agentmonitor-mac`).

Stand: 17.11.2026, Commit siehe `git log`. 47 Tests grün.

## Was er zeigt

| Bereich | Inhalt | Quelle |
| --- | --- | --- |
| Sitzung | Projekt (letzter Ordner von `cwd`), Titel, Modell (z. B. „Fable 5.2“) | Hooks, Statuszeile |
| Agent | Phase: denkt / arbeitet / wartet auf dich / fertig / Fehler / bereit, Laufzeit, Ergebnis (erste Zeile der letzten Antwort) | Hooks |
| Aufgabenbalken | echter Fortschritt „3/7 Aufgaben“, Zahl im Balken; bei Fable dunkleres Grün | `TaskCreated`/`TaskCompleted`, `TodoWrite` |
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
