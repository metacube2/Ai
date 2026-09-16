"""
Plugin: Glitch-Dimension: Realität kollabiert zwischen Dimensionen

Ausgelagert aus video.py – Effekte sind reine Plugins und kennen nur das Paket
`bildeffekte`.
"""

from typing import Any, Dict, List, Optional, Tuple

import collections
import time

import cv2
import numpy as np

from bildeffekte import (IEffectStrategy, PixelGrid, rand_int, random_box, scale_image, shift_image,
                         to_rgb_colormap)


class GlitchDimensionEffectStrategy(IEffectStrategy):
    """
    Extremer Glitch: Die Realität kollabiert zwischen Dimensionen, mit
    Farbverschiebungen, räumlichen Verzerrungen und Datenkorruption.
    Achtung: starkes Flackern.
    """

    def __init__(self):
        self.rng = np.random.default_rng()
        self.grid = PixelGrid()
        self.frame_counter = 0
        self.last_frames: collections.deque = collections.deque(maxlen=8)
        self.dimension_shift_time = time.time()
        self.reality_stability = 1.0
        self._stability_offset = 0.4       # Start ruhig, danach Pendeln um 0.6
        self.current_dimension = 0
        self.transition = 0.0              # Überblendung nach Dimensionswechsel (1 -> 0)
        self.last_output: Optional[np.ndarray] = None
        self.last_reality_breach = 0.0
        self.breach_coordinates: List[Tuple[int, int]] = []
        self.rgb_shift_values = [0, 0, 0]
        self.glitch_blocks = [self._new_glitch_block() for _ in range(15)]
        self.dimension_effects = [
            self._corrupt_reality,        # 0: Realitätskorruption
            self._digital_decay,          # 1: Digitaler Zerfall
            self._quantum_uncertainty,    # 2: Quantenunsicherheit
            self._void_intrusion,         # 3: Eindringen der Leere
            self._hyperbolic_distortion,  # 4: Hyperbolische Verzerrung
        ]

    @property
    def name(self) -> str:
        return "Glitch-Dimension"

    def _new_glitch_block(self) -> Dict[str, Any]:
        rng = self.rng
        return {
            'x': rng.uniform(0, 1), 'y': rng.uniform(0, 1),
            'width': rng.uniform(0.05, 0.3), 'height': rng.uniform(0.05, 0.3),
            'corruption': rng.uniform(0.3, 1.0),
            'lifetime': int(rng.integers(5, 30)), 'current_age': 0,
            'shift_values': [int(v) for v in rng.integers(-30, 31, 3)],
            'direction': int(rng.integers(0, 4)),   # 0: rechts, 1: links, 2: oben, 3: unten
            'type': int(rng.integers(0, 5)),
        }

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        now = time.time()
        rng = self.rng
        self.frame_counter += 1

        if self.last_frames and self.last_frames[-1].shape != frame.shape:
            self.last_frames.clear()
            self.last_output = None
        if len(self.last_frames) < 8 or self.frame_counter % 2 == 0:
            self.last_frames.append(frame.copy())

        self._update_reality_stability()

        result = self._apply_rgb_channel_shift(frame)
        result = self._apply_glitch_blocks(result)
        result = self._apply_high_frequency_noise(result)
        result = self._apply_dimension_jumps(result)
        result = self._apply_temporal_anomalies(result)
        result = self.dimension_effects[self.current_dimension](result)

        if rng.random() < 0.01 * (2.0 - self.reality_stability) and now - self.last_reality_breach > 3.0:
            result = self._apply_reality_breach(result)
            self.last_reality_breach = now

        # Sicherheitsmechanismus gegen zu extreme Bilder
        if rng.random() < 0.05:
            intensity = rng.uniform(0.3, 0.7)
            result = cv2.addWeighted(result, intensity, frame, 1 - intensity, 0)

        # Weicher Übergang nach einem Dimensionswechsel
        if self.transition > 0 and self.last_output is not None:
            result = cv2.addWeighted(result, 1 - self.transition, self.last_output, self.transition, 0)
            self.transition = max(0.0, self.transition - 0.1)

        self.last_output = result
        self._update_glitch_parameters(now)
        return result

    def _update_reality_stability(self) -> None:
        """Stabilität pendelt langsam um 0.6, mit Rauschen und seltenen Sprüngen"""
        rng = self.rng
        if rng.random() < 0.005:
            self._stability_offset += rng.uniform(-0.4, 0.4)
        self._stability_offset = 0.97 * self._stability_offset + rng.normal(0, 0.03)
        self.reality_stability = float(np.clip(
            0.6 + 0.3 * np.sin(self.frame_counter * 0.01) + self._stability_offset, 0.2, 1.0))

    def _update_glitch_parameters(self, now: float) -> None:
        rng = self.rng
        instability = 1.0 - self.reality_stability * 0.8
        for i in range(3):
            limit = int(30 * instability)
            if limit > 0 and rng.random() < instability * 0.3:
                self.rgb_shift_values[i] = int(rng.integers(-limit, limit))
            else:
                self.rgb_shift_values[i] = int(self.rgb_shift_values[i] * 0.85)

        for block in self.glitch_blocks:
            block['current_age'] += 1
            if block['current_age'] >= block['lifetime']:
                direction = block['direction']
                block.update(self._new_glitch_block())
                block['direction'] = direction
            speed = 0.01 * (1.0 - self.reality_stability) * block['corruption']
            if block['direction'] == 0:
                block['x'] = min(1.1, block['x'] + speed)
            elif block['direction'] == 1:
                block['x'] = max(-0.1, block['x'] - speed)
            elif block['direction'] == 2:
                block['y'] = max(-0.1, block['y'] - speed)
            else:
                block['y'] = min(1.1, block['y'] + speed)

        if now - self.dimension_shift_time > 5.0 and rng.random() < 0.02:
            new_dimension = int(rng.integers(0, len(self.dimension_effects)))
            if new_dimension != self.current_dimension:
                self.current_dimension = new_dimension
                self.transition = 1.0
            self.dimension_shift_time = now

    # --- Basiseffekte ---

    def _apply_rgb_channel_shift(self, frame: np.ndarray) -> np.ndarray:
        """Chromatische Aberration: Kanäle horizontal gegeneinander verschoben"""
        if not any(self.rgb_shift_values):
            return frame
        channels = cv2.split(frame)
        return cv2.merge([shift_image(c, -s, 0) for c, s in zip(channels, self.rgb_shift_values)])

    def _apply_glitch_blocks(self, frame: np.ndarray) -> np.ndarray:
        """Korrupte, verschobene und verzerrte Blöcke"""
        rng = self.rng
        height, width = frame.shape[:2]
        result = frame.copy()

        for block in self.glitch_blocks:
            if rng.random() > block['corruption'] * (1.0 - self.reality_stability):
                continue
            x1 = int(np.clip(width * block['x'], 0, width - 1))
            y1 = int(np.clip(height * block['y'], 0, height - 1))
            x2 = int(np.clip(x1 + width * block['width'], 0, width - 1))
            y2 = int(np.clip(y1 + height * block['height'], 0, height - 1))
            if x2 <= x1 or y2 <= y1:
                continue

            region = frame[y1:y2, x1:x2].copy()
            bh, bw = region.shape[:2]
            kind = block['type']

            if kind == 0:
                region = cv2.merge([shift_image(c, s, 0, cv2.BORDER_CONSTANT)
                                    for c, s in zip(cv2.split(region), block['shift_values'])])
            elif kind == 1:
                scale = max(1, int(10 * block['corruption']))
                small = cv2.resize(region, (max(1, bw // scale), max(1, bh // scale)))
                region = cv2.resize(small, (bw, bh), interpolation=cv2.INTER_NEAREST)
            elif kind == 2:
                shifts = (np.sin(np.arange(bh) / 5) * 10 * block['corruption']).astype(int)
                cols = (np.arange(bw)[None, :] - shifts[:, None]) % bw
                region = region[np.arange(bh)[:, None], cols]
            elif kind == 3:
                mask = rng.random((bh, bw)) < 0.2 * block['corruption']
                region[mask] = rng.integers(0, 256, (int(mask.sum()), 3), dtype=np.uint8)
            else:
                region = 255 - region
                over = region > int(128 * block['corruption'])
                region[over] = 255 - region[over]

            result[y1:y2, x1:x2] = region
        return result

    def _apply_high_frequency_noise(self, frame: np.ndarray) -> np.ndarray:
        """Scanlines, Sync-Störungen, CRT-Rauschen, Farbflecken, Störlinien"""
        rng = self.rng
        height, width = frame.shape[:2]
        result = frame.copy()
        noise_level = 20 * (1.0 - self.reality_stability)

        if rng.random() < 0.7:
            darkness = rng.uniform(0.7, 0.9, size=(len(range(0, height, 2)), 1, 1)).astype(np.float32)
            result[::2] = (result[::2] * darkness).astype(np.uint8)

        if rng.random() < 0.3:
            for _ in range(int(rng.integers(1, 5))):
                y0 = int(rng.integers(0, height))
                y1 = min(height, y0 + int(rng.integers(1, 10)))
                shift = rand_int(rng, -width // 5, width // 5)
                if shift > 0:
                    result[y0:y1, shift:] = result[y0:y1, :-shift].copy()
                    result[y0:y1, :shift] = 0
                elif shift < 0:
                    result[y0:y1, :shift] = result[y0:y1, -shift:].copy()
                    result[y0:y1, shift:] = 0

        if noise_level >= 1 and rng.random() < 0.5:
            noise = rng.standard_normal((height, width, 3), dtype=np.float32) * noise_level
            result = np.clip(result.astype(np.float32) + noise, 0, 255).astype(np.uint8)

        if rng.random() < 0.2:
            for _ in range(int(rng.integers(10, 50))):
                color = tuple(int(c) for c in rng.integers(0, 256, 3))
                center = (int(rng.integers(0, width)), int(rng.integers(0, height)))
                cv2.circle(result, center, int(rng.integers(1, 5)), color, -1)

        if rng.random() < 0.15 * (1.0 - self.reality_stability):
            for _ in range(int(rng.integers(1, 10))):
                y = int(rng.integers(0, height))
                y1 = min(height, y + int(rng.integers(1, 5)))
                if rng.random() < 0.5:
                    result[y:y1] = rng.integers(0, 256, (y1 - y, width, 3), dtype=np.uint8)
                else:
                    result[y:y1] = 0 if rng.random() < 0.5 else 255
        return result

    def _apply_dimension_jumps(self, frame: np.ndarray) -> np.ndarray:
        """Wirbel mit Farbsprung und 'Portale' in frühere Frames"""
        rng = self.rng
        height, width = frame.shape[:2]
        result = frame

        if rng.random() < 0.02 * (1.0 - self.reality_stability):
            cx = width // 2 + rand_int(rng, -width // 4, width // 4)
            cy = height // 2 + rand_int(rng, -height // 4, height // 4)
            matrix = cv2.getRotationMatrix2D((cx, cy), rng.uniform(0, 360), rng.uniform(0.7, 1.3))
            matrix[0, 0] *= rng.uniform(0.8, 1.2)
            matrix[1, 1] *= rng.uniform(0.8, 1.2)
            result = cv2.warpAffine(result, matrix, (width, height))

            hsv = cv2.cvtColor(result, cv2.COLOR_RGB2HSV)
            hsv[:, :, 0] = ((hsv[:, :, 0].astype(np.int32) + int(rng.integers(0, 180))) % 180).astype(np.uint8)
            shifted = cv2.cvtColor(hsv, cv2.COLOR_HSV2RGB)
            mask = np.zeros((height, width), dtype=np.uint8)
            cv2.circle(mask, (cx, cy), int(min(width, height) * rng.uniform(0.1, 0.3)), 255, -1)
            result = np.where(mask[..., None] > 0, shifted, result)

        if rng.random() < 0.03 * (1.0 - self.reality_stability) and self.last_frames:
            result = result.copy()
            for _ in range(int(rng.integers(1, 4))):
                x, y, pw, ph = random_box(rng, width, height, 30, 200)
                source = self.last_frames[int(rng.integers(0, len(self.last_frames)))]
                portal = source[y:y + ph, x:x + pw]
                if rng.random() < 0.5:
                    portal = 255 - portal
                py, px = np.ogrid[:ph, :pw]
                dist = np.hypot(px - pw / 2, py - ph / 2) / max(1e-6, np.hypot(pw / 2, ph / 2))
                mask = cv2.GaussianBlur(np.clip(1 - dist, 0, 1).astype(np.float32), (7, 7), 0)[..., None]
                target = result[y:y + ph, x:x + pw]
                result[y:y + ph, x:x + pw] = (portal * mask + target * (1 - mask)).astype(np.uint8)
                color = tuple(int(c) for c in rng.integers(0, 256, 3))
                cv2.rectangle(result, (x, y), (x + pw, y + ph), color, 2)
        return result

    def _apply_temporal_anomalies(self, frame: np.ndarray) -> np.ndarray:
        """Bereiche zeigen frühere Frames: Streifen, Ringe, Blobs oder Wellen"""
        rng = self.rng
        if len(self.last_frames) < 3 or rng.random() >= 0.1 * (1.0 - self.reality_stability):
            return frame

        height, width = frame.shape[:2]
        past = self.last_frames[int(rng.integers(0, len(self.last_frames)))]
        mask = np.zeros((height, width), dtype=np.float32)
        kind = int(rng.integers(0, 4))

        if kind == 0:
            vertical = rng.random() < 0.5
            count = int(rng.integers(3, 10))
            size = max(1, (width if vertical else height) // count)
            for i in range(count):
                if rng.random() < 0.4:
                    if vertical:
                        mask[:, i * size:(i + 1) * size] = 1.0
                    else:
                        mask[i * size:(i + 1) * size, :] = 1.0
        elif kind == 1:
            xs, ys = self.grid.get(height, width)
            dist = np.hypot(xs - width // 2, ys - height // 2)
            max_radius = np.hypot(width // 2, height // 2)
            count = int(rng.integers(3, 8))
            for i in range(count):
                if rng.random() < 0.5:
                    mask[(dist >= max_radius * i / count) & (dist < max_radius * (i + 1) / count)] = 1.0
        elif kind == 2:
            for _ in range(int(rng.integers(2, 6))):
                center = (int(rng.integers(0, width)), int(rng.integers(0, height)))
                cv2.circle(mask, center, int(rng.integers(20, 100)), 1.0, -1)
        else:
            wave = (np.sin(np.arange(width, dtype=np.float32) * rng.uniform(1, 5) * np.pi / width
                           + rng.uniform(0, 2 * np.pi))[None, :]
                    * np.cos(np.arange(height, dtype=np.float32) * rng.uniform(1, 5) * np.pi / height
                             + rng.uniform(0, 2 * np.pi))[:, None])
            mask = ((wave + 1) / 2 > 0.6).astype(np.float32)

        mask = cv2.GaussianBlur(mask, (15, 15), 0)[..., None]
        return (past * mask + frame * (1 - mask)).astype(np.uint8)

    # --- Dimensionen ---

    def _corrupt_reality(self, frame: np.ndarray) -> np.ndarray:
        """Dimension 0: verschobene Streifen, Kanalversatz, Pixelblöcke, Rauschen"""
        rng = self.rng
        height, width = frame.shape[:2]
        result = frame.copy()
        intensity = rng.uniform(0.2, 0.8)

        for _ in range(int(rng.integers(3, 12))):
            band = rand_int(rng, 10, height // 4)
            y = rand_int(rng, 0, height - band)
            shift = rand_int(rng, 5, width // 3) * (1 if rng.random() > 0.5 else -1)
            result[y:y + band] = np.roll(result[y:y + band], shift, axis=1)

        if rng.random() < 0.7:
            r, g, b = cv2.split(result)
            r = np.roll(r, int(rng.integers(5, 20)), axis=1)
            g = np.roll(g, int(rng.integers(5, 20)), axis=0)
            result = cv2.merge([r, g, b])

        for _ in range(int(rng.integers(2, 6))):
            x, y, bw, bh = random_box(rng, width, height, 50, 150)
            pixel = int(rng.integers(5, 20))
            small = cv2.resize(result[y:y + bh, x:x + bw], (max(1, bw // pixel), max(1, bh // pixel)))
            result[y:y + bh, x:x + bw] = cv2.resize(small, (bw, bh), interpolation=cv2.INTER_NEAREST)

        if rng.random() < 0.5:
            for _ in range(int(rng.integers(10, 50))):
                y = int(rng.integers(0, height))
                result[y] = np.roll(result[y], rand_int(rng, 10, width // 2), axis=0)

        noise = rng.standard_normal(result.shape, dtype=np.float32) * (25 * intensity)
        return np.clip(result.astype(np.float32) + noise, 0, 255).astype(np.uint8)

    def _digital_decay(self, frame: np.ndarray) -> np.ndarray:
        """Dimension 1: Kompressionsartefakte, Datenverlust, Bitfehler, Interlacing"""
        rng = self.rng
        height, width = frame.shape[:2]
        result = frame.copy()
        decay = rng.uniform(0.3, 0.7)

        if rng.random() < 0.8:
            for _ in range(int(10 * decay)):
                x, y, size, _ = random_box(rng, width, height, 15, 60)
                size = min(size, height - y)
                if size < 2:
                    continue
                block = result[y:y + size, x:x + size]
                kind = rng.random()
                if kind < 0.25:
                    small = cv2.resize(block, (max(1, size // 8), max(1, size // 8)))
                    block = cv2.resize(small, (size, size), interpolation=cv2.INTER_NEAREST)
                elif kind < 0.5:
                    factor = int(rng.integers(3, 8))
                    block = (block // factor) * factor
                elif kind < 0.75:
                    gray = cv2.cvtColor(cv2.cvtColor(block, cv2.COLOR_RGB2GRAY), cv2.COLOR_GRAY2RGB)
                    factor = int(rng.integers(2, 6))
                    block = (gray // factor) * factor
                else:
                    block = shift_image(block, int(rng.integers(-10, 11)), int(rng.integers(-10, 11)),
                                        cv2.BORDER_CONSTANT)
                result[y:y + size, x:x + size] = block

        if rng.random() < 0.7:
            for _ in range(int(20 * decay)):
                y = int(rng.integers(0, height))
                length = rand_int(rng, max(1, width // 5), width)
                x = rand_int(rng, 0, width - length)
                kind = rng.random()
                if kind < 0.3:
                    result[y, x:x + length] = 0 if rng.random() < 0.6 else 255
                elif kind < 0.6:
                    result[y, x:x + length] = rng.integers(0, 256, (length, 3), dtype=np.uint8)
                else:
                    result[y, x:x + length] = result[y, x:x + length][rng.permutation(length)]

        if rng.random() < 0.5:
            count = int(100 * decay)
            ys = rng.integers(0, height, count)
            xs = rng.integers(0, width, count)
            channels = rng.integers(0, 3, count)
            result[ys, xs, channels] ^= (1 << rng.integers(0, 8, count)).astype(np.uint8)

        if rng.random() < 0.6 and height >= 8 and width >= 8:
            checker = ((np.indices((8, 8)).sum(axis=0) % 2) * 255).astype(np.uint8)
            gradient = np.repeat((np.arange(8) * 255 // 7).astype(np.uint8)[:, None], 8, axis=1)
            for _ in range(int(15 * decay)):
                x = int(rng.integers(0, width - 7))
                y = int(rng.integers(0, height - 7))
                kind = rng.random()
                if kind < 0.33:
                    pattern = checker
                elif kind < 0.66:
                    pattern = gradient
                else:
                    pattern = np.full((8, 8), int(rng.integers(0, 256)), dtype=np.uint8)
                result[y:y + 8, x:x + 8] = pattern[..., None]

        if rng.random() < 0.4:
            shift = rand_int(rng, 5, max(6, width // 3)) * (1 if rng.random() > 0.5 else -1)
            result[::2] = np.roll(result[::2], shift, axis=1)
        return result

    def _quantum_uncertainty(self, frame: np.ndarray) -> np.ndarray:
        """Dimension 2: Unschärfewolken, Quantensprünge, Interferenz, Tunneln, Überlagerung"""
        rng = self.rng
        height, width = frame.shape[:2]
        result = frame.copy()
        uncertainty = rng.uniform(0.2, 0.8)

        if rng.random() < 0.7:
            for _ in range(int(rng.integers(3, 8))):
                x, y, bw, bh = random_box(rng, width, height, 50, 200)
                strength = max(3, int(15 * (1.0 - (bw * bh) / (width * height)) * uncertainty))
                region = result[y:y + bh, x:x + bw]
                blurred = cv2.GaussianBlur(region, (strength * 2 + 1, strength * 2 + 1), 0)
                alpha = rng.uniform(0.4, 0.9)
                result[y:y + bh, x:x + bw] = cv2.addWeighted(region, 1 - alpha, blurred, alpha, 0)

        if rng.random() < 0.6:
            for _ in range(int(20 * uncertainty)):
                sx, sy, bw, bh = random_box(rng, width, height, 5, 20)
                dx = rand_int(rng, 0, width - bw + 1)
                dy = rand_int(rng, 0, height - bh + 1)
                source = result[sy:sy + bh, sx:sx + bw].copy()
                target = result[dy:dy + bh, dx:dx + bw]
                keep = rng.random((bh, bw, 1)) < 0.7
                result[dy:dy + bh, dx:dx + bw] = np.where(keep, source, target)

        if rng.random() < 0.4:
            scale = rng.uniform(10, 30)
            x = np.arange(width, dtype=np.float32)
            y = np.arange(height, dtype=np.float32)
            interference = (np.sin(x / scale + rng.random() * 10)[None, :]
                            + np.sin(y / scale + rng.random() * 10)[:, None]
                            + np.cos((x[None, :] + y[:, None]) / scale + rng.random() * 10)) / 3.0
            result = scale_image(result, (1.0 + interference * 0.15 * uncertainty).astype(np.float32))

        if rng.random() < 0.5:
            count = int(100 * uncertainty)
            distance = max(1, int(min(width, height) * 0.3 * uncertainty))
            sx = rng.integers(0, width, count)
            sy = rng.integers(0, height, count)
            tx = np.clip(sx + rng.integers(-distance, distance + 1, count), 0, width - 1)
            ty = np.clip(sy + rng.integers(-distance, distance + 1, count), 0, height - 1)
            result[ty, tx] = result[sy, sx]

        if rng.random() < 0.3:
            for _ in range(int(rng.integers(1, 4))):
                x, y, bw, bh = random_box(rng, width, height, 50, 200)
                region = result[y:y + bh, x:x + bw]
                xs = np.arange(bw, dtype=np.float32)
                ys = np.arange(bh, dtype=np.float32)
                mask = np.zeros((bh, bw), dtype=np.float32)
                for _ in range(10):
                    freq = rng.uniform(0.01, 0.1)
                    phase = rng.uniform(0, 2 * np.pi)
                    mask += 0.1 * np.sin(freq * xs + phase)[None, :] * np.cos(freq * ys + phase)[:, None]
                span = mask.max() - mask.min()
                mask = ((mask - mask.min()) / span if span > 0 else np.zeros_like(mask))[..., None]
                result[y:y + bh, x:x + bw] = (region * (1 - mask) + (255 - region) * mask).astype(np.uint8)
        return result

    def _void_intrusion(self, frame: np.ndarray) -> np.ndarray:
        """Dimension 3: Die Leere saugt das Bild an, mit dunklen Flecken und Adern"""
        rng = self.rng
        height, width = frame.shape[:2]
        result = frame.copy()
        void_intensity = rng.uniform(0.4, 0.9)

        if rng.random() < 0.8:
            cx = rand_int(rng, width // 4, width * 3 // 4)
            cy = rand_int(rng, height // 4, height * 3 // 4)
            max_radius = max(4, int(min(width, height) * 0.4 * void_intensity))

            mask = np.zeros((height, width), dtype=np.float32)
            for _ in range(int(rng.integers(3, 8))):
                center = (cx + rand_int(rng, -max_radius // 2, max_radius // 2),
                          cy + rand_int(rng, -max_radius // 2, max_radius // 2))
                cv2.circle(mask, center, rand_int(rng, max_radius // 3, max_radius), 1.0, -1)
            mask = cv2.GaussianBlur(mask, (21, 21), max_radius / 5)
            if mask.max() > 0:
                mask /= mask.max()

            # Sog zum Zentrum
            xs, ys = self.grid.get(height, width)
            dx, dy = xs - cx, ys - cy
            dist = np.hypot(dx, dy)
            pull = np.where(dist < max_radius * 2, 5.0 * mask * (1.0 - dist / (max_radius * 2)), 0.0)
            safe = np.maximum(dist, 1e-6)
            result = cv2.remap(result, xs - dx / safe * pull, ys - dy / safe * pull,
                               cv2.INTER_LINEAR, borderMode=cv2.BORDER_REFLECT)

            weight = (mask * void_intensity)[..., None]
            void_color = np.array([5, 0, 10], dtype=np.float32)
            result = (result * (1.0 - weight) + void_color * weight).astype(np.uint8)

        for _ in range(int(rng.integers(2, 8))):
            result = self._void_spot(result)

        if rng.random() < 0.4:
            for _ in range(int(rng.integers(3, 10))):
                center = (int(rng.integers(0, width)), int(rng.integers(0, height)))
                radius = int(rng.integers(2, 8))
                if rng.random() < 0.5:
                    cv2.circle(result, center, radius, (0, 0, 0), -1)
                else:
                    axes = (radius, int(radius * rng.uniform(0.5, 2.0)))
                    cv2.ellipse(result, center, axes, int(rng.integers(0, 180)), 0, 360, (0, 0, 0), -1)

        if rng.random() < 0.6:
            budget = [300]   # begrenzt die Zahl der Aderlinien
            for _ in range(int(rng.integers(2, 6))):
                self._draw_void_veins(result, int(rng.integers(0, width)), int(rng.integers(0, height)),
                                      0, int(rng.integers(3, 7)), budget)

        if rng.random() < 0.7:
            hsv = cv2.cvtColor(result, cv2.COLOR_RGB2HSV)
            dark = cv2.GaussianBlur((cv2.cvtColor(result, cv2.COLOR_RGB2GRAY) < 30).astype(np.float32), (21, 21), 0)
            hsv[:, :, 1] = (hsv[:, :, 1] * (1.0 - dark * 0.8)).astype(np.uint8)
            result = cv2.cvtColor(hsv, cv2.COLOR_HSV2RGB)
        return result

    def _void_spot(self, image: np.ndarray) -> np.ndarray:
        """Ein unregelmäßiger dunkler oder invertierter Fleck (nur im betroffenen Ausschnitt gerechnet)"""
        rng = self.rng
        height, width = image.shape[:2]
        sx, sy = int(rng.integers(0, width)), int(rng.integers(0, height))
        radius = int(rng.integers(5, 30))
        pad = radius * 2 + 16
        x0, x1 = max(0, sx - pad), min(width, sx + pad)
        y0, y1 = max(0, sy - pad), min(height, sy + pad)

        mask = np.zeros((y1 - y0, x1 - x0), dtype=np.float32)
        cv2.circle(mask, (sx - x0, sy - y0), radius, 1.0, -1)
        for angle in np.radians(np.arange(0, 360, 20)):
            r = radius * (0.7 + 0.6 * rng.random())
            px = int(np.clip(sx + r * np.cos(angle), 0, width - 1)) - x0
            py = int(np.clip(sy + r * np.sin(angle), 0, height - 1)) - y0
            cv2.circle(mask, (px, py), int(radius * 0.4), 1.0, -1)
        mask = cv2.GaussianBlur(mask, (15, 15), radius / 3)
        if mask.max() <= 0:
            return image
        mask = (mask / mask.max())[..., None]

        region = image[y0:y1, x0:x1].astype(np.float32)
        if rng.random() < 0.7:
            color = np.array([int(rng.integers(0, 15)), 0, int(rng.integers(0, 20))], dtype=np.float32)
            blended = region * (1.0 - mask * 0.9) + color * mask * 0.9
        else:
            blended = region * (1.0 - mask * 0.7) + (255 - region) * mask * 0.7
        image[y0:y1, x0:x1] = blended.astype(np.uint8)
        return image

    def _draw_void_veins(self, image: np.ndarray, x: int, y: int, depth: int, max_depth: int,
                         budget: List[int]) -> None:
        """Rekursiv verästelte schwarze Adern"""
        height, width = image.shape[:2]
        if depth >= max_depth or budget[0] <= 0 or not (0 <= x < width and 0 <= y < height):
            return
        budget[0] -= 1
        rng = self.rng
        length = int(rng.integers(10, 40))
        angle = rng.uniform(0, 2 * np.pi)
        end_x = int(np.clip(x + length * np.cos(angle), 0, width - 1))
        end_y = int(np.clip(y + length * np.sin(angle), 0, height - 1))
        cv2.line(image, (x, y), (end_x, end_y), (0, 0, 0), max(1, 3 - depth))
        for _ in range(int(rng.integers(1, 4))):
            t = rng.uniform(0.3, 1.0)
            self._draw_void_veins(image, int(x + t * (end_x - x)), int(y + t * (end_y - y)),
                                  depth + 1, max_depth, budget)

    def _hyperbolic_distortion(self, frame: np.ndarray) -> np.ndarray:
        """Dimension 4: gekrümmter Raum, Perspektivebenen, Spiegelungen, Streifen, Farbfaltung"""
        rng = self.rng
        height, width = frame.shape[:2]
        result = frame.copy()
        intensity = rng.uniform(0.3, 0.9)

        # 1. Hyperbolische Koordinatenverzerrung
        if rng.random() < 0.8:
            cx = width // 2 + rand_int(rng, -width // 4, width // 4)
            cy = height // 2 + rand_int(rng, -height // 4, height // 4)
            k1 = rng.uniform(-0.3, 0.3) * intensity
            k2 = rng.uniform(-0.2, 0.2) * intensity
            rotation = rng.uniform(0, 2 * np.pi)
            scale = rng.uniform(0.7, 1.3)

            xs, ys = self.grid.get(height, width)
            nx = (xs - cx) / width
            ny = (ys - cy) / height
            r = np.hypot(nx, ny)
            theta = np.arctan2(ny, nx) + rotation
            rd = r + k1 * np.tanh(r * 4) + k2 * (1 / (r + 0.1) - 1)
            map_x = np.where(r > 0, cx + rd * np.cos(theta) * width * scale, xs).astype(np.float32)
            map_y = np.where(r > 0, cy + rd * np.sin(theta) * height * scale, ys).astype(np.float32)
            result = cv2.remap(result, map_x, map_y, cv2.INTER_LINEAR, borderMode=cv2.BORDER_REFLECT)

        # 2. Bildteile in verschiedenen Raumebenen
        if rng.random() < 0.5:
            for _ in range(int(rng.integers(2, 5))):
                x, y, rw, rh = random_box(rng, width, height, max(2, width // 4), max(2, width // 2))
                rh = min(rh, max(2, height // 2))
                y = min(y, height - rh)
                factor = int(min(rw, rh) * 0.3 * intensity)
                if factor < 1 or rw < 2 or rh < 2:
                    continue
                src = np.float32([[0, 0], [rw - 1, 0], [rw - 1, rh - 1], [0, rh - 1]])
                dst = (src + rng.integers(-factor, factor, (4, 2))).astype(np.float32)
                matrix = cv2.getPerspectiveTransform(src, dst)
                result[y:y + rh, x:x + rw] = cv2.warpPerspective(result[y:y + rh, x:x + rw], matrix, (rw, rh))

        # 3. Möbius-artige Spiegelungen mit Farbtonumkehr
        if rng.random() < 0.3:
            for _ in range(int(rng.integers(1, 4))):
                x, y, bw, bh = random_box(rng, width, height, 50, 200)
                region = result[y:y + bh, x:x + bw]
                kind = int(rng.integers(0, 3))
                region = cv2.flip(region, 1) if kind == 0 else cv2.flip(region, 0) if kind == 1 \
                    else cv2.rotate(region, cv2.ROTATE_180)
                if rng.random() < 0.5:
                    hsv = cv2.cvtColor(region, cv2.COLOR_RGB2HSV)
                    hsv[:, :, 0] = ((hsv[:, :, 0].astype(np.int32) + 90) % 180).astype(np.uint8)
                    region = cv2.cvtColor(hsv, cv2.COLOR_HSV2RGB)
                result[y:y + bh, x:x + bw] = region

        # 4. Streifenweise nicht-euklidische Manipulationen
        if rng.random() < 0.4:
            vertical = rng.random() < 0.5
            # Vertikale Streifen = horizontale Streifen im transponierten Bild
            work = np.ascontiguousarray(result.transpose(1, 0, 2)) if vertical else result
            self._manipulate_stripes(work, int(rng.integers(4, 12)))
            result = np.ascontiguousarray(work.transpose(1, 0, 2)) if vertical else work

        # 5. Farbige Raumfaltung
        if rng.random() < 0.6:
            xs, ys = self.grid.get(height, width)
            kind = int(rng.integers(0, 3))
            if kind == 0:
                cx = width // 2 + rand_int(rng, -width // 4, width // 4)
                cy = height // 2 + rand_int(rng, -height // 4, height // 4)
                max_radius = np.hypot(width, height) / 2
                gradient = 0.5 + 0.5 * np.sin(np.hypot(xs - cx, ys - cy) / max_radius * np.pi * rng.integers(2, 5))
            elif kind == 1:
                angle = rng.uniform(0, np.pi)
                frequency = rng.uniform(5, 15) * np.pi / max(width, height)
                gradient = 0.5 + 0.5 * np.sin((xs * np.cos(angle) + ys * np.sin(angle)) * frequency)
            else:
                dx, dy = xs - width // 2, ys - height // 2
                gradient = 0.5 + 0.5 * np.sin(np.arctan2(dy, dx) * rng.integers(3, 8) + np.hypot(dx, dy) / 20)
            gradient = cv2.GaussianBlur(gradient.astype(np.float32), (15, 15), 0)
            colormap = [cv2.COLORMAP_JET, cv2.COLORMAP_VIRIDIS, cv2.COLORMAP_PLASMA][int(rng.integers(0, 3))]
            heatmap = to_rgb_colormap((gradient * 255).astype(np.uint8), colormap)
            alpha = rng.uniform(0.3, 0.7) * intensity
            result = cv2.addWeighted(result, 1 - alpha, heatmap, alpha, 0)
        return result

    def _manipulate_stripes(self, image: np.ndarray, count: int) -> None:
        """Verschiebt, skaliert oder dreht horizontale Streifen (in place)"""
        rng = self.rng
        height, width = image.shape[:2]
        stripe = max(1, height // count)
        for i in range(count):
            if rng.random() >= 0.5:
                continue
            y0, y1 = i * stripe, min((i + 1) * stripe, height)
            if y1 <= y0:
                continue
            band = image[y0:y1].copy()
            kind = int(rng.integers(0, 3))
            if kind == 0:
                image[y0:y1] = np.roll(band, rand_int(rng, -width // 4, width // 4), axis=1)
            elif kind == 1:
                new_width = max(1, int(width * rng.uniform(0.7, 1.3)))
                resized = cv2.resize(band, (new_width, y1 - y0))
                if new_width < width:
                    pad = (width - new_width) // 2
                    image[y0:y1] = 0
                    image[y0:y1, pad:pad + new_width] = resized
                else:
                    crop = (new_width - width) // 2
                    image[y0:y1] = resized[:, crop:crop + width]
            else:
                # Drehpunkt im Streifen selbst (nicht in Bildkoordinaten)
                matrix = cv2.getRotationMatrix2D((width // 2, (y1 - y0) // 2),
                                                 rng.uniform(-10, 10), rng.uniform(0.9, 1.1))
                image[y0:y1] = cv2.warpAffine(band, matrix, (width, y1 - y0), borderMode=cv2.BORDER_REFLECT)

    def _apply_reality_breach(self, frame: np.ndarray) -> np.ndarray:
        """Leuchtender, verästelter Riss mit Verwirbelung und Farbfehlern"""
        rng = self.rng
        height, width = frame.shape[:2]
        result = frame.copy()
        self.breach_coordinates = []

        side = int(rng.integers(0, 4))
        x, y = [(rng.integers(0, width), 0), (width - 1, rng.integers(0, height)),
                (rng.integers(0, width), height - 1), (0, rng.integers(0, height))][side]
        x, y = float(x), float(y)
        target_x = width // 2 + rand_int(rng, -width // 4, width // 4)
        target_y = height // 2 + rand_int(rng, -height // 4, height // 4)
        chaos = rng.uniform(0.3, 0.8)
        segments = int(rng.integers(10, 30))
        main_points = [(int(x), int(y))]

        for i in range(segments):
            angle = np.arctan2(target_y - y, target_x - x) + rng.uniform(-np.pi / 3, np.pi / 3) * chaos
            length = rand_int(rng, 10, max(11, int(min(width, height) * 0.1)))
            x = float(np.clip(x + length * np.cos(angle), 0, width - 1))
            y = float(np.clip(y + length * np.sin(angle), 0, height - 1))
            main_points.append((int(x), int(y)))

            if rng.random() < 0.3 and i > 1:
                bx, by, branch_angle = x, y, angle + rng.uniform(-np.pi / 2, np.pi / 2)
                branch = []
                for _ in range(rand_int(rng, 3, max(4, segments // 3))):
                    branch_angle += rng.uniform(-np.pi / 4, np.pi / 4) * chaos
                    step = rand_int(rng, 5, max(6, length))
                    bx = float(np.clip(bx + step * np.cos(branch_angle), 0, width - 1))
                    by = float(np.clip(by + step * np.sin(branch_angle), 0, height - 1))
                    branch.append((int(bx), int(by)))
                self.breach_coordinates.extend(branch)
                if len(branch) > 1:
                    color = (int(rng.integers(50, 150)), int(rng.integers(50, 200)), int(rng.integers(100, 256)))
                    cv2.polylines(result, [np.array(branch, np.int32)], False, color, int(rng.integers(1, 4)))

        self.breach_coordinates.extend(main_points)
        color = (int(rng.integers(150, 256)), int(rng.integers(200, 256)), int(rng.integers(200, 256)))
        cv2.polylines(result, [np.array(main_points, np.int32)], False, color, int(rng.integers(3, 9)))

        # Aura
        overlay = np.zeros_like(result)
        for px, py in self.breach_coordinates:
            glow = tuple(int(c) for c in rng.integers(0, 256, 3))
            cv2.circle(overlay, (px, py), int(rng.integers(5, 20)), glow, -1)
        result = cv2.addWeighted(result, 0.7, cv2.GaussianBlur(overlay, (21, 21), 0), 0.5, 0)

        # Verwirbelung um die Risspunkte
        xs, ys = self.grid.get(height, width)
        map_x, map_y = xs.copy(), ys.copy()
        for px, py in self.breach_coordinates:
            radius = int(rng.integers(10, 50))
            strength = rng.uniform(5.0, 15.0)
            x0, x1 = max(0, px - radius), min(width, px + radius + 1)
            y0, y1 = max(0, py - radius), min(height, py + radius + 1)
            lx, ly = xs[y0:y1, x0:x1] - px, ys[y0:y1, x0:x1] - py
            dist = np.hypot(lx, ly)
            factor = np.where(dist < radius, (1.0 - dist / radius) * strength, 0.0)
            angle = np.arctan2(ly, lx) + np.pi / 2
            map_x[y0:y1, x0:x1] += factor * np.cos(angle)
            map_y[y0:y1, x0:x1] += factor * np.sin(angle)
        result = cv2.remap(result, map_x, map_y, cv2.INTER_LINEAR, borderMode=cv2.BORDER_REFLECT)

        # Punktuelle chromatische Aberration
        if rng.random() < 0.7:
            r, g, b = cv2.split(result)
            for px, py in self.breach_coordinates:
                radius = int(rng.integers(20, 60))
                x0, x1 = max(0, px - radius), min(width, px + radius)
                y0, y1 = max(0, py - radius), min(height, py + radius)
                if x1 > x0 and y1 > y0:
                    r[y0:y1, x0:x1] = shift_image(r[y0:y1, x0:x1].copy(), int(rng.integers(-5, 6)),
                                                  int(rng.integers(-5, 6)), cv2.BORDER_CONSTANT)
                    g[y0:y1, x0:x1] = shift_image(g[y0:y1, x0:x1].copy(), int(rng.integers(-5, 6)),
                                                  int(rng.integers(-5, 6)), cv2.BORDER_CONSTANT)
            result = cv2.merge([r, g, b])

        # Pixel-Anomalien entlang des Risses
        if rng.random() < 0.5 and self.breach_coordinates:
            for _ in range(int(rng.integers(3, 10))):
                cx, cy = self.breach_coordinates[int(rng.integers(0, len(self.breach_coordinates)))]
                gw, gh = min(int(rng.integers(10, 50)), width), min(int(rng.integers(10, 50)), height)
                x = int(np.clip(cx + rng.integers(-30, 31), 0, width - gw))
                y = int(np.clip(cy + rng.integers(-30, 31), 0, height - gh))
                region = result[y:y + gh, x:x + gw]
                kind = rng.random()
                if kind < 0.33:
                    region = np.roll(region, int(rng.integers(5, 20)), axis=1)
                elif kind < 0.66:
                    region = region.copy()
                    region[:, :, int(rng.integers(0, 3))] = int(rng.integers(0, 256))
                else:
                    pixel = int(rng.integers(5, 15))
                    small = cv2.resize(region, (max(1, gw // pixel), max(1, gh // pixel)))
                    region = cv2.resize(small, (gw, gh), interpolation=cv2.INTER_NEAREST)
                result[y:y + gh, x:x + gw] = region
        return result
