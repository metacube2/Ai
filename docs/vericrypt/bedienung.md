# Vericrypt — Bedienung

Stand: 2026-09-16

## Starten

- **Schreibtisch:** Doppelklick auf `Vericrypt.command`
- **Terminal:** `cd Ai && ../venv/bin/python _Loose-Dateien/cryptov2.py`

Benötigt numpy, scipy und sounddevice; alle sind im venv vorhanden. macOS fragt beim
ersten Start nach dem Mikrofon.

## Fenster

| Bereich | Bedienung |
| --- | --- |
| **AUDIO DEVICES** | Ein- und Ausgabegerät, 🔄 liest die Liste neu ein |
| **CRYPTO** | Betriebsart CLEAR / ENCRYPT / DECRYPT, Sync-Verfahren, Schlüssel |
| **EFFEKTE** | Frequenzinversion, Bandpass 300–3400 Hz |
| **SEGMENT-RING** | Zeigt die aktuelle Permutation, den Frame-Zähler und bei DECRYPT, ob der Sync steht |
| **PEGEL** | Ein- und Ausgangspegel in dB |

## Betriebsarten

| Modus | Bedeutung |
| --- | --- |
| **CLEAR** | Ton wird nur durchgereicht, Effekte wirken |
| **ENCRYPT** | Sprache wird verwürfelt, das Ergebnis geht auf den Ausgang (Aufnahme aufs Band, Funkgerät) |
| **DECRYPT** | Verwürfeltes Signal wird zurückgerechnet |

## Sync-Verfahren

| Verfahren | Wofür | Latenz Senden / Empfangen |
| --- | --- | --- |
| **FSK 3,8/4,4 kHz** | Nur Soundkarte oder Tonband. Über Funk und Telefon **nicht** nutzbar, weil diese nur bis etwa 3 kHz übertragen | ~0,5 s / ~1 s |
| **Pilotton 1200/1800 Hz** | Im Sprachband, damit auch über Funk und Telefon. Nähe am historischen Vorbild | ~0,6 s / ~1,6 s |

**Beide Seiten müssen dasselbe Verfahren und denselben Schlüssel benutzen.** Das
Umschalten wirkt sofort, auch während der Stream läuft.

## Schlüssel

Sechsstellig, 000000 bis 999999. Aus Schlüssel und Frame-Zähler wird die Permutation
der 16 Segmente berechnet. Eine ungültige Eingabe wird rot angezeigt und **nicht**
stillschweigend durch einen Standardwert ersetzt; es bleibt der zuletzt gültige
Schlüssel aktiv.

## Effekte

| Effekt | Wirkung |
| --- | --- |
| **Frequenzinversion** | Spiegelt das Band 300–3000 Hz um 3300 Hz. Zweimal angewendet ergibt wieder das Original, muss also beim Senden und Empfangen gleich gesetzt sein |
| **Bandpass 300–3400 Hz** | Telefonklang |

## Typische Fehlerbilder

| Symptom | Ursache |
| --- | --- |
| „NO SYNC" im Ring | Falsches Verfahren, zu leises Signal oder Kanal überträgt den Pilot nicht (bei FSK über Funk immer) |
| Entschlüsseltes klingt zerhackt | Falscher Schlüssel, oder Sync steht nicht |
| Ton klingt hohl | Normal: Das Sprachband ist auf 300–3000 Hz begrenzt, beim Pilotton kommen zwei schmale Kerben dazu |
| Erst Stille beim Start | Gewollt. Bis der erste Frame fertig ist, wird nichts ausgegeben, damit kein Klartext durchrutscht |
| Rückkopplung | Mikrofon und Lautsprecher im selben Raum, Kopfhörer benutzen |
