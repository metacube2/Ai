# Vericrypt — Messwerte

Stand: 2026-09-16

## Verfahren

Deutsche Testsprache (macOS `say`), vollständige Kette in Blöcken von 1024 Samples:
**Verschlüsseln → simulierter Kanal → Entschlüsseln**. Der Kanal kann verzögern,
dämpfen, rauschen, das Tempo verändern und die Bandbreite begrenzen.

Kennzahl ist die Korrelation mit dem Original, in 250-ms-Fenstern mit lokaler
Lagesuche gemessen (unempfindlich gegen Laufzeit, empfindlich gegen Zerhacken).
1,00 ist perfekt, etwa 0,2 entspricht Rauschen.

Die Prüfskripte lagen außerhalb des Repositories und sind **nicht eingecheckt**.

## Ergebnisse

| Kanal | alte Version | FSK 3,8/4,4 kHz | Pilotton 1200/1800 Hz |
| --- | ---: | ---: | ---: |
| ideal | 0,22 | **1,00** | 0,89¹ |
| 12 000 Samples Verzögerung, halber Pegel | 0,17 | **1,00** | 0,89 |
| leise + Rauschen | 0,19 | **0,99** | 0,83 |
| Band 0,5 % zu schnell | 0,18 | 0,91 | **0,89** |
| Band 1 % zu langsam | 0,17 | 0,89 | **0,89** |
| Gleichlaufschwankung ±0,3 % | – | 0,92 | **0,89** |
| **Funkkanal 300–3000 Hz** | – | **kein Sync** | **0,88** |
| Funk + Rauschen + 0,5 % | – | kein Sync | 0,82 |
| falscher Schlüssel (soll schlecht sein) | 0,20 | 0,28–0,35 | 0,34 |

¹ Der Pilotton liegt nur scheinbar zurück: Wird die Referenz mit denselben Kerbfiltern
versehen, ergibt sich 0,98. Die Differenz ist das, was die Kerben wegnehmen.

**Kernaussagen**

- Die alte Version konnte **nie** entschlüsseln, auch nicht im idealen Kanal.
- FSK ist im sauberen Kanal exakt, über Funk aber unbrauchbar.
- Der Pilotton hält Tempo- und Gleichlauffehler konstant aus, weil die Zeitbasis
  laufend nachgeführt wird.

## Weitere Messungen

| Messung | Wert |
| --- | --- |
| Rechenzeit je Block, Dauerbetrieb | Senden 3,1 ms, Empfangen 5,6 ms (Budget 23 ms) |
| Erster Block nach Moduswechsel | 14 ms (einmaliger Aufbau) |
| Pilot-Rest im entschlüsselten Ton | −88 dBFS bei −31 dBFS im Kanal, unhörbar |
| Sprachverlust durch die Kerben (1–2 kHz) | etwa −1 dB |
| Gesamtlatenz | FSK 1,05 s, Pilotton 2,1 s |
| Frequenzinversion | Testtöne 500/1000/2000 Hz werden exakt zu 2800/2300/1300 Hz und zurück |

## Gefundene und behobene Fehler der alten Version

1. **Crossfade kürzte jeden Frame** um rund 1000 Samples, die Segmentgrenzen passten nie.
2. **Keine Positions-Synchronisation:** Übertragen wurde nur die Frame-Nummer.
3. **Sync mitten im Sprachband** (1200/1800 Hz ohne Kerbfilter), von der Sprache überdeckt.
4. **Demodulator war phasenabhängig** (nur Sinus-Korrelation), Bits fielen zufällig aus.
5. **Klartext-Leck:** Bei Pufferunterlauf ging die unverschlüsselte Sprache mit 30 % Pegel raus.
6. **Filter ohne Zustand** über Blockgrenzen: Klicks alle 23 ms.
7. **Lücke zwischen Frame 0 und 1** beim Start (1024 Samples Stille), der Frame-Takt sprang.
8. Kleinere: Reset aus dem GUI-Thread, Mausrad auf macOS ohne Funktion, ungültiger
   Schlüssel wurde stillschweigend ersetzt.

## Nicht getestet

- **Echtes Funkgerät, echtes Tonband, Lautsprecher und Mikrofon.** Alle Ergebnisse
  stammen aus der Simulation.
- **Verständlichkeit durch Zuhören.** Es wurde gemessen, nicht angehört.
- Variante C wurde nur am Rechner geprüft, nicht über eine echte Funkstrecke.
