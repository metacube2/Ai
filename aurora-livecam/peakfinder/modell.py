"""Kameramodell fuer den Peakfinder: Drehung (Azimut, Neigung, Rollwinkel) plus
Fischaugen-Objektiv r = f * th * (1 + k1 th^2 + k2 th^4) (Kannala-Brandt, zwei Terme),
Parameter p = [gier, neigung, roll, f, k1, k2, dn, de, dh, cx, cy]; cx/cy = Lage der
optischen Achse gegenueber der Bildmitte in Pixeln (dn/de/dh = Standort, nur in der
Kalibrierung benutzt).
Koordinaten in Pixeln des Kamerabilds 3072 x 1728.
"""
from __future__ import annotations

import numpy as np

BREITE, HOEHE = 3072, 1728


def _achsen(gier, neigung):
    y, n = np.radians([gier, neigung])
    vor = np.array([np.cos(n) * np.sin(y), np.cos(n) * np.cos(y), np.sin(n)])
    rechts = np.array([np.cos(y), -np.sin(y), 0.0])
    oben = np.cross(rechts, vor)
    return vor, rechts, oben


def projektion(p, az, el):
    """Azimut/Elevation (Grad) -> Bildpunkt (x, y)."""
    gier, neigung, roll, f, k1, k2 = p[:6]
    az = np.radians(np.atleast_1d(az))
    el = np.radians(np.atleast_1d(el))
    v = np.stack([np.cos(el) * np.sin(az), np.cos(el) * np.cos(az), np.sin(el)], -1)
    vor, rechts, oben = _achsen(gier, neigung)
    xr, yo, zv = v @ rechts, v @ oben, v @ vor
    c, s = np.cos(np.radians(roll)), np.sin(np.radians(roll))
    xr, yo = c * xr + s * yo, -s * xr + c * yo
    th = np.arccos(np.clip(zv, -1, 1))
    phi = np.arctan2(yo, xr)
    rd = f * th * (1 + k1 * th ** 2 + k2 * th ** 4)
    cx = BREITE / 2 + (p[9] if len(p) > 9 else 0); cy = HOEHE / 2 + (p[10] if len(p) > 10 else 0)
    return cx + rd * np.cos(phi), cy - rd * np.sin(phi)


def rueckprojektion(p, x, y):
    """Bildpunkt -> Azimut/Elevation (Grad)."""
    gier, neigung, roll, f, k1, k2 = p[:6]
    dx = np.asarray(x, dtype=float) - BREITE / 2 - (p[9] if len(p) > 9 else 0)
    dy = HOEHE / 2 + (p[10] if len(p) > 10 else 0) - np.asarray(y, dtype=float)
    rd = np.hypot(dx, dy)
    phi = np.arctan2(dy, dx)
    th = rd / f
    for _ in range(20):
        g = f * th * (1 + k1 * th ** 2 + k2 * th ** 4) - rd
        th = th - g / (f * (1 + 3 * k1 * th ** 2 + 5 * k2 * th ** 4))
    c, s = np.cos(np.radians(roll)), np.sin(np.radians(roll))
    xr2, yo2, zv = np.sin(th) * np.cos(phi), np.sin(th) * np.sin(phi), np.cos(th)
    xr, yo = c * xr2 - s * yo2, s * xr2 + c * yo2
    vor, rechts, oben = _achsen(gier, neigung)
    v = xr[:, None] * rechts + yo[:, None] * oben + zv[:, None] * vor
    az = (np.degrees(np.arctan2(v[:, 0], v[:, 1])) + 360) % 360
    el = np.degrees(np.arcsin(np.clip(v[:, 2], -1, 1)))
    return az, el


def horizontlinie(bild_rgb: np.ndarray, oben=0.38, unten=0.75):
    """Horizont im Bild: je Spalte der erste Uebergang hell (Himmel) -> dunkel
    (Gelaende), gesucht von oben. Liefert y (float, NaN wo nichts gefunden)."""
    from scipy.ndimage import gaussian_filter

    L = gaussian_filter(bild_rgb.astype(np.float32).mean(axis=2), 1.0)
    H, W = L.shape
    mx = np.stack([L[k:H - 14 + k] for k in range(14)]).max(0)
    mx = np.vstack([mx, np.full((14, W), np.inf)])
    cs = np.vstack([np.zeros((1, W)), np.cumsum(L, 0)])
    ueber = np.zeros_like(L)
    ueber[22:] = (cs[20:H - 2] - cs[0:H - 22]) / 20.0
    y0, y1 = int(oben * H), int(unten * H)
    kante = np.full(W, np.nan)
    for x in range(W):
        c = (mx[y0:y1, x] < 0.70 * ueber[y0:y1, x]) & (ueber[y0:y1, x] > 105)
        i = int(np.argmax(c))
        if not c[i]:
            continue
        y = y0 + i
        j = int(np.argmin(np.diff(L[y - 6:y + 6, x])))
        kante[x] = y - 6 + j + 0.5
    return kante
