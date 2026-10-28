#!/usr/bin/env python3
"""Peakfinder-Kalibrierung fuer die Aurora-Livecam (cAurora).

Erzeugt aus Referenzbild, Passpunkten, Hoehenmodell und OpenStreetMap die
Gipfeldaten fuer die Webseite:

  1. Horizontlinie im Referenzbild finden (Himmel hell -> Gelaende dunkel).
  2. Kamerastandort, Blickrichtung, Neigung, Rollwinkel, Brennweite,
     Fischaugen-Verzeichnung und Lage der optischen Achse gemeinsam ausgleichen:
     Horizont des Hoehenmodells (Grate bis 35 km) gegen die Bildlinie, dazu die
     Passpunkte (Mythen), die Sonne beim Untergang am 22.10. und die Sonnenbahn
     vom 27.10. (Ephemeride, unabhaengig vom Hoehenmodell).
  3. Sichtbare Gipfel bestimmen (nichts davor hoeher als Gipfel - 0.15 Grad,
     Erdkruemmung und Refraktion k = 0.13), in Bildkoordinaten umrechnen.
  4. ``gipfel.json`` (hier) und ``../js/peakfinder-daten.js`` (Webseite) schreiben,
     ``referenz_band.png`` fuer den Abgleich auf dem Server.

Aufruf (mit einem Python mit numpy, scipy, pillow, opencv):
    python3 kalibrieren.py [--aktuell bild.jpg] [--pruefbild bild.jpg] [--ohne-mythen]

``--aktuell``: aktuelles Kamerabild; dessen Versatz wird Standardwert der Webseite
(falls der Abgleich-Dienst noch nichts geliefert hat).
``--ohne-mythen``: Ausgleich ohne die Mythen (Gegenprobe, nichts wird geschrieben).
Zwischenspeicher (Kacheln, OSM): ~/.cache/caurora-peakfinder.
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import sys
from pathlib import Path
from zoneinfo import ZoneInfo

import numpy as np
from PIL import Image
from scipy.optimize import least_squares

HIER = Path(__file__).resolve().parent
sys.path.insert(0, str(HIER))

import abgleich  # noqa: E402
import gelaende as g  # noqa: E402
from modell import BREITE, HOEHE, horizontlinie, projektion, rueckprojektion  # noqa: E402

CACHE = Path.home() / ".cache" / "caurora-peakfinder"
GRAT_MAX = 35_000.0      # Horizont-Ausgleich nur gegen Grate bis hier (ferne Alpen oft im Dunst)
SKYLINE_GEWICHT = 0.25   # Horizontlinie bestimmt v. a. die Hoehe, Passpunkte die Richtung
SICHT_RAND = 0.15        # Grad, um die ein Gipfel ueber dem Gelaende davor stehen muss
MIN_DIST = 8_000.0       # naeher: Standort zu unsicher (300 m Fehler = Grade)
RAND_UNSICHER = 300      # px links: Verzeichnung am Rand ungeprueft


def robust(r, f=3.0):
    return np.sign(r) * np.sqrt(2 * f * f * (np.sqrt(1 + (r / f) ** 2) - 1))


class Kalibrierung:
    def __init__(self, pp: dict):
        self.pp = pp
        self.lat0 = pp["standort_start"]["lat"]
        self.lon0 = pp["standort_start"]["lon"]
        self.g11 = g.Gelaende(CACHE, 11)
        self.g12 = g.Gelaende(CACHE, 12)
        self.g13 = g.Gelaende(CACHE, 13)
        self.osm = {e["id"]: e for e in g.gipfel_holen(CACHE)}
        bild = np.asarray(Image.open(HIER / pp["referenz"]).convert("RGB"))
        kante = horizontlinie(bild)
        xs = np.arange(BREITE)[~np.isnan(kante)][::4].astype(float)
        self.xs, self.ys = xs, kante[xs.astype(int)]
        self.kandidaten = self._grate()

    def stufen(self):
        return [(8000, self.g13), (60000, self.g12), (1e9, self.g11)]

    def _grate(self):
        """Gratpunkte (lat, lon, h, d) vom Startstandort: lokale Maxima der scheinbaren
        Hoehe entlang jedes Strahls, die hoechstens 0.8 Grad unter dem Horizont liegen."""
        h0 = float(self.g13.hoehe(self.lat0, self.lon0)[0]) + 2
        s = g.Sicht(self.lat0, self.lon0, h0, self.stufen())
        d = g.distanzen(130_000)
        d = d[d > 2500]
        teile = []
        for az in np.arange(165, 290, 0.04):
            e, h = s.strahl(az, d)
            lokal = np.r_[False, (e[1:-1] >= e[:-2]) & (e[1:-1] >= e[2:]), False]
            k = (e > e.max() - 0.8) & lokal
            la, lo = g.ziel(self.lat0, self.lon0, az, d[k])
            teile.append(np.stack([la, lo, h[k], d[k]], 1))
        k = np.concatenate(teile)
        return k[k[:, 3] < GRAT_MAX]

    def standort(self, q):
        la, lo = g.ziel(self.lat0, self.lon0, 0, q[0])
        la, lo = g.ziel(float(la), float(lo), 90, q[1])
        la, lo = float(la), float(lo)
        return la, lo, float(self.g13.hoehe(la, lo)[0]) + q[2]

    def huelle(self, la, lo, hc):
        az, d = g.peilung(la, lo, self.kandidaten[:, 0], self.kandidaten[:, 1])
        el = g.scheinbare_hoehe(self.kandidaten[:, 2] - hc, d)
        b = np.floor((az - 160) / 0.1).astype(int)
        env = np.full(1400, -9.0)
        ok = (b >= 0) & (b < 1400)
        np.maximum.at(env, b[ok], el[ok])
        return 160 + 0.1 * np.arange(1400) + 0.05, env

    def gipfel_richtung(self, osm_id, la, lo, hc):
        e = self.osm[osm_id]
        az, d = g.peilung(la, lo, e["lat"], e["lon"])
        return float(az), float(g.scheinbare_hoehe(g.ele_zahl(e["tags"]) - hc, d))

    def sonnenpunkte(self, mit_bahn=True, mit_abend=True):
        erg = []
        for s in (self.pp["sonne"] if mit_abend else []) + (self.pp.get("sonne_bahn", []) if mit_bahn else []):
            teile = s["bild"].split("_")
            t = dt.datetime.strptime(teile[1] + teile[2][:6], "%Y%m%d%H%M%S")
            t = t.replace(tzinfo=ZoneInfo("Europe/Zurich")).astimezone(dt.timezone.utc)
            az, el = abgleich.sonne(t, self.lat0, self.lon0)
            y = None if s["y"] is None else s["y"] - s["versatz"][1]
            erg.append((az, el, s["x"] - s["versatz"][0], y, *s["sigma"]))
        return erg

    def residuen(self, p, gipfel, sonne):
        la, lo, hc = self.standort(p[6:9])
        A, E = self.huelle(la, lo, hc)
        az, el = rueckprojektion(p, self.xs, self.ys)
        he = np.interp(az, A, E)
        he[he < -8] = np.nan
        r = [SKYLINE_GEWICHT * robust(np.nan_to_num((el - he) * p[3] * np.pi / 180, nan=60.0))]
        for gp in gipfel:
            a, e = self.gipfel_richtung(gp["osm"], la, lo, hc)
            px, py = projektion(p, a, e)
            r.append(3 / gp["sigma"] * np.array([px[0] - gp["x"], py[0] - gp["y"]]))
        for a, e, x, y, sx, sy in sonne:
            px, py = projektion(p, a, e)
            r.append(np.array([3 * (px[0] - x) / sx] + ([] if y is None else [3 * (py[0] - y) / sy])))
        return np.concatenate(r)

    def ausgleich(self, gipfel, mit_bahn=True, mit_abend=True):
        st = self.pp["start"]
        sonne = self.sonnenpunkte(mit_bahn, mit_abend)
        unten = [150, -20, -15, 800, -0.8, -0.8, -1500, -1500, 0.5, -200, -200]
        oben = [300, 25, 15, 5000, 0.8, 0.8, 1500, 1500, 60, 200, 200]
        bester = None
        for dn in (-300, 0, 300):
            for de in (-300, 0, 300):
                for k1 in (-0.25, -0.15):
                    p0 = [st["gier"], st["neigung"], st["roll"], st["f"], k1, 0, dn, de, 3, 0, 0]
                    s = least_squares(self.residuen, p0, args=(gipfel, sonne), bounds=(unten, oben),
                                      x_scale=[1, 1, 1, 100, 0.05, 0.05, 100, 100, 5, 20, 20])
                    if bester is None or s.cost < bester.cost:
                        bester = s
        return bester.x, sonne

    def bericht(self, p, gipfel_alle, sonne):
        la, lo, hc = self.standort(p[6:9])
        A, E = self.huelle(la, lo, hc)
        az, el = rueckprojektion(p, self.xs, self.ys)
        he = np.interp(az, A, E)
        ok = he > -8
        rh = np.abs((el - he)[ok]) * p[3] * np.pi / 180
        b = {"horizont_px_median": round(float(np.median(rh)), 2),
             "horizont_px_p90": round(float(np.percentile(rh, 90)), 2), "gipfel": [], "sonne": []}
        for gp in gipfel_alle:
            a, e = self.gipfel_richtung(gp["osm"], la, lo, hc)
            px, py = projektion(p, a, e)
            b["gipfel"].append({"name": gp["name"], "dx": round(float(px[0] - gp["x"]), 1),
                                "dy": round(float(py[0] - gp["y"]), 1)})
        for a, e, x, y, *_ in sonne:
            px, py = projektion(p, a, e)
            b["sonne"].append({"az": round(a, 2), "el": round(e, 2), "dx": round(float(px[0] - x), 1),
                               "dy": None if y is None else round(float(py[0] - y), 1)})
        return b

    def sichtbare_gipfel(self, p):
        la, lo, hc = self.standort(p[6:9])
        s = g.Sicht(la, lo, hc, self.stufen())
        az_l, _ = rueckprojektion(p, np.array([0.0]), np.array([HOEHE * 0.6]))
        az_r, _ = rueckprojektion(p, np.array([BREITE - 1.0]), np.array([HOEHE * 0.6]))
        bevorzugt = list(self.pp.get("bevorzugt", {}).get("namen", []))
        erg = []
        for e in self.osm.values():
            t = e.get("tags", {})
            ele = g.ele_zahl(t)
            if ele is None:
                continue
            az, d = g.peilung(la, lo, e["lat"], e["lon"])
            az, d = float(az), float(d)
            if not (az_l[0] - 1 <= az <= az_r[0] + 1) or not (MIN_DIST <= d <= 150_000):
                continue
            sicht, az, el, d, vor = s.sichtbar(e["lat"], e["lon"], ele, toleranz_grad=SICHT_RAND)
            if not sicht:
                continue
            x, y = projektion(p, az, el)
            x, y = float(x[0]), float(y[0])
            if not (0 <= x < BREITE and 0.3 * HOEHE <= y <= 0.9 * HOEHE):
                continue
            # Ueberstand ueber das Gelaende davor ohne den eigenen Bergstock (letzte 15 %)
            dd = g.distanzen(d * 0.85)
            vorne = float(s.strahl(az, dd)[0].max()) if len(dd) else -90.0
            erg.append({
                "name": g.name_de(t), "hoehe": round(ele), "dist_km": round(d / 1000, 1),
                "az": round(az, 2), "el": round(el, 3), "x": round(x, 1), "y": round(y, 1),
                # Rang fuer die Auswahl auf der Seite: wie weit der Gipfel ueber das Gelaende
                # davor ragt (sichtbare Spitze), dann Bekanntheit und Hoehe
                "rang": round(1000 * min(max(el - vorne, 0), 2) + ele / 10
                              + (300 if t.get("wikidata") or t.get("wikipedia") else 0)
                              + (2000 + 100 * (len(bevorzugt) - bevorzugt.index(g.name_de(t)))
                                 if g.name_de(t) in bevorzugt else 0)),
                "unsicher": x < RAND_UNSICHER, "osm": e["id"],
            })
        erg.sort(key=lambda o: -o["rang"])
        return erg


def schreiben(p, kal, gipfel, bericht, versatz_std, pfad_js: Path, pfad_json: Path):
    la, lo, hc = kal.standort(p[6:9])
    daten = {
        "stand": dt.date.today().isoformat(),
        "quelle": "OpenStreetMap (natural=peak, ODbL), Hoehenmodell Terrarium (AWS Open Data)",
        "standort": {"lat": round(la, 5), "lon": round(lo, 5), "hoehe_m": round(hc, 1)},
        "kamera": {"gier": round(p[0], 3), "neigung": round(p[1], 3), "roll": round(p[2], 3),
                   "f_px": round(p[3], 1), "k1": round(p[4], 4), "k2": round(p[5], 4),
                   "cx": round(p[9], 1), "cy": round(p[10], 1)},
        "bild": {"breite": BREITE, "hoehe": HOEHE, "referenz": kal.pp["referenz"]},
        "versatz_standard": {"dx": round(versatz_std[0], 1), "dy": round(versatz_std[1], 1)},
        "versatz_url": "peakfinder-versatz.txt",
        "genauigkeit": bericht,
        "gipfel": [{k: v for k, v in o.items() if k not in ("osm",)} for o in gipfel],
    }
    pfad_json.write_text(json.dumps(daten, ensure_ascii=False, indent=1) + "\n")
    kurz = dict(daten)
    kurz.pop("genauigkeit")
    pfad_js.write_text(
        "/* Peakfinder-Daten der Aurora-Livecam, erzeugt von aurora-livecam/peakfinder/kalibrieren.py. "
        "Nicht von Hand aendern. Gipfel: OpenStreetMap-Mitwirkende (ODbL). */\n"
        "window.AURORA_PEAKFINDER = " + json.dumps(kurz, ensure_ascii=False, separators=(",", ":")) + ";\n")


def main(argv=None):
    a = argparse.ArgumentParser(description="Peakfinder-Kalibrierung")
    a.add_argument("--aktuell", type=Path, help="aktuelles Kamerabild fuer den Standard-Versatz")
    a.add_argument("--pruefbild", type=Path, help="Bild zur Pruefung in anderer Kameralage")
    a.add_argument("--ohne-mythen", action="store_true", help="Gegenprobe ohne Mythen, schreibt nichts")
    a.add_argument("--ohne-bahn", action="store_true", help="Sonnenbahn 27.10. nicht verwenden")
    a.add_argument("--ohne-abend", action="store_true", help="Gegenprobe ohne Sonnenuntergang 22.10., schreibt nichts")
    a.add_argument("--js", type=Path, default=HIER.parent / "js" / "peakfinder-daten.js")
    o = a.parse_args(argv)

    pp = json.loads((HIER / "passpunkte.json").read_text())
    kal = Kalibrierung(pp)
    gipfel_fit = [] if o.ohne_mythen else pp["gipfel"]
    p, sonne = kal.ausgleich(gipfel_fit, not o.ohne_bahn, not o.ohne_abend)
    if o.ohne_abend:
        sonne = kal.sonnenpunkte(not o.ohne_bahn, True)
    la, lo, hc = kal.standort(p[6:9])
    print("Kamera: gier %.2f neigung %.2f roll %.2f f %.0f k1 %.3f k2 %.3f, Achse %+.0f/%+.0f px"
          % (*p[:6], p[9], p[10]))
    print("Standort: %.5f %.5f, %.1f m" % (la, lo, hc))
    bericht = kal.bericht(p, pp["gipfel"], sonne)
    bericht["passpunkte_im_ausgleich"] = [gp["name"] for gp in gipfel_fit] + ["Sonne x%d" % len(sonne)]
    print(json.dumps(bericht, ensure_ascii=False))
    if o.ohne_mythen or o.ohne_abend:
        return 0

    ref_band = abgleich.band_grau(HIER / pp["referenz"])
    Image.fromarray(ref_band).save(HIER / "referenz_band.png")
    versatz_std = (0.0, 0.0)
    if o.aktuell:
        dx, dy, r = abgleich.versatz(o.aktuell, ref_band)
        versatz_std = (dx, dy)
        print("Versatz aktuelles Bild: dx %.1f dy %.1f (Antwort %.2f)" % (dx, dy, r))
    if o.pruefbild:
        dx, dy, r = abgleich.versatz(o.pruefbild, ref_band)
        for pr in pp.get("pruefung", []):
            if pr["bild"] == o.pruefbild.name:
                gp = next(x for x in pp["gipfel"] if x["name"] == pr["gipfel"])
                a_, e_ = kal.gipfel_richtung(gp["osm"], la, lo, hc)
                px, py = projektion(p, a_, e_)
                bericht["pruefung"] = {"bild": pr["bild"], "versatz": [round(dx, 1), round(dy, 1)],
                                       "dx": round(float(px[0] + dx - pr["x"]), 1),
                                       "dy": round(float(py[0] + dy - pr["y"]), 1)}
                print("Pruefung", json.dumps(bericht["pruefung"], ensure_ascii=False))
    gipfel = kal.sichtbare_gipfel(p)
    print("Sichtbare Gipfel: %d (davon am linken Rand unsicher: %d)"
          % (len(gipfel), sum(o_["unsicher"] for o_ in gipfel)))
    schreiben(p, kal, gipfel, bericht, versatz_std, o.js, HIER / "gipfel.json")
    print("Geschrieben:", o.js, HIER / "gipfel.json", HIER / "referenz_band.png")
    return 0


if __name__ == "__main__":
    sys.exit(main())
