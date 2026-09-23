"""Optionale lokale Personenmaske und Gesichtsmerkmale über macOS Vision."""

import sys
from concurrent.futures import ThreadPoolExecutor

import cv2
import numpy as np

_ANALYSIS = ThreadPoolExecutor(max_workers=1, thread_name_prefix='portrait-analysis')


def submit_portrait(frame):
    """Nur das eingefrorene Bild analysieren; keine Arbeit im GUI-/Audio-Thread."""
    return _ANALYSIS.submit(analyse_portrait, frame.copy())


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
            if not success or not person.results():
                return None
            buffer = person.results()[0].pixelBuffer()
            Quartz.CVPixelBufferLockBaseAddress(buffer, Quartz.kCVPixelBufferLock_ReadOnly)
            try:
                w = Quartz.CVPixelBufferGetWidth(buffer)
                h = Quartz.CVPixelBufferGetHeight(buffer)
                stride = Quartz.CVPixelBufferGetBytesPerRow(buffer)
                data = Quartz.CVPixelBufferGetBaseAddress(buffer).as_buffer(stride * h)
                mask = np.frombuffer(data, np.uint8).reshape(h, stride)[:, :w].copy()
            finally:
                Quartz.CVPixelBufferUnlockBaseAddress(buffer, Quartz.kCVPixelBufferLock_ReadOnly)
            height, width = frame.shape[:2]
            mask = cv2.resize(mask, (width, height), interpolation=cv2.INTER_LINEAR)
            if np.count_nonzero(mask > 127) < width * height * 0.015:
                return None
            result = {'mask': mask, 'face': None, 'features': {}}
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
