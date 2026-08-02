// Prueft die Echtzeit-DSP und die Kette Geometrie -> Klang.
//
// Ausfuehren:  node tests/synth.test.mjs
//
// Getestet wird die echte Datei web/synth-processor.js, geladen mit einem
// kleinen Schim fuer die AudioWorklet-Umgebung. Es gibt keinen zweiten
// Nachbau der Synthese, der auseinanderlaufen koennte.
//
// Die zentrale Frage ist die Beschwerde, die zu diesem Umbau gefuehrt hat:
// klingt wirklich jede Form gleich? Dafuer reicht "unterschiedliche Formen
// erzeugen unterschiedliche Signale" nicht - das erfuellt schon reines
// Rauschen. Geprueft wird stattdessen die Quelle-Filter-Trennung:
//
//   Tonhoehe aendern -> Grundton wandert, Koerperresonanzen bleiben stehen.
//   Form aendern     -> Koerperresonanzen wandern, Grundton bleibt.

import { analyzeGeometry, activePart } from "../web/geometry.js";
import { modalModel } from "../web/physics.js";

const RATE = 44100;
const BLOCK = 128;

let failures = 0;
let checks = 0;

function report(name, ok, detail) {
  checks += 1;
  if (!ok) failures += 1;
  console.log(`${ok ? "  ok  " : " FAIL "} ${name}${detail ? `\n         ${detail}` : ""}`);
}

// ------------------------------------------------- AudioWorklet-Schim
globalThis.sampleRate = RATE;
globalThis.currentTime = 0;
let ProcessorClass = null;
globalThis.AudioWorkletProcessor = class {
  constructor() {
    this.port = { postMessage() {}, onmessage: null };
  }
};
globalThis.registerProcessor = (_name, constructor) => { ProcessorClass = constructor; };
await import("../web/synth-processor.js");

if (!ProcessorClass) {
  console.error("synth-processor.js hat keinen Prozessor registriert");
  process.exit(1);
}

function makeProcessor(message) {
  const processor = new ProcessorClass();
  processor.port.onmessage({ data: message });
  return processor;
}

/** Rendert Bloecke und gibt ein Float64Array zurueck. */
function render(processor, seconds, { warmupBlocks = 0 } = {}) {
  const buffer = new Float32Array(BLOCK);
  const outputs = [[buffer]];
  for (let i = 0; i < warmupBlocks; i += 1) {
    buffer.fill(0);
    processor.process([], outputs);
  }
  const blocks = Math.ceil((seconds * RATE) / BLOCK);
  const signal = new Float64Array(blocks * BLOCK);
  for (let b = 0; b < blocks; b += 1) {
    buffer.fill(0);
    processor.process([], outputs);
    signal.set(buffer, b * BLOCK);
  }
  return signal;
}

/** Betrag bei einer Frequenz, Hann-gefenstert gegen Leckeffekte. */
function magnitudeAt(signal, frequency) {
  const n = signal.length;
  const k = 2 * Math.cos((2 * Math.PI * frequency) / RATE);
  let s1 = 0;
  let s2 = 0;
  for (let i = 0; i < n; i += 1) {
    const window = 0.5 - 0.5 * Math.cos((2 * Math.PI * i) / (n - 1));
    const s = signal[i] * window + k * s1 - s2;
    s2 = s1;
    s1 = s;
  }
  return Math.sqrt(Math.max(0, s1 * s1 + s2 * s2 - k * s1 * s2)) / (n / 4);
}

/** Frequenz mit dem groessten Betrag in einem Suchbereich. */
function dominantFrequency(signal, low, high, step = 2) {
  let best = low;
  let bestMagnitude = -1;
  for (let f = low; f <= high; f += step) {
    const magnitude = magnitudeAt(signal, f);
    if (magnitude > bestMagnitude) { bestMagnitude = magnitude; best = f; }
  }
  return best;
}

function rms(signal) {
  let sum = 0;
  for (const value of signal) sum += value * value;
  return Math.sqrt(sum / signal.length);
}

// ============================================================== DSP allein
// Guetewerte im Bereich einer Holzplatte. Kuenstlich hohe Guete waere
// bequemer zu messen, wuerde aber taeuschen: Moden mit einem Bruchteil
// eines Hertz Bandbreite trifft eine harmonische Anregung praktisch nie,
// und der Koerper bliebe im Test unhoerbar.
const BODY_A = [
  { frequency: 300, q: 90, gain: 1.0 },
  { frequency: 742, q: 74, gain: 0.7 },
  { frequency: 1180, q: 62, gain: 0.5 },
];
const BODY_B = [
  { frequency: 455, q: 90, gain: 1.0 },
  { frequency: 1090, q: 74, gain: 0.7 },
  { frequency: 1720, q: 62, gain: 0.5 },
];

// Der Anschlag legt die Eigenresonanzen des Koerpers frei: danach schwingt
// nur noch der Koerper, ohne dass eine Quelle eine Tonhoehe aufpraegt.
{
  const processor = makeProcessor({
    instrument: "mallet", volume: 0.5, excitation: 0.6, modes: BODY_A, gate: false,
  });
  render(processor, 0.4);
  processor.port.onmessage({ data: { gate: true } });
  const signal = render(processor, 0.5);

  const onMode = Math.min(
    magnitudeAt(signal, 300),
    magnitudeAt(signal, 742),
  );
  const offMode = Math.max(
    magnitudeAt(signal, 480),
    magnitudeAt(signal, 950),
  );
  report(
    "Anschlag legt die Koerpermoden frei",
    onMode > offMode * 3,
    `auf Mode ${onMode.toExponential(3)}, daneben ${offMode.toExponential(3)}, Faktor ${(onMode / offMode).toFixed(1)}`,
  );
}

// Der alte Fehler: die Koerpermoden folgten der Tonhoehe. Sie duerfen es
// jetzt nicht mehr tun.
{
  const peaks = [110, 220, 440, 880].map((pitch) => {
    const processor = makeProcessor({
      instrument: "mallet", volume: 0.5, excitation: 0.6, pitch, modes: BODY_A, gate: false,
    });
    render(processor, 0.4);
    processor.port.onmessage({ data: { gate: true } });
    return dominantFrequency(render(processor, 0.5), 150, 1400);
  });
  const spread = (Math.max(...peaks) - Math.min(...peaks)) / Math.min(...peaks);
  report(
    "Koerperresonanz haengt nicht am Tonhoehenregler",
    spread < 0.03,
    `Spitzen bei ${peaks.join(", ")} Hz ueber drei Oktaven Reglerweg`,
  );
}

// Gegenprobe: dieselbe Anregung, anderer Koerper -> andere Resonanz.
{
  const dominant = [BODY_A, BODY_B].map((modes) => {
    const processor = makeProcessor({
      instrument: "mallet", volume: 0.5, excitation: 0.6, pitch: 220, modes, gate: false,
    });
    render(processor, 0.4);
    processor.port.onmessage({ data: { gate: true } });
    return dominantFrequency(render(processor, 0.5), 150, 1400);
  });
  report(
    "Anderer Koerper, gleiche Anregung -> andere Resonanz",
    Math.abs(dominant[0] - 300) < 12 && Math.abs(dominant[1] - 455) < 14,
    `Koerper A bei ${dominant[0]} Hz (erwartet 300), Koerper B bei ${dominant[1]} Hz (erwartet 455)`,
  );
}

// Bei harmonischer Anregung folgt der Grundton dem Regler.
{
  const measure = (pitch) => {
    const processor = makeProcessor({
      instrument: "violin", volume: 0.5, excitation: 0.6, pitch, modes: BODY_A, gate: false,
    });
    render(processor, 0.4);
    processor.port.onmessage({ data: { gate: true } });
    const signal = render(processor, 0.6, { warmupBlocks: 60 });
    return { at220: magnitudeAt(signal, 220), at330: magnitudeAt(signal, 330) };
  };
  const low = measure(220);
  const high = measure(330);
  report(
    "Bogen: Grundton folgt dem Tonhoehenregler",
    low.at220 > low.at330 * 2.5 && high.at330 > high.at220 * 2.5,
    `bei 220 Hz: ${low.at220.toExponential(2)} vs ${low.at330.toExponential(2)}; bei 330 Hz: ${high.at330.toExponential(2)} vs ${high.at220.toExponential(2)}`,
  );
}

// Gleiche Tonhoehe, anderer Koerper -> andere Klangfarbe.
//
// Gemessen wird ein breites Spektrum, nicht nur die Vielfachen des
// Grundtons: der Koerper klingt auch zwischen den Obertoenen, und genau
// dieser Anteil traegt die Formunterscheidung.
{
  const profile = (modes, instrument) => {
    const processor = makeProcessor({
      instrument, volume: 0.5, excitation: 0.6, pitch: 220, modes, gate: false,
    });
    render(processor, 0.4);
    processor.port.onmessage({ data: { gate: true } });
    const signal = render(processor, 0.6, { warmupBlocks: 60 });
    const bins = [];
    for (let hz = 110; hz <= 3500; hz += 55) bins.push(magnitudeAt(signal, hz));
    const total = bins.reduce((sum, value) => sum + value, 0) || 1;
    return {
      linear: bins.map((value) => value / total),
      decibel: bins.map((value) => 20 * Math.log10(Math.max(value, 1e-9))),
    };
  };
  const spectralDistance = (a, b) => {
    let linear = 0;
    let decibel = 0;
    for (let i = 0; i < a.linear.length; i += 1) {
      linear += Math.abs(a.linear[i] - b.linear[i]);
      decibel += Math.abs(a.decibel[i] - b.decibel[i]);
    }
    return { linear, decibel: decibel / a.linear.length };
  };

  const bowed = spectralDistance(profile(BODY_A, "violin"), profile(BODY_B, "violin"));
  report(
    "Bogen: gleiche Tonhoehe, anderer Koerper -> andere Klangfarbe",
    bowed.linear > 0.2 && bowed.decibel > 2.5,
    `L1 ${bowed.linear.toFixed(3)}, mittlerer Pegelunterschied ${bowed.decibel.toFixed(1)} dB`,
  );

  const struck = spectralDistance(profile(BODY_A, "mallet"), profile(BODY_B, "mallet"));
  report(
    "Anschlag: der Koerper allein unterscheidet sich deutlich",
    struck.linear > 1.0,
    `L1 ${struck.linear.toFixed(3)}, mittlerer Pegelunterschied ${struck.decibel.toFixed(1)} dB`,
  );
}

// Numerische Robustheit.
{
  const processor = makeProcessor({
    instrument: "violin", volume: 0.6, excitation: 1, pitch: 880, modes: BODY_A, gate: true,
  });
  const signal = render(processor, 1.0);
  let peak = 0;
  let finite = true;
  for (const value of signal) {
    if (!Number.isFinite(value)) finite = false;
    peak = Math.max(peak, Math.abs(value));
  }
  report(
    "Ausgang bleibt endlich und begrenzt",
    finite && peak <= 1.0 && rms(signal) > 1e-4,
    `Spitze ${peak.toFixed(4)}, Effektivwert ${rms(signal).toExponential(3)}`,
  );
}

// Geometriewechsel im laufenden Betrieb darf nicht knacken.
//
// Der Abstand zweier benachbarter Abtastwerte taugt dafuer nicht: die
// Anregung enthaelt Rauschen, das von Natur aus grosse Spruenge hat. Ein
// Knacken zeigt sich stattdessen als Ausschlag ueber den bisherigen
// Spitzenwert hinaus.
{
  const peakOf = (signal, from, to) => {
    let peak = 0;
    for (let i = from; i < to; i += 1) peak = Math.max(peak, Math.abs(signal[i]));
    return peak;
  };
  const processor = makeProcessor({
    instrument: "violin", volume: 0.5, excitation: 0.6, pitch: 220, modes: BODY_A, gate: true,
  });
  const before = render(processor, 0.4);
  processor.port.onmessage({ data: { modes: BODY_B } });
  const after = render(processor, 0.4);

  const steady = peakOf(before, before.length / 2, before.length);
  const transition = peakOf(after, 0, Math.round(RATE * 0.03));
  report(
    "Wechsel des Koerpers erzeugt keinen Knack",
    transition < steady * 1.25,
    `Spitze vorher ${steady.toFixed(4)}, in den ersten 30 ms danach ${transition.toFixed(4)}`,
  );

  // Und der Klang darf dabei nicht kurz abreissen.
  const windowRms = (signal, from, length) => {
    let sum = 0;
    for (let i = from; i < from + length; i += 1) sum += signal[i] * signal[i];
    return Math.sqrt(sum / length);
  };
  const quietest = Math.min(
    ...Array.from({ length: 12 }, (_unused, i) => windowRms(after, i * 512, 512)),
  );
  report(
    "Klang reisst beim Wechsel nicht ab",
    quietest > windowRms(before, before.length - 512, 512) * 0.4,
    `leisestes Fenster nach dem Wechsel ${quietest.toExponential(2)}`,
  );
}

// ================================================ Geometrie bis zum Klang
// Die vollstaendige Kette: gezeichnete Form -> Eigenloeser -> Moden -> DSP.
{
  const settings = {
    material: "wood",
    damping: 0.35,
    brightness: 0.6,
    modes: 12,
  };
  const soundOf = (objects) => {
    const { parts } = analyzeGeometry(objects, 34);
    const part = activePart(parts, 100, 100);
    const centre = part.centroid;
    const model = modalModel(part, {
      ...settings,
      excite: { x: centre.x - part.spacing * 5, y: centre.y - part.spacing * 3 },
      pickup: { x: centre.x + part.spacing * 4, y: centre.y + part.spacing * 5 },
    });
    const processor = makeProcessor({
      instrument: "mallet", volume: 0.5, excitation: 0.6, modes: model.modes, gate: false,
    });
    render(processor, 0.4);
    processor.port.onmessage({ data: { gate: true } });
    return {
      model,
      dominant: dominantFrequency(render(processor, 0.5), 60, 3000, 2),
    };
  };

  const square = soundOf([{ type: "rect", x: 40, y: 40, w: 200, h: 200 }]);
  const circle = soundOf([{ type: "ellipse", x: 40, y: 40, w: 200, h: 200 }]);
  const slim = soundOf([{ type: "rect", x: 40, y: 40, w: 320, h: 90 }]);

  report(
    "Gezeichnete Form bestimmt die hoerbare Resonanz",
    Math.abs(square.dominant - circle.dominant) > 8
      && Math.abs(square.dominant - slim.dominant) > 30,
    `Quadrat ${square.dominant} Hz, Kreis ${circle.dominant} Hz, schmales Rechteck ${slim.dominant} Hz`,
  );

  report(
    "Berechnete Grundmode und gehoerte Resonanz stimmen ueberein",
    Math.abs(square.dominant - square.model.modes[0].frequency) < 15,
    `berechnet ${square.model.modes[0].frequency.toFixed(1)} Hz, gemessen ${square.dominant} Hz`,
  );

  // Zusammensetzen senkt den Grundton hoerbar.
  const single = soundOf([{ type: "rect", x: 40, y: 40, w: 160, h: 160 }]);
  const joined = soundOf([
    { type: "rect", x: 40, y: 40, w: 160, h: 160 },
    { type: "rect", x: 200, y: 40, w: 160, h: 160 },
  ]);
  report(
    "Beruehrende Teile klingen hoerbar tiefer als das Einzelteil",
    joined.dominant < single.dominant * 0.92,
    `einzeln ${single.dominant} Hz, verbunden ${joined.dominant} Hz`,
  );

  // Material verschiebt die Lage, nicht die Verhaeltnisse.
  const { parts } = analyzeGeometry([{ type: "rect", x: 40, y: 40, w: 200, h: 200 }], 34);
  const part = activePart(parts, 100, 100);
  const base = {
    damping: 0.35,
    brightness: 0.6,
    modes: 8,
    excite: { x: part.centroid.x - 30, y: part.centroid.y - 20 },
    pickup: { x: part.centroid.x + 25, y: part.centroid.y + 30 },
  };
  const wood = modalModel(part, { ...base, material: "wood" });
  const metal = modalModel(part, { ...base, material: "metal" });
  const shift = metal.modes[0].frequency / wood.modes[0].frequency;
  const ratioWood = wood.modes[3].frequency / wood.modes[0].frequency;
  const ratioMetal = metal.modes[3].frequency / metal.modes[0].frequency;
  report(
    "Material verschiebt die Tonlage, nicht die Modenverhaeltnisse",
    shift > 1.5 && Math.abs(ratioWood - ratioMetal) < 0.01,
    `Metall liegt ${shift.toFixed(2)}x hoeher; Verhaeltnis Mode 4/1 ${ratioWood.toFixed(3)} vs ${ratioMetal.toFixed(3)}`,
  );
  report(
    "Metall klingt laenger nach als Kunststoff",
    metal.modes[0].q > modalModel(part, { ...base, material: "plastic" }).modes[0].q * 10,
    `Guete Metall ${metal.modes[0].q.toFixed(0)}, Kunststoff ${modalModel(part, { ...base, material: "plastic" }).modes[0].q.toFixed(1)}`,
  );

  // Anregungspunkt: eine Mode mit Knoten am Anregungspunkt bleibt stumm.
  const centred = modalModel(part, {
    ...base,
    material: "wood",
    excite: { x: part.centroid.x, y: part.centroid.y },
    pickup: { x: part.centroid.x, y: part.centroid.y },
  });
  const offCentre = modalModel(part, { ...base, material: "wood" });
  const centredSecond = Math.abs(centred.modes[1].gain);
  const offCentreSecond = Math.abs(offCentre.modes[1].gain);
  report(
    "Anregung in der Mitte laesst die zweite Mode verstummen",
    centredSecond < offCentreSecond * 0.2,
    `Mode 2 mittig ${centredSecond.toExponential(2)}, ausser Mitte ${offCentreSecond.toExponential(2)}`,
  );
}

console.log(`\n${checks - failures}/${checks} Pruefungen bestanden.`);
process.exit(failures ? 1 : 0);
