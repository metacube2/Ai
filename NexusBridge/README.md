---
title: Nexus Bridge — VST3-Anbindung an den Media-AI-Dienst
topic: apps.nexusbridge
tags: [vst3, juce, plugin, media-ai, vorleser, tts, ace-step, cubase, ableton]
status: current
updated: 2026-09-07
---

# Nexus Bridge

VST3-Plugin (und Standalone-App), das aus der DAW heraus den lokalen
Media-AI-Dienst auf Port 8013 anspricht. Geprueft in **Cubase 15**; Ableton Live 12 Suite liegt unter
`/Volumes/ExtOsX/Programme/` und ist damit ungetestet, aber VST3-faehig. Erste Ausbaustufe: der
**Vorleser** — Text hinein, gesprochenes Audio zurueck, abspielbar im Plugin und
per Ziehen in die Spur.

## Was das Plugin ist und was nicht

Es ist **kein Echtzeit-Effekt**. Qwen3-TTS rechnet Sekunden bis Minuten,
ACE-Step steht zusaetzlich in der gemeinsamen Warteschlange mit Bild und Video.
Das Plugin ist ein Generator: Auftrag abschicken, Ergebnis abholen, in die Spur
ziehen. HTTP laeuft ausschliesslich auf Hintergrundthreads, `processBlock`
belegt keinen Speicher und nimmt keine Sperre.

Beim Schliessen wartet das Plugin **nicht** auf einen laufenden Auftrag. Der
Thread laeuft zu Ende und sein Ergebnis wird verworfen — sonst wuerde die DAW
beim Schliessen minutenlang haengen (`Source/Auftrag.h`).

## Stand der Tabs

| Tab | Zustand |
|---|---|
| Vorleser | fertig — `/reader/status`, `/reader/generate`, `/reader/output` |
| Song | Platzhalter — `/song/generate` existiert, ist noch nicht angebunden |
| EQ & Restaurieren | fertig — `/restore/status`, `/restore/upload`, `/restore/analyse`, `/restore/run` |

## EQ & Restaurieren

Der Tab spricht das Modul `n8n/automation/mai/restore.py` an. Zwei Strecken:

- **Frequenzgang uebertragen** — das mittlere Spektrum einer Referenz wird in
  Terzbaendern mit dem der Quelle verglichen, die Differenz als
  linearphasiger FIR angewandt. Ueber der Signalkante der Quelle laeuft die
  Kurve auf 0 dB aus, damit kein Rauschen angehoben wird.
- **Entrauschen** — DeepFilterNet3 aus `dfn-env`.
- **Superresolution** — AudioSR aus `audiosr-env`, erzeugt Inhalt oberhalb der
  Bandgrenze neu und gibt immer 48 kHz aus. Rechnet ohne CUDA auf der CPU: ein
  Vielfaches der Spieldauer, die Schrittzahl ist der Hebel.

Der Dienst erzwingt die Reihenfolge entrauschen → hochrechnen → Frequenzgang.

Die Quelle kommt auf drei Wegen:

1. **Von der Spur aufnehmen** — der Knopf schneidet mit, was in das Plugin
   hineinlaeuft, also das Material der Spur, auf der es liegt. Druecken,
   Spur abspielen, erneut druecken: die Aufnahme geht direkt als Quelle an den
   Dienst. Geschrieben wird 24 Bit in der Projektrate auf einem eigenen Thread
   (`AudioFormatWriter::ThreadedWriter`), der Audiothread schiebt nur in die
   Warteschlange. Aufgenommen wird der Eingang **vor** der eigenen Wiedergabe,
   damit sich das Plugin nicht selbst mitschneidet.
2. **Datei als Quelle …** — irgendeine Datei von der Platte.
3. **Aktuellen Clip als Quelle** — das Ergebnis aus dem Vorleser-Tab. Das Ergebnis wird als Clip geladen, laesst
sich also sofort abspielen, in die Spur ziehen oder sichern. Die Anzeige
zeichnet Quellspektrum, Referenzspektrum und angewandte Kurve.

Fuer diesen Tab braucht das Konto das Recht **`restore`**; das Konto aus
`config.env` (Rolle `user`) hat es nicht.

Ein Vorbehalt zur Superresolution: das Modell **erfindet** Obertoene, die nie
aufgenommen wurden. Fuer Musik und Effekt egal, fuer Zeitzeugenmaterial ist das
eine Aussage ueber das Original, die so nicht stimmt.

## Stand am 2026-09-07

Gebaut und nach `~/Library/Audio/Plug-Ins/VST3/Nexus Bridge.vst3` installiert;
Standalone unter `build/NexusBridge_artefacts/Release/Standalone/`. JUCE 9.0.2
in `~/JUCE`, gebaut mit CMake und Unix Makefiles (kein volles Xcode auf dieser
Maschine).

**Bestaetigt:** Endpunkte und Auth gegen 8013, Stimmenliste, Spracherzeugung,
Aufbau der Oberflaeche, Umlaute (JUCE `String(const char*)` nimmt ASCII an —
alle Literale mit Umlaut liegen als `CharPointer_UTF8` vor).

**Noch nicht im Betrieb bestaetigt:** das Ziehen in eine Cubase-Spur nach der
Umrechnung auf die Projektrate, die Aufnahme von der Spur, und der
Restaurieren-Tab (dessen Konto braucht erst das Recht `restore`).

**Naechste sinnvolle Schritte, falls es weitergeht:**

1. Song-Tab anbinden — `/song/generate` ist vorhanden und kennt Stil, Text,
   Laenge, BPM, Tonart, Schritte, CFG, LoRAs, Referenz und Parameterreihen.
   Anders als Vorleser und Restaurieren laeuft er ueber die gemeinsame
   Warteschlange; der Tab braucht deshalb Warteposition und Zustand aus
   `/song/status` und `/song/queue`, nicht nur einen Balken.
2. Ein Zeitlimit oder Abbruch fuer den Superres-Schritt — drei Stunden
   blockierte Route ohne Rueckmeldung sind im Plugin unangenehm.
3. AU zusaetzlich zu VST3, falls je gebraucht: dafuer muesste das volle Xcode
   installiert werden.

## Bauen

JUCE 9 wird ausserhalb des Workspace erwartet, damit kein Vendor-Code im
Repository landet:

```bash
git clone --depth 1 https://github.com/juce-framework/JUCE.git ~/JUCE
cd /Users/metacube/Projects/Git/Ai/NexusBridge
cmake -B build -G "Unix Makefiles" -DCMAKE_BUILD_TYPE=Release
cmake --build build --target NexusBridge_VST3 NexusBridge_Standalone -j 8
```

Auf diesem Mac ist nur das Command Line Tools SDK installiert, kein volles
Xcode. `-G Xcode` schlaegt deshalb fehl, `Unix Makefiles` (oder Ninja) ist der
Weg. Aus demselben Grund wird **kein AudioUnit** gebaut — Cubase und Ableton
laden ohnehin VST3.

`COPY_PLUGIN_AFTER_BUILD` legt das Ergebnis direkt nach
`~/Library/Audio/Plug-Ins/VST3/Nexus Bridge.vst3`. Cubase findet es nach einem
Neustart. Nach jedem Neubau muss Cubase neu gestartet werden, sonst laeuft die
alte geladene Fassung weiter.

Zum Ausprobieren ohne DAW:
`build/NexusBridge_artefacts/Release/Standalone/Nexus Bridge.app`.

## Verbindung und Rechte

Oben rechts **Verbindung** oeffnet Host, Port, Benutzer und Passwort.
**Aus config.env** liest `MEDIA_AI_API_PORT`, `MEDIA_AI_AUTH_USER` und
`MEDIA_AI_AUTH_PASSWORD` aus `n8n/automation/config.env`.

Die Zugangsdaten liegen unverschluesselt in
`~/Library/Application Support/Metacube/NexusBridge.settings` — dieselbe
Vertrauensstufe wie `config.env` selbst, aber es ist eine zweite Kopie.

Der Dienst prueft Rechte pro Bereich (`mai/benutzer.py`, `route_permissions`):

- Vorleser braucht `reader` — das Konto aus `config.env` (Rolle `user`) hat es.
- EQ & Restaurieren braucht `restore` — im Benutzer-Tab des Dashboards zu setzen.
- Song wird `song` brauchen — dieses Konto hat es **nicht**, `/song/status`
  antwortet mit `bereich_nicht_freigegeben`. Vor dem Song-Tab also entweder ein
  Admin-Konto eintragen oder dem Konto ueber `/users/permissions` das Recht
  geben.

## Bedienung des Vorleser-Tabs

1. **Neu laden** holt Stimmerzeugungen, Stimmen und die letzten 20 Ausgaben.
2. Stimmerzeugung waehlen: `macOS Stimmen` (schnell, Systemstimmen) oder
   Qwen3-TTS mit den geklonten Stimmen aus dem Dashboard.
3. Text eingeben, hoechstens 10000 Zeichen — der Dienst weist mehr ab.
4. **Sprechen lassen**. Der Balken laeuft unbestimmt, weil der Dienst keine
   Restzeit meldet.
5. Ergebnis erscheint als Wellenform. **Abspielen** mischt es in den
   Plugin-Ausgang.
6. **Frühere Ausgaben** laedt eine aeltere Datei ohne neue Rechnung.

## Wie das Audio in die Spur kommt

Ein VST3-Plugin kann kein Audio in die Anordnung der DAW schreiben - die
Schnittstelle sieht das nicht vor. Ein Knopf "in Spur kopieren" waere gelogen.
Es gibt drei ehrliche Wege, alle drei sind eingebaut:

1. **In die Spur ziehen** - der gestrichelte Griff unter der Wellenform. Ziehen
   (nicht klicken) legt die WAV-Datei auf einer Spur ab. Das ist derselbe Weg,
   den Melodyne oder Serato Sample benutzen. In Cubase 15 geprueft.
2. **Mit DAW-Transport abspielen** - Haken setzen, dann startet der Clip mit der
   Wiedergabe und laeuft ueber den Plugin-Ausgang.
   In Cubase nimmt ihn ein **Audio-Mixdown ueber den Locatorbereich** auf, oder
   ein **Render in Place** auf einer Spur, die im Locatorbereich ein Event hat.
   Auf einer leeren Audiospur liefert Render in Place bzw. Freeze **Stille** -
   dort gibt es kein Event, das gerendert werden koennte. Fuer den Fall gehoert
   das Plugin auf eine Gruppen- oder FX-Spur.
3. **Sichern unter …** - legt die Datei irgendwohin und wird von dort normal
   importiert.

### Abtastrate

Der Dienst liefert **24000 Hz** (Qwen3-TTS) beziehungsweise **22050 Hz**
(macOS-Stimmen). Cubase rechnet eine gezogene Datei nicht zwingend auf die
Projektrate um und spielt sie dann zu schnell ab - eine 24000er Datei in einem
48000er Projekt klingt genau doppelt so schnell.

Das Plugin legt deshalb neben der Originaldatei eine Fassung in der Projektrate
an (`…_48k.wav`) und uebergibt beim Ziehen und beim Sichern **diese**. Aendert
sich die Projektrate, wird die Fassung neu erzeugt. Stimmen Projekt- und
Dateirate ueberein, geht das Original direkt raus.

Alle Dateien fuer die DAW liegen unter `~/Music/NexusBridge/` - bewusst nicht in
`/tmp`, weil Cubase eine gezogene Datei je nach Einstellung nur referenziert
statt sie ins Projekt zu kopieren.

## Aufbau

```text
Source/
├── ApiClient.{h,cpp}        Basic-Auth-HTTP gegen 8013, JSON, Datei-Up/Download
├── Auftrag.h                Hintergrundthread mit Abmeldung statt Warten
├── PluginProcessor.{h,cpp}  Clip, Wiedergabe, Spuraufnahme, Ratenumrechnung
├── PluginEditor.{h,cpp}     Kopfzeile, Tabs, Zugangsueberlagerung
├── ReaderPanel.{h,cpp}      Vorleser-Tab, Wellenanzeige, Ziehgriff
├── RestorePanel.{h,cpp}     EQ- und Restaurieren-Tab, Kurvenanzeige
├── PlatzhalterPanel.{h,cpp} Song-Tab, solange er nicht angebunden ist
└── NexusLookAndFeel.{h,cpp} Farben und Zeichnung
```

Die Spuraufnahme laeuft ueber `AudioFormatWriter::ThreadedWriter` auf einem
eigenen Thread; `processBlock` schiebt nur in die Warteschlange und schreibt nur
dann, wenn der Block mindestens so viele Kanaele hat wie der Schreiber erwartet.

Die Transportkopplung liest im `processBlock` den Playhead und startet den Clip
auf der steigenden Flanke von `getIsPlaying()`; sie ist der einzige Weg, auf dem
ein Bounce das Ergebnis aufnimmt.

Die Uebergabe des Clips an den Audiothread laeuft ueber einen
referenzgezaehlten Puffer plus `std::atomic<ClipPuffer*>`; ein Timer im
Prozessor gibt Puffer frei, die weder angefordert noch vom Audiothread gehalten
werden. Liegt der Dienst auf demselben Rechner, kopiert `ApiClient::holeDatei`
die Ausgabedatei direkt statt sie ueber HTTP zu laden.
