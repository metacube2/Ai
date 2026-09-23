"""Hardwarefreie Regressionstests: ../venv/bin/python -m unittest discover -s tests -v"""

import importlib.util
import tempfile
import threading
import unittest
from concurrent.futures import Future
from pathlib import Path
from types import SimpleNamespace
from unittest.mock import Mock, patch

import numpy as np
import cv2

import video
from bildeffekte import Container, IAudioEffect, IEffectStrategy, StftAudioEffect


class IdentityAudio(StftAudioEffect):
    name = "Identity"

    def process_frame(self, mag, phase, features):
        return mag, phase


class AudioMixTests(unittest.TestCase):
    def test_stereo_survives_dry_and_partial_mix_with_latency(self):
        rng = np.random.default_rng(123)
        left = rng.normal(0, 0.1, 9000).astype(np.float32)
        for right in (-left, rng.normal(0, 0.1, len(left)).astype(np.float32)):
            source = np.column_stack((left, right))
            for mix in (0.0, 0.35, 1.0):
                with self.subTest(antiphase=np.array_equal(right, -left), mix=mix):
                    engine = video.AudioEffectEngine(video.VideoFeatures())
                    effect = IdentityAudio()
                    engine.register(effect)
                    engine.select(effect.name)
                    engine.mix = mix
                    result = []
                    pos = 0
                    sizes = (128, 333, 512, 1024)
                    while pos < len(source):
                        n = sizes[len(result) % len(sizes)]
                        result.append(engine.process(source[pos:pos + n]))
                        pos += n
                    output = np.concatenate(result)
                    delayed = np.concatenate((np.zeros((effect.latency, 2), np.float32),
                                              source))[:len(source)]
                    expected = delayed * (1 - mix) + delayed.mean(axis=1, keepdims=True) * mix
                    self.assertEqual(output.dtype, np.float32)
                    np.testing.assert_allclose(output, expected, atol=1e-7)

    def test_restart_with_different_channels_and_sample_rate(self):
        engine = video.AudioEffectEngine(video.VideoFeatures())
        effect = IdentityAudio()
        engine.register(effect)
        engine.select(effect.name)
        engine.mix = 0
        engine.process(np.ones((512, 2), np.float32))
        engine.prepare(44100, 1)
        source = np.full((4096, 1), 0.25, np.float32)
        output = engine.process(source)
        self.assertEqual(effect.sample_rate, 44100)
        np.testing.assert_array_equal(output[:effect.latency], 0)
        np.testing.assert_array_equal(output[effect.latency:], source[:-effect.latency])

    def test_no_effect_preserves_input(self):
        engine = video.AudioEffectEngine(video.VideoFeatures())
        source = np.array([[0.2, -0.3]], np.float32)
        np.testing.assert_array_equal(engine.process(source), source)


PLUGIN = '''
import numpy as np
from bildeffekte import IAudioEffect, IEffectStrategy

class Picture(IEffectStrategy):
    name = 'Reload picture'
    def apply(self, frame, config):
        return np.full_like(frame, VALUE)

class Sound(IAudioEffect):
    name = 'Reload sound'
    latency = 0
    def reset(self, sample_rate):
        self.sample_rate = sample_rate
    def process(self, block, features):
        return np.full_like(block, VALUE / 1000)
'''


class ReloadTests(unittest.TestCase):
    def setUp(self):
        Container.reset()
        self.addCleanup(Container.reset)
        self.bus = video.EventBus()
        self.features = video.VideoFeatures()
        self.processor = video.EffectProcessor(Mock(), self.bus, self.features)
        self.addCleanup(self.processor._lifecycle.shutdown)
        self.engine = video.AudioEffectEngine(self.features)
        self.audio = video.AudioController(Mock(), self.engine, self.bus, self.features)
        self.addCleanup(self.audio.shutdown)
        Container.register_instance_many(IEffectStrategy, video.PassThroughEffectStrategy())
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)
        directories = patch.object(video, 'plugin_directories', return_value=[self.directory.name])
        directories.start()
        self.addCleanup(directories.stop)
        self.path = Path(self.directory.name) / 'reload_example.py'
        self.path.write_text(PLUGIN.replace('VALUE', '10'))
        self.plugins = video.PluginManager(self.bus)
        self.plugins.load()
        self.sync()
        self.processor.change_effect('Reload picture')
        self.audio.select_effect('Reload sound')
        self.engine.process(np.zeros((512, 2), np.float32))

    def sync(self):
        self.assertEqual(video.sync_effects(self.processor, self.engine, self.audio), [])
        self.assertEqual(self.plugins.errors, [])

    def test_active_plugins_use_new_code_without_reselection(self):
        old = self.processor.current_strategy
        events = []
        old.deactivate = lambda: events.append('old stopped')
        # Exercise the real lifecycle executor without screen capture hardware.
        self.processor._thread = SimpleNamespace(is_alive=lambda: True)
        self.path.write_text(PLUGIN.replace('VALUE', '200'))
        self.plugins.reload()
        fresh = Container.resolve_all(IEffectStrategy)[-1]
        fresh.activate = lambda: events.append('new started')
        self.sync()
        self.processor._lifecycle.submit(lambda: None).result(timeout=2)
        self.assertEqual(events, ['old stopped', 'new started'])
        frame = np.zeros((16, 16, 3), np.uint8)
        np.testing.assert_array_equal(self.processor.current_strategy.apply(frame, {}), 200)
        output = self.engine.process(np.zeros((512, 2), np.float32))
        np.testing.assert_allclose(output, 0.2)
        self.assertEqual(self.audio.active, 'Reload sound')

    def test_deleted_active_plugins_fall_back_and_disable_audio(self):
        self.path.unlink()
        self.plugins.reload()
        self.sync()
        self.assertEqual(self.processor.current_strategy.name, 'Original')
        self.assertIsNone(self.audio.active)
        source = np.full((512, 2), 0.3, np.float32)
        np.testing.assert_array_equal(self.engine.process(source), source)

    def test_audio_reload_takes_effect_at_next_block(self):
        self.path.write_text(PLUGIN.replace('VALUE', '200'))
        self.plugins.reload()
        self.sync()
        fresh = self.engine.effects['Reload sound']
        fresh.reset = Mock(wraps=fresh.reset)
        fresh.reset.assert_not_called()
        for _ in range(3):
            output = self.engine.process(np.zeros((512, 2), np.float32))
            np.testing.assert_allclose(output, 0.2)
        fresh.reset.assert_called_once_with(48000)

    def test_pending_user_selection_survives_reload(self):
        self.audio.select_effect(None)
        self.plugins.reload()
        self.sync()
        source = np.full((512, 2), 0.3, np.float32)
        np.testing.assert_array_equal(self.engine.process(source), source)

    def test_reload_while_capture_stopped_does_not_activate_devices(self):
        self.path.write_text(PLUGIN.replace('VALUE', '200'))
        self.plugins.reload()
        fresh = Container.resolve_all(IEffectStrategy)[-1]
        fresh.activate = Mock()
        self.sync()
        self.processor._lifecycle.submit(lambda: None).result(timeout=2)
        fresh.activate.assert_not_called()
        self.assertIs(self.processor.current_strategy, fresh)


class DeviceRecoveryTests(unittest.TestCase):
    def test_device_selection_retries_failed_start(self):
        loop = video.SoundDeviceLoopService(video.EventBus())
        device = dict(name='Mock', max_input_channels=2, max_output_channels=2,
                      default_samplerate=48000)
        stream = Mock()
        with patch.object(video.sd, 'query_devices', side_effect=RuntimeError('Unavailable')) as query, \
                patch.object(video.sd, 'check_input_settings'), \
                patch.object(video.sd, 'check_output_settings'), \
                patch.object(video.sd, 'Stream', return_value=stream) as create:
            stage = Mock()
            loop.set_stage('effect', stage)
            self.assertIsNone(loop._stream)
            query.side_effect = None
            query.return_value = device
            loop.set_devices(1, 2)
            stream.start.assert_called_once()
            self.assertEqual(create.call_args.kwargs['device'], (1, 2))
            stage.prepare.assert_called_once_with(48000, 2)
            loop.set_stage('effect', None)
            stream.close.assert_called_once()

    def test_device_selection_without_stages_does_not_open_audio(self):
        loop = video.SoundDeviceLoopService(video.EventBus())
        with patch.object(video.sd, 'Stream') as create:
            loop.set_devices(1, 2)
            create.assert_not_called()

    def test_existing_stream_is_closed_before_restart(self):
        loop = video.SoundDeviceLoopService(video.EventBus())
        old = Mock()
        loop._stream = old
        loop._stages['effect'] = Mock()
        def check_closed():
            old.stop.assert_called_once()
            old.close.assert_called_once()
        with patch.object(loop, '_start', side_effect=check_closed) as start:
            loop.set_devices(1, 2)
            start.assert_called_once()


class CameraSourceTests(unittest.TestCase):
    def test_camera_frames_use_rgb_and_release_device(self):
        bgr = np.array([[[10, 20, 30]]], dtype=np.uint8)
        handle = Mock()
        handle.isOpened.return_value = True
        handle.read.return_value = (True, bgr)
        with patch.object(video.cv2, 'VideoCapture', return_value=handle):
            camera = video.OpenCvCameraCaptureService(0)
            camera.open()
            np.testing.assert_array_equal(camera.capture(), [[[30, 20, 10]]])
            camera.close()
        handle.release.assert_called_once()

    def test_camera_uses_existing_effect_pipeline(self):
        bus = video.EventBus()
        processor = video.EffectProcessor(Mock(), bus, video.VideoFeatures())
        self.addCleanup(processor._lifecycle.shutdown)
        processor.register_effect(video.PassThroughEffectStrategy())
        received = threading.Event()
        frames = []
        bus.subscribe('frame_processed', lambda frame: (frames.append(frame), received.set()))
        camera = Mock()
        camera.capture.return_value = np.full((24, 32, 3), 70, dtype=np.uint8)
        with patch.object(video, 'OpenCvCameraCaptureService', return_value=camera) as factory:
            processor.start_capture({'kind': 'camera', 'index': 2})
            self.assertTrue(received.wait(2.0))
            processor.stop_capture(quiet=True)
        factory.assert_called_once_with(2)
        camera.open.assert_called_once()
        camera.close.assert_called_once()
        self.assertEqual(processor.config['width'], 32)
        self.assertEqual(processor.config['height'], 24)
        np.testing.assert_array_equal(frames[0], camera.capture.return_value)


class ContourLabTests(unittest.TestCase):
    def test_low_contrast_eyes_and_mouth_produce_contours(self):
        path = Path(video.__file__).parent / 'plugins' / '35_video_contour_lab.py'
        spec = importlib.util.spec_from_file_location('contour_lab_test', path)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        frame = np.full((240, 320, 3), 110, dtype=np.uint8)
        for x in (120, 200):
            cv2.ellipse(frame, (x, 105), (15, 8), 0, 0, 360, (117, 117, 117), -1)
        cv2.ellipse(frame, (160, 165), (35, 13), 0, 0, 180, (117, 117, 117), 3)
        edges, *_ = module._analysis(frame)
        self.assertGreater(np.count_nonzero(edges[90:120, 95:145]), 0)
        self.assertGreater(np.count_nonzero(edges[90:120, 175:225]), 0)
        self.assertGreater(np.count_nonzero(edges[145:185, 120:200]), 0)


class LiquidMetalTests(unittest.TestCase):
    def test_settled_metal_has_moving_reflections_on_plain_background(self):
        path = Path(video.__file__).parent / 'plugins' / '36_video_liquid_metal.py'
        spec = importlib.util.spec_from_file_location('liquid_reflections_test', path)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        effect = module.Fluessigmetall()
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
        path = Path(video.__file__).parent / 'plugins' / '36_video_liquid_metal.py'
        spec = importlib.util.spec_from_file_location('liquid_async_test', path)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        old, new = Future(), Future()
        old.set_running_or_notify_cancel()
        first = np.full((160, 240, 3), 30, np.uint8)
        second = np.full_like(first, 180)
        effect = module.Fluessigmetall()
        with patch.object(module, 'submit_portrait', side_effect=[old, new]):
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
        path = Path(video.__file__).parent / 'plugins' / '36_video_liquid_metal.py'
        spec = importlib.util.spec_from_file_location('liquid_metal_test', path)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        effect = module.Fluessigmetall()
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
        effect.apply(second, {})
        self.assertFalse(np.array_equal(effect.scene, frozen))


class ShippedPluginTests(unittest.TestCase):
    def test_analog_looks_remove_hd_checker_detail(self):
        Container.reset()
        self.addCleanup(Container.reset)
        video.register_services()
        bus = video.EventBus()
        plugins = video.PluginManager(bus)
        with patch.object(video, 'plugin_directories',
                          return_value=[str(Path(video.__file__).parent / 'plugins')]):
            plugins.load()
        effects = {effect.name: effect for effect in Container.resolve_all(IEffectStrategy)}
        stripe = (np.arange(800) % 2 * 255).astype(np.uint8)
        frame = np.broadcast_to(stripe[None, :, None], (450, 800, 3)).copy()
        for name in ('Uralter Film (1910er)', 'VHS Störung', 'VHS Tracking-Riss',
                     'VHS Farbbluten', 'VHS Kopierkassette', 'VHS Pause'):
            with self.subTest(effect=name):
                output = effects[name].apply(frame, {})
                contrast = np.abs(output[:, 1:, 0].astype(np.int16) -
                                  output[:, :-1, 0].astype(np.int16)).mean()
                self.assertLess(contrast, 30)

    def test_all_plugins_process_frames_and_stereo_audio(self):
        Container.reset()
        self.addCleanup(Container.reset)
        video.register_services()
        bus = video.EventBus()
        plugins = video.PluginManager(bus)
        with patch.object(video, 'plugin_directories',
                          return_value=[str(Path(video.__file__).parent / 'plugins')]):
            plugins.load()
        self.assertEqual(plugins.errors, [])
        pictures = Container.resolve_all(IEffectStrategy)
        sounds = Container.resolve_all(IAudioEffect)
        self.assertEqual((len(pictures), len(sounds)), (39, 19))
        rng = np.random.default_rng(123)
        for effect in pictures:
            with self.subTest(picture=effect.name):
                config = {}
                for h, w in ((16, 16), (90, 160), (17, 31)):
                    frame = rng.integers(0, 256, (h, w, 3), dtype=np.uint8)
                    output = effect.apply(frame, config)
                    self.assertEqual(output.shape, frame.shape)
                    self.assertEqual(output.dtype, np.uint8)
        engine = video.AudioEffectEngine(video.VideoFeatures())
        for effect in sounds:
            engine.register(effect)
            for rate in (44100, 48000):
                with self.subTest(sound=effect.name, sample_rate=rate):
                    engine.prepare(rate, 2)
                    engine.select(effect.name)
                    engine.mix = 0.35
                    for n in (128, 333, 512, 1024) * 4:
                        block = rng.normal(0, 0.05, (n, 2)).astype(np.float32)
                        output = engine.process(block)
                        self.assertEqual(output.shape, block.shape)
                        self.assertEqual(output.dtype, np.float32)
                        self.assertTrue(np.isfinite(output).all())


if __name__ == '__main__':
    unittest.main()
