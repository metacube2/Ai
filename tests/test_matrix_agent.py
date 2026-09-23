"""Face-morph geometry, asynchronous tracking and plugin integration."""
import importlib.util
from concurrent.futures import Future
from pathlib import Path
from unittest.mock import patch
import unittest
import tempfile
import cv2
import numpy as np
from bildeffekte.face_morph import face_points, track_points

SPEC = importlib.util.spec_from_file_location('matrix_agent_test',Path(__file__).resolve().parents[1]/'plugins/51_video_matrix_agent.py')
module = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(module)


class MatrixAgentTests(unittest.TestCase):
    def setUp(self):
        self.effect = module.MatrixAgent()
        self.frame = np.random.default_rng(8).integers(40,180,(240,320,3),dtype=np.uint8)
        self.points = face_points((100,50,100,140),{})

    def test_identity_geometry_and_background(self):
        np.testing.assert_array_equal(self.effect.render(self.frame,self.points,0),self.frame)
        before = self.frame.copy()
        for p in (.3,.65,1):
            out = self.effect.render(self.frame,self.points,p)
            self.assertEqual(out.shape,self.frame.shape)
            self.assertEqual(out.dtype,np.uint8)
            np.testing.assert_array_equal(out[:,:60],before[:,:60])
            self.assertGreater(np.abs(out.astype(float)-before).mean(),1)
        np.testing.assert_array_equal(self.frame,before)

    def test_clipped_face_is_safe(self):
        for offset in ((-130,-80),(200,100)):
            out = self.effect.render(self.frame,self.points+np.float32(offset),.6)
            self.assertEqual(out.shape,self.frame.shape)

    def test_no_face_and_restart_discard_stale_results(self):
        old,new = Future(),Future()
        old.set_running_or_notify_cancel()
        with patch.object(module,'submit_face',side_effect=[old,new]):
            np.testing.assert_array_equal(self.effect.apply(self.frame,{}),self.frame)
            self.effect.activate()
            old.set_result(self.points)
            np.testing.assert_array_equal(self.effect.apply(self.frame,{}),self.frame)
            self.assertIs(self.effect._pending,new)
            self.assertIsNone(self.effect._points)

    def test_drama_mode_is_stronger_and_keeps_background(self):
        before = self.frame.copy()
        for p in (.3,.5,.7):
            calm = self.effect.render(self.frame,self.points,p).astype(float)
            drama = self.effect.render(self.frame,self.points,p,drama=True)
            self.assertEqual(drama.shape,self.frame.shape)
            np.testing.assert_array_equal(drama[:,:20],before[:,:20])
            self.assertGreater(np.abs(drama-before.astype(float)).mean(),np.abs(calm-before).mean())
        np.testing.assert_array_equal(self.effect.render(self.frame,self.points,0,drama=True),self.frame)
        self.assertLess(self.effect._progress(3.0,True),self.effect._progress(3.0))   # längere Übernahme

    def test_drama_option_restarts_loop(self):
        self.effect._started = 1.0
        self.effect.apply(self.frame,{'effect_options': {self.effect.name: {'drama': True}}})
        self.assertTrue(self.effect._drama)
        self.assertIsNone(self.effect._started)

    def test_optical_flow_follows_translation(self):
        gray = cv2.cvtColor(self.frame,cv2.COLOR_RGB2GRAY)
        moved = cv2.warpAffine(gray,np.float32([[1,0,4],[0,1,3]]),(320,240))
        points = track_points(gray,moved,self.points)
        self.assertIsNotNone(points)
        np.testing.assert_allclose(np.median(points-self.points,axis=0),[4,3],atol=.7)


class TargetImageTests(unittest.TestCase):
    def test_jpg_load_and_default_restore(self):
        effect = module.MatrixAgent()
        original = effect.agent
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder)/'Gesicht mit Leerzeichen.jpg'
            rgb = np.full((200,300,3),(170,90,40),np.uint8)
            cv2.imwrite(str(path),cv2.cvtColor(rgb,cv2.COLOR_RGB2BGR))
            points = face_points((90,30,100,140),{})
            with patch.object(module,'detect_face',return_value=points):
                selected = effect.prepare_target_image(path)
            np.testing.assert_allclose(selected[0][0,0],rgb[0,0],atol=2)
            frame = np.zeros((32,32,3),np.uint8)
            effect.apply(frame,{'target_images':{effect.name:selected}})
            self.assertIs(effect.agent,selected[0])
            effect.apply(frame,{'target_images':{}})
            self.assertIs(effect.agent,original)

    def test_no_face_rejected_without_replacing_target(self):
        effect = module.MatrixAgent()
        original = effect.agent
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder)/'leer.jpg'
            cv2.imwrite(str(path),np.zeros((120,160,3),np.uint8))
            with patch.object(module,'detect_face',return_value=None):
                with self.assertRaisesRegex(ValueError,'Kein frontales Gesicht'):
                    effect.prepare_target_image(path)
        self.assertIs(effect.agent,original)
