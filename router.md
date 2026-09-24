# Router — globaler Einstieg

Stand: 2026-09-22

Dies ist der **einzige** globale Einstieg in die Dokumentation dieses Repositories.
Von hier führt genau ein Schritt in einen Themenast, von dort genau ein Schritt in
die Detaildatei.

**Ladeweg für eine Aufgabe: `router.md` → ein Unterrouter → eine Detaildatei.**

## Vorrangregeln

1. **Gemessene Werte schlagen Schätzungen.** Jede Zahl in der Doku nennt, womit sie
   gemessen wurde. Wo nichts gemessen wurde, steht das ausdrücklich dabei.
2. **Arbeitsnotizen belegen den Tag ihrer Messung, nicht heute.** Für den aktuellen
   Stand gelten Code und die Detaildateien der Themenäste.
3. **Was nicht getestet ist, wird als ungetestet gekennzeichnet.** Besonders alles,
   was echte Hardware braucht (Funkgerät, Tonband, Mikrofon, Lautsprecher).
4. **Der Code ist die Wahrheit über das Verhalten.** Die Doku beschreibt Absicht,
   Bedienung und Messwerte, nicht jede Codezeile.
5. **Aus Fehlern lernen.** Vor jeder Aufgabe `worldmodell.md` lesen. Jeder Fehler,
   der bei einer Aufgabe passiert, wird dort mit Ursache und Regel nachgetragen.

## Themenäste

| Ast | Wofür | Unterrouter |
| --- | --- | --- |
| **Bildeffekte** | Bildschirm- und Kameraeffekte, Ton-Effekte, Verwandlungen einschließlich Matrix-Gesichtsmorph, Plugin-System | `docs/bildeffekte/router.md` |
| **Vericrypt** | Sprachverschlüsselung im Stil der 70er/80er (`_Loose-Dateien/cryptov2.py`) | `docs/vericrypt/router.md` |
| **Arbeitsnotizen** | Was an einem Tag gemessen und entschieden wurde | `docs/arbeitsnotizen/` |
| **Weltmodell** | Learnings aus Fehlern: Werkzeuge, Umgebung, Numerik, Git – vor jeder Aufgabe lesen | `worldmodell.md` |

## Übrige Projekte

Diese Projekte liegen im Repository und bringen eigene Dokumentation mit. Sie sind
hier nur verzeichnet, nicht beschrieben:

`ansi-noise-clock`, `aurora-livecam`, `AudioVUMeter`, `dctp`, `familyalbums`,
`FT991A-Remote`, `GeometricResonator`, `gitpusher`, `HealthBridge`, `NexusBridge`,
`paperless-report`, `photography-tools`, `powerbi` (+ `powerbi.md`),
`ProductPromotion`, `PsytranceVisualizer`, `RollkofferSimulator`, `trade_web`,
`TrafagSalesExporter` (eigener Router: `TrafagSalesExporter/router.md`), `training`,
`twelve-tone-synthesizer`, `video-converter-suite`, `jobcontrol.md`.

## Wenn der Ast nicht klar ist

| Frage | Ast |
| --- | --- |
| „Wie starte ich die Bildeffekte?" | Bildeffekte |
| „Wie schreibe ich einen eigenen Effekt?" | Bildeffekte → Plugins |
| „Warum klingt der entschlüsselte Ton falsch?" | Vericrypt |
| „Welcher Sync-Modus funktioniert über Funk?" | Vericrypt |
