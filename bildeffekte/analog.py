"""Gemeinsame Bildbandbreite und Transportfehler für analoge Videoeffekte."""

import cv2
import numpy as np


def old_film_detail(frame: np.ndarray) -> np.ndarray:
    """Weicher Projektor-/Kopien-Look ohne scharfe HD-Kameradetails."""
    height, width = frame.shape[:2]
    scale = min(1.0, 420 / max(width, 1), 330 / max(height, 1))
    size = (max(1, round(width * scale)), max(1, round(height * scale)))
    small = cv2.resize(frame, size, interpolation=cv2.INTER_AREA)
    small = cv2.GaussianBlur(small, (3, 3), 0.7)
    return small


def vhs_signal(frame: np.ndarray, rng: np.random.Generator, tick: int,
               wear: float = 1.0, native: bool = False) -> np.ndarray:
    """VHS-ähnliches Luma/Chroma-Bandlimit plus zeitabhängige Bandfehler.

    Das ist eine visuelle Simulation, keine exakte PAL-/NTSC-Dekodierung.
    """
    height, width = frame.shape[:2]
    if not height or not width:
        return frame.copy()
    # Bei 4:3 entsprechen rund 320-384 Bildpunkte der begrenzten VHS-Detailschärfe.
    scale = min(1.0, 384 / width, 432 / height)
    low_w, low_h = max(1, round(width * scale)), max(1, round(height * scale))
    ycc = cv2.cvtColor(frame, cv2.COLOR_RGB2YCrCb)
    small = cv2.resize(ycc, (low_w, low_h), interpolation=cv2.INTER_AREA)
    luma = small[:, :, 0].astype(np.float32)
    # Rauschende, unterschiedlich helle Bandzeilen bleiben beim Hochskalieren sichtbar.
    line_noise = rng.normal(0, 2.2 * wear, (low_h, 1)).astype(np.float32)
    grain = rng.normal(0, 4.0 * wear, (low_h, low_w)).astype(np.float32)
    luma = np.clip(luma + line_noise + grain, 0, 255).astype(np.uint8)
    chroma_w = max(1, min(low_w, round(low_w / 7)))
    chroma_h = max(1, min(low_h, round(low_h / 2)))
    chroma = cv2.resize(small[:, :, 1:3], (chroma_w, chroma_h), interpolation=cv2.INTER_AREA)
    chroma = cv2.resize(chroma, (low_w, low_h), interpolation=cv2.INTER_LINEAR)
    chroma = np.clip(chroma.astype(np.int16) + rng.integers(-3, 4, (low_h, 1, 2)), 0, 255).astype(np.uint8)
    low = np.dstack((luma, chroma))
    # Zeilenweises Zeitbasiswackeln; bei Trackingverlust bewegt sich ein ganzes Band.
    wobble = np.round(1.2 * wear * np.sin(np.arange(low_h) * 0.093 + tick * 0.27)).astype(np.int32)
    if low_h > 4 and tick % 29 < 3:
        top = (tick * 7) % low_h
        wobble[top:min(low_h, top + max(2, low_h // 13))] += max(1, round(5 * wear))
    if low_w > 1:
        for row, shift in enumerate(wobble):
            if shift:
                low[row] = np.roll(low[row], int(shift), axis=0)
    if low_h > 5 and tick % 17 < 2:
        # Head-Switching und Dropout am unteren Bildrand.
        top = low_h - max(2, low_h // 18)
        low[top:] = np.roll(low[top:], max(1, low_w // 30), axis=1)
        noisy = rng.integers(0, 256, (min(2, low_h - top), low_w), dtype=np.uint8)
        low[top:top + len(noisy), :, 0] = noisy
    result = cv2.cvtColor(low, cv2.COLOR_YCrCb2RGB)
    return result if native else cv2.resize(result, (width, height), interpolation=cv2.INTER_LINEAR)
