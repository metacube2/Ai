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
- **Aufnahme:** Mikrofon wählen, auf Wunsch "Systemton mitschneiden" (dann ist auch zu hören, was die anderen in Teams sagen), Titel eintragen, roten Knopf drücken, zum Beenden nochmals drücken. Rechts zeigt ein VU-Meter wie am Tonbandgerät die Lautstärke des Mikrofons: Beim Sprechen sollte die Nadel um 0 pendeln. Bleibt sie im roten Bereich oder leuchtet PEAK rot, ist es zu laut (weiter weg vom Mikrofon oder Pegel in Windows senken). Zuckt sie kaum, ist es zu leise. Die Datei landet in `input` und wird auf Wunsch gleich transkribiert. Teilnehmende vorher informieren.
- **Editor & KI:** Transkript korrigieren und speichern. "Protokoll erstellen", "In Hochdeutsch glätten" und "Übersetzen nach" laufen mit einer lokalen KI auf diesem Laptop, es geht nichts ins Internet. Das dauert auf dem Laptop einige Minuten. Ergebnis prüfen, die KI kann sich irren, dann "Ergebnis speichern".
- **Verlauf:** alle Transkripte, neueste zuerst, mit Suche. Doppelklick öffnet im Editor.

## Gut zu wissen

- **Ordner hinzufügen** durchsucht auch alle Unterordner. Die Unterordnerstruktur wird in `output` gespiegelt, gleichnamige Dateien aus verschiedenen Ordnern bleiben also getrennt.
- **Vorhandene überspringen** (standardmäßig an): Dateien, für die schon ein Transkript da ist, werden nicht nochmals transkribiert. Ein abgebrochener Stapel macht so dort weiter, wo er aufgehört hat. Das klappt auch, wenn der Titel geändert wurde.
- **Es wird nie ein vorhandenes Transkript überschrieben.** Ist der Name schon belegt, bekommt die neue Datei einen Zeitstempel angehängt.
- Dateien ohne Tonspur (zum Beispiel Bildschirmvideos ohne Ton) werden übersprungen.
- Die frühere kleine Oberfläche (`WhisperGui`) wurde am 22.10.2026 entfernt. Die Desktop-Verknüpfung "Whisper" startet das Studio.

## Schnellbeispiel

```powershell
.\whisper\record_mic.ps1 -Seconds 1800 -OutputFile teams_meeting.wav
.\whisper\transcribe.ps1 .\whisper\input\teams_meeting.wav -Language de -Model medium
notepad .\whisper\output\teams_meeting.txt
```
