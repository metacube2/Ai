"""
Plugin: Ton-Effekte im Frequenzbereich

Ausgelagert aus video.py – Effekte sind reine Plugins und kennen nur das Paket
`bildeffekte`.
"""

from typing import Any, Dict, List, Optional, Tuple

import time

import numpy as np

from bildeffekte import PROFILE_ROWS, StftAudioEffect, VideoFeatureSnapshot


class ImageStencilEffect(StftAudioEffect):
    description = ("Das Bild ist der Filter: Jede Bildzeile steuert ein Frequenzband "
                   "(oben hoch, unten tief). Helle Stellen lassen durch, dunkle schlucken.")
    MIN_HZ, MAX_HZ = 80.0, 12000.0
    FLOOR = 0.03

    @property
    def name(self) -> str:
        return "Bild-Schablone"

    def on_reset(self) -> None:
        position = np.clip(np.log(np.maximum(self.freqs, 1.0) / self.MIN_HZ) / np.log(self.MAX_HZ / self.MIN_HZ), 0, 1)
        self._row = (1.0 - position) * (PROFILE_ROWS - 1)
        self._gains = np.ones(len(self.freqs))

    def process_frame(self, mag, phase, video):
        if video.valid and video.row_profile is not None:
            profile = video.row_profile.astype(np.float64)
            lo, hi = profile.min(), profile.max()
            stretched = (profile - lo) / (hi - lo) if hi - lo > 0.05 else profile
            profile = 0.3 * profile + 0.7 * stretched
            target = self.FLOOR + (1 - self.FLOOR) * np.interp(self._row, np.arange(len(profile)), profile) ** 1.5
        else:
            target = np.ones(len(self.freqs))
        self._gains += 0.35 * (target - self._gains)
        return mag * self._gains, phase


class AudioDatamoshEffect(StftAudioEffect):
    description = ("Steht das Bild, friert der Klang ein. Bewegung zerreißt ihn: senkrecht verschiebt "
                   "die Tonhöhe, waagrecht verschmiert in Blöcken. Ein Bildschnitt ist ein Keyframe.")
    BLOCK_BINS = 16
    KEYFRAME_HOPS = 8

    @property
    def name(self) -> str:
        return "Audio-Datamosh"

    def on_reset(self) -> None:
        self._held: Optional[np.ndarray] = None
        self._phase = np.zeros(len(self.freqs))
        self._keyframe = 0
        self._last_cut: Optional[int] = None
        self._hops_since_key = 0

    def process_frame(self, mag, phase, video):
        if self._held is None:
            self._keyframe = self.KEYFRAME_HOPS
        if self._last_cut is not None and video.cut_count != self._last_cut:
            self._keyframe = self.KEYFRAME_HOPS
        self._last_cut = video.cut_count
        self._hops_since_key += 1
        if not video.valid and self._hops_since_key > 3 * self.hops_per_second:
            self._keyframe = self.KEYFRAME_HOPS     # ohne Video: alle 3 s ein Keyframe

        if self._keyframe > 0:
            self._keyframe -= 1
            self._hops_since_key = 0
            self._held = mag.copy()
            self._phase = phase.copy()
            return mag, phase

        held = self._held
        if video.valid:
            # Bild nach oben -> Ton höher; 1 % Bildhöhe pro Videobild ≈ 8 Halbtöne pro Sekunde
            semitones = -video.motion_y * 800.0 / self.hops_per_second
            if abs(semitones) > 0.005:
                ratio = 2.0 ** (semitones / 12.0)
                held = np.interp(self.bins / ratio, self.bins, held, left=0.0, right=0.0)
            smear = float(np.clip(abs(video.motion_x) * 12.0, 0, 1))
            if smear > 0.02:
                blended = held * (1 - smear) + mag * smear
                usable = len(blended) // self.BLOCK_BINS * self.BLOCK_BINS
                blocks = blended[:usable].reshape(-1, self.BLOCK_BINS).mean(axis=1).repeat(self.BLOCK_BINS)
                pick = self.rng.random(usable) < smear
                blended[:usable] = np.where(pick, blocks, blended[:usable])
                held = blended
        self._held = held * 0.9995
        self._phase = self._phase + self.bin_advance + self.rng.uniform(-0.2, 0.2, len(self._phase))
        return self._held, self._phase


class ColorVowelEffect(StftAudioEffect):
    description = ("Die Farben im Bild bestimmen den Vokal: Rot 'a', Gelb 'e', Grün 'i', Blau 'o', Violett 'u'. "
                   "Auch Geräusche beginnen zu sprechen. Graues Bild = neutral.")
    # Farbton (Grad) -> Formanten F1, F2, F3 (Hz)
    VOWELS = [(0, (800, 1200, 2500)), (72, (500, 1900, 2500)), (144, (300, 2300, 3000)),
              (216, (500, 900, 2400)), (288, (320, 800, 2300)), (360, (800, 1200, 2500))]
    BANDWIDTHS = np.array([90.0, 110.0, 160.0])
    AMPLITUDES = np.array([1.0, 0.7, 0.35])

    @property
    def name(self) -> str:
        return "Farb-Vokale"

    def on_reset(self) -> None:
        self._formants = np.array(self.VOWELS[0][1], dtype=np.float64)
        self._lfo = 0.0

    def _target(self, hue: float) -> np.ndarray:
        hues = [h for h, _ in self.VOWELS]
        table = np.array([f for _, f in self.VOWELS], dtype=np.float64)
        return np.array([np.interp(hue % 360.0, hues, table[:, i]) for i in range(3)])

    def process_frame(self, mag, phase, video):
        if video.valid:
            hue, strength = video.hue, float(np.clip(video.saturation * 2.5, 0, 1))
        else:
            self._lfo = (self._lfo + 360.0 * 0.1 / self.hops_per_second) % 360.0
            hue, strength = self._lfo, 1.0
        self._formants += 0.08 * (self._target(hue) - self._formants)

        # Eigene Klangfarbe des Eingangs glätten, dann Vokal-Hüllkurve aufprägen
        envelope = np.exp(np.convolve(np.log(mag + 1e-6), np.ones(25) / 25, mode='same'))
        flat = mag / envelope
        vowel = 0.05 + np.sum(self.AMPLITUDES[:, None]
                              / (1.0 + ((self.freqs[None, :] - self._formants[:, None])
                                        / (self.BANDWIDTHS[:, None] / 2)) ** 2), axis=0)
        shaped = flat * vowel
        shaped *= np.sqrt(np.sum(mag ** 2) / (np.sum(shaped ** 2) + 1e-12))
        return mag * (1 - strength) + shaped * strength, phase


class NegativeSoundEffect(StftAudioEffect):
    description = ("Das akustische Negativ: Das Lauteste wird entfernt, das Leise verstärkt – Atem, Raum, "
                   "Nebengeräusche treten hervor. Dunkles Bild = stärker.")
    MAX_BOOST_DB = 24.0

    @property
    def name(self) -> str:
        return "Negativ-Ton"

    def on_reset(self) -> None:
        self._long_rms = 0.01
        self._gains = np.ones(len(self.freqs))
        self._makeup = 1.0

    def process_frame(self, mag, phase, video):
        rms = float(np.sqrt(np.mean(mag ** 2)))
        if rms < 1e-7:
            return mag, phase
        strength = 0.6 + 0.4 * (1.0 - video.brightness) if video.valid else 0.8

        reference = float(np.percentile(mag, 70)) + 1e-9
        over = mag / reference
        target = np.where(over > 1.0, over ** (-1.5 * strength), np.minimum(1.0 / (over + 0.25), 4.0) ** strength)
        self._gains += 0.5 * (target - self._gains)

        self._long_rms = 0.995 * self._long_rms + 0.005 * rms
        dynamic = float(np.clip((self._long_rms / rms) ** strength, 10 ** (-12 / 20), 10 ** (self.MAX_BOOST_DB / 20)))
        out = mag * self._gains * dynamic

        # Was übrig bleibt, hörbar machen: Pegel Richtung 70 % des Eingangs, höchstens +24 dB, geglättet
        out_rms = float(np.sqrt(np.mean(out ** 2))) + 1e-12
        wanted = float(np.clip(0.7 * rms / out_rms, 1.0, 10 ** (self.MAX_BOOST_DB / 20)))
        self._makeup += 0.05 * (wanted - self._makeup)
        return out * self._makeup, phase


class HarmonyMagnetEffect(StftAudioEffect):
    description = ("Alle Frequenzen, auch Rauschen und Lärm, werden zu den Tönen eines Akkords gezogen. "
                   "Farbton wählt den Grundton, Helligkeit Dur (hell) oder Moll (dunkel).")
    FRAME = 4096
    HOP = 1024
    MAX_PEAKS = 80
    RESIDUAL = 0.12
    PROGRESSION = [(0, False), (9, True), (5, False), (7, False)]   # C, Am, F, G ohne Video

    @property
    def name(self) -> str:
        return "Harmonie-Magnet"

    def on_reset(self) -> None:
        self._prev_phase = np.zeros(len(self.freqs))
        self._out_phase = np.zeros(len(self.freqs))
        self._chord: Tuple[int, bool] = (0, False)
        self._chord_time = 0.0
        self._targets = self._chord_frequencies(*self._chord)
        self._progression_index = 0

    @staticmethod
    def _chord_frequencies(root: int, minor: bool) -> np.ndarray:
        intervals = (0, 3, 7) if minor else (0, 4, 7)
        notes = [m for m in range(28, 109) if (m - root) % 12 in intervals]   # ~41 Hz bis ~8 kHz
        return 440.0 * 2.0 ** ((np.array(notes) - 69) / 12.0)

    def _choose_chord(self, video: VideoFeatureSnapshot) -> None:
        now = time.time()
        if now - self._chord_time < 1.0:
            return
        if video.valid:
            chord = (int(round(video.hue / 30.0)) % 12, video.brightness < 0.45)
        elif now - self._chord_time >= 4.0:
            self._progression_index = (self._progression_index + 1) % len(self.PROGRESSION)
            chord = self.PROGRESSION[self._progression_index]
        else:
            return
        if chord != self._chord:
            self._chord = chord
            self._targets = self._chord_frequencies(*chord)
        self._chord_time = now

    def process_frame(self, mag, phase, video):
        self._choose_chord(video)
        n_bins = len(mag)
        bin_hz = self.sample_rate / self.FRAME

        # Momentanfrequenz jedes Bins aus der Phasendifferenz (Phase-Vocoder)
        deviation = phase - self._prev_phase - self.bin_advance
        deviation = (deviation + np.pi) % (2 * np.pi) - np.pi
        true_bins = self.bins + deviation * self.FRAME / (2 * np.pi * self.HOP)
        self._prev_phase = phase

        inner = mag[1:-1]
        candidates = np.where((inner > mag[:-2]) & (inner >= mag[2:]) & (inner > mag.max() * 0.01))[0] + 1
        if len(candidates) > self.MAX_PEAKS:
            candidates = candidates[np.argsort(mag[candidates])[-self.MAX_PEAKS:]]

        out_mag = mag * self.RESIDUAL
        out_phase = phase.copy()
        claimed = np.zeros(n_bins, dtype=bool)
        new_out_phase = phase.copy()
        for peak in candidates[np.argsort(-mag[candidates])]:
            freq = true_bins[peak] * bin_hz
            if freq <= 20:
                continue
            index = np.searchsorted(self._targets, freq)
            options = self._targets[max(0, index - 1):index + 1]
            target = options[np.argmin(np.abs(options - freq))]
            center = int(round(target / bin_hz))
            if center < 2 or center >= n_bins - 2 or claimed[center]:
                continue
            peak_phase = self._out_phase[center] + 2 * np.pi * target * self.HOP / self.sample_rate
            for offset in range(-2, 3):
                src, dst = peak + offset, center + offset
                if 0 <= src < n_bins:
                    if not claimed[dst]:
                        out_mag[dst] = 0.0
                    out_mag[dst] += mag[src]
                    out_phase[dst] = peak_phase + (phase[src] - phase[peak])
                    new_out_phase[dst] = out_phase[dst]
                    claimed[dst] = True
        self._out_phase = new_out_phase
        return out_mag, out_phase


class BlackHoleEffect(StftAudioEffect):
    description = ("Ein schwarzes Loch wandert durchs Spektrum: Frequenzen stürzen hinein, werden gedehnt, "
                   "rotverschoben und am Ereignishorizont verschluckt – nur Hawking-Rauschen entkommt. "
                   "Die hellste Bildzeile bestimmt, wo das Loch sitzt.")
    MIN_HZ, MAX_HZ = 120.0, 5000.0
    HORIZON_OCTAVES = 0.12

    @property
    def name(self) -> str:
        return "Schwarzes Loch"

    def on_reset(self) -> None:
        self._hole_hz = 600.0
        self._lfo = 0.0
        self._prev_mag = np.zeros(len(self.freqs))
        self._phase = np.zeros(len(self.freqs))
        self._octave = np.log2(np.maximum(self.freqs, 1.0))

    def _target_hole(self, video: VideoFeatureSnapshot) -> float:
        if video.valid and video.row_profile is not None:
            row = int(np.argmax(video.row_profile))
            position = 1.0 - row / (len(video.row_profile) - 1)       # oben = hoch
        else:
            self._lfo += 0.05 / self.hops_per_second
            position = 0.5 + 0.5 * np.sin(2 * np.pi * self._lfo)
        return self.MIN_HZ * (self.MAX_HZ / self.MIN_HZ) ** position

    def process_frame(self, mag, phase, video):
        self._hole_hz *= (self._target_hole(video) / self._hole_hz) ** 0.02   # träge Umlaufbahn
        distance = self._octave - np.log2(self._hole_hz)                        # in Oktaven
        pull = 0.85 / (1.0 + (np.abs(distance) / 0.6) ** 2)                     # Gravitation

        # Frequenzen fallen Richtung Loch (in Oktaven gestaucht) und verlieren Energie
        target_octave = self._octave - distance * pull
        target_bins = np.clip(np.round(2.0 ** target_octave * self.FRAME / self.sample_rate), 0, len(mag) - 1)
        pulled = np.zeros_like(mag)
        np.add.at(pulled, target_bins.astype(np.int64), mag * (1.0 - 0.6 * pull))

        # Ereignishorizont schluckt alles, Hawking-Strahlung entweicht als feines Rauschen
        horizon = np.abs(distance) < self.HORIZON_OCTAVES
        pulled[horizon] = 0.0
        rim = (np.abs(distance) >= self.HORIZON_OCTAVES) & (np.abs(distance) < 2 * self.HORIZON_OCTAVES)
        pulled[rim] += self.rng.random(int(rim.sum())) * mag.mean() * 0.8

        # Zeitdehnung ("Spaghettifizierung"): nahe am Loch verändert sich der Klang kaum noch
        inertia = np.clip(pull * 1.1, 0, 0.97)
        out = self._prev_mag * inertia + pulled * (1.0 - inertia)
        self._prev_mag = out

        self._phase += self.bin_advance
        near = pull > 0.3
        self._phase = np.where(near, self._phase, phase)
        return out, self._phase


class DreamMemoryEffect(StftAudioEffect):
    description = ("Der Ton wird laufend aus Schnipseln der letzten 30 Sekunden neu zusammengesetzt – jeweils aus dem "
                   "Moment, der am besten zur aktuellen Klangform passt. Dunkles Bild = ältere Erinnerungen.")
    MEMORY_SECONDS = 30.0
    BANDS = 40

    @property
    def name(self) -> str:
        return "Traum-Gedächtnis"

    def on_reset(self) -> None:
        size = int(self.MEMORY_SECONDS * self.hops_per_second)
        self._mags = np.zeros((size, len(self.freqs)), dtype=np.float16)
        self._envs = np.zeros((size, self.BANDS), dtype=np.float32)
        self._rms = np.zeros(size, dtype=np.float32)
        self._stored = 0
        self._write = 0
        self._last_match = -1
        self._phase = np.zeros(len(self.freqs))
        edges = np.geomspace(80, min(12000, self.sample_rate / 2 - 1), self.BANDS + 1)
        self._band_of_bin = np.clip(np.searchsorted(edges, self.freqs) - 1, 0, self.BANDS - 1)

    def _envelope(self, mag: np.ndarray) -> np.ndarray:
        energy = np.bincount(self._band_of_bin, weights=mag, minlength=self.BANDS)
        env = np.log1p(energy * 50.0)
        env -= env.mean()
        return (env / (np.linalg.norm(env) + 1e-9)).astype(np.float32)

    def process_frame(self, mag, phase, video):
        size = len(self._rms)
        env = self._envelope(mag)
        rms = float(np.sqrt(np.mean(mag ** 2)))

        brightness = video.brightness if video.valid else 0.6
        min_age = int((3.0 + (1.0 - brightness) * 20.0) * self.hops_per_second)
        out_mag, out_phase = mag, phase
        if self._stored > min_age + 10 and rms > 1e-6:
            ages = (self._write - np.arange(size) - 1) % size
            usable = (ages >= min_age) & (ages < self._stored)
            scores = self._envs @ env
            scores[~usable] = -np.inf
            if self._last_match >= 0:
                follow = (self._last_match + 1) % size
                if usable[follow]:
                    scores[follow] += 0.25              # Fortsetzung bevorzugen: längere, zusammenhängende Schnipsel
            best = int(np.argmax(scores))
            if np.isfinite(scores[best]):
                self._last_match = best
                memory = self._mags[best].astype(np.float64)
                out_mag = memory * (rms / (float(self._rms[best]) + 1e-9))
                self._phase += self.bin_advance + self.rng.uniform(-0.1, 0.1, len(self._phase))
                out_phase = self._phase

        self._mags[self._write] = mag
        self._envs[self._write] = env
        self._rms[self._write] = rms
        self._write = (self._write + 1) % size
        self._stored = min(self._stored + 1, size)
        return out_mag, out_phase
