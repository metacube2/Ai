"""Effektkette: mehrere Bild-Effekte nacheinander, z. B. VHS → Matrix-Agent."""
import threading
import time
import unittest
from unittest.mock import Mock

import numpy as np

import video
from bildeffekte import IEffectStrategy


class Step(IEffectStrategy):
    """Hängt seine Nummer an: so ist die Reihenfolge im Ergebnis ablesbar."""

    def __init__(self, name, digit, fail=False):
        self._name, self.digit, self.fail = name, digit, fail
        self.events = []

    @property
    def name(self):
        return self._name

    def apply(self, frame, config):
        if self.fail:
            raise RuntimeError('kaputt')
        return frame * 10 + self.digit

    def activate(self):
        self.events.append('an')

    def deactivate(self):
        self.events.append('aus')


class ChainTests(unittest.TestCase):
    def setUp(self):
        self.bus = video.EventBus()
        self.processor = video.EffectProcessor(Mock(), self.bus, video.VideoFeatures())
        self.addCleanup(self.processor._lifecycle.shutdown)
        self.a, self.b, self.c = Step('A', 1), Step('B', 2), Step('C', 3)
        for effect in (self.a, self.b, self.c):
            self.processor.register_effect(effect)
        self.changes = []
        self.bus.subscribe('chain_changed', self.changes.append)

    def run_chain(self):
        frame = np.zeros((2, 2, 3), np.int64)
        frames = []
        self.bus.subscribe('frame_processed', frames.append)
        stop = threading.Event()
        self.processor.capture_service.capture.side_effect = lambda region: (stop.set(), frame)[1]
        self.processor._capture_loop({'kind': 'screen'}, stop)
        return int(frames[0][0, 0, 0])

    def test_single_effect_behaves_as_before(self):
        self.assertEqual(self.processor.chain_names, ['A'])
        self.processor.change_effect('B')
        self.assertEqual(self.processor.chain_names, ['B'])
        self.assertIs(self.processor.current_strategy, self.b)
        self.assertEqual(self.run_chain(), 2)

    def test_append_order_move_and_remove(self):
        self.processor.add_effect('B')
        self.processor.add_effect('C')
        self.assertEqual(self.processor.chain_names, ['A', 'B', 'C'])
        self.assertIs(self.processor.current_strategy, self.c)
        self.assertEqual(self.run_chain(), 123)          # A zuerst, C zuletzt
        self.processor.move_slot(-1)
        self.assertEqual(self.processor.chain_names, ['A', 'C', 'B'])
        self.assertEqual(self.run_chain(), 132)
        self.processor.select_slot(0)
        self.processor.change_effect('B')                 # schon drin: nur auswählen
        self.assertEqual(self.processor.chain_names, ['A', 'C', 'B'])
        self.assertIs(self.processor.current_strategy, self.b)
        self.processor.remove_slot()
        self.assertEqual(self.processor.chain_names, ['A', 'C'])
        self.processor.remove_slot()
        self.processor.remove_slot()                      # letzter bleibt stehen
        self.assertEqual(len(self.processor.chain), 1)
        self.assertEqual(self.changes[-1], (self.processor.chain_names, self.processor.selected))

    def test_duplicates_are_refused(self):
        self.processor.add_effect('B')
        self.processor.add_effect('A')
        self.assertEqual(self.processor.chain_names, ['A', 'B'])
        self.assertIs(self.processor.current_strategy, self.a)

    def test_failing_link_passes_its_input_on(self):
        self.processor.register_effect(Step('X', 9, fail=True))
        self.processor.add_effect('X')
        self.processor.add_effect('C')
        self.assertEqual(self.run_chain(), 13)

    def test_lifecycle_covers_the_whole_chain(self):
        self.processor.add_effect('B')
        self.processor._thread = type('T', (), {'is_alive': lambda self: True})()
        self.processor.add_effect('C')                    # läuft: C startet sofort
        self.processor.remove_slot(1)                     # B stoppt
        self.processor._lifecycle.submit(lambda: None).result(timeout=2)
        self.assertEqual(self.c.events, ['an'])
        self.assertEqual(self.b.events, ['aus'])

    def test_removed_plugin_leaves_rest_of_chain(self):
        self.processor.add_effect('B')
        self.processor.remove_effect('A')
        self.assertEqual(self.processor.chain_names, ['B'])
        self.processor.remove_effect('B')
        self.assertEqual(self.processor.chain_names, ['C'])   # Rückfall auf vorhandenen Effekt


if __name__ == '__main__':
    unittest.main()
