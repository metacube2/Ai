from __future__ import annotations

import argparse
from pathlib import Path

import sounddevice as sd
import soundfile as sf

ROOT = Path(__file__).resolve().parents[1]
INPUT = ROOT / "input"


def main() -> int:
    parser = argparse.ArgumentParser(description="Record microphone audio to WAV.")
    parser.add_argument("--seconds", "-s", type=int, required=True, help="Recording duration in seconds")
    parser.add_argument("--output", "-o", default="recording.wav", help="Output WAV filename/path")
    parser.add_argument("--device", "-d", help="Optional input device id or name")
    parser.add_argument("--list-devices", action="store_true", help="Print audio devices and exit")
    parser.add_argument("--samplerate", type=int, default=16000)
    parser.add_argument("--channels", type=int, default=1)
    args = parser.parse_args()

    if args.list_devices:
        print(sd.query_devices())
        return 0

    out = Path(args.output)
    if not out.is_absolute():
        out = INPUT / out
    out.parent.mkdir(parents=True, exist_ok=True)

    device = int(args.device) if args.device and args.device.isdigit() else args.device
    print(f"Recording {args.seconds}s to {out}")
    if device is not None:
        print(f"Device: {device}")
    audio = sd.rec(
        int(args.seconds * args.samplerate),
        samplerate=args.samplerate,
        channels=args.channels,
        dtype="float32",
        device=device,
    )
    sd.wait()
    sf.write(out, audio, args.samplerate)
    print("Done.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
