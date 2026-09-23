"""Besondere Verwandlungen: aus der Pfütze auftauchen, zu Asche zerfallen,
vereisen und zerspringen, Formwandler, Predator-Tarnung.

Alle nutzen den gemeinsamen Kern bildeffekte/verwandlung.py (Einfrieren, Warten
auf eine Person, Tiefe, gemerkter Hintergrund, Kreislauf vorwärts/rückwärts).
Die Predator-Tarnung arbeitet als einzige live, ohne einzufrieren.
"""

import time

import cv2
import numpy as np

from bildeffekte import IEffectStrategy, verwandlung
from bildeffekte.liquid_sim import PrecomputeProcess, precompute_in_thread
from bildeffekte.verwandlung import FORWARD_LIMIT, FluidVerwandlung, Verwandlung, smooth

_BEST = (" Beste Wirkung: kurz aus dem Bild gehen, Effekt wählen, dann ins Bild treten "
         "(dann ist der Hintergrund hinter dir bekannt).")


class AusDerPfuetze(FluidVerwandlung):
    name = "Aus der Pfütze"
    description = ("Wie beim T-1000: Am Boden liegt eine Metallpfütze, aus der sich die Person "
                   "formt – kurz stehen, wieder zerfließen, erneut auftauchen." + _BEST)

    hold_end = 1.0             # kurz als Pfütze liegen, dann auftauchen

    def __init__(self):
        super().__init__()
        self._job = None
        self._offset = 0.0

    def _setup(self):
        super()._setup()
        self._start_job()

    def _start_job(self, use_process=True):
        args = (self._sim_args, self.melt_start, self.melt_ramp, self.fluid)
        if use_process:
            try:
                self._job = PrecomputeProcess(*args, limit=FORWARD_LIMIT)
                return
            except Exception:
                pass
        self._job = precompute_in_thread(*args, limit=FORWARD_LIMIT)

    def _on_stop(self):
        super()._on_stop()
        if self._job is not None:
            self._job.stop()
            self._job = None

    def _puddle_snapshot(self):
        """Zustand „alles liegt in der Pfütze“ – steht schon vor der Berechnung fest."""
        sim, empty = self.sim, np.zeros(0, np.float32)
        return (0.0, empty, empty, empty, empty, sim.initial_count, float(sim.x.sum()), 0.0)

    def _render_live(self, elapsed):
        if self.sim is None:                 # niemand erkannt: Standbild
            return self.scene
        if self._loop_end is None:
            job = self._job
            if job is not None:
                job.collect()
                if not job.alive():
                    # Nebenprozess nicht verfügbar: im Thread rechnen.
                    job.stop()
                    self._start_job(use_process=False)
                elif job.done and job.snapshots:
                    # Das Ende der Schmelze ist fast Stillstand (letzte Tropfen);
                    # rückwärts würde die Pfütze lange nur daliegen. Abschneiden,
                    # sobald weniger als 1,5 % des Metalls übrig sind.
                    limit = self.sim.initial_count * .015
                    snaps = job.snapshots
                    cut = next((i for i, snap in enumerate(snaps)
                                if snap[0] > self.melt_start and len(snap[1]) < limit), len(snaps) - 1)
                    self._history = [(snap[0], snap) for snap in snaps[:cut + 1]]
                    self._loop_end = self._history[-1][0]
                    self._offset = elapsed
                    job.stop()
                    self._job = None
            if self._loop_end is None:
                # Bis die Schmelze vorausberechnet ist, liegt schon die Pfütze da.
                puddle = self._render(FORWARD_LIMIT + elapsed, snapshot=self._puddle_snapshot())
                fade = float(smooth(elapsed / .6))
                return (self.scene * (1 - fade) + puddle * fade).astype(np.uint8)
        # Kreislauf ab der Pfütze: auftauchen (rückwärts) → stehen → zerfließen.
        t = self._timeline(self._loop_end + elapsed - self._offset)
        return self._render(t, snapshot=self._at(t))


def _splat_points(x, y, colour, weight, shape, sigma=.6):
    """Punkte (Farbe × Gewicht) aufs Bild streuen, weich ausgeblendet."""
    h, w = shape
    inside = (x >= 0) & (x < w - 1) & (y >= 0) & (y < h - 1) & (weight > .003)
    x, y, colour, weight = x[inside], y[inside], colour[inside], weight[inside]
    index = y.astype(np.int32) * w + x.astype(np.int32)
    cover = np.bincount(index, weight, h * w).reshape(h, w).astype(np.float32)
    rgb = np.dstack([np.bincount(index, weight * colour[:, c], h * w).reshape(h, w)
                     for c in range(3)]).astype(np.float32)
    if sigma > 0:
        cover = cv2.GaussianBlur(cover, (0, 0), sigma)
        rgb = cv2.GaussianBlur(rgb, (0, 0), sigma)
    return rgb, cover


class ZerfallZuAsche(Verwandlung):
    name = "Zerfall zu Asche"
    description = ("Die Person zerfällt von einer Seite her zu Asche, die der Wind davonträgt – "
                   "danach setzt sie sich rückwärts wieder zusammen." + _BEST)

    START = .8          # Sekunden bis die Front loslegt
    SWEEP = 3.8         # Dauer, bis die Front die Person überquert hat
    DRIFT = 2.4         # Lebensdauer eines Aschekorns

    def _setup(self):
        h, w = self.scene.shape[:2]
        left, top, bw, bh = self.bounds
        rng = np.random.default_rng(11)
        # Zerfallszeit je Pixel: Front von rechts nach links, rauschig ausgefranst.
        noise = cv2.GaussianBlur(rng.standard_normal((h, w)).astype(np.float32), (0, 0), max(2.0, bw / 25))
        noise /= noise.std() + 1e-6
        fine = cv2.GaussianBlur(rng.standard_normal((h, w)).astype(np.float32), (0, 0), 1.2)
        fine /= fine.std() + 1e-6
        across = np.clip((left + bw - self.x) / max(1, bw), 0, 1)
        detach = self.START + across * self.SWEEP + noise * .45 + fine * .12
        self.detach = np.maximum(detach, self.START * .6).astype(np.float32)
        # Aschekörner: jedes zweite Pixel der Person.
        solid = self._solid > 0
        grid = np.zeros_like(solid)
        grid[::2, ::2] = True
        py, px = np.nonzero(solid & grid)
        self.p0 = np.stack([px, py], 1).astype(np.float32) + rng.uniform(-.5, .5, (len(px), 2)).astype(np.float32)
        self.td = self.detach[py, px]
        photo = self.scene[py, px].astype(np.float32)
        ash = np.float32((92, 86, 80))
        # Asche: grau-braun, nur ein Hauch der ursprünglichen Farbe; Flocken
        # unterschiedlich hell.
        shade = rng.uniform(.55, 1.25, (len(px), 1)).astype(np.float32)
        self.ash_colour = (photo * .18 + ash * .82) * shade
        n = len(px)
        scale = max(40.0, bw)
        # Wind nach links oben, individuell verschieden; Wirbel mit Zufallsphasen.
        self.wind = np.stack([-rng.uniform(.35, .75, n), -rng.uniform(.05, .30, n)], 1).astype(np.float32) * scale
        self.swirl_f = rng.uniform(1.5, 4.0, (n, 2)).astype(np.float32)
        self.swirl_p = rng.uniform(0, 2 * np.pi, (n, 2)).astype(np.float32)
        self.swirl_a = (rng.uniform(.02, .07, (n, 1)) * scale).astype(np.float32)
        self.life = rng.uniform(.7, 1.3, n).astype(np.float32) * self.DRIFT
        self._loop_end = float(self.td.max() + self.life.max())

    def _render_live(self, elapsed):
        if self._loop_end is None:           # niemand erkannt: Standbild
            return self.scene
        return self._render(self._timeline(elapsed))

    def _render(self, t):
        h, w = self.scene.shape[:2]
        if not np.any(self.mask):
            return self.scene
        # Körper: vor der Front unverändert, kurz davor verkohlt mit Glutsaum.
        until = self.detach - t
        present = np.clip(until / .08, 0, 1)
        body = present * self.mask
        char = np.clip(1 - until / .7, 0, 1)[..., None]
        ember = np.exp(-((until - .06) / .07) ** 2)[..., None]
        grey = self.scene.mean(axis=2, keepdims=True)
        skin = self.scene * (1 - char * .8) + grey * .25 * char
        skin = skin + np.float32((255, 120, 30)) * ember * 1.1
        # Aschekörner: beschleunigt vom Wind, verwirbelt, verblassen.
        tau = np.maximum(0, t - self.td)
        moving = tau > 0
        tau_m = tau[moving][:, None]
        p = (self.p0[moving] + self.wind[moving] * tau_m ** 1.6 * .5
             + self.swirl_a[moving] * tau_m * np.sin(self.swirl_f[moving] * tau_m + self.swirl_p[moving]))
        fade = np.clip(1 - tau[moving] / self.life[moving], 0, 1) ** 1.5
        # Jedes Korn steht für 2×2 Pixel: als Flocke von etwa 2 px streuen.
        dust_rgb, dust_raw = _splat_points(p[:, 0], p[:, 1], self.ash_colour[moving], fade, (h, w), sigma=1.0)
        dust = dust_rgb / np.maximum(dust_raw[..., None], 1e-4)
        dust_cover = np.clip(dust_raw * 3.2, 0, 1)
        # Solange die Person da ist: Originalbild. Wo sie zerfallen ist: der ganze
        # ersetzte Hintergrund (auch der Saum, den die Maske nicht erfasst).
        present = present[..., None]
        result = self.scene * present + self.background * (1 - present)
        result = result + (skin - self.scene) * body[..., None]
        result = result * (1 - dust_cover[..., None]) + np.clip(dust, 0, 255) * dust_cover[..., None]
        reveal = float(smooth(t / .35))
        result = self.scene * (1 - reveal) + result * reveal
        return np.clip(result, 0, 255).astype(np.uint8)


class EisUndZerspringen(Verwandlung):
    name = "Eis & Zerspringen"
    description = ("Frost kriecht von unten über die Person, bis sie zu Eis erstarrt – dann "
                   "zerspringt sie in Scherben. Rückwärts fliegen die Scherben wieder zusammen." + _BEST)

    FREEZE = (.4, 2.6)      # Frost von … bis … (Sekunden)
    CRACK = 3.0             # Risse laufen vom Aufprallpunkt los
    SHATTER = 3.6           # Aufprall: Scherben fliegen
    FLIGHT = 3.2            # bis die Scherben liegen
    LIGHT = np.float32((-.35, -.55, .76))   # Licht von oben links vorn

    def _setup(self):
        h, w = self.scene.shape[:2]
        left, top, bw, bh = self.bounds
        rng = np.random.default_rng(23)
        solid = self._solid > 0
        unit = max(1.0, bw / 100)

        def field(sigma, stretch=1.0):
            f = cv2.GaussianBlur(rng.standard_normal((h, w)).astype(np.float32), (0, 0),
                                 sigmaX=max(.5, sigma), sigmaY=max(.5, sigma * stretch))
            return f / (f.std() + 1e-6)

        # Frostfront von den Füßen aufwärts. Mehrere Oktaven: grob wellig, am Rand
        # fein verästelt wie wachsende Eisnadeln, nicht wie eine weiche Wolke.
        front = field(bw / 30) * .20 + field(unit * 2.5) * .07 + field(unit * .8) * .04
        rise = np.clip((top + bh - self.y) / max(1, bh), 0, 1)
        start, end = self.FREEZE
        self.frost_at = (start + rise * (end - start) + front).astype(np.float32)

        # Eisoberfläche: Körperform plus unregelmäßige, angeschmolzene Buckel.
        surface = (self.depth * .35 + (field(unit * 7, 1.4) * .45 + field(unit * 2.5) * .12) * unit
                   ).astype(np.float32)
        gy, gx = np.gradient(cv2.GaussianBlur(surface, (0, 0), 1.6))
        nz = 1 / np.sqrt(1 + gx * gx + gy * gy)
        nx, ny = -gx * nz, -gy * nz
        inside = self.mask[..., None]
        # Dicke: an der Silhouette dünn, zur Mitte hin dick (bestimmt Farbe und Streuung).
        dist = cv2.distanceTransform(np.pad(self._solid, 1), cv2.DIST_L2, 5)[1:-1, 1:-1]
        thick = np.clip(cv2.GaussianBlur(dist, (0, 0), 3) / max(4.0, bw * .12), 0, 1)[..., None]

        # Brechung: Die eingeschlossene Person erscheint durch die Buckel verzerrt und
        # durch Streuung im Eis weichgezeichnet; Rot wird stärker geschluckt als Blau.
        strength = unit * 3
        mx = (self.x - nx * strength).astype(np.float32)
        my = (self.y - ny * strength).astype(np.float32)
        under = cv2.remap(self.scene.astype(np.float32), mx, my, cv2.INTER_LINEAR,
                          borderMode=cv2.BORDER_REFLECT)
        under = cv2.GaussianBlur(under, (0, 0), .6 + unit * .25)
        grey = under.mean(axis=2, keepdims=True)
        under = grey + (under - grey) * .5
        absorb = np.exp(-(.3 + thick * .8) * np.float32((.45, .18, .05)))
        milk = 1 - np.exp(-(.4 + thick * 1.6) * .55)      # Lufteinschlüsse, milchiges Eis
        body = under * absorb * (1 - milk * .4) + np.float32((178, 205, 225)) * milk * .4

        # Eingeschlossene Luftbläschen und feine innere Bruchflächen.
        bubbles = np.zeros((h, w), np.float32)
        py, px = np.nonzero(solid)
        for i in rng.choice(len(px), min(len(px), int(bw * bh / 350)), replace=False):
            cv2.circle(bubbles, (int(px[i]), int(py[i])), int(rng.integers(1, 3)), float(rng.uniform(.3, 1)), -1)
        bubbles = cv2.GaussianBlur(bubbles, (0, 0), .7)
        planes = np.zeros((h, w), np.float32)
        for _ in range(int(np.clip(bw * bh / 2500, 8, 60))):
            i = rng.integers(len(px))
            angle, length = rng.uniform(0, np.pi), rng.uniform(.03, .12) * bw
            d = np.float32((np.cos(angle), np.sin(angle))) * length
            p0 = (int(px[i] - d[0]), int(py[i] - d[1]))
            p1 = (int(px[i] + d[0]), int(py[i] + d[1]))
            cv2.line(planes, p0, p1, float(rng.uniform(.25, .7)), max(1, int(unit * 1.5)), cv2.LINE_AA)
        planes = cv2.GaussianBlur(planes, (0, 0), 1.2 + unit) * np.clip(field(unit * 4) * .5 + .6, 0, 1)
        body = body + (bubbles * 45 + planes * 60)[..., None] * thick
        # Kühler Grundton; dünnes Eis an der Silhouette leuchtet durch Streulicht auf.
        body = body * np.float32((.9, 1.0, 1.08)) + np.float32((0, 8, 20))
        rim = np.exp(-dist / max(1.5, unit * 2.5))[..., None]
        body = body * (1 - rim * .5) + np.float32((205, 228, 244)) * rim * .5

        # Spiegelung der Umgebung (Fresnel) und harte Glanzlichter wie bei nassem Eis.
        env = self.environment
        u = np.clip(256 + nx * 230, 0, 511).astype(np.float32)
        v = np.clip(118 + ny * 110 - 20, 0, 255).astype(np.float32)
        reflect = cv2.remap(env, u, v, cv2.INTER_LINEAR)
        fresnel = (.04 + .96 * (1 - nz) ** 5)[..., None]
        half = self.LIGHT + np.float32((0, 0, 1))
        half /= np.linalg.norm(half)
        facing = np.clip(nx * half[0] + ny * half[1] + nz * half[2], 0, 1)
        glint = (facing ** 160 * 1.2 + facing ** 30 * .08)[..., None] * 255
        ice = body * (1 - fresnel * .5) + reflect * fresnel * .45 + glint
        self._ice_full = np.clip(ice, 0, 255).astype(np.float32)

        # Raureif: körnig-weißer, matter Belag, der der Front vorausläuft und in
        # Mulden und an Rändern teilweise liegen bleibt.
        grain = np.clip(field(.6) * .5 + field(unit * 1.2) * .5 + .3, 0, 1)
        self._frost_rest = (np.clip(field(unit * 5) * .6 - .1 + (1 - thick[..., 0]) * .6, 0, 1)
                            * .3).astype(np.float32)
        white = np.float32((226, 236, 245))
        self._frost = (self.scene * .35 + white * .65) * (.72 + grain[..., None] * .28)
        self._frost = (self._frost + (grain > .97)[..., None] * 30).astype(np.float32)

        # Scherben: Voronoi-Zellen, dichter um den Aufprallpunkt (Brust), mit
        # gezackten Kanten wie echte Bruchlinien.
        impact = np.float32((left + bw * .5, top + bh * .45))
        self.impact = impact
        count = int(np.clip(bw * bh / 900, 30, 110))
        pick = rng.choice(len(px), count, replace=False)
        seeds = np.stack([px[pick], py[pick]], 1).astype(np.float32)
        near = impact + rng.normal(0, 1, (count // 2, 2)).astype(np.float32) * bw * .12
        near = near[(near[:, 0] >= 0) & (near[:, 0] < w) & (near[:, 1] >= 0) & (near[:, 1] < h)]
        near = near[solid[near[:, 1].astype(int), near[:, 0].astype(int)]]
        seeds = np.concatenate([seeds, near])
        marker = np.full((h, w), 255, np.uint8)
        marker[seeds[:, 1].astype(int), seeds[:, 0].astype(int)] = 0
        _, labels = cv2.distanceTransformWithLabels(marker, cv2.DIST_L2, 5,
                                                    labelType=cv2.DIST_LABEL_PIXEL)
        jag_x = self.x + field(unit * 3) * unit * 1.2 + field(unit * .8) * unit * .35
        jag_y = self.y + field(unit * 3) * unit * 1.2 + field(unit * .8) * unit * .35
        labels = cv2.remap(labels.astype(np.float32), jag_x.astype(np.float32), jag_y.astype(np.float32),
                           cv2.INTER_NEAREST, borderMode=cv2.BORDER_REPLICATE).astype(np.int32)
        labels = np.where(solid, labels, 0)
        self.labels = labels
        edge = (cv2.morphologyEx(labels.astype(np.float32), cv2.MORPH_GRADIENT, np.ones((2, 2))) > 0)
        line = (edge & solid).astype(np.float32)
        self.cracks = line
        # Riss = dunkle Bruchkante plus weißlich schimmernde Bruchfläche daneben.
        self._crack_dark = cv2.GaussianBlur(line, (0, 0), .5)
        self._crack_haze = cv2.GaussianBlur(line, (0, 0), 1.5 + unit)
        # Risse laufen vom Aufprall nach außen, nicht alle auf einmal.
        reach = np.hypot(self.x - impact[0], self.y - impact[1]) / max(1, max(bw, bh) * .75)
        self.crack_at = (self.CRACK + np.clip(reach, 0, 1.2) * .45 + field(unit * 2) * .03).astype(np.float32)
        # Nach dem Bruch sitzt jede Scherbe minimal versetzt: Bild springt an den Rissen.
        shift = rng.normal(0, unit * .9, (labels.max() + 1, 2)).astype(np.float32)
        cracked = cv2.remap(self._ice_full, (self.x + shift[labels, 0]).astype(np.float32),
                            (self.y + shift[labels, 1]).astype(np.float32), cv2.INTER_LINEAR,
                            borderMode=cv2.BORDER_REFLECT)
        self._ice_cracked = np.where(solid[..., None], cracked, self._ice_full)

        floor = min(h - 3, self.floor_px - 2)
        self.shards = []
        for label in np.unique(labels[labels > 0]):
            ys, xs = np.nonzero(labels == label)
            if len(xs) < 6:
                continue
            x0, x1, y0, y1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
            centre = np.float32((xs.mean(), ys.mean()))
            away = centre - impact
            away /= np.linalg.norm(away) + 1e-3
            speed = rng.uniform(.4, 1.1) * bw * (1.3 - min(1.0, np.linalg.norm(centre - impact) / bw))
            velocity = away * speed + np.float32((rng.normal(0, .15), -rng.uniform(.3, .8))) * bw
            size = max(x1 - x0, y1 - y0)
            self.shards.append(dict(box=(x0, y0, x1, y1), centre=centre, velocity=velocity,
                                    spin=rng.normal(0, 4.5), depth=rng.normal(0, .35),
                                    phase=rng.uniform(0, 2 * np.pi),
                                    rest=floor - size * .3, mask=(labels[y0:y1, x0:x1] == label)))
        # Eissplitter und Pulver aus dem Aufprall und entlang der Risse.
        n = int(np.clip(bw * bh / 60, 200, 1500))
        cy_, cx_ = np.nonzero(line > 0)
        idx = rng.choice(len(cx_), n, replace=len(cx_) < n) if len(cx_) else np.zeros(0, int)
        origin = np.stack([cx_[idx], cy_[idx]], 1).astype(np.float32) if len(idx) else np.zeros((0, 2), np.float32)
        out = origin - impact
        out /= np.linalg.norm(out, axis=1, keepdims=True) + 1e-3
        self._dust = dict(origin=origin,
                          velocity=(out * rng.uniform(.2, 1.3, (len(origin), 1)) * bw
                                    + rng.normal(0, .25, (len(origin), 2)) * bw).astype(np.float32),
                          life=rng.uniform(.5, 1.4, len(origin)).astype(np.float32),
                          size=rng.uniform(.4, 1.0, len(origin)).astype(np.float32))
        self.gravity = 9.81 * bh / 1.0
        self._loop_end = self.SHATTER + self.FLIGHT
        self._shattered = self._ice(self.SHATTER).astype(np.float32)
        for shard in self.shards:
            # Einmal vorab: Textur, Kantenneigung und Kantenleuchten im Scherbenraster.
            x0, y0, x1, y1 = shard['box']
            piece = np.pad(shard['mask'].astype(np.float32), 1)
            soft = cv2.GaussianBlur(piece, (0, 0), .8)
            shard['gx'] = cv2.Sobel(soft, cv2.CV_32F, 1, 0, ksize=1)
            shard['gy'] = cv2.Sobel(soft, cv2.CV_32F, 0, 1, ksize=1)
            glow = np.clip(piece - cv2.GaussianBlur(piece, (0, 0), 1.2) * .6, 0, 1) * 110
            tex = np.pad(self._shattered[y0:y1, x0:x1], ((1, 1), (1, 1), (0, 0))) * 1.08
            shard['tex'] = (tex + np.float32((0, 6, 14)) + glow[..., None]) * piece[..., None]
            shard['alpha'] = piece

    def _ice(self, t):
        """Standbild zur Zeit t: Raureif läuft vor, dahinter klares Eis, dann Risse."""
        since = t - self.frost_at
        rime = np.clip(since / .22, 0, 1)                  # Raureif legt sich an
        clear = smooth((since - .15) / .55)                # Eis wird klar
        frost = rime * (1 - clear * (1 - self._frost_rest))
        ice = self._ice_full
        if t > self.CRACK:
            grow = np.clip((t - self.crack_at) / .08, 0, 1)
            ice = np.where((grow > 0)[..., None], self._ice_cracked, ice)
            ice = (ice * (1 - (self._crack_dark * grow * .2)[..., None])
                   + (self._crack_haze * grow)[..., None] * np.float32((150, 168, 180)))
        ice = ice * (1 - frost[..., None]) + self._frost * frost[..., None]
        amount = (rime * self.mask)[..., None]
        return self.scene * (1 - amount) + np.clip(ice, 0, 255) * amount

    def _shard_pose(self, shard, tau):
        """Position und Drehung einer Scherbe nach tau Sekunden Flug (mit Aufprall)."""
        (vx, vy), (cx, cy) = shard['velocity'], shard['centre']
        g, rest = self.gravity, shard['rest']
        # Wurfparabel bis zum Boden, dann gedämpft rutschen und liegen bleiben.
        a, b, c = .5 * g, vy, cy - rest
        hit = (-b + np.sqrt(max(0.0, b * b - 4 * a * c))) / (2 * a) if c < 0 else 0.0
        if tau < hit:
            x, y, turn = cx + vx * tau, cy + vy * tau + .5 * g * tau * tau, shard['spin'] * tau
        else:
            slide = tau - hit
            ease = 1 - np.exp(-slide * 4)
            x = cx + vx * hit + vx * .25 * ease / 4
            y = rest - abs(vy + g * hit) * .08 * np.exp(-slide * 7) * abs(np.sin(slide * 14))
            turn = shard['spin'] * (hit + .2 * ease)
        return x, y, turn

    def _render_live(self, elapsed):
        if self._loop_end is None:           # niemand erkannt: Standbild
            return self.scene
        return self._render(self._timeline(elapsed))

    def _render(self, t):
        h, w = self.scene.shape[:2]
        if not np.any(self.mask):
            return self.scene
        if t < self.SHATTER:
            ice = self._ice(t)
            result = self.scene + (ice - self.scene) * self.mask[..., None]
            reveal = float(smooth(t / .35))
            result = self.scene * (1 - reveal) + result * reveal
            return np.clip(result, 0, 255).astype(np.uint8)
        ice = self._shattered
        tau = t - self.SHATTER
        result = self.background.copy()
        layer = np.zeros((h, w, 3), np.float32)
        cover = np.zeros((h, w), np.float32)
        light = np.float32(self.LIGHT[:2]) / np.linalg.norm(self.LIGHT[:2])
        for shard in self.shards:
            x0, y0, x1, y1 = shard['box']
            x, y, turn = self._shard_pose(shard, tau)
            scale = float(np.clip(1 + shard['depth'] * min(tau, 1.0) * .4, .6, 1.5))
            (cx, cy) = shard['centre']
            matrix = cv2.getRotationMatrix2D((float(cx - x0), float(cy - y0)), float(np.degrees(turn)), scale)
            matrix[:, 2] += (x - cx + x0, y - cy + y0)
            # Nur in den Zielbereich der Scherbe transformieren, nicht ins ganze Bild.
            corners = np.array([[0, 0, 1], [x1 - x0, 0, 1], [0, y1 - y0, 1], [x1 - x0, y1 - y0, 1]], np.float32)
            placed = corners @ matrix.T
            dx0, dy0 = np.floor(placed.min(0)).astype(int) - 1
            dx1, dy1 = np.ceil(placed.max(0)).astype(int) + 2
            dx0, dy0, dx1, dy1 = max(dx0, 0), max(dy0, 0), min(dx1, w), min(dy1, h)
            if dx1 <= dx0 or dy1 <= dy0:
                continue
            local = matrix.copy()
            local[:, 2] -= (dx0, dy0)
            size = (dx1 - dx0, dy1 - dy0)
            # Bruchkante als Fase: zum Licht hin hell, abgewandt dunkel (Licht ins
            # Scherbenraster zurückgedreht, damit die Fase mit der Drehung wandert).
            c, sn = np.cos(turn), np.sin(turn)
            lx, ly = light[0] * c - light[1] * sn, light[0] * sn + light[1] * c
            bevel = np.clip(-(shard['gx'] * lx + shard['gy'] * ly) * 1.1, -1, 1)
            # Kurzes Aufblitzen, wenn eine Fläche beim Drehen zum Licht zeigt.
            flash = max(0.0, float(np.cos(turn * 1.7 + shard['phase']))) ** 12 * 110
            alpha_src = shard['alpha']
            source = np.dstack([shard['tex'] + ((bevel * 140 + flash) * alpha_src)[..., None], alpha_src])
            local[:, 2] -= local[:, :2] @ np.float32((1, 1))   # Rand von 1 px im Raster
            moved = cv2.warpAffine(source, local, size, flags=cv2.INTER_LINEAR)
            shaded, alpha = moved[..., :3], moved[..., 3]
            region = (slice(dy0, dy1), slice(dx0, dx1))
            layer[region] = layer[region] * (1 - alpha[..., None]) + shaded
            cover[region] = np.maximum(cover[region], alpha)
        # Weicher Kontaktschatten der Scherben am Boden.
        shadow = np.roll(cv2.GaussianBlur(cover, (0, 0), 2.5), 2, axis=0)
        near_floor = np.clip(1 - (self.floor_px - self.y) / max(1, self.bounds[3] * .25), 0, 1)
        result = result * (1 - (shadow * near_floor * .45)[..., None])
        # Eis ist durchscheinend: der Hintergrund schimmert leicht durch die Scherben.
        result = result * (1 - cover[..., None] * .9) + np.clip(layer, 0, 255) * cover[..., None] * .9
        result = self._dust_layer(result, tau)
        return np.clip(result, 0, 255).astype(np.uint8)

    def _dust_layer(self, result, tau):
        """Eispulver und feine Splitter, die aus den Rissen stieben und verglühen."""
        d = self._dust
        age = tau / d['life']
        live = age < 1
        if tau > 1.5 or not live.any():
            return result
        h, w = result.shape[:2]
        pos = d['origin'][live] + d['velocity'][live] * tau + np.float32((0, .5 * .35 * self.gravity)) * tau * tau
        fade = (1 - age[live]) ** 1.5 * d['size'][live]
        ok = (pos[:, 0] >= 0) & (pos[:, 0] < w - 1) & (pos[:, 1] >= 0) & (pos[:, 1] < h - 1)
        spots = np.zeros((h, w), np.float32)
        np.add.at(spots, (pos[ok, 1].astype(int), pos[ok, 0].astype(int)), fade[ok])
        spots = np.clip(cv2.GaussianBlur(spots, (0, 0), .8) * 3.5, 0, 1)
        # Kurzer Nebel aus Eisstaub am Aufprallpunkt.
        strength = max(0.0, 1 - tau / .9) ** 2 * .35
        if strength > 0:
            r = max(1.0, self.bounds[2] * (.15 + tau * .5))
            dx = (np.arange(w, dtype=np.float32) - self.impact[0]) ** 2
            dy = (np.arange(h, dtype=np.float32) - self.impact[1]) ** 2
            spots = spots + np.outer(np.exp(-dy / (2 * r * r)), np.exp(-dx / (2 * r * r))) * strength
        cloud = np.clip(spots, 0, 1)[..., None]
        return result * (1 - cloud) + np.float32((228, 238, 248)) * cloud


class Formwandler(FluidVerwandlung):
    name = "Formwandler"
    description = ("Wie der T-1000: Deine erste Pose wird zu Chrom, dann nimmst du eine neue Pose "
                   "ein (Countdown) – das Metall fließt von der einen Form in die andere und "
                   "wieder zurück." + _BEST)

    COUNTDOWN = 3.0
    MORPH = (1.5, 5.0)         # Fließen von … bis … (Sekunden im Ablauf)
    END = 6.5                  # Foto der zweiten Pose erreicht
    hold_end = 1.5
    hold_start = 1.5
    PARTICLES = 6000

    def __init__(self):
        super().__init__()
        self._reset_shift()

    def _reset_shift(self):
        self.phase = 'metal'
        self._second = None
        self._second_pending = None
        self._countdown_from = 0.0
        self._offset = 0.0

    def _on_reset(self):
        super()._on_reset()
        self._reset_shift()

    def _setup(self):
        # Keine Strömungssimulation: die Bewegung ergibt sich aus der Zuordnung.
        self._reset_shift()

    def _prepared(self):
        return True

    # --- zweite Pose ------------------------------------------------------------

    def _relief_for(self, scene, portrait):
        """Relief und Maske einer weiteren Aufnahme mit denselben Bausteinen."""
        keep = {k: getattr(self, k) for k in ('scene', 'mask', 'bounds', 'depth', 'depth_source')}
        try:
            self.scene = scene
            binary = portrait['mask']
            self.mask = cv2.GaussianBlur(binary.astype(np.float32) / 255, (0, 0), 0.65)
            solid = (self.mask > .45).astype(np.uint8)
            if solid.sum() < max(8, solid.size * .015):
                return None
            self.bounds = cv2.boundingRect(cv2.findNonZero(solid))
            self._build_relief(portrait, solid)
            return dict(scene=scene, mask=self.mask, depth=self.depth, solid=solid, bounds=self.bounds)
        finally:
            for key, value in keep.items():
                setattr(self, key, value)

    def _match(self, second):
        """Punkte der ersten Form den Plätzen der zweiten zuordnen (sliced optimal transport)."""
        rng = np.random.default_rng(31)
        def sample(solid):
            # Gleich viele Punkte in beiden Formen (bei kleinen Formen mehrfach).
            ys, xs = np.nonzero(solid)
            pick = rng.choice(len(xs), self.PARTICLES, replace=len(xs) < self.PARTICLES)
            return np.stack([xs[pick], ys[pick]], 1).astype(np.float32) + rng.uniform(-.5, .5, (len(pick), 2))
        first_points = sample(self._solid)
        target = sample(second['solid'])
        moved = first_points.copy()
        for _ in range(60):
            angle = rng.uniform(0, np.pi)
            direction = np.array([np.cos(angle), np.sin(angle)], np.float32)
            a, b = moved @ direction, target @ direction
            ia, ib = np.argsort(a), np.argsort(b)
            moved[ia] += ((b[ib] - a[ia]))[:, None] * direction
        self.pa = first_points.astype(np.float32)
        self.pb = moved.astype(np.float32)
        n = len(self.pa)
        left, top, bw, bh = self.bounds
        # Unten beginnt das Fließen etwas später; jeder Punkt wabert eigen.
        self.delay = (np.clip((self.pa[:, 1] - top) / max(1, bh), 0, 1) * .25 +
                      rng.uniform(0, .08, n)).astype(np.float32)
        self.wobble_p = rng.uniform(0, 2 * np.pi, (n, 2)).astype(np.float32)
        self.wobble_a = (rng.uniform(.01, .04, (n, 1)) * bw).astype(np.float32)
        self.sag = float(bh * .08)
        self._second = second
        self._loop_end = self.END

    # --- Darstellung -------------------------------------------------------------

    def _fields(self, s):
        """Dichte und Herkunft in beiden Aufnahmen für den Fortschritt s (0 … 1)."""
        from bildeffekte.liquid_sim import _splat
        h, w = self.scene.shape[:2]
        si = np.clip((s - self.delay) / .67, 0, 1)
        si = (si * si * (3 - 2 * si))[:, None]
        arc = np.sin(np.pi * si)
        pos = self.pa + (self.pb - self.pa) * si
        pos = pos + self.wobble_a * arc * np.sin(si * 7 + self.wobble_p)
        pos[:, 1:] += self.sag * arc[:, :1]
        sup = .5                                    # halbe Auflösung reicht, danach geglättet
        gh, gw = max(4, int(h * sup)), max(4, int(w * sup))
        x, y = pos[:, 0] * sup, pos[:, 1] * sup
        fields = []
        weight = None
        for source in (self.pa, self.pb):
            sx, weight = _splat(x, y, (source[:, 0] - pos[:, 0]).astype(np.float64), (gh, gw))
            sy, _ = _splat(x, y, (source[:, 1] - pos[:, 1]).astype(np.float64), (gh, gw))
            fields.append((sx, sy))
        blur = lambda f, sigma: cv2.GaussianBlur(f.astype(np.float32), (0, 0), sigma)
        wide = np.maximum(blur(weight, 2.0), 1e-4)
        up = lambda f: cv2.resize(f, (w, h), interpolation=cv2.INTER_LINEAR)
        maps = [(self.x + up(blur(sx, 2.0) / wide), self.y + up(blur(sy, 2.0) / wide)) for sx, sy in fields]
        expected = len(pos) / max(1.0, float(np.count_nonzero(self._solid)) * sup * sup)
        density = up(blur(weight, 1.0)) / max(1e-4, expected)
        return density, maps

    def _morph(self, u):
        h, w = self.scene.shape[:2]
        second = self._second
        s = float(smooth((u - self.MORPH[0]) / (self.MORPH[1] - self.MORPH[0])))
        to_metal = float(smooth(u / 1.5))
        to_photo = float(smooth((u - self.MORPH[1]) / (self.END - self.MORPH[1])))
        density, ((ax, ay), (bx, by)) = self._fields(s)
        coverage = np.clip(density * 2.5, 0, 1)
        remap = lambda img, mx, my: cv2.remap(img, mx, my, cv2.INTER_LINEAR, borderMode=cv2.BORDER_CONSTANT)
        alpha = (remap(self.mask, ax, ay) * (1 - s) + remap(second['mask'], bx, by) * s) * coverage
        depth = remap(self.depth, ax, ay) * (1 - s) + remap(second['depth'], bx, by) * s
        photo_a = cv2.remap(self.scene, ax, ay, cv2.INTER_LINEAR, borderMode=cv2.BORDER_REPLICATE)
        photo_b = cv2.remap(second['scene'], bx, by, cv2.INTER_LINEAR, borderMode=cv2.BORDER_REPLICATE)
        box = cv2.boundingRect(cv2.findNonZero((alpha > .003).astype(np.uint8)))
        chrome = np.zeros((h, w, 3), np.float32)
        if box[2] and box[3]:
            y0, x0 = max(0, box[1] - 6), max(0, box[0] - 6)
            y1, x1 = min(h, box[1] + box[3] + 6), min(w, box[0] + box[2] + 6)
            crop = (slice(y0, y1), slice(x0, x1))
            zeros = np.zeros((y1 - y0, x1 - x0), np.float32)
            chrome[crop] = self._shade(crop, u, 0.0, False, depth[crop], zeros, zeros + 2, 0.0, 0.0, 1.0)
        photo = photo_a * (1 - s) + photo_b * s
        surface = photo * (1 - to_metal) + chrome * to_metal
        surface = surface * (1 - to_photo) + photo_b * to_photo
        shadow = cv2.GaussianBlur(alpha, (0, 0), 3) * .12 * to_metal
        result = self.background * (1 - shadow[..., None]) * (1 - alpha[..., None]) + surface * alpha[..., None]
        reveal = float(smooth(u / .35))
        result = self.scene * (1 - reveal) + result * reveal
        return np.clip(result, 0, 255).astype(np.uint8)

    def _first_only(self, u):
        """Nur die erste Pose (vor der zweiten Aufnahme): Foto → Chrom."""
        if self._second is None:
            self._second = dict(scene=self.scene, mask=self.mask, depth=self.depth,
                                solid=self._solid, bounds=self.bounds)
            self.pa = self.pb = None
        return self._static_morph(u)

    def _static_morph(self, u):
        # Ohne zweite Form: Punkte bleiben, wo sie sind.
        if getattr(self, 'pa', None) is None or len(self.pa) == 0:
            ys, xs = np.nonzero(self._solid)
            step = max(1, len(xs) // self.PARTICLES)
            self.pa = np.stack([xs[::step], ys[::step]], 1).astype(np.float32)
            self.pb = self.pa.copy()
            self.delay = np.zeros(len(self.pa), np.float32)
            self.wobble_p = np.zeros((len(self.pa), 2), np.float32)
            self.wobble_a = np.zeros((len(self.pa), 1), np.float32)
            self.sag = 0.0
        return self._morph(min(u, self.MORPH[0]))

    def _render_live(self, elapsed):
        if not np.any(self.mask):
            return self.scene
        if self.phase == 'metal':
            if elapsed < self.MORPH[0] + .3:
                return self._first_only(elapsed)
            self.phase, self._countdown_from = 'countdown', elapsed
        if self.phase == 'countdown':
            left = self.COUNTDOWN - (elapsed - self._countdown_from)
            live = cv2.resize(self._current_frame, self.scene.shape[1::-1], interpolation=cv2.INTER_AREA)
            if left > 0:
                return self._countdown(live, left)
            self.phase = 'analyse'
            self._second_scene = live
            self._second_pending = self._submit_second(live)
        if self.phase == 'analyse':
            if self._second_pending is None or not self._second_pending.done():
                return self._first_only(self.MORPH[0])
            try:
                portrait = self._second_pending.result()
            except Exception:
                portrait = None
            self._second_pending = None
            second = self._relief_for(self._second_scene, portrait) if portrait and portrait.get('mask') is not None else None
            if second is None:
                self.phase, self._countdown_from = 'countdown', elapsed      # neuer Versuch
                return self._first_only(self.MORPH[0])
            self._match(second)
            self.phase, self._offset = 'loop', elapsed - self.MORPH[0]
        return self._morph(self._timeline(elapsed - self._offset))

    def _submit_second(self, scene):
        return verwandlung.submit_portrait(scene)

    def _countdown(self, live, left):
        out = live.copy()
        h, w = out.shape[:2]
        scale = max(.5, w / 520)
        text = "Neue Pose! %d" % int(np.ceil(left))
        size = cv2.getTextSize(text, cv2.FONT_HERSHEY_SIMPLEX, scale * 1.4, int(3 * scale))[0]
        org = ((w - size[0]) // 2, int(h * .15) + size[1])
        cv2.putText(out, text, org, cv2.FONT_HERSHEY_SIMPLEX, scale * 1.4, (0, 0, 0), int(6 * scale), cv2.LINE_AA)
        cv2.putText(out, text, org, cv2.FONT_HERSHEY_SIMPLEX, scale * 1.4, (255, 255, 255), int(3 * scale), cv2.LINE_AA)
        return out


def _learn_and_keep(frame, person):
    """Im Analyse-Thread: Hintergrund lernen und die Maske für die Tarnung zurückgeben."""
    if person is None:
        return None
    verwandlung.PLATE.update(frame, person)
    return person


class PredatorTarnung(IEffectStrategy):
    name = "Predator-Tarnung"
    description = ("Live: Du wirst unsichtbar wie der Predator – der Hintergrund scheint "
                   "flimmernd durch dich hindurch, nur deine Kontur bricht das Licht. Für "
                   "perfekte Tarnung vorher kurz aus dem Bild gehen, damit der Hintergrund "
                   "hinter dir bekannt ist.")
    category = "Verwandlungen"

    ENGAGE = 1.6            # Einschalten von oben nach unten

    def __init__(self):
        self._pending = None
        self._mask = None
        self._reset()

    def _reset(self):
        if self._pending is not None:
            self._pending.cancel()
        self._pending = None
        self._mask = None
        self._size = None
        self._reset_requested = False
        self._engaged_at = None
        self._next_glitch = 0.0
        self._glitch_until = 0.0
        self._rng = np.random.default_rng()

    def activate(self):
        self._reset_requested = True

    def deactivate(self):
        self._reset_requested = True

    def _internal(self, frame):
        size = frame.shape[1], frame.shape[0]
        scale = min(1.0, 512 / max(size), 400 / frame.shape[0])
        return max(1, round(size[0] * scale)), max(1, round(size[1] * scale))

    def _update_mask(self, small):
        if self._pending is not None and self._pending.done():
            try:
                mask = self._pending.result()
            except Exception:
                mask = None
            self._pending = None
            if mask is not None:
                self._mask = mask
        if self._pending is None:
            self._pending = verwandlung.submit_person_mask(small, _learn_and_keep)

    def apply(self, frame, config):
        width, height = self._internal(frame)
        if self._reset_requested or self._size != (width, height):
            self._reset()
            self._size = (width, height)
        if min(width, height) < 120:
            return frame
        small = cv2.resize(frame, (width, height), interpolation=cv2.INTER_AREA)
        self._update_mask(small)
        mask = self._mask
        if mask is None or mask.shape != small.shape[:2] or np.count_nonzero(mask > 127) < mask.size * .01:
            self._engaged_at = None
            return frame
        now = time.monotonic()
        if self._engaged_at is None:
            self._engaged_at = now
            self._next_glitch = now + self.ENGAGE + self._rng.uniform(5, 9)
        result = self._cloak(small, mask, now - self._engaged_at, now)
        return cv2.resize(result, (frame.shape[1], frame.shape[0]), interpolation=cv2.INTER_LINEAR)

    def _background(self, small, person):
        """Gemerkter Hintergrund; wo er fehlt, aus dem aktuellen Bild ergänzt."""
        h, w = small.shape[:2]
        size = (max(1, w // 4), max(1, h // 4))
        hole = cv2.resize(cv2.dilate(person, np.ones((5, 5), np.uint8)), size)
        guess = cv2.resize(cv2.inpaint(cv2.resize(small, size), hole, 3, cv2.INPAINT_TELEA), (w, h))
        guess = guess.astype(np.float32)
        plate = verwandlung.PLATE
        image, confidence = plate.snapshot()
        if image is None or image.shape != small.shape:
            return guess, 0.0
        known = np.clip((confidence - .5) * 2, 0, 1)[..., None]
        coverage = float(known[..., 0][person > 0].mean()) if np.any(person) else 1.0
        return image * known + guess * (1 - known), coverage

    def _cloak(self, small, mask, since, now):
        h, w = small.shape[:2]
        frame = small.astype(np.float32)
        person = (mask > 110).astype(np.uint8)
        # Etwas größer als die (leicht verzögerte) Maske, weich auslaufend.
        margin = max(3, w // 90)
        cover = cv2.GaussianBlur(cv2.dilate(person, np.ones((margin, margin), np.uint8)).astype(np.float32),
                                 (0, 0), 1.5)
        background, coverage = self._background(small, person)
        # Relief: Körperwölbung plus Helligkeitsstruktur (Gesichtszüge brechen mit).
        distance = cv2.distanceTransform(np.pad(person, 1), cv2.DIST_L2, 3)[1:-1, 1:-1]
        height = np.sqrt(cv2.GaussianBlur(distance, (0, 0), 3) * max(4.0, w / 40))
        grey = cv2.cvtColor(small, cv2.COLOR_RGB2GRAY).astype(np.float32)
        height += (grey - cv2.GaussianBlur(grey, (0, 0), 6)) * .06
        height = cv2.GaussianBlur(height, (0, 0), 1.2)
        gx = cv2.Sobel(height, cv2.CV_32F, 1, 0, ksize=3) * .125
        gy = cv2.Sobel(height, cv2.CV_32F, 0, 1, ksize=3) * .125
        grid_y, grid_x = np.mgrid[0:h, 0:w].astype(np.float32)
        t = now
        phase = height * .35 + grid_y * .045 - t * 4.0
        strength = w / 90
        dx = gx * strength * 2.2 + np.sin(phase) * strength * .5
        dy = gy * strength * 2.2 + np.cos(phase * 1.3) * strength * .5
        channels = []
        for c, k in enumerate((1.12, 1.0, .88)):             # leichte Farbaufspaltung
            channels.append(cv2.remap(background[..., c], grid_x + dx * k, grid_y + dy * k,
                                      cv2.INTER_LINEAR, borderMode=cv2.BORDER_REFLECT))
        refracted = np.dstack(channels)
        # Kontur: dünner, heller Saum, wo das Licht an der Kante gebrochen wird.
        rim = cover - cv2.GaussianBlur(cv2.erode(person, np.ones((3, 3), np.uint8)).astype(np.float32), (0, 0), 1.2)
        rim = np.clip(rim, 0, 1)[..., None] * (1 + .5 * np.sin(phase * 2))[..., None]
        cloak = refracted * .93 + frame * .05 + rim * np.float32((20, 30, 36))
        # Einschalten: Front von oben nach unten, an der Front Entladungen.
        front = since / self.ENGAGE * (h * 1.15) - h * .1
        engaged = np.clip((front - grid_y) / (h * .08), 0, 1)
        # Aussetzer: für einen Augenblick reißt die Tarnung in Flecken auf.
        if now >= self._next_glitch:
            self._glitch_until = now + .35
            self._next_glitch = now + self._rng.uniform(6, 10)
        sparks = np.zeros((h, w), np.float32)
        if now < self._glitch_until:
            noise = cv2.resize(self._rng.random((max(2, h // 24), max(2, w // 24))).astype(np.float32), (w, h))
            engaged = engaged * (noise > .45)
            sparks = self._arcs(person, h, w, count=10)
        if since < self.ENGAGE:
            band = np.exp(-((grid_y - front) / (h * .02)) ** 2) * person
            sparks = np.maximum(sparks, band * self._arcs(person, h, w, count=14, near=front))
        alpha = (cover * engaged)[..., None]
        result = frame * (1 - alpha) + cloak * alpha
        result += sparks[..., None] * np.float32((150, 210, 255))
        return np.clip(result, 0, 255).astype(np.uint8)

    def _arcs(self, person, h, w, count, near=None):
        """Blau-weiße Entladungen: zufällige Zickzacklinien auf der Person."""
        canvas = np.zeros((h, w), np.float32)
        ys, xs = np.nonzero(person)
        if len(xs) == 0:
            return canvas
        if near is not None:
            close = np.abs(ys - near) < h * .05
            if np.any(close):
                ys, xs = ys[close], xs[close]
        for _ in range(count):
            i = self._rng.integers(len(xs))
            points = [(xs[i], ys[i])]
            for _ in range(self._rng.integers(3, 7)):
                x, y = points[-1]
                points.append((int(x + self._rng.normal(0, w / 60)), int(y + self._rng.normal(0, h / 60))))
            cv2.polylines(canvas, [np.array(points, np.int32)], False, 1.0, 1, cv2.LINE_AA)
        return cv2.GaussianBlur(canvas, (0, 0), .8) * 1.6
