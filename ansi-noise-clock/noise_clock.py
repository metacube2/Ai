#!/usr/bin/env python3
"""ANSI Noise Clock.

Zeigt HH:MM:SS als geordnetes Rauschen in einem Terminal-Raster:
ausserhalb der Ziffern duennes, dunkles Rauschen, innerhalb der Ziffern
dichtes, helles Rauschen. Die Sekundengruppe zoomt bei jedem Sekundentakt
auf und faellt weich in die Normalposition zurueck.

Reines Python 3 ohne Fremdpakete. 256-Farben-ANSI.
"""

from __future__ import annotations

import argparse
import random
import shutil
import signal
import sys
import time

# --------------------------------------------------------------------------
# Ziffernmasken, 5x7 (':' ist 2 breit)
# --------------------------------------------------------------------------

GLYPH_H = 7

_RAW = {
    "0": [".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###."],
    "1": ["..#..", ".##..", "..#..", "..#..", "..#..", "..#..", ".###."],
    "2": [".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####"],
    "3": ["####.", "....#", "....#", ".###.", "....#", "....#", "####."],
    "4": ["...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#."],
    "5": ["#####", "#....", "####.", "....#", "....#", "#...#", ".###."],
    "6": ["..##.", ".#...", "#....", "####.", "#...#", "#...#", ".###."],
    "7": ["#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#..."],
    "8": [".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###."],
    "9": [".###.", "#...#", "#...#", ".####", "....#", "...#.", ".##.."],
    ":": ["..", "##", "##", "..", "##", "##", ".."],
    " ": ["..", "..", "..", "..", "..", "..", ".."],
}

# Masken als Tupel aus Bitzeilen, damit das Sampling billig bleibt.
GLYPHS = {
    ch: (len(rows[0]), tuple(tuple(c == "#" for c in row) for row in rows))
    for ch, rows in _RAW.items()
}

# --------------------------------------------------------------------------
# Zeichensaetze fuer das Rauschen
# --------------------------------------------------------------------------

# innerhalb der Ziffern: dicht
DENSE = "███▓▓▒░"
# ausserhalb: duenn, mit viel Leerraum
SPARSE = "·.:'`,˚·"

# 256-Farben-Rampen: (hintergrundrauschen, ziffernrauschen)
PALETTES = {
    "green": ([234, 235, 236, 238], [22, 28, 34, 40, 46, 82, 118, 155]),
    "amber": ([234, 235, 236, 238], [58, 94, 130, 166, 172, 214, 220, 228]),
    "ice":   ([234, 235, 236, 238], [17, 18, 24, 31, 38, 45, 51, 123]),
    "mono":  ([234, 235, 236, 237], [240, 244, 247, 250, 252, 253, 255, 255]),
    "plasma": ([234, 235, 236, 238], [53, 90, 127, 164, 201, 207, 213, 219]),
}

# Zellzustaende
OFF, ON_FIX, ON_SEC = 0, 1, 2

CSI = "\x1b["


# --------------------------------------------------------------------------
# Layout
# --------------------------------------------------------------------------

class Layout:
    """Positionen der acht Glyphen von HH:MM:SS bei Basisskalierung."""

    def __init__(self, cols: int, rows: int, pattern: str):
        self.pattern = pattern
        self.cols = cols
        self.rows = rows
        self.scale = self._fit(cols, rows, pattern)
        self.boxes = self._place(self.scale)

    @staticmethod
    def _width_at(pattern: str, scale: float) -> tuple[int, int]:
        gap = max(1, round(scale))
        total = 0
        for i, ch in enumerate(pattern):
            gw = GLYPHS[ch][0]
            total += max(1, round(gw * scale))
            if i:
                total += gap
        return total, max(1, round(GLYPH_H * scale))

    def _fit(self, cols: int, rows: int, pattern: str) -> float:
        # groesste Skalierung, die mit Rand in das Raster passt
        best = 0.6
        s = 0.6
        while s < 40:
            w, h = self._width_at(pattern, s)
            if w > cols - 2 or h > rows - 2:
                break
            best = s
            s += 0.1
        return round(best, 2)

    def _place(self, scale: float) -> list[tuple[int, int, int, int]]:
        gap = max(1, round(scale))
        widths = [max(1, round(GLYPHS[c][0] * scale)) for c in self.pattern]
        height = max(1, round(GLYPH_H * scale))
        total = sum(widths) + gap * (len(widths) - 1)
        x = (self.cols - total) // 2
        y = (self.rows - height) // 2
        boxes = []
        for w in widths:
            boxes.append((x, y, w, height))
            x += w + gap
        return boxes


def pattern_for(fmt: str) -> str:
    """Platzhaltermuster fuer das Layout: jede Ziffer wird zur breitesten Ziffer."""
    stamp = time.strftime(fmt, time.localtime(0))
    out = []
    for ch in stamp:
        if ch.isdigit():
            out.append("8")
        elif ch in GLYPHS:
            out.append(ch)
        else:
            out.append(" ")
    return "".join(out) or " "


def draw_glyph(cells, cols, rows, ch, x0, y0, w, h, state) -> None:
    """Zeichnet eine Maske ratiobasiert (nicht per Ganzzahl-Wiederholung)."""
    gw, mask = GLYPHS[ch]
    y_start = max(0, -y0)
    y_end = min(h, rows - y0)
    x_start = max(0, -x0)
    x_end = min(w, cols - x0)
    for gy in range(y_start, y_end):
        sy = mask[gy * GLYPH_H // h]
        base = (y0 + gy) * cols + x0
        for gx in range(x_start, x_end):
            if sy[gx * gw // w]:
                cells[base + gx] = state


# --------------------------------------------------------------------------
# Uhr
# --------------------------------------------------------------------------

class NoiseClock:
    def __init__(self, args):
        self.args = args
        self.rnd = random.Random()
        self.cols = self.rows = 0
        self.layout = None
        self.noise: list[float] = []
        self.pattern = pattern_for(args.format)
        self.dim, self.bright = PALETTES[args.palette]

    # -- Groesse -----------------------------------------------------------

    def ensure_size(self) -> None:
        size = shutil.get_terminal_size(fallback=(80, 24))
        cols, rows = max(20, size.columns), max(9, size.lines)
        if (cols, rows) == (self.cols, self.rows):
            return
        self.cols, self.rows = cols, rows
        self.layout = Layout(cols, rows, self.pattern)
        self.noise = [self.rnd.random() for _ in range(cols * rows)]
        sys.stdout.write(CSI + "2J")

    # -- Animation ---------------------------------------------------------

    def pulse(self, now: float) -> float:
        """1.0 exakt auf dem Sekundentakt, danach weiche Rueckkehr auf 0."""
        frac = now % 1.0
        decay = self.args.decay
        if frac >= decay:
            return 0.0
        t = 1.0 - frac / decay
        return t * t * (3 - 2 * t)  # smoothstep

    def seconds_scale(self, pulse: float) -> float:
        base = self.layout.scale
        # Zoomfaktor an der Terminalhoehe kappen, sonst laeuft die Ziffer aus
        max_by_height = self.rows / (GLYPH_H * base)
        zoom = min(self.args.zoom, max(1.0, max_by_height))
        return base * (1.0 + (zoom - 1.0) * pulse)

    # -- Rendern -----------------------------------------------------------

    def build_cells(self, now: float, pulse: float) -> bytearray:
        cols, rows = self.cols, self.rows
        cells = bytearray(cols * rows)
        stamp = [c if c in GLYPHS else " "
                 for c in time.strftime(self.args.format, time.localtime(now))]
        if len(stamp) != len(self.pattern):   # z. B. Formatwechsel zur Laufzeit
            stamp = (stamp + [" "] * len(self.pattern))[:len(self.pattern)]
        boxes = self.layout.boxes

        # feste Gruppe HH:MM: (alles vor der Sekundengruppe)
        for i in range(len(self.pattern) - 2):
            draw_glyph(cells, cols, rows, stamp[i], *boxes[i], ON_FIX)

        # Sekundengruppe: eigene Skalierung, um das eigene Zentrum expandiert
        scale = self.seconds_scale(pulse)
        i0 = len(self.pattern) - 2
        gap = max(1, round(scale))
        widths = [max(1, round(GLYPHS[c][0] * scale)) for c in self.pattern[i0:]]
        height = max(1, round(GLYPH_H * scale))
        group_w = sum(widths) + gap * (len(widths) - 1)

        bx, by, bw, bh = boxes[i0]
        last = boxes[-1]
        # Zoomzentrum wandert mit dem Puls zur Bildmitte und wieder zurueck in
        # die Normalposition - sonst klemmt die wachsende Ziffer am Rand fest.
        gx = (bx + last[0] + last[2]) / 2.0
        gy = by + bh / 2.0
        cx = gx + (cols / 2.0 - gx) * pulse
        cy = gy + (rows / 2.0 - gy) * pulse
        x = int(round(cx - group_w / 2.0))
        y = int(round(cy - height / 2.0))
        if group_w <= cols:
            x = max(0, min(x, cols - group_w))
        if height <= rows:
            y = max(0, min(y, rows - height))

        for ch, w in zip(stamp[i0:], widths):
            draw_glyph(cells, cols, rows, ch, x, y, w, height, ON_SEC)
            x += w + gap
        return cells

    def stir_noise(self) -> None:
        """Nur einen Teil der Zellen neu wuerfeln: lebendig, aber nicht hektisch."""
        n = len(self.noise)
        count = int(n * self.args.churn)
        rnd = self.rnd
        noise = self.noise
        for _ in range(count):
            noise[rnd.randrange(n)] = rnd.random()

    def frame(self, cells: bytearray, pulse: float) -> str:
        cols, rows = self.cols, self.rows
        noise = self.noise
        dim, bright = self.dim, self.bright
        dense_n, sparse_n = len(DENSE), len(SPARSE)
        nb = len(bright)
        # Schockwelle: der Hintergrund verdichtet sich kurz beim Takt
        gate = self.args.density + 0.22 * pulse
        out = []
        push = out.append
        for y in range(rows):
            push(f"{CSI}{y + 1};1H")
            cur = -1
            row_base = y * cols
            # Farbe pro Zeile vorberechnen -> wenige SGR-Wechsel je Zeile
            t = y / max(1, rows - 1)
            c_dim = dim[int(t * (len(dim) - 1) + 0.5)]
            lo = bright[min(nb - 1, int(t * (nb - 2)))]
            hi = bright[min(nb - 1, int(t * (nb - 2)) + 1 + int(3 * pulse))]
            width = cols - 1 if y == rows - 1 else cols
            for x in range(width):
                idx = row_base + x
                v = noise[idx]
                state = cells[idx]
                if state:
                    ch = DENSE[int(v * dense_n)]
                    col = hi if (state == ON_SEC or v > 0.45) else lo
                else:
                    if v > gate:
                        ch = " "
                        col = c_dim
                    else:
                        ch = SPARSE[int(v / gate * sparse_n) % sparse_n]
                        col = c_dim
                if col != cur:
                    push(f"{CSI}38;5;{col}m")
                    cur = col
                push(ch)
        return "".join(out)

    # -- Schleife ----------------------------------------------------------

    def run(self, deadline: float | None = None) -> None:
        period = 1.0 / self.args.fps
        nxt = time.time()
        while deadline is None or time.time() < deadline:
            self.ensure_size()
            now = time.time()
            pulse = self.pulse(now)
            self.stir_noise()
            cells = self.build_cells(now, pulse)
            sys.stdout.write(self.frame(cells, pulse))
            sys.stdout.flush()
            nxt += period
            delay = nxt - time.time()
            if delay > 0:
                time.sleep(delay)
            else:
                nxt = time.time()   # nach Last oder Sleep neu synchronisieren


# --------------------------------------------------------------------------
# Terminalzustand
# --------------------------------------------------------------------------

def enter_screen(alt: bool) -> None:
    if alt:
        sys.stdout.write(CSI + "?1049h")
    sys.stdout.write(CSI + "?25l" + CSI + "2J")
    sys.stdout.flush()


def leave_screen(alt: bool) -> None:
    sys.stdout.write(CSI + "0m" + CSI + "?25h")
    if alt:
        sys.stdout.write(CSI + "?1049l")
    else:
        sys.stdout.write(CSI + "2J" + CSI + "H")
    sys.stdout.flush()


def parse_args(argv=None):
    p = argparse.ArgumentParser(
        description="ANSI-Uhr als geordnetes Rauschen mit Sekundenzoom.")
    p.add_argument("--fps", type=float, default=30.0, help="Bilder pro Sekunde")
    p.add_argument("--zoom", type=float, default=2.6,
                   help="maximaler Sekundenzoom relativ zur Normalgroesse")
    p.add_argument("--decay", type=float, default=0.55,
                   help="Anteil der Sekunde fuer den Rueckweg (0.05-1.0)")
    p.add_argument("--churn", type=float, default=0.30,
                   help="Anteil der Zellen, die pro Bild neu rauschen")
    p.add_argument("--density", type=float, default=0.34,
                   help="Dichte des Hintergrundrauschens (0-1)")
    p.add_argument("--palette", choices=sorted(PALETTES), default="green")
    p.add_argument("--format", default="%H:%M:%S",
                   help="strftime-Format, muss HH:MM:SS-Form behalten")
    p.add_argument("--seconds", type=float, default=0.0,
                   help="nach n Sekunden beenden (0 = endlos)")
    p.add_argument("--no-alt-screen", action="store_true",
                   help="im aktuellen Puffer zeichnen (Debug)")
    a = p.parse_args(argv)
    a.fps = min(max(a.fps, 1.0), 120.0)
    a.decay = min(max(a.decay, 0.05), 1.0)
    a.churn = min(max(a.churn, 0.0), 1.0)
    a.density = min(max(a.density, 0.02), 0.95)
    a.zoom = max(1.0, a.zoom)
    return a


def main(argv=None) -> int:
    args = parse_args(argv)
    alt = not args.no_alt_screen
    deadline = time.time() + args.seconds if args.seconds > 0 else None

    def on_signal(signum, frame):
        raise KeyboardInterrupt

    for sig in (signal.SIGINT, signal.SIGTERM):
        signal.signal(sig, on_signal)

    enter_screen(alt)
    try:
        NoiseClock(args).run(deadline)
    except KeyboardInterrupt:
        pass
    finally:
        leave_screen(alt)
    return 0


if __name__ == "__main__":
    sys.exit(main())
