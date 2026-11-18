# Tester-Seite (www.aiscom.ch/tester/)

**Seit 2026-11-06 mit Passwort** (Wunsch Benutzer: nur er und Adi): Apache-Sperre `../aiscom-tester-schutz.conf`
(cAiscom `/etc/apache2/conf-available/aiscom-tester-schutz.conf`, Passwortdatei `/etc/apache2/aiscom-tester.htpasswd`,
Benutzer `tester`, Passwort beim Benutzer). Geschützt: `/tester/` und die Update-Kanäle der schlüsselfreien
Tester-Fassungen (`/updates/{antennen-studio,fotogedaechtnis,kamera-import,macyaesu,mietpruefer,obhut,stretcher}-tester/`).
Offen: `/updates/taploop-tester/` (öffentliche Seite /taploop/) und `/updates/aurora-studio/`. Folge: Sparkle-Updates der
geschützten Tester-Fassungen laufen nicht mehr automatisch (401) – neue Fassungen über die Seite laden. Passwort ändern:
`ssh root@192.168.178.161 pct exec 104 -- htpasswd -B /etc/apache2/aiscom-tester.htpasswd tester` (interaktiv, Benutzer).
Prüfen: `curl -s -o /dev/null -w "%{http_code}" https://www.aiscom.ch/tester/` → 401.

Download-Seite für Tester (nicht verlinkt, `noindex`): alle Mac-Apps als DMG (Liste im Wörterbuch von `erzeugen.py`,
Stand 2026-11-08 u. a. TapLoop, Makrowerk, ScreenCam, Mietprüfer, Fotogedächtnis, Obhut, Chronik-Apps), dazu
Android-APKs und das E-Book; DMGs mit Verknüpfung „Programme“ zum Hineinziehen, Icon, Version/Build/Datum/Grösse/SHA-256 je App und die
Installationsschritte. Sonst nichts: keine Testpunkte, Grenzen oder Hinweise (Benutzer 2026-10-24). Alle Apps sind
**Vollversionen**: Kamera-Import, Antennen-Studio und MacYaesu als Tester-Fassung ohne Lizenzschlüssel
(`verteilen.sh --tester`, Compile-Schalter `KAMERA_TESTER`, `ANTENNEN_TESTER`, `MACYAESU_TESTER`), Aurora Studio und
Stretcher haben keine Demo-Grenze. Kein Schlüssel steht auf der Seite.

**Gruppen** (Wunsch Benutzer 2026-10-27): Jeder Plattform-Reiter zeigt „Zum Testen bereit“ (grün), „In Entwicklung“
(rot) und „Fertig“ (grün, = im Store freigegeben). Zuordnung im Wörterbuch `STAND` in `erzeugen.py` nach `kennung`;
nicht aufgeführte Apps sind „In Entwicklung“. Stand 2026-10-27: TapLoop und Kamera-Import testbereit, alle anderen in
Entwicklung, noch keine fertig.

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

Stand 2026-11-18 22:10: neun **Alpha**-Apps neu (Agenten-Monitor, Funkblick, Für Dich, Painbrush, MainBeam, Tabwache, Spiegelwerk, Vitalbrücke, Zeitregler; Zeigerwerk bewusst nicht, Adi hat eine bessere gebaut). Markierung über `"alpha": True` im APPS-Eintrag (orange Marke an der Karte, „– Alpha“ im Feed); Gruppe „In Entwicklung“. Neue Apps ohne eigenes Skript: `instructions/scripts/tester-dmg.sh` (signieren, DMG, notarisieren). Icons für Agenten-Monitor und Funkblick gezeichnet (kein App-Icon vorhanden). Painbrush und Zeitregler haben über ihr Verteilskript auch die Sparkle-Liste veröffentlicht (`--ohne-update` verhindert das). iPhone-Reiter von Hand übernommen, danach auch die Zahl im Reiterkopf (13) von Hand setzen. Stand 2026-11-18: Benutzer sah eine alte Fassung der Seite, obwohl der Server die neue lieferte (Browser-Cache, Apache schickte für HTML kein Cache-Control). Seither setzt `/etc/apache2/conf-available/aiscom-tester-schutz.conf` auf cAiscom für `/tester/`, `index.html`, `feed.xml` und `*.html` `Cache-Control: no-cache, must-revalidate` (always, also auch bei 401). Stand 2026-11-17 23:26: **FT991A Remote für Windows 1.0.0** (Tauri, `ft991a-remote-win/build/windows/…_x64-setup.exe`, NSIS unsigniert, per Cross-Build) als `FT991A-Remote-Windows-Setup.exe` mit `FT991A-Remote-Windows-Testanleitung.html` (aus dem Repo `docs/`); `erzeugen.py` liest die Version jetzt auch aus NSIS-Namen. iPhone-Reiter wieder von Hand übernommen. Hash auf dem Server geprüft (öffentlich 401 = Passwortschutz). Stand 2026-11-17 13:18: neuer Reiter **Windows** (Liste `WINDOWS` in `erzeugen.py`, MSI aus den Desktop-Ports unter
`/Volumes/ExtOsX/Entwicklung/desktop/`, Version aus dem Dateinamen, Testanleitung als eigene Seite daneben, z. B.
`MacYaesu-Windows-Testanleitung.html`, Wunsch Benutzer). Erste Karte MacYaesu für Windows 1.1.1 (`MacYaesu-Windows.msi`,
unsigniert, SmartScreen-Hinweis in den Schritten). Einspielen wie die DMGs per `deploy-lxc.sh 104`. iPhone-Reiter wieder von
Hand aus der vorigen Seite übernommen. Stand 2026-11-15 18:12: NetBottleneck 1.0 (2) (Live-Spuren, Legende, Ring; Helper 0.3.0); iPhone-Reiter wieder von Hand aus der Live-Seite übernommen (Simulatorbauten fehlen weiterhin). Stand 2026-11-15 14:36: NetBottleneck 1.0 (1) neu (`nadeloehr-mac/mac/werkzeuge/verteilen.sh`, notarisiert, DMG mit Ordner „NetBottleneck Helper“ für Windows/Linux/macOS, ohne Sparkle; Gruppe „Zum Testen bereit“). Seit diesem Lauf lässt `erzeugen.py` fehlende iPhone-Simulatorbauten mit Warnung weg; weil alle 13 fehlten, wurde der iPhone-Reiter von Hand aus der vorigen Seite übernommen (Simulatorbauten neu erzeugen, dann wieder normal). Stand 2026-11-05 23:10: TapLoop 1.0 (44) macOS (Video-Teller, Sparkle taploop-tester). Stand 2026-10-24 abends (laut `verlauf.json`): Aurora 1.0 (4), Kamera-Import 1.0 (9), Antennen-Studio 1.0 (8), MacYaesu 1.1 (3), Stretcher 0.2 (5), Obhut 0.1 (2) – alle mit Sparkle. Stand 2026-10-27 abends: TapLoop 1.0 (41) macOS (notarisiert, Sparkle taploop-tester, 28 synthetische Beispielplatten, Farbwelle/Taktuhr/Tonkreis). Stand 2026-10-27: Makrowerk 0.1 (1) neu (`makrowerk/werkzeuge/verteilen.sh`, ohne Sparkle, iPhone-Karte aus Klicktest 5/5 + Foto-Test übersprungen). Stand 2026-10-26: TapLoop 1.0 (38) macOS (notarisiert, Sparkle taploop-tester), iPhone-Karte 1.0 (36). Stand 2026-10-24: Aurora 1.0 (3), Kamera-Import 1.0 (7), Antennen-Studio 1.0 (7), MacYaesu 1.1 (2),
Stretcher 0.2 (2), Obhut 0.1 (1, `obhut-app/werkzeuge/verteilen.sh`). Mietprüfer 0.1 (1) seit 2026-10-24 (`mietpruefer/werkzeuge/verteilen.sh`, Tester-Fassung `MIET_TESTER`, Sparkle-Kanal `mietpruefer-tester`; iPhone-Karte aus dem vollen Klicktestlauf 12/12). Kamera-Import Build 8 (Einordnung gut/schlecht) ist nicht hier, aber seit 2026-11-06 die Website-Fassung (ohne Sparkle).

Reiter **Buch** (seit 2026-10-24): E-Book „Die Schweiz denkt mit“ (`Ai/ki-buch/ausgabe/…epub`, Liste `BUECHER` in `erzeugen.py`,
Umschlag `bilder/ki-buch.png` aus `ausgabe/umschlag.png`); einspielen nach `/var/www/html/tester/Die-Schweiz-denkt-mit.epub`
(Apache liefert `application/epub+zip`).

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
