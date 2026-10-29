# Whisper Deployment Handoff

Stand: 2026-10-22 (alte WinForms-GUI entfernt; Whisper Studio: neue WPF-Oberflaeche, Benennung Datum_Uhrzeit_Titel, Word/SRT/VTT, Aufnahme mit Systemton, Editor, Verlauf, lokale KI)
Pfad: `C:\Users\koi\source\repos\Ai\TrafagSalesExporter\whisper`
Ziel: Lokale Teams-Audioaufnahme und Transkription in `.txt`, ohne Adminrechte und komplett in diesem Unterordner.

## Zielbild

Dieses Setup soll auf einem Firmenrechner ohne Adminrechte laufen. Es nimmt Audio lokal per Mikrofon auf oder verarbeitet vorhandene Audio-/Videodateien und schreibt eine Textdatei in `output`.

Wichtige Sprachanforderung:

- Schweizerdeutsch soll moeglichst als gut lesbares Hochdeutsch ausgegeben werden.
- Hochdeutsch soll normal transkribiert werden.
- Englisch soll ebenfalls funktionieren.

Dafuer wird bei schweizerdeutschen Meetings `-Language de` empfohlen.

## Aktueller Status

Fertig eingerichtet und getestet:

- Portable Python liegt unter `python`.
- `pip` ist in dieser portablen Python-Installation vorhanden.
- Python-Pakete fuer Transkription sind lokal installiert.
- `faster-whisper` funktioniert.
- Modell `medium` wurde bereits heruntergeladen und getestet.
- Modell `tiny` wurde fuer Smoke-Test ebenfalls heruntergeladen.
- Modell `large-v3` ist ebenfalls vollstaendig heruntergeladen (`cache\models`, ca. 3.1 GB).
- Mikrofon-Geraete koennen gelistet werden.
- Ausgabe in `.txt` funktioniert.
- Alles bleibt im Ordner `whisper`.
- Whisper Studio (`gui\WhisperStudio`) als Hauptoberflaeche, siehe Abschnitt "Whisper Studio". Die alte WinForms-GUI (`gui\WhisperGui`) wurde am 2026-10-22 entfernt (Wunsch Ingo); ihre Quellen liegen in der Git-Historie bis Commit `ae599c2`.

Aktuelle Groesse des Ordners nach Setup: ca. 5 GB (`python` ~0.3 GB, `cache\models` ~4.6 GB mit tiny/medium/large-v3).

Gemessene Performance auf einem Testgeraet (AMD Ryzen 7 PRO 4750U, 8C/16T, CPU int8):

- Modell `medium`: ca. Echtzeit (Faktor ~1.0), 12.1 Min. Audio in 12.3 Min. transkribiert.
- Modell `small`/`large-v3`: noch nicht mit echter Datei gemessen, grobe Schaetzung siehe Tabelle unten.

| Modell | Groesse lokal | Geschwindigkeit (grob) |
|---|---|---|
| `tiny` | ~75 MB | sehr schnell, schlechte Qualitaet |
| `small` | (nicht geladen) | ca. 0.3-0.5x der Audiolaenge |
| `medium` | ~1.5 GB | ca. 1x der Audiolaenge (gemessen) |
| `large-v3` | ~3.1 GB | ca. 2-3x der Audiolaenge (geschaetzt) |

## Umzug auf einen anderen PC

Der ganze `whisper`-Ordner kann per Datei-Kopie (USB-Stick, Netzlaufwerk, OneDrive) auf einen anderen Windows-PC gezogen werden. Geprueft:

- `python\python312._pth` und die Paket-Metadaten enthalten nur relative Pfade, keine Pfade zu diesem PC. Portable Python + alle Pakete (`faster-whisper`, `ctranslate2`, `numpy`, `onnxruntime`, ...) laufen unveraendert auf einem anderen Windows-x64-Rechner.
- `transcribe.ps1`/`record_mic.ps1` und die Python-Skripte ermitteln ihren Wurzelordner ueber `$PSScriptRoot` bzw. `Path(__file__)`, also relativ. Kein Pfad ist hart codiert.
- `cache\models` (heruntergeladene Whisper-Modelle) reist mit, es muss nichts neu heruntergeladen werden, solange die Kopie vollstaendig war.
- Ausnahme: `python\Scripts\pip.exe`, `huggingface-cli.exe` und aehnliche Launcher-EXEs von pip haben den Installationspfad fest eingebaut und funktionieren nach dem Verschieben nicht mehr direkt. Betrifft nicht die normale Nutzung (Transkription/Aufnahme laeuft ueber `python.exe script.py`), nur falls jemand `pip`/`huggingface-cli` direkt aufrufen will. Workaround: `python.exe -m pip ...` statt `pip.exe ...`.
- Das Studio (`gui\WhisperStudio\bin\Release\net9.0-windows\WhisperStudio.exe`) ist standardmaessig **framework-abhaengig** und braucht auf dem Ziel-PC die .NET 9 Desktop Runtime. Fuer einen PC ohne .NET-Installation den self-contained Build erzeugen (Befehl im Abschnitt "Whisper Studio") und `publish\WhisperStudio.exe` mitnehmen. Die lokale KI in `llm\` reist mit.
- Zielrechner muss Windows x64 sein; sehr alte CPUs ohne AVX2 koennten bei `ctranslate2` Probleme machen (in der Praxis bei Firmen-Laptops kein Thema).
- Keine Adminrechte fuer die Nutzung noetig, wie bisher.

Kurz: **Ja, alles ist nach dem Kopieren direkt lauffaehig** - mit der einzigen Ausnahme, dass fuer die GUI entweder .NET 9 auf dem Ziel-PC vorhanden sein muss oder der self-contained Build (`publish\WhisperStudio.exe`) mitgenommen wird.

## Ordnerstruktur

```text
whisper\
  ANLEITUNG.md              Benutzeranleitung
  README.md                 Kurzanleitung
  HANDOFF.md                Diese Datei
  transcribe.ps1            PowerShell-Wrapper fuer Transkription
  record_mic.ps1            PowerShell-Wrapper fuer Mikrofonaufnahme
  python\                  Portable Python-Installation
  scripts\
    transcribe.py           Python-Transkriptionslogik
    record_mic.py           Python-Aufnahmelogik
  gui\
    WhisperStudio\          WPF-Hauptoberflaeche (seit 2026-10-22)
    WhisperStudio.Tests\    xUnit-Tests zum Studio
  llm\                     lokale KI: bin\llama-server.exe, models\*.gguf (nicht in Git)
  input\                   Aufnahme-/Eingabedateien
  output\                  TXT-Ausgaben
  cache\
    models\                faster-whisper Modellcache
    pip\                   Pip-Downloadcache
    hf\                    Hugging-Face Cache
    studio_settings.json   Einstellungen des Studios (Sprache, Modell, Exporte, Mikrofon, KI-Endpunkt)
    gui_settings.json      Einstellungen der entfernten alten GUI (unbenutzt, darf geloescht werden)
  downloads\               heruntergeladene Installer/Bootstrap-Dateien
  temp\                    lokales Temp-Verzeichnis
```

Ein `whisper\.gitignore` schliesst `python\`, `cache\`, `downloads\`, `temp\`, `input\`, `output\` von Git aus, da dort nur lokale Laufzeitdaten und persoenliche Dateipfade liegen.

## Wichtige Designentscheidungen

### faster-whisper statt openai-whisper

Verwendet wird `faster-whisper`, nicht das originale `openai-whisper` Paket.

Grund:

- Kein grosses PyTorch-Setup notwendig.
- Auf Windows/CPU pragmatischer.
- Modelle werden als CTranslate2-Modelle geladen.
- CPU-Modus mit `int8` reduziert Speicherbedarf.

### Alles im Unterordner

Die Wrapper setzen lokale Umgebungsvariablen:

- `HF_HOME`
- `HF_HUB_CACHE`
- `XDG_CACHE_HOME`
- `PIP_CACHE_DIR`
- `TEMP`
- `TMP`

Damit bleiben Modelle, Cache und Temp-Dateien im Ordner `whisper`.

### Schweizerdeutsch als Hochdeutsch

In `scripts\transcribe.py` gibt es einen `PROMPT`:

```text
Transcribe Swiss German as clear, readable Standard German. Keep High German as German. Keep English sentences in English. Use normal punctuation and paragraphs for meeting notes.
```

Dieser Prompt ist bewusst gesetzt. Er soll Schweizerdeutsch nicht dialektgetreu schreiben, sondern als lesbares Hochdeutsch ausgeben.

Wichtig: Whisper/faster-whisper garantiert keine perfekte Uebersetzung von Dialekt zu Hochdeutsch. Es verbessert aber typischerweise die Lesbarkeit.

## Hauptbefehle

### Geraete anzeigen

```powershell
.\whisper\record_mic.ps1 -ListDevices
```

### Aufnahme starten

10 Minuten:

```powershell
.\whisper\record_mic.ps1 -Seconds 600 -OutputFile teams_meeting.wav
```

30 Minuten:

```powershell
.\whisper\record_mic.ps1 -Seconds 1800 -OutputFile teams_meeting.wav
```

Mit bestimmtem Geraet:

```powershell
.\whisper\record_mic.ps1 -Seconds 1800 -OutputFile teams_meeting.wav -Device 2
```

### Transkription Deutsch/Schweizerdeutsch

```powershell
.\whisper\transcribe.ps1 .\whisper\input\teams_meeting.wav -Language de -Model medium
```

### Transkription Englisch

```powershell
.\whisper\transcribe.ps1 .\whisper\input\teams_meeting.wav -Language en -Model medium
```

### Automatische Spracherkennung

```powershell
.\whisper\transcribe.ps1 .\whisper\input\teams_meeting.wav -Language auto -Model medium
```

### Mit Zeitstempeln

```powershell
.\whisper\transcribe.ps1 .\whisper\input\teams_meeting.wav -Language de -Model medium -Timestamps
```

## Modellwechsel

Aktuelles Standardmodell ist `medium`.

Moegliche Modelle:

| Modell | Zweck | Hinweis |
|---|---|---|
| `tiny` | Smoke-Test | sehr schnell, schlechte Qualitaet |
| `small` | schnellere Transkription | ggf. weniger gut bei Schweizerdeutsch |
| `medium` | Standard | gute Balance, bereits geladen |
| `large-v3` | bessere Qualitaet | deutlich langsamer auf CPU, laedt mehrere GB |

Beispiel fuer anderes Modell:

```powershell
.\whisper\transcribe.ps1 .\whisper\input\teams_meeting.wav -Language de -Model large-v3
```

Beim ersten Start wird das Modell nach `cache\models` geladen. Danach bleibt es lokal.

## Weiterentwicklung mit anderem Modell

Wenn ein anderer Chat oder Entwickler weiterarbeiten soll, sind die wichtigsten Ansatzpunkte:

1. Modellparameter in `transcribe.ps1` und `scripts\transcribe.py` pruefen.
2. Falls ein anderes Standardmodell gewuenscht ist, Default in beiden Dateien anpassen:
   - `transcribe.ps1`: `[string]$Model = 'medium'`
   - `scripts\transcribe.py`: `parser.add_argument("--model", default="medium", ...)`
3. Fuer bessere Schweizerdeutsch-zu-Hochdeutsch-Ausgabe den `PROMPT` in `scripts\transcribe.py` anpassen.
4. Testdatei in `input` legen und mit `-Language de` testen.
5. Ergebnis in `output` kontrollieren.

Empfohlene Tests nach Aenderungen:

```powershell
.\whisper\python\python.exe -m py_compile .\whisper\scripts\transcribe.py .\whisper\scripts\record_mic.py
.\whisper\record_mic.ps1 -ListDevices
.\whisper\transcribe.ps1 .\whisper\input\<testdatei.wav> -Language de -Model medium
```

## Whisper Studio (WPF, ab 2026-10-22) - Hauptoberflaeche

`gui\WhisperStudio` ersetzt die WinForms-GUI als Bedienoberflaeche. Die alte GUI wurde am selben Tag entfernt (Abschnitt unten).

- Technik: .NET 9 WPF mit `ThemeMode="System"` (Fluent/Windows-11-Optik, folgt Hell/Dunkel; in den Einstellungen umstellbar). Pakete: `NAudio.Wasapi`/`NAudio.Core` 2.2.1 (Aufnahme), `DocumentFormat.OpenXml` 3.1.1 (Word). Tests: `gui\WhisperStudio.Tests` (xUnit, 35 Tests: Benennung, Fortsetzen ueber die `.json`-Quelle, SRT/VTT, DOCX, Aufteilen fuer die KI, Codeblock-Entfernung, Fortschrittszeile, Mischen der Aufnahmespuren, VU-Skala und Pegelmessung. Drei davon pruefen nur, wenn eine Umgebungsvariable gesetzt ist: `WS_LLM_URL=http://127.0.0.1:8088/v1` (echtes Protokoll, 25 s), `WS_WHISPER_MEDIA=<kurze Sprachdatei>` (Whisper tiny + Export, 25 s), `WS_RECORD=1` (3 s echte Aufnahme Mikrofon + Systemton), dazu `WS_VU_PNG=<pfad>.png` schreibt drei Bilder des VU-Meters (Ruhe, Sprache, zu laut). Am 2026-10-22 alle gruen.).
- Bereiche (linke Navigation): Transkribieren, Aufnahme, Editor & KI, Verlauf, Einstellungen.
- **Dateiname** (`Core\OutputNaming.cs`): `JJJJ-MM-TT_HHMM_Titel.txt` statt `audio.txt`. Datum/Uhrzeit = Aenderungszeit der Mediendatei, Titel = Dateiname, wenn er etwas aussagt, sonst der naechste aussagekraeftige Ordnername (`...\einklauf\Data\audio.wav` -> `einklauf`). Generische Namen (audio, au3333dio, Data, Aufnahme, Neuer Ordner ...) werden uebersprungen. Beides ist in der Warteschlange pro Zeile aenderbar. Der Name ist pro Datei deterministisch, damit "Vorhandene ueberspringen" weiter greift. Wurde der Titel von Hand geaendert, findet das Studio das fruehere Transkript ueber die Quelle in der `.json` (`TranscriptMeta.FindExistingTranscript`), sonst wuerde die Datei nach erneutem Hinzufuegen nochmals transkribiert. Dateien, die schon mit `JJJJ-MM-TT_HHMM_` beginnen (Studio-Aufnahmen), behalten ihren Namen. Belegte Namen bekommen wie bisher einen Zeitstempel angehaengt, ueberschrieben wird nie.
- **Segmentdatei**: `scripts\transcribe.py` schreibt neben der `.txt` eine `.json` mit Segmenten (Start, Ende, Text), Modell, Sprache, Dauer (`Sidecar: <pfad>` im Log). Daraus erzeugt das Studio SRT, VTT und Word (`Core\Transcript.cs`). Wurde die `.txt` im Editor geaendert (juenger als die `.json`), nimmt Word den bearbeiteten Text ohne Zeiten; SRT/VTT bleiben bei der Originalerkennung, weil nur die `.json` Zeiten hat.
- **Uebersetzen durch Whisper**: Aufgabe "Direkt nach Englisch uebersetzen" (`transcribe.ps1 -Task translate`). Whisper kann nur nach Englisch uebersetzen; der Schweizerdeutsch-Prompt wird dabei weggelassen, weil Whisper ihn sonst als Text ausgibt. Andere Zielsprachen ueber die lokale KI.
- **Fortschritt**: pro Zeile Balken, Prozent und Restzeit, unten Gesamtfortschritt (Protokoll `PROGRESS ...` wie bei der WinForms-GUI).
- **Aufnahme** (`Core\Recorder.cs`): Mikrofon (WASAPI, Geraet waehlbar) und optional Systemton per WASAPI-Loopback (das, was Teams abspielt). Beide Spuren werden in `temp\` aufgenommen und beim Stoppen zu einer 16-kHz-Mono-WAV gemischt: `input\JJJJ-MM-TT_HHMM_Titel.wav`. Loopback liefert bei Stille keine Daten; die Luecken werden mit Stille aufgefuellt, sonst laufen die Spuren auseinander. Gestoppt wird kooperativ (Writer schliessen), nie per Kill, sonst fehlt der WAV-Kopf. Optional startet danach automatisch die Transkription. Pegel: `Recorder.Level` liefert pro Mikrofonpuffer Spitze und RMS (`Recorder.Measure`); angezeigt im analogen VU-Meter `Controls\VuMeter.cs` (seit 2026-10-22, ersetzt den schmalen Balken). Skala -20 bis +3 VU, spannungslinear wie ein echtes Instrument, roter Bereich ab 0 VU; 0 VU = -18 dBFS RMS. Die Nadel ist eine leicht unterdaempfte Feder (etwa 300 ms Einschwingzeit, Endanschlag), die PEAK-Lampe leuchtet ab -1 dBFS Spitze und haelt 1,2 s. Gezeichnet per `OnRender`, das Ziffernblatt wird pro Groesse einmal als `DrawingGroup` gecacht, die Animation laeuft ueber `CompositionTarget.Rendering` nur solange sichtbar und in Bewegung.
- **Editor & KI**: Transkript bearbeiten und speichern, Export Word/SRT/VTT. Lokale KI (`Core\LocalAi.cs`): "Protokoll erstellen" (Zusammenfassung, Themen, Beschluesse, Pendenzen-Tabelle, offene Fragen), "In Hochdeutsch glaetten", "Uebersetzen nach" (8 Sprachen). Lange Transkripte werden in Abschnitte von 5000 Zeichen geteilt; das Protokoll entsteht in zwei Stufen (Notizen je Abschnitt, dann Gesamtprotokoll). Ergebnisse speichert "Ergebnis speichern" als `<stamm>_protokoll.md`, `<stamm>_geglaettet.md`, `<stamm>_uebersetzung_<Sprache>.md`. Die Prompts verlangen: nichts erfinden, Namen/Fachbegriffe/Zahlen unveraendert.
- **Lokale KI = llama.cpp** statt Ollama: `llm\bin\llama-server.exe` (CPU-Build b11799, ca. 23 MB, portable, keine Adminrechte) und `llm\models\qwen2.5-3b-instruct-q4_k_m.gguf` (2,1 GB, Hugging Face `Qwen/Qwen2.5-3B-Instruct-GGUF`). Ollama gibt es nur als grosses Paket mit GPU-Bibliotheken (ca. 1,5 GB); llama-server hat dieselbe OpenAI-kompatible Schnittstelle. Das Studio startet den Server bei Bedarf selbst (`-c 8192`, Port 8088, Threads = halbe logische Kerne) und beendet ihn beim Schliessen. Endpunkt und Modellname sind Einstellungen; ein anderer OpenAI-kompatibler Dienst ist damit nur eine Konfigurationsaenderung (vorher Datenschutz klaeren). 3B statt 7B, weil der Laptop nur CPU hat: 7B waere etwa doppelt so langsam. Groesseres Modell = andere `.gguf` in `llm\models` ablegen (es wird die alphabetisch erste genommen).
- **Verlauf**: alle `.txt` in `output` (rekursiv), neueste zuerst, Datum aus dem Namenspraefix (alte Namen: Dateizeit), Suche in Titel, Ordner und ab 3 Zeichen im Volltext. Doppelklick oeffnet im Editor.
- Einstellungen: `cache\studio_settings.json` (getrennt von `cache\gui_settings.json` der alten GUI).

Bauen und starten:

```powershell
cd whisper\gui\WhisperStudio
dotnet build -c Release
.\bin\Release\net9.0-windows\WhisperStudio.exe
```

Ohne .NET auf dem Ziel-PC: `dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish`.

Tests: `dotnet test whisper\gui\WhisperStudio.Tests`.

Bekannte Grenzen: Die KI auf CPU braucht fuer ein einstuendiges Meeting einige Minuten; bei sehr langen Meetings kann die zweite Protokollstufe das 8k-Kontextfenster ueberschreiten. Die Studio-Oberflaeche wurde per Bildschirmfoto geprueft, die Aufnahme per Test mit echtem Mikrofon und Systemton. Ein Bedienlauf mit Maus (Drag&Drop, Tabelle bearbeiten, KI-Knoepfe) steht noch aus.

## Alte Desktop-GUI (C#/WinForms) - am 2026-10-22 entfernt, nur noch Historie

Der Ordner `gui\WhisperGui` wurde am 2026-10-22 auf Wunsch von Ingo geloescht, nachdem das Studio uebernommen war; die Desktop-Verknuepfung "Whisper" zeigt auf das Studio. Quellen in der Git-Historie bis Commit `ae599c2`, Zip-Sicherung in `whisper\backup\`. Die folgende Beschreibung bleibt als Entwurfsbegruendung stehen, weil das Studio dieselben Regeln uebernommen hat (Fortsetzen ueber den natuerlichen Namen vor dem Reservieren, Kollisionsschutz, Exit-Codes, `Written:`, Fortschrittsprotokoll). Alles, was dort ueber `scripts\transcribe.py` steht, gilt unveraendert.

Funktionen (Stand bei der Entfernung):

- Mehrere Dateien auswaehlen (Dialog mit Mehrfachauswahl oder Drag&Drop mehrerer Dateien gleichzeitig) fuer Audio/Video. Sie landen in einer Warteschlange (`ListBox`) mit Status je Datei (wartend/laeuft/fertig/Fehler/abgebrochen).
- "Ordner hinzufuegen..." (oder einen Ordner ins Fenster ziehen) durchsucht den Ordner rekursiv (`Directory.EnumerateFiles ... SearchOption.AllDirectories`) nach Mediendateien (Endungen in `MediaExtensions`: wav/mp3/m4a/mp4/mov/aac/flac/wma/ogg/opus/mkv/webm/avi/wmv/3gp/amr) und legt alle gefundenen Dateien in die Warteschlange. Pro Datei wird der relative Unterordner zum gewaehlten Wurzelordner (`QueueItem.RelativeDir`) gemerkt.
- Ausgabestruktur spiegelt die Unterordner: `ComputeNaturalOutputPath` haengt `RelativeDir` unter `output\` an, d.h. `<Wurzel>\2026-01\meeting.wav` -> `output\2026-01\meeting.txt`. `transcribe.py` legt fehlende Ausgabe-Unterordner selbst an (`out_path.parent.mkdir(parents=True, exist_ok=True)`). Einzeln hinzugefuegte Dateien haben `RelativeDir=""` und landen direkt in `output\`.
- Sprache (`auto`/`de`/`en`), Modell (`tiny`/`small`/`medium`/`large-v3`), Zeitstempel-Option gelten fuer die ganze Warteschlange.
- "Start Batch-Transkription" arbeitet die Warteschlange sequenziell ab: pro Datei ein `transcribe.ps1`-Subprozess (PowerShell -> Python), danach automatisch die naechste wartende Datei. Konsolen-Log live im "Log"-Tab, jede Datei mit `[n/total]`-Praefix.
- Resume/Fortsetzen: Checkbox "Vorhandene ueberspringen" (Default an, in `gui_settings.json` als `SkipExisting` gemerkt). Vor dem Start jeder Datei prueft die GUI, ob deren Zielpfad in `output` schon als nicht-leere Datei existiert; wenn ja, wird die Datei mit Status ⊘ uebersprungen (kein erneuter Lauf). Damit setzt ein abgebrochener/neu gestarteter 224-Dateien-Batch praktisch dort fort, wo er aufgehoert hat. Haekchen weg = alles neu transkribieren, wobei die alte Ausgabe **nicht** ueberschrieben, sondern durch eine Zeitstempeldatei ergaenzt wird (siehe Kollisionsschutz).
  - Reihenfolge-Falle: Die Skip-Pruefung laeuft gegen den *natuerlichen* Namen (`ComputeNaturalOutputPath`) und muss **vor** `ReserveOutputPath` stehen. Wuerde man den reservierten Pfad pruefen, existiert dieser per Konstruktion nie, `File.Exists` waere immer falsch und Resume waere still tot – jeder Lauf erzeugte stattdessen eine neue Zeitstempelkopie.
- Robustheit im Batch: `scripts\transcribe.py` prueft vor dem Laden des Modells per PyAV (`has_audio_stream`), ob die Datei ueberhaupt eine Audiospur hat. Dateien ohne Audio (z.B. stumme Bildschirmaufnahmen, Video-only-AVI/MP4) werden mit klarer Meldung uebersprungen (Exit-Code 3) statt mit einem tiefen `IndexError`-Traceback abzustuerzen. Die eigentliche Transkription ist in `try/except` gekapselt: bei Decodier-/Modellfehlern gibt es eine einzeilige Meldung (Exit-Code 1) statt Traceback. Der Transkripttext wird zuerst komplett im Speicher gebaut und erst nach Erfolg geschrieben, damit ein Fehler mitten in der Datei keine 0-Byte-Ausgabe hinterlaesst. Exit-Codes: 0 = fertig, 1 = Fehler, 3 = uebersprungen. Die GUI wertet diese aus (Status ✓/✗/⊘) und zeigt am Ende `x fertig, y uebersprungen, z Fehler`.
- Kollisionsschutz beim Ausgabenamen (`ComputeNaturalOutputPath` + `ReserveOutputPath`): Die GUI berechnet je Datei einen eindeutigen Zielpfad und uebergibt ihn explizit per `-OutputFile` an `transcribe.ps1`. Reserviert wird gegen die schon vergebenen Queue-Pfade **und** gegen `File.Exists`. Ist der natuerliche Name belegt, lautet der Zielname `<stem>_yyyyMMdd_HHmmss.txt`; bei zwei gleichnamigen Items in derselben Sekunde haengt ein Zaehler an (`..._2.txt`), der Zeitstempel allein reicht dafuer nicht. Ohne diese Logik wuerde `transcribe.py` beide auf `output\<stem>.txt` schreiben und die zweite Datei die erste per `open("w")` ueberschreiben.
- Schreibfehler kosten kein Transkript (Fix 2026-08-07): Frueher lag `out_path.open("w", ...)` in `transcribe.py` **ausserhalb** des `try`-Blocks. War die Zieldatei vorhanden und nicht beschreibbar (schreibgeschuetzt, von einem Editor gesperrt, Sync-Konflikt), gab es nach der kompletten, teuren Transkription einen `PermissionError`-Traceback, Exit-Code 1 und keine Ausgabe - der Lauf war verloren. Jetzt gilt: `free_output_path` weicht schon bei der Planung auf einen Zeitstempelnamen aus, `write_transcript` faengt `OSError` beim Schreiben ab und versucht bis zu drei Zeitstempel-Alternativen, bevor es aufgibt. Das Skript meldet den tatsaechlich geschriebenen Pfad als `Written: <pfad>`; die GUI liest diese Zeile mit (`QueueItem.WrittenOutputPath`) und laedt daraus den "Ergebnis"-Tab, damit ein Ausweichname nicht als "Erwartete Ausgabedatei nicht gefunden" endet. `OnItemProcessExited` ruft dafuer zuerst `WaitForExit()`, weil `Exited` feuern kann, bevor die umgeleiteten Streams geleert sind.
- "Entfernen"/"Liste leeren" nehmen noch nicht gestartete Dateien wieder aus der Warteschlange; waehrend eine Datei laeuft, koennen weitere Dateien per "Dateien hinzufuegen..." ergaenzt werden.
- Fortschrittsanzeige (seit 2026-10-22): `scripts\transcribe.py` gibt pro Segment eine Zeile `PROGRESS <prozent> <bearbeitet_s> <gesamt_s>` aus (Anteil = `segment.end / info.duration`; `segment.end` liegt auf der Originalzeitachse, VAD-Luecken sind also eingerechnet) und am Schluss `PROGRESS 100`. Die Zeilen werden mit `flush=True` geschrieben, `transcribe.ps1` setzt zusaetzlich `PYTHONUNBUFFERED=1`, sonst kaemen sie ueber die Pipe erst am Ende an. faster-whisper liefert Segmente fensterweise (ca. 30 s Audio), die Anzeige springt deshalb in Schritten. Die GUI loggt `PROGRESS`-Zeilen nicht, sondern zeigt sie an: oberer Balken = aktuelle Datei (Marquee, solange Modell geladen und Audio vorbereitet wird, danach echte Prozent), Statuszeile mit Prozent, Audiolaenge, Laufzeit der Datei und geschaetzter Restzeit (ab 2 %, linear hochgerechnet), unterer Balken = ganze Warteschlange (erledigte Dateien plus Anteil der laufenden). In der Warteschlange steht beim laufenden Eintrag die Prozentzahl.
- "Abbrechen" beendet den aktuell laufenden Prozess (inkl. Kindprozesse) und stoppt den Batch; bereits fertige Dateien bleiben erhalten, die abgebrochene Datei ist als "abgebrochen" markiert, der Rest bleibt "wartend" und kann per erneutem Start weiterverarbeitet werden.
- Nach jeder erfolgreich transkribierten Datei wird deren `.txt` automatisch geladen und im "Ergebnis"-Tab angezeigt (jeweils die zuletzt fertiggestellte); ein Doppelklick auf eine fertige Datei in der Warteschlange oeffnet ihre Textdatei direkt im Standardeditor.
- "Ausgabeordner oeffnen" und "Ergebnis im Editor oeffnen" fuer den Windows-Explorer/Standardeditor.
- Sprache/Modell/Zeitstempel und der zuletzt genutzte Ordner (fuer den Dateidialog) werden in `cache\gui_settings.json` gemerkt und beim naechsten Start vorbelegt. Die Warteschlange selbst wird nicht gespeichert (startet bei jedem GUI-Start leer).

### Ideen fuer die GUI

Die frueheren Ideen (Aufnahme in der GUI, echter Fortschrittsbalken, Verlauf, Editor, LLM-Nachbearbeitung, self-contained Build) sind seit 2026-10-22 im Whisper Studio umgesetzt, siehe oben. Offen: Sprechertrennung (wer sagt was), Pegelanzeige auch fuer den Systemton (das VU-Meter zeigt nur das Mikrofon), Aufnahme pausieren.

## Potenzielle naechste Verbesserungen

### 1. Besseres Modell testen

`large-v3` kann fuer Schweizerdeutsch bessere Resultate liefern, ist auf CPU aber langsamer.

Test:

```powershell
.\whisper\transcribe.ps1 .\whisper\input\teams_meeting.wav -Language de -Model large-v3
```

### 2. Schnellmodus ergaenzen

Ein zusaetzlicher Wrapper koennte `small` als Standard nutzen:

```powershell
.\whisper\transcribe.ps1 .\whisper\input\teams_meeting.wav -Language de -Model small
```

### 3. Nachbearbeitung fuer Hochdeutsch (umgesetzt 2026-10-22, lokal im Studio)

Whisper transkribiert, macht aber keine perfekte sprachliche Redaktion. Fuer bessere Lesbarkeit koennte nach der Transkription ein zweiter Schritt eingefuehrt werden:

- TXT in Abschnitte teilen.
- Schweizerdeutsche/unsaubere Saetze in sauberes Hochdeutsch umformulieren.
- Fachbegriffe erhalten.
- Keine inhaltlichen Aussagen erfinden.

Das waere ein separater LLM-Schritt, lokal oder per Webservice.

### 4. LLM-Webservice optional (Stand 2026-10-22: bewusst lokal, Entscheid Ingo)

OpenAI API ist netzwerkseitig erreichbar. Ohne API-Key wurde korrekt ein Authentifizierungsfehler gemeldet. Wenn erlaubt, koennte ein optionales Skript ergaenzt werden, das die rohe Transkription in ein besser lesbares Protokoll umformt.

Wichtig: Vor Nutzung externer Webservices Datenschutz/Firmenfreigabe pruefen.

### 5. Aufnahme verbessern (Start/Stopp, Dateiname, Pegel, Geraetewahl umgesetzt 2026-10-22 im Studio)

`record_mic.ps1` nimmt weiterhin eine feste Dauer auf. Moegliche Verbesserung:

- Start/Stop interaktiv per Tastendruck.
- Automatische Dateinamen mit Datum/Uhrzeit.
- Pegelanzeige.
- Standardgeraet in Konfigurationsdatei speichern.

## Bekannte Einschraenkungen

- Aufnahme ueber Mikrofon nimmt das auf, was das Mikrofon hoert. Fuer direkte Systemaudio-Aufnahme kann VoiceMeeter oder ein Loopback-Geraet verwendet werden, falls firmenintern erlaubt. *(Gilt nur noch fuer `record_mic.ps1`; das Studio nimmt den Systemton seit 2026-10-22 selbst per WASAPI-Loopback auf, Abschnitt "Whisper Studio".)*
- Schweizerdeutsch wird nicht perfekt als Hochdeutsch ausgegeben.
- `medium` auf CPU kann bei langen Meetings langsam sein.
- `large-v3` braucht mehr Speicherplatz und Zeit.
- Die Hugging-Face Downloads laufen ohne Token. Das funktioniert, kann aber bei vielen Downloads Rate Limits treffen.
- Keine Adminrechte noetig, aber Firmenrichtlinien zur Aufnahme und Verarbeitung von Meetings beachten.

## Installationshistorie

Geprueft:

- Kein systemweites Python/Pip vorhanden.
- Kein `ffmpeg` im PATH noetig, weil `faster-whisper` mit PyAV arbeitet.
- Internetzugriff auf `python.org`, PyPI, GitHub und OpenAI API grundsaetzlich vorhanden.
- Portable Python ZIP wurde verwendet, weil der normale Python-Installer in dieser Umgebung nicht sauber lief.

Installiert:

- Python 3.12.10 embeddable ZIP
- pip via `get-pip.py`
- Python-Pakete:
  - `faster-whisper`
  - `sounddevice`
  - `soundfile`
  - Abhaengigkeiten wie `ctranslate2`, `av`, `huggingface-hub`, `onnxruntime`, `numpy`

## Dateien, die fuer Bedienung wichtig sind

- `ANLEITUNG.md`: fuer Benutzer
- `README.md`: Kurzfassung
- `HANDOFF.md`: technischer Stand und Weiterentwicklung
- `transcribe.ps1`: normaler Einstieg fuer Transkription
- `record_mic.ps1`: normaler Einstieg fuer Aufnahme
- `scripts\transcribe.py`: Hauptlogik fuer Whisper
- `scripts\record_mic.py`: Hauptlogik fuer Aufnahme

## Sicherheits-/Compliance-Hinweis

Teams-Meetings duerfen je nach Firmenregel, Land und Teilnehmerkreis nicht ohne Information oder Zustimmung aufgenommen werden. Vor produktiver Nutzung intern klaeren.
