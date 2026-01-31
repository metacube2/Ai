"""
Phase-Locked Timestretcher
==========================

High-quality offline time-stretching using a phase-locked phase vocoder.
This approach keeps the original spectral texture by propagating peak phases
and locking surrounding bins to preserve vertical phase coherence.

Usage:
  python phase_locked_vocoder.py input.wav output.wav 10.0
"""

from __future__ import annotations

import argparse
from dataclasses import dataclass
from typing import Tuple

import numpy as np
from scipy import signal

try:
    import soundfile as sf
except ImportError:  # pragma: no cover - optional dependency
    sf = None


@dataclass
class StretchConfig:
    stretch_factor: float = 10.0
    window_size: int = 4096
    hop_size: int = 1024
    peak_threshold_db: float = -60.0
    peak_min_distance: int = 3


def stft(audio: np.ndarray, window_size: int, hop_size: int) -> np.ndarray:
    window = signal.windows.hann(window_size, sym=False)
    n_frames = 1 + (len(audio) - window_size) // hop_size
    frames = np.lib.stride_tricks.as_strided(
        audio,
        shape=(n_frames, window_size),
        strides=(audio.strides[0] * hop_size, audio.strides[0]),
        writeable=False,
    )
    windowed = frames * window[None, :]
    return np.fft.rfft(windowed, axis=1).T


def istft(stft_matrix: np.ndarray, window_size: int, hop_size: int, length: int) -> np.ndarray:
    window = signal.windows.hann(window_size, sym=False)
    n_frames = stft_matrix.shape[1]
    output = np.zeros(hop_size * (n_frames - 1) + window_size)
    window_sums = np.zeros_like(output)

    for i in range(n_frames):
        frame = np.fft.irfft(stft_matrix[:, i], n=window_size)
        start = i * hop_size
        output[start:start + window_size] += frame * window
        window_sums[start:start + window_size] += window**2

    nonzero = window_sums > 1e-8
    output[nonzero] /= window_sums[nonzero]
    return output[:length]


def detect_peaks(magnitude: np.ndarray, threshold_db: float, min_distance: int) -> np.ndarray:
    mag_db = 20 * np.log10(magnitude + 1e-12)
    candidates = np.where(
        (mag_db[1:-1] > threshold_db)
        & (mag_db[1:-1] > mag_db[:-2])
        & (mag_db[1:-1] > mag_db[2:])
    )[0] + 1

    if candidates.size == 0:
        return np.array([], dtype=int)

    # Enforce minimum distance between peaks
    peaks = [candidates[0]]
    for idx in candidates[1:]:
        if idx - peaks[-1] >= min_distance:
            peaks.append(idx)
    return np.array(peaks, dtype=int)


def phase_locked_vocoder(
    stft_matrix: np.ndarray,
    hop_size: int,
    stretch_factor: float,
    peak_threshold_db: float,
    peak_min_distance: int,
) -> np.ndarray:
    n_bins, n_frames = stft_matrix.shape
    if n_frames < 2:
        return stft_matrix

    time_steps = np.arange(0, n_frames - 1, 1 / stretch_factor)
    output = np.zeros((n_bins, len(time_steps)), dtype=np.complex128)

    phase_acc = np.angle(stft_matrix[:, 0])
    expected_phase = 2 * np.pi * hop_size * np.arange(n_bins) / (2 * (n_bins - 1))

    for t, step in enumerate(time_steps):
        idx = int(np.floor(step))
        frac = step - idx
        if idx + 1 >= n_frames:
            break

        mag1 = np.abs(stft_matrix[:, idx])
        mag2 = np.abs(stft_matrix[:, idx + 1])
        mag = (1 - frac) * mag1 + frac * mag2

        phase1 = np.angle(stft_matrix[:, idx])
        phase2 = np.angle(stft_matrix[:, idx + 1])

        phase_diff = phase2 - phase1 - expected_phase
        phase_diff = (phase_diff + np.pi) % (2 * np.pi) - np.pi
        true_freq = expected_phase + phase_diff
        phase_acc += true_freq

        peaks = detect_peaks(mag, threshold_db=peak_threshold_db, min_distance=peak_min_distance)
        if peaks.size == 0:
            output[:, t] = mag * np.exp(1j * phase_acc)
            continue

        output_phase = phase_acc.copy()
        peak_phases = phase_acc[peaks]
        analysis_phases = phase1

        # Determine regions between peaks
        boundaries = [0]
        boundaries += [int((peaks[i] + peaks[i + 1]) / 2) for i in range(len(peaks) - 1)]
        boundaries.append(n_bins - 1)

        for i, peak in enumerate(peaks):
            start = boundaries[i]
            end = boundaries[i + 1]
            if end <= start:
                continue
            relative_phase = analysis_phases[start:end + 1] - analysis_phases[peak]
            output_phase[start:end + 1] = peak_phases[i] + relative_phase

        output[:, t] = mag * np.exp(1j * output_phase)

    return output


def stretch_audio(audio: np.ndarray, sample_rate: int, config: StretchConfig) -> np.ndarray:
    if audio.ndim > 1:
        audio = np.mean(audio, axis=1)

    audio = audio.astype(np.float64)
    audio /= np.max(np.abs(audio)) + 1e-12

    if len(audio) < config.window_size:
        raise ValueError("Audio is shorter than the analysis window.")

    padded = np.pad(audio, (config.window_size // 2, config.window_size // 2), mode="reflect")
    stft_matrix = stft(padded, config.window_size, config.hop_size)

    stretched_stft = phase_locked_vocoder(
        stft_matrix,
        hop_size=config.hop_size,
        stretch_factor=config.stretch_factor,
        peak_threshold_db=config.peak_threshold_db,
        peak_min_distance=config.peak_min_distance,
    )

    output_length = int(len(audio) * config.stretch_factor)
    output = istft(stretched_stft, config.window_size, config.hop_size, output_length + config.window_size)

    output = output[config.window_size // 2:config.window_size // 2 + output_length]
    peak = np.max(np.abs(output))
    if peak > 0:
        output = 0.95 * output / peak
    return output


def stretch_file(input_path: str, output_path: str, config: StretchConfig) -> None:
    if sf is None:
        raise RuntimeError("soundfile is required for file IO. Install with `pip install soundfile`.")

    audio, sr = sf.read(input_path)
    result = stretch_audio(audio, sr, config)
    sf.write(output_path, result, sr)


def parse_args() -> Tuple[str, str, StretchConfig]:
    parser = argparse.ArgumentParser(description="Phase-locked time-stretching")
    parser.add_argument("input", help="Input WAV file")
    parser.add_argument("output", help="Output WAV file")
    parser.add_argument("stretch", type=float, help="Stretch factor (e.g., 10.0)")
    parser.add_argument("--window", type=int, default=4096)
    parser.add_argument("--hop", type=int, default=1024)
    parser.add_argument("--peak-db", type=float, default=-60.0)
    parser.add_argument("--peak-distance", type=int, default=3)
    args = parser.parse_args()

    config = StretchConfig(
        stretch_factor=args.stretch,
        window_size=args.window,
        hop_size=args.hop,
        peak_threshold_db=args.peak_db,
        peak_min_distance=args.peak_distance,
    )
    return args.input, args.output, config


def main() -> None:
    input_path, output_path, config = parse_args()
    stretch_file(input_path, output_path, config)


if __name__ == "__main__":
    main()
