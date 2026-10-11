# Bilder von uns (bildervon.ingok.ch)

Linkseite fuer Familienfotos. Jede Familie meldet sich mit eigenem Login an und
sieht nur die Anlaesse, fuer die der Admin sie freigegeben hat. Ein Anlass hat
Jahr, Titel, optional Datum und Beschreibung sowie Links zu Nextcloud, Immich,
PhotoPrism oder anderen https-Seiten. Familien waehlen oben das Jahr.
Ersetzt den alten PHP-Prototyp `Ai/familyalbums` (am 2026-10-11 entfernt; nur
Demo-Daten, nie in Betrieb).

## Betrieb

- cDocker (LXC 100), Stack `/opt/stacks/bildervon`, Container `bildervon`,
  Port `8095` → 8000 (gunicorn, ein Worker).
- Daten: `/opt/stacks/bildervon/data/` (`bildervon.db` SQLite, `secret.key`
  fuer Sitzungen, beim Erststart `admin-passwort.txt`, root 600). Die Datei
  verschwindet, sobald der Admin sein Passwort in der Oberflaeche aendert.
- Oeffentlich: `https://bildervon.ingok.ch` ueber NPM (cNginx) mit
  Let's-Encrypt; intern Unbound-Override `bildervon.ingok.ch → 192.168.178.111`.
- Aktualisieren: Dateien nach `/opt/stacks/bildervon` kopieren (ohne `data/`),
  `docker compose up -d --build`.

```bash
ssh root@192.168.178.113 'cat /opt/stacks/bildervon/data/admin-passwort.txt'
```

## Rollen

- **Admin** (`admin`): legt Familien (Name, Login, Passwort) und Anlaesse an,
  traegt Links ein und hakt an, welche Familien einen Anlass sehen.
- **Familie**: sieht nur freigegebene Anlaesse, Jahr fuer Jahr.

## Sicherheit

- Passwoerter als Werkzeug-Hash (scrypt), Familie min. 8, Admin min. 12 Zeichen.
- CSRF-Token auf jedem POST, Sitzungscookie `Secure`/`HttpOnly`/`SameSite=Lax`,
  30 Tage gueltig. `BILDERVON_COOKIE_SECURE=0` nur fuer lokale Tests ohne https.
- Login-Sperre: 5 Fehlversuche je IP in 10 Minuten (im Speicher, darum ein Worker).
- Nur `https://`-Links; `noindex`, CSP ohne fremde Quellen, kein Einbetten.
- Die Links selbst sind Freigabe-Links der Fotodienste: Wer einen Link kennt,
  kommt auch ohne diese Seite an die Bilder. Fuer Heikles in Nextcloud/Immich
  zusaetzlich ein Freigabe-Passwort setzen.

## Uebernommene Freigaben (2026-10-11)

Immich und PhotoPrism hatten keine oeffentlichen Freigaben. Aus Nextcloud
uebernommen (noch fuer keine Familie freigegeben):

- 2024 „See Juli Harry“ → Ordner-Freigabe (es gibt zwei Links auf denselben
  Ordner; uebernommen `xHngcjePjpzsfo7`).
- 2024 „Familienfilme Super 8“ → Datei `Familienfilme Super8.mp4`
  (Freigabe vom Mai 2024, Jahr der Filme unbekannt).
