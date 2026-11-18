# Kompass (kompass.aiscom.ch)

Persönliches ADHS-Alltags-Dashboard: eine Sache jetzt, Zeit sichtbar, Erfassen in
2 Sekunden, keine roten Zähler. Plan und Grundsätze: [PLAN.md](PLAN.md). Stufe 1 ist
seit 2026-10-17 in Betrieb.

## Module (Stufe 1)

| Modul | Was es macht |
|---|---|
| **Heute** | Nächster Termin mit Countdown-Balken (füllt sich in den letzten 2 h), ganztägige Termine; **Jetzt-Karte** (Routine im Zeitfenster → Papier-Frist ≤ 3 Tage → Hauptaufgabe bzw. deren erster Schritt → erster offener Schritt); höchstens 3 Hauptaufgaben; „Heute neu entscheiden“ für Verpasstes; Geplant / Irgendwann |
| **Eingang** | Erfassen oben auf jeder Seite (Enter, Taste `N`); einsortieren mit `H` heute, `T` Tag, `P` Papier, `Z` zerlegen, `L` loslassen, `E` erledigt |
| **Routinen** | Morgen, Abend, Haus verlassen (mehrmals am Tag); abhaken, Fortschrittsbalken, Push um `erinnern_um` |
| **Papierkram** | Frist, Kategorie, Betrag, immer ein nächster Schritt; „Schritt erledigt“ fragt nach dem nächsten |
| **Zerleger** | Claude Haiku 5.5 schlägt 5–8 Schritte vor (nur auf Knopfdruck, nur der Eintragstext), bearbeitbar; ohne Schlüssel von Hand |
| **Fokus** | 5/15/25/45 min oder frei, Ring, Restzeit im Tab-Titel, Ton und Push am Ende, Hyperfokus-Push nach 90 min am Stück (über Verlängerungen) |

Desktop ab 900 px mit Seitenleiste und Timer rechts, Handy mit Leiste unten. Die PWA
lässt sich zum Startbildschirm hinzufügen. Der Service Worker hält nur die App-Hülle,
Daten kommen immer vom Server. Tastenkürzel: `?`.

## Betrieb

- **Ort:** cDocker (LXC 100), Stack `/opt/stacks/kompass`, Container `kompass`, Port
  **8096** → 8000 (gunicorn, **ein** Worker mit 4 Threads: Login-Sperre und
  Hintergrund-Thread leben im Prozess).
- **Daten:** `/opt/stacks/kompass/data/` (700, root):
  - `kompass.db` (SQLite)
  - `config.json` (600; iCal-Adresse, ntfy-Thema, Claude-Schlüssel)
  - `secret.key`
  - `sicherung/kompass-JJJJMMTT.db` (täglich nach 03:30, 14 Tage)
  - beim Erststart `admin-passwort.txt`. Die Datei verschwindet beim ersten
    Passwortwechsel.
- **Sicherung:** Zusätzlich liegt CT 100 im täglichen vzdump-Job von pvenuc (21:00,
  `NVMEBackupExt`).
- **Hintergrund-Thread** im Prozess:
  - alle 30 s Erinnerungen
  - alle 10 min Kalender
  - täglich die Sicherung
- **Öffentlich:** `https://kompass.aiscom.ch` über NPM-Host 45 und Zertifikat 91
  (`npm-91`, bis 2027-01-15, NPM erneuert). Angelegt mit
  [`deploy/npm_host_45.sh`](deploy/npm_host_45.sh); einmalig, nicht erneut ausführen.
  - DNS: CNAME `kompass.aiscom.ch` → `aiscom.duckdns.org.` bei hosttech.
  - Im LAN über den Hairpin, ohne Unbound-Override.

Startpasswort lesen (im eigenen Terminal):

```zsh
ssh root@192.168.178.113 'cat /opt/stacks/kompass/data/admin-passwort.txt'
```

Aktualisieren (vom Mac, im Ordner `Ai/kompass`):

```zsh
COPYFILE_DISABLE=1 tar czf - --exclude __pycache__ app.py Dockerfile compose.yaml requirements.txt .dockerignore kompass \
  | ssh root@192.168.178.113 'tar xzf - -C /opt/stacks/kompass --no-same-owner && cd /opt/stacks/kompass && docker compose up -d --build'
```

## Anbindungen

- **Kalender:** geheime iCal-Adresse aus Google Kalender (Einstellungen → Kalender →
  „Privatadresse im iCal-Format“), nur lesen, 14 Tage voraus, Wiederholungen über
  `recurring-ical-events`, abgesagte Termine fallen weg. Die Adresse wird nie geloggt
  und nur maskiert angezeigt.
- **ntfy:** JSON-Publish an `http://192.168.178.113:8086`, Thema zufällig
  (`kompass-…`, in den Einstellungen sichtbar).
  - **Empfang:** In der ntfy-App den Server und dieses Thema abonnieren. Unterwegs
    braucht das Handy Tailscale, denn ntfy ist nicht öffentlich freigegeben.
  - **Ruhezeiten:** Standard 21:30–07:00, gelten für Routinen, Papier und Termine.
    Selbst gestartete Timer melden sich immer.
  - **Doppelsperre:** über die Tabelle `gesendet`; ein fehlgeschlagener Push wird
    beim nächsten Durchlauf wiederholt.
- **Claude:** direkt per HTTP an `api.anthropic.com/v1/messages` (kein SDK).
  - Nur auf Knopfdruck, höchstens alle 10 s, Zeitlimit 20 s.
  - Die Antwort wird als JSON-Liste geprüft (≤ 12 Schritte).
  - Der Schlüssel erscheint nie in Fehlermeldungen.

## Sicherheit

- **Login:** `admin`, werkzeug-Hash, neues Passwort mindestens 12 Zeichen. Sperre nach
  5 Fehlversuchen je IP in 10 min (IP über `ProxyFix` aus NPM).
- **Sitzung:** Cookie `kompass` mit `Secure`, `HttpOnly` und `SameSite=Lax`, 30 Tage.
  `KOMPASS_COOKIE_SECURE=0` nur für lokale Tests ohne https.
- **Anfragen:** CSRF im Formular bzw. als Header `X-CSRF`; die API nimmt nur JSON an.
- **Kopfzeilen:** CSP ohne `unsafe-inline` (keine Inline-Skripte und -Stile in Templates),
  `X-Frame-Options: DENY`, `Referrer-Policy: same-origin`, `noindex`.

## Entwicklung und Tests

```zsh
python3 -m venv .venv && .venv/bin/pip install -r requirements.txt pytest
.venv/bin/python -m pytest -q          # 28 Tests, ohne Netz (autouse-Sperre in conftest.py)
```

- **Testaufbau:** Die Tests laufen mit fester Uhr (`uhr`-Fixture) und temporärer DB.
  Externe Dienste ersetzen Attrappen; ein echter Netzzugriff lässt den Test scheitern.
- **Oberfläche geprüft am 2026-10-17:**
  - bei 390 px (Handy, über einen Rahmen im lokalen Testserver, weil Chrome headless
    kein Fenster unter 500 px kennt) und 1440 px, jeweils hell und dunkel
  - Bedienung im Browser: Erfassen, Taste Z, Zerleger ohne Schlüssel, Schritte
    übernehmen, Timer per Leertaste, Routine abhaken, Papier-Schritt
- **Struktur:**

  | Datei | Inhalt |
  |---|---|
  | `kompass/__init__.py` | App-Fabrik, Login, Kopfzeilen, Hintergrund |
  | `api.py` | JSON-API |
  | `auswahl.py` | Jetzt-Karte und Heute |
  | `db.py` | Schema, Migrationen, Vorlagen |
  | `erinnern.py` | Pushes |
  | `fokus.py` | Timer |
  | `kalender.py` | Kalender |
  | `zerleger.py` | Zerleger |
  | `einstellungen.py` | Einstellungen |
  | `zeit.py` | Zeitfunktionen |
  | `static/app.js` | Oberfläche |
  | `static/stil.css` | Stil |

## Offen

- **Benutzer:** Startpasswort ändern, iCal-Adresse und Claude-Schlüssel in den
  Einstellungen eintragen, ntfy-Thema abonnieren, Test-Push bestätigen
  (Definition of Done).
- **Stufe 2** laut PLAN.md: Wochenrückblick, Check-in, Haushalt rollierend,
  Offline-Erfassung.
