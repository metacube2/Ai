# Gym Cockpit

Trainingslogbuch für das Studio, als Web-App unter <https://gym.ingok.ch>. Sie
ist für das Handy gebaut (iPhone und Android) und lässt sich als App auf den
Homebildschirm legen.

## Funktionen

- **Profil:** Name (Pflicht) und Körpergewicht.
- **Geräte:** Name, Art (Beinpresse, Latzug, …), eingestelltes Gewicht,
  Ziel-Sätze und Einstellung am Gerät (Sitz, Polster).
- **Training:** „Training starten“ misst die Zeit im Gym. „Start“ an einem
  Gerät startet das Training bei Bedarf selbst und hält die Uhrzeit fest.
- **Cockpit:** grosser Balken, der hin- (2,5 s) und zurückfährt (2,5 s), dazu
  Ton und Vibration beim Richtungswechsel. Die Wiederholungen zählt es mit.
  Das Tempo und Haltezeiten oben und unten stellst du im Profil ein.
  „Satz fertig“ öffnet die Eingabe für Gewicht und Wiederholungen, mit dem
  Zählerstand vorbelegt. Ist die Zahl der Ziel-Sätze erreicht, schliesst das
  Cockpit.
- **Verlauf:** Trainingstage mit Dauer, Körpergewicht und allen Sätzen sowie
  der Fortschritt pro Gerät (Höchstgewicht je Tag).

## Aufbau

| Teil | Datei |
|---|---|
| Oberfläche | `public/index.html`, `style.css`, `app.js` (Vanilla JS, kein Build) |
| Offline | `public/sw.js`: Netz zuerst, Cache als Rückfall; die API wird nie gecacht |
| Server | `public/api.php`: ein JSON-Dokument pro Profil in SQLite `/var/lib/gym/gym.db`, 30 Revisionen |
| vHost | `deploy/gym.conf` (Apache auf cAiscom) |
| Proxy | `deploy/npm_host_42.sh` (NPM-Host 42, Zertifikat 88) |

Die Daten liegen zuerst im Browser (`localStorage`) und gehen danach an den
Server. Ohne Netz im Keller geht also nichts verloren. Ändern zwei Geräte
gleichzeitig, meldet der Server 409. Der Client führt dann die Stände nach ID
und `mod`-Zeit zusammen. Gelöschtes bleibt als `deleted` stehen, damit
das Zusammenführen es nicht wieder herstellt.

Es gibt kein Login. Ein Profil ist ein zufälliges 128-Bit-Token. Der Link
`https://gym.ingok.ch/#k=<token>` (Profil → „Link kopieren“) öffnet dasselbe
Logbuch auf einem anderen Gerät. Neue Profile: höchstens 20 pro IP und Stunde.

## Einspielen

```zsh
~/Projects/Git/Ai/gym/deploy/deploy.sh
```

Das Skript sichert einen vorhandenen Stand nach `/root/gym-backup-<zeit>` und
spielt die Dateien einzeln ein (kein `tar .`). Ändern sich CSS oder JS, in
`index.html` und `sw.js` die Version `?v=` bzw. `CACHE` hochzählen.

## Speicherung und Auswertung

Jeder Satz, jedes Gerät und jeder Trainingsstart wird sofort gespeichert, erst
im Browser und eine gute Sekunde später auf dem Server. Der Punkt neben dem
Namen zeigt den Stand: grün heisst auf dem Server, orange heisst offline, der
Abgleich folgt dann später. Beim nächsten Training zeigt jedes Gerät die Sätze
vom letzten Mal, und das Cockpit füllt das zuletzt benutzte Gewicht vor. Unter
„Verlauf“ stehen alle Tage und pro Gerät eine Kurve des Höchstgewichts.

## Offene Punkte

- Ein vergessenes „Training beenden“ lässt die Gym-Uhr weiterlaufen. Ein
  automatisches Ende nach einer Pause ohne Satz ist vorgeschlagen, aber noch
  nicht entschieden.
- Getestet ist alles nur im Chrome mit Handygrösse, nicht auf einem echten
  iPhone oder Android. Offen sind dort Ton, Bildschirmsperre und die
  Installation auf dem Homebildschirm.
- Hairpinning über die WAN-IP geht nicht (siehe
  `instructions/homelab/reverse-proxy-dns.md`). Im Heimnetz läuft der Zugriff
  über Split-DNS.
