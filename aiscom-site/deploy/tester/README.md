# Tester-Seite (www.aiscom.ch/tester/)

Download-Seite für Tester (nicht verlinkt, `noindex`): Aurora Studio, Kamera-Import, Antennen-Studio, MacYaesu und
Stretcher als DMG mit Verknüpfung „Programme“ zum Hineinziehen, Icon, Version/Build/Datum/Grösse/SHA-256 je App und die
Installationsschritte. Sonst nichts: keine Testpunkte, Grenzen oder Hinweise (Benutzer 2026-10-24). Alle Apps sind
**Vollversionen**: Kamera-Import, Antennen-Studio und MacYaesu als Tester-Fassung ohne Lizenzschlüssel
(`verteilen.sh --tester`, Compile-Schalter `KAMERA_TESTER`, `ANTENNEN_TESTER`, `MACYAESU_TESTER`), Aurora Studio und
Stretcher haben keine Demo-Grenze. Kein Schlüssel steht auf der Seite.

Neue Fassung veröffentlichen:

```zsh
# 1. DMGs notarisiert bauen (Benutzer-OK seit 2026-10-24 fuer die Tester-Seite). Vorher pruefen, dass der Bildschirm
#    entsperrt ist (ioreg … IOConsoleLocked), sonst findet notarytool den Zugang im Schluesselbund nicht.
#    --ohne-notar nur zum Pruefen (erster Start dann per Rechtsklick › Öffnen), nicht weitergeben.
(cd ~/Projects/Git/aurora-studio-mac && werkzeuge/verteilen.sh)                 # build/verteilen/Aurora Studio.dmg
(cd ~/Projects/Git/kamera-import && ./verteilen.sh --tester)                    # dist/KameraImport-Tester.dmg
(cd ~/Projects/Git/antennen-studio-mac && werkzeuge/verteilen.sh --tester)      # build/verteilen-tester/Antennen-Studio-Tester.dmg
(cd ~/Projects/Git/Ai/FT991A-Remote && Tools/verteilen.sh --tester)             # dist/MacYaesu-Tester.dmg
(cd ~/Projects/Git/stretcher-mac && werkzeuge/verteilen.sh)                     # build/verteilen/Stretcher.dmg
# 2. Seite erzeugen (liest DMGs und App-Pakete; Notarisierung erkennt es selbst per `stapler validate`)
python3 erzeugen.py
# 3. Einspielen (Pfade unter /var/www/ sind in deploy-lxc.sh für 104 freigegeben)
D=~/Projects/Git/instructions/scripts/deploy-lxc.sh
$D 104 "$HOME/Projects/Git/aurora-studio-mac/build/verteilen/Aurora Studio.dmg" /var/www/html/tester/AuroraStudio.dmg
$D 104 ~/Projects/Git/kamera-import/dist/KameraImport-Tester.dmg /var/www/html/tester/KameraImport.dmg
$D 104 ~/Projects/Git/antennen-studio-mac/build/verteilen-tester/Antennen-Studio-Tester.dmg /var/www/html/tester/AntennenStudio.dmg
$D 104 ~/Projects/Git/Ai/FT991A-Remote/dist/MacYaesu-Tester.dmg /var/www/html/tester/MacYaesu.dmg
$D 104 ~/Projects/Git/stretcher-mac/build/verteilen/Stretcher.dmg /var/www/html/tester/Stretcher.dmg
$D 104 index.html /var/www/html/tester/index.html
$D 104 bilder/<app>.png /var/www/html/tester/bilder/<app>.png   # nur bei neuem Icon
# 4. Prüfen: Seite, Dateigrösse und Hash öffentlich, Cache-Header lesen
curl -sI https://www.aiscom.ch/tester/Stretcher.dmg | grep -i "content-length"
curl -s https://www.aiscom.ch/tester/Stretcher.dmg | shasum -a 256
```

Stand 2026-10-24: Aurora 1.0 (3), Kamera-Import 1.0 (7), Antennen-Studio 1.0 (7), MacYaesu 1.1 (2),
Stretcher 0.2 (2), Obhut 0.1 (1, `obhut-app/werkzeuge/verteilen.sh`). Mietprüfer 0.1 (1) seit 2026-10-24 (`mietpruefer/werkzeuge/verteilen.sh`, Tester-Fassung `MIET_TESTER`, Sparkle-Kanal `mietpruefer-tester`; iPhone-Karte aus dem vollen Klicktestlauf 12/12). Kamera-Import Build 8 (Einordnung gut/schlecht) ist bewusst nicht hier, nur lokal zum Testen.

Nicht auf der Seite: Zeigerwerk (braucht VU1-Hardware, Lizenz-Prüfschlüssel offen).


## Reiter je Plattform (seit 2026-10-24)

Die Seite hat drei Reiter (`#mac`, `#ios`, `#android`):
- **macOS:** DMGs. Version, Signatur und System liest `erzeugen.py` aus der App IM DMG (eingehängt), nicht aus dem
  Bauordner. So passt die Seite immer zum Download. Kamera-Import bleibt auf Build 7
  (`kamera-import/dist/tester-live/`), Build 8 ist nur beim Benutzer.
- **iPhone/iPad:** keine Downloads (Apple erlaubt nur TestFlight). Die Karte zeigt Version aus dem Simulator-Bau und das
  Ergebnis des letzten vollen Klicktestlaufs aus `build/mobiltest.xcresult` des jeweiligen Projekts.
- **Android:** signierte Release-APKs. Version und Mindest-Android kommen aus `aapt`, das Zertifikat aus `apksigner`
  (braucht `JAVA_HOME` von Homebrew-OpenJDK). Einspielen wie die DMGs: `deploy-lxc.sh 104 <apk> /var/www/html/tester/<Name>.apk`.
