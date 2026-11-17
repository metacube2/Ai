# Video-Doku

Erstellt aus PowerPoint-Folien oder aus Bildern und Clips mit Sprechertext ein fertiges MP4-Video:
Stimme je Szene, Untertitel, sanfter Bildzoom, Hintergrundmusik, die beim Sprechen leiser wird.
Bedienbar über eine Oberfläche **und vollständig durch eine KI über MCP**. Gebaut am 17.11.2026 auf Wunsch von Ingo.

Stand: 17.11.2026 abends. 29 Tests grün, davon ein Ende-zu-Ende-Test mit echtem ffmpeg. Zusätzlich geprüft:
PowerPoint-Import mit echter Präsentation, Windows-Stimme „Karsten“ (Sprache −24 dB im Mittel, Spuren Video/Ton/
Untertitel), Klick auf „Video erstellen“ in der Oberfläche per UI-Automation, und eine Claude-Sitzung, die nur
über MCP ein Projekt anlegt, Text ändert, das Video erstellt und wartet (Fehler kommen im Klartext an).

| Ort | Inhalt |
| --- | --- |
| `C:\Users\koi\source\repos\VideoDoku` | eigenes Repo, hier wird entwickelt |
| GitHub `metacube2/Ai`, Branch `werkzeuge/agentmonitor-videodoku`, Ordner `VideoDoku/` | Kopie mit Verlauf (git subtree), **öffentlich**, noch nicht in `main` |
| `%LOCALAPPDATA%\VideoDoku\{app,mcp,ffmpeg}` | installierte Oberfläche, MCP-Server, ffmpeg |

Nachziehen in den Branch: im Arbeitsverzeichnis `..\Ai-werkzeuge`
`git subtree pull --prefix=VideoDoku C:/Users/koi/source/repos/VideoDoku main`, dann `git push`.

### Offen (Stand 17.11. abends)

| Punkt | Stand |
| --- | --- |
| Sprachdienst `tts.ingok.ch` mit Stimme horst | **live seit 17.11. 16:54** (von aussen geprueft: voices, speak 200 und 202, 403, 429), MP3 q5 rund 54 kbit/s |
| Schluessel „video-doku“ | auf dem Laptop eingetragen (DPAPI); eingefuegte Zeile wie `video-doku=…` wird bereinigt, das Werkzeug zeigt gueltig/ungueltig |
| Erster Lauf mit horst | 17.11. 17:05: Finance-Video „Gruppenmarge“ (8 Folien, 2:25, Inhalt von der Finance-Sitzung 6d), `%USERPROFILE%VideosVideoDokuinance_gruppenmarge_2026-11-17` |
| schluessel neu ausstellen | beide schluessel standen als bildschirmfoto im chat; lagoon soll sie ersetzen (entscheid ingo) |
| Öffentlich im Repo `Ai` | Adresse des Sprachdienstes und dass horst Ingos eigene Stimme ist (keine Schlüssel). Auf Wunsch Branch löschen und neutralisiert neu pushen |
| Merge nach `main` | Entscheid Ingo |

## Stimmen

| Quelle | Stimme | Stand |
| --- | --- | --- |
| Sprachdienst `https://tts.ingok.ch/api/tts/v1/` (Qwen3-TTS auf Ingos Mac) | `horst` (Ingos eigene Stimme, Einwilligung 17.11.2026) | Adresse und Zertifikat stehen; **Dienst selbst wird von der Mac-Sitzung lagoon noch gebaut**. Schlüssel „video-doku“ bekommt Ingo dort als Datei, nie über den Chat. |
| Windows lokal | „Karsten“ (de-CH) und weitere installierte Windows-Stimmen | funktioniert, Ersatz bis der Dienst steht |

Den Schlüssel trägt Ingo in der Oberfläche unter „Schlüssel“ ein; er wird mit Windows-DPAPI nur für
seinen Windows-Benutzer lesbar gespeichert (`%APPDATA%\VideoDoku\schluessel.bin`).

Schnittstelle des Dienstes (mit lagoon abgestimmt): `GET voices`, `POST speak {text, voice, language, format:"mp3"}`
→ 200 MP3 oder 202 `{"id"}`, `GET jobs/{id}`, `GET jobs/{id}/audio`, Fehler 401/403/413/429, Bearer-Schlüssel.

## Quellen

- **PowerPoint** (`.pptx`): über das installierte PowerPoint werden sichtbare Folien als PNG 1920×1080
  exportiert, die Sprechernotizen werden zum Sprechertext. Ausgeblendete Folien fehlen. Ein offenes
  PowerPoint wird nicht beendet. Bildzoom für Folien standardmässig aus.
- **Skript** (`.txt`) für freie Dokus:

  ```text
  titel: Unser Ausflug
  musik: musik/ruhig.mp3

  [bild: bilder/01.jpg]
  Sprechertext, gern über mehrere Zeilen.

  Leerzeile = neuer Absatz.
  [pause: 1,5]

  [clip: clips/see.mp4]
  Text über dem Clip (Originalton läuft leise darunter).

  [bild: bilder/02.png]
  [dauer: 6]
  ```

  `#` = Kommentar. Pfade relativ zum Skriptordner. Clip ohne Text läuft mit Originalton ganz durch,
  Bild ohne Text steht `[dauer]` Sekunden (Standard 4).

## Bedienung durch eine KI (MCP)

Der Server `VideoDokuMcp.exe` ist in Claude Code für alle Projekte angemeldet
(`claude mcp add videodoku --scope user -- %LOCALAPPDATA%\VideoDoku\mcp\VideoDokuMcp.exe`).

| Werkzeug | Zweck |
| --- | --- |
| `videodoku_status` | ffmpeg, Schlüssel, Stimmen, offene Projekte und Aufträge |
| `videodoku_list_voices` | Stimmen (Sprachdienst zuerst, dann Windows) |
| `videodoku_open` | PowerPoint oder Skript öffnen → Projekt-Id und Szenen |
| `videodoku_create_project` | freie Doku aus Bildern/Clips anlegen, schreibt `skript.txt` |
| `videodoku_get_project` | Projekt mit Szenen anzeigen |
| `videodoku_set_scene` | Text, Pause, Standdauer einer Szene ändern |
| `videodoku_set_project` | Titel, Musik ändern |
| `videodoku_save_script` | Projekt als Skript speichern |
| `videodoku_build` | Video im Hintergrund erstellen → Auftrags-Id |
| `videodoku_job` / `videodoku_wait` | Stand abfragen / auf das Ende warten |
| `videodoku_cancel` | Auftrag abbrechen |
| `videodoku_speak` | nur eine Sprachdatei erzeugen |
| `videodoku_open_ui` | Oberfläche für den Menschen öffnen |

Fehler kommen als `McpException` mit Klartext zurück. Andere Ausnahmen zeigt das MCP-SDK der KI nur als
„An error occurred“ (beim ersten Test so passiert, deshalb überall umgesetzt).
Vom Werkzeug angelegte Skripte werden bei Änderungen automatisch mitgespeichert; fremde Skripte nie
ungefragt (Kommentare blieben sonst nicht erhalten).

Der Fortschritt eines Auftrags erscheint auch im Agenten-Monitor (Sitzung „Video: Titel“, je Szene eine Aufgabe).

## Ablauf je Video

1. Je Szene Stimme holen, im Ordner `<Ziel>_arbeit/stimme` zwischengespeichert (Schlüssel: Quelle, Stimme,
   Sprache, Text). Ein zweiter Lauf mit gleichem Text holt nichts neu.
2. Je Szene ein Stück rendern (H.264 1080p30, AAC 48 kHz): Stimme setzt 0,4 s nach Szenenbeginn ein,
   Ein- und Ausblenden 0,4 s, Clips werden eingepasst und bei Bedarf mit dem letzten Bild verlängert.
3. Zusammensetzen ohne Neucodierung, Untertitel als Spur (`mov_text`) und als `.srt`, Musik per
   `sidechaincompress` unter die Stimme geduckt, `+faststart`.

## Installation (so auf Ingos Laptop)

- ffmpeg 8.1 (BtbN-Build, SHA-256 geprüft) unter `%LOCALAPPDATA%\VideoDoku\ffmpeg\bin`
  (gyan.dev antwortete am 17.11. mit 503).
- Oberfläche: `dotnet publish src/VideoDoku.App -c Release -o %LOCALAPPDATA%\VideoDoku\app`
- MCP-Server: `dotnet publish src/VideoDoku.Mcp -c Release -o %LOCALAPPDATA%\VideoDoku\mcp`

## Aufbau

| Projekt | Inhalt |
| --- | --- |
| `VideoDoku.Core` | Modell, `ScriptParser`/`ScriptWriter`, `Subtitles`, `FfmpegCommands` (reine Argumente, testbar), `Ffmpeg` (Aufruf mit Fortschritt), `IngokTtsClient`, `DokuBuilder`, `MonitorReporter` |
| `VideoDoku.Windows` | Windows-Stimmen (WinRT), PowerPoint-Import (COM), Einstellungen und Schlüssel (DPAPI) |
| `VideoDoku.App` | WPF-Oberfläche (Strg+Mausrad vergrössert), Datei als Startargument |
| `VideoDoku.Mcp` | MCP-Server (stdio, offizielles C#-SDK `ModelContextProtocol` 2.4.0) |
| `VideoDoku.Tests` | 24 xUnit-Tests inklusive Ende-zu-Ende mit echtem ffmpeg |

## Fallstricke (gelernt)

- PowerPoint `Slide.Export` scheitert an Pfaden mit `/` („kann ^0 auf ^1 nicht speichern“): Pfade werden normalisiert.
- Ein globaler `TextBlock`-Stil in `App.xaml` färbt auch den Text in der ComboBox hell; deshalb Vordergrund am Fenster setzen.
- `.gitignore`-Regel `*.app` erfasst `src/VideoDoku.App` unter Windows; Ausnahme ist eingetragen.
- `SystemParameters.WorkArea` gilt nur für den Hauptbildschirm. Auf dem kleineren Laptop-Bildschirm war deshalb
  „Video erstellen“ abgeschnitten; das Fenster passt sich jetzt beim Öffnen dem Bildschirm an, auf dem es aufgeht.
- MCP-SDK (`ModelContextProtocol` 2.4.0): Nur `McpException` kommt mit Text bei der KI an, alles andere als
  „An error occurred“. Jedes Werkzeug leitet Fehler deshalb über `Safe`/`SafeAsync` weiter.
- Stimme vom Sprachdienst war deutlich leiser (−28 dB im Mittel) als die Windows-Stimme: Sprache wird je Szene mit `loudnorm` auf rund −16 LUFS angeglichen (danach −19 dB im Mittel, Spitze −1,5 dB).
- Eingefuegte Schluessel: einmal mit der ganzen Zeile `video-doku=…` gespeichert (401). `IngokTtsClient.NormalizeKey` entfernt Namen, Leerraum und unsichtbare Zeichen; die Oberflaeche prueft den Schluessel beim Speichern.
- PowerShell 5.1 liest Skripte ohne BOM als ANSI: Umlaute in Folien und Notizen waren kaputt („Ã–sterreich“). Skripte mit Umlauten mit BOM speichern und das Ergebnis ansehen, bevor vertont wird.
- Vom MCP-Server geänderte Texte landen sonst nur im Speicher: Skripte, die das Werkzeug selbst angelegt hat,
  werden mitgespeichert, damit Video und Skript übereinstimmen.
