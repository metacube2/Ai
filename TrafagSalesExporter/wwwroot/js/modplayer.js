// ProTracker-MOD-Abspieler in reinem JavaScript.
//
// Warum selbst geschrieben und keine Bibliothek: alles muss lokal liegen. Der
// Produktivserver laedt nichts aus dem Netz nach (three.js liegt aus demselben Grund
// unter wwwroot/js/vendor), und ein WASM-Blob von einem CDN waere genau das Gegenteil.
// Ein ProTracker-Abspieler ist gut dokumentiert und in wenigen hundert Zeilen machbar.
//
// Aufgeteilt in Zerlegen (parseMod) und Mischen (ModPlayer.render). Beides rechnet
// ohne WebAudio und laeuft deshalb auch kopflos in Node - das ist die einzige Chance,
// den Abspieler ohne Browser zu pruefen.

const PAL_CLOCK = 7093789.2;
const ROWS_PER_PATTERN = 64;

// Periodentabelle, Oktaven 1-3 in Halbtonschritten (ProTracker-Standard, Finetune 0).
const PERIODS = [
  856, 808, 762, 720, 678, 640, 604, 570, 538, 508, 480, 453,
  428, 404, 381, 360, 339, 320, 302, 285, 269, 254, 240, 226,
  214, 202, 190, 180, 170, 160, 151, 143, 135, 127, 120, 113,
];

// Finetune veraendert die Periode um jeweils rund 1/8 Halbton.
const FINETUNE_FACTOR = [];
for (let i = 0; i < 16; i++) {
  const ft = i < 8 ? i : i - 16;          // 0..7 = 0..+7, 8..15 = -8..-1
  FINETUNE_FACTOR.push(Math.pow(2, -ft / (12 * 8)));
}

const SINE_TABLE = [
  0, 24, 49, 74, 97, 120, 141, 161, 180, 197, 212, 224, 235, 244, 250, 253,
  255, 253, 250, 244, 235, 224, 212, 197, 180, 161, 141, 120, 97, 74, 49, 24,
];

function readString(bytes, offset, length) {
  let out = "";
  for (let i = 0; i < length; i++) {
    const c = bytes[offset + i];
    if (c === 0) break;
    if (c >= 32 && c < 127) out += String.fromCharCode(c);
  }
  return out.trim();
}

function channelsFromTag(tag) {
  if (tag === "M.K." || tag === "M!K!" || tag === "FLT4" || tag === "4CHN") return 4;
  if (tag === "6CHN") return 6;
  if (tag === "8CHN" || tag === "CD81" || tag === "OKTA") return 8;
  const m = /^(\d)CHN$/.exec(tag);
  if (m) return Number(m[1]);
  const m2 = /^(\d\d)CH$/.exec(tag);
  if (m2) return Number(m2[1]);
  return 0;
}

export function parseMod(buffer) {
  const bytes = new Uint8Array(buffer);
  if (bytes.length < 1084) throw new Error("Datei ist zu kurz fuer ein MOD");

  const tag = readString(bytes, 1080, 4);
  let channels = channelsFromTag(tag);
  let sampleCount = 31;
  let headerEnd = 1084;
  if (channels === 0) {
    // Ohne erkennbare Kennung: die alten 15-Sample-Module von Ultimate Soundtracker.
    channels = 4;
    sampleCount = 15;
    headerEnd = 600;
  }

  const samples = [];
  const sampleHeader = sampleCount === 31 ? 20 : 20;
  for (let i = 0; i < sampleCount; i++) {
    const off = sampleHeader + i * 30;
    const lengthWords = (bytes[off + 22] << 8) | bytes[off + 23];
    const finetune = bytes[off + 24] & 0x0f;
    const volume = Math.min(64, bytes[off + 25]);
    const repeatStart = ((bytes[off + 26] << 8) | bytes[off + 27]) * 2;
    const repeatLength = ((bytes[off + 28] << 8) | bytes[off + 29]) * 2;
    samples.push({
      name: readString(bytes, off, 22),
      length: lengthWords * 2,
      finetune,
      volume,
      repeatStart,
      repeatLength,
      data: null,
    });
  }

  const orderOffset = sampleCount === 31 ? 950 : 470;
  const songLength = bytes[orderOffset];
  const order = [];
  for (let i = 0; i < 128; i++) order.push(bytes[orderOffset + 2 + i]);
  // Wieviele Muster passen ueberhaupt noch in die Datei? Ohne diese Schranke
  // reicht eine beliebige Datei aus, um Millionen Muster zu behaupten und den
  // Browser zum Stehen zu bringen - gemessen an Zufallsdaten.
  const patternBytes = ROWS_PER_PATTERN * channels * 4;
  const claimed = Math.max(...order.slice(0, Math.max(1, songLength))) + 1;
  const available = Math.floor((bytes.length - headerEnd) / patternBytes);
  const patternCount = Math.max(1, Math.min(claimed, available, 128));

  const patterns = [];
  const rowBytes = channels * 4;
  let p = headerEnd;
  for (let pat = 0; pat < patternCount; pat++) {
    const rows = [];
    for (let row = 0; row < ROWS_PER_PATTERN; row++) {
      const cells = [];
      for (let ch = 0; ch < channels; ch++) {
        const b0 = bytes[p], b1 = bytes[p + 1], b2 = bytes[p + 2], b3 = bytes[p + 3];
        p += 4;
        cells.push({
          sample: (b0 & 0xf0) | (b2 >> 4),
          period: ((b0 & 0x0f) << 8) | b1,
          effect: b2 & 0x0f,
          param: b3,
        });
      }
      rows.push(cells);
    }
    patterns.push(rows);
    // Sicherheitsnetz gegen abgeschnittene Dateien.
    if (p + rowBytes > bytes.length) break;
  }

  for (const sample of samples) {
    if (sample.length === 0) continue;
    // p kann hinter dem Dateiende liegen, wenn die Kopfdaten mehr behaupten als
    // vorhanden ist. Dann bleibt fuer dieses und alle weiteren Samples nichts uebrig.
    if (p >= bytes.length) { sample.length = 0; continue; }
    const end = Math.min(bytes.length, p + sample.length);
    const pcm = new Float32Array(Math.max(0, end - p));
    for (let i = 0; i < pcm.length; i++) {
      const v = bytes[p + i];
      pcm[i] = (v > 127 ? v - 256 : v) / 128;      // signed 8 bit
    }
    sample.data = pcm;
    sample.length = pcm.length;
    if (sample.repeatStart + sample.repeatLength > sample.length) {
      sample.repeatLength = Math.max(0, sample.length - sample.repeatStart);
    }
    p = end;
  }

  return {
    title: readString(bytes, 0, 20),
    tag: tag || "STK",
    channels,
    samples,
    patterns,
    order: order.slice(0, Math.max(1, songLength)),
    songLength: Math.max(1, songLength),
  };
}

export class ModPlayer {
  constructor(song, sampleRate) {
    this.song = song;
    this.sampleRate = sampleRate;
    this.reset();
  }

  reset() {
    this.orderIndex = 0;
    this.row = 0;
    this.tick = 0;
    this.speed = 6;              // Ticks je Zeile
    this.bpm = 125;
    this.samplesPerTick = Math.round((this.sampleRate * 2.5) / this.bpm);
    this.tickCursor = 0;
    this.finished = false;
    this.loops = 0;
    this.patternDelay = 0;
    this.breakToRow = -1;
    this.jumpToOrder = -1;
    this.channels = [];
    for (let i = 0; i < this.song.channels; i++) {
      this.channels.push({
        sampleIndex: -1, position: 0, period: 0, targetPeriod: 0,
        volume: 0, finetune: 0, playing: false,
        portaSpeed: 0, vibratoPos: 0, vibratoSpeed: 0, vibratoDepth: 0,
        arpeggio: 0, arpCounter: 0, retrig: 0, cutTick: -1, delayTick: -1,
        pending: null,
        // Amiga-Panorama waere hart links/rechts; das ist ueber Kopfhoerer
        // unangenehm, deshalb abgeschwaecht.
        pan: (i % 4 === 0 || i % 4 === 3) ? -0.6 : 0.6,
      });
    }
  }

  periodToStep(period) {
    if (period <= 0) return 0;
    const freq = PAL_CLOCK / (period * 2);
    return freq / this.sampleRate;
  }

  startRow() {
    const patternIndex = this.song.order[this.orderIndex] ?? 0;
    const pattern = this.song.patterns[patternIndex];
    if (!pattern) { this.finished = true; return; }
    const cells = pattern[this.row];
    if (!cells) return;

    for (let c = 0; c < cells.length; c++) {
      const cell = cells[c];
      const ch = this.channels[c];
      if (!ch) continue;
      ch.arpeggio = 0;
      ch.arpCounter = 0;
      ch.retrig = 0;
      ch.cutTick = -1;
      ch.delayTick = -1;

      if (cell.sample > 0 && cell.sample <= this.song.samples.length) {
        const s = this.song.samples[cell.sample - 1];
        ch.sampleIndex = cell.sample - 1;
        ch.volume = s.volume;
        ch.finetune = s.finetune;
      }

      const isTonePorta = cell.effect === 0x3 || cell.effect === 0x5;
      if (cell.period > 0) {
        const tuned = cell.period * FINETUNE_FACTOR[ch.finetune];
        if (isTonePorta) {
          ch.targetPeriod = tuned;
        } else if (cell.effect === 0xe && (cell.param >> 4) === 0xd && (cell.param & 0x0f) > 0) {
          ch.delayTick = cell.param & 0x0f;      // Notenverzoegerung
          ch.pending = { period: tuned };
        } else {
          ch.period = tuned;
          ch.targetPeriod = tuned;
          ch.position = 0;
          ch.playing = ch.sampleIndex >= 0;
          ch.vibratoPos = 0;
        }
      }

      this.applyRowEffect(ch, cell);
    }
  }

  applyRowEffect(ch, cell) {
    const p = cell.param;
    const hi = p >> 4;
    const lo = p & 0x0f;
    switch (cell.effect) {
      case 0x0: ch.arpeggio = p; break;
      case 0x3: if (p > 0) ch.portaSpeed = p; break;
      case 0x4:
        if (hi > 0) ch.vibratoSpeed = hi;
        if (lo > 0) ch.vibratoDepth = lo;
        break;
      case 0x9: {                              // Sample-Offset
        const start = p * 256;
        const s = this.song.samples[ch.sampleIndex];
        if (s && start < s.length) ch.position = start;
        break;
      }
      case 0xb: this.jumpToOrder = p; break;   // Positionssprung
      case 0xc: ch.volume = Math.min(64, p); break;
      case 0xd: this.breakToRow = hi * 10 + lo; break;
      case 0xe:
        if (hi === 0x1) ch.period = Math.max(113, ch.period - lo);
        else if (hi === 0x2) ch.period = Math.min(856, ch.period + lo);
        else if (hi === 0x9) ch.retrig = lo;
        else if (hi === 0xa) ch.volume = Math.min(64, ch.volume + lo);
        else if (hi === 0xb) ch.volume = Math.max(0, ch.volume - lo);
        else if (hi === 0xc) ch.cutTick = lo;
        else if (hi === 0xe) this.patternDelay = lo;
        break;
      case 0xf:
        if (p === 0) break;
        if (p < 32) { this.speed = p; }
        else { this.bpm = p; this.samplesPerTick = Math.round((this.sampleRate * 2.5) / this.bpm); }
        break;
      default: break;
    }
  }

  // Effekte, die auf jedem Tick ausser dem ersten wirken.
  applyTickEffect(ch, cell, tick) {
    const p = cell.param;
    const hi = p >> 4;
    const lo = p & 0x0f;
    switch (cell.effect) {
      case 0x0:
        if (p !== 0) {
          ch.arpCounter = (ch.arpCounter + 1) % 3;
          const semis = ch.arpCounter === 0 ? 0 : ch.arpCounter === 1 ? hi : lo;
          ch.arpeggioPeriod = ch.period * Math.pow(2, -semis / 12);
        }
        break;
      case 0x1: ch.period = Math.max(113, ch.period - p); break;
      case 0x2: ch.period = Math.min(856, ch.period + p); break;
      case 0x3: case 0x5: {
        if (ch.targetPeriod > 0 && ch.portaSpeed > 0) {
          if (ch.period < ch.targetPeriod) ch.period = Math.min(ch.targetPeriod, ch.period + ch.portaSpeed);
          else if (ch.period > ch.targetPeriod) ch.period = Math.max(ch.targetPeriod, ch.period - ch.portaSpeed);
        }
        if (cell.effect === 0x5) this.volumeSlide(ch, p);
        break;
      }
      case 0x4: case 0x6: {
        if (cell.effect === 0x4 || true) {
          ch.vibratoPos = (ch.vibratoPos + ch.vibratoSpeed) & 63;
          const idx = ch.vibratoPos & 31;
          const delta = (SINE_TABLE[idx] * ch.vibratoDepth) / 128;
          ch.vibratoPeriod = ch.period + (ch.vibratoPos < 32 ? delta : -delta);
        }
        if (cell.effect === 0x6) this.volumeSlide(ch, p);
        break;
      }
      case 0xa: this.volumeSlide(ch, p); break;
      case 0xe:
        if (hi === 0x9 && ch.retrig > 0 && tick % ch.retrig === 0) ch.position = 0;
        else if (hi === 0xc && tick === ch.cutTick) ch.volume = 0;
        else if (hi === 0xd && tick === ch.delayTick && ch.pending) {
          ch.period = ch.pending.period;
          ch.targetPeriod = ch.period;
          ch.position = 0;
          ch.playing = ch.sampleIndex >= 0;
          ch.pending = null;
        }
        break;
      default: break;
    }
  }

  volumeSlide(ch, param) {
    const up = param >> 4;
    const down = param & 0x0f;
    if (up > 0) ch.volume = Math.min(64, ch.volume + up);
    else if (down > 0) ch.volume = Math.max(0, ch.volume - down);
  }

  advanceTick() {
    // Arpeggio und Vibrato gelten nur fuer den laufenden Tick und werden von
    // applyTickEffect jedes Mal neu gesetzt.
    for (const ch of this.channels) {
      ch.arpeggioPeriod = 0;
      ch.vibratoPeriod = 0;
    }
    if (this.tick === 0) {
      this.startRow();
    } else {
      const patternIndex = this.song.order[this.orderIndex] ?? 0;
      const pattern = this.song.patterns[patternIndex];
      const cells = pattern ? pattern[this.row] : null;
      if (cells) {
        for (let c = 0; c < cells.length; c++) {
          const ch = this.channels[c];
          if (ch) this.applyTickEffect(ch, cells[c], this.tick);
        }
      }
    }

    this.tick++;
    const ticksThisRow = this.speed * (1 + this.patternDelay);
    if (this.tick >= ticksThisRow) {
      this.tick = 0;
      this.patternDelay = 0;
      this.nextRow();
    }
  }

  nextRow() {
    if (this.jumpToOrder >= 0) {
      this.orderIndex = this.jumpToOrder;
      this.row = this.breakToRow >= 0 ? this.breakToRow : 0;
      this.jumpToOrder = -1;
      this.breakToRow = -1;
      if (this.orderIndex >= this.song.songLength) { this.orderIndex = 0; this.loops++; }
      return;
    }
    if (this.breakToRow >= 0) {
      this.row = this.breakToRow;
      this.breakToRow = -1;
      this.orderIndex++;
      if (this.orderIndex >= this.song.songLength) { this.orderIndex = 0; this.loops++; }
      return;
    }
    this.row++;
    if (this.row >= ROWS_PER_PATTERN) {
      this.row = 0;
      this.orderIndex++;
      if (this.orderIndex >= this.song.songLength) { this.orderIndex = 0; this.loops++; }
    }
  }

  /**
   * Mischt frameCount Bilder nach left/right (Float32Array). Rueckgabe: gemischte Anzahl.
   * Rein rechnend, kein WebAudio - genau deshalb kopflos pruefbar.
   */
  render(left, right, frameCount) {
    let done = 0;
    while (done < frameCount) {
      if (this.tickCursor <= 0) {
        this.advanceTick();
        this.tickCursor = this.samplesPerTick;
      }
      const chunk = Math.min(frameCount - done, this.tickCursor);
      for (let i = done; i < done + chunk; i++) { left[i] = 0; right[i] = 0; }

      for (const ch of this.channels) {
        if (!ch.playing || ch.sampleIndex < 0) continue;
        const s = this.song.samples[ch.sampleIndex];
        if (!s || !s.data || s.length === 0) continue;
        const period = ch.arpeggioPeriod || ch.vibratoPeriod || ch.period;
        const step = this.periodToStep(period);
        if (step <= 0) continue;
        const vol = ch.volume / 64;
        const gainL = vol * (1 - Math.max(0, ch.pan)) * 0.5;
        const gainR = vol * (1 + Math.min(0, ch.pan)) * 0.5;
        const looping = s.repeatLength > 2;

        for (let i = done; i < done + chunk; i++) {
          const pos = ch.position | 0;
          const frac = ch.position - pos;
          const a = s.data[pos] || 0;
          const b = s.data[pos + 1] !== undefined ? s.data[pos + 1] : a;
          const v = a + (b - a) * frac;        // lineare Interpolation
          left[i] += v * gainL;
          right[i] += v * gainR;
          ch.position += step;
          if (ch.position >= s.length) {
            if (looping) {
              ch.position = s.repeatStart + ((ch.position - s.length) % s.repeatLength);
            } else {
              ch.playing = false;
              break;
            }
          }
        }
      }

      // Weiche Begrenzung statt hartem Clipping - bei acht Kanaelen sonst hoerbar.
      for (let i = done; i < done + chunk; i++) {
        left[i] = Math.tanh(left[i]);
        right[i] = Math.tanh(right[i]);
      }

      this.tickCursor -= chunk;
      done += chunk;
    }
    return done;
  }
}
