#!/usr/bin/env python3
"""
Vericrypt-Style Audio Scrambler

Inspiriert vom BBC Vericrypt 1100 (1979):
- Time-Division Scrambling mit 30ms Segmenten
- FSK-Pilotton für Synchronisation (~100 Baud)
- 6-stelliger Schlüssel (000000-999999)

Funktionsweise:
1. ENCRYPT: Sprache wird in 30ms Segmente zerlegt und permutiert
2. Ein FSK-Sync-Signal wird dem Audio beigemischt
3. DECRYPT: FSK wird dekodiert, Segmente werden zurück-permutiert
"""

import math
import queue
import threading
from dataclasses import dataclass
from enum import Enum
from typing import Optional, List

import numpy as np
import sounddevice as sd
import tkinter as tk
from tkinter import ttk
from scipy.signal import butter, sosfilt


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


class FSKModem:
    """
    FSK-Modem für Synchronisation.
    Mark (1): 1200 Hz, Space (0): 1800 Hz, ~100 Baud.
    """

    MARK_FREQ = 1200
    SPACE_FREQ = 1800
    BAUD_RATE = 100
    AMPLITUDE = 0.03
    # FIX #5: Minimale Energie damit der Decoder nicht auf Rauschen reagiert
    ENERGY_THRESHOLD = 0.001

    def __init__(self, sample_rate: int):
        self.sample_rate = sample_rate
        self.samples_per_bit = int(sample_rate / self.BAUD_RATE)
        self.phase = 0.0

        self._fsk_bandpass = butter(
            N=4,
            Wn=[1000 / (0.5 * sample_rate), 2000 / (0.5 * sample_rate)],
            btype='band',
            output='sos'
        )

    def encode_bits(self, bits: List[int]) -> np.ndarray:
        """Kodiert Bits als FSK-Signal."""
        total_samples = self.samples_per_bit * len(bits)
        result = np.empty(total_samples, dtype=np.float32)

        pos = 0
        for bit in bits:
            freq = self.MARK_FREQ if bit else self.SPACE_FREQ
            phase_inc = 2 * np.pi * freq / self.sample_rate
            t = np.arange(self.samples_per_bit)
            phases = self.phase + phase_inc * t
            result[pos:pos + self.samples_per_bit] = self.AMPLITUDE * np.sin(phases)
            self.phase = phases[-1] % (2 * np.pi)
            pos += self.samples_per_bit

        return result

    def decode_bits(self, signal: np.ndarray) -> List[int]:
        """
        Dekodiert FSK-Signal zu Bits.
        FIX #5: Mit Energie-Schwellwert gegen Rausch-Fehlauslösung.
        """
        filtered = sosfilt(self._fsk_bandpass, signal).astype(np.float32)

        bits = []
        for i in range(0, len(filtered) - self.samples_per_bit, self.samples_per_bit):
            chunk = filtered[i:i + self.samples_per_bit]

            t = np.arange(len(chunk)) / self.sample_rate
            mark_ref = np.sin(2 * np.pi * self.MARK_FREQ * t)
            space_ref = np.sin(2 * np.pi * self.SPACE_FREQ * t)

            mark_energy = np.abs(np.sum(chunk * mark_ref))
            space_energy = np.abs(np.sum(chunk * space_ref))

            total_energy = mark_energy + space_energy
            if total_energy < self.ENERGY_THRESHOLD * self.samples_per_bit:
                bits.append(-1)  # Ungültig — zu wenig Signal
            else:
                bits.append(1 if mark_energy > space_energy else 0)

        return bits


class RingBuffer:
    """
    FIX #6: Numpy-basierter Ringbuffer statt deque.
    O(1) amortisiert für read/write statt O(n) Python-Loops pro Sample.
    """

    def __init__(self, capacity: int):
        self.buf = np.zeros(capacity, dtype=np.float32)
        self.capacity = capacity
        self.write_pos = 0
        self.read_pos = 0
        self.count = 0

    def write(self, data: np.ndarray):
        """Schreibt Samples in den Buffer."""
        n = len(data)
        if n > self.capacity - self.count:
            overflow = n - (self.capacity - self.count)
            self.read_pos = (self.read_pos + overflow) % self.capacity
            self.count -= overflow

        end = self.write_pos + n
        if end <= self.capacity:
            self.buf[self.write_pos:end] = data
        else:
            first = self.capacity - self.write_pos
            self.buf[self.write_pos:] = data[:first]
            self.buf[:n - first] = data[first:]

        self.write_pos = (self.write_pos + n) % self.capacity
        self.count += n

    def read(self, n: int) -> Optional[np.ndarray]:
        """Liest n Samples. Gibt None zurück wenn nicht genug da."""
        if n > self.count:
            return None

        end = self.read_pos + n
        if end <= self.capacity:
            result = self.buf[self.read_pos:end].copy()
        else:
            first = self.capacity - self.read_pos
            result = np.empty(n, dtype=np.float32)
            result[:first] = self.buf[self.read_pos:]
            result[first:] = self.buf[:n - first]

        self.read_pos = (self.read_pos + n) % self.capacity
        self.count -= n
        return result

    def available(self) -> int:
        return self.count

    def clear(self):
        self.write_pos = 0
        self.read_pos = 0
        self.count = 0


class VericryptScrambler:
    """
    Time-Division Scrambler im Vericrypt-Stil.
    16 Segmente à 30ms pro Frame = 480ms.
    """

    SEGMENT_MS = 30
    FRAME_SEGMENTS = 16

    def __init__(self, sample_rate: int):
        self.sample_rate = sample_rate
        self.segment_samples = int(sample_rate * self.SEGMENT_MS / 1000)
        self.frame_samples = self.segment_samples * self.FRAME_SEGMENTS

        self.fsk = FSKModem(sample_rate)
        self.frame_counter = 0

        self.current_segment = 0
        self.current_permutation = np.arange(self.FRAME_SEGMENTS)

        # FIX #7: Overlap-Add Crossfade
        self.fade_len = max(1, int(self.segment_samples * 0.05))
        self.fade_in = np.linspace(0.0, 1.0, self.fade_len, dtype=np.float32)
        self.fade_out = np.linspace(1.0, 0.0, self.fade_len, dtype=np.float32)

    def _get_permutation(self, key: int, frame_num: int, decrypt: bool = False) -> np.ndarray:
        seed = (key * 1000003 + frame_num * 1000033) % (2**31)
        rng = np.random.RandomState(seed)
        perm = rng.permutation(self.FRAME_SEGMENTS)

        if decrypt:
            inv = np.empty_like(perm)
            inv[perm] = np.arange(self.FRAME_SEGMENTS)
            return inv
        return perm

    def _create_sync_bits(self, frame_num: int, packet_num: int = 0) -> List[int]:
        preamble = [1, 0, 1, 0]
        counter_bits = [(frame_num >> i) & 1 for i in range(7, -1, -1)]
        packet_bits = [(packet_num >> i) & 1 for i in range(3, -1, -1)]
        checksum = (frame_num ^ packet_num ^ 0x5) & 0xF
        check_bits = [(checksum >> i) & 1 for i in range(3, -1, -1)]
        return preamble + counter_bits + packet_bits + check_bits

    def _apply_crossfade(self, segments: List[np.ndarray]) -> np.ndarray:
        """
        FIX #7: Overlap-Add Crossfade.
        Segmente überlappen um fade_len Samples und werden addiert
        statt destruktivem Fade (der Energieverlust verursacht).
        """
        if not segments:
            return np.array([], dtype=np.float32)

        fl = self.fade_len
        seg_len = self.segment_samples
        n = len(segments)
        total_len = n * seg_len - (n - 1) * fl
        result = np.zeros(total_len, dtype=np.float32)

        pos = 0
        for i, seg in enumerate(segments):
            s = seg.copy()
            if i < n - 1:
                s[-fl:] *= self.fade_out
            if i > 0:
                s[:fl] *= self.fade_in
            end = pos + seg_len
            result[pos:end] += s
            pos = end - fl

        return result

    def encrypt_frame(self, audio: np.ndarray, key: int) -> np.ndarray:
        segments = []
        for i in range(self.FRAME_SEGMENTS):
            start = i * self.segment_samples
            end = start + self.segment_samples
            if end <= len(audio):
                segments.append(audio[start:end].copy())
            else:
                seg = np.zeros(self.segment_samples, dtype=np.float32)
                remaining = len(audio) - start
                if remaining > 0:
                    seg[:remaining] = audio[start:]
                segments.append(seg)

        perm = self._get_permutation(key, self.frame_counter, decrypt=False)
        self.current_permutation = perm
        scrambled = [segments[p] for p in perm]

        audio_out = self._apply_crossfade(scrambled)

        # FSK-Sync generieren
        fsk_parts = []
        fsk_len = 0
        packet_num = 0
        target_len = len(audio_out)
        while fsk_len < target_len:
            sync_bits = self._create_sync_bits(self.frame_counter, packet_num)
            packet_signal = self.fsk.encode_bits(sync_bits)
            fsk_parts.append(packet_signal)
            fsk_len += len(packet_signal)
            packet_num = (packet_num + 1) % 16

        fsk_signal = np.concatenate(fsk_parts)[:target_len]

        self.frame_counter = (self.frame_counter + 1) % 256
        return audio_out + fsk_signal

    def decrypt_frame(self, audio: np.ndarray, key: int) -> np.ndarray:
        decoded_bits = self.fsk.decode_bits(audio)

        frame_num = self.frame_counter  # Fallback

        # FIX #14: Präambel-Suche mit Checksum-Validierung und ungültige-Bits-Filter
        preamble = [1, 0, 1, 0]
        for i in range(len(decoded_bits) - 20):
            candidate = decoded_bits[i:i + 4]
            if candidate == preamble:
                counter_bits = decoded_bits[i + 4:i + 12]
                if len(counter_bits) == 8 and all(b >= 0 for b in counter_bits):
                    extracted = sum(b << (7 - j) for j, b in enumerate(counter_bits))
                    # Checksum prüfen
                    packet_bits = decoded_bits[i + 12:i + 16]
                    check_bits = decoded_bits[i + 16:i + 20]
                    if (len(packet_bits) == 4 and len(check_bits) == 4
                            and all(b >= 0 for b in packet_bits)
                            and all(b >= 0 for b in check_bits)):
                        pkt = sum(b << (3 - j) for j, b in enumerate(packet_bits))
                        chk = sum(b << (3 - j) for j, b in enumerate(check_bits))
                        expected_chk = (extracted ^ pkt ^ 0x5) & 0xF
                        if chk == expected_chk:
                            frame_num = extracted
                            break
                    # Ohne gültige Checksum trotzdem nutzen (best effort)
                    frame_num = extracted
                    break

        segments = []
        for i in range(self.FRAME_SEGMENTS):
            start = i * self.segment_samples
            end = start + self.segment_samples
            if end <= len(audio):
                segments.append(audio[start:end].copy())
            else:
                seg = np.zeros(self.segment_samples, dtype=np.float32)
                if start < len(audio):
                    seg[:len(audio) - start] = audio[start:]
                segments.append(seg)

        inv_perm = self._get_permutation(key, frame_num, decrypt=True)
        unscrambled = [segments[p] for p in inv_perm]
        audio_out = self._apply_crossfade(unscrambled)

        self.frame_counter = (frame_num + 1) % 256
        return audio_out


class AudioProcessor:
    """Haupt-Audio-Prozessor."""

    def __init__(self, sample_rate: int = 44100, chunk_size: int = 1024):
        self.sample_rate = sample_rate
        self.chunk_size = chunk_size
        self.settings = AudioSettings()
        self._lock = threading.Lock()

        self.scrambler = VericryptScrambler(sample_rate)

        # FIX #6: Numpy-RingBuffer statt deque
        buf_capacity = self.scrambler.frame_samples * 4
        self.process_buffer = RingBuffer(buf_capacity)
        self.output_buffer = RingBuffer(buf_capacity)

        self._bandpass_sos = butter(
            N=4,
            Wn=[300 / (0.5 * sample_rate), 3400 / (0.5 * sample_rate)],
            btype='band',
            output='sos'
        )

        self._inversion_carrier_phase = 0.0

    def update_settings(self, **kwargs):
        with self._lock:
            for key, value in kwargs.items():
                if hasattr(self.settings, key):
                    setattr(self.settings, key, value)

    def get_settings(self) -> AudioSettings:
        with self._lock:
            return AudioSettings(
                inversion=self.settings.inversion,
                bandpass=self.settings.bandpass,
                crypto_mode=self.settings.crypto_mode,
                crypto_key=self.settings.crypto_key,
                input_device=self.settings.input_device,
                output_device=self.settings.output_device
            )

    def invert_frequencies(self, data: np.ndarray) -> np.ndarray:
        carrier_freq = 3000
        phase_inc = 2 * np.pi * carrier_freq / self.sample_rate
        phases = self._inversion_carrier_phase + phase_inc * np.arange(len(data))
        carrier = np.cos(phases)
        self._inversion_carrier_phase = phases[-1] % (2 * np.pi)
        return (data * carrier).astype(np.float32)

    def apply_bandpass(self, data: np.ndarray) -> np.ndarray:
        return sosfilt(self._bandpass_sos, data).astype(np.float32)

    def process(self, data: np.ndarray) -> np.ndarray:
        settings = self.get_settings()

        if settings.crypto_mode != CryptoMode.OFF:
            self.process_buffer.write(data)

            frame_size = self.scrambler.frame_samples
            segment_size = self.scrambler.segment_samples

            buf_pos = self.process_buffer.available() % frame_size
            self.scrambler.current_segment = buf_pos // segment_size

            while self.process_buffer.available() >= frame_size:
                frame = self.process_buffer.read(frame_size)

                if settings.crypto_mode == CryptoMode.ENCRYPT:
                    processed = self.scrambler.encrypt_frame(frame, settings.crypto_key)
                else:
                    processed = self.scrambler.decrypt_frame(frame, settings.crypto_key)

                self.output_buffer.write(processed)

            if self.output_buffer.available() >= len(data):
                data = self.output_buffer.read(len(data))
            else:
                data = data.copy() * 0.3

        if settings.inversion:
            data = self.invert_frequencies(data)

        if settings.bandpass:
            data = self.apply_bandpass(data)

        return data

    def reset_buffers(self):
        """FIX #13: Auch FSK-Phase zurücksetzen."""
        self.process_buffer.clear()
        self.output_buffer.clear()
        self.scrambler.frame_counter = 0
        self.scrambler.fsk.phase = 0.0


class LevelMeter:
    """Audio-Pegel Visualisierung."""

    def __init__(self, history_size: int = 50):
        self.history_size = history_size
        self.input_history = []
        self.output_history = []

    def update(self, input_data: np.ndarray, output_data: np.ndarray):
        input_rms = np.sqrt(np.mean(input_data ** 2))
        output_rms = np.sqrt(np.mean(output_data ** 2))

        input_db = 20 * np.log10(max(input_rms, 1e-10))
        output_db = 20 * np.log10(max(output_rms, 1e-10))

        self.input_history.append(input_db)
        self.output_history.append(output_db)

        if len(self.input_history) > self.history_size:
            self.input_history.pop(0)
            self.output_history.pop(0)

    def get_current(self) -> tuple:
        if not self.input_history:
            return -60.0, -60.0
        return self.input_history[-1], self.output_history[-1]


class AudioEffectsApp:
    """Hauptanwendung."""

    SAMPLE_RATE = 44100
    CHUNK_SIZE = 1024

    def __init__(self):
        self.processor = AudioProcessor(self.SAMPLE_RATE, self.CHUNK_SIZE)
        self.level_meter = LevelMeter()
        self.stream: Optional[sd.Stream] = None
        self.gui_queue = queue.Queue()

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
            print(f"Device query error: {e}")

    def _populate_device_combos(self):
        """Füllt die Comboboxen mit Geräten."""
        input_names = ["Default"] + [d[1] for d in self.input_devices]
        self.input_combo['values'] = input_names
        self.input_combo.current(0)

        output_names = ["Default"] + [d[1] for d in self.output_devices]
        self.output_combo['values'] = output_names
        self.output_combo.current(0)

    def _do_refresh_devices(self):
        """FIX #3: Refresh + Combobox-Update in einem Aufruf."""
        self._refresh_devices()
        self._populate_device_combos()

    def _on_device_change(self, event=None):
        """
        FIX #11: Einziger Ort für Device-Wechsel.
        Ermittelt Device-IDs, aktualisiert Settings, startet Stream neu falls aktiv.
        """
        input_sel = self.input_combo.current()
        input_id = None if input_sel == 0 else self.input_devices[input_sel - 1][0]

        output_sel = self.output_combo.current()
        output_id = None if output_sel == 0 else self.output_devices[output_sel - 1][0]

        self.processor.update_settings(
            input_device=input_id,
            output_device=output_id
        )

        if self.stream is not None:
            self.stream.stop()
            self.stream.close()
            self.stream = None
            self._start_stream()

    def _get_selected_devices(self):
        """Gibt ausgewählte Device-Indizes zurück."""
        settings = self.processor.get_settings()
        return settings.input_device, settings.output_device

    def _start_stream(self):
        """Startet den Audio-Stream mit ausgewählten Devices."""
        try:
            input_idx, output_idx = self._get_selected_devices()

            self.processor.reset_buffers()
            self.stream = sd.Stream(
                callback=self._audio_callback,
                samplerate=self.SAMPLE_RATE,
                blocksize=self.CHUNK_SIZE,
                device=(input_idx, output_idx),
                channels=1,
                dtype=np.float32
            )
            self.stream.start()
            self.button_text.set("⏹ STOP")
            self._on_settings_change()
            return True
        except Exception as e:
            self.status_var.set(f"FEHLER: {e}")
            self.stream = None
            self.button_text.set("▶ START")
            return False

    def _setup_gui(self):
        self.root = tk.Tk()
        self.root.title("Vericrypt-Style Scrambler")
        self.root.geometry("700x600")
        self.root.configure(bg='#1a1a2e')

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

        # FIX #9: Window-ID speichern statt find_withtag('all')
        self._canvas_window = self.scroll_canvas.create_window(
            (0, 0), window=self.scrollable_frame, anchor='nw'
        )
        self.scroll_canvas.configure(yscrollcommand=scrollbar.set)

        # FIX #10: Mousewheel-Handler — scoped, nicht bind_all
        def _on_mousewheel(event):
            self.scroll_canvas.yview_scroll(int(-1 * (event.delta / 120)), 'units')

        def _on_mousewheel_linux_up(event):
            self.scroll_canvas.yview_scroll(-1, 'units')

        def _on_mousewheel_linux_down(event):
            self.scroll_canvas.yview_scroll(1, 'units')

        self.scroll_canvas.bind('<MouseWheel>', _on_mousewheel)
        self.scroll_canvas.bind('<Button-4>', _on_mousewheel_linux_up)
        self.scroll_canvas.bind('<Button-5>', _on_mousewheel_linux_down)

        # Hilfsfunktion für rekursives Binding (ohne Comboboxen)
        def _bind_mousewheel(widget):
            widget.bind('<MouseWheel>', _on_mousewheel)
            widget.bind('<Button-4>', _on_mousewheel_linux_up)
            widget.bind('<Button-5>', _on_mousewheel_linux_down)

        self._bind_mousewheel_to_widget = _bind_mousewheel

        scrollbar.pack(side='right', fill='y')
        self.scroll_canvas.pack(side='left', fill='both', expand=True)

        # FIX #9: Canvas-Breite korrekt an das gespeicherte Window-Item binden
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

        # Audio Device Frame
        device_frame = ttk.LabelFrame(main_frame, text="🎤 AUDIO DEVICES", padding=10)
        device_frame.pack(fill='x', padx=15, pady=10)

        self._refresh_devices()

        # Input Device
        input_frame = ttk.Frame(device_frame)
        input_frame.pack(fill='x', pady=(0, 5))

        ttk.Label(input_frame, text="INPUT:", width=8).pack(side='left')
        self.input_device_var = tk.StringVar()
        self.input_combo = ttk.Combobox(
            input_frame,
            textvariable=self.input_device_var,
            state='readonly',
            width=45
        )
        self.input_combo.pack(side='left', fill='x', expand=True, padx=(5, 0))
        self.input_combo.bind('<<ComboboxSelected>>', self._on_device_change)

        # Output Device
        output_frame = ttk.Frame(device_frame)
        output_frame.pack(fill='x')

        ttk.Label(output_frame, text="OUTPUT:", width=8).pack(side='left')
        self.output_device_var = tk.StringVar()
        self.output_combo = ttk.Combobox(
            output_frame,
            textvariable=self.output_device_var,
            state='readonly',
            width=45
        )
        self.output_combo.pack(side='left', fill='x', expand=True, padx=(5, 0))
        self.output_combo.bind('<<ComboboxSelected>>', self._on_device_change)

        # FIX #3: Refresh Button ruft _do_refresh_devices auf (inkl. Combo-Update)
        refresh_btn = ttk.Button(device_frame, text="🔄", width=3, command=self._do_refresh_devices)
        refresh_btn.pack(side='right', pady=(5, 0))

        self._populate_device_combos()

        # Crypto-Frame
        crypto_frame = ttk.LabelFrame(main_frame, text="⚡ CRYPTO", padding=10)
        crypto_frame.pack(fill='x', padx=15, pady=10)

        self.crypto_mode_var = tk.StringVar(value="off")

        modes_frame = ttk.Frame(crypto_frame)
        modes_frame.pack(fill='x')

        # FIX #1: NUR command= verwenden, KEIN zusätzliches bind('<Button-1>')
        ttk.Radiobutton(
            modes_frame, text="CLEAR",
            variable=self.crypto_mode_var, value="off",
            command=self._on_settings_change
        ).pack(side='left', padx=(0, 20))

        ttk.Radiobutton(
            modes_frame, text="🔒 ENCRYPT",
            variable=self.crypto_mode_var, value="encrypt",
            command=self._on_settings_change
        ).pack(side='left', padx=(0, 20))

        ttk.Radiobutton(
            modes_frame, text="🔓 DECRYPT",
            variable=self.crypto_mode_var, value="decrypt",
            command=self._on_settings_change
        ).pack(side='left')

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

        ttk.Label(
            key_frame,
            text="(6-stellig: 000000-999999)",
            foreground='#666'
        ).pack(side='left')

        # Info
        info_frame = ttk.Frame(crypto_frame)
        info_frame.pack(fill='x', pady=(10, 0))

        ttk.Label(
            info_frame,
            text="▸ 30ms Zeitsegmente  ▸ FSK-Sync 100 Baud  ▸ ~500ms Latenz",
            foreground='#0a0',
            font=('Courier', 8)
        ).pack()

        # Effekte-Frame
        effects_frame = ttk.LabelFrame(main_frame, text="⚙ EFFEKTE", padding=10)
        effects_frame.pack(fill='x', padx=15, pady=10)

        self.inversion_var = tk.BooleanVar(value=False)
        self.bandpass_var = tk.BooleanVar(value=False)

        # FIX #1: NUR command= verwenden, KEIN zusätzliches bind('<Button-1>')
        ttk.Checkbutton(
            effects_frame,
            text="Frequenzinversion (Voice Inverter)",
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
        self.status_var = tk.StringVar(value="BEREIT")
        ttk.Label(
            main_frame,
            textvariable=self.status_var,
            font=('Courier', 10),
            foreground='#ff0'
        ).pack(pady=5)

        # FIX #2: Start/Stop — NUR command=, KEIN bind('<Button-1>')
        self.button_text = tk.StringVar(value="▶ START")
        self.toggle_button = ttk.Button(
            main_frame,
            textvariable=self.button_text,
            command=self._toggle_stream
        )
        self.toggle_button.pack(pady=10)

        # Padding am Ende
        ttk.Frame(main_frame, height=20).pack()

        # FIX #10: Mousewheel rekursiv auf Children binden (nach Aufbau)
        self.root.after(100, self._bind_scroll_to_children)

        self.root.focus_set()
        self._update_gui()

    def _bind_scroll_to_children(self):
        """FIX #10: Bindet Mousewheel rekursiv, Comboboxen ausgenommen."""
        def _recurse(widget):
            if not isinstance(widget, ttk.Combobox):
                self._bind_mousewheel_to_widget(widget)
                for child in widget.winfo_children():
                    _recurse(child)

        _recurse(self.scrollable_frame)

    def _on_settings_change(self):
        mode_str = self.crypto_mode_var.get()
        crypto_mode = {
            "off": CryptoMode.OFF,
            "encrypt": CryptoMode.ENCRYPT,
            "decrypt": CryptoMode.DECRYPT
        }.get(mode_str, CryptoMode.OFF)

        try:
            key = int(self.crypto_key_var.get())
            key = max(0, min(999999, key))
        except ValueError:
            key = 123456

        old_mode = self.processor.get_settings().crypto_mode
        if old_mode != crypto_mode:
            self.processor.reset_buffers()

        self.processor.update_settings(
            inversion=self.inversion_var.get(),
            bandpass=self.bandpass_var.get(),
            crypto_mode=crypto_mode,
            crypto_key=key
        )

        if crypto_mode == CryptoMode.ENCRYPT:
            self.status_var.set("🔒 ENCRYPT - Aufnahme auf Tape")
        elif crypto_mode == CryptoMode.DECRYPT:
            self.status_var.set("🔓 DECRYPT - Wiedergabe von Tape")
        elif self.stream:
            self.status_var.set("▶ CLEAR - Durchgang")
        else:
            self.status_var.set("BEREIT")

    def _audio_callback(self, indata, outdata, frames, time, status):
        if status:
            self.gui_queue.put(('status', str(status)))

        input_data = indata.flatten()
        output_data = self.processor.process(input_data.copy())

        self.gui_queue.put(('level', (input_data, output_data)))
        outdata[:] = output_data.reshape(-1, 1)

    def _toggle_stream(self):
        if self.stream is None:
            self._start_stream()
        else:
            self.stream.stop()
            self.stream.close()
            self.stream = None
            self.button_text.set("▶ START")
            self.status_var.set("GESTOPPT")

    def _update_gui(self):
        while True:
            try:
                msg_type, data = self.gui_queue.get_nowait()
                if msg_type == 'level':
                    self.level_meter.update(*data)
                elif msg_type == 'status':
                    self.status_var.set(data)
            except queue.Empty:
                break

        self._draw_meters()
        self._draw_ring()
        self.root.after(30, self._update_gui)

    def _draw_meters(self):
        canvas = self.meter_canvas
        canvas.delete('all')

        width = canvas.winfo_width()
        height = canvas.winfo_height()
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

        num_segments = 16
        current_seg = self.processor.scrambler.current_segment
        perm = self.processor.scrambler.current_permutation
        crypto_mode = self.processor.get_settings().crypto_mode

        # FIX #8: math ist jetzt top-level importiert (kein import math hier)

        for i in range(num_segments):
            angle_start = (i / num_segments) * 2 * math.pi - math.pi / 2
            angle_end = ((i + 1) / num_segments) * 2 * math.pi - math.pi / 2

            points = []
            for a in [angle_start, (angle_start + angle_end) / 2, angle_end]:
                points.extend([cx + radius_outer * math.cos(a), cy + radius_outer * math.sin(a)])
            for a in [angle_end, (angle_start + angle_end) / 2, angle_start]:
                points.extend([cx + radius_inner * math.cos(a), cy + radius_inner * math.sin(a)])

            if crypto_mode == CryptoMode.OFF:
                fill = '#1a1a2a'
                outline = '#333'
            elif i == current_seg:
                fill = '#0f0'
                outline = '#0f0'
            else:
                # FIX #12: Unbenutzte hue-Variable entfernt
                perm_idx = perm[i] if i < len(perm) else i
                colors = ['#2a1a1a', '#1a2a1a', '#1a1a2a', '#2a2a1a',
                          '#2a1a2a', '#1a2a2a', '#2a2a2a', '#3a1a1a',
                          '#1a3a1a', '#1a1a3a', '#3a3a1a', '#3a1a3a',
                          '#1a3a3a', '#3a3a3a', '#4a1a1a', '#1a4a1a']
                fill = colors[perm_idx % len(colors)]
                outline = '#444'

            canvas.create_polygon(points, fill=fill, outline=outline)

            mid_angle = (angle_start + angle_end) / 2
            text_r = (radius_outer + radius_inner) / 2
            tx = cx + text_r * math.cos(mid_angle)
            ty = cy + text_r * math.sin(mid_angle)

            text_color = '#000' if i == current_seg else '#666'
            canvas.create_text(tx, ty, text=str(i), fill=text_color, font=('Courier', 8))

        frame_num = self.processor.scrambler.frame_counter
        canvas.create_oval(cx - radius_inner + 10, cy - radius_inner + 10,
                           cx + radius_inner - 10, cy + radius_inner - 10,
                           fill='#0a0a1a', outline='#333')
        canvas.create_text(cx, cy - 10, text="FRAME", fill='#666', font=('Courier', 8))
        canvas.create_text(cx, cy + 10, text=f"{frame_num:03d}", fill='#0f0',
                           font=('Courier', 14, 'bold'))

    def run(self):
        self.root.mainloop()
        if self.stream:
            self.stream.stop()
            self.stream.close()


if __name__ == '__main__':
    app = AudioEffectsApp()
    app.run()
