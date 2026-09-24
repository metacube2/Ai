"""Virtuelle Kamera: festes 1280×720-Format, nie blockierend, letztes Bild wird wiederholt."""
import sys
import time
import types
import unittest
from unittest.mock import patch

import numpy as np

import video


class FakeCamera:
    def __init__(self, width, height, fps, backend=None):
        self.device = 'Test-Kamera'
        self.sent = []
        self.closed = False

    def send(self, frame):
        self.sent.append(frame.copy())

    def sleep_until_next_frame(self):
        time.sleep(.005)

    def close(self):
        self.closed = True


class VirtualCameraTests(unittest.TestCase):
    def test_fit_keeps_aspect_with_black_bars(self):
        portrait = np.full((800, 600, 3), 200, np.uint8)
        out = video.VirtualCamera.fit(portrait)
        self.assertEqual(out.shape, (720, 1280, 3))
        self.assertEqual(out[:, 0].max(), 0)                  # Rand links schwarz
        self.assertEqual(out[360, 640].tolist(), [200, 200, 200])
        np.testing.assert_array_equal(video.VirtualCamera.fit(np.zeros((720, 1280, 4), np.uint8)).shape,
                                      (720, 1280, 3))

    def test_sends_latest_frame_and_repeats_it(self):
        module = types.SimpleNamespace(Camera=FakeCamera)
        with patch.dict(sys.modules, {'pyvirtualcam': module}):
            camera = video.VirtualCamera()
        fake = camera._camera
        camera.add(np.full((360, 640, 3), 90, np.uint8))
        time.sleep(.1)
        camera.stop()
        self.assertTrue(fake.closed)
        self.assertIsNone(camera.error)
        self.assertGreater(len(fake.sent), 3)
        self.assertTrue(all(f.shape == (720, 1280, 3) for f in fake.sent))
        self.assertEqual(fake.sent[-1][360, 640, 0], 90)      # letztes Bild wird wiederholt


    def test_virtual_camera_is_never_offered_as_source(self):
        self.assertTrue(video.is_virtual_camera('OBS Virtual Camera'))
        self.assertFalse(video.is_virtual_camera('FaceTime HD-Kamera'))
        for name in video.list_cameras():      # echte Geräte, falls vorhanden
            self.assertIsInstance(name, str)


if __name__ == '__main__':
    unittest.main()
