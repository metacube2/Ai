"""Tests der Verwandlungs-Effekte (38_video_verwandlung_material, 39_video_verwandlung_spezial)."""

import importlib.util
import time
import unittest
from concurrent.futures import Future
from pathlib import Path
from unittest.mock import patch

import cv2
import numpy as np

import video
import bildeffekte.verwandlung as core


def _load(filename, name):
    path = Path(video.__file__).parent / 'plugins' / filename
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def _room():
    rng = np.random.default_rng(3)
    texture = cv2.GaussianBlur(rng.uniform(0, 255, (120, 160, 3)).astype(np.float32), (0, 0), 3)
    return np.clip((texture - texture.mean()) * 4 + 128, 0, 255).astype(np.uint8)


def _person(room):
    frame = room.copy()
    cv2.circle(frame, (80, 35), 20, (200, 160, 130), -1)
    cv2.rectangle(frame, (60, 52), (100, 119), (130, 90, 60), -1)
    return frame


def _prepared(cls, frame):
    """Effekt mit kleinem Bild (GrabCut-Weg, ohne Vision) synchron vorbereiten."""
    effect = cls()
    with patch.object(core, 'submit_person_mask', return_value=Future()):
        effect.apply(frame, {})
    effect._on_stop()
    return effect


class MaterialTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.module = _load('38_video_verwandlung_material.py', 'verwandlung_material_test')

    def setUp(self):
        core.PLATE.reset()
        self.room = _room()
        for _ in range(3):
            core.PLATE.update(self.room, np.zeros(self.room.shape[:2], np.uint8))
        self.frame = _person(self.room)

    def _frames(self, cls, times=(0.0, 1.0, 3.0, 6.0, 9.0)):
        effect = _prepared(cls, self.frame)
        out = {t: effect._render(t) for t in times}
        for image in out.values():
            self.assertEqual(image.shape, self.frame.shape)
            self.assertEqual(image.dtype, np.uint8)
        np.testing.assert_array_equal(out[0.0], effect.scene)      # Start = unverändert
        return effect, out

    def test_gold_is_gold(self):
        effect, out = self._frames(self.module.Fluessiggold)
        inside = effect.mask > .9
        red, blue = out[3.0][..., 0][inside].mean(), out[3.0][..., 2][inside].mean()
        self.assertGreater(red, blue * 1.4)

    def test_glass_shows_the_background_through_the_person(self):
        effect, out = self._frames(self.module.Fluessigglas)
        inside = effect.mask > .9
        to_room = np.abs(out[3.0].astype(float) - self.room)[inside].mean()
        to_person = np.abs(out[3.0].astype(float) - self.frame)[inside].mean()
        self.assertLess(to_room, to_person)

    def test_lava_glows(self):
        effect, out = self._frames(self.module.LavaSchmelze)
        inside = effect.mask > .9
        self.assertGreater(float((out[3.0][..., 0][inside] > 200).mean()), .05)

    def test_honey_flows_slower_than_metal(self):
        from bildeffekte.liquid_sim import LiquidSheet
        mask = np.zeros((120, 160), np.float32)
        mask[20:120, 55:105] = 1
        metal = LiquidSheet(mask, 4, 120, 100)
        honey = LiquidSheet(mask, 4, 120, 100, **self.module.HonigSchmelze.fluid)
        for sim in (metal, honey):
            sim.advance(2.0, lambda t: .6)
        self.assertGreater(honey.count, metal.count)


class SpecialTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.module = _load('39_video_verwandlung_spezial.py', 'verwandlung_spezial_test')

    def setUp(self):
        core.PLATE.reset()
        self.room = _room()
        for _ in range(3):
            core.PLATE.update(self.room, np.zeros(self.room.shape[:2], np.uint8))
        self.frame = _person(self.room)

    def test_ash_disappears_and_reassembles(self):
        effect = _prepared(self.module.ZerfallZuAsche, self.frame)
        end = effect._loop_end
        np.testing.assert_array_equal(effect._render(0.0), effect.scene)
        gone = effect._render(end).astype(float)
        inside = effect.mask > .9
        self.assertLess(np.abs(gone - effect.background)[inside].mean(), 3)
        self.assertEqual(effect._timeline(end * 2 + effect.hold_end + .1), 0.0)
        np.testing.assert_array_equal(effect._render_live(end * 2 + effect.hold_end + .1), effect.scene)

    def test_ice_shards_land_on_the_floor_and_fly_back(self):
        effect = _prepared(self.module.EisUndZerspringen, self.frame)
        self.assertGreater(len(effect.shards), 10)
        end = effect._loop_end
        landed = effect._render(end).astype(float)
        changed = np.abs(landed - effect.background).max(axis=2) > 25
        rows = np.nonzero(changed.any(axis=1))[0]
        self.assertGreater(rows.min(), effect.bounds[1] + effect.bounds[3] * .4)   # nur noch unten
        back = end * 2 + effect.hold_end + .1
        np.testing.assert_array_equal(effect._render_live(back), effect.scene)

    def test_emerge_starts_as_puddle_and_ends_as_the_person(self):
        module = self.module
        with patch.object(module, 'PrecomputeProcess', side_effect=OSError):
            effect = _prepared(module.AusDerPfuetze, self.frame)
            effect._start_job()
        deadline = time.monotonic() + 60
        elapsed = 0.0
        while effect._loop_end is None and time.monotonic() < deadline:
            effect._render_live(elapsed)
            elapsed += .1
            time.sleep(.01)
        self.assertIsNotNone(effect._loop_end)
        last = effect._history[-1][1]
        self.assertLess(len(last[1]), effect.sim.initial_count * .016)   # Ende = Pfütze
        start = effect._offset + effect.hold_end + effect._loop_end + .1
        np.testing.assert_array_equal(effect._render_live(start), effect.scene)

    def test_shapeshifter_flows_from_first_to_second_pose(self):
        module = self.module
        effect = _prepared(module.Formwandler, self.frame)
        second_scene = self.room.copy()
        cv2.rectangle(second_scene, (10, 40), (50, 119), (40, 160, 90), -1)
        mask = np.zeros(self.room.shape[:2], np.uint8)
        mask[40:120, 10:51] = 255
        second = effect._relief_for(second_scene, {'mask': mask, 'face': None, 'features': {}})
        effect._match(second)
        self.assertEqual(len(effect.pa), len(effect.pb))
        # Zugeordnete Plätze liegen in der zweiten Form.
        inside = mask[np.clip(effect.pb[:, 1].astype(int), 0, 119), np.clip(effect.pb[:, 0].astype(int), 0, 159)]
        self.assertGreater(float((inside > 0).mean()), .9)
        np.testing.assert_array_equal(effect._morph(0.0), effect.scene)
        final = effect._morph(effect.END).astype(float)
        self.assertLess(np.abs(final[60:110, 15:45] - second_scene[60:110, 15:45]).mean(), 12)
        self.assertLess(np.abs(final[70:110, 65:95] - self.room[70:110, 65:95]).mean(), 12)

    def test_predator_shows_the_background_instead_of_the_person(self):
        module = self.module
        frame = cv2.resize(_person(self.room), (320, 240))
        room = cv2.resize(self.room, (320, 240))
        core.PLATE.reset()
        for _ in range(3):
            core.PLATE.update(room, np.zeros(room.shape[:2], np.uint8))
        person = np.zeros(room.shape[:2], np.uint8)
        cv2.circle(person, (160, 70), 40, 255, -1)
        cv2.rectangle(person, (120, 104), (200, 239), 255, -1)
        def mask_ready(small, then):
            future = Future()
            future.set_result(person)
            return future
        effect = module.PredatorTarnung()
        effect.activate()
        with patch.object(core, 'submit_person_mask', side_effect=mask_ready):
            effect.apply(frame, {})
            effect._engaged_at = time.monotonic() - 5          # Tarnung voll eingeschaltet
            effect._next_glitch = time.monotonic() + 60
            out = effect.apply(frame, {}).astype(float)
        inside = cv2.erode(person, np.ones((9, 9), np.uint8)) > 0
        self.assertLess(np.abs(out - room)[inside].mean(), np.abs(out - frame)[inside].mean())
        self.assertEqual(out.shape, frame.shape)
