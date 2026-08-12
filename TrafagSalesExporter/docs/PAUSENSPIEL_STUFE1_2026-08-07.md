# Pausenspiel "FPV-Fernpilot" gebaut

Stand: 2026-08-11. Konzept:
`docs/PAUSENSPIEL_DROHNEN_KONZEPT_2026-08-07.md`.

**Produktiv deployed am 11.08.2026 um 11:23 Uhr.** Der neue FPV-Code ist auf dem Server;
der Menüschalter bleibt wie zuvor auf `Pause:Enabled=false`, deshalb ist der Reiter weiterhin
ausgeblendet, bis er im Admin-Bereich bewusst eingeschaltet wird.

Deploynachweis: `461/461` Release-Tests, `28/28` FPV-Probes und `18/18` MOD-Probes grün.
`wwwroot/js/pausegame.js` ist lokal und produktiv byteidentisch (42.645 Bytes, SHA256
`DE09879793AD70B6AF9DDEA3CFDBB9F4AD4D853A1BEE37EA7E234B1D3129BB28`). `/pause` und
`/js/pausegame.js` liefern HTTPS 200. Ein manueller Browsertest des Spielgefühls bleibt offen.

## Ergebnis

Der fruehere Artilleriekern wurde ersetzt. Der Wurm bleibt nun sichtbar an einer
Fernsteuerung, waehrend der Spieler die Drohne direkt aus einer mitfahrenden
Bordkameraperspektive durch vier Kontrollpunkte bis zu einer fiktionalen
Zielstation fliegt.

Gebaut sind:

- direkte Steuerung per Pfeiltasten oder `W/A/S/D`;
- Zusatzschub per `Umschalt` mit hoeherem Akkuverbrauch;
- Trägheit, Luftwiderstand, Hoechstgeschwindigkeit und zweidimensionale Boeen;
- Kollisionen mit Gelaende, Kartenrand und vier Bauwerkstypen;
- Akku- und Funkmodell samt Abschattung und Verbindungsabbruch;
- nah folgende FPV-Kamera mit HUD und Warnanzeigen;
- Wurmfigur mit Sender, sichtbare Drohne, Kontrollringe und Zielstation in three.js;
- Hotseat fuer zwei Namen auf derselben Strecke;
- ein sichtbarer Referenzpilot mit derselben Physik wie der menschliche Pilot;
- Wertung aus Zielerreichung, Kontrollpunkten, Strecke, Flugzeit und Restakku;
- lokale Siegerstatistik ohne Server- oder Datenbankzugriff;
- deutsche und englische Spieltexte;
- bestehender synthetischer Ton und lokaler MOD-Abspieler, standardmaessig aus.

Entfernt wurden Mannschaften, laufende und springende Wuermer, Waffenwahl,
Spreng-/Abwurf-/Spaehdrohnen, Ballistik, Explosionen, Gesundheit, zerstoerbares
Gelaende, Wasser und Schuss-KI.

## Dateien

| Datei | Stand |
|---|---|
| `wwwroot/js/pausegame.js` | neuer FPV-Spielkern, Szene, Oberflaeche und Rechenfunktionen |
| `Tools/PauseGame.Probe/probe.mjs` | Regressionstests fuer Flugmodell und Referenzpilot |
| `wwwroot/js/modplayer.js` | unveraendert, lokaler ProTracker-Abspieler |
| `Components/Pages/PauseGame.razor` | unveraendert, Browser-Modul und Lebenszyklus |
| `Services/PauseGameOptions.cs` | unveraendert, Pausenreiter-Schalter |

## Betrieb und Ausblenden

`Pause:Enabled` steht im Repository weiterhin auf `false`. Das Spiel wird damit
standardmaessig weder im Menue angezeigt noch unter `/pause` geladen. Einschalten
erfolgt wie bisher unter Admin > Settings. Namen, Ergebnisse und Audioeinstellungen
bleiben im Browser; der Produktivserver erhaelt keinen Spielzustand.

## Automatischer Nachweis

Der neue Probe-Test deckt ab:

- 40 erzeugte Strecken mit gueltigem Boden und freien Zielpunkten;
- Kreis-/Rechteck-, Boden- und Bauwerkskollisionen;
- Bewegung in beiden Achsen und Begrenzung der Geschwindigkeit;
- normalen und erhoehten Akkuverbrauch;
- Signalabnahme mit Entfernung;
- Reihenfolge der Kontrollpunkte und Zielerkennung;
- 60 Referenzfluege ueber alle drei Streckenstufen;
- Wertungsregeln fuer Erfolg, Teilfortschritt, Zeit und Restakku.

Aufruf:

```text
node Tools/PauseGame.Probe/probe.mjs
node Tools/PauseGame.Probe/modprobe.mjs
dotnet test
```

## Browsernachweis und noch offene manuelle Pruefung

Am 2026-08-11 wurden Startbildschirm, Vorflugansicht mit Wurm und Fernsteuerung sowie
die laufende FPV-Szene in Edge Headless mit Software-WebGL gerendert und visuell
geprueft. Die erste Kamerafassung schnitt den naechsten Kontrollring am Bildrand ab;
die Kamera richtet sich deshalb nun dynamisch zwischen Drohne und aktuellem
Wegpunkt aus. Drohne, Hindernis und kompletter Ring sind dadurch gleichzeitig
sichtbar.

Noch offen ist das subjektive Spielgefuehl mit echter Tastatureingabe: Reaktion,
Schwierigkeitsabstufung und Warnanzeigen muessen einmal von einem Menschen gespielt
werden. Der Browsernachweis belegt Darstellung und Modullauf, nicht ob die Steuerung
bereits Spass macht.
