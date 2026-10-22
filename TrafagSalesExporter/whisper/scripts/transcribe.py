from __future__ import annotations

import argparse
import json
import os
from datetime import datetime, timedelta
from pathlib import Path

import av
from faster_whisper import WhisperModel

# Exit codes shared with the GUI so it can distinguish outcomes in a batch.
EXIT_OK = 0
EXIT_ERROR = 1
EXIT_SKIPPED = 3  # file has no usable audio stream (e.g. video without audio)

ROOT = Path(__file__).resolve().parents[1]
CACHE = ROOT / "cache"
OUTPUT = ROOT / "output"

os.environ.setdefault("HF_HOME", str(CACHE / "hf"))
os.environ.setdefault("HF_HUB_CACHE", str(CACHE / "hf" / "hub"))
os.environ.setdefault("XDG_CACHE_HOME", str(CACHE))
os.environ.setdefault("HF_HUB_DISABLE_SYMLINKS_WARNING", "1")
os.environ.setdefault("TEMP", str(ROOT / "temp"))
os.environ.setdefault("TMP", str(ROOT / "temp"))

PROMPT = (
    "Transcribe Swiss German as clear, readable Standard German. "
    "Keep High German as German. Keep English sentences in English. "
    "Use normal punctuation and paragraphs for meeting notes."
)


def has_audio_stream(path: Path) -> bool:
    """Return True if the media file contains at least one audio stream.

    Video files without an audio track (e.g. silent screen recordings) otherwise
    crash deep inside faster-whisper/PyAV with an IndexError. If probing itself
    fails we return True and let the normal transcription path report the error.
    """
    try:
        with av.open(str(path)) as container:
            return any(stream.type == "audio" for stream in container.streams)
    except Exception:
        return True


def report_progress(done_s: float, total_s: float) -> None:
    """Machine-readable progress line for the GUI: "PROGRESS <percent> <done_s> <total_s>".

    flush=True matters: stdout is a pipe when the GUI runs us, so without it the
    lines would only arrive in one block at the very end.
    """
    pct = 100.0 if total_s <= 0 else min(100.0, max(0.0, done_s / total_s * 100.0))
    print(f"PROGRESS {pct:.1f} {done_s:.0f} {total_s:.0f}", flush=True)


def format_ts(seconds: float) -> str:
    total = int(seconds)
    hours = total // 3600
    minutes = (total % 3600) // 60
    secs = total % 60
    return f"{hours:02d}:{minutes:02d}:{secs:02d}"


def build_output_path(input_path: Path, output_arg: str | None) -> Path:
    OUTPUT.mkdir(parents=True, exist_ok=True)
    if output_arg:
        out = Path(output_arg)
        if not out.is_absolute():
            out = ROOT / out
        return out
    return OUTPUT / f"{input_path.stem}.txt"


def stamped_variant(path: Path, attempt: int = 1) -> Path:
    """Sibling of *path* carrying the source name plus a timestamp.

    Used instead of overwriting an existing transcript, and as the escape hatch
    when the intended target cannot be written (locked, read-only, synced away).
    """
    stamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    suffix = "" if attempt <= 1 else f"_{attempt}"
    return path.with_name(f"{path.stem}_{stamp}{suffix}{path.suffix}")


def _taken(path: Path) -> bool:
    # The .json sidecar shares the stem, so a free .txt next to a foreign .json
    # would still mix two transcripts.
    return path.exists() or path.with_suffix(".json").exists()


def free_output_path(path: Path) -> Path:
    """Never overwrite: fall back to a timestamped name if *path* is taken."""
    if not _taken(path):
        return path
    attempt = 1
    while True:
        candidate = stamped_variant(path, attempt)
        if not _taken(candidate):
            return candidate
        attempt += 1


def write_transcript(out_path: Path, text: str) -> Path:
    """Write *text* and return the path it actually landed on.

    The transcription is the expensive part, so a failing write must never throw
    the result away. If the intended target refuses the write, retry on a
    timestamped sibling before giving up.
    """
    try:
        with out_path.open("w", encoding="utf-8", newline="\n") as f:
            f.write(text)
        return out_path
    except OSError as exc:
        print(f"Ausgabedatei nicht beschreibbar ({type(exc).__name__}: {exc}); weiche auf neuen Namen aus.")

    for attempt in range(1, 4):
        fallback = stamped_variant(out_path, attempt)
        if fallback.exists():
            continue
        try:
            with fallback.open("w", encoding="utf-8", newline="\n") as f:
                f.write(text)
            return fallback
        except OSError as exc:
            print(f"Auch {fallback.name} nicht beschreibbar ({type(exc).__name__}: {exc}).")

    raise OSError(f"Kein beschreibbarer Ausgabepfad neben {out_path}")


def main() -> int:
    parser = argparse.ArgumentParser(description="Transcribe audio/video files with local faster-whisper.")
    parser.add_argument("input", help="Audio/video file, e.g. .wav, .mp3, .m4a, .mp4")
    parser.add_argument("--output", "-o", help="Output txt path. Defaults to whisper/output/<input>.txt")
    parser.add_argument("--model", default="medium", help="Model: small, medium, large-v3. Default: medium")
    parser.add_argument("--language", default="auto", help="auto, de, en. Use de for Swiss German meetings. Default: auto")
    parser.add_argument("--timestamps", action="store_true", help="Include segment timestamps")
    parser.add_argument("--task", default="transcribe", choices=["transcribe", "translate"],
                        help="translate = Whisper translates directly into English")
    parser.add_argument("--no-json", action="store_true", help="Do not write the .json segment sidecar")
    parser.add_argument("--beam-size", type=int, default=5)
    args = parser.parse_args()

    input_path = Path(args.input)
    if not input_path.is_absolute():
        candidate = Path.cwd() / input_path
        input_path = candidate if candidate.exists() else ROOT / "input" / input_path
    input_path = input_path.resolve()
    if not input_path.exists():
        raise FileNotFoundError(f"Input file not found: {input_path}")

    print(f"Input:  {input_path}")

    if not has_audio_stream(input_path):
        print(f"Übersprungen: keine Audiospur in {input_path.name}")
        return EXIT_SKIPPED

    out_path = build_output_path(input_path, args.output)
    out_path.parent.mkdir(parents=True, exist_ok=True)

    requested_path = out_path
    out_path = free_output_path(out_path)
    if out_path != requested_path:
        print(f"Vorhanden: {requested_path.name} bleibt erhalten.")

    language = None if args.language.lower() == "auto" else args.language.lower()

    print(f"Output: {out_path}")
    print(f"Model:  {args.model} on CPU int8")
    print(f"Lang:   {args.language}")
    print(f"Task:   {args.task}", flush=True)

    # Build the whole transcript in memory first, then write once. That way a
    # decode error mid-file does not leave a truncated/0-byte output behind.
    try:
        print("Lade Modell...", flush=True)
        model = WhisperModel(args.model, device="cpu", compute_type="int8", download_root=str(CACHE / "models"))
        segments, info = model.transcribe(
            str(input_path),
            language=language,
            task=args.task,
            beam_size=args.beam_size,
            vad_filter=True,
            # The prompt is written for transcription; when translating, Whisper
            # tends to echo it back as "translated" text.
            initial_prompt=PROMPT if args.task == "transcribe" else None,
        )

        # transcribe() returns a lazy generator; segment.end is on the original
        # timeline (VAD gaps restored), so end / duration is the share done.
        print(f"Dauer:  {format_ts(info.duration)}", flush=True)
        report_progress(0.0, info.duration)

        segment_rows = []
        lines = [
            f"Source: {input_path}",
            f"Model: {args.model}",
            f"Detected language: {info.language} ({info.language_probability:.2f})",
            "",
        ]
        for segment in segments:
            report_progress(segment.end, info.duration)
            text = segment.text.strip()
            if not text:
                continue
            segment_rows.append({"start": round(segment.start, 2), "end": round(segment.end, 2), "text": text})
            if args.timestamps:
                lines.append(f"[{format_ts(segment.start)} - {format_ts(segment.end)}] {text}")
            else:
                lines.append(text)
        report_progress(info.duration, info.duration)
    except Exception as exc:
        print(f"Fehler bei der Transkription: {type(exc).__name__}: {exc}")
        return EXIT_ERROR

    try:
        written_path = write_transcript(out_path, "\n".join(lines) + "\n")
    except OSError as exc:
        print(f"Fehler beim Schreiben der Ausgabe: {exc}")
        return EXIT_ERROR

    if not args.no_json:
        # Sidecar for the studio: exact segment times for SRT/VTT/DOCX export.
        # Follows the name the .txt really got, so both stay a pair.
        meta = {
            "source": str(input_path),
            "model": args.model,
            "task": args.task,
            "language": info.language,
            "language_probability": round(info.language_probability, 3),
            "duration": round(info.duration, 2),
            "created": datetime.now().isoformat(timespec="seconds"),
            "segments": segment_rows,
        }
        json_path = written_path.with_suffix(".json")
        try:
            json_path.write_text(json.dumps(meta, ensure_ascii=False, indent=1), encoding="utf-8")
            print(f"Sidecar: {json_path}")
        except OSError as exc:
            print(f"Segmentdatei nicht geschrieben ({exc}); Text ist trotzdem gespeichert.")

    print(f"Written: {written_path}")
    print("Done.")
    return EXIT_OK


if __name__ == "__main__":
    raise SystemExit(main())
