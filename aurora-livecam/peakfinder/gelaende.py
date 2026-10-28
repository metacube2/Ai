"""Gelaendemodell, Gipfel aus OpenStreetMap und Sichtrechnung fuer den Peakfinder.

Hoehen: Terrarium-Kacheln (AWS Open Data „elevation-tiles-prod“, Quelle u. a.
SRTM, EU-DEM, swisstopo), zwischengespeichert unter ``cache/``.
Gipfel: Overpass (natural=peak mit Namen), ebenfalls zwischengespeichert.

Sichtrechnung: scheinbare Hoehe eines Punkts in Distanz d mit Erdkruemmung und
Refraktion k: dz = h - h_kamera - d^2 / (2 R) * (1 - k).
"""
from __future__ import annotations

import io
import json
import math
import os
import time
import urllib.parse
import urllib.request
from pathlib import Path

import numpy as np
from PIL import Image

ERDRADIUS = 6_371_000.0
REFRAKTION = 0.13
KACHEL_URL = "https://s3.amazonaws.com/elevation-tiles-prod/terrarium/{z}/{x}/{y}.png"
OVERPASS = [
    "https://overpass-api.de/api/interpreter",
    "https://overpass.kumi.systems/api/interpreter",
    "https://overpass.private.coffee/api/interpreter",
]
AGENT = "caurora-peakfinder/1.0 (aurora-weather-livecam.com)"


def _hole(url: str, daten: bytes | None = None, zeit: int = 120) -> bytes:
    req = urllib.request.Request(url, data=daten, headers={"User-Agent": AGENT})
    with urllib.request.urlopen(req, timeout=zeit) as r:
        return r.read()


# ---------------------------------------------------------------- Kacheln

def _kachel_xy(lat: float, lon: float, z: int) -> tuple[float, float]:
    n = 2 ** z
    x = (lon + 180.0) / 360.0 * n
    y = (1.0 - math.asinh(math.tan(math.radians(lat))) / math.pi) / 2.0 * n
    return x, y


class Gelaende:
    """Hoehenabfrage ueber Terrarium-Kacheln einer Zoomstufe (bilinear)."""

    def __init__(self, cache: Path, z: int):
        self.cache = Path(cache) / f"terrarium{z}"
        self.cache.mkdir(parents=True, exist_ok=True)
        self.z = z
        self.kacheln: dict[tuple[int, int], np.ndarray] = {}

    def kachel(self, x: int, y: int) -> np.ndarray:
        k = self.kacheln.get((x, y))
        if k is not None:
            return k
        datei = self.cache / f"{x}_{y}.png"
        if not datei.exists():
            roh = _hole(KACHEL_URL.format(z=self.z, x=x, y=y), zeit=60)
            datei.write_bytes(roh)
        a = np.asarray(Image.open(datei).convert("RGB"), dtype=np.float64)
        k = a[..., 0] * 256 + a[..., 1] + a[..., 2] / 256 - 32768
        self.kacheln[(x, y)] = k
        return k

    def vorladen(self, lat0, lat1, lon0, lon1):
        x0, y0 = _kachel_xy(lat1, lon0, self.z)
        x1, y1 = _kachel_xy(lat0, lon1, self.z)
        for x in range(int(x0), int(x1) + 1):
            for y in range(int(y0), int(y1) + 1):
                self.kachel(x, y)

    def hoehe(self, lat, lon) -> np.ndarray:
        lat = np.atleast_1d(np.asarray(lat, dtype=np.float64))
        lon = np.atleast_1d(np.asarray(lon, dtype=np.float64))
        n = 2 ** self.z
        fx = (lon + 180.0) / 360.0 * n * 256 - 0.5
        fy = (1.0 - np.arcsinh(np.tan(np.radians(lat))) / np.pi) / 2.0 * n * 256 - 0.5
        ix = np.floor(fx).astype(np.int64)
        iy = np.floor(fy).astype(np.int64)
        tx = fx - ix
        ty = fy - iy
        erg = np.zeros_like(fx)
        werte = {}
        for dx in (0, 1):
            for dy in (0, 1):
                px = ix + dx
                py = iy + dy
                kx = px // 256
                ky = py // 256
                v = np.empty_like(fx)
                for key in set(zip(kx.tolist(), ky.tolist())):
                    m = (kx == key[0]) & (ky == key[1])
                    t = self.kachel(*key)
                    v[m] = t[py[m] - key[1] * 256, px[m] - key[0] * 256]
                werte[(dx, dy)] = v
        erg = (werte[(0, 0)] * (1 - tx) * (1 - ty) + werte[(1, 0)] * tx * (1 - ty)
               + werte[(0, 1)] * (1 - tx) * ty + werte[(1, 1)] * tx * ty)
        return erg


# ---------------------------------------------------------------- Geometrie

def ziel(lat, lon, az_grad, dist_m):
    """Punkt in Distanz und Azimut (Kugel)."""
    p1 = math.radians(lat)
    l1 = math.radians(lon)
    a = np.radians(az_grad)
    d = np.asarray(dist_m) / ERDRADIUS
    p2 = np.arcsin(np.sin(p1) * np.cos(d) + np.cos(p1) * np.sin(d) * np.cos(a))
    l2 = l1 + np.arctan2(np.sin(a) * np.sin(d) * np.cos(p1), np.cos(d) - np.sin(p1) * np.sin(p2))
    return np.degrees(p2), np.degrees(l2)


def peilung(lat, lon, lat2, lon2):
    """Azimut (Grad) und Distanz (m) vom Standort zum Punkt."""
    p1, p2 = math.radians(lat), np.radians(lat2)
    dl = np.radians(lon2) - math.radians(lon)
    y = np.sin(dl) * np.cos(p2)
    x = math.cos(p1) * np.sin(p2) - math.sin(p1) * np.cos(p2) * np.cos(dl)
    az = (np.degrees(np.arctan2(y, x)) + 360) % 360
    h = np.sin((p2 - p1) / 2) ** 2 + math.cos(p1) * np.cos(p2) * np.sin(dl / 2) ** 2
    return az, 2 * ERDRADIUS * np.arcsin(np.sqrt(h))


def scheinbare_hoehe(dh, dist):
    """Elevationswinkel (Grad) fuer Hoehenunterschied dh in Distanz dist."""
    dist = np.maximum(dist, 1.0)
    return np.degrees(np.arctan2(dh - dist ** 2 / (2 * ERDRADIUS) * (1 - REFRAKTION), dist))


def distanzen(max_m=160_000):
    """Schrittweiten: nah fein, fern grob."""
    d = [30.0]
    while d[-1] < max_m:
        d.append(d[-1] + max(15.0, d[-1] * 0.004))
    return np.array(d)


class Sicht:
    """Sicht vom Standort. ``stufen``: Liste (bis_distanz_m, Gelaende), nah zuerst;
    nahes Gelaende aus feineren Kacheln, fernes aus groberen."""

    def __init__(self, lat, lon, h_kamera, stufen):
        self.lat, self.lon, self.h = lat, lon, h_kamera
        self.stufen = sorted(stufen, key=lambda s: s[0])

    def hoehe(self, lat, lon, dist):
        h = np.empty(len(dist))
        unten = 0.0
        for bis, gel in self.stufen:
            m = (dist >= unten) & (dist < bis)
            if m.any():
                h[m] = gel.hoehe(lat[m], lon[m])
            unten = bis
        m = dist >= unten
        if m.any():
            h[m] = self.stufen[-1][1].hoehe(lat[m], lon[m])
        return h

    def strahl(self, az, d):
        lat, lon = ziel(self.lat, self.lon, az, d)
        h = self.hoehe(lat, lon, d)
        return scheinbare_hoehe(h - self.h, d), h

    def horizont(self, azimute, max_m=160_000):
        """Horizontprofil: je Azimut hoechster Elevationswinkel und dessen Distanz."""
        d = distanzen(max_m)
        el = np.empty(len(azimute))
        dist = np.empty(len(azimute))
        for i, az in enumerate(azimute):
            e, _ = self.strahl(az, d)
            j = int(np.argmax(e))
            el[i], dist[i] = e[j], d[j]
        return el, dist

    def sichtbar(self, lat, lon, ele, toleranz_grad=0.05):
        """(sichtbar, az, el, dist, verdeckung) eines Gipfels. Der Gipfel zaehlt als
        sichtbar, wenn kein Gelaende davor hoeher erscheint (Toleranz fuer Raster)."""
        az, dist = peilung(self.lat, self.lon, lat, lon)
        az, dist = float(az), float(dist)
        el = float(scheinbare_hoehe(ele - self.h, dist))
        d = distanzen(dist)
        d = d[d < dist - max(150.0, dist * 0.01)]
        if len(d) == 0:
            return True, az, el, dist, -90.0
        e, _ = self.strahl(az, d)
        vor = float(e.max())
        return vor < el - toleranz_grad, az, el, dist, vor


# ---------------------------------------------------------------- Gipfel

def gipfel_holen(cache: Path, bbox=(46.3, 7.4, 47.7, 9.9)) -> list[dict]:
    """Benannte Gipfel (natural=peak) im Rechteck (sued, west, nord, ost)."""
    datei = Path(cache) / ("gipfel_%s.json" % "_".join(f"{v:.2f}" for v in bbox))
    if datei.exists():
        return json.loads(datei.read_text())
    q = ("[out:json][timeout:160];node[natural=peak][name](%s,%s,%s,%s);out body;" % bbox)
    daten = urllib.parse.urlencode({"data": q}).encode()
    letzter = None
    for versuch in range(6):
        for url in OVERPASS:
            try:
                j = json.loads(_hole(url, daten, zeit=200))
                if "elements" in j:
                    datei.write_text(json.dumps(j["elements"], ensure_ascii=False))
                    return j["elements"]
            except Exception as e:  # noqa: BLE001 - naechster Spiegel
                letzter = e
        time.sleep(10 * 2 ** versuch)
    raise RuntimeError(f"Overpass nicht erreichbar: {letzter}")


def ele_zahl(tags: dict) -> float | None:
    s = (tags.get("ele") or "").replace(",", ".").replace("m", "").strip()
    try:
        return float(s.split(";")[0])
    except ValueError:
        return None


def name_de(tags: dict) -> str:
    return tags.get("name:de") or tags.get("name") or "?"
