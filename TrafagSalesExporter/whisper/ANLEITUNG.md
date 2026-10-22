# Whisper Teams Transkription

Diese lokale Installation transkribiert Audioaufnahmen in eine `.txt` Datei. Alles liegt in diesem Ordner `whisper` und funktioniert ohne Adminrechte.

## Was installiert ist

- Portable Python: `whisper\python`
- Transkriptionsskript: `whisper\transcribe.ps1`
- Mikrofon-Aufnahmeskript: `whisper\record_mic.ps1`
- Eingabeordner: `whisper\input`
- Ausgabeordner: `whisper\output`
- Lokaler Modellcache: `whisper\cache\models`
- Standardmodell: `medium`

## Typischer Ablauf

1. Teams-Meeting starten.
2. Aufnahme lokal per Mikrofon starten.
3. Nach dem Meeting transkribieren.
4. Textdatei aus `whisper\output` verwenden.

## Mikrofone anzeigen

Vom Repository-Hauptordner aus ausführen:

```powershell
.\whisper\record_mic.ps1 -ListDevices
```

Merke dir die Nummer des gewünschten Eingabegeräts. Beispiele aus der Geräteliste können sein:

- `Microphone Array`
- `Kopfhörer`
- `VoiceMeeter Output`
- `In 1-2 (MOTU M Series)`

## Audio aufnehmen

10 Minuten mit dem Standard-Mikrofon aufnehmen:

```powershell
.\whisper\record_mic.ps1 -Seconds 600 -OutputFile teams_meeting.wav
```

30 Minuten:

```powershell
.\whisper\record_mic.ps1 -Seconds 1800 -OutputFile teams_meeting.wav
```

Mit bestimmtem Gerät, zum Beispiel Gerät `2`:

```powershell
.\whisper\record_mic.ps1 -Seconds 1800 -OutputFile teams_meeting.wav -Device 2
```

Die Aufnahme landet hier:

```text
whisper\input\teams_meeting.wav
```

## Schweizerdeutsch, Hochdeutsch und Englisch transkribieren

Für Schweizerdeutsch oder gemischtes Deutsch nutze `-Language de`. Das ist bewusst so gewählt, damit Schweizerdeutsch möglichst als gut lesbares Hochdeutsch ausgegeben wird:

```powershell
.\whisper\transcribe.ps1 .\whisper\input\teams_meeting.wav -Language de -Model medium
```

Für automatische Spracherkennung:

```powershell
.\whisper\transcribe.ps1 .\whisper\input\teams_meeting.wav -Language auto -Model medium
```

Für Englisch-only:

```powershell
.\whisper\transcribe.ps1 .\whisper\input\teams_meeting.wav -Language en -Model medium
```

Die Textdatei landet automatisch hier:

```text
whisper\output\teams_meeting.txt
```

## Mit Zeitstempeln transkribieren

```powershell
.\whisper\transcribe.ps1 .\whisper\input\teams_meeting.wav -Language de -Model medium -Timestamps
```

Dann sieht die Ausgabe ungefähr so aus:

```text
[00:00:03 - 00:00:08] Guten Morgen zusammen, wir starten mit dem ersten Punkt.
```

## Andere Audiodateien transkribieren

Du kannst auch vorhandene Dateien in `whisper\input` legen, zum Beispiel:

- `.wav`
- `.mp3`
- `.m4a`
- `.mp4`

Beispiel:

```powershell
.\whisper\transcribe.ps1 .\whisper\input\aufnahme.mp3 -Language de -Model medium
```

## Modellwahl

Empfohlen ist `medium`:

```powershell
-Model medium
```

Optionen:

| Modell | Qualität | Geschwindigkeit | Hinweis |
|---|---|---|---|
| `tiny` | niedrig | sehr schnell | nur Test |
| `small` | mittel | schneller | wenn `medium` zu langsam ist |
| `medium` | gut | mittel/langsam | empfohlen |
| `large-v3` | sehr gut | langsam | lädt beim ersten Mal zusätzlich mehrere GB |

Beispiel mit `small`:

```powershell
.\whisper\transcribe.ps1 .\whisper\input\teams_meeting.wav -Language de -Model small
```

## Wichtige Hinweise

- Schweizerdeutsch wird nicht dialektgetreu geschrieben, sondern möglichst in lesbares Hochdeutsch übertragen.
- Bei gemischten Meetings mit Schweizerdeutsch und Englisch ist `-Language de` meist besser, wenn der Hauptanteil Deutsch/Schweizerdeutsch ist.
- Die erste Nutzung eines noch nicht vorhandenen Modells dauert länger, weil es nach `whisper\cache\models` geladen wird.
- Eine bereits vorhandene Textdatei wird nie überschrieben – auch nicht beim direkten Aufruf über `transcribe.ps1` und auch nicht bei explizitem `-OutputFile`. Das Skript legt stattdessen `<name>_<Datum>_<Uhrzeit>.txt` daneben und gibt am Ende die tatsächlich geschriebene Datei als `Written: ...` aus.
- Alles bleibt im Unterordner `whisper`; es wird nichts systemweit installiert.
- Teilnehmer und Firmenregeln zur Aufnahme von Teams-Meetings beachten.

## Whisper Studio (empfohlen)

Starten: `whisper\gui\WhisperStudio\bin\Release\net9.0-windows\WhisperStudio.exe` (Bauen siehe HANDOFF.md).

- **Transkribieren:** Dateien oder Ordner ins Fenster ziehen. Pro Datei stehen Aufnahmezeit und Titel in der Liste und lassen sich ändern; daraus entsteht der Dateiname, zum Beispiel `2026-10-22_1430_Einkauf.txt`. Unter "Zusätzlich speichern" Word, SRT (Untertitel) oder VTT ankreuzen. "Direkt nach Englisch übersetzen" lässt Whisper selbst ins Englische übersetzen. Fortschritt und Restzeit stehen pro Zeile und unten für alle Dateien.
- **Aufnahme:** Mikrofon wählen, auf Wunsch "Systemton mitschneiden" (dann ist auch zu hören, was die anderen in Teams sagen), Titel eintragen, roten Knopf drücken, zum Beenden nochmals drücken. Die Datei landet in `input` und wird auf Wunsch gleich transkribiert. Teilnehmende vorher informieren.
- **Editor & KI:** Transkript korrigieren und speichern. "Protokoll erstellen", "In Hochdeutsch glätten" und "Übersetzen nach" laufen mit einer lokalen KI auf diesem Laptop, es geht nichts ins Internet. Das dauert auf dem Laptop einige Minuten. Ergebnis prüfen, die KI kann sich irren, dann "Ergebnis speichern".
- **Verlauf:** alle Transkripte, neueste zuerst, mit Suche. Doppelklick öffnet im Editor.

## Alte grafische Oberfläche (GUI)

Statt der PowerShell-Befehle kann auch die kleine Desktop-App genutzt werden:

```powershell
cd whisper\gui\WhisperGui
dotnet build
.\bin\Debug\net9.0-windows\WhisperGui.exe
```

In der GUI:

1. Dateien oder ganze Ordner zur Warteschlange hinzufügen:
   - "Dateien hinzufügen..." für einzelne Dateien (Mehrfachauswahl möglich), oder mehrere Dateien ins Fenster ziehen.
   - "Ordner hinzufügen..." für einen ganzen Ordner: Er wird **rekursiv** nach Mediendateien (Audio/Video) in allen Unterordnern durchsucht, alle gefundenen Dateien landen in der Warteschlange. Ein Ordner kann auch direkt ins Fenster gezogen werden.
2. Sprache und Modell einstellen (gelten für die ganze Warteschlange). Option **"Vorhandene überspringen"** (standardmäßig an): Dateien, für die in `output` schon eine (nicht leere) Textdatei liegt, werden nicht erneut transkribiert. So macht ein abgebrochener oder erneut gestarteter Batch dort weiter, wo er aufgehört hat. Zum kompletten Neu-Transkribieren das Häkchen entfernen.
3. "Start Batch-Transkription" klicken. Die Dateien werden nacheinander abgearbeitet, der Status je Datei (wartend/läuft/fertig/übersprungen/Fehler) wird in der Liste angezeigt. Der obere Balken zeigt den Fortschritt der laufenden Datei in Prozent, daneben stehen Audiolänge, bisherige Laufzeit und die geschätzte Restzeit. Solange das Modell geladen wird, läuft der Balken nur hin und her. Der untere Balken zeigt den Fortschritt der ganzen Warteschlange. Der Balken springt in Schritten, weil Whisper das Audio in Abschnitten von etwa 30 Sekunden verarbeitet. Dateien ohne Audiospur (z.B. Videos ohne Ton) werden automatisch übersprungen.
4. Für jede Datei entsteht eine eigene Textdatei in `output`:
   - Einzeln hinzugefügte Dateien landen direkt in `output` (Dateiname = Eingabedateiname).
   - Bei einem hinzugefügten Ordner wird die **Unterordnerstruktur in `output` gespiegelt**: eine Datei unter `<Ordner>\2026-01\meeting.wav` ergibt `output\2026-01\meeting.txt`. So bleiben gleichnamige Dateien aus verschiedenen Unterordnern getrennt.
   - **Es wird nie eine vorhandene Textdatei überschrieben.** Liegt der Zielname schon da – weil zwei Eingabedateien darauf zielen (z.B. `meeting.wav` und `meeting.mp3` im selben Ordner) oder weil eine frühere Transkription noch dort liegt und "Vorhandene überspringen" aus ist – bekommt die neue Datei den Namen der verarbeiteten Datei plus Zeitstempel: `meeting_20260807_132804.txt`. Im Log steht dann "Vorhanden: meeting.txt bleibt erhalten."
   Das Ergebnis der zuletzt fertiggestellten Datei erscheint im Tab "Ergebnis"; ein Doppelklick auf eine fertige Datei in der Warteschlange öffnet deren Textdatei direkt im Editor.
5. Mit "Entfernen"/"Liste leeren" können noch nicht gestartete Dateien aus der Warteschlange genommen werden, mit "Ausgabeordner öffnen" gelangt man zu allen erzeugten Textdateien.

## Schnellbeispiel

```powershell
.\whisper\record_mic.ps1 -Seconds 1800 -OutputFile teams_meeting.wav
.\whisper\transcribe.ps1 .\whisper\input\teams_meeting.wav -Language de -Model medium
notepad .\whisper\output\teams_meeting.txt
```
