"""Live-Gesichtseffekte auf Basis der Gesichtspunkte des Matrix-Agenten.

Zeitversetztes Ich, Alterungs-Zeitraffer, Gesichts-Hologramm und Kubistisches Gesicht.
Alle vier verfolgen das Gesicht wie der Matrix-Agent (`bildeffekte.face_track`) und
rechnen nur in einem Ausschnitt um das Gesicht. Ohne Gesicht bleibt das Bild unverändert.
"""
import time
from collections import deque

import cv2
import numpy as np
from scipy.spatial import Delaunay

from bildeffekte import IEffectStrategy
from bildeffekte.face_morph import warp_mesh
from bildeffekte.face_track import (FaceGeometry, FaceTracker, face_crop, face_mask,
                                    template_points, template_triangles)
from bildeffekte.verwandlung import smooth


def _phase(t, keys):
    """Stückweise weiche Kurve aus (Zeit, Wert)-Paaren."""
    for (t0, a), (t1, b) in zip(keys, keys[1:]):
        if t0 <= t < t1:
            return float(a + (b - a) * smooth((t - t0) / max(t1 - t0, 1e-6)))
    return float(keys[-1][1])


class _FaceLoop(IEffectStrategy):
    """Gemeinsamer Ablauf: Gesicht verfolgen, Ausschnitt rendern, weich einsetzen."""

    category = 'Gesichter'
    MARGIN = 0.35      # Ausschnittrand in Gesichtsbreiten
    CYCLE = 10.0

    def __init__(self):
        self.clock = time.monotonic
        self.tracker = FaceTracker(clock=lambda: self.clock())

    def activate(self):
        self.tracker.reset()
        self._reset_state()

    def deactivate(self):
        self.tracker.reset()

    def _reset_state(self):
        pass

    def observe(self, small, points, now):
        """Wird für jedes Arbeitsbild aufgerufen, auch ohne Gesicht (points None)."""

    def render(self, crop, points, t):
        """Liefert (Bild, Alpha) für den Ausschnitt oder None für unverändert."""
        raise NotImplementedError

    def apply(self, frame, config):
        height, width = frame.shape[:2]
        size = self.tracker.working_size(frame)
        if min(size) < 64:
            return frame
        small = cv2.resize(frame, size, interpolation=cv2.INTER_AREA)
        points = self.tracker.update(small)
        now = self.clock()
        self.observe(small, points, now)
        if points is None:
            return frame
        box = face_crop(points, small.shape, self.MARGIN)
        if box is None:
            return frame
        x0, y0, x1, y1 = box
        rendered = self.render(small[y0:y1, x0:x1], np.float32(points - [x0, y0]),
                               (now - self.tracker.since) % self.CYCLE)
        if rendered is None:
            return frame
        image, alpha = rendered
        # Nur den Ausschnitt in voller Auflösung einsetzen; außerhalb der Maske bleibt das Original scharf.
        sx, sy = width / size[0], height / size[1]
        X0, Y0 = int(round(x0 * sx)), int(round(y0 * sy))
        X1, Y1 = min(width, int(round(x1 * sx))), min(height, int(round(y1 * sy)))
        if X1 - X0 < 2 or Y1 - Y0 < 2:
            return frame
        image = cv2.resize(np.float32(image), (X1 - X0, Y1 - Y0), interpolation=cv2.INTER_LINEAR)
        alpha = cv2.resize(np.float32(alpha), (X1 - X0, Y1 - Y0), interpolation=cv2.INTER_LINEAR)
        out = frame.copy()
        region = out[Y0:Y1, X0:X1].astype(np.float32)
        out[Y0:Y1, X0:X1] = np.clip(region + (image - region) * alpha[..., None], 0, 255).astype(np.uint8)
        return out


class ZeitversetztesIch(_FaceLoop):
    """Das eigene Gesicht von vor vier Sekunden sitzt auf dem Kopf von jetzt."""

    name = 'Zeitversetztes Ich'
    description = ('Dein Gesicht wird durch dein eigenes Gesicht von vor 4 Sekunden ersetzt, '
                   'passend auf die aktuelle Kopfhaltung verformt.')
    DELAY = 4.0

    def __init__(self):
        super().__init__()
        self.triangles = template_triangles()
        self._reset_state()

    def _reset_state(self):
        self.history = deque()

    def observe(self, small, points, now):
        if self.history and self.history[-1][1].shape[2:] != small.shape[2:]:
            self.history.clear()
        while self.history and now - self.history[0][0] > self.DELAY + 1.0:
            self.history.popleft()
        if points is None:
            return
        box = face_crop(points, small.shape, self.MARGIN)
        if box is None:
            return
        x0, y0, x1, y1 = box
        self.history.append((now, small[y0:y1, x0:x1].copy(), np.float32(points - [x0, y0])))

    def _delayed(self, now):
        chosen = None
        for entry in self.history:
            if now - entry[0] >= self.DELAY:
                chosen = entry
            else:
                break
        return chosen

    def render(self, crop, points, t):
        entry = self._delayed(self.clock())
        if entry is None:
            return None
        _, old_crop, old_points = entry
        return self.swap(crop, points, old_crop, old_points)

    def swap(self, crop, points, old_crop, old_points):
        shape = crop.shape[:2]
        old = warp_mesh(old_crop, old_points, points, self.triangles, shape).astype(np.float32)
        alpha = face_mask(points, shape, shrink=0.9, feather=0.14)
        region = alpha > 0.5
        if region.sum() > 30:
            # Licht kann sich in vier Sekunden ändern: Helligkeit je Kanal angleichen.
            gain = crop[region].astype(np.float32).mean(0) / np.maximum(old[region].mean(0), 1)
            old *= np.clip(gain, 0.7, 1.4)
        return old, alpha


class Alterungszeitraffer(_FaceLoop):
    """Das Gesicht altert um Jahrzehnte, blitzt als Röntgenschädel auf und wird wieder jung."""

    name = 'Alterungs-Zeitraffer'
    description = ('Falten, Altersflecken, graues Haar und hängende Wangen in Sekunden; '
                   'kurzer Röntgenblitz, dann rückwärts bis zum Kindergesicht.')
    MARGIN = 0.45
    CYCLE = 14.0
    AGE_KEYS = ((0, 0), (1, 0), (6, 1), (7.5, 1), (11, -1), (12.5, -1), (13.5, 0), (14, 0))
    FLASHES = ((6.25, 6.45), (6.8, 6.9))

    def __init__(self):
        super().__init__()
        rng = np.random.default_rng(11)
        # Altersflecken fest im Gesicht: Stirn, Schläfen und Wangen.
        self.spots = [(rng.uniform(-0.3, 0.3), rng.choice([rng.uniform(-0.38, -0.2), rng.uniform(0.0, 0.2)]),
                       rng.uniform(0.008, 0.02), rng.uniform(0.4, 1.0)) for _ in range(16)]

    @classmethod
    def age(cls, t):
        return _phase(t, cls.AGE_KEYS)

    @classmethod
    def flash(cls, t):
        for t0, t1 in cls.FLASHES:
            if t0 <= t < t1:
                return float(np.sin(np.pi * (t - t0) / (t1 - t0)))
        return 0.0

    def render(self, crop, points, t):
        return self.render_age(crop, points, self.age(t), self.flash(t))

    # --- Bausteine -----------------------------------------------------------------

    @staticmethod
    def _bulge(u, v, cu, cv, radius, strength):
        """Verschiebung zur Mitte hin (Lupe) in Gesichtsbreiten."""
        du, dv = u - cu, v - cv
        g = np.exp(-(du * du + dv * dv) / (2 * radius * radius))
        return -du * strength * g, -dv * strength * g

    def _warp(self, crop, geo, age):
        u, v = geo.uv_grid(crop.shape[:2])
        su, sv = np.zeros_like(u), np.zeros_like(v)
        if age > 0:
            # Alter: Wangen und Kiefer sacken ab, Mundwinkel hängen.
            for side in (-1, 1):
                g = np.exp(-(((u - side * 0.27) / 0.14) ** 2 + ((v - 0.3) / 0.16) ** 2))
                sv -= 0.07 * age * g
                su += side * 0.012 * age * g
            corners = np.exp(-((np.abs(u) - 0.17) ** 2 / 0.004 + (v - 0.27) ** 2 / 0.004))
            sv -= 0.025 * age * corners
        elif age < 0:
            young = -age
            # Jung: größere Augen, kleinere Nase, kürzeres Kinn.
            for eye in (0, 1):
                eu, ev = self._eye_uv[eye]
                du, dv = self._bulge(u, v, eu, ev, 0.1, 0.35 * young)
                su += du
                sv += dv
            du, dv = self._bulge(u, v, 0, 0.1, 0.08, -0.25 * young)
            su += du
            sv += dv
            chin = np.clip((v - 0.3) / 0.3, 0, 1) * np.exp(-(u / 0.4) ** 2)
            sv += 0.05 * young * chin
        taper = np.clip((1.15 - np.sqrt((u / 0.55) ** 2 + ((v - 0.05) / 0.72) ** 2)) / 0.25, 0, 1)
        dx = (su * geo.axis[0] + sv * geo.down[0]) * geo.width * taper
        dy = (su * geo.axis[1] + sv * geo.down[1]) * geo.width * taper
        yy, xx = np.indices(crop.shape[:2], dtype=np.float32)
        return cv2.remap(crop, xx + dx, yy + dy, cv2.INTER_LINEAR, borderMode=cv2.BORDER_REFLECT_101)

    def _wrinkles(self, shape, geo, age):
        """Faltenkarte 0..1; feine Falten erscheinen später als tiefe."""
        canvas = np.zeros(shape, np.float32)
        thick = max(1, int(round(geo.width * 0.012)))

        def line(us, vs, depth, onset):
            strength = float(np.clip((age - onset) / (1 - onset), 0, 1)) * depth
            if strength <= 0:
                return
            pts = np.rint(geo.to_image(us, vs)).astype(np.int32)
            cv2.polylines(canvas, [pts], False, strength, thick, cv2.LINE_AA)

        s = np.linspace(-1, 1, 14)
        for k, v0 in enumerate((-0.29, -0.345, -0.4)):
            line(s * 0.24, v0 + 0.012 * np.sin(s * 3 + k) - 0.03 * s * s, 0.9 - 0.2 * k, 0.15 + 0.1 * k)
        for side in (-1, 1):
            eu, ev = self._eye_uv[0 if side < 0 else 1]
            q = np.linspace(0, 1, 8)
            # Zornesfalten, Krähenfüße, Tränensäcke, Nasolabialfalten, Marionettenfalten.
            line(side * (0.035 + 0.01 * q), -0.22 + 0.09 * q, 0.7, 0.35)
            for ray in (-0.35, 0, 0.35):
                line(eu + side * (0.1 + 0.07 * q), ev + (ray + 0.1) * 0.07 * q + ray * 0.02, 0.6, 0.45)
            arc = np.linspace(-1, 1, 10)
            line(eu + arc * 0.07, ev + 0.065 + 0.025 * (1 - arc * arc), 0.8, 0.25)
            line(eu + arc * 0.06, ev + 0.1 + 0.02 * (1 - arc * arc), 0.5, 0.55)
            line(side * (0.085 + 0.12 * q ** 1.3), 0.04 + 0.24 * q, 1.0, 0.1)
            line(side * (0.17 + 0.015 * q), 0.28 + 0.14 * q, 0.8, 0.4)
        for x in np.linspace(-0.08, 0.08, 5):
            line(np.float32([x, x * 1.1]), np.float32([0.2, 0.235]), 0.5, 0.6)
        return cv2.GaussianBlur(canvas, (0, 0), max(0.8, geo.width * 0.009))

    def _hair_region(self, geo, shape):
        u, v = geo.uv_grid(shape)
        crown = np.exp(-((u / 0.55) ** 2 + ((v + 0.5) / 0.35) ** 4))
        temples = np.exp(-(((np.abs(u) - 0.5) / 0.12) ** 2 + ((v + 0.1) / 0.25) ** 2))
        return np.clip(np.maximum(crown, temples) * (v < 0.1), 0, 1).astype(np.float32)

    def _skull(self, crop, points, geo):
        """Röntgenbild: helle Knochen, dunkle Augenhöhlen und Nasenloch, Zahnreihe.

        Weich genug für halbe Auflösung; das halbiert die Rechenzeit des Blitzes.
        """
        full = crop.shape[:2]
        crop = cv2.resize(crop, (max(1, full[1] // 2), max(1, full[0] // 2)), interpolation=cv2.INTER_AREA)
        geo = FaceGeometry(points * np.float32([crop.shape[1] / full[1], crop.shape[0] / full[0]]))
        shape = crop.shape[:2]
        u, v = geo.uv_grid(shape)
        # Schädelform: breiter Hirnschädel, schmaler Kiefer.
        half_width = 0.44 - 0.12 * np.clip((v - 0.05) / 0.4, 0, 1)
        head = np.exp(-((u / half_width) ** 2 + ((v + 0.04) / 0.58) ** 2) ** 4)
        bone = 0.8 * head
        for eu, ev in self._eye_uv:
            socket = np.exp(-(((u - eu) / 0.1) ** 2 + ((v - ev) / 0.09) ** 2) ** 2)
            bone -= 1.2 * socket
            bone += 0.35 * np.exp(-(((u - eu) / 0.13) ** 2 + ((v - ev + 0.1) / 0.025) ** 2))  # Brauenbogen
        nose_w = 0.02 + 0.3 * np.clip(v - 0.03, 0, 0.15)
        bone -= 1.0 * np.exp(-((u / nose_w) ** 4 + ((v - 0.11) / 0.06) ** 4))
        mouth_v = float(np.mean(self._mouth_v))
        teeth = np.exp(-((u / 0.16) ** 6 + ((v - mouth_v) / 0.05) ** 6))
        gaps = np.abs(np.sin(u / 0.032 * np.pi)) ** 0.3
        bone = bone * (1 - teeth) + teeth * (0.35 + 0.65 * gaps)
        bone -= 0.9 * teeth * np.exp(-((v - mouth_v) / 0.008) ** 2)
        for side in (-1, 1):
            bone += 0.3 * np.exp(-(((u - side * 0.27) / 0.09) ** 2 + ((v - 0.03) / 0.045) ** 2))
            bone -= 0.35 * np.exp(-(((u - side * 0.24) / 0.07) ** 2 + ((v - 0.24) / 0.09) ** 2))
        bone = cv2.GaussianBlur(np.clip(bone, 0, 1), (0, 0), max(0.6, geo.width * 0.005))
        flesh = cv2.cvtColor(crop, cv2.COLOR_RGB2GRAY).astype(np.float32) / 255
        level = np.clip(0.04 + 0.12 * (1 - flesh) + 0.95 * bone, 0, 1)
        level = cv2.resize(level, (full[1], full[0]), interpolation=cv2.INTER_LINEAR)
        return level[..., None] * np.float32([205, 228, 255]) + np.float32([4, 8, 22])

    def render_age(self, crop, points, age, flash=0.0):
        if abs(age) < 0.01 and flash <= 0:
            return None
        geo = FaceGeometry(points)
        shape = crop.shape[:2]
        self._eye_uv = []
        for p in points[20:22]:
            d = p - geo.center
            self._eye_uv.append((float(d @ geo.axis) / geo.width, float(d @ geo.down) / geo.width))
        self._mouth_v = [float((p - geo.center) @ geo.down) / geo.width for p in points[23:28]]
        face = face_mask(points, shape, shrink=0.97, feather=0.1)
        image = self._warp(crop, geo, age).astype(np.float32)
        lab = cv2.cvtColor(np.clip(image, 0, 255).astype(np.uint8), cv2.COLOR_RGB2LAB).astype(np.float32)
        u, v = geo.uv_grid(shape)
        if age > 0:
            a = age
            # Haut: fahler, gelblicher, Poren betont.
            detail = lab[..., 0] - cv2.GaussianBlur(lab[..., 0], (0, 0), max(1.0, geo.width * 0.01))
            lab[..., 0] += detail * 1.2 * a * face
            lab[..., 0] *= 1 - 0.06 * a * face
            lab[..., 1:] = 128 + (lab[..., 1:] - 128) * (1 - 0.3 * a * face[..., None])
            lab[..., 2] += 5 * a * face
            wrinkles = self._wrinkles(shape, geo, a) * face
            shift = np.roll(wrinkles, -max(1, int(geo.width * 0.008)), axis=0)
            lab[..., 0] *= 1 - 0.48 * np.clip(wrinkles * 1.3, 0, 1)
            lab[..., 0] += 22 * np.clip(shift - wrinkles, 0, 1)
            spots = np.zeros(shape, np.float32)
            for su, sv, r, depth in self.spots:
                spots += depth * np.exp(-((u - su) ** 2 + (v - sv) ** 2) / (2 * r * r))
            spots = np.clip(spots, 0, 1) * face * float(np.clip((a - 0.3) / 0.7, 0, 1))
            lab[..., 0] -= 22 * spots
            lab[..., 2] += 12 * spots
            lab[..., 1] += 4 * spots
            # Haare ergrauen: dunkle Pixel über der Stirn und an den Schläfen werden silbern.
            hair = self._hair_region(geo, shape) * (1 - face)
            dark = np.clip((120 - lab[..., 0]) / 60, 0, 1)
            gray_w = hair * dark * float(np.clip((a - 0.2) / 0.8, 0, 1))
            lab[..., 0] += (175 + 0.3 * (lab[..., 0] - 60) - lab[..., 0]) * gray_w
            lab[..., 1:] += (128 - lab[..., 1:]) * gray_w[..., None]
        elif age < 0:
            y = -age
            smooth_skin = cv2.bilateralFilter(np.clip(image, 0, 255).astype(np.uint8), 7, 35, 7)
            soft = cv2.cvtColor(smooth_skin, cv2.COLOR_RGB2LAB).astype(np.float32)
            lab = lab + (soft - lab) * (0.85 * y * face)[..., None]
            lab[..., 0] += 8 * y * face
            cheeks = sum(np.exp(-(((u - s * 0.22) / 0.09) ** 2 + ((v - 0.12) / 0.07) ** 2)) for s in (-1, 1))
            lab[..., 1] += 10 * y * cheeks * face
        result = cv2.cvtColor(np.clip(lab, 0, 255).astype(np.uint8), cv2.COLOR_LAB2RGB).astype(np.float32)
        alpha = np.maximum(face_mask(points, shape, shrink=1.0, feather=0.08),
                           self._hair_region(geo, shape) if age > 0 else 0)
        if flash > 0:
            skull = self._skull(crop, points, geo)
            cover = flash * face_mask(points, shape, shrink=1.08, feather=0.06)
            result += (skull - result) * cover[..., None]
            alpha = np.maximum(alpha, cover)
        return result, alpha


class GesichtsHologramm(_FaceLoop):
    """Das Gesicht wird zum leuchtenden Dreiecksnetz, fliegt auseinander und setzt sich zusammen."""

    name = 'Gesichts-Hologramm'
    description = ('Scanlinie verwandelt das Gesicht in ein blaues Hologramm mit Drahtgitter. '
                   'Die Dreiecke fliegen auseinander und setzen sich wieder zusammen; die Mimik bleibt live.')
    MARGIN = 0.9
    CYCLE = 12.0
    SWEEP_KEYS = ((0, 0), (1, 0), (2.5, 1), (10, 1), (11.5, 0), (12, 0))
    BURST_KEYS = ((0, 0), (5, 0), (6.5, 1), (7, 1), (8.5, 0), (12, 0))
    COLOR = np.float32([90, 210, 255])

    def __init__(self):
        super().__init__()
        self._build_mesh()
        self.rng = np.random.default_rng()

    def _build_mesh(self):
        """Dichtes Netz im Standardgesicht; jeder Punkt hängt baryzentrisch an den 28 Gesichtspunkten."""
        base = template_points()
        coarse = Delaunay(base)
        center = base[:20].mean(0)
        rim = [base[i] + (base[(i + 1) % 20] - base[i]) * k for i in range(20) for k in (0, 0.5)]
        dense = [center + (p - center) * 0.995 for p in rim]
        low, high = base.min(0), base.max(0)
        step = (high[0] - low[0]) / 11
        for y in np.arange(low[1] + step / 2, high[1], step * 0.87):
            offset = step / 2 if int((y - low[1]) / (step * 0.87)) % 2 else 0
            for x in np.arange(low[0] + offset, high[0], step):
                dense.append((x, y))
        dense.extend(base[20:])
        dense = np.float32(dense)
        simplex = coarse.find_simplex(dense)
        dense = dense[simplex >= 0]
        simplex = simplex[simplex >= 0]
        # Punkte zu nah an Landmarken entfernen, sonst entstehen Nadeldreiecke.
        keep = []
        for i, p in enumerate(dense):
            if all(np.linalg.norm(p - dense[j]) > step * 0.45 for j in keep):
                keep.append(i)
        dense, simplex = dense[keep], simplex[keep]
        transform = coarse.transform[simplex]
        bary = np.einsum('nij,nj->ni', transform[:, :2], dense - transform[:, 2])
        self.bary = np.hstack((bary, 1 - bary.sum(1, keepdims=True))).astype(np.float32)
        self.vertex_ids = coarse.simplices[simplex]
        self.triangles = Delaunay(dense).simplices
        rng = np.random.default_rng(5)
        count = len(self.triangles)
        self.spin = rng.uniform(-1, 1, count).astype(np.float32) * np.pi * 1.5
        self.scatter = rng.normal(0, 0.25, (count, 2)).astype(np.float32)
        self.speed = rng.uniform(0.6, 1.4, count).astype(np.float32)

    def mesh(self, points):
        return np.einsum('ni,nij->nj', self.bary, points[self.vertex_ids]).astype(np.float32)

    def render(self, crop, points, t):
        return self.render_holo(crop, points, _phase(t, self.SWEEP_KEYS), _phase(t, self.BURST_KEYS))

    def render_holo(self, crop, points, sweep, burst):
        if sweep <= 0:
            return None
        shape = crop.shape[:2]
        geo = FaceGeometry(points)
        verts = self.mesh(points)
        face = face_mask(points, shape, shrink=1.0, feather=0.05)
        # Scanlinie läuft von der Stirn zum Kinn, darüber ist das Gesicht schon Hologramm.
        u, v = geo.uv_grid(shape)
        line_v = -0.75 + 1.6 * sweep
        above = np.clip((line_v - v) / 0.03, 0, 1)
        gray = cv2.cvtColor(crop, cv2.COLOR_RGB2GRAY).astype(np.float32) / 255
        gray = cv2.equalizeHist((gray * 255).astype(np.uint8)).astype(np.float32) / 255
        flicker = 0.85 + 0.15 * self.rng.random()
        rows = (np.arange(shape[0]) % 3 == 0)[:, None]
        holo = self.COLOR * (0.15 + 0.85 * gray[..., None]) * flicker
        holo[rows[:, 0]] *= 0.55
        if self.rng.random() < 0.08:
            y0 = int(self.rng.integers(0, shape[0]))
            y1 = min(shape[0], y0 + max(2, shape[0] // 25))
            holo[y0:y1] = np.roll(holo[y0:y1], int(self.rng.integers(-8, 9)), axis=1)
        void = holo * 0.22 + crop.astype(np.float32) * 0.18
        wire = np.zeros(shape, np.float32)
        thick = 1
        if burst <= 0.001:
            tris = np.rint(verts[self.triangles] * 4).astype(np.int32)
            cv2.polylines(wire, list(tris), True, 1.0, thick, cv2.LINE_AA, shift=2)
            for p in np.rint(verts * 4).astype(np.int32):
                cv2.circle(wire, tuple(p.tolist()), 6, 1.0, -1, cv2.LINE_AA, shift=2)
            fill = np.zeros(shape, np.float32)
            skin = face
        else:
            # Dreiecke fliegen nach außen, drehen sich und fallen leicht.
            fill = np.zeros(shape, np.float32)
            tris = verts[self.triangles]
            centroid = tris.mean(1)
            direction = centroid - geo.center
            direction /= np.maximum(np.linalg.norm(direction, axis=1, keepdims=True), 1e-3)
            direction += self.scatter
            k = burst ** 1.3
            offset = (direction * self.speed[:, None] * geo.width * 0.9 * k
                      + geo.down * geo.width * 0.25 * k * k)
            angle = self.spin * k
            cos, sin = np.cos(angle)[:, None], np.sin(angle)[:, None]
            rel = tris - centroid[:, None]
            rot = np.stack((rel[..., 0] * cos - rel[..., 1] * sin, rel[..., 0] * sin + rel[..., 1] * cos), -1)
            moved = np.rint((rot + centroid[:, None] + offset[:, None]) * 4).astype(np.int32)
            shade = 0.25 + 0.35 * (1 - k)
            for tri in moved:
                cv2.fillConvexPoly(fill, tri, shade, cv2.LINE_AA, shift=2)
            cv2.polylines(wire, list(moved), True, 1.0, thick, cv2.LINE_AA, shift=2)
            skin = face * (1 - smooth(burst * 1.6))
        glow = cv2.GaussianBlur(wire, (0, 0), max(1.5, geo.width * 0.012)) * 1.6
        light = np.clip(wire + glow, 0, 1.5)
        holo_face = void + (holo - void) * skin[..., None]
        holo_face = holo_face * (1 - fill[..., None]) + self.COLOR * fill[..., None]
        holo_face += self.COLOR[None, None] * light[..., None] * 0.8 + 60 * wire[..., None]
        coverage = np.clip(np.maximum(face, np.maximum(fill * 3, glow)), 0, 1) * above
        edge = np.exp(-((v - line_v) / 0.012) ** 2) * (np.abs(u) < 0.7) * (sweep < 1)
        result = crop.astype(np.float32) * (1 - coverage[..., None]) + holo_face * coverage[..., None]
        result += edge[..., None] * np.float32([180, 240, 255])
        alpha = np.clip(np.maximum(coverage, edge), 0, 1)
        return result, alpha


class KubistischesGesicht(_FaceLoop):
    """Augen, Nase und Mund werden ausgeschnitten, verdreht und neu arrangiert."""

    name = 'Kubistisches Gesicht'
    description = ('Picasso-Stil: Gesicht in farbige Flächen zerlegt, Augen, Nase und Mund '
                   'versetzt, gedreht und vergrößert. Neue Anordnung alle 3 Sekunden.')
    MARGIN = 0.45
    CYCLE = 15.0
    HOLD = 3.0
    MORPH = 0.8
    PLANE_COLORS = np.float32([[215, 165, 85], [85, 120, 175], [205, 110, 100], [120, 160, 110]])
    # Teil: (du, dv in Gesichtsbreiten, Drehung in Grad, Maßstab, spiegeln)
    LAYOUTS = (
        {},
        {'left_eye': (-0.04, -0.06, -18, 1.35, 0), 'right_eye': (0.05, 0.05, 25, 0.85, 0),
         'nose': (0.06, 0.0, 90, 1.1, 1), 'mouth': (-0.05, 0.03, -12, 1.25, 0)},
        {'left_eye': (0.12, 0.2, 0, 1.1, 1), 'right_eye': (-0.02, -0.08, -35, 1.5, 0),
         'nose': (-0.08, 0.02, -20, 1.3, 0), 'mouth': (0.07, -0.02, 20, 0.9, 1)},
        {'left_eye': (0.0, 0.05, 90, 1.2, 0), 'right_eye': (0.03, 0.0, 0, 1.0, 0),
         'nose': (0.1, -0.05, 180, 1.2, 0), 'mouth': (-0.08, 0.06, -30, 1.4, 0)},
        {'left_eye': (-0.06, 0.1, 15, 1.6, 0), 'right_eye': (0.1, -0.1, 10, 0.8, 1),
         'nose': (0.0, 0.04, 60, 1.0, 1), 'mouth': (0.0, 0.02, 180, 1.2, 0)},
    )
    PLANE_ANGLES = ((), (80, -20), (60, 150), (100, 10), (30, 120))

    def layout(self, t):
        """Gemischte Parameter und Flächenwinkel zum Zeitpunkt t."""
        count = len(self.LAYOUTS)
        index = int(t // self.HOLD) % count
        local = t % self.HOLD
        mix = float(smooth(local / self.MORPH)) if local < self.MORPH else 1.0
        previous = (index - 1) % count
        return self._blend(self.LAYOUTS[previous], self.LAYOUTS[index], mix), \
            self.PLANE_ANGLES[previous], self.PLANE_ANGLES[index], mix

    @staticmethod
    def _blend(a, b, mix):
        identity = (0, 0, 0, 1, 0)
        result = {}
        for part in ('left_eye', 'right_eye', 'nose', 'mouth'):
            pa, pb = np.float32(a.get(part, identity)), np.float32(b.get(part, identity))
            result[part] = pa + (pb - pa) * mix
        return result

    def render(self, crop, points, t):
        params, angles_a, angles_b, mix = self.layout(t)
        return self.render_cubist(crop, points, params, angles_a, angles_b, mix)

    def _parts(self, points, geo):
        eyes = points[20:22]
        span = geo.eye_span
        mid = eyes.mean(0)
        nose = points[22]
        mouth_w = float(np.linalg.norm(points[25] - points[23]))
        nose_len = max(float(np.linalg.norm(nose - mid)), span * 0.3)
        return {
            'left_eye': (eyes[0], (span * 0.42, span * 0.26)),
            'right_eye': (eyes[1], (span * 0.42, span * 0.26)),
            'nose': (mid + (nose - mid) * 0.6, (span * 0.24, nose_len * 0.75)),
            'mouth': (points[24], (mouth_w * 0.72, max(mouth_w * 0.32, span * 0.16))),
        }

    def render_cubist(self, crop, points, params, angles_a=(), angles_b=(), mix=1.0):
        shape = crop.shape[:2]
        geo = FaceGeometry(points)
        base = crop.astype(np.float32)
        face = face_mask(points, shape, shrink=1.0, feather=0.03)
        u, v = geo.uv_grid(shape)
        strength = max(float(np.abs(np.float32(list(params.values()))[:, :3]).max()) * 4, 0)
        strength = float(np.clip(strength, 0, 1))
        if strength <= 0.01 and not angles_a and not angles_b:
            return None
        # Farbflächen: zwei Schnitte durch die Gesichtsmitte, jede Fläche eigener Farbton.
        result = base.copy()
        outline = np.zeros(shape, np.float32)
        thick = max(1, int(round(geo.width * 0.008)))
        posterized = light = None
        for angles, weight in ((angles_a, 1 - mix), (angles_b, mix)):
            if not angles or weight <= 0.01:
                continue
            label = np.zeros(shape, np.int32)
            for k, deg in enumerate(angles):
                rad = np.radians(deg)
                label += ((u * np.cos(rad) + v * np.sin(rad)) > 0.03 * k).astype(np.int32) << k
            if posterized is None:
                posterized = np.floor(base / 42) * 42 + 21
                light = posterized.mean(2, keepdims=True) / 128
            # Ein Durchlauf für alle Flächen: Farbton je Fläche aus der Tabelle.
            tint = self.PLANE_COLORS[label % len(self.PLANE_COLORS)]
            target = posterized * 0.6 + tint * 0.4 * light
            result += (target - result) * (0.8 * face * weight)[..., None]
            edges = cv2.Canny((label * 60).astype(np.uint8), 10, 30).astype(np.float32) / 255
            outline = np.maximum(outline, cv2.dilate(edges, np.ones((thick, thick), np.uint8)) * face * weight)
        blurred = cv2.GaussianBlur(base, (0, 0), max(2.0, geo.width * 0.06))
        parts = self._parts(points, geo)
        pasted = np.zeros(shape, np.float32)
        layers = []
        for name, (center, axes) in parts.items():
            du, dv, rot, scale, flip = params[name]
            mask = np.zeros(shape, np.float32)
            cv2.ellipse(mask, (tuple(np.rint(center).astype(int).tolist()),
                               (float(axes[0] * 2), float(axes[1] * 2)), geo.angle), 1.0, -1, cv2.LINE_AA)
            moved = abs(du) + abs(dv) + abs(rot) / 90 + abs(scale - 1) + flip
            if moved < 0.005:
                continue
            # Wo der Teil wegwandert, bleibt verwischte Haut statt eines doppelten Auges.
            gone = float(np.clip(moved * 3, 0, 1)) * mask
            result += (blurred - result) * gone[..., None]
            target = center + geo.axis * du * geo.width + geo.down * dv * geo.width
            matrix = cv2.getRotationMatrix2D(tuple(np.float32(center).tolist()), float(-rot), float(scale))
            if flip > 0.5:
                # An der Gesichtsachse gespiegelt: Profilansicht.
                a = np.radians(geo.angle)
                ax = np.float32([np.cos(a), np.sin(a)])
                reflect = np.eye(2, dtype=np.float32) - 2 * np.outer(ax, ax)
                m2 = matrix[:, :2] @ reflect
                matrix = np.hstack((m2, (center - m2 @ center)[:, None])).astype(np.float32)
            matrix[:, 2] += target - center
            patch = cv2.warpAffine(base, matrix, (shape[1], shape[0]), flags=cv2.INTER_LINEAR,
                                   borderMode=cv2.BORDER_REFLECT_101)
            warped = cv2.warpAffine(mask, matrix, (shape[1], shape[0]), flags=cv2.INTER_LINEAR)
            layers.append((patch, warped))
        for patch, warped in layers:
            ring = np.clip(cv2.dilate(warped, np.ones((thick * 2 + 1, thick * 2 + 1), np.uint8)) - warped, 0, 1)
            result = result * (1 - ring[..., None]) + np.float32([30, 25, 25]) * ring[..., None]
            result = result * (1 - warped[..., None]) + patch * warped[..., None]
            pasted = np.maximum(pasted, np.maximum(warped, ring))
        result = result * (1 - outline[..., None] * 0.85) + np.float32([30, 25, 25]) * outline[..., None] * 0.85
        alpha = np.clip(np.maximum(face, pasted), 0, 1)
        return result, alpha
