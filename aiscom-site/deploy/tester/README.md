# Tester-Seite (www.aiscom.ch/tester/)

Download-Seite für Tester (nicht verlinkt, `noindex`): Aurora Studio und Kamera-Import als DMG mit Verknüpfung
„Programme“ zum Hineinziehen, Icons, Version/Build/Datum/Grösse/SHA-256 je App, Installationsschritte, Testpunkte.

Neue Fassung veröffentlichen:

```zsh
# 1. DMGs bauen (Developer ID; --ohne-notar = ohne Apples Notardienst, dann braucht der erste Start Rechtsklick › Öffnen)
(cd ~/Projects/Git/aurora-studio-mac && werkzeuge/verteilen.sh --ohne-notar)   # build/verteilen/Aurora Studio.dmg
(cd ~/Projects/Git/kamera-import && ./verteilen.sh --ohne-notar)              # dist/KameraImport.dmg
# 2. Seite erzeugen (liest die DMGs und App-Pakete; --notarisiert, wenn ohne --ohne-notar gebaut wurde)
python3 erzeugen.py
# 3. Einspielen (Pfade unter /var/www/ sind in deploy-lxc.sh für 104 freigegeben)
D=~/Projects/Git/instructions/scripts/deploy-lxc.sh
$D 104 "$HOME/Projects/Git/aurora-studio-mac/build/verteilen/Aurora Studio.dmg" /var/www/html/tester/AuroraStudio.dmg
$D 104 ~/Projects/Git/kamera-import/dist/KameraImport.dmg /var/www/html/tester/KameraImport.dmg
$D 104 index.html /var/www/html/tester/index.html
# 4. Prüfen: Seite, Dateigrösse und Hash öffentlich
curl -sI https://www.aiscom.ch/tester/AuroraStudio.dmg | grep -i "content-length"
curl -s https://www.aiscom.ch/tester/AuroraStudio.dmg | shasum -a 256
```
