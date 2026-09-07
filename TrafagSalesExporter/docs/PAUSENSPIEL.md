# Pausenspiel „FPV-Fernpilot"

Stand: 2026-08-17. Zusammengefuehrt aus `PAUSENSPIEL_DROHNEN_KONZEPT_2026-08-07.md`
(Fassung 3) und `PAUSENSPIEL_STUFE1_2026-08-07.md`.

Nebenfeature ohne fachlichen Bezug zum Dashboard. **Der Reiter ist ausgeblendet**, solange
`Pause:Enabled=false` gesetzt ist; er muss im Admin-Bereich bewusst eingeschaltet werden.

## Konzept

Der Wurm bleibt sichtbar an einem Steuerplatz und bedient eine Fernsteuerung. Der Spieler
uebernimmt die Drohne und fliegt sie aus einer nahen FPV-/Bordkameraperspektive durch vier
Kontrollringe bis zu einer Zielstation.

Das Szenario ist bewusst fiktional: keine realen Orte, Einheiten oder Personen, keine
Anleitung fuer einen realen Einsatz, das Ziel ist eine deutlich markierte unbemannte
Trainingsstation.

Damit ist es kein Artilleriespiel mehr: der Wurm schiesst nicht, die Drohne ist kein
ballistisches Projektil sondern wird durchgehend gesteuert, und es gibt keine Waffenwahl,
keine gegnerischen Wuermer und kein zerstoerbares Gelaende. Entscheidend sind Traegheit,
Wind, Funkverbindung, Akku und Fluglinie.

Eine Partie besteht aus zwei Fluegen auf derselben Strecke. Im Hotseat-Modus fliegen zwei
Personen nacheinander, im Rechnermodus zeigt das Spiel einen sichtbaren Referenzpiloten —
dieser nutzt **dieselbe Physik und dieselben Leistungsgrenzen** wie ein Mensch und wird
nicht als erfundene Zeit in die Wertung geschrieben.

## Gebaut

- Direkte Steuerung per Pfeiltasten oder `W/A/S/D`, Zusatzschub per `Umschalt` mit
  hoeherem Akkuverbrauch
- Traegheit, Luftwiderstand, Hoechstgeschwindigkeit, zweidimensionale Boeen
- Kollisionen mit Gelaende, Kartenrand und vier Bauwerkstypen
- Akku- und Funkmodell samt Abschattung und Verbindungsabbruch
- Nah folgende FPV-Kamera mit HUD und Warnanzeigen
- Wurmfigur mit Sender, sichtbare Drohne, Kontrollringe und Zielstation in three.js

## Dateien

| Datei | Rolle |
| --- | --- |
| `wwwroot/js/pausegame.js` | Spielkern |
| `Tools/PauseGame.Probe/probe.mjs` | Probes |

## Stand

Produktiv deployed am 2026-08-11 um 11:23. `461/461` Release-Tests, `28/28` FPV-Probes und
`18/18` MOD-Probes gruen. `wwwroot/js/pausegame.js` lokal und produktiv byteidentisch
(42'645 Bytes, SHA256 `DE09879793AD70B6AF9DDEA3CFDBB9F4AD4D853A1BEE37EA7E234B1D3129BB28`).
`/pause` und `/js/pausegame.js` liefern HTTPS `200`.

**Offen:** ein manueller Browsertest des Spielgefuehls.
