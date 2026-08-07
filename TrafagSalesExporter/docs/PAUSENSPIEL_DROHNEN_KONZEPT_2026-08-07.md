# Konzept: Pausenspiel „Drohnen-Duell" im BiDashboard

Stand: 2026-08-07, Fassung 2 — **Konzept, nichts davon ist gebaut.**

Fassung 2 traegt zwei Entscheide von Ingo ein: **gegen PC oder gegen einen Kollegen am
selben Rechner (Hotseat), beide geben ihren Namen ein**, und **3D-Optik, gespielt auf
einer Ebene**.

## 1. Ziel und Abgrenzung

Ein rundenbasiertes Artilleriespiel nach Worms-Vorbild als Pausenreiter im Dashboard.
Statt Kanonen werden **Drohnen** gestartet, die die gegnerischen Wuermer ausschalten;
verschiedene Drohnentypen sind vor jedem Zug waehlbar. Der Reiter ist ausblendbar.

**Ausdruecklich nicht Teil davon:** Netzwerkspiel ueber zwei Rechner, hausweite
Ranglisten, Speichern in der Produktivdatenbank, Mobilbedienung.

Die Leitplanke, unter der alles andere steht: **der Server, auf dem das laeuft, ist
der Produktiv-Webserver des BiDashboards.** Das Spiel darf dessen Betrieb weder
verlangsamen noch dessen Daten beruehren. Jede Entwurfsentscheidung unten faellt in
diese Richtung.

## 2. Wo der Reiter haengt und wie er verschwindet

Ausblenden muss nichts Neues erfunden werden — es gibt drei Ebenen, die schon
existieren, und ich empfehle alle drei uebereinander:

1. **Konfigurationsschalter** `"Pause": { "Enabled": false }` in `appsettings.json`,
   nach dem Muster von `LandingPage:ShowWalkingLabFigure` und
   `Navigation:ShowFinanceComparison`. Steht er auf `false`, ist die Route gar nicht
   erst registriert — kein Reiter, kein Code, keine Seite.
2. **Menueintrag** in `NavigationMenuItems` mit `IsVisible = false` als Standard. Damit
   ueber die vorhandene Menuestruktur-Seite ein- und ausblendbar, ohne Deploy.
3. **`RequiredPolicy`** am Menueintrag, falls der Reiter nur fuer bestimmte Personen
   sichtbar sein soll.

Route: `/pause`. Kein Passwort-Gate wie bei Finance/HR.

Wichtig: `IsVisible = false` versteckt nur den *Eintrag*, nicht die *Route*. Wer die
URL kennt, kommt hin. Nur Ebene 1 macht die Seite wirklich unerreichbar.

## 3. Startbildschirm, Modus und Namen

Vor jeder Partie ein schlichter Vorbau — das ist zugleich die Stelle, an der die Namen
hereinkommen:

- **Modus:** `Gegen den Rechner` oder `Gegen einen Kollegen`.
- **Namen:** zwei Eingabefelder (bei Rechnergegner nur eines, der Gegner heisst je nach
  Schwierigkeitsgrad `Drohnenpilot`, `Schwarmfuehrer`, `Luftwacht`). Hoechstens 20
  Zeichen, Leereingabe faellt auf `Spieler 1` / `Spieler 2` zurueck.
- **Schwierigkeitsgrad** (nur gegen den Rechner): drei Stufen, siehe Abschnitt 7.
- **Mannschaftsgroesse:** 3 oder 4 Wuermer.

Die Namen wirken an vier Stellen: als **Schild ueber jedem Wurm** im Spielfeld
(`Ingo 1` … `Ingo 4`), in der **Zuganzeige** oben, im **Abspann** („Ingo gewinnt
3:1") und in der **Bestenliste** im Browser.

Zur Technik der Schilder: `wwwroot/js/finance3d.js` erzeugt Beschriftungen bereits
ueber `THREE.CanvasTexture` auf einem `THREE.Sprite` (Zeilen 456-458). Dasselbe Muster
traegt hier — Sprites drehen sich immer zur Kamera, also bleiben die Namen aus jedem
Kamerawinkel lesbar.

Die zuletzt benutzten Namen liegen im `localStorage` und sind beim naechsten Start
vorbelegt. **Sie verlassen den Browser nicht** — kein Serveraufruf, kein Eintrag in der
Datenbank, kein Protokoll. Wer wann gegen wen gespielt hat, ist damit auf dem Server
nirgends nachvollziehbar, und das ist Absicht.

## 4. Die eine Architekturentscheidung, die zaehlt

**Der Spielkern laeuft vollstaendig im Browser (three.js + JS-Modul), nicht in Blazor
Server.**

Das ist keine Geschmacksfrage. Blazor Server haelt pro offener Seite einen
SignalR-Circuit; jede Zustandsaenderung geht ueber das Netz und kostet Serverzeit.
Ein Spiel mit 60 Bildern pro Sekunde und Flugbahnen wuerde genau das tun — auf
demselben Prozess, der die Cockpit-Abfragen bedient. Bei zwei Leuten in der
Mittagspause faellt das nicht auf; das ist aber kein Argument, sondern Glueck.

Konkret:

- `Components/Pages/PauseGame.razor` enthaelt fast nichts: ein `<canvas>`, den
  Startbildschirm und den Start-Knopf.
- `wwwroot/js/pausegame.js` enthaelt Spielschleife, Physik, Gelaende, Szene und
  Zeichnen — nach dem Muster von `wwwroot/js/finance3d.js`.
- Ueber die Bruecke gehen nur zwei Dinge: beim Start Modus, Namen und Einstellungen
  hinein, am Ende das Ergebnis heraus. Keine Uebertragung pro Bild.
- Die Schleife haelt an, wenn der Reiter im Hintergrund ist
  (`document.visibilityState`), und wird beim Verlassen der Seite sauber abgebaut
  (`IAsyncDisposable` in der Razor, `dispose()` im Modul).

Preis dieser Entscheidung, ehrlich benannt: die Spiellogik liegt in JavaScript und
damit ausserhalb der C#-Testsuite. Sie ist nicht durch `dotnet test` abgedeckt.

**Hotseat ist bestaetigt, Netzwerkspiel ist damit ausgeschlossen.** Zwei Browser
gegeneinander hiesse Spielzustand auf dem Server, Lobby, Synchronisierung,
Verbindungsabbrueche und Betrugsschutz — und damit genau die Serverlast, die dieser
Abschnitt vermeidet. Falls es das spaeter geben soll, ist der saubere Weg ein eigener
kleiner Dienst, nicht der BiDashboard-Prozess. Die Spiellogik wird trotzdem so
geschnitten, dass sie einen Zug als Datensatz entgegennimmt statt direkt aus der
Eingabe zu lesen — das haelt die Tuer offen, ohne heute etwas zu kosten.

## 5. 3D: was geht, und warum genau so

**Die gute Nachricht zuerst:** `three.js` r160 liegt bereits lokal unter
`wwwroot/js/vendor/three.min.js` und wird in `Components/App.razor` Zeile 26 auf
**jeder** Seite geladen. Keine neue Abhaengigkeit, kein Zugriff nach draussen, kein
Zwischenspeicherproblem — die Bibliothek ist im Browser jedes Benutzers ohnehin schon
da, und `finance3d.js` beweist seit Monaten produktiv, dass WebGL auf den
Arbeitsplatzrechnern laeuft.

**Entschieden: volle 3D-Darstellung, gespielt wird auf einer Ebene.** Wuermer und
Drohnen bewegen sich in der X/Y-Ebene; die Szene, das Licht, die Schatten und die
Kamera sind echt dreidimensional.

Warum nicht weiter: eine echte 3D-Landschaft verlangt Zielen im Raum und eine frei
drehbare Kamera. Das ist nicht nur mehr Arbeit, es macht das Spiel in einer
Zehn-Minuten-Pause auch schlechter — der Reiz von Worms ist, dass man den Winkel
*sieht*. Voxel mit echten Tunneln waeren die technisch ehrliche 3D-Variante und
kosten ein bis zwei Wochen zusaetzlich.

Aufbau der Szene:

- **Gelaende:** die Wahrheit bleibt eine Pixelmaske auf einem verborgenen 2D-Canvas —
  daran haengen Kollision und Zerstoerung, und sie ist mit Abstand die einfachste
  belastbare Loesung. Dargestellt wird sie als Platte, deren Alphakanal aus eben dieser
  Maske kommt, mit einer zweiten, dunkleren Platte leicht dahinter. Dadurch bekommen
  Kraterraender sichtbare Tiefe. Die Maske wird nur bei einer Explosion neu zur Textur
  gemacht, nicht pro Bild.
  Ausbaustufe, falls es gut aussehen soll: Umriss der Maske ueber Marching Squares in
  eine `THREE.Shape` und daraus eine `ExtrudeGeometry` — dann hat das Gelaende echte
  Dicke und wirft Schatten auf sich selbst. Neuberechnung nur bei Einschlag.
- **Licht:** ein `DirectionalLight` mit Schattenkarte plus ein Hemisphaerenlicht als
  Aufhellung — `finance3d.js` Zeile 32 macht den ersten Teil bereits genauso.
- **Kamera:** perspektivisch. **Waehrend des Zielens steht sie senkrecht auf der
  Spielebene**, sonst laesst sich der Winkel nicht mehr abschaetzen und das Spiel wird
  frustrierend. Erst wenn die Drohne fliegt und niemand mehr eingibt, schwenkt sie
  hinter die Drohne und faehrt mit. Das ist der Punkt, an dem 3D-Spiele dieser Art
  ueblicherweise kaputtgehen.
- **Modelle:** es gibt kein Budget fuer einen 3D-Grafiker, also entsteht alles aus
  three.js-Grundkoerpern — Wuermer als Kapsel mit Augen, Drohnen aus Quader, Zylinder
  und vier rotierenden Rotorscheiben. Das sieht bewusst spielzeughaft aus, ist in
  Stunden gebaut und altert besser als schlechte gekaufte Modelle.
- **Leistungsschutz:** `renderer.setPixelRatio(Math.min(devicePixelRatio, 2))` wie in
  `finance3d.js`, begrenzte Schattenkartengroesse und ein Schalter „Effekte
  reduzieren" (Schatten aus, halbe Aufloesung) fuer aeltere Rechner.

## 6. Spielablauf

Rundenbasiert, zwei Mannschaften mit je 3–4 Wuermern, abwechselnd ein Zug.

1. **Bewegen** — 20 Sekunden, Laufen und Springen auf dem Gelaende.
2. **Drohne waehlen** — aus den freigeschalteten Typen (Abschnitt 8).
3. **Starten** — je nach Typ ueber Winkel und Schub oder ueber gesetzte Wegpunkte.
4. **Wirkung** — Explosion, Gelaendeschaden, Rueckstoss, Fallschaden.
5. **Wechsel** — nach 3 Sekunden Nachlauf ist die Gegenseite dran.

Rahmenbedingungen:

- **Wind** wechselt jede Runde, wirkt auf leichte Drohnen stark und auf schwere kaum,
  und ist als Pfeil eingeblendet. Wichtigste taktische Groesse.
- **Zerstoerbares Gelaende** ueber die Pixelmaske aus Abschnitt 5.
- **Wasser** unten: wer hineinfaellt, ist raus.
- **Sudden Death** nach 15 Runden: der Wasserspiegel steigt.

## 7. Der Rechnergegner

Drei Stufen, alle auf derselben Grundlage: Ziel auswaehlen, Ballistik rechnen, Ergebnis
mit einem Streufehler versehen.

- **Drohnenpilot** — grosser Streufehler, waehlt immer die Sprengdrohne, ignoriert den
  Wind zur Haelfte.
- **Schwarmfuehrer** — kleiner Streufehler, rechnet den Wind voll ein, nimmt die
  Abwurfdrohne gegen Gruppen.
- **Luftwacht** — trifft sehr genau, nutzt die Spaehdrohne zur Vorbereitung und zielt
  bevorzugt auf Wuermer nahe am Wasser.

Kein Lernen, keine Wegfindung. Das reicht fuer eine Pause und kostet einen Nachmittag
statt einer Woche. Ein wichtiges Detail: der Rechner **darf nicht sofort ziehen** —
eine kurze Bedenkzeit mit sichtbarem Zielen macht ihn spuerbar angenehmer als eine
sofortige perfekte Antwort.

## 8. Drohnentypen

Der Kern der Idee. Jeder Typ hat eine eigene Steuerung — das ist der Unterschied zu
Worms, wo fast alles derselbe Wurf mit anderer Wirkung ist.

| Drohne | Steuerung | Wirkung | Wind | Vorrat |
|---|---|---|---|---|
| **Sprengdrohne** | Winkel + Schub, wie ein Wurf | Standardexplosion, mittlerer Krater | stark | unbegrenzt |
| **Abwurfdrohne** | fliegt waagrecht auf gewaehlter Hoehe, Ausklinken per Klick | drei kleine Bomben in Reihe, breite flache Schaeden | mittel | 3 |
| **Schwarmdrohne** | Winkel + Schub, teilt sich am Scheitelpunkt | fuenf Mini-Drohnen, leichte Zielsuche, wenig Einzelschaden | sehr stark | 2 |
| **Bohrdrohne** | Winkel + Schub | graebt sich ins Gelaende und zuendet nach 2 s — gegen Eingegrabene | schwach | 2 |
| **Spaehdrohne** | frei steuerbar, 8 s Flugzeit | kein Schaden; deckt Gelaende auf und markiert ein Ziel: naechster Treffer +50 % | stark | 2 |
| **Bergedrohne** | Zielpunkt klicken | hebt den **eigenen** Wurm an eine andere Stelle, oder wirft ein Medipack ab | mittel | 1 |

Zwei Punkte, die den Entwurf tragen:

- **Nicht alle Drohnen schiessen.** Spaeh- und Bergedrohne machen keinen Schaden. Das
  ist der Grund, warum das Spiel nicht nach zwei Partien langweilig wird.
- **Die Steuerung unterscheidet sich wirklich.** Wurf, Ueberflug, freier Flug und
  Zielpunkt sind vier Bedienarten. Das ist der groesste Teil der Arbeit und zugleich
  der Grund, warum es sich nicht wie ein Worms-Abklatsch anfuehlt.

Fuer die erste Fassung reichen **Sprengdrohne, Abwurfdrohne und Spaehdrohne** — Wurf,
Ueberflug und freier Flug also je einmal.

## 9. Was gespeichert wird: nichts Serverseitiges

Namen, Bestenliste und Einstellungen liegen im **`localStorage` des Browsers**. Keine
neue Tabelle, keine Migration, kein Schreibzugriff auf `trafag_exporter.db`.

Das ist bewusst so: die Produktivdatenbank ist 339 MB, liegt im Publish-Verzeichnis und
ist das einzige unwiederbringliche Stueck des ganzen Systems. Ein Pausenspiel hat darin
nichts verloren — auch nicht mit einer harmlosen Punktetabelle. Der zweite Grund steht
in Abschnitt 3: es soll serverseitig nicht nachvollziehbar sein, wer wann gespielt hat.

## 10. Falle: der Lokalisierungstest

`UiTextServiceTests.Generated_Translations_Cover_Every_Literal_Ui_Key_And_Preserve_Placeholders`
liest **alle** `Components/**/*.razor` und sammelt daraus Uebersetzungsschluessel — aus
`T("de","en")`, aus `…De="…" …En="…"`-Attributpaaren **und aus zwei benachbarten
Zeichenkettenliteralen**. Jeder gefundene Schluessel braucht danach Eintraege in sechs
Sprachen (`es`, `it`, `hi`, `sq`, `tr`, `tlh`).

Ein Spiel hat viel Text: Drohnennamen, Beschreibungen, Meldungen, Endstaende,
Startbildschirm. In der Razor waeren das schnell 40 Schluessel — also **240
Uebersetzungen**, davon 40 auf Klingonisch, fuer ein Pausenspiel.

Deshalb: **der gesamte Spieltext gehoert in `wwwroot/js/pausegame.js` bzw. eine
JSON-Datei daneben**, nicht in die Razor. Der Scanner sieht nur
`Components/**/*.razor`. In der Razor bleiben die zwei, drei Beschriftungen, die
wirklich zum Dashboard gehoeren (Reitertitel, Start-Knopf). Zwei Sprachen im Spiel
selbst (de/en, aus der aktuellen UI-Sprache uebergeben) reichen.

Das ist kein Detail, sondern der Unterschied zwischen drei Tagen und fuenf.

## 11. Umfang in Stufen

**Stufe 1 — spielbar (Schaetzung 3–4 Tage konzentriert)**
Reiter mit Konfigurationsschalter und Menueintrag, Startbildschirm mit Modus und Namen,
three.js-Szene mit Licht und Kamerafahrt, zerstoerbares Gelaende als Maske mit
Alphaplatte, Wurfphysik mit Wind, Laufen und Springen, Zugwechsel mit Uhr, drei
Drohnentypen, Hotseat gegen Kollegen, Rechnergegner Stufe `Drohnenpilot`, Sieg und
Niederlage, Namensschilder ueber den Wuermern.

Gegenueber Fassung 1 (2–3 Tage) kommen Startbildschirm, Namen und der 3D-Aufbau dazu.

**Stufe 2 — rund (2 Tage)**
Die drei uebrigen Drohnentypen, alle drei Schwierigkeitsgrade, Sudden Death,
Bestenliste im `localStorage`, Gelaende mit `ExtrudeGeometry` statt Alphaplatte, Ton
(abschaltbar, standardmaessig aus — Bueroumgebung), Schalter „Effekte reduzieren".

**Stufe 3 — nur falls gewollt**
Gelaendevorlagen, Zuschauermodus, Partie fortsetzen, Turniermodus fuer mehrere Namen.

Stufe 1 ist der ehrliche Pruefstein: wenn das nach vier Tagen keinen Spass macht,
machen die restlichen Drohnen es auch nicht besser.

## 12. Risiken und bewusst offene Punkte

- **Der Aufwand steckt in der Bedienung, nicht in der Physik.** Ballistik und
  Gelaendemaske sind ein bekannter Nachmittag. Vier Steuerungsarten so zu bauen, dass
  sie sich gut anfuehlen, ist der Rest der Zeit.
- **Die Kamera ist die groesste Gefahr fuer das Spielgefuehl.** Siehe Abschnitt 5:
  waehrend des Zielens senkrecht, erst im Flug beweglich. Wird das vermischt, ist das
  Spiel unbedienbar, und das merkt man erst spaet.
- **JavaScript-Anteil ohne Testabdeckung.** Bewusst in Kauf genommen, aber es heisst:
  Fehler im Spielkern faellt kein `dotnet test` auf.
- **Optik im Haus.** Der Reiter steht standardmaessig auf unsichtbar. Ob ein Spiel auf
  dem Firmen-BI-Server erwuenscht ist, ist eine Frage an Ingo und gegebenenfalls seine
  Vorgesetzten, keine technische.
- **Nicht geprueft:** ob der Produktiv-IIS statische `.js`-Dateien aus `wwwroot`
  zwischenspeichert und wie sich das auf Aktualisierungen des Spielmoduls auswirkt.
  Fuer `finance3d.js` besteht dasselbe Thema und ist offenbar nie aufgefallen, aber
  gemessen habe ich es nicht.
- **Nicht abgeschaetzt:** wie sich Schattenkarten und ein grosses Gelaende-Canvas auf
  aelteren Arbeitsplatzrechnern verhalten. `finance3d.js` laeuft dort, ist aber eine
  ruhende Szene ohne 60 Bilder pro Sekunde — das ist kein Beleg.
