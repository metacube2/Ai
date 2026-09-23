"""Matrix-Agent: lokales Live-Gesichtsmorphing mit fotografischem Zielgesicht."""
import json
import time
from pathlib import Path

import cv2
import numpy as np
from scipy.spatial import Delaunay

from bildeffekte import IEffectStrategy
from bildeffekte.face_morph import submit_face, warp_mesh, track_points, detect_face
from bildeffekte.verwandlung import smooth


def rim_limit(points, shape):
    """Maske darf nie über das verformte Mesh hinausreichen."""
    limit = np.zeros(shape,np.uint8)
    cv2.fillConvexPoly(limit,np.rint(points[:20]).astype(np.int32),255)
    return limit


class MatrixAgent(IEffectStrategy):
    name = 'Matrix-Agent – Gesichtsmorph'
    category = 'Verwandlungen'
    supports_target_image = True
    supports_drama = True
    DETECTION_GAIN = .3
    # Dramatik-Modus (Referenz morph.mp4): wandernde Schwellungen unter der Haut, u/v in
    # Gesichtsbreiten ab Gesichtsmitte (u quer, v nach unten). Pro Beule: Start, Ziel,
    # Zeitfenster im Fortschritt, Radius, Aufblähung, Schub entlang der Bahn.
    DRAMA_BULGES = (
        ((0.00, 0.55), (0.02, -0.10), (0.10, 0.80), 0.30, 0.55, 0.12),   # Pilot: Welle vom Kinn über das Gesicht
        ((0.30, 0.35), (0.22, -0.05), (0.20, 0.75), 0.20, 0.45, 0.08),   # rechte Wange steigt auf
        ((-0.32, 0.25), (-0.18, 0.10), (0.30, 0.90), 0.19, 0.45, 0.07),  # linke Wange zieht zur Mitte
        ((0.00, -0.05), (0.00, 0.30), (0.05, 0.45), 0.24, -0.35, 0.00),  # Mittelgesicht sackt zusammen
    )
    description = ('Live-Gesicht wird zum Agenten: Wangen, Kiefer und Mund verziehen sich, '
                   'dann erscheinen Agentengesicht und Sonnenbrille. Automatischer Rückweg. '
                   'Am besten frontal, gut beleuchtet und mit ruhigem Kopf.')

    def __init__(self):
        asset = Path(__file__).resolve().parents[1] / 'bildeffekte' / 'assets'
        raw = cv2.imread(str(asset / 'matrix_agent.png'))
        if raw is None:
            raise FileNotFoundError('matrix_agent.png fehlt')
        self.agent = cv2.cvtColor(cv2.resize(raw, (512, 512)), cv2.COLOR_BGR2RGB)
        info = json.loads((asset / 'matrix_agent.json').read_text())
        self.target = np.float32(info['points']) * np.float32([512/info['size'][0],512/info['size'][1]])
        self.triangles = Delaunay(self.target).simplices
        self._default_target = (self.agent, self.target, self.triangles)
        self._custom_target = None
        self._pending = None
        self._reset_requested = True
        self._shape = None
        self._points = None
        self._gray = None
        self._submitted_gray = None
        self._started = None
        self._seen = 0.0
        self._submitted = 0.0
        self._drama = False

    @staticmethod
    def prepare_target_image(path):
        """Aufruf im UI-Hintergrundworker; liefert nur validierte Zielbilder."""
        raw = cv2.imread(str(path))
        if raw is None:
            raise ValueError('Das JPG konnte nicht gelesen werden.')
        h,w = raw.shape[:2]
        scale = min(1., 768/max(h,w))
        image = cv2.cvtColor(cv2.resize(raw,(max(1,round(w*scale)),max(1,round(h*scale)))),cv2.COLOR_BGR2RGB)
        points = detect_face(image)
        if points is None:
            raise ValueError('Kein frontales Gesicht erkannt. Bitte ein anderes JPG wählen.')
        return image, points, Delaunay(points).simplices

    def activate(self):
        self._reset_requested = True

    def deactivate(self):
        self._reset_requested = True
        if self._pending is not None:
            self._pending.cancel()

    def _reset(self, shape):
        if self._pending is not None:
            self._pending.cancel()
        self._pending = None
        self._points = None
        self._gray = None
        self._submitted_gray = None
        self._started = None
        self._seen = self._submitted = 0.0
        self._shape = shape
        self._reset_requested = False

    @staticmethod
    def _progress(elapsed, drama=False):
        t = elapsed % 10.0
        if drama:
            # Längerer Kampf: 1 s Original, 4,2 s Übernahme, kurz halten, schneller Rückweg.
            if t < 1:
                return 0.0
            if t < 5.2:
                return float(smooth((t-1)/4.2))
            if t < 7:
                return 1.0
            if t < 9:
                return float(1-smooth((t-7)/2))
            return 0.0
        # Eine Sekunde Original, 2,6 Sekunden Übernahme, halten, Rückweg.
        if t < 1:
            return 0.0
        if t < 3.6:
            return float(smooth((t-1)/2.6))
        if t < 6:
            return 1.0
        if t < 8.6:
            return float(1-smooth((t-6)/2.6))
        return 0.0

    def apply(self, frame, config):
        drama = bool(config.get('effect_options', {}).get(self.name, {}).get('drama', False))
        if drama != self._drama:
            self._drama = drama
            self._started = None   # Umschalten startet die Schleife neu, damit die ganze Übernahme zu sehen ist
        selected = config.get('target_images', {}).get(self.name)
        if selected is not self._custom_target:
            self._custom_target = selected
            self.agent, self.target, self.triangles = self._default_target if selected is None else selected
            self._reset_requested = True
        h, w = frame.shape[:2]
        scale = min(1., 512/max(h,w))
        size = (max(1,round(w*scale)), max(1,round(h*scale)))
        if self._reset_requested or self._shape != frame.shape:
            self._reset(frame.shape)
        if min(size) < 64:
            return frame
        small = cv2.resize(frame,size,interpolation=cv2.INTER_AREA)
        gray = cv2.cvtColor(small,cv2.COLOR_RGB2GRAY)
        now = time.monotonic()
        if self._points is not None and self._gray is not None:
            self._points = track_points(self._gray,gray,self._points)
            if self._points is None:
                self._started = None
        self._gray = gray
        if self._pending is not None and self._pending.done():
            try:
                points = self._pending.result()
            except Exception:
                points = None
            self._pending = None
            if points is None or now-self._submitted > .7:
                self._points = None
                self._started = None
            else:
                if self._submitted_gray is not None:
                    tracked = track_points(self._submitted_gray,gray,points)
                    if tracked is not None:
                        points = tracked
                if self._points is not None:
                    jump = float(np.linalg.norm(points[20:22].mean(0)-self._points[20:22].mean(0)))
                    width = float(np.linalg.norm(points[20]-points[21]))
                    if jump < width*.6:
                        # Bewegung liefert der optische Fluss; die Erkennung korrigiert nur Drift.
                        # Voll übernommen würde ihr Rauschen das Gesicht ~11-mal pro Sekunde verreißen.
                        points = self._points + (points-self._points)*self.DETECTION_GAIN
                    else:
                        self._started = None
                self._points = points
                self._seen = now
                if self._started is None:
                    self._started = now
        if self._pending is None and now-self._submitted >= .09:
            self._submitted = now
            self._submitted_gray = gray.copy()
            self._pending = submit_face(small)
        if self._points is None or now-self._seen > .5 or self._started is None:
            return frame
        progress = self._progress(now-self._started,self._drama)
        if progress <= 0:
            return frame
        result = self.render(small,self._points,progress,self._drama)
        return cv2.resize(result,(w,h),interpolation=cv2.INTER_LINEAR)

    def render(self, frame, points, progress, drama=False):
        if progress <= 0:
            return frame.copy()
        h,w = frame.shape[:2]
        # Dramatik: Schwellungen reichen über die Gesichtskontur hinaus
        margin = max(8.,float(np.ptp(points[:,0]))*(.45 if drama else .2))
        low = np.floor(points.min(0)-margin).astype(int)
        high = np.ceil(points.max(0)+margin).astype(int)
        x0,y0 = max(0,low[0]),max(0,low[1])
        x1,y1 = min(w,high[0]),min(h,high[1])
        if x1-x0 < 12 or y1-y0 < 12:
            return frame.copy()
        out = frame.copy()
        out[y0:y1,x0:x1] = self._render_face(frame[y0:y1,x0:x1],
                                            np.float32(points-[x0,y0]),progress,drama)
        return out

    @staticmethod
    def _inner_mask(points, shape, width):
        """Nur Hautfläche: Die Rim-Ellipse reicht bis in Haare, Ohren und Kragen."""
        center = (points[20:22].mean(0)+points[26])/2
        angles = np.arange(20)*2*np.pi/20
        # Seiten und Kinn leicht, Stirn stärker einziehen (Haaransatz bleibt echt).
        shrink = .80-.13*np.clip(-np.sin(angles),0,1)**2
        rim = center+(points[:20]-center)*shrink[:,None]
        mask = np.zeros(shape,np.uint8)
        cv2.fillPoly(mask,[np.rint(rim).astype(np.int32)],255)
        # Brillenband breiter, damit die Gläser an den Schläfen nicht verwischen.
        eyes = points[20:22]
        span = float(np.linalg.norm(eyes[1]-eyes[0]))
        angle = float(np.degrees(np.arctan2(*(eyes[1]-eyes[0])[::-1])))
        cv2.ellipse(mask,(tuple(np.rint(eyes.mean(0)).astype(int).tolist()),
                          (span*3.0,span*.75),angle),255,-1)
        mask[rim_limit(points,shape) == 0] = 0
        # Nach innen weich auslaufen lassen, damit keine Stanzkante entsteht.
        edge = cv2.distanceTransform(mask,cv2.DIST_L2,3)
        return smooth(edge/max(3.,width*.14)).astype(np.float32)

    @staticmethod
    def _match_skin(agent, frame, weight):
        """Hautton und Helligkeit des Kamerabilds im Lab-Raum übernehmen."""
        region = weight > .5
        if region.sum() < 30:
            return agent
        a_lab = cv2.cvtColor(np.clip(agent,0,255).astype(np.uint8),cv2.COLOR_RGB2LAB).astype(np.float32)
        f_lab = cv2.cvtColor(frame,cv2.COLOR_RGB2LAB).astype(np.float32)
        a_px, f_px = a_lab[region], f_lab[region]
        # Brille, Schatten, Zähne und Glanzlichter aus der Statistik nehmen.
        a_skin = a_px[a_px[:,0] > np.percentile(a_px[:,0],30)]
        lo, hi = np.percentile(f_px[:,0],(15,95))
        f_skin = f_px[(f_px[:,0] >= lo) & (f_px[:,0] <= hi)]
        if len(a_skin) < 10 or len(f_skin) < 10:
            return agent
        a_mean, f_mean = a_skin.mean(0), f_skin.mean(0)
        # Helligkeit multiplikativ, damit Schwarz (Brille) schwarz bleibt.
        a_lab[...,0] *= np.clip(f_mean[0]/max(a_mean[0],1),.45,1.6)
        spread = np.clip(f_skin[:,1:].std(0)/np.maximum(a_skin[:,1:].std(0),1),.6,1.6)
        skinlike = np.clip(a_lab[...,:1]/max(a_mean[0],1),0,1)
        a_lab[...,1:] = a_lab[...,1:]+((a_lab[...,1:]-a_mean[1:])*(spread-1)+f_mean[1:]-a_mean[1:])*skinlike
        return cv2.cvtColor(np.clip(a_lab,0,255).astype(np.uint8),cv2.COLOR_LAB2RGB).astype(np.float32)

    def _bulge_warp(self, image, center, axis, down, width, p):
        """
        Dichte Verformung für den Dramatik-Modus: Beulen wandern unter der Haut,
        blähen sie auf und schieben Wülste vor sich her. Zum Rand hin auf null.
        """
        h, w = image.shape[:2]
        # Das Feld ist weich: in halber Auflösung rechnen, dann hochskalieren (halbiert die Rechenzeit)
        hs, ws = (h+1)//2, (w+1)//2
        yy, xx = np.indices((hs, ws), dtype=np.float32)*2
        dx, dy = xx-center[0], yy-center[1]
        u, v = (dx*axis[0]+dy*axis[1])/width, (dx*down[0]+dy*down[1])/width
        su, sv = np.zeros_like(u), np.zeros_like(v)   # Abtastversatz in Gesichtsbreiten
        # Die alte Person wehrt sich: die Schwellungen zucken
        fight = 1+.35*np.sin(2*np.pi*(p*6.5))
        for (u0, v0), (u1, v1), (t0, t1), radius, swell, push in self.DRAMA_BULGES:
            t = (p-t0)/(t1-t0)
            if not 0 < t < 1:
                continue
            envelope = np.sin(np.pi*t)**1.5*fight
            k = float(smooth(t))
            cu, cv = u0+(u1-u0)*k, v0+(v1-v0)*k
            ru, rv = u-cu, v-cv
            g = np.exp(-(ru*ru+rv*rv)/(2*radius*radius))
            # Aufblähen: Abtastung zur Beulenmitte ziehen (Lupe)
            su -= ru*swell*g*envelope
            sv -= rv*swell*g*envelope
            # Schub: Inhalt wandert mit der Beule, dahinter Wülste wie in der Pilotszene
            path = np.float32([u1-u0, v1-v0])
            length = max(float(np.linalg.norm(path)), 1e-5)
            pu, pv = path/length
            along = ru*pu+rv*pv
            ridge = push*g*envelope*(1+.6*np.sin(along/radius*4))   # 2-3 breite Wülste
            su -= pu*ridge
            sv -= pv*ridge
        edge = np.minimum.reduce([xx, yy, w-1-xx, h-1-yy])
        # Nur Gesicht und Kiefer verformen, Helm, Haare und Hintergrund bleiben ruhig
        face = np.sqrt((u/.62)**2+((v-.1)/.78)**2)
        taper = np.clip(edge/max(width*.12, 3.), 0, 1)*np.clip((1.25-face)/.35, 0, 1)
        su, sv = su*taper*width, sv*taper*width
        shift_x = cv2.resize(su*axis[0]+sv*down[0], (w, h), interpolation=cv2.INTER_LINEAR)
        shift_y = cv2.resize(su*axis[1]+sv*down[1], (w, h), interpolation=cv2.INTER_LINEAR)
        full_y, full_x = np.indices((h, w), dtype=np.float32)
        map_x, map_y = full_x+shift_x, full_y+shift_y
        return cv2.remap(image, map_x, map_y, cv2.INTER_LINEAR, borderMode=cv2.BORDER_REFLECT_101)

    def _render_face(self, frame, points, progress, drama=False):
        if progress <= 0:
            return frame.copy()
        p = float(np.clip(progress,0,1))
        source = np.float32(points)
        # Augen bestimmen Drehung und Maßstab des Agenten.
        ta, tb = self.target[20:22]
        sa, sb = source[20:22]
        tv, sv = tb-ta, sb-sa
        a = np.dot(tv,sv)/np.dot(tv,tv)
        b = (tv[0]*sv[1]-tv[1]*sv[0])/np.dot(tv,tv)
        matrix = np.float32([[a,-b],[b,a]])
        aligned = (self.target-ta)@matrix.T+sa
        aligned[:20] = source[:20]  # Übergang an Stirn und Kiefer an die echte Silhouette binden.
        # Nase und Mund halb am echten Gesicht: Proportionen bleiben menschlich, Mund folgt.
        aligned[22:] = aligned[22:]*.5+source[22:]*.5
        destination = source*(1-p)+aligned*p
        axis = sv/max(float(np.linalg.norm(sv)),1e-5)
        down = np.float32([-axis[1],axis[0]])
        width = float(np.linalg.norm(source[0]-source[10]))
        surge = np.sin(np.pi*p)**2
        # Referenz morph.mp4: leichtes asynchrones Ausbeulen, dann Einrasten.
        # Stärkere Werte lassen Kiefer und Mund schnauzenartig wirken.
        for i in range(20):
            angle = i*2*np.pi/20
            destination[i] += axis*(np.cos(angle)*width*.045*surge*(1+.3*np.sin(angle*3+p*5)))
        destination[22] += axis*width*.03*np.sin(p*np.pi*2)*surge
        destination[23:28] += down*width*.035*surge
        destination[23] -= axis*width*.03*surge
        destination[25] += axis*width*.03*surge
        if drama:
            # Referenz: Untergesicht wird breit, Stirn staucht sich, der Mund reißt auf
            # und die Winkel werden weit auseinandergezogen.
            struggle = surge*(1+.3*np.sin(2*np.pi*p*6.5))
            for i in range(20):
                angle = i*2*np.pi/20
                lower = np.clip(np.sin(angle),0,1)   # Kinn und Kiefer
                upper = np.clip(-np.sin(angle),0,1)  # Stirn
                destination[i] += axis*np.cos(angle)*width*(.14*lower-.05*upper)*struggle
                destination[i] += down*width*(.10*lower+.06*upper)*struggle
            destination[22] += axis*width*.07*np.sin(p*np.pi*3)*struggle
            destination[23] += (-axis*.17+down*.06)*width*struggle
            destination[25] += (axis*.17+down*.06)*width*struggle
            destination[26] -= down*width*.04*struggle
            destination[24] += down*width*.07*struggle
            destination[27] += down*width*.16*struggle
        original = warp_mesh(frame,source,destination,self.triangles,frame.shape[:2]).astype(np.float32)
        agent = warp_mesh(self.agent,self.target,destination,self.triangles,frame.shape[:2]).astype(np.float32)
        rim = np.zeros(frame.shape[:2],np.uint8)
        cv2.fillConvexPoly(rim,np.rint(destination[:20]).astype(np.int32),255)
        # Verformung am Mesh-Rand weich in das unveränderte Bild überführen.
        warp_alpha = smooth(cv2.distanceTransform(rim,cv2.DIST_L2,3)/max(3.,width*.08))
        face = self._inner_mask(destination,frame.shape[:2],width)
        agent = self._match_skin(agent,frame,face)
        # Kamera ist weicher als die Studiovorlage: Schärfe angleichen.
        agent = cv2.GaussianBlur(agent,(0,0),max(.4,width/260))
        yy = np.indices(frame.shape[:2],dtype=np.float32)[0]
        cy = float(source[22,1])
        # Stirn, Wangen und Mund werden zeitversetzt übernommen.
        # Dramatik: der Agent dringt vor und wird zweimal zurückgedrängt, bevor er gewinnt
        take = p+.16*np.sin(2*np.pi*2.5*p)*np.sin(np.pi*p) if drama else p
        mix = smooth((take-.22)/.65 + (cy-yy)/max(width,1)*.18*np.sin(np.pi*p))*face
        surface = original*(1-mix[...,None])+agent*mix[...,None]
        result = frame*(1-warp_alpha[...,None])+surface*warp_alpha[...,None]
        if drama and 0 < p < 1:
            center = (source[20:22].mean(0)+source[26])/2
            result = self._bulge_warp(result.astype(np.float32),center,axis,down,width,p)
        return np.clip(result,0,255).astype(np.uint8)
