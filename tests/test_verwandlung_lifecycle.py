"""Randfälle der Live-Verwandlungen: leere Quelle und asynchrone Neustarts."""
import importlib.util
from pathlib import Path
from concurrent.futures import Future
from unittest.mock import patch
import unittest
import numpy as np
from bildeffekte import verwandlung as core


def predator():
    path = Path(__file__).resolve().parents[1] / 'plugins/39_video_verwandlung_spezial.py'
    spec = importlib.util.spec_from_file_location('special_lifecycle', path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module.PredatorTarnung()


class LifecycleTests(unittest.TestCase):
    def test_empty_source_survives_loop_boundary(self):
        class Fluid(core.FluidVerwandlung):
            name = 'Test'
        effect = Fluid()
        frame = np.full((60, 80, 3), 80, np.uint8)
        effect.apply(frame, {})
        effect.started -= core.FORWARD_LIMIT + 1
        for _ in range(3):
            np.testing.assert_array_equal(effect.apply(frame, {}), frame)
        self.assertIsNone(effect._loop_end)

    def test_predator_restart_discards_old_running_analysis(self):
        effect = predator()
        frame = np.full((160, 240, 3), 80, np.uint8)
        old, new = Future(), Future()
        old.set_running_or_notify_cancel()
        effect._pending = old
        effect._mask = np.full(frame.shape[:2], 255, np.uint8)
        effect.activate()
        old.set_result(np.full(frame.shape[:2], 255, np.uint8))
        with patch.object(core, 'submit_person_mask', return_value=new):
            np.testing.assert_array_equal(effect.apply(frame, {}), frame)
        self.assertIs(effect._pending, new)
        self.assertIsNone(effect._mask)

    def test_background_snapshot_survives_replacement(self):
        plate = core.BackgroundPlate()
        frame = np.full((60, 80, 3), 80, np.uint8)
        mask = np.zeros(frame.shape[:2], np.uint8)
        for _ in range(3):
            plate.update(frame, mask)
        snapshot = plate.snapshot()
        plate.reset()
        self.assertTrue(plate.matches(frame, mask == 0, snapshot=snapshot))
        self.assertFalse(plate.matches(frame, mask == 0))
        self.assertEqual(snapshot[0].shape[:2], snapshot[1].shape)
