"""FLIP-Strömungslöser in der Bildebene für die Flüssigmetall-Schmelze II.

Verfahren nach Zhu & Bridson (2005), wie in Filmeffekten üblich:
- Die Masse steckt in Partikeln und bleibt dadurch exakt erhalten.
- Geschwindigkeiten liegen zusätzlich auf einem versetzten MAC-Gitter.
- Eine Druckprojektion (Poisson-Gleichung, vorkonditioniertes CG) erzwingt Inkompressibilität.
- Schwerkraft, innere Reibung, Viskosität (PIC-Anteil) und Oberflächenspannung
  (CSF, Brackbill et al. 1992) wirken auf das Gitter.
Jedes Partikel trägt seine Herkunft im Ausgangsbild; so fließen Gesicht und
Relief mit dem Metall mit.

Die Bildebene hat keine Tiefe. Metall, das am Boden ankommt, läuft deshalb in
eine Bodenpfütze ab, die sich in der Bodenebene (also auch zur Kamera hin)
ausbreitet. Ihr Volumen ist die Summe der abgeflossenen Partikel.
"""

import cv2
import numpy as np
from scipy.sparse import coo_matrix
from scipy.sparse import diags
from scipy.sparse.linalg import cg


def _corners(x, y, shape):
    """Bilineare Gewichte: vier Nachbarknoten je Punkt."""
    h, w = shape
    x = np.clip(x, 0, w - 1.001)
    y = np.clip(y, 0, h - 1.001)
    i, j = x.astype(np.int32), y.astype(np.int32)
    fx, fy = x - i, y - j
    return ((j, i, (1 - fx) * (1 - fy)), (j, i + 1, fx * (1 - fy)),
            (j + 1, i, (1 - fx) * fy), (j + 1, i + 1, fx * fy))


def _splat(x, y, values, shape):
    total = np.zeros(shape[0] * shape[1], np.float64)
    weight = np.zeros_like(total)
    for j, i, w in _corners(x, y, shape):
        index = j * shape[1] + i
        total += np.bincount(index, w * values, total.size)
        weight += np.bincount(index, w, total.size)
    return total.reshape(shape), weight.reshape(shape)


def _gather(grid, x, y):
    return sum(grid[j, i] * w for j, i, w in _corners(x, y, grid.shape))


class LiquidSheet:
    """Metall als Schicht vor der Kamera; die Schwerkraft zeigt bildabwärts."""

    def __init__(self, mask, cell, floor_row, subject_height_px, seed=7,
                 drag_base=1.2, drag_solid=110.0, pic_base=.04, pic_solid=.5,
                 tension=60.0, drain_base=1.0, drain_height=.15):
        """Die Stoffwerte (Reibung, PIC-Anteil = Zähigkeit, Oberflächenspannung,
        Abfluss) sind für Flüssigmetall voreingestellt; Honig ist z. B. zäher."""
        height, width = mask.shape
        self.cell_size = cell
        self.drag_base, self.drag_solid = drag_base, drag_solid
        self.pic_base, self.pic_solid = pic_base, pic_solid
        self.tension = tension
        self.drain_base, self.drain_height = drain_base, drain_height
        gw, gh = max(8, round(width / cell)), max(8, round(height / cell))
        self.shape = gh, gw
        self.sx, self.sy = width / gw, height / gh
        self.floor = int(np.clip(round(floor_row / self.sy), 2, gh))
        coverage = cv2.resize(mask.astype(np.float32), (gw, gh), interpolation=cv2.INTER_AREA)
        coverage[self.floor:] = 0
        rng = np.random.default_rng(seed)
        cj, ci = np.nonzero(coverage > .5)
        # 2×2 Partikel je Zelle, leicht verrauscht (vermeidet Gittermuster).
        offsets = np.array([(.25, .25), (.75, .25), (.25, .75), (.75, .75)], np.float32)
        self.x = (ci[:, None] + offsets[None, :, 0]).ravel().astype(np.float32)
        self.y = (cj[:, None] + offsets[None, :, 1]).ravel().astype(np.float32)
        self.x += rng.uniform(-.2, .2, self.x.size).astype(np.float32)
        self.y += rng.uniform(-.2, .2, self.y.size).astype(np.float32)
        self.y = np.minimum(self.y, self.floor - .01)
        self.ox, self.oy = self.x * self.sx - .5, self.y * self.sy - .5
        self.u = np.zeros_like(self.x)
        self.v = np.zeros_like(self.x)
        # Sichtbarer Körper entspricht grob einem Meter.
        self.gravity = 9.81 * max(8.0, subject_height_px / self.sy)
        self.time = 0.0
        self._pressure = None
        self._rng = rng
        self.initial_count = self.x.size
        self.pool_count = 0
        self.pool_x_sum = 0.0
        self.inflow = 0.0          # geglättete Zuflussrate in Partikel/s
        self.snapshot = self._snapshot()

    def _snapshot(self):
        # Ein Tupel wird atomar ersetzt: Leser in anderen Threads sehen immer
        # einen zusammengehörigen Zustand.
        return (self.time, self.x, self.y, self.ox, self.oy, self.pool_count,
                self.pool_x_sum, self.inflow)

    @property
    def count(self):
        return self.x.size

    # --- Gitter ---------------------------------------------------------------

    def _to_grid(self):
        gh, gw = self.shape
        su, wu = _splat(self.x, self.y - .5, self.u, (gh, gw + 1))
        sv, wv = _splat(self.x - .5, self.y, self.v, (gh + 1, gw))
        u = np.where(wu > 1e-6, su / np.maximum(wu, 1e-6), 0)
        v = np.where(wv > 1e-6, sv / np.maximum(wv, 1e-6), 0)
        counts = np.bincount(np.clip(self.y.astype(np.int32), 0, gh - 1) * gw +
                             np.clip(self.x.astype(np.int32), 0, gw - 1), minlength=gh * gw)
        return u, v, counts.reshape(gh, gw)

    def _solid_faces(self, u, v):
        u[:, 0] = u[:, -1] = 0
        v[0] = np.minimum(v[0], 0) * 0
        v[self.floor:] = 0

    def _project(self, u, v, fluid):
        gh, gw = self.shape
        solid = np.zeros((gh, gw), bool)
        solid[self.floor:] = True
        div = (u[:, 1:] - u[:, :-1]) + (v[1:] - v[:-1])
        index = -np.ones((gh, gw), np.int64)
        cells = np.nonzero(fluid)
        n = len(cells[0])
        if n == 0:
            return
        index[cells] = np.arange(n)
        diag = np.zeros(n)
        rows, cols = [], []
        cj, ci = cells
        for dj, di in ((0, -1), (0, 1), (-1, 0), (1, 0)):
            nj, ni = cj + dj, ci + di
            inside = (nj >= 0) & (nj < gh) & (ni >= 0) & (ni < gw)
            open_ = inside.copy()
            open_[inside] = ~solid[nj[inside], ni[inside]]
            diag += open_
            neighbour = np.full(n, -1)
            neighbour[inside] = index[nj[inside], ni[inside]]
            link = open_ & (neighbour >= 0)
            rows.append(np.nonzero(link)[0])
            cols.append(neighbour[link])
        rows = np.concatenate(rows + [np.arange(n)])
        cols = np.concatenate(cols + [np.arange(n)])
        data = np.concatenate([-np.ones(len(rows) - n), diag])
        matrix = coo_matrix((data, (rows, cols)), shape=(n, n)).tocsr()
        # CG mit Jacobi-Vorkonditionierer; Warmstart mit dem Druck des letzten
        # Schritts, der sich zwischen zwei Teilschritten kaum ändert.
        start = self._pressure[cells] if self._pressure is not None else None
        pressure, _ = cg(matrix, -div[cells], x0=start, rtol=2e-3, maxiter=60,
                         M=diags(1.0 / np.maximum(diag, 1)))
        p = np.zeros((gh, gw))
        p[cells] = pressure
        self._pressure = p
        # Druckgradient an Flächen zwischen nicht-festen Zellen abziehen.
        u[:, 1:-1] -= p[:, 1:] - p[:, :-1]
        v[1:-1] -= p[1:] - p[:-1]
        self._solid_faces(u, v)

    def _surface_tension(self, counts, strength):
        # CSF auf geglätteter Dichte; Kraft in Zellen, dann auf die Flächen.
        density = cv2.GaussianBlur(np.clip(counts / 4.0, 0, 1).astype(np.float32), (0, 0), 1.0)
        dy, dx = np.gradient(density)
        norm = np.sqrt(dx * dx + dy * dy) + 1e-3
        kappa = -(np.gradient(dx / norm, axis=1) + np.gradient(dy / norm, axis=0))
        fx, fy = strength * kappa * dx, strength * kappa * dy
        fu = np.zeros((fx.shape[0], fx.shape[1] + 1))
        fu[:, 1:-1] = (fx[:, 1:] + fx[:, :-1]) * .5
        fv = np.zeros((fy.shape[0] + 1, fy.shape[1]))
        fv[1:-1] = (fy[1:] + fy[:-1]) * .5
        return fu, fv

    # --- Schritt --------------------------------------------------------------

    def step(self, dt, softness):
        """softness 0 = fest, 1 = flüssig: steuert innere Reibung und Zähigkeit."""
        self.time += dt
        if softness <= 0 or self.count == 0:
            return
        u, v, counts = self._to_grid()
        u_old, v_old = u.copy(), v.copy()
        fluid = counts > 0
        v[1:-1] += self.gravity * dt * (fluid[1:] | fluid[:-1])
        fu, fv = self._surface_tension(counts, self.tension)
        u += dt * fu
        v += dt * fv
        # Innere Reibung: zähes Metall kriecht, flüssiges fällt fast frei.
        damping = np.exp(-(self.drag_base + self.drag_solid * (1 - softness) ** 2) * dt)
        u *= damping
        v *= damping
        self._solid_faces(u, v)
        self._project(u, v, fluid)
        # FLIP mit PIC-Anteil: mehr PIC = zäher, weniger Spritzrauschen.
        pic = self.pic_base + self.pic_solid * (1 - softness)
        gu = _gather(u, self.x, self.y - .5)
        gv = _gather(v, self.x - .5, self.y)
        du = gu - _gather(u_old, self.x, self.y - .5)
        dv = gv - _gather(v_old, self.x - .5, self.y)
        self.u = (pic * gu + (1 - pic) * (self.u + du)).astype(np.float32)
        self.v = (pic * gv + (1 - pic) * (self.v + dv)).astype(np.float32)
        # Runge-Kutta 2 im Gitterfeld.
        mx = self.x + .5 * dt * gu
        my = self.y + .5 * dt * gv
        self.x = (self.x + dt * _gather(u, mx, my - .5)).astype(np.float32)
        self.y = (self.y + dt * _gather(v, mx - .5, my)).astype(np.float32)
        gh, gw = self.shape
        self.x = np.clip(self.x, .01, gw - .01)
        hit = self.y > self.floor - .01
        self.y = np.clip(self.y, .01, self.floor - .01)
        self.v[hit] = np.minimum(self.v[hit], 0)
        self.u[hit] *= .9
        self._drain(dt, softness)

    def _drain(self, dt, softness):
        # Abfluss in die Bodenpfütze: Metall in der untersten Schicht läuft mit
        # einer Rate ab, die mit der Flüssigkeit wächst.
        # Abflussrate wächst mit dem hydrostatischen Druck, also mit der Höhe der
        # Metallsäule darüber (4 Partikel je Zelle).
        gh, gw = self.shape
        column = np.clip(self.x.astype(np.int32), 0, gw - 1)
        height = np.bincount(column, minlength=gw)[column] / 4.0
        band = self.y > self.floor - 2.0
        chance = dt * softness * (self.drain_base + self.drain_height * height)
        leave = band & (self._rng.random(self.x.size) < chance)
        moved = int(leave.sum())
        if moved:
            self.pool_count += moved
            self.pool_x_sum += float(self.x[leave].sum())
            keep = ~leave
            for name in ('x', 'y', 'ox', 'oy', 'u', 'v'):
                setattr(self, name, getattr(self, name)[keep])
        self.inflow += (moved / max(dt, 1e-4) - self.inflow) * min(1.0, dt * 2.5)

    def advance(self, until, softness_at, max_dt=1 / 45, stop=None):
        while self.time < until - 1e-6 and not (stop is not None and stop.is_set()):
            speed = float(np.max(np.abs(self.v))) if self.count else 0.0
            speed = max(speed, float(np.max(np.abs(self.u))) if self.count else 0.0)
            # CFL: höchstens gut eine Zelle pro Teilschritt.
            dt = min(max_dt, until - self.time, 1.2 / max(speed, 1e-3))
            self.step(dt, softness_at(self.time + dt))
            self.snapshot = self._snapshot()

    def fields(self, size, snapshot=None, supersample=2):
        """Dichte und Herkunftskoordinaten in Bildauflösung."""
        _, px, py, pox, poy = (snapshot or self.snapshot)[:5]
        width, height = size
        gh, gw = self.shape[0] * supersample, self.shape[1] * supersample
        x, y = px * supersample - .5, py * supersample - .5
        # Verschiebung statt absoluter Herkunft übertragen: ohne Bewegung exakt
        # null, also rauschfrei; in der Strömung glatt.
        shift_x = pox - (px * self.sx - .5)
        shift_y = poy - (py * self.sy - .5)
        sdx, weight = _splat(x, y, shift_x, (gh, gw))
        sdy, _ = _splat(x, y, shift_y, (gh, gw))
        blur = lambda f, sigma: cv2.GaussianBlur(f.astype(np.float32), (0, 0), sigma)
        w = blur(weight, .7 * supersample)
        wide = blur(weight, 1.5 * supersample)
        dx = blur(sdx, 1.5 * supersample) / np.maximum(wide, 1e-4)
        dy = blur(sdy, 1.5 * supersample) / np.maximum(wide, 1e-4)
        # 4 Partikel je Zelle = voll; weiche Kante über den Dichteabfall.
        density = w * (supersample ** 2) / 4.0
        up = lambda f: cv2.resize(f, (width, height), interpolation=cv2.INTER_LINEAR)
        grid_x, grid_y = np.meshgrid(np.arange(width, dtype=np.float32),
                                     np.arange(height, dtype=np.float32))
        return up(density), grid_x + up(dx), grid_y + up(dy)


# --- Eigener Prozess ------------------------------------------------------------
# Die Simulation braucht etwa einen halben Prozessorkern. In einem Thread würde
# sie über die GIL das Rendern ausbremsen; ein Prozess nutzt einen zweiten Kern.

def softness_curve(t, start, ramp):
    value = min(1.0, max(0.0, (t - start) / ramp))
    return value * value * (3 - 2 * value)


def _process_main(conn, started, start, ramp, fluid=None):
    import time as _time
    # Kleine Gitter: OpenCV-Threads kosten hier mehr als sie bringen und
    # konkurrieren mit dem Renderprozess um die Kerne.
    cv2.setNumThreads(1)
    try:
        sim = LiquidSheet(*conn.recv(), **(fluid or {}))
    except (EOFError, OSError):
        return
    soft = lambda t: softness_curve(t, start, ramp)
    try:
        while True:
            if conn.poll():
                if conn.recv() == 'stop':
                    return
            target = _time.monotonic() - started
            if sim.time < target - 1e-3:
                # Bis zur aktuellen Zeit rechnen, dann einmal senden. Die Leitung
                # fasst nur etwa einen Zustand; wer pro Teilschritt sendet, wird auf
                # die Bildrate des Empfängers gebremst.
                sim.advance(target, soft)
                conn.send(sim.snapshot)
                if sim.count == 0:
                    return           # alles Metall liegt in der Pfütze
            else:
                _time.sleep(.004)
    except (EOFError, BrokenPipeError, KeyboardInterrupt):
        return


class SimulationProcess:
    """Führt eine LiquidSheet-Simulation in Echtzeit in einem Nebenprozess aus."""

    def __init__(self, args, started, start, ramp, fluid=None):
        import multiprocessing as mp
        import threading
        context = mp.get_context('spawn')
        self._conn, child = context.Pipe()
        # Beim Start nur kleine Daten übergeben: Stirbt der Kindprozess vor dem
        # Lesen, blockierte ein großer Startdatensatz sonst für immer.
        self._process = context.Process(target=_process_main, name='liquid-metal-sim',
                                        args=(child, started, start, ramp, dict(fluid or {})),
                                        daemon=True)
        self._process.start()
        child.close()
        self.snapshot = None
        self.failed = False
        # Die Maske (einige hundert KB) aus einem Hilfsthread senden: Der Empfänger
        # liest erst nach seinem Start (≈ 1 s), das Rendern soll nicht warten.
        self._sender = threading.Thread(target=self._send_args, args=(args,), daemon=True)
        self._sender.start()

    def _send_args(self, args):
        try:
            self._conn.send(args)
        except (BrokenPipeError, OSError):
            self.failed = True

    def latest(self):
        """Neuesten Zustand abholen (ältere verwerfen), None solange keiner da ist."""
        try:
            while self._conn.poll():
                self.snapshot = self._conn.recv()
        except (EOFError, OSError):
            pass
        return self.snapshot

    def alive(self):
        return self._process.is_alive() and not self.failed

    def stop(self):
        self._sender.join(timeout=.5)
        try:
            if not self._sender.is_alive():
                self._conn.send('stop')
        except (BrokenPipeError, OSError):
            pass
        self._process.join(timeout=.5)
        if self._process.is_alive():
            self._process.terminate()
        self._conn.close()


# --- Vorausberechnung -----------------------------------------------------------
# Für Effekte, die eine Schmelze rückwärts zeigen, bevor sie vorwärts lief: so
# schnell wie möglich rechnen und die Zustände gebündelt schicken.

def _precompute_main(conn, start, ramp, fluid, frame_dt, limit):
    import time as _time
    cv2.setNumThreads(1)
    try:
        sim = LiquidSheet(*conn.recv(), **(fluid or {}))
    except (EOFError, OSError):
        return
    soft = lambda t: softness_curve(t, start, ramp)
    batch, last = [sim.snapshot], _time.monotonic()
    try:
        while True:
            if conn.poll() and conn.recv() == 'stop':
                return
            sim.advance(sim.time + frame_dt, soft)
            batch.append(sim.snapshot)
            finished = sim.count == 0 or sim.time >= limit
            if finished or _time.monotonic() - last > .25:
                conn.send(batch)
                batch, last = [], _time.monotonic()
            if finished:
                conn.send('done')
                return
    except (EOFError, BrokenPipeError, KeyboardInterrupt):
        return


class PrecomputeProcess:
    """Rechnet eine komplette Schmelze im Nebenprozess vorab (ohne Echtzeit-Takt)."""

    def __init__(self, args, start, ramp, fluid=None, frame_dt=1 / 30, limit=16.0):
        import multiprocessing as mp
        import threading
        context = mp.get_context('spawn')
        self._conn, child = context.Pipe()
        self._process = context.Process(target=_precompute_main, name='liquid-precompute',
                                        args=(child, start, ramp, dict(fluid or {}), frame_dt, limit),
                                        daemon=True)
        self._process.start()
        child.close()
        self.snapshots = []
        self.done = False
        self.failed = False
        self._sender = threading.Thread(target=self._send_args, args=(args,), daemon=True)
        self._sender.start()

    def _send_args(self, args):
        try:
            self._conn.send(args)
        except (BrokenPipeError, OSError):
            self.failed = True

    def collect(self):
        """Neu eingetroffene Zustände übernehmen; True, sobald alles da ist."""
        try:
            while not self.done and self._conn.poll():
                message = self._conn.recv()
                if message == 'done':
                    self.done = True
                else:
                    self.snapshots.extend(message)
        except (EOFError, OSError):
            pass
        return self.done

    def alive(self):
        return (self._process.is_alive() or self.done) and not self.failed

    def stop(self):
        self._sender.join(timeout=.5)
        try:
            if not self._sender.is_alive() and not self.done:
                self._conn.send('stop')
        except (BrokenPipeError, OSError):
            pass
        self._process.join(timeout=.5)
        if self._process.is_alive():
            self._process.terminate()
        self._conn.close()


def precompute_in_thread(args, start, ramp, fluid=None, frame_dt=1 / 30, limit=16.0):
    """Ersatz ohne Nebenprozess: gleiche Rechnung in einem Thread."""
    import threading
    job = type('PrecomputeThread', (), {})()
    job.snapshots, job.done, job.failed = [], False, False
    job._stop = threading.Event()

    def run():
        sim = LiquidSheet(*args, **(fluid or {}))
        soft = lambda t: softness_curve(t, start, ramp)
        job.snapshots.append(sim.snapshot)
        while not job._stop.is_set():
            sim.advance(sim.time + frame_dt, soft)
            job.snapshots.append(sim.snapshot)
            if sim.count == 0 or sim.time >= limit:
                job.done = True
                return

    job.collect = lambda: job.done
    job.alive = lambda: True
    job.stop = lambda: job._stop.set()
    threading.Thread(target=run, name='liquid-precompute', daemon=True).start()
    return job
