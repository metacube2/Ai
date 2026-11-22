// Kleiner ProTracker-Replayer (4 Kanaele, M.K./4CHN/FLT4) als AudioWorklet.
// Effekte: 0 1 2 3 4 5 6 9 A B C D E1 E2 E9 EA EB EC F. Reicht fuer funkerbier.mod
// und spielt die meisten klassischen Amiga-MODs brauchbar ab.
const PAULA = 3546894.6; // PAL-Takt / 2
const SINUS = Array.from({ length: 64 }, (_, i) => Math.round(255 * Math.sin((Math.PI * i) / 32)));

class ModPlayer {
  constructor(rate) {
    this.rate = rate;
    this.mod = null;
  }

  laden(buf) {
    const b = new Uint8Array(buf);
    const txt = (o, n) => String.fromCharCode(...b.subarray(o, o + n)).replace(/\0.*$/s, '');
    const sig = txt(1080, 4);
    if (!['M.K.', 'M!K!', '4CHN', 'FLT4'].includes(sig)) throw new Error('kein 4-Kanal-MOD: ' + sig);
    const samples = [];
    for (let i = 0; i < 31; i++) {
      const o = 20 + i * 30;
      const ft = b[o + 24] & 15;
      samples.push({
        name: txt(o, 22),
        len: ((b[o + 22] << 8) | b[o + 23]) * 2,
        finetune: ft > 7 ? ft - 16 : ft,
        vol: Math.min(64, b[o + 25]),
        loopStart: ((b[o + 26] << 8) | b[o + 27]) * 2,
        loopLen: ((b[o + 28] << 8) | b[o + 29]) * 2,
      });
    }
    const songLen = b[950];
    const restart = b[951] < songLen ? b[951] : 0;
    const order = Array.from(b.subarray(952, 952 + 128));
    const nPat = Math.max(...order) + 1;
    const patterns = b.slice(1084, 1084 + nPat * 1024);
    let o = 1084 + nPat * 1024;
    for (const s of samples) {
      const roh = new Int8Array(b.buffer, b.byteOffset + o, Math.max(0, Math.min(s.len, b.length - o)));
      s.data = Float32Array.from(roh, (v) => v / 128);
      o += s.len;
      s.len = s.data.length;
      s.loop = s.loopLen > 2 && s.loopStart + s.loopLen <= s.len;
      if (s.loop) s.loopEnd = s.loopStart + s.loopLen;
    }
    this.mod = { title: txt(0, 20), samples, songLen, restart, order, patterns };
    this.reset();
  }

  reset() {
    this.speed = 6;
    this.bpm = 125;
    this.tick = 0;
    this.pos = 0;
    this.row = 0;
    this.restSamples = 0;
    this.sprung = null;
    this.ch = [0, 1, 2, 3].map(() => ({
      s: null, pos: 0, period: 0, out: 0, vol: 0, eff: 0, par: 0,
      ziel: 0, portaSpeed: 0, vibPos: 0, vibSpeed: 0, vibTiefe: 0, aktiv: false, note: false,
    }));
  }

  zeile() {
    const m = this.mod;
    const p = m.order[this.pos];
    const basis = p * 1024 + this.row * 16;
    const ereignisse = [];
    for (let k = 0; k < 4; k++) {
      const c = this.ch[k];
      const o = basis + k * 4;
      const b0 = m.patterns[o], b1 = m.patterns[o + 1], b2 = m.patterns[o + 2], b3 = m.patterns[o + 3];
      const nr = (b0 & 0xf0) | (b2 >> 4);
      const per = ((b0 & 0x0f) << 8) | b1;
      c.eff = b2 & 0x0f;
      c.par = b3;
      c.note = false;
      if (nr > 0 && m.samples[nr - 1].len > 0) {
        c.s = m.samples[nr - 1];
        c.vol = c.s.vol;
      }
      if (per > 0 && c.s) {
        if (c.eff === 3 || c.eff === 5) {
          c.ziel = per;
        } else {
          c.period = per;
          c.pos = c.eff === 9 ? c.par * 256 : 0;
          c.aktiv = true;
          c.vibPos = 0;
          c.note = true;
          ereignisse.push(k);
        }
      }
      c.out = c.period;
      const x = c.par >> 4, y = c.par & 15;
      switch (c.eff) {
        case 3: if (c.par) c.portaSpeed = c.par; break;
        case 4: if (x) c.vibSpeed = x; if (y) c.vibTiefe = y; break;
        case 0xb: this.sprung = { pos: c.par, row: 0 }; break;
        case 0xc: c.vol = Math.min(64, c.par); break;
        case 0xd: this.sprung = { pos: (this.sprung?.pos ?? this.pos + 1), row: Math.min(63, x * 10 + y) }; break;
        case 0xe:
          if (x === 1) c.period = Math.max(113, c.period - y);
          else if (x === 2) c.period = Math.min(856, c.period + y);
          else if (x === 0xa) c.vol = Math.min(64, c.vol + y);
          else if (x === 0xb) c.vol = Math.max(0, c.vol - y);
          else if (x === 0xc && y === 0) c.vol = 0;
          c.out = c.period;
          break;
        case 0xf:
          if (c.par === 0) break;
          if (c.par < 32) this.speed = c.par; else this.bpm = c.par;
          break;
      }
    }
    return ereignisse;
  }

  effekte() {
    for (const c of this.ch) {
      const x = c.par >> 4, y = c.par & 15;
      c.out = c.period;
      switch (c.eff) {
        case 0:
          if (c.par) {
            const n = [0, x, y][this.tick % 3];
            c.out = c.period * Math.pow(2, -n / 12);
          }
          break;
        case 1: c.period = Math.max(113, c.period - c.par); c.out = c.period; break;
        case 2: c.period = Math.min(856, c.period + c.par); c.out = c.period; break;
        case 3: case 5:
          if (c.ziel) {
            if (c.period < c.ziel) c.period = Math.min(c.ziel, c.period + c.portaSpeed);
            else c.period = Math.max(c.ziel, c.period - c.portaSpeed);
            c.out = c.period;
          }
          if (c.eff === 5) this.volSlide(c);
          break;
        case 4: case 6:
          c.out = c.period + (SINUS[c.vibPos & 63] * c.vibTiefe) / 128;
          c.vibPos += c.vibSpeed;
          if (c.eff === 6) this.volSlide(c);
          break;
        case 0xa: this.volSlide(c); break;
        case 0xe:
          if (x === 0xc && this.tick === y) c.vol = 0;
          if (x === 9 && y && this.tick % y === 0) c.pos = 0;
          break;
      }
    }
  }

  volSlide(c) {
    const x = c.par >> 4, y = c.par & 15;
    c.vol = x ? Math.min(64, c.vol + x) : Math.max(0, c.vol - y);
  }

  weiter() {
    if (this.sprung) {
      this.pos = this.sprung.pos;
      this.row = this.sprung.row;
      this.sprung = null;
    } else if (++this.row >= 64) {
      this.row = 0;
      this.pos++;
    }
    if (this.pos >= this.mod.songLen) this.pos = this.mod.restart;
  }

  // Fuellt l/r ab Index a bis e. Rueckgabe: Liste neuer Noten (fuer die Optik)
  rendern(l, r, a, e, meldung) {
    let i = a;
    while (i < e) {
      if (this.restSamples <= 0) {
        if (this.tick === 0) {
          const ev = this.zeile();
          meldung(this.pos, this.row, ev);
        } else {
          this.effekte();
        }
        if (++this.tick >= this.speed) {
          this.tick = 0;
          this.weiter();
        }
        this.restSamples += (this.rate * 2.5) / this.bpm;
      }
      const n = Math.min(e - i, Math.ceil(this.restSamples));
      for (let k = 0; k < 4; k++) {
        const c = this.ch[k];
        if (!c.aktiv || !c.s || c.vol === 0 || c.out < 1) continue;
        const s = c.s, d = s.data;
        const schritt = PAULA / Math.max(c.out, 28) / this.rate;
        const v = c.vol / 64;
        const links = k === 0 || k === 3;
        let pos = c.pos;
        for (let j = i; j < i + n; j++) {
          if (pos >= (s.loop ? s.loopEnd : s.len)) {
            if (!s.loop) { c.aktiv = false; break; }
            pos = s.loopStart + ((pos - s.loopStart) % s.loopLen);
          }
          const smp = d[pos | 0] * v;
          if (links) { l[j] += smp; r[j] += smp * 0.35; } else { r[j] += smp; l[j] += smp * 0.35; }
          pos += schritt;
        }
        c.pos = pos;
      }
      i += n;
      this.restSamples -= n;
    }
  }
}

if (typeof AudioWorkletProcessor !== 'undefined') {
  class ModProcessor extends AudioWorkletProcessor {
    constructor() {
      super();
      this.player = new ModPlayer(sampleRate);
      this.an = false;
      this.port.onmessage = (e) => {
        const m = e.data;
        try {
          if (m.typ === 'laden') { this.player.laden(m.buf); this.port.postMessage({ typ: 'geladen', title: this.player.mod.title }); }
          if (m.typ === 'spielen') this.an = m.an;
        } catch (err) {
          this.port.postMessage({ typ: 'fehler', text: String(err.message || err) });
        }
      };
    }

    process(_in, outs) {
      const out = outs[0];
      if (!this.an || !this.player.mod) return true;
      const l = out[0], r = out[1] || out[0];
      l.fill(0); if (r !== l) r.fill(0);
      this.player.rendern(l, r, 0, l.length, (pos, row, ev) => {
        this.port.postMessage({ typ: 'zeile', pos, row, ev });
      });
      for (let j = 0; j < l.length; j++) { l[j] *= 0.45; if (r !== l) r[j] *= 0.45; }
      return true;
    }
  }
  registerProcessor('mod-player', ModProcessor);
}

if (typeof module !== 'undefined') module.exports = { ModPlayer };
