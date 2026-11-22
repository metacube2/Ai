class GeometricSynthProcessor extends AudioWorkletProcessor {
  constructor() {
    super();
    this.params = {
      instrument: "violin",
      shape: "circle",
      pitch: 220,
      size: 100,
      excitation: 0.55,
      damping: 0.45,
      brightness: 0.55,
      volume: 0.35,
      gate: false,
      customModes: null,
    };
    this.phase = 0;
    this.modalPhases = new Float64Array(6);
    this.envelope = 0;
    this.previousNoise = 0;
    this.levelCounter = 0;
    this.levelSum = 0;
    this.port.onmessage = (event) => Object.assign(this.params, event.data);
  }

  modesForShape(shape) {
    switch (shape) {
      case "triangle": return [1, 1.67, 2.33, 2.81, 3.42, 4.05];
      case "square": return [1, 1.41, 2, 2.24, 2.83, 3.16];
      case "hexagon": return [1, 1.5, 2, 2.5, 3, 3.5];
      default: return [1, 1.59, 2.14, 2.30, 2.65, 3.16];
    }
  }

  bodySample(frequency) {
    const p = this.params;
    const ratios = Array.isArray(p.customModes) && p.customModes.length
      ? p.customModes
      : this.modesForShape(p.shape);
    if (this.modalPhases.length < ratios.length) {
      const phases = new Float64Array(ratios.length);
      phases.set(this.modalPhases);
      this.modalPhases = phases;
    }
    const decay = 0.42 + p.damping * 1.8;
    let value = 0;
    let weightSum = 0;
    for (let mode = 0; mode < ratios.length; mode += 1) {
      const weight = Math.exp(-mode * decay);
      this.modalPhases[mode] += 2 * Math.PI * frequency * ratios[mode] / sampleRate;
      if (this.modalPhases[mode] > 2 * Math.PI) this.modalPhases[mode] -= 2 * Math.PI;
      value += Math.sin(this.modalPhases[mode] + mode * 0.21) * weight;
      weightSum += weight;
    }
    return value / weightSum;
  }

  violinSample(frequency, time) {
    const p = this.params;
    const vibrato = 1 + 0.0038 * p.excitation * Math.sin(2 * Math.PI * 5.3 * time);
    this.phase += 2 * Math.PI * frequency * vibrato / sampleRate;
    if (this.phase > 2 * Math.PI) this.phase -= 2 * Math.PI;
    const rolloff = 0.72 + (1 - p.brightness) * 1.25;
    let string = 0;
    let normalizer = 0;
    for (let harmonic = 1; harmonic <= 12; harmonic += 1) {
      const amplitude = 1 / Math.pow(harmonic, rolloff);
      string += Math.sin(this.phase * harmonic) * amplitude;
      normalizer += amplitude;
    }
    string = Math.tanh((string / normalizer) * (0.65 + p.excitation * 2.2));
    const noise = Math.random() * 2 - 1;
    const bowNoise = noise - this.previousNoise;
    this.previousNoise = noise;
    return 0.76 * string + 0.28 * this.bodySample(frequency) + bowNoise * 0.018;
  }

  fluteSample(frequency, time) {
    const p = this.params;
    const vibrato = 1 + 0.0028 * p.excitation * Math.sin(2 * Math.PI * (4.7 + p.excitation) * time);
    this.phase += 2 * Math.PI * frequency * vibrato / sampleRate;
    if (this.phase > 2 * Math.PI) this.phase -= 2 * Math.PI;
    const overblow = Math.max(0, Math.min(1, (p.excitation - 0.7) / 0.3));
    const pipe = (
      Math.sin(this.phase)
      + (0.10 + 0.25 * p.brightness) * Math.sin(2 * this.phase + 0.18)
      + (0.04 + 0.34 * overblow) * Math.sin(3 * this.phase + 0.31)
    ) / 1.5;
    const noise = Math.random() * 2 - 1;
    const breath = noise - this.previousNoise;
    this.previousNoise = noise;
    return 0.78 * pipe + 0.20 * this.bodySample(frequency) + breath * (0.012 + p.excitation * 0.045);
  }

  process(_inputs, outputs) {
    const output = outputs[0][0];
    if (!output) return true;
    const p = this.params;
    const frequency = Math.max(35, Math.min(2200, p.pitch * 100 / p.size));
    const target = p.gate ? 1 : 0;
    const envelopeTime = p.gate ? 0.008 : 0.025;
    const coefficient = 1 - Math.exp(-1 / (sampleRate * envelopeTime));
    for (let index = 0; index < output.length; index += 1) {
      this.envelope += (target - this.envelope) * coefficient;
      const time = currentTime + index / sampleRate;
      let value = p.instrument === "flute"
        ? this.fluteSample(frequency, time)
        : this.violinSample(frequency, time);
      value = Math.tanh(value * (1.1 + p.excitation * 1.9));
      value *= this.envelope * p.volume * 0.85;
      output[index] = value;
      this.levelSum += value * value;
      this.levelCounter += 1;
    }
    if (this.levelCounter >= 2048) {
      this.port.postMessage({ level: Math.sqrt(this.levelSum / this.levelCounter) });
      this.levelCounter = 0;
      this.levelSum = 0;
    }
    return true;
  }
}

registerProcessor("geometric-synth", GeometricSynthProcessor);
