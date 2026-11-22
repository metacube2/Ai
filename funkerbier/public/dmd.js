// Dot-Matrix-Display wie in einer Flipper-Backbox: 128 x 32 Punkte, orange.
const B = 128, H = 32;
const F = {
  A: [14, 17, 17, 31, 17, 17, 17], B: [30, 17, 17, 30, 17, 17, 30], C: [14, 17, 16, 16, 16, 17, 14],
  D: [28, 18, 17, 17, 17, 18, 28], E: [31, 16, 16, 30, 16, 16, 31], F: [31, 16, 16, 30, 16, 16, 16],
  G: [14, 17, 16, 23, 17, 17, 15], H: [17, 17, 17, 31, 17, 17, 17], I: [14, 4, 4, 4, 4, 4, 14],
  J: [7, 2, 2, 2, 2, 18, 12], K: [17, 18, 20, 24, 20, 18, 17], L: [16, 16, 16, 16, 16, 16, 31],
  M: [17, 27, 21, 21, 17, 17, 17], N: [17, 17, 25, 21, 19, 17, 17], O: [14, 17, 17, 17, 17, 17, 14],
  P: [30, 17, 17, 30, 16, 16, 16], Q: [14, 17, 17, 17, 21, 18, 13], R: [30, 17, 17, 30, 20, 18, 17],
  S: [15, 16, 16, 14, 1, 1, 30], T: [31, 4, 4, 4, 4, 4, 4], U: [17, 17, 17, 17, 17, 17, 14],
  V: [17, 17, 17, 17, 17, 10, 4], W: [17, 17, 17, 21, 21, 21, 10], X: [17, 17, 10, 4, 10, 17, 17],
  Y: [17, 17, 17, 10, 4, 4, 4], Z: [31, 1, 2, 4, 8, 16, 31],
  0: [14, 17, 19, 21, 25, 17, 14], 1: [4, 12, 4, 4, 4, 4, 14], 2: [14, 17, 1, 2, 4, 8, 31],
  3: [31, 2, 4, 2, 1, 17, 14], 4: [2, 6, 10, 18, 31, 2, 2], 5: [31, 16, 30, 1, 1, 17, 14],
  6: [6, 8, 16, 30, 17, 17, 14], 7: [31, 1, 2, 4, 8, 8, 8], 8: [14, 17, 17, 14, 17, 17, 14],
  9: [14, 17, 17, 15, 1, 2, 12],
  ' ': [0, 0, 0, 0, 0, 0, 0], '!': [4, 4, 4, 4, 4, 0, 4], '?': [14, 17, 1, 2, 4, 0, 4],
  '.': [0, 0, 0, 0, 0, 12, 12], ',': [0, 0, 0, 0, 12, 4, 8], ':': [0, 12, 12, 0, 12, 12, 0],
  '-': [0, 0, 0, 31, 0, 0, 0], '+': [0, 4, 4, 31, 4, 4, 0], '/': [0, 1, 2, 4, 8, 16, 0],
  "'": [12, 4, 8, 0, 0, 0, 0], '"': [10, 10, 0, 0, 0, 0, 0], '(': [2, 4, 8, 8, 8, 4, 2],
  ')': [8, 4, 2, 2, 2, 4, 8], '*': [0, 4, 21, 14, 21, 4, 0], '#': [10, 10, 31, 10, 31, 10, 10],
  '>': [8, 4, 2, 1, 2, 4, 8], '<': [2, 4, 8, 16, 8, 4, 2], '=': [0, 0, 31, 0, 31, 0, 0],
  '&': [12, 18, 20, 8, 21, 18, 13], '%': [24, 25, 2, 4, 8, 19, 3], '@': [14, 17, 23, 21, 23, 16, 15],
  'Ä': [10, 0, 14, 17, 31, 17, 17], 'Ö': [10, 0, 14, 17, 17, 17, 14], 'Ü': [10, 0, 17, 17, 17, 17, 14],
  '♪': [6, 5, 4, 4, 28, 60, 24], '·': [0, 0, 0, 4, 0, 0, 0], '–': [0, 0, 0, 31, 0, 0, 0],
};

function glyph(c) {
  const g = F[c] || F[c.toUpperCase()] || F[c.normalize('NFD')[0]?.toUpperCase()] || F['?'];
  return g;
}

export class Dmd {
  constructor(canvas, vorleser) {
    this.c = canvas;
    this.vorleser = vorleser;
    this.ctx = canvas.getContext('2d');
    this.punkte = new Uint8Array(B * H);
    this.queue = [];
    this.idle = ['FUNKERBIER'];
    this.idleIndex = 0;
    this.aktuell = null;
    this.rand = 0;
    this.groesse();
    addEventListener('resize', () => this.groesse());
    this.ruhig = matchMedia('(prefers-reduced-motion: reduce)').matches;
    requestAnimationFrame((t) => this.schleife(t));
  }

  groesse() {
    const breite = this.c.clientWidth || 600;
    const dpr = Math.min(devicePixelRatio || 1, 2);
    this.punkt = Math.max(2, Math.floor((breite * dpr) / B));
    this.c.width = this.punkt * B;
    this.c.height = this.punkt * H;
    this.dirty = true;
  }

  // Nachricht einreihen: {text, art: 'lauf'|'blink'|'mitte', gross, dauer}
  zeige(text, opt = {}) {
    const m = { text: String(text).toUpperCase(), art: opt.art || 'mitte', gross: !!opt.gross, dauer: opt.dauer || 1800 };
    if (opt.sofort) { this.queue = [m]; this.aktuell = null; } else this.queue.push(m);
    if (this.vorleser) this.vorleser.textContent = text;
  }

  setzeIdle(liste) { this.idle = liste; this.idleIndex = 0; }

  pulsieren() { this.rand = 1; }

  textBreite(t, s) { return t.length * 6 * s - s; }

  zeichneText(t, x, y, s) {
    for (const ch of t) {
      const g = glyph(ch);
      for (let r = 0; r < 7; r++) {
        const bits = g[r];
        for (let k = 0; k < 5; k++) {
          if (bits & (16 >> k)) {
            for (let dy = 0; dy < s; dy++) for (let dx = 0; dx < s; dx++) {
              const px = x + k * s + dx, py = y + r * s + dy;
              if (px >= 0 && px < B && py >= 0 && py < H) this.punkte[py * B + px] = 1;
            }
          }
        }
      }
      x += 6 * s;
      if (x > B) break;
    }
  }

  naechste(t) {
    let m = this.queue.shift();
    if (!m) {
      const i = this.idle[this.idleIndex++ % this.idle.length];
      m = typeof i === 'string' ? { text: i.toUpperCase(), art: i.length > 10 ? 'lauf' : 'mitte', gross: i.length <= 10, dauer: 2600 } : i;
      if (typeof i === 'function') { const r = i(); m = { text: r.toUpperCase(), art: 'mitte', gross: r.length <= 10, dauer: 2600 }; }
    }
    const s = m.gross ? 2 : 1;
    let breite = this.textBreite(m.text, s);
    if (m.art !== 'lauf' && breite > B) { m.art = 'lauf'; }
    if (m.art === 'mitte' && m.gross && breite > B) { m.gross = false; }
    m.s = m.gross ? 2 : 1;
    m.breite = this.textBreite(m.text, m.s);
    m.start = t;
    if (m.art === 'lauf') m.dauer = ((B + m.breite) / 60) * 1000 + 200;
    this.aktuell = m;
  }

  schleife(t) {
    requestAnimationFrame((tt) => this.schleife(tt));
    if (!this.aktuell || t - this.aktuell.start > this.aktuell.dauer) { this.naechste(t); this.dirty = true; }
    const m = this.aktuell;
    if (!this.dirty && m.art === 'mitte' && this.rand <= 0) return;
    if (this.letzte && t - this.letzte < 33) return; // ~30 fps
    this.letzte = t;
    this.dirty = false;
    this.punkte.fill(0);
    const s = m.s, h = 7 * s;
    const y = Math.floor((H - h) / 2);
    const vergangen = t - m.start;
    if (m.art === 'lauf') {
      this.zeichneText(m.text, Math.round(B - (this.ruhig ? B : (vergangen / 1000) * 60)), y, s);
      if (this.ruhig) { this.punkte.fill(0); this.zeichneText(m.text, 0, y, s); }
    } else if (m.art === 'blink') {
      if (this.ruhig || Math.floor(vergangen / 180) % 2 === 0) this.zeichneText(m.text, Math.floor((B - m.breite) / 2), y, s);
      // Rahmen-Lauflicht
      for (let i = 0; i < B; i += 4) { const on = (i / 4 + Math.floor(vergangen / 60)) % 2; this.punkte[i] = on; this.punkte[(H - 1) * B + i] = on; }
    } else {
      this.zeichneText(m.text, Math.floor((B - m.breite) / 2), y, s);
    }
    if (this.rand > 0) {
      for (let i = 0; i < B; i++) { this.punkte[i] = 1; this.punkte[(H - 1) * B + i] = 1; }
      this.rand -= 0.25;
    }
    this.malen();
  }

  malen() {
    const ctx = this.ctx, p = this.punkt, r = Math.max(0.8, p * 0.36);
    ctx.fillStyle = '#0c0400';
    ctx.fillRect(0, 0, this.c.width, this.c.height);
    ctx.fillStyle = '#2a1200';
    ctx.beginPath();
    for (let y = 0; y < H; y++) for (let x = 0; x < B; x++) if (!this.punkte[y * B + x]) { ctx.moveTo(x * p + p / 2 + r, y * p + p / 2); ctx.arc(x * p + p / 2, y * p + p / 2, r, 0, 7); }
    ctx.fill();
    ctx.fillStyle = '#ff7a00';
    ctx.shadowColor = '#ff7a00';
    ctx.shadowBlur = p * 1.2;
    ctx.beginPath();
    for (let y = 0; y < H; y++) for (let x = 0; x < B; x++) if (this.punkte[y * B + x]) { ctx.moveTo(x * p + p / 2 + r, y * p + p / 2); ctx.arc(x * p + p / 2, y * p + p / 2, r, 0, 7); }
    ctx.fill();
    ctx.shadowBlur = 0;
  }
}
