# Funkerbier – Terminsuche

Ein einfacher Doodle-Ersatz für das Funkerbier, als Web-App unter
<https://bier.ingok.ch>. Die Optik ist Amiga Workbench 1.3 trifft
Flipper-Backbox trifft Kurzwellen-Shack. Seit 2026-10-15.

## Ablauf

1. **Termin erfassen:** Titel, Ort, Notiz, Dauer, Name und Mail eingeben und
   Termine vorschlagen (einzeln oder „+ 4 Freitage“). Der Organisator bekommt
   per Mail den Link für alle und den geheimen Admin-Link.
2. **Eintragen:** Wer den Link hat, drückt bei jedem Termin den Bumper
   (grün = JA, gelb = VIELLEICHT, rot = NEIN) und trägt Name, Rufzeichen
   (optional) und Mail ein. Danach kommt eine QSL-Bestätigung mit einem
   Bearbeiten-Link. Der Organisator bekommt eine kurze Meldung.
3. **Festlegen:** Der Admin drückt bei einem Termin zweimal FESTLEGEN. Dann
   geht eine Rundmail mit `.ics`-Kalendereintrag an alle Eingetragenen,
   höchstens alle 10 Minuten. Die Seite zeigt JACKPOT und einen
   Kalender-Knopf.

Die Startseite zeigt im Fenster „LOG · WER IST QRV“ alle laufenden Runden
(ein Termin heute oder später, höchstens 12) mit Namen, Rufzeichen und Zusagen
je Termin (API `GET api.php?a=uebersicht`, ohne Mailadressen). Damit ist jede
laufende Runde für alle sichtbar, nicht nur für Inhaber des Links.

Ein Login gibt es nicht. Eine Runde hat eine öffentliche ID mit 8 Zeichen.
Der Admin-Link und die Bearbeiten-Links tragen zufällige 128-Bit-Token. Der
Browser merkt sich die Token in `localStorage`, Mailadressen sieht nur der Admin.

## Atmosphäre

| Teil | Datei |
|---|---|
| Musik | `public/funkerbier.mod`: eine echte ProTracker-MOD mit 4 Kanälen und eigener Komposition (Am–F–C–G, Arpeggio-Akkorde, die Melodie morst „CQ“). Erzeugt mit `tools/make_mod.py`, also frei von Rechten. |
| Abspieler | `public/mod-worklet.js`: eigener ProTracker-Replayer als AudioWorklet (Effekte 0–6, 9, A–F). Offline testen mit `node tools/mod-test.js <out.wav> 80`. |
| Funk | `public/audio.js`: Bandrauschen mit QSB-Fading, Knackser, durchziehende Träger, CW-Rufe („CQ CQ DE HB9BIER“), Bandwechsel am VFO, Roger-Beep, Squelch. Dazu die Flippertöne: Bumper, Jackpot, Tilt, Diskettenlaufwerk. |
| Optik | `public/dmd.js` (Dot-Matrix-Display mit 128×32 Punkten), `public/fx.js` (Copper-Balken, Sternenfeld, Sinus-Laufschrift, S-Meter und Wasserfall aus den Analysern der Audiokette) |
| Gags | Boot-Diskette als Klick für den Ton (Autoplay-Sperre). Fünfmal schnell aufs DMD tippen ergibt TILT und setzt die Punkte zurück. |

Der Ton startet erst nach einem Klick: Auf der Diskette „TERMIN ERFASSEN ♪“
oder später mit dem Knopf ⏻. Die Regler und der Ton-Zustand bleiben im Browser
gespeichert.

## Aufbau und Betrieb

| Teil | Ort |
|---|---|
| Oberfläche | `public/*.html, *.js, *.css` (Vanilla JS als ES-Module, kein Build) |
| Server | `public/api.php`, SQLite `/var/lib/bier/bier.db` (www-data) |
| Mail | PHPMailer in `/opt/bier/vendor` (Composer), Gmail-Zugang `/etc/bier/mail.json` (root:www-data 640, aus `gmail` in cAuroras `config.local.php`) |
| vHost | `deploy/bier.conf` (Apache auf cAiscom, LXC 104) |
| Einspielen | `deploy/deploy.sh` (sichert nach `/root/bier-backup-<zeit>`, `php -l`, `configtest`) |
| Proxy | `deploy/npm_host.sh`: NPM-Host und Zertifikat, die IDs bestimmt das Skript selbst. Erst nach dem CNAME ausführen. |

Schutz gegen Missbrauch als Mailschleuder:
- Pro IP und Stunde: 6 neue Runden, 20 neue Einträge, 60 Änderungen und 5 Link-Anfragen. Die echte IP kommt über `X-Real-IP` von NPM.
- Ein Honigtopf-Feld fängt Bots ab.
- Je Runde und Mailadresse ist nur ein Eintrag erlaubt.
- Mailtexte sind feste Vorlagen; Betreffzeilen und Adressen werden ohne Zeilenumbrüche übernommen.
- Adressen auf `.invalid` (RFC 2606) werden nie angeschrieben. Mit ihnen lässt sich die API ohne echte Mails testen.

## Stand 2026-10-15

- Auf cAiscom eingespielt und intern per `Host: bier.ingok.ch` getestet.
  Geprüft sind der API-Durchlauf (anlegen, eintragen, doppelt, ändern,
  Rufzeichenprüfung, Datenschutz der Mails, falscher Admin, festlegen,
  Bremse der Rundmail, ICS mit 19:00 MESZ = 17:00Z, Honigtopf) und die
  Oberfläche in Chrome auf Desktop und in Handybreite, mit Ton.
- Seit 2026-10-16 öffentlich: CNAME bei hosttech, NPM-Host 43, Zertifikat
  `npm-89` (bis 2027-01-14). Im LAN über den Hairpin, ohne Unbound-Override.
- **Offen:**
  - ein echter Mailtest an eine echte Adresse
  - die Mengenbremse ist noch nicht unter Last geprüft
