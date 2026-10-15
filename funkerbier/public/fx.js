// Demo-Effekte: Copper-Balken mit Sternenfeld im Hintergrund, Sinus-Laufschrift unten.
const ruhig = matchMedia('(prefers-reduced-motion: reduce)').matches;

export function hintergrund(canvas) {
  const ctx = canvas.getContext('2d');
  let w, h, sterne = [];
  const farben = [[255, 42, 109], [255, 211, 25], [5, 217, 232], [57, 255, 20], [255, 136, 0]];
  let puls = 0;
  function groesse() {
    w = canvas.width = Math.ceil(innerWidth / 2);
    h = canvas.height = Math.ceil(innerHeight / 2);
    sterne = Array.from({ length: 90 }, () => ({ x: Math.random() * w, y: Math.random() * h, z: 0.2 + Math.random() * 1.6 }));
  }
  groesse();
  addEventListener('resize', groesse);
  let letzte = -1e9;
  function bild(t) {
    if (!ruhig) requestAnimationFrame(bild);
    if (t - letzte < 40) return;
    letzte = t;
    const g = ctx.createLinearGradient(0, 0, 0, h);
    g.addColorStop(0, '#000814');
    g.addColorStop(1, '#001233');
    ctx.fillStyle = g;
    ctx.fillRect(0, 0, w, h);
    for (const s of sterne) {
      s.x -= s.z * 0.8;
      if (s.x < 0) { s.x = w; s.y = Math.random() * h; }
      const c = Math.floor(90 + s.z * 90);
      ctx.fillStyle = `rgb(${c},${c},${c + 20})`;
      ctx.fillRect(Math.floor(s.x), Math.floor(s.y), s.z > 1.3 ? 2 : 1, 1);
    }
    // Copper-Balken
    const zeit = t / 1000;
    farben.forEach(([r, gg, b], i) => {
      const y = h * 0.5 + Math.sin(zeit * 0.9 + i * 0.7) * h * 0.38;
      const dicke = 10 + puls * 6;
      const gr = ctx.createLinearGradient(0, y - dicke, 0, y + dicke);
      gr.addColorStop(0, `rgba(${r},${gg},${b},0)`);
      gr.addColorStop(0.5, `rgba(${r},${gg},${b},${0.22 + puls * 0.15})`);
      gr.addColorStop(1, `rgba(${r},${gg},${b},0)`);
      ctx.fillStyle = gr;
      ctx.fillRect(0, y - dicke, w, dicke * 2);
    });
    puls *= 0.85;
  }
  requestAnimationFrame(bild);
  return { takt() { puls = 1; } };
}

export function laufschrift(canvas, text) {
  const ctx = canvas.getContext('2d');
  let w, h, x, breiten = [], gesamt = 0;
  const dpr = Math.min(devicePixelRatio || 1, 2);
  function groesse() {
    w = canvas.width = Math.ceil(canvas.clientWidth * dpr);
    h = canvas.height = Math.ceil(canvas.clientHeight * dpr);
    ctx.font = `${Math.round(18 * dpr)}px "Press Start 2P", monospace`;
    breiten = [...text].map((c) => ctx.measureText(c).width);
    gesamt = breiten.reduce((a, b) => a + b, 0);
    if (x === undefined) x = w;
  }
  groesse();
  addEventListener('resize', groesse);
  document.fonts?.ready.then(groesse);
  let letzte = 0;
  function bild(t) {
    requestAnimationFrame(bild);
    if (t - letzte < 33) return;
    const dt = letzte ? (t - letzte) / 1000 : 0;
    letzte = t;
    ctx.clearRect(0, 0, w, h);
    x -= (ruhig ? 0 : 110 * dpr) * dt;
    if (x < -gesamt) x = w;
    ctx.textBaseline = 'middle';
    let px = x;
    const chars = [...text];
    for (let i = 0; i < chars.length; i++) {
      const bw = breiten[i];
      if (px > -bw && px < w) {
        const y = h / 2 + (ruhig ? 0 : Math.sin(px / (60 * dpr) + t / 300) * h * 0.25);
        const farbe = (i * 12 + t / 20) % 360;
        ctx.fillStyle = '#000';
        ctx.fillText(chars[i], px + 3 * dpr, y + 3 * dpr);
        ctx.fillStyle = `hsl(${farbe} 100% 60%)`;
        ctx.fillText(chars[i], px, y);
      }
      px += bw;
    }
  }
  requestAnimationFrame(bild);
}

// S-Meter (Zeiger) und Wasserfall aus den Analysern der Audio-Kette
export function instrumente(smeter, wasserfall, quellen) {
  const sm = smeter.getContext('2d');
  const wf = wasserfall.getContext('2d');
  const ws = wasserfall.width, wh = wasserfall.height;
  wf.fillStyle = '#000';
  wf.fillRect(0, 0, ws, wh);
  let zeiger = 0, letzte = 0;
  const zeit = new Uint8Array(1024), spek = new Uint8Array(512);
  function bild(t) {
    requestAnimationFrame(bild);
    if (t - letzte < 50) return;
    letzte = t;
    const { rauschen, master } = quellen();
    // S-Meter
    let pegel = 0.08 + Math.sin(t / 2300) * 0.03 + Math.random() * 0.02;
    if (rauschen) {
      rauschen.getByteTimeDomainData(zeit);
      let q = 0;
      for (const v of zeit) { const d = (v - 128) / 128; q += d * d; }
      pegel = Math.min(1, Math.sqrt(q / zeit.length) * 5.5);
    }
    zeiger += (pegel - zeiger) * 0.35;
    const W = smeter.width, H = smeter.height;
    sm.fillStyle = '#f3e7c4';
    sm.fillRect(0, 0, W, H);
    sm.strokeStyle = '#222';
    sm.fillStyle = '#222';
    sm.lineWidth = 1.5;
    sm.font = '10px VT323, monospace';
    sm.textAlign = 'center';
    const cx = W / 2, cy = H + 30, R = H + 8;
    const skala = ['1', '3', '5', '7', '9', '+20', '+40', '+60'];
    sm.beginPath();
    sm.arc(cx, cy, R - 18, Math.PI * 1.25, Math.PI * 1.75);
    sm.stroke();
    skala.forEach((s, i) => {
      const a = Math.PI * 1.25 + (i / (skala.length - 1)) * Math.PI * 0.5;
      sm.fillStyle = i > 4 ? '#c00' : '#222';
      sm.fillText(s, cx + Math.cos(a) * (R - 6), cy + Math.sin(a) * (R - 6) + 3);
    });
    const a = Math.PI * 1.25 + zeiger * Math.PI * 0.5;
    sm.strokeStyle = '#b00';
    sm.lineWidth = 2;
    sm.beginPath();
    sm.moveTo(cx, cy);
    sm.lineTo(cx + Math.cos(a) * (R - 12), cy + Math.sin(a) * (R - 12));
    sm.stroke();
    // Wasserfall
    const bild2 = wf.getImageData(0, 0, ws, wh - 1);
    wf.putImageData(bild2, 0, 1);
    if (master) master.getByteFrequencyData(spek);
    const zeile = wf.createImageData(ws, 1);
    for (let x = 0; x < ws; x++) {
      const v = master ? spek[Math.floor((x / ws) * 200)] / 255 : Math.random() * 0.15;
      const [r, g, b] = farbe(Math.max(0, Math.min(1, v * 1.2 + Math.random() * 0.05)));
      zeile.data.set([r, g, b, 255], x * 4);
    }
    wf.putImageData(zeile, 0, 0);
  }
  requestAnimationFrame(bild);
}

function farbe(v) {
  // schwarz -> blau -> cyan -> gelb -> rot, wie ein SDR-Wasserfall
  const stopps = [[0, 0, 0], [0, 0, 140], [0, 200, 230], [255, 220, 0], [255, 40, 0]];
  const p = v * (stopps.length - 1), i = Math.min(stopps.length - 2, Math.floor(p)), f = p - i;
  return stopps[i].map((c, k) => Math.round(c + (stopps[i + 1][k] - c) * f));
}
