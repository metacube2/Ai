"""Verwandlungen: gemeinsamer Kern von Flüssigmetall II und den Verwandlungs-Effekten.

Ablauf (Einfrieren, Warten auf eine Person, Analyse), Hintergrundspeicher,
Relief, Studio-Umgebung, Aufzeichnung und Kreislauf sowie die
Strömungs-Verwandlung mit Chrom-Oberfläche. Ursprünglich Flüssigmetall II:

- Form: Tiefenschätzung mit Depth Anything V2 (lokal), sonst geschätztes Relief.
- Bewegung: FLIP-Flüssigkeitssimulation (bildeffekte.liquid_sim) mit Schwerkraft,
  Inkompressibilität, Viskosität und Oberflächenspannung; Metall am Boden läuft in
  eine Bodenpfütze ab, deren Fläche aus dem abgeflossenen Volumen folgt.
- Hintergrund: Livebilder werden gemerkt, wo gerade niemand steht. Ist beim
  Anwählen niemand im Bild, wartet der Effekt, bis jemand hineintritt.
"""

import bisect
from types import SimpleNamespace
import threading
import time

import cv2
import numpy as np

from bildeffekte.api import IEffectStrategy
from bildeffekte.liquid_sim import LiquidSheet, SimulationProcess
from bildeffekte.portrait2 import submit_person_mask, submit_portrait

METAL_SECONDS = 1.5        # Übergang Foto → Chrom
MELT_START = 1.7           # ab hier verliert das Metall seinen Zusammenhalt
MELT_RAMP = 4.5            # Sekunden bis voll flüssig
LIVE_INTERVAL = .3         # Abstand der Personenmasken für den Hintergrundspeicher
HOLD_PUDDLE = 2.0          # Pause als Pfütze, bevor es rückwärts läuft
HOLD_START = 1.5           # Pause auf dem Startbild, bevor es wieder schmilzt
FORWARD_LIMIT = 16.0       # spätestens hier gilt die Schmelze als fertig


def smooth(value):
    value = np.clip(value, 0.0, 1.0)
    return value * value * (3 - 2 * value)



class BackgroundPlate:
    """Merkt sich Hintergrundpixel aus Livebildern, an denen niemand stand."""

    def __init__(self):
        self._snapshot = (None, None)

    def reset(self):
        self._snapshot = (None, None)

    def snapshot(self):
        """Bild und Konfidenz aus derselben Aktualisierung lesen."""
        return self._snapshot

    @property
    def image(self):
        return self._snapshot[0]

    @property
    def confidence(self):
        return self._snapshot[1]

    def update(self, frame, person, protect=None):
        """Läuft im Analyse-Thread. Neue Arrays werden am Ende atomar gesetzt.

        protect = (Maske, Standbild): Innerhalb der eingefrorenen Metallform nur
        Pixel lernen, die sich gegenüber dem Standbild deutlich geändert haben. So
        wird ein gehaltener Gegenstand, den die schnelle Personenmaske nicht zur
        Person zählt, nicht als Hintergrund gelernt.
        """
        frame = frame.astype(np.float32)
        # Sicherheitsabstand zur Person: Haare und Bewegungsunschärfe.
        margin = max(3, frame.shape[1] // 60)
        occupied = cv2.dilate((person > 40).astype(np.uint8), np.ones((margin, margin), np.uint8))
        free = occupied == 0
        if protect is not None and protect[1].shape == frame.shape:
            region, still = protect
            unchanged = np.abs(frame - still.astype(np.float32)).max(axis=2) < 35
            unchanged = cv2.dilate(unchanged.astype(np.uint8), np.ones((margin, margin), np.uint8)) > 0
            free &= ~(region & unchanged)
        image, confidence = self.snapshot()
        if image is None or image.shape != frame.shape:
            image = frame.copy()
            confidence = np.zeros(frame.shape[:2], np.float32)
        known = free & (confidence > .5)
        if known.sum() > frame.shape[0] * frame.shape[1] * .05:
            difference = np.abs(image - frame).mean(axis=2)[known]
            if float(np.median(difference)) > 22:
                # Kamera bewegt oder Licht stark verändert: neu anfangen.
                confidence = np.zeros_like(confidence)
        # Neu gesehene Pixel direkt übernehmen, bekannte gleitend mitteln.
        rate = np.where(free, np.where(confidence <= 0, 1.0, .25), 0).astype(np.float32)[..., None]
        image = image + (frame - image) * rate
        confidence = np.where(free, np.minimum(1, confidence + .34), confidence).astype(np.float32)
        self._snapshot = (image, confidence)

    def matches(self, scene, visible, snapshot=None):
        """Passt der Speicher zum eingefrorenen Bild (gleiche Kamera, gleiches Licht)?"""
        image, confidence = self.snapshot() if snapshot is None else snapshot
        if image is None or image.shape != scene.shape:
            return False
        known = visible & (confidence > .5)
        if known.sum() < 50:
            return False
        difference = np.abs(image[known] - scene[known].astype(np.float32)).mean(axis=1)
        return float(np.median(difference)) < 22


PLATE = BackgroundPlate()


def learn_plate(frame, person, protect=None):
    """Im Analyse-Thread: Speicher lernen; Rückgabe: steht jemand im Bild?"""
    if person is None:
        return None
    PLATE.update(frame, person, protect)
    return bool(np.count_nonzero(person > 127) > person.size * .03)


class Verwandlung(IEffectStrategy):
    """Gemeinsamer Ablauf aller Verwandlungen einer eingefrorenen Person.

    Einfrieren (oder warten, bis jemand ins Bild tritt), Personenmaske, Tiefe,
    Relief, Hintergrund hinter der Person, Studio-Umgebung, Aufzeichnung und
    Kreislauf vorwärts/rückwärts. Unterklassen liefern `_setup()` und
    `_render_live(elapsed)` (Bild in interner Auflösung).
    """

    category = "Verwandlungen"

    def __init__(self):
        self.scene = None
        self.size = None
        self.started = 0.0
        self._pending = None
        self._reset_requested = False
        self._ready = False
        self._protect = None
        self.mask = None
        self.segmentation = 'none'
        self.depth_source = 'none'
        self.watching = False
        self._person_seen = 0
        self._live = None
        self._live_time = 0.0
        self._plate_version = 0
        self._background_version = -1
        self._has_background = False
        self._history = []         # (Zeit, Zustand) des ersten Durchlaufs
        self._loop_end = None      # Dauer des ersten Durchlaufs, sobald er fertig ist

    def activate(self):
        # activate() läuft im Lifecycle-Thread: Renderzustand nur in apply() ersetzen.
        self._reset_requested = True

    def deactivate(self):
        self._reset_requested = True
        # Hintergrundarbeit nicht weiterlaufen lassen, wenn der Effekt abgewählt ist.
        self._on_stop()

    # --- Haken für Unterklassen ------------------------------------------------

    def _setup(self):
        """Nach der Analyse: eigene Vorbereitung (Simulation, Partikel …)."""

    def _prepared(self):
        return True

    def _on_stop(self):
        """Hintergrundarbeit beenden (Effekt abgewählt oder neu eingefroren)."""

    def _on_reset(self):
        """Zustand des vorigen Standbilds verwerfen."""

    def _render_live(self, elapsed):
        return self.scene

    # --- Ablauf ----------------------------------------------------------------

    def _internal_size(self, frame):
        size = frame.shape[1], frame.shape[0]
        scale = min(1.0, 512 / max(size), 400 / frame.shape[0])
        return max(1, round(size[0] * scale)), max(1, round(size[1] * scale))

    def _freeze(self, frame):
        self._on_stop()
        if self._pending is not None:
            self._pending.cancel()
        self.size = frame.shape[1], frame.shape[0]
        width, height = self._internal_size(frame)
        self.scene = cv2.resize(frame, (width, height), interpolation=cv2.INTER_AREA)
        self.mask = None
        self._has_background = False
        self._on_reset()
        self._history = []
        self._loop_end = None
        self._ready = False
        self._pending = None
        self.watching = False
        self.segmentation = 'none'
        # Sehr kleine Bilder eignen sich nicht für semantische Gesichtsanalyse.
        if min(width, height) >= 120 and max(width, height) >= 240:
            self._pending = submit_portrait(self.scene)
        else:
            self._prepare(None)

    def _learn_background(self, frame):
        """Personenmasken der Livebilder in den Hintergrundspeicher übernehmen."""
        if self._live is not None and self._live.done():
            try:
                present = self._live.result()
            except Exception:
                present = None
            self._live = None
            if present is not None:
                self._plate_version += 1
                self._person_seen = self._person_seen + 1 if present else 0
        now = time.monotonic()
        if self._live is None and now - self._live_time >= LIVE_INTERVAL:
            width, height = self._internal_size(frame)
            if min(width, height) >= 120:
                self._live_time = now
                small = cv2.resize(frame, (width, height), interpolation=cv2.INTER_AREA)
                protect = self._protect if self._ready and self.mask is not None else None
                self._live = submit_person_mask(small, lambda f, m: learn_plate(f, m, protect))

    # --- Vorbereitung ------------------------------------------------------------

    def _fallback_mask(self):
        h, w = self.scene.shape[:2]
        if min(h, w) < 24:
            return np.zeros((h, w), np.uint8)
        labels = np.zeros((h, w), np.uint8)
        margin = max(1, w // 20)
        try:
            cv2.grabCut(self.scene, labels, (margin, 1, w - margin * 2, h - 2),
                        np.zeros((1, 65)), np.zeros((1, 65)), 3, cv2.GC_INIT_WITH_RECT)
            return ((labels == cv2.GC_FGD) | (labels == cv2.GC_PR_FGD)).astype(np.uint8) * 255
        except cv2.error:
            return np.zeros((h, w), np.uint8)

    def _prepare(self, portrait):
        h, w = self.scene.shape[:2]
        if portrait is not None and portrait.get('mask') is None:
            # Vision sieht niemanden: Hintergrund lernen und auf eine Person warten.
            self.watching = True
            self._person_seen = 0
            return
        binary = portrait['mask'] if portrait is not None else self._fallback_mask()
        self.segmentation = 'Apple Vision' if portrait is not None else 'Kontrastkontur'
        self.mask = cv2.GaussianBlur(binary.astype(np.float32) / 255, (0, 0), 0.65)
        self._ready = True
        self.started = time.monotonic()
        solid = (self.mask > 0.45).astype(np.uint8)
        if solid.sum() < max(8, w * h * 0.015):
            self.mask.fill(0)
            return
        self.bounds = cv2.boundingRect(cv2.findNonZero(solid))
        self.x, self.y = np.meshgrid(np.arange(w, dtype=np.float32), np.arange(h, dtype=np.float32))
        self._solid = solid
        region = cv2.dilate(solid, np.ones((7, 7), np.uint8)) > 0
        self._protect = (region, self.scene.copy())
        self._build_relief(portrait, solid)
        self._build_background()
        self._make_environment()
        left, top, bw, bh = self.bounds
        self.floor_px = min(h, top + bh + 1)
        self._setup()

    def _build_relief(self, portrait, solid):
        h, w = self.scene.shape[:2]
        left, top, bw, bh = self.bounds
        distance = cv2.distanceTransform(np.pad(solid, 1), cv2.DIST_L2, 5)[1:-1, 1:-1]
        # Geglättet, sonst entsteht auf der Mittelachse ein scharfer Grat als Naht.
        distance = cv2.GaussianBlur(distance, (0, 0), max(1.0, bw * .03))
        rounding = np.sqrt(distance * max(4, bw * 0.10))
        learned = portrait.get('depth') if portrait is not None else None
        fw = min(bw * 0.60, bh * 0.42)
        if learned is not None and np.count_nonzero(solid) > 50:
            # Gelernte Tiefe: echte Form von Gesicht, Kragen und Falten. Nur innerhalb
            # der Person normiert; der Rand wird zusätzlich abgerundet, weil die
            # Oberfläche an der Silhouette von der Kamera wegdreht.
            values = learned[solid > 0]
            low, high = np.percentile(values, 3), np.percentile(values, 97)
            relative = np.clip((learned - low) / max(1e-6, high - low), 0, 1.2)
            self.depth = relative * bw * .20 + rounding * .45
            self.depth_source = 'Depth Anything V2'
        else:
            self.depth = self._estimated_relief(portrait, solid, rounding)
            self.depth_source = 'geschätzt'
        gray = cv2.cvtColor(self.scene, cv2.COLOR_RGB2GRAY).astype(np.float32) / 255
        smooth = cv2.GaussianBlur(gray, (0, 0), 2)
        self.depth += (smooth - cv2.GaussianBlur(smooth, (0, 0), 8)) * fw * .02
        rng = np.random.default_rng(1991)
        lumps = np.zeros((h, w), np.float32)
        for sigma, weight in ((max(2.0, bw * .02), .35), (max(4.0, bw * .05), 1.0)):
            # Senkrecht gestreckt: wirkt wie fließendes, nicht wie gehämmertes Metall.
            field = cv2.GaussianBlur(rng.standard_normal((h, w)).astype(np.float32), (0, 0),
                                     sigmaX=sigma, sigmaY=sigma * 1.8)
            lumps += field / (field.std() + 1e-6) * weight
        self.depth += lumps * fw * .004
        self.depth = cv2.GaussianBlur(np.maximum(self.depth, 0) * self.mask, (0, 0), 1.1).astype(np.float32)

    def _estimated_relief(self, portrait, solid, rounding):
        left, top, bw, bh = self.bounds
        depth = rounding.copy()
        face = portrait.get('face') if portrait is not None else None
        if face is None:
            upper = np.argwhere(solid[top:top + max(1, bh // 3)])
            cx = float(upper[:, 1].mean()) if len(upper) else left + bw / 2
            fw = min(bw * 0.60, bh * 0.42)
            face = (cx - fw / 2, top + bh * 0.04, fw, fw * 1.12)
        fx, fy, fw, fh = face
        # Ellipsoidischer Kopf plus Augenhöhlen, Nase und Mund.
        q = ((self.x - fx - fw * 0.5) / max(1, fw * 0.63)) ** 2
        q += ((self.y - fy - fh * 0.40) / max(1, fh * 0.78)) ** 2
        head = np.sqrt(np.maximum(0, 1 - q)) * fw * 0.40
        depth = np.maximum(depth * (1 - 0.4 * np.exp(-q * 2)), head)
        defaults = {'nose': (fx + fw * .5, fy + fh * .57),
                    'leftEye': (fx + fw * .30, fy + fh * .36),
                    'rightEye': (fx + fw * .70, fy + fh * .36),
                    'outerLips': (fx + fw * .5, fy + fh * .79)}
        features = portrait.get('features', {}) if portrait is not None else {}
        for key, amplitude, sx, sy in (('nose', .19, .11, .20), ('leftEye', -.07, .13, .07),
                                        ('rightEye', -.07, .13, .07), ('outerLips', -.035, .22, .04)):
            cx, cy = features.get(key, defaults[key])
            q = ((self.x - cx) / max(1, fw * sx)) ** 2 + ((self.y - cy) / max(1, fh * sy)) ** 2
            depth += fw * amplitude * np.exp(-q * .5)
        return depth

    def _build_background(self):
        """Hintergrund hinter der Person: gemerkte Livebilder, sonst Inpainting."""
        h, w = self.scene.shape[:2]
        if getattr(self, '_inpainted_for', None) is not self.scene:
            repair_mask = cv2.dilate(self._solid, np.ones((5, 5), np.uint8)) * 255
            size = (max(1, w // 2), max(1, h // 2))
            inpainted = cv2.inpaint(cv2.resize(self.scene, size), cv2.resize(repair_mask, size),
                                    4, cv2.INPAINT_TELEA)
            self._inpainted = cv2.resize(inpainted, (w, h)).astype(np.float32)
            self._repair_mask, self._inpainted_for = repair_mask, self.scene
        repair_mask, repaired = self._repair_mask, self._inpainted
        self.plate_coverage = 0.0
        snapshot = PLATE.snapshot()
        plate_image, plate_confidence = snapshot
        if PLATE.matches(self.scene, repair_mask == 0, snapshot=snapshot):
            known = np.clip((plate_confidence - .5) * 2, 0, 1)
            known = cv2.GaussianBlur(known, (0, 0), 1.5)[..., None]
            repaired = plate_image * known + repaired * (1 - known)
            hidden = repair_mask > 0
            self.plate_coverage = float(known[..., 0][hidden].mean()) if hidden.any() else 1.0
        replace = cv2.GaussianBlur(repair_mask, (0, 0), 1).astype(np.float32)[..., None] / 255
        self.background = self.scene * (1 - replace) + repaired * replace
        self._background32 = self.background.astype(np.float32)
        self._background_version = self._plate_version
        self._background_time = time.monotonic()
        self._has_background = True

    def _make_environment(self):
        # Wie bei Chromfiguren im Film: überwiegend dunkle, kühle Umgebung,
        # dazwischen wenige sehr helle, hart begrenzte Lichtflächen. Erst dieser
        # Kontrast lässt Metall als Metall erscheinen.
        room = cv2.resize(self.background.astype(np.float32), (512, 256))
        room = cv2.GaussianBlur(room, (0, 0), .8)
        grey = room.mean(axis=2, keepdims=True)
        room = np.clip((grey + (room - grey) * .45 - 118) * 1.25 + 100, 0, 255)
        self.reflected_room = room.copy()
        v = np.linspace(0, 1, 256, dtype=np.float32)[:, None, None]
        sky = np.float32((78, 90, 108)) * (1 - v * 1.6)[..., :1] + np.float32((26, 30, 38))
        floor = np.float32((34, 34, 36)) * (1.4 - v)
        env = np.where(v < .47, sky, floor) * np.ones((1, 512, 1), np.float32)
        env[117:127] = (6, 7, 10)
        # Zur Kamera gewandte Flächen spiegeln den Raum hinter der Kamera, nicht die
        # Wand hinter der Person. Der ist unbekannt: Studio mit Lichtflächen, das
        # Bild selbst nur als leichte Färbung.
        env = env * .88 + room * .12
        for x0, y0, x1, y1, level in ((180, 14, 226, 150, 420), (296, 22, 334, 118, 360),
                                      (60, 30, 120, 58, 330), (400, 18, 470, 44, 300),
                                      (238, 0, 290, 12, 380), (452, 70, 462, 150, 280),
                                      (20, 150, 90, 160, 170)):
            # Softbox mit Abfall zur Kante: Verlauf innerhalb des Reflexes.
            yy, xx = np.mgrid[y0:y1 + 1, x0:x1 + 1].astype(np.float32)
            fall = (1 - np.abs((xx - (x0 + x1) / 2) / max(1, (x1 - x0) / 2)) ** 2 * .45)
            fall *= (1 - (yy - y0) / max(1, y1 - y0) * .35)
            env[y0:y1 + 1, x0:x1 + 1] = fall[..., None] * np.float32((level * .96, level, level * 1.04))
        # Scharfe Kanten plus weicher Lichthof, damit Flächen nicht wie Poster wirken.
        self.environment = (cv2.GaussianBlur(env, (0, 0), .7) * .7 +
                            cv2.GaussianBlur(env, (0, 0), 9) * .3).astype(np.float32)

    # --- Vorwärts und rückwärts ----------------------------------------------------

    def _record(self, snapshot):
        """Zustände der ersten Schmelze merken: sie werden später rückwärts abgespielt."""
        if not self._history or snapshot[0] > self._history[-1][0] + 1e-6:
            self._history.append((snapshot[0], snapshot))

    def _at(self, t):
        """Aufgezeichneter Zustand zur Zeit t (der letzte davor)."""
        index = bisect.bisect_right([entry[0] for entry in self._history], t) - 1
        return self._history[max(0, index)][1]

    hold_end = HOLD_PUDDLE     # Pause am Ende, bevor es rückwärts läuft
    hold_start = HOLD_START    # Pause auf dem Startbild

    def _timeline(self, elapsed):
        """Wiedergabezeit im Kreislauf: Ende → rückwärts → Startbild → vorwärts."""
        end = self._loop_end
        period = 2 * end + self.hold_end + self.hold_start
        phase = (elapsed - end) % period
        if phase < self.hold_end:
            return end
        phase -= self.hold_end
        if phase < end:
            return end - phase                      # rückwärts
        phase -= end
        if phase < self.hold_start:
            return 0.0
        return phase - self.hold_start              # wieder vorwärts

    def apply(self, frame, config):
        if self._reset_requested:
            self._reset_requested = False
            self._on_stop()
            self.scene = None
        self._learn_background(frame)
        self._current_frame = frame          # für Effekte, die das Livebild brauchen
        if self.scene is None or self.size != (frame.shape[1], frame.shape[0]):
            self._freeze(frame)
        if self._pending is not None and self._pending.done():
            try:
                portrait = self._pending.result()
            except Exception:
                portrait = None
            self._pending = None
            self._prepare(portrait)
        if self.watching:
            # Niemand im Bild: live zeigen, Hintergrund lernen, auf Person warten.
            if self._person_seen >= 2:
                self._freeze(frame)
                return frame
            return self._hint(frame)
        if (self._ready and self._has_background and self._prepared()
                and self._background_version != self._plate_version
                and time.monotonic() - self._background_time > 1.0):
            # Neu aufgedeckter Hintergrund (Person hat sich bewegt) sofort nutzen.
            self._build_background()
        if self._ready:
            result = self._render_live(max(0, time.monotonic() - self.started))
        else:
            result = self.scene
        return cv2.resize(result, self.size, interpolation=cv2.INTER_LINEAR)

    @staticmethod
    def _hint(frame):
        out = frame.copy()
        scale = max(.4, frame.shape[1] / 1600)
        text = "Hintergrund wird gemerkt - jetzt ins Bild treten"
        org = (int(20 * scale * 2), int(frame.shape[0] - 30 * scale * 2))
        cv2.putText(out, text, org, cv2.FONT_HERSHEY_SIMPLEX, scale, (0, 0, 0), int(4 * scale + 2), cv2.LINE_AA)
        cv2.putText(out, text, org, cv2.FONT_HERSHEY_SIMPLEX, scale, (255, 255, 255), int(2 * scale + 1), cv2.LINE_AA)
        return out


class FluidVerwandlung(Verwandlung):
    """Verwandlung mit FLIP-Strömung in eine Bodenpfütze und Chrom-Oberfläche."""

    # Strömung: Parameter für LiquidSheet (leer = Flüssigmetall) und Schmelzverlauf.
    fluid = {}
    melt_start = MELT_START
    melt_ramp = MELT_RAMP

    def __init__(self):
        super().__init__()
        self.sim = None
        self._sim_thread = None
        self._sim_stop = None
        self._sim_lock = threading.Lock()
        self._use_process = True

    def _softness_at(self, t):
        return float(smooth((t - self.melt_start) / self.melt_ramp))

    def _setup(self):
        h = self.scene.shape[0]
        left, top, bw, bh = self.bounds
        cell = max(3.0, h / 64)
        self._sim_args = (self.mask, cell, self.floor_px, bh)
        self.sim = LiquidSheet(*self._sim_args, **self.fluid)

    def _prepared(self):
        return self.sim is not None

    def _on_stop(self):
        self._stop_sim()

    def _on_reset(self):
        self.sim = None

    # --- Simulation ------------------------------------------------------------

    def _stop_sim(self):
        with self._sim_lock:
            self._stop_sim_locked()

    def _stop_sim_locked(self):
        if self._sim_thread is not None:
            if isinstance(self._sim_thread, SimulationProcess):
                self._sim_thread.stop()
            else:
                self._sim_stop.set()
                self._sim_thread.join(timeout=1.0)
        self._sim_thread = None
        self._sim_stop = None

    def _start_sim(self):
        if self._sim_thread is not None or self.sim is None:
            return
        if self._use_process:
            try:
                # Nebenprozess: eigener Prozessorkern, bremst das Rendern nicht.
                self._sim_thread = SimulationProcess(self._sim_args, self.started, self.melt_start,
                                                     self.melt_ramp, self.fluid)
                return
            except Exception:
                self._sim_thread = None
                self._use_process = False
        # Ersatz ohne Mehrprozessbetrieb: Thread.
        self._sim_stop = threading.Event()
        sim, stop, started = self.sim, self._sim_stop, self.started

        def run():
            # Läuft der Echtzeit nach, nie voraus: die Anzeige zeigt stets den
            # neuesten fertigen Zustand.
            while not stop.is_set():
                target = time.monotonic() - started
                if sim.time < target - 1e-3:
                    sim.advance(min(target, sim.time + .1), self._softness_at, stop=stop)
                else:
                    stop.wait(.004)

        self._sim_thread = threading.Thread(target=run, name='liquid-metal-sim', daemon=True)
        self._sim_thread.start()

    def _snapshot(self, elapsed, live):
        if live:
            with self._sim_lock:
                self._start_sim()
                runner = self._sim_thread
                latest = runner.latest() if isinstance(runner, SimulationProcess) else None
                if isinstance(runner, SimulationProcess) and not runner.alive() and \
                        (latest is None or len(latest[1])):
                    # Prozess vorzeitig beendet (z. B. Start nicht möglich): im
                    # Thread weiterrechnen, ab dem letzten bekannten Stand der Zeit.
                    runner.stop()
                    self._sim_thread, self._use_process = None, False
                    self._start_sim()
                    runner, latest = self._sim_thread, None
            if isinstance(runner, SimulationProcess):
                # Bis der Prozess läuft (Start ≈ 1 s), zeigt der Anfangszustand
                # das noch feste Metall.
                snapshot = latest if latest is not None else self.sim.snapshot
            else:
                snapshot = self.sim.snapshot
            self._record(snapshot)
            return snapshot
        # Direkter Aufruf (Tests, Export): synchron und reproduzierbar rechnen.
        self._stop_sim()
        if self.sim.time > elapsed + 1e-6:
            left, top, bw, bh = self.bounds
            self.sim = LiquidSheet(self.mask, self.sim.cell_size, self.floor_px, bh, **self.fluid)
        self.sim.advance(elapsed, self._softness_at)
        return self.sim.snapshot

    def _pool_geometry(self, snapshot):
        """Bodenpfütze aus dem abgeflossenen Volumen (Fläche = Volumen / Höhe)."""
        h, w = self.scene.shape[:2]
        left, top, bw, bh = self.bounds
        count, x_sum, inflow = snapshot[5], snapshot[6], snapshot[7]
        sim = self.sim
        fraction = count / max(1, sim.initial_count)
        center = (x_sum / count * sim.sx) if count else left + bw * .5
        # Jedes Partikel steht für ein Viertel Zelle mal Körperdicke.
        volume = count * sim.sx * sim.sy / 4 * bw * .30
        # Flüssigmetall bildet wegen hoher Oberflächenspannung dicke Lachen.
        height = max(2.0, bh * .06)
        radius = float(np.sqrt(volume / height / np.pi))
        rx = max(2.0, min(w * .48, radius))
        ry = max(2.0, rx * .22)          # Bodenebene in Kameraperspektive
        floor = min(self.floor_px, h - 2) - ry * .7
        settling = .32 + .68 * float(np.clip(inflow / max(1, sim.initial_count * .15), 0, 1))
        return fraction, center, floor, rx, ry, height, settling

    def _render_live(self, elapsed):
        # Ohne erkannte Form gibt es keine Simulation und keine Aufzeichnung.
        if self.sim is None or not np.any(self.mask):
            return self.scene
        if self._loop_end is None:
            result = self._render(elapsed, live=True)
            last = self._history[-1][1] if self._history else None
            done = last is not None and len(last[1]) == 0 and last[0] > self.melt_start
            if done or elapsed > FORWARD_LIMIT:
                # Erste Schmelze fertig: ab jetzt nur noch Aufzeichnung abspielen.
                self._loop_end = self._history[-1][0] if self._history else elapsed
                self._stop_sim()
            return result
        t = self._timeline(elapsed)
        return self._render(t, snapshot=self._at(t))

    def _render(self, elapsed, live=False, snapshot=None):
        h, w = self.scene.shape[:2]
        if not np.any(self.mask) or self.sim is None:
            return self.scene
        metal_mix = float(smooth(elapsed / METAL_SECONDS))
        if snapshot is None:
            snapshot = self._snapshot(elapsed, live)
        density, map_x, map_y = self.sim.fields((w, h), snapshot)
        # Für Materialien mit mitfließender Textur (z. B. Lavarisse).
        self._frame_maps = (map_x, map_y)
        coverage = np.clip(density * 2.5, 0, 1)
        # Maske über die Herkunft zurückverfolgt: scharfe Kante, fließt mit.
        alpha = cv2.remap(self.mask, map_x, map_y, cv2.INTER_LINEAR,
                          borderMode=cv2.BORDER_CONSTANT) * coverage
        depth = cv2.remap(self.depth, map_x, map_y, cv2.INTER_LINEAR, borderMode=cv2.BORDER_CONSTANT)
        warped = cv2.remap(self.scene, map_x, map_y, cv2.INTER_LINEAR, borderMode=cv2.BORDER_REPLICATE)

        fraction, center, floor, rx, ry, height, settling = self._pool_geometry(snapshot)
        pool_on = fraction > .002
        q = ((self.x - center) / rx) ** 2 + ((self.y - floor) / ry) ** 2
        if pool_on:
            pool_depth = np.sqrt(np.maximum(0, 1 - q)) * (height * 1.2)
            pool_alpha = np.clip((1 - q) * min(rx, ry) * .6, 0, 1) * min(1, fraction * 40)
        else:
            pool_depth = pool_alpha = np.zeros_like(q)
        depth = np.maximum(depth, pool_depth)
        alpha = np.maximum(alpha, pool_alpha)
        pool_weight = np.clip((pool_depth - depth + 1.5) / 1.5, 0, 1) * pool_alpha
        # Schattierung nur im Bereich des Metalls rechnen (plus Rand für die Filter).
        box = cv2.boundingRect(cv2.findNonZero((alpha > .003).astype(np.uint8)))
        chrome = np.zeros((h, w, 3), np.float32)
        if box[2] and box[3]:
            y0, x0 = max(0, box[1] - 6), max(0, box[0] - 6)
            y1, x1 = min(h, box[1] + box[3] + 6), min(w, box[0] + box[2] + 6)
            crop = (slice(y0, y1), slice(x0, x1))
            chrome[crop] = self._shade(crop, elapsed, settling, pool_on, depth[crop],
                                       pool_weight[crop], q[crop], center, floor, ry)
        amount = min(1.0, fraction * 3)
        # Schatten sind weich: in Viertelauflösung rechnen spart viel Zeit.
        small = (max(1, w // 4), max(1, h // 4))
        soft = lambda f, sigma: cv2.resize(cv2.GaussianBlur(cv2.resize(f, small, interpolation=cv2.INTER_AREA),
                                                            (0, 0), max(.5, sigma / 4)), (w, h))
        shadow = soft(alpha, 3) * .16 * amount
        if pool_on:
            # Kontaktschatten: dunkler Saum, wo das Metall auf dem Boden aufliegt.
            shadow += soft(pool_alpha, max(1.0, ry * .5)) * .45 * amount
        background = self.background * (1 - shadow[..., None])
        object_rgb = warped * (1 - metal_mix) + chrome * metal_mix
        result = background * (1 - alpha[..., None]) + object_rgb * alpha[..., None]
        result = self._finish(result, alpha, elapsed, metal_mix)
        # Zu Beginn wirklich das unveränderte eingefrorene Bild zeigen.
        reveal = float(smooth(elapsed / .35))
        result = self.scene * (1 - reveal) + result * reveal
        return np.clip(result, 0, 255).astype(np.uint8)

    def _shade(self, crop, elapsed, settling, pool_on, depth, pool_weight, q, center, floor, ry):
        h, w = self.scene.shape[:2]
        X, Y = self.x[crop], self.y[crop]
        radius = np.sqrt(((X - center) * .55) ** 2 + ((Y - floor) * 1.5) ** 2 + 1)
        ripples = (.70 * np.sin(radius * .19 - elapsed * 2.2) +
                   .28 * np.sin(X * .13 + Y * .16 + elapsed * 1.3))
        depth += ripples * pool_weight * settling
        depth = cv2.GaussianBlur(depth.astype(np.float32), (0, 0), .85)
        nx = -cv2.Sobel(depth, cv2.CV_32F, 1, 0, ksize=3) * .125
        ny = -cv2.Sobel(depth, cv2.CV_32F, 0, 1, ksize=3) * .125
        wave_x, wave_y = nx.copy(), ny.copy()
        # Die Pfütze liegt auf einer Bodenebene; ihre Normale zeigt schräg nach oben.
        ny -= pool_weight * 1.15
        norm = np.sqrt(nx * nx + ny * ny + 1)
        nx /= norm
        ny /= norm
        nz = 1 / norm
        # Spiegelrichtung aus den Oberflächennormalen.
        rxv, ryv, rzv = 2 * nz * nx, 2 * nz * ny, 2 * nz * nz - 1
        surface = SimpleNamespace(crop=crop, X=X, Y=Y, nx=nx, ny=ny, nz=nz, rxv=rxv, ryv=ryv,
                                  rzv=rzv, wave_x=wave_x, wave_y=wave_y, depth=depth,
                                  pool_weight=pool_weight, q=q, pool_on=pool_on,
                                  center=center, floor=floor, ry=ry, elapsed=elapsed)
        return self._surface(surface)

    def _finish(self, result, alpha, elapsed, mix):
        """Nachbearbeitung des fertigen Bildes (z. B. Leuchten); Standard: nichts."""
        return result

    def _reflection(self, s):
        """Spiegelung der Umgebung samt Rand-Spiegelung und Glanzlichtern (ungegradet)."""
        h, w = self.scene.shape[:2]
        X, Y, nx, ny, nz, rxv, ryv, rzv = s.X, s.Y, s.nx, s.ny, s.nz, s.rxv, s.ryv, s.rzv
        u = (np.arctan2(rxv, rzv) / (2 * np.pi) + .5) * 511
        v = (np.arccos(np.clip(-ryv, -1, 1)) / np.pi) * 255
        chrome = cv2.remap(self.environment, u, v, cv2.INTER_LINEAR, borderMode=cv2.BORDER_WRAP)
        # Näherung der räumlichen Umgebungsspiegelung mit Parallaxe. So spiegelt
        # eine breite flache Fläche nicht überall denselben einzelnen Bildpunkt.
        room_x = (X / max(1, w - 1) * 511 + rxv * 145).astype(np.float32)
        room_y = (255 - Y / max(1, h - 1) * 170 + ryv * 95).astype(np.float32)
        reflected = cv2.remap(self.reflected_room, room_x, room_y, cv2.INTER_LINEAR,
                              borderMode=cv2.BORDER_REFLECT)
        # Die Wand hinter der Person erscheint nur bei streifendem Blick, am Rand.
        rim = np.clip((1 - nz) * 2.2, 0, 1)[..., None]
        chrome = chrome * (1 - .5 * rim) + reflected * (.5 * rim)
        # Kleine gerichtete Lichtquellen erzeugen scharfe, wandernde Glanzlichter.
        light_a = np.maximum(0, -.38 * nx - .42 * ny + .824 * nz)
        light_b = np.maximum(0, .55 * nx - .22 * ny + .805 * nz)
        specular = 190 * light_a ** 70 + 115 * light_b ** 110
        chrome += specular[..., None]
        # Streifender Blick: Metall spiegelt fast vollständig, Silhouette leuchtet.
        chrome *= (.78 + .55 * (1 - nz) ** 2.5)[..., None]
        return chrome, specular

    def _surface(self, s):
        """Material der Oberfläche; Standard: Chrom mit spiegelnder Pfütze."""
        chrome, specular = self._reflection(s)
        chrome = self._grade(chrome)
        if s.pool_on:
            chrome = self._pool_mirror(s.X, s.Y, chrome, specular, s.pool_weight, s.q, s.wave_x,
                                       s.wave_y, s.center, s.floor - s.ry, s.ry)
        return chrome

    @staticmethod
    def _grade(chrome):
        # Stahl: Tiefen kühl und satt dunkel, Lichter neutral mit weichem Rolloff.
        luma = chrome.mean(axis=2, keepdims=True)
        cool = np.clip(1 - luma / 150, 0, 1)
        chrome = chrome * (1 - cool * np.float32((.22, .12, -.04)))
        chrome = np.maximum(chrome - 10, 0) * 1.12
        return np.where(chrome > 205, 205 + 50 * (1 - np.exp(-(chrome - 205) / 80)), chrome)

    def _pool_mirror(self, X, Y, chrome, specular, pool_weight, q, wave_x, wave_y, center, pool_top, ry):
        # Die Pfütze ist ein liegender Spiegel: Sie zeigt die Szene darüber
        # kopfüber. Wellen verzerren das Bild, der gewölbte Rand staucht es.
        edge = np.clip(q, 0, 1)
        # Vorn schaut die Kamera steiler auf die Pfütze: Die Spiegelung steigt über
        # die Wand hinaus bis zur Decke. Hinten spiegelt sich die Wand selbst.
        near = np.clip((Y - pool_top) / max(1.0, 2 * ry), 0, 1)
        mirror_y = pool_top - 1 - near ** .8 * pool_top * 1.9 + wave_y * 90
        mirror_x = X + (X - center) * .30 * edge + wave_x * 70
        mirrored = cv2.remap(self._background32, mirror_x.astype(np.float32),
                             mirror_y.astype(np.float32), cv2.INTER_LINEAR,
                             borderMode=cv2.BORDER_REFLECT)
        # Über dem Bildrand: Decke und Lampen aus der Studio-Umgebung.
        above = np.clip(-mirror_y / max(1.0, pool_top), 0, 1)
        ceiling = cv2.remap(self.environment,
                            (X / max(1, X.max()) * 511 + wave_x * 60).astype(np.float32),
                            (70 - above * 66).astype(np.float32), cv2.INTER_LINEAR,
                            borderMode=cv2.BORDER_WRAP)
        mirrored = mirrored * (1 - np.clip(above * 4, 0, 1))[..., None] + \
            ceiling * np.clip(above * 4, 0, 1)[..., None]
        # Silber färbt kaum, schluckt aber Licht: leicht kühl, etwas entsättigt,
        # dunkle Bereiche bleiben dunkel statt zu Grau aufzuhellen.
        grey = mirrored.mean(axis=2, keepdims=True)
        mirrored = (grey + (mirrored - grey) * .8) * np.float32((.80, .84, .90))
        mirrored = np.clip((mirrored - 100) * 1.25 + 95, 0, 255)
        # Am gewölbten Rand spiegelt sich die dunkle Umgebung knapp über dem Boden.
        mirrored *= (1 - .55 * edge ** 4)[..., None]
        # Über der Wand liegen Decke und Lampen: Anteil der Umgebungsspiegelung, damit
        # die Pfütze vor einer einfarbigen Wand nicht wieder einfarbig wird.
        pool = mirrored * .80 + chrome * .20 + specular[..., None] * .5
        weight = pool_weight[..., None]
        return chrome * (1 - weight) + pool * weight
