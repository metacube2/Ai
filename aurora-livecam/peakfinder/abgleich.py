#!/usr/bin/env python3
"""Peakfinder-Abgleich fuer cAurora: misst, wie weit das aktuelle Kamerabild
gegenueber dem Referenzbild der Kalibrierung verschoben ist, und schreibt den
Versatz fuer die Webseite.

Die Kamera bleibt nicht exakt stehen: zwischen 21. und 28.10.2026 sprang das Bild
um bis zu 40 px (bei 3072 x 1728), dazu kommen wenige Pixel Tagesgang. Die
Gipfelmarken der Webseite sind fuer das Referenzbild berechnet; der Versatz hier
verschiebt sie auf die aktuelle Lage.

Messung: Kantenbild (Sobel-Betrag) des Bandes Zeile 540..840 der auf 1536 x 864
verkleinerten Bilder, Phasenkorrelation mit Hanning-Fenster. Vorzeichen: Inhalt des
aktuellen Bilds = Referenz + (dx, dy). Gemessen nur bei Tageslicht (Sonnenhoehe
> 3 Grad); uebernommen wird ein neuer Wert erst, wenn drei Messungen hintereinander
auf +-3 px uebereinstimmen. Nachts und bei Nebel bleibt der letzte gute Wert stehen.

Aufruf (Dienst caurora-peakfinder.timer, alle 5 Minuten):
    caurora-peakfinder-abgleich [--bild /var/www/html/image/current.jpg]
Kalibrierung und Tests nutzen ``versatz()`` und ``sonne()`` aus dieser Datei.
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import math
import os
import sys
from pathlib import Path

ZUSTAND = Path("/var/lib/caurora-peakfinder/zustand.json")
REFERENZ = Path("/var/lib/caurora-peakfinder/referenz_band.png")
AUSGABE = Path("/var/www/html/peakfinder-versatz.txt")
BILD = Path("/var/www/html/image/current.jpg")
STANDORT = (47.27082, 8.87238)   # Kamerastandort aus der Kalibrierung (nur fuer Tag/Nacht)
BAND = (540, 840)          # Zeilen im 1536 x 864-Bild
MAX_VERSATZ = 120.0        # px bei 3072; groesser = Messung unbrauchbar
TOLERANZ = 3.0             # px, drei Messungen muessen so nah beieinander liegen


def band_grau(pfad):
    """Graustufen-Band (1536 x 300, uint8) wie fuer Referenz und Messung benutzt."""
    import cv2

    im = cv2.imread(str(pfad), cv2.IMREAD_GRAYSCALE)
    if im is None:
        raise FileNotFoundError(pfad)
    im = cv2.resize(im, (1536, 864), interpolation=cv2.INTER_AREA)
    return im[BAND[0]:BAND[1]]


def _kanten(band):
    import cv2
    import numpy as np

    a = band.astype(np.float32)
    return np.sqrt(cv2.Sobel(a, cv2.CV_32F, 1, 0) ** 2 + cv2.Sobel(a, cv2.CV_32F, 0, 1) ** 2)


def versatz(bild, referenz_band):
    """(dx, dy, antwort) in Pixeln des 3072er-Bilds; referenz_band = Graustufen-Band."""
    import cv2

    fenster = cv2.createHanningWindow((1536, BAND[1] - BAND[0]), cv2.CV_32F)
    (dx, dy), r = cv2.phaseCorrelate(_kanten(referenz_band), _kanten(band_grau(bild)), fenster)
    return dx * 2, dy * 2, r


def sonne(t_utc: dt.datetime, lat: float, lon: float):
    """Sonnenazimut und -hoehe in Grad (NOAA-Naeherung, Hoehe mit Refraktion)."""
    jd = t_utc.timestamp() / 86400 + 2440587.5
    T = (jd - 2451545) / 36525
    L0 = (280.46646 + T * (36000.76983 + 0.0003032 * T)) % 360
    M = math.radians(357.52911 + T * (35999.05029 - 0.0001537 * T))
    e = 0.016708634 - T * (0.000042037 + 0.0000001267 * T)
    C = (math.sin(M) * (1.914602 - T * (0.004817 + 0.000014 * T))
         + math.sin(2 * M) * (0.019993 - 0.000101 * T) + math.sin(3 * M) * 0.000289)
    om = math.radians(125.04 - 1934.136 * T)
    lam = math.radians(L0 + C - 0.00569 - 0.00478 * math.sin(om))
    eps0 = 23 + (26 + (21.448 - T * (46.815 + T * (0.00059 - T * 0.001813))) / 60) / 60
    eps = math.radians(eps0 + 0.00256 * math.cos(om))
    dek = math.asin(math.sin(eps) * math.sin(lam))
    y = math.tan(eps / 2) ** 2
    L0r = math.radians(L0)
    zgl = 4 * math.degrees(y * math.sin(2 * L0r) - 2 * e * math.sin(M)
                           + 4 * e * y * math.sin(M) * math.cos(2 * L0r)
                           - 0.5 * y * y * math.sin(4 * L0r) - 1.25 * e * e * math.sin(2 * M))
    minuten = t_utc.hour * 60 + t_utc.minute + t_utc.second / 60
    ws = math.radians(((minuten + zgl + 4 * lon) % 1440) / 4 - 180)
    br = math.radians(lat)
    cz = math.sin(br) * math.sin(dek) + math.cos(br) * math.cos(dek) * math.cos(ws)
    el = 90 - math.degrees(math.acos(max(-1.0, min(1.0, cz))))
    az = (math.degrees(math.atan2(math.sin(ws), math.cos(ws) * math.sin(br)
                                  - math.tan(dek) * math.cos(br))) + 180) % 360
    if el > -0.575:
        te = math.tan(math.radians(el))
        r = (58.1 / te - 0.07 / te ** 3 + 0.000086 / te ** 5) if el > 5 else \
            (1735 + el * (-518.2 + el * (103.4 + el * (-12.79 + el * 0.711))))
        el += r / 3600
    return az, el


def _schreiben(pfad: Path, text: str):
    tmp = pfad.with_name(pfad.name + ".tmp")
    tmp.write_text(text)
    os.chmod(tmp, 0o644)
    os.replace(tmp, pfad)


def lauf(bild: Path, referenz: Path, zustand: Path, ausgabe: Path, jetzt=None) -> dict:
    import cv2

    jetzt = jetzt or dt.datetime.now(dt.timezone.utc)
    z = {"messungen": [], "gueltig": None}
    if zustand.exists():
        try:
            z = json.loads(zustand.read_text())
        except ValueError:
            pass
    _, sonnenhoehe = sonne(jetzt, *STANDORT)
    info = {"zeit": jetzt.isoformat(timespec="seconds"), "sonnenhoehe": round(sonnenhoehe, 1)}
    if sonnenhoehe < 3:
        info["ergebnis"] = "nacht"
    else:
        ref = cv2.imread(str(referenz), cv2.IMREAD_GRAYSCALE)
        dx, dy, r = versatz(bild, ref)
        info.update(dx=round(dx, 1), dy=round(dy, 1), antwort=round(r, 3))
        if max(abs(dx), abs(dy)) > MAX_VERSATZ:
            info["ergebnis"] = "unplausibel"
        else:
            z["messungen"] = (z.get("messungen") or [])[-2:] + [[dx, dy]]
            m = z["messungen"]
            if len(m) == 3 and all(abs(a[0] - m[-1][0]) <= TOLERANZ and abs(a[1] - m[-1][1]) <= TOLERANZ for a in m):
                mx = sorted(a[0] for a in m)[1]
                my = sorted(a[1] for a in m)[1]
                z["gueltig"] = {"dx": round(mx, 1), "dy": round(my, 1), "zeit": info["zeit"]}
                info["ergebnis"] = "uebernommen"
            else:
                info["ergebnis"] = "wartet auf stabile Messung"
    z["letzte"] = info
    zustand.parent.mkdir(parents=True, exist_ok=True)
    _schreiben(zustand, json.dumps(z))
    if z.get("gueltig"):
        g = z["gueltig"]
        _schreiben(ausgabe, json.dumps({"dx": g["dx"], "dy": g["dy"], "breite": 3072, "hoehe": 1728,
                                        "gemessen": g["zeit"], "geprueft": info["zeit"]}) + "\n")
    return info


def main(argv=None):
    a = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    a.add_argument("--bild", type=Path, default=BILD)
    a.add_argument("--referenz", type=Path, default=REFERENZ)
    a.add_argument("--zustand", type=Path, default=ZUSTAND)
    a.add_argument("--ausgabe", type=Path, default=AUSGABE)
    o = a.parse_args(argv)
    info = lauf(o.bild, o.referenz, o.zustand, o.ausgabe)
    print(json.dumps(info, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    sys.exit(main())
