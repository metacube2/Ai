#!/usr/bin/env python3
"""
Vericrypt-Style Audio Scrambler

Inspiriert vom Vericrypt 1100 (Brown, Boveri & Cie):
- Time-Division Scrambling mit 30ms Segmenten (16 Segmente = 480ms Frame)
- 6-stelliger Schlüssel (000000-999999)
- Zwei wählbare Synchronisationsverfahren

Sync "FSK 3.8/4.4 kHz":
    FSK mit 100 Baud oberhalb des Sprachbands. Pro Frame (48 Bit):
    SYNC (16) | ZÄHLER (16) | CRC-8 (8) | FÜLLBITS (8)
    Sehr präzise, braucht aber einen Kanal bis ~4.5 kHz (Soundkarte, Band).

Sync "Pilotton 1200/1800 Hz" (Variante C, Stil 70er/80er):
    Im Sprachband, daher auch über Funk (300-3000 Hz) und Telefon nutzbar.
    - 1200 Hz Dauerton als Takt: Der Empfänger regelt damit Bandgeschwindigkeit
      und Gleichlauf fortlaufend aus (Zeitbasiskorrektur wie beim Pilotton-Band).
    - 1800 Hz mit weich geformter Phasenumtastung, 1 Symbol pro Segment:
      je 2 Frames BARKER-13 | ZÄHLER (11) | CRC-8 (8).
    - Schmale Bandsperren (±55 Hz) halten Sprache und Pilot auseinander.
    1200 und 1800 Hz haben ganzzahlig viele Schwingungen pro Segment.

Sync "BBC 1830 Hz" (nach dem Original Vericrypt/Cryptophon 1100):
    Ein einzelner FSK-Pilotton 1830 ± 100 Hz (1730/1930 Hz) mitten im Sprachband,
    gleicher Rahmen wie "FSK 3.8/4.4 kHz". Eine breite Bandsperre räumt das
    Pilotband in der Sprache frei, der Empfänger entfernt den Pilot vor dem
    Entwürfeln. Klingt hohler als der Pilotton, hat keine Zeitbasiskorrektur.
"""

import hashlib
import math
import queue
import sys
import threading
from dataclasses import dataclass, replace
from enum import Enum
from typing import Optional, List, Tuple

import numpy as np
import sounddevice as sd
from PySide6.QtCore import Qt, QTimer, QRectF, QPointF
from PySide6.QtGui import QColor, QFont, QPainter, QPainterPath, QPen
from PySide6.QtWidgets import (QApplication, QButtonGroup, QCheckBox, QComboBox, QGridLayout,
                               QGroupBox, QHBoxLayout, QLabel, QLineEdit, QMainWindow,
                               QPushButton, QScrollArea, QVBoxLayout, QWidget)
from scipy.signal import butter, ellip, ellipord, sosfilt, sosfiltfilt


class CryptoMode(Enum):
    """Betriebsmodus."""
    OFF = "off"
    ENCRYPT = "encrypt"
    DECRYPT = "decrypt"


class SyncMode(Enum):
    """Synchronisationsverfahren (Sender und Empfänger müssen gleich eingestellt sein)."""
    FSK = "fsk"
    PILOT = "pilot"
    BBC = "bbc"


@dataclass
class AudioSettings:
    """Einstellungen."""
    inversion: bool = False
    bandpass: bool = False
    crypto_mode: CryptoMode = CryptoMode.OFF
    sync_mode: SyncMode = SyncMode.FSK
    crypto_key: int = 123456
    input_device: Optional[int] = None   # None = Default
    output_device: Optional[int] = None  # None = Default


class StreamingFilter:
    """SOS-Filter mit Zustand über Blockgrenzen hinweg (sonst Klicks alle ~23ms)."""

    def __init__(self, sos: np.ndarray):
        self.sos = sos
        self.zi = np.zeros((sos.shape[0], 2))

    def process(self, data: np.ndarray) -> np.ndarray:
        if len(data) == 0:
            return np.zeros(0, dtype=np.float32)
        out, self.zi = sosfilt(self.sos, data, zi=self.zi)
        return out.astype(np.float32)


def speech_lowpass(sample_rate: int) -> np.ndarray:
    """Steiler Tiefpass: hält das Sprachsignal vom FSK-Band fern."""
    return ellip(8, 0.5, 70, 3000, btype='low', fs=sample_rate, output='sos')


class FrequencyInverter:
    """
    Voice Inverter: spiegelt das Band 300-3000 Hz um die Mitte (f -> 3300 - f).
    Vor- und Nachfilter entfernen Aliasing und das obere Seitenband (3300 + f),
    das sonst hörbar bleibt. Zweimal angewendet ergibt wieder das Original.
    """

    CARRIER_FREQ = 3300

    def __init__(self, sample_rate: int):
        self.sample_rate = sample_rate
        self.pre = StreamingFilter(butter(4, [300, 3000], btype='band', fs=sample_rate, output='sos'))
        self.post = StreamingFilter(speech_lowpass(sample_rate))
        self.phase = 0.0

    def process(self, data: np.ndarray) -> np.ndarray:
        x = self.pre.process(data)
        phase_inc = 2 * np.pi * self.CARRIER_FREQ / self.sample_rate
        phases = self.phase + phase_inc * np.arange(len(x))
        self.phase = (self.phase + phase_inc * len(x)) % (2 * np.pi)
        # Faktor 2: Mischung halbiert die Amplitude des gewünschten Seitenbands
        return self.post.process(2.0 * x * np.cos(phases))


class RingBuffer:
    """Numpy-Ringbuffer für Audio-Samples."""

    def __init__(self, capacity: int):
        self.buf = np.zeros(capacity, dtype=np.float32)
        self.capacity = capacity
        self.write_pos = 0
        self.read_pos = 0
        self.count = 0

    def write(self, data: np.ndarray):
        """Schreibt Samples; bei Überlauf werden die ältesten verworfen."""
        n = len(data)
        if n >= self.capacity:
            data = data[-self.capacity:]
            n = self.capacity
        free = self.capacity - self.count
        if n > free:
            self.discard(n - free)

        end = self.write_pos + n
        if end <= self.capacity:
            self.buf[self.write_pos:end] = data
        else:
            first = self.capacity - self.write_pos
            self.buf[self.write_pos:] = data[:first]
            self.buf[:n - first] = data[first:]

        self.write_pos = end % self.capacity
        self.count += n

    def peek(self, n: int) -> Optional[np.ndarray]:
        """Liest n Samples ohne sie zu entfernen."""
        if n > self.count:
            return None
        end = self.read_pos + n
        if end <= self.capacity:
            return self.buf[self.read_pos:end].copy()
        first = self.capacity - self.read_pos
        return np.concatenate([self.buf[self.read_pos:], self.buf[:n - first]])

    def discard(self, n: int):
        n = min(n, self.count)
        self.read_pos = (self.read_pos + n) % self.capacity
        self.count -= n

    def read(self, n: int) -> Optional[np.ndarray]:
        """Liest n Samples. Gibt None zurück wenn nicht genug da."""
        result = self.peek(n)
        if result is not None:
            self.discard(n)
        return result

    def available(self) -> int:
        return self.count


def crc8(data: bytes) -> int:
    """CRC-8, Polynom 0x07."""
    crc = 0
    for byte in data:
        crc ^= byte
        for _ in range(8):
            crc = ((crc << 1) ^ 0x07) & 0xFF if crc & 0x80 else (crc << 1) & 0xFF
    return crc


def int_to_bits(value: int, width: int) -> List[int]:
    return [(value >> i) & 1 for i in range(width - 1, -1, -1)]


def bits_to_int(bits) -> int:
    value = 0
    for b in bits:
        value = (value << 1) | int(b)
    return value


class FSKModem:
    """
    Nicht-kohärentes FSK-Modem für die Frame-Synchronisation.
    Mark (1): 3800 Hz, Space (0): 4400 Hz, 100 Baud — oberhalb der Sprache.
    """

    MARK_FREQ = 3800
    SPACE_FREQ = 4400
    BAUD_RATE = 100
    AMPLITUDE = 0.05

    SYNC_WORD = 0x066B          # Autokorrelations-Nebenmaxima <= 2/16
    SYNC_BITS = 16
    FRAME_BITS = 48
    SYNC_THRESHOLD = 0.6        # mittlere Soft-Bit-Übereinstimmung
    MIN_AMPLITUDE = 0.002       # darunter gilt der Kanal als stumm

    sync_interval = 1           # Sync in jedem Frame
    counter_modulo = 1 << 16
    speech_path_delay = 0

    def __init__(self, sample_rate: int):
        if sample_rate % self.BAUD_RATE:
            raise ValueError("Samplerate muss ein Vielfaches von 100 sein")
        self.sample_rate = sample_rate
        self.samples_per_bit = sample_rate // self.BAUD_RATE
        self.search_tolerance = self.samples_per_bit * 4
        self.max_innovation = self.samples_per_bit
        self.lookahead = self.FRAME_BITS * self.samples_per_bit
        self.margin = 2 * self.samples_per_bit
        self.phase = 0.0
        self.sync_template = np.array(int_to_bits(self.SYNC_WORD, self.SYNC_BITS)) * 2.0 - 1.0
        self._band_filter = self.receive_filter(sample_rate)
        self._min_energy = (self.MIN_AMPLITUDE * self.samples_per_bit / 2) ** 2

    def build_frame_bits(self, counter: int) -> List[int]:
        counter_bytes = counter.to_bytes(2, 'big')
        return (int_to_bits(self.SYNC_WORD, 16) + int_to_bits(counter, 16)
                + int_to_bits(crc8(counter_bytes), 8) + [1, 0] * 4)

    def receive_filter(self, sample_rate: int) -> np.ndarray:
        """Trennt im Empfänger das FSK-Band von der Sprache."""
        return ellip(8, 0.5, 70, 3500, btype='high', fs=sample_rate, output='sos')

    def speech_notches(self) -> List[np.ndarray]:
        return []   # FSK liegt über der Sprache, der Tiefpass reicht

    def frame_signal(self, counter: int) -> np.ndarray:
        return self.modulate(self.build_frame_bits(counter))

    def modulate(self, bits: List[int]) -> np.ndarray:
        """Kodiert Bits als FSK-Signal mit durchgehender Phase."""
        freqs = np.repeat(np.where(np.array(bits) == 1, self.MARK_FREQ, self.SPACE_FREQ),
                          self.samples_per_bit)
        phases = self.phase + np.cumsum(2 * np.pi * freqs / self.sample_rate)
        self.phase = phases[-1] % (2 * np.pi)
        return (self.AMPLITUDE * np.sin(phases)).astype(np.float32)

    def _discriminator(self, x: np.ndarray) -> Tuple[np.ndarray, np.ndarray]:
        """
        Liefert für jede Startposition a die über ein Bit integrierte Ton-Energie.
        I/Q-Mischung macht das Ergebnis unabhängig von der Trägerphase.
        d[a] in [-1, 1]: +1 = Mark, -1 = Space.
        """
        spb = self.samples_per_bit
        x = sosfiltfilt(self._band_filter, x.astype(np.float64))  # nullphasig: kein Zeitversatz
        n = np.arange(len(x))

        def energy(freq):
            mixed = x * np.exp(-2j * np.pi * freq * n / self.sample_rate)
            c = np.concatenate([[0], np.cumsum(mixed)])
            return np.abs(c[spb:] - c[:-spb]) ** 2

        e_mark = energy(self.MARK_FREQ)
        e_space = energy(self.SPACE_FREQ)
        total = e_mark + e_space
        return (e_mark - e_space) / (total + 1e-12), total

    def find_frame(self, x: np.ndarray, lo: int, hi: int):
        """
        Sucht den Frame-Anfang p im Bereich [lo, hi].
        Rückgabe: (p, zähler oder None bei CRC-Fehler, score) oder None.
        """
        spb = self.samples_per_bit
        d, total = self._discriminator(x)
        hi = min(hi, len(d) - self.FRAME_BITS * spb)
        if hi < lo:
            return None

        positions = np.arange(lo, hi + 1)
        score = np.zeros(len(positions))
        for k, t in enumerate(self.sync_template):
            score += t * d[positions + k * spb]
        score /= self.SYNC_BITS

        best = int(np.argmax(score))
        if score[best] < self.SYNC_THRESHOLD:
            return None
        p = int(positions[best])

        bit_starts = p + np.arange(self.FRAME_BITS) * spb
        if np.mean(total[bit_starts[:self.SYNC_BITS]]) < self._min_energy:
            return None

        bits = (d[bit_starts] > 0).astype(int)
        counter = bits_to_int(bits[16:32])
        if bits_to_int(bits[32:40]) != crc8(counter.to_bytes(2, 'big')):
            return p, None, float(score[best])
        return p, counter, float(score[best])


class BBCPilotModem(FSKModem):
    """
    FSK-Pilotton nach dem Original (Vericrypt/Cryptophon 1100): 1830 Hz ± 100 Hz.
    Ein einzelner Ton, der je Bit zwischen 1730 und 1930 Hz umspringt, mitten im
    Sprachband. Rahmen und Empfänger wie FSKModem, nur mit Bandpass statt Hochpass.
    """

    MARK_FREQ = 1730
    SPACE_FREQ = 1930
    BAND = (1580, 2080)         # Pilotband, 100 Baud belegen etwa ±200 Hz um 1830 Hz
    PASS_MARGIN = 80            # Übergang bis zur ungedämpften Sprache

    def receive_filter(self, sample_rate: int) -> np.ndarray:
        return ellip(6, 0.5, 60, self.BAND, btype='bandpass', fs=sample_rate, output='sos')

    def speech_notches(self) -> List[np.ndarray]:
        lo, hi = self.BAND
        order, wn = ellipord([lo - self.PASS_MARGIN, hi + self.PASS_MARGIN], [lo, hi],
                             0.5, 50, fs=self.sample_rate)
        return [ellip(order, 0.5, 50, wn, btype='bandstop', fs=self.sample_rate, output='sos')]


def raised_cosine(t: np.ndarray, rolloff: float) -> np.ndarray:
    """Raised-Cosine-Puls (t in Symboldauern): bandbegrenzt, ohne Intersymbol-Störung."""
    singular = np.isclose(np.abs(2 * rolloff * t), 1.0)
    denom = np.where(singular, 1.0, 1.0 - (2 * rolloff * t) ** 2)
    pulse = np.sinc(t) * np.cos(np.pi * rolloff * t) / denom
    return np.where(singular, np.pi / 4 * np.sinc(1 / (2 * rolloff)), pulse)


def narrow_bandstop(sample_rate: int, freq: float) -> np.ndarray:
    """Bandsperre: >= 50 dB innerhalb ±30 Hz, Durchlass ab ±55 Hz."""
    order, wn = ellipord([freq - 55, freq + 55], [freq - 30, freq + 30], 0.5, 50, fs=sample_rate)
    return ellip(order, 0.5, 50, wn, btype='bandstop', fs=sample_rate, output='sos')


class PilotToneModem:
    """
    Variante C: Pilottöne im Sprachband.

    1200 Hz: unmodulierter Taktton (Auswertung in PilotClockRecovery).
    1800 Hz: BPSK mit Raised-Cosine-Pulsen, 1 Symbol pro 30ms-Segment (33 Baud,
    belegt nur ±25 Hz). Ein Superframe aus 2 Frames trägt 32 Symbole:
    BARKER-13 | ZÄHLER/2 (11 Bit) | CRC-8.
    """

    CLOCK_FREQ = 1200
    DATA_FREQ = 1800
    CLOCK_AMPLITUDE = 0.03
    DATA_AMPLITUDE = 0.03
    ROLLOFF = 0.5
    PULSE_SPAN = 4              # Pulslänge je Seite in Symbolen
    BARKER13 = np.array([1, 1, 1, 1, 1, -1, -1, 1, 1, -1, 1, -1, 1], dtype=float)
    COUNTER_BITS = 11
    SUPERFRAME_SYMBOLS = 32
    SYNC_THRESHOLD = 0.8
    MIN_AMPLITUDE = 0.003
    DECIMATION = 21             # 1323 Samples/Symbol = 63 * 21

    sync_interval = 2
    counter_modulo = 1 << (COUNTER_BITS + 1)

    def __init__(self, sample_rate: int, frame_samples: int, segment_samples: int):
        self.sample_rate = sample_rate
        self.frame_samples = frame_samples
        self.symbol_samples = segment_samples
        self.search_tolerance = sample_rate // 100 * 4
        self.max_innovation = sample_rate // 100
        self.lookahead = self.SUPERFRAME_SYMBOLS * segment_samples + segment_samples
        self.margin = segment_samples

        half = self.PULSE_SPAN * segment_samples
        self.pulse = raised_cosine(np.arange(-half, half + 1) / segment_samples, self.ROLLOFF)
        # Pulsmitte liegt `half` Samples hinter dem Symbolzeitpunkt -> Sprache gleich verzögern
        self.speech_path_delay = half
        self._accumulator = np.zeros(frame_samples + len(self.pulse))
        self._sample_index = 0

        self._notch_sos = [narrow_bandstop(sample_rate, f) for f in (self.CLOCK_FREQ, self.DATA_FREQ)]
        if segment_samples % self.DECIMATION or len(self.pulse) // 2 % self.DECIMATION:
            raise ValueError("Segmentlänge passt nicht zur Dezimierung")
        self._data_lowpass = butter(4, 30, fs=sample_rate / self.DECIMATION, output='sos')
        # Antwort eines Symbols nach Integration über ein Symbol, im dezimierten Raster
        integrated = np.convolve(self.pulse, np.ones(segment_samples) / segment_samples, mode='same')
        self._symbol_response = integrated[::self.DECIMATION]

    def speech_notches(self) -> List[np.ndarray]:
        return self._notch_sos

    def superframe_symbols(self, superframe: int) -> np.ndarray:
        counter_bits = int_to_bits(superframe, self.COUNTER_BITS)
        crc_bits = int_to_bits(crc8(superframe.to_bytes(2, 'big')), 8)
        data = np.array(counter_bits + crc_bits, dtype=float) * 2 - 1
        return np.concatenate([self.BARKER13, data])

    def frame_signal(self, counter: int) -> np.ndarray:
        symbols = self.superframe_symbols(counter // 2)
        half = symbols[:16] if counter % 2 == 0 else symbols[16:]
        S, F, L = self.symbol_samples, self.frame_samples, len(self.pulse)

        acc = self._accumulator
        for k, a in enumerate(half):
            pos = k * S + S // 2
            acc[pos:pos + L] += a * self.pulse
        baseband = acc[:F].copy()
        acc[:-F] = acc[F:]
        acc[-F:] = 0.0

        # Beide Töne haben ganzzahlig viele Schwingungen pro Sekunde -> Index modulo Samplerate
        n = (self._sample_index + np.arange(F)) % self.sample_rate
        self._sample_index = (self._sample_index + F) % self.sample_rate
        clock = self.CLOCK_AMPLITUDE * np.sin(2 * np.pi * self.CLOCK_FREQ * n / self.sample_rate)
        data = self.DATA_AMPLITUDE * baseband * np.sin(2 * np.pi * self.DATA_FREQ * n / self.sample_rate)
        return (clock + data).astype(np.float32)

    def find_frame(self, x: np.ndarray, lo: int, hi: int):
        """
        Sucht einen Superframe-Anfang in [lo, hi]. Rückgabe wie FSKModem.find_frame.

        Das 1800-Hz-Signal wird ins Basisband gemischt, über ein Symbol integriert
        (löscht den 1200-Hz-Takt exakt aus) und 21-fach reduziert ausgewertet.
        """
        S, D = self.symbol_samples, self.DECIMATION
        per_symbol = S // D
        lo = -(-lo // D) * D
        hi = min(hi, len(x) - (self.SUPERFRAME_SYMBOLS + 1) * S)
        if hi < lo:
            return None

        n = np.arange(len(x))
        z = x.astype(np.float64) * np.exp(-2j * np.pi * self.DATA_FREQ * (n % self.sample_rate) / self.sample_rate)
        c = np.concatenate([[0], np.cumsum(z)])
        zd = (c[S::D] - c[:-S:D])[:(len(x) - S) // D + 1] / S   # zd[j]: Symbol mit Beginn bei j*D
        zd = sosfiltfilt(self._data_lowpass, zd)

        j_lo, j_hi = lo // D, hi // D
        last = (self.SUPERFRAME_SYMBOLS - 1) * per_symbol
        # BPSK: z² hat die doppelte Trägerphase; ±180° klärt das Barker-Vorzeichen
        region = zd[j_lo:j_hi + last + 1]
        theta = 0.5 * np.angle(np.sum(region ** 2))
        soft = np.real(zd * np.exp(-1j * theta))

        js = np.arange(j_lo, j_hi + 1)
        corr = np.zeros(len(js))
        for k, b in enumerate(self.BARKER13):
            corr += b * soft[js + k * per_symbol]

        best = int(np.argmax(np.abs(corr)))
        j0 = int(js[best])
        polarity = np.sign(corr[best])
        values = soft[j0 + np.arange(self.SUPERFRAME_SYMBOLS) * per_symbol] * polarity
        sync_values = values[:len(self.BARKER13)]
        score = float(np.dot(self.BARKER13, sync_values) / (np.sum(np.abs(sync_values)) + 1e-12))
        if score < self.SYNC_THRESHOLD or np.mean(np.abs(sync_values)) < self.MIN_AMPLITUDE / 3:
            return None

        bits = (values > 0).astype(int)
        superframe = bits_to_int(bits[13:24])
        if bits_to_int(bits[24:32]) != crc8(superframe.to_bytes(2, 'big')):
            return j0 * D, None, score
        return self._refine_position(soft * polarity, j0, superframe), 2 * superframe, score

    def _refine_position(self, soft: np.ndarray, j0: int, superframe: int) -> float:
        """
        Feinsynchronisation: Korrelation mit dem vollständig bekannten Symbolzug,
        danach Parabel-Interpolation. Die Barker-Suche allein wird durch
        Nachbarsymbole verschoben.
        """
        D = self.DECIMATION
        per_symbol = self.symbol_samples // D
        response = self._symbol_response
        center = len(response) // 2
        inner = np.arange(self.PULSE_SPAN * per_symbol, (self.SUPERFRAME_SYMBOLS - self.PULSE_SPAN) * per_symbol)
        template = np.zeros(len(inner))
        for k, a in enumerate(self.superframe_symbols(superframe)):
            idx = inner - k * per_symbol + center
            ok = (idx >= 0) & (idx < len(response))
            template[ok] += a * response[idx[ok]]

        search = per_symbol // 2
        lags = np.arange(-search, search + 1)
        valid = (j0 + lags + inner[0] >= 0) & (j0 + lags + inner[-1] < len(soft))
        lags = lags[valid]
        if len(lags) < 3:
            return float(j0 * D)
        scores = np.array([np.dot(template, soft[j0 + lag + inner]) for lag in lags])
        i = int(np.argmax(scores))
        offset = float(lags[i])
        if 0 < i < len(lags) - 1:
            y0, y1, y2 = scores[i - 1], scores[i], scores[i + 1]
            denom = y0 - 2 * y1 + y2
            if denom < 0:
                offset += 0.5 * (y0 - y2) / denom
        return (j0 + offset) * D


class PilotClockRecovery:
    """
    Zeitbasiskorrektur über den 1200-Hz-Taktton (Empfänger, Variante C).

    Die Phase des Takttons verrät, wie weit die Sender-Zeit an jeder Stelle des
    Kanals fortgeschritten ist. Der Eingang wird darauf umgerechnet, sodass danach
    Frames exakt ihre Sollänge haben — Bandgeschwindigkeit und Gleichlauf sind raus.
    Ohne Taktton wird das Signal unverändert durchgereicht.
    """

    LOWPASS_HZ = 20
    MIN_AMPLITUDE = 0.003

    def __init__(self, sample_rate: int, freq: float = PilotToneModem.CLOCK_FREQ):
        self.sample_rate = sample_rate
        self.omega = 2 * np.pi * freq / sample_rate
        self._sos = butter(6, self.LOWPASS_HZ, fs=sample_rate, output='sos')
        self._zi = np.zeros((self._sos.shape[0], 2), dtype=complex)
        impulse = np.zeros(sample_rate)
        impulse[0] = 1.0
        h = sosfilt(self._sos, impulse)
        self._delay = int(round(np.dot(np.arange(len(h)), h) / np.sum(h)))

        self._n = 0                      # verarbeitete Kanal-Samples
        self._x = np.zeros(0)            # Kanal-Samples ab Index _start
        self._u = np.zeros(0)            # Sender-Zeit dieser Samples
        self._start = 0
        self._prev_raw = 0.0
        self._phase = 0.0
        self._next_t = None
        self.locked = False

    def process(self, x: np.ndarray) -> np.ndarray:
        idx = self._n + np.arange(len(x))
        baseband = x * np.exp(-1j * self.omega * (idx % self.sample_rate))
        lp, self._zi = sosfilt(self._sos, baseband, zi=self._zi)

        amplitude = np.abs(lp)
        raw = np.angle(lp)
        steps = np.diff(np.concatenate([[self._prev_raw], raw]))
        steps = (steps + np.pi) % (2 * np.pi) - np.pi
        steps[amplitude < self.MIN_AMPLITUDE / 2] = 0.0   # ohne Taktton Phase halten
        phase = self._phase + np.cumsum(steps)
        self._prev_raw, self._phase = raw[-1], phase[-1]
        self.locked = bool(np.mean(amplitude) >= self.MIN_AMPLITUDE / 2)

        # lp[i] beschreibt Kanal-Sample idx[i] - delay
        m = idx - self._delay
        valid = m >= 0
        u = m[valid] + phase[valid] / self.omega
        if len(self._u):
            u = np.maximum.accumulate(np.concatenate([[self._u[-1]], u]))[1:]
        self._x = np.concatenate([self._x, x.astype(np.float64)])
        self._u = np.concatenate([self._u, u])
        self._n += len(x)

        if len(self._u) < 2:
            return np.zeros(0, dtype=np.float32)
        if self._next_t is None:
            self._next_t = math.ceil(self._u[0])

        count = int(math.floor(self._u[-1] - self._next_t)) + 1
        if count <= 0:
            return np.zeros(0, dtype=np.float32)
        t = self._next_t + np.arange(count)
        self._next_t += count

        abs_index = self._start + np.arange(len(self._u))
        m_out = np.interp(t, self._u, abs_index)
        y = np.interp(m_out, self._start + np.arange(len(self._x)), self._x)

        keep_from = min(max(int(m_out[-1]) - 2 - self._start, 0), len(self._u) - 2)
        self._x = self._x[keep_from:]
        self._u = self._u[keep_from:]
        self._start += keep_from
        return y.astype(np.float32)



class VericryptScrambler:
    """
    Time-Division Scrambler im Vericrypt-Stil.
    16 Segmente à 30ms (= 3 FSK-Bits) pro Frame = 480ms.
    """

    SEGMENT_MS = 30
    FRAME_SEGMENTS = 16

    def __init__(self, sample_rate: int):
        self.sample_rate = sample_rate
        self.segment_samples = sample_rate * self.SEGMENT_MS // 1000
        self.frame_samples = self.segment_samples * self.FRAME_SEGMENTS

    def permutation(self, key: int, counter: int) -> np.ndarray:
        """perm[k] = Quellsegment, das an Position k gesendet wird."""
        digest = hashlib.sha256(f"vericrypt:{key:06d}:{counter}".encode()).digest()
        # RandomState ist über numpy-Versionen hinweg reproduzierbar (Generator nicht)
        rng = np.random.RandomState(int.from_bytes(digest[:4], 'big'))
        return rng.permutation(self.FRAME_SEGMENTS)

    def scramble(self, frame: np.ndarray, perm: np.ndarray) -> np.ndarray:
        segments = frame.reshape(self.FRAME_SEGMENTS, self.segment_samples)
        return segments[perm].reshape(-1)

    def unscramble(self, frame: np.ndarray, perm: np.ndarray) -> np.ndarray:
        segments = frame.reshape(self.FRAME_SEGMENTS, self.segment_samples)
        out = np.empty_like(segments)
        out[perm] = segments
        return out.reshape(-1)


class FrameEncoder:
    """ENCRYPT-Pfad: Bandbegrenzung -> (Inversion) -> Permutation -> Tiefpass/Kerben -> + Sync-Signal."""

    def __init__(self, sample_rate: int, scrambler: VericryptScrambler, modem):
        self.scrambler = scrambler
        self.modem = modem
        self.speech_filter = StreamingFilter(
            butter(4, [300, 3000], btype='band', fs=sample_rate, output='sos'))
        self.inverter = FrequencyInverter(sample_rate)
        # Segmentsprünge erzeugen breitbandige Klicks, die sonst ins FSK-Band streuen
        self.post_filter = StreamingFilter(speech_lowpass(sample_rate))
        # Pilotbänder freiräumen: sonst überdeckt Sprache den Pilot
        self.notches = [StreamingFilter(sos) for sos in modem.speech_notches()]
        self.delay_line = np.zeros(modem.speech_path_delay, dtype=np.float32)
        self.counter = 0
        self.last_permutation = np.arange(scrambler.FRAME_SEGMENTS)

    def encode(self, frame: np.ndarray, settings: AudioSettings) -> np.ndarray:
        speech = self.speech_filter.process(frame)
        if settings.inversion:
            speech = self.inverter.process(speech)

        perm = self.scrambler.permutation(settings.crypto_key, self.counter)
        self.last_permutation = perm
        scrambled = self.post_filter.process(self.scrambler.scramble(speech, perm))
        for notch in self.notches:
            scrambled = notch.process(scrambled)
        if len(self.delay_line):
            delayed = np.concatenate([self.delay_line, scrambled])
            scrambled, self.delay_line = delayed[:len(scrambled)], delayed[len(scrambled):]

        sync = self.modem.frame_signal(self.counter)
        self.counter = (self.counter + 1) % self.modem.counter_modulo
        return np.clip(scrambled + sync, -1.0, 1.0)


class FrameDecoder:
    """
    DECRYPT-Pfad mit samplegenauer Synchronisation.

    Im gelockten Zustand wird der nächste Frame-Anfang um Position `tol` im
    Eingangspuffer erwartet und nur im Fenster [0, 2*tol] gesucht. Ein
    Alpha-Beta-Filter glättet die gemessene Position (Rauschen) und schätzt die
    Abweichung der Frame-Länge (Gleichlauf von Band/Soundkarte). Jeder Frame
    wird auf die Sollänge umgerechnet, damit alle 16 Segmentgrenzen stimmen.
    Fehlt der Sync kurz, läuft der Decoder einige Frames im Schwungrad-Modus.
    """

    FLYWHEEL_FRAMES = 4
    ALPHA = 0.3            # Gewicht der Positionsmessung
    BETA_MIN = 0.05        # minimale Nachführung der Längenabweichung

    def __init__(self, sample_rate: int, scrambler: VericryptScrambler, modem,
                 speech_delay: int):
        self.scrambler = scrambler
        self.modem = modem
        self.speech_delay = speech_delay
        self.tol = modem.search_tolerance
        self.max_drift = self.tol / 2          # ~4% Geschwindigkeitsabweichung
        self.lowpass = StreamingFilter(speech_lowpass(sample_rate))
        self.inverter = FrequencyInverter(sample_rate)
        self.bandpass = StreamingFilter(butter(4, [300, 3400], btype='band', fs=sample_rate, output='sos'))
        self.locked = False
        self.missed = 0
        self.locked_frames = 0
        self.expected = float(self.tol)        # erwartete Frame-Position im Puffer
        self.drift = 0.0                       # Samples, die ein Frame länger ist als nominal
        self.counter = 0
        self.last_permutation = np.arange(scrambler.FRAME_SEGMENTS)

    def _sync_due(self) -> bool:
        return not self.locked or self.counter % self.modem.sync_interval == 0

    def _track(self, found) -> Optional[Tuple[float, int]]:
        """Entscheidet über Frame-Position und Zähler. None = kein Sync."""
        measured, counter, _ = found if found is not None else (None, None, 0.0)

        if not self.locked:
            if counter is None:
                return None
            self.locked, self.missed, self.locked_frames, self.drift = True, 0, 0, 0.0
            return float(measured), counter

        if not self._sync_due():
            # Frame ohne eigenen Sync (Pilotton-Superframe): Zeitlage fortschreiben
            return self.expected, self.counter

        innovation = None if measured is None else measured - self.expected
        if innovation is not None and abs(innovation) > self.modem.max_innovation:
            # Weit daneben: nur mit gültiger CRC als echter Neu-Sync akzeptieren
            if counter is not None:
                self.missed, self.locked_frames, self.drift = 0, 0, 0.0
                return float(measured), counter
            innovation = None

        if innovation is None:
            self.missed += 1
            if self.missed > self.FLYWHEEL_FRAMES:
                self.locked = False
                return None
            return self.expected, self.counter

        self.missed = 0
        self.locked_frames += 1
        beta = max(self.BETA_MIN, 1.0 / (self.locked_frames + 1))
        self.drift = float(np.clip(self.drift + beta * innovation, -self.max_drift, self.max_drift))
        position = self.expected + self.ALPHA * innovation
        return position, counter if counter is not None else self.counter

    def pump(self, in_buf: RingBuffer, out_buf: RingBuffer, settings: AudioSettings,
             speech_buf: Optional[RingBuffer] = None):
        """
        in_buf: Kanal mit Sync-Signal. speech_buf: derselbe Kanal mit entfernten
        Pilottönen (gleich lang, gleich ausgerichtet); None = in_buf verwenden.
        """
        frame_len = self.scrambler.frame_samples
        modem = self.modem

        while True:
            if self.locked:
                lo, hi = 0, 2 * self.tol
            else:
                lo, hi = self.tol, self.tol + frame_len
            need = hi + max(frame_len + self.speech_delay + int(self.max_drift), modem.lookahead) + modem.margin
            if in_buf.available() < need:
                return

            window = in_buf.peek(need)
            found = modem.find_frame(window, lo, hi) if self._sync_due() else None
            decision = self._track(found)

            if decision is None:
                # Kein Sync: Stille ausgeben statt Rauschen, weiter suchen
                in_buf.discard(frame_len)
                if speech_buf is not None:
                    speech_buf.discard(frame_len)
                out_buf.write(np.zeros(frame_len, dtype=np.float32))
                continue

            position, counter = decision
            actual_len = frame_len + self.drift
            sample_pos = position + self.speech_delay + np.arange(frame_len) * (actual_len / frame_len)
            speech_window = window if speech_buf is None else speech_buf.peek(need)
            frame = np.interp(sample_pos, np.arange(len(window)), speech_window).astype(np.float32)

            perm = self.scrambler.permutation(settings.crypto_key, counter)
            self.last_permutation = perm

            speech = self.lowpass.process(self.scrambler.unscramble(frame, perm))
            if settings.inversion:
                speech = self.inverter.process(speech)
            if settings.bandpass:
                speech = self.bandpass.process(speech)
            out_buf.write(speech)

            next_start = position + actual_len
            consumed = int(next_start - self.tol)
            in_buf.discard(consumed)
            if speech_buf is not None:
                speech_buf.discard(consumed)
            self.expected = next_start - consumed
            self.counter = (counter + 1) % modem.counter_modulo


class AudioProcessor:
    """
    Haupt-Audio-Prozessor.
    Sämtlicher DSP-Zustand wird nur im Audio-Thread verändert; die GUI setzt
    lediglich Einstellungen (unter Lock) und fordert Resets per Flag an.
    """

    def __init__(self, sample_rate: int = 44100, chunk_size: int = 1024):
        self.sample_rate = sample_rate
        self.chunk_size = chunk_size
        self.settings = AudioSettings()
        self._lock = threading.Lock()
        self._reset_requested = threading.Event()

        self.scrambler = VericryptScrambler(sample_rate)
        self._init_state(CryptoMode.OFF, SyncMode.FSK)

    def _make_modem(self, sync_mode: SyncMode):
        if sync_mode == SyncMode.PILOT:
            return PilotToneModem(self.sample_rate, self.scrambler.frame_samples,
                                  self.scrambler.segment_samples)
        modem = BBCPilotModem(self.sample_rate) if sync_mode == SyncMode.BBC else FSKModem(self.sample_rate)
        assert self.scrambler.frame_samples == modem.FRAME_BITS * modem.samples_per_bit
        return modem

    def _speech_delay(self, modem) -> int:
        """
        Laufzeit der Filter nach dem Scrambling: um so viel liegt die Sprache
        hinter dem Frame-Anfang, den das Sync-Signal markiert.
        """
        impulse = np.zeros(8192)
        impulse[0] = 1.0
        h = sosfilt(speech_lowpass(self.sample_rate), impulse)
        for sos in modem.speech_notches():
            h = sosfilt(sos, h)
        return int(np.argmax(np.abs(h)))

    def _init_state(self, mode: CryptoMode, sync_mode: SyncMode):
        capacity = self.scrambler.frame_samples * 4 + self.chunk_size
        self.in_buffer = RingBuffer(capacity)
        self.out_buffer = RingBuffer(capacity)
        self.modem = self._make_modem(sync_mode)
        self.encoder = FrameEncoder(self.sample_rate, self.scrambler, self.modem)
        self.decoder = FrameDecoder(self.sample_rate, self.scrambler, self.modem,
                                    self._speech_delay(self.modem))
        self.clock_recovery = PilotClockRecovery(self.sample_rate) if sync_mode == SyncMode.PILOT else None
        # Pilottöne im Sprachband fortlaufend entfernen, solange sie noch lückenlos sind (vor dem Entwürfeln)
        self.speech_buffer = RingBuffer(capacity) if self.modem.speech_notches() else None
        self.pilot_removers = [StreamingFilter(sos) for sos in self.modem.speech_notches()]
        self.clear_inverter = FrequencyInverter(self.sample_rate)
        self.clear_bandpass = StreamingFilter(
            butter(4, [300, 3400], btype='band', fs=self.sample_rate, output='sos'))
        self._active_mode = mode
        self._active_sync = sync_mode
        self._out_position = 0
        self._first_frame_ready = False
        self._primed = False

    # --- Schnittstelle für die GUI (thread-sicher) ---

    def update_settings(self, **kwargs):
        with self._lock:
            for key, value in kwargs.items():
                if hasattr(self.settings, key):
                    setattr(self.settings, key, value)

    def get_settings(self) -> AudioSettings:
        with self._lock:
            return replace(self.settings)

    def request_reset(self):
        self._reset_requested.set()

    @property
    def current_segment(self) -> int:
        return (self._out_position % self.scrambler.frame_samples) // self.scrambler.segment_samples

    @property
    def current_permutation(self) -> np.ndarray:
        if self._active_mode == CryptoMode.DECRYPT:
            return self.decoder.last_permutation
        return self.encoder.last_permutation

    @property
    def frame_counter(self) -> int:
        if self._active_mode == CryptoMode.DECRYPT:
            return self.decoder.counter
        return self.encoder.counter

    @property
    def sync_locked(self) -> bool:
        return self.decoder.locked

    # --- Audio-Thread ---

    def process(self, data: np.ndarray) -> np.ndarray:
        settings = self.get_settings()
        if (self._reset_requested.is_set() or settings.crypto_mode != self._active_mode
                or settings.sync_mode != self._active_sync):
            self._reset_requested.clear()
            self._init_state(settings.crypto_mode, settings.sync_mode)

        if settings.crypto_mode == CryptoMode.OFF:
            if settings.inversion:
                data = self.clear_inverter.process(data)
            if settings.bandpass:
                data = self.clear_bandpass.process(data)
            return data.astype(np.float32)

        if settings.crypto_mode == CryptoMode.DECRYPT and self.speech_buffer is not None:
            nominal = self.clock_recovery.process(data) if self.clock_recovery is not None else data
            self.in_buffer.write(nominal)
            for remover in self.pilot_removers:
                nominal = remover.process(nominal)
            self.speech_buffer.write(nominal)
        else:
            self.in_buffer.write(data)
        if settings.crypto_mode == CryptoMode.ENCRYPT:
            frame_len = self.scrambler.frame_samples
            while self.in_buffer.available() >= frame_len:
                frame = self.in_buffer.read(frame_len)
                self.out_buffer.write(self.encoder.encode(frame, settings))
        else:
            self.decoder.pump(self.in_buffer, self.out_buffer, settings, self.speech_buffer)

        if not self._primed:
            # Nie Klartext durchreichen: bis der erste Frame fertig ist -> Stille.
            # Ausgabe erst einen Block nach dem ersten Frame starten: dann läuft der
            # Puffer nie leer und der Frame-Takt bekommt keine Lücke zwischen Frame 0 und 1.
            if not self._first_frame_ready:
                self._first_frame_ready = self.out_buffer.available() > 0
                return np.zeros(len(data), dtype=np.float32)
            self._primed = True

        out = self.out_buffer.read(len(data))
        if out is None:
            out = np.zeros(len(data), dtype=np.float32)
            rest = self.out_buffer.read(self.out_buffer.available())
            out[:len(rest)] = rest
        self._out_position += len(data)
        return out


class LevelMeter:
    """Audio-Pegel Visualisierung."""

    def __init__(self):
        self.input_db = -60.0
        self.output_db = -60.0

    @staticmethod
    def to_db(data: np.ndarray) -> float:
        rms = float(np.sqrt(np.mean(np.square(data)))) if len(data) else 0.0
        return 20 * math.log10(max(rms, 1e-10))

    def update(self, input_db: float, output_db: float):
        self.input_db = input_db
        self.output_db = output_db

    def get_current(self) -> tuple:
        return self.input_db, self.output_db


BG = '#1a1a2e'
PANEL = '#0a0a1a'
ACCENT = '#0f0'

STYLESHEET = f"""
QWidget {{ background: {BG}; color: #eee; font-family: Menlo, Monaco, 'Courier New', monospace; font-size: 12px; }}
QGroupBox {{ border: 1px solid #333; border-radius: 4px; margin-top: 14px; padding: 10px 8px 8px 8px; }}
QGroupBox::title {{ subcontrol-origin: margin; left: 10px; padding: 0 4px; color: {ACCENT}; }}
QComboBox, QLineEdit {{ background: #2a2a4e; border: 1px solid #444; border-radius: 3px; padding: 4px 6px; }}
QComboBox QAbstractItemView {{ background: #2a2a4e; selection-background-color: #0a4a0a; }}
QPushButton {{ background: #2a2a4e; border: 1px solid #444; border-radius: 3px; padding: 6px 12px; }}
QPushButton:hover {{ border-color: #888; }}
QPushButton:checked {{ background: #0a3a0a; border-color: {ACCENT}; color: {ACCENT}; }}
QPushButton#start {{ font-size: 14px; font-weight: bold; padding: 10px 30px; }}
QCheckBox {{ padding: 5px 0; spacing: 8px; }}
QScrollArea {{ border: none; }}
"""


class NoWheelComboBox(QComboBox):
    """Mausrad scrollt die Seite statt das Audiogerät (und damit den Stream) zu wechseln."""

    def wheelEvent(self, event):
        event.ignore()


class WideCheckBox(QCheckBox):
    """Die ganze Zeile ist anklickbar, nicht nur Kästchen und Text."""

    def hitButton(self, pos) -> bool:
        return self.rect().contains(pos)


class SegmentRing(QWidget):
    """Zeigt die aktuelle Permutation der 16 Segmente, Frame-Zähler und Sync-Status."""

    COLORS = ['#2a1a1a', '#1a2a1a', '#1a1a2a', '#2a2a1a',
              '#2a1a2a', '#1a2a2a', '#2a2a2a', '#3a1a1a',
              '#1a3a1a', '#1a1a3a', '#3a3a1a', '#3a1a3a',
              '#1a3a3a', '#3a3a3a', '#4a1a1a', '#1a4a1a']

    def __init__(self, processor: AudioProcessor):
        super().__init__()
        self.processor = processor
        self.setMinimumHeight(170)

    def paintEvent(self, event):
        p = QPainter(self)
        p.setRenderHint(QPainter.Antialiasing)
        p.fillRect(self.rect(), QColor(PANEL))

        cx, cy = self.width() / 2, self.height() / 2
        r_outer = min(cx, cy) - 10
        r_inner = r_outer - 28
        outer = QRectF(cx - r_outer, cy - r_outer, 2 * r_outer, 2 * r_outer)
        inner = QRectF(cx - r_inner, cy - r_inner, 2 * r_inner, 2 * r_inner)

        processor = self.processor
        crypto_mode = processor.get_settings().crypto_mode
        active = crypto_mode != CryptoMode.OFF
        current_seg = processor.current_segment
        perm = processor.current_permutation
        n = VericryptScrambler.FRAME_SEGMENTS
        span = 360 / n
        p.setFont(QFont('Menlo', 9))

        for i in range(n):
            start = 90 - i * span     # Qt: Grad gegen den Uhrzeigersinn ab 3 Uhr; Segment 0 oben
            path = QPainterPath()
            path.arcMoveTo(outer, start)
            path.arcTo(outer, start, -span)
            path.arcTo(inner, start - span, span)
            path.closeSubpath()

            if not active:
                fill, outline = '#1a1a2a', '#333'
            elif i == current_seg:
                fill, outline = ACCENT, ACCENT
            else:
                fill, outline = self.COLORS[int(perm[i]) % len(self.COLORS)], '#444'
            p.setPen(QPen(QColor(outline), 1))
            p.setBrush(QColor(fill))
            p.drawPath(path)

            mid = math.radians(start - span / 2)
            r_text = (r_outer + r_inner) / 2
            tx, ty = cx + r_text * math.cos(mid), cy - r_text * math.sin(mid)
            p.setPen(QColor('#000' if active and i == current_seg else '#777'))
            p.drawText(QRectF(tx - 12, ty - 8, 24, 16), Qt.AlignCenter, str(int(perm[i]) if active else i))

        if crypto_mode == CryptoMode.DECRYPT:
            caption, color = ("SYNC", ACCENT) if processor.sync_locked else ("NO SYNC", '#f44')
        else:
            caption, color = "FRAME", '#666'
        p.setPen(QColor(color))
        p.drawText(QRectF(cx - 50, cy - 22, 100, 16), Qt.AlignCenter, caption)
        p.setPen(QColor(ACCENT))
        p.setFont(QFont('Menlo', 15, QFont.Bold))
        p.drawText(QRectF(cx - 60, cy - 4, 120, 24), Qt.AlignCenter, f"{processor.frame_counter:05d}")
        p.end()


class LevelBars(QWidget):
    """Ein- und Ausgangspegel in dB (−60 … 0)."""

    def __init__(self, level_meter: LevelMeter):
        super().__init__()
        self.level_meter = level_meter
        self.setMinimumHeight(84)

    def paintEvent(self, event):
        p = QPainter(self)
        p.setRenderHint(QPainter.Antialiasing)
        p.fillRect(self.rect(), QColor(PANEL))
        left, right = 45, self.width() - 10
        usable = right - left

        def db_to_width(db):
            return max(0.0, min(1.0, (db + 60) / 60)) * usable

        input_db, output_db = self.level_meter.get_current()
        p.setFont(QFont('Menlo', 9))
        for y, label, db, colors in (
                (12, "IN", input_db, ('#0f0', '#ff0', '#f00')),
                (42, "OUT", output_db, ('#0af', '#f0f', '#f00'))):
            p.setPen(QColor('#0a0'))
            p.drawText(QRectF(8, y, 36, 16), Qt.AlignVCenter | Qt.AlignLeft, label)
            p.setPen(QColor('#333'))
            p.setBrush(QColor('#1a1a2a'))
            p.drawRect(QRectF(left, y, usable, 16))
            color = colors[0] if db < -6 else colors[1] if db < -3 else colors[2]
            p.setPen(Qt.NoPen)
            p.setBrush(QColor(color))
            p.drawRect(QRectF(left, y, db_to_width(db), 16))

        p.setFont(QFont('Menlo', 8))
        for db in (-48, -24, -12, -6, 0):
            x = left + db_to_width(db)
            p.setPen(QColor('#444'))
            p.drawLine(QPointF(x, 62), QPointF(x, 68))
            p.drawText(QRectF(x - 15, 68, 30, 12), Qt.AlignCenter, str(db))
        p.end()


class AudioEffectsApp(QMainWindow):
    """Hauptanwendung."""

    SAMPLE_RATE = 44100
    CHUNK_SIZE = 1024

    def __init__(self):
        super().__init__()
        self.processor = AudioProcessor(self.SAMPLE_RATE, self.CHUNK_SIZE)
        self.level_meter = LevelMeter()
        self.stream: Optional[sd.Stream] = None
        # Begrenzt: blockiert die GUI, darf der Audio-Thread nicht Speicher fressen
        self.gui_queue = queue.Queue(maxsize=200)
        self.input_devices = []
        self.output_devices = []

        self._setup_gui()

        self.timer = QTimer(self)
        self.timer.timeout.connect(self._update_gui)
        self.timer.start(30)

    def _refresh_devices(self):
        """Aktualisiert die Liste der Audio-Geräte."""
        self.input_devices = []
        self.output_devices = []

        try:
            devices = sd.query_devices()
            for i, dev in enumerate(devices):
                name = f"{i}: {dev['name']}"
                if dev['max_input_channels'] > 0:
                    self.input_devices.append((i, name))
                if dev['max_output_channels'] > 0:
                    self.output_devices.append((i, name))
        except Exception as e:
            self.status_label.setText(f"Geräteabfrage fehlgeschlagen: {e}")

    def _populate_device_combos(self):
        """Füllt die Comboboxen und behält die aktuelle Auswahl bei."""
        settings = self.processor.get_settings()

        for combo, devices, selected in (
                (self.input_combo, self.input_devices, settings.input_device),
                (self.output_combo, self.output_devices, settings.output_device)):
            combo.blockSignals(True)
            combo.clear()
            combo.addItems(["Default"] + [d[1] for d in devices])
            ids = [d[0] for d in devices]
            combo.setCurrentIndex(ids.index(selected) + 1 if selected in ids else 0)
            combo.blockSignals(False)

    def _do_refresh_devices(self):
        self._refresh_devices()
        self._populate_device_combos()
        self._on_device_change()

    def _on_device_change(self, *_):
        """Ermittelt Device-IDs, aktualisiert Settings, startet Stream neu falls aktiv."""
        input_sel = self.input_combo.currentIndex()
        input_id = None if input_sel <= 0 else self.input_devices[input_sel - 1][0]

        output_sel = self.output_combo.currentIndex()
        output_id = None if output_sel <= 0 else self.output_devices[output_sel - 1][0]

        old = self.processor.get_settings()
        if (old.input_device, old.output_device) == (input_id, output_id):
            return

        self.processor.update_settings(input_device=input_id, output_device=output_id)

        if self.stream is not None:
            self._stop_stream()
            self._start_stream()

    def _start_stream(self):
        """Startet den Audio-Stream mit ausgewählten Devices."""
        self._stop_stream()
        try:
            settings = self.processor.get_settings()
            self.processor.request_reset()
            self.stream = sd.Stream(
                callback=self._audio_callback,
                samplerate=self.SAMPLE_RATE,
                blocksize=self.CHUNK_SIZE,
                device=(settings.input_device, settings.output_device),
                channels=1,
                dtype=np.float32
            )
            self.stream.start()
            self.start_button.setText("⏹ STOP")
            self._on_settings_change()
            return True
        except Exception as e:
            self._stop_stream()
            self.status_label.setText(f"FEHLER: {e}")
            self.start_button.setText("▶ START")
            return False

    def _stop_stream(self):
        stream, self.stream = self.stream, None
        if stream is not None:
            try:
                stream.stop()
                stream.close()
            except Exception as e:
                print(f"Stream-Stop Fehler: {e}")

    @staticmethod
    def _toggle_group(parent_layout: QHBoxLayout, options, default: str, on_change) -> QButtonGroup:
        """Reihe exklusiver Umschaltknöpfe; die ganze Knopffläche ist anklickbar."""
        group = QButtonGroup(parent_layout)
        group.setExclusive(True)
        for text, value in options:
            button = QPushButton(text)
            button.setCheckable(True)
            button.setChecked(value == default)
            button.setProperty('value', value)
            button.setCursor(Qt.PointingHandCursor)
            group.addButton(button)
            parent_layout.addWidget(button)
        parent_layout.addStretch()
        group.buttonClicked.connect(lambda _: on_change())
        return group

    @staticmethod
    def _group_value(group: QButtonGroup) -> str:
        return group.checkedButton().property('value')

    def _setup_gui(self):
        self.setWindowTitle("Vericrypt-Style Scrambler")
        self.resize(1000, 600)
        self.setStyleSheet(STYLESHEET)

        scroll = QScrollArea()
        scroll.setWidgetResizable(True)
        self.setCentralWidget(scroll)
        content = QWidget()
        scroll.setWidget(content)
        page = QVBoxLayout(content)
        page.setContentsMargins(15, 10, 15, 15)
        page.setSpacing(8)
        columns = QHBoxLayout()
        columns.setSpacing(15)
        # Links Einstellungen, rechts Anzeige und START: passt ohne Scrollen auf kleine Bildschirme
        left, right = QVBoxLayout(), QVBoxLayout()
        left.setSpacing(8)
        right.setSpacing(8)
        columns.addLayout(left, 3)
        columns.addLayout(right, 2)

        title = QLabel("≡ VERICRYPT ≡")
        title.setAlignment(Qt.AlignCenter)
        title.setStyleSheet(f"color: {ACCENT}; font-size: 22px; font-weight: bold;")
        page.addWidget(title)
        subtitle = QLabel("Time-Division Speech Scrambler (BBC 1979 Style)")
        subtitle.setAlignment(Qt.AlignCenter)
        subtitle.setStyleSheet("color: #888; font-size: 11px;")
        page.addWidget(subtitle)
        page.addLayout(columns)

        # Status früh anlegen: _refresh_devices meldet Fehler darüber
        self.status_label = QLabel("BEREIT")
        self.status_label.setAlignment(Qt.AlignCenter)
        self.status_label.setStyleSheet("color: #ff0; font-size: 13px;")

        # Audio-Geräte
        device_box = QGroupBox("🎤 AUDIO DEVICES")
        device_layout = QGridLayout(device_box)
        self._refresh_devices()
        self.input_combo = NoWheelComboBox()
        self.output_combo = NoWheelComboBox()
        device_layout.addWidget(QLabel("INPUT:"), 0, 0)
        device_layout.addWidget(self.input_combo, 0, 1)
        device_layout.addWidget(QLabel("OUTPUT:"), 1, 0)
        device_layout.addWidget(self.output_combo, 1, 1)
        refresh_button = QPushButton("🔄")
        refresh_button.setToolTip("Geräteliste neu einlesen")
        refresh_button.clicked.connect(self._do_refresh_devices)
        device_layout.addWidget(refresh_button, 0, 2, 2, 1)
        device_layout.setColumnStretch(1, 1)
        self._populate_device_combos()
        self.input_combo.currentIndexChanged.connect(self._on_device_change)
        self.output_combo.currentIndexChanged.connect(self._on_device_change)
        left.addWidget(device_box)

        # Crypto
        crypto_box = QGroupBox("⚡ CRYPTO")
        crypto_layout = QVBoxLayout(crypto_box)

        mode_row = QHBoxLayout()
        mode_row.addWidget(self._bold_label("MODE:"))
        self.mode_group = self._toggle_group(
            mode_row, (("CLEAR", CryptoMode.OFF.value), ("🔒 ENCRYPT", CryptoMode.ENCRYPT.value),
                       ("🔓 DECRYPT", CryptoMode.DECRYPT.value)),
            CryptoMode.OFF.value, self._on_settings_change)
        crypto_layout.addLayout(mode_row)

        sync_row = QHBoxLayout()
        sync_row.addWidget(self._bold_label("SYNC:"))
        self.sync_group = self._toggle_group(
            sync_row, (("FSK 3.8/4.4 kHz", SyncMode.FSK.value),
                       ("Pilotton 1200/1800 Hz (Funk)", SyncMode.PILOT.value),
                       ("BBC 1830 Hz (Original)", SyncMode.BBC.value)),
            SyncMode.FSK.value, self._on_settings_change)
        crypto_layout.addLayout(sync_row)

        key_row = QHBoxLayout()
        key_row.addWidget(self._bold_label("KEY: "))
        self.key_edit = QLineEdit("123456")
        self.key_edit.setMaxLength(6)
        self.key_edit.setFixedWidth(110)
        self.key_edit.setAlignment(Qt.AlignCenter)
        self.key_edit.setStyleSheet("font-size: 17px; font-weight: bold;")
        self.key_edit.textEdited.connect(lambda _: self._on_settings_change())
        key_row.addWidget(self.key_edit)
        self.key_hint = QLabel("(6-stellig: 000000-999999)")
        self.key_hint.setStyleSheet("color: #666;")
        key_row.addWidget(self.key_hint)
        key_row.addStretch()
        crypto_layout.addLayout(key_row)

        self.info_label = QLabel()
        self.info_label.setStyleSheet("color: #0a0; font-size: 11px;")
        self.info_label.setWordWrap(True)
        crypto_layout.addWidget(self.info_label)
        left.addWidget(crypto_box)

        # Effekte
        effects_box = QGroupBox("⚙ EFFEKTE")
        effects_layout = QVBoxLayout(effects_box)
        self.inversion_check = WideCheckBox("Frequenzinversion (Voice Inverter, bei ENCRYPT und DECRYPT gleich setzen)")
        self.bandpass_check = WideCheckBox("Bandpass 300-3400 Hz (Telefon)")
        for check in (self.inversion_check, self.bandpass_check):
            check.setCursor(Qt.PointingHandCursor)
            check.toggled.connect(lambda _: self._on_settings_change())
            effects_layout.addWidget(check)
        left.addWidget(effects_box)
        left.addStretch()

        # Segment-Ring
        ring_box = QGroupBox("◎ SEGMENT-RING")
        ring_layout = QVBoxLayout(ring_box)
        self.ring = SegmentRing(self.processor)
        ring_layout.addWidget(self.ring)
        right.addWidget(ring_box)

        # Pegel
        meter_box = QGroupBox("📊 PEGEL")
        meter_layout = QVBoxLayout(meter_box)
        self.meters = LevelBars(self.level_meter)
        meter_layout.addWidget(self.meters)
        right.addWidget(meter_box)

        right.addWidget(self.status_label)
        self.start_button = QPushButton("▶ START")
        self.start_button.setObjectName('start')
        self.start_button.setCursor(Qt.PointingHandCursor)
        self.start_button.clicked.connect(self._toggle_stream)
        right.addWidget(self.start_button, alignment=Qt.AlignCenter)
        right.addStretch()

        self._on_settings_change()

    @staticmethod
    def _bold_label(text: str) -> QLabel:
        label = QLabel(text)
        label.setStyleSheet("font-weight: bold;")
        label.setFixedWidth(52)
        return label

    def _parse_key(self) -> Optional[int]:
        text = self.key_edit.text().strip()
        if text.isdigit() and len(text) <= 6:
            return int(text)
        return None

    def _on_settings_change(self):
        crypto_mode = CryptoMode(self._group_value(self.mode_group))
        sync_mode = SyncMode(self._group_value(self.sync_group))

        if sync_mode == SyncMode.PILOT:
            self.info_label.setText("▸ 30ms Segmente  ▸ Pilot 1200/1800 Hz im Sprachband  ▸ Latenz ~0.6s / ~1.6s")
        elif sync_mode == SyncMode.BBC:
            self.info_label.setText("▸ 30ms Segmente  ▸ FSK-Pilot 1830±100 Hz wie das Original  ▸ Latenz ~0.5s / ~1s")
        else:
            self.info_label.setText("▸ 30ms Segmente  ▸ FSK 3.8/4.4 kHz, nur Soundkarte/Band  ▸ Latenz ~0.5s / ~1s")

        updates = dict(
            inversion=self.inversion_check.isChecked(),
            bandpass=self.bandpass_check.isChecked(),
            crypto_mode=crypto_mode,
            sync_mode=sync_mode,
        )
        key = self._parse_key()
        if key is None:
            # Ungültige Eingabe nicht still durch einen Default-Key ersetzen
            self.key_hint.setText("⚠ ungültig — alter Key aktiv")
            self.key_hint.setStyleSheet("color: #f44;")
        else:
            updates['crypto_key'] = key
            self.key_hint.setText(f"(aktiv: {key:06d})")
            self.key_hint.setStyleSheet("color: #666;")

        self.processor.update_settings(**updates)

        if crypto_mode == CryptoMode.ENCRYPT:
            self.status_label.setText("🔒 ENCRYPT - Aufnahme auf Tape")
        elif crypto_mode == CryptoMode.DECRYPT:
            self.status_label.setText("🔓 DECRYPT - Wiedergabe von Tape")
        elif self.stream:
            self.status_label.setText("▶ CLEAR - Durchgang")
        else:
            self.status_label.setText("BEREIT")

    def _audio_callback(self, indata, outdata, frames, time, status):
        try:
            input_data = indata[:, 0].copy()
            output_data = self.processor.process(input_data)
            outdata[:, 0] = output_data
            message = ('level', (LevelMeter.to_db(input_data), LevelMeter.to_db(output_data)))
        except Exception as e:
            outdata.fill(0)
            message = ('status', f"DSP-FEHLER: {e}")

        try:
            if status:
                self.gui_queue.put_nowait(('status', str(status)))
            self.gui_queue.put_nowait(message)
        except queue.Full:
            pass

    def _toggle_stream(self):
        if self.stream is None:
            self._start_stream()
        else:
            self._stop_stream()
            self.start_button.setText("▶ START")
            self.status_label.setText("GESTOPPT")

    def closeEvent(self, event):
        self.timer.stop()
        self._stop_stream()
        super().closeEvent(event)

    def _update_gui(self):
        while True:
            try:
                msg_type, data = self.gui_queue.get_nowait()
            except queue.Empty:
                break
            if msg_type == 'level':
                self.level_meter.update(*data)
            elif msg_type == 'status':
                self.status_label.setText(data)

        self.meters.update()
        self.ring.update()


if __name__ == '__main__':
    qt_app = QApplication(sys.argv)
    qt_app.setStyle('Fusion')
    window = AudioEffectsApp()
    window.show()
    exit_code = qt_app.exec()
    window._stop_stream()
    sys.exit(exit_code)
