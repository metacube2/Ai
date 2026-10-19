# Vericrypt — Technik

Stand: 2026-09-16

## Verwürfelung

- Segment: 30 ms, Frame: 16 Segmente = 480 ms
- Permutation aus SHA-256 über Schlüssel und Frame-Zähler, daraus der Startwert für
  `numpy.random.RandomState` (dessen Zufallsfolge ist über numpy-Versionen stabil,
  die neuere `Generator`-API ist es nicht)
- Vor dem Verwürfeln wird die Sprache auf 300–3000 Hz begrenzt, danach nochmals
  tiefpassgefiltert: Die Sprünge an den Segmentgrenzen streuen sonst breitbandig in
  das Sync-Band

## Verfahren 1: FSK 3,8/4,4 kHz

Oberhalb des Sprachbands, 100 Baud, 48 Bit je Frame:

```
SYNC (16) | ZÄHLER (16) | CRC-8 (8) | FÜLLBITS (8)
```

- Sync-Wort `0x066B`, per Brute Force gewählt: Nebenmaxima der Autokorrelation
  höchstens 2 von 16
- Empfänger: nullphasige Filterung, I/Q-Mischung (dadurch unabhängig von der
  Trägerphase), Integration über ein Bit, Korrelation mit dem Sync-Wort über alle
  Sample-Positionen → **samplegenaue** Frame-Lage
- Nachführung mit Alpha-Beta-Filter: glättet die Messung und schätzt, wie stark ein
  Frame vom Sollmaß abweicht (Gleichlauf von Band oder Soundkarte). Jeder Frame wird
  auf die Sollänge umgerechnet
- Fehlt der Sync kurz, läuft der Empfänger im Schwungrad weiter; ohne Sync kommt
  Stille statt Rauschen

## Verfahren 2: Pilotton 1200/1800 Hz

Im Sprachband, damit funk- und telefontauglich.

- **1200 Hz Dauerton** als Takt. Aus seiner Phase errechnet der Empfänger, wie weit
  die Sender-Zeit an jeder Stelle fortgeschritten ist, und rechnet das Signal darauf
  um. Das ist eine Zeitbasiskorrektur wie bei Pilotton-Tonbändern: Gleichlauf- und
  Geschwindigkeitsfehler sind danach weg
- **1800 Hz** trägt Daten mit weich geformter Phasenumtastung, ein Symbol je Segment
  (33 Baud, belegt ±25 Hz). Ein Superframe aus zwei Frames trägt
  `BARKER-13 | ZÄHLER (11 Bit) | CRC-8`
- **Kerbfilter:** Elliptische Bandsperren (Durchlass ab ±55 Hz, mindestens 50 dB
  Dämpfung innerhalb ±30 Hz) räumen die beiden Pilotbänder in der Sprache frei. Der
  Empfänger entfernt den Pilot, **bevor** entwürfelt wird, denn nur dort ist er
  lückenlos
- Suche im Empfänger: Mischung auf das Basisband, Integration über ein Symbol
  (löscht den 1200-Hz-Takt exakt aus), 21-fache Reduktion, Korrelation mit Barker-13,
  Feinabgleich gegen den vollständig bekannten Symbolzug

**Warum genau 1200 und 1800 Hz?** Beide haben ganzzahlig viele Schwingungen pro
30-ms-Segment (36 bzw. 54) und pro 10-ms-Bit. Der Pilot bleibt dadurch auch nach dem
Permutieren lückenlos. Gemessen: Bei 1210 Hz bleiben nach dem Kerbfilter −6 dB übrig,
bei 1200 Hz praktisch nichts.

## Verfahren 3: BBC 1830 Hz (Original)

Nachbildung des Pilottons im Vericrypt/Cryptophon 1100: **ein** FSK-Ton
1830 ± 100 Hz (Mark 1730 Hz, Space 1930 Hz), 100 Baud, gleicher Rahmen wie Verfahren 1.

- Bandsperre 1580–2080 Hz (≥ 50 dB, Durchlass ab ±80 Hz davor und dahinter) räumt
  das Pilotband in der Sprache frei; der Empfänger entfernt den Pilot mit derselben
  Sperre vor dem Entwürfeln
- Empfänger wie Verfahren 1, nur mit Bandpass 1580–2080 Hz statt Hochpass
- Keine Zeitbasiskorrektur: Bandgleichlauf wird nur über den Alpha-Beta-Filter
  nachgeführt
- Bitfolge und Baudrate des Originals sind nicht dokumentiert; übernommen sind nur
  Frequenz, Hub, 30-ms-Segmente und der sechsstellige Schlüssel

## Frequenzinversion

Spiegelung um 3300 Hz mit Vor- und Nachfilter. Ohne Nachfilter bleibt das obere
Seitenband (3300 + f) hörbar. Zweimal angewendet ergibt sich das Original, bis auf
eine konstante Phasendrehung aller Anteile, die praktisch unhörbar ist.

## Historische Vorbilder

- **Vericrypt 1100** von Brown, Boveri & Cie (BBC) — Zeitsegment-Scrambler, unter
  anderem in Polizeinetzen. „BBC" ist hier nicht die britische Rundfunkanstalt.
  30-ms-Segmente, FSK-Pilot 1830 Hz (± 100 Hz), 6-stelliger Tagesschlüssel plus fester
  Grundschlüssel, Zufallsgenerator mit ca. 8,5 h Periode, ca. 1,5 s Einrasten,
  ca. 0,5 s Verzögerung. Vorbild für Verfahren 3
- **US 3,610,828** — Takt- und Rahmensignal (2150 Hz und 67 Hz) werden offen
  mitgesendet und beim Empfänger mit Kerbfiltern entfernt. Das entspricht dem
  Pilotton-Verfahren
- **US 4,268,720** — 0,5 s langer 600-Hz-Burst alle 10 Sekunden, Sprache wird dafür
  unterbrochen, dazwischen läuft es frei auf Quarzbasis
- **US 4,305,152** — Rahmen- und Taktsignal im verwürfelten Signal eingebettet

Quellen: [prc68.com – Voice Scrambler US Patents](http://www.prc68.com/I/VoiceX.shtml),
[US4268720A](https://patents.google.com/patent/US4268720A/en),
[NCJRS zum Vericrypt 1100](https://www.ojp.gov/ncjrs/virtual-library/abstracts/integration-time-division-speech-scrambling-police),
[Crypto Museum – BBC Vericrypt 1100](https://www.cryptomuseum.com/crypto/bbc/vericrypt/index.htm),
[Klaus Paffenholz – BBC Cryptophon 1100](https://www.klaus-paffenholz.de/bos-funk/bbc-vericrypt-1100/cryptophon1100dt.html)

## Verworfene Varianten

| Variante | Warum verworfen |
| --- | --- |
| FSK 100 Baud auf 1200/1800 Hz mit breiten Bandsperren (±250 Hz) | Nimmt der Sprache 9,5 dB im Formantbereich 1–2 kHz, klingt deutlich hohler |
| FSK 25 Baud, Sperren ±115 Hz | Nur 12 Bit je Frame, der Zähler müsste über mehrere Frames verteilt werden |
| Pilot ohne Kerbfilter (wie in der alten Version) | Sprache überdeckt den Pilot: Frame-Nummer kam meist an, die Position nur in 6 von 27 Frames |
