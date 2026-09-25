"""Gesichtseffekte: Tracker, Renderpfade und Einsetzen ins Kamerabild."""
import importlib.util
from concurrent.futures import Future
from pathlib import Path
from unittest.mock import patch
import unittest

import numpy as np

from bildeffekte import face_morph
from bildeffekte.face_morph import face_points
from bildeffekte.face_track import FaceTracker, face_mask

SPEC = importlib.util.spec_from_file_location(
    'face_fx_test', Path(__file__).resolve().parents[1] / 'plugins/52_video_face_fx.py')
module = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(module)

EFFECTS = (module.ZeitversetztesIch, module.Alterungszeitraffer,
           module.GesichtsHologramm, module.KubistischesGesicht)


def done(value):
    future = Future()
    future.set_result(value)
    return future


class FakeClock:
    def __init__(self):
        self.now = 0.0

    def __call__(self):
        return self.now


class FaceFxTests(unittest.TestCase):
    def setUp(self):
        self.frame = np.random.default_rng(4).integers(40, 180, (240, 320, 3), dtype=np.uint8)
        self.points = face_points((110, 50, 100, 130), {})

    def run_effect(self, cls, seconds, points=None):
        """Effekt mit synchroner Erkennung und simulierter Uhr über `seconds` laufen lassen."""
        effect = cls()
        clock = FakeClock()
        effect.clock = clock
        effect.activate()
        points = self.points if points is None else points
        out = self.frame
        with patch.object(face_morph, 'submit_face', side_effect=lambda frame: done(points)):
            for _ in range(int(seconds * 30)):
                out = effect.apply(self.frame, {})
                clock.now += 1 / 30
        return effect, out

    def test_without_face_frame_is_unchanged(self):
        for cls in EFFECTS:
            with self.subTest(effect=cls.name):
                effect, out = self.run_effect(cls, 1.0, points=None)
                with patch.object(face_morph, 'submit_face', side_effect=lambda frame: done(None)):
                    out = effect.apply(self.frame, {})
                np.testing.assert_array_equal(out, self.frame)

    def test_effects_change_face_but_not_background(self):
        moments = {module.ZeitversetztesIch: 4.6, module.Alterungszeitraffer: 5.0,
                   module.GesichtsHologramm: 3.0, module.KubistischesGesicht: 4.0}
        for cls, seconds in moments.items():
            with self.subTest(effect=cls.name):
                _, out = self.run_effect(cls, seconds)
                self.assertEqual(out.shape, self.frame.shape)
                self.assertEqual(out.dtype, np.uint8)
                np.testing.assert_array_equal(out[:, :40], self.frame[:, :40])
                if cls is not module.ZeitversetztesIch:
                    # Das Zeit-Ich tauscht bei ruhigem Kopf dasselbe Gesicht ein; die anderen verändern sichtbar.
                    face = (slice(90, 160), slice(130, 190))
                    self.assertGreater(np.abs(out[face].astype(float) - self.frame[face]).mean(), 3)

    def test_render_paths_are_safe_for_clipped_faces(self):
        crop = self.frame[20:200, 60:260]
        for offset in ((-150, -90), (0, 0), (170, 120)):
            points = np.float32(self.points - [60, 20] + offset)
            age = module.Alterungszeitraffer()
            for a, flash in ((1, 0), (1, 1), (-1, 0)):
                result = age.render_age(crop, points, a, flash)
                self.assertEqual(result[0].shape[:2], crop.shape[:2])
            holo = module.GesichtsHologramm()
            for burst in (0, 0.5, 1):
                result = holo.render_holo(crop, points, 1, burst)
                self.assertEqual(result[1].shape, crop.shape[:2])
            cubist = module.KubistischesGesicht()
            params, a, b, mix = cubist.layout(4.0)
            result = cubist.render_cubist(crop, points, params, a, b, mix)
            self.assertEqual(result[0].shape[:2], crop.shape[:2])

    def test_identity_moments_leave_frame_alone(self):
        crop = self.frame[20:200, 60:260]
        points = np.float32(self.points - [60, 20])
        self.assertIsNone(module.Alterungszeitraffer().render_age(crop, points, 0, 0))
        self.assertIsNone(module.GesichtsHologramm().render_holo(crop, points, 0, 0))
        cubist = module.KubistischesGesicht()
        self.assertIsNone(cubist.render_cubist(crop, points, cubist._blend({}, {}, 1)))

    def test_time_self_uses_face_from_four_seconds_ago(self):
        effect, _ = self.run_effect(module.ZeitversetztesIch, 3.5)
        self.assertIsNone(effect._delayed(effect.clock()))
        effect, _ = self.run_effect(module.ZeitversetztesIch, 5.0)
        stamp = effect._delayed(effect.clock())[0]
        self.assertAlmostEqual(effect.clock() - stamp, effect.DELAY, delta=0.05)
        self.assertLessEqual(effect.clock() - effect.history[0][0], effect.DELAY + 1.0 + 1e-6)

    def test_time_self_swaps_in_the_old_face(self):
        effect = module.ZeitversetztesIch()
        crop = self.frame[20:200, 60:260]
        points = np.float32(self.points - [60, 20])
        old = np.full_like(crop, 200)
        image, alpha = effect.swap(crop, points, old, points)
        inside = alpha > 0.9
        self.assertTrue(inside.any())
        # Helligkeit wird ans aktuelle Licht angeglichen, bleibt aber eine gleichförmige Fläche.
        self.assertLess(image[inside].std(), 1.0)

    def test_aging_timeline(self):
        cls = module.Alterungszeitraffer
        self.assertEqual(cls.age(0.5), 0)
        self.assertEqual(cls.age(6.5), 1)
        self.assertEqual(cls.age(12), -1)
        self.assertGreater(cls.flash(6.35), 0.5)
        self.assertEqual(cls.flash(3), 0)

    def test_hologram_mesh_follows_points(self):
        holo = module.GesichtsHologramm()
        moved = self.points + np.float32([7, -3])
        np.testing.assert_allclose(holo.mesh(moved) - holo.mesh(self.points), [[7, -3]] * len(holo.bary), atol=1e-3)
        self.assertGreater(len(holo.triangles), 100)

    def test_cubist_layouts_cycle(self):
        cubist = module.KubistischesGesicht()
        first, *_ = cubist.layout(0.0 + cubist.MORPH)
        later, *_ = cubist.layout(cubist.HOLD + cubist.MORPH)
        self.assertFalse(np.allclose(first['mouth'], later['mouth']))


class FaceTrackerTests(unittest.TestCase):
    def test_tracker_reports_points_after_detection_and_drops_them_when_lost(self):
        clock = FakeClock()
        tracker = FaceTracker(clock=clock)
        frame = np.random.default_rng(1).integers(0, 255, (240, 320, 3), dtype=np.uint8)
        points = face_points((110, 50, 100, 130), {})
        with patch.object(face_morph, 'submit_face', side_effect=lambda f: done(points)):
            self.assertIsNone(tracker.update(frame))   # erste Erkennung läuft noch
            clock.now += 0.1
            self.assertIsNotNone(tracker.update(frame))
        with patch.object(face_morph, 'submit_face', side_effect=lambda f: done(None)):
            clock.now += 0.1
            tracker.update(frame)
            clock.now += 0.1
            self.assertIsNone(tracker.update(frame))

    def test_face_mask_is_soft_and_inside_rim(self):
        points = face_points((110, 50, 100, 130), {})
        mask = face_mask(points, (240, 320))
        self.assertEqual(mask[:, :100].max(), 0)
        self.assertGreater(mask[110, 160], 0.9)
        self.assertTrue(((mask > 0.05) & (mask < 0.95)).any())


if __name__ == '__main__':
    unittest.main()
