// wav.js
//
// Offline-Rendering als WAV.
//
// Gerendert wird mit demselben AudioWorklet wie die Echtzeitwiedergabe,
// nur in einem OfflineAudioContext. Frueher gab es dafuer eine zweite,
// getrennt gepflegte Implementierung der Synthese; die konnte sich von der
// Echtzeitversion wegentwickeln und tat es auch. Jetzt gibt es nur noch
// einen DSP-Pfad.

const RELEASE_SECONDS = 0.45;

// Gegen die eigene Moduladresse aufgeloest, nicht gegen die Seite: sonst
// haengt das Laden davon ab, aus welchem Verzeichnis die Seite kommt.
const PROCESSOR_URL = new URL("./synth-processor.js", import.meta.url).href;

/**
 * Rendert die aktuelle Einstellung in einen AudioBuffer.
 *
 * @param {object} message Parameter fuer den Worklet, inklusive `modes`
 * @param {number} seconds Laenge
 * @param {number} rate Abtastrate
 */
export async function renderOffline(message, seconds = 5, rate = 44100) {
  if (typeof OfflineAudioContext === "undefined") {
    throw new Error("OfflineAudioContext wird von diesem Browser nicht unterstuetzt");
  }
  const context = new OfflineAudioContext(1, Math.ceil(seconds * rate), rate);
  await context.audioWorklet.addModule(PROCESSOR_URL);

  const synth = new AudioWorkletNode(context, "geometric-synth", {
    outputChannelCount: [1],
    processorOptions: { ...message, gate: true },
  });
  // Eine Ausnahme im Worklet fuehrt sonst nur zu Stille ohne Hinweis.
  let processorError = null;
  synth.onprocessorerror = (event) => {
    processorError = event && event.message ? event.message : "Fehler im Worklet";
  };
  const fade = context.createGain();
  synth.connect(fade).connect(context.destination);

  // Der Ausklang wird ueber die Verstaerkung gesteuert, nicht ueber eine
  // Nachricht an den Worklet: die Zustellung von Nachrichten ist im
  // Offline-Rendering zeitlich nicht abtastgenau, eine Automation schon.
  fade.gain.setValueAtTime(1, 0);
  fade.gain.setValueAtTime(1, Math.max(0, seconds - RELEASE_SECONDS));
  fade.gain.linearRampToValueAtTime(0.0001, seconds);

  synth.port.postMessage({ ...message, gate: true });
  const rendered = await context.startRendering();
  if (processorError) throw new Error(`Worklet: ${processorError}`);
  return rendered;
}

/** Kodiert den ersten Kanal als 16-Bit-PCM-WAV. */
export function encodeWav(buffer) {
  const samples = buffer.getChannelData(0);
  const bytes = new ArrayBuffer(44 + samples.length * 2);
  const view = new DataView(bytes);
  const text = (offset, value) => {
    for (let i = 0; i < value.length; i += 1) view.setUint8(offset + i, value.charCodeAt(i));
  };

  text(0, "RIFF");
  view.setUint32(4, 36 + samples.length * 2, true);
  text(8, "WAVE");
  text(12, "fmt ");
  view.setUint32(16, 16, true);
  view.setUint16(20, 1, true);
  view.setUint16(22, 1, true);
  view.setUint32(24, buffer.sampleRate, true);
  view.setUint32(28, buffer.sampleRate * 2, true);
  view.setUint16(32, 2, true);
  view.setUint16(34, 16, true);
  text(36, "data");
  view.setUint32(40, samples.length * 2, true);

  for (let i = 0; i < samples.length; i += 1) {
    const clamped = Math.max(-1, Math.min(1, samples[i]));
    view.setInt16(44 + i * 2, Math.round(clamped * 32767), true);
  }
  return new Blob([bytes], { type: "audio/wav" });
}

/** Rendert und laedt die Datei herunter. */
export async function downloadWav(message, filename = "geometric-resonator.wav", seconds = 5) {
  const buffer = await renderOffline(message, seconds);
  const url = URL.createObjectURL(encodeWav(buffer));
  const link = document.createElement("a");
  link.href = url;
  link.download = filename;
  link.click();
  setTimeout(() => URL.revokeObjectURL(url), 2000);
}
