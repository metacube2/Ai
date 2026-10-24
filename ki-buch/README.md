# Die Schweiz denkt mit (E-Book)

Zukunftsbuch über künstliche Intelligenz in der Schweiz, 2027–2045, auf Wunsch des Benutzers vom 2026-10-24:
„schreib ein Buch über verschiedene Zukunftserrungenschaften der KI in der Schweiz, 100 Seiten, mit SVG-Bildern,
spannend geschrieben, Download als E-Book“.

## Aufbau

- Rahmen: Die Reporterin Lea Caflisch reist 2045 durch die Schweiz und sucht „die lauten Nächte“, in denen Menschen
  zum ersten Mal einer Maschine vertrauen mussten. Jedes Kapitel ist eine eigene Geschichte in einem anderen Jahr.
- Jedes Kapitel endet mit einem Kasten „Stand heute“: was es beim Schreiben (Herbst 2026) schon gibt. Nur Fakten,
  die sicher belegt sind; Zukunftsteil klar als Fiktion gekennzeichnet (Titelei).
- `kapitel/NN-name.txt`: Text mit einfacher Auszeichnung (siehe Kopf von `werkzeuge/bauen.py`).
- `bilder/NN-name.svg`: Kapitelbild, wird inline ins XHTML eingebettet (`<title>` = Bildunterschrift);
  `bilder/umschlag.svg` ist der Umschlag, daraus entsteht `ausgabe/umschlag.png` (Chrome ohne Fenster).
- `werkzeuge/bauen.py` → `ausgabe/Die-Schweiz-denkt-mit.epub` (EPUB 3), geprüft mit `epubcheck` (Homebrew).

```zsh
python3 werkzeuge/bauen.py && epubcheck ausgabe/Die-Schweiz-denkt-mit.epub
```

## Stand

Siehe Ende dieser Datei (Wortzahl, Prüfergebnis), wird nach jedem Bau nachgetragen.

Stand 2026-10-24: 18 Teile (Prolog, 16 Kapitel, Epilog), 26 187 Wörter (rund 93 Textseiten, mit 18 Bildern, Umschlag,
Titelei und Inhalt gut 100 Seiten), 19 SVG-Bilder aus `werkzeuge/bilder.py`. `epubcheck` 5.4.0: 0 Fehler, 0 Warnungen.
Kopie für den Benutzer: `~/Desktop/Die-Schweiz-denkt-mit.epub`. Konzept und wörtlicher Auftrag: [KONZEPT.md](KONZEPT.md).
Bildschirmanzeigen der Maschinen (Zeilen `*…*`) stehen je als eigener Absatz in Schreibmaschinenschrift.
