# Kompass – ADHS-Alltags-Dashboard (Plan für Claude Code)

Stand 2026-10-17. **Stufe 1 ist gebaut und in Betrieb** (siehe [README.md](README.md)); offen ist nur die Definition of Done durch den Benutzer (echter Push bestätigt). Dieser Plan ist die Arbeitsgrundlage für Claude Code. Er hält die
Entscheidungen des Benutzers fest (Fragebogen vom 2026-10-17), das Datenmodell, die
Ausbaustufen und die Regeln für die Umsetzung. Zuerst ganz lesen, dann Stufe für
Stufe bauen. Erst wenn eine Stufe die Definition of Done erfüllt, kommt die nächste.

## 1. Ziel und Grundsätze

Ein persönliches Dashboard, das den Alltag mit ADHS leichter macht: weniger merken,
schneller anfangen, Zeit sehen, nichts verlieren. Es ist ein Werkzeug zur
Selbstorganisation und keine medizinische Anwendung.

Diese Grundsätze gehen jedem Feature-Wunsch vor:

1. **Eine Sache jetzt.** Die Startansicht zeigt eine Karte „Das ist dran“, keine lange Liste.
2. **Zeit sichtbar machen.** Countdown-Balken bis zum nächsten Termin, laufende Timer
   mit Restzeit, nirgends ein abstraktes „später“.
3. **Erfassen in 2 Sekunden.** Ein Feld, Enter, fertig. Kein Formular, keine Pflichtfelder,
   kein Einsortieren beim Erfassen.
4. **Keine Schuld-Anzeige.** Keine roten Zähler, kein „17 überfällig“. Verpasstes
   landet still unter „Heute neu entscheiden“, mit den Knöpfen *heute*, *anderer Tag*,
   *loslassen*.
5. **Weniger ist mehr.** Jede Ansicht hat höchstens eine Hauptaktion. Neue Module
   brauchen einen Grund aus der Nutzung, nicht aus der Idee.
6. **Kein Punkte- und Abzeichen-System.** Erfolg zeigt der Wochenrückblick (Stufe 2):
   Was ist geschafft worden.
7. **Freundliche Sprache.** „Was ist der nächste kleine Schritt?“ statt „Aufgabe fällig“.

## 2. Entscheidungen (Fragebogen 2026-10-17)

| Frage | Entscheidung |
|---|---|
| Hauptgerät | **Handy und Desktop gleichwertig** |
| Betrieb | **Heimserver**: Docker auf cDocker (LXC 100), SQLite |
| Technik | **Python + Vanilla-JS** (Flask, keine Build-Kette), Muster `Ai/bildervon` |
| Stufe 1 | **Heute + Eingang, Routinen, Papierkram & Fristen, Zerleger + Fokus-Timer** |
| Anbindungen von Anfang an | **Google-Kalender (lesen), Push über ntfy, Claude-Zerleger** |
| Adresse | **kompass.aiscom.ch** (gewählt war kompass.ingok.ch; der CNAME liegt in der Zone aiscom.ch, darum diese Adresse) |

Vom Planer festgelegt (begründet, änderbar):

- **Kalender über die geheime iCal-Adresse** (Google Kalender → Einstellungen →
  „Privatadresse im iCal-Format“) statt OAuth. Nur lesen, kein Google-Login im Dienst,
  kein Token-Ablauf. Die Adresse gilt als Geheimnis.
- **Zerleger mit Claude Haiku 5.5** (`claude-haiku-5-5`): schnell und günstig, für
  kurze Schrittlisten ausreichend. Modell per Konfiguration umstellbar.
- **Ein Benutzer**, Login `admin` wie bei `bildervon`. Mehrbenutzer ist kein Ziel.

## 3. Architektur und Betrieb

```
Handy/Desktop (PWA) ──https──> NPM cNginx .111 (Host 45, Zertifikat 91)
                                  │
                                  ▼
                cDocker .113:8096 ─ Container kompass (Flask + gunicorn, 1 Worker)
                                  │   ├─ /data/kompass.db (SQLite, WAL)
                                  │   ├─ /data/config.json (iCal-URL, ntfy-Thema, Claude-Schlüssel; 600)
                                  │   └─ Hintergrund-Thread: Kalender holen, Erinnerungen
                                  ├──> ntfy http://192.168.178.113:8086 (Push)
                                  ├──> Google iCal-Adresse (alle 10 min)
                                  └──> api.anthropic.com (nur beim Zerlegen)
```

- **Ordner im Repo:** `Ai/kompass/` mit `app.py` (oder Paket `kompass/`, sobald
  `app.py` über ~600 Zeilen wächst), `templates/`, `static/`, `Dockerfile`,
  `compose.yaml`, `requirements.txt`, `tests/`, `README.md`.
- **Stack auf cDocker:** `/opt/stacks/kompass`, Port **8096** → 8000 (am 2026-10-17 frei;
  vor dem Einbau mit `ss -ltn` nochmals prüfen). Daten in `/opt/stacks/kompass/data/`.
- **Ein gunicorn-Worker mit Threads**, weil Login-Sperre und Hintergrund-Thread im
  Prozess leben (wie `bildervon`).
- **Zeitzone** `Europe/Zurich` im Container (`TZ`), alle Zeiten in der DB als UTC-ISO,
  Anzeige lokal.
- **Abhängigkeiten klein halten:** `flask`, `gunicorn`, `icalendar` +
  `recurring-ical-events` (Wiederholungstermine), `anthropic`. Kein ORM, `sqlite3` direkt.
- **PWA:** `manifest.webmanifest`, Icons, Service Worker nur für die App-Hülle
  (HTML/CSS/JS offline). Daten immer frisch vom Server, kein Offline-Datenstand in Stufe 1.

### Öffentlicher Zugang

1. **DNS:** erledigt am 2026-10-17. Der CNAME `kompass.aiscom.ch` → `aiscom.duckdns.org.`
   ist auf ns1–ns3.hosttech aktiv. Vor dem Zertifikat nochmals
   `dig +short @8.8.8.8 kompass.aiscom.ch` prüfen. (`*.ingok.ch` zeigt per Wildcard auf
   hosttech; für `kompass.ingok.ch` müsste der CNAME in die Zone ingok.ch.)
2. **NPM:** Host 45, Zertifikat 91 (IDs vor dem Anlegen per SQLite prüfen), Vorlage
   `cAuroraAndroid/backend/caurora-studio/npm_host_44.sh`: certbot mit NPM-Parametern,
   Zeilen in `certificate`/`proxy_host`, `.conf` aus Host 43, `nginx -t`, Rücknahme bei
   Fehler. Force SSL, HTTP/2, HSTS, Block Exploits.
3. **LAN:** Der Hairpin ist aktiv. Kein Unbound-Override, also keine OPNsense-Änderung.
4. Prüfen nach `harness.md` §6: öffentlich mehrmals abrufen, `x-served-by`, Zertifikat,
   HTTP→HTTPS 301, ohne Login 302 auf `/login`.

## 4. Datenmodell (SQLite)

Alle Tabellen haben `id INTEGER PRIMARY KEY`, `erstellt` und `geaendert` (UTC-ISO).
Gelöscht wird weich (`archiviert` statt `DELETE`), damit nichts verloren geht.

```sql
-- Eingang und Aufgaben: ein Eintrag ist zuerst nur ein Gedanke
CREATE TABLE eintrag (
  id INTEGER PRIMARY KEY,
  text TEXT NOT NULL,
  art TEXT NOT NULL DEFAULT 'eingang',      -- eingang | aufgabe | notiz | papier
  status TEXT NOT NULL DEFAULT 'offen',     -- offen | erledigt | losgelassen
  heute INTEGER NOT NULL DEFAULT 0,         -- 1 = eine der 1–3 Hauptaufgaben für 'tag'
  tag TEXT,                                 -- geplanter Tag (YYYY-MM-DD), NULL = irgendwann
  geschaetzt_min INTEGER,                   -- optionale Schätzung
  eltern_id INTEGER REFERENCES eintrag(id), -- Schritt eines zerlegten Eintrags
  reihenfolge INTEGER NOT NULL DEFAULT 0,
  erledigt_am TEXT,
  erstellt TEXT NOT NULL, geaendert TEXT NOT NULL
);

-- Papierkram & Fristen: Zusatz zu eintrag (art = 'papier')
CREATE TABLE papier (
  eintrag_id INTEGER PRIMARY KEY REFERENCES eintrag(id),
  kategorie TEXT,                -- rechnung | kuendigung | versicherung | amt | sonstiges
  frist TEXT,                    -- YYYY-MM-DD
  betrag_rappen INTEGER,         -- optional, nur zur Übersicht (keine Zahlungsfunktion)
  naechster_schritt TEXT NOT NULL,  -- immer ein konkreter Schritt, z. B. „PDF im Postfach öffnen“
  erinnern_tage_vorher INTEGER NOT NULL DEFAULT 3
);

-- Routinen
CREATE TABLE routine (
  id INTEGER PRIMARY KEY,
  name TEXT NOT NULL,            -- „Morgen“, „Abend“, „Haus verlassen“
  zeitfenster TEXT,              -- z. B. '06:30-09:00', optional
  erinnern_um TEXT,              -- HH:MM, optional (Push)
  aktiv INTEGER NOT NULL DEFAULT 1,
  reihenfolge INTEGER NOT NULL DEFAULT 0
);
CREATE TABLE routine_schritt (
  id INTEGER PRIMARY KEY,
  routine_id INTEGER NOT NULL REFERENCES routine(id),
  text TEXT NOT NULL,
  reihenfolge INTEGER NOT NULL DEFAULT 0
);
CREATE TABLE routine_lauf (      -- ein Durchgang pro Tag
  id INTEGER PRIMARY KEY,
  routine_id INTEGER NOT NULL REFERENCES routine(id),
  tag TEXT NOT NULL,
  abgehakt TEXT NOT NULL DEFAULT '[]',   -- JSON-Liste der erledigten schritt_ids
  fertig_am TEXT,
  UNIQUE (routine_id, tag)
);

-- Fokus-Timer
CREATE TABLE fokus (
  id INTEGER PRIMARY KEY,
  eintrag_id INTEGER REFERENCES eintrag(id),
  start TEXT NOT NULL,
  geplant_min INTEGER NOT NULL,
  ende TEXT,                     -- NULL = läuft
  pausen_erinnert INTEGER NOT NULL DEFAULT 0
);

-- Kalender-Zwischenspeicher (aus der iCal-Adresse, wird bei jedem Abruf ersetzt)
CREATE TABLE termin (
  uid TEXT NOT NULL, beginn TEXT NOT NULL, ende TEXT, titel TEXT NOT NULL,
  ganztags INTEGER NOT NULL DEFAULT 0, ort TEXT,
  PRIMARY KEY (uid, beginn)
);

-- Gesendete Erinnerungen (gegen Doppel-Push)
CREATE TABLE gesendet (schluessel TEXT PRIMARY KEY, am TEXT NOT NULL);

CREATE TABLE konto (id INTEGER PRIMARY KEY, name TEXT UNIQUE NOT NULL, hash TEXT NOT NULL);
```

Schema-Änderungen über eine Tabelle `schema_version` und nummerierte Migrationen in
`app.py` (keine Handarbeit an der Produktiv-DB).

## 5. Stufe 1 – Module

### 5.1 Heute (Startansicht)

- **Oben:** nächster Termin aus dem Kalender mit Countdown-Balken („in 47 min: Zahnarzt“).
  Der Balken füllt sich in den letzten 2 Stunden. Ganztägige Termine stehen als
  Zeile darüber. Ohne Termin: „Heute keine Termine mehr.“
- **Jetzt-Karte:** genau ein Eintrag mit den Knöpfen *Starten* (öffnet den Fokus-Timer),
  *Erledigt*, *Zerlegen*, *Nicht jetzt*.
  - **Auswahl:** zuerst eine laufende Routine in ihrem Zeitfenster, dann die
    nächstgelegene Papier-Frist (≤ 3 Tage), dann die erste Hauptaufgabe, dann der erste
    offene Schritt einer zerlegten Aufgabe.
  - *Nicht jetzt* nimmt die nächste Karte, ohne die erste zu bestrafen.
- **Hauptaufgaben:** höchstens 3 für heute (`heute = 1`). Eine vierte lässt sich nur
  hinzufügen, wenn eine andere weicht (sanfter Hinweis).
- **Heute neu entscheiden:** erscheint nur, wenn Einträge mit `tag < heute` offen sind.
  Je Eintrag *heute*, *anderer Tag*, *loslassen*. Zeigt keine Anzahl in Rot.
- **Erfassen:** Das Eingabefeld ist auf jeder Seite oben fixiert. Enter speichert in den
  Eingang. Desktop: Taste `N` fokussiert das Feld.

**Fertig, wenn:** Ein neuer Gedanke ist in unter 2 s gespeichert (Messung im Test), die
Jetzt-Karte folgt der Auswahlreihenfolge (Unit-Test), und ohne Kalender und Einträge
erscheint ein freundlicher Leerzustand.

### 5.2 Eingang

- **Liste** aller `art = 'eingang'`, neueste oben.
- **Einsortieren** mit einem Wisch (Handy) bzw. Tasten (Desktop):
  - `H` heute
  - `T` anderer Tag (Datumswahl mit „morgen“, „übermorgen“, „nächste Woche“)
  - `P` Papierkram (öffnet Frist und nächsten Schritt)
  - `Z` zerlegen
  - `L` loslassen
  - `E` erledigt
- **Hinweis** „Eingang leeren dauert meist 2 Minuten“, sobald mehr als 10 Einträge
  liegen. Neutral formuliert, ohne Warnfarbe.

### 5.3 Routinen

- **Vorlagen beim ersten Start**, frei änderbar:
  - **Morgen:** Wasser trinken, Medikamente, Zähne, Tagesplan ansehen.
  - **Abend:** Handy laden, Sachen für morgen bereitlegen, Wecker prüfen.
  - **Haus verlassen:** Schlüssel, Portemonnaie, Handy, Kopfhörer, Herd/Licht.
- **Abhaken:** Grosse Abhak-Zeilen (Touchziel ≥ 48 px). Der Fortschritt steht als
  Balken, nicht als Prozentzahl.
- **„Haus verlassen“** ist von der Startansicht mit einem Tipp erreichbar und
  setzt sich für jeden Durchgang neu zurück (mehrmals am Tag möglich: dann `routine_lauf`
  je Durchgang statt je Tag, Feld `durchgang`).
- **Push** um `erinnern_um`, nur wenn die Routine an dem Tag noch nicht fertig ist.

### 5.4 Papierkram & Fristen

- **Eintrag** mit Kategorie, Frist, optional Betrag und **immer einem nächsten Schritt**.
  Ohne nächsten Schritt lässt er sich nicht speichern. Das Feld schlägt vor:
  „Was ist der kleinste erste Handgriff?“
- **Ansicht** nach Frist sortiert. Die Wochengruppen heissen „diese Woche“,
  „nächste Woche“ und „später“.
- **Erledigt-Schritt:** Ein abgehakter Schritt fragt sofort nach dem nächsten
  („Gibt es noch einen Schritt?“). Erst *Nein, fertig* schliesst den Eintrag.
- **Push** `erinnern_tage_vorher` Tage vor der Frist und am Fristtag um 09:00. Inhalt:
  der nächste Schritt, nicht „Frist läuft ab“.

### 5.5 Zerleger

- **Ablauf:** Aus jedem Eintrag heraus *Zerlegen* wählen. Der Dienst fragt Claude nach
  5–8 Schritten von je höchstens 5 Minuten. Die Schritte erscheinen als Vorschlag, der
  Benutzer kann sie bearbeiten, streichen und umsortieren. Erst *Übernehmen* speichert
  sie als Kind-Einträge (`eltern_id`).
- **Prompt (serverseitig, fest):**
  - Schritte als JSON-Liste `[{ "text": "...", "min": 3 }]` liefern.
  - Jeder Schritt beginnt mit einem Verb, der erste ist trivial klein („Ordner öffnen“).
  - Keine Ratschläge, keine Diagnosen, keine Moral.
  - Den Eintragstext als Daten übergeben, nicht als Anweisung.
- **Absicherung:**
  - Höchstens 1 Aufruf pro 10 s.
  - Antwort gegen ein Schema prüfen und auf maximal 12 Schritte begrenzen.
  - Zeitlimit 20 s.
  - Bei Fehler oder fehlendem Schlüssel: „Zerleger gerade nicht erreichbar, Schritte
    selbst eintragen“ mit leerer Schrittliste.
- **Datenschutz:** Gesendet wird nur der Eintragstext. Die Einstellungsseite sagt das
  offen.

### 5.6 Fokus-Timer

- **Start** aus der Jetzt-Karte oder frei. Dauer per Schnellwahl 5/15/25/45 min oder
  frei.
- **Anzeige** als grosser schrumpfender Ring mit Restzeit und dem Eintragstext. Am
  Desktop bleibt die Restzeit im Tab-Titel sichtbar (`12:34 · Kompass`).
- **Ende:** Ton (abschaltbar) und Push, dann die Frage *Erledigt*, *Noch 10 min* oder
  *Pause*.
- **Hyperfokus-Schutz:** Nach 90 min am Stück (Summe über Verlängerungen) kommt ein Push
  „Trinken, aufstehen, kurz raus?“. Die Schwelle ist einstellbar.
- **Gerätewechsel:** Der laufende Timer liegt auf dem Server und erscheint auf allen
  Geräten.

### 5.7 Einstellungen

- **Eingaben:** iCal-Adresse, ntfy-Thema, Claude-Schlüssel, Push an/aus je Art, Ruhezeiten
  (Standard 21:30–07:00, keine Pushes), Hyperfokus-Schwelle, Ton.
- **Geheimnisse anzeigen:** Gespeicherte Geheimnisse erscheinen nur maskiert
  (`…xyz9`) und lassen sich nur ersetzen, nie anzeigen.
- **Test:** Knopf „Test-Push senden“.

## 6. Anbindungen

| Dienst | Weg | Regeln |
|---|---|---|
| Google-Kalender | iCal-Adresse, Abruf alle 10 min im Hintergrund-Thread, `recurring-ical-events` für 14 Tage voraus | Adresse nie loggen; Fehler nur als „Kalender nicht erreichbar seit …“ in der Oberfläche |
| ntfy | `POST http://192.168.178.113:8086/<thema>` mit `Title`, `Priority`, `Click: https://kompass.aiscom.ch/…` | Thema zufällig und lang (`kompass-<24 Zeichen>`), steht nur in `config.json`; Ruhezeiten beachten; jeder Push mit Schlüssel in `gesendet` gegen Doppel |
| Claude | `anthropic`-SDK, Modell `claude-haiku-5-5`, `max_tokens` 600 | Schlüssel nur in `config.json` (600); Aufruf nur auf Knopfdruck, nie automatisch |

Hinweis zu ntfy: Das Handy braucht die ntfy-App mit dem Server
`https://ntfy…` bzw. im Heimnetz `http://192.168.178.113:8086`. Ist ntfy von aussen
nicht freigegeben, kommen die Pushes nur im Heimnetz oder über Tailscale an. Das vor
Stufe 1 klären (Abschnitt 11).

## 7. Oberfläche (Handy und Desktop gleichwertig)

- **Ein Layout, zwei Anordnungen:**
  - **Handy:** einspaltig, unten eine Leiste mit 4 Zielen (Heute, Eingang, Routinen,
    Papier); Zerleger und Timer öffnen sich als Vollbild.
  - **Desktop ab 900 px:** links eine Seitenleiste, in der Mitte Heute, rechts Eingang
    bzw. Timer.
- **Tastatur am Desktop:** `N` erfassen, `1–4` Bereiche, im Eingang die Tasten aus 5.2,
  `Leertaste` Timer start/pause, `?` zeigt die Kürzel.
- **Lesbarkeit:** Schrift ≥ 16 px, Touchziele ≥ 48 px, viel Abstand, ruhige Farben, ein
  Akzent. Hell und dunkel nach Systemeinstellung, umschaltbar.
- **Bewegung:** `prefers-reduced-motion` respektieren, keine blinkenden Elemente, Erfolg
  als kurze ruhige Bestätigung.
- **Sprache:** Deutsch (Schweizer Schreibweise, ss statt ß), du-Form, kurze Sätze.
- **Kein Framework:** Vanilla-JS-Module unter `static/`, `fetch` gegen eine JSON-API
  unter `/api/…`; Seiten-Rohbau per Jinja-Template.

## 8. Sicherheit

- **Login:** `admin`, Passwort-Hash (werkzeug scrypt).
  - Das Erst-Passwort steht in `data/admin-passwort.txt` (600). Die Datei verschwindet
    beim ersten Passwortwechsel (wie `bildervon`).
- **Sitzung:** Cookie `Secure`, `HttpOnly`, `SameSite=Lax`, 30 Tage.
- **Schutz der Anfragen:** CSRF-Token auf jedem POST, JSON-API nur mit `Content-Type:
  application/json`.
- **Login-Sperre:** 5 Fehlversuche je IP in 10 min, die echte IP aus `X-Real-IP` (setzt
  NPM selbst).
- **Kopfzeilen:**
  - `Content-Security-Policy` ohne `unsafe-inline` für Skripte.
  - `Referrer-Policy: same-origin`, `X-Content-Type-Options`.
- **Logs:** keine Inhalte von Einträgen, keine Geheimnisse, keine iCal-Adresse.
- **Sicherung:** täglich um 03:30 `sqlite3 .backup` nach `data/sicherung/kompass-JJJJMMTT.db`,
  die letzten 14 Tage bleiben. Zusätzlich `data/` in die bestehende Sicherung von
  cDocker aufnehmen (prüfen, wie `bildervon` gesichert wird).

## 9. Ausbaustufen

| Stufe | Inhalt |
|---|---|
| **1** | Abschnitte 3–8: Heute, Eingang, Routinen, Papierkram, Zerleger, Fokus-Timer, Kalender lesen, ntfy, Login, PWA, öffentlicher Zugang |
| **2** | Wochenrückblick (Sonntagabend-Push: „Diese Woche geschafft: …“), Kurz-Check-in (Energie/Schlaf/Stimmung 1–5, Verlauf als ruhige Linie), Haushalt rollierend („zuletzt vor X Tagen“), Offline-Erfassung (Warteschlange im Browser, Abgleich beim nächsten Kontakt) |
| **3** | Impuls-Parkplatz (48 h parken, dann entscheiden), „Wo ist was“, Erfassung per Sprachnotiz (Mac/Qwen oder Whisper), Erfassung über einen Messenger-Bot |

Was in Stufe 2/3 steht, wird in Stufe 1 **nicht** vorbereitet (kein leeres Menü, keine
toten Tabellen). Ausnahme: Das Datenmodell darf Felder enthalten, die eine spätere Stufe
ohne Migration braucht, wenn sie heute schon sinnvoll sind.

## 10. Arbeitsanweisungen für Claude Code

**Vor dem Start**

1. `CLAUDE.md` des Workspaces befolgen: Router, `gelernte-fehler.md`, `harness.md`.
2. Dann `git status` im Repo `Ai`.
3. Vorlage lesen: `Ai/bildervon/` (Aufbau, Login, CSRF, Docker, README-Stil).

**Reihenfolge innerhalb von Stufe 1**

Jeder Schritt mit Tests und eigenem Commit.

1. Gerüst:
   - Flask-App mit Login, CSRF, Kopfzeilen und Migrationen
   - `/gesund`
   - Docker und `compose.yaml`
   - leere Heute-Seite
2. Eingang und Erfassen (Feld oben, API, Liste, Einsortieren).
3. Heute mit Hauptaufgaben, Jetzt-Karte und „Heute neu entscheiden“.
4. Routinen mit Vorlagen.
5. Papierkram & Fristen.
6. Fokus-Timer, serverseitig mit Hyperfokus-Schwelle.
7. Hintergrund-Thread: Kalender holen, Erinnerungen ausrechnen und per ntfy senden,
   Ruhezeiten.
8. Zerleger mit Claude.
9. PWA (Manifest, Icons, Service Worker für die Hülle).
10. Einstellungen.
11. Einbau auf cDocker, danach öffentlicher Zugang (Abschnitt 3).

**Tests (`tests/`, pytest, ohne Netz)**

- **Unit-Tests:**
  - Auswahl der Jetzt-Karte
  - Fristen und Erinnerungszeitpunkte inklusive Ruhezeiten und Sommerzeitwechsel
  - Wiederholungstermine aus einer iCal-Testdatei
  - Prüfung der Claude-Antwort (gültig, kaputt, zu lang)
  - Doppel-Push-Sperre
- **API-Tests:** mit Flasks Test-Client auf einer temporären DB. Tests lesen nie
  `data/` der echten Installation.
- **Externe Dienste** (Claude, ntfy, iCal) werden in Tests durch Attrappen ersetzt.
- **Oberfläche:** Jede neue Ansicht wird gerendert geprüft, mit Bildschirmfoto bei
  390×844 (Handy) und 1440×900 (Desktop), hell und dunkel. Keine waagrechte
  Scrollleiste am Handy.

**Regeln**

- **Geheimnisse:** nie in Repo, Logs, Chat oder Ansage. `data/` steht in `.gitignore`.
- **Kalender:** Den echten Kalender nur lesen, nie schreiben.
- **Pushes:** Testpushes nur an das eigene Thema, nach Rückfrage.
- **Firewall/OPNsense:** nur mit ausdrücklicher Freigabe; für diesen Plan nicht nötig.
- **`app.py`:** Wächst die Datei über ~600 Zeilen, das Aufteilen in ein Paket ansprechen,
  nicht stillschweigend weiterbauen.
- **Doku:**
  - `README.md` des Projekts (Betrieb, Daten, Aktualisieren, Sicherheit) pflegen.
  - `instructions/homelab/service-inventory.md` nachführen.
  - `instructions/baum.md` nachführen (neue README.md und PLAN.md).
  - `instructions/homelab/reverse-proxy-dns.md` nachführen (Host 45).

**Definition of Done (je Stufe)**

- Alle Tests grün, gerendert geprüft auf Handy und Desktop.
- Öffentlich erreichbar und mehrmals abgerufen (Statuscode, `x-served-by`, Zertifikat),
  ohne Login nur `/login`.
- Ein echter Push kam auf dem Handy an, und der Benutzer hat das bestätigt.
- Doku und Commit erledigt, Ansage per `ai_announce.py`.

## 11. Vom Benutzer vor dem Bau zu klären oder zu liefern

1. ~~CNAME~~ erledigt: `kompass.aiscom.ch` ist aktiv.
2. **iCal-Adresse:** Google Kalender → Einstellungen → Kalender → „Privatadresse im
   iCal-Format“. Später in den Einstellungen des Dashboards eintragen, nicht im Chat.
3. **Claude-API-Schlüssel** für den Zerleger, ebenfalls nur in den Einstellungen.
4. **ntfy von unterwegs:** Ist ntfy (cDocker :8086) von aussen erreichbar? Wenn nicht,
   kommen Pushes nur im Heimnetz oder über Tailscale an. Entscheiden: Tailscale auf dem
   Handy genügt, oder ntfy über NPM freigeben (eigener Schritt, eigener Plan).
5. **Routinen-Vorlagen:** passen die Vorschläge aus 5.3, oder gibt es eigene?
