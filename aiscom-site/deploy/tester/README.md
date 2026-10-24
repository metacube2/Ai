# Tester-Seite (www.aiscom.ch/tester/)

Download-Seite für Tester (nicht verlinkt, `noindex`): Aurora Studio, Kamera-Import und Antennen-Studio als DMG mit
Verknüpfung „Programme“ zum Hineinziehen, Icon, Version/Build/Datum/Grösse/SHA-256 je App und die Installationsschritte.
Sonst nichts: keine Testpunkte, Grenzen oder Hinweise (Benutzer 2026-10-24). Alle Apps sind **Vollversionen**:
Kamera-Import und Antennen-Studio als Tester-Fassung ohne Lizenzschlüssel (`verteilen.sh --tester`, Compile-Schalter
`KAMERA_TESTER` bzw. `ANTENNEN_TESTER`), Aurora Studio hat keine Demo-Grenze. Kein Schlüssel steht auf der Seite.

Neue Fassung veröffentlichen:

```zsh
# 1. DMGs bauen (Developer ID; --ohne-notar = ohne Apples Notardienst, dann braucht der erste Start Rechtsklick › Öffnen;
#    Notarisierung nur mit OK des Benutzers)
(cd ~/Projects/Git/aurora-studio-mac && werkzeuge/verteilen.sh --ohne-notar)              # build/verteilen/Aurora Studio.dmg
(cd ~/Projects/Git/kamera-import && ./verteilen.sh --tester --ohne-notar)                 # dist/KameraImport-Tester.dmg
(cd ~/Projects/Git/antennen-studio-mac && werkzeuge/verteilen.sh --tester --ohne-notar)   # build/verteilen-tester/Antennen-Studio-Tester.dmg
# 2. Seite erzeugen (liest DMGs und App-Pakete; Notarisierung erkennt es selbst per `stapler validate`)
python3 erzeugen.py
# 3. Einspielen (Pfade unter /var/www/ sind in deploy-lxc.sh für 104 freigegeben)
D=~/Projects/Git/instructions/scripts/deploy-lxc.sh
$D 104 "$HOME/Projects/Git/aurora-studio-mac/build/verteilen/Aurora Studio.dmg" /var/www/html/tester/AuroraStudio.dmg
$D 104 ~/Projects/Git/kamera-import/dist/KameraImport-Tester.dmg /var/www/html/tester/KameraImport.dmg
$D 104 ~/Projects/Git/antennen-studio-mac/build/verteilen-tester/Antennen-Studio-Tester.dmg /var/www/html/tester/AntennenStudio.dmg
$D 104 index.html /var/www/html/tester/index.html
$D 104 bilder/antennen-studio.png /var/www/html/tester/bilder/antennen-studio.png   # nur bei neuem Icon
# 4. Prüfen: Seite, Dateigrösse und Hash öffentlich, Cache-Header lesen
curl -sI https://www.aiscom.ch/tester/AntennenStudio.dmg | grep -i "content-length"
curl -s https://www.aiscom.ch/tester/AntennenStudio.dmg | shasum -a 256
```

Nicht auf der Seite (Stand 2026-10-24): Zeigerwerk (braucht VU1-Hardware, Lizenz-Prüfschlüssel offen), MacYaesu
(braucht FT-991A, altes Projekt `Ai/FT991A-Remote`), Stretcher (Forschungswerkzeug ohne Verteilskript).
