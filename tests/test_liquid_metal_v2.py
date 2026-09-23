"""Regressionstests der Flüssigmetall-Kopie II: ../venv/bin/python -m unittest discover -s tests -v

Bewusst eine eigene Datei, damit Arbeiten am ursprünglichen Effekt und an der
Kopie sich nicht in derselben Testdatei überschneiden.
"""

import importlib.util
import unittest
from concurrent.futures import Future
from pathlib import Path
from unittest.mock import patch

import numpy as np
import cv2

import video
import bildeffekte.verwandlung as core


class LiquidMetalIITests(unittest.TestCase):
    def test_settled_metal_has_moving_reflections_on_plain_background(self):
        path = Path(video.__file__).parent / 'plugins' / '37_video_liquid_metal_v2.py'
        spec = importlib.util.spec_from_file_location('liquid2_reflections_test', path)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        effect = module.FluessigmetallII()
        frame = np.full((120, 160, 3), 80, np.uint8)
        cv2.circle(frame, (80, 35), 20, (200, 160, 130), -1)
        cv2.rectangle(frame, (60, 52), (100, 105), (130, 90, 60), -1)
        effect.apply(frame, {})
        first, later = effect._render(8), effect._render(8.4)
        # Zentrum der fertigen Pfütze; nicht die Außenkante gegen den Hintergrund.
        interior = np.zeros((120, 160), np.uint8)
        cv2.ellipse(interior, (80, 102), (20, 5), 0, 0, 360, 1, -1)
        luminance = first.mean(axis=2)[interior > 0]
        self.assertGreater(float(luminance.std()), 20)
        self.assertGreater(float(np.ptp(luminance)), 80)
        change = np.abs(first.astype(np.float32) - later.astype(np.float32))
        self.assertGreater(float(change[interior > 0].mean()), 2)

    def test_pending_person_analysis_does_not_replace_new_snapshot(self):
        path = Path(video.__file__).parent / 'plugins' / '37_video_liquid_metal_v2.py'
        spec = importlib.util.spec_from_file_location('liquid2_async_test', path)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        old, new = Future(), Future()
        old.set_running_or_notify_cancel()
        first = np.full((160, 240, 3), 30, np.uint8)
        second = np.full_like(first, 180)
        effect = module.FluessigmetallII()
        with patch.object(core, 'submit_portrait', side_effect=[old, new]):
            np.testing.assert_array_equal(effect.apply(first, {}), first)
            np.testing.assert_array_equal(effect.apply(second, {}), first)
            effect.activate()
            np.testing.assert_array_equal(effect.apply(second, {}), second)
            old.set_result(None)
            effect.apply(second, {})
            self.assertIs(effect._pending, new)
            mask = np.zeros((160, 240), np.uint8)
            mask[20:145, 80:165] = 255
            new.set_result({'mask': mask, 'face': None, 'features': {}})
            effect.apply(second, {})
            self.assertEqual(effect.segmentation, 'Apple Vision')
            np.testing.assert_array_equal(effect._render(0), second)
            np.testing.assert_array_equal(effect._render(8)[0, 0], second[0, 0])

    def test_freezes_first_frame_then_restarts_with_new_frame(self):
        path = Path(video.__file__).parent / 'plugins' / '37_video_liquid_metal_v2.py'
        spec = importlib.util.spec_from_file_location('liquid2_metal_test', path)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        effect = module.FluessigmetallII()
        first = np.full((120, 160, 3), 35, dtype=np.uint8)
        cv2.circle(first, (80, 40), 18, (220, 165, 125), -1)
        cv2.rectangle(first, (58, 58), (102, 108), (90, 140, 190), -1)
        second = np.full_like(first, 190)
        beginning = effect.apply(first, {})
        frozen = effect.scene.copy()
        effect.started -= 4.0
        melted = effect.apply(second, {})
        np.testing.assert_array_equal(effect.scene, frozen)
        self.assertEqual(melted.shape, first.shape)
        self.assertEqual(melted.dtype, np.uint8)
        self.assertGreater(np.abs(beginning.astype(np.int16) - melted.astype(np.int16)).mean(), 2)
        effect.activate()


def _load_plugin(name):
    path = Path(video.__file__).parent / 'plugins' / '37_video_liquid_metal_v2.py'
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class LiquidSimulationTests(unittest.TestCase):
    def setUp(self):
        from bildeffekte.liquid_sim import LiquidSheet
        self.mask = np.zeros((120, 160), np.float32)
        self.mask[20:120, 55:105] = 1
        self.LiquidSheet = LiquidSheet

    def test_mass_is_conserved_and_drains_into_pool(self):
        sim = self.LiquidSheet(self.mask, 4, 120, 100)
        sim.advance(4.0, lambda t: 1.0)
        # Jedes Partikel ist entweder noch im Körper oder in der Bodenpfütze.
        self.assertEqual(sim.count + sim.pool_count, sim.initial_count)
        self.assertGreater(sim.pool_count, 0)
        self.assertTrue(np.all(sim.y < sim.floor))
        self.assertTrue(np.isfinite(sim.u).all() and np.isfinite(sim.v).all())

    def test_rigid_metal_maps_every_pixel_to_itself(self):
        sim = self.LiquidSheet(self.mask, 4, 120, 100)
        sim.advance(1.0, lambda t: 0.0)
        density, map_x, map_y = sim.fields((160, 120))
        grid_y, grid_x = np.mgrid[0:120, 0:160].astype(np.float32)
        inside = density > .5
        self.assertGreater(np.count_nonzero(inside), 1000)
        # Ohne Bewegung keine Verschiebung: kein Rauschen im Relief.
        self.assertLess(float(np.abs(map_x - grid_x)[inside].max()), 1e-3)
        self.assertLess(float(np.abs(map_y - grid_y)[inside].max()), 1e-3)

    def test_body_sinks_once_liquid(self):
        sim = self.LiquidSheet(self.mask, 4, 120, 100)
        top = float(sim.y.min())
        sim.advance(3.0, lambda t: 1.0)
        self.assertGreater(float(sim.y.min()) if sim.count else sim.floor, top + 5)


class BackgroundPlateTests(unittest.TestCase):
    def setUp(self):
        self.module = _load_plugin('liquid2_plate_test')
        # Raum mit Struktur (wie Regale, Poster), nicht bloß ein Farbverlauf.
        rng = np.random.default_rng(3)
        texture = cv2.GaussianBlur(rng.uniform(0, 255, (120, 160, 3)).astype(np.float32), (0, 0), 3)
        self.room = np.clip((texture - texture.mean()) * 4 + 128, 0, 255).astype(np.uint8)

    def test_learns_only_where_nobody_stands_and_resets_on_camera_move(self):
        plate = self.module.BackgroundPlate()
        person = np.zeros((120, 160), np.uint8)
        person[30:120, 60:100] = 255
        plate.update(self.room, person)
        self.assertEqual(float(plate.confidence[70, 80]), 0.0)
        self.assertGreater(float(plate.confidence[10, 10]), 0.0)
        np.testing.assert_allclose(plate.image[10, 10], self.room[10, 10])
        plate.update(self.room, person)
        plate.update(self.room, person)
        self.assertTrue(plate.matches(self.room, person == 0))
        moved = np.roll(self.room, 40, axis=1)
        self.assertFalse(plate.matches(moved, person == 0))
        plate.update(moved, person)
        np.testing.assert_allclose(plate.image[10, 10], moved[10, 10])

    def test_remembered_background_replaces_inpainting_behind_person(self):
        module = self.module
        for _ in range(3):
            module._PLATE.update(self.room, np.zeros((120, 160), np.uint8))
        frame = self.room.copy()
        cv2.circle(frame, (80, 35), 20, (200, 160, 130), -1)
        cv2.rectangle(frame, (60, 52), (100, 119), (130, 90, 60), -1)
        effect = module.FluessigmetallII()
        with patch.object(core, 'submit_person_mask', return_value=Future()):
            effect.apply(frame, {})
        self.assertGreater(effect.plate_coverage, .9)
        # Mitten hinter der Person steht der gemerkte Raum, keine Verlaufsschliere.
        np.testing.assert_allclose(effect.background[85, 80], self.room[85, 80], atol=2)

    def test_waits_for_person_when_nobody_is_in_frame(self):
        module = self.module
        empty, person = Future(), Future()
        present = []

        def live(frame, then):
            future = Future()
            future.set_result(bool(present))
            return future

        frame = cv2.resize(self.room, (320, 240))      # groß genug für die Personenerkennung
        with patch.object(core, 'submit_portrait', side_effect=[empty, person]) as portrait, \
                patch.object(core, 'submit_person_mask', side_effect=live):
            effect = module.FluessigmetallII()
            effect.apply(frame, {})
            empty.set_result({'mask': None, 'person': False})
            effect._live_time = 0
            hinted = effect.apply(frame, {})
            self.assertTrue(effect.watching)
            self.assertFalse(np.array_equal(hinted, frame))    # Hinweis eingeblendet
            present.append(True)
            for _ in range(3):
                effect._live_time = 0
                effect.apply(frame, {})
            self.assertFalse(effect.watching)
            self.assertEqual(portrait.call_count, 2)            # neu eingefroren
            self.assertIs(effect._pending, person)


class ReversePlaybackTests(unittest.TestCase):
    def test_melt_plays_backwards_to_the_exact_start_image(self):
        module = _load_plugin('liquid2_reverse_test')
        frame = np.full((120, 160, 3), 80, np.uint8)
        cv2.circle(frame, (80, 35), 20, (200, 160, 130), -1)
        cv2.rectangle(frame, (60, 52), (100, 119), (130, 90, 60), -1)
        effect = module.FluessigmetallII()
        with patch.object(core, 'submit_person_mask', return_value=Future()):
            effect.apply(frame, {})
        effect._stop_sim()
        for t in np.arange(0, 12.01, 1 / 15):
            effect._record(effect._snapshot(float(t), live=False))
        end = effect._loop_end = effect._history[-1][0]
        self.assertAlmostEqual(effect._timeline(end + .5), end)                  # Pfütze
        back = end + module.HOLD_PUDDLE + 3.0
        self.assertAlmostEqual(effect._timeline(back), end - 3.0)                # rückwärts
        # Rückwärts wächst die Figur wieder aus der Pfütze.
        early, late = effect._at(end - 8.0), effect._at(end - 1.0)
        self.assertGreater(len(early[1]), len(late[1]))
        self.assertLess(early[5], late[5])
        start = end + module.HOLD_PUDDLE + end + .5
        self.assertEqual(effect._timeline(start), 0.0)
        np.testing.assert_array_equal(effect._render_live(start), effect.scene)  # Startbild
        again = end + module.HOLD_PUDDLE + end + module.HOLD_START + 2.0
        self.assertAlmostEqual(effect._timeline(again), 2.0)                     # wieder vorwärts
