"""Sechs experimentelle Live-Toneffekte mit begrenztem Zustand pro Instanz."""

import numpy as np

from bildeffekte import StftAudioEffect


class FrequencyOrigami(StftAudioEffect):
    description = "Das Spektrum wird an einer wandernden Falz gespiegelt; hohe Geräusche landen tief und umgekehrt."
    name = "Frequenz-Origami"

    def on_reset(self):
        self.tick = 0

    def process_frame(self, mag, phase, video):
        self.tick += 1
        pivot = int(90 + 35 * np.sin(self.tick * 0.035) + (video.hue / 360 * 40 if video.valid else 0))
        source = np.arange(1, min(len(mag), pivot * 4))
        target = np.abs(2 * pivot - source).clip(1, len(mag) - 1)
        folded = np.zeros_like(mag)
        np.add.at(folded, target, mag[source])
        folded *= 0.45
        return 0.35 * mag + 0.65 * folded, phase


class SpectralRain(StftAudioEffect):
    description = "Obertöne tropfen über mehrere Bilder nach unten; Anschläge ziehen schimmernde Frequenzspuren."
    name = "Spektral-Regen"

    def on_reset(self):
        self.trail = np.zeros(len(self.freqs), dtype=np.float64)
        self.tick = 0

    def process_frame(self, mag, phase, video):
        self.tick += 1
        fall = 2 + self.tick % 4
        self.trail[:-fall] = 0.88 * self.trail[fall:] + 0.20 * mag[fall:]
        self.trail[-fall:] = 0
        self.trail[:3] = 0
        return 0.45 * mag + 0.55 * self.trail, phase


class PhaseVortex(StftAudioEffect):
    description = "Ein wandernder Wirbel verdreht die Phasen lokaler Frequenzwolken; Stimmen werden flüssig und fremd."
    name = "Phasen-Wirbel"

    def on_reset(self):
        self.tick = 0
        self.rotation = np.zeros(len(self.freqs), dtype=np.float64)

    def process_frame(self, mag, phase, video):
        self.tick += 1
        center = 80 + 60 * np.sin(self.tick * 0.025)
        distance = (self.bins - center) / 55
        strength = 0.5 + (video.activity * 5 if video.valid else 0.4)
        self.rotation += np.exp(-distance * distance) * strength * np.sin(self.bins * 0.14 + self.tick * 0.1)
        return mag, phase + self.rotation


class PulseMosaic(StftAudioEffect):
    description = "Frequenzkacheln öffnen und schließen sich in gegeneinander laufenden Takten; die Stimme zerfällt in Muster."
    name = "Puls-Mosaik"

    def on_reset(self):
        self.tick = 0
        self.groups = (np.log2(1 + self.freqs / 55) * 3).astype(np.int32)

    def process_frame(self, mag, phase, video):
        self.tick += 1
        speed = 1 + (int(video.activity * 10) if video.valid else 0)
        gate = ((self.groups * 3 + (self.tick * speed) // 3) % 7 < 3).astype(np.float64)
        return mag * (0.12 + 0.88 * gate), phase


class HarmonicMirage(StftAudioEffect):
    description = "Frequenzanteile erscheinen gleichzeitig als ungerade, verschobene Spiegelungen: ein unmöglicher Chor."
    name = "Harmonische Fata Morgana"

    def on_reset(self):
        self.tick = 0

    def process_frame(self, mag, phase, video):
        self.tick += 1
        result = 0.42 * mag
        limit = len(mag) - 1
        source = np.arange(1, limit // 2)
        shift = 1.37 + 0.12 * np.sin(self.tick * 0.018)
        for ratio, amount in ((shift, 0.42), (2.13, 0.25)):
            target = np.minimum((source * ratio).astype(np.int32), limit)
            np.add.at(result, target, mag[source] * amount)
        return result, phase


class TimeIris(StftAudioEffect):
    description = "Tiefe und hohe Frequenzen blicken unterschiedlich weit zurück; bei Bildbewegung öffnet sich die Zeitblende."
    name = "Zeit-Iris"

    def on_reset(self):
        self.history = np.zeros((24, len(self.freqs)), dtype=np.complex64)
        self.tick = 0
        self.bin_delay = np.clip((np.log2(1 + self.freqs / 80) * 3).astype(np.int32), 1, 22)

    def process_frame(self, mag, phase, video):
        spectrum = mag * np.exp(1j * phase)
        self.history[self.tick % len(self.history)] = spectrum
        old = self.history[(self.tick - self.bin_delay) % len(self.history), np.arange(len(mag))]
        self.tick += 1
        old_mix = 0.75 - (0.35 * min(1.0, video.activity * 8) if video.valid else 0)
        mixed = (1 - old_mix) * spectrum + old_mix * old
        return np.abs(mixed), np.angle(mixed)
