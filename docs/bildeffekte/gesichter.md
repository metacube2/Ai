# Gesichter – Live-Gesichtseffekte

Stand: 2026-09-25

Vier Effekte in der Gruppe **Gesichter** der Effektauswahl, alle in
`plugins/52_video_face_fx.py`. Kamera als Quelle wählen und das Gesicht frontal zeigen.
Ohne erkanntes Gesicht bleibt das Bild unverändert; jede Schleife beginnt, sobald ein
Gesicht ohne Unterbrechung verfolgt wird.

| Effekt | Ablauf |
| --- | --- |
| **Zeitversetztes Ich** | Ab 4 s nach dem ersten Erkennen trägt der Kopf dauerhaft das eigene Gesicht von vor 4 Sekunden. Blinzeln, Lachen und Sprechen kommen verspätet. Die Helligkeit wird ans aktuelle Licht angeglichen. |
| **Alterungs-Zeitraffer** | 14-s-Schleife: 1 s normal, in 5 s alt (Stirn-, Zornes-, Nasolabial- und Marionettenfalten, Krähenfüße, Tränensäcke, Altersflecken, fahle Haut, absackende Wangen, graue Haare an Oberkopf und Schläfen), zwei Röntgenblitze mit Schädel, danach in 3,5 s rückwärts zum jungen Gesicht (glatte Haut, größere Augen, kleinere Nase, rosige Wangen) und zurück. |
| **Gesichts-Hologramm** | 12-s-Schleife: Eine Scanlinie läuft von der Stirn zum Kinn und macht das Gesicht zum blauen Hologramm mit Drahtgitter, Zeilenstruktur und Flackern. Bei 5 s fliegen die Dreiecke auseinander, bei 7 s setzen sie sich wieder zusammen. Die Mimik bleibt live, weil das Netz an den Gesichtspunkten hängt. |
| **Kubistisches Gesicht** | Alle 3 s eine neue Anordnung mit 0,8 s Übergang: Das Gesicht wird in zwei bis vier farbige Flächen geteilt, Augen, Nase und Mund werden ausgeschnitten, versetzt, gedreht, vergrößert oder gespiegelt und dunkel umrandet. Wo ein Teil wegwandert, bleibt verwischte Haut. Nach fünf Anordnungen beginnt es beim echten Gesicht. |

## Umsetzung

- `bildeffekte/face_track.py`: `FaceTracker` bündelt das Verfahren des Matrix-Agenten
  (asynchrone Apple-Vision-Erkennung alle 0,09 s, optischer Fluss dazwischen,
  Korrektur nur zu 30 %). Dazu `FaceGeometry` (Augenachse, Gesichtsbreite,
  Gesichtskoordinaten u/v), `face_mask` (weiche innere Gesichtsfläche) und
  `template_triangles` (feste Dreiecke, weil die Punktreihenfolge immer gleich ist).
  Der Matrix-Agent selbst nutzt noch seine eigene Kopie.
- `_FaceLoop` in der Plugin-Datei: Arbeitsbild bis 512 px, Ausschnitt um das Gesicht,
  Rückgabe von Bild und Alpha. Eingesetzt wird nur der Ausschnitt, in voller Auflösung
  über das Originalbild geblendet; außerhalb der Maske bleibt das Kamerabild scharf.
- Zeitversetztes Ich: Ringpuffer der Gesichtsausschnitte der letzten 5 s, Dreiecks-Warp
  vom alten auf das aktuelle Mesh.
- Alterung: Falten als Linien in Gesichtskoordinaten, die je nach Alter nacheinander
  erscheinen, dunkel mit heller Kante darunter. Verformung per Verschiebungsfeld.
  Der Röntgenschädel wird prozedural in halber Auflösung gezeichnet.
- Hologramm: 172 Netzpunkte und 306 Dreiecke, baryzentrisch an die 28 Gesichtspunkte gebunden.
- Kubismus: Flächen über Schnittlinien durch die Gesichtsmitte, Teile per `warpAffine`.

## Messwerte

Reine Renderzeit ohne Erkennung und ohne optischen Fluss, Arbeitsbild 512×288 aus
1280×720, Gesicht 150 px breit, Median über 35 Läufe (2026-09-25, MacBook Air):

| Effekt | Median | p95 |
| --- | ---: | ---: |
| Zeitversetztes Ich | 5,5 ms | 5,9 ms |
| Alterung alt / Röntgenblitz / jung | 21,7 / 27,8 / 11,0 ms | 22,4 / 28,5 / 12,0 ms |
| Hologramm / Explosion | 17,5 / 19,2 ms | 19,0 / 20,7 ms |
| Kubistisches Gesicht | 29,5 ms | 30,9 ms |

Die Zeit wächst mit der Gesichtsgröße. Kubismus liegt nahe am 33-ms-Budget; in einer
Kette mit weiteren Effekten sinkt die Bildrate.

## Prüfung und Grenzen

- Sichtgeprüft als Bildfolgen an zwei Porträtfotos mit simulierter Kopfbewegung
  (Verschiebung und Drehung). Für das Zeit-Ich wurde nach 5 s die Person gewechselt:
  Das alte Gesicht saß 4 s lang passend auf dem neuen Kopf.
- `tests/test_face_fx.py`: kein Gesicht → Bild unverändert, Hintergrund unverändert,
  angeschnittene Gesichter, Zeitversatz, Zeitleisten, Netz folgt den Punkten, Tracker.
- **Live-Webcam ist ungeprüft.** Starke Kopfdrehung, Hände vor dem Gesicht und schnelle
  Bewegungen können Fehler zeigen. Eine Sonnenbrille bleibt bei Alterung und Röntgenschädel
  sichtbar.
- Das Zeit-Ich braucht Bewegung oder Mimik, damit man den Versatz sieht; bei ruhigem
  Kopf ist das alte Gesicht fast gleich.

## Eigenen Gesichtseffekt bauen

Von `_FaceLoop` erben (in derselben Plugin-Datei, Klassen mit `_` werden nicht geladen),
`name`, `CYCLE` und `MARGIN` setzen und `render(crop, points, t)` schreiben:
`points` sind die 28 Punkte im Ausschnitt, `t` die Zeit in der Schleife. Rückgabe
`(bild, alpha)` oder `None` für unverändert.
