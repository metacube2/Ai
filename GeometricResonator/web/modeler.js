// modeler.js
//
// Anwendungslogik: Zustand, Eingabe, Rechenauftraege an den Worker, Audio.
//
// Die eigentliche Physik liegt in eigen.js und physics.js, die Darstellung
// in editor.js, die Synthese in synth-processor.js. Diese Datei haelt das
// zusammen.

import { boundsOf, overallBounds } from "./geometry.js";
import { MATERIALS, DEFAULT_MATERIAL } from "./physics.js";
import { drawScene, hitTest, snapOffset, unionBounds } from "./editor.js";
import { downloadWav } from "./wav.js";

const STORAGE_KEY = "geometric-resonator-design-v2";
const DRAFT_RESOLUTION = 24;
const FINAL_RESOLUTION = 38;
const POINT_RADIUS = 13;

// Gegen die eigene Moduladresse aufgeloest, nicht gegen die Seite: sonst
// haengt das Laden davon ab, aus welchem Verzeichnis die Seite kommt.
const WORKER_URL = new URL("./modes-worker.js", import.meta.url).href;
const PROCESSOR_URL = new URL("./synth-processor.js", import.meta.url).href;

const canvas = document.getElementById("canvas");
const wrap = document.getElementById("canvasWrap");
const context = canvas.getContext("2d");

const controls = Object.fromEntries(
  ["instrument", "material", "pitch", "excitation", "damping", "brightness", "volume"]
    .map((id) => [id, document.getElementById(id)]),
);
const fields = Object.fromEntries(
  ["objectWidth", "objectHeight", "objectX", "objectY"]
    .map((id) => [id, document.getElementById(id)]),
);
const readout = document.getElementById("analysis");
const modeList = document.getElementById("modeList");
const selectionStatus = document.getElementById("selectionStatus");
const audioState = document.getElementById("audioState");

const state = {
  objects: [],
  selected: new Set(),
  tool: "select",
  excite: { x: 260, y: 220 },
  pickup: { x: 360, y: 300 },
  analysis: null,
  modes: [],
  sounding: false,
  level: 0,
  dragging: null,
  snapHint: null,
  snapping: true,
};

let nextId = 1;
let operation = null;
let audioContext = null;
let synth = null;
let worker = null;
let requestToken = 0;
let pendingDraft = 0;

// ------------------------------------------------------------- Werkzeuge

function setTool(value) {
  state.tool = value;
  document.querySelectorAll("[data-tool]").forEach((button) => {
    button.classList.toggle("active", button.dataset.tool === value);
  });
  canvas.style.cursor = value === "select" ? "default" : "crosshair";
}

function selectedObjects() {
  return state.objects.filter((object) => state.selected.has(object.id));
}

function pointerPosition(event) {
  const rect = canvas.getBoundingClientRect();
  return { x: event.clientX - rect.left, y: event.clientY - rect.top };
}

function normalize(object) {
  if (object.w < 0) { object.x += object.w; object.w = -object.w; }
  if (object.h < 0) { object.y += object.h; object.h = -object.h; }
  return object;
}

function near(point, target) {
  return Math.hypot(point.x - target.x, point.y - target.y) <= POINT_RADIUS;
}

/** Setzt Anregung und Abnahme auf sinnvolle Startpunkte im Koerper. */
function placePoints() {
  const bounds = overallBounds(state.objects);
  if (!bounds) return;
  state.excite = { x: bounds.x + bounds.w * 0.32, y: bounds.y + bounds.h * 0.36 };
  state.pickup = { x: bounds.x + bounds.w * 0.68, y: bounds.y + bounds.h * 0.64 };
}

// ------------------------------------------------------------- Eingabe

canvas.addEventListener("pointerdown", (event) => {
  const point = pointerPosition(event);
  canvas.setPointerCapture(event.pointerId);

  if (state.objects.length && near(point, state.excite)) {
    operation = { kind: "point", which: "excite" };
    state.dragging = "excite";
    return;
  }
  if (state.objects.length && near(point, state.pickup)) {
    operation = { kind: "point", which: "pickup" };
    state.dragging = "pickup";
    return;
  }

  if (state.tool !== "select") {
    const wasEmpty = state.objects.length === 0;
    const object = { id: nextId, type: state.tool, x: point.x, y: point.y, w: 1, h: 1 };
    nextId += 1;
    state.objects.push(object);
    state.selected = new Set([object.id]);
    operation = { kind: "draw", object, start: point, wasEmpty };
    updateInspector();
    return;
  }

  const object = hitTest(state.objects, point);
  if (!object) {
    if (!event.shiftKey) state.selected.clear();
    operation = null;
    updateInspector();
    return;
  }
  if (event.shiftKey) {
    if (state.selected.has(object.id)) state.selected.delete(object.id);
    else state.selected.add(object.id);
  } else if (!state.selected.has(object.id)) {
    state.selected = new Set([object.id]);
  }
  operation = {
    kind: "move",
    start: point,
    originals: selectedObjects().map((item) => ({ item, x: item.x, y: item.y })),
  };
  updateInspector();
});

canvas.addEventListener("pointermove", (event) => {
  if (!operation) return;
  const point = pointerPosition(event);

  if (operation.kind === "point") {
    state[operation.which] = point;
    requestModes(DRAFT_RESOLUTION);
    return;
  }

  if (operation.kind === "draw") {
    operation.object.w = point.x - operation.start.x;
    operation.object.h = point.y - operation.start.y;
    state.snapHint = null;
  } else {
    const dx = point.x - operation.start.x;
    const dy = point.y - operation.start.y;
    for (const entry of operation.originals) {
      entry.item.x = entry.x + dx;
      entry.item.y = entry.y + dy;
    }
    applySnapping(operation.originals.map((entry) => entry.item));
  }
  updateInspector();
  requestModes(DRAFT_RESOLUTION);
});

canvas.addEventListener("pointerup", () => {
  if (operation?.kind === "draw") {
    const object = normalize(operation.object);
    if (object.w < 10 || object.h < 10) {
      state.objects = state.objects.filter((item) => item !== object);
      state.selected.clear();
    } else {
      applySnapping([object]);
      if (operation.wasEmpty) placePoints();
    }
    setTool("select");
  }
  operation = null;
  state.dragging = null;
  state.snapHint = null;
  updateInspector();
  requestModes(FINAL_RESOLUTION);
  saveDesign();
});

/**
 * Rastet die bewegte Auswahl an den uebrigen Teilen ein. Ohne das muesste
 * man pixelgenau zielen, um zwei Teile zu einem Klangkoerper zu verbinden.
 */
function applySnapping(moved) {
  if (!state.snapping) { state.snapHint = null; return; }
  const movedIds = new Set(moved.map((item) => item.id));
  const others = state.objects.filter((item) => !movedIds.has(item.id));
  const offset = snapOffset(moved, others);
  if (!offset.dx && !offset.dy) { state.snapHint = null; return; }
  for (const item of moved) {
    item.x += offset.dx;
    item.y += offset.dy;
  }
  state.snapHint = (offset.contactX || offset.contactY) ? unionBounds(moved) : null;
}

// ------------------------------------------------------------ Inspektor

function updateInspector() {
  const list = selectedObjects();
  const single = list.length === 1 ? list[0] : null;
  const names = { rect: "Rechteck", ellipse: "Ellipse", triangle: "Dreieck" };
  selectionStatus.textContent = list.length
    ? `${list.length} Teil${list.length > 1 ? "e" : ""} ausgewählt${single ? ` · ${names[single.type]}` : ""}`
    : "Kein Teil ausgewählt";
  for (const field of Object.values(fields)) field.disabled = !single;
  if (single) {
    const box = boundsOf(single);
    fields.objectWidth.value = Math.round(box.w);
    fields.objectHeight.value = Math.round(box.h);
    fields.objectX.value = Math.round(box.x);
    fields.objectY.value = Math.round(box.y);
  }
}

for (const [id, field] of Object.entries(fields)) {
  field.addEventListener("change", () => {
    const object = selectedObjects()[0];
    if (!object) return;
    const raw = Number(field.value);
    if (!Number.isFinite(raw)) return;
    if (id === "objectWidth") object.w = Math.max(10, raw);
    if (id === "objectHeight") object.h = Math.max(10, raw);
    if (id === "objectX") object.x = raw;
    if (id === "objectY") object.y = raw;
    updateInspector();
    requestModes(FINAL_RESOLUTION);
    saveDesign();
  });
}

// -------------------------------------------------------------- Aktionen

document.getElementById("toolbar").addEventListener("click", (event) => {
  const button = event.target.closest("[data-tool]");
  if (button) setTool(button.dataset.tool);
});

function afterEdit() {
  updateInspector();
  requestModes(FINAL_RESOLUTION);
  saveDesign();
}

document.getElementById("duplicate").onclick = () => {
  const copies = selectedObjects().map((object) => ({
    ...object, id: nextId++, x: object.x + 20, y: object.y + 20,
  }));
  if (!copies.length) return;
  state.objects.push(...copies);
  state.selected = new Set(copies.map((object) => object.id));
  afterEdit();
};

document.getElementById("remove").onclick = () => {
  state.objects = state.objects.filter((object) => !state.selected.has(object.id));
  state.selected.clear();
  afterEdit();
};

document.getElementById("clear").onclick = () => {
  state.objects = [];
  state.selected.clear();
  state.analysis = null;
  state.modes = [];
  afterEdit();
};

document.getElementById("example").onclick = () => {
  state.objects = [
    { id: nextId++, type: "ellipse", x: 180, y: 120, w: 260, h: 190 },
    { id: nextId++, type: "rect", x: 440, y: 175, w: 150, h: 80 },
    { id: nextId++, type: "triangle", x: 250, y: 310, w: 170, h: 130 },
  ];
  state.selected.clear();
  placePoints();
  afterEdit();
};

document.getElementById("snapToggle").onclick = (event) => {
  state.snapping = !state.snapping;
  event.currentTarget.classList.toggle("active", state.snapping);
  event.currentTarget.textContent = state.snapping ? "⇥ Einrasten an" : "⇥ Einrasten aus";
};

window.addEventListener("keydown", (event) => {
  if ((event.key === "Delete" || event.key === "Backspace") && !event.target.matches("input,select")) {
    event.preventDefault();
    document.getElementById("remove").click();
  }
});

// ------------------------------------------------------ Rechenauftraege

function settings() {
  return {
    material: controls.material.value,
    damping: Number(controls.damping.value),
    brightness: Number(controls.brightness.value),
  };
}

function ensureWorker() {
  if (worker) return worker;
  worker = new Worker(WORKER_URL, { type: "module" });
  worker.onmessage = (event) => {
    const data = event.data;
    if (data.token !== requestToken) return;
    if (data.error) {
      readout.textContent = `Berechnung fehlgeschlagen: ${data.error}`;
      return;
    }
    if (data.empty) {
      state.analysis = null;
      state.modes = [];
      renderReadout(null);
      if (synth) synth.port.postMessage({ modes: [] });
      return;
    }
    state.analysis = data;
    state.modes = data.modes;
    if (synth) synth.port.postMessage({ modes: data.modes });
    renderReadout(data);
  };
  worker.onerror = (event) => {
    readout.textContent = `Worker-Fehler: ${event.message || "unbekannt"}`;
  };
  return worker;
}

/**
 * Fordert eine neue Eigenloesung an. Waehrend des Ziehens wird grob
 * gerechnet, damit die Rueckmeldung sofort kommt; nach dem Loslassen fein.
 */
function requestModes(resolution) {
  if (!state.objects.length) {
    state.analysis = null;
    state.modes = [];
    renderReadout(null);
    if (synth) synth.port.postMessage({ modes: [] });
    return;
  }
  if (resolution === DRAFT_RESOLUTION) {
    const now = performance.now();
    if (now - pendingDraft < 70) return;
    pendingDraft = now;
  }
  requestToken += 1;
  ensureWorker().postMessage({
    token: requestToken,
    objects: state.objects.map(({ id, type, x, y, w, h }) => ({ id, type, x, y, w, h })),
    settings: settings(),
    resolution,
    excite: state.excite,
    pickup: state.pickup,
  });
}

function renderReadout(data) {
  if (!data) {
    readout.innerHTML = "Zeichne mindestens ein Teil.";
    modeList.innerHTML = "";
    return;
  }
  const d = data.diagnostics;
  const material = MATERIALS[controls.material.value] ?? MATERIALS[DEFAULT_MATERIAL];
  const drawn = state.objects.length;
  // Interessant ist nicht die Zahl der gezeichneten Teile allein, sondern
  // wie viele Koerper daraus geworden sind: genau das steuert der Nutzer,
  // indem er Teile aneinanderschiebt.
  const bodies = d.parts === 1
    ? "ein zusammenhängender Klangkörper"
    : `${d.parts} getrennte Klangkörper · es klingt der mit dem Anregungspunkt`;
  readout.innerHTML = `
    <strong>${drawn} Teil${drawn === 1 ? "" : "e"} → ${bodies}</strong><br>
    Material: ${material.label} · Grundton ${d.fundamental.toFixed(1)} Hz<br>
    Gitter: ${d.activeCells} Zellen à ${d.spacing.toFixed(1)} px · ${d.kept} Moden<br>
    Rechenzeit: ${d.milliseconds} ms`;
  modeList.innerHTML = data.modes.slice(0, 12).map((mode, index) => {
    const strength = Math.min(1, Math.abs(mode.gain));
    return `<div class="mode-row">
      <span class="mode-index">${index + 1}</span>
      <span class="mode-frequency">${mode.frequency.toFixed(0)} Hz</span>
      <span class="mode-bar"><i style="width:${(strength * 100).toFixed(0)}%"></i></span>
      <span class="mode-q">Q ${mode.q.toFixed(0)}</span>
    </div>`;
  }).join("");
}

// ----------------------------------------------------------------- Audio

function audioMessage(extra = {}) {
  return {
    instrument: controls.instrument.value,
    pitch: Number(controls.pitch.value),
    excitation: Number(controls.excitation.value),
    volume: Number(controls.volume.value),
    modes: state.modes,
    ...extra,
  };
}

async function ensureAudio() {
  if (!audioContext) {
    audioContext = new AudioContext({ latencyHint: "interactive" });
    await audioContext.audioWorklet.addModule(PROCESSOR_URL);
    synth = new AudioWorkletNode(audioContext, "geometric-synth", {
      outputChannelCount: [1],
      processorOptions: audioMessage(),
    });
    synth.onprocessorerror = () => { audioState.textContent = "Fehler im Audio-Worklet"; };
    synth.connect(audioContext.destination);
    synth.port.onmessage = (event) => {
      if (event.data.level !== undefined) state.level = event.data.level;
    };
    synth.port.postMessage(audioMessage());
  }
  if (audioContext.state !== "running") await audioContext.resume();
  audioState.textContent = "Audio aktiv";
}

document.getElementById("render").onclick = async () => {
  if (!state.objects.length) return;
  try {
    await ensureAudio();
    state.sounding = true;
    synth.port.postMessage(audioMessage({ gate: true }));
  } catch (error) {
    audioState.textContent = `Audio nicht verfuegbar: ${error.message}`;
  }
};

document.getElementById("stop").onclick = () => {
  state.sounding = false;
  if (synth) synth.port.postMessage({ gate: false });
};

document.getElementById("wav").onclick = async (event) => {
  if (!state.modes.length) return;
  const button = event.currentTarget;
  const label = button.textContent;
  button.disabled = true;
  button.textContent = "rendert…";
  try {
    await downloadWav(audioMessage());
  } catch (error) {
    audioState.textContent = `WAV fehlgeschlagen: ${error.message}`;
  } finally {
    button.disabled = false;
    button.textContent = label;
  }
};

for (const [id, control] of Object.entries(controls)) {
  control.addEventListener("input", () => {
    updateLabels();
    // Material, Daempfung und Helligkeit gehen in die Modenberechnung ein,
    // die uebrigen Regler nur in die Anregung.
    if (id === "material" || id === "damping" || id === "brightness") {
      requestModes(FINAL_RESOLUTION);
    } else if (synth) {
      synth.port.postMessage(audioMessage());
    }
    saveDesign();
  });
}

function updateLabels() {
  for (const id of ["pitch", "excitation", "damping", "brightness", "volume"]) {
    const value = Number(controls[id].value);
    const output = document.getElementById(`${id}Out`);
    if (output) output.textContent = id === "pitch" ? `${Math.round(value)} Hz` : value.toFixed(2);
  }
  const bowed = controls.instrument.value !== "mallet";
  document.getElementById("pitchControl").style.opacity = bowed ? "1" : ".4";
}

// ----------------------------------------------------------- Persistenz

function saveDesign() {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify({
      objects: state.objects,
      nextId,
      excite: state.excite,
      pickup: state.pickup,
      values: Object.fromEntries(
        Object.entries(controls).map(([id, control]) => [id, control.value]),
      ),
    }));
  } catch (_error) { /* Speicher voll oder gesperrt: nicht kritisch */ }
}

function loadDesign() {
  try {
    const saved = JSON.parse(localStorage.getItem(STORAGE_KEY) || "null");
    if (!saved) return;
    // Entwuerfe der Vorversion enthalten ein Feld `group`; es wird ignoriert,
    // weil Verbundenheit jetzt aus der Geometrie folgt.
    state.objects = (saved.objects || []).map(({ id, type, x, y, w, h }) => ({ id, type, x, y, w, h }));
    nextId = saved.nextId || state.objects.length + 1;
    if (saved.excite) state.excite = saved.excite;
    if (saved.pickup) state.pickup = saved.pickup;
    for (const [id, value] of Object.entries(saved.values || {})) {
      if (controls[id]) controls[id].value = value;
    }
  } catch (_error) { /* beschaedigter Eintrag: leer starten */ }
}

// ----------------------------------------------------------- Darstellung

function resizeCanvas() {
  const rect = wrap.getBoundingClientRect();
  const ratio = Math.min(devicePixelRatio || 1, 2);
  canvas.width = Math.round(rect.width * ratio);
  canvas.height = Math.round(rect.height * ratio);
  context.setTransform(ratio, 0, 0, ratio, 0, 0);
}
new ResizeObserver(resizeCanvas).observe(wrap);

function frame(now) {
  const rect = wrap.getBoundingClientRect();
  drawScene(context, { width: rect.width, height: rect.height }, state, now / 1000);
  requestAnimationFrame(frame);
}

// ------------------------------------------------------------------ Start

for (const key of Object.keys(MATERIALS)) {
  const option = document.createElement("option");
  option.value = key;
  option.textContent = MATERIALS[key].label;
  controls.material.append(option);
}
controls.material.value = DEFAULT_MATERIAL;

loadDesign();
setTool("select");
updateInspector();
updateLabels();
resizeCanvas();
requestModes(FINAL_RESOLUTION);
requestAnimationFrame(frame);
