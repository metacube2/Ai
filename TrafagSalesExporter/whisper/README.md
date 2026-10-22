# Local Whisper transcription setup

Everything is contained in this folder.

## Transcribe an existing recording

Put audio/video files into `input`, then run from the repository root:

```powershell
.\whisper\transcribe.ps1 .\whisper\input\meeting.wav -Language auto -Model medium
```

Output goes to `output\meeting.txt`.

For Swiss German-heavy meetings, use German mode:

```powershell
.\whisper\transcribe.ps1 .\whisper\input\meeting.wav -Language de -Model medium
```

For English-only meetings:

```powershell
.\whisper\transcribe.ps1 .\whisper\input\meeting.wav -Language en -Model medium
```

For timestamps:

```powershell
.\whisper\transcribe.ps1 .\whisper\input\meeting.wav -Language de -Timestamps
```

## Record from microphone

List devices:

```powershell
.\whisper\record_mic.ps1 -ListDevices
```

Record 10 minutes with the default input device:

```powershell
.\whisper\record_mic.ps1 -Seconds 600 -OutputFile teams_meeting.wav
.\whisper\transcribe.ps1 .\whisper\input\teams_meeting.wav -Language de -Model medium
```

Record with a specific device id from the list:

```powershell
.\whisper\record_mic.ps1 -Seconds 600 -OutputFile teams_meeting.wav -Device 2
```

The `medium` model is already downloaded. Additional models download into `cache\models` on first use.

## Whisper Studio (main app since 2026-10-22)

`gui\WhisperStudio` (WPF, Windows 11 look): queue with date/time/title naming, progress, Word/SRT/VTT export, recording of microphone plus system sound, editor, history and a local LLM (llama.cpp in `llm\`) for minutes, clean-up and translation. Details in HANDOFF.md.

```powershell
cd whisper\gui\WhisperStudio
dotnet build -c Release
.\bin\Release\net9.0-windows\WhisperStudio.exe
```

## Old GUI

The older WinForms app lives in `gui\WhisperGui` (file picker, start button, live log, result view). See ANLEITUNG.md/HANDOFF.md for details. Build and run:

```powershell
cd whisper\gui\WhisperGui
dotnet build
.\bin\Debug\net9.0-windows\WhisperGui.exe
```
