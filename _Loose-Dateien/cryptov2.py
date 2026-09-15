#!/usr/bin/env python3
"""
Vericrypt-Style Audio Scrambler

Inspiriert vom BBC Vericrypt 1100 (1979):
- Time-Division Scrambling mit 30ms Segmenten (16 Segmente = 480ms Frame)
- FSK-Synchronisation mit 100 Baud, ausserhalb des Sprachbands
- 6-stelliger Schlüssel (000000-999999)

Funktionsweise:
1. ENCRYPT: Sprache wird auf 300-3000 Hz begrenzt, in 30ms Segmente zerlegt
   und permutiert. Oberhalb des Sprachbands (3800/4400 Hz) liegt ein FSK-Signal,
   das pro Frame Sync-Wort, Frame-Zähler und CRC trägt.
2. DECRYPT: Das Sync-Wort wird samplegenau gesucht (Matched Filter). Damit
   stimmen die Segmentgrenzen auch nach Band, Lautsprecher/Mikrofon oder
   Verzögerung. Danach werden die Segmente zurück-permutiert und das FSK
   herausgefiltert.

Frame-Aufbau (48 Bit = exakt 480ms bei 100 Baud):
    SYNC (16) | ZÄHLER (16) | CRC-8 (8) | FÜLLBITS (8)
"""

import hashlib
import math
import queue
import threading
from dataclasses import dataclass, replace
from enum import Enum
from typing import Optional, List, Tuple

import numpy as np
import sounddevice as sd
import tkinter as tk
from tkinter import ttk
from scipy.signal import butter, ellip, sosfilt, sosfiltfilt


class CryptoMode(Enum):
    """Betriebsmodus."""
    OFF = "off"
    ENCRYPT = "encrypt"
    DECRYPT = "decrypt"


@dataclass
class AudioSettings:
    """Einstellungen."""
    inversion: bool = False
    bandpass: bool = False
    crypto_mode: CryptoMode = CryptoMode.OFF
    crypto_key: int = 123456
    input_device: Optional[int] = None   # None = Default
    output_device: Optional[int] = None  # None = Default


class StreamingFilter:
    """SOS-Filter mit Zustand über Blockgrenzen hinweg (sonst Klicks alle ~23ms)."""

    def __init__(self, sos: np.ndarray):
        self.sos = sos
        self.zi = np.zeros((sos.shape[0], 2))

    def process(self, data: np.ndarray) -> np.ndarray:
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

    def __init__(self, sample_rate: int):
        if sample_rate % self.BAUD_RATE:
            raise ValueError("Samplerate muss ein Vielfaches von 100 sein")
        self.sample_rate = sample_rate
        self.samples_per_bit = sample_rate // self.BAUD_RATE
        self.phase = 0.0
        self.sync_template = np.array(int_to_bits(self.SYNC_WORD, self.SYNC_BITS)) * 2.0 - 1.0
        self._highpass = ellip(8, 0.5, 70, 3500, btype='high', fs=sample_rate, output='sos')
        self._min_energy = (self.MIN_AMPLITUDE * self.samples_per_bit / 2) ** 2

    def build_frame_bits(self, counter: int) -> List[int]:
        counter_bytes = counter.to_bytes(2, 'big')
        return (int_to_bits(self.SYNC_WORD, 16) + int_to_bits(counter, 16)
                + int_to_bits(crc8(counter_bytes), 8) + [1, 0] * 4)

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
        x = sosfiltfilt(self._highpass, x.astype(np.float64))  # nullphasig: kein Zeitversatz
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
    """ENCRYPT-Pfad: Bandbegrenzung -> (Inversion) -> Permutation -> Tiefpass -> + FSK."""

    def __init__(self, sample_rate: int, scrambler: VericryptScrambler, modem: FSKModem):
        self.scrambler = scrambler
        self.modem = modem
        self.speech_filter = StreamingFilter(
            butter(4, [300, 3000], btype='band', fs=sample_rate, output='sos'))
        self.inverter = FrequencyInverter(sample_rate)
        # Segmentsprünge erzeugen breitbandige Klicks, die sonst ins FSK-Band streuen
        self.post_filter = StreamingFilter(speech_lowpass(sample_rate))
        self.counter = 0
        self.last_permutation = np.arange(scrambler.FRAME_SEGMENTS)

    def encode(self, frame: np.ndarray, settings: AudioSettings) -> np.ndarray:
        speech = self.speech_filter.process(frame)
        if settings.inversion:
            speech = self.inverter.process(speech)

        perm = self.scrambler.permutation(settings.crypto_key, self.counter)
        self.last_permutation = perm
        scrambled = self.post_filter.process(self.scrambler.scramble(speech, perm))

        fsk = self.modem.modulate(self.modem.build_frame_bits(self.counter))
        self.counter = (self.counter + 1) & 0xFFFF
        return np.clip(scrambled + fsk, -1.0, 1.0)


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

    def __init__(self, sample_rate: int, scrambler: VericryptScrambler, modem: FSKModem,
                 speech_delay: int):
        self.scrambler = scrambler
        self.modem = modem
        self.speech_delay = speech_delay
        self.tol = modem.samples_per_bit * 4
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

    def _track(self, found) -> Optional[Tuple[float, int]]:
        """Entscheidet über Frame-Position und Zähler. None = kein Sync."""
        spb = self.modem.samples_per_bit
        measured, counter, _ = found if found is not None else (None, None, 0.0)

        if not self.locked:
            if counter is None:
                return None
            self.locked, self.missed, self.locked_frames, self.drift = True, 0, 0, 0.0
            return float(measured), counter

        innovation = None if measured is None else measured - self.expected
        if innovation is not None and abs(innovation) > spb:
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

    def pump(self, in_buf: RingBuffer, out_buf: RingBuffer, settings: AudioSettings):
        frame_len = self.scrambler.frame_samples
        spb = self.modem.samples_per_bit

        while True:
            if self.locked:
                lo, hi = 0, 2 * self.tol
            else:
                lo, hi = self.tol, self.tol + frame_len
            need = hi + frame_len + self.speech_delay + int(self.max_drift) + 2 * spb
            if in_buf.available() < need:
                return

            window = in_buf.peek(need)
            decision = self._track(self.modem.find_frame(window, lo, hi))

            if decision is None:
                # Kein Sync: Stille ausgeben statt Rauschen, weiter suchen
                in_buf.discard(frame_len)
                out_buf.write(np.zeros(frame_len, dtype=np.float32))
                continue

            position, counter = decision
            actual_len = frame_len + self.drift
            sample_pos = position + self.speech_delay + np.arange(frame_len) * (actual_len / frame_len)
            frame = np.interp(sample_pos, np.arange(len(window)), window).astype(np.float32)

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
            self.expected = next_start - consumed
            self.counter = (counter + 1) & 0xFFFF


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
        self.modem = FSKModem(sample_rate)
        assert self.scrambler.frame_samples == self.modem.FRAME_BITS * self.modem.samples_per_bit

        # Laufzeit des Tiefpasses nach dem Scrambling: um so viel liegt die
        # Sprache hinter dem (unverzögerten) FSK-Frame-Anfang
        impulse = np.zeros(4096)
        impulse[0] = 1.0
        self.speech_delay = int(np.argmax(np.abs(sosfilt(speech_lowpass(sample_rate), impulse))))

        self._init_state(CryptoMode.OFF)

    def _init_state(self, mode: CryptoMode):
        capacity = self.scrambler.frame_samples * 4 + self.chunk_size
        self.in_buffer = RingBuffer(capacity)
        self.out_buffer = RingBuffer(capacity)
        self.encoder = FrameEncoder(self.sample_rate, self.scrambler, self.modem)
        self.decoder = FrameDecoder(self.sample_rate, self.scrambler, self.modem, self.speech_delay)
        self.clear_inverter = FrequencyInverter(self.sample_rate)
        self.clear_bandpass = StreamingFilter(
            butter(4, [300, 3400], btype='band', fs=self.sample_rate, output='sos'))
        self.modem.phase = 0.0
        self._active_mode = mode
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
        if self._reset_requested.is_set() or settings.crypto_mode != self._active_mode:
            self._reset_requested.clear()
            self._init_state(settings.crypto_mode)

        if settings.crypto_mode == CryptoMode.OFF:
            if settings.inversion:
                data = self.clear_inverter.process(data)
            if settings.bandpass:
                data = self.clear_bandpass.process(data)
            return data.astype(np.float32)

        self.in_buffer.write(data)
        if settings.crypto_mode == CryptoMode.ENCRYPT:
            frame_len = self.scrambler.frame_samples
            while self.in_buffer.available() >= frame_len:
                frame = self.in_buffer.read(frame_len)
                self.out_buffer.write(self.encoder.encode(frame, settings))
        else:
            self.decoder.pump(self.in_buffer, self.out_buffer, settings)

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


class AudioEffectsApp:
    """Hauptanwendung."""

    SAMPLE_RATE = 44100
    CHUNK_SIZE = 1024

    def __init__(self):
        self.processor = AudioProcessor(self.SAMPLE_RATE, self.CHUNK_SIZE)
        self.level_meter = LevelMeter()
        self.stream: Optional[sd.Stream] = None
        # Begrenzt: blockiert die GUI, darf der Audio-Thread nicht Speicher fressen
        self.gui_queue = queue.Queue(maxsize=200)
        self.input_devices = []
        self.output_devices = []

        self._setup_gui()

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
            self.status_var.set(f"Geräteabfrage fehlgeschlagen: {e}")

    def _populate_device_combos(self):
        """Füllt die Comboboxen und behält die aktuelle Auswahl bei."""
        settings = self.processor.get_settings()

        for combo, devices, selected in (
                (self.input_combo, self.input_devices, settings.input_device),
                (self.output_combo, self.output_devices, settings.output_device)):
            combo['values'] = ["Default"] + [d[1] for d in devices]
            ids = [d[0] for d in devices]
            combo.current(ids.index(selected) + 1 if selected in ids else 0)

    def _do_refresh_devices(self):
        self._refresh_devices()
        self._populate_device_combos()
        self._on_device_change()

    def _on_device_change(self, event=None):
        """Ermittelt Device-IDs, aktualisiert Settings, startet Stream neu falls aktiv."""
        input_sel = self.input_combo.current()
        input_id = None if input_sel <= 0 else self.input_devices[input_sel - 1][0]

        output_sel = self.output_combo.current()
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
            self.button_text.set("⏹ STOP")
            self._on_settings_change()
            return True
        except Exception as e:
            self._stop_stream()
            self.status_var.set(f"FEHLER: {e}")
            self.button_text.set("▶ START")
            return False

    def _stop_stream(self):
        stream, self.stream = self.stream, None
        if stream is not None:
            try:
                stream.stop()
                stream.close()
            except Exception as e:
                print(f"Stream-Stop Fehler: {e}")

    def _setup_gui(self):
        self.root = tk.Tk()
        self.root.title("Vericrypt-Style Scrambler")
        self.root.geometry("700x720")
        self.root.configure(bg='#1a1a2e')
        self.root.protocol("WM_DELETE_WINDOW", self._on_close)

        style = ttk.Style()
        style.theme_use('clam')
        style.configure('TCheckbutton', background='#1a1a2e', foreground='#eee')
        style.configure('TRadiobutton', background='#1a1a2e', foreground='#eee')
        style.configure('TButton', padding=10)
        style.configure('TLabel', background='#1a1a2e', foreground='#eee')
        style.configure('TLabelframe', background='#1a1a2e', foreground='#0f0')
        style.configure('TLabelframe.Label', background='#1a1a2e', foreground='#0f0')
        style.configure('TEntry', fieldbackground='#2a2a4e')
        style.configure('TFrame', background='#1a1a2e')
        style.configure('TCombobox', fieldbackground='#2a2a4e')

        # Scrollbarer Container
        container = ttk.Frame(self.root)
        container.pack(fill='both', expand=True)

        self.scroll_canvas = tk.Canvas(container, bg='#1a1a2e', highlightthickness=0)
        scrollbar = ttk.Scrollbar(container, orient='vertical', command=self.scroll_canvas.yview)

        self.scrollable_frame = ttk.Frame(self.scroll_canvas)
        self.scrollable_frame.bind(
            '<Configure>',
            lambda e: self.scroll_canvas.configure(scrollregion=self.scroll_canvas.bbox('all'))
        )

        self._canvas_window = self.scroll_canvas.create_window(
            (0, 0), window=self.scrollable_frame, anchor='nw'
        )
        self.scroll_canvas.configure(yscrollcommand=scrollbar.set)

        def _on_mousewheel(event):
            # macOS liefert kleine Deltas (±1..±n), Windows ±120 — nur das Vorzeichen zählt
            if event.delta:
                self.scroll_canvas.yview_scroll(-1 if event.delta > 0 else 1, 'units')

        def _on_mousewheel_linux_up(event):
            self.scroll_canvas.yview_scroll(-1, 'units')

        def _on_mousewheel_linux_down(event):
            self.scroll_canvas.yview_scroll(1, 'units')

        def _bind_mousewheel(widget):
            widget.bind('<MouseWheel>', _on_mousewheel)
            widget.bind('<Button-4>', _on_mousewheel_linux_up)
            widget.bind('<Button-5>', _on_mousewheel_linux_down)

        _bind_mousewheel(self.scroll_canvas)
        self._bind_mousewheel_to_widget = _bind_mousewheel

        scrollbar.pack(side='right', fill='y')
        self.scroll_canvas.pack(side='left', fill='both', expand=True)

        def _on_canvas_resize(event):
            self.scroll_canvas.itemconfig(self._canvas_window, width=event.width)

        self.scroll_canvas.bind('<Configure>', _on_canvas_resize)

        # Ab hier: Alle Widgets in self.scrollable_frame
        main_frame = self.scrollable_frame

        # Header
        ttk.Label(
            main_frame,
            text="≡ VERICRYPT ≡",
            font=('Courier', 18, 'bold'),
            foreground='#0f0'
        ).pack(pady=10)

        ttk.Label(
            main_frame,
            text="Time-Division Speech Scrambler (BBC 1979 Style)",
            font=('Courier', 9),
            foreground='#888'
        ).pack()

        # Status früh anlegen: _refresh_devices meldet Fehler darüber
        self.status_var = tk.StringVar(value="BEREIT")

        # Audio Device Frame
        device_frame = ttk.LabelFrame(main_frame, text="🎤 AUDIO DEVICES", padding=10)
        device_frame.pack(fill='x', padx=15, pady=10)

        self._refresh_devices()

        # Input Device
        input_frame = ttk.Frame(device_frame)
        input_frame.pack(fill='x', pady=(0, 5))

        ttk.Label(input_frame, text="INPUT:", width=8).pack(side='left')
        self.input_combo = ttk.Combobox(input_frame, state='readonly', width=45)
        self.input_combo.pack(side='left', fill='x', expand=True, padx=(5, 0))
        self.input_combo.bind('<<ComboboxSelected>>', self._on_device_change)

        # Output Device
        output_frame = ttk.Frame(device_frame)
        output_frame.pack(fill='x')

        ttk.Label(output_frame, text="OUTPUT:", width=8).pack(side='left')
        self.output_combo = ttk.Combobox(output_frame, state='readonly', width=45)
        self.output_combo.pack(side='left', fill='x', expand=True, padx=(5, 0))
        self.output_combo.bind('<<ComboboxSelected>>', self._on_device_change)

        refresh_btn = ttk.Button(device_frame, text="🔄", width=3, command=self._do_refresh_devices)
        refresh_btn.pack(side='right', pady=(5, 0))

        self._populate_device_combos()

        # Crypto-Frame
        crypto_frame = ttk.LabelFrame(main_frame, text="⚡ CRYPTO", padding=10)
        crypto_frame.pack(fill='x', padx=15, pady=10)

        self.crypto_mode_var = tk.StringVar(value="off")

        modes_frame = ttk.Frame(crypto_frame)
        modes_frame.pack(fill='x')

        for text, value in (("CLEAR", "off"), ("🔒 ENCRYPT", "encrypt"), ("🔓 DECRYPT", "decrypt")):
            ttk.Radiobutton(
                modes_frame, text=text,
                variable=self.crypto_mode_var, value=value,
                command=self._on_settings_change
            ).pack(side='left', padx=(0, 20))

        # Schlüssel
        key_frame = ttk.Frame(crypto_frame)
        key_frame.pack(fill='x', pady=(15, 5))

        ttk.Label(key_frame, text="KEY:", font=('Courier', 10, 'bold')).pack(side='left')

        self.crypto_key_var = tk.StringVar(value="123456")
        key_entry = ttk.Entry(
            key_frame,
            textvariable=self.crypto_key_var,
            width=8,
            font=('Courier', 14, 'bold'),
            justify='center'
        )
        key_entry.pack(side='left', padx=(10, 10))
        key_entry.bind('<KeyRelease>', lambda e: self._on_settings_change())

        self.key_hint_var = tk.StringVar(value="(6-stellig: 000000-999999)")
        self.key_hint = ttk.Label(key_frame, textvariable=self.key_hint_var, foreground='#666')
        self.key_hint.pack(side='left')

        # Info
        info_frame = ttk.Frame(crypto_frame)
        info_frame.pack(fill='x', pady=(10, 0))

        ttk.Label(
            info_frame,
            text="▸ 30ms Segmente  ▸ FSK-Sync 3.8/4.4 kHz  ▸ Latenz ~0.5s / ~1s",
            foreground='#0a0',
            font=('Courier', 8)
        ).pack()

        # Effekte-Frame
        effects_frame = ttk.LabelFrame(main_frame, text="⚙ EFFEKTE", padding=10)
        effects_frame.pack(fill='x', padx=15, pady=10)

        self.inversion_var = tk.BooleanVar(value=False)
        self.bandpass_var = tk.BooleanVar(value=False)

        ttk.Checkbutton(
            effects_frame,
            text="Frequenzinversion (Voice Inverter, bei ENCRYPT und DECRYPT gleich setzen)",
            variable=self.inversion_var,
            command=self._on_settings_change
        ).pack(anchor='w')

        ttk.Checkbutton(
            effects_frame,
            text="Bandpass 300-3400 Hz (Telefon)",
            variable=self.bandpass_var,
            command=self._on_settings_change
        ).pack(anchor='w')

        # Ring-Visualisierung
        ring_frame = ttk.LabelFrame(main_frame, text="◎ SEGMENT-RING", padding=10)
        ring_frame.pack(fill='x', padx=15, pady=10)

        self.ring_canvas = tk.Canvas(
            ring_frame,
            bg='#0a0a1a',
            height=150,
            highlightthickness=1,
            highlightbackground='#333'
        )
        self.ring_canvas.pack(fill='x')

        # Pegel-Anzeige
        meter_frame = ttk.LabelFrame(main_frame, text="📊 PEGEL", padding=10)
        meter_frame.pack(fill='x', padx=15, pady=10)

        self.meter_canvas = tk.Canvas(
            meter_frame,
            bg='#0a0a1a',
            height=80,
            highlightthickness=1,
            highlightbackground='#333'
        )
        self.meter_canvas.pack(fill='both', expand=True)

        # Status
        ttk.Label(
            main_frame,
            textvariable=self.status_var,
            font=('Courier', 10),
            foreground='#ff0'
        ).pack(pady=5)

        self.button_text = tk.StringVar(value="▶ START")
        self.toggle_button = ttk.Button(
            main_frame,
            textvariable=self.button_text,
            command=self._toggle_stream
        )
        self.toggle_button.pack(pady=10)

        ttk.Frame(main_frame, height=20).pack()

        self.root.after(100, self._bind_scroll_to_children)

        self.root.focus_set()
        self._update_gui()

    def _bind_scroll_to_children(self):
        """Bindet Mousewheel rekursiv, Comboboxen ausgenommen."""
        def _recurse(widget):
            if not isinstance(widget, ttk.Combobox):
                self._bind_mousewheel_to_widget(widget)
                for child in widget.winfo_children():
                    _recurse(child)

        _recurse(self.scrollable_frame)

    def _parse_key(self) -> Optional[int]:
        text = self.crypto_key_var.get().strip()
        if text.isdigit() and len(text) <= 6:
            return int(text)
        return None

    def _on_settings_change(self):
        crypto_mode = CryptoMode(self.crypto_mode_var.get())

        updates = dict(
            inversion=self.inversion_var.get(),
            bandpass=self.bandpass_var.get(),
            crypto_mode=crypto_mode,
        )
        key = self._parse_key()
        if key is None:
            # Ungültige Eingabe nicht still durch einen Default-Key ersetzen
            self.key_hint_var.set("⚠ ungültig — alter Key aktiv")
            self.key_hint.configure(foreground='#f44')
        else:
            updates['crypto_key'] = key
            self.key_hint_var.set(f"(aktiv: {key:06d})")
            self.key_hint.configure(foreground='#666')

        self.processor.update_settings(**updates)

        if crypto_mode == CryptoMode.ENCRYPT:
            self.status_var.set("🔒 ENCRYPT - Aufnahme auf Tape")
        elif crypto_mode == CryptoMode.DECRYPT:
            self.status_var.set("🔓 DECRYPT - Wiedergabe von Tape")
        elif self.stream:
            self.status_var.set("▶ CLEAR - Durchgang")
        else:
            self.status_var.set("BEREIT")

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
            self.button_text.set("▶ START")
            self.status_var.set("GESTOPPT")

    def _on_close(self):
        self._stop_stream()
        self.root.destroy()

    def _update_gui(self):
        while True:
            try:
                msg_type, data = self.gui_queue.get_nowait()
            except queue.Empty:
                break
            if msg_type == 'level':
                self.level_meter.update(*data)
            elif msg_type == 'status':
                self.status_var.set(data)

        self._draw_meters()
        self._draw_ring()
        self.root.after(30, self._update_gui)

    def _draw_meters(self):
        canvas = self.meter_canvas
        canvas.delete('all')

        width = canvas.winfo_width()
        if width < 10:
            return

        input_db, output_db = self.level_meter.get_current()

        def db_to_width(db):
            normalized = (db + 60) / 60
            return max(0, min(1, normalized)) * (width - 60)

        canvas.create_text(10, 20, text="IN", fill='#0a0', anchor='w', font=('Courier', 9))
        canvas.create_rectangle(45, 12, width - 10, 28, fill='#1a1a2a', outline='#333')
        in_w = db_to_width(input_db)
        color = '#0f0' if input_db < -6 else '#ff0' if input_db < -3 else '#f00'
        canvas.create_rectangle(45, 12, 45 + in_w, 28, fill=color, outline='')

        canvas.create_text(10, 50, text="OUT", fill='#0a0', anchor='w', font=('Courier', 9))
        canvas.create_rectangle(45, 42, width - 10, 58, fill='#1a1a2a', outline='#333')
        out_w = db_to_width(output_db)
        color = '#0af' if output_db < -6 else '#f0f' if output_db < -3 else '#f00'
        canvas.create_rectangle(45, 42, 45 + out_w, 58, fill=color, outline='')

        for db in [-48, -24, -12, -6, 0]:
            x = 45 + db_to_width(db)
            canvas.create_line(x, 62, x, 70, fill='#333')
            canvas.create_text(x, 75, text=str(db), fill='#444', font=('', 7))

    def _draw_ring(self):
        """Zeichnet den Segment-Ring."""
        canvas = self.ring_canvas
        canvas.delete('all')

        width = canvas.winfo_width()
        height = canvas.winfo_height()
        if width < 50 or height < 50:
            return

        cx, cy = width // 2, height // 2
        radius_outer = min(cx, cy) - 10
        radius_inner = radius_outer - 25

        num_segments = VericryptScrambler.FRAME_SEGMENTS
        processor = self.processor
        crypto_mode = processor.get_settings().crypto_mode
        current_seg = processor.current_segment
        perm = processor.current_permutation

        colors = ['#2a1a1a', '#1a2a1a', '#1a1a2a', '#2a2a1a',
                  '#2a1a2a', '#1a2a2a', '#2a2a2a', '#3a1a1a',
                  '#1a3a1a', '#1a1a3a', '#3a3a1a', '#3a1a3a',
                  '#1a3a3a', '#3a3a3a', '#4a1a1a', '#1a4a1a']

        for i in range(num_segments):
            angle_start = (i / num_segments) * 2 * math.pi - math.pi / 2
            angle_end = ((i + 1) / num_segments) * 2 * math.pi - math.pi / 2

            points = []
            for a in [angle_start, (angle_start + angle_end) / 2, angle_end]:
                points.extend([cx + radius_outer * math.cos(a), cy + radius_outer * math.sin(a)])
            for a in [angle_end, (angle_start + angle_end) / 2, angle_start]:
                points.extend([cx + radius_inner * math.cos(a), cy + radius_inner * math.sin(a)])

            if crypto_mode == CryptoMode.OFF:
                fill, outline = '#1a1a2a', '#333'
            elif i == current_seg:
                fill, outline = '#0f0', '#0f0'
            else:
                fill, outline = colors[int(perm[i]) % len(colors)], '#444'

            canvas.create_polygon(points, fill=fill, outline=outline)

            mid_angle = (angle_start + angle_end) / 2
            text_r = (radius_outer + radius_inner) / 2
            tx = cx + text_r * math.cos(mid_angle)
            ty = cy + text_r * math.sin(mid_angle)

            label = str(int(perm[i])) if crypto_mode != CryptoMode.OFF else str(i)
            text_color = '#000' if i == current_seg and crypto_mode != CryptoMode.OFF else '#666'
            canvas.create_text(tx, ty, text=label, fill=text_color, font=('Courier', 8))

        canvas.create_oval(cx - radius_inner + 10, cy - radius_inner + 10,
                           cx + radius_inner - 10, cy + radius_inner - 10,
                           fill='#0a0a1a', outline='#333')

        if crypto_mode == CryptoMode.DECRYPT:
            locked = processor.sync_locked
            caption, caption_color = ("SYNC", '#0f0') if locked else ("NO SYNC", '#f44')
        else:
            caption, caption_color = "FRAME", '#666'
        canvas.create_text(cx, cy - 10, text=caption, fill=caption_color, font=('Courier', 8))
        canvas.create_text(cx, cy + 10, text=f"{processor.frame_counter:05d}", fill='#0f0',
                           font=('Courier', 14, 'bold'))

    def run(self):
        self.root.mainloop()
        self._stop_stream()


if __name__ == '__main__':
    app = AudioEffectsApp()
    app.run()
