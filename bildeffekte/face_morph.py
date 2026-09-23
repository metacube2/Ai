"""Lokale Gesichtspunkte und Dreiecks-Warp für den Matrix-Agenten."""
import sys
from concurrent.futures import ThreadPoolExecutor
import cv2
import numpy as np

_WORKER = ThreadPoolExecutor(max_workers=1, thread_name_prefix='matrix-face')


def submit_face(frame):
    return _WORKER.submit(detect_face, frame.copy())


def face_points(rect, features):
    x, y, w, h = rect
    eyes = sorted([features.get('leftEye', (x + .32*w, y + .38*h)),
                   features.get('rightEye', (x + .68*w, y + .38*h))], key=lambda p: p[0])
    left, right = np.float32(eyes)
    axis = right - left
    axis /= max(float(np.linalg.norm(axis)), 1e-5)
    down = np.array([-axis[1], axis[0]], np.float32)
    center = (left + right) / 2 + down * h * .12
    angles = np.linspace(0, 2*np.pi, 20, endpoint=False)
    rim = center + np.cos(angles)[:, None] * axis * w*.53 + np.sin(angles)[:, None] * down * h*.61
    nose = features.get('nose', center + down*h*.10)
    mouth = np.float32(features.get('outerLips', center + down*h*.30))
    return np.float32([*rim, left, right, nose,
                       features.get('mouthLeft', mouth-axis*w*.17), mouth,
                       features.get('mouthRight', mouth+axis*w*.17),
                       features.get('mouthTop', mouth-down*h*.025),
                       features.get('mouthBottom', mouth+down*h*.025)])


def lip_points(coords):
    """
    Mundwinkel und Lippenmitten aus der äußeren Lippenkontur.
    Ober- und Unterlippe als Mittel um die Mundmitte: Der höchste Einzelpunkt
    springt zwischen den beiden Spitzen des Amorbogens hin und her (bis 10 px).
    """
    left, right = coords[coords[:, 0].argmin()], coords[coords[:, 0].argmax()]
    axis = right - left
    span = max(float(np.linalg.norm(axis)), 1e-5)
    axis = axis / span
    down = np.float32([-axis[1], axis[0]])
    center = (left + right) / 2
    u, v = (coords - center) @ axis, (coords - center) @ down
    central = np.abs(u) < span * .25
    result = {'mouthLeft': left, 'mouthRight': right}
    for key, side in (('mouthTop', v < 0), ('mouthBottom', v > 0)):
        chosen = central & side
        if chosen.any():
            result[key] = center + down * float(v[chosen].mean())
    return result


def detect_face(frame):
    """Größtes frontales Gesicht; keine Personensegmentierung oder Tiefenschätzung."""
    if sys.platform == 'darwin':
        try:
            import objc, Vision, Foundation
            with objc.autorelease_pool():
                encoded = cv2.imencode('.png', cv2.cvtColor(frame, cv2.COLOR_RGB2BGR))[1].tobytes()
                data = Foundation.NSData.dataWithBytes_length_(encoded, len(encoded))
                handler = Vision.VNImageRequestHandler.alloc().initWithData_options_(data, {})
                request = Vision.VNDetectFaceLandmarksRequest.alloc().initWithCompletionHandler_(None)
                success, _ = handler.performRequests_error_([request], None)
                if success:
                    if not request.results():
                        return None
                    face = max(request.results(), key=lambda f: f.boundingBox().size.width*f.boundingBox().size.height)
                    if face.yaw() is not None and abs(float(face.yaw())) > .65:
                        return None
                    r = face.boundingBox(); w, h = frame.shape[1], frame.shape[0]
                    x, y, fw, fh = r.origin.x, r.origin.y, r.size.width, r.size.height
                    features = {}
                    for name in ('leftEye', 'rightEye', 'nose', 'outerLips'):
                        region = getattr(face.landmarks(), name)()
                        if region is not None and region.pointCount():
                            points = region.normalizedPoints()
                            coords = np.float32([(w*(x+points[i].x*fw), h*(1-y-points[i].y*fh))
                                                 for i in range(region.pointCount())])
                            features[name] = coords.mean(0)
                            if name == 'outerLips':
                                features.update(lip_points(coords))
                    return face_points((x*w, (1-y-fh)*h, fw*w, fh*h), features)
        except (ImportError, AttributeError, RuntimeError):
            pass
    cascade = cv2.CascadeClassifier(cv2.data.haarcascades+'haarcascade_frontalface_default.xml')
    found = cascade.detectMultiScale(cv2.cvtColor(frame, cv2.COLOR_RGB2GRAY), 1.15, 5, minSize=(40, 40))
    if len(found):
        return face_points(max(found, key=lambda r: r[2]*r[3]), {})
    return None


def warp_mesh(image, source, destination, triangles, shape):
    """Stückweise affine Abbildung; auch bei teilweise angeschnittenen Gesichtern."""
    h, w = shape
    mx = np.full((h, w), -1, np.float32)
    my = np.full((h, w), -1, np.float32)
    for triangle in triangles:
        src, dst = source[triangle], destination[triangle]
        if abs(float(np.linalg.det(np.stack([dst[1]-dst[0], dst[2]-dst[0]])))) < .1:
            continue
        x, y, bw, bh = cv2.boundingRect(dst)
        x0, y0, x1, y1 = max(0,x), max(0,y), min(w,x+bw), min(h,y+bh)
        if x0 >= x1 or y0 >= y1:
            continue
        transform = cv2.getAffineTransform(dst, src)
        yy, xx = np.mgrid[y0:y1, x0:x1].astype(np.float32)
        mask = np.zeros(xx.shape, np.uint8)
        cv2.fillConvexPoly(mask, np.rint(dst-[x0,y0]).astype(np.int32), 1)
        keep = mask > 0
        mx[y0:y1,x0:x1][keep] = (transform[0,0]*xx+transform[0,1]*yy+transform[0,2])[keep]
        my[y0:y1,x0:x1][keep] = (transform[1,0]*xx+transform[1,1]*yy+transform[1,2])[keep]
    return cv2.remap(image, mx, my, cv2.INTER_LINEAR, borderMode=cv2.BORDER_REFLECT_101)


def track_points(previous, current, points):
    """Zwischen Analysen folgt das Mesh der Kamerabewegung per optischem Fluss."""
    moved, status, error = cv2.calcOpticalFlowPyrLK(
        previous, current, points.reshape(-1, 1, 2), None,
        winSize=(21, 21), maxLevel=3,
        criteria=(cv2.TERM_CRITERIA_EPS | cv2.TERM_CRITERIA_COUNT, 20, .03))
    if moved is None:
        return None
    valid = status.ravel().astype(bool) & (error.ravel() < 35)
    if valid.sum() < 8:
        return None
    transform, inliers = cv2.estimateAffinePartial2D(points[valid], moved.reshape(-1, 2)[valid],
                                                    method=cv2.RANSAC, ransacReprojThreshold=3)
    if transform is None or inliers.sum() < 6:
        return None
    return np.float32(points @ transform[:, :2].T + transform[:, 2])
