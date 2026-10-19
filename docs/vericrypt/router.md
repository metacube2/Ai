# Vericrypt — Unterrouter

Stand: 2026-09-16

Sprachverschlüsselung im Stil der 70er/80er: Die Sprache wird in 30-ms-Segmente
zerlegt und permutiert, ein Synchronsignal hält Sender und Empfänger zusammen.

**Programm:** `_Loose-Dateien/cryptov2.py`
**Start:** `Vericrypt.command` auf dem Schreibtisch, oder
`venv/bin/python _Loose-Dateien/cryptov2.py`

## Detaildateien

| Datei | Wofür |
| --- | --- |
| `bedienung.md` | Starten, Betriebsarten, Schlüssel, Effekte, typische Fehlerbilder |
| `technik.md` | Rahmenformat, beide Sync-Verfahren, Zeitbasiskorrektur, historische Vorbilder |
| `messwerte.md` | Kanalsimulation, Ergebnisse je Verfahren, behobene Fehler, was ungetestet ist |

## Kurzfassung

| Frage | Antwort |
| --- | --- |
| Welches Sync-Verfahren wofür? | **Pilotton 1200/1800 Hz** für Funk und Telefon, **FSK 3,8/4,4 kHz** nur für Soundkarte und Tonband, **BBC 1830 Hz** klingt wie das Original |
| Entschlüsselt die alte Version? | Nein. Vor der Überarbeitung kam nur Rauschen heraus, auch ohne Störung |
| Wie gut ist es jetzt? | Korrelation 1,00 im idealen Kanal, 0,88 über einen simulierten Funkkanal |
| Sind alte Aufnahmen noch lesbar? | Nein, das Format hat sich geändert. Sie waren aber auch vorher nicht entschlüsselbar |
