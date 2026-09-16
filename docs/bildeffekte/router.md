# Bildeffekte — Unterrouter

Stand: 2026-09-16

Echtzeit-Effekte auf einen frei wählbaren Bildschirmbereich, dazu Ton-Effekte, die
vom Bild gesteuert werden. Oberfläche in PySide6, alle Effekte sind Plugins.

**Start:** `Bildeffekte.command` auf dem Schreibtisch, oder
`venv/bin/python video.py` im Ordner `Ai`.

## Dateien

| Pfad | Inhalt |
| --- | --- |
| `video.py` | Dienste, Ablauf, Oberfläche (1329 Zeilen) |
| `bildeffekte/` | API-Paket für Plugins: Schnittstellen, Hilfsmittel, DI-Container (487 Zeilen) |
| `plugins/` | 11 Dateien mit allen 31 Effekten |
| `plugins/README.md` | Anleitung zum Schreiben eigener Plugins |

## Detaildateien

| Datei | Wofür |
| --- | --- |
| `bedienung.md` | Starten, Bereich wählen, Ton-Geräte, BlackHole, Tastenkürzel, Stolpersteine |
| `effekte.md` | Alle Bild- und Ton-Effekte mit Beschreibung und gemessener Rechenzeit |
| `architektur.md` | Container, Threads, Audio-Pipeline, Kopplung Bild ↔ Ton |
| `messwerte.md` | Wie gemessen wurde, alle Zahlen, was ungetestet ist |
| `../../plugins/README.md` | Eigene Effekte schreiben (API, Beispiele, Dienste anfordern) |

## Häufige Fragen

| Frage | Datei |
| --- | --- |
| „Nur schwarzes Bild" | `bedienung.md` |
| „Wie bekomme ich den Ton des Videos statt des Mikrofons?" | `bedienung.md` |
| „Welcher Effekt ist wie schnell?" | `effekte.md` |
| „Warum ruckelt es bei großem Bereich?" | `messwerte.md` |
| „Wie baue ich einen eigenen Effekt ein?" | `plugins/README.md` |
