"""Flüssigmetall: lokale Personenmaske, Gesichtsrelief, reflektierende Schmelze."""

import time

import cv2
import numpy as np

from bildeffekte import IEffectStrategy
from bildeffekte.portrait import submit_portrait


def _smooth(value):
    value = np.clip(value, 0.0, 1.0)
    return value * value * (3 - 2 * value)


class Fluessigmetall(IEffectStrategy):
    category = "Natur & Material"

    name = "Flüssigmetall-Schmelze"
    description = ("Ein Standbild wird zu flüssigem Chrom: Gesicht und Silhouette verlieren "
                   "ihre Form und sammeln sich als Metallpfütze. Erneut anwählen startet neu. "
                   "Die Personenerkennung kann beim ersten Mal einige Sekunden brauchen.")

    def __init__(self):
        self.scene = None
        self.size = None
        self.started = 0.0
        self._pending = None
        self._reset_requested = False
        self.mask = None
        self.segmentation = 'none'

    def activate(self):
        # activate() läuft im Lifecycle-Thread: Renderzustand nur in apply() ersetzen.
        self._reset_requested = True

    def deactivate(self):
        self._reset_requested = True

    def _freeze(self, frame):
        if self._pending is not None:
            self._pending.cancel()
        self.size = frame.shape[1], frame.shape[0]
        scale = min(1.0, 512 / max(self.size), 400 / frame.shape[0])
        width, height = max(1, round(self.size[0] * scale)), max(1, round(self.size[1] * scale))
        self.scene = cv2.resize(frame, (width, height), interpolation=cv2.INTER_AREA)
        self.mask = None
        self._ready = False
        self._pending = None
        self.segmentation = 'none'
        # Sehr kleine Bilder eignen sich nicht für semantische Gesichtsanalyse.
        if min(width, height) >= 120 and max(width, height) >= 240:
            self._pending = submit_portrait(self.scene)
        else:
            self._prepare(None)

    def _fallback_mask(self):
        h, w = self.scene.shape[:2]
        if min(h, w) < 24:
            return np.zeros((h, w), np.uint8)
        labels = np.zeros((h, w), np.uint8)
        margin = max(1, w // 20)
        try:
            cv2.grabCut(self.scene, labels, (margin, 1, w - margin * 2, h - 2),
                        np.zeros((1, 65)), np.zeros((1, 65)), 3, cv2.GC_INIT_WITH_RECT)
            return ((labels == cv2.GC_FGD) | (labels == cv2.GC_PR_FGD)).astype(np.uint8) * 255
        except cv2.error:
            return np.zeros((h, w), np.uint8)

    def _prepare(self, portrait):
        h, w = self.scene.shape[:2]
        binary = portrait['mask'] if portrait is not None else self._fallback_mask()
        self.segmentation = 'Apple Vision' if portrait is not None else 'Kontrastkontur'
        self.mask = cv2.GaussianBlur(binary.astype(np.float32) / 255, (0, 0), 0.65)
        self._ready = True
        self.started = time.monotonic()
        solid = (self.mask > 0.45).astype(np.uint8)
        if solid.sum() < max(8, w * h * 0.015):
            self.mask.fill(0)
            return
        self.bounds = cv2.boundingRect(cv2.findNonZero(solid))
        left, top, bw, bh = self.bounds
        self.x, self.y = np.meshgrid(np.arange(w, dtype=np.float32), np.arange(h, dtype=np.float32))
        distance = cv2.distanceTransform(np.pad(solid, 1), cv2.DIST_L2, 5)[1:-1, 1:-1]
        self.depth = np.sqrt(distance * max(4, bw * 0.10))
        face = portrait.get('face') if portrait is not None else None
        if face is None:
            upper = np.argwhere(solid[top:top + max(1, bh // 3)])
            cx = float(upper[:, 1].mean()) if len(upper) else left + bw / 2
            fw = min(bw * 0.60, bh * 0.42)
            face = (cx - fw / 2, top + bh * 0.04, fw, fw * 1.12)
        fx, fy, fw, fh = face
        # Ellipsoidischer Kopf plus Augenhöhlen, Nase und Mund: Relief für Reflexionen.
        q = ((self.x - fx - fw * 0.5) / max(1, fw * 0.63)) ** 2
        q += ((self.y - fy - fh * 0.40) / max(1, fh * 0.78)) ** 2
        head = np.sqrt(np.maximum(0, 1 - q)) * fw * 0.40
        self.depth = np.maximum(self.depth * (1 - 0.4 * np.exp(-q * 2)), head)
        defaults = {'nose': (fx + fw * .5, fy + fh * .57),
                    'leftEye': (fx + fw * .30, fy + fh * .36),
                    'rightEye': (fx + fw * .70, fy + fh * .36),
                    'outerLips': (fx + fw * .5, fy + fh * .79)}
        features = portrait.get('features', {}) if portrait is not None else {}
        for key, amplitude, sx, sy in (('nose', .19, .11, .20), ('leftEye', -.07, .13, .07),
                                        ('rightEye', -.07, .13, .07), ('outerLips', -.035, .22, .04)):
            cx, cy = features.get(key, defaults[key])
            q = ((self.x - cx) / max(1, fw * sx)) ** 2 + ((self.y - cy) / max(1, fh * sy)) ** 2
            self.depth += fw * amplitude * np.exp(-q * .5)
        gray = cv2.cvtColor(self.scene, cv2.COLOR_RGB2GRAY).astype(np.float32) / 255
        smooth = cv2.GaussianBlur(gray, (0, 0), 2)
        self.depth += (smooth - cv2.GaussianBlur(smooth, (0, 0), 8)) * fw * .035
        self.depth = cv2.GaussianBlur(np.maximum(self.depth, 0) * self.mask, (0, 0), 1.4)

        # Nur verdeckten Hintergrund ergänzen; sichtbare Bildbereiche bleiben scharf.
        size = (max(1, w // 2), max(1, h // 2))
        repair_mask = cv2.dilate(solid, np.ones((5, 5), np.uint8)) * 255
        repaired = cv2.inpaint(cv2.resize(self.scene, size), cv2.resize(repair_mask, size),
                               4, cv2.INPAINT_TELEA)
        repaired = cv2.resize(repaired, (w, h)).astype(np.float32)
        replace = cv2.GaussianBlur(repair_mask, (0, 0), 1).astype(np.float32)[..., None] / 255
        self.background = self.scene * (1 - replace) + repaired * replace
        self._make_environment()

    def _make_environment(self):
        # Fotografische Strukturen bleiben in der Spiegelung erkennbar. Lichtflächen
        # und Fensterrahmen geben auch vor einer einfarbigen Wand Metallkontrast.
        room = cv2.resize(self.background.astype(np.float32), (512, 256))
        room = cv2.GaussianBlur(room, (0, 0), 1.2)
        room = room * .28 + room.mean(axis=2, keepdims=True) * .72
        room = np.clip((room - 110) * 1.45 + 105, 8, 230)
        self.reflected_room = room.copy()
        env = room * .70 + 20
        # Decke über der Pfütze: helle Flächen mit dunklen Rahmen. Die liegende
        # Oberfläche spiegelt andere Richtungen als die stehende Figur.
        ceiling = np.linspace(205, 100, 60, dtype=np.float32)[:, None, None]
        env[:60] = env[:60] * .22 + ceiling
        env[18:23] *= .32
        cv2.rectangle(env, (0, 28), (58, 102), (290, 300, 315), -1)
        cv2.rectangle(env, (455, 28), (511, 102), (290, 300, 315), -1)
        env[112:125] *= .22
        # HDR-Lichtquellen: nach dem Abtasten weich begrenzen, nicht flächig grau.
        cv2.rectangle(env, (185, 16), (230, 227), (340, 350, 365), -1)
        cv2.rectangle(env, (293, 18), (355, 203), (265, 280, 300), -1)
        cv2.rectangle(env, (231, 16), (237, 227), (12, 15, 20), -1)
        cv2.rectangle(env, (280, 40), (285, 210), (410, 410, 400), -1)
        cv2.rectangle(env, (170, 57), (365, 62), (18, 21, 25), -1)
        cv2.rectangle(env, (170, 153), (365, 160), (20, 24, 30), -1)
        self.environment = cv2.GaussianBlur(env, (0, 0), 1.5).astype(np.float32)

    def _render(self, elapsed):
        h, w = self.scene.shape[:2]
        if not np.any(self.mask):
            return self.scene
        metal_mix = float(_smooth(elapsed / 1.5))
        melt = float(_smooth((elapsed - 1.7) / 6.0))
        left, top, bw, bh = self.bounds
        center = left + bw * .5
        bottom = min(h - 1, top + bh)
        floor = bottom - h * .035 * melt
        scale = 1 - .985 * melt
        # Glatte, ungleich schnelle Fließkanäle; sie beruhigen sich in der Pfütze.
        channels = (.55 + .25 * np.sin(self.x * .037) + .20 * np.sin(self.x * .071 + 1.2))
        sag = bh * .14 * melt * (1 - melt) * channels
        relative_y = (self.y - (floor - bh * scale) - sag) / max(1, bh * scale)
        gravity_y = 1 - np.maximum(0, 1 - relative_y) ** (1 / (1 + 1.8 * melt))
        map_y = (top + bh * gravity_y).astype(np.float32)
        map_y[(relative_y < 0) | (relative_y > 1)] = -h
        spread = 1 + .22 * melt
        flow = 6 * melt * (1 - melt) * np.sin(self.y * .055 + elapsed * .8)
        map_x = center + (self.x - center) / spread + flow
        alpha = cv2.remap(self.mask, map_x, map_y, cv2.INTER_LINEAR, borderMode=cv2.BORDER_CONSTANT)
        depth = cv2.remap(self.depth, map_x, map_y, cv2.INTER_LINEAR, borderMode=cv2.BORDER_CONSTANT)
        depth *= np.sqrt(scale)
        warped = cv2.remap(self.scene, map_x, map_y, cv2.INTER_LINEAR, borderMode=cv2.BORDER_REPLICATE)
        depth += alpha * (1.2 * np.sin(self.x * .10 + self.y * .025 + elapsed) * melt * (1 - melt))

        # Ein zusammenhängender flacher Metallkörper, kein ausgeblendetes Gespenst.
        rx = max(2, min(w * .45, bw * (.16 + .54 * float(np.sqrt(melt)))))
        ry = max(2, bh * (.012 + .078 * float(np.sqrt(melt))))
        q = ((self.x - center) / rx) ** 2 + ((self.y - floor) / ry) ** 2
        pool_depth = np.sqrt(np.maximum(0, 1 - q)) * (bh * .042 * melt)
        pool_alpha = np.clip((1 - q) * min(rx, ry) * .6, 0, 1) if melt > .001 else np.zeros_like(q)
        pool_alpha *= min(1, melt * 15)
        depth = np.maximum(depth, pool_depth)
        alpha = np.maximum(alpha, pool_alpha)
        # Kapillarwellen ändern die Normalen und damit die Spiegelung, auch in
        # der fertigen Pfütze. Keine direkt aufgemalten Helligkeitsstreifen.
        pool_weight = np.clip((pool_depth - depth + 1.5) / 1.5, 0, 1) * pool_alpha
        radius = np.sqrt(((self.x - center) * .55) ** 2 + ((self.y - floor) * 1.5) ** 2 + 1)
        settling = .32 + .68 * np.exp(-max(0.0, elapsed - 7.7) * .35)
        ripples = (.70 * np.sin(radius * .19 - elapsed * 2.2) +
                   .28 * np.sin(self.x * .13 + self.y * .16 + elapsed * 1.3))
        depth += ripples * pool_weight * settling
        depth = cv2.GaussianBlur(depth.astype(np.float32), (0, 0), .85)
        nx = -cv2.Sobel(depth, cv2.CV_32F, 1, 0, ksize=3) * .125
        ny = -cv2.Sobel(depth, cv2.CV_32F, 0, 1, ksize=3) * .125
        # Die Pfütze liegt auf einer Bodenebene; ihre Normale zeigt schräg nach oben.
        ny -= pool_weight * 1.15
        norm = np.sqrt(nx * nx + ny * ny + 1)
        nx /= norm
        ny /= norm
        nz = 1 / norm
        # Spiegelrichtung aus den Oberflächennormalen.
        rxv, ryv, rzv = 2 * nz * nx, 2 * nz * ny, 2 * nz * nz - 1
        u = (np.arctan2(rxv, rzv) / (2 * np.pi) + .5) * 511
        v = (np.arccos(np.clip(-ryv, -1, 1)) / np.pi) * 255
        chrome = cv2.remap(self.environment, u, v, cv2.INTER_LINEAR, borderMode=cv2.BORDER_WRAP)
        # Näherung der räumlichen Umgebungsspiegelung mit Parallaxe. So spiegelt
        # eine breite flache Fläche nicht überall denselben einzelnen Bildpunkt.
        room_x = (self.x / max(1, w - 1) * 511 + rxv * 145).astype(np.float32)
        room_y = (255 - self.y / max(1, h - 1) * 170 + ryv * 95).astype(np.float32)
        reflected = cv2.remap(self.reflected_room, room_x, room_y, cv2.INTER_LINEAR,
                              borderMode=cv2.BORDER_REFLECT)
        chrome = chrome * .75 + reflected * .35
        # Kleine gerichtete Lichtquellen erzeugen scharfe, wandernde Glanzlichter.
        light_a = np.maximum(0, -.38 * nx - .42 * ny + .824 * nz)
        light_b = np.maximum(0, .55 * nx - .22 * ny + .805 * nz)
        specular = 190 * light_a ** 70 + 115 * light_b ** 110
        chrome += specular[..., None]
        chrome *= (.90 + .10 * (1 - nz) ** 5)[..., None]
        # Weicher Highlight-Rolloff bewahrt Unterschiede in sehr hellen Reflexen.
        chrome = np.where(chrome > 200, 200 + 55 * (1 - np.exp(-(chrome - 200) / 95)), chrome)
        shadow = cv2.GaussianBlur(alpha, (0, 0), 3) * .16 * melt
        background = self.background * (1 - shadow[..., None])
        object_rgb = warped * (1 - metal_mix) + chrome * metal_mix
        result = background * (1 - alpha[..., None]) + object_rgb * alpha[..., None]
        # Zu Beginn wirklich das unveränderte eingefrorene Bild zeigen.
        reveal = float(_smooth(elapsed / .35))
        result = self.scene * (1 - reveal) + result * reveal
        return np.clip(result, 0, 255).astype(np.uint8)

    def apply(self, frame, config):
        if self._reset_requested:
            self._reset_requested = False
            self.scene = None
        if self.scene is None or self.size != (frame.shape[1], frame.shape[0]):
            self._freeze(frame)
        if self._pending is not None and self._pending.done():
            try:
                portrait = self._pending.result()
            except Exception:
                portrait = None
            self._pending = None
            self._prepare(portrait)
        result = self._render(max(0, time.monotonic() - self.started)) if self._ready else self.scene
        return cv2.resize(result, self.size, interpolation=cv2.INTER_LINEAR)
