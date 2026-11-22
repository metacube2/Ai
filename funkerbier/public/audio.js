// Klang: MOD-Musik (AudioWorklet), Kurzwellen-Rauschen mit QSB, CW-Rufe,
// Pfeifen, Knackser, dazu Flipper-Effekte. Startet erst nach einem Klick.
const CW_TEXTE = ['CQ CQ DE HB9BIER K', 'QRZ?', 'FUNKERBIER QRV', 'TNX QSO 73', 'CQ BIER CQ BIER K', 'QSL VIA BIER', 'R R 599 599'];
const MORSE = {
  A: '.-', B: '-...', C: '-.-.', D: '-..', E: '.', F: '..-.', G: '--.', H: '....', I: '..', J: '.---', K: '-.-',
  L: '.-..', M: '--', N: '-.', O: '---', P: '.--.', Q: '--.-', R: '.-.', S: '...', T: '-', U: '..-', V: '...-',
  W: '.--', X: '-..-', Y: '-.--', Z: '--..', 0: '-----', 1: '.----', 2: '..---', 3: '...--', 4: '....-',
  5: '.....', 6: '-....', 7: '--...', 8: '---..', 9: '----.', '?': '..--..', '/': '-..-.',
};
export const BAENDER = [
  { band: '80M', qrg: 3.65, mode: 'LSB', filter: 1400, cw: 560 },
  { band: '40M', qrg: 7.09, mode: 'LSB', filter: 1700, cw: 600 },
  { band: '20M', qrg: 14.285, mode: 'USB', filter: 2100, cw: 640 },
  { band: '10M', qrg: 28.4, mode: 'USB', filter: 2600, cw: 700 },
  { band: '2M', qrg: 145.5, mode: 'FM', filter: 3200, cw: 800 },
  { band: '70CM', qrg: 438.65, mode: 'FM', filter: 3200, cw: 820 },
];

export class Funk {
  constructor() {
    this.ctx = null;
    this.an = false;
    this.pegel = { musik: 0.1, rauschen: 0.1, sfx: 0.1 };
    this.beiZeile = () => {};
    this.band = BAENDER[5];
  }

  get bereit() { return !!this.ctx; }

  async starten() {
    if (this.ctx) { await this.ctx.resume(); this.an = true; this.anwenden(); return; }
    const ctx = (this.ctx = new (window.AudioContext || window.webkitAudioContext)());
    this.master = ctx.createGain();
    this.analyse = ctx.createAnalyser();
    this.analyse.fftSize = 1024;
    this.analyse.smoothingTimeConstant = 0.6;
    this.master.connect(this.analyse).connect(ctx.destination);
    this.musik = ctx.createGain();
    this.rausch = ctx.createGain();
    this.sfx = ctx.createGain();
    this.rauschAnalyse = ctx.createAnalyser();
    this.rauschAnalyse.fftSize = 1024;
    this.musik.connect(this.master);
    this.rausch.connect(this.rauschAnalyse).connect(this.master);
    this.sfx.connect(this.master);
    this.an = true;
    this.anwenden();
    this.rauschenAufbauen();
    this.cwPlanen(4);
    this.funkenPlanen();
    try {
      await ctx.audioWorklet.addModule('mod-worklet.js?v=1');
      const mod = (this.mod = new AudioWorkletNode(ctx, 'mod-player', { numberOfInputs: 0, outputChannelCount: [2] }));
      mod.connect(this.musik);
      mod.port.onmessage = (e) => {
        if (e.data.typ === 'zeile') this.beiZeile(e.data);
        if (e.data.typ === 'fehler') console.warn('MOD:', e.data.text);
      };
      const buf = await (await fetch('funkerbier.mod?v=1')).arrayBuffer();
      mod.port.postMessage({ typ: 'laden', buf }, [buf]);
      mod.port.postMessage({ typ: 'spielen', an: true });
    } catch (err) {
      console.warn('MOD-Player nicht verfuegbar', err);
    }
  }

  pause() {
    this.an = false;
    this.anwenden();
  }

  setze(kanal, wert) {
    this.pegel[kanal] = wert;
    this.anwenden();
  }

  anwenden() {
    if (!this.ctx) return;
    const t = this.ctx.currentTime;
    const m = this.an ? 1 : 0;
    this.musik.gain.setTargetAtTime(this.pegel.musik * m, t, 0.08);
    this.rausch.gain.setTargetAtTime(this.pegel.rauschen * 0.9 * m, t, 0.08);
    this.sfx.gain.setTargetAtTime(this.pegel.sfx * m, t, 0.05);
    if (this.mod) this.mod.port.postMessage({ typ: 'spielen', an: this.an });
  }

  rauschenAufbauen() {
    const ctx = this.ctx;
    const len = ctx.sampleRate * 3;
    const buf = ctx.createBuffer(1, len, ctx.sampleRate);
    const d = buf.getChannelData(0);
    for (let i = 0; i < len; i++) d[i] = Math.random() * 2 - 1;
    this.rauschBuffer = buf;
    const quelle = ctx.createBufferSource();
    quelle.buffer = buf;
    quelle.loop = true;
    const bp = (this.bandpass = ctx.createBiquadFilter());
    bp.type = 'bandpass';
    bp.frequency.value = this.band.filter;
    bp.Q.value = 0.55;
    const qsb = ctx.createGain();
    qsb.gain.value = 0.35;
    const lfo = ctx.createOscillator();
    lfo.frequency.value = 0.11;
    const tiefe = ctx.createGain();
    tiefe.gain.value = 0.16;
    lfo.connect(tiefe).connect(qsb.gain);
    lfo.start();
    quelle.connect(bp).connect(qsb).connect(this.rausch);
    quelle.start();
  }

  bandWechseln(b) {
    this.band = b;
    if (!this.ctx) return;
    this.bandpass.frequency.setTargetAtTime(b.filter, this.ctx.currentTime, 0.1);
    this.squelch(0.35);
  }

  // Zufaellige Knackser und Pfeifen (Traeger, die durchs Band ziehen)
  funkenPlanen() {
    const ctx = this.ctx;
    const tick = () => {
      if (this.an && this.pegel.rauschen > 0) {
        const t = ctx.currentTime;
        if (Math.random() < 0.55) this.knacks(t + Math.random() * 0.5);
        if (Math.random() < 0.12) {
          const o = ctx.createOscillator(), g = ctx.createGain();
          o.type = 'sine';
          o.frequency.setValueAtTime(400 + Math.random() * 1600, t);
          o.frequency.linearRampToValueAtTime(300 + Math.random() * 1800, t + 2.5);
          g.gain.setValueAtTime(0, t);
          g.gain.linearRampToValueAtTime(0.025, t + 0.6);
          g.gain.linearRampToValueAtTime(0, t + 2.6);
          o.connect(g).connect(this.rausch);
          o.start(t);
          o.stop(t + 2.7);
        }
      }
      setTimeout(tick, 900 + Math.random() * 2200);
    };
    tick();
  }

  knacks(t, staerke = 0.5) {
    const ctx = this.ctx;
    const q = ctx.createBufferSource();
    q.buffer = this.rauschBuffer;
    const g = ctx.createGain();
    g.gain.setValueAtTime(staerke, t);
    g.gain.exponentialRampToValueAtTime(0.001, t + 0.03);
    q.connect(g).connect(this.rausch);
    q.start(t, Math.random() * 2, 0.05);
  }

  cwPlanen(verzoegerung) {
    setTimeout(() => {
      if (this.an && this.pegel.rauschen > 0) this.morse(CW_TEXTE[Math.floor(Math.random() * CW_TEXTE.length)]);
      this.cwPlanen(25 + Math.random() * 40);
    }, verzoegerung * 1000);
  }

  morse(text, wpm = 18 + Math.random() * 8, ziel) {
    const ctx = this.ctx;
    if (!ctx) return 0;
    const dit = 1.2 / wpm;
    const o = ctx.createOscillator(), g = ctx.createGain(), fade = ctx.createGain();
    o.frequency.value = this.band.cw + (Math.random() * 40 - 20);
    g.gain.value = 0;
    fade.gain.value = ziel ? 0.25 : 0.06 + Math.random() * 0.08;
    o.connect(g).connect(fade).connect(ziel || this.rausch);
    let t = ctx.currentTime + 0.1;
    const start = t;
    for (const ch of text.toUpperCase()) {
      if (ch === ' ') { t += dit * 4; continue; }
      for (const z of MORSE[ch] || '') {
        const l = z === '.' ? dit : dit * 3;
        g.gain.setValueAtTime(0, t);
        g.gain.linearRampToValueAtTime(1, t + 0.005);
        g.gain.setValueAtTime(1, t + l - 0.005);
        g.gain.linearRampToValueAtTime(0, t + l);
        t += l + dit;
      }
      t += dit * 2;
    }
    o.start(start);
    o.stop(t + 0.1);
    return t - start;
  }

  // ---- Flipper- und Funkeffekte (auf dem SFX-Bus) --------------------------
  ton(freq, dauer, typ = 'square', laut = 0.2, nach = null, start = 0) {
    if (!this.ctx || !this.an) return;
    const ctx = this.ctx, t = ctx.currentTime + start;
    const o = ctx.createOscillator(), g = ctx.createGain();
    o.type = typ;
    o.frequency.setValueAtTime(freq, t);
    if (nach) o.frequency.exponentialRampToValueAtTime(nach, t + dauer);
    g.gain.setValueAtTime(laut, t);
    g.gain.exponentialRampToValueAtTime(0.001, t + dauer);
    o.connect(g).connect(this.sfx);
    o.start(t);
    o.stop(t + dauer + 0.02);
  }

  bumper(stufe = 0) {
    this.ton(1400 + stufe * 300, 0.09, 'square', 0.16, 500);
    this.ton(220, 0.12, 'triangle', 0.3, 80);
  }

  punkte() { [880, 1175, 1568].forEach((f, i) => this.ton(f, 0.08, 'square', 0.08, null, i * 0.05)); }

  roger() {
    this.squelch(0.25);
    this.ton(1000, 0.12, 'sine', 0.18, null, 0.25);
    this.ton(1500, 0.14, 'sine', 0.18, null, 0.37);
  }

  squelch(dauer = 0.3) {
    if (!this.ctx || !this.an) return;
    const ctx = this.ctx, t = ctx.currentTime;
    const q = ctx.createBufferSource();
    q.buffer = this.rauschBuffer;
    const hp = ctx.createBiquadFilter();
    hp.type = 'highpass';
    hp.frequency.value = 900;
    const g = ctx.createGain();
    g.gain.setValueAtTime(0.35, t);
    g.gain.exponentialRampToValueAtTime(0.001, t + dauer);
    q.connect(hp).connect(g).connect(this.sfx);
    q.start(t, Math.random(), dauer + 0.05);
  }

  jackpot() {
    const noten = [523, 659, 784, 1047, 784, 1047, 1319, 1568];
    noten.forEach((f, i) => this.ton(f, 0.16, 'square', 0.12, null, i * 0.09));
    setTimeout(() => this.an && this.morse('JACKPOT', 26, this.sfx), 900);
  }

  tilt() {
    this.ton(110, 0.6, 'sawtooth', 0.25, 55);
    this.ton(116, 0.6, 'sawtooth', 0.2, 58);
  }

  diskette() {
    if (!this.ctx) return;
    const t = this.ctx.currentTime;
    for (let i = 0; i < 14; i++) this.knacksSfx(t + i * 0.07 + Math.random() * 0.02);
    this.ton(60, 0.5, 'sawtooth', 0.08, 50);
  }

  knacksSfx(t) {
    const ctx = this.ctx;
    const q = ctx.createBufferSource();
    q.buffer = this.rauschBuffer;
    const bp = ctx.createBiquadFilter();
    bp.type = 'bandpass';
    bp.frequency.value = 2500;
    const g = ctx.createGain();
    g.gain.setValueAtTime(0.6, t);
    g.gain.exponentialRampToValueAtTime(0.001, t + 0.025);
    q.connect(bp).connect(g).connect(this.sfx);
    q.start(t, Math.random() * 2, 0.04);
  }
}
