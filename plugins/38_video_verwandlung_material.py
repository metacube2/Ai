"""Verwandlungen in andere Stoffe: Gold, Glas, Lava, Honig.

Gleicher Ablauf wie Flüssigmetall II (bildeffekte/verwandlung.py): Person
einfrieren, gelernte Tiefe, FLIP-Strömung in eine Bodenpfütze, gemerkter
Hintergrund, danach rückwärts bis zum Startbild. Die Effekte unterscheiden sich
in der Oberfläche (`_surface`) und in den Stoffwerten der Strömung (`fluid`).
"""

import cv2
import numpy as np

from bildeffekte.verwandlung import FluidVerwandlung, smooth

_BEST = (" Beste Wirkung: kurz aus dem Bild gehen, Effekt wählen, dann ins Bild treten. "
         "Danach läuft alles rückwärts bis zum Startbild.")


def _fresnel(nz, f0):
    """Schlick-Näherung: Reflexionsgrad je nach Blickwinkel."""
    return f0 + (1 - f0) * ((1 - np.clip(nz, 0, 1)) ** 5)[..., None]


class _Transparent(FluidVerwandlung):
    """Gemeinsames für durchsichtige Stoffe: Brechung des Hintergrunds."""

    def _refract(self, s, strength, dispersion=0.0):
        # Brechung: Der Blick durch eine geneigte Fläche wird seitlich versetzt,
        # dickere Stellen versetzen stärker. Pro Farbkanal leicht verschieden.
        thickness = 1 + s.depth / max(1.0, self.bounds[2] * .2)
        out = []
        for channel, k in enumerate((1 + dispersion, 1.0, 1 - dispersion)):
            mx = (s.X - s.nx * strength * k * thickness).astype(np.float32)
            my = (s.Y - s.ny * strength * k * thickness).astype(np.float32)
            out.append(cv2.remap(self._background32[..., channel], mx, my, cv2.INTER_LINEAR,
                                 borderMode=cv2.BORDER_REFLECT))
        return np.dstack(out), thickness


class Fluessiggold(FluidVerwandlung):
    name = "Flüssiggold"
    description = "Die Person wird zu poliertem Gold und schmilzt in eine goldene Pfütze." + _BEST

    GOLD = np.float32((1.00, .77, .34))      # Reflexionsfarbe von Gold (RGB)

    def _surface(self, s):
        reflection, specular = self._reflection(s)
        if s.pool_on:
            reflection = self._pool_mirror(s.X, s.Y, reflection, specular, s.pool_weight, s.q,
                                           s.wave_x, s.wave_y, s.center, s.floor - s.ry, s.ry)
        tint = _fresnel(s.nz, self.GOLD)
        gold = reflection * tint
        # Warme, tiefe Schatten; helle Stellen weich begrenzt (kein Weißclipping).
        gold = gold * 1.15 + np.float32((10, 5, 0))
        return np.where(gold > 215, 215 + 40 * (1 - np.exp(-(gold - 215) / 70)), gold)


class Fluessigglas(_Transparent):
    name = "Flüssigglas"
    description = ("Die Person wird zu klarem Glas: Der Hintergrund scheint verzerrt hindurch, "
                   "dann zerfließt sie zu einer gläsernen Pfütze." + _BEST)
    fluid = dict(drag_solid=90.0, pic_solid=.4, tension=80.0)

    def _surface(self, s):
        reflection, specular = self._reflection(s)
        refracted, thickness = self._refract(s, 22.0, dispersion=.06)
        # Leichte grünliche Eigenfarbe, dicker = satter (wie Glaskanten).
        absorb = np.exp(-(thickness - 1)[..., None] * np.float32((.20, .06, .12)))
        transmitted = refracted * absorb * .96
        fres = _fresnel(s.nz, np.float32((.04, .04, .04)))
        glass = transmitted * (1 - fres) + reflection * fres * 1.3
        # Glanzlichter und dunkle Kanten (Totalreflexion an der Silhouette).
        glass += specular[..., None] * .9
        glass *= (1 - .35 * np.clip((1 - s.nz) * 2.5, 0, 1) ** 2)[..., None]
        return np.clip(glass, 0, 255)


class LavaSchmelze(FluidVerwandlung):
    name = "Lava-Schmelze"
    description = ("Die Person glüht auf und fließt als zähe Lava zu Boden; die Pfütze kühlt "
                   "zu dunkler Kruste mit glühenden Rissen ab." + _BEST)
    fluid = dict(drag_solid=150.0, drag_base=3.0, pic_base=.18, pic_solid=.6, tension=45.0,
                 drain_base=.8, drain_height=.10)
    melt_ramp = 5.5

    def _setup(self):
        super()._setup()
        h, w = self.scene.shape[:2]
        rng = np.random.default_rng(5)
        # Rissmuster im Material: schmale Grate zweier Rauschfelder.
        cracks = np.zeros((h, w), np.float32)
        for sigma, weight in ((max(2.0, w / 90), 1.0), (max(1.0, w / 220), .6)):
            field = cv2.GaussianBlur(rng.standard_normal((h, w)).astype(np.float32), (0, 0), sigma)
            field /= field.std() + 1e-6
            cracks = np.maximum(cracks, weight * np.exp(-(field / .22) ** 2))
        self._cracks = cracks
        self._emissive = np.zeros((h, w, 3), np.float32)

    @staticmethod
    def _glow_colour(t):
        """Glühfarbe nach Temperatur 0 … 1 (dunkelrot → orange → gelb → weiß)."""
        t = np.clip(t, 0, 1)[..., None]
        red = np.clip(t * 3.0, 0, 1)
        green = np.clip(t * 2.2 - .55, 0, 1)
        blue = np.clip(t * 3.0 - 2.1, 0, 1)
        return np.concatenate([red * 255, green * 205, blue * 170], axis=-1)

    def _surface(self, s):
        map_x, map_y = (m[s.crop] for m in self._frame_maps)
        cracks = cv2.remap(self._cracks, map_x, map_y, cv2.INTER_LINEAR)
        heat = float(smooth((s.elapsed - .3) / 2.2))
        # Beim Fließen bricht die Kruste auf, in der Pfütze erstarrt sie wieder.
        flowing = self._softness_at(s.elapsed)
        cooling = float(smooth((s.elapsed - 6.0) / 6.0))
        pool = s.pool_weight
        thick = np.clip(s.depth / max(1.0, self.bounds[2] * .12), 0, 1)
        # Offene Glut: Risse immer, dazu breite Bahnen solange es fließt; dicke
        # Stellen bleiben länger heiß.
        open_melt = np.clip(cracks * 1.3 + (flowing * (1 - cooling * pool)) * (.35 + .5 * thick), 0, 1)
        shimmer = .9 + .1 * np.sin(s.X * .09 + s.Y * .06 - s.elapsed * 2.3)
        temperature = heat * open_melt * shimmer
        temperature = temperature * (1 - pool * cooling * .6)
        emissive = self._glow_colour(temperature * 1.1)
        # Kruste: dunkles, raues Gestein, das die Umgebung matt spiegelt; mit
        # rötlichem Durchscheinen, solange sie heiß ist.
        reflection, specular = self._reflection(s)
        grey = reflection.mean(axis=2, keepdims=True)
        crust = grey * .22 + np.float32((20, 16, 14)) + specular[..., None] * .35
        crust += np.float32((90, 18, 0)) * (heat * (1 - cooling * .7) * .35)
        crust_amount = np.clip(1 - temperature * 2.2, 0, 1)[..., None]
        lava = crust * crust_amount + emissive * (1 - crust_amount * .85)
        self._emissive[s.crop] = emissive * (1 - crust_amount)
        return lava

    def _finish(self, result, alpha, elapsed, mix):
        # Glut beleuchtet die Umgebung: weicher, großer Lichthof.
        h, w = result.shape[:2]
        small = (max(1, w // 4), max(1, h // 4))
        glow = cv2.resize(self._emissive * alpha[..., None], small, interpolation=cv2.INTER_AREA)
        glow = cv2.GaussianBlur(glow, (0, 0), max(1.0, w / 60))
        self._emissive[:] = 0
        return result + cv2.resize(glow, (w, h)) * .55 * mix


class HonigSchmelze(_Transparent):
    name = "Honig-Schmelze"
    description = ("Die Person wird zu goldgelbem, durchscheinendem Honig und sinkt sehr zäh "
                   "in sich zusammen." + _BEST)
    fluid = dict(drag_solid=200.0, drag_base=5.0, pic_base=.45, pic_solid=.5, tension=90.0,
                 drain_base=.5, drain_height=.08)
    melt_ramp = 6.5

    def _surface(self, s):
        reflection, specular = self._reflection(s)
        refracted, thickness = self._refract(s, 14.0)
        # Bernstein: Blau wird stark geschluckt, Rot kaum; dickere Stellen dunkler.
        absorb = np.exp(-thickness[..., None] * np.float32((.10, .45, 1.6)))
        body = refracted * absorb * np.float32((1.05, .95, .9))
        body += np.float32((70, 32, 2)) * (1 - np.exp(-thickness[..., None] * .7))   # Streulicht
        fres = _fresnel(s.nz, np.float32((.05, .05, .05)))
        honey = body * (1 - fres) + reflection * fres
        honey += specular[..., None] * 1.1
        return np.clip(honey, 0, 255)
