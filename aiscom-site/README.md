---
title: AISCOM-Webseite (www.aiscom.ch) – Quellcode und Design
topic: web.aiscom
tags: [aiscom, website, nextjs, tailwind, css-animation, svg, macyaesu, kamera-uploader]
status: current
updated: 2026-10-04
---

# AISCOM-Webseite

Firmenseite AISCOM (IT-Dienstleistungen und 3D-Druck, Zürich) mit dem Bereich
„Projekte im Verkauf“ (MacYaesu, Kamera-Uploader). Live: <https://www.aiscom.ch>.

Betrieb, Server, Deploy und Sicherheit sind bewusst nicht in diesem
öffentlichen Repo dokumentiert, sondern im privaten Doku-Repo
(`instructions/homelab/aiscom-website.md`).

## Aufbau

| Ordner | Inhalt |
|---|---|
| `frontend/` | Next.js 14 (App Router), Tailwind, de/en/it, SQLite für Kontakt/Upload |
| `frontend/locales/` | Alle öffentlichen Texte, inkl. Produktkarten (`products.items`) |
| `frontend/components/visual/` | Grafik- und Animationsbausteine (siehe unten) |
| `deploy/kamera-uploader/` | Statische Platzhalterseite `/kamera-uploader/` |
| `deploy/` | Einmaliger Umstieg 2026-10-04 (Dienst, Proxy, Sitemap), `pruefen.sh` |

## Design

Nur CSS und SVG – keine Bilddateien, keine KI-Bilder, scharf in jeder Auflösung.

- `HeroPrinter`: stilisierter 3D-Drucker mit Materialstation, druckt eine Vase
  Schicht für Schicht (Spulen drehen, Kopf fährt und steigt, Fortschrittsbalken).
- `CodeWindow`: Editorfenster, C#- und ABAP-Zeilen werden „getippt“.
- `HeroBackdrop`: Aurora-Verläufe, Sterne, perspektivisches Blueprint-Raster.
- `Marquee`: Laufband mit Technologien; `Wave`: Abschnittsübergänge.
- `ProductVisuals`: Funkgerät mit Frequenzanzeige, S-Meter und VFO-Knopf
  (MacYaesu); Objektiv mit Upload-Balken (Kamera-Uploader).
- `PageHeader`: einheitlicher Kopf der Unterseiten; `Reveal`: Einblenden beim Scrollen.

Animationen stehen in `frontend/app/globals.css`. Bei „weniger Bewegung“ im
System stehen sie still; ohne JavaScript bleibt alles sichtbar.

## Lokal entwickeln

```bash
cd frontend
npm ci --ignore-scripts --legacy-peer-deps   # eslint-config-next 16 neben eslint 8
./node_modules/.bin/next build && ./node_modules/.bin/next start -p 3055
```

- Der Betriebs-Build läuft auf dem Server (natives `better-sqlite3`); ein
  Mac-Build ist nur ein Test.
- Einen alten Testserver über den Port beenden (`lsof -ti tcp:3055 | xargs kill`);
  `pkill -f "next start"` trifft den Prozess `next-server` nicht.
- `deploy/pruefen.sh` prüft die Live-Seite am Inhalt.

## Kamera-Uploader

Noch Platzhalter. Wenn die Unterlagen da sind: `deploy/kamera-uploader/index.html`
zur Produktseite ausbauen, Texte in `frontend/locales/*.json` (`products.items`)
anpassen, optional `CameraVisual` am echten Produkt ausrichten.
