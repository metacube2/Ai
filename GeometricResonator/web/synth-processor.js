// synth-processor.js
//
// Echtzeitsynthese im AudioWorklet.
//
// Aufbau: Anregung -> Modalresonatorbank -> Ausgang.
//
// Die Resonatorbank IST der simulierte Koerper. Jede Mode ist ein
// Zweipolresonator auf ihrer absoluten, aus der Geometrie berechneten
// Frequenz. Die Anregung liefert nur das Spektrum, das gefiltert wird; sie
// bestimmt die Tonhoehe, aber nicht die Klangfarbe des Koerpers.
//
// Daraus folgt der Test, an dem sich das Modell messen laesst:
//   Tonhoehenregler bewegen  -> Grundton wandert, Formanten bleiben stehen.
//   Form aendern             -> Grundton bleibt, Formanten wandern.
//
// Die Datei kommt ohne Importe aus, weil AudioWorklet-Module das nicht
// zuverlaessig unterstuetzen. Dadurch laesst sie sich zugleich unter node
// mit einem kleinen Schim laden und testen.

const MAX_MODES = 24;

/** Bank aus Zweipolresonatoren mit Nullstellen bei +-1, Spitzenverstaerkung ~1. */
class ModalBank {
  constructor(maxModes) {
    this.size = maxModes;
    this.a1 = new Float64Array(maxModes);
    this.a2 = new Float64Array(maxModes);
    this.gain = new Float64Array(maxModes);
    this.targetA1 = new Float64Array(maxModes);
    this.targetA2 = new Float64Array(maxModes);
    this.targetGain = new Float64Array(maxModes);
    this.y1 = new Float64Array(maxModes);
    this.y2 = new Float64Array(maxModes);
    this.previous = 0;
    this.beforePrevious = 0;
    this.normalizer = 1;
    this.targetNormalizer = 1;
    this.fresh = true;
    this.count = 0;
  }

  /**
   * Setzt neue Zielkoeffizienten. Der Zustand bleibt erhalten, damit eine
   * Geometrieaenderung den Klang stetig ueberblendet statt zu knacken.
   */
  setModes(modes, rate) {
    const nyquist = rate * 0.45;
    let used = 0;
    let energy = 0;
    for (const mode of modes) {
      if (used >= this.size) break;
      if (!(mode.frequency > 0) || mode.frequency >= nyquist) continue;
      const omega = (2 * Math.PI * mode.frequency) / rate;
      const radius = Math.exp(-Math.PI * mode.frequency / (Math.max(1e-3, mode.q) * rate));
      this.targetA1[used] = 2 * radius * Math.cos(omega);
      this.targetA2[used] = -(radius * radius);
      this.targetGain[used] = ((1 - radius * radius) / 2) * mode.gain;
      // Ein noch stummer Resonator darf seine Pole sofort bekommen; nur die
      // Lautstaerke wird eingeblendet. Sonst wanderte er hoerbar von einer
      // alten Frequenz auf die neue.
      if (this.fresh || this.gain[used] === 0) {
        this.a1[used] = this.targetA1[used];
        this.a2[used] = this.targetA2[used];
      }
      energy += mode.gain * mode.gain;
      used += 1;
    }
    for (let i = used; i < this.size; i += 1) this.targetGain[i] = 0;
    this.count = used;
    this.targetNormalizer = energy > 0 ? 1 / Math.sqrt(energy) : 1;
    if (this.fresh) {
      this.normalizer = this.targetNormalizer;
      this.fresh = false;
    }
  }

  /** Gleitet einmal pro Block auf die Zielwerte zu (~20 ms Zeitkonstante). */
  glide(rate = 0.14) {
    for (let i = 0; i < this.size; i += 1) {
      this.a1[i] += (this.targetA1[i] - this.a1[i]) * rate;
      this.a2[i] += (this.targetA2[i] - this.a2[i]) * rate;
      this.gain[i] += (this.targetGain[i] - this.gain[i]) * rate;
    }
    this.normalizer += (this.targetNormalizer - this.normalizer) * rate;
  }

  process(input) {
    const drive = input - this.beforePrevious;
    let sum = 0;
    for (let i = 0; i < this.size; i += 1) {
      const value = this.gain[i] * drive + this.a1[i] * this.y1[i] + this.a2[i] * this.y2[i];
      this.y2[i] = this.y1[i];
      this.y1[i] = value;
      sum += value;
    }
    this.beforePrevious = this.previous;
    this.previous = input;
    if (!Number.isFinite(sum)) { this.reset(); return 0; }
    return sum * this.normalizer;
  }

  reset() {
    this.y1.fill(0);
    this.y2.fill(0);
    this.previous = 0;
    this.beforePrevious = 0;
  }
}

/** Bandbegrenzter Saegezahn; ohne PolyBLEP wuerde Aliasing die Moden treffen. */
function polyBlep(phase, increment) {
  if (phase < increment) {
    const t = phase / increment;
    return t + t - t * t - 1;
  }
  if (phase > 1 - increment) {
    const t = (phase - 1) / increment;
    return t * t + t + t + 1;
  }
  return 0;
}

class GeometricSynthProcessor extends AudioWorkletProcessor {
  constructor() {
    super();
    this.params = {
      instrument: "violin",
      pitch: 220,
      excitation: 0.55,
      volume: 0.32,
      gate: false,
    };
    this.bank = new ModalBank(MAX_MODES);
    this.phase = 0;
    this.noiseState = 0;
    this.breathState = 0;
    this.sourceEnvelope = 0;
    this.outputEnvelope = 0;
    this.strike = 0;
    this.strikeLength = 1;
    this.wasGated = false;
    this.levelSum = 0;
    this.levelCount = 0;
    this.smoothedPitch = 220;

    this.port.onmessage = (event) => {
      const data = event.data || {};
      if (Array.isArray(data.modes)) this.bank.setModes(data.modes, sampleRate);
      for (const key of ["instrument", "pitch", "excitation", "volume", "gate"]) {
        if (data[key] !== undefined) this.params[key] = data[key];
      }
      if (data.panic) this.bank.reset();
    };
  }

  /**
   * Anregung. Alle drei Modelle liefern bewusst ein breitbandiges Signal:
   * eine Anregung, die oberhalb weniger Kilohertz nichts mehr enthaelt,
   * liesse die hohen Koerpermoden stumm und die Form wieder unhoerbar.
   */
  source(frequency) {
    const p = this.params;
    const white = Math.random() * 2 - 1;

    if (p.instrument === "mallet") {
      // Anschlag: kurzer Impuls, danach schwingt nur noch der Koerper.
      if (this.strike > 0) {
        const position = 1 - this.strike / this.strikeLength;
        this.strike -= 1;
        const window = 0.5 - 0.5 * Math.cos(2 * Math.PI * position);
        return window * (0.55 + 0.45 * white) * (0.4 + p.excitation);
      }
      return 0;
    }

    const increment = Math.min(0.45, frequency / sampleRate);
    this.phase += increment;
    if (this.phase >= 1) this.phase -= 1;

    if (p.instrument === "flute") {
      // Luftstrom: gefiltertes Rauschen als Jet, dazu ein schwacher Rohrton.
      this.breathState += 0.22 * (white - this.breathState);
      const jet = this.breathState * (0.35 + p.excitation * 1.1);
      const tone = Math.sin(2 * Math.PI * this.phase)
        + 0.22 * Math.sin(4 * Math.PI * this.phase);
      return 0.68 * jet + 0.32 * tone * (0.3 + p.excitation * 0.7);
    }

    // Bogen: Saegezahn als Helmholtz-Naeherung plus breitbandiges
    // Bogenrauschen. Der Bogendruck steuert beides.
    const saw = 2 * this.phase - 1 - polyBlep(this.phase, increment);
    this.noiseState += 0.5 * (white - this.noiseState);
    const scratch = white - this.noiseState;
    return saw * (0.45 + p.excitation * 0.55) + scratch * (0.04 + p.excitation * 0.20);
  }

  process(_inputs, outputs) {
    const output = outputs[0] && outputs[0][0];
    if (!output) return true;
    const p = this.params;

    if (p.gate && !this.wasGated) {
      this.strikeLength = Math.max(8, Math.round(sampleRate * 0.0022));
      this.strike = this.strikeLength;
    }
    this.wasGated = p.gate;

    this.bank.glide();

    const target = p.gate ? 1 : 0;
    // Die Anregung schaltet schnell, der Ausgang blendet nur langsam aus.
    // Dadurch klingt der Koerper nach dem Loslassen aus, statt
    // abgeschnitten zu werden - so wie ein echter Korpus.
    const sourceCoefficient = 1 - Math.exp(-1 / (sampleRate * (p.gate ? 0.006 : 0.02)));
    const outputCoefficient = 1 - Math.exp(-1 / (sampleRate * (p.gate ? 0.004 : 0.35)));
    const pitchCoefficient = 1 - Math.exp(-1 / (sampleRate * 0.01));

    for (let index = 0; index < output.length; index += 1) {
      this.smoothedPitch += (p.pitch - this.smoothedPitch) * pitchCoefficient;
      this.sourceEnvelope += (target - this.sourceEnvelope) * sourceCoefficient;
      this.outputEnvelope += (target - this.outputEnvelope) * outputCoefficient;

      const excitation = this.source(this.smoothedPitch) * this.sourceEnvelope;
      const body = this.bank.process(excitation);
      // Ein kleiner Direktanteil haelt die Tonhoehe deutlich; den Klang
      // selbst traegt der Koerper. Zu viel Direktanteil war einer der
      // Gruende, warum frueher jede Form gleich klang.
      let value = 0.88 * body + 0.12 * excitation;
      value = Math.tanh(value * 1.6) * this.outputEnvelope * p.volume;
      output[index] = Number.isFinite(value) ? value : 0;
      this.levelSum += value * value;
      this.levelCount += 1;
    }

    if (this.levelCount >= 2048) {
      this.port.postMessage({ level: Math.sqrt(this.levelSum / this.levelCount) });
      this.levelSum = 0;
      this.levelCount = 0;
    }
    return true;
  }
}

registerProcessor("geometric-synth", GeometricSynthProcessor);
