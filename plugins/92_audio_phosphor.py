"""
Plugin: Phosphor-Nachleuchten

Klanganteile glühen nach wie der Leuchtstoff einer Bildröhre. Beispiel für einen
Ton-Effekt als Plugin: eine Klasse, die von StftAudioEffect erbt.
"""

import numpy as np

from bildeffekte import StftAudioEffect


class PhosphorGlowEffect(StftAudioEffect):
    description = ("Laute Klanganteile glühen nach wie der Leuchtstoff einer Bildröhre und sinken beim "
                   "Abkühlen in der Tonhöhe. Helles Bild = längeres Nachleuchten.")
    COOLING_SEMITONES_PER_SECOND = 3.0

    @property
    def name(self) -> str:
        return "Phosphor-Nachleuchten"

    def on_reset(self) -> None:
        self._glow = np.zeros(len(self.freqs))
        self._phase = np.zeros(len(self.freqs))

    def process_frame(self, mag, phase, video):
        brightness = video.brightness if video.valid else 0.4
        seconds = 0.8 + 2.7 * brightness
        base = 0.001 ** (1.0 / (seconds * self.hops_per_second))
        decay = base ** (1.0 + self.freqs / 3000.0)          # Höhen kühlen schneller ab
        factor = 2.0 ** (-self.COOLING_SEMITONES_PER_SECOND / 12.0 / self.hops_per_second)
        cooled = np.interp(self.bins / factor, self.bins, self._glow, right=0.0)
        self._glow = np.maximum(cooled * decay, mag * 0.9)

        self._phase += self.bin_advance
        glowing = self._glow > mag
        self._phase = np.where(glowing, self._phase, phase)
        return np.maximum(mag, self._glow), self._phase
