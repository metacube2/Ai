# Bildeffekte — Unterrouter

Stand: 2026-09-22

Echtzeit-Effekte auf einen frei wählbaren Bildschirmbereich oder eine Live-Kamera, dazu Ton-Effekte, die
vom Bild gesteuert werden. Oberfläche in PySide6, alle Effekte sind Plugins.

**Start:** `Bildeffekte.command` auf dem Schreibtisch, oder
`venv/bin/python video.py` im Ordner `Ai`.

## Dateien

| Pfad | Inhalt |
| --- | --- |
| `video.py` | Dienste, Ablauf, Oberfläche |
| `bildeffekte/` | API-Paket für Plugins: Schnittstellen, Hilfsmittel, DI-Container; Kern der Verwandlungen |
| `plugins/` | 19 Dateien mit 57 Plugin-Effekten; dazu „Original“ als feste Bildstrategie: insgesamt 39 Bild- und 19 Ton-Effekte, darunter vier im Kontur-Labor und elf Verwandlungen |
| `plugins/README.md` | Anleitung zum Schreiben eigener Plugins |

## Detaildateien

| Datei | Wofür |
| --- | --- |
| `bedienung.md` | Starten, Quelle und Bereich wählen, Vollbild, Morph-Zielbild, Ton-Geräte und Tastenkürzel |
| `effekte.md` | Alle Bild- und Ton-Effekte mit Beschreibung und gemessener Rechenzeit |
| `architektur.md` | Container, Threads, Audio-Pipeline, Kopplung Bild ↔ Ton |
| `messwerte.md` | Wie gemessen wurde, alle Zahlen, was ungetestet ist |
| `matrix-agent.md` | Matrix-Gesichtsmorph nach `morph.mp4`: eigene JPG-Zielbilder, Standard-Agent, Verfahren und Grenzen |
| `verwandlungen.md` | Flüssigmetall II und neun Verwandlungen: Bedienung, Installation, Kern, Verfahren, Messwerte |
| `../../plugins/README.md` | Eigene Effekte schreiben (API, Beispiele, Dienste anfordern) |

## Häufige Fragen

| Frage | Datei |
| --- | --- |
| „Nur schwarzes Bild" | `bedienung.md` |
| „Wie bekomme ich den Ton des Videos statt des Mikrofons?" | `bedienung.md` |
| „Welcher Effekt ist wie schnell?" | `effekte.md` |
| „Warum ruckelt es bei großem Bereich?" | `messwerte.md` |
| „Wie baue ich einen eigenen Effekt ein?" | `plugins/README.md` |
| „Wie verwandle ich mein Kameragesicht in den Agenten oder ein eigenes JPG?" | `matrix-agent.md` |
| „Verwandlungen: Hintergrund unsauber, Modell fehlt, eigene Verwandlung bauen?" | `verwandlungen.md` |
