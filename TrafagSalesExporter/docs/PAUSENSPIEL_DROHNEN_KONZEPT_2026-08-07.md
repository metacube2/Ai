# Konzept: Pausenspiel "FPV-Fernpilot" im BiDashboard

Stand: 2026-08-11, Fassung 3. Diese Fassung ersetzt das fruehere Artilleriekonzept.

## 1. Kernidee

Der Wurm ist kein Soldat auf einem Worms-Spielfeld. Er bleibt an einem kleinen
Steuerplatz und bedient dort eine Fernsteuerung. Der Spieler uebernimmt nach dem
Start direkt die Drohne und fliegt sie aus einer nahen FPV-/Bordkameraperspektive
durch Hindernisse und Kontrollpunkte bis zu einer Zielstation.

Die Anmutung greift zeitgenoessische Fernsteuerung und FPV-Flug auf. Das Szenario
bleibt jedoch bewusst fiktional: keine realen Orte, Einheiten oder Personen und
keine Anleitung fuer einen realen Einsatz. Das Ziel ist eine deutlich markierte,
unbemannte Trainingsstation.

Damit ist das Spiel kein "Worms mit anderen Geschossen" mehr:

- Der Wurm laeuft und schiesst nicht, sondern bleibt sichtbar am Sender.
- Die Drohne ist kein ballistisches Projektil, sondern wird waehrend des gesamten
  Flugs direkt gesteuert.
- Entscheidend sind Trägheit, Wind, Funkverbindung, Akku, Hindernisse und Fluglinie.
- Es gibt keine Waffenwahl, keine gegnerischen Wuermer und kein zerstoerbares
  Artilleriegelaende.

## 2. Spielablauf

Eine Partie besteht aus zwei Fluegen auf derselben Strecke.

1. Pilot und Vergleichsmodus waehlen.
2. Der Wurm steht am Sender; die Drohne startet neben ihm.
3. Der Pilot fliegt vier Kontrollringe in der vorgegebenen Reihenfolge an.
4. Danach wird die gelb markierte Zielstation angeflogen.
5. Der zweite Pilot fliegt dieselbe Strecke.
6. Punkte aus Zielerreichung, Flugzeit und Restakku entscheiden den Vergleich.

Im Hotseat-Modus fliegen zwei Kollegen nacheinander. Im Rechnermodus zeigt das
Spiel als zweiten Lauf einen sichtbaren Referenzpiloten. Dieser benutzt dieselbe
Physik und dieselben Leistungsgrenzen wie ein Mensch; er wird nicht nur als
erfundene Zeit in die Wertung geschrieben.

## 3. Steuerung und Flugmodell

- Pfeiltasten oder `W/A/S/D`: Schub in die jeweilige Richtung.
- `Umschalt`: zusaetzlicher Schub, aber hoeherer Akkuverbrauch.
- `R`: den laufenden menschlichen Flug neu starten.
- Die Drohne besitzt Trägheit, Luftwiderstand und eine begrenzte
  Hoechstgeschwindigkeit.
- Boeen wirken horizontal und vertikal; ihre Staerke haengt von der gewaehlten
  Strecke ab.
- Eine Beruehrung mit Boden, Bauwerk oder Kartenrand beendet den Flug.

Die drei Streckenstufen veraendern Wind, Akkureserve und Funkdaempfung:

| Stufe | Schwerpunkt |
|---|---|
| Ruhiger Wind | groesste Akkureserve und geringe Boeen |
| Boeeiges Tal | staerkere Boeen und merkliche Funkdaempfung |
| Schwaches Signal | staerkste Boeen, kleinster Akku und hoechste Funkdaempfung |

## 4. Bordkamera und Darstellung

Die Welt wird mit dem bereits lokal vorhandenen three.js in 3D dargestellt, die
Fluglogik bleibt auf einer gut lesbaren X/Y-Ebene. Die Kamera faehrt nah mit der
Drohne mit und blickt etwas voraus, damit Hindernisse rechtzeitig sichtbar sind.

Ein FPV-HUD zeigt Pilot, Flugzeit, Akku, Signal, Kontrollpunkt und Warnungen. Der
Wurm mit Fernsteuerung ist am Start als 3D-Figur sichtbar und bleibt dort, waehrend
die Kamera der Drohne folgt. Kontrollpunkte leuchten blau, die Zielstation gelb.

## 5. Akku, Funk und Wertung

Der Akku sinkt kontinuierlich. Richtungssteuerung kostet mehr als Schweben, der
Zusatzschub am meisten. Das Funksignal nimmt mit Entfernung, Hoehe und Abschattung
hinter Bauwerken ab; absolvierte Kontrollpunkte wirken als kleine Relais. Bleibt
das Signal laenger als 2,4 Sekunden vollstaendig weg, endet der Flug.

Ein erfolgreicher Flug erhaelt einen deutlichen Zielbonus. Restakku verbessert die
Wertung, lange Flugzeit verringert sie. Bei einem abgebrochenen Flug zaehlen die
bereits erreichten Kontrollpunkte und die zurueckgelegte Strecke, sodass auch zwei
Fehlversuche sinnvoll vergleichbar bleiben.

## 6. Architektur und Datenschutz

Der Spielkern liegt in `wwwroot/js/pausegame.js` und laeuft vollstaendig im Browser.
Blazor uebergibt beim Laden nur das Host-Element und die Sprache. Pro Bild gibt es
keinen SignalR-Aufruf, keinen Serverzustand und keinen Datenbankzugriff.

Namen, Audioeinstellungen und lokale Siege liegen ausschliesslich im
`localStorage`. Beim Verlassen der Seite werden Animationsschleife, WebGL-Ressourcen
und AudioContext abgebaut. Wenn der Browserreiter unsichtbar ist, pausiert die
Simulation.

Der bestehende Schalter `Pause:Enabled` und die Sichtbarkeit des Menueintrags
`pause-game` bleiben unveraendert. Standard im Repository ist AUS.

## 7. Ton und Lokalisierung

Geraeusche sind standardmaessig ausgeschaltet und werden synthetisch erzeugt. Eine
eigene `.mod`-Datei kann weiterhin lokal gewaehlt werden; sie wird nicht hochgeladen.
`wwwroot/js/modplayer.js` bleibt dafuer unveraendert bestehen.

Alle Spieltexte stehen in Deutsch und Englisch im JavaScript-Modul. In der Razor
bleiben nur die beiden Dashboard-Texte, damit der globale Lokalisierungstest nicht
fuer jeden Spieltext sechs zusaetzliche Uebersetzungen verlangt.

## 8. Nachweisgrenze

`Tools/PauseGame.Probe/probe.mjs` prueft ohne Browser Streckenerzeugung, Kollision,
Steuerung, Geschwindigkeitsgrenze, Akku, Signal, Kontrollpunkte, Ziel, Wertung und
den Referenzpiloten ueber viele Zufallsstrecken. Startbildschirm, Vorflugansicht und
laufende FPV-Szene wurden zusaetzlich am 2026-08-11 in Edge mit Software-WebGL
gerendert. Dabei wurde die Kamera so korrigiert, dass Drohne, naechstes Hindernis
und aktueller Kontrollring gleichzeitig sichtbar sind. Das subjektive
Tastaturgefuehl muss weiterhin durch einen Menschen beurteilt werden.
