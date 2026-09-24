# Weltmodell – Learnings aus Fehlern

Stand: 2026-09-24

Hier steht, was bei Aufgaben in diesem Repository schiefging und wie es beim nächsten
Mal vermieden wird. **Vor jeder Aufgabe lesen, nach jedem Fehler ergänzen.**
Ein Eintrag besteht aus Fehler, Ursache und Regel. Neue Einträge kommen oben in ihren
Abschnitt. Veraltete Regeln werden gelöscht, nicht nur ergänzt.

## Arbeitsweise allgemein

- **Erst sichtbar prüfen, dann behaupten.** (2026-09-23) Eine Maskenänderung an der
  Sonnenbrille zeigte keine Wirkung, weil die weiche Kante sie wieder auffraß.
  Regel: Jede Bildänderung als Vorher/Nachher-Ausschnitt rendern und ansehen, bevor
  sie als erledigt gilt.
- **Bei Stiländerungen vorsichtig anfangen.** (2026-09-23) Der erste Eis-Entwurf war
  mit starker Umgebungsspiegelung und kräftigen Buckeln zu metallisch, die Person
  war nicht mehr erkennbar. Regel: Spiegelung, Glanz und Relief zuerst schwach
  ansetzen und hochregeln. Das Motiv muss erkennbar bleiben.
- **Ursache erst im Bild suchen, nicht im Code raten.** (2026-09-23) Der
  „gestanzte“ Matrix-Agent kam daher, dass die Masken-Ellipse beim Zielbild Ohren,
  Haare, Hintergrund und Kragen mitnahm. Sichtbar wurde das erst, als die
  Landmarks auf die Vorlage gezeichnet waren. Regel: Masken und Punkte als Overlay
  auf Quelle **und** Ziel zeichnen.

## Werkzeuge und Umgebung

- **Tests laufen mit `unittest`, nicht mit `pytest`.** (2026-09-23) `pytest` ist im
  venv nicht installiert. Aufruf:
  `cd Ai && ../venv/bin/python -m unittest discover -s tests -q`
- **Plugins nicht außerhalb von `Ai/plugins` ausführen.** (2026-09-23) Eine Kopie des
  Plugins im Scratchpad fand ihre Assets nicht, weil der Pfad über
  `Path(__file__).parents[1]` gebildet wird. Regel: Für einen Vorher-Vergleich
  zuerst das Vorher-Bild rendern und danach ändern, oder `git stash` bzw.
  `git show` verwenden.
- **pip nur über `python -m pip`.** (2026-09-16) Das gemeinsame venv
  `Desktop/git/venv` (Python 3.11) wurde verschoben. Die Startzeile von
  `venv/bin/pip` zeigt noch auf den alten Pfad und bricht mit `bad interpreter` ab.
  Regel: `../venv/bin/python -m pip install …`, nie `venv/bin/pip`.
- **Große Kopien zum Mac Mini nicht per SMB.** (2026-09-20/22) Der Mini
  (Tailscale 100.79.88.30) läuft über das DERP-Relay Frankfurt statt direkt:
  RTT 413–954 ms, SMB-Schreibrate 425 KB/s, `ls` blockierte über eine Minute.
  Regel: Zuerst `tailscale status` prüfen. Steht dort `relay`, liegt es an der
  Verbindung, nicht an SMB-Einstellungen. Große Schreibvorgänge wie Kamera-Importe
  lokal ablegen und danach mit `rsync -av --partial --progress -e ssh` oder
  Taildrop übertragen.
- **Testbilder:** (2026-09-23) `thispersondoesnotexist.com` liefert bei `curl` nur
  HTML. Brauchbar sind frontale Porträts aus den Beispielen von
  `ageitgey/face_recognition` (`examples/obama.jpg`) oder `dlib/examples/faces`.
  Nach dem Download immer mit `file` prüfen.

## Numerik und OpenCV

- **`np.gradient` stürzt bei sehr kleinen Arrays ab.** (2026-09-23) Winzige Scherben
  mit 1 px Kantenlänge lösten einen `ValueError` aus. Regel: Für Ableitungen auf
  beliebig kleinen Ausschnitten `cv2.Sobel(..., ksize=1)` verwenden.
- **Padding verschiebt die Affinmatrix.** (2026-09-23) Wird die Quelle um p px
  gepolstert, gilt `t' = t − M·(p, p)`, nicht `+`. Das Vorzeichen war zuerst
  falsch. Regel: Das Vorzeichen per Hand an einem Punkt nachrechnen.
- **Pro-Objekt-Numpy in Schleifen ist teuer.** (2026-09-23) Rund 160 Scherben mit je
  mehreren Blur- und Gradientenaufrufen kosteten 48 ms pro Bild. Regel: Alles, was
  nur von der Form abhängt, einmal in `_setup` vorberechnen und pro Bild nur einen
  Warp machen. Danach mit `cProfile` nachmessen.

## Git

- **Vor dem Commit `git status` des ganzen Repos prüfen.** (2026-09-23) Neue Plugins
  hingen an Dateien, die nie committet waren (`verwandlung.py`, `liquid_sim.py`
  u. a.). Ein Commit nur der geänderten Dateien wäre nicht lauffähig gewesen.
  Regel: Abhängigkeiten prüfen und bei fremdem, offenem Stand den Umfang mit dem
  Benutzer klären.
- **Push und `sudo` brauchen ein echtes Terminal des Benutzers.** (2026-09-16)
  `git push` nach `github.com/metacube2/Ai` scheitert mit `could not read Username`:
  Es gibt kein Token im Schlüsselbund und keinen SSH-Schlüssel, und `gh` ist nicht
  installiert. Regel: lokal committen und den Push dem Benutzer vorschlagen
  (Personal Access Token als Passwort). Das gilt ebenso für `sudo`, etwa `pmset`.
- **Globaler `git config user.name` ist „initial comm“.** (2026-09-22) Das ist
  vermutlich ein Versehen. Regel: Vor **jedem** Commit `git config user.name`
  prüfen, auch in bestehenden Repos. Am 2026-09-24 trugen alle 16 ungepushten
  Commits in `Ai` diesen Autor, weil die Regel nur für neue Repos galt. Sie wurden
  per Rebase auf `metacube <metacube@gmail.com>` umgeschrieben, und im Repo `Ai`
  ist `user.name = metacube` jetzt lokal gesetzt.
