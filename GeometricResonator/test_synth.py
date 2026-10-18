import tempfile
import unittest
from pathlib import Path

import numpy as np

from synth import SynthEngine


class SynthEngineTests(unittest.TestCase):
    def test_frequency_changes_with_size(self):
        engine = SynthEngine()
        engine.update(pitch_hz=220.0, size=200.0)
        self.assertAlmostEqual(engine.snapshot().frequency, 110.0)

    def test_both_instruments_render_finite_audio(self):
        for instrument in ("Violine", "Flöte"):
            engine = SynthEngine(sample_rate=8_000, block_size=128)
            engine.update(instrument=instrument)
            audio = engine.render(0.25)
            self.assertEqual(len(audio), 2_000)
            self.assertTrue(np.isfinite(audio).all())
            self.assertGreater(float(np.max(np.abs(audio))), 0.001)

    def test_shapes_have_different_spectra(self):
        engine = SynthEngine(sample_rate=8_000, block_size=128)
        engine.update(shape="Kreis")
        circle = engine.render(0.2)
        engine.update(shape="Dreieck")
        triangle = engine.render(0.2)
        self.assertFalse(np.allclose(circle, triangle))

    def test_wav_export(self):
        engine = SynthEngine(sample_rate=8_000, block_size=128)
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "test.wav"
            engine.export_wav(str(path), 0.1)
            self.assertGreater(path.stat().st_size, 44)


if __name__ == "__main__":
    unittest.main()

