"""Personenmaske, Gesichtsmerkmale und Tiefe über macOS Vision und Depth Anything (Flüssigmetall II)."""

import sys
from concurrent.futures import ThreadPoolExecutor

import cv2
import numpy as np

from bildeffekte.depth2 import estimate_depth

_ANALYSIS = ThreadPoolExecutor(max_workers=1, thread_name_prefix='portrait2-analysis')
# Eigener Thread für die laufende Personenmaske, damit sie nie die Analyse des
# eingefrorenen Bildes verzögert.
_LIVE = ThreadPoolExecutor(max_workers=1, thread_name_prefix='portrait2-live')


def submit_portrait(frame):
    """Nur das eingefrorene Bild analysieren; keine Arbeit im GUI-/Audio-Thread."""
    return _ANALYSIS.submit(analyse_portrait, frame.copy())


def submit_person_mask(frame, then=None):
    """Schnelle Personenmaske eines Livebilds; `then(frame, mask)` läuft im selben Thread."""
    frame = frame.copy()
    if then is None:
        return _LIVE.submit(person_mask, frame)
    return _LIVE.submit(lambda: then(frame, person_mask(frame)))


def person_mask(frame):
    """Maske 0–255 (auch leer, wenn niemand im Bild ist) oder None ohne Vision."""
    if sys.platform != 'darwin':
        return None
    try:
        import objc
        import Vision
        import Quartz
        import Foundation
    except ImportError:
        return None
    try:
        with objc.autorelease_pool():
            encoded = cv2.imencode('.png', cv2.cvtColor(frame, cv2.COLOR_RGB2BGR))[1].tobytes()
            data = Foundation.NSData.dataWithBytes_length_(encoded, len(encoded))
            handler = Vision.VNImageRequestHandler.alloc().initWithData_options_(data, {})
            person = Vision.VNGeneratePersonSegmentationRequest.alloc().initWithCompletionHandler_(None)
            person.setQualityLevel_(Vision.VNGeneratePersonSegmentationRequestQualityLevelBalanced)
            success, _ = handler.performRequests_error_([person], None)
            if not success or not person.results():
                return np.zeros(frame.shape[:2], np.uint8)
            mask = _read_mask(person.results()[0].pixelBuffer())
            return cv2.resize(mask, frame.shape[1::-1], interpolation=cv2.INTER_LINEAR)
    except Exception:
        return None


def _read_mask(buffer):
    import Quartz
    Quartz.CVPixelBufferLockBaseAddress(buffer, Quartz.kCVPixelBufferLock_ReadOnly)
    try:
        w = Quartz.CVPixelBufferGetWidth(buffer)
        h = Quartz.CVPixelBufferGetHeight(buffer)
        stride = Quartz.CVPixelBufferGetBytesPerRow(buffer)
        data = Quartz.CVPixelBufferGetBaseAddress(buffer).as_buffer(stride * h)
        return np.frombuffer(data, np.uint8).reshape(h, stride)[:, :w].copy()
    finally:
        Quartz.CVPixelBufferUnlockBaseAddress(buffer, Quartz.kCVPixelBufferLock_ReadOnly)


def analyse_portrait(frame):
    if sys.platform != 'darwin':
        return None
    try:
        import objc
        import Vision
        import Quartz
        import Foundation
    except ImportError:
        return None
    try:
        with objc.autorelease_pool():
            encoded = cv2.imencode('.png', cv2.cvtColor(frame, cv2.COLOR_RGB2BGR))[1].tobytes()
            data = Foundation.NSData.dataWithBytes_length_(encoded, len(encoded))
            handler = Vision.VNImageRequestHandler.alloc().initWithData_options_(data, {})
            person = Vision.VNGeneratePersonSegmentationRequest.alloc().initWithCompletionHandler_(None)
            person.setQualityLevel_(Vision.VNGeneratePersonSegmentationRequestQualityLevelAccurate)
            faces = Vision.VNDetectFaceLandmarksRequest.alloc().initWithCompletionHandler_(None)
            success, error = handler.performRequests_error_([person, faces], None)
            if not success:
                return None
            height, width = frame.shape[:2]
            if not person.results():
                return {'mask': None, 'person': False}
            mask = _read_mask(person.results()[0].pixelBuffer())
            mask = cv2.resize(mask, (width, height), interpolation=cv2.INTER_LINEAR)
            if np.count_nonzero(mask > 127) < width * height * 0.015:
                # Vision lief, aber niemand ist im Bild.
                return {'mask': None, 'person': False}
            result = {'mask': mask, 'face': None, 'features': {}, 'depth': estimate_depth(frame)}
            if faces.results():
                face = max(faces.results(), key=lambda f: f.boundingBox().size.width * f.boundingBox().size.height)
                rect = face.boundingBox()
                x, y, fw, fh = rect.origin.x, rect.origin.y, rect.size.width, rect.size.height
                result['face'] = (x * width, (1 - y - fh) * height, fw * width, fh * height)
                landmarks = face.landmarks()
                for name in ('nose', 'leftEye', 'rightEye', 'outerLips'):
                    region = getattr(landmarks, name)()
                    if region is not None and region.pointCount():
                        points = region.normalizedPoints()
                        coords = [(width * (x + points[i].x * fw),
                                   height * (1 - y - points[i].y * fh)) for i in range(region.pointCount())]
                        result['features'][name] = np.mean(coords, axis=0).astype(np.float32)
            return result
    except Exception:
        # Ältere macOS-Versionen oder nicht verfügbare Vision-Modelle: Kontur-Fallback.
        return None
