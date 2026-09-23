"""Lokale Tiefenschätzung mit Depth Anything V2 Small (ONNX, Apache-2.0).

Modell einmalig herunterladen (≈50 MB):
    mkdir -p ~/.bildeffekte/models && curl -L -o \\
      ~/.bildeffekte/models/depth_anything_v2_small_fp16.onnx \\
      https://huggingface.co/onnx-community/depth-anything-v2-small/resolve/main/onnx/model_fp16.onnx
Ohne Modell oder ohne onnxruntime liefert `estimate_depth` None.
"""

import os
import threading

import cv2
import numpy as np

MODEL_PATH = os.environ.get(
    'BILDEFFEKTE_DEPTH_MODEL',
    os.path.expanduser('~/.bildeffekte/models/depth_anything_v2_small_fp16.onnx'))
INPUT_SIZE = 518          # Vielfaches von 14, Trainingsauflösung des Modells
_MEAN = np.float32((0.485, 0.456, 0.406))
_STD = np.float32((0.229, 0.224, 0.225))

_session = None
_lock = threading.Lock()


def available():
    try:
        import onnxruntime  # noqa: F401
    except ImportError:
        return False
    return os.path.isfile(MODEL_PATH)


def _get_session():
    global _session
    with _lock:
        if _session is None:
            import onnxruntime as ort
            options = ort.SessionOptions()
            options.log_severity_level = 3
            # Nach der einmaligen Schätzung nicht aktiv weiterwarten: sonst belegen
            # die Worker-Threads Kerne, die Rendern und Simulation brauchen.
            options.add_session_config_entry('session.intra_op.allow_spinning', '0')
            options.add_session_config_entry('session.inter_op.allow_spinning', '0')
            _session = ort.InferenceSession(MODEL_PATH, options, providers=['CPUExecutionProvider'])
        return _session


def estimate_depth(frame):
    """Relative inverse Tiefe (groß = nah) in Bildgröße, oder None."""
    if not available():
        return None
    try:
        height, width = frame.shape[:2]
        x = cv2.resize(frame, (INPUT_SIZE, INPUT_SIZE), interpolation=cv2.INTER_CUBIC)
        x = ((x.astype(np.float32) / 255 - _MEAN) / _STD).transpose(2, 0, 1)[None]
        session = _get_session()
        out = session.run(None, {session.get_inputs()[0].name: np.ascontiguousarray(x)})[0][0]
        return cv2.resize(out.astype(np.float32), (width, height), interpolation=cv2.INTER_CUBIC)
    except Exception:
        return None
