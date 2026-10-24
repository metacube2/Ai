# RSS-Feeds der Apps

Seit 2026-10-24 (Wunsch des Benutzers: „RSS-Feed für deine Apps machen und auf der Webseite einbinden“).

| Feed | Live | Quelle | Inhalt |
|---|---|---|---|
| öffentlich | <https://www.aiscom.ch/feed.xml> | `deploy/feed/erzeugen.py` | Mac-Apps mit öffentlichem Download (MacYaesu, Kamera-Import: DMG geladen, Version aus der App im DMG), cAurora Android (Beta-APK, Version aus `aapt`), Produktseiten ohne Download (Antennen-Studio, Aurora Studio) |
| Tester | <https://www.aiscom.ch/tester/feed.xml> | `deploy/tester/erzeugen.py` | jede Fassung der Tester-Seite (macOS, iPhone/iPad mit Klicktest-Ergebnis, Android) |

- Gemeinsame Bausteine in `rss.py` (RSS 2.0 mit `atom:link rel="self"`). Jede Version ist ein Eintrag mit fester
  Kennung `<app>-<plattform>-<version>-<build>`; der Verlauf liegt in `verlauf.json` (je Ordner, im Repo) und wächst
  nur. Datum: „Last-Modified“ der verteilten Datei bzw. Ende des Klicktestlaufs.
- Eingebunden: Next-Seite (`app/[locale]/layout.tsx` → `<link rel="alternate" type="application/rss+xml">` in allen
  Sprachen, sichtbarer Link „Neue App-Versionen per RSS-Feed abonnieren“ unter den Produkten in beiden Designs,
  Text `products.rss_text` in `locales/*.json`); Tester-Seite (`<link rel="alternate">`, Kasten „Neueste Fassungen“
  mit den letzten sechs Einträgen und Knopf „RSS-Feed abonnieren“).
- `/feed.xml` liefert Apache statisch (nicht in der Next-Allowlist des Proxys).

Nach jeder neuen Version:

```zsh
cd ~/Projects/Git/Ai/aiscom-site/deploy
python3 feed/erzeugen.py && ~/Projects/Git/instructions/scripts/deploy-lxc.sh 104 feed/feed.xml /var/www/html/feed.xml
python3 tester/erzeugen.py   # schreibt index.html und feed.xml der Tester-Seite; beide einspielen
```
